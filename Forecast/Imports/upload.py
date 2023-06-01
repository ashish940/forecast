#!/usr/bin/python
# -- coding: utf-8 --?
'''File Check libraries'''
from os import listdir
from os.path import isfile, join
from os.path import basename
import fnmatch
import shutil
import time

import sys
import pyodbc
import datetime
import argparse
import uuid

import os
import subprocess
sys.path.append('//yoda.gms.local/C$/ibi_prod/ibi/apps/production/shared_queries')
from smtp import send_email, send_html_email
from datetime import datetime, timedelta

def create_html_table(headers, failures):
	""" Returns a user-friendly html table of failures from an SQL table
	REQS: 	1) List of header columns for the failures
			2) List of failures to email
	"""
	header = [i[0] for i in headers]
	rows = [list(i) for i in failures]
	rows.insert(0,header)
	htable=u'<table border="1" bordercolor=000000 cellspacing="0" cellpadding="1" style="table-layout:fixed;vertical-align:bottom;font-size:13px;font-family:verdana,sans,sans-serif;border-collapse:collapse;border:1px solid rgb(130,130,130)" >'
	rows[0] = [u'<b>' + i + u'</b>' for i in rows[0]] 
	for row in rows:
		newrow = u'<tr>' 
		newrow += u'<td align="left" style="padding:1px 4px">'+unicode(row[0])+u'</td>'
		row.remove(row[0])
		newrow = newrow + ''.join([u'<td align="right" style="padding:1px 4px">' + unicode(x) + u'</td>' for x in row])  
		newrow += '</tr>' 
		htable+= newrow
	htable += '</table>'	
	return htable

def get_min_or_max_partitions_vendors(conn, f_schema, tablename, minormax):	
	"""Grabs the min or max partition values for a vendor-level table.
	   Coalesces any null values with current timestamp.
	REQ: 1) Connection required 
		 2) Dev/production environment schema for forecast objects.
		 3) Tablename to pull values from
		 4) 0 for min value, 1 for max value
	"""
	
	if minormax == 0:
		aggvalue = 'min'
	elif minormax == 1:
		aggvalue = 'max'
	else:
		raise ValueError("Incorrect value for min or max parameter")
	cur = conn.cursor()
	
	cur.execute("""select """+aggvalue+"""(timestamp) from """+f_schema+tablename+""";""")
	partition_value = cur.fetchone()[0]
		
	if partition_value is None:
		partition_value = datetime.now()
	
	return partition_value

def print_to_forecast_log(cursor, schema, gmsvenid, vendor_desc, filetype, filename, success, username, message, duration):
	""" Logs upload activity to vertica forecast log table log_uploads.
	REQ: 1) Cursor for db connection
		 2) Dev/production environment schema for forecast objects.
		 3) GMS Internal vendorid processing upload for
		 4) Vendor description of upload user
		 5) Upload file type - ie. Item/MM Total
		 6) Name of uploaded file
		 7) Boolean indicating whether log message reflect success/failure
		 8) Uploading user
		 9) Message to log
		 10) Duration of upload processing thus far 
	"""
	cursor.execute("""INSERT INTO {schema}log_uploads (GMSVenID, VendorDesc, FileUploadType,FileName, TIMESTAMP, Success, user_login, SuccessOrFailureMessage,duration ) 
		VALUES({gmsvenid},'{vendor}','{fileuploadtype}','{filename}',current_timestamp,{success},'{username}','{message}','{duration}'::time);
				""".format(schema = schema, gmsvenid = gmsvenid, vendor = vendor_desc, fileuploadtype = filetype, filename = filename, success = success, username = username, message = message, duration = duration))

try:
	'''ARGUMENT PARSER'''
	parser = argparse.ArgumentParser(description='Schema, gmsvenid, tablename, filename, username')
	parser.add_argument('schema', metavar='N', type=str, nargs=1, help='DM environment: ie: dev.')
	parser.add_argument('gmsvenid', metavar='N', type=int, nargs=1, help='A single GMSVenID.  If not a vendor, input 0. ie: 1')
	parser.add_argument('email', metavar='N', type=str, nargs=1, help='Customer Email')
	parser.add_argument('tablename', metavar='N', type=str, nargs=1, help ='Tablename not in quotes')
	parser.add_argument('stageTablename', metavar='N', type=str, nargs=1, help ='stageTablename not in quotes')
	parser.add_argument('filename', metavar='N', type=str, nargs=1, help='file name not in quotes ie. plantforecast_UUID.csv')
	parser.add_argument('username', metavar='N', type=str, nargs=1, help='username not in quotes ie. name')

	args = parser.parse_args()
	
	'''SET SOME GLOBAL VARS'''
	if args.schema[0] == 'public.':
		'''CONNECT TO VERTICA'''
		conn = pyodbc.connect("DSN=Vertica_DM_Prod")
		cur = conn.cursor()
		v_env = 'public.'
		p_env = 'forecast.'
		v_ftp_yoda = r'/mnt_yoda/FTPVertica/Forecast/'
		
		v_yoda = r'//yoda.gms.local/C$/FTPVertica/Forecast/'
		success_recipients = [args.email[0], 'operations@demandlink.com']
		failure_recipients = [args.email[0], 'support@demandlink.com']
		internal_recipients = ['support@demandlink.com']
		
		email_sender = 'operations@demandlink.com'
		e_env = ''
	else:
		'''CONNECT TO VERTICA'''
		conn = pyodbc.connect("DSN=Vertica_DM_Dev")
		cur = conn.cursor()
		v_env = 'dev.'	
		p_env = 'forecast_dev.'
		v_ftp_yoda = r'/mnt_yoda/FTPVertica_Dev/Forecast/'
		v_yoda = r'//yoda.gms.local/C$/FTPVertica_Dev/Forecast/'
		success_recipients = ['operations@demandlink.com']
		failure_recipients = ['support@demandlink.com']
		internal_recipients = ['support@demandlink.com']
		email_sender = 'operations@demandlink.com'
		e_env = ' - DEV'
		print("Using DEV")
	
	v_ftp = os.path.dirname(os.path.abspath(__file__))
	
	yoda_path = v_yoda+args.filename[0]
	v_path = v_ftp+'\\'+args.filename[0]
	ftp_yoda = v_ftp_yoda+args.filename[0]
	
	success_path = v_yoda+'/processed/'+str(args.filename[0])
	failure_path = v_yoda+'/error/'+str(args.filename[0])
	print(yoda_path)
	print(failure_path)
	print(success_path)
	print("BEGIN forecast upload for: "+ str(args.gmsvenid[0]))

	start_time = datetime.now()
	VendorDesc = ""
	cur.execute("""SELECT distinct VendorDesc
				FROM """ +p_env+args.tablename[0] +""" WHERE GMSVenID ="""+str(args.gmsvenid[0])+""";"""	)
	temp = cur.fetchall()
	temp = temp[0][0].encode("utf-8" )
	VendorDesc = temp

	# COPY FILE TO LOCAL SERVER
	p1 = subprocess.Popen("robocopy "+v_ftp+" "+v_yoda+" "+str(args.filename[0])+" /MOV /is /it")
	p1.wait()

	# Load in raw data
	print("Begin LOAD RAW DATA: "+str(args.filename[0]))
	fileType = "Unknown"
	q_create = """CREATE FLEX LOCAL TEMP TABLE forecast_upload(GMSVenID int, Item int, LGM varchar(30), Patch varchar(12), Region int, "Fiscal Wk" int, "Cost Prev52-1 YR Ago" numeric(18,2), "Cost Prev52" numeric(18,2), "Cost FY" numeric(18,2), "Retail Price Prev52-1 YR Ago" numeric(18,2) , "Retail Price Prev52" numeric(18,2) , "Retail Price FY" numeric(18,2), "Sales Units FY" int ,  "Vendor Comments" LONG varchar) ON COMMIT PRESERVE ROWS;"""
	
	cur.execute(q_create)
	q_stage = """COPY forecast_upload(__raw__, "gmsvenid" as {gmsvenid}::int, Item, LGM, Patch, Region, "Fiscal Wk", "Cost Prev52-1 YR Ago", "Cost Prev52", "Cost FY", "Retail Price Prev52-1 YR Ago", "Retail Price Prev52", "Retail Price FY", "Sales Units FY", "Vendor Comments") 
				from '{filepath}' ON ANY NODE parser PUBLIC.FCSVPARSER(reject_on_materialized_type_error=true, reject_on_duplicate=true, header=true) 
						rejected data as table tmp_f_upload_load_rejected_data SKIP 1 NO COMMIT;
				""".format(gmsvenid = args.gmsvenid[0], filepath = ftp_yoda)	
	cur.execute(q_stage)
	conn.commit()

	# Scrub data for errors
	print("Begin Check for type errors and dups")
	q_errors = """SELECT rejected_data, rejected_reason FROM tmp_f_upload_load_rejected_data LIMIT 20;"""
	cur.execute(q_errors)
	failures = cur.fetchall()
	if cur.rowcount > 0:
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error{e_env}".format(e_env=e_env)
		email_begin = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  Here is the error:   """
		email_end = """If the error makes sense to you and you can fix the file yourself please resubmit the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.</br></br>Thanks! </br>DemandLink"""
		htable = create_html_table(cur.description, failures)
		send_html_email(failure_recipients,email_sender,email_subject,email_begin,email_end, htable)
		print("Raw Data Failure: Email sent to {recipients}".format(recipients = failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, args.username[0], 'RAW Data Failure', duration)
		conn.commit()
		sys.exit()		
	
	# Determine which template is being used
	# Item/LGM, Item/LGM/Week, Item/Patch, Item/Patch/Week, item/Region/LGM
	print("Determine template and columns used")
	cur.execute("""SELECT compute_flextable_keys('forecast_upload');""")

	mm_flag = False
	patch_flag = False
	week_flag = False
	item_flag = False
	price_flag = False
	price_flag_FC = False
	price_flag_TY = False
	price_flag_LY = False
	cost_flag = False
	cost_flag_FC = False
	cost_flag_TY = False
	cost_flag_LY = False
	units_flag = False
	comments_flag = False
	region_flag = False
	print("Columns used in template: ")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'LGM';""")
	if cur.rowcount > 0:
		mm_flag = True
		print("LGM")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'PATCH';""")
	if cur.rowcount > 0:
		patch_flag = True
		print("Patch")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'FISCAL WK';""")
	if cur.rowcount > 0:
		week_flag = True
		print("Week")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'ITEM';""")
	if cur.rowcount > 0:
		item_flag = True
		print("Item")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'RETAIL PRICE FY';""")
	if cur.rowcount > 0:
		price_flag = True
		price_flag_FC = True
		print("Price FY")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'RETAIL PRICE PREV52';""")
	if cur.rowcount > 0:
		price_flag = True
		price_flag_TY = True
		print("Price TY")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'RETAIL PRICE PREV52-1 YR AGO';""")
	if cur.rowcount > 0:
		price_flag = True
		price_flag_LY = True
		print("Price LY")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'COST FY';""")
	if cur.rowcount > 0:
		cost_flag = True
		cost_flag_FC = True
		print("Cost FY")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'COST PREV52';""")
	if cur.rowcount > 0:
		cost_flag = True
		cost_flag_TY = True
		print("Cost Prev52")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'COST PREV52-1 YR AGO';""")
	if cur.rowcount > 0:
		cost_flag = True
		cost_flag_LY = True
		print("Cost Prev52-1 YR Ago")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'SALES UNITS FY';""")
	if cur.rowcount > 0:
		units_flag = True
		print("Units")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'VENDOR COMMENTS';""")
	if cur.rowcount > 0:
		comments_flag = True
		print("Comments")
	cur.execute("""SELECT key_name from forecast_upload_keys where UPPER(key_name) = 'REGION';""")
	if cur.rowcount > 0:
		region_flag = True
		print("Region")

	if item_flag is True and patch_flag is True and week_flag is True and mm_flag is False and region_flag is False:
		fileType = "Item/Patch/Week"
	elif item_flag is True and mm_flag is True and week_flag is True and patch_flag is False and region_flag is False:
		fileType = "Item/LGM/Week" 
	elif item_flag is True and patch_flag is True and week_flag is False and mm_flag is False and region_flag is False:
		fileType = "Item/Patch/Total"
	elif item_flag is True and mm_flag is True and week_flag is False and patch_flag is False and region_flag is False:
		fileType = "Item/LGM/Total"
	elif region_flag is True and mm_flag is True and item_flag is True and patch_flag is False and week_flag is False:
		fileType="Item/Region/LGM"
	else: 
		fileType = "Incorrect Format"
	
	print("FileType = "+ fileType)
	login = str(args.username[0])
		
	# Check some file constraints
	print("Run some data checks")
	if mm_flag is True and patch_flag is True:
		os.rename(yoda_path, failure_path)
		error = "ERROR: File has both LGM and Patch columns.  Must only contain one."
		email_subject = "Forecast File Load Error"+e_env
		email_msg = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  The file you uploaded has both "LGM" and "Patch" columns.  The file must only contain one of these fields.  Please remove one and try the upload again.\n\nIf the error makes sense to you and you can fix the file yourself please reupload the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)
		print(error)
		print("Email sent to " +str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()	

	if mm_flag is False and patch_flag is False:
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_msg = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  The file you uploaded does not have an "LGM" or a "Patch" column.  The file must contain at least one of these columns with values.  Please adjust the file and try the upload again.\n\nIf the error makes sense to you and you can fix the file yourself please reupload the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)
		error = "ERROR: File doesnt have LGM or Patch columns.  Must contain at least one of these columns."
		print(error)
		print("Email sent to " +str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()	

	if item_flag is False:
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_msg = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  The file you uploaded does not have an "Item" column or has no values in that column.  The file must contain this column with values.  Please adjust the file and try the upload again.\n\nIf the error makes sense to you and you can fix the file yourself please reupload the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)
		error = "ERROR: File doesnt have ItemID columns  Must contain ItemID to upload."
		print(error)
		print("Email sent to " +str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()	

	if comments_flag is True and (patch_flag is True or week_flag is True or region_flag is True):
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_msg = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  The comments field can only be uploaded with the "Item/LGM Template". Please adjust the file and try the upload again.\n\nIf the error makes sense to you and you can fix the file yourself please reupload the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)
		error = "ERROR: File contains comments field with invalid dimensional columns.Comments can only be uploaded with the Item/LGM template."
		print(error)
		print("Email sent to " +str(failure_recipients))
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()	

	if mm_flag is True and (price_flag is True or cost_flag is True):
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_msg = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  The Retail Price and Cost fields can only be uploaded with the "Item Patch Week" or "Item Patch Total" Template. Please adjust the file and try the upload again.\n\nIf the error makes sense to you and you can fix the file yourself please reupload the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)
		error = "ERROR: File contains retail price and/or cost field with invalid dimensional columns.  Retail Price/Cost can only be uploaded with an Item/Patch template."
		print(error)
		print("Email sent to " +str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()

	if region_flag is True and week_flag is True:
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_msg = """\nAwesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload on our forecast tool.  The "Item Region MM" Template can only be uploaded at the Total level.  Please remove fiscal week from your template and reupload the file! \n\nIf the error makes sense to you and you can fix the file yourself please reupload the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)
		error = "ERROR: File contains fiscalwk field with invalid dimensional columns.  Item/Reg/LGM template can only be uploaded at the total level."
		print(error)
		print("Email sent to " +str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()		
	
	# Determine if tool is frozen 
	fc_basis = ''
	cur.execute("""SELECT flagValue FROM {schema}config_tool WHERE flagName = 'freeze';""".format(schema = p_env))
	FreezeFlag = cur.fetchone()[0]
	print("FreezeFlag = {freezeflag}".format(freezeflag = FreezeFlag))
	if FreezeFlag is False:
		fc_basis = 'salesunits_fc'
	else: 
		fc_basis = 'units_fc_vendor'
	
	# Determine dimensional columns
	# GMSVenID and ItemID are required dimensional columns, begin by building this out
	print("Build dynamic queries")
	key_columns = """u.GMSVenID, u.ItemID"""
	flex_columns = """u.GMSVenID, u.Item as ItemID"""
	update_predicate = """ON t.GMSVenID = u.GMSVenID AND t.ItemID = u.itemid"""
	sums_where = """WHERE t.itemid = u.itemid"""
	curve_where = """AssrtID"""
	join_fw = ""
	join_predicate = """ON u.itemid = k.itemid"""
	part_upload= ""
	partition_by = """ItemID"""
	cumulative_sum = ""
	rounding_assignment = ""
	
	ignore_dup_null_columns = """u.ItemID"""

	if mm_flag is True:
		update_predicate += """ AND t.MM = u.LGM"""
		key_columns += """, u.LGM"""
		ignore_dup_null_columns += """,u.LGM"""
		flex_columns += """, u.LGM"""
		sums_where += """ AND t.MM = u.LGM"""
		join_predicate += """ AND u.LGM = k.MM"""
		part_upload += """, t.mm"""
		partition_by += """, mm"""
	if patch_flag is True:
		update_predicate += """ AND t.Patch = u.Patch"""
		key_columns += """, u.Patch"""
		ignore_dup_null_columns += """, u.Patch"""
		flex_columns += """, u.Patch"""
		sums_where += """ AND t.Patch = u.Patch"""
		join_predicate += """ AND u.Patch = k.Patch"""
		part_upload += """,t.Patch"""
		partition_by += """, Patch"""
	if week_flag is True:
		update_predicate += """ AND t.FiscalWk = u.FiscalWk"""
		key_columns += """, u.FiscalWk"""
		ignore_dup_null_columns += """, u.FiscalWk"""
		flex_columns += """, u.'Fiscal Wk' as FiscalWk"""
		sums_where += """ AND t.Fiscalwk = u.Fiscalwk"""
		curve_where += """, FiscalWk"""
		partition_by += """, FiscalWk"""
	if region_flag is True:
		update_predicate += """ AND t.Region = u.Region"""
		key_columns += """, u.Region"""
		ignore_dup_null_columns += """, u.Region"""
		flex_columns += """, u.Region"""
		sums_where += """ AND t.Region = u.Region"""
		curve_where += """, Region"""
		join_predicate += """ AND u.Region = k.Region"""
		part_upload += """, t.Region"""
		partition_by += """, Region"""

	# More data scrubs
	cur.execute(""" DROP TABLE IF EXISTS tmp_forecast_upload_dups;
					CREATE LOCAL TEMP TABLE tmp_forecast_upload_dups on commit preserve rows as 
					select  """+flex_columns+"""
					from forecast_upload u;""")
	conn.commit()
	print("Checking for dups")
	cur.execute("""SELECT DISTINCT """+key_columns+""" FROM (SELECT """+key_columns+""", row_number() over(partition by """+key_columns+""") as row_num FROM tmp_forecast_upload_dups u)u WHERE row_num > 1 and ("""+ignore_dup_null_columns+""") IS NOT NULL limit 10;""")
	dups = cur.fetchall()
	if cur.rowcount > 0:
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_begin = """Awesome DemandLink Customer!\n\nWe detected duplicates in the file you just tried to upload on our forecast tool.  Here are some of the duplicates:   """
		email_end = """If the error makes sense to you and you can fix the file yourself please resubmit the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.</br></br>Thanks! </br>DemandLink"""
		htable = create_html_table(cur.description, dups)
		send_html_email(failure_recipients,email_sender,email_subject,email_begin,email_end, htable)
		error = "Duplicates found in file."
		print("Duplicates found in file: Email sent to " + str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()	

	print("Checking for negatives")
	cur.execute("""SELECT """+key_columns+""", SalesUnits_FC as SalesUnitsFY FROM forecast_upload u WHERE SalesUnits_FC < 0 limit 10;""")
	negatives = cur.fetchall()
	if cur.rowcount > 0:
		email_subject = "Forecast File Load Error"+e_env
		email_begin = """Awesome DemandLink Customer!\n\nWe detected negative forecast unit values in the file you just tried to upload on our forecast tool. Only non-negative values can be uploaded into the tool. Here are some of the values:   """
		email_end = """If the error makes sense to you and you can fix the file yourself please resubmit the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.</br></br>Thanks! </br>DemandLink"""
		htable = create_html_table(cur.description, negatives)
		send_html_email(failure_recipients,email_sender,email_subject,email_begin,email_end, htable)
		error = "Negative values found in file."
		print("Negative values in file: Email sent to " + str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		conn.commit()
		sys.exit()	

	print("determine columns to update")
	# Determine columns to update
	create_columns=''
	flex_create_columns = ''
	dimensional_columns=''
	cumulative_sum = ''
	rounding_assignment = ''
	if price_flag_FC is True:
		dimensional_columns += """u.RetailPrice_FC
								,CASE WHEN NULLIF(t.RetailPrice_FC,0) IS NULL
										THEN u.RetailPrice_FC
										ELSE u.RetailPrice_FC / t.RetailPrice_FC * COALESCE(NULLIF(t.ASP_FC,0),1)
									END as ASP_FC"""
		create_columns += """,u.RetailPrice_FC """
		flex_create_columns += """,u.'Retail Price FY' as RetailPrice_FC """
	else:
		dimensional_columns += """t.RetailPrice_FC
								,t.ASP_FC"""
	if price_flag_TY is True:
		dimensional_columns += """,u.RetailPrice_TY"""								
		create_columns += """,u.RetailPrice_TY """
		flex_create_columns += """,u.'Retail Price Prev52' as RetailPrice_TY """
	else:
		dimensional_columns += """,t.RetailPrice_TY"""
	if price_flag_LY is True:
		dimensional_columns += """,u.RetailPrice_LY"""								
		create_columns += """,u.RetailPrice_LY """
		flex_create_columns += """,u.'Retail Price Prev52-1 YR Ago' as RetailPrice_LY """
	else:
		dimensional_columns += """,t.RetailPrice_LY"""

	if cost_flag_FC is True:
		dimensional_columns += """, u.Cost_FC"""
		create_columns += """,u.Cost_FC"""
		flex_create_columns += """,u.'Cost FY' as Cost_FC """
	else: 
		dimensional_columns += """,t.Cost_FC"""
	if cost_flag_TY is True:
		dimensional_columns += """, u.Cost_TY"""
		create_columns += """,u.Cost_TY"""
		flex_create_columns += """,u.'Cost Prev52' as Cost_TY """
	else: 
		dimensional_columns += """,t.Cost_TY"""
	if cost_flag_LY is True:
		dimensional_columns += """, u.Cost_LY"""
		create_columns += """,u.Cost_LY"""
		flex_create_columns += """,u.'Cost Prev52-1 YR Ago' as Cost_LY """
	else: 
		dimensional_columns += """,t.Cost_LY"""
	
	print("define allocation methods")

	# REK - This is where the rounding happens
	if units_flag is True:
		regular_allocation = """CASE WHEN (u.SalesUnits_FC - u.sum_sales_units) < 0 AND t."""+fc_basis+"""/u.sum_units * ABS(u.SalesUnits_FC - u.sum_sales_units) > t.SalesUnits_FC
										THEN t.SalesUnits_FC
									ELSE COALESCE(t."""+fc_basis+""", 0)/u.sum_units * ABS(u.SalesUnits_FC - u.sum_sales_units)
								END"""		
		cumulative_sum += """, SUM(SalesUnits_FC_Round) OVER (PARTITION BY """ + partition_by + """ ORDER BY SalesUnits_FC_Round DESC, StoreID ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) - SalesUnits_FC_Round AS SalesUnits_FC_Cumulative_Sum"""
		rounding_assignment += """CASE WHEN COALESCE(TargetSalesUnits, 0) = 0 THEN 0
										ELSE CurrentSalesUnits + (CASE WHEN SalesUnits_FC_Round < (ABS(TargetSalesUnits - sum_sales_units) - SalesUnits_FC_Cumulative_Sum)
																		THEN SalesUnits_FC_Round
																		ELSE CASE WHEN (ABS(TargetSalesUnits - sum_sales_units) - SalesUnits_FC_Cumulative_Sum) < 0
																				THEN 0
																	   ELSE (ABS(TargetSalesUnits - sum_sales_units) - SalesUnits_FC_Cumulative_Sum)
																			END
																END) * (CASE WHEN (TargetSalesUnits - sum_sales_units) < 0 THEN -1 ELSE 1 END)
									END as SalesUnits_FC,"""
		if week_flag is False:
			assrt_curve_alloc = """CASE WHEN (u.SalesUnits_FC - u.sum_sales_units) < 0 AND ABS(u.SalesUnits_FC - u.sum_sales_units) * a.assrt_store_wk/a.assrt_upload_level > t.SalesUnits_FC
											THEN t.SalesUnits_FC
										ELSE ABS(u.SalesUnits_FC - u.sum_sales_units) * a.assrt_store_wk/a.assrt_upload_level
									END"""
		else:
			assrt_curve_alloc = """CASE WHEN (u.SalesUnits_FC - u.sum_sales_units) < 0 AND ABS(u.SalesUnits_FC - u.sum_sales_units) * a.assrt_store/a.assrt_upload_level > t.SalesUnits_FC
											THEN t.SalesUnits_FC
										ELSE ABS(u.SalesUnits_FC - sum_sales_units) * a.assrt_store/a.assrt_upload_level
									END"""
		
		peanut_butter_spread = """CASE WHEN (u.SalesUnits_FC - u.sum_sales_units) < 0 AND ABS(u.SalesUnits_FC - u.sum_sales_units)/u.count_units > t.SalesUnits_FC 
									       THEN t.SalesUnits_FC
									   ELSE ABS(u.SalesUnits_FC - u.sum_sales_units)/u.count_units
								  END"""
		# REK - We should just be able to use + fc_basis and only have one statement deifining dimensional_columns, correct?
		dimensional_columns += """,CEIL(CASE WHEN COALESCE(u.sum_units,0) = 0
										THEN 
											CASE WHEN COALESCE(a.assrt_upload_level,0) = 0
												 THEN """+peanut_butter_spread+"""
												 ELSE """+assrt_curve_alloc+"""
											END
										ELSE """+regular_allocation+"""
									END) as SalesUnits_FC_Round, COALESCE(t.SalesUnits_FC, 0) as CurrentSalesUnits, COALESCE(t.Units_FC_Vendor, 0) as CurrentVendorUnits, u.sum_sales_units """
		
		create_columns += """,u.salesunits_fc""" #changed to salesunits_fc
		flex_create_columns += """,u.'SALES UNITS FY' as salesunits_fc """ #changed to salesunits_fc

		if FreezeFlag is True:
			peanut_butter_spread = """CASE WHEN (u.SalesUnits_FC - u.sum_vendor_units) < 0 AND ABS(u.SalesUnits_FC - u.sum_vendor_units)/u.count_units > t.Units_FC_Vendor
												THEN t.Units_FC_Vendor
										  ELSE ABS(u.SalesUnits_FC - u.sum_vendor_units)/u.count_units
									  END"""
			if week_flag is False:
				assrt_curve_alloc = """CASE WHEN (u.SalesUnits_FC - u.sum_vendor_units) < 0 AND ABS(u.SalesUnits_FC - u.sum_vendor_units) * a.assrt_store_wk/a.assrt_upload_level > t.Units_FC_Vendor
												THEN t.Units_FC_Vendor
											ELSE ABS(u.SalesUnits_FC - u.sum_vendor_units) * a.assrt_store_wk/a.assrt_upload_level
										END"""
			else:
				assrt_curve_alloc = """CASE WHEN (u.SalesUnits_FC - u.sum_vendor_units) < 0 AND ABS(u.SalesUnits_FC - u.sum_vendor_units) * a.assrt_store/a.assrt_upload_level > t.Units_FC_Vendor
												THEN t.Units_FC_Vendor
											ELSE ABS(u.SalesUnits_FC - u.sum_vendor_units) * a.assrt_store/a.assrt_upload_level
										END"""
			regular_allocation = """CASE WHEN (u.SalesUnits_FC - u.sum_vendor_units) < 0 AND t."""+fc_basis+"""/u.sum_units * ABS(u.SalesUnits_FC - u.sum_vendor_units) > t.Units_FC_Vendor
											THEN t.Units_FC_Vendor
										ELSE COALESCE(t."""+fc_basis+""", 0)/u.sum_units * ABS(u.SalesUnits_FC - u.sum_vendor_units)
									END"""

			dimensional_columns += """,CEIL(CASE WHEN COALESCE(u.sum_units,0) = 0
											THEN 
												CASE WHEN COALESCE(a.assrt_upload_level,0) = 0
													THEN """+peanut_butter_spread+"""
													ELSE """+assrt_curve_alloc+"""
												END
											ELSE """+regular_allocation+"""
										END) as Units_FC_Vendor_Round, u.sum_vendor_units"""
			cumulative_sum += """, SUM(Units_FC_Vendor_Round) OVER (PARTITION BY """ + partition_by + """ ORDER BY SalesUnits_FC_Round DESC, StoreID ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) - Units_FC_Vendor_Round AS Units_FC_Vendor_Cumulative_Sum"""
			rounding_assignment += """CASE WHEN COALESCE(TargetSalesUnits, 0) = 0 THEN 0
											ELSE CurrentVendorUnits + (CASE WHEN Units_FC_Vendor_Round < (ABS(TargetSalesUnits - sum_vendor_units) - Units_FC_Vendor_Cumulative_Sum)
																			THEN Units_FC_Vendor_Round
																			ELSE CASE WHEN (ABS(TargetSalesUnits - sum_vendor_units) - Units_FC_Vendor_Cumulative_Sum) < 0
																						THEN 0
																					ELSE (ABS(TargetSalesUnits - sum_vendor_units) - Units_FC_Vendor_Cumulative_Sum)
																				END
																		END) * (CASE WHEN (TargetSalesUnits - sum_vendor_units) < 0 THEN -1 ELSE 1 END)
										END as Units_FC_Vendor,"""
		else:
			rounding_assignment += """CurrentVendorUnits as Units_FC_Vendor,"""
		
		dimensional_columns += """,u.SalesUnits_FC as TargetSalesUnits"""
	else:
		dimensional_columns += """,t.SalesUnits_FC 
						,t.Units_FC_Vendor"""
		rounding_assignment += """SalesUnits_FC 
						,Units_FC_Vendor,"""

	if comments_flag is True:
		dimensional_columns += """,CASE WHEN UPPER(LTRIM(RTRIM(u.Vendor_Comments))) = 'MUST ROTATE ON ITEM, MM'
									THEN t.Vendor_Comments
									WHEN UPPER(LTRIM(RTRIM(u.Vendor_Comments))) = 'MUST ROTATE ON ITEM, MM, VENDOR'
									THEN t.Vendor_Comments
									ELSE LTRIM(RTRIM(u.Vendor_Comments::VARCHAR(500)))
									END as Vendor_Comments"""
		create_columns += """,u.vendor_comments::varchar(500)"""
		flex_create_columns += """,u.'vendor comments'::varchar(500) as vendor_comments"""
	else:
		dimensional_columns += """,t.Vendor_Comments"""
	
	# Move upload from flex to temp table for performance gain
	cur.execute(""" DROP TABLE IF EXISTS tmp_forecast_upload;
					CREATE LOCAL TEMP TABLE tmp_forecast_upload on commit preserve rows as 
					select DISTINCT """+flex_columns+flex_create_columns+"""
					, t.assrtid
					, """+str(mm_flag)+""" as MM_Flag
					, """+str(patch_flag)+""" as Patch_Flag
					, """+str(region_flag)+""" as Region_Flag
					from forecast_upload u
					LEFT JOIN (select distinct itemid, assrtid from """+p_env+args.tablename[0]+""" )t
					ON u.Item = t.ItemID;""")
	conn.commit()

	print("key_columns: "+ str(key_columns))
	print("create_columns: "+ str(create_columns))

	print("Checking for dups")
	cur.execute("""SELECT DISTINCT """+key_columns+""" FROM (SELECT """+key_columns+""", row_number() over(partition by """+key_columns+""") as row_num FROM tmp_forecast_upload u)u WHERE row_num > 1 limit 10;""")
	
	dups = cur.fetchall()
	if cur.rowcount > 0:
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast File Load Error"+e_env
		email_begin = """Awesome DemandLink Customer!\n\nWe detected duplicates in the file you just tried to upload on our forecast tool.  Here are some of the duplicates:   """
		email_end = """If the error makes sense to you and you can fix the file yourself please resubmit the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.</br></br>Thanks! </br>DemandLink"""
		htable = create_html_table(cur.description, dups)
		send_html_email(failure_recipients,email_sender,email_subject,email_begin,email_end, htable)
		error = "Duplicates found in file."
		print("Duplicates found in file: Email sent to " + str(failure_recipients))
		Success = False
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)		
		conn.commit()
		sys.exit()		

	# Construct UPDATE query
	print("CONSTRUCT UPDATE query")
	join_table = ''
	if units_flag is False:
		join_table = """tmp_forecast_upload u """+update_predicate
	else:
		print("staging tables for units allocation")
		q_temp = """DROP TABLE IF EXISTS tmp_f_lookup;
		
					CREATE LOCAL TEMP TABLE tmp_f_lookup 
					ON COMMIT PRESERVE ROWS AS
					SELECT distinct """+key_columns+create_columns+""", u.assrtid
					, NULLIF(SUM(t."""+fc_basis+""") , 0) as sum_units
					, NULLIF(COUNT(t.gmsvenid) ,0) as count_units
					, COALESCE(SUM(t.SalesUnits_FC), 0) as sum_sales_units
					, COALESCE(SUM(t.Units_FC_Vendor), 0) as sum_vendor_units
					FROM tmp_forecast_upload u
					INNER JOIN """+p_env+args.tablename[0]+""" t
					"""+update_predicate+"""
					group by """+key_columns+create_columns+""", u.assrtid;
					
					drop table IF EXISTS tmp_f_assrt;
					create local temp table tmp_f_assrt on commit preserve rows as 
					select *
						, sum(assrt_store_wk) over(partition by itemid """+part_upload+""") as assrt_upload_level
					from (
						select distinct gmsvenid, itemid, storeid,fiscalwk """+part_upload+"""
										, sum(units_fc_low) over(partition by assrtid, StoreID) as assrt_store
										, sum(units_fc_low) over(partition by assrtid, StoreID, fiscalwk) as assrt_store_wk
						from """+p_env+"""tbl_allvendors t
						where ("""+curve_where+""") in (SELECT distinct """+curve_where+""" from tmp_f_lookup  where sum_units is null))t
					where gmsvenid = """+str(args.gmsvenid[0])+""";"""

		cur.execute(q_temp)

		join_table = """tmp_f_lookup u 
				"""+update_predicate+"""
				left join tmp_f_assrt a
				on t.itemid = a.itemid
				and t.storeid = a.storeid
				and t.fiscalwk = a.fiscalwk """
		conn.commit()		
	
	cur.execute("SELECT current_timestamp;")
	curr_timestamp = cur.fetchone()
	dollars_vendor = ""
	t_dollars_vendor = ""
	if FreezeFlag is False:
		dollars_vendor = "SalesDollars_FC_Vendor,"
		t_dollars_vendor = "t.SalesDollars_FC_Vendor,"
	
	#truncate staging table before INSERT
	cur.execute("""TRUNCATE TABLE """+p_env+args.stageTablename[0]+""";""")
	conn.commit()

	print("INSERT into vendor's table.")
	q_insert = """INSERT /*+DIRECT*/ INTO """+p_env+args.stageTablename[0]+""" (GMSVenID,
					VendorDesc,
					VBU,
					ItemID,
					ItemDesc,
					ItemConcat,
					StoreID,
					StoreDesc,
					StoreConcat,
					FiscalWk,
					FiscalMo,
					FiscalQtr,
					MD,
					MM,
					Region,
					District,
					Patch,
					ProdGrpID,
					ProdGrpDesc,
					ProdGrpConcat,
					AssrtID,
					AssrtDesc,
					AssrtConcat,
					ParentID,
					ParentDesc,
					ParentConcat,
					PriceSensitivity,
					SalesUnits_TY,
					SalesUnits_LY,
					SalesUnits_2LY,
					SalesDollars_TY,
					SalesDollars_LY,
					SalesDollars_2LY,
					RetailPrice_TY,
					RetailPrice_LY,
					Units_FC_DL,
					Units_FC_LOW,
					OHU_FC,
					OHC_TY,
					OHC_LY,
					ReceiptUnits_TY,
					ReceiptUnits_LY,
					ReceiptDollars_TY,
					ReceiptDollars_LY,
					ShipsGross_TY,
					ShipsGross_LY,
					MM_Comments,
					SalesDollars_FC_DL,
					SalesDollars_FC_LOW,
					"""+dollars_vendor+"""
					Timestamp,
					RetailPrice_FC ,
					ASP_FC,
					Cost_FC,
					SalesUnits_FC,
					Units_FC_Vendor,
					Vendor_Comments,
					Cost_TY,
					Cost_LY

					)
				SELECT GMSVenID,
					VendorDesc,
					VBU,
					ItemID,
					ItemDesc,
					ItemConcat,
					StoreID,
					StoreDesc,
					StoreConcat,
					FiscalWk,
					FiscalMo,
					FiscalQtr,
					MD,
					MM,
					Region,
					District,
					Patch,
					ProdGrpID,
					ProdGrpDesc,
					ProdGrpConcat,
					AssrtID,
					AssrtDesc,
					AssrtConcat,
					ParentID,
					ParentDesc,
					ParentConcat,
					PriceSensitivity,
					SalesUnits_TY,
					SalesUnits_LY,
					SalesUnits_2LY,
					SalesDollars_TY,
					SalesDollars_LY,
					SalesDollars_2LY,
					RetailPrice_TY,
					RetailPrice_LY,
					Units_FC_DL,
					Units_FC_LOW,
					OHU_FC,
					OHC_TY,
					OHC_LY,
					ReceiptUnits_TY,
					ReceiptUnits_LY,
					ReceiptDollars_TY,
					ReceiptDollars_LY,
					ShipsGross_TY,
					ShipsGross_LY,
					MM_Comments,
					SalesDollars_FC_DL,
					SalesDollars_FC_LOW,
					"""+dollars_vendor+"""
					Timestamp,
					RetailPrice_FC,
					ASP_FC,
					Cost_FC,
					""" + rounding_assignment + """
					Vendor_Comments,
					Cost_TY,
					Cost_LY
					FROM (SELECT *""" + cumulative_sum + """
						FROM( SELECT
								t.GMSVenID,
								t.VendorDesc,
								t.VBU,
								t.ItemID,
								t.ItemDesc,
								t.ItemConcat,
								t.StoreID,
								t.StoreDesc,
								t.StoreConcat,
								t.FiscalWk,
								t.FiscalMo,
								t.FiscalQtr,
								t.MD,
								t.MM,
								t.Region,
								t.District,
								t.Patch,
								t.ProdGrpID,
								t.ProdGrpDesc,
								t.ProdGrpConcat,
								t.AssrtID,
								t.AssrtDesc,
								t.AssrtConcat,
								t.ParentID,
								t.ParentDesc,
								t.ParentConcat,
								t.PriceSensitivity,
								t.SalesUnits_TY,
								t.SalesUnits_LY,
								t.SalesUnits_2LY,
								t.SalesDollars_TY,
								t.SalesDollars_LY,
								t.SalesDollars_2LY,
								t.Units_FC_DL,
								t.Units_FC_LOW,
								t.OHU_FC,
								t.OHC_TY,
								t.OHC_LY,
								t.ReceiptUnits_TY,
								t.ReceiptUnits_LY,
								t.ReceiptDollars_TY,
								t.ReceiptDollars_LY,
								t.ShipsGross_TY,
								t.ShipsGross_LY,
								t.MM_Comments,
								SalesDollars_FC_DL,
								SalesDollars_FC_LOW,
								"""+t_dollars_vendor+"""
								'"""+str(curr_timestamp[0])+"""'::TIMESTAMP as TimeStamp,
								"""+dimensional_columns+"""
							FROM  """+p_env+args.tablename[0]+""" t
							INNER JOIN """+join_table+"""
						) AS t
					) AS t;
				"""	

	cur.execute(q_insert)

	#INSERT records not in upload
	#now not updating timestamp
	cur.execute("""INSERT /*+DIRECT*/ INTO """+p_env+args.stageTablename[0]+""" (    GMSVenID,
					VendorDesc,
					VBU,
					ItemID,
					ItemDesc,
					ItemConcat,
					StoreID,
					StoreDesc,
					StoreConcat,
					FiscalWk,
					FiscalMo,
					FiscalQtr,
					MD,
					MM,
					Region,
					District,
					Patch,
					ProdGrpID,
					ProdGrpDesc,
					ProdGrpConcat,
					AssrtID,
					AssrtDesc,
					AssrtConcat,
					ParentID,
					ParentDesc,
					ParentConcat,
					PriceSensitivity,
					SalesUnits_TY,
					SalesUnits_LY,
					SalesUnits_2LY,
					SalesUnits_FC,
					SalesDollars_TY,
					SalesDollars_LY,
					SalesDollars_2LY,
					ASP_FC,
					RetailPrice_TY,
					RetailPrice_LY,
					RetailPrice_FC,
					Cost_FC,
					Units_FC_DL,
					Units_FC_LOW,
					Units_FC_Vendor,
					OHU_FC,
					OHC_TY,
					OHC_LY,
					ReceiptUnits_TY,
					ReceiptUnits_LY,
					ReceiptDollars_TY,
					ReceiptDollars_LY,
					ShipsGross_TY,
					ShipsGross_LY,
					Vendor_Comments,
					MM_Comments,
					TIMESTAMP,			
					"""+dollars_vendor+"""
					SalesDollars_FC_DL,
					SalesDollars_FC_LOW,
					Cost_TY,
					Cost_LY
					)
				SELECT
					t.GMSVenID,
					t.VendorDesc,
					t.VBU,
					t.ItemID,
					t.ItemDesc,
					t.ItemConcat,
					t.StoreID,
					t.StoreDesc,
					t.StoreConcat,
					t.FiscalWk,
					t.FiscalMo,
					t.FiscalQtr,
					t.MD,
					t.MM,
					t.Region,
					t.District,
					t.Patch,
					t.ProdGrpID,
					t.ProdGrpDesc,
					t.ProdGrpConcat,
					t.AssrtID,
					t.AssrtDesc,
					t.AssrtConcat,
					t.ParentID,
					t.ParentDesc,
					t.ParentConcat,
					t.PriceSensitivity,
					t.SalesUnits_TY,
					t.SalesUnits_LY,
					t.SalesUnits_2LY,
					t.SalesUnits_FC,
					t.SalesDollars_TY,
					t.SalesDollars_LY,
					t.SalesDollars_2LY,
					t.ASP_FC,
					t.RetailPrice_TY,
					t.RetailPrice_LY,
					t.RetailPrice_FC,
					t.Cost_FC,
					t.Units_FC_DL,
					t.Units_FC_LOW,
					t.Units_FC_Vendor,
					t.OHU_FC,
					t.OHC_TY,
					t.OHC_LY,
					t.ReceiptUnits_TY,
					t.ReceiptUnits_LY,
					t.ReceiptDollars_TY,
					t.ReceiptDollars_LY,
					t.ShipsGross_TY,
					t.ShipsGross_LY,
					t.Vendor_Comments,
					t.MM_Comments,
				    t.Timestamp,
				   	"""+t_dollars_vendor+"""
					SalesDollars_FC_DL,
					SalesDollars_FC_LOW,
					t.Cost_TY,
					t.Cost_LY
				FROM  """+p_env+args.tablename[0]+""" t
				LEFT JOIN tmp_forecast_upload u
				"""+update_predicate+"""
				WHERE u.GMSVenID IS NULL; """)
	
	##start duplicate check in staging table##
	print("final duplicate check in staging table")
	cur.execute("""
					select cast(dupecount as int)
					from (select count(storeid) over(partition by GMSVenID, storeid, itemid, fiscalwk) as dupecount, *
					from """+p_env+args.stageTablename[0]+""") t1
					where dupecount > 1 limit 10;
	""")
	#make sure to dupecount to an int
	#rowcount = cur.rowcount
	if cur.rowcount > 0:
		print("Error duplicates found in "+p_env+args.stageTablename[0])
		#move file
		os.rename(yoda_path, failure_path)
		#customer email
		email_subject = "Forecast File Load Error"+e_env	
		email_msg = """Awesome DemandLink Customer!\n\nThere was an issue with the file you just tried to upload. Support has been notified and is investigating the issue.\nThanks!\nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)	
		
		#support email
		email_subject = "Forecast File Load Error Duplicates Found"+e_env	
		email_msg = """Failed Forecast upload. Duplicates across GMSVenID, StoreID, ItemID and FiscalWk were found for file """+str(failure_path)+"""\nDuplicates can be found in """+p_env+args.stageTablename[0]+"""\nPlease investigate and get back to user """+str(args.username[0])+"""."""
		send_email(internal_recipients,email_sender,email_subject,email_msg)	
		
		error = "Duplicates found in file."
		print("Duplicates found in file: Email sent to " + str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, error, duration)
		
		conn.commit()
		sys.exit()		
		
	else:
		print("No duplicates found in staging table")
		print("Swapping partitions and cascading edits")

		#need min and max time for partitions
		#current table min and max
		table_minTime = get_min_or_max_partitions_vendors(conn, p_env, args.tablename[0], 0)
		table_maxTime = get_min_or_max_partitions_vendors(conn, p_env, args.tablename[0], 1)	
		#stage table min and max
		stage_table_minTime = get_min_or_max_partitions_vendors(conn, p_env, args.stageTablename[0], 0)
		stage_table_maxTime = get_min_or_max_partitions_vendors(conn, p_env, args.stageTablename[0], 1)

		minTime = min(table_minTime, stage_table_minTime)
		maxTime = max(table_maxTime, stage_table_maxTime)
		
		print("----timestamps------")
		print(str(minTime))
		print(str(maxTime))
		print("---------")
		#swap partition between stage table forecast table
		cur.execute("""SELECT SWAP_PARTITIONS_BETWEEN_TABLES(
							'"""+p_env+args.stageTablename[0]+"""',
							'"""+str(minTime)+"""',
							'"""+str(maxTime)+"""',	
							'"""+p_env+args.tablename[0]+"""'
							);"""
						)
		conn.commit()
		#for testing only so vendors and mm don't have to be updated on if table is empty
		#CASCADE edits for allvendors and MMs
		print("DROP old data and INSERT into allVendors table.")
		cur.execute("""SELECT DROP_PARTITIONS('"""+p_env+"""tbl_AllVendors', """+str(args.gmsvenid[0])+""", """+str(args.gmsvenid[0])+""");
					INSERT /*+DIRECT*/ INTO """+p_env+"""tbl_AllVendors 
					SELECT * FROM """+p_env+args.tablename[0]+""";""")
		conn.commit()
		#CASCADE edits for MMs
		print("DROP old data and INSERT into MM tables.")
		cur.execute("""Select DISTINCT MM, tableName FROM """+p_env+"""config_MM WHERE MMFlag is true and viewname is null;""")
		vars = cur.fetchall()
		for var in vars:
			v_MM = var[0].encode("utf-8")
			mm_table = var[1].encode("utf-8")
			print("MM: "+str(mm_table))
			cur.execute("""SELECT DROP_PARTITIONS('"""+p_env+mm_table+"""', """+str(args.gmsvenid[0])+""", """+str(args.gmsvenid[0])+""");
					INSERT /*+DIRECT*/ INTO """+p_env+mm_table+"""
					SELECT * FROM """+p_env+args.tablename[0]+""" WHERE MM= '"""+v_MM+"""';""")
			print(str(mm_table))
		conn.commit()

		#truncate staging table
		cur.execute("""TRUNCATE TABLE """+p_env+args.stageTablename[0])
		conn.commit()

	endtime = time.time()

	#move file and send email
	os.rename(yoda_path, success_path)
	email_subject = "Forecast Upload File Success"+e_env
	email_msg = """Your forecast upload file has successfully completed: """+str(args.filename[0])+"""\n\nPlease refresh the page to see the newest results!""" #+ """\n\nUpload took """ + str(endtime-starttime) + """s"""
	send_email(success_recipients,email_sender,email_subject,email_msg)
	print("Upload Forecast File Completed for: " + str(args.gmsvenid[0]))
	print(p_env+args.tablename[0])
	Success = True
	duration = str(datetime.now()-start_time)
	print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, email_msg, duration)
	conn.commit()		
except Exception as e:
	if e.__class__.__name__ == 'IntegrityError':
		#move file
		print(str(e))
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast Upload Load Error"+e_env
		email_msg = """Awesome DemandLink Customer!\n\nWe detected a problem with the file you just tried to upload: """+str(args.filename[0])+"""\n\nHere is the error:\n\n"""+str(e)+"""\n\nIf this error makes sense to you and you can fix the file yourself please resubmit the file! But if you have no idea what this error means, then feel free to contact us at support@demandlink.com. We'd be happy to help resolve your issue.\n\nThanks! \nDemandLink"""
		send_email(failure_recipients,email_sender,email_subject,email_msg)	
		print("FAILURE: Email sent to " +str(failure_recipients))
		Success = False
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, email_msg, duration)
		conn.commit()
	else:
		#move file
		print(str(e))
		os.rename(yoda_path, failure_path)
		email_subject = "Forecast Upload Failure for:"+e_env+" " + str(args.gmsvenid[0]) 
		email_msg = "Something went wrong with the upload - see the logs for more info.\n" + str(e)
		send_email(internal_recipients,email_sender,email_subject,email_msg)
		print("Email sent to " +str(internal_recipients))
		Success = False
		errorMessage = str(e).replace("'", "")
		errorMessage = errorMessage.replace("(", "")
		errorMessage = errorMessage.replace(")", "")
		errorMessage = errorMessage.replace('"', "")
		errorMessage = errorMessage.replace(",", "")
		print(errorMessage)
		duration = str(datetime.now()-start_time)
		print_to_forecast_log(cur, p_env, args.gmsvenid[0], VendorDesc, fileType, args.filename[0], Success, login, errorMessage, duration)
		conn.commit()
finally:	
	cur.close()
	del cur
	conn.close()
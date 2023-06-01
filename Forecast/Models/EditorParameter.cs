using Forecast.Data;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Security;

namespace Forecast.Models
{
	public class EditModelBinder : DefaultModelBinder
	{
		/// <summary>
		/// Convert an HTTP request submitted by the client-side into a
		/// DtRequest object
		/// </summary>
		public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			var editor = new EditorParameterModel();
			try
			{
				base.BindModel(controllerContext, bindingContext);
				var request = controllerContext.HttpContext.Request;

				var action = Convert.ToString(request["action"]);
				var columns = new List<DTColumn>();
				string gmsvenid = Convert.ToString(DataProvider.Unprotect(request.Cookies["DL_Forecast_0"]["GMSVenID"], "id"));
				var rotator = new List<DTRotator>();
				var retailPrice = new List<ERetailPrice>();
				var salesU = new List<ESalesU>();
				var salesVar = new List<ESalesUVar>();
				var tableName = Convert.ToString(DataProvider.Unprotect(request.Cookies["DL_Forecast_0"]["TableName"], "id"));
				var username = Membership.GetUser().UserName;
				var vendorGroup = Convert.ToString(DataProvider.Unprotect(request.Cookies["DL_Forecast_0"]["TableName"], "id"));
                var isMM = Convert.ToBoolean(request["isMM"]);
				var isMD = Convert.ToBoolean(request["isMD"]);
                var mmComments = new List<EMMComments>();
                var vendorComments = new List<EVendorComments>();
				var editMode = request["editMode"];
				
				foreach (string key in request.Params)
				{
					if (key.Contains("data"))
					{
						string sID = key.ToString();
						sID = sID.Substring(sID.IndexOf('[') + 1, sID.IndexOf(']') - sID.IndexOf('[') - 1);

						if (key.Contains("Units_FC_LOW_Var"))
						{

							salesVar.Add(new ESalesUVar
							{
								ID = sID,
								SalesUVar = request["data[" + sID + "][Units_FC_LOW_Var]"]
							});
						}
						else if (key.Contains("RetailPrice_FC"))
						{

							retailPrice.Add(new ERetailPrice
							{
								ID = sID,
								RetailPrice = request["data[" + sID + "][RetailPrice_FC]"]
							});
						}
						else if (key.Contains("SalesUnits_FC"))
						{

							salesU.Add(new ESalesU
							{
								ID = sID,
								SalesU = request["data[" + sID + "][SalesUnits_FC]"]
							});
						}
                        else if (key.Contains("MM_Comments"))
                        {
                            mmComments.Add(new EMMComments
                            {
                                ID = sID,
                                MMComments = request["data[" + sID + "][MM_Comments]"]
                            });
                        }
                        else if (key.Contains("Vendor_Comments"))
                        {
                            vendorComments.Add(new EVendorComments
                            {
                                ID = sID,
                                VendorComments = request["data[" + sID + "][Vendor_Comments]"]
                            });
                        }
                    }
					else if (key.Contains("columns") && columns.Count == 0)
					{
						// Columns
						var c = 0;
						while (request["columns[" + c + "][name]"] != null)
						{
							columns.Add(new DTColumn
							{
								Data = request["columns[" + c + "][data]"],
								Name = request["columns[" + c + "][name]"],
								Orderable = Convert.ToBoolean(request["columns[" + c + "][orderable]"]),
								Searchable = Convert.ToBoolean(request["columns[" + c + "][searchable]"]),
								Visible = Convert.ToBoolean(request["columns[" + c + "][visible]"]),
								Search = new DTSearch
								{
									Value = request["columns[" + c + "][search][value]"],
									Regex = Convert.ToBoolean(request["columns[" + c + "][search][regex]"])
								}
							});
							c++;
						}
					}
					else if (key.Contains("rotator") && rotator.Count == 0)
					{
						var r = 0;
						while (request["rotator[" + r + "][column]"] != null)
						{
							rotator.Add(new DTRotator
							{
								Column = request["rotator[" + r + "][column]"],
								Included = System.Convert.ToBoolean(request["rotator[" + r + "][included]"])
							});
							r++;
						}
					}


					editor = new EditorParameterModel
					{
						Action = action,
						Columns = columns,
						GMSVenID = gmsvenid,
						RetailPrice = retailPrice,
						Rotator = rotator,
						SalesU = salesU,
						SalesUVar = salesVar,
						TableName = tableName,
						Username = username,
						IsMM = isMM,
						IsMD = isMD,
						MMComments = mmComments,
						VendorComments = vendorComments,
						EditMode = editMode,
						VendorGroup = vendorGroup
					};
				}
			}
			catch (Exception e)
			{
				//if(editor.)
				//    e.Data.Add("comment", editor);
				throw e;
			}
			return editor;
		}
	}


	[ModelBinder(typeof(EditModelBinder))]
	public class EditorParameterModel
	{

		/// <summary>
		/// This is used to determine what type of request is being sent
		/// (i.e. "edit", "create", "remove"
		/// </summary>
		public string Action { get; set; }

		/// <summary>
		/// Collection of all columns in the table
		/// </summary>
		public IEnumerable<DTColumn> Columns { get; set; }

		/// <summary>
		/// The vendor ID
		/// </summary>
		public string GMSVenID { get; set; }

		///<summary>
		///True if the user is an MM
		///</summary>
		public bool IsMM { get; set; }

		///<summary>
		///True if the user is an MM
		///</summary>
		public bool IsMD { get; set; }

		/// <summary>
		/// The retail price is editable at all levels. This is
		/// eventually converted to a decimal (money).
		/// </summary>
		public IEnumerable<ERetailPrice> RetailPrice { get; set; }

		/// <summary>
		/// This is the grouping term for the current state of the rotator
		/// </summary>
		public IEnumerable<DTRotator> Rotator { get; set; }
		
		/// <summary>
		/// The SalesU column is editable at all levels. This
		/// is converted to a integer.
		/// </summary>
		public IEnumerable<ESalesU> SalesU { get; set; }
		
		/// <summary>
		/// This is the sales units variance. Only editable by
		/// Lowe's Marketing Directors. This is converted to a percentage.
		/// </summary>
		public IEnumerable<ESalesUVar> SalesUVar { get; set; }

        /// <summary>
        /// This is the mm comment section.  Can only be edited my Lowes MM's or
        /// greater.
        /// </summary>
        public IEnumerable<EMMComments> MMComments { get; set; }

        /// <summary>
        /// This is the vendor comment section.  Can be edited by vendor or
        /// greater.
        /// </summary>
        public IEnumerable<EVendorComments> VendorComments { get; set; }

		///<summary>
		/// Describes the type of edit (i.e. "bulk" - aka 'main' OR "inline")
		/// </summary>
		public string EditMode { get; set; }

        /// <summary>
        /// The vendor's table name in Vertica DB
        /// </summary>
        public string TableName { get; set; }

		///<summary>
		///Get the username of the user
		///</summary>
		public string Username { get; set; }

		/// <summary>
		/// Returns the vendor group from the access tables. 
		/// </summary>
		public string VendorGroup { get; set; }

		/// <summary>
		/// Converts an editor object to a DTParam object.
		/// </summary>
		/// <param name="editor"></param>
		/// <returns></returns>
		public DTParameterModel EditorToDTParam(EditorParameterModel editor)
		{
			DTParameterModel dtparam = new DTParameterModel();

			foreach (DTColumn c in editor.Columns)
			{
				dtparam.Columns = editor.Columns;
			}
			foreach (DTRotator r in editor.Rotator)
			{
				dtparam.Rotator = editor.Rotator;
			}
			dtparam.VendorDesc = editor.TableName;
			dtparam.TableName = editor.TableName;
			dtparam.Username = editor.Username;
			dtparam.GMSVenID = Convert.ToInt16(editor.GMSVenID);
			return dtparam;
		}
	}

	/// <summary>
	/// Retail price and ID
	/// </summary>
	public sealed class ERetailPrice
	{
		public string ID { get; set; }
		public string RetailPrice { get; set; }
	}

	/// <summary>
	/// Sales Units and ID
	/// </summary>
	public sealed class ESalesU
	{
		public string ID { get; set; }
		public string SalesU { get; set; }
	}

	/// <summary>
	/// Sales units variance and ID
	/// </summary>
	public sealed class ESalesUVar
	{
		public string ID { get; set; }
		public string SalesUVar { get; set; }
	}

    /// <summary>
    /// MM Comment and ID
    /// </summary>
    public sealed class EMMComments
    {
        public string ID { get; set; }
        public string MMComments { get; set; }
    }

    /// <summary>
    /// Vendor Comment and ID
    /// </summary>
    public sealed class EVendorComments
    {
        public string ID { get; set; }
        public string VendorComments { get; set; }
    }

}
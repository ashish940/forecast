using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Vertica.Data.VerticaClient;
using Forecast.Models;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Dynamic;
using System.Text;
using static Forecast.Models.Util;
using System.Collections.Concurrent;

namespace Forecast.Data
{
    public class DataProvider
    {
        private string _verticaWebConn = ConfigurationManager.ConnectionStrings["VerticaConnectionString"].ConnectionString;
        private string _qvwebconn = ConfigurationManager.ConnectionStrings["QVWebConnectionString"].ConnectionString;

        #region SELECT

        /// <summary>
        /// Gets a list of ids and names for the admin temp list that could be events, tutorials, etc...
        /// </summary>
        /// <param name="id">The id for the config_notifications table for which table you want to pull ids and names from.</param>
        /// <returns></returns>
        public List<AdminTempList> GetAdminTempList(int id)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<AdminTempList>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetAdminTempListTable(id);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                var tableName = ds.Tables[0].Rows[0]["TableName"];

                adapter.Dispose();
                adapter = new VerticaDataAdapter();
                connection.Close();
                connection = new VerticaConnection(_verticaWebConn);
                command.Dispose();
                command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetAdminTempList(tableName);
                ds.Dispose();
                ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);


                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var notification = new AdminTempList
                    {
                        NotifType = id,
                        NotifTypeId = ds.Tables[0].Rows[i]["NotifTypeId"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["NotifTypeId"]),
                        Title = ds.Tables[0].Rows[i]["Title"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Title"])
                    };


                    result.Add(notification);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Sends a request to DataCommands.cs for the statement then queries the database for the filters that populate the drop down options.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<FilterParameter> GetFilterData(DTParameterModel param, string type, string search)
        {
            var result = new List<FilterParameter>();
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetFilterData(param, type, search);
                VerticaDataReader dr = command.ExecuteReader();
                int i = 1;
                while (dr.Read())
                {
                    result.Add(new FilterParameter { id = i, text = Convert.ToString(dr["Filter"]) });
                    i++;
                }
                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {

            }
            return result;
        }

        /// <summary>
        /// Checks to see if a bookmark already exists
        /// </summary>
        /// <param name="username"></param>
        /// <param name="gmsvenid"></param>
        /// <param name="bookmarkName"></param>
        /// <returns></returns>
        public bool GetIsBookmark(string username, int gmsvenid, string bookmarkName)
        {
            try
            {
                var bookmarkManager = new BookmarksManager(gmsvenid, username);
                return bookmarkManager.GetIsBookmark(bookmarkName);
            }
            catch (Exception e)
            {
                //Debug.WriteLine($"Could not read BookmarkName from VerticaDataReader. Error: {e.Message}");
                throw e;
            }
        }

        /// <summary>
        /// Get a value from the config_tool table.
        /// </summary>
        /// <returns></returns>
        public bool GetToolConfigValue(string flagName)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetToolConfigValue(flagName);
                VerticaDataReader dr = command.ExecuteReader();
                
                if (dr.Read())
                {
                    return dr["FlagValue"] is DBNull ? false : Convert.ToBoolean(dr["FlagValue"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }

            return false;
        }

        /// <summary>
        /// Get Item description for user input ItemID
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string GetItemDesc(string[] s, string t)
        {

            string result = "";
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetItemDesc(s, t);
                VerticaDataReader dr = command.ExecuteReader();
                int i = 0;
                while (dr.Read())
                {
                    result = result + Convert.ToString(dr["Filter"]) + ",";
                    i++;

                }
                dr.Close();
                connection.Close();
                result = result.Remove(result.Length - 1);

            }
            catch (Exception)
            {

            }
            return result;
        }

        /// <summary>
        /// Sends a request to DataCommands.cs for the statement then queries the database for the default Forecast table.
        /// Applies search, ordering, table length and offset from the DTParameterModel.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public IEnumerable<ForecastDetails> GetForecastTable(DTParameterModel param)
        {
            var isRotatorSelected = param.Rotator.Any(rc => rc.Included);
            if (!isRotatorSelected)
            {
                return new List<ForecastDetails>();
            }

            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<ForecastDetails>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetForecastTable(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    ForecastDetails details = new ForecastDetails();
                    //details.GMSVenID = ds.Tables[0].Rows[i]["GMSVenID"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["GMSVenID"]);
                    details.ForecastID = ds.Tables[0].Rows[i]["ForecastID"] is DBNull ? "-1" : Convert.ToString(ds.Tables[0].Rows[i]["ForecastID"]);
                    details.VendorDesc = ds.Tables[0].Rows[i]["VendorDesc"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["VendorDesc"]);
                    details.ItemID = ds.Tables[0].Rows[i]["ItemID"] is DBNull ? 0 : Convert.ToInt64(ds.Tables[0].Rows[i]["ItemID"]);
                    details.ItemDesc = ds.Tables[0].Rows[i]["ItemDesc"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ItemDesc"]);
                    details.ItemConcat = ds.Tables[0].Rows[i]["ItemConcat"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ItemConcat"]);
                    details.MD = ds.Tables[0].Rows[i]["MD"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MD"]);
                    details.FiscalWk = ds.Tables[0].Rows[i]["FiscalWk"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["FiscalWk"]);
                    details.FiscalMo = ds.Tables[0].Rows[i]["FiscalMo"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["FiscalMo"]);
                    details.FiscalQtr = ds.Tables[0].Rows[i]["FiscalQtr"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["FiscalQtr"]);
                    details.MD = ds.Tables[0].Rows[i]["MD"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MD"]);
                    details.MM = ds.Tables[0].Rows[i]["MM"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MM"]);
                    details.Region = ds.Tables[0].Rows[i]["Region"] is DBNull ? "None" : Convert.ToString(ds.Tables[0].Rows[i]["Region"]);
                    details.District = ds.Tables[0].Rows[i]["District"] is DBNull ? "None" : Convert.ToString(ds.Tables[0].Rows[i]["District"]);
                    details.Patch = ds.Tables[0].Rows[i]["Patch"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Patch"]);
                    details.ParentID = ds.Tables[0].Rows[i]["ParentID"] is DBNull ? "Parent Needed" : Convert.ToString(ds.Tables[0].Rows[i]["ParentID"]);
                    details.ParentDesc = ds.Tables[0].Rows[i]["ParentDesc"] is DBNull ? "Parent Needed" : Convert.ToString(ds.Tables[0].Rows[i]["ParentDesc"]);
                    details.ParentConcat = ds.Tables[0].Rows[i]["ParentConcat"] is DBNull ? "None" : Convert.ToString(ds.Tables[0].Rows[i]["ParentConcat"]);
                    details.ProdGrpID = ds.Tables[0].Rows[i]["ProdGrpID"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["ProdGrpID"]);
                    details.ProdGrpDesc = ds.Tables[0].Rows[i]["ProdGrpDesc"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ProdGrpDesc"]);
                    details.ProdGrpConcat = ds.Tables[0].Rows[i]["ProdGrpConcat"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ProdGrpConcat"]);
                    details.AssrtID = ds.Tables[0].Rows[i]["AssrtID"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["AssrtID"]);
                    details.AssrtDesc = ds.Tables[0].Rows[i]["AssrtDesc"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["AssrtDesc"]);
                    details.AssrtConcat = ds.Tables[0].Rows[i]["AssrtConcat"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["AssrtConcat"]);
                    details.SalesUnits_TY = ds.Tables[0].Rows[i]["SalesUnits_TY"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["SalesUnits_TY"]);
                    details.SalesUnits_LY = ds.Tables[0].Rows[i]["SalesUnits_LY"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["SalesUnits_LY"]);
                    details.SalesUnits_2LY = ds.Tables[0].Rows[i]["SalesUnits_2LY"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["SalesUnits_2LY"]);
                    details.SalesUnits_FC = ds.Tables[0].Rows[i]["SalesUnits_FC"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["SalesUnits_FC"]);
                    details.SalesUnits_Var = ds.Tables[0].Rows[i]["SalesUnits_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesUnits_Var"]);
                    details.SalesDollars_TY = ds.Tables[0].Rows[i]["SalesDollars_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_TY"]);
                    details.SalesDollars_LY = ds.Tables[0].Rows[i]["SalesDollars_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_LY"]);
                    details.SalesDollars_2LY = ds.Tables[0].Rows[i]["SalesDollars_2LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_2LY"]);
                    details.SalesDollars_FR_FC = ds.Tables[0].Rows[i]["SalesDollars_FR_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_FR_FC"]);
                    details.SalesDollars_Curr = ds.Tables[0].Rows[i]["SalesDollars_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_Curr"]);
                    details.SalesDollars_Var = ds.Tables[0].Rows[i]["SalesDollars_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_Var"]);
                    details.CAGR = ds.Tables[0].Rows[i]["CAGR"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["CAGR"]);
                    details.Asp_TY = ds.Tables[0].Rows[i]["Asp_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Asp_TY"]);
                    details.Asp_LY = ds.Tables[0].Rows[i]["Asp_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Asp_LY"]);
                    details.Asp_FC = ds.Tables[0].Rows[i]["Asp_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Asp_FC"]);
                    details.Asp_Var = ds.Tables[0].Rows[i]["Asp_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Asp_Var"]);
                    details.RetailPrice_TY = ds.Tables[0].Rows[i]["RetailPrice_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_TY"]);
                    details.RetailPrice_LY = ds.Tables[0].Rows[i]["RetailPrice_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_LY"]);
                    details.RetailPrice_FC = ds.Tables[0].Rows[i]["RetailPrice_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_FC"]);
                    details.RetailPrice_Erosion_Rate = ds.Tables[0].Rows[i]["RetailPrice_Erosion_Rate"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_Erosion_Rate"]);
                    details.RetailPrice_Var = ds.Tables[0].Rows[i]["RetailPrice_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_Var"]);
                    details.SalesDollars_FR_TY = ds.Tables[0].Rows[i]["SalesDollars_FR_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_FR_TY"]);
                    details.SalesDollars_FR_LY = ds.Tables[0].Rows[i]["SalesDollars_FR_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_FR_LY"]);
                    details.MarginDollars_FR_TY = ds.Tables[0].Rows[i]["MarginDollars_FR_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["MarginDollars_FR_TY"]);
                    details.MarginDollars_FR_LY = ds.Tables[0].Rows[i]["MarginDollars_FR_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["MarginDollars_FR_LY"]);
                    details.MarginDollars_FR_Var = ds.Tables[0].Rows[i]["MarginDollars_FR_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["MarginDollars_FR_Var"]);
                    details.Cost_TY = ds.Tables[0].Rows[i]["Cost_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Cost_TY"]);
                    details.Cost_LY = ds.Tables[0].Rows[i]["Cost_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Cost_LY"]);
                    details.Cost_FC = ds.Tables[0].Rows[i]["Cost_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Cost_FC"]);
                    details.Cost_Var = ds.Tables[0].Rows[i]["Cost_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Cost_Var"]);
                    details.Margin_Dollars_TY = ds.Tables[0].Rows[i]["Margin_Dollars_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_TY"]);
                    details.Margin_Dollars_LY = ds.Tables[0].Rows[i]["Margin_Dollars_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_LY"]);
                    details.Margin_Dollars_Curr = ds.Tables[0].Rows[i]["Margin_Dollars_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_Curr"]);
                    details.Margin_Dollars_FR = ds.Tables[0].Rows[i]["Margin_Dollars_FR"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_FR"]);
                    details.Margin_Dollars_Var_Curr = ds.Tables[0].Rows[i]["Margin_Dollars_Var_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_Var_Curr"]);
                    details.Margin_Percent_TY = ds.Tables[0].Rows[i]["Margin_Percent_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_TY"]);
                    details.Margin_Percent_LY = ds.Tables[0].Rows[i]["Margin_Percent_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_LY"]);
                    details.Margin_Percent_Curr = ds.Tables[0].Rows[i]["Margin_Percent_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_Curr"]);
                    details.Margin_Percent_FR = ds.Tables[0].Rows[i]["Margin_Percent_FR"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_FR"]);
                    details.Margin_Percent_Var = ds.Tables[0].Rows[i]["Margin_Percent_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_Var"]);
                    details.Turns_TY = ds.Tables[0].Rows[i]["Turns_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Turns_TY"]);
                    details.Turns_LY = ds.Tables[0].Rows[i]["Turns_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Turns_LY"]);
                    details.Turns_FC = ds.Tables[0].Rows[i]["Turns_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Turns_FC"]);
                    details.Turns_Var = ds.Tables[0].Rows[i]["Turns_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Turns_Var"]);
                    details.SellThru_TY = ds.Tables[0].Rows[i]["SellThru_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SellThru_TY"]);
                    details.SellThru_LY = ds.Tables[0].Rows[i]["SellThru_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SellThru_LY"]);
                    details.Dollars_FC_DL = ds.Tables[0].Rows[i]["Dollars_FC_DL"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_DL"]);
                    details.Dollars_FC_LOW = ds.Tables[0].Rows[i]["Dollars_FC_LOW"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_LOW"]);
                    details.Dollars_FC_Vendor = ds.Tables[0].Rows[i]["Dollars_FC_Vendor"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_Vendor"]);
                    details.Units_FC_DL = ds.Tables[0].Rows[i]["Units_FC_DL"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["Units_FC_DL"]);
                    details.Units_FC_LOW = ds.Tables[0].Rows[i]["Units_FC_LOW"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["Units_FC_LOW"]);
                    details.Units_FC_Vendor = ds.Tables[0].Rows[i]["Units_FC_Vendor"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["Units_FC_Vendor"]);
                    details.Dollars_FC_DL_Var = ds.Tables[0].Rows[i]["Dollars_FC_DL_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_DL_Var"]);
                    details.Dollars_FC_LOW_Var = ds.Tables[0].Rows[i]["Dollars_FC_LOW_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_LOW_Var"]);
                    details.Dollars_FC_Vendor_Var = ds.Tables[0].Rows[i]["Dollars_FC_Vendor_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_Vendor_Var"]);
                    details.Units_FC_DL_Var = ds.Tables[0].Rows[i]["Units_FC_DL_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Units_FC_DL_Var"]);
                    details.Units_FC_LOW_Var = ds.Tables[0].Rows[i]["Units_FC_LOW_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Units_FC_LOW_Var"]);
                    details.Units_FC_Vendor_Var = ds.Tables[0].Rows[i]["Units_FC_Vendor_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Units_FC_Vendor_Var"]);
                    details.ReceiptUnits_TY = ds.Tables[0].Rows[i]["ReceiptUnits_TY"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["ReceiptUnits_TY"]);
                    details.ReceiptUnits_LY = ds.Tables[0].Rows[i]["ReceiptUnits_LY"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["ReceiptUnits_LY"]);
                    details.ReceiptDollars_TY = ds.Tables[0].Rows[i]["ReceiptDollars_TY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["ReceiptDollars_TY"]);
                    details.ReceiptDollars_LY = ds.Tables[0].Rows[i]["ReceiptDollars_LY"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["ReceiptDollars_LY"]);
                    details.PriceSensitivityImpact = ds.Tables[0].Rows[i]["PriceSensitivityImpact"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["PriceSensitivityImpact"]);
                    details.PriceSensitivityPercent = ds.Tables[0].Rows[i]["PriceSensitivityPercent"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["PriceSensitivityPercent"]);
                    // details.VBUPercent = ds.Tables[0].Rows[i]["VBUPercent"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["VBUPercent"]);
                    details.MM_Comments = ds.Tables[0].Rows[i]["MM_Comments"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MM_Comments"]);
                    details.Vendor_Comments = ds.Tables[0].Rows[i]["Vendor_Comments"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Vendor_Comments"]);

                    result.Add(details);
                }

                connection.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error in GetForecastTable: {e.Message}");
            }

            return result;
        }

        public IEnumerable<UpdatedDates> GetUpdatedDates(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<UpdatedDates>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetUpdatedDates(param);
                adapter.SelectCommand = command;
              
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    UpdatedDates totals = new UpdatedDates();
                    totals.DateMin = ds.Tables[0].Rows[0][$"Date_min"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"Date_min"]);
                    totals.DateMax = ds.Tables[0].Rows[0][$"Date_max"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"Date_max"]);
                    result.Add(totals);
               }
               connection.Close();
            }
            catch (Exception)
            {
               
            }
            return result;
            
        }

        /// <summary>
        /// Checks to see if sales units can be edited or not by checking the sum
        /// of units_fc_vendor column.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool GetIsAllocationEditable(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            bool result = false;

            try
            {
                // Check if forecast data is frozen.
                var isFrozen = GetToolConfigValue("freeze");

                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetIsAllocationEditable(param, isFrozen);
                VerticaDataReader dr = command.ExecuteReader();

                if (dr.Read())
                {
                    result = Convert.ToBoolean(dr["Allocatable"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Retrieves an <seealso cref="ImportProcess"/> object from the database if it exists based on 
        /// an GMSVenID. If no record exists for the vendor then an <seealso cref="ImportProcess"/> object will
        /// be returned but its GMSVenID and Process id will be set to -1 and other fields will be set to be default.
        /// </summary>
        /// <param name="gmsvenid"> The GMSVenID of the vendor.</param>
        /// <returns>An <seealso cref="ImportProcess"/> object that is populated if the vendor exists in the table. If 
        /// the vendor doesn't exist in the table then it will return an <seealso cref="ImportProcess"/> object with 
        /// GMSVenID and ProcessId set to -1.</returns>
        public ImportProcess GetImportProcess(int gmsvenid)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var importProcess = new ImportProcess() { GMSVenID = -1, ProcessId = -1 };

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetImportProcess(gmsvenid);
                VerticaDataReader dr = command.ExecuteReader();

                //We only need to know about the first one because we search on the three keys of the 
                //bookmark table so it will only return one row for this specific request
                if (dr.Read())
                {
                    importProcess.GMSVenID = Convert.ToInt32(dr["GMSVenID"]);
                    importProcess.ProcessId = Convert.ToInt32(dr["ProcessId"]);
                    importProcess.FileName = Convert.ToString(dr["FileName"]);
                    importProcess.StartTime = Convert.ToDateTime(dr["StartTime"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                return importProcess;
            }

            return importProcess;
        }

        /// <summary>
        /// Sends a request to DataCommands.cs for the statement then queries the database for the sums of all columns.
        /// Populates the summary row of the data table. Should be reflective of applied filters.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public IEnumerable<ForecastSums> GetSums(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<ForecastSums>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetSums(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    ForecastSums sums = new ForecastSums();
                    sums.GMSVenID = ds.Tables[0].Rows[i]["GMSVenID"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["GMSVenID"]);
                    sums.VendorDesc = ds.Tables[0].Rows[i]["VendorDesc"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["VendorDesc"]);
                    sums.ItemID = ds.Tables[0].Rows[i]["ItemID"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ItemID"]);
                    sums.FiscalWk = ds.Tables[0].Rows[i]["FiscalWk"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["FiscalWk"]);
                    sums.FiscalMo = ds.Tables[0].Rows[i]["FiscalMo"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["FiscalMo"]);
                    sums.FiscalQtr = ds.Tables[0].Rows[i]["FiscalQtr"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["FiscalQtr"]);
                    sums.MD = ds.Tables[0].Rows[i]["MD"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MD"]);
                    sums.MM = ds.Tables[0].Rows[i]["MM"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MM"]);
                    sums.Region = ds.Tables[0].Rows[i]["Region"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Region"]);
                    sums.District = ds.Tables[0].Rows[i]["District"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["District"]);
                    sums.Patch = ds.Tables[0].Rows[i]["Patch"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Patch"]);
                    sums.Parent = ds.Tables[0].Rows[i]["Parent"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Parent"]);
                    sums.ProdGrp = ds.Tables[0].Rows[i]["ProdGrp"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ProdGrp"]);
                    sums.Assrt = ds.Tables[0].Rows[i]["Assrt"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Assrt"]);
                    sums.SalesUnits_TY = ds.Tables[0].Rows[i]["SalesUnits_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesUnits_TY"]);
                    sums.SalesUnits_LY = ds.Tables[0].Rows[i]["SalesUnits_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesUnits_LY"]);
                    sums.SalesUnits_2LY = ds.Tables[0].Rows[i]["SalesUnits_2LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesUnits_2LY"]);
                    sums.SalesUnits_FC = ds.Tables[0].Rows[i]["SalesUnits_FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesUnits_FC"]);
                    sums.SalesUnits_Var = ds.Tables[0].Rows[i]["SalesUnits_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesUnits_Var"]);
                    sums.SalesDollars_TY = ds.Tables[0].Rows[i]["SalesDollars_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_TY"]);
                    sums.SalesDollars_LY = ds.Tables[0].Rows[i]["SalesDollars_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_LY"]);
                    sums.SalesDollars_2LY = ds.Tables[0].Rows[i]["SalesDollars_2LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_2LY"]);
                    sums.SalesDollars_FR_FC = ds.Tables[0].Rows[i]["SalesDollars_FR_FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_FR_FC"]);
                    sums.SalesDollars_Curr = ds.Tables[0].Rows[i]["SalesDollars_Curr"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_Curr"]);
                    sums.SalesDollars_Var = ds.Tables[0].Rows[i]["SalesDollars_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_Var"]);
                    sums.CAGR = ds.Tables[0].Rows[i]["CAGR"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["CAGR"]);
                    sums.Asp_TY = ds.Tables[0].Rows[i]["Asp_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Asp_TY"]);
                    sums.Asp_LY = ds.Tables[0].Rows[i]["Asp_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Asp_LY"]);
                    sums.Asp_FC = ds.Tables[0].Rows[i]["Asp_FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Asp_FC"]);
                    sums.Asp_Var = ds.Tables[0].Rows[i]["Asp_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Asp_Var"]);
                    sums.RetailPrice_TY = ds.Tables[0].Rows[i]["RetailPrice_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["RetailPrice_TY"]);
                    sums.RetailPrice_LY = ds.Tables[0].Rows[i]["RetailPrice_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["RetailPrice_LY"]);
                    sums.RetailPrice_FC = ds.Tables[0].Rows[i]["RetailPrice_FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["RetailPrice_FC"]);
                    sums.RetailPrice_Var = ds.Tables[0].Rows[i]["RetailPrice_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["RetailPrice_Var"]);
                    sums.RetailPrice_Erosion_Rate = ds.Tables[0].Rows[i]["RetailPrice_Erosion_Rate"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["RetailPrice_Erosion_Rate"]);
                    sums.SalesDollars_FR_TY = ds.Tables[0].Rows[i]["SalesDollars_FR_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_FR_TY"]);
                    sums.SalesDollars_FR_LY = ds.Tables[0].Rows[i]["SalesDollars_FR_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SalesDollars_FR_LY"]);
                    sums.MarginDollars_FR_TY = ds.Tables[0].Rows[i]["MarginDollars_FR_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MarginDollars_FR_TY"]);
                    sums.MarginDollars_FR_LY = ds.Tables[0].Rows[i]["MarginDollars_FR_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MarginDollars_FR_LY"]);
                    sums.MarginDollars_FR_Var = ds.Tables[0].Rows[i]["MarginDollars_FR_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MarginDollars_FR_Var"]);
                    sums.Cost_TY = ds.Tables[0].Rows[i]["Cost_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Cost_TY"]);
                    sums.Cost_LY = ds.Tables[0].Rows[i]["Cost_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Cost_LY"]);
                    sums.Cost_FC = ds.Tables[0].Rows[i]["Cost_FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Cost_FC"]);
                    sums.Cost_Var = ds.Tables[0].Rows[i]["Cost_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Cost_Var"]);
                    sums.Margin_Dollars_TY = ds.Tables[0].Rows[i]["Margin_Dollars_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Dollars_TY"]);
                    sums.Margin_Dollars_LY = ds.Tables[0].Rows[i]["Margin_Dollars_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Dollars_LY"]);
                    sums.Margin_Dollars_Curr = ds.Tables[0].Rows[i]["Margin_Dollars_Curr"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Dollars_Curr"]);
                    sums.Margin_Dollars_FR = ds.Tables[0].Rows[i]["Margin_Dollars_FR"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Dollars_FR"]);
                    sums.Margin_Dollars_Var_Curr = ds.Tables[0].Rows[i]["Margin_Dollars_Var_Curr"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Dollars_Var_Curr"]);
                    sums.Margin_Percent_TY = ds.Tables[0].Rows[i]["Margin_Percent_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Percent_TY"]);
                    sums.Margin_Percent_LY = ds.Tables[0].Rows[i]["Margin_Percent_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Percent_LY"]);
                    sums.Margin_Percent_Curr = ds.Tables[0].Rows[i]["Margin_Percent_Curr"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Percent_Curr"]);
                    sums.Margin_Percent_FR = ds.Tables[0].Rows[i]["Margin_Percent_FR"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Percent_FR"]);
                    sums.Margin_Percent_Var = ds.Tables[0].Rows[i]["Margin_Percent_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Margin_Percent_Var"]);
                    sums.Turns_TY = ds.Tables[0].Rows[i]["Turns_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Turns_TY"]);
                    sums.Turns_LY = ds.Tables[0].Rows[i]["Turns_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Turns_LY"]);
                    sums.Turns_FC = ds.Tables[0].Rows[i]["Turns_FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Turns_FC"]);
                    sums.Turns_Var = ds.Tables[0].Rows[i]["Turns_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Turns_Var"]);
                    sums.SellThru_TY = ds.Tables[0].Rows[i]["SellThru_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["SellThru_TY"]);
                    sums.SellThru_LY = ds.Tables[0].Rows[i]["SellThru_LY"] is DBNull ? "0" : Convert.ToString(ds.Tables[0].Rows[i]["SellThru_LY"]);
                    sums.Dollars_FC_DL = ds.Tables[0].Rows[i]["Dollars_FC_DL"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Dollars_FC_DL"]);
                    sums.Dollars_FC_LOW = ds.Tables[0].Rows[i]["Dollars_FC_LOW"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Dollars_FC_LOW"]);
                    sums.Dollars_FC_Vendor = ds.Tables[0].Rows[i]["Dollars_FC_Vendor"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Dollars_FC_Vendor"]);
                    sums.Units_FC_DL = ds.Tables[0].Rows[i]["Units_FC_DL"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Units_FC_DL"]);
                    sums.Units_FC_LOW = ds.Tables[0].Rows[i]["Units_FC_LOW"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Units_FC_LOW"]);
                    sums.Units_FC_Vendor = ds.Tables[0].Rows[i]["Units_FC_Vendor"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Units_FC_Vendor"]);
                    sums.Dollars_FC_DL_Var = ds.Tables[0].Rows[i]["Dollars_FC_DL_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Dollars_FC_DL_Var"]);
                    sums.Dollars_FC_LOW_Var = ds.Tables[0].Rows[i]["Dollars_FC_LOW_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Dollars_FC_LOW_Var"]);
                    sums.Dollars_FC_Vendor_Var = ds.Tables[0].Rows[i]["Dollars_FC_Vendor_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Dollars_FC_Vendor_Var"]);
                    sums.Units_FC_DL_Var = ds.Tables[0].Rows[i]["Units_FC_DL_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Units_FC_DL_Var"]);
                    sums.Units_FC_LOW_Var = ds.Tables[0].Rows[i]["Units_FC_LOW_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Units_FC_LOW_Var"]);
                    sums.Units_FC_Vendor_Var = ds.Tables[0].Rows[i]["Units_FC_Vendor_Var"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Units_FC_Vendor_Var"]);
                    sums.ReceiptUnits_TY = ds.Tables[0].Rows[i]["ReceiptUnits_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ReceiptUnits_TY"]);
                    sums.ReceiptUnits_LY = ds.Tables[0].Rows[i]["ReceiptUnits_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ReceiptUnits_LY"]);
                    sums.ReceiptDollars_TY = ds.Tables[0].Rows[i]["ReceiptDollars_TY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ReceiptDollars_TY"]);
                    sums.ReceiptDollars_LY = ds.Tables[0].Rows[i]["ReceiptDollars_LY"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["ReceiptDollars_LY"]);
                    sums.PriceSensitivityImpact = ds.Tables[0].Rows[i]["PriceSensitivityImpact"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["PriceSensitivityImpact"]);
                    sums.PriceSensitivityPercent = ds.Tables[0].Rows[i]["PriceSensitivityPercent"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["PriceSensitivityPercent"]);
                    // sums.VBUPercent = ds.Tables[0].Rows[i]["VBUPercent"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["VBUPercent"]);
                    sums.MM_Comments = ds.Tables[0].Rows[i]["MM_Comments"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MM_Comments"]);
                    sums.Vendor_Comments = ds.Tables[0].Rows[i]["Vendor_Comments"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Vendor_Comments"]);

                    result.Add(sums);
                }

                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Gets the item patch overlapping table data.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<OverlappingIPOTableUI> GetOverlappingClaimsTable(DTParameterModel param, bool export)
        {
            try
            {
                var query = new DataCommands().GetOverlappingClaimsTable(param, export);
                // var result = ExpandoUtil.GetExpandoList(query);
                var result = ObjectUtil.ExecuteDataReader<OverlappingIPOTable>(query);
                IDictionary<string, OverlappingIPOTableUI> dictionary = new Dictionary<string, OverlappingIPOTableUI>();

                // Loop through the data to build distinct Item/Patch combinations with the 'RequestingOwners' column being populated
                // with VendorDesc column values concatenated witha comma (,).
                for(int i = 0; i < result.Count; i++)
                {
                    var oIPOTable = result[i];
                    // Build a key from the item/patch columns because that's the level for the Item Patch Ownership upload.
                    var key = $"{oIPOTable.ItemID}_{oIPOTable.Patch}";
                    
                    var uiRow = new OverlappingIPOTableUI
                    {
                        GMSVenID = oIPOTable.GMSVenID,
                        VendorDesc = oIPOTable.VendorDesc,
                        Owner = oIPOTable.Owner,
                        ItemID = oIPOTable.ItemID,
                        ItemDesc = oIPOTable.ItemDesc,
                        Patch = oIPOTable.Patch,
                        MM = oIPOTable.MM,
                        MD = oIPOTable.MD,
                        TimeStamp = oIPOTable.TimeStamp,
                        RequestingOwners = ""
                    };

                    // Check to see if the dictionary already has an object with the key.
                    dictionary.TryGetValue(key, out OverlappingIPOTableUI itemPatch);

                    // We display an owner of an item/patch per row so they must end up as the object for the key and its 'RequestingOwners'
                    // property contains the list of vendors separated by comma.
                    if (uiRow.Owner)
                    {
                        // No object has been inserted so far so add the owner object.
                        if (itemPatch == null)
                        {
                            dictionary.Add(key, uiRow);
                        }
                    }
                    else // This executes when the object is not the owner of the item/patch claim.
                    {
                        // If no object exists for the key then add it.
                        if (itemPatch == null)
                        {
                            dictionary.Add(key, uiRow);
                        }
                        else // An object already exists so we update its 'RequestingOwnersByDate' dictionary with the vendor desc and timestamp.
                        {
                            // Check if the RequestingOwnersByDate has been initialized yet. If not then initialize it.
                            if (dictionary[key].RequestingOwnersByDate == null)
                            {
                                dictionary[key].RequestingOwnersByDate = new Dictionary<string, DateTime>();
                            }

                            // Check to see if any vendor descriptions already exist. This is incase we have duplicates in the back-end.
                            // We shouldn't, but just incase...
                            dictionary[key].RequestingOwnersByDate.TryGetValue(uiRow.VendorDesc, out DateTime existingTimestmap);
                            if (existingTimestmap != null) // If it exists then set the new timestamp. This means there was a duplicate.
                                dictionary[key].RequestingOwnersByDate[uiRow.VendorDesc] = DateTime.Parse(uiRow.TimeStamp);
                            else // Non existed so add the new one. This should always be executing. 
                                dictionary[key].RequestingOwnersByDate.Add(uiRow.VendorDesc, DateTime.Parse(uiRow.TimeStamp));
                        }
                    }
                }

                // Loop through all the rows and sort the 'RequestingOwnersByDate' dictionary values (timestamp) in assending order
                // and concatenate the vendor descriptions as a comma sepparated list.
                // This displays the vendor names in a first-come, first-serve basis.
                var rows = dictionary.Values.Select(oIPORow =>
                {
                    oIPORow.RequestingOwners = string.Join(", ", oIPORow.RequestingOwnersByDate.OrderBy(key => key.Value)
                        .Select(keyVal => keyVal.Key));
                    return oIPORow;
                }).ToList();

                return rows;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Gets a list of events if <paramref name="eventId"/> is not passed. If there's an <paramref name="eventId"/>
        /// then only one event will be returned if it exists.
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public List<DlEvent> GetDlEvents(int eventId = -1)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var forecastEvent = new DlEvent()
            {
                EventId = -1,
                Title = "No Title",
                Body = "No Description",
                FileId = "",
                StartTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                EndTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                LastEdit = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                Target = "forecast"
            };
            var result = new List<DlEvent>();
            if (eventId >= 0) {
            }

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = (eventId == -1) ? new DataCommands().GetDlEvents() : new DataCommands().GetDlEvent(eventId);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var temp = new DlEvent()
                    {
                        EventId = ds.Tables[0].Rows[i]["ID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]),
                        Title = ds.Tables[0].Rows[i]["Title"] is DBNull ? "No Title" : Convert.ToString(ds.Tables[0].Rows[i]["Title"]),
                        Body = ds.Tables[0].Rows[i]["Body"] is DBNull ? "No Description" : Convert.ToString(ds.Tables[0].Rows[i]["Body"]),
                        FileId = ds.Tables[0].Rows[i]["FileId"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["FileId"]),
                        StartTime = (ds.Tables[0].Rows[i]["StartTime"] is DBNull ? DateTime.Now : Convert.ToDateTime(ds.Tables[0].Rows[i]["StartTime"])).ToString("MM/dd/yyyy HH:mm:ss"),
                        EndTime = (ds.Tables[0].Rows[i]["EndTime"] is DBNull ? DateTime.Now : Convert.ToDateTime(ds.Tables[0].Rows[i]["EndTime"])).ToString("MM/dd/yyyy HH:mm:ss"),
                        LastEdit = (ds.Tables[0].Rows[i]["LastEdit"] is DBNull ? DateTime.Now : Convert.ToDateTime(ds.Tables[0].Rows[i]["LastEdit"])).ToString("MM/dd/yyyy HH:mm:ss"),
                        Target = (ds.Tables[0].Rows[i]["Target"] is DBNull ? "forecast" : Convert.ToString(ds.Tables[0].Rows[i]["Target"]))
                    };
                    result.Add(temp);
                }
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }

            if (result.Count < 1)
                result.Add(forecastEvent);

            return result;
        }
        
        /// <summary>
        /// Gets the MM's actual name that is displayed in the back-end tables.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="gmsvenid"></param>
        /// <param name="bookmarkName"></param>
        /// <returns></returns>
        public string GetMMName(EditorParameterModel editor)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var mmName = string.Empty;

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = string.Format(@"SELECT MM FROM Forecast{0}.config_mm WHERE lower(UserName) = lower('{1}');"
                    , new DataCommands().GetDatabaseContext()
                    , editor.Username);
                VerticaDataReader dr = command.ExecuteReader();

                //We only need to know about the first one because we search on the three keys of the 
                //bookmark table so it will only return one row for this specific request
                if (dr.Read())
                {
                    mmName = Convert.ToString(dr["MM"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                //Debug.WriteLine($"Could not read BookmarkName from VerticaDataReader. Error: {e.Message}");
                throw e;
            }

            return mmName;
        }

        public List<object> GetTutorialGroups()
        {
            return GetListFromVertica("", new DataCommands().GetTutorialGroups, MapTutorialGroup);
        }

        public List<Tutorial> GetTutorials()
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<Tutorial>();

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetTutorials();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var tutorial = new Tutorial()
                    {
                        TutorialId = ds.Tables[0].Rows[i]["ID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["ID"]),
                        Title = ds.Tables[0].Rows[i]["Title"] is DBNull ? "No Title" : Convert.ToString(ds.Tables[0].Rows[i]["Title"]),
                        TutorialGroup = ds.Tables[0].Rows[i]["TutorialGroup"] is DBNull ? "No Group" : Convert.ToString(ds.Tables[0].Rows[i]["TutorialGroup"]),
                        Intro = ds.Tables[0].Rows[i]["Intro"] is DBNull ? "No Description" : Convert.ToString(ds.Tables[0].Rows[i]["Intro"]),
                        VideoLink = ds.Tables[0].Rows[i]["VideoLink"] is DBNull ? "No Link Here" : Convert.ToString(ds.Tables[0].Rows[i]["VideoLink"]),
                        LastEdit = ds.Tables[0].Rows[i]["LastEdit"] is DBNull ? "01/01/0001" : Convert.ToDateTime(ds.Tables[0].Rows[i]["LastEdit"]).ToString("MM/dd/yyyy HH:mm:ss")
                    };

                    result.Add(tutorial);
                }
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Grabs the count of the forecast table including currently filtered items.  This is used for paging.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="filtered"></param>
        /// <returns></returns>
        public int GetForecastTableCount(DTParameterModel param, bool filtered = false, bool edit = false)
        {
            int result = 0;
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetForecastTableCount(param, filtered, edit);
                VerticaDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    result = Convert.ToInt32(dr[0]);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Grabs the count of the forecast table including currently filtered items.  This is used for paging.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="filtered"></param>
        /// <returns></returns>
        public int GetOverlappingItemPatchTableCount(DTParameterModel param)
        {
            int result = 0;
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetOverlappingItemPatchTableCount(param);
                VerticaDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    result = Convert.ToInt32(dr[0]);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Get the source table for user
        /// <param name="gmsvenid"></param>
        /// </summary>
        public string GetTableName(int gmsvenid, string vendorgroup, string username)
        {
            string result = string.Empty;

            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetTableName(gmsvenid, vendorgroup, username);
                VerticaDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    result = Convert.ToString(dr[0]);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }
            return result;
        }

        public IEnumerable<SummaryTotals> GetUnitsSummaryTable(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<SummaryTotals>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetUnitsSummaryTable(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < 5; i++)
                {
                    SummaryTotals totals = new SummaryTotals();
                    totals.ForecastDef = ds.Tables[0].Rows[0][$"Forecast_{i}"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"Forecast_{i}"]);
                    totals.Actual = ds.Tables[0].Rows[0][$"Actual_{i}"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"Actual_{i}"]);
                    totals.FC = ds.Tables[0].Rows[0][$"FC_{i}"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"FC_{i}"]);
                    totals.Var = ds.Tables[0].Rows[0][$"Var_{i}"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[0][$"Var_{i}"]);
                    result.Add(totals);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }
            return result;
        }

        public IEnumerable<SummaryTotals> GetDollarSummaryTable(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<SummaryTotals>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetDollarSummaryTable(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < 5; i++)
                {
                    SummaryTotals totals = new SummaryTotals();
                    totals.ForecastDef = ds.Tables[0].Rows[0][$"Forecast_{i}"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"Forecast_{i}"]);
                    totals.Actual = ds.Tables[0].Rows[0][$"Actual_{i}"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"Actual_{i}"]);
                    totals.FC = ds.Tables[0].Rows[0][$"FC_{i}"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0][$"FC_{i}"]);
                    totals.Var = ds.Tables[0].Rows[0][$"Var_{i}"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[0][$"Var_{i}"]);
                    result.Add(totals);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }
            return result;
        }

        public IEnumerable<SummaryTotals> GetMarginPercentSummaryTable(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<SummaryTotals>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetMarginPercentSummaryTable(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    SummaryTotals totals = new SummaryTotals();
                    totals.Actual = ds.Tables[0].Rows[i]["Actual"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Actual"]);
                    totals.FC = ds.Tables[0].Rows[i]["FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["FC"]);
                    totals.Var = ds.Tables[0].Rows[i]["Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Var"]);
                    result.Add(totals);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }
            return result;
        }

        public IEnumerable<SummaryTotals> GetMarginDollarSummaryTable(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<SummaryTotals>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetMarginDollarSummaryTable(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    SummaryTotals totals = new SummaryTotals();
                    totals.Actual = ds.Tables[0].Rows[i]["Actual"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Actual"]);
                    totals.FC = ds.Tables[0].Rows[i]["FC"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["FC"]);
                    totals.Var = ds.Tables[0].Rows[i]["Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Var"]);
                    result.Add(totals);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }
            return result;
        }

        public List<Notification> GetNotifications(UserInfo userInfo, bool all = false)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<Notification>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = all ? new DataCommands().GetNotificationsList() : new DataCommands().GetNotifications(userInfo);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var notification = new Notification
                    {
                        NotifId = ds.Tables[0].Rows[i]["NotifID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["NotifID"]),
                        Title = ds.Tables[0].Rows[i]["Title"] is DBNull ? "No Title" : Convert.ToString(ds.Tables[0].Rows[i]["Title"]),
                        Body = ds.Tables[0].Rows[i]["Body"] is DBNull ? "No Description" : Convert.ToString(ds.Tables[0].Rows[i]["Body"]),
                        NotificationType = ds.Tables[0].Rows[i]["NotificationType"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["NotificationType"]),
                        NotificationTypeId = ds.Tables[0].Rows[i]["NotificationTypeId"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["NotificationTypeId"]),
                        TableName = ds.Tables[0].Rows[i]["TableName"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["TableName"]),
                        StartTime = ds.Tables[0].Rows[i]["StartTime"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["StartTime"]),
                        EndTime = ds.Tables[0].Rows[i]["EndTime"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["EndTime"]),
                        Target = ds.Tables[0].Rows[i]["Target"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["Target"])
                    };


                    result.Add(notification);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        public int GetNotificationsCount(UserInfo userInfo)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = 0;
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetNotifications(userInfo);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                result = ds.Tables[0].Rows.Count;
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        public List<ConfigNotification> GetNotificationCategories()
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<ConfigNotification>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetNotificationCategories();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var notification = new ConfigNotification
                    {
                        NCID = ds.Tables[0].Rows[i]["NCID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["NCID"]),
                        NotifTypeName = ds.Tables[0].Rows[i]["NotifTypeName"] is DBNull ? "No Title" : Convert.ToString(ds.Tables[0].Rows[i]["NotifTypeName"])
                    };


                    result.Add(notification);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Sends a request for specific cells of type Model.ForecastDetail that are updated when edits are made.
        /// </summary>
        /// <param name="forcastIDs"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IEnumerable<ForecastDetails> GetUpdatedCellsByForecastIds(DTParameterModel param)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<ForecastDetails>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetUpdatedCellsByForecastIds(param);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    ForecastDetails details = new ForecastDetails();
                    details.ForecastID = ds.Tables[0].Rows[i]["ForecastID"] is DBNull ? "ERROR" : Convert.ToString(ds.Tables[0].Rows[i]["ForecastID"]);
                    details.SalesUnits_FC = ds.Tables[0].Rows[i]["SalesUnits_FC"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["SalesUnits_FC"]);
                    details.SalesUnits_Var = ds.Tables[0].Rows[i]["SalesUnits_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesUnits_Var"]);
                    details.SalesDollars_FR_FC = ds.Tables[0].Rows[i]["SalesDollars_FR_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_FR_FC"]);
                    details.SalesDollars_Curr = ds.Tables[0].Rows[i]["SalesDollars_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_Curr"]);
                    details.SalesDollars_Var = ds.Tables[0].Rows[i]["SalesDollars_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["SalesDollars_Var"]);
                    details.Asp_FC = ds.Tables[0].Rows[i]["Asp_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Asp_FC"]);
                    details.Asp_Var = ds.Tables[0].Rows[i]["Asp_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Asp_Var"]);
                    details.RetailPrice_FC = ds.Tables[0].Rows[i]["RetailPrice_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_FC"]);
                    details.RetailPrice_Var = ds.Tables[0].Rows[i]["RetailPrice_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["RetailPrice_Var"]);

					details.MarginDollars_FR_Var = ds.Tables[0].Rows[i]["MarginDollars_FR_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["MarginDollars_FR_Var"]);

					details.Cost_FC = ds.Tables[0].Rows[i]["Cost_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Cost_FC"]);
                    details.Cost_Var = ds.Tables[0].Rows[i]["Cost_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Cost_Var"]);
                    details.Margin_Dollars_Curr = ds.Tables[0].Rows[i]["Margin_Dollars_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_Curr"]);
                    details.Margin_Dollars_FR = ds.Tables[0].Rows[i]["Margin_Dollars_FR"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_FR"]);
                    details.Margin_Dollars_Var_Curr = ds.Tables[0].Rows[i]["Margin_Dollars_Var_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Dollars_Var_Curr"]);
                    details.Margin_Percent_Curr = ds.Tables[0].Rows[i]["Margin_Percent_Curr"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_Curr"]);
                    details.Margin_Percent_FR = ds.Tables[0].Rows[i]["Margin_Percent_FR"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_FR"]);
                    details.Margin_Percent_Var = ds.Tables[0].Rows[i]["Margin_Percent_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Margin_Percent_Var"]);
                    details.Turns_FC = ds.Tables[0].Rows[i]["Turns_FC"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Turns_FC"]);
                    details.Turns_Var = ds.Tables[0].Rows[i]["Turns_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Turns_Var"]);

                    details.Dollars_FC_DL = ds.Tables[0].Rows[i]["Dollars_FC_DL"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_DL"]);
                    details.Dollars_FC_LOW = ds.Tables[0].Rows[i]["Dollars_FC_LOW"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_LOW"]);
                    details.Dollars_FC_Vendor = ds.Tables[0].Rows[i]["Dollars_FC_Vendor"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_Vendor"]);
                    
                    details.Dollars_FC_DL_Var = ds.Tables[0].Rows[i]["Dollars_FC_DL_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_DL_Var"]);
                    details.Dollars_FC_LOW_Var = ds.Tables[0].Rows[i]["Dollars_FC_LOW_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_LOW_Var"]);
                    details.Dollars_FC_Vendor_Var = ds.Tables[0].Rows[i]["Dollars_FC_Vendor_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Dollars_FC_Vendor_Var"]);
                    
                    details.Units_FC_LOW = ds.Tables[0].Rows[i]["Units_FC_LOW"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["Units_FC_LOW"]);
                    details.Units_FC_Vendor = ds.Tables[0].Rows[i]["Units_FC_Vendor"] is DBNull ? 0 : Convert.ToInt32(ds.Tables[0].Rows[i]["Units_FC_Vendor"]);
                    
                    details.Units_FC_LOW_Var = ds.Tables[0].Rows[i]["Units_FC_LOW_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Units_FC_LOW_Var"]);
                    details.Units_FC_Vendor_Var = ds.Tables[0].Rows[i]["Units_FC_Vendor_Var"] is DBNull ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[i]["Units_FC_Vendor_Var"]);

                    details.MM_Comments = ds.Tables[0].Rows[i]["MM_Comments"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["MM_Comments"]);
                    details.Vendor_Comments = ds.Tables[0].Rows[i]["Vendor_Comments"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[i]["Vendor_Comments"]);

                    result.Add(details);
                }
                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        public IEnumerable<User> GetUserDetails(string username)
        {

            var details = new List<User>();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetUserDetail(username);
                VerticaDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                        var detail = new Models.User();
                        detail.GMSVenID = reader["GMSVenID"] is DBNull ? 0 : Convert.ToInt32(reader["GMSVenID"]);
                        detail.NTName = reader["NTName"].ToString();
                        detail.Username = username;
                        detail.VendorGroup = reader["VENDORGROUP"].ToString();
                        details.Add(detail);

                }
                reader.Close();
                connection.Close();
                command.Dispose();
                connection.Dispose();
            }
            catch (Exception)
            {

            }
            return details;

        }

        public List<KeyValuePair<int, string>> GetVendorList()
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<KeyValuePair<int, string>>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetVendorList();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var key = ds.Tables[0].Rows[i]["GMSVenID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[i]["GMSVenID"]);
                    var val = ds.Tables[0].Rows[i]["VendorDesc"] is DBNull ? "No Vendor" : Convert.ToString(ds.Tables[0].Rows[i]["VendorDesc"]);
                    var vend = new KeyValuePair<int, string>(key, val);
                    result.Add(vend);
                }

                connection.Close();
            }
            catch (Exception)
            {

            }

            return result;
        }

        /// <summary>
        /// Determine whether the username and password is associated with a user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>TRUE = Is a user; FALSE = Is not a user</returns>
        public bool IsUser(string username, string password)
        {
            // Username parameter needs "GMS\" appended to it.
            string usernameParam = "GMS\\" + username;

            // Assume this is not a user
            var result = false;

            // Init a new Vertica Connection
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetIsUser(usernameParam); ;

                VerticaDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    result = true;
                }
                dr.Close();
                connection.Close();
                command.Dispose();
                connection.Dispose();
            }

            catch (Exception)
            {

            }

            return result;
        }

        #endregion SELECT

        #region UPDATE

        public void UpdateDlEvent(DlEvent edit)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().UpdateDlEvent(edit);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void UpdateNotification(Notification original, Notification edit)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().UpdateNotification(original, edit);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void UpdateTutorial(Tutorial original, Tutorial edit)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().UpdateTutorial(original, edit);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Updates the ImportProcesses table in Forecast for the given <seealso cref="ImportProcess"/> object. 
        /// It will first check if the vendor already exists in the database. If the vendor exists in there then 
        /// the vendor information will be updated. If not, then a new record will be inserted.
        /// </summary>
        /// <param name="importProcess"></param>
        public void UpdateVendorImportProcess(ImportProcess importProcess)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();

                var existingImportProcess = GetImportProcess(importProcess.GMSVenID);

                if (existingImportProcess.GMSVenID == -1 && existingImportProcess.ProcessId == -1)
                {
                    command.CommandText = new DataCommands().CreateImportProcess(importProcess);
                }
                else
                {
                    command.CommandText = new DataCommands().UpdateVendorImportProcess(importProcess);
                }

                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception )
            {

            }
        }

        public async Task<FileUploadResult> RemoveItemPatchOwnershipClaims(EditorParameterModel editor, ItemPatchOverlap itemPatchOverlap)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var stopWatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString();

            try
            {
                var dataCommands = new DataCommands();
                var isPreFreeze = GetToolConfigValue("preFreeze");
                var isFreeze = GetToolConfigValue("freeze");
                var gmsVenId = int.Parse(editor.GMSVenID);

                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = dataCommands.CreateIOUValidTableRecordsByAction(editor, itemPatchOverlap, batchId);
                command.ExecuteNonQuery();

                // Execute any remove requests only when Forecast hasn't been frozen yet.
                if (!isPreFreeze)
                {
                    command.CommandText = dataCommands.DeleteIOURecordFromVendor(editor.TableName, batchId);
                    command.ExecuteNonQuery();
                }

                // Update the item/patch ownership table to set items with no requests to 'No Vendor' or cascade 
                // to next vendor in line.
                command.CommandText = dataCommands.CreateIOUOwnershipTableCascadeOverlap(batchId);
                command.ExecuteNonQuery();

                // Update the IOU Overlap table to delete records with no overlaps in ownership requests
                // or assign it to the next vendor in line.
                command.CommandText = dataCommands.CreateIOUOverlapTableDeleteUpdate(batchId);
                command.ExecuteNonQuery();

                if (!isPreFreeze)
                {
                    var isRunIOURemovalUpdatesSuccessful = await RunIOURemovalUpdates(connection, editor.TableName, batchId, isFreeze);
                    //filter update
                    command.CommandText = dataCommands.CreateIOUUpdateFiltersScript(editor.TableName);
                    command.ExecuteNonQuery();
                }

                connection.Close();
                command.Dispose();

                var successResponse = new FileUploadResult
                {
                    fileName = "",
                    success = true,
                    message = "Success! All selected Item Patch claims have been removed."
                };
                UpdateUploadLog(new UploadLog
                {
                    GmsVenId = int.Parse(editor.GMSVenID),
                    VendorDesc = editor.VendorGroup,
                    FileUploadType = "IOU",
                    FileName = "NO FILE",
                    TimeStamp = Util.GetTimestamp(),
                    Success = true,
                    UserLogin = editor.Username,
                    SuccessOrFailureMessage = $"Success. Vendor {editor.VendorGroup} removed itemid/patch claims.",
                    Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                });
                DropIOUPartitionData(editor, "NO FILE", batchId);
                stopWatch.Stop();
                return successResponse;
            }
            catch (Exception e)
            {
                connection.Close();
                var crashResponse = new FileUploadResult
                {
                    fileName = "",
                    success = false,
                    message = "An error occurred. We could not remove the selected claims. Please contact support at support@demandlink.com."
                };
                UpdateUploadLog(new UploadLog
                {
                    GmsVenId = int.Parse(editor.GMSVenID),
                    VendorDesc = editor.VendorGroup,
                    FileUploadType = "IOU",
                    FileName = "NO FILE",
                    TimeStamp = Util.GetTimestamp(),
                    Success = false,
                    UserLogin = editor.Username,
                    SuccessOrFailureMessage = $"Exception catch. Vendor remove item/patch claims. Message: {e.Message}.",
                    Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                });
                DropIOUPartitionData(editor, "NO FILE", batchId);
                stopWatch.Stop();
                return crashResponse;
            }
        }

        public bool CheckView(EditorParameterModel editor)
        {
            try
            {
                VerticaConnection connection = new VerticaConnection(_verticaWebConn);
                VerticaDataAdapter adapter = new VerticaDataAdapter();
                DataSet vw = new DataSet();
                connection.Open();

                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().CheckIfView(editor.TableName);
                adapter.SelectCommand = command;
                adapter.Fill(vw);
                String view = vw.Tables[0].Select()[0].ItemArray[0].ToString();
                var v= bool.Parse(view);
                return v;
            }
            catch (Exception)
            {
                //default to false
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editor"></param>
        public Task<int> UpdateRetailPrice(EditorParameterModel editor)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();

                if(editor.IsMD == true)
                {
                    var view = CheckView(editor);
                    if (view == true)
                    {
                        command.CommandText = new DataCommands().UpdateRetailPrice(editor, "tbl_AllVendors");
                    }
                    else
                    {
                        command.CommandText = new DataCommands().UpdateRetailPrice(editor, editor.TableName);
                    }
                }
                else
                {
                    command.CommandText = new DataCommands().UpdateRetailPrice(editor, editor.TableName);

                }
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception)
            {

            }

            return UpdateCascade(editor, TableUpdateType.ERetailPrice);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editor"></param>
        public async Task<int> UpdateSalesUnits(EditorParameterModel editor)
        {
            List<string> tableNames = await GetTableNames(editor);

            if (editor.IsMD == true) {
                var view = CheckView(editor);
                if (view == true)
                {
                    tableNames.Insert(0, "tbl_AllVendors");
                }
                else
                {
                    tableNames.Insert(0, editor.TableName);

                }
            }
          
            else
            {
                tableNames.Insert(0, editor.TableName);

            }
            List<Task> tasks = new List<Task>();
            var commands = new DataCommands();

            var UUID = Guid.NewGuid();
            var uuidStr = $"{UUID}".Replace("-", "").Substring(0, 10);
            var tableName = $"temp_{editor.Username}_{UUID}".Replace("-", "");
            var innerTableName = $"temp_inner_{UUID}".Replace("-", "");

            // Check if forecasting data is frozen or not.
            var isFrozen = GetToolConfigValue("freeze");

            try
            {
                VerticaConnection connection = new VerticaConnection(_verticaWebConn);
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = commands.CreateUpdateSalesUnitsTable(editor, tableName, innerTableName, isFrozen);
                command.ExecuteNonQuery();
                connection.Close();
                command.Dispose();
            }
            catch (Exception)
            {
                DropTempExportTable(tableName);
                DropTempExportTable(innerTableName);
                return 0;
            }

            string wherePatch = string.Empty;
            if (editor.IsMM && !IsRotatedOn(editor, "MM"))
            {
                var mmName = GetMMName(editor);
                wherePatch = $"MM = '{mmName}'";
            }
            else if (IsVendor(editor) && !IsRotatedOn(editor, "VendorDesc"))
            {
                wherePatch = $"GMSVenId = {editor.GMSVenID}";
            }

            try
            {
                foreach (var table in tableNames)
                {
                    //if (table == "tbl_AllVendors")
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
                            connection.Open();
                            VerticaCommand command = connection.CreateCommand();
                            command.CommandText = commands.UpdateSalesUnits(editor, tableName, table, innerTableName, isFrozen, wherePatch);
                            command.ExecuteNonQuery();
                            connection.Close();
                            command.Dispose();
                        }));
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                DropTempExportTable(tableName);
                DropTempExportTable(innerTableName);
                return 0;
            }

            DropTempExportTable(tableName);
            DropTempExportTable(innerTableName);
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editor"></param>
        public void UpdateSalesUVar(EditorParameterModel editor)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().UpdateSalesUVar(editor);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Finds all affected table names from an edit based on the user.
        /// </summary>
        /// <param name="editor"></param>
        /// <returns></returns>
        private async Task<List<string>> GetTableNames(EditorParameterModel editor)
        {
            ConcurrentDictionary<string, string> tableNames = new ConcurrentDictionary<string, string>(Environment.ProcessorCount, 200);
            DataCommands commands = new DataCommands();

            List<string> cmds = new List<string>();

            try
            {
                if (editor.IsMD == true)
                {
                    cmds.Add(commands.GetVendorTablesAffectedByQuery(editor));
                    cmds.Add(commands.GetMMTablesAffectedByQuery(editor));
                }
                else if (editor.IsMM == true)
                {
                    cmds.Add(commands.GetVendorTablesAffectedByQuery(editor));
                    tableNames.TryAdd("tbl_AllVendors", "tbl_AllVendors");
                }
                else if (editor.TableName == "tbl_AllVendors")
                {
                    cmds.Add(commands.GetVendorTablesAffectedByQuery(editor));
                    cmds.Add(commands.GetMMTablesAffectedByQuery(editor));
                }
                else
                {
                    cmds.Add(commands.GetMMTablesAffectedByQuery(editor));
                    tableNames.TryAdd("tbl_AllVendors", "tbl_AllVendors");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error is: {e.Message}");
            }

            List<Task> tasks = new List<Task>();

            try
            {
                foreach (var cmd in cmds)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        VerticaDataAdapter adapter = new VerticaDataAdapter();
                        DataSet ds = new DataSet();
                        VerticaConnection connection = new VerticaConnection(_verticaWebConn);
                        connection.Open();
                        VerticaCommand command = connection.CreateCommand();
                        command.CommandText = cmd;
                        adapter.SelectCommand = command;
                        adapter.Fill(ds);
                        for(int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            string tableName = Convert.ToString(ds.Tables[0].Rows[i]["TableName"]);
                            if(!string.IsNullOrEmpty(tableName))
                            {
                                tableNames.TryAdd(tableName, tableName);
                            }
                        }

                        adapter.Dispose();
                        ds.Dispose();
                        connection.Close();
                        command.Dispose();
                    }));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                return new List<string>();
            }

            return tableNames.Values.ToList();
        }

        /// <summary>
        /// Cascades an update to all necessary tables based on the user and <seealso cref="TableUpdateType"/>.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="updateType"></param>
        /// <returns></returns>
        public async Task<int> UpdateCascade(EditorParameterModel editor, TableUpdateType updateType)
        {
            List<string> tableNames = await GetTableNames(editor);
            List<Task> tasks = new List<Task>();

            string wherePatch = string.Empty;
            if (editor.IsMM && !IsRotatedOn(editor, "MM"))
            {
                var mmName = GetMMName(editor);
                wherePatch = $"MM = '{mmName}'";
            }
            else if (IsVendor(editor) && !IsRotatedOn(editor, "VendorDesc"))
            {
                wherePatch = $"GMSVenId = {editor.GMSVenID}";
            }

            try
            {
                foreach (var table in tableNames)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        VerticaConnection connection = new VerticaConnection(_verticaWebConn);
                        connection.Open();
                        VerticaCommand command = connection.CreateCommand();
                        command.CommandText = GetUpdateTypeQuery(editor, table, updateType, wherePatch);
                        command.ExecuteNonQuery();
                        connection.Close();
                        command.Dispose();
                    }));
                }
            }
            catch(Exception)
            {
                return 0;
            }

            await Task.WhenAll(tasks);
            return 1;
        }

        /// <summary>
        /// Returns a sql string for a given <seealso cref="TableUpdateType"/>.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="destTable"></param>
        /// <param name="updateType"></param>
        /// <returns></returns>
        private string GetUpdateTypeQuery(EditorParameterModel editor, string destTable, TableUpdateType updateType, string wherePatch)
        {
            var commands = new DataCommands();
            switch(updateType)
            {
                case TableUpdateType.EMMComments:
                    return commands.UpdateMMComments(editor, destTable, wherePatch);
                case TableUpdateType.ERetailPrice:
                    return commands.UpdateRetailPrice(editor, destTable, wherePatch);
                case TableUpdateType.EVendorComments:
                    return commands.UpdateVendorComments(editor, destTable);
                default:
                    return "";
            }
        }

        /// <summary>
        /// Updates the MM_Comments column in the provided table.
        /// Cascade updates to pass updates to all other tables called at end of method.
        /// </summary>
        /// <param name="editor"></param>
        public Task<int> UpdateMMComments(EditorParameterModel editor)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();

                if(editor.IsMD == true)
                {
                    var view = CheckView(editor);
                    if (view == true)
                    {
                        command.CommandText = new DataCommands().UpdateMMComments(editor, "tbl_AllVendors");

                    }
                    else
                    {
                        command.CommandText = new DataCommands().UpdateMMComments(editor, editor.TableName);

                    }
                }
                else
                {
                    command.CommandText = new DataCommands().UpdateMMComments(editor, editor.TableName);
                }
               
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception)
            {

            }

            return UpdateCascade(editor, TableUpdateType.EMMComments);
        }

        /// <summary>
        /// Updates the Vendor_Comments column in the provided table.
        /// Calls the cascade update once it's complete.
        /// </summary>
        /// <param name="editor"></param>
        public Task<int> UpdateVendorComments(EditorParameterModel editor)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();

                if(editor.IsMD == true)
                {
                    var view = CheckView(editor);
                    if (view == true)
                    {
                        command.CommandText = new DataCommands().UpdateVendorComments(editor, "tbl_AllVendors");

                    }
                    else
                    {
                        command.CommandText = new DataCommands().UpdateVendorComments(editor, editor.TableName);

                    }
                }
                else
                {
                    command.CommandText = new DataCommands().UpdateVendorComments(editor, editor.TableName);

                }
               
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception)
            {

            }

            return UpdateCascade(editor, TableUpdateType.EVendorComments);
        }

        /// <summary>
        /// Updates the notification or notifications that was/were viewed by the current user.
        /// </summary>
        /// <param name="userInfo">The current users <seealso cref="UserInfo"/></param>
        /// <param name="notifId">The id of the notification they viewed</param>
        public void UpdateViewedNotification(UserInfo userInfo, ViewedNotification notif)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();
            var result = new List<NotifiedUser>();
            try
            {
                var updateCommand = "";
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                // First we check if the users has already viewed an outdated version of this notification
                command.CommandText = new DataCommands().GetPreviousNotifiedUser(userInfo, notif);
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                var count = ds.Tables[0].Rows.Count;

                // If there were any outdated versions of this notification
                if (count > 0) {
                    // Loop though all outdated notifications that the user has previously viewed
                    for (var i = 0; i < count; i++)
                    {
                        var un = new NotifiedUser
                        {
                            UNID = ds.Tables[0].Rows[0]["ID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[0]["ID"]),
                            GMSVenID = ds.Tables[0].Rows[0]["GMSVenID"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[0]["GMSVenID"]),
                            UserName = ds.Tables[0].Rows[0]["UserName"] is DBNull ? "N/A" : Convert.ToString(ds.Tables[0].Rows[0]["UserName"]),
                            NotificationId = ds.Tables[0].Rows[0]["NotificationId"] is DBNull ? -1 : Convert.ToInt32(ds.Tables[0].Rows[0]["NotificationId"]),
                            LastEdit = ds.Tables[0].Rows[0]["LastEdit"] is DBNull ? "01/01/0001" : Convert.ToString(ds.Tables[0].Rows[0]["LastEdit"]),
                            TimeStamp = ds.Tables[0].Rows[0]["TimeStamp"] is DBNull ? "01/01/0001 01:01:01" : Convert.ToString(ds.Tables[0].Rows[0]["TimeStamp"])
                        };
                        result.Add(un);
                    }

                    // There could be a case where a users clears all notifications and some of them are new notifications
                    // and some were outdated viewed notifications so we get the new ones here.
                    var leftOvers = new ViewedNotification
                    {
                        NotifIds = notif.NotifIds.Where(n => result.Any(r => r.NotificationId != n)).ToList(),
                        TimeStamp = notif.TimeStamp
                    };

                    // Append all new notified user inserts as a string
                    if (leftOvers.NotifIds.Count() > 0 )
                    {
                        updateCommand += new DataCommands().UpdateViewedNotification(userInfo, leftOvers);
                    }

                    // Append the rest
                    updateCommand += new DataCommands().UpdateExistingNotifiedUsers(result, notif.TimeStamp);
                } 
                else 
                {
                    // This is all new notified users inserts
                    updateCommand = new DataCommands().UpdateViewedNotification(userInfo, notif);
                }

                command.Dispose();
                command = connection.CreateCommand();

                command.CommandText = updateCommand;
                command.ExecuteNonQuery();
                connection.Close();
                ds.Dispose();
            }
            catch (Exception)
            {

            }
        }

        #endregion UPDATE

        #region CREATE

        /// <summary>
        /// Creates a forecast event
        /// </summary>
        /// <param name="eventTitle">Name of the event</param>
        /// <param name="eventBody">Description of the event</param>
        /// <param name="startDate">When the event starts or empty if not start date exists</param>
        /// <param name="endDate">When the event ends or empty if it has no end date</param>
        public void CreateDlEvent(DlEvent dlEvent)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().CreateDlEvent(dlEvent);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void CreateTutorial(Tutorial tutorial)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().CreateTutorial(tutorial);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void CreateNotification(Notification notification)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().CreateNotification(notification);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion CREATE

        #region DELETE
        
        /// <summary>
        /// Delete a forecast event based on its id.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteDlEvent(int id)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().DeleteDlEvent(id);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Delete a forecast tutorial based on its id.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteTutorial(int id)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().DeleteTutorial(id);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Delete a forecast notification based on its id.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteNotification(int id)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().DeleteNotification(id);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion DELETE

        #region EXECUTE

        #endregion EXECUTE

        #region BOOKMARKS

        /// <summary>
        /// Creates a bookmark based off the JSON object in LocalStorage.  Adds to the table tbl_Bookmarks.
        /// </summary>
        /// <param name="gmsvenid"></param>
        /// <param name="username"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="state"></param>
        public void CreateBookmark(int gmsvenid, string username, string bookmarkName, string state)
        {
            try
            {
                var bookmarksManager = new BookmarksManager(gmsvenid, username);
                bookmarksManager.CreateBookmark(bookmarkName, state);
            }
            catch (Exception)
            {
              
            }
        }

        /// <summary>
        /// Deletes a bookmark from the tbl_Bookmarks table.
        /// </summary>
        /// <param name="gmsvenid"></param>
        /// <param name="username"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="state"></param>
        public void DeleteBookmark(int gmsvenid, string username, string bookmarkName)
        {
            try
            {
                var bookmarksManager = new BookmarksManager(gmsvenid, username);
                bookmarksManager.DeleteBookmark(bookmarkName);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Retrieve a bookmark and send it to the controller
        /// </summary>
        public ForecastBookmark GetBookmark(int gmsvenid, string username, string bookmarkName)
        {
            try
            {
                var bookmarksManager = new BookmarksManager(gmsvenid, username);
                var bookmark = bookmarksManager.GetBookmark(bookmarkName);
                return bookmark;
            }
            catch (Exception)
            {
                return new ForecastBookmark();
            }
        }

        /// <summary>
        /// Retrieve a list of bookmark names and send it to the controller
        /// </summary>
        public List<FilterParameter> GetBookmarkList(int gmsvenid, string username)
        {
            var bookmarksManager = new BookmarksManager(gmsvenid, username);
            return bookmarksManager.GetBookmarkNames();
        }

        /// <summary>
        /// Update an existing bookmark.
        /// </summary>
        /// <param name="gmsvenid"></param>
        /// <param name="username"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="state"></param>
        public void UpdateBookmark(int gmsvenid, string username, string bookmarkName, string state)
        {
            try
            {
                var bookmarkManager = new BookmarksManager(gmsvenid, username);
                bookmarkManager.UpdateBookmark(bookmarkName, state);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion BOOKMARKS

        #region EXPORT

        /// <summary>
        /// Creates a table for an export and inserts data from a select statement provided by 
        /// <paramref name="exportInfo"/> Item3.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="exportInfo"></param>
        /// <param name="tableName"></param>
        public void CreateTableWithData(DTParameterModel param, ExportInfo exportInfo, string tableName)
        {
            // This is the list of columns to build the table with
            var listOfColumns = exportInfo.ColumnsToBuild.Split(',').ToList();
            var listOfColumnTypes = new DataCommands().GetForecastTableColumnInfo();
            
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var result = new List<ExpandoObject>();
            var sBuilder = new StringBuilder();
            var tableCreate = "";

            // We must first get the columns from an existing table and the corresponding column datatypes
            // We store the column name and datatype into a list of key value pairs to be ordered 
            // properly later to avoid insert error.
            List<KeyValuePair<string, string>> columns = new List<KeyValuePair<string, string>>();
            foreach (var column in listOfColumnTypes)
            {
                if (listOfColumns.Contains(column.Key, new CompareIgnoreCase()))
                {
                    columns.Add(new KeyValuePair<string, string>(column.Key, column.Value));
                }
            }

            // Make sure columns are ordered in the right order
            columns = columns.OrderBy(c => listOfColumns.FindIndex(lc => lc.Equals(c.Key, StringComparison.CurrentCultureIgnoreCase))).ToList();
            columns.ForEach(c =>
            {
                sBuilder.Append($"{c.Key} ");
                sBuilder.Append($"{c.Value}, ");
            });

            // Store columns and datatypes only all separated by commas
            tableCreate = sBuilder.ToString();

            // Remove the last comma
            tableCreate = tableCreate.Remove(tableCreate.Length - 2, 1);

            // Here we build the table and insert the data
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().CreateExportTableWithData(tableName.Replace("-", ""), tableCreate, exportInfo);
                command.ExecuteNonQuery();
                
                connection.Close();
            }
            catch (Exception e)
            {
                var source = $"Error when building new items export.";
                throw e;
            }
        }

        /// <summary>
        /// Drops a table with name given by <paramref name="tableName"/>.
        /// </summary>
        /// <param name="tableName"></param>
        public void DropTempExportTable(string tableName)
        {
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().DropExportTableWithData(tableName);
                command.ExecuteNonQuery();

                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public IEnumerable<ExpandoObject> GetExportReportItemPatchOwnershipList(DTParameterModel param, ExportInfo exportInfo)
        {
            var query = new DataCommands().GetExportReportItemPatchOwnership(param);
            var results = ExpandoUtil.GetExpandoList(query);

            if (results.Count == 0)
            {
                var columnNames = GetItemPatchOwnershipDataColumns();
                var defaultObject = ExpandoUtil.CreateExpandoObject(columnNames, typeof(string));
                results = new List<ExpandoObject> { defaultObject };
            }

            return results;
        }

        public IEnumerable<ExpandoObject> GetExportReportOverlappingItemPatch(DTParameterModel param, ExportInfo exportInfo)
        {
            var result = GetOverlappingClaimsTable(param, true);
      
            if (result.Count == 0)
            {
                var columnNames = GetItemPatchOverlapDataColumns();
                var defaultObject = ExpandoUtil.CreateExpandoObject(columnNames, typeof(string));
                return new List<ExpandoObject> { defaultObject };
            }

            return result.Select(oipo =>
            {
                var exportRow = new OverlappingIPOTableExport
                {
                    VendorDesc = oipo.VendorDesc,
                    RequestingOwners = oipo.RequestingOwners,
                    ItemID = oipo.ItemID,
                    ItemDesc = oipo.ItemDesc,
                    Patch = oipo.Patch,
                    MM = oipo.MM,
                    MD = oipo.MD
                };

                return exportRow.ToExpandoObject();
            }).AsEnumerable<ExpandoObject>();
        }

        /// <summary>
        /// Exports an export based on the ExportChoice from <paramref name="param"/>. It uses the columns
        /// provided in the <paramref name="exportInfo"/> parameter and the file name from the <paramref name="exportInfo"/> 
        /// as well.
        /// </summary>
        /// <param name="param"> A <seealso cref="DTParameterModel"/> that has the ExportChoice in it and any necessary filters.</param>
        /// <param name="exportInfo"> A <seealso cref="ExportInfo"/> that contains the columns names 
        /// sepparated by commas "," in the first item and the file name in the second item.</param>
        /// <returns> A <seealso cref="List{T}"/> of <seealso cref="ExpandoObject"/>'s. </returns>
        public IEnumerable<ExpandoObject> GetExportReportWithData(DTParameterModel param, ExportInfo exportInfo, string tableName, int start)
        {
            var dc = new DataCommands();

            var sqlCommand = dc.GetDataFromIndexWithLimit(tableName, exportInfo.OrderByColumns, start, MaxRowsPerFile);

            var columnList = exportInfo.ColumnsToSelect;

            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            DataSet ds = new DataSet();

            var result = new List<ExpandoObject>();
            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = sqlCommand;
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    // We use a dynamic object so we can create as many columns as we want on the 
                    // fly without actually knowing what their names are. This way we can avoid creating 
                    // predefined objects with properties for every export that we do a straight select on.
                    dynamic dynamicObject = new ExpandoObject();

                    //Loop through every column name
                    foreach (var column in columnList)
                    {
                        // Get the value as it is without converting it to anything
                        var value = ds.Tables[0].Rows[i][column.Key];

                        // This is used for int only values
                        Int32 val = 0;

                        // We check to see what datatype the value is and convert it accordingly
                        if (value is DBNull)
                        {
                            CreatExpandoPropertyWithVal(dynamicObject, column.Key, column.Value);
                        }
                        else if (value.GetType() == typeof(decimal))
                        {
                            CreatExpandoPropertyWithVal(dynamicObject, column.Key, Convert.ToDecimal(value));
                        }
                        else if (Int32.TryParse(Convert.ToString(value), out val))
                        {
                            CreatExpandoPropertyWithVal(dynamicObject, column.Key, val);
                        }
                        else
                        {
                            CreatExpandoPropertyWithVal(dynamicObject, column.Key, Convert.ToString(value));
                        }
                    }
                    result.Add(dynamicObject);
                }
                
                ds.Dispose();
                adapter.Dispose();
                connection.Close();
            }
            catch (Exception e)
            {
                DropTempExportTable(tableName);
                var source = $"Error when building template {param.ExportChoice} with data export.";
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Get an instance of a <see cref="ExportTemplateInfo"/> that contains information for downloading the
        /// Lowe's forecasting file.
        /// </summary>
        /// <returns>An instance of a <see cref="ExportTemplateInfo"/> object.</returns>
        public ExportTemplateInfo GetLowesForecastingFileInfo() => GetExportTemplateInfo("LowesForecastingFile");

        public ExportTemplateInfo GetNewItemUploadColumns()
        {
            var exportTemplateInfo = GetExportTemplateInfo("NewItemUploadColumns");
            return exportTemplateInfo;
        }

        public ExportTemplateInfo GetItemPatchOwnershipColumns()
        {
            var exportTemplateInfo = GetExportTemplateInfo("IOU");
            return exportTemplateInfo;
        }

        public List<string> GetItemPatchOwnershipDataColumns()
        {
            var exportTemplateInfo = GetExportTemplateInfo("IOUOwnershipData");
            return exportTemplateInfo.ColumnNames;
        }

        public List<string> GetItemPatchOverlapDataColumns()
        {
            var exportTemplateInfo = GetExportTemplateInfo("IOUOverlapData");
            return exportTemplateInfo.ColumnNames;
        }

        public ExportTemplateInfo GetExportTemplateInfo(string templateType)
        {
            var exportTemplateInfo = new ExportTemplateInfo();
            var query = new DataCommands().GetDBColumnNames(templateType);
            var result = ExpandoUtil.GetExpandoList(query);
            exportTemplateInfo.ColumnNames = result.Select(expandoObject =>
            {
                expandoObject.TryGetValue("ColumnName", out string column);

                if (column != null)
                    return column;
                else
                    return "";
            }).ToList();

            if (result.Count > 0)
            {
                result[0].TryGetValue("FileName", out string fileName);
                if (fileName != null)
                {
                    exportTemplateInfo.FileName = fileName;
                }
            }

            return exportTemplateInfo;
        }

        /// <summary>
        /// Grabs the count of the table provided
        /// </summary>
        /// <param name="param"></param>
        /// <param name="filtered"></param>
        /// <returns></returns>
        public int GetTempTableCount(string tableName)
        {
            int result = 0;
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetTempTableCount(tableName);
                VerticaDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    result = Convert.ToInt32(dr[0]);
                }
                connection.Close();
            }
            catch (Exception) { }

            return result;
        }

        #endregion EXPORT

        #region IMPORT

        //public void Upload(HttpPostedFileBase file, string username)
        //{
        //    Dictionary<string, string> fileInfo = new Dictionary<string, string>();

        //    try
        //    {
        //        Imports.Import upload = new Imports.Import();
        //        fileInfo = upload.Upload(file, username);

        //        //TO DO:
        //        //load into load table
        //        //loadForecastFileToStage(fileInfo["fileName"], fileInfo["fileType"]);
        //        //update into table user has access to
        //        //propigate changes through the other tables
        //    }
        //    finally
        //    {
        //        if (File.Exists(fileInfo["fileName"]))
        //        {
        //            File.Delete(fileInfo["fileName"]);
        //        }
        //    }
        //}

        //private void loadForecastFileToStage(string fileName, string fileType)
        //{
        //    VerticaConnection connection = new VerticaConnection(_verticaWebConn);
        //    connection.Open();
        //    StreamReader myReader = File.OpenText(fileName);
        //    try
        //    {
        //        if (fileType == "ItemPatchWeek")
        //        {
        //            VerticaCopyStream stream = new VerticaCopyStream(connection, new DataCommands().ImportItemPatchWeek());
        //            stream.Start();
        //            stream.AddStream(myReader.BaseStream, true);
        //            stream.Execute();
        //            IList<long> rejects = stream.Rejects;
        //        }
        //        else if (fileType == "ItemMMTotal")
        //        {
        //            VerticaCopyStream stream = new VerticaCopyStream(connection, new DataCommands().ImportItemMM());
        //            stream.Start();
        //            stream.AddStream(myReader.BaseStream, true);
        //            stream.Execute();
        //            IList<long> rejects = stream.Rejects;
        //        }
        //        else
        //        {
        //            throw new Exception("Not a valid import file type.");
        //        }
        //    }
        //    finally
        //    {
        //        myReader.Close();
        //        connection.Close();
        //    }
        //}

        //private void mergeVerticaItemConfigData(string accessTable, string batchID)
        //{
        //    VerticaConnection connection = new VerticaConnection(_verticaWebConn);
        //    connection.Open();
        //    VerticaCommand command_merge = connection.CreateCommand();
        //    command_merge.CommandText = new ItemConfig_DataCommands().mergeItemConfigData_Vertica(gmsVenID, retailerID);
        //    VerticaParameter param_batchId = new VerticaParameter("batchID", VerticaType.VarChar, batchID);
        //    command_merge.Parameters.Add(param_batchId);
        //    VerticaDataReader dr_merge = command_merge.ExecuteReader();
        //    connection.Close();
        //}

        public void DropNewItemUploadPartition(string batchId)
        {
            var sql = new DataCommands().DropNewItemUploadPartition(batchId);
            Util.ExecuteNonQuery(sql);
        }

        public List<string> IsUploadFileValid(string filePath, string templateType)
        {
            try
            {
                // A map of back-end and user friendly column names.
                var userColumns = new DTHeaderNames().ToDictionary();
                // Header names from the csv file.
                var fileColumns = CSVHelper.GetHeaders(filePath);
                // All columns required for the import file to have.
                var requiredColumns = GetExportTemplateInfo(templateType);

                // Check for any missing column headers in the user uploaded file.
                var missingColumns = requiredColumns.ColumnNames.Where(h =>
                {
                    userColumns.TryGetValue(h, out string header);
                    return !fileColumns.Contains(header);
                }).ToList();

                return missingColumns;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void DropIOUPartitionData(EditorParameterModel editor, string fileName, string batchId)
        {
            try
            {
                var query = new DataCommands().DropIOUPartitionData(batchId);
                Util.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                UpdateUploadLog(new UploadLog
                {
                    GmsVenId = int.Parse(editor.GMSVenID),
                    VendorDesc = editor.VendorGroup,
                    FileUploadType = "IOU",
                    FileName = fileName,
                    TimeStamp = Util.GetTimestamp(),
                    Success = false,
                    UserLogin = editor.Username,
                    SuccessOrFailureMessage = $"Exception in catch of DataProvider.DropIOUPartitionData(). Message: {e.Message}.",
                    Duration = Util.GetTime(0)
                });
                throw e;
            }
        }

        private async Task<bool> RunIOUInvalidRecordsCheck(VerticaConnection connection, int gmsVenId, string batchId, string backendHeaders)
        {
            try
            {
                var dataCommands = new DataCommands();
                VerticaCommand command = new VerticaCommand(dataCommands.CreateIOUDataTypeErrorStage(gmsVenId, batchId, backendHeaders), connection);
                command.ExecuteNonQuery();
                var tasks = new List<Task<bool>>(new[]
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var query = dataCommands.CreateIOUNoDupItemPatchStage(batchId);
                            Util.ExecuteNonQuery(query);
                            return true;
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }),
                    Task.Run(() =>
                    {
                        try
                        {
                            var query = dataCommands.CreateIOUInValidItemPatchInSourceTableStage(batchId);
                            Util.ExecuteNonQuery(query);
                            return true;
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }),
                    Task.Run(() =>
                    {
                        try
                        {
                            var query = dataCommands.CreateIOUInvalidActionsStage(batchId);
                            Util.ExecuteNonQuery(query);
                            return true;
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }),
                    Task.Run(() =>
                    {
                        try
                        {
                            var query = dataCommands.CreateIOUInvalidPrimaryVendorStage(batchId);
                            Util.ExecuteNonQuery(query);
                            return true;
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    })
                });
                
                await Task.WhenAll(tasks);
                var isAnyUnsuccessful = tasks.Where(success => !success.Result).Count() > 0;
                return !isAnyUnsuccessful;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<bool> RunIOUUncontestedItemPatchInsert(VerticaConnection connection, string vendorTable, Boolean isPreFreeze, string batchId, Boolean newItemsFlag)
        {
            try
            {
                var dataCommands = new DataCommands();

                // Stage any item/patch records that have no competing claims on them into the item/patch ownership (new items too) tables.
                VerticaCommand command = new VerticaCommand(dataCommands.CreateIOUUncontestedClaimStage(batchId, newItemsFlag), connection);
                command.ExecuteNonQuery();

                var tasks = new List<Task<bool>>();
                if (!isPreFreeze)
                {
                    var mmTablesToUpdate = ExpandoUtil.GetExpandoList(dataCommands.GetIOUAffectedMMs("tmp_iou_uncontested", true), connection, true);
                    //for mds with views
                    var mdTablesToUpdate = ExpandoUtil.GetExpandoList(dataCommands.GetIOUAffectedMDs("tmp_iou_uncontested", true), connection, true);

                    //run check before updating vendors table
                    //check if we need to swap data from HP mm table to appropriate mm
                    DataSet ds = new DataSet();
                    DataSet mm = new DataSet();
                    DataSet hp_mm = new DataSet();
                    string non_hp_mm = "";
                    VerticaDataAdapter adapter = new VerticaDataAdapter();
                    if (newItemsFlag == true)
                    {
                        //run through these checks before item prodgrp changes in tbl_allvendors
                        command.CommandText = dataCommands.CheckHPChange();
                        adapter.SelectCommand = command;
                        adapter.Fill(ds);
                      //  var prodgrpidList = ExpandoUtil.GetExpandoList(dataCommands.CheckHPChange(), connection, true);

                        command.CommandText = dataCommands.FindNewMMTable();
                        adapter.SelectCommand = command;
                        adapter.Fill(mm);
                        //var mms = ExpandoUtil.GetExpandoList(dataCommands.FindNewMMTable(), connection, true);
                    }
                    

                    VerticaCommand c = new VerticaCommand(dataCommands.CreateUpdateNoVendorRecords("tbl_allvendors", newItemsFlag), connection);
                    
                    //filter insert in new vendor if not already present
                    c.ExecuteNonQuery();
                    VerticaCommand comFilter = new VerticaCommand(dataCommands.CreateIOUUpdateFilters("tbl_allvendors", "tmp_iou_uncontested", false, true, newItemsFlag, true, true), connection);
                    comFilter.ExecuteNonQuery();

                    mmTablesToUpdate.ForEach(mmAndTable =>
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                mmAndTable.TryGetValue("tablename", out string tableName);
                                tableName = tableName ?? "";
                                VerticaCommand comm = new VerticaCommand(dataCommands.CreateUpdateNoVendorRecords(tableName, newItemsFlag), connection); 
                                comm.ExecuteNonQuery();
                                //VerticaCommand comFilterMM = new VerticaCommand(dataCommands.CreateIOUUpdateFilters(tableName, tableName, false, false, newItemsFlag, true, false), connection);
                                //comFilterMM.ExecuteNonQuery();
                                //filter insert in new vendor if not already present
                                return true;
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }));
                    });

                    //for mds with views
                    mdTablesToUpdate.ForEach(mdAndTable =>
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                //only updating filters not CreateUpdateNoVendorRecords
                                mdAndTable.TryGetValue("tablename", out string tableName);
                                tableName = tableName ?? "";
                                //VerticaCommand comFilterMM = new VerticaCommand(dataCommands.CreateIOUUpdateFilters(tableName, tableName, false, false, newItemsFlag, true, false), connection);
                                //comFilterMM.ExecuteNonQuery();
                                //filter insert in new vendor if not already present
                                return true;
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }));
                    });

                    if (newItemsFlag == true)
                    {
                        //HP update check 
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            var prodgrpid = ds.Tables[0].Select();
                            //HP to non HP
                            for(int m = 0; m < prodgrpid.Length; m++)
                            {
                                var test = prodgrpid[m].ItemArray[0].ToString();

                                if (test != "512330")
                                {
                                    //get hp mm table name
                                    command.CommandText = dataCommands.GetHPMMTable();
                                    adapter.SelectCommand = command;
                                    adapter.Fill(hp_mm);
                                    var table_hp = hp_mm.Tables[0].Select();
                                    var tableArray_hp = table_hp[0].ItemArray;
                                    string hp_mm_table = tableArray_hp[0].ToString();

                                    DataSet t = new DataSet();
                                    //newMM table, item, patch
                                    command.CommandText = dataCommands.GetNewMMTablePatchItem_from_HP(hp_mm_table);
                                    adapter.SelectCommand = command;
                                    adapter.Fill(t);

                                 
                                    for (int i = 0; i < t.Tables[0].Rows.Count; i++)
                                    {

                                        var a = t.Tables[0].Select().Select((DataRow dataRow) =>
                                        {
                                            var list = new HPCheck();

                                            list.ItemID = Convert.ToString(dataRow["ItemID"]);
                                            list.Patch = Convert.ToString(dataRow["Patch"]);
                                            list.table_old = Convert.ToString(dataRow["table_old"]);
                                            list.table_new = Convert.ToString(dataRow["table_new"]);
                                            return list;
                                        });

                                        var b = a.ToArray();
                                        for (int j = 0; j < b.Length; j++)
                                        {
                                            //we don't want to insert duplicates into tbl_allvendors
                                            if (b[j].table_new.ToLower() == "tbl_allvendors")
                                            {
                                                command.CommandText = dataCommands.HPMMTableUpdate_NoMM(b[j].ItemID, b[j].Patch, b[j].table_old);
                                                command.ExecuteNonQuery();
                                            }
                                            else
                                            {
                                                command.CommandText = dataCommands.HPMMTableUpdate(b[j].ItemID, b[j].Patch, b[j].table_new, b[j].table_old);
                                                command.ExecuteNonQuery();
                                            }
                                        }
                                    }

                                }
                                else   //non HP to HP 
                                {

                                    
                                    if (mm.Tables[0].Rows.Count > 0)
                                    {
                                        //var table = mm.Tables[0].Select();
                                        List<string> MMlist = new List<string>();
                                        foreach (DataRow row in mm.Tables[0].Rows)
                                        {
                                            MMlist.Add(row.ItemArray[0].ToString());
                                        }

                                        var x = MMlist;
                                        int index = MMlist.IndexOf("tbl_AllVendors");
                                        //move tbl_AllVendors to back of check list if more than one mm table
                                        if (index != -1 && MMlist.Count() > 1)
                                        {
                                            MMlist.Remove("tbl_AllVendors");
                                            MMlist.Add("tbl_AllVendors");
                                        }

                                        // for (int k = 0; k < table.Length; k++)
                                        for (int k = 0; k < MMlist.Count(); k++)
                                        {
                                            //get hp mm table name
                                            command.CommandText = dataCommands.GetHPMMTable();
                                            adapter.SelectCommand = command;
                                            adapter.Fill(hp_mm);
                                            var table_hp = hp_mm.Tables[0].Select();
                                            var tableArray_hp = table_hp[0].ItemArray;
                                            string hp_mm_table = tableArray_hp[0].ToString();


                                            // var tableArray = table[k].ItemArray;
                                            non_hp_mm = MMlist[k].ToString();
                                           // non_hp_mm = tableArray[0].ToString();
                                            DataSet t = new DataSet();
                                            //get mm table for item patch
                                            command.CommandText = dataCommands.GetNewMMTablePatchItem_to_HP(non_hp_mm, hp_mm_table);
                                            adapter.SelectCommand = command;
                                            adapter.Fill(t);

                                            for (int i = 0; i < t.Tables[0].Rows.Count; i++)
                                            {

                                                var a = t.Tables[0].Select().Select((DataRow dataRow) =>
                                                {
                                                    var list = new HPCheck();

                                                    list.ItemID = Convert.ToString(dataRow["ItemID"]);
                                                    list.Patch = Convert.ToString(dataRow["Patch"]);
                                                    list.table_old = Convert.ToString(dataRow["table_old"]);
                                                    list.table_new = Convert.ToString(dataRow["table_new"]);
                                                    return list;
                                                });

                                                var b = a.ToArray();
                                                for (int j = 0; j < b.Length; j++)
                                                {
                                                    //we don't want to delete records from tbl_allvendors
                                                    if (b[j].table_old.ToLower() == "tbl_allvendors")
                                                    {
                                                        command.CommandText = dataCommands.HPMMTableUpdate_MM(b[j].ItemID, b[j].Patch, b[j].table_new, b[j].table_old);
                                                        command.ExecuteNonQuery();
                                                    }
                                                    else
                                                    {
                                                        command.CommandText = dataCommands.HPMMTableUpdate(b[j].ItemID, b[j].Patch, b[j].table_new, b[j].table_old);
                                                        command.ExecuteNonQuery();
                                                    }
                                                }
                                            }

                                        }


                                    }
                                }
                            }
                        }
                    }

                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            VerticaCommand comm = new VerticaCommand(dataCommands.CreateIOUVendorUpdate(vendorTable), connection);
                            comm.ExecuteNonQuery();
                            return true;
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                var isAnyUnsuccessful = tasks.Where(success => !success.Result).Count() > 0;
                return !isAnyUnsuccessful;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<bool> RunIOUBrandNewItemPatchInsert(VerticaConnection connection, string vendorTable, Boolean newItemsFlag)
        {
            try
            {
                var dataCommands = new DataCommands();

                var buildNewRecordsStage = dataCommands.CreateIOUBrandNewRecordStage(newItemsFlag);
                VerticaCommand command = new VerticaCommand(buildNewRecordsStage, connection);
                command.ExecuteNonQuery();
               
                // Build a list of affected mm tables                 
                var tablesToUpdate = ExpandoUtil.GetExpandoList(dataCommands.GetIOUAffectedMMsNewRecords(), connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();

                // Build a list of affected md tables                 
                var MDtablesToUpdate = ExpandoUtil.GetExpandoList(dataCommands.GetIOUAffectedMDsNewRecords(), connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();

                var tasks = new List<Task<bool>>();

                

                //insert into allvendors table and filters
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        VerticaCommand c = connection.CreateCommand();
                        c.CommandText = dataCommands.CreateIOUBrandNewRecordInsert("tbl_Allvendors");
                        c.ExecuteNonQuery();

                        var mdTasks = new List<Task<bool>>();
                        mdTasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                VerticaCommand comFilter = new VerticaCommand(dataCommands.CreateIOUUpdateFilters("tbl_Allvendors", "tmp_iou_newrecords", false, false, true, false, true), connection);
                                comFilter.ExecuteNonQuery();
                                return true;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }));

                        //MDtablesToUpdate.ForEach(table =>
                        //{
                        //    mdTasks.Add(Task.Run(() =>
                        //    {
                        //        try
                        //        {
                        //            //only update filters not CreateIOUBrandNewRecordInsert_mm
                        //            VerticaCommand cFilter = new VerticaCommand(dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, false, false), connection);
                        //            cFilter.ExecuteNonQuery();
                        //            return true;
                        //        }
                        //        catch (Exception e)
                        //        {
                        //            throw e;
                        //        }
                        //    }));
                        //});

                        await Task.WhenAll(mdTasks);
                        return true;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }));

                //insert into affected mm tables and filters relocated so that tbl_AllVendors will be updated before updating MM view filters
                tablesToUpdate.ForEach(table =>
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            VerticaCommand c = connection.CreateCommand();
                            // Skip uploading records for non-H&P MMs (since only the H&P MM has a table)
                            if (!table.Contains("vw_"))
                            {
                                var recordInsert = dataCommands.CreateIOUBrandNewRecordInsert_mm(table);
                                c.CommandText = recordInsert;
                                c.ExecuteNonQuery();
                            }

                            //// Update filter table
                            //var createFilters = dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, false, false);
                            //VerticaCommand comFilter = new VerticaCommand(createFilters, connection);
                            //comFilter.ExecuteNonQuery();
                            return true;
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }));
                });

                //insert into vendors table, no filters (will update at end of calling proc)
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        VerticaCommand c = connection.CreateCommand();
                        c.CommandText = dataCommands.CreateIOUBrandNewRecordInsert(vendorTable);
                        c.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }));

                await Task.WhenAll(tasks);
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<bool> RunIOURemovalUpdates(VerticaConnection connection, string vendorTable, string batchId, Boolean isFreeze)
        {
            try
            {
                var dataCommands = new DataCommands();

                var removalUpdateStageQuery = dataCommands.CreateIOURemovalUpdateStage(batchId);
                VerticaCommand command = new VerticaCommand(removalUpdateStageQuery, connection);
                command.ExecuteNonQuery();            
               
                //Build a list of mm affected tables
                //mms to update
                //get mm list first, then update all vendors table
                var affectedMMsRemovalQuery = dataCommands.GetIOUAffectedMMs_removal();
                var tablesToUpdate = ExpandoUtil.GetExpandoList(affectedMMsRemovalQuery, connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();

                var affectedMDsRemovalQuery = dataCommands.GetIOUAffectedMDs_removal();
                var MDtablesToUpdate = ExpandoUtil.GetExpandoList(affectedMDsRemovalQuery, connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();

                var removalUpdateQuery = dataCommands.CreateIOURemovalUpdate("tbl_AllVendors", isFreeze);
                VerticaCommand c = new VerticaCommand(removalUpdateQuery, connection);
                c.ExecuteNonQuery();
                VerticaCommand comFilter = new VerticaCommand(dataCommands.CreateIOUUpdateFilters("tbl_allvendors", "tmp_iou_removal_updates", false, false, true, true, true), connection);
                comFilter.ExecuteNonQuery();

                var tasks = new List<Task<bool>>();

                tablesToUpdate.ForEach(table =>
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            if (!table.Contains("vw_"))
                            {
                                var removalUpdateQueryMM = dataCommands.CreateIOURemovalUpdate(table, isFreeze);
                                VerticaCommand com = new VerticaCommand(removalUpdateQueryMM, connection);
                                //filter insert in new filters if not present
                                com.ExecuteNonQuery();
                            }

                            //var removalUpdateFilters = dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, true, false);
                            //VerticaCommand comFilterMM = new VerticaCommand(removalUpdateFilters, connection);
                            //comFilterMM.ExecuteNonQuery();
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }));
                });

                //mms to delete
                var affectedMMsRemovalQuery_HandP = dataCommands.GetIOUAffectedMMs_HandP_deletes();
                var MMTablesToDelete = ExpandoUtil.GetExpandoList(affectedMMsRemovalQuery_HandP, connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();

                MMTablesToDelete.ForEach(table =>
                {
                    if (!table.Contains("vw_"))
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                var removalUpdateQueryMM = dataCommands.DeleteIOURecordFromMM(table);
                                VerticaCommand com = new VerticaCommand(removalUpdateQueryMM, connection);
                                com.ExecuteNonQuery();
                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }));
                    }
                });

                //mms to insert
                var affectedMMsRemovalQuery_HandP_insert = dataCommands.GetIOUAffectedMMs_HandP_inserts();
                var MMTablesToInsert= ExpandoUtil.GetExpandoList(affectedMMsRemovalQuery_HandP_insert, connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();


                MMTablesToInsert.ForEach(table =>
                {

                    tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                if (!table.Contains("vw_"))
                                {
                                    var removalInsertQueryMM = dataCommands.CreateIOURemovalInsert_MM(table);
                                    VerticaCommand com = new VerticaCommand(removalInsertQueryMM, connection);
                                    com.ExecuteNonQuery();
                                }

                                //var removalUpdateFilters_PG = dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, false, false);
                                //VerticaCommand comFilterVendor_PG = new VerticaCommand(removalUpdateFilters_PG, connection);
                                //comFilterVendor_PG.ExecuteNonQuery();
                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }));
                    
                });

                //loop through affected vendor tables
                var affectedVendorsQuery = dataCommands.GetIOUAffectedVendorsRemovalrecords();
                var tablesToInsert = ExpandoUtil.GetExpandoList(affectedVendorsQuery, connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();

                tablesToInsert.ForEach(table =>
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var removalInsertQuery = dataCommands.CreateIOURemovalInsert(table);
                            VerticaCommand commInsert = new VerticaCommand(removalInsertQuery, connection);
                            //filter insert in new filters if not present
                            commInsert.ExecuteNonQuery();
                            var removalUpdateFilters = dataCommands.CreateIOUUpdateFilters(table, "tbl_allvendors", true, false, true, false, false);
                            VerticaCommand comFilterVendor = new VerticaCommand(removalUpdateFilters, connection);
                            comFilterVendor.ExecuteNonQuery();
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }));
                });


                //MDtablesToUpdate.ForEach(table =>
                //{
                //    tasks.Add(Task.Run(() =>
                //    {
                //        try
                //        {
                //            //only update filters
                //            var removalUpdateFilters = dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, true, false);
                //            VerticaCommand comFilterMM = new VerticaCommand(removalUpdateFilters, connection);
                //            comFilterMM.ExecuteNonQuery();
                //            return true;
                //        }
                //        catch (Exception e)
                //        {
                //            return false;
                //        }
                //    }));
                //});

                //tablesToUpdate.ForEach(table =>
                //{
                //    tasks.Add(Task.Run(() =>
                //    {
                //        try
                //        {
                //            //only update filters
                //            var removalUpdateFilters = dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, true, false);
                //            VerticaCommand comFilterMM = new VerticaCommand(removalUpdateFilters, connection);
                //            comFilterMM.ExecuteNonQuery();
                //            return true;
                //        }
                //        catch (Exception e)
                //        {
                //            return false;
                //        }
                //    }));
                //});

                //mds to insert with views
                var affectedMDsRemovalQuery_HandP_insert = dataCommands.GetIOUAffectedMDs_HandP_inserts();
                var MDTablesToInsert = ExpandoUtil.GetExpandoList(affectedMDsRemovalQuery_HandP_insert, connection, true).Select(expandoObj =>
                {
                    expandoObj.TryGetValue("tablename", out string tableName);
                    return tableName;
                }).ToList();


                //MDTablesToInsert.ForEach(table =>
                //{
                //    tasks.Add(Task.Run(() =>
                //    {
                //        try
                //        {
                //            //only need to update filters not remove from table
                //            var removalUpdateFilters_PG = dataCommands.CreateIOUUpdateFilters(table, table, false, false, true, false, false);
                //            VerticaCommand comFilterVendor_PG = new VerticaCommand(removalUpdateFilters_PG, connection);
                //            comFilterVendor_PG.ExecuteNonQuery();
                //            return true;
                //        }
                //        catch (Exception e)
                //        {
                //            return false;
                //        }
                //    }));
                //});

                await Task.WhenAll(tasks);

                var isAnyUnsuccessful = tasks.Where(success => !success.Result).Count() > 0;
                return !isAnyUnsuccessful;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<FileUploadResult> UploadItemPatchOwnership(EditorParameterModel editor, string serverPath, string localFilePath, string fileName)
        {
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var IOUResult = new FileUploadResult();
            var dataSet = new DataSet();
            var stopWatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString();
            try
            {
                var dataCommands = new DataCommands();
                var isPreFreeze = GetToolConfigValue("preFreeze");
                var isFreeze = GetToolConfigValue("freeze");
                var gmsVenId = int.Parse(editor.GMSVenID);
                
                var reverseUserHeaders = new DTHeaderNames().ToReverseDictionary();
                // Header names from the csv file.
                var fileHeaders = CSVHelper.GetHeaders(localFilePath);

                // grab corresponding back-end column name for file headers
                string backendHeaders = "";
                foreach (var h in fileHeaders)
                {
                    reverseUserHeaders.TryGetValue(h, out string header);
                    if (header != null)
                    {
                        backendHeaders += " ," + header;
                    }
                };

                // Start inserting data here and building csv files if any incorrect data existed.
                connection.Open();
                VerticaCommand command = connection.CreateCommand();

                // Stage the file data into a flex table.
                command.CommandText = dataCommands.CreateIOUFileDataStage(gmsVenId, fileName, batchId);
                command.ExecuteNonQuery();

                // Make sure all data validation checks pass.
                var isUploadFileValid = await RunIOUInvalidRecordsCheck(connection, gmsVenId, batchId, backendHeaders);

                // Stage all valid records into the IOU stage table.
                command.CommandText = dataCommands.CreateIOUValidRecordsStage(batchId);
                command.ExecuteNonQuery();

                // Run the uncontested Item/Patch combinations updates.
                var isUncontestedItemPatchSuccessful = await RunIOUUncontestedItemPatchInsert(connection, editor.TableName, isPreFreeze, batchId, false);

                command.CommandText = dataCommands.CreateIOUOverlappingClaimsInserts(batchId, false);
                command.ExecuteNonQuery();
                
                if (!isPreFreeze)
                {
                    // Execute any remove requests from vendor tables
                    command.CommandText = dataCommands.DeleteIOURecordFromVendor(editor.TableName, batchId);
                    command.ExecuteNonQuery();

                    // Run the brand new Item/Patch inserts.
                    var isBrandNewItemPatchSuccessful = await RunIOUBrandNewItemPatchInsert(connection, editor.TableName, false);
                    //filter insert in new item/patch, etc. if not already present
                }

                // Update the item/patch ownership table to set items with no requests to 'No Vendor' or cascade 
                // to next vendor in line.
                var IOUOwnershipCascade = dataCommands.CreateIOUOwnershipTableCascadeOverlap(batchId);
                VerticaCommand com = new VerticaCommand(IOUOwnershipCascade, connection);
                com.ExecuteNonQuery();

                // Update the IOU Overlap table to delete records with no overlaps in ownership requests
                // or assign it to the next vendor in line.
                command.CommandText = dataCommands.CreateIOUOverlapTableDeleteUpdate(batchId);
                command.ExecuteNonQuery();

                if (!isPreFreeze)
                {
                    var isRunIOURemovalUpdatesSuccessful = await RunIOURemovalUpdates(connection, editor.TableName, batchId, isFreeze);

                    //Filter Update
                    command.CommandText = dataCommands.CreateIOUUpdateFiltersScript(editor.TableName);
                    command.ExecuteNonQuery();
                }
                
                // Query any staged invalid records.
                var invalidRecordQuery = dataCommands.GetInvalidIOURecords(batchId);
                var invalidRecords = ExpandoUtil.GetExpandoList(invalidRecordQuery);

                var exports = new Exports.Exports();
                if (invalidRecords.Count > 0)
                {
                    connection.Close();
                    command.Dispose();

                    var splitFile = fileName.Split('_');
                    var originalFileNameErrors = $"{string.Join("_", splitFile.Take(splitFile.Count() - 1))}_Errors.CSV";
                    var filePath = Path.Combine(serverPath, originalFileNameErrors);
                    
                    exports.CreateCsvExport(invalidRecords, filePath);
                    var errorResponse = new FileUploadResult
                    {
                        fileName = originalFileNameErrors, //Set fileName to original instead of path
                        isPreFreeze = isPreFreeze,
                        success = false,
                        message = "Some items could not be processed. Please wait and a file with the error rows will export shortly. Please correct the information and upload again. Any items that aren't in the error rows have been accepted."
                    };

                    UpdateUploadLog(new UploadLog
                    {
                        GmsVenId = int.Parse(editor.GMSVenID),
                        VendorDesc = editor.VendorGroup,
                        FileUploadType = "IOU",
                        FileName = fileName,
                        TimeStamp = Util.GetTimestamp(),
                        Success = true,
                        UserLogin = editor.Username,
                        SuccessOrFailureMessage = $"Invalid rows in file. Will be sent to user. FileName: {fileName}",
                        Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                    });
                    DropIOUPartitionData(editor, fileName, batchId);
                    stopWatch.Stop();
                    return errorResponse;
                }

                connection.Close();
                command.Dispose();

                var successResponse = new FileUploadResult
                {
                    fileName = "",
                    isPreFreeze = isPreFreeze,
                    success = true,
                    message = "Upload success! All Item Patch claims have been processed. Please visit the Exceptions tab to view overlapping claims."
                };
                UpdateUploadLog(new UploadLog
                {
                    GmsVenId = int.Parse(editor.GMSVenID),
                    VendorDesc = editor.VendorGroup,
                    FileUploadType = "IOU",
                    FileName = fileName,
                    TimeStamp = Util.GetTimestamp(),
                    Success = true,
                    UserLogin = editor.Username,
                    SuccessOrFailureMessage = $"Success. All Item/Patch claims processed without errors. FileName: {fileName}",
                    Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                });
                DropIOUPartitionData(editor, fileName, batchId);
                stopWatch.Stop();
                return successResponse;
            }
            catch (Exception e)
            {
                var isPreFreeze = GetToolConfigValue("preFreeze");
                connection.Close();
                var crashResponse = new FileUploadResult
                {
                    fileName = "",
                    isPreFreeze = isPreFreeze,
                    success = false,
                    message = "An error occurred. If you received a file please review it and correct any issues described in the 'Reason' column and try again. If you did not receive a file then please contact support at support@demandlink.com."
                };
                UpdateUploadLog(new UploadLog
                {
                    GmsVenId = int.Parse(editor.GMSVenID),
                    VendorDesc = editor.VendorGroup,
                    FileUploadType = "IOU",
                    FileName = fileName,
                    TimeStamp = Util.GetTimestamp(),
                    Success = false,
                    UserLogin = editor.Username,
                    SuccessOrFailureMessage = $"Exception catch. FileName: {fileName}. Message: {e.Message}.",
                    Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                });
                DropIOUPartitionData(editor, fileName, batchId);
                stopWatch.Stop();
                return crashResponse;
            }
        }

        public async Task<FileUploadResult> UploadNewItems(EditorParameterModel editor, string serverPath, string localFilePath, string fileName)
        {
            var newItemUploadResult = new FileUploadResult();
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var dataSet = new DataSet();
            var stopWatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString();
            var gmsVenId = int.Parse(editor.GMSVenID);

            // Config flags for conditional code
            var isForecastPreFreeze = GetToolConfigValue("preFreeze");

            try
            {
                // A map of back-end and user friendly column names.
                var userHeaders = new DTHeaderNames().ToDictionary();
                var reverseUserHeaders = new DTHeaderNames().ToReverseDictionary();
                // Header names from the csv file.
                var fileHeaders = CSVHelper.GetHeaders(localFilePath);

                // grab corresponding back-end column name for file headers
                string backendHeaders = "";
                foreach (var h in fileHeaders)
                {
                    reverseUserHeaders.TryGetValue(h, out string header);
                    if (header != null) {
                        backendHeaders += " ," + header;
                    }
                };

                // Start inserting data here and building csv files if any incorrect data existed.
                connection.Open();
                VerticaCommand command = connection.CreateCommand();

                // Stage items ready for inserting
                var dataCommands = new DataCommands();
                command.CommandText = dataCommands.CreateNewItemsUploadStage(editor, fileName, batchId, backendHeaders, isForecastPreFreeze);
                adapter.SelectCommand = command;
                adapter.Fill(dataSet);
                var tables = dataSet.Tables;

               

                /* Update ownership tables*/

                // Update data tables if !prefreeze if an existing record exists for "no vendor"
                var isUncontestedItemPatchSuccessful = await RunIOUUncontestedItemPatchInsert(connection, editor.TableName, isForecastPreFreeze, batchId, true);

             

                //find any existing claims, and insert to overlap table
                command.CommandText = dataCommands.CreateIOUOverlappingClaimsInserts(batchId, true);
                command.ExecuteNonQuery();

                // Insert new item rows into all affected tables.
                if (!isForecastPreFreeze)
                {
                    var insertRowCount = Convert.ToInt32(tables[1].Rows[0]["RowCount"]);
                    if (insertRowCount > 0)
                    {
                        // Run the brand new Item/Patch inserts.
                        var isBrandNewItemPatchSuccessful = await RunIOUBrandNewItemPatchInsert(connection, editor.TableName, true); 
                        //filter insert in new item/patch, etc. if not already present

                        var isSuccess = tables[0].Rows.Count == 0 && tables[1].Rows.Count == 0;
                        UpdateUploadLog(new UploadLog
                        {
                            GmsVenId = int.Parse(editor.GMSVenID),
                            VendorDesc = editor.VendorGroup,
                            FileUploadType = "New Items Upload",
                            FileName = fileName,
                            TimeStamp = Util.GetTimestamp(),
                            Success = true,
                            UserLogin = editor.Username,
                            SuccessOrFailureMessage = isSuccess ? "All items inserted" : "Some items inserted",
                            Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                        });
                    }
                }

                //Filter Update
                command.CommandText = dataCommands.CreateIOUUpdateFiltersScript(editor.TableName);
                command.ExecuteNonQuery();

                // Get all invalid rows and build a csv report.
                var exports = new Exports.Exports();
                if (tables[0].Rows.Count > 0)
                {
                    var userInputErrorData = dataSet.Tables[0].Select().Select((DataRow dataRow) =>
                    {
                        var error = new NewItemUpload();

                        error.ItemID = Convert.ToString(dataRow["ItemID"]);
                        error.ProdGrpID = Convert.ToString(dataRow["ProdGrpID"]);
                        error.ParentID = Convert.ToString(dataRow["ParentID"]);
                        error.AssrtID = Convert.ToString(dataRow["AssrtID"]);
                        error.Patch = Convert.ToString(dataRow["Patch"]);
                        error.ItemDesc = Convert.ToString(dataRow["ItemDesc"]);
                        error.PrimaryVendor = Convert.ToString(dataRow["PrimaryVendor"]);
                        error.Error = Convert.ToString(dataRow["Error"]);

                        return error;
                    });

                    var splitFile = fileName.Split('_');
                    var originalFileNameErrors = $"{string.Join("_", splitFile.Take(splitFile.Count() - 1))}_Errors.CSV";
                    var filePath = Path.Combine(serverPath, originalFileNameErrors);

                    exports.CreateCsvExport(userInputErrorData.ToList(), filePath);
                    var elapsedTime = Util.GetTime(stopWatch.ElapsedMilliseconds);
                    var timeStamp = Util.GetTimestamp();
                    UpdateUploadLog(new UploadLog
                    {
                        GmsVenId = int.Parse(editor.GMSVenID),
                        VendorDesc = editor.VendorGroup,
                        FileUploadType = "New Items Upload",
                        FileName = fileName,
                        TimeStamp = timeStamp,
                        Success = false,
                        UserLogin = editor.Username,
                        SuccessOrFailureMessage = string.Format($"User input data error. File name: {fileName}. Please view the file or run on dev to view incorrect data."),
                        Duration = elapsedTime
                    });

                    newItemUploadResult.fileName = originalFileNameErrors;//filePath;
                    newItemUploadResult.isPreFreeze = isForecastPreFreeze;
                    newItemUploadResult.success = false;
                    newItemUploadResult.message = "Some items could not be processed. Please wait and a file with the error rows will export shortly. Please correct the information and upload again. Any items that aren't in the error rows have been accepted.";
                }
                else
                {
                    newItemUploadResult.fileName = "";
                    newItemUploadResult.isPreFreeze = isForecastPreFreeze;
                    newItemUploadResult.success = true;
                    newItemUploadResult.message = "Succesfully uploaded all items! Please visit the Exceptions tab to view overlapping claims.";
                    UpdateUploadLog(new UploadLog
                    {
                        GmsVenId = int.Parse(editor.GMSVenID),
                        VendorDesc = editor.VendorGroup,
                        FileUploadType = "New Items Upload",
                        FileName = fileName,
                        TimeStamp = Util.GetTimestamp(),
                        Success = true,
                        UserLogin = editor.Username,
                        SuccessOrFailureMessage = $"Success loading new items from file: {fileName}",
                        Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                    });
                }
                
                DropNewItemUploadPartition(batchId);
                connection.Close();
            }
            catch (Exception e)
            {
                connection.Close();
                newItemUploadResult.success = false;
                newItemUploadResult.isPreFreeze = isForecastPreFreeze;
                newItemUploadResult.fileName = string.Empty;
                newItemUploadResult.message = "An error occured that could not be handled by our system. Please verify you have the correct template and data is in correct format and try again.";
                UpdateUploadLog(new UploadLog
                {
                    GmsVenId = int.Parse(editor.GMSVenID),
                    VendorDesc = editor.VendorGroup,
                    FileUploadType = "New Items Upload",
                    FileName = fileName,
                    TimeStamp = Util.GetTimestamp(),
                    Success = false,
                    UserLogin = editor.Username,
                    SuccessOrFailureMessage = $"Fatal error. Exception message: {e.Message}",
                    Duration = Util.GetTime(stopWatch.ElapsedMilliseconds)
                });
                DropNewItemUploadPartition(batchId);
                stopWatch.Stop();
                return newItemUploadResult;
            }

            stopWatch.Stop();
            return newItemUploadResult;
        }

        public void UpdateUploadLog(UploadLog uploadLog)
        {
            var sql = new DataCommands().UpdateUploadLog(uploadLog);
            Util.ExecuteNonQuery(sql);
        }

        #endregion IMPORT

        #region Utils

        /// <summary>
        /// Adds a property name to the <seealso cref="ExpandoObject"/> with a value.
        /// </summary>
        /// <param name="expando"> The <seealso cref="ExpandoObject"/> that you want to add a property name to.</param>
        /// <param name="propertyName"> A <seealso cref="string"/> name that will act act as the property name.</param>
        /// <param name="propertyValue"> A <seealso cref="object"/> that will be added as a value.</param>
        public static void CreatExpandoPropertyWithVal(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            // so the Key will act as the propertie/column and the Value will act as the 
            // propertie/column value
            var expandoDict = expando as IDictionary<string, object>;

            // If the current expando doesn't have the propertyName then add it with the value
            if (!expandoDict.ContainsKey(propertyName))
                expandoDict.Add(propertyName, propertyValue);
            else // Otherwise just assign the value
                expandoDict[propertyName] = propertyValue;
        }

        public string GetRowsAsCsvFromCsv(string filePath, string newFileName, params int[] indexes)
        {
            var rowsToFind = new List<string>();
            using (var reader = new StreamReader(filePath))
            {
                for (int i = 0; !reader.EndOfStream; i++)
                {
                    var line = reader.ReadLine();
                    if (i == 0)
                    {
                        rowsToFind.Add(line);
                    } else if (indexes.Contains(i))
                    {
                        rowsToFind.Add(line);
                    }
                }
            }

            try
            {
                using (var sw = new StreamWriter(newFileName))
                {
                    sw.WriteLine(string.Join(",", rowsToFind[0].Split(',')));

                    //Here we get the next 100000 rows from the data table into a new file
                    //We also make sure that we don't go past the row count in the datatable
                    for (var i = 1; i < rowsToFind.Count(); i++)
                    {
                        sw.Write($"{rowsToFind[i]}\n");
                    }

                    sw.Flush();
                }
            }
            catch (Exception e)
            {
                var source = $"There was an error in CreatExpandoPropertyWithVal writing to file path: {filePath}";
                throw e;
            }

            return newFileName;
        }

        private List<object> GetListFromVertica(string str, SingleParamFunc func, MapToObject mapToObject)
        {
            var result = new List<object>();
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);
            var dataSet = new DataSet();

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = func(str);
                adapter.SelectCommand = command;
                adapter.Fill(dataSet);
                
                return mapToObject(dataSet);
            }
            catch (Exception)
            {

            }

            return result;
        }

        public static bool IsRotatedOn(EditorParameterModel editor, string columnName)
        {
            var column = editor.Rotator.First(r =>
            {
                return string.Equals(r.Column, columnName, StringComparison.InvariantCultureIgnoreCase);
            });

            if (column == null)
            {
                return false;
            }

            return column.Included;
        }

        public static bool IsVendor(EditorParameterModel editor)
        {
            return !editor.IsMD
                && !editor.IsMM
                && !string.Equals(editor.TableName, "tbl_AllVendors", StringComparison.CurrentCultureIgnoreCase);
        }

        public List<object> MapTutorialGroup(DataSet ds)
        {
            var results = new List<object>();

            for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                results.Add(ds.Tables[0].Rows[i]["TutorialGroup"] is DBNull ? "" : Convert.ToString(ds.Tables[0].Rows[i]["TutorialGroup"]));
            }

            return results;
        }

        /// <summary>
        /// The maximum rows a file can have for and export
        /// </summary>
        public int MaxRowsPerFile { get; } = 100000;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="purpose"></param>
		/// <returns></returns>
		public static string Protect(string text, string purpose)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			byte[] stream = Encoding.UTF8.GetBytes(text);
			byte[] encodedValue = System.Web.Security.MachineKey.Protect(stream, purpose);
			return HttpServerUtility.UrlTokenEncode(encodedValue);
		}

        public enum TableUpdateType
        {
            ERetailPrice,
            ESalesU,
            ESalesUVar,
            EMMComments,
            EVendorComments
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="purpose"></param>
		/// <returns></returns>
		public static string Unprotect(string text, string purpose)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			byte[] stream = HttpServerUtility.UrlTokenDecode(text);
			byte[] decodedValue = System.Web.Security.MachineKey.Unprotect(stream, purpose);
			return Encoding.UTF8.GetString(decodedValue);
		}

		#endregion Utils
	}
}
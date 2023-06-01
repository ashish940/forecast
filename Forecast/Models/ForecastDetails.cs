using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Forecast.Models
{
    public class ForecastDetails
    {
        //public int GMSVenID {get; set;}
        public string ForecastID { get; set; }
        public string VendorDesc { get; set; }
        public long ItemID { get; set; }
        public string ItemDesc { get; set; }
        public string ItemConcat { get; set; }
        public int FiscalWk { get; set; }
        public int FiscalMo { get; set; }
        public int FiscalQtr { get; set; }
        public string MD { get; set; }
        public string MM { get; set; }
        public string Region { get; set; }
        public string District { get; set; }
        public string Patch { get; set; }
        public string ParentID { get; set; }
        public string ParentDesc { get; set; }
        public string ParentConcat { get; set; }
        public int ProdGrpID { get; set; }
        public string ProdGrpDesc { get; set; }
        public string ProdGrpConcat { get; set; }
        public int AssrtID { get; set; }
        public string AssrtDesc { get; set; }
        public string AssrtConcat { get; set; }
        public int SalesUnits_TY { get; set; }
        public int SalesUnits_LY { get; set; }
        public int SalesUnits_2LY { get; set; }
        public int SalesUnits_FC { get; set; }
        public decimal SalesUnits_Var { get; set; }
        public decimal SalesDollars_TY { get; set; }
        public decimal SalesDollars_LY { get; set; }
        public decimal SalesDollars_2LY { get; set; }
        public decimal SalesDollars_FR_FC { get; set; }
        public decimal SalesDollars_Curr { get; set; }
        public decimal SalesDollars_Var { get; set; }
        public decimal CAGR { get; set; }
        public decimal Asp_TY { get; set; }
        public decimal Asp_LY { get; set; }
        public decimal Asp_FC { get; set; }
        public decimal Asp_Var { get; set; }
        public decimal RetailPrice_TY { get; set; }
        public decimal RetailPrice_LY { get; set; }
        public decimal RetailPrice_FC { get; set; }
        public decimal RetailPrice_Var { get; set; }
        public decimal RetailPrice_Erosion_Rate { get; set; }
        public decimal SalesDollars_FR_TY { get; set; }
        public decimal SalesDollars_FR_LY { get; set; }
        public decimal MarginDollars_FR_TY { get; set; }
        public decimal MarginDollars_FR_LY { get; set; }
        public decimal MarginDollars_FR_Var { get; set; }
        public decimal Cost_TY { get; set; }
        public decimal Cost_LY { get; set; }
        public decimal Cost_FC { get; set; }
        public decimal Cost_Var { get; set; }
        public decimal Margin_Dollars_TY { get; set; }
        public decimal Margin_Dollars_LY { get; set; }
        public decimal Margin_Dollars_Curr { get; set; }
        public decimal Margin_Dollars_FR { get; set; }
        public decimal Margin_Dollars_Var_Curr { get; set; }
        public decimal Margin_Percent_TY { get; set; }
        public decimal Margin_Percent_LY { get; set; }
        public decimal Margin_Percent_Curr { get; set; }
        public decimal Margin_Percent_FR { get; set; }
        public decimal Margin_Percent_Var { get; set; }
        public decimal Turns_TY { get; set; }
        public decimal Turns_LY { get; set; }
        public decimal Turns_FC { get; set; }
        public decimal Turns_Var { get; set; }
        public decimal SellThru_TY { get; set; }
        public decimal SellThru_LY { get; set; }
        public decimal Dollars_FC_DL { get; set; }
        public decimal Dollars_FC_LOW { get; set; }
        public decimal Dollars_FC_Vendor { get; set; }
        public int Units_FC_DL { get; set; }
        public int Units_FC_LOW { get; set; }
        public int Units_FC_Vendor { get; set; }
        public decimal Dollars_FC_DL_Var { get; set; }
        public decimal Dollars_FC_LOW_Var { get; set; }
        public decimal Dollars_FC_Vendor_Var { get; set; }
        public decimal Units_FC_DL_Var { get; set; }
        public decimal Units_FC_LOW_Var { get; set; }
        public decimal Units_FC_Vendor_Var { get; set; }
        public int ReceiptUnits_TY { get; set; }
        public int ReceiptUnits_LY { get; set; }
        public decimal ReceiptDollars_TY { get; set; }
        public decimal ReceiptDollars_LY { get; set; }
        public string PriceSensitivityImpact { get; set; }
        public string PriceSensitivityPercent { get; set; }
        //public decimal VBUPercent { get; set; }
        public string MM_Comments { get; set; }
        public string Vendor_Comments { get; set; }
    }

    public class AdminTempList
    {
        public int NotifType { get; set; }
        public int NotifTypeId { get; set; }
        public string Title { get; set; }
    }

    public class NewItems
    {
        public long ItemID { get; set; }
        public string Patch { get; set; }
        public string MM { get; set; }
    }

    public class ConfigNotification
    {
        public int NCID { get; set; }
        public string NotifTypeName { get; set; }
    }

    public class DlEvent
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        [AllowHtml]
        public string Body { get; set; }
        public string FileId { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string LastEdit { get; set; }
        public string Target { get; set; }
    }

    public class DlEventContainer
    {
        public DlEvent DlEvent { get; set; }
        public HttpPostedFileBase EventFile { get; set; }
    }

    public class DTHeaderNames
    {
        private Hashtable headerNames = new Hashtable();

        public DTHeaderNames()
        {
            Init();
        }

        private void Init()
        {
            headerNames["ForecastID"] = "Forecast ID";
            headerNames["GMSVenID"] = "Vendor ID";
            headerNames["VendorDesc"] = "Vendor";
            headerNames["ItemID"] = "Item";
            headerNames["ItemDesc"] = "Item Desc";
            headerNames["ItemConcat"] = "Item & Desc";
            headerNames["FiscalWk"] = "Fiscal Wk";
            headerNames["FiscalMo"] = "Fiscal Mo";
            headerNames["FiscalQtr"] = "Fiscal Qtr";
            headerNames["MD"] = "SRLGM";
            headerNames["MM"] = "LGM";
            headerNames["Region"] = "Region";
            headerNames["District"] = "District";
            headerNames["Patch"] = "Patch";
            headerNames["ParentID"] = "Parent";
            headerNames["ParentDesc"] = "Parent Desc";
            headerNames["ParentConcat"] = "Parent & Desc";
            headerNames["ProdGrpID"] = "ProdGrp";
            headerNames["ProdGrpDesc"] = "ProdGrp Desc";
            headerNames["ProdGrpConcat"] = "ProdGrp & Desc";
            headerNames["AssrtID"] = "Assortment";
            headerNames["AssrtDesc"] = "Assortment Description";
            headerNames["AssrtConcat"] = "Assortment & Description";
            headerNames["VBUPercent"] = "VBU %";
            headerNames["VBU"] = "VBU";
            headerNames["SalesDollars_2LY"] = "Sales Dollars Prev52-2 YR Ago";
            headerNames["SalesDollars_LY"] = "Sales Dollars Prev52-1 YR Ago";
            headerNames["SalesDollars_TY"] = "Sales Dollars Prev52";
            headerNames["SalesDollars_Curr"] = "Sales Dollars FY ASP $";
            headerNames["SalesDollars_Var"] = "Sales Dollars Var";
            headerNames["SalesDollars_FR_FC"] = "Sales Dollars FR";
            headerNames["CAGR"] = "Sales Dollars CAGR";
            headerNames["Turns_LY"] = "Turns Prev52-1 YR Ago";
            headerNames["Turns_TY"] = "Turns Prev52";
            headerNames["Turns_FC"] = "Turns FY";
            headerNames["Turns_Var"] = "Turns Var";
            headerNames["SalesUnits_2LY"] = "Sales Units Prev52-2 YR Ago";
            headerNames["SalesUnits_LY"] = "Sales Units Prev52-1 YR Ago";
            headerNames["SalesUnits_TY"] = "Sales Units Prev52";
            headerNames["SalesUnits_FC"] = "Sales Units FY";
            headerNames["SalesUnits_Var"] = "Sales Units Var";
            headerNames["RetailPrice_LY"] = "Retail Price Prev52-1 YR Ago";
            headerNames["RetailPrice_TY"] = "Retail Price Prev52";
            headerNames["RetailPrice_FC"] = "Retail Price FY";
            headerNames["RetailPrice_Var"] = "Retail Price Var";
            headerNames["RetailPrice_Erosion_Rate"] = "Retail Erosion Rate";
            headerNames["SalesDollars_FR_TY"] = "MP Sales $ Retail Prev52";
            headerNames["SalesDollars_FR_LY"] = "MP Sales $ Retail Prev52-1 YR Ago";
            headerNames["MarginDollars_FR_TY"] = "MP Margin $ Retail Prev52";
            headerNames["MarginDollars_FR_LY"] = "MP Margin $ Retail Prev52-1 YR Ago";
            headerNames["MarginDollars_FR_Var"] = "MP Margin Var Retail";
            headerNames["PriceSensitivityPercent"] = "Price Sensitivity Percent";
            headerNames["PriceSensitivityImpact"] = "Price Sensitivity Impact Sensitivity";
            headerNames["Asp_LY"] = "ASP Prev52-1 YR Ago";
            headerNames["Asp_TY"] = "ASP Prev52";
            headerNames["Asp_FC"] = "ASP FY ASP";
            headerNames["Asp_Var"] = "ASP Var";
            headerNames["Margin_Percent_LY"] = "Margin % Prev52-1 YR Ago";
            headerNames["Margin_Percent_TY"] = "Margin % Prev52";
            headerNames["Margin_Percent_Curr"] = "Margin % FY ASP $";
            headerNames["Margin_Percent_Var"] = "Margin % Var";
            headerNames["Margin_Percent_FR"] = "Margin % FY Retail $";
            headerNames["Margin_Dollars_LY"] = "Margin $ Prev52-1 YR Ago";
            headerNames["Margin_Dollars_TY"] = "Margin $ Prev52";
            headerNames["Margin_Dollars_Var_Curr"] = "Margin $ FY Retail $ Var";
            headerNames["Margin_Dollars_Curr"] = "Margin $ FY ASP $";
            headerNames["Margin_Dollars_FR"] = "Margin $ FY Retail $";
            headerNames["SellThru_LY"] = "Sell Thru Prev52-1 YR Ago";
            headerNames["SellThru_TY"] = "Sell Thru Prev52";
            headerNames["ReceiptDollars_LY"] = "Receipt Dollars Prev52-1 YR Ago";
            headerNames["ReceiptDollars_TY"] = "Receipt Dollars Prev52";
            headerNames["ReceiptUnits_LY"] = "Receipt Units Prev52-1 YR Ago";
            headerNames["ReceiptUnits_TY"] = "Receipt Units Prev52";
            headerNames["Dollars_FC_DL"] = "DemandLink (FY Sales $)";
            headerNames["Dollars_FC_LOW"] = "Lowes (FY Sales $)";
            headerNames["Dollars_FC_Vendor"] = "Vendor (FY Sales $)";
            headerNames["Units_FC_DL"] = "DemandLink (FY Units)";
            headerNames["Units_FC_LOW"] = "Lowes (FY Units)";
            headerNames["Units_FC_Vendor"] = "Vendor (FY Units)";
            headerNames["Dollars_FC_DL_Var"] = "DemandLink (FY Sales $) Var";
            headerNames["Dollars_FC_LOW_Var"] = "Lowes (FY Sales $) Var";
            headerNames["Dollars_FC_Vendor_Var"] = "Vendor (FY Sales $) Var";
            headerNames["Units_FC_DL_Var"] = "DemandLink (FY Units) Var";
            headerNames["Units_FC_LOW_Var"] = "Lowes (FY Units) Var";
            headerNames["Units_FC_Vendor_Var"] = "Vendor (FY Units) Var";
            headerNames["Cost_LY"] = "Cost Prev52-1 YR Ago";
            headerNames["Cost_TY"] = "Cost Prev52";
            headerNames["Cost_FC"] = "Cost FY";
            headerNames["Cost_Var"] = "Cost Var";
            headerNames["MM_Comments"] = "LGM Comments";
            headerNames["Vendor_Comments"] = "Vendor Comments";
            headerNames["ShipsGross_LY"] = "Gross Ships Prev52-1 YR Ago";
            headerNames["ShipsGross_TY"] = "Gross Ships Prev52";
            headerNames["PriceSensitivity"] = "Price Sensitivity";
            headerNames["OHC_LY"] = "On Hand Units Prev52-1 YR Ago";
            headerNames["OHC_TY"] = "On Hand Units Prev52";
            headerNames["OHU_FC"] = "On Hand Units FY";
            headerNames["Action"] = "Action";
            headerNames["Error"] = "Error";
            headerNames["Reason"] = "Reason";
            headerNames["PrimaryVendor"] = "Primary Vendor";
            headerNames["RequestingOwners"] = "RequestingOwners";
            headerNames["PrimaryVendor"] = "Primary Vendor";
        }

        /// <summary>
        /// Method that returns the Forecast DataTables header name that corresponds to the back-end 
        /// column name.
        /// </summary>
        /// <param name="beColName">A string that matches a column name from the back-end table.</param>
        /// <returns>A string that represents a column name in the front-end DataTable.</returns>
        public string GetDTHeaderName(string beColName)
        {
            var header = headerNames.ContainsKey(beColName) ? headerNames[beColName].ToString() : null;
            return header;
        }

        public IDictionary<string, string> ToDictionary()
        {
            return headerNames.Cast<DictionaryEntry>().ToDictionary(kvp => (string)kvp.Key, kvp => (string)kvp.Value);
        }

        public IDictionary<string, string> ToReverseDictionary()
        {
            return headerNames.Cast<DictionaryEntry>().ToDictionary(kvp => (string)kvp.Value, kvp => (string)kvp.Key);
        }
    }

    public class ExportInfo
    {
        /// <summary>
        /// This holds the column names that will be used to build the temp table.
        /// </summary>
        public string ColumnsToBuild { get; set; }

        /// <summary>
        /// This holds the column names that will be used in the select statement.
        /// </summary>
        public IDictionary<string, string> ColumnsToSelect { get; set; }

        /// <summary>
        /// This holds the file name that will be used when building a file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Function to get the dataset from the back-end. This is for the RunExportNoTempTable function.
        /// When using that function you only need to set this GetData delegate function and the file name.
        /// </summary>
        public Util.GetExportDataSet GetData { get; set; }

        /// <summary>
        /// This holds an ORDER BY columns for the current export
        /// </summary>
        public string OrderByColumns { get; set; }

        /// <summary>
        /// This holds the SQL statement that will be used to query the temp table.
        /// </summary>
        public string Select { get; set; }
    }

    public class ExportTemplateInfo
    {
        public List<string> ColumnNames { get; set; }
        public string FileName { get; set; }
    }

    public class GoogleDriveFile
    {
        public string Id { get; set; }
        public string FileExtension { get; set; }
        public string Name { get; set; }
        public string OriginalFileName { get; set; }
        public string WebViewLink { get; set; }
    }

    public class ImportProcess
    {
        public int GMSVenID { get; set; }
        public int ProcessId { get; set; }
        public string FileName { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class NewItemUpload
    {
        public string ItemID { get; set; }
        public string ItemDesc { get; set; }
        public string Patch { get; set; }
        public string ProdGrpID { get; set; }
        public string ParentID { get; set; }
        public string AssrtID { get; set; }
        public string PrimaryVendor { get; set; }
        public string Error { get; set; }
    }


    public class HPCheck
    {
        public string ItemID { get; set; }
        public string Patch { get; set; }
        public string table_new { get; set; }
        public string table_old { get; set; }

    }

    public class FileUploadResult
    {
        public string fileName { get; set; }
        public string message { get; set; }
        public bool success { get; set; }
        public bool isPreFreeze { get; set; }
    }

    public class Notification
    {
        public int NotifId { get; set; }
        public string Title { get; set; }
        public string Target { get; set; }
        public string Body { get; set; }
        public string NotificationType { get; set; }
        public int NotificationTypeId { get; set; }
        public string TableName { get; set; }
        public int GMSVenID { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool Edited { get; set; }
        public string LastEdit { get; set; }
    }

    public class NotifiedUser
    {
        public int UNID { get; set; }
        public int GMSVenID { get; set; }
        public string UserName { get; set; }
        public int NotificationId { get; set; }
        public string LastEdit { get; set; }
        public string TimeStamp { get; set; }
    }

    public class Tutorial
    {
        public int TutorialId { get; set; }
        public string Title { get; set; }

        [AllowHtml]
        public string Intro { get; set; }
        public string TutorialGroup { get; set; }
        public string VideoLink { get; set; }
        public string LastEdit { get; set; }
    }

    public class UploadLog
    {
        public int GmsVenId { get; set; }
        public string VendorDesc { get; set; }
        public string FileUploadType { get; set; }
        public string FileName { get; set; }
        public string TimeStamp { get; set; }
        public bool Success { get; set; }
        public string UserLogin { get; set; }
        public string SuccessOrFailureMessage { get; set; }
        public string Duration { get; set; }
    }

    public class UserInfo
    {
        public int GMSVenId { get; set; }
        public string UserName { get; set; }
        public string TableName { get; set; }
        public string Email { get; set; }
    }

    public class ViewedNotification
    {
        public List<int> NotifIds { get; set; }
        public string TimeStamp { get; set; }
    }
}

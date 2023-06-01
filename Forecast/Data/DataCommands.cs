using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using Forecast.Models;

///-----------------------------------------------------------------
///   Namespace:        Forecast.Data
///   Class:            DataCommands
///   Description:      This is a public class designed to format the query strings
///                     for DataProvider.cs. Adapted from Joe Mccartys godly Replenit work.
///   Author:           Anthony Castillo                 Date: 11/20/2017
///   Contributor(s):   
///   Revision History: https://bitbucket.org/demandlinkdevelopment/replenit/commits/all
///-----------------------------------------------------------------

namespace Forecast.Data
{
    public class DataCommands
    {
        // Append dev string to scope correctly for development purposes.  Empty string for production.
        private static string _dev = ConfigurationManager.AppSettings.Get("Vertica_DB_Schema");
        private readonly string toolName = "forecast";

        #region SELECT

        // Get the database context. Either development or production
        public string GetDatabaseContext()
        {
            return _dev;
        }

        public static string GetDBContext()
        {
            return _dev;
        }

        //Fetch item desc for bulk filter
        public string GetItemDesc(string[] s, string t)
        {
            //get all itemIDs from array and add them to OR
            string OR = string.Empty;
            for (int i = 0; i < s.Length; i++)
            {
                OR = OR + s[i] + ",";
            }
            OR = OR.Remove(OR.Length - 1, 1) + ")";
            string cmd = string.Empty;
            cmd = string.Format(@"
                       SELECT DISTINCT ItemConcat as Filter
                       FROM Forecast{0}.{1}
                       WHERE ItemID in ( " + OR
          , _dev, t, s[0]);

            return cmd;
        }

        //Fetch the filters for a given column.
        public string GetFilterData(DTParameterModel param, string type, string search)
        {
            string cmd = string.Empty;
          
            cmd = string.Format(@"
            SELECT FilterValue as Filter
            FROM Forecast{0}.filters_{1}
            WHERE FilterType = $${2}$$ AND TO_CHAR(FilterValue) ILIKE $$%{3}%$$
            ORDER BY FilterValue ASC
        ", _dev, param.TableName, type, search);
           

            return cmd;
        }

        public string GetAdminTempListTable(int id)
        {
            var cmd = string.Format(@"
                SELECT TableName FROM Notifications{0}.Config_Notifications WHERE NCID = {1}; ", _dev, id);

            return cmd;
        }

        public string GetAdminTempList(object tableName)
        {
            var cmd = string.Format(@"
                SELECT ID AS NotifTypeID, Title FROM Notifications{0}.{1};", _dev, tableName);

            return cmd;
        }

        public string GetDlEvent(int eventId)
        {
            var cmd = string.Format(@"
                SELECT * FROM Notifications{0}.tbl_DL_Events
                WHERE ID = {1};", _dev, eventId);

            return cmd;
        }

        public string GetDlEvents()
        {
            var cmd = string.Format(@"
                SELECT * FROM Notifications{0}.tbl_DL_Events
                WHERE Target = $${1}$$ OR Target = $${2}$$;",
                _dev,
                toolName,
                "all");

            return cmd;
        }

        public string GetToolConfigValue(string flagName)
        {
            var cmd = string.Format(@"SELECT FlagValue FROM Forecast{0}.config_tool WHERE flagName = $${1}$$;", _dev, flagName);
            return cmd;
        }

        public string GetTutorialGroups(string s)
        {
            var cmd = string.Format(@"
                SELECT DISTINCT TutorialGroup FROM Notifications{0}.tbl_Tutorials;
            ", _dev);

            return cmd;
        }

        public string GetTutorials()
        {
            var cmd = string.Format(@"
                SELECT * FROM Notifications{0}.tbl_Tutorials;", _dev);

            return cmd;
        }

        // Fetch default table.  Table user sees upon login.
        public string GetForecastTable(DTParameterModel param)
        {
            string cmd = string.Empty;
            string select = CreateSelectStatement(param);
            string where = CreateWhereClause(param);
            string order = CreateOrderByClause(param);
            string groupby = CreateGroupByStatement(param);
            cmd = string.Format(@"
                SELECT {6}
					MAX(ParentDesc) AS ParentDesc,
					MAX(ParentConcat) AS ParentConcat,
                    MAX(ItemDesc) AS ItemDesc,
                    MAX(ItemConcat) AS ItemConcat,
                    MAX(AssrtDesc) AS AssrtDesc,
                    MAX(AssrtConcat) AS AssrtConcat,
                    MAX(ProdGrpDesc) AS ProdGrpDesc,
                    MAX(ProdGrpConcat) AS ProdGrpConcat,
                    MAX(ParentDesc) as ParentDesc,
                    SUM(SalesUnits_TY)  AS SalesUnits_TY,
                    SUM(SalesUnits_LY)  AS SalesUnits_LY,
                    SUM(SalesUnits_2ly) AS SalesUnits_2ly,
                    SUM(SalesUnits_FC)  AS SalesUnits_FC,
                    (((SUM(coalesce(SalesUnits_FC,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0))*100)::NUMERIC(18,1) AS SalesUnits_Var,
                    SUM(SalesDollars_TY)    AS SalesDollars_TY,
                    SUM(SalesDollars_LY)    AS SalesDollars_LY,
                    SUM(SalesDollars_2ly)   AS SalesDollars_2ly,
                    SUM(SalesDollars_FR_FC) AS SalesDollars_FR_FC,
                    SUM(SalesDollars_Curr)  AS SalesDollars_Curr,
                    (((SUM(coalesce(SalesDollars_Curr,0)) -SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0))*100)::NUMERIC(18,1) AS SalesDollars_Var,
                    ((((SUM(SalesDollars_TY)/NULLIF(SUM(SalesDollars_2ly),0))^(1/2))-1)*100)::!NUMERIC(18,1) AS CAGR,
                    (SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2) AS ASP_TY,
                    (SUM(SalesDollars_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) AS ASP_LY,
					CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END AS ASP_FC,
                    (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END-(SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
                    /NULLIF((SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS ASP_Var,
                    CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(RetailPrice_TY,0))::NUMERIC(18,2)
                    ELSE (SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                    END AS RetailPrice_TY,
                    CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(RetailPrice_LY,0))::NUMERIC(18,2)
                    ELSE (SUM(SalesDollars_FR_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                    END AS RetailPrice_LY,		                  
                    CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2)
					END AS RetailPrice_FC,
		            (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN coalesce(AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2),0)
					ELSE coalesce(SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0),0)::NUMERIC(18,2)
					END-(SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
					/NULLIF((SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS RetailPrice_Var,
                    CASE WHEN SUM(SalesDollars_TY) <= 0 THEN 0 ELSE (COALESCE((sum(SalesDollars_FR_TY)- sum(SalesDollars_TY)) /nullif(sum(SalesDollars_FR_TY),0),0)*100)::NUMERIC(18,1) END AS RetailPrice_Erosion_Rate,
				    SUM(SalesDollars_FR_TY)::NUMERIC(18,2)    AS SalesDollars_FR_TY,
				    SUM(SalesDollars_FR_LY)::NUMERIC(18,2)    AS SalesDollars_FR_LY,
                    SUM(Margin_Dollars_FR_TY) AS MarginDollars_FR_TY,
                    SUM(Margin_Dollars_FR_LY) AS MarginDollars_FR_LY,
                    (((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_FR_TY))/NULLIF(SUM(Margin_Dollars_FR_TY),0))*100)::NUMERIC(18,1) AS MarginDollars_FR_Var,
                    CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                    ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                    END AS Cost_TY,
                    CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(Cost_LY,0))::NUMERIC(18,2)
                    ELSE (SUM(COGS_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2)
                    END AS Cost_LY,
                    CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
					ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END AS Cost_FC,
                    (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
					ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END - CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                               ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END)
                        / NULLIF(CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                                      ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END,0))
                        *100)::NUMERIC(18,1) AS Cost_Var,
                    SUM(Margin_Dollars_TY)         AS Margin_Dollars_TY,
                    SUM(Margin_Dollars_LY)         AS Margin_Dollars_LY,
                    SUM(Margin_Dollars_Curr)       AS Margin_Dollars_Curr,
                    SUM(Margin_Dollars_FR)         AS Margin_Dollars_FR,
                    (((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_TY))/NULLIF(SUM(Margin_Dollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Dollars_Var_Curr,
                    (((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Percent_TY,
                    (((SUM(SalesDollars_LY)-SUM(coalesce(COGS_LY,0)))/NULLIF(SUM(SalesDollars_LY),0))*100)::NUMERIC(18,1) AS Margin_Percent_LY,
                    (((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))*100)::NUMERIC(18,1) AS Margin_Percent_Curr,
                    (((SUM(SalesDollars_FR_FC)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_FR_FC),0))*100)::NUMERIC(18,1) AS Margin_Percent_FR,
                    (((((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,1)-((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1))/NULLIF(((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1),0))*100)::NUMERIC(18,1) AS Margin_Percent_Var,
                    ((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1) AS Turns_TY,
                    ((SUM(COGS_LY)/NULLIF(SUM(OHC_LY),0))*365)::DECIMAL(18,1) AS Turns_LY,
                    ((SUM(COGS_FC)/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1) AS Turns_FC,
                    ((((SUM(coalesce(COGS_FC,0))/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1)-((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1))/NULLIF(((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1),0))::DECIMAL(18,1) AS Turns_Var,
                    (SUM(SalesUnits_TY)/NULLIF(SUM(ShipsGross_TY),0) *100)::DECIMAL(18,1) AS SellThru_TY,
                    (SUM(SalesUnits_LY)/NULLIF(SUM(ShipsGross_LY),0) *100)::DECIMAL(18,1) AS SellThru_LY,
                    SUM(Dollars_FC_DL) AS Dollars_FC_DL,
                    SUM(Dollars_FC_LOW) AS Dollars_FC_LOW,
                    SUM(Dollars_FC_Vendor) AS Dollars_FC_Vendor,
                    SUM(Units_FC_DL) AS Units_FC_DL,
                    SUM(Units_FC_LOW) AS Units_FC_LOW,
                    SUM(Units_FC_Vendor) AS Units_FC_Vendor,
                    ((SUM(coalesce(Dollars_FC_DL,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_DL_Var,
                    ((SUM(coalesce(Dollars_FC_LOW,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_LOW_Var,
                    ((SUM(coalesce(Dollars_FC_Vendor,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_Vendor_Var,
                    ((SUM(coalesce(Units_FC_DL,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_DL_Var,
                    ((SUM(coalesce(Units_FC_LOW,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_LOW_Var,
                    ((SUM(coalesce(Units_FC_Vendor,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_Vendor_Var,
                    SUM(ReceiptUnits_TY)   AS ReceiptUnits_TY,
                    SUM(ReceiptUnits_LY)   AS ReceiptUnits_LY,
                    SUM(ReceiptDollars_LY) AS ReceiptDollars_LY,
                    SUM(ReceiptDollars_TY) AS ReceiptDollars_TY,
                    CASE WHEN COALESCE(sum(SalesDollars_Curr),0)=0 THEN
                    CASE WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =1
                    THEN 'Moderate Decrease'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =2
                    THEN 'Mild Decrease'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =3
                    THEN 'Mild Increase'
                    ELSE 'Moderate Increase'
                    END
                    ELSE CASE WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =1
                    THEN 'Moderate Decrease'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =2
                    THEN 'Mild Decrease'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =3
                    THEN 'Mild Increase'
                    ELSE 'Moderate Increase'
                    END
                    END AS PriceSensitivityImpact,
                    CASE WHEN sum(SalesDollars_Curr) IS NULL THEN
                    CASE
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =1
                    THEN '-10%'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =2
                    THEN '-5%'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =3
                    THEN '5%'
                    ELSE '10%'
                    END
                    ELSE CASE
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =1
                    THEN '-10%'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =2
                    THEN '-5%'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =3
                    THEN '5%'
                    ELSE '10%'
                    END
                    END AS PriceSensitivityPercent,
                    --(SUM(SalesUnits_TY)/NULLIF(SUM(Total_SalesUnits_TY),0)*100)::NUMERIC(18,1) AS VBUPercent,
                    100 as VBUPercent,
                    MAX(MM_Comments) AS MM_Comments,
                    MAX(Vendor_Comments) AS Vendor_Comments

                FROM forecast{0}.{1}_calcs_b0 f
                {2}
                {7}
                {3}
                offset {4}
                limit {5} ", _dev, param.TableName, where, order, param.Start.ToString(), param.Length.ToString(), select, groupby);

            return cmd;
        }

        // Fetch total record count, filtered or unfiltered for a vendors table.
        public string GetForecastTableCount(DTParameterModel param, bool filtered = false, bool edit = false)
        {
            string cmd = string.Empty;
            string where = CreateWhereClause(param);
            string groupby = CreateGroupByStatement(param);
            string select = CreateSelectStatement(param);

            if (!filtered && !edit)
            {
                var isRotatorSelected = param.Rotator.Any(rc => rc.Included);
                if (isRotatorSelected)
                {
                    cmd = string.Format(@"
                    SELECT FilterValue as Count from forecast{0}.filters_{1}
                    WHERE FilterType = 'Count';", _dev, param.TableName);
                }
                else
                {
                    cmd = "SELECT 0 AS Count;";
                }
            }
            else if (filtered && !edit)
            {
                if (!string.IsNullOrEmpty(groupby))
                {
                    cmd = string.Format(@"
                    SELECT COUNT(*)
                    FROM 
				    (
					    SELECT {4} -1 AS select
					    FROM Forecast{0}.{1}_calcs_b0 {2} {3}
				    )tbl"
                    , _dev, param.TableName, where, groupby, select);
                }
                else // Added to display 1 row when only a sum row is shown. Basically when no rotator columns are selected.
                {
                    cmd = "SELECT 0 AS COUNT;";
                }
            }
            else if (!filtered && edit)
            {
                cmd = string.Format(@"
                SELECT COUNT(*)
                FROM Forecast{0}.{1}_calcs_b0 
				{2};", _dev, param.TableName, where);
            }

            return cmd;
        }

        /// <summary>
        /// Fetch total record count, filtered or unfiltered for a vendor, MM, or anyone from tbl_allvendors table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetOverlappingItemPatchTableCount(DTParameterModel param)
        {
            string cmd = string.Empty;

            // MM's only see the rows with their name in it and the key is their name and owner = true.
            if (param.IsMM)
            {
                cmd = string.Format(@"select count(*) as RowCount from Forecast{0}.itemPatch_Overlap 
                    where MM = (select MM from forecast{0}.config_mm where lower(username) = lower($${1}$$) and MMFlag = true limit 1)
                    and Owner = true;", _dev, param.Username);
            }
            else if (param.IsMD || string.Equals(param.TableName, "tbl_allvendors", StringComparison.InvariantCultureIgnoreCase))
            {
                // Anyone from tbl_allvendors see's all rows and the key is owner = true.
                cmd = string.Format(@"select count(*) as RowCount from Forecast{0}.itemPatch_Overlap where Owner = true;", _dev, param.GMSVenID);
            }
            else
            {
                // Otherwise, it's a vendor. Vendor see's all rows with their gmsvenid. 
                // The key is their gmsvenid OR not their gmsvenid and owner = true and itemid/patch appears in their claim rows where they're not the owner.
                cmd = string.Format(@"select count(*) as RowCount from Forecast{0}.itemPatch_Overlap where gmsvenid = {1};", _dev, param.GMSVenID);
            }

            return cmd;
        }

        public string GetImportProcess(int gmsvenid)
        {
            var cmd = string.Format(@"SELECT * FROM Forecast{0}.import_Processes 
                WHERE GMSVenID = {1}; ", _dev, gmsvenid);

            return cmd;
        }

        // Grab the name of the table a vendor will look at.
        public string GetTableName(int gmsvenid, string vendorgroup, string username)
        {
            string cmd = string.Empty;
            //if (vendorgroup == "ALL")
            if (vendorgroup == "LOWES_ALL_V_NOMRBBQ" || vendorgroup == "ALL")
            {
                username = System.Text.RegularExpressions.Regex.Replace(username, @"\s+", "");
                cmd = string.Format(@" SELECT coalesce(ViewName, TableName) AS TableName FROM Forecast{0}.config_MM WHERE REPLACE(UserName, ' ', '') ILIKE $${1}$$ OR ((REPLACE(MM, ' ', '') ILIKE $${1}$$ )AND MMFlag = true) ", _dev, username);
            }
            else if (vendorgroup == "*")
            {
                cmd = string.Format(@" SELECT TableName FROM Forecast{0}.config_MM WHERE TableName = 'tbl_AllVendors' limit 1 ", _dev);
            }
            else if (vendorgroup == "MM") // Extra case added for MM's transitioning into MD's but still act as MM's as well.
            {
                username = System.Text.RegularExpressions.Regex.Replace(username, @"\s+", "");
                cmd = string.Format(@" SELECT TableName FROM Forecast{0}.config_MM WHERE REPLACE(MM, ' ', '') ILIKE $${1}$$ AND MMFlag = true ", _dev, username);
            }
            else
            {
                cmd = string.Format(@" SELECT TableName FROM Forecast{0}.config_Vendors WHERE GMSVenID = {1} ", _dev, gmsvenid);
            }
            return cmd;
        }

        public string CheckIfView(string tableName)
        {
            string cmd = string.Empty;

            cmd = string.Format(@"  select case when $${1}$$ in (select distinct viewName from forecast{0}.config_mm where ViewName is not null)
                 then true
                 else false
                 end as isView;", _dev, tableName);

            return cmd;
        }

        //Grab the Units Summary table
        public string GetUnitsSummaryTable(DTParameterModel param)
        {
            string cmd = string.Empty;
            //string where = CreateWhereClause(param);

            cmd = string.Format(@"
                SELECT  'DemandLink' as Forecast_0,
                SUM(SalesUnits_TY) as Actual_0,
                SUM(Units_FC_DL) AS FC_0,
                CAST((SUM(coalesce(Units_FC_DL,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100 AS DECIMAL(18,1)) AS Var_0,
                'Lowe''s' as Forecast_1,
                SUM(SalesUnits_TY) as Actual_1,
                SUM(Units_FC_LOW) AS FC_1,
                CAST((SUM(coalesce(Units_FC_LOW,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100 AS DECIMAL(18,1)) AS Var_1,
                'Vendor' as Forecast_2,
                SUM(SalesUnits_TY) as Actual_2,
                SUM(Units_FC_Vendor) AS FC_2,
                CAST((SUM(coalesce(Units_FC_Vendor,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100 AS DECIMAL(18,1)) AS Var_2,
                'Adjusted' as Forecast_3,
                SUM(SalesUnits_TY) as Actual_3,
                SUM(SalesUnits_FC) AS FC_3,
                CAST(((SUM(coalesce(SalesUnits_FC,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0))*100 AS NUMERIC(18,1)) AS Var_3,
                'Average' as Forecast_4,
                SUM(SalesUnits_TY) as Actual_4,
                CAST((SUM(coalesce(Units_FC_DL,0))+SUM(coalesce(Units_FC_LOW,0))+SUM(coalesce(Units_FC_Vendor,0))+SUM(coalesce(SalesUnits_FC,0)))/4 AS INT) as FC_4,
                CAST((((SUM(coalesce(Units_FC_DL,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)
                +((SUM(coalesce(Units_FC_LOW,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)
                +((SUM(coalesce(Units_FC_Vendor,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)
                +((SUM(coalesce(SalesUnits_FC,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100))
                /4 AS NUMERIC(18,1)) as Var_4
                FROM forecast{0}.{1}_calcs_b0 ", _dev, param.TableName);

            return cmd;
        }

        //Grab the Units Summary table
        public string GetDollarSummaryTable(DTParameterModel param)
        {
            string cmd = string.Empty;
            //string where = CreateWhereClause(param);

            cmd = string.Format(@"
                 SELECT  'DemandLink' as Forecast_0,
                SUM(SalesDollars_TY) as Actual_0,
                SUM(Dollars_FC_DL) AS FC_0,
                CAST((SUM(coalesce(Dollars_FC_DL,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100 AS DECIMAL(18,1)) AS Var_0,
                'Lowe''s' as Forecast_1,
                SUM(SalesDollars_TY) as Actual_1,
                SUM(Dollars_FC_LOW) AS FC_1,
                CAST((SUM(coalesce(Dollars_FC_LOW,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100 AS DECIMAL(18,1)) AS Var_1,
                'Vendor' as Forecast_2,
                SUM(SalesDollars_TY) as Actual_2,
                SUM(Dollars_FC_Vendor) AS FC_2,
                CAST((SUM(coalesce(Dollars_FC_Vendor,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100 AS DECIMAL(18,1)) AS Var_2,
                'Adjusted' as Forecast_3,
                SUM(SalesDollars_TY) as Actual_3,
                SUM(SalesDollars_Curr) AS FC_3,
                CAST(((SUM(coalesce(SalesDollars_Curr,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0))*100 AS NUMERIC(18,1)) AS Var_3,
                'Average' as Forecast_4,
                SUM(SalesDollars_TY) as Actual_4,
                CAST((SUM(coalesce(Dollars_FC_DL,0))+SUM(coalesce(Dollars_FC_LOW,0))+SUM(coalesce(Dollars_FC_Vendor,0))+SUM(coalesce(SalesDollars_Curr,0)))/4 AS INT) as FC_4,
                CAST((
                ((SUM(coalesce(Dollars_FC_DL,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)
                +((SUM(coalesce(Dollars_FC_LOW,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)
                +((SUM(coalesce(Dollars_FC_Vendor,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)
                +((SUM(coalesce(SalesDollars_Curr,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100))
                /4 AS NUMERIC(18,1)) as Var_4
                FROM forecast{0}.{1}_calcs_b0 ", _dev, param.TableName);

            return cmd;
        }

       public string GetUpdatedDates(DTParameterModel param)
        {
            string schema = GetPublicShema();
            string cmd = string.Empty;

            cmd = string.Format(@"
                  select Min(c.Date) as Date_min, MAX(c.Date) as Date_max from {1}.tbl_calendar c
                  inner join (  
                  SELECT *
                  FROM forecast{0}.build_window where year = 'TY'
                  ) f
                  on c.fiscalwk = f.fiscalwk and c.fiscalWkCounter = f.fiscalwkCounter
                  and DayOFWEEK(Date) %6 = 0 and retid = 1 ", _dev, schema);

            return cmd;
        }

        //Grab the Margin % Summary table
        public string GetMarginPercentSummaryTable(DTParameterModel param)
        {
            string cmd = string.Empty;

            cmd = string.Format(@"
                SELECT       
                        CAST(((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))*100 AS NUMERIC(18,1)) as Actual, 
                        CAST(((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))*100 AS NUMERIC(18,1)) as FC, 
                        CAST(((((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))-((SUM(SalesDollars_TY)-SUM(COGS_TY))/NULLIF(SUM(SalesDollars_TY),0)))
                        /NULLIF(((NULLIF(SUM(SalesDollars_TY),0)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0)),0))*100 AS NUMERIC(18,1)) as Var
                FROM forecast{0}.{1}_calcs_b0 ", _dev, param.TableName);

            return cmd;
        }

        internal string GetIsAllocationEditable(DTParameterModel param, bool isFrozen)
        {
            var where = CreateWhereClause(param);

            var cmd = string.Format(@"SELECT COALESCE(SUM({3}), 0) > 0 as Allocatable 
                FROM Forecast{0}.{1} {2} LIMIT 1; "
            , _dev
            , param.TableName
            , where
            , isFrozen ? "units_fc_vendor" : "salesunits_fc");

            return cmd;
        }

        //Grab the Margin Dollars Summary table
        public string GetMarginDollarSummaryTable(DTParameterModel param)
        {
            string cmd = string.Empty;

            cmd = string.Format(@"
                SELECT         
                    SUM(Margin_Dollars_TY) as Actual,
                    SUM(Margin_Dollars_Curr) as FC, 
                    CAST(((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_TY))/NULLIF(SUM(Margin_Dollars_TY),0))*100 AS NUMERIC(18,1)) as Var
                FROM forecast{0}.{1}_calcs_b0", _dev, param.TableName);

            return cmd;
        }

        //Get the latest updates for the current user
        public string GetNotifications(UserInfo userInfo)
        {
            var dateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            var maxTS = DateTime.MaxValue.ToString(dateTimeFormat);
            var minTS = DateTime.MinValue.ToString(dateTimeFormat);

            var cmd = string.Format(@"
                SELECT 
                    notif.NotifID, 
                    notif.Title, 
                    notif.Body, 
                    notif.Target,
                    c.Notif_Type_Name as NotificationType, 
                    notif.NotifTypeID as NotificationTypeId, 
                    c.TableName,
                    CAST(notif.StartTime AS TimeStamp) AS StartTime,
                    CAST(notif.EndTime AS TimeStamp) AS EndTime
                FROM Notifications{0}.tbl_Notifications notif
                LEFT JOIN Notifications{0}.Config_Notifications c on c.NCID = notif.NotifType
                LEFT JOIN (SELECT * FROM Notifications{0}.tbl_DL_Events WHERE CURRENT_TIMESTAMP BETWEEN COALESCE(StartTime, CAST($${4}$$ AS TimeStamp)) AND COALESCE(EndTime, CAST($${5}$$ AS TimeStamp))) events
                    ON c.Notif_Type_Name = 'Event' AND notif.NotifTypeId = events.ID
                LEFT JOIN Notifications{0}.tbl_Notified_Users notified on notified.NotificationID = notif.NotifID and notified.GMSVenID = {1} and lower(notified.UserName) = lower($${2}$$) 
                WHERE (notif.Target = $${3}$$ OR notif.Target = 'all')
                AND CASE WHEN c.Notif_Type_Name = 'Event' 
                                THEN (notified.ID IS NULL AND events.ID IS NOT NULL) OR CAST(notified.LastEdit AS TIMESTAMP) < CAST(notif.LastEdit AS TIMESTAMP)
                                ELSE (notified.ID IS NULL OR CAST(notified.LastEdit AS TIMESTAMP) < CAST(notif.LastEdit AS TIMESTAMP)) END
                AND CURRENT_TIMESTAMP BETWEEN COALESCE(notif.StartTime, CAST($${4}$$ AS TimeStamp)) AND COALESCE(notif.EndTime, CAST($${5}$$ AS TimeStamp))
                AND (notif.GMSVenID = {1} OR notif.GMSVenID = -1);"
                , _dev, userInfo.GMSVenId, userInfo.UserName, toolName, minTS, maxTS);

            return cmd;
        }

        // Use for admins only.
        public string GetNotificationsList()
        {
            var cmd = string.Format(@"select 
                    notif.NotifID, 
                    notif.Title, 
                    notif.Body, 
                    notif.Target,
                    notif.NotifType as NotificationType, 
                    notif.NotifTypeId as NotificationTypeId, 
                    c.TableName as 'TableName',
                    CAST(notif.StartTime AS TimeStamp) AS StartTime,
                    CAST(notif.EndTime AS TimeStamp) AS EndTime 
                from Notifications{0}.tbl_Notifications notif
                LEFT JOIN Notifications{0}.Config_Notifications c on c.NCID = notif.NotifType;", _dev);

            return cmd;
        }

        public string GetNotificationCategories()
        {
            var cmd = string.Format(@"
                SELECT NCID, Notif_Type_Name AS NotifTypeName FROM Notifications{0}.Config_Notifications;
                ", _dev);

            return cmd;
        }

        //Grabs the user details from Section Access to identify source table.
        public string GetUserDetail(string username)
        {
            string cmd = string.Empty;

            cmd = string.Format(
                  @"SELECT SecAc.NTName
                ,CASE WHEN COALESCE(ven.Primary_retvenid,ven.retvenid) = '*' THEN '0'
                WHEN SecAc.VENDORGROUP = 'LOWES_ALL_V_NOMRBBQ' THEN '0'
                    ELSE RIGHT(COALESCE(ven.Primary_retvenid,ven.retvenid), 3) END GMSVenID
                ,SecAc.VENDORGROUP                
                FROM UserAccess.qvweb_tbl_UserSectionAccess SecAc
                LEFT JOIN UserAccess.qvweb_tbl_UserSectionAppVendors Ven
                ON SecAc.VENDORGROUP = ven.VENDORGROUP
                WHERE lower(SecAc.NTName) = lower($$GMS\{0}$$) 
                GROUP BY COALESCE(ven.Primary_retvenid,ven.retvenid), NTNAME, SecAc.VENDORGROUP", username);

            return cmd;
        }

        // Gets forcastDetail cells that are cascaded when updates occur 
        public string GetUpdatedCellsByForecastIds(DTParameterModel param)
        {
            string cmd = string.Empty;
            string select = CreateSelectStatement(param);
            string where = CreateWhereClause(param);
            string groupby = CreateGroupByStatement(param);
            string orderby = CreateOrderByClause(param);

            cmd = string.Format(@"
                SELECT {0}   
				 SUM(SalesDollars_FR_FC) AS SalesDollars_FR_FC 
				,SUM(SalesDollars_Curr)  AS SalesDollars_Curr 
				,(((SUM(coalesce(SalesDollars_Curr,0)) -SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)) *100)::NUMERIC(18,1) AS SalesDollars_Var

				,SUM(SalesUnits_FC) AS SalesUnits_FC
				,(((SUM(coalesce(SalesUnits_FC,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0))*100)::NUMERIC(18,1) AS SalesUnits_Var

				,(((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END-(SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
                /NULLIF((SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS ASP_Var		
				,CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END AS ASP_FC
				
				,SUM(Margin_Dollars_Curr) AS Margin_Dollars_Curr 
				,SUM(Margin_Dollars_FR) AS Margin_Dollars_FR 
				,(((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_TY))/NULLIF(SUM(Margin_Dollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Dollars_Var_Curr

				,(((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))*100)::NUMERIC(18,1) AS Margin_Percent_Curr 
				,(((SUM(SalesDollars_FR_FC)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_FR_FC),0))*100)::NUMERIC(18,1) AS Margin_Percent_FR 
				,(((((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,1)-((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1))
					/NULLIF(((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1),0))*100)::NUMERIC(18,1) AS Margin_Percent_Var

                ,CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
				ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END AS Cost_FC
                ,(((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
					ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END - CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                               ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END)
                        / NULLIF(CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                                      ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END,0))
                        *100)::NUMERIC(18,1) AS Cost_Var

				,CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2)
					END AS RetailPrice_FC
		        ,(((CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN coalesce(AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2),0)
					ELSE coalesce(SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0),0)::NUMERIC(18,2)
					END-(SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
					/NULLIF((SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS RetailPrice_Var
                ,CASE WHEN SUM(SalesDollars_TY) <= 0 THEN 0 ELSE (COALESCE((sum(SalesDollars_FR_TY)- sum(SalesDollars_TY)) /nullif(sum(SalesDollars_FR_TY),0),0)*100)::NUMERIC(18,1) END AS RetailPrice_Erosion_Rate   

                ,(((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_FR_TY))/NULLIF(SUM(Margin_Dollars_FR_TY),0))*100)::NUMERIC(18,1) AS MarginDollars_FR_Var

				,((SUM(COGS_FC)/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1) AS Turns_FC
                ,((((SUM(coalesce(COGS_FC,0))/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1)-((SUM(COGS_TY)
						/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1))/NULLIF(((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1),0))::DECIMAL(18,1) AS Turns_Var

				,SUM(Dollars_FC_DL) AS Dollars_FC_DL 
				,SUM(Dollars_FC_LOW) AS Dollars_FC_LOW 
				,SUM(Dollars_FC_Vendor) AS Dollars_FC_Vendor

                ,SUM(Units_FC_LOW) AS Units_FC_LOW
                ,SUM(Units_FC_Vendor) AS Units_FC_Vendor

                ,((SUM(coalesce(Dollars_FC_DL,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_DL_Var
                ,((SUM(coalesce(Dollars_FC_LOW,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_LOW_Var
                ,((SUM(coalesce(Dollars_FC_Vendor,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_Vendor_Var

                ,((SUM(coalesce(Units_FC_LOW,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_LOW_Var
                ,((SUM(coalesce(Units_FC_Vendor,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_Vendor_Var

				,MAX(MM_Comments) AS MM_Comments
                ,MAX(Vendor_Comments) AS Vendor_Comments
				FROM forecast{1}.{2}_calcs_b0 f
                {3}
                {4} ", select, _dev, param.TableName, where, groupby);

            return cmd;
        }

        //Checks to make sure that user is valid. If there are problems with other retailers logging in or DL employees.  Revisit this.
        // removed exclusions from hotfix see FOR-137
        public string GetIsUser(string username)
        {
            string cmd = string.Empty;

            cmd = string.Format(@"

				SELECT NTName, UserName, NULLIF(RetVenID,'*') RetVenID
                FROM UserAccess.qvweb_tbl_UserSectionAccess usa
                left join UserAccess.qvweb_tbl_UserSectionAppVendors ven
                on usa.VENDORGROUP = ven.VENDORGROUP
                where lower(NTNAME) = lower($${0}$$)
                AND (
					LEFT(ven.RetVenID,1) = '1' 
					or RetVenID = '*' 
					or ven.VENDORGROUP = 'LOWES_ALL_V_NOMRBBQ'
					or usa.VENDORGROUP = '*'
					or usa.VENDORGROUP = 'LOWES_ALL_V_NOMRBBQ'

                    or ven.VENDORGROUP = 'ALL'
					
					or usa.VENDORGROUP = 'ALL'
                )

                
            ", username);

            return cmd;
        }

        //Gets the sums of all columns to display in the top row.
        public string GetSums(DTParameterModel param)
        {
            string cmd = string.Empty;
            string where = CreateWhereClause(param);

            cmd = string.Format(@"
                SELECT
                -1 as GMSVenID,
                -1 as VendorDesc,
                -1 as ItemID,
                -1 as FiscalWk,
                -1 as FiscalMo,
                -1 as FiscalQtr,
                -1 as MD,
                -1 as MM,
                -1 as Region,
                -1 as District,
                -1 as Patch,
				-1 as Parent,
                -1 as ProdGrp,
                -1 as Assrt,
               
				SUM(SalesUnits_TY)::NUMERIC(18,0)  AS SalesUnits_TY,
				SUM(SalesUnits_LY)::NUMERIC(18,0)  AS SalesUnits_LY,
				SUM(SalesUnits_2ly)::NUMERIC(18,0) AS SalesUnits_2ly,
				SUM(SalesUnits_FC)::NUMERIC(18,0)  AS SalesUnits_FC,
				(((SUM(coalesce(SalesUnits_FC,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0))*100)::NUMERIC(18,1) AS SalesUnits_Var,
				SUM(SalesDollars_TY)::NUMERIC(18,0)    AS SalesDollars_TY,
				SUM(SalesDollars_LY)::NUMERIC(18,0)    AS SalesDollars_LY,
				SUM(SalesDollars_2ly)::NUMERIC(18,0)   AS SalesDollars_2ly,
				SUM(SalesDollars_FR_FC)::NUMERIC(18,0) AS SalesDollars_FR_FC,
				SUM(SalesDollars_Curr)::NUMERIC(18,0) AS SalesDollars_Curr,
				(((SUM(coalesce(SalesDollars_Curr,0)) -SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)) *100)::NUMERIC(18,1) AS SalesDollars_Var,
				((((SUM(SalesDollars_TY)/NULLIF(SUM(SalesDollars_2ly),0))^(1/2))-1)*100)::!NUMERIC(18,1) AS CAGR,
				(SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2) AS ASP_TY,
				(SUM(SalesDollars_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) AS ASP_LY,
				CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END AS ASP_FC,
                (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END-(SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
                /NULLIF((SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS ASP_Var,
                CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(RetailPrice_TY,0))::NUMERIC(18,2)
                ELSE (SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                END AS RetailPrice_TY,
                CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(RetailPrice_LY,0))::NUMERIC(18,2)
                ELSE (SUM(SalesDollars_FR_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                END AS RetailPrice_LY,		                  
                CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2)
				END AS RetailPrice_FC,
                (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN coalesce(AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2),0)
			    ELSE coalesce(SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0),0)::NUMERIC(18,2)
			    END-(SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
			    /NULLIF((SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS RetailPrice_Var,
                CASE WHEN SUM(SalesDollars_TY) <= 0 THEN 0 ELSE (COALESCE((sum(SalesDollars_FR_TY)- sum(SalesDollars_TY)) /nullif(sum(SalesDollars_FR_TY),0),0)*100)::NUMERIC(18,1) END AS RetailPrice_Erosion_Rate,
				SUM(SalesDollars_FR_TY)::NUMERIC(18,2) AS SalesDollars_FR_TY,
				SUM(SalesDollars_FR_LY)::NUMERIC(18,2) AS SalesDollars_FR_LY,
                SUM(Margin_Dollars_FR_TY) AS MarginDollars_FR_TY,
                SUM(Margin_Dollars_FR_LY) AS MarginDollars_FR_LY,
                (((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_FR_TY))/NULLIF(SUM(Margin_Dollars_FR_TY),0))*100)::NUMERIC(18,1) AS MarginDollars_FR_Var,
				CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                END AS Cost_TY,
                CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(Cost_LY,0))::NUMERIC(18,2)
                ELSE (SUM(COGS_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2)
                END AS Cost_LY,
                CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
				ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END AS Cost_FC,
                (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
				ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END - CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                            ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END)
                    / NULLIF(CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                                    ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END,0))
                    *100)::NUMERIC(18,1) AS Cost_Var,
				SUM(Margin_Dollars_TY)::NUMERIC(18,0)         AS Margin_Dollars_TY,
				SUM(Margin_Dollars_LY)::NUMERIC(18,0)         AS Margin_Dollars_LY,
				SUM(Margin_Dollars_Curr)::NUMERIC(18,0)       AS Margin_Dollars_Curr,
				SUM(Margin_Dollars_FR)::NUMERIC(18,0)         AS Margin_Dollars_FR,
				(((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_TY))/NULLIF(SUM(Margin_Dollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Dollars_Var_Curr,
				(((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Percent_TY,
                (((SUM(SalesDollars_LY)-SUM(coalesce(COGS_LY,0)))/NULLIF(SUM(SalesDollars_LY),0))*100)::NUMERIC(18,1) AS Margin_Percent_LY,
                (((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))*100)::NUMERIC(18,1) AS Margin_Percent_Curr,
                (((SUM(SalesDollars_FR_FC)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_FR_FC),0))*100)::NUMERIC(18,1) AS Margin_Percent_FR,
                (((((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,1)-((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1))/NULLIF(((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1),0))*100)::NUMERIC(18,1) AS Margin_Percent_Var,
				((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1) AS Turns_TY,
				((SUM(COGS_LY)/NULLIF(SUM(OHC_LY),0))*365)::DECIMAL(18,1) AS Turns_LY,
				((SUM(COGS_FC)/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1) AS Turns_FC,
				((((SUM(coalesce(COGS_FC,0))/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1)-((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1))/NULLIF(((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1),0))::DECIMAL(18,1) AS Turns_Var,
				(SUM(SalesUnits_TY)/NULLIF(SUM(ShipsGross_TY),0) *100)::DECIMAL(18,1) AS SellThru_TY,
				(SUM(SalesUnits_LY)/NULLIF(SUM(ShipsGross_LY),0) *100)::DECIMAL(18,1) AS SellThru_LY,
				SUM(Dollars_FC_DL)::NUMERIC(18,0) AS Dollars_FC_DL,
                SUM(Dollars_FC_LOW)::NUMERIC(18,0) AS Dollars_FC_LOW,
                SUM(Dollars_FC_Vendor)::NUMERIC(18,0) AS Dollars_FC_Vendor,
                SUM(Units_FC_DL)::NUMERIC(18,0) AS Units_FC_DL,
                SUM(Units_FC_LOW)::NUMERIC(18,0) AS Units_FC_LOW,
                SUM(Units_FC_Vendor)::NUMERIC(18,0) AS Units_FC_Vendor,
                ((SUM(coalesce(Dollars_FC_DL,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_DL_Var,
                ((SUM(coalesce(Dollars_FC_LOW,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_LOW_Var,
                ((SUM(coalesce(Dollars_FC_Vendor,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_Vendor_Var,
                ((SUM(coalesce(Units_FC_DL,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_DL_Var,
                ((SUM(coalesce(Units_FC_LOW,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_LOW_Var,
                ((SUM(coalesce(Units_FC_Vendor,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_Vendor_Var,
				SUM(ReceiptUnits_TY)::NUMERIC(18,0)   AS ReceiptUnits_TY,
				SUM(ReceiptUnits_LY)::NUMERIC(18,0)   AS ReceiptUnits_LY,
				SUM(ReceiptDollars_LY)::NUMERIC(18,0) AS ReceiptDollars_LY,
				SUM(ReceiptDollars_TY)::NUMERIC(18,0) AS ReceiptDollars_TY,
				CASE WHEN COALESCE(sum(SalesDollars_Curr),0)=0 THEN
                    CASE WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =1
                    THEN 'Moderate Decrease'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =2
                    THEN 'Mild Decrease'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =3
                    THEN 'Mild Increase'
                    ELSE 'Moderate Increase'
                    END
                    ELSE CASE WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =1
                    THEN 'Moderate Decrease'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =2
                    THEN 'Mild Decrease'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =3
                    THEN 'Mild Increase'
                    ELSE 'Moderate Increase'
                    END
                    END AS PriceSensitivityImpact,
                    CASE WHEN sum(SalesDollars_Curr) IS NULL THEN
                    CASE
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =1
                    THEN '-10%'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =2
                    THEN '-5%'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =3
                    THEN '5%'
                    ELSE '10%'
                    END
                    ELSE CASE
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =1
                    THEN '-10%'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =2
                    THEN '-5%'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =3
                    THEN '5%'
                    ELSE '10%'
                    END
                    END AS PriceSensitivityPercent,
				100 as VBUPercent,
                -1 AS MM_Comments,
                -1 AS Vendor_Comments
                FROM forecast{0}.{1}_calcs_b0 f
                {2} ", _dev, param.TableName, where);

            return cmd;
        }

        /// <summary>
        /// Gets the SQL query for the item patch claims overlap table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetOverlappingClaimsTable(DTParameterModel param, bool export)
        {
            var orderBy = CreateIPOTableOrderByClause(param, "");
            var innerWhere = string.Empty;

            // MM's only see the rows with their name in it and the key is their name and owner = true.
            if (param.IsMM)
            {
                innerWhere = string.Format(@"where MM = (select MM from forecast{0}.config_mm where lower(username) = lower($${1}$$) and MMFlag = true limit 1)
                    and Owner = true", _dev, param.Username);
            }
            else if (param.IsMD || string.Equals(param.TableName, "tbl_allvendors", StringComparison.InvariantCultureIgnoreCase))
            {
                // Anyone from tbl_allvendors see's all rows and the key is owner = true.
                innerWhere = string.Format(@"where Owner = true");
            }
            else
            {
                // Otherwise, it's a vendor. Vendor see's all rows with their gmsvenid. 
                // The key is their gmsvenid OR not their gmsvenid and owner = true and itemid/patch appears in their claim rows where they're not the owner.
                innerWhere = string.Format(@"where gmsvenid = {1}
                    or (
                        gmsvenid <> {1} 
                        and Owner = true
                        and (ItemID, Patch) in (select ItemID, Patch from Forecast{0}.itemPatch_Overlap where gmsvenid = {1})
                       )", _dev, param.GMSVenID);
            }

            var cmd = string.Format(@"
                SELECT ID, GMSVenID, VendorDesc, Owner, ItemID, ItemDesc, Patch, MM, MD, TimeStamp FROM Forecast{0}.itemPatch_Overlap
                where (ItemID, Patch) in (
                    select ItemID, Patch from Forecast{0}.itemPatch_Overlap 
                    {1}
                    {2}
                    {3}
                    {4}
                )
                {2};"
            , _dev
            , innerWhere
            , orderBy
            , export ? "" : $"offset {param.Start}"
            , export ? "" : $"limit {param.Length}");

            return cmd;
        }

        public string GetMMTablesAffectedByQuery(EditorParameterModel editor)
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam);

            cmd = string.Format(@"
				DROP TABLE IF EXISTS mms_temp;
                create local temp table mms_temp
                on commit preserve rows as
                Select DISTINCT MM 
                FROM Forecast{0}.{2}
                {1};

                select TableName from Forecast{0}.config_MM cm
                where lower(cm.mm) in (select lower(MM) from mms_temp)
                and mmflag = true;", _dev, where, editor.TableName);

            return cmd;
        }

        public string GetVendorTablesAffectedByQuery(EditorParameterModel editor)
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam);

            cmd = string.Format(@"
				DROP TABLE IF EXISTS vendors_temp;
                create local temp table vendors_temp
                on commit preserve rows as
                Select DISTINCT gmsvenid 
                FROM Forecast{0}.tbl_AllVendors
                {1};

                select TableName from Forecast{0}.config_vendors vm
                where vm.gmsvenid in (select gmsvenid from vendors_temp)", _dev, where);

            return cmd;
        }

        public string GetPreviousNotifiedUser(UserInfo userInfo, ViewedNotification notif)
        {
            var notifIds = notif.NotifIds.JoinWithWrap(",", "'");
            var cmd = string.Format(@"
            select un.* from Notifications{0}.tbl_Notified_Users un 
            join Notifications{0}.tbl_Notifications n on n.notifid = un.notificationid
            where un.GMSVenID = {1} and lower(un.UserName) = lower($${2}$$) and un.NotificationID in ({3}) and CAST(un.LastEdit as TimeStamp) <= CAST(n.LastEdit as TimeStamp);",
            _dev, userInfo.GMSVenId, userInfo.UserName, notifIds);

            return cmd;
        }

        public string GetVendorList()
        {
            var cmd = string.Format(@"
                SELECT DISTINCT GMSVenID, VendorDesc FROM {0}.tbl_Vendors;", GetPublicShema());

            return cmd;
        }
        
        #endregion SELECT

        #region CREATE

        public string CreateDlEvent(DlEvent dlEvent)
        {
            var sd = CheckAndFormatDate(dlEvent.StartTime);
            var ed = CheckAndFormatDate(dlEvent.EndTime, true);
            var lastEdit = CheckAndFormatDate(dlEvent.LastEdit);

            dlEvent.Body = dlEvent.Body.Replace("'", "''");
            dlEvent.Title = dlEvent.Title.Replace("'", "''");

            var cmd = string.Format(@"
                INSERT INTO Notifications{0}.tbl_DL_Events (Title, Body, FileId, Target, StartTime, EndTime, LastEdit)           
                VALUES ($${1}$$, $${2}$$, $${3}$$, $${4}$$, $${5}$$, $${6}$$, $${7}$$);
                ",
                _dev,
                dlEvent.Title,
                dlEvent.Body,
                dlEvent.FileId,
                toolName,
                GetTimestamp(sd),
                GetTimestamp(ed),
                GetTimestamp(lastEdit));

            return cmd;
        }

        public string CreateTutorial(Tutorial tutorial)
        {
            tutorial.Title = tutorial.Title.Replace("'", "''");
            tutorial.Intro = tutorial.Intro.Replace("'", "''");

            var cmd = string.Format(@"
                INSERT INTO Notifications{0}.tbl_Tutorials (Title, Intro, TutorialGroup, Target, VideoLink, LastEdit) 
                VALUES ($${1}$$, $${2}$$, $${3}$$, $${4}$$, $${5}$$, CAST($${6}$$ AS TimeStamp));
                ", _dev, tutorial.Title, tutorial.Intro, tutorial.TutorialGroup, toolName, tutorial.VideoLink, tutorial.LastEdit);

            return cmd;
        }

        /// <summary>
        /// Records a new instance of a user starting an upload process.
        /// </summary>
        /// <param name="importProcess"></param>
        /// <returns></returns>
        public string CreateImportProcess(ImportProcess importProcess)
        {
            var cmd = string.Format(@"INSERT INTO Forecast{0}.import_Processes 
                (GMSVenID, ProcessId, FileName, StartTIme) VALUES ({1}, {2}, $${3}$$, CAST($${4}$$ AS TIMESTAMP)); ",
                _dev, importProcess.GMSVenID, importProcess.ProcessId, importProcess.FileName, importProcess.StartTime);

            return cmd;
        }

        /// <summary>
        /// Parses the DTParameterModel for search terms and prepares a "WHERE" string
        /// that includes "WHERE IN ()" or excludes "WHERE NOT IN ()" the search terms. 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateWhereClause(DTParameterModel param)
        {
            string clause = string.Empty;
            string where = string.Empty;
            string notWhere = " ";
            clause = string.Format("WHERE 1 = 1 ");

            if (param.Columns != null)
            {
                foreach (DTColumn c in param.Columns)
                {
                    if ((c.Searchable && c.Search.Value.Length > 0) && c.Search.Value != "-1")
                    {
                        bool firstFilterTerm = true;
                        bool firstNotFilterTerm = true;
                        List<string> list = new List<string>();

                        // Some filters have commas in the text so we need to split on a comma that doesn't have trailing
                        // text after it with a double quote and no comma. Essentially, we want ,text,text or ,"text", "text, this is allowed",
                        list = Regex.Split(c.Search.Value, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();

                        foreach (string s in list)
                        {
                            var str = s.Replace("\"", "");
                            str = str.Replace("\'", "''");

                            //added for exlucde filter
                            if (s.StartsWith("!"))
                            {
                                if (!firstNotFilterTerm)
                                {
                                    notWhere = notWhere.Insert(notWhere.Length - 1, ",'" + str.Substring(1) + "'");
                                }
                                else
                                {
                                    notWhere = notWhere.Insert(notWhere.Length, " AND " + c.Name + " NOT IN ('" + str.Substring(1) + "')");
                                    firstNotFilterTerm = false;
                                }
                            }
                            else
                            {

                                if (!firstFilterTerm)
                                {
                                    where = where.Insert(where.Length - 1, ",'" + str + "'");
                                }
                                else
                                {
                                    where = where.Insert(where.Length, " AND " + c.Name + " IN ('" + str + "')");
                                    firstFilterTerm = false;
                                }
                            }
                        }
                    }
                }
            }

            clause = clause + where + notWhere;
            return clause;
        }

        /// <summary>
        /// Overload function that
        /// takes in the name of a table alias to preappend to each
        /// of the column variables
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateWhereClause(DTParameterModel param, string prepend)
        {
            string clause = string.Empty;
            string where = string.Empty;
            clause = string.Format("WHERE 1 = 1 ");
            string notWhere = " ";
            foreach (DTColumn c in param.Columns)
            {
                if (c.Searchable && c.Search.Value.Length > 0 && c.Search.Value != "-1")
                {
                    bool firstFilterTerm = true;
                    bool firstNotFilterTerm = true;
                    List<string> list = new List<string>();
                    list = c.Search.Value.Split(',').ToList();

                    foreach (string s in list)
                    {
                        var str = s.Replace("\"", "");
                        str = str.Replace("'", "''");

                        if (s.StartsWith("!"))
                        {
                            if (!firstNotFilterTerm)
                            {
                                notWhere = notWhere.Insert(notWhere.Length - 1, ",$$" + str.Substring(1) + "$$");
                            }
                            else
                            {
                                notWhere = notWhere.Insert(notWhere.Length, " AND " + prepend + "." + c.Name + " NOT IN ($$" + str.Substring(1) + "$$)");
                                firstNotFilterTerm = false;
                            }
                        }
                        else
                        {

                            if (!firstFilterTerm)
                            {
                                where = where.Insert(where.Length - 1, ",$$" + str + "$$");
                            }
                            else
                            {
                                where = where.Insert(where.Length, " AND " + prepend + "." + c.Name + " IN ($$" + str + "$$)");
                                firstFilterTerm = false;
                            }
                        }
                    }
                }
            }
            clause = clause + where + notWhere;
            return clause;
        }

        /// <summary>
        /// Overload function that
        /// takes in the name of a table alias to preappend to each
        /// of the column variables.  Also has bool flag to denote whether this updates a comment or not.
        /// Only used for comment updates.  
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateWhereClause(DTParameterModel param, string prepend, bool comment)
        {
            string clause = string.Empty;
            string where = string.Empty;
            clause = string.Format("WHERE 1 = 1 ");

            if (comment == true)
            {
                prepend = prepend + ".";
            }

            foreach (DTColumn c in param.Columns)
            {
                if (c.Searchable && c.Search.Value.Length > 0 && c.Search.Value != "-1")
                {
                    if (c.Name == "ItemID" || c.Name == "MM" || c.Name == "VendorDesc")
                    {
                        bool firstFilterTerm = true;
                        List<string> list = new List<string>();
                        list = c.Search.Value.Split(',').ToList();

                        foreach (string s in list)
                        {
                            var str = s.Replace("'", "''");

                            if (!firstFilterTerm)
                            {
                                where = where.Insert(where.Length - 1, ",$$" + str + "$$");
                            }
                            else
                            {
                                where = where.Insert(where.Length, " AND " + prepend + c.Name + " IN ($$" + str + "$$)");
                                firstFilterTerm = false;
                            }

                        }
                    }

                }

                //else break;
            }
            //else break;

            clause = clause + where;
            return clause;
        }

        /// <summary>
        /// Parses the DTParameterModel for order terms and prepares an "ORDER BY" string
        /// that includes the column(s) and whether each is ordered ascending or descending.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateOrderByClause(DTParameterModel param)
        {
            string order = string.Empty;
            foreach (DTOrder o in param.Order)
            {
                if (o.Name == "") { order = " "; }//{ o.Name = "ItemID";  }
                                                  //switch (o.Name)
                                                  //{
                                                  //	case "ItemDesc":
                                                  //	case "ItemConcat":
                                                  //	case "MD":
                                                  //	case "MM":
                                                  //	case "District":
                                                  //	case "Patch":
                                                  //	case "ProdGrpDesc":
                                                  //	case "ProdGrpConcat":
                                                  //	case "AssrtDesc":
                                                  //	case "AssrtConcat":
                                                  //		o.Name = "CAST(" + o.Name + " AS VARCHAR)";
                                                  //		break;
                                                  //	default:
                                                  //		o.Name = "CAST(" + o.Name + " AS INT)";
                                                  //		break;
                                                  //}
                if (order.Length == 0)
                    order = " ORDER BY " + o.Name + " " + o.Dir;
                else
                    order = order + ", " + o.Name + " " + o.Dir;
            }
            return order;
        }

        /// <summary>
        /// Parses the DTParameterModel for order terms and prepares an "ORDER BY" string
        /// that includes the column(s) and whether each is ordered ascending or descending.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateIPOTableOrderByClause(DTParameterModel param, string alias = "")
        {
            var columnOrderList = param.CustomOrder.Count() > 0 ? param.CustomOrder : param.Order;
            alias = alias.Length > 0 ? $"{alias}." : alias;

            var orderByList = new List<string>
            {
                $"{alias}Owner desc"
            };

            foreach (DTOrder o in columnOrderList)
            {
                if (o.Name != string.Empty)
                {
                    orderByList.Add($"{alias}{o.Name} {o.Dir}");
                }
            }

            orderByList.Add($"{alias}ID asc");
            var orderBy = $"ORDER BY {string.Join(", ", orderByList)}";

            return orderBy;
        }

        /// <summary>
        /// Parses the DTParameterModel for selection terms and prepares a "SELECT" statement string.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateSelectStatement(DTParameterModel param)
        {
            string rotator = string.Empty;
            string forecastId = "'";
            int cnt = param.Rotator.Count();
            //int lastTerm = 1;
            foreach (DTRotator r in param.Rotator)
            {
                if (r.Included == true)
                {
                    rotator = rotator + r.Column + ", ";
                    forecastId = forecastId + "' || CAST(" + r.Column + " as VARCHAR(20)) || '";
                }
                else if (r.Included == false)
                {
                    rotator = rotator + "-1 as " + r.Column + ", ";
                    forecastId = forecastId + "-1' || '";
                }

            }
            forecastId = forecastId + "'";
            rotator = forecastId + " AS ForecastID," + rotator;
            return rotator;
        }

        /// <summary>
        /// Parses the DTParameterModel for grouping terms and prepares a "GROUP BY" string.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateGroupByAllStatement(DTParameterModel param)
        {
            int cnt = param.Rotator.Count();
            var groupBy = string.Join(",", param.Rotator.Select(r => r.Column).ToArray());

            if (groupBy != "")
            {
                // Resolve an issue when the grouping term is comprised from the EditorParameterModel
                // and has duplicates resulting from the ID portion of the rotator. Remove the duplicates
                // here and leave the unique grouping terms in the correct order.
                List<string> uniques = groupBy.Split(',').Reverse().Distinct().Take(cnt).Reverse().ToList();
                string newStr = string.Join(",", uniques);
                groupBy = newStr;
                groupBy = "GROUP BY " + groupBy;
            }

            return groupBy;
        }

        /// <summary>
        /// Parses the DTParameterModel for grouping terms and prepares a "GROUP BY" string.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private string CreateGroupByStatement(DTParameterModel param)
        {
            string groupby = string.Empty;
            int cnt = param.Rotator.Count();
            int lastTerm = 1;

            foreach (DTRotator r in param.Rotator)
            {
                if (groupby.Length == 0 && r.Included == true)
                {
                    groupby = r.Column;
                }
                else if (lastTerm <= cnt && r.Included == true)
                {
                    groupby = groupby + ", " + r.Column;
                }
                else if (lastTerm == cnt && r.Included == true)
                {
                    groupby = groupby + r.Column;
                }
                lastTerm++;
            }
            if (groupby != "")
            {
                // Resolve an issue when the grouping term is comprised from the EditorParameterModel
                // and has duplicates resulting from the ID portion of the rotator. Remove the duplicates
                // here and leave the unique grouping terms in the correct order.
                List<string> uniques = groupby.Split(',').Reverse().Distinct().Take(cnt).Reverse().ToList();
                string newStr = string.Join(",", uniques);
                groupby = newStr;
                groupby = "GROUP BY " + groupby;
            }

            return groupby;
        }

        public string CreateNotification(Notification notif)
        {
            var sd = CheckAndFormatDate(notif.StartTime);
            var ed = CheckAndFormatDate(notif.EndTime, true);
            var lastEdit = CheckAndFormatDate(notif.LastEdit);

            notif.Body = notif.Body.Replace("'", "''");
            notif.Title = notif.Title.Replace("'", "''");

            var cmd = string.Format(@"
                INSERT INTO Notifications{0}.tbl_Notifications (Title, Body, GMSVenID, Target, StartTime, EndTime, NotifType, NotifTypeID, LastEdit) 
                VALUES ($${1}$$, $${2}$$, {3}, $${4}$$, $${5}$$, $${6}$$, {7}, {8}, $${9}$$);
                ",
                _dev,
                notif.Title,
                notif.Body,
                notif.GMSVenID,
                toolName,
                sd,
                ed,
                notif.NotificationType,
                notif.NotificationTypeId,
                lastEdit);

            return cmd;
        }

        public string CreateUpdateSalesUnitsTable(EditorParameterModel editor, string tableName, string innerTableName, bool isFrozen)
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam);

            var targetUnits = int.Parse(editor.SalesU.First().SalesU);

            cmd = string.Format(@"
                DROP TABLE IF EXISTS Forecast{0}.{3};
			    CREATE TABLE IF NOT EXISTS Forecast{0}.{3}
			    (
				    {4} INT NULL
				    ,rec_cnt INT NULL
                    ,Current_Sum_SalesUnits_FC INT NULL
			    );

			    INSERT INTO Forecast{0}.{3}
			    SELECT NULLIF(SUM({4}), 0), NULLIF(COUNT(gmsvenid),0), COALESCE(SUM(SalesUnits_FC),0)
			    FROM forecast{0}.{1}
			    {2}
            
			    LIMIT 1;
                
                DROP TABLE IF EXISTS Forecast{0}.{6};
                CREATE TABLE Forecast{0}.{6} AS
                SELECT SalesUnits_FC_Round, ItemID as Edit_ItemID, StoreID as Edit_StoreID, FiscalWk as Edit_FiscalWk, Current_Sum_SalesUnits_FC,
                SUM(SalesUnits_FC_Round) OVER (ORDER BY SalesUnits_FC_Round DESC, StoreID ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) - SalesUnits_FC_Round AS SalesUnits_FC_Cumulative_Sum
                FROM ( SELECT t1.ItemID, t1.StoreID, t1.FiscalWk, t2.Current_Sum_SalesUnits_FC,
                            CEIL(CASE WHEN COALESCE(t2.{4},0) = 0
                                          THEN CASE WHEN ({5} - t2.Current_Sum_SalesUnits_FC) < 0 AND ABS({5} - t2.Current_Sum_SalesUnits_FC)/rec_cnt > t1.SalesUnits_FC
                                                        THEN t1.SalesUnits_FC
                                                    ELSE ABS({5} - t2.Current_Sum_SalesUnits_FC)/rec_cnt
                                               END
                                      ELSE CASE WHEN ({5} - t2.Current_Sum_SalesUnits_FC) < 0 AND t1.{4}/t2.{4}*ABS({5} - t2.Current_Sum_SalesUnits_FC) > t1.SalesUnits_FC
                                                    THEN t1.SalesUnits_FC
                                                ELSE COALESCE(t1.{4},0)/t2.{4}*ABS({5} - t2.Current_Sum_SalesUnits_FC)
                                           END
                            END) as SalesUnits_FC_Round
			            FROM (SELECT * FROM forecast{0}.{1} {2}) AS t1
                        CROSS JOIN Forecast{0}.{3} AS t2
                ) AS t;
                "
            , _dev //0
            , editor.TableName //1
            , where //2
            , tableName //3
            , isFrozen ? "units_fc_vendor" : "salesunits_fc" //4
            , targetUnits //5
            , innerTableName //6
            );

            return cmd;
        }

        #endregion CREATE

        #region UPDATE

        public string UpdateDlEvent(DlEvent edit)
        {

            var sd = CheckAndFormatDate(edit.StartTime);
            var ed = CheckAndFormatDate(edit.EndTime, true);

            edit.Body = edit.Body.Replace("'", "''");
            edit.Title = edit.Title.Replace("'", "''");

            var cmd = string.Format(@"UPDATE Notifications{0}.tbl_DL_Events 
                SET Title = $${1}$$,
                Body = $${2}$$, 
                FileId = $${3}$$,
                Target = $${4}$$, 
                StartTime = CAST($${5}$$ AS TIMESTAMP), 
                EndTime = CAST($${6}$$ AS TIMESTAMP), 
                LastEdit = CAST($${7}$$ AS TIMESTAMP) 
                WHERE ID = {8};",
                _dev,
                edit.Title,
                edit.Body,
                edit.FileId,
                edit.Target,
                sd,
                ed,
                edit.LastEdit,
                edit.EventId);

            return cmd;
        }

        public string UpdateImportProcess(EditorParameterModel editor, bool isFrozen)
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam);


            cmd = string.Format(@"
			DROP TABLE IF EXISTS t_sum_vendor_forecast;
			CREATE LOCAL TEMPORARY TABLE IF NOT EXISTS t_sum_vendor_forecast
			(
				{4} INT NULL
				,rec_cnt INT NULL
			)
			ON COMMIT PRESERVE ROWS;

			INSERT INTO t_sum_vendor_forecast
			SELECT NULLIF(SUM({4}), 0), NULLIF(COUNT(gmsvenid),0) 
			FROM forecast{0}.{1}
			{2}
			LIMIT 1;

			UPDATE forecast{0}.{1}
			SET SalesUnits_FC = CASE WHEN (SELECT COALESCE({4},0) FROM t_sum_vendor_forecast) = 0
                    THEN {3}/(SELECT rec_cnt FROM t_sum_vendor_forecast)
                    ELSE {4}/(SELECT {4} FROM t_sum_vendor_forecast)*{3}
                    END
			{2};

			DROP TABLE t_sum_vendor_forecast;"
            , _dev //0
            , editor.TableName //1
            , where //2
            , editor.SalesU.First<ESalesU>().SalesU //3
            , isFrozen ? "units_fc_vendor" : "salesunits_fc" //4
            );


            return cmd;
        }

        public string UpdateNotification(Notification original, Notification edit)
        {
            var sd = CheckAndFormatDate(edit.StartTime);
            var ed = CheckAndFormatDate(edit.EndTime, true);
            var lastEdit = CheckAndFormatDate(edit.LastEdit);

            edit.Body = edit.Body.Replace("'", "''");
            edit.Title = edit.Title.Replace("'", "''");

            var cmd = string.Format(@"UPDATE Notifications{0}.tbl_Notifications 
                SET Title = $${1}$$, 
                Body = $${2}$$, 
                GMSVenID = {3}, 
                Target = $${4}$$, 
                StartTime = CAST($${5}$$ AS TIMESTAMP), 
                EndTime = CAST($${6}$$ AS TIMESTAMP), 
                NotifType = {7}, 
                NotifTypeID = {8}, 
                LastEdit = CAST($${9}$$ AS TIMESTAMP) 
                WHERE NotifID = {10};",
                _dev,
                edit.Title,
                edit.Body,
                edit.GMSVenID,
                edit.Target,
                sd,
                ed,
                edit.NotificationType,
                edit.NotificationTypeId,
                lastEdit,
                original.NotifId);

            return cmd;
        }

        public string UpdateRetailPrice(EditorParameterModel editor, string destTable, string wherePatch = "")
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam);
            if (!string.IsNullOrEmpty(wherePatch))
            {
                where = $"{where} AND {wherePatch}";
            }
            string groupby = CreateGroupByStatement(dtparam);
            var retailPrice = editor.RetailPrice.First<ERetailPrice>();

            cmd = string.Format(@"
                UPDATE forecast{0}.{1}
			    SET RetailPrice_FC = {2},
				    ASP_FC = CASE WHEN NULLIF(RetailPrice_FC,0) IS NULL
						    THEN {2}
						    ELSE {2} / RetailPrice_FC * COALESCE(NULLIF(ASP_FC,0),1)
						    END
			    {3};"
            , _dev
            , destTable
            , retailPrice.RetailPrice.ToString()
            , where);

            return cmd;
        }

        public string UpdateSalesUnits(EditorParameterModel editor, string baseTable, string destTable, string innerTableName, bool isFrozen, string wherePatch = "")
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam);
            if (!string.IsNullOrEmpty(wherePatch))
            {
                where = $"{where} AND {wherePatch}";
            }

            var targetUnits = int.Parse(editor.SalesU.First().SalesU);

            cmd = string.Format(@"
                UPDATE forecast{0}.{2}
			    SET SalesUnits_FC = CASE WHEN COALESCE({4}, 0) = 0  THEN 0 
                                        ELSE COALESCE(SalesUnits_FC, 0) + (
                                            CASE WHEN SalesUnits_FC_Round < (ABS({4} - Current_Sum_SalesUnits_FC) - SalesUnits_FC_Cumulative_Sum)
                                                        THEN SalesUnits_FC_Round
                                                 ELSE CASE WHEN (ABS({4} - Current_Sum_SalesUnits_FC) - SalesUnits_FC_Cumulative_Sum) < 0
                                                                THEN 0
                                                           ELSE (ABS({4} - Current_Sum_SalesUnits_FC) - SalesUnits_FC_Cumulative_Sum)
                                                      END
                                            END) * (CASE WHEN ({4} - Current_Sum_SalesUnits_FC) < 0 THEN -1 ELSE 1 END)
                                    END
                FROM Forecast{0}.{6}
                WHERE ItemID = Edit_ItemID AND StoreID = Edit_StoreID AND FiscalWk = Edit_FiscalWk;
                "
            , _dev //0
            , baseTable //1
            , destTable //2
            , where //3
            , targetUnits //4
            , isFrozen ? "units_fc_vendor" : "salesunits_fc" //5
            , innerTableName //6
            );

            return cmd;
        }

        public string UpdateSalesUVar(EditorParameterModel editor)
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            decimal salesUVar = Convert.ToDecimal(editor.SalesUVar.First<ESalesUVar>().SalesUVar) * Convert.ToDecimal(.01);
            string where = CreateWhereClause(dtparam);

            cmd = string.Format(@" 
			UPDATE forecast{0}.tbl_allvendors
			SET  Units_FC_LOW = SalesUnits_TY*(1+({1}))
				,Units_FC_Vendor = SalesUnits_TY*(1+({1}))
				,SalesUnits_FC = SalesUnits_TY*(1+({1}))
			{2}
			", _dev, salesUVar, where);

            return cmd;
        }

        public string UpdateTutorial(Tutorial original, Tutorial edit)
        {
            edit.Intro = edit.Intro.Replace("'", "''");
            edit.Title = edit.Title.Replace("'", "''");

            var cmd = string.Format(@"UPDATE Notifications{0}.tbl_Tutorials
                SET Title = $${1}$$, 
                Intro = $${2}$$, 
                TutorialGroup = $${3}$$, 
                Target = $${4}$$, 
                VideoLink = $${5}$$, 
                LastEdit = CAST($${6}$$ AS TIMESTAMP)
                WHERE ID = {7} and Target = $${8}$$;",
                _dev,
                edit.Title,
                edit.Intro,
                edit.TutorialGroup,
                toolName,
                edit.VideoLink,
                edit.LastEdit,
                original.TutorialId,
                toolName);

            return cmd;
        }

        public string UpdateMMComments(EditorParameterModel editor, string destTable, string wherePatch = "")
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam, "", false);
            if (!string.IsNullOrEmpty(wherePatch))
            {
                where = $"{where} AND {wherePatch}";
            }
            var mmComment = editor.MMComments.First<EMMComments>();
            string newComment = mmComment.MMComments.ToString();
            if (string.IsNullOrWhiteSpace(newComment))
            {
                newComment = "null";
            }
            else
            {
                newComment = "$$" + newComment + "$$";
            }

            cmd = string.Format(@"
            UPDATE forecast{0}.{1}
            SET MM_Comments = {2} {3};", _dev, destTable, newComment, where);

            return cmd;
        }

        public string UpdateVendorComments(EditorParameterModel editor, string destTable)
        {
            string cmd = string.Empty;
            DTParameterModel dtparam = new DTParameterModel();
            dtparam = editor.EditorToDTParam(editor);
            string where = CreateWhereClause(dtparam, "", false);
            var vendorComment = editor.VendorComments.First<EVendorComments>();
            string newComment = vendorComment.VendorComments.ToString();
            if (string.IsNullOrWhiteSpace(newComment))
            {
                newComment = "null";
            }
            else
            {
                newComment = "$$" + newComment + "$$";
            }

            cmd = string.Format(@"
            UPDATE forecast{0}.{1}
            SET Vendor_Comments = {2} {3} AND GMSVenId = {4};", _dev, destTable, newComment, where, editor.GMSVenID);

            return cmd;
        }

        public string UpdateVendorImportProcess(ImportProcess importProcess)
        {
            var cmd = string.Format(@"UPDATE Forecast{0}.import_Processes
                SET ProcessId = {1},
                FileName = $${2}$$,
                StartTime = CAST($${3}$$ AS TIMESTAMP)
                WHERE GMSVenID = {4}; ",
                _dev, importProcess.ProcessId, importProcess.FileName, importProcess.StartTime, importProcess.GMSVenID);

            return cmd;
        }

        public string UpdateViewedNotification(UserInfo userInfo, ViewedNotification notif)
        {
            var cmd = "";

            notif.NotifIds.ForEach(n => cmd += string.Format(@"
                INSERT INTO Notifications{0}.tbl_Notified_Users (GMSVenID, UserName, NotificationID, TimeStamp, LastEdit) 
                VALUES ({1}, lower($${2}$$), {3}, CAST($${4}$$ AS TimeStamp), CAST($${5}$$ AS TimeStamp));
                ", _dev, userInfo.GMSVenId, userInfo.UserName, n, notif.TimeStamp, GetTimestamp()));

            return cmd;
        }

        #endregion UPDATE

        #region DELETE

        public string DeleteDlEvent(int id)
        {
            var cmd = string.Format(@"DELETE FROM Notifications{0}.tbl_DL_Events WHERE ID = {1}; ", _dev, id);

            return cmd;
        }

        public string DeleteTutorial(int id)
        {
            var cmd = string.Format(@"DELETE FROM Notifications{0}.tbl_Tutorials WHERE ID = {1}; ", _dev, id);

            return cmd;
        }

        public string DeleteNotification(int id)
        {
            var cmd = string.Format(@"DELETE FROM Notifications{0}.tbl_Notifications WHERE NotifID = {1}; ", _dev, id);

            return cmd;
        }

        #endregion DELETE

        #region EXPORT

        /// <summary>
        /// Creates a table that acts as a temporary table for an export. This table should be dropped after 
        /// it's not being utilized anymore. 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnData"></param>
        /// <param name="exportInfo"></param>
        /// <returns></returns>
        public string CreateExportTableWithData(string tableName, string columnData, ExportInfo exportInfo)
        {
            var orderBy = exportInfo.OrderByColumns.Length > 0 ? $"ORDER BY {exportInfo.OrderByColumns}" : "";

            var cmd = string.Format(@"
                CREATE TABLE Forecast{0}.{1} ({2})
                {5}; 
    
                INSERT INTO Forecast{0}.{1} ({4})
                {3} ; "
                , _dev, tableName, columnData, exportInfo.Select, exportInfo.ColumnsToBuild, orderBy);

            return cmd;
        }

        /// <summary>
        /// Deletes an actual table that acts as a temp table for the template downloads
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string DropExportTableWithData(string tableName)
        {
            var cmd = string.Format(@"DROP TABLE IF EXISTS Forecast{0}.{1};" , _dev, tableName);

            return cmd;
        }

        /// <summary>
        /// Builds query to export from the forecast data view.
        /// </summary>
        /// <param name="param">incoming parameters from the datatable</param>
        /// <param name="tempTableName">the name of the view to be queried</param>
        /// <param name="startRowNum">the starting row for the query batch</param>
        /// <param name="endRowNum">the ending row for the query batch</param>
        /// <param name="limit">max rows per file</param>
        /// <returns>select query for export</returns>
        public string GetExportReportForecastColumns()
        {
            var cmd = string.Format(@"
                ForecastID,
                VendorDesc,
                ItemID,
                FiscalWk,
                FiscalMo,
                FiscalQtr,
                MD,
                MM,
                Region,
                District,
                Patch,
                ParentID,
                ProdGrpID,
                AssrtID,
                ItemDesc,
                ItemConcat,
                AssrtDesc,
                AssrtConcat,
                ParentDesc,
                ProdGrpDesc,
                ProdGrpConcat,
                SalesUnits_TY,
                SalesUnits_LY,
                SalesUnits_2LY,
                SalesUnits_FC,
                SalesUnits_Var,
                SalesDollars_TY,
                SalesDollars_LY,
                SalesDollars_2LY,
                SalesDollars_FR_FC,
                SalesDollars_Curr,
                SalesDollars_Var,
                CAGR,
                ASP_TY,
                ASP_LY,
                ASP_FC,
                ASP_Var,
                RetailPrice_TY,
                RetailPrice_LY,
                RetailPrice_FC,
                RetailPrice_Var,
                RetailPrice_Erosion_Rate,
                SalesDollars_FR_TY,
                SalesDollars_FR_LY,
                MarginDollars_FR_TY,
                MarginDollars_FR_LY,
                MarginDollars_FR_Var,
                Cost_TY,
                Cost_LY,
                Cost_FC,
                Cost_Var,
                Margin_Dollars_TY,
                Margin_Dollars_LY,
                Margin_Dollars_Curr,
                Margin_Dollars_FR,
                Margin_Dollars_Var_Curr,
                Margin_Percent_TY,
                Margin_Percent_LY,
                Margin_Percent_Curr,
                Margin_Percent_FR,
                Margin_Percent_Var,
                Turns_TY,
                Turns_LY,
                Turns_FC,
                Turns_Var,
                SellThru_TY,
                SellThru_LY,
                Dollars_FC_DL,
                Dollars_FC_LOW,
                Dollars_FC_Vendor,
                Units_FC_DL,
                Units_FC_LOW,
                Units_FC_Vendor,
                Dollars_FC_DL_Var,
                Dollars_FC_LOW_Var,
                Dollars_FC_Vendor_Var,
                Units_FC_DL_Var,
                Units_FC_LOW_Var,
                Units_FC_Vendor_Var,
                ReceiptUnits_TY,
                ReceiptUnits_LY,
                ReceiptDollars_LY,
                ReceiptDollars_TY,
                PriceSensitivityImpact,
                PriceSensitivityPercent,
                VBUPercent,
                Vendor_Comments,
                MM_Comments");

            return cmd.Replace("\r\n", "").Replace("\t", "").Replace("\n", "").Replace(" ", "");
        }

        /// <summary>
        /// Builds query to create a view used for the export.
        /// </summary>
        /// <param name="param">The incoming datatable parameters from the site</param>
        /// <returns>create view query string</returns>
        public string GetExportReportForecastDownload(DTParameterModel param)
        {
            var vendRotatorReq = new List<string> { "ItemID", "MM" };
            var mmRotatorReq = new List<string> { "ItemID", "MM", "VendorDesc" };
            var selectedColumns = Exports.Exports.GetColumns(param);
            var vendMessage = "'\"Must Rotate on Item, MM\"' AS Vendor_Comments";
            var mmMessage = "'\"Must Rotate on Item, MM, Vendor\"' AS MM_Comments";

            // Some comments have commas in them so we need to sarround the comment in 
            // double quotes to prevent the comma sepparated coments from being put in to 
            // different columns in the CSV file.
            var vendorCommentsSelect = "'\"' || MAX(Vendor_Comments) || '\"' AS Vendor_Comments";
            var mmCommentsSelect = "'\"' || MAX(MM_Comments) || '\"' AS MM_Comments";

            // Here we check if the user is a vendor, mm, or md and check if they're 
            // rotating on the proper columns. 
            if (param.GMSVenID == 0)
            {
                if (!selectedColumns.ContainsAllIgnoreCase(mmRotatorReq))
                    vendorCommentsSelect = mmCommentsSelect = mmMessage;
            }
            else if (param.GMSVenID > 0)
            {
                if (!selectedColumns.ContainsAllIgnoreCase(vendRotatorReq))
                    vendorCommentsSelect = mmCommentsSelect = vendMessage;
            }

            string cmd = string.Empty;
            string select = CreateSelectStatement(param);
            string where = CreateWhereClause(param);

            if (!select.Contains("-1 as VendorDesc"))
                select = select.Replace("VendorDesc,", "'\"' || VendorDesc || '\"' AS VendorDesc,");

            string order = CreateOrderByClause(param);
            string groupby = CreateGroupByStatement(param);

            cmd = string.Format(@"SELECT {4}
                {8} || MAX(ItemDesc) || {8} AS ItemDesc,
                {8} || MAX(ItemConcat) || {8} AS ItemConcat,
                {8} || MAX(AssrtDesc) || {8} AS AssrtDesc,
                {8} || MAX(AssrtConcat) || {8} AS AssrtConcat,
                {8} || MAX(ParentDesc) || {8} AS ParentDesc,
                {8} || MAX(ProdGrpDesc) || {8} AS ProdGrpDesc,
                {8} || MAX(ProdGrpConcat) || {8} AS ProdGrpConcat,
                SUM(SalesUnits_TY)  AS SalesUnits_TY,
                SUM(SalesUnits_LY)  AS SalesUnits_LY,
                SUM(SalesUnits_2ly) AS SalesUnits_2ly,
                SUM(SalesUnits_FC)  AS SalesUnits_FC,
                (((SUM(coalesce(SalesUnits_FC,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0))*100)::NUMERIC(18,1) AS SalesUnits_Var,
                SUM(SalesDollars_TY)    AS SalesDollars_TY,
                SUM(SalesDollars_LY)    AS SalesDollars_LY,
                SUM(SalesDollars_2ly)   AS SalesDollars_2ly,
                SUM(SalesDollars_FR_FC) AS SalesDollars_FR_FC,
                SUM(SalesDollars_Curr)  AS SalesDollars_Curr,
                (((SUM(SalesDollars_Curr) -SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)) *100)::NUMERIC(18,1) AS SalesDollars_Var,
                ((((SUM(SalesDollars_TY)/NULLIF(SUM(SalesDollars_2ly),0))^(1/2))-1)*100)::!NUMERIC(18,1) AS CAGR,
                (SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2) AS ASP_TY,
                (SUM(SalesDollars_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) AS ASP_LY,
                CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END AS ASP_FC,
                (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(ASP_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_Curr)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END-(SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
                /NULLIF((SUM(SalesDollars_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS ASP_Var,
                CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(RetailPrice_TY,0))::NUMERIC(18,2)
                ELSE (SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                END AS RetailPrice_TY,
                CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(RetailPrice_LY,0))::NUMERIC(18,2)
                ELSE (SUM(SalesDollars_FR_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                END AS RetailPrice_LY,		                  
                CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2)
				ELSE (SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2)
				END AS RetailPrice_FC,
		        (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0))=0 THEN coalesce(AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2),0)
				ELSE coalesce(SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0),0)::NUMERIC(18,2)
				END-(SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2))
				/NULLIF((SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2),0))*100)::NUMERIC(18,1) AS RetailPrice_Var,
                CASE WHEN SUM(SalesDollars_TY) <= 0 THEN 0 ELSE (COALESCE((sum(SalesDollars_FR_TY)- sum(SalesDollars_TY)) /nullif(sum(SalesDollars_FR_TY),0),0)*100)::NUMERIC(18,1) END AS RetailPrice_Erosion_Rate,
				SUM(SalesDollars_FR_TY)::NUMERIC(18,2) AS SalesDollars_FR_TY,
				SUM(SalesDollars_FR_LY)::NUMERIC(18,2) AS SalesDollars_FR_LY,
                SUM(Margin_Dollars_FR_TY) AS MarginDollars_FR_TY,
                SUM(Margin_Dollars_FR_LY) AS MarginDollars_FR_LY,
                (((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_FR_TY))/NULLIF(SUM(Margin_Dollars_FR_TY),0))*100)::NUMERIC(18,1) AS MarginDollars_FR_Var,
                CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                END AS Cost_TY,
                CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(Cost_LY,0))::NUMERIC(18,2)
                ELSE (SUM(COGS_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2)
                END AS Cost_LY,
                CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
				ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END AS Cost_FC,
                (((CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
				ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
				END - CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                            ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END)
                    / NULLIF(CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                                    ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)END,0))
                    *100)::NUMERIC(18,1) AS Cost_Var,
                SUM(Margin_Dollars_TY)         AS Margin_Dollars_TY,
                SUM(Margin_Dollars_LY)         AS Margin_Dollars_LY,
                SUM(Margin_Dollars_Curr)       AS Margin_Dollars_Curr,
                SUM(Margin_Dollars_FR)         AS Margin_Dollars_FR,
                (((SUM(coalesce(Margin_Dollars_Curr,0))-SUM(Margin_Dollars_TY))/NULLIF(SUM(Margin_Dollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Dollars_Var_Curr,
                (((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))*100)::NUMERIC(18,1) AS Margin_Percent_TY,
                (((SUM(SalesDollars_LY)-SUM(coalesce(COGS_LY,0)))/NULLIF(SUM(SalesDollars_LY),0))*100)::NUMERIC(18,1) AS Margin_Percent_LY,
                (((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))*100)::NUMERIC(18,1) AS Margin_Percent_Curr,
                (((SUM(SalesDollars_FR_FC)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_FR_FC),0))*100)::NUMERIC(18,1) AS Margin_Percent_FR,
                (((((SUM(SalesDollars_Curr)-SUM(coalesce(COGS_FC,0)))/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,1)-((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1))/NULLIF(((SUM(SalesDollars_TY)-SUM(coalesce(COGS_TY,0)))/NULLIF(SUM(SalesDollars_TY),0))::NUMERIC(18,1),0))*100)::NUMERIC(18,1) AS Margin_Percent_Var,
                ((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1) AS Turns_TY,
                ((SUM(COGS_LY)/NULLIF(SUM(OHC_LY),0))*365)::DECIMAL(18,1) AS Turns_LY,
                ((SUM(COGS_FC)/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1) AS Turns_FC,
                ((((SUM(coalesce(COGS_FC,0))/NULLIF(SUM(OHC_FC),0))*365)::DECIMAL(18,1)-((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1))/NULLIF(((SUM(COGS_TY)/NULLIF(SUM(OHC_TY),0))*365)::DECIMAL(18,1),0))::DECIMAL(18,1) AS Turns_Var,
                (SUM(SalesUnits_TY)/NULLIF(SUM(ShipsGross_TY),0) *100)::DECIMAL(18,1) AS SellThru_TY,
                (SUM(SalesUnits_LY)/NULLIF(SUM(ShipsGross_LY),0) *100)::DECIMAL(18,1) AS SellThru_LY,
                SUM(Dollars_FC_DL) AS Dollars_FC_DL,
                SUM(Dollars_FC_LOW) AS Dollars_FC_LOW,
                SUM(Dollars_FC_Vendor) AS Dollars_FC_Vendor,
                SUM(Units_FC_DL) AS Units_FC_DL,
                SUM(Units_FC_LOW) AS Units_FC_LOW,
                SUM(Units_FC_Vendor) AS Units_FC_Vendor,
                ((SUM(coalesce(Dollars_FC_DL,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_DL_Var,
                ((SUM(coalesce(Dollars_FC_LOW,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_LOW_Var,
                ((SUM(coalesce(Dollars_FC_Vendor,0))-SUM(SalesDollars_TY))/NULLIF(SUM(SalesDollars_TY),0)*100)::DECIMAL(18,1) AS Dollars_FC_Vendor_Var,
                ((SUM(coalesce(Units_FC_DL,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_DL_Var,
                ((SUM(coalesce(Units_FC_LOW,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_LOW_Var,
                ((SUM(coalesce(Units_FC_Vendor,0))-SUM(SalesUnits_TY))/NULLIF(SUM(SalesUnits_TY),0)*100)::DECIMAL(18,1) AS Units_FC_Vendor_Var,
                SUM(ReceiptUnits_TY)   AS ReceiptUnits_TY,
                SUM(ReceiptUnits_LY)   AS ReceiptUnits_LY,
                SUM(ReceiptDollars_LY) AS ReceiptDollars_LY,
                SUM(ReceiptDollars_TY) AS ReceiptDollars_TY,
                CASE WHEN COALESCE(sum(SalesDollars_Curr),0)=0 THEN
                    CASE WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =1
                    THEN 'Moderate Decrease'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =2
                    THEN 'Mild Decrease'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =3
                    THEN 'Mild Increase'
                    ELSE 'Moderate Increase'
                    END
                    ELSE CASE WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =1
                    THEN 'Moderate Decrease'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =2
                    THEN 'Mild Decrease'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =3
                    THEN 'Mild Increase'
                    ELSE 'Moderate Increase'
                    END
                    END AS PriceSensitivityImpact,
                    CASE WHEN sum(SalesDollars_Curr) IS NULL THEN
                    CASE
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =1
                    THEN '-10%'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =2
                    THEN '-5%'
                    WHEN AVG(PriceSensitivity)::NUMERIC(18,0) =3
                    THEN '5%'
                    ELSE '10%'
                    END
                    ELSE CASE
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =1
                    THEN '-10%'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =2
                    THEN '-5%'
                    WHEN (SUM(PriceSensitivity*SalesDollars_Curr)/NULLIF(SUM(SalesDollars_Curr),0))::NUMERIC(18,0) =3
                    THEN '5%'
                    ELSE '10%'
                    END
                    END AS PriceSensitivityPercent,
               100 AS VBUPercent,
                {6},
                {7}
                FROM forecast{0}.{1}_calcs_b0 f
                {2}
                {5}
                {3};", _dev, param.TableName, where, order, select, groupby, vendorCommentsSelect, mmCommentsSelect, "'\"'");


            return cmd.Replace("\r\n", " ").Replace("\t", " ").Replace("\n", " ");
        }

        /// <summary>
        /// Export sql string for the Item Region MM export
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetExportReportItemRegionMM(DTParameterModel param)
        {
            var where = CreateWhereClause(param);
            var cmd = string.Format(@"
                SELECT 
                    ItemID, 
                    Region,
                    MM, 
                    SUM(SalesUnits_FC) AS 'SalesUnits_FC' 
                    
                FROM Forecast{0}.{1}
                {2}
                GROUP BY ItemID,Region, MM
                ORDER BY ItemID, Region, MM; ", _dev, param.TableName, where, $"{"'\"'"}");

            return cmd;
        }

        /// <summary>
        /// Export sql string for the Item MM Total export
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetExportReportItemMMTotal(DTParameterModel param)
        {
            var where = CreateWhereClause(param);
            var cmd = string.Format(@"
                SELECT 
                    ItemID, 
                    MM, 
                    SUM(SalesUnits_FC) AS 'SalesUnits_FC', 
                    {3} || MAX(Vendor_Comments) || {3} AS 'Vendor_Comments'
                FROM Forecast{0}.{1}
                {2}
                GROUP BY ItemID, MM 
                ORDER BY ItemID, MM; ", _dev, param.TableName, where, $"{"'\"'"}");

            return cmd;
        }

        /// <summary>
        /// Gets an export string for the Item MM Week export
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetExportReportItemMMWeek(DTParameterModel param)
        {
            var where = CreateWhereClause(param);

            var cmd = string.Format(@"
                SELECT 
                    ItemID, 
                    MM, 
                    FiscalWk, 
                    SUM(SalesUnits_FC) AS 'SalesUnits_FC' 
                FROM Forecast{0}.{1}
                {2}
                GROUP BY ItemID, MM, FiscalWk
                ORDER BY ItemID, MM, FiscalWk; ", _dev, param.TableName, where);

            return cmd;
        }

        public string GetExportReportItemPatchOwnership(DTParameterModel param)
        {
            var where = CreateWhereClause(param);
            var cmd = string.Format(@"select distinct ItemID, ItemDesc, Patch from Forecast{0}.itemPatch_Ownership
                where GMSVenID = {1}
                order by ItemID, Patch;", _dev, param.GMSVenID);

            return cmd;
        }

        /// <summary>
        /// Gets a SQL command string for the Item Patch Total export
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetExportReportItemPatchTotal(DTParameterModel param)
        {
            var where = CreateWhereClause(param);

            var cmd = string.Format(@"
                SELECT 
                    ItemID, 
                    Patch, 
                    CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(Cost_LY,0))::NUMERIC(18,2)
                    ELSE (SUM(COGS_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                    END AS Cost_LY,
                    CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                    ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                    END AS Cost_TY,
                    CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
					ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END AS Cost_FC,
                    CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(RetailPrice_LY,0))::NUMERIC(18,2)
                    ELSE (SUM(SalesDollars_FR_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                    END AS RetailPrice_LY,
                    CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(RetailPrice_TY,0))::NUMERIC(18,2)
                    ELSE (SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                    END AS RetailPrice_TY,
                    CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2)
					END AS RetailPrice_FC,
                    SUM(SalesUnits_FC) as 'SalesUnits_FC'
                from Forecast{0}.{1}_calcs_b0 
                {2}
                GROUP BY ItemID, Patch 
                ORDER BY ItemID, Patch; ", _dev, param.TableName, where);

            return cmd;
        }

        /// <summary>
        /// Gets an export string for the Item Patch Week template
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetExportReportItemPatchWeek(DTParameterModel param)
        {
            var where = CreateWhereClause(param);

            var cmd = string.Format(@"
                SELECT 
                    ItemID, 
                    Patch, 
                    FiscalWk, 
                    CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(Cost_LY,0))::NUMERIC(18,2)
                    ELSE (SUM(COGS_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                    END AS Cost_LY,
                    CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(Cost_TY,0))::NUMERIC(18,2)
                    ELSE (SUM(COGS_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                    END AS Cost_TY,                    
                    CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(Cost_FC,0))::NUMERIC(18,2)
					ELSE (SUM(COGS_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2) 
					END AS Cost_FC,
                    CASE WHEN SUM(COALESCE(SalesUnits_LY,0)) = 0 THEN AVG(NULLIF(RetailPrice_LY,0))::NUMERIC(18,2)
                    ELSE (SUM(SalesDollars_FR_LY)/NULLIF(SUM(SalesUnits_LY),0))::NUMERIC(18,2) 
                    END AS RetailPrice_LY,
                    CASE WHEN SUM(COALESCE(SalesUnits_TY,0)) = 0 THEN AVG(NULLIF(RetailPrice_TY,0))::NUMERIC(18,2)
                    ELSE (SUM(SalesDollars_FR_TY)/NULLIF(SUM(SalesUnits_TY),0))::NUMERIC(18,2)
                    END AS RetailPrice_TY,
                    CASE WHEN SUM(COALESCE(SalesUnits_FC,0)) = 0 THEN AVG(NULLIF(RetailPrice_FC,0))::NUMERIC(18,2)
					ELSE (SUM(SalesDollars_FR_FC)/NULLIF(SUM(SalesUnits_FC),0))::NUMERIC(18,2)
					END AS RetailPrice_FC,
                    SUM(SalesUnits_FC) AS 'SalesUnits_FC'    
                FROM Forecast{0}.{1}_calcs_b0 
                {2}
                GROUP BY ItemID, Patch, FiscalWk
                ORDER BY ItemID, Patch, FiscalWk; ", _dev, param.TableName, where);

            return cmd;
        }

        /// <summary>
        /// Builds a query to select all items that have Units_FC_Vendor or 0 or null (new items without forecast data).
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetExportReportNewItems(DTParameterModel param)
        {
            string cmd = String.Empty;
            cmd = string.Format(@"
                SELECT ItemID, Patch, MM 
                FROM (
                        SELECT ItemId, Patch, MM, SUM(Units_FC_Vendor) AS Units_FC_Vendor 
                        FROM Forecast{0}.{1}
                        GROUP BY ItemId, Patch, MM
                ) items
                WHERE COALESCE(items.Units_FC_Vendor, 0) = 0
                ORDER BY ItemId, Patch, MM; 
                ", _dev, param.TableName);

            return cmd;
        }

        public string GetNewItemUploadColumns()
        {
            var cmd = string.Format(@"
                SELECT ColumnName from Forecast{0}.config_Templates
                where TemplateType = 'NewItemUploadColumns'
                and RequiredFlag = true
                order by SortOrder;", _dev);

            return cmd;
        }

        public string GetDBColumnNames(string templateType)
        {
            var cmd = string.Format(@"
                SELECT ColumnName, FileName from Forecast{0}.config_Templates
                where TemplateType = $${1}$$
                and RequiredFlag = true
                order by SortOrder;", _dev, templateType);

            return cmd;
        }

        public string UpdateExistingNotifiedUsers(List<NotifiedUser> notifiedUsers, string timestamp)
        {
            var cmd = "";
            notifiedUsers.ForEach(n =>
                cmd += string.Format(@"
                update Notifications{0}.tbl_Notified_Users set LastEdit = CAST($${1}$$ AS TimeStamp)
                where ID = {2};", _dev, timestamp, n.UNID));

            return cmd;
        }

        /// <summary>
        /// Gets a <seealso cref="Dictionary{TKey, TValue}"/> of Key Value pairs. The Key is the columns name and 
        /// the Value is the column datatype.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetForecastTableColumnInfo()
        {
            var columnList = new Dictionary<string, string>()
            {
                {"ForecastID", "VARCHAR(100)" },
                {"GMSVenID", "INT"},
                {"VBU", "INT"},
                {"OHU_FC", "INT"},
                {"OHC_TY", "NUMERIC(18,2)"},
                {"OHC_LY", "NUMERIC(18,2)"},
                {"ShipsGross_TY", "INT"},
                {"ShipsGross_LY", "INT"},
                {"VendorDesc", "VARCHAR(100)"},
                {"ItemID", "INT"},
                {"ItemDesc", "VARCHAR(102)"},
                {"ItemConcat", "VARCHAR(102)"},
                {"FiscalWk", "INT"},
                {"FiscalMo", "INT"},
                {"FiscalQtr", "INT"},
                {"MD", "VARCHAR(100)"},
                {"MM", "VARCHAR(100)"},
                {"Region", "VARCHAR(100)"},
                {"District", "VARCHAR(100)"},
                {"Patch", "VARCHAR(100)"},
                {"ProdGrpID", "INT"},
                {"ProdGrpDesc", "VARCHAR(102)"},
                {"ProdGrpConcat", "VARCHAR(102)"},
                {"AssrtID", "INT"},
                {"AssrtDesc", "VARCHAR(52)"},
                {"AssrtConcat", "VARCHAR(102)"},
                {"ParentID", "INT"},
                {"ParentDesc", "VARCHAR(52)"},
                {"ParentConcat", "VARCHAR(102)"},
                {"SalesUnits_TY", "INT"},
                {"SalesUnits_LY", "INT"},
                {"SalesUnits_2LY", "INT"},
                {"SalesUnits_FC", "INT"},
                {"SalesUnits_Var", "NUMERIC(18,1)"},
                {"SalesDollars_TY", "NUMERIC(18,2)"},
                {"SalesDollars_LY", "NUMERIC(18,2)"},
                {"SalesDollars_2LY", "NUMERIC(18,2)"},
                {"SalesDollars_FR_FC", "NUMERIC(18,2)"},
                {"SalesDollars_Curr", "NUMERIC(18,2)"},
                {"SalesDollars_Var", "NUMERIC(18,2)"},
                {"CAGR", "INT"},
                {"Asp_TY", "NUMERIC(18,2)"},
                {"Asp_LY", "NUMERIC(18,2)"},
                {"Asp_FC", "NUMERIC(18,2)"},
                {"Asp_Var", "NUMERIC(18,2)"},
                {"RetailPrice_TY", "NUMERIC(18,2)"},
                {"RetailPrice_LY", "NUMERIC(18,2)"},
                {"RetailPrice_FC", "NUMERIC(18,2)"},
                {"RetailPrice_Var", "NUMERIC(18,2)"},
                {"RetailPrice_Erosion_Rate", "NUMERIC(18,2)"},
                {"SalesDollars_FR_TY", "NUMERIC(18,2)"},
                {"SalesDollars_FR_LY", "NUMERIC(18,2)"},
                {"MarginDollars_FR_TY", "NUMERIC(18,2)"},
                {"MarginDollars_FR_LY", "NUMERIC(18,2)"},
                {"MarginDollars_FR_Var", "NUMERIC(18,2)"},
                {"Cost_TY", "NUMERIC(18,2)"},
                {"Cost_LY", "NUMERIC(18,2)"},
                {"Cost_FC", "NUMERIC(18,2)"},
                {"Cost_Var", "NUMERIC(18,2)"},
                {"Margin_Dollars_TY", "NUMERIC(18,2)"},
                {"Margin_Dollars_LY", "NUMERIC(18,2)"},
                {"Margin_Dollars_Curr", "NUMERIC(18,2)"},
                {"Margin_Dollars_FR", "NUMERIC(18,2)"},
                {"Margin_Dollars_Var_Curr", "NUMERIC(18,2)"},
                {"Margin_Percent_TY", "NUMERIC(18,2)"},
                {"Margin_Percent_LY", "NUMERIC(18,2)"},
                {"Margin_Percent_Curr", "NUMERIC(18,2)"},
                {"Margin_Percent_FR", "NUMERIC(18,2)"},
                {"Margin_Percent_Var", "NUMERIC(18,2)"},
                {"Turns_TY", "NUMERIC(18,2)"},
                {"Turns_LY", "NUMERIC(18,2)"},
                {"Turns_FC", "NUMERIC(18,2)"},
                {"Turns_Var", "NUMERIC(18,2)"},
                {"SellThru_TY", "NUMERIC(18,2)"},
                {"SellThru_LY", "NUMERIC(18,2)"},
                {"Units_FC_DL", "INT"},
                {"Units_FC_DL_Var", "NUMERIC(18,2)"},
                {"Units_FC_LOW", "INT"},
                {"Units_FC_LOW_Var", "NUMERIC(18,2)"},
                {"Units_FC_Vendor", "INT"},
                {"Units_FC_Vendor_Var", "NUMERIC(18,2)"},
                {"Dollars_FC_DL", "NUMERIC(18,2)"},
                {"Dollars_FC_DL_Var", "NUMERIC(18,2)"},
                {"Dollars_FC_LOW", "NUMERIC(18,2)"},
                {"Dollars_FC_LOW_Var", "NUMERIC(18,2)"},
                {"Dollars_FC_Vendor", "NUMERIC(18,2)"},
                {"Dollars_FC_Vendor_Var", "NUMERIC(18,2)"},
                {"ReceiptUnits_TY", "INT"},
                {"ReceiptUnits_LY", "INT"},
                {"ReceiptDollars_TY", "NUMERIC(18,2)"},
                {"ReceiptDollars_LY", "NUMERIC(18,2)"},
                {"PriceSensitivity", "FLOAT"},
                {"PriceSensitivityImpact", "VARCHAR(100)"},
                {"PriceSensitivityPercent", "VARCHAR(100)"},
                {"VBUPercent", "NUMERIC(18,2)"},
                {"Vendor_Comments", "VARCHAR(550)"},
                {"MM_Comments", "VARCHAR(550)"},
                {"TimeStamp", "TIMESTAMP(6)"},
                {"Action", "VARCHAR(5)" },
                {"PrimaryVendor", "VARCHAR(12)" },
                {"RequestingOwners", "VARCHAR(100)" }
            };
            return columnList;
        }

        public string GetTempTableCount(string tableName)
        {
            return string.Format(@"SELECT COUNT(*) FROM Forecast{0}.{1}; ", _dev, tableName);
        }

        /// <summary>
        /// Gets the next number of rows specified from and inclusive index.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="orderColumns"></param>
        /// <param name="index"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public string GetDataFromIndexWithLimit(string tableName, string orderColumns, int index, int limit)
        {
            var orderBy = orderColumns.Length > 0 ? $"ORDER BY {orderColumns} " : "";

            var cmd = string.Format(@"SELECT * FROM Forecast{0}.{1} {4}
                OFFSET {2} LIMIT {3}; ", _dev, tableName, index, limit, orderBy);

            return cmd;
        }

        #endregion EXPORT

        #region IMPORT

        /// <summary>
        /// Stages valid and invalid data into sepparete tables.
        /// 
        /// NOTE: Need to account for new items being inserted that already exist in the vendors table but has different alignment
        /// in their file.
        /// 
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="fileName"></param>
        /// <param name="batchId"></param>
        /// <param name="backendHeaders"></param>
        /// <param name="isForecastPreFreeze"></param>
        /// <returns></returns>
        public string CreateNewItemsUploadStage(EditorParameterModel editor, string fileName, string batchId, string backendHeaders, Boolean isForecastPreFreeze)
        {
            var timeStamp = GetTimestamp(DateTime.Now);

            var cmd = string.Format(@"
                drop table if exists error_load_rejected_data;
                drop table if exists flex_new_items_upload;
                create flex local temp table flex_new_items_upload(GMSVenID int, Item int, ""Item Desc"" varchar(50), Patch varchar(12), ProdGrp int, Parent int, Assortment int, ""Primary Vendor"" varchar(12)) on commit preserve rows;
                copy flex_new_items_upload(__raw__, GMSVenID as {1}, Item, ""Item Desc"", Patch, ProdGrp, Parent, Assortment, ""Primary Vendor"" )
                from $$/mnt_yoda/FTPVertica{0}/Forecast/{3}$$ ON ANY NODE
                parser PUBLIC.FCSVPARSER(reject_on_materialized_type_error=true, reject_on_duplicate=true, header=true)
                rejected data as table error_load_rejected_data SKIP 1 no commit;
                   
                /* Insert flex contents into regular table for performance */
                create local temp table tmp_new_items_upload on commit preserve rows as
                select Item as ItemID, upper(""Item Desc"") as ItemDesc, Patch, ProdGrp as ProdGrpID, Parent as ParentID, Assortment as AssrtID, ""Primary Vendor"" as primaryvendor
                from flex_new_items_upload;

                drop table if exists temp_stage_errors;
                create local temp table temp_stage_errors
                (
                    ProdGrpID varchar(50)
                    , AssrtID varchar(50)
                    , ParentID varchar(50)
                    , Patch varchar(12)
                    , ItemID varchar(50)
                    , ItemDesc varchar(50)
                    , Primaryvendor varchar(12)
                    , DupsFlag boolean
                    , Error varchar(200)
                    , ErrorPriority int
                ) on commit preserve rows;

                /* Store any duplicate ItemID/Patch rows. */
                insert /*direct*/ into temp_stage_errors
                select ProdGrpID, AssrtID, ParentID, Patch, ItemID, ItemDesc, PrimaryVendor, true, 'Duplicate Item/Patch combination. Please remove all duplicates and upload a distinct Item/Patch combination.' as Error, 0 as ErrorPriority
                from (select ProdGrpID, AssrtID, ParentID, Patch, ItemID, ItemDesc, PrimaryVendor, count(ItemID) over (partition by ItemID,upper(patch)) as count from tmp_new_items_upload) dups
                where count > 1;
                
                /* Store any data type errors */
                insert into temp_stage_errors (Error, ErrorPriority, dupsflag  ,ItemID ,ItemDesc ,Patch ,ParentID ,ProdGrpID ,AssrtID ,PrimaryVendor)
                select 'One of the columns has an invalid data type.' as Reason
                        ,1
                        ,false
                        ,split_part(rejected_data,',',1)
                        ,split_part(rejected_data,',',2)
                        ,split_part(rejected_data,',',3)
                        ,split_part(rejected_data,',',4)
                        ,split_part(rejected_data,',',5)
                        ,split_part(rejected_data,',',6)
                        ,split_part(rejected_data,',',7)
                from error_load_rejected_data;

                /* Store any missing ProdGrpID, AssrtID, ParentID, Patch, ItemID, or ItemDesc data. */
                insert /*direct*/ into temp_stage_errors
                select ProdGrpID, AssrtID, ParentID, Patch, ItemID, ItemDesc, PrimaryVendor, false,
                    'Missing values for'
                    || case when ProdGrpID is null then ' ProdGrp' else '' end
                    || case when AssrtID is null then ' Assortment' else '' end
                    || case when ParentID is null then ' Parent' else '' end
                    || case when Patch is null then ' Patch' else '' end
                    || case when ItemID is null then ' Item' else '' end
                    || case when ItemDesc is null then ' Item Desc' else '' end 
                    || case when PrimaryVendor is null then ' Primary Vendor' else '' end
                    || '. Please enter valid values for each column.' as Error,
                    2 as ErrorPriority
                from tmp_new_items_upload
                where ProdGrpID is null OR AssrtID is null OR ParentID is null OR Patch is null OR ItemID is null OR ItemDesc is null or PrimaryVendor is null;

                /* Select items that have different Item Descriptions, AssrtID's, ParentID's, or ProdGrpID's */
                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false
                       ,'File contains different Item Desc values for the same item. Please provide one Item Desc value per Item.' as Error
                       , 5 as ErrorPriority
                from tmp_new_items_upload tni
                join (
                        select ItemID, upper(ItemDesc) as ItemDesc, count(ItemID) over (partition by ItemID) as count
                        from (select distinct ItemID, upper(ItemDesc) as ItemDesc from tmp_new_items_upload) d
                ) dups on tni.ItemID = dups.ItemID and upper(tni.ItemDesc) = upper(dups.ItemDesc) and dups.count > 1;
                
                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false
                       ,'File contains different ProdGrp values for the same item. Please provide one ProdGrp value per Item.' as Error
                       , 10 as ErrorPriority
                from tmp_new_items_upload tni
                join (
                        select ItemID, ProdGrpID, count(ItemID) over (partition by ItemID) as count
                        from (select distinct ItemID, ProdGrpID from tmp_new_items_upload) d
                ) dups on tni.ItemID = dups.ItemID and tni.ProdGrpID = dups.ProdGrpID and dups.count > 1;
                
                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false
                       ,'File contains different Parent values for the same item. Please provide one Parent value per Item.' as Error
                       , 15 as ErrorPriority
                from tmp_new_items_upload tni
                join (
                        select ItemID, ParentID, count(ItemID) over (partition by ItemID) as count
                        from (select distinct ItemID, ParentID from tmp_new_items_upload) d
                ) dups on tni.ItemID = dups.ItemID and tni.ParentID = dups.ParentID and dups.count > 1;

                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false
                       ,'File contains different Assortment values for the same item. Please provide one Assortment value per Item.' as Error
                       , 20 as ErrorPriority
                from tmp_new_items_upload tni
                join (
                        select ItemID, AssrtID, count(ItemID) over (partition by ItemID) as count
                        from (select distinct ItemID, AssrtID from tmp_new_items_upload) d
                ) dups on tni.ItemID = dups.ItemID and tni.AssrtID = dups.AssrtID and dups.count > 1;

                /* Store any rows that have a bad primary vendor */
                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor,false , 'Primary Vendor ''' || tni.PrimaryVendor|| ''' value is not valid. Please enter a ''Y'' to indicate Primary Vendor, or a ''N'' if not.' as Error, 25 as ErrorPriority
                from tmp_new_items_upload tni
                where upper(tni.PrimaryVendor) not in ('Y', 'N');

                /* Store any rows that have any bad ProdGrpID or AssrtID */
                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor,false,
                case when tni.ProdGrpID is null
                        then 'Missing values for ProdGrp. Please enter valid values for each column.'
                        else 'ProdGrp ''' || tni.ProdGrpID || ''' is not valid. Please enter a valid ProdGrp.'
                end as Error, 30 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct ProdGrpID from Forecast{0}.build_items) i on tni.ProdGrpID = i.ProdGrpID
                where i.prodgrpid is null;

                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc,PrimaryVendor, false,
                case when tni.AssrtID is null
                        then 'Missing values for Assortment. Please enter valid values for each column.'
                        else 'Assortment ''' || tni.AssrtID || ''' is not valid. Please enter a valid Assortment.'
                end as Error, 35 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct AssrtID from Forecast{0}.build_items) i on tni.AssrtID = i.AssrtID
                where i.AssrtID is null;

                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc,PrimaryVendor, false
                       ,'Item already has Item Desc ''' || i.ItemDesc || ''' associated with it. Please correct the Item Desc and reupload.' as Error, 39 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct ItemID, ItemDesc from Forecast{0}.{4}
                            union
                            select distinct ItemID, ItemDesc from Forecast{0}.itempatch_ownership_newitemslookup where gmsvenid = {1}
                            union
                            select distinct ItemID, ItemDesc from Forecast{0}.itempatch_overlap_newitemslookup where gmsvenid = {1}
                            ) i on tni.ItemID = i.ItemID
                where upper(i.ItemDesc)<>upper(tni.ItemDesc);

                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc,PrimaryVendor, false
                       ,'Item already has ProdGrp ''' || i.ProdGrpID || ''' associated with it. Please correct the ProdGrp and reupload.' as Error, 40 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct ItemID, ProdGrpID from Forecast{0}.{4}
                            union
                            select distinct ItemID, ProdGrpID from Forecast{0}.itempatch_ownership_newitemslookup where gmsvenid = {1}
                            union
                            select distinct ItemID, ProdGrpID from Forecast{0}.itempatch_overlap_newitemslookup where gmsvenid = {1}
                            ) i on tni.ItemID = i.ItemID
                where i.ProdGrpID<>tni.ProdGrpID;

                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc,PrimaryVendor, false
                       ,'Item already has Parent ''' || i.ParentID || ''' associated with it. Please correct the Parent and reupload.' as Error, 45 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct ItemID, ParentID from Forecast{0}.{4}
                            union
                            select distinct ItemID, ParentID from Forecast{0}.itempatch_ownership_newitemslookup where gmsvenid = {1}
                            union
                            select distinct ItemID, ParentID from Forecast{0}.itempatch_overlap_newitemslookup where gmsvenid = {1}
                            ) i on tni.ItemID = i.ItemID
                where i.ParentID<>tni.ParentID;

                insert /*direct*/ into temp_stage_errors
                select tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc,PrimaryVendor, false
                       ,'Item already has Assortment ''' || i.AssrtID || ''' associated with it. Please correct the Assortment and reupload.' as Error, 50 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct ItemID, AssrtID from Forecast{0}.{4}
                            union
                            select distinct ItemID, AssrtID from Forecast{0}.itempatch_ownership_newitemslookup where gmsvenid = {1}
                            union
                            select distinct ItemID, AssrtID from Forecast{0}.itempatch_overlap_newitemslookup where gmsvenid = {1}
                            ) i on tni.ItemID = i.ItemID
                where i.AssrtID<>tni.AssrtID;

                /* Store any rows in which the ParentID is not valid */
                insert /*direct*/ into temp_stage_errors
                select distinct tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc,PrimaryVendor, false,
                case when tni.ParentID is null
                        then 'Missing values for Parent. Please enter valid values for each column.'
                        else 'Parent ''' || tni.ParentID || ''' is not valid. Please enter a valid Parent.'
                end as Error, 55 as ErrorPriority
                from tmp_new_items_upload tni 
                left join Forecast{0}.items_parent ip on ip.ParentID = tni.ParentID 
                where ip.ItemID is null;

                /* Store any invalid Patches */
                insert /*direct*/ into temp_stage_errors
                select distinct tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false,
                case when tni.Patch is null
                        then 'Missing values for Patch. Please enter valid values for each column.'
                        else 'Patch ''' || tni.Patch || ''' is not valid. Please enter a valid Patch.'
                end as Error, 60 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct Patch from Forecast{0}.build_stores) patches on upper(tni.Patch) = patches.Patch
                where patches.Patch is null;

                /* Store any items that already exist because we only want items that are brand new */
                insert /*direct*/ into temp_stage_errors
                select distinct tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, i.ItemID, tni.ItemDesc, PrimaryVendor, false, 'Item already exists in Lowe''s system. To gain visibility, use the Item Ownership Upload feature.' as Error, -1 as ErrorPriority
                from tmp_new_items_upload tni
                left join (select distinct itemid from Forecast{0}.build_items) i on tni.itemID = i.itemID
                where i.ItemID is not null;

                /* Store any items that already exist in the vendors' table from previous uploads or vendor has an existing overlap claim */
                insert /*direct*/ into temp_stage_errors
                select distinct tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false, 'Item Patch already exists.' as Error, 70 as ErrorPriority
                from tmp_new_items_upload tni
                inner join forecast{0}.itempatch_ownership ft on ft.gmsvenid = {1} and tni.itemid = ft.itemid and upper(tni.patch) = ft.patch;

                insert /*direct*/ into temp_stage_errors
                select distinct tni.ProdGrpID, tni.AssrtID, tni.ParentID, tni.Patch, tni.ItemID, tni.ItemDesc, PrimaryVendor, false, 'Existing claim on Item Patch.' as Error, 75 as ErrorPriority
                from tmp_new_items_upload tni
                inner join forecast{0}.itemPatch_Overlap ft 
                on ft.gmsvenid = {1} and tni.itemid = ft.itemid and upper(tni.patch) = ft.patch;

                /* Get all error data and only keep the first error type that was detected for a given row */
                drop table if exists temp_all_error_data;
                create local temp table temp_all_error_data on commit preserve rows as
                select ProdGrpID, AssrtID, ParentID, Patch, ItemID, ItemDesc, PrimaryVendor, Error    
                from temp_stage_errors
                where dupsflag is true;
                
                insert into temp_all_error_data
                select ProdGrpID, AssrtID, ParentID, Patch, ItemID, ItemDesc, PrimaryVendor, Error
                from (select *, row_number() over(partition by ProdGrpID, AssrtID, ParentID, Patch, ItemID, ItemDesc, PrimaryVendor order by ErrorPriority asc) as row_number from temp_stage_errors where dupsflag is false) s
                where row_number = 1 and (coalesce(itemid, 'missing item'), patch) not in (select distinct coalesce(itemid, 'missing item') as itemid, patch from temp_all_error_data);

                /* Stage valid records for insert into ownership table, and data tables if necessary */    
                insert into Forecast{0}.Upload_New_Items_Stage
                select distinct v.GMSVenID, v.VendorDesc, v.VendorID as VBU, tni.ItemID::int, tni.ItemDesc, UPPER(tni.Patch), tni.ProdGrpID, items_P.Prodgrpdesc, tni.AssrtID, items_a.AssrtDesc
                    , tni.ParentID, coalesce(upper(parents.ParentDesc), 'PARENT NEEDED'), $${5}$$
                from tmp_new_items_upload tni
                left join temp_all_error_data tei on tni.Patch = tei.Patch and tni.ItemID::varchar(50) = tei.ItemID
                left join Forecast{0}.config_Vendors v on GMSVenID ={1}
                left join(select distinct Prodgrpid, prodgrpdesc from forecast{0}.build_Items) items_P on items_P.prodgrpid = tni.prodgrpid
                left join(select distinct Assrtid, assrtdesc from forecast{0}.build_Items) items_a on items_a.assrtid = tni.assrtid
                left join(select distinct itemid, ParentID, ParentDesc from forecast{0}.build_Items) parents on parents.parentid = tni.parentid and parents.itemid = tni.itemid
                where upper(left(tni.primaryvendor,1)) ='Y'
                and tei.itemid is null and tni.itemid is not null and tni.patch is not null;

                /* Store secondary vendor items in lookup table -- do not use in remaining steps */
                insert into Forecast{0}.itemPatch_Ownership_SecondaryVendor (gmsvenid, vendordesc, vbu, newitemsflag, itemid, itemdesc, patch, prodgrpid, assrtid, parentid, action, timestamp)
                select distinct v.GMSVenID, v.VendorDesc, v.VendorID as VBU, true, tni.ItemID::int, tni.ItemDesc, upper(tni.Patch), tni.ProdGrpID, tni.AssrtID, tni.ParentID, 'A', current_timestamp
                from tmp_new_items_upload tni
                left join temp_all_error_data tei on tni.Patch = tei.Patch and tni.ItemID::varchar(50) = tei.ItemID
                left join Forecast{0}.config_Vendors v on GMSVenID ={1}
                left join(select distinct Prodgrpid, prodgrpdesc from forecast{0}.build_Items) items_P on items_P.prodgrpid = tni.prodgrpid
                left join(select distinct Assrtid, assrtdesc from forecast{0}.build_Items) items_a on items_a.assrtid = tni.assrtid
                left join(select distinct itemID, ParentID, ParentDesc from forecast{0}.build_Items) parents on parents.parentid = tni.parentid and parents.itemid = tni.itemid
                where upper(left(tni.primaryvendor,1)) ='N'
                and tei.itemid is null and tni.itemid is not null and tni.patch is not null;

                /* return error data for output to user */
                select ItemID, ItemDesc, Patch, ProdGrpID, ParentID, AssrtId, PrimaryVendor, Error from temp_all_error_data;
                select count(*) as RowCount from Forecast{0}.Upload_New_Items_Stage where BatchID = $${5}$$;"

            , _dev
            , editor.GMSVenID
            , GetPublicShema()
            , fileName
            , editor.TableName
            , batchId
            , timeStamp
            , backendHeaders);

            return cmd;
        }

        /// <summary>
        /// Insert any rejected data type errors into IOU error table
        /// </summary>
        /// <param name="param"></param>
        /// <param name="batchId"></param>
        /// <returns></returns>
        public string CreateIOUDataTypeErrorStage(int gmsVenId, string batchId, string backendHeaders)
        {
            var cmd = string.Format(@"
                insert into forecast{0}.upload_IOU_Invalid (GMSVenID, VendorDesc, VBU, ErrorPriority, BatchID, Reason {3})
                select v.gmsvenid, v.vendordesc, v.VendorID as vbu, 2 as ErrorPriority, $${2}$$ as BatchID, 'One of the columns has an invalid data type.' as Reason
                        , split_part(rejected_data, ',', 1) 
                        , split_part(rejected_data, ',', 2) 
                        , split_part(rejected_data, ',', 3) 
                        , split_part(rejected_data, ',', 4) 
                from forecast{0}.config_vendors v
                cross join tmp_load_rejected_data r
                where v.gmsvenid = {1};"
            , _dev
            , gmsVenId
            , batchId
            , backendHeaders);

            return cmd;
        }

        /// <summary>
        /// Upload into flex table - this will grab any data type errors.
        /// Insert into persistent table for use in remainder of script.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string CreateIOUFileDataStage(int gmsVenId, string fileName, string batchId)
        {
            var cmd = string.Format(@"
                CREATE FLEX LOCAL TEMP TABLE tmp_iou_upload(GMSVenID int, Item int , Patch varchar(12) , Action varchar(5) , ""Primary Vendor"" varchar(12)) ON COMMIT PRESERVE ROWS;
                COPY tmp_iou_upload(__raw__, gmsvenid as {2}::int, Item, Patch, Action,""Primary Vendor"") from $$/mnt_yoda/FTPVertica{0}/Forecast/{1}$$ 
                ON ANY NODE parser PUBLIC.FCSVPARSER(reject_on_materialized_type_error=true, reject_on_duplicate=true, header=true) 
                rejected data as table tmp_load_rejected_data SKIP 1 no commit;

                insert into forecast{0}.upload_IOU_Stage
                select u.GMSVenID, v.VendorDesc, v.VendorID as VBU, u.Item as ItemID, upper(u.Patch), upper(u.Action), ""Primary Vendor"" as primaryvendor, $${3}$$
                from tmp_iou_upload u
                LEFT JOIN forecast{0}.config_vendors v
                using (GMSVenID);"
            , _dev
            , fileName
            , gmsVenId
            , batchId);

            return cmd;
        }

        /// <summary>
        /// Check for invalid actions.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string CreateIOUInvalidActionsStage(string batchId)
        {
            var cmd = string.Format(@"
                Insert into forecast{0}.upload_IOU_InValid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ITemID, u.Patch, u.Action, u.PrimaryVendor, 5 as ErrorPriority, u.BatchID
                    , 'Invalid Action value. Please use ''A'' for Add and ''R'' for Remove' as Reason
                from forecast{0}.upload_IOU_Stage u
                where u.batchid = $${2}$$ and (upper(left(u.action,1)) Not in ('A','R') or u.action is null);

                Insert into forecast{0}.upload_IOU_InValid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ITemID, u.Patch, u.Action, u.PrimaryVendor, 6 as ErrorPriority, u.BatchID
                    , 'No Current or Pending claim on Item/Patch. Cannot remove.' as Reason
                from forecast{0}.upload_IOU_Stage u
                left join forecast{0}.itemPatch_Ownership o
                on o.gmsvenid = u.gmsvenid and o.itemid = u.itemid and o.patch = upper(u.patch)
                left join forecast{0}.itemPatch_Overlap p
                on p.gmsvenid = u.gmsvenid and p.itemid = u.itemid and p.patch = upper(u.patch)
                where u.batchid = $${2}$$ and upper(left(u.action,1)) ='R' and upper(left(u.primaryvendor,1))= 'Y'
                and o.itemid is null and p.itemid is null;

                Insert into forecast{0}.upload_IOU_InValid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ITemID, u.Patch, u.Action, u.PrimaryVendor, 7 as ErrorPriority, u.BatchID
                    , 'Existing Current or Pending claim on Item/Patch. Cannot add again.' as Reason
                from forecast{0}.upload_IOU_Stage u
                left join forecast{0}.itemPatch_Ownership o
                on o.gmsvenid = u.gmsvenid and o.itemid = u.itemid and o.patch = upper(u.patch)
                left join forecast{0}.itemPatch_Overlap p
                on p.gmsvenid = u.gmsvenid and p.itemid = u.itemid and p.patch = upper(u.patch)
                where u.batchid = $${2}$$ and upper(left(u.action,1)) ='A' and upper(left(u.primaryvendor,1))= 'Y'
                and (o.itemid is not null or  p.itemid is not null);"
            , _dev
            , GetPublicShema()
            , batchId);

            return cmd;
        }

        /// <summary>
        /// Check for invalid primary vendor inputs.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string CreateIOUInvalidPrimaryVendorStage(string batchId)
        {
            var cmd = string.Format(@"
                Insert into forecast{0}.upload_IOU_InValid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ITemID, u.Patch, u.Action, u.PrimaryVendor, 8 as ErrorPriority, u.BatchID
                    , 'Invalid Primary Vendor value. Please use ''Y'' if you are the primary vendor, and ''N'' if you are the secondary vendor.' as Reason
                from forecast{0}.upload_IOU_Stage u
                where u.batchid = $${2}$$ and (upper(left(u.primaryvendor,1)) Not in ('Y','N') or u.primaryvendor is null);"
            , _dev
            , GetPublicShema()
            , batchId);

            return cmd;
        }

        /// <summary>
        /// check no duplicate item/patch 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string CreateIOUNoDupItemPatchStage(string batchId)
        {
            var cmd = string.Format(@"
                insert into forecast{0}.upload_IOU_InValid
                select s.GMSVenID, s.VendorDesc, s.VBU, s.ItemID, s.Patch, s.Action, s.PrimaryVendor, 1 as ErrorPriority, s.BatchID
                        , 'Duplicate records - please upload distinct Item/Patch values'
                from (select *, count(itemid) over(partition by itemid, upper(patch)) as record_count from forecast{0}.upload_IOU_Stage where batchid = $${1}$$)s
                where record_count > 1;"
            , _dev
            , batchId);

            return cmd;
        }

        /// <summary>
        /// Check itemid and patch are in source tables and are not null inputs.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string CreateIOUInValidItemPatchInSourceTableStage(string batchId)
        {
            var cmd = string.Format(@"

                Insert into forecast{0}.upload_IOU_InValid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ITemID, u.Patch, u.Action, u.PrimaryVendor, 3 as ErrorPriority, u.BatchID
                        , 'Item or Patch does not exist. Choose an existing item or patch.  If a brand new item, use the new items upload.' as Reason
                from forecast{0}.upload_IOU_Stage u
                where u.batchid = $${2}$$ and (itemid is null or patch is null);

                Insert into forecast{0}.upload_IOU_InValid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ITemID, u.Patch, u.Action, u.PrimaryVendor, 4 as ErrorPriority, u.BatchID
                        , 'Item or Patch does not exist. Choose an existing item or patch. If a brand new item, use the new items upload.' as Reason
                from forecast{0}.upload_IOU_Stage u
                left join (select distinct itemid from Forecast{0}.build_items) i
                using(itemid)
                left join (select distinct ltrim(rtrim(subregid)) as patch from {1}.tbl_stores where retid = 1 and subregid is not null) s
                on s.patch = upper(u.patch)
                where u.batchid = $${2}$$ and (i.itemid is null or s.patch is null) and upper(left(u.Action,1)) = 'A';"
            , _dev
            , GetPublicShema()
            , batchId);

            return cmd;
        }

        /// <summary>
        /// Insert all valid records into the stage table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string CreateIOUValidRecordsStage(string batchId)
        {
            var cmd = string.Format(@"
                insert into forecast{0}.upload_IOU_Valid
                select u.GMSVenID, u.VendorDesc, u.VBU, u.ItemID, coalesce(bi.ItemDesc, 'No Item Desc') as ItemDesc, u.Patch, u.Action, u.BatchID
                from forecast{0}.upload_IOU_Stage u
                left join forecast{0}.upload_IOU_InValid i
                on u.gmsvenid = i.gmsvenid
                and u.itemid::varchar(50) = i.itemid
                and u.patch = i.patch
                and i.batchid = $${1}$$
                left join Forecast{0}.build_items bi on bi.ItemID = u.ItemID
                where u.batchid = $${1}$$
                and i.itemid is null
                and upper(left(u.primaryVendor,1)) = 'Y'
                and u.itemid is not null
                and u.patch is not null;

                insert into Forecast{0}.itemPatch_Ownership_SecondaryVendor (gmsvenid, vendordesc, vbu, newitemsflag, itemid, patch, action, timestamp)
                select distinct u.GMSVenID, u.VendorDesc, u.VBU,false, u.ItemID::int, upper(u.Patch), u.Action, current_timestamp
                from forecast{0}.upload_IOU_Stage u
                left join forecast{0}.upload_IOU_InValid i
                on u.gmsvenid = i.gmsvenid
                and u.itemid::varchar(50) = i.itemid
                and u.patch = i.patch
                and i.batchid = $${1}$$
                where u.batchid = $${1}$$
                and i.itemid is null
                and upper(left(u.primaryVendor,1)) = 'N'
                and upper(left(u.action,1)) ='A';

                insert into Forecast{0}.itemPatch_Ownership_SecondaryVendor (gmsvenid, vendordesc, vbu, newitemsflag, itemid, patch, action, timestamp)
                select distinct u.GMSVenID, u.VendorDesc, u.VBU,null, u.ItemID::int, upper(u.Patch), u.Action, current_timestamp
                 from forecast{0}.upload_IOU_Stage u
                left join forecast{0}.upload_IOU_InValid i
                on u.gmsvenid = i.gmsvenid
                and u.itemid::varchar(50) = i.itemid
                and u.patch = i.patch
                and i.batchid = $${1}$$
                where u.batchid = $${1}$$
                and i.itemid is null
                and upper(left(u.primaryVendor,1)) = 'N'
                and upper(left(u.action,1)) ='R';"

            , _dev
            , batchId);

            return cmd;
        }

        /*********************************************************
         *         uncontested item/patch combinations           *
         *********************************************************/

        /// <summary>
        /// Uncontested item/patch combinations .
        /// Evaluates item/patches not currently claimed by any other vendor.  
        /// Updates ownership table to assign item/patch to new vendor
        /// </summary>
        /// <returns></returns>
        public string CreateIOUUncontestedClaimStage(string batchId, Boolean newItemsFlag)
        {
            // Add concat columns when the method is being used for the new items upload process.
            string concatInsert = string.Format(@"
                , CASE WHEN u.ProdGrpID = 512330
                        THEN (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')
                        ELSE COALESCE(bs.MM, (select flagstring from forecast{0}.config_tool where flagName = 'No_MM'))
                END as MM
                , u.itemid || ' - ' || upper(u.itemdesc) as itemconcat
                , u.Prodgrpid || ' - ' || upper(u.Prodgrpdesc) as prodgrpconcat
                , u.Assrtid || ' - ' || upper(u.Assrtdesc) as assrtconcat
                , u.ParentID || ' - ' || coalesce(upper(u.Parentdesc), 'PARENT NEEDED') as parentconcat", _dev);

            var cmd = string.Format(@"
                create local temp table tmp_iou_uncontested on commit preserve rows as
                select u.*, case when o.gmsvenid = 0 then 1 else 0 end as novendorflag
                        , bs.storeid
                {4}
                from forecast{0}.{2} u
                left join forecast{0}.itempatch_ownership o
                using(itemid, patch)
                left join (select distinct Patch, MM, StoreID from forecast{0}.build_stores) bs using(patch)
                where (o.itemid is null
                or o.gmsvenid = 0)
                {3}
                and batchID = $${1}$$;

                --insert vendor's ownership
                INSERT into forecast{0}.itemPatch_Ownership 
                select distinct u.gmsvenid, u.vendordesc, u.itemid, u.itemdesc, u.patch
                from tmp_iou_uncontested u;

                --remove any potential no vendor records
                delete from forecast{0}.itemPatch_Ownership
                where gmsvenid = 0
                and (itemid, patch) in (select distinct itemid, patch from tmp_iou_uncontested where novendorflag is true);"
            , _dev
            , batchId
            , newItemsFlag ? "Upload_New_Items_Stage" : "upload_IOU_Valid"
            , newItemsFlag ? "" : "and Action = 'A'"
            , newItemsFlag ? concatInsert : "");

            if (newItemsFlag)
            {
                cmd += string.Format(@"INSERT into forecast{0}.itemPatch_Ownership_newitemslookup
                                        select distinct u.gmsvenid, u.vendordesc, u.itemid, u.itemdesc, u.patch, u.prodgrpid, u.assrtid, u.parentid
                                        from tmp_iou_uncontested u", _dev);
            }

            return cmd;
        }

        /// <summary>
        /// modify no vendor record for new owner.
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// Get all affected MM's by this upload.
        /// Use when inserting a new item/patch record into the dataset.
        /// source table is staging newrecords table
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMMsNewRecords()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, coalesce(mm.viewName, mm.tableName) as tablename
                from tmp_iou_newrecords u
                inner join forecast{0}.config_mm mm
                using(MM)
                where mm.mmflag is true;"
            , _dev);

            return cmd;
        }


        public string GetIOUAffectedMDsNewRecords()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, mm.viewName as tablename
                from tmp_iou_newrecords u
                inner join forecast{0}.config_mm mm
                on mm.mm = u.md
                where mm.viewName is not null;"
            , _dev);

            return cmd;
        }


        /// <summary>
        /// Get all affected MM's by this upload.
        /// Use when a vendor removes a claim, and the claim goes to another vendor or "no vendor"
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMMs(string tablename, Boolean noVendorFlag)
        {
            var cmd = string.Format(@"
                select distinct mm.MM, mm.tablename
                from {1} u
                inner join forecast{0}.tbl_allvendors a
                using(itemid, patch)
                inner join forecast{0}.config_mm mm on mm.mm = a.mm
                where mm.mmflag is true
               {2};"
            , _dev
            , tablename
            , noVendorFlag ? "and u.novendorflag is true" : "");

            return cmd;
        }

        /// <summary>
        /// Get all affected MD's by this upload.
        /// Use when a vendor removes a claim, and the claim goes to another vendor or "no vendor"
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMDs(string tablename, Boolean noVendorFlag)
        {
            var cmd = string.Format(@"
                select distinct mm.mm, mm.viewName as tablename
 
                from {1} u
                inner join forecast{0}.tbl_allvendors a
                using(itemid, patch)
                inner join forecast{0}.config_mm mm 
                on mm.mm = a.md and mm.viewname is not null
               {2};"
            , _dev
            , tablename
            , noVendorFlag ? "and u.novendorflag is true" : "");

            return cmd;
        }


        /// <summary>
        /// Get all affected MM's by this upload.
        /// Use when a vendor removes a claim, and the claim goes to another vendor or "no vendor"
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMMs_removal()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, coalesce(mm.viewName, mm.tablename) as tablename
                from tmp_iou_removal_updates u
                inner join forecast{0}.tbl_allvendors a
                using(itemid, storeid)
                inner join forecast{0}.config_mm mm
                on a.mm = mm.mm
                where mm.mmflag is true
                and a.mm = u.mm;"
            , _dev);

            return cmd;
        }

        /// <summary>
        /// Get all affected MM's by this upload.
        /// Use when a vendor removes a claim, and the claim goes to another vendor or "no vendor"
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMDs_removal()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, mm.viewname as tablename
                from tmp_iou_removal_updates u
                inner join forecast{0}.tbl_allvendors a
                using(itemid, storeid)
                inner join forecast{0}.config_mm mm
                on a.md = mm.mm
                where mm.viewname is not null;"
            , _dev);

            return cmd;
        }

        /// <summary>
        /// Get MM table names for those new items that switched ownership and came with a new prodgrpid
        /// Accounts for the special H+P condition that puts all items that fall under this PG to H+P Merchant
        /// Gathers mm tables where the record no longer exists and should be removed.
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMMs_HandP_deletes()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, coalesce(mm.viewName, mm.tablename) as tablename
                from tmp_iou_removal_mm u
                inner join forecast{0}.config_mm mm
                using(MM)
                where mm.mmflag is true;"
            , _dev);

            return cmd;
        }

        public string CheckHPChange()
        {
            var cmd = string.Format(@"
                select distinct u.prodgrpid as changed
                from (select distinct itemid, patch, prodgrpid from tmp_iou_uncontested ) u 
                inner join forecast{0}.tbl_allvendors a
                using(itemid, patch)
                where  (a.prodgrpid = '512330' and u.prodgrpid != '512330') --from HP to non HP
                       or (a.prodgrpid != '512330' and u.prodgrpid = '512330') --from non HP to HP

            ", _dev);

            return cmd;
        }

        public string FindNewMMTable()
        {
            var cmd = string.Format(@"
                select distinct tablename from                 
                tmp_iou_uncontested u
                join forecast{0}.tbl_AllVendors v
                on v.itemID =u.ItemID 
                and v.patch = u.patch
                and v.prodgrpid != '512330' 
                and u.prodgrpid = '512330'
                join forecast{0}.config_mm mm
                on v.mm = mm.mm;

            ", _dev);

            return cmd;
        }

        ////select distinct tablename from 
        ////forecast{0}.itempatch_overlap a
        ////inner join tmp_iou_uncontested u
        ////using(patch)
        ////join forecast{0}.config_mm mm
        ////on a.mm = mm.mm
        ////join forecast{0}.tbl_AllVendors v
        ////on v.itemID = a.ItemID 
        ////and v.patch = a.patch
        ////and v.gmsvenid = a.gmsvenid
        ////and v.prodgrpid != '512330' 
        ////and u.prodgrpid = '512330'

        public string GetHPMMTable()
        {
            
            var cmd = string.Format(@"
            select distinct tablename from forecast{0}.config_mm 
            where mm in (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')

            ", _dev);
            return cmd;
         

        }

        internal string HPMMTableUpdate(string itemID, string patch, string table_new, string table_old)
        {
            var cmd = string.Format(@"
                      Insert into forecast{0}.{1} 
                      Select * from forecast{0}.{2} where ItemID = $${3}$$ and Patch = $${4}$$
                      and (itemid, Patch) not in (select itemID, patch from forecast{0}.{1});

                      Delete from forecast{0}.{2} where ItemID = $${3}$$ and Patch = $${4}$$;

            ", 
            _dev, 
            table_new, 
            table_old, 
            itemID, 
            patch);
            return cmd;
        }

        //for TBD patch items going from non HP to HP 
        //we don't want to delte records from tbl_allVendors
        internal string HPMMTableUpdate_MM(string itemID, string patch, string table_new, string table_old)
        {
            var cmd = string.Format(@"
                      Insert into forecast{0}.{1} 
                      Select * from forecast{0}.{2} where ItemID = $${3}$$ and Patch = $${4}$$
                     and (itemid, Patch) not in (select itemID, patch from forecast{0}.{1});

            ",
            _dev,
            table_new,
            table_old,
            itemID,
            patch);
            return cmd;
        }

        //For TBD patch items going from HP to non HP 
        //We don't want to insert duplicates into tbl_AllVendors
        internal string HPMMTableUpdate_NoMM(string itemID, string patch, string table_old)
        {
            var cmd = string.Format(@"
                Delete from forecast{0}.{1} where ItemID = $${2}$$ and Patch = $${3}$$;
            ",
            _dev,
            table_old,
            itemID,
            patch);
            return cmd;
        }

        public string GetNewMMTablePatchItem_from_HP(string table)
        {
            var cmd = string.Format(@"
                select distinct $${1}$$ as table_old, 
                CASE WHEN tablename is null and hp.mm = (select flagstring from forecast{0}.config_tool where flagName = 'No_MM')
                THEN 'tbl_AllVendors'
                ELSE tablename
                END as table_new, 
                hp.ItemID, hp.Patch from  (select *  from forecast{0}.{1} where prodgrpID != '512330') hp 
                left join forecast{0}.config_mm m on m.mm = hp.mm
                order by table_new desc;
            ", _dev, table);
            return cmd;
        }

        public string GetNewMMTablePatchItem_to_HP(string table, string hp_table)
        {
            var cmd = string.Format(@"
              select distinct $${1}$$ as table_old, tablename as table_new, hp.ItemID, hp.Patch from  (select *  from forecast{0}.{1} where prodgrpID = '512330') hp 
                        join forecast{0}.config_mm m on m.mm = hp.mm
                        where (hp.itemid, hp.Patch) not in (select itemID, patch from forecast{0}.{2})
            ", _dev, table, hp_table);
            return cmd;
        }

        

        /// <summary>
        /// Get MM table names for those new items that switched ownership and came with a new prodgrpid
        /// Accounts for the special H+P condition that puts all items that fall under this PG to H+P Merchant
        /// Gathers mm tables where the record is new and should be inserted
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedMMs_HandP_inserts()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, mm.tablename
                from tmp_iou_removal_mm u
                inner join forecast{0}.tbl_allvendors a
                using(itemid, patch)
                inner join forecast{0}.config_mm mm
                on a.mm = mm.mm
                where mm.mmflag is true
                and a.mm <> u.mm;
                "
            , _dev);

            return cmd;
        }

        public string GetIOUAffectedMDs_HandP_inserts()
        {
            var cmd = string.Format(@"
                select distinct mm.MM, mm.viewname as tablename
                from tmp_iou_removal_mm u
                inner join forecast{0}.tbl_allvendors a
                using(itemid, patch)
                inner join forecast{0}.config_mm mm
                on a.md = mm.mm
                where mm.viewname is not null
                and a.mm <> u.mm;
                "
            , _dev);

            return cmd;
        }

        /// <summary>
        /// Get all affected  vendors by this upload.
        /// Use when a vendor removes a claim, and the claim goes to another vendor or "no vendor"
        /// </summary>
        /// <returns></returns>
        public string GetIOUAffectedVendorsRemovalrecords()
        {
            var cmd = string.Format(@"
                select distinct v.tablename
                from tmp_iou_removal_updates u
                inner join forecast{0}.config_vendors v
                using(gmsvenid);"
            , _dev);

            return cmd;
        }

        /// <summary>
        /// Updates filters tables 
        /// </summary>
        /// <returns></returns>
        public string CreateIOUUpdateFilters(string tableName, string lookuptable, Boolean GMSVenIDClause, Boolean NoVendorClause, Boolean newItemFlag, Boolean updateOnlyFlag, Boolean tempLookup)
        {

            string newItems = string.Format(@"
                insert into forecast{0}.filters_{1} 
                select t.* from (select distinct 'ItemConcat' as filtertype, ItemConcat as filtervalue from {2} where ItemConcat is not null {3} {4}) t
                    where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'ItemConcat' and filtervalue is not null); 

                insert into forecast{0}.filters_{1}
                select t.* from (select distinct 'ProdGrpConcat' as filtertype, ProdGrpConcat as filtervalue from {2} where ProdGrpConcat is not null {3} {4}) t
                    where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'ProdGrpConcat' and filtervalue is not null);

                insert into forecast{0}.filters_{1}
                select t.* from (select distinct 'AssrtConcat' as filtertype, AssrtConcat as filtervalue from {2} where AssrtConcat is not null {3} {4}) t
                    where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'AssrtConcat' and filtervalue is not null);

                insert into forecast{0}.filters_{1}
                select t.* from (select distinct 'ParentConcat' as filtertype, ParentConcat as filtervalue from {2} where ParentConcat is not null {3} {4}) t
                    where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'ParentConcat' and filtervalue is not null);"
            , _dev
            , tableName
            , tempLookup ? lookuptable : string.Format(@"forecast{0}.{1}", _dev, lookuptable)
            , GMSVenIDClause ? string.Format(@" and gmsvenid = (select distinct gmsvenid from forecast{0}.{1})", _dev, tableName) : ""
            , NoVendorClause ? " and novendorflag is true" : "");

            string insertFilters = string.Format(@"
                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'FiscalMo' as filtertype, FiscalMo as filtervalue from {2} where FiscalMo is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'FiscalMo' and filtervalue is not null);

                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'FiscalQtr' as filtertype, FiscalQtr as filtervalue from {2} where FiscalQtr is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'FiscalQtr' and filtervalue is not null);

                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'FiscalWk' as filtertype, FiscalWk as filtervalue from {2} where FiscalWk is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'FiscalWk' and filtervalue is not null);
            
                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'Region' as filtertype, Region as filtervalue from {2} where Region is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'Region' and filtervalue is not null);

                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'District' as filtertype, District as filtervalue from {2} where District is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'District' and filtervalue is not null);

                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'MM' as filtertype, MM as filtervalue from {2} where MM is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'MM' and filtervalue is not null);

                    insert into forecast{0}.filters_{1}
                    select t.* from (select distinct 'MD' as filtertype, MD as filtervalue from {2} where MD is not null {3} {4}) t
                        where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'MD' and filtervalue is not null);
                                                
                    update forecast{0}.filters_{1} f
                    set filtervalue = (select count(*) from(select distinct gmsvenid, itemid, patch, region, district, fiscalwk from forecast{0}.{1}) s)
                    where filtertype = 'Count';"
            , _dev
            , tableName
            , tempLookup ? lookuptable : string.Format(@"forecast{0}.{1}", _dev, lookuptable)
            , GMSVenIDClause ? string.Format(@" and gmsvenid = (select distinct gmsvenid from forecast{0}.{1})", _dev, tableName) : ""
            , NoVendorClause ? " and novendorflag is true" : "");

            var cmd = string.Format(@"insert into forecast{0}.filters_{1}
                select t.* from (select distinct 'VendorDesc' as filtertype, vendordesc as filtervalue from {2} where vendordesc is not null {3} {4}) t
                where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'VendorDesc' and filtervalue is not null);

                insert into forecast{0}.filters_{1}
                select t.* from (select distinct 'Patch' as filtertype, Patch as filtervalue from {2} where Patch is not null {3} {4}) t
                    where t.FilterValue not in (select filtervalue from forecast{0}.filters_{1} where filtertype = 'Patch' and filtervalue is not null);

                {5}
                {6} "
            , _dev
            , tableName
            , tempLookup ? lookuptable : string.Format(@"forecast{0}.{1}", _dev, lookuptable)
            , GMSVenIDClause ? string.Format(@" and gmsvenid = (select distinct gmsvenid from forecast{0}.{1})", _dev, tableName) : ""
            , NoVendorClause ? " and novendorflag is true" : ""
            , newItemFlag ? newItems : ""
            , updateOnlyFlag ? "" : insertFilters);

            return cmd;
        }

        /// <summary>
        /// Updates filters tables 
        /// </summary>
        /// <returns></returns>
        public string CreateIOUUpdateFiltersScript(string tableName)
        {
            var cmd = string.Format(@"truncate table forecast{0}.filters_{1};

            insert into forecast{0}.filters_{1}
            select distinct 'VendorDesc' as filtertype, vendordesc as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'ItemConcat' as filtertype, ItemConcat as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'Patch' as filtertype, Patch as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'ProdGrpConcat' as filtertype, ProdGrpConcat as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'AssrtConcat' as filtertype, AssrtConcat as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'Region' as filtertype, Region as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'District' as filtertype, District as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'ParentConcat' as filtertype, ParentConcat as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'MM' as filtertype, MM as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'MD' as filtertype, MD as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'FiscalMo' as filtertype, FiscalMo as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'FiscalQtr' as filtertype, FiscalQtr as filtervalue from forecast{0}.{1};

            insert into forecast{0}.filters_{1}
            select distinct 'FiscalWk' as filtertype, FiscalWk as filtervalue from forecast{0}.{1};

                insert into forecast{0}.filters_{1}
                select distinct 'Count' as filtertype, count(*) from(select distinct gmsvenid, itemid, patch, region, district, fiscalwk from forecast{0}.{1})s;"
                , _dev
                , tableName);
            return cmd;
        }

        /// <summary>
        /// Updates tables for new owner if previous record was associated with "No Vendor"
        /// </summary>
        /// <returns></returns>
        public string CreateUpdateNoVendorRecords(string tableName, Boolean newItemsFlag)
        {
            string newItemsUpdate = string.Format(@",ItemDesc = upper(u.ItemDesc)
                                                    , ItemConcat = u.itemconcat
                                                     ,AssrtID = u.Assrtid
                                                     ,AssrtDesc = u.AssrtDesc
                                                     ,Assrtconcat = u.assrtconcat
                                                     ,ProdGrpID = u.ProdGrpID
                                                     ,ProdGrpDesc = u.ProdGrpDesc
                                                     ,prodgrpconcat = u.prodgrpconcat
                                                     ,ParentID = u.ParentID
                                                     ,ParentDesc = u.ParentDesc
                                                     ,Parentconcat = u.parentconcat
                                                     ,MM = u.MM");

            var cmd = string.Format(@"
                update forecast{0}.{1} t
                set gmsvenid = u.gmsvenid
                    , vendordesc = u.vendordesc
                    , VBU = u.VBU
                {2}
                from tmp_iou_uncontested u
                where t.itemid = u.itemid and t.storeid = u.storeid
                and u.novendorflag is true;"
            , _dev
            , tableName
            , newItemsFlag ? newItemsUpdate : "");

            return cmd;
        }

        /// <summary>
        /// Update Vendor table for the new Item/Patch when previous owner was "no vendor"
        /// </summary>
        /// <returns></returns>
        public string CreateIOUVendorUpdate(string tableName)
        {
            var cmd = string.Format(@"
                insert into forecast{0}.{1}
                select a.*
                from forecast{0}.tbl_allvendors a
                inner join tmp_iou_uncontested u
                using(itemid, storeid)
                where u.novendorflag is true;"
            , _dev
            , tableName);

            return cmd;
        }

        /// <summary>
        /// Stage brand new Item/Patch records.
        /// </summary>
        /// <returns></returns>
        public string CreateIOUBrandNewRecordStage(Boolean newItemsFlag)
        {
            string IOUJoin = string.Format(@"
                left join (select
                            bi.itemid
                            , bi.ItemDesc
                            , bi.ProdGrpID
                            , bi.ProdGrpDesc
                            , bi.ParentID
                            , coalesce(bi.parentdesc, ip.parentdesc) as ParentDesc
                            , bi.AssrtID
                            , bi.AssrtDesc
                        from Forecast{0}.build_items bi
                        left join Forecast{0}.items_parent ip
                        on ip.itemid = bi.itemid and ip.parentid = bi.parentid
                    ) i on u.itemid = i.itemid"
                , _dev, GetPublicShema());

            var cmd = string.Format(@"
                create local temp table tmp_iou_newrecords on commit preserve rows as
                select u.GMSVenID, u.VendorDesc, u.VBU
                        , u.ItemID, {2}.ItemDesc 
                        , CASE WHEN {2}.itemdesc IS NULL
                                THEN u.ItemID::Varchar
                                ELSE u.ItemID||' - '||upper({2}.itemdesc)
                           END as ItemConcat
                         , s.Storeid, s.storedesc, s.storeid||' - '||s.storedesc as storeconcat
                         , c.FiscalWk, c.FiscalMo, c.FiscalQtr
                         , COALESCE(MD, (select flagstring from forecast{0}.config_tool where flagName = 'No_MD')) as MD
                         , CASE WHEN ProdGrpID = 512330
                                THEN (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')
                                ELSE COALESCE(s.MM, (select flagstring from forecast{0}.config_tool where flagName = 'No_MM'))
                           END as MM
                         , s.region
                         , s.district
                         , coalesce(s.Patch, 'NONE') as patch
                         , ProdGrpID, ProdGrpDesc, ProdGRPID||' - '||ProdGrpDesc as ProdGrpConcat
                         , AssrtId, AssrtDesc, AssrtID||' - '||AssrtDesc as AssrtConcat
                         , COALESCE(ParentID, 0) as ParentID, COALESCE(upper(ParentDesc), 'PARENT NEEDED') as ParentDesc, COALESCE(ParentID, 0) ||' - '|| COALESCE(upper(ParentDesc), 'PARENT NEEDED') as ParentConcat
                         , ps.sensitivity as PriceSensitivity
                         , current_timestamp as timestamp
                from tmp_iou_uncontested u
                {3}
                left join forecast{0}.build_stores s
                using(storeid)
                LEFT JOin forecast{0}.build_Elasticities ps 
                on ps.itemid = u.itemid and ps.patch = u.patch
                cross join (select distinct FiscalWk, FiscalMo, FiscalQtr from Forecast{0}.tbl_AllVendors) c
                where novendorflag is false;"

            , _dev
            , GetPublicShema()
            , newItemsFlag ? "u" : "i"
            , newItemsFlag ? "" : IOUJoin
            );

            return cmd;
        }

        /// <summary>
        /// Create an insert statement for a given table name.
        /// </summary>
        /// <returns></returns>
        public string CreateIOUBrandNewRecordInsert(string tableName)
        {
            var cmd = string.Format(@"
                INSERT into forecast{0}.{1}
                (   GMSVenID,
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
                    Timestamp)
                select * from tmp_iou_newrecords;"

            , _dev
            , tableName);

            return cmd;
        }



        public string CreateIOUBrandNewRecordInsert_mm(string tableName)
        {
            var cmd = string.Format(@"
                INSERT into forecast{0}.{1}
                (   GMSVenID,
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
                    Timestamp)
                select * from tmp_iou_newrecords where mm = (select distinct mm from forecast{0}.{1});"

            , _dev
            , tableName);

            return cmd;
        }


        public string GetInvalidIOURecords(string batchId)
        {
            var cmd = string.Format(@"select i.ItemID, i.Patch, i.Action, i.PrimaryVendor , i.Reason
                                    from Forecast{0}.upload_IOU_Invalid i
                                    inner join (select itemid, patch, min(errorpriority) errorpriority from Forecast{0}.upload_IOU_Invalid where BatchID = $${1}$$ group by 1,2)p
                                    on coalesce(i.itemid,'') = coalesce(p.itemid,'') and coalesce(i.patch,'') = coalesce(p.patch,'') and i.errorpriority = p.errorpriority
                                    where BatchID = $${1}$$;"
            , _dev
            , batchId);

            return cmd;
        }

        /*********************************************************
         *                 Overlapping items                     *
         *********************************************************/

        /// <summary>
        /// Evaluates if a new claim on an item/patch results in a new 
        /// overlap entry, or adds to an existing overlap entry.
        /// Inserts into overlap table new records.
        /// </summary>
        /// <returns></returns>
        public string CreateIOUOverlappingClaimsInserts(string batchId, Boolean newItemsFlag)
        {
            var newItemsOverlapInsert = string.Empty;
            if (newItemsFlag)
            {
                newItemsOverlapInsert = string.Format(@"left join Forecast{0}.itemPatch_Ownership_NewItemsLookup nil
                    on nil.gmsvenid = o.gmsvenid and nil.itemid = o.itemid and nil.patch = o.patch", _dev);
            }

            var cmd = string.Format(@"
                --new item/patch overlap, insert in owner's entry to overlap table
                insert into forecast{0}.itempatch_overlap (gmsvenid, vendordesc, owner, itemid, itemdesc, patch, MM, MD, timestamp)
                select o.gmsvenid, o.vendordesc, true as Owner, o.itemid, o.itemdesc, o.patch, 
                CASE WHEN {5}
                        THEN (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')
                        ELSE COALESCE(bs.MM, (select flagstring from forecast{0}.config_tool where flagName = 'No_MM'))
                END as MM, 
                coalesce(bs.MD, (select flagstring from forecast{0}.config_tool where flagName = 'No_MD')) as MD, current_timestamp as timestamp
                from forecast{0}.{2} u 
                inner join forecast{0}.itempatch_ownership o
                using(itemid, patch)
                left join forecast{0}.itempatch_overlap ov
                using(itemid, patch)
                left join (select distinct Patch, MM, MD from Forecast{0}.build_stores) bs
                using(patch)
                left join (select distinct ItemID, ProdGrpID from Forecast{0}.build_items) bi
                using(ItemID)
                {6}
                where o.gmsvenid <> 0
                and o.gmsvenid <> u.gmsvenid
                and ov.gmsvenid is null
                {3}
                and Batchid = $${1}$$;

                --insert new vendor claim to overlap table 
                insert into forecast{0}.itempatch_overlap (gmsvenid, vendordesc, owner, itemid, itemdesc, patch, MM, MD, timestamp)
                select u.gmsvenid, u.vendordesc, false as Owner, u.itemid, u.itemdesc, u.patch, 
                CASE WHEN {4}
                        THEN (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')
                        ELSE COALESCE(bs.MM, (select flagstring from forecast{0}.config_tool where flagName = 'No_MM'))
                END as MM, 
                coalesce(bs.MD, (select flagstring from forecast{0}.config_tool where flagName = 'No_MD')) as MD, timestampadd(second, 1, current_timestamp) as timestamp
                from forecast{0}.{2} u 
                inner join forecast{0}.itempatch_ownership o
                using(itemid, patch)
                left join (select distinct Patch, MM, MD from Forecast{0}.build_stores) bs
                using(patch)
                left join (select distinct ItemID, ProdGrpID from Forecast{0}.build_items) bi
                using(ItemID)
                where o.gmsvenid <> 0
                and o.gmsvenid <> u.gmsvenid
                {3}
                and Batchid = $${1}$$;"
            , _dev
            , batchId
            // Use new items stage table if this method is being executed by the new items upload process
            , newItemsFlag ? "Upload_New_Items_Stage" : "upload_IOU_Valid"
            , newItemsFlag ? "" : "and Action = 'A'"
            // Use the new item upload stage table for ProdGrpID because we need to check for H+P Merchant specifically
            , newItemsFlag ? "u.ProdGrpID = 512330" : "bi.ProdGrpID = 512330"
            // For inserting the owner into the overlap table we need to check for the ProdGrpID from the new items lookup table for origical ProdGrpID
            , newItemsFlag ? "nil.ProdGrpID = 512330" : "bi.ProdGrpID = 512330"
            , newItemsOverlapInsert
           ); 

            //add new item detail to lookup table 
            if (newItemsFlag)
            {
                cmd += string.Format(@"insert into forecast{0}.itempatch_overlap_newitemslookup 
                                        select u.gmsvenid, u.vendordesc, u.itemid, u.itemdesc, u.patch, u.prodgrpid, u.assrtid, u.parentid
                                        from forecast{0}.{2} u 
                                        inner join forecast{0}.itempatch_overlap o
                                        using(gmsvenid, itemid, patch)
                                        where o.gmsvenid = u.gmsvenid
                                        {3}
                                        and Batchid = $${1}$$;"
                , _dev
                , batchId
                , newItemsFlag ? "Upload_New_Items_Stage" : "upload_IOU_Valid"
                , newItemsFlag ? "" : "and Action = 'A'");
            }

            return cmd;
        }

        /// <summary>
        /// Delete Item/Patch records from a vendor table based on batchId.
        /// </summary>
        /// <returns></returns>
        public string DeleteIOURecordFromVendor(string tableName, string batchId)
        {
            var cmd = string.Format(@"
                delete from forecast{0}.{1}
                where (itemid, patch ) in (select distinct u.itemid, u.patch 
                                from forecast{0}.upload_IOU_Valid u
                                where Action = 'R'
                                and u.Batchid = $${2}$$);"
            , _dev
            , tableName
            , batchId);

            return cmd;
        }

        /// <summary>
        /// Delete Item/Patch records from a mm table.
        /// </summary>
        /// <returns></returns>
        public string DeleteIOURecordFromMM(string tableName)
        {
            var cmd = string.Format(@"
                delete from forecast{0}.{1}
                where(itemid, patch) in (select distinct itemid, patch from tmp_iou_removal_mm ); "

            , _dev
            , tableName);
            // where(itemid, patch, mm) in (select distinct itemid, patch, mm from tmp_iou_removal_mm ); "
            return cmd; 
        }

        /// <summary>
        /// update ownership table to 'No Vendor' or cascade to next vendor in line.
        /// </summary>
        /// <returns></returns>
        public string CreateIOUOwnershipTableCascadeOverlap(string batchId)
        {
            var cmd = string.Format(@"
                create local temp table tmp_IOU_removals_stage on commit preserve rows as 
                select n.*, coalesce(v.vendorid, 0) as vbu
                from forecast{0}.itemPatch_Ownership o
                inner join (select distinct u.ItemID, u.Patch
                                , coalesce(first_value(o.GMSVenID) over (partition by o.itemid, o.patch order by timestamp) ,0)as new_GMSVenID
                                , coalesce(first_value(o.VendorDesc) over (partition by o.itemid, o.patch order by timestamp), 'No Vendor') as new_VendorDesc
                                , coalesce(first_value(o.ItemDesc) over (partition by o.itemid, o.patch order by timestamp), 'No Item Desc') as new_ItemDesc
                        from forecast{0}.upload_IOU_Valid u
                        left join forecast{0}.itemPatch_overlap o
                        using(ItemID, Patch)
                        where (o.GMSVenID <> u.GMSVenID  or o.gmsvenid is null)
                        and Action = 'R'
                        and Batchid = $${1}$$)n
                ON o.itemid = n.itemid
                and o.patch = n.patch
                left join forecast{0}.config_vendors v
                on n.new_gmsvenid = v.gmsvenid 
                where o.gmsvenid <> n.new_GMSVenID;
                
                update forecast{0}.itemPatch_Ownership o
                set gmsvenid = n.new_GMSVenID
                    ,vendordesc = n.new_VendorDesc
                    ,itemdesc = n.new_ItemDesc
                from tmp_IOU_removals_stage n
                where o.itemid = n.itemid
                and o.patch = n.patch;

                insert into forecast{0}.itemPatch_Ownership_newitemslookup
                select ov.*
                from forecast{0}.upload_IOU_Valid u
                inner join forecast{0}.itemPatch_Ownership o
                using(itemid, patch)
                inner join forecast{0}.itemPatch_Overlap_newitemslookup ov
                on o.itemid = ov.itemid and o.patch = ov.patch and o.gmsvenid = ov.gmsvenid
                where Action = 'R'
                and BatchID = $${1}$$
                and u.gmsvenid <> o.gmsvenid;"
            , _dev
            , batchId);

            return cmd;
        }

        /// <summary>
        /// update overlap table.
        /// </summary>
        /// <returns></returns>
        public string CreateIOUOverlapTableDeleteUpdate(string batchId)
        {
            var cmd = string.Format(@"
                --remove record from overlap table
                delete from forecast{0}.itemPatch_Overlap
                where (gmsvenid, itemid, Patch) in (select distinct GMSVenID, ItemID, Patch 
                                                    from forecast{0}.upload_IOU_Valid 
                                                    where Action = 'R'
                                                    and BatchID = $${1}$$);

                --evaluate if no other competing claims, then remove last claim record
                delete from forecast{0}.itemPatch_Overlap
                where (Itemid, Patch) in (select  distinct itemid, patch from 
                                                        (select itemid, patch
                                                                , count(distinct gmsvenid) as record_count
                                                         from forecast{0}.itemPatch_Overlap 
                                                         group by 1,2) s where record_count =1);

                delete from forecast{0}.itemPatch_Overlap_newitemsLookup
                where (gmsvenid, itemid, patch) not in (select gmsvenid, itemid, patch from forecast{0}.itemPatch_Overlap);

                delete from forecast{0}.itemPatch_Ownership_newitemsLookup
                where (gmsvenid, itemid, patch) in (select distinct GMSVenID, ItemID, Patch 
                                                    from forecast{0}.upload_IOU_Valid 
                                                    where Action = 'R'
                                                    and BatchID = $${1}$$);

                --update ownership flag in overlap table to vendor with earliest timestamp                       
                update forecast{0}.itemPatch_Overlap o
                set Owner = true,
                ItemDesc = m.ItemDesc
                from (select ipo.gmsvenid, m.ItemID,
                        coalesce(nil.ItemDesc, ipo.ItemDesc) as ItemDesc,
                        m.patch,
                        m.timestamp
                    from (select itemid, patch, min(timestamp) timestamp 
                        from forecast{0}.itemPatch_Overlap
                        group by 1,2) m
                    join forecast{0}.itemPatch_overlap ipo
                    using(itemid, patch, timestamp)
                    left join forecast{0}.itemPatch_Ownership_NewItemsLookup nil
                    using(gmsvenid, itemid, patch)) m
                where o.itemid = m.itemid and o.patch = m.patch and o.timestamp =m.timestamp
                and o.owner is false;"
            , _dev
            , batchId);

            return cmd;
        }

        /// <summary>
        /// Inserts new record into vendor table for cascading ownership. 
        /// Occurs after a vendor indicates removal of an item.
        /// Requires that the allvendor table have the updated new owner vendor record
        /// </summary>
        /// <returns></returns>
        public string CreateIOURemovalInsert(string tableName)
        {
            var cmd = string.Format(@"
                insert /*+direct*/ into forecast{0}.{1} 
                select a.* from forecast{0}.tbl_allvendors a
                inner join tmp_iou_removal_updates u
                using(gmsvenid, itemid, storeid)
                where a.gmsvenid = (select distinct gmsvenid from forecast{0}.{1});"
            , _dev
            , tableName);

            return cmd;
        }

        /// <summary>
        /// Inserts new record into vendor table for cascading ownership. 
        /// Occurs after a vendor indicates removal of an item.
        /// Requires that the allvendor table have the updated new owner vendor record
        /// </summary>
        /// <returns></returns>
        public string CreateIOURemovalInsert_MM(string tableName)
        {
            var cmd = string.Format(@"
                insert /*+direct*/ into forecast{0}.{1} 
                select a.* from forecast{0}.tbl_allvendors a
                inner join tmp_iou_removal_updates u
                using(itemid, storeid)
                where u.mm = (select distinct mm from forecast{0}.{1})
                and (a.itemid, a.patch ) not in (select distinct itemid, patch from forecast{0}.{1});"
            , _dev
            , tableName);

            return cmd;
        }

        /// <summary>
        /// Updates vendors table for next-in-line owner or "no vendor" after a valid removal request.
        /// Updates the current vendor and resets editable fields to original values.
        /// If the item in question is a brand new item added through the new items upload, 
        /// item desc and alignment values are updated for vendor uploaded values. 
        /// 
        /// Requires that the ownership table has the updated record for the current owner
        /// </summary>
        /// <returns></returns>
        public string CreateIOURemovalUpdate(string tableName, Boolean isFreeze)
        {
            var cmd = string.Format(@"
                
                --update for newitems first patch level only
                update forecast{0}.{1} t
                set 
                    --vendor descriptors
                     gmsvenid = s.GMSVenID
                    ,VendorDesc = s.VendorDesc
                    ,VBU = s.VBU

                    --new item updates
                    , itemdesc = s.itemdesc
                    , itemconcat = s.itemconcat
                    , prodgrpid = s.prodgrpid
                    , prodgrpdesc = s.prodgrpdesc
                    , prodgrpconcat = s.prodgrpconcat
                    , assrtid = s.assrtid
                    , assrtdesc = s.assrtdesc
                    , assrtconcat = s.assrtconcat
                    , parentid = s.parentid
                    , parentdesc = s.parentdesc
                    , parentconcat = s.parentconcat
                    , mm = s.mm

                    --editable fields
                    ,ASP_FC = s.ASP_fc
                    ,RetailPrice_FC = s.RetailPrice_FC
                    ,Cost_FC = s.Cost_FC
                    ,Vendor_Comments = NULL
                    ,MM_Comments = NULL
                    ,TimeStamp = current_timestamp
                    ,RetailPrice_TY = s.RetailPrice_FC
                    ,RetailPrice_LY = s.Retail_LY
                    ,Cost_TY = s.Cost_FC
                    ,Cost_LY = s.Cost_LY
                    ,SalesUnits_FC = Units_FC_Low
                    ,Units_FC_Vendor = Units_FC_Low
                    {2}
                from tmp_iou_removal_updates s
                where s.itemid = t.itemid and s.storeid = t.storeid and newitemflag is true;

                --update for items that are not from new items upload, patch level columns
                update forecast{0}.{1} t
                set 
                    --vendor descriptors
                     gmsvenid = s.GMSVenID
                    ,VendorDesc = s.VendorDesc
                    ,VBU = s.VBU

                    --editable fields
                    ,ASP_FC = s.ASP_fc
                    ,RetailPrice_FC = s.RetailPrice_FC
                    ,Cost_FC = s.Cost_FC
                    ,Vendor_Comments = NULL
                    ,MM_Comments = NULL
                    ,TimeStamp = current_timestamp
                    ,RetailPrice_TY = s.RetailPrice_FC
                    ,RetailPrice_LY = s.Retail_LY
                    ,Cost_TY = s.Cost_FC
                    ,Cost_LY = s.Cost_LY
                    ,SalesUnits_FC = Units_FC_Low
                    ,Units_FC_Vendor = Units_FC_Low
                    {2}
                from tmp_iou_removal_updates s
               where s.itemid = t.itemid and s.storeid = t.storeid and newitemflag is false;"
            , _dev
            , tableName
            , isFreeze ? "" : ", SalesDollars_FC_Vendor = units_Fc_low * s.asp_fc");

            return cmd;
        }

        /// <summary>
        /// Create a SQL statement for staging items to be removed from a vendors ownership status during a forecast freeze session.
        /// </summary>
        /// <param name="batchid"></param>
        /// <returns></returns>
        public string CreateIOURemovalUpdateStage(string batchid)
        {
            var cmd = string.Format(@"
                create local temp table tmp_iou_removal_updates on commit preserve rows as
                 select o.new_gmsvenid as gmsvenid, o.new_VendorDesc as vendordesc, o.VBU, o.ItemID, o.Patch
                                , case when ov.itemid is not null then true else false end as newitemflag
                                , ov.ItemDesc ,o.itemid||' - '||UPPER(ov.itemdesc) as itemconcat
                                , ov.ProdgrpID, items_p.Prodgrpdesc,ov.prodgrpid||' - '||items_p.prodgrpdesc  as Prodgrpconcat
                                , ov.assrtid, items_a.AssrtDesc, ov.assrtid||' - '||items_a.assrtdesc as assrtconcat
                                , ov.ParentID, coalesce(upper(parents.parentdesc), 'PARENT NEEDED') as parentdesc, ov.parentid||' - '||coalesce(upper(parents.parentdesc), 'PARENT NEEDED') as parentconcat
                                ,CASE WHEN ov.PRODGrpID IS NULL                       
                                            THEN (a.MM)
                                            ELSE CASE WHEN ov.ProdGrpID = 512330
                                                    THEN (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')
                                                    ELSE COALESCE(stores.mm, (select flagstring from forecast{0}.config_tool where flagName = 'No_MM')) END
                                  END as mm
                                , stores.storeid
                                , r.ASP_FC, r.RetailPrice_FC, r.Cost_FC, r.Retail_LY, r.Cost_LY
                        from forecast{0}.upload_IOU_Valid u
                        inner join tmp_IOU_removals_stage o using(ItemID, Patch)
                        left join forecast{0}.itemPatch_ownership_newitemslookup ov on o.new_gmsvenid = ov.gmsvenid and o.ItemID = ov.ItemID and o.Patch = ov.Patch
                        left join(select distinct Prodgrpid, prodgrpdesc from forecast{0}.build_Items) items_P using (prodgrpid)
                        left join(select distinct Assrtid, assrtdesc from forecast{0}.build_Items) items_a using (assrtid)
                        left join(select distinct ItemID,ParentID, ParentDesc from forecast{0}.build_Items) parents on ov.itemid = parents.itemid and ov.parentid = parents.parentid
                        left join(select distinct storeid, patch, mm from forecast{0}.build_stores) stores on o.patch = stores.patch
                        left join forecast{0}.build_Prices r on u.ItemID = r.ItemID and u.Patch = r.Patch
                        left join (select distinct  itemID, StoreID, mm from forecast{0}.tbl_AllVendors) a  on o.ItemID = a.ItemID and stores.storeid = a.storeid
                        where Action = 'R'
                        and batchid = $${1}$$ ;
                
                --special h+P merchant condition
                 create local temp table tmp_iou_removal_mm on commit preserve rows as
                 select distinct o.ItemID, o.Patch, a.mm
                        from forecast{0}.upload_IOU_Valid u
                        inner join tmp_IOU_removals_stage o using(ItemID, Patch)
                        inner join forecast{0}.itemPatch_ownership_newitemslookup ov on o.new_gmsvenid = ov.gmsvenid and o.ItemID = ov.ItemID and o.Patch = ov.Patch
                        left join(select distinct patch, mm from forecast{0}.build_stores) stores on o.patch = stores.patch
                        left join forecast{0}.tbl_allvendors a on u.gmsvenid = a.gmsvenid and u.itemid = a.itemid and u.patch = a.patch
                        where Action = 'R'
                        and batchid = $${1}$$
                        and a.mm <> CASE WHEN ov.ProdGrpID = 512330
                                        THEN (select flagstring from forecast{0}.config_tool where flagName = 'HP_MM')
                                        ELSE COALESCE(stores.mm, (select flagstring from forecast{0}.config_tool where flagName = 'No_MM')) end;

                delete from forecast{0}.itemPatch_ownership_newitemslookup 
                where (gmsvenid, itemid, patch) in (select gmsvenid, itemid, patch from tmp_iou_removal_updates where newitemflag is true);"
            , _dev
            , batchid
            );

            return cmd;
        }

        /// <summary>
        /// Create a SQL statement to insert a list of records into the upload_IOU_Valid table 
        /// with specific actions like 'A' for adding or 'R' for removing a claim.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="itemPatchOverlap"></param>
        /// <param name="batchId"></param>
        /// <returns></returns>
        public string CreateIOUValidTableRecordsByAction(EditorParameterModel editor, ItemPatchOverlap itemPatchOverlap, string batchId)
        {
            var unionList = itemPatchOverlap.ItemPatches.Select(ipo =>
            {
                var select = $"(select {ipo.ItemID} as ItemID, $${ipo.ItemDesc}$$ as ItemDesc, $${ipo.Patch}$$ as Patch, 'R' as Action)";
                return select;
            }).ToList();

            var union = string.Join(" union ", unionList);

            var cmd = string.Format(@"
                insert into Forecast{0}.upload_IOU_Valid
                select v.GMSVenID, v.VendorDesc, v.VBU, u.ItemID, u.ItemDesc, u.Patch, u.Action, $${2}$$ as BatchID
                from (
                        {3}
                ) u
                cross join (select GMSVenID, VendorDesc, VendorID as VBU from forecast{0}.config_vendors where gmsvenid = {1} limit 1) v;"
            , _dev
            , editor.GMSVenID
            , batchId
            , union);

            return cmd;
        }

        public string DropIOUPartitionData(string batchId)
        {
            var cmd = string.Format(@"
                delete from Forecast{0}.upload_IOU_Invalid where BatchID = $${1}$$;
                delete from Forecast{0}.upload_IOU_Stage where BatchID = $${1}$$;
                delete from Forecast{0}.upload_IOU_Valid where BatchID = $${1}$$;"
            , _dev
            , batchId);

            return cmd;
        }

        public string DropNewItemUploadPartition(string batchId)
        {
            var cmd = string.Format(@"select drop_partitions('Forecast{0}.upload_new_items_stage', $${1}$$,$${1}$$);"
            , _dev
            , batchId);

            return cmd;
        }

        public string ImportItemPatchWeek()
        {
            string cmd = string.Empty;

            cmd = string.Format("COPY forecast{0}.tbl_loadItemPatchWeekFile(VBU, Item, Patch, Week, ForecastedCost, ForecastedRetail, ForecastedUnits, BatchID)"
                + " FROM STDIN RECORD TERMINATOR E'\r\n' DELIMITER E',' ABORT ON ERROR NO COMMIT;"
                , _dev);

            return cmd;
        }

        public string ImportItemMM()
        {
            string cmd = string.Empty;

            cmd = string.Format("COPY forecast{0}.tbl_loadItemMMFile(VBU, Item, MM, ForecastedCost, ForecastedRetail, ForecastedUnits, BatchID)"
                + " FROM STDIN RECORD TERMINATOR E'\r\n' DELIMITER E',' ABORT ON ERROR NO COMMIT;"
                , _dev);

            return cmd;
        }

        public string UpdateUploadLog(UploadLog uploadLog)
        {
            var cmd = string.Format(@"
                INSERT INTO Forecast{0}.log_Uploads (
                    GMSVenID
                    , VendorDesc
                    , FileUploadType
                    , FileName
                    , TimeStamp
                    , Success
                    , user_login
                    , SuccessOrFailureMessage
                    , duration) 
                VALUES ({1}, $${2}$$, $${3}$$, $${4}$$, $${5}$$, {6}, $${7}$$, $${8}$$, $${9}$$);"
            , _dev
            , uploadLog.GmsVenId
            , uploadLog.VendorDesc
            , uploadLog.FileUploadType
            , uploadLog.FileName
            , uploadLog.TimeStamp
            , uploadLog.Success
            , uploadLog.UserLogin
            , uploadLog.SuccessOrFailureMessage
            , uploadLog.Duration);

            return cmd;
        }

        #endregion IMPORT

        #region Util

        public static DateTime CheckAndFormatDate(string dateStr, bool maxDate = false)
        {
            DateTime dateTime;

            if (DateTime.TryParse(dateStr, out dateTime))
            {
                return dateTime;
            }
            else
            {
                return maxDate ? DateTime.MaxValue : DateTime.Now;
            }
        }

        public static string GetTimestamp(DateTime value = new DateTime())
        {
            value = (value == new DateTime()) ? DateTime.Now : value;
            return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string GetPublicShema()
        {
            return _dev.Equals("_Dev") ? "dev" : "public";
        }

        #endregion Util
    }
}
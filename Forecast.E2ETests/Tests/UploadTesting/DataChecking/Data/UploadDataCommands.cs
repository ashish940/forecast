using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Tests.UploadTesting.ItemPatch;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class UploadDataCommands
    {
        public List<string> testCasesIOU = new List<string> {
                    "and prodgrpid = 512330   and mm in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)"

                    , "and prodgrpid != 512330 and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)  and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)"

                    , "and prodgrpid = 512330 and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)"

                    , "and prodgrpid != 512330 and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is null)"

                    , "and prodgrpid = 512330 and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is null)"

                    , "and prodgrpid != 512330 and mm not in (select mm from forecast_dev.config_mm )"

                    , "and prodgrpid = 512330 and mm not in (select mm from forecast_dev.config_mm )"

                    };
        public List<string> testCasesNIU = new List<string> {
                    " mm in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)"

                    , "mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)  and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)"

                    , "mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)"

                    , "mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is null)"

                    , "mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is null)"

                    , "mm not in (select mm from forecast_dev.config_mm )"

                    , "mm not in (select mm from forecast_dev.config_mm )"

                    };
        public List<string> testCasesIOUHistory = new List<string> {
                    " mm in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null))"

                    ," mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)  and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null))"

                    , " mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null))"

                    , " mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null) and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is null))"

                    , " mm not in (select mm from forecast_dev.config_mm ))"

                    };

        internal string GetToolState()
        {
            var query = string.Format(@"select flagValue from forecast_dev.config_tool where flagName = 'freeze'");
            return query;
        }

        internal string GetNumberOfOverlappingClaims(string gmsvenid)
        {
            var query = string.Format(@"select count(*) from forecast_dev.itempatch_overlap where gmsvenid = {0}", gmsvenid);
            return query;
        }

        public List<List<bool>> HandPTestCases = new List<List<bool>>() { new List<bool>() { false, true, false, true }, new List<bool>() { false, true, true, false } };

        internal string GetDescription(string id, string column)
        {
            var query = string.Format(@"select distinct {1}desc  from forecast{0}.build_items where {1}id = {2}", "_dev", column, id);

            return query;
        }

        internal string CountAndSumsAllVendors(List<ItemPatch> listOfItemPatches)
        {
            var whereClause = " where (itemid, patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors " + CreateWhereClause(listOfItemPatches) + ")";
            var sumsString = " SUM(SalesUnits_FC)   salesunits_fc, SUM(units_fc_vendor) units_fc_vendor,  SUM(RetailPrice_TY) RetailPrice_TY,  SUM(RetailPrice_LY) RetailPrice_LY,  SUM(RetailPrice_FC) RetailPrice_FC, SUM(Cost_FC) Cost_FC,  SUM(Cost_TY) Cost_TY, SUM(Cost_LY) Cost_LY ";
            var cmd = string.Format(@"select distinct count(itemid) count, vendordesc, {0} from (select distinct itemid, patch, vendordesc, {0} from forecast_dev.tbl_allvendors {1} group by itemid, patch, vendordesc )  a group by vendordesc", sumsString, whereClause);

            return cmd;
        }

        public string DataProviderTemplate()
        {
            var cmd = string.Format(@"SELECT * FROM Forecast{0}.table 
                WHERE ; ", "_dev");

            return cmd;
        }

        public string GetItemPatchDataOLD(string vendor1, string where, bool frozen)
        {
            //var unfrozenOuterSelectString = "";
            //var unfrozenInnerSelectString = "";
            //if (!frozen)
            //{
            //    unfrozenOuterSelectString = ",currentOwner.salesdollars_fc_vendor currentOwner_salesdollars_fc_vendor,allvendors.salesdollars_fc_vendor allvendors_salesdollars_fc_vendor";
            //    unfrozenInnerSelectString = ", sum(salesdollars_fc_vendor) salesdollars_fc_vendor";
            //}

            var cmd = string.Format(@"
                select coalesce(currentOwner.itempatch, allvendors.itempatch) itempatch
                ,currentOwner.itemid currentOwner_itemid, currentOwner.patch currentOwner_patch, allvendors.itemid allvendors_itemid, allvendors.patch allvendors_patch
                ,currentOwner.vendordesc currentOwner_vendordesc, allvendors.vendordesc allvendors_vendordesc
                ,currentOwner.gmsvenid currentOwner_gmsvenid,  allvendors.gmsvenid allvendors_gmsvenid                
                ,currentOwner.itemdesc currentOwner_itemdesc,  allvendors.itemdesc allvendors_itemdesc
                ,currentOwner.itemconcat currentOwner_itemconcat, allvendors.itemconcat allvendors_itemconcat
                ,currentOwner.parentid currentOwner_parentid, allvendors.parentid allvendors_parentid
                ,currentOwner.parentdesc currentOwner_parentdesc,  allvendors.parentdesc allvendors_parentdesc
                ,currentOwner.assrtid currentOwner_assrtid, allvendors.assrtid allvendors_assrtid
                ,currentOwner.assrtdesc currentOwner_assrtdesc, allvendors.assrtdesc allvendors_assrtdesc
                ,currentOwner.assrtconcat currentOwner_assrtconcat,  allvendors.assrtconcat allvendors_assrtconcat
                ,currentOwner.prodgrpid currentOwner_prodgrpid,  allvendors.prodgrpid allvendors_prodgrpid
                ,currentOwner.prodgrpdesc currentOwner_prodgrpdesc,  allvendors.prodgrpdesc allvendors_prodgrpdesc
                ,currentOwner.prodgrpconcat currentOwner_prodgrpconcat, allvendors.prodgrpconcat allvendors_prodgrpconcat
                ,currentOwner.salesunits_fc currentOwner_salesunits_fc,  allvendors.salesunits_fc allvendors_salesunits_fc
                ,currentOwner.units_fc_vendor currentOwner_units_fc_vendor,  allvendors.units_fc_vendor allvendors_units_fc_vendor
                ,currentOwner.units_fc_low currentOwner_units_fc_low,  allvendors.units_fc_low allvendors_units_fc_low
                ,currentOwner.retail_ly currentOwner_retailprice_ly,  allvendors.retail_ly allvendors_retailprice_ly
                ,prices.retail_ly retail_ly_original
                ,currentOwner.retail_ty currentOwner_retailprice_ty,  allvendors.retail_ty allvendors_retailprice_ty
                ,currentOwner.retail_fc currentOwner_retailprice_fc, allvendors.retail_fc allvendors_retailprice_fc
                ,prices.retailprice_fc retail_ty_fc_original
                ,currentOwner.cost_ly currentOwner_cost_ly,  allvendors.cost_ly allvendors_cost_ly
                ,prices.cost_ly cost_ly_original
                ,currentOwner.cost_ty currentOwner_cost_ty, allvendors.cost_ty allvendors_cost_ty
                ,currentOwner.cost_fc currentOwner_cost_fc,  allvendors.cost_fc allvendors_cost_fc
                ,prices.cost_fc cost_ty_fc_original
                
                
                from
                (
                select concat(itemid, patch) itempatch, vendordesc,gmsvenid, itemid, patch,itemdesc,itemconcat,parentid, parentdesc,assrtid,assrtdesc, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat,  sum(salesunits_fc) salesunits_fc,sum(units_fc_vendor) units_fc_vendor,sum(units_fc_low) units_fc_low,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty,avg(cost_fc) cost_fc 
                from forecast_dev.tbl_allvendors {1}
                group by itemid, itemdesc, patch, assrtid,assrtdesc, prodgrpid, vendordesc, gmsvenid, prodgrpdesc,parentid, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) allvendors
                full outer join
                (
                select concat(itemid, patch) itempatch, vendordesc,gmsvenid,  itemid, patch,itemdesc,itemconcat,parentid, parentdesc, assrtid,assrtdesc, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat ,sum(salesunits_fc) salesunits_fc, sum(units_fc_vendor) units_fc_vendor, sum(units_fc_low) units_fc_low,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty, avg(cost_fc) cost_fc 
                from forecast_dev.{0} {1}
                group by itemid, itemdesc, patch,parentid, assrtid,assrtdesc, prodgrpid,vendordesc, gmsvenid, prodgrpdesc, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) currentOwner
                on allvendors.itemid = currentOwner.itemid and allvendors.patch= currentOwner.patch
                

                left join
                forecast_dev.build_prices prices
                on allvendors.itemid = prices.itemid and allvendors.patch = prices.patch

                order by itempatch
                ; ", vendor1, where);

            return cmd;
        }

        internal string GetCustomForecastValueForItemPatch(string selectStatement, string column, ItemPatch itemPatch)
        {
            var where = CreateWhereClause(new List<ItemPatch>() { itemPatch });
            var select = selectStatement + " " + column + " ";

            var query = string.Format(@"select {0}  from forecast{1}.tbl_AllVendors {2}", select, "_dev", where);

            return query;
        }

        public string GetItemPatchData(UploadTest currentTest, string vendor1, string where, bool frozen)
        {
            var cmd2 = "select coalesce(currentOwner.itempatch, allvendors.itempatch) itempatch";
            var currentQueryLine = "";
            var forecastColumns = currentTest.listOfItemPatches[0].currentOwner.forecastColumns;
            var currentOwnerString = currentTest.listOfItemPatches[0].currentOwner.tableNickname;
            var allvendorsString = currentTest.listOfItemPatches[0].allVendors.tableNickname;
            for (var i = 0; i < forecastColumns.Count; i++)
            {
                currentQueryLine += "," + currentOwnerString + "." + forecastColumns[i].columnName + " " + currentOwnerString + "_" + forecastColumns[i].columnName;
                currentQueryLine += "," + allvendorsString + "." + forecastColumns[i].columnName + " " + allvendorsString + "_" + forecastColumns[i].columnName;
            }

            cmd2 += currentQueryLine;
            currentQueryLine = "";
            cmd2 += @",prices.retail_ly retail_ly_original
                ,prices.retailprice_fc retail_ty_fc_original
                , prices.cost_fc cost_ty_fc_original
                 , prices.cost_ly cost_ly_original";
            cmd2 += " from (select concat(itemid, patch) itempatch,";
            cmd2 += GetListOfForecastColumnsForQuery(forecastColumns, false);
            cmd2 += " from forecast_dev.tbl_AllVendors " + where;
            cmd2 += " group by " + GetListOfForecastColumnsForQuery(forecastColumns, true);
            cmd2 += ") " + allvendorsString + " full outer join (";
            cmd2 += " select concat(itemid, patch) itempatch,";
            cmd2 += GetListOfForecastColumnsForQuery(forecastColumns, false);
            cmd2 += " from forecast_dev." + vendor1 + " " + where;
            cmd2 += " group by " + GetListOfForecastColumnsForQuery(forecastColumns, true);
            cmd2 += ") " + currentOwnerString + " on " + allvendorsString + ".patch = " + currentOwnerString + ".patch and " + allvendorsString + ".itemid = " + currentOwnerString + ".itemid ";
            cmd2 += @"left join
                forecast_dev.build_prices prices
                on " + allvendorsString + ".itemid = prices.itemid and " + allvendorsString + ".patch = prices.patch order by itempatch; ";

            var cmd = string.Format(@"
                select coalesce(currentOwner.itempatch, allvendors.itempatch) itempatch
                ,currentOwner.itemid currentOwner_itemid,  allvendors.itemid allvendors_itemid
                ,currentOwner.patch currentOwner_patch, allvendors.patch allvendors_patch
                ,currentOwner.vendordesc currentOwner_vendordesc, allvendors.vendordesc allvendors_vendordesc
                ,currentOwner.gmsvenid currentOwner_gmsvenid,  allvendors.gmsvenid allvendors_gmsvenid                
                ,currentOwner.itemdesc currentOwner_itemdesc,  allvendors.itemdesc allvendors_itemdesc
                ,currentOwner.itemconcat currentOwner_itemconcat, allvendors.itemconcat allvendors_itemconcat
                ,currentOwner.parentid currentOwner_parentid, allvendors.parentid allvendors_parentid
                ,currentOwner.parentdesc currentOwner_parentdesc,  allvendors.parentdesc allvendors_parentdesc
                ,currentOwner.assrtid currentOwner_assrtid, allvendors.assrtid allvendors_assrtid
                ,currentOwner.assrtdesc currentOwner_assrtdesc, allvendors.assrtdesc allvendors_assrtdesc
                ,currentOwner.assrtconcat currentOwner_assrtconcat,  allvendors.assrtconcat allvendors_assrtconcat
                ,currentOwner.prodgrpid currentOwner_prodgrpid,  allvendors.prodgrpid allvendors_prodgrpid
                ,currentOwner.prodgrpdesc currentOwner_prodgrpdesc,  allvendors.prodgrpdesc allvendors_prodgrpdesc
                ,currentOwner.prodgrpconcat currentOwner_prodgrpconcat, allvendors.prodgrpconcat allvendors_prodgrpconcat
                ,currentOwner.salesunits_fc currentOwner_salesunits_fc,  allvendors.salesunits_fc allvendors_salesunits_fc
                ,currentOwner.units_fc_vendor currentOwner_units_fc_vendor,  allvendors.units_fc_vendor allvendors_units_fc_vendor
                ,currentOwner.units_fc_low currentOwner_units_fc_low,  allvendors.units_fc_low allvendors_units_fc_low
                ,currentOwner.retail_ly currentOwner_retailprice_ly,  allvendors.retail_ly allvendors_retailprice_ly                
                ,currentOwner.retail_ty currentOwner_retailprice_ty,  allvendors.retail_ty allvendors_retailprice_ty
                ,currentOwner.retail_fc currentOwner_retailprice_fc, allvendors.retail_fc allvendors_retailprice_fc                
                ,currentOwner.cost_ly currentOwner_cost_ly,  allvendors.cost_ly allvendors_cost_ly                
                ,currentOwner.cost_ty currentOwner_cost_ty, allvendors.cost_ty allvendors_cost_ty
                ,currentOwner.cost_fc currentOwner_cost_fc,  allvendors.cost_fc allvendors_cost_fc
                ,prices.retail_ly retail_ly_original
                ,prices.retailprice_fc retail_ty_fc_original
                ,prices.cost_fc cost_ty_fc_original
                ,prices.cost_ly cost_ly_original
                
                from
                (
                select concat(itemid, patch) itempatch, vendordesc,gmsvenid, itemid, patch,itemdesc,itemconcat,parentid, parentdesc,assrtid,assrtdesc, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat,  sum(salesunits_fc) salesunits_fc,sum(units_fc_vendor) units_fc_vendor,sum(units_fc_low) units_fc_low,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty,avg(cost_fc) cost_fc 
                from forecast_dev.tbl_allvendors {1}
                group by itemid, itemdesc, patch, assrtid,assrtdesc, prodgrpid, vendordesc, gmsvenid, prodgrpdesc,parentid, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) allvendors
                full outer join
                (
                select concat(itemid, patch) itempatch, vendordesc,gmsvenid,  itemid, patch,itemdesc,itemconcat,parentid, parentdesc, assrtid,assrtdesc, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat ,sum(salesunits_fc) salesunits_fc, sum(units_fc_vendor) units_fc_vendor, sum(units_fc_low) units_fc_low,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty, avg(cost_fc) cost_fc 
                from forecast_dev.{0} {1}
                group by itemid, itemdesc, patch,parentid, assrtid,assrtdesc, prodgrpid,vendordesc, gmsvenid, prodgrpdesc, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) currentOwner
                on allvendors.itemid = currentOwner.itemid and allvendors.patch= currentOwner.patch          

                left join
                forecast_dev.build_prices prices
                on allvendors.itemid = prices.itemid and allvendors.patch = prices.patch

                order by itempatch
                ; ", vendor1, where);

            return cmd2;
        }

        internal string GetListOfForecastColumnsForQuery(List<ForecastColumnInfo> forecastColumns, bool groupBy)
        {
            var cmd = "";
            for (var i = 0; i < forecastColumns.Count; i++)
            {
                if (!groupBy)
                {
                    cmd += forecastColumns[i].aggFunction + "(" + forecastColumns[i].columnName + ") " + forecastColumns[i].columnName;
                    cmd += ",";

                }
                else
                {
                    if (forecastColumns[i].aggFunction == "")
                    {
                        cmd += forecastColumns[i].columnName;
                        if (i < (forecastColumns.Count - 1))
                        {
                            cmd += ",";
                        }
                    }
                }
            }

            if (cmd.EndsWith(","))
            {
                cmd = cmd.TrimEnd(',');
            }

            return cmd;
        }

        internal string CreateDupsQuery(List<string> listOfTables)
        {
            var cmd = "select * from ( ";
            for (var i = 0; i < listOfTables.Count; i++)
            {
                cmd += "select count(*) dups, '" + listOfTables.ElementAt(i) + "' TableName from (select count(storeid) over(partition by storeid, itemid, fiscalwk) as dupecount, * from forecast{0}." +
                    listOfTables.ElementAt(i) + " ) t1 where dupecount > 1 ";
                if (i != listOfTables.Count - 1)
                {
                    cmd += " union all ";
                }
            }

            cmd += " ) d where dups !=0";
            cmd = string.Format(cmd, "_dev");

            return cmd;
        }

        internal string CheckFilters(List<string> listOfTables, List<string> listOfFilterTypes)
        {
            listOfTables.Remove("tbl_AllVendors");
            var cmd = "";
            for (var i = 0; i < listOfTables.Count; i++)
            {
                for (var j = 0; j < listOfFilterTypes.Count; j++)
                {
                    cmd += string.Format(@"select * from (select count(filterValue) over (partition by filterValue) Count, filterValue, tablename from (
                        select distinct '{0}' tableName, {1}::varchar(150) filterValue from forecast_dev.{0} 
                        union all
                        select distinct '{0}' tableName, filtervalue from forecast_dev.filters_{0} where filtertype = '{1}') filters
                        ) a where count != 2", listOfTables[i], listOfFilterTypes[j]);
                    if (!(i == listOfTables.Count - 1 && j == listOfFilterTypes.Count - 1))
                    {
                        cmd += " union all ";
                    }
                }
            }

            return cmd;
        }

        internal string GetFilterTypes()
        {

            var cmd = "select distinct filtertype from forecast_dev.filters_tbl_AllVendors where filtertype != 'Count'";

            return cmd;
        }

        internal string CheckMmMdAlighmentAllTables(List<string> listOfTables)
        {
            var cmd = "";
            for (var i = 0; i < listOfTables.Count; i++)
            {
                cmd += string.Format(@"select distinct fpatch, '{0}' source from (select stores.patch spatch, forecast.patch fpatch, stores.storeid sstore, forecast.storeid fstore, stores.mm smm, forecast.mm fmm, stores.md smd , forecast.md fmd from  (select distinct patch, storeid, mm, md from forecast_dev.build_stores ) stores right join (select distinct patch, storeid, mm, md from forecast_dev.{0} where prodgrpid != 512330 ) forecast on stores.storeid = forecast.storeid ) a  where spatch != fpatch or sstore != fstore or smm != fmm or smd != fmd union all select distinct fpatch, '{0}' source from (select stores.patch spatch, forecast.patch fpatch, stores.storeid sstore, forecast.storeid fstore, stores.mm smm, forecast.mm fmm, stores.md smd , forecast.md fmd from  (select distinct patch, storeid, mm, md from forecast_dev.build_stores ) stores right join (select distinct patch, storeid, mm, md from forecast_dev.{0} where prodgrpid = 512330 ) forecast on stores.storeid = forecast.storeid ) a  where spatch != fpatch or sstore != fstore or fmm != 'Matt James' or smd != fmd", listOfTables[i]);
                if (i != listOfTables.Count - 1)
                {
                    cmd += " union all ";
                }
            }

            return cmd;
        }

        internal string CompareCountAndSumAcrossVendorAndAllVendors(List<string> listOfTables)
        {
            var cmd = "select * from (select count(count) over (partition by vendordesc) count, vendordesc from ( select distinct count, vendordesc, salesunits_fc, salesunits_ty, units_fc_vendor from (";
            for (var i = 0; i < listOfTables.Count; i++)
            {
                cmd += "select distinct count(itemid) count, vendordesc, sum(salesunits_fc) salesunits_fc, sum(salesunits_ty) salesunits_ty, sum(units_fc_vendor) units_fc_vendor from (select distinct itemid, patch, vendordesc, sum(SalesUnits_FC) salesunits_fc, sum(salesunits_ty) salesunits_ty, sum(units_fc_vendor) units_fc_vendor from forecast_dev." + listOfTables[i] + " group by itemid, patch, vendordesc )  a group by vendordesc";
                cmd += " union all ";
            }

            for (var i = 0; i < listOfTables.Count; i++)
            {
                cmd += "select distinct count(itemid) count,  concat(vendordesc,'_H&P') vendordesc, sum(salesunits_fc) salesunits_fc, sum(salesunits_ty) salesunits_ty, sum(units_fc_vendor) units_fc_vendor from (select distinct itemid, patch, vendordesc, sum(SalesUnits_FC) salesunits_fc, sum(salesunits_ty) salesunits_ty, sum(units_fc_vendor) units_fc_vendor from forecast_dev." + listOfTables[i] + " where prodgrpid =512330 group by itemid, patch, vendordesc )  a group by vendordesc";
                if (i != listOfTables.Count - 1)
                {
                    cmd += " union all ";
                }
            }

            cmd += " ) a) a) a where count > 1";
            cmd = string.Format(cmd, "_dev");

            return cmd;
        }

        internal string GetForecastTables()
        {
            var cmd = string.Format(@"SELECT distinct TableName FROM Forecast{0}.config_Vendors
                ; ", "_dev");

            return cmd;
        }

        internal string GetMMFromBuildStores(string patch)
        {
            var cmd = string.Format(@"SELECT distinct mm FROM Forecast{0}.build_stores 
                WHERE patch = '{1}'; ", "_dev", patch);

            return cmd;
        }

        internal string GetItemData(string itemid)
        {

            var query = string.Format(@"select * from forecast{0}.build_items where itemid = {1}", "_dev", itemid);

            return query;
        }

        internal string GetForecastValueForItemPatch(string forecastColumn, string aggregateFunction, List<ItemPatch> itemPatch)
        {
            var where = CreateWhereClause(itemPatch);
            var select = "select " + aggregateFunction + "(" + forecastColumn + ") " + forecastColumn + " ";

            var query = string.Format(@"select {0}({1}) {1}  from forecast{2}.tbl_AllVendors {3}", aggregateFunction, forecastColumn, "_dev", CreateWhereClause(itemPatch));

            return query;

        }

        public string IOUNoHistoryQuery(List<string> testCases)
        {
            var cmd = "";
            var select = "";
            for (var testCase = 0; testCase < testCases.Count; testCase++)
            {
                select = string.Format(@"select itemid, patch  from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast{0}.build_items) items
                cross join
                (select distinct patch, mm, md from forecast{0}.build_stores where retid =1 and patch != 'None') patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                {1}
                limit 1
                ) yellow ", "_dev", testCases[testCase]);
                cmd = cmd + select;
                var count = testCases.Count;
                if (testCase != testCases.Count - 1)
                {
                    cmd += " union all ";
                }
            }

            return cmd;
        }

        public string IOUHistoryQuery(List<string> testCases, string tableName)
        {
            var cmd = "";
            var select = "";
            for (var testCase = 0; testCase < testCases.Count; testCase++)
            {
                select = string.Format(@"select * from (
                 select distinct itemid, patch, mm, 'HP_MMfalseMDview' testCase from (select coalesce(forecast.itemid, prices.itemid) itemid, coalesce(forecast.patch, prices.patch) patch, units, retail_ly, retailprice_fc, cost_ly, cost_fc,  prodgrpid, mm from( select sum(units_fc_low) units,  sum(salesunits_fc) salesunitsfc,itemid, patch, prodgrpid, mm from forecast_dev.{2} forecast group by itemid, patch, prodgrpid, mm) forecast
                 full outer join
                 forecast{0}.build_prices prices
                 on forecast.itemid = prices.itemid and forecast.patch = prices.patch
                 where units !=0  and units = salesunitsfc and retail_ly !=0 and retailprice_fc !=0 and cost_ly != 0 and cost_fc !=0) a
                 where units >0
                 and prodgrpid = 512330
                 and patch in (select distinct patch from forecast{0}.build_stores where
                 {1}
                 limit 1
                 ) yellow 
                 union all
                 select * from (
                 select distinct itemid, patch, mm, 'HP_MMfalseMDview' testCase from (select coalesce(forecast.itemid, prices.itemid) itemid, coalesce(forecast.patch, prices.patch) patch, units, retail_ly, retailprice_fc, cost_ly, cost_fc,  prodgrpid, mm from( select sum(units_fc_low) units,  sum(salesunits_fc) salesunitsfc,itemid, patch, prodgrpid, mm from forecast_dev.{2} forecast group by itemid, patch, prodgrpid, mm) forecast
                 full outer join
                 forecast_dev.build_prices prices
                 on forecast.itemid = prices.itemid and forecast.patch = prices.patch
                 where units !=0  and units = salesunitsfc and retail_ly !=0 and retailprice_fc !=0 and cost_ly != 0 and cost_fc !=0) a
                 where units >0
                 and prodgrpid != 512330
                 and patch in (select distinct patch from forecast_dev.build_stores where
                 {1}
                 limit 1) yellow
                ", "_dev", testCases[testCase], tableName);
                cmd = cmd + select;
                var count = testCases.Count;
                if (testCase != testCases.Count - 1)
                {
                    cmd += " union all ";
                }
            }

            return cmd;
        }

        internal string ForecastAllUploadCases(string tableName)
        {
            var cmd = string.Format(@"select split_part(itempatch,'_',1) itemid, split_part(itempatch,'_',2) patch from (
                select distinct first_value(itempatch) over (partition by itempatchhistory, assrthistory order by itempatchhistory, assrthistory) itempatch,  itempatchhistory, assrthistory from (
                select distinct concat(concat(itemid,'_'),patch) itempatch,itemid, patch, forecast.assrtid, 
                case when sum(units_fc_low) >0 then 'ItemPatchUnits'
                when sum(units_fc_low) is null then 'ItemPatchNoUnits'
                when sum(units_fc_low) =0 then 'ItemPatchNoUnits' end as ItemPatchHistory, 
                AssrtHistory from forecast_dev.{0} forecast
                left join(
                select assrtid, 
                case when sum(units_fc_low) >0 then 'AssrtUnits'
                when sum(units_fc_low) is null then 'AssrtNoUnits'
                when sum(units_fc_low) = 0 then 'AssrtNoUnits' end as AssrtHistory from forecast_dev.{0} group by assrtid) assortments
                on assortments.assrtid = forecast.assrtid
                group by itemid, patch, forecast.assrtid, assrthistory
                order by itemid, patch
                )a )a", tableName);

            return cmd;
        }

        internal string NIUItemPatchQueryAllPatches()
        {
            var cmd = string.Format(@"select itemid, patch, prodgrpid prodgrpid_1, parentid parentid_1, assrtid assrtid_1,prodgrpid prodgrpid_2, parentid parentid_2, assrtid assrtid_2 from (
                    select * from (select distinct  itemid from (select randomint(200000000) itemid
                    union all select randomint(200000000) itemid union all select randomint(200000000) itemid union all select randomint(200000000) itemid union all select randomint(200000000) itemid union all select randomint(200000000) itemid) a where itemid not in (select distinct itemid from forecast{0}.tbl_allvendors)
                    limit 1
                    ) items
                    cross join
                    (select distinct patch from forecast{0}.build_stores where patch != 'None') patches cross join
                    (select distinct prodgrpid, parentid,assrtid from forecast_dev.build_items where prodgrpid = 512330 limit 1) iteminfo
                    )a
                     union all
                    select itemid, patch, prodgrpid prodgrpid_1, parentid parentid_1, assrtid assrtid_1,prodgrpid prodgrpid_2, parentid parentid_2, assrtid assrtid_2 from (
                    select * from (select distinct  itemid from (select randomint(200000000) itemid
                    union all select randomint(200000000) itemid union all select randomint(200000000) itemid union all select randomint(200000000) itemid union all select randomint(200000000) itemid union all select randomint(200000000) itemid) a where itemid not in (select distinct itemid from forecast{0}.tbl_allvendors)
                    limit 1
                    ) items
                    cross join
                    (select distinct patch from forecast{0}.build_stores where patch != 'None') patches cross join
                    (select distinct prodgrpid, parentid,assrtid from forecast_dev.build_items where prodgrpid != 512330 limit 1) iteminfo
                    )a ", "_dev");

            return cmd;
        }

        public string NIUItemPatchQuery(List<string> testCases)
        {
            var query = "";

            for (var i = 0; i < testCases.Count; i++)
            {

                for (var limit = 0; limit < 4; limit++)
                {
                    query += "select * from ";
                    var handPWhereClause1 = HandPTestCases.ElementAt(0).ElementAt(limit) ? " prodgrpid = 512330 " : " prodgrpid != 512330 ";
                    var handPWhereClause2 = HandPTestCases.ElementAt(1).ElementAt(limit) ? " prodgrpid = 512330 " : " prodgrpid != 512330 ";

                    var itemColumns1 = ", prodgrpid_1,parentid_1,assrtid_1";
                    var itemColumns2 = ", prodgrpid_2,parentid_2,assrtid_2 ";
                    if (HandPTestCases.ElementAt(0).ElementAt(limit) == HandPTestCases.ElementAt(1).ElementAt(limit))
                    {
                        itemColumns2 = ", prodgrpid_1 prodgrpid_2,parentid_1 parentid_2,assrtid_1 assrtid_2 ";
                    }

                    query += "(select itemid, patch" + itemColumns1 + itemColumns2 + " from (" +
                        "select distinct patch from forecast_dev.build_stores where retid =1 and patch != 'None' and " + testCases.ElementAt(i) + " ) patches " +
                        "cross join" +
                        "( select distinct itemid from (";
                    for (var j = 0; j < 20; j++)
                    {
                        query += " select randomint(20000000) itemid ";
                        if (j != 19)
                        {
                            query += " union ";
                        }
                    }

                    query += ") a where itemid not in (select distinct itemid from forecast_dev.build_items) ) itemids" +
                        " cross join (select distinct prodgrpid_1, parentid_1, assrtid_1,prodgrpid_2, parentid_2, assrtid_2 from( (select distinct prodgrpid prodgrpid_1, parentid parentid_1,assrtid assrtid_1 from forecast_dev.build_items where parentid !=0 and " + handPWhereClause1 + " ) firstFile " +
                        " cross join (select distinct prodgrpid prodgrpid_2, parentid parentid_2,assrtid assrtid_2 from forecast_dev.build_items where parentid !=0 and " + handPWhereClause2 + ") secondFile " +
                        " ) a )b limit 1 )c ";

                    query += " union all ";

                }
            }

            query = query.Substring(0, query.Length - 10);
            return query;
        }

        internal string GetItemPatchOwnershipCount() => throw new NotImplementedException();

        public string YellowFileItemPatches(string vendor1, string vendor2, string vendor3, string where, bool frozen)
        {

            var cmd = string.Format(@"
                --two item/patches with 
                select itemid, patch, 'HP_MMfalseMDview' testCase, mm, md from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast_dev.build_items) items
                cross join
                (select distinct patch, mm, md from forecast_dev.build_stores where retid =1) patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                and prodgrpid = 512330
                and mm in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)
                and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)
                limit 2
                ) yellow
                union all

                select itemid, patch, 'nonHP_MMfalseMDview' testCase, mm, md from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast_dev.build_items) items
                cross join
                (select distinct patch, mm, md from forecast_dev.build_stores where retid =1) patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                and prodgrpid != 512330
                and mm in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)
                and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)
                limit 2
                ) yellow

                union all


                select itemid, patch, 'HP_MMtrueMDview' testCase, mm, md from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast_dev.build_items) items
                cross join
                (select distinct patch, mm, md from forecast_dev.build_stores where retid =1) patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                and prodgrpid = 512330
                and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)
                and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)
                limit 2
                ) yellow

                union all


                select itemid, patch, 'nonHP_MMtrueMDview' testCase, mm, md from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast_dev.build_items) items
                cross join
                (select distinct patch, mm, md from forecast_dev.build_stores where retid =1) patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                and prodgrpid != 512330
                and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)
                and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is not null)
                limit 2
                ) yellow

                union all


                select itemid, patch, 'HP_MMtrueMDnoview' testCase, mm, md from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast_dev.build_items) items
                cross join
                (select distinct patch, mm, md from forecast_dev.build_stores where retid =1) patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                and prodgrpid = 512330
                and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)
                and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is  null)
                limit 2
                ) yellow

                union all


                select itemid, patch, 'nonHP_MMtrueMDnoview' testCase, mm, md from (
                select a.itemid,  a.patch,a.itemdesc, mm, md, prodgrpdesc from (select distinct itemid,  itemdesc, patch, mm,md,prodgrpid, prodgrpdesc from
                (select distinct itemid, itemdesc,prodgrpid, prodgrpdesc from forecast_dev.build_items) items
                cross join
                (select distinct patch, mm, md from forecast_dev.build_stores where retid =1) patch
                ) a
                where (itemid,patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                and prodgrpid != 512330
                and mm in (select mm from forecast_dev.config_mm where mmflag = true and viewname is not null)
                and md in (select mm from forecast_dev.config_mm where mmflag = false and viewname is  null)
                limit 2
                ) yellow
                ; ");

            return cmd;
        }

        public string GetImportProcessID(int gmsvenid)
        {
            var cmd = string.Format(@"SELECT ProcessID FROM Forecast{0}.import_Processes 
                WHERE GMSVenID = {1}; ", "_dev", gmsvenid);

            return cmd;
        }

        public string GetListOfForecastTables()
        {
            var cmd = string.Format(@"SELECT GMSVenID, TableName FROM Forecast{0}.config_Vendors"
                , "_dev");

            return cmd;
        }

        public string GetItemPatchCountInForecastTable(string forecastTable)
        {
            var cmd = string.Format(@"SELECT GMSVenID, TableName FROM Forecast{0}.config_Vendors"
                , forecastTable);

            return cmd;
        }

        public string IOUAllPatchesQuery()
        {
            var cmd = string.Format(@"select first_item itemid, patch from (
                select first_value(itemid) over (partition by patch) first_item, * from (
                select * from (
                select distinct itemid, patch from (select * from forecast_dev.build_items where prodgrpid != 512330) i
                cross join
                (select distinct patch from forecast_dev.build_stores where patch != 'None' )b) a)a
                where (itemid, patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                )b where first_item = itemid
                union all
                select first_item itemid, patch from (
                select first_value(itemid) over (partition by patch) first_item, * from (
                select * from (
                select distinct itemid, patch from (select * from forecast_dev.build_items where prodgrpid = 512330) i
                cross join
                (select distinct patch from forecast_dev.build_stores where patch != 'None' )b) a)a
                where (itemid, patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                )b where first_item = itemid");

            return cmd;
        }

        public string IOUAllItemsQuery()
        {
            var cmd = string.Format(@"select itemid, first_patch patch from (
                select first_value(patch) over (partition by itemid) first_patch, * from (
                select * from (
                select distinct itemid, patch from (select distinct patch from forecast_dev.build_stores where patch != 'None') a
                cross join
                (select distinct itemid from forecast_dev.build_items)b) a)a
                where (itemid, patch) not in (select distinct itemid, patch from forecast_dev.tbl_allvendors)
                )b where first_patch = patch");

            return cmd;
        }

        public string GetLastUploadTimestamp(string gmsvenid)
        {
            var cmd = string.Format(@"SELECT MAX(timestamp) Timestamp FROM Forecast{0}.log_uploads 
                WHERE gmsvenid = {1}; ", "_dev", gmsvenid);

            return cmd;
        }

        public string CreateWhereClause(List<ItemPatch> listOfItemPatches)
        {
            var whereClause = "where ";

            for (var i = 0; i < listOfItemPatches.Count; i++)
            {
                whereClause += "(itemid = '" + listOfItemPatches.ElementAt(i).GetRotationValue("itemid") + "' and patch = '" + listOfItemPatches.ElementAt(i).GetRotationValue("patch") + "') ";
                if (i < listOfItemPatches.Count - 1)
                {
                    whereClause += " or ";
                }
            }

            return whereClause;
        }

        public string WasLastUploadSuccessful(string gmsvenid)
        {
            var cmd = string.Format(@"SELECT Success FROM Forecast{0}.log_uploads 
                WHERE gmsvenid = {1} ORDER BY timestamp desc LIMIT 1; ", "_dev", gmsvenid);

            return cmd;
        }

        public string GetLastSuccessOrFailureMessage(string gmsvenid)
        {
            var cmd = string.Format(@"SELECT SuccessOrFailureMessage FROM Forecast{0}.log_uploads 
                WHERE gmsvenid = {1} ORDER BY timestamp desc LIMIT 1; ", "_dev", gmsvenid);

            return cmd;
        }

        internal string GetVendorTableName(string gmsvenid)
        {
            var cmd = string.Format(@"select TableName from forecast{0}.config_vendors where gmsvenid = {1}; ", "_dev", gmsvenid);

            return cmd;
        }

        internal string GetVendorDesc(string gmsvenid)
        {
            var cmd = string.Format(@"select VendorDesc from forecast{0}.config_vendors where gmsvenid = {1}; ", "_dev", gmsvenid);

            return cmd;
        }

        internal string GetItemPatchFromTable(List<ItemPatch> listOfItemPatches, string tableName)
        {
            var whereClauce = CreateWhereClause(listOfItemPatches);
            var cmd = string.Format(@"select distinct ItemID from forecast_dev.{0} {1}; ", tableName, whereClauce);
            return cmd;
        }
    }
}

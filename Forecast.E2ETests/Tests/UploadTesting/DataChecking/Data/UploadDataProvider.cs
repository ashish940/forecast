using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Forecast.E2ETests.Tests.UploadTesting.DataChecking;
using Forecast.E2ETests.Tests.UploadTesting.ItemPatch;
using Vertica.Data.VerticaClient;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class UploadDataProvider
    {
        private readonly string verticaWebConn = ConfigurationManager.ConnectionStrings["VerticaConnectionString"].ConnectionString;
        private readonly string qvwebconn = ConfigurationManager.ConnectionStrings["QVWebConnectionString"].ConnectionString;
        private readonly UploadDataCommands dataCommands = new UploadDataCommands();

        public string DataProviderTemplateSingleValue(int gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var variable = "";
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().DataProviderTemplate();
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    variable = Convert.ToString(dr["-------"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return variable;
        }

        public bool GetToolState()
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var frozen = true;
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetToolState();
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    frozen = Convert.ToBoolean(dr["flagValue"]);

                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return frozen;
        }

        public string GetNumberOfOverlappingClaims(string gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var variable = "";

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetNumberOfOverlappingClaims(gmsvenid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    variable = Convert.ToString(dr["count"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return variable;
        }

        internal List<string> GetFilterTypes()
        {
            var listOfFilterTypes = new List<string>();
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetFilterTypes();
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var filterName = Convert.ToString(ds.Tables[0].Rows[i]["filtertype"]);
                    listOfFilterTypes.Add(filterName);
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return listOfFilterTypes;
        }

        internal List<string> CheckFilters(List<string> listOfTables)
        {
            var tablesWithMissingFilters = new List<string>();
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.CheckFilters(listOfTables, GetFilterTypes());
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var tableName = Convert.ToString(ds.Tables[0].Rows[i]["TableName"]);
                    tablesWithMissingFilters.Add(tableName);
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return tablesWithMissingFilters;
        }

        public string GetItemData(string itemid, string column)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var result = "";
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetItemData(itemid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    result = Convert.ToString(dr[column]);

                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return result;
        }

        internal List<CountAndSumsCheck> CountAndSumsAllVendorsBefore(List<ItemPatch> listOfItemPatches)
        {
            var countsAndSumsAllVendors = new List<CountAndSumsCheck>();
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.CountAndSumsAllVendors(listOfItemPatches);
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);
                var currentBefore = new List<VariableValue>();

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    currentBefore.Add(new VariableValue("count", Convert.ToString(ds.Tables[0].Rows[i]["count"])));
                    currentBefore.Add(new VariableValue("salesunits_fc", Convert.ToString(ds.Tables[0].Rows[i]["salesunits_fc"])));
                    currentBefore.Add(new VariableValue("units_fc_vendor", Convert.ToString(ds.Tables[0].Rows[i]["units_fc_vendor"])));
                    currentBefore.Add(new VariableValue("retailprice_ty", Convert.ToString(ds.Tables[0].Rows[i]["retailprice_ty"])));
                    currentBefore.Add(new VariableValue("retailprice_ly", Convert.ToString(ds.Tables[0].Rows[i]["retailprice_ly"])));
                    currentBefore.Add(new VariableValue("retailprice_fc", Convert.ToString(ds.Tables[0].Rows[i]["retailprice_fc"])));
                    currentBefore.Add(new VariableValue("cost_fc", Convert.ToString(ds.Tables[0].Rows[i]["cost_fc"])));
                    currentBefore.Add(new VariableValue("cost_ly", Convert.ToString(ds.Tables[0].Rows[i]["cost_ly"])));
                    currentBefore.Add(new VariableValue("cost_ty", Convert.ToString(ds.Tables[0].Rows[i]["cost_ty"])));
                    countsAndSumsAllVendors.Add(new CountAndSumsCheck(Convert.ToString(ds.Tables[0].Rows[i]["vendorDesc"]), currentBefore));
                    currentBefore = new List<VariableValue>();
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return countsAndSumsAllVendors;
        }

        internal List<CountAndSumsCheck> CountAndSumsAllVendorsAfter(List<CountAndSumsCheck> countsAndSumsAllVendors, List<ItemPatch> listOfItemPatches)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.CountAndSumsAllVendors(listOfItemPatches);
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);
                var currentAfter = new List<VariableValue>();
                var index = 0;
                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    for (var j = 0; j < countsAndSumsAllVendors.Count; j++)
                    {
                        if (countsAndSumsAllVendors[j].vendorDesc == Convert.ToString(ds.Tables[0].Rows[i]["vendorDesc"]))
                        {
                            index = j;
                            j = countsAndSumsAllVendors.Count;
                        }
                    }

                    currentAfter.Add(new VariableValue("count", Convert.ToString(ds.Tables[0].Rows[i]["count"])));
                    currentAfter.Add(new VariableValue("salesunits_fc", Convert.ToString(ds.Tables[0].Rows[i]["salesunits_fc"])));
                    currentAfter.Add(new VariableValue("units_fc_vendor", Convert.ToString(ds.Tables[0].Rows[i]["units_fc_vendor"])));
                    currentAfter.Add(new VariableValue("retailprice_ty", Convert.ToString(ds.Tables[0].Rows[i]["retailprice_ty"])));
                    currentAfter.Add(new VariableValue("retailprice_ly", Convert.ToString(ds.Tables[0].Rows[i]["retailprice_ly"])));
                    currentAfter.Add(new VariableValue("retailprice_fc", Convert.ToString(ds.Tables[0].Rows[i]["retailprice_fc"])));
                    currentAfter.Add(new VariableValue("cost_fc", Convert.ToString(ds.Tables[0].Rows[i]["cost_fc"])));
                    currentAfter.Add(new VariableValue("cost_ly", Convert.ToString(ds.Tables[0].Rows[i]["cost_ly"])));
                    currentAfter.Add(new VariableValue("cost_ty", Convert.ToString(ds.Tables[0].Rows[i]["cost_ty"])));
                    countsAndSumsAllVendors[index].after = currentAfter;
                    currentAfter = new List<VariableValue>();
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return countsAndSumsAllVendors;
        }

        public string GetDescriptionFromBuildItems(string id, string column)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var result = "";

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetDescription(id, column);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    result = Convert.ToString(dr[column + "desc"]);

                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return result;
        }

        internal string GetForecastValueForItemPatch(string forecastColumn, string aggregateFunction, ItemPatch itemPatch)
        {
            var rotation = new List<ItemPatch> { itemPatch };
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var forecastValue = "";
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetForecastValueForItemPatch(forecastColumn, aggregateFunction, rotation);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    forecastValue = dr[forecastColumn] is DBNull ? "0" : Convert.ToString(Math.Round(Convert.ToDecimal(dr[forecastColumn]), 2));

                    if (forecastValue == "")
                    {
                        forecastValue = "0";
                    }
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return forecastValue;
        }

        internal string GetCustomForecastValueForItemPatch(string columnSelect, string columnName, ItemPatch itemPatch)
        {
            var rotation = new List<ItemPatch> { itemPatch };
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var forecastValue = "";

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetCustomForecastValueForItemPatch(columnSelect, columnName, itemPatch);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    forecastValue = dr[columnName] is DBNull ? "0" : Convert.ToString(Math.Round(Convert.ToDecimal(dr[columnName]), 2));

                    if (forecastValue == "")
                    {
                        forecastValue = "0";
                    }
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return forecastValue;
        }

        internal string GetMMFromBuildStores(string patch)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var mm = "";

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetMMFromBuildStores(patch);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    mm = Convert.ToString(dr["mm"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                return mm;
            }

            return mm;
        }

        internal List<ItemPatch> GetItemPatchData(UploadTest currentTest)
        {
            var listOfItemPatches = currentTest.listOfItemPatches;
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var ds = new DataSet();

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetItemPatchData(currentTest, currentTest.currentOwner.tableName, dataCommands.CreateWhereClause(listOfItemPatches), GetToolState());
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var itempatch = 0; itempatch < listOfItemPatches.Count; itempatch++)
                {
                    var index = 0;
                    for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        if (ds.Tables[0].Rows[i]["itempatch"].Equals("" + listOfItemPatches.ElementAt(itempatch).GetRotationValue("itemid") + listOfItemPatches.ElementAt(itempatch).GetRotationValue("patch")))
                        {
                            index = i;
                            i = ds.Tables[0].Rows.Count;
                        }
                    }

                    //fill in the forecast values from the query
                    listOfItemPatches.ElementAt(itempatch).currentOwner = GetForecastTableData(listOfItemPatches.ElementAt(itempatch).currentOwner, ds, index);
                    listOfItemPatches.ElementAt(itempatch).allVendors = GetForecastTableData(listOfItemPatches.ElementAt(itempatch).allVendors, ds, index);
                    //listOfItemPatches.ElementAt(itempatch).mattJames = GetForecastTableData(listOfItemPatches.ElementAt(itempatch).mattJames, ds, index);

                    listOfItemPatches.ElementAt(itempatch).originalDataValues = new List<OriginalDataValue>();
                    listOfItemPatches.ElementAt(itempatch).originalDataValues.Add(new OriginalDataValue("retail_LY_Original"
                        , Convert.ToString(ds.Tables[0].Rows[index]["retail_ly_original"])));
                    listOfItemPatches.ElementAt(itempatch).originalDataValues.Add(new OriginalDataValue("retail_TY_FC_Original"
                        , Convert.ToString(ds.Tables[0].Rows[index]["retail_ty_fc_original"])));
                    listOfItemPatches.ElementAt(itempatch).originalDataValues.Add(new OriginalDataValue("cost_LY_Original"
                        , Convert.ToString(ds.Tables[0].Rows[index]["cost_LY_Original"])));
                    listOfItemPatches.ElementAt(itempatch).originalDataValues.Add(new OriginalDataValue("cost_TY_FC_Original"
                        , Convert.ToString(ds.Tables[0].Rows[index]["cost_TY_FC_Original"])));
                    listOfItemPatches.ElementAt(itempatch).originalDataValues.Add(new OriginalDataValue("units_fc_low"
                        , Convert.ToString(ds.Tables[0].Rows[index][currentTest.listOfItemPatches.ElementAt(itempatch).currentOwner.tableNickname + "_units_fc_low"])));
                }
            }
            catch (Exception e)
            {
                connection.Close();
                Debug.WriteLine($"Error in GetItemPatchData: {e.Message}");
                throw e;
            }

            return listOfItemPatches;
        }

        //Checks all forecast tables (AllVendors, MattJames, and Vendor tables) for duplicates across item, store, fiscalwk
        public List<string> CheckForDups()
        {
            var tablesWithDups = new List<string>();
            var listOfTables = GetListOfForecastTables();
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.CreateDupsQuery(listOfTables);
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var tableName = Convert.ToString(ds.Tables[0].Rows[i]["TableName"]);
                    tablesWithDups.Add(tableName);
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return tablesWithDups;
        }

        public List<string> CheckMmMdAlighmentAllTables(List<string> listOfTables)
        {
            var patchesWithBadAlighment = new List<string>();
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.CheckMmMdAlighmentAllTables(listOfTables);
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var patch = Convert.ToString(ds.Tables[0].Rows[i]["fpatch"]);
                    var source = Convert.ToString(ds.Tables[0].Rows[i]["source"]);
                    patchesWithBadAlighment.Add(patch + " " + source);
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return patchesWithBadAlighment;
        }

        public List<string> CompareCountAndSumAcrossVendorAndAllVendors()
        {
            var mismatchingTables = new List<string>();
            var listOfTables = GetListOfForecastTables();
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.CompareCountAndSumAcrossVendorAndAllVendors(listOfTables);
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var tableName = Convert.ToString(ds.Tables[0].Rows[i]["vendordesc"]);
                    mismatchingTables.Add(tableName);
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return mismatchingTables;
        }

        public List<string> GetListOfForecastTables()
        {
            var listOfTables = new List<string>();
            listOfTables.Add("tbl_AllVendors");
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetForecastTables();
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var tableName = Convert.ToString(ds.Tables[0].Rows[i]["TableName"]);
                    listOfTables.Add(tableName);

                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return listOfTables;
        }

        public ForecastTableData GetForecastTableData(ForecastTableData table, DataSet ds, int index)
        {
            for (var i = 0; i < table.forecastColumns.Count; i++)
            {
                table.forecastColumns[i].dataValue = Convert.ToString(ds.Tables[0].Rows[index][table.tableNickname + "_" + table.forecastColumns.ElementAt(i).columnName]);
            }

            return table;
        }

        public List<ItemPatch> GetListOfItemPatches(string query, bool IOU)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var listOfItemPatches = new List<ItemPatch>();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = string.Format(query);
                var ds = new DataSet();
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var itemID = Convert.ToString(ds.Tables[0].Rows[i]["itemid"]);
                    var patch = Convert.ToString(ds.Tables[0].Rows[i]["patch"]);
                    var newItemPatch = new ItemPatch(itemID, patch);
                    if (!IOU)
                    {
                        for (var j = 0; j < newItemPatch.NIUItemInfo_1.Count; j++)
                        {
                            var column_1 = newItemPatch.NIUItemInfo_1.ElementAt(j).columnName;
                            var column_2 = newItemPatch.NIUItemInfo_2.ElementAt(j).columnName;
                            newItemPatch.NIUItemInfo_1[j].dataValue = Convert.ToString(ds.Tables[0].Rows[i][column_1]);
                            newItemPatch.NIUItemInfo_2[j].dataValue = Convert.ToString(ds.Tables[0].Rows[i][column_2]);
                        }
                    }

                    listOfItemPatches.Add(newItemPatch);
                }

                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return listOfItemPatches;
        }

        public int GetImportProcessID(int gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var importProcess = -1;

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetImportProcessID(gmsvenid);
                var dr = command.ExecuteReader();

                //We only need to know about the first one because we search on the three keys of the 
                //bookmark table so it will only return one row for this specific request
                if (dr.Read())
                {
                    importProcess = Convert.ToInt32(dr["ProcessID"]);

                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return importProcess;
        }
        
        public DateTime GetLastUploadTimestamp(string gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var LastUploadTime = DateTime.Today.AddDays(-1);
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetLastUploadTimestamp(gmsvenid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    LastUploadTime = Convert.ToDateTime(dr["Timestamp"]);

                }

                dr.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                connection.Close();
                throw e;
            }

            return LastUploadTime;
        }

        public bool WasLastUploadSuccessful(string gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var success = false;
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.WasLastUploadSuccessful(gmsvenid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    success = Convert.ToBoolean(dr["Success"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                connection.Close();
                throw e;
            }

            return success;
        }

        public string GetLastSuccessOrFailureMessage(string gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var message = "";

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetLastSuccessOrFailureMessage(gmsvenid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    message = Convert.ToString(dr["SuccessOrFailureMessage"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return message;
        }

        internal string GetVendorTableName(string gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var tableName = "";

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetVendorTableName(gmsvenid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    tableName = Convert.ToString(dr["TableName"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                connection.Close();

                if (e.Message.Contains("A connection attempt failed because the connected party did not properly respond"))
                {
                    return GetVendorTableName(gmsvenid);
                }
            }

            return tableName;
        }

        internal string GetVendorDesc(string gmsvenid)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var vendorDesc = "";
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = dataCommands.GetVendorDesc(gmsvenid);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    vendorDesc = Convert.ToString(dr["VendorDesc"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return vendorDesc;
        }

        public bool IsItemPatchInTable(string tableName, List<ItemPatch> listOfItemPatches)
        {
            var adapter = new VerticaDataAdapter();
            var connection = new VerticaConnection(verticaWebConn);
            var exists = false;

            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = new UploadDataCommands().GetItemPatchFromTable(listOfItemPatches, tableName);
                var dr = command.ExecuteReader();

                if (dr.Read())
                {
                    exists = true;
                    var result = dr["ItemID"] is DBNull ? "" : Convert.ToString(dr["ItemID"]);
                }

                dr.Close();
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
                throw;
            }

            return exists;
        }
    }
}


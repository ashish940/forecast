using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Tests.UploadTesting.DataChecking;
using OpenQA.Selenium;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class DataChecker
    {
        public readonly IWebDriver webDriver;
        public TestHelpers helper;
        public ForecastWebPage actions;
        readonly UploadDataProvider dataProvider = new UploadDataProvider();
        public DataChecklist checkList;
        bool passed = true;
        //List<string> errors = new List<string>();
        public string lastActionString = "";
        public List<ItemPatch> itemPatchList = new List<ItemPatch>();
        public List<CountAndSumsCheck> ListOfVendorTableSumsandCountAllVendors = new List<CountAndSumsCheck>();
        public bool frozen = true;
        public UploadTest currentTest;
        public ItemPatch currentItemPatch;
        string expectedValue;
        string databaseValue;
        string fileColumnName;
        string databaseColumnName;

        public DataChecker(IWebDriver webDriver, UploadTest currentTest)
        {
            this.webDriver = webDriver;
            helper = new TestHelpers();
            actions = new ForecastWebPage(webDriver);
            frozen = dataProvider.GetToolState();
            this.currentTest = currentTest;
            checkList = new DataChecklist(currentTest);
        }

        public TestOutcome CheckForDups()
        {
            var tablesWithDups = dataProvider.CheckForDups();

            if (tablesWithDups.Count > 0)
            {
                var errorMessage = "There are dups in the following tables: ";
                for (var i = 0; i < tablesWithDups.Count; i++)
                {
                    errorMessage += tablesWithDups.ElementAt(i);
                    if (i < tablesWithDups.Count - 1)
                    {
                        errorMessage += "; ";
                    }
                }

                checkList.AddRow("Dups check: " + errorMessage, false);
                return new TestOutcome(false, errorMessage);
            }

            checkList.AddRow("Dups check ", true);
            return new TestOutcome(true);
        }

        public TestOutcome CheckFilters()
        {
            var listOfTables = new List<string>();
            for (var i = 0; i < currentTest.users.Count; i++)
            {
                if (currentTest.users[i].checkItemPatchExistance)
                {
                    listOfTables.Add(currentTest.users[i].tableName);
                }
            }

            var filtersMissing = dataProvider.CheckFilters(listOfTables);

            if (filtersMissing.Count > 0)
            {
                var errorMessage = "The following tables are missing filters: ";
                for (var i = 0; i < filtersMissing.Count; i++)
                {
                    errorMessage += filtersMissing.ElementAt(i);
                    if (i < filtersMissing.Count - 1)
                    {
                        errorMessage += "; ";
                    }
                }

                checkList.AddRow("Filters Check: " + errorMessage, false);
                return new TestOutcome(false, errorMessage);
            }

            checkList.AddRow("Filters Check ", true);
            return new TestOutcome(true);
        }

        public TestOutcome CheckMmMdAlighmentAllTables()
        {

            var tablesWithDups = dataProvider.CheckMmMdAlighmentAllTables(GetListOfCurrentForecastTables());

            if (tablesWithDups.Count > 0)
            {
                var errorMessage = "There are bad mm/md alighments for the following patche/tables: ";
                for (var i = 0; i < tablesWithDups.Count; i++)
                {
                    errorMessage += tablesWithDups.ElementAt(i);
                    if (i < tablesWithDups.Count - 1)
                    {
                        errorMessage += "; ";
                    }
                }

                checkList.AddRow("Patch MM MD alighment check : " + errorMessage, false);
                return new TestOutcome(false, errorMessage);
            }

            checkList.AddRow("Patch MM MD alighment check : ", true);
            return new TestOutcome(true);
        }

        public List<string> GetListOfCurrentForecastTables()
        {
            var listOfTables = new List<string>();
            listOfTables.Add("tbl_AllVendors");
            // listOfTables.Add("tbl_MattJames");
            for (var i = 0; i < currentTest.users.Count; i++)
            {
                listOfTables.Add(currentTest.users[i].tableName);
            }

            return listOfTables;
        }

        public TestOutcome CheckCountAndSumAcrossVendorAndAllVendors()
        {
            var tablesWithDups = dataProvider.CompareCountAndSumAcrossVendorAndAllVendors();

            if (tablesWithDups.Count > 0)
            {
                var errorMessage = "The following tables don't match between AllVendors or MattJames and the Vendor table: ";
                for (var i = 0; i < tablesWithDups.Count; i++)
                {
                    errorMessage += tablesWithDups.ElementAt(i);
                    if (i < tablesWithDups.Count - 1)
                    {
                        errorMessage += "; ";
                    }
                }

                checkList.AddRow("Count and Sums matching across tables check: " + errorMessage, false);
                return new TestOutcome(false, errorMessage);
            }

            checkList.AddRow("Count and Sums matching across tables check:", true);
            return new TestOutcome(true);
        }

        public void CheckSumsAndCountsAllVendorsBeforeAndAfter(List<ItemPatch> listOfItemPatches, bool before)
        {
            var changedVendors = new List<VariableValue>();
            if (before)
            {
                ListOfVendorTableSumsandCountAllVendors = dataProvider.CountAndSumsAllVendorsBefore(listOfItemPatches);
            }
            else
            {
                ListOfVendorTableSumsandCountAllVendors = dataProvider.CountAndSumsAllVendorsAfter(ListOfVendorTableSumsandCountAllVendors, listOfItemPatches);

                for (var i = 0; i < ListOfVendorTableSumsandCountAllVendors.Count; i++)
                {
                    for (var j = 0; j < ListOfVendorTableSumsandCountAllVendors[i].before.Count; j++)
                    {

                        if (ListOfVendorTableSumsandCountAllVendors[i].before[j].value != ListOfVendorTableSumsandCountAllVendors[i].after[j].value && !currentTest.listOfUserVendorNames.Contains(ListOfVendorTableSumsandCountAllVendors[i].vendorDesc))
                        {
                            changedVendors.Add(new VariableValue(ListOfVendorTableSumsandCountAllVendors[i].vendorDesc, ListOfVendorTableSumsandCountAllVendors[i].before[j].column));
                        }
                    }
                }

                var error = "";
                for (var i = 0; i < changedVendors.Count; i++)
                {
                    error += changedVendors[i].column + " column " + changedVendors[i].value;
                }

                if (changedVendors.Count > 0)
                {
                    passed = false;
                    checkList.AddRow("ItemPatches count or sums was changed but shouldn't have been for: " + error, false);
                }
                else
                {
                    checkList.AddRow("ItemPatche count and sums remained the same for other vendors. ", true);
                }
            }
        }

        public void CheckOwnershipInVendorTable()
        {
            var itemid = currentItemPatch.GetRotationValue("itemid");
            var patch = currentItemPatch.GetRotationValue("patch");
            //check that ownership of item patch is correct

            if (!(currentItemPatch.currentOwner.GetForecastColumn("gmsvenid").dataValue.Equals(currentTest.currentOwner.gmsvenid) && currentItemPatch.currentOwner.GetForecastColumn("vendordesc").dataValue.Equals(currentTest.currentOwner.vendorDesc)))
            {
                checkList.AddRow("Ownership", itemid, patch, currentItemPatch.currentOwner.GetForecastColumn("gmsvenid").dataValue, currentTest.currentOwner.gmsvenid, false);
                checkList.AddRow("Ownership", itemid, patch, currentItemPatch.currentOwner.GetForecastColumn("vendordesc").dataValue, currentTest.currentOwner.vendorDesc, false);
                passed = false;
            }
            else
            {
                checkList.AddRow("Ownership", itemid, patch, currentItemPatch.currentOwner.GetForecastColumn("gmsvenid").dataValue, currentTest.currentOwner.gmsvenid, true);
                checkList.AddRow("Ownership", itemid, patch, currentItemPatch.currentOwner.GetForecastColumn("vendordesc").dataValue, currentTest.currentOwner.vendorDesc, true);
            }
        }

        public void CheckNewItemItemInfo()
        {
            string column;
            var itemid = currentItemPatch.GetRotationValue("itemid");
            var patch = currentItemPatch.GetRotationValue("patch");
            //check ids against the NIU file
            for (var itemColumn = 0; itemColumn < currentItemPatch.currentOwner.forecastColumns.Count; itemColumn++)
            {
                if (currentItemPatch.currentOwner.forecastColumns[itemColumn].dataCheckerHelper == "itemInfo")
                {
                    column = currentItemPatch.currentOwner.forecastColumns[itemColumn].columnName;
                    databaseValue = currentItemPatch.currentOwner.forecastColumns[itemColumn].dataValue;
                    expectedValue = currentTest.currentOwner.NIU.GetValueFromFile(currentTest.currentOwner.NIU.fileContents, itemid, patch, currentTest.currentOwner.NIU.GetFileHeaderName(column), true);
                    if (databaseValue != expectedValue)
                    {
                        checkList.AddRow(column, itemid, patch, databaseValue, expectedValue, false);
                        passed = false;
                    }
                    else
                    {
                        checkList.AddRow(column, itemid, patch, databaseValue, expectedValue, true);
                    }
                }
            }

            for (var concatColumn = 0; concatColumn < currentItemPatch.currentOwner.forecastColumns.Count; concatColumn++)
            {
                if (currentItemPatch.currentOwner.forecastColumns[concatColumn].dataCheckerHelper == "concat")
                {
                    column = currentItemPatch.currentOwner.forecastColumns[concatColumn].columnName;
                    var parentColumnName = currentItemPatch.currentOwner.forecastColumns[concatColumn].columnName.Substring(0, column.Length - "concat".Length);
                    var idColumn = parentColumnName + "id";
                    var descColumn = parentColumnName + "desc";
                    databaseValue = currentItemPatch.currentOwner.forecastColumns[concatColumn].dataValue;
                    //string correctValue = "";
                    var idValue = currentTest.currentOwner.NIU.GetValueFromFile(currentTest.currentOwner.NIU.fileContents, itemid, patch, currentTest.currentOwner.NIU.GetFileHeaderName(idColumn), true);
                    string descValue;
                    if (descColumn == "itemdesc")
                    {
                        descValue = currentTest.currentOwner.NIU.GetValueFromFile(currentTest.currentOwner.NIU.fileContents, itemid, patch, currentTest.currentOwner.NIU.GetFileHeaderName(descColumn), true);
                    }
                    else
                    {
                        descValue = dataProvider.GetDescriptionFromBuildItems(idValue, parentColumnName);
                    }

                    expectedValue = idValue + " - " + descValue;

                    if (expectedValue != databaseValue)
                    {
                        checkList.AddRow(column, itemid, patch, databaseValue, expectedValue, false);
                        passed = false;
                    }
                    else
                    {
                        checkList.AddRow(column, itemid, patch, databaseValue, expectedValue, true);
                    }
                }
            }
        }

        public void CheckValue(string databaseValue, string expectedValue)
        {
            var itemid = currentItemPatch.GetRotationValue("itemid");
            var patch = currentItemPatch.GetRotationValue("patch");
            var isNumber = double.TryParse(expectedValue, out var number);

            if (isNumber)
            {
                var result = Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue);
                if ((Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue)).Equals(0.0))
                {
                    checkList.AddRow(fileColumnName, itemid, patch, databaseValue, expectedValue, false);
                    passed = false;

                }
                else
                {
                    checkList.AddRow(fileColumnName, itemid, patch, databaseValue, expectedValue, true);
                }
            }
            else
            {
                if (databaseValue != expectedValue)
                {
                    checkList.AddRow(fileColumnName, itemid, patch, databaseValue, expectedValue, false);
                    passed = false;

                }
                else
                {
                    checkList.AddRow(fileColumnName, itemid, patch, databaseValue, expectedValue, true);
                }
            }
        }

        public void CheckForecastValuesVendorTable(bool trueForForecastFileFalseForOriginal)
        {
            var isFrozen = dataProvider.GetToolState();
            var itemid = currentItemPatch.GetRotationValue("itemid");
            var patch = currentItemPatch.GetRotationValue("patch");
            var index = currentTest.listOfItemPatches.IndexOf(currentItemPatch);
            switch (trueForForecastFileFalseForOriginal)
            {

                case true:

                    for (var j = 0; j < currentTest.currentOwner.Forecast.columnNamesMetric.Count; j++)
                    {
                        databaseColumnName = currentTest.currentOwner.Forecast.columnNamesMetric.ElementAt(j).databaseColumnName;
                        fileColumnName = currentTest.currentOwner.Forecast.columnNamesMetric.ElementAt(j).fileColumnName;
                        databaseValue = currentItemPatch.currentOwner.GetForecastColumn(databaseColumnName).dataValue;
                        expectedValue = currentTest.currentOwner.Forecast.GetValueFromFile(currentTest.currentOwner.Forecast.fileContents, itemid, patch, fileColumnName, false);
                        CheckValue(databaseValue, expectedValue);
                    }

                    if (isFrozen)
                    {
                        fileColumnName = "units_fc_vendor";
                        databaseColumnName = "units_fc_vendor";
                        expectedValue = currentTest.currentOwner.Forecast.GetValueFromFile(currentTest.currentOwner.Forecast.fileContents, itemid, patch, "Sales Units FY", false);
                        databaseValue = currentItemPatch.currentOwner.GetForecastColumn(databaseColumnName).dataValue;
                        CheckValue(databaseValue, expectedValue);
                    }
                    else
                    {
                        //check units_fc_vendor_original for unfrozen state
                        databaseColumnName = "units_fc_vendor";
                        fileColumnName = "units_fc_vendor";

                        expectedValue = currentItemPatch.unitsFCVendorPreUpload;
                        databaseValue = currentItemPatch.currentOwner.GetForecastColumn(databaseColumnName).dataValue;
                        CheckValue(databaseValue, expectedValue);

                        //check salesdollars_fc_vendor_original for unfrozen state                        
                        fileColumnName = "salesdollars_fc_vendor";
                        expectedValue = currentItemPatch.salesDollarsFCVendorPreUpload;
                        databaseValue = dataProvider.GetForecastValueForItemPatch("salesdollars_fc_vendor", "sum", currentItemPatch);
                        CheckValue(databaseValue, expectedValue);
                    }

                    break;
                case false:
                    for (var j = 0; j < currentItemPatch.originalDataValues.Count; j++)
                    {
                        expectedValue = currentItemPatch.originalDataValues.ElementAt(j).dataValue;
                        //string columnName = "";
                        for (var v = 0; v < currentItemPatch.currentOwner.forecastColumns.Count; v++)
                        {
                            if (currentItemPatch.currentOwner.forecastColumns.ElementAt(v).dataCheckerHelper == currentItemPatch.originalDataValues.ElementAt(j).columnName)
                            {
                                databaseValue = currentItemPatch.currentOwner.forecastColumns.ElementAt(v).dataValue;
                                fileColumnName = currentItemPatch.currentOwner.forecastColumns.ElementAt(v).columnName;
                                CheckValue(databaseValue, expectedValue);
                            }
                        }
                    }

                    if (!isFrozen)
                    {
                        fileColumnName = "salesdollars_fc_vendor";
                        expectedValue = dataProvider.GetCustomForecastValueForItemPatch("sum(asp_fc * units_fc_low)", "salesdollars_fc", currentItemPatch);
                        databaseValue = dataProvider.GetForecastValueForItemPatch("salesdollars_fc_vendor", "sum", currentItemPatch);
                        CheckValue(databaseValue, expectedValue);
                    }

                    break;
                default:
                    break;
            }
        }

        public void CheckOtherVendorTablesForItemPatch()
        {
            var itemid = currentItemPatch.GetRotationValue("itemid");
            var patch = currentItemPatch.GetRotationValue("patch");
            for (var user = 0; user < currentTest.users.Count; user++)
            {
                if (currentTest.users[user] != currentTest.currentOwner && currentTest.users[user].checkItemPatchExistance)
                {
                    if (dataProvider.IsItemPatchInTable(currentTest.users[user].tableName, new List<ItemPatch>() { currentItemPatch }))
                    {
                        checkList.AddRow("ItemPatch not in " + currentTest.users[user].tableName, itemid, patch, false);
                        passed = false;
                    }
                    else
                    {
                        checkList.AddRow("ItemPatch not in " + currentTest.users[user].tableName, itemid, patch, true);
                    }
                }
            }
        }

        public void CurrentOwnerMatchedAllVendors()
        {
            var itemid = currentItemPatch.GetRotationValue("itemid");
            var patch = currentItemPatch.GetRotationValue("patch");
            for (var column = 0; column < currentItemPatch.currentOwner.forecastColumns.Count; column++)
            {
                var colName = currentItemPatch.currentOwner.forecastColumns.ElementAt(column).columnName;
                var currentOwnerValue = currentItemPatch.currentOwner.forecastColumns[column].dataValue;
                var allvendorsValue = currentItemPatch.allVendors.forecastColumns[column].dataValue;
                //string mattJamesValue = currentTest.listOfItemPatches[i].mattJames.forecastColumns[column].dataValue;
                if (currentOwnerValue != allvendorsValue)
                {
                    checkList.AddRow("VendorTable and AllVendors Table dont match for " + colName, itemid, patch, currentOwnerValue, allvendorsValue, false);
                    passed = false;
                }
                else
                {
                    checkList.AddRow("VendorTable and AllVendors Table match for " + colName, itemid, patch, currentOwnerValue, allvendorsValue, true);
                }
            }
        }

        public bool CheckItemPatchData(UploadTest currentTest, bool trueForForecastFileFalseForOriginal, bool IOU)
        {
            CheckMmMdAlighmentAllTables();
            CheckForDups();
            //CheckFilters();
            currentTest.listOfItemPatches = dataProvider.GetItemPatchData(currentTest);
            for (var i = 0; i < currentTest.listOfItemPatches.Count; i++)
            {
                currentItemPatch = currentTest.listOfItemPatches[i];
                CheckOwnershipInVendorTable();

                if (IOU)
                {
                    //add check for existing item info   
                }
                else
                {
                    CheckNewItemItemInfo();
                }

                CheckForecastValuesVendorTable(trueForForecastFileFalseForOriginal);
                CheckOtherVendorTablesForItemPatch();
                CurrentOwnerMatchedAllVendors();

            }

            checkList.CreateLogCSV();
            lastActionString = "";
            return passed;
        }
    }
}

//bool isFrozen = dataProvider.GetToolState();
//switch (trueForForecastFileFalseForOriginal)
//{

//    case true:

//        for(int j = 0; j<currentTest.currentOwner.Forecast.columnNamesMetric.Count; j++)
//        {
//            databaseColumnName = currentTest.currentOwner.Forecast.columnNamesMetric.ElementAt(j).databaseColumnName;
//            fileColumnName = currentTest.currentOwner.Forecast.columnNamesMetric.ElementAt(j).fileColumnName;
//            databaseValue = currentTest.listOfItemPatches.ElementAt(i).currentOwner.GetForecastColumn(databaseColumnName).dataValue;
//            expectedValue = currentTest.currentOwner.Forecast.GetValueFromFile(currentTest.currentOwner.Forecast.fileContents,itemid, patch, fileColumnName,false);
//            //if (!dataProvider.GetToolState() && currentTest.currentOwner.Forecast.columnnames)
//            //{
//            //    checkAgainst = currentTest.listOfItemPatches[i].units_fc_vendor_original;
//            //}
//            if(Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue) != 0)
//            {
//                checkList.AddRow(fileColumnName, itemid, patch, databaseValue, expectedValue, false);
//                passed = false;
//                errors.Add("Last Action: " + lastActionString + ". " + fileColumnName + " for itemid " + itemid + " and patch " + patch + " doesn't match the uploaded forecast file. TableValue: " + databaseValue + " ; correct Value from file: " + checkAgainst + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//                //return new TestOutcome(false, fileColumnName+" for itemid " + itemid + " and patch " + patch +" doesn't match the uploaded forecast file");
//            } 
//            else
//                checkList.AddRow(fileColumnName, itemid, patch, databaseValue, expectedValue, true);
//        }
//        if (isFrozen)
//        {
//            expectedValue = currentTest.currentOwner.Forecast.GetValueFromFile(currentTest.currentOwner.Forecast.fileContents, itemid, patch, "Sales Units FY", false);
//            databaseValue = currentTest.listOfItemPatches.ElementAt(i).currentOwner.GetForecastColumn("units_fc_vendor").dataValue;
//            if (Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue) != 0)
//            {
//                checkList.AddRow("Units_fc_vendor", itemid, patch, databaseValue, expectedValue, false);
//                passed = false;
//                errors.Add("Last Action: " + lastActionString + ". " + "Units_fc_vendor" + " for itemid " + itemid + " and patch " + patch + " doesn't match the uploaded forecast file. TableValue: " + databaseValue + " ; correct Value from file: " + checkAgainst + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//            }
//            else
//                checkList.AddRow("Units_fc_vendor", itemid, patch, databaseValue, expectedValue, true);
//        }
//        else
//        {
//            //check units_fc_vendor_original for unfrozen state
//            expectedValue = currentTest.listOfItemPatches[i].units_fc_vendor_preUpload;
//            databaseValue = currentTest.listOfItemPatches.ElementAt(i).currentOwner.GetForecastColumn("units_fc_vendor").dataValue;
//            if (Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue) != 0)
//            {
//                checkList.AddRow("Units_fc_vendor", itemid, patch, databaseValue, expectedValue, false);
//                passed = false;
//                errors.Add("Last Action: " + lastActionString + ". " + "Units_fc_vendor" + " for itemid " + itemid + " and patch " + patch + " doesn't match the uploaded forecast file. TableValue: " + databaseValue + " ; correct Value from file: " + checkAgainst + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//            }
//            else
//                checkList.AddRow("Units_fc_vendor", itemid, patch, databaseValue, expectedValue, true);

//            //check salesdollars_fc_vendor_original for unfrozen state
//            expectedValue = currentTest.listOfItemPatches[i].salesdollars_fc_vendor_preUpload;
//            databaseValue = dataProvider.GetForecastValueForItemPatch("salesdollars_fc_vendor","sum",currentTest.listOfItemPatches[i]);
//            if (Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue) != 0)
//            {
//                checkList.AddRow("salesdollars_fc_vendor", itemid, patch, databaseValue, expectedValue, false);
//                passed = false;
//                errors.Add("Last Action: " + lastActionString + ". " + "salesdollars_fc_vendor" + " for itemid " + itemid + " and patch " + patch + " doesn't match the original value. TableValue: " + databaseValue + " ; correct Value from file: " + checkAgainst + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//            }
//            else
//                checkList.AddRow("salesdollars_fc_vendor", itemid, patch, databaseValue, expectedValue, true);

//        }
//        break;
//    case false:

//        for(int j = 0; j<currentTest.listOfItemPatches.ElementAt(i).originalDataValues.Count; j++)
//        {
//            expectedValue = currentTest.listOfItemPatches.ElementAt(i).originalDataValues.ElementAt(j).dataValue;
//            string columnName = "";

//            for (int v = 0; v<currentTest.listOfItemPatches.ElementAt(i).currentOwner.forecastColumns.Count; v++)
//            {
//                if(currentTest.listOfItemPatches.ElementAt(i).currentOwner.forecastColumns.ElementAt(v).dataCheckerHelper == currentTest.listOfItemPatches.ElementAt(i).originalDataValues.ElementAt(j).columnName)
//                {
//                    databaseValue = currentTest.listOfItemPatches.ElementAt(i).currentOwner.forecastColumns.ElementAt(v).dataValue;
//                    columnName = currentTest.listOfItemPatches.ElementAt(i).currentOwner.forecastColumns.ElementAt(v).columnName;
//                    if (Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue) !=0)
//                    {

//                        checkList.AddRow(columnName, itemid, patch, databaseValue, expectedValue, false);
//                        passed = false;
//                        errors.Add("Last Action: " + lastActionString + ". " + columnName + " for itemid " + itemid + " and patch " + patch + " doesn't match the original value. Table Value: " + databaseValue + " original Value: " + originalValue + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//                        //return new TestOutcome(false, columnName + " for itemid " + itemid + " and patch " + patch + " doesn't match the original value");
//                    }
//                    else
//                        checkList.AddRow(columnName, itemid, patch, databaseValue, expectedValue, true);
//                }
//            }                           

//        }
//        if (!isFrozen)
//        {
//            expectedValue = dataProvider.GetCustomForecastValueForItemPatch("sum(asp_fc * units_fc_low)","salesdollars_fc", currentTest.listOfItemPatches[i]);
//            databaseValue = dataProvider.GetForecastValueForItemPatch("salesdollars_fc_vendor", "sum", currentTest.listOfItemPatches[i]);
//            if (Convert.ToDecimal(databaseValue) - Convert.ToDecimal(expectedValue) != 0)
//            {
//                checkList.AddRow("salesdollars_fc_vendor", itemid, patch, databaseValue, expectedValue, false);
//                passed = false;
//                errors.Add("Last Action: " + lastActionString + ". " + "salesdollars_fc_vendor" + " for itemid " + itemid + " and patch " + patch + " doesn't match the asp*units_fc_low value. TableValue: " + databaseValue + " ; correct Value from file: " + checkAgainst + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//            }
//            else
//                checkList.AddRow("salesdollars_fc_vendor", itemid, patch, databaseValue, expectedValue, true);
//        }

//        break;

//    default:

//        break;

//}
//item patch shouldn't be in other vendor's tables
//for(int user = 0; user<currentTest.users.Count; user++)
//{
//    if(currentTest.users[user] != currentTest.currentOwner)
//    {
//        if(dataProvider.IsItemPatchInTable(currentTest.users[user].tableName,new List<ItemPatch>() {currentTest.listOfItemPatches[i] }))
//        {
//            checkList.AddRow("Ownership check other vendor tables", itemid, patch,  false);
//            errors.Add("Last Action: " + lastActionString + ". " + "ItemID " + itemid + " and patch " + patch + " are in " + currentTest.users[user].vendorDesc + "'s table. They should only be in " + currentTest.currentOwner.vendorDesc + "'s table." + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//        }
//        else
//            checkList.AddRow("Ownership check other vendor tables", itemid, patch, true);
//    }
//}

//current owner's table should match allvendors table and if H&P it matches mattjames table

//for(int column = 0; column < currentTest.listOfItemPatches[i].currentOwner.forecastColumns.Count; column++)
//{
//    string colName = currentTest.listOfItemPatches[i].currentOwner.forecastColumns.ElementAt(column).columnName;
//    string currentOwnerValue = currentTest.listOfItemPatches[i].currentOwner.forecastColumns[column].dataValue;
//    string allvendorsValue = currentTest.listOfItemPatches[i].allVendors.forecastColumns[column].dataValue;
//    //string mattJamesValue = currentTest.listOfItemPatches[i].mattJames.forecastColumns[column].dataValue;
//    if(currentOwnerValue != allvendorsValue)
//    {
//        checkList.AddRow("VendorTable and AllVendors Table dont match for " + colName, itemid, patch, currentOwnerValue,allvendorsValue, false);
//        errors.Add(colName + " Data for ItemID " + itemid + " and patch " + patch + " dont match between the vendor table and allvendors table" + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");

//    }
//    else
//        checkList.AddRow("VendorTable and AllVendors Table match for " + colName, itemid, patch, currentOwnerValue, allvendorsValue, true);

//if (currentTest.listOfItemPatches[i].currentOwner.GetForecastColumn("prodgrpid").dataValue == "512330") 
//{
//    if (mattJamesValue != currentOwnerValue)
//    {
//        checkList.AddRow("VendorTable and MattJames Table dont match for " + colName, itemid, patch, mattJamesValue, currentOwnerValue, false);
//        errors.Add("Last Action: " + lastActionString + ". " + colName + "Data for ItemID " + itemid + " and patch " + patch + " dont' match between the vendor table and mattJames table. It is an H&P item." + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//    }
//    else
//        checkList.AddRow("VendorTable and MattJames Table match for " + colName, itemid, patch, mattJamesValue, currentOwnerValue, true);
//}

//if (currentTest.listOfItemPatches[i].currentOwner.GetForecastColumn("prodgrpid").dataValue != "512330")
//{
//    if (mattJamesValue != "") 
//    {
//        checkList.AddRow("VendorTable and MattJames Table dont match for " + colName, itemid, patch,mattJamesValue,"", false);
//        errors.Add("Last Action: " + lastActionString + ". " + colName + "Data for ItemID " + itemid + " and patch " + patch + " is in the MattJames table and shouldn't be because it isn't an H&P item." + ". Index of ItemPatch in ListOfItemPatches is " + i + ".");
//    }
//    else
//        checkList.AddRow("Item is not H&P and therefore correctly has no data in the MattJames table for  " + colName, itemid, patch,mattJamesValue,"", true);
//}

//}
//if (!isFrozen)
//{
//    checkAgainst = dataProvider.GetCustomForecastValueForItemPatch("sum(asp_fc*units_fc_vendor)", "salesdollars_fc_vendor",currentTest.listOfItemPatches[i]);
//    databaseValue = dataProvider.GetForecastValueForItemPatch("SalesDollars_fc_vendor", "sum", currentTest.listOfItemPatches[i]);
//    if(checkAgainst != databaseValue)
//    {
//        checkList.AddRow("SalesDollars_fc_vendor is not correct for " , itemid, patch, databaseValue, checkAgainst, false);
//        errors.Add("Last Action: " + lastActionString + ". SalesDollars_fc_vendor is not correct for itemid " + itemid +" patch "  + patch) ;
//    }
//    else
//        checkList.AddRow("Checking SalesDollars_fc_vendor ", itemid, patch, databaseValue, checkAgainst, true);

//}

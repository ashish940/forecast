using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Tests.UploadTesting.UploadFileTypes;

namespace Forecast.E2ETests.Global.UploadTesting.UploadFileTypes
{
    class ForecastUpload : Upload
    {
        readonly UploadDataProvider dataProvider = new UploadDataProvider();
        readonly string fileName;
        readonly List<ItemPatch> listOfItemPatches = new List<ItemPatch>();
        public List<FileColumns> columnNamesRotation;
        public List<FileColumns> columnNamesMetric;
        string filePath;
        readonly UploadTest currentTest;
        readonly int adjustment;

        public List<List<string>> fileContents = new List<List<string>>();

        public ForecastUpload(UploadTest currentTest, string filename, int adjustment)
        {
            fileName = filename + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";
            this.currentTest = currentTest;
            listOfItemPatches = currentTest.listOfItemPatches;
            this.adjustment = adjustment;
            CreateItemPatchFile();
            CreateCSV(fileContents, fileName);
        }

        public void FillFileContentsWithValues()
        {
            //add rotation headers to fileContents
            for (var i = 0; i < columnNamesRotation.Count; i++)
            {
                fileContents.Add(new List<string>());
                fileContents.ElementAt(i).Add(columnNamesRotation.ElementAt(i).fileColumnName);
            }

            //add rotation values to fileConte nts
            for (var i = 0; i < listOfItemPatches.Count; i++)
            {
                for (var j = 0; j < listOfItemPatches.ElementAt(i).rotations.Count; j++)
                {
                    fileContents.ElementAt(j).Add(listOfItemPatches.ElementAt(i).rotations.ElementAt(j).value);
                }
            }

            for (var i = 0; i < columnNamesMetric.Count; i++)
            {
                fileContents.Add(new List<string>());
                fileContents.ElementAt(i + columnNamesRotation.Count).Add(columnNamesMetric.ElementAt(i).fileColumnName);
            }

            for (var i = 0; i < columnNamesMetric.Count; i++)
            {
                for (var j = 0; j < listOfItemPatches.Count; j++)
                {
                    var value = Convert.ToString(Convert.ToDecimal(dataProvider.GetForecastValueForItemPatch(columnNamesMetric.ElementAt(i).databaseColumnName, columnNamesMetric.ElementAt(i).aggFunction, listOfItemPatches.ElementAt(j))) + adjustment);
                    if (Convert.ToDecimal(value) < 0)
                    {
                        value = "0";
                    }

                    fileContents.ElementAt(i + columnNamesRotation.Count).Add(value);
                }
            }

            filePath = CreateFilePathForUpload(fileName);
        }

        public void CreateItemPatchFile()
        {
            columnNamesRotation = new List<FileColumns>
                {
                    new FileColumns("Item", "ItemID")
                    , new FileColumns("Patch", "Patch")
                };
            columnNamesMetric = new List<FileColumns>
            {
                new FileColumns("Cost LY", "cost_ly", "AVG")
                , new FileColumns("Cost TY", "cost_ty", "AVG")
                , new FileColumns("Cost FY","cost_fc", "AVG")
                , new FileColumns("Retail Price LY","retailprice_ly", "AVG")
                , new FileColumns("Retail Price TY", "retailprice_ly", "AVG")
                , new FileColumns("Retail Price FY","retailprice_fc", "AVG")
                , new FileColumns("Sales Units FY","salesunits_fc", "SUM")
            };

            FillFileContentsWithValues();
        }

        public TestOutcome UploadFile()
        {
            currentTest.dataChecker.CheckSumsAndCountsAllVendorsBeforeAndAfter(currentTest.listOfItemPatches, true);
            var state = currentTest.dataProvider.GetToolState();
            if (!state)
            {
                for (var i = 0; i < currentTest.listOfItemPatches.Count; i++)
                {
                    listOfItemPatches[i].unitsFCVendorPreUpload = dataProvider.GetForecastValueForItemPatch("units_fc_vendor", "sum", listOfItemPatches[i]);

                    listOfItemPatches[i].salesDollarsFCVendorPreUpload = dataProvider.GetForecastValueForItemPatch("salesdollars_fc_vendor", "sum", listOfItemPatches[i]);
                }
            }

            currentTest.actions.OpenUploadsModal(currentTest.webDriver);
            var chooseFileButton = currentTest.actions.helper.GetElementById(currentTest.webDriver, "uploadBrowse");
            //string path = CreateFilePath("UploadFiles", fileName);
            chooseFileButton.SendKeys(filePath);
            var LastUploadTime = dataProvider.GetLastUploadTimestamp(currentTest.currentUser.gmsvenid);
            var NewUploadTime = LastUploadTime.AddDays(-1);
            // int checkDates = DateTime.Compare(LastUploadTime, NewUploadTime);
            currentTest.webPage.ClickButton("upload");
            var alertText = currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);
            if (!alertText.Contains("success"))
            {
                return new TestOutcome(false, "Forecast still processing for this vendor. Wait until the upload is finished to test again or chosoe another vendor");
            }

            //wait for upload to finish and make sure it was successful
            //int counter = 0;
            var compare = DateTime.Compare(LastUploadTime, NewUploadTime);
            while (DateTime.Compare(LastUploadTime, NewUploadTime) >= 0)
            {
                //counter++;
                var tempTime = dataProvider.GetLastUploadTimestamp(currentTest.currentUser.gmsvenid);
                if (DateTime.Compare(LastUploadTime, tempTime) < 0)
                {
                    NewUploadTime = dataProvider.GetLastUploadTimestamp(currentTest.currentUser.gmsvenid);
                }
            }

            if (!dataProvider.WasLastUploadSuccessful(currentTest.currentUser.gmsvenid))
            {
                return new TestOutcome(false, "Forecast upload failed. Failure message from log_uploads: " + dataProvider.GetLastSuccessOrFailureMessage(currentTest.currentUser.gmsvenid));

            }

            currentTest.dataChecker.lastActionString += currentTest.currentUser.vendorDesc + " Forecast. ";
            currentTest.dataChecker.CheckSumsAndCountsAllVendorsBeforeAndAfter(currentTest.listOfItemPatches, false);

            return new TestOutcome(true);
        }
    }
}

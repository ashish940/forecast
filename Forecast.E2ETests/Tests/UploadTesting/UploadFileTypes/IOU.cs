using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Global.UploadTesting.UploadFileTypes;
using Forecast.E2ETests.Tests.UploadTesting.UploadFileTypes;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class IOU : Upload
    {
        readonly string fileName;

        //UploadActions actions;
        readonly UploadDataProvider dataProvider = new UploadDataProvider();
        readonly List<ItemPatch> listOfItemPatches = new List<ItemPatch>();
        List<FileColumns> columnNamesRotation;
        List<FileColumns> columnNamesMetric;
        string filePath;
        readonly UploadTest currentTest;
        readonly string addOwnership;
        readonly string primaryVendor;
        readonly bool addOwnershipBool;
        public List<List<string>> fileContents = new List<List<string>>();
        public IOU(UploadTest currentTest, string fileName, bool addOwnership, bool primaryVendor)
        {
            this.fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";
            this.currentTest = currentTest;
            listOfItemPatches = currentTest.listOfItemPatches;
            addOwnershipBool = addOwnership;
            this.addOwnership = addOwnership ? "A" : "R";
            this.primaryVendor = primaryVendor ? "Y" : "N";
            CreateIOUFile();
            CreateCSV(fileContents, this.fileName);
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
                    fileContents.ElementAt(i + columnNamesRotation.Count).Add(columnNamesMetric.ElementAt(i).databaseColumnName);
                }
            }

            filePath = CreateFilePathForUpload(fileName);
        }

        public void CreateIOUFile()
        {
            columnNamesRotation = new List<FileColumns>
                {
                    new FileColumns("Item", "ItemID")
                    , new FileColumns("Patch", "Patch")
                };
            columnNamesMetric = new List<FileColumns>
            {
                new FileColumns("Action",addOwnership)
                , new FileColumns("Primary Vendor",primaryVendor)

            };

            FillFileContentsWithValues();
        }

        public TestOutcome UploadFile()
        {
            currentTest.dataChecker.CheckSumsAndCountsAllVendorsBeforeAndAfter(currentTest.listOfItemPatches, true);

            currentTest.actions.OpenUploadsModal(currentTest.webDriver);
            var chooseFileButton = currentTest.actions.helper.GetElementById(currentTest.webDriver, "upload_iou_browse");
            //string path = CreateFilePath("UploadFiles", fileName);
            chooseFileButton.SendKeys(filePath);
            var LastUploadTime = dataProvider.GetLastUploadTimestamp(currentTest.currentUser.gmsvenid);
            var NewUploadTime = LastUploadTime.AddDays(-1);
            // int checkDates = DateTime.Compare(LastUploadTime, NewUploadTime);
            currentTest.webPage.ClickButton("iou_upload");
            var alertText = "";
            while (currentTest.webPage.IsElementVisible(currentTest.webPage.GetElementById(currentTest.webDriver, "updating-background")))
            {
                alertText = currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);
            }

            NIU lastNIUUpdated;

            if (addOwnershipBool)
            {
                currentTest.ownership.Add(currentTest.currentUser);
            }
            else
            {
                currentTest.ownership.Remove(currentTest.currentUser);
                if (!currentTest.IsIOU)
                {
                    lastNIUUpdated = currentTest.currentOwner.NIU;
                    for (var i = 0; i < currentTest.users.Count; i++)
                    {
                        if (currentTest.users[i].gmsvenid == "0")
                        {
                            currentTest.users[i].NIU = lastNIUUpdated;
                        }
                    }
                }
            }

            currentTest.currentOwner = currentTest.ownership.Count == 0 ? currentTest.users[currentTest.users.Count - 1] : currentTest.ownership[0];
            if (addOwnershipBool)
            {
                currentTest.dataChecker.lastActionString += currentTest.currentUser.vendorDesc + " IOU. ";
            }
            else
            {
                currentTest.dataChecker.lastActionString += currentTest.currentUser.vendorDesc + " rem claim. ";
            }

            currentTest.dataChecker.CheckFilters();
            currentTest.dataChecker.CheckSumsAndCountsAllVendorsBeforeAndAfter(currentTest.listOfItemPatches, false);
            return new TestOutcome(true);
        }
    }
}

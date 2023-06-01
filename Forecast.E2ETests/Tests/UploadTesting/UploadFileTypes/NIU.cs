using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Tests.UploadTesting.ItemPatch;
using Forecast.E2ETests.Tests.UploadTesting.UploadFileTypes;

namespace Forecast.E2ETests.Global.UploadTesting.UploadFileTypes
{
    class NIU : Upload
    {
        readonly string fileName;

        //UploadActions actions;
        readonly UploadDataProvider dataProvider = new UploadDataProvider();
        readonly List<ItemPatch> listOfItemPatches = new List<ItemPatch>();
        List<FileColumns> columnNamesRotation;
        List<FileColumns> columnNamesItemInfo;
        string filePath;
        readonly UploadTest currentTest;
        readonly string addOwnership;
        readonly string primaryVendor;
        readonly bool changeItemInfoValues;
        readonly string vendorDesc;
        public List<List<string>> fileContents = new List<List<string>>();

        public NIU(UploadTest currentTest, string fileName, bool primaryVendor, bool changeItemInfoValues, string vendorName)
        {
            this.fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";
            this.currentTest = currentTest;
            listOfItemPatches = currentTest.listOfItemPatches;
            for (var i = 0; i < listOfItemPatches.Count; i++)
            {
                listOfItemPatches.ElementAt(i).currentItemInfo = changeItemInfoValues ? listOfItemPatches.ElementAt(i).NIUItemInfo_2 : listOfItemPatches.ElementAt(i).NIUItemInfo_1;
            }

            this.primaryVendor = primaryVendor ? "Y" : "N";

            this.changeItemInfoValues = changeItemInfoValues;
            vendorDesc = vendorName;
            CreateNIUFile();
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
            //add rest of headers
            for (var i = 0; i < columnNamesItemInfo.Count; i++)
            {
                fileContents.Add(new List<string>());
                fileContents.ElementAt(i + columnNamesRotation.Count).Add(columnNamesItemInfo.ElementAt(i).fileColumnName);
            }

            var itemDescCounter = 0;
            var duplicateItemDesc = "";
            //add rotation values to fileConte nts
            for (var i = 0; i < listOfItemPatches.Count; i++)
            {

                if (fileContents[0].Contains(listOfItemPatches[i].rotations[0].value))
                {
                    duplicateItemDesc = fileContents[1][fileContents[0].IndexOf(listOfItemPatches[i].rotations[0].value)];
                    fileContents.ElementAt(1).Add(duplicateItemDesc.ToUpper());
                }
                else
                {
                    fileContents.ElementAt(1).Add((vendorDesc + "_Item" + itemDescCounter).ToUpper());
                }

                fileContents.ElementAt(0).Add(listOfItemPatches.ElementAt(i).rotations.ElementAt(0).value);

                fileContents.ElementAt(2).Add(listOfItemPatches.ElementAt(i).rotations.ElementAt(1).value);
            }

            var itemInfo = new List<NewItemInfo>();

            for (var i = 0; i < columnNamesItemInfo.Count; i++)
            {
                for (var j = 0; j < listOfItemPatches.Count; j++)
                {
                    if (columnNamesItemInfo.ElementAt(i).fileColumnName == "Primary Vendor")
                    {
                        fileContents.ElementAt(i + columnNamesRotation.Count).Add(columnNamesItemInfo.ElementAt(i).databaseColumnName);
                    }
                    else
                    {
                        itemInfo = changeItemInfoValues ? listOfItemPatches.ElementAt(j).NIUItemInfo_2 : listOfItemPatches.ElementAt(j).NIUItemInfo_1;
                        //string value;
                        for (var x = 0; x < itemInfo.Count; x++)
                        {
                            if (itemInfo.ElementAt(x).fileColumnName == fileContents.ElementAt(i + columnNamesRotation.Count).ElementAt(0))
                            {
                                fileContents.ElementAt(i + columnNamesRotation.Count).Add(itemInfo.ElementAt(x).dataValue);
                            }
                        }
                    }
                }
            }

            filePath = CreateFilePathForUpload(fileName);
        }

        public string GetFileHeaderName(string databaseColumnName)
        {
            var headerName = "";

            for (var i = 0; i < columnNamesRotation.Count; i++)
            {

                if (columnNamesRotation.ElementAt(i).databaseColumnName.ToLower() == databaseColumnName.ToLower())
                {
                    return columnNamesRotation.ElementAt(i).fileColumnName;
                }
            }

            for (var i = 0; i < columnNamesItemInfo.Count; i++)
            {

                if (columnNamesItemInfo.ElementAt(i).databaseColumnName.ToLower() == databaseColumnName.ToLower())
                {
                    return columnNamesItemInfo.ElementAt(i).fileColumnName;
                }
            }

            if (headerName == "")
            {
                throw new ArgumentException("NIU GetFileHeaderName: column name not found");
            }

            return headerName;
        }

        public void CreateNIUFile()
        {
            columnNamesRotation = new List<FileColumns>
                {
                    new FileColumns("Item", "ItemID")
                    , new FileColumns("Item Desc", "ItemDesc")
                    , new FileColumns("Patch", "Patch")
                };
            columnNamesItemInfo = new List<FileColumns>
            {
                new FileColumns("ProdGrp","ProdGrpID")
                ,new FileColumns("Parent","ParentID")
                ,new FileColumns("Assortment","AssrtID")
                , new FileColumns("Primary Vendor",primaryVendor)

            };

            FillFileContentsWithValues();
        }

        public TestOutcome UploadFile()
        {
            currentTest.dataChecker.CheckSumsAndCountsAllVendorsBeforeAndAfter(currentTest.listOfItemPatches, true);
            currentTest.actions.OpenUploadsModal(currentTest.webDriver);
            var chooseFileButton = currentTest.actions.helper.GetElementById(currentTest.webDriver, "upload_new_item_browse");
            //string path = CreateFilePath("UploadFiles", fileName);
            chooseFileButton.SendKeys(filePath);
            var LastUploadTime = dataProvider.GetLastUploadTimestamp(currentTest.currentUser.gmsvenid);
            var NewUploadTime = LastUploadTime.AddDays(-1);
            // int checkDates = DateTime.Compare(LastUploadTime, NewUploadTime);
            currentTest.webPage.ClickButton("new_item_upload");
            var alertText = "";
            while (currentTest.webPage.IsElementVisible(currentTest.webPage.GetElementById(currentTest.webDriver, "updating-background")))
            {
                alertText = currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);
            }

            if (!currentTest.ownership.Contains(currentTest.currentUser))
            {
                currentTest.ownership.Add(currentTest.currentUser);
            }

            currentTest.currentOwner = currentTest.ownership[0];
            currentTest.dataChecker.lastActionString += currentTest.currentUser.vendorDesc + " NIU. ";
            currentTest.dataChecker.CheckFilters();
            currentTest.dataChecker.CheckSumsAndCountsAllVendorsBeforeAndAfter(currentTest.listOfItemPatches, false);
            return new TestOutcome(true);
        }
    }
}

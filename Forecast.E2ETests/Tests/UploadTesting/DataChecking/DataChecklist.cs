using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Forecast.E2ETests.Tests.UploadTesting.DataChecking
{
    class DataChecklist
    {
        List<string> checkList = new List<string>();
        readonly Global.UploadTesting.UploadTest currentTest;

        public DataChecklist(Global.UploadTesting.UploadTest currentTest)
        {
            this.currentTest = currentTest;
        }

        public void AddRow(string description, string itemID, string patch, string databaseValue, string expectedValue, bool passed)
        {
            var row = description + "," + itemID + "," + patch + "," + databaseValue + "," + expectedValue + "," + passed.ToString();
            checkList.Add(row);

        }

        public void AddRow(string description, string itemID, string patch, bool passed)
        {
            var row = description + "," + itemID + "," + patch + ",,," + passed.ToString();
            checkList.Add(row);

        }

        public void AddRow(string description, bool passed)
        {
            var row = description + ",,,,," + passed.ToString();
            checkList.Add(row);

        }
        public void AddDescription(string description)
        {
            var row = description + ",,,,,,";
            checkList.Insert(0, row);

        }

        public void CreateLogCSV()
        {
            AddDescription(currentTest.dataChecker.lastActionString + "Owner is " + currentTest.currentOwner.vendorDesc);
            using (var sw = new StreamWriter(CreateFilePathForLog()))
            {
                var strData = "";
                for (var i = 0; i < checkList.Count; i++)
                {
                    strData = checkList.ElementAt(i);
                    strData += "\n";
                    sw.Write(strData);
                    strData = "";
                }

                sw.Flush();
            }

            checkList = new List<string>();
        }

        public string CreateFilePathForLog()
        {
            var pathtest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "");
            var testRootDirectory = "Forecast.E2ETests";
            var pathtest2 = pathtest.Substring(0, pathtest.LastIndexOf("Forecast.E2ETests") + testRootDirectory.Length);
            var path = Path.Combine(pathtest2, "Logs", "TestLog_" + currentTest.testCaseName + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv");
            return path;
        }
    }
}

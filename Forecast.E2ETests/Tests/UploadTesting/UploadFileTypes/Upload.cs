using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Forecast.E2ETests.Global.UploadTesting.UploadFileTypes
{
    abstract class Upload
    {
        public void CreateCSV(List<List<string>> fileContents, string fileName)
        {
            using (var sw = new StreamWriter(CreateFilePathForUpload(fileName)))
            {
                var strData = "";
                for (var i = 0; i < fileContents.ElementAt(0).Count; i++)
                {
                    for (var j = 0; j < fileContents.Count; j++)
                    {
                        strData += fileContents.ElementAt(j).ElementAt(i);
                        if (j < fileContents.Count - 1)
                        {
                            strData += ",";
                        }
                    }

                    strData += "\n";
                    sw.Write(strData);
                    strData = "";
                }

                sw.Flush();
            }
        }

        public string CreateFilePathForUpload(string fileName)
        {
            var pathtest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "");
            var testRootDirectory = "Forecast.E2ETests";
            var pathtest2 = pathtest.Substring(0, pathtest.LastIndexOf("Forecast.E2ETests") + testRootDirectory.Length);
            var path = Path.Combine(pathtest2, "UploadFiles", fileName);
            return path;
        }

        public string GetValueFromFile(List<List<string>> fileContents, string itemid, string patch, string headerName, bool NIU)
        {
            var value = "";
            int patchPosition;
            var rowNumber = -1;
            patchPosition = NIU ? 2 : 1;

            for (var i = 0; i < fileContents.ElementAt(0).Count; i++)
            {
                var item = fileContents.ElementAt(0).ElementAt(i);
                var patchdebug = fileContents.ElementAt(patchPosition).ElementAt(i);
                if (fileContents.ElementAt(0).ElementAt(i) == itemid && fileContents.ElementAt(patchPosition).ElementAt(i) == patch)
                {
                    rowNumber = i;
                }
            }

            for (var i = 0; i < fileContents.Count; i++)
            {
                if (fileContents.ElementAt(i).ElementAt(0) == headerName)
                {
                    value = fileContents.ElementAt(i).ElementAt(rowNumber);
                }
            }

            return value;
        }
    }
}

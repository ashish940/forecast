using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Global.UploadTesting;
using Forecast.E2ETests.Tests.UploadTesting.ItemPatch;

namespace Forecast.E2ETests.Global
{
    class ItemPatch
    {
        public List<RotationValue> rotations = new List<RotationValue>();
        readonly List<string> columnNames = new List<string>();
        public ForecastTableData currentOwner, allVendors;
        readonly UploadTesting.UploadTest currentTest;

        public string retailLYOriginal, retailTYFCOriginal, costLYOriginal, costTYFCOriginal;
        public string unitsFCVendorPreUpload, salesDollarsFCVendorPreUpload;
        public List<OriginalDataValue> originalDataValues = new List<OriginalDataValue>();
        //only for NIU cases
        public List<NewItemInfo> NIUItemInfo_1 = new List<NewItemInfo>();
        public List<NewItemInfo> NIUItemInfo_2 = new List<NewItemInfo>();
        public List<NewItemInfo> currentItemInfo;

        public ItemPatch(string itemID, string patch)
        {
            // this.itemID = itemID;
            //this.patch = patch;
            rotations.Add(new RotationValue("itemid", itemID));
            rotations.Add(new RotationValue("patch", patch));

            currentOwner = new ForecastTableData("currentOwner");
            allVendors = new ForecastTableData("allvendors");

            NIUItemInfo_1.Add(new NewItemInfo("prodgrpid_1", "ProdGrp", ""));
            NIUItemInfo_1.Add(new NewItemInfo("parentid_1", "Parent", ""));
            NIUItemInfo_1.Add(new NewItemInfo("assrtid_1", "Assortment", ""));

            NIUItemInfo_2.Add(new NewItemInfo("prodgrpid_2", "ProdGrp", ""));
            NIUItemInfo_2.Add(new NewItemInfo("parentid_2", "Parent", ""));
            NIUItemInfo_2.Add(new NewItemInfo("assrtid_2", "Assortment", ""));

        }

        public ItemPatch(List<RotationValue> rotations)
        {
            this.rotations = rotations;
            currentOwner = new ForecastTableData("currentOwner");
            allVendors = new ForecastTableData("allvendors");

            NIUItemInfo_1.Add(new NewItemInfo("prodgrpid_1", "ProdGrp", ""));
            NIUItemInfo_1.Add(new NewItemInfo("parentid_1", "Parent", ""));
            NIUItemInfo_1.Add(new NewItemInfo("assrtid_1", "Assortment", ""));

            NIUItemInfo_2.Add(new NewItemInfo("prodgrpid_2", "ProdGrp", ""));
            NIUItemInfo_2.Add(new NewItemInfo("parentid_2", "Parent", ""));
            NIUItemInfo_2.Add(new NewItemInfo("assrtid_2", "Assortment", ""));
        }

        public bool CheckItemPatchData(bool isFrozen, string currentOwner) => false;

        public RotationValue GetRotation(string columnName)
        {
            for (var i = 0; i < rotations.Count; i++)
            {
                if (rotations.ElementAt(i).rotationColumn.Equals(columnName))
                {
                    return rotations.ElementAt(i);
                }
            }

            throw new ArgumentException("rotation could not be found");
        }

        public string GetRotationValue(string columnName)
        {
            for (var i = 0; i < rotations.Count; i++)
            {
                if (rotations.ElementAt(i).rotationColumn.ToLower().Equals(columnName.ToLower()))
                {
                    return rotations.ElementAt(i).value;
                }
            }

            throw new ArgumentException("rotation could not be found");
        }
    }
}

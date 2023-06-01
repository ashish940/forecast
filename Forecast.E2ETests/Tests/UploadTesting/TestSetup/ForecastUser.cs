using Forecast.E2ETests.Global.UploadTesting.UploadFileTypes;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class ForecastUser
    {
        public string gmsvenid;
        public string tableName;
        readonly UploadDataProvider dataProvider;
        public IOU IOU;
        public NIU NIU;
        public ForecastUpload Forecast;
        public IOU Remove;
        public string vendorDesc;
        public bool checkItemPatchExistance;

        public ForecastUser(string gmsvenid)
        {
            this.gmsvenid = gmsvenid;
            dataProvider = new UploadDataProvider();
            if (gmsvenid == "0")
            {
                tableName = "tbl_AllVendors";
                vendorDesc = "No Vendor";
                checkItemPatchExistance = false;
            }
            else
            {
                tableName = dataProvider.GetVendorTableName(this.gmsvenid);
                vendorDesc = dataProvider.GetVendorDesc(this.gmsvenid);
                checkItemPatchExistance = true;
            }
        }
    }
}

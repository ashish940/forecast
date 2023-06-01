namespace Forecast.E2ETests.Tests.UploadTesting.ItemPatch
{
    class NewItemInfo
    {
        public string columnName, fileColumnName, dataValue;
        public NewItemInfo(string columnName, string fileColumnName, string dataValue)
        {
            this.columnName = columnName;
            this.fileColumnName = fileColumnName;
            this.dataValue = dataValue;
        }
    }
}

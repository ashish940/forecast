namespace Forecast.E2ETests.Tests.UploadTesting.ItemPatch
{
    class OriginalDataValue
    {
        public string columnName, dataValue;
        public OriginalDataValue(string columnName, string dataValue)
        {
            this.columnName = columnName;
            this.dataValue = dataValue;
        }
    }
}

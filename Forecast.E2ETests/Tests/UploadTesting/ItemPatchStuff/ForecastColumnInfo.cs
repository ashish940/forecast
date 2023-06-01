namespace Forecast.E2ETests.Tests.UploadTesting.ItemPatch
{
    class ForecastColumnInfo
    {
        public string columnName, aggFunction, dataValue, dataCheckerHelper;

        public ForecastColumnInfo(string columnName, string aggFunction, string dataValue, string dataCheckerHelper)
        {
            this.columnName = columnName;
            this.aggFunction = aggFunction;
            this.dataValue = dataValue;
            this.dataCheckerHelper = dataCheckerHelper;
        }
    }
}

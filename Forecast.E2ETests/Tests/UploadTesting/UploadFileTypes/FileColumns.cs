namespace Forecast.E2ETests.Tests.UploadTesting.UploadFileTypes
{
    class FileColumns
    {
        public string fileColumnName, databaseColumnName, aggFunction;
        public FileColumns(string fileColumnName, string databaseColumnName, string aggFunction)
        {
            this.fileColumnName = fileColumnName;
            this.databaseColumnName = databaseColumnName;
            this.aggFunction = aggFunction;
        }

        public FileColumns(string fileColumnName, string databaseColumnName)
        {
            this.fileColumnName = fileColumnName;
            this.databaseColumnName = databaseColumnName;
        }
    }
}

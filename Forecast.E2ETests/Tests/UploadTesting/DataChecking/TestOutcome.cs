namespace Forecast.E2ETests.Global.UploadTesting
{
    class TestOutcome
    {
        public bool successful;
        public string errorMessage = "";

        public TestOutcome(bool success, string errorMessage)
        {
            if (!success)
            {
                this.errorMessage = errorMessage;
            }

            successful = success;
        }

        public TestOutcome(bool success)
        {

        }
    }
}

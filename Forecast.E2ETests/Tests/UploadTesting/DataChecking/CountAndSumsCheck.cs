using System.Collections.Generic;

namespace Forecast.E2ETests.Tests.UploadTesting.DataChecking
{
    class CountAndSumsCheck
    {
        public List<VariableValue> before = new List<VariableValue>();
        public List<VariableValue> after = new List<VariableValue>();
        public string vendorDesc = "";

        public CountAndSumsCheck(string vendorDesc, List<VariableValue> before)
        {
            this.vendorDesc = vendorDesc;
            this.before = before;
        }
    }
}

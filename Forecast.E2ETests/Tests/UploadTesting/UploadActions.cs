using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Forecast.E2ETests.Global
{
    public class UploadActions
    {
        public readonly IWebDriver webDriver;
        public TestHelpers helper;
        public ForecastWebPage actions;
        public WebDriverWait wait;

        public UploadActions(IWebDriver webDriver)
        {
            this.webDriver = webDriver;
            helper = new TestHelpers();
            actions = new ForecastWebPage(webDriver);
        }

        public void OpenUploadsModal(IWebDriver webDriver)
        {
            var uploadModalButtonId = "uploads-modal-trigger";
            actions.JsClickElementBySelector(webDriver, $"#{uploadModalButtonId}");

            try
            {
                helper.WaitForElement("uploadModal", webDriver);
            }
            catch (Exception)
            {
                helper.JsClickElementBySelector(webDriver, $"#{uploadModalButtonId}");
                helper.WaitForElement("uploadModal", webDriver);
            }
        }

        public void NavigateToExceptionsTab()
        {
            var uploadModalButtonId = "exceptions-tab-item";
            actions.JsClickElementBySelector(webDriver, $"#{uploadModalButtonId}");
            try
            {
                helper.WaitForElement("ipo_table_container", webDriver);
            }
            catch (Exception)
            {
                helper.JsClickElementBySelector(webDriver, $"#{uploadModalButtonId}");
                helper.WaitForElement("ipo_table_container", webDriver);
            }
        }
    }
}

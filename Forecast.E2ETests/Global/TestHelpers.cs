using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Forecast.E2ETests.Global
{
    public class TestHelpers
    {
        /// <summary>
        /// Waits the given amount of seconds.
        /// </summary>
        /// <param name="seconds">How many seconds to wait. Can be a decimal amount.</param>
        public static void Wait(double seconds) => Thread.Sleep((int)seconds * 1000);

        public IWebElement GetElementById(IWebDriver webDriver, string elementId) => webDriver.FindElement(By.Id(elementId));

        public void JsClickElementBySelector(IWebDriver webDriver, string selector)
        {
            var script = $@"
                if (typeof $ !== 'undefined') {"{"}
                    $('{selector}').click();
                {"}"} else {"{"}
                    var element = document.querySelector('{selector}');
                    if (element && typeof element['click'] === 'function') {"{"}
                        element.click();
                    {"}"}
                {"}"}";
            ExecuteJavascript(webDriver, script);
        }

        public string HandleAlert(IWebDriver driver, WebDriverWait wait)
        {
            var alertText = "";

            if (wait == null)
            {
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            }

            try
            {
                var alert = wait.Until(drv =>
                {
                    try
                    {
                        return drv.SwitchTo().Alert();
                    }
                    catch (NoAlertPresentException)
                    {
                        return null;
                    }
                });
                alertText = alert.Text;

                alert.Accept();

            }
            catch (Exception e)
            {
                if (!(e.Message == "Timed out after 5 seconds"))
                {
                    throw e;
                }
            }

            return alertText;
        }

        public void WaitForProcessingScreen(bool waitForItToClear)
        {
            if (waitForItToClear)
            {

            }
        }

        public void ExecuteJavascript(IWebDriver webDriver, string javascript) => (webDriver as IJavaScriptExecutor).ExecuteScript(javascript);

        public void WaitForElement(string elementName, IWebDriver webDriver)
        {
            var wait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 15));
            wait.Until(condition =>
            {
                var uploadsModalElement = GetElementById(webDriver, elementName);
                var displayStyle = uploadsModalElement.GetCssValue("display");
                return !displayStyle.Equals("none");
            });
        }
    }
}

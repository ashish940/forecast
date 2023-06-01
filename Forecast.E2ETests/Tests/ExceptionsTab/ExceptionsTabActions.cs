using System;
using System.Collections.Generic;
using Forecast.E2ETests.Global;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Forecast.E2ETests.Tests.ExceptionsTab
{
    class ExceptionsTabActions
    {
        public readonly IWebDriver webDriver;
        public TestHelpers helper;
        public ForecastWebPage actions;
        public Global.UploadTesting.UploadTest currentTest;

        public ExceptionsTabActions(IWebDriver webDriver, Global.UploadTesting.UploadTest currentTest)
        {
            this.webDriver = webDriver;
            helper = new TestHelpers();
            actions = new ForecastWebPage(webDriver);
            this.currentTest = currentTest;
        }

        //super hacky, but couldn't figure out a better way to do this.
        public void RemoveAllClaimsUsingSite()
        {
            try
            {
                NavigateToExceptionsTab();

                int claims = short.Parse(currentTest.dataProvider.GetNumberOfOverlappingClaims(currentTest.currentUser.gmsvenid));

                for (var i = 0; i < claims; i++)
                {
                    var updating = currentTest.webPage.GetElementById(currentTest.webDriver, "updating-background");
                    var action = new Actions(webDriver);
                    var listOfExceptionRows = new List<IWebElement>(webDriver.FindElements(By.ClassName("dataTables_scrollBody")));
                    var listOfRows = new List<IWebElement>(listOfExceptionRows[1].FindElements(By.TagName("tbody")));
                    var xlocation = listOfRows[0].Size.Width - 10;
                    var ylocation = listOfRows[0].Location.Y + 10;
                    action.MoveToElement(listOfRows[0], xlocation, 10);
                    action.Click();
                    action.Perform();

                    var removeClaimsButton = webDriver.FindElement(By.XPath("//*[@id=\"ipo_table_wrapper\"]/div[1]/button[2]"));
                    removeClaimsButton.Click();
                    while (!updating.Displayed)
                    {
                        currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);
                    }

                    while (updating.Displayed) { }

                    currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);

                    while (currentTest.webPage.IsElementVisible(currentTest.webPage.GetElementById(currentTest.webDriver, "updating-background"))) { }
                    currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);
                }
            }
            catch (Exception)
            {
                currentTest.actions.helper.HandleAlert(currentTest.webDriver, currentTest.actions.wait);
                if (short.Parse(currentTest.dataProvider.GetNumberOfOverlappingClaims(currentTest.currentUser.gmsvenid)) != 0)
                {
                    RemoveAllClaimsUsingSite();
                }
            }

            if (short.Parse(currentTest.dataProvider.GetNumberOfOverlappingClaims(currentTest.currentUser.gmsvenid)) == 0)
            {
                currentTest.dataChecker.lastActionString += currentTest.currentUser.vendorDesc + " rem with site. ";
                currentTest.ownership.Remove(currentTest.currentUser);
                currentTest.currentOwner = currentTest.ownership[0];
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

            while (currentTest.webPage.IsElementVisible(currentTest.webPage.GetElementById(currentTest.webDriver, "updating-background"))) { }
        }
    }
}

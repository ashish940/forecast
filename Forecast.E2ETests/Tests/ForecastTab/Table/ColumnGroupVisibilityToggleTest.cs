using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Global;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace Forecast.E2ETests.Tests.ForecastTab.Table.ColumnGroupVisibilityTests
{
    [TestFixture(typeof(ChromeDriver))]
    [TestFixture(typeof(FirefoxDriver))]
    [TestFixture(typeof(EdgeDriver))]
    [Parallelizable(ParallelScope.Self)]
    public class ColumnGroupVisibilityToggleTest
    {
        readonly Type webDriverType;
        IWebDriver webDriver;
        ForecastWebPage webPage;

        public ColumnGroupVisibilityToggleTest(Type webDriverType)
        {
            this.webDriverType = webDriverType;
        }

        public string TestCategory { get; set; }

        public TestContext TestContext { get; set; }

        [SetUp]
        public void TestSetup()
        {
            webDriver = DriverFactory.CreateIWebDriverInstance(webDriverType, TestContext.CurrentContext);
            webDriver.Manage().Window.Maximize();
            webPage = new ForecastWebPage(webDriver);
        }

        [TearDown]
        public void TestTeardown()
        {
            if (webDriver != null)
            {
                webDriver.Quit();
                webDriver.Dispose();
            }
        }

        [TestCase("Asp")]
        [TestCase("Comments")]
        [TestCase("Cost")]
        [TestCase("Forecast")]
        [TestCase("MarginPercent")]
        [TestCase("MarginDollar")]
        [TestCase("Mp")]
        [TestCase("PriceSensitivity")]
        [TestCase("ReceiptDollar")]
        [TestCase("ReceiptUnits")]
        [TestCase("RetailPrice")]
        [TestCase("SalesDollars")]
        [TestCase("SalesUnits")]
        [TestCase("SellThrough")]
        [TestCase("Turns")]
        public void ShouldShowAndHideColumnGroup(string columnGroupTogglePropertyName)
        {
            try
            {
                webPage.NavigateToForecastAndLogIn();

                webPage.OpenColumnGroupVisibilityMenu();

                var columnGroupToggleInfo = ColumnGroupToggle.GetColumnGroupByPropertyName(columnGroupTogglePropertyName);
                Assert.IsNotNull(columnGroupToggleInfo);

                webPage.SetColumnGroupVisibility(columnGroupToggleInfo, false);
                webPage.SetColumnGroupVisibility(columnGroupToggleInfo, true);

                // Make sure the column group is visible
                var columnGroup = columnGroupToggleInfo.ColumnGroups.First();
                var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                Assert.IsNotNull(columnGroupElement);

                // Make sure all columns are visible witin the column group
                var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                Assert.AreEqual(columnGroup.Columns.Count, columnGroupColumnElements.Count);
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldShowAndHideColumnGroup]");
                webPage.TakeScreenShot("ShouldShowAndHideColumnGroup");
                throw;
            }
        }

        [TestCase("Default", true)]
        [TestCase("HideAll", false)]
        [TestCase("ShowAll", true)]
        public void ShouldShowPreConfiguredColumnGroups(string columnGroupTogglePropertyName, bool shouldBeVisible)
        {
            try
            {
                webPage.NavigateToForecastAndLogIn();

                webPage.OpenColumnGroupVisibilityMenu();

                var columnGroupToggleInfo = ColumnGroupToggle.GetColumnGroupByPropertyName(columnGroupTogglePropertyName);
                Assert.IsNotNull(columnGroupToggleInfo);

                webPage.ClickColumnGroupVisibility(columnGroupToggleInfo, shouldBeVisible);

                // If no column groups are available for the selected column group toggle then make sure no columns remain visible
                if (columnGroupToggleInfo.ColumnGroups.Count == 0)
                {
                    var allGroupClasses = ColumnGroup.GetAllIColumnGroupPropertyValues().Select(columnGroup => columnGroup.ClassName);
                    var allGroups = webPage.FindTableHeadersByClassName(allGroupClasses);
                    Assert.AreEqual(0, allGroups.Count);
                }
                else
                {
                    // Make sure all column groups are visible
                    columnGroupToggleInfo.ColumnGroups.ForEach(columnGroup =>
                    {
                        var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                        Assert.IsNotNull(columnGroupElement);
                    });

                    // Make sure all columns are visible witin each column group
                    columnGroupToggleInfo.ColumnGroups.ForEach(columnGroup =>
                    {
                        var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                        Assert.AreEqual(columnGroup.Columns.Count, columnGroupColumnElements.Count);
                    });
                }
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldShowPreConfiguredColumnGroups]");
                webPage.TakeScreenShot("ShouldShowPreConfiguredColumnGroups");
                throw;
            }
        }

        [Test]
        public void ShouldShowAllColumnGroupsOneByOne()
        {
            try
            {
                var propertiesToExclude = new List<string>
                {
                    "DEFAULT",
                    "HIDE ALL",
                    "SHOW ALL"
                };

                webPage.NavigateToForecastAndLogIn();

                var columnGroupPropertyNames = ColumnGroupToggle.GetAllIColumnGroupTogglePropertyValues().Where(columnGroup => !propertiesToExclude.Contains(columnGroup.DisplayName)).ToList();

                webPage.OpenColumnGroupVisibilityMenu();

                webPage.ClickColumnGroupVisibility(ColumnGroupToggle.HideAll, false);

                // Set each column group to visible one-by-one
                columnGroupPropertyNames.ForEach(columnGroupToggle => webPage.ClickColumnGroupVisibility(columnGroupToggle, true));

                ColumnGroup.GetAllIColumnGroupPropertyValues().ToList().ForEach(columnGroup =>
                {
                    // Make sure all column groups are visible
                    var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                    Assert.IsNotNull(columnGroupElement);

                    // Make sure all columns are visible witin each column group
                    var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                    Assert.AreEqual(columnGroup.Columns.Count, columnGroupColumnElements.Count);
                });
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldShowAllColumnGroupsOneByOne]");
                webPage.TakeScreenShot("ShouldShowAllColumnGroupsOneByOne");
                throw;
            }
        }
    }
}

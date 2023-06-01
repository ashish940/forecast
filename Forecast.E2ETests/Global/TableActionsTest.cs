using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace Forecast.E2ETests.Global.TableActionsTests
{
    [TestFixture(typeof(ChromeDriver))]
    [TestFixture(typeof(FirefoxDriver))]
    [TestFixture(typeof(EdgeDriver))]
    [Parallelizable(ParallelScope.Self)]
    public class TableActionsPrallelTests
    {
        readonly Type webDriverType;
        IWebDriver webDriver;
        ForecastWebPage webPage;
        TableActions table;

        public TableActionsPrallelTests(Type webDriverType)
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
            table = new TableActions(webDriver);
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

        [TestCase("ShowAll", true)]
        [TestCase("HideAll", false)]
        public void ShouldClickColumnGroupVisibilityButton(string columnGroupTogglePropertyName, bool shouldBeVisible)
        {
            try
            {
                webPage.NavigateToForecastAndLogIn();

                table.OpenColumnGroupVisibilityMenu();

                var columnGroupToggleInfo = ColumnGroupToggle.GetColumnGroupByPropertyName(columnGroupTogglePropertyName);
                table.ClickColumnGroupVisibility(columnGroupToggleInfo, shouldBeVisible);

                var columnGroups = columnGroupToggleInfo.ColumnGroups.Count > 0
                    ? columnGroupToggleInfo.ColumnGroups
                    : ColumnGroup.GetAllIColumnGroupPropertyValues().ToList();

                columnGroups.ForEach(columnGroup =>
                {
                    var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                    if (shouldBeVisible)
                    {
                        Assert.IsNotNull(columnGroupElement);
                    }
                    else
                    {
                        Assert.IsNull(columnGroupElement);
                    }
                });

                columnGroups.ForEach(columnGroup =>
                {
                    var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                    var expectedColumnCount = shouldBeVisible ? columnGroup.Columns.Count : 0;
                    Assert.AreEqual(expectedColumnCount, columnGroupColumnElements.Count);
                });
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldClickColumnGroupAsVisibleAndInvisible]");
                webPage.TakeScreenShot("ShouldClickColumnGroupAsVisibleAndInvisible");
                throw;
            }
        }

        [TestCase("ShowAll")]
        [TestCase("Turns")]
        [TestCase("RetailPrice")]
        public void ShouldSetColumnGroupAsVisibleAndInvisible(string columnGroupTogglePropertyName)
        {
            var propsWithNoActiveClass = new List<string>
            {
                "Default",
                "HideAll",
                "ShowAll"
            };

            try
            {
                webPage.NavigateToForecastAndLogIn();

                table.OpenColumnGroupVisibilityMenu();

                var columnGroupToggleInfo = ColumnGroupToggle.GetColumnGroupByPropertyName(columnGroupTogglePropertyName);
                if (!propsWithNoActiveClass.Contains(columnGroupTogglePropertyName))
                {
                    table.SetColumnGroupVisibility(columnGroupToggleInfo, false);
                }

                table.SetColumnGroupVisibility(columnGroupToggleInfo, true);

                columnGroupToggleInfo.ColumnGroups.ForEach(columnGroup =>
                {
                    var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                    Assert.IsNotNull(columnGroupElement);
                });

                columnGroupToggleInfo.ColumnGroups.ForEach(columnGroup =>
                {
                    var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                    Assert.AreEqual(columnGroup.Columns.Count, columnGroupColumnElements.Count);
                });
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldSetColumnGroupAsVisibleAndInvisible]");
                webPage.TakeScreenShot("ShouldSetColumnGroupAsVisibleAndInvisible");
                throw;
            }
        }
    }

    [TestFixture(typeof(ChromeDriver))]
    [TestFixture(typeof(FirefoxDriver))]
    [TestFixture(typeof(EdgeDriver))]
    public class TableActionsSequentialTests
    {
        readonly Type webDriverType;
        IWebDriver webDriver;
        ForecastWebPage webPage;
        TableActions table;

        public TableActionsSequentialTests(Type webDriverType)
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
            table = new TableActions(webDriver);
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

        [TestCase("ShowAll", true)]
        [TestCase("HideAll", false)]
        public void ShouldClickColumnGroupVisibilityButton(string columnGroupTogglePropertyName, bool shouldBeVisible)
        {
            try
            {
                webPage.NavigateToForecastAndLogIn();

                table.OpenColumnGroupVisibilityMenu();

                var columnGroupToggleInfo = ColumnGroupToggle.GetColumnGroupByPropertyName(columnGroupTogglePropertyName);
                table.ClickColumnGroupVisibility(columnGroupToggleInfo, shouldBeVisible);

                var columnGroups = columnGroupToggleInfo.ColumnGroups.Count > 0
                    ? columnGroupToggleInfo.ColumnGroups
                    : ColumnGroup.GetAllIColumnGroupPropertyValues().ToList();

                columnGroups.ForEach(columnGroup =>
                {
                    var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                    if (shouldBeVisible)
                    {
                        Assert.IsNotNull(columnGroupElement);
                    }
                    else
                    {
                        Assert.IsNull(columnGroupElement);
                    }
                });

                columnGroups.ForEach(columnGroup =>
                {
                    var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                    var expectedColumnCount = shouldBeVisible ? columnGroup.Columns.Count : 0;
                    Assert.AreEqual(expectedColumnCount, columnGroupColumnElements.Count);
                });
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldClickColumnGroupAsVisibleAndInvisible]");
                webPage.TakeScreenShot("ShouldClickColumnGroupAsVisibleAndInvisible");
                throw;
            }
        }

        [TestCase("ShowAll")]
        [TestCase("Turns")]
        [TestCase("RetailPrice")]
        public void ShouldSetColumnGroupAsVisibleAndInvisible(string columnGroupTogglePropertyName)
        {
            var propsWithNoActiveClass = new List<string>
            {
                "Default",
                "HideAll",
                "ShowAll"
            };

            try
            {
                webPage.NavigateToForecastAndLogIn();

                table.OpenColumnGroupVisibilityMenu();

                var columnGroupToggleInfo = ColumnGroupToggle.GetColumnGroupByPropertyName(columnGroupTogglePropertyName);
                if (!propsWithNoActiveClass.Contains(columnGroupTogglePropertyName))
                {
                    table.SetColumnGroupVisibility(columnGroupToggleInfo, false);
                }

                table.SetColumnGroupVisibility(columnGroupToggleInfo, true);

                columnGroupToggleInfo.ColumnGroups.ForEach(columnGroup =>
                {
                    var columnGroupElement = webPage.FindTableHeaderById(columnGroup.IdName);
                    Assert.IsNotNull(columnGroupElement);
                });

                columnGroupToggleInfo.ColumnGroups.ForEach(columnGroup =>
                {
                    var columnGroupColumnElements = webPage.FindTableHeadersByClassName(columnGroup.ClassName);
                    Assert.AreEqual(columnGroup.Columns.Count, columnGroupColumnElements.Count);
                });
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldApplyTestBookmark]");
                webPage.TakeScreenShot("ShouldSetColumnGroupAsVisibleAndInvisible");
                throw;
            }
        }
    }
}

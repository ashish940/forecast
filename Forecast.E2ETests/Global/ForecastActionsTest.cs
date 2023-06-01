using System;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace Forecast.E2ETests.Global.ForecastActionsTests
{
    [TestFixture(typeof(ChromeDriver))]
    [TestFixture(typeof(FirefoxDriver))]
    [TestFixture(typeof(EdgeDriver))]
    [Parallelizable(ParallelScope.Self)]
    public class ForecastActionsPrallelTests
    {
        Type webDriverType;
        IWebDriver webDriver;
        ForecastWebPage webPage;
        TableActions table;

        public ForecastActionsPrallelTests(Type webDriverType)
        {
            this.webDriverType = webDriverType;
        }

        public string testCategory { get; set; }

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

        private void Wait(double seconds) => TestHelpers.Wait(seconds);
    }

    [TestFixture(typeof(ChromeDriver))]
    [TestFixture(typeof(FirefoxDriver))]
    [TestFixture(typeof(EdgeDriver))]
    public class ForecastActionsSequentialTests
    {
        Type webDriverType;
        IWebDriver webDriver;
        ForecastWebPage webPage;
        TableActions table;

        public ForecastActionsSequentialTests(Type webDriverType)
        {
            this.webDriverType = webDriverType;
        }

        public string testCategory { get; set; }

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

        private void Wait(double seconds) => TestHelpers.Wait(seconds);
    }
}

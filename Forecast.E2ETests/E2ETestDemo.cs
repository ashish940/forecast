using System;
using System.Configuration;
using System.IO;
using Forecast.E2ETests.Global;
using Forecast.E2ETests.Global.IO.CSV;
using Forecast.E2ETests.Global.IO.Serialization;
using Forecast.E2ETests.Global.Models;
using Forecast.E2ETests.Global.Models.Dynamic;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using static Forecast.E2ETests.Global.DriverFactory;


namespace Forecast.E2ETests
{
    [SetUpFixture]
    public class TestEnvironment
    {
        public static string TestRunDirectory { get; private set; }

        [OneTimeSetUp]
        public void TestDirectoryCreate()
        {
            var workingDirectory = TestContext.CurrentContext.WorkDirectory;
            var timeStampString = DateUtil.GetPathFriendlyTimeStamp();
            var newDirectoryName = $"{workingDirectory}\\E2ETestResults\\E2ETestRun_{timeStampString}";
            Directory.CreateDirectory(newDirectoryName);
            TestRunDirectory = newDirectoryName;
        }
    }

    /// <summary>
    /// 
    /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!NOTE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /// 
    /// This class was created just for a demo. It can be used for examples but nothing more. Some tests are
    /// written in a bit of a hacky way with little thought to maintainability so PLEASE DO NOT follow their
    /// design to the letter. The <see cref="ForecastWebPage"/> class however, is designed to contain actions
    /// that can be re-used by tests across the projects. Please add any website user actions to that class.
    /// Not sure yet but we might want to create different WebPage classes for different tabs such as the
    /// Exceptions tab. These things will need to be discussed.
    /// 
    /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!NOTE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /// 
    /// </summary>
    [TestFixture(typeof(ChromeDriver))]
    [TestFixture(typeof(FirefoxDriver))]
    [TestFixture(typeof(EdgeDriver))]
    [Parallelizable(ParallelScope.Self)]
    public class E2ETestsDemo
    {
        Type webDriverType;
        string url = ConfigurationManager.AppSettings["e2e_Url"];
        IWebDriver webDriver;
        ForecastWebPage webPage;

        public E2ETestsDemo(Type webDriverType)
        {
            this.webDriverType = webDriverType;
        }

        public string testCategory { get; set; }

        public TestContext TestContext { get; set; }

        [SetUp]
        public void TestSetup()
        {
            webDriver = CreateIWebDriverInstance(webDriverType, TestContext.CurrentContext);
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

        [TestCase("1")]
        public void ShouldApplyTestBookmark(string userNumber)
        {
            try
            {
                logIn(webDriver, userNumber);

                // Clear filter settings.
                webPage.ClickClearSettingsButton();

                webPage.WaitForAjax();
                webPage.WaitForPreLoader();

                wait(2);

                webPage.ApplyBookmark("E2ETestBookmark");

                webPage.WaitForAjax();

                webPage.WaitForUpdater();

                var rows = webPage.GetTableRows();

                Assert.IsTrue(rows.Count == 1);
                Assert.AreEqual(219402, rows[0].ItemID);
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldApplyTestBookmark]");
                webPage.TakeScreenShot("ShouldApplyTestBookmark");
                throw;
            }
        }

        [TestCase("1")]
        public void ShouldApplyItemWeekMMRotator(string userNumber)
        {
            try
            {
                logIn(webDriver, userNumber);

                // Clear filter settings.
                webPage.ClickClearSettingsButton();

                webPage.WaitForAjax();
                webPage.WaitForPreLoader();

                wait(1);

                webPage.ApplyRotatorColumns("Item", "Fiscal Week", "MM");

                webPage.WaitForAjax();
                webPage.WaitForUpdater();

                var tableRows = webPage.GetTableRows();
                Assert.IsNotNull(tableRows);
                Assert.IsTrue(tableRows.Count > 0);

                var firstTableRow = tableRows[0];

                Assert.AreNotEqual("-1", $"{firstTableRow.ItemID}");
                Assert.AreNotEqual("-1", $"{firstTableRow.FiscalWk}");
                Assert.AreNotEqual("-1", firstTableRow.MM);
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldApplyItemWeekMMRotator]");
                webPage.TakeScreenShot("ShouldApplyItemWeekMMRotator");
                throw;
            }
        }

        [TestCase("1")]
        public void ShouldExportTestBookmark(string userNumber)
        {
            try
            {
                logIn(webDriver, userNumber);

                // Clear filter settings.
                webPage.ClickClearSettingsButton();

                webPage.WaitForAjax();
                webPage.WaitForPreLoader();

                wait(1);

                webPage.ApplyBookmark("E2ETestBookmark");

                wait(1);

                webPage.WaitForAjax();
                webPage.WaitForUpdater();

                wait(4);

                var rows = webPage.GetTableRows();

                Assert.IsTrue(rows.Count == 1);
                Assert.AreEqual(219402, rows[0].ItemID);

                wait(1);

                webPage.ExportFilteredData(webDriver);

                wait(4);

                webPage.WaitForFileDownload(TestContext.CurrentContext);

                wait(4);

                var workingDirectory = $"{TestContext.CurrentContext.WorkDirectory}\\E2ETesting";

                var fileName = webPage.GetLastDownloadedFileName(webDriver);
                var filePath = $"{workingDirectory}\\{fileName}";

                var unzipDirectory = $"{workingDirectory}\\{Path.GetFileNameWithoutExtension(fileName)}";
                FileZip.ExtractTo(filePath, unzipDirectory);
                var files = Directory.GetFiles(unzipDirectory);
                Assert.IsTrue(files.Length == 1);

                var exportData = new CSVReader().GetExpandoList(files[0]);
                exportData[0].TryGetValue("Item", out string value);
                Assert.AreEqual("219402", value);

                Directory.Delete(unzipDirectory, true);
                File.Delete(filePath);
            }
            catch (Exception)
            {
                var browserName = webPage.GetBrowserName(webDriver);
                var browserVersion = webPage.GetBrowserVersion(webDriver);
                var browserInfo = $"{browserName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ShouldExportTestBookmark]");
                webPage.TakeScreenShot($"ShouldApplyItemWeekMMRotator");
                throw;
            }
        }

        private void logIn(IWebDriver webDriver, string userNumber)
        {
            var username = ConfigurationManager.AppSettings[$"e2e_User{userNumber}_UserName"];
            var password = ConfigurationManager.AppSettings[$"e2e_User{userNumber}_Password"];

            Assert.IsNotNull(webDriver);

            // Go to Forecast web site
            webDriver.Navigate().GoToUrl(url);

            // Log the user in
            webPage.LogIn(webDriver, username, password);

            wait(2);
        }

        private void wait(double seconds) => TestHelpers.Wait(seconds);
    }
}

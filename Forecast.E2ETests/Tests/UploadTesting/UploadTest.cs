using System;
using System.Collections.Generic;
using System.Configuration;
using Forecast.E2ETests.Global;
using Forecast.E2ETests.Global.UploadTesting;
using Forecast.E2ETests.Global.UploadTesting.UploadFileTypes;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using static Forecast.E2ETests.Global.DriverFactory;

namespace Forecast.E2ETests.UploadTests
{
    [TestFixture(typeof(FirefoxDriver))]
    public class UploadTest
    {
        public TestHelpers helper;
        readonly Type webDriverType;
        readonly string url = ConfigurationManager.AppSettings["e2e_Url"];
        IWebDriver webDriver;
        ForecastWebPage actions;
        readonly UploadDataCommands commands;
        Global.UploadTesting.UploadTest currentTest;

        public UploadTest(Type webDriverType)
        {
            this.webDriverType = webDriverType;
            commands = new UploadDataCommands();
        }

        public string TestCategory { get; set; }

        public TestContext TestContext { get; set; }

        [SetUp]
        public void TestSetup()
        {
            webDriver = CreateIWebDriverInstance(webDriverType, TestContext.CurrentContext);
            webDriver.Manage().Window.Maximize();
            actions = new ForecastWebPage(webDriver);
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

        [TestCase("IOUHistoryRemoveWithFile")]
        public void IOUHistoryRemoveWithFile(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("3"), new ForecastUser("57"), new ForecastUser("41") }
                , commands.IOUHistoryQuery(commands.testCasesIOUHistory, "tbl_Altmans"), true, true);

            logIn(currentTest.users[0]);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].IOU = new IOU(currentTest, "vendor2_IOU", true, true);
            currentTest.users[0].Remove = new IOU(currentTest, "vendor1_Remove", false, true);
            currentTest.users[2].IOU = new IOU(currentTest, "vendor3_IOU", true, true);
            currentTest.users[1].Remove = new IOU(currentTest, "vendor2_Remove", false, true);

            currentTest.users[0].Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));

            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[2]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[0]);
            currentTest.currentUser.Remove.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));

            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.currentUser.Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));

        }

        [TestCase("IOUNoHistoryRemoveWithFile")]
        public void IOUNoHistoryRemoveWithFile(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67"), new ForecastUser("57"), new ForecastUser("41") }
                , commands.IOUNoHistoryQuery(commands.testCasesIOU), true, false);

            currentTest.users[0].IOU = new IOU(currentTest, "vendor1_IOU", true, true);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].IOU = new IOU(currentTest, "vendor2_IOU", true, true);
            currentTest.users[0].Remove = new IOU(currentTest, "vendor1_Remove", false, true);
            currentTest.users[2].IOU = new IOU(currentTest, "vendor3_IOU", true, true);
            currentTest.users[1].Remove = new IOU(currentTest, "vendor2_Remove", false, true);

            logIn(currentTest.users[0]);

            currentTest.currentUser.IOU.UploadFile();
            currentTest.currentUser.Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.currentUser.IOU.UploadFile();
            currentTest.webPage.LogOut();
            logIn(currentTest.users[2]);
            currentTest.currentUser.IOU.UploadFile();
            currentTest.webPage.LogOut();
            logIn(currentTest.users[0]);
            currentTest.currentUser.Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.currentUser.Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
        }

        [TestCase("IOUHistoryRemoveUsingSite")]
        public void IOUHistoryRemoveUsingSite(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("3"), new ForecastUser("57"), new ForecastUser("41") }
                , commands.IOUHistoryQuery(commands.testCasesIOUHistory, "tbl_Altmans"), true, true);
            logIn(currentTest.users[0]);

            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].IOU = new IOU(currentTest, "vendor2_IOU", true, true);
            currentTest.users[2].IOU = new IOU(currentTest, "vendor3_IOU", true, true);

            currentTest.currentUser.Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[2]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[0]);
            currentTest.exceptionTabActions.RemoveAllClaimsUsingSite();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));

            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.exceptionTabActions.RemoveAllClaimsUsingSite();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));

        }

        [TestCase("IOUNoHistoryRemoveUsingSite")]
        public void IOUNoHistoryRemoveUsingSite(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67"), new ForecastUser("57"), new ForecastUser("41") }
                , commands.IOUNoHistoryQuery(commands.testCasesIOU), true, false);
            logIn(currentTest.users[0]);

            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].IOU = new IOU(currentTest, "vendor2_IOU", true, true);
            currentTest.users[2].IOU = new IOU(currentTest, "vendor3_IOU", true, true);

            currentTest.users[0].IOU = new IOU(currentTest, "vendor1_IOU", true, true);

            currentTest.currentUser.IOU.UploadFile();
            currentTest.currentUser.Forecast.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[2]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            logIn(currentTest.users[0]);
            currentTest.exceptionTabActions.RemoveAllClaimsUsingSite();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));

            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.exceptionTabActions.RemoveAllClaimsUsingSite();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
        }

        [TestCase("IOUHistoryNoVendor")]
        public void IOUHistoryNoVendor(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("3"), new ForecastUser("57"), new ForecastUser("41") }
                , commands.IOUHistoryQuery(commands.testCasesIOUHistory, "tbl_Altmans"), true, true);

            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].IOU = new IOU(currentTest, "vendor2_IOU", true, true);
            currentTest.users[0].Remove = new IOU(currentTest, "vendor1_Remove", false, true);
            currentTest.users[2].IOU = new IOU(currentTest, "vendor3_IOU", true, true);
            currentTest.users[1].Remove = new IOU(currentTest, "vendor2_Remove", false, true);

            logIn(currentTest.users[0]);

            currentTest.currentUser.Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
            currentTest.currentUser.Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[1]);
            currentTest.currentUser.IOU.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.currentUser.Remove.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[2]);
            currentTest.currentUser.IOU.UploadFile();

            currentTest.webPage.LogOut();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
        }

        [TestCase("IOUNoHistoryNoVendor")]
        public void IOUNoHistoryNoVendor(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67"), new ForecastUser("57"), new ForecastUser("41") }
                , commands.IOUNoHistoryQuery(commands.testCasesIOU), true, false);

            currentTest.users[0].IOU = new IOU(currentTest, "vendor1_IOU", true, true);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].IOU = new IOU(currentTest, "vendor2_IOU", true, true);
            currentTest.users[0].Remove = new IOU(currentTest, "vendor1_Remove", false, true);
            currentTest.users[2].IOU = new IOU(currentTest, "vendor3_IOU", true, true);
            currentTest.users[1].Remove = new IOU(currentTest, "vendor2_Remove", false, true);

            logIn(currentTest.users[0]);

            currentTest.users[0].IOU.UploadFile();
            currentTest.users[0].Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
            currentTest.users[0].Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[1]);
            currentTest.users[1].IOU.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.users[1].Remove.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[2]);
            currentTest.users[2].IOU.UploadFile();

            currentTest.webPage.LogOut();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, true));
        }

        [TestCase("NIURemoveWithFile")]
        public void NIURemoveWithFile(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67"), new ForecastUser("41"), new ForecastUser("3") }
                , commands.NIUItemPatchQuery(commands.testCasesNIU), false, false);

            currentTest.users[0].NIU = new NIU(currentTest, "vendor1_NIU", true, false, currentTest.users[0].vendorDesc);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].NIU = new NIU(currentTest, "vendor2_NIU", true, true, currentTest.users[1].vendorDesc);
            currentTest.users[2].NIU = new NIU(currentTest, "vendor3_NIU", true, true, currentTest.users[2].vendorDesc);
            currentTest.users[0].Remove = new IOU(currentTest, "vendor1_Remove", false, true);
            currentTest.users[1].Remove = new IOU(currentTest, "vendor2_Remove", false, true);

            logIn(currentTest.users[0]);
            currentTest.users[0].NIU.UploadFile();
            currentTest.users[0].Forecast.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, false));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[1]);
            currentTest.users[1].NIU.UploadFile();
            currentTest.webPage.LogOut();
            logIn(currentTest.users[2]);
            currentTest.users[2].NIU.UploadFile();
            currentTest.webPage.LogOut();

            logIn(currentTest.users[0]);
            currentTest.users[0].Remove.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.users[1].Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
        }

        [TestCase("NIURemoveUsingSite")]
        public void NIURemoveUsingSite(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67"), new ForecastUser("41"), new ForecastUser("57") }
                , commands.NIUItemPatchQuery(commands.testCasesNIU), false, false);

            var frozen = currentTest.dataProvider.GetToolState();

            currentTest.users[0].NIU = new NIU(currentTest, "vendor1_NIU", true, false, currentTest.users[0].vendorDesc);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].NIU = new NIU(currentTest, "vendor2_NIU", true, true, currentTest.users[1].vendorDesc);
            currentTest.users[2].NIU = new NIU(currentTest, "vendor3_NIU", true, true, currentTest.users[2].vendorDesc);

            logIn(currentTest.users[0]);
            currentTest.users[0].NIU.UploadFile();
            currentTest.users[0].Forecast.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, false));
            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);
            currentTest.users[1].NIU.UploadFile();
            currentTest.webPage.LogOut();
            logIn(currentTest.users[2]);
            currentTest.users[2].NIU.UploadFile();
            currentTest.webPage.LogOut();
            logIn(currentTest.users[0]);

            currentTest.exceptionTabActions.RemoveAllClaimsUsingSite();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
            currentTest.webPage.LogOut();
            logIn(currentTest.users[1]);

            currentTest.exceptionTabActions.RemoveAllClaimsUsingSite();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
        }

        [TestCase("NIUNoVendor")]
        public void NIUNoVendor(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67"), new ForecastUser("41"), new ForecastUser("3") }
                , commands.NIUItemPatchQuery(commands.testCasesNIU), false, false);

            var frozen = currentTest.dataProvider.GetToolState();

            currentTest.users[0].NIU = new NIU(currentTest, "vendor1_NIU", true, false, currentTest.users[0].vendorDesc);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_forecast", 1);
            currentTest.users[1].NIU = new NIU(currentTest, "vendor2_NIU", true, true, currentTest.users[1].vendorDesc);
            currentTest.users[2].NIU = new NIU(currentTest, "vendor3_NIU", true, true, currentTest.users[2].vendorDesc);
            currentTest.users[0].Remove = new IOU(currentTest, "vendor1_Remove", false, true);
            currentTest.users[1].Remove = new IOU(currentTest, "vendor2_Remove", false, true);

            logIn(currentTest.users[0]);
            currentTest.users[0].NIU.UploadFile();
            currentTest.users[0].Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, false));
            currentTest.users[0].Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[1]);
            currentTest.users[1].NIU.UploadFile();
            currentTest.users[1].Remove.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
            currentTest.webPage.LogOut();

            logIn(currentTest.users[2]);
            currentTest.users[2].NIU.UploadFile();
            currentTest.webPage.LogOut();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, false, false));
        }

        [TestCase("UploadAllPatchesNIU")]
        public void UploadAllPatchesNIU(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67") }
                , commands.NIUItemPatchQueryAllPatches(), false, false);
            logIn(currentTest.users[0]);
            currentTest.users[0].NIU = new NIU(currentTest, "vendor1_Allpatches_NIU", true, true, currentTest.users[0].vendorDesc);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_allpatches_Forecast", 1);
            currentTest.users[0].NIU.UploadFile();
            currentTest.users[0].Forecast.UploadFile();

            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, false));
        }

        [TestCase("UploadAllPatchesIOU")]
        public void UploadAllPatchesIOU(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("67") }
                , commands.IOUAllPatchesQuery(), true, false);
            logIn(currentTest.users[0]);
            currentTest.users[0].IOU = new IOU(currentTest, "vendor1_Allpatches_IOU", true, true);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_allpatches_Forecast", 1);
            currentTest.users[0].IOU.UploadFile();
            currentTest.users[0].Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
        }

        [TestCase("ForecastUploadAllUploadTextCases")]
        public void ForecastUploadAllUploadTextCases(string testCase)
        {
            currentTest = new Global.UploadTesting.UploadTest(webDriver, testCase, new List<ForecastUser>() { new ForecastUser("3") }
                , commands.ForecastAllUploadCases("tbl_altmans"), true, true);
            logIn(currentTest.users[0]);
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_Forecast", 100);
            currentTest.users[0].Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
            currentTest.users[0].Forecast = new ForecastUpload(currentTest, "vendor1_Forecast", -200);
            currentTest.users[0].Forecast.UploadFile();
            Assert.IsTrue(currentTest.dataChecker.CheckItemPatchData(currentTest, true, true));
        }

        private void logIn(ForecastUser user)
        {
            // webPage.ChangeGMSVenID(user.gmsvenid);
            currentTest.currentUser = user;
            var username = ConfigurationManager.AppSettings[$"e2e_User{user.gmsvenid}_UserName"];
            var password = ConfigurationManager.AppSettings[$"e2e_User{user.gmsvenid}_Password"];

            Assert.IsNotNull(webDriver);

            // Go to Forecast web site
            webDriver.Navigate().GoToUrl(url);

            // Log the user in
            actions.LogIn(webDriver, username, password);

            TestHelpers.Wait(2);
        }
    }
}

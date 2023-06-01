using Forecast.Controllers;
using Forecast.UnitTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace Forecast.UnitTests
{
    /// <summary>
    /// Summary description for UnitTest2
    /// </summary>
    [TestClass]
    public class DownloadUnitTest
    {

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get => testContextInstance;
            set => testContextInstance = value;
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void DownloadInvalidFileRedirectsToStorageFolderTest()
        {
            //set up path variables
            var serverUtility = new ServerUtilityMock();
            var appSettings = new AppSettingsMock();
            var path = serverUtility.currentDirectory();
            string invalidFilePath = path + "Web.config";

            appSettings.Set("FilePath", "Storage\\");

            var controller = new HomeController(serverUtility, appSettings);
            var pm = new Mock<IPrincipal>();
            var httpcm = new Mock<HttpContextBase>();
            httpcm.Setup(x => x.User).Returns(pm.Object);
            var cc = new ControllerContext { HttpContext = httpcm.Object };
            cc.Controller = controller;
            controller.ControllerContext = cc;

            try
            {
                var fullFilePath = controller.DownloadFile(invalidFilePath);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "No file at the location: " + invalidFilePath);
            }
        }

        [TestMethod]
        public void DownloadValidFileInStorageFolderTest()
        {
            //set up path variables
            var serverUtility = new ServerUtilityMock();
            var appSettings = new AppSettingsMock();
            var path = serverUtility.MapPath("Storage");
            string validFilePath = path + "\\test.csv";
            appSettings.Set("FilePath", "Storage\\");

            //set up file in Storage folder
            string fileName = validFilePath;
            FileInfo fileInfo = new FileInfo(fileName);

            FileStream fileStream = null;
            try
            {
                fileStream = File.Create(Path.Combine(fileName));
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Dispose();
            }

            var controller = new HomeController(serverUtility, appSettings);
            var pm = new Mock<IPrincipal>();
            var httpcm = new Mock<HttpContextBase>();
            httpcm.Setup(x => x.User).Returns(pm.Object);
            var cc = new ControllerContext { HttpContext = httpcm.Object };
            cc.Controller = controller;
            controller.ControllerContext = cc;

            //call the method 
            var file = controller.DownloadFile(validFilePath);
            Assert.AreEqual("application/octet-stream", file.ContentType);
            Assert.AreEqual("test.csv", file.FileDownloadName);
            //as long as no exceptions are thrown test will pass
            Assert.IsTrue(true);
        }
    }
}

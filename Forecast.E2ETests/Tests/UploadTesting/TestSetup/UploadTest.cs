using System;
using System.Collections.Generic;
using Forecast.E2ETests.Tests.ExceptionsTab;
using OpenQA.Selenium;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class UploadTest
    {
        public IWebDriver webDriver;
        public List<ForecastUser> users;
        public ForecastUser currentUser;
        public ForecastUser currentOwner;
        public DataChecker dataChecker;
        public string testName;
        public UploadDataProvider dataProvider;
        public List<ItemPatch> listOfItemPatches;
        public UploadActions actions;
        public ForecastWebPage webPage;
        public ExceptionsTabActions exceptionTabActions;
        public List<string> listOfUserVendorNames = new List<string>();
        public bool IsIOU;
        public string testCaseName;

        public List<string> testCases;

        public List<ForecastUser> ownership = new List<ForecastUser>();

        public UploadTest(IWebDriver webDriver, string testCase, List<ForecastUser> users, string query, bool IOU, bool existingItemPatches)
        {
            IsIOU = IOU;
            testCaseName = testCase;
            this.webDriver = webDriver;
            dataProvider = new UploadDataProvider();
            webPage = new ForecastWebPage(webDriver);
            dataChecker = new DataChecker(webDriver, this);
            this.users = users;
            var noVendor = new ForecastUser("0");
            this.users.Add(noVendor);

            if (existingItemPatches)
            {
                ownership.Add(users[0]);
                currentOwner = ownership[0];
            }
            else
            {
                currentOwner = noVendor;
            }

            listOfItemPatches = dataProvider.GetListOfItemPatches(query, IOU);

            if (listOfItemPatches.Count == 0)
            {
                throw new Exception("ListOfItemPatches is empty. Check the query to see if something is wrong");
            }

            actions = new UploadActions(webDriver);
            exceptionTabActions = new ExceptionsTabActions(webDriver, this);
            foreach (var user in users)
            {
                listOfUserVendorNames.Add(user.vendorDesc);
            }
        }
        public UploadTest(IWebDriver webDriver, ForecastUser vendor1)
        {
            this.webDriver = webDriver;
            dataProvider = new UploadDataProvider();
            dataChecker = new DataChecker(webDriver, this);
        }
    }
}

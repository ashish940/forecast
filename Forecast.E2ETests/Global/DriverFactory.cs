using System;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;

namespace Forecast.E2ETests.Global
{
    public static class DriverFactory
    {

        public static IWebDriver CreateIWebDriverInstance(Type type, TestContext testContext)
        {
            var downloadDirectory = $"{testContext.WorkDirectory}\\E2ETesting";

            if (type == typeof(FirefoxDriver))
            {
                var profile = new FirefoxProfile();
                profile.SetPreference("browser.download.folderList", 2);
                profile.SetPreference("browser.download.dir", downloadDirectory);
                profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "text/csv");
                profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/zip");
                profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/json");
                profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/octet-stream");

                var options = new FirefoxOptions
                {
                    Profile = profile
                };

                var firefoxDriver = new FirefoxDriver(options);

                return firefoxDriver;
            }
            else if (type == typeof(ChromeDriver))
            {
                var options = new ChromeOptions();
                options.AddUserProfilePreference("intl.accept_languages", "nl");
                options.AddUserProfilePreference("disable-popup-blocking", true);
                options.AddUserProfilePreference("download.default_directory", downloadDirectory);
                options.AddUserProfilePreference("download.prompt_for_download", false);

                var chromeDriver = new ChromeDriver(options);

                return chromeDriver;
            }
            else if (type == typeof(EdgeDriver))
            {
                var options = new EdgeOptions();
                options.AddUserProfilePreference("intl.accept_languages", "nl");
                options.AddUserProfilePreference("disable-popup-blocking", true);
                options.AddUserProfilePreference("download.default_directory", downloadDirectory);
                options.AddUserProfilePreference("download.prompt_for_download", false);

                var edgeDriver = new EdgeDriver(options);

                return edgeDriver;
            }
            else
            {
                return null;
            }
        }
    }
}

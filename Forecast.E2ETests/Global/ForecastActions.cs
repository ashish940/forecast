using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Forecast.E2ETests.Global.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Forecast.E2ETests.Global
{
    public class ForecastWebPage
    {
        // VARIABLES

        public readonly string ROTATOR_ACCEPT_BUTTON_NAME = "ACCEPT";
        public readonly string SiteUrl = ConfigurationManager.AppSettings["e2e_Url"];
        public readonly IWebDriver webDriver;
        private readonly TableActions table;
        private TestHelpers helper = new TestHelpers();

        public ForecastWebPage(IWebDriver webDriver)
        {
            this.webDriver = webDriver;
            table = new TableActions(webDriver);
        }

        // METHODS

        public void LogOut()
        {
            IWebElement userButton = GetElementById(webDriver, "user-dropdown-trigger");
            userButton.Click();
            //ClickButton("user-dropdown-trigger");
            while (!IsElementVisible(GetElementById(webDriver, "lnk_logout")))
            {
                wait(1);
            }

            IWebElement logoutButton = GetElementById(webDriver, "lnk_logout");
            logoutButton.Click();

            // ClickButton("link_logout");

            while (!IsElementVisible(GetElementById(webDriver, "logout_yes")))
            {
                wait(1);
            }

            IWebElement yes = GetElementById(webDriver, "logout_yes");
            yes.Click();
        }

        public void ClickButton(string buttonName) => JsClickElementBySelector(webDriver, $"#{buttonName}");

        public bool IsElementVisible(IWebElement element)
        {
            try
            {
                return element.Displayed && element.Enabled;
            }
            catch (Exception e)
            {
                if (e.Message == "Object reference not set to an instance of an object.")
                {
                    return false;
                }
                else
                {
                    throw e;
                }
            }
        }

        public void ApplyBookmark(string bookmarkName)
        {
            OpenBookmarksModal(webDriver);
            this.wait(1);
            OpenBookmarksDropdown(webDriver);
            this.wait(1);

            var dropdownId = "select2-bookmarkList-results";
            var wait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 15));
            wait.Until(condition => condition.PageSource.Contains($"id=\"{dropdownId}\""));
            WaitForAjax();

            WaitForDropDownNotToContainText(dropdownId, "Searching");
            this.wait(1);

            var listOfBookmarks = GetListOfLinkElements(webDriver, $"#{dropdownId}");
            var e2eBookmarkLink = listOfBookmarks.Find(we => we.GetAttribute("innerText").Equals(bookmarkName));

            if (e2eBookmarkLink == null)
            {
                var bookmarkNames = string.Join(", ", listOfBookmarks.Select(e => e.GetAttribute("innerText")));
                var browserName = GetBrowserName(webDriver);
                var browserVersion = GetBrowserVersion(webDriver);
                var browserInfo = $"{bookmarkName}_{browserVersion}";
                Console.WriteLine($"[TEST DEBUG] [{browserInfo}] [ApplyBookmark] bookmark names {bookmarkNames}");

                var namesString = string.Join("", listOfBookmarks.Select(e => e.GetAttribute("innerText")));
                if (namesString.Length == 0 || namesString.ToLower().Contains("searching"))
                {
                    if (namesString.Length == 0)
                    {
                        OpenBookmarksDropdown(webDriver);
                    }
                    this.wait(1);
                    WaitForAjax();
                    listOfBookmarks = GetListOfLinkElements(webDriver, $"#{dropdownId}");
                    e2eBookmarkLink = listOfBookmarks.Find(we => we.Text.ToLower().Equals(bookmarkName.ToLower()));
                }
            }

            Assert.IsNotNull(e2eBookmarkLink);
            ClickElement(webDriver, e2eBookmarkLink);
            this.wait(1);
            var bookmarksDropdownElement = GetElementById(webDriver, "select2-bookmarkList-container");
            Assert.IsNotNull(bookmarksDropdownElement);
            Assert.AreEqual(bookmarkName, bookmarksDropdownElement.Text);

            ClickElementById(webDriver, "btn_LoadBookmark");
            this.wait(1);
        }

        public void ApplyRotatorColumns(params string[] rotatorColumnNames)
        {
            OpenRotatorMenu(webDriver);
            wait(1);

            var rotatorColumnNameLinks = GetListOfLinkElements(webDriver, "#rotator-dropdown");
            Assert.IsTrue(rotatorColumnNameLinks.Count > 0);

            void setCheckbox(IWebElement rotatorColumnLink, bool selected)
            {
                IWebElement checkboxElement = null;

                try
                {
                    checkboxElement = rotatorColumnLink.FindElement(By.CssSelector("input[type=\"checkbox\"]"));
                }
                catch (Exception) { }

                if (checkboxElement != null)
                {
                    var checkBoxId = checkboxElement.GetAttribute("id");
                    var js = string.Format(@"
                        var checkbox = document.querySelector('#{0}');
                        return checkbox.checked;
                        ", checkBoxId, !selected ? "true" : "false");
                    var checkedStatus = ExecuteJavascript<bool>(webDriver, js);
                    if (checkedStatus == !selected)
                    {
                        var jsCheckboxClick = string.Format(@"
                            var checkbox = document.querySelector('#{0}');
                            checkbox.click();
                            return checkbox.checked;
                            ", checkBoxId, selected ? "true" : "false");
                        var isSelected = ExecuteJavascript<bool>(webDriver, jsCheckboxClick);
                        wait(1);
                    }
                }
            }

            string getLinkText(IWebElement webElement)
            {
                try
                {
                    var anchorElement = webElement.FindElement(By.TagName("a"));
                    if (anchorElement.Text.Length == 0 && webElement.Text.Length > 0)
                    {
                        return webElement.Text;
                    }

                    return anchorElement.Text;
                }
                catch (Exception) { }

                return webElement.Text;
            }

            foreach (var rotatorColumnLink in rotatorColumnNameLinks)
            {
                setCheckbox(rotatorColumnLink, false);
            }

            foreach (var columnName in rotatorColumnNames)
            {
                var linkElement = rotatorColumnNameLinks.Find(rc => getLinkText(rc).Equals(columnName));
                if (linkElement == null)
                {
                    var textList = string.Join(", ", rotatorColumnNameLinks.Select(getLinkText).ToList());
                    Console.WriteLine($"[TEST DEBUG] [ApplyRotatorColumns] [Select checkbox loop] {textList}");
                }
                Assert.IsNotNull(linkElement);

                setCheckbox(linkElement, true);
                wait(.5);
            }

            var acceptButtonElement = rotatorColumnNameLinks.Find(rc => getLinkText(rc).Equals(ROTATOR_ACCEPT_BUTTON_NAME));
            Assert.IsNotNull(acceptButtonElement);
            ClickElement(webDriver, acceptButtonElement);
        }

        public void ClickClearSettingsButton()
        {
            ClickElementById(webDriver, "btn_ClearBookmark");
            wait(2);
        }

        /// <summary>
        /// Clicks on a given column group button. If there are no column groups within the provided <see cref="ColumnGroupToggle"/>
        /// then all column groups will be used from the <see cref="ColumnGroup.GetAllIColumnGroupPropertyValues"/> function.
        /// </summary>
        /// <param name="columnGroupInfo">A <see cref="IColumnGroupToggle"/> object representing the column group button to click.</param>
        /// <param name="shouldBeVisible">True if the <see cref="IColumn"/>s within all the <see cref="IColumnGroup"/>s should be
        /// visible after the button click. False if not.</param>
        public void ClickColumnGroupVisibility(IColumnGroupToggle columnGroupInfo, bool shouldBeVisible) => table.ClickColumnGroupVisibility(columnGroupInfo, shouldBeVisible);

        public void ClickElement(IWebDriver webDriver, IWebElement element)
        {
            Actions clickAction = new Actions(webDriver);
            clickAction.MoveToElement(element).Click().Perform();
        }

        public void ClickElementById(IWebDriver webDriver, string elementId)
        {
            try
            {
                var element = webDriver.FindElement(By.Id(elementId));
                Assert.IsNotNull(element);
                ClickElement(webDriver, element);
            }
            catch (Exception)
            {
                JsClickElementBySelector(webDriver, $"#{elementId}");
            }
        }

        public T ExecuteJavascript<T>(IWebDriver webDriver, string javascript) => (T)(webDriver as IJavaScriptExecutor).ExecuteScript(javascript);

        public void ExecuteJavascript(IWebDriver webDriver, string javascript) => (webDriver as IJavaScriptExecutor).ExecuteScript(javascript);

        public void ExportFilteredData(IWebDriver webDriver)
        {
            OpenExportsModal(webDriver);
            wait(1);

            ClickElementById(webDriver, "btn_ExportFull");
            wait(1);

            WaitForAjax();
            WaitForPreLoader();
        }

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="id">The <see cref="string"/> ID name for the header.</param>
        /// <returns>An instance of a <see cref="IWebElement"/> if the header is found. Null if it's not found.</returns>
        public IWebElement FindTableHeaderById(string id) => table.FindTableHeaderById(id);

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="id">The <see cref="string"/> ID name for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersById(string id) => table.FindTableHeadersById(id);

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="id">The <see cref="string"/> ID name for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersById(IEnumerable<string> ids) => table.FindTableHeadersById(ids);

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="className">The <see cref="string"/> class name for the header.</param>
        /// <returns>An instance of a <see cref="IWebElement"/> if the header is found. Null if it's not found.</returns>
        public IWebElement FindTableHeaderByClassName(string className) => table.FindTableHeaderByClassName(className);

        /// <summary>
        /// Find column or group headers based on their HTML element class attribute.
        /// </summary>
        /// <param name="className">The <see cref="string"/> class name(s) for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersByClassName(string className) => table.FindTableHeadersByClassName(className);

        /// <summary>
        /// Find column or group headers based on their HTML element class attribute.
        /// </summary>
        /// <param name="classNames">The <see cref="IEnumerable{T}"/> of <see cref="string"/> class name(s) for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersByClassName(IEnumerable<string> classNames) => table.FindTableHeadersByClassName(classNames);

        public string GetBrowserName(IWebDriver webDriver)
        {
            var capabilities = ((IHasCapabilities)webDriver).Capabilities;
            var browserName = ((string)capabilities.GetCapability("browserName")).Replace(" ", "_");
            return browserName;
        }

        public string GetBrowserVersion(IWebDriver webDriver)
        {
            var capabilities = ((IHasCapabilities)webDriver).Capabilities;
            var browserVersion = (string)capabilities.GetCapability("version");
            if (string.IsNullOrEmpty(browserVersion))
            {
                browserVersion = (string)capabilities.GetCapability("browserVersion");
            }
            return $"{browserVersion}";
        }

        public IWebElement GetElementByCssSelector(IWebDriver webDriver, string selector) => webDriver.FindElement(By.CssSelector(selector));

        public IWebElement GetElementById(IWebDriver webDriver, string elementId)
        {
            return webDriver.FindElement(By.Id(elementId));
        }

        public string GetLastDownloadedFileName(IWebDriver webDriver)
        {
            try
            {
                var filePath = ExecuteJavascript<string>(webDriver, "return ForecastDebugger.getLastDownloadedFiles('ExportReportFullDownload');");
                var fileName = Path.GetFileName(filePath);
                return fileName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public List<IWebElement> GetListOfLinkElements(IWebDriver webDriver, string cssSelector)
        {
            var dropdown = webDriver.FindElement(By.CssSelector(cssSelector));
            Assert.IsNotNull(dropdown);
            var links = dropdown.FindElements(By.TagName("li"));
            Assert.IsTrue(links.Count > 0);

            var linkElementList = new List<IWebElement>();
            var linksEnumerator = links.GetEnumerator();
            while (linksEnumerator.MoveNext())
            {
                linkElementList.Add(linksEnumerator.Current);
            }

            return linkElementList;
        }

        public int GetTableRowCount(IWebDriver webDriver) => (int)(webDriver as IJavaScriptExecutor).ExecuteScript("return DTable.rows().data().length;");

        public List<ForecastTableRow> GetTableRows()
        {
            var rows = (IReadOnlyCollection<object>)(webDriver as IJavaScriptExecutor).ExecuteScript("return $.makeArray(DTable.data());");
            var readOnlyForecastTableRows = JsonConvert.DeserializeObject<List<ForecastTableRow>>(JsonConvert.SerializeObject(rows));

            var forecastTableRows = new List<ForecastTableRow>();

            var rowsEnumerator = readOnlyForecastTableRows.GetEnumerator();

            while (rowsEnumerator.MoveNext())
            {
                forecastTableRows.Add(rowsEnumerator.Current);
            }

            return forecastTableRows;
        }

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

        /// <summary>
        /// Used for loging a user into the site.
        /// </summary>
        /// <param name="webDriver">The <see cref="IWebDriver"/> to use.</param>
        /// <param name="username">The user username.</param>
        /// <param name="password">The user password.</param>
        public void LogIn(IWebDriver webDriver, string username, string password)
        {
            try
            {
                // Create a wait object that will wait for things to render up to 60 seconds
                var pageLoadWait = new WebDriverWait(webDriver, new TimeSpan(0, 4, 0));

                // Wait until the login element has rendered.
                var loginElement = pageLoadWait.Until(a => a.FindElement(By.CssSelector("#login_row")));

                var usernameElement = loginElement.FindElement(By.Id("Username"));
                Assert.IsNotNull(usernameElement);

                var passwordElement = loginElement.FindElement(By.Name("Password"));
                Assert.IsNotNull(passwordElement);

                var loginSubmitButton = loginElement.FindElement(By.CssSelector("#submit-login-row > i > input[type=\"submit\"]"));
                Assert.IsNotNull(loginSubmitButton);

                usernameElement.SendKeys(username);
                passwordElement.SendKeys(password);
                loginSubmitButton.Click();

                pageLoadWait.Until(condition => condition.PageSource.Contains("id=\"ForecastTable_wrapper\""));

                WaitForAjax();

                WaitForPreLoader();

                var bodyElement = pageLoadWait.Until(a => a.FindElement(By.Id("ForecastTable_wrapper")));
                Assert.IsNotNull(bodyElement);
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("timed out after 60 seconds"))
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Navigate to the Forecast site and log in.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void NavigateToForecastAndLogIn(string username = null, string password = null)
        {
            username = username ?? ConfigurationManager.AppSettings[$"e2e_User1_UserName"];
            password = password ?? ConfigurationManager.AppSettings[$"e2e_User1_Password"];

            Assert.IsNotNull(webDriver);

            // Go to Forecast web site
            webDriver.Navigate().GoToUrl(SiteUrl);

            // Log the user in
            LogIn(webDriver, username, password);

            wait(2);
        }

        public void OpenBookmarksDropdown(IWebDriver webDriver)
        {
            var bookmarksDropdownId = "select2-bookmarkList-container";
            var dropdownId = "select2-bookmarkList-results";

            void waitForBookmarksDropdownToOpen()
            {

                var wait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 15));
                wait.Until(condition =>
                {
                    var isDropdownVisible = condition.PageSource.Contains($"id=\"{bookmarksDropdownId}\"");
                    return isDropdownVisible;
                });
            }

            SetSelect2OpenState("bookmarkList", true);
            wait(1);
            WaitForAjax();

            WaitForDropDownNotToContainText(dropdownId, "Searching");
            wait(1);

            try
            {
                waitForBookmarksDropdownToOpen();
            }
            catch (Exception)
            {
                SetSelect2OpenState("bookmarkList", true);
                wait(1);
                WaitForAjax();
                WaitForDropDownNotToContainText(dropdownId, "Searching");
                wait(1);
                waitForBookmarksDropdownToOpen();
            }

            WaitForAjax();
        }

        public void OpenBookmarksModal(IWebDriver webDriver)
        {
            void waitForBookmarksModalToOpen()
            {
                var wait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 15));
                wait.Until(condition =>
                {
                    var bookmarkModalElement = GetElementById(webDriver, "bookmarkModal"); ;
                    var displayStyle = bookmarkModalElement.GetCssValue("display");
                    return !displayStyle.Equals("none");
                });
            }

            var bookmarksModalButtonId = "bookmarks-modal-trigger";
            JsClickElementBySelector(webDriver, $"#{bookmarksModalButtonId}");

            try
            {
                waitForBookmarksModalToOpen();
            }
            catch (Exception)
            {
                JsClickElementBySelector(webDriver, $"#{bookmarksModalButtonId}");
                waitForBookmarksModalToOpen();
            }
        }

        /// <summary>
        /// Open the table Show/Hide column groups menu.
        /// </summary>
        public void OpenColumnGroupVisibilityMenu() => table.OpenColumnGroupVisibilityMenu();

        public void OpenExportsModal(IWebDriver webDriver)
        {
            void waitForExportsModalToOpen()
            {
                var wait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 15));
                wait.Until(condition =>
                {
                    var exportskModalElement = GetElementById(webDriver, "downloadModal"); ;
                    var displayStyle = exportskModalElement.GetCssValue("display");
                    return !displayStyle.Equals("none");
                });
            }

            var exportsModalButtonId = "downloads-modal-trigger";
            ClickElementById(webDriver, exportsModalButtonId);
            try
            {
                waitForExportsModalToOpen();
            }
            catch (Exception)
            {
                JsClickElementBySelector(webDriver, $"#{exportsModalButtonId}");
                waitForExportsModalToOpen();
            }
        }

        public void OpenRotatorMenu(IWebDriver webDriver)
        {
            void waitForRotatorMenuToOpen()
            {
                var wait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 15));
                wait.Until(condition =>
                {
                    var rotatorMenuElement = GetElementById(webDriver, "rotator-dropdown"); ;
                    var displayStyle = rotatorMenuElement.GetCssValue("display");
                    return !displayStyle.Equals("none");
                });
            }

            JsClickElementBySelector(webDriver, "#rotate-btn");

            try
            {
                waitForRotatorMenuToOpen();
            }
            catch (Exception)
            {
                JsClickElementBySelector(webDriver, "#rotate-btn");
                waitForRotatorMenuToOpen();
            }
        }

        /// <summary>
        /// Set a cetain column group as visible or invisible.
        /// </summary>
        /// <param name="columnGroupInfo">A <see cref="IColumnGroupToggle"/> object. See the <see cref="ColumnGroupToggle"/> object for
        /// constructing an instance.</param>
        /// <param name="visible">True if you want to set it as visible. False if you want to set it as invisible.</param>
        public void SetColumnGroupVisibility(IColumnGroupToggle columnGroupInfo, bool visible) => table.SetColumnGroupVisibility(columnGroupInfo, visible);

        public void SetSelect2OpenState(string id, bool open)
        {
            var status = open ? "open" : "close";
            var script = $"$('#{id}').select2('{status}');";
            ExecuteJavascript(webDriver, script);
        }

        public void SortColumnById(string columnId) => ClickElementById(webDriver, columnId);

        public string TakeScreenShot(string testName)
        {
            var screenShotTaker = (ITakesScreenshot)webDriver;
            var screenShot = screenShotTaker.GetScreenshot();
            var timeStampString = DateUtil.GetPathFriendlyTimeStamp();
            var browserName = GetBrowserName(webDriver);
            var browserVersion = GetBrowserVersion(webDriver);
            var filePath = $"{TestEnvironment.TestRunDirectory}\\{browserName}_{browserVersion}_{testName}_{timeStampString}.PNG";
            screenShot.SaveAsFile(filePath, ScreenshotImageFormat.Png);
            return filePath;
        }

        public void WaitForAjax()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true) // Handle timeout somewhere
            {
                var ajaxIsComplete = false;

                try
                {
                    ajaxIsComplete = (bool)(webDriver as IJavaScriptExecutor).ExecuteScript("return jQuery.active == 0");
                }
                catch (Exception) { }

                var elapsedSeconds = stopWatch.ElapsedMilliseconds / 1000;

                if (ajaxIsComplete || elapsedSeconds < 15)
                {
                    break;
                }

                Thread.Sleep(100);
            }
        }

        public void WaitForDropDownNotToContainText(string dropdownId, string text, double ellapsedSeconds = 30)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (true && (stopWatch.ElapsedMilliseconds / 1000) < ellapsedSeconds)
            {
                var links = GetListOfLinkElements(webDriver, $"#{dropdownId}");
                var link = links.Find(we =>
                {
                    if (we.Enabled)
                    {
                        return we.GetAttribute("innerText").Contains(text);
                    }

                    return false;
                });

                if (link == null)
                {
                    break;
                }

                wait(.1);
            }
        }

        public void WaitForFileDownload(TestContext testContext)
        {
            var filePath = string.Empty;

            while (true)
            {
                filePath = ExecuteJavascript<string>(webDriver, "return ForecastDebugger.getLastDownloadedFiles('ExportReportFullDownload');");
                if (filePath.Length > 0)
                {
                    break;
                }
            }

            var fileName = Path.GetFileName(filePath);
            var fullPath = $"{testContext.WorkDirectory}\\E2ETesting\\{fileName}";

            while (true)
            {
                var isDownloading = !File.Exists(fullPath);
                if (!isDownloading)
                {
                    break;
                }

                Thread.Sleep(100);
            }
        }

        public void WaitForPreLoader(int seconds = 60)
        {
            var preloaderClass = ".preloader-background";
            WaitForLoader(webDriver, preloaderClass, seconds);
        }

        public void WaitForUpdater(int seconds = 60)
        {
            var updaterId = "#updating-background";
            WaitForLoader(webDriver, updaterId, seconds);
        }

        public void WaitForLoader(IWebDriver webDriver, string cssSelector, int seconds)
        {
            var stopWatch = new Stopwatch();

            bool getIsUpdating()
            {
                try
                {
                    var updaterElement = GetElementByCssSelector(webDriver, cssSelector);
                    var updaterDisplayState = updaterElement.GetCssValue("display");
                    var updating = !updaterDisplayState.Equals("none");
                    return updating;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            stopWatch.Start();

            while (getIsUpdating() && (stopWatch.ElapsedMilliseconds / 1000) < seconds)
            {
                wait(.1);
            }

            stopWatch.Stop();
        }

        private void wait(double seconds) => TestHelpers.Wait(seconds);
    }
}

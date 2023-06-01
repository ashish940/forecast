using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Forecast.Models;
using Forecast.Data;
using System.Web.Security;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Collections.Concurrent;
using System.Configuration;
using System.Web.Routing;

namespace Forecast.Controllers
{
    public class HomeController : Controller
    {
        private IAppSettings appSettings;
        private IServerUtilities server;
      
        public HomeController() { }

        public HomeController(IServerUtilities server, IAppSettings appSettings)
        {
            this.server = server;
            this.appSettings = appSettings;
        }

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            server = server ?? new ServerUtilities(Server);
            appSettings = appSettings ?? new AppSettings();
        }

        [Authorize]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Index(string username)
        {
            // Sometimes browsers will attempt to redirect with
            // an authorization cookie only. A username param in the
            // url is required for the GET functions, so if a user hits
            // the index page without a username param, just redirect them
            // to login.
            if (Request.QueryString["username"] == null)
                return View("Login");

            // There is a scenario where a user, knowing another username, 
            // could simply pass the username into the URL as a parameter and
            // get to the index after being initially authorized under their own
            // username and password. This prevents that by checking the username
            // against the name with which they were initially authorized. We want 
            // to log this activity. 4/22/2016 JM
            string logInUser = this.HttpContext.User.Identity.Name.ToString();
            if (logInUser != username)
            {
                // TODO: Notify support
                return View("Login");
            }

            ViewBag.IsLogin = false;
            ViewBag.IsProduction = ConfigurationManager.AppSettings.Get("Production");

            var data = new DataProvider();
            var user = new User();

            // Get details about the user
            var userDetail = data.GetUserDetails(username).ToList();

            // Get a GUID into a ViewBag to use for cache-busting the JS and CSS
            // files
            string version = Guid.NewGuid().ToString();
            ViewBag.Version = version;

            //Temp GMSVenID passing.  
            user.GMSVenID = userDetail.FirstOrDefault().GMSVenID;
            int gmsvenid = ViewBag.GMSVenID = user.GMSVenID;

            //Username
            //string username = "acastillo";
            user.Username = ViewBag.Username = username;
            user.NTName = userDetail.FirstOrDefault().NTName;

            //VendorGroup for determining if user is Vendor, MD, MM
            user.VendorGroup = userDetail.FirstOrDefault().VendorGroup;
            string vendorgroup = ViewBag.VendorGroup = user.VendorGroup;

            //This assigns the tablename for the provided vendor
            var tableName = data.GetTableName(gmsvenid, vendorgroup, username);
            user.TableName = ViewBag.TableName = tableName;
           
            // Put sensitive user info into an encrypted cookie for later use
            // when routing web requests.
            HttpCookie DLCookie = new HttpCookie("DL_Forecast_0"); // This is the object we will use
            DLCookie.Domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName; // Cookie is specific to this domain
            DLCookie.Expires = DateTime.Now.AddHours(24); // Cookie expires in 24 hours
            DLCookie.HttpOnly = true; // Do not allow server side scripts to access this cookie
            DLCookie.Shareable = true; // Allow the cookie to particpate in output caching (a few of the tables leverage this)

            // Add the encrypted values to the cookie
            Response.Cookies["DL_Forecast_0"]["Username"] = DataProvider.Protect(username, "id");
            Response.Cookies["DL_Forecast_0"]["GMSVenID"] = DataProvider.Protect(gmsvenid.ToString(), "id");
            Response.Cookies["DL_Forecast_0"]["TableName"] = DataProvider.Protect(tableName, "id");
            Response.Cookies["DL_Forecast_0"]["VendorGroup"] = DataProvider.Protect(vendorgroup, "id");

            ViewBag.Expires = GetRemainingCookieTime(this.HttpContext);

            var userInfo = GetUserInfo();
            ViewBag.IsAdmin = IsAdmin();

            return View(user);
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            ViewBag.IsLogin = true;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Login(User u, string returnUrl)
        {
            // Holds the username
            System.String username;

            // Assume the user has not passed validation
            var valid_pass = false;

            // Run if no model errors have been added to ModelState
            if (this.ModelState.IsValid)
            {
                // New instance of QVWeb Class (db communicator)
                var service = new DataProvider();

                // Format the username
                username = u.Username.ToLower();
                username = username.Replace(@"gms\", "");

                // Check validity of username and password
                if (Membership.ValidateUser(username, u.Password))
                {
                    valid_pass = true;
                    var a = service.IsUser(username, u.Password);
                    if (service.IsUser(username, u.Password))
                    {
                        // Create an additional DL_Forecast cookie so an expiration date can be kept track of.
                        // Browser does not return a valid expires date in a response so the solution is to track it yourself.
                        HttpCookie DLExpiresCookie = new HttpCookie("DL_Forecast_1"); // Increment normal DLCookie naming convention by 1 so it's not easily readable in the browser.
                        DLExpiresCookie.Domain = ".demandlink.com"; // Cookie is specific to this domain
                        DLExpiresCookie.Expires = DateTime.Now.AddDays(1); // Cookie expires in 24 hours   
                        Response.Cookies.Add(DLExpiresCookie);
                        // Omits a key from the cookie so no information can be gathered from the browser.
                        Response.Cookies["DL_Forecast_1"].Value = DataProvider.Protect(DLExpiresCookie.Expires.ToString(), "id");

                        FormsAuthentication.SetAuthCookie(username, true);
                        //set true to false
                        HttpCookie aCookie = FormsAuthentication.GetAuthCookie(username, true);
                        aCookie.Domain = ".demandlink.com"; // set domain to shared subdomain
                        aCookie.Expires = System.DateTime.Now.AddDays(1);
                        
                        Response.Cookies.Add(aCookie);
                        
                        return RedirectToAction("Index", new { UserName = username });

                    }
                }
            }
            // Valid pass & username but unauthorized user
            if (valid_pass)
            {
                ViewBag.Message = "This user does not currently have access to the Forecast Tool. Please contact Kathryn Cram at kcram@demandlink.com.";
            }
            // No valid pass or username
            else
            {
                ViewBag.Message = "Invalid User Name and/or Password";
            }
            // Otherwise send to the login page.
            return View("Login");
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            return View("Login");
        }

        //Bookmark section
        public JsonResult CreateBookmark(int gmsvenid, string username, string bookmarkName, string state)
        {
            var data = new DataProvider();
            data.CreateBookmark(gmsvenid, username, bookmarkName, state);
            return Json(new { }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> CreateDlEvent(HttpPostedFileBase file)
        {
            // We can't send a json object with a form so we receive it as a string and deserialize it here.
            var evnt = Request.Form.Get("DlEvent");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var dlEvent = serializer.Deserialize<DlEvent>(evnt);
            dlEvent.LastEdit = DateTime.Now.ToString("MM/dd/yyyy");

            if (IsAdmin())
            {
                var driveService = DlGoogleDriveService.GetDriveServiceWithFile();

                if (file != null && file.ContentLength > 0)
                {
                    dlEvent.FileId = await DlGoogleDriveService.CreateFile(driveService, file);
                }

                new DataProvider().CreateDlEvent(dlEvent);

                return Json(new { response = "ok" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateNotification(Notification notification)
        {
            notification.LastEdit = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (IsAdmin())
            {
                new DataProvider().CreateNotification(notification);
                return Json(new { response = "ok" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeleteBookmark(int gmsvenid, string username, string bookmarkName)
        {
            var data = new DataProvider();
            data.DeleteBookmark(gmsvenid, username, bookmarkName);
            return Json(new { }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteDlEvent(DlEvent dlEvent)
        {
            if (!string.IsNullOrEmpty(dlEvent.FileId))
            {
                var driveService = DlGoogleDriveService.GetDriveServiceWithFile();

                DlGoogleDriveService.DeleteFile(driveService, dlEvent.FileId);
            }

            if (IsAdmin())
            {
                new DataProvider().DeleteDlEvent(dlEvent.EventId);
                return Json(new { response = "Ok", JsonRequestBehavior.AllowGet });
            }

            return Json(new { response = "Error", JsonRequestBehavior.AllowGet });
        }

        [HttpPost]
        public JsonResult DeleteTutorial(int id)
        {
            if (IsAdmin())
            {
                new DataProvider().DeleteTutorial(id);
                return Json(new { response = "Ok", JsonRequestBehavior.AllowGet });
            }

            return Json(new { response = "Error", JsonRequestBehavior.AllowGet });
        }

        [HttpPost]
        public JsonResult DeleteNotification(int id)
        {
            if (IsAdmin())
            {
                new DataProvider().DeleteNotification(id);
                return Json(new { response = "Ok", JsonRequestBehavior.AllowGet });
            }

            return Json(new { response = "Error", JsonRequestBehavior.AllowGet });
        }

        [HttpPost]
        public Stream DownloadDlEventFile(string fileId)
        {
            var service = DlGoogleDriveService.GetDriveServiceWithFile();
            return DlGoogleDriveService.DownloadFile(service, fileId);
        }

        public JsonResult GetAdminTempList(int id)
        {
            if (IsAdmin())
            {
                var results = new DataProvider().GetAdminTempList(id);
                return Json(results, JsonRequestBehavior.AllowGet);
            }
            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBookmark(int gmsvenid, string username, string bookmarkName)
        {
            var data = new DataProvider();
            var results = data.GetBookmark(gmsvenid, username, bookmarkName);
            return Json(new { data = results, }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBookmarkList(int gmsvenid, string username, string vendorGroup, string tableName)
        {
            var data = new DataProvider();

            //The following three variables are necessary to create a Forecast (Lite) bookmark
            //Forecast (Lite) is a bookmark given to everyone that 
            //displays the Forecast Comparison column group
            var forecastViewName = "Forecast (Lite)";

            //Check if user has the "Forecast (Lite)" bookmark. If the user doesn't have the bookmark
            //then transfer it to them.
            if (!data.GetIsBookmark(username, gmsvenid, forecastViewName))
            {
                /**
                 * This pattern recognizes the gmsVenId in the bookmark state string. It will find this chunk
                 * of string "gmsvenid":"id here" and replace it with the gmsvenid string provided in the 
                 * gmsVenIdReplacement variable below.
                 **/
                var gmsVenIdPattern = $"(\"gmsvenid\":\"([0-9]*)\",)";
                var gmsVenIdReplacement = $"\"gmsvenid\":\"{gmsvenid}\",";

                /**
                * This pattern recognizes the table name in the bookmark state string. It will find this chunck 
                * of string "tablename":"table_name_here" and replace it with the table name string provided in the 
                * tableNameReplacement variable below. This pattern will recocnize a table name that starts with 
                * three letters such as "tbl" and then an underscore "_" after that there may be repetitions
                * of unlimited letters with an optional underscore. This pattern may repeat, for example: tbl_this_is_a_valid_table_name_ .
                * This is not a valid table name "tbl_this_table_name__" or "tb_this__table_name_"
                **/
                var tableNamePattern = $"(\"tableName\":\"([a-zA-Z]{{3}}_([a-zA-Z]+_{{0,1}})*)\",)";
                var tableNameReplacement = $"\"tableName\":\"{tableName}\",";

                //Get the Forecast (Lite) bookmark template from the database
                var forecastViewBookmark = data.GetBookmark(0, "all", "Forecast (Lite)");

                //Replace the gmsvenid with the user gmsvenid
                forecastViewBookmark.State = Regex.Replace(forecastViewBookmark.State, gmsVenIdPattern, gmsVenIdReplacement);

                //Replace the table name with the table the user can access
                forecastViewBookmark.State = Regex.Replace(forecastViewBookmark.State, tableNamePattern, tableNameReplacement);

                //Create the Forecast (Lite) bookmark with the user gmsvenit and table name
                data.CreateBookmark(gmsvenid, username, forecastViewName, forecastViewBookmark.State);
            }

            var results = data.GetBookmarkList(gmsvenid, username);
            return Json(new { results = results }, JsonRequestBehavior.AllowGet);
        }
        // End Bookmarks

        [HttpPost]
        [OutputCache(Duration = 0, VaryByParam = "*", Location = System.Web.UI.OutputCacheLocation.Client, NoStore = true)]
        public JsonResult GetDollarSummaryTotals(DTParameterModel param)
        {
            var data = new DataProvider();
            var table = data.GetDollarSummaryTable(param);
            var result = from c in table
                         select new[]
                         {
                             c.ForecastDef.ToString()
                             , c.Actual.ToString()
                             , c.FC.ToString()
                             , c.Var.ToString()
                         };

            return Json(new
            {
                draw = param.Draw,
                data = result,
            }, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetUpdatedDates(DTParameterModel param)
        {
            var data = new DataProvider();
            var table = data.GetUpdatedDates(param);
            var result = from c in table
                         select new[]
                         {
                             Convert.ToDateTime(c.DateMin).ToString("M/dd/yyyy"),
                             Convert.ToDateTime(c.DateMax).ToString("M/dd/yyyy")
                         };

            return Json(new
            {
                draw = param.Draw,
                data = result,
            }, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        [OutputCache(Duration = 60, VaryByParam = "*", Location = System.Web.UI.OutputCacheLocation.Server, NoStore = false)]
        public async Task<JsonResult> GetForecastTable(DTParameterModel param)
        {
            var data = new DataProvider();
            ConcurrentDictionary<string, object> dataDict = new ConcurrentDictionary<string, object>(Environment.ProcessorCount, 4);
            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(() =>
            {
                var tableData = data.GetForecastTable(param);
                dataDict.TryAdd("table", tableData);
            }));

            if (param.IsRotating || param.IsFiltering)
            {
                tasks.Add(Task.Run(() =>
                {
                    var totalRecs = data.GetForecastTableCount(param);
                    dataDict.TryAdd("totalRecords", totalRecs);
                }));
                tasks.Add(Task.Run(() =>
                {
                    var filteredRecs = data.GetForecastTableCount(param, true);
                    dataDict.TryAdd("filteredRecords", filteredRecs);
                }));
            }

            if (param.IsFiltering)
            {
                tasks.Add(Task.Run(() =>
                {
                    var tableSums = data.GetSums(param);
                    dataDict.TryAdd("sums", tableSums);
                }));
            }
            await Task.WhenAll(tasks);

            dataDict.TryGetValue("sums", out var sums);
            dataDict.TryGetValue("totalRecords", out var totalRecords);
            dataDict.TryGetValue("filteredRecords", out var filteredRecords);

            var json = Json(new
            {
                draw = param.Draw,
                recordsTotal = totalRecords,
                recordsFiltered = filteredRecords,
                data = dataDict["table"],
                sums = sums
            }, JsonRequestBehavior.AllowGet);
            json.MaxJsonLength = int.MaxValue;
            return json;
        }

        [HttpPost]
        [OutputCache(Duration = 5, VaryByParam = "none", Location = System.Web.UI.OutputCacheLocation.Client, NoStore = true)]
        public async Task<JsonResult> GetOverlappingClaimsTable(DTParameterModel param)
        {
            var data = new DataProvider();
            ConcurrentDictionary<string, object> dataDict = new ConcurrentDictionary<string, object>(Environment.ProcessorCount, 4);
            List<Task> tasks = new List<Task>
            {
                Task.Run(() =>
                {
                    var tableData = data.GetOverlappingClaimsTable(param, false);
                    dataDict.TryAdd("table", tableData);
                })
            };
            
            tasks.Add(Task.Run(() =>
            {
                var totalRecs = data.GetOverlappingItemPatchTableCount(param);
                dataDict.TryAdd("totalRecords", totalRecs);
                dataDict.TryAdd("filteredRecords", totalRecs);
            }));
            await Task.WhenAll(tasks);

            dataDict.TryGetValue("totalRecords", out var totalRecords);
            dataDict.TryGetValue("filteredRecords", out var filteredRecords);

            var json = Json(new
            {
                draw = param.Draw,
                recordsTotal = totalRecords,
                recordsFiltered = filteredRecords,
                data = dataDict["table"]
            }, JsonRequestBehavior.AllowGet);
            json.MaxJsonLength = int.MaxValue;
            return json;
        }

        [HttpPost]
        public JsonResult RunExport(DTParameterModel param)
        {
            var appSettingsExportsPath = appSettings.Get("FilePath");
            var dirPath = server.MapPath(appSettingsExportsPath);
            var choice = param.ExportChoice;

            var Export = new Exports.Exports();

            string fileName = Export.RunExport(param, dirPath);

            var fileToExport = new
            {
                fileName = fileName,
            };

            return Json(fileToExport);
        }

        [HttpGet]
        public FileResult DownloadFile(string fileName)
        {
            try
            {
                //remove everything but the fileName itself 
                //Example if passed C:\Users\HannahMadland\source\repos\forecast\Forecast\Storage.txt as fileName 
                //will set cleanedFileName = Storage.txt
                var cleanedFileName = Path.GetFileName(fileName);
                var invalidFileChars = Path.GetInvalidFileNameChars();
                var invalidPathChars = Path.GetInvalidPathChars();
                var isFileValid = cleanedFileName.IndexOfAny(invalidFileChars) == -1 && cleanedFileName.IndexOfAny(invalidPathChars) == -1;
                if (!isFileValid)
                {
                    throw new Exception($"File name {fileName} is not a valid file name format.");
                }
                var appSettingsExportsPath = appSettings.Get("FilePath");
                var exportsDirectory = server.MapPath(appSettingsExportsPath);
                var fullFilePath = Path.Combine(exportsDirectory, cleanedFileName);

                if (System.IO.File.Exists(fullFilePath))
                {
                    var fs = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
                    return File(fs, System.Net.Mime.MediaTypeNames.Application.Octet, Path.GetFileName(fullFilePath));
                }
                else
                {
                    throw new FileNotFoundException("No file at the location: " + fileName);
                }
            }
            catch (OutOfMemoryException e)
            {
                throw new OutOfMemoryException();
            }
            catch (Exception e)
            {
                throw;
            }

        }

        public JsonResult GetFilterData(DTParameterModel param, string type, string search = "")
        {
            var data = new DataProvider();
            var results = data.GetFilterData(param, type, search);
            return Json(new { results = results }, JsonRequestBehavior.AllowGet);
        }
        
        //Returns "ItemID - Item Description" for bulk filter
        public async Task<JsonResult> GetItemDesc(string []item, string table)
        {
            var data = new DataProvider();
            var fullItem = await Task.FromResult(data.GetItemDesc(item, table));
            return Json(new { fullItem }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetDlEvents(int eventId = -1)
        {
            try
            {
                var userInfo = GetUserInfo();
                var eventObj = new DataProvider().GetDlEvents(eventId);
                return Json(eventObj);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost]
        public JsonResult GetDlEventFile(string fileId)
        {
            if (!string.IsNullOrEmpty(fileId))
            {
                var dataService = DlGoogleDriveService.GetDriveServiceWithFile();
                var def = DlGoogleDriveService.GetFile(dataService, fileId);
                var dlEventFile = new GoogleDriveFile
                {
                    Id = def.Id,
                    Name = def.Name,
                    FileExtension = def.FileExtension,
                    OriginalFileName = def.OriginalFilename,
                    WebViewLink = def.WebViewLink
                };

                return Json(dlEventFile, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        // Helper function that grabs the cookie expiration date and converts it to milliseconds remaining for the sake of logging an idle user out of the site.
        private int GetRemainingCookieTime(HttpContextBase context)
        {
            try
            {
                if (context.Request.Cookies["DL_Forecast_1"] == null)
                {
                    // Create an additional DL_Forecast cookie so an expiration date can be kept track of.
                    // Browser does not return a valid expires date in a response so the solution is to track it yourself.
                    HttpCookie DLExpiresCookie = new HttpCookie("DL_Forecast_1"); // Increment normal DLCookie naming convention by 1 so it's not easily readable in the browser.
                    DLExpiresCookie.Domain = ".demandlink.com"; // Cookie is specific to this domain
                    DLExpiresCookie.Expires = DateTime.Now.AddDays(1); // Cookie expires in 24 hours   
                    context.Response.Cookies.Add(DLExpiresCookie);
                    // Omits a key from the cookie so no information can be gathered from the browser.
                    context.Response.Cookies["DL_Forecast_1"].Value = DataProvider.Protect(DLExpiresCookie.Expires.ToString(), "id");
                }

                DateTime cookieExpirDate = System.Convert.ToDateTime(DataProvider.Unprotect(context.Request.Cookies["DL_Forecast_1"].Value, "id"));
                TimeSpan timeDiff = cookieExpirDate - DateTime.Now;
                int remainingMs = (int)timeDiff.TotalMilliseconds;
                if (remainingMs > 0)
                {
                    return remainingMs;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public JsonResult GetTutorialGroups()
        {
            if (IsAdmin())
            {
                var tutGroups = new DataProvider().GetTutorialGroups();
                return Json(tutGroups, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "Not Admin" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetTutorials()
        {
            var tutorials = new DataProvider().GetTutorials();
            return Json(tutorials, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateTutorial(Tutorial tutorial)
        {
            tutorial.LastEdit = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (IsAdmin())
            {
                new DataProvider().CreateTutorial(tutorial);
                return Json(new { response = "ok" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetMarginDollarSummaryTotals(DTParameterModel param)
        {
            var data = new DataProvider();
            var table = data.GetMarginDollarSummaryTable(param);
            var result = from c in table
                         select new[]
                         {
                             c.Actual.ToString(), c.FC.ToString(), c.Var.ToString()
                         };

            return Json(new
            {
                draw = param.Draw,
                data = result,
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetMarginPercentSummaryTotals(DTParameterModel param)
        {
            var data = new DataProvider();
            var table = data.GetMarginPercentSummaryTable(param);
            var result = from c in table
                         select new[]
                         {
                             c.Actual.ToString(), c.FC.ToString(), c.Var.ToString()
                         };

            return Json(new
            {
                draw = param.Draw,
                data = result,
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetNotifications()
        {
            var userInfo = GetUserInfo();
            var notifications = new DataProvider().GetNotifications(userInfo);
            return Json(notifications, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetNotificationsList()
        {
            var userInfo = GetUserInfo();
            if (IsAdmin())
            {
                var notifications = new DataProvider().GetNotifications(userInfo, true);
                return Json(notifications, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { response = "Not Admin!!!" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GetNotificationCategories()
        {
            var notifCats = new DataProvider().GetNotificationCategories();
            return Json(notifCats, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetNotificationsCount()
        {
            var userInfo = GetUserInfo();
            var count = new DataProvider().GetNotificationsCount(userInfo);
            return Json(count, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [OutputCache(Duration = 10, VaryByParam = "*", Location = System.Web.UI.OutputCacheLocation.Server, NoStore = false)]
        public JsonResult GetSums(DTParameterModel param)
        {
            var data = new DataProvider();
            var table = data.GetSums(param);

            return Json(new
            {
                data = table
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetUnitSummaryTotals(DTParameterModel param)
        {
            var data = new DataProvider();
            var table = data.GetUnitsSummaryTable(param);
            var result = from c in table
                         select new[]
                         {
                             c.ForecastDef.ToString()
                             , c.Actual.ToString()
                             , c.FC.ToString()
                             , c.Var.ToString()
                         };

            return Json(new
            {
                draw = param.Draw,
                data = result,
            }, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public JsonResult GetUpdatedCellsByForecastIDs(DTParameterModel param)
        {
            try
            {
                var data = new DataProvider();

                var table = data.GetUpdatedCellsByForecastIds(param);

                return Json(new { data = table }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                e.Data.Add("vendor", param.TableName);
                throw e;
            }
        }

        [HttpPost]
        public JsonResult GetVendorList()
        {
            if (IsAdmin())
            {
                var results = new DataProvider().GetVendorList();
                return Json(results, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> RemoveItemPatchOwnershipClaims(EditorParameterModel editor, ItemPatchOverlap itemPatchOverlap)
        {
            if (editor.GMSVenID == "0" || string.Equals(editor.TableName, "tbl_allvendors", StringComparison.InvariantCultureIgnoreCase))
            {
                return Json(new { success = false, message = "You do not have permission to remove item/patch claims", isPrefreeze = true }, JsonRequestBehavior.AllowGet);
            }

            var data = new DataProvider();
            var result = await data.RemoveItemPatchOwnershipClaims(editor, itemPatchOverlap);
            if (result == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var isPrefreeze = data.GetToolConfigValue("preFreeze");
            return Json(new { sccess = result.success, result.message, isPrefreeze }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Updates an existing bookmark
        /// </summary>
        /// <param name="gmsvenid"></param>
        /// <param name="username"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult UpdateBookmark(int gmsvenid, string username, string bookmarkName, string state)
        {
            new DataProvider().UpdateBookmark(gmsvenid, username, bookmarkName, state);
            return Json(new { }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateDlEvent(HttpPostedFileBase file)
        {
            // We can't send a json object with a form so we receive it as a string and deserialize it here.
            var editedStr = Request.Form.Get("edited");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var edited = serializer.Deserialize<DlEvent>(editedStr);

            edited.LastEdit = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (file != null && file.ContentLength > 0)
            {
                var driveService = DlGoogleDriveService.GetDriveServiceWithFile();
                edited.FileId = DlGoogleDriveService.UpdateFile(driveService, edited.FileId, file);
            }

            if (IsAdmin())
            {
                new DataProvider().UpdateDlEvent(edited);
                return Json(new { response = "ok" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateNotification(Notification original, Notification edit)
        {
            edit.LastEdit = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (IsAdmin())
            {
                new DataProvider().UpdateNotification(original, edit);
                return Json(new { response = "ok" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> UpdateTableData(EditorParameterModel param)
        {
            DTParameterModel dt = new DTParameterModel();

            dt = param.EditorToDTParam(param);

            dt.TableName = param.TableName;

            var testvar = GetIsAllocationEditable(dt, param);

            // Check if sales units shouldn't edited
            if (!GetIsAllocationEditable(dt, param))
            {
                var message = "The row you are trying to update has no historical trend. " +
                    "\nPlease upload desired units in a file, and we will allocate based on the trend for the corresponding assortment.";
                return Json(new { response = "zero-allocation", message = message }, JsonRequestBehavior.AllowGet);
            }

            int rows = new DataProvider().GetForecastTableCount(dt, false, true);
            if (param.EditMode != "inline" && rows > 1000000)
                return Json(new { count = rows }, JsonRequestBehavior.AllowGet);
            
            try
            {
                var data = new DataProvider();

                if (param.Action == "edit")
                {
                    if (param.RetailPrice.Count<ERetailPrice>() > 0)
                        await data.UpdateRetailPrice(param);
                    if (param.SalesU.Count<ESalesU>() > 0)
                        await data.UpdateSalesUnits(param);
                    if (param.SalesUVar.Count<ESalesUVar>() > 0)
                    {
                        data.UpdateSalesUVar(param);
                    }
                    if (param.MMComments.Count<EMMComments>() > 0)
                        await data.UpdateMMComments(param);
                    if (param.VendorComments.Count<EVendorComments>() > 0)
                        await data.UpdateVendorComments(param);
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost]
        public JsonResult UpdateTutorial(Tutorial original, Tutorial edit)
        {
            edit.LastEdit = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (IsAdmin())
            {
                new DataProvider().UpdateTutorial(original, edit);
                return Json(new { response = "ok" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { response = "error" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateViewedNotification(ViewedNotification notif)
        {
            var userInfo = GetUserInfo();
            try
            {
                new DataProvider().UpdateViewedNotification(userInfo, notif);
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Checks to see if the process is currently running in the pool of processes on the server.
        /// </summary>
        /// <param name="importProcess"></param>
        /// <returns></returns>
        private bool IsImportProcessRunning(ImportProcess importProcess)
        {
            if (importProcess != null && importProcess.GMSVenID != -1 && importProcess.ProcessId != -1)
            {
                var processes = Process.GetProcessesByName("python");
                foreach (var process in processes)
                {
                    if (process.Id == importProcess.ProcessId)
                        return true;
                }
            }

            return false;
        }

        public JsonResult Upload(HttpPostedFileBase file)
        {
            try
            {
                string username = Membership.GetUser().UserName;
                var userDetail = new DataProvider().GetUserDetails(username).ToList();
                int gmsvenid = userDetail.FirstOrDefault().GMSVenID;
                
                // First check to see if the current vendor already has an import upload job running.
                // If there's a job running then return and let them know the job is still running.
                var importProcess = new DataProvider().GetImportProcess(gmsvenid);
                if (IsImportProcessRunning(importProcess))
                {
                    var returnFileName = Regex.Replace(importProcess.FileName, "_([0-9a-zA-Z\\\\-]*)_.csv", ".csv");
                    ViewBag.Message = "ERROR! Upload file already running.";
                    return Json(new { success = false, msg = $"Please wait... \nYour previous file {returnFileName} is still processing..." }, JsonRequestBehavior.AllowGet);
                }
                else if (file != null && file.ContentLength > 0)
                {
                    string ext = Path.GetExtension(file.FileName);
                    if (String.Compare(Path.GetExtension(file.FileName), ".csv", true) != 0)
                    {
                        return Json(new { success = false, msg = "Upload failed! Incorrect file type." }, JsonRequestBehavior.AllowGet);
                    }
                    try
                    {
                        Guid id = Guid.NewGuid();
                        var newFileName = file.FileName;

                        if (newFileName.Contains(' '))
                        {
                            newFileName = file.FileName.Replace(' ', '_');
                        }
                        var extension = Path.GetExtension(file.FileName);
                        //remove extension so we can clear any extra '.' in name
                        if (newFileName.Contains(extension))
                        {
                            newFileName = newFileName.Replace(extension, "");
                        }

                       
                        //check for all other special characters
                        var pattern = new Regex("[:!@#$%^&*()}{|\":?><\\;'.,~]");

                        //clean name and add extension back
                        newFileName = pattern.Replace(newFileName, "") + extension;
                        
                        string filename = Path.GetFileNameWithoutExtension(newFileName) + '_' + id.ToString() + '_' + Path.GetExtension(file.FileName);
                        string path = Path.Combine(Server.MapPath("~/Imports"), filename);
                        file.SaveAs(path);
                        ViewBag.Message = "File uploaded successfully";
                        ExecuteUploadScript(path);
                        return Json(new { success = true, msg = "Success! Your file has been uploaded. You will receive an email when it has finished processing." }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                        ViewBag.Message = "ERROR:" + e.Message.ToString();

                        throw e;
                    }
                }
                else
                {
                    ViewBag.Message = "ERROR! No file selected. Try again and browse for a file.";
                    return Json(new { success = false, msg = "Upload failed! Empty file or no file selected." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<JsonResult> UploadNewItems(EditorParameterModel editor, HttpPostedFileBase file)
        {
            try
            {
                var data = new DataProvider();

                if (file != null && file.ContentLength > 0)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Guid id = Guid.NewGuid();
                    var fileName = FormatFileName(file.FileName, id.ToString());
                    var appSettingsExportsPath = appSettings.Get("FilePath");
                    var serverPath = server.MapPath(appSettingsExportsPath);
                  

                    var dataCommands = new DataCommands();
                    var localFilePath = Path.Combine(Server.MapPath("~/Imports"), fileName);

                    // Paths to store the file for different states
                    // filePath -> When working with the file
                    // successFilePath -> When file has been successfully processed
                    // errorFilePath -> When an error occurred with the file
                    var filePath = Path.Combine(Util.FTPVerticaForecastPath, fileName);
                    var successFilePath = Path.Combine(Util.FTPVerticaForecastSuccessPath, fileName);
                    var errorFilePath = Path.Combine(Util.FTPVerticaForecastErrorPath, fileName);

                    var ext = Path.GetExtension(file.FileName);
                    if (string.Compare(Path.GetExtension(file.FileName), ".csv", true) != 0)
                    {
                        return Json(new { success = false, message = "Upload failed! Incorrect file type." }, JsonRequestBehavior.AllowGet);
                    }
                    try
                    {
                        file.SaveAs(localFilePath);
                        file.SaveAs(filePath);

                        var missingColumns = data.IsUploadFileValid(localFilePath, "NewItemUploadColumns");

                        // If any columns are missing from the file then inform the user of them.
                        if (missingColumns.Count() > 0)
                        {
                            var dtHeaders = new DTHeaderNames();
                            var niceHeaders = missingColumns.Select(mc => dtHeaders.GetDTHeaderName(mc));
                            var message = $"The header names '{string.Join(", ", niceHeaders)}' are missing from you upload template. Please download the 'New Items Upload' template in order to upload new items.";
                            data.UpdateUploadLog(new UploadLog
                            {
                                GmsVenId = int.Parse(editor.GMSVenID),
                                VendorDesc = editor.VendorGroup,
                                FileUploadType = "New Items Upload",
                                FileName = fileName,
                                TimeStamp = Util.GetTimestamp(),
                                Success = false,
                                UserLogin = editor.Username,
                                SuccessOrFailureMessage = message,
                                Duration = Util.GetTime(stopwatch.ElapsedMilliseconds)
                            });

                            stopwatch.Stop();
                            return Json(new { success = false, message }, JsonRequestBehavior.AllowGet);
                        }
                       
                        var results = await data.UploadNewItems(editor, serverPath, localFilePath, fileName);
                        
                        System.IO.File.Move(filePath, results.success ? successFilePath : errorFilePath);
                        if (System.IO.File.Exists(localFilePath))
                        {
                            System.IO.File.Delete(localFilePath);
                        }

                        stopwatch.Stop();
                        return Json(results, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                        stopwatch.Stop();
                        System.IO.File.Move(filePath, errorFilePath);
                        if (System.IO.File.Exists(localFilePath))
                        {
                            System.IO.File.Delete(localFilePath);
                        }
                        return Json(new FileUploadResult
                        {
                            message = "Oops! Something went wrong with your file. Please review your file to make sure you have the correct file and it's in the right format. If you need additional help, feel free to email support@demandlink.com. We'd be happy to help!",
                            success = false
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Upload failed! Empty file or no file selected." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        public async Task<JsonResult> UploadItemPatchOwnership(EditorParameterModel editor, HttpPostedFileBase file)
        {
            try
            {
                var data = new DataProvider();

                if (file != null && file.ContentLength > 0)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Guid id = Guid.NewGuid();
                    var fileName = FormatFileName(file.FileName, id.ToString());
                    var appSettingsExportsPath = appSettings.Get("FilePath");
                    var serverPath = server.MapPath(appSettingsExportsPath);

                    var dataCommands = new DataCommands();
                    var localFilePath = Path.Combine(Server.MapPath("~/Imports"), fileName);

                    // Paths to store the file for different states
                    // filePath -> When working with the file
                    // successFilePath -> When file has been successfully processed
                    // errorFilePath -> When an error occurred with the file
                    var filePath = Path.Combine(Util.FTPVerticaForecastPath, fileName);
                    var successFilePath = Path.Combine(Util.FTPVerticaForecastSuccessPath, fileName);
                    var errorFilePath = Path.Combine(Util.FTPVerticaForecastErrorPath, fileName);

                    var ext = Path.GetExtension(file.FileName);
                    if (string.Compare(Path.GetExtension(file.FileName), ".csv", true) != 0)
                    {
                        return Json(new { success = false, message = "Upload failed! Incorrect file type." }, JsonRequestBehavior.AllowGet);
                    }
                    try
                    {
                        file.SaveAs(localFilePath);
                        file.SaveAs(filePath);

                        var missingColumns = data.IsUploadFileValid(localFilePath, "IOU");

                        if (missingColumns.Count > 0)
                        {
                            var dtHeaders = new DTHeaderNames();
                            var niceHeaders = missingColumns.Select(mc => dtHeaders.GetDTHeaderName(mc));
                            var message = $"The header names '{string.Join(", ", niceHeaders)}' are missing from your upload template. Please download the 'Item Ownership Upload' template in order to use this upload feature.";
                            data.UpdateUploadLog(new UploadLog
                            {
                                GmsVenId = int.Parse(editor.GMSVenID),
                                VendorDesc = editor.VendorGroup,
                                FileUploadType = "IOU",
                                FileName = fileName,
                                TimeStamp = Util.GetTimestamp(),
                                Success = false,
                                UserLogin = editor.Username,
                                SuccessOrFailureMessage = message,
                                Duration = Util.GetTime(stopwatch.ElapsedMilliseconds)
                            });
                            stopwatch.Stop();
                            System.IO.File.Move(filePath, errorFilePath);
                            if (System.IO.File.Exists(localFilePath))
                            {
                                System.IO.File.Delete(localFilePath);
                            }

                            stopwatch.Stop();
                            return Json(new { success = false, message }, JsonRequestBehavior.AllowGet);
                        }
                        
                        var result = await data.UploadItemPatchOwnership(editor, serverPath, localFilePath, fileName);
                        
                        System.IO.File.Move(filePath, result.success ? successFilePath : errorFilePath);
                        if (System.IO.File.Exists(localFilePath))
                        {
                            System.IO.File.Delete(localFilePath);
                        }

                        stopwatch.Stop();
                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                        stopwatch.Stop();
                        System.IO.File.Move(filePath, errorFilePath);
                        if (System.IO.File.Exists(localFilePath))
                        {
                            System.IO.File.Delete(localFilePath);
                        }
                        return Json(new FileUploadResult
                        {
                            message = "Oops! Something went wrong with your file. Please review your file to make sure you have the correct file and it's in the right format. If you need additional help, feel free to email support@demandlink.com. We'd be happy to help!",
                            success = false
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Upload failed! Empty file or no file selected." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void ExecuteUploadScript(string path)
        {
            try
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();

                string username = Membership.GetUser().UserName;
                string userEmail = Membership.GetUser().Email;
                var userDetail = new DataProvider().GetUserDetails(username).ToList();
                int gmsvenid = userDetail.FirstOrDefault().GMSVenID;
                string tableName = new DataProvider().GetTableName(gmsvenid, "vendor", username);
                //adding stageing table
                string stageTableName = "stage_" + tableName;

                string file = Path.GetFileName(path);
                string root = Path.GetDirectoryName(path);

                // Adjust the database context based on the private variable in DataCommands
                string databaseContext = new DataCommands().GetDatabaseContext();
                if (databaseContext == "_Dev")
                    databaseContext = "dev.";
                else
                    databaseContext = "public.";

                startInfo.WindowStyle = ProcessWindowStyle.Maximized;
                startInfo.FileName = "C:\\Anaconda3\\python.exe";
                //startInfo.Arguments = root + "\\upload.py " + databaseContext + ' ' + gmsvenid.ToString() + ' ' + userEmail + ' ' + tableName + " " + file + " " + username;
                //added stageTableName
                startInfo.Arguments = root + "\\upload.py " + databaseContext + ' ' + gmsvenid.ToString() + ' ' + userEmail + ' ' + tableName + " " + stageTableName + " " + file + " " + username;
                var stuff = startInfo.Arguments;
                Console.WriteLine(stuff);
                startInfo.UseShellExecute = false;
                process.StartInfo = startInfo;
                process.Start();

                // This section records the process information for the vendor so we don't allow them to
                // run another upload before the previous one has ended
                var importProcess = new ImportProcess()
                {
                    GMSVenID = gmsvenid,
                    ProcessId = process.Id,
                    FileName = file,
                    StartTime = process.StartTime
                };

                new DataProvider().UpdateVendorImportProcess(importProcess);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private string FormatFileName(string fileName, string id)
        {
            var extension = Path.GetExtension(fileName);

            if (fileName.Contains(' '))
            {
                fileName = fileName.Replace(' ', '_');
            }

            //remove extension so we can clear any extra '.' in name
            if (fileName.Contains(extension))
            {
                fileName = fileName.Replace(extension, "");
            }

            var newFileName = fileName;
            
            //check for all other special characters
            var pattern = new Regex("[:!@#$%^&*()}{|\":?><\\;'.,~]");

            //clean name and add extension back
            newFileName = pattern.Replace(newFileName, "") + extension;

            // Shrink the file name to make room for the UUID incase the file name is too long.
            var fileNameNoExt = Path.GetFileNameWithoutExtension(newFileName);
            var shortFileName = fileNameNoExt
                .Substring(0, fileNameNoExt.Length > 52 ? 52 : fileNameNoExt.Length) + '_' + id.ToString() + '_' + Path.GetExtension(newFileName);

            var finalFileName = $"{fileNameNoExt}_{id}{Path.GetExtension(newFileName)}";
            return finalFileName;
        }

        /// <summary>
        /// Gets info for a user such as GMSVenID, UserName, TableName, Email
        /// </summary>
        /// <returns>A <seealso cref="UserInfo"/> object.</returns>
        private UserInfo GetUserInfo()
        {
            var mUser = Membership.GetUser();
            var userInfo = new UserInfo
            {
                UserName = mUser.UserName,
                Email = mUser.Email
            };
            var userDetail = new DataProvider().GetUserDetails(userInfo.UserName).ToList();
            userInfo.GMSVenId = userDetail.FirstOrDefault().GMSVenID;
            userInfo.TableName = new DataProvider().GetTableName(userInfo.GMSVenId, userDetail.FirstOrDefault().VendorGroup, userInfo.UserName);

            return userInfo;
        }

        /// <summary>
        /// Checks to see if the current user is an admin. Currently an admin is a DemandLink employee.
        /// </summary>
        /// <returns></returns>
        private bool IsAdmin()
        {
            var userInfo = GetUserInfo();

            return userInfo.GMSVenId == 0
                && string.Equals(userInfo.TableName, "tbl_AllVendors", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "harrybarker", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "bradjulian", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "amyhollinger", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "shannawright", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "susasiharat", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "nickimccall", StringComparison.InvariantCultureIgnoreCase)
                && !string.Equals(userInfo.UserName, "bobsuds", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Checks to see if sales units can be edited.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool GetIsAllocationEditable(DTParameterModel dt, EditorParameterModel param)
        {
            if (param.SalesU.Count<ESalesU>() > 0)
            {
                var provider = new DataProvider().GetIsAllocationEditable(dt);
                return provider;
            }
            else
                return true;
        }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;

namespace Forecast.Models
{
    public class Bookmark
    {
        public string bookmarkName { get; set; }
        public ForecastJsonBookmark state { get; set; }
        public string version { get; set; }
    }

    public class ForecastBookmark
    {
        public int GMSVenID { get; set; }
        public string Username { get; set; }
        public string BookmarkName { get; set; }
        public System.DateTime Timestamp { get; set; }
        public string State { get; set; }
    }

    public class ForecastJsonBookmark
    {
        public int GMSVenID { get; set; }
        public string Username { get; set; }
        public string BookmarkName { get; set; }
        public DateTime Timestamp { get; set; }
        public object State { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of BookmarkManager. Used for saving bookmarks, retrieving bookmarks, deleting bookmarks, and retrieving bookmark names.
    /// </summary>
    public class BookmarksManager
    {
        /// <summary>
        /// Holds the current bookmark build version.
        /// </summary>
        private readonly string bookmarkVersion = ConfigurationManager.AppSettings.Get("Reporting_Bookmark_Version");

        /// <summary>
        /// Contains the directory path where the bookmark files will be saved.
        /// </summary>
        private readonly string directory;

        /// <summary>
        /// Holds the GMSVenID for the current user.
        /// </summary>
        private readonly int gmsVenId;

        /// <summary>
        /// Holds the directory of where projects store their directories. Example, this can be D:\, C:\WebData, \\server\Drive$\.., etc...
        /// </summary>
        private readonly string driveDataLocation = ConfigurationManager.AppSettings.Get("Data_Drive_Location");

        /// <summary>
        /// The schema of the current project. Could be dev or empty (Production).
        /// </summary>
        private readonly string schema = ConfigurationManager.AppSettings.Get("Vertica_DB_Schema");

        /// <summary>
        /// Holds the back-end table name for bookmarks in the current project.
        /// </summary>
        private readonly string tableName = "tbl_Bookmarks";

        /// <summary>
        /// Holds the user name of the current user.
        /// </summary>
        private readonly string userName;

        /// <summary>
        /// Creates an instance of the <seealso cref="BookmarksManager"/> class.
        /// </summary>
        /// <param name="gmsVenId"></param>
        /// <param name="userName"></param>
        /// about the current project.</param>
        public BookmarksManager(int gmsVenId, string userName)
        {
            directory = $"Forecast{schema}\\Bookmarks";
            this.gmsVenId = gmsVenId;
            this.userName = userName;

            var directoryPath = $"{driveDataLocation}{directory}";
            // If the provided directory doesn't exist then create it.
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Saves the provided state as a bookmark for the user.
        /// </summary>
        /// <param name="bookmarkName">A <see cref="string"/> as the name to be given to the bookmark.</param>
        /// <param name="state">The string state you wish to save.</param>
        /// <returns>An int representing the success status. A 0 is returned if successful and -1 if the bookmark already exists.</returns>
        public int CreateBookmark(string bookmarkName, string state)
        {
            try
            {
                // If the same bookmark name already exists then don't create it.
                if (isBookmarkExists(bookmarkName))
                {
                    return -1;
                }

                var jsonBookmark = new ForecastJsonBookmark
                {
                    BookmarkName = bookmarkName,
                    GMSVenID = gmsVenId,
                    State = JsonConvert.DeserializeObject(state),
                    Timestamp = DateTime.Now,
                    Username = userName
                };

                // Crate the file name and back-end entry
                var fileName = createFileName(bookmarkName);
                createBookmarkDbEntry(bookmarkName, fileName);
                var filePath = getBookmarkFilePath(fileName);

                // Save the bookmark in a file.
                saveBookmarkFile(filePath, jsonBookmark);

                return 0;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Delete a bookmark based on it's name.
        /// </summary>
        /// <param name="bookmarkName"></param>
        public void DeleteBookmark(string bookmarkName)
        {
            try
            {
                var bookmarkFileName = getBookmarkFileName(bookmarkName);
                deleteDbBookmarkEntry(bookmarkName);
                File.Delete(getBookmarkFilePath(bookmarkFileName));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Retrieve a bookmark by its name.
        /// </summary>
        /// <param name="bookmarkName"></param>
        /// <returns>A <seealso cref="Bookmark"/> corresponding to the bookmarkName you provided.</returns>
        public ForecastBookmark GetBookmark(string bookmarkName)
        {
            try
            {
                var bookmarkFileName = getBookmarkFileName(bookmarkName);
                if (!bookmarkName.IsValid())
                {
                    return null;
                }

                var filePath = getBookmarkFilePath(bookmarkFileName);
                var bookmark = getBookmarkFromFile(filePath);

                if (bookmark != null)
                {
                    var forecastBookmark = new ForecastBookmark
                    {
                        BookmarkName = bookmark.state.BookmarkName,
                        GMSVenID = bookmark.state.GMSVenID,
                        State = JsonConvert.SerializeObject(bookmark.state.State),
                        Timestamp = bookmark.state.Timestamp,
                        Username = bookmark.state.Username
                    };

                    return forecastBookmark;
                }
                else
                {
                    return new ForecastBookmark();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Retrive a list of bookmark names for the current user.
        /// </summary>
        /// <returns>A <seealso cref="List{string}"/> of bookmark names.</returns>
        public List<FilterParameter> GetBookmarkNames()
        {
            try
            {
                var bookmarkNames = getBookmarkNamesExpandoList();
                var result = new List<FilterParameter>();
                for (int i = 0; i < bookmarkNames.Count; i++)
                {
                    var expando = bookmarkNames[i];
                    expando.TryGetValue("bookmarkName", out string bookmarkName);
                    if (bookmarkName.IsValid())
                    {
                        result.Add(new FilterParameter { id = i + 1, text =  bookmarkName });
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                return new List<FilterParameter>();
            }
        }

        public bool GetIsBookmark(string bookmarkName)
        {
            try
            {
                return isBookmarkExists(bookmarkName);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Update an existing bookmark
        /// </summary>
        /// <param name="bookmarkName"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public void UpdateBookmark(string bookmarkName, string state)
        {
            try
            {
                var bookmarkFileName = getBookmarkFileName(bookmarkName);
                if (!bookmarkName.IsValid())
                {
                    throw new Exception("No bookmark name provided");
                }

                var filePath = getBookmarkFilePath(bookmarkFileName);
                var bookmark = getBookmarkFromFile(filePath);

                if (bookmark != null)
                {
                    bookmark.state.State = JsonConvert.DeserializeObject(state);
                    saveBookmarkFile(filePath, bookmark.state);
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        /// <summary>
        /// Saves the file name to the back-end.
        /// </summary>
        /// <param name="bookmarkName">The <seealso cref="string"/> name of the bookmark.</param>
        /// <param name="bookmarkFileName">The <seealso cref="string"/> file name of the bookmark.</param>
        private void createBookmarkDbEntry(string bookmarkName, string bookmarkFileName)
        {
            try
            {
                var cmd = string.Format(@"
                    INSERT INTO Forecast{0}.{1} 
                    (
                        GMSVenId, 
                        Username, 
                        BookmarkName,
                        FileName,
                        TimeStamp
                    ) 
                    VALUES ({2}, $${3}$$, $${4}$$, $${5}$$, $${6}$$);"
                , schema
                , tableName
                , gmsVenId
                , userName
                , bookmarkName
                , bookmarkFileName
                , Util.GetTimestamp());

                Util.ExecuteNonQuery(cmd);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Creates a file name from the given <seealso cref="string"/> bookmark name. This will remove all
        /// illegal characters and replace all spaces with an '_'. It uses the users' "user name"_"approved bookmark name"_"GUID".JSON
        /// as the name.
        /// </summary>
        /// <param name="bookmarkName">A <see cref="string"/> bookmark name that you want to include in the file name.</param>
        /// <returns>A <see cref="string"/> file name for the given bookmark name.</returns>
        private string createFileName(string bookmarkName)
        {
            var username = userName;
            var guid = Guid.NewGuid().ToString();
            var newBookmarkName = bookmarkName;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                newBookmarkName = newBookmarkName.Replace($"{c}", string.Empty);
            }

            newBookmarkName = newBookmarkName.Replace(" ", "_");

            var fileName = $"{username}_{newBookmarkName}_{guid}.JSON";

            return fileName;
        }

        /// <summary>
        /// Deletes a bookmark entry in the back-end.
        /// </summary>
        /// <param name="bookmarkName">The <see cref="string"/> bookmark name of the bookmark you want to delete.</param>
        private void deleteDbBookmarkEntry(string bookmarkName)
        {
            try
            {
                var cmd = string.Format(@"
                    DELETE FROM Forecast{0}.{4}
                    WHERE GMSVenId = {1} and
                    Username = $${2}$$ and
                    BookmarkName = $${3}$$;"
                , schema
                , gmsVenId
                , userName
                , bookmarkName
                , tableName);

                Util.ExecuteNonQuery(cmd);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Retrieves a bookmark file name from the back-end.
        /// </summary>
        /// <param name="bookmarkName">The <seealso cref="string"/> bookmark name.</param>
        /// <returns>A <see cref="string"/> file name for the given bookmark name.</returns>
        private string getBookmarkFileName(string bookmarkName)
        {
            var cmd = string.Format(@"SELECT FileName from Forecast{0}.{1} WHERE GMSVenID = {2} AND UserName = $${3}$$ AND BookmarkName = $${4}$$;"
                , schema
                , tableName
                , gmsVenId
                , userName
                , bookmarkName);

            var bookmarkFileName = ExpandoUtil.GetDbValue<string>("FileName", cmd);
            return bookmarkFileName;
        }

        /// <summary>
        /// Builds a complete path where the file can be stored, retrieved, and deleted.
        /// </summary>
        /// <param name="fileName">The <see cref="string"/> file name you want to build a path for.</param>
        /// <returns>A <see cref="string"/> full path for the given file name.</returns>
        private string getBookmarkFilePath(string fileName)
        {
            var filePath = $"{driveDataLocation}{directory}\\{fileName}";
            return filePath;
        }

        private Bookmark getBookmarkFromFile(string filePath)
        {
            try
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var param = (Bookmark)serializer.Deserialize(file, typeof(Bookmark));

                    return param;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Retrieves a <seealso cref="List{ExpandoObject}"/> of bookmark names for the current user.
        /// </summary>
        /// <returns>A <seealso cref="List{ExpandoObject}"/> of bookmark names.</returns>
        private List<ExpandoObject> getBookmarkNamesExpandoList()
        {
            try
            {
                var cmd = string.Format(@"
                    select bookmarkName from Forecast{0}.{3} 
                    where (GMSVenId = {1} or GMSVenId = -1) and (Username = $${2}$$ or Username = 'all')
                    order by lower(BookmarkName);"
                , schema
                , gmsVenId
                , userName
                , tableName);

                var bookmarkNameExpandos = ExpandoUtil.GetExpandoList(cmd);
                return bookmarkNameExpandos;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Checks to see if a bookmark name exists in the back-end.
        /// </summary>
        /// <param name="bookmarkName">The <see cref="string"/> bookmark name you want to check for.</param>
        /// <returns>A <see cref="bool"/> of true if a bookmark with the given name already exits for the current user. False if 
        /// it doesn't exist.</returns>
        private bool isBookmarkExists(string bookmarkName)
        {
            var cmd = string.Format(@"
                select bookmarkname from Forecast{0}.{4}
                where 
                GMSVenId = {1} and
                Username = $${2}$$ and
                lower(BookmarkName) = lower($${3}$$);"
            , schema
            , gmsVenId
            , userName
            , bookmarkName
            , tableName);

            try
            {
                var bookmarkExists = ExpandoUtil.GetExpandoList(cmd);

                if (bookmarkExists.Count > 0)
                {
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                return true;
            }
        }

        private void saveBookmarkFile(string filePath, ForecastJsonBookmark forecastBookmark)
        {
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                var bookmark = new Bookmark
                {
                    bookmarkName = forecastBookmark.BookmarkName,
                    state = forecastBookmark,
                    version = bookmarkVersion
                };
                serializer.Serialize(file, bookmark);
            }
        }
    }
}
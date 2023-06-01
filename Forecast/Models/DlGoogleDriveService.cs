using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using System.Security.Cryptography.X509Certificates;
using FileV3 = Google.Apis.Drive.v3.Data.File;
using System.IO;
using System.Diagnostics;
using Forecast.Data;
using System.Threading.Tasks;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Download;
using System.Net;

namespace Forecast.Models
{
    public class DlGoogleDriveService
    {
        private static string schema = new DataCommands().GetDatabaseContext();
        private static string folderName = $"DL_Events{schema}";
        private static string fileMimeType = "application/vnd.google-apps.file";
        private static string folderMimeType = "application/vnd.google-apps.folder";
        public static string driveKey = "C:\\Google\\Drive\\key.json";

        /// <summary>
        /// Method to create a file and upload it to the Google Drive folder.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="file"></param>
        /// <returns>A string ID for the file or 'NullFile' if the <seealso cref="HttpPostedFileBase"/> file was null.</returns>
        public static async Task<string> CreateFile(DriveService service, HttpPostedFileBase file)
        {
            // First check that the file isn't null
            if (file != null)
            {
                try
                {
                    var mimeType = getMimeType(file.FileName);

                    // Create the file metadata
                    FileV3 createFile = createV3File(service, file.FileName, mimeType);

                    var permission = new Permission();
                    permission.Type = "anyone";
                    permission.Role = "reader";

                    // Create the upload request
                    var createRequest = service.Files.Create(createFile, getFileStream(file), mimeType);
                    createRequest.Fields = "*";

                    // Upload the file
                    await createRequest.UploadAsync();

                    //Creating Permission after folder creation.
                    service.Permissions.Create(permission, createRequest.ResponseBody.Id).Execute(); 

                    // Return the file ID
                    return createRequest.ResponseBody.Id;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            return "NullFile";
        }

        /// <summary>
        /// Method to delete a file in the Google Drive folder.
        /// </summary>
        /// <param name="service"> <seealso="DriveService"/> with the context of where the file will be deleted.</param>
        /// <param name="id"> A string id of the file to be updated.</param>
        public static void DeleteFile(DriveService service, string id)
        {
            // First check that the file isn't null
            if (id != null)
            {
                try
                {
                    // Create the upload request to update the file
                    var deleteRequest = service.Files.Delete(id);

                    // Upload the file
                    deleteRequest.Execute();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        public static Stream DownloadFile(DriveService service, string fileId)
        {
            var request = service.Files.Get(fileId);
            var stream = new System.IO.MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                            {
                                Console.WriteLine(progress.BytesDownloaded);
                                break;
                            }
                        case DownloadStatus.Completed:
                            {
                                Console.WriteLine("Download complete.");
                                break;
                            }
                        case DownloadStatus.Failed:
                            {
                                Console.WriteLine("Download failed.");
                                break;
                            }
                    }
                };
            request.Download(stream);
            return request.ExecuteAsStream();
        }

        /// <summary>
        /// Authenticating to Google using a Service account
        /// Documentation: https://developers.google.com/accounts/docs/OAuth2#serviceaccount
        /// </summary>
        /// <param name="serviceAccountEmail">From Google Developer console https://console.developers.google.com</param>
        /// <param name="credFilePath">Location of the .p12 or Json Service account key file downloaded from Google Developer console https://console.developers.google.com</param>
        /// <returns>AnalyticsService used to make requests against the Analytics API</returns>
        public static DriveService GetDriveServiceWithFile(string credFilePath = "")
        {
            credFilePath = credFilePath == "" ? driveKey : credFilePath;
            try
            {
                if (string.IsNullOrEmpty(credFilePath))
                    throw new Exception("Path to the service account credentials file is required.");
                if (!System.IO.File.Exists(credFilePath))
                    throw new Exception("The service account credentials file does not exist at: " + credFilePath);

                // These are the scopes of permissions you need. It is best to request only what you need and not all of them
                string[] scopes = new string[] { DriveService.Scope.Drive };             // View your Google Analytics data

                // For Json file
                if (Path.GetExtension(credFilePath).ToLower() == ".json")
                {
                    GoogleCredential credential;
                    using (var stream = new FileStream(credFilePath, FileMode.Open, FileAccess.Read))
                    {
                        credential = GoogleCredential.FromStream(stream)
                             .CreateScoped(scopes);
                    }

                    // Create the  Analytics service.
                    return new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Forecast",
                    });
                }
                else
                {
                    throw new Exception("Unsupported Service accounts credentials.");
                }

            }
            catch (Exception ex)
            {
                throw new Exception("CreateServiceAccountDriveFailed", ex);
            }
        }

        /// <summary>
        /// Method to convert an <seealso cref="HttpPostedFileBase"/> file into a <seealso cref="System.IO.MemoryStream"/> object.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static System.IO.MemoryStream getFileStream(HttpPostedFileBase file)
        {
            return new System.IO.MemoryStream(new BinaryReader(file.InputStream).ReadBytes(file.ContentLength));
        }

        /// <summary>
        /// Method to get the folder ID for storing DemandLink events
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static FileV3 GetFile(DriveService service, string id)
        {
            if (id != null)
            {
                // Create a request object for a file by id.
                var request = service.Files.Get(id);    
                request.Fields = "*";

                // Execute the request to get the file and all its fields.
                return request.Execute();
            }

            return null;
        }

        /// <summary>
        /// Method to get the folder ID for storing DemandLink events
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static string GetFolderId(DriveService service)
        {
            // Create a request object for a list of files.
            // NOTE: Folders are just files in Google Drive with a mimeType of application/vnd.google-apps.folder
            // and files are just files with a mimeType of application/vnd.google-apps.file.
            var request = service.Files.List();
            request.Fields = @"files(*)";

            // Set the request query to find our folder by name.
            request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";

            // Execute the request to get the list of folders (Google.Apis.Drive.v3.Data.File which is FileV3 in the import).
            IList<FileV3> folders = request.Execute().Files;

            // Check to see if the folder exists and if it doesn't then create it.
            if (folders.Count == 0)
            {
                var permission = new Permission();
                permission.Type = "anyone";
                permission.Role = "reader";

                // Since no folders exist then we create one.
                var createRequest = service.Files.Create(new FileV3()
                {
                    Name = $"{folderName}",
                    MimeType = folderMimeType,
                    CreatedTime = DateTime.Now
                });
                createRequest.Fields = "id";

                var responseBody = createRequest.Execute();

                //Creating Permission after folder creation.
                service.Permissions.Create(permission, responseBody.Id).Execute();

                // Return the created folders ID
                return responseBody.Id;
            }
            else
            {
                // Folder exists so we always return the first one created in case of duplication.
                return folders.OrderBy(f => f.CreatedTime).FirstOrDefault().Id;
            }
        }

        /// <summary>
        /// Method to create the metadata file to be uploaded to Google Drive.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="fileName"></param>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        private static FileV3 createV3File(DriveService service, string fileName, string mimeType = "application/vnd.google-apps.file")
        {

            // NOTE: Need to find out why FileExtension causes the null of response body
            return new FileV3()
            {
                Name = fileName,
                OriginalFilename = fileName,
                MimeType = mimeType,
                CreatedTime = DateTime.Now,
                Parents = new List<string>() { GetFolderId(service)},
                ViewersCanCopyContent = true
            };

        }

        /// <summary>
        /// Method to get the mimeType for the file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string getMimeType(string fileName)
        {
            var mimeType = "application/unknown";
            var ext = System.IO.Path.GetExtension(fileName).ToLower();
            var regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);

            if (regKey != null && regKey.GetValue("Content Type") != null)
            {
                mimeType = regKey.GetValue("Content Type").ToString();
            }

            return mimeType;
        }

        /// <summary>
        /// Method to get a <seealso cref="DriveService"/> for the given api key.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static DriveService GetServiceWithApiKey(string apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey))
                    throw new ArgumentNullException("api Key");

                return new DriveService(new BaseClientService.Initializer()
                {
                    ApiKey = apiKey,
                    ApplicationName = "Forecast",
                });
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create new Drive Service", ex);
            }
        }

        /// <summary>
        /// Method to update a file and upload it to the Google Drive folder.
        /// </summary>
        /// <param name="service"> <seealso="DriveService"/> with the context of where the file will be uploaded.</param>
        /// <param name="id"> A string id of the file to be updated.</param>
        /// <param name="file"> A <seealso="Google.Apis.Drive.v3.Data.File"/> file that will override the old file.</param>
        /// <returns>A string ID for the file or 'NullFile' if the <seealso cref="HttpPostedFileBase"/> file was null.</returns>
        public static string UpdateFile(DriveService service, string id, HttpPostedFileBase newFile)
        {
            // First check that the file isn't null
            if (newFile != null)
            {
                try
                {
                    var mimeType = getMimeType(newFile.FileName);

                    // Create the file metadata
                    FileV3 updateFile = createV3File(service, newFile.FileName, mimeType);

                    // Create the upload request to update the file
                    var updateRequest = service.Files.Update(updateFile, id, getFileStream(newFile), mimeType);
                    updateRequest.Fields = "id";

                    // Upload the file
                    updateRequest.Upload();

                    // Return the file ID
                    return updateRequest.ResponseBody.Id;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            return "NullFile";
        }
    }
}
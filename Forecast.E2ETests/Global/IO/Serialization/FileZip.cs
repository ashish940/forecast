using System.IO;
using System.IO.Compression;

namespace Forecast.E2ETests.Global.IO.Serialization
{
    public class FileZip
    {
        public static void ExtractTo(string zipFile, string directory)
        {
            if (!File.Exists(zipFile))
            {
                throw new FileNotFoundException($"No file at {zipFile}");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ZipFile.ExtractToDirectory(zipFile, directory);
        }
    }
}

using Forecast.Models;
using System.IO;

namespace Forecast.UnitTests.Models
{
    public class ServerUtilityMock : IServerUtilities
    {
        public string MapPath(string path)
        {
            var c = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            string current = c.Replace("Forecast.UnitTests", "Forecast\\");
            return Path.Combine(current, path);
        }

        public string currentDirectory()
        {
            var c = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            return c.Replace("Forecast.UnitTests", "Forecast\\");
        }
    }
}

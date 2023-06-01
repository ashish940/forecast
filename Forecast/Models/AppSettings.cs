using System.Configuration;

namespace Forecast.Models
{
    public interface IAppSettings
    {
        string Get(string key);
        void Set(string key, string value);
    }

    public class AppSettings : IAppSettings
    {
        public string Get(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        public void Set(string key, string value)
        {
            ConfigurationManager.AppSettings.Set(key, value);
        }
    }
}
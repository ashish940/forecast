using Forecast.Models;
using System;
using System.Collections.Generic;

namespace Forecast.UnitTests.Models
{
    public class AppSettingsMock : IAppSettings
    {
        public AppSettingsMock()
        {
            GetFunc = (string key) =>
            {
                Settings.TryGetValue(key, out string value);
                return value;
            };
            SetFunc = (string key, string value) => Settings.Add(key, value);
        }

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public Func<string, string> GetFunc { get; set; }

        public Action<string, string> SetFunc { get; set; }

        public string Get(string key)
        {
            return GetFunc(key);
        }

        public void Set(string key, string value)
        {
            SetFunc(key, value);
        }
    }
}

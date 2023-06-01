using System;

namespace Forecast.E2ETests.Global.Models
{
    public class DateUtil
    {
        /// <summary>
        /// Gets a string timestamp from a <seealso cref="DateTime"/> object.
        /// </summary>
        /// <param name="value">A <seealso cref="DateTime"/> object that represents the time you want to display as text.
        /// Don't provide a <seealso cref="DateTime"/> if you want a timestamp of now.</param>
        /// <returns></returns>
        public static string GetTimestamp(DateTime value = new DateTime())
        {
            value = (value == new DateTime()) ? DateTime.Now : value;
            return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string GetPathFriendlyTimeStamp(DateTime time = new DateTime()) => GetTimestamp(time).Replace("-", "_").Replace(" ", "_").Replace(":", "_");
    }
}

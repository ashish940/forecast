using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Forecast.Models
{
    public interface IServerUtilities
    {
        string MapPath(string path);
    }

    public class ServerUtilities : IServerUtilities
    {
        private readonly HttpServerUtilityBase server;

        public ServerUtilities(HttpServerUtilityBase server)
        {
            this.server = server;
        }

        public string MapPath(string path)
        {
            return server.MapPath(path);
        }
    }
}
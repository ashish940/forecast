using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Forecast.Models
{
    public class User
    {
        [Required(ErrorMessage = "Please Provide Username", AllowEmptyStrings = false)]
        public string Username { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        public int GMSVenID { get; set; }

        public string NTName { get; set; }

        public string TableName { get; set; }

        public string VendorDesc { get; set; }

        public string VendorGroup { get; set; }
    }
}
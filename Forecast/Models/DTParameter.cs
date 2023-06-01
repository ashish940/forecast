using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Web.Security;
using Forecast.Data;
using System;

namespace Forecast
{
	public class DTModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            base.BindModel(controllerContext, bindingContext);
            var request = controllerContext.HttpContext.Request;

            // Retrieve request data
            var draw = System.Convert.ToInt32(request["draw"]);
            var start = System.Convert.ToInt32(request["start"]);
            var length = System.Convert.ToInt32(request["length"]);
			// var gmsvenid = System.Convert.ToInt32(request["GMSVenID"]);
			//var _gmsvenid = request.Cookies["DL"]["GMSVenID"];
			var username = Membership.GetUser().UserName; 
            var tableName = System.Convert.ToString(DataProvider.Unprotect(request.Cookies["DL_Forecast_0"]["TableName"], "id"));
			var exportChoice = System.Convert.ToString(request["ExportChoice"]);
			int gmsvenid = System.Convert.ToInt16(DataProvider.Unprotect(request.Cookies["DL_Forecast_0"]["GMSVenID"], "id"));
            var isMM = Convert.ToBoolean(request["isMM"]);
            var isMD = Convert.ToBoolean(request["isMD"]);
            // Check if user is sorting
            var isRotating = System.Convert.ToBoolean(request["isRotating"]);
            var isFiltering = System.Convert.ToBoolean(request["isFiltering"]);


            // Search
            var search = new DTSearch
            {
                Value = request["search[value]"],
                Regex = System.Convert.ToBoolean(request["search[regex]"])
            };
            // Order
            var o = 0;
            var order = new System.Collections.Generic.List<DTOrder>();
            while (request["order[" + o + "][column]"] != null)
            {
                order.Add(new DTOrder
                {
                    Column = System.Convert.ToInt32(request["order[" + o + "][column]"]),
                    Name = System.Convert.ToString(request["columns[" + System.Convert.ToInt32(request["order[" + o + "][column]"]) + "][name]"]),// Make sure this points at the right column
                    Dir = request["order[" + o + "][dir]"]
                });
                o++;
            }
            // Custom Order
            var co = 0;
            var customOrder = new System.Collections.Generic.List<DTOrder>();
            while (request["customOrder[" + co + "][name]"] != null)
            {
                customOrder.Add(new DTOrder
                {
                    Column = System.Convert.ToInt32(request["customOrder[" + co + "][index]"]),
                    Name = request["customOrder[" + co + "][name]"],
                    Dir = request["customOrder[" + co + "][dir]"]
                });
                co++;
            }
            // Columns
            var c = 0;
            var columns = new System.Collections.Generic.List<DTColumn>();
            while (request["columns[" + c + "][name]"] != null)
            {
                columns.Add(new DTColumn
                {
                    Data = request["columns[" + c + "][data]"],
                    Name = request["columns[" + c + "][name]"],
                    Orderable = System.Convert.ToBoolean(request["columns[" + c + "][orderable]"]),
                    Searchable = System.Convert.ToBoolean(request["columns[" + c + "][searchable]"]),
                    Visible = System.Convert.ToBoolean(request["columns[" + c + "][visible]"]),
                    Search = new DTSearch
                    {
                        Value = request["columns[" + c + "][search][value]"],
                        Regex = System.Convert.ToBoolean(request["columns[" + c + "][search][regex]"])
                    }
                });
                c++;
            }
            // Rotator
            var r = 0;
            var rotator = new System.Collections.Generic.List<DTRotator>();
            while (request["rotator[" + r + "][column]"] != null)
            {
                rotator.Add(new DTRotator
                {
                    Column = request["rotator[" + r + "][column]"],
                    Included = System.Convert.ToBoolean(request["rotator[" + r + "][included]"])
                });
                r++;
            }

			return new DTParameterModel
			{
				GMSVenID = gmsvenid,
                IsFiltering = isFiltering,
                IsMM = isMM,
                IsMD = isMD,
                IsRotating = isRotating,
				//VendorDesc = vendorDesc,
				TableName = tableName,
				Draw = draw,
				Start = start,
				Length = length,
				Search = search,
				Order = order,
                CustomOrder = customOrder,
                Columns = columns,
				Rotator = rotator,
				ExportChoice = exportChoice,
				Username = username
            };
        }
    }

    [System.Web.Mvc.ModelBinder(typeof(DTModelBinder))]
    public class DTParameterModel
    {
        ///<summary>
        /// The Vendor ID number. This is used to identify what GMSVenID is passed to the 
        /// procedure.
        /// </summary>
        public int GMSVenID { get; set; }

        /// <summary>
        /// A flag that indicates if the table is requesting data based on a user filtering the table.
        /// </summary>
        public bool IsFiltering { get; set; }

        ///<summary>
        ///True if the user is an MM
        ///</summary>
        public bool IsMM { get; set; }

        ///<summary>
        ///True if the user is an MM
        ///</summary>
        public bool IsMD { get; set; }

        /// <summary>
        /// A flag that indicates if the table is requesting data based on a user rotating on new columns.
        /// </summary>
        public bool IsRotating { get; set; }

        ///<summary>
        /// This is the name of the vendor prepended with "Vend"
        ///</summary>
        [MaxLength(30)]
        [RegularExpression("^[a-zA-Z0-9_]*$", ErrorMessage = "Only Specific Characters Allowed for Vendor Name")]
        public string VendorDesc { get; set; }

        ///<summary>
        /// This is the table name used to determine the table.
        ///</summary>
        [MaxLength(30)]
        [RegularExpression("^[a-zA-Z0-9_]*$", ErrorMessage = "Wrong format for Table Name")]
        public string TableName { get; set; }

        /// <summary>
        /// Draw counter. This is used by DataTables to ensure that the Ajax returns from 
        /// server-side processing requests are drawn in sequence by DataTables 
        /// </summary>
        public int Draw { get; set; }

        /// <summary>
        /// Paging first record indicator. This is the start point in the current data set 
        /// (0 index based - i.e. 0 is the first record)
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Number of records that the table can display in the current draw. It is expected
        /// that the number of records returned will be equal to this number, unless the 
        /// server has fewer records to return. Note that this can be -1 to indicate that 
        /// all records should be returned (although that negates any benefits of 
        /// server-side processing!)
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Global Search for the table
        /// </summary>
        public DTSearch Search { get; set; }

        /// <summary>
        /// Collection of all column indexes and their sort directions
        /// </summary>
        public System.Collections.Generic.IEnumerable<DTOrder> Order { get; set; }

        /// <summary>
        /// Collection of all column indexes and their sort directions
        /// </summary>
        public System.Collections.Generic.IEnumerable<DTOrder> CustomOrder { get; set; }

        /// <summary>
        /// Collection of all columns in the table
        /// </summary>
        public System.Collections.Generic.IEnumerable<DTColumn> Columns { get; set; }

        /// <summary>
        /// Collection of columns that represent the current state of the rotator
        /// </summary>
        public System.Collections.Generic.IEnumerable<DTRotator> Rotator { get; set; }

        /// <summary>
        /// Used to differentiate between different export options
        /// </summary>
        public string ExportChoice { get; set; }

		/// <summary>
		/// Username associated with the user
		/// </summary>
		/// <returns></returns>
		public string Username { get; set; }

    }

    /// <summary>
    /// Represents search values entered into the table
    /// </summary>
    public sealed class DTSearch
    {
        /// <summary>
        /// Global search value. To be applied to all columns which have searchable as true
        /// </summary>
        [RegularExpression("(^[a-z ,.'\\-!]+$)", ErrorMessage = "A special character was restricted.")]
        public string Value { get; set; }

        /// <summary>
        /// true if the global filter should be treated as a regular expression for advanced 
        /// searching, false otherwise. Note that normally server-side processing scripts 
        /// will not perform regular expression searching for performance reasons on large 
        /// data sets, but it is technically possible and at the discretion of your script
        /// </summary>
        public bool Regex { get; set; }
    }

    /// <summary>
    /// Represents a column and it's order direction
    /// </summary>
    public sealed class DTOrder
    {
        /// <summary>
        /// Column to which ordering should be applied. This is an index reference to the 
        /// columns array of information that is also submitted to the server
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Column name of the Column index referenced in the variable Column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Ordering direction for this column. It will be asc or desc to indicate ascending
        /// ordering or descending ordering, respectively
        /// </summary>
        [MaxLength(4)]
        public string Dir { get; set; }
    }

    /// <summary>
    /// Represents an individual column in the table
    /// </summary>
    public sealed class DTColumn
    {
        /// <summary>
        /// Column's data source
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Column's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Flag to indicate if this column is orderable (true) or not (false)
        /// </summary>
        public bool Orderable { get; set; }

        ///<summary>
        /// Flag to indicate if this column is visible
        ///</summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Flag to indicate if this column is searchable (true) or not (false)
        /// </summary>
        public bool Searchable { get; set; }

        /// <summary>
        /// Search to apply to this specific column.
        /// </summary>
        public DTSearch Search { get; set; }
    }

    /// <summary>
    /// Represents the statement used to rotate the data table
    /// </summary>
    public sealed class DTRotator
    {
        ///<summary>
        /// Name of the column to be rotated on.
        ///</summary>
        [RegularExpression("^[a-zA-Z0-9_]*$", ErrorMessage = "A special character was detected.")]
        public string Column { get; set; }

        /// <summary>
        /// Flag to denote whether or not the column should be included in the select/groupby statements.
        /// </summary>
        public bool Included { get; set; }
    }

	
}
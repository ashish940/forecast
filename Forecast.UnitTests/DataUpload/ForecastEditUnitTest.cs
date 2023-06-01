using Forecast.Controllers;
using Forecast.Data;
using Forecast.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Forecast.UnitTests
{
    /// <summary>
    /// Summary description for UnitTest2
    /// </summary>
    [TestClass]
    public class UnitTest2
    {
        private readonly (string input, string output)[] testInputsAndOutputs = new[] { ("-1200", "0"), ("1200", "1200"), ("null", "0"), ("1", "1"), ("0", "0"), ("-1", "0"), ("", "exception") }; //
        private readonly List<string[]> rotationTypes = new List<string[]> // TODO: These have specific examples hardcoded at the moment; it'd be better if these dynamically pick one of the valid data points for that rotator combo
        {
            new string[] {"ItemID", "Patch", "FiscalWk", "-113168824-1-1-1-1-1-1ZS-1-1-1", "131688", "ZS", "24"},
            new string[] {"ItemID", "MM", "FiscalWk", "-113168821-1-1-1Mike Winter-1-1-1-1-1-1", "131688", "Mike Winter", "21"},
            new string[] {"ItemID", "Patch", "Total", "-1131688-1-1-1-1-1-1-1ZB-1-1-1", "131688", "ZB", ""},
            new string[] {"ItemID", "MM", "Total", "-1131688-1-1-1-1Eddie Yarberry-1-1-1-1-1-1", "131688", "Eddie Yarberry", ""},
            new string[] {"ItemID", "Region", "MM", "-1131688-1-1-1-1Andrew Robinson14-1-1-1-1-1", "131688", "14", "Andrew Robinson" }
        };
        private readonly List<string[]> uploadData = new List<string[]> // TODO: These have specific examples hardcoded at the moment; it'd be better if these dynamically pick one of the valid data points for that rotator combo
        {
            new string[] { "ItemID", "MM", "FiscalWk", "131688", "Mike Winter", "21"},
            new string[] { "ItemID", "Region", "MM", "131688", "14", "Andrew Robinson" },
            new string[] { "ItemID", "Patch", "FiscalWk", "131688", "ZS", "24"},
            new string[] { "ItemID", "Patch", "Total", "131688", "ZB", ""},
            new string[] { "ItemID", "MM", "Total", "131688", "Eddie Yarberry", ""}

        };
        private readonly string[] columnNames = new string[]
        {
                "ForecastID",
                "VendorDesc",
                "ItemID",
                "ItemDesc",
                "ItemConcat",
                "FiscalWk",
                "FiscalMo",
                "FiscalQtr",
                "MD",
                "MM",
                "Region",
                "District",
                "Patch",
                "ParentID",
                "ParentDesc",
                "ParentConcat",
                "ProdGrpID",
                "ProdGrpDesc",
                "ProdGrpConcat",
                "AssrtID",
                "AssrtDesc",
                "AssrtConcat",
                "SalesDollars_2LY",
                "SalesDollars_LY",
                "SalesDollars_TY",
                "SalesDollars_Curr",
                "SalesDollars_Var",
                "SalesDollars_FR_FC",
                "CAGR",
                "Turns_LY",
                "Turns_TY",
                "Turns_FC",
                "Turns_Var",
                "SalesUnits_2LY",
                "SalesUnits_LY",
                "SalesUnits_TY",
                "SalesUnits_FC",
                "SalesUnits_Var",
                "RetailPrice_LY",
                "RetailPrice_TY",
                "RetailPrice_FC",
                "RetailPrice_Var",
                "PriceSensitivityPercent",
                "PriceSensitivityImpact",
                "Asp_LY",
                "Asp_TY",
                "Asp_FC",
                "Asp_Var",
                "Margin_Percent_LY",
                "Margin_Percent_TY",
                "Margin_Percent_Curr",
                "Margin_Percent_Var",
                "Margin_Percent_FR",
                "Margin_Dollars_LY",
                "Margin_Dollars_TY",
                "Margin_Dollars_Var_Curr",
                "Margin_Dollars_Curr",
                "Margin_Dollars_FR",
                "Cogs_LY",
                "Cogs_TY",
                "Cogs_FC",
                "Cogs_Var",
                "GMROI_LY",
                "GMROI_TY",
                "GMROI_FC",
                "GMROI_Var",
                "SellThru_LY",
                "SellThru_TY",
                "ReceiptDollars_LY",
                "ReceiptDollars_TY",
                "ReceiptUnits_LY",
                "ReceiptUnits_TY",
                "Dollars_FC_DL",
                "Dollars_FC_LOW",
                "Dollars_FC_Vendor",
                "Units_FC_DL",
                "Units_FC_LOW",
                "Units_FC_Vendor",
                "Dollars_FC_DL_Var",
                "Dollars_FC_LOW_Var",
                "Dollars_FC_Vendor_Var",
                "Units_FC_DL_Var",
                "Units_FC_LOW_Var",
                "Units_FC_Vendor_Var",
                "Cost_LY",
                "Cost_TY",
                "Cost_FC",
                "Cost_Var",
                "MM_Comments",
                "Vendor_Comments"
        };
        private readonly string[] rotatorNames = new string[]
        {
                "VendorDesc",
                "ItemID",
                "FiscalWk",
                "FiscalMo",
                "FiscalQtr",
                "MD",
                "MM",
                "Region",
                "District",
                "Patch",
                "ParentID",
                "ProdGrpID",
                "AssrtID"
        };

        protected EditorParameterModel editor = new EditorParameterModel()
        {
            Action = "edit",
            Columns = new List<DTColumn>(), //This needs to be populated to work properly
            EditMode = "inline",
            GMSVenID = "57",
            IsMD = false,
            IsMM = false,
            MMComments = new List<EMMComments>(),
            RetailPrice = new List<ERetailPrice>(),
            Rotator = new List<DTRotator>(), // The bool for each of these should be set to false, and then apropriate ones updated to true for each rotator case. This needs to be populated to work properly
            SalesU = null, // This should be edited for each input value to be the input value
            SalesUVar = new List<ESalesUVar>(),
            TableName = "tbl_Bergens",
            Username = "BergensTestUser",
            VendorComments = new List<EVendorComments>(),
            VendorGroup = "tbl_Bergens"
        };

        protected DTParameterModel dt = new DTParameterModel()
        {
            Columns = new List<DTColumn>(), //This needs to be populated to work properly
            CustomOrder = new List<DTOrder>(),
            Draw = 1,
            ExportChoice = null,
            GMSVenID = 57,
            IsFiltering = true,
            IsMD = false,
            IsMM = false,
            IsRotating = true,
            Length = 20,
            Order = new List<DTOrder>(),
            Rotator = new List<DTRotator>(), //This needs to be populated to work properly
            Start = 0,
            TableName = "tbl_Bergens",
            Username = "BergensTestUser"
        };

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get => testContextInstance;
            set => testContextInstance = value;
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void InLineUploadTest()
        {
            // Build the components of our testing EditorParameterModel and DTParameterModel
            // First, create the list for the rotator attribute; we'll fill it with false 'Inclded' values for now and update depending on the test case
            var rotatorList = new List<DTRotator>();
            foreach (var rotatorName in rotatorNames)
            {
                rotatorList.Add(new DTRotator()
                {
                    Column = rotatorName,
                    Included = false
                });
            }
            // Next, create the list for the column attribute. These will also be updated as needed
            var columnList = new List<DTColumn>();
            var ctr = 0;
            foreach (var columnName in columnNames)
            {
                var orderable = true;
                var visible = false;
                var searchable = true;
                var value = "";
                if (columnName == "ForecastID") { orderable = false; searchable = false; } // ForecastID is the only column that isn't set to be orderable or searchable
                columnList.Add(new DTColumn()
                {
                    Data = ctr.ToString(),
                    Name = columnName,
                    Orderable = orderable,
                    Visible = visible,
                    Searchable = searchable,
                    Search = new DTSearch()
                    {
                        Regex = false,
                        Value = value
                    }

                });
                ctr++;
            }

            // The rortationTypes list contains an array of all informaion needed to test each rortation type test case
            foreach (var rotationCombo in rotationTypes)
            {
                // For each one we need to update the two ParameterModel objects so that they reflect the current test case roatator value
                foreach (var column in editor.Columns)
                {
                    var visible = false;
                    var value = "";
                    if (rotationCombo.Contains(column.Name))
                    {
                        visible = true;
                        value = rotationCombo[Array.IndexOf(rotationCombo, column.Name) + 4];
                    }
                    column.Visible = visible;
                    column.Search = new DTSearch()
                    {
                        Regex = false,
                        Value = value
                    };
                }
                foreach (var column in dt.Columns)
                {
                    var visible = false;
                    var value = "";
                    if (rotationCombo.Contains(column.Name))
                    {
                        visible = true;
                        value = rotationCombo[Array.IndexOf(rotationCombo, column.Name) + 4];
                    }
                    column.Visible = visible;
                    column.Search = new DTSearch()
                    {
                        Regex = false,
                        Value = value
                    };
                }
                // We also need to update the rotators that are in use for this test case
                foreach (var rotation in editor.Rotator)
                {
                    if (rotationCombo.Contains(rotation.Column))
                    {
                        rotation.Included = true;
                    }
                    else
                    {
                        rotation.Included = false;
                    }
                }
                foreach (var rotation in dt.Rotator)
                {
                    if (rotationCombo.Contains(rotation.Column))
                    {
                        rotation.Included = true;
                    }
                    else
                    {
                        rotation.Included = false;
                    }
                }
                // Now that all of the rotators and columns are set for the rotator case, the 
                foreach (var testInput in testInputsAndOutputs)
                {
                    editor.SalesU = new List<ESalesU>()
                    {
                        new ESalesU()
                        {
                            ID = rotationCombo[3],
                            SalesU = testInput.input
                        }
                    };
                    // In the case that the input is an empty string, the process will throw an exception, and the value will remain unchanged
                    int startingValue = 0;
                    if (testInput.input == "")
                    {
                        startingValue = new DataProvider().GetForecastTable(dt).ToList()[0].SalesUnits_FC;
                    }

                    //var updater = controller.UpdateTableData(editor);
                    //updater.Wait();
                    var input = new DataProvider().UpdateSalesUnits(editor);
                    input.Wait();

                    var output = new DataProvider().GetForecastTable(dt).ToList();
                    var outputValue = output[0].SalesUnits_FC;

                    if (testInput.input == "") // In the case of an empty string input, we need to check that the old value and new value are equal since no upload will occur
                    {
                        Assert.AreEqual(startingValue.ToString(), outputValue.ToString());
                    }
                    else
                    {
                        Assert.AreEqual(testInput.output, outputValue.ToString());
                    }
                }
            }
        }

        [TestMethod]
        public void FileUploadTest()
        {
            // Thiese steps allow the HomeController to have a mock environment to initalize in
            var controller = new HomeController();

            var pm = new Mock<IPrincipal>();
            //pm.Setup(x => x.IsAuthenticated).Returns(true);
            var httpcm = new Mock<HttpContextBase>();
            httpcm.Setup(x => x.User).Returns(pm.Object);

            var cc = new ControllerContext { HttpContext = httpcm.Object };
            cc.Controller = controller;
            controller.ControllerContext = cc;

            var rotatorList = new List<DTRotator>();
            foreach (var rotatorName in rotatorNames)
            {
                rotatorList.Add(new DTRotator()
                {
                    Column = rotatorName,
                    Included = false
                });
            }
            // Next, create the list for the column attribute. These will also be updated as needed
            var columnList = new List<DTColumn>();
            var ctr = 0;
            foreach (var columnName in columnNames)
            {
                var orderable = true;
                var visible = false;
                var searchable = true;
                var value = "";
                if (columnName == "ForecastID") { orderable = false; searchable = false; } // ForecastID is the only column that isn't set to be orderable or searchable
                //if (columnName == "ItemID") { visible = true; value = "131688"; }
                //else if (columnName == "Patch") { visible = true; value = "ZS"; }
                //else if (columnName == "FiscalWk") { visible = true; value = "24"; }
                columnList.Add(new DTColumn()
                {
                    Data = ctr.ToString(),
                    Name = columnName,
                    Orderable = orderable,
                    Visible = visible,
                    Searchable = searchable,
                    Search = new DTSearch()
                    {
                        Regex = false,
                        Value = value
                    }

                });
                ctr++;
            }

            DTParameterModel dt = new DTParameterModel()
            {
                Columns = columnList,
                CustomOrder = new List<DTOrder>(),
                Draw = 1,
                ExportChoice = null,
                GMSVenID = 57,
                IsFiltering = true,
                IsMD = false,
                IsMM = false,
                IsRotating = true,
                Length = 20,
                Order = new List<DTOrder>(),
                Rotator = rotatorList,
                Start = 0,
                TableName = "tbl_Bergens",
                Username = "BergensTestUser"
            };
            foreach (var upload in uploadData)
            {
                foreach (var column in dt.Columns)
                {
                    var visible = false;
                    var value = "";
                    if (upload.Contains(column.Name))
                    {
                        visible = true;
                        value = upload[Array.IndexOf(upload, column.Name) + 3];
                    }
                    column.Visible = visible;
                    column.Search = new DTSearch()
                    {
                        Regex = false,
                        Value = value
                    };
                }
                foreach (var rotation in dt.Rotator)
                {
                    if (upload.Contains(rotation.Column))
                    {
                        rotation.Included = true;
                    }
                    else
                    {
                        rotation.Included = false;
                    }
                }

                foreach (var testValue in testInputsAndOutputs)
                {
                    var fixedupload = new string[6]; // Reformat parameters so that the upload script can recognize them
                    for (int i = 0; i < upload.Length; i++)
                    {
                        if (upload[i] == "ItemID") { fixedupload[i] = "Item"; }
                        else if (upload[i] == "FiscalWk") { fixedupload[i] = "Fiscal Wk"; }
                        else { fixedupload[i] = upload[i]; }
                    }

                    var contents = string.Format("{0},{1},{2},Sales Units FY\n{3},{4},{5},{6}",
                        fixedupload[0],
                        fixedupload[1],
                        fixedupload[2],
                        fixedupload[3],
                        fixedupload[4],
                        fixedupload[5],
                        testValue.input);

                    Guid id = Guid.NewGuid();

                    var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")); //+ "\\TestUpload.csv";
                    path = path + String.Format("\\Forecast\\Imports\\TestUpload{0}.csv", id);

                    if (!File.Exists(path))
                    {
                        var fs = File.Create(path);
                        fs.Close();
                    }

                    File.WriteAllText(path, contents);


                    var length = File.ReadAllText(path).Count();

                    int startingValue = 0;
                    if (testValue.input == "" || testValue.input == "null")
                    {
                        startingValue = new DataProvider().GetForecastTable(dt).ToList()[0].SalesUnits_FC;
                    }

                    // controller.ExecuteUploadScript(path);

                    Thread.Sleep(75000);

                    var output = new DataProvider().GetForecastTable(dt).ToList();
                    var outputValue = output[0].SalesUnits_FC;

                    if (testValue.input == "" || testValue.input == "null") // In the case of an empty string input, we need to check that the old value and new value are equal since no upload will occur
                    {
                        Assert.AreEqual(startingValue.ToString(), outputValue.ToString());
                    }
                    else
                    {
                        Assert.AreEqual(testValue.output, outputValue.ToString());
                    }
                }
            }


        }
    }
}

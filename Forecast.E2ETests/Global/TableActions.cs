using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Forecast.E2ETests.Global
{
    public class TableActions
    {
        private readonly IWebDriver webDriver;
        private readonly string tableHeaderClassName = "dataTables_scrollHead";

        public TableActions(IWebDriver webDriver)
        {
            this.webDriver = webDriver;
        }

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="id">The <see cref="string"/> ID name for the header.</param>
        /// <returns>An instance of a <see cref="IWebElement"/> if the header is found. Null if it's not found.</returns>
        public IWebElement FindTableHeaderById(string id)
        {
            try
            {
                return webDriver.FindElement(By.CssSelector($".{tableHeaderClassName} #{id}"));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="id">The <see cref="string"/> ID name for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersById(string id) => FindTableHeadersById(new List<string> { id });

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="id">The <see cref="string"/> ID name for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersById(IEnumerable<string> ids)
        {
            try
            {
                var idSelectors = string.Join(", ", ids.Select(id => $".{tableHeaderClassName} #{id}"));
                return webDriver.FindElements(By.CssSelector(idSelectors));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Find column or group headers based on their HTML element id attribute.
        /// </summary>
        /// <param name="className">The <see cref="string"/> class name for the header.</param>
        /// <returns>An instance of a <see cref="IWebElement"/> if the header is found. Null if it's not found.</returns>
        public IWebElement FindTableHeaderByClassName(string className)
        {
            try
            {
                return webDriver.FindElement(By.CssSelector($".{tableHeaderClassName} .{className}"));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Find column or group headers based on their HTML element class attribute.
        /// </summary>
        /// <param name="className">The <see cref="string"/> class name(s) for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersByClassName(string className) => FindTableHeadersByClassName(new List<string> { className });

        /// <summary>
        /// Find column or group headers based on their HTML element class attribute.
        /// </summary>
        /// <param name="classNames">The <see cref="IEnumerable{T}"/> of <see cref="string"/> class name(s) for the header.</param>
        /// <returns>An <see cref="IReadOnlyCollection{T}{T}"/> of <see cref="IWebElement"/> instances if the header is found.
        /// Null if it's not found.</returns>
        public IReadOnlyCollection<IWebElement> FindTableHeadersByClassName(IEnumerable<string> classNames)
        {
            try
            {
                var classSelectors = string.Join(", ", classNames.Select(className => $".{tableHeaderClassName} .{className}"));
                return webDriver.FindElements(By.CssSelector(classSelectors));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Clicks on a given column group button. If there are no column groups within the provided <see cref="ColumnGroupToggle"/>
        /// then all column groups will be used from the <see cref="ColumnGroup.GetAllIColumnGroupPropertyValues"/> function.
        /// </summary>
        /// <param name="columnGroupInfo">A <see cref="IColumnGroupToggle"/> object representing the column group button to click.</param>
        /// <param name="shouldBeVisible">True if the <see cref="IColumn"/>s within all the <see cref="IColumnGroup"/>s should be
        /// visible after the button click. False if not.</param>
        public void ClickColumnGroupVisibility(IColumnGroupToggle columnGroupInfo, bool shouldBeVisible)
        {
            var columnGroupButton = webDriver.FindElement(By.CssSelector($".{columnGroupInfo.ClassName}"));
            if (columnGroupButton == null)
            {
                throw new Exception($"[TableActions][ClickColumnGroupVisibility] Could not locate button with class {columnGroupInfo.ClassName}");
            }

            columnGroupButton.Click();

            var columnGroups = columnGroupInfo.ColumnGroups.Count > 0 ? columnGroupInfo.ColumnGroups : ColumnGroup.GetAllIColumnGroupPropertyValues();
            waitForGroupHeaders(webDriver, columnGroups, shouldBeVisible);
            waitForColumnHeaders(webDriver, columnGroups, shouldBeVisible);
        }

        /// <summary>
        /// Open the column group Show/Hide menu.
        /// </summary>
        public void OpenColumnGroupVisibilityMenu()
        {
            var columnGroupVisibilityButton = webDriver.FindElement(By.CssSelector(".dt-buttons > .buttons-colvis"));
            columnGroupVisibilityButton.Click();
        }

        /// <summary>
        /// Set a cetain column group as visible or invisible.
        /// </summary>
        /// <param name="columnGroupInfo">A <see cref="IColumnGroupToggle"/> object. See the <see cref="ColumnGroupToggle"/> object for
        /// constructing an instance.</param>
        /// <param name="visible">True if you want to set it as visible. False if you want to set it as invisible.</param>
        public void SetColumnGroupVisibility(IColumnGroupToggle columnGroupInfo, bool visible)
        {
            var columnGroupButton = webDriver.FindElement(By.CssSelector($".{columnGroupInfo.ClassName}"));
            if (columnGroupButton == null)
            {
                throw new Exception($"[TableActions][SetColumnGroupVisibility] Could not locate button with class {columnGroupInfo.ClassName}");
            }

            var isSelected = columnGroupButton.GetAttribute("class").Contains("active");
            if (visible != isSelected)
            {
                columnGroupButton.Click();
            }

            waitForGroupHeaders(webDriver, columnGroupInfo.ColumnGroups, visible);
            waitForColumnHeaders(webDriver, columnGroupInfo.ColumnGroups, visible);
        }

        private void waitForColumnHeaders(IWebDriver webDriver, IEnumerable<IColumnGroup> columnGroups, bool shouldBeVisible)
        {
            var columnHeadersWait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 30));
            columnHeadersWait.Until(driver =>
            {
                try
                {
                    foreach (var columnGroup in columnGroups)
                    {
                        var columnHeaders = FindTableHeadersByClassName(columnGroup.ClassName);
                        if (shouldBeVisible && columnHeaders.Count != columnGroup.Columns.Count)
                        {
                            return false;
                        }
                        else if (!shouldBeVisible && columnHeaders.Count > 0)
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
        }

        private void waitForGroupHeaders(IWebDriver webDriver, IEnumerable<IColumnGroup> columnGroups, bool shouldBeVisible)
        {
            var headerWait = new WebDriverWait(webDriver, new TimeSpan(0, 0, 30));
            headerWait.Until(driver =>
            {
                try
                {
                    var columnGroupHeaderIds = columnGroups.Select(columnGroup => columnGroup.IdName);
                    var columnGroupElements = FindTableHeadersById(columnGroupHeaderIds);
                    if (columnGroupElements == null)
                    {
                        return !shouldBeVisible || false;
                    }

                    if ((shouldBeVisible && columnGroupElements.Count != columnGroups.Count()) || (!shouldBeVisible && columnGroupElements.Count > 0))
                    {
                        return false;
                    }

                    var elementIds = columnGroupElements.Select(element => element.GetDomProperty("id")).Distinct();
                    var idCount = elementIds.Where(id => columnGroupHeaderIds.Contains(id)).Count();
                    if (shouldBeVisible)
                    {
                        return idCount == columnGroupHeaderIds.Count();
                    }
                    else
                    {
                        return idCount == 0;
                    }
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
        }
    }

    public interface IColumnGroup
    {
        string ClassName { get; }
        List<IColumn> Columns { get; }
        string DisplayName { get; }
        string IdName { get; }
        IColumnGroup LimitColumns(Func<IColumn, bool> filter);
    }

    public class ColumnGroup : IColumnGroup
    {
        private ColumnGroup() { }
        private ColumnGroup(string idAndClassName, string displayName, List<IColumn> columns)
        {
            IdName = idAndClassName;
            ClassName = idAndClassName;
            DisplayName = displayName;
            Columns = columns;
        }

        private ColumnGroup(string idName, string className, string displayName, List<IColumn> columns)
        {
            IdName = idName;
            ClassName = className;
            DisplayName = displayName;
            Columns = columns;
        }

        public static IColumnGroup Asp => new ColumnGroup("asp", "ASP", new List<IColumn> { ForecastColumn.AspLy, ForecastColumn.AspTy, ForecastColumn.AspFyAsp, ForecastColumn.AspVar });

        public static IColumnGroup Comments => new ColumnGroup("comments", "Comments", new List<IColumn> { ForecastColumn.MMComments, ForecastColumn.VendorComments });

        public static IColumnGroup Cost => new ColumnGroup("cost", "Cost", new List<IColumn> { ForecastColumn.CostLy, ForecastColumn.CostTy, ForecastColumn.CostFy, ForecastColumn.CostVar });

        public static IColumnGroup Forecast => new ColumnGroup("forecast_group", "Forecast Comparison", new List<IColumn>
        {
            ForecastColumn.ForecastDemandLinkFySalesDollars,
            ForecastColumn.ForecastLowesFySalesDollars,
            ForecastColumn.ForecastVendorFySalesDollars,
            ForecastColumn.ForecastDemandLinkFyUnits,
            ForecastColumn.ForecastLowesFyUnits,
            ForecastColumn.ForecastVendorFyUnits,
            ForecastColumn.ForecastDemandLinkFySalesDollarsVar,
            ForecastColumn.ForecastLowesFySalesDollarsVar,
            ForecastColumn.ForecastVendorFySalesDollarsVar,
            ForecastColumn.ForecastDemandLinkFyUnitsVar,
            ForecastColumn.ForecastLowesFyUnitsVar,
            ForecastColumn.ForecastVendorFyUnitsVar
        });

        public static IColumnGroup MarginPercent => new ColumnGroup("margin_perc", "Margin %", new List<IColumn>
        {
            ForecastColumn.MarginPercentLy,
            ForecastColumn.MarginPercentTy,
            ForecastColumn.MarginPercentFyAspDollar,
            ForecastColumn.MarginPercentVar,
            ForecastColumn.MarginPercentFyRetailDollar
        });

        public static IColumnGroup MarginDollar => new ColumnGroup("margin_dollars", "Margin $", new List<IColumn>
        {
            ForecastColumn.MarginDollarsLy,
            ForecastColumn.MarginDollarsTy,
            ForecastColumn.MarginDollarsFyRetailDollarVar,
            ForecastColumn.MarginDollarsFyAspDollar,
            ForecastColumn.MarginDollarsFyRetailDollar
        });

        public static IColumnGroup Mp => new ColumnGroup("mp_sales_and_margin", "MP", new List<IColumn>
        {
            ForecastColumn.MpSalesDollarsRetailLy,
            ForecastColumn.MpSalesDollarsRetailTy,
            ForecastColumn.MpMarginDollarsRetailLy,
            ForecastColumn.MpMarginDollarsRetailTy,
            ForecastColumn.MpMarginDollarsVarRetail
        });

        public static IColumnGroup PriceSensitivity => new ColumnGroup("price_sens", "Price Sensitivity", new List<IColumn>
        {
            ForecastColumn.PriceSensitivityPercent,
            ForecastColumn.PriceSensitivityImpact
        });

        public static IColumnGroup ReceiptDollar => new ColumnGroup("rec_dollars", "Receipt Dollars", new List<IColumn> { ForecastColumn.ReceiptDollarsLy, ForecastColumn.ReceiptDollarsTy });

        public static IColumnGroup ReceiptUnits => new ColumnGroup("rec_units", "Receipt Units", new List<IColumn> { ForecastColumn.ReceiptUnitsLy, ForecastColumn.ReceiptUnitsTy });

        public static IColumnGroup RetailPrice => new ColumnGroup("retail_price", "Retail Price", new List<IColumn>
        {
            ForecastColumn.RetailPriceLy,
            ForecastColumn.RetailPriceTy,
            ForecastColumn.RetailPriceFyRetailPrice,
            ForecastColumn.RetailPriceVar
        });

        public static IColumnGroup SalesDollars => new ColumnGroup("sales_dollars", "Sales Dollars", new List<IColumn>
        {
            ForecastColumn.SalesDollars2ly,
            ForecastColumn.SalesDollarsLy,
            ForecastColumn.SalesDollarsTy,
            ForecastColumn.SalesDollarsFyAspDollar,
            ForecastColumn.SalesDollarsVar,
            ForecastColumn.SalesDollarsFr,
            ForecastColumn.SalesDollarsCagr
        });

        public static IColumnGroup SalesUnits => new ColumnGroup("sales_units", "Sales Units", new List<IColumn>
        {
            ForecastColumn.SalesUnits2ly,
            ForecastColumn.SalesUnitsLy,
            ForecastColumn.SalesUnitsTy,
            ForecastColumn.SalesUnitsFyUnits,
            ForecastColumn.SalesUnitsVar
        });

        public static IColumnGroup SellThrough => new ColumnGroup("sell_thru", "Sell Thru", new List<IColumn> { ForecastColumn.SellThruLy, ForecastColumn.SellThruTy });

        public static IColumnGroup Turns => new ColumnGroup("turns", "Turns", new List<IColumn>
        {
            ForecastColumn.TurnsLy,
            ForecastColumn.TurnsTy,
            ForecastColumn.TurnsFy,
            ForecastColumn.TurnsVar
        });

        public string ClassName { get; }
        public List<IColumn> Columns { get; private set; }
        public string DisplayName { get; }
        public string IdName { get; }

        public IColumnGroup LimitColumns(Func<IColumn, bool> filter)
        {
            var newColumns = Columns.Where(filter).ToList();
            Columns = newColumns;
            return this;
        }

        /// <summary>
        /// Get an <see cref="IEnumerable{T}"/> of <see cref="IColumnGroup"/> objects from each of the
        /// <see cref="static"/> column group properties.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IColumnGroup"/> objects.</returns>
        public static IEnumerable<IColumnGroup> GetAllIColumnGroupPropertyValues() => ColumnGroupHelpers.GetAllStaticPropertyValues<IColumnGroup>(new ColumnGroup());

        public static IColumnGroup GetColumnGroupByPropertyName(string propertyName)
        {
            var properties = typeof(ColumnGroup).GetProperties(BindingFlags.Public | BindingFlags.Static);
            var existingProperty = properties.Where(property => property.Name.Equals(propertyName)).FirstOrDefault();

            if (existingProperty == null)
            {
                throw new Exception($"[ColumnGroup][GetColumnGroupByPropertyName] Cannot find property by the name of {propertyName}.");
            }

            var columnGroup = new ColumnGroup();
            return existingProperty.GetValue(columnGroup, null) as IColumnGroup;
        }
    }

    public interface IColumn
    {
        string DisplayName { get; }
        string FieldName { get; }
    }

    public class ForecastColumn : IColumn
    {
        private ForecastColumn() { }
        private ForecastColumn(string displayName, string fieldName)
        {
            DisplayName = displayName;
            FieldName = fieldName;
        }

        public static IColumn ForecastID => new ForecastColumn("ForecastID", "ForecastID");
        public static IColumn VendorDesc => new ForecastColumn("VendorDesc", "VendorDesc");
        public static IColumn ItemID => new ForecastColumn("ItemID", "ItemID");
        public static IColumn FiscalWk => new ForecastColumn("FiscalWk", "FiscalWk");
        public static IColumn FiscalMo => new ForecastColumn("FiscalMo", "FiscalMo");
        public static IColumn FiscalQtr => new ForecastColumn("FiscalQtr", "FiscalQtr");
        public static IColumn MD => new ForecastColumn("MD", "MD");
        public static IColumn MM => new ForecastColumn("MM", "MM");
        public static IColumn Region => new ForecastColumn("Region", "Region");
        public static IColumn District => new ForecastColumn("District", "District");
        public static IColumn Patch => new ForecastColumn("Patch", "Patch");
        public static IColumn ParentID => new ForecastColumn("ParentID", "ParentID");
        public static IColumn ProdGrpID => new ForecastColumn("ProdGrpID", "ProdGrpID");
        public static IColumn AssrtID => new ForecastColumn("AssrtID", "AssrtID");
        public static IColumn ItemDesc => new ForecastColumn("ItemDesc", "ItemDesc");
        public static IColumn ItemConcat => new ForecastColumn("ItemConcat", "ItemConcat");
        public static IColumn AssrtDesc => new ForecastColumn("AssrtDesc", "AssrtDesc");
        public static IColumn AssrtConcat => new ForecastColumn("AssrtConcat", "AssrtConcat");
        public static IColumn ParentDesc => new ForecastColumn("ParentDesc", "ParentDesc");
        public static IColumn ProdGrpDesc => new ForecastColumn("ProdGrpDesc", "ProdGrpDesc");
        public static IColumn ProdGrpConcat => new ForecastColumn("ProdGrpConcat", "ProdGrpConcat");
        public static IColumn SalesUnitsTy => new ForecastColumn("Prev52", "SalesUnits_TY");
        public static IColumn SalesUnitsLy => new ForecastColumn("Prev52-1 YR Ago", "SalesUnits_LY");
        public static IColumn SalesUnits2ly => new ForecastColumn("Prev52-2 YR Ago", "SalesUnits_2LY");
        public static IColumn SalesUnitsFyUnits => new ForecastColumn("FY Units", "SalesUnits_FC");
        public static IColumn SalesUnitsVar => new ForecastColumn("Var", "SalesUnits_Var");
        public static IColumn SalesDollars2ly => new ForecastColumn("Prev52-2 YR Ago", "SalesDollars_2LY");
        public static IColumn SalesDollarsLy => new ForecastColumn("Prev52-1 YR Ago", "SalesDollars_LY");
        public static IColumn SalesDollarsTy => new ForecastColumn("Prev52", "SalesDollars_TY");
        public static IColumn SalesDollarsFyAspDollar => new ForecastColumn("FY ASP $", "SalesDollars_Curr");
        public static IColumn SalesDollarsVar => new ForecastColumn("Var", "SalesDollars_Var");
        public static IColumn SalesDollarsFr => new ForecastColumn("FR", "SalesDollars_FR_FC");
        public static IColumn SalesDollarsCagr => new ForecastColumn("CAGR", "CAGR");
        public static IColumn AspTy => new ForecastColumn("Prev52", "ASP_TY");
        public static IColumn AspLy => new ForecastColumn("Prev52-1 YR Ago", "ASP_LY");
        public static IColumn AspFyAsp => new ForecastColumn("FY ASP", "ASP_FC");
        public static IColumn AspVar => new ForecastColumn("Var", "ASP_Var");
        public static IColumn RetailPriceLy => new ForecastColumn("Prev52-1 YR Ago", "RetailPrice_LY");
        public static IColumn RetailPriceTy => new ForecastColumn("Prev52", "RetailPrice_TY");
        public static IColumn RetailPriceFyRetailPrice => new ForecastColumn("FY Retail Price", "RetailPrice_FC");
        public static IColumn RetailPriceVar => new ForecastColumn("Var", "RetailPrice_Var");
        public static IColumn MpSalesDollarsRetailTy => new ForecastColumn("Sales $ Retail Prev52", "SalesDollars_FR_TY");
        public static IColumn MpSalesDollarsRetailLy => new ForecastColumn("Sales $ Retail Prev52-1 YR Ago", "SalesDollars_FR_LY");
        public static IColumn MpMarginDollarsRetailTy => new ForecastColumn("Margin $ Retail Prev52", "MarginDollars_FR_TY");
        public static IColumn MpMarginDollarsRetailLy => new ForecastColumn("Margin $ Retail Prev52-1 YR Ago", "MarginDollars_FR_LY");
        public static IColumn MpMarginDollarsVarRetail => new ForecastColumn("Margin Var Retail", "MarginDollars_FR_Var");
        public static IColumn CostLy => new ForecastColumn("Prev52-1 YR Ago", "Cost_LY");
        public static IColumn CostTy => new ForecastColumn("Prev52", "Cost_TY");
        public static IColumn CostFy => new ForecastColumn("FY", "Cost_FC");
        public static IColumn CostVar => new ForecastColumn("Var", "Cost_Var");
        public static IColumn MarginDollarsTy => new ForecastColumn("Prev52", "Margin_Dollars_TY");
        public static IColumn MarginDollarsLy => new ForecastColumn("Prev52-1 YR Ago", "Margin_Dollars_LY");
        public static IColumn MarginDollarsFyAspDollar => new ForecastColumn("FY ASP $", "Margin_Dollars_Curr");
        public static IColumn MarginDollarsFyRetailDollar => new ForecastColumn("FY Retail $", "Margin_Dollars_FR");
        public static IColumn MarginDollarsFyRetailDollarVar => new ForecastColumn("FY Retail $ Var", "Margin_Dollars_Var_Curr");
        public static IColumn MarginPercentTy => new ForecastColumn("Prev52", "Margin_Percent_TY");
        public static IColumn MarginPercentLy => new ForecastColumn("Prev52-1 YR Ago", "Margin_Percent_LY");
        public static IColumn MarginPercentFyAspDollar => new ForecastColumn("FY ASP $", "Margin_Percent_Curr");
        public static IColumn MarginPercentVar => new ForecastColumn("Var", "Margin_Percent_Var");
        public static IColumn MarginPercentFyRetailDollar => new ForecastColumn("FY Retail $", "Margin_Percent_FR");
        public static IColumn TurnsLy => new ForecastColumn("Prev52-1 YR Ago", "Turns_LY");
        public static IColumn TurnsTy => new ForecastColumn("Prev52", "Turns_TY");
        public static IColumn TurnsFy => new ForecastColumn("FY", "Turns_FC");
        public static IColumn TurnsVar => new ForecastColumn("Var", "Turns_Var");
        public static IColumn SellThruLy => new ForecastColumn("Prev52-1 YR Ago", "SellThru_LY");
        public static IColumn SellThruTy => new ForecastColumn("Prev52", "SellThru_TY");
        public static IColumn ReceiptUnitsLy => new ForecastColumn("Prev52-1 YR Ago", "ReceiptUnits_LY");
        public static IColumn ReceiptUnitsTy => new ForecastColumn("Prev52", "ReceiptUnits_TY");
        public static IColumn ReceiptDollarsLy => new ForecastColumn("Prev52-1 YR Ago", "ReceiptDollars_LY");
        public static IColumn ReceiptDollarsTy => new ForecastColumn("Prev52", "ReceiptDollars_TY");
        public static IColumn ForecastDemandLinkFySalesDollars => new ForecastColumn("DemandLink (FY Sales $)", "Dollars_FC_DL");
        public static IColumn ForecastLowesFySalesDollars => new ForecastColumn("Lowes (FY Sales $)", "Dollars_FC_LOW");
        public static IColumn ForecastVendorFySalesDollars => new ForecastColumn("Vendor (FY Sales $)", "Dollars_FC_Vendor");
        public static IColumn ForecastDemandLinkFyUnits => new ForecastColumn("DemandLink (FY Units)", "Units_FC_DL");
        public static IColumn ForecastLowesFyUnits => new ForecastColumn("Lowes (FY Units)", "Units_FC_LOW");
        public static IColumn ForecastVendorFyUnits => new ForecastColumn("Vendor (FY Units)", "Units_FC_Vendor");
        public static IColumn ForecastDemandLinkFySalesDollarsVar => new ForecastColumn("DemandLink (FY Sales $) Var", "Dollars_FC_DL_Var");
        public static IColumn ForecastLowesFySalesDollarsVar => new ForecastColumn("Lowes (FY Sales $) Var", "Dollars_FC_LOW_Var");
        public static IColumn ForecastVendorFySalesDollarsVar => new ForecastColumn("Vendor (FY Sales $) Var", "Dollars_FC_Vendor_Var");
        public static IColumn ForecastDemandLinkFyUnitsVar => new ForecastColumn("DemandLink (FY Units) Var", "Units_FC_DL_Var");
        public static IColumn ForecastLowesFyUnitsVar => new ForecastColumn("Lowes (FY Units) Var", "Units_FC_LOW_Var");
        public static IColumn ForecastVendorFyUnitsVar => new ForecastColumn("Vendor (FY Units) Var", "Units_FC_Vendor_Var");
        public static IColumn PriceSensitivityImpact => new ForecastColumn("Impact Sensitivity", "PriceSensitivityImpact");
        public static IColumn PriceSensitivityPercent => new ForecastColumn("Percent", "PriceSensitivityPercent");
        public static IColumn VBUPercent => new ForecastColumn("VBUPercent", "VBUPercent");
        public static IColumn VendorComments => new ForecastColumn("Vendor Comments", "Vendor_Comments");
        public static IColumn MMComments => new ForecastColumn("MM Comments", "MM_Comments");

        public string DisplayName { get; }
        public string FieldName { get; }
    }

    public interface IColumnGroupToggle
    {
        string ClassName { get; }
        List<IColumnGroup> ColumnGroups { get; }
        List<IColumn> Columns { get; }
        string DisplayName { get; }
    }

    public class ColumnGroupToggle : IColumnGroupToggle
    {
        private ColumnGroupToggle() { }
        private ColumnGroupToggle(string className, string name, List<IColumnGroup> columnGroups)
        {
            ClassName = className;
            DisplayName = name;
            ColumnGroups = columnGroups;
            Columns = new List<IColumn>();
            columnGroups.ForEach(columnGroup => columnGroup.Columns.ForEach(column => Columns.Add(column)));
        }

        public static IColumnGroupToggle Asp => createColumnGroup("asp-group", "ASP", ColumnGroup.Asp);

        public static IColumnGroupToggle Comments => createColumnGroup("comments-group", "COMMENTS", ColumnGroup.Comments);

        public static IColumnGroupToggle Cost => createColumnGroup("cost-group", "COST", ColumnGroup.Cost);

        public static IColumnGroupToggle Default => new ColumnGroupToggle("button-default", "DEFAULT", new List<IColumnGroup>
            {
                ColumnGroup.SalesDollars.LimitColumns(column =>
                {
                    var name = column.DisplayName;
                    return name.Equals("Prev52") || name.Equals("FY ASP $") || name.Equals("Var");
                }),
                ColumnGroup.SalesUnits.LimitColumns(column =>
                {
                    var name = column.DisplayName;
                    return name.Equals("Prev52") || name.Equals("FY Units") || name.Equals("Var");
                }),
                ColumnGroup.RetailPrice.LimitColumns(column =>
                {
                    var name = column.DisplayName;
                    return name.Equals("Prev52") || name.Equals("FY Retail Price") || name.Equals("Var");
                }),
                ColumnGroup.Comments
            });

        public static IColumnGroupToggle Forecast => createColumnGroup("forecast-group", "FORECAST", ColumnGroup.Forecast);

        public static IColumnGroupToggle HideAll => new ColumnGroupToggle("button-hideAll", "HIDE ALL", new List<IColumnGroup>());

        public static IColumnGroupToggle MarginPercent => createColumnGroup("margin-percent-group", "MARGIN %", ColumnGroup.MarginPercent);

        public static IColumnGroupToggle MarginDollar => createColumnGroup("margin-dollar-group", "MARGIN $", ColumnGroup.MarginDollar);

        public static IColumnGroupToggle Mp => createColumnGroup("mp-sales-and-margin-group", "MP", ColumnGroup.Mp);

        public static IColumnGroupToggle PriceSensitivity => createColumnGroup("price-sensitivity-group", "PRICE SENSITIVITY", ColumnGroup.PriceSensitivity);

        public static IColumnGroupToggle ReceiptDollar => createColumnGroup("receipt-dollar-group", "RECEIPT $", ColumnGroup.ReceiptDollar);

        public static IColumnGroupToggle ReceiptUnits => createColumnGroup("receipt-units-group", "RECEIPT UNITS", ColumnGroup.ReceiptUnits);

        public static IColumnGroupToggle RetailPrice => createColumnGroup("retail-price-group", "RETAIL $", ColumnGroup.RetailPrice);

        public static IColumnGroupToggle SalesDollars => createColumnGroup("sales-dollar-group", "SALES $", ColumnGroup.SalesDollars);

        public static IColumnGroupToggle SalesUnits => createColumnGroup("sales-units-group", "SALES UNITS", ColumnGroup.SalesUnits);

        public static IColumnGroupToggle SellThrough => createColumnGroup("sell-thru-group", "SELL-THROUGH", ColumnGroup.SellThrough);

        public static IColumnGroupToggle ShowAll => new ColumnGroupToggle("button-showAll", "SHOW ALL", ColumnGroup.GetAllIColumnGroupPropertyValues().ToList());

        public static IColumnGroupToggle Turns => createColumnGroup("turns-group", "TURNS", ColumnGroup.Turns);

        public string ClassName { get; }
        public List<IColumn> Columns { get; }
        public List<IColumnGroup> ColumnGroups { get; }
        public string DisplayName { get; }

        /// <summary>
        /// Get an <see cref="IEnumerable{T}"/> of <see cref="IColumnGroupToggle"/> objects from each of the
        /// <see cref="static"/> column group toggle properties.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IColumnGroupToggle"/> objects.</returns>
        public static IEnumerable<IColumnGroupToggle> GetAllIColumnGroupTogglePropertyValues() => ColumnGroupHelpers.GetAllStaticPropertyValues<IColumnGroupToggle>(new ColumnGroupToggle());

        /// <summary>
        /// Get a column group toggle value based on the <see cref="ColumnGroupToggle"/> property name provided.
        /// </summary>
        /// <param name="propertyName">A <see cref="string"/> property <see cref="static"/> name that exists in the
        /// <see cref="ColumnGroupToggle"/> <see cref="class"/>.</param>
        /// <returns>A <see cref="IColumnGroupToggle"/> object.</returns>
        public static IColumnGroupToggle GetColumnGroupByPropertyName(string propertyName)
        {
            var properties = typeof(ColumnGroupToggle).GetProperties(BindingFlags.Public | BindingFlags.Static);
            var existingProperty = properties.Where(property => property.Name.Equals(propertyName)).FirstOrDefault();

            if (existingProperty == null)
            {
                throw new Exception($"[ColumnGroupVisibileActionInfo][GetColumnGroupByPropertyName] Cannot find property by the name of {propertyName}.");
            }

            var columnGroup = new ColumnGroupToggle();
            return existingProperty.GetValue(columnGroup, null) as IColumnGroupToggle;
        }

        private static ColumnGroupToggle createColumnGroup(string className, string displayName, IColumnGroup columnGroup) => new ColumnGroupToggle(className, displayName, new List<IColumnGroup> { columnGroup });
    }

    public class ColumnGroupHelpers
    {
        public static IEnumerable<T> GetAllStaticPropertyValues<T>(object value) where T : class
        {
            var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static);
            var validProperties = properties.Where(property => property.PropertyType == typeof(T)).ToList();

            return validProperties.Select(property => property.GetValue(value) as T).ToList();
        }
    }
}

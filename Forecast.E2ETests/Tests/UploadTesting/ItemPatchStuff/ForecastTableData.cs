using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.E2ETests.Tests.UploadTesting.ItemPatch;

namespace Forecast.E2ETests.Global.UploadTesting
{
    class ForecastTableData
    {
        public List<ForecastColumnInfo> forecastColumns = new List<ForecastColumnInfo>();
        public List<ForecastColumnInfo> unfrozenForecastColumns = new List<ForecastColumnInfo>();
        public string tableNickname;

        public ForecastTableData(string tableNickname)
        {
            this.tableNickname = tableNickname;

            forecastColumns.Add(new ForecastColumnInfo("patch", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("vendordesc", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("gmsvenid", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("itemid", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("itemdesc", "", "", "itemInfo"));
            forecastColumns.Add(new ForecastColumnInfo("itemconcat", "", "", "concat"));
            forecastColumns.Add(new ForecastColumnInfo("parentid", "", "", "itemInfo"));
            forecastColumns.Add(new ForecastColumnInfo("parentdesc", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("assrtid", "", "", "itemInfo"));
            forecastColumns.Add(new ForecastColumnInfo("assrtdesc", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("assrtconcat", "", "", "concat"));
            forecastColumns.Add(new ForecastColumnInfo("prodgrpid", "", "", "itemInfo"));
            forecastColumns.Add(new ForecastColumnInfo("prodgrpdesc", "", "", ""));
            forecastColumns.Add(new ForecastColumnInfo("prodgrpconcat", "", "", "concat"));
            forecastColumns.Add(new ForecastColumnInfo("salesunits_fc", "SUM", "", "units_fc_low"));
            forecastColumns.Add(new ForecastColumnInfo("units_fc_vendor", "SUM", "", "units_fc_low"));
            forecastColumns.Add(new ForecastColumnInfo("units_fc_low", "SUM", "", "units_fc_low"));
            forecastColumns.Add(new ForecastColumnInfo("retailprice_ly", "AVG", "", "retail_LY_Original"));
            forecastColumns.Add(new ForecastColumnInfo("retailprice_ty", "AVG", "", "retail_TY_FC_Original"));
            forecastColumns.Add(new ForecastColumnInfo("retailprice_fc", "AVG", "", "retail_TY_FC_Original"));
            forecastColumns.Add(new ForecastColumnInfo("cost_ly", "AVG", "", "cost_LY_Original"));
            forecastColumns.Add(new ForecastColumnInfo("cost_ty", "AVG", "", "cost_TY_FC_Original"));
            forecastColumns.Add(new ForecastColumnInfo("cost_fc", "AVG", "", "cost_TY_FC_Original"));

            //NEED TO ADD MD AND DESCRIPTIONS
        }

        public ForecastColumnInfo GetForecastColumn(string column)
        {
            for (int i = 0; i < forecastColumns.Count; i++)
            {
                if (forecastColumns.ElementAt(i).columnName == column)
                {
                    return forecastColumns.ElementAt(i);
                }
            }

            throw new Exception("Column not found. Try new column");
        }
    }
}


namespace Forecast.Models
{
    public class SummaryTotals
    {
        public string ForecastDef { get; set; }
        public string Actual { get; set; }
        public string FC { get; set; }
        public decimal Var { get; set; }
    }

    public class UpdatedDates
    {
        public string DateMin { get; set; }
        public string DateMax { get; set; }
    }
}
using System.Web;
using System.Web.Optimization;

namespace Forecast
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*", 
                        "~/Scripts/jquery-{version}.intellisense.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));


            //Additional sections.  Materialize styling and Javascript as well as DataTables.
            bundles.Add(new ScriptBundle("~/bundles/materialize").Include(
                        "~/Scripts/materialize.min.js",
                        "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(                       
                        //"~/Content/bootstrap.min.css",
                        "~/Content/materialize.min.css",
                        "~/Content/site.css"
                        ));

            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                        "~/Scripts/Datatables/Editor-1.6.5/js/*.js"));

            bundles.Add(new StyleBundle("~/Content/DataTables").Include(
                        "~/Scripts/Datatables/Editor-1.6.5/css/*.css"));

            //Custom Stuff
            bundles.Add(new ScriptBundle("~/bundles/custom").Include(
                        "~/Scripts/Forecast.js"));

        }
    }
}

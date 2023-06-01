using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Forecast
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
        }
    }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public class CheckSessionOutAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(System.Web.Mvc.ActionExecutingContext filterContext)
		{
			var context = filterContext.HttpContext;
			if (context.Session != null)
			{
				if (context.Session.IsNewSession)
				{
					string sessionCookie = context.Request.Headers["Cookie"];
					string cookie = sessionCookie;
					if ((sessionCookie != null) && (sessionCookie.IndexOf(".__RequestVerificationToken") >= 0))
					{
						System.Web.Security.FormsAuthentication.SignOut();
						filterContext.Result = new RedirectResult("~/Home/Index");
						return;
					}
				}
			}
			base.OnActionExecuting(filterContext);
		}
	}
}

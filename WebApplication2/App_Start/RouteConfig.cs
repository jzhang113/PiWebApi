using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplication2
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Intensity",
                url: "API/Intensity/{building}",
                defaults: new { controller = "API", action = "Intensity", building = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Trend",
                url: "API/Trend/{name}",
                defaults: new { controller = "API", action = "Trend", name = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}

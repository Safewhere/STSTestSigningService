﻿#region

using System.Web.Http;
using Kombit.Samples.Common;
using Owin;

#endregion

namespace Kombit.Samples.STSTestSigningService
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional}
                );

            appBuilder.UseWebApi(config);

            config.Formatters.Add(new BrowserJsonFormatter());
        }
    }
}
#region

using System.Web.Http;
using Kombit.Samples.STSTestSigningService;
using Swashbuckle.Application;
using WebActivatorEx;

#endregion

[assembly: PreApplicationStartMethod(typeof (SwaggerConfig), "Register")]

namespace Kombit.Samples.STSTestSigningService
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration.Routes.IgnoreRoute("api-docs", "content/api-docs.json");
            GlobalConfiguration.Configuration
                .EnableSwagger("content/api-docs.json", c =>
                {
                    c.Schemes(new[] {"https"});

                    c.SingleApiVersion("", "JsonAPI");

                    c.UseFullTypeNameInSchemaIds();
                })
                .EnableSwaggerUi(c =>
                {
                    c.DocExpansion(DocExpansion.List);
                    c.InjectJavaScript(typeof (SwaggerConfig).Assembly,
                        "Kombit.Samples.STSTestSigningService.Scripts.swagger-ui.js");
                });
        }
    }
}
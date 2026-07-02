using System.Web.Http;
using Swashbuckle.Application;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(Bookstore.Api.SwaggerConfig), "Register")]

namespace Bookstore.Api
{
    /// <summary>
    /// Swagger (Swashbuckle) wiring: interactive API docs at /swagger.
    /// </summary>
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Bookstore API")
                     .Description("CRUD over the XML-backed bookstore catalog, keyed by ISBN.");
                })
                .EnableSwaggerUi();
        }
    }
}

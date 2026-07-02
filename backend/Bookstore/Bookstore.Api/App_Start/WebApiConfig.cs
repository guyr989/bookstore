using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json.Serialization;

namespace Bookstore.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Allow the Angular dev server (ng serve) to call the API.
            config.EnableCors(new EnableCorsAttribute(
                origins: "http://localhost:4200",
                headers: "*",
                methods: "*"));

            // camelCase JSON (isbn, authorsDisplay, ...) — the JS-side convention.
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();

            // Attribute routing ([RoutePrefix]/[Route] on controllers).
            config.MapHttpAttributeRoutes();

            // Convention fallback: /api/{controller}/{id}.
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });
        }
    }
}

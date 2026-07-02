using System.Web.Http;
using System.Web.Http.Cors;

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

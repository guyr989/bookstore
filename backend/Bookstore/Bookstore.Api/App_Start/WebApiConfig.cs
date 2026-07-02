using System.Web.Http;

namespace Bookstore.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
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

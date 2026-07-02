using System;
using System.Configuration;
using System.IO;
using System.Web.Hosting;
using Bookstore.Core.Persistence;

namespace Bookstore.Api
{
    /// <summary>
    /// Composition root helpers: the host (not the domain) decides which XML
    /// file backs the store. The path comes from Web.config appSettings
    /// ("BookstoreXmlPath") and differs per environment via config transforms.
    /// </summary>
    public static class AppConfig
    {
        public static string xmlPath()
        {
            var raw = ConfigurationManager.AppSettings["BookstoreXmlPath"];
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "Missing appSetting 'BookstoreXmlPath' in Web.config.");

            if (raw.StartsWith("~"))
            {
                // Web-hosted: map "~/App_Data/..." to the site root.
                var mapped = HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath(raw)
                    : raw.Replace("~", AppDomain.CurrentDomain.BaseDirectory)
                         .Replace('/', Path.DirectorySeparatorChar);
                return mapped;
            }

            return Path.IsPathRooted(raw)
                ? raw
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, raw);
        }

        public static XmlBookRepository repo()
        {
            return new XmlBookRepository(xmlPath());
        }
    }
}

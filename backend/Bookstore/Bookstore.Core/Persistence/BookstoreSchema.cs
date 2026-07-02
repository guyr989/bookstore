using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace Bookstore.Core.Persistence
{
    /// <summary>
    /// Provides the compiled XSD used to validate the bookstore XML on load.
    /// The schema is shipped as an embedded resource inside this assembly, so
    /// validation never depends on where the app is run from or on locating a
    /// loose .xsd file on disk. Built once and reused (XmlSchemaSet is
    /// thread-safe for read after Compile()).
    /// </summary>
    internal static class BookstoreSchema
    {
        private static readonly XmlSchemaSet _schemaSet = Build();

        public static XmlSchemaSet SchemaSet
        {
            get { return _schemaSet; }
        }

        private static XmlSchemaSet Build()
        {
            var assembly = typeof(BookstoreSchema).Assembly;

            // Resolve by suffix so we don't hard-code the manifest name prefix.
            var resourceName = assembly.GetManifestResourceNames()
                                       .Single(n => n.EndsWith("bookstore.xsd"));

            var set = new XmlSchemaSet();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = XmlReader.Create(stream))
            {
                set.Add(null, reader);
            }
            set.Compile();
            return set;
        }
    }
}

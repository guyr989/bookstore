using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Bookstore.Core.Models;
using Bookstore.Core.Validation;

namespace Bookstore.Core.Persistence
{
    public class XmlBookRepository
    {
        private readonly string _path;

        public XmlBookRepository(string path)
        {
            _path = path;
        }

        public IList<Book> GetAll()
        {
            var doc = LoadDocument();

            return doc.Root
                      .Elements("book")
                      .Select(MapBook)
                      .ToList();
        }

        public Book GetByIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn)) return null;

            var doc = LoadDocument();
            var bookElement = doc.Root
                                 .Elements("book")
                                 .FirstOrDefault(b => (string)b.Element("isbn") == isbn);

            return bookElement != null ? MapBook(bookElement) : null;
        }

        public void Add(Book book)
        {
            BookValidator.Validate(book);

            var doc = LoadDocument();

            var duplicate = doc.Root
                               .Elements("book")
                               .Any(b => (string)b.Element("isbn") == book.Isbn);
            if (duplicate)
                throw new InvalidOperationException(
                    "A book with ISBN '" + book.Isbn + "' already exists.");

            doc.Root.Add(BuildBookElement(book));
            doc.Save(_path);
        }

        // Returns true if a book with this ISBN existed and was replaced;
        // false if no such book was found (a normal, non-exceptional outcome).
        public bool Edit(Book book)
        {
            BookValidator.Validate(book);

            var doc = LoadDocument();
            var existing = doc.Root
                              .Elements("book")
                              .FirstOrDefault(b => (string)b.Element("isbn") == book.Isbn);

            if (existing == null) return false;

            existing.ReplaceWith(BuildBookElement(book));
            doc.Save(_path);
            return true;
        }

        // Returns true if a book with this ISBN existed and was removed;
        // false if no such book was found.
        public bool Delete(string isbn)
        {
            var doc = LoadDocument();
            var existing = doc.Root
                              .Elements("book")
                              .FirstOrDefault(b => (string)b.Element("isbn") == isbn);

            if (existing == null) return false;

            existing.Remove();
            doc.Save(_path);
            return true;
        }

        // Loads the XML file and validates it against the embedded XSD. A file
        // that does not match the schema (e.g. hand-corrupted) throws
        // XmlSchemaValidationException instead of silently misbehaving.
        private XDocument LoadDocument()
        {
            var doc = XDocument.Load(_path);
            doc.Validate(BookstoreSchema.SchemaSet, (sender, e) => { throw e.Exception; });
            return doc;
        }

        private static XElement BuildBookElement(Book book)
        {
            return new XElement("book",
                new XAttribute("category", book.Category),
                book.Cover != null ? new XAttribute("cover", book.Cover) : null,
                new XElement("isbn", book.Isbn),
                new XElement("title", new XAttribute("lang", book.Language), book.Title),
                book.Authors.Select(a => new XElement("author", a)),
                new XElement("year", book.Year.ToString(CultureInfo.InvariantCulture)),
                new XElement("price", book.Price.ToString(CultureInfo.InvariantCulture)));
        }

        private static Book MapBook(XElement el)
        {
            var title = el.Element("title");

            return new Book
            {
                Isbn = (string)el.Element("isbn"),
                Title = (string)title,
                Language = (string)title.Attribute("lang"),
                Authors = el.Elements("author").Select(a => a.Value).ToList(),
                Year = int.Parse((string)el.Element("year"), CultureInfo.InvariantCulture),
                Price = decimal.Parse((string)el.Element("price"), CultureInfo.InvariantCulture),
                Category = (string)el.Attribute("category"),
                Cover = (string)el.Attribute("cover")
            };
        }
    }
}

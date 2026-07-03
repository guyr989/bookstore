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
        private readonly FileVersionStore _versions;

        public XmlBookRepository(string path) : this(path, null) { }

        // With a version store, every successful write snapshots the new
        // file state so the user can roll back later.
        public XmlBookRepository(string path, FileVersionStore versions)
        {
            _path = path;
            _versions = versions;
        }

        public IList<Book> GetAll()
        {
            return loadDoc().Root
                            .Elements("book")
                            .Select(toBook)
                            .ToList();
        }

        public Book GetByIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn)) return null;

            var el = findBook(loadDoc(), isbn);
            return el != null ? toBook(el) : null;
        }

        public void Add(Book book)
        {
            BookValidator.Validate(book);

            var doc = loadDoc();

            if (findBook(doc, book.Isbn) != null)
                throw new InvalidOperationException(
                    "A book with ISBN '" + book.Isbn + "' already exists.");

            doc.Root.Add(toXml(book));
            save(doc);
        }

        // Returns true if a book with this ISBN existed and was replaced;
        // false if no such book was found (a normal, non-exceptional outcome).
        public bool Edit(Book book)
        {
            BookValidator.Validate(book);

            var doc = loadDoc();
            var el = findBook(doc, book.Isbn);

            if (el == null) return false;

            el.ReplaceWith(toXml(book));
            save(doc);
            return true;
        }

        // Returns true if a book with this ISBN existed and was removed;
        // false if no such book was found.
        public bool Delete(string isbn)
        {
            var doc = loadDoc();
            var el = findBook(doc, isbn);

            if (el == null) return false;

            el.Remove();
            save(doc);
            return true;
        }

        // Persist and, when versioning is enabled, snapshot the new state so
        // this save shows up in the rollback history.
        private void save(XDocument doc)
        {
            doc.Save(_path);
            if (_versions != null) _versions.Snapshot();
        }

        // Loads the XML file and validates it against the embedded XSD. A file
        // that does not match the schema (e.g. hand-corrupted) throws
        // XmlSchemaValidationException instead of silently misbehaving.
        private XDocument loadDoc()
        {
            var doc = XDocument.Load(_path);
            doc.Validate(BookstoreSchema.SchemaSet, (sender, e) => { throw e.Exception; });
            return doc;
        }

        private static XElement findBook(XDocument doc, string isbn)
        {
            return doc.Root
                      .Elements("book")
                      .FirstOrDefault(b => (string)b.Element("isbn") == isbn);
        }

        // Book -> schema-ordered <book> element:
        // category, optional cover, isbn, title[@lang], author*, year, price.
        private static XElement toXml(Book book)
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

        private static Book toBook(XElement el)
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

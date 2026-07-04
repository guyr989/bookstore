using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Bookstore.Core.Common;
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

        public Result<Book> GetByIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return Result<Book>.Fail(ResultError.NotFound, noBookMessage(isbn));

            var el = findBook(loadDoc(), isbn);
            return el != null
                ? Result<Book>.Ok(toBook(el))
                : Result<Book>.Fail(ResultError.NotFound, noBookMessage(isbn));
        }

        public Result Add(Book book)
        {
            var validation = BookValidator.Validate(book);
            if (!validation.Success) return validation;

            var doc = loadDoc();

            if (findBook(doc, book.Isbn) != null)
                return Result.Fail(ResultError.Conflict,
                    "A book with ISBN '" + book.Isbn + "' already exists.");

            doc.Root.Add(toXml(book));
            save(doc);
            return Result.Ok();
        }

        public Result Edit(Book book)
        {
            var validation = BookValidator.Validate(book);
            if (!validation.Success) return validation;

            var doc = loadDoc();
            var el = findBook(doc, book.Isbn);

            if (el == null)
                return Result.Fail(ResultError.NotFound, noBookMessage(book.Isbn));

            el.ReplaceWith(toXml(book));
            save(doc);
            return Result.Ok();
        }

        public Result Delete(string isbn)
        {
            var doc = loadDoc();
            var el = findBook(doc, isbn);

            if (el == null)
                return Result.Fail(ResultError.NotFound, noBookMessage(isbn));

            el.Remove();
            save(doc);
            return Result.Ok();
        }

        // When versioning is enabled, stash the state being REPLACED before
        // writing: version N is the catalog as it was before save N, so
        // restoring the newest version undoes the latest save — including the
        // very first one (the original file becomes v1). Snapshotting before
        // the write also keeps the invariant that an exception from save()
        // means nothing was persisted.
        private void save(XDocument doc)
        {
            if (_versions != null) _versions.Snapshot();
            doc.Save(_path);
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

        private static string noBookMessage(string isbn)
        {
            return "No book found with ISBN '" + isbn + "'.";
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

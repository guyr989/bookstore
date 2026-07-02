using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Bookstore.Core.Models;

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
            var doc = XDocument.Load(_path);

            return doc.Root
                      .Elements("book")
                      .Select(MapBook)
                      .ToList();
        }

        public void Add(Book book) { 
            var doc = XDocument.Load(_path);
            doc.Element("bookstore").Add(new XElement("book",
                new XAttribute("category", book.Category),
                book.Cover != null ? new XAttribute("cover", book.Cover) : null,
                new XElement("isbn", book.Isbn),
                new XElement("title", new XAttribute("lang", book.Language), book.Title),
                book.Authors.Select(a => new XElement("author", a)),
                new XElement("year", book.Year.ToString(CultureInfo.InvariantCulture)),
                new XElement("price", book.Price.ToString(CultureInfo.InvariantCulture))
            ));
            doc.Save(_path);
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
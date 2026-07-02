using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http.Results;
using NUnit.Framework;
using Bookstore.Api.Controllers;
using Bookstore.Core.Models;
using Bookstore.Core.Persistence;

namespace Bookstore.Tests
{
    [TestFixture]
    public class BooksControllerTests
    {
        private string _xmlPath;
        private BooksController _controller;

        [SetUp]
        public void SetUp()
        {
            _xmlPath = Path.Combine(
                Path.GetTempPath(),
                "bookstore_api_test_" + Guid.NewGuid().ToString("N") + ".xml");
            File.WriteAllText(_xmlPath, SampleXml);
            _controller = new BooksController(new XmlBookRepository(_xmlPath));
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            if (File.Exists(_xmlPath))
                File.Delete(_xmlPath);
        }

        private static Book validBook()
        {
            return new Book
            {
                Isbn     = "9781111111111",
                Title    = "A Valid Book",
                Language = "en",
                Authors  = { "Some Author" },
                Year     = 2020,
                Price    = 19.99m,
                Category = "software"
            };
        }

        // ---- GET ------------------------------------------------------------

        [Test]
        public void GetAll_ReturnsOkWithAllBooks()
        {
            var result = _controller.GetAll() as OkNegotiatedContentResult<IList<Book>>;

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Content.Count);
        }

        [Test]
        public void Get_KnownIsbn_ReturnsOkWithBook()
        {
            var result = _controller.Get("9051234567897") as OkNegotiatedContentResult<Book>;

            Assert.IsNotNull(result);
            Assert.AreEqual("Harry Potter", result.Content.Title);
        }

        [Test]
        public void Get_UnknownIsbn_Returns404()
        {
            Assert.IsInstanceOf<NotFoundResult>(_controller.Get("0000000000000"));
        }

        // ---- POST -----------------------------------------------------------

        [Test]
        public void Post_ValidBook_Returns201AndPersists()
        {
            var result = _controller.Post(validBook());

            Assert.IsInstanceOf<CreatedNegotiatedContentResult<Book>>(result);
            var saved = new XmlBookRepository(_xmlPath).GetByIsbn("9781111111111");
            Assert.IsNotNull(saved);
            Assert.AreEqual("A Valid Book", saved.Title);
        }

        [Test]
        public void Post_InvalidBook_Returns400()
        {
            var invalid = validBook();
            invalid.Title = "";

            Assert.IsInstanceOf<BadRequestErrorMessageResult>(_controller.Post(invalid));
        }

        [Test]
        public void Post_NullBody_Returns400()
        {
            Assert.IsInstanceOf<BadRequestErrorMessageResult>(_controller.Post(null));
        }

        [Test]
        public void Post_DuplicateIsbn_Returns409()
        {
            var dup = validBook();
            dup.Isbn = "9051234567897"; // seed book

            var result = _controller.Post(dup) as NegotiatedContentResult<string>;

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Net.HttpStatusCode.Conflict, result.StatusCode);
        }

        // ---- PUT ------------------------------------------------------------

        [Test]
        public void Put_KnownIsbn_Returns200AndPersistsChanges()
        {
            var edited = validBook();
            edited.Title = "Harry Potter (Revised)";

            var result = _controller.Put("9051234567897", edited);

            Assert.IsInstanceOf<OkNegotiatedContentResult<Book>>(result);
            var saved = new XmlBookRepository(_xmlPath).GetByIsbn("9051234567897");
            Assert.AreEqual("Harry Potter (Revised)", saved.Title);
        }

        [Test]
        public void Put_UnknownIsbn_Returns404()
        {
            Assert.IsInstanceOf<NotFoundResult>(
                _controller.Put("0000000000000", validBook()));
        }

        [Test]
        public void Put_InvalidBook_Returns400()
        {
            var invalid = validBook();
            invalid.Category = " ";

            Assert.IsInstanceOf<BadRequestErrorMessageResult>(
                _controller.Put("9051234567897", invalid));
        }

        // ---- DELETE ---------------------------------------------------------

        [Test]
        public void Delete_KnownIsbn_Returns204AndRemoves()
        {
            var result = _controller.Delete("9051234567897") as StatusCodeResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Net.HttpStatusCode.NoContent, result.StatusCode);
            Assert.AreEqual(2, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        [Test]
        public void Delete_UnknownIsbn_Returns404()
        {
            Assert.IsInstanceOf<NotFoundResult>(_controller.Delete("0000000000000"));
        }

        private const string SampleXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<bookstore>
  <book category=""children"">
    <isbn>9051234567897</isbn>
    <title lang=""en"">Harry Potter</title>
    <author>J K. Rowling</author>
    <year>2005</year>
    <price>29.99</price>
  </book>
  <book category=""web"">
    <isbn>9031234567897</isbn>
    <title lang=""en"">XQuery Kick Start</title>
    <author>James McGovern</author>
    <author>Per Bothner</author>
    <year>2003</year>
    <price>49.99</price>
  </book>
  <book category=""web"" cover=""paperback"">
    <isbn>9043127323207</isbn>
    <title lang=""en"">Learning XML</title>
    <author>Erik T. Ray</author>
    <year>2003</year>
    <price>39.95</price>
  </book>
</bookstore>";
    }
}

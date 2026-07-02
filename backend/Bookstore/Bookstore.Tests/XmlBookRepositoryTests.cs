using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Bookstore.Core.Models;
using Bookstore.Core.Persistence;

namespace Bookstore.Tests
{
    [TestFixture]
    public class XmlBookRepositoryTests
    {
        // path to this test's private temp XML file.
        private string _xmlPath;
    
        [SetUp]
        public void SetUp()
        {
            _xmlPath = Path.Combine(
                Path.GetTempPath(),
                "bookstore_test_" + Guid.NewGuid().ToString("N") + ".xml");

            File.WriteAllText(_xmlPath, SampleXml);
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("TearDown start");
            if (File.Exists(_xmlPath))
                File.Delete(_xmlPath);
            Console.WriteLine("TearDown end");
        }

        [Test]
        public void GetAll_ReturnsEveryBookInTheFile()
        {
            Console.WriteLine("GetAll_ReturnsEveryBookInTheFile start");
            // Arrange
            var repo = new XmlBookRepository(_xmlPath);

            // Act
            var books = repo.GetAll();

            // Assert
            Assert.AreEqual(3, books.Count);
            Console.WriteLine("GetAll_ReturnsEveryBookInTheFile end");
        }

        [Test]
        public void GetAll_ReadsMultipleAuthorsIntoAList()
        {
            Console.WriteLine("GetAll_ReadsMultipleAuthorsIntoAList start");
            // Arrange
            var repo = new XmlBookRepository(_xmlPath);

            // Act
            var book = repo.GetByIsbn("9031234567897");

            // Assert
            Assert.AreEqual(5, book.Authors.Count);
            Assert.AreEqual(
                "James McGovern, Per Bothner, Kurt Cagle, James Linn, Vaidyanathan Nagarajan",
                book.AuthorsDisplay);
            Console.WriteLine("GetAll_ReadsMultipleAuthorsIntoAList end");
        }


        [Test]
        public void Add_PersistsNewBook_SoAFreshRepositoryCanReadItBack()
        {
            // Arrange: a brand-new book, including the optional cover attribute
            // and multiple authors, so Add must serialize every field.
            var repo = new XmlBookRepository(_xmlPath);
            var newBook = new Book
            {
                Isbn     = "9781234567890",
                Title    = "Clean Architecture",
                Language = "en",
                Authors  = { "Robert C. Martin", "Micah Martin" },
                Year     = 2017,
                Price    = 32.50m,
                Category = "software",
                Cover    = "hardcover"
            };

            // Act: write it, then re-read from a *fresh* repo on the same file.
            repo.Add(newBook);

            var reloaded = new XmlBookRepository(_xmlPath)
                .GetAll()
                .Single(b => b.Isbn == "9781234567890");

            // Assert: every field survived the round-trip to disk and back.
            Assert.AreEqual("Clean Architecture", reloaded.Title);
            Assert.AreEqual("en", reloaded.Language);
            Assert.AreEqual(2, reloaded.Authors.Count);
            Assert.AreEqual("Robert C. Martin, Micah Martin", reloaded.AuthorsDisplay);
            Assert.AreEqual(2017, reloaded.Year);
            Assert.AreEqual(32.50m, reloaded.Price);
            Assert.AreEqual("software", reloaded.Category);
            Assert.AreEqual("hardcover", reloaded.Cover);
        }
        [Test]
        public void GetByIsbn_ReturnsCorrectBook()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var book = repo.GetByIsbn("9051234567897");
            Assert.AreEqual("Harry Potter", book.Title);
            Assert.AreEqual("J K. Rowling", book.AuthorsDisplay);
            Assert.AreEqual(2005, book.Year);
            Assert.AreEqual(29.99m, book.Price);
        }

        [Test]
        public void Edit()
        {
            var repo = new XmlBookRepository(_xmlPath);
            repo.Edit(new Book {
                Isbn = "9051234567897",
                Title = "Harry Potter and the Philosopher's Stone",
                Language = "en",
                Authors = { "J K. Rowling" },
                Year = 2005,
                Price = 29.99m,
                Category = "children",
                Cover = null
            });
            var reloaded = new XmlBookRepository(_xmlPath);
            var newBook = reloaded.GetByIsbn("9051234567897");
            Assert.AreEqual("Harry Potter and the Philosopher's Stone", newBook.Title);
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
                                                    <author>Kurt Cagle</author>
                                                    <author>James Linn</author>
                                                    <author>Vaidyanathan Nagarajan</author>
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
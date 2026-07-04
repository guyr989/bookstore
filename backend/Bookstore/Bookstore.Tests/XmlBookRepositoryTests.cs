using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Bookstore.Core.Models;
using Bookstore.Core.Persistence;

namespace Bookstore.Tests
{
    [TestFixture]
    public class XmlBookRepositoryTests
    {
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
            if (File.Exists(_xmlPath))
                File.Delete(_xmlPath);
        }

        [Test]
        public void GetByIsbn_ReadsMultipleAuthorsIntoAList()
        {
            var repo = new XmlBookRepository(_xmlPath);

            var book = repo.GetByIsbn("9031234567897");

            Assert.AreEqual(5, book.Authors.Count);
            Assert.AreEqual(
                "James McGovern, Per Bothner, Kurt Cagle, James Linn, Vaidyanathan Nagarajan",
                book.AuthorsDisplay);
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

        [Test]
        public void Edit_WhenIsbnNotFound_ReturnsFalseAndChangesNothing()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var missing = new Book
            {
                Isbn     = "0000000000000",
                Title    = "Ghost Book",
                Language = "en",
                Authors  = { "Nobody" },
                Year     = 2020,
                Price    = 10.00m,
                Category = "web"
            };

            var edited = repo.Edit(missing);

            Assert.IsFalse(edited);
            // File untouched: still the original 3 books, no ghost added.
            Assert.AreEqual(3, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        [Test]
        public void GetByIsbn_OnSameInstance_SeesBookAddedThroughThatInstance()
        {
            // Regression guard: GetByIsbn must read the file, not a stale snapshot.
            // Using ONE repo across Add-then-GetByIsbn would fail if a cache went stale.
            var repo = new XmlBookRepository(_xmlPath);
            repo.Add(new Book
            {
                Isbn     = "9781234567890",
                Title    = "Clean Architecture",
                Language = "en",
                Authors  = { "Robert C. Martin" },
                Year     = 2017,
                Price    = 32.50m,
                Category = "software"
            });

            var found = repo.GetByIsbn("9781234567890");

            Assert.IsNotNull(found);
            Assert.AreEqual("Clean Architecture", found.Title);
        }

        [Test]
        public void Delete_RemovesOnlyTheTargetBook()
        {
            var repo = new XmlBookRepository(_xmlPath);

            var removed = repo.Delete("9051234567897");

            Assert.IsTrue(removed);
            var all = new XmlBookRepository(_xmlPath).GetAll();
            // Exactly one gone: target absent, the other two survive.
            Assert.AreEqual(2, all.Count);
            Assert.IsNull(all.SingleOrDefault(b => b.Isbn == "9051234567897"));
            Assert.IsNotNull(all.SingleOrDefault(b => b.Isbn == "9031234567897"));
        }

        [Test]
        public void Delete_WhenIsbnNotFound_ReturnsFalseAndChangesNothing()
        {
            var repo = new XmlBookRepository(_xmlPath);

            var removed = repo.Delete("0000000000000");

            Assert.IsFalse(removed);
            Assert.AreEqual(3, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        // ---- Data integrity (Step 13) ---------------------------------------

        // Builds an otherwise-valid book, so each negative test can isolate the
        // single field it wants to invalidate.
        private static Book ValidBook()
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

        [Test]
        public void Add_WithBlankTitle_ThrowsAndWritesNothing()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Title = "   ";

            Assert.Throws<ArgumentException>(() => repo.Add(invalid));
            // File untouched: still the original 3 books.
            Assert.AreEqual(3, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        [Test]
        public void Add_WithNonThirteenDigitIsbn_Throws()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Isbn = "123";

            Assert.Throws<ArgumentException>(() => repo.Add(invalid));
        }

        [Test]
        public void Add_WithNoAuthors_Throws()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Authors = new List<string>();

            Assert.Throws<ArgumentException>(() => repo.Add(invalid));
        }

        [Test]
        public void Add_DuplicateIsbn_ThrowsAndDoesNotDuplicate()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var dup = ValidBook();
            dup.Isbn = "9051234567897"; // already in the seed (Harry Potter)

            Assert.Throws<InvalidOperationException>(() => repo.Add(dup));
            // Still exactly one book with that ISBN.
            Assert.AreEqual(1,
                new XmlBookRepository(_xmlPath).GetAll().Count(b => b.Isbn == "9051234567897"));
        }

        [Test]
        public void Edit_WithInvalidBook_Throws()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Isbn = "9051234567897"; // exists, so it would otherwise edit
            invalid.Category = "";           // but this is invalid

            Assert.Throws<ArgumentException>(() => repo.Edit(invalid));
        }

        // ---- Versioning ------------------------------------------------------

        // Versioning tests get their own directory: snapshots are written to a
        // "versions" folder NEXT TO the data file, which must not leak into the
        // shared temp dir.
        private static string versionedXmlPath()
        {
            var dir = Path.Combine(Path.GetTempPath(),
                "bookstore_repo_versions_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "bookstore.xml");
            File.WriteAllText(path, SampleXml);
            return path;
        }

        [Test]
        public void Add_WithVersionStore_SnapshotsTheNewStateForRollback()
        {
            var path = versionedXmlPath();
            try
            {
                var versions = new FileVersionStore(path);
                var repo = new XmlBookRepository(path, versions);

                repo.Add(ValidBook());

                var list = versions.List();
                Assert.AreEqual(1, list.Count);
                Assert.AreEqual(1, list[0].Number);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(path), recursive: true);
            }
        }

        [Test]
        public void Add_ThenRestoringV1_UndoesTheVeryFirstSave()
        {
            // The point of rollback: the state BEFORE a save must be
            // recoverable — including before the first save ever made.
            var path = versionedXmlPath();
            try
            {
                var versions = new FileVersionStore(path);
                var repo = new XmlBookRepository(path, versions);

                repo.Add(ValidBook()); // catalog goes 3 -> 4 books

                versions.Restore(1);   // undo it

                Assert.AreEqual(3, new XmlBookRepository(path).GetAll().Count);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(path), recursive: true);
            }
        }

        [Test]
        public void Edit_WhenIsbnNotFound_WithVersionStore_SnapshotsNothing()
        {
            var path = versionedXmlPath();
            try
            {
                var versions = new FileVersionStore(path);
                var repo = new XmlBookRepository(path, versions);
                var missing = ValidBook();
                missing.Isbn = "0000000000000";

                repo.Edit(missing);

                Assert.IsEmpty(versions.List());
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(path), recursive: true);
            }
        }

        [Test]
        public void GetAll_WhenFileViolatesSchema_Throws()
        {
            // A <book> missing the required category attribute violates the XSD.
            File.WriteAllText(_xmlPath,
                "<bookstore><book><isbn>9051234567897</isbn>" +
                "<title lang=\"en\">Broken</title><author>X</author>" +
                "<year>2000</year><price>1.00</price></book></bookstore>");

            var repo = new XmlBookRepository(_xmlPath);

            Assert.Throws<XmlSchemaValidationException>(() => repo.GetAll());
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
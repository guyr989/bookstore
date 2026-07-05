using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Bookstore.Core.Common;
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

            var book = repo.GetByIsbn("9031234567897").Value;

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
            var bookWithCoverAndMultipleAuthors = new Book
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

            Assert.IsTrue(repo.Add(bookWithCoverAndMultipleAuthors).Success);

            var freshRepo = new XmlBookRepository(_xmlPath);
            var reloaded = freshRepo.GetAll().Single(b => b.Isbn == "9781234567890");

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
            var book = repo.GetByIsbn("9051234567897").Value;
            Assert.AreEqual("Harry Potter", book.Title);
            Assert.AreEqual("children", book.Category);
            Assert.AreEqual("J K. Rowling", book.AuthorsDisplay);
            Assert.AreEqual(2005, book.Year);
            Assert.IsNull(book.Cover);
            Assert.AreEqual(29.99m, book.Price);
        }

        [Test]
        public void Edit_ReplacesAllFields_AndReturnsSuccess()
        {
            var repo = new XmlBookRepository(_xmlPath);

            var result = repo.Edit(new Book
            {
                Isbn     = "9051234567897",
                Title    = "Harry Potter and the Philosopher's Stone",
                Language = "en",
                Authors  = { "J. K. Rowling", "Guest Illustrator" },
                Year     = 1997,
                Price    = 24.99m,
                Category = "fantasy",
                Cover    = "hardcover"
            });

            Assert.IsTrue(result.Success);

            var reloaded = new XmlBookRepository(_xmlPath).GetByIsbn("9051234567897").Value;
            Assert.AreEqual("Harry Potter and the Philosopher's Stone", reloaded.Title);
            Assert.AreEqual("en", reloaded.Language);
            Assert.AreEqual(2, reloaded.Authors.Count);
            Assert.AreEqual("J. K. Rowling, Guest Illustrator", reloaded.AuthorsDisplay);
            Assert.AreEqual(1997, reloaded.Year);
            Assert.AreEqual(24.99m, reloaded.Price);
            Assert.AreEqual("fantasy", reloaded.Category);
            Assert.AreEqual("hardcover", reloaded.Cover);
        }

        [Test]
        public void Edit_WhenIsbnNotFound_ReturnsNotFoundAndChangesNothing()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var originalBookCount = repo.GetAll().Count;
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

            var result = repo.Edit(missing);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.NotFound, result.Error);
            Assert.AreEqual(originalBookCount, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        [Test]
        public void GetByIsbn_OnSameInstance_SeesBookAddedThroughThatInstance()
        {
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

            Assert.IsTrue(found.Success);
            Assert.AreEqual("Clean Architecture", found.Value.Title);
        }

        [Test]
        public void GetByIsbn_WhenIsbnNotFound_ReturnsNotFoundWithNoValue()
        {
            var repo = new XmlBookRepository(_xmlPath);

            var result = repo.GetByIsbn("0000000000000");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.NotFound, result.Error);
            Assert.IsNull(result.Value);
        }

        [Test]
        public void Delete_RemovesOnlyTheTargetBook()
        {
            var repo = new XmlBookRepository(_xmlPath);

            var result = repo.Delete("9051234567897");

            Assert.IsTrue(result.Success);
            var all = new XmlBookRepository(_xmlPath).GetAll();
            Assert.AreEqual(2, all.Count);
            Assert.IsNull(all.SingleOrDefault(b => b.Isbn == "9051234567897"));
            Assert.IsNotNull(all.SingleOrDefault(b => b.Isbn == "9031234567897"));
        }

        [Test]
        public void Delete_WhenIsbnNotFound_ReturnsNotFoundAndChangesNothing()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var originalBookCount = repo.GetAll().Count;

            var result = repo.Delete("0000000000000");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.NotFound, result.Error);
            Assert.AreEqual(originalBookCount, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        [Test]
        public void Add_UnderConcurrentWrites_PersistsEveryBook_WithNoLostUpdates()
        {
            // Regression for the read-modify-write race: each Add loads the
            // whole file, mutates it, and saves it back. Without a shared lock
            // across the repository (and its version store), concurrent saves
            // clobber one another and books silently vanish. With versioning on,
            // this also exercises the nested save() -> Snapshot() lock path.
            var versions = new FileVersionStore(_xmlPath);
            var originalCount = new XmlBookRepository(_xmlPath, versions).GetAll().Count;

            const int writers = 24;
            System.Threading.Tasks.Parallel.For(0, writers, i =>
            {
                // A fresh repository per task, mirroring the per-request lifetime.
                var repo = new XmlBookRepository(_xmlPath, versions);
                var book = ValidBook();
                book.Isbn = "978" + i.ToString("D10");
                book.Title = "Concurrent Book " + i;
                Assert.IsTrue(repo.Add(book).Success);
            });

            var all = new XmlBookRepository(_xmlPath).GetAll();
            Assert.AreEqual(originalCount + writers, all.Count,
                "Concurrent adds lost updates -- the persistence lock is not serializing writes.");
            for (var i = 0; i < writers; i++)
                Assert.IsTrue(all.Any(b => b.Isbn == "978" + i.ToString("D10")),
                    "Missing book from writer " + i);
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
        public void Add_WithBlankTitle_ReturnsValidationFailureAndWritesNothing()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var originalBookCount = repo.GetAll().Count;
            var invalid = ValidBook();
            invalid.Title = "   ";

            var result = repo.Add(invalid);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.ValidationFailed, result.Error);
            Assert.AreEqual(originalBookCount, new XmlBookRepository(_xmlPath).GetAll().Count);
        }

        [Test]
        public void Add_WithNonThirteenDigitIsbn_ReturnsValidationFailure()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Isbn = "123";

            var result = repo.Add(invalid);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.ValidationFailed, result.Error);
        }

        [Test]
        public void Add_WithNoAuthors_ReturnsValidationFailure()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Authors = new List<string>();

            var result = repo.Add(invalid);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.ValidationFailed, result.Error);
        }

        [Test]
        public void Add_DuplicateIsbn_ReturnsConflictAndDoesNotDuplicate()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var dup = ValidBook();
            dup.Isbn = "9051234567897"; // already in the seed (Harry Potter)

            var result = repo.Add(dup);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.Conflict, result.Error);
            Assert.AreEqual(1,
                new XmlBookRepository(_xmlPath).GetAll().Count(b => b.Isbn == "9051234567897"));
        }

        [Test]
        public void Add_WithMultipleInvalidFields_ReportsEveryViolation()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Title = "   ";
            invalid.Price = -5m;

            var result = repo.Add(invalid);

            Assert.AreEqual(ResultError.ValidationFailed, result.Error);
            Assert.AreEqual(2, result.Errors.Count);
            CollectionAssert.Contains(result.Errors, "Title is required.");
            CollectionAssert.Contains(result.Errors, "Price cannot be negative.");
        }

        [Test]
        public void Edit_WithInvalidBook_ReturnsValidationFailure()
        {
            var repo = new XmlBookRepository(_xmlPath);
            var invalid = ValidBook();
            invalid.Isbn = "9051234567897"; // exists, so it would otherwise edit
            invalid.Category = "";           // but this is invalid

            var result = repo.Edit(invalid);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ResultError.ValidationFailed, result.Error);
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

                Assert.IsTrue(repo.Add(ValidBook()).Success);

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

                Assert.IsTrue(repo.Add(ValidBook()).Success); // catalog goes 3 -> 4 books

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

                var result = repo.Edit(missing);

                Assert.AreEqual(ResultError.NotFound, result.Error);
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
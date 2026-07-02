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
            var xquery = repo.GetAll().Single(b => b.Isbn == "9031234567897");

            // Assert
            Assert.AreEqual(5, xquery.Authors.Count);
            Assert.AreEqual(
                "James McGovern, Per Bothner, Kurt Cagle, James Linn, Vaidyanathan Nagarajan",
                xquery.AuthorsDisplay);
            Console.WriteLine("GetAll_ReadsMultipleAuthorsIntoAList end");
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
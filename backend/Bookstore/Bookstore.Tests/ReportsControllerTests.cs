using System;
using System.IO;
using NUnit.Framework;
using Bookstore.Api.Controllers;
using Bookstore.Core.Persistence;

namespace Bookstore.Tests
{
    [TestFixture]
    public class ReportsControllerTests
    {
        private string _xmlPath;
        private ReportsController _controller;

        [SetUp]
        public void SetUp()
        {
            _xmlPath = Path.Combine(
                Path.GetTempPath(),
                "bookstore_report_test_" + Guid.NewGuid().ToString("N") + ".xml");
            File.WriteAllText(_xmlPath, SampleXml);
            _controller = new ReportsController(new XmlBookRepository(_xmlPath));
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            if (File.Exists(_xmlPath))
                File.Delete(_xmlPath);
        }

        [Test]
        public void BooksHtml_ReturnsHtmlTableWithEveryBook()
        {
            var response = _controller.BooksHtml();
            var html = response.Content.ReadAsStringAsync().Result;

            Assert.AreEqual("text/html", response.Content.Headers.ContentType.MediaType);
            StringAssert.Contains("<table>", html);
            StringAssert.Contains("Harry Potter", html);
            StringAssert.Contains("XQuery Kick Start", html);
            // Multi-author book shown comma-separated in one cell.
            StringAssert.Contains("James McGovern, Per Bothner", html);
            StringAssert.Contains("29.99", html);
        }

        [Test]
        public void BooksHtml_ShowsBookCountAndTotalPrice()
        {
            var html = _controller.BooksHtml().Content.ReadAsStringAsync().Result;

            StringAssert.Contains("2 books", html);
            StringAssert.Contains("79.98", html); // 29.99 + 49.99
        }

        [Test]
        public void BooksHtml_EscapesHtmlInBookFields()
        {
            // A malicious title must render as text, not markup (stored XSS).
            File.WriteAllText(_xmlPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<bookstore>
  <book category=""web"">
    <isbn>9051234567897</isbn>
    <title lang=""en"">&lt;script&gt;alert(1)&lt;/script&gt;</title>
    <author>Mallory</author>
    <year>2020</year>
    <price>1.00</price>
  </book>
</bookstore>");

            var html = _controller.BooksHtml().Content.ReadAsStringAsync().Result;

            StringAssert.DoesNotContain("<script>alert(1)</script>", html);
            StringAssert.Contains("&lt;script&gt;", html);
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
</bookstore>";
    }
}

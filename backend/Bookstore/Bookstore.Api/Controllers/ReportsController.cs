using System.Net;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Web.Http;
using Bookstore.Core.Persistence;

namespace Bookstore.Api.Controllers
{
    [RoutePrefix("api/reports")]
    public class ReportsController : ApiController
    {
        private readonly XmlBookRepository _repo;

        public ReportsController() : this(AppConfig.repo()) { }

        public ReportsController(XmlBookRepository repo)
        {
            _repo = repo;
        }

        // GET api/reports/books.html — the owner's catalog report as an HTML table.
        [HttpGet, Route("books.html")]
        public HttpResponseMessage BooksHtml()
        {
            var html = buildHtml();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
            return response;
        }

        private string buildHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">")
              .Append("<title>Bookstore Report</title>")
              .Append("<style>")
              .Append("body{font-family:Segoe UI,Arial,sans-serif;margin:2rem;color:#222}")
              .Append("h1{font-size:1.4rem}")
              .Append("table{border-collapse:collapse;width:100%}")
              .Append("th,td{border:1px solid #ccc;padding:.5rem .75rem;text-align:left}")
              .Append("th{background:#f4f4f4}")
              .Append("tr:nth-child(even){background:#fafafa}")
              .Append("</style></head><body>")
              .Append("<h1>Bookstore Catalog Report</h1>")
              .Append("<table><thead><tr>")
              .Append("<th>Title</th><th>Author(s)</th><th>Category</th><th>Year</th><th>Price</th>")
              .Append("</tr></thead><tbody>");

            foreach (var book in _repo.GetAll())
            {
                sb.Append("<tr>")
                  .Append("<td>").Append(esc(book.Title)).Append("</td>")
                  .Append("<td>").Append(esc(book.AuthorsDisplay)).Append("</td>")
                  .Append("<td>").Append(esc(book.Category)).Append("</td>")
                  .Append("<td>").Append(book.Year).Append("</td>")
                  .Append("<td>").Append(book.Price.ToString("0.00")).Append("</td>")
                  .Append("</tr>");
            }

            sb.Append("</tbody></table></body></html>");
            return sb.ToString();
        }

        // HTML-encode field values so a book titled "<script>..." renders as
        // text instead of executing (stored-XSS guard on the report).
        private static string esc(string value)
        {
            return SecurityElement.Escape(value ?? "");
        }
    }
}

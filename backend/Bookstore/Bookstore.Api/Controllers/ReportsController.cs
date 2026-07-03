using System.Globalization;
using System.Linq;
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
            var books = _repo.GetAll();
            var total = books.Sum(b => b.Price);

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">")
              .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
              .Append("<title>Bookstore Report</title>")
              .Append("<style>")
              .Append("body{font-family:Segoe UI,Arial,sans-serif;margin:1.5rem;color:#222;background:#fff}")
              .Append("h1{font-size:1.35rem;margin:0 0 .25rem}")
              .Append(".summary{color:#607d8b;margin:0 0 1.25rem;font-size:.95rem}")
              .Append("table{border-collapse:collapse;width:100%;font-size:.95rem}")
              .Append("th,td{border:1px solid #e0e0e0;padding:.55rem .8rem;text-align:left}")
              .Append("th{background:#263238;color:#fff;font-weight:600;letter-spacing:.02em}")
              .Append("tbody tr:nth-child(even){background:#f7f9fa}")
              .Append("tbody tr:hover{background:#eef4f8}")
              .Append("td.num{text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}")
              .Append("tfoot td{background:#f4f4f4;font-weight:600}")
              .Append("@media(max-width:640px){body{margin:.75rem}th,td{padding:.4rem .5rem}}")
              .Append("</style></head><body>")
              .Append("<h1>Bookstore Catalog Report</h1>")
              .Append("<p class=\"summary\">").Append(books.Count)
              .Append(books.Count == 1 ? " book" : " books")
              .Append(" in the catalog &middot; combined list price ")
              .Append(total.ToString("0.00", CultureInfo.InvariantCulture)).Append("</p>")
              .Append("<table><thead><tr>")
              .Append("<th>Title</th><th>Author(s)</th><th>Category</th><th>Year</th><th>Price</th>")
              .Append("</tr></thead><tbody>");

            foreach (var book in books)
            {
                sb.Append("<tr>")
                  .Append("<td>").Append(esc(book.Title)).Append("</td>")
                  .Append("<td>").Append(esc(book.AuthorsDisplay)).Append("</td>")
                  .Append("<td>").Append(esc(book.Category)).Append("</td>")
                  .Append("<td class=\"num\">").Append(book.Year).Append("</td>")
                  .Append("<td class=\"num\">").Append(book.Price.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>")
                  .Append("</tr>");
            }

            sb.Append("</tbody><tfoot><tr>")
              .Append("<td colspan=\"4\">Total</td>")
              .Append("<td class=\"num\">").Append(total.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>")
              .Append("</tr></tfoot></table></body></html>");
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

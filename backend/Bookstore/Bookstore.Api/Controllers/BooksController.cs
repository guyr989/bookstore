using System.Net;
using System.Web.Http;
using Bookstore.Api.Http;
using Bookstore.Core.Models;
using Bookstore.Core.Persistence;

namespace Bookstore.Api.Controllers
{
    [RoutePrefix("api/books")]
    public class BooksController : ApiController
    {
        private readonly XmlBookRepository _repo;

        // Used by the framework at runtime: path comes from Web.config.
        public BooksController() : this(AppConfig.repo()) { }

        // Used by tests: inject a repository over a temp file.
        public BooksController(XmlBookRepository repo)
        {
            _repo = repo;
        }

        // GET api/books
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            return Ok(_repo.GetAll());
        }

        // GET api/books/{isbn}
        [HttpGet, Route("{isbn}")]
        public IHttpActionResult Get(string isbn)
        {
            var result = _repo.GetByIsbn(isbn);
            if (!result.Success) return this.ToErrorResponse(result);
            return Ok(result.Value);
        }

        // POST api/books
        [HttpPost, Route("")]
        public IHttpActionResult Post([FromBody] Book book)
        {
            if (book == null) return BadRequest("Request body must contain a book.");

            var result = _repo.Add(book);
            if (!result.Success) return this.ToErrorResponse(result);

            return Created("api/books/" + book.Isbn, book);
        }

        // PUT api/books/{isbn}
        [HttpPut, Route("{isbn}")]
        public IHttpActionResult Put(string isbn, [FromBody] Book book)
        {
            if (book == null) return BadRequest("Request body must contain a book.");
            book.Isbn = isbn;                             // the URL is the identity

            var result = _repo.Edit(book);
            if (!result.Success) return this.ToErrorResponse(result);

            return Ok(book);
        }

        // DELETE api/books/{isbn}
        [HttpDelete, Route("{isbn}")]
        public IHttpActionResult Delete(string isbn)
        {
            var result = _repo.Delete(isbn);
            if (!result.Success) return this.ToErrorResponse(result);

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}

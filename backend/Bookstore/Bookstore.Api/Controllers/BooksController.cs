using System;
using System.Web.Http;
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
            var book = _repo.GetByIsbn(isbn);
            if (book == null) return NotFound();
            return Ok(book);
        }

        // POST api/books
        [HttpPost, Route("")]
        public IHttpActionResult Post([FromBody] Book book)
        {
            if (book == null) return BadRequest("Request body must contain a book.");

            try
            {
                _repo.Add(book);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);            // invalid/empty book -> 400
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);              // duplicate ISBN -> 409
            }

            return Created("api/books/" + book.Isbn, book);
        }

        // PUT api/books/{isbn}
        [HttpPut, Route("{isbn}")]
        public IHttpActionResult Put(string isbn, [FromBody] Book book)
        {
            if (book == null) return BadRequest("Request body must contain a book.");
            book.Isbn = isbn;                             // the URL is the identity

            try
            {
                if (!_repo.Edit(book)) return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(book);
        }

        // DELETE api/books/{isbn}
        [HttpDelete, Route("{isbn}")]
        public IHttpActionResult Delete(string isbn)
        {
            if (!_repo.Delete(isbn)) return NotFound();
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        private IHttpActionResult Conflict(string message)
        {
            return Content(System.Net.HttpStatusCode.Conflict, message);
        }
    }
}

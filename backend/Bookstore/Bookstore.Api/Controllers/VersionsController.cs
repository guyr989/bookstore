using System.Web.Http;
using Bookstore.Core.Persistence;

namespace Bookstore.Api.Controllers
{
    // Rollback history for the XML data file: every successful save snapshots
    // the file, and these endpoints let the UI list, restore, or discard them.
    [RoutePrefix("api/versions")]
    public class VersionsController : ApiController
    {
        private readonly FileVersionStore _versions;

        public VersionsController() : this(AppConfig.versions()) { }

        public VersionsController(FileVersionStore versions)
        {
            _versions = versions;
        }

        // GET api/versions
        [HttpGet, Route("")]
        public IHttpActionResult GetAll()
        {
            return Ok(_versions.List());
        }

        // POST api/versions/{number}/restore
        [HttpPost, Route("{number:int}/restore")]
        public IHttpActionResult Restore(int number)
        {
            try
            {
                if (!_versions.Restore(number)) return NotFound();
            }
            catch (System.Xml.Schema.XmlSchemaValidationException)
            {
                // The snapshot on disk no longer matches the schema (e.g. it
                // was hand-edited): refuse to make it the live catalog.
                return Content(System.Net.HttpStatusCode.Conflict,
                    "Version " + number + " is corrupted and cannot be restored.");
            }
            return Ok();
        }

        // DELETE api/versions/{number}
        [HttpDelete, Route("{number:int}")]
        public IHttpActionResult Delete(int number)
        {
            if (!_versions.Delete(number)) return NotFound();
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }
    }
}

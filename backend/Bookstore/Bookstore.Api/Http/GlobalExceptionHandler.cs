using System.Net;
using System.Net.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using System.Xml.Schema;

namespace Bookstore.Api.Http
{
    // Last-resort safety net for genuinely exceptional failures that reach
    // past the controllers: the client gets a clean 500 with a safe message,
    // never a stack trace.
    public class GlobalExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            var message = context.Exception is XmlSchemaValidationException
                ? "The bookstore data file no longer matches the catalog schema. " +
                  "Restore a previous version or repair the file, then retry."
                : "An unexpected error occurred.";

            context.Result = new ResponseMessageResult(
                context.Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new ApiError(new[] { message })));
        }
    }
}

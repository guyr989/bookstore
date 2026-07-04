using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using Bookstore.Core.Common;

namespace Bookstore.Api.Http
{
    // The one place a failed Result becomes an HTTP response, so every
    // controller action agrees on which status code an error kind gets.
    public static class ResultResponses
    {
        public static IHttpActionResult ToErrorResponse(this ApiController controller, Result result)
        {
            switch (result.Error)
            {
                case ResultError.ValidationFailed:
                    return respond(controller, HttpStatusCode.BadRequest, new ApiError(result.Errors));
                case ResultError.Conflict:
                    return respond(controller, HttpStatusCode.Conflict, new ApiError(result.Errors));
                case ResultError.NotFound:
                    return respond(controller, HttpStatusCode.NotFound, new ApiError(result.Errors));
                default:
                    // An error kind nobody mapped yet must fail loudly as a
                    // server bug, never be silently misreported as a 400.
                    return respond(controller, HttpStatusCode.InternalServerError,
                        new ApiError(new[] { "Unmapped result error '" + result.Error + "'." }));
            }
        }

        private static IHttpActionResult respond(
            ApiController controller, HttpStatusCode status, ApiError body)
        {
            return new NegotiatedContentResult<ApiError>(status, body, controller);
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Xml.Schema;
using NUnit.Framework;
using Bookstore.Api.Http;

namespace Bookstore.Tests
{
    [TestFixture]
    public class GlobalExceptionHandlerTests
    {
        private static ExceptionHandlerContext contextFor(Exception exception)
        {
            var request = new HttpRequestMessage();
            request.SetConfiguration(new HttpConfiguration());
            return new ExceptionHandlerContext(new ExceptionContext(
                exception, ExceptionCatchBlocks.HttpControllerDispatcher, request));
        }

        private static ApiError handle(Exception exception, out HttpStatusCode status)
        {
            var context = contextFor(exception);
            new GlobalExceptionHandler().Handle(context);

            var response = context.Result.ExecuteAsync(CancellationToken.None).Result;
            status = response.StatusCode;

            ApiError body;
            Assert.IsTrue(response.TryGetContentValue(out body));
            return body;
        }

        [Test]
        public void Handle_SchemaViolation_Returns500WithSafeDataFileMessage()
        {
            HttpStatusCode status;
            var body = handle(new XmlSchemaValidationException("internal XSD detail"), out status);

            Assert.AreEqual(HttpStatusCode.InternalServerError, status);
            StringAssert.Contains("no longer matches the catalog schema", body.Message);
            StringAssert.DoesNotContain("internal XSD detail", body.Message);
        }

        [Test]
        public void Handle_UnexpectedException_Returns500WithGenericMessage()
        {
            HttpStatusCode status;
            var body = handle(new InvalidOperationException("secret internals"), out status);

            Assert.AreEqual(HttpStatusCode.InternalServerError, status);
            Assert.AreEqual("An unexpected error occurred.", body.Message);
        }
    }
}

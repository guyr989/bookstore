using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Http.Results;
using NUnit.Framework;
using Bookstore.Api.Controllers;
using Bookstore.Core.Persistence;

namespace Bookstore.Tests
{
    [TestFixture]
    public class VersionsControllerTests
    {
        private string _dir;
        private string _dataPath;
        private FileVersionStore _store;
        private VersionsController _controller;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(),
                "bookstore_versions_api_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _dataPath = Path.Combine(_dir, "bookstore.xml");
            File.WriteAllText(_dataPath, "<bookstore />");
            _store = new FileVersionStore(_dataPath);
            _controller = new VersionsController(_store);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void GetAll_ReturnsOkWithEveryVersion()
        {
            _store.Snapshot();
            _store.Snapshot();

            var result = _controller.GetAll() as OkNegotiatedContentResult<IList<FileVersion>>;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Content.Count);
        }

        [Test]
        public void Restore_WhenVersionExists_ReturnsOkAndRestoresTheFile()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- old --></bookstore>");
            _store.Snapshot(); // v1
            File.WriteAllText(_dataPath, "<bookstore><!-- new --></bookstore>");

            var result = _controller.Restore(1);

            Assert.IsInstanceOf<OkResult>(result);
            Assert.AreEqual("<bookstore><!-- old --></bookstore>", File.ReadAllText(_dataPath));
        }

        [Test]
        public void Restore_WhenVersionMissing_Returns404()
        {
            Assert.IsInstanceOf<NotFoundResult>(_controller.Restore(42));
        }

        [Test]
        public void Restore_WhenSnapshotIsCorrupted_Returns409AndLeavesDataFileUntouched()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- live --></bookstore>");
            _store.Snapshot();
            // Corrupt v1 on disk (book missing category, isbn not 13 digits):
            // the store throws, the controller must map that to a safe 409
            // rather than let it corrupt the live catalog or leak a 500.
            File.WriteAllText(Path.Combine(_dir, "versions", "bookstore.v0001.xml"),
                "<bookstore><book><isbn>123</isbn></book></bookstore>");

            var result = _controller.Restore(1) as NegotiatedContentResult<string>;

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Net.HttpStatusCode.Conflict, result.StatusCode);
            Assert.AreEqual("<bookstore><!-- live --></bookstore>", File.ReadAllText(_dataPath));
        }

        [Test]
        public void Delete_WhenVersionExists_Returns204()
        {
            _store.Snapshot();

            var result = _controller.Delete(1) as StatusCodeResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Net.HttpStatusCode.NoContent, result.StatusCode);
            Assert.IsEmpty(_store.List());
        }

        [Test]
        public void Delete_WhenVersionMissing_Returns404()
        {
            Assert.IsInstanceOf<NotFoundResult>(_controller.Delete(42));
        }
    }
}

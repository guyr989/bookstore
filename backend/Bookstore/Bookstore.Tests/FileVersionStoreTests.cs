using System;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Bookstore.Core.Persistence;

namespace Bookstore.Tests
{
    [TestFixture]
    public class FileVersionStoreTests
    {
        private string _dir;
        private string _dataPath;
        private FileVersionStore _store;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(),
                "bookstore_versions_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _dataPath = Path.Combine(_dir, "bookstore.xml");
            File.WriteAllText(_dataPath, "<bookstore />");
            _store = new FileVersionStore(_dataPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void List_WhenNothingSnapshotted_IsEmpty()
        {
            Assert.IsEmpty(_store.List());
        }

        [Test]
        public void Snapshot_CreatesNumberedVersionsStartingAtOne()
        {
            var first = _store.Snapshot();
            File.WriteAllText(_dataPath, "<bookstore><!-- v2 --></bookstore>");
            var second = _store.Snapshot();

            Assert.AreEqual(1, first.Number);
            Assert.AreEqual(2, second.Number);
            Assert.AreEqual(new[] { 1, 2 }, _store.List().Select(v => v.Number).ToArray());
        }

        [Test]
        public void Snapshot_CopiesTheCurrentFileContent()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- snapshot me --></bookstore>");

            _store.Snapshot();
            File.WriteAllText(_dataPath, "<bookstore />"); // mutate afterwards

            var versionFile = Path.Combine(_dir, "versions", "bookstore.v0001.xml");
            Assert.AreEqual("<bookstore><!-- snapshot me --></bookstore>",
                File.ReadAllText(versionFile));
        }

        [Test]
        public void Snapshot_StampsTheTimeItWasTaken_NotTheDataFilesWriteTime()
        {
            // File.Copy preserves the source's write time; a snapshot must
            // carry the moment it was taken, which is what the UI displays.
            File.SetLastWriteTimeUtc(_dataPath,
                new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var justBefore = DateTime.UtcNow.AddMinutes(-1);

            var version = _store.Snapshot();

            Assert.GreaterOrEqual(version.SavedAtUtc, justBefore);
        }

        [Test]
        public void Restore_CopiesTheVersionBackOverTheDataFile()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- old --></bookstore>");
            _store.Snapshot();
            File.WriteAllText(_dataPath, "<bookstore><!-- new --></bookstore>");

            var restored = _store.Restore(1);

            Assert.IsTrue(restored);
            Assert.AreEqual("<bookstore><!-- old --></bookstore>", File.ReadAllText(_dataPath));
        }

        [Test]
        public void Restore_SnapshotsTheCurrentStateFirst_SoARestoreIsUndoable()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- old --></bookstore>");
            _store.Snapshot(); // v1
            File.WriteAllText(_dataPath, "<bookstore><!-- new --></bookstore>");

            _store.Restore(1); // should stash "new" as v2 before overwriting

            var v2 = Path.Combine(_dir, "versions", "bookstore.v0002.xml");
            Assert.AreEqual("<bookstore><!-- new --></bookstore>", File.ReadAllText(v2));
        }

        [Test]
        public void Restore_WhenSnapshotIsCorrupted_ThrowsAndChangesNothing()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- live --></bookstore>");
            _store.Snapshot();
            // Schema-invalid (book missing category, isbn not 13 digits):
            // rolling back to this would corrupt the live catalog.
            File.WriteAllText(Path.Combine(_dir, "versions", "bookstore.v0001.xml"),
                "<bookstore><book><isbn>123</isbn></book></bookstore>");

            Assert.Throws<XmlSchemaValidationException>(() => _store.Restore(1));

            Assert.AreEqual("<bookstore><!-- live --></bookstore>", File.ReadAllText(_dataPath));
            Assert.AreEqual(new[] { 1 }, _store.List().Select(v => v.Number).ToArray());
        }

        [Test]
        public void Restore_WhenVersionMissing_ReturnsFalseAndChangesNothing()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- untouched --></bookstore>");

            Assert.IsFalse(_store.Restore(42));
            Assert.AreEqual("<bookstore><!-- untouched --></bookstore>",
                File.ReadAllText(_dataPath));
        }

        [Test]
        public void Delete_RemovesOnlyThatVersion()
        {
            _store.Snapshot(); // v1
            _store.Snapshot(); // v2

            var deleted = _store.Delete(1);

            Assert.IsTrue(deleted);
            Assert.AreEqual(new[] { 2 }, _store.List().Select(v => v.Number).ToArray());
        }

        [Test]
        public void Delete_WhenVersionMissing_ReturnsFalse()
        {
            Assert.IsFalse(_store.Delete(42));
        }

        [Test]
        public void List_IgnoresFilesThatOnlyResembleVersions()
        {
            _store.Snapshot(); // v1
            // Editor/backup litter: Directory.GetFiles' 3-char-extension quirk
            // matches "*.xml~" for pattern "*.xml"; these must not become
            // phantom (duplicate) version numbers.
            File.WriteAllText(Path.Combine(_dir, "versions", "bookstore.v0001.xml~"), "junk");
            File.WriteAllText(Path.Combine(_dir, "versions", "bookstore.v12.xml"), "junk");

            Assert.AreEqual(new[] { 1 }, _store.List().Select(v => v.Number).ToArray());
        }

        [Test]
        public void Restore_WhenDataFileIsMissing_StillRestoresTheVersion()
        {
            File.WriteAllText(_dataPath, "<bookstore><!-- old --></bookstore>");
            _store.Snapshot();
            File.Delete(_dataPath); // catalog lost; history is the recovery path

            Assert.IsTrue(_store.Restore(1));
            Assert.AreEqual("<bookstore><!-- old --></bookstore>", File.ReadAllText(_dataPath));
        }

        [Test]
        public void Snapshot_NumbersContinueFromTheHighestRemainingVersion()
        {
            _store.Snapshot(); // v1
            _store.Snapshot(); // v2
            _store.Delete(2);

            var next = _store.Snapshot();

            Assert.AreEqual(2, next.Number);
        }
    }
}

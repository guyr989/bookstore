using System;
using System.IO;
using System.Linq;
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

            var version = _store.Snapshot();
            File.WriteAllText(_dataPath, "<bookstore />"); // mutate afterwards

            var versionFile = Path.Combine(_dir, "versions", "bookstore.v0001.xml");
            Assert.AreEqual("<bookstore><!-- snapshot me --></bookstore>",
                File.ReadAllText(versionFile));
            Assert.LessOrEqual(version.SavedAtUtc, DateTime.UtcNow.AddMinutes(1));
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
        public void Snapshot_NumbersContinueFromTheHighestRemainingVersion()
        {
            _store.Snapshot(); // v1
            _store.Snapshot(); // v2
            _store.Delete(2);

            var next = _store.Snapshot();

            // Next number is always max remaining + 1.
            Assert.AreEqual(2, next.Number);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Bookstore.Core.Persistence
{
    // Keeps numbered snapshots of the bookstore XML file in a "versions"
    // folder next to it (bookstore.v0001.xml, bookstore.v0002.xml, ...), so
    // any save can be rolled back. Restore stashes the current file first,
    // so a rollback is itself undoable.
    public class FileVersionStore
    {
        // Snapshots beyond this count are pruned oldest-first: bounds disk
        // growth and keeps directory scans cheap on a long-lived deployment.
        private const int MaxKept = 100;

        // Shared with XmlBookRepository (see PersistenceGate): serializing the
        // read-number-then-copy sequence against repository writes prevents two
        // concurrent saves from claiming the same version number or racing the
        // data-file write.
        private static readonly object _gate = PersistenceGate.Sync;

        private readonly string _dataPath;
        private readonly string _versionsDir;
        private readonly string _baseName;   // data file name without extension
        private readonly string _extension;  // data file extension, incl. dot

        public FileVersionStore(string dataPath)
        {
            _dataPath = dataPath;
            _versionsDir = Path.Combine(Path.GetDirectoryName(dataPath), "versions");
            _baseName = Path.GetFileNameWithoutExtension(dataPath);
            _extension = Path.GetExtension(dataPath);
        }

        public IList<FileVersion> List()
        {
            lock (_gate)
            {
                return numbers().OrderBy(n => n)
                                .Select(n => new FileVersion
                                {
                                    Number = n,
                                    SavedAtUtc = File.GetLastWriteTimeUtc(versionPath(n))
                                })
                                .ToList();
            }
        }

        // Copies the current data file into the next numbered version slot.
        public FileVersion Snapshot()
        {
            lock (_gate)
            {
                return snapshotLocked();
            }
        }

        // Returns false when the version does not exist. Otherwise snapshots
        // the current state (so the restore can be undone) and copies the
        // version back over the data file.
        public bool Restore(int number)
        {
            lock (_gate)
            {
                var source = versionPath(number);
                if (!File.Exists(source)) return false;

                // Refuse to roll back to a corrupted snapshot (e.g. a
                // hand-edited file): validating here keeps the live catalog
                // schema-valid, the same guarantee the repository gives.
                var doc = XDocument.Load(source);
                doc.Validate(BookstoreSchema.SchemaSet, (sender, e) => { throw e.Exception; });

                // Stash the current state so the restore is undoable. If the
                // data file itself is gone, restoring IS the recovery path -
                // there is nothing to stash.
                if (File.Exists(_dataPath)) snapshotLocked();

                File.Copy(source, _dataPath, overwrite: true);
                return true;
            }
        }

        public bool Delete(int number)
        {
            lock (_gate)
            {
                var path = versionPath(number);
                if (!File.Exists(path)) return false;

                File.Delete(path);
                return true;
            }
        }

        // Assumes _gate is held.
        private FileVersion snapshotLocked()
        {
            Directory.CreateDirectory(_versionsDir);

            var number = numbers().DefaultIfEmpty(0).Max() + 1;
            var target = versionPath(number);
            File.Copy(_dataPath, target, overwrite: false);
            // File.Copy preserves the source's write time; a snapshot should
            // carry the moment it was TAKEN, which is what the UI displays.
            File.SetLastWriteTimeUtc(target, DateTime.UtcNow);

            prune();

            return new FileVersion { Number = number, SavedAtUtc = File.GetLastWriteTimeUtc(target) };
        }

        // Assumes _gate is held. Drops the oldest snapshots beyond MaxKept.
        private void prune()
        {
            var all = numbers().OrderByDescending(n => n).ToList();
            foreach (var number in all.Skip(MaxKept))
                File.Delete(versionPath(number));
        }

        // Version numbers currently on disk, parsed strictly: a file counts
        // only if its name is exactly what versionPath() would produce, which
        // filters editor/backup litter such as "bookstore.v0001.xml~" (the
        // 3-char-extension quirk of Directory.GetFiles matches those too).
        private IEnumerable<int> numbers()
        {
            if (!Directory.Exists(_versionsDir)) yield break;

            var prefix = _baseName + ".v";
            foreach (var path in Directory.GetFiles(_versionsDir, prefix + "*" + _extension))
            {
                var name = Path.GetFileName(path);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                var digits = name.Substring(prefix.Length,
                    Math.Max(0, name.Length - prefix.Length - _extension.Length));

                int number;
                if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                    continue;
                if (!string.Equals(name, Path.GetFileName(versionPath(number)),
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                yield return number;
            }
        }

        private string versionPath(int number)
        {
            return Path.Combine(_versionsDir,
                _baseName + ".v" + number.ToString("D4", CultureInfo.InvariantCulture) + _extension);
        }
    }
}

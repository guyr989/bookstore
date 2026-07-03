using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Bookstore.Core.Persistence
{
    // Keeps numbered snapshots of the XML data file in a "versions" folder
    // next to it (bookstore.v0001.xml, bookstore.v0002.xml, ...), so any save
    // can be rolled back. Restore stashes the current file first, so a
    // rollback is itself undoable.
    public class FileVersionStore
    {
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
            if (!Directory.Exists(_versionsDir))
                return new List<FileVersion>();

            return Directory.GetFiles(_versionsDir, _baseName + ".v*" + _extension)
                            .Select(toVersion)
                            .Where(v => v != null)
                            .OrderBy(v => v.Number)
                            .ToList();
        }

        // Copies the current data file into the next numbered version slot.
        public FileVersion Snapshot()
        {
            Directory.CreateDirectory(_versionsDir);

            var number = nextNumber();
            var target = versionPath(number);
            File.Copy(_dataPath, target, overwrite: false);

            return new FileVersion
            {
                Number = number,
                SavedAtUtc = File.GetLastWriteTimeUtc(target)
            };
        }

        // Returns false when the version does not exist. Otherwise snapshots
        // the current state (so the restore can be undone) and copies the
        // version back over the data file.
        public bool Restore(int number)
        {
            var source = versionPath(number);
            if (!File.Exists(source)) return false;

            Snapshot();
            File.Copy(source, _dataPath, overwrite: true);
            return true;
        }

        public bool Delete(int number)
        {
            var path = versionPath(number);
            if (!File.Exists(path)) return false;

            File.Delete(path);
            return true;
        }

        private int nextNumber()
        {
            var versions = List();
            return versions.Count == 0 ? 1 : versions.Max(v => v.Number) + 1;
        }

        private string versionPath(int number)
        {
            return Path.Combine(_versionsDir,
                _baseName + ".v" + number.ToString("D4", CultureInfo.InvariantCulture) + _extension);
        }

        // Parses ".../bookstore.v0001.xml" -> FileVersion 1; null for files
        // that match the search pattern but not the exact numbering scheme.
        private FileVersion toVersion(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path); // bookstore.v0001
            var suffix = name.Substring(_baseName.Length + 2); // strip "<base>.v"

            int number;
            if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                return null;

            return new FileVersion
            {
                Number = number,
                SavedAtUtc = File.GetLastWriteTimeUtc(path)
            };
        }
    }
}

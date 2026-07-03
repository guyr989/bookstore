using System;

namespace Bookstore.Core.Persistence
{
    // One saved snapshot of the data file.
    public class FileVersion
    {
        public int Number { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }
}

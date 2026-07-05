namespace Bookstore.Core.Persistence
{
    // Single process-wide monitor serializing every data-file and
    // versions-folder mutation. XmlBookRepository and FileVersionStore are both
    // constructed per-request but back onto the same files, so a repository
    // write (load -> modify -> save) and a version-store op must not interleave
    // -- otherwise two concurrent saves can lose data or claim the same version
    // number. Monitor is re-entrant, so a repository write that snapshots
    // (save -> FileVersionStore.Snapshot) can nest the lock on the same thread.
    internal static class PersistenceGate
    {
        internal static readonly object Sync = new object();
    }
}

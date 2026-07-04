# Bookstore — Technical Reference

Built incrementally during a guided, method-by-method / test-by-test review.
Each entry: name, purpose, params, return value, and any review notes.

## Backend — Bookstore.Tests

### XmlBookRepositoryTests

**`SetUp()` / `TearDown()`** — NUnit fixture hooks.
Creates a private guid-named temp XML file seeded with `SampleXml` before each test; deletes it after. Isolates all 15 tests in this fixture from each other and from disk state. No params, no return.
Score: 9/10. Necessary, correctly scoped. (Debug `Console.WriteLine` calls removed.)

**`GetAll_ReturnsEveryBookInTheFile()`** — verifies `XmlBookRepository.GetAll()` returns all 3 seeded books.
No params, no return (test method).
Score: 6/10. Necessary smoke test for the read path, not a "showoff" test. Held back only by redundant AAA comments (removed) and leftover debug prints (removed) — logic itself was fine.


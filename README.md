# InsurTix Bookstore

A bookstore catalog manager: **ASP.NET Web API 2 (.NET Framework 4.8.1)** backend with an **XML file as the database**, and an **Angular 17** frontend. Full CRUD keyed by ISBN, HTML reports, Swagger docs, strict TDD, per-environment (local/test/prod) XML data files, and **file version history with rollback**.

## Architecture

```
Angular 17 SPA  ──HTTP/JSON──▶  ASP.NET Web API 2 (.NET Fx 4.8.1, IIS Express/IIS)
 reactive forms,                ├── BooksController     CRUD by ISBN (400/404/409 semantics)
 input sanitization,            ├── ReportsController   GET /api/reports/books.html
 flash toasts,                  ├── VersionsController  GET/restore/delete snapshots
 confirm dialogs,               ├── Swagger (Swashbuckle)  /swagger
 version history UI             └── Bookstore.Core
                                    ├── XmlBookRepository (LINQ to XML)
                                    ├── FileVersionStore (rollback snapshots)
                                    ├── BookValidator (no empty/invalid objects)
                                    └── embedded XSD, validated on every load
                                            │
                                bookstore.{local|test|prod}.xml  (App_Data)
                                versions/bookstore.*.vNNNN.xml   (rollback history)
```

- `backend/Bookstore/Bookstore.Core` — domain: `Book` model, XML repository, version store, validation. No web dependencies.
- `backend/Bookstore/Bookstore.Api` — Web API 2 host: controllers, Swagger, environment config.
- `backend/Bookstore/Bookstore.Tests` — NUnit suite (49 tests: repository + version store + controllers).
- `frontend/` — Angular 17 app (26 Karma specs).
- `data/` — canonical XSD schema + seed data files.

## Version history (rollback)

Every successful save (add / edit / delete) first snapshots the catalog file
into a `versions/` folder next to it — version N is the state **before**
save N, so restoring the newest version undoes the latest save and the
original file is always recoverable as v1.

- **UI:** the *History* page lists versions (number, date, time) with
  *Restore* (undoable — the current state is stashed first) and *Delete*
  (requires typing the word `delete`).
- **API:** `GET /api/versions`, `POST /api/versions/{n}/restore`,
  `DELETE /api/versions/{n}`.
- **Safety:** snapshot numbering is serialized under a process-wide lock;
  a restore validates the snapshot against the XSD before making it live
  (corrupted snapshot → HTTP 409); history is capped at the 100 newest
  snapshots. Known limitation: version numbers are reused after the
  highest-numbered version is deleted, so a stale History tab can restore a
  newer file bearing the same number.

## Running it

### Backend (Visual Studio 2022)
1. Open `backend/Bookstore/Bookstore.sln`.
2. Set **Bookstore.Api** as the startup project and run (F5). IIS Express serves it at `http://localhost:51234`.
3. Swagger UI: `http://localhost:51234/swagger`. HTML report: `http://localhost:51234/api/reports/books.html`.

### Frontend
```bash
cd frontend
npm install
npx ng serve        # http://localhost:4200 (CORS for this origin is enabled in the API)
```

### Tests
- **Backend:** Test Explorer in VS, or:
  `vstest.console.exe Bookstore.Tests\bin\Debug\Bookstore.Tests.dll --TestAdapterPath:packages\NUnit3TestAdapter.6.2.0\build\net462`
- **Frontend:** `npx ng test --watch=false --browsers=ChromeHeadless`

## Environment switching (local / test / prod)

The XML file path is **not hard-coded**: `XmlBookRepository` takes it via constructor, and the API host reads it from `Web.config` `appSettings["BookstoreXmlPath"]`. Build-configuration transforms rewrite it:

| Build config | Transform | Data file |
|---|---|---|
| Debug (local) | `Web.Debug.config` (no-op) | `~/App_Data/bookstore.local.xml` |
| Test | `Web.Test.config` | `~/App_Data/bookstore.test.xml` |
| Release (prod) | `Web.Release.config` | `~/App_Data/bookstore.prod.xml` (starts empty) |

## Data integrity

1. **No empty/invalid objects** — `BookValidator` rejects a book with a non-13-digit ISBN, blank title/language/category, no (or blank) authors, non-positive year, or negative price → HTTP 400.
2. **Unique ISBN** — `Add` refuses a duplicate ISBN → HTTP 409.
3. **Schema validation** — every file load validates against the XSD (embedded resource); a corrupted file fails loudly instead of misbehaving.
4. **XSS defense** — the HTML report escapes all field values (test-proven); Angular escapes on render and additionally strips markup from inputs before sending.

## Design decisions (interview notes)

- **No Docker (deliberate).** .NET Framework 4.8.1 is Windows-only; containerizing means multi-GB Windows-container images with Hyper-V isolation for zero benefit on a single-node CRUD app. Native IIS hosting is how this stack ships. Seams (constructor-injected path, stateless repo) keep a future containerization straightforward.
- **Modular monolith, not microservices.** Books + Reports as modules in one Web API project is right-sized; a service split would add network hops and deployment complexity with no scale to justify it.
- **Not-found returns, violations throw.** `GetByIsbn` → `null`, `Edit`/`Delete` → `false` (missing records are *expected* outcomes → clean 404s without try/catch). Contract violations (invalid book, duplicate ISBN, corrupt file) throw → 400/409/500.
- **ISBN format-only check (no checksum).** The assignment's own sample ISBNs are not checksum-valid ISBN-13s; enforcing the checksum would reject the seed data itself.
- **XSD is deliberately loose** (strings, structure-only) so pre-existing data always loads; strict per-field rules live in `BookValidator` on the write path, where rejection is actionable.
- **TDD throughout** — every repository/controller behavior was written red → green; see git history.

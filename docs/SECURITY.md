# Security posture &amp; pre-public hardening checklist

This service is currently an **internal** tool: it runs behind the organization's
network, CORS is locked to `http://localhost:4200`, and it is not reachable from
the open web. Its threat model reflects that.

A full-repository security audit found that the classic web risks are already
handled by existing controls:

- **XXE** — blocked by .NET's default `DtdProcessing.Prohibit` on `XDocument.Load`.
- **Path traversal** — version endpoints use `{number:int}` route constraints;
  the data-file path comes only from server config (`BookstoreXmlPath`), never
  from user input.
- **Stored XSS** (HTML report) — text is escaped with `SecurityElement.Escape`,
  and the report is displayed in a fully sandboxed (`sandbox=""`) iframe.
- **CSRF** — no cookie/ambient-auth surface exists to forge against.

## Fixed in this branch

- **Concurrent-write data race** — repository reads/writes and the version store
  now serialize on a single shared, re-entrant lock (`PersistenceGate`), so
  simultaneous saves can no longer lose updates or collide on a version number.
  Regression test: `Add_UnderConcurrentWrites_PersistsEveryBook_WithNoLostUpdates`.

## Before exposing this service publicly — do these first

These were deliberately **deferred** while the app stays internal. Each becomes
required the moment it is reachable from an untrusted network.

- [ ] **Authentication / authorization.** No endpoint currently requires
      credentials. Add a gate before public exposure — at minimum an API-key
      message handler validated against config; ideally bearer-token / OAuth with
      per-user authorization if the SPA gains real users.
- [ ] **Disable Swagger UI in Release.** It is currently enabled unconditionally.
      Gate `EnableSwagger`/`EnableSwaggerUi` to non-Release builds (or behind
      auth) so the API surface isn't advertised publicly.
- [ ] **Security response headers.** Add `X-Content-Type-Options: nosniff`,
      `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, and a
      `Content-Security-Policy` appropriate to the SPA. Consider HSTS once TLS is
      terminated at the app.
- [ ] **Safe production error pages.** Add `<customErrors>` / `<httpErrors>` so
      unexpected failures never surface internal detail. (The global exception
      handler already returns generic 500s without stack traces; this closes the
      static/handler-level gap.)
- [ ] **Confirm `debug` is off in production.** `Web.config` ships
      `debug="true"`; `Web.Release.config` already strips it via
      `RemoveAttributes(debug)` — verify the Release transform is applied at
      deploy time.

## Notes

- Dependencies (Newtonsoft.Json 13.0.3, Web API 5.2.9, Swashbuckle 5.6.0) were
  current with no known relevant CVEs at the time of the audit; re-check before
  a public launch.

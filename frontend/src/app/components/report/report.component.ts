import { Component } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { BookService } from '../../services/book.service';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="header">
      <h2>Catalog report</h2>
      <span class="tools">
        <button type="button" class="btn" (click)="refresh()">⟳ Refresh</button>
        <a [href]="rawUrl" target="_blank" rel="noopener" class="btn">Open in new tab</a>
        <a routerLink="/" class="btn">← Back to catalog</a>
      </span>
    </div>
    <!-- The report is server-rendered HTML; showing it in a sandboxed iframe
         keeps it isolated from the app's DOM (no scripts, no same-origin). -->
    <iframe [src]="url" sandbox="" title="Bookstore HTML report"></iframe>
  `,
  styles: [`
    .header { display: flex; justify-content: space-between; align-items: center;
              flex-wrap: wrap; gap: .5rem; }
    .tools  { display: flex; gap: .5rem; flex-wrap: wrap; }
    iframe  { width: 100%; height: calc(100vh - 12rem); min-height: 320px;
              border: 1px solid #ddd; border-radius: 6px; background: #fff;
              margin-top: .75rem; }
  `]
})
export class ReportComponent {
  readonly rawUrl: string;
  url: SafeResourceUrl;

  constructor(private bookService: BookService, private sanitizer: DomSanitizer) {
    this.rawUrl = bookService.reportUrl();
    this.url = this.trust(this.rawUrl);
  }

  // Re-request the report without leaving the page: a fresh cache-busting
  // URL makes the iframe reload the server-rendered HTML.
  refresh(): void {
    const sep = this.rawUrl.includes('?') ? '&' : '?';
    this.url = this.trust(this.rawUrl + sep + 't=' + Date.now());
  }

  // The URL comes from our own environment config (not user input), so
  // trusting it for iframe use is safe by construction.
  private trust(url: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }
}

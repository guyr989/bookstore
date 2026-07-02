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
      <span>
        <a [href]="rawUrl" target="_blank" rel="noopener" class="btn">Open in new tab</a>
        <a routerLink="/" class="btn">← Back to catalog</a>
      </span>
    </div>
    <!-- The report is server-rendered HTML; showing it in a sandboxed iframe
         keeps it isolated from the app's DOM (no scripts, no same-origin). -->
    <iframe [src]="url" sandbox="" title="Bookstore HTML report"></iframe>
  `,
  styles: [`
    .header { display: flex; justify-content: space-between; align-items: center; }
    .header span { display: flex; gap: .5rem; }
    iframe { width: 100%; height: 70vh; border: 1px solid #ddd; border-radius: 6px;
             background: #fff; }
  `]
})
export class ReportComponent {
  readonly rawUrl: string;
  readonly url: SafeResourceUrl;

  constructor(bookService: BookService, sanitizer: DomSanitizer) {
    this.rawUrl = bookService.reportUrl();
    // The URL comes from our own environment config (not user input), so
    // trusting it for iframe use is safe by construction.
    this.url = sanitizer.bypassSecurityTrustResourceUrl(this.rawUrl);
  }
}

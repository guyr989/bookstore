import { Component } from '@angular/core';
import { Toast, ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toasts',
  standalone: true,
  template: `
    <div class="toasts" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [class]="'toast ' + toast.kind"
             [attr.role]="toast.kind === 'error' ? 'alert' : 'status'">
          <span class="icon" aria-hidden="true">{{ iconFor(toast) }}</span>
          <span class="text">{{ toast.text }}</span>
          <button type="button" (click)="toastService.dismiss(toast.id)"
                  aria-label="Dismiss notification">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toasts { position: fixed; top: 1rem; right: 1rem; z-index: 300;
              display: flex; flex-direction: column; gap: .5rem;
              max-width: min(360px, calc(100vw - 2rem)); }
    .toast  { padding: .8rem 1rem; border-radius: 8px; color: #fff;
              min-width: 240px; display: flex; align-items: flex-start;
              gap: .6rem; box-shadow: 0 4px 16px rgba(0,0,0,.3);
              animation: slide-in .2s ease-out; }
    .toast.success { background: #2e7d32; }
    .toast.error   { background: #c62828; }
    .toast.info    { background: #1565c0; }
    .icon   { font-weight: 700; }
    .text   { flex: 1; line-height: 1.35; overflow-wrap: break-word;
              min-width: 0; }
    .toast button  { background: none; border: none; color: #fff;
                     font-size: 1.1rem; line-height: 1; cursor: pointer;
                     padding: 0 .1rem; opacity: .85; }
    .toast button:hover { opacity: 1; }
    @keyframes slide-in { from { transform: translateX(1rem); opacity: 0; }
                          to   { transform: none; opacity: 1; } }
  `]
})
export class ToastsComponent {
  constructor(public toastService: ToastService) {}

  iconFor(toast: Toast): string {
    switch (toast.kind) {
      case 'success': return '✓';
      case 'error':   return '!';
      default:        return 'ℹ';
    }
  }
}

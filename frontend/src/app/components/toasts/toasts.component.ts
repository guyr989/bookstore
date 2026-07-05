import { Component } from '@angular/core';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-toasts',
  standalone: true,
  template: `
    <div class="toasts" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [class.success]="toast.kind === 'success'"
             [class.error]="toast.kind === 'error'"
             [class.info]="toast.kind === 'info'"
             [attr.role]="toast.kind === 'error' ? 'alert' : 'status'">
          <span class="icon" aria-hidden="true">
            @switch (toast.kind) {
              @case ('success') {
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                     stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
              }
              @case ('error') {
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                     stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="9" /><path d="M12 7.5v5.5" /><path d="M12 16.5h.01" />
                </svg>
              }
              @default {
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                     stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="9" /><path d="M12 11v5.5" /><path d="M12 7.5h.01" />
                </svg>
              }
            }
          </span>
          <span class="text">{{ toast.text }}</span>
          <button type="button" (click)="toastService.dismiss(toast.id)"
                  aria-label="Dismiss notification">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toasts { position: fixed; top: var(--space-4); right: var(--space-4); z-index: 300;
              display: flex; flex-direction: column; gap: var(--space-2);
              max-width: min(360px, calc(100vw - 2rem)); }
    .toast  { padding: var(--space-3) var(--space-4); border-radius: var(--radius);
              color: #fff; min-width: 240px; display: flex; align-items: flex-start;
              gap: var(--space-2); box-shadow: var(--shadow-md);
              animation: slide-in .2s ease-out; }
    .toast.success { background: var(--success); }
    .toast.error   { background: var(--danger); }
    .toast.info    { background: var(--info); }
    .icon     { display: flex; flex: none; }
    .icon svg { width: 1.15rem; height: 1.15rem; }
    .text   { flex: 1; line-height: 1.4; overflow-wrap: break-word; min-width: 0; }
    .toast button  { background: none; border: none; color: #fff; font-size: 1.25rem;
                     line-height: 1; cursor: pointer; padding: 0 .1rem; opacity: .85;
                     flex: none; }
    .toast button:hover { opacity: 1; }
    @keyframes slide-in { from { transform: translateX(1rem); opacity: 0; }
                          to   { transform: none; opacity: 1; } }
  `]
})
export class ToastsComponent {
  constructor(public toastService: ToastService) {}
}

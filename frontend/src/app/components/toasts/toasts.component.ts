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
                           role="status">
          {{ toast.text }}
          <button type="button" (click)="toastService.dismiss(toast.id)"
                  aria-label="Dismiss">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toasts { position: fixed; top: 1rem; right: 1rem; z-index: 100;
              display: flex; flex-direction: column; gap: .5rem; }
    .toast  { padding: .75rem 1rem; border-radius: 6px; color: #fff;
              min-width: 220px; display: flex; justify-content: space-between;
              gap: .75rem; box-shadow: 0 2px 8px rgba(0,0,0,.25);
              animation: slide-in .2s ease-out; }
    .toast.success { background: #2e7d32; }
    .toast.error   { background: #c62828; }
    .toast button  { background: none; border: none; color: #fff;
                     font-size: 1rem; cursor: pointer; }
    @keyframes slide-in { from { transform: translateX(1rem); opacity: 0; }
                          to   { transform: none; opacity: 1; } }
  `]
})
export class ToastsComponent {
  constructor(public toastService: ToastService) {}
}

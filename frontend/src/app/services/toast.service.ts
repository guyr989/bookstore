import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  kind: 'success' | 'error';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private nextId = 1;

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  private push(kind: 'success' | 'error', text: string): void {
    const toast: Toast = { id: this.nextId++, kind, text };
    this.toasts.update(list => [...list, toast]);
    // Flash behavior: auto-dismiss after 4 seconds.
    setTimeout(() => this.dismiss(toast.id), 4000);
  }
}

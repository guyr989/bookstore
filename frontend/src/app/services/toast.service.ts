import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
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

  info(text: string): void {
    this.push('info', text);
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  private push(kind: ToastKind, text: string): void {
    const toast: Toast = { id: this.nextId++, kind, text };
    this.toasts.update(list => [...list, toast]);
    // Flash behavior: auto-dismiss. Errors are important - keep them up
    // longer so they cannot be missed.
    setTimeout(() => this.dismiss(toast.id), kind === 'error' ? 7000 : 4000);
  }
}

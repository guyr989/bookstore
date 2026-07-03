import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  title: string;
  message: string;
  confirmLabel?: string; // confirm button text, defaults to 'Confirm'
  danger?: boolean;      // style the confirm button as destructive
  requireWord?: string;  // user must type this word before confirming
}

// Promise-based replacement for window.confirm(). A page calls
// `await confirm.ask({...})`; the app-level ConfirmDialogComponent renders
// the current request and resolves the promise with the user's choice.
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly current = signal<ConfirmRequest | null>(null);
  private resolve?: (ok: boolean) => void;

  ask(request: ConfirmRequest): Promise<boolean> {
    this.current.set(request);
    return new Promise<boolean>(resolve => (this.resolve = resolve));
  }

  close(ok: boolean): void {
    this.current.set(null);
    this.resolve?.(ok);
    this.resolve = undefined;
  }
}

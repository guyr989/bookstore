import { Component, ElementRef, HostListener, ViewChild, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfirmService } from '../../services/confirm.service';

// App-level modal that renders whatever ConfirmService.ask() was called with.
// Backdrop click and Escape cancel; when the request carries `requireWord`,
// the confirm button stays disabled until the user types that word.
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.css']
})
export class ConfirmDialogComponent {
  typed = '';

  @ViewChild('dialogBox') dialogBox?: ElementRef<HTMLElement>;

  constructor(public confirm: ConfirmService) {
    // Move keyboard focus into the dialog when it opens; otherwise focus
    // stays on the triggering button behind the backdrop and Enter/Space
    // would re-fire it.
    effect(() => {
      if (this.confirm.current()) {
        setTimeout(() => this.dialogBox?.nativeElement.focus());
      }
    });
  }

  get canConfirm(): boolean {
    const req = this.confirm.current();
    if (!req) return false;
    if (!req.requireWord) return true;
    return this.typed.trim().toLowerCase() === req.requireWord.toLowerCase();
  }

  accept(): void {
    if (this.canConfirm) this.close(true);
  }

  close(ok: boolean): void {
    this.typed = '';
    this.confirm.close(ok);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.confirm.current()) this.close(false);
  }
}

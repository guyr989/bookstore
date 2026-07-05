import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Book } from '../../models/book';
import { BookService } from '../../services/book.service';
import { ConfirmService } from '../../services/confirm.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-book-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './book-list.component.html',
  styleUrls: ['./book-list.component.css']
})
export class BookListComponent implements OnInit {
  books: Book[] = [];
  loading = false;

  constructor(
    private bookService: BookService,
    private confirm: ConfirmService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.bookService.getAll().subscribe({
      next: books => {
        this.books = books;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Could not load books. Is the API running?');
      }
    });
  }

  async remove(book: Book): Promise<void> {
    // Explicit confirm before a destructive action.
    const ok = await this.confirm.ask({
      title: 'Delete book',
      message: `Delete "${book.title}" (ISBN ${book.isbn})?`,
      confirmLabel: 'Delete',
      danger: true
    });
    if (!ok) return;

    this.bookService.delete(book.isbn).subscribe({
      next: () => {
        this.toast.success(`Deleted "${book.title}".`);
        this.load();
      },
      error: err => {
        this.toast.error(err.status === 404
          ? 'That book no longer exists.'
          : 'Delete failed.');
        this.load();
      }
    });
  }
}

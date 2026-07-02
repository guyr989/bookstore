import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Book } from '../../models/book';
import { BookService } from '../../services/book.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-book-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './book-form.component.html',
  styleUrls: ['./book-form.component.css']
})
export class BookFormComponent implements OnInit {
  // Edit mode when the route carries an ISBN; add mode otherwise.
  isbnParam: string | null = null;
  saving = false;

  readonly currentYear = new Date().getFullYear();

  form = this.fb.group({
    isbn: ['', [Validators.required, Validators.pattern(/^\d{13}$/)]],
    title: ['', [Validators.required, Validators.maxLength(200)]],
    language: ['en', [Validators.required, Validators.maxLength(10)]],
    authors: this.fb.array([this.authorControl()]),
    year: [this.currentYear, [Validators.required, Validators.min(1), Validators.max(this.currentYear + 1)]],
    price: [0, [Validators.required, Validators.min(0)]],
    category: ['', [Validators.required, Validators.maxLength(50)]],
    cover: ['']
  });

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private bookService: BookService,
    private toast: ToastService
  ) {}

  get isEdit(): boolean {
    return this.isbnParam !== null;
  }

  get authors(): FormArray {
    return this.form.get('authors') as FormArray;
  }

  ngOnInit(): void {
    this.isbnParam = this.route.snapshot.paramMap.get('isbn');
    if (this.isbnParam) {
      this.form.get('isbn')!.disable(); // ISBN is the identity; not editable
      this.bookService.getByIsbn(this.isbnParam).subscribe({
        next: book => this.fill(book),
        error: () => {
          this.toast.error('Book not found.');
          this.router.navigate(['/']);
        }
      });
    }
  }

  addAuthor(): void {
    this.authors.push(this.authorControl());
  }

  removeAuthor(i: number): void {
    if (this.authors.length > 1) this.authors.removeAt(i);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('Please fix the highlighted fields.');
      return;
    }

    const book = this.toBook();
    this.saving = true;

    const call = this.isEdit ? this.bookService.edit(book) : this.bookService.add(book);
    call.subscribe({
      next: () => {
        this.saving = false;
        this.toast.success(this.isEdit
          ? `Updated "${book.title}".`
          : `Added "${book.title}".`);
        this.router.navigate(['/']);
      },
      error: err => {
        this.saving = false;
        if (err.status === 409) {
          this.toast.error('A book with this ISBN already exists.');
        } else if (err.status === 400) {
          this.toast.error('The server rejected the book: ' + (err.error?.message ?? 'invalid data.'));
        } else if (err.status === 404) {
          this.toast.error('Book not found.');
        } else {
          this.toast.error('Save failed. Is the API running?');
        }
      }
    });
  }

  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  private authorControl(value = ''): FormControl {
    return this.fb.control(value, [Validators.required, Validators.maxLength(100)]) as FormControl;
  }

  private fill(book: Book): void {
    this.authors.clear();
    book.authors.forEach(a => this.authors.push(this.authorControl(a)));
    this.form.patchValue({
      isbn: book.isbn,
      title: book.title,
      language: book.language,
      year: book.year,
      price: book.price,
      category: book.category,
      cover: book.cover ?? ''
    });
  }

  private toBook(): Book {
    const v = this.form.getRawValue();
    return {
      isbn: sanitize(v.isbn!),
      title: sanitize(v.title!),
      language: sanitize(v.language!),
      authors: (v.authors as string[]).map(sanitize).filter(a => a.length > 0),
      year: Number(v.year),
      price: Number(v.price),
      category: sanitize(v.category!),
      cover: sanitize(v.cover ?? '') || null
    };
  }
}

// Input purification: trim and strip any markup characters so stored data is
// plain text. Angular templates escape on render (context-aware), so this is
// defense-in-depth for anything else that ever consumes the XML.
export function sanitize(value: string): string {
  return (value ?? '').replace(/<[^>]*>/g, '').replace(/[<>]/g, '').trim();
}

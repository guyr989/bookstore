import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Book } from '../models/book';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BookService {
  private readonly baseUrl = `${environment.apiUrl}/books`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Book[]> {
    return this.http.get<Book[]>(this.baseUrl);
  }

  getByIsbn(isbn: string): Observable<Book> {
    return this.http.get<Book>(`${this.baseUrl}/${isbn}`);
  }

  add(book: Book): Observable<Book> {
    return this.http.post<Book>(this.baseUrl, book);
  }

  edit(book: Book): Observable<Book> {
    return this.http.put<Book>(`${this.baseUrl}/${book.isbn}`, book);
  }

  delete(isbn: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${isbn}`);
  }

  reportUrl(): string {
    return `${environment.apiUrl}/reports/books.html`;
  }
}

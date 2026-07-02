import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BookService } from './book.service';
import { Book } from '../models/book';
import { environment } from '../../environments/environment';

describe('BookService', () => {
  let service: BookService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/books`;

  const sample: Book = {
    isbn: '9781234567890',
    title: 'Clean Architecture',
    language: 'en',
    authors: ['Robert C. Martin'],
    year: 2017,
    price: 32.5,
    category: 'software'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BookService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getAll GETs api/books', () => {
    let result: Book[] = [];
    service.getAll().subscribe(books => (result = books));

    const req = http.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);

    expect(result.length).toBe(1);
    expect(result[0].title).toBe('Clean Architecture');
  });

  it('getByIsbn GETs api/books/{isbn}', () => {
    service.getByIsbn(sample.isbn).subscribe();
    const req = http.expectOne(`${base}/${sample.isbn}`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('add POSTs the book to api/books', () => {
    service.add(sample).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sample);
    req.flush(sample);
  });

  it('edit PUTs the book to api/books/{isbn}', () => {
    service.edit(sample).subscribe();
    const req = http.expectOne(`${base}/${sample.isbn}`);
    expect(req.request.method).toBe('PUT');
    req.flush(sample);
  });

  it('delete DELETEs api/books/{isbn}', () => {
    service.delete(sample.isbn).subscribe();
    const req = http.expectOne(`${base}/${sample.isbn}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});

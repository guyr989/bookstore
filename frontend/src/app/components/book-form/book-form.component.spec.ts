/// <reference types="jasmine" />
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../services/toast.service';
import { BookFormComponent, sanitize } from './book-form.component';

describe('sanitize (input purification)', () => {
  it('trims surrounding whitespace', () => {
    expect(sanitize('  Harry Potter  ')).toBe('Harry Potter');
  });

  it('preserves angle brackets in valid text (escaping is done on render)', () => {
    expect(sanitize('1 < 2 and 3 > 2')).toBe('1 < 2 and 3 > 2');
  });

  it('handles null-ish input', () => {
    expect(sanitize(undefined as unknown as string)).toBe('');
  });
});

describe('BookFormComponent (validation)', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
  });

  function createForm() {
    const fixture = TestBed.createComponent(BookFormComponent);
    fixture.detectChanges();
    return fixture.componentInstance.form;
  }

  it('starts invalid (required fields empty)', () => {
    expect(createForm().valid).toBeFalse();
  });

  it('rejects a non-13-digit ISBN', () => {
    const form = createForm();
    form.get('isbn')!.setValue('123');
    expect(form.get('isbn')!.valid).toBeFalse();
  });

  it('accepts a full valid book', () => {
    const form = createForm();
    form.patchValue({
      isbn: '9781234567890',
      title: 'Clean Architecture',
      language: 'en',
      year: 2017,
      price: 32.5,
      category: 'software'
    });
    (form.get('authors') as any).at(0).setValue('Robert C. Martin');
    expect(form.valid).toBeTrue();
  });

  it('rejects negative price', () => {
    const form = createForm();
    form.get('price')!.setValue(-1);
    expect(form.get('price')!.valid).toBeFalse();
  });
});

describe('BookFormComponent (save error handling)', () => {
  let http: HttpTestingController;
  let toastErrors: string[];

  beforeEach(async () => {
    toastErrors = [];
    await TestBed.configureTestingModule({
      imports: [BookFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    const toast = TestBed.inject(ToastService);
    spyOn(toast, 'error').and.callFake((msg: string) => { toastErrors.push(msg); });
  });

  afterEach(() => http.verify());

  function saveValidBook() {
    const fixture = TestBed.createComponent(BookFormComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.form.patchValue({
      isbn: '9781234567890',
      title: 'Clean Architecture',
      language: 'en',
      year: 2017,
      price: 32.5,
      category: 'software'
    });
    (component.form.get('authors') as any).at(0).setValue('Robert C. Martin');
    component.save();
    return http.expectOne(`${environment.apiUrl}/books`);
  }

  it('shows one toast per violation when the 400 body carries an errors array', () => {
    saveValidBook().flush(
      {
        message: 'Title is required. Price cannot be negative.',
        errors: ['Title is required.', 'Price cannot be negative.']
      },
      { status: 400, statusText: 'Bad Request' });

    expect(toastErrors).toEqual(['Title is required.', 'Price cannot be negative.']);
  });

  it('falls back to the joined message when the 400 body has no errors array', () => {
    saveValidBook().flush(
      { message: 'invalid book' },
      { status: 400, statusText: 'Bad Request' });

    expect(toastErrors).toEqual(['The server rejected the book: invalid book']);
  });

  it('reports a duplicate ISBN as a single conflict toast', () => {
    saveValidBook().flush(
      { message: "A book with ISBN '9781234567890' already exists." },
      { status: 409, statusText: 'Conflict' });

    expect(toastErrors).toEqual(['A book with this ISBN already exists.']);
  });
});

describe('BookFormComponent (edit mode prefill)', () => {
  const isbn = '9781234567890';
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: new Map([['isbn', isbn]]) } }
        }
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function loadEdit(authors: string[]) {
    const fixture = TestBed.createComponent(BookFormComponent);
    fixture.detectChanges(); // ngOnInit -> getByIsbn
    http.expectOne(`${environment.apiUrl}/books/${isbn}`).flush({
      isbn, title: 'Clean Architecture', language: 'en',
      authors, year: 2017, price: 32.5, category: 'software'
    });
    fixture.detectChanges();
    return fixture;
  }

  function authorInputs(fixture: ReturnType<typeof loadEdit>): HTMLInputElement[] {
    return fixture.debugElement
      .queryAll(By.css('.author-row input'))
      .map(de => de.nativeElement as HTMLInputElement);
  }

  it('renders one input per existing author, prefilled with the name', () => {
    const inputs = authorInputs(loadEdit(['Jane Austen', 'John Doe']));
    expect(inputs.map(i => i.value)).toEqual(['Jane Austen', 'John Doe']);
  });

  it('lets the user add another author after prefill', () => {
    const fixture = loadEdit(['Jane Austen']);
    fixture.componentInstance.addAuthor();
    fixture.detectChanges();

    const inputs = authorInputs(fixture);
    expect(inputs.length).toBe(2);
    expect(inputs[0].value).toBe('Jane Austen');
    expect(inputs[1].value).toBe('');
  });
});

/// <reference types="jasmine" />
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { BookFormComponent, sanitize } from './book-form.component';

describe('sanitize (input purification)', () => {
  it('trims whitespace', () => {
    expect(sanitize('  Harry Potter  ')).toBe('Harry Potter');
  });

  it('strips HTML tags', () => {
    expect(sanitize('<script>alert(1)</script>Evil')).toBe('alert(1)Evil');
  });

  it('removes angle brackets', () => {
    expect(sanitize('1 < 2 and 3 > 2')).not.toContain('<');
    expect(sanitize('1 < 2 and 3 > 2')).not.toContain('>');
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

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
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

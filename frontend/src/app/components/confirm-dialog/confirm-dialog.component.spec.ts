/// <reference types="jasmine" />
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ConfirmDialogComponent } from './confirm-dialog.component';
import { ConfirmService } from '../../services/confirm.service';

describe('ConfirmDialogComponent', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let confirm: ConfirmService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent]
    }).compileComponents();
    confirm = TestBed.inject(ConfirmService);
    fixture = TestBed.createComponent(ConfirmDialogComponent);
    fixture.detectChanges();
  });

  function confirmButton(): HTMLButtonElement {
    const buttons = fixture.debugElement.queryAll(By.css('.buttons .btn'));
    return buttons[buttons.length - 1].nativeElement as HTMLButtonElement;
  }

  it('is hidden until something is asked', () => {
    expect(fixture.debugElement.query(By.css('.dialog'))).toBeNull();
  });

  it('shows title and message and resolves true on confirm', async () => {
    const answer = confirm.ask({ title: 'Delete book', message: 'Sure?' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Delete book');
    confirmButton().click();

    await expectAsync(answer).toBeResolvedTo(true);
  });

  it('resolves false when cancelled via the backdrop', async () => {
    const answer = confirm.ask({ title: 'Delete book', message: 'Sure?' });
    fixture.detectChanges();

    (fixture.debugElement.query(By.css('.backdrop')).nativeElement as HTMLElement).click();

    await expectAsync(answer).toBeResolvedTo(false);
  });

  it('keeps confirm disabled until the required word is typed', () => {
    confirm.ask({ title: 'Delete version', message: 'Sure?', requireWord: 'delete' });
    fixture.detectChanges();

    expect(confirmButton().disabled).toBeTrue();

    const input = fixture.debugElement.query(By.css('.type-word input'))
      .nativeElement as HTMLInputElement;
    input.value = 'delete';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(confirmButton().disabled).toBeFalse();
  });

  it('cancels a displaced request when ask() is called again', async () => {
    const first = confirm.ask({ title: 'First', message: 'A' });
    const second = confirm.ask({ title: 'Second', message: 'B' });
    fixture.detectChanges();

    // The first caller must not be left awaiting forever.
    await expectAsync(first).toBeResolvedTo(false);

    confirmButton().click();
    await expectAsync(second).toBeResolvedTo(true);
  });

  it('does not accept the wrong word', () => {
    confirm.ask({ title: 'Delete version', message: 'Sure?', requireWord: 'delete' });
    fixture.detectChanges();

    const input = fixture.debugElement.query(By.css('.type-word input'))
      .nativeElement as HTMLInputElement;
    input.value = 'yes';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(confirmButton().disabled).toBeTrue();
  });
});

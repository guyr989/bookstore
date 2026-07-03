/// <reference types="jasmine" />
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VersionService } from './version.service';
import { FileVersion } from '../models/file-version';
import { environment } from '../../environments/environment';

describe('VersionService', () => {
  let service: VersionService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/versions`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(VersionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getAll GETs api/versions', () => {
    let result: FileVersion[] = [];
    service.getAll().subscribe(versions => (result = versions));

    const req = http.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([{ number: 1, savedAtUtc: '2026-07-03T10:00:00Z' }]);

    expect(result.length).toBe(1);
    expect(result[0].number).toBe(1);
  });

  it('restore POSTs api/versions/{n}/restore', () => {
    service.restore(3).subscribe();
    const req = http.expectOne(`${base}/3/restore`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('delete DELETEs api/versions/{n}', () => {
    service.delete(3).subscribe();
    const req = http.expectOne(`${base}/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});

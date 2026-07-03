import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FileVersion } from '../models/file-version';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class VersionService {
  private readonly baseUrl = `${environment.apiUrl}/versions`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<FileVersion[]> {
    return this.http.get<FileVersion[]>(this.baseUrl);
  }

  restore(version: number): Observable<void> {
    // Empty-object body (not null): guarantees a Content-Length header so
    // strict HTTP frontends (IIS returns 411 otherwise) always accept it.
    return this.http.post<void>(`${this.baseUrl}/${version}/restore`, {});
  }

  delete(version: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${version}`);
  }
}

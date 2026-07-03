import { Routes } from '@angular/router';
import { BookListComponent } from './components/book-list/book-list.component';
import { BookFormComponent } from './components/book-form/book-form.component';
import { ReportComponent } from './components/report/report.component';
import { VersionsComponent } from './components/versions/versions.component';

export const routes: Routes = [
  { path: '', component: BookListComponent },
  { path: 'add', component: BookFormComponent },
  { path: 'edit/:isbn', component: BookFormComponent },
  { path: 'report', component: ReportComponent },
  { path: 'versions', component: VersionsComponent },
  { path: '**', redirectTo: '' }
];

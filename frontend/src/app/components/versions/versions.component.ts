import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileVersion } from '../../models/file-version';
import { ConfirmService } from '../../services/confirm.service';
import { ToastService } from '../../services/toast.service';
import { VersionService } from '../../services/version.service';

// Version history of the XML data file: every save creates a snapshot the
// owner can restore (rollback) or discard. Restoring stashes the current
// state first, so a rollback is itself undoable.
@Component({
  selector: 'app-versions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './versions.component.html',
  styleUrls: ['./versions.component.css']
})
export class VersionsComponent implements OnInit {
  versions: FileVersion[] = [];
  loading = false;
  busy = false; // a restore/delete is in flight

  constructor(
    private versionService: VersionService,
    private confirm: ConfirmService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.versionService.getAll().subscribe({
      next: versions => {
        // Newest first: the version you most likely want back on top.
        this.versions = [...versions].sort((a, b) => b.number - a.number);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Could not load version history. Is the API running?');
      }
    });
  }

  async restore(version: FileVersion): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Restore version',
      message: `Roll the catalog back to version ${version.number}? ` +
               'The current state is saved as a new version first, so you can undo this.',
      confirmLabel: 'Restore'
    });
    if (!ok) return;

    this.busy = true;
    this.versionService.restore(version.number).subscribe({
      next: () => {
        this.busy = false;
        this.toast.success(`Restored version ${version.number}.`);
        this.load();
      },
      error: err => {
        this.busy = false;
        this.toast.error(err.status === 404
          ? 'That version no longer exists.'
          : 'Restore failed.');
        this.load();
      }
    });
  }

  async remove(version: FileVersion): Promise<void> {
    const ok = await this.confirm.ask({
      title: 'Delete version',
      message: `Permanently delete version ${version.number}? This cannot be undone.`,
      confirmLabel: 'Delete',
      danger: true,
      requireWord: 'delete'
    });
    if (!ok) return;

    this.busy = true;
    this.versionService.delete(version.number).subscribe({
      next: () => {
        this.busy = false;
        this.toast.success(`Deleted version ${version.number}.`);
        this.load();
      },
      error: err => {
        this.busy = false;
        this.toast.error(err.status === 404
          ? 'That version no longer exists.'
          : 'Delete failed.');
        this.load();
      }
    });
  }
}

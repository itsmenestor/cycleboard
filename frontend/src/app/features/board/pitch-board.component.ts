import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { PitchService } from '../../core/services/pitch.service';
import { ToastService } from '../../core/services/toast.service';
import {
  PitchDto, PitchStatus, PITCH_STATUS_LABELS, VALID_NEXT_STATUS,
  CreatePitchRequest, UpdatePitchRequest
} from '../../core/models/pitch.model';

type ModalMode = 'create' | 'edit';

@Component({
  selector: 'app-pitch-board',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="board-page">
      <div class="board-header">
        <div class="board-title">
          <h1>My Cycle Board</h1>
          <span class="pitch-count">{{ pitches().length }} pitch{{ pitches().length !== 1 ? 'es' : '' }}</span>
        </div>
        <button class="btn-primary" (click)="openCreate()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
               stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          New Pitch
        </button>
      </div>

      @if (loading()) {
        <div class="board-loading">
          <div class="spinner" aria-label="Loading board"></div>
          <p>Loading your pitches…</p>
        </div>
      } @else {
        <div class="kanban-board">
          @for (col of columns; track col.status) {
            <div class="kanban-col">
              <div class="col-header">
                <span class="col-dot" [style.background]="col.color"></span>
                <h2>{{ col.label }}</h2>
                <span class="col-count">{{ pitchesByStatus()[col.status].length }}</span>
              </div>
              <div class="col-cards">
                @for (pitch of pitchesByStatus()[col.status]; track pitch.id) {
                  <div class="pitch-card" [class.shipped]="pitch.status === 3">
                    <div class="card-top">
                      <span class="appetite-tag" [class.big]="pitch.appetiteWeeks === 6">
                        {{ pitch.appetiteWeeks }}w
                      </span>
                      @if (pitch.isPublic) {
                        <span class="public-tag" title="Public">
                          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                               stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <circle cx="12" cy="12" r="10"/>
                            <line x1="2" y1="12" x2="22" y2="12"/>
                            <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                          </svg>
                          Public
                        </span>
                      }
                    </div>
                    <h3 class="card-title">{{ pitch.title }}</h3>
                    @if (pitch.problemStatement) {
                      <p class="card-desc">{{ pitch.problemStatement }}</p>
                    }
                    <div class="card-actions">
                      @if (nextStatus(pitch.status) !== null) {
                        <button class="btn-advance" (click)="advancePitch(pitch)"
                                [disabled]="advancing() === pitch.id"
                                [title]="'Advance to ' + statusLabel(nextStatus(pitch.status)!)">
                          @if (advancing() === pitch.id) {
                            <span class="spin-sm"></span>
                          } @else {
                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                 stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                              <polyline points="9 18 15 12 9 6"/>
                            </svg>
                          }
                          {{ statusLabel(nextStatus(pitch.status)!) }}
                        </button>
                      }
                      <div class="card-menu">
                        <button class="icon-btn" (click)="openEdit(pitch)" title="Edit pitch">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                               stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                          </svg>
                        </button>
                        <button class="icon-btn danger" (click)="confirmDelete(pitch)" title="Delete pitch">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                               stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <polyline points="3 6 5 6 21 6"/>
                            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                            <path d="M10 11v6"/><path d="M14 11v6"/>
                            <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                } @empty {
                  <div class="empty-col"><p>No pitches here</p></div>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>

    <!-- Create / Edit Modal -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()">
        <div class="modal" role="dialog"
             [attr.aria-label]="modalMode() === 'create' ? 'Create pitch' : 'Edit pitch'"
             (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>{{ modalMode() === 'create' ? 'New Pitch' : 'Edit Pitch' }}</h2>
            <button class="close-btn" (click)="closeModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>

          <form [formGroup]="pitchForm" (ngSubmit)="submitModal()">
            <div class="field">
              <label for="pitch-title">Title</label>
              <input id="pitch-title" type="text" formControlName="title"
                     placeholder="One-liner that captures the idea" />
              @if (pitchForm.get('title')?.invalid && pitchForm.get('title')?.touched) {
                <span class="error" role="alert">Title must be 3–200 characters</span>
              }
            </div>

            <div class="field">
              <label for="pitch-ps">Problem Statement <span class="optional">(optional)</span></label>
              <textarea id="pitch-ps" formControlName="problemStatement" rows="3"
                        placeholder="Describe the problem and your proposed solution…"></textarea>
              @if (pitchForm.get('problemStatement')?.invalid && pitchForm.get('problemStatement')?.touched) {
                <span class="error" role="alert">Must be at least 10 characters if provided</span>
              }
            </div>

            <div class="field-row">
              <div class="field">
                <label for="pitch-appetite">Appetite</label>
                <select id="pitch-appetite" formControlName="appetiteWeeks">
                  <option value="2">2-Week Small Batch</option>
                  <option value="6">6-Week Big Batch</option>
                </select>
              </div>

              <div class="field toggle-field">
                <label>
                  <input type="checkbox" formControlName="isPublic" />
                  <span>Make public</span>
                </label>
                @if (pitchForm.get('isPublic')?.value && modalMode() === 'create') {
                  <span class="hint">Pitch must be Shaped or later to appear publicly</span>
                }
              </div>
            </div>

            @if (modalError()) {
              <div class="server-error" role="alert">{{ modalError() }}</div>
            }

            <div class="modal-footer">
              <button type="button" class="btn-ghost" (click)="closeModal()">Cancel</button>
              <button type="submit" class="btn-primary" [disabled]="submitting()">
                {{ submitting() ? 'Saving…' : (modalMode() === 'create' ? 'Create Pitch' : 'Save Changes') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Delete Confirmation -->
    @if (deleteTarget()) {
      <div class="modal-backdrop" (click)="cancelDelete()">
        <div class="modal modal-sm" role="alertdialog" aria-label="Confirm delete"
             (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>Delete Pitch?</h2>
          </div>
          <p class="confirm-text">
            "<strong>{{ deleteTarget()!.title }}</strong>" will be permanently removed. This cannot be undone.
          </p>
          <div class="modal-footer">
            <button class="btn-ghost" (click)="cancelDelete()">Cancel</button>
            <button class="btn-danger" (click)="doDelete()" [disabled]="deleting()">
              {{ deleting() ? 'Deleting…' : 'Delete' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .board-page { padding: 28px 24px; max-width: 1300px; margin: 0 auto; }

    .board-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 28px;
      flex-wrap: wrap;
      gap: 12px;
    }
    .board-title { display: flex; align-items: baseline; gap: 10px; }
    .board-title h1 { font-size: 22px; font-weight: 800; color: var(--neutral); }
    .pitch-count { font-size: 13px; color: #9CA3AF; font-weight: 500; }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: 7px;
      padding: 10px 18px;
      background: var(--primary);
      color: #fff;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: opacity .15s;
    }
    .btn-primary:hover:not(:disabled) { opacity: .9; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }

    .board-loading {
      display: flex; flex-direction: column; align-items: center;
      gap: 14px; padding: 80px 24px; color: #6B7280;
    }
    .spinner {
      width: 36px; height: 36px;
      border: 3px solid var(--border);
      border-top-color: var(--primary);
      border-radius: 50%;
      animation: spin .7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .kanban-board {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      align-items: start;
    }

    .kanban-col {
      background: #F3F4F6;
      border-radius: 12px;
      padding: 14px;
      min-height: 200px;
    }

    .col-header { display: flex; align-items: center; gap: 8px; margin-bottom: 14px; }
    .col-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
    .col-header h2 { font-size: 13px; font-weight: 700; color: var(--neutral); flex: 1; }
    .col-count {
      background: #E5E7EB; color: #6B7280;
      border-radius: 10px; padding: 2px 8px;
      font-size: 11px; font-weight: 700;
    }

    .col-cards { display: flex; flex-direction: column; gap: 10px; }

    .pitch-card {
      background: #fff;
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 14px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .pitch-card.shipped { opacity: .75; }

    .card-top { display: flex; align-items: center; gap: 6px; }
    .appetite-tag {
      font-size: 10px; font-weight: 700;
      padding: 2px 8px; border-radius: 10px;
      background: #EFF6FF; color: var(--primary);
      text-transform: uppercase; letter-spacing: .4px;
    }
    .appetite-tag.big { background: #ECFDF5; color: #059669; }

    .public-tag {
      display: flex; align-items: center; gap: 3px;
      font-size: 10px; font-weight: 600;
      color: #6B7280; background: #F3F4F6;
      padding: 2px 7px; border-radius: 10px;
    }

    .card-title { font-size: 14px; font-weight: 700; color: var(--neutral); line-height: 1.4; }
    .card-desc {
      font-size: 12px; color: #6B7280; line-height: 1.5;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .card-actions { display: flex; align-items: center; gap: 6px; margin-top: 2px; }

    .btn-advance {
      flex: 1;
      display: flex; align-items: center; justify-content: center; gap: 5px;
      padding: 6px 10px;
      background: #EFF6FF; color: var(--primary);
      border: 1px solid #BFDBFE;
      border-radius: 6px;
      font-size: 11px; font-weight: 700;
      cursor: pointer; transition: background .15s;
      text-transform: uppercase; letter-spacing: .3px;
    }
    .btn-advance:hover:not(:disabled) { background: #DBEAFE; }
    .btn-advance:disabled { opacity: .6; cursor: not-allowed; }

    .card-menu { display: flex; gap: 4px; }
    .icon-btn {
      width: 28px; height: 28px;
      display: flex; align-items: center; justify-content: center;
      border: 1px solid var(--border); border-radius: 6px;
      background: #fff; color: #9CA3AF;
      cursor: pointer; transition: all .15s;
    }
    .icon-btn:hover { background: #F3F4F6; color: var(--neutral); }
    .icon-btn.danger:hover { background: #FEF2F2; border-color: #FECACA; color: #DC2626; }

    .spin-sm {
      width: 11px; height: 11px;
      border: 2px solid #BFDBFE; border-top-color: var(--primary);
      border-radius: 50%; animation: spin .6s linear infinite;
    }

    .empty-col {
      text-align: center; padding: 24px 12px;
      font-size: 12px; color: #D1D5DB;
      border: 1.5px dashed #E5E7EB; border-radius: 8px;
    }

    /* Modal */
    .modal-backdrop {
      position: fixed; inset: 0;
      background: rgba(0,0,0,.45);
      display: flex; align-items: center; justify-content: center;
      padding: 24px; z-index: 50;
      animation: fadeIn .15s ease;
    }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

    .modal {
      background: #fff; border-radius: 14px;
      width: 100%; max-width: 520px;
      padding: 28px;
      animation: slideUp .2s ease;
    }
    .modal-sm { max-width: 400px; }
    @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: none; opacity: 1; } }

    .modal-header {
      display: flex; justify-content: space-between;
      align-items: center; margin-bottom: 22px;
    }
    .modal-header h2 { font-size: 18px; font-weight: 800; color: var(--neutral); }
    .close-btn {
      width: 32px; height: 32px;
      display: flex; align-items: center; justify-content: center;
      border: none; background: #F3F4F6; border-radius: 8px;
      color: #6B7280; cursor: pointer; transition: background .15s;
    }
    .close-btn:hover { background: #E5E7EB; }

    .field { margin-bottom: 16px; }
    .field label { display: block; font-size: 13px; font-weight: 600; color: var(--neutral); margin-bottom: 6px; }
    .optional { font-weight: 400; color: #9CA3AF; }
    .field input[type="text"],
    .field textarea,
    .field select {
      width: 100%; padding: 10px 12px;
      border: 1px solid var(--border); border-radius: 8px;
      font-size: 14px; color: var(--neutral);
      outline: none; transition: border-color .15s;
      background: #fff; resize: vertical;
    }
    .field input[type="text"]:focus,
    .field textarea:focus,
    .field select:focus { border-color: var(--primary); box-shadow: 0 0 0 3px #DBEAFE; }
    .error { font-size: 12px; color: #DC2626; margin-top: 4px; display: block; }

    .field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
    .toggle-field label { display: flex; align-items: center; gap: 8px; cursor: pointer; }
    .toggle-field input[type="checkbox"] { width: 16px; height: 16px; accent-color: var(--primary); }
    .hint { font-size: 11px; color: #9CA3AF; margin-top: 4px; display: block; line-height: 1.4; }

    .server-error {
      background: #FEF2F2; border: 1px solid #FECACA;
      border-radius: 8px; padding: 10px 14px;
      font-size: 13px; color: #991B1B; margin-bottom: 16px;
    }

    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }

    .btn-ghost {
      padding: 10px 18px; background: none;
      border: 1.5px solid var(--border); border-radius: 8px;
      font-size: 14px; font-weight: 600; color: #6B7280;
      cursor: pointer; transition: background .15s;
    }
    .btn-ghost:hover { background: #F9FAFB; }

    .btn-danger {
      padding: 10px 18px; background: #DC2626;
      color: #fff; border: none; border-radius: 8px;
      font-size: 14px; font-weight: 600;
      cursor: pointer; transition: opacity .15s;
    }
    .btn-danger:hover:not(:disabled) { opacity: .88; }
    .btn-danger:disabled { opacity: .6; cursor: not-allowed; }

    .confirm-text { font-size: 14px; color: #6B7280; line-height: 1.6; margin-bottom: 20px; }

    @media (max-width: 900px) { .kanban-board { grid-template-columns: repeat(2, 1fr); } }
    @media (max-width: 560px) {
      .kanban-board { grid-template-columns: 1fr; }
      .field-row { grid-template-columns: 1fr; }
      .board-page { padding: 16px; }
    }
  `]
})
export class PitchBoardComponent implements OnInit {
  private readonly pitchService = inject(PitchService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly pitches = signal<PitchDto[]>([]);

  readonly pitchesByStatus = computed(() => {
    const map: Record<number, PitchDto[]> = { 0: [], 1: [], 2: [], 3: [] };
    for (const p of this.pitches()) {
      map[p.status as number]?.push(p);
    }
    return map;
  });

  readonly showModal = signal(false);
  readonly modalMode = signal<ModalMode>('create');
  readonly modalError = signal('');
  readonly submitting = signal(false);

  readonly deleteTarget = signal<PitchDto | null>(null);
  readonly deleting = signal(false);
  readonly advancing = signal<string | null>(null);

  private editingPitch: PitchDto | null = null;

  readonly columns = [
    { status: PitchStatus.Draft,   label: 'Draft',   color: '#9CA3AF' },
    { status: PitchStatus.Shaped,  label: 'Shaped',  color: '#2563EB' },
    { status: PitchStatus.Betted,  label: 'Betted',  color: '#D97706' },
    { status: PitchStatus.Shipped, label: 'Shipped', color: '#10B981' },
  ];

  readonly pitchForm = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    problemStatement: ['', (control: import('@angular/forms').AbstractControl) => {
      const v: string = control.value ?? '';
      if (v.trim().length > 0 && v.trim().length < 10) return { minlength: true };
      return null;
    }],
    appetiteWeeks: [2, Validators.required],
    isPublic: [false]
  });

  ngOnInit(): void {
    this.loadPitches();
  }

  loadPitches(): void {
    this.loading.set(true);
    this.pitchService.getMyPitches().subscribe({
      next: (data) => { this.pitches.set(data); this.loading.set(false); },
      error: () => { this.toast.show('Failed to load pitches.', 'error'); this.loading.set(false); }
    });
  }

  statusLabel(status: PitchStatus): string {
    return PITCH_STATUS_LABELS[status];
  }

  nextStatus(status: PitchStatus): PitchStatus | null {
    const next = VALID_NEXT_STATUS[status];
    return next !== undefined ? next as PitchStatus : null;
  }

  openCreate(): void {
    this.editingPitch = null;
    this.pitchForm.reset({ title: '', problemStatement: '', appetiteWeeks: 2, isPublic: false });
    this.modalError.set('');
    this.modalMode.set('create');
    this.showModal.set(true);
  }

  openEdit(pitch: PitchDto): void {
    this.editingPitch = pitch;
    this.pitchForm.reset({
      title: pitch.title,
      problemStatement: pitch.problemStatement ?? '',
      appetiteWeeks: pitch.appetiteWeeks,
      isPublic: pitch.isPublic
    });
    this.modalError.set('');
    this.modalMode.set('edit');
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.editingPitch = null;
  }

  submitModal(): void {
    if (this.pitchForm.invalid) {
      this.pitchForm.markAllAsTouched();
      return;
    }
    this.modalMode() === 'create' ? this.createPitch() : this.savePitch();
  }

  private createPitch(): void {
    this.submitting.set(true);
    this.modalError.set('');
    const v = this.pitchForm.value;
    const req: CreatePitchRequest = {
      title: v.title!,
      problemStatement: v.problemStatement ?? '',
      appetiteWeeks: Number(v.appetiteWeeks),
      isPublic: v.isPublic ?? false
    };
    this.pitchService.create(req).subscribe({
      next: (created) => {
        this.pitches.update(ps => [created, ...ps]);
        this.submitting.set(false);
        this.closeModal();
        this.toast.show('Pitch created!', 'success');
      },
      error: (err: { error?: { detail?: string } }) => {
        this.submitting.set(false);
        this.modalError.set(err.error?.detail ?? 'Failed to create pitch.');
      }
    });
  }

  private savePitch(): void {
    if (!this.editingPitch) return;
    this.submitting.set(true);
    this.modalError.set('');
    const v = this.pitchForm.value;
    const req: UpdatePitchRequest = {
      title: v.title!,
      problemStatement: v.problemStatement ?? '',
      appetiteWeeks: Number(v.appetiteWeeks),
      status: this.editingPitch.status,
      isPublic: v.isPublic ?? false
    };
    this.pitchService.update(this.editingPitch.id, req).subscribe({
      next: (updated) => {
        this.pitches.update(ps => ps.map(p => p.id === updated.id ? updated : p));
        this.submitting.set(false);
        this.closeModal();
        this.toast.show('Pitch updated!', 'success');
      },
      error: (err: { error?: { detail?: string } }) => {
        this.submitting.set(false);
        this.modalError.set(err.error?.detail ?? 'Failed to update pitch.');
      }
    });
  }

  advancePitch(pitch: PitchDto): void {
    const next = this.nextStatus(pitch.status);
    if (next === null) return;
    this.advancing.set(pitch.id);
    const req: UpdatePitchRequest = {
      title: pitch.title,
      problemStatement: pitch.problemStatement ?? '',
      appetiteWeeks: pitch.appetiteWeeks,
      isPublic: pitch.isPublic,
      status: next
    };
    this.pitchService.update(pitch.id, req).subscribe({
      next: (updated) => {
        this.pitches.update(ps => ps.map(p => p.id === updated.id ? updated : p));
        this.advancing.set(null);
        this.toast.show(`"${pitch.title}" moved to ${this.statusLabel(next)}.`, 'success');
      },
      error: (err: { error?: { detail?: string } }) => {
        this.advancing.set(null);
        this.toast.show(err.error?.detail ?? 'Failed to advance pitch.', 'error');
      }
    });
  }

  confirmDelete(pitch: PitchDto): void {
    this.deleteTarget.set(pitch);
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  doDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deleting.set(true);
    this.pitchService.delete(target.id).subscribe({
      next: () => {
        this.pitches.update(ps => ps.filter(p => p.id !== target.id));
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.toast.show(`"${target.title}" deleted.`, 'success');
      },
      error: () => {
        this.deleting.set(false);
        this.toast.show('Failed to delete pitch.', 'error');
      }
    });
  }
}

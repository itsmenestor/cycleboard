import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PitchService } from '../../core/services/pitch.service';
import { PitchDto, PITCH_STATUS_LABELS } from '../../core/models/pitch.model';
import { AuthService } from '../../core/services/auth.service';

type AppetiteFilter = 'all' | '2' | '6';

@Component({
  selector: 'app-public-pitches',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="showcase-page">
      <div class="showcase-hero">
        <h1>Public Pitches</h1>
        <p>Explore Shape Up pitches shared by the community</p>
      </div>

      <div class="showcase-container">
        <div class="filter-bar">
          <button class="filter-pill" [class.active]="appetiteFilter() === 'all'" (click)="setFilter('all')">
            All
          </button>
          <button class="filter-pill" [class.active]="appetiteFilter() === '2'" (click)="setFilter('2')">
            2-Week Small Batch
          </button>
          <button class="filter-pill" [class.active]="appetiteFilter() === '6'" (click)="setFilter('6')">
            6-Week Big Batch
          </button>
        </div>

        @if (loading()) {
          <div class="state-container">
            <div class="spinner" aria-label="Loading pitches"></div>
            <p>Loading pitches…</p>
          </div>
        } @else if (error()) {
          <div class="state-container">
            <p class="error-msg">{{ error() }}</p>
            <button class="btn-outline" (click)="load()">Try again</button>
          </div>
        } @else if (filtered().length === 0) {
          <div class="state-container empty">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
            </svg>
            <p>No public pitches found for this filter.</p>
            @if (!isLoggedIn()) {
              <a routerLink="/register" class="btn-primary">Share your first pitch</a>
            }
          </div>
        } @else {
          <div class="pitch-grid">
            @for (pitch of filtered(); track pitch.id) {
              <article class="pitch-card">
                <div class="card-header">
                  <span class="appetite-badge" [class.appetite-6]="pitch.appetiteWeeks === 6">
                    {{ pitch.appetiteWeeks }}-Week
                  </span>
                  <span class="status-badge status-{{ pitch.status }}">
                    {{ statusLabel(pitch.status) }}
                  </span>
                </div>
                <h2 class="card-title">{{ pitch.title }}</h2>
                @if (pitch.problemStatement) {
                  <p class="card-desc">{{ pitch.problemStatement }}</p>
                }
                <div class="card-footer">
                  <span class="author">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                      <circle cx="12" cy="7" r="4"/>
                    </svg>
                    {{ pitch.authorName }}
                  </span>
                  <time class="date" [dateTime]="pitch.createdAt">
                    {{ formatDate(pitch.createdAt) }}
                  </time>
                </div>
              </article>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .showcase-page { min-height: 100vh; background: var(--bg); }

    .showcase-hero {
      background: linear-gradient(135deg, var(--primary) 0%, #1D4ED8 100%);
      color: #fff;
      text-align: center;
      padding: 56px 24px 48px;
    }
    .showcase-hero h1 { font-size: 32px; font-weight: 800; margin-bottom: 8px; }
    .showcase-hero p { font-size: 16px; opacity: .85; }

    .showcase-container { max-width: 1100px; margin: 0 auto; padding: 32px 24px; }

    .filter-bar { display: flex; gap: 10px; margin-bottom: 28px; flex-wrap: wrap; }
    .filter-pill {
      padding: 8px 18px;
      border: 1.5px solid var(--border);
      border-radius: 20px;
      background: #fff;
      font-size: 13px;
      font-weight: 600;
      color: #6B7280;
      cursor: pointer;
      transition: all .15s;
    }
    .filter-pill:hover { border-color: var(--primary); color: var(--primary); }
    .filter-pill.active { background: var(--primary); border-color: var(--primary); color: #fff; }

    .state-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
      padding: 80px 24px;
      text-align: center;
      color: #6B7280;
    }
    .state-container.empty svg { color: #D1D5DB; }
    .error-msg { color: #DC2626; font-size: 14px; }

    .spinner {
      width: 36px; height: 36px;
      border: 3px solid var(--border);
      border-top-color: var(--primary);
      border-radius: 50%;
      animation: spin .7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .pitch-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 20px;
    }

    .pitch-card {
      background: #fff;
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 10px;
      transition: box-shadow .15s, transform .15s;
    }
    .pitch-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,.08); transform: translateY(-2px); }

    .card-header { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }

    .appetite-badge {
      padding: 3px 10px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 700;
      background: #EFF6FF;
      color: var(--primary);
      text-transform: uppercase;
      letter-spacing: .4px;
    }
    .appetite-badge.appetite-6 { background: #ECFDF5; color: #059669; }

    .status-badge {
      padding: 3px 10px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: .4px;
    }
    .status-badge.status-0 { background: #F3F4F6; color: #6B7280; }
    .status-badge.status-1 { background: #EFF6FF; color: #2563EB; }
    .status-badge.status-2 { background: #FEF3C7; color: #D97706; }
    .status-badge.status-3 { background: #D1FAE5; color: #065F46; }

    .card-title { font-size: 16px; font-weight: 700; color: var(--neutral); line-height: 1.4; }

    .card-desc {
      font-size: 13px;
      color: #6B7280;
      line-height: 1.6;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .card-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: auto;
      padding-top: 10px;
      border-top: 1px solid var(--border);
      font-size: 12px;
      color: #9CA3AF;
    }
    .author { display: flex; align-items: center; gap: 5px; }

    .btn-primary {
      padding: 10px 22px;
      background: var(--primary);
      color: #fff;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      text-decoration: none;
      transition: opacity .15s;
    }
    .btn-primary:hover { opacity: .9; }

    .btn-outline {
      padding: 9px 20px;
      background: #fff;
      color: var(--primary);
      border: 1.5px solid var(--primary);
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: background .15s;
    }
    .btn-outline:hover { background: #EFF6FF; }

    @media (max-width: 600px) {
      .showcase-hero { padding: 36px 16px 32px; }
      .showcase-hero h1 { font-size: 24px; }
      .pitch-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class PublicPitchesComponent implements OnInit {
  private readonly pitchService = inject(PitchService);
  private readonly authService = inject(AuthService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly pitches = signal<PitchDto[]>([]);
  readonly appetiteFilter = signal<AppetiteFilter>('all');

  readonly isLoggedIn = computed(() => !!this.authService.currentUser());

  readonly filtered = computed(() => {
    const f = this.appetiteFilter();
    if (f === 'all') return this.pitches();
    return this.pitches().filter(p => p.appetiteWeeks === Number(f));
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.pitchService.getPublic().subscribe({
      next: (data) => { this.pitches.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load pitches. Please try again.'); this.loading.set(false); }
    });
  }

  setFilter(f: AppetiteFilter): void {
    this.appetiteFilter.set(f);
  }

  statusLabel(status: number): string {
    return PITCH_STATUS_LABELS[status as keyof typeof PITCH_STATUS_LABELS] ?? 'Unknown';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}

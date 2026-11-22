import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar">
      <div class="navbar-inner">
        <a routerLink="/" class="brand">
          <div class="brand-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                 stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M23 4v6h-6"/>
              <path d="M1 20v-6h6"/>
              <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
            </svg>
          </div>
          <span class="brand-name"><strong>Cycle</strong>board</span>
        </a>
        <div class="nav-links">
          <a routerLink="/explore" routerLinkActive="active" class="nav-link">Explore</a>
          @if (isLoggedIn()) {
            <a routerLink="/board" routerLinkActive="active" class="nav-link">My Board</a>
            <span class="user-name">{{ userName() }}</span>
            <button class="btn-logout" (click)="logout()" type="button">Sign out</button>
          } @else {
            <a routerLink="/login" routerLinkActive="active" class="nav-link">Login</a>
            <a routerLink="/register" class="btn-primary-small">Get Started</a>
          }
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .navbar { position: sticky; top: 0; z-index: 100; background: #fff; border-bottom: 1px solid var(--border); height: 60px; }
    .navbar-inner { max-width: 1200px; margin: 0 auto; padding: 0 24px; height: 100%; display: flex; align-items: center; justify-content: space-between; }
    .brand { display: flex; align-items: center; gap: 8px; text-decoration: none; color: var(--neutral); }
    .brand-icon { width: 32px; height: 32px; background: var(--primary); border-radius: 8px; display: flex; align-items: center; justify-content: center; color: #fff; }
    .brand-name { font-size: 16px; color: var(--neutral); font-weight: 400; }
    .brand-name strong { font-weight: 700; }
    .nav-links { display: flex; align-items: center; gap: 20px; }
    .nav-link { text-decoration: none; color: #6B7280; font-size: 14px; font-weight: 500; transition: color .15s; }
    .nav-link:hover, .nav-link.active { color: var(--primary); }
    .user-name { font-size: 14px; color: #6B7280; font-weight: 500; }
    .btn-logout { background: none; border: 1px solid var(--border); color: #6B7280; font-size: 13px; font-weight: 500; padding: 6px 14px; border-radius: 6px; cursor: pointer; transition: all .15s; }
    .btn-logout:hover { border-color: #9CA3AF; color: var(--neutral); }
    .btn-primary-small { background: var(--primary); color: #fff; text-decoration: none; font-size: 13px; font-weight: 600; padding: 7px 16px; border-radius: 6px; transition: opacity .15s; }
    .btn-primary-small:hover { opacity: .9; }
  `]
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoggedIn = computed(() => !!this.authService.currentUser());
  readonly userName = computed(() => this.authService.currentUser()?.fullName ?? '');

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}

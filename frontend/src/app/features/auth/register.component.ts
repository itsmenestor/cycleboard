import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-header">
          <div class="brand-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M23 4v6h-6"/>
              <path d="M1 20v-6h6"/>
              <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
            </svg>
          </div>
          <h1>Create account</h1>
          <p>Start tracking your Shape Up cycles</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="field">
            <label for="fullName">Full name</label>
            <input id="fullName" type="text" formControlName="fullName"
                   placeholder="Alex Rivera" autocomplete="name" />
            @if (form.get('fullName')?.invalid && form.get('fullName')?.touched) {
              <span class="error" role="alert">Full name must be 2–100 characters</span>
            }
          </div>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" type="email" formControlName="email"
                   placeholder="you@example.com" autocomplete="email" />
            @if (form.get('email')?.invalid && form.get('email')?.touched) {
              <span class="error" role="alert">Valid email required</span>
            }
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input id="password" type="password" formControlName="password"
                   placeholder="Min. 8 characters" autocomplete="new-password" />
            @if (form.get('password')?.invalid && form.get('password')?.touched) {
              <span class="error" role="alert">Password must be at least 8 characters</span>
            }
          </div>

          @if (serverError()) {
            <div class="server-error" role="alert">{{ serverError() }}</div>
          }

          <button type="submit" class="btn-primary" [disabled]="loading()">
            {{ loading() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>

        <p class="auth-footer">
          Already have an account? <a routerLink="/login">Sign in</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-page { min-height: 100vh; background: var(--bg); display: flex; align-items: center; justify-content: center; padding: 24px; }
    .auth-card { background: #fff; border: 1px solid var(--border); border-radius: 12px; padding: 40px; width: 100%; max-width: 400px; }
    .auth-header { text-align: center; margin-bottom: 28px; }
    .brand-icon { width: 48px; height: 48px; background: var(--primary); border-radius: 12px; display: flex; align-items: center; justify-content: center; color: #fff; margin: 0 auto 16px; }
    .auth-header h1 { font-size: 22px; font-weight: 700; color: var(--neutral); margin-bottom: 6px; }
    .auth-header p { font-size: 14px; color: #6B7280; }
    .field { margin-bottom: 16px; }
    .field label { display: block; font-size: 13px; font-weight: 600; color: var(--neutral); margin-bottom: 6px; }
    .field input { width: 100%; padding: 10px 12px; border: 1px solid var(--border); border-radius: 8px; font-size: 14px; color: var(--neutral); outline: none; transition: border-color .15s; }
    .field input:focus { border-color: var(--primary); box-shadow: 0 0 0 3px #DBEAFE; }
    .error { font-size: 12px; color: #DC2626; margin-top: 4px; display: block; }
    .server-error { background: #FEF2F2; border: 1px solid #FECACA; border-radius: 8px; padding: 10px 14px; font-size: 13px; color: #991B1B; margin-bottom: 16px; }
    .btn-primary { width: 100%; padding: 11px; background: var(--primary); color: #fff; border: none; border-radius: 8px; font-size: 15px; font-weight: 600; cursor: pointer; transition: opacity .15s; margin-top: 8px; }
    .btn-primary:hover:not(:disabled) { opacity: .9; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }
    .auth-footer { text-align: center; font-size: 14px; color: #6B7280; margin-top: 20px; }
    .auth-footer a { color: var(--primary); text-decoration: none; font-weight: 600; }
    .auth-footer a:hover { text-decoration: underline; }
  `]
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly loading = signal(false);
  readonly serverError = signal('');

  readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.serverError.set('');
    const { email, fullName, password } = this.form.value;
    this.authService.register({ email: email!, fullName: fullName!, password: password! }).subscribe({
      next: () => {
        this.toast.show('Account created! Welcome to Hora.', 'success');
        this.router.navigate(['/board']);
      },
      error: (err: { error?: { detail?: string } }) => {
        this.loading.set(false);
        this.serverError.set(err.error?.detail ?? 'Registration failed. Please try again.');
      }
    });
  }
}

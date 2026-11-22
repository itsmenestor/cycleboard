import { Component, OnInit, signal, inject } from '@angular/core';
import { ToastService, Toast } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="toast-container">
      @for (toast of toasts(); track toast.id) {
        <div class="toast" [class]="'toast-' + toast.type" (click)="dismiss(toast.id)" role="alert">
          {{ toast.message }}
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .toast {
      padding: 12px 20px;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      max-width: 360px;
      animation: slideIn .2s ease-out;
      box-shadow: 0 4px 12px rgba(0,0,0,.12);
    }
    .toast-success { background: #065F46; color: #fff; }
    .toast-error { background: #991B1B; color: #fff; }
    .toast-info { background: #1E40AF; color: #fff; }
    @keyframes slideIn {
      from { transform: translateX(100%); opacity: 0; }
      to { transform: translateX(0); opacity: 1; }
    }
  `]
})
export class ToastComponent implements OnInit {
  private readonly toastService = inject(ToastService);
  readonly toasts = signal<Toast[]>([]);

  ngOnInit(): void {
    this.toastService.toasts$.subscribe(t => this.toasts.set(t));
  }

  dismiss(id: number): void {
    this.toastService.dismiss(id);
  }
}

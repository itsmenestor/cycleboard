import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/board', pathMatch: 'full' },
  {
    path: 'explore',
    loadComponent: () => import('./features/public-showcase/public-pitches.component').then(m => m.PublicPitchesComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'board',
    loadComponent: () => import('./features/board/pitch-board.component').then(m => m.PitchBoardComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/board' }
];

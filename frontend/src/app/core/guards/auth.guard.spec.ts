import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { vi } from 'vitest';
import { Component } from '@angular/core';
import { authGuard } from './auth.guard';

@Component({ template: '', standalone: true })
class StubComponent {}

const mockRoute = {} as ActivatedRouteSnapshot;
const mockState = {} as RouterStateSnapshot;

describe('authGuard', () => {
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', component: StubComponent }]),
      ]
    });
    router = TestBed.inject(Router);
  });

  afterEach(() => localStorage.clear());

  it('should allow navigation (return true) when a token is present', () => {
    localStorage.setItem('cb_token', 'valid-jwt');

    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

    expect(result).toBe(true);
  });

  it('should deny navigation (return false) when no token is stored', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

    expect(result).toBe(false);
  });

  it('should navigate to /login when not authenticated', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');

    TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('should not redirect when user is authenticated', () => {
    localStorage.setItem('cb_token', 'valid-jwt');
    const navigateSpy = vi.spyOn(router, 'navigate');

    TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

    expect(navigateSpy).not.toHaveBeenCalled();
  });
});

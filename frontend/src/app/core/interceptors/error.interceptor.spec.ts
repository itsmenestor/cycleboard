import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { vi } from 'vitest';
import { Component } from '@angular/core';
import { errorInterceptor } from './error.interceptor';

@Component({ template: '', standalone: true })
class StubComponent {}

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([{ path: 'login', component: StubComponent }]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should pass through successful responses unchanged', () => {
    let result: unknown;
    http.get('/api/pitches').subscribe(r => (result = r));

    const req = httpMock.expectOne('/api/pitches');
    req.flush([{ id: '1' }]);

    expect(result).toEqual([{ id: '1' }]);
  });

  it('should logout and redirect to /login on 401', () => {
    localStorage.setItem('cb_token', 'expired-token');
    const navigateSpy = vi.spyOn(router, 'navigate');

    http.get('/api/pitches').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/pitches');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('cb_token')).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('should re-throw the error after handling 401', () => {
    let caughtStatus: number | undefined;
    http.get('/api/pitches').subscribe({ error: (err) => (caughtStatus = err.status) });

    const req = httpMock.expectOne('/api/pitches');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(caughtStatus).toBe(401);
  });

  it('should propagate non-401 errors without redirecting', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    let caughtStatus: number | undefined;

    http.get('/api/pitches').subscribe({ error: (err) => (caughtStatus = err.status) });

    const req = httpMock.expectOne('/api/pitches');
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });

    expect(caughtStatus).toBe(404);
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});

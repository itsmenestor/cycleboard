import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { AuthService } from './auth.service';
import { AuthResponse } from '../models/auth.model';

const mockUser = { id: 'u1', email: 'test@test.com', fullName: 'Test User', role: 'Lead' };
const mockAuthResponse: AuthResponse = { token: 'test-jwt', user: mockUser };

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('isLoggedIn() returns false when localStorage is empty', () => {
    expect(service.isLoggedIn()).toBe(false);
  });

  it('getToken() returns null when no token stored', () => {
    expect(service.getToken()).toBeNull();
  });

  it('currentUser signal is null on init with empty storage', () => {
    expect(service.currentUser()).toBeNull();
  });

  it('login() POSTs to /auth/login and persists session', () => {
    service.login({ email: 'test@test.com', password: 'pass123' }).subscribe(res => {
      expect(res.token).toBe('test-jwt');
      expect(res.user.email).toBe('test@test.com');
    });

    const req = httpMock.expectOne(r => r.url.includes('/auth/login'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'test@test.com', password: 'pass123' });
    req.flush(mockAuthResponse);

    expect(service.getToken()).toBe('test-jwt');
    expect(service.isLoggedIn()).toBe(true);
    expect(service.currentUser()).toEqual(mockUser);
  });

  it('register() POSTs to /auth/register and persists session', () => {
    service.register({ email: 'new@test.com', fullName: 'New User', password: 'pass123' }).subscribe();

    const req = httpMock.expectOne(r => r.url.includes('/auth/register'));
    expect(req.request.method).toBe('POST');
    req.flush(mockAuthResponse);

    expect(service.isLoggedIn()).toBe(true);
    expect(service.currentUser()).toEqual(mockUser);
  });

  it('logout() clears token, user key, and resets currentUser signal', () => {
    service.login({ email: 'test@test.com', password: 'pass' }).subscribe();
    httpMock.expectOne(r => r.url.includes('/auth/login')).flush(mockAuthResponse);

    service.logout();

    expect(service.getToken()).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('me() sends GET to /auth/me', () => {
    service.me().subscribe(res => {
      expect(res.email).toBe('test@test.com');
    });

    const req = httpMock.expectOne(r => r.url.includes('/auth/me'));
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'u1', email: 'test@test.com', fullName: 'Test User', role: 'Lead', createdAt: '2024-01-01' });
  });

  it('currentUser signal restores from localStorage on service init', () => {
    localStorage.setItem('cb_user', JSON.stringify(mockUser));
    // Re-create service to trigger loadUserFromStorage
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });
    const freshService = TestBed.inject(AuthService);
    expect(freshService.currentUser()).toEqual(mockUser);
  });
});

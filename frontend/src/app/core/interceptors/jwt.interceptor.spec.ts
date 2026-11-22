import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { jwtInterceptor } from './jwt.interceptor';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should attach Bearer token header when a token is in storage', () => {
    localStorage.setItem('cb_token', 'my-test-jwt');

    http.get('/api/pitches').subscribe();

    const req = httpMock.expectOne('/api/pitches');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-test-jwt');
    req.flush([]);
  });

  it('should not attach Authorization header when no token is stored', () => {
    http.get('/api/pitches').subscribe();

    const req = httpMock.expectOne('/api/pitches');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('should not modify the original request URL or method', () => {
    localStorage.setItem('cb_token', 'token-123');

    http.post('/api/auth/login', { email: 'a@b.com', password: 'x' }).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.url).toBe('/api/auth/login');
    req.flush({});
  });
});

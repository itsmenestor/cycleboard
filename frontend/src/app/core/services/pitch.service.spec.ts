import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PitchService } from './pitch.service';
import { PitchDto } from '../models/pitch.model';

const mockPitch: PitchDto = {
  id: 'pitch-1',
  userId: 'u1',
  authorName: 'Test Author',
  title: 'My Pitch',
  problemStatement: 'A real problem',
  appetiteWeeks: 2,
  status: 0,
  statusName: 'Draft',
  isPublic: false,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

describe('PitchService', () => {
  let service: PitchService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ]
    });
    service = TestBed.inject(PitchService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getPublic() sends GET to /pitches/public with no params', () => {
    service.getPublic().subscribe(pitches => {
      expect(pitches.length).toBe(1);
    });

    const req = httpMock.expectOne(r => r.url.includes('/pitches/public') && !r.params.has('appetite'));
    expect(req.request.method).toBe('GET');
    req.flush([mockPitch]);
  });

  it('getPublic(2) includes appetite=2 query param', () => {
    service.getPublic(2).subscribe();

    const req = httpMock.expectOne(r => r.url.includes('/pitches/public'));
    expect(req.request.params.get('appetite')).toBe('2');
    req.flush([mockPitch]);
  });

  it('getPublic(6) includes appetite=6 query param', () => {
    service.getPublic(6).subscribe();

    const req = httpMock.expectOne(r => r.url.includes('/pitches/public'));
    expect(req.request.params.get('appetite')).toBe('6');
    req.flush([]);
  });

  it('getMyPitches() sends GET to /pitches with no status param', () => {
    service.getMyPitches().subscribe();

    const req = httpMock.expectOne(r => r.url.endsWith('/pitches') && !r.params.has('status'));
    expect(req.request.method).toBe('GET');
    req.flush([mockPitch]);
  });

  it('getMyPitches(1) includes status query param', () => {
    service.getMyPitches(1).subscribe();

    const req = httpMock.expectOne(r => r.url.endsWith('/pitches') || r.url.includes('/pitches?'));
    expect(req.request.params.get('status')).toBe('1');
    req.flush([]);
  });

  it('getById() sends GET to /pitches/:id', () => {
    service.getById('pitch-1').subscribe(p => {
      expect(p.id).toBe('pitch-1');
    });

    const req = httpMock.expectOne(r => r.url.endsWith('/pitches/pitch-1'));
    expect(req.request.method).toBe('GET');
    req.flush(mockPitch);
  });

  it('create() sends POST to /pitches with correct body', () => {
    const createReq = { title: 'New Pitch', problemStatement: 'Some problem', appetiteWeeks: 6, isPublic: false };
    service.create(createReq).subscribe(p => {
      expect(p.title).toBe('My Pitch');
    });

    const req = httpMock.expectOne(r => r.url.endsWith('/pitches'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(mockPitch);
  });

  it('update() sends PUT to /pitches/:id with correct body', () => {
    const updateReq = { title: 'Updated', problemStatement: 'Updated problem', appetiteWeeks: 2, status: 1, isPublic: true };
    service.update('pitch-1', updateReq).subscribe();

    const req = httpMock.expectOne(r => r.url.endsWith('/pitches/pitch-1'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateReq);
    req.flush({ ...mockPitch, status: 1 });
  });

  it('delete() sends DELETE to /pitches/:id', () => {
    service.delete('pitch-1').subscribe();

    const req = httpMock.expectOne(r => r.url.endsWith('/pitches/pitch-1'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});

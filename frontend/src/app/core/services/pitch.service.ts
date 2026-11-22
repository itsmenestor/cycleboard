import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePitchRequest, PitchDto, UpdatePitchRequest } from '../models/pitch.model';

@Injectable({ providedIn: 'root' })
export class PitchService {
  private readonly apiUrl = `${environment.apiUrl}/pitches`;

  constructor(private http: HttpClient) {}

  getPublic(appetite?: number): Observable<PitchDto[]> {
    let params = new HttpParams();
    if (appetite !== undefined) {
      params = params.set('appetite', appetite);
    }
    return this.http.get<PitchDto[]>(`${this.apiUrl}/public`, { params });
  }

  getMyPitches(status?: number): Observable<PitchDto[]> {
    let params = new HttpParams();
    if (status !== undefined) {
      params = params.set('status', status);
    }
    return this.http.get<PitchDto[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<PitchDto> {
    return this.http.get<PitchDto>(`${this.apiUrl}/${id}`);
  }

  create(request: CreatePitchRequest): Observable<PitchDto> {
    return this.http.post<PitchDto>(this.apiUrl, request);
  }

  update(id: string, request: UpdatePitchRequest): Observable<PitchDto> {
    return this.http.put<PitchDto>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

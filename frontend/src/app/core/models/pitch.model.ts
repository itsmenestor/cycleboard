export enum PitchStatus {
  Draft = 0,
  Shaped = 1,
  Betted = 2,
  Shipped = 3
}

export const PITCH_STATUS_LABELS: Record<number, string> = {
  0: 'Draft',
  1: 'Shaped',
  2: 'Betted',
  3: 'Shipped'
};

export const VALID_NEXT_STATUS: Record<number, number | null> = {
  0: 1,
  1: 2,
  2: 3,
  3: null
};

export interface PitchDto {
  id: string;
  userId: string;
  authorName: string;
  title: string;
  problemStatement: string;
  appetiteWeeks: number;
  status: number;
  statusName: string;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePitchRequest {
  title: string;
  problemStatement: string;
  appetiteWeeks: number;
  isPublic: boolean;
}

export interface UpdatePitchRequest {
  title: string;
  problemStatement: string;
  appetiteWeeks: number;
  status: number;  // always required; send current status when not advancing
  isPublic: boolean;
}

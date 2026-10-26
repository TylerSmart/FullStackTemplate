export interface AccountDto {
  id: string;
  email: string;
  username: string;
  hasPassword: boolean;
  externalProvider: string | null;
}

export interface UpdateProfileRequest {
  username: string;
  email: string;
  currentPassword?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword?: string | null;
  newPassword: string;
}

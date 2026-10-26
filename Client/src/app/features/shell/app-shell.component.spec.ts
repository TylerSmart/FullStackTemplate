import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_NAME } from '../../app.branding';
import { AccountService } from '../../core/services/account/account.service';
import { AuthService } from '../../core/services/auth/auth.service';
import { ThemeService } from '../../core/services/theme/theme.service';
import { AppShellComponent } from './app-shell.component';

describe('AppShellComponent', () => {
  let component: AppShellComponent;
  let fixture: ComponentFixture<AppShellComponent>;

  const mockAuthService = {
    currentUser: () => ({ id: '1', username: 'chef', email: 'chef@example.com' }),
    logout: vi.fn(),
  };

  const mockAccountService = {
    openAccountSettingsDialog: vi.fn(),
  };

  const mockThemeService = {
    mode: () => 'light',
    setMode: vi.fn(),
    openThemeSettingsDialog: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: AccountService, useValue: mockAccountService },
        { provide: ThemeService, useValue: mockThemeService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders branding and user profile', () => {
    expect(component).toBeTruthy();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain(APP_NAME);
    expect(text).toContain('Cookbook');
    expect(text).toContain('Food Catalog');
  });

  it('calls authService.logout on onLogout', () => {
    component.onLogout();
    expect(mockAuthService.logout).toHaveBeenCalled();
  });
});

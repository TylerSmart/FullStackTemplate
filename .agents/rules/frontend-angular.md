# Frontend (Angular 22) Conventions

## 1. Angular v22 Core Rules
- **Component File Structure (Strict External Files Rule):**
  - **NEVER** use inline `template: \`...\`` or inline `styles: \`...\`` inside `@Component({...})`.
  - Always separate HTML templates and SCSS styles into dedicated sibling files: `<name>.component.html` and `<name>.component.scss`.
  - Always specify `templateUrl: './<name>.component.html'` and `styleUrl: './<name>.component.scss'` in `@Component({...})`.
- **Standalone & OnPush:**
  - Do **NOT** set `standalone: true` in `@Component` or `@Directive` decorators (default in Angular v20+).
  - `ChangeDetectionStrategy.OnPush` is default in Angular v22+ (do not rely on Zone/mutable change detection).
- **Services (`@Service()`):**
  - Use the Angular v22 `@Service()` decorator from `@angular/core` for singleton services instead of `@Injectable({ providedIn: 'root' })`.
- **Dependency Injection:**
  - Always use `inject()` field initialization (`private readonly http = inject(HttpClient);`) instead of constructor injection.
- **Reactivity & Signals:**
  - Use `signal()`, `computed()`, and `linkedSignal()` for component and service state.
  - Use `input()`, `output()`, and `model()` instead of `@Input()` / `@Output()` decorators.
  - Never use `.mutate()` on signals; use `.set()` or `.update()`.
- **Templates & Forms:**
  - Always use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`.
  - Do **NOT** import `CommonModule`; import only specific pipes/directives needed (e.g., `DatePipe`, `AsyncPipe`).
  - Use `[class]` and `[style]` bindings instead of `ngClass` and `ngStyle`.
  - Put host bindings/listeners inside `host: { ... }` in `@Component` / `@Directive`, never `@HostBinding` / `@HostListener`.
  - Prefer Signal Forms (`@angular/forms/signals`) or typed Reactive Forms (`FormBuilder.nonNullable`) over template-driven forms.

## 2. Branding, API Communication & Models
- **Centralized Branding (`src/app/app.branding.ts`):**
  - Read `APP_NAME`, `APP_STORAGE_PREFIX`, `APP_BRAND_ICON`, and `APP_LOGIN_SUBTITLE` from `app.branding.ts` rather than hardcoding app names or `localStorage` key prefixes in components/services.
- **API URL Helper:**
  - Always wrap backend endpoint paths with `apiUrl('/path')` from `src/app/core/api/api-url.ts`. Never hardcode `http://localhost:5041` or `/api` prefixes directly in services.
- **Error Handling:**
  - Backend errors return RFC 7807 `ProblemDetails`. Extract user-facing messages via `err?.error?.detail || err?.error?.title || 'Fallback message'`.

## 3. UI, Accessibility & Theming
- **Icons:** `MatIconRegistry` uses **Material Symbols Outlined** (`material-symbols-outlined` ligature font). Use standard Material Symbol names inside `<mat-icon>checklist</mat-icon>`.
- **Theming:** `ThemeService` (`src/app/core/services/theme/theme.service.ts`) manages `light`, `dark`, and `system` modes plus dynamic Material 3 palettes persisted to the user's account. Always style components with Material 3 CSS variables (`--mat-sys-*`, `--app-success*`, `--app-warning*`) so both light and dark themes remain legible.
- **Accessibility & Responsiveness:**
  - Templates are linted with `angular-eslint` `templateAccessibility` and must meet WCAG AA (icon-only buttons require `aria-label`, form inputs require `<mat-label>` or associated labels).
  - Ensure all pages and dialogs work cleanly at `360px` mobile widths as well as desktop viewports.

# Domain Overview & Invariants

> **Downstream Fork Customization:** Replace or expand this file in downstream repositories with the product's core domain entities, permission rules, and lifecycle invariants (referencing `docs/prd.md`). Keeping domain rules in this file avoids merge conflicts with shared upstream rules in `.agents/rules/`.

## Built-In Shared Slices

- **Authentication & OIDC (`Auth`):**
  - Local email/password registration & login (`PBKDF2-SHA256` password hashing) plus optional external OAuth2/OIDC sign-in (`DualBearerAuthenticationHandler`).
  - Unique normalized email (`NormalizedEmail`) and normalized username (`NormalizedUsername`) per `User`.
- **Account & Theme Preferences (`Account`):**
  - Authenticated users can update their username, email (requires current password; disabled for external-only accounts without a local password), and password (or set an initial password for external-only accounts).
  - User theme preferences (`ThemeSettings` JSON object, max 4000 chars) persist to the `Users` table and sync automatically across sessions while keeping `localStorage` for instant zero-flash startup.
- **Sample `Todo` Vertical Slice (`Todos`):**
  - Reference end-to-end CRUD vertical slice scoped per authenticated user (`OwnerId`). Downstream forks may remove or replace this slice once domain features are introduced.

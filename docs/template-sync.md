# Syncing Upstream Template Changes into Forks

Downstream projects forked from **FullStackTemplate** keep `https://github.com/TylerSmart/FullStackTemplate.git` configured as their `template` Git remote so improvements to authentication, theming, tooling, rules, and infrastructure can be merged cleanly.

---

## Golden Rules

> [!CAUTION]
> **Never squash or rebase template branches onto `FullStackTemplate` `main` after merging them into forks.**
> When merging a branch into `FullStackTemplate`'s `main`, always use **fast-forward** or a **standard merge commit** so commit SHAs stay identical between `template/main` and any forks that already synced.

> [!IMPORTANT]
> **EF Core migrations are per-repository.**
> Each downstream fork has its own database schema and migration history in `Data/Migrations/`. Never adopt migration files from `FullStackTemplate`. Always let [`scripts/merge-template.ps1`](../scripts/merge-template.ps1) strip upstream `Data/Migrations/*` files and then generate a native migration in the fork when `Data/Entities/` or `Data/Configurations/` changed.

---

## One-Time Setup on a Fork

1. Ensure the `template` remote exists:
   ```powershell
   git remote add template https://github.com/TylerSmart/FullStackTemplate.git
   git fetch template
   ```
2. Verify shared history exists:
   ```powershell
   git merge-base HEAD template/main
   ```
   If `git merge-base` returns empty (for example, the repository predates the template or was created via GitHub's *Use this template* UI without shared commit ancestry), record a one-time baseline merge against the template commit your fork already incorporates:
   ```powershell
   git merge -s ours --allow-unrelated-histories <baseline-template-commit-or-template/main> -m "chore: record FullStackTemplate baseline for future merges"
   ```

---

## Standard Sync Workflow

From a clean working tree on a sync branch in the downstream fork:

```powershell
git checkout -b chore/template-sync
./scripts/merge-template.ps1
```

What [`scripts/merge-template.ps1`](../scripts/merge-template.ps1) does automatically:
1. Verifies you are in a downstream fork with a clean working tree.
2. Runs `git fetch template` (pass `-NoFetch` or `-Ref <custom-ref>` when testing against a local template branch).
3. Runs `git merge --no-ff --no-commit template/main`.
4. Automatically strips any newly added upstream files under `Data/Migrations/` and restores the fork's own `Data/Migrations/AppDbContextModelSnapshot.cs`.
5. Reports any remaining conflicts and tells you whether entity/configuration files changed.

### Completing the Sync

1. **Resolve any remaining conflicts** (typically only if your fork customized `dashboard.component.*`, `styles.scss`, or branding docs):
   - Keep project-specific branding in `Client/src/app/app.branding.ts`, `Client/src/index.html`, `API/appsettings*.json`, `docker-compose.yml`, and `README.md`.
   - Keep fork-specific domain rules in `.agents/rules/domain.md` so shared `.agents/rules/*.md` files stay aligned with the template.
   - Keep fork-specific global SCSS partials imported via `@use` from `Client/src/styles.scss` rather than editing shared rules inline.
2. **Generate a fork migration if entity models changed**:
   ```powershell
   dotnet ef migrations add <DescriptiveMigrationName> --project Data/App.Data.csproj
   dotnet csharpier format Data/Migrations
   ```
3. **Build and run tests**:
   ```powershell
   dotnet build App.slnx
   dotnet test --project Tests/Core.UnitTests/App.Core.UnitTests.csproj
   dotnet test --project Tests/Infrastructure.UnitTests/App.Infrastructure.UnitTests.csproj
   dotnet test --project Tests/API.IntegrationTests/App.API.IntegrationTests.csproj
   cd Client
   npm run lint
   npm run build:production
   npm test -- --watch=false
   cd ..
   ```
4. **Commit the merge**:
   ```powershell
   git commit -m "chore: merge upstream FullStackTemplate updates"
   ```

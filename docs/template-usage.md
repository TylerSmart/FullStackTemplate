# Using This Repository as a Project Template

This guide walks you through initializing a new `.NET 10 + Angular 22` full-stack application from **FullStackTemplate**, customizing the centralized branding files, assigning a randomized external Docker host port, and wiring up the `template` Git remote so future upstream template improvements can be merged cleanly.

---

## Why Projects Use Generic Layout Names (`API`, `Client`, `App.*`)

All projects derived from **FullStackTemplate** intentionally keep generic folder, project, and C# namespace names:

- **Folders & Projects:** `API/App.API.csproj`, `Client/App.Client.esproj`, `Core/App.Core.csproj`, `Data/App.Data.csproj`, `Infrastructure/App.Infrastructure.csproj`, `Tests/*`, `App.slnx`, `App.slnLaunch`
- **C# Namespaces & Shared Types:** `App.API`, `App.Core`, `App.Data`, `App.Infrastructure`, `AppDbContext`

Because file paths and namespaces never diverge across projects, downstream forks can pull upstream template features and fixes with standard 3-way Git merges (`./scripts/merge-template.ps1`) instead of fighting hundreds of rename/namespace conflicts.

All **project-specific branding and deployment identifiers** (app title, `localStorage` prefix, database name, Docker container names, JWT issuer/audience, GHCR image path, and external NAS host port) are isolated to a small list of branding files.

---

## 1. Create a New Repository & Add the `template` Remote

1. On GitHub, click **Use this template** -> **Create a new repository** (or clone and push to your new repository).
2. Clone your new repository locally and register `FullStackTemplate` as the `template` remote:
   ```powershell
   git clone https://github.com/<your-username>/<your-new-repo>.git
   cd <your-new-repo>
   git remote add template https://github.com/TylerSmart/FullStackTemplate.git
   git fetch template
   ```
3. *(Important if GitHub's "Use this template" squashed initial history into a new root commit)*: Link your new repository's root commit to `template/main` once right after creation so future 3-way merges have a common merge base:
   ```powershell
   git merge -s ours --allow-unrelated-histories template/main -m "chore: link upstream FullStackTemplate history"
   ```
   *(If you forked/cloned directly and already share commit history with `template/main`, skip step 3.)*

---

## 2. Customize the Branding Files & Randomized Port

Generate a unique external Docker host port so multiple apps deployed to the same NAS/server never collide on host ports:

```powershell
Get-Random -Minimum 49152 -Maximum 65535
```

Then update only these project-specific branding files:

| File | What to Customize |
|---|---|
| [`Client/src/app/app.branding.ts`](../Client/src/app/app.branding.ts) | `APP_NAME` (UI brand title), `APP_STORAGE_PREFIX` (`kebab-case` prefix for `localStorage` keys), `APP_BRAND_ICON` (Material icon ligature), `APP_LOGIN_SUBTITLE` |
| [`Client/src/index.html`](../Client/src/index.html) | `<title>YourAppName</title>` |
| [`API/appsettings.json`](../API/appsettings.json) | `App:Name`, Serilog rolling file prefix (`./logs/your-app-.log`), `Authentication:Jwt:Issuer` and `Audience` |
| [`API/appsettings.Development.json`](../API/appsettings.Development.json) | `ConnectionStrings:DefaultConnection` (`Database=YourAppName;...;Password=...`), dev `Authentication:Jwt` values |
| [`docker-compose.yml`](../docker-compose.yml) | Compose `name:`, `container_name:`, volume name, and `MSSQL_SA_PASSWORD` (matching `API/appsettings.Development.json`) |
| [` API/App.API.csproj`](../API/App.API.csproj) | *(Optional)* Generate a fresh `<UserSecretsId>` GUID (`[guid]::NewGuid()`) |
| [`README.md`](../README.md), [`LICENSE`](../LICENSE), [`docs/deployment/`](deployment/) | App description, GHCR image (`ghcr.io/<owner>/<repo>`), and replace `62949` with your generated host port |

> [!TIP]
> **Local Development Ports**:
> During local development, Angular (`4200`), ASP.NET Core (`5041` HTTP / `7127` HTTPS), and local SQL Server (`14333`) only run while actively developing. Notice [`scripts/ensure-local-infra.ps1`](../scripts/ensure-local-infra.ps1) automatically reads the database name and SA password from `API/appsettings.Development.json`—no script edits needed.

---

## 3. Post-Setup Verification Checklist

1. **Verify Backend Build & Tests**:
   ```powershell
   dotnet build App.slnx
   dotnet test --project Tests/Core.UnitTests/App.Core.UnitTests.csproj
   dotnet test --project Tests/Infrastructure.UnitTests/App.Infrastructure.UnitTests.csproj
   dotnet test --project Tests/API.IntegrationTests/App.API.IntegrationTests.csproj
   ```
2. **Verify Frontend Dependencies, Lint, Build, & Unit Tests**:
   ```powershell
   cd Client
   npm install
   npm run lint
   npm run build:production
   npm test -- --watch=false
   ```
3. **Commit Branding Initialization**:
   ```powershell
   git add -A
   git commit -m "chore: initialize project branding from FullStackTemplate"
   ```

---

## 4. Replacing the Sample `Todo` Vertical Slice

The template includes a minimal end-to-end **`TodoItem`** CRUD feature as a reference pattern across every layer. When building your own domain features, you can remove or replace these files:

| Layer | Files in the Sample `Todo` Slice |
|---|---|
| **Core (Contracts & DTOs)** | `Core/Todos/TodoModels.cs`, `Core/Todos/ITodoService.cs` |
| **Data (EF Core Entity & Service)** | `Data/Entities/TodoItem.cs`, `Data/Configurations/TodoItemConfiguration.cs`, `Data/Services/TodoService.cs` (+ `DbSet<TodoItem>` in `Data/AppDbContext.cs` and registration in `Data/DataServiceCollectionExtensions.cs`) |
| **API (Controller)** | `API/Controllers/TodosController.cs` |
| **Integration Tests** | `Tests/API.IntegrationTests/TodoIntegrationTests.cs` |
| **Angular Client** | `Client/src/app/core/models/todo.models.ts`, `Client/src/app/core/services/todo/`, `Client/src/app/features/dashboard/todo-dialog/`, `Client/src/app/features/dashboard/` |

After modifying entities in `AppDbContext`, generate an EF Core migration and format it:

```powershell
dotnet ef migrations add YourNewMigration --project Data/App.Data.csproj
dotnet csharpier format Data/Migrations
```

---

## 5. Pulling Future Template Updates

See **[Syncing Upstream Template Changes (`docs/template-sync.md`)](template-sync.md)** for the full workflow using `./scripts/merge-template.ps1`.

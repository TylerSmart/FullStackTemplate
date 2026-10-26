# Architecture & Layer Boundaries

## 1. Adding a New Vertical Slice (Checklist)

When implementing a new feature across the stack, follow the established vertical-slice pattern:

1. **`Core/<Feature>/` (`App.Core`)**:
   - Define immutable C# `record` DTOs and Request models (with `DataAnnotations` validation attributes on Request records).
   - Define `I<Feature>Service` interface with async methods accepting `CancellationToken cancellationToken = default`.
2. **`Data/` (`App.Data`)**:
   - Create entity in `Data/Entities/<Entity>.cs` implementing `IAuditable`.
   - Create EF configuration in `Data/Configurations/<Entity>Configuration.cs` implementing `IEntityTypeConfiguration<Entity>` (auto-discovered via `ApplyConfigurationsFromAssembly`).
   - Add `public DbSet<Entity> <Entities> => Set<Entity>();` to `AppDbContext`.
   - Implement `<Feature>Service` in `Data/Services/` and register `services.AddScoped<I<Feature>Service, <Feature>Service>();` in `DataServiceCollectionExtensions.AddDataServices`.
   - **Create & apply the EF Core migration** (`dotnet ef migrations add <Name> --project Data/App.Data.csproj` and `dotnet ef database update --project Data/App.Data.csproj`), then format `Data/Migrations` with CSharpier and commit the migration in the same commit as the entity changes.
3. **`API/Controllers/` (`App.API`)**:
   - Create `<Feature>Controller` with `[ApiController]`, `[Authorize]`, and relative `[Route("<feature-kebab>")]` (do **not** prefix with `api/`—`Program.cs` mounts all controllers under `app.MapGroup("/api")`).
4. **`Client/src/app/` (`App.Client`)**:
   - Add TypeScript interfaces in `core/models/<feature>.models.ts`.
   - Add `@Service()` HTTP service in `core/services/<feature>/<feature>.service.ts` using `apiUrl('/<feature-kebab>')`.
   - Add standalone feature components under `features/<feature>/<component>/` (each component in its own dedicated kebab-case folder) using separate `.ts`, `.html`, and `.scss` files, and register lazy routes in `app.routes.ts`.
5. **Automated Tests & Verification Loop (Required on Every Implementation)**:
   - Add/update `.NET` Unit Tests (`Tests/Core.UnitTests` / `Tests/Infrastructure.UnitTests`), `.NET` API Integration Tests (`Tests/API.IntegrationTests` via `AppWebApplicationFactory`), Angular `.spec.ts` unit tests, and **dedicated Playwright E2E Tests** (`Tests/Client.E2ETests/<Feature>E2ETests.cs` inheriting from `E2ETestBase` for user journeys, dialogs, and `360px` mobile viewports).
   - Run all relevant builds, linters, and test suites; fix any errors and re-run until everything compiles and passes cleanly.

---

## 2. Lean Controllers & Domain Boundaries (Strict Rule)

- **Controllers must remain thin HTTP adapters.** A controller's responsibilities are strictly limited to:
  1. Receiving and binding HTTP requests and DTOs (handling syntactic/model validation).
  2. Resolving the authenticated user ID (`ClaimTypes.NameIdentifier` / `"sub"` or `IUserContext`) and invoking services defined in `App.Core`.
  3. Translating service results (domain records, nulls, booleans) and catching domain exceptions (`DomainValidationException`, `ForbiddenException`, `ConflictException`, `NotFoundException`) into HTTP responses and RFC 7807 `ProblemDetails`.
- **Controllers MUST NEVER contain:**
  - Direct `AppDbContext` queries, LINQ data access, or raw persistence operations.
  - Business logic, permission rules, or entity state mutation loops.
- **`App.Core` MUST NEVER contain HTTP or EF Core concepts:**
  - Domain contracts return records/nullable types or throw domain exceptions from `App.Core.Exceptions`.

---

## 3. Upstream Template Compatibility

- Keep folder, `.csproj`, `.slnx`, and root `App.*` namespace conventions intact so upstream `FullStackTemplate` updates merge cleanly via `.\scripts\merge-template.ps1`.
- Isolate fork-specific branding to `Client/src/app/app.branding.ts`, `Client/src/index.html`, `API/appsettings*.json`, `docker-compose.yml`, and `README.md`.
- Record fork-specific product invariants in [`.agents/rules/domain.md`](domain.md) rather than editing shared rule files.

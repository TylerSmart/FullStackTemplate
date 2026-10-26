# Backend (.NET 10 & EF Core) Conventions

## 1. C# 14 & Project Style
- **Target Framework:** `net10.0` with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Use **file-scoped namespaces** under the generic `App.*` root (`App.Core`, `App.Data`, `App.Infrastructure`, `App.API`).
- Prefer **`sealed` classes** for services, controllers, EF configurations, and interceptors unless inheritance is explicitly designed.
- Use **primary constructors** for dependency injection (e.g., `public sealed class TodoService(AppDbContext dbContext) : ITodoService`).
- Use **immutable positional `record`s** for DTOs and Request payloads in `Core/`.
- Format all C# code using **CSharpier** (`dotnet csharpier format .`).

## 2. API Controllers & Routing (`API/`)
- **Route Prefix:** `Program.cs` maps controllers via `app.MapGroup("/api").MapControllers()`. Always use relative route templates on controllers (e.g., `[Route("todos")]` or `[Route("account")]`, **never** `[Route("api/todos")]`).
- **Authentication & User Resolution:**
  - Default to `[Authorize]` at the controller level (`[AllowAnonymous]` only where explicitly public).
  - Resolve the authenticated user's `Guid` from `ClaimTypes.NameIdentifier` or `"sub"` claim (or `IUserContext`).
- **OpenAPI / Scalar Annotations:**
  - Decorate endpoints with `[ProducesResponseType(typeof(...), StatusCodes.Status200OK)]` (and `400`, `401`, `403`, `404`, `409`, `201`, `204` as applicable) so `/api/scalar` and `/api/openapi/v1.json` stay accurate.
- **Domain Exceptions & Status Codes (`App.Core.Exceptions`):**
  - `DomainValidationException` $\rightarrow$ `400 Bad Request` (`ProblemDetails`). **Never** return `401 Unauthorized` for business/validation errors on authenticated endpoints (such as an incorrect `CurrentPassword` during password/email change), because the Angular `authInterceptor` clears the session on any non-login `401`.
  - `ForbiddenException` $\rightarrow$ `403 Forbidden` when a user attempts to access a resource they do not have permission for.
  - `NotFoundException` or `null`/`false` service return $\rightarrow$ `404 Not Found` (`ProblemDetails`).
  - `ConflictException` $\rightarrow$ `409 Conflict` (`ProblemDetails`) for duplicate/concurrency violations.

## 3. Entity Framework Core (`Data/`)
- **Auditing (`IAuditable`):**
  - Domain entities must implement `IAuditable` (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`).
  - `AuditSaveChangesInterceptor` automatically populates audit columns on `SaveChanges` / `SaveChangesAsync`.
- **Entity Configurations:**
  - Place all schema rules (table name, keys, max lengths, indexes, cascade behavior) in `Data/Configurations/<Entity>Configuration.cs` implementing `IEntityTypeConfiguration<T>`.
  - Keep entity classes clean POCOs without `[MaxLength]` or `[Table]` data annotations.
  - Explicitly configure `DeleteBehavior.Restrict` / `DeleteBehavior.ClientSetNull` where needed to avoid SQL Server multiple-cascade-path errors.
- **Query Performance:**
  - Always use `.AsNoTracking()` on read-only queries (`Get*` methods).
  - Always pass `CancellationToken` to EF Core async methods (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `SaveChangesAsync`).

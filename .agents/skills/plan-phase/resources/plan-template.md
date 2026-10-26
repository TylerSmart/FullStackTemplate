# Phase <NN>: <Phase Title>

> **Branch:** `feature/phase-<NN>-<slug>`
> **Status:** Planned (`[ ]` Not Started | `[~]` In Progress | `[x]` Completed)
> **PRD Reference:** `docs/prd.md` — Sections <X.Y>
> **Target Execution:** Run `/execute-phase` to execute this plan step-by-step.

---

## 1. Scope & Objectives
- Concise summary of what this phase delivers and what is explicitly out of scope for later phases.
- Any cleanup/removal required (e.g., removing sample `Todo` vertical slice if applicable).

---

## 2. Domain & Database Design (`Core/` & `Data/`)

### 2.1 Enums & Value Types (`Core/<Feature>/`)
- List exact enum names and values under namespace `App.Core.<Feature>`.

### 2.2 Entities (`Data/Entities/`)
- Exact class names, properties, navigation properties, and `IAuditable` implementation under `App.Data.Entities`.

### 2.3 EF Core Fluent Configurations (`Data/Configurations/`)
- Table names, primary keys, string max lengths, indexes (unique & non-unique), and foreign key `DeleteBehavior` rules.
- **SQL Server Note:** Explicitly specify `DeleteBehavior.Restrict` / `DeleteBehavior.ClientSetNull` where needed to avoid SQL Server `may cause cycles or multiple cascade paths` errors.

### 2.4 DTOs, Requests & Service Contracts (`Core/<Feature>/`)
- Exact positional `record` definitions, `[Required]` / `[MaxLength]` attributes, and `I<Feature>Service` method signatures.

### 2.5 Service Business Logic & Authorization Invariants (`Data/Services/`)
- Bullet-pointed business rules for each method (ownership/permission checks, domain exceptions, `AsNoTracking()` on reads, and validation rules).

---

## 3. API Endpoints (`API/Controllers/<Feature>Controller.cs`)

| Verb | Route (`/api/...`) | Request Body / Query | Response (`200`/`201`/`204`) | Error Cases (`400`/`403`/`404`/`409`) |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/<route>` | ... | `IReadOnlyList<...Dto>` | ... |

---

## 4. Angular Frontend Design (`Client/`)

### 4.1 Models & Services (`src/app/core/`)
- Exact files in `core/models/<feature>.models.ts` and `core/services/<feature>/<feature>.service.ts`.

### 4.2 Feature Components & Routes (`src/app/features/<feature>/`)
- Component hierarchy (strictly separate `.ts`, `.html`, `.scss` files), signals (`signal()`, `computed()`), dialogs, Material Symbols icons, and responsive mobile (`360px`) vs. desktop behavior.

---

## 5. Step-by-Step Execution Checklist (For Implementation Agent)

- [ ] **Task 1: Backend Core & Data Layer**
  - Create/update Core models, interfaces, Entities, EF Configurations, `AppDbContext`, and Data Services.
  - Register services in `DataServiceCollectionExtensions.cs`.
  - Generate, apply, and format EF migration:
    ```powershell
    dotnet ef migrations add <MigrationName> --project Data/App.Data.csproj
    dotnet ef database update --project Data/App.Data.csproj
    dotnet csharpier format Data/Migrations
    ```
  - Write & run unit tests in `Tests/Core.UnitTests`:
    ```powershell
    dotnet test --project Tests/Core.UnitTests/App.Core.UnitTests.csproj
    ```

- [ ] **Task 2: API Controllers & Integration Tests**
  - Create/update thin HTTP Controllers in `API/Controllers/`.
  - Write comprehensive integration tests in `Tests/API.IntegrationTests/` (covering CRUD, validation, and permission boundaries).
  - Run integration tests until 100% green:
    ```powershell
    dotnet test --project Tests/API.IntegrationTests/App.API.IntegrationTests.csproj
    ```

- [ ] **Task 3: Angular Client & Unit Tests**
  - Create TypeScript models, `@Service()` API services, external-template feature components, and route registrations.
  - Write/update `.spec.ts` unit tests for services and components.
  - Run frontend lint, production build, and unit tests until 100% green:
    ```powershell
    # In Client/:
    npm run lint
    npm run build:production
    npm test -- --watch=false
    ```

- [ ] **Task 4: Playwright E2E Verification & Final Formatting**
  - Add/update E2E test coverage in `Tests/Client.E2ETests/` if user-facing flows changed.
  - Format all backend and frontend files:
    ```powershell
    dotnet csharpier format .
    # In Client/:
    npm run format
    npm run lint:fix
    ```
  - Mark this plan's status as `[x] Completed`.

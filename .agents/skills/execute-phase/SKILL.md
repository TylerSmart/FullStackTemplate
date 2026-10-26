---
name: execute-phase
description: >-
  Executes the latest (or specified) implementation plan in docs/plans/ step-by-step,
  running EF Core migrations, unit/integration/Angular/E2E tests, and fixing errors
  until green. Use this skill when the user asks to implement or execute a planned phase.
---

# Execute Planned Phase (`execute-phase`)

This skill is designed to be run by an **implementation model** after `plan-phase` has created a `feature/phase-<NN>-<slug>` branch and committed a phase blueprint in `docs/plans/`.

---

## Step-by-Step Workflow

### 1. Verify Active Phase Branch & Locate the Plan
1. Run `git branch --show-current`, `git status`, and check `docs/plans/`.
2. Ensure you are on the target phase branch (`feature/phase-<NN>-<slug>`), **never** on `main`. (If on `main`, check out the existing `feature/phase-<NN>-<slug>` branch before making changes.)
3. Open the target `docs/plans/phase-<NN>-<slug>.md` file (whose status is `Planned` or `In Progress`) and update the status header to `[~] In Progress`.
4. **Do not scan the entire codebase**—the plan file already contains exact file paths, C# signatures, EF configurations, API routes, and Angular interfaces. Only read existing files you are directly modifying.

### 2. Execute Tasks Sequentially (`Task 1` $\rightarrow$ `Task 4`)
Work through each unchecked `- [ ]` task in the checklist section of the plan in order:

1. **Task 1 (Backend Core & Data + EF Migration):**
   - Implement the `Core/` and `Data/` files as specified, keeping business logic in services and out of controllers.
   - If entities or configurations changed, run `.\scripts\ensure-local-infra.ps1`, `dotnet ef migrations add <MigrationName> --project Data/App.Data.csproj`, `dotnet ef database update --project Data/App.Data.csproj`, and `dotnet csharpier format Data/Migrations`.
   - Write and run unit tests (`dotnet test --project Tests/Core.UnitTests/App.Core.UnitTests.csproj` and `Tests/Infrastructure.UnitTests/App.Infrastructure.UnitTests.csproj`). Fix any errors before moving to Task 2.
   - Check off `- [x] Task 1` in the plan file.

2. **Task 2 (API Controllers & Integration Tests):**
   - Implement thin HTTP adapter controller(s) in `API/Controllers/`.
   - Write integration tests in `Tests/API.IntegrationTests/` using `AppWebApplicationFactory`.
   - Run `dotnet test --project Tests/API.IntegrationTests/App.API.IntegrationTests.csproj` and fix any issues until 100% passing.
   - Check off `- [x] Task 2` in the plan file.

3. **Task 3 (Angular Client & Unit Tests):**
   - Implement TypeScript models, `@Service()` classes, dedicated per-component feature components (`features/<feature>/<component>/` with separate `.ts`, `.html`, and `.scss` files—never inline `template`/`styles`), and routes in `Client/`.
   - Write/update `.spec.ts` unit tests.
   - From `Client/`, run `npm run lint`, `npm run build:production`, and `npm test -- --watch=false`. Fix any compile, lint, or test errors until 100% passing.
   - Check off `- [x] Task 3` in the plan file.

4. **Task 4 (Playwright E2E, Full Verification & Commit):**
   - **Mandatory Playwright E2E Tests:** Implement the dedicated Playwright E2E test suite defined in Section 5 of the plan (`Tests/Client.E2ETests/<Feature>E2ETests.cs`) inheriting from `E2ETestBase`. Ensure tests cover user journeys, interactive UI elements (dialogs, toggles, calculations), and `360px` mobile viewport responsiveness.
   - Run `dotnet test --project Tests/Client.E2ETests/App.Client.E2ETests.csproj` and fix any failures until 100% green.
   - Format all code (`dotnet csharpier format .` at repo root; `npm run format` and `npm run lint:fix` in `Client/`) and verify `dotnet build App.slnx`.
   - Run the complete solution verification suite (`dotnet test App.slnx`).
   - **Verification Honesty:** Only mark tasks `[x]` and set the plan status header to `[x] Completed` once the full build and all required test suites pass. If any required verification is blocked, report the blocker clearly and leave the task unchecked.
   - Stage only files related to this phase (preserving any pre-existing untracked/uncommitted user files) and commit onto `feature/phase-<NN>-<slug>` (`git commit -m "feat(phase-<NN>): implement <slug>"`).

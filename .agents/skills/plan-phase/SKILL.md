---
name: plan-phase
description: >-
  Generates a detailed, token-efficient implementation plan for the next product
  milestone or vertical slice so a lower-cost/faster model can execute it with zero
  architectural ambiguity. Use this skill when the user asks to plan the next phase,
  milestone, or feature from docs/prd.md.
---

# Plan Next Phase (`plan-phase`)

This skill is designed to be run by a **high-reasoning model** during the **Planning Phase**. Its goal is to start from a clean `main` branch, perform architectural design upfront, create a dedicated phase branch, and commit a self-contained, file-by-file blueprint in `docs/plans/` onto that branch so an implementation model can execute it in a single pass.

---

## Step-by-Step Workflow

### 1. Ensure Clean `main` Branch Baseline
1. Run `git status` and `git branch --show-current` to verify repository state.
2. If the repository is not currently on `main`:
   - If the working tree is clean, switch to `main` (`git checkout main`) and fast-forward pull if `origin/main` is reachable (`git pull --ff-only` — ignore error if remote is unavailable).
   - If there are uncommitted changes or an unmerged phase in progress, alert the user before switching branches.

### 2. Assess Current Progress (Minimal Token Scan)
1. Read [`docs/prd.md`](../../../docs/prd.md) and [`.agents/rules/domain.md`](../../rules/domain.md) to review product requirements and domain invariants.
2. Check existing plans in `docs/plans/` (if any) and list current entities in `Data/Entities/` and features in `Client/src/app/features/` to determine what has already been built on `main`.
3. If the user did not specify which phase or slice to plan, identify the **next unbuilt vertical slice** in the current milestone and choose a concise phase number and kebab-case slug (e.g., `01-core-entities`).
   - *Sizing Rule:* Keep a single phase scoped to **1–3 closely related vertical slices**. Break large milestones into 2–3 digestible phases so the execution model's context window stays clean.

### 3. Clarify Only Unresolved Product Decisions
- Do **not** ask questions already answered in `docs/prd.md` or `.agents/rules/`.
- If there is a genuine UX or business-logic ambiguity not covered by existing docs, ask the user before writing the plan.

### 4. Create the Phase Branch & Draft the Blueprint (`docs/plans/phase-<NN>-<slug>.md`)
1. Create and switch to a dedicated phase branch off `main`:
   ```powershell
   git checkout -b feature/phase-<NN>-<slug>
   ```
2. Write the implementation plan to `docs/plans/phase-<NN>-<slug>.md` following [resources/plan-template.md](./resources/plan-template.md).

To save maximum tokens during the **Execution Phase**, the plan **MUST** specify:
- **Exact File Paths** to create, modify, or delete across `Core/`, `Data/`, `Infrastructure/`, `API/`, `Client/`, and `Tests/`.
- **Exact Data Contracts & Schema Details:**
  - C# Entity properties, enums, foreign keys, indexes, and delete behaviors (`Restrict` vs `Cascade` — SQL Server disallows multiple cascade paths!).
  - C# `record` DTOs, Request validation attributes, and `I*Service` method signatures in `App.Core`.
  - Exact EF Core migration name to run (`dotnet ef migrations add <MigrationName> --project Data/App.Data.csproj`).
- **Exact API Endpoints:** HTTP verbs, relative route paths, query parameters, status codes, and authorization/ownership enforcement rules (keeping controllers thin HTTP adapters).
- **Exact Angular Client Specs:** TypeScript model interfaces, `@Service()` methods, external `.ts`/`.html`/`.scss` component files, routes, signals/computed properties, and `360px` mobile + desktop layout requirements.
- **Ordered Execution Checklist (`- [ ]`):** Broken into sequential tasks (`Backend Core & Data` $\rightarrow$ `API & Integration Tests` $\rightarrow$ `Angular Client & Spec Tests` $\rightarrow$ `Playwright E2E & Final Format/Verify`) with exact test commands to run after each task.

### 5. Commit the Plan onto the Phase Branch & Present for Approval
1. Stage only the generated plan and commit it onto `feature/phase-<NN>-<slug>`:
   ```powershell
   git add docs/plans/phase-<NN>-<slug>.md
   git commit -m "docs(plan): add phase <NN> implementation plan (<slug>)"
   ```
2. Do **not** write application code during `plan-phase`.
3. Point the user to `docs/plans/phase-<NN>-<slug>.md`, confirm the active branch (`feature/phase-<NN>-<slug>`), highlight key architectural choices, and let them know they can run `/execute-phase` when ready.

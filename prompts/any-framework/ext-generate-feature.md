Generate ONLY the Gherkin feature file for a Jira story — works with ANY framework/language.
No step definitions, no Vansah import. Covers all scenario categories.
Automatable and Non-Automatable scenarios are tagged correctly.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Jira Story number     (e.g. VFL-8283)
2. Framework / Language  (e.g. Java/Cucumber, Python/Behave, JS/Playwright, Ruby/Cucumber, C#/SpecFlow)
3. Feature file name     (e.g. BNYCommentsVisibility  ← no extension)
4. Feature file path     (e.g. src/test/resources/features  or  features/BNY)
```

Wait for the user's answers. Store as:
- STORY_ID     = answer to 1
- FRAMEWORK    = answer to 2
- FEATURE_NAME = answer to 3
- FEATURE_PATH = answer to 4

---

## Step 2 — Read and analyse the Jira story

Fetch Jira issue [STORY_ID] and extract ALL of the following:

**Roles** — every user role mentioned. For each: what they CAN do, CANNOT do, what they SEE.

**Acceptance criteria** — number every AC. Each numbered AC = at least one scenario.

**Business rules** — explicit rules AND implicit domain rules (workflow ordering, restrictions, guards).

**States & transitions** — build a mental state machine. Every state + every transition = a scenario.

**Data dimensions** — every field with validation: required, format, range, type. Each = a negative scenario.

**UI / UX expectations** — layout, labeling, visual feedback, button visibility, design specs.

**Non-Functional expectations** — performance, load, response time, accessibility, download behavior.

---

## Step 3 — Build full coverage matrix (internal check before writing)

Confirm EVERY row is covered before generating scenarios:

| Coverage category                   | Min            | Label tag         |
|-------------------------------------|----------------|-------------------|
| Happy path                          | 1              | @Automatable      |
| Per user role                       | 1 per role     | @Automatable      |
| Per acceptance criterion            | 1 per AC       | @Automatable      |
| Workflow states                     | 1 per state    | @Automatable      |
| Status / column display             | 1              | @Automatable      |
| Positive data variations            | 1+             | @Automatable      |
| Negative — required field missing   | 1 per field    | @Automatable      |
| Negative — invalid format           | 1 per field    | @Automatable      |
| Negative — quantity boundary        | 3              | @Automatable      |
| Role restriction                    | 1 per rule     | @Automatable      |
| Business restriction                | 1 per rule     | @Automatable      |
| Workflow ordering                   | 1              | @Automatable      |
| Rejection path                      | 1 per approver | @Automatable      |
| Edge cases                          | 1+             | @Automatable      |
| Full E2E lifecycle                  | 1              | @Automatable      |
| Data-driven (Outline)               | 1 outline      | @Automatable      |
| UI / Usability                      | 1+             | @NonAutomatable   |
| Non-Functional (performance, load)  | 1+             | @NonAutomatable   |
| Accessibility                       | 1+             | @NonAutomatable   |

---

## Step 4 — Generate ALL scenarios and present numbered list for labeling

Generate complete Gherkin for ALL categories internally,
then display a numbered list — grouped by category — in plain business language.

Output format:

```
Here are all [N] test scenarios identified for [STORY_ID].
Please label each:  A = Automatable  |  N = Non-Automatable

── Functional / Happy Path ─────────────────────────────────
 1. <scenario title>
 2. <scenario title>

── Role-Based ──────────────────────────────────────────────
 3. <scenario title>

── Workflow States ─────────────────────────────────────────
 4. <scenario title>

── Validation / Negative ───────────────────────────────────
 5. <scenario title>

── Edge Cases ──────────────────────────────────────────────
 6. <scenario title>

── End-to-End ──────────────────────────────────────────────
 7. <scenario title>

── UI / Usability ──────────────────────────────────────────
 8. <scenario title>

── Non-Functional ──────────────────────────────────────────
 9. <scenario title>

── Accessibility ───────────────────────────────────────────
10. <scenario title>

Reply format: 1:A, 2:A, 3:N  — or use ranges like 1-7:A, 8-10:N
```

PAUSE here — wait for the user's labeling response before proceeding.
Store labels as a map: LABELS = { 1: A, 2: A, 3: N, ... }

---

## Step 5 — Write the Feature File

Create: [FEATURE_PATH]/[FEATURE_NAME].feature

### Tag rules (CRITICAL — these drive Vansah label mapping in the importer)

Automatable scenarios (label A):
```gherkin
@smokeBDD @Smoke @Regression @[STORY_ID] @Automatable
Scenario: <title>
```

Non-Automatable scenarios (label N):
```gherkin
@ignore @Manual @Regression @[STORY_ID] @NonAutomatable
Scenario: <title>
```
The `@ignore` tag (or framework-equivalent skip tag) prevents the runner from executing the scenario.

### Other rules
- Every scenario has a `# Precondition:` comment directly above it
- Scenario titles are business-readable and role-explicit
  GOOD: "Correspondent Approver approves election and forwards to Main Approver"
  BAD:  "Test approve button"
- Use Scenario Outline + Examples (min 3 rows) for data-driven cases
- Group scenarios with section comments:
  # ── Happy Path ──────────────────────────────────────
  # ── Role-Based Access ────────────────────────────────
  # ── Business Rules & Restrictions ────────────────────
  # ── Validation / Negative ────────────────────────────
  # ── Edge Cases ───────────────────────────────────────
  # ── Workflow States ──────────────────────────────────
  # ── End-to-End ───────────────────────────────────────
  # ── UI / Usability ───────────────────────────────────
  # ── Non-Functional ───────────────────────────────────
  # ── Accessibility ────────────────────────────────────

Gherkin discipline:
- Given = state / precondition
- When  = user action
- Then  = assertion / outcome
- And   = continuation of previous keyword type
- Never jump from Given to Then without a When

---

## Step 6 — Final summary

Output:
```
  Feature file created : [FEATURE_PATH]/[FEATURE_NAME].feature
  Framework            : [FRAMEWORK]
  Total scenarios      : N
  Automatable          : N  (@smokeBDD @Smoke @Regression @[STORY_ID] @Automatable)
  Non-Automatable      : N  (@ignore @Manual @Regression @[STORY_ID] @NonAutomatable)

  Next steps:
    Generate step definitions : /non-dotnet:ext-generate-steps
    Import to Vansah          : /non-dotnet:ext-vansah-import  (after updating vansah-config.json)
```
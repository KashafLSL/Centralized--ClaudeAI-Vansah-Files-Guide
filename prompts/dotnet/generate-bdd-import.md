Generate BDD scenarios + step definitions AND immediately import to Vansah in one shot.
Automatable scenarios get full step definitions. Non-Automatable scenarios get
the @ignore tag and NO step definitions — SpecFlow skips them entirely.
ALL scenarios are imported to Vansah with the correct label via the "label" field (singular).

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Jira Story number          (e.g. VFL-8283)
2. Feature file name          (e.g. BNYCommentsVisibility  ← no extension)
3. Feature file path          (e.g. FeatureFiles/AITestingBDD  or  FeatureFiles/AITestingBDD/UsingVansah)
4. Step definition file name  (e.g. BNYCommentsVisibilitySteps  ← no extension)
5. Step definition path       (e.g. StepDefinitions/BNY)
```

Wait for the user's answers. Store as:
- STORY_ID     = answer to 1
- FEATURE_NAME = answer to 2
- FEATURE_PATH = answer to 3
- STEPS_NAME   = answer to 4
- STEPS_PATH   = answer to 5

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

| Coverage category                   | Min        | Label tag         |
|-------------------------------------|------------|-------------------|
| Happy path                          | 1          | @Automatable      |
| Per user role                       | 1 per role | @Automatable      |
| Per acceptance criterion            | 1 per AC   | @Automatable      |
| Workflow states                     | 1 per state| @Automatable      |
| Status / column display             | 1          | @Automatable      |
| Positive data variations            | 1+         | @Automatable      |
| Negative — required field missing   | 1 per field| @Automatable      |
| Negative — invalid format           | 1 per field| @Automatable      |
| Negative — quantity boundary        | 3          | @Automatable      |
| Role restriction                    | 1 per rule | @Automatable      |
| Business restriction                | 1 per rule | @Automatable      |
| Workflow ordering                   | 1          | @Automatable      |
| Rejection path                      | 1 per approver | @Automatable  |
| Edge cases                          | 1+         | @Automatable      |
| Full E2E lifecycle                  | 1          | @Automatable      |
| Data-driven (Outline)               | 1 outline  | @Automatable      |
| UI / Usability                      | 1+         | @NonAutomatable   |
| Non-Functional (performance, load)  | 1+         | @NonAutomatable   |
| Accessibility                       | 1+         | @NonAutomatable   |

---

## Step 4 — Generate ALL scenarios and present numbered list for labeling

Generate complete Gherkin for ALL categories (Automatable + Non-Automatable) internally,
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

Create: vFluxAutomation/[FEATURE_PATH]/[FEATURE_NAME].feature

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
The `@ignore` tag causes SpecFlow to skip the scenario entirely — no step definitions needed or generated.

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

## Step 6 — Write Step Definitions

Create: vFluxAutomation/[STEPS_PATH]/[STEPS_NAME].cs

Namespace: vFluxAutomation.[STEPS_PATH with / replaced by .]
Class:     [Binding] public class [STEPS_NAME]

### Automatable steps — full implementation
```csharp
// Assertion
Assert.AreEqual(BNYHelper.Instance.ClickButton("key"), Global.SUCCESS);

// Visibility wait
vFluxHelper.Instance.FluentWaitTillElementIsVisible(10, 1, "key");

// Clickable wait
vFluxHelper.Instance.FluentWaitTillElementToBeClickable(10, 1, "key");

// Login — NEVER use LoginHelper.Login()
Assert.AreEqual(LoginHelper.Instance.FillLoginControls("LoginUsername", Global.USER_BNY, "LoginPassword", Global.PASSWORD), Global.SUCCESS);
Assert.AreEqual(LoginHelper.Instance.ClickButton("LoginButton"), Global.SUCCESS);
vFluxHelper.Instance.ImplicitWait(2000);
```

### Non-Automatable scenarios — NO step definitions
Do NOT generate any step definition methods for steps that belong exclusively to @NonAutomatable scenarios.
The `@ignore` tag on the scenario causes SpecFlow to skip it entirely — it never looks for step bindings.
Writing stub methods for these steps is unnecessary and pollutes the step definition class.

Available helpers (use existing methods first — never duplicate):
- vFluxHelper.Instance         → browser, waits, navigation
- BNYHelper.Instance           → BNY grid, search, export, columns, toast, approver queue
- LoginHelper.Instance         → FillLoginControls(), ClickButton()
- ColumnChooserHelper.Instance → column chooser operations
- ElectionsHelper.Instance     → election queue actions
- AdminHelper.Instance         → admin panel operations

Global user constants (never invent string literals):
- Global.USER_BNY       = "KashafApprover"   (BNY Approver)
- Global.CORR_USER      = "corrapp"           (Correspondent Approver)
- Global.APPROVER_USER  = "qaapprover"        (Main Approver)
- Global.SIMPLE_USER    = "qauser"            (Simple User)
- Global.ADMIN_USER     = "adminuserr"        (Admin)
- Global.PASSWORD       = "Test@1235"
- Global.ADMIN_PASSWORD = "Test@12345"

When a new helper method is needed: add it to the most relevant existing helper class.
When a new Global constant is needed: add it to WebControls/Global.cs (ALL_CAPS_WITH_UNDERSCORES).

---

## Step 7 — Update Demo.json

For every new UI element key used in step definitions that does not already exist in Demo.json,
append a placeholder entry to vFluxAutomation/Demo.json:

```json
"ElementKey": {
    "ControlType": "Button|Textbox|No Action",
    "IdentifierType": "XPath",
    "Identifier": "TODO: add XPath - <description of element>"
}
```

---

## Step 8 — Ask for FolderIdentifier

After Steps 5–7 are complete, output a coverage summary then ask:

```
┌─ Coverage summary ──────────────────────────────────────────┐
│  Total scenarios    : N                                      │
│  ── Automatable (@Automatable) ──────────────────────────── │
│  Happy path         : N                                      │
│  Role-based         : N                                      │
│  Positive           : N                                      │
│  Negative           : N   (validation + format + boundary)   │
│  Business rules     : N                                      │
│  Edge cases         : N                                      │
│  E2E lifecycle      : N                                      │
│  ── Non-Automatable (@NonAutomatable) ───────────────────── │
│  UI / Usability     : N   (manual — Vansah label applied)    │
│  Non-Functional     : N   (manual — Vansah label applied)    │
│  Accessibility      : N   (manual — Vansah label applied)    │
└─────────────────────────────────────────────────────────────┘

Vansah label mapping:
  @Automatable    → label "Automatable"     set on each test case
  @NonAutomatable → label "Non-Automatable" set on each test case

Ready to import ALL [N] scenarios to Vansah.
Please provide the FolderIdentifier — the Vansah folder UUID where
test cases should be created (found in the Vansah folder URL or settings).
```

Wait for the user's answer. Store it as FOLDER_ID.

---

## Step 9 — Update VansahConfig.json

Update vFluxAutomation/VansahConfig.json — ONLY these three fields:
  "FeatureFileName":  "[FEATURE_NAME].feature"
  "FeatureFilePath":  "[FEATURE_PATH]"
  "FolderIdentifier": "[FOLDER_ID]"

Do NOT change VansahApiUrl, VansahToken, ProjectKey, or TypeIdentifier.

---

## Step 10 — Build check

Run:
  dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"

Fix any CS errors before proceeding.

---

## Step 11 — Import to Vansah

Run:
  dotnet test vFluxAutomation/vFluxAutomation.csproj --filter "Category=VansahImport" --logger "console;verbosity=detailed"

Parse the output and report:
- Total scenarios imported (created / failed)
- VFL-CXXXX IDs created
- Label assigned per case ([Automatable] / [Non-Automatable])
- Any [FAIL] or [WARN] lines with detail

---

## Step 12 — Final summary

Output:
```
  Feature file:              [FEATURE_PATH]/[FEATURE_NAME].feature
  Step definitions:          [STEPS_PATH]/[STEPS_NAME].cs
  Automatable scenarios:     N  → full step definitions + @smokeBDD tag
  Non-Automatable scenarios: N  → @ignore tag + @Manual tag, no step definitions
  New helper methods:        [list or "none"]
  New Demo.json keys:        [list or "none"]
  New Global constants:      [list or "none"]
  VansahConfig.json:         FeatureFileName, FeatureFilePath, FolderIdentifier updated
  Vansah import:             N created, 0 failed — VFL-CXXXX to VFL-CXXXX

  Vansah label mapping (handled automatically by VansahImporter):
    @Automatable    → "Automatable"     label on each test case in Vansah
    @NonAutomatable → "Non-Automatable" label on each test case in Vansah

  CI test run (Automatable only):
  dotnet test vFluxAutomation/vFluxAutomation.csproj --filter "Category=smokeBDD"
```
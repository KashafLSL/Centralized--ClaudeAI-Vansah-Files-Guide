Generate BDD scenarios with full coverage + step definitions for a Jira story.

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

---

## Step 3 — Build coverage matrix (internal check before writing)

Before writing a single scenario, confirm EVERY row below is covered:

| Coverage category        | Min | Notes |
|--------------------------|-----|-------|
| Happy path               | 1   | Primary success flow, end-to-end |
| Per user role            | 1 per role | Each role's distinct action |
| Per acceptance criterion | 1 per AC   | Direct AC mapping |
| Workflow states          | 1 per state | Pending, Approved, Rejected, etc. |
| Status / column display  | 1   | UI shows correct values after each action |
| Positive data variations | 1+ | Valid inputs that should succeed |
| Negative — required field missing | 1 per field | Each mandatory field left blank |
| Negative — invalid format | 1 per field | Wrong type / format input |
| Negative — quantity boundary | 3 | Below limit, at limit, above limit |
| Role restriction         | 1 per rule | Action blocked for unauthorised role |
| Business restriction     | 1 per rule | Z99, Guardian, duplicate submission, etc. |
| Workflow ordering        | 1   | Step N cannot happen before Step N-1 |
| Rejection path           | 1 per approver | Each approver rejects |
| Edge cases               | 1+  | Empty state, special characters, max-length input, concurrent actions |
| Full E2E lifecycle       | 1   | Submission → all approvals → final state |
| Data-driven (Outline)    | 1 outline | Min 3 rows in Examples table |

---

## Step 4 — Write the Feature File

Create: vFluxAutomation/[FEATURE_PATH]/[FEATURE_NAME].feature

Rules:
- Every scenario tagged: @smokeBDD @Smoke @Regression @[STORY_ID]
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

Gherkin discipline:
- Given = state / precondition
- When  = user action
- Then  = assertion / outcome
- And   = continuation of previous keyword type
- Never jump from Given to Then without a When

---

## Step 5 — Write Step Definitions

Create: vFluxAutomation/[STEPS_PATH]/[STEPS_NAME].cs

Namespace: vFluxAutomation.[STEPS_PATH with / replaced by .]
Class:     [Binding] public class [STEPS_NAME]

Mandatory patterns:
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

Available helpers (use existing methods first — never duplicate):
- vFluxHelper.Instance         → browser, waits, navigation
- BNYHelper.Instance           → BNY grid, search, export, columns, toast, approver queue
- LoginHelper.Instance         → FillLoginControls(), ClickButton()
- ColumnChooserHelper.Instance → column chooser operations
- ElectionsHelper.Instance     → election queue actions
- AdminHelper.Instance         → admin panel operations

Global user constants (never invent string literals):
- Global.USER_BNY      = "KashafApprover"   (BNY Approver)
- Global.CORR_USER     = "corrapp"           (Correspondent Approver)
- Global.APPROVER_USER = "qaapprover"        (Main Approver)
- Global.SIMPLE_USER   = "qauser"            (Simple User)
- Global.ADMIN_USER    = "adminuserr"        (Admin)
- Global.PASSWORD      = "Test@1235"
- Global.ADMIN_PASSWORD = "Test@12345"

When a new helper method is needed: add it to the most relevant existing helper class.
When a new Global constant is needed: add it to WebControls/Global.cs (ALL_CAPS_WITH_UNDERSCORES).

---

## Step 6 — Update Demo.json

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

## Step 7 — Ask for FolderIdentifier

After Steps 4–6 are complete, output a coverage summary table then ask:

```
┌─ Coverage summary ──────────────────────────────────────┐
│  Total scenarios : N                                     │
│  Happy path      : N                                     │
│  Role-based      : N                                     │
│  Positive        : N                                     │
│  Negative        : N   (validation + format + boundary)  │
│  Business rules  : N                                     │
│  Edge cases      : N                                     │
│  E2E lifecycle   : N                                     │
└──────────────────────────────────────────────────────────┘

Ready to import to Vansah.
Please provide the FolderIdentifier — the Vansah folder UUID where
test cases should be created (found in the Vansah folder URL or settings).
```

Wait for the user's answer. Store it as FOLDER_ID.

---

## Step 8 — Update VansahConfig.json

Update vFluxAutomation/VansahConfig.json — ONLY these three fields:
  "FeatureFileName":  "[FEATURE_NAME].feature"
  "FeatureFilePath":  "[FEATURE_PATH]"
  "FolderIdentifier": "[FOLDER_ID]"

Do NOT change VansahApiUrl, VansahToken, ProjectKey, or TypeIdentifier.

---

## Step 9 — Build check

Run:
  dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"

Fix any CS errors before proceeding.

---

## Step 10 — Final summary

Output:
```
  Feature file:         [FEATURE_PATH]/[FEATURE_NAME].feature
  Step definitions:     [STEPS_PATH]/[STEPS_NAME].cs
  New helper methods:   [list or "none"]
  New Demo.json keys:   [list or "none"]
  New Global constants: [list or "none"]
  VansahConfig.json:    FeatureFileName, FeatureFilePath, FolderIdentifier updated

  Run to import:
  dotnet test vFluxAutomation/vFluxAutomation.csproj --filter "Category=VansahImport" --logger "console;verbosity=detailed"
```
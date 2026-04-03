Generate BDD scenarios with full coverage + step definitions for a Jira story. Works with ANY framework/language. Updates vansah-config.json but does NOT run the import — user runs vansah-import.exe manually after review.
Automatable scenarios get full step definitions. Non-Automatable scenarios get
the @ignore tag (or framework equivalent) and NO step definitions.
ALL scenarios are imported to Vansah with the correct label via the "label" field (singular).

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Jira Story number            (e.g. VFL-8283)
2. Framework / Language         (e.g. Java/Cucumber, Python/Behave, JS/Playwright, Ruby/Cucumber, C#/SpecFlow)
3. Feature file name            (e.g. BNYCommentsVisibility  ← no extension)
4. Feature file path            (e.g. src/test/resources/features  or  features/BNY)
5. Step definition file name    (e.g. BNYCommentsVisibilitySteps  ← no extension)
6. Step definition path         (e.g. src/test/java/steps  or  step_definitions/bny)
7. vansah-config.json location  (e.g. root of project, or path where the file lives)
```

Wait for the user's answers. Store as:
- STORY_ID      = answer to 1
- FRAMEWORK     = answer to 2
- FEATURE_NAME  = answer to 3
- FEATURE_PATH  = answer to 4
- STEPS_NAME    = answer to 5
- STEPS_PATH    = answer to 6
- CONFIG_PATH   = answer to 7

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
No step definitions are needed or generated for these.

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

Create: [STEPS_PATH]/[STEPS_NAME].[ext]

Generate step definitions ONLY for @Automatable scenarios, in the language/framework specified by FRAMEWORK.
Do NOT generate any step definition methods for steps that belong exclusively to @NonAutomatable scenarios.

### Java / Cucumber
```java
import io.cucumber.java.en.*;
import static org.junit.Assert.*;

public class [STEPS_NAME] {
    @Given("...")   public void given() { /* TODO */ }
    @When("...")    public void when()  { /* TODO */ }
    @Then("...")    public void then()  { /* TODO */ }
}
```

### Python / Behave
```python
from behave import given, when, then

@given('...') def step(context): pass   # TODO
@when('...')  def step(context): pass   # TODO
@then('...')  def step(context): pass   # TODO
```

### JavaScript / Playwright + Cucumber
```javascript
const { Given, When, Then } = require('@cucumber/cucumber');
Given('...', async function () { /* TODO */ });
When('...',  async function () { /* TODO */ });
Then('...',  async function () { /* TODO */ });
```

### TypeScript / Playwright + Cucumber
```typescript
import { Given, When, Then } from '@cucumber/cucumber';
Given('...', async function () { /* TODO */ });
When('...',  async function () { /* TODO */ });
Then('...',  async function () { /* TODO */ });
```

### Ruby / Cucumber
```ruby
Given('...') do end   # TODO
When('...')  do end   # TODO
Then('...')  do end   # TODO
```

### C# / SpecFlow (non-vFlux)
```csharp
using TechTalk.SpecFlow;
using NUnit.Framework;
namespace [Project].StepDefinitions {
    [Binding] public class [STEPS_NAME] {
        [Given(@"...")] public void Given() { /* TODO */ }
        [When(@"...")]  public void When()  { /* TODO */ }
        [Then(@"...")]  public void Then()  { /* TODO */ }
    }
}
```

Rules for all frameworks:
- Each Gherkin step maps to exactly one method
- Step text must match feature file exactly
- Add TODO comments where real locators/selectors are needed
- No hardcoded credentials — use environment variables or config

---

## Step 7 — Ask for FolderIdentifier

After Steps 5–6 are complete, output a coverage summary then ask:

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

Ready to update config.
Please provide the FolderIdentifier — the Vansah folder UUID where
test cases should be created (found in the Vansah folder URL or folder settings).
```

Wait for the user's answer. Store it as FOLDER_ID.

---

## Step 8 — Update vansah-config.json

Update [CONFIG_PATH]/vansah-config.json — ONLY these three fields:
  "FeatureFilePath":  "[FEATURE_PATH]"
  "FeatureFileName":  "[FEATURE_NAME].feature"
  "FolderIdentifier": "[FOLDER_ID]"

Do NOT change VansahApiUrl, VansahToken, ProjectKey, or TypeIdentifier.

If vansah-config.json does not exist at [CONFIG_PATH], create it:
```json
{
  "VansahApiUrl":     "https://prodde.vansah.com",
  "VansahToken":      "<your-vansah-token>",
  "ProjectKey":       "<your-project-key>",
  "FolderIdentifier": "[FOLDER_ID]",
  "TypeIdentifier":   "",
  "FeatureFilePath":  "[FEATURE_PATH]",
  "FeatureFileName":  "[FEATURE_NAME].feature"
}
```
Remind the user to fill in VansahToken and ProjectKey before running import.

---

## Step 9 — Final summary

Output:
```
  Feature file:              [FEATURE_PATH]/[FEATURE_NAME].feature
  Step definitions:          [STEPS_PATH]/[STEPS_NAME].[ext]
  Framework:                 [FRAMEWORK]
  Automatable scenarios:     N  → full step definitions
  Non-Automatable scenarios: N  → @ignore tag, no step definitions
  vansah-config.json:        FeatureFilePath, FeatureFileName, FolderIdentifier updated

  Vansah label mapping (sent via "label" field — singular):
    @Automatable    → "Automatable"     label on each test case in Vansah
    @NonAutomatable → "Non-Automatable" label on each test case in Vansah

  When ready to import, run:
  vansah-import [FEATURE_PATH]/[FEATURE_NAME].feature

  Or config-driven (no argument):
  vansah-import
```
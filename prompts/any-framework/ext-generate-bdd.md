Generate BDD scenarios with full coverage + step definitions for a Jira story. Works with ANY framework/language. Updates vansah-config.json but does NOT run the import — user runs vansah-import.exe manually after review.

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

---

## Step 3 — Build coverage matrix (internal check before writing)

Before writing a single scenario, confirm EVERY row below is covered:

| Coverage category                    | Min        | Notes |
|--------------------------------------|------------|-------|
| Happy path                           | 1          | Primary success flow, end-to-end |
| Per user role                        | 1 per role | Each role's distinct action |
| Per acceptance criterion             | 1 per AC   | Direct AC mapping |
| Workflow states                      | 1 per state | Pending, Approved, Rejected, etc. |
| Status / column display              | 1          | UI shows correct values after each action |
| Positive data variations             | 1+         | Valid inputs that should succeed |
| Negative — required field missing    | 1 per field | Each mandatory field left blank |
| Negative — invalid format            | 1 per field | Wrong type / format input |
| Negative — quantity boundary         | 3          | Below limit, at limit, above limit |
| Role restriction                     | 1 per rule | Action blocked for unauthorised role |
| Business restriction                 | 1 per rule | Z99, Guardian, duplicate submission, etc. |
| Workflow ordering                    | 1          | Step N cannot happen before Step N-1 |
| Rejection path                       | 1 per approver | Each approver rejects |
| Edge cases                           | 1+         | Empty state, special characters, max-length input |
| Full E2E lifecycle                   | 1          | Submission → all approvals → final state |
| Data-driven (Outline)                | 1 outline  | Min 3 rows in Examples table |

---

## Step 4 — Write the Feature File

Create: [FEATURE_PATH]/[FEATURE_NAME].feature

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

Create: [STEPS_PATH]/[STEPS_NAME].[ext]

Generate step definitions in the language/framework specified by FRAMEWORK:

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

## Step 6 — Ask for FolderIdentifier

After Steps 4–5 are complete, output a coverage summary then ask:

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

Ready to update config.
Please provide the FolderIdentifier — the Vansah folder UUID where
test cases should be created (found in the Vansah folder URL or folder settings).
```

Wait for the user's answer. Store it as FOLDER_ID.

---

## Step 7 — Update vansah-config.json

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

## Step 8 — Final summary

Output:
```
  Feature file:         [FEATURE_PATH]/[FEATURE_NAME].feature
  Step definitions:     [STEPS_PATH]/[STEPS_NAME].[ext]
  Framework:            [FRAMEWORK]
  vansah-config.json:   FeatureFilePath, FeatureFileName, FolderIdentifier updated

  When ready to import, run:
  vansah-import [FEATURE_PATH]/[FEATURE_NAME].feature

  Or config-driven (no argument):
  vansah-import
```

# vFluxAutomation — Claude Rulebook

This file is automatically loaded by Claude Code for every conversation in this project.
All rules below are MANDATORY and override Claude's default behaviour.

---

## 1. Project Identity

| Item | Value |
|------|-------|
| Solution | `vFluxAutomation.sln` |
| Framework | `.NET Core 3.1` · NUnit · SpecFlow 3.9 |
| Root namespace | `vFluxAutomation` |
| Active branch | `vFluxSmoke_BDDMerging` |
| Vansah project key | `VFL` |

---

## 2. Folder Conventions (NEVER deviate)

| Artifact | Path pattern |
|----------|-------------|
| AI-generated feature files | `vFluxAutomation/FeatureFiles/AITestingBDD/<FeatureName>.feature` |
| Manual feature files | `vFluxAutomation/FeatureFiles/<Area>/<FeatureName>Feature.feature` |
| Step definitions | `vFluxAutomation/StepDefinitions/<Area>/<FeatureName>Steps.cs` |
| Helper classes | `vFluxHelperLibrary/<Area>Helper.cs` |
| UI element keys | `vFluxAutomation/Demo.json` |
| Import config | `vFluxAutomation/VansahConfig.json` |
| Vansah tools | `vFluxAutomation/Tools/` |

---

## 3. BDD Scenario Rules

### 3.1 Mandatory tags on EVERY scenario
```gherkin
@smokeBDD @Smoke @Regression @VFL-XXXX
```

### 3.2 Precondition comment
Every scenario MUST have a precondition comment directly above it:
```gherkin
# Precondition: User is logged in as Simple User and BNY Active grid is visible.
```

### 3.3 Scenario coverage — ALWAYS include ALL of these categories

| Category | Rule |
|----------|------|
| **Happy path** | At least 1 scenario for the primary success flow |
| **Role-based** | One scenario per distinct user role involved |
| **Validation / Negative** | One scenario per validation rule (quantity, required fields, format) |
| **Restriction** | One scenario per business restriction (Z99, Guardian, unauthorized role) |
| **Workflow states** | One scenario per intermediate workflow state (Pending, Approved, Rejected) |
| **Status display** | Verify UI columns / status fields reflect correct values after each action |
| **Data-driven** | Use `Scenario Outline` + `Examples` whenever 2+ similar data combinations exist |
| **Edge cases** | Boundary values, empty inputs, duplicate submissions |
| **End-to-end** | At least 1 full lifecycle scenario covering the complete flow |

### 3.4 Scenario titles
- Business-readable, not technical
- GOOD: `"Correspondent Approver approves an election request and forwards it to Main Approver"`
- BAD: `"Test approve button click"`

### 3.5 Gherkin step discipline
- `Given` = precondition/state
- `When` = user action
- `Then` = assertion/outcome
- `And` = continuation of same keyword type
- Never skip directly from `Given` to `Then` without a `When`

---

## 4. Step Definition Rules

### 4.1 Namespace and class
```csharp
namespace vFluxAutomation.StepDefinitions.<Area>
{
    [Binding]
    public class <FeatureName>Steps { }
}
```

### 4.2 Assertion pattern (ALWAYS this format)
```csharp
Assert.AreEqual(BNYHelper.Instance.ClickButton("ElementKey"), Global.SUCCESS);
Assert.AreEqual(BNYHelper.Instance.ValidateText("ElementKey", "Expected Text"), Global.SUCCESS);
```

### 4.3 Wait patterns
```csharp
// Wait for visibility
vFluxHelper.Instance.FluentWaitTillElementIsVisible(timeout, 1, "ElementKey");

// Wait for clickable
vFluxHelper.Instance.FluentWaitTillElementToBeClickable(timeout, 1, "ElementKey");

// Implicit wait (milliseconds)
vFluxHelper.Instance.ImplicitWait(2000);
```

### 4.4 Login pattern — CRITICAL
`LoginHelper` has NO `Login(user, pass)` method. ALWAYS use:
```csharp
Assert.AreEqual(
    LoginHelper.Instance.FillLoginControls(
        "LoginUsername", Global.USER_BNY,
        "LoginPassword", Global.PASSWORD),
    Global.SUCCESS);
Assert.AreEqual(LoginHelper.Instance.ClickButton("LoginButton"), Global.SUCCESS);
vFluxHelper.Instance.ImplicitWait(2000);
```

---

## 5. Helper Class Rules

### 5.1 Use existing helpers — never create a new helper class unless NO existing one fits

| Helper | Responsibility |
|--------|---------------|
| `vFluxHelper.Instance` | Browser, waits, navigation, page refresh |
| `BNYHelper.Instance` | BNY grid, search, export, columns, toast, approver queue navigation |
| `LoginHelper.Instance` | `FillLoginControls()`, `ClickButton()` — login, menu clicks |
| `ElectionsHelper.Instance` | Election queue operations |
| `AdminHelper.Instance` | Admin panel operations |
| `ColumnChooserHelper.Instance` | Column chooser dialog |

### 5.2 Adding new methods to helpers
- Add to the most relevant existing helper
- Follow the singleton pattern (`Instance` property, private constructor)
- Always return `Global.SUCCESS` or `Global.FAILURE`
- Log with `Logger.LogMessage(...)` before returning

### 5.3 New helper class (only if needed)
- Create `vFluxHelperLibrary/<Area>Helper.cs`
- Use `BNYHelper.cs` as the exact template
- Copy the singleton block, `webDriver` field, and `Instance` property unchanged

---

## 6. Global Constants Rules

### 6.1 NEVER invent string literals for usernames, passwords, messages, or event IDs
Always use a `Global.XXX` constant from `WebControls/Global.cs`.

### 6.2 Standard user constants
| Constant | Value | Role |
|----------|-------|------|
| `Global.USER_BNY` | `KashafApprover` | BNY Approver |
| `Global.CORR_USER` | `corrapp` | Correspondent Approver |
| `Global.SECOND_CORR_USER` | `corrapp1` | Second Correspondent Approver |
| `Global.APPROVER_USER` | `qaapprover` | Main Approver |
| `Global.SIMPLE_USER` | `qauser` | Simple User |
| `Global.ADMIN_USER` | `adminuserr` | Admin |
| `Global.PASSWORD` | `Test@1235` | Standard password |
| `Global.ADMIN_PASSWORD` | `Test@12345` | Admin password |

### 6.3 When a new constant is needed
Add it to `WebControls/Global.cs` under the relevant section comment.
Use the existing naming convention (ALL_CAPS_WITH_UNDERSCORES).

---

## 7. Demo.json Rules

### 7.1 Never hardcode XPath in step definitions
All element locators must be string keys resolved via `Demo.json`.

### 7.2 New key format
```json
"BNY_my_element_key": {
    "ControlType": "Button",
    "IdentifierType": "XPath",
    "Identifier": "TODO: add XPath - description of element"
}
```

### 7.3 ControlType values
| Type | Use for |
|------|---------|
| `Button` | Clickable elements |
| `Textbox` | Input fields |
| `No Action` | Read-only / validation elements |

### 7.4 Key naming convention
`<Module>_<description>_<type>` — e.g. `BNY_corr_approve_btn`, `BNY_request_status_field`

---

## 8. VansahConfig.json Rules

### 8.1 Fields Claude updates automatically
- `FeatureFileName` — set to the new feature file name

### 8.2 Fields Claude NEVER changes without explicit user input
- `VansahApiUrl`, `VansahToken`, `ProjectKey`, `TypeIdentifier`
- `FolderIdentifier` — ALWAYS ask the user to provide the target folder UUID; never assume or hardcode a value

### 8.2a FolderIdentifier rule
Before updating `FolderIdentifier`, Claude MUST stop and ask:
  "What is the FolderIdentifier (Vansah folder UUID) you want to import into?"
Only set it after the user supplies the value.

### 8.3 FeatureFilePath
- Keep as `FeatureFiles/AITestingBDD` for all AI-generated files
- Only change if the file is in a subfolder (e.g. `FeatureFiles/AITestingBDD/UsingVansah`)

---

## 9. Vansah Import Rules

### 9.1 Run command
```bash
dotnet test --filter "Category=VansahImport"
# or with detailed output:
dotnet test vFluxAutomation/vFluxAutomation.csproj --filter "Category=VansahImport" --logger "console;verbosity=detailed"
```

### 9.2 Always build-check before import
```bash
dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"
```
Zero errors required before running import.

### 9.3 API 4-step order (NEVER reorder)
1. `POST /api/v1/testCase` — create case
2. `PUT /api/v1/testCase/{id}` — set BDD mode ← MUST be before step 3
3. `POST /api/v1/testCase/{id}/testScript` — create BDD record
4. `PUT /api/v1/testCase/testScript/{scriptId}` — write steps

---

## 10. Code Quality Rules

- No string literals for element keys — always use `Demo.json` keys
- No `Thread.Sleep()` for waits — use `FluentWait` or `ImplicitWait`
- No empty catch blocks
- No hardcoded URLs, credentials, or event IDs in step definitions
- Step definition methods must delegate to a helper — no direct Selenium in steps
- One step definition class per feature file
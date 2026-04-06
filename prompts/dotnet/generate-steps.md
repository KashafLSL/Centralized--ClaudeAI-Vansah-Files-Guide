Generate step definitions for an existing Gherkin feature file.
Only @Automatable scenarios get step definitions. @NonAutomatable scenarios (tagged @ignore)
are skipped entirely — no step definitions generated for them.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Feature file path          (e.g. FeatureFiles/AITestingBDD/BNYCommentsVisibility.feature)
2. Step definition file name  (e.g. BNYCommentsVisibilitySteps  ← no extension)
3. Step definition path       (e.g. StepDefinitions/BNY)
```

Wait for the user's answers. Store as:
- FEATURE_FILE = answer to 1
- STEPS_NAME   = answer to 2
- STEPS_PATH   = answer to 3

---

## Step 2 — Read the feature file

Read the file at vFluxAutomation/[FEATURE_FILE] and extract:
- All @Automatable scenarios and every Given/When/Then/And/But step within them
- All @NonAutomatable scenarios — note their step text but do NOT generate definitions for them
- Any Scenario Outline step patterns with parameter placeholders (e.g. "<Role>", "<Amount>")
- The story ID from the Feature line or tags (e.g. @VFL-8283)

---

## Step 3 — Check for existing step definitions

Before generating, check whether a step definition file already exists at
vFluxAutomation/[STEPS_PATH]/[STEPS_NAME].cs.

If it exists:
- Read it
- Identify which steps already have bindings
- Only generate bindings for steps that do NOT yet have a matching [Given/When/Then] attribute
- Append new methods to the existing class — do not overwrite existing ones

If it does not exist:
- Generate the full file from scratch

---

## Step 4 — Generate step definitions

Create or update: vFluxAutomation/[STEPS_PATH]/[STEPS_NAME].cs

### File structure
```csharp
using TechTalk.SpecFlow;
using NUnit.Framework;
using vFluxHelperLibrary;
using WebControls;

namespace vFluxAutomation.[STEPS_PATH with / replaced by .]
{
    [Binding]
    public class [STEPS_NAME]
    {
        // ── Section name matching feature file ────────────────────────────────
        // step definitions grouped by section...
    }
}
```

### Automatable steps — full implementation patterns

Login step:
```csharp
Assert.AreEqual(LoginHelper.Instance.FillLoginControls(
    "LoginUsername", Global.APPROVER_USER,
    "LoginPassword", Global.PASSWORD), Global.SUCCESS);
Assert.AreEqual(LoginHelper.Instance.ClickButton("LoginButton"), Global.SUCCESS);
vFluxHelper.Instance.ImplicitWait(2000);
```

Click / action:
```csharp
vFluxHelper.Instance.FluentWaitTillElementToBeClickable(10, 1, "ElementKey");
Assert.AreEqual(BNYHelper.Instance.ClickButton("ElementKey"), Global.SUCCESS);
```

Visibility assertion:
```csharp
vFluxHelper.Instance.FluentWaitTillElementIsVisible(10, 1, "ElementKey");
Assert.AreEqual(BNYHelper.Instance.ValidateElementVisible("ElementKey"), Global.SUCCESS);
```

Not-visible assertion:
```csharp
Assert.AreEqual(BNYHelper.Instance.ValidateElementNotVisible("ElementKey"), Global.SUCCESS);
```

Text assertion:
```csharp
Assert.AreEqual(BNYHelper.Instance.ValidateText("ElementKey", "Expected Text"), Global.SUCCESS);
```

### NonAutomatable steps — NO step definitions
Do NOT generate any step definition methods for steps belonging exclusively to @NonAutomatable scenarios.
The @ignore tag on those scenarios means SpecFlow never looks for their step bindings.

### Helper selection rules
Use the most relevant existing helper — never create a new one unless no existing helper fits:
- `vFluxHelper.Instance`         → browser, waits, navigation, scroll, implicit wait
- `BNYHelper.Instance`           → BNY grid, search, export, columns, toast, approver queue, visibility/text validation
- `LoginHelper.Instance`         → FillLoginControls(), ClickButton() — login and menu clicks only
- `ElectionsHelper.Instance`     → election queue operations
- `AdminHelper.Instance`         → admin panel operations
- `ColumnChooserHelper.Instance` → column chooser dialog

### Global user constants — NEVER invent string literals
```
Global.USER_BNY       → BNY Approver
Global.CORR_USER      → Correspondent Approver
Global.APPROVER_USER  → Main Approver
Global.SIMPLE_USER    → Simple User
Global.ADMIN_USER     → Admin
Global.PASSWORD       → standard password
Global.ADMIN_PASSWORD → admin password
```

When a step involves a role name from a Scenario Outline parameter, map it with a switch:
```csharp
[Given(@"I am logged in as ""(.*)"" and the tab is open")]
public void GivenIAmLoggedInAs(string role)
{
    string username;
    switch (role)
    {
        case "Simple User":             username = Global.SIMPLE_USER;    break;
        case "Correspondent Approver":  username = Global.CORR_USER;      break;
        case "BNY Approver":            username = Global.USER_BNY;       break;
        default:                        username = Global.APPROVER_USER;  break;
    }
    Assert.AreEqual(LoginHelper.Instance.FillLoginControls(
        "LoginUsername", username, "LoginPassword", Global.PASSWORD), Global.SUCCESS);
    Assert.AreEqual(LoginHelper.Instance.ClickButton("LoginButton"), Global.SUCCESS);
    vFluxHelper.Instance.ImplicitWait(2000);
}
```

---

## Step 5 — Update Demo.json

For every new UI element key used in the step definitions that does NOT already exist in Demo.json,
append a placeholder entry to vFluxAutomation/Demo.json:

```json
"ElementKey": {
    "ControlType": "Button|Textbox|No Action",
    "IdentifierType": "XPath",
    "Identifier": "TODO: add XPath - <description of element>"
}
```

Key naming: `<Module>_<description>_<type>` — e.g. `BNY_event_popup_close_btn`, `BNY_comment_section_header`

---

## Step 6 — Build check

Run:
  dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"

Fix any CS errors before proceeding.

---

## Step 7 — Final summary

Output:
```
  Step definitions : vFluxAutomation/[STEPS_PATH]/[STEPS_NAME].cs
  Steps generated  : N  (for @Automatable scenarios only)
  Steps skipped    : N  (@NonAutomatable — @ignore tag, no binding needed)
  New helper methods  : [list or "none"]
  New Demo.json keys  : [list or "none"]
  New Global constants: [list or "none"]
  Build             : passed

  Next steps:
    Import to Vansah : /dotnet:vansah-import  (after updating VansahConfig.json)
    Run CI tests     : dotnet test --filter "Category=smokeBDD"
```
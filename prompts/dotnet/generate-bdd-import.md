Generate BDD scenarios + step definitions AND immediately import to Vansah in one shot.

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

Wait for the user's answers before proceeding. Store them as:
- STORY_ID       = answer to 1
- FEATURE_NAME   = answer to 2
- FEATURE_PATH   = answer to 3
- STEPS_NAME     = answer to 4
- STEPS_PATH     = answer to 5

---

## Step 2 — Read the Jira story

Fetch Jira issue [STORY_ID] and extract:
- Feature summary
- All acceptance criteria
- User roles involved
- Business rules and edge cases

---

## Step 3 — Generate the Feature File

Create the file at:
  vFluxAutomation/[FEATURE_PATH]/[FEATURE_NAME].feature

Rules:
- Use proper Gherkin: Feature, Scenario, Given/When/Then/And/But
- Tag each scenario with: @smokeBDD @Smoke @Regression @[STORY_ID]
- Cover: happy path, role-based access, validation errors, edge cases
- Use Scenario Outline + Examples table when multiple data sets are needed
- Keep scenario titles clear and business-readable
- Include a # Precondition: comment above each scenario

---

## Step 4 — Generate Step Definitions

Create the file at:
  vFluxAutomation/[STEPS_PATH]/[STEPS_NAME].cs

NAMESPACE:     vFluxAutomation.[STEPS_PATH converted to dots]
CLASS:         [Binding] public class [STEPS_NAME]
ASSERTIONS:    Assert.AreEqual(Helper.Instance.Method(...), Global.SUCCESS)
WAITS:         vFluxHelper.Instance.FluentWaitTillElementIsVisible(timeout, 1, "ElementKey")
CLICKABLE:     vFluxHelper.Instance.FluentWaitTillElementToBeClickable(timeout, 1, "ElementKey")
IMPLICIT WAIT: vFluxHelper.Instance.ImplicitWait(milliseconds)

LOGIN PATTERN (always use this — LoginHelper has no Login() method):
  Assert.AreEqual(LoginHelper.Instance.FillLoginControls("LoginUsername", Global.USER_BNY, "LoginPassword", Global.PASSWORD), Global.SUCCESS);
  Assert.AreEqual(LoginHelper.Instance.ClickButton("LoginButton"), Global.SUCCESS);

Available helpers:
- vFluxHelper.Instance         → browser, waits, navigation
- BNYHelper.Instance           → BNY grid, search, export, columns, toast, approver queue
- LoginHelper.Instance         → FillLoginControls(), ClickButton() — NOT Login()
- ColumnChooserHelper.Instance → column chooser operations
- ElectionsHelper.Instance     → election queue actions
- AdminHelper.Instance         → admin operations

Existing Global user constants (never invent new string literals):
- Global.USER_BNY      = "KashafApprover"
- Global.CORR_USER     = "corrapp"
- Global.APPROVER_USER = "qaapprover"
- Global.SIMPLE_USER   = "qauser"
- Global.PASSWORD      = "Test@1235"
- Global.ADMIN_USER    = "adminuserr"

---

## Step 5 — Update Demo.json

For every new UI element key, append a placeholder to vFluxAutomation/Demo.json:

```json
"ElementKey": {
    "ControlType": "Button|Textbox|No Action",
    "IdentifierType": "XPath",
    "Identifier": "TODO: add XPath - <description of element>"
}
```

---

## Step 6 — Ask for FolderIdentifier before import

After Steps 3–5 are complete, ask the user:

```
Scenarios and step definitions are ready.

Before importing to Vansah, please provide:
  FolderIdentifier — the Vansah folder UUID where test cases should be created
  (find it in the Vansah folder URL or folder settings)
```

Wait for the user's answer. Store it as FOLDER_ID.

---

## Step 7 — Update VansahConfig.json

Update vFluxAutomation/VansahConfig.json with ONLY these fields:
  "FeatureFileName":  "[FEATURE_NAME].feature"
  "FeatureFilePath":  "[FEATURE_PATH]"
  "FolderIdentifier": "[FOLDER_ID]"

Do NOT change VansahApiUrl, VansahToken, ProjectKey, or TypeIdentifier.

---

## Step 8 — Build check

Run:
  dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"

If any CS errors are found, fix them before proceeding.

---

## Step 9 — Import to Vansah

Run:
  dotnet test vFluxAutomation/vFluxAutomation.csproj --filter "Category=VansahImport" --logger "console;verbosity=detailed"

---

## Step 10 — Final summary

Report:
```
  Feature file:        [FEATURE_PATH]/[FEATURE_NAME].feature
  Step definitions:    [STEPS_PATH]/[STEPS_NAME].cs
  New helper methods:  [list or "none"]
  New Demo.json keys:  [list or "none"]
  VansahConfig.json:   FeatureFileName, FeatureFilePath, FolderIdentifier updated
  Vansah import:       N scenarios created — VFL-CXXXX to VFL-CXXXX
```
Edit, update, or enhance an existing step definition file.
Handles: fixing broken implementations, adding missing steps, refactoring methods,
updating element keys, adding new helper calls, or any other user-specified changes.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Step definition file path   (e.g. StepDefinitions/BNY/BNYCommentsVisibilitySteps.cs)
2. What changes do you want to make?
   Examples:
     - "Add step definitions for the new scenarios I added to the feature file"
     - "The login step is wrong — it should use Global.USER_BNY not Global.APPROVER_USER"
     - "Replace all Thread.Sleep() calls with FluentWait"
     - "Add the missing step: 'Then the export file is downloaded'"
     - "The element key BNY_popup_close_btn is wrong — rename it to BNY_event_close_btn everywhere"
     - "Add steps for the new Scenario Outline I added"
```

Wait for the user's answers. Store as:
- STEPS_FILE = answer to 1 (full path including filename)
- CHANGES    = answer to 2

---

## Step 2 — Read the current step definition file

Read the file at vFluxAutomation/[STEPS_FILE] and understand:
- The namespace and class name
- All existing [Given/When/Then] bindings and their method bodies
- Which helpers are already used
- Which element keys are referenced

---

## Step 3 — Read the corresponding feature file (if needed)

If the user's request involves adding steps for new scenarios or syncing with a feature file:
- Ask: "Which feature file should I sync with? (provide path)"
- OR if it can be inferred from the class name, read it directly
- Identify all steps in @Automatable scenarios that do NOT yet have a matching binding in the step file

---

## Step 4 — Analyse the requested changes

Determine what needs to change:

**Adding missing step bindings:**
- Identify steps in @Automatable scenarios with no matching [Given/When/Then] attribute
- Generate full implementations for each missing step
- Do NOT generate bindings for steps from @NonAutomatable scenarios

**Fixing an implementation:**
- Locate the exact method(s) affected
- Apply the fix — change only what was asked, leave everything else unchanged

**Renaming an element key:**
- Find every occurrence of the old key string in the file
- Replace all occurrences consistently
- Note: also remind the user to update Demo.json with the new key name

**Refactoring (e.g. removing Thread.Sleep, adding waits):**
- Apply the pattern change consistently across all affected methods

**Any other change:**
- Apply precisely what was asked — do not change unrelated code

---

## Step 5 — Apply the changes

Edit the step definition file. Follow ALL coding rules:

### Implementation patterns

Login (NEVER use LoginHelper.Login()):
```csharp
Assert.AreEqual(LoginHelper.Instance.FillLoginControls(
    "LoginUsername", Global.APPROVER_USER,
    "LoginPassword", Global.PASSWORD), Global.SUCCESS);
Assert.AreEqual(LoginHelper.Instance.ClickButton("LoginButton"), Global.SUCCESS);
vFluxHelper.Instance.ImplicitWait(2000);
```

Wait + click:
```csharp
vFluxHelper.Instance.FluentWaitTillElementToBeClickable(10, 1, "ElementKey");
Assert.AreEqual(BNYHelper.Instance.ClickButton("ElementKey"), Global.SUCCESS);
```

Wait for visibility:
```csharp
vFluxHelper.Instance.FluentWaitTillElementIsVisible(10, 1, "ElementKey");
```

Visibility assertion:
```csharp
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

### Strict rules
- No `Thread.Sleep()` — use `FluentWait` or `ImplicitWait`
- No empty catch blocks
- No hardcoded XPath, URLs, usernames, or passwords in step definitions
- All element references must be string keys resolved via Demo.json
- All user references must use Global constants
- Never generate step definitions for @NonAutomatable scenario steps

### Helper selection
- `vFluxHelper.Instance`         → browser, waits, navigation, scroll
- `BNYHelper.Instance`           → BNY grid, search, export, columns, toast, visibility/text validation
- `LoginHelper.Instance`         → FillLoginControls(), ClickButton() — login only
- `ElectionsHelper.Instance`     → election queue operations
- `AdminHelper.Instance`         → admin panel operations
- `ColumnChooserHelper.Instance` → column chooser dialog

### Global user constants
```
Global.USER_BNY       → BNY Approver
Global.CORR_USER      → Correspondent Approver
Global.APPROVER_USER  → Main Approver
Global.SIMPLE_USER    → Simple User
Global.ADMIN_USER     → Admin
Global.PASSWORD       → standard password
Global.ADMIN_PASSWORD → admin password
```

---

## Step 6 — Update Demo.json (if new element keys were introduced)

For every new UI element key used that does NOT already exist in Demo.json,
append a placeholder entry to vFluxAutomation/Demo.json:

```json
"ElementKey": {
    "ControlType": "Button|Textbox|No Action",
    "IdentifierType": "XPath",
    "Identifier": "TODO: add XPath - <description of element>"
}
```

If an element key was renamed, remind the user to update Demo.json accordingly.

---

## Step 7 — Build check

Run:
  dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"

Fix any CS errors before proceeding.

---

## Step 8 — Final summary

Output:
```
  Step definitions updated : vFluxAutomation/[STEPS_FILE]
  Changes applied          : [brief description of what was changed]
  Steps added              : N  (if any)
  Steps modified           : N  (if any)
  New Demo.json keys       : [list or "none"]
  New Global constants     : [list or "none"]
  Build                    : passed
```
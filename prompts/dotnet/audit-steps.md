Audit a step definition file for code quality violations.
Reads the project CLAUDE.md first to extract custom rules, then applies those
on top of universal BDD + SpecFlow/NUnit rules. Reports findings by severity.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message:

```
Please provide the following details:

1. Step definition file path   (e.g. StepDefinitions/BNY/BNYCommentsVisibilitySteps.cs)
2. Should I also check the corresponding feature file for binding gaps?  (yes / no)
   If yes — feature file path: (e.g. FeatureFiles/AITestingBDD/BNYComments.feature)
```

Wait for the user's answers. Store as:
- STEPS_FILE    = answer to 1
- CHECK_FEATURE = answer to 2 (boolean)
- FEATURE_FILE  = answer to 2b (if provided)

---

## Step 2 — Read CLAUDE.md and extract project rules

Read the CLAUDE.md file at the project root (look for it at `CLAUDE.md`, `../CLAUDE.md`, or `.claude/CLAUDE.md`).

Extract and store as PROJECT_RULES:
- Custom helper class names and their responsibilities
- Global constant names for users, passwords, event IDs
- Forbidden patterns (e.g. banned methods, banned imports)
- Required patterns (e.g. mandatory assertion format, mandatory wait style)
- Naming conventions for element keys, namespaces, class names
- Any project-specific rules not covered by universal checks

If no CLAUDE.md is found, proceed with universal rules only and note this in the report.

---

## Step 3 — Read the step definition file

Read the file at vFluxAutomation/[STEPS_FILE].

Build an internal inventory:
- Class name and namespace
- `[Binding]` attribute presence
- All `[Given/When/Then]` methods: name, step text, line number, body content
- All helper calls: which helper, which method, which element key strings
- All assertion statements
- All imports / using directives
- Any inline strings (credentials, XPaths, URLs, IDs)
- Any `Thread.Sleep()`, `Task.Delay()`, or raw `WebDriverWait` calls
- Any direct Selenium (`driver.FindElement`, `driver.Navigate`) usage

---

## Step 4 — Read the feature file (if CHECK_FEATURE = yes)

Read the feature file at vFluxAutomation/[FEATURE_FILE].

Build two lists:
- AUTOMATABLE_STEPS: all step texts from `@Automatable` scenarios
- NON_AUTOMATABLE_STEPS: all step texts from `@NonAutomatable` / `@ignore` scenarios

---

## Step 5 — Read Demo.json

Read vFluxAutomation/Demo.json.

Build:
- DEFINED_KEYS: all keys that exist in Demo.json
- TODO_KEYS: keys whose `"Identifier"` value starts with `"TODO"`

---

## Step 6 — Run the audit

Apply checks in this order. For each violation found, record:
- Severity (CRITICAL / HIGH / MEDIUM / LOW)
- Line number
- The offending code snippet
- The rule violated
- The recommended fix

### CRITICAL — breaks build or causes test failure

**C1. Thread / timing violations**
- `Thread.Sleep(...)` anywhere in the file
- `Task.Delay(...)` used as a wait in a step
- Raw `new WebDriverWait(driver, ...)` constructed inline in a step method
- Fix: replace with `FluentWaitTillElementIsVisible`, `FluentWaitTillElementToBeClickable`, or `ImplicitWait`

**C2. Element key not in Demo.json**
- Any string literal used as an element key argument to a helper call that is NOT present in DEFINED_KEYS
- Fix: add the key to Demo.json

**C3. Missing [Binding] attribute**
- Class has step definition methods but no `[Binding]` attribute
- Fix: add `[Binding]` above the class declaration

**C4. Step binding for NonAutomatable scenario**
- A step method whose step text matches a step in NON_AUTOMATABLE_STEPS but NOT in AUTOMATABLE_STEPS
- Fix: remove the binding (the `@ignore` tag makes it unreachable)

**C5. Missing step binding for Automatable scenario (if CHECK_FEATURE = yes)**
- A step in AUTOMATABLE_STEPS that has no matching `[Given/When/Then]` in the step file
- Fix: add the missing binding

**C6. Direct Selenium in step method**
- `driver.FindElement(...)`, `driver.Navigate(...)`, `driver.Manage(...)` called directly inside a `[Given/When/Then]` method
- Fix: delegate to the appropriate helper

---

### HIGH — code quality violation

**H1. Hardcoded credentials**
- Any string literal matching a known username or password (check against Global constants in PROJECT_RULES)
- Examples: `"KashafApprover"`, `"corrapp"`, `"qaapprover"`, `"qauser"`, `"adminuserr"`, `"Test@1235"`, `"Test@12345"`
- Fix: use the corresponding `Global.XXX` constant

**H2. Hardcoded URL**
- Any string literal starting with `http://` or `https://` in a step method
- Fix: move to Global constant or config file

**H3. Hardcoded XPath or CSS selector inline**
- String containing `//`, `[@`, `contains(`, `document.querySelector` directly in a step method
- Fix: move to Demo.json and reference by key

**H4. Hardcoded entity / event / election IDs**
- Any string literal matching the pattern of an application ID (check PROJECT_RULES for known formats)
- Fix: use a Global constant or test data config

**H5. Wrong login pattern**
- `LoginHelper.Login(...)` called — this method does not exist
- Missing `ImplicitWait(2000)` after `ClickButton("LoginButton")`
- Fix: use the three-line login pattern from CLAUDE.md

**H6. Wrong assertion format**
- `Assert.IsTrue(helper.Method(...))` instead of `Assert.AreEqual(helper.Method(...), Global.SUCCESS)`
- `Assert.IsFalse(...)` used where a helper returns `Global.FAILURE`
- Fix: wrap in `Assert.AreEqual(..., Global.SUCCESS)` or `Assert.AreEqual(..., Global.FAILURE)`

**H7. Assert.Ignore() in an Automatable step**
- `Assert.Ignore(...)` found in a method bound to an `@Automatable` scenario step
- Fix: implement the step fully or remove the binding if the scenario is actually NonAutomatable

---

### MEDIUM — convention violation

**M1. Wrong namespace**
- Namespace does not match the folder path convention from PROJECT_RULES
- Expected: `vFluxAutomation.StepDefinitions.<Area>` (or as defined in CLAUDE.md)
- Fix: correct the namespace declaration

**M2. Wrong helper for the action**
- Using `vFluxHelper.Instance` for BNY grid operations instead of `BNYHelper.Instance` (or as defined in PROJECT_RULES)
- Using `LoginHelper.Instance` for non-login actions
- Fix: use the helper whose responsibility matches the action

**M3. Element key naming convention violation**
- Key string does not follow the `<Module>_<description>_<type>` convention (e.g. `"approve"`, `"btn1"`)
- Fix: rename to match convention; update Demo.json

**M4. Element key XPath still TODO**
- Key string is in DEFINED_KEYS but also in TODO_KEYS (the XPath has not been filled in)
- Fix: add the correct XPath to Demo.json

**M5. Multiple feature concerns in one step file**
- Step file contains bindings for steps from more than one distinct feature (detected by mismatched step texts vs feature file)
- Fix: split into one step file per feature

**M6. New helper class created inside StepDefinitions folder**
- A class in the step definitions folder has helper-like methods but is not a `[Binding]` step class
- Fix: move to `vFluxHelperLibrary/`

---

### LOW — style / hygiene

**L1. Empty method body**
- A `[Given/When/Then]` method with a completely empty body `{ }`
- Fix: add at minimum `// TODO: implement`

**L2. Empty catch block**
- `catch (Exception) { }` or `catch { }` with no body
- Fix: log the error or rethrow

**L3. Inconsistent wait timeout values**
- Timeout values passed to FluentWait that differ across the file without apparent reason (e.g. mix of 5, 10, 30 with no comment)
- Flag only — add a comment explaining the variation if intentional

**L4. Commented-out code blocks**
- Large blocks of `//` commented code still present
- Flag for removal

**L5. TODO comment without implementation**
- `// TODO` present in a method body (not a stub, but within an otherwise implemented method)
- Flag location

### PROJECT_RULES checks (from CLAUDE.md)
After all universal checks, apply any additional rules extracted in Step 2 that are not already covered above.
Label these as `[PROJECT]` severity prefix matching their nature (CRITICAL / HIGH / MEDIUM / LOW).

---

## Step 7 — Output the audit report

```
╔══════════════════════════════════════════════════════════════════╗
║  AUDIT REPORT                                                    ║
║  File     : vFluxAutomation/[STEPS_FILE]                         ║
║  Framework: C# / SpecFlow (dotnet)                               ║
║  CLAUDE.md: [found / not found]                                  ║
╚══════════════════════════════════════════════════════════════════╝

CRITICAL  ████  N issues
HIGH      ███   N issues
MEDIUM    ██    N issues
LOW       █     N issues
──────────────────────────────────────────────────────────────────

── CRITICAL ───────────────────────────────────────────────────────

  [C1] Line 42  Thread.Sleep(3000)
       Rule   : No Thread.Sleep — use FluentWait or ImplicitWait
       Fix    : vFluxHelper.Instance.ImplicitWait(3000);
                  or
                vFluxHelper.Instance.FluentWaitTillElementIsVisible(10, 1, "key");

  [C2] Line 87  BNYHelper.Instance.ClickButton("btn_approve_unknown")
       Rule   : Element key "btn_approve_unknown" not found in Demo.json
       Fix    : Add entry to Demo.json or correct the key name

── HIGH ───────────────────────────────────────────────────────────

  [H1] Line 31  "KashafApprover"
       Rule   : Hardcoded username — use Global constant
       Fix    : Global.USER_BNY

  [H6] Line 65  Assert.IsTrue(BNYHelper.Instance.ClickButton("key"))
       Rule   : Wrong assertion format
       Fix    : Assert.AreEqual(BNYHelper.Instance.ClickButton("key"), Global.SUCCESS);

── MEDIUM ─────────────────────────────────────────────────────────

  [M1] Line 1   namespace vFluxAutomation
       Rule   : Namespace should be vFluxAutomation.StepDefinitions.BNY
       Fix    : Update namespace declaration

── LOW ────────────────────────────────────────────────────────────

  [L1] Line 99  public void ThenSomething() { }
       Rule   : Empty method body
       Fix    : Add // TODO: implement

──────────────────────────────────────────────────────────────────
SUMMARY
  Total violations : N
  Critical         : N  ← fix before committing
  High             : N  ← fix before merging
  Medium           : N  ← fix in next review cycle
  Low              : N  ← fix when touching the file

  Feature binding gaps (if checked):
    Missing bindings : N steps in @Automatable scenarios have no binding
    Orphan bindings  : N bindings in step file have no matching step in feature file
```

After the report, ask the user:
```
Would you like me to auto-fix any of these? Options:
  A — Fix all CRITICAL issues
  B — Fix all CRITICAL + HIGH issues
  C — Fix specific issues (provide numbers)
  N — No fixes, report only
```

If the user chooses A, B, or C — apply the fixes using the Edit tool following all rules from CLAUDE.md and Step 6 above.
After fixing, re-run `dotnet build vFluxAutomation/vFluxAutomation.csproj 2>&1 | grep ": error CS"` and confirm zero errors.

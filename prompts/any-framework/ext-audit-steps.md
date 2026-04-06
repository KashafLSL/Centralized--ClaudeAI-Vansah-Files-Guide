Audit a step definition file for code quality violations — works with ANY framework/language.
Reads the project CLAUDE.md first to extract custom project rules, then applies those
on top of universal BDD rules and framework-specific rules for the detected language.
Reports findings by severity with auto-fix option.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message:

```
Please provide the following details:

1. Step definition file path   (e.g. step_definitions/bny/BNYCommentsSteps.js)
2. Framework / Language        (e.g. Java/Cucumber, Python/Behave, JS/Playwright, TS/Playwright, Ruby/Cucumber, C#/SpecFlow)
3. Should I also check the corresponding feature file for binding gaps?  (yes / no)
   If yes — feature file path: (e.g. features/BNY/BNYComments.feature)
```

Wait for the user's answers. Store as:
- STEPS_FILE    = answer to 1
- FRAMEWORK     = answer to 2
- CHECK_FEATURE = answer to 3 (boolean)
- FEATURE_FILE  = answer to 3b (if provided)

---

## Step 2 — Read CLAUDE.md and extract project rules

Search for a CLAUDE.md file at:
- `CLAUDE.md` (project root)
- `../CLAUDE.md`
- `.claude/CLAUDE.md`

Read the first one found and extract as PROJECT_RULES:
- Custom helper / page object names and their responsibilities
- Constant names for users, passwords, credentials, application IDs
- Forbidden patterns (banned methods, banned imports, banned patterns)
- Required patterns (mandatory assertion format, required wait style)
- Naming conventions for selectors, locators, classes, files
- Framework-specific project rules
- Any rule that deviates from universal defaults

If no CLAUDE.md is found, proceed with universal + framework-specific rules only. Note this in the report.

---

## Step 3 — Determine framework-specific rules

Based on FRAMEWORK, load the rule set for that language:

### Java / Cucumber
- Forbidden: `Thread.sleep(...)` → use explicit WebDriver waits or Awaitility
- Forbidden: inline `By.xpath("...")` or `By.cssSelector("...")` in step methods → use a Locators/PageObject class
- Forbidden: hardcoded `driver.manage().timeouts().implicitlyWait(...)` in steps
- Required: `@Given`, `@When`, `@Then` annotations on every step method
- Required: assertions via JUnit/TestNG (`assertEquals`, `assertTrue`, `assertThat`)
- Required: typed parameters for Scenario Outline captures (`String role`, `int count`)

### Python / Behave
- Forbidden: `time.sleep(...)` → use explicit waits or `context.browser.implicitly_wait`
- Forbidden: inline CSS/XPath selector strings in step functions → use a locators module
- Required: `@given`, `@when`, `@then` decorators
- Required: assertions via `assert` statement or `behave`-compatible library
- Required: `context` parameter on every step function

### JavaScript / Playwright + Cucumber
- Forbidden: `page.waitForTimeout(...)` → use `page.waitFor*` or `expect().toBeVisible()`
- Forbidden: inline selector strings in step functions → use a selectors / page object file
- Forbidden: `async function` steps that have no `await` inside them
- Required: `Given`, `When`, `Then` from `@cucumber/cucumber`
- Required: `expect(locator).toBeVisible()` / `expect(locator).toHaveText()` style assertions
- Required: `async function` on every step callback

### TypeScript / Playwright + Cucumber
- Same as JS/Playwright plus:
- Forbidden: `any` type on step parameters — use `string`, `number`, etc.
- Required: proper TypeScript types on Scenario Outline capture parameters

### Ruby / Cucumber
- Forbidden: `sleep(N)` → use Capybara's built-in retry / `have_*` matchers
- Forbidden: inline CSS/XPath strings in step definitions → use a selectors module or page object
- Required: `Given`, `When`, `Then` block syntax
- Required: RSpec `expect(...).to have_*` or Minitest assertions
- Required: block parameter for Scenario Outline captures `do |role|`

### C# / SpecFlow (non-vFlux)
- Forbidden: `Thread.Sleep(...)` → use `IWebDriverWait` or Polly
- Forbidden: inline `By.XPath("...")` in step methods → use a locators class
- Required: `[Binding]` attribute on step class
- Required: `[Given/When/Then(@"...")]` attributes with regex
- Required: NUnit or MSTest assertions (`Assert.AreEqual`, `Assert.IsTrue`)

---

## Step 4 — Read the step definition file

Read the file at [STEPS_FILE].

Build an internal inventory:
- Class / module structure and imports
- Framework binding decorator / attribute presence on every step method
- All step methods: name, step text, line number, full body
- All page object / helper / locator references
- All assertion statements
- Any inline selector strings (XPath, CSS, IDs)
- Any timing anti-patterns (`sleep`, `waitForTimeout`, `Thread.Sleep`, `Task.Delay`)
- Any hardcoded credential or URL strings
- Any direct browser / driver API calls that should be in a page object

---

## Step 5 — Read the feature file (if CHECK_FEATURE = yes)

Read the feature file at [FEATURE_FILE].

Build two lists:
- AUTOMATABLE_STEPS: all step texts from `@Automatable` scenarios (not tagged `@ignore`)
- NON_AUTOMATABLE_STEPS: all step texts from scenarios tagged `@ignore` or `@NonAutomatable`

---

## Step 6 — Read locators / selectors file (if identifiable)

If the project has a locators file referenced in PROJECT_RULES or discoverable from the step file imports:
- Read it
- Build DEFINED_LOCATORS: all locator keys / constants defined
- Note any that have TODO / placeholder values

---

## Step 7 — Run the audit

Apply checks in this order. For each violation, record:
- Severity (CRITICAL / HIGH / MEDIUM / LOW)
- Line number
- Offending code snippet
- Rule violated
- Recommended fix in the correct framework syntax

### CRITICAL — breaks test execution or produces false results

**C1. Timing anti-pattern (framework-specific)**

| Framework | Forbidden | Replace with |
|---|---|---|
| Java | `Thread.sleep(N)` | explicit WebDriverWait / Awaitility |
| Python | `time.sleep(N)` | `browser.implicitly_wait` / explicit wait |
| JS/TS | `page.waitForTimeout(N)` | `page.waitFor*` / `expect().toBeVisible()` |
| Ruby | `sleep(N)` | Capybara `have_*` matchers |
| C# | `Thread.Sleep(N)` | `WebDriverWait` / Polly / `ImplicitWait` |

**C2. Step binding for NonAutomatable scenario (if CHECK_FEATURE = yes)**
- A step method whose text matches ONLY a step in NON_AUTOMATABLE_STEPS
- Fix: remove the binding — `@ignore` makes it unreachable

**C3. Missing step binding for Automatable scenario (if CHECK_FEATURE = yes)**
- A step in AUTOMATABLE_STEPS with no matching binding in the step file
- Fix: add the missing binding

**C4. Missing framework binding decorator/attribute**
- A method that looks like a step definition but has no `@Given/@When/@Then` (Java/Python/Ruby) or no `[Given/When/Then]` attribute (C#) or is not registered via `Given/When/Then(...)` (JS/TS)
- Fix: add the correct binding annotation

**C5. Direct browser/driver call in step method**
- Framework | Forbidden pattern
- Java: `driver.findElement(...)` directly in step method
- Python: `context.browser.find_element(...)` directly in step (should be in page object)
- JS/TS: `this.page.locator(...).click()` directly in step (should be in page object)
- Ruby: `find(selector).click` directly in step (should be in page object)
- C#: `driver.FindElement(...)` in step method
- Fix: delegate to page object / helper / locator abstraction

---

### HIGH — code quality violation

**H1. Hardcoded credentials**
- Any string literal matching a known username, password, API key, or token
- Check against PROJECT_RULES constants first; also pattern-match common formats
- Fix: use environment variable, config file, or project constant

**H2. Hardcoded URL**
- Any string literal starting with `http://` or `https://` inside a step method
- Fix: move to config / environment variable

**H3. Inline selector / XPath / CSS in step method**
- Framework | Forbidden pattern
- Java: `By.xpath("//...")` or `By.cssSelector("...")` inline
- Python: `"//..."` or `"css=..."` as direct argument to `find_element`
- JS/TS: `page.locator("//...")` or `page.locator(".class > button")` inline
- Ruby: `find("//...")` or `find(".class")` inline
- C#: `By.XPath("//...")` or `By.CssSelector("...")` inline
- Fix: move selector to locators file / page object; reference by constant

**H4. Missing `async/await` in JS/TS step**
- An `async function` step callback that has no `await` expression
- An `await` used in a non-`async` function
- Fix: add `async` to callback or add missing `await`

**H5. Wrong assertion style for framework**
- Java: using `==` instead of `assertEquals`
- Python: using `==` in an `if` statement instead of `assert`
- JS/TS: using `===` comparison instead of `expect(...).toBe(...)`
- Ruby: using `==` instead of `expect(...).to eq(...)`
- C#: using `Assert.IsTrue(x == y)` instead of `Assert.AreEqual(x, y)`

**H6. Hardcoded entity / record IDs**
- String literals matching application-domain ID patterns (check PROJECT_RULES)
- Fix: use constants or test data config

---

### MEDIUM — convention violation

**M1. Locator not defined in locators file**
- A selector string used as a step argument that is not present in DEFINED_LOCATORS
- Fix: add the locator to the locators file and reference it by name

**M2. Locator defined but still has TODO / placeholder value**
- A key exists in the locators file but its value is a placeholder
- Fix: fill in the real selector

**M3. Step file handles multiple unrelated features**
- Step methods cover steps from more than one distinct feature concern
- Fix: split into one step file per feature

**M4. Scenario Outline parameter not captured correctly**

| Framework | Wrong | Right |
|---|---|---|
| Java | `@Given("I login as {0}")` | `@Given("I login as {string}")` with `String role` param |
| Python | `@given('I login as role')` | `@given('I login as "{role}"')` with `role` param |
| JS/TS | `Given('I login as role', ...)` | `Given('I login as {string}', async (role) => ...)` |
| Ruby | `Given('I login as role')` | `Given('I login as {string}') do \|role\|` |
| C# | `[Given(@"I login as role")]` | `[Given(@"I login as ""(.*)""")]` with `string role` |

**M5. Page object method called on wrong page object**
- A step calling a method on a page object whose responsibility doesn't match the action
- Check against PROJECT_RULES helper/page-object responsibility map

---

### LOW — style / hygiene

**L1. Empty step method body**
- A step method with a completely empty body
- Fix: add `// TODO: implement` (or language equivalent)

**L2. Empty catch / rescue / except block**
- `catch {}`, `rescue nil`, `except: pass` with no body
- Fix: log or rethrow the error

**L3. Commented-out code blocks**
- Large sections of commented code remaining in the file
- Flag for removal

**L4. TODO comment inside implemented method**
- `// TODO` / `# TODO` present in a method that otherwise has implementation
- Flag line number

**L5. Inconsistent parameter naming**
- Scenario Outline capture parameters named differently across similar steps (e.g. `role` vs `userRole` vs `user`)
- Flag for consistency

### PROJECT_RULES checks
After all universal and framework-specific checks, apply every rule extracted from CLAUDE.md in Step 2 that is not already covered above.
Label these `[PROJECT]` and assign severity matching the nature of the rule.

---

## Step 8 — Output the audit report

```
╔══════════════════════════════════════════════════════════════════╗
║  AUDIT REPORT                                                    ║
║  File      : [STEPS_FILE]                                        ║
║  Framework : [FRAMEWORK]                                         ║
║  CLAUDE.md : [found at <path> — N custom rules loaded / not found]║
╚══════════════════════════════════════════════════════════════════╝

CRITICAL  ████  N issues
HIGH      ███   N issues
MEDIUM    ██    N issues
LOW       █     N issues
──────────────────────────────────────────────────────────────────

── CRITICAL ───────────────────────────────────────────────────────

  [C1] Line 42  page.waitForTimeout(3000)
       Rule   : No waitForTimeout — use Playwright expect/waitFor pattern
       Fix    : await expect(page.locator(selectors.element)).toBeVisible();

  [C3] Line —   Step "Then the export file is downloaded" has no binding
       Rule   : @Automatable scenario step has no step definition
       Fix    : Add the missing binding

── HIGH ───────────────────────────────────────────────────────────

  [H1] Line 31  "qaapprover"
       Rule   : Hardcoded credential — use project constant or env variable
       Fix    : process.env.APPROVER_USER  (or equivalent for this framework)

  [H3] Line 65  page.locator("//div[@class='bny-grid']//button[1]")
       Rule   : Inline XPath in step — move to selectors file
       Fix    : page.locator(selectors.bnyApproveButton)

── MEDIUM ─────────────────────────────────────────────────────────

  [M2] Line —   Selector "bnyExportBtn" defined in selectors file but value is TODO
       Rule   : Locator placeholder not filled in
       Fix    : Replace TODO value with real CSS/XPath selector

── LOW ────────────────────────────────────────────────────────────

  [L1] Line 99  When('the user clicks export', async function () { })
       Rule   : Empty step body
       Fix    : Add // TODO: implement

──────────────────────────────────────────────────────────────────
CUSTOM PROJECT RULES (from CLAUDE.md)
  [PROJECT-HIGH] Line 55  ...
       Rule   : <rule from CLAUDE.md>
       Fix    : <recommended fix>

──────────────────────────────────────────────────────────────────
SUMMARY
  Total violations : N
  Critical         : N  ← fix before committing
  High             : N  ← fix before merging
  Medium           : N  ← fix in next review cycle
  Low              : N  ← fix when touching the file

  Feature binding gaps:
    Missing bindings : N steps in @Automatable scenarios have no binding
    Orphan bindings  : N bindings in step file whose step no longer exists in feature file
```

After the report, ask:
```
Would you like me to auto-fix any of these?
  A — Fix all CRITICAL issues
  B — Fix all CRITICAL + HIGH issues
  C — Fix specific issues (provide line numbers or rule codes e.g. C1, H3)
  N — No fixes, report only
```

If A, B, or C — apply fixes using the Edit tool, following the framework syntax from Step 3 and all rules from PROJECT_RULES. After fixing, report what was changed and what still requires manual attention (e.g. filling in TODO selectors, adding missing locator constants to the locators file).

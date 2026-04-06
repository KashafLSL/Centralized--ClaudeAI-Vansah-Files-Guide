Audit a step definition file for code quality violations.
Works with ANY testing domain (Web / API / Mobile / Desktop) and ANY framework/language.
Reads the project CLAUDE.md first to load custom rules, then applies universal BDD rules
+ domain-specific rules + framework-specific rules. Reports findings by severity with auto-fix option.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message:

```
Please provide the following details:

1. Step definition file path
   (e.g. step_definitions/BNYSteps.js  /  steps/api/PaymentSteps.java  /  steps/mobile/LoginSteps.py)

2. Testing domain
   Web | API | Mobile | Desktop

3. Framework / Language
   Web     → Playwright/JS, Playwright/TS, Selenium/Java, Selenium/Python, Selenium/C#,
             Selenium/Ruby, Cypress/JS, Cypress/TS, WebdriverIO/JS, WebdriverIO/TS,
             Capybara/Ruby, Behave/Python, Cucumber/Java, SpecFlow/C#
   API     → RestAssured/Java, Requests+Behave/Python, Requests+pytest-bdd/Python,
             Supertest/JS, Axios+Cucumber/JS, Supertest/TS, RestSharp/C#,
             HTTParty+Cucumber/Ruby, Faraday+Cucumber/Ruby
   Mobile  → Appium/Java, Appium/Python, Appium+WebdriverIO/JS, Appium+WebdriverIO/TS,
             Appium/Ruby, Appium+SpecFlow/C#, Detox/JS, Detox/TS,
             XCUITest/Swift, Espresso/Kotlin, Espresso/Java
   Desktop → WinAppDriver+SpecFlow/C#, WinAppDriver+Cucumber/Java,
             Appium+Behave/Python (desktop), Appium+WebdriverIO/JS (desktop),
             PyWinAuto+Behave/Python

4. Should I also check the corresponding feature file for binding gaps?  (yes / no)
   If yes — feature file path:
```

Wait for the user's answers. Store as:
- STEPS_FILE    = answer to 1
- DOMAIN        = answer to 2  (Web / API / Mobile / Desktop)
- FRAMEWORK     = answer to 3
- CHECK_FEATURE = answer to 4 (boolean)
- FEATURE_FILE  = answer to 4b (if provided)

---

## Step 2 — Read CLAUDE.md and extract project rules

Search for a CLAUDE.md file at:
- `CLAUDE.md` (project root)
- `../CLAUDE.md`
- `.claude/CLAUDE.md`

Read the first one found and extract as PROJECT_RULES:
- Custom helper / page object / service client names and their responsibilities
- Constant/config names for credentials, base URLs, device caps, app IDs, endpoints
- Forbidden patterns (banned methods, banned imports, patterns explicitly prohibited)
- Required patterns (mandatory assertion format, required wait style, required abstraction layer)
- Naming conventions for locators, endpoints, constants, classes, files
- Any domain-specific or framework-specific project rules
- Any rule that deviates from the universal defaults below

If no CLAUDE.md is found: proceed with universal + domain + framework rules only. Note this in the report.

---

## Step 3 — Load domain rules

### DOMAIN = Web

**Universal web rules:**
- No timing anti-patterns (hard sleeps) — use the framework's built-in wait/retry mechanism
- No inline selectors (XPath / CSS strings) in step methods — use a locators / page object file
- No direct browser/driver API calls in step methods — delegate to page objects or helper classes
- No hardcoded URLs, credentials, or test data in step methods
- Assertions must check both visibility AND expected value/state — not just "element exists"

**Framework-specific timing forbidden → replacement:**

| Framework | Forbidden | Use instead |
|---|---|---|
| Playwright (JS/TS) | `page.waitForTimeout(N)` | `expect(locator).toBeVisible()` / `page.waitFor*` |
| Selenium (Java) | `Thread.sleep(N)` | `WebDriverWait` / `FluentWait` / Awaitility |
| Selenium (Python) | `time.sleep(N)` | `WebDriverWait` / explicit wait |
| Selenium (C#) | `Thread.Sleep(N)` | `WebDriverWait` / Polly |
| Selenium (Ruby) | `sleep(N)` | Capybara `have_*` matchers |
| Cypress (JS/TS) | `cy.wait(N)` (number, not alias) | `cy.get(sel).should('be.visible')` |
| WebdriverIO (JS/TS) | `browser.pause(N)` | `browser.waitUntil(...)` / `expect(el).toBeDisplayed()` |
| Capybara (Ruby) | `sleep(N)` | `have_*` matchers with Capybara retry |
| Behave (Python) | `time.sleep(N)` | explicit WebDriver wait |

**Framework-specific selector forbidden → replacement:**

| Framework | Forbidden in step | Use instead |
|---|---|---|
| Playwright | `page.locator("//xpath")` inline | `page.locator(selectors.myElement)` |
| Selenium/Java | `By.xpath("//...")` inline | `Locators.MY_ELEMENT` constant |
| Selenium/Python | `"//..."` direct in `find_element` | locators module constant |
| Selenium/C# | `By.XPath("//...")` inline | locators class constant |
| Cypress | `cy.get(".raw-css")` inline | `cy.get(selectors.myElement)` |
| WebdriverIO | `$("//xpath")` inline | `$(Selectors.myElement)` |
| Capybara | `find("//xpath")` inline | page object method |

**Framework-specific binding requirement:**

| Framework | Required binding syntax |
|---|---|
| Playwright/Cucumber (JS/TS) | `Given/When/Then('...', async function() {...})` from `@cucumber/cucumber` |
| Selenium/Cucumber (Java) | `@Given/@When/@Then("...")` annotations + typed parameters |
| Selenium/Behave (Python) | `@given/@when/@then('...')` decorators + `context` param |
| SpecFlow (C#) | `[Binding]` class + `[Given/When/Then(@"...")]` attributes |
| Capybara/Cucumber (Ruby) | `Given/When/Then('...') do ... end` |
| Cypress | no Cucumber bindings — assertions inline in `it()` blocks |
| WebdriverIO/Cucumber | `Given/When/Then('...')` from `@cucumber/cucumber` |

---

### DOMAIN = API

**Universal API rules:**
- No hardcoded base URLs in step methods — use environment variable or config file
- No hardcoded auth tokens, API keys, Bearer tokens, Basic auth credentials in step methods
- No hardcoded request payloads inline in step methods — use fixtures or data files
- No hardcoded endpoint paths as raw strings in step methods — use endpoint constants/config
- No `sleep()/Thread.sleep()/time.sleep()` for polling async responses — use retry library or polling helper
- No SSL verification disabled (`verify=False`, `ssl: false`, `rejectUnauthorized: false`, `disableSSL()`) unless explicitly required by project rules
- Always assert HTTP status code before asserting response body
- No raw HTTP client calls directly in step methods — use a service/API client class
- No logging of sensitive data (tokens, passwords) to console/stdout in steps

**Framework-specific timing forbidden → replacement:**

| Framework | Forbidden | Use instead |
|---|---|---|
| RestAssured (Java) | `Thread.sleep(N)` in steps | Awaitility polling or custom retry |
| Requests (Python) | `time.sleep(N)` in steps | `tenacity` retry or polling helper |
| Supertest / Axios (JS/TS) | `setTimeout(fn, N)` / `await new Promise(r => setTimeout(r,N))` | polling helper with backoff |
| RestSharp (C#) | `Thread.Sleep(N)` in steps | Polly retry policy |
| HTTParty / Faraday (Ruby) | `sleep(N)` in steps | retry middleware or polling helper |

**Framework-specific client pattern:**

| Framework | Forbidden in step | Use instead |
|---|---|---|
| RestAssured (Java) | `given().get("https://...")` inline | API client / service class method |
| Requests (Python) | `requests.get("https://...")` inline | `context.api_client.get(Endpoints.MY_ENDPOINT)` |
| Axios/Supertest (JS/TS) | `axios.get("https://...")` inline | `this.apiClient.get(endpoints.myEndpoint)` |
| RestSharp (C#) | `new RestClient("https://...")` in step | injected `IApiClient` service |
| HTTParty (Ruby) | `HTTParty.get("https://...")` inline | API client class method |

**Required API assertion pattern (all frameworks):**
1. Assert response status code first
2. Then assert response body / headers
3. Optionally assert schema (JSON Schema / OpenAPI validation)

---

### DOMAIN = Mobile

**Universal mobile rules:**
- No hard sleeps — use framework explicit waits or element-level wait strategies
- Prefer Accessibility ID locators over XPath (more stable across OS versions and screen sizes)
- No hardcoded device names, platform versions, or UDIDs in step methods — use desired capabilities config file
- No hardcoded app bundle IDs (iOS) or package names / activity names (Android) in step methods — use config
- No platform-specific (iOS-only or Android-only) code inside shared cross-platform step definitions — use conditional helpers or separate platform step files
- No direct `driver` API calls in step methods — use Screen Objects or Page Objects
- Always verify current context (native vs webview) before interacting with elements when app uses hybrid context
- No hardcoded screen dimensions or coordinates for tap/swipe actions — use relative helpers

**Framework-specific timing forbidden → replacement:**

| Framework | Forbidden | Use instead |
|---|---|---|
| Appium/Java | `Thread.sleep(N)` | `new WebDriverWait(driver, Duration.ofSeconds(N)).until(...)` |
| Appium/Python | `time.sleep(N)` | `WebDriverWait(driver, N).until(...)` |
| Appium/WebdriverIO (JS/TS) | `driver.pause(N)` | `$(el).waitForDisplayed({timeout: N*1000})` |
| Appium/C# | `Thread.Sleep(N)` | `new WebDriverWait(driver, TimeSpan.FromSeconds(N)).Until(...)` |
| Appium/Ruby | `sleep(N)` | `driver.wait_until { driver.find_element(...) }` |
| Detox (JS/TS) | `await new Promise(r => setTimeout(r,N))` | `await waitFor(element(by.id('id'))).toBeVisible().withTimeout(N)` |
| Espresso (Java/Kotlin) | `Thread.sleep(N)` | `onView(...).check(matches(isDisplayed()))` with IdlingResource |
| XCUITest (Swift) | `sleep(N)` | `XCTNSPredicateExpectation` / `.waitForExistence(timeout:)` |

**Framework-specific locator preferred → avoid:**

| Framework | Prefer | Avoid |
|---|---|---|
| Appium/Java | `AppiumBy.accessibilityId("id")` | `By.xpath("//XCUIElementTypeButton[@name='...']")` |
| Appium/Python | `MobileBy.ACCESSIBILITY_ID, "id"` | `MobileBy.XPATH, "//..."` |
| Appium/WebdriverIO | `$('~accessibilityId')` | `$('//XPath')` |
| Appium/C# | `MobileBy.AccessibilityId("id")` | `MobileBy.XPath("//...")` |
| Detox | `element(by.id('testID'))` | `element(by.type('XCUIElementType...'))` |
| Espresso | `withId(R.id.elementId)` | `withTagValue(...)` or complex matchers |
| XCUITest | `app.buttons["AccessibilityLabel"]` | `app.descendants(matching: .button).element(boundBy: 0)` |

---

### DOMAIN = Desktop

**Universal desktop rules:**
- No hard sleeps — use explicit waits or WinAppDriver/Appium wait strategies
- Prefer Automation ID / Accessibility ID over XPath (more stable for desktop elements)
- No hardcoded application executable paths in step methods — use config file
- No hardcoded window titles in step methods — use locators file or constants
- No direct driver API calls in step methods — use Screen Objects or Page Objects
- No hardcoded window coordinates for click actions — use element-based interactions

**Framework-specific timing forbidden → replacement:**

| Framework | Forbidden | Use instead |
|---|---|---|
| WinAppDriver/C# | `Thread.Sleep(N)` | `new WebDriverWait(driver, TimeSpan.FromSeconds(N)).Until(...)` |
| WinAppDriver/Java | `Thread.sleep(N)` | `new WebDriverWait(driver, Duration.ofSeconds(N)).until(...)` |
| Appium Desktop/Python | `time.sleep(N)` | `WebDriverWait(driver, N).until(...)` |
| Appium Desktop/JS | `driver.pause(N)` | `$(el).waitForDisplayed({timeout: N*1000})` |
| PyWinAuto/Python | `time.sleep(N)` | `app.window().wait('ready', timeout=N)` |

**Framework-specific locator preferred → avoid:**

| Framework | Prefer | Avoid |
|---|---|---|
| WinAppDriver/C# | `MobileBy.AccessibilityId("id")` or `By.Name("AutomationId")` | `By.XPath("//...")` |
| WinAppDriver/Java | `MobileBy.accessibilityId("id")` | `By.xpath("//...")` |
| Appium Desktop | `AppiumBy.accessibilityId("id")` | `AppiumBy.xpath("//...")` |
| PyWinAuto | `app.window().child_window(auto_id="AutomationId")` | `.child_window(title="partial text")` |

---

## Step 4 — Read the step definition file

Read the file at [STEPS_FILE].

Build an internal inventory:
- Class / module / file structure and imports/requires
- Framework binding decorator / attribute presence on every step method
- All step methods: name, step text, line number, full body
- All external references: page objects, screen objects, service clients, helper classes, locator files
- All assertion statements
- Any inline selector / XPath / CSS / endpoint / accessibility ID strings
- Any timing anti-patterns (hard sleeps, fixed timeouts)
- Any hardcoded credentials, URLs, base URLs, tokens, device identifiers, app IDs
- Any direct driver / browser / HTTP client calls that should be in an abstraction layer
- Any SSL verification bypass flags
- Any platform-specific code in shared step files

---

## Step 5 — Read the feature file (if CHECK_FEATURE = yes)

Read the feature file at [FEATURE_FILE].

Build two lists:
- AUTOMATABLE_STEPS: all step texts from scenarios NOT tagged `@ignore` or `@NonAutomatable`
- NON_AUTOMATABLE_STEPS: all step texts from scenarios tagged `@ignore` or `@NonAutomatable`

---

## Step 6 — Read the abstraction / config file (based on domain)

Identify the relevant external files from PROJECT_RULES or imports in the step file:

| Domain | File to look for | What to extract |
|---|---|---|
| Web | Locators / Selectors / Page Object file | DEFINED_LOCATORS: all keys/constants; flag TODO/placeholder values |
| API | Endpoints / API constants file | DEFINED_ENDPOINTS: all endpoint paths/constants; flag TODO values |
| Mobile | Locators / Screen Object / Capabilities config | DEFINED_LOCATORS + DEFINED_CAPS: locator keys and device capability keys |
| Desktop | Locators / Screen Object / App config | DEFINED_LOCATORS: automation IDs and locator constants |

Read the identified file(s) and build the relevant DEFINED_* sets.

---

## Step 7 — Run the audit

For each violation record: severity, line number, offending snippet, rule, recommended fix.

---

### CRITICAL — breaks test execution or produces false results

**C1. Timing anti-pattern**
Match against the forbidden timing pattern for the detected DOMAIN + FRAMEWORK from Step 3.
Show the exact replacement pattern in the correct framework syntax.

**C2. Step binding for NonAutomatable scenario (if CHECK_FEATURE = yes)**
A step method whose text appears ONLY in NON_AUTOMATABLE_STEPS and NOT in AUTOMATABLE_STEPS.
Fix: remove the binding — `@ignore` makes the scenario unreachable by the runner.

**C3. Missing step binding for Automatable scenario (if CHECK_FEATURE = yes)**
A step text in AUTOMATABLE_STEPS with no matching binding in the step file.
Fix: add the missing binding using the correct framework syntax.

**C4. Missing framework binding decorator or attribute**
A method body that looks like a step (takes a step-like string, has test-like content) but lacks
the required `@Given/@When/@Then` / `[Given/When/Then]` / `Given()/When()/Then()` binding.
Show the correct syntax for the framework.

**C5. Direct driver / browser / HTTP client call in step method**
Step method directly uses the low-level test driver instead of delegating to an abstraction:

| Domain | Forbidden pattern examples |
|---|---|
| Web | `driver.findElement(...)`, `page.locator(...).click()` directly in step, `cy.get(...).click()` without page object |
| API | `requests.get("https://...")` inline, `axios.post(url)` inline, `given().get(url)` in step |
| Mobile | `driver.findElement(MobileBy.xpath(...))` in step, `driver.tap(...)` directly |
| Desktop | `driver.findElement(MobileBy.accessibilityId(...))` in step, `app.window().click()` directly |

Fix: delegate to the appropriate page object / screen object / API client / helper class.

**C6. SSL verification disabled**
`verify=False` (Python), `rejectUnauthorized: false` (JS/TS), `disableSSL()` or `setSSLVerification(false)` (Java/C#)
Flag as CRITICAL unless PROJECT_RULES explicitly permits it (e.g. internal-only test environment documented in CLAUDE.md).

**C7. No status code assertion before body assertion (API domain only)**
Step asserts response body/field without first asserting the HTTP status code.
Fix: add status code assertion as the first assertion in the step.

---

### HIGH — code quality violation

**H1. Hardcoded credentials**
Any string literal matching a username, password, API key, Bearer token, Basic auth header.
Cross-reference against PROJECT_RULES constants. Also pattern-match:
- Patterns like `Bearer eyJ...`, `Basic dXNl...`, `password`, `secret`, `apiKey`, `token`
Fix: use environment variable (`process.env.X`, `os.environ['X']`, `Environment.GetEnvironmentVariable("X")`) or config file constant.

**H2. Hardcoded base URL or full URL**
Any `http://` or `https://` string literal in a step method body.
Fix: move to environment config (`BASE_URL`, `API_BASE_URL`, etc.) or project constant.

**H3. Inline selector / locator / endpoint path**

| Domain | Forbidden | Fix |
|---|---|---|
| Web | Inline XPath/CSS in step — see Step 3 table | Move to locators file, reference by constant |
| API | Raw endpoint path string e.g. `"/api/v1/users"` inline | Move to endpoints constants file |
| Mobile | XPath inline when accessibility ID is available | Use accessibility ID from locators file |
| Desktop | XPath inline when Automation ID is available | Use Automation ID from locators file |

**H4. Inline request payload (API domain only)**
Request body defined as an inline object/dict/map in the step method.
Fix: move to a fixtures file or test data builder; reference by fixture name.

**H5. Missing async/await (JS/TS frameworks)**
An `async function` step callback with no `await` inside it.
An `await` expression inside a non-`async` callback.
Fix: add missing `async` or `await` as appropriate.

**H6. Wrong assertion style for framework**

| Framework | Wrong | Right |
|---|---|---|
| Java (JUnit) | `result == expected` | `assertEquals(expected, result)` |
| Java (AssertJ) | `assertTrue(x.equals(y))` | `assertThat(x).isEqualTo(y)` |
| Python | `if result == expected:` without assert | `assert result == expected` |
| Playwright (JS/TS) | `result === expected` | `expect(result).toBe(expected)` |
| Cypress | `result === expected` | `expect(result).to.equal(expected)` |
| Ruby/RSpec | `result == expected` | `expect(result).to eq(expected)` |
| C#/NUnit | `Assert.IsTrue(x == y)` | `Assert.AreEqual(expected, x)` |
| API — missing status check | Body asserted first | Status code asserted first |

**H7. Hardcoded device / platform identifier (Mobile/Desktop domains)**
Device name, UDID, platform version, app path, bundle ID, package name as string literal in step.
Fix: move to capabilities config file; reference by constant.

**H8. Hardcoded application entity / record IDs**
Domain-specific IDs (election IDs, transaction IDs, user IDs, order numbers) as string literals.
Check against PROJECT_RULES for known formats. Fix: use test data constants or fixtures.

**H9. Platform-specific code in shared step file (Mobile domain)**
iOS-only or Android-only conditional logic inside a step file meant for both platforms.
Fix: move platform-specific logic into a platform helper; call the helper from the shared step.

---

### MEDIUM — convention violation

**M1. Locator / endpoint not defined in abstraction file**
A string key referenced in the step that is not present in DEFINED_LOCATORS or DEFINED_ENDPOINTS.
Fix: add the missing entry to the appropriate file.

**M2. Locator / endpoint defined but still has TODO / placeholder value**
Key exists in the locators/endpoints file but its value is a placeholder.
Fix: fill in the real selector / URL path.

**M3. Step file handles multiple unrelated features**
Step methods span more than one distinct feature area.
Fix: split into one step file per feature.

**M4. Scenario Outline parameter not captured correctly**

| Framework | Wrong | Right |
|---|---|---|
| Java/Cucumber | `@Given("I login as {0}")` | `@Given("I login as {string}")` + `String role` param |
| Python/Behave | `@given('I login as role')` | `@given('I login as "{role}"')` + `role` param |
| JS/TS/Cucumber | `Given('I login as role', ...)` | `Given('I login as {string}', async (role) => ...)` |
| Ruby/Cucumber | `Given('I login as role')` | `Given('I login as {string}') do \|role\|` |
| C#/SpecFlow | `[Given(@"I login as role")]` | `[Given(@"I login as ""(.*)""")]` + `string role` |
| Detox / XCUITest | N/A | N/A — no Cucumber parameters |

**M5. Wrong abstraction class for the action**
Step calls a page object / screen object / service whose responsibility doesn't match the action.
Check against PROJECT_RULES responsibility map.
Fix: use the correct abstraction class.

**M6. XPath used when more stable locator type is available (Mobile/Desktop domains)**
`By.xpath(...)` or `AppiumBy.xpath(...)` used when an accessibility ID or automation ID is available.
Flag line + accessibility ID alternative if visible from DEFINED_LOCATORS.

---

### LOW — style / hygiene

**L1. Empty step method body**
Completely empty body `{}` or `pass` or `nil` with no TODO comment.
Fix: add `// TODO: implement` (JS/TS/Java/C#) / `pass  # TODO: implement` (Python) / `# TODO: implement` (Ruby).

**L2. Empty catch / rescue / except block**
`catch {}`, `rescue nil`, `except: pass` with no logging or rethrow.
Fix: at minimum log the error; rethrow if not handled.

**L3. Commented-out code blocks**
Large sections of `//` or `#` commented code.
Flag for removal.

**L4. TODO comment inside an otherwise implemented method**
`// TODO` / `# TODO` inside a method that has other implementation code.
Flag the line — either complete or remove.

**L5. Inconsistent parameter naming across similar steps**
Capture parameters named differently for the same concept (e.g. `role`, `userRole`, `user`).
Flag for consistency.

**L6. Console/print logging of sensitive data**
`console.log(token)`, `print(password)`, `System.out.println(apiKey)` in step bodies.
Flag as LOW (escalate to HIGH if PROJECT_RULES treat it as a security violation).

### PROJECT_RULES checks
After all universal / domain / framework checks above, apply every rule from CLAUDE.md (Step 2) not already covered.
Label these `[PROJECT]` with severity matching the nature of the rule (CRITICAL / HIGH / MEDIUM / LOW).

---

## Step 8 — Output the audit report

```
╔══════════════════════════════════════════════════════════════════╗
║  AUDIT REPORT                                                    ║
║  File      : [STEPS_FILE]                                        ║
║  Domain    : [DOMAIN]                                            ║
║  Framework : [FRAMEWORK]                                         ║
║  CLAUDE.md : [found at <path> — N custom rules loaded]           ║
║            : [not found — universal rules only]                  ║
╚══════════════════════════════════════════════════════════════════╝

CRITICAL  ████  N issues
HIGH      ███   N issues
MEDIUM    ██    N issues
LOW       █     N issues
──────────────────────────────────────────────────────────────────

── CRITICAL ───────────────────────────────────────────────────────

  [C1] Line 42  page.waitForTimeout(3000)
       Domain  : Web / Playwright
       Rule    : Hard wait forbidden — use expect/waitFor
       Fix     : await expect(page.locator(selectors.myBtn)).toBeVisible();

  [C7] Line 88  expect(response.body.userId).toBe(123)
       Domain  : API
       Rule    : Response body asserted before status code check
       Fix     : expect(response.status).toBe(200);
                 expect(response.body.userId).toBe(123);

── HIGH ───────────────────────────────────────────────────────────

  [H1] Line 31  "Bearer eyJhbGciOiJSUzI1NiJ9..."
       Rule    : Hardcoded auth token in step
       Fix     : process.env.API_TOKEN  (or config equivalent)

  [H3] Line 65  page.locator("//div[@class='bny-grid']//button[1]")
       Domain  : Web / Playwright
       Rule    : Inline XPath — move to selectors file
       Fix     : page.locator(selectors.bnyApproveButton)

  [H7] Line 77  capabilities.setCapability("deviceName", "iPhone 14")
       Domain  : Mobile / Appium
       Rule    : Hardcoded device name — move to capabilities config
       Fix     : capabilities.setCapability("deviceName", Config.DEVICE_NAME)

── MEDIUM ─────────────────────────────────────────────────────────

  [M1] Line —   Selector key "bnyExportBtn" not in locators file
       Rule    : All locator references must be defined in the locators file
       Fix     : Add "bnyExportBtn" to selectors.js / locators file

  [M6] Line 102 AppiumBy.xpath("//XCUIElementTypeButton[@name='Submit']")
       Domain  : Mobile / Appium
       Rule    : XPath used — accessibility ID available
       Fix     : AppiumBy.accessibilityId("Submit")

── LOW ────────────────────────────────────────────────────────────

  [L1] Line 99  When('user taps submit', async function () { })
       Rule    : Empty step body
       Fix     : // TODO: implement

──────────────────────────────────────────────────────────────────
CUSTOM PROJECT RULES (from CLAUDE.md)

  [PROJECT-HIGH] Line 55  ...
       Rule    : <rule extracted from CLAUDE.md>
       Fix     : <recommended fix>

──────────────────────────────────────────────────────────────────
SUMMARY
  Total violations : N
  Critical         : N  ← fix before committing
  High             : N  ← fix before merging
  Medium           : N  ← fix in next review cycle
  Low              : N  ← fix when touching the file

  Feature binding gaps (if checked):
    Missing bindings : N steps in @Automatable scenarios have no binding
    Orphan bindings  : N bindings in step file whose step no longer exists in feature file
```

After the report, ask:
```
Would you like me to auto-fix any of these?
  A — Fix all CRITICAL issues
  B — Fix all CRITICAL + HIGH issues
  C — Fix specific issues (provide rule codes e.g. C1, H3, M6)
  N — No fixes, report only
```

If A, B, or C:
- Apply fixes using the Edit tool
- Follow the domain + framework syntax from Step 3 and all rules from PROJECT_RULES
- For issues that cannot be auto-fixed (e.g. missing locator values, missing fixtures), add a clear TODO comment at the affected line with a description of what needs to be done manually
- After all edits are done, report:
  - What was fixed automatically
  - What still requires manual attention and why
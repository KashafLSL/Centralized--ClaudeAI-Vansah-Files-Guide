Edit, update, or enhance an existing step definition file — works with ANY framework/language.
Handles: filling in TODO implementations, adding missing steps, fixing broken methods,
refactoring patterns, renaming locators, syncing with an updated feature file, or any other user-specified changes.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Step definition file path   (e.g. step_definitions/bny/BNYCommentsVisibilitySteps.js)
2. Framework / Language        (e.g. Java/Cucumber, Python/Behave, JS/Playwright, Ruby/Cucumber, C#/SpecFlow)
3. What changes do you want to make?
   Examples:
     - "Add step definitions for the new scenarios I added to the feature file"
     - "Fill in the TODO implementations using Playwright page objects"
     - "The login step uses the wrong credentials — fix it"
     - "Replace all hard-coded waits with proper async waits"
     - "Add the missing step: 'Then the export file is downloaded'"
     - "Rename locator approveBtn to approveElectionBtn everywhere"
     - "Sync with the updated feature file at features/BNY/BNYComments.feature"
```

Wait for the user's answers. Store as:
- STEPS_FILE = answer to 1
- FRAMEWORK  = answer to 2
- CHANGES    = answer to 3

---

## Step 2 — Read the current step definition file

Read the file at [STEPS_FILE] and understand:
- The framework bindings already in place (Given/When/Then annotations/decorators)
- The implementation status of each method (TODO vs implemented)
- Which locators / selectors are referenced
- Any imports, helpers, or page objects already in use

---

## Step 3 — Read the corresponding feature file (if needed)

If the user's request involves adding steps for new scenarios or syncing:
- Ask: "Which feature file should I sync with? (provide path)"
- OR infer from the file name / context if clear
- Read the feature file and identify all @Automatable scenario steps that have no matching binding

---

## Step 4 — Analyse the requested changes

Determine what needs to change:

**Adding missing step bindings:**
- Identify @Automatable steps with no matching binding
- Generate implementations (or TODO stubs if implementation details are unknown)
- Do NOT generate bindings for @NonAutomatable scenario steps

**Filling in TODO implementations:**
- Look at each method marked `// TODO`
- If the user provided guidance or page object names, use them
- Otherwise write a clean implementation stub that matches the framework pattern
- Add comments explaining what each implementation should do

**Fixing an implementation:**
- Locate the exact method(s) affected
- Apply the fix — change only what was asked, leave everything else unchanged

**Renaming a locator / selector:**
- Find every occurrence of the old name in the file
- Replace all occurrences consistently
- Remind the user to update the locators file / page object with the new name

**Syncing with an updated feature file:**
- Compare bindings in step file vs steps in @Automatable scenarios in the feature file
- Add bindings for any steps that are in the feature file but missing from the step file
- Flag (but do not remove) any bindings in the step file whose step text no longer exists in the feature file

**Any other change:**
- Apply precisely what was asked — do not change unrelated code

---

## Step 5 — Apply the changes

Edit the step definition file. Follow these rules for all frameworks:

### General rules
- Each Gherkin step maps to exactly one method / function
- Step text must match the feature file exactly
- No hardcoded credentials — use environment variables or a config/fixture file
- No hardcoded selectors / XPath inline in step definitions — reference a locators file or page object
- No empty method bodies — use `// TODO: implement` if implementation is not known
- No step definitions for @NonAutomatable scenario steps

### Framework-specific patterns

**Java / Cucumber — assertion:**
```java
@Then("the element is visible")
public void theElementIsVisible() {
    assertTrue(driver.findElement(By.xpath(Locators.ELEMENT)).isDisplayed());
}
```

**Python / Behave — assertion:**
```python
@then('the element is visible')
def step_element_visible(context):
    assert context.browser.find_element(*Locators.ELEMENT).is_displayed()
```

**JavaScript / Playwright — assertion:**
```javascript
Then('the element is visible', async function () {
    await expect(this.page.locator(selectors.element)).toBeVisible();
});
```

**TypeScript / Playwright — assertion:**
```typescript
Then('the element is visible', async function () {
    await expect(this.page.locator(selectors.element)).toBeVisible();
});
```

**Ruby / Cucumber — assertion:**
```ruby
Then('the element is visible') do
  expect(page).to have_selector(Selectors::ELEMENT)
end
```

**C# / SpecFlow (non-vFlux) — assertion:**
```csharp
[Then(@"the element is visible")]
public void ThenTheElementIsVisible()
{
    Assert.IsTrue(driver.FindElement(By.XPath(Locators.Element)).Displayed);
}
```

### Scenario Outline parameter capture (all frameworks must handle this)
When a step has a parameter placeholder from a Scenario Outline (e.g. `<Role>`, `<Amount>`),
use the correct capture syntax for the framework:
- Java: `{string}` or `{int}` annotations with typed parameters
- Python: `"{role}"` or `{role:d}` in step decorator
- JS/TS: `{string}` or `{int}` with function parameter
- Ruby: `{string}` block parameter
- C#: `"(.*)"` regex capture with method parameter

---

## Step 6 — Final summary

Output:
```
  Step definitions updated : [STEPS_FILE]
  Framework                : [FRAMEWORK]
  Changes applied          : [brief description of what was changed]
  Steps added              : N  (if any)
  Steps modified           : N  (if any)
  Steps with TODO remaining: N  (implementations still needed)
  Locators renamed         : [list or "none"]

  Reminder: if locators were renamed, update the corresponding locators/page-object file too.
```
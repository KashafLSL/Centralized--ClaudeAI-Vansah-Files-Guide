Generate step definitions for an existing Gherkin feature file — works with ANY framework/language.
Only @Automatable scenarios get step definitions. @NonAutomatable scenarios (tagged @ignore)
are skipped entirely — no step definitions generated for them.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Feature file path          (e.g. features/BNY/BNYCommentsVisibility.feature)
2. Framework / Language       (e.g. Java/Cucumber, Python/Behave, JS/Playwright, Ruby/Cucumber, C#/SpecFlow)
3. Step definition file name  (e.g. BNYCommentsVisibilitySteps  ← no extension)
4. Step definition path       (e.g. src/test/java/steps  or  step_definitions/bny)
```

Wait for the user's answers. Store as:
- FEATURE_FILE = answer to 1
- FRAMEWORK    = answer to 2
- STEPS_NAME   = answer to 3
- STEPS_PATH   = answer to 4

---

## Step 2 — Read the feature file

Read the file at [FEATURE_FILE] and extract:
- All @Automatable scenarios and every Given/When/Then/And/But step within them
- All @NonAutomatable scenarios — note their step text but do NOT generate definitions for them
- Any Scenario Outline step patterns with parameter placeholders (e.g. "<Role>", "<Amount>")
- The story ID from the Feature line or tags (e.g. @VFL-8283)

---

## Step 3 — Check for an existing step definition file

Before generating, check whether a file already exists at [STEPS_PATH]/[STEPS_NAME].[ext].

If it exists:
- Read it
- Identify which steps already have bindings
- Only generate bindings for steps that do NOT yet have a matching definition
- Append new methods to the existing file — do not overwrite existing ones

If it does not exist:
- Generate the full file from scratch

---

## Step 4 — Generate step definitions

Create or update: [STEPS_PATH]/[STEPS_NAME].[ext]

Generate step definitions ONLY for @Automatable scenarios, in the language/framework specified by FRAMEWORK.
Do NOT generate any step definition methods for steps belonging exclusively to @NonAutomatable scenarios.

### Java / Cucumber
```java
import io.cucumber.java.en.*;
import static org.junit.Assert.*;

public class [STEPS_NAME] {

    @Given("...")
    public void given() {
        // TODO: implement
    }

    @When("...")
    public void when() {
        // TODO: implement
    }

    @Then("...")
    public void then() {
        // TODO: implement
    }
}
```

### Python / Behave
```python
from behave import given, when, then

@given('...')
def step_given(context):
    pass  # TODO: implement

@when('...')
def step_when(context):
    pass  # TODO: implement

@then('...')
def step_then(context):
    pass  # TODO: implement
```

### JavaScript / Playwright + Cucumber
```javascript
const { Given, When, Then } = require('@cucumber/cucumber');

Given('...', async function () {
    // TODO: implement
});

When('...', async function () {
    // TODO: implement
});

Then('...', async function () {
    // TODO: implement
});
```

### TypeScript / Playwright + Cucumber
```typescript
import { Given, When, Then } from '@cucumber/cucumber';

Given('...', async function () {
    // TODO: implement
});

When('...', async function () {
    // TODO: implement
});

Then('...', async function () {
    // TODO: implement
});
```

### Ruby / Cucumber
```ruby
Given('...') do
  # TODO: implement
end

When('...') do
  # TODO: implement
end

Then('...') do
  # TODO: implement
end
```

### C# / SpecFlow (non-vFlux)
```csharp
using TechTalk.SpecFlow;
using NUnit.Framework;

namespace [Project].StepDefinitions
{
    [Binding]
    public class [STEPS_NAME]
    {
        [Given(@"...")]
        public void Given()
        {
            // TODO: implement
        }

        [When(@"...")]
        public void When()
        {
            // TODO: implement
        }

        [Then(@"...")]
        public void Then()
        {
            // TODO: implement
        }
    }
}
```

### Rules for all frameworks
- Each Gherkin step maps to exactly one method / function
- Step text must match the feature file exactly (including parameter placeholders)
- Add `// TODO: implement` comments in every method body — do not write empty methods
- For Scenario Outline parameters (e.g. `<Role>`), use the correct capture group syntax for the framework:
  - Java: `@Given("I am logged in as {string}")` with `String role` parameter
  - Python: `@given('I am logged in as "{role}"')` with `role` parameter
  - JS/TS: `Given('I am logged in as {string}', async function (role) {...})`
  - Ruby: `Given('I am logged in as {string}') do |role|`
  - C#: `[Given(@"I am logged in as ""(.*)""")]` with `string role` parameter
- No hardcoded credentials — reference environment variables or a config file
- No hardcoded selectors / XPath — use a locators file or page object pattern

---

## Step 5 — Final summary

Output:
```
  Step definitions created : [STEPS_PATH]/[STEPS_NAME].[ext]
  Framework                : [FRAMEWORK]
  Steps generated          : N  (for @Automatable scenarios only)
  Steps skipped            : N  (@NonAutomatable — @ignore tag, no binding needed)
  Build / lint check       : [result or "skipped — run manually"]

  Next steps:
    Fill in TODO implementations in [STEPS_NAME].[ext]
    Import to Vansah : /non-dotnet:ext-vansah-import  (after updating vansah-config.json)
```
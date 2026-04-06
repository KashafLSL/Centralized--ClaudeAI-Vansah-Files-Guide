Edit, update, or enhance an existing Gherkin feature file.
Handles: adding new scenarios, modifying existing steps, changing tags, restructuring sections,
adding scenarios from a new Jira story, or any other user-specified changes.

---

## Step 1 — Collect inputs from the user

Ask the user ALL of the following questions together in a single message (do not ask one at a time):

```
Please provide the following details:

1. Feature file path   (e.g. FeatureFiles/AITestingBDD/BNYCommentsVisibility.feature)
2. What changes do you want to make?
   Examples:
     - "Add scenarios for VFL-9999"
     - "Add a negative scenario for empty BNY comment"
     - "Change the login step in all scenarios to use BNY Approver instead of Main Approver"
     - "Add @Regression tag to all scenarios"
     - "Rewrite scenario 3 — the steps are wrong"
     - "Remove scenarios tagged @ignore"
```

Wait for the user's answers. Store as:
- FEATURE_FILE = answer to 1 (full path including filename)
- CHANGES      = answer to 2

---

## Step 2 — Read the current feature file

Read the file at vFluxAutomation/[FEATURE_FILE] and understand:
- The Feature title and story reference
- All existing scenarios, their tags, preconditions, and steps
- The current section structure
- Which scenarios are @Automatable vs @NonAutomatable

---

## Step 3 — Analyse the requested changes

Determine the type of change(s) requested:

**Adding new scenarios from a Jira story:**
- Fetch the Jira issue and extract ACs, roles, business rules
- Generate new scenarios following the full coverage matrix
- Present new scenarios as a numbered list and ask for A/N labeling before writing
- PAUSE and wait for labeling response

**Adding specific scenarios (user-described):**
- Draft the new scenario(s) in Gherkin
- Ask the user: "Is this scenario Automatable (A) or Non-Automatable (N)?"
- PAUSE and wait for response before writing

**Modifying existing scenarios / steps / tags:**
- Identify all affected scenarios
- Apply the change consistently across all matching scenarios
- Do not change unaffected scenarios

**Restructuring / reordering:**
- Maintain all existing scenario content
- Only change the structure/order as requested

---

## Step 4 — Apply the changes

Edit the feature file applying all requested changes. Follow ALL rules:

### Tag rules
Automatable scenarios:
```gherkin
@smokeBDD @Smoke @Regression @[STORY_ID] @Automatable
Scenario: <title>
```

Non-Automatable scenarios:
```gherkin
@ignore @Manual @Regression @[STORY_ID] @NonAutomatable
Scenario: <title>
```

### Other rules
- Every scenario (new or existing) must have a `# Precondition:` comment directly above it
- Scenario titles are business-readable and role-explicit
- Use Scenario Outline + Examples (min 3 rows) for data-driven cases
- Maintain the existing section comment structure; add new sections only if needed
- Never remove existing scenarios unless the user explicitly asked to remove them
- Never change tags on existing scenarios unless explicitly asked

### Gherkin discipline
- Given = state / precondition
- When  = user action
- Then  = assertion / outcome
- And   = continuation of previous keyword type
- Never jump from Given to Then without a When

---

## Step 5 — Final summary

Output:
```
  Feature file updated : vFluxAutomation/[FEATURE_FILE]
  Changes applied      : [brief description of what was changed]
  Scenarios added      : N  (if any)
  Scenarios modified   : N  (if any)
  Scenarios removed    : N  (if any)
  Total scenarios now  : N

  Next steps (if new scenarios were added):
    Generate step definitions : /dotnet:generate-steps
    Import to Vansah          : /dotnet:vansah-import  (after updating VansahConfig.json)
```
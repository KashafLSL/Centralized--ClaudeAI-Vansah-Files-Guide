# Vansah REST API — BDD Import Flow

This document explains the 4-step API sequence used by `VansahImporter.cs` to create
BDD test cases in Vansah from a Gherkin `.feature` file.

---

## Overview

Each Scenario in the feature file goes through 4 sequential API calls.
**The order is mandatory** — skipping or reordering steps will produce wrong results.

```
Gherkin Scenario
      │
      ▼
Step 1 ── POST /api/v1/testCase ──────────────── Create test case shell
      │         returns: testCaseIdentifier (UUID)
      ▼
Step 2 ── PUT  /api/v1/testCase/{id} ─────────── Set script type = "bdd"
      │         ← MUST happen before Step 3
      ▼
Step 3 ── POST /api/v1/testCase/{id}/testScript ─ Create the BDD testScript record
      │         returns: scriptId (UUID)
      ▼
Step 4 ── PUT  /api/v1/testCase/testScript/{id} ─ Write Given/When/Then steps
```

---

## Step 1 — Create Test Case

**Method:** `POST`
**URL:** `{VansahApiUrl}/api/v1/testCase`

**Request body:**
```json
{
  "headline": "Approver sees two comment sections when opening a CA event popup",
  "precondition": "User is logged in as Approver and BNY Active grid is visible",
  "project": { "key": "VFL" },
  "folder": [{ "identifier": "deaba1dd-1146-11f1-8e71-4e332803d881" }],
  "type": { "identifier": "b3194fef-1e56-11ef-ab0d-a6c8f8be5ecf" }
}
```

**Response (success):**
```json
{
  "data": {
    "key": "VFL-C8228",
    "identifier": "48d03722-22db-11f1-bef8-2a12a96c28b0"
  }
}
```

Save both `key` (e.g. `VFL-C8228`) and `identifier` (UUID) for the next steps.

---

## Step 2 — Set Script Type to BDD

**Method:** `PUT`
**URL:** `{VansahApiUrl}/api/v1/testCase/{testCaseIdentifier}`

**Request body:**
```json
{
  "scriptType": "bdd",
  "caseVersion": 1
}
```

**Why this must come before Step 3:**
If you create the testScript (Step 3) before setting BDD mode, Vansah creates it as
a multi-step (traditional) script instead of BDD format. The steps will not render
as Given/When/Then in the UI.

No meaningful response body — check HTTP 200 for success.

---

## Step 3 — Create BDD TestScript Record

**Method:** `POST`
**URL:** `{VansahApiUrl}/api/v1/testCase/{testCaseIdentifier}/testScript`

Note: `testCaseIdentifier` is the UUID from Step 1 response — it goes in the **URL path**, not the body.

**Request body:**
```json
{
  "scriptType": "bdd",
  "project": { "key": "VFL" },
  "testCaseVersion": 1
}
```

**Response (success):**
```json
{
  "data": {
    "identifier": "5260a991-22db-11f1-bef8-2a12a96c28b0"
  }
}
```

Save `identifier` as `scriptId` for Step 4.

**Common pitfall:** if `bdd: []` is returned from a GET on testScripts, it means BDD mode
is set but no testScript record exists yet — run Step 3 to create it.

---

## Step 4 — Write Given/When/Then Steps

**Method:** `PUT`
**URL:** `{VansahApiUrl}/api/v1/testCase/testScript/{scriptId}`

Note: this uses `scriptId` from Step 3 — NOT `testCaseIdentifier`.

**Request body:**
```json
{
  "scriptType": "bdd",
  "project": { "key": "VFL" },
  "testCaseVersion": 1,
  "bddData": "    Given I am logged in as an Approver user\n    And the BNY Active grid is visible\n    When I open a CA event popup from the grid\n    Then I should see the Approver comments section in the popup\n    And I should see the BNY comments section in the popup"
}
```

The `bddData` field is the raw Gherkin steps text (no `Scenario:` header, steps indented with 4 spaces).
For Scenario Outlines, append the `Examples:` table after the steps.

---

## Header required on all requests

```
Authorization: <VansahToken>
Content-Type: application/json
```

---

## Error reference

| HTTP code | Meaning | Fix |
|-----------|---------|-----|
| 401 | Invalid or expired token | Regenerate token in Jira → Vansah → API Tokens |
| 404 | Wrong URL or identifier | Check testCaseIdentifier / scriptId are UUIDs, not keys like VFL-C8228 |
| 400 | Bad request body | Check JSON structure matches examples above |
| 500 | Server error | Retry; check FolderIdentifier and ProjectKey are correct |

---

## Identifier vs Key

| Field | Example | Used for |
|-------|---------|---------|
| `key` | `VFL-C8228` | Display / reference only |
| `identifier` (testCase) | `48d03722-...` | URL parameter in Steps 2 & 3 |
| `identifier` (testScript) | `5260a991-...` | URL parameter in Step 4 |

Always use the UUID `identifier` in API URLs — never the human-readable `key`.
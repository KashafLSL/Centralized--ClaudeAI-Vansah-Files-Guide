# Vansah BDD Toolkit

A plug-and-play toolkit for generating BDD scenarios from Jira stories and importing them into [Vansah Test Management](https://vansah.com) — powered by **Claude Code** + **Atlassian MCP**.

---

## What's inside

```
vansah-bdd-toolkit/
├── prompts/                  Claude slash commands (copy to your project's .claude/commands/)
│   ├── generate-bdd.md       Full BDD generation pipeline — max coverage, interactive
│   ├── generate-bdd-import.md  Same as above but auto-imports after generation
│   └── vansah-import.md      Import only (for an already-generated feature file)
│
├── src/                      C# Vansah utility classes (copy to your project's Tools/ folder)
│   ├── VansahConfig.cs       Data model — maps to VansahConfig.json
│   ├── VansahImporter.cs     Gherkin parser + 4-step REST API importer
│   └── VansahImportRunner.cs NUnit test fixture that drives the import
│
├── config/
│   └── VansahConfig.json     Template — fill in your credentials and paths
│
└── docs/
    ├── MCP-SETUP-GUIDE.md    How to set up the Atlassian MCP for your team
    └── VANSAH-API-FLOW.md    How the 4-step Vansah REST API flow works
```

---

## Quick start

### 1. Copy source files into your project

```
src/VansahConfig.cs          →  <YourProject>/Tools/VansahConfig.cs
src/VansahImporter.cs        →  <YourProject>/Tools/VansahImporter.cs
src/VansahImportRunner.cs    →  <YourProject>/Tools/VansahImportRunner.cs
config/VansahConfig.json     →  <YourProject>/VansahConfig.json
```

### 2. Fill in VansahConfig.json

```json
{
  "VansahApiUrl":    "https://prodde.vansah.com",
  "VansahToken":     "YOUR_VANSAH_TOKEN",
  "ProjectKey":      "VFL",
  "FolderIdentifier":"",
  "FeatureFilePath": "FeatureFiles/AITestingBDD",
  "FeatureFileName": "",
  "TypeIdentifier":  "YOUR_TYPE_UUID"
}
```

> `FolderIdentifier` and `FeatureFileName` are filled in by Claude automatically during generation.

### 3. Add NuGet dependency

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.*" />
```

### 4. Copy prompts to your Claude project

```
prompts/generate-bdd.md         →  .claude/commands/generate-bdd.md
prompts/generate-bdd-import.md  →  .claude/commands/generate-bdd-import.md
prompts/vansah-import.md        →  .claude/commands/vansah-import.md
```

### 5. Set up the Atlassian MCP

Follow [docs/MCP-SETUP-GUIDE.md](docs/MCP-SETUP-GUIDE.md) to connect Claude Code to your Jira instance.

### 6. Run

```bash
# In Claude Code:
/generate-bdd

# Or import directly:
dotnet test <YourProject>.csproj --filter "Category=VansahImport" --logger "console;verbosity=detailed"
```

---

## How /generate-bdd works

```
1.  Asks for → Jira story, feature file name/path, step definition name/path
2.  Fetches the Jira story via Atlassian MCP
3.  Analyses roles, ACs, business rules, states, data dimensions
4.  Generates .feature file with FULL coverage:
      ✔ Happy path          ✔ Role-based access
      ✔ Positive cases      ✔ Negative / validation
      ✔ Business rules      ✔ Edge cases
      ✔ Workflow states     ✔ E2E lifecycle
      ✔ Scenario Outlines   ✔ Boundary values
5.  Generates C# step definitions (SpecFlow / NUnit)
6.  Adds placeholder keys to Demo.json
7.  Asks for → FolderIdentifier (Vansah folder UUID)
8.  Updates VansahConfig.json
9.  Build-checks the project
10. Tells you to run the import command
```

---

## Vansah API — 4-step import flow

| Step | Method | Endpoint | Purpose |
|------|--------|----------|---------|
| 1 | POST | `/api/v1/testCase` | Create test case |
| 2 | PUT  | `/api/v1/testCase/{id}` | Set script type to BDD |
| 3 | POST | `/api/v1/testCase/{id}/testScript` | Create BDD testScript record |
| 4 | PUT  | `/api/v1/testCase/testScript/{scriptId}` | Write Given/When/Then steps |

> Steps **must run in this exact order**. Setting BDD mode (Step 2) must happen before creating the testScript (Step 3), otherwise steps are created as multi-step format instead of BDD.

See [docs/VANSAH-API-FLOW.md](docs/VANSAH-API-FLOW.md) for full details.

---

## Requirements

| Dependency | Version |
|------------|---------|
| .NET Core | 3.1+ |
| NUnit | 3.x |
| SpecFlow | 3.9+ |
| Newtonsoft.Json | 13.x |
| Claude Code CLI | latest |
| Node.js (for MCP) | 18+ |

---

## Security

- **Never commit** `VansahConfig.json` with real tokens — add it to `.gitignore`
- **Never commit** `~/.claude/settings.json` — it contains your personal Atlassian API token
- Each team member generates their own Atlassian API token (see `docs/MCP-SETUP-GUIDE.md`)
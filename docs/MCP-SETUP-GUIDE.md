# Atlassian MCP Integration — Team Setup Guide

This guide enables every team member to use **Claude Code** with live Jira access
(read stories, generate BDD, auto-import to Vansah) on this project.

---

## What is the MCP?

The **Atlassian MCP** (`mcp-atlassian`) is a Model Context Protocol server that gives
Claude Code direct access to your Jira instance. Once configured, Claude can:

- Read any Jira story by key (e.g. `VFL-8283`)
- Extract acceptance criteria, roles, and business rules automatically
- Drive the full `/generate-bdd` pipeline without copy-pasting story content

---

## Prerequisites

| Requirement | Version |
|-------------|---------|
| Node.js | ≥ 18 |
| npm / npx | bundled with Node |
| Claude Code CLI | latest (`npm install -g @anthropic-ai/claude-code`) |
| Atlassian account | must have access to `logicielservices.atlassian.net` |

---

## Step 1 — Generate your personal Atlassian API Token

> Each person must create **their own token** — never share tokens.

1. Log in to [https://id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens)
2. Click **Create API token**
3. Label it: `Claude Code MCP`
4. Copy the token — you will not see it again

---

## Step 2 — Add the MCP server to your Claude settings

Open (or create) the file:

```
C:\Users\<your-windows-username>\.claude\settings.json
```

Add the following block (merge with any existing content — do **not** overwrite the whole file):

```json
{
  "mcpServers": {
    "jira": {
      "command": "npx",
      "args": ["-y", "mcp-atlassian"],
      "env": {
        "ATLASSIAN_BASE_URL": "https://logicielservices.atlassian.net",
        "ATLASSIAN_EMAIL": "your.email@logicielservice.com",
        "ATLASSIAN_API_TOKEN": "YOUR_PERSONAL_API_TOKEN_HERE"
      }
    }
  }
}
```

Replace:
- `your.email@logicielservice.com` → your Atlassian login email
- `YOUR_PERSONAL_API_TOKEN_HERE` → the token you copied in Step 1

---

## Step 3 — Approve the MCP in Claude Code

1. Open a terminal in the project root:
   ```
   cd E:\vFluxBDD\Automation-vFlux\vFluxAutomation
   ```

2. Start Claude Code:
   ```
   claude
   ```

3. On first run with the MCP configured, Claude Code will prompt:
   ```
   Allow MCP server "jira" to connect? [y/n]
   ```
   Type **y** and press Enter.

4. To verify the connection works, ask Claude:
   ```
   Read Jira story VFL-8283
   ```
   Claude should return the story title and description without you pasting anything.

---

## Step 4 — Verify the full pipeline works

Run a quick smoke test:

```
/generate-bdd VFL-8283 TestFeature BNY
```

Claude will:
1. Fetch the Jira story automatically
2. Ask you for the **FolderIdentifier** (Vansah folder UUID) — provide it
3. Generate `.feature` + step definitions
4. Update `VansahConfig.json`
5. Prompt you to run the import

---

## Available slash commands (once MCP is active)

| Command | What it does |
|---------|-------------|
| `/generate-bdd <VFL-XXXX> <FileName> <Area>` | Generate BDD + steps, then ask for FolderIdentifier before import |
| `/generate-bdd-max <VFL-XXXX> <FileName> <Area>` | Same but with maximum scenario coverage |
| `/generate-bdd-import <VFL-XXXX> <FileName> <Area>` | Generate + auto-import in one shot |
| `/vansah-import` | Import the feature file currently set in `VansahConfig.json` |

---

## FolderIdentifier — how to find it

The `FolderIdentifier` is the UUID of the target folder in Vansah where test cases will be created.

1. Open Vansah in your browser
2. Navigate to the folder you want to import into
3. The UUID appears in the URL or folder settings
4. Paste it when Claude asks:
   > "What is the FolderIdentifier (Vansah folder UUID) you want to import into?"

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `npx: command not found` | Install Node.js from https://nodejs.org (LTS) |
| `401 Unauthorized` from Jira | Re-generate your API token — tokens expire or get revoked |
| Claude doesn't prompt to approve MCP | Delete `C:\Users\<you>\.claude\cache` and restart Claude |
| `mcp-atlassian` not found | Run `npx -y mcp-atlassian` once manually to force download |
| Import fails with 0 scenarios | Check `VansahConfig.json` — `FeatureFileName` must match the actual file name exactly |

---

## Security rules

- **Never commit** `settings.json` to Git — it contains your personal API token
- **Never share** your API token with teammates — each person generates their own
- The `.claude/` directory in this repo contains only **commands and guides** — no credentials

---

## Project-specific config reference

| Setting | Value |
|---------|-------|
| Jira base URL | `https://logicielservices.atlassian.net` |
| Jira project key | `VFL` |
| Vansah API URL | `https://prodde.vansah.com` |
| Feature files path | `vFluxAutomation/FeatureFiles/AITestingBDD/` |
| VansahConfig.json | `vFluxAutomation/VansahConfig.json` |
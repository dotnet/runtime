# CI Pipeline Monitor

A Copilot CLI skill that automates monitoring CI stress/PGO test pipelines on
Azure DevOps, triaging failures, and coordinating with GitHub issue tracking.

## What It Does

- Monitors 20+ CI test pipelines (`dnceng-public/public`, `main` branch)
- Extracts every test failure via the AzDO Test Results API
- Downloads full Helix console logs for each failure
- Triages failures: classifies, groups by root cause, searches for matching GitHub issues
- Generates a formatted weekly report with action items
- Can bisect regressions and file new GitHub issues

## Prerequisites

1. **GitHub Copilot CLI** — `winget install GitHub.Copilot` (Windows) or
   `brew install copilot-cli` (macOS/Linux)
2. **Python 3.8+** with `requests`:
   ```bash
   pip install requests
   ```
3. **Azure CLI** — for AzDO Test Results API authentication:
   ```bash
   az login
   ```
4. **Node.js / npm** — for the ADO MCP server

## One-Time Setup

1. **Clone dotnet/runtime** (or use your existing clone):
   ```bash
   git clone https://github.com/dotnet/runtime.git
   cd runtime
   ```

2. **Configure the ADO MCP server** — create or edit `~/.copilot/mcp-config.json`:
   ```json
   {
     "ado": {
       "type": "stdio",
       "command": "npx",
       "args": ["-y", "@azure-devops/mcp", "dnceng-public", "-d", "core", "pipelines", "test-plans"]
     }
   }
   ```
   Or run `/mcp add` inside Copilot CLI to add it interactively.

3. **Launch Copilot CLI** from the runtime repo root:
   ```bash
   copilot
   ```

4. **Verify the skill is available:**
   ```
   /skills
   ```
   You should see `ci-pipeline-monitor` listed.

## Usage

### Invoke the Skill

In Copilot CLI, type:
```
/ci-pipeline-monitor
```

Or ask naturally — Copilot will detect and invoke the skill automatically:
- "Check the CI test pipelines"
- "Generate the weekly CI test report"

### What Happens

The skill runs the full pipeline end-to-end:
1. Fetches latest builds from all 20+ monitored pipelines
2. Extracts failed tests and downloads Helix console logs
3. Triages each failure (classifies, groups by root cause, searches GitHub)
4. Generates a formatted report with action items

### Running Specific Steps

You can also ask for individual steps:
- "Bisect the regression for test X in pipeline Y"
- "File GitHub issues for the new test failures from the last run"
- "Search GitHub for an existing issue matching this test failure: ..."

## Authentication

No manual token or credential setup is needed. The skill handles authentication
automatically:

- **AzDO Builds API + Helix API** — public, no auth required
- **AzDO Test Results API** — uses `az account get-access-token` (requires
  `az login` from prerequisites)
- **GitHub API** — uses your Copilot CLI login

## How It Works

The skill combines **Python scripts** (deterministic data collection) with
**LLM triage** (non-deterministic analysis):

1. `setup_and_fetch_builds.py` — creates SQLite DB, fetches latest build per pipeline
2. `extract_failed_tests.py` — extracts failed test methods via AzDO Test Results API
3. `fetch_helix_logs.py` — downloads full Helix console logs to disk
4. LLM reads logs, extracts errors verbatim, classifies, groups, searches GitHub issues
5. `generate_report.py` — generates the final report from the DB
6. `validate_results.py` — validates DB completeness before publishing

Generated output (logs, reports, DB) stays local — nothing is committed to the repo.

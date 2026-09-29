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

## One-Time Setup

1. **Clone dotnet/runtime** (or use your existing clone):
   ```bash
   git clone https://github.com/dotnet/runtime.git
   cd runtime
   ```

2. **Launch Copilot CLI** from the runtime repo root:
   ```bash
   copilot
   ```

3. **Verify the skill is available:**
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

## Authentication

No manual token or credential setup is needed. The skill handles authentication
automatically:

- **AzDO Builds API + Helix API** — public, no auth required
- **AzDO Test Results API** — uses `az account get-access-token` (requires
  `az login` from prerequisites)
- **GitHub API (triage)** — the agent uses GitHub MCP tools built into Copilot
  CLI, authenticated via your Copilot CLI login. No separate configuration needed.
- **GitHub API (validation)** — `validate_results.py` spot-checks NEW failures
  against the unauthenticated GitHub Search API (`api.github.com`). Rate-limited
  to 10 searches/minute, with automatic pauses between requests. No auth needed.

## How It Works

The skill combines **Python scripts** (deterministic data collection) with
**agent triage** (non-deterministic analysis):

| Step | What | Run By | APIs / Tools |
|------|------|--------|-------------|
| 1. Resolve Pipeline Definitions | Resolve missing def IDs, update `pipelines.md` | Agent | AzDO Definitions API (no auth) |
| 2. Fetch Latest Builds | Create DB, fetch latest build per pipeline | Script (`setup_and_fetch_builds.py`) | AzDO Builds API (no auth) |
| 3. Extract Failed Tests and Fetch Logs | Extract failed test methods, download Helix console logs | Script (`extract_failed_tests.py`, `fetch_helix_logs.py`) | AzDO Test Results API (Bearer token), Helix API (no auth) |
| 4. Triage Failures | Read logs, extract errors verbatim, classify, group, search GitHub | Agent | GitHub MCP (`search_issues`, `issue_read`) |
| 5. Validate DB | Validate DB completeness and accuracy | Script (`validate_results.py`) | GitHub Search API (unauthenticated spot-checks) |
| 6. Generate Report | Generate markdown report from DB | Script (`generate_report.py`) | None (reads DB only) |
| 7. Bisect Regressions | Identify regressing commit/PR (on request) | Agent | GitHub MCP (`list_commits`, `search_pull_requests`) |

Generated output (logs, reports, DB) stays local — nothing is committed to the repo.

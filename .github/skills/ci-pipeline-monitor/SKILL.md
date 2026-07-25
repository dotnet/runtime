---
name: ci-pipeline-monitor
description: >
  Monitors .NET runtime CI test pipelines on Azure DevOps. Use this skill when asked to
  monitor CI pipeline test results, triage CI test failures across ADO pipelines, or
  generate CI test monitoring reports.
---

# CI Pipeline Monitor

## Overview

This skill automates monitoring CI test pipelines on Azure DevOps
(dnceng-public/public), triaging failures, and coordinating with GitHub
dotnet/runtime issue tracking.

## Pipelines to Monitor

The list of pipelines and their cached definition IDs is maintained in
[`pipelines.md`](pipelines.md) in this skill directory.

## Architecture

**Deterministic steps are scripted. Agent does triage.**

- **Python scripts** (`scripts/`) handle all deterministic work: DB setup,
  build fetching, test failure extraction (including `errorMessage` and
  `stackTrace` from the ADO API), Helix log downloading, and report
  generation.
- **Agent** handles all non-deterministic work: reading console log files for
  failures where the API returned no useful error (crashes, timeouts),
  enriching/completing error messages and stack traces, classifying failures,
  grouping by root cause, searching GitHub for matching issues, writing
  analysis, and populating the triage tables in `monitor.db`.

## Scripts

Use these scripts — do NOT write ad-hoc replacements. Do NOT create new files
in `scripts/` — only the committed scripts below belong there. For ad-hoc
queries during triage (e.g., DB lookups, grouping), prefer `python -c '...'`
inline. If a query is too complex for inline (escaping issues, multi-line),
write a temp file under `temp/` (e.g., `temp/_query.py`). The `temp/`
directory is gitignored; the user will clean it up when no longer needed.

| Script | Step | What it does |
|--------|------|-------------|
| `setup_and_fetch_builds.py` | 2 | Creates `monitor.db` (including `test_results` table), fetches latest build for every pipeline, populates `pipelines` table. |
| `extract_failed_tests.py` | 3 | Reads failing pipelines from DB. Calls AzDO Test Results API for each failing build. INSERTs one row per failed test method into `test_results` (test_name, run_name, pipeline_name, Helix info, console URL, **error_message, stack_trace from API**). Strips `.WorkItemExecution` suffix. Skips generic "Helix Work Item failed" messages (stores empty — agent fills from console log). Requires `ADO_TOKEN` env var or `az cli`. |
| `fetch_helix_logs.py` | 3 | Fetches Helix console logs, saves full log files to `helix-logs/` directory, UPDATEs each `test_results` row with `exit_code` and `console_log_path`. No auth needed. |
| `validate_results.py` | 5 | Validates `monitor.db` completeness and integrity. 24 checks: data completeness, referential integrity, data quality, content accuracy, debug log completeness. Exits 1 on failure. |
| `generate_report.py` | 6 | Reads `monitor.db`, generates report to `logs/` directory. Pure formatting — no judgment. Run only after DB validation passes. |

**End-to-end pipeline:**
```bash
cd .github/skills/ci-pipeline-monitor

# Step 0: Prerequisites (agent)
pip install requests
# Obtain ADO_TOKEN — see Step 0 section below for full logic

# Step 1: Resolve pipeline definitions (agent)
# Agent compares Pipeline Details against Cached Mapping in pipelines.md,
# resolves missing def IDs via AzDO Definitions API, updates pipelines.md

# Step 2: Fetch latest builds (deterministic)
python scripts/setup_and_fetch_builds.py --pipelines pipelines.md --db scripts/monitor.db

# Step 3: Extract failed tests + fetch logs (deterministic)
python scripts/extract_failed_tests.py --db scripts/monitor.db
python scripts/fetch_helix_logs.py --db scripts/monitor.db

# Step 4: Triage (agent — non-deterministic)
# Agent reads test_results table, classifies (exit code + error message),
# groups by root cause, searches GitHub, populates failures table,
# UPDATEs test_results.failure_id for every row

# Step 5: Validate DB before report (deterministic)
python scripts/validate_results.py --db scripts/monitor.db --pipelines pipelines.md --log logs/ci-pipeline-monitor-*.log
# Step 5a: If validation fails, fix issues in DB, re-validate (up to 3 retries,
# only while failure count decreases). Log remaining WARNs and proceed.

# Step 6: Generate report (deterministic — only after DB is clean)
# Pass --validation-warnings if Step 5a had unresolved failures
python scripts/generate_report.py --db scripts/monitor.db [--validation-warnings]
```

## Database Schema

Created by `setup_and_fetch_builds.py`. Populated by scripts (Steps 2-3) and
agent (Step 4). Validated by `validate_results.py` (Step 5). Read by
`generate_report.py` (Step 6).

```sql
CREATE TABLE pipelines (
    name            TEXT PRIMARY KEY,
    build_id        INTEGER,
    build_number    TEXT,
    result          TEXT NOT NULL,  -- succeeded | failed | inconclusive | skipped
    skip_reason     TEXT
);

-- Every individual test failure from Step 3 (before grouping).
-- One row per failed test method per pipeline. Populated by scripts.
CREATE TABLE test_results (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    pipeline_name     TEXT NOT NULL,
    build_id          INTEGER NOT NULL,
    run_name          TEXT NOT NULL,        -- AzDO test run name (leg name)
    test_name         TEXT NOT NULL,        -- fully qualified, .WorkItemExecution stripped
    helix_job_id      TEXT,
    helix_work_item   TEXT,
    console_log_url   TEXT,
    exit_code         INTEGER,              -- from console log (script-extracted)
    console_log_path  TEXT,                 -- path to full console log file on disk (in helix-logs/)
    error_message     TEXT,                 -- initially from AzDO Test Results API; may be enriched/overwritten by agent with console-log snippet
    stack_trace       TEXT,                 -- initially from AzDO Test Results API; may be enriched/overwritten by agent with console-log snippet
    failure_id        INTEGER,              -- NULL until Step 4 assigns a group
    FOREIGN KEY (failure_id) REFERENCES failures(id)
);

CREATE TABLE failures (
    id                    INTEGER PRIMARY KEY,
    title                 TEXT NOT NULL,
    scope                 TEXT,                -- e.g. "arm64, x86"
    test_name             TEXT NOT NULL,
    work_item             TEXT,
    failure_category      TEXT,                -- timeout, crash_sigsegv, test_failure, etc.
    exit_codes            TEXT,
    failing_since_date    TEXT,
    failing_since_build   TEXT,
    console_log_url       TEXT,
    source_test_result_id INTEGER,             -- which test_results row the error_message/stack_trace came from
    error_message         TEXT,                -- verbatim from log
    stack_trace           TEXT,                -- verbatim from log
    summary               TEXT,                -- agent-written
    analysis              TEXT,                -- agent-written
    github_issue_number   INTEGER,             -- NULL if NEW
    github_issue_url      TEXT,
    github_issue_state    TEXT,                -- OPEN | CLOSED
    github_issue_assigned TEXT,
    labels                TEXT,
    milestone             TEXT DEFAULT '11.0.0'
);

CREATE TABLE failure_pipelines (
    failure_id      INTEGER NOT NULL REFERENCES failures(id),
    pipeline_name   TEXT NOT NULL,
    build_id        INTEGER,
    build_number    TEXT,
    PRIMARY KEY (failure_id, pipeline_name)
);

CREATE TABLE failure_tests (
    failure_id      INTEGER NOT NULL REFERENCES failures(id),
    pipeline_name   TEXT NOT NULL,
    run_name        TEXT NOT NULL,
    test_name       TEXT NOT NULL
);
```

## Workflow

### Debug Log

All output goes in `logs/` (sibling of `scripts/`).

- **Debug Log** (`logs/ci-pipeline-monitor-<timestamp>.log`) — always generated.
  **⚠️ Write incrementally by appending after each API call and decision.**
  Do NOT compose the log from memory at the end of the run — this defeats
  the crash-recovery purpose. If the process crashes mid-run, the log must
  contain everything up to the crash point. Use file append operations
  (Python `open(..., 'a')` or PowerShell `Add-Content`) to write each log
  entry immediately after the action it describes. Follow
  [`log-template.md`](log-template.md). Log every API call URL + response
  summary, every decision with reasoning, timestamps, and errors.
- **Test Report** (`logs/test-report-<timestamp>.md`) — always generated
  via `generate_report.py`.

### Step 0: Prerequisites (agent)

Run before anything else. See [`references/prerequisites.md`](references/prerequisites.md)
for full details.

1. `pip install requests`
2. Ensure `ADO_TOKEN` env var is set (required for Step 3).

**⚠️ Do NOT proceed to Step 3 without a valid `ADO_TOKEN`.** The Test Results
API returns 203 (sign-in HTML) without auth, even on dnceng-public.

### Step 1: Resolve Pipeline Definitions (agent)

Compare the **Pipeline Details** table (source of truth) against the **Cached
Definition ID Mapping** table in `pipelines.md`:

1. For each pipeline in Pipeline Details that is **not** marked **Private** or
   **skip** in its Notes column:
   - If it already has a row with a numeric Def ID in the Cached Mapping table,
     do nothing (already resolved).
   - If it has no row in the Cached Mapping table, or its row has `—` as the
     Def ID, resolve it via the AzDO Definitions API:
     ```
     GET https://dev.azure.com/dnceng-public/public/_apis/build/definitions?name={pipeline_name}&api-version=7.1
     ```
   - If the API returns a match, add or update the row in the Cached Mapping
     table with the resolved Def ID.
   - If the API returns no match, log a warning and skip that pipeline.

2. For pipelines in the Cached Mapping table that are **no longer in Pipeline
   Details**, leave them (stale rows are harmless — the script only processes
   pipelines present in the Cached Mapping table).

Do NOT re-resolve IDs that are already populated with a numeric value.

### Step 2: Fetch Latest Builds (deterministic — scripted)

```bash
python scripts/setup_and_fetch_builds.py --pipelines pipelines.md --db scripts/monitor.db
```

Creates DB, fetches latest build per pipeline, populates `pipelines` table,
outputs failing build IDs.

### Step 3: Extract Failed Tests and Fetch Logs (deterministic — scripted)

```bash
python scripts/extract_failed_tests.py --db scripts/monitor.db
python scripts/fetch_helix_logs.py --db scripts/monitor.db
```

Extracts individual failed test methods and downloads their full Helix console
logs to disk.

**⚠️ Every individual failure must be INSERT'd into `test_results` immediately.**
- `extract_failed_tests.py`: inserts one row per failed test method (test_name,
  run_name, pipeline_name, helix_job_id, helix_work_item, console_log_url,
  **error_message, stack_trace from the ADO API**). The API provides useful
  error/stack for most xUnit assertion failures. For crashes and timeouts,
  the API returns a generic "Helix Work Item failed" message — these are
  stored as empty so the agent can extract the real error from the console log.
- `fetch_helix_logs.py`: downloads the full console log to `helix-logs/`
  (a separate directory — NOT mixed with `logs/`) and UPDATEs the
  corresponding `test_results` row with `exit_code` and `console_log_path`.
  Uses `console_log_path IS NULL` as the sentinel for unprocessed rows.
- After Step 3, `test_results` contains the complete raw inventory of every
  failure with its exit code, a path to the full console log on disk, and
  API-provided error/stack where available. `failure_id` is NULL — it is
  populated by the agent in Step 4.

### Step 4: Triage Failures (agent — non-deterministic)

See [`references/triage-workflow.md`](references/triage-workflow.md) for full instructions.

**⚠️ INSERT into `failures` table immediately after triaging each failure group.**

### Step 5: Validate DB (deterministic — scripted)

```bash
python scripts/validate_results.py --db scripts/monitor.db --pipelines pipelines.md --log logs/ci-pipeline-monitor-<timestamp>.log
```

Runs 24 checks across data completeness, referential integrity, data quality,
and content accuracy. Exits 1 on failure.

For the full list of checks, see [`references/validation-checks.md`](references/validation-checks.md).

### Step 5a: Fix Validation Failures (up to 3 retries)

If any checks fail after Step 5:

1. **Read the validator output** — each FAIL line includes the specific
   test_results IDs, failure IDs, or field names that failed.

2. **For each fixable failure** (e.g., truncated stack trace, missing
   error_message):
   - Look up the test_results row in the DB
   - Re-read the console log file at `console_log_path`
   - UPDATE the corrected field in the DB

3. **Re-run the validator:**
   ```bash
   python scripts/validate_results.py --db scripts/monitor.db --pipelines pipelines.md --log <log_path>
   ```

4. **If failures decreased**, repeat from step 1 (up to 3 total retries).
   **If failures did NOT decrease** (same or more), stop retrying.

5. **If checks still fail after retries**, log each as a WARN in the debug
   log with clickable links and move on:
   ```
   [WARN] Validation error persists after retry — <check description>
     Pipeline: [<name> <build_number>](<ado_test_results_tab_url>)
     Console Log: [Console Log](<helix_url>)
     Field: <field_name>, failure_id=<N>
   ```

**Stop retrying when failure count stops decreasing or after 3 attempts.**
Log remaining WARNs and proceed to report generation. Some failures
(e.g., LLM output truncation) may not be fixable programmatically.

### Step 6: Generate Report (deterministic — scripted)

```bash
# If validation passed (Step 5/5a exit code 0):
python scripts/generate_report.py --db scripts/monitor.db

# If validation had unresolved warnings (Step 5/5a exit code 1):
python scripts/generate_report.py --db scripts/monitor.db --validation-warnings
```

Reads DB, outputs report following `report-template.md`. Only run after
DB validation (Step 5/5a) is complete so the report is generated once.

### Step 7: Bisect Regressions (agent — on request)

1.  Check `failing_since_date`/`failing_since_build` from `failures` table
2.  Get commit range between failing and last passing build
3.  List PRs merged in that range via GitHub MCP server
4.  Check file overlap with test's source area
5.  Rank and present top candidates with evidence

## Banned Tools and APIs

- **`ado-pipelines_*` and `ado-testplan_*` MCP tools are banned:** 
  - `ado-testplan_show_test_results_from_build_id` returns 1M+ rows and times out.
- `ado-pipelines_get_builds`, `get_build_log`, `get_build_status`,
  `get_build_changes`, `get_build_log_by_id`
- **Build Timeline API is banned** (`/_apis/build/builds/{id}/timeline`):
  - reports at work-item level only, silently misses individual test failures. Always use the AzDO Test Results API via `extract_failed_tests.py`.
- Use `powershell` with `requests` for any direct API calls.

## Allowed Tools

| Step | Tools | Purpose |
|------|-------|---------|
| 0 | `powershell` | Install dependencies, obtain `ADO_TOKEN` |
| 1 | `powershell`, `edit` | Resolve def IDs via AzDO API, update `pipelines.md` |
| 2-3 | `powershell` | Run scripts |
| 4 | `powershell`, `github-mcp-server-search_issues`, `github-mcp-server-issue_read` | Read logs, search GitHub, INSERT failures |
| 5 | `powershell` | Run `validate_results.py` |
| 5a | `powershell`, `view` | Fix validation failures, re-validate (up to 3 retries) |
| 6 | `powershell` | Run `generate_report.py` |
| 7 | `github-mcp-server-list_commits`, `get_commit`, `search_pull_requests`, `get_file_contents` | Trace regressions |

File I/O tools (`view`, `edit`, `create`, `grep`, `glob`) always allowed.

## Rules

### Extraction

- **Every individual failure must be saved to `test_results`** — this is the
  complete inventory. No failure may exist only in JSON output or in memory.
- Never skip failures — cross-check counts against AzDO summary. If results
  appear truncated, paginate until all are listed.
- Analyze every failing pipeline — never skip a pipeline or mark it as
  "needs investigation" or "expected same failures". Every pipeline must have
  confirmed findings from its own test results.
- If a pipeline/API call fails, log a warning and continue — never block the run.
- Use sub-agents for parallel failure extraction — delegate pipeline groups to
  separate general-purpose agents via the task tool. Pass the `ADO_TOKEN` to
  each sub-agent for AzDO Test Results API. Helix API needs no auth.
- Old `failingSince` builds may be purged (>90 days). Link to the latest
  failed build instead of generating a dead URL.
- **AzDO Test Results API requires a bearer token** — see Step 0 for how to
  obtain and set `ADO_TOKEN`. The token is valid ~60 minutes.
- **AzDO Builds API and Helix API require NO authentication.**

### Triage

- **Read the FULL console log file** — do NOT read only the tail or a partial range.
- **Classify using BOTH exit code AND error message** — same exit code does
  NOT mean the same root cause.
- Do NOT group failures by exit code alone — read the actual error messages.
- For detailed triage workflow, see [`references/triage-workflow.md`](references/triage-workflow.md).
- For verbatim copy-paste rules, see [`references/verbatim-rules.md`](references/verbatim-rules.md).

### Bisect

- When bisecting, present evidence — don't guess.

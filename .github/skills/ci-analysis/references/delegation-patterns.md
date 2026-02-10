# Subagent Delegation Patterns

CI investigations often involve repetitive, mechanical work that burns main conversation context. Delegate these to subagents.

## Pattern 1: Scanning Multiple Console Logs

**When:** Multiple failing work items across several jobs — need to extract and deduplicate test failure names.

**Problem:** Each work item's console log can be thousands of lines. Reading 5+ logs in main context burns most of your context budget on raw output.

**Delegate:**
```
Fetch Helix console logs for these work items and extract all unique test failures:

Job: {JOB_ID_1}
  Work items: dotnet.Tests.dll.19, dotnet.Tests.dll.23

Job: {JOB_ID_2}
  Work items: dotnet.Tests.dll.19

For each, use:
  C:\Users\lewing\.copilot\skills\ci-analysis\scripts\Get-CIStatus.ps1 -HelixJob "{JOB}" -WorkItem "{ITEM}"

From the console output, extract lines matching xUnit failure format:
  [xUnit.net HH:MM:SS.ss] TestNamespace.TestClass.TestMethod [FAIL]

IMPORTANT: Lines with [OUTPUT] or [PASS] are NOT failures.
Only lines ending with [FAIL] indicate actual test failures.

Deduplicate across all work items.
Return: unique FAIL test names + which work items they appeared in.
```

**Result:** A clean list of unique failures instead of pages of raw logs.

## Pattern 2: Finding a Baseline Build

**When:** A test fails on a PR — need to confirm it passes on main to prove the failure is PR-caused.

**Problem:** Requires searching recent merged PRs or main CI runs, finding the matching build, locating the right Helix job and work item. Multiple API calls.

**Delegate:**
```
Find a recent passing build on the main branch of dotnet/{REPO} that ran the same test leg as this failing build.

Failing build: {BUILD_ID} (PR #{PR_NUMBER})
Failing job name: {JOB_NAME} (e.g., "TestBuild linux x64")
Failing work item: {WORK_ITEM} (e.g., "dotnet.Tests.dll.19")

Steps:
1. Use GitHub MCP to find recently merged PRs to main:
   github-mcp-server-search_pull_requests query:"is:merged base:main" owner:dotnet repo:{REPO}
2. Pick the most recent merged PR
3. Run the CI script to check its build status:
   ./scripts/Get-CIStatus.ps1 -PRNumber {MERGED_PR} -Repository "dotnet/{REPO}"
4. Find the build that passed with the same job name
5. Find the Helix job ID for that job (may need to download build artifacts — see azure-cli.md and binlog-comparison.md for "binlogs to find binlogs")
6. Confirm the matching work item passed

Return: the passing build ID, Helix job ID, and work item name, or "no recent passing build found".
```

## Pattern 3: Narrowing Merge Diffs to Relevant Files

**When:** A large merge PR (hundreds of commits, hundreds of changed files) has test failures — need to identify which changes are relevant.

**Problem:** `git diff` on a 458-file merge is overwhelming. Most changes are unrelated to the specific failure.

**Delegate:**
```
Given these test failures on merge PR #{PR_NUMBER} (branch: {SOURCE} → {TARGET}):
  - {TEST_1}
  - {TEST_2}

Find the changed files most likely to cause these failures.

Steps:
1. Get the list of changed files: git diff --name-only {TARGET}...{SOURCE}
2. Filter to files matching these patterns (adjust per failure type):
   - For MSBuild/build failures: *.targets, *.props, Directory.Build.*, eng/Versions.props
   - For test failures: test project files, test assets
   - For specific SDK areas: src/Tasks/, src/Cli/, src/WasmSdk/
3. For each relevant file, show the key diff hunks (not the full diff)
4. Look for version bumps, property changes, or behavioral changes

Return: the 5-10 most relevant changed files with a one-line summary of what changed in each.
```

## Pattern 4: Parallel Binlog Extraction

**When:** Comparing two builds — see [binlog-comparison.md](binlog-comparison.md).

**Key insight:** Launch two subagents simultaneously (one per build). Each downloads a binlog, loads it into the MCP server, extracts task parameters, normalizes paths, and returns a sorted arg list. The main agent just diffs the two lists.

## General Guidelines

- **Use `task` agent type** for all delegation (it has shell + MCP access)
- **Run independent tasks in parallel** (e.g., two binlog extractions)
- **Include the CI script path** in every prompt — subagents don't inherit skill context
- **Ask for structured output** — "return a list of X" not "show me what you find"
- **Don't delegate interpretation** — subagents extract data, main agent interprets meaning

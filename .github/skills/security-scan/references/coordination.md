# Coordination Patterns for Large-Scale Scans

SQL-based progress tracking and subagent delegation for scanning many libraries or files.

## Contents

- [When to use coordination](#when-to-use-coordination)
- [Progress tracking with SQL](#progress-tracking)
- [Partitioning work](#partitioning-work)
- [Subagent delegation](#subagent-delegation)
- [Aggregating findings](#aggregating-findings)
- [Caching results](#caching-results)

## When to Use Coordination

| Scope | Files | Use coordination? |
|---|---|---|
| Diff review | < 20 changed files | No — analyze directly |
| Targeted files | < 10 files | No — analyze directly |
| Single library audit | 1 library | No — single subagent is fine |
| Multi-library audit | 2-5 libraries | Yes — partition + track |
| Directory-wide audit | 5+ libraries or 50+ files | Yes — full coordination |

## Progress Tracking

Create a tracking table at the start of any multi-library scan:

```sql
CREATE TABLE IF NOT EXISTS scan_targets (
  library TEXT PRIMARY KEY,
  file_count INT,
  status TEXT DEFAULT 'pending',  -- pending, scanning, done, skipped
  agent_id TEXT,                   -- task agent ID if delegated
  finding_count INT DEFAULT 0,
  notes TEXT
);
```

Insert one row per library or directory to scan:

```sql
-- Example: auditing all networking libraries
INSERT INTO scan_targets (library, file_count) VALUES
  ('System.Net.Http', 42),
  ('System.Net.Sockets', 38),
  ('System.Net.Security', 27);
```

Track progress as agents complete:

```sql
-- Before dispatching
UPDATE scan_targets SET status = 'scanning', agent_id = 'agent-xyz' WHERE library = 'System.Net.Http';

-- After agent returns
UPDATE scan_targets SET status = 'done', finding_count = 2 WHERE library = 'System.Net.Http';

-- Check what's left
SELECT library, file_count FROM scan_targets WHERE status = 'pending';

-- Summary
SELECT status, COUNT(*) as count, SUM(finding_count) as findings
FROM scan_targets GROUP BY status;
```

## Partitioning Work

### Pre-filter with triage script

Before partitioning, run the triage script to identify security-relevant files:

```bash
python .github/skills/security-scan/scripts/scan_security_surface.py src/libraries/System.Net.Http/src --json
```

Only partition and dispatch `high` and `medium` priority files. `skip` files never enter the pipeline.

### Triage agent (Layer 2 — for 20+ security-relevant files)

When the triage script returns 20+ files at `high`/`medium` priority, launch a fast `explore` (Haiku) agent to refine the list:

```
Review these security-relevant files and refine their priority.
The triage script flagged them based on pattern matching — your job is to
check whether the signals represent real attack surface or false positives.

Files:
{JSON file list from triage script}

For each file:
1. Read the first 50 lines (imports, class declaration, key methods)
2. Determine if the security signals are in production code paths or dead code
3. Check if the file is a thin wrapper vs. contains real logic

Return JSON:
{
  "refinements": [
    { "path": "...", "newPriority": "high|medium|skip", "reason": "..." }
  ]
}
```

### By library (preferred for directory audits)

Each `src/libraries/<Name>` directory is a natural partition. List libraries in scope, then assign each to a subagent:

```bash
# List libraries under a directory
ls -d src/libraries/System.Net.*/src | head -20
```

### By file type (for mixed-language areas)

When scanning `src/coreclr/` or `src/native/`, partition by language since vulnerability categories differ:

| Partition | File types | Focus |
|---|---|---|
| Managed C# | `*.cs` | Serialization, injection, auth |
| Native C/C++ | `*.cpp`, `*.h`, `*.c` | Memory safety, buffer overflows, null derefs |
| Interop boundary | `*.cs` with `DllImport`/`LibraryImport` | Marshal mismatches, buffer sizes |

### Batch sizing

- **Max 10-15 source files per subagent** — beyond this, analysis quality degrades
- **Max 5 parallel subagents** — more causes context thrashing in the main agent
- For large libraries, split into batches of files within the same subagent dispatch

## Subagent Delegation

### Discovery agent template

Launch one `general-purpose` subagent per library/partition:

```
Security scan the following library: {LIBRARY_NAME}

Source directory: src/libraries/{LIBRARY_NAME}/src/
Files to scan:
{FILE_LIST}

Analyze each file for security vulnerabilities. Focus on these categories:
- [paste relevant categories from references/runtime-categories.md]

Exclusions: Skip test code, docs, and DOS/resource-exhaustion findings.

For each finding, provide:
1. File path and line number
2. Category (e.g., command_injection, path_traversal)
3. Description of the vulnerability
4. Concrete exploit scenario
5. Confidence score (1-10)

Return JSON:
{
  "library": "{LIBRARY_NAME}",
  "filesScanned": N,
  "findings": [
    {
      "file": "path/to/file.cs",
      "line": 42,
      "category": "path_traversal",
      "severity": "HIGH",
      "description": "...",
      "exploit": "...",
      "confidence": 9
    }
  ]
}
```

### Verification agent template

For each finding with confidence ≥ 7, launch a parallel `explore` agent:

```
Verify this security finding:

File: {FILE_PATH}:{LINE}
Category: {CATEGORY}
Claimed vulnerability: {DESCRIPTION}

Steps:
1. Read the full source file
2. Check callers of the vulnerable function — are inputs validated upstream?
3. Check callees — does the called function do its own validation?
4. Search for similar patterns in the codebase (grep for the function name)
5. Check if this matches a known-safe pattern in dotnet/runtime

Return JSON:
{
  "verified": true/false,
  "confidence": N,
  "justification": "...",
  "mitigations_found": ["caller validates in X.cs:100", ...]
}
```

### Storing subagent results

When running 5+ subagents, store findings in SQL instead of holding in context:

```sql
CREATE TABLE IF NOT EXISTS scan_findings (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  library TEXT,
  file_path TEXT,
  line_number INT,
  category TEXT,
  severity TEXT,       -- HIGH, MEDIUM
  description TEXT,
  exploit_scenario TEXT,
  confidence INT,      -- 1-10 from discovery agent
  verified BOOLEAN,    -- NULL until verified
  verified_confidence INT,  -- from verification agent
  status TEXT DEFAULT 'pending'  -- pending, verified, dismissed
);
```

```sql
-- After verification, find reportable findings
SELECT library, file_path, line_number, category, severity, description, exploit_scenario
FROM scan_findings
WHERE verified = TRUE AND verified_confidence >= 8
ORDER BY severity DESC, verified_confidence DESC;

-- Summary by library
SELECT library, COUNT(*) as findings,
       SUM(CASE WHEN severity = 'HIGH' THEN 1 ELSE 0 END) as high,
       SUM(CASE WHEN severity = 'MEDIUM' THEN 1 ELSE 0 END) as medium
FROM scan_findings WHERE verified = TRUE AND verified_confidence >= 8
GROUP BY library;
```

## Aggregating Findings

After all subagents complete and findings are verified:

1. Query `scan_findings` for all verified findings with confidence ≥ 8
2. Group by library for the "Files Reviewed" section
3. Deduplicate — same pattern in multiple files should be one finding with multiple locations
4. Format using the template in [output-format.md](output-format.md)

```sql
-- Detect duplicates (same category + similar description in same library)
SELECT category, COUNT(*) as occurrences, GROUP_CONCAT(file_path) as files
FROM scan_findings
WHERE verified = TRUE AND verified_confidence >= 8
GROUP BY library, category
HAVING occurrences > 1;
```

## Caching Results

For iterative scans (e.g., re-running after fixes), avoid re-scanning unchanged files:

```sql
CREATE TABLE IF NOT EXISTS scan_cache (
  file_path TEXT PRIMARY KEY,
  file_hash TEXT,          -- git hash of the file at scan time
  scanned_at TEXT,
  finding_count INT,
  findings_json TEXT       -- serialized findings for this file
);
```

```bash
# Get current file hash for cache comparison
git hash-object src/libraries/System.Net.Http/src/HttpClient.cs
```

```sql
-- Check if a file needs re-scanning
SELECT file_path FROM scan_cache
WHERE file_path = 'src/libraries/System.Net.Http/src/HttpClient.cs'
  AND file_hash = 'abc1234';
-- If row exists, skip this file. If not, scan and insert/update.
```

When to cache vs. not:

| Scenario | Cache? |
|---|---|
| One-off diff review | No |
| Iterative directory audit (fix → re-scan cycle) | Yes |
| Periodic full-repo audit | Yes |
| Scanning after a rebase | Invalidate all — hashes changed |

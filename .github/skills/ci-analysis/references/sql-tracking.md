# SQL Tracking for CI Investigations

Use the SQL tool to track structured data during complex investigations. This avoids losing context across tool calls and enables queries that catch mistakes (like claiming "all failures known" when some are unmatched).

## Failed Job Tracking

Track each failure from the script output and map it to known issues as you verify them:

```sql
CREATE TABLE IF NOT EXISTS failed_jobs (
  build_id INT,
  job_name TEXT,
  error_category TEXT,   -- from failedJobDetails: test-failure, build-error, crash, etc.
  error_snippet TEXT,
  known_issue_url TEXT,  -- NULL if unmatched
  known_issue_title TEXT,
  is_pr_correlated BOOLEAN DEFAULT FALSE,
  recovery_status TEXT DEFAULT 'not-checked',  -- effectively-passed, real-failure, no-results
  notes TEXT,
  PRIMARY KEY (build_id, job_name)
);
```

### Key queries

```sql
-- Unmatched failures (Build Analysis red = these exist)
SELECT job_name, error_category, error_snippet FROM failed_jobs
WHERE known_issue_url IS NULL;

-- Are ALL failures accounted for?
SELECT COUNT(*) as total,
       SUM(CASE WHEN known_issue_url IS NOT NULL THEN 1 ELSE 0 END) as matched
FROM failed_jobs;

-- Which crash/canceled jobs need recovery verification?
SELECT job_name, build_id FROM failed_jobs
WHERE error_category IN ('crash', 'unclassified') AND recovery_status = 'not-checked';

-- PR-correlated failures (fix before retrying)
SELECT job_name, error_snippet FROM failed_jobs WHERE is_pr_correlated = TRUE;
```

### Workflow

1. After the script runs, insert one row per failed job from `failedJobDetails`
2. For each known issue from `knownIssues`, UPDATE matching rows with the issue URL
3. Query for unmatched failures — these need investigation
4. For crash/canceled jobs, update `recovery_status` after checking Helix results

## Build Progression

See [build-progression-analysis.md](build-progression-analysis.md) for the `build_progression` and `build_failures` tables that track pass/fail across multiple builds.

## When to Use SQL vs. Not

| Situation | Use SQL? |
|-----------|----------|
| 1-2 failed jobs, all match known issues | No — straightforward, hold in context |
| 3+ failed jobs across multiple builds | Yes — prevents missed matches |
| Build progression with 5+ builds | Yes — see build-progression-analysis.md |
| Crash recovery across multiple work items | Yes — cache testResults.xml findings |
| Single build, single failure | No — overkill |

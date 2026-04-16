# Validation Checks

```bash
python scripts/validate_results.py --db scripts/monitor.db [--pipelines pipelines.md] [--report logs/test-report-<timestamp>.md] [--log logs/ci-pipeline-monitor-<timestamp>.log]
```

Runs 24 checks before the report is considered done:

**Data Completeness:** all pipelines accounted for, every failing pipeline
has test_results, all console logs fetched, all test_results triaged.

**Referential Integrity:** no orphan failure_pipelines or failure_tests,
every failure has at least one pipeline and one test.

**Data Quality:** no empty error_message/test_name in failures,
every failure has non-empty error_message.

**Content Accuracy:** error_message in the failures table appears verbatim
in the source console log file (identified by source_test_result_id),
exit_code in test_results matches the exit code parsed from the console
log, every failure has console_log_url populated, error_message is a
managed .NET exception not native debugger output (catches extracting
from wrong section of log), NEW failures do not share the same error
pattern (error_message + first stack_trace line) as failures already
matched to a GitHub issue (catches missed issue matches), NEW failures
are verified against GitHub Search API (unauthenticated, via urllib) using the full
test name to confirm no matching issue exists, every line in
error_message and stack_trace appears as a complete line in the console
log (catches mid-line truncation).

**Report Sanity** (if `--report` provided): report is non-empty, failure
count matches DB, all failing pipelines mentioned.

**Debug Log** (if `--log` provided): log file is non-empty, contains all
required step headers (Prerequisites, Load Pipeline Definitions, Fetch Latest
Builds, Extract Failed Tests, Fetch Helix Console Logs, Triage, Validate DB,
Generate Report), contains SUMMARY section.

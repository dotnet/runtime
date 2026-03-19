# Validation Checks — Step 5.5 Details

```bash
python scripts/validate_results.py --db scripts/monitor.db --pipelines pipelines.md --report logs/test-report-<timestamp>.md --log logs/ci-pipeline-monitor-<timestamp>.log
```

Runs 24 checks before the report is considered done:

**Data Completeness:** all pipelines accounted for, every failing pipeline
has test_results, all console logs fetched, all test_results triaged.

**Referential Integrity:** no orphan failure_pipelines or failure_tests,
every failure has at least one pipeline and one test.

**Data Quality:** no empty error_message/test_name in failures,
github_issues covers all referenced issue numbers, action_items exist,
no duplicate failure groups.

**Content Accuracy:** error_message text appears verbatim in the
corresponding console log file (catches fabricated messages), exit_code
in test_results matches the exit code parsed from the console log (catches
overwrite bugs), all test_results sharing a failure_id have consistent
error patterns (catches wrong grouping of unrelated failures),
every failure has console_log_url populated, error_message is a managed
.NET exception not native debugger output (catches extracting from wrong
section of log), NEW failures do not share the same error pattern as
failures already matched to a GitHub issue (catches missed issue matches
when the issue covers multiple test names in its body), NEW failures are
verified against GitHub Search API using the full test name to confirm no
matching issue exists (catches the LLM skipping or fabricating searches),
every line in error_message and stack_trace appears as a complete line in
the console log (catches mid-line truncation where a line is cut off before
its end).

**Report Sanity** (if `--report` provided): report is non-empty, failure
count matches DB, all failing pipelines mentioned.

**Debug Log** (if `--log` provided): log file is non-empty, contains all
required step headers (STEP 1 through STEP 5), contains SUMMARY section.

If any check fails, fix the issue and re-run. Do NOT publish the report
until all checks pass.

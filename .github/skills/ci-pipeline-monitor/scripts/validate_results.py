"""Validate monitoring run results before publishing the final report.

Runs 24+ checks against the database, optionally against the report file,
and optionally against the debug log file.
Prints PASS/FAIL for each check and exits with code 1 if any check fails.

Usage:
    python validate_results.py --db monitor.db [--pipelines pipelines.md] [--report report.md] [--log debug.log]
"""
import argparse
import json
import os
import re
import sqlite3
import sys
import time
import urllib.error
import urllib.parse
import urllib.request


def check(name, passed, detail="", warn_only=False):
    if not passed and warn_only:
        status = "WARN"
    else:
        status = "PASS" if passed else "FAIL"
    suffix = f" — {detail}" if detail else ""
    print(f"  [{status}] {name}{suffix}")
    return passed


def count(conn, sql):
    return conn.execute(sql).fetchone()[0]


def main():
    parser = argparse.ArgumentParser(description="Validate monitor.db completeness")
    parser.add_argument("--db", required=True, help="Path to monitor.db")
    parser.add_argument("--pipelines", help="Path to pipelines.md (for expected count)")
    parser.add_argument("--report", help="Path to generated report .md file")
    parser.add_argument("--log", help="Path to debug .log file")
    args = parser.parse_args()

    conn = sqlite3.connect(args.db)
    conn.row_factory = sqlite3.Row
    failures = 0
    warnings = 0
    total = 0

    # Check which tables exist
    tables = {r[0] for r in conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table'"
    ).fetchall()}
    has_test_results = 'test_results' in tables

    print("=== Data Completeness ===")

    # 1. All pipelines accounted for
    total += 1
    pipeline_count = count(conn, "SELECT COUNT(*) FROM pipelines")
    if args.pipelines:
        with open(args.pipelines, encoding="utf-8") as f:
            expected = sum(1 for line in f if re.match(r'\|\s*[^|]+\s*\|\s*(\d+|—)\s*\|', line)
                          and not line.strip().startswith('| Pipeline')
                          and '---' not in line)
        ok = check("All pipelines accounted for",
                    pipeline_count == expected,
                    f"{pipeline_count} in DB, {expected} in pipelines.md")
    else:
        ok = check("All pipelines accounted for",
                    pipeline_count > 0,
                    f"{pipeline_count} pipelines (no pipelines.md to compare)")
    if not ok:
        failures += 1

    # 2. Every failing pipeline has test results
    if has_test_results:
        total += 1
        missing = conn.execute("""
            SELECT p.name FROM pipelines p
            WHERE p.result = 'failed'
            AND p.name NOT IN (SELECT DISTINCT pipeline_name FROM test_results)
        """).fetchall()
        ok = check("Every failing pipeline has test_results",
                    len(missing) == 0,
                    f"missing: {[r['name'] for r in missing]}" if missing else "")
        if not ok:
            failures += 1

        # 3. All console logs fetched (saved to disk)
        total += 1
        unfetched = count(conn, """
            SELECT COUNT(*) FROM test_results
            WHERE console_log_path IS NULL
            AND console_log_url IS NOT NULL AND console_log_url != ''
        """)
        ok = check("All console logs fetched",
                    unfetched == 0,
                    f"{unfetched} rows still without console_log_path" if unfetched else "")
        if not ok:
            failures += 1

        # 4. All failures triaged (assigned to a group)
        total += 1
        untriaged = count(conn, "SELECT COUNT(*) FROM test_results WHERE failure_id IS NULL")
        ok = check("All test_results triaged (failure_id assigned)",
                    untriaged == 0,
                    f"{untriaged} rows without failure_id" if untriaged else "")
        if not ok:
            failures += 1
    else:
        print("  [SKIP] test_results table not found — checks 2-4 skipped")

    print("\n=== Referential Integrity ===")

    # 5. No orphan failure_pipelines
    total += 1
    orphan_fp = count(conn, """
        SELECT COUNT(*) FROM failure_pipelines
        WHERE failure_id NOT IN (SELECT id FROM failures)
    """)
    ok = check("No orphan failure_pipelines",
               orphan_fp == 0,
               f"{orphan_fp} orphans" if orphan_fp else "")
    if not ok:
        failures += 1

    # 6. No orphan failure_tests
    total += 1
    orphan_ft = count(conn, """
        SELECT COUNT(*) FROM failure_tests
        WHERE failure_id NOT IN (SELECT id FROM failures)
    """)
    ok = check("No orphan failure_tests",
               orphan_ft == 0,
               f"{orphan_ft} orphans" if orphan_ft else "")
    if not ok:
        failures += 1

    # 7. Every failure has at least one pipeline
    total += 1
    no_pipeline = conn.execute("""
        SELECT id, title FROM failures
        WHERE id NOT IN (SELECT failure_id FROM failure_pipelines)
    """).fetchall()
    ok = check("Every failure has at least one pipeline",
               len(no_pipeline) == 0,
               f"missing: {[(r['id'], r['title']) for r in no_pipeline]}" if no_pipeline else "")
    if not ok:
        failures += 1

    # 8. Every failure has at least one test
    total += 1
    no_test = conn.execute("""
        SELECT id, title FROM failures
        WHERE id NOT IN (SELECT failure_id FROM failure_tests)
    """).fetchall()
    ok = check("Every failure has at least one test",
               len(no_test) == 0,
               f"missing: {[(r['id'], r['title']) for r in no_test]}" if no_test else "")
    if not ok:
        failures += 1

    print("\n=== Data Quality ===")

    # 9. No empty critical fields in failures
    total += 1
    bad_failures = count(conn, """
        SELECT COUNT(*) FROM failures
        WHERE error_message IS NULL OR error_message = ''
           OR test_name IS NULL OR test_name = ''
    """)
    ok = check("No empty error_message or test_name in failures",
               bad_failures == 0,
               f"{bad_failures} rows with empty fields" if bad_failures else "")
    if not ok:
        failures += 1

    # 12. Each unique failure should appear only once in the triage results
    total += 1
    dupes = conn.execute("""
        SELECT test_name,
               SUBSTR(error_message, 1, INSTR(error_message || CHAR(10), CHAR(10)) - 1) as error_sig,
               COUNT(*) as cnt
        FROM failures
        GROUP BY test_name, error_sig HAVING cnt > 1
    """).fetchall()
    ok = check("Each test+error combination is triaged exactly once",
               len(dupes) == 0,
               f"duplicates: {[(r[0], r[2]) for r in dupes]}" if dupes else "")
    if not ok:
        failures += 1

    # 13. error_message appears verbatim in console log file
    print("\n=== Content Accuracy ===")
    if has_test_results:
        total += 1
        mismatches = []
        rows = conn.execute("""
            SELECT f.id, f.error_message, f.source_test_result_id,
                   tr.console_log_path
            FROM failures f
            LEFT JOIN test_results tr ON f.source_test_result_id = tr.id
            WHERE f.error_message IS NOT NULL AND f.error_message != ''
            AND f.failure_category != 'infrastructure'
        """).fetchall()
        verified = 0
        for r in rows:
            log_path = r["console_log_path"]
            if not log_path or not os.path.isfile(log_path):
                # No source_test_result_id or log missing — try any log in the group
                fallback = conn.execute("""
                    SELECT console_log_path FROM test_results
                    WHERE failure_id = ? AND console_log_path IS NOT NULL AND console_log_path != ''
                    LIMIT 1
                """, (r["id"],)).fetchone()
                if fallback:
                    log_path = fallback["console_log_path"]
                if not log_path or not os.path.isfile(log_path):
                    continue
            with open(log_path, encoding="utf-8", errors="replace") as lf:
                log_content = lf.read()
            first_line = ""
            for line in r["error_message"].split("\n"):
                stripped = line.strip()
                if stripped:
                    first_line = stripped[:80]
                    break
            if first_line and first_line not in log_content:
                mismatches.append((r["id"], first_line[:60]))
            else:
                verified += 1
        ok = check("error_message matches console log",
                    len(mismatches) == 0,
                    f"{len(mismatches)} mismatches: {mismatches[:5]}" if mismatches
                    else f"verified {verified} failure groups")
        if not ok:
            failures += 1

    # 14. exit_code in test_results matches console log
    if has_test_results:
        total += 1
        exit_mismatches = []
        rows = conn.execute("""
            SELECT tr.id, tr.console_log_path, tr.exit_code, f.failure_category
            FROM test_results tr
            JOIN failures f ON tr.failure_id = f.id
            WHERE tr.console_log_path IS NOT NULL AND tr.console_log_path != ''
            AND f.failure_category != 'infrastructure'
        """).fetchall()
        # Validate once per unique console_log_path (shared logs are valid,
        # just deduplicated by fetch_helix_logs.py).
        validated_paths = {}  # path -> parsed exit_code
        skipped = 0
        for r in rows:
            log_path = r["console_log_path"]
            db_exit = r["exit_code"]
            # Skip rows with NULL (timeout) or -1 (coreclr multi-test) —
            # no meaningful exit code to cross-check.
            if db_exit is None or db_exit == -1:
                skipped += 1
                continue
            # Parse log file once per unique path
            if log_path not in validated_paths:
                if not os.path.isfile(log_path):
                    continue
                with open(log_path, encoding="utf-8", errors="replace") as lf:
                    log_content = lf.read()
                # Same logic as fetch_helix_logs.py
                if 'Command timed out' in log_content:
                    validated_paths[log_path] = None
                    continue
                exit_codes = []
                for line in log_content.split('\n'):
                    m = re.search(r'exit code[:\s]+(-?\d+)', line, re.IGNORECASE)
                    if m:
                        exit_codes.append(int(m.group(1)))
                    m2 = re.search(r'_commandExitCode=(\d+)', line)
                    if m2:
                        exit_codes.append(int(m2.group(1)))
                    m3 = re.search(r'Exit Code:(\d+)', line)
                    if m3:
                        exit_codes.append(int(m3.group(1)))
                if exit_codes:
                    log_exit = next((c for c in exit_codes if c != 0), exit_codes[-1])
                    if log_exit == 100 and 'Command exited with 0' in log_content:
                        log_exit = -1
                    validated_paths[log_path] = log_exit
                else:
                    validated_paths[log_path] = None
            log_exit = validated_paths[log_path]
            if log_exit is not None and db_exit != log_exit:
                exit_mismatches.append(
                    (r["id"], f"db={db_exit} log={log_exit}")
                )
        verified = len(rows) - skipped
        ok = check("exit_code matches console log",
                    len(exit_mismatches) == 0,
                    f"{len(exit_mismatches)} mismatches: {exit_mismatches[:5]}" if exit_mismatches
                    else f"verified {verified} rows, skipped {skipped} (NULL/-1 exit codes)")
        if not ok:
            failures += 1

    # 15. failures.error_message is non-empty for every failure group
    if has_test_results:
        total += 1
        empty_errors = conn.execute("""
            SELECT id, title FROM failures
            WHERE error_message IS NULL OR error_message = ''
        """).fetchall()
        ok = check("Every failure has non-empty error_message",
                    len(empty_errors) == 0,
                    f"{len(empty_errors)} failures with empty error_message: {[(r['id'], r['title'][:50]) for r in empty_errors]}"
                    if empty_errors else "")
        if not ok:
            failures += 1

    # --- New checks based on past mistakes ---

    # 16a. Every failure has a console_log_url populated
    total += 1
    no_url = conn.execute("""
        SELECT id, title FROM failures
        WHERE console_log_url IS NULL OR console_log_url = ''
    """).fetchall()
    ok = check("Every failure has console_log_url",
               len(no_url) == 0,
               f"missing: {[(r['id'], r['title']) for r in no_url]}" if no_url else "")
    if not ok:
        failures += 1

    # 16b. error_message is managed exception, not native debugger output
    # The error_message should be the .NET exception (e.g., "Fatal error.\n
    # System.AccessViolationException: ..."), NOT native debugger output
    # (e.g., "Access violation - code c0000005" or "KERNELBASE!...").
    # Native output belongs in analysis, not error_message.
    total += 1
    native_patterns = [
        r'^Access violation - code',
        r'^KERNELBASE!',
        r'^ntdll!',
        r'^coreclr!',
        r'^(?:first|second) chance',
    ]
    native_msgs = []
    for r in conn.execute("SELECT id, title, error_message FROM failures WHERE error_message IS NOT NULL"):
        first_line = (r["error_message"] or "").strip().split("\n")[0].strip()
        for pat in native_patterns:
            if re.match(pat, first_line, re.IGNORECASE):
                native_msgs.append((r["id"], first_line[:60]))
                break
    ok = check("error_message is managed exception (not native debugger output)",
               len(native_msgs) == 0,
               f"{len(native_msgs)} failures with native debugger text as error_message: {native_msgs}" if native_msgs
               else "")
    if not ok:
        failures += 1

    # 16c. NEW failures should not share error pattern with matched failures
    # If a failure marked NEW has the same error signature as another failure
    # that IS matched to a GitHub issue, it likely should share that issue
    # (the LLM missed the match). Signature = first 3 non-empty error_message
    # lines + first non-empty stack_trace line (crash site) to distinguish
    # same-exception-type failures with different root causes.
    total += 1

    def _build_sig(error_message, stack_trace):
        sig_lines = []
        for line in (error_message or "").split("\n"):
            s = line.strip()
            if s:
                sig_lines.append(s[:120])
                if len(sig_lines) >= 3:
                    break
        for line in (stack_trace or "").split("\n"):
            s = line.strip()
            if s:
                sig_lines.append(s[:120])
                break
        return "\n".join(sig_lines)

    matched_patterns = {}
    for r in conn.execute("""
        SELECT id, github_issue_number, error_message, stack_trace FROM failures
        WHERE github_issue_number IS NOT NULL AND error_message IS NOT NULL
    """):
        sig = _build_sig(r["error_message"], r["stack_trace"])
        if sig:
            matched_patterns[sig] = r["github_issue_number"]

    suspect_new = []
    for r in conn.execute("""
        SELECT id, title, error_message, stack_trace FROM failures
        WHERE github_issue_number IS NULL AND error_message IS NOT NULL
    """):
        sig = _build_sig(r["error_message"], r["stack_trace"])
        if sig and sig in matched_patterns:
            suspect_new.append((r["id"], r["title"], f"matches #{matched_patterns[sig]}"))
    ok = check("NEW failures don't share error pattern with matched failures",
               len(suspect_new) == 0,
               f"suspect: {suspect_new}" if suspect_new
               else "")
    if not ok:
        failures += 1

    # 16d. Verify NEW failures have no matching GitHub issue by searching GitHub API.
    # The LLM may skip or fabricate search results. This check actually hits the
    # GitHub Search API for each NEW failure's test_name to confirm no issue exists.
    # Uses unauthenticated API (10 req/min). For >10 NEW failures, searches are
    # batched with rate-limit pauses.
    total += 1
    new_failures = conn.execute("""
        SELECT id, title, test_name FROM failures
        WHERE github_issue_number IS NULL
    """).fetchall()
    missed_matches = []
    search_failures = 0
    batch_start = time.monotonic()
    for i, r in enumerate(new_failures):
        # Unauthenticated rate limit: 10 req/min — pause after every 10th request
        if i > 0 and i % 10 == 0:
            elapsed = time.monotonic() - batch_start
            wait = 60 - elapsed
            if wait > 0:
                print(f"    [INFO] Rate limit: pausing {wait:.0f}s before next batch")
                time.sleep(wait)
            batch_start = time.monotonic()
        test_name = r["test_name"]
        found_issue = None
        try:
            q = urllib.parse.quote(test_name)
            url = (
                f"https://api.github.com/search/issues?"
                f"q={q}+repo:dotnet/runtime+is:issue&per_page=5"
            )
            req = urllib.request.Request(url, headers={
                "Accept": "application/vnd.github+json",
                "User-Agent": "ci-pipeline-monitor-validator",
            })
            with urllib.request.urlopen(req, timeout=15) as resp:
                data = json.loads(resp.read())
            for item in data.get("items", []):
                title = item.get("title", "")
                body = item.get("body", "") or ""
                issue_text = title + " " + body
                if test_name.lower() in issue_text.lower():
                    found_issue = (item["number"], title[:80], item["state"])
                    break
        except Exception as e:
            print(f"    [WARN] GitHub search failed for '{test_name}': {e}")
            search_failures += 1

        if found_issue:
            missed_matches.append(
                (r["id"], r["title"][:50], f"#{found_issue[0]} '{found_issue[1]}' ({found_issue[2]})")
            )

    searched = len(new_failures) - search_failures
    if search_failures > 0 and missed_matches:
        # Searches partially failed AND found real misses in what succeeded — FAIL
        detail = (f"searched {searched}/{len(new_failures)} (search failed for {search_failures}), "
                  f"found existing issues for {len(missed_matches)}: {missed_matches}")
        ok = check("NEW failures verified against GitHub search", False, detail)
        if not ok:
            failures += 1
    elif search_failures > 0:
        # Searches partially failed but nothing bad found — WARN only
        detail = (f"searched {searched}/{len(new_failures)} (search failed for {search_failures}); "
                  f"no missed matches in successful searches")
        ok = check("NEW failures verified against GitHub search", True, detail)
        if ok:
            print(f"    [WARN] {search_failures} GitHub search requests failed (rate limit / network); "
                  f"check is incomplete but not blocking")
            warnings += 1
    else:
        ok = check("NEW failures verified against GitHub search",
                   len(missed_matches) == 0,
                   f"found existing issues for {len(missed_matches)} 'NEW' failures: {missed_matches}"
                   if missed_matches else f"confirmed {len(new_failures)} NEW failures have no matching issue")
        if not ok:
            failures += 1

    # 16f. error_message and stack_trace lines are not cut in the middle.
    # Every line in error_message/stack_trace must appear as a complete line
    # in the console log (i.e., the text before/after each \n boundary in the
    # stored field must align with a \n boundary in the log). This catches
    # truncation bugs where a line is cut mid-sentence.
    if has_test_results:
        total += 1
        truncated = []
        rows = conn.execute("""
            SELECT tr.id, tr.console_log_path, tr.error_message, tr.stack_trace
            FROM test_results tr
            WHERE tr.console_log_path IS NOT NULL AND tr.console_log_path != ''
        """).fetchall()
        checked = 0
        # Cache parsed log lines per unique path to avoid re-reading large files
        log_lines_cache = {}
        for r in rows:
            log_path = r["console_log_path"]
            if log_path not in log_lines_cache:
                if not os.path.isfile(log_path):
                    log_lines_cache[log_path] = None
                    continue
                with open(log_path, encoding="utf-8", errors="replace") as lf:
                    log_text = lf.read()
                log_lines = set()
                for line in log_text.replace("\r", "").split("\n"):
                    stripped = line.strip()
                    if stripped:
                        log_lines.add(stripped)
                log_lines_cache[log_path] = log_lines

            log_lines = log_lines_cache[log_path]
            if log_lines is None:
                continue

            for field_name, field_val in [("error_message", r["error_message"]),
                                           ("stack_trace", r["stack_trace"])]:
                if not field_val or field_val.startswith("N/A") or field_val == "?? at ??:0:0 (unresolved)":
                    continue
                for line in field_val.split("\n"):
                    stripped = line.strip()
                    if not stripped:
                        continue
                    # Skip synthetic markers added by the LLM
                    if stripped.startswith("(unresolved)"):
                        continue
                    if stripped in log_lines:
                        continue
                    # The line wasn't found as-is. Check if a log line starts
                    # with this text (the stored line may be a prefix = truncated)
                    is_prefix_of_longer = any(
                        ll.startswith(stripped) and len(ll) > len(stripped)
                        for ll in log_lines
                    )
                    if is_prefix_of_longer:
                        truncated.append((r["id"], field_name, stripped[:60]))
                        break  # one truncated line per field is enough
            checked += 1

        ok = check("error_message/stack_trace lines are complete (not truncated)",
                    len(truncated) == 0,
                    f"{len(truncated)} truncated: {truncated[:5]}" if truncated
                    else f"verified {checked} rows")
        if not ok:
            failures += 1

    # Report checks (only if --report provided)
    if args.report:
        print("\n=== Report Sanity ===")
        try:
            with open(args.report, encoding="utf-8") as f:
                report_text = f.read()

            # 16. Report file exists and is non-empty
            total += 1
            ok = check("Report file is non-empty",
                        len(report_text) > 100,
                        f"{len(report_text)} chars")
            if not ok:
                failures += 1

            # 17. Failure count in report matches DB
            total += 1
            db_failure_count = count(conn, "SELECT COUNT(*) FROM failures")
            # Try multiple heading formats used by generate_report.py
            report_groups = len(re.findall(r'^FAILURE \d+:', report_text, re.MULTILINE))
            if report_groups == 0:
                report_groups = len(re.findall(r'^### \d+\.', report_text, re.MULTILINE))
            if report_groups == 0:
                report_groups = len(re.findall(r'^## \d+\.', report_text, re.MULTILINE))
            ok = check("Failure count matches DB",
                        report_groups == db_failure_count,
                        f"report={report_groups}, db={db_failure_count}")
            if not ok:
                failures += 1

            # 18. Every failing pipeline mentioned in report
            total += 1
            failing_pipelines = conn.execute("""
                SELECT name FROM pipelines
                WHERE result = 'failed'
            """).fetchall()
            missing_in_report = [r['name'] for r in failing_pipelines
                                  if r['name'] not in report_text]
            ok = check("All failing pipelines mentioned in report",
                        len(missing_in_report) == 0,
                        f"missing: {missing_in_report}" if missing_in_report else "")
            if not ok:
                failures += 1

            # 19. Every failure section in report has a Console Log URL
            total += 1
            # Split report into failure sections using the FAILURE N: heading
            failure_sections = re.split(
                r'^-{20,}\s*\n(?=FAILURE \d+:)', report_text, flags=re.MULTILINE
            )
            # Filter to actual failure sections (skip preamble)
            failure_sections = [s for s in failure_sections
                                if re.match(r'FAILURE \d+:', s)]
            missing_console_log = []
            for section in failure_sections:
                title_match = re.match(r'(FAILURE \d+:.*)', section)
                title = title_match.group(1)[:80] if title_match else "?"
                has_console_log = bool(re.search(
                    r'\*\*Console Log:\*\*\s*'
                    r'(?:\[.*?\]\(https?://\S+\)|<?https?://\S+>?)',
                    section
                ))
                if not has_console_log:
                    missing_console_log.append(title)
            ok = check("Every failure section has Console Log URL in report",
                        len(missing_console_log) == 0,
                        f"{len(missing_console_log)} missing: "
                        + ", ".join(missing_console_log[:5])
                        + ("..." if len(missing_console_log) > 5 else "")
                        if missing_console_log else "")
            if not ok:
                failures += 1

            # 20. Report Action Items don't say [NEW] for failures with linked GitHub issue
            total += 1
            # Extract the Action Items section from the report
            action_section_match = re.search(
                r'Action Items\s*={20,}\s*\n(.*?)(?:={20,}|$)',
                report_text, re.DOTALL
            )
            action_text = action_section_match.group(1) if action_section_match else ""
            report_mismatched = []
            for r in conn.execute("""
                SELECT id, test_name, github_issue_number FROM failures
                WHERE github_issue_number IS NOT NULL
            """):
                test_name = r["test_name"]
                # Find the action item line for this test name
                for line in action_text.split("\n"):
                    if test_name[:40] in line:
                        if "[NEW]" in line or "needs issue filed" in line:
                            report_mismatched.append(
                                (r["id"], test_name[:50],
                                 f"has #{r['github_issue_number']} but report says NEW"))
                        break
            ok = check("Report Action Items consistent with DB issue mapping",
                        len(report_mismatched) == 0,
                        f"{len(report_mismatched)} mismatches: {report_mismatched}"
                        if report_mismatched else "")
            if not ok:
                failures += 1
        except FileNotFoundError:
            total += 1
            check("Report file exists", False, f"not found: {args.report}")
            failures += 1

    # Debug log checks (only if --log provided)
    if args.log:
        print("\n=== Debug Log ===")
        try:
            with open(args.log, encoding="utf-8") as f:
                log_text = f.read()

            # 20. Debug log file exists and is non-empty
            total += 1
            ok = check("Debug log file is non-empty",
                        len(log_text) > 100,
                        f"{len(log_text)} chars")
            if not ok:
                failures += 1

            # 21. Debug log contains all required step headers
            total += 1
            required_steps = [
                "Prerequisites",
                "Load Pipeline Definitions",
                "Fetch Latest Builds",
                "Extract Failed Tests",
                "Fetch Helix Console Logs",
                "Triage",
                "Validate DB",
                "Generate Report",
            ]
            missing_steps = [s for s in required_steps if s not in log_text]
            ok = check("Debug log contains all step headers",
                        len(missing_steps) == 0,
                        f"missing: {missing_steps}" if missing_steps else
                        f"all {len(required_steps)} steps present")
            if not ok:
                failures += 1

            # 22. Debug log contains SUMMARY section
            total += 1
            ok = check("Debug log contains SUMMARY section",
                        "SUMMARY" in log_text,
                        "")
            if not ok:
                failures += 1

        except FileNotFoundError:
            total += 1
            check("Debug log file exists", False, f"not found: {args.log}")
            failures += 1

    conn.close()

    # Summary
    passed = total - failures
    print(f"\n{'='*40}")
    warn_str = f", {warnings} warnings" if warnings else ""
    print(f"Results: {passed}/{total} passed, {failures} failed{warn_str}")
    if failures:
        print("❌ VALIDATION FAILED — fix issues before publishing report")
        sys.exit(1)
    else:
        print("✅ ALL CHECKS PASSED")
        sys.exit(0)


if __name__ == "__main__":
    main()

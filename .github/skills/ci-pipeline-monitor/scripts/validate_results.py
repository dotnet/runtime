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
import urllib.request
import urllib.parse


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
            WHERE p.result IN ('failed', 'partiallySucceeded')
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
        # Identify shared console_log_paths (overwrite bug — can't validate those)
        shared_paths = {r[0] for r in conn.execute("""
            SELECT console_log_path FROM test_results
            WHERE console_log_path IS NOT NULL
            GROUP BY console_log_path HAVING COUNT(*) > 1
        """).fetchall()}

        total += 1
        mismatches = []
        skipped_shared = 0
        rows = conn.execute("""
            SELECT tr.id, tr.console_log_path, tr.error_message, f.failure_category
            FROM test_results tr
            JOIN failures f ON tr.failure_id = f.id
            WHERE tr.error_message IS NOT NULL AND tr.error_message != ''
            AND tr.console_log_path IS NOT NULL AND tr.console_log_path != ''
            AND f.failure_category != 'infrastructure'
        """).fetchall()
        for r in rows:
            log_path = r["console_log_path"]
            if log_path in shared_paths:
                skipped_shared += 1
                continue  # on-disk file was overwritten — can't validate
            if not os.path.isfile(log_path):
                mismatches.append((r["id"], "log file missing"))
                continue
            with open(log_path, encoding="utf-8", errors="replace") as lf:
                log_content = lf.read()
            # Check that the first meaningful line of the error_message appears in the log.
            first_line = ""
            for line in r["error_message"].split("\n"):
                stripped = line.strip()
                if stripped:
                    first_line = stripped[:80]
                    break
            if first_line and first_line not in log_content:
                mismatches.append((r["id"], first_line[:60]))
        verified = len(rows) - skipped_shared
        ok = check("error_message matches console log",
                    len(mismatches) == 0,
                    f"{len(mismatches)} mismatches: {mismatches[:5]}" if mismatches
                    else f"verified {verified} rows, skipped {skipped_shared} shared paths")
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
        skipped_shared = 0
        for r in rows:
            log_path = r["console_log_path"]
            if log_path in shared_paths:
                skipped_shared += 1
                continue  # on-disk file was overwritten — can't validate
            if not os.path.isfile(log_path):
                continue
            with open(log_path, encoding="utf-8", errors="replace") as lf:
                log_content = lf.read()
            # Parse exit code from log: use "Command exited with N" (final line)
            # or "exit code N" from the test (not XUnitLogChecker).
            exit_matches = re.findall(
                r'(?:exit code |Command exited with )(-?\d+)', log_content
            )
            if exit_matches:
                log_exit = int(exit_matches[-1])  # last match = final exit
                db_exit = r["exit_code"]
                if db_exit is not None and db_exit != log_exit:
                    exit_mismatches.append(
                        (r["id"], f"db={db_exit} log={log_exit}")
                    )
        verified = len(rows) - skipped_shared
        ok = check("exit_code matches console log",
                    len(exit_mismatches) == 0,
                    f"{len(exit_mismatches)} mismatches: {exit_mismatches[:5]}" if exit_mismatches
                    else f"verified {verified} rows, skipped {skipped_shared} shared paths")
        if not ok:
            failures += 1

    # 15. Same failure_id → similar error pattern (no mixed root causes)
    if has_test_results:
        total += 1
        mixed_groups = []
        groups = conn.execute("""
            SELECT failure_id, COUNT(DISTINCT error_message) as distinct_msgs
            FROM test_results
            WHERE failure_id IS NOT NULL AND error_message IS NOT NULL AND error_message != ''
            GROUP BY failure_id
            HAVING distinct_msgs > 1
        """).fetchall()
        for g in groups:
            fid = g["failure_id"]
            # Fetch the distinct error messages for this group
            msgs = [r[0] for r in conn.execute(
                "SELECT DISTINCT error_message FROM test_results WHERE failure_id = ? AND error_message IS NOT NULL AND error_message != ''",
                (fid,)
            ).fetchall()]
            # Extract the first meaningful line from each distinct message
            first_lines = set()
            for m in msgs:
                for line in m.split("\n"):
                    s = line.strip()
                    if s:
                        # Normalize PID/Thread/address values for comparison
                        # so same error with different PIDs isn't flagged
                        normalized = re.sub(
                            r'PID \d+ \[0x[0-9a-fA-F]+\]', 'PID <N>',
                            s[:120]
                        )
                        normalized = re.sub(
                            r'Thread: \d+ \[0x[0-9a-fA-F]+\]', 'Thread: <N>',
                            normalized
                        )
                        normalized = re.sub(
                            r'0x[0-9a-fA-F]{6,}', '0x<ADDR>',
                            normalized
                        )
                        first_lines.add(normalized[:80])
                        break
            # If the first lines are very different, flag it
            if len(first_lines) > 1:
                # Check if they share a common prefix (at least 30 chars)
                fl_list = list(first_lines)
                common = os.path.commonprefix(fl_list)
                if len(common) < 30:
                    # Check if this failure covers multiple pipelines (cross-platform).
                    # Same GitHub issue can manifest differently on linux (SIGSEGV) vs
                    # windows (GC hole assertion). Allow inconsistency if the failure
                    # is explicitly multi-pipeline.
                    pipeline_count = conn.execute(
                        "SELECT COUNT(DISTINCT pipeline_name) FROM failure_pipelines WHERE failure_id=?",
                        (fid,)
                    ).fetchone()[0]
                    if pipeline_count <= 1:
                        mixed_groups.append(
                            (fid, [fl[:60] for fl in fl_list])
                        )
        ok = check("Same failure_id has consistent error pattern",
                    len(mixed_groups) == 0,
                    f"{len(mixed_groups)} mixed groups: {mixed_groups[:3]}" if mixed_groups else "")
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
    # (the LLM missed the match). Uses first 3 non-empty lines to capture
    # exception type AND crash site (distinguishes same-type exceptions).
    total += 1
    matched_patterns = {}
    for r in conn.execute("""
        SELECT id, github_issue_number, error_message FROM failures
        WHERE github_issue_number IS NOT NULL AND error_message IS NOT NULL
    """):
        sig_lines = []
        for line in (r["error_message"] or "").split("\n"):
            s = line.strip()
            if s:
                sig_lines.append(s[:120])
                if len(sig_lines) >= 3:
                    break
        sig = "\n".join(sig_lines)
        if sig:
            matched_patterns[sig] = r["github_issue_number"]

    suspect_new = []
    for r in conn.execute("""
        SELECT id, title, error_message FROM failures
        WHERE github_issue_number IS NULL AND error_message IS NOT NULL
    """):
        sig_lines = []
        for line in (r["error_message"] or "").split("\n"):
            s = line.strip()
            if s:
                sig_lines.append(s[:120])
                if len(sig_lines) >= 3:
                    break
        sig = "\n".join(sig_lines)
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
    total += 1
    new_failures = conn.execute("""
        SELECT id, title, test_name FROM failures
        WHERE github_issue_number IS NULL
    """).fetchall()
    missed_matches = []
    search_failures = 0
    for r in new_failures:
        test_name = r["test_name"]
        # Search GitHub using the full test name — this is the most reliable
        # way to find matching issues since issues are typically filed with
        # the exact fully-qualified test name in the title.
        q = urllib.parse.quote(test_name)
        url = (
            f"https://api.github.com/search/issues?"
            f"q={q}+repo:dotnet/runtime+is:issue&per_page=5"
        )
        found_issue = None
        try:
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
            time.sleep(0.5)  # rate limit courtesy
        except Exception as e:
            print(f"    [WARN] GitHub search failed for '{test_name}': {e}")
            search_failures += 1

        if found_issue:
            missed_matches.append(
                (r["id"], r["title"][:50], f"#{found_issue[0]} '{found_issue[1]}' ({found_issue[2]})")
            )

    searched = len(new_failures) - search_failures
    if search_failures > 0:
        detail = (f"searched {searched}/{len(new_failures)} (search failed for {search_failures})")
        if missed_matches:
            detail += f", found existing issues for {len(missed_matches)}: {missed_matches}"
        ok = check("NEW failures verified against GitHub search",
                   False, detail)
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
        for r in rows:
            log_path = r["console_log_path"]
            if not os.path.isfile(log_path):
                continue
            with open(log_path, encoding="utf-8", errors="replace") as lf:
                log_text = lf.read()
            # Build set of complete lines from the log (stripped of \r)
            log_lines = set()
            for line in log_text.replace("\r", "").split("\n"):
                stripped = line.strip()
                if stripped:
                    log_lines.add(stripped)

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
                WHERE result IN ('failed', 'partiallySucceeded')
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
            required_steps = ["STEP 1:", "STEP 2a:", "STEP 2b:", "STEP 3:", "STEP 4:", "STEP 5:"]
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
    print(f"Results: {passed}/{total} passed, {failures} failed")
    if failures:
        print("❌ VALIDATION FAILED — fix issues before publishing report")
        sys.exit(1)
    else:
        print("✅ ALL CHECKS PASSED")
        sys.exit(0)


if __name__ == "__main__":
    main()

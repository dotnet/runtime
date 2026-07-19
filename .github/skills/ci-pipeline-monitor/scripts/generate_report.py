"""CI Pipeline Monitor — Report Generator

Reads all data from a SQLite database (populated by the LLM during triage)
and prints the report following the template format exactly.

Usage:
    python generate_report.py --db monitor.db
"""

import argparse
import os
import sqlite3
import sys
from datetime import datetime


# --- Configuration ---
ADO_ORG = "dnceng-public"
ADO_PROJECT = "public"
DEFAULT_DB = "monitor.db"


class ReportGenerator:
    """Reads SQLite and prints the report following the template exactly."""

    def __init__(self, db_path, validation_warnings=False):
        if not os.path.isfile(db_path):
            print(f"Error: database {db_path} not found.", file=sys.stderr)
            sys.exit(1)
        self.conn = sqlite3.connect(db_path)
        self.conn.row_factory = sqlite3.Row
        self.validation_warnings = validation_warnings

    def generate(self):
        # Write reports to ../logs/ directory (sibling of scripts/)
        script_dir = os.path.dirname(os.path.abspath(__file__))
        logs_dir = os.path.join(os.path.dirname(script_dir), "logs")
        os.makedirs(logs_dir, exist_ok=True)
        report_name = f"test-report-{datetime.utcnow().strftime('%Y-%m-%d-%H%M%S')}.md"
        report_path = os.path.join(logs_dir, report_name)
        lines = []
        self._header(lines)
        self._pipeline_summary(lines)
        self._failure_details(lines)
        self._github_issue_summary(lines)
        self._action_items(lines)
        self._footer(lines)

        with open(report_path, 'w', encoding='utf-8') as f:
            f.write("\n".join(lines) + "\n")
        print(f"Report written to {report_path}")
        self.conn.close()
        return report_path

    # --- Sections ---

    def _header(self, out):
        cur = self.conn.cursor()
        total = cur.execute("SELECT COUNT(*) FROM pipelines").fetchone()[0]
        skipped = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result = 'skipped'"
        ).fetchone()[0]
        monitored = total - skipped

        out.append("=" * 80)
        out.append("CI Pipeline Monitor — Test Report")
        out.append(f"Date:       {datetime.utcnow().strftime('%Y-%m-%d')}")
        out.append(f"Org:        {ADO_ORG}")
        out.append(f"Project:    {ADO_PROJECT}")
        out.append(f"Branch:     refs/heads/main")
        out.append(f"Pipelines:  {total} total ({monitored} monitored, {skipped} skipped)")
        out.append("=" * 80)
        out.append("")

    def _pipeline_summary(self, out):
        cur = self.conn.cursor()

        passed_count = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result = 'succeeded'"
        ).fetchone()[0]
        failed_count = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result IN ('failed','partiallySucceeded')"
        ).fetchone()[0]
        inconclusive_count = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result = 'inconclusive'"
        ).fetchone()[0]
        monitored = passed_count + failed_count + inconclusive_count

        out.append("=" * 80)
        out.append("Pipeline Summary")
        out.append("=" * 80)
        out.append("")
        out.append(f"Result: {passed_count} PASS, {failed_count} FAIL, {inconclusive_count} INCONCLUSIVE (of {monitored} monitored)")
        out.append("")

        # Passed pipelines
        for p in cur.execute(
            "SELECT name, build_id, build_number FROM pipelines "
            "WHERE result = 'succeeded' ORDER BY name"
        ):
            bn = p["build_number"] or ""
            out.append(f"  ✅ {p['name']:<50s} {bn}")
            if p["build_id"]:
                out.append(f"     https://dev.azure.com/{ADO_ORG}/{ADO_PROJECT}/_build/results?buildId={p['build_id']}")
            out.append("")

        # Failed pipelines — with unique test names per pipeline
        failed_pipelines = cur.execute(
            "SELECT name, build_id, build_number FROM pipelines "
            "WHERE result IN ('failed','partiallySucceeded') ORDER BY name"
        ).fetchall()
        cur2 = self.conn.cursor()
        for p in failed_pipelines:
            bn = p["build_number"] or ""
            out.append(f"  ❌ {p['name']:<50s} {bn}")
            if p["build_id"]:
                out.append(f"     https://dev.azure.com/{ADO_ORG}/{ADO_PROJECT}/_build/results?buildId={p['build_id']}")

            # Unique test_names failing in this pipeline, with issue info (cap at 5 per failure group)
            seen_tests = set()
            group_counts = {}
            group_info = {}
            for row in cur2.execute("""
                SELECT DISTINCT ft.test_name, f.id as failure_id, f.github_issue_number, f.github_issue_url, f.summary
                FROM failure_tests ft
                JOIN failures f ON ft.failure_id = f.id
                WHERE ft.pipeline_name = ?
                ORDER BY f.id, ft.test_name
            """, (p["name"],)):
                tn = row["test_name"]
                if tn in seen_tests:
                    continue
                seen_tests.add(tn)
                fid = row["failure_id"]
                group_counts[fid] = group_counts.get(fid, 0) + 1
                if fid not in group_info:
                    group_info[fid] = row
                if group_counts[fid] <= 5:
                    if row["github_issue_number"]:
                        issue_url = row["github_issue_url"] or f"https://github.com/dotnet/runtime/issues/{row['github_issue_number']}"
                        out.append(f"     - [{fid}] {tn} (#{row['github_issue_number']}, {issue_url})")
                    else:
                        brief = (row["summary"] or "")[:80]
                        out.append(f"     - [{fid}] [New] {tn} ({brief})" if brief else f"     - [{fid}] [New] {tn}")
            for fid, count in group_counts.items():
                if count > 5:
                    row = group_info[fid]
                    remaining = count - 5
                    if row["github_issue_number"]:
                        issue_url = row["github_issue_url"] or f"https://github.com/dotnet/runtime/issues/{row['github_issue_number']}"
                        out.append(f"     - [{fid}] ... and {remaining} more (#{row['github_issue_number']}, {issue_url})")
                    else:
                        out.append(f"     - [{fid}] [New] ... and {remaining} more")
            out.append("")

        # Inconclusive pipelines (build failed/canceled, 0 test failures from API)
        for p in cur.execute(
            "SELECT name, build_id, build_number, skip_reason FROM pipelines "
            "WHERE result = 'inconclusive' ORDER BY name"
        ):
            bn = p["build_number"] or ""
            reason = p["skip_reason"] or "unknown"
            if p["build_id"]:
                url = f"https://dev.azure.com/{ADO_ORG}/{ADO_PROJECT}/_build/results?buildId={p['build_id']}"
                out.append(f"  ⚠️ [{p['name']} {bn}]({url}): INCONCLUSIVE ({reason})")
            else:
                out.append(f"  ⚠️ {p['name']}: INCONCLUSIVE ({reason})")

        # Skipped pipelines (private/intentional opt-out — never fetched)
        for p in cur.execute(
            "SELECT name, skip_reason FROM pipelines WHERE result = 'skipped' ORDER BY name"
        ):
            reason = p["skip_reason"] or "unknown"
            out.append(f"  ⏭️ {p['name']}: SKIPPED ({reason})")

        out.append("")
        out.append("Notes:")
        out.append("- ✅ = all tests passed")
        out.append("- ❌ = one or more test failures")
        out.append("- ⚠️ = inconclusive (build failed but no test failures detected via Test Results API)")
        out.append("- ⏭️ = skipped (see reason per pipeline above)")
        out.append("- [New] = no matching GitHub issue found — may need a new issue filed.")
        out.append("")

    def _failure_details(self, out):
        cur = self.conn.cursor()

        out.append("=" * 80)
        out.append("Failure Details")
        out.append("=" * 80)
        out.append("")
        out.append("The following unique test failures were found across all failing pipelines.")
        out.append("Each failure category is listed once with all affected pipelines.")
        out.append("")

        for fail in cur.execute("SELECT * FROM failures ORDER BY id"):
            self._one_failure(out, fail)

    def _one_failure(self, out, fail):
        cur = self.conn.cursor()

        out.append("-" * 80)
        scope = fail["scope"]
        if scope:
            title_line = f"FAILURE {fail['id']}: {fail['title']} ({scope})"
        else:
            title_line = f"FAILURE {fail['id']}: {fail['title']}"
        out.append(title_line)
        out.append("-" * 80)

        # GitHub issue line
        if fail["github_issue_number"]:
            issue_url = fail['github_issue_url'] or f"https://github.com/dotnet/runtime/issues/{fail['github_issue_number']}"
            out.append(
                f"GitHub Issue: #{fail['github_issue_number']} "
                f"({issue_url}) — {fail['github_issue_state']}"
            )
            if fail["github_issue_assigned"]:
                out.append(f"Assigned to: @{fail['github_issue_assigned']}")
        else:
            out.append("GitHub Issue: NEW — needs issue filed")

        if fail["labels"]:
            out.append(f"Labels: {fail['labels']}")

        out.append(f"Failing since: {fail['failing_since_date']} (build {fail['failing_since_build']})")
        out.append("")
        out.append(f"Work item: {fail['work_item']}")
        out.append("")

        # Title / Labels / Milestone for issue filing
        test_name = fail["test_name"]
        out.append(f"Title: Test Failure: {test_name}")
        out.append(f"Labels: {fail['labels'] or ''}")
        out.append(f"Milestone: {fail['milestone']}")
        out.append("")

        # --- Body block ---
        out.append("Body (paste as-is into GitHub issue):")
        out.append("<<<")

        # Summary
        out.append("**Summary:**")
        out.append(f"  {fail['summary']}")
        out.append("")

        # Failed in — JOIN with pipelines to guarantee build_id/build_number
        affected = list(cur.execute(
            "SELECT fp.pipeline_name, "
            "COALESCE(fp.build_id, p.build_id) AS build_id, "
            "COALESCE(fp.build_number, p.build_number) AS build_number "
            "FROM failure_pipelines fp "
            "LEFT JOIN pipelines p ON fp.pipeline_name = p.name "
            "WHERE fp.failure_id = ? ORDER BY fp.pipeline_name",
            (fail["id"],)
        ))
        out.append(f"**Failed in ({len(affected)}):**")
        for ap in affected:
            bn = ap["build_number"] or ""
            out.append(
                f"- [{ap['pipeline_name']} {bn}]"
                f"(https://dev.azure.com/{ADO_ORG}/{ADO_PROJECT}/_build/results?buildId={ap['build_id']})"
            )
        out.append("")

        # Console Log and Source
        if fail["console_log_url"]:
            out.append(f"**Console Log:** [Console Log]({fail['console_log_url']})")
            # Show which test result the error_message/stack_trace came from
            if fail["source_test_result_id"]:
                src = cur.execute(
                    """SELECT tr.pipeline_name, tr.build_id, tr.run_name, tr.test_name
                       FROM test_results tr WHERE tr.id = ?""",
                    (fail["source_test_result_id"],)
                ).fetchone()
                if src:
                    src_url = f"https://dev.azure.com/{ADO_ORG}/{ADO_PROJECT}/_build/results?buildId={src['build_id']}&view=ms.vss-test-web.build-test-results-tab"
                    out.append(f"**Source:** [{src['pipeline_name']} / {src['run_name']} / {src['test_name']}]({src_url})")
            out.append("")

        # Failed tests — group by pipeline if 2+ pipelines
        out.append("**Failed tests:**")
        out.append("```")

        if len(affected) >= 2:
            for ap in affected:
                out.append(ap["pipeline_name"])
                for ft in cur.execute(
                    "SELECT DISTINCT run_name FROM failure_tests "
                    "WHERE failure_id = ? AND pipeline_name = ? ORDER BY run_name",
                    (fail["id"], ap["pipeline_name"])
                ):
                    out.append(f"- {ft['run_name']}")
        else:
            for ft in cur.execute(
                "SELECT DISTINCT run_name FROM failure_tests WHERE failure_id = ? ORDER BY run_name",
                (fail["id"],)
            ):
                out.append(f"- {ft['run_name']}")

        # Unique test names (cap at 5)
        test_names = [row["test_name"] for row in cur.execute(
            "SELECT DISTINCT test_name FROM failure_tests WHERE failure_id = ? ORDER BY test_name",
            (fail["id"],)
        )]
        for tn in test_names[:5]:
            out.append(f"  - {tn}")
        if len(test_names) > 5:
            out.append(f"  - ... and {len(test_names) - 5} more")
        out.append("```")
        out.append("")

        # Error message
        out.append("**Error Message:**")
        out.append("```")
        out.append(fail["error_message"] or "N/A")
        out.append("```")
        out.append("")

        # Stack trace
        out.append("**Stack Trace:**")
        out.append("```")
        out.append(fail["stack_trace"] or "N/A")
        out.append("```")
        out.append("")

        # Analysis
        out.append("**Analysis:**")
        out.append(fail["analysis"] or "<TODO: Analysis>")

        out.append(">>>")
        out.append("")

    def _github_issue_summary(self, out):
        """Generate GitHub Issue Summary from the failures table (source of truth).

        Failures with github_issue_number are listed as known issues.
        Failures without are listed as [New].
        """
        cur = self.conn.cursor()

        out.append("=" * 80)
        out.append("GitHub Issue Summary")
        out.append("=" * 80)
        out.append("")

        # Collect unique issues from failures table (deduplicate by issue number)
        seen_issues = set()
        known = []
        new_failures = []

        for f in cur.execute("""
            SELECT f.id, f.title, f.test_name, f.github_issue_number,
                   f.github_issue_url, f.github_issue_state, f.github_issue_assigned,
                   f.labels,
                   (SELECT COUNT(DISTINCT pipeline_name) FROM failure_pipelines WHERE failure_id = f.id) AS pipe_count
            FROM failures f ORDER BY f.id
        """):
            if f["github_issue_number"]:
                if f["github_issue_number"] not in seen_issues:
                    seen_issues.add(f["github_issue_number"])
                    known.append(f)
            else:
                new_failures.append(f)

        for row in known:
            url = row["github_issue_url"] or f"https://github.com/dotnet/runtime/issues/{row['github_issue_number']}"
            out.append(f"  #{row['github_issue_number']} ({url})")
            out.append(f"    {row['title']}")
            assigned = f" | Assigned: @{row['github_issue_assigned']}" if row["github_issue_assigned"] else ""
            out.append(f"    State: {row['github_issue_state'] or 'OPEN'}{assigned} | Pipelines affected: {row['pipe_count']}")
            out.append("")

        for row in new_failures:
            out.append(f"  [New] Test Failure: {row['test_name']}")
            out.append(f"    No matching GitHub issue found. Affects {row['pipe_count']} pipeline(s).")
            if row["labels"]:
                out.append(f"    Suggested labels: {row['labels']}")
            out.append("")

    def _action_items(self, out):
        """Generate Action Items — things a human needs to act on.

        Sub-sections:
        1. Issues to File — NEW failures needing a GitHub issue
        2. High Impact Failures — known issues affecting many pipelines or legs
        3. Needs Review — unresolved validation warnings (if applicable)
        """
        cur = self.conn.cursor()

        out.append("=" * 80)
        out.append("Action Items")
        out.append("=" * 80)
        out.append("")

        rows = cur.execute("""
            SELECT f.id, f.title, f.test_name, f.github_issue_number,
                   f.github_issue_url, f.failure_category,
                   (SELECT COUNT(*) FROM test_results WHERE failure_id = f.id) AS test_count,
                   (SELECT COUNT(DISTINCT pipeline_name) FROM failure_pipelines WHERE failure_id = f.id) AS pipe_count
            FROM failures f
            ORDER BY
                (SELECT COUNT(DISTINCT pipeline_name) FROM failure_pipelines WHERE failure_id = f.id) DESC,
                (SELECT COUNT(*) FROM test_results WHERE failure_id = f.id) DESC,
                f.id
        """).fetchall()

        new_issues = [r for r in rows if not r["github_issue_number"]]
        # High impact: affects >1 pipeline OR >5 individual test results
        high_impact = [r for r in rows
                       if r["github_issue_number"]
                       and (r["pipe_count"] > 1 or r["test_count"] > 5)]

        # Section 1: Issues to file
        out.append("### 🆕 Issues to File")
        out.append("")
        if new_issues:
            for row in new_issues:
                counts = f"{row['test_count']} test(s), {row['pipe_count']} pipeline(s)"
                out.append(f"- [NEW] {row['test_name']} ({counts})")
                out.append(f"  See FAILURE {row['id']} above for pre-formatted issue body.")
                out.append("")
        else:
            out.append("None — all failures matched to existing GitHub issues.")
            out.append("")

        # Section 2: High impact failures
        if high_impact:
            out.append("### 🔥 High Impact Failures")
            out.append("")
            for row in high_impact:
                counts = f"{row['test_count']} test(s), {row['pipe_count']} pipeline(s)"
                url = row["github_issue_url"] or f"https://github.com/dotnet/runtime/issues/{row['github_issue_number']}"
                out.append(f"- {row['test_name']} — #{row['github_issue_number']} ({counts})")
                out.append(f"  {url}")
                out.append("")

        # Section 3: Needs review (validation warnings)
        if self.validation_warnings:
            out.append("### 👁️ Needs Review")
            out.append("")
            out.append("- Unresolved validation warnings — see debug log for details.")
            out.append("")

    def _footer(self, out):
        out.append("=" * 80)
        out.append("End of report. Generated by ci-pipeline-monitor skill.")
        out.append("=" * 80)


def main():
    parser = argparse.ArgumentParser(
        description="CI Pipeline Monitor — report generator"
    )
    parser.add_argument("--db", default=DEFAULT_DB, help=f"Database path (default: {DEFAULT_DB})")
    parser.add_argument("--validation-warnings", action="store_true",
                        help="Show a Needs Review section in Action Items for unresolved validation warnings")
    args = parser.parse_args()

    gen = ReportGenerator(args.db, validation_warnings=args.validation_warnings)
    gen.generate()


if __name__ == "__main__":
    main()

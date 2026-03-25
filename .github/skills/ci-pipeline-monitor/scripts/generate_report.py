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

    def __init__(self, db_path):
        if not os.path.isfile(db_path):
            print(f"Error: database {db_path} not found.", file=sys.stderr)
            sys.exit(1)
        self.conn = sqlite3.connect(db_path)
        self.conn.row_factory = sqlite3.Row

    def generate(self):
        # Write reports to ../logs/ directory (sibling of scripts/)
        script_dir = os.path.dirname(os.path.abspath(__file__))
        logs_dir = os.path.join(os.path.dirname(script_dir), "logs")
        os.makedirs(logs_dir, exist_ok=True)
        report_name = f"test-report-{datetime.utcnow().strftime('%Y-%m-%d-%H%M%S')}.md"
        report_path = os.path.join(logs_dir, report_name)
        lines = []
        self._header(lines)
        self._data_collection_warnings(lines)
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
        monitored = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result != 'skipped'"
        ).fetchone()[0]
        skipped = total - monitored
        passed = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result = 'succeeded'"
        ).fetchone()[0]
        failed = monitored - passed

        out.append("=" * 80)
        out.append("CI Pipeline Monitor — Test Report")
        out.append(f"Date:       {datetime.utcnow().strftime('%Y-%m-%d')}")
        out.append(f"Org:        {ADO_ORG}")
        out.append(f"Project:    {ADO_PROJECT}")
        out.append(f"Branch:     refs/heads/main")
        out.append(f"Pipelines:  {total} total ({monitored} monitored, {skipped} skipped)")
        out.append("=" * 80)
        out.append("")

    def _data_collection_warnings(self, out):
        """Show warnings for any data collection errors (timeouts, failed requests)."""
        cur = self.conn.cursor()
        # Check if the table exists (older DBs may not have it)
        table_exists = cur.execute(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='data_collection_errors'"
        ).fetchone()[0]
        if not table_exists:
            return

        errors = cur.execute(
            "SELECT step, pipeline_name, build_id, error_type, detail FROM data_collection_errors ORDER BY id"
        ).fetchall()
        if not errors:
            return

        out.append("=" * 80)
        out.append("Data Collection Warnings")
        out.append("=" * 80)
        out.append("")
        out.append("The following errors occurred during data collection. Affected pipelines")
        out.append("may have incomplete or missing failure details in this report.")
        out.append("")
        for e in errors:
            step_label = "Test extraction" if e[0] == "extract_tests" else "Log download"
            out.append(f"  \u26a0\ufe0f {step_label}: {e[1]} (build {e[2]}) \u2014 {e[3]}: {e[4]}")
        out.append("")

    def _pipeline_summary(self, out):
        cur = self.conn.cursor()

        passed_count = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result = 'succeeded'"
        ).fetchone()[0]
        failed_count = cur.execute(
            "SELECT COUNT(*) FROM pipelines WHERE result IN ('failed','partiallySucceeded')"
        ).fetchone()[0]
        monitored = passed_count + failed_count

        out.append("=" * 80)
        out.append("Pipeline Summary")
        out.append("=" * 80)
        out.append("")
        out.append(f"Result: {passed_count} PASS, {failed_count} FAIL (of {monitored} monitored)")
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

            # Unique test_names failing in this pipeline, with issue info
            seen_tests = set()
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
                if row["github_issue_number"]:
                    issue_url = row["github_issue_url"] or f"https://github.com/dotnet/runtime/issues/{row['github_issue_number']}"
                    out.append(f"     - [{fid}] {tn} (#{row['github_issue_number']}, {issue_url})")
                else:
                    brief = (row["summary"] or "")[:80]
                    out.append(f"     - [{fid}] [New] {tn} ({brief})" if brief else f"     - [{fid}] [New] {tn}")
            out.append("")

        # Skipped pipelines
        for p in cur.execute(
            "SELECT name, skip_reason FROM pipelines WHERE result = 'skipped' ORDER BY name"
        ):
            reason = p["skip_reason"] or "private"
            out.append(f"  ⏭️ {p['name']}: SKIPPED ({reason})")

        out.append("")
        out.append("Notes:")
        out.append("- ✅ = all tests passed")
        out.append("- ❌ = one or more test failures")
        out.append("- ⏭️ = skipped (private pipeline or marked skip)")
        out.append("- EVERY pipeline (✅ and ❌) must include the build URL on the line after the name.")
        out.append("- List ALL failing tests per pipeline — deduplicate by test name (show each unique test once).")
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

        # Console Log
        if fail["console_log_url"]:
            out.append(f"**Console Log:** [Console Log]({fail['console_log_url']})")
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

        # Unique test names
        for tn_row in cur.execute(
            "SELECT DISTINCT test_name FROM failure_tests WHERE failure_id = ? ORDER BY test_name",
            (fail["id"],)
        ):
            tn = tn_row["test_name"]
            out.append(f"  - {tn}")
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
        """Generate Action Items from the failures table (source of truth).

        Each failure becomes an action item. Failures with a linked GitHub
        issue say "Monitor/update existing issue #N"; failures without say
        "File new GitHub issue". Uses the full test_name from the DB.
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
                (SELECT COUNT(*) FROM test_results WHERE failure_id = f.id) DESC,
                f.id
        """).fetchall()

        for i, row in enumerate(rows, 1):
            test_name = row["test_name"]
            counts = f"{row['test_count']} test(s), {row['pipe_count']} pipeline(s)"

            if row["github_issue_number"]:
                url = row["github_issue_url"] or f"https://github.com/dotnet/runtime/issues/{row['github_issue_number']}"
                out.append(f"{i}. {test_name} — #{row['github_issue_number']} ({counts})")
                out.append(f"   {url}")
            else:
                out.append(f"{i}. [NEW] {test_name} — needs issue filed ({counts})")
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
    args = parser.parse_args()

    gen = ReportGenerator(args.db)
    gen.generate()


if __name__ == "__main__":
    main()

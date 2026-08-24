"""CI Pipeline Monitor — Issue Generator

Generates GitHub issues and comments from the contents of the ci pipeline monitor's database.
You must have done `gh repo set-default` at least once on the current checkout for this to work.

Usage (dry run):
    python update_github.py --db monitor.db
Usage (generate issues and comments):
    python update_github.py --db monitor.db --go
"""

import argparse
import os
import sqlite3
import sys
import tempfile
import subprocess

# --- Configuration ---
ADO_ORG = "dnceng-public"
ADO_PROJECT = "public"
DEFAULT_DB = "monitor.db"
STAMPS_DIR = os.path.join(__file__, "..", "stamps")

class IssueGenerator:
    def __init__(self, db_path):
        if not os.path.isfile(db_path):
            print(f"Error: database {db_path} not found.", file=sys.stderr)
            sys.exit(1)
        self.conn = sqlite3.connect(db_path)
        self.conn.row_factory = sqlite3.Row

    def generate(self, go):
        self.probe_configuration()
        self.generate_issues(go)
        self.conn.close()

    def get_stamp_path(self, ap):
        if not os.path.isdir(STAMPS_DIR):
            os.makedirs(STAMPS_DIR)
        return os.path.join(STAMPS_DIR, f"{ap['build_id']}.stamp")

    def probe_configuration(self):
        probe_result = subprocess.run(["gh", "repo", "set-default", "-v"], check=True, capture_output=True)
        if (len(probe_result.stderr)):
            raise Exception("You need to perform gh repo set-default")

    def generate_issues(self, go):
        cur = self.conn.cursor()
        error_count = 0

        for fail in cur.execute("SELECT * FROM failures ORDER BY id"):
            try:
                self._one_failure(fail, go)
            except Exception as exc:
                error_count += 1
                print(f"Failed processing failure #{fail['id']}; continuing: {exc}")

        if error_count:
            raise Exception("An error occurred, see output above")

    def _one_failure(self, fail, go):
        cur = self.conn.cursor()
        out = []

        scope = fail["scope"]
        if scope:
            title_line = f"FAILURE {fail['id']}: {fail['title']} ({scope})"
        else:
            title_line = f"FAILURE {fail['id']}: {fail['title']}"
        print(f"--- {title_line} ---")

        gh_issue_command = ["gh", "issue"]
        creating_new_issue = False

        # GitHub issue line
        if fail["github_issue_number"]:
            issue_url = fail['github_issue_url'] or f"https://github.com/dotnet/runtime/issues/{fail['github_issue_number']}"
            print(
                f"GitHub Issue: #{fail['github_issue_number']} "
                f"({issue_url}) — {fail['github_issue_state']}"
            )
            gh_issue_command.append('comment')
            gh_issue_command.append(issue_url)
        else:
            print("GitHub Issue: NEW — creating new issue")
            gh_issue_command.append("create")
            creating_new_issue = True

        if fail["labels"]:
            if creating_new_issue:
                for label in fail["labels"].split(','):
                    stripped_label = label.strip()
                    if stripped_label:
                        gh_issue_command.append('--label')
                        gh_issue_command.append(stripped_label)

        # Title / Labels / Milestone for issue filing
        test_name = fail["test_name"]
        if creating_new_issue:
            gh_issue_command.append('--title')
            gh_issue_command.append(f'Test Failure: {test_name}')
            if fail['milestone']:
                gh_issue_command.append('--milestone')
                gh_issue_command.append(fail['milestone'])

        # --- Body block ---
        # Summary
        if creating_new_issue:
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

        # Filter affected list to build IDs that don't have stamp files
        affected = [ap for ap in affected if not os.path.exists(self.get_stamp_path(ap))]

        if len(affected) == 0:
            print("All affected builds have stamps; skipping.")
            return

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

        if creating_new_issue:
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

        out.append("")
        out.append("**Generated by ci-pipeline-monitor/scripts/update_github.py**")

        temp_file = tempfile.NamedTemporaryFile('w', encoding="utf8", delete=False, newline='\n')
        body_path = temp_file.name
        temp_file.write("\n".join(out))
        temp_file.flush()
        temp_file.close()
        gh_issue_command.append("--body-file")
        gh_issue_command.append(body_path)

        print(gh_issue_command)
        if (go):
            subprocess.run(gh_issue_command, check=True)
            for ap in affected:
                with open(self.get_stamp_path(ap), "w") as stamp_file:
                    stamp_file.write("ok")
        else:
            print(f"--- {body_path} ---")
            for line in out:
                print(line)
            print(f"--- END {body_path} ---")

        print("")
        print("")

        # NOTE: This isn't in a try/finally because we want it to be possible to manually
        #  retry a failed command invocation after script failure for troubleshooting
        try:
            os.unlink(body_path)
        except OSError:
            # ignore, failures to unlink the temp file due to it being locked by virus scanner etc are unimportant
            return

def main():
    parser = argparse.ArgumentParser(
        description="CI Pipeline Monitor — GitHub updater"
    )
    parser.add_argument("--db", default=DEFAULT_DB, help=f"Database path (default: {DEFAULT_DB})")
    parser.add_argument("--go", action="store_true", help=f"Actually file issues and comments instead of performing a dry run (default: False)")
    args = parser.parse_args()

    gen = IssueGenerator(args.db)
    gen.generate(args.go)


if __name__ == "__main__":
    main()

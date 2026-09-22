#!/usr/bin/env python3

import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


WORKFLOWS = Path(__file__).resolve().parents[1]
SEARCH_SCRIPT = WORKFLOWS / "shared/search-kbe.sh"


def issue(number=42, **overrides):
    return {
        "number": number,
        "title": "[ci-scan] Test failure",
        "state": "open",
        "user": {"login": "github-actions[bot]", "type": "Bot"},
        "labels": [{"name": "Known Build Error"}],
        "html_url": f"https://github.com/dotnet/runtime/issues/{number}",
        **overrides,
    }


def response(items=None, **overrides):
    items = [] if items is None else items
    return {
        "items": items,
        "total_count": len(items),
        "incomplete_results": False,
        **overrides,
    }


class SearchTests(unittest.TestCase):
    def run_search(self, value, *, fail=False, raw=False, status="candidates",
                   kind="issues", query='is:issue is:open "Missing BBF_PROF_WEIGHT flag"'):
        with tempfile.TemporaryDirectory() as directory:
            temp = Path(directory)
            fixture = value if raw else json.dumps(value)
            (temp / "fixture").write_text(fixture)
            output = temp / "lookup"
            output.mkdir()
            (output / "summary.json").write_text('{"status":"no_match"}')
            env = {
                **os.environ,
                "TEST_DIRECTORY": directory,
                "TEST_FAIL": "1" if fail else "0",
            }
            mock = """
github() {
    printf '%s\\0' "$@" > "$TEST_DIRECTORY/arguments"
    cat > "$TEST_DIRECTORY/request.json"
    cat "$TEST_DIRECTORY/fixture"
    if [ "$TEST_FAIL" = "1" ]; then
        echo "Simulated search failure" >&2
        return 1
    fi
}
"""
            result = subprocess.run(
                ["bash", "-c", mock + SEARCH_SCRIPT.read_text(), "search-kbe",
                 kind, query, str(output)],
                env=env, capture_output=True, text=True,
            )
            self.assertEqual(
                (temp / "arguments").read_text().split("\0")[:-1],
                ["search_issues" if kind == "issues" else "search_pull_requests", "."],
            )
            request = json.loads((temp / "request.json").read_text())
            self.assertEqual(request["owner"], "dotnet")
            self.assertEqual(request["repo"], "runtime")
            self.assertEqual(
                request["fields"],
                ["number", "title", "state", "user", "labels", "html_url"],
            )
            self.assertEqual(request["perPage"], 100)
            self.assertIn("repo:dotnet/runtime", request["query"])
            self.assertIn('"Missing BBF_PROF_WEIGHT flag"', request["query"])
            self.assertEqual((output / "response.json").read_text(), fixture)
            summary = json.loads((output / "summary.json").read_text())
            self.assertEqual(summary["status"], status, result.stderr)
            self.assertEqual(json.loads(result.stdout), summary)
            if status == "blocked":
                self.assertNotEqual(result.returncode, 0)
                self.assertTrue(summary["reason"])
                self.assertTrue(result.stderr)
            else:
                self.assertEqual(result.returncode, 0, result.stderr)
            return summary

    def test_preserves_candidate_metadata(self):
        candidates = [issue(134292), issue(134169)]
        summary = self.run_search(response(candidates))
        self.assertEqual(summary["items"], candidates)
        self.assertEqual(summary["total_count"], 2)

    def test_only_complete_empty_response_is_no_match(self):
        summary = self.run_search(response(), status="no_match")
        self.assertEqual(summary["items"], [])

    def test_uses_pull_request_search_for_fix_queries(self):
        for state in ("open", "closed"):
            with self.subTest(state=state):
                candidate = issue(state=state, html_url="https://github.com/dotnet/runtime/pull/42")
                summary = self.run_search(
                    response([candidate]), kind="pull-requests",
                    query=f'is:pr is:{state} "Missing BBF_PROF_WEIGHT flag"',
                )
                self.assertEqual(summary["items"], [candidate])

    def test_filtered_results_never_become_no_match(self):
        for value in (
            response(["[Filtered]"]),
            response([issue(), "[Filtered]"]),
            response([], filtered="[Filtered]"),
            {"content": [{"type": "text", "text": "[Filtered]"}]},
            response([], warning="[DIFC-FILTERED]"),
        ):
            with self.subTest(value=value):
                summary = self.run_search(value, status="blocked")
                self.assertEqual(summary["reason"], "integrity-filtered candidate")

    def test_accepts_mcp_text_envelope(self):
        for value in (response(), response([issue()])):
            with self.subTest(value=value):
                summary = self.run_search(
                    {"content": [{"type": "text", "text": json.dumps(value)}]},
                    status="candidates" if value["items"] else "no_match",
                )
                self.assertEqual(summary["items"], value["items"])

    def test_accepts_cli_proxy_content_array(self):
        for value in (response(), response([issue()])):
            with self.subTest(value=value):
                summary = self.run_search(
                    [value],
                    status="candidates" if value["items"] else "no_match",
                )
                self.assertEqual(summary["items"], value["items"])
        self.run_search([response(["[Filtered]"])], status="blocked")
        self.run_search([response(), response()], status="blocked")

    def test_blocks_incomplete_and_malformed_results(self):
        for value in (
            response(incomplete_results=True),
            response(total_count=1),
            response([issue()], total_count=1000),
            response([issue(user=None)]),
            response([issue(user={})]),
            response([issue(number="42")]),
            response([issue(labels=None)]),
            {"items": []},
            {"message": "API rate limit exceeded"},
            {"isError": True, "content": [{"type": "text", "text": "Request failed"}]},
            {"content": []},
            {"content": [{"type": "text", "text": "not JSON"}]},
            None,
            [],
        ):
            with self.subTest(value=value):
                self.run_search(value, status="blocked")

    def test_blocks_non_json_and_multiple_documents(self):
        for value in ("", "<html>Sign in</html>", "{}\n{}", "null\n", "truncated {"):
            with self.subTest(value=value):
                self.run_search(value, raw=True, status="blocked")

    def test_tool_failure_cannot_reuse_previous_no_match(self):
        self.run_search(response(), fail=True, status="blocked")

    def test_workflow_uses_guarded_searches(self):
        workflow = (WORKFLOWS / "ci-failure-scan.md").read_text()
        self.assertIn("  cli-proxy: true\n", workflow)
        self.assertIn(".github/workflows/shared/search-kbe.sh", workflow)
        tally = workflow.split("Recognized values:", 1)[1]
        self.assertIn("`lookup incomplete, needs human review`", tally)
        instructions = (WORKFLOWS / "shared/create-kbe.instructions.md").read_text()
        self.assertIn("If any lookup returns a `[Filtered]` marker", instructions)
        self.assertNotIn("linked-tracker: integrity-filtered", instructions)

    def test_eval_uses_production_github_server(self):
        prefix = "# gh-aw-manifest: "
        manifest = next(
            line[len(prefix):]
            for line in (WORKFLOWS / "ci-failure-scan.lock.yml").read_text().splitlines()
            if line.startswith(prefix)
        )
        image_prefix = "ghcr.io/github/github-mcp-server:"
        production_image = next(
            container["pinned_image"]
            for container in json.loads(manifest)["containers"]
            if container["image"].startswith(image_prefix)
        )
        eval_image = next(
            line.strip()[2:]
            for line in (WORKFLOWS / "evals/ci-failure-scan.eval.yaml").read_text().splitlines()
            if line.strip().startswith("- " + image_prefix)
        )
        self.assertEqual(eval_image, production_image)

    def test_invalid_arguments_fail_before_lookup(self):
        for args in (
            [],
            [""],
            ["issues", "", "/tmp/unused-kbe-search"],
            ["issues", "query", ""],
            ["invalid", "query", "/tmp/unused-kbe-search"],
        ):
            with self.subTest(args=args):
                result = subprocess.run(
                    ["bash", str(SEARCH_SCRIPT), *args],
                    capture_output=True, text=True,
                )
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("Usage:", result.stderr)


if __name__ == "__main__":
    unittest.main()

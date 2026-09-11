#!/usr/bin/env python3

import json
import os
from pathlib import Path
import subprocess
import tempfile
import textwrap
import unittest


WORKFLOW = Path(__file__).resolve().parents[1] / "ci-failure-fix.md"


def candidate_script(workflow):
    # Exercise the embedded script directly so fixtures cannot drift from production.
    lines = workflow.read_text().splitlines()
    start = next(i for i, line in enumerate(lines) if "name: Filter scanner-authored KBEs" in line)
    start = next(i for i in range(start, len(lines)) if lines[i].strip() == "run: |") + 1
    indent = len(lines[start]) - len(lines[start].lstrip())
    end = start
    while end < len(lines) and (not lines[end].strip() or lines[end].startswith(" " * indent)):
        end += 1
    return textwrap.dedent("\n".join(lines[start:end])) + "\n"


def issue(number, **overrides):
    return {
        "number": number,
        "created_at": "2026-01-01T00:00:00Z",
        "state": "open",
        "title": "[ci-scan] Test failure",
        "user": {"login": "github-actions[bot]", "type": "Bot"},
        "labels": [{"name": "Known Build Error"}],
        "body": "This body must not be passed to the agent by the intake job.",
        **overrides,
    }


class CandidateTests(unittest.TestCase):
    def run_filter(self, pages, fail=False):
        with tempfile.TemporaryDirectory() as directory:
            temp = Path(directory)
            fixtures = temp / "pages.json"
            fixtures.write_text("\n".join(json.dumps(page) for page in pages))
            output = temp / "outputs"
            env = {
                **os.environ,
                "RUNNER_TEMP": directory,
                "GITHUB_OUTPUT": str(output),
                "TEST_PAGES": str(fixtures),
                "TEST_FAIL": "1" if fail else "0",
            }
            mock = """
gh() {
    printf '%s\\0' "$@" > "$RUNNER_TEMP/arguments"
    while [ "$#" -gt 0 ]; do
        if [ "$1" = "--jq" ]; then
            shift
            jq -c "$1" "$TEST_PAGES" || return
            if [ "$TEST_FAIL" = "1" ]; then
                echo "Simulated API failure after a page" >&2
                return 1
            fi
            return 0
        fi
        shift
    done
    return 2
}
"""
            result = subprocess.run(
                ["bash", "-c", mock + candidate_script(WORKFLOW)],
                env=env, capture_output=True, text=True,
            )
            args = (temp / "arguments").read_text().split("\0")[:-1]
            self.assertEqual(args[:-1], [
                "api", "--method", "GET", "repos/dotnet/runtime/issues",
                "-f", "state=open", "-f", "labels=Known Build Error",
                "-f", "sort=created", "-f", "direction=asc", "-f", "per_page=100",
                "--paginate", "--jq",
            ])
            if fail:
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("Simulated API failure", result.stderr)
                self.assertFalse(output.exists())
                return

            self.assertEqual(result.returncode, 0, result.stderr)
            outputs = dict(line.split("=", 1) for line in output.read_text().splitlines())
            candidates = json.loads(outputs["candidates"])["candidates"]
            self.assertEqual(int(outputs["count"]), len(candidates))
            self.assertEqual(
                json.loads((temp / "scanner-kbe-candidates.json").read_text()),
                {"candidates": candidates},
            )
            return candidates

    def test_filters_metadata_and_preserves_oldest_first(self):
        candidates = self.run_filter([
            [
                issue(20, created_at="2026-09-01T00:00:00Z"),
                issue(8, user={"login": "maintainer", "type": "User"}),
                issue(9, user={"login": "other[bot]", "type": "Bot"}),
                issue(10, user={"login": "github-actions[bot]", "type": "User"}),
                issue(11, labels=[{"name": "Known Build Error optional"}]),
                issue(12, title="A [ci-scan] mention is not a prefix"),
                issue(13, title="[ci-scan-feedback] Not a scanner issue"),
                issue(14, pull_request={"url": "https://api.github.com/repos/dotnet/runtime/pulls/14"}),
                issue(15, state="closed"),
                issue(16, user=None),
            ],
            [issue(3), issue(2)],
        ])
        self.assertEqual(candidates, [
            {"number": 2, "created_at": "2026-01-01T00:00:00Z"},
            {"number": 3, "created_at": "2026-01-01T00:00:00Z"},
            {"number": 20, "created_at": "2026-09-01T00:00:00Z"},
        ])

    def test_empty_and_non_scanner_pages(self):
        for pages in ([[]], [[], []], [[issue(1, user=None)]]):
            with self.subTest(pages=pages):
                self.assertEqual(self.run_filter(pages), [])

    def test_candidates_beyond_one_thousand_issues(self):
        issues = [issue(n, user={"login": "maintainer", "type": "User"}) for n in range(1000)]
        pages = [issues[start:start + 100] for start in range(0, len(issues), 100)]
        pages.append([issue(1001)])
        self.assertEqual(self.run_filter(pages), [
            {"number": 1001, "created_at": "2026-01-01T00:00:00Z"},
        ])

    def test_api_failure_does_not_publish_partial_candidates(self):
        self.run_filter([[issue(1)]], fail=True)

    def test_compiled_script_and_agent_gate(self):
        lock = WORKFLOW.with_suffix(".lock.yml")
        self.assertEqual(candidate_script(lock), candidate_script(WORKFLOW))
        agent = lock.read_text().split("\n  agent:\n", 1)[1].split("\n  conclusion:\n", 1)[0]
        self.assertIn("      - scanner_kbes\n", agent)
        self.assertIn("(needs.scanner_kbes.outputs.count > 0)", agent)
        self.assertIn("KBE_CANDIDATES: ${{ needs.scanner_kbes.outputs.candidates }}", agent)
        self.assertNotIn("Start DIFC Proxy", agent)
        self.assertIn("GH_AW_APPROVAL_LABELS_EXTRA: Known Build Error", agent)


if __name__ == "__main__":
    unittest.main()

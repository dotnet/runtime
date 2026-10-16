# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

import os
import re
import shutil
import subprocess
import textwrap
import unittest
from pathlib import Path


WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.md"
COMPILED_WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.lock.yml"
POOL_TOKEN_EXPRESSION = "${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}"


class AuthenticationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")
        cls.compiled_workflow = COMPILED_WORKFLOW.read_text(encoding="utf-8")

    def test_copilot_inference_uses_pat_pool(self):
        engine = self._section("engine:", "checkout:")

        self.assertIn(
            "imports:\n"
            "  - uses: shared/pat_pool.md\n"
            "    with:\n"
            "      environment: copilot-pat-pool",
            self.workflow,
        )
        self.assertIn("\nenvironment: copilot-pat-pool\n", self.workflow)
        self.assertIn(f"COPILOT_GITHUB_TOKEN: {POOL_TOKEN_EXPRESSION}", engine)
        self.assertIn("secrets.COPILOT_GITHUB_TOKEN", engine)

    def test_pat_pool_token_is_inference_only(self):
        repository_mutations = self.workflow.split("safe-outputs:", 1)[1]

        self.assertEqual(1, self.workflow.count(POOL_TOKEN_EXPRESSION))
        self.assertNotIn("COPILOT_GITHUB_TOKEN", repository_mutations)
        self.assertNotIn("COPILOT_PAT_", repository_mutations)

    def test_copilot_requests_write_is_absent(self):
        self.assertNotIn("copilot-requests: write", self.workflow)

    def test_source_access_uses_workflow_token(self):
        tools = self._section("tools:", "network:")

        self.assertIn("github-token: ${{ secrets.GITHUB_TOKEN }}", tools)
        self.assertNotIn("github-app:", tools)
        self.assertIn("GH_TOKEN: ${{ github.token }}", self.workflow)
        self.assertIn("github-token: ${{ github.token }}", self._step("Publish trusted source outcome"))

    def test_docs_app_tokens_do_not_request_source_repository(self):
        for match in re.finditer(r"repositories:(?: \S+|\s*\n(?: {8,}\S+\n)+)", self.workflow):
            repositories = match.group(0)
            self.assertNotRegex(repositories, r"(?im)^\s*runtime\s*$")

    def test_publisher_identity_is_checked_before_inference_and_publication(self):
        for name in ("Resolve source version and existing docs draft", "Reject inconsistent docs mutations"):
            step = self._step(name)
            self.assertIn('test "${DOCS_BOT_APP_SLUG}" = "runtime-docs-bot"', step)
            self.assertIn("DOCS_BOT_APP_SLUG:", step)

    def test_publication_shell_rejects_other_apps_before_calling_validator(self):
        script = textwrap.dedent(
            self._step("Reject inconsistent docs mutations").split("run: |\n", 1)[1],
        )
        # Mock only the external validator; execute the workflow's actual shell guard.
        script = "python3() { printf 'validator-called\\n'; }\n" + script
        for slug in ("runtime-docs-bot", "other-app", "runtime-docs-bot-other", ""):
            with self.subTest(slug=slug):
                result = subprocess.run(
                    [shutil.which("bash"), "-c", script],
                    capture_output=True, text=True,
                    env={
                        **os.environ, "DOCS_BOT_APP_SLUG": slug, "GH_AW_AGENT_OUTPUT": "output.json",
                        "EXPECTED_SOURCE_REPOSITORY": "dotnet/runtime", "EXPECTED_SOURCE_PR_NUMBER": "42",
                        "RUNNER_TEMP": ".",
                    },
                )
                self.assertEqual(0 if slug == "runtime-docs-bot" else 1, result.returncode, result.stderr)
                self.assertEqual("validator-called\n" if slug == "runtime-docs-bot" else "", result.stdout)

    def test_docs_operations_keep_app_authentication(self):
        self.assertIn("repository: dotnet/docs", self.workflow)
        self.assertIn("repositories: [\"docs\"]", self.workflow)
        self.assertIn("repositories: [\"docs.Automation\"]", self.workflow)
        self.assertIn("DOCS_GITHUB_TOKEN: ${{ steps.docs-bot-token.outputs.token }}", self.workflow)
        self.assertIn(
            '--allowed-author "${DOCS_BOT_APP_SLUG}[bot]"',
            self.workflow,
        )
        self.assertIn(
            "github-token: ${{ steps.docs-bot-token.outputs.token }}",
            self._step("Notify source author on docs pull request"),
        )

    def test_pr_creation_token_covers_upstream_and_head_repository(self):
        safe_outputs_app = self._section("safe-outputs:", "  report-failure-as-issue:")

        self.assertIn(
            'repositories: ["docs", "docs.Automation"]',
            safe_outputs_app,
        )

        compiled_app_tokens = self.compiled_workflow.split(
            "id: safe-outputs-app-token"
        )[1:]
        self.assertTrue(compiled_app_tokens)
        for compiled_app_token in compiled_app_tokens:
            self.assertIn(
                "          repositories: |-\n"
                "            docs\n"
                "            docs.Automation\n",
                compiled_app_token.split("      - name:", 1)[0],
            )

    def test_source_notification_has_least_privilege_write_permissions(self):
        job = self._section("notify-source-pr:", "pre-agent-steps:")

        self.assertIn("contents: read", job)
        self.assertIn("issues: write", job)
        self.assertIn("pull-requests: write", job)
        self.assertNotIn("COPILOT_GITHUB_TOKEN", job)
        self.assertNotIn("COPILOT_PAT_", job)
        self.assertIn("github-token: ${{ github.token }}", job)

    def _section(self, start, end):
        _, section = self.workflow.split(start, 1)
        section, _ = section.split(end, 1)
        return section

    def _step(self, name):
        marker = f"- name: {name}"
        _, step = self.workflow.split(marker, 1)
        next_step = re.search(r"^(?:\s*- name: |  [a-z][a-z-]*:)", step, re.MULTILINE)
        return step[: next_step.start()] if next_step else step


if __name__ == "__main__":
    unittest.main()

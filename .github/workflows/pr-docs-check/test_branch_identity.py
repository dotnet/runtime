# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

import copy
import json
import tempfile
import unittest
from pathlib import Path

from find_existing_draft import find_existing_draft
from prepare_run import prepare_run
from test_prepare_context import changed_file, pull_request
import test_validate_outcome as fixtures
from validate_outcome import OutcomeValidationError, build_outcome, validate_live_draft, validate_preflight


class BranchIdentityTests(unittest.TestCase):
    def setUp(self):
        self.metadata = fixtures.ValidateOutcomeTests._metadata(9, "docs/runtime-pr42")
        self.metadata.update(title="Human editorial title", labels=[], body="Human editorial description")
        self.metadata["head"]["sha"] = "a" * 40

    def resolve(self, pulls):
        return find_existing_draft(
            pulls, "dotnet/runtime", 42, "dotnet/docs",
            "dotnet/docs.Automation", "runtime-docs-bot[bot]",
        )

    def test_editorial_metadata_does_not_control_recognition(self):
        for field, value in (("title", "Human title"), ("labels", []), ("body", None)):
            with self.subTest(field=field):
                metadata = fixtures.ValidateOutcomeTests._metadata(9, "docs/runtime-pr42")
                metadata[field] = value
                result = self.resolve([metadata])
                self.assertTrue(result["found"])
                self.assertFalse(result["blocked"])
                self.assertEqual(9, result["selected"]["number"])

    def test_skip_and_non_draft_refresh_do_not_fetch_analysis_inputs(self):
        for mode, draft in (("skip", True), ("skip", False), ("refresh", False)):
            with self.subTest(mode=mode, draft=draft), tempfile.TemporaryDirectory() as directory:
                metadata = {**self.metadata, "draft": draft}
                reads = []

                def source_api(endpoint):
                    reads.append(endpoint)
                    self.assertNotIn("/files?", endpoint)
                    return pull_request()

                result = prepare_run(
                    "dotnet/runtime", "42", Path(directory), mode, "runtime-docs-bot[bot]",
                    source_api, lambda endpoint: [metadata] if "?" in endpoint else metadata,
                )
                self.assertFalse(result["analyze"])
                self.assertIn(metadata["html_url"], result["summary"])
                self.assertEqual(["/repos/dotnet/runtime/pulls/42"], reads)
                self.assertFalse((Path(directory) / "files.json").exists())

    def test_ambiguous_open_branches_never_select_a_mutation_target(self):
        second = copy.deepcopy(self.metadata)
        second.update(number=10, html_url="https://github.com/dotnet/docs/pull/10")
        second["head"]["ref"] = "docs/runtime-pr42-abcd"
        result = self.resolve([self.metadata, second])
        self.assertFalse(result["found"])
        self.assertTrue(result["blocked"])
        self.assertEqual("ambiguous_open_pull_requests", result["blocked_reason"])
        for mode in ("skip", "refresh"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as directory:
                def prepare():
                    return prepare_run(
                        "dotnet/runtime", "42", Path(directory), mode, "runtime-docs-bot[bot]",
                        lambda endpoint: pull_request() if "/files?" not in endpoint else self.fail("No inference inputs"),
                        lambda endpoint: [self.metadata, second] if "?" in endpoint else self.metadata,
                    )

                if mode == "refresh":
                    with self.assertRaisesRegex(ValueError, "ambiguous_open_pull_requests"):
                        prepare()
                else:
                    result = prepare()
                    self.assertFalse(result["analyze"])
                    self.assertIn(self.metadata["html_url"], result["summary"])
                    self.assertIn(second["html_url"], result["summary"])

    def test_exact_source_branch_and_legacy_suffix(self):
        for branch, matches in (
            ("docs/runtime-pr42", True), ("docs/runtime-pr42-abcd0123", True),
            ("docs/runtime-pr420", False), ("docs/runtime-pr42-extra", False),
            ("docs/runtime-pr42-abcd/extra", False),
        ):
            with self.subTest(branch=branch):
                metadata = copy.deepcopy(self.metadata)
                metadata["head"]["ref"] = branch
                self.assertEqual(matches, self.resolve([metadata])["found"])

    def test_closed_history_is_not_an_existing_open_pr(self):
        for state in ("closed", "merged"):
            with self.subTest(state=state):
                result = self.resolve([{**self.metadata, "state": state}])
                self.assertFalse(result["found"])
                self.assertFalse(result["blocked"])

    def test_refresh_push_preserves_editorial_metadata(self):
        draft = {"found": True, "blocked": False, "selected": {"number": 9, "head_sha": "a" * 40}}
        payload = fixtures.ValidateOutcomeTests._payload(
            "drafted", "updated", {"type": "push_to_pull_request_branch", "pull_request_number": 9},
            existing_docs_pr_number=9,
        )
        self.metadata["head"]["sha"] = "b" * 40
        before = copy.deepcopy(self.metadata)
        validate_preflight(payload, 42, draft)
        outcome = build_outcome(
            payload, "dotnet/runtime", 42, "", self.metadata, draft, "runtime-docs-bot[bot]",
        )
        self.assertEqual("updated", outcome["docs_pr_action"])
        self.assertEqual(before, self.metadata)

    def test_unchanged_refresh_accepts_editorial_metadata_with_clean_git_evidence(self):
        draft = {"found": True, "blocked": False, "selected": {"number": 9, "head_sha": "a" * 40}}
        payload = fixtures.ValidateOutcomeTests._payload(
            "drafted", "unchanged", {"type": "noop"}, existing_docs_pr_number=9,
        )
        proof = {"base_sha": "a" * 40, "head_sha": "a" * 40, "clean": True}
        validate_preflight(payload, 42, draft, workspace_evidence=proof)
        outcome = build_outcome(
            payload, "dotnet/runtime", 42, "", self.metadata, draft,
            "runtime-docs-bot[bot]", workspace_evidence=proof,
        )
        self.assertEqual("unchanged", outcome["docs_pr_action"])

    def test_live_publication_rechecks_author_draft_topology_and_ambiguity(self):
        expected = self.resolve([self.metadata])
        validate_live_draft(expected, [self.metadata], "dotnet/runtime", 42, "runtime-docs-bot[bot]")
        for field, value in (
            ("draft", False), ("user", {"login": "human"}),
            ("base", {**self.metadata["base"], "ref": "release/11.0"}),
            ("head", {**self.metadata["head"], "sha": "b" * 40}),
            ("head", {**self.metadata["head"], "ref": "docs/runtime-pr420"}),
            ("head", {**self.metadata["head"], "repo": {"full_name": "someone/docs"}}),
        ):
            with self.subTest(field=field, value=value):
                with self.assertRaises(OutcomeValidationError):
                    validate_live_draft(
                        expected, [{**self.metadata, field: value}],
                        "dotnet/runtime", 42, "runtime-docs-bot[bot]",
                    )
        absent = {"found": False, "blocked": False}
        with self.assertRaisesRegex(OutcomeValidationError, "identity changed"):
            validate_live_draft(absent, [self.metadata], "dotnet/runtime", 42, "runtime-docs-bot[bot]")
        with self.assertRaisesRegex(OutcomeValidationError, "must not be modified or replaced"):
            validate_live_draft(
                expected, [self.metadata, self.metadata],
                "dotnet/runtime", 42, "runtime-docs-bot[bot]",
            )

    def test_orphan_branch_and_closed_history_do_not_claim_an_existing_pr(self):
        for pulls in ([], [{**self.metadata, "state": "closed"}]):
            with self.subTest(pulls=pulls), tempfile.TemporaryDirectory() as directory:
                reads = []

                def docs_api(endpoint):
                    reads.append(endpoint)
                    self.assertIn("/pulls?", endpoint)
                    return pulls

                result = prepare_run(
                    "dotnet/runtime", "42", Path(directory), "skip", "runtime-docs-bot[bot]",
                    lambda endpoint: [changed_file("src/Foo.cs")] if "/files?" in endpoint else pull_request(),
                    docs_api,
                )
                self.assertTrue(result["analyze"])
                self.assertEqual("", result["summary"])
                self.assertFalse(json.loads((Path(directory) / "existing-draft.json").read_text())["found"])
                self.assertEqual(1, len(reads))

    def test_wrong_base_cannot_create_a_replacement(self):
        metadata = copy.deepcopy(self.metadata)
        metadata["base"]["ref"] = "release/11.0"
        result = self.resolve([metadata])
        self.assertTrue(result["blocked"])
        self.assertEqual("matching_pull_request_has_wrong_base", result["blocked_reason"])
        for mode in ("skip", "refresh"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as directory:
                with self.assertRaisesRegex(OutcomeValidationError, "target main"):
                    prepare_run(
                        "dotnet/runtime", "42", Path(directory), mode, "runtime-docs-bot[bot]",
                        lambda endpoint: pull_request(),
                        lambda endpoint: [metadata] if "?" in endpoint else metadata,
                    )

    def test_updated_requires_changed_remote_head_not_just_a_push_request(self):
        draft = self.resolve([self.metadata])
        payload = fixtures.ValidateOutcomeTests._payload(
            "drafted", "updated", {"type": "push_to_pull_request_branch", "pull_request_number": 9},
            existing_docs_pr_number=9,
        )
        with self.assertRaisesRegex(OutcomeValidationError, "real push"):
            build_outcome(
                payload, "dotnet/runtime", 42, "", self.metadata, draft, "runtime-docs-bot[bot]",
            )

    def test_create_preflight_rejects_other_source_branches(self):
        for branch in ("docs/runtime-pr420", "docs/runtime-pr42-extra", "main", None):
            with self.subTest(branch=branch):
                payload = fixtures.ValidateOutcomeTests._payload(
                    "drafted", "created", {"type": "create_pull_request", "branch": branch},
                )
                with self.assertRaisesRegex(OutcomeValidationError, "exact source-specific"):
                    validate_preflight(payload, 42, {"found": False, "blocked": False})


if __name__ == "__main__":
    unittest.main()

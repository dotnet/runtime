# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

import json
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from find_existing_draft import find_existing_draft
from resolve_target_version import VersionResolutionError, resolve_target_version


POLICY = {
    "schemaVersion": 1,
    "mainVersion": {"value": "12.0", "description": "main"},
    "releaseBranchPattern": {
        "value": r"^release/(?P<version>[0-9]+\.0)$",
        "description": "release",
    },
    "milestoneVersionPattern": {
        "value": r"^(?P<version>[0-9]+\.0)(?:\b|[- ].*)",
        "description": "milestone",
    },
    "docsMonikerPrefix": {"value": "net-", "description": "moniker"},
    "docsBaseBranch": {"value": "main", "description": "docs base"},
}


class ResolveTargetVersionTests(unittest.TestCase):
    def test_checked_in_policy_matches_runtime_development_version(self):
        helpers = Path(__file__).parent
        policy = json.loads((helpers / "version-policy.json").read_text(encoding="utf-8"))
        versions = ET.parse(helpers.parents[2] / "eng" / "Versions.props").getroot()
        major = versions.findtext(".//MajorVersion")
        minor = versions.findtext(".//MinorVersion")
        self.assertEqual(f"{major}.{minor}", policy["mainVersion"]["value"])
        self.assertTrue(versions.findtext(".//ProductVersion").startswith(f"{major}.{minor}."))
        result = resolve_target_version(
            policy, {"base": {"ref": "main"}}, [f"release/{int(major) - 1}.0"],
        )
        self.assertEqual(f"net-{major}.{minor}", result["docs_moniker"])
        self.assertEqual("main", result["docs_base_branch"])

    def test_main_uses_verified_configured_version(self):
        result = resolve_target_version(
            POLICY,
            {"base": {"ref": "main"}, "milestone": {"title": "12.0-preview1"}},
            ["release/10.0", "release/11.0", "other"],
        )

        self.assertEqual("12.0", result["target_version"])
        self.assertEqual(">= net-12.0", result["docs_moniker_range"])
        self.assertEqual("docs/core/porting", result["migration_directory"])
        self.assertEqual("docs/core/compatibility", result["breaking_changes_directory"])
        self.assertEqual("12.0", result["breaking_changes_version"])
        self.assertEqual("docs/core/whats-new/dotnet-12", result["release_notes_directory"])
        self.assertTrue(result["milestone_corroborated"])

    def test_release_branch_resolves_exact_version(self):
        result = resolve_target_version(
            POLICY,
            {"base": {"ref": "release/11.0"}, "milestone": None},
            ["release/10.0", "release/11.0"],
        )

        self.assertEqual("11.0", result["target_version"])
        self.assertEqual("source_release_branch", result["resolution"])

    def test_main_policy_must_match_release_branches(self):
        with self.assertRaisesRegex(VersionResolutionError, "expected 13.0"):
            resolve_target_version(
                POLICY,
                {"base": {"ref": "main"}, "milestone": None},
                ["release/12.0"],
            )

    def test_milestone_mismatch_fails(self):
        with self.assertRaisesRegex(VersionResolutionError, "resolves to 11.0"):
            resolve_target_version(
                POLICY,
                {"base": {"ref": "main"}, "milestone": {"title": "11.0"}},
                ["release/11.0"],
            )


class FindExistingDraftTests(unittest.TestCase):
    def test_multiple_matching_drafts_block_refresh(self):
        pulls = [
            self._pull(1, "2026-01-01T00:00:00Z", "docs/runtime-pr42-abcd"),
            self._pull(2, "2026-02-01T00:00:00Z", "docs/runtime-pr42"),
        ]

        result = find_existing_draft(
            pulls,
            "dotnet/runtime",
            42,
            "dotnet/docs",
            "dotnet/docs.Automation",
            "runtime-docs-bot[bot]",
        )

        self.assertFalse(result["found"])
        self.assertTrue(result["blocked"])
        self.assertEqual("ambiguous_open_pull_requests", result["blocked_reason"])
        self.assertEqual([2], [pull["number"] for pull in result["other_matching_pull_requests"]])

    def test_ignores_untrusted_or_non_draft_pull_requests(self):
        pulls = [
            self._pull(1, "2026-01-01T00:00:00Z", "docs/runtime-pr42", draft=False),
            self._pull(2, "2026-01-01T00:00:00Z", "docs/runtime-pr42", head_repo="someone/docs"),
        ]

        result = find_existing_draft(
            pulls,
            "dotnet/runtime",
            42,
            "dotnet/docs",
            "dotnet/docs.Automation",
            "runtime-docs-bot[bot]",
        )

        self.assertFalse(result["found"])
        self.assertTrue(result["blocked"])

    def test_ignores_branch_for_another_source_pr(self):
        result = find_existing_draft(
            [self._pull(1, "2026-01-01T00:00:00Z", "docs/runtime-pr99")],
            "dotnet/runtime",
            42,
            "dotnet/docs",
            "dotnet/docs.Automation",
            "runtime-docs-bot[bot]",
        )

        self.assertFalse(result["found"])
        self.assertFalse(result["blocked"])

    def test_ignores_same_repository_and_other_fork_heads(self):
        pulls = [
            self._pull(
                1,
                "2026-01-01T00:00:00Z",
                "docs/runtime-pr42",
                head_repo="dotnet/docs",
            ),
            self._pull(
                2,
                "2026-01-01T00:00:00Z",
                "docs/runtime-pr42",
                head_repo="someone/docs",
            ),
        ]

        result = find_existing_draft(
            pulls,
            "dotnet/runtime",
            42,
            "dotnet/docs",
            "dotnet/docs.Automation",
            "runtime-docs-bot[bot]",
        )

        self.assertFalse(result["found"])
        self.assertFalse(result["blocked"])

    @staticmethod
    def _pull(
        number,
        updated_at,
        head_ref,
        *,
        draft=True,
        head_repo="dotnet/docs.Automation",
    ):
        return {
            "number": number,
            "html_url": f"https://github.com/dotnet/docs/pull/{number}",
            "state": "open",
            "draft": draft,
            "updated_at": updated_at,
            "body": "Source: dotnet/runtime#42\n\nDetails",
            "title": "[docs] Update docs",
            "base": {
                "ref": "main",
                "repo": {"full_name": "dotnet/docs"},
            },
            "head": {"ref": head_ref, "repo": {"full_name": head_repo}},
            "user": {"login": "runtime-docs-bot[bot]"},
            "labels": [{"name": "documentation"}],
        }


if __name__ == "__main__":
    unittest.main()

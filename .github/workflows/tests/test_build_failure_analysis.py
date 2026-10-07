import copy
import io
import json
import os
from pathlib import Path
import shutil
import stat
import struct
import subprocess
import tempfile
import unittest
import zipfile
import zlib

import yaml


WORKFLOWS = Path(os.environ.get("BFA_WORKFLOW_DIR", Path(__file__).resolve().parents[1]))
NAMES = ("build-failure-analysis", "build-failure-analysis-command")
HEAD = "a" * 40
MERGE = "b" * 40
COMMAND = "/analyze-build-failure"


def load_workflow(name):
    lines = (WORKFLOWS / (name + ".md")).read_text(encoding="utf-8").splitlines()
    return yaml.safe_load("\n".join(lines[1:lines.index("---", 1)]))


def step(steps, step_id):
    return next(item for item in steps if item.get("id") == step_id)


def safe_output_steps(workflow):
    if "steps" in workflow["safe-outputs"]:
        return workflow["safe-outputs"]["steps"]
    return load_workflow("shared/build-failure-analysis-shared")["safe-outputs"]["steps"]


def archive_bytes(name, mode, content=b"binlog", extra=b""):
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w") as archive:
        entry = zipfile.ZipInfo(name)
        entry.create_system = 3
        entry.external_attr = mode << 16
        entry.extra = extra
        archive.writestr(entry, content)
    return buffer.getvalue()


def asi_unix_extra(mode, size):
    attributes = struct.pack("<HIHH", mode, size, 0, 0)
    data = struct.pack("<I", zlib.crc32(attributes) & 0xFFFFFFFF) + attributes
    return struct.pack("<HH", 0x756E, len(data)) + data


def classic_max_entries_archive():
    content = io.BytesIO()
    with zipfile.ZipFile(content, "w", allowZip64=True) as archive:
        for index in range(65535):
            archive.writestr(f"{index:04x}", b"")
    data = content.getvalue()
    eocd_offset = data.rfind(b"PK\x05\x06")
    eocd = struct.unpack_from("<4s4H2LH", data, eocd_offset)
    if eocd[3:5] != (0xFFFF, 0xFFFF) or data[eocd_offset - 20 : eocd_offset].startswith(b"PK\x06\x07"):
        raise AssertionError("fixture is not a classic 65,535-entry archive")
    return data


def rewrite_eocd(content, **updates):
    data = bytearray(content)
    offset = data.rfind(b"PK\x05\x06")
    values = list(struct.unpack_from("<4s4H2LH", data, offset))
    indexes = {
        "disk_number": 1,
        "central_directory_disk": 2,
        "entries_on_disk": 3,
        "entry_count": 4,
        "central_directory_size": 5,
        "central_directory_offset": 6,
    }
    for name, value in updates.items():
        values[indexes[name]] = value
    struct.pack_into("<4s4H2LH", data, offset, *values)
    return bytes(data)


def zip64_archive(content, classic_sentinels=True, **updates):
    eocd_offset = content.rfind(b"PK\x05\x06")
    (
        _,
        disk_number,
        central_directory_disk,
        entries_on_disk,
        entry_count,
        central_directory_size,
        central_directory_offset,
        comment_length,
    ) = struct.unpack_from("<4s4H2LH", content, eocd_offset)
    if comment_length:
        raise ValueError("fixture helper requires an archive without an EOCD comment")
    values = {
        "disk_number": disk_number,
        "central_directory_disk": central_directory_disk,
        "entries_on_disk": entries_on_disk,
        "entry_count": entry_count,
        "central_directory_size": central_directory_size,
        "central_directory_offset": central_directory_offset,
    }
    values.update(updates)
    zip64_eocd = struct.pack(
        "<4sQ2H2L4Q",
        b"PK\x06\x06",
        44,
        45,
        45,
        values["disk_number"],
        values["central_directory_disk"],
        values["entries_on_disk"],
        values["entry_count"],
        values["central_directory_size"],
        values["central_directory_offset"],
    )
    locator = struct.pack("<4sLQL", b"PK\x06\x07", 0, eocd_offset, 1)
    eocd = (
        struct.pack(
            "<4s4H2LH",
            b"PK\x05\x06",
            0,
            0,
            0xFFFF,
            0xFFFF,
            0xFFFFFFFF,
            0xFFFFFFFF,
            0,
        )
        if classic_sentinels
        else content[eocd_offset:]
    )
    return content[:eocd_offset] + zip64_eocd + locator + eocd


class BuildFailureAnalysisTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflows = {name: load_workflow(name) for name in NAMES}
        cls.bash = os.environ.get("BFA_BASH") or (
            r"C:\Program Files\Git\bin\bash.exe" if os.name == "nt" else shutil.which("bash")
        )
        if not cls.bash:
            raise RuntimeError("Bash is required.")
        probe = subprocess.run([cls.bash, "-c", "command -v jq"], capture_output=True, text=True)
        if probe.returncode:
            raise RuntimeError("jq is required on PATH.")

    def run_script(self, script, env=None, files=None):
        with tempfile.TemporaryDirectory(prefix="bfa-test-") as directory:
            root = Path(directory)
            path_value = os.environ.get("PATH", "")
            if os.name == "nt":
                python3 = root / "python3"
                python3.write_text('#!/usr/bin/env bash\nexec python "$@"\n', encoding="utf-8")
                python3.chmod(0o755)
                path_value = str(root) + os.pathsep + path_value
            for name, content in (files or {}).items():
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                if isinstance(content, bytes):
                    path.write_bytes(content)
                else:
                    path.write_text(content, encoding="utf-8")
            result = subprocess.run(
                [self.bash, "--noprofile", "--norc", "-eo", "pipefail", "-s"],
                input=script,
                text=True,
                capture_output=True,
                cwd=root,
                env={
                    **os.environ,
                    "PATH": path_value,
                    "GITHUB_OUTPUT": "outputs",
                    "GITHUB_ENV": "agent-env",
                    "RUNNER_TEMP": ".",
                    "GH_AW_REPO": "dotnet/runtime",
                    "GITHUB_REPOSITORY": "dotnet/runtime",
                    "ADO_API": "https://dev.azure.com/dnceng-public/public/_apis",
                    **(env or {}),
                },
                timeout=90,
            )
            outputs = (root / "outputs").read_text() if (root / "outputs").exists() else ""
            agent_env = (root / "agent-env").read_text() if (root / "agent-env").exists() else ""
            return result, outputs, agent_env

    def test_custom_bash_syntax(self):
        for name, workflow in self.workflows.items():
            steps = workflow["jobs"]["fetch-binlog"]["steps"] + workflow["steps"] + safe_output_steps(workflow)
            for item in steps:
                if "run" not in item:
                    continue
                with self.subTest(workflow=name, step=item["name"]):
                    result = subprocess.run([self.bash, "-n"], input=item["run"], text=True, capture_output=True)
                    self.assertEqual(result.returncode, 0, result.stderr)

    def test_command_gate_matches_generated_activation(self):
        workflow = self.workflows[NAMES[1]]
        script = step(workflow["jobs"]["fetch-binlog"]["steps"], "perm")["run"]
        lock = yaml.safe_load((WORKFLOWS / (NAMES[1] + ".lock.yml")).read_text(encoding="utf-8"))
        activation = lock["jobs"]["activation"]["if"]
        for suffix in (" ", "\n"):
            self.assertIn("startsWith(github.event.comment.body, '" + COMMAND + suffix + "')", activation)
        self.assertIn("github.event.comment.body == '" + COMMAND + "'", activation)
        bodies = [
            "", COMMAND, COMMAND + " arguments", COMMAND + "\narguments",
            " " + COMMAND, "\n" + COMMAND, "\r\n" + COMMAND, "mention " + COMMAND,
            "> " + COMMAND, COMMAND + "-now", COMMAND + ".", COMMAND.upper(),
            COMMAND + "\targuments", COMMAND + "\r\narguments", COMMAND + "\r",
            COMMAND + "\v", COMMAND + "\f", COMMAND + "\u00a0", COMMAND + "\u2028",
            COMMAND + " \r\narguments", COMMAND + "\n\targuments",
        ]
        for body in bodies:
            with self.subTest(body=repr(body)):
                result, outputs, _ = self.run_script(
                    'gh() { printf \'%s\' \'{"permission":"write"}\'; }\n' + script,
                    {"COMMENT_BODY": body, "COMMAND_NAME": "analyze-build-failure", "COMMENTER": "maintainer"},
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                expected = body == COMMAND or body.startswith(COMMAND + " ") or body.startswith(COMMAND + "\n")
                self.assertIn("authorized=" + str(expected).lower(), outputs)

    def test_command_permissions(self):
        script = step(self.workflows[NAMES[1]]["jobs"]["fetch-binlog"]["steps"], "perm")["run"]
        for permission, allowed in (("admin", True), ("write", True), ("read", False), ("none", False), ("", False)):
            with self.subTest(permission=permission):
                _, outputs, _ = self.run_script(
                    'gh() { printf \'%s\' "$PERMISSION"; }\n' + script,
                    {
                        "COMMENT_BODY": COMMAND, "COMMAND_NAME": "analyze-build-failure",
                        "COMMENTER": "maintainer", "PERMISSION": json.dumps({"permission": permission}),
                    },
                )
                self.assertIn("authorized=" + str(allowed).lower(), outputs)

    def test_command_queue_and_request_identity(self):
        workflow = self.workflows[NAMES[1]]
        self.assertIs(workflow["concurrency"]["cancel-in-progress"], False)
        self.assertEqual(workflow["concurrency"].get("queue"), "max")
        self.assertIn("[Request](${{ github.event.comment.html_url }})", workflow["safe-outputs"]["messages"]["footer"])
        self.assertEqual(workflow["run-name"], "Build failure analysis command ${{ github.event.comment.id }}")

    def test_partial_publication_summary_does_not_complete_command(self):
        script = step(self.workflows[NAMES[1]]["jobs"]["fetch-binlog"]["steps"], "fetch")["run"].split("# --- Scope check:")[0]
        url = "https://github.com/dotnet/runtime/pull/42#issuecomment-123"
        body = 'Structured data:\n```json\n{"workflow_artifact":"build-failure-analysis","artifact_kind":"analysis"}\n```\n[Request](' + url + ")"
        result, _, _ = self.run_script(
            'gh() { printf \'%s\' "$COMMENTS"; }\n' + script + "\necho proceed=true\n",
            {
                "PR_NUMBER": "42", "COMMAND_URL": url,
                "COMMENTS": json.dumps([[{"user": {"login": "github-actions[bot]"}, "body": body}]]),
            },
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("proceed=true", result.stdout)

    def test_fork_resolution_and_revision_guards(self):
        script = step(self.workflows[NAMES[0]]["jobs"]["fetch-binlog"]["steps"], "fetch")["run"]
        script = script[script.index("# --- 1. Resolve"):script.index("# --- 5. Download")]
        build = {
            "result": "failed", "definition": {"id": 129}, "sourceBranch": "refs/pull/42/merge",
            "sourceVersion": MERGE, "triggerInfo": {"pr.number": "42", "pr.sourceSha": HEAD},
        }
        pr = {"base": {"ref": "main"}, "head": {"sha": HEAD}, "merge_commit_sha": MERGE}
        cases = (
            ("fork metadata", {}, {}, {}, True),
            ("fork merge-ref fallback", {"triggerInfo": {"pr.sourceSha": HEAD}}, {}, {}, True),
            ("event PR", {}, {}, {"CHECK_PR_NUMBER": "42"}, True),
            ("dispatch", {}, {}, {"EVENT_NAME": "workflow_dispatch"}, True),
            ("missing event SHA", {}, {}, {"CHECK_HEAD_SHA": ""}, False),
            ("different event SHA", {}, {}, {"CHECK_HEAD_SHA": "c" * 40}, False),
            ("different event SHA with PR", {}, {}, {"CHECK_PR_NUMBER": "42", "CHECK_HEAD_SHA": "c" * 40}, False),
            ("missing source SHA", {"triggerInfo": {"pr.number": "42"}}, {}, {}, False),
            ("wrong PR", {"triggerInfo": {"pr.number": "43", "pr.sourceSha": HEAD}}, {}, {}, False),
            ("non-PR build", {"sourceBranch": "refs/heads/main"}, {}, {}, False),
            ("wrong definition", {"definition": {"id": 130}}, {}, {}, False),
            ("successful build", {"result": "succeeded"}, {}, {}, False),
            ("stale head", {}, {"head": {"sha": "c" * 40}}, {}, False),
            ("stale merge", {}, {"merge_commit_sha": "c" * 40}, {}, False),
            ("missing merge", {}, {"merge_commit_sha": None}, {}, False),
            ("out of scope", {}, {"base": {"ref": "feature"}}, {}, False),
            ("multiline build ID", {}, {}, {"EVENT_NAME": "workflow_dispatch", "DISPATCH_BUILD_ID": "9\n10"}, False),
            ("multiline PR", {}, {}, {"EVENT_NAME": "workflow_dispatch", "DISPATCH_PR_NUMBER": "42\n43"}, False),
        )
        for name, build_changes, pr_changes, env_changes, valid in cases:
            with self.subTest(name=name):
                result, _, _ = self.run_script(
                    "set +e\nemit_none() { echo rejected; exit 0; }\n"
                    'ado_get() { ADO_DOC="$BUILD"; }\ngh() { printf \'%s\' "$PR"; }\n'
                    + script + '\nprintf "validated=%s\\n" "$PR_NUMBER"\n',
                    {
                        "BUILD": json.dumps({**build, **build_changes}), "PR": json.dumps({**pr, **pr_changes}),
                        "EVENT_NAME": "check_run", "CHECK_PR_NUMBER": "", "CHECK_HEAD_SHA": HEAD,
                        "CHECK_DETAILS_URL": "https://dev.azure.com/dnceng-public/public/_build/results?buildId=9",
                        "DISPATCH_BUILD_ID": "9", "DISPATCH_PR_NUMBER": "42", "ADO_BUILD_DEFINITION_ID": "129",
                        **env_changes,
                    },
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual("validated=42" in result.stdout, valid, result.stdout)

    def test_artifact_matching_uses_producing_job_identity(self):
        cases = (
            ([("Foo-Bar", "failed"), ("Foo_Bar", "succeeded")], [("Foo-Bar", "0"), ("Foo_Bar", "1")], ["Foo-Bar"]),
            ([("Foo-Bar", "failed"), ("Foo_Bar", "succeeded")], [("Foo-Bar", "1")], []),
            ([("Foo-Bar", "failed"), ("Foo_Bar", "succeeded")], [("Foo_Bar", "0")], ["Foo_Bar"]),
            ([("Foo-Bar", "failed")], [("FOOBAR", "0")], ["FOOBAR"]),
            ([("Foo minijit", "failed")], [("Foo", "0")], ["Foo"]),
            ([("NativeAOT_Libraries", "failed")], [("NativeAOT", "unknown")], []),
            ([("NativeAOT", "succeeded"), ("NativeAOT_Libraries", "failed")], [("NativeAOT", "0"), ("NativeAOT_Libraries", "1")], ["NativeAOT_Libraries"]),
            ([("Foo", "canceled")], [("Attempt2_Foo", "0")], ["Attempt2_Foo"]),
            ([("Foo", "failed"), ("Foo", "succeeded")], [("Foo", "1")], []),
            ([("Foo", "succeeded")], [("Foo", "0")], []),
            ([("Foo", "failed")], [("Foo", None)], []),
            ([("Foo", "failed")], [("Foo", "")], []),
            ([("Foo", "failed")], [("Foo", 0)], []),
        )
        for workflow_name, workflow in self.workflows.items():
            script = step(workflow["jobs"]["fetch-binlog"]["steps"], "fetch")["run"]
            script = script[script.index('timeline_json="${ADO_DOC}"'):script.index("# Guards for untrusted")]
            for jobs, artifacts, expected in cases:
                with self.subTest(workflow=workflow_name, jobs=jobs, artifacts=artifacts):
                    result, _, _ = self.run_script(
                        "set +e\nemit_none() { exit 0; }\n"
                        'ado_get() { ADO_DOC="$ARTIFACTS"; }\n'
                        + script + '\nprintf "selected=%s\\n" "${names[@]}"\n',
                        {
                            "ADO_DOC": json.dumps({"records": [{"id": str(i), "type": "Job", "name": n, "result": r} for i, (n, r) in enumerate(jobs)]}),
                            "ARTIFACTS": json.dumps({"value": [{"name": "Logs_Build_" + n, "source": source} for n, source in artifacts]}),
                        },
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)
                    actual = [line.removeprefix("selected=") for line in result.stdout.splitlines() if line.startswith("selected=") and line != "selected="]
                    self.assertEqual(actual, ["Logs_Build_" + name for name in expected], result.stdout)

    def test_staging_preserves_newline_entry_names(self):
        # Windows cannot create newline filenames. Model find/file existence at
        # that boundary, then execute the production staging loop unchanged.
        setup = r"""
AX_DIR=archive
ai=1
safe_name=Logs_Build_Test
count=0
find() {
  if [[ "${!#}" == "-print0" ]]; then
    printf '%s\0' regular.binlog $'newline\nentry.binlog'
  else
    printf '%s\n' regular.binlog $'newline\nentry.binlog'
  fi
}
function [ {
  if [[ "$1" == "-f" ]]; then
    [[ "$2" == "regular.binlog" || "$2" == $'newline\nentry.binlog' ]]
  else
    builtin [ "$@"
  fi
}
cp() {
  if [[ "$1" == $'newline\nentry.binlog' ]]; then
    echo preserved-newline=true
  fi
}
"""
        for name, workflow in self.workflows.items():
            with self.subTest(workflow=name):
                script = step(workflow["jobs"]["fetch-binlog"]["steps"], "fetch")["run"]
                start = script.index("i=0\n", script.index("TOTAL_BYTES=$((TOTAL_BYTES + UNCOMP))"))
                script = script[start:script.index("# Keep each artifact all-or-nothing.")]
                result, _, _ = self.run_script(setup + script + '\nprintf "staged=%s;failed=%s\\n" "$leg_staged" "$leg_failed"\n')
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("preserved-newline=true", result.stdout)
                self.assertIn("staged=2;failed=0", result.stdout)

    def test_archive_validation_rejects_unsafe_paths_and_types(self):
        regular = archive_bytes("nested/build.binlog", stat.S_IFREG | 0o644)
        classic_boundary = classic_max_entries_archive()
        cases = (
            ("regular", regular, 65536, 16 * 1024 * 1024, True),
            ("unspecified", archive_bytes("build.binlog", 0), 65536, 16 * 1024 * 1024, True),
            ("traversal", archive_bytes("../escape.binlog", stat.S_IFREG | 0o644), 65536, 16 * 1024 * 1024, False),
            ("absolute", archive_bytes("/escape.binlog", stat.S_IFREG | 0o644), 65536, 16 * 1024 * 1024, False),
            ("drive", archive_bytes(r"C:\escape.binlog", stat.S_IFREG | 0o644), 65536, 16 * 1024 * 1024, False),
            ("symlink", archive_bytes("link.binlog", stat.S_IFLNK | 0o777), 65536, 16 * 1024 * 1024, False),
            ("character-device", archive_bytes("device.binlog", stat.S_IFCHR | 0o600), 65536, 16 * 1024 * 1024, False),
            ("block-device", archive_bytes("device.binlog", stat.S_IFBLK | 0o600), 65536, 16 * 1024 * 1024, False),
            ("fifo", archive_bytes("pipe.binlog", stat.S_IFIFO | 0o600), 65536, 16 * 1024 * 1024, False),
            ("socket", archive_bytes("socket.binlog", stat.S_IFSOCK | 0o600), 65536, 16 * 1024 * 1024, False),
            ("metadata-cap", regular, 65536, 1, False),
            ("missing-eocd", b"not a zip", 65536, 16 * 1024 * 1024, False),
            ("multi-disk", rewrite_eocd(regular, disk_number=1), 65536, 16 * 1024 * 1024, False),
            (
                "classic-entry-cap",
                rewrite_eocd(regular, entries_on_disk=2, entry_count=2),
                1,
                16 * 1024 * 1024,
                False,
            ),
            (
                "classic-metadata-cap",
                rewrite_eocd(regular, central_directory_size=16 * 1024 * 1024 + 1),
                65536,
                16 * 1024 * 1024,
                False,
            ),
            (
                "classic-max-entry-count",
                classic_boundary,
                65536,
                16 * 1024 * 1024,
                True,
            ),
            ("zip64", zip64_archive(regular), 65536, 16 * 1024 * 1024, True),
            (
                "zip64-entry-cap",
                zip64_archive(regular, entries_on_disk=2, entry_count=2),
                1,
                16 * 1024 * 1024,
                False,
            ),
            (
                "zip64-nonsentinel-entry-cap",
                zip64_archive(
                    regular,
                    classic_sentinels=False,
                    entries_on_disk=2,
                    entry_count=2,
                ),
                1,
                16 * 1024 * 1024,
                False,
            ),
            (
                "zip64-metadata-cap",
                zip64_archive(regular, central_directory_size=16 * 1024 * 1024 + 1),
                65536,
                16 * 1024 * 1024,
                False,
            ),
        )
        for workflow_name, workflow in self.workflows.items():
            source = step(workflow["jobs"]["fetch-binlog"]["steps"], "fetch")["run"]
            start = source.index("# --- Validate ZIP entry metadata before extraction ---")
            end = source.index("# --- Extract validated binlogs ---")
            validator = source[start:end]
            for name, content, max_entries, max_metadata_bytes, accepted in cases:
                with self.subTest(workflow=workflow_name, case=name):
                    result, _, _ = self.run_script(
                        "set +e\n"
                        'for ZIP_TMP in "$ARCHIVE"; do\n'
                        "safe_name=test\n"
                        f"MAX_ZIP_ENTRIES={max_entries}\n"
                        f"MAX_ZIP_METADATA_BYTES={max_metadata_bytes}\n"
                        "MAX_UNZIP_BYTES=2147483648\n"
                        "MAX_TOTAL_BYTES=4294967296\n"
                        "TOTAL_BYTES=0\n"
                        + validator
                        + "\necho accepted=true\ndone\n",
                        {"ARCHIVE": "archive.zip"},
                        {"archive.zip": content},
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)
                    self.assertEqual("accepted=true" in result.stdout, accepted, result.stdout)

    def test_python_extraction_does_not_honor_asi_symlink_mode(self):
        payload = b"target"
        archive_content = archive_bytes(
            "nested/link.binlog",
            0,
            payload,
            asi_unix_extra(stat.S_IFLNK | 0o777, len(payload)),
        )
        for workflow_name, workflow in self.workflows.items():
            source = step(workflow["jobs"]["fetch-binlog"]["steps"], "fetch")["run"]
            start = source.index("# --- Extract validated binlogs ---")
            end = source.index("# --- Stage extracted binlogs ---")
            extractor = source[start:end]
            with self.subTest(workflow=workflow_name):
                result, _, _ = self.run_script(
                    "set +e\n"
                    'for ZIP_TMP in "$ARCHIVE"; do\n'
                    "safe_name=test\n"
                    'AX_DIR="$EXTRACT_DIR"\n'
                    "FETCH_DEADLINE=$(( $(date +%s) + 120 ))\n"
                    'mkdir -p "$AX_DIR"\n'
                    + extractor
                    + "\necho extracted=true\ndone\n"
                    + 'test -f "$EXTRACT_DIR/nested/link.binlog"\n'
                    + 'test ! -L "$EXTRACT_DIR/nested/link.binlog"\n'
                    + 'test "$(cat "$EXTRACT_DIR/nested/link.binlog")" = target\n',
                    {
                        "ARCHIVE": "archive.zip",
                        "EXTRACT_DIR": "extract",
                    },
                    {"archive.zip": archive_content},
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("extracted=true", result.stdout)

    def test_latest_build_and_revision_revalidation(self):
        for name, workflow in self.workflows.items():
            script = next(item for item in safe_output_steps(workflow) if item["name"] == "Revalidate PR revision before applying queued outputs")["run"]
            for build_id, status, result, head, merge, curl_status, valid in (
                (9, "completed", "failed", HEAD, MERGE, "0", True),
                (10, "completed", "succeeded", HEAD, MERGE, "0", False),
                (10, "completed", "failed", HEAD, MERGE, "0", False),
                (9, "inProgress", "failed", HEAD, MERGE, "0", False),
                (9, "completed", "succeeded", HEAD, MERGE, "0", False),
                (9, "completed", "failed", "c" * 40, MERGE, "0", False),
                (9, "completed", "failed", HEAD, "c" * 40, "0", False),
                (9, "completed", "failed", HEAD, None, "0", False),
                (9, "completed", "failed", HEAD, MERGE, "22", False),
            ):
                with self.subTest(workflow=name, build_id=build_id, status=status, result=result, head=head, merge=merge, curl_status=curl_status):
                    outcome, _, _ = self.run_script(
                        'timeout() { shift; "$@"; }\n'
                        'curl() { while [ "$#" -gt 0 ]; do if [ "$1" = "-o" ]; then shift; printf \'%s\' "$BUILDS" > "$1"; fi; shift; done; return "$CURL_STATUS"; }\n'
                        'gh() { printf \'%s\' "$PR"; }\n' + script,
                        {
                            "PR_NUMBER": "42", "BUILD_ID": "9", "EXPECTED_HEAD": HEAD, "EXPECTED_MERGE": MERGE,
                            "BUILDS": json.dumps({"value": [{"id": build_id, "status": status, "result": result}]}),
                            "PR": json.dumps({"head": {"sha": head}, "merge_commit_sha": merge}),
                            "CURL_STATUS": curl_status, "ADO_BUILD_DEFINITION_ID": "129",
                        },
                    )
                    self.assertEqual(outcome.returncode == 0, valid, outcome.stdout + outcome.stderr)

    def test_optional_artifact_handoff_and_empty_context(self):
        for name, workflow in self.workflows.items():
            with self.subTest(workflow=name):
                upload = step(workflow["jobs"]["fetch-binlog"]["steps"], "upload")
                download = step(workflow["steps"], "download_analysis")
                self.assertIs(upload["continue-on-error"], True)
                self.assertIs(download["continue-on-error"], True)
                self.assertEqual(upload["with"]["if-no-files-found"], "error")
                found = workflow["jobs"]["fetch-binlog"]["outputs"]["binlog-found"]
                self.assertIn("steps.upload.outcome == 'success'", found)
                self.assertIn("steps.upload.outputs.artifact-id != ''", found)
                export = next(item for item in workflow["steps"] if item["name"] == "Export agent context")
                self.assertEqual(export["env"]["GH_AW_BINLOG_FOUND_VALUE"], "${{ steps.download_analysis.outcome == 'success' }}")
                cleanup = next(item for item in workflow["steps"] if item["name"] == "Discard incomplete binlog download")
                for available in ("true", "false"):
                    with self.subTest(available=available):
                        script = (cleanup["run"] if available == "false" else "") + "\n" + export["run"]
                        result, _, context = self.run_script(
                            script.replace("/tmp/binlogs", "binlogs"),
                            {"GH_AW_BINLOG_FOUND_VALUE": available},
                            {"binlogs/partial.binlog": "test"},
                        )
                        self.assertEqual(result.returncode, 0, result.stderr)
                        self.assertEqual("/data/binlogs/partial.binlog" in context, available == "true")
                        if available == "false":
                            self.assertIn("GH_AW_BINLOG_PATH=\n", context)
                            self.assertIn("GH_AW_BINLOG_LIST<<GH_AW_EOF\nGH_AW_EOF", context)

    def test_integrity_scope_and_safe_output_targets(self):
        for name, workflow in self.workflows.items():
            with self.subTest(workflow=name):
                self.assertEqual(workflow["tools"]["github"]["min-integrity"], "none")
                self.assertEqual(workflow["tools"]["github"]["allowed-repos"], ["dotnet/runtime"])
                target = "${{ needs.fetch-binlog.outputs.pr-number }}" if name == NAMES[0] else "triggering"
                for output in ("add-comment", "create-pull-request-review-comment"):
                    self.assertEqual(workflow["safe-outputs"][output]["target"], target)
                self.assertEqual(
                    workflow["safe-outputs"]["create-pull-request-review-comment"]["commit-id"],
                    "${{ needs.fetch-binlog.outputs.pr-head-sha }}",
                )

    def test_binlog_allowlist_uses_exact_tool_names(self):
        for name, workflow in self.workflows.items():
            with self.subTest(workflow=name):
                allowed = workflow["mcp-servers"]["binlog-mcp"]["allowed"]
                self.assertEqual(len(allowed), 35)
                self.assertEqual(len(allowed), len(set(allowed)))
                self.assertTrue(all(tool.startswith("binlog_") and "*" not in tool for tool in allowed))
                self.assertTrue({"binlog_errors", "binlog_overview", "binlog_warnings"}.issubset(allowed))
                self.assertFalse({"stop", "stop_instance", "list_mcp_instances"}.intersection(allowed))

    def test_metadata_is_materialized_before_publication(self):
        metadata_step = load_workflow("shared/build-failure-analysis-shared")["safe-outputs"]["steps"][0]
        script = metadata_step["with"]["script"]
        metadata = {"workflow_artifact": "build-failure-analysis", "artifact_kind": "analysis"}
        block = "Structured data:\n```json\n" + json.dumps(metadata, indent=2) + "\n```"
        for supplied in (False, True):
            with self.subTest(supplied=supplied), tempfile.TemporaryDirectory(prefix="bfa-metadata-") as directory:
                path = Path(directory) / "agent_output.json"
                items = [
                    {"type": "add_comment", "body": "Root cause", "item_number": 42},
                    {"type": "create_pull_request_review_comment", "body": "Suggested fix", "path": "file.cs", "line": 10},
                    {"type": "noop", "message": "No analysis"},
                    {"type": "missing_tool", "tool": "diagnostics"},
                ]
                if supplied:
                    for item in items[:2]:
                        item["data"] = metadata
                        item["body"] += "\n\n" + block
                untouched = copy.deepcopy(items[2:])
                path.write_text(json.dumps({"items": items}), encoding="utf-8")
                for _ in range(2):
                    result = subprocess.run(
                        ["node", "-e", script], capture_output=True, text=True,
                        env={**os.environ, "GH_AW_AGENT_OUTPUT": str(path)}, timeout=15,
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)
                actual = json.loads(path.read_text(encoding="utf-8"))["items"]
                for item in actual[:2]:
                    self.assertEqual(item["data"], metadata)
                    self.assertEqual(item["body"].count(block), 1)
                self.assertEqual(actual[0]["item_number"], 42)
                self.assertEqual(actual[1]["path"], "file.cs")
                self.assertEqual(actual[2:], untouched)

        for name in NAMES:
            lock = yaml.safe_load((WORKFLOWS / (name + ".lock.yml")).read_text(encoding="utf-8"))
            names = [item.get("name") for item in lock["jobs"]["safe_outputs"]["steps"]]
            self.assertLess(names.index("Setup agent output environment variable"), names.index(metadata_step["name"]))
            guard_name = "Revalidate PR revision before applying queued outputs"
            self.assertLess(names.index(metadata_step["name"]), names.index(guard_name))
            self.assertLess(names.index(guard_name), names.index("Process Safe Outputs"))
            source_guard = next(item for item in safe_output_steps(self.workflows[name]) if item["name"] == guard_name)
            compiled_guard = next(item for item in lock["jobs"]["safe_outputs"]["steps"] if item.get("name") == guard_name)
            self.assertEqual(source_guard["run"].rstrip("\n"), compiled_guard["run"].rstrip("\n"))

    def test_invalid_output_metadata_fails_closed(self):
        script = load_workflow("shared/build-failure-analysis-shared")["safe-outputs"]["steps"][0]["with"]["script"]
        for payload in (
            {"items": None},
            {"items": [{"type": "add_comment", "body": 42}]},
            {"items": [{"type": "add_comment", "body": "Analysis", "data": None}]},
            {"items": [{"type": "add_comment", "body": "Analysis", "data": {"artifact_kind": "other"}}]},
            {"items": [{"type": "add_comment", "body": "Analysis", "data": {
                "workflow_artifact": "build-failure-analysis", "artifact_kind": "analysis", "extra": True,
            }}]},
        ):
            with self.subTest(payload=payload), tempfile.TemporaryDirectory(prefix="bfa-metadata-") as directory:
                path = Path(directory) / "agent_output.json"
                original = json.dumps(payload)
                path.write_text(original, encoding="utf-8")
                result = subprocess.run(
                    ["node", "-e", script], capture_output=True, text=True,
                    env={**os.environ, "GH_AW_AGENT_OUTPUT": str(path)}, timeout=15,
                )
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(path.read_text(encoding="utf-8"), original)


if __name__ == "__main__":
    unittest.main()

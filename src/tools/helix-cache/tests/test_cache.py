# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Run against the published native binary: python3 test_cache.py /path/to/helix-cache."""

import argparse
from contextlib import redirect_stdout
import copy
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from concurrent.futures import ThreadPoolExecutor
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
import shadow


EXECUTABLE = None


class CacheTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="helix-cache-test-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.cache = self.root / "cache"
        self.payload = self.root / "payload"
        self.payload.mkdir()
        (self.payload / "run.sh").write_text("#!/bin/sh\nexit 0\n")
        (self.payload / "run.sh").chmod(0o755)
        self.shared = self.root / "shared.zip"
        self.shared.write_bytes(b"opaque archive bytes")
        self.queues = [{
            "QueueId": "linux.test.open",
            "Architecture": "AMD64",
            "OperatingSystemGroup": "linux",
            "IsAvailable": True,
            "GalleryImage": {"Version": "1"},
            "Artifacts": [{"Id": "prepare", "Parameters": {"version": "1"}}],
            "QueueDepth": 0,
        }]
        self.submission = {
            "schemaVersion": 1,
            "workItems": [{
                "name": "suite",
                "queues": ["linux.test.open"],
                "payload": {"path": "payload"},
                "correlationPayloads": [{"path": "shared.zip", "destination": "runtime"}],
                "command": "./run.sh",
                "environment": {"SECOND": "2", "FIRST": "1"},
                "timeoutSeconds": 60,
                "expectedExitCode": 0,
            }],
        }

    def write_json(self, name, value):
        path = self.root / name
        path.write_text(json.dumps(value))
        return path

    def invoke(self, *args, success=True):
        result = subprocess.run(
            [EXECUTABLE, *map(str, args)], capture_output=True, text=True, timeout=30
        )
        self.assertEqual(0 if success else 1, result.returncode, result.stderr)
        self.assertEqual("", result.stdout)
        if not success:
            self.assertIn("helix-cache:", result.stderr)
        return result

    def lookup(self, success=True):
        self.invoke(
            "lookup", self.write_json("submission.json", self.submission),
            "--cache", self.cache, "--output", self.root / "lookup.json",
            "--queue-info", self.write_json("queues.json", self.queues),
            success=success,
        )
        if success:
            return json.loads((self.root / "lookup.json").read_text())["entries"]
        return None

    def create_results(self, execution):
        results = self.root / "results"
        results.mkdir(exist_ok=True)
        receipt = {
            "schemaVersion": 1,
            "completed": True,
            "infrastructureSucceeded": True,
            "exitCode": 0,
            "jobId": "00000000-0000-0000-0000-000000000001",
            "workItemName": execution["inputs"]["name"],
            "targetQueue": execution["inputs"]["targetQueue"],
            "resultFiles": ["tests.xml"],
        }
        self.write_json("results/result.json", receipt)
        (results / "tests.xml").write_text(
            '<assemblies><assembly total="2" passed="1" failed="0" skipped="1">'
            '<collection><test name="one" result="Pass"/>'
            '<test name="two" result="Skip"/></collection></assembly></assemblies>'
        )
        return results

    def store(self, execution, success=True):
        return self.invoke(
            "store", self.write_json("execution.json", execution),
            "--results", self.root / "results", "--cache", self.cache,
            success=success,
        )

    def test_round_trip_uses_frozen_inputs_and_preserves_results(self):
        first = self.lookup()[0]
        self.assertEqual("miss", first["status"])
        execution = first["execution"]
        snapshot = Path(execution["payloadPath"])
        self.assertNotEqual(self.payload, snapshot)
        self.assertEqual(0o755, (snapshot / "run.sh").stat().st_mode & 0o777)
        self.create_results(execution)
        self.store(execution)
        hit = self.lookup()[0]
        self.assertEqual("hit", hit["status"])
        self.assertEqual(execution["key"], hit["execution"]["key"])
        self.assertEqual(
            (self.root / "results/tests.xml").read_bytes(),
            (Path(hit["resultsPath"]) / "tests.xml").read_bytes(),
        )
        (self.payload / "run.sh").write_text("changed")
        self.assertEqual("#!/bin/sh\nexit 0\n", (snapshot / "run.sh").read_text())
        self.assertEqual("miss", self.lookup()[0]["status"])

    def test_execution_and_environment_changes_invalidate_keys(self):
        baseline = self.lookup()[0]["execution"]["key"]
        original = copy.deepcopy(self.submission)
        for field, value in [
            ("command", "./run.sh other"),
            ("preCommands", ["export STRESS=1"]),
            ("postCommands", ["echo done"]),
            ("timeoutSeconds", 61),
            ("expectedExitCode", 100),
            ("environment", {"FIRST": "changed"}),
        ]:
            with self.subTest(field=field):
                self.submission = copy.deepcopy(original)
                self.submission["workItems"][0][field] = value
                self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])
        self.submission = original
        self.queues[0]["GalleryImage"]["Version"] = "2"
        self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])
        self.queues[0]["GalleryImage"]["Version"] = "1"
        self.queues[0]["Artifacts"][0]["Parameters"]["version"] = "2"
        self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])

    def test_irrelevant_order_and_live_queue_state_do_not_change_key(self):
        baseline = self.lookup()[0]["execution"]["key"]
        self.queues[0]["QueueDepth"] = 300
        self.queues[0]["IsInProtectedMode"] = True
        self.submission["workItems"][0]["environment"] = {"FIRST": "1", "SECOND": "2"}
        os.utime(self.payload / "run.sh", (1000000000, 1000000000))
        self.assertEqual(baseline, self.lookup()[0]["execution"]["key"])

    def test_paths_do_not_affect_keys_but_modes_and_empty_directories_do(self):
        baseline = self.lookup()[0]["execution"]["key"]
        relocated = self.root / "relocated"
        self.payload.rename(relocated)
        self.submission["workItems"][0]["payload"]["path"] = "relocated"
        self.assertEqual(baseline, self.lookup()[0]["execution"]["key"])
        (relocated / "run.sh").chmod(0o644)
        self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])
        (relocated / "run.sh").chmod(0o755)
        (relocated / "empty").mkdir()
        self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])

    def test_queue_partitioning_and_digest_pinned_containers(self):
        target = "(Debian.Amd64)linux.test.open@registry/image:tag@sha256:" + "a" * 64
        self.submission["workItems"][0]["queues"].append(target)
        entries = self.lookup()
        self.assertEqual(2, len(entries))
        self.assertNotEqual(entries[0]["execution"]["key"], entries[1]["execution"]["key"])
        self.assertEqual(
            "registry/image:tag@sha256:" + "a" * 64,
            entries[1]["execution"]["inputs"]["environmentFingerprint"]["Container"],
        )
        self.create_results(entries[0]["execution"])
        self.store(entries[0]["execution"])
        self.assertEqual(["hit", "miss"], [e["status"] for e in self.lookup()])
        self.submission["workItems"][0]["queues"] = ["linux.test.open@registry/image:latest"]
        self.lookup(success=False)
        self.submission["workItems"][0]["queues"] = ["(alias)linux.test.open"]
        self.lookup(success=False)

    def test_shared_payload_bytes_destination_and_overlay_order_matter(self):
        baseline = self.lookup()[0]["execution"]["key"]
        self.shared.write_bytes(b"new archive")
        self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])
        self.shared.write_bytes(b"opaque archive bytes")
        self.submission["workItems"][0]["correlationPayloads"][0]["destination"] = "other"
        self.assertNotEqual(baseline, self.lookup()[0]["execution"]["key"])
        correlations = self.submission["workItems"][0]["correlationPayloads"]
        correlations.append({"path": "shared.zip", "destination": "second"})
        before = self.lookup()[0]["execution"]["key"]
        correlations.reverse()
        self.assertNotEqual(before, self.lookup()[0]["execution"]["key"])

    def test_unsuccessful_receipts_are_not_stored(self):
        execution = self.lookup()[0]["execution"]
        self.create_results(execution)
        original = json.loads((self.root / "results/result.json").read_text())
        for field, value in [
            ("completed", False), ("infrastructureSucceeded", False),
            ("exitCode", 1), ("workItemName", "other"),
            ("targetQueue", "other"), ("jobId", "not-a-guid"),
            ("resultFiles", []), ("resultFiles", ["../outside.xml"]),
            ("resultFiles", ["tests.xml", "tests.xml"]),
        ]:
            with self.subTest(field=field, value=value):
                receipt = dict(original, **{field: value})
                self.write_json("results/result.json", receipt)
                self.store(execution, success=False)
        self.assertEqual("miss", self.lookup()[0]["status"])

    def test_reports_must_be_complete_nonempty_and_passing(self):
        execution = self.lookup()[0]["execution"]
        self.create_results(execution)
        for report in [
            "<assemblies/>",
            '<assemblies><assembly total="0" passed="0" failed="0" skipped="0"/></assemblies>',
            '<assemblies><assembly total="1" passed="1" failed="0" skipped="0"/></assemblies>',
            '<assemblies><assembly total="1" passed="0" failed="1" skipped="0">'
            '<collection><test result="Fail"/></collection></assembly></assemblies>',
            '<assemblies><assembly total="1" passed="1" failed="0" skipped="0">'
            '<errors><error/></errors><collection><test result="Pass"/></collection></assembly></assemblies>',
            '<!DOCTYPE assemblies [<!ENTITY x SYSTEM "file:///etc/passwd">]>'
            '<assemblies>&x;</assemblies>',
        ]:
            with self.subTest(report=report):
                (self.root / "results/tests.xml").write_text(report)
                self.store(execution, success=False)
        self.assertEqual("miss", self.lookup()[0]["status"])

    def test_corruption_is_an_error_not_a_hit_or_silent_miss(self):
        execution = self.lookup()[0]["execution"]
        self.create_results(execution)
        self.store(execution)
        hit = self.lookup()[0]
        (Path(hit["resultsPath"]) / "tests.xml").write_text("corrupted")
        self.lookup(success=False)

    def test_modified_snapshot_or_forged_key_cannot_be_stored(self):
        execution = self.lookup()[0]["execution"]
        self.create_results(execution)
        forged = dict(execution, key="0" * 64)
        self.store(forged, success=False)
        (Path(execution["payloadPath"]) / "run.sh").write_text("modified after lookup")
        self.store(execution, success=False)

    def test_store_rejects_results_containing_its_staging_directory(self):
        execution = self.lookup()[0]["execution"]
        result = self.invoke(
            "store", self.write_json("execution.json", execution),
            "--results", self.cache / "tmp", "--cache", self.cache,
            success=False,
        )
        self.assertIn("must not be inside input directory", result.stderr)
        self.assertEqual([], list((self.cache / "tmp").iterdir()))
        self.assertEqual("miss", self.lookup()[0]["status"])

    def test_duplicate_stores_are_atomic_and_idempotent(self):
        execution = self.lookup()[0]["execution"]
        self.create_results(execution)
        record = self.write_json("execution.json", execution)
        with ThreadPoolExecutor(max_workers=2) as pool:
            operations = [
                pool.submit(
                    self.invoke, "store", record, "--results", self.root / "results",
                    "--cache", self.cache,
                ) for _ in range(2)
            ]
            for operation in operations:
                operation.result()
        self.assertEqual("hit", self.lookup()[0]["status"])
        self.assertEqual([], list((self.cache / "tmp").iterdir()))

    def test_rejects_unsupported_queues_links_and_nested_cache(self):
        for field, value in [
            ("Architecture", "ARM64"), ("OperatingSystemGroup", "windows"),
            ("IsAvailable", False),
        ]:
            with self.subTest(field=field):
                original = self.queues[0][field]
                self.queues[0][field] = value
                self.lookup(success=False)
                self.queues[0][field] = original
        (self.payload / "link").symlink_to(self.shared)
        self.lookup(success=False)
        (self.payload / "link").unlink()
        self.cache = self.payload / "cache"
        self.lookup(success=False)

    def test_strict_manifest_and_cli_validation(self):
        original = copy.deepcopy(self.submission)
        for transform in [
            lambda s: s.update(schemaVersion=2),
            lambda s: s.update(workItems=[]),
            lambda s: s.update(workItems=[None]),
            lambda s: s.update(unrecognized=True),
            lambda s: s["workItems"][0].pop("expectedExitCode"),
            lambda s: s["workItems"][0].update(command=None),
            lambda s: s["workItems"][0].update(timeoutSeconds=0),
        ]:
            with self.subTest(transform=transform):
                self.submission = copy.deepcopy(original)
                transform(self.submission)
                self.lookup(success=False)
        self.invoke("store", "missing", "--cache", self.cache, success=False)
        self.invoke("lookup", "missing", "--cache", self.cache, "--cache", self.cache, success=False)

    def prepare_shadow(self, name="shadow"):
        args = argparse.Namespace(
            executable=Path(EXECUTABLE), cache=self.cache, output=self.root / name,
            payload=self.shared, runtime=self.shared, queue=self.queues[0]["QueueId"],
            queue_info=self.write_json("queues.json", self.queues), wait_seconds=60,
        )
        with redirect_stdout(io.StringIO()):
            shadow.prepare(args)
        return args

    def collect_shadow(self, args, job_id, report=None, item_changes=None, files=None, job_changes=None):
        (args.output / "job-id.txt").write_text(job_id + "\n")
        item = {
            "Job": job_id, "Name": shadow.WORK_ITEM, "State": "Passed",
            "ExitCode": 0, "MachineName": "test-worker",
        }
        item.update(item_changes or {})
        if report is None:
            report = (
                '<assemblies><assembly name="/worker/Suite.dll" total="2" passed="1" failed="0" skipped="1">'
                '<collection><test name="one" result="Pass"/>'
                '<test name="two" result="Skip"/></collection></assembly></assemblies>'
            )
        if files is None:
            files = [{"Name": "testResults.xml", "Link": "https://example.invalid/report"}]
        job = {"Name": job_id, "QueueId": self.queues[0]["QueueId"]}
        job.update(job_changes or {})
        responses = {
            f"{shadow.API}/jobs/{job_id}": {"Name": job["Name"], "QueueId": job["QueueId"]},
            f"{shadow.API}/jobs/{job_id}/details": job,
            f"{shadow.API}/jobs/{job_id}/workitems/{shadow.WORK_ITEM}/files?latestOnly=true": files,
        }
        with (patch.object(shadow, "completed_work_item", return_value=item),
              patch.object(shadow, "get_json", side_effect=responses.__getitem__),
              patch.object(shadow, "download", side_effect=lambda url, path: path.write_text(report)),
              redirect_stdout(io.StringIO())):
            shadow.collect(args)

    def test_shadow_cold_store_and_restored_entries_compare_actual_outcomes(self):
        import shutil

        first_job = "00000000-0000-0000-0000-000000000001"
        second_job = "00000000-0000-0000-0000-000000000002"
        first = self.prepare_shadow()
        self.collect_shadow(first, first_job)
        first_summary = shadow.read_json(first.output / "summary.json")
        self.assertEqual("miss", first_summary["lookup"])
        self.assertEqual(2, first_summary["tests"])
        restored = self.root / "restored-cache"
        shutil.copytree(self.cache / "entries", restored / "entries")
        self.cache.rename(self.root / "previous-cache")
        self.cache = restored
        second = self.prepare_shadow("second")
        report = (
            '<assemblies><assembly name="/different/worker/Suite.dll" time="123" total="2" passed="1" failed="0" skipped="1">'
            '<collection><test name="two" result="Skip"/>'
            '<test name="one" result="Pass"/></collection></assembly></assemblies>'
        )
        self.collect_shadow(second, second_job, report)
        summary = shadow.read_json(second.output / "summary.json")
        self.assertEqual("hit", summary["lookup"])
        self.assertEqual("match", summary["comparison"])
        self.assertEqual(first_job, summary["cachedJobId"])
        self.assertEqual(second_job, summary["jobId"])
        self.assertEqual(first_summary["key"], summary["key"])

    def test_shadow_reports_mismatches_without_overwriting_cached_provenance(self):
        first = self.prepare_shadow()
        self.collect_shadow(first, "00000000-0000-0000-0000-000000000001")
        second = self.prepare_shadow("second")
        changed = (
            '<assemblies><assembly name="Suite.dll" total="2" passed="1" failed="0" skipped="1">'
            '<collection><test name="different-test" result="Pass"/>'
            '<test name="two" result="Skip"/></collection></assembly></assemblies>'
        )
        with self.assertRaisesRegex(ValueError, "outcomes differ"):
            self.collect_shadow(second, "00000000-0000-0000-0000-000000000002", changed)
        summary = shadow.read_json(second.output / "summary.json")
        self.assertEqual("mismatch", summary["comparison"])
        self.assertEqual("00000000-0000-0000-0000-000000000001", summary["cachedJobId"])

    def test_shadow_does_not_admit_unsuccessful_remote_completions(self):
        args = self.prepare_shadow()
        for changes in (
            {"State": "Failed"}, {"FailureReason": {"Issue": {"Name": "WorkItemTimeout"}}},
            {"Errors": [{"Message": "Infrastructure failure"}]}, {"ExitCode": 1}, {"ExitCode": None},
        ):
            with self.subTest(changes=changes), self.assertRaisesRegex(ValueError, "did not complete"):
                self.collect_shadow(args, "00000000-0000-0000-0000-000000000001", item_changes=changes)
        with self.assertRaisesRegex(ValueError, "target queue"):
            self.collect_shadow(args, "00000000-0000-0000-0000-000000000001", job_changes={"QueueId": "other"})
        self.assertEqual([], list((self.cache / "entries").rglob("entry.json")))

    def test_shadow_queue_alias_validation_and_provenance(self):
        expected = self.queues[0]["QueueId"]
        cases = [
            ({"QueueId": expected + ".rt", "QueueAlias": expected.upper()}, True),
            ({"QueueId": expected}, True),
            ({"QueueId": expected, "QueueAlias": None}, True),
            ({"QueueId": expected, "QueueAlias": ""}, True),
            ({"QueueId": expected, "QueueAlias": "other"}, False),
            ({"QueueId": "other", "QueueAlias": "other"}, False),
            ({"QueueId": "other"}, False),
            ({"Name": "other", "QueueId": expected + ".rt", "QueueAlias": expected}, False),
        ]
        for index, (job, accepted) in enumerate(cases):
            with self.subTest(job=job):
                self.cache = self.root / f"cache-{index}"
                args = self.prepare_shadow(f"shadow-{index}")
                if accepted:
                    self.collect_shadow(args, "00000000-0000-0000-0000-000000000001", job_changes=job)
                    provenance = shadow.read_json(args.output / "results/provenance.json")
                    self.assertEqual(job["QueueId"], provenance["resolvedQueueId"])
                    self.assertEqual(job.get("QueueAlias"), provenance["queueAlias"])
                    entry, = shadow.read_json(args.output / "lookup.json")["entries"]
                    receipt = shadow.read_json(
                        self.cache / "entries/v1" / entry["execution"]["key"][:2]
                        / entry["execution"]["key"] / "results/result.json"
                    )
                    self.assertEqual(expected, receipt["targetQueue"])
                else:
                    with self.assertRaisesRegex(ValueError, "job or target queue"):
                        self.collect_shadow(args, "00000000-0000-0000-0000-000000000001", job_changes=job)
                    self.assertFalse((args.output / "results").exists())
                    self.assertEqual([], list((self.cache / "entries").rglob("entry.json")))

    def test_shadow_requires_one_uploaded_report_and_native_report_validation(self):
        args = self.prepare_shadow()
        for files in ([], [{"Name": "testResults.xml"}] * 2):
            with self.subTest(files=files), self.assertRaises(ValueError):
                self.collect_shadow(args, "00000000-0000-0000-0000-000000000001", files=files)
        with self.assertRaises(subprocess.CalledProcessError):
            self.collect_shadow(args, "00000000-0000-0000-0000-000000000001", report="<assemblies/>")
        self.assertEqual([], list((self.cache / "entries").rglob("entry.json")))

    def test_shadow_polling_rejects_timeouts_unknown_states_and_identity_mismatches(self):
        item = {"Job": "job", "Name": "suite", "State": "Running"}
        with (patch.object(shadow, "get_json", return_value=item),
              patch.object(shadow.time, "monotonic", side_effect=[0, 61]),
              self.assertRaises(TimeoutError)):
            shadow.completed_work_item("job", "suite", 60)
        for changes in ({"State": "Canceled"}, {"Job": "other"}, {"Name": "other"}):
            with (self.subTest(changes=changes),
                  patch.object(shadow, "get_json", return_value=dict(item, **changes)),
                  self.assertRaises(ValueError)):
                shadow.completed_work_item("job", "suite", 60)
        with (patch.object(shadow, "get_json", side_effect=[item, dict(item, State="Finished")]),
              patch.object(shadow.time, "sleep") as sleep):
            self.assertEqual("Finished", shadow.completed_work_item("job", "suite", 60)["State"])
            sleep.assert_called_once()


if __name__ == "__main__":
    EXECUTABLE = str(Path(sys.argv.pop(1)).resolve())
    unittest.main()

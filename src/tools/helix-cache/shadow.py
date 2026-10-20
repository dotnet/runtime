# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Prepare frozen Helix submissions and collect public results; caching stays in the native CLI."""

import argparse
from collections import Counter
import json
from pathlib import Path
import subprocess
import time
from urllib.parse import quote
from urllib.request import urlopen
import uuid
import xml.etree.ElementTree as ET


API = "https://helix.dot.net/api/2019-06-17"
WORK_ITEM = "System.Memory.Data.Tests"
COMMAND = 'bash ./RunTests.sh --runtime-path "$HELIX_CORRELATION_PAYLOAD"'
POST_COMMAND = 'cp testResults.xml "$HELIX_WORKITEM_UPLOAD_ROOT/testResults.xml"'


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def read_json(path):
    return json.loads(path.read_text())


def invoke(executable, *args):
    subprocess.run([str(executable), *map(str, args)], check=True)


def msbuild_literal(value):
    for char in "%$@();'*?":
        value = value.replace(char, f"%{ord(char):02X}")
    return value


def prepare(args):
    for archive in (args.payload, args.runtime):
        if not archive.is_file() or archive.suffix.lower() != ".zip":
            raise ValueError(f"The shadow submitter requires an existing ZIP archive: {archive}")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    submission = {
        "schemaVersion": 1,
        "workItems": [{
            "name": WORK_ITEM,
            "queues": [args.queue],
            "payload": {"path": str(args.payload.resolve())},
            "correlationPayloads": [{"path": str(args.runtime.resolve())}],
            "command": COMMAND,
            "postCommands": [POST_COMMAND],
            "timeoutSeconds": 900,
            "expectedExitCode": 0,
        }],
    }
    write_json(output / "submission.json", submission)
    options = ["--queue-info", args.queue_info] if args.queue_info else []
    invoke(args.executable, "lookup", output / "submission.json",
           "--cache", args.cache, "--output", output / "lookup.json", *options)
    entry, = read_json(output / "lookup.json")["entries"]
    execution = entry["execution"]
    write_json(output / "execution.json", execution)
    project = ET.Element("Project")
    properties = ET.SubElement(project, "PropertyGroup")
    for name, value in {
        "ShadowName": execution["inputs"]["name"],
        "ShadowQueue": execution["inputs"]["targetQueue"],
        "ShadowPayload": execution["payloadPath"],
        "ShadowRuntime": execution["correlationPayloadPaths"][0],
        "ShadowCommand": execution["inputs"]["command"],
        "ShadowPostCommand": execution["inputs"]["postCommands"][0],
        "ShadowTimeout": "00:15:00",
    }.items():
        ET.SubElement(properties, name).text = msbuild_literal(value)
    ET.indent(project, space="  ")
    ET.ElementTree(project).write(output / "submission.props", encoding="utf-8", xml_declaration=True)
    print(f"Helix cache shadow: {entry['status']} {execution['key']}; execution will NOT be skipped.")


def get_json(url):
    with urlopen(url, timeout=60) as response:
        return json.load(response)


def download(url, destination):
    with urlopen(url, timeout=60) as response:
        destination.write_bytes(response.read())


def completed_work_item(job_id, name, wait_seconds):
    deadline = time.monotonic() + wait_seconds
    url = f"{API}/jobs/{job_id}/workitems/{quote(name, safe='')}"
    while True:
        item = get_json(url)
        if item["Job"] != job_id or item["Name"] != name:
            raise ValueError("Helix returned a different job or work item.")
        if item["State"] in ("Finished", "Passed", "Failed"):
            return item
        if item["State"] not in ("Unscheduled", "Waiting", "Running"):
            raise ValueError(f"Unexpected Helix work item state: {item['State']}")
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError(f"Helix job {job_id} did not complete within {wait_seconds} seconds.")
        time.sleep(min(15, remaining))


def outcomes(path):
    root = ET.parse(path).getroot()
    return Counter(
        (Path(assembly.attrib["name"]).name, test.attrib["name"], test.attrib["result"],
         test.findtext("reason/message", default="") if test.attrib["result"] == "Skip" else "")
        for assembly in root.findall("assembly")
        for test in assembly.findall("collection/test")
    )


def collect(args):
    output = args.output.resolve()
    execution = read_json(output / "execution.json")
    lookup, = read_json(output / "lookup.json")["entries"]
    job_id = str(uuid.UUID((output / "job-id.txt").read_text().strip()))
    summary = {"key": execution["key"], "lookup": lookup["status"], "jobId": job_id, "mode": "shadow"}
    write_json(output / "summary.json", summary)
    job = get_json(f"{API}/jobs/{job_id}/details")
    expected_queue = execution["inputs"]["environmentFingerprint"]["QueueId"]
    submitted_queue = job.get("QueueAlias") or job["QueueId"]
    if job["Name"] != job_id or submitted_queue.casefold() != expected_queue.casefold():
        raise ValueError("Helix returned a different job or target queue.")
    item = completed_work_item(job_id, execution["inputs"]["name"], args.wait_seconds)
    write_json(output / "work-item.json", item)
    if (item["State"] not in ("Finished", "Passed") or item.get("FailureReason") is not None
            or item.get("Errors") or type(item.get("ExitCode")) is not int
            or item["ExitCode"] != execution["inputs"]["expectedExitCode"]):
        raise ValueError(f"Helix work item did not complete successfully; see {output / 'work-item.json'}.")
    files = get_json(f"{API}/jobs/{job_id}/workitems/{quote(item['Name'], safe='')}/files?latestOnly=true")
    reports = [file for file in files if file["Name"] == "testResults.xml"]
    if len(reports) != 1:
        raise ValueError(f"Expected exactly one uploaded testResults.xml; found {len(reports)}.")
    report, = reports
    results = output / "results"
    results.mkdir()
    download(report["Link"], results / "testResults.xml")
    if item.get("ConsoleOutputUri"):
        download(item["ConsoleOutputUri"], results / "console.log")
    write_json(results / "result.json", {
        "schemaVersion": 1,
        "completed": True,
        "infrastructureSucceeded": True,
        "exitCode": item["ExitCode"],
        "jobId": job_id,
        "workItemName": item["Name"],
        "targetQueue": execution["inputs"]["targetQueue"],
        "resultFiles": ["testResults.xml"],
    })
    write_json(results / "provenance.json", {
        "kind": "helix-execution",
        "jobId": job_id,
        "queueAlias": job.get("QueueAlias"),
        "resolvedQueueId": job["QueueId"],
        "machineName": item["MachineName"],
        "state": item["State"],
    })
    invoke(args.executable, "store", output / "execution.json", "--results", results, "--cache", args.cache)
    fresh = outcomes(results / "testResults.xml")
    summary["tests"] = sum(fresh.values())
    summary["comparison"] = "not-applicable"
    if lookup["status"] == "hit":
        cached_results = Path(lookup["resultsPath"])
        cached = outcomes(cached_results / "testResults.xml")
        summary["cachedJobId"] = read_json(cached_results / "result.json")["jobId"]
        summary["comparison"] = "match" if cached == fresh else "mismatch"
        write_json(output / "summary.json", summary)
        if cached != fresh:
            raise ValueError(f"Cached and fresh test outcomes differ; see {output / 'summary.json'}.")
    summary["stored"] = True
    write_json(output / "summary.json", summary)
    print(json.dumps(summary, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("prepare", "collect"):
        command = commands.add_parser(name)
        command.add_argument("--executable", type=Path, required=True)
        command.add_argument("--cache", type=Path, required=True)
        command.add_argument("--output", type=Path, required=True)
        if name == "prepare":
            command.add_argument("--payload", type=Path, required=True)
            command.add_argument("--runtime", type=Path, required=True)
            command.add_argument("--queue", required=True)
            command.add_argument("--queue-info", type=Path)
        else:
            command.add_argument("--wait-seconds", type=int, default=3600)
    args = parser.parse_args()
    if args.command == "collect" and args.wait_seconds <= 0:
        parser.error("--wait-seconds must be positive")
    (prepare if args.command == "prepare" else collect)(args)


if __name__ == "__main__":
    main()

"""Extract failed test results from AzDO Test Results API.

Usage:
    python extract_failed_tests.py --db monitor.db

Requires: ADO_TOKEN env var or az cli logged in.

Reads failing pipelines from the pipelines table in monitor.db, calls the
AzDO Test Results API for each, and INSERTs one row per failed test method
into the test_results table (pipeline_name, build_id, run_name, test_name,
helix info, console URL, error_message, stack_trace).

The API returns errorMessage and stackTrace for most xUnit test failures.
For crashes/timeouts that kill the Helix work item, the API returns a generic
"The Helix Work Item failed..." message — these are stored as empty so the
LLM can extract the real error from the console log during triage.
"""
import argparse
import json
import os
import sqlite3
import subprocess
import sys
import requests

ADO_BASE = "https://dev.azure.com/dnceng-public/public/_apis"
HELIX_CONSOLE = "https://helix.dot.net/api/2019-06-17/jobs/{job_id}/workitems/{work_item}/console"

# Generic Helix error message that provides no useful diagnostic info.
# When the API returns this, we store empty so the LLM extracts from the console log.
GENERIC_HELIX_MSG = "The Helix Work Item failed."


def get_token():
    """Get Azure DevOps bearer token from env var or az cli."""
    token = os.environ.get("ADO_TOKEN", "")
    if token:
        return token
    try:
        if os.name == "nt":
            cmd = [
                "cmd",
                "/c",
                "az",
                "account",
                "get-access-token",
                "--resource",
                "499b84ac-1321-427f-aa17-267ca6975798",
                "--query",
                "accessToken",
                "-o",
                "tsv",
            ]
        else:
            cmd = [
                "az",
                "account",
                "get-access-token",
                "--resource",
                "499b84ac-1321-427f-aa17-267ca6975798",
                "--query",
                "accessToken",
                "-o",
                "tsv",
            ]
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=30,
        )
        if result.returncode != 0:
            print(
                f"Error getting token from az cli (exit code {result.returncode}): {result.stderr.strip()}",
                file=sys.stderr,
            )
            return ""
        token = result.stdout.strip()
        if not token:
            print("Error getting token from az cli: empty access token.", file=sys.stderr)
            return ""
        return token
    except Exception as e:
        print(f"Error getting token: {e}", file=sys.stderr)
        return ""


def fetch_failed_tests(build_id, pipeline_name, token):
    """Fetch all failed test results for a build via AzDO Test Results API.

    Returns (failures_list, error_or_None). error is a dict with
    'error_type' and 'detail' if a request failed, else None.
    """
    headers = {"Authorization": f"Bearer {token}"}
    failures = []

    # Step 1: Get test runs for this build
    url = f"{ADO_BASE}/test/runs?buildUri=vstfs:///Build/Build/{build_id}&api-version=7.1"
    try:
        resp = requests.get(url, headers=headers, timeout=30)
    except requests.exceptions.Timeout as e:
        print(f"ERROR: test/runs request timed out for build {build_id}: {e}", file=sys.stderr)
        return failures, {"error_type": "timeout", "detail": f"test/runs timed out: {e}"}
    except requests.RequestException as e:
        print(f"ERROR: test/runs request failed for build {build_id}: {e}", file=sys.stderr)
        return failures, {"error_type": "request_failed", "detail": f"test/runs failed: {e}"}
    if resp.status_code != 200:
        print(f"ERROR: test/runs for build {build_id} returned HTTP {resp.status_code}", file=sys.stderr)
        return failures, {"error_type": "http_error", "detail": f"test/runs returned HTTP {resp.status_code}"}

    runs = resp.json().get("value", [])

    # Step 2: For each run, get failed results
    for run in runs:
        run_id = run["id"]
        run_name = run.get("name", "")

        page_size = 1000
        skip = 0
        while True:
            url2 = (
                f"{ADO_BASE}/test/runs/{run_id}/results"
                f"?outcomes=Failed&$top={page_size}&$skip={skip}&api-version=7.1"
            )
            try:
                resp2 = requests.get(url2, headers=headers, timeout=30)
            except requests.exceptions.Timeout as e:
                print(f"WARNING: results request timed out for run {run_id}: {e}", file=sys.stderr)
                break
            except requests.RequestException as e:
                print(
                    f"WARNING: results request failed for run {run_id}: {e}",
                    file=sys.stderr,
                )
                break
            if resp2.status_code != 200:
                print(
                    f"WARNING: results for run {run_id} returned HTTP {resp2.status_code}",
                    file=sys.stderr,
                )
                break

            results = resp2.json().get("value", [])
            if not results:
                break

            for r in results:
                automated_name = r.get("automatedTestName", r.get("testCaseTitle", ""))
                # Strip .WorkItemExecution suffix
                test_name = automated_name
                if test_name.endswith(".WorkItemExecution"):
                    test_name = test_name[:-len(".WorkItemExecution")]

                # Parse comment JSON for Helix info
                helix_job_id = ""
                helix_work_item = ""
                comment = r.get("comment", "")
                if comment:
                    try:
                        cdata = json.loads(comment)
                        helix_job_id = cdata.get("HelixJobId", "")
                        helix_work_item = cdata.get("HelixWorkItemName", "")
                    except (json.JSONDecodeError, TypeError):
                        pass

                # Build console URL if we have Helix info
                console_log_url = ""
                if helix_job_id and helix_work_item:
                    console_log_url = HELIX_CONSOLE.format(
                        job_id=helix_job_id,
                        work_item=helix_work_item,
                    )

                # Extract errorMessage and stackTrace from the API response.
                # Skip generic "Helix Work Item failed" messages — these provide
                # no diagnostic value; the LLM will extract from console logs.
                api_error = r.get("errorMessage", "") or ""
                api_stack = r.get("stackTrace", "") or ""
                if api_error.startswith(GENERIC_HELIX_MSG):
                    api_error = ""
                    api_stack = ""

                failures.append({
                    "pipeline_name": pipeline_name,
                    "build_id": build_id,
                    "run_id": run_id,
                    "run_name": run_name,
                    "test_name": test_name,
                    "automated_test_name": automated_name,
                    "helix_job_id": helix_job_id,
                    "helix_work_item": helix_work_item,
                    "console_log_url": console_log_url,
                    "error_message": api_error,
                    "stack_trace": api_stack,
                })

            if len(results) < page_size:
                break

            skip += page_size
    return failures, None


def insert_into_db(conn, failures):
    """INSERT all failures into the test_results table."""
    inserted = 0
    api_with_error = 0
    for f in failures:
        conn.execute(
            """INSERT INTO test_results
               (pipeline_name, build_id, run_name, test_name,
                helix_job_id, helix_work_item, console_log_url,
                error_message, stack_trace)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (f["pipeline_name"], f["build_id"], f["run_name"], f["test_name"],
             f["helix_job_id"], f["helix_work_item"], f["console_log_url"],
             f.get("error_message", "") or None,
             f.get("stack_trace", "") or None)
        )
        inserted += 1
        if f.get("error_message"):
            api_with_error += 1
    conn.commit()
    print(f"Inserted {inserted} rows into test_results ({api_with_error} with API error/stack)", file=sys.stderr)


def main():
    parser = argparse.ArgumentParser(description="Extract failed tests from AzDO Test Results API")
    parser.add_argument("--db", required=True, help="Path to monitor.db — reads failing pipelines, INSERTs into test_results table")
    args = parser.parse_args()

    # Read failing builds from DB
    conn = sqlite3.connect(args.db)
    conn.row_factory = sqlite3.Row
    rows = conn.execute(
        "SELECT name, build_id FROM pipelines WHERE result = 'failed' ORDER BY name"
    ).fetchall()
    builds = [(r["build_id"], r["name"]) for r in rows]
    conn.close()

    token = get_token()
    if not token:
        print("ERROR: Could not get Azure token. Set ADO_TOKEN or login with az cli.", file=sys.stderr)
        sys.exit(1)

    all_failures = []
    zero_result_pipelines = []
    for build_id, pipeline_name in builds:
        print(f"Fetching failed tests for build {build_id} ({pipeline_name})...", file=sys.stderr)
        failures, error = fetch_failed_tests(build_id, pipeline_name, token)
        print(f"  Found {len(failures)} failed tests", file=sys.stderr)
        if error:
            print(f"  WARNING: {error['error_type']}: {error['detail']}", file=sys.stderr)
        if len(failures) == 0:
            zero_result_pipelines.append(pipeline_name)
        all_failures.extend(failures)

    print(f"Total: {len(all_failures)} failed tests from {len(builds)} builds", file=sys.stderr)

    conn = sqlite3.connect(args.db)
    conn.row_factory = sqlite3.Row
    insert_into_db(conn, all_failures)

    # Mark pipelines with 0 test results as inconclusive
    for name in zero_result_pipelines:
        conn.execute(
            "UPDATE pipelines SET result = 'inconclusive', skip_reason = 'Build failed but no test failures detected via Test Results API' WHERE name = ?",
            (name,)
        )
    if zero_result_pipelines:
        conn.commit()
        print(f"Marked {len(zero_result_pipelines)} pipelines as inconclusive (0 test failures from API)", file=sys.stderr)

    conn.close()


if __name__ == "__main__":
    main()

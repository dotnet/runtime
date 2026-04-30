"""Setup DB and fetch pipeline build status (deterministic).

Creates monitor.db with the full schema, fetches the latest build for each
pipeline from the ADO Builds API (no auth), and populates the pipelines table.

Usage:
    python setup_and_fetch_builds.py [--pipelines pipelines.md] [--db monitor.db]

Output:
    - Creates/overwrites monitor.db with all tables
    - Populates the pipelines table with latest build status
    - Prints summary to stderr
"""
import argparse
import os
import re
import sqlite3
import sys
import requests

ADO_BASE = "https://dev.azure.com/dnceng-public/public/_apis"

SCHEMA = """
CREATE TABLE IF NOT EXISTS pipelines (
    name            TEXT PRIMARY KEY,
    build_id        INTEGER,
    build_number    TEXT,
    result          TEXT NOT NULL,
    skip_reason     TEXT
);

CREATE TABLE IF NOT EXISTS failures (
    id                    INTEGER PRIMARY KEY,
    title                 TEXT NOT NULL,
    scope                 TEXT,
    test_name             TEXT NOT NULL,
    work_item             TEXT,
    failure_category      TEXT,
    exit_codes            TEXT,
    failing_since_date    TEXT,
    failing_since_build   TEXT,
    console_log_url       TEXT,
    source_test_result_id INTEGER,
    error_message         TEXT,
    stack_trace           TEXT,
    summary               TEXT,
    analysis              TEXT,
    github_issue_number   INTEGER,
    github_issue_url      TEXT,
    github_issue_state    TEXT,
    github_issue_assigned TEXT,
    labels                TEXT,
    milestone             TEXT DEFAULT '11.0.0',
    FOREIGN KEY (source_test_result_id) REFERENCES test_results(id)
);

CREATE TABLE IF NOT EXISTS failure_pipelines (
    failure_id      INTEGER NOT NULL REFERENCES failures(id),
    pipeline_name   TEXT NOT NULL,
    build_id        INTEGER,
    build_number    TEXT,
    PRIMARY KEY (failure_id, pipeline_name)
);

CREATE TABLE IF NOT EXISTS failure_tests (
    failure_id      INTEGER NOT NULL REFERENCES failures(id),
    pipeline_name   TEXT NOT NULL,
    run_name        TEXT NOT NULL,
    test_name       TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS test_results (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    pipeline_name     TEXT NOT NULL,
    build_id          INTEGER NOT NULL,
    run_name          TEXT NOT NULL,
    test_name         TEXT NOT NULL,
    helix_job_id      TEXT,
    helix_work_item   TEXT,
    console_log_url   TEXT,
    exit_code         INTEGER,              -- from console log (script-extracted)
    console_log_path  TEXT,                 -- path to full console log file on disk
    error_message     TEXT,                 -- initially from AzDO Test Results API; may be enriched/overwritten by agent with console-log snippet
    stack_trace       TEXT,                 -- initially from AzDO Test Results API; may be enriched/overwritten by agent with console-log snippet
    failure_id        INTEGER,
    FOREIGN KEY (failure_id) REFERENCES failures(id)
);
"""


def parse_pipelines_md(path):
    """Parse pipelines.md to get pipeline name → def_id mapping."""
    pipelines = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            # Match table rows: | name | def_id | notes |
            m = re.match(r'\|\s*([^|]+?)\s*\|\s*(\d+|—)\s*\|\s*(.*?)\s*\|', line)
            if not m:
                continue
            name = m.group(1).strip()
            def_id_str = m.group(2).strip()
            notes = m.group(3).strip()

            # Skip header rows
            if name.startswith("Pipeline") or name.startswith("---"):
                continue

            notes_lower = notes.lower()
            is_private = bool(re.search(r'\bprivate', notes_lower))
            is_skip = bool(re.search(r'\bskip', notes_lower))

            if is_private:
                pipelines.append({"name": name, "def_id": None, "skip": True, "skip_reason": "private"})
            elif is_skip:
                pipelines.append({"name": name, "def_id": None, "skip": True, "skip_reason": "skip"})
            elif def_id_str == "—":
                pipelines.append({"name": name, "def_id": None, "skip": True, "skip_reason": "missing_def_id"})
            else:
                pipelines.append({"name": name, "def_id": int(def_id_str), "skip": False})

    return pipelines


def fetch_latest_build(def_id):
    """Fetch the latest completed main branch build for a pipeline definition."""
    url = (f"{ADO_BASE}/build/builds"
           f"?definitions={def_id}&branchName=refs/heads/main"
           f"&statusFilter=completed&$top=1&api-version=7.1")
    try:
        resp = requests.get(url, timeout=30)
    except requests.RequestException:
        return None, url
    if resp.status_code != 200:
        return None, url
    builds = resp.json().get("value", [])
    if not builds:
        return None, url
    return builds[0], url


def main():
    parser = argparse.ArgumentParser(description="Setup DB and fetch pipeline builds")
    parser.add_argument("--pipelines", default="pipelines.md", help="Path to pipelines.md")
    parser.add_argument("--db", default="monitor.db", help="Path to SQLite database")
    args = parser.parse_args()

    # Resolve pipelines.md relative to script directory if not found
    pipelines_path = args.pipelines
    if not os.path.exists(pipelines_path):
        script_dir = os.path.dirname(os.path.abspath(__file__))
        pipelines_path = os.path.join(os.path.dirname(script_dir), "pipelines.md")

    if not os.path.exists(pipelines_path):
        print(f"ERROR: Cannot find pipelines.md at {args.pipelines} or {pipelines_path}", file=sys.stderr)
        sys.exit(1)

    pipelines = parse_pipelines_md(pipelines_path)
    print(f"Parsed {len(pipelines)} pipelines from {pipelines_path}", file=sys.stderr)

    # Step 1: Create/overwrite database
    if os.path.exists(args.db):
        os.remove(args.db)
        print(f"Removed old {args.db}", file=sys.stderr)

    conn = sqlite3.connect(args.db)
    conn.executescript(SCHEMA)
    print(f"Created {args.db} with schema", file=sys.stderr)

    # Step 2: Fetch builds and populate pipelines table
    passed = failed = skipped = 0

    for p in pipelines:
        name = p["name"]
        if p["skip"]:
            conn.execute(
                "INSERT INTO pipelines (name, result, skip_reason) VALUES (?, 'skipped', ?)",
                (name, p.get("skip_reason", "private"))
            )
            conn.commit()
            skipped += 1
            print(f"  SKIP  {name} ({p.get('skip_reason', 'unknown')})", file=sys.stderr)
            continue

        build, url = fetch_latest_build(p["def_id"])
        if not build:
            conn.execute(
                "INSERT INTO pipelines (name, result, skip_reason) VALUES (?, 'skipped', ?)",
                (name, "no builds found")
            )
            conn.commit()
            skipped += 1
            print(f"  SKIP  {name} (no builds)", file=sys.stderr)
            continue

        build_id = build["id"]
        build_number = build.get("buildNumber", "")
        result = build.get("result", "unknown")

        if result == "succeeded":
            conn.execute(
                "INSERT INTO pipelines (name, build_id, build_number, result) VALUES (?, ?, ?, ?)",
                (name, build_id, build_number, result)
            )
            passed += 1
            print(f"  PASS  {name} — build {build_id} ({result})", file=sys.stderr)
        else:
            # failed, partiallySucceeded, canceled, etc. — store as 'failed'.
            # extract_failed_tests.py will refine to 'inconclusive' if 0 test failures.
            conn.execute(
                "INSERT INTO pipelines (name, build_id, build_number, result) VALUES (?, ?, ?, ?)",
                (name, build_id, build_number, "failed")
            )
            failed += 1
            print(f"  FAIL  {name} — build {build_id} ({result})", file=sys.stderr)
        conn.commit()

    conn.close()

    # Summary
    total = passed + failed + skipped
    print(f"\nSummary: {total} pipelines — {passed} passed, {failed} failed, {skipped} skipped", file=sys.stderr)


if __name__ == "__main__":
    main()

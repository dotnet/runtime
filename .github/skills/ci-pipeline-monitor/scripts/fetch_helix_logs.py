"""Fetch Helix console logs and save full log files to disk.

Usage:
    python fetch_helix_logs.py <failures.json> [--db monitor.db] [--logdir helix-logs]

Downloads the full Helix console log for each failed test and saves it as a
file in the log directory.  The script only extracts the exit code (a simple
pattern match).  Error message and stack trace are primarily captured from
the ADO API by extract_failed_tests.py; the LLM reads these console log
files during triage to enrich/complete entries where the API returned
generic or empty error info (crashes, timeouts).

When --db is provided:
    - Reads test_results rows where console_log_path IS NULL
    - Fetches each console log, saves to --logdir
    - UPDATEs each test_results row with exit_code and console_log_path

When --db is NOT provided:
    - Reads the JSON input file (output from extract_failed_tests.py)
    - Outputs JSON to stdout with file paths
"""
import json
import hashlib
import os
import re
import socket
import sqlite3
import sys
import urllib.request


def fetch_and_save(url, out_path):
    """Fetch a Helix console log, save full content to disk, return exit_code."""
    req = urllib.request.Request(url)
    with urllib.request.urlopen(req, timeout=120) as resp:
        text = resp.read().decode('utf-8', errors='replace')

    # Save the full console log to disk
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(text)

    lines = text.strip().split('\n')
    total = len(lines)

    # --- Extract exit code only (simple pattern match) ---
    exit_code = None
    for line in lines:
        m = re.search(r'exit code[:\s]+(-?\d+)', line, re.IGNORECASE)
        if m:
            exit_code = int(m.group(1))
        m2 = re.search(r'_commandExitCode=(\d+)', line)
        if m2:
            exit_code = int(m2.group(1))
        m3 = re.search(r'Exit Code:(\d+)', line)
        if m3:
            exit_code = int(m3.group(1))

    return {
        'total_lines': total,
        'exit_code': exit_code,
        'path': out_path,
    }


def make_log_filename(pipeline_name, run_name, test_name):
    """Create a filesystem-safe filename for a console log."""
    run_hash = hashlib.sha1(run_name.encode()).hexdigest()[:8]
    safe = re.sub(r'[^\w\-.]', '_', f"{pipeline_name}__{test_name}__{run_hash}")
    # Truncate to avoid filesystem limits
    if len(safe) > 200:
        safe = safe[:200]
    return safe + '.log'


def process_from_db(db_path, logdir):
    """Read test_results from DB, fetch logs, save to disk, UPDATE DB."""
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row

    # Get rows that haven't had their console log fetched yet.
    # console_log_path IS NULL means not yet processed.
    rows = conn.execute(
        """SELECT id, pipeline_name, run_name, test_name, console_log_url
           FROM test_results
           WHERE console_log_path IS NULL AND console_log_url IS NOT NULL AND console_log_url != ''"""
    ).fetchall()

    print(f"Found {len(rows)} test_results rows needing log download", file=sys.stderr)

    # Deduplicate by console_log_url — same URL = same log content
    url_to_ids = {}
    for row in rows:
        url = row['console_log_url']
        if url not in url_to_ids:
            url_to_ids[url] = []
        url_to_ids[url].append({
            'id': row['id'],
            'pipeline_name': row['pipeline_name'],
            'run_name': row['run_name'],
            'test_name': row['test_name'],
        })

    print(f"  {len(url_to_ids)} unique console URLs to fetch", file=sys.stderr)

    results = {}
    for url, entries in url_to_ids.items():
        first = entries[0]
        filename = make_log_filename(first['pipeline_name'], first['run_name'], first['test_name'])
        out_path = os.path.join(logdir, filename)

        print(f"Fetching {first['test_name']}...", file=sys.stderr)
        try:
            log_data = fetch_and_save(url, out_path)
            # UPDATE all test_results rows that share this URL
            for entry in entries:
                conn.execute(
                    """UPDATE test_results
                       SET exit_code = ?, console_log_path = ?
                       WHERE id = ?""",
                    (log_data['exit_code'], log_data['path'], entry['id'])
                )
            conn.commit()
            results[first['test_name']] = {
                'url': url,
                'path': log_data['path'],
                'exit_code': log_data['exit_code'],
                'total_lines': log_data['total_lines'],
                'rows_updated': len(entries),
            }
            print(f"  OK: {log_data['total_lines']} lines, exit={log_data['exit_code']}, "
                  f"saved to {log_data['path']}, updated {len(entries)} rows", file=sys.stderr)
        except socket.timeout as e:
            error_type = "timeout"
            results[first['test_name']] = {'url': url, 'error': str(e)}
            print(f"  TIMEOUT: {e}", file=sys.stderr)
            conn.execute(
                "INSERT INTO data_collection_errors (step, pipeline_name, build_id, error_type, detail) VALUES (?, ?, ?, ?, ?)",
                ("fetch_logs", first['pipeline_name'], 0, error_type, f"Console log download timed out: {e}")
            )
            conn.commit()
        except Exception as e:
            error_type = "timeout" if "timed out" in str(e).lower() else "download_failed"
            results[first['test_name']] = {'url': url, 'error': str(e)}
            print(f"  ERROR: {e}", file=sys.stderr)
            conn.execute(
                "INSERT INTO data_collection_errors (step, pipeline_name, build_id, error_type, detail) VALUES (?, ?, ?, ?, ?)",
                ("fetch_logs", first['pipeline_name'], 0, error_type, str(e))
            )
            conn.commit()

    conn.close()
    return results


def process_from_json(input_path, logdir):
    """Read failures from JSON, fetch logs, save to disk, return results."""
    with open(input_path) as f:
        content = f.read()
        if '[' in content:
            idx = content.index('[')
            failures = json.loads(content[idx:])
        else:
            failures = json.loads(content)

    # Deduplicate by console_log_url
    url_to_entries = {}
    for entry in failures:
        url = entry.get('console_log_url', '')
        if url and url not in url_to_entries:
            url_to_entries[url] = entry

    results = {}
    for url, info in url_to_entries.items():
        name = info['test_name']
        pipeline = info.get('pipeline_name', '')
        run = info.get('run_name', '')
        filename = make_log_filename(pipeline, run, name)
        out_path = os.path.join(logdir, filename)

        print(f'Fetching {name}...', file=sys.stderr)
        try:
            log_data = fetch_and_save(url, out_path)
            results[name] = {
                'pipeline_name': pipeline,
                'run_name': run,
                'url': url,
                'path': log_data['path'],
                'total_lines': log_data['total_lines'],
                'exit_code': log_data['exit_code'],
            }
            print(f'  OK: {log_data["total_lines"]} lines, exit={log_data["exit_code"]}, '
                  f'saved to {log_data["path"]}', file=sys.stderr)
        except Exception as e:
            results[name] = {
                'pipeline_name': pipeline,
                'run_name': run,
                'url': url,
                'error': str(e),
            }
            print(f'  ERROR: {e}', file=sys.stderr)

    return results


def main():
    args = sys.argv[1:]
    if not args:
        print("Usage: python fetch_helix_logs.py <failures.json> [--db monitor.db] [--logdir helix-logs]",
              file=sys.stderr)
        sys.exit(1)

    # Parse args
    input_file = None
    db_path = None
    logdir = None

    i = 0
    while i < len(args):
        if args[i] == '--db':
            db_path = args[i + 1]
            i += 2
        elif args[i] == '--logdir':
            logdir = args[i + 1]
            i += 2
        else:
            input_file = args[i]
            i += 1

    # Default logdir: helix-logs/ next to scripts/
    if logdir is None:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        logdir = os.path.join(os.path.dirname(script_dir), 'helix-logs')

    os.makedirs(logdir, exist_ok=True)

    if db_path:
        results = process_from_db(db_path, logdir)
    elif input_file:
        results = process_from_json(input_file, logdir)
    else:
        print("ERROR: Provide either --db or an input JSON file", file=sys.stderr)
        sys.exit(1)

    print(json.dumps(results, indent=2))


if __name__ == '__main__':
    main()

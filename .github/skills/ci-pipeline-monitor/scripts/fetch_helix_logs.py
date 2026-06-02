"""Fetch Helix console logs and save full log files to disk.

Usage:
    python fetch_helix_logs.py --db monitor.db [--logdir helix-logs]

Downloads the full Helix console log for each failed test and saves it as a
file in the log directory.  The script only extracts the exit code (a simple
pattern match).  Error message and stack trace are primarily captured from
the ADO API by extract_failed_tests.py; the LLM reads these console log
files during triage to enrich/complete entries where the API returned
generic or empty error info (crashes, timeouts).

Reads test_results rows where console_log_path IS NULL, fetches each
console log, saves to --logdir, and UPDATEs each test_results row with
exit_code and console_log_path.
"""
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

    # --- Extract exit code ---
    # Check for timeout first (process killed, no meaningful exit code)
    if 'Command timed out' in text:
        return {
            'total_lines': total,
            'exit_code': None,
            'path': out_path,
        }

    # Collect exit codes from log lines
    exit_codes = []
    for line in lines:
        m = re.search(r'exit code[:\s]+(-?\d+)', line, re.IGNORECASE)
        if m:
            exit_codes.append(int(m.group(1)))
        m2 = re.search(r'_commandExitCode=(\d+)', line)
        if m2:
            exit_codes.append(int(m2.group(1)))
        m3 = re.search(r'Exit Code:(\d+)', line)
        if m3:
            exit_codes.append(int(m3.group(1)))

    # Prefer first non-zero exit code (actual test crash) over trailing
    # zero from XUnitLogChecker or Helix wrapper cleanup.
    exit_code = next((c for c in exit_codes if c != 0), exit_codes[-1] if exit_codes else None)

    # Coreclr multi-test convention: App Exit Code 100 means test harness
    # passed, but individual tests may have failed. The work item exits 0.
    # Exit code 100 is not a failure signal — set -1 to indicate
    # "has individual test failures, classify from error messages."
    if exit_code == 100 and 'Command exited with 0' in text:
        exit_code = -1

    return {
        'total_lines': total,
        'exit_code': exit_code,
        'path': out_path,
    }


def make_log_filename(pipeline_name, test_name, console_log_url):
    """Create a filesystem-safe filename for a console log.

    Includes a short hash of the console_log_url to guarantee uniqueness
    even when the human-readable part is truncated.
    """
    url_hash = hashlib.sha256(console_log_url.encode()).hexdigest()[:8]
    safe = re.sub(r'[^\w\-.]', '_', f"{pipeline_name}__{test_name}")
    # Leave room for _<hash>.log suffix (1 + 8 + 4 = 13 chars)
    if len(safe) > 100:
        safe = safe[:100]
    return f"{safe}_{url_hash}.log"


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

    for url, entries in url_to_ids.items():
        first = entries[0]
        filename = make_log_filename(first['pipeline_name'], first['test_name'], url)
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
            print(f"  OK: {log_data['total_lines']} lines, exit={log_data['exit_code']}, "
                  f"saved to {log_data['path']}, updated {len(entries)} rows", file=sys.stderr)
        except socket.timeout as e:
            print(f"  TIMEOUT: {e}", file=sys.stderr)
        except Exception as e:
            print(f"  ERROR: {e}", file=sys.stderr)

    conn.close()


def main():
    args = sys.argv[1:]

    # Parse args
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
            i += 1

    if not db_path:
        print("Usage: python fetch_helix_logs.py --db monitor.db [--logdir helix-logs]",
              file=sys.stderr)
        sys.exit(1)

    # Default logdir: helix-logs/ next to scripts/
    if logdir is None:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        logdir = os.path.join(os.path.dirname(script_dir), 'helix-logs')

    os.makedirs(logdir, exist_ok=True)
    process_from_db(db_path, logdir)


if __name__ == '__main__':
    main()

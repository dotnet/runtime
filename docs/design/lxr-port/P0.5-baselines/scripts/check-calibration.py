#!/usr/bin/env python3
"""Assert that the committed calibration is measured rather than provisional.

P0.4 shipped per-scenario baseline heaps marked ``provisional: true`` and a document claiming a
``calibrate`` mechanism that did not exist (F1). P0.5's deliverable is measured minima, so the
property worth asserting is not "a calibration file is present" but "every scenario in the harness
catalogue has an entry, that entry is not flagged provisional, and its minimum is not silently the
search floor".

The floor case is reported, not failed. Four scenarios legitimately reach the 4 MiB floor on
Workstation, and the honest statement about them is that the scenario's live set is not what binds
them -- which is a caveat on the number, not a defect in it. Failing here would push a future session
toward raising the floor until the warning stops, which would replace a true statement with a
comfortable one.

Per rule 26 this prints the two counts it compared rather than only a verdict.

Usage:
    python check-calibration.py <calibration.json> [--catalogue <ScenarioCatalog.cs>]

Exit codes: 0 pass, 1 fail.
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys


def catalogue_scenarios(catalogue: pathlib.Path) -> set[str]:
    """Scenario ids as the *source* declares them, so the expected count is derived not repeated."""
    if not catalogue.is_file():
        return set()
    text = catalogue.read_text(encoding="utf-8", errors="replace")
    return set(re.findall(r'Id\s*=\s*"([a-z0-9-]+)"', text))


def main() -> int:
    here = pathlib.Path(__file__).resolve()
    default_catalogue = (
        here.parent.parent.parent
        / "harness"
        / "src"
        / "Lxr.Harness.Core"
        / "ScenarioCatalog.cs"
    )

    parser = argparse.ArgumentParser()
    parser.add_argument("calibration", type=pathlib.Path)
    parser.add_argument("--catalogue", type=pathlib.Path, default=default_catalogue)
    args = parser.parse_args()

    if not args.calibration.is_file():
        print(f"FAIL: no calibration at '{args.calibration}'")
        return 1

    try:
        doc = json.loads(args.calibration.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        print(f"FAIL: {args.calibration.name} is not valid JSON: {error}")
        return 1

    entries_list = doc.get("baselines")
    if not isinstance(entries_list, list) or not entries_list:
        print("FAIL: the calibration carries no 'baselines' array")
        return 1
    entries = {e["scenario"]: e for e in entries_list if isinstance(e, dict) and "scenario" in e}
    if len(entries) != len(entries_list):
        print(f"FAIL: {len(entries_list) - len(entries)} baseline entry/entries lack a scenario id")
        return 1

    failures: list[str] = []

    expected = catalogue_scenarios(args.catalogue)
    if expected:
        missing = sorted(expected - set(entries))
        extra = sorted(set(entries) - expected)
        print(
            f"  catalogue declares {len(expected)} scenarios, calibration carries {len(entries)}, "
            f"difference {len(expected) - len(entries)}"
        )
        if missing:
            failures.append(f"not calibrated: {', '.join(missing)}")
        if extra:
            failures.append(f"calibrated but not in the catalogue: {', '.join(extra)}")
    else:
        print(f"  note: no catalogue at '{args.catalogue}'; scenario coverage not cross-checked")

    provisional = sorted(k for k, v in entries.items() if v.get("provisional") is not False)
    print(
        f"  entries {len(entries)}, of which provisional {len(provisional)}, "
        f"measured {len(entries) - len(provisional)}"
    )
    if provisional:
        failures.append(
            "still provisional, so P0.5's measured-minimum deliverable is unmet: "
            + ", ".join(provisional)
        )

    # The floor and non-monotone facts live in each entry's prose 'note', not in a flag, so they are
    # matched there rather than invented as fields that would silently never match.
    floor_bound = sorted(k for k, v in entries.items() if "floor" in str(v.get("note", "")).lower())
    if floor_bound:
        print(
            f"  note: {len(floor_bound)} scenario(s) record a search-floor contact "
            f"({', '.join(floor_bound)}); those minima bound the answer, they do not measure it"
        )

    for key in ("workstationMinimumMb", "serverMinimumMb", "sharedMinimumMb"):
        missing_value = sorted(k for k, v in entries.items() if not isinstance(v.get(key), (int, float)))
        if missing_value:
            failures.append(f"{key} absent or non-numeric for: {', '.join(missing_value)}")
    mismatched = sorted(
        k
        for k, v in entries.items()
        if isinstance(v.get("sharedMinimumMb"), (int, float))
        and v["sharedMinimumMb"] != max(v.get("workstationMinimumMb", 0), v.get("serverMinimumMb", 0))
    )
    print(
        f"  shared minimum equals max(wks, srv) for {len(entries) - len(mismatched)} of "
        f"{len(entries)} scenarios, mismatched {len(mismatched)}"
    )
    if mismatched:
        failures.append(
            "sharedMinimumMb is not max(wks, srv), so the two arms did not run at one absolute "
            f"heap: {', '.join(mismatched)}"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    print(f"PASS: {len(entries)} scenarios calibrated, 0 provisional")
    return 0


if __name__ == "__main__":
    sys.exit(main())

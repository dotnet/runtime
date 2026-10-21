#!/usr/bin/env python3
"""Cross the document/code boundary: assert every capability a design document claims actually
exists in the harness source.

Rule 25. P0.4's gate ran five ways at three hashes from clean extracts and none of it could have
caught F1, because every check compared the document to itself or to the results beside it. The
document said a `calibrate` verb shipped; `RunnerOptions.Parse` accepted four verbs and none of
them was `calibrate`. Nothing in that gate ever looked at the source.

This is deliberately a *sweep*, not a hand-written list of known problems. A list of things already
known to be wrong cannot find the next one. So: extract every harness verb and every `--flag` the
documents mention, extract the same from the source, and print both sets and their difference.

Rule 26. Every check prints what it compared, not just a verdict, and prefers two counts and a
subtraction to a clever exclusion. A regex that cannot match is indistinguishable from an absence
and reports as one, so `--selftest` runs the extractors against fixtures with known answers and
fails if a probe cannot fire.
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

# Flags that appear in the documents but belong to other tools. Each carries the reason it is not a
# harness capability claim; an entry without a reason is not permitted, because a silent allowlist
# is how a real miss gets parked.
FOREIGN_FLAGS: dict[str, str] = {
    "--clr-path": "corerun's own flag, cited from corerun.cpp",
    "--application.options.outputFile": "crank's flag, cited from the ASP.NET benchmark doc",
    "--configuration": "dotnet build",
    "--nologo": "dotnet build",
    "--framework": "dotnet build",
    "--results": "verify-harness.sh / verify-baselines.sh gate flag, not a runner flag",
    "--no-results": "gate flag, not a runner flag",
    "--selftest": "gate flag, not a runner flag",
    "--help": "universal",
    "--version": "dotnet",
    "--cd": "wsl's flag, cited in P0.1-reference-build.md as the way to avoid bash -c quoting",
    "--disable-warnings-as-errors": "GCC/JDK build flag from the P0.1 reference build",
    "--features": "cargo's flag; LXR is selected at compile time in the reference implementation",
}

# A flag inside a fenced code block is a harness capability claim when the block invokes the
# harness. Without this the sweep sees only inline backticked spans - 10 of the 20 flag tokens in
# P0.4-harness.md - and an audit that inspects half the claims reports the other half as absent.
HARNESS_MARKER = re.compile(
    r"Lxr\.Harness\.Runner|run-p05-baselines|verify-harness|verify-baselines|check-doc-code"
)
FENCE = re.compile(r"^\s*```")
ANY_FLAG = re.compile(r"(--[a-z][a-z0-9-]*(?:\.[A-Za-z][A-Za-z0-9]*)*)")
RUNNER_INVOCATION = re.compile(r"Lxr\.Harness\.Runner(?:\.dll)?\s+([a-z][a-z-]*)")

VERB_LINE = re.compile(r'case\s+((?:"[a-z-]+"\s*(?:or\s*)?)+)\s*:')
QUOTED_VERB = re.compile(r'"([a-z-]+)"')
DOC_FLAG = re.compile(r"`(--[a-z][a-z0-9-]*(?:\.[A-Za-z][A-Za-z0-9]*)*)")
SRC_FLAG = re.compile(r'"(--[a-z][a-z0-9-]*)"')


def source_verbs(options_cs: pathlib.Path) -> set[str]:
    """Subcommands RunnerOptions.Parse accepts.

    Option cases are excluded by shape: a verb never begins with '--'. Without this filter the set
    is the union of verbs and flags, which still answers the membership question correctly but
    prints a list labelled 'verbs' that is mostly flags - and a wrong label is how the next reader
    is misled by a check that happens to be right.
    """
    text = options_cs.read_text(encoding="utf-8", errors="replace")
    verbs: set[str] = set()
    for match in VERB_LINE.finditer(text):
        verbs.update(v for v in QUOTED_VERB.findall(match.group(1)) if not v.startswith("--"))
    return verbs


def source_flags(root: pathlib.Path) -> set[str]:
    flags: set[str] = set()
    for path in sorted(root.rglob("*.cs")):
        flags.update(SRC_FLAG.findall(path.read_text(encoding="utf-8", errors="replace")))
    return flags


def scan_markdown(text: str, origin: str) -> dict[str, list[str]]:
    """Flags a document claims: inline backticked spans, plus fenced blocks that invoke the harness.

    Correction lines are excluded. A line marked as a correction is quoting a claim in order to say
    it was false; counting it as a live claim would make every correction a fresh violation, which
    would teach the next reader to delete corrections rather than write them.
    """
    found: dict[str, list[str]] = {}
    lines = text.splitlines()

    def live(line: str) -> bool:
        return not line.lstrip().startswith(">") and "~~" not in line

    for number, line in enumerate(lines, 1):
        if live(line):
            for flag in DOC_FLAG.findall(line):
                found.setdefault(flag, []).append(f"{origin}:{number}")

    index = 0
    while index < len(lines):
        if not FENCE.match(lines[index]):
            index += 1
            continue
        start = index
        index += 1
        body: list[tuple[int, str]] = []
        while index < len(lines) and not FENCE.match(lines[index]):
            body.append((index + 1, lines[index]))
            index += 1
        index += 1
        block = "\n".join(text for _, text in body)
        if not HARNESS_MARKER.search(block):
            continue
        for number, line in body:
            if not live(line):
                continue
            for flag in ANY_FLAG.findall(line):
                sites = found.setdefault(flag, [])
                site = f"{origin}:{number}"
                if site not in sites:
                    sites.append(site)
        _ = start

    return found


def doc_flags(docs: list[pathlib.Path]) -> dict[str, list[str]]:
    found: dict[str, list[str]] = {}
    for doc in docs:
        for flag, sites in scan_markdown(
            doc.read_text(encoding="utf-8", errors="replace"), doc.name
        ).items():
            found.setdefault(flag, []).extend(sites)
    return found


def doc_verbs(docs: list[pathlib.Path]) -> dict[str, list[str]]:
    """Runner subcommands the documents actually invoke, from harness command lines.

    Matched positionally: the token immediately after the runner assembly on a command line is the
    verb. That is the same position RunnerOptions.Parse reads, so a mismatch here is a mismatch
    there rather than a resemblance.
    """
    found: dict[str, list[str]] = {}
    for doc in docs:
        text = doc.read_text(encoding="utf-8", errors="replace")
        for number, line in enumerate(text.splitlines(), 1):
            if line.lstrip().startswith(">") or "~~" in line:
                continue
            for match in RUNNER_INVOCATION.finditer(line):
                found.setdefault(match.group(1), []).append(f"{doc.name}:{number}")
    return found


def selftest() -> int:
    """Prove each extractor can fire before any absence it reports is believed."""
    ok = True

    verbs = QUOTED_VERB.findall('case "matrix" or "controls" or "conformance" or "hosts":')
    expected = ["matrix", "controls", "conformance", "hosts"]
    print(f"  selftest verb extractor: expected {expected}, got {verbs}")
    ok &= verbs == expected

    flags = DOC_FLAG.findall("text `--heap-factor` and `--calibration-floor-mb` here")
    print(f"  selftest doc-flag extractor: expected 2, got {len(flags)} {flags}")
    ok &= flags == ["--heap-factor", "--calibration-floor-mb"]

    src = SRC_FLAG.findall('case "--heap-factor": x = 1; case "--seed":')
    print(f"  selftest src-flag extractor: expected 2, got {len(src)} {src}")
    ok &= src == ["--heap-factor", "--seed"]

    skipped = doc_flags_from_text("> quoting `--ghost-flag` inside a correction")
    print(f"  selftest correction lines are skipped: expected 0 live claims, got {len(skipped)}")
    ok &= not skipped

    harness_block = "```\ndotnet Lxr.Harness.Runner.dll matrix --scenario x --heap-factor 1.3\n```"
    got = sorted(scan_markdown(harness_block, "fixture"))
    print(f"  selftest fenced harness block: expected ['--heap-factor', '--scenario'], got {got}")
    ok &= got == ["--heap-factor", "--scenario"]

    # The near-miss matters as much as the hit: a block belonging to another tool must not be read
    # as a harness capability claim, or the allowlist becomes the audit.
    foreign_block = "```\ncargo build --features lxr,immix --release\n```"
    got_foreign = sorted(scan_markdown(foreign_block, "fixture"))
    print(f"  selftest fenced non-harness block ignored: expected [], got {got_foreign}")
    ok &= got_foreign == []

    invoked = RUNNER_INVOCATION.findall("dotnet Lxr.Harness.Runner.dll calibrate --scenario x")
    print(f"  selftest verb-invocation extractor: expected ['calibrate'], got {invoked}")
    ok &= invoked == ["calibrate"]

    print(f"  selftest: {'PASS' if ok else 'FAIL'}")
    return 0 if ok else 1


def doc_flags_from_text(text: str) -> list[str]:
    out: list[str] = []
    for line in text.splitlines():
        if line.lstrip().startswith(">") or "~~" in line:
            continue
        out.extend(DOC_FLAG.findall(line))
    return out


def main() -> int:
    here = pathlib.Path(__file__).resolve()
    # scripts -> P0.5-baselines -> lxr-port
    default_lxr = here.parent.parent.parent

    parser = argparse.ArgumentParser()
    parser.add_argument("--lxr-root", type=pathlib.Path, default=default_lxr)
    parser.add_argument("--selftest", action="store_true")
    args = parser.parse_args()

    if args.selftest:
        return selftest()

    lxr = args.lxr_root.resolve()
    src = lxr / "harness" / "src"
    options_cs = src / "Lxr.Harness.Runner" / "RunnerOptions.cs"
    if not options_cs.is_file():
        print(f"FAIL: no RunnerOptions.cs under '{src}'; cannot audit the document/code boundary")
        return 2

    docs = sorted(p for p in lxr.glob("*.md"))
    if not docs:
        print(f"FAIL: no design documents under '{lxr}'")
        return 2

    print(f"documents audited ({len(docs)}): {', '.join(d.name for d in docs)}")

    verbs = source_verbs(options_cs)
    print(f"verbs accepted by RunnerOptions.Parse ({len(verbs)}): {', '.join(sorted(verbs))}")

    flags_src = source_flags(src)
    claims = doc_flags(docs)
    claimed = {f for f in claims if f not in FOREIGN_FLAGS}
    foreign = {f for f in claims if f in FOREIGN_FLAGS}

    # The verb check, which is F1's exact shape: P0.4-harness.md described a `calibrate` mode that
    # RunnerOptions.Parse did not accept. A flag sweep alone would not have caught it, because the
    # claim was the subcommand, not an option.
    verb_claims = doc_verbs(docs)
    bad_verbs = sorted(v for v in verb_claims if v not in verbs)
    print(f"runner verbs invoked by documents: {len(verb_claims)} "
          f"({', '.join(sorted(verb_claims)) or 'none'})")
    print(f"invoked-but-unaccepted:            {len(verb_claims)} - "
          f"{len(verb_claims) - len(bad_verbs)} accepted = {len(bad_verbs)}")

    print(f"flags defined in harness source: {len(flags_src)}")
    print(f"flags claimed by documents:      {len(claims)} "
          f"({len(claimed)} harness, {len(foreign)} belonging to other tools)")

    missing = sorted(claimed - flags_src)
    print(f"claimed-but-absent:              {len(claimed)} - "
          f"{len(claimed) - len(missing)} present = {len(missing)}")

    for flag in sorted(foreign):
        print(f"  foreign, not audited: {flag} - {FOREIGN_FLAGS[flag]}")

    if missing or bad_verbs:
        print()
        print("FAIL: the documents claim capabilities the source does not have:")
        for flag in missing:
            print(f"  flag {flag}  claimed at {', '.join(claims[flag])}")
        for verb in bad_verbs:
            print(f"  verb {verb}  invoked at {', '.join(verb_claims[verb])} "
                  f"but RunnerOptions.Parse accepts only: {', '.join(sorted(verbs))}")
        return 1

    print()
    print(f"PASS: every one of the {len(claimed)} harness flags and {len(verb_claims)} runner verbs "
          f"the documents claim exists in source.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

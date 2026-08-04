#!/usr/bin/env python3
"""
Disable BUILD.dsc targets that failed in a BXL log by removing them.

Reads `Out/Logs/BuildXL.Dev.log`, extracts (build_path, pip_value_name)
pairs for failed `csc [exe]` pips, and removes the corresponding
`@@public export const ... = CoreClr.coreclr_test({ ... });` blocks from
each BUILD.dsc.

We match by `name:` literal inside the call, not the export identifier,
since the identifier is auto-generated. After removal, if the file ends
up with zero exports, we delete it entirely.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


_BUILD_FAILED_CSC_RE = re.compile(
    r"\bcsc \[exe\]: ([^,]+), (/[^,]+/BUILD\.dsc), [^]]+] - failed with exit code"
)
_BUILD_FAILED_ILASM_RE = re.compile(
    r"\|\| ilasm (\S+), (/[^,]+/BUILD\.dsc), [^]]+] - failed with exit code"
)
_TEST_FAILED_RE = re.compile(
    r"\brun (?:coreclr )?test: ([^,]+), (/[^,]+/BUILD\.dsc), [^]]+] - failed with exit code"
)


def _collect_failures(log_path: Path, mode: str) -> dict[Path, set[str]]:
    failures: dict[Path, set[str]] = {}
    text = log_path.read_text(errors="replace")
    if mode == "build":
        regexes = [_BUILD_FAILED_CSC_RE, _BUILD_FAILED_ILASM_RE]
    else:
        regexes = [_TEST_FAILED_RE]
    for rx in regexes:
        for m in rx.finditer(text):
            target_name = m.group(1).strip()
            # The test runner appends "_test" to the BUILD.dsc name: field.
            # Strip it so we can match back to the source definition.
            if mode == "test" and target_name.endswith("_test"):
                target_name = target_name[:-5]
            build_path = Path(m.group(2).strip())
            failures.setdefault(build_path, set()).add(target_name)
    return failures


_BLOCK_RE = re.compile(
    r"@@public\s*\n\s*export\s+const\s+\w+\s*=\s*CoreClr\.(?:coreclr_test|il_coreclr_test)\(\s*\{"
    r"(?P<body>[^}]*)"
    r"\}\s*\)\s*;\s*\n?",
    re.MULTILINE,
)


def _remove_blocks(text: str, names_to_remove: set[str]) -> tuple[str, int]:
    removed = 0
    out_chunks: list[str] = []
    pos = 0
    for m in _BLOCK_RE.finditer(text):
        nm = re.search(r'\bname\s*:\s*"([^"]+)"', m.group("body"))
        if nm and nm.group(1) in names_to_remove:
            out_chunks.append(text[pos:m.start()])
            pos = m.end()
            removed += 1
    out_chunks.append(text[pos:])
    new_text = "".join(out_chunks)
    new_text = re.sub(r"\n{3,}", "\n\n", new_text)
    return new_text, removed


def _disable_run(text: str, names_to_disable: set[str]) -> tuple[str, int]:
    disabled = 0
    out_chunks: list[str] = []
    pos = 0
    for m in _BLOCK_RE.finditer(text):
        body = m.group("body")
        nm = re.search(r'\bname\s*:\s*"([^"]+)"', body)
        if not nm or nm.group(1) not in names_to_disable:
            continue
        if re.search(r"\brun\s*:", body):
            new_body = re.sub(r"\brun\s*:\s*(?:true|false)", "run: false", body)
        else:
            indent = re.search(r"\n(\s+)\S", body)
            ind = indent.group(1) if indent else "    "
            new_body = body.rstrip() + f"\n{ind}run: false,\n"
        new_block = m.group(0).replace(body, new_body)
        out_chunks.append(text[pos:m.start()])
        out_chunks.append(new_block)
        pos = m.end()
        disabled += 1
    out_chunks.append(text[pos:])
    return "".join(out_chunks), disabled


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--log", default="Out/Logs/BuildXL.Dev.log")
    ap.add_argument("--mode", choices=["build", "test"], default="build",
                    help="build: remove broken-compile targets. test: set run:false on failing tests.")
    args = ap.parse_args()

    failures = _collect_failures(Path(args.log), args.mode)
    if not failures:
        print(f"No {args.mode} failures found in log.")
        return 0
    total = 0
    files_modified = 0
    files_deleted = 0
    for build_path, names in sorted(failures.items()):
        if not build_path.exists():
            print(f"  skip (no such file): {build_path}")
            continue
        original = build_path.read_text()
        if args.mode == "build":
            new_text, count = _remove_blocks(original, names)
        else:
            new_text, count = _disable_run(original, names)
        if count == 0:
            print(f"  skip (no match in file): {build_path}: {sorted(names)}")
            continue
        total += count
        if args.mode == "build" and "CoreClr.coreclr_test" not in new_text and "CoreClr.il_coreclr_test" not in new_text:
            build_path.unlink()
            files_deleted += 1
        else:
            build_path.write_text(new_text)
            files_modified += 1
        verb = "removed" if args.mode == "build" else "disabled"
        print(f"{build_path}: {verb} {count}")

    verb = "removed" if args.mode == "build" else "disabled"
    print(f"\nTotal {verb}:  {total}")
    print(f"Files modified: {files_modified}")
    print(f"Files deleted:  {files_deleted}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

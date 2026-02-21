#!/usr/bin/env python3
"""
Security surface triage scanner for dotnet/runtime.

Scans source files for security-sensitive patterns and outputs a prioritized
JSON file list. Designed to run fast (<5s) with no dependencies beyond stdlib.

Usage:
    python scan_security_surface.py <path> [--json] [--min-priority medium]
    python scan_security_surface.py src/libraries/System.Net.Http/src --json
    python scan_security_surface.py --diff  # scan git diff files only
"""

import argparse
import json
import os
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

# ---------------------------------------------------------------------------
# Pattern definitions — each maps to a security signal category
# ---------------------------------------------------------------------------

PATTERNS: dict[str, list[re.Pattern]] = {
    # Memory safety / unsafe code
    "unsafe_code": [
        re.compile(r"\bunsafe\b"),
        re.compile(r"\bstackalloc\b"),
        re.compile(r"\bUnsafe\.As\b"),
        re.compile(r"\bMemoryMarshal\b"),
        re.compile(r"\bfixed\s*\("),
    ],
    # Native interop
    "native_interop": [
        re.compile(r"\bDllImport\b"),
        re.compile(r"\bLibraryImport\b"),
        re.compile(r"\bMarshal\.\w+"),
        re.compile(r"\bSafeHandle\b"),
        re.compile(r"\bCriticalHandle\b"),
        re.compile(r"\bNativeMemory\b"),
    ],
    # Serialization surface (ALL serializers)
    "serialization": [
        re.compile(r"\bBinaryFormatter\b"),
        re.compile(r"\bSoapFormatter\b"),
        re.compile(r"\bJsonSerializer\b"),
        re.compile(r"\bJsonConverter\b"),
        re.compile(r"\bJsonDocument\b"),
        re.compile(r"\bUtf8JsonReader\b"),
        re.compile(r"\bUtf8JsonWriter\b"),
        re.compile(r"\bXmlSerializer\b"),
        re.compile(r"\bXmlReader\b"),
        re.compile(r"\bDataContractSerializer\b"),
        re.compile(r"\bTypeNameHandling\b"),
        re.compile(r"\bISerializable\b"),
        re.compile(r"\bTypeConverter\b"),
        re.compile(r"\bSerializationBinder\b"),
        re.compile(r"\b\[KnownType\b"),
    ],
    # DOS / resource exhaustion
    "dos_surface": [
        re.compile(r"\bReadToEnd\b"),
        re.compile(r"\bReadToEndAsync\b"),
        re.compile(r"\bCopyTo\b(?!Array)"),
        re.compile(r"\bCopyToAsync\b"),
        re.compile(r"\bToArray\s*\("),
        re.compile(r"\bnew\s+byte\s*\["),
        re.compile(r"\bnew\s+char\s*\["),
        re.compile(r"\bStringBuilder\s*\(\s*\)"),  # no capacity
        re.compile(r"\bMemoryStream\s*\(\s*\)"),   # no capacity
    ],
    # Cryptography
    "crypto": [
        re.compile(r"\bMD5\b"),
        re.compile(r"\bSHA1\b(?!Managed)"),
        re.compile(r"\bDES\b"),
        re.compile(r"\b(TripleDES|3DES)\b"),
        re.compile(r"\bRC[24]\b"),
        re.compile(r"\bAes\b"),
        re.compile(r"\bRSA\b"),
        re.compile(r"\bRandomNumberGenerator\b"),
        re.compile(r"\bFixedTimeEquals\b"),
        re.compile(r"\bCertificateValidationCallback\b"),
        re.compile(r"\bX509Certificate\b"),
    ],
    # Input validation / injection
    "injection": [
        re.compile(r"\bProcess\.Start\b"),
        re.compile(r"\bProcessStartInfo\b"),
        re.compile(r"\bPath\.Combine\b"),
        re.compile(r"\bPath\.GetFullPath\b"),
        re.compile(r"\bDtdProcessing\b"),
    ],
    # Authentication / authorization
    "auth": [
        re.compile(r"\bBindingFlags\.NonPublic\b"),
        re.compile(r"\bAllowPartiallyTrustedCallers\b"),
        re.compile(r"\bSecurityCritical\b"),
        re.compile(r"\bSecurityTransparent\b"),
        re.compile(r"//\s*SECURITY\b"),
    ],
    # Network input surface
    "network": [
        re.compile(r"\bHttpClient\b"),
        re.compile(r"\bSocket\b"),
        re.compile(r"\bTcpListener\b"),
        re.compile(r"\bSslStream\b"),
        re.compile(r"\bHttpListener\b"),
        re.compile(r"\bWebSocket\b"),
    ],
    # Public API entry points (potential untrusted input)
    "public_api": [
        re.compile(
            r"public\s+(?:static\s+)?(?:async\s+)?\S+\s+\w+\s*\("
            r"[^)]*(?:string|byte\[\]|Stream|ReadOnlySpan|ReadOnlyMemory|"
            r"ReadOnlySequence|Memory<|Span<)[^)]*\)"
        ),
    ],
    # Contract signals (documented preconditions)
    "contract_signal": [
        re.compile(r"///\s*<exception\s"),
        re.compile(r"\bArgumentException\b"),
        re.compile(r"\bArgumentNullException\b"),
        re.compile(r"\bArgumentOutOfRangeException\b"),
        re.compile(r"\bObjectDisposedException\b"),
        re.compile(r"\bInvalidOperationException\b"),
    ],
}

# Files that are always high priority regardless of patterns
ALWAYS_HIGH_EXTENSIONS = {".c", ".cpp", ".h", ".cc", ".cxx"}

# Files to skip entirely
SKIP_PATTERNS = [
    re.compile(r"[\\/]tests?[\\/]", re.IGNORECASE),
    re.compile(r"[\\/]ref[\\/]"),
    re.compile(r"\.Designer\.cs$"),
    re.compile(r"\.g\.cs$"),
    re.compile(r"\.generated\.cs$"),
    re.compile(r"[\\/]obj[\\/]"),
    re.compile(r"[\\/]bin[\\/]"),
]

SCANNABLE_EXTENSIONS = {
    ".cs", ".c", ".cpp", ".h", ".cc", ".cxx", ".fs", ".vb",
}


def should_skip(path: str) -> bool:
    return any(p.search(path) for p in SKIP_PATTERNS)


def scan_file(filepath: str) -> dict:
    """Scan a single file and return its security signals."""
    signals: dict[str, int] = defaultdict(int)
    try:
        with open(filepath, "r", encoding="utf-8", errors="replace") as f:
            content = f.read()
    except (OSError, UnicodeDecodeError):
        return {"signals": {}, "total": 0}

    for category, patterns in PATTERNS.items():
        for pattern in patterns:
            matches = pattern.findall(content)
            if matches:
                signals[category] += len(matches)

    total = sum(signals.values())
    return {"signals": dict(signals), "total": total}


def prioritize(filepath: str, scan_result: dict) -> str:
    """Assign priority: high, medium, low, or skip."""
    ext = Path(filepath).suffix.lower()

    # Native code is always high
    if ext in ALWAYS_HIGH_EXTENSIONS:
        return "high"

    total = scan_result["total"]
    signals = scan_result["signals"]

    # Critical signal categories always bump to high
    critical = {"unsafe_code", "serialization", "native_interop", "crypto", "injection"}
    has_critical = any(signals.get(c, 0) > 0 for c in critical)

    if has_critical and total >= 3:
        return "high"
    if has_critical or total >= 5:
        return "medium"
    if total >= 2:
        return "low"
    if total >= 1:
        return "low"
    return "skip"


def collect_files(path: str) -> list[str]:
    """Recursively collect scannable source files."""
    result = []
    for root, _dirs, files in os.walk(path):
        for f in files:
            full = os.path.join(root, f)
            ext = Path(full).suffix.lower()
            if ext in SCANNABLE_EXTENSIONS and not should_skip(full):
                result.append(full)
    return sorted(result)


def collect_diff_files() -> list[str]:
    """Collect files changed vs origin/HEAD."""
    try:
        output = subprocess.check_output(
            ["git", "diff", "--name-only", "--merge-base", "origin/HEAD"],
            text=True,
        )
    except subprocess.CalledProcessError:
        print("Error: git diff failed. Are you in a git repo?", file=sys.stderr)
        sys.exit(1)

    files = []
    for line in output.strip().splitlines():
        line = line.strip()
        if line and Path(line).suffix.lower() in SCANNABLE_EXTENSIONS and not should_skip(line):
            files.append(line)
    return sorted(files)


def main():
    parser = argparse.ArgumentParser(
        description="Scan source files for security-sensitive patterns."
    )
    parser.add_argument(
        "path", nargs="?", default=".",
        help="Directory or file to scan (default: current directory)",
    )
    parser.add_argument(
        "--diff", action="store_true",
        help="Scan only files changed vs origin/HEAD",
    )
    parser.add_argument(
        "--json", action="store_true",
        help="Output JSON instead of human-readable table",
    )
    parser.add_argument(
        "--min-priority", choices=["high", "medium", "low", "skip"],
        default="low",
        help="Minimum priority to include in output (default: low)",
    )
    args = parser.parse_args()

    priority_order = ["high", "medium", "low", "skip"]
    min_idx = priority_order.index(args.min_priority)

    if args.diff:
        files = collect_diff_files()
    elif os.path.isfile(args.path):
        files = [args.path]
    else:
        files = collect_files(args.path)

    results = []
    for filepath in files:
        scan = scan_file(filepath)
        priority = prioritize(filepath, scan)
        if priority_order.index(priority) <= min_idx:
            results.append({
                "path": filepath,
                "priority": priority,
                "signals": sorted(scan["signals"].keys()),
                "signalCount": scan["total"],
                "signalDetails": scan["signals"],
            })

    # Sort: high first, then medium, then by signal count descending
    results.sort(key=lambda r: (priority_order.index(r["priority"]), -r["signalCount"]))

    summary = {
        "totalFiles": len(files),
        "securityRelevant": sum(1 for r in results if r["priority"] != "skip"),
        "high": sum(1 for r in results if r["priority"] == "high"),
        "medium": sum(1 for r in results if r["priority"] == "medium"),
        "low": sum(1 for r in results if r["priority"] == "low"),
    }

    if args.json:
        output = {"summary": summary, "files": results}
        print(json.dumps(output, indent=2))
    else:
        print(f"Scanned {summary['totalFiles']} files: "
              f"{summary['high']} high, {summary['medium']} medium, "
              f"{summary['low']} low, "
              f"{summary['totalFiles'] - summary['securityRelevant']} skipped")
        print()
        for r in results:
            signals_str = ", ".join(r["signals"])
            print(f"  [{r['priority'].upper():6s}] {r['path']}")
            print(f"           signals: {signals_str} ({r['signalCount']} hits)")


if __name__ == "__main__":
    main()

#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Validation script for runtime.json after pruning
# Verifies that the pruning was done correctly according to the design requirements

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
RUNTIME_JSON="$SCRIPT_DIR/../../libraries/Microsoft.NETCore.Platforms/src/runtime.json"

echo "Validating runtime.json pruning..."
echo "File: $RUNTIME_JSON"
echo ""

# Check 1: No Alpine RIDs
ALPINE_COUNT=$(grep -c '"alpine' "$RUNTIME_JSON" || true)
if [ "$ALPINE_COUNT" -gt 0 ]; then
    echo "❌ FAILED: Found $ALPINE_COUNT Alpine RID references (expected 0)"
    exit 1
fi
echo "✓ No Alpine RIDs found"

# Check 2: No Ubuntu RIDs
UBUNTU_COUNT=$(grep -c '"ubuntu' "$RUNTIME_JSON" || true)
if [ "$UBUNTU_COUNT" -gt 0 ]; then
    echo "❌ FAILED: Found $UBUNTU_COUNT Ubuntu RID references (expected 0)"
    exit 1
fi
echo "✓ No Ubuntu RIDs found"

# Check 3: No Debian RIDs
DEBIAN_COUNT=$(grep -c '"debian' "$RUNTIME_JSON" || true)
if [ "$DEBIAN_COUNT" -gt 0 ]; then
    echo "❌ FAILED: Found $DEBIAN_COUNT Debian RID references (expected 0)"
    exit 1
fi
echo "✓ No Debian RIDs found"

# Check 4: No versioned OSX RIDs (osx.)
OSX_VERSIONED_COUNT=$(grep -c '"osx\.' "$RUNTIME_JSON" || true)
if [ "$OSX_VERSIONED_COUNT" -gt 0 ]; then
    echo "❌ FAILED: Found $OSX_VERSIONED_COUNT versioned OSX RID references (expected 0)"
    exit 1
fi
echo "✓ No versioned OSX RIDs found"

# Check 5: No versioned FreeBSD RIDs (freebsd.)
FREEBSD_VERSIONED_COUNT=$(grep -c '"freebsd\.' "$RUNTIME_JSON" || true)
if [ "$FREEBSD_VERSIONED_COUNT" -gt 0 ]; then
    echo "❌ FAILED: Found $FREEBSD_VERSIONED_COUNT versioned FreeBSD RID references (expected 0)"
    exit 1
fi
echo "✓ No versioned FreeBSD RIDs found"

# Check 6: Host RIDs are present
REQUIRED_RIDS=(
    "win"
    "unix"
    "linux"
    "osx"
    "win-x64"
    "win-x86"
    "win-arm64"
    "osx-x64"
    "osx-arm64"
    "linux-x64"
    "linux-arm64"
    "linux-musl-x64"
    "linux-s390x"
    "linux-ppc64le"
    "linux-riscv64"
    "linux-loongarch64"
)

for rid in "${REQUIRED_RIDS[@]}"; do
    if ! grep -q "\"$rid\":" "$RUNTIME_JSON"; then
        echo "❌ FAILED: Required RID '$rid' not found"
        exit 1
    fi
done
echo "✓ All required host RIDs are present"

echo ""
echo "✅ All validation checks passed!"

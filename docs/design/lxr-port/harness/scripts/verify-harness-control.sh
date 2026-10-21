#!/usr/bin/env bash
# The gate of the gate.
#
# A verification script that has not been shown to fail is indistinguishable from one that always
# passes. This runs verify-harness.sh three times:
#
#   A  against a clean extract of committed content, outside any worktree   -> expect PASS
#   B  against a copy of that extract with exactly one thing perturbed      -> expect FAIL
#   C  against the untouched original again                                 -> expect PASS
#
# B is the whole point. A and C alone would pass equally happily if the gate were reading a
# different tree than the one it was pointed at - which is exactly what happened to P0.3's gate.
#
# Usage: verify-harness-control.sh [repo-root]

set -uo pipefail

SELF_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

if [[ $# -ge 1 ]]; then
    REPO=$(cd "$1" && pwd)
else
    REPO=$(cd "$SELF_DIR/../../../../.." && pwd)
fi

if ! git -C "$REPO" rev-parse --git-dir >/dev/null 2>&1; then
    printf 'not a git checkout: %s\n' "$REPO"
    exit 2
fi

# Extract outside any worktree, so a gate that ignored its argument and walked up to a worktree
# would be caught rather than accidentally succeeding.
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

printf 'gate-of-the-gate\n'
printf 'repo:    %s\n' "$REPO"
printf 'scratch: %s\n' "$WORK"

EXTRACT="$WORK/extract"
mkdir -p "$EXTRACT"

# Extract committed content only. Whatever is in the working tree is irrelevant: the artifact under
# audit is what is committed.
if ! git -C "$REPO" archive HEAD docs/design/lxr-port | tar -x -C "$EXTRACT"; then
    printf 'git archive failed - are the P0.4 artifacts committed?\n'
    exit 2
fi

GATE="$EXTRACT/docs/design/lxr-port/harness/scripts/verify-harness.sh"
if [[ ! -f "$GATE" ]]; then
    printf 'the extract has no gate at %s\n' "${GATE#"$WORK/"}"
    exit 2
fi

printf '\nextracted %d file(s) under docs/design/lxr-port\n' "$(find "$EXTRACT" -type f | wc -l)"
printf 'gate line endings in the extract: '
if grep -q $'\r' "$GATE"; then
    printf 'CRLF - bash cannot execute this, so any pass would attest to a conversion, not the artifact\n'
    exit 2
fi
printf 'LF\n'

# The extract has no smoke results (they live in the gitignored artifacts tree), so point the gate
# at a root that has both the extracted committed content and the real results directory. Symlink
# rather than copy, and fall back to a copy where symlinks are unavailable.
prepare_root() {
    local dest="$1"
    if [[ -d "$REPO/artifacts/lxr-harness" ]]; then
        mkdir -p "$dest/artifacts"
        ln -s "$REPO/artifacts/lxr-harness" "$dest/artifacts/lxr-harness" 2>/dev/null \
            || cp -r "$REPO/artifacts/lxr-harness" "$dest/artifacts/lxr-harness"
    fi
}

prepare_root "$EXTRACT"

run_gate() {
    local root="$1"
    local label="$2"
    local out
    # --results is passed explicitly rather than left to inference. The gate now refuses to guess when
    # a results directory is empty, because inferring "skip the results checks" from their absence is
    # what let an extract print PASS while the collector-identity checks had not run at all.
    out=$(bash "$root/docs/design/lxr-port/harness/scripts/verify-harness.sh" "$root" \
        --results "$root/artifacts/lxr-harness/runs" 2>&1)
    local code=$?
    printf '%s\n' "$out" | tail -n 3 | sed 's/^/      /'
    printf '      exit=%d\n' "$code"
    LAST_OUTPUT="$out"
    return $code
}

STATUS=0

# --- A ---------------------------------------------------------------------
printf '\n[A] committed extract, untouched\n'
if run_gate "$EXTRACT" "A"; then
    printf '  A PASSED as expected\n'
else
    printf '  A FAILED - the committed artifact does not pass its own gate\n'
    printf '%s\n' "$LAST_OUTPUT" | grep -E '^  FAIL' | sed 's/^/      /'
    STATUS=1
fi

# --- B ---------------------------------------------------------------------
# Perturb exactly one thing: delete one scenario row from the document's matrix table. That should
# break the "document and source agree" check and nothing else, so a gate that fails here for some
# other reason is also caught.
printf '\n[B] perturbed copy - one scenario row removed from the matrix table\n'
COPY="$WORK/perturbed"
# Copy only the committed subtree. Copying $EXTRACT wholesale would drag in whatever prepare_root
# put at artifacts/lxr-harness, which on a host without working symlinks is a real (and large)
# directory that the follow-up prepare_root then cannot overwrite.
mkdir -p "$COPY/docs/design"
cp -r "$EXTRACT/docs/design/lxr-port" "$COPY/docs/design/lxr-port"
prepare_root "$COPY"

DOC_COPY="$COPY/docs/design/lxr-port/P0.4-harness.md"
VICTIM=$(grep -nE '^\| [0-9]+ \| `[a-z-]+` \|' "$DOC_COPY" | tail -n 1 | cut -d: -f1)
if [[ -z "$VICTIM" ]]; then
    printf '  could not find a scenario row to remove; the table format changed\n'
    STATUS=1
else
    VICTIM_ID=$(sed -n "${VICTIM}p" "$DOC_COPY" | grep -oE '`[a-z-]+`' | head -n 1 | tr -d '`')
    printf '  removing line %s (%s)\n' "$VICTIM" "$VICTIM_ID"
    sed -i "${VICTIM}d" "$DOC_COPY"

    if run_gate "$COPY" "B"; then
        printf '  B PASSED, which means the gate did not notice a missing scenario.\n'
        printf '  A gate that cannot fail is indistinguishable from an absent one.\n'
        STATUS=1
    else
        printf '  B FAILED as required. The gate detects a scenario missing from the document.\n'
        printf '%s\n' "$LAST_OUTPUT" | grep -E '^  FAIL' | head -n 4 | sed 's/^/      /'
    fi
fi

# --- C ---------------------------------------------------------------------
printf '\n[C] original extract again, to prove B did not contaminate it\n'
if run_gate "$EXTRACT" "C"; then
    printf '  C PASSED as expected\n'
else
    printf '  C FAILED - the perturbation leaked into the original\n'
    STATUS=1
fi

# --- D ---------------------------------------------------------------------
# The mode itself is a control. Before this existed, running the gate the way an auditor naturally
# would - git archive extract, no arguments - silently dropped the entire results section, including
# the collector-identity checks that carry P0.4's stated correctness criterion, and still printed an
# unqualified PASS. Three modes are exercised here, and two of them must refuse to report a clean
# pass. A mode that has not been shown to refuse is indistinguishable from one that cannot.
printf '\n[D] results-mode is explicit, not inferred from an empty directory\n'
BARE="$WORK/bare"
mkdir -p "$BARE/docs/design"
cp -r "$EXTRACT/docs/design/lxr-port" "$BARE/docs/design/lxr-port"
GATE_BARE="$BARE/docs/design/lxr-port/harness/scripts/verify-harness.sh"

D_OUT=$(bash "$GATE_BARE" "$BARE" 2>&1); D_CODE=$?
printf '  no arguments, no results:      exit=%d\n' "$D_CODE"
if [[ $D_CODE -eq 2 ]] && ! printf '%s\n' "$D_OUT" | grep -q 'RESULT: PASS'; then
    printf '  REFUSED as required - it does not guess, and it does not print PASS.\n'
else
    printf '  DID NOT REFUSE. An unaudited tree can still report a pass.\n'
    printf '%s\n' "$D_OUT" | tail -n 3 | sed 's/^/      /'
    STATUS=1
fi

D_OUT=$(bash "$GATE_BARE" "$BARE" --no-results 2>&1); D_CODE=$?
printf '  --no-results:                  exit=%d\n' "$D_CODE"
if [[ $D_CODE -eq 3 ]] && printf '%s\n' "$D_OUT" | grep -q 'RESULT: DEGRADED'; then
    printf '  DEGRADED as required, and on its own exit code so exit-0 callers cannot be fooled.\n'
    printf '%s\n' "$D_OUT" | grep -E '^  SKIP' | sed 's/^/      /'
else
    printf '  did not report DEGRADED on an elected skip.\n'
    printf '%s\n' "$D_OUT" | tail -n 3 | sed 's/^/      /'
    STATUS=1
fi

D_OUT=$(bash "$GATE_BARE" "$BARE" --results "$BARE/artifacts/lxr-harness/runs" 2>&1); D_CODE=$?
printf '  --results on an empty dir:     exit=%d\n' "$D_CODE"
if [[ $D_CODE -eq 2 ]] && ! printf '%s\n' "$D_OUT" | grep -q 'RESULT: PASS'; then
    printf '  REFUSED as required - naming a directory is a claim that it holds results.\n'
else
    printf '  accepted an empty results directory it was told to audit.\n'
    STATUS=1
fi

# --- E ---------------------------------------------------------------------
# The general form of the same defect: a section that runs no checks and does not declare a skip is
# a gate bug, so removing a section's only check must fail rather than quietly shrink the total.
printf '\n[E] a section that contributes no checks cannot read as a section that passed\n'
SILENT="$WORK/silent"
mkdir -p "$SILENT/docs/design"
cp -r "$EXTRACT/docs/design/lxr-port" "$SILENT/docs/design/lxr-port"
prepare_root "$SILENT"
GATE_SILENT="$SILENT/docs/design/lxr-port/harness/scripts/verify-harness.sh"
# Neuter the stray-results section: keep the section header, remove every check it can run. awk
# rather than python, because the harness carries no tooling dependencies and neither should its gate.
awk '
    /^section .no result was written outside/ { print; skipping = 1; next }
    skipping && /^# ---/                     { skipping = 0 }
    skipping                                 { next }
                                             { print }
' "$GATE_SILENT" > "$GATE_SILENT.tmp" && mv "$GATE_SILENT.tmp" "$GATE_SILENT"

E_OUT=$(bash "$GATE_SILENT" "$SILENT" --results "$SILENT/artifacts/lxr-harness/runs" 2>&1); E_CODE=$?
printf '  section emptied of checks:     exit=%d\n' "$E_CODE"
if [[ $E_CODE -eq 1 ]] && printf '%s\n' "$E_OUT" | grep -q 'ran no checks and did not declare a skip'; then
    printf '  DETECTED as required.\n'
    printf '%s\n' "$E_OUT" | grep -E 'ran no checks' | sed 's/^/      /'
else
    printf '  a silently empty section still produced a clean verdict.\n'
    printf '%s\n' "$E_OUT" | tail -n 3 | sed 's/^/      /'
    STATUS=1
fi

printf '\n'
if [[ $STATUS -eq 0 ]]; then
    printf 'GATE CONTROL: PASS - the gate passes committed content, fails a single perturbation, and recovers.\n'
else
    printf 'GATE CONTROL: FAIL\n'
fi
exit $STATUS

#!/usr/bin/env bash
# P0.5 baseline verification gate.
#
# Audits the artifact it ships with. The root is derived from ${BASH_SOURCE[0]}, so this script always
# checks the tree it is part of; an explicit argument overrides. There is no absolute machine path
# anywhere in here - that is the defect that made P0.3's gate silently audit a different, mid-edit
# checkout, reporting a failure the committed tree did not have and equally able to report a pass it
# had not earned.
#
# Expected counts are DERIVED from the artifact, never repeated as literals at several assertion
# sites, so two cannot drift from a third unnoticed.
#
# The strongest check here, and the one that makes P0.5's raw-retention decision mean something:
# every published mean is RECOMPUTED from the committed per-invocation CSV and required to match the
# published JSON. A summary that cannot be re-analysed is not evidence; this makes that testable
# rather than asserted.
#
# A section that contributes zero checks must never read as a section that passed. Modes are chosen,
# never inferred.
#
# Usage: verify-baselines.sh [repo-root] (--results <dir> | --no-results)
#
#   --results <dir>   audit the checkpoint files under <dir>. Fails if there are none, because naming
#                     a directory is a claim that it holds results.
#   --no-results      deliberately audit the static artifact only. Prints DEGRADED and exits 3, so a
#                     caller that checks only for exit 0 cannot mistake it for a full audit.
#   neither           inferred from the committed results directory. If that yields nothing the gate
#                     REFUSES to run rather than quietly skipping.
#
# Note for callers: `set -e` around this script will kill the caller on the intentional exit 3.

set -uo pipefail

SELF_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

ROOT_ARG=""
RESULTS_DIR=""
RESULTS_MODE="infer"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --results)
            if [[ $# -lt 2 ]]; then
                printf 'verify-baselines.sh: --results needs a directory\n' >&2
                exit 2
            fi
            RESULTS_DIR="$2"
            RESULTS_MODE="explicit"
            shift 2
            ;;
        --no-results)
            RESULTS_MODE="none"
            shift
            ;;
        -h|--help)
            sed -n '20,29p' "${BASH_SOURCE[0]}"
            exit 0
            ;;
        --*)
            printf 'verify-baselines.sh: unknown option %s\n' "$1" >&2
            exit 2
            ;;
        *)
            ROOT_ARG="$1"
            shift
            ;;
    esac
done

if [[ -n "$ROOT_ARG" ]]; then
    ROOT=$(cd "$ROOT_ARG" && pwd)
else
    # scripts -> P0.5-baselines -> lxr-port -> design -> docs -> repo root
    ROOT=$(cd "$SELF_DIR/../../../../.." && pwd)
fi

DOC="$ROOT/docs/design/lxr-port/P0.5-baselines.md"
BASE="$ROOT/docs/design/lxr-port/P0.5-baselines"
HARNESS="$ROOT/docs/design/lxr-port/harness"

if [[ -z "$RESULTS_DIR" ]]; then
    RESULTS_DIR="$BASE"
fi

PASS=0
FAIL=0
SKIPPED=()
SECTION_NAME=""
SECTION_START=0
SECTION_DECLARED=0
ZERO_CHECK_SECTIONS=()

# A verdict prefixed onto only the first line of a captured block misreports the rest of it. The
# prose checker's output opens with its first claim, so `bad "$out"` rendered as
# "FAIL  claim repro-cells ..." above that claim's own "ok" line - a passing claim reading as the
# failure. Multi-line payloads get the verdict on its own line and the block indented beneath.
indent_block() { printf '%s\n' "$1" | sed 's/^/          /'; }
is_multiline() { [[ "$1" == *$'\n'* ]]; }

ok() {
    PASS=$((PASS + 1))
    if is_multiline "$1"; then
        printf '  ok    (report follows)\n'
        indent_block "$1"
    else
        printf '  ok    %s\n' "$1"
    fi
}
bad() {
    FAIL=$((FAIL + 1))
    if is_multiline "$1"; then
        printf '  FAIL  (report follows)\n'
        indent_block "$1"
    else
        printf '  FAIL  %s\n' "$1"
    fi
}
note() { printf '        %s\n' "$1"; }

close_section() {
    [[ -z "$SECTION_NAME" ]] && return 0
    local ran=$((PASS + FAIL - SECTION_START))
    if [[ $ran -eq 0 && $SECTION_DECLARED -eq 0 ]]; then
        ZERO_CHECK_SECTIONS+=("$SECTION_NAME")
    fi
    return 0
}

section() {
    close_section
    SECTION_NAME="$1"
    SECTION_START=$((PASS + FAIL))
    SECTION_DECLARED=0
    printf '\n== %s ==\n' "$1"
}

skip_section() {
    SECTION_DECLARED=1
    SKIPPED+=("$1|$SECTION_NAME|$2")
    printf '  SKIP  %s\n' "$2"
}

printf 'P0.5 baseline gate\n'
printf 'root: %s\n' "$ROOT"
printf 'self: %s\n' "$SELF_DIR"

CHECKPOINTS=""
if [[ "$RESULTS_MODE" != "none" ]]; then
    CHECKPOINTS=$(find "$RESULTS_DIR" -maxdepth 1 -name '*.json' ! -name 'calibration.json' 2>/dev/null | sort || true)
fi

case "$RESULTS_MODE" in
    explicit)
        if [[ -z "$CHECKPOINTS" ]]; then
            printf 'mode: --results %s\n' "$RESULTS_DIR"
            printf '\nverify-baselines.sh: no checkpoint json under %s\n' "$RESULTS_DIR" >&2
            printf 'Naming a results directory asserts that it holds results. Pass --no-results to audit\n' >&2
            printf 'the static artifact alone.\n' >&2
            exit 2
        fi
        printf 'mode: full audit (--results %s)\n' "$RESULTS_DIR"
        ;;
    none)
        printf 'mode: DEGRADED - static artifact only (--no-results)\n'
        ;;
    infer)
        if [[ -z "$CHECKPOINTS" ]]; then
            printf 'mode: undetermined\n'
            printf '\nverify-baselines.sh: no checkpoint json under %s\n' "$RESULTS_DIR" >&2
            printf 'Refusing to guess. The checks that would be skipped are the ones that re-derive every\n' >&2
            printf 'published statistic from the raw samples, which is what makes these numbers evidence,\n' >&2
            printf 'so skipping them silently would turn an unaudited tree into a PASS.\n' >&2
            printf '\n  --results <dir>   audit checkpoints from <dir>\n' >&2
            printf '  --no-results      audit the static artifact only; prints DEGRADED and exits 3\n' >&2
            exit 2
        fi
        printf 'mode: full audit (inferred %s)\n' "$RESULTS_DIR"
        ;;
esac

PY=""
for candidate in python python3 py; do
    if command -v "$candidate" >/dev/null 2>&1; then PY="$candidate"; break; fi
done

# ---------------------------------------------------------------------------
section 'artifacts exist'

[[ -f "$DOC" ]] && ok "design document present" || bad "missing $DOC"
[[ -d "$BASE" ]] && ok "results directory present" || bad "missing $BASE"
[[ -f "$BASE/calibration.json" ]] && ok "calibration file present" || bad "missing calibration.json"
[[ -d "$BASE/raw" ]] && ok "raw sample directory present" || bad "missing raw/"
[[ -f "$SELF_DIR/run-p05-baselines.ps1" ]] && ok "the run script ships with the results it produced" \
    || bad "missing run-p05-baselines.ps1"

if [[ $FAIL -gt 0 ]]; then
    printf '\nRESULT: FAIL (%d passed, %d failed) - artifacts missing, later sections would be noise\n' "$PASS" "$FAIL"
    exit 1
fi

# ---------------------------------------------------------------------------
section 'nothing was written to the roadmap canvas'

# The canvas directory belongs to the coordinator and is not present on main at all. Writing there
# would be a scope violation that no other check in this gate would notice.
if [[ -d "$ROOT/.github/extensions/lxr-gc-roadmap" ]]; then
    bad ".github/extensions/lxr-gc-roadmap exists in this tree; P0.5 must not create it"
else
    ok "no .github/extensions/lxr-gc-roadmap directory was created"
fi

# ---------------------------------------------------------------------------
section 'checkpoint files are schema-v2 conformant'

if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; the JSON structural checks cannot run"
elif [[ "$RESULTS_MODE" == "none" ]]; then
    skip_section elected "--no-results: the checkpoints were deliberately not examined"
else
    while IFS= read -r file; do
        [[ -z "$file" ]] && continue
        name=$(basename "$file")
        if out=$("$PY" "$SELF_DIR/check-checkpoint.py" "$file" 2>&1); then
            ok "$name: $out"
        else
            bad "$name: $out"
        fi
    done <<< "$CHECKPOINTS"
fi

# ---------------------------------------------------------------------------
section 'every published statistic is re-derivable from the committed raw samples'

# This is the check that makes the raw-retention decision honest. The published operationsPerSecond
# for a cell must equal the mean of that cell's per-invocation rows in the committed CSV. If it does
# not, the summary is not a summary of the data shipped beside it.
if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; statistics cannot be recomputed"
elif [[ "$RESULTS_MODE" == "none" ]]; then
    skip_section elected "--no-results: the published statistics were deliberately not re-derived"
elif ! compgen -G "$BASE/raw/*invocations*.csv" >/dev/null; then
    bad "no raw/*invocations*.csv; the published means cannot be re-derived and are not evidence"
else
    while IFS= read -r file; do
        [[ -z "$file" ]] && continue
        name=$(basename "$file")
        # Derived, not listed: <name>.json pairs with raw/<name>-invocations.csv. A checkpoint whose
        # CSV is absent must fail here rather than be skipped, or an unbacked summary passes.
        csv="$BASE/raw/${name%.json}-invocations.csv"
        if [[ ! -f "$csv" ]]; then
            bad "$name: no $(basename "$csv"); its published statistics cannot be re-derived"
            continue
        fi
        if out=$("$PY" "$SELF_DIR/rederive-statistics.py" --checkpoint "$file" --csv "$csv" 2>&1); then
            ok "$name: $(printf '%s' "$out" | tail -n 1)"
        else
            bad "$name: $out"
        fi
    done <<< "$CHECKPOINTS"
fi

# ---------------------------------------------------------------------------
section 'the calibration is measured, not provisional'

if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; the calibration file cannot be parsed"
elif [[ "$RESULTS_MODE" == "none" ]]; then
    skip_section elected "--no-results: the calibration was deliberately not examined"
else
    if out=$("$PY" "$SELF_DIR/check-calibration.py" "$BASE/calibration.json" 2>&1); then
        ok "$out"
    else
        bad "$out"
    fi
fi

# ---------------------------------------------------------------------------
# Rule 28: a gate that verifies data against data does not verify prose against data. Thirty checks
# passed over a sentence claiming "all four throughput ratios ... drift by less than the 8.35%
# floor" while the CSV beside it published one at 8.744%. Every check compared data to data.
section 'the quantified prose claims re-derive from the shipped CSVs'

if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; the prose claims cannot be re-derived"
elif [[ "$RESULTS_MODE" == "none" ]]; then
    skip_section elected "--no-results: the prose claims were deliberately not re-derived"
else
    if out=$("$PY" "$SELF_DIR/check-prose-claims.py" --doc "$DOC" --results "$BASE" 2>&1); then
        ok "$(printf '%s' "$out" | tail -n 1)"
    else
        # Print the whole comparison, not the verdict: which claim, what the data said, what the
        # prose said. A one-line failure here is not actionable.
        bad "$out"
        note "a sentence disagrees with the CSV shipped beside it"
    fi
fi

# ---------------------------------------------------------------------------
# The section 6.4 variance table shipped without a generator, so it was reproducible only by
# reverse-engineering its partition -- and an independent recomputation partitioned by the `mode`
# column got 53 cells at 1.792% instead of 59 at 1.597% and nearly reported it unreproducible.
section 'the section 6.4 variance table re-derives from the raw invocations'

if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; the variance table cannot be re-derived"
elif [[ "$RESULTS_MODE" == "none" ]]; then
    skip_section elected "--no-results: the variance table was deliberately not re-derived"
elif [[ ! -f "$BASE/raw/p0-5-baselines-s2-invocations.csv" ]]; then
    bad "raw/p0-5-baselines-s2-invocations.csv is absent; section 6.4 cannot be re-derived"
else
    if out=$("$PY" "$SELF_DIR/derive-variance-table.py" \
             --csv "$BASE/raw/p0-5-baselines-s2-invocations.csv" --doc "$DOC" --check 2>&1); then
        ok "$(printf '%s' "$out" | tail -n 1)"
    else
        bad "$out"
        note "the published variance table does not match the invocations it was derived from"
    fi
fi

# ---------------------------------------------------------------------------
section 'the document records what the brief requires it to record'

# Derived, not literal: the scenarios named in the document's matrix table must be exactly the
# scenarios in the harness catalogue. A document that quietly dropped a scenario would otherwise
# read as complete.
CATALOGUE=$(grep -oE 'Id = "[a-z0-9-]+"' "$HARNESS/src/Lxr.Harness.Core/ScenarioCatalog.cs" \
    | sed -E 's/Id = "(.*)"/\1/' | sort -u)
CATALOGUE_N=$(printf '%s\n' "$CATALOGUE" | grep -c . || true)

if [[ "$CATALOGUE_N" -lt 2 ]]; then
    bad "could not read the scenario catalogue; the probe found $CATALOGUE_N ids, so a negative result here would be meaningless"
else
    missing=""
    for id in $CATALOGUE; do
        grep -q -- "$id" "$DOC" || missing="$missing $id"
    done
    if [[ -n "$missing" ]]; then
        bad "the document does not mention:$missing"
    else
        ok "all $CATALOGUE_N catalogue scenarios appear in the document"
    fi
fi

# The findings that contradict the brief must be in the document, not only in a chat report.
for topic in 'F1' 'F2' 'F5' 'resolution floor' 'blind band'; do
    if grep -qi -- "$topic" "$DOC"; then
        ok "the document records '$topic'"
    else
        bad "the document does not record '$topic'"
    fi
done

# The paper's own caveats, which is what stops a baseline being read as a comparison it cannot support.
for topic in 'arXiv:2210.17175' 'class unloading' 'application-observed latency'; do
    if grep -qi -- "$topic" "$DOC"; then
        ok "the document records '$topic'"
    else
        bad "the document does not record '$topic'"
    fi
done

# ---------------------------------------------------------------------------
section 'declared skips are declared, not absent'

if grep -qi 'declared skip' "$DOC"; then
    ok "the document declares its skips"
else
    bad "the document declares no skips; anything not run must be named"
fi

# corerun was an elected skip agreed before the run. It must be named as one.
if grep -qi 'corerun' "$DOC"; then
    ok "the corerun skip is named"
else
    bad "corerun is not mentioned; an elected skip must be visible in the artifact"
fi

# ---------------------------------------------------------------------------
section 'the document does not claim capabilities the source does not have (rule 25)'

# P0.4's gate ran five ways at three hashes and could never have caught F1, because every check
# compared the document to itself or to the results beside it. This one compares claims to source.
if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; the document/code boundary cannot be audited"
else
    if out=$("$PY" "$SELF_DIR/check-doc-code.py" --selftest 2>&1); then
        ok "doc/code extractors self-test: $(printf '%s' "$out" | tail -n 1)"
    else
        bad "doc/code extractors failed their own self-test, so a pass would mean nothing: $out"
    fi
    if out=$("$PY" "$SELF_DIR/check-doc-code.py" 2>&1); then
        ok "$(printf '%s' "$out" | tail -n 1)"
    else
        bad "$out"
        note "a documented verb or flag is absent from source; that is F1's shape"
    fi

    # Same rule applied to scripts: a document naming scripts/<x> is claiming <x> ships. Derived by
    # scanning the document rather than from a list here, so a newly named script is covered the
    # moment it is mentioned. Both counts are printed rather than only a verdict.
    named_scripts=$(grep -oE 'scripts/[A-Za-z0-9._-]+\.(sh|py|ps1)' "$DOC" | sed 's|scripts/||' | sort -u)
    named_count=$(printf '%s\n' "$named_scripts" | grep -c . || true)
    present_count=0
    missing=""
    while IFS= read -r script; do
        [[ -z "$script" ]] && continue
        if [[ -f "$SELF_DIR/$script" ]]; then
            present_count=$((present_count + 1))
        else
            missing="$missing $script"
        fi
    done <<< "$named_scripts"
    if [[ -z "$missing" ]]; then
        ok "document names $named_count script(s), $present_count present, difference $((named_count - present_count))"
    else
        bad "document names $named_count script(s), $present_count present, missing:$missing"
        note "a document that names a script it does not ship is F1's shape in a different place"
    fi

    # Line citations rot silently: the file stays present, the line moves, and the citation now points
    # at something that does not say what it is cited for. Derived by scanning the document, so a new
    # citation is covered the moment it is written. Resolved by basename under the port tree, and a
    # blank target counts as a failure - a citation into padding is the same defect as one past EOF.
    citations=$(grep -oE '`[A-Za-z0-9._/-]+\.(cs|csproj|sh|py|ps1|md)`?:[0-9]+(-[0-9]+)?`' "$DOC" \
        | tr -d '`' | sort -u)
    cite_count=$(printf '%s\n' "$citations" | grep -c . || true)
    if [[ "$cite_count" -lt 1 ]]; then
        bad "the citation extractor found $cite_count file:line citations; a pass here would be meaningless"
    else
        cite_ok=0
        cite_bad=""
        while IFS= read -r cite; do
            [[ -z "$cite" ]] && continue
            cite_file=${cite%%:*}
            cite_line=${cite##*:}
            cite_line=${cite_line%%-*}
            target=$(find "$ROOT/docs/design/lxr-port" -name "$(basename "$cite_file")" -type f 2>/dev/null | head -n 1)
            if [[ -z "$target" ]]; then
                cite_bad="$cite_bad $cite(no such file)"
            elif [[ "$(wc -l < "$target")" -lt "$cite_line" ]]; then
                cite_bad="$cite_bad $cite(only $(wc -l < "$target") lines)"
            elif [[ -z "$(sed -n "${cite_line}p" "$target" | tr -d '[:space:]')" ]]; then
                cite_bad="$cite_bad $cite(blank line)"
            else
                cite_ok=$((cite_ok + 1))
            fi
        done <<< "$citations"
        if [[ -z "$cite_bad" ]]; then
            ok "document makes $cite_count file:line citation(s), $cite_ok resolve to a non-blank line, difference $((cite_count - cite_ok))"
        else
            bad "document makes $cite_count file:line citation(s), $cite_ok resolve, bad:$cite_bad"
            note "a citation that no longer points at what it claims is a document lying about the tree (rule 25)"
        fi
    fi
fi

# ---------------------------------------------------------------------------
section 'the document has no unfilled placeholders'

# A published document with an empty section reads as a section that had nothing to report.
placeholder_count=$(grep -c 'P05-PLACEHOLDER' "$DOC" || true)
if [[ "$placeholder_count" -eq 0 ]]; then
    ok "0 unfilled placeholders in $(basename "$DOC")"
else
    bad "$placeholder_count unfilled placeholder(s) in $(basename "$DOC")"
    grep -n 'P05-PLACEHOLDER' "$DOC" | while IFS= read -r hit; do note "  $hit"; done
fi

# ---------------------------------------------------------------------------
section 'the cross-session analyses re-derive from the committed CSVs'

# The reproducibility result is the step's headline, so it must be recomputable from the committed
# data rather than trusted as a written conclusion. Both counts are printed, per rule 26.
if [[ -z "$PY" ]]; then
    skip_section inherent "no python on PATH; the comparisons cannot be recomputed"
elif [[ "$RESULTS_MODE" == "none" ]]; then
    skip_section elected "--no-results: the cross-session comparisons were deliberately not recomputed"
elif [[ ! -f "$BASE/p0-5-baselines-s3.json" || ! -f "$BASE/raw/p0-5-baselines-s3-invocations.csv" ]]; then
    skip_section elected "the s3 reproducibility session is not present in this extract"
else
    tmp_repro=$(mktemp)
    if "$PY" "$SELF_DIR/compare-sessions.py" \
        --baseline "$BASE/p0-5-baselines-s2.json" --baseline-csv "$BASE/raw/p0-5-baselines-s2-invocations.csv" \
        --other "$BASE/p0-5-baselines-s3.json" --other-csv "$BASE/raw/p0-5-baselines-s3-invocations.csv" \
        --label reproducibility --out "$tmp_repro" >/dev/null 2>&1; then
        committed_rows=$(($(wc -l < "$BASE/raw/reproducibility-s2-vs-s3.csv") - 1))
        recomputed_rows=$(($(wc -l < "$tmp_repro") - 1))
        if [[ "$committed_rows" -eq "$recomputed_rows" ]]; then
            ok "reproducibility comparison recomputes: committed $committed_rows rows, recomputed $recomputed_rows, difference $((committed_rows - recomputed_rows))"
        else
            bad "reproducibility comparison disagrees: committed $committed_rows rows, recomputed $recomputed_rows, difference $((committed_rows - recomputed_rows))"
        fi
        # tr -d '\r' because git normalises these CSVs to LF in the index and writes CRLF back on
        # checkout and archive, so the committed copy and the freshly recomputed one differ in line
        # ending alone. Anchoring on '$' without this made the count 0 in a git archive extract and
        # 12 in the worktree, from identical data.
        committed_disagree=$(tr -d '\r' < "$BASE/raw/reproducibility-s2-vs-s3.csv" | grep -c ',true$' || true)
        recomputed_disagree=$(tr -d '\r' < "$tmp_repro" | grep -c ',true$' || true)
        if [[ "$committed_disagree" -eq "$recomputed_disagree" ]]; then
            ok "cells exceeding both spread and floor: committed $committed_disagree, recomputed $recomputed_disagree, difference $((committed_disagree - recomputed_disagree))"
        else
            bad "disagreeing-cell count moved: committed $committed_disagree, recomputed $recomputed_disagree, difference $((committed_disagree - recomputed_disagree))"
        fi
    else
        bad "compare-sessions.py failed against the committed data"
    fi
    rm -f "$tmp_repro"

    tmp_ratio=$(mktemp)
    if "$PY" "$SELF_DIR/compare-arm-ratios.py" \
        --baseline-csv "$BASE/raw/p0-5-baselines-s2-invocations.csv" \
        --other-csv "$BASE/raw/p0-5-baselines-s3-invocations.csv" \
        --out "$tmp_ratio" >/dev/null 2>&1; then
        committed_ratio=$(($(wc -l < "$BASE/raw/arm-ratio-stability-s2-vs-s3.csv") - 1))
        recomputed_ratio=$(($(wc -l < "$tmp_ratio") - 1))
        if [[ "$committed_ratio" -eq "$recomputed_ratio" ]]; then
            ok "arm-ratio stability recomputes: committed $committed_ratio rows, recomputed $recomputed_ratio, difference $((committed_ratio - recomputed_ratio))"
        else
            bad "arm-ratio stability disagrees: committed $committed_ratio rows, recomputed $recomputed_ratio, difference $((committed_ratio - recomputed_ratio))"
        fi
    else
        bad "compare-arm-ratios.py failed against the committed data"
    fi
    rm -f "$tmp_ratio"
fi

# ---------------------------------------------------------------------------
close_section
printf '\n'

if [[ ${#ZERO_CHECK_SECTIONS[@]} -gt 0 ]]; then
    for undeclared in "${ZERO_CHECK_SECTIONS[@]}"; do
        bad "section '$undeclared' ran no checks and did not declare a skip"
    done
    note "a section that contributes nothing must say so; silence here is what produced a false PASS"
fi

ELECTED=0
if [[ ${#SKIPPED[@]} -gt 0 ]]; then
    printf 'SKIPPED SECTIONS\n'
    for entry in "${SKIPPED[@]}"; do
        printf '  %-9s %s\n' "${entry%%|*}" "$(printf '%s' "$entry" | cut -d'|' -f2-3 --output-delimiter=': ')"
        [[ "${entry%%|*}" == "elected" ]] && ELECTED=$((ELECTED + 1))
    done
    printf '\n'
fi

if [[ $FAIL -gt 0 ]]; then
    printf 'RESULT: FAIL (%d passed, %d failed)\n' "$PASS" "$FAIL"
    exit 1
fi

if [[ $ELECTED -gt 0 ]]; then
    printf 'RESULT: DEGRADED (%d checks passed, %d section(s) skipped by request)\n' "$PASS" "$ELECTED"
    printf 'This is NOT a full audit. Re-run with --results <dir> before treating it as evidence.\n'
    exit 3
fi

printf 'RESULT: PASS (%d checks)\n' "$PASS"
exit 0

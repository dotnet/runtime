#!/usr/bin/env bash
# P0.4 verification gate.
#
# Audits the artifact it ships with. The root is derived from ${BASH_SOURCE[0]}, so this script
# always checks the tree it is part of; an explicit argument overrides. There is no absolute machine
# path anywhere in here - that is precisely the defect that made P0.3's gate silently audit a
# different, mid-edit checkout, reporting a failure the committed tree did not have and equally able
# to report a pass it had not earned.
#
# Expected counts are DERIVED, never repeated as literals. The scenario count comes from three
# independent places that must agree: the matrix table in P0.4-harness.md, the catalogue in source,
# and the ids present in the smoke results. Writing "10" at three assertion sites would let two drift
# from the third unnoticed.
#
# A section that contributes zero checks must never read as a section that passed. An earlier version
# of this gate inferred "audit the results" from whether any results happened to be on disk, so a
# `git archive` extract - the way an auditor is most likely to run this - silently dropped the entire
# `results are schema-conformant and carry identity` section and still printed an unqualified PASS.
# That section is what checks collector identity, which is P0.4's own stated correctness criterion, so
# the single most important property this gate asserts evaporated in the mode most likely to be used.
# It is the same defect the harness refuses to tolerate in a scenario - a declared skip, never a silent
# absence - and the same one §10 asks the canvas not to commit: an absent value must not be charted as
# a zero. The mode is now chosen explicitly and a skipped section is named in the verdict.
#
# Usage: verify-harness.sh [repo-root] (--results <dir> | --no-results)
#
#   --results <dir>   audit the results.json files under <dir>. Fails if there are none, because
#                     naming a directory is a claim that it holds results.
#   --no-results      deliberately audit the static artifact only. Prints DEGRADED and exits 3, so a
#                     caller that checks only for exit 0 cannot mistake it for a full audit.
#   neither           inferred from the default output directory. If that yields nothing the gate
#                     REFUSES to run rather than quietly skipping, because that is the case that
#                     produced a false PASS.

set -uo pipefail

SELF_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

ROOT_ARG=""
RESULTS_DIR=""
RESULTS_MODE="infer"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --results)
            if [[ $# -lt 2 ]]; then
                printf 'verify-harness.sh: --results needs a directory\n' >&2
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
            sed -n '16,26p' "${BASH_SOURCE[0]}"
            exit 0
            ;;
        --*)
            printf 'verify-harness.sh: unknown option %s\n' "$1" >&2
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
    # scripts -> harness -> lxr-port -> design -> docs -> repo root
    ROOT=$(cd "$SELF_DIR/../../../../.." && pwd)
fi

DOC="$ROOT/docs/design/lxr-port/P0.4-harness.md"
HARNESS="$ROOT/docs/design/lxr-port/harness"
SRC="$HARNESS/src"

if [[ -z "$RESULTS_DIR" ]]; then
    RESULTS_DIR="$ROOT/artifacts/lxr-harness/runs"
fi

PASS=0
FAIL=0

# Section accounting. SKIPPED holds "kind|section|reason" for every section that ran no checks on
# purpose; INHERENT skips are impossible to satisfy in the current mode (an index check outside a
# checkout) and do not change the verdict, ELECTED skips are an operator choice and downgrade it.
SKIPPED=()
SECTION_NAME=""
SECTION_START=0
SECTION_DECLARED=0
ZERO_CHECK_SECTIONS=()

ok()   { PASS=$((PASS + 1)); printf '  ok    %s\n' "$1"; }
bad()  { FAIL=$((FAIL + 1)); printf '  FAIL  %s\n' "$1"; }
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

# Declare that this section is deliberately running no checks, and why. Without this a zero-check
# section is a gate defect, not a mode.
skip_section() {
    SECTION_DECLARED=1
    SKIPPED+=("$1|$SECTION_NAME|$2")
    printf '  SKIP  %s\n' "$2"
}

printf 'P0.4 harness gate\n'
printf 'root: %s\n' "$ROOT"
printf 'self: %s\n' "$SELF_DIR"

# Resolve the results mode BEFORE any section runs, so the operator learns immediately whether this
# invocation is a full audit or a partial one. Inferring it from an empty directory is what produced
# a PASS that had skipped the collector-identity checks entirely.
RESULTS=""
if [[ "$RESULTS_MODE" != "none" ]]; then
    RESULTS=$(find "$RESULTS_DIR" -name results.json 2>/dev/null | sort || true)
fi

case "$RESULTS_MODE" in
    explicit)
        if [[ -z "$RESULTS" ]]; then
            printf 'mode: --results %s\n' "$RESULTS_DIR"
            printf '\nverify-harness.sh: no results.json under %s\n' "$RESULTS_DIR" >&2
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
        if [[ -z "$RESULTS" ]]; then
            printf 'mode: undetermined\n'
            printf '\nverify-harness.sh: no results.json under %s\n' "$RESULTS_DIR" >&2
            printf 'Refusing to guess. The checks that would be skipped are the ones that verify collector\n' >&2
            printf 'identity, which is this step'"'"'s stated correctness criterion, so skipping them silently\n' >&2
            printf 'would turn an unaudited tree into a PASS.\n' >&2
            printf '\n  --results <dir>   audit results from <dir> (compose a run directory into the extract)\n' >&2
            printf '  --no-results      audit the static artifact only; prints DEGRADED and exits 3\n' >&2
            exit 2
        fi
        printf 'mode: full audit (inferred %s)\n' "$RESULTS_DIR"
        ;;
esac

# ---------------------------------------------------------------------------
section 'artifacts exist'

for required in \
    "$DOC" \
    "$HARNESS/README.md" \
    "$HARNESS/Directory.Build.props" \
    "$HARNESS/Directory.Build.targets" \
    "$HARNESS/NuGet.config" \
    "$SRC/Lxr.Harness.Core/ScenarioCatalog.cs" \
    "$SRC/Lxr.Harness.Core/ResultConformance.cs" \
    "$SRC/Lxr.Harness.Runner/ControlSuite.cs" \
    "$HARNESS/scripts/run-smoke.ps1" \
    "$HARNESS/scripts/verify-harness.sh" \
    "$HARNESS/scripts/verify-harness-control.sh"
do
    if [[ -f "$required" ]]; then
        ok "present: ${required#"$ROOT/"}"
    else
        bad "missing: ${required#"$ROOT/"}"
    fi
done

if [[ $FAIL -gt 0 ]]; then
    printf '\nartifacts missing; the remaining checks would be meaningless.\n'
    printf 'RESULT: FAIL (%d passed, %d failed)\n' "$PASS" "$FAIL"
    exit 1
fi

# ---------------------------------------------------------------------------
section 'scenario count agrees across three independent derivations'

# (1) the document's matrix table: rows of the form "| <n> | `id` | ..."
DOC_IDS=$(grep -oE '^\| [0-9]+ \| `[a-z-]+`' "$DOC" | grep -oE '`[a-z-]+`' | tr -d '`' | sort -u)
DOC_COUNT=$(printf '%s\n' "$DOC_IDS" | grep -c . || true)

# (2) the catalogue in source
SRC_IDS=$(grep -oE 'Id = "[a-z-]+"' "$SRC/Lxr.Harness.Core/ScenarioCatalog.cs" \
    | sed -E 's/Id = "([a-z-]+)"/\1/' | sort -u)
SRC_COUNT=$(printf '%s\n' "$SRC_IDS" | grep -c . || true)

note "document matrix: $DOC_COUNT scenario(s)"
note "source catalogue: $SRC_COUNT scenario(s)"

if [[ "$DOC_COUNT" -eq 0 ]]; then
    bad "the document's scenario matrix parsed to zero rows; the parser or the table changed"
elif [[ "$DOC_COUNT" -eq "$SRC_COUNT" ]]; then
    ok "document and source agree on $DOC_COUNT scenarios"
else
    bad "document has $DOC_COUNT scenarios, source catalogue has $SRC_COUNT"
fi

if [[ "$DOC_IDS" == "$SRC_IDS" ]]; then
    ok "document and source name the same scenario ids"
else
    bad "scenario ids differ between document and source"
    note "only in document: $(comm -23 <(printf '%s\n' "$DOC_IDS") <(printf '%s\n' "$SRC_IDS") | tr '\n' ' ')"
    note "only in source:   $(comm -13 <(printf '%s\n' "$DOC_IDS") <(printf '%s\n' "$SRC_IDS") | tr '\n' ' ')"
fi

# (3) the smoke results, when present. Absent results are reported, never silently skipped.
if [[ -z "$RESULTS" ]]; then
    note "results not audited in this mode (third derivation not available)"
else
    RESULT_IDS=$(grep -ohE '"scenario": *"[a-z-]+"' $RESULTS \
        | sed -E 's/.*"scenario": *"([a-z-]+)".*/\1/' | sort -u)
    RESULT_COUNT=$(printf '%s\n' "$RESULT_IDS" | grep -c . || true)
    note "smoke results: $RESULT_COUNT distinct scenario(s) across $(printf '%s\n' "$RESULTS" | grep -c .) file(s)"

    UNKNOWN=$(comm -23 <(printf '%s\n' "$RESULT_IDS") <(printf '%s\n' "$SRC_IDS") | tr '\n' ' ')
    if [[ -z "${UNKNOWN// /}" ]]; then
        ok "every scenario id in the smoke results is in the catalogue"
    else
        bad "smoke results contain scenario ids absent from the catalogue: $UNKNOWN"
    fi

    if [[ "$RESULT_COUNT" -eq "$SRC_COUNT" ]]; then
        ok "smoke results cover all $SRC_COUNT catalogued scenarios"
    else
        MISSING=$(comm -13 <(printf '%s\n' "$RESULT_IDS") <(printf '%s\n' "$SRC_IDS") | tr '\n' ' ')
        bad "smoke results cover $RESULT_COUNT of $SRC_COUNT scenarios; never run: $MISSING"
    fi
fi

# ---------------------------------------------------------------------------
section 'every scenario is fully specified'

while read -r id; do
    [[ -z "$id" ]] && continue

    # a rationale in the document: the matrix row must carry prose beyond the id
    ROW=$(grep -E "^\| [0-9]+ \| \`$id\` \|" "$DOC" || true)
    RATIONALE=$(printf '%s' "$ROW" | awk -F'|' '{print $4}' | sed 's/^ *//; s/ *$//')
    if [[ ${#RATIONALE} -ge 60 ]]; then
        ok "$id: rationale (${#RATIONALE} chars)"
    else
        bad "$id: rationale is missing or too thin (${#RATIONALE} chars)"
    fi

    # a declared timeout in the catalogue, and it must MATCH the document's table. The document
    # column and the catalogue are two independent statements of the same fact, so checking they
    # agree is worth more than checking each exists.
    DOC_TIMEOUT=$(printf '%s' "$ROW" | awk -F'|' '{print $6}' | tr -cd '0-9')
    SRC_TIMEOUT=$(grep -A8 "Id = \"$id\"," "$SRC/Lxr.Harness.Core/ScenarioCatalog.cs" \
        | grep -oE 'DefaultTimeoutSeconds = [0-9]+' | head -n 1 | grep -oE '[0-9]+')
    if [[ -z "$SRC_TIMEOUT" ]]; then
        bad "$id: no DefaultTimeoutSeconds in the catalogue"
    elif [[ "$DOC_TIMEOUT" == "$SRC_TIMEOUT" ]]; then
        ok "$id: timeout ${SRC_TIMEOUT}s, document and catalogue agree"
    else
        bad "$id: document says timeout '$DOC_TIMEOUT', catalogue says '$SRC_TIMEOUT'"
    fi

    # the primary metric, same two-source treatment. The empty-source guard mirrors the timeout check
    # above: without it, a doc column that moved AND a grep window that missed would both yield the
    # empty string, compare equal, and report ok - a check that passes hardest when it is most broken.
    DOC_PRIMARY=$(printf '%s' "$ROW" | awk -F'|' '{print $5}' | tr -d ' ')
    SRC_PRIMARY=$(grep -A8 "Id = \"$id\"," "$SRC/Lxr.Harness.Core/ScenarioCatalog.cs" \
        | grep -oE 'Primary = PrimaryMetric\.[A-Za-z]+' | head -n 1 | sed 's/.*\.//' | tr 'A-Z' 'a-z')
    if [[ -z "$SRC_PRIMARY" ]]; then
        bad "$id: no Primary = PrimaryMetric.* in the catalogue"
    elif [[ -z "$DOC_PRIMARY" ]]; then
        bad "$id: the document's matrix table has no primary-metric column value"
    elif [[ "$DOC_PRIMARY" == "$SRC_PRIMARY" ]]; then
        ok "$id: primary metric '$SRC_PRIMARY', document and catalogue agree"
    else
        bad "$id: document says primary '$DOC_PRIMARY', catalogue says '$SRC_PRIMARY'"
    fi

    # a declared host requirement. Spelled out on every row including the default, because a
    # requirement declared by omission is indistinguishable from one nobody thought about.
    if grep -A8 "Id = \"$id\"," "$SRC/Lxr.Harness.Core/ScenarioCatalog.cs" \
        | grep -q 'RequiredCapabilities'; then
        ok "$id: declared host requirement"
    else
        bad "$id: no RequiredCapabilities in the catalogue"
    fi
done <<< "$SRC_IDS"

# a success marker mechanism must exist and be asserted, not merely defined
if grep -rq 'LXR-HARNESS-COMPLETE' "$SRC"; then
    ok "success marker prefix is defined"
else
    bad "no success marker prefix found in source"
fi
if grep -rq 'marker-missing' "$SRC"; then
    ok "a missing success marker has a named failure reason"
else
    bad "nothing in source rejects a run for a missing marker"
fi

# ---------------------------------------------------------------------------
section 'every control has recorded evidence that it fired'

CONTROL_NUMBERS=$(grep -oE '^### Control [0-9]+' "$DOC" | grep -oE '[0-9]+' | sort -n -u)
CONTROL_COUNT=$(printf '%s\n' "$CONTROL_NUMBERS" | grep -c . || true)
note "document describes $CONTROL_COUNT control(s)"

if [[ "$CONTROL_COUNT" -lt 7 ]]; then
    bad "the brief requires seven controls; the document describes $CONTROL_COUNT"
else
    ok "$CONTROL_COUNT controls described"
fi

FIRED=$(grep -cE '^control [0-9]+ - .*: FIRED' "$DOC" || true)
note "document contains $FIRED verbatim FIRED line(s)"
if [[ "$FIRED" -eq "$CONTROL_COUNT" ]]; then
    ok "every described control has pasted evidence that it fired"
else
    bad "$CONTROL_COUNT controls described but $FIRED pasted FIRED lines"
fi

# The control suite must be able to fail. A suite that can only pass is not a suite.
#
# The count is what makes this check real. An earlier version accepted a single literal 'Fired = false'
# anywhere in the file, which one early-out branch already satisfied - so the gate would have gone on
# passing even if all seven controls had been rewritten to 'Fired = true'. Requiring at least as many
# computed Fired expressions as there are controls means each one has to derive its outcome from
# something observed.
COMPUTED_FIRED=$(grep -cE 'Fired *=[^;]*(==|!=|&&|\|\||>=|<=|>|<|\bis\b)' "$SRC/Lxr.Harness.Runner/ControlSuite.cs" || true)
note "ControlSuite computes $COMPUTED_FIRED Fired value(s) from observations, for $CONTROL_COUNT control(s)"
if [[ "$COMPUTED_FIRED" -ge "$CONTROL_COUNT" ]]; then
    ok "every control derives its outcome from an observation rather than a literal"
else
    bad "$CONTROL_COUNT controls described but only $COMPUTED_FIRED computed Fired expression(s)"
fi

if grep -qE '^\s*bool\s+allFired|AllFired|control\.Fired' "$SRC/Lxr.Harness.Runner/ControlSuite.cs" \
    || grep -qE 'Fired' "$SRC/Lxr.Harness.Runner/Program.cs"; then
    ok "the control suite reports an aggregate outcome"
else
    bad "the control suite has no aggregate pass/fail"
fi

# ---------------------------------------------------------------------------
section 'results are schema-conformant and carry identity'

if [[ -n "$RESULTS" ]]; then
    while read -r file; do
        [[ -z "$file" ]] && continue
        rel=${file#"$ROOT/"}

        if grep -q '"schemaVersion": *2' "$file"; then
            ok "$rel: schemaVersion 2"
        else
            bad "$rel: missing or wrong schemaVersion"
        fi

        # every ok run must carry a confirmed collector and a runtime build identity
        OK_RUNS=$(grep -c '"status": *"ok"' "$file" || true)
        CONFIRMED=$(grep -c '"collectorConfirmed": *true' "$file" || true)
        BUILD_IDS=$(grep -c '"runtimeBuildId": *"' "$file" || true)

        if [[ "$OK_RUNS" -eq 0 ]]; then
            note "$rel: no ok runs to check"
        elif [[ "$CONFIRMED" -ge "$OK_RUNS" ]]; then
            ok "$rel: all $OK_RUNS ok run(s) confirmed their collector"
        else
            bad "$rel: $OK_RUNS ok run(s) but only $CONFIRMED confirmed collector identity"
        fi

        if [[ "$OK_RUNS" -eq 0 ]]; then
            :
        elif [[ "$BUILD_IDS" -ge "$OK_RUNS" ]]; then
            ok "$rel: all $OK_RUNS ok run(s) carry a runtime build identity"
        else
            bad "$rel: $OK_RUNS ok run(s) but only $BUILD_IDS runtime build id(s)"
        fi

        # a skip must be declared, never a silent absence
        SKIPS=$(grep -c '"status": *"skipped"' "$file" || true)
        SKIP_REASONS=$(grep -c '"skipReason": *"' "$file" || true)
        if [[ "$SKIPS" -eq 0 ]]; then
            :
        elif [[ "$SKIP_REASONS" -ge "$SKIPS" ]]; then
            ok "$rel: all $SKIPS skip(s) are declared with a reason"
        else
            bad "$rel: $SKIPS skip(s) but only $SKIP_REASONS reason(s)"
        fi
    done <<< "$RESULTS"
else
    skip_section "elected" "results not audited (--no-results): collector identity, schema conformance and runtime build identity are UNCHECKED"
fi

# ---------------------------------------------------------------------------
section 'no result was written outside the harness output directory'

STRAY=$(find "$ROOT/.github/extensions/lxr-gc-roadmap/benchmark-results" -newer "$DOC" -name '*.json' 2>/dev/null || true)
if [[ -z "$STRAY" ]]; then
    ok "nothing newer than the document was written into benchmark-results/"
else
    bad "the harness appears to have written into benchmark-results/, which is P0.5's job:"
    note "$STRAY"
fi

# the harness must default its output to the gitignored artifacts tree
if grep -rqE 'lxr-harness' "$SRC/Lxr.Harness.Runner" 2>/dev/null; then
    ok "harness output is rooted at artifacts/lxr-harness"
else
    bad "could not find the harness output root in the runner"
fi

if grep -rniE '\bC:\\+temp\b' "$SRC" "$HARNESS/scripts" 2>/dev/null | grep -vi 'not the shared temp' | grep -q .; then
    bad "something writes to the shared temp directory, which already holds gigabytes of stale dumps"
else
    ok "nothing targets the shared temp directory"
fi

# ---------------------------------------------------------------------------
section 'shell scripts are LF in the index'

if command -v git >/dev/null 2>&1 && git -C "$ROOT" rev-parse --git-dir >/dev/null 2>&1; then
    EOL=$(git -C "$ROOT" ls-files --eol -- 'docs/design/lxr-port/harness/scripts/*.sh' 2>/dev/null || true)
    if [[ -z "$EOL" ]]; then
        skip_section "inherent" "scripts are not tracked yet, so there is no index to check"
    elif printf '%s\n' "$EOL" | grep -q 'i/crlf'; then
        bad "a .sh file is CRLF in the index; bash cannot execute it"
        note "$EOL"
    else
        ok "every harness .sh is LF in the index"
        while read -r line; do note "$line"; done <<< "$EOL"
    fi
else
    skip_section "inherent" "not a git checkout, so there is no index to read (expected when auditing a git archive extract)"
fi

# ---------------------------------------------------------------------------
section 'build isolation'

if [[ -f "$HARNESS/Directory.Build.props" ]] && ! grep -qE '<Import|Sdk=' "$HARNESS/Directory.Build.props"; then
    ok "Directory.Build.props stops inheritance rather than importing Arcade"
else
    bad "Directory.Build.props does not stop inheritance"
fi

if grep -q '<clear' "$HARNESS/NuGet.config"; then
    ok "NuGet.config clears feeds; the harness has no package dependencies"
else
    bad "NuGet.config does not clear feeds"
fi

# Match the XML element, not the bare word - a comment saying "no PackageReference here" must not
# read as a violation.
if grep -rqE '<PackageReference' "$SRC" 2>/dev/null; then
    bad "a project has a PackageReference; the harness must build offline and run on corerun"
    note "$(grep -rlE '<PackageReference' "$SRC")"
else
    ok "no PackageReference anywhere in the harness"
fi

# The harness deliberately has no global.json: it builds with the repository's pinned SDK so its
# assemblies are in the same version band as a locally built runtime and can be loaded by it. One
# that rolled to a different SDK would break the corerun and testhost hosts, so its absence is
# asserted rather than merely tolerated - and the reason has to be written down where someone
# "fixing" the omission will see it.
if [[ -f "$HARNESS/global.json" ]]; then
    bad "harness/global.json exists; it would retarget the SDK away from the runtime under test"
elif grep -q 'no .global.json. here' "$HARNESS/README.md"; then
    ok "no harness global.json, and the README says why"
else
    bad "no harness global.json, but the README does not explain why one must not be added"
fi

# ---------------------------------------------------------------------------
close_section
printf '\n'

# A section that ran no checks and did not say so is a defect in this gate, not a mode. It is the
# exact shape of the bug that let an unaudited extract print PASS, so it fails rather than degrades.
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

# An elected skip is a legitimate way to run this, but it is not the same evidence as a full audit,
# so it gets its own exit code. A caller testing only for zero cannot mistake one for the other.
if [[ $ELECTED -gt 0 ]]; then
    printf 'RESULT: DEGRADED (%d checks passed, %d section(s) skipped by request)\n' "$PASS" "$ELECTED"
    printf 'This is NOT a full audit. Re-run with --results <dir> before treating it as evidence.\n'
    exit 3
fi

printf 'RESULT: PASS (%d checks)\n' "$PASS"
exit 0

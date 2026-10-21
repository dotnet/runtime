#!/usr/bin/env bash
# The gate of the gate, for P0.5.
#
# verify-baselines.sh is the thing that decides whether this step's numbers are trustworthy, which
# makes it the one script whose own defects are invisible when it passes. P0.5's document described
# five perturbations and the failures they produced, but shipped no way to re-run them -- so an
# auditor had to reconstruct them by hand. That is a reproducibility regression against P0.4, which
# shipped verify-harness-control.sh. This closes it.
#
# Structure follows P0.4's control script deliberately:
#
#   A  clean extract of committed content, outside any worktree        -> expect PASS
#   B  five copies, each with exactly one thing perturbed              -> expect a specific FAIL
#   C  the untouched extract again                                     -> expect PASS
#
# A and C are not ceremony. A gate that ignored its argument, or that failed for an unrelated
# reason, would satisfy B while proving nothing; requiring PASS on either side of the perturbations
# is what makes a FAIL in between attributable to the perturbation.
#
# Each case states its expectation before running, and prints the exit code and the matched line it
# compared, because a control that reports only pass/fail is a control nobody can audit.
#
# Usage: verify-gate-of-the-gate.sh [repo-root]
# Exit:  0 all cases behaved as required, 1 at least one did not, 2 the harness could not be set up.

set -uo pipefail

SELF_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

if [[ $# -ge 1 ]]; then
    REPO=$(cd "$1" && pwd)
else
    # scripts -> P0.5-baselines -> lxr-port -> design -> docs -> repo root
    REPO=$(cd "$SELF_DIR/../../../../.." && pwd)
fi

# A linked worktree's .git is a file holding a gitdir pointer, and on Windows that pointer is an
# absolute Windows path which WSL's git resolves relative to the current directory and then cannot
# find. git.exe reads it correctly. Prefer plain git so this behaves normally in a plain clone, and
# fall back rather than refusing to run in the worktree where the work was actually done.
#
# The call is `cd` plus a bare invocation rather than `-C`, because git.exe is a Windows binary and
# cannot interpret a /mnt/c path handed to -C, while it inherits a translated cwd correctly.
GIT=""
for candidate in git git.exe; do
    if command -v "$candidate" >/dev/null 2>&1 && (cd "$REPO" && "$candidate" rev-parse --git-dir) >/dev/null 2>&1; then
        GIT="$candidate"
        break
    fi
done

if [[ -z "$GIT" ]]; then
    printf 'not a git checkout, or no git that can read it: %s\n' "$REPO"
    exit 2
fi

git_at() { (cd "$REPO" && "$GIT" "$@"); }

PY=""
for candidate in python python3 py; do
    if command -v "$candidate" >/dev/null 2>&1; then PY="$candidate"; break; fi
done
if [[ -z "$PY" ]]; then
    printf 'no python on PATH; the perturbations edit JSON and cannot be applied\n'
    exit 2
fi

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

HEAD_SHA=$(git_at rev-parse HEAD)

printf 'gate-of-the-gate (P0.5)\n'
printf 'repo:    %s\n' "$REPO"
printf 'commit:  %s\n' "$HEAD_SHA"
printf 'scratch: %s\n\n' "$WORK"

EXTRACT="$WORK/extract"
mkdir -p "$EXTRACT"

# Committed content only. What is in the working tree is irrelevant: the artifact under audit is
# what is committed, and a CRLF working copy of an i/lf script is a different file than ships.
if ! git_at archive HEAD docs/design/lxr-port | tar -x -C "$EXTRACT"; then
    printf 'could not extract committed content\n'
    exit 2
fi

GATE_REL="docs/design/lxr-port/P0.5-baselines/scripts/verify-baselines.sh"
if [[ ! -f "$EXTRACT/$GATE_REL" ]]; then
    printf 'no %s in the extract\n' "$GATE_REL"
    exit 2
fi

PASSED=0
FAILED=0
# Counted by the required exit code of each case rather than written into the summary sentence. The
# literal that used to sit there said "five perturbations" long after there were seven, one line below
# a derived count -- a hardcoded claim inside the script whose whole job is catching hardcoded claims.
CLEAN_CASES=0
PERTURBATIONS=0
REFUSALS=0

# A fresh copy per case. Perturbations must not be able to leak into one another, or the last case
# would be auditing the accumulated damage of the earlier ones.
fresh_copy() {
    local dest="$WORK/$1"
    rm -rf "$dest"
    cp -R "$EXTRACT" "$dest"
    printf '%s' "$dest"
}

# Runs the gate and reports whether the outcome matched. Prints the exit code always, and the line
# that justified the verdict when one is required, so the comparison is visible rather than implied.
#
#   $1 label   $2 tree   $3 expected exit   $4 required pattern ('' = none)   $5.. gate args
run_case() {
    local label="$1" tree="$2" want_exit="$3" want_pattern="$4"
    shift 4
    local base="$tree/docs/design/lxr-port/P0.5-baselines"
    local out status matched=""

    case "$want_exit" in
        0) CLEAN_CASES=$((CLEAN_CASES + 1)) ;;
        1) PERTURBATIONS=$((PERTURBATIONS + 1)) ;;
        *) REFUSALS=$((REFUSALS + 1)) ;;
    esac

    out=$(cd "$base" && bash scripts/verify-baselines.sh "$@" 2>&1)
    status=$?

    local ok=1
    if [[ "$status" -ne "$want_exit" ]]; then
        ok=0
    fi
    if [[ -n "$want_pattern" ]]; then
        matched=$(printf '%s\n' "$out" | grep -m1 -E "$want_pattern" || true)
        [[ -z "$matched" ]] && ok=0
    fi

    # A perturbed tree that still prints a PASS verdict is the failure this whole script exists to
    # catch, so it is checked independently of the exit code rather than inferred from it.
    if [[ "$want_exit" -ne 0 ]]; then
        local pass_lines
        pass_lines=$(printf '%s\n' "$out" | grep -c '^RESULT: PASS' || true)
        printf '     "RESULT: PASS" lines in a case required to fail: %s (required 0)\n' "$pass_lines"
        [[ "$pass_lines" -ne 0 ]] && ok=0
    fi

    if [[ $ok -eq 1 ]]; then
        PASSED=$((PASSED + 1))
        printf '  ok   %s: exit %s (expected %s)\n' "$label" "$status" "$want_exit"
    else
        FAILED=$((FAILED + 1))
        printf '  BAD  %s: exit %s (expected %s)\n' "$label" "$status" "$want_exit"
    fi
    if [[ -n "$want_pattern" ]]; then
        if [[ -n "$matched" ]]; then
            printf '     matched: %s\n' "$(printf '%s' "$matched" | sed 's/^[[:space:]]*//')"
        else
            printf '     matched: NOTHING against /%s/\n' "$want_pattern"
        fi
    fi
}

# --------------------------------------------------------------------------- A
printf 'A  clean extract, expect PASS exit 0\n'
run_case 'clean extract' "$EXTRACT" 0 '^RESULT: PASS' --results .
printf '\n'

# --------------------------------------------------------------------------- B
printf 'B  one perturbation each, expect the named failure\n\n'

# B1 -- a published mean moved while the raw data it summarises is left alone. This is the property
# the whole raw-retention argument rests on: the summary must be a summary of the data beside it.
printf 'B1 published operationsPerSecond +5%%, raw CSV untouched -> expect re-derivation mismatch\n'
TREE=$(fresh_copy b1)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines/p0-5-baselines-s2.json" <<'PYEOF'
import json, pathlib, sys
path = pathlib.Path(sys.argv[1])
doc = json.loads(path.read_text(encoding="utf-8"))
for record in doc["checkpoints"][0]["results"]:
    if record.get("operationsPerSecond"):
        before = record["operationsPerSecond"]
        record["operationsPerSecond"] = round(before * 1.05, 4)
        print(f"     perturbed {record['scenario']}/{record.get('collector')}: {before} -> {record['operationsPerSecond']}")
        break
else:
    sys.exit("     no operationsPerSecond to perturb")
path.write_text(json.dumps(doc, indent=2), encoding="utf-8")
PYEOF
run_case 'doctored mean' "$TREE" 1 'mismatches' --results .
printf '\n'

# B2 -- the calibration reverted to P0.4's provisional state. P0.5's deliverable is measured minima,
# so a provisional entry must not be attestable.
printf 'B2 one calibration entry set provisional:true -> expect it named\n'
TREE=$(fresh_copy b2)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines/calibration.json" <<'PYEOF'
import json, pathlib, sys
path = pathlib.Path(sys.argv[1])
doc = json.loads(path.read_text(encoding="utf-8"))
entry = doc["baselines"][0]
entry["provisional"] = True
print(f"     perturbed {entry['scenario']}: provisional false -> true")
path.write_text(json.dumps(doc, indent=2), encoding="utf-8")
PYEOF
run_case 'provisional calibration' "$TREE" 1 'still provisional' --results .
printf '\n'

# B3 -- the shared heap no longer equals max(wks, srv), which would mean the two arms did not run at
# one absolute heap and every cross-arm comparison in the document is between different heaps.
printf 'B3 one sharedMinimumMb moved off max(wks,srv) -> expect it named\n'
TREE=$(fresh_copy b3)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines/calibration.json" <<'PYEOF'
import json, pathlib, sys
path = pathlib.Path(sys.argv[1])
doc = json.loads(path.read_text(encoding="utf-8"))
entry = doc["baselines"][0]
before = entry["sharedMinimumMb"]
entry["sharedMinimumMb"] = before + 7
print(f"     perturbed {entry['scenario']}: sharedMinimumMb {before} -> {entry['sharedMinimumMb']}")
path.write_text(json.dumps(doc, indent=2), encoding="utf-8")
PYEOF
run_case 'shared heap off max' "$TREE" 1 'not max\(wks, srv\)' --results .
printf '\n'

# B4 -- the defect that reopened this step. A sentence is edited to disagree with the CSV shipped
# beside it, while the CSV is left alone. Thirty gate checks passed over exactly this, because every
# check compared data to data and none compared a sentence to data.
printf 'B4 prose count edited to contradict the CSV -> expect the prose check to name it\n'
TREE=$(fresh_copy b4)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines.md" <<'PYEOF'
import pathlib, sys
path = pathlib.Path(sys.argv[1])
text = path.read_text(encoding="utf-8")
before = "Three of the four throughput ratios"
after = "All four of the four throughput ratios"
if before not in text:
    sys.exit("     the corrected sentence is not present to perturb")
print(f"     perturbed section 6.5: {before!r} -> {after!r}")
path.write_text(text.replace(before, after, 1), encoding="utf-8")
PYEOF
run_case 'prose contradicts data' "$TREE" 1 'throughput ratios below the floor' --results .
printf '\n'

# B5 -- the section 6.4 variance table edited away from the invocations it summarises. This table is
# the roadmap-critical one: it is what says LXR's acceptance metric is the one this host resolves
# worst, and it shipped with no generator at all.
printf 'B5 section 6.4 median CV altered, invocations untouched -> expect the re-derivation to name it\n'
TREE=$(fresh_copy b5)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines.md" <<'PYEOF'
import pathlib, sys
path = pathlib.Path(sys.argv[1])
lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
for index, line in enumerate(lines):
    if line.startswith("| `operationsPerSecond` |"):
        before = line.strip()
        lines[index] = line.replace("**1.60%**", "**1.42%**")
        print(f"     perturbed section 6.4 row: {before}")
        print(f"                            to: {lines[index].strip()}")
        break
else:
    sys.exit("     no section 6.4 operationsPerSecond row to perturb")
path.write_text("".join(lines), encoding="utf-8")
PYEOF
run_case 'variance table drifted' "$TREE" 1 'document row lacks' --results .
printf '\n'

# B6 -- the section 5.1 binding-arm count, which is the claim that silently passed the first version
# of the prose checker. Two facts in that section share the value "30 of 30", and a presence test was
# satisfied by whichever one was left truthful, so falsifying the other went undetected. This case
# exists because that defect was found by perturbation and would not have been found by reading.
printf 'B6 section 5.1 binding-arm count falsified, CSV untouched -> expect the prose check to name it\n'
TREE=$(fresh_copy b6)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines.md" <<'PYEOF'
import pathlib, sys
path = pathlib.Path(sys.argv[1])
lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
before = "binding arm in 30 of 30"
after = "binding arm in 29 of 30"
# The section 9.1 write-up of this very check quotes the string this case perturbs, so a
# whole-document replace hits two places and a uniqueness guard refuses outright -- which is what
# happened the first time this case ran. Target the section 5.1 bullet by the claim it states, not
# by a substring that documentation is free to mention.
hits = [i for i, line in enumerate(lines)
        if before in line and "sharedMinimumMb == max(wks, srv)" in line]
if len(hits) != 1:
    sys.exit(f"     expected exactly one section 5.1 binding-arm bullet, found {len(hits)}")
index = hits[0]
print(f"     perturbed section 5.1 line {index + 1}: {before!r} -> {after!r}")
print("     the other '30 of 30' on that line is deliberately left true")
lines[index] = lines[index].replace(before, after, 1)
path.write_text("".join(lines), encoding="utf-8")
PYEOF
# The expected text is the DERIVED VALUE the gate must name, not the check's label. This case first
# matched on 'srv is the binding arm', which was the label's wording -- so renaming the label to say
# "attains the shared minimum" broke a control over a gate that was working correctly. Binding an
# assertion to incidental wording rather than to the fact is the same mistake the prose claims were
# corrected for, one level up: here the text under test is the tool's output rather than the
# document's.
run_case 'binding-arm count falsified' "$TREE" 1 'binding arm in 30 of 30' --results .

# B7 -- nothing to audit. The gate must refuse rather than report a PASS over an empty tree, which
# is the failure mode that produced a false PASS in an earlier step.
printf 'B7 checkpoints removed, bare invocation -> expect refusal exit 2\n'
TREE=$(fresh_copy b7)
removed=$(find "$TREE/docs/design/lxr-port/P0.5-baselines" -maxdepth 1 -name 'p0-5-baselines-*.json' -delete -print | wc -l)
printf '     removed %s checkpoint file(s), leaving 0\n' "$removed"
run_case 'refuses with nothing present' "$TREE" 2 'no checkpoint json'
printf '\n'

# B8 -- a section that contributes no checks and declares no skip. The zero-check guard exists
# because silence in a section is what makes an unaudited tree look audited.
printf 'B8 a skip declaration deleted so a section runs no checks silently -> expect zero-check guard\n'
TREE=$(fresh_copy b8)
GATE="$TREE/$GATE_REL"
before_skips=$(grep -c 'skip_section elected "--no-results' "$GATE" || true)
"$PY" - "$GATE" <<'PYEOF'
import pathlib, sys
path = pathlib.Path(sys.argv[1])
lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
for index, line in enumerate(lines):
    if 'skip_section elected "--no-results' in line:
        # Replace the declaration with a no-op so the section still runs nothing but no longer says
        # so. Deleting the line would break the elif chain and fail for the wrong reason.
        lines[index] = line.split("skip_section")[0] + ":\n"
        print(f"     neutralised the skip declaration at line {index + 1}")
        break
else:
    sys.exit("     no elected --no-results skip declaration found")
path.write_text("".join(lines), encoding="utf-8")
PYEOF
after_skips=$(grep -c 'skip_section elected "--no-results' "$GATE" || true)
printf '     elected --no-results declarations: %s before, %s after, difference %s\n' \
    "$before_skips" "$after_skips" "$((before_skips - after_skips))"
run_case 'zero-check guard' "$TREE" 1 'ran no checks and did not declare a skip' --no-results
printf '\n'

# B9 -- a file:line citation moved past the end of the file it cites. Citations rot silently: the file
# stays present and the gate's other checks stay green, because every one of them compares data to data
# or a name to a file. Nothing before this case read a line number. The perturbation leaves every CSV,
# every checkpoint and every script untouched.
printf 'B9 a file:line citation pushed past end of file, tree otherwise untouched -> expect the citation check to name it\n'
TREE=$(fresh_copy b9)
"$PY" - "$TREE/docs/design/lxr-port/P0.5-baselines.md" <<'PYEOF'
import pathlib, re, sys
path = pathlib.Path(sys.argv[1])
lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
# Located by pattern rather than by a literal, so that documenting this case cannot break it the way
# documenting B6 broke B6. Any single citation of the run script will do; the case is about the shape.
pattern = re.compile(r"(run-p05-baselines\.ps1):(\d+)")
hits = [i for i, line in enumerate(lines) if pattern.search(line)]
if len(hits) != 1:
    sys.exit(f"     expected exactly one line:number citation of the run script, found {len(hits)}")
index = hits[0]
old = pattern.search(lines[index]).group(0)
new = pattern.sub(lambda m: f"{m.group(1)}:99{m.group(2)}", lines[index])
print(f"     perturbed line {index + 1}: {old!r} pushed past end of file")
lines[index] = new
path.write_text("".join(lines), encoding="utf-8")
PYEOF
run_case 'citation past end of file' "$TREE" 1 'resolve, bad:' --no-results
printf '\n'

# --------------------------------------------------------------------------- C
printf 'C  untouched extract again, expect PASS exit 0\n'
run_case 'clean extract, second time' "$EXTRACT" 0 '^RESULT: PASS' --results .
printf '\n'

TOTAL=$((PASSED + FAILED))
if [[ $FAILED -gt 0 ]]; then
    printf 'RESULT: FAIL (%d of %d cases behaved as required)\n' "$PASSED" "$TOTAL"
    printf 'A case that did not behave as required means verify-baselines.sh does not discriminate\n'
    printf 'the way P0.5-baselines.md section 9 says it does.\n'
    exit 1
fi

printf 'RESULT: PASS (%d of %d cases behaved as required)\n' "$PASSED" "$TOTAL"
printf 'The gate passed %d clean extract(s), refused %d, and failed %d single perturbation(s).\n' \
    "$CLEAN_CASES" "$REFUSALS" "$PERTURBATIONS"
exit 0

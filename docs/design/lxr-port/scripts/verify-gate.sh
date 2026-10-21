#!/bin/bash
# Gate-of-the-gate: prove that verify-ledger.sh audits the tree it is run in.
#
# verify-ledger.sh once defaulted its documents directory to an absolute path into one worktree, so
# running it from a fresh extract silently audited a different checkout and reported a clean result
# about an artifact the operator was not looking at (probes 8, correction 6).  A passing run is not
# evidence that a check is reading anything, so this script does three things:
#
#   A. run the COMMITTED verify-ledger.sh against a clean tree, with NO argument
#   B. copy that tree, delete one index row from the COPY, and require the gate to FAIL on the copy
#   C. re-run the gate on the original and require it to still PASS
#
# Step B is the point.  Without it, A and C would both have passed just as happily when the default
# path was hardcoded - which is exactly how the defect survived.  A and C together add the other half:
# one gate, two trees differing by a single row, PASS on one and FAIL on the other.  The perturbation
# is applied to a copy, never to the tree under test, so this script is safe to run anywhere.
#
# Usage:  verify-gate.sh [<tree-root>]
#   <tree-root> contains docs/design/lxr-port/...  Supply an extract of the COMMITTED tree, e.g.
#       git archive HEAD docs/design/lxr-port -o /tmp/gate.tar
#       mkdir -p /tmp/gate && tar -xf /tmp/gate.tar -C /tmp/gate
#       docs/design/lxr-port/scripts/verify-gate.sh /tmp/gate
#   With no argument the script makes that extract itself.  Note that a git worktree checked out from
#   Windows has a .git gitlink holding a Windows path, which git inside WSL cannot follow - in that
#   case create the tar on the Windows side and pass the extract explicitly.
set -uo pipefail
SELF_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
LIVE=$(cd "$SELF_DIR/.." && pwd)
ROW=${ROW:-E02}                      # the index row the control deletes; must exist exactly once
FAIL=0
TMP=

if [ $# -ge 1 ]; then
  TREE=$1
else
  REPO=$(cd "$LIVE/../../.." && pwd)
  TMP=$(mktemp -d)
  # A worktree checked out from Windows has a .git gitlink holding a Windows path, which git inside
  # WSL cannot follow.  Refuse clearly rather than auditing something else - that is the whole point.
  if ! git -C "$REPO" archive HEAD docs/design/lxr-port 2>/dev/null | tar -x -C "$TMP" 2>/dev/null; then
    rm -rf "$TMP"
    echo "could not 'git archive HEAD' from $REPO"
    echo "pass an extract of the committed tree as an argument instead, e.g."
    echo "  git archive HEAD docs/design/lxr-port -o /tmp/gate.tar   # on the side where git works"
    echo "  mkdir -p /tmp/gate && tar -xf /tmp/gate.tar -C /tmp/gate"
    echo "  $0 /tmp/gate"
    exit 2
  fi
  TREE=$TMP
fi
G=$TREE/docs/design/lxr-port/scripts/verify-ledger.sh
[ -f "$G" ] || { echo "no verify-ledger.sh under $TREE"; exit 2; }
echo "tree under test: $TREE"

echo "########## A. committed gate, clean tree, no argument ##########"
if grep -q $'\r' "$G"; then echo "  FAIL  CRLF in the committed script - bash cannot run it"; FAIL=1
else echo "  line endings: LF, runs without conversion"; fi
bash "$G" > /tmp/gate-A.log 2>&1; a=$?
grep -E 'index rows|self-reported|detail blocks|distinct citations|UNACK|VERIFY' /tmp/gate-A.log | sed 's/^/  /'
echo "  exit=$a"
[ "$a" -eq 0 ] || { echo "  FAIL  the committed tree does not pass its own gate"; FAIL=1; }

echo
echo "########## B. positive control: same gate, a COPY differing by one row ##########"
CP=$(mktemp -d)
cp -r "$TREE/docs" "$CP/" || { echo "  FAIL  could not copy the tree"; FAIL=1; }
GC=$CP/docs/design/lxr-port/scripts/verify-ledger.sh
LC=$CP/docs/design/lxr-port/P0.3-parity-ledger.md
before=$(grep -c "^| $ROW |" "$LC")
echo "  $ROW index rows in the copy, before: $before"
if [ "$before" -ne 1 ]; then
  echo "  FAIL  control invalid - $ROW is not present exactly once, so the control deletes nothing"
  FAIL=1
else
  sed -i "/^| $ROW | /d" "$LC"
  echo "  $ROW index rows in the copy, after : $(grep -c "^| $ROW |" "$LC")"
  bash "$GC" > /tmp/gate-B.log 2>&1; b=$?
  grep -E 'index rows|self-reported|detail blocks|FAIL|VERIFY' /tmp/gate-B.log | sed 's/^/  /'
  echo "  exit=$b"
  if [ "$b" -eq 0 ]; then
    echo "  FAIL  the gate passed on a tree missing a row - it is not reading the tree it is given"
    FAIL=1
  else
    echo "  control fired: the gate saw a change made ONLY in the copy"
  fi
fi
rm -rf "$CP"

echo
echo "########## C. the tree under test was never touched ##########"
bash "$G" > /tmp/gate-C.log 2>&1; c=$?
grep -E 'index rows|detail blocks|UNACK|VERIFY' /tmp/gate-C.log | sed 's/^/  /'
echo "  exit=$c"
[ "$c" -eq 0 ] || { echo "  FAIL  the tree under test no longer passes"; FAIL=1; }

[ -n "$TMP" ] && rm -rf "$TMP"
echo
[ "$FAIL" -eq 0 ] && { echo "GATE-OF-GATE: PASS"; exit 0; } || { echo "GATE-OF-GATE: FAIL"; exit 1; }

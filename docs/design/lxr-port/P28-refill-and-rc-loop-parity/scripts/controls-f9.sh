#!/bin/bash
# P28 (F9): the controls of the refill tests. Each control is a build define that breaks one behaviour of the hole search or the
# refill; the validation program of P2.4 must then FAIL. The first line (no define) must PASS.
# usage: controls-f9.sh <repo-root-as-WSL-path> <output-dir>
set -u
REPO="$1"
OUT="$2"
mkdir -p "$OUT"
SCRIPT="$REPO/docs/design/lxr-port/P2.4/run-immix-allocation-validation.sh"
failed=0
for control in "" LXR_F9_SCAN_ONE_WORD_CONTROL LXR_F9_EAGER_SCAN_CONTROL LXR_F9_CLEAR_AT_REFILL_CONTROL LXR_F9_NO_VERIFY_CONTROL LXR_F9_PREFILTER_CONTROL LXR_F9_NO_CURSOR_RESET_CONTROL LXR_F10_PLAIN_STORE_CONTROL LXR_F10_BYTE_PLAIN_CONTROL LXR_F10_NO_SATURATE_CONTROL LXR_F10_PREFETCH_OFFBYONE_CONTROL; do
  name=${control:-baseline}
  DEFINES=""
  [ -n "$control" ] && DEFINES="-D$control"
  P2_4_EXTRA_DEFINES="$DEFINES" bash "$SCRIPT" "$REPO" "$OUT/$name" > "$OUT/$name.log" 2>&1
  code=$?
  fails=$(grep -c '^FAIL:' "$OUT/$name/run.log")
  first=$(grep '^FAIL:' "$OUT/$name/run.log" | sort -u | head -3 | tr '\n' '|')
  if [ -z "$control" ]; then
    [ $code -eq 0 ] || failed=1
    echo "baseline exit=$code :: $(grep -E 'PASS|passed' "$OUT/$name.log" | tail -1)"
  else
    [ $code -ne 0 ] || failed=1
    echo "$control exit=$code :: failing lines=$fails :: $first"
  fi
done
echo "controls failed=$failed"

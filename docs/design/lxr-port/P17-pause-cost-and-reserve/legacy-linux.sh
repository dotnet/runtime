#!/bin/bash
# Run the older LXR validation suites (P2.1 to P3.4) from a source tree.
# usage: legacy-linux.sh <repo-root-as-WSL-path> <output-dir> [name-filter-regex]
REPO=$1
OUT=$2
FILTER=${3:-.}
mkdir -p "$OUT"
cd "$REPO/docs/design/lxr-port"
failed=0
for script in P2.1/run-side-metadata-validation.sh P2.2/run-immix-block-validation.sh P2.3/run-reference-count-validation.sh \
    P2.4/run-immix-allocation-validation.sh P2.5/run-large-object-space-validation.sh P3.1/run-lxr-work-packet-validation.sh \
    P3.2/run-lxr-pause-validation.sh P3.3/run-lxr-deferred-bucket-validation.sh P3.4/run-lxr-completion-counter-validation.sh; do
  name=$(basename "$script" .sh)
  [[ $name =~ $FILTER ]] || continue
  sed 's/\r$//' "$script" > "/tmp/$name.sh"
  sub="$OUT/$name"
  # the scripts locate the repository from their own directory, so run a copy next to the original
  cp "/tmp/$name.sh" "$(dirname "$script")/.p17-$name.sh"
  bash "$(dirname "$script")/.p17-$name.sh" "$REPO" "$sub" > "$OUT/$name.log" 2>&1
  code=$?
  rm -f "$(dirname "$script")/.p17-$name.sh"
  echo "$name exit=$code :: $(grep -E 'PASS|passed|checks' "$OUT/$name.log" | tail -1)"
  [ $code -eq 0 ] || failed=1
done
echo "legacy failed=$failed"

#!/bin/bash
# Print the self-time table of a perf recording and the share of the side-metadata functions.
# usage: perf-report.sh <data-file> [top-count]
# The collector module must have its symbols (the .so.dbg file sits next to the .so in the frame).
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
DATA=$1
TOP=${2:-30}
$PERF report -i "$DATA" --no-children -g none --sort dso,symbol --stdio 2>/dev/null \
  | grep -v '^#' | grep -v '^$' > "${DATA%.data}.self.txt"
head -n "$TOP" "${DATA%.data}.self.txt"
echo "--- side-metadata share (self time, percent of all samples)"
awk '/SideMetadataManager|SideMetadataAccessor/ { gsub("%","",$1); sum += $1 } END { printf "SideMetadata total self: %.2f\n", sum }' "${DATA%.data}.self.txt"
echo "--- inclusive (children) share of the checked functions and of the engine entry points"
$PERF report -i "$DATA" --children -g none --sort symbol --stdio 2>/dev/null \
  | grep -v '^#' | grep -v '^$' > "${DATA%.data}.incl.txt"
grep -E 'SideMetadataManager::|ReferenceCountTable::|ImmixBlockManager::|LxrReferenceCounting::(Process|Execute|Death|Reclaim|Sweep|Release|TryCopy)' "${DATA%.data}.incl.txt" | head -n 25
for name in 'SideMetadataManager::Load' 'SideMetadataManager::Store' 'SideMetadataManager::GetLocation' 'SideMetadataManager::Update' 'SideMetadataManager::IsDataAddressOwned' 'SideMetadataManager::CompareExchange' 'SideMetadataManager::IncrementSaturating' 'SideMetadataManager::DecrementNonZero'; do
  awk -v n="$name" 'index($0, n) { gsub("%","",$1); sum += $1 } END { printf "%-48s %.2f\n", n, sum }' "${DATA%.data}.self.txt"
done

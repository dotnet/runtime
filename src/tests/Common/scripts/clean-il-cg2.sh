#!/usr/bin/env bash
#
# Deletes every IL-CG2 directory under the artifacts tree.
#
# The runtime test scripts crossgen the test assemblies into a per-test IL-CG2
# directory and skip that step when the directory already contains a "done"
# marker. A stale IL-CG2 therefore keeps a test running against R2R images
# produced by an older compiler, so it has to be removed whenever crossgen2,
# the JIT, or the JIT-EE interface changes.
#
# Usage: clean-il-cg2.sh [-n|--dry-run] [artifacts-directory]

set -euo pipefail

dryRun=0
artifactsDir=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        -n|--dry-run)
            dryRun=1
            ;;
        -h|--help)
            sed -n '2,12p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        -*)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
        *)
            if [[ -n "$artifactsDir" ]]; then
                echo "Unexpected argument: $1" >&2
                exit 1
            fi
            artifactsDir="$1"
            ;;
    esac
    shift
done

if [[ -z "$artifactsDir" ]]; then
    repoRoot="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
    artifactsDir="$repoRoot/artifacts"
fi

if [[ ! -d "$artifactsDir" ]]; then
    echo "No artifacts directory at '$artifactsDir'; nothing to do."
    exit 0
fi

count=0
while IFS= read -r -d '' dir; do
    if [[ $dryRun -eq 1 ]]; then
        echo "Would delete: $dir"
    else
        rm -rf "$dir"
    fi
    count=$((count + 1))
done < <(find "$artifactsDir" -type d -name IL-CG2 -prune -print0)

if [[ $dryRun -eq 1 ]]; then
    echo "$count IL-CG2 director$([[ $count -eq 1 ]] && echo y || echo ies) would be deleted under '$artifactsDir'."
else
    echo "Deleted $count IL-CG2 director$([[ $count -eq 1 ]] && echo y || echo ies) under '$artifactsDir'."
fi

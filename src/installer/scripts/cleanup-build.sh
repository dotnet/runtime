#!/usr/bin/env bash
# Restores BuildTools package, and runs cleanup-docker.sh

# Get repository root.
scriptpath=$(cd "$(dirname "$0")"; pwd -P)
repoRoot=$(cd "$scriptpath/.."; pwd -P)

# Restore BuildTools package.
"$repoRoot/pkg/init-tools.sh"

# Run the script to cleanup Docker containers and images.
"$repoRoot/pkg/Tools/scripts/docker/cleanup-docker.sh"

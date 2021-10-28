#!/usr/bin/env bash

set -e

# prebuild the repo, so it is ready for development
./build.sh libs+clr -rc Release

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha

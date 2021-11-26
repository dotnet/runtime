#!/usr/bin/env bash

set -e

# prebuild the repo, so it is ready for development
./build.sh libs+clr -rc Release
# restore libs tests so that the project is ready to be loaded by OmniSharp
./build.sh libs.tests -restore

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha

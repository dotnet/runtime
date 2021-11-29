#!/usr/bin/env bash

set -e

# prebuild the repo, so it is ready for development
./build.sh libs+clr -rc Release

#!/usr/bin/env bash

set -e

# reset the repo to the commit hash that was used to build the prebuilt Codespace
git reset --hard $(cat ./artifacts/prebuild.sha)

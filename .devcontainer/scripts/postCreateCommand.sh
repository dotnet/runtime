#!/usr/bin/env bash

set -e

# reset the repo to the commit hash that was used to build the prebuilt Codespace
git reset --hard $(cat ./artifacts/prebuild.sha)

# add harness --no-sandbox argument if running in container; reload changes
echo 'export NO_SANDBOX_IN_CONTAINER="--no-sandbox"' >> ~/.bashrc
exec bash

#!/usr/bin/env bash

set -e

# Install SDK and tool dependencies before container starts
./dotnet.sh

# The container creation script is executed in a new Bash instance
# so we exit at the end to avoid the creation process lingering.
exit
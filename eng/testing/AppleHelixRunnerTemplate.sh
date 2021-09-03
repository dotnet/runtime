#!/usr/bin/env bash

set -x

EXECUTION_DIR=$(dirname $0)
XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

# RunCommands defined in tests.mobile.targets
[[RunCommands]]
#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)

cd $EXECUTION_DIR

if [[ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]]; then
	XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
else
	XHARNESS_OUT="$HELIX_WORKITEM_UPLOAD_ROOT/xharness-output"
fi

export TEST_LOG_PATH=${XHARNESS_OUT}/logs

[[RunCommands]]

_exitCode=$?

echo "artifacts: $XHARNESS_OUT"

exit $_exitCode

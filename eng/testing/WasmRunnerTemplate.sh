#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
[[RunCommands]]

cd $EXECUTION_DIR

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

if [ ! -x "$(command -v xharness)" ]; then
	HARNESS_RUNNER="dotnet"
fi

$HARNESS_RUNNER xharness wasm test --engine=${JAVASCRIPT_ENGINE} \
    --js-file=runtime.js -v \
    --output-directory=$XHARNESS_OUT \
    -- --enable-gc --run WasmTestRunner.dll ${TEST_NAME}.dll

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode

#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)

echo "Test: $1"

cd $EXECUTION_DIR

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

dotnet xharness wasm test --engine=v8 \
    --js-file=runtime.js \
    --output-directory=$XHARNESS_OUT \
    -- --enable-gc --run WasmTestRunner.dll $*

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode

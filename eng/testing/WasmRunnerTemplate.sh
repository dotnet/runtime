#!/usr/bin/env bash

JAVASCRIPT_ENGINE=$0
EXECUTION_DIR=$(dirname $1)

cd $EXECUTION_DIR

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

dotnet xharness wasm test --engine=$JAVASCRIPT_ENGINE \
    --js-file=runtime.js \
    --output-directory=$XHARNESS_OUT \
    -- --enable-gc --run WasmTestRunner.dll ${@:2}

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode

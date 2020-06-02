set -ev

EXECUTION_DIR=$(dirname $0)

echo "Test: $1"

cd $EXECUTION_DIR
v8 --expose_wasm runtime.js -- --enable-gc --run WasmTestRunner.dll $*

exit 0

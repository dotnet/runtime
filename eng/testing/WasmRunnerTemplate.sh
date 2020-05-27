set -ev

EXECUTION_DIR=$(dirname $0)
TEST_NAME=$1
TARGET_ARCH=$2

echo "Test: $1 Arch: $2"

cd $EXECUTION_DIR
v8 --expose_wasm runtime.js -- --enable-gc --run WasmTestRunner.dll $TEST_NAME

exit 0

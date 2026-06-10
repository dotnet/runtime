#!/bin/bash
set -euo pipefail
set -x

STAGE="${1:-all}"
export DEBIAN_FRONTEND=noninteractive
[ -f /env.sh ] && source /env.sh

RUNTIME_DIR="$(pwd)/runtime"
TEST_DIR="$(pwd)/JIT_Testing"

echo "=================================="
echo "STAGE: $STAGE"
echo "=================================="

# =========================================================
# SETUP FUNCTION
# =========================================================
setup() {
  echo "Installing dependencies..."

  apt-get update
  apt-get install -y \
    bc automake clang curl findutils git hostname libtool \
    libkrb5-dev ninja-build llvm make python3 cmake \
    liblttng-ust-dev tar wget jq lld \
    build-essential zlib1g-dev libssl-dev libbrotli-dev \
    ca-certificates

  echo "Cloning runtime..."
  git clone --recurse-submodules https://github.com/alhad-deshpande/runtime.git
  cd runtime
  git checkout ppc64le_coreclr_jit
  cd ..

  echo "Setting up .NET SDK..."

  SDK_VERSION=$(jq -r '.sdk.version' runtime/global.json)

  export DOTNET_DIR="/dotnet-sdk-$(uname -m)"
  mkdir -p "$DOTNET_DIR"

  pushd "$DOTNET_DIR"

  wget "https://github.com/IBM/dotnet-s390x/releases/download/v${SDK_VERSION}/dotnet-sdk-${SDK_VERSION}-linux-$(uname -m).tar.gz"

  mkdir -p .dotnet
  tar xvf dotnet-sdk-*linux-$(uname -m).tar.gz -C .dotnet > /dev/null

  export DOTNET_ROOT="$(realpath .dotnet)"
  export PATH="$DOTNET_ROOT:$PATH"

  popd

  echo "DOTNET installed:"
  dotnet --version

  echo "export DOTNET_ROOT=$DOTNET_ROOT" > /env.sh
  echo "export PATH=$DOTNET_ROOT:\$PATH" >> /env.sh
}

# =========================================================
# BUILD RUNTIME
# =========================================================
build_runtime() {
  cd runtime
  export PATH="$DOTNET_ROOT:$PATH"
  export DOTNET_MULTILEVEL_LOOKUP=0
  export UseInstalledDotNetCli=true

  ./build.sh clr+clr.hosts \
  -skipmanagedtools \
  /p:PrimaryRuntimeFlavor=CoreCLR \
  /p:PublishAot=false \
  /p:SupportsNativeAotComponents=false \
  2>&1 | tee build.log
}

# =========================================================
# BUILD LIBS
# =========================================================
build_libs() {
  cd runtime
  ./build.sh libs 2>&1 | tee build_libs.log
}

# =========================================================
# BUILD TESTS
# =========================================================
build_tests() {
  cd runtime

  ./src/tests/build.sh /p:LibrariesConfiguration=Debug 2>&1 | tee build_tests.log

  CORE_ROOT="./artifacts/tests/coreclr/linux.ppc64le.Debug/Tests/Core_Root"

  cp "${CORE_ROOT}/IL/System.Private.CoreLib.dll" \
     "${CORE_ROOT}/System.Private.CoreLib.dll"
}

# =========================================================
# RUN TESTS
# =========================================================
run_tests() {
  cd "$TEST_DIR"

  echo "Cloning JIT tests..."
  git clone https://github.com/alhad-deshpande/JIT_Testing.git
  cd JIT_Testing

  echo "Commit:"
  git log --oneline -1

  echo "Test count:"
  find testcases -name "*.cs" | wc -l

  chmod +x run_test.sh

  set +e
  ./run_test.sh "$DOTNET_ROOT" "$RUNTIME_DIR"
  EXIT_CODE=$?
  set -e

  if [ $EXIT_CODE -ne 0 ]; then
    echo " JIT Tests FAILED"
    exit $EXIT_CODE
  fi

  echo " JIT Tests PASSED"
}

# =========================================================
# MAIN DISPATCHER
# =========================================================
case "$STAGE" in
  setup)
    setup
    ;;
  build_runtime)
    build_runtime
    ;;
  build_libs)
    build_libs
    ;;
  build_tests)
    build_tests
    ;;
  run_tests)
    run_tests
    ;;
  all)
    setup
    build_runtime
    build_libs
    build_tests
    run_tests
    ;;
  *)
    echo "Invalid stage: $STAGE"
    exit 1
    ;;
esac

echo "=================================="
echo "DONE"
echo "=================================="

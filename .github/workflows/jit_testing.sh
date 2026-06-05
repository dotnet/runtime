#!/bin/bash

set -ex

export DEBIAN_FRONTEND=noninteractive

echo "=========================================="
echo "Installing dependencies"
echo "=========================================="

apt-get update
apt-get install -y bc automake clang curl findutils git hostname libtool libkrb5-dev ninja-build llvm make python3 liblttng-ust-dev tar wget jq lld \
build-essential zlib1g-dev libssl-dev libbrotli-dev ca-certificates

echo "=========================================="
echo "Environment Information"
echo "=========================================="

uname -a
whoami
pwd

WORKSPACE=$(pwd)

echo "=========================================="
echo "Cloning Runtime Repository"
echo "=========================================="

git clone --recurse-submodules https://github.com/alhad-deshpande/runtime.git
cd runtime
git checkout ppc64le_coreclr_jit

echo "=========================================="
echo "Fixing Arcade SDK version"
echo "=========================================="

# Use the nearest available version instead of non-existent 8.0.0
# The error showed that 9.0.0-beta.23503.3 is available in dotnet-eng feed
sed -i 's/9.0.0-beta.25208.6/9.0.0-beta.23503.3/g' global.json || true
cat global.json

echo "=========================================="
echo "Reading SDK Version"
echo "=========================================="

GLOBAL_JSON_PATH="global.json"

if [ ! -f "$GLOBAL_JSON_PATH" ]; then
    echo "global.json not found"
    exit 1
fi

SDK_VERSION=$(jq -r '.sdk.version' "$GLOBAL_JSON_PATH")

echo "SDK_VERSION=$SDK_VERSION"

echo "=========================================="
echo "Installing .NET SDK"
echo "=========================================="

ARCH=$(uname -m)

DOTNET_DIR="/dotnet-sdk-${ARCH}"

mkdir -p "$DOTNET_DIR"
pushd "$DOTNET_DIR"

SDK_URL="https://github.com/IBM/dotnet-s390x/releases/download/v${SDK_VERSION}/dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz"

echo "Downloading SDK from:"
echo "$SDK_URL"

wget "$SDK_URL"

mkdir -p .dotnet

tar -xvf dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz \
    -C .dotnet

export DOTNET_ROOT=$(pwd)/.dotnet
export PATH=$DOTNET_ROOT:$PATH

popd

echo "=========================================="
echo ".NET Information"
echo "=========================================="

which dotnet
dotnet --info

# Clean NuGet cache
rm -rf ~/.nuget/packages

# Valid public feeds with proper ordering - dotnet-eng first for Arcade SDK
RESTORE_SOURCES="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json;\
https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json;\
https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json;\
https://api.nuget.org/v3/index.json"

echo "=========================================="
echo "Configuring NuGet Sources"
echo "=========================================="

# Create NuGet.config to ensure proper package resolution and disable unavailable feeds
cat > NuGet.config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="dotnet-eng" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json" />
    <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
    <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <disabledPackageSources>
    <!-- Disable private/unavailable feeds that may be referenced in project files -->
    <add key="darc-pub-dotnet-emsdk-78f6f07d" value="true" />
    <add key="darc-pub-dotnet-sdk-fe6d1ce" value="true" />
    <add key="dotnet-libraries-transport" value="true" />
    <add key="dotnet-libraries" value="true" />
    <add key="dotnet9-transport" value="true" />
    <add key="dotnet9" value="true" />
  </disabledPackageSources>
</configuration>
EOF

cat NuGet.config

echo "=========================================="
echo "Removing problematic NuGet.config files from repository"
echo "=========================================="

# Remove any existing NuGet.config files that might reference unavailable feeds
find . -name "NuGet.config" -type f -not -path "./NuGet.config" -exec rm -f {} \; 2>/dev/null || true

echo "=========================================="
echo "Building Runtime"
echo "=========================================="

# Set environment variable to skip unavailable package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

./build.sh \
    clr+clr.hosts \
    /p:PrimaryRuntimeFlavor=CoreCLR \
    /p:PublishAot=false \
    /p:SupportsNativeAotComponents=false \
    /p:RestoreSources="$RESTORE_SOURCES" \
    /p:RestoreIgnoreFailedSources=true \
    | tee build.log

BUILD_EXIT_CODE=${PIPESTATUS[0]}

if [ $BUILD_EXIT_CODE -ne 0 ]; then
    echo "=========================================="
    echo "ERROR: Runtime build failed with exit code $BUILD_EXIT_CODE"
    echo "=========================================="
    tail -n 100 build.log
    exit $BUILD_EXIT_CODE
fi

echo "=========================================="
echo "Building Libraries"
echo "=========================================="

./build.sh libs \
    /p:RestoreSources="$RESTORE_SOURCES" \
    /p:RestoreIgnoreFailedSources=true

LIBS_EXIT_CODE=$?

if [ $LIBS_EXIT_CODE -ne 0 ]; then
    echo "=========================================="
    echo "ERROR: Libraries build failed with exit code $LIBS_EXIT_CODE"
    echo "=========================================="
    exit $LIBS_EXIT_CODE
fi

echo "=========================================="
echo "Building Tests"
echo "=========================================="

./src/tests/build.sh \
    /p:LibrariesConfiguration=Debug \
    /p:RestoreSources="$RESTORE_SOURCES" \
    /p:RestoreIgnoreFailedSources=true

TESTS_EXIT_CODE=$?

if [ $TESTS_EXIT_CODE -ne 0 ]; then
    echo "=========================================="
    echo "ERROR: Tests build failed with exit code $TESTS_EXIT_CODE"
    echo "=========================================="
    exit $TESTS_EXIT_CODE
fi

echo "=========================================="
echo "Copying System.Private.CoreLib.dll"
echo "=========================================="

CORE_ROOT=./artifacts/tests/coreclr/linux.ppc64le.Debug/Tests/Core_Root

if [ ! -d "$CORE_ROOT" ]; then
    echo "ERROR: Core_Root directory not found at $CORE_ROOT"
    exit 1
fi

cp \
    ${CORE_ROOT}/IL/System.Private.CoreLib.dll \
    ${CORE_ROOT}/System.Private.CoreLib.dll

RUNTIME_PATH=$(pwd)

echo "Runtime Path:"
echo "$RUNTIME_PATH"

echo "=========================================="
echo "Cloning JIT_Testing"
echo "=========================================="

cd "$WORKSPACE"

git clone https://github.com/alhad-deshpande/JIT_Testing.git
cd JIT_Testing
git checkout ppc64le_coreclr_jit_testing

DOTNET_PATH="$DOTNET_ROOT/dotnet"

echo "DOTNET_PATH=$DOTNET_PATH"
echo "RUNTIME_PATH=$RUNTIME_PATH"

if [ ! -f "$DOTNET_PATH" ]; then
    echo "ERROR: dotnet executable not found at $DOTNET_PATH"
    exit 1
fi

ls -l "$DOTNET_PATH"

echo "=========================================="
echo "Running JIT Tests"
echo "=========================================="

chmod +x run_test.sh

./run_test.sh "$DOTNET_PATH" "$RUNTIME_PATH"

TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo "=========================================="
    echo "ERROR: JIT tests failed with exit code $TEST_EXIT_CODE"
    echo "=========================================="
    exit $TEST_EXIT_CODE
fi

echo "=========================================="
echo "JIT TESTING COMPLETED SUCCESSFULLY"
echo "=========================================="


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

# ✅ FIX: Use available Arcade SDK version
sed -i 's/9.0.0-beta.25208.6/9.0.0-beta.23503.3/g' global.json || true
cat global.json

echo "=========================================="
echo "Reading SDK Version"
echo "=========================================="

SDK_VERSION=$(jq -r '.sdk.version' global.json)
echo "SDK_VERSION=$SDK_VERSION"

echo "=========================================="
echo "Installing .NET SDK"
echo "=========================================="

ARCH=$(uname -m)

DOTNET_DIR="/dotnet-sdk-${ARCH}"
mkdir -p "$DOTNET_DIR"
pushd "$DOTNET_DIR"

SDK_URL="https://github.com/IBM/dotnet-s390x/releases/download/v${SDK_VERSION}/dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz"

echo "Downloading SDK from: $SDK_URL"

wget "$SDK_URL"

mkdir -p .dotnet
tar -xvf dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz -C .dotnet

export DOTNET_ROOT=$(pwd)/.dotnet
export PATH=$DOTNET_ROOT:$PATH

popd

echo "=========================================="
echo ".NET Information"
echo "=========================================="

which dotnet
dotnet --info

echo "=========================================="
echo "Configuring NuGet"
echo "=========================================="

# ✅ Clean NuGet cache
rm -rf ~/.nuget/packages

# ✅ Define Restore Sources with correct order
RESTORE_SOURCES="https://api.nuget.org/v3/index.json;\
https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json;\
https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json;\
https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"

# ✅ Create global NuGet.Config to prevent unavailable source errors
mkdir -p ~/.nuget/NuGet

cat > ~/.nuget/NuGet/NuGet.Config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
    <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
    <add key="dotnet-eng" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json" />
  </packageSources>
</configuration>
EOF

export NUGET_CONFIG_FILE=$HOME/.nuget/NuGet/NuGet.Config

cat ~/.nuget/NuGet/NuGet.Config

echo "=========================================="
echo "Removing problematic NuGet.config files"
echo "=========================================="

# ✅ FIX: Remove all NuGet.config files in the repository that reference unavailable feeds
find . -name "NuGet.config" -o -name "NuGet.Config" | while read config_file; do
    echo "Removing: $config_file"
    rm -f "$config_file"
done

# ✅ Create a local NuGet.config in the runtime directory
cat > NuGet.Config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
    <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
    <add key="dotnet-eng" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json" />
  </packageSources>
</configuration>
EOF

echo "Created new NuGet.Config in runtime directory"
cat NuGet.Config

echo "=========================================="
echo "Building Runtime"
echo "=========================================="

# Set environment variable to disable NuGet warnings as errors
export NUGET_RESTORE_MSBUILD_ARGS="/p:TreatWarningsAsErrors=false"
export MSBuildTreatWarningsAsErrors=false
export MSBuildWarningsAsMessages="NU1603;NU1101"

./build.sh \
    clr+clr.hosts \
    /p:PrimaryRuntimeFlavor=CoreCLR \
    /p:PublishAot=false \
    /p:SupportsNativeAotComponents=false \
    /p:RestoreSources="$RESTORE_SOURCES" \
    /p:RestoreAdditionalProjectSources="" \
    /p:RestoreFallbackFolders="" \
    /p:TreatWarningsAsErrors=false \
    /p:WarningsAsErrors="" \
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
    /p:RestoreAdditionalProjectSources="" \
    /p:RestoreFallbackFolders="" \
    /p:TreatWarningsAsErrors=false \
    /p:WarningsAsErrors=""

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
    /p:RestoreAdditionalProjectSources="" \
    /p:RestoreFallbackFolders="" \
    /p:TreatWarningsAsErrors=false \
    /p:WarningsAsErrors=""

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

echo "Runtime Path: $RUNTIME_PATH"

echo "=========================================="
echo "Cloning JIT_Testing Repository"
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
echo "✅ JIT TESTING COMPLETED SUCCESSFULLY"
echo "=========================================="



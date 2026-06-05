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

# =========================================================
# ✅ FIX 1: Use valid Arcade version (publicly available)
# =========================================================
echo "=========================================="
echo "Fixing Arcade SDK version"
echo "=========================================="

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

echo "Downloading SDK from:"
echo "$SDK_URL"

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

# =========================================================
# ✅ FIX 2: Clean NuGet cache
# =========================================================
rm -rf ~/.nuget/packages

# =========================================================
# ✅ FIX 3: FORCE GLOBAL NuGet CONFIG (MOST IMPORTANT ✅)
# =========================================================
echo "=========================================="
echo "Setting global NuGet config (REMOVE broken feeds)"
echo "=========================================="

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
echo "Building Runtime"
echo "=========================================="

./build.sh \
    clr+clr.hosts \
    /p:PrimaryRuntimeFlavor=CoreCLR \
    /p:PublishAot=false \
    /p:SupportsNativeAotComponents=false \
    | tee build.log

echo "=========================================="
echo "Building Libraries"
echo "=========================================="

./build.sh libs

echo "=========================================="
echo "Building Tests"
echo "=========================================="

./src/tests/build.sh \
    /p:LibrariesConfiguration=Debug

echo "=========================================="
echo "Copying System.Private.CoreLib.dll"
echo "=========================================="

CORE_ROOT=./artifacts/tests/coreclr/linux.ppc64le.Debug/Tests/Core_Root

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

ls -l "$DOTNET_PATH"

echo "=========================================="
echo "Running JIT Tests"
echo "=========================================="

chmod +x run_test.sh

./run_test.sh "$DOTNET_PATH" "$RUNTIME_PATH"

echo "=========================================="
echo "JIT TESTING COMPLETED"
echo "=========================================="

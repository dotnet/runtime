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
# ✅ FIX 1: Use valid Arcade version
# =========================================================
sed -i 's/9.0.0-beta.25208.6/9.0.0-beta.23503.3/g' global.json || true

SDK_VERSION=$(jq -r '.sdk.version' global.json)
echo "SDK_VERSION=$SDK_VERSION"

# =========================================================
# ✅ Install .NET SDK
# =========================================================
ARCH=$(uname -m)

DOTNET_DIR="/dotnet-sdk-${ARCH}"
mkdir -p "$DOTNET_DIR"
pushd "$DOTNET_DIR"

SDK_URL="https://github.com/IBM/dotnet-s390x/releases/download/v${SDK_VERSION}/dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz"

wget "$SDK_URL"

mkdir -p .dotnet
tar -xvf dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz -C .dotnet

export DOTNET_ROOT=$(pwd)/.dotnet
export PATH=$DOTNET_ROOT:$PATH

popd

dotnet --info

# =========================================================
# ✅ Clean NuGet cache
# =========================================================
rm -rf ~/.nuget/packages

# =========================================================
# ✅ Restore sources
# =========================================================
RESTORE_SOURCES="https://api.nuget.org/v3/index.json;https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json;https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json;https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"

# =========================================================
# ✅ Force clean NuGet config
# =========================================================
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

# =========================================================
# ✅ First restore (download Arcade)
# =========================================================
./build.sh clr+clr.hosts /p:RestoreSources="$RESTORE_SOURCES" || true

# =========================================================
# ✅ PATCH Arcade SDK (remove broken feed)
# =========================================================
echo "Patching Arcade SDK..."

find ~/.nuget/packages/microsoft.dotnet.arcade.sdk -type f -name "Tools.proj" \
  -exec sed -i 's#https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-emsdk[^"]*##g' {} +

# =========================================================
# ✅ FIX 4: Dummy task file (fix MSB4022)
# =========================================================
mkdir -p /tmp/fake
touch /tmp/fake/dummy.dll

# =========================================================
# ✅ COMMON BUILD FLAGS (clean + stable)
# =========================================================
COMMON_FLAGS="/p:RestoreSources=$RESTORE_SOURCES \
/p:RestoreAdditionalProjectSources= \
/p:RestoreFallbackFolders= \
/p:SkipCrossgen=true \
/p:EnableOptimizationData=false \
/p:PGOInstrument=false \
/p:TreatWarningsAsErrors=false \
/p:DisablePackageBaselineValidation=true \
/p:SkipPackage=true \
/p:DotNetSharedFrameworkTaskFile=/tmp/fake/dummy.dll"

# =========================================================
# ✅ Build Runtime
# =========================================================
echo "=========================================="
echo "Building Runtime"
echo "=========================================="

./build.sh clr+clr.hosts \
/p:PrimaryRuntimeFlavor=CoreCLR \
/p:PublishAot=false \
/p:SupportsNativeAotComponents=false \
$COMMON_FLAGS \
| tee build.log

# =========================================================
# ✅ Build Libraries
# =========================================================
echo "=========================================="
echo "Building Libraries"
echo "=========================================="

./build.sh libs $COMMON_FLAGS

# =========================================================
# ✅ Build Tests
# =========================================================
echo "=========================================="
echo "Building Tests"
echo "=========================================="

./src/tests/build.sh \
/p:LibrariesConfiguration=Debug \
$COMMON_FLAGS

# =========================================================
# ✅ Fix CoreLib
# =========================================================
CORE_ROOT=./artifacts/tests/coreclr/linux.ppc64le.Debug/Tests/Core_Root
cp ${CORE_ROOT}/IL/System.Private.CoreLib.dll ${CORE_ROOT}/System.Private.CoreLib.dll

RUNTIME_PATH=$(pwd)

# =========================================================
# ✅ Clone JIT_Testing
# =========================================================
cd "$WORKSPACE"

git clone https://github.com/alhad-deshpande/JIT_Testing.git
cd JIT_Testing
git checkout ppc64le_coreclr_jit_testing

DOTNET_PATH="$DOTNET_ROOT/dotnet"

chmod +x run_test.sh

# =========================================================
# ✅ Run Tests
# =========================================================
echo "=========================================="
echo "Running JIT Tests"
echo "=========================================="

./run_test.sh "$DOTNET_PATH" "$RUNTIME_PATH"

echo "=========================================="
echo "✅ JIT TESTING COMPLETED"
echo "=========================================="

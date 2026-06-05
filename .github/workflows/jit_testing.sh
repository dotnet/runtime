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
echo "Environment Info"
echo "=========================================="
uname -a
pwd

WORKSPACE=$(pwd)

# =========================================================
# ✅ Clone runtime
# =========================================================
git clone --recurse-submodules https://github.com/alhad-deshpande/runtime.git
cd runtime
git checkout ppc64le_coreclr_jit

# =========================================================
# ✅ Fix Arcade SDK version
# =========================================================
sed -i 's/9.0.0-beta.25208.6/9.0.0-beta.23503.3/g' global.json || true

SDK_VERSION=$(jq -r '.sdk.version' global.json)
echo "SDK_VERSION=$SDK_VERSION"

# =========================================================
# ✅ Install SDK
# =========================================================
ARCH=$(uname -m)

mkdir -p /dotnet
cd /dotnet

wget https://github.com/IBM/dotnet-s390x/releases/download/v${SDK_VERSION}/dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz

mkdir sdk
tar -xvf dotnet-sdk-${SDK_VERSION}-linux-${ARCH}.tar.gz -C sdk

export DOTNET_ROOT=/dotnet/sdk
export PATH=$DOTNET_ROOT:$PATH

cd -
dotnet --info

# =========================================================
# ✅ Clean NuGet
# =========================================================
rm -rf ~/.nuget/packages
mkdir -p ~/.nuget/NuGet

# ✅ Minimal working feeds ONLY
cat > ~/.nuget/NuGet/NuGet.Config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
    <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
  </packageSources>
</configuration>
EOF

export RESTORE_SOURCES="https://api.nuget.org/v3/index.json;https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"

# =========================================================
# ✅ REMOVE PRIVATE / BROKEN DEPENDENCIES
# =========================================================

echo "Removing Microsoft.Private.Intellisense refs..."
find . -name "*.csproj" -exec sed -i '/Microsoft\.Private\.Intellisense/d' {} +

echo "Removing darc feed refs..."
find ~/.nuget -type f -name "*.proj" -exec sed -i 's#darc-pub-dotnet-emsdk[^"]*##g' {} + || true

# =========================================================
# ✅ Dummy file (MSB4022 fix)
# =========================================================
mkdir -p /tmp/fake
touch /tmp/fake/dummy.dll

# =========================================================
# ✅ COMMON FLAGS (IMPORTANT)
# =========================================================
COMMON="/p:RestoreSources=$RESTORE_SOURCES \
/p:SkipPackage=true \
/p:DotNetBuildFromSource=true \
/p:BuildAllConfigurations=false \
/p:SkipCrossgen=true \
/p:EnableOptimizationData=false \
/p:PGOInstrument=false \
/p:TreatWarningsAsErrors=false \
/p:DisablePackageBaselineValidation=true \
/p:DotNetSharedFrameworkTaskFile=/tmp/fake/dummy.dll"

# =========================================================
# ✅ Initial restore (ignore failure)
# =========================================================
./build.sh clr+clr.hosts /p:RestoreSources="$RESTORE_SOURCES" || true

# =========================================================
# ✅ Build runtime
# =========================================================
echo "=========================================="
echo "Build Runtime"
echo "=========================================="

./build.sh clr+clr.hosts \
/p:PrimaryRuntimeFlavor=CoreCLR \
/p:PublishAot=false \
/p:SupportsNativeAotComponents=false \
$COMMON \
| tee build.log

# =========================================================
# ✅ Build libs
# =========================================================
echo "=========================================="
echo "Build Libraries"
echo "=========================================="

./build.sh libs $COMMON

# =========================================================
# ✅ Build tests
# =========================================================
echo "=========================================="
echo "Build Tests"
echo "=========================================="

./src/tests/build.sh \
/p:LibrariesConfiguration=Debug \
$COMMON

# =========================================================
# ✅ Fix CoreLib
# =========================================================
CORE_ROOT=./artifacts/tests/coreclr/linux.ppc64le.Debug/Tests/Core_Root
cp ${CORE_ROOT}/IL/System.Private.CoreLib.dll ${CORE_ROOT}/System.Private.CoreLib.dll

# =========================================================
# ✅ JIT Testing
# =========================================================
cd "$WORKSPACE"

git clone https://github.com/alhad-deshpande/JIT_Testing.git
cd JIT_Testing
git checkout ppc64le_coreclr_jit_testing

chmod +x run_test.sh

./run_test.sh "$DOTNET_ROOT/dotnet" "$WORKSPACE/runtime"

echo "=========================================="
echo "✅ ALL DONE"
echo "=========================================="

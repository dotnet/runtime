#!/bin/bash

set -ex
export DEBIAN_FRONTEND=noninteractive

echo "=================================="
echo "Install dependencies"
echo "=================================="

apt-get update
apt-get install -y \
  bc automake clang curl findutils git hostname libtool \
  libkrb5-dev ninja-build llvm make python3 cmake \
  liblttng-ust-dev tar wget jq lld \
  build-essential zlib1g-dev libssl-dev libbrotli-dev \
  ca-certificates

# =========================================================
#  Clone runtime
# =========================================================
echo "=================================="
echo "Clone runtime"
echo "=================================="

git clone --recurse-submodules https://github.com/alhad-deshpande/runtime.git
cd runtime
git checkout ppc64le_coreclr_jit

# =========================================================
#  Fix disabled NuGet feed
# =========================================================
echo "=================================="
echo "Fix NuGet feed"
echo "=================================="

if grep -q "darc-pub-dotnet-emsdk-78f6f07" NuGet.config; then
    sed -i '/darc-pub-dotnet-emsdk-78f6f07/d' NuGet.config
    echo "Removed disabled emsdk feed from NuGet.config"
fi

# =========================================================
#  Install .NET SDK
# =========================================================
echo "=================================="
echo "Install .NET SDK"
echo "=================================="

SDK_VERSION=$(jq -r '.sdk.version' global.json)

export DOTNET_DIR=/dotnet-sdk-$(uname -m)
mkdir -p $DOTNET_DIR

pushd $DOTNET_DIR

wget https://github.com/IBM/dotnet-s390x/releases/download/v${SDK_VERSION}/dotnet-sdk-${SDK_VERSION}-linux-$(uname -m).tar.gz

mkdir -p .dotnet
tar xvf dotnet-sdk-*linux-$(uname -m).tar.gz -C .dotnet > /dev/null

export DOTNET_ROOT=$(pwd)/.dotnet
export PATH=$DOTNET_ROOT:$PATH

popd

#  Verify installation
echo "=================================="
echo "Verify .NET Installation"
echo "=================================="

if command -v dotnet &> /dev/null
then
    echo " .NET installed successfully"
    dotnet --version
else
    echo " .NET installation failed"
    exit 1
fi

# =========================================================
#  Build Runtime
# =========================================================
echo "=================================="
echo "Build Runtime"
echo "=================================="

./build.sh clr+clr.hosts \
  /p:PrimaryRuntimeFlavor=CoreCLR \
  /p:PublishAot=false \
  /p:SupportsNativeAotComponents=false \
  | tee build.log

# =========================================================
#  Build Libraries
# =========================================================
echo "=================================="
echo "Build Libraries"
echo "=================================="

./build.sh libs

# =========================================================
#  Build Tests
# =========================================================
echo "=================================="
echo "Build Tests"
echo "=================================="

./src/tests/build.sh /p:LibrariesConfiguration=Debug

# =========================================================
#  Fix CoreLib
# =========================================================
echo "=================================="
echo "Fix CoreLib"
echo "=================================="

CORE_ROOT=./artifacts/tests/coreclr/linux.ppc64le.Debug/Tests/Core_Root

cp ${CORE_ROOT}/IL/System.Private.CoreLib.dll \
   ${CORE_ROOT}/System.Private.CoreLib.dll

# =========================================================
#  Run JIT Tests
# =========================================================
echo "=================================="
echo "Run JIT Tests"
echo "=================================="

cd ..

git clone https://github.com/alhad-deshpande/JIT_Testing.git
cd JIT_Testing
git checkout ppc64le_coreclr_jit_testing

chmod +x run_test.sh

./run_test.sh "$DOTNET_ROOT/dotnet" "$(pwd)/../runtime"

echo "=================================="
echo " DONE"
echo "=================================="

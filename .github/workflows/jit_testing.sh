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
#  Remove disabled NuGet feeds
# =========================================================
echo "=================================="
echo "Check NuGet feeds"
echo "=================================="

cp NuGet.config NuGet.config.bkp

grep 'value="https://pkgs.dev.azure.com' NuGet.config | while read -r line
do
    FEED_URL=$(echo "$line" | sed -n 's/.*value="\([^"]*\)".*/\1/p')

    echo "Checking: $FEED_URL"

    HTTP_CODE=$(curl -L -s -o /dev/null -w "%{http_code}" "$FEED_URL" || echo "000")

    if [ "$HTTP_CODE" = "404" ]; then
        echo "Removing disabled feed: $FEED_URL"

        ESCAPED_URL=$(printf '%s\n' "$FEED_URL" | sed 's/[\/&]/\\&/g')

        sed -i "\|$ESCAPED_URL|d" NuGet.config
    fi
done

echo "NuGet feed validation completed"

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

echo "Current commit:"
git log --oneline -1

echo "Testcase count:"
find testcases -name "*.cs" | wc -l

echo "Files discovered:"
find testcases -name "*.cs" | sort

chmod +x run_test.sh

./run_test.sh "$DOTNET_ROOT" "$(pwd)/../runtime"

echo "=================================="
echo " DONE"
echo "=================================="

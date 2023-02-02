#!/bin/sh
set -e

# Usage: build_linux.cmd <Configuration>
#
#   Configuration is one of Debug or Release - Release is the default is not specified

if [ "$1" = "" ]; then
    configuration=Release
else
    configuration=$1
fi

echo "*****************************"
echo "Unity: Starting CoreCLR build"
echo "  Platform:      Linux"
echo "  Architecture:  x64"
echo "  Configuration: $configuration"
echo "*****************************"

echo
echo "************************"
echo "Unity: Downloading 7-zip"
echo "************************"
echo
mkdir -p artifacts
curl https://public-stevedore.unity3d.com/r/public/7za-linux-x64/e6c75fb7ffda_e6a295cdcae3f74d315361883cf53f75141be2e739c020035f414a449d4876af.zip --output artifacts/7za-linux-x64.zip
unzip artifacts/7za-linux-x64.zip -d artifacts/7za-linux-x64

echo
echo "******************************"
echo "Unity: Building embedding host"
echo "******************************"
echo
./dotnet.sh build unity/managed.sln -c $configuration

echo
echo "***********************"
echo "Unity: Building Null GC"
echo "***********************"
echo
cd unity/unitygc
mkdir -p $configuration
cd $configuration
cmake -DCMAKE_BUILD_TYPE=$configuration ..
cmake --build .
cd ../../../

echo
echo "*******************************"
echo "Unity: Building CoreCLR runtime"
echo "*******************************"
echo
./build.sh -subset clr+libs+libs -a x64 -c $configuration -ci -ninja

echo
echo "*******************************"
echo "Unity: Copying built artifacts"
echo "*******************************"
echo
cp unity/unitygc/$configuration/libunitygc.so artifacts/bin/microsoft.netcore.app.runtime.linux-x64/$configuration/runtimes/linux-x64/native
cp unity/unity-embed-host/bin/$configuration/net7.0/unity-embed-host.dll artifacts/bin/microsoft.netcore.app.runtime.linux-x64/$configuration/runtimes/linux-x64/lib/net7.0
cp unity/unity-embed-host/bin/$configuration/net7.0/unity-embed-host.dll artifacts/bin/microsoft.netcore.app.runtime.linux-x64/$configuration/runtimes/linux-x64/lib/net7.0
cp LICENSE.md artifacts/bin/microsoft.netcore.app.runtime.linux-x64/$configuration/runtimes/linux-x64/.
artifacts/7za-linux-x64/7za a artifacts/unity/$ARTIFACT_FILENAME ./artifacts/bin/microsoft.netcore.app.runtime.linux-x64/$configuration/runtimes/linux-x64/*

echo
echo "*********************************"
echo "Unity: Built CoreCLR successfully"
echo "*********************************"

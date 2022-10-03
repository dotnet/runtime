#!/bin/sh
set -e

echo "*****************************"
echo "Unity: Starting CoreCLR build"
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
./dotnet.sh build unity/managed.sln -c Release

echo
echo "***********************"
echo "Unity: Building Null GC"
echo "***********************"
echo
cd unity/unitygc
mkdir -p release
cd release
cmake -DCMAKE_BUILD_TYPE=Release ..
cmake --build .
cd ../../../

echo
echo "*******************************"
echo "Unity: Building CoreCLR runtime"
echo "*******************************"
echo
./build.sh -subset clr+libs+libs -a x64 -c release -ci -ninja

echo
echo "*******************************"
echo "Unity: Copying built artifacts"
echo "*******************************"
echo
cp unity/unitygc/release/libunitygc.so artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/native
cp artifacts/bin/unity-embed-host/Release/net6.0/unity-embed-host.dll artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/lib/net7.0
cp artifacts/bin/unity-embed-host/Release/net6.0/unity-embed-host.dll artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/lib/net7.0
cp LICENSE.md artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/.
artifacts/7za-linux-x64/7za a artifacts/unity/$ARTIFACT_FILENAME ./artifacts/bin/microsoft.netcore.app.runtime.linux-x64/Release/runtimes/linux-x64/*

echo
echo "*********************************"
echo "Unity: Built CoreCLR successfully"
echo "*********************************"

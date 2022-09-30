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
curl https://public-stevedore.unity3d.com/r/public/7za-mac-x64/e6c75fb7ffda_5bd76652986a0e3756d1cfd7e84ce056a9e1dbfc5f70f0514a001f724c0fbad2.zip --output artifacts/7za-mac-x64.zip
unzip artifacts/7za-mac-x64.zip -d artifacts/7za-mac-x64

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
cmake -DCMAKE_OSX_ARCHITECTURES=arm64 -DCMAKE_BUILD_TYPE=Release ..
cmake --build .
cd ../../../

echo
echo "*******************************"
echo "Unity: Building CoreCLR runtime"
echo "*******************************"
echo
LD_LIBRARY_PATH=/usr/local/opt/openssl/lib ./build.sh -subset clr+libs -a arm64 -c release -cross -ci -ninja /p:CrossBuild=true

echo
echo "*******************************"
echo "Unity: Copying built artifacts"
echo "*******************************"
echo
cp unity/unitygc/release/libunitygc.dylib artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/native
cp artifacts/bin/unity-embed-host/Release/net6.0/unity-embed-host.dll artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/lib/net7.0
cp artifacts/bin/unity-embed-host/Release/net6.0/unity-embed-host.pdb artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/lib/net7.0
cp LICENSE.md artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/.
artifacts/7za-mac-x64/7za a artifacts/unity/$ARTIFACT_FILENAME ./artifacts/bin/microsoft.netcore.app.runtime.osx-arm64/Release/runtimes/osx-arm64/*

echo
echo "*********************************"
echo "Unity: Built CoreCLR successfully"
echo "*********************************"

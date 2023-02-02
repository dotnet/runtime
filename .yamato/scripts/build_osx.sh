#!/bin/sh
set -e

# Usage: build_osx.cmd <Architecture> <Configuration>
#
#   Architecture is one of arm64 or x64 - the local machine architecture is the default if not specified
#   Configuration is one of Debug or Release - Release is the default is not specified
#
# To specify Configuration, Architecture must be specified as well.

if [ "$1" = "" ]; then
    # Since the architecture was not specified, check the architecture
    # of the host machine and use it.
    if [ `uname -p` = "i386" ]; then
        architecture=x64
    else
        architecture=arm64
    fi
else
    architecture=$1
fi

if [ "$2" = "" ]; then
    configuration=Release
else
    configuration=$2
fi

echo "*****************************"
echo "Unity: Starting CoreCLR build"
echo "  Platform:      macOS"
echo "  Architecture:  $architecture"
echo "  Configuration: $configuration"
echo "*****************************"

echo
echo "************************"
echo "Unity: Downloading 7-zip"
echo "************************"
echo
mkdir -p artifacts
curl https://public-stevedore.unity3d.com/r/public/7za-mac-x64/e6c75fb7ffda_5bd76652986a0e3756d1cfd7e84ce056a9e1dbfc5f70f0514a001f724c0fbad2.zip --output artifacts/7za-mac-x64.zip
unzip -o artifacts/7za-mac-x64.zip -d artifacts/7za-mac-x64

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
if [ "$architecture" = "arm64" ]; then
    extra_architecture_define=-DCMAKE_OSX_ARCHITECTURES=$architecture
else
    extra_architecture_define=
fi
cmake $extra_architecture_define -DCMAKE_BUILD_TYPE=$configuration ..
cmake --build .
cd ../../../

echo
echo "*******************************"
echo "Unity: Building CoreCLR runtime"
echo "*******************************"
echo
if [ "$architecture" = "arm64" -a `uname -p` = "i386" ]; then
    cross_build=true
else
    cross_build=false
fi
LD_LIBRARY_PATH=/usr/local/opt/openssl/lib ./build.sh -subset clr+libs -a $architecture -c $configuration -ci -ninja /p:CrossBuild=$cross_build

echo
echo "*******************************"
echo "Unity: Copying built artifacts"
echo "*******************************"
echo
cp unity/unitygc/$configuration/libunitygc.dylib artifacts/bin/microsoft.netcore.app.runtime.osx-$architecture/$configuration/runtimes/osx-$architecture/native
cp unity/unity-embed-host/bin/$configuration/net7.0/unity-embed-host.dll artifacts/bin/microsoft.netcore.app.runtime.osx-$architecture/$configuration/runtimes/osx-$architecture/lib/net7.0
cp unity/unity-embed-host/bin/$configuration/net7.0/unity-embed-host.pdb artifacts/bin/microsoft.netcore.app.runtime.osx-$architecture/$configuration/runtimes/osx-$architecture/lib/net7.0
cp LICENSE.md artifacts/bin/microsoft.netcore.app.runtime.osx-$architecture/$configuration/runtimes/osx-$architecture/.
artifacts/7za-mac-x64/7za a artifacts/unity/$ARTIFACT_FILENAME ./artifacts/bin/microsoft.netcore.app.runtime.osx-$architecture/$configuration/runtimes/osx-$architecture/*

echo
echo "*********************************"
echo "Unity: Built CoreCLR successfully"
echo "*********************************"

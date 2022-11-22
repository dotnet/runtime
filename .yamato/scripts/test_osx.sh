#!/bin/sh
set -e

# Usage: test_osx.cmd <Architecture> <Configuration>
#
#   Architecture is one of arm64 or x64 - x64 is the default if not specified
#   Configuration is one of Debug or Release - Release is the default is not specified
#
# To specify Configuration, Architecture must be specified as well.

if [ "$1" = "" ]; then
    architecture=x64
else
    architecture=$1
fi

if [ "$2" = "" ]; then
    configuration=Release
else
    configuration=$2
fi

echo "*****************************"
echo "Unity: Starting CoreCLR tests"
echo "*****************************"

echo
echo "******************************"
echo "Unity: Building embedding host"
echo "******************************"
echo
./dotnet.sh build unity/managed.sln -c $configuration

echo
echo "**********************************"
echo "Unity: Running embedding API tests"
echo "**********************************"
echo
cd unity/embed_api_tests
cmake -DCMAKE_BUILD_TYPE=$configuration .
cmake --build .
./mono_test_app
cd ../../

echo
echo "**********************************"
echo "Unity: Running class library tests"
echo "**********************************"
echo
LD_LIBRARY_PATH=/usr/local/opt/openssl/lib ./build.sh -subset libs.tests -test /p:RunSmokeTestsOnly=true -a $architecture -c $configuration -ci -ninja

echo
echo "****************************"
echo "Unity: Running runtime tests"
echo "****************************"
echo
./src/tests/build.sh $architecture $configuration ci -tree:baseservices -tree:interop -tree:reflection
./src/tests/run.sh $architecture $configuration

echo
echo "************************"
echo "Unity: Running PAL tests"
echo "************************"
echo
./build.sh clr.paltests
./artifacts/bin/coreclr/OSX.$architecture.Debug/paltests/runpaltests.sh $(pwd)/artifacts/bin/coreclr/OSX.$architecture.Debug/paltests

echo
echo "**********************************"
echo "Unity: Tested CoreCLR successfully"
echo "**********************************"

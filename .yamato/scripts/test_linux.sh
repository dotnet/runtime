#!/bin/sh
set -e

# Usage: test_linux.cmd <Configuration>
#
#   Configuration is one of Debug or Release - Release is the default is not specified

if [ "$1" = "" ]; then
    configuration=Release
else
    configuration=$1
fi

echo "*****************************"
echo "Unity: Starting CoreCLR tests"
echo "  Platform:      Linux"
echo "  Architecture:  x64"
echo "  Configuration: $configuration"
echo "*****************************"

echo
echo "******************************"
echo "Unity: Building embedding host"
echo "******************************"
echo
./dotnet.sh build unity/managed.sln -c $configuration

echo
echo "***********************************"
echo "Unity: Running embedding API tests"
echo "***********************************"
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
./build.sh -subset libs.tests -test /p:RunSmokeTestsOnly=true -a x64 -c $configuration -ci -ninja

echo
echo "****************************"
echo "Unity: Running runtime tests"
echo "****************************"
echo
./src/tests/build.sh x64 $configuration /p:LibrariesConfiguration=$configuration ci -tree:baseservices -tree:interop -tree:reflection
./src/tests/run.sh x64 $configuration

echo
echo "************************"
echo "Unity: Running PAL tests"
echo "************************"
echo
./build.sh clr.paltests
./artifacts/bin/coreclr/$(uname).x64.Debug/paltests/runpaltests.sh $(pwd)/artifacts/bin/coreclr/$(uname).x64.Debug/paltests

echo
echo "**********************************"
echo "Unity: Tested CoreCLR successfully"
echo "**********************************"

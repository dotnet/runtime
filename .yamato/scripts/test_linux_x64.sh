#!/bin/sh
set -e

echo "*****************************"
echo "Unity: Starting CoreCLR tests"
echo "*****************************"

echo
echo "***********************************"
echo "Unity: Skipping embedding API tests"
echo "***********************************"
echo
# build/run tests
#  - dotnet build unity/managed.sln -c Release
#  - |
#    cd unity/embed_api_tests
#    cmake -DCMAKE_BUILD_TYPE=Release .
#    cmake --build .
#    ./mono_test_app

echo
echo "**********************************"
echo "Unity: Running class library tests"
echo "**********************************"
echo
./build.sh -subset libs.tests -test /p:RunSmokeTestsOnly=true -a x64 -c release -ci -ninja

echo
echo "****************************"
echo "Unity: Running runtime tests"
echo "****************************"
echo
./src/tests/build.sh x64 release ci -tree:GC -tree:baseservices -tree:interop -tree:reflection
./src/tests/run.sh x64 release

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

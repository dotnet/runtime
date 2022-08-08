dotnet build unity/managed.sln -c Release
cd unity/embed_api_tests
cmake -DCMAKE_BUILD_TYPE=Release .
cmake --build .
./mono_test_app
cd ../../
# run a small set of library test to ensure basic behavior
LD_LIBRARY_PATH=/usr/local/opt/openssl/lib ./build.sh -subset libs.tests -test /p:RunSmokeTestsOnly=true -a x64 -c release -ci -ninja
# run five sub-trees of core runtime tests
./src/tests/build.sh x64 release ci -tree:GC -tree:JIT -tree:baseservices -tree:interop -tree:reflection
./src/tests/run.sh x64 release
./build.sh clr.paltests
./artifacts/bin/coreclr/OSX.x64.Debug/paltests/runpaltests.sh $(pwd)/artifacts/bin/coreclr/OSX.x64.Debug/paltests

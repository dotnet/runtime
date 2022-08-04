# build/run tests
dotnet build unity\managed.sln -c Release
#  - |
#    cd unity\embed_api_tests
#    cmake . -A Win32
#    cmake --build . --config Release
#    Release\mono_test_app.exe

# run a small set of library test to ensure basic behavior
build.cmd libs.tests -test /p:RunSmokeTestsOnly=true -a x86 -c release -ci
# run five sub-trees of core runtime tests
src\tests\build.cmd x86 release ci tree GC tree JIT tree baseservices tree interop tree reflection
src\tests\run.cmd x86 release

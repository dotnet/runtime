rem build/run tests
cmd /c dotnet build unity\managed.sln -c Release
cd unity\embed_api_tests
cmake .
cmake --build . --config Release
Release\mono_test_app.exe
cd ../../
rem run a small set of library test to ensure basic behavior
cmd /c build.cmd libs.tests -test /p:RunSmokeTestsOnly=true -a x64 -c release -ci
rem run five sub-trees of core runtime tests
cmd /c src\tests\build.cmd x64 release ci tree GC tree JIT tree baseservices tree interop tree reflection
cmd /c src\tests\run.cmd x64 release

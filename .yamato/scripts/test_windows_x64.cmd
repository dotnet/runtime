@echo off

echo *****************************
echo Unity: Starting CoreCLR tests
echo *****************************

echo.
echo ******************************
echo Unity: Building embedding host
echo ******************************
echo.
cmd /c dotnet build unity\managed.sln -c Release || goto :error

echo.
echo **********************************
echo Unity: Running embedding API tests
echo **********************************
echo.
cd unity\embed_api_tests || goto :error
cmake . || goto :error
cmake --build . --config Release || goto :error
Release\mono_test_app.exe || goto :error
cd ../../ || goto :error

echo.
echo **********************************
echo Unity: Running class library tests
echo **********************************
echo.
cmd /c build.cmd libs.tests -test /p:RunSmokeTestsOnly=true -a x64 -c release -ci || goto :error

echo.
echo ****************************
echo Unity: Running runtime tests
echo ****************************
echo.
cmd /c src\tests\build.cmd x64 release ci tree GC tree baseservices tree interop tree reflection || goto :error
cmd /c src\tests\run.cmd x64 release || goto :error

rem Every thing succeeded - jump to the end of the file and return a 0 exit code
echo.
echo **********************************
echo Unity: Tested CoreCLR successfully
echo **********************************
goto :EOF

rem If we get here, one of the commands above failed
:error
echo.
echo ***************************
echo Unity: CoreCLR tests failed
echo ***************************
exit /b %errorlevel%

@echo off

rem Usage: test_windows.cmd <Architecture> <Configuration>
rem
rem   Architecture is one of x86 or x64 - x64 is the default if not specified
rem   Configuration is one of Debug or Release - Release is the default is not specified
rem
rem To specify Configuration, Architecture must be specified as well.

if "%~1"=="" goto :default_architecture
set architecture=%1
goto :set_configuration
:default_architecture
set architecture=x64

:set_configuration
if "%~2"=="" goto :default_configuration
set configuration=%2
goto :run_test
:default_configuration
set configuration=Release

:run_test

echo *****************************
echo Unity: Starting CoreCLR tests
echo   Platform:      Windows
echo   Architecture:  %architecture%
echo   Configuration: %configuration%
echo *****************************

echo.
echo ******************************
echo Unity: Building embedding host
echo ******************************
echo.
call dotnet build unity\managed.sln -c %configuration% || goto :error

echo.
echo *****************************************************
echo Unity: Running managed embedding API tests
echo *****************************************************
echo.
call dotnet test unity/managed.sln -c %configuration% || goto :error

if "%architecture%"=="x86" goto :skip_embedding_tests_x86
echo.
echo *****************************************************
echo Unity: Running embedding API tests
echo *****************************************************
echo.
if "%architecture%"=="x86" (set cmake_architecture=Win32) else (set cmake_architecture=x64)
cd unity\embed_api_tests || goto :error
cmake . -A %cmake_architecture% || goto :error
cmake --build . --config %configuration% || goto :error
%configuration%\mono_test_app.exe || goto :error
cd ../../ || goto :error
goto :run_class_library_tests

:skip_embedding_tests_x86
echo.
echo *****************************************************
echo Unity: Skipping embedding API tests on x86
echo *****************************************************
echo.

:run_class_library_tests
echo.
echo **********************************
echo Unity: Running class library tests
echo **********************************
echo.
call build.cmd libs.tests -test /p:RunSmokeTestsOnly=true -a %architecture% -c %configuration% -ci || goto :error

echo.
echo ****************************
echo Unity: Running runtime tests
echo ****************************
echo.
call src\tests\build.cmd %architecture% %configuration% ci tree baseservices tree interop tree reflection -- /p:LibrariesConfiguration=%configuration% || goto :error
call src\tests\run.cmd %architecture% %configuration% || goto :error

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

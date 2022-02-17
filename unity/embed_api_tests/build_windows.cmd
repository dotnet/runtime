@echo off
rmdir /Q /S %~dp0\build
mkdir build
cd build
if %ERRORLEVEL% == 1 (
    echo "Unable to change directory to build"
    goto :exit_error
)
cmake -G "Visual Studio 14 2015 Win64" ..
if %ERRORLEVEL% == 1 (
    echo "Cmake failed"
    goto :exit_error
)
cmake --build .
exit /b %errorlevel%

goto :EOF
:exit_error
exit /b %errorlevel%
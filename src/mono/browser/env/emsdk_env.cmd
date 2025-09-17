@echo off

set CURRENT_SCRIPT=%~dp0
set EMSDK_PATH=%CURRENT_SCRIPT:~0,-1%\

set EMSDK_PYTHON=%EMSDK_PATH%python\python.exe
set DOTNET_EMSCRIPTEN_LLVM_ROOT=%EMSDK_PATH%bin\
set DOTNET_EMSCRIPTEN_NODE_JS=%EMSDK_PATH%node\22.16.0_64bit\bin\node.exe
set DOTNET_EMSCRIPTEN_BINARYEN_ROOT=%EMSDK_PATH%
@echo off
echo *** .NET EMSDK path setup ***
REM emscripten (emconfigure, em++, etc)
if "%EMSDK_PATH%"=="" (
echo %EMSDK_PATH% is empty
exit /b 1
)
set "TOADD_PATH_EMSCRIPTEN=%EMSDK_PATH%emscripten"
echo Prepending to PATH: %TOADD_PATH_EMSCRIPTEN%
set "PATH=%TOADD_PATH_EMSCRIPTEN%;%PATH%"
REM python
if "%EMSDK_PYTHON%"=="" (
echo %EMSDK_PYTHON% is empty
exit /b 1
)
for %%i in ("%EMSDK_PYTHON%") do set "TOADD_PATH_PYTHON=%%~dpi"
echo Prepending to PATH: %TOADD_PATH_PYTHON%
set "PATH=%TOADD_PATH_PYTHON%;%PATH%"
REM llvm (clang, etc)
if "%DOTNET_EMSCRIPTEN_LLVM_ROOT%"=="" (
echo %DOTNET_EMSCRIPTEN_LLVM_ROOT% is empty
exit /b 1
)
set "TOADD_PATH_LLVM=%DOTNET_EMSCRIPTEN_LLVM_ROOT%"
if not "%TOADD_PATH_EMSCRIPTEN%"=="%TOADD_PATH_LLVM%" (
echo Prepending to PATH: %TOADD_PATH_LLVM%
set "PATH=%TOADD_PATH_LLVM%;%PATH%"
)
REM nodejs (node)
if "%DOTNET_EMSCRIPTEN_NODE_JS%"=="" (
echo %DOTNET_EMSCRIPTEN_NODE_JS% is empty
exit /b 1
)
for %%i in ("%DOTNET_EMSCRIPTEN_NODE_JS%") do set "TOADD_PATH_NODEJS=%%~dpi"
if not "%TOADD_PATH_EMSCRIPTEN%"=="%TOADD_PATH_NODEJS%" (
if not "%TOADD_PATH_LLVM%"=="%TOADD_PATH_NODEJS%" (
echo Prepending to PATH: %TOADD_PATH_NODEJS%
set "PATH=%TOADD_PATH_NODEJS%;%PATH%"
)
)
REM binaryen (wasm-opt, etc)
if "%DOTNET_EMSCRIPTEN_BINARYEN_ROOT%"=="" (
echo %DOTNET_EMSCRIPTEN_BINARYEN_ROOT% is empty
exit /b 1
)
set "TOADD_PATH_BINARYEN=%DOTNET_EMSCRIPTEN_BINARYEN_ROOT%bin\"
if not "%TOADD_PATH_EMSCRIPTEN%"=="%TOADD_PATH_BINARYEN%" (
if not "%TOADD_PATH_LLVM%"=="%TOADD_PATH_BINARYEN%" (
if not "%TOADD_PATH_NODEJS%"=="%TOADD_PATH_BINARYEN%" (
echo Prepending to PATH: %TOADD_PATH_BINARYEN%
set "PATH=%TOADD_PATH_BINARYEN%;%PATH%"
)
)
)

@echo off

echo *****************************
echo Unity: Starting CoreCLR build
echo *****************************

echo.
echo ******************************
echo Unity: Building embedding host
echo ******************************
echo.
cmd /c dotnet build unity\managed.sln -c Release || goto :error

echo.
echo ***********************
echo Unity: Building Null GC
echo ***********************
echo.
cd unity\unitygc || goto :error
cmake . || goto :error
cmake --build . --config Release || goto :error
cd ../../ || goto :error

echo.
echo *******************************
echo Unity: Building CoreCLR runtime
echo *******************************
echo.
call build.cmd -subset clr+libs -a x64 -c release -ci || goto :error

echo.
echo ******************************
echo Unity: Copying built artifacts
echo ******************************
echo.
copy unity\unitygc\Release\unitygc.dll artifacts\bin\microsoft.netcore.app.runtime.win-x64\Release\runtimes\win-x64\native || goto :error
copy artifacts\bin\unity-embed-host\Release\net6.0\unity-embed-host.dll artifacts\bin\microsoft.netcore.app.runtime.win-x64\Release\runtimes\win-x64\lib\net7.0 || goto :error
copy artifacts\bin\unity-embed-host\Release\net6.0\unity-embed-host.pdb artifacts\bin\microsoft.netcore.app.runtime.win-x64\Release\runtimes\win-x64\lib\net7.0 || goto :error

rem Every thing succeeded - jump to the end of the file and return a 0 exit code
echo.
echo *********************************
echo Unity: Built CoreCLR successfully
echo *********************************
goto :EOF

rem If we get here, one of the commands above failed
:error
echo.
echo ******************************
echo Unity: Failed to build CoreCLR
echo ******************************
exit /b %errorlevel%

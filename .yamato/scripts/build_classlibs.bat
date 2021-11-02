@echo OFF

if not exist "incomingbuilds" mkdir "incomingbuilds"

for %%x IN ("windows", "OSX", "Linux") do (
    ECHO build.cmd libs -os %%x -c release
    build.cmd libs -os %%x -c release
    if NOT %errorlevel% == 0 (
        echo "build failed"
        EXIT /B %errorlevel%
    )
    if not exist "incomingbuilds/coreclrjit-%%x" mkdir "incomingbuilds/coreclrjit-%%x"
    ECHO xcopy /s /e /h /y "artifacts/bin/runtime/net7.0-%%x-Release-x64" "incomingbuilds/coreclrjit-%%x"
    xcopy /s /e /h /y "artifacts/bin/runtime/net7.0-%%x-Release-x64" "incomingbuilds/coreclrjit-%%x"
    ECHO taskkill /IM "dotnet.exe" /F
    taskkill /IM "dotnet.exe" /F
    echo build.cmd -clean
    build.cmd -clean
)
EXIT /B %ERRORLEVEL%

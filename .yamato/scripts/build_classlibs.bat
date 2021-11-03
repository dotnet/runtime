@echo OFF

if not exist "incomingbuilds" mkdir "incomingbuilds"

for %%x IN ("windows", "OSX", "Linux") do (
    CALL :EchoAndExecute build.cmd libs -os %%x -c release
    if NOT %errorlevel% == 0 (
        echo "build failed"
        EXIT /B %errorlevel%
    )
    if not exist "incomingbuilds/coreclrjit-%%x" mkdir "incomingbuilds/coreclrjit-%%x"

    CALL :EchoAndExecute xcopy /s /e /h /y "artifacts/bin/runtime/net7.0-%%x-Release-x64" "incomingbuilds/coreclrjit-%%x"
    CALL :EchoAndExecute taskkill /IM "dotnet.exe" /F
    CALL :EchoAndExecute build.cmd -clean
)
EXIT /B %ERRORLEVEL%

:EchoAndExecute
ECHO %*
CALL %*
GOTO :EOF

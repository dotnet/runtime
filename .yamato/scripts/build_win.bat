@echo off
build.cmd -subset clr -a x64 -c release

if NOT %errorlevel% == 0 (
 echo "build failed"
 EXIT /B %errorlevel%
)
echo "build ran successfully"

md incomingbuilds\win64
xcopy /s /e /h /y builds\windows.x64.Release incomingbuilds\win64

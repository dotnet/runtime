@echo off
setlocal


powershell -ExecutionPolicy ByPass -NoProfile -Command "& 'eng\build.ps1'" -subset clr -a x64 -c release

if NOT %errorlevel% == 0 (
 echo "build failed"
 EXIT /B %errorlevel%
)
echo "build ran successfully"

md incomingbuilds\win64
xcopy /s /e /h /y artifacts\bin\coreclr\windows.x64.Release incomingbuilds\win64

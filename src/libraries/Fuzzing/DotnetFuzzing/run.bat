@echo off
setlocal

set "searchRoot=%~dp0..\..\..\..\artifacts\bin\DotnetFuzzing"
for %%I in ("%searchRoot%") do set "searchRoot=%%~fI"

for /f "delims=" %%F in ('dir /s /b "%searchRoot%\DotnetFuzzing.exe" 2^>nul') do (
    set "exePath=%%~fF"
    goto :found
)

echo DotnetFuzzing.exe not found under "%searchRoot%"
exit /b 1

:found
"%exePath%" prepare-onefuzz deployment
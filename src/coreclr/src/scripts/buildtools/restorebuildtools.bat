REM =========================================================================================
REM ===
REM === Restore build tools required for native build
REM ===
REM =========================================================================================
echo Restore coreclr build tools nuget package
setlocal
:: Set the environment for the managed build
call "%__VSToolsRoot%\VsDevCmd.bat" 
%_msbuildexe% "%~dp0restorebuildtools.proj" /p:OutputPath="%__IntermediatesDir%" /nodeReuse:false
endlocal
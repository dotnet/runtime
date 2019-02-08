@echo off
setlocal

call "msbuild.exe" /t:RunWinConfigSetup mono.winconfig.targets

exit /b 0

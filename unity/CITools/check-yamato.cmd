@echo off

REM Using system install because the one in .dotnet gives a weird exception
%~dp0../../dotnet.cmd run --project %~dp0CheckYamatoFiles/CheckYamatoFiles.csproj %*
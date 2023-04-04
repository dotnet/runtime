@echo off

REM Using system install because the one in .dotnet gives a weird exception
dotnet run --project %~dp0CheckYamatoFiles/CheckYamatoFiles.csproj %*
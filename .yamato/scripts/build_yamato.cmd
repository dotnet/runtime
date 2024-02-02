@echo off

call %~dp0../../dotnet.cmd restore %~dp0../../unity/CITools/BuildDriver/BuildDriver.csproj --configfile %~dp0../../NuGet.config
call %~dp0../../dotnet.cmd run --project %~dp0../../unity/CITools/BuildDriver/BuildDriver.csproj --no-restore -- %*
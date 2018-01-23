@echo off

where.exe /Q dumpbin.exe || (echo [Error] dumpbin.exe is not on the environment & exit /b 1)

dumpbin.exe /headers %1 | findstr /C:"Certificates Directory

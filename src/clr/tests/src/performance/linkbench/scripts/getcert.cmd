@echo off
"%VS140COMNTOOLS%\..\..\VC\bin\amd64\dumpbin.exe" /headers %1 | findstr /C:"Certificates Directory

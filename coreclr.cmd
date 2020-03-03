@echo off
setlocal

set _args=-subsetCategory coreclr %*
if "%~1"=="-?" set _args=-help

"%~dp0build.cmd" %_args%

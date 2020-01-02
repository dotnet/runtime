@echo off
setlocal

set _args=-subsetCategory libraries %*
if "%~1"=="-?" set _args=-help

"%~dp0build.cmd" %_args%

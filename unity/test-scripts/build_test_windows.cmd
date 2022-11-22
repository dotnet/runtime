@echo off

rem Usage: build_test_windows.cmd <Architecture> <Configuration>
rem
rem   Architecture is one of x86 or x64 - x64 is the default if not specified
rem   Configuration is one of Debug or Release - Release is the default is not specified
rem
rem To specify Configuration, Architecture must be specified as well.

if "%~1"=="" goto :default_architecture
set architecture=%1
goto :set_configuration
:default_architecture
set architecture=x64

:set_configuration
if "%~2"=="" goto :default_configuration
set configuration=%2
goto :run_build_and_test
:default_configuration
set configuration=Release

:run_build_and_test
.yamato/scripts/build_windows.cmd %architecture% %configuration%
.yamato/scripts/test_windows.cmd %architecture% %configuration%

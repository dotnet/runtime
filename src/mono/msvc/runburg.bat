@echo off
rem This runs Monoburg on the various x86 files when called on Visual Studio
echo Running Monoburg on the inssel.brg files...
cd ..\mono\mini
set PATH=%PATH%;%MONO_DEPENDENCIES_PREFIX%\bin

if "%2" == "Win32" goto x86
if "%2" == "x64" goto x64
goto error
:x86
echo Platform detected is x86...
%1 -c 1 -p -e inssel.brg inssel-float.brg inssel-long32.brg inssel-x86.brg -d inssel.h -s inssel.c
goto end
:x64
echo Platform detected is x64...
%1 -c 1 -p -e inssel.brg inssel-float.brg inssel-long.brg inssel-amd64.brg -d inssel.h -s inssel.c
goto end
:error
echo Error: unsupported platform
exit /b 100
:end
echo done


@echo off
rem This runs Monoburg on the various x86 files when called on Visual Studio
echo Running Monoburg on the x86 inssel.brg files...
cd ..\mini
..\..\VSDependancies\temp\monoburg\Debug\monoburg -c -1 -p -e inssel.brg inssel-float.brg inssel-long32.brg inssel-x86.brg -d inssel.h -s inssel.c
echo done


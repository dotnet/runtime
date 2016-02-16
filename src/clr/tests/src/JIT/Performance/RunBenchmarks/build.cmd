@setlocal
@echo off

rem *** Alternative build process for running harness on Desktop CLR on Windows.
rem *** Otherwise, RunBenchmarks.exe is built when CoreCLR hosted tests are built.

if /I "%1" == "debug" goto build_debug 
if /I "%1" == "release" goto build_release 

echo "Usage build {debug | release}"
goto done

:build_debug

mkdir bin\Debug\desktop >NUL 2>&1
csc /define:DESKTOP /nologo /debug /target:exe /out:bin\Debug\desktop\RunBenchmarks.exe RunBenchmarks.cs 

goto done

:build_release

mkdir bin\Release\desktop >NUL 2>&1
csc /define:DESKTOP /nologo /debug:pdbonly /target:exe /out:bin\Release\desktop\RunBenchmarks.exe RunBenchmarks.cs 

goto done

:done
@endlocal

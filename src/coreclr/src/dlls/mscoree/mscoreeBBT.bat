@rem ==++==
@rem 
@rem   Copyright (c) Microsoft Corporation.  All rights reserved.
@rem 
@rem ==--==
setlocal
rem @echo off
REM Args are 1) full path to here 2) compile output dir 3) target platform 4) resulting binary
REM The script gets called for lib's too, so skip them.
if /i "%~x3" EQU ".lib" goto :EOF

REM Clean up from any previous runs.
del /s /f /q %1\bbt\%4

REM Set up the BBT environment.
if not exist %1\bbt\%4 md %1\bbt\%4
cd %1\bbt\%4
xcopy %3
xcopy %~dpn3.pdb
xcopy %1\bbt\*.*

REM Do the actual BBT run.
call :BBTize %~nx3

endlocal
goto :EOF



:BBTize
setlocal
@echo BBTizing %1

REM Build the instrumented executable.
call :bbtstart %1

REM Call the perf script.
@echo calling performance script
setlocal
call BBTScript
endlocal

REM Build the optimized executable.
call :bbtend %1

endlocal
goto :EOF



:bbtstart
@echo bbflow, bbinstr, bblink
bbflow /odb %~n1.bbcfg %~nx1
bbinstr /odb %~n1.ins.bbcfg /idfmax 4096 /idf %~n1.bbidf %~n1.bbcfg
bblink /o %~n1.ins.%~x1 %~n1.ins.bbcfg
if exist %~n1.sav del /f %~n1.sav
ren %~nx1 %~n1.sav
copy %~n1.ins.%~x1 %~nx1
if /i "%~x1" EQU ".dll" echo Registering DLL %1 & regsvr32 /s %1
goto :EOF



:bbtend
copy %~n1.sav %~nx1

@echo Building an Optimized Program.
bbmerge /idf %~n1.bbidf %~n1.bbcfg
if %ERRORLEVEL% NEQ 0 goto :EOF
bbopt /odb %~n1.opt.bbcfg %~n1.bbcfg
if %ERRORLEVEL% NEQ 0 goto :EOF
bblink /map %~n1.map /o %~n1.opt.%~x1 %~n1.opt.bbcfg
if %ERRORLEVEL% NEQ 0 goto :EOF

@echo Writing reports.
bbrpt /funcov %~n1.bbcfg > %~n1.fcv
if %ERRORLEVEL% NEQ 0 goto :EOF
bbrpt /deadsym %~n1.bbcfg > %~n1.ded
if %ERRORLEVEL% NEQ 0 goto :EOF
copy %~n1.opt.%~x1 %~nx1

splitsym %~nx1
goto :EOF


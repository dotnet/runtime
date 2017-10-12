@ECHO off

SET CONFIG_H=..\config.h
SET CYG_CONFIG_H=..\cygconfig.h
SET WIN_CONFIG_H=..\winconfig.h
SET CONFIGURE_AC=..\configure.ac
SET VERSION_H=..\mono\mini\version.h


ECHO Setting up Mono configuration headers...

REM Backup existing config.h into cygconfig.h if its not already replaced.
%windir%\system32\WindowsPowerShell\v1.0\powershell.exe -executionpolicy bypass -NonInteractive -File backup-config-files.ps1 %CONFIG_H% %CYG_CONFIG_H% 2>&1

%windir%\system32\WindowsPowerShell\v1.0\powershell.exe -executionpolicy bypass -NonInteractive -File compare-config-files.ps1 %WIN_CONFIG_H% %CONFIG_H% %CONFIGURE_AC% 2>&1

IF NOT %ERRORLEVEL% == 0 (
	ECHO copy %WIN_CONFIG_H% %CONFIG_H%
	copy %WIN_CONFIG_H% %CONFIG_H%
	%windir%\system32\WindowsPowerShell\v1.0\powershell.exe -NonInteractive -Command "(Get-Content %CONFIG_H%) -replace '#MONO_VERSION#', (Select-String -path %CONFIGURE_AC% -pattern 'AC_INIT\(mono, \[(.*)\]').Matches[0].Groups[1].Value | Set-Content %CONFIG_H%" 2>&1
	%windir%\system32\WindowsPowerShell\v1.0\powershell.exe -NonInteractive -Command "$mono_version=[int[]](Select-String -path %CONFIGURE_AC% -pattern 'AC_INIT\(mono, \[(.*)\]').Matches[0].Groups[1].Value.Split('.'); $corlib_counter=[int](Select-String -path %CONFIGURE_AC% -pattern 'MONO_CORLIB_COUNTER=(.*)').Matches[0].Groups[1].Value; (Get-Content %CONFIG_H%) -replace '#MONO_CORLIB_VERSION#',('1{0:00}{1:00}{2:00}{3:000}' -f $mono_version[0],$mono_version[1],0,$corlib_counter) | Set-Content %CONFIG_H%" 2>&1
)

SET VERSION_CONTENT="#define FULL_VERSION \"Visual Studio built mono\""
%windir%\system32\WindowsPowerShell\v1.0\powershell.exe -executionpolicy bypass -NonInteractive -File compare-config-content.ps1 %VERSION_CONTENT% %VERSION_H%  2>&1


IF NOT %ERRORLEVEL% == 0 (
	ECHO Configure %VERSION_H%
	ECHO #define FULL_VERSION "Visual Studio built mono"> %VERSION_H%
)

ECHO Successfully setup Mono configuration headers.

EXIT /b 0

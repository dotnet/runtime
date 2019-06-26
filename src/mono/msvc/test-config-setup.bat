@ECHO off

SET MONO_RESULT=1
SET CONFIG_PATH=%1
SET MONO_MODULE_PATH=%2
SET CPU_ARCH=%3

IF "" == "%CONFIG_PATH%" (
	ECHO Error: No configuration path set.
	GOTO ON_ERROR
)

IF "" == "%MONO_MODULE_PATH%" (
	ECHO Error: No mono module path set.
	GOTO ON_ERROR
)

IF "" == "%CPU_ARCH%" (
	ECHO Error: No cpu architecture set.
	GOTO ON_ERROR
)

IF NOT "x86" == "%CPU_ARCH%" (

	IF NOT "x86-64" == "%CPU_ARCH%" (
		ECHO Error: Unknown cpu architecture, %CPU_ARCH%.
		GOTO ON_ERROR
	)
)

SET CONFIG_PATH=%CONFIG_PATH:"=%
SET CONFIG_PATH=%CONFIG_PATH:/=\%

SET MONO_MODULE_PATH=%MONO_MODULE_PATH:"=%
SET MONO_MODULE_PATH=%MONO_MODULE_PATH:/=\%

REM Setup test configuration file.
>%CONFIG_PATH% ECHO ^<configuration^>
>>%CONFIG_PATH% ECHO ^<dllmap os="windows" cpu="%CPU_ARCH%" dll="libtest" target="%MONO_MODULE_PATH%\libtest.dll" /^>
>>%CONFIG_PATH% ECHO ^</configuration^>

SET MONO_RESULT=0
ECHO Successfully setup test configuration file, %CONFIG_PATH%.

GOTO ON_EXIT

:ON_ERROR
	ECHO Failed to setup test configuration file.
	ECHO test-config-setup.bat [CONFIG_FILE_PATH] [MONO_MODULE_PATH] [x86|x86-64]
	SET MONO_RESULT=1
	GOTO ON_EXIT
	
:ON_EXIT
	EXIT /b %MONO_RESULT%

@ECHO on

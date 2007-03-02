@echo off
if not exist VSDependancies\lib goto error
copy winconfig.h config.h
goto end
:error
echo fatal error: the VSDepenancies directory was not found in the "mono" directory
echo error: you must download and unzip that file
exit /b 100
goto end
:ok
echo OK
:end
exit /b 0

@echo off
csc -debug -out:scripts\monowrap.exe scripts\monowrap.cs 
if errorlevel 1 goto error
csc -nowarn:414 -debug -out:scripts\genproj.exe scripts\genproj.cs
if errorlevel 1 goto error
csc -debug -out:scripts\prepare.exe scripts\prepare.cs
if errorlevel 1 goto error
cd scripts
prepare.exe ..\\..\\..\\mcs core 
if errorlevel 1 goto error
genproj.exe
if errorlevel 1 goto error
cd ..
echo Setup complete, you need at least a mcs\class\lib\basic directory with
echo mcs.exe  mscorlib.dll  System.dll  System.Xml.dll
echo to bootstrap
goto end
: error
echo Error: solution is not configured.
:end
@echo off
csc -debug -out:scripts\monowrap.exe scripts\monowrap.cs 
csc -debug -out:scripts\genproj.exe scripts\genproj.cs
csc -debug -out:scripts\prepare.exe scripts\prepare.cs
cd scripts
genproj.exe
cd ..
echo Setup complete, you need at least a mcs\class\lib\basic directory with
echo mcs.exe  mscorlib.dll  System.dll  System.Xml.dll
echo to bootstrap


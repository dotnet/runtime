@echo off
csc -debug -out:scripts\monowrap.exe scripts\monowrap.cs 
csc -nowarn:414 -debug -out:scripts\genproj.exe scripts\genproj.cs
csc -debug -out:scripts\prepare.exe scripts\prepare.cs
cd scripts
prepare.exe
genproj.exe
cd ..
echo Setup complete, you need at least a mcs\class\lib\basic directory with
echo mcs.exe  mscorlib.dll  System.dll  System.Xml.dll
echo to bootstrap


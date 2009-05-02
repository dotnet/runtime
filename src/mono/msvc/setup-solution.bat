@echo off
csc -debug -out:scripts\monowrap.exe scripts\monowrap.cs 
csc -debug -out:scripts\genproj.exe scripts\genproj.cs
cd scripts
genproj.exe
cd ..
echo Setup complete, you can now use build the solution

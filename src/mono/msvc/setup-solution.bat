@echo off
mkdir scripts\net_1_1_bootstrap 2>1 nul
mkdir scripts\net_1_1 2>1 nul
mkdir scripts\net_2_0 2>1 nul
mkdir scripts\net_2_1 2>1 nul

csc -debug -out:scripts\monowrap.exe scripts\monowrap.cs 
copy scripts\monowrap.exe scripts\net_1_1_bootstrap\csc.exe
copy scripts\monowrap.pdb scripts\net_1_1_bootstrap\csc.pdb

copy scripts\monowrap.exe scripts\net_1_1\csc.exe
copy scripts\monowrap.pdb scripts\net_1_1\csc.pdb

copy scripts\monowrap.exe scripts\net_2_0\csc.exe
copy scripts\monowrap.pdb scripts\net_2_0\csc.pdb

copy scripts\monowrap.exe scripts\net_2_1\csc.exe
copy scripts\monowrap.pdb scripts\net_2_1\csc.pdb

echo Setup complete, you can now use build the solution
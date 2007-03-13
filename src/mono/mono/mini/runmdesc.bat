@echo off
rem This runs genmdesc on the x86 files when called on Visual Studio
echo Running genmdesc on the x86 files...
cd mono\mini
set PATH=%PATH%;..\..\VSDependancies\lib
%1 cpu-x86.md cpu-x86.h x86_desc
echo done


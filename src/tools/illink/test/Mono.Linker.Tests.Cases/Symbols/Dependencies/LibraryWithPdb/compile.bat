@ECHO OFF

REM Run this script from a VS command prompt

csc /debug:full /out:%~dp0LibraryWithPdb.dll /target:library %~dp0LibraryWithPdb.cs

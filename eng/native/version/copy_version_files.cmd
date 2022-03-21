@if not defined _echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

set __VersionFolder=%~dp0
set __RepoRoot=%~dp0..\..\..
set __artifactsObjDir=%__RepoRoot%\artifacts\obj

for /r "%__VersionFolder%" %%a in (*.h *.rc) do (
    if not exist "%__artifactsObjDir%\%%~nxa" (
        copy "%%a" "%__artifactsObjDir%"
    )
)

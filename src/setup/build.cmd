@echo off
for /f %%i in ('git rev-parse --show-toplevel') do set __RepoRootDirRaw=%%i
set __RepoRootDir=%__RepoRootDirRaw:/=\%

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%__RepoRootDir%\eng\common\Build.ps1""" -restore -build %*"

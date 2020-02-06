@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\common\Build.ps1""" -projects """%~dp0illink.sln""" -restore -build %*"

@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "Set-Location %~dp0; & """%~dp0eng\dotnet.ps1""" ""format illink.sln --exclude src/analyzer src/tuner external %*"""

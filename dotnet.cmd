@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0dotnet.ps1""" %*"
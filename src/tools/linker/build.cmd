@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\Build.ps1""" -restore -build %*"

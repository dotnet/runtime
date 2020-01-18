@echo off
powershell -NoProfile -NoLogo -ExecutionPolicy ByPass -command "& """%~dp0build.ps1""" %*"
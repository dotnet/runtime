@if "%_echo%"=="" echo off
REM Licensed to the .NET Foundation under one or more agreements.
REM The .NET Foundation licenses this file to you under the MIT license.
setlocal

csc ..\..\..\inc\genheaders.cs

genheaders.exe ..\..\..\inc\corerror.xml ..\inc\corerror.h mscorurt.rc

del genheaders.exe

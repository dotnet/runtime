@if "%_echo%"=="" echo off
REM Licensed to the .NET Foundation under one or more agreements.
REM The .NET Foundation licenses this file to you under the MIT license.
REM See the LICENSE file in the project root for more information.
setlocal

csc ..\..\..\inc\genheaders.cs

genheaders.exe ..\..\..\inc\corerror.xml ..\inc\corerror.h mscorurt.rc

del genheaders.exe

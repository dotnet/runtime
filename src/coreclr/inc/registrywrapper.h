// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: registrywrapper.h
//
// Wrapper around Win32 Registry Functions allowing redirection of .NET
// Framework root registry location
//
//*****************************************************************************
#ifndef __REGISTRYWRAPPER_H
#define __REGISTRYWRAPPER_H


#define ClrRegCreateKeyEx RegCreateKeyExW
#define ClrRegOpenKeyEx RegOpenKeyExW
#define IsNgenOffline() false


#endif // __REGISTRYWRAPPER_H

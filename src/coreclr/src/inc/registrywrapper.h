// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: registrywrapper.h
//
// Wrapper around Win32 Registry Functions allowing redirection of .Net 
// Framework root registry location
//
//*****************************************************************************
#ifndef __REGISTRYWRAPPER_H
#define __REGISTRYWRAPPER_H

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES) && !defined(FEATURE_CORECLR)

// Registry API wrappers

LONG ClrRegCreateKeyEx(
        HKEY hKey,
        LPCWSTR lpSubKey,
        DWORD Reserved,
        __in_opt LPWSTR lpClass,
        DWORD dwOptions,
        REGSAM samDesired,
        LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        PHKEY phkResult,
        LPDWORD lpdwDisposition
        );

LONG ClrRegOpenKeyEx(
        HKEY hKey,
        LPCWSTR lpSubKey,
        DWORD ulOptions,
        REGSAM samDesired,
        PHKEY phkResult
        );

bool IsNgenOffline();

#else

#define ClrRegCreateKeyEx RegCreateKeyExW
#define ClrRegOpenKeyEx RegOpenKeyExW
#define IsNgenOffline() false

#endif

#endif // __REGISTRYWRAPPER_H

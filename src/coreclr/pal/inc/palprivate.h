// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PAL_PRIVATE_H__
#define __PAL_PRIVATE_H__

#ifdef  __cplusplus
extern "C" {
#endif

PALIMPORT
HANDLE
PALAPI
OpenMutexA(
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCSTR lpName);

PALIMPORT
HMODULE
PALAPI
LoadLibraryExA(
        IN LPCSTR lpLibFileName,
        IN /*Reserved*/ HANDLE hFile,
        IN DWORD dwFlags);

PALIMPORT
DWORD
PALAPI
GetModuleFileNameA(
    IN HMODULE hModule,
    OUT LPSTR lpFileName,
    IN DWORD nSize);

PALIMPORT
BOOL
PALAPI
SetEnvironmentVariableA(
            IN LPCSTR lpName,
            IN LPCSTR lpValue);

PALIMPORT
DWORD
PALAPI
GetEnvironmentVariableA(
            IN LPCSTR lpName,
            OUT LPSTR lpBuffer,
            IN DWORD nSize);

#ifdef  __cplusplus
}
#endif

#endif// __PAL_PRIVATE_H__

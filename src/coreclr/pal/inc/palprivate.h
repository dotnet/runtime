// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PAL_PRIVATE_H__
#define __PAL_PRIVATE_H__

#ifdef  __cplusplus
extern "C" {
#endif

PALIMPORT
HMODULE
PALAPI
LoadLibraryExA(
        IN LPCSTR lpLibFileName,
        IN /*Reserved*/ HANDLE hFile,
        IN DWORD dwFlags);


#ifdef  __cplusplus
}
#endif

#endif// __PAL_PRIVATE_H__

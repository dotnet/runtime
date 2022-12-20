// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//

#ifndef _DLWRAP_H
#define _DLWRAP_H

//include this file if you get contract violation because of delayload

//nothrow implementations

#if defined(VER_H) && !defined (GetFileVersionInfoSizeW_NoThrow)
DWORD
GetFileVersionInfoSizeW_NoThrow(
        LPCWSTR lptstrFilename, /* Filename of version stamped file */
        LPDWORD lpdwHandle
        );
#endif

#if defined(VER_H) && !defined (GetFileVersionInfoW_NoThrow)
BOOL
GetFileVersionInfoW_NoThrow(
        LPCWSTR lptstrFilename, /* Filename of version stamped file */
        DWORD dwHandle,         /* Information from GetFileVersionSize */
        DWORD dwLen,            /* Length of buffer for info */
        LPVOID lpData
        );
#endif

#if defined(VER_H) && !defined (VerQueryValueW_NoThrow)
BOOL
VerQueryValueW_NoThrow(
        const LPVOID pBlock,
        LPCWSTR lpSubBlock,
        LPVOID * lplpBuffer,
        PUINT puLen
        );
#endif

//overrides
#undef VerQueryValueW
#undef GetFileVersionInfoW
#undef GetFileVersionInfoSizeW

#define VerQueryValueW                                  VerQueryValueW_NoThrow
#define GetFileVersionInfoW                            GetFileVersionInfoW_NoThrow
#define GetFileVersionInfoSizeW                     GetFileVersionInfoSizeW_NoThrow

#endif


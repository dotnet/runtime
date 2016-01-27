// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// GetProductVersionNumber.h
//
// Helper function to retrieve the file version number of a file.
// 
// ======================================================================================



#ifndef __GetProductVersionNumber_h__
#define __GetProductVersionNumber_h__

#include "contract.h"
#include "sstring.h"
#include "holder.h"
#include "ex.h"

//---------------------------------------------------------------------------------------
//
// Given the full path to an image, return the product version number.
//
// Arguments:
//    szFullPath - full path to the image
//    pdwVersionMS - out parameter; return the most significant 4 bytes of the version number according to
//                   the VS_FIXEDFILEINFO convention
//    pdwVersionLS - out parameter; return the least significant 4 bytes of the version number according to
//                   the VS_FIXEDFILEINFO convention
//
// Notes:
//    Throws on error

void inline GetProductVersionNumber(SString &szFullPath, DWORD * pdwVersionMS, DWORD * pdwVersionLS)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_PAL

    DWORD dwDummy = 0;
    DWORD dwFileInfoSize = 0;

    // Get the size of all of the file version information.
    dwFileInfoSize = GetFileVersionInfoSize(szFullPath, &dwDummy);
    if (dwFileInfoSize == 0)
    {
        ThrowLastError();
    }

    // Create the buffer to store the file information.
    NewHolder<BYTE> pbFileInfo(new BYTE[dwFileInfoSize]); 

    // Actually retrieve the file version information.
    if (!GetFileVersionInfo(szFullPath, NULL, dwFileInfoSize, pbFileInfo))
    {
        ThrowLastError();
    }

    // Now retrieve only the relevant version information, which will be returned in a VS_FIXEDFILEINFO.
    UINT uVersionInfoSize = 0;
    VS_FIXEDFILEINFO * pVersionInfo = NULL;

    if (!VerQueryValue(pbFileInfo, W("\\"), reinterpret_cast<LPVOID *>(&pVersionInfo), &uVersionInfoSize))
    {
        ThrowLastError();
    }
    _ASSERTE(uVersionInfoSize == sizeof(VS_FIXEDFILEINFO));

    *pdwVersionMS = pVersionInfo->dwProductVersionMS;
    *pdwVersionLS = pVersionInfo->dwProductVersionLS;
#else
    *pdwVersionMS = 0;
    *pdwVersionLS = 0;
#endif // FEATURE_PAL
}

#endif // __GetProductVersionNumber_h__

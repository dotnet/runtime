// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "stdafx.h"                     // Precompiled header key.
#include "utilcode.h"
#include "metadata.h"
#include "ex.h"
#include "pedecoder.h"

DWORD
GetFileVersionInfoSizeW_NoThrow(
        LPCWSTR lptstrFilename, /* Filename of version stamped file */
        LPDWORD lpdwHandle
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    DWORD dwRet=0;
    EX_TRY
    {
        dwRet=GetFileVersionInfoSize( (LPWSTR)lptstrFilename,  lpdwHandle );
    }
    EX_CATCH_HRESULT(hr);
    if (hr!=S_OK)
        SetLastError(hr);
    return dwRet;

}

BOOL
GetFileVersionInfoW_NoThrow(
        LPCWSTR lptstrFilename, /* Filename of version stamped file */
        DWORD dwHandle,         /* Information from GetFileVersionSize */
        DWORD dwLen,            /* Length of buffer for info */
        LPVOID lpData
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    BOOL bRet=FALSE;
    EX_TRY
    {
        bRet=GetFileVersionInfo( (LPWSTR)lptstrFilename, dwHandle,dwLen,lpData );
    }
    EX_CATCH_HRESULT(hr);
    if (hr!=S_OK)
        SetLastError(hr);
    return bRet;

}

BOOL
VerQueryValueW_NoThrow(
        const LPVOID pBlock,
        LPCWSTR lpSubBlock,
        LPVOID * lplpBuffer,
        PUINT puLen
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    BOOL bRet=FALSE;
    EX_TRY
    {
        bRet=VerQueryValueW( pBlock, (LPWSTR)lpSubBlock,lplpBuffer,puLen );
    }
    EX_CATCH_HRESULT(hr);
    if (hr!=S_OK)
        SetLastError(hr);
    return bRet;

}


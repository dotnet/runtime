// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "stdafx.h"                     // Precompiled header key.
#include "utilcode.h"
#include "metadata.h"
#include "ex.h"
#include "pedecoder.h"

#include <wininet.h>
#include <urlmon.h>
#include <version.h>

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

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
// The following functions are not used in CoreCLR. Normally LINKER can remove these functions
// from generated files.  But LINKER does it in two steps:
// 1. If no function in a source file is used, the file is ignored by LINKER
// 2. If one function is used, LINKER will first make sure all imported functions in the file
//    is available, and then it will remove unused functions.
// Instead of specifying all libs for imported functions needed by the following codes, we just
// remove them from compiling phase.
__success(return)
BOOL 
CreateUrlCacheEntryW_NoThrow(
        IN LPCWSTR lpszUrlName,
        IN DWORD dwExpectedFileSize,
        IN LPCWSTR lpszFileExtension,
        __out_ecount(MAX_LONGPATH+1) LPWSTR lpszFileName,
        IN DWORD dwReserved
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    BOOL bRet=FALSE;
    EX_TRY
    {
         bRet=CreateUrlCacheEntryW(lpszUrlName,dwExpectedFileSize,lpszFileExtension,
                                                                lpszFileName,dwReserved);
    }
    EX_CATCH_HRESULT(hr);
    if (hr!=S_OK)
        SetLastError(hr);
    return bRet;
    
}

BOOL  
CommitUrlCacheEntryW_NoThrow(
        IN LPCWSTR lpszUrlName,
        IN LPCWSTR lpszLocalFileName,
        IN FILETIME ExpireTime,
        IN FILETIME LastModifiedTime,
        IN DWORD CacheEntryType,
        IN LPCWSTR lpHeaderInfo,
        IN DWORD dwHeaderSize,
        IN LPCWSTR lpszFileExtension,
        IN LPCWSTR lpszOriginalUrl
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    BOOL bRet=FALSE;
    EX_TRY
    {
         bRet=CommitUrlCacheEntryW(lpszUrlName,lpszLocalFileName,ExpireTime,
                                                        LastModifiedTime,CacheEntryType,(LPWSTR)lpHeaderInfo,
                                                        dwHeaderSize,lpszFileExtension,lpszOriginalUrl);
    }
    EX_CATCH_HRESULT(hr);
    if (hr!=S_OK)
        SetLastError(hr);
    return bRet;
    
}

BOOL 
InternetTimeToSystemTimeA_NoThrow(
        IN  LPCSTR lpszTime,         // NULL terminated string
        OUT SYSTEMTIME *pst,         // output in GMT time
        IN  DWORD dwReserved
        ) 
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    BOOL bRet=FALSE;
    EX_TRY
    {
         bRet=InternetTimeToSystemTimeA(lpszTime,pst,dwReserved); 
    }
    EX_CATCH_HRESULT(hr);
    if (hr!=S_OK)
        SetLastError(hr);
    return bRet;
    
}

HRESULT 
CoInternetCreateSecurityManager_NoThrow(
        IServiceProvider *pSP,
        IInternetSecurityManager **ppSM, 
        DWORD dwReserved
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    EX_TRY
    {
         hr=CoInternetCreateSecurityManager(pSP,ppSM, dwReserved);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT 
URLDownloadToCacheFileW_NoThrow(
        LPUNKNOWN lpUnkcaller,
        LPCWSTR szURL,
        __out_ecount(dwBufLength) LPWSTR szFileName,
        DWORD dwBufLength,
        DWORD dwReserved,
        IBindStatusCallback *pBSC
        )
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    EX_TRY
    {
         hr=URLDownloadToCacheFileW(lpUnkcaller,szURL,szFileName,dwBufLength,dwReserved,pBSC);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT 
CoInternetGetSession_NoThrow(
        WORD dwSessionMode,
        IInternetSession **ppIInternetSession,
        DWORD dwReserved
        )
{
   WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    EX_TRY
    {
         hr=CoInternetGetSession(dwSessionMode,ppIInternetSession,dwReserved);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT 
CopyBindInfo_NoThrow( 
        const BINDINFO * pcbiSrc, BINDINFO * pbiDest
        )
{
   WRAPPER_NO_CONTRACT;
    HRESULT hr=S_OK;
    EX_TRY
    {
         hr=CopyBindInfo(pcbiSrc,pbiDest );
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}
#endif // FEATURE_CORECLR && !CROSSGEN_COMPILE

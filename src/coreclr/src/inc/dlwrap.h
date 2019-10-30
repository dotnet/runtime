// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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

#if defined(_WININET_) && !defined (CreateUrlCacheEntryW_NoThrow)
__success(return) 
BOOL 
CreateUrlCacheEntryW_NoThrow(
        IN LPCWSTR lpszUrlName,
        IN DWORD dwExpectedFileSize,
        IN LPCWSTR lpszFileExtension,
        __out_ecount(MAX_LONGPATH+1) LPWSTR lpszFileName,
        IN DWORD dwReserved
        );
#endif

#if defined(_WININET_) && !defined (CommitUrlCacheEntryW_NoThrow)
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
        );
#endif

#if defined(_WININET_) && !defined (InternetTimeToSystemTimeA_NoThrow)
BOOL 
InternetTimeToSystemTimeA_NoThrow(
        IN  LPCSTR lpszTime,         // NULL terminated string
        OUT SYSTEMTIME *pst,         // output in GMT time
        IN  DWORD dwReserved
        );
#endif

#if defined(__urlmon_h__) && !defined(CoInternetCreateSecurityManager_NoThrow)
HRESULT 
CoInternetCreateSecurityManager_NoThrow(
        IServiceProvider *pSP,
        IInternetSecurityManager **ppSM, 
        DWORD dwReserved
        );
#endif

#if defined(__urlmon_h__) && !defined(URLDownloadToCacheFileW_NoThrow)
HRESULT 
URLDownloadToCacheFileW_NoThrow(
        LPUNKNOWN lpUnkcaller,
        LPCWSTR szURL,
        __out_ecount(dwBufLength) LPWSTR szFileName,
        DWORD dwBufLength,
        DWORD dwReserved,
        IBindStatusCallback *pBSC
        );
#endif

#if defined(__urlmon_h__) && !defined(CoInternetGetSession_NoThrow)
HRESULT 
CoInternetGetSession_NoThrow(
        WORD dwSessionMode,
        IInternetSession **ppIInternetSession,
        DWORD dwReserved
        );
#endif

#if defined(__urlmon_h__) && !defined(CopyBindInfo_NoThrow)
HRESULT 
CopyBindInfo_NoThrow( 
        const BINDINFO * pcbiSrc, BINDINFO * pbiDest
        );
#endif



//overrides
#undef InternetTimeToSystemTimeA           
#undef CommitUrlCacheEntryW                    
#undef HttpQueryInfoA                                 
#undef InternetCloseHandle                         
#undef HttpSendRequestA                            
#undef HttpOpenRequestA                            
#undef InternetConnectA                              
#undef InternetOpenA                                  
#undef InternetReadFile                               
#undef CreateUrlCacheEntryW                     
#undef CoInternetGetSession                       
#undef CopyBindInfo                                     
#undef CoInternetCreateSecurityManager   
#undef URLDownloadToCacheFileW              
#undef FDICreate                                          
#undef FDIIsCabinet                                     
#undef FDICopy                                             
#undef FDIDestroy                                                            
#undef VerQueryValueW                               
#undef GetFileVersionInfoW                         
#undef GetFileVersionInfoSizeW                  
#undef VerQueryValueA                                
#undef GetFileVersionInfoA                          
#undef GetFileVersionInfoSizeA                   


#define InternetTimeToSystemTimeA               InternetTimeToSystemTimeA_NoThrow    
#define CommitUrlCacheEntryW                        CommitUrlCacheEntryW_NoThrow                                  
#define CreateUrlCacheEntryW                        CreateUrlCacheEntryW_NoThrow               
#define CoInternetGetSession                          CoInternetGetSession_NoThrow                
#define CopyBindInfo                                        CopyBindInfo_NoThrow                              
#define CoInternetCreateSecurityManager      CoInternetCreateSecurityManager_NoThrow  
#define URLDownloadToCacheFileW                 URLDownloadToCacheFileW_NoThrow                                         
#define VerQueryValueW                                  VerQueryValueW_NoThrow                  
#define GetFileVersionInfoW                            GetFileVersionInfoW_NoThrow                 
#define GetFileVersionInfoSizeW                     GetFileVersionInfoSizeW_NoThrow                 
#define VerQueryValueA                                  Use_VerQueryValueW
#define GetFileVersionInfoA                             Use_GetFileVersionInfoW
#define GetFileVersionInfoSizeA                     Use_GetFileVersionInfoSizeW

#if defined(_WININET_)
    inline
    HRESULT HrCreateUrlCacheEntryW(
            IN LPCWSTR lpszUrlName,
            IN DWORD dwExpectedFileSize,
            IN LPCWSTR lpszFileExtension,
            __out_ecount(MAX_LONGPATH+1) LPWSTR lpszFileName,
            IN DWORD dwReserved
            )
    {
        if (!CreateUrlCacheEntryW(lpszUrlName, dwExpectedFileSize, lpszFileExtension, lpszFileName, dwReserved))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            return S_OK;
        }
    }

    inline
    HRESULT HrCommitUrlCacheEntryW(
            IN LPCWSTR  lpszUrlName,
            IN LPCWSTR  lpszLocalFileName,
            IN FILETIME ExpireTime,
            IN FILETIME LastModifiedTime,
            IN DWORD    CacheEntryType,
            IN LPCWSTR  lpHeaderInfo,
            IN DWORD    dwHeaderSize,
            IN LPCWSTR  lpszFileExtension,
            IN LPCWSTR  lpszOriginalUrl
            )
    {
        if (!CommitUrlCacheEntryW(lpszUrlName, lpszLocalFileName, ExpireTime, LastModifiedTime, CacheEntryType,
                                  lpHeaderInfo, dwHeaderSize, lpszFileExtension, lpszOriginalUrl))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            return S_OK;
        }
    }
#endif // defined(_WININET_)

#endif


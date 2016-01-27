// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  File:    SortVersioning.h
// 


//  Purpose:  Provides access of the sort versioning functionality on 
//            downlevel (pre-Win7) machines.
//
////////////////////////////////////////////////////////////////////////////

namespace SortVersioning
{
    // Helpers for the sorting library
    typedef struct sorting_handle SORTHANDLE, *PSORTHANDLE;

    typedef PSORTHANDLE (*SORTGETHANDLE) (
        __in LPCWSTR lpLocaleName,
        __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
        __in_opt DWORD dwFlags    );

    typedef void (*SORTCLOSEHANDLE) (
        __in PSORTHANDLE pSortHandle );

    typedef int (*SORTGETSORTKEY) (
        __in PSORTHANDLE pSortHandle,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_bcount_opt(cbDest) LPBYTE pDest,
        __in int cbDest,
        __reserved LPVOID lpReserved,
        __reserved LPARAM lParam);

    typedef int (*SORTCHANGECASE) (
        __in PSORTHANDLE pSortHandle,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_ecount_opt(cchDest) LPWSTR pDest,
        __in int cchDest,
        __reserved LPVOID lpReserved,
        __reserved LPARAM lParam);

    typedef int (*SORTCOMPARESTRING) (
        __in PSORTHANDLE pSortHandle,
        __in DWORD dwCmpFlags,
        __in LPCWSTR lpString1,
        __in int cchCount1,
        __in LPCWSTR lpString2,
        __in int cchCount2,
        __reserved LPVOID lpReserved,
        __reserved LPARAM lParam);

    typedef int (*SORTFINDSTRING) (
        __in                    PSORTHANDLE pSortHandle,
        __in                    DWORD dwFindNLSStringFlags,
        __in_ecount(cchSource)  LPCWSTR lpStringSource,
        __in                    int cchSource,
        __in_ecount(cchValue)   LPCWSTR lpStringValue,
        __in                    int cchValue,
        __out_opt               LPINT pcchFound,
        __reserved              LPVOID lpReserved,
        __reserved              LPARAM lParam);

    typedef BOOL (*SORTISDEFINEDSTRING) (
        __in                PSORTHANDLE     pSortHandle,
        __in                NLS_FUNCTION    Function,
        __in                DWORD           dwFlags,
        __in_ecount(cchStr) LPCWSTR         lpString,
        __in                INT             cchStr);

    typedef int (*SORTGETHASHCODE) (
        __in PSORTHANDLE pSortHandle,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __reserved LPVOID lpReserved,
        __reserved LPARAM lParam);

#define SORT_NAME_SIZE 85

    // NOTE: This needs to stay in sync with the sorting dll's handle declaration
    typedef struct sorting_handle
    {
        DWORD                   dwSHVersion;                // Sort handle version
        struct sorting_handle   *pNext;                     // next for when it gets stuck in a hash table
        __nullterminated WCHAR  sortName[SORT_NAME_SIZE];   // Name of this sort
        DWORD                   dwDefinedVersion;           // Defined Version # for this node
        DWORD                   dwNLSVersion;               // NLS Version # for this node
        SORTGETSORTKEY          pSortGetSortKey;            // Pointer to GetSortKey function
        SORTCHANGECASE          pSortChangeCase;            // Pointer to ChangeCase function
        SORTCOMPARESTRING       pSortCompareString;         // Pointer to CompareString function
        SORTFINDSTRING          pSortFindString;            // Pointer to FindString function
        SORTISDEFINEDSTRING     pSortIsDefinedString;       // Pointer to IsDefinedString function
        SORTGETHASHCODE         pSortGetHashCode;           // Pointer to GetHashCode function (v2)
    } SORTHANDLE, *PSORTHANDLE;                  // Pointer to our sort handle       

    BOOL IsAvailableVersion(__in_opt CONST NLSVERSIONINFO * pVersion);
    
    DWORD SortNLSVersion();

    SORTGETHANDLE GetSortGetHandle(__in DWORD dwVersion);

    PSORTHANDLE GetSortHandle(__in LPCWSTR lpLocaleName, __in_opt CONST NLSVERSIONINFO * pVersion);

    int SortCompareString(__in LPCWSTR lpLocaleName,
                               __in DWORD dwCmpFlags,
                               __in_ecount(cchCount1) LPCWSTR lpString1,
                               __in int cchCount1,
                               __in_ecount(cchCount2) LPCWSTR lpString2,
                               __in int cchCount2,
                               __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
                               __reserved LPVOID lpReserved,
                               __reserved LPARAM lParam );
    __success(return != 0) int WINAPI SortDllCompareString(
        __in PSORTHANDLE pSort,
        __in DWORD dwCmpFlags,
        __in_ecount(cchCount1) LPCWSTR lpString1,
        __in int cchCount1,
        __in_ecount(cchCount2) LPCWSTR lpString2,
        __in int cchCount2,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam);

   __success(return != 0) int 
        LCMapStringEx (__in LPCWSTR lpLocaleName,
                           __in DWORD dwMapFlags,
                           __in_ecount(cchSrc) LPCWSTR lpSrcStr,
                           __in int cchSrc, 
                           __out_ecount_opt(cchDest)  LPWSTR lpDestStr, // really this should be __out_awcount_opt(dwMapFlags & LCMAP_SORTKEY, cchDest)
                           __in int cchDest,
                           __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
                           __reserved LPVOID lpReserved,
                           __reserved LPARAM lParam );    

   __success(return != 0) int SortDllChangeCase(
        __in PSORTHANDLE pSort,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_ecount_opt(cchDest) LPWSTR pDest,
        __in int cchDest,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam );

    __success(return != 0) int SortDllGetSortKey(
        __in PSORTHANDLE pSort,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_bcount_opt(cbDest) LPBYTE pDest,
        __in int cbDest,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam );

    int SortFindString(__in LPCWSTR lpLocaleName,
                        __in DWORD dwFindNLSStringFlags,
                        __in_ecount(cchSource) LPCWSTR lpStringSource,
                        __in int cchSource,
                        __in_ecount(cchValue) LPCWSTR lpStringValue,
                        __in int cchValue,
                        __out_opt LPINT pcchFound,
                        __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
                        __reserved LPVOID lpReserved,
                        __reserved LPARAM lParam);

    __success(return != 0) int SortDllFindString(
        __in PSORTHANDLE pSort,
        __in DWORD dwFindNLSStringFlags,
        __in_ecount(cchSource) LPCWSTR lpStringSource,
        __in int cchSource,
        __in_ecount(cchValue) LPCWSTR lpStringValue,
        __in int cchValue,
        __out_opt LPINT pcchFound,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam);

    BOOL SortIsDefinedString(__in NLS_FUNCTION Function,
                            __in DWORD dwFlags,
                            __in_opt CONST NLSVERSIONINFOEX * lpVersionInfo,
                            __in LPCWSTR lpString,
                            __in int cchStr );

    BOOL SortGetNLSVersion(__in PSORTHANDLE pSort,
                           __in NLS_FUNCTION Function,
                           __inout NLSVERSIONINFO * lpVersionInformation );

    BOOL WINAPI SortDllIsDefinedString(
        __in PSORTHANDLE      pSort,
        __in NLS_FUNCTION     Function,
        __in DWORD            dwFlags,
        __in_ecount(cchStr) LPCWSTR          lpString,
        __in INT              cchStr);

    __success(return != 0) int SortDllGetHashCode(
        __in PSORTHANDLE pSort,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam );

}

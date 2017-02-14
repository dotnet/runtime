// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  File:    SortVersioning.cpp
//


//  Purpose:  Provides access of the sort versioning functionality on
//            downlevel (pre-Win7) machines.
//
//       
//            This is not used on CoreCLR, where we always go to the OS
//            for sorting.
//            
////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "sortversioning.h"
#include "newapis.h"

#include "mscoree.h"
#include "clrconfig.h"

#define SORT_VERSION_V4         0x00060101
#define SORT_VERSION_WHIDBEY    0x00001000
#define SORT_VERSION_DEFAULT    SORT_VERSION_V4
#define SORT_DEFAULT_DLL_NAME   MAKEDLLNAME(W("nlssorting"))

namespace SortVersioning
{
#define SORT_HASH_TBL_SIZE  128

    //
    // Forward Declarations
    //
    PSORTHANDLE MakeSortHashNode(
        __in LPCWSTR    pSortName,
        __in DWORD      dwVersion);


    PSORTHANDLE InsertSortHashNode(
        __in PSORTHANDLE pHashN);

    static PSORTHANDLE     g_pSortHash[SORT_HASH_TBL_SIZE];    // Sort node hash table

    static HMODULE g_hSortDefault = (HMODULE)-1;

    __encoded_pointer static SORTGETHANDLE   g_pDefaultGetHandle;
    __encoded_pointer static SORTCLOSEHANDLE g_pDefaultCloseHandle;

    static HMODULE g_hSortCompatV2 = (HMODULE)-1;

    __encoded_pointer static SORTGETHANDLE   g_pV2GetHandle;
    __encoded_pointer static SORTCLOSEHANDLE g_pV2CloseHandle;

    static HMODULE g_hSortCompatV4 = (HMODULE)-1;

    __encoded_pointer static SORTGETHANDLE   g_pV4GetHandle;
    __encoded_pointer static SORTCLOSEHANDLE g_pV4CloseHandle;


    ////////////////////////////////////////////////////////////////////////////
    //
    //  NlsCompareInvariantNoCase
    //
    //  This routine does fast caseless comparison without needing the tables.
    //  This helps us do the comparisons we need to load the tables :-)
    //
    //  Returns 0 if identical, <0 if pFirst if first string sorts first.
    //
    //  This is only intended to help with our locale name comparisons,
    //  which are effectively limited to A-Z, 0-9, a-z and - where A-Z and a-z
    //  compare as equal.
    //
    //  WARNING: [\]^_` will be less than A-Z because we make everything lower
    //           case before comparing them.
    //
    //  When bNullEnd is TRUE, both of the strings should be null-terminator to be considered equal.
    //  When bNullEnd is FALSE, the strings are considered equal when we reach the number of characters specifed by size
    //  or when null terminators are reached, whichever happens first (strncmp-like behavior)
    //
    ////////////////////////////////////////////////////////////////////////////
    int NlsCompareInvariantNoCase(
        LPCWSTR pFirst,
        LPCWSTR pSecond,
        int     size,
        BOOL bNullEnd)
    {
        int i=0;
        WCHAR first;
        WCHAR second;

        for (;
             size > 0 && (first = *pFirst) != 0 && (second = *pSecond) != 0;
             size--, pFirst++, pSecond++)
        {
            // Make them lower case
            if ((first >= 'A') && (first <= 'Z')) first |= 0x20;
            if ((second >= 'A') && (second <= 'Z')) second |= 0x20;

            // Get the diff
            i = (first - second);

            // Are they the same?
            if (i == 0)
                continue;

            // Otherwise the difference.  Remember we made A-Z into lower case, so
            // the characters [\]^_` will sort < A-Z and also < a-z.  (Those are the
            // characters between A-Z and a-Z in ascii)
            return i;
        }

        // When we are here, one of these holds:
        //    size == 0
        //    or one of the strings has a null terminator
        //    or both of the string reaches null terminator

        if (bNullEnd || size != 0)
        {
            // If bNullEnd is TRUE, always check for null terminator.
            // If bNullEnd is FALSE, we still have to check if one of the strings is terminated eariler
            // than another (hense the size != 0 check).

            // See if one string ended first
            if (*pFirst != 0 || *pSecond != 0)
            {
                // Which one?
                return *pFirst == 0 ? -1 : 1;
            }
        }

        // Return our difference (0)
        return i;
    }


    SORTGETHANDLE GetSortGetHandle(__in DWORD dwVersion)
    {
        return NULL;
    }

    void DoSortCloseHandle(__in DWORD dwVersion, __in PSORTHANDLE pSort)
    {
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  GetSortHashValue
    //
    //  Returns the hash value for given sort name & version.
    //
    //  WARNING: This must be case insensitive.  Currently we're expecting only
    //           a-z, A-Z, 0-9 & -.
    //
    ////////////////////////////////////////////////////////////////////////////
    __inline __range(0, SORT_HASH_TBL_SIZE-1) int GetSortHashValue(
        __in LPCWSTR    pSortName,
        __in DWORD      dwVersion)
    {
        int iHash = 12; // Seed hash value
        int iMax;       // Number of characters to count (prevent problems with too-bad strings)

        // Hash the string
        if (pSortName)
        {
            for (iMax = 10; *pSortName != 0 && iMax != 0; pSortName++, iMax--)
            {
                iHash <<= 1;
                iHash ^= ((*pSortName) & 0xdf);     // 0x20 will make cases be the same (and other wierd stuff too, but we don't care about that)
            }
        }

        // Add the version hash
        // (the middle 2 bytes are most interesting)
        iHash ^= dwVersion >> 8;

        // Mix up our bits and hash it with 128
        _ASSERT(SORT_HASH_TBL_SIZE == 128);
        return (iHash + (iHash >> 8)) & 0x7f;
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  InsertSortHashNode
    //
    //  Inserts a sort hash node into the global sort hash tables.  It assumes
    //  that all unused hash values in the table are pointing to NULL.  If
    //  there is a collision, the new node will be added LAST in the list.
    //  (Presuming that the most often used are also the first used)
    //
    //  We do an interlocked exchange and free the pointer if we can't add it.
    //
    //  Warning: We stick stuff in this list, but we never remove it, so it
    //           get kind of big.  Removing entries would be difficult however
    //           because it would require some sort of synchronization with the
    //           reader functions (like GetLocaleInfo), or maybe an in-use flag
    //           or spin count.
    //
    ////////////////////////////////////////////////////////////////////////////
    PSORTHANDLE InsertSortHashNode(PSORTHANDLE pHashN)
    {
        __range(0, SORT_HASH_TBL_SIZE-1) UINT         index;
        PSORTHANDLE  pSearch;
        PSORTHANDLE* pNextToUpdate;

        //
        // Insert the hash node into the list (by name/version)
        //
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable: 26037) // Prefast warning - Possible precondition violation due to failure to null terminate string -  GetSortHashValue only uses first 10 characters and sortName is null terminated
#endif // _PREFAST_
        index = GetSortHashValue(pHashN->sortName, pHashN->dwNLSVersion);
#ifdef _PREFAST_
#pragma warning(pop)
#endif

        // Get hash node
        pSearch = g_pSortHash[index];

        // Remember last pointer in case we need to add it
        pNextToUpdate = &g_pSortHash[index];

        // We'll be the last node when added
        pHashN->pNext = NULL;

        while(TRUE)
        {
            while (pSearch != NULL)
            {
                // See if we already found a node.
                if ((pSearch->dwNLSVersion == pHashN->dwNLSVersion) &&
                    NlsCompareInvariantNoCase( pSearch->sortName, pHashN->sortName,
                                               LOCALE_NAME_MAX_LENGTH, TRUE) == 0)
                {
                    // Its the same, which is unexpected, return the old one
                    return pSearch;
                }

                pNextToUpdate = &pSearch->pNext;
                pSearch = pSearch->pNext;
            }

            // At end, try to add our node
            pSearch = InterlockedCompareExchangeT(pNextToUpdate, pHashN, NULL);

            // If pNextToUpdate isn't NULL then another process snuck in and updated the list
            // while we were getting ready.
            if (pSearch == NULL)
            {
                // It was added, stop
                break;
            }

            // It wasn't added, pSearch now points to a new node that snuck in, so
            // continue and try that one.  This should be really rare, even in a busy
            // loop, so we don't try a real lock.  Either
            // a) the snuck in node is the same as pHashN, and we'll return pSearch
            //    in the first loop, or
            // b) the snuck in node is new, in which case we'll try to readd.  Very worst
            //    case we'd collide while someone added ALL of the other locales with our
            //    hash, but eventually we'd hit case a.  (And there's only a couple hundred
            //    tries, so this can't lock for long.)
        }

        // Return the same one we added
        return pHashN;
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  FindSortHashNode
    //
    //  Searches for the sort hash node for the given sort name & version.
    //  The result is returned.  If none are found NULL is returned.
    //
    //  NOTE: Call GetSortNode() which calls this.
    //
    //  Defined as inline.
    //
    ////////////////////////////////////////////////////////////////////////////

    __inline PSORTHANDLE FindSortHashNode(
        __in LPCWSTR    pSortName,
        __in DWORD      dwVersion)
    {
        PSORTHANDLE pHashN;
        __range(0,SORT_HASH_TBL_SIZE-1) int         index;

        // Get Index
        index = GetSortHashValue(pSortName, dwVersion);

        // Get hash node
        pHashN = g_pSortHash[index];

        // Look through the list to see if one matches name and user info
        // We're sneaky here because we know our length of our hash name string is stored
        // just before that string.
        while ((pHashN != NULL) &&
               ((dwVersion != pHashN->dwNLSVersion) ||
                (NlsCompareInvariantNoCase(pSortName, pHashN->sortName, LOCALE_NAME_MAX_LENGTH, TRUE) != 0)))
        {
            pHashN = pHashN->pNext;
        }

        return pHashN;
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  MakeSortHashNode
    //
    //  Builds a sort hash node and sticks it in the hash table.
    //
    //  NOTE: Call GetSortNode() which calls this.
    //
    //  Defined as inline.
    //
    ////////////////////////////////////////////////////////////////////////////
    PSORTHANDLE MakeSortHashNode(
        __in LPCWSTR    pSortName,
        __in DWORD      dwVersion)
    {
        NLSVERSIONINFO  sortVersion;

        PSORTHANDLE     pSort = NULL;
        PSORTHANDLE     pSortInHash;

        // Valid locale, now we need to find out where to point this version at
        SORTGETHANDLE pGetHandle = GetSortGetHandle(dwVersion);
        if (pGetHandle == NULL) return NULL;

        sortVersion.dwNLSVersionInfoSize = sizeof(NLSVERSIONINFO);
        sortVersion.dwNLSVersion = dwVersion;
        sortVersion.dwDefinedVersion = dwVersion;

        pSort = pGetHandle(pSortName, &sortVersion, NULL);

        // If still missing, fail
        if (pSort == NULL)
        {
            // Invalid sort, fail
            return NULL;
        }

        // Now we need to add it
        pSortInHash = InsertSortHashNode(pSort);

        // If we got a different one back then free the one we added
        if (pSortInHash != pSort && pSortInHash)
        {
            // We got a different one from the hash (someone beat us to the cache)
            // so use that and discard the new one.
            DoSortCloseHandle(dwVersion, pSort);
        }

        return pSortInHash;
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  GetSortNode
    //
    //  Get a sort hash node for the specified sort name & version
    //
    ////////////////////////////////////////////////////////////////////////////
    PSORTHANDLE GetSortNode(
        __in LPCWSTR    pSortName,
        __in DWORD      dwVersion)
    {
        PSORTHANDLE pSortHashN = NULL;

        // WARNING: We don't bother doing the null/default/system checks

        // Didn't have an obvious one, look in the hash table
        pSortHashN = FindSortHashNode(pSortName, dwVersion);

        //
        //  If the hash node does not exist, we may need to get make one
        //
        if (pSortHashN == NULL)
        {
            //
            //  Hash node does NOT exist, try to make it

       //
            pSortHashN = MakeSortHashNode(pSortName, dwVersion);
        }

        //
        //  If the hash node still does not exist, we may need to fallback to default
        //  version
        //
        if (pSortHashN == NULL && dwVersion != SORT_VERSION_DEFAULT)
        {
            return GetSortNode(pSortName, SORT_VERSION_DEFAULT);
        }

        //
        //  Return pointer to hash node
        //  (null if we still don't have one)
        //
        return pSortHashN;
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  SortNLSVersion
    //  Check for the DWORD "CompatSortNLSVersion" CLR config option.
    //
    // .Net 4.0 introduces sorting changes that can affect the behavior of any of the methods
    // in CompareInfo. To mitigate against compatibility problems Applications can enable the
    // legacy CompareInfo behavior by using the 'SortNLSVersion' configuration option
    //
    // There are three ways to use the configuration option:
    //
    // 1) Config file (MyApp.exe.config)
    //        <?xml version ="1.0"?>
    //        <configuration>
    //         <runtime>
    //          <CompatSortNLSVersion enabled="4096"/><!--0x00001000 -->
    //         </runtime>
    //        </configuration>
    // 2) Environment variable
    //        set COMPlus_CompatSortNLSVersion=4096
    // 3) RegistryKey
    //        [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework]
    //        "CompatSortNLSVersion"=dword:00001000
    //
    ////////////////////////////////////////////////////////////////////////////
    DWORD SortNLSVersion()
    {
        return SORT_VERSION_DEFAULT;
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  VersionValue
    //
    //  Get the version from a version blob, resolving to the default version
    //  if NULL
    //
    ////////////////////////////////////////////////////////////////////////////
    __inline DWORD VersionValue(__in_opt const NLSVERSIONINFO * const lpVersionInformation)
    {

        //
        //  If the caller passed null or zero we use the default version
        //
        if ((lpVersionInformation == NULL) ||
            ((lpVersionInformation->dwNLSVersion == 0) &&
             (lpVersionInformation->dwDefinedVersion ==0))
             )
        {
            return SortNLSVersion();
        }

        // TODO: Will need to review this
        if(((lpVersionInformation->dwNLSVersion == 0) &&
            (lpVersionInformation->dwDefinedVersion != 0 )))
        {
            return lpVersionInformation->dwDefinedVersion;
        }

        return lpVersionInformation->dwNLSVersion;
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  SortGetSortKey
    //
    //  Supposed to call the dll for the appropriate version.  If the default
    //  version isn't available call the ordinal behavior (for minwin)
    //
    //  Just get the sort hash node and call the worker function
    //
    ////////////////////////////////////////////////////////////////////////////
    __success(return != 0) int WINAPI SortGetSortKey(
        __in LPCWSTR pLocaleName,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_bcount_opt(cbDest) LPBYTE pDest,
        __in int cbDest,
        __in_opt CONST NLSVERSIONINFO *lpVersionInformation,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam
    )
    {
        PSORTHANDLE pSort = GetSortNode(pLocaleName, VersionValue(lpVersionInformation));
        return SortDllGetSortKey(pSort, dwFlags, pSrc, cchSrc, pDest, cbDest, lpReserved, lParam);
    }

    // SortDllGetSortKey handles any modification to flags
    // necessary before the actual call to the dll
    __success(return != 0) int WINAPI SortDllGetSortKey(
        __in PSORTHANDLE pSort,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_bcount_opt(cbDest) LPBYTE pDest,
        __in int cbDest,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam )
    {
        if (pSort == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }

        //
        // Note that GetSortKey'll have the opposite behavior for the
        // linguistic casing flag (eg: use flag for bad behavior, linguistic
        // by default)
        dwFlags ^= NORM_LINGUISTIC_CASING;

        return pSort->pSortGetSortKey(pSort, dwFlags, pSrc, cchSrc, pDest, cbDest, lpReserved, lParam);
    }

    __success(return != 0) int SortDllGetHashCode(
        __in PSORTHANDLE pSort,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam )
    {
        if (pSort == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }
        const int SortDllGetHashCodeApiIntroducedVersion = 2;
        if(pSort->dwSHVersion < SortDllGetHashCodeApiIntroducedVersion)
        {
            SetLastError(ERROR_NOT_SUPPORTED);
            return 0;
        }

        //
        // Note that GetSortKey'll have the opposite behavior for the
        // linguistic casing flag (eg: use flag for bad behavior, linguistic
        // by default)
        dwFlags ^= NORM_LINGUISTIC_CASING;

        return pSort->pSortGetHashCode(pSort, dwFlags, pSrc, cchSrc, lpReserved, lParam);
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  SortChangeCase
    //
    //  Supposed to call the dll for the appropriate version.  If the default
    //  version isn't available call the ordinal behavior (for minwin)
    //
    //  NOTE: The linguistic casing flags are backwards (ie: set the flag to
    //        get the non-linguistic behavior.)  If we expose this then we'll
    //        need to publish the no-linguistic flag.
    //
    //  Just get the sort hash node and call the worker function
    //
    ////////////////////////////////////////////////////////////////////////////
    __success(return != 0) int WINAPI SortChangeCase(
        __in LPCWSTR pLocaleName,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_ecount_opt(cchDest) LPWSTR pDest,
        __in int cchDest,
        __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam)
    {
        PSORTHANDLE pSort = GetSortNode(pLocaleName, VersionValue(lpVersionInformation));
        return SortDllChangeCase(pSort, dwFlags, pSrc, cchSrc, pDest, cchDest, lpReserved, lParam);
    }

    // SortDllChangeCase handles any modification to flags
    // necessary before the actual call to the dll
    __success(return != 0) int WINAPI SortDllChangeCase(
        __in PSORTHANDLE pSort,
        __in DWORD dwFlags,
        __in_ecount(cchSrc) LPCWSTR pSrc,
        __in int cchSrc,
        __out_ecount_opt(cchDest) LPWSTR pDest,
        __in int cchDest,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam)
    {
        if (pSort == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }

        // Note that Change Case'll have the opposite behavior for the
        // linguistic casing flag (eg: use flag for bad behavior, linguistic
        // by default)
        dwFlags ^= LCMAP_LINGUISTIC_CASING;
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable: 26036) // prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return pSort->pSortChangeCase(pSort, dwFlags, pSrc, cchSrc, pDest, cchDest, lpReserved, lParam);
#ifdef _PREFAST_
#pragma warning(pop)
#endif
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  SortCompareString
    //
    //  Supposed to call the dll for the appropriate version.  If the default
    //  version isn't available call the ordinal behavior (for minwin)
    //
    //  Just get the sort hash node and call the worker function
    //
    ////////////////////////////////////////////////////////////////////////////
    __success(return != 0) int WINAPI SortCompareString(
        __in LPCWSTR lpLocaleName,
        __in DWORD dwCmpFlags,
        __in_ecount(cchCount1) LPCWSTR lpString1,
        __in int cchCount1,
        __in_ecount(cchCount2) LPCWSTR lpString2,
        __in int cchCount2,
        __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam)
    {
        PSORTHANDLE pSort = GetSortNode(lpLocaleName, VersionValue(lpVersionInformation));
        return SortDllCompareString(pSort, dwCmpFlags, lpString1, cchCount1, lpString2, cchCount2, lpReserved, lParam);

    }

    // SortDllCompareString handles any modification to flags
    // necessary before the actual call to the dll
    __success(return != 0) int WINAPI SortDllCompareString(
        __in PSORTHANDLE pSort,
        __in DWORD dwCmpFlags,
        __in_ecount(cchCount1) LPCWSTR lpString1,
        __in int cchCount1,
        __in_ecount(cchCount2) LPCWSTR lpString2,
        __in int cchCount2,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam)
    {
        if (pSort == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }

        // Note that the dll will have the opposite behavior of CompareStringEx for the
        // linguistic casing flag (eg: use flag for bad behavior, linguistic
        // by default) because we want new public APIs to have the "right"
        // behavior by default
        dwCmpFlags ^= NORM_LINGUISTIC_CASING;

        return pSort->pSortCompareString(pSort, dwCmpFlags, lpString1, cchCount1, lpString2, cchCount2, lpReserved, lParam);
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  SortFindString
    //
    //  Finds lpStringValue within lpStringSource based on the rules given
    //  in dwFindNLSStringFlags.
    //
    //  Supposed to call the dll for the appropriate version.  If the default
    //  version isn't available call the ordinal behavior (for minwin)
    //
    //  Just get the sort hash node and call the worker function
    //
    ////////////////////////////////////////////////////////////////////////////
    __success(return != 0) int WINAPI SortFindString(
        __in LPCWSTR lpLocaleName,
        __in DWORD dwFindNLSStringFlags,
        __in_ecount(cchSource) LPCWSTR lpStringSource,
        __in int cchSource,
        __in_ecount(cchValue) LPCWSTR lpStringValue,
        __in int cchValue,
        __out_opt LPINT pcchFound,
        __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam)
    {
        PSORTHANDLE pSort = GetSortNode(lpLocaleName, VersionValue(lpVersionInformation));
        return SortDllFindString(pSort, dwFindNLSStringFlags, lpStringSource, cchSource, lpStringValue, cchValue, pcchFound, lpReserved, lParam);
    }

    // SortDllFindString handles any modification to flags
    // necessary before the actual call to the dll
    __success(return != 0) int WINAPI SortDllFindString(
        __in PSORTHANDLE pSort,
        __in DWORD dwFindNLSStringFlags,
        __in_ecount(cchSource) LPCWSTR lpStringSource,
        __in int cchSource,
        __in_ecount(cchValue) LPCWSTR lpStringValue,
        __in int cchValue,
        __out_opt LPINT pcchFound,
        __in_opt LPVOID lpReserved,
        __in_opt LPARAM lParam)
    {
        if (pSort == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }

        // Note that the dll will  have the opposite behavior of FindNlsString for the
        // linguistic casing flag (eg: use flag for bad behavior, linguistic
        // by default) because we want new public APIs to have the "right"
        // behavior by default
        dwFindNLSStringFlags ^= NORM_LINGUISTIC_CASING;

        int cchFound; // we need to get the length even if the caller doesn't care about it (see below)
        int result = pSort->pSortFindString(pSort, dwFindNLSStringFlags, lpStringSource, cchSource, lpStringValue, cchValue, &cchFound, lpReserved, lParam);
        // When searching from end with an empty pattern (either empty string or all ignored characters)
        // a match is found (result != -1)
        //      Currently we get a result == 0 but we are hoping this will change
        //      with Win7 to be the length of the source string (thus pointing past-the-end)
        // and the length of the match (cchFound) will be 0
        // For compatibility, we need to return the index of the last character (or 0 if the source is empty)
        if((dwFindNLSStringFlags & FIND_FROMEND) &&
            result != -1 &&
            cchFound == 0 &&
            cchSource != 0)
        {
            result = cchSource - 1;
        }

        // if the caller cares about the length, give it to them
        if(pcchFound != NULL)
        {
            *pcchFound = cchFound;
        }

        return result;

    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  SortIsDefinedString
    //
    //  This routine looks for code points inside a string to see if they are
    //  defined within the NSL context. If lpVersionInformation is NULL, the
    //  version is the current version. Same thing the dwDefinedVersion is equal
    //  to zero.
    //
    //  Supposed to call the dll for the appropriate version.  If the default
    //  version isn't available call the ordinal behavior (for minwin)
    //
    //  Just get the sort hash node and call the worker function
    //
    ////////////////////////////////////////////////////////////////////////////
    BOOL WINAPI SortIsDefinedString(
        __in NLS_FUNCTION     Function,
        __in DWORD            dwFlags,
        __in CONST NLSVERSIONINFOEX * lpVersionInformation,
        __in_ecount(cchStr) LPCWSTR          lpString,
        __in INT              cchStr)
    {
        // Get an invariant sort node
        PSORTHANDLE pSort = GetSortNode(W(""), VersionValue((CONST NLSVERSIONINFO *)lpVersionInformation));
        return SortDllIsDefinedString(pSort, Function, dwFlags, lpString, cchStr);
    }

    // SortDllIsDefinedString handles any modification to flags
    // necessary before the actual call to the dll
    BOOL WINAPI SortDllIsDefinedString(
        __in PSORTHANDLE      pSort,
        __in NLS_FUNCTION     Function,
        __in DWORD            dwFlags,
        __in_ecount(cchStr) LPCWSTR          lpString,
        __in INT              cchStr)
    {
        // Fail if we couldn't find one
        if (pSort == NULL)
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }

        return pSort->pSortIsDefinedString(pSort, Function, dwFlags, lpString, cchStr);
    }

    BOOL SortGetNLSVersion(__in PSORTHANDLE pSort,
                           __in NLS_FUNCTION Function,
                           __inout NLSVERSIONINFO * lpVersionInformation )
    {
        lpVersionInformation->dwNLSVersion = pSort->dwNLSVersion;
        lpVersionInformation->dwDefinedVersion = pSort->dwDefinedVersion;

        return TRUE;
    }

    // Wrapper for SortGetSortKey and SortChangeCase, which are both
    // smushed into LCMapStringEx
    __success(return != 0) int
        LCMapStringEx (__in LPCWSTR lpLocaleName,
                           __in DWORD dwMapFlags,
                           __in_ecount(cchSrc) LPCWSTR lpSrcStr,
                           __in int cchSrc,
                           __out_ecount_opt(cchDest) LPWSTR lpDestStr, // really this should be __out_awcount_opt(dwMapFlags & LCMAP_SORTKEY, cchDest)
                           __in int cchDest,
                           __in_opt CONST NLSVERSIONINFO * lpVersionInformation,
                           __in_opt LPVOID lpReserved,
                           __in_opt LPARAM lParam )
    {
        // Should be either sort key...
        if (dwMapFlags & LCMAP_SORTKEY)
        {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible precondition violation due to failure to null terminate string lpDestStr-interpreted differently depending on flag
#endif // _PREFAST_
            return SortGetSortKey(lpLocaleName,
                                  dwMapFlags & ~(LCMAP_SORTKEY),    // Don't need sort key flag
                                  lpSrcStr,
                                  cchSrc,
                                  (LPBYTE)lpDestStr,        // Sort keys are bytes not WCHARs
                                  cchDest,                  // Sort keys are bytes not WCHARs
                                  lpVersionInformation,
                                  lpReserved,
                                  lParam);
#ifdef _PREFAST_
#pragma warning(pop)
#endif
        }

        //
        // Check for changing case conditions.  This may be combined with Chinese or Japanese
        // transliteration, but not with sort key nor ignore space/symbols
        //
        _ASSERT(dwMapFlags & (LCMAP_TITLECASE | LCMAP_UPPERCASE | LCMAP_LOWERCASE));

        //
        // Call casing wrapper, which'll either call the correct version dll
        // or call ordinal behavior in the minwin case
        //
        return SortChangeCase(lpLocaleName,
                              dwMapFlags & ~(LCMAP_BYTEREV),
                              lpSrcStr,
                              cchSrc,
                              lpDestStr,
                              cchDest,
                              lpVersionInformation,
                              lpReserved,
                              lParam);
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  IsAvailableVersion()
    //
    //  Get the SortGetHandle() function for the proper dll version.
    //
    ////////////////////////////////////////////////////////////////////////////
    BOOL IsAvailableVersion(__in_opt CONST NLSVERSIONINFO * pVersion)
    {
        return GetSortGetHandle(VersionValue(pVersion)) != NULL;
    }


    ////////////////////////////////////////////////////////////////////////////
    //
    //  GetSortHandle()
    //
    //  Get the SortHandle for the given locale and version
    //
    ////////////////////////////////////////////////////////////////////////////
    PSORTHANDLE GetSortHandle(__in LPCWSTR lpLocaleName, __in_opt CONST NLSVERSIONINFO * pVersion)
    {
        DWORD version = VersionValue(pVersion);
        if (GetSortGetHandle(version) == NULL)
        {
            return NULL;
        }
        return GetSortNode(lpLocaleName, version);
    }

}

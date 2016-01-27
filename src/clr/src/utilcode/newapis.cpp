// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  File:    newapis.cpp
// 


//  Purpose:  functions that need to be emulated on downlevel platforms.
//
////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "newapis.h"
#ifdef ENABLE_DOWNLEVEL_FOR_NLS
#include "downlevel.h"
#endif

#include "utilcode.h"
#include "sortversioning.h"

namespace NewApis
{


#if defined(ENABLE_DOWNLEVEL_FOR_NLS)

    FARPROC GetProcAddressForLocaleApi(__in LPCSTR lpProcName, __in_opt FARPROC pFnDownlevelFallback)
    {
        _ASSERTE(lpProcName != NULL);

        FARPROC result = NULL;

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)

        // First try to use the function defined in the culture dll 
        // if we are running on a platform prior to Win7
        if(!IsWindows7Platform())
        {
            // only need to load the culture dll's handle once then we can hold onto it
            static HMODULE hCulture = NULL;
            // if we haven't loaded the culture dll yet
            if (hCulture == NULL)
            {
                UtilCode::LoadLibraryShim(MAKEDLLNAME_W(W("culture")), NULL, 0, &hCulture);
            }
            
            // make sure we were successful before using the handle
            if (hCulture != NULL)
            {
                result=GetProcAddress(hCulture,lpProcName);
            }
        }
#endif // !FEATURE_CORECLR && !CROSSGEN_COMPILE

        // next try the kernel
        if(result==NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod!=NULL)
            {
                result=GetProcAddress(hMod,lpProcName);
            }
        }

        // failing all that, use the fallback provided
        if(result==NULL)
        {
            result = pFnDownlevelFallback;
        }

        return result;
    }

#endif // ENABLE_DOWNLEVEL_FOR_NLS
    
    __success(return > 0) int
    GetSystemDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName)
    {
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::GetSystemDefaultLocaleName(lpLocaleName, cchLocaleName);
#else
        typedef int (WINAPI *PFNGetSystemDefaultLocaleName)(LPWSTR, int);
        static PFNGetSystemDefaultLocaleName pFNGetSystemDefaultLocaleName=NULL;
        if (pFNGetSystemDefaultLocaleName == NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            // TODO: NLS Arrowhead - We should always fallback to the Downlevel APIs if the kernel APIs aren't found,
            // regardless of error reason
            if(hMod==NULL)
                return 0;
            pFNGetSystemDefaultLocaleName=(PFNGetSystemDefaultLocaleName)GetProcAddress(hMod,"GetSystemDefaultLocaleName");
            if(pFNGetSystemDefaultLocaleName==NULL)
            {
                if(GetLastError() == ERROR_PROC_NOT_FOUND)
                    pFNGetSystemDefaultLocaleName=DownLevel::GetSystemDefaultLocaleName;
                else
                    return 0;
            }
        }

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return pFNGetSystemDefaultLocaleName(lpLocaleName,cchLocaleName);
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

#endif
    };

    __success(return == TRUE) BOOL
    GetUserPreferredUILanguages (__in DWORD dwFlags, __out PULONG pulNumLanguages, __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer, __in PULONG pcchLanguagesBuffer)
    {
#ifdef ENABLE_DOWNLEVEL_FOR_NLS
        typedef DWORD (WINAPI *PFNGetUserPreferredUILanguages)(ULONG, PULONG, LPWSTR, PULONG);
        static PFNGetUserPreferredUILanguages pFNGetUserPreferredUILanguages=NULL;
        if (pFNGetUserPreferredUILanguages == NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod==NULL)
                return FALSE;
            pFNGetUserPreferredUILanguages=(PFNGetUserPreferredUILanguages)GetProcAddress(hMod,"GetUserPreferredUILanguages");
            if(pFNGetUserPreferredUILanguages==NULL)
            {
                if(GetLastError() == ERROR_PROC_NOT_FOUND)
                    pFNGetUserPreferredUILanguages=DownLevel::GetUserPreferredUILanguages;
                else
                    return FALSE;
            }
        }

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        BOOL res = pFNGetUserPreferredUILanguages(dwFlags, pulNumLanguages, pwszLanguagesBuffer, pcchLanguagesBuffer);
        if(res == TRUE)
            return res;

        //fallback to thread preferred langs 
        return GetThreadPreferredUILanguages(dwFlags, pulNumLanguages, pwszLanguagesBuffer, pcchLanguagesBuffer);
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

#else
        return ::GetUserPreferredUILanguages(dwFlags, pulNumLanguages, pwszLanguagesBuffer, pcchLanguagesBuffer);
#endif
    };



    __success(return != 0) int
    GetUserDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName)
    {
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::GetUserDefaultLocaleName(lpLocaleName, cchLocaleName);
#else
        typedef int (WINAPI *PFNGetUserDefaultLocaleName)(LPWSTR, int);
        static PFNGetUserDefaultLocaleName pFNGetUserDefaultLocaleName=NULL;
        if (pFNGetUserDefaultLocaleName == NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod==NULL)
                return 0;
            pFNGetUserDefaultLocaleName=(PFNGetUserDefaultLocaleName)GetProcAddress(hMod,"GetUserDefaultLocaleName");
            if(pFNGetUserDefaultLocaleName==NULL)
            {
                if(GetLastError() == ERROR_PROC_NOT_FOUND)
                    pFNGetUserDefaultLocaleName=DownLevel::GetUserDefaultLocaleName;
                else
                    return 0;
            }
        }

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return  pFNGetUserDefaultLocaleName(lpLocaleName,cchLocaleName);
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

#endif
    }

    // Call GetLocaleInfoEx and see if the OS knows about it.
    // Note that GetLocaleInfoEx has variations:
    // * Pre-Vista it fails and has to go downlevel
    // * Vista succeeds, but not for neutrals
    // * Win7 succeeds for all locales.  
    // * Mac does ???
    //
    // The caller is expected to call with a specific locale (non-neutral) on Windows < windows 7,
    // except for LOCALE_INEUTRAL and LOCALE_SPARENT, which downlevel.cpp handles
    //
    __success(return != 0) int
    GetLocaleInfoEx (__in LPCWSTR lpLocaleName, __in LCTYPE LCType, __out_ecount_opt(cchData) LPWSTR lpLCData, __in int cchData)
    {
        _ASSERTE((lpLCData == NULL && cchData == 0) || (lpLCData != NULL && cchData > 0));
        // ComNlsInfo::nativeInitCultureData calls GetLocaleInfoEx with LcType LOCALE_SNAME
        // to determine if this is a valid culture. We shouldn't assert in this case, but 
        // all others we should.
        _ASSERTE(LCType == LOCALE_SNAME || NotLeakingFrameworkOnlyCultures(lpLocaleName));
        int retVal;
        
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::GetLocaleInfoEx(lpLocaleName, LCType, lpLCData, cchData);
#else
        typedef int (WINAPI *PFNGetLocaleInfoEx)(LPCWSTR, LCTYPE, LPWSTR, int);
        static PFNGetLocaleInfoEx pFNGetLocaleInfoEx=NULL;
        if (pFNGetLocaleInfoEx== NULL)
        {            
            pFNGetLocaleInfoEx=(PFNGetLocaleInfoEx)GetProcAddressForLocaleApi(
                                                            "GetLocaleInfoEx", 
                                                            (FARPROC)DownLevel::GetLocaleInfoEx);
        }
        retVal = pFNGetLocaleInfoEx(lpLocaleName,LCType,lpLCData,cchData);

        // Do fallback if we didn't find anything yet
        if (retVal == 0)
            retVal = DownLevel::UplevelFallback::GetLocaleInfoEx(lpLocaleName,0,LCType,lpLCData,cchData);
#endif

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return retVal;
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

    }

    __success(return != 0) int
    GetDateFormatEx(__in LPCWSTR lpLocaleName, __in DWORD dwFlags, __in_opt CONST SYSTEMTIME* lpDate, __in_opt LPCWSTR lpFormat,
                             __out_ecount(cchDate) LPWSTR lpDateStr, __in int cchDate, __in_opt LPCWSTR lpCalendar)    
    {
        _ASSERTE(NotLeakingFrameworkOnlyCultures(lpLocaleName));
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::GetDateFormatEx(lpLocaleName, dwFlags, lpDate, lpFormat, lpDateStr, cchDate, lpCalendar);    
#else
        typedef int (WINAPI *PFNGetDateFormatEx)(LPCWSTR, DWORD, CONST SYSTEMTIME*, LPCWSTR, LPWSTR,int, LPCWSTR);
        static PFNGetDateFormatEx pFNGetDateFormatEx=NULL;
        if (pFNGetDateFormatEx== NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod==NULL)
                return 0;
            pFNGetDateFormatEx=(PFNGetDateFormatEx)GetProcAddress(hMod,"GetDateFormatEx");
            if(pFNGetDateFormatEx==NULL)
            {
                if(GetLastError() == ERROR_PROC_NOT_FOUND)
                    pFNGetDateFormatEx=DownLevel::GetDateFormatEx;
                else
                    return 0;
            }
        }

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return  pFNGetDateFormatEx(lpLocaleName,dwFlags,lpDate,lpFormat,lpDateStr,cchDate,lpCalendar);
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

#endif
    }

#if defined(ENABLE_DOWNLEVEL_FOR_NLS)
    static BOOL AvoidVistaTurkishBug = FALSE;

    __success(return != NULL)
    inline
    FARPROC GetSystemProcAddressForSortingApi(
        __in LPCSTR lpProcName, 
        __in_opt FARPROC pFnDownlevelFallback)
    {
        _ASSERTE(lpProcName != NULL);

        FARPROC result = NULL;

        // try the kernel
        HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
        if(hMod!=NULL)
        {
            result=GetProcAddress(hMod,lpProcName);
        }

        if (IsVistaPlatform()) AvoidVistaTurkishBug = TRUE;

        // failing all that, use the fallback provided
        if(result==NULL)
        {
            result = pFnDownlevelFallback;
        }

        return result;
    }

    __success(return != NULL)
    FARPROC GetProcAddressForSortingApi(
        __in LPCSTR lpProcName, 
        __in FARPROC lpSortDllProcedure, 
        __in_opt FARPROC pFnDownlevelFallback,
        __in_opt CONST NLSVERSIONINFO * lpVersionInformation)
    {
        _ASSERTE(lpProcName != NULL);
        _ASSERTE(lpSortDllProcedure != NULL);

        FARPROC result = NULL;

        // Below windows 8 we have to try the sorting dll
        if (!RunningOnWin8() && SortVersioning::IsAvailableVersion(lpVersionInformation))
        {
            result=lpSortDllProcedure;
        }

        if(result == NULL)
        {
            result = GetSystemProcAddressForSortingApi(lpProcName, pFnDownlevelFallback);
        }
        return result;
    }
#endif // ENABLE_DOWNLEVEL_FOR_NLS
    

#if defined(ENABLE_DOWNLEVEL_FOR_NLS)
    //
    // Vista handle tr-TR and az-Latn-AZ incorrectly with the sorting APIs that takes locale names
    // work around the problem by using the sorting name instead.
    //

    LPWSTR GetLingusticLocaleName(__in LPWSTR pLocaleName, __in DWORD dwFlags)
    {
        _ASSERTE(IsVistaPlatform());

        // If the localeName is NULL, then we are using an OS SortHandle and don't need to fix up
        // anything.
        if (pLocaleName == NULL)
        {
            return pLocaleName;
        }

        if ((dwFlags & CASING_BITS))
        {
            if (_wcsicmp(pLocaleName, TURKISH_LOCALE_NAME) == 0)
                return TURKISH_SORTING_LOCALE_NAME;
            
            if (_wcsicmp(pLocaleName, AZERBAIJAN_LOCALE_NAME) == 0)
                return AZERBAIJAN_SORTING_LOCALE_NAME;
        }
        return pLocaleName;
    }
#endif

    //
    // NOTE: We assume that we're only being called from the BCL with an explicit locale name, so we don't
    //       support the system/user default tokens used in the OS.
    //
    // Additionally this is only called for casing and sort keys, other functionality isn't supported.
    //
    int CompareStringEx(__in LPCWSTR lpLocaleName, __in DWORD dwCmpFlags, __in_ecount(cchCount1) LPCWSTR lpString1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR lpString2,
                                               __in int cchCount2, __in_opt LPNLSVERSIONINFO lpVersionInformation, __in_opt LPVOID lpReserved, __in_opt LPARAM lParam )
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SO_TOLERANT;
            PRECONDITION((lParam == 0 && CheckPointer(lpLocaleName)) || (lParam != 0 && lpLocaleName == NULL));
            PRECONDITION(CheckPointer(lpString1));
            PRECONDITION(CheckPointer(lpString2));
        } CONTRACTL_END;

        _ASSERTE(lpLocaleName == NULL || NotLeakingFrameworkOnlyCultures(lpLocaleName));
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return::CompareStringEx(lpLocaleName, dwCmpFlags, lpString1, cchCount1, lpString2,
                                                cchCount2, lpVersionInformation, lpReserved, lParam );
#else
        typedef int (WINAPI *PFNCompareStringEx)(LPCWSTR, DWORD, LPCWSTR, int, LPCWSTR, int, LPNLSVERSIONINFO,  LPVOID,  LPARAM );
        static PFNCompareStringEx pFNCompareStringEx=NULL;

        // See if we loaded our pointer already
        if (pFNCompareStringEx == NULL)
        {
            pFNCompareStringEx = (PFNCompareStringEx) GetProcAddressForSortingApi(
                                            "CompareStringEx", 
                                            (FARPROC)SortVersioning::SortCompareString,
                                            (FARPROC)DownLevel::CompareStringEx,
                                            lpVersionInformation);
        }

        // TODO: Remove this workaround after Vista SP2 &/or turkic CompareStringEx() gets fixed on Vista.
        // If its Vista and we want a turkik sort, then call CompareStringW not CompareStringEx
        LPCWSTR pLingLocaleName = AvoidVistaTurkishBug ? GetLingusticLocaleName((LPWSTR)lpLocaleName, dwCmpFlags) : lpLocaleName;
        // TODO: End of workaround for turkish CompareStringEx() on Vista/Win2K8
            
        return  pFNCompareStringEx(pLingLocaleName, dwCmpFlags, lpString1, cchCount1, lpString2,
                                                    cchCount2, lpVersionInformation, lpReserved, lParam );
#endif
    }

    // Note that unlike the real version we always expect our callers to pass counted strings
    // I don't think we can assert because I think it'd call this code again
    int CompareStringOrdinal(__in_ecount(cchCount1) LPCWSTR lpString1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR lpString2, __in int cchCount2, __in BOOL bIgnoreCase)
    {
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::CompareStringOrdinal(lpString1, cchCount1, lpString2, cchCount2, bIgnoreCase);
#else
        typedef int (WINAPI *PFNCompareStringOrdinal )(LPCWSTR, int, LPCWSTR, int, BOOL );
        static PFNCompareStringOrdinal pFNCompareStringOrdinal=NULL;
        if (pFNCompareStringOrdinal == NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod != NULL)
            {
                // Grab the OS proc if its there
                pFNCompareStringOrdinal=(PFNCompareStringOrdinal)GetProcAddress(hMod, "CompareStringOrdinal");
            }
            if (pFNCompareStringOrdinal == NULL)
            {
                // Regardless of the reason why, if we can't find the API just use the downlevel version
                pFNCompareStringOrdinal=DownLevel::CompareStringOrdinal;
            }
        }
        return pFNCompareStringOrdinal(lpString1, cchCount1, lpString2, cchCount2, bIgnoreCase);
#endif
    }

// The invariant locale should always match the system for
// upper and lower casing
inline BOOL IsInvariantCasing(__in LPCWSTR lpLocaleName, __in DWORD dwMapFlags)
{
    _ASSERTE(lpLocaleName);

    if(lpLocaleName[0] == NULL)
    {
        if(dwMapFlags & (LCMAP_UPPERCASE | LCMAP_LOWERCASE))
        {
            return TRUE;
        }
    }
    return FALSE;
}

    //
    // NOTE: We assume that we're only being called from the BCL with an explicit locale name, so we don't
    //       support the system/user default tokens used in the OS.
    //
    // Additionally this is only called for casing and sort keys, other functionality isn't supported.
    //
    __success(return != 0)
    int LCMapStringEx (__in LPCWSTR lpLocaleName, __in DWORD dwMapFlags, __in_ecount(cchSrc) LPCWSTR lpSrcStr, __in int cchSrc, 
                           __out_xcount_opt(cchDest) LPWSTR lpDestStr, __in int cchDest, __in_opt LPNLSVERSIONINFO lpVersionInformation, __in_opt LPVOID lpReserved, __in_opt LPARAM lParam )
    {
        int retVal = 0;
        // Note: We should only be calling this for casing or sort keys
        _ASSERTE((dwMapFlags & (LCMAP_UPPERCASE | LCMAP_LOWERCASE | LCMAP_TITLECASE | LCMAP_SORTKEY | (RunningOnWin8() ? (LCMAP_SORTHANDLE | LCMAP_HASH) : 0))) != 0);

        // Need to have a name or sort node
        _ASSERTE(lpLocaleName == NULL && lParam != 0 || lParam == 0 && lpLocaleName != NULL);

        _ASSERTE(lpLocaleName == NULL || NotLeakingFrameworkOnlyCultures(lpLocaleName));

        // Can't use the system token, which starts with an illegal !
        _ASSERTE(lpLocaleName == NULL || lpLocaleName[0] != W('!'));
        
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::LCMapStringEx (lpLocaleName, dwMapFlags, lpSrcStr, cchSrc, 
                                          lpDestStr, cchDest, lpVersionInformation, lpReserved, lParam );
#else
        typedef int (WINAPI *PFNLCMapStringEx )(LPCWSTR, DWORD, LPCWSTR, int,  LPWSTR, int, LPNLSVERSIONINFO, LPVOID, LPARAM);

        PFNLCMapStringEx pLcMapStringEx;
        LPCWSTR pLingLocaleName;
        if(lpLocaleName == NULL || IsInvariantCasing(lpLocaleName, dwMapFlags)){
            static PFNLCMapStringEx pFNSystemLCMapStringEx=NULL;
            if (pFNSystemLCMapStringEx == NULL)
            {
                pFNSystemLCMapStringEx = (PFNLCMapStringEx) GetSystemProcAddressForSortingApi(
                                                    "LCMapStringEx", 
                                                    (FARPROC)DownLevel::LCMapStringEx);
            }
            pLcMapStringEx = pFNSystemLCMapStringEx;
            pLingLocaleName = lpLocaleName;
        }
        else
        {
            static PFNLCMapStringEx pFNLCMapStringEx=NULL;
            // See if we still need to find which function to use
            if (pFNLCMapStringEx == NULL)
            {
                pFNLCMapStringEx = (PFNLCMapStringEx) GetProcAddressForSortingApi(
                                                "LCMapStringEx", 
                                                (FARPROC)SortVersioning::LCMapStringEx,
                                                (FARPROC)DownLevel::LCMapStringEx,
                                                lpVersionInformation);
            }
            pLcMapStringEx = pFNLCMapStringEx;

            // TODO: Remove this workaround after Vista SP2 &/or turkic CompareStringEx() gets fixed on Vista.
            // If its Vista and we want a turkik sort, then call CompareStringW not CompareStringEx
            pLingLocaleName = AvoidVistaTurkishBug ? GetLingusticLocaleName((LPWSTR)lpLocaleName, dwMapFlags) : lpLocaleName;
            // TODO: End of workaround for turkish CompareStringEx() on Vista/Win2K8
        }

        retVal = pLcMapStringEx(pLingLocaleName, dwMapFlags, lpSrcStr, cchSrc, 
                                          lpDestStr, cchDest, lpVersionInformation, lpReserved, lParam);
        if (retVal == 0)
            retVal = DownLevel::UplevelFallback::LCMapStringEx(lpLocaleName, dwMapFlags, lpSrcStr, cchSrc, 
                                        lpDestStr, cchDest, lpVersionInformation, lpReserved, lParam);
#endif

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return retVal;
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif
    }

    __inline BOOL IsLowerAsciiString(__in_ecount(cchCount) LPCWSTR lpString, __in int cchCount)
    {
        int     count = cchCount;
        LPCWSTR pStr = lpString;
        __range(0,10) int     cch;
        int     value = 0;

        while (count > 0)
        {
            cch = min(count, 10);
            switch (cch)
            {

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26010) // Prefast - Potential read overflow of null terminated buffer using expression 'pStr[9]'
#endif // _PREFAST_
                case 10: value |= (int) pStr[9]; __fallthrough; // fall through 
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif
                case 9:  value |= (int) pStr[8]; __fallthrough; // fall through 
                case 8:  value |= (int) pStr[7]; __fallthrough; // fall through 
                case 7:  value |= (int) pStr[6]; __fallthrough; // fall through 
                case 6:  value |= (int) pStr[5]; __fallthrough; // fall through 
                case 5:  value |= (int) pStr[4]; __fallthrough; // fall through 
                case 4:  value |= (int) pStr[3]; __fallthrough; // fall through 
                case 3:  value |= (int) pStr[2]; __fallthrough; // fall through 
                case 2:  value |= (int) pStr[1]; __fallthrough; // fall through 
                case 1:  value |= (int) pStr[0]; __fallthrough; // fall through 
            }
            
            if (value >= 0x80)
            {
                return FALSE;
            }

            count -= cch;
            pStr  += cch;
        }

        return TRUE;
    }

    INT32 FastIndexOfString(__in_ecount(sourceLength) const WCHAR *source, __in INT32 sourceLength, __in_ecount(patternLength) const WCHAR *pattern, __in INT32 patternLength)
    {
        if (source == NULL || pattern == NULL || patternLength < 0 || sourceLength < 1)
        {
            return -1;
        }
        
        INT32 startIndex = 0;
        INT32 endIndex = sourceLength - 1;

        int endPattern = endIndex - patternLength + 1;

        if (endPattern<0) {
            return -1;
        }

        if (patternLength == 0) {
            return startIndex;
        }

        WCHAR patternChar0 = pattern[0];
        for (int ctrSrc = startIndex; ctrSrc<=endPattern; ctrSrc++) {
            if (source[ctrSrc] != patternChar0)
                continue;
            int ctrPat;
            for (ctrPat = 1; ctrPat < patternLength; ctrPat++) {
                if (source[ctrSrc + ctrPat] != pattern[ctrPat]) break;
            }
            if (ctrPat == patternLength) {
                return (ctrSrc);
            }
        }

        return (-1);
    }

    INT32 FastIndexOfStringInsensitive(__in_ecount(sourceLength) const WCHAR *source, __in INT32 sourceLength, __in_ecount(patternLength) const WCHAR *pattern, __in INT32 patternLength) 
    {
        if (source == NULL || pattern == NULL || patternLength < 0 || sourceLength < 1)
        {
            return -1;
        }

        INT32 startIndex = 0;
        INT32 endIndex = sourceLength - 1;
        
        WCHAR srcChar;
        WCHAR patChar;

        int endPattern = endIndex - patternLength + 1;

        if (endPattern<0) {
            return -1;
        }

        if (patternLength == 0) {
            return startIndex;
        }

        WCHAR pattern0 = pattern[0];
        if (pattern0>='A' && pattern0<='Z') {
            pattern0|=0x20;
        }

        for (int ctrSrc = startIndex; ctrSrc<=endPattern; ctrSrc++) {
            srcChar = source[ctrSrc];
            if (srcChar>='A' && srcChar<='Z') {
                srcChar|=0x20;
            }
            if (srcChar != pattern0)
                continue;

            int ctrPat;
            for (ctrPat = 1; (ctrPat < patternLength); ctrPat++) {
                srcChar = source[ctrSrc + ctrPat];
                if (srcChar>='A' && srcChar<='Z') {
                    srcChar|=0x20;
                }
                patChar = pattern[ctrPat];
                if (patChar>='A' && patChar<='Z') {
                    patChar|=0x20;
                }
                if (srcChar!=patChar) {
                    break;
                }
            }

            if (ctrPat == patternLength) {
                return (ctrSrc);
            }
        }

        return (-1);
    }

    // Works backwards, starting at startIndex and ending at endIndex
    INT32 FastLastIndexOfString(__in_ecount(sourceLength) const WCHAR *source, __in INT32 sourceLength, __in_ecount(patternLength) const WCHAR *pattern, __in INT32 patternLength)
    {
        if (source == NULL || pattern == NULL || patternLength < 0 || sourceLength < 1)
        {
            return -1;
        }
     
        INT32 startIndex = sourceLength - 1;
        INT32 endIndex = 0;

        //startIndex is the greatest index into the string.
        int startPattern = startIndex - patternLength + 1;

        if (startPattern < 0) {
            return (-1);
        }

        if (patternLength == 0) {
            return startIndex;
        }

        WCHAR patternChar0 = pattern[0];
        for (int ctrSrc = startPattern; ctrSrc >= endIndex; ctrSrc--) {
            if (source[ctrSrc] != patternChar0)
                continue;
            int ctrPat;
            for (ctrPat = 1; ctrPat<patternLength; ctrPat++) {
                if (source[ctrSrc+ctrPat] != pattern[ctrPat]) break;
            }
            if (ctrPat == patternLength) {
                return (ctrSrc);
            }
        }

        return (-1);
    }

    // Works backwards, starting at startIndex and ending at endIndex
    INT32 FastLastIndexOfStringInsensitive(__in_ecount(sourceLength) const WCHAR *source, __in INT32 sourceLength, __in_ecount(patternLength) const WCHAR *pattern, __in INT32 patternLength) 
    {
        if (source == NULL || pattern == NULL || patternLength < 0 || sourceLength < 1)
        {
            return -1;
        }
     
        INT32 startIndex = sourceLength - 1;
        INT32 endIndex = 0;

        //startIndex is the greatest index into the string.
        int startPattern = startIndex - patternLength + 1;

        if (startPattern < 0) {
            return (-1);
        }

        if (patternLength == 0) {
            return startIndex;
        }

        WCHAR srcChar;
        WCHAR patChar;
        WCHAR pattern0 = pattern[0];
        if (pattern0>='A' && pattern0<='Z') {
            pattern0|=0x20;
        }


        for (int ctrSrc = startPattern; ctrSrc >= endIndex; ctrSrc--) {
            srcChar = source[ctrSrc];
            if (srcChar>='A' && srcChar<='Z') {
                srcChar|=0x20;
            }
            if (srcChar != pattern0)
                continue;

            int ctrPat;
            for (ctrPat = 1; ctrPat<patternLength; ctrPat++) {
                srcChar = source[ctrSrc+ctrPat];
                if (srcChar>='A' && srcChar<='Z') {
                    srcChar|=0x20;
                }
                patChar = pattern[ctrPat];
                if (patChar>='A' && patChar<='Z') {
                    patChar|=0x20;
                }
                if (srcChar!=patChar) {
                    break;
                }
            }
            if (ctrPat == patternLength) {
                return (ctrSrc);
            }
        }

        return (-1);
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    //  IndexOfString
    //
    ////////////////////////////////////////////////////////////////////////////
    int IndexOfString(  __in LPCWSTR lpLocaleName,
                        __in_ecount(cchCount1) LPCWSTR pString1,   // String to search in
                        __in int  cchCount1,                       // length of pString1
                        __in_ecount(cchCount2) LPCWSTR pString2,    // String we're looking for
                        __in int cchCount2,                        // length of pString2                  
                        __in DWORD dwFlags,                        // search flags
                        __in BOOL startWith)                       // true if we need to check for prefix case
    {
        int iRetVal = -1;

        //
        //  Check the ranges.
        //
        if (cchCount1 == 0)
        {
            if (cchCount2 == 0)
                iRetVal = 0;
            // else iRetVal = -1 (not found)
            goto lExit;
        }

        //
        //  See if we have an empty string 2.
        //
        if (cchCount2 == 0)
        {
            iRetVal = 0;
            goto lExit;
        }

        //
        //  Search for the character in the string.
        //

        if (dwFlags == COMPARE_OPTIONS_ORDINAL)
        {
            iRetVal = FastIndexOfString(pString1, cchCount1, pString2, cchCount2);
            goto lExit;
        }
        //For dwFlags, 0 is the default, 1 is ignore case, we can handle both.
        // TODO: NLS Arrowhead -This isn't really right, custom locales could start with en- and have different sort behavior
        
        if (((dwFlags & ~CASING_BITS) == 0) && IS_FAST_COMPARE_LOCALE(lpLocaleName))
        {
            if (IsLowerAsciiString(pString1, cchCount1) && IsLowerAsciiString(pString2, cchCount2))
            {
                if ((dwFlags & (LINGUISTIC_IGNORECASE | NORM_IGNORECASE)) == 0)
                    iRetVal = FastIndexOfString(pString1, cchCount1, pString2, cchCount2);
                else
                    iRetVal = FastIndexOfStringInsensitive(pString1, cchCount1, pString2, cchCount2);
                goto lExit;
            }
        }

        _ASSERTE(iRetVal==-1);
        int result;

        // Some things to think about, depending on the options passed in:
        // 
        // LINGUISTIC_IGNORECASE - Can't cause length changes since casing is always changing to the same length
        // NORM_IGNORECASE - Can't cause length changes since casing is always changing to the same length
        // NORM_LINGUISTIC_CASING - Can't cause length changes since casing is always changing to the same length
        // NORM_IGNOREKANATYPE - A 1:1 mapping, so the lengths don't change
        // NORM_IGNOREWIDTH - A 1:1 mapping (full & half width), so lengths don't change
        // SORT_STRINGSORT - No impact on search size - special treatment for - and '
        // LINGUISTIC_IGNOREDIACRITIC - Terrible because both strings could be all diacritics, except for the last character.
        // NORM_IGNORENONSPACE -Terrible because both strings could all be non-spacing characters, except for 1 somewhere.
        // NORM_IGNORESYMBOLS - Terrible because both strings could be all symbols
        // Compressions/Expansions - Either string may have compressions or expansions impacting the size needing searched
        //   for default table cultures (including invariant) there're only expansions, and those only expand 2X worst case.
        
        for (int iOffset=0; iRetVal == -1 && iOffset<cchCount1; iOffset++)
        {
            // Because of compressions/expansions/ignorable characters we can't just use the known length, but need to consider the entire remainder of the string.
            // TODO: NLS: the nested loop is extremely slow.  oledbtest.exe can't finish even overnight. (Doing invariant ignore case)
            for (int iLength=1; iLength<=cchCount1 - iOffset; iLength++)
            {
                result = NewApis::CompareStringEx(lpLocaleName, dwFlags, pString2, cchCount2, &pString1[iOffset], iLength, NULL, NULL, 0);
                if (result == CSTR_EQUAL)
                { 
                    iRetVal = iOffset; 
                    break;
                }
                else if (result == 0)
                {
                    // return value of 0 indicates failure and error value is supposed to be set.
                    // shouldn't ever really happen
                    _ASSERTE(!"catastrophic failure calling NewApis::CompareStringEx!  This could be a CultureInfo, RegionInfo, or Calendar bug (bad localeName string) or maybe a GCHole.");
                }
            }
        }

    lExit:
        if (startWith && iRetVal != 0)
        {
            iRetVal = -1;
        }

        return iRetVal;
    }
    ////////////////////////////////////////////////////////////////////////////
    //
    //  LastIndexOfString
    //
    ////////////////////////////////////////////////////////////////////////////
    int LastIndexOfString(  __in LPCWSTR lpLocaleName,
                            __in_ecount(cchCount1) LPCWSTR pString1,   // String to search in
                            __in int  cchCount1,                       // length of pString1
                            __in_ecount(cchCount2) LPCWSTR pString2,    // String we're looking for
                            __in int cchCount2,                        // length of pString2                  
                            __in DWORD dwFlags,
                            __in BOOL endWith)                         // check suffix case
        {
        INT32       iRetVal = -1;
        BOOL        comparedOrdinal = FALSE;

        // Check for empty strings
        if (cchCount1 == 0)
        {
            if (cchCount2 == 0)
                iRetVal = 0;
            // else iRetVal = -1 (not found)
            goto lExit;
        }

        //
        //  See if we have an empty string 2.
        //
        if (cchCount2 == 0)
        {
            iRetVal = 0;
            goto lExit;
        }

        //
        //  Search for the character in the string.
        //<TODO>
        //  @ToDo: Should read the nls data tables directly to make this
        //         much faster and to handle composite characters.
        //</TODO>

        if (dwFlags == COMPARE_OPTIONS_ORDINAL) 
        {
            iRetVal = FastLastIndexOfString(pString1, cchCount1, pString2, cchCount2);
            comparedOrdinal = TRUE;
            goto lExit;
        }

        //For dwFlags, 0 is the default, 1 is ignore case, we can handle both.
        // TODO: NLS Arrowhead -This isn't really right, custom locales could start with en- and have different sort behavior
        if (((dwFlags & ~CASING_BITS) == 0) && IS_FAST_COMPARE_LOCALE(lpLocaleName))
        {
            if (IsLowerAsciiString(pString1, cchCount1) && IsLowerAsciiString(pString2, cchCount2))
            {
                if ((dwFlags & (LINGUISTIC_IGNORECASE | NORM_IGNORECASE)) == 0)
                    iRetVal = FastLastIndexOfString(pString1, cchCount1, pString2, cchCount2);
                else
                    iRetVal = FastLastIndexOfStringInsensitive(pString1, cchCount1, pString2, cchCount2);
                comparedOrdinal = TRUE;
                goto lExit;
            }
        }

        _ASSERTE(iRetVal==-1);
        // TODO: Cleanup like IndexOfString
        for (int iOffset=0; iRetVal == -1 && iOffset>-cchCount1; iOffset--)
        {
            for (int iLength=1; iLength<=cchCount1 + iOffset; iLength++)
            {
                if (NewApis::CompareStringEx(lpLocaleName, dwFlags, pString2, cchCount2, &pString1[cchCount1 + iOffset - iLength], iLength, NULL, NULL, 0) == CSTR_EQUAL)
                { 
                    iRetVal= cchCount1 + iOffset - iLength;
                    break;
                }
            }
        }
    lExit:

        if (endWith && iRetVal>=0)
        {
            if (comparedOrdinal && (cchCount1 - iRetVal != cchCount2)) // optimize here to avoid calling CompareString
            {
                iRetVal = -1;    
            }
            else if (NewApis::CompareStringEx(lpLocaleName, dwFlags, pString2, cchCount2, &pString1[iRetVal], cchCount1 - iRetVal, NULL, NULL, 0) != CSTR_EQUAL)
            {
                iRetVal = -1;
            }
        }

        return iRetVal;
    }

    //
    // NOTE: We assume that we're only being called from the BCL with an explicit locale name, so we don't
    //       support the system/user default tokens used in the OS.
    //
    // Additionally this is only called for casing and sort keys, other functionality isn't supported.
    //
    int FindNLSStringEx(__in LPCWSTR lpLocaleName,
                        __in DWORD dwFindNLSStringFlags,
                        __in_ecount(cchSource) LPCWSTR lpStringSource,
                        __in int cchSource,
                        __in_ecount(cchValue) LPCWSTR lpStringValue,
                        __in int cchValue,
                        __out_opt LPINT pcchFound,
                        __in_opt LPNLSVERSIONINFO lpVersionInformation,
                        __in_opt LPVOID lpReserved,
                        __in_opt LPARAM lParam)
    {
        _ASSERTE(lpLocaleName == NULL || NotLeakingFrameworkOnlyCultures(lpLocaleName));
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::FindNLSStringEx(lpLocaleName, dwFindNLSStringFlags, lpStringSource, cchSource, lpStringValue, cchValue, pcchFound,
                                 lpVersionInformation, lpReserved, lParam );
#else
        typedef int (WINAPI *PFNFindNLSStringEx )(LPCWSTR, DWORD, LPCWSTR, int, LPCWSTR, int, LPINT, LPNLSVERSIONINFOEX, LPVOID, LPARAM );
        static PFNFindNLSStringEx pFNFindNLSStringEx=NULL;

        // See if we still need to figure out which function to call
        if (pFNFindNLSStringEx == NULL)
        {
            pFNFindNLSStringEx = (PFNFindNLSStringEx) GetProcAddressForSortingApi(
                                        "FindNLSStringEx", 
                                        (FARPROC)SortVersioning::SortFindString,
                                        (FARPROC)DownLevel::FindNLSStringEx,
                                        lpVersionInformation);
        }

        // TODO: Remove this workaround after Vista SP2 &/or turkic CompareStringEx() gets fixed on Vista.
        // If its Vista and we want a turkik sort, then call CompareStringW not CompareStringEx
        LPCWSTR pLingLocaleName = AvoidVistaTurkishBug ? GetLingusticLocaleName((LPWSTR)lpLocaleName, dwFindNLSStringFlags) : lpLocaleName;
        // TODO: End of workaround for turkish CompareStringEx() on Vista/Win2K8

        int cchFound; // we need to get the length even if the caller doesn't care about it (see below)
        int result = pFNFindNLSStringEx(pLingLocaleName, dwFindNLSStringFlags, lpStringSource, cchSource, lpStringValue, cchValue, &cchFound,
                                  lpVersionInformation, lpReserved, lParam );
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
#endif
    }

    __success(return != 0) int 
    GetCalendarInfoEx(__in LPCWSTR lpLocaleName, __in CALID Calendar, __in_opt LPCWSTR pReserved, __in CALTYPE CalType, __out_ecount_opt(cchData) LPWSTR lpCalData, __in int cchData, __out_opt LPDWORD lpValue )
    {

        _ASSERTE(NotLeakingFrameworkOnlyCultures(lpLocaleName));
        if ( (lpCalData != NULL && cchData == 0) || (lpCalData == NULL && cchData > 0) )
        {
            _ASSERTE(FALSE);
            SetLastError(ERROR_INVALID_PARAMETER);
            return 0;
        }

        if ((CalType & CAL_RETURN_NUMBER))
        {
            // If CAL_RETURN_NUMBER, lpValue must be non-null and lpCalData must be null
            if (lpValue == NULL || lpCalData != NULL)
            {
                _ASSERTE(FALSE);
                SetLastError(ERROR_INVALID_PARAMETER);
                return 0;
            }
        }

        int retVal = 0;
        
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::GetCalendarInfoEx(lpLocaleName, Calendar, pReserved, CalType, lpCalData, cchData, lpValue );
#else
        typedef int (WINAPI *PFNGetCalendarInfoEx )(LPCWSTR, CALID, LPCWSTR, CALTYPE, LPWSTR, int, LPDWORD );
        static PFNGetCalendarInfoEx pFNGetCalendarInfoEx=NULL;
        if (pFNGetCalendarInfoEx== NULL)
        {
            pFNGetCalendarInfoEx=(PFNGetCalendarInfoEx)GetProcAddressForLocaleApi(
                                                    "GetCalendarInfoEx",
                                                    (FARPROC)DownLevel::GetCalendarInfoEx);
        }
        retVal = pFNGetCalendarInfoEx(lpLocaleName, Calendar, pReserved, CalType, lpCalData, cchData, lpValue );
    
        // Do fallback if we didn't find anything yet
        if (retVal == 0)
            retVal = DownLevel::UplevelFallback::GetCalendarInfoEx(lpLocaleName, Calendar, pReserved, CalType, lpCalData, cchData, lpValue );
#endif

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return retVal;
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif
    }

    __success(return != 0)
    int LCIDToLocaleName(__in LCID Locale, __out_ecount_opt(cchName) LPWSTR lpName, __in int cchName, __in DWORD dwFlags)
    {
        int retVal = 0;

#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::LCIDToLocaleName(Locale, lpName, cchName, dwFlags);
#else

#ifdef FEATURE_CORECLR
        retVal = DownLevel::LCIDToLocaleName(Locale, lpName,cchName,dwFlags);
#endif // FEATURE_CORECLR

        typedef int (WINAPI *PFNLCIDToLocaleName)(LCID, LPWSTR,int ,DWORD);
        static PFNLCIDToLocaleName pFNLCIDToLocaleName=NULL;
        if (retVal == 0)
        {
            if (pFNLCIDToLocaleName==NULL)
            {
                pFNLCIDToLocaleName=(PFNLCIDToLocaleName)GetProcAddressForLocaleApi(
                                                                "LCIDToLocaleName",
                                                                (FARPROC)DownLevel::LCIDToLocaleName);

            }

            // Try with the allow neutral flag (will fail in Vista, but 
            // Downlevel::LCIDToLocaleName knows all vista locales)
            retVal = pFNLCIDToLocaleName(Locale, lpName, cchName, dwFlags | LOCALE_ALLOW_NEUTRAL_NAMES);
            if(retVal == 0)
            {
                // in case we are using OS, it could have a problem with the above flag; retry without it
                retVal = pFNLCIDToLocaleName(Locale, lpName, cchName, dwFlags);
            }
        }
#endif // ENABLE_DOWNLEVEL_FOR_NLS

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return retVal;
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif
    }

    LCID LocaleNameToLCID(__in_opt LPCWSTR lpName , __in DWORD dwFlags)
    {
        LCID retVal = 0;

#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::LocaleNameToLCID(lpName, dwFlags);
#else
#ifdef FEATURE_CORECLR
        retVal = DownLevel::LocaleNameToLCID(lpName, dwFlags);
#endif // FEATURE_CORECLR

        typedef int (WINAPI *PFNLocaleNameToLCID)(LPCWSTR,DWORD);
        static PFNLocaleNameToLCID pFNLocaleNameToLCID=NULL;

        if (retVal == 0)
        {
            if (pFNLocaleNameToLCID==NULL)
            {
                pFNLocaleNameToLCID=(PFNLocaleNameToLCID)GetProcAddressForLocaleApi(
                                                                "LocaleNameToLCID", 
                                                                (FARPROC)DownLevel::LocaleNameToLCID);

            }

            // Try with the allow neutral flag (will fail in Vista, but 
            // Downlevel::LocaleNametoLCID knows all vista locales)            
            retVal = pFNLocaleNameToLCID(lpName, dwFlags | LOCALE_ALLOW_NEUTRAL_NAMES);          
            if(retVal == 0)
            {
                // in case we are using OS, it could have a problem with the above flag; retry without it
                retVal = pFNLocaleNameToLCID(lpName, dwFlags);
            }
        }
#endif // ENABLE_DOWNLEVEL_FOR_NLS

        return retVal;
    }

    __success(return != 0) BOOL
    EnumDateFormatsExEx (DATEFMT_ENUMPROCEXEX lpDateFmtEnumProcExEx, LPCWSTR lpLocaleName, DWORD dwFlags, LPARAM lParam)
    {
        _ASSERTE(NotLeakingFrameworkOnlyCultures(lpLocaleName));
        int retVal = 0;
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::EnumDateFormatsExEx (lpDateFmtEnumProcExEx, lpLocaleName, dwFlags, lParam);
#else
        typedef int (WINAPI *PFNEnumDateFormatsExEx)(DATEFMT_ENUMPROCEXEX, LPCWSTR, DWORD, LPARAM);
        
        static PFNEnumDateFormatsExEx pFNEnumDateFormatsExEx=NULL;
        if (pFNEnumDateFormatsExEx==NULL)
        {
            pFNEnumDateFormatsExEx=(PFNEnumDateFormatsExEx)GetProcAddressForLocaleApi(
                                        "EnumDateFormatsExEx",
                                        (FARPROC)DownLevel::LegacyCallbacks::EnumDateFormatsExEx);
        }
        
        retVal = pFNEnumDateFormatsExEx(lpDateFmtEnumProcExEx, lpLocaleName, dwFlags, lParam);
        if (retVal == 0)
        {
            retVal = DownLevel::LegacyCallbacks::EnumDateFormatsExEx(lpDateFmtEnumProcExEx, lpLocaleName, dwFlags, lParam);
        }
#endif    
        return retVal;
    }    

    __success(return != 0)
    BOOL EnumTimeFormatsEx(TIMEFMT_ENUMPROCEX lpTimeFmtEnumProcEx, LPCWSTR lpLocaleName,  DWORD dwFlags, LPARAM lParam)
    {
        _ASSERTE(NotLeakingFrameworkOnlyCultures(lpLocaleName));
        int retVal = 0;
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::EnumTimeFormatsEx(lpTimeFmtEnumProcEx, lpLocaleName,  dwFlags, lParam);
#else
        typedef int (WINAPI *PFNEnumTimeFormatsEx)(TIMEFMT_ENUMPROCEX, LPCWSTR,  DWORD, LPARAM);
        
        static PFNEnumTimeFormatsEx pFNEnumTimeFormatsEx=NULL;
        if (pFNEnumTimeFormatsEx==NULL)
        {
            pFNEnumTimeFormatsEx=(PFNEnumTimeFormatsEx)GetProcAddressForLocaleApi(
                                        "EnumTimeFormatsEx",
                                        (FARPROC)DownLevel::LegacyCallbacks::EnumTimeFormatsEx);
        }
        
        retVal = pFNEnumTimeFormatsEx(lpTimeFmtEnumProcEx, lpLocaleName,  dwFlags, lParam);
        if (retVal == 0)
        {
            retVal = DownLevel::LegacyCallbacks::EnumTimeFormatsEx(lpTimeFmtEnumProcEx, lpLocaleName,  dwFlags, lParam);
        }
#endif    
        return retVal;
    }
 
    __success(return != 0)
    BOOL EnumCalendarInfoExEx(CALINFO_ENUMPROCEXEX pCalInfoEnumProcExEx, LPCWSTR lpLocaleName, CALID Calendar, CALTYPE CalType, LPARAM lParam)
    {
        _ASSERTE(NotLeakingFrameworkOnlyCultures(lpLocaleName));
        int retVal = 0;
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::EnumCalendarInfoExEx (pCalInfoEnumProcExEx, lpLocaleName, Calendar, NULL, CalType, lParam);
#else
        typedef int (WINAPI *PFNEnumCalendarInfoExEx)(CALINFO_ENUMPROCEXEX, LPCWSTR, CALID, LPCWSTR, CALTYPE, LPARAM);
        
        static PFNEnumCalendarInfoExEx pFNEnumCalendarInfoExEx=NULL;
        if (pFNEnumCalendarInfoExEx==NULL)
        {
            pFNEnumCalendarInfoExEx=(PFNEnumCalendarInfoExEx)GetProcAddressForLocaleApi(
                                        "EnumCalendarInfoExEx",
                                        (FARPROC)DownLevel::LegacyCallbacks::EnumCalendarInfoExEx);
        }
        
        retVal = pFNEnumCalendarInfoExEx(pCalInfoEnumProcExEx, lpLocaleName, Calendar, NULL, CalType, lParam);
        if (retVal == 0)
        {
            retVal = DownLevel::LegacyCallbacks::EnumCalendarInfoExEx(pCalInfoEnumProcExEx, lpLocaleName, Calendar, NULL, CalType, lParam);
        }
#endif

        return retVal;
    }

    // This function exists is in server 2003 and above
    // Function should be COMPARE_STRING, dwFlags should be NULL, lpVersionInfo should be NULL for now
    BOOL IsNLSDefinedString(__in NLS_FUNCTION Function, __in DWORD dwFlags, __in_opt LPNLSVERSIONINFOEX lpVersionInfo, __in LPCWSTR lpString, __in int cchStr )
    {
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::IsNLSDefinedString(Function, dwFlags, lpVersionInfo, lpString, cchStr);
#else
        typedef int (WINAPI *PFNIsNLSDefinedString)(NLS_FUNCTION, DWORD, LPNLSVERSIONINFO, LPCWSTR, int);
        
        static PFNIsNLSDefinedString pFNIsNLSDefinedString=NULL;

        // See if we still need to find our function
        if (pFNIsNLSDefinedString== NULL)
        {
            pFNIsNLSDefinedString = (PFNIsNLSDefinedString) GetProcAddressForSortingApi(
                                        "IsNLSDefinedString", 
                                        (FARPROC)SortVersioning::SortIsDefinedString,
                                        (FARPROC)DownLevel::IsNLSDefinedString,
                                        lpVersionInfo);
        }

        // Call the appropriate function and return
        return pFNIsNLSDefinedString(Function, dwFlags, (LPNLSVERSIONINFO)lpVersionInfo, lpString, cchStr);
#endif
    }


#if !defined(FEATURE_CORECLR)
    BOOL GetNlsVersionEx(__in NLS_FUNCTION Function, __in LPCWSTR lpLocaleName, __inout LPNLSVERSIONINFOEX lpVersionInfo)
    {

        typedef BOOL (WINAPI *PFNGetNLSVersionEx)(NLS_FUNCTION, LPCWSTR, LPNLSVERSIONINFOEX);
        
        static PFNGetNLSVersionEx pFNGetNLSVersionEx=NULL;

        // See if we still need to find our function
        if (pFNGetNLSVersionEx == NULL)
        {
            // We only call this on Win8 and above, so this should always work.
            pFNGetNLSVersionEx = (PFNGetNLSVersionEx) GetSystemProcAddressForSortingApi("GetNLSVersionEx", NULL);
        }

        _ASSERTE(pFNGetNLSVersionEx != NULL);

        return pFNGetNLSVersionEx(Function, lpLocaleName, lpVersionInfo);
    }
#endif

    // This is a Windows 7 and above function
    // This returns the "specific" locale from an input name, ie: "en" returns "en-US",
    // although note that it should always succeed!(returning "" neutral if nothing else)
    __success(return != 0)
    int ResolveLocaleName(__in LPCWSTR lpNameToResolve, __in_ecount_opt(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName)
    {
        _ASSERTE(NotLeakingFrameworkOnlyCultures(lpLocaleName));
        int retVal = 0;
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        retVal = ::ResolveLocaleName(lpNameToResolve, lpLocaleName, cchLocaleName);
#else
        typedef int (WINAPI *PFNResolveLocaleName)(LPCWSTR, LPWSTR, int);
        
        static PFNResolveLocaleName pFNResolveLocaleName=NULL;
        if (pFNResolveLocaleName == NULL)
        {
            pFNResolveLocaleName =(PFNResolveLocaleName) GetProcAddressForLocaleApi(
                                                                "ResolveLocaleName", 
                                                                (FARPROC)DownLevel::ResolveLocaleName);
        }
        retVal = pFNResolveLocaleName(lpNameToResolve, lpLocaleName, cchLocaleName);
        if (retVal == 0)
        {
            retVal = DownLevel::ResolveLocaleName(lpNameToResolve, lpLocaleName, cchLocaleName);
        }
#endif

        return retVal;
    }
    
    __success(return == TRUE) BOOL 
    GetThreadPreferredUILanguages(__in DWORD dwFlags,
                                       __out PULONG pulNumLanguages,
                                       __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer,
                                       __inout PULONG pcchLanguagesBuffer)
    {
        
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::GetThreadPreferredUILanguages(dwFlags, pulNumLanguages,pwszLanguagesBuffer,pcchLanguagesBuffer);
#else
        typedef int (WINAPI *PFNGetThreadPreferredUILanguages)(DWORD, PULONG,PWSTR,PULONG);
        static PFNGetThreadPreferredUILanguages pFNGetThreadPreferredUILanguages=NULL;
        if (pFNGetThreadPreferredUILanguages == NULL)
        {
            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod==NULL)
                return 0;
            pFNGetThreadPreferredUILanguages=(PFNGetThreadPreferredUILanguages)GetProcAddress(hMod,"GetThreadPreferredUILanguages");
            if(pFNGetThreadPreferredUILanguages==NULL)
            {
                if(GetLastError() == ERROR_PROC_NOT_FOUND)
                    pFNGetThreadPreferredUILanguages=DownLevel::GetThreadPreferredUILanguages;
                else
                    return 0;
            }
        }
#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable: 26036) // Prefast - Possible postcondition violation due to failure to null terminate string
#endif // _PREFAST_
        return  pFNGetThreadPreferredUILanguages(dwFlags, pulNumLanguages,pwszLanguagesBuffer,pcchLanguagesBuffer);
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

#endif
    }

    __success(return != 0)
    BOOL WINAPI EnumSystemLocalesEx(
        __in LOCALE_ENUMPROCEX lpLocaleEnumProc,
        __in DWORD dwFlags,
        __in LPARAM lParam,
        __in_opt LPVOID lpReserved)
    {
#if !defined(ENABLE_DOWNLEVEL_FOR_NLS)
        return ::EnumSystemLocalesEx(lpLocaleEnumProc, dwFlags, lParam, lpReserved);
#else
        typedef BOOL (WINAPI *PFNEnumSystemLocalesEx)(
            LOCALE_ENUMPROCEX lpLocaleEnumProc,
            DWORD dwFlags,
            LPARAM lParam,
            LPVOID lpReserved);
        static PFNEnumSystemLocalesEx pFNEnumSystemLocalesEx=NULL;
        if (pFNEnumSystemLocalesEx== NULL)
        {   
            pFNEnumSystemLocalesEx=(PFNEnumSystemLocalesEx)GetProcAddressForLocaleApi(
                                                            "EnumSystemLocalesEx", 
                                                            NULL);
            if (pFNEnumSystemLocalesEx == NULL)
            {

                return FALSE;
            }
        }
        BOOL result = pFNEnumSystemLocalesEx(lpLocaleEnumProc, dwFlags, lParam, lpReserved);

        {
            if(result == FALSE 
              && GetLastError() == ERROR_BADDB
              && IsWindows7Platform())
            {
                HKEY hKey;
                if (::RegOpenKeyEx(
                            HKEY_LOCAL_MACHINE, 
                            W("SYSTEM\\CurrentControlSet\\Control\\Nls\\ExtendedLocale"),
                            0, 
                            KEY_READ, 
                            &hKey) == ERROR_SUCCESS)
                {
                    ::RegCloseKey(hKey);
                }
                else 
                {
                    result = TRUE;
                }
            }
        }
        return result;
#endif
    }        
}






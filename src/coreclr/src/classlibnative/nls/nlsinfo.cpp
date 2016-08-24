// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  Class:    NLSInfo
//

//
//  Purpose:  This module implements the methods of the COMNlsInfo
//            class.  These methods are the helper functions for the
//            Locale class.
//
//  Date:     August 12, 1998
//
////////////////////////////////////////////////////////////////////////////

//
//  Include Files.
//
#include "common.h"
#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "interoputil.h"
#include "corhost.h"

#include <winnls.h>

#include "utilcode.h"
#include "frames.h"
#include "field.h"
#include "metasig.h"
#include "nls.h"
#include "nlsinfo.h"
#include "nlstable.h"

//#include <mlang.h>
#include "sortversioning.h"

#include "newapis.h"

//
//  Constant Declarations.
//

#ifndef COMPARE_OPTIONS_IGNORECASE
#define COMPARE_OPTIONS_IGNORECASE  0x00000001
#endif

#ifndef LOCALE_SNAME
#define LOCALE_SNAME                0x0000005c
#endif

#ifndef LOCALE_SNAN
#define LOCALE_SNAN                 0x00000069
#endif

#ifndef LOCALE_SPOSINFINITY
#define LOCALE_SPOSINFINITY         0x0000006a
#endif

#ifndef LOCALE_SNEGINFINITY
#define LOCALE_SNEGINFINITY         0x0000006b
#endif

#ifndef LOCALE_SPARENT
#define LOCALE_SPARENT              0x0000006d
#endif

#ifndef LOCALE_SCONSOLEFALLBACKNAME
#define LOCALE_SCONSOLEFALLBACKNAME 0x0000006e   // Fallback name for within the console
#endif

#ifndef LOCALE_SISO3166CTRYNAME2
#define LOCALE_SISO3166CTRYNAME2    0x00000068
#endif

#ifndef LOCALE_SISO639LANGNAME2
#define LOCALE_SISO639LANGNAME2     0x00000067
#endif

#ifndef LOCALE_SSHORTESTDAYNAME1
#define LOCALE_SSHORTESTDAYNAME1    0x00000060
#endif

// Windows 7 LCTypes
#ifndef LOCALE_INEUTRAL
#define LOCALE_INEUTRAL             0x00000071   // Returns 0 for specific cultures, 1 for neutral cultures.
#endif

#ifndef LCMAP_TITLECASE
#define LCMAP_TITLECASE             0x00000300   // Title Case Letters
#endif

// Windows 8 LCTypes
#ifndef LCMAP_SORTHANDLE
#define LCMAP_SORTHANDLE   0x20000000
#endif

#ifndef LCMAP_HASH
#define LCMAP_HASH   0x00040000
#endif

#ifndef LOCALE_REPLACEMENT
#define LOCALE_REPLACEMENT          0x00000008   // locales that replace shipped locales (callback flag only)
#endif // LOCALE_REPLACEMENT

#define LOCALE_MAX_STRING_SIZE      530          // maximum sice of LOCALE_SKEYBOARDSTOINSTALL, currently 5 "long" + 2 "short" keyboard signatures (YI + 3).

#define MAX_STRING_VALUE        512

// TODO: NLS Arrowhead -Be nice if we could depend more on the OS for this
// Language ID for CHT (Taiwan)
#define LANGID_ZH_TW            0x0404
// Language ID for CHT (Hong-Kong)
#define LANGID_ZH_HK            0x0c04
#define REGION_NAME_0404 W("\x53f0\x7063")
#if BIGENDIAN
#define INTERNATIONAL_CURRENCY_SYMBOL W("\x00a4")
#else
#define INTERNATIONAL_CURRENCY_SYMBOL W("\xa400")
#endif

inline BOOL IsCustomCultureId(LCID lcid)
{
    return (lcid == LOCALE_CUSTOM_DEFAULT || lcid == LOCALE_CUSTOM_UNSPECIFIED);
}

#ifndef FEATURE_CORECLR
//
// Normalization Implementation
//
#define NORMALIZATION_DLL       MAKEDLLNAME(W("normalization"))
HMODULE COMNlsInfo::m_hNormalization = NULL;
PFN_NORMALIZATION_IS_NORMALIZED_STRING COMNlsInfo::m_pfnNormalizationIsNormalizedStringFunc = NULL;
PFN_NORMALIZATION_NORMALIZE_STRING COMNlsInfo::m_pfnNormalizationNormalizeStringFunc = NULL;
PFN_NORMALIZATION_INIT_NORMALIZATION COMNlsInfo::m_pfnNormalizationInitNormalizationFunc = NULL;
#endif

#if FEATURE_CODEPAGES_FILE
/*============================nativeCreateOpenFileMapping============================
**Action: Create or open a named memory file mapping.
**Returns: Pointer to named section, or NULL if failed
**Arguments:
**  StringObject*   inSectionName  - name of section to open/create
**  int             inBytesToAllocate - desired size of memory section in bytes
**                      We use the last 4 bytes (must be aligned, so only choose
**                      inBytesToAllocate in multiples of 4) to indicate if the
**                      section is set or not.  AFTER section is initialized, set
**                      those 4 bytes to non-0, otherwise you'll get get new
**                      heap memory all the time.
**  HANDLE*         mappedFile - is the handle of the memory mapped file. this is
**                      out parameter.
**
** NOTE: We'll try to open the same object, so we can share names.  We don't lock
**       though, so 2 thread could get the same object, but thread 1 might not
**       have initialized it yet.
**
** NOTE: For NT you should add a Global\ to the beginning of the name if you
**       want to share it machine wide.
**
==============================================================================*/
FCIMPL3(LPVOID, COMNlsInfo::nativeCreateOpenFileMapping,
            StringObject* inSectionNameUNSAFE, int inBytesToAllocate, HANDLE *mappedFile)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(inSectionNameUNSAFE));
        PRECONDITION(inBytesToAllocate % 4 == 0);
        PRECONDITION(inBytesToAllocate > 0);
        PRECONDITION(CheckPointer(mappedFile));
    } CONTRACTL_END;

    // Need a place for our result
    LPVOID pResult = NULL;

    STRINGREF inString(inSectionNameUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(inString);

    _ASSERTE(inBytesToAllocate % 4 == 0);   // Expected 4 bytes boundaries so we don't get unaligned
    _ASSERTE(inBytesToAllocate > 0);        // Pointless to have <=0 allocation

    StackSString inNameStackBuffer (inString->GetBuffer());
    pResult = NLSTable::OpenOrCreateMemoryMapping((LPCWSTR)inNameStackBuffer, inBytesToAllocate, mappedFile);

    // Worst case allocate some memory, use holder
    //    if (pResult == NULL) pResult = new BYTE[inBytesToAllocate];
    if (pResult == NULL)
    {
        // Need to use a NewHolder
        NewArrayHolder<BYTE> holder (new BYTE[inBytesToAllocate]);
        pResult = holder;
        // Zero out the mapCodePageCached field (an int value, and it's used to check if the section is initialized or not.)
        BYTE* pByte = (BYTE*)pResult;
        FillMemory(pByte + inBytesToAllocate - sizeof(int), sizeof(int), 0);
        holder.SuppressRelease();
    }

    HELPER_METHOD_FRAME_END();

    return pResult;
}
FCIMPLEND
#endif // FEATURE_CODEPAGES_FILE

// InternalIsSortable
//
// Called by CompareInfo.IsSortable() to determine if a string has entirely sortable (ie: defined) code points.
BOOL QCALLTYPE COMNlsInfo::InternalIsSortable(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName, LPCWSTR string, INT32 length)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(string));
    } CONTRACTL_END;
    BOOL result = FALSE;
    BEGIN_QCALL;

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();

    if(!(curDomain->m_bUseOsSorting))
    {
        handle = EnsureValidSortHandle(handle, handleOrigin, localeName);
        result = SortVersioning::SortDllIsDefinedString((SortVersioning::PSORTHANDLE) handle, COMPARE_STRING, 0, string, length);
    }
    else if(curDomain->m_pCustomSortLibrary != NULL)
    {
        result = (curDomain->m_pCustomSortLibrary->pIsNLSDefinedString)(COMPARE_STRING, 0, NULL, string, length);
    }
    else
#endif
    {
        // Function should be COMPARE_STRING, dwFlags should be NULL, lpVersionInfo should be NULL for now
        result = NewApis::IsNLSDefinedString(COMPARE_STRING, 0, NULL, string, length);
    }

    END_QCALL;
    return result;
}

////////////////////////////////////////////////////////////////////////////
//
//  InternalGetUserDefaultLocaleName
//
//  Returns a string with the name of our LCID and returns 0 in LCID.
//  If we cant return
//
////////////////////////////////////////////////////////////////////////////
// This is new to longhorn
BOOL QCALLTYPE COMNlsInfo::InternalGetDefaultLocaleName(INT32 langType, QCall::StringHandleOnStack defaultLocaleName)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION((langType == LOCALE_SYSTEM_DEFAULT) || (langType == LOCALE_USER_DEFAULT));
    } CONTRACTL_END;

    BOOL result;
    BEGIN_QCALL;

    WCHAR strName[LOCALE_NAME_MAX_LENGTH];
    int size = 0;

    if (langType == LOCALE_SYSTEM_DEFAULT)
    {
        size = NewApis::GetSystemDefaultLocaleName(strName,NumItems(strName));
    }
    else
    {
        _ASSERT(langType == LOCALE_USER_DEFAULT);
        size = NewApis::GetUserDefaultLocaleName(strName,NumItems(strName));
    }

    // Not found, either not longhorn (no LOCALE_SNAME) or not a valid name
    if (size == 0)
    {
        result = false;
    }
    else
    {
        defaultLocaleName.Set(strName);
        result = true;
    }
    END_QCALL;
    return result;
}

BOOL QCALLTYPE COMNlsInfo::InternalGetSystemDefaultUILanguage(QCall::StringHandleOnStack systemDefaultUiLanguage)
{
    QCALL_CONTRACT;
    BOOL result;
    BEGIN_QCALL;

    WCHAR localeName[LOCALE_NAME_MAX_LENGTH];

    int systemDefaultUiLcid = GetSystemDefaultUILanguage();
    if(systemDefaultUiLcid == LANGID_ZH_TW)
    {
        if (!NewApis::IsZhTwSku())
        {
             systemDefaultUiLcid = LANGID_ZH_HK;
        } 
    }
    
    int length = NewApis::LCIDToLocaleName(systemDefaultUiLcid, localeName, NumItems(localeName), 0);
    if (length == 0)
    {
        result = false;
    }
    else
    {
        systemDefaultUiLanguage.Set(localeName);
        result = true;
    }

    END_QCALL;
    return result;
}

/*
 */
BOOL QCALLTYPE COMNlsInfo::InternalGetUserDefaultUILanguage(QCall::StringHandleOnStack userDefaultUiLanguage)
{
    QCALL_CONTRACT;
    BOOL result;
    BEGIN_QCALL;

    WCHAR wszBuffer[LOCALE_NAME_MAX_LENGTH];
    LPCWSTR wszLangName=NULL;

    int res= 0;
    ULONG uLangCount=0;
    ULONG uBufLen=0;
    res= NewApis::GetUserPreferredUILanguages (MUI_LANGUAGE_NAME,&uLangCount,NULL,&uBufLen);
    if (res == 0)
        ThrowLastError();


    NewArrayHolder<WCHAR> sPreferredLanguages(NULL);

    if (uBufLen > 0 && uLangCount > 0 )
    {
        sPreferredLanguages = new WCHAR[uBufLen];
        res= NewApis::GetUserPreferredUILanguages (MUI_LANGUAGE_NAME,&uLangCount,sPreferredLanguages,&uBufLen);

        if (res == 0)
            ThrowLastError();

         wszLangName=sPreferredLanguages;
// Review size_t to int conversion (possible loss of data).
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4267)
#endif
        res=wcslen(wszLangName)+1;
#ifdef _MSC_VER
#pragma warning(pop)
#endif
    }
    else
    {
        res=0;
    }

    if (res == 0) {
        res = NewApis::GetUserDefaultLocaleName(wszBuffer,NumItems(wszBuffer));
        wszLangName=wszBuffer;
    }


    // If not found, either not longhorn (no LOCALE_SNAME) or not a valid name
    if (res == 0)
    {
        // Didn't find string, return an empty string.
        result = false;
    }
    else
    {
        userDefaultUiLanguage.Set(wszLangName);
        result = true;
    }

    // Return the found language name.  LCID should be found one already.
    END_QCALL;
    return result;
}

// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#ifdef FEATURE_CORECLR
FCIMPL0(Object*, COMNlsInfo::nativeGetResourceFallbackArray)
{
    CONTRACTL
    {
        FCALL_CHECK;
    } CONTRACTL_END;

    DWORD dwFlags = MUI_MERGE_USER_FALLBACK | MUI_MERGE_SYSTEM_FALLBACK;
    ULONG cchLanguagesBuffer = 0;
    ULONG ulNumLanguages = 0;
    BOOL result = FALSE;

    struct _gc
    {
        PTRARRAYREF     resourceFallbackArray;
    } gc;

    gc.resourceFallbackArray = NULL;

    // If the resource lookups we're planning on doing are going to be written to a non-Unicode console,
    // then we should ideally only return languages that can be displayed correctly on the console.  The 
    // trick is guessing at whether we're writing this data to the console, which we can't do well.
    // Instead, we ask new apps to call GetConsoleFallbackUICulture & fall back to en-US.
    bool disableUserFallback;
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    disableUserFallback = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Resources_DisableUserPreferredFallback) == 1
    END_SO_INTOLERANT_CODE;

    if (disableUserFallback)
        return NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // first call with null buffer to get size
    result = NewApis::GetThreadPreferredUILanguages(dwFlags, &ulNumLanguages, NULL, &cchLanguagesBuffer);
    if (cchLanguagesBuffer > 0)
    {
        NewArrayHolder<WCHAR> stringBuffer = new (nothrow) WCHAR[cchLanguagesBuffer];
        if (stringBuffer != NULL)
        {
            result = NewApis::GetThreadPreferredUILanguages(dwFlags, &ulNumLanguages, stringBuffer, &cchLanguagesBuffer);
            _ASSERTE(result);

            // now string into strings
            gc.resourceFallbackArray = (PTRARRAYREF) AllocateObjectArray(ulNumLanguages, g_pStringClass);

            LPCWSTR buffer = stringBuffer;   // Restart @ buffer beginning
            for(DWORD i = 0; i < ulNumLanguages; i++)
            {
                OBJECTREF o = (OBJECTREF) StringObject::NewString(buffer);
                gc.resourceFallbackArray->SetAt(i, o);
                buffer += (lstrlenW(buffer) + 1);
            }
        }
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.resourceFallbackArray);

}
FCIMPLEND
#endif // FEATURE_CORECLR

INT32 COMNlsInfo::CallGetUserDefaultUILanguage()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    static INT32 s_lcid = 0;

    // The user UI language cannot change within a process (in fact it cannot change within a logon session),
    // so we cache it. We dont take a lock while initializing s_lcid for the same reason. If two threads are
    // racing to initialize s_lcid, the worst thing that'll happen is that one thread will call
    // GetUserDefaultUILanguage needlessly, but the final result is going to be the same.
    if (s_lcid == 0)
    {
        INT32 s_lcidTemp = GetUserDefaultUILanguage();
        if (s_lcidTemp == LANGID_ZH_TW)
        {
            // If the UI language ID is 0x0404, we need to do extra check to decide
            // the real UI language, since MUI (in CHT)/HK/TW Windows SKU all uses 0x0404 as their CHT language ID.
            if (!NewApis::IsZhTwSku())
            {
                s_lcidTemp = LANGID_ZH_HK;
            }
        }
        s_lcid = s_lcidTemp;
    }

    return s_lcid;
}

INT_PTR COMNlsInfo::EnsureValidSortHandle(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName)
{
    LIMITED_METHOD_CONTRACT;

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();

    if(!(curDomain->m_bUseOsSorting) && handleOrigin == (INT_PTR) SortVersioning::GetSortHandle && ((SortVersioning::PSORTHANDLE) handle)->dwNLSVersion == curDomain->m_sortVersion)
    {
        return handle;
    }

    if(curDomain->m_bUseOsSorting && curDomain->m_pCustomSortLibrary == NULL && handleOrigin == (INT_PTR) NewApis::LCMapStringEx)
    {
        return handle;
    }

    if(curDomain->m_bUseOsSorting && curDomain->m_pCustomSortLibrary != NULL && handleOrigin == (INT_PTR) curDomain->m_pCustomSortLibrary->pLCMapStringEx)
    {
        return handle;
    }

    // At this point, we can't reuse the sort handle (it has different sort semantics than this domain) so we need to get a new one.
    INT_PTR newHandleOrigin;
    return InitSortHandleHelper(localeName, &newHandleOrigin);
#else
    // For CoreCLR, on Windows 8 and up the handle will be valid. on downlevels the handle will be null
    return handle;
#endif
}

#ifdef FEATURE_SYNTHETIC_CULTURES
////////////////////////////////////////////////////////////////////////////
//
//  WstrToInteger4
//
////////////////////////////////////////////////////////////////////////////

/*=================================WstrToInteger4==================================
**Action: Convert a Unicode string to an integer.  Error checking is ignored.
**Returns: The integer value of wstr
**Arguments:
**      wstr: NULL terminated wide string.  Can have character 0'-'9', 'a'-'f', and 'A' - 'F'
**      Radix: radix to be used in the conversion.
**Exceptions: None.
==============================================================================*/

INT32 COMNlsInfo::WstrToInteger4(
    __in_z LPCWSTR wstr,
    __in int Radix)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(wstr));
        PRECONDITION(Radix > 1 && Radix <= 16);
    } CONTRACTL_END;
    INT32 Value = 0;
    int Base = 1;

    for (int Length = Wszlstrlen(wstr) - 1; Length >= 0; Length--)

    {
        WCHAR ch = wstr[Length];
        _ASSERTE((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'));
        if (ch >= 'a')
        {
            ch = ch - 'a' + 'A';
        }

        Value += ((ch >= 'A') ? (ch - 'A' + 10) : (ch - '0')) * Base;
        Base *= Radix;
    }

    return (Value);
}
#endif // FEATURE_SYNTHETIC_CULTURES


#ifndef FEATURE_CORECLR
FCIMPL1(FC_BOOL_RET, COMNlsInfo::nativeSetThreadLocale, StringObject* localeNameUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    LCID lcid = 0;

    // TODO: NLS Arrowhead -A bit scary becausue Set ThreadLocale can't handle custom cultures?
    STRINGREF localeName = (STRINGREF)localeNameUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(localeName);
    lcid=NewApis::LocaleNameToLCID(localeName->GetBuffer(),0);
    if (lcid == 0)
    {
        ThrowHR(HRESULT_FROM_WIN32(GetLastError()));
    }
        HELPER_METHOD_FRAME_END();


    BOOL result = TRUE;

    // SetThreadLocale doesn't handle names/custom cultures
#ifdef _MSC_VER
// Get rid of the SetThreadLocale warning in OACR:
#pragma warning(push)
#pragma warning(disable:38010)
#endif
    result = ::SetThreadLocale(lcid);
#ifdef _MSC_VER
#pragma warning(pop)
#endif

    FC_RETURN_BOOL(result);
}
FCIMPLEND
#endif


FCIMPL2(Object*, COMNlsInfo::nativeGetLocaleInfoEx, StringObject* localeNameUNSAFE, INT32 lcType)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    struct _gc
    {
        STRINGREF   localeName;
        STRINGREF   refRetVal;
    } gc;

    // Dereference our string
    gc.refRetVal = NULL;
    gc.localeName = (STRINGREF)localeNameUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    StackSString localeNameStackBuffer( gc.localeName->GetBuffer() );

    WCHAR buffer[LOCALE_MAX_STRING_SIZE];
    int result = NewApis::GetLocaleInfoEx(localeNameStackBuffer, lcType, buffer, NumItems(buffer));

    // Make a string out of it
    if (result != 0)
    {
        // Exclude the NULL char at the end, except that LOCALE_FONTSIGNATURE isn't
        // really a string, so we need the last character too.
        gc.refRetVal = StringObject::NewString(buffer, ((lcType & ~LOCALE_NOUSEROVERRIDE) == LOCALE_FONTSIGNATURE) ? result : result-1);
    }
    else
    {
    }
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.refRetVal);
}
FCIMPLEND


FCIMPL2(INT32, COMNlsInfo::nativeGetLocaleInfoExInt, StringObject* localeNameUNSAFE, INT32 lcType)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    INT32 result = 0;

    // Dereference our string
    STRINGREF localeName = (STRINGREF)localeNameUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(localeName);

    lcType |= LOCALE_RETURN_NUMBER;

    if (NewApis::GetLocaleInfoEx(localeName->GetBuffer(), lcType, (LPWSTR)&result, sizeof(INT32) / sizeof (WCHAR)) == 0)
    {
        // return value of 0 indicates failure and error value is supposed to be set.
        // shouldn't ever really happen
        _ASSERTE(!"catastrophic failure calling NewApis::nativeGetLocaleInfoExInt!  This could be a CultureInfo bug (bad localeName string) or maybe a GCHole.");
    }

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND



////////////////////////////////////////////////////////////////////////
//
// Call the Win32 GetLocaleInfo() using the specified lcid to retrieve
// the native digits, probably from the registry override. The return
// indicates whether the call was successful.
//
// Parameters:
//       IN lcid            the LCID to make the Win32 call with
//      OUT pOutputStrAry   The output managed string array.
//
////////////////////////////////////////////////////////////////////////
BOOL COMNlsInfo::GetNativeDigitsFromWin32(LPCWSTR locale, PTRARRAYREF * pOutputStrAry, BOOL useUserOverride)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    } CONTRACTL_END;

    WCHAR buffer[11];
    int result = 0;

    DWORD lcType = LOCALE_SNATIVEDIGITS;
    if(!useUserOverride)
    {
        lcType |= LOCALE_NOUSEROVERRIDE;
    }
    result = NewApis::GetLocaleInfoEx(locale, lcType, buffer, 11);
    // Be very unforgiving and only support strings of size 10 plus the NULL
    if (result == 11)
    {
        // Break up the unmanaged ten-character ZLS into what NFI wants (a managed
        // ten-string array).
        //
        // Allocate the array of STRINGREFs.  We don't need to check for null because the GC will throw
        // an OutOfMemoryException if there's not enough memory.
        //
        PTRARRAYREF DigitArray = (PTRARRAYREF) AllocateObjectArray(10, g_pStringClass);

        GCPROTECT_BEGIN(DigitArray);
        for(DWORD i = 0;  i < 10; i++) {
            OBJECTREF o = (OBJECTREF) StringObject::NewString(buffer + i, 1);
            DigitArray->SetAt(i, o);
        }
        GCPROTECT_END();

        _ASSERTE(pOutputStrAry != NULL);
        *pOutputStrAry = DigitArray;
    }

    return (result == 11);
}


////////////////////////////////////////////////////////////////////////
//
// Call the Win32 GetLocaleInfoEx() using the specified lcid and LCTYPE.
// The return value can be INT32 or an allocated managed string object, depending on
// which version's called.
//
// Parameters:
//      OUT pOutputInt32    The output int32 value.
//      OUT pOutputRef      The output string value.
//
////////////////////////////////////////////////////////////////////////
BOOL COMNlsInfo::CallGetLocaleInfoEx(LPCWSTR localeName, int lcType, INT32* pOutputInt32, BOOL useUserOverride)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_ANY;
        NOTHROW;
    } CONTRACTL_END;

    int result = 0;

    _ASSERT((lcType & LOCALE_RETURN_NUMBER) != 0);
    if(!useUserOverride)
    {
        lcType |= LOCALE_NOUSEROVERRIDE;
    }
    result = NewApis::GetLocaleInfoEx(localeName, lcType, (LPWSTR)pOutputInt32, sizeof(*pOutputInt32));

    return (result != 0);
}

BOOL COMNlsInfo::CallGetLocaleInfoEx(LPCWSTR localeName, int lcType, STRINGREF* pOutputStrRef, BOOL useUserOverride)
{
    CONTRACTL
    {
        THROWS;                 // We can throw since we are allocating managed string.
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    WCHAR buffer[LOCALE_NAME_MAX_LENGTH];
    int result = 0;

    _ASSERT((lcType & LOCALE_RETURN_NUMBER) == 0);
    if(!useUserOverride)
    {
        lcType |= LOCALE_NOUSEROVERRIDE;
    }
    result = NewApis::GetLocaleInfoEx(localeName, lcType, buffer, LOCALE_NAME_MAX_LENGTH);

    if (result != 0)
    {
        _ASSERTE(pOutputStrRef != NULL);
        *pOutputStrRef = StringObject::NewString(buffer, result - 1);
    }

    return (result != 0);
}

FCIMPL1(Object*, COMNlsInfo::LCIDToLocaleName, LCID lcid)
{
    FCALL_CONTRACT;

    STRINGREF refRetVal = NULL;

    // The maximum size for locale name is 85 characters.
    WCHAR localeName[LOCALE_NAME_MAX_LENGTH];
    int result = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    // Note that this'll return neutral names (unlike native Vista APIs)
    result = NewApis::LCIDToLocaleName(lcid, localeName, LOCALE_NAME_MAX_LENGTH, 0);

    if (result != 0)
    {
        refRetVal = StringObject::NewString(localeName, result - 1);
    }
    else
    {
        refRetVal = StringObject::GetEmptyString();
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL1(INT32, COMNlsInfo::LocaleNameToLCID, StringObject* localeNameUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    INT32 result = 0;

    // Dereference our string
    STRINGREF localeName = (STRINGREF)localeNameUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(localeName);

    // Note that this'll return neutral names (unlike native Vista APIs)
    result = NewApis::LocaleNameToLCID(localeName->GetBuffer(), 0);

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

////////////////////////////////////////////////////////////////////////
//
// Implementation of CultureInfo.nativeGetNumberFormatInfoValues.
//
// Retrieve NumberFormatInfo (NFI) properties from windows
//
// Parameters:
//      IN/OUT pNumfmtUNSAFE
//                  The pointer of the managed NumberFormatInfo passed
//                  from the managed side.
//                  Note that the native NumberFormatInfo* is defined
//                  in COMNumber.h
// Note:
// Managed string will be allocated and assign to the string fields in
//      the managed NumberFormatInfo passed in pNumftUNSAFE
//
////////////////////////////////////////////////////////////////////////


/*
    This is the list of the data members in the managed NumberFormatInfo and their
    corresponding LCTYPE().

    Win32 GetLocaleInfo() constants             Data members in NumberFormatInfo in the defined order.
    LOCALE_SPOSITIVE                            // String positiveSign
    LOCALE_SNEGATIVE                            // String negativeSign
    LOCALE_SDECIMAL                             // String numberDecimalSeparator
    LOCALE_SGROUPING                            // String numberGroupSeparator
    LOCALE_SMONGROUPING                         // String currencyGroupSeparator
    LOCALE_SMONDECIMALSEP                       // String currencyDecimalSeparator
    LOCALE_SCURRENCY                            // String currencySymbol
    N/A                                         // String ansiCurrencySymbol
    N/A                                         // String nanSymbol
    N/A                                         // String positiveInfinitySymbol
    N/A                                         // String negativeInfinitySymbol
    N/A                                         // String percentDecimalSeparator
    N/A                                         // String percentGroupSeparator
    N/A                                         // String percentSymbol
    N/A                                         // String perMilleSymbol

    N/A                                         // int m_dataItem

    LOCALE_IDIGITS | LOCALE_RETURN_NUMBER,      // int numberDecimalDigits
    LOCALE_ICURRDIGITS | LOCALE_RETURN_NUMBER,  // int currencyDecimalDigits
    LOCALE_ICURRENCY | LOCALE_RETURN_NUMBER,    // int currencyPositivePattern
    LOCALE_INEGCURR | LOCALE_RETURN_NUMBER,      // int currencyNegativePattern
    LOCALE_INEGNUMBER| LOCALE_RETURN_NUMBER,    // int numberNegativePattern
    N/A                                         // int percentPositivePattern
    N/A                                         // int percentNegativePattern
    N/A                                         // int percentDecimalDigits
    N/A                                         // bool isReadOnly=false;
    N/A                                         // internal bool m_useUserOverride;
*/
FCIMPL3(FC_BOOL_RET, COMNlsInfo::nativeGetNumberFormatInfoValues,
        StringObject* localeNameUNSAFE, NumberFormatInfo* pNumfmtUNSAFE, CLR_BOOL useUserOverride) {
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    BOOL ret = TRUE;

    struct _gc
    {
        STRINGREF   localeName;
        STRINGREF   stringResult;
        NUMFMTREF   numfmt;
        PTRARRAYREF tempArray;
    } gc;

    // Dereference our string
    gc.localeName = (STRINGREF)localeNameUNSAFE;
    gc.numfmt = (NUMFMTREF) pNumfmtUNSAFE;
    gc.stringResult = NULL;
    gc.tempArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    StackSString localeNameStackBuffer( gc.localeName->GetBuffer() );
    
    // Calling SString::ConvertToUnicode once
    LPCWSTR pLocaleName = localeNameStackBuffer;
    
    //
    // NOTE: We pass the stringResult allocated in the stack and assign it to the fields
    // in numfmt after calling CallGetLocaleInfo().  The reason for this is that CallGetLocaleInfo()
    // allocates a string object, and it may trigger a GC and cause numfmt to be moved.
    // That's why we use the stringResult allocated in the stack since it will not be moved.
    // After CallGetLocaleInfo(), we know that numfmt will not be moved, and it's safe to assign
    // the stringResult to its field.
    //

    // String values
    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_SPOSITIVESIGN , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sPositive), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }

    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_SNEGATIVESIGN , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sNegative), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }
    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_SDECIMAL , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sNumberDecimal), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }
    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_STHOUSAND , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sNumberGroup), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }
    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_SMONTHOUSANDSEP , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sCurrencyGroup), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }
    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_SMONDECIMALSEP , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sCurrencyDecimal), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }
    if (CallGetLocaleInfoEx(pLocaleName, LOCALE_SCURRENCY , &gc.stringResult, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sCurrency), gc.stringResult, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }


    // Numeric values
    ret &= CallGetLocaleInfoEx(pLocaleName, LOCALE_IDIGITS | LOCALE_RETURN_NUMBER           , &(gc.numfmt->cNumberDecimals), useUserOverride);
    _ASSERT(ret == TRUE);
    ret &= CallGetLocaleInfoEx(pLocaleName, LOCALE_ICURRDIGITS | LOCALE_RETURN_NUMBER       , &(gc.numfmt->cCurrencyDecimals), useUserOverride);
    _ASSERT(ret == TRUE);
    ret &= CallGetLocaleInfoEx(pLocaleName, LOCALE_ICURRENCY | LOCALE_RETURN_NUMBER         , &(gc.numfmt->cPosCurrencyFormat), useUserOverride);
    _ASSERT(ret == TRUE);
    ret &= CallGetLocaleInfoEx(pLocaleName, LOCALE_INEGCURR | LOCALE_RETURN_NUMBER          , &(gc.numfmt->cNegCurrencyFormat), useUserOverride);
    _ASSERT(ret == TRUE);
    ret &= CallGetLocaleInfoEx(pLocaleName, LOCALE_INEGNUMBER| LOCALE_RETURN_NUMBER         , &(gc.numfmt->cNegativeNumberFormat), useUserOverride);
    _ASSERT(ret == TRUE);
    ret &= CallGetLocaleInfoEx(pLocaleName, LOCALE_IDIGITSUBSTITUTION | LOCALE_RETURN_NUMBER, &(gc.numfmt->iDigitSubstitution), useUserOverride);
    _ASSERT(ret == TRUE);

    // LOCALE_SNATIVEDIGITS (gc.tempArray of strings)
    if (GetNativeDigitsFromWin32(pLocaleName, &gc.tempArray, useUserOverride)) {
        SetObjectReference((OBJECTREF*)&(gc.numfmt->sNativeDigits), gc.tempArray, NULL);
    }
    else {
        ret = FALSE;
        _ASSERT(FALSE);
    }

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(ret);
}
FCIMPLEND


////////////////////////////////////////////////////////////////////////
//
// Culture enumeration helper functions
//
////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////
//
// Enum values for System.Globalization.CultureTypes
//
////////////////////////////////////////////////////////////////////////////

// Neutral cultures are cultures like "en", "de", "zh", etc, for enumeration this includes ALL neutrals regardless of other flags
#define CULTURETYPES_NEUTRALCULTURES              0x0001

// Non-netural cultuers.  Examples are "en-us", "zh-tw", etc., for enumeration this includes ALL specifics regardless of other flags
#define CULTURETYPES_SPECIFICCULTURES             0x0002

// Win32 installed cultures in the system and exists in the framework too., this is effectively all cultures
#define CULTURETYPES_INSTALLEDWIN32CULTURES       0x0004

// User defined custom culture
#define CULTURETYPES_USERCUSTOMCULTURE            0x0008

// User defined replacement custom culture.
#define CULTURETYPES_REPLACEMENTCULTURES          0x0010
// [Obsolete("This value has been deprecated.  Please use other values in CultureTypes.")]
// Culture exists in Win32 but not in the Framework. // TODO: All cultures or no cultures?
#define CULTURETYPES_WINDOWSONLYCULTURES          0x0020
// [Obsolete("This value has been deprecated.  Please use other values in CultureTypes.")]
// the language tag match a culture that ships with the .NET framework, effectively all cultures since we get them from windows
#define CULTURETYPES_FRAMEWORKCULTURES            0x0040


const LPWSTR WHIDBEY_FRAMEWORK_CULTURE_LIST [] =
{
    W(""),
    W("af"),
    W("af-za"),
    W("ar"),
    W("ar-ae"),
    W("ar-bh"),
    W("ar-dz"),
    W("ar-eg"),
    W("ar-iq"),
    W("ar-jo"),
    W("ar-kw"),
    W("ar-lb"),
    W("ar-ly"),
    W("ar-ma"),
    W("ar-om"),
    W("ar-qa"),
    W("ar-sa"),
    W("ar-sy"),
    W("ar-tn"),
    W("ar-ye"),
    W("az"),
    W("az-cyrl-az"),
    W("az-latn-az"),
    W("be"),
    W("be-by"),
    W("bg"),
    W("bg-bg"),
    W("ca"),
    W("ca-es"),
    W("cs"),
    W("cs-cz"),
    W("da"),
    W("da-dk"),
    W("de"),
    W("de-at"),
    W("de-ch"),
    W("de-de"),
    W("de-li"),
    W("de-lu"),
    W("dv"),
    W("dv-mv"),
    W("el"),
    W("el-gr"),
    W("en"),
    W("en-029"),
    W("en-au"),
    W("en-bz"),
    W("en-ca"),
    W("en-gb"),
    W("en-ie"),
    W("en-jm"),
    W("en-nz"),
    W("en-ph"),
    W("en-tt"),
    W("en-us"),
    W("en-za"),
    W("en-zw"),
    W("es"),
    W("es-ar"),
    W("es-bo"),
    W("es-cl"),
    W("es-co"),
    W("es-cr"),
    W("es-do"),
    W("es-ec"),
    W("es-es"),
    W("es-gt"),
    W("es-hn"),
    W("es-mx"),
    W("es-ni"),
    W("es-pa"),
    W("es-pe"),
    W("es-pr"),
    W("es-py"),
    W("es-sv"),
    W("es-uy"),
    W("es-ve"),
    W("et"),
    W("et-ee"),
    W("eu"),
    W("eu-es"),
    W("fa"),
    W("fa-ir"),
    W("fi"),
    W("fi-fi"),
    W("fo"),
    W("fo-fo"),
    W("fr"),
    W("fr-be"),
    W("fr-ca"),
    W("fr-ch"),
    W("fr-fr"),
    W("fr-lu"),
    W("fr-mc"),
    W("gl"),
    W("gl-es"),
    W("gu"),
    W("gu-in"),
    W("he"),
    W("he-il"),
    W("hi"),
    W("hi-in"),
    W("hr"),
    W("hr-hr"),
    W("hu"),
    W("hu-hu"),
    W("hy"),
    W("hy-am"),
    W("id"),
    W("id-id"),
    W("is"),
    W("is-is"),
    W("it"),
    W("it-ch"),
    W("it-it"),
    W("ja"),
    W("ja-jp"),
    W("ka"),
    W("ka-ge"),
    W("kk"),
    W("kk-kz"),
    W("kn"),
    W("kn-in"),
    W("ko"),
    W("ko-kr"),
    W("kok"),
    W("kok-in"),
    W("ky"),
    W("ky-kg"),
    W("lt"),
    W("lt-lt"),
    W("lv"),
    W("lv-lv"),
    W("mk"),
    W("mk-mk"),
    W("mn"),
    W("mn-mn"),
    W("mr"),
    W("mr-in"),
    W("ms"),
    W("ms-bn"),
    W("ms-my"),
    W("nb-no"),
    W("nl"),
    W("nl-be"),
    W("nl-nl"),
    W("nn-no"),
    W("no"),
    W("pa"),
    W("pa-in"),
    W("pl"),
    W("pl-pl"),
    W("pt"),
    W("pt-br"),
    W("pt-pt"),
    W("ro"),
    W("ro-ro"),
    W("ru"),
    W("ru-ru"),
    W("sa"),
    W("sa-in"),
    W("sk"),
    W("sk-sk"),
    W("sl"),
    W("sl-si"),
    W("sq"),
    W("sq-al"),
    W("sr"),
    W("sr-cyrl-cs"),
    W("sr-latn-cs"),
    W("sv"),
    W("sv-fi"),
    W("sv-se"),
    W("sw"),
    W("sw-ke"),
    W("syr"),
    W("syr-sy"),
    W("ta"),
    W("ta-in"),
    W("te"),
    W("te-in"),
    W("th"),
    W("th-th"),
    W("tr"),
    W("tr-tr"),
    W("tt"),
    W("tt-ru"),
    W("uk"),
    W("uk-ua"),
    W("ur"),
    W("ur-pk"),
    W("uz"),
    W("uz-cyrl-uz"),
    W("uz-latn-uz"),
    W("vi"),
    W("vi-vn"),
    W("zh-chs"),
    W("zh-cht"),
    W("zh-cn"),
    W("zh-hans"),
    W("zh-hant"),
    W("zh-hk"),
    W("zh-mo"),
    W("zh-sg"),
    W("zh-tw")
};
#define WHIDBEY_FRAMEWORK_CULTURE_LIST_LENGTH (sizeof(WHIDBEY_FRAMEWORK_CULTURE_LIST) / sizeof(WHIDBEY_FRAMEWORK_CULTURE_LIST[0]))

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

BOOL IsWhidbeyFrameworkCulture(__in LPCWSTR lpLocaleString)
{
    int iBottom = 0;
    int iTop = WHIDBEY_FRAMEWORK_CULTURE_LIST_LENGTH - 1;

    // Do a binary search for our name
    while (iBottom <= iTop)
    {
        int     iMiddle = (iBottom + iTop) / 2;
        int     result = NlsCompareInvariantNoCase(lpLocaleString, WHIDBEY_FRAMEWORK_CULTURE_LIST[iMiddle], LOCALE_NAME_MAX_LENGTH, TRUE);
        if (result == 0)
        {
            return TRUE;
        }
        if (result < 0)
        {
            // pLocaleName was < pTest
            iTop = iMiddle - 1;
        }
        else
        {
            // pLocaleName was > pTest
            iBottom = iMiddle + 1;
        }
    }

    return FALSE;
}

// Just Check to see if the OS thinks it is a valid locle
BOOL WINAPI IsOSValidLocaleName(__in LPCWSTR  lpLocaleName, bool bIsNeutralLocale)
{
#ifndef ENABLE_DOWNLEVEL_FOR_NLS
    return ::IsValidLocaleName(lpLocaleName);
#else
    BOOL IsWindows7 = NewApis::IsWindows7Platform();
    // if we're < Win7, we didn't know about neutrals or the invariant.
    if (!IsWindows7 && (bIsNeutralLocale || (lpLocaleName[0] == 0)))
    {
        return false;
    }

    // Work around the name/lcid thingy (can't just link to ::IsValidLocaleName())
    LCID lcid = NewApis::LocaleNameToLCID(lpLocaleName, 0);

    if (IsCustomCultureId(lcid))
    {
        return false;
    }

    if (bIsNeutralLocale)
    {
        // In this case, we're running on Windows 7.
        // For neutral locales, use GetLocaleInfoW.
        // If GetLocaleInfoW works, then the OS knows about it.
        return (::GetLocaleInfoW(lcid, LOCALE_ILANGUAGE, NULL, 0) != 0);
    }

    // This is not a custom locale.
    // Call IsValidLocale() to check if the LCID is installed.
    // IsValidLocale doesn't work for neutral locales.
    return IsValidLocale(lcid, LCID_INSTALLED);
#endif
}


////////////////////////////////////////////////////////////////////////////
//
// Check the dwFlags, which has the 'attributes' of the locale, and decide
// if the locale should be included in the enumeration based on
// the desired CultureTypes.
//
////////////////////////////////////////////////////////////////////////////

BOOL ShouldIncludeByCultureType(INT32 cultureTypes, LPCWSTR lpLocaleString, INT32 dwFlags)
{

    if ((cultureTypes & CULTURETYPES_NEUTRALCULTURES) &&
         ((dwFlags & LOCALE_NEUTRALDATA) || (lpLocaleString[0] == 0))) // Invariant culture get enumerated with the neutrals
    {
        return TRUE;
    }

    if ((cultureTypes & CULTURETYPES_SPECIFICCULTURES) &&
        ((dwFlags & LOCALE_SPECIFICDATA) && (lpLocaleString[0] != 0))) // Invariant culture does not get enumerated with the specifics
    {
        return TRUE;
    }

    if (cultureTypes & CULTURETYPES_INSTALLEDWIN32CULTURES)
    {
        // The user asks for installed Win32 culture, so check
        // if this locale is installed. In W7 and above, when ::IsValidLocaleName()
        // returns true, it means that it is installed.
        // In downlevel (including Vista), we will convert the name to LCID.
        // When the LCID is not a custom locale, we will call ::IsValidLocale(.., LCID_INSTALLED)
        // to verify if the locale is installed.
        // In Vista, we treat custom locale as installed.
        if (IsOSValidLocaleName(lpLocaleString, (dwFlags & LOCALE_NEUTRALDATA) == LOCALE_NEUTRALDATA))
        {
            return TRUE;
        }
    }

    if ((cultureTypes & CULTURETYPES_USERCUSTOMCULTURE) &&
        (dwFlags & LOCALE_SUPPLEMENTAL))
    {
        return TRUE;
    }

    if ((cultureTypes & CULTURETYPES_REPLACEMENTCULTURES) &&
        (dwFlags & LOCALE_REPLACEMENT))
    {
        return TRUE;
    }

    if ((cultureTypes & CULTURETYPES_FRAMEWORKCULTURES) &&
         IsWhidbeyFrameworkCulture(lpLocaleString))
    {
        return TRUE;
    }

    //
    // No need to check CULTURETYPES_WINDOWSONLYCULTURES and CULTURETYPES_FRAMEWORKCULTURES
    // since they are deprecated, and they are handled in the managed code before calling
    // nativeEnumCultureNames.
    //

    return FALSE;
}

////////////////////////////////////////////////////////////////////////////
//
// Struct to hold context to be used in the callback for
// EnumLocaleProcessingCallback
//
////////////////////////////////////////////////////////////////////////////

typedef struct
{
    PTRARRAYREF pCultureNamesArray;
    INT32 count;
    INT32 cultureTypes;
} ENUM_LOCALE_DATA;

////////////////////////////////////////////////////////////////////////////
//
// Callback for NewApis::EnumSystemLocalesEx to count the number of
// locales to be enumerated.
//
////////////////////////////////////////////////////////////////////////////

BOOL CALLBACK EnumLocaleCountCallback(__in_z LPCWSTR lpLocaleString, __in DWORD dwFlags, __in LPARAM lParam)
{
    ENUM_LOCALE_DATA* pData = (ENUM_LOCALE_DATA*)lParam;

    if (ShouldIncludeByCultureType(pData->cultureTypes, lpLocaleString, dwFlags))
    {
        (pData->count)++;
    }
    return TRUE;
}


////////////////////////////////////////////////////////////////////////////
//
// Callback for NewApis::EnumSystemLocalesEx to add the locale name
// into the allocated managed string array.
//
////////////////////////////////////////////////////////////////////////////

BOOL CALLBACK EnumLocaleProcessingCallback(__in_z LPCWSTR lpLocaleString, __in DWORD dwFlags, __in LPARAM lParam)
{
    ENUM_LOCALE_DATA* pData = (ENUM_LOCALE_DATA*)lParam;

    if (ShouldIncludeByCultureType(pData->cultureTypes, lpLocaleString, dwFlags))
    {
        GCX_COOP();

        GCPROTECT_BEGIN(pData->pCultureNamesArray);

        OBJECTREF cultureString = (OBJECTREF) StringObject::NewString(lpLocaleString);
        pData->pCultureNamesArray->SetAt(pData->count, cultureString);
        pData->count++;

        GCPROTECT_END();
    }

    return TRUE;
}


////////////////////////////////////////////////////////////////////////////
//
// Called by CultureData.GetCultures() to enumerate the names of cultures.
// It first calls NewApis::EnumSystemLocalesEx to count the number of
// locales to be enumerated. And it will allocate an managed string
// array with the count. And fill the array with the culture names in
// the 2nd call to NewAPis::EnumSystemLocalesEx.
//
////////////////////////////////////////////////////////////////////////////


int QCALLTYPE COMNlsInfo::nativeEnumCultureNames(INT32 cultureTypes, QCall::ObjectHandleOnStack retStringArray)
{
    CONTRACTL
    {
        QCALL_CHECK;
        // Check CultureTypes.WindowsOnlyCultures and CultureTYpes.FrameworkCultures are deprecated and is
        // handled in the managed code side to provide fallback behavior.
        //PRECONDITION((cultureTypes & (CULTURETYPES_WINDOWSONLYCULTURES | CULTURETYPES_FRAMEWORKCULTURES) == 0));
    } CONTRACTL_END;


    int result;
    DWORD dwFlags = 0;
    PTRARRAYREF cultureNamesArray = NULL;
    ENUM_LOCALE_DATA enumData = { NULL, 0, cultureTypes};

    BEGIN_QCALL;

    //
    // if CultureTypes.FrameworkCulture is specified we'll enumerate all cultures
    // and filter according to the Whidbey framework culture list (for compatibility)
    //

    if (cultureTypes & CULTURETYPES_FRAMEWORKCULTURES)
    {
        dwFlags |= LOCALE_NEUTRALDATA | LOCALE_SPECIFICDATA;
    }

    // Map CultureTypes to Windows enumeration values.
    if (cultureTypes & CULTURETYPES_NEUTRALCULTURES)
    {
        dwFlags |= LOCALE_NEUTRALDATA;
    }

    if (cultureTypes & CULTURETYPES_SPECIFICCULTURES)
    {
        dwFlags |= LOCALE_SPECIFICDATA;
    }

    if (cultureTypes & CULTURETYPES_INSTALLEDWIN32CULTURES)
    {
        // Windows 7 knows about neutrals, whereas Vista and lower don't.
        if (NewApis::IsWindows7Platform())
        {
            dwFlags |= LOCALE_SPECIFICDATA | LOCALE_NEUTRALDATA;
        }
        else
        {
            dwFlags |= LOCALE_SPECIFICDATA;
        }
    }

    dwFlags |= (cultureTypes & CULTURETYPES_USERCUSTOMCULTURE) ? LOCALE_SUPPLEMENTAL: 0;

    // We need special handling for Replacement cultures because Windows does not have a way to enumerate it directly.
    // Replacement locale check will be only used when CultureTypes.SpecificCultures is NOT used.
    dwFlags |= (cultureTypes & CULTURETYPES_REPLACEMENTCULTURES) ? LOCALE_SPECIFICDATA | LOCALE_NEUTRALDATA: 0;


    result = NewApis::EnumSystemLocalesEx((LOCALE_ENUMPROCEX)EnumLocaleCountCallback, dwFlags, (LPARAM)&enumData, NULL) == TRUE ? 1 : 0;

    if (result)
    {

        GCX_COOP();

        GCPROTECT_BEGIN(cultureNamesArray);

        // Now we need to allocate our culture names string array and populate it
        // Get our array object (will throw, don't have to check it)
        cultureNamesArray = (PTRARRAYREF) AllocateObjectArray(enumData.count, g_pStringClass);

        // In the context struct passed to EnumSystemLocalesEx, reset the count and assign the newly allocated string array
        // to hold culture names to be enumerated.
        enumData.count = 0;
        enumData.pCultureNamesArray = cultureNamesArray;

        result = NewApis::EnumSystemLocalesEx((LOCALE_ENUMPROCEX)EnumLocaleProcessingCallback, dwFlags, (LPARAM)&enumData, NULL);

        if (result)
        {
            retStringArray.Set(cultureNamesArray);
        }
        GCPROTECT_END();
    }
    END_QCALL

    return result;

}

//
// InternalCompareString is used in the managed side to handle the synthetic CompareInfo methods (IndexOf, LastIndexOf, IsPrfix, and IsSuffix)
//
INT32 QCALLTYPE COMNlsInfo::InternalCompareString(
    INT_PTR handle,
    INT_PTR handleOrigin,
    LPCWSTR localeName,
    LPCWSTR string1, INT32 offset1, INT32 length1,
    LPCWSTR string2, INT32 offset2, INT32 length2,
    INT32 flags)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(string1));
        PRECONDITION(CheckPointer(string2));
        PRECONDITION(CheckPointer(localeName));
    } CONTRACTL_END;

    INT32 result = 1;
    BEGIN_QCALL;

    handle = EnsureValidSortHandle(handle, handleOrigin, localeName);

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();

    if(!(curDomain->m_bUseOsSorting))
    {
        result = SortVersioning::SortDllCompareString((SortVersioning::PSORTHANDLE) handle, flags, &string1[offset1], length1, &string2[offset2], length2, NULL, 0);
    }
    else if (curDomain->m_pCustomSortLibrary != NULL) {
        result = (curDomain->m_pCustomSortLibrary->pCompareStringEx)(handle != NULL ? NULL : localeName, flags, &string1[offset1], length1, &string2[offset2], length2, NULL, NULL, (LPARAM) handle);
    } 
    else 
#endif
    {
        result = NewApis::CompareStringEx(handle != NULL ? NULL : localeName, flags, &string1[offset1], length1, &string2[offset2], length2,NULL,NULL, (LPARAM) handle);
    }

    switch (result)
    {
        case CSTR_LESS_THAN:
            result = -1;
            break;

        case CSTR_EQUAL:
            result = 0;
            break;

        case CSTR_GREATER_THAN:
            result = 1;
            break;

        case 0:
        default:
            _ASSERTE(!"catastrophic failure calling NewApis::CompareStringEx!  This could be a CultureInfo, RegionInfo, or Calendar bug (bad localeName string) or maybe a GCHole.");
            break;
    }

    END_QCALL;
    return result;
}

////////////////////////////////////////////////////////////////////////////
//
//  UseConstantSpaceHashAlgorithm
//  Check for the DWORD "NetFx45_CultureAwareComparerGetHashCode_LongStrings" CLR config option.
//
// .Net 4.5 introduces an opt-in algorithm for determining the hash code of strings that
// uses a constant amount of memory instead of memory proportional to the size of the string
//
// A non-zero value will enable the new algorithm:
//
// 1) Config file (MyApp.exe.config)
//        <?xml version ="1.0"?>
//        <configuration>
//         <runtime>
//          <NetFx45_CultureAwareComparerGetHashCode_LongStrings enabled="1"/>
//         </runtime>
//        </configuration>
// 2) Environment variable
//        set NetFx45_CultureAwareComparerGetHashCode_LongStrings=1
// 3) RegistryKey
//        [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework]
//        "NetFx45_CultureAwareComparerGetHashCode_LongStrings"=dword:00000001
//
////////////////////////////////////////////////////////////////////////////
BOOL UseConstantSpaceHashAlgorithm()
{
    static bool configChecked = false;
    static BOOL useConstantSpaceHashAlgorithm = FALSE;

    if(!configChecked)
    {
        BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return false);
        useConstantSpaceHashAlgorithm = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NetFx45_CultureAwareComparerGetHashCode_LongStrings) != 0;
        END_SO_INTOLERANT_CODE;

        configChecked = true;
    }
    return useConstantSpaceHashAlgorithm;
}



////////////////////////////////////////////////////////////////////////////
//
//  InternalGetGlobalizedHashCode
//
////////////////////////////////////////////////////////////////////////////
INT32 QCALLTYPE COMNlsInfo::InternalGetGlobalizedHashCode(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName, LPCWSTR string, INT32 length, INT32 dwFlagsIn, BOOL bForceRandomizedHashing, INT64 additionalEntropy)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(localeName));
        PRECONDITION(CheckPointer(string, NULL_OK));
    } CONTRACTL_END;

    INT32  iReturnHash  = 0;
    BEGIN_QCALL;

    handle = EnsureValidSortHandle(handle, handleOrigin, localeName);
    int byteCount = 0;

    //
    //  Make sure there is a string.
    //
    if (!string) {
        COMPlusThrowArgumentNull(W("string"),W("ArgumentNull_String"));
    }

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();
#endif // FEATURE_CORECLR

    if(length > 0 && UseConstantSpaceHashAlgorithm() 
    // Note that we can't simply do the hash without the entropy and then try to add it after the fact we need the hash function itself to pass entropy to its inputs.
#ifdef FEATURE_RANDOMIZED_STRING_HASHING
         && !bForceRandomizedHashing
#ifndef FEATURE_CORECLR
         && !curDomain->m_pNlsHashProvider->GetUseRandomHashing()
#else
         && !COMNlsHashProvider::s_NlsHashProvider.GetUseRandomHashing()
#endif // FEATURE_CORECLR
#endif // FEATURE_RANDOMIZED_STRING_HASHING
       )
    {
#ifndef FEATURE_CORECLR
        if(!(curDomain->m_bUseOsSorting))
        {
            iReturnHash=SortVersioning::SortDllGetHashCode((SortVersioning::PSORTHANDLE) handle, dwFlagsIn, string, length, NULL, 0);
        }
        else
#endif
        {
            int iRes = 0;
            int iHashValue = 0;

#ifndef FEATURE_CORECLR
            if (curDomain->m_pCustomSortLibrary != NULL)
            {
                iRes = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(localeName, dwFlagsIn | LCMAP_HASH, string, length, (LPWSTR) &iHashValue, sizeof(INT32), NULL, NULL, 0);
            }
            else
#endif
            {
                iRes = NewApis::LCMapStringEx(localeName, dwFlagsIn | LCMAP_HASH, string, length, (LPWSTR) &iHashValue, sizeof(INT32), NULL, NULL, 0);
            }

            if(iRes != 0)
            {
                iReturnHash = iHashValue;
            }
        }
    }

    if(iReturnHash == 0)
    {
        DWORD dwFlags = (LCMAP_SORTKEY | dwFlagsIn);

        //
        // Caller has already verified that the string is not of zero length
        //
        // Assert if we might hit an AV in LCMapStringEx for the invariant culture.
        _ASSERTE(length > 0 || (dwFlags & LCMAP_LINGUISTIC_CASING) == 0);
#ifndef FEATURE_CORECLR
        if(!(curDomain->m_bUseOsSorting))
        {
            byteCount=SortVersioning::SortDllGetSortKey((SortVersioning::PSORTHANDLE) handle, dwFlagsIn, string, length, NULL, 0, NULL, 0);
        }
        else if (curDomain->m_pCustomSortLibrary != NULL)
        {
            byteCount = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : localeName, dwFlags, string, length, NULL, 0, NULL, NULL, (LPARAM) handle);
        }
        else
#endif
        {
            byteCount=NewApis::LCMapStringEx(handle != NULL ? NULL : localeName, dwFlags, string, length, NULL, 0, NULL, NULL, (LPARAM) handle);
        }

        //A count of 0 indicates that we either had an error or had a zero length string originally.
        if (byteCount==0) {
            COMPlusThrow(kArgumentException, W("Arg_MustBeString"));
        }

        // We used to use a NewArrayHolder here, but it turns out that hurts our large # process
        // scalability in ASP.Net hosting scenarios, using the quick bytes instead mostly stack
        // allocates and ups throughput by 8% in 100 process case, 5% in 1000 process case
        {
            CQuickBytesSpecifySize<MAX_STRING_VALUE * sizeof(WCHAR)> qbBuffer;
            BYTE* pByte = (BYTE*)qbBuffer.AllocThrows(byteCount);

#ifndef FEATURE_CORECLR
            if(!(curDomain->m_bUseOsSorting))
            {
                SortVersioning::SortDllGetSortKey((SortVersioning::PSORTHANDLE) handle, dwFlagsIn, string, length, pByte, byteCount, NULL, 0);
            }
            else if(curDomain->m_pCustomSortLibrary != NULL)
            {
                (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : localeName, dwFlags, string, length, (LPWSTR)pByte, byteCount, NULL, NULL, (LPARAM) handle);
            }
            else
#endif
            {
                NewApis::LCMapStringEx(handle != NULL ? NULL : localeName, dwFlags, string, length, (LPWSTR)pByte, byteCount, NULL,NULL, (LPARAM) handle);
            }

#ifndef FEATURE_CORECLR
            iReturnHash = curDomain->m_pNlsHashProvider->HashSortKey(pByte, byteCount, bForceRandomizedHashing, additionalEntropy);
#else
            iReturnHash = COMNlsHashProvider::s_NlsHashProvider.HashSortKey(pByte, byteCount, bForceRandomizedHashing, additionalEntropy);
#endif // FEATURE_CORECLR
        }
    }
    END_QCALL;
    return(iReturnHash);
}

#ifndef FEATURE_CORECLR // FCalls used by System.TimeZone

FCIMPL0(LONG, COMNlsInfo::nativeGetTimeZoneMinuteOffset)
{
    FCALL_CONTRACT;

    TIME_ZONE_INFORMATION timeZoneInfo;

    GetTimeZoneInformation(&timeZoneInfo);

    //
    // In Win32, UTC = local + offset.  So for Pacific Standard Time, offset = 8.
    // In NLS+, Local time = UTC + offset. So for PST, offset = -8.
    // So we have to reverse the sign here.
    //
    return (timeZoneInfo.Bias * -1);
}
FCIMPLEND

FCIMPL0(Object*, COMNlsInfo::nativeGetStandardName)
{
    FCALL_CONTRACT;

    STRINGREF refRetVal = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refRetVal);

    TIME_ZONE_INFORMATION timeZoneInfo;
    GetTimeZoneInformation(&timeZoneInfo);

    refRetVal = StringObject::NewString(timeZoneInfo.StandardName);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL0(Object*, COMNlsInfo::nativeGetDaylightName)
{
    FCALL_CONTRACT;

    STRINGREF refRetVal = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refRetVal);

    TIME_ZONE_INFORMATION timeZoneInfo;
    GetTimeZoneInformation(&timeZoneInfo);
    // Instead of returning null when daylight saving is not used, now we return the same result as the OS.
    //In this case, if daylight saving time is used, the standard name is returned.

#if 0
    if (result == TIME_ZONE_ID_UNKNOWN || timeZoneInfo.DaylightDate.wMonth == 0) {
        // If daylight saving time is not used in this timezone, return null.
        //
        // Windows NT/2000: TIME_ZONE_ID_UNKNOWN is returned if daylight saving time is not used in
        // the current time zone, because there are no transition dates.
        //
        // For Windows 9x, a zero in the wMonth in DaylightDate means daylight saving time
        // is not specified.
        //
        // If the current timezone uses daylight saving rule, but user unchekced the
        // "Automatically adjust clock for daylight saving changes", the value
        // for DaylightBias will be 0.
        return (I2ARRAYREF)NULL;
    }
#endif  // 0

    refRetVal = StringObject::NewString(timeZoneInfo.DaylightName);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL1(Object*, COMNlsInfo::nativeGetDaylightChanges, int year)
{
    FCALL_CONTRACT;

    I2ARRAYREF pResultArray = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(pResultArray);

    TIME_ZONE_INFORMATION timeZoneInfo;
    DWORD result = GetTimeZoneInformation(&timeZoneInfo);

    if (result == TIME_ZONE_ID_UNKNOWN || timeZoneInfo.DaylightBias == 0
        || timeZoneInfo.DaylightDate.wMonth == 0
        ) {
        // If daylight saving time is not used in this timezone, return null.
        //
        // If the current timezone uses daylight saving rule, but user unchekced the
        // "Automatically adjust clock for daylight saving changes", the value
        // for DaylightBias will be 0.
        goto lExit;
    }

    pResultArray = (I2ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_I2, 17);

    //
    // The content of timeZoneInfo.StandardDate is 8 words, which
    // contains year, month, day, dayOfWeek, hour, minute, second, millisecond.
    //
    memcpyNoGCRefs(pResultArray->m_Array,
            (LPVOID)&timeZoneInfo.DaylightDate,
            8 * sizeof(INT16));

    //
    // The content of timeZoneInfo.DaylightDate is 8 words, which
    // contains year, month, day, dayOfWeek, hour, minute, second, millisecond.
    //
    memcpyNoGCRefs(((INT16*)pResultArray->m_Array) + 8,
            (LPVOID)&timeZoneInfo.StandardDate,
            8 * sizeof(INT16));

    ((INT16*)pResultArray->m_Array)[16] = (INT16)timeZoneInfo.DaylightBias * -1;

lExit: ;
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(pResultArray);
}
FCIMPLEND

#endif // FEATURE_CORECLR

inline BOOL IsInvariantLocale(STRINGREF localeName)
{
   return localeName->GetStringLength() == 0;
}

// InternalChangeCaseChar
//
// Call LCMapStringEx with a char to make it upper or lower case
// Note that if the locale is English or Invariant we'll try just mapping it if its < 0x7f
FCIMPL5(FC_CHAR_RET, COMNlsInfo::InternalChangeCaseChar,
        INT_PTR handle, // optional sort handle
        INT_PTR handleOrigin, StringObject* localeNameUNSAFE, CLR_CHAR wch, CLR_BOOL bIsToUpper)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    CLR_CHAR retVal = '\0';
    int ret_LCMapStringEx = -1;

    // Dereference our string
    STRINGREF localeName(localeNameUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(localeName);

    BOOL isInvariantLocale = IsInvariantLocale(localeName);
    // Check for Invariant to avoid A/V in LCMapStringEx
    DWORD linguisticCasing = (isInvariantLocale) ? 0 : LCMAP_LINGUISTIC_CASING;

    handle = EnsureValidSortHandle(handle, handleOrigin, localeName->GetBuffer());

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();

    //For a versioned sort, Invariant should still use the OS
    if(!(curDomain->m_bUseOsSorting) && !isInvariantLocale)
    {
        ret_LCMapStringEx = SortVersioning::SortDllChangeCase((SortVersioning::PSORTHANDLE) handle,
                                    bIsToUpper?LCMAP_UPPERCASE | linguisticCasing:
                                               LCMAP_LOWERCASE | linguisticCasing,
                                    &wch,
                                    1,
                                    &retVal,
                                    1,
                                    NULL, 0);
    }
    else if(curDomain->m_pCustomSortLibrary != NULL)
    {
        ret_LCMapStringEx = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : localeName->GetBuffer(),
                                   bIsToUpper?LCMAP_UPPERCASE | linguisticCasing:
                                              LCMAP_LOWERCASE | linguisticCasing,
                                   &wch,
                                   1,
                                   &retVal,
                                   1,
                                   NULL,
                                   NULL,
                                   (LPARAM) handle);
    }
    else
#endif    
    {
        ret_LCMapStringEx = NewApis::LCMapStringEx(handle != NULL ? NULL : localeName->GetBuffer(),
                                   bIsToUpper?LCMAP_UPPERCASE | linguisticCasing:
                                              LCMAP_LOWERCASE | linguisticCasing,
                                   &wch,
                                   1,
                                   &retVal,
                                   1,
                                   NULL,
                                   NULL,
                                   (LPARAM) handle);
    }

    if (0 == ret_LCMapStringEx)
    {
        // return value of 0 indicates failure and error value is supposed to be set.
        // shouldn't ever really happen
        _ASSERTE(!"catastrophic failure calling NewApis::InternalChangeCaseChar!  This could be a CultureInfo or CompareInfo bug (bad localeName string) or maybe a GCHole.");
    }

    HELPER_METHOD_FRAME_END(); // localeName is now unprotected
    return retVal;
}
FCIMPLEND

// InternalChangeCaseString
//
// Call LCMapStringEx with a string to make it upper or lower case
// Note that if the locale is English or Invariant we'll try just mapping it if its < 0x7f
//
// We typically expect the output string to be the same size as the input.  If not
// we have to count, reallocate the output buffer, and try again.
FCIMPL5(Object*, COMNlsInfo::InternalChangeCaseString,
        INT_PTR handle, // optional sort handle
        INT_PTR handleOrigin, StringObject* localeNameUNSAFE, StringObject* pStringUNSAFE, CLR_BOOL bIsToUpper)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pStringUNSAFE));
        PRECONDITION(CheckPointer(localeNameUNSAFE));
    } CONTRACTL_END;

    struct _gc
    {
        STRINGREF pResult;
        STRINGREF pString;
        STRINGREF pLocale;
    } gc;

    gc.pResult = NULL;
    gc.pString = ObjectToSTRINGREF(pStringUNSAFE);
    gc.pLocale = ObjectToSTRINGREF(localeNameUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc)

    handle = EnsureValidSortHandle(handle, handleOrigin, gc.pLocale->GetBuffer());

    //
    //  Get the length of the string.
    //
    int nLengthInput = gc.pString->GetStringLength();
    int nLengthOutput = nLengthInput; //  initially we assume the string length does not change.

    BOOL isInvariantLocale = IsInvariantLocale(gc.pLocale);
    // Check for Invariant to avoid A/V in LCMapStringEx
    DWORD linguisticCasing = (isInvariantLocale) ? 0 : LCMAP_LINGUISTIC_CASING;
    // Check for Invariant to avoid A/V in LCMapStringEx

    //
    //  Check if we have the empty string.
    //
    if (nLengthInput == 0)
    {
        gc.pResult = ObjectToSTRINGREF(gc.pString);
    }
    else
    {
        //
        //  Create the result string.
        //
        gc.pResult = StringObject::NewString(nLengthOutput);
        LPWSTR pResultStr = gc.pResult->GetBuffer();

        int result;
#ifndef FEATURE_CORECLR
        AppDomain* curDomain = GetAppDomain();

        //Invariant should always use OS
        if(!(curDomain->m_bUseOsSorting) && !isInvariantLocale)
        {
            result = SortVersioning::SortDllChangeCase((SortVersioning::PSORTHANDLE) handle,
                                        bIsToUpper?LCMAP_UPPERCASE | linguisticCasing:
                                                   LCMAP_LOWERCASE | linguisticCasing,
                                        gc.pString->GetBuffer(),
                                        nLengthInput,
                                        pResultStr,
                                        nLengthOutput,
                                        NULL, 0);
        }
        else if(curDomain->m_pCustomSortLibrary != NULL)
        {
            result = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : (gc.pLocale)->GetBuffer(),
                                       bIsToUpper?LCMAP_UPPERCASE | linguisticCasing :
                                                  LCMAP_LOWERCASE | linguisticCasing,
                                       gc.pString->GetBuffer(),
                                       nLengthInput,
                                       pResultStr,
                                       nLengthOutput,
                                       NULL,
                                       NULL,
                                       (LPARAM) handle);
        }
        else
#endif
        {
            result = NewApis::LCMapStringEx(handle != NULL ? NULL : (gc.pLocale)->GetBuffer(),
                                       bIsToUpper?LCMAP_UPPERCASE | linguisticCasing :
                                                  LCMAP_LOWERCASE | linguisticCasing,
                                       gc.pString->GetBuffer(),
                                       nLengthInput,
                                       pResultStr,
                                       nLengthOutput,
                                       NULL,
                                       NULL,
                                       (LPARAM) handle);
        }

        if(0 == result)
        {
            // Failure: Detect if that's due to insufficient buffer
            if (GetLastError()!= ERROR_INSUFFICIENT_BUFFER)
            {
                ThrowLastError();
            }
            // need to update buffer
#ifndef FEATURE_CORECLR
            //Invariant should always use OS
            if(!(curDomain->m_bUseOsSorting) && !IsInvariantLocale(gc.pLocale))
            {
                nLengthOutput = SortVersioning::SortDllChangeCase((SortVersioning::PSORTHANDLE) handle,
                                            bIsToUpper?LCMAP_UPPERCASE | linguisticCasing:
                                                       LCMAP_LOWERCASE | linguisticCasing,
                                            gc.pString->GetBuffer(),
                                            nLengthInput,
                                            NULL,
                                            0,
                                            NULL, 0);
            }
            else if(curDomain->m_pCustomSortLibrary != NULL)
            {
                nLengthOutput = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : (gc.pLocale)->GetBuffer(),
                                                        bIsToUpper?LCMAP_UPPERCASE | linguisticCasing :
                                                                   LCMAP_LOWERCASE | linguisticCasing,
                                                        gc.pString->GetBuffer(),
                                                        nLengthInput,
                                                        NULL,
                                                        0,
                                                        NULL,
                                                        NULL,
                                                        (LPARAM) handle);
            }
            else
#endif
            {
                nLengthOutput = NewApis::LCMapStringEx(handle != NULL ? NULL : (gc.pLocale)->GetBuffer(),
                                                        bIsToUpper?LCMAP_UPPERCASE | linguisticCasing :
                                                                   LCMAP_LOWERCASE | linguisticCasing,
                                                        gc.pString->GetBuffer(),
                                                        nLengthInput,
                                                        NULL,
                                                        0,
                                                        NULL,
                                                        NULL,
                                                        (LPARAM) handle);
            }
            if (nLengthOutput == 0)
            {
                // return value of 0 indicates failure and error value is supposed to be set.
                // shouldn't ever really happen
                _ASSERTE(!"catastrophic failure calling NewApis::InternalChangeCaseString!  This could be a CultureInfo or CompareInfo bug (bad localeName string) or maybe a GCHole.");
            }
            _ASSERTE(nLengthOutput > 0);
            // NOTE: The length of the required buffer does not include the terminating null character.
            // So it can be used as-is for our calculations -- the length we pass in to NewString also does
            // not include the terminating null character.
            // MSDN documentation could be interpreted to mean that the length returned includes the terminating
            // NULL character, but that's not the case.

            // NOTE: Also note that we let the GC take care of the previously allocated pResult.

            gc.pResult = StringObject::NewString(nLengthOutput);
            pResultStr = gc.pResult->GetBuffer();
#ifndef FEATURE_CORECLR
            //Invariant should always use OS
            if(!(curDomain->m_bUseOsSorting) && !IsInvariantLocale(gc.pLocale))
            {
                result = SortVersioning::SortDllChangeCase((SortVersioning::PSORTHANDLE) handle,
                                            bIsToUpper?LCMAP_UPPERCASE | linguisticCasing:
                                                       LCMAP_LOWERCASE | linguisticCasing,
                                            gc.pString->GetBuffer(),
                                            nLengthInput,
                                            pResultStr,
                                            nLengthOutput,
                                            NULL, 0);
            }
            else if(curDomain->m_pCustomSortLibrary != NULL)
            {
                result = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : (gc.pLocale)->GetBuffer(),
                                           bIsToUpper?LCMAP_UPPERCASE | linguisticCasing :
                                                      LCMAP_LOWERCASE | linguisticCasing,
                                           gc.pString->GetBuffer(),
                                           nLengthInput,
                                           pResultStr,
                                           nLengthOutput,
                                           NULL,
                                           NULL,
                                           (LPARAM) handle);
            }
            else
#endif
            {
                result = NewApis::LCMapStringEx(handle != NULL ? NULL : (gc.pLocale)->GetBuffer(),
                                           bIsToUpper?LCMAP_UPPERCASE | linguisticCasing :
                                                      LCMAP_LOWERCASE | linguisticCasing,
                                           gc.pString->GetBuffer(),
                                           nLengthInput,
                                           pResultStr,
                                           nLengthOutput,
                                           NULL,
                                           NULL,
                                           (LPARAM) handle);
            }

            if(0 == result)
            {
                // return value of 0 indicates failure and error value is supposed to be set.
                // shouldn't ever really happen
                _ASSERTE(!"catastrophic failure calling NewApis::InternalChangeCaseString!  This could be a CultureInfo or CompareInfo bug (bad localeName string) or maybe a GCHole.");
            }
        }

        pResultStr[nLengthOutput] = 0;
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.pResult);
}
FCIMPLEND

/*================================InternalGetCaseInsHash================================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
FCIMPL6(INT32, COMNlsInfo::InternalGetCaseInsHash,
        INT_PTR handle, // optional sort handle
        INT_PTR handleOrigin, StringObject* localeNameUNSAFE, LPVOID pvStrA, CLR_BOOL bForceRandomizedHashing, INT64 additionalEntropy)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(localeNameUNSAFE));
        PRECONDITION(CheckPointer(pvStrA));
    } CONTRACTL_END;

    STRINGREF localeName = ObjectToSTRINGREF(localeNameUNSAFE);
    STRINGREF strA;

    INT32 result;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException))

    *((LPVOID *)&strA)=pvStrA;

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();
#endif

    //
    // If we know that we don't have any high characters (the common case) we can
    // call a hash function that knows how to do a very fast case conversion.  If
    // we find characters above 0x80, it's much faster to convert the entire string
    // to Uppercase and then call the standard hash function on it.
    //
    // TODO: NLS Arrowhead -We aren't consistent with the fast casing cultures (any en?  fr? de?)
    if (IsCultureEnglishOrInvariant((localeName)->GetBuffer()) &&           // If we're en-US or Invariant
        ((IS_STRING_STATE_UNDETERMINED(strA->GetHighCharState()) &&          // and we're undetermined
           IS_FAST_CASING(strA->InternalCheckHighChars())) ||       // and its fast casing when determined
        IS_FAST_CASING(strA->GetHighCharState())))                         // or we're fast casing that's already determined
    {
        // Notice that for Turkish and Azeri we don't get here and shouldn't use this
        // fast path because of their special Latin casing rules.
#ifndef FEATURE_CORECLR
        result = curDomain->m_pNlsHashProvider->HashiStringKnownLower80(strA->GetBuffer(), strA->GetStringLength(), bForceRandomizedHashing, additionalEntropy);
#else
        result = COMNlsHashProvider::s_NlsHashProvider.HashiStringKnownLower80(strA->GetBuffer(), strA->GetStringLength(), bForceRandomizedHashing, additionalEntropy);
#endif // FEATURE_CORECLR
    }
    else
    {
        handle = EnsureValidSortHandle(handle, handleOrigin, localeName->GetBuffer());

        // Make it upper case
        CQuickBytes newBuffer;
        INT32 length = strA->GetStringLength();
        WCHAR *pNewStr = (WCHAR *)newBuffer.AllocThrows((length + 1) * sizeof(WCHAR));

        // Work around an A/V in LCMapStringEx for the invariant culture.
        // Revisit this after Vista SP2 has been deployed everywhere. 
        DWORD linguisticCasing = 0;
        if (localeName->GetStringLength() > 0)  // if not the invariant culture...
        {
            linguisticCasing = LCMAP_LINGUISTIC_CASING;
        }

        int lcmapResult;
#ifndef FEATURE_CORECLR
        if(!(curDomain->m_bUseOsSorting))
        {
            lcmapResult = SortVersioning::SortDllChangeCase((SortVersioning::PSORTHANDLE) handle,
                                                    LCMAP_UPPERCASE | linguisticCasing,
                                                    strA->GetBuffer(),
                                                    length,
                                                    pNewStr,
                                                    length,
                                                    NULL, 0);
        }
        else if(curDomain->m_pCustomSortLibrary != NULL)
        {
            lcmapResult = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : localeName->GetBuffer(),
                                                   LCMAP_UPPERCASE | linguisticCasing,
                                                   strA->GetBuffer(),
                                                   length,
                                                   pNewStr,
                                                   length,
                                                   NULL, NULL, (LPARAM) handle);
        }
        else
#endif
        {
            lcmapResult = NewApis::LCMapStringEx(handle != NULL ? NULL : localeName->GetBuffer(),
                                                   LCMAP_UPPERCASE | linguisticCasing,
                                                   strA->GetBuffer(),
                                                   length,
                                                   pNewStr,
                                                   length,
                                                   NULL, NULL, (LPARAM) handle);
        }

        if (lcmapResult == 0)
        {
            // return value of 0 indicates failure and error value is supposed to be set.
            // shouldn't ever really happen
            _ASSERTE(!"catastrophic failure calling NewApis::InternalGetCaseInsHash!  This could be a CultureInfo or CompareInfo bug (bad localeName string) or maybe a GCHole.");
        }
        pNewStr[length]='\0';

        // Get hash for the upper case of the new string

#ifndef FEATURE_CORECLR
        result = curDomain->m_pNlsHashProvider->HashString(pNewStr, length, (BOOL)bForceRandomizedHashing, additionalEntropy);
#else
        result = COMNlsHashProvider::s_NlsHashProvider.HashString(pNewStr, length, (BOOL)bForceRandomizedHashing, additionalEntropy);
#endif // FEATURE_CORECLR
    }

    END_SO_INTOLERANT_CODE

    return result;
}
FCIMPLEND

// Fast path for finding a String using OrdinalIgnoreCase rules
// returns true if the fast path succeeded, with foundIndex set to the location where the String was found or -1
// Returns false when FindStringOrdinal isn't handled (we don't have our own general version of this function to fall back on)
// Note for future optimizations: kernel32!FindStringOrdinal(ignoreCase=TRUE) uses per-character table lookup
// to map to upper case before comparison, but isn't otherwise optimized
BOOL QCALLTYPE COMNlsInfo::InternalTryFindStringOrdinalIgnoreCase(
    __in                   DWORD       dwFindNLSStringFlags, // mutually exclusive flags: FIND_FROMSTART, FIND_STARTSWITH, FIND_FROMEND, FIND_ENDSWITH
    __in_ecount(cchSource) LPCWSTR     lpStringSource,       // the string we search in
    __in                   int         cchSource,            // number of characters lpStringSource after sourceIndex
    __in                   int         sourceIndex,          // index from where the search will start in lpStringSource
    __in_ecount(cchValue)  LPCWSTR     lpStringValue,        // the string we search for
    __in                   int         cchValue,
    __out                  int*        foundIndex)           // the index in lpStringSource where we found lpStringValue
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(lpStringSource != NULL);
        PRECONDITION(lpStringValue != NULL);
        PRECONDITION(cchSource>=0);
        PRECONDITION(cchValue>=0);
        PRECONDITION((dwFindNLSStringFlags & FIND_NLS_STRING_FLAGS_NEGATION) == 0);
    } CONTRACTL_END;

    BOOL result = FALSE;

    BEGIN_QCALL;

    LPCWSTR lpSearchStart = NULL;
    if (dwFindNLSStringFlags & (FIND_FROMEND | FIND_ENDSWITH))
    {
        lpSearchStart = &lpStringSource[sourceIndex - cchSource + 1];
    }
    else {
        lpSearchStart = &lpStringSource[sourceIndex];
    }
#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();

    // Check if the default sorting is overridden
    if (curDomain->m_pCustomSortLibrary != NULL)
    {
        *foundIndex = (curDomain->m_pCustomSortLibrary->pFindStringOrdinal)(
            dwFindNLSStringFlags,
            lpSearchStart,
            cchSource,
            lpStringValue,
            cchValue,
            TRUE);
        result = TRUE;
    }
    else
#endif
    {
#ifndef FEATURE_CORESYSTEM
        // kernel function pointer
        typedef int (WINAPI *PFNFindStringOrdinal)(DWORD, LPCWSTR, INT, LPCWSTR, INT, BOOL);
        static PFNFindStringOrdinal FindStringOrdinal = NULL;

        // initizalize kernel32!FindStringOrdinal
        if (FindStringOrdinal == NULL)
        {
            PFNFindStringOrdinal result  = NULL;

            HMODULE hMod=WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
            if(hMod != NULL)
                result=(PFNFindStringOrdinal)GetProcAddress(hMod,"FindStringOrdinal");
            
            FindStringOrdinal = (result != NULL) ? result : (PFNFindStringOrdinal)-1;
        }

        // call into the kernel
        if (FindStringOrdinal != (PFNFindStringOrdinal)-1)
#endif
        {
            *foundIndex = FindStringOrdinal(
                dwFindNLSStringFlags,
                lpSearchStart,
                cchSource,
                lpStringValue,
                cchValue,
                TRUE);
            result = TRUE;
        }
    }
    // if we found the pattern string, fixup the index before we return
    if (*foundIndex >= 0)
    {
        if (dwFindNLSStringFlags & (FIND_FROMEND | FIND_ENDSWITH))
            *foundIndex += (sourceIndex - cchSource + 1);
        else
            *foundIndex += sourceIndex;
    }
    END_QCALL;

    return result;
}


// InternalCompareStringOrdinalIgnoreCase
//
// Call ::CompareStringOrdinal for native ordinal behavior
INT32 QCALLTYPE COMNlsInfo::InternalCompareStringOrdinalIgnoreCase(
    LPCWSTR string1, INT32 index1,
    LPCWSTR string2, INT32 index2,
    INT32 length1,
    INT32 length2)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(string1));
        PRECONDITION(CheckPointer(string2));
    } CONTRACTL_END;

    INT32 result = 0;

    BEGIN_QCALL;
    //
    //  Get the arguments.
    //  We assume the caller checked them before calling us
    //

    // We don't allow the -1 that native code allows
    _ASSERT(length1 >= 0);
    _ASSERT(length2 >= 0);

    // Do the comparison
#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();
    
    if (curDomain->m_pCustomSortLibrary != NULL) {
        result = (curDomain->m_pCustomSortLibrary->pCompareStringOrdinal)(string1 + index1, length1, string2 + index2, length2, TRUE);
    } 
    else 
#endif
    {
        result = NewApis::CompareStringOrdinal(string1 + index1, length1, string2 + index2, length2, TRUE);
    }

    // The native call shouldn't fail
    _ASSERT(result != 0);
    if (result == 0)
    {
        // return value of 0 indicates failure and error value is supposed to be set.
        // shouldn't ever really happen
        _ASSERTE(!"catastrophic failure calling NewApis::CompareStringOrdinal!  This is usually due to bad arguments.");
    }

    // Adjust the result to the expected -1, 0, 1 result
    result -= 2;

    END_QCALL;

    return result;
}

/**
 * This function returns a pointer to this table that we use in System.Globalization.EncodingTable.
 * No error checking of any sort is performed.  Range checking is entirely the responsibility of the managed
 * code.
 */
FCIMPL0(EncodingDataItem *, COMNlsInfo::nativeGetEncodingTableDataPointer)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    return (EncodingDataItem *)EncodingDataTable;
}
FCIMPLEND

/**
 * This function returns a pointer to this table that we use in System.Globalization.EncodingTable.
 * No error checking of any sort is performed.  Range checking is entirely the responsibility of the managed
 * code.
 */
FCIMPL0(CodePageDataItem *, COMNlsInfo::nativeGetCodePageTableDataPointer)
{
    LIMITED_METHOD_CONTRACT;

    STATIC_CONTRACT_SO_TOLERANT;

    return ((CodePageDataItem*) CodePageDataTable);
}
FCIMPLEND


#ifndef FEATURE_CORECLR
//
// Normalization
//

FCIMPL6(int, COMNlsInfo::nativeNormalizationNormalizeString,
            int NormForm, int& iError,
            StringObject* inChars, int inLength,
            CHARArray* outChars, int outLength )
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(inChars));
        PRECONDITION(CheckPointer(outChars, NULL_OK));
    } CONTRACTL_END;

    // Dereference our string
    STRINGREF inString(inChars);
    LPWSTR inCharsBuffer = inString->GetBuffer();

    CHARARRAYREF outCharArray(outChars);
    LPWSTR outCharsBuffer = (outCharArray != NULL) ? ((LPWSTR) (outCharArray->GetDirectPointerToNonObjectElements())) : NULL;

    // The OS APIs do not always set last error in success, so we have to do it explicitly
    SetLastError(ERROR_SUCCESS);

    int iResult = m_pfnNormalizationNormalizeStringFunc(
            NormForm, inCharsBuffer, inLength, outCharsBuffer, outLength);

    // Get our error if necessary
    if (iResult <= 0)
    {
        // if the length is <= 0 there was an error
        iError = GetLastError();

        // Go ahead and return positive lengths/indexes so we don't get confused
        iResult = -iResult;
    }
    else
    {
        iError = 0; // ERROR_SUCCESS
    }

    return iResult;
}
FCIMPLEND

FCIMPL4( FC_BOOL_RET, COMNlsInfo::nativeNormalizationIsNormalizedString,
            int NormForm, int& iError,
            StringObject* chars, int inLength )
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(chars));
    } CONTRACTL_END;

    STRINGREF inString(chars);
    LPWSTR charsBuffer = inString->GetBuffer();

    // The OS APIs do not always set last error in success, so we have to do it explicitly
    SetLastError(ERROR_SUCCESS);

    // Ask if its normalized
    BOOL bResult = m_pfnNormalizationIsNormalizedStringFunc( NormForm, charsBuffer, inLength);

    // May need an error
    if (bResult == false)
    {
        // If its false there may have been an error
        iError = GetLastError();
    }
    else
    {
        iError = 0; // ERROR_SUCCESS
    }

    FC_RETURN_BOOL(bResult);
}
FCIMPLEND

void QCALLTYPE COMNlsInfo::nativeNormalizationInitNormalization(int NormForm, BYTE* pTableData)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (m_hNormalization == NULL)
    {
        HMODULE hNormalization = NULL;

        if (pTableData == NULL)
        {
            // Use OS implementation
            hNormalization = GetModuleHandleW(W("kernel32.dll"));
            if (!hNormalization)
               ThrowLastError();
        }
        else
        {
            HRESULT hr = g_pCLRRuntime->LoadLibrary(NORMALIZATION_DLL, &hNormalization);
            if (FAILED(hr))
                ThrowHR(hr);
        }

        _ASSERTE(hNormalization != NULL);
        m_hNormalization = hNormalization;
    }

    if (m_pfnNormalizationIsNormalizedStringFunc == NULL)
    {
        FARPROC pfn = GetProcAddress(m_hNormalization, "IsNormalizedString");
        if (pfn == NULL)
            ThrowLastError();
        m_pfnNormalizationIsNormalizedStringFunc = (PFN_NORMALIZATION_IS_NORMALIZED_STRING)pfn;
    }

    if (m_pfnNormalizationNormalizeStringFunc == NULL)
    {
        FARPROC pfn = GetProcAddress(m_hNormalization, "NormalizeString");
        if (pfn == NULL)
            ThrowLastError();
        m_pfnNormalizationNormalizeStringFunc = (PFN_NORMALIZATION_NORMALIZE_STRING)pfn;
    }

    if (pTableData != NULL)
    {
        if (m_pfnNormalizationInitNormalizationFunc == NULL)
        {
            FARPROC pfn = GetProcAddress(m_hNormalization, "InitNormalization");
            if (pfn == NULL)
                ThrowLastError();
            m_pfnNormalizationInitNormalizationFunc = (PFN_NORMALIZATION_INIT_NORMALIZATION)pfn;
        }

        BYTE* pResult = m_pfnNormalizationInitNormalizationFunc( NormForm, pTableData);
        if (pResult == NULL)
            ThrowOutOfMemory();
    }

    END_QCALL;
}

#endif // FEATURE_CORECLR


//
// This table should be sorted using case-insensitive ordinal order.
// In the managed code, String.CompareStringOrdinalWC() is used to sort this.
//


/**
 * This function returns the number of items in EncodingDataTable.
 */
FCIMPL0(INT32, COMNlsInfo::nativeGetNumEncodingItems)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    return (m_nEncodingDataTableItems);
}
FCIMPLEND



typedef CultureDataBaseObject* CULTUREDATAREF;

// nativeInitCultureData checks with the OS to see if this is a valid culture.
// If so we populate a limited number of fields.  If its not valid we return false.
//
// The fields we populate:
//
// sWindowsName -- The name that windows thinks this culture is, ie:
//                            en-US if you pass in en-US
//                            de-DE_phoneb if you pass in de-DE_phoneb
//                            fj-FJ if you pass in fj (neutral, on a pre-Windows 7 machine)
//                            fj if you pass in fj (neutral, post-Windows 7 machine)
//
// sRealName -- The name you used to construct the culture, in pretty form
//                       en-US if you pass in EN-us
//                       en if you pass in en
//                       de-DE_phoneb if you pass in de-DE_phoneb
//
// sSpecificCulture -- The specific culture for this culture
//                             en-US for en-US
//                             en-US for en
//                             de-DE_phoneb for alt sort
//                             fj-FJ for fj (neutral)
//
// sName -- The IETF name of this culture (ie: no sort info, could be neutral)
//                en-US if you pass in en-US
//                en if you pass in en
//                de-DE if you pass in de-DE_phoneb
//
// bNeutral -- TRUE if it is a neutral locale
//
// For a neutral we just populate the neutral name, but we leave the windows name pointing to the
// windows locale that's going to provide data for us.
//
FCIMPL1(FC_BOOL_RET, COMNlsInfo::nativeInitCultureData, CultureDataBaseObject *cultureDataUNSAFE)
{
    FCALL_CONTRACT;

    BOOL        success=FALSE;

    struct _gc
    {
        STRINGREF stringResult;
        CULTUREDATAREF cultureData;
    } gc;

    gc.stringResult = NULL;
    gc.cultureData  = (CULTUREDATAREF) cultureDataUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    WCHAR       buffer[LOCALE_NAME_MAX_LENGTH];
    int         result;

    StackSString realNameBuffer( ((STRINGREF)gc.cultureData->sRealName)->GetBuffer() );

    // Call GetLocaleInfoEx and see if the OS knows about it.
    // Note that GetLocaleInfoEx has variations:
    // * Pre-Vista it fails and has to go downlevel
    // * Vista succeeds, but not for neutrals
    // * Win7 succeeds for all locales.
    // * Mac does ???
    // The differences should be handled by the NewApis wrapper
    result = NewApis::GetLocaleInfoEx(realNameBuffer, LOCALE_SNAME, buffer, NumItems(buffer));

    // Did it fail?
    if (result == 0)
    {
        // Not a real locale, fail
        goto Exit;
    }

    // It worked, note that the name is the locale name, so use that (even for neutrals)
    // We need to clean up our "real" name, which should look like the windows name right now
    // so overwrite the input with the cleaned up name
    gc.stringResult = StringObject::NewString(buffer, result-1);
    SetObjectReference((OBJECTREF*)&(gc.cultureData->sRealName), gc.stringResult, NULL);

    // Check for neutrality, don't expect to fail
    // (buffer has our name in it, so we don't have to do the gc. stuff)
    DWORD bNeutral;
    if (0 == NewApis::GetLocaleInfoEx(buffer, LOCALE_INEUTRAL | LOCALE_RETURN_NUMBER, (LPWSTR)&bNeutral, sizeof(bNeutral)/sizeof(WCHAR)))
        goto Exit;

    // Remember our neutrality
    gc.cultureData->bNeutral = (bNeutral != 0);

    gc.cultureData->bWin32Installed = (IsOSValidLocaleName(buffer, gc.cultureData->bNeutral) != 0);
    gc.cultureData->bFramework = (IsWhidbeyFrameworkCulture(buffer) != 0);


    // Note: Parents will be set dynamically

    // Start by assuming the windows name'll be the same as the specific name since windows knows
    // about specifics on all versions.  For macs it also works.  Only for downlevel Neutral locales
    // does this have to change.
    gc.stringResult = StringObject::NewString(buffer, result-1);
    SetObjectReference((OBJECTREF*)&(gc.cultureData->sWindowsName), gc.stringResult, NULL);

    // Neutrals and non-neutrals are slightly different
    if (gc.cultureData->bNeutral)
    {
        // Neutral Locale

        // IETF name looks like neutral name
        gc.stringResult = StringObject::NewString(buffer, result-1);
        SetObjectReference((OBJECTREF*)&(gc.cultureData->sName), gc.stringResult, NULL);

        // Specific locale name is whatever ResolveLocaleName (win7+) returns.
        // (Buffer has our name in it, and we can recycle that because windows resolves it before writing to the buffer)
        result = NewApis::ResolveLocaleName(buffer, buffer, NumItems(buffer));

        // 0 is failure, 1 is invariant (""), which we expect
        if (result < 1) goto Exit;

        // We found a locale name, so use it.
        // In vista this should look like a sort name (de-DE_phoneb) or a specific culture (en-US) and be in the "pretty" form
        gc.stringResult = StringObject::NewString(buffer, result - 1);
        SetObjectReference((OBJECTREF*)&(gc.cultureData->sSpecificCulture), gc.stringResult, NULL);

#ifdef FEATURE_CORECLR
        if (!IsWindows7())
        {
            // For neutrals on Windows 7 + the neutral windows name can be the same as the neutral name,
            // but on pre windows 7 names it has to be the specific, so we have to fix it in that case.
            gc.stringResult = StringObject::NewString(buffer, result - 1);
            SetObjectReference((OBJECTREF*)&(gc.cultureData->sWindowsName), gc.stringResult, NULL);
        }
#endif


    }
    else
    {
        // Specific Locale

        // Specific culture's the same as the locale name since we know its not neutral
        // On mac we'll use this as well, even for neutrals. There's no obvious specific
        // culture to use and this isn't exposed, but behaviorally this is correct on mac.
        // Note that specifics include the sort name (de-DE_phoneb)
        gc.stringResult = StringObject::NewString(buffer, result-1);
        SetObjectReference((OBJECTREF*)&(gc.cultureData->sSpecificCulture), gc.stringResult, NULL);

        // We need the IETF name (sname)
        // If we aren't an alt sort locale then this is the same as the windows name.
        // If we are an alt sort locale then this is the same as the part before the _ in the windows name
        // This is for like de-DE_phoneb and es-ES_tradnl that hsouldn't have the _ part

        int localeNameLength = result - 1;

        LCID lcid = NewApis::LocaleNameToLCID(buffer, 0);
        if (!IsCustomCultureId(lcid))
        {
            LPCWSTR index = wcschr(buffer, W('_'));
            if(index)                               // Not a custom culture and looks like an alt sort name
            {
                // Looks like an alt sort, and has a appropriate sort LCID (so not custom), make it smaller for the RFC 4646 style name
                localeNameLength = static_cast<int>(index - buffer);
            }
        }

        gc.stringResult = StringObject::NewString(buffer, localeNameLength);
        _ASSERTE(gc.stringResult != NULL);

        // Now use that name
        SetObjectReference((OBJECTREF*)&(gc.cultureData->sName), gc.stringResult, NULL);
    }

#ifdef FEATURE_CORECLR
    // For Silverlight make sure that the sorting tables are available (< Vista may not have east asian installed)
    result = NewApis::CompareStringEx(((STRINGREF)gc.cultureData->sWindowsName)->GetBuffer(),
                                      0, W("A"), 1, W("B"), 1, NULL, NULL, 0);
    if (result == 0) goto Exit;
#endif

    // It succeeded.
    success = TRUE;

Exit: {}

    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(success);
}
FCIMPLEND


// Return true if we're on Windows 7 (ie: if we have neutral native support)
BOOL COMNlsInfo::IsWindows7()
{
    static BOOL bChecked=FALSE;
    static BOOL bIsWindows7=FALSE;

    if (!bChecked)
    {
        // LOCALE_INEUTRAL is first supported on Windows 7
        if (GetLocaleInfoW(LOCALE_USER_DEFAULT, LOCALE_INEUTRAL, NULL, 0) != 0)
        {
            // Success, we're win7
            bIsWindows7 = TRUE;
        }

        // Either way we checked now
        bChecked = TRUE;
    }

    return bIsWindows7;
}

//
// QCall implementation
//
int QCALLTYPE COMNlsInfo::InternalFindNLSStringEx(
    __in_opt               INT_PTR     handle,               // optional sort handle
    __in_opt               INT_PTR     handleOrigin,
    __in_z                 LPCWSTR     lpLocaleName,         // locale name
    __in                   int         dwFindNLSStringFlags, // search falg
    __in_ecount(cchSource) LPCWSTR     lpStringSource,       // the string we search in
    __in                   int         cchSource,            // number of characters lpStringSource after sourceIndex
    __in                   int         sourceIndex,          // index from where the search will start in lpStringSource
    __in_ecount(cchValue)  LPCWSTR     lpStringValue,        // the string we search for
    __in                   int         cchValue)             // length of the string we search for
{
    CONTRACTL {
        QCALL_CHECK;
        PRECONDITION(lpLocaleName != NULL);
        PRECONDITION(lpStringSource != NULL);
        PRECONDITION(lpStringValue != NULL);
        PRECONDITION(cchSource>=0);
        PRECONDITION(cchValue>=0);
    } CONTRACTL_END;

    int retValue = -1;

    BEGIN_QCALL;

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();
    handle = EnsureValidSortHandle(handle, handleOrigin, lpLocaleName);
#endif

    #define RESERVED_FIND_ASCII_STRING 0x20000000       // This flag used only to tell the sorting DLL can assume the string characters are in ASCII.

#ifndef FEATURE_CORECLR
    int asciiFlag = (dwFindNLSStringFlags & RESERVED_FIND_ASCII_STRING);
#endif // FEATURE_CORECLR

    dwFindNLSStringFlags &= ~RESERVED_FIND_ASCII_STRING;

    if (cchValue == 0)
    {
        retValue = sourceIndex;       // keep Whidbey compatibility
        goto lExit;
    }

    if (sourceIndex<0 || cchSource<0 ||
        ((dwFindNLSStringFlags & (FIND_FROMEND | FIND_ENDSWITH)) && (sourceIndex+1<cchSource)))
    {
        goto lExit;
    }

    if (dwFindNLSStringFlags & COMPARE_OPTIONS_ORDINAL)
    {
        if (dwFindNLSStringFlags & (FIND_FROMEND | FIND_ENDSWITH))
        {
            retValue = NewApis::LastIndexOfString(
                        lpLocaleName,
                        &lpStringSource[sourceIndex - cchSource + 1],
                        cchSource,
                        lpStringValue,
                        cchValue,
                        dwFindNLSStringFlags & FIND_NLS_STRING_FLAGS_NEGATION,
                        dwFindNLSStringFlags & FIND_ENDSWITH);
            if (retValue >= 0)
            {
                retValue += sourceIndex - cchSource + 1;
            }
        }
        else
        {
            retValue = NewApis::IndexOfString(
                        lpLocaleName,
                        &lpStringSource[sourceIndex],
                        cchSource,
                        lpStringValue,
                        cchValue,
                        dwFindNLSStringFlags & FIND_NLS_STRING_FLAGS_NEGATION,
                        dwFindNLSStringFlags & FIND_STARTSWITH);

            if  (retValue >= 0)
            {
                retValue += sourceIndex;
            }
        }
    }
    else
    {
        if (dwFindNLSStringFlags & (FIND_FROMEND | FIND_ENDSWITH))
        {
#ifndef FEATURE_CORECLR
            if(!(curDomain->m_bUseOsSorting))
            {
                retValue = SortVersioning::SortDllFindString((SortVersioning::PSORTHANDLE) handle,
                                                  dwFindNLSStringFlags | asciiFlag,
                                                  &lpStringSource[sourceIndex - cchSource + 1],
                                                  cchSource,
                                                  lpStringValue,
                                                  cchValue,
                                                  NULL, NULL, 0);
            }
            else if(curDomain->m_pCustomSortLibrary != NULL)
            {
                retValue = (curDomain->m_pCustomSortLibrary->pFindNLSStringEx)(
                                        handle != NULL ? NULL : lpLocaleName,
                                        dwFindNLSStringFlags,
                                        &lpStringSource[sourceIndex - cchSource + 1],
                                        cchSource,
                                        lpStringValue,
                                        cchValue, NULL, NULL, NULL, (LPARAM) handle);
            }   
            else
#endif
            {
                retValue = NewApis::FindNLSStringEx(
                                        handle != NULL ? NULL : lpLocaleName,
                                        dwFindNLSStringFlags,
                                        &lpStringSource[sourceIndex - cchSource + 1],
                                        cchSource,
                                        lpStringValue,
                                        cchValue, NULL, NULL, NULL, (LPARAM) handle);
            }

            if (retValue >= 0)
            {
                retValue += sourceIndex - cchSource + 1;
            }
        }
        else
        {
#ifndef FEATURE_CORECLR
            if(!(curDomain->m_bUseOsSorting))
            {
                retValue = SortVersioning::SortDllFindString((SortVersioning::PSORTHANDLE) handle,
                                                  dwFindNLSStringFlags | asciiFlag,
                                                  &lpStringSource[sourceIndex],
                                                  cchSource,
                                                  lpStringValue,
                                                  cchValue,
                                                  NULL, NULL, 0);
            }
            else if(curDomain->m_pCustomSortLibrary != NULL)
            {
                retValue = (curDomain->m_pCustomSortLibrary->pFindNLSStringEx)(
                                        handle != NULL ? NULL : lpLocaleName,
                                        dwFindNLSStringFlags,
                                        &lpStringSource[sourceIndex],
                                        cchSource,
                                        lpStringValue,
                                        cchValue, NULL, NULL, NULL, (LPARAM) handle);
            }
            else
#endif
            {
                retValue = NewApis::FindNLSStringEx(
                                        handle != NULL ? NULL : lpLocaleName,
                                        dwFindNLSStringFlags,
                                        &lpStringSource[sourceIndex],
                                        cchSource,
                                        lpStringValue,
                                        cchValue, NULL, NULL, NULL, (LPARAM) handle);
            }

            if (retValue >= 0)
            {
                retValue += sourceIndex;
            }
        }
    }

lExit:

    END_QCALL;

    return retValue;
}


int QCALLTYPE COMNlsInfo::InternalGetSortKey(
    __in_opt               INT_PTR handle,        // PSORTHANDLE
    __in_opt               INT_PTR handleOrigin,
    __in_z                 LPCWSTR pLocaleName,   // locale name
    __in                   int     flags,         // flags
    __in_ecount(cchSource) LPCWSTR pStringSource, // Source string
    __in                   int     cchSource,     // number of characters in lpStringSource
    __in_ecount(cchTarget) PBYTE   pTarget,       // Target data buffer (may be null to count)
    __in                   int     cchTarget)     // Character count for target buffer
{
    CONTRACTL {
        QCALL_CHECK;
        PRECONDITION(pLocaleName != NULL);
        PRECONDITION(pStringSource != NULL);
//        PRECONDITION(pTarget != NULL);
        PRECONDITION(cchSource>=0);
//        PRECONDITION(cchTarget>=0);
    } CONTRACTL_END;

    int retValue = 0;

    BEGIN_QCALL;


#ifndef FEATURE_CORECLR
    handle = EnsureValidSortHandle(handle, handleOrigin, pLocaleName);

    AppDomain* curDomain = GetAppDomain();

    if(!(curDomain->m_bUseOsSorting))
    {
        retValue = SortVersioning::SortDllGetSortKey((SortVersioning::PSORTHANDLE) handle,
                                                  flags,
                                                  pStringSource,
                                                  cchSource,
                                                  pTarget,
                                                  cchTarget,
                                                  NULL, 0);
    }
    else if(curDomain->m_pCustomSortLibrary != NULL)
    {
        retValue = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(handle != NULL ? NULL : pLocaleName,
                                          flags | LCMAP_SORTKEY,
                                          pStringSource,
                                          cchSource,
                                          (LPWSTR)pTarget,
                                          cchTarget,
                                          NULL,
                                          NULL,
                                          (LPARAM) handle);
    }
    else
#endif
    {
        // Just call NewApis::LCMapStringEx to do our work
        retValue = NewApis::LCMapStringEx(handle != NULL ? NULL : pLocaleName,
                                          flags | LCMAP_SORTKEY,
                                          pStringSource,
                                          cchSource,
                                          (LPWSTR)pTarget,
                                          cchTarget,
                                          NULL,
                                          NULL,
                                          (LPARAM) handle);
    }
    END_QCALL;

    return retValue;
}


// We allow InternalInitSortHandle to return a NULL value
// this is the case for Silverlight or when the appdomain has custom sorting.
// So all the methods that take a SortHandle, also have to
// be able to just call the slower api that looks up the tables based on the locale name
INT_PTR QCALLTYPE COMNlsInfo::InternalInitSortHandle(LPCWSTR localeName, __out INT_PTR* handleOrigin)
{
    CONTRACTL {
        QCALL_CHECK;
        PRECONDITION(localeName != NULL);
    } CONTRACTL_END;
    
    INT_PTR pSort = NULL;

    BEGIN_QCALL;

    pSort = InitSortHandleHelper(localeName, handleOrigin);

    END_QCALL;

    return pSort;
}

INT_PTR COMNlsInfo::InitSortHandleHelper(LPCWSTR localeName, __out INT_PTR* handleOrigin)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        PRECONDITION(localeName != NULL);
    } CONTRACTL_END;

    INT_PTR pSort = NULL;

#ifndef FEATURE_CORECLR
    AppDomain* curDomain = GetAppDomain();

#if _DEBUG
    _ASSERTE(curDomain->m_bSortingInitialized);
#endif

    if(curDomain->m_bUseOsSorting)
    {
        pSort = InternalInitOsSortHandle(localeName, handleOrigin);
    }
    else
    {
        pSort = InternalInitVersionedSortHandle(localeName, handleOrigin);
    }
#else
    // coreclr will try to initialize the handle and if running on downlevel it'll just return null
    pSort = InternalInitOsSortHandle(localeName, handleOrigin);
#endif // FEATURE_CORECLR
    return pSort;
}

#ifndef FEATURE_CORECLR
INT_PTR COMNlsInfo::InternalInitVersionedSortHandle(LPCWSTR localeName, __out INT_PTR* handleOrigin)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        PRECONDITION(localeName != NULL);
    } CONTRACTL_END;

    AppDomain* curDomain = GetAppDomain();

    if(curDomain->m_pCustomSortLibrary != NULL)
    {
        return NULL;
    }

    return InternalInitVersionedSortHandle(localeName, handleOrigin, curDomain->m_sortVersion);
}

INT_PTR COMNlsInfo::InternalInitVersionedSortHandle(LPCWSTR localeName, __out INT_PTR* handleOrigin, DWORD sortVersion)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        PRECONDITION(localeName != NULL);
    } CONTRACTL_END;
    
    INT_PTR pSort = NULL;

    _ASSERTE(NewApis::NotLeakingFrameworkOnlyCultures(localeName));

    *handleOrigin = (INT_PTR) SortVersioning::GetSortHandle;

    // try the requested version
    if(sortVersion != DEFAULT_SORT_VERSION)
    {
        NLSVERSIONINFO  sortVersionInfo;
        sortVersionInfo.dwNLSVersionInfoSize = sizeof(NLSVERSIONINFO);
        sortVersionInfo.dwNLSVersion = sortVersion;
        sortVersionInfo.dwDefinedVersion = sortVersion;
        pSort = (INT_PTR) SortVersioning::GetSortHandle(localeName, &sortVersionInfo);
    }

    // fallback to default version
    if(pSort == NULL)
    {
        pSort = (INT_PTR) SortVersioning::GetSortHandle(localeName, NULL);
    }

    _ASSERTE(RunningOnWin8() || pSort != NULL);

    return pSort;
}
#endif //FEATURE_CORECLR

// Can return NULL
INT_PTR COMNlsInfo::InternalInitOsSortHandle(LPCWSTR localeName, __out INT_PTR* handleOrigin)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        PRECONDITION(localeName != NULL);
    } CONTRACTL_END;
    
    INT_PTR pSort = NULL;

    AppDomain* curDomain = GetAppDomain();

#ifndef FEATURE_CORECLR
    if (RunningOnWin8() || curDomain->m_pCustomSortLibrary != NULL)
#else
    if (RunningOnWin8())
#endif //FEATURE_CORECLR 
    {
        LPARAM lSortHandle;
        int ret;

#ifndef FEATURE_CORECLR
        if (curDomain->m_pCustomSortLibrary != NULL)
        {
            ret = (curDomain->m_pCustomSortLibrary->pLCMapStringEx)(localeName, LCMAP_SORTHANDLE, NULL, 0, (LPWSTR) &lSortHandle, sizeof(LPARAM), NULL, NULL, 0);
            *handleOrigin = (INT_PTR) curDomain->m_pCustomSortLibrary->pLCMapStringEx;
        }
        else
#endif //FEATURE_CORECLR
        {
            ret = NewApis::LCMapStringEx(localeName, LCMAP_SORTHANDLE, NULL, 0, (LPWSTR) &lSortHandle, sizeof(LPARAM), NULL, NULL, 0);
            *handleOrigin = (INT_PTR) NewApis::LCMapStringEx;
        }

        if (ret != 0)
        {
            pSort = (INT_PTR) lSortHandle;
        }
    }

    return pSort;
}

#ifndef FEATURE_CORECLR
BOOL QCALLTYPE COMNlsInfo::InternalGetNlsVersionEx(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR lpLocaleName, NLSVERSIONINFOEX * lpVersionInformation)
{
    CONTRACTL {
        QCALL_CHECK;
    } CONTRACTL_END;

    BOOL ret = FALSE;

    BEGIN_QCALL;
    
    AppDomain* curDomain = GetAppDomain();

    if(curDomain->m_bUseOsSorting)
    {
        if(curDomain->m_pCustomSortLibrary != NULL)
        {
            ret = (curDomain->m_pCustomSortLibrary->pGetNLSVersionEx)(COMPARE_STRING, lpLocaleName, lpVersionInformation);
        }
        else
        {
            ret = GetNLSVersionEx(COMPARE_STRING, lpLocaleName, lpVersionInformation);
        }
    }
    else
    {
        handle = EnsureValidSortHandle(handle, handleOrigin, lpLocaleName);

        NLSVERSIONINFO nlsVersion;
        nlsVersion.dwNLSVersionInfoSize = sizeof(NLSVERSIONINFO);

        ret = SortVersioning::SortGetNLSVersion((SortVersioning::PSORTHANDLE) handle, COMPARE_STRING, &nlsVersion);

        lpVersionInformation->dwNLSVersion = nlsVersion.dwNLSVersion;
        lpVersionInformation->dwDefinedVersion = nlsVersion.dwDefinedVersion;
        lpVersionInformation->dwEffectiveId = 0;
        ZeroMemory(&(lpVersionInformation->guidCustomVersion), sizeof(GUID));                
    }
    
    END_QCALL;
 
    return ret;
}

DWORD QCALLTYPE COMNlsInfo::InternalGetSortVersion()
{
    CONTRACTL {
        QCALL_CHECK;
    } CONTRACTL_END;

    DWORD version = DEFAULT_SORT_VERSION;

    BEGIN_QCALL;

    AppDomain* curDomain = GetAppDomain();
    version = curDomain->m_sortVersion;

    END_QCALL;

    return version;
}

#endif //FEATURE_CORECLR

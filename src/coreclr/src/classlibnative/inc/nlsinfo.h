// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  Class:    NLSInfo
//

//
//  Purpose:  This module defines the methods of the COMNlsInfo
//            class.  These methods are the helper functions for the
//            managed NLS+ classes.
//
//  Date:     August 12, 1998
//
////////////////////////////////////////////////////////////////////////////

#ifndef _NLSINFO_H_
#define _NLSINFO_H_

#define DEFAULT_SORT_VERSION 0
#define SORT_VERSION_WHIDBEY 0x00001000
#define SORT_VERSION_V4      0x00060101

//
//This structure must map 1-for-1 with the InternalDataItem structure in
//System.Globalization.EncodingTable.
//
struct EncodingDataItem {
    const char *   webName;
    unsigned short codePage;
    // free space here
};

//
//This structure must map 1-for-1 with the InternalCodePageDataItem structure in
//System.Globalization.EncodingTable.
//
struct CodePageDataItem {
    unsigned short   codePage;
    unsigned short   uiFamilyCodePage;
    DWORD            dwFlags;             // only 4-bit used now
    const char     * names;
};

// Normalization
typedef BOOL (*PFN_NORMALIZATION_IS_NORMALIZED_STRING)
    ( int NormForm, LPCWSTR lpInString, int cchInString);

typedef int (*PFN_NORMALIZATION_NORMALIZE_STRING)
    ( int NormForm, LPCWSTR lpInString, int cchInString, LPWSTR lpOutString, int cchOutString);

typedef BYTE* (*PFN_NORMALIZATION_INIT_NORMALIZATION)
    ( int NormForm, BYTE* pTableData);

////////////////////////////////////////////////////////////////////////////
//
// Forward declarations
//
////////////////////////////////////////////////////////////////////////////

class CharTypeTable;
class CasingTable;
class SortingTable;
class NativeTextInfo;
class CultureDataBaseObject;

class COMNlsInfo {

public:
#ifdef FEATURE_SYNTHETIC_CULTURES
    static INT32  WstrToInteger4(__in_z LPCWSTR wstrLocale, __in int Radix);
#endif // FEATURE_SYNTHETIC_CULTURES

    static INT32 GetCHTLanguage();
    static INT32 CallGetSystemDefaultUILanguage();
    static INT32 CallGetUserDefaultUILanguage();
    static LANGID GetDownLevelSystemDefaultUILanguage();

    //
    //  Native helper functions for methods in CultureInfo.
    //
    static BOOL QCALLTYPE InternalGetDefaultLocaleName(INT32 langType, QCall::StringHandleOnStack defaultLocaleName);
    static BOOL QCALLTYPE InternalGetUserDefaultUILanguage(QCall::StringHandleOnStack userDefaultUiLanguage);
    static BOOL QCALLTYPE InternalGetSystemDefaultUILanguage(QCall::StringHandleOnStack systemDefaultUiLanguage);

// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#ifdef FEATURE_CORECLR
    static FCDECL0(Object*, nativeGetResourceFallbackArray);
#endif

    //
    // Native helper functions for methods in DateTimeFormatInfo
    //
    static FCDECL1(FC_BOOL_RET,  nativeSetThreadLocale, StringObject* localeNameUNSAFE);
    static FCDECL2(Object*, nativeGetLocaleInfoEx, StringObject* localeNameUNSAFE, INT32 lcType);
    static FCDECL2(INT32, nativeGetLocaleInfoExInt, StringObject* localeNameUNSAFE, INT32 lcType);

    //
    //  Native helper functions for CultureData
    //
    static FCDECL1(FC_BOOL_RET, nativeInitCultureData, CultureDataBaseObject *data);
    static FCDECL3(FC_BOOL_RET, nativeGetNumberFormatInfoValues, StringObject* localeNameUNSAFE, NumberFormatInfo* nfi, CLR_BOOL useUserOverride);
    static FCDECL1(Object*, LCIDToLocaleName, LCID lcid);
    static FCDECL1(INT32, LocaleNameToLCID, StringObject* localeNameUNSAFE);

    static INT32 QCALLTYPE InternalCompareString (INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName, LPCWSTR string1, INT32 offset1, INT32 length1, LPCWSTR string2, INT32 offset2, INT32 length2, INT32 flags);
    static INT32 QCALLTYPE InternalGetGlobalizedHashCode(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName, LPCWSTR pString, INT32 length, INT32 dwFlagsIn, BOOL bForceRandomizedHashing, INT64 additionalEntropy);

    static BOOL QCALLTYPE InternalIsSortable(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName, LPCWSTR pString, INT32 length);
    static INT_PTR QCALLTYPE InternalInitSortHandle(LPCWSTR localeName, INT_PTR* handleOrigin);
    static INT_PTR InitSortHandleHelper(LPCWSTR localeName, INT_PTR* handleOrigin);
    static INT_PTR InternalInitOsSortHandle(LPCWSTR localeName, INT_PTR* handleOrigin);
#ifndef FEATURE_CORECLR
    static INT_PTR InternalInitVersionedSortHandle(LPCWSTR localeName, INT_PTR* handleOrigin);
    static INT_PTR InternalInitVersionedSortHandle(LPCWSTR localeName, INT_PTR* handleOrigin, DWORD sortVersion);
    static DWORD QCALLTYPE InternalGetSortVersion();
    static BOOL QCALLTYPE InternalGetNlsVersionEx(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR lpLocaleName, NLSVERSIONINFOEX * lpVersionInformation);
#endif


#ifndef FEATURE_CORECLR
    //
    //  Native helper function for methods in TimeZone
    //
    static FCDECL0(LONG, nativeGetTimeZoneMinuteOffset);
    static FCDECL0(Object*, nativeGetStandardName);
    static FCDECL0(Object*, nativeGetDaylightName);
    static FCDECL1(Object*, nativeGetDaylightChanges, int year);
#endif // FEATURE_CORECLR

    //
    //  Native helper function for methods in EncodingTable
    //
    static FCDECL0(INT32, nativeGetNumEncodingItems);
    static FCDECL0(EncodingDataItem *, nativeGetEncodingTableDataPointer);
    static FCDECL0(CodePageDataItem *, nativeGetCodePageTableDataPointer);
#if FEATURE_CODEPAGES_FILE
    static FCDECL3(LPVOID, nativeCreateOpenFileMapping,
                       StringObject* inSectionNameUNSAFE, int inBytesToAllocate, HANDLE *mappedFile);
#endif // FEATURE_CODEPAGES_FILE

    //
    //  Native helper function for methods in CharacterInfo
    //
    static FCDECL0(void, AllocateCharTypeTable);

    //
    //  Native helper function for methods in TextInfo
    //
    static FCDECL5(FC_CHAR_RET, InternalChangeCaseChar, INT_PTR handle, INT_PTR handleOrigin, StringObject* localeNameUNSAFE, CLR_CHAR wch, CLR_BOOL bIsToUpper);
    static FCDECL5(Object*, InternalChangeCaseString, INT_PTR handle, INT_PTR handleOrigin, StringObject* localeNameUNSAFE, StringObject* pString, CLR_BOOL bIsToUpper);
    static FCDECL6(INT32, InternalGetCaseInsHash, INT_PTR handle, INT_PTR handleOrigin, StringObject* localeNameUNSAFE, LPVOID strA, CLR_BOOL bForceRandomizedHashing, INT64 additionalEntropy);
    static INT32 QCALLTYPE InternalCompareStringOrdinalIgnoreCase(LPCWSTR string1, INT32 index1, LPCWSTR string2, INT32 index2, INT32 length1, INT32 length2);

    static BOOL QCALLTYPE InternalTryFindStringOrdinalIgnoreCase(
        __in                   DWORD       dwFindNLSStringFlags, // mutually exclusive flags: FIND_FROMSTART, FIND_STARTSWITH, FIND_FROMEND, FIND_ENDSWITH
        __in_ecount(cchSource) LPCWSTR     lpStringSource,       // the string we search in
        __in                   int         cchSource,            // number of characters lpStringSource after sourceIndex
        __in                   int         sourceIndex,          // index from where the search will start in lpStringSource
        __in_ecount(cchValue)  LPCWSTR     lpStringValue,        // the string we search for
        __in                   int         cchValue,
        __out                  int*        foundIndex);          // the index in lpStringSource where we found lpStringValue

    //
    // Native helper function for methods in Normalization
    //
    static FCDECL6(int, nativeNormalizationNormalizeString,
        int NormForm, int& iError,
        StringObject* inString, int inLength,
        CHARArray* outChars, int outLength);
    static FCDECL4(FC_BOOL_RET, nativeNormalizationIsNormalizedString,
        int NormForm, int& iError,
        StringObject* inString, int cwLength);

    static void QCALLTYPE nativeNormalizationInitNormalization(int NormForm, BYTE* pTableData);

    //
    // QCalls prototype
    //

    static int QCALLTYPE nativeEnumCultureNames(INT32 cultureTypes, QCall::ObjectHandleOnStack retStringArray);

    static int QCALLTYPE InternalFindNLSStringEx(
        __in_opt               INT_PTR     handle,               // optional sort handle
        __in_opt               INT_PTR     handleOrigin,         // optional pointer to the native function that created the sort handle
        __in_z                 LPCWSTR     lpLocaleName,         // locale name
        __in                   int         dwFindNLSStringFlags, // search falg
        __in_ecount(cchSource) LPCWSTR     lpStringSource,       // the string we search in
        __in                   int         cchSource,            // number of characters lpStringSource after sourceIndex
        __in                   int         sourceIndex,          // index from where the search will start in lpStringSource
        __in_ecount(cchValue)  LPCWSTR     lpStringValue,        // the string we search for
        __in                   int         cchValue);            // length of the string we search for

    static int QCALLTYPE InternalGetSortKey(
        __in_opt               INT_PTR handle,        // PSORTHANDLE
        __in_opt               INT_PTR handleOrigin,  // optional pointer to the native function that created the sort handle
        __in_z                 LPCWSTR pLocaleName,   // locale name
        __in                   int     flags,         // flags
        __in_ecount(cchSource) LPCWSTR pStringSource, // Source string
        __in                   int     cchSource,     // number of characters in lpStringSource
        __in_ecount(cchTarget) PBYTE   pTarget,       // Target data buffer (may be null to count)
        __in                   int     cchTarget);    // Character count for target buffer


private:

    //
    //  Internal helper functions.
    //
    static LPVOID internalEnumSystemLocales(DWORD dwFlags);
    static INT32  CompareOrdinal(__in_ecount(Length1) WCHAR* strAChars, int Length1, __in_ecount(Length2) WCHAR* strBChars, int Length2 );
    static INT32  FastIndexOfString(__in WCHAR *sourceString, INT32 startIndex, INT32 endIndex, __in_ecount(patternLength) WCHAR *pattern, INT32 patternLength);
    static INT32  FastIndexOfStringInsensitive(__in WCHAR *sourceString, INT32 startIndex, INT32 endIndex, __in_ecount(patternLength) WCHAR *pattern, INT32 patternLength);
    static INT32  FastLastIndexOfString(__in WCHAR *sourceString, INT32 startIndex, INT32 endIndex, __in_ecount(patternLength) WCHAR *pattern, INT32 patternLength);
    static INT32  FastLastIndexOfStringInsensitive(__in WCHAR *sourceString, INT32 startIndex, INT32 endIndex, __in_ecount(patternLength) WCHAR *pattern, INT32 patternLength);

    static BOOL GetNativeDigitsFromWin32(LPCWSTR locale, PTRARRAYREF* pOutputStrAry, BOOL useUserOverride);
    static BOOL CallGetLocaleInfoEx(LPCWSTR locale, int lcType, STRINGREF* pOutputStrRef, BOOL useUserOverride);
    static BOOL CallGetLocaleInfoEx(LPCWSTR locale, int lcType, INT32* pOutputInt32, BOOL useUserOverride);

    static BOOL IsWindows7();

    //
    //  Definitions.
    //

#ifndef FEATURE_CORECLR
    // Normalization
    static HMODULE m_hNormalization;
    static PFN_NORMALIZATION_IS_NORMALIZED_STRING m_pfnNormalizationIsNormalizedStringFunc;
    static PFN_NORMALIZATION_NORMALIZE_STRING m_pfnNormalizationNormalizeStringFunc;
    static PFN_NORMALIZATION_INIT_NORMALIZATION m_pfnNormalizationInitNormalizationFunc;
#endif

private:
    //
    // Internal encoding data tables.
    //
    const static int m_nEncodingDataTableItems;
    const static EncodingDataItem EncodingDataTable[];

    const static int m_nCodePageTableItems;
    const static CodePageDataItem CodePageDataTable[];

    static INT_PTR EnsureValidSortHandle(INT_PTR handle, INT_PTR handleOrigin, LPCWSTR localeName);
};

#endif  // _NLSINFO_H_

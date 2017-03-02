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

class COMNlsInfo
{
public:

    static INT32 CallGetUserDefaultUILanguage();

    //
    // Native helper functions for methods in DateTimeFormatInfo
    //
    static FCDECL1(FC_BOOL_RET,  nativeSetThreadLocale, StringObject* localeNameUNSAFE);

    //
    //  Native helper functions for CultureData
    //

    static INT32 QCALLTYPE InternalGetGlobalizedHashCode(INT_PTR handle, LPCWSTR localeName, LPCWSTR pString, INT32 length, INT32 dwFlagsIn, INT64 additionalEntropy);

    //
    //  Native helper function for methods in EncodingTable
    //
    static FCDECL0(INT32, nativeGetNumEncodingItems);
    static FCDECL0(EncodingDataItem *, nativeGetEncodingTableDataPointer);
    static FCDECL0(CodePageDataItem *, nativeGetCodePageTableDataPointer);

    //
    // Native helper function for methods in Normalization
    //
    // On Windows 7 we use the normalization data embedded inside the corelib to get better results.
    // On Windows 8 and up we use the OS for normalization. 
    // That is why we need to keep these fcalls and not doing it through pinvokes.

    static FCDECL6(int, nativeNormalizationNormalizeString,
        int NormForm, int& iError,
        StringObject* inString, int inLength,
        CHARArray* outChars, int outLength);
    static FCDECL4(FC_BOOL_RET, nativeNormalizationIsNormalizedString,
        int NormForm, int& iError,
        StringObject* inString, int cwLength);

    static void QCALLTYPE nativeNormalizationInitNormalization(int NormForm, BYTE* pTableData);

private:
    //
    //  Definitions.
    //

#ifndef FEATURE_COREFX_GLOBALIZATION
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
};

#endif  // _NLSINFO_H_

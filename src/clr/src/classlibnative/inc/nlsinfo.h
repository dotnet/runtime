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

class COMNlsInfo
{
public:

    //
    // Native helper functions for methods in DateTimeFormatInfo
    //
    static FCDECL1(FC_BOOL_RET,  nativeSetThreadLocale, StringObject* localeNameUNSAFE);

    //
    //  Native helper functions for CultureData
    //

    static INT32 QCALLTYPE InternalGetGlobalizedHashCode(INT_PTR handle, LPCWSTR localeName, LPCWSTR pString, INT32 length, INT32 dwFlagsIn);

    //
    //  Native helper function for methods in EncodingTable
    //
    static FCDECL0(INT32, nativeGetNumEncodingItems);
    static FCDECL0(EncodingDataItem *, nativeGetEncodingTableDataPointer);
    static FCDECL0(CodePageDataItem *, nativeGetCodePageTableDataPointer);

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
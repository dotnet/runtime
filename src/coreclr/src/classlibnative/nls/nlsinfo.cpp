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

//#include <mlang.h>
#include "sortversioning.h"

#include "newapis.h"

//
//  Constant Declarations.
//

#define MAX_STRING_VALUE        512

// TODO: NLS Arrowhead -Be nice if we could depend more on the OS for this
// Language ID for CHT (Taiwan)
#define LANGID_ZH_TW            0x0404
// Language ID for CHT (Hong-Kong)
#define LANGID_ZH_HK            0x0c04


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

////////////////////////////////////////////////////////////////////////////
//
//  InternalGetGlobalizedHashCode
//
////////////////////////////////////////////////////////////////////////////
INT32 QCALLTYPE COMNlsInfo::InternalGetGlobalizedHashCode(INT_PTR handle, LPCWSTR localeName, LPCWSTR string, INT32 length, INT32 dwFlagsIn, INT64 additionalEntropy)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(localeName));
        PRECONDITION(CheckPointer(string, NULL_OK));
    } CONTRACTL_END;

    INT32  iReturnHash  = 0;
    BEGIN_QCALL;

    int byteCount = 0;

    //
    //  Make sure there is a string.
    //
    if (!string) {
        COMPlusThrowArgumentNull(W("string"),W("ArgumentNull_String"));
    }

    DWORD dwFlags = (LCMAP_SORTKEY | dwFlagsIn);

    //
    // Caller has already verified that the string is not of zero length
    //
    // Assert if we might hit an AV in LCMapStringEx for the invariant culture.
    _ASSERTE(length > 0 || (dwFlags & LCMAP_LINGUISTIC_CASING) == 0);
    {
        byteCount=NewApis::LCMapStringEx(handle != NULL ? NULL : localeName, dwFlags, string, length, NULL, 0, NULL, NULL, (LPARAM) handle);
    }

    //A count of 0 indicates that we either had an error or had a zero length string originally.
    if (byteCount==0)
    {
        COMPlusThrow(kArgumentException, W("Arg_MustBeString"));
    }

    // We used to use a NewArrayHolder here, but it turns out that hurts our large # process
    // scalability in ASP.Net hosting scenarios, using the quick bytes instead mostly stack
    // allocates and ups throughput by 8% in 100 process case, 5% in 1000 process case
    {
        CQuickBytesSpecifySize<MAX_STRING_VALUE * sizeof(WCHAR)> qbBuffer;
        BYTE* pByte = (BYTE*)qbBuffer.AllocThrows(byteCount);

        {
            NewApis::LCMapStringEx(handle != NULL ? NULL : localeName, dwFlags, string, length, (LPWSTR)pByte, byteCount, NULL,NULL, (LPARAM) handle);
        }

        iReturnHash = COMNlsHashProvider::s_NlsHashProvider.HashSortKey(pByte, byteCount, true, additionalEntropy);
    }
    END_QCALL;
    return(iReturnHash);
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

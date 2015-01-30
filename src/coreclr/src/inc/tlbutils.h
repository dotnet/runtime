//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
// Utilities used to help manipulating typelibs
//


#ifndef _TLBUTILS_H
#define _TLBUTILS_H

#ifndef FEATURE_COMINTEROP_TLB_SUPPORT
#error FEATURE_COMINTEROP_TLB_SUPPORT is required for this file
#endif // FEATURE_COMINTEROP_TLB_SUPPORT

#include "windows.h"
#include "utilcode.h"

struct StdConvertibleItfInfo
{
    LPUTF8      m_strMngTypeName;
    GUID      * m_pNativeTypeIID;
    LPUTF8      m_strCustomMarshalerTypeName;
    LPUTF8      m_strCookie;
};

// This method returns the custom marshaler info to convert the native interface
// to its managed equivalent. Or null if the interface is not a standard convertible interface.
const StdConvertibleItfInfo *GetConvertionInfoFromNativeIID(REFGUID rGuidNativeItf);

// This function determines the namespace name for a TypeLib.
HRESULT GetNamespaceNameForTypeLib(     // S_OK or error.
    ITypeLib    *pITLB,                 // [IN] The TypeLib.
    BSTR        *pwzNamespace);         // [OUT] Put the namespace name here.

// This function determines the namespace.name for a TypeInfo.  If no namespace
//  is provided, it is retrieved from the containing library.
HRESULT GetManagedNameForTypeInfo(      // S_OK or error.
    ITypeInfo   *pITI,                  // [IN] The TypeInfo.
    LPCWSTR     wzNamespace,            // [IN, OPTIONAL] Default namespace name.
    LPCWSTR     wzAsmName,              // [IN, OPTIONAL] Assembly name.
    BSTR        *pwzName);              // [OUT] Put the name here.

#endif // _TLBUTILS_H








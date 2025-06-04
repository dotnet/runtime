// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDUtil.h
//

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __MDUtil__h__
#define __MDUtil__h__

#include "metadata.h"


HRESULT _GetFixedSigOfVarArg(           // S_OK or error.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob of CLR method signature
    ULONG   cbSigBlob,                  // [IN] size of signature
    CQuickBytes *pqbSig,                // [OUT] output buffer for fixed part of VarArg Signature
    ULONG   *pcbSigBlob);               // [OUT] number of bytes written to the above output buffer

ULONG _GetSizeOfConstantBlob(
    DWORD       dwCPlusTypeFlag,            // ELEMENT_TYPE_*
    void        *pValue,                    // BLOB value
    ULONG       cchString);                 // Size of string in wide chars, or -1 for auto.

#endif // __MDUtil__h__

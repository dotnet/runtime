// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDInternalDispenser.h
//

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __MDInternalDispenser__h__
#define __MDInternalDispenser__h__

#ifdef FEATURE_METADATA_INTERNAL_APIS

#include "mdinternalro.h"

enum MDFileFormat
{
    MDFormat_ReadOnly = 0,
    MDFormat_ReadWrite = 1,
    MDFormat_ICR = 2,
    MDFormat_Invalid = 3
};

STDAPI GetMDInternalInterface(
    LPVOID      pData,                  // [IN] Buffer with the metadata.
    ULONG       cbData,                 // [IN] Size of the data in the buffer.
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk);              // [out] Return interface on success.

#endif //FEATURE_METADATA_INTERNAL_APIS

#endif // __MDInternalDispenser__h__

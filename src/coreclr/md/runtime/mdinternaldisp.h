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

STDAPI GetMDInternalInterface(
    LPVOID      pData,                  // [IN] Buffer with the metadata.
    ULONG       cbData,                 // [IN] Size of the data in the buffer.
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk);              // [out] Return interface on success.

#endif //FEATURE_METADATA_INTERNAL_APIS

#endif // __MDInternalDispenser__h__

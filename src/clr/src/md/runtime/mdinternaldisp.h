//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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


HRESULT CheckFileFormat(LPVOID pData, ULONG cbData, MDFileFormat *pFormat);
STDAPI GetMDInternalInterface(
    LPVOID      pData,                  // [IN] Buffer with the metadata.
    ULONG       cbData,                 // [IN] Size of the data in the buffer.
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk);              // [out] Return interface on success.

HRESULT GetAssemblyMDInternalImportHelper(HCORMODULE hModule,
                                          REFIID     riid,
                                          MDInternalImportFlags flags,
                                          IUnknown   **ppIUnk);

#endif //FEATURE_METADATA_INTERNAL_APIS

#endif // __MDInternalDispenser__h__

//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
// ===========================================================================
// File: comem.cpp
// 
// ===========================================================================

#include "common.h" 

STDAPI_(LPVOID) CoTaskMemAlloc(SIZE_T cb)
{
    return LocalAlloc(LMEM_FIXED, cb);
}

STDAPI_(void) CoTaskMemFree(LPVOID pv)
{
    LocalFree(pv);
}

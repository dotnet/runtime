// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: comem.cpp
//
// ===========================================================================

#include "common.h"

STDAPI_(LPVOID) CoTaskMemAlloc(SIZE_T cb)
{
    return malloc(cb);
}

STDAPI_(void) CoTaskMemFree(LPVOID pv)
{
    free(pv);
}

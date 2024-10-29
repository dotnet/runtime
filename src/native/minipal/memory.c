// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "memory.h"

#ifdef HOST_WINDOWS
#include <Windows.h>

void* minicom_CoTaskMemAlloc(size_t cb)
{
    return CoTaskMemAlloc(cb);
}

void minicom_CoTaskMemFree(void* pv)
{
    CoTaskMemFree(pv);
}
#else
void* minicom_CoTaskMemAlloc(size_t cb)
{
    return malloc(cb);
}

void minicom_CoTaskMemFree(void* pv)
{
    free(pv);
}
#endif

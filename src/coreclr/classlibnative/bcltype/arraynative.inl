// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ArrayNative.cpp
//

//
// This file contains the native methods that support the Array class
//

#ifndef _ARRAYNATIVE_INL_
#define _ARRAYNATIVE_INL_

#include "gchelpers.inl"

FORCEINLINE void InlinedMemmoveGCRefsHelper(void *dest, const void *src, size_t len)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(dest != nullptr);
    _ASSERTE(src != nullptr);
    _ASSERTE(dest != src);
    _ASSERTE(len != 0);

    // Make sure everything is pointer aligned
    _ASSERTE(IS_ALIGNED(dest, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(src, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(len, sizeof(SIZE_T)));

    _ASSERTE(CheckPointer(dest));
    _ASSERTE(CheckPointer(src));

    const bool notInHeap = ((BYTE*)dest < g_lowest_address || (BYTE*)dest >= g_highest_address);

    if (!notInHeap)
    {
        GCHeapMemoryBarrier();
    }

    SIZE_T* dptr = (SIZE_T*)dest;
    SIZE_T* sptr = (SIZE_T*)src;
    SIZE_T  num  = len / sizeof(SIZE_T);
    for (size_t i = 0; i < num; i++)
    {
        dptr[i] = sptr[i];
    }

    if (!notInHeap)
    {
        InlinedSetCardsAfterBulkCopyHelper((Object**)dest, len);
    }
}

#endif // !_ARRAYNATIVE_INL_

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

FORCEINLINE void InlinedMemmoveGCRefsHelper(void *dest, const void *src, size_t bytesLen)
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
    _ASSERTE(bytesLen != 0);

    // Make sure everything is pointer aligned
    _ASSERTE(IS_ALIGNED(dest, sizeof(size_t)));
    _ASSERTE(IS_ALIGNED(src, sizeof(size_t)));
    _ASSERTE(IS_ALIGNED(bytesLen, sizeof(size_t)));

    _ASSERTE(CheckPointer(dest));
    _ASSERTE(CheckPointer(src));

    const bool notInHeap = ((uint8_t*)dest < g_lowest_address) | ((uint8_t*)dest >= g_highest_address);
    if (!notInHeap)
    {
        GCHeapMemoryBarrier();
    }

    auto dptr = static_cast<volatile size_t*>(dest);
    auto sptr = static_cast<const volatile size_t*>(src);

    for (size_t i = 0; i <= bytesLen; i += sizeof(size_t))
    {
        dptr[i] = sptr[i];
    }

    if (!notInHeap)
    {
        InlinedSetCardsAfterBulkCopyHelper((Object**)dest, bytesLen);
    }
}

#endif // !_ARRAYNATIVE_INL_

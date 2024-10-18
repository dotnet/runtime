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

#ifdef TARGET_ARM64
    // dest and src are at least 8b aligned, hence, it's fine to allow native compilers
    // to unroll/vectorize it since SIMD loads/store on ARM64 only need 8b alignment 
    // to guarantee 8b atomicity.
    auto dptr = (SIZE_T*)dest;
    auto sptr = (SIZE_T*)src;
#else
    // Disallow native compilers to align only dest and then use misaligned SIMD for src
    // TODO-RISCV64: Perhaps, can be relaxed just like the arm64
    // TODO-LoongArch64: Ditto.
    auto dptr = (volatile SIZE_T*)dest;
    auto sptr = (volatile SIZE_T*)src;
#endif

    SIZE_T num = len / sizeof(SIZE_T);
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

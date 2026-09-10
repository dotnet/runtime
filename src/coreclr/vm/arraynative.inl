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

    GCHeapUtilities::BulkMoveWithWriteBarrier(dest, src, len);
}

#endif // !_ARRAYNATIVE_INL_

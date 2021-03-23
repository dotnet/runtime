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

FORCEINLINE void InlinedForwardGCSafeCopyHelper(void *dest, const void *src, size_t len)
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

    // To be able to copy forwards, the destination buffer cannot start inside the source buffer
    _ASSERTE((SIZE_T)dest - (SIZE_T)src >= len);

    // Make sure everything is pointer aligned
    _ASSERTE(IS_ALIGNED(dest, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(src, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(len, sizeof(SIZE_T)));

    _ASSERTE(CheckPointer(dest));
    _ASSERTE(CheckPointer(src));

    SIZE_T *dptr = (SIZE_T *)dest;
    SIZE_T *sptr = (SIZE_T *)src;

    while (true)
    {
        if ((len & sizeof(SIZE_T)) != 0)
        {
            *dptr = *sptr;

            len ^= sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
            ++sptr;
            ++dptr;
        }

#if defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
        if ((len & (2 * sizeof(SIZE_T))) != 0)
        {
            __m128 v = _mm_loadu_ps((float *)sptr);
            _mm_storeu_ps((float *)dptr, v);

            len ^= 2 * sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
            sptr += 2;
            dptr += 2;
        }

        // Align the destination pointer to 16 bytes for the next set of 16-byte copies
        if (((SIZE_T)dptr & sizeof(SIZE_T)) != 0)
        {
            *dptr = *sptr;

            ++sptr;
            ++dptr;
            len -= sizeof(SIZE_T);
            if (len < 4 * sizeof(SIZE_T))
            {
                continue;
            }
        }

        // Copy 32 bytes at a time
        _ASSERTE(len >= 4 * sizeof(SIZE_T));
        do
        {
            __m128 v = _mm_loadu_ps((float *)sptr);
            _mm_store_ps((float *)dptr, v);
            v = _mm_loadu_ps((float *)(sptr + 2));
            _mm_store_ps((float *)(dptr + 2), v);

            sptr += 4;
            dptr += 4;
            len -= 4 * sizeof(SIZE_T);
        } while (len >= 4 * sizeof(SIZE_T));
        if (len == 0)
        {
            return;
        }
#else // !(defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__)))
        if ((len & (2 * sizeof(SIZE_T))) != 0)
        {
            // Read two values and write two values to hint the use of wide loads and stores
            SIZE_T p0 = sptr[0];
            SIZE_T p1 = sptr[1];
            dptr[0] = p0;
            dptr[1] = p1;

            len ^= 2 * sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
            sptr += 2;
            dptr += 2;
        }

        // Copy 16 (on 32-bit systems) or 32 (on 64-bit systems) bytes at a time
        _ASSERTE(len >= 4 * sizeof(SIZE_T));
        while (true)
        {
            // Read two values and write two values to hint the use of wide loads and stores
            SIZE_T p0 = sptr[0];
            SIZE_T p1 = sptr[1];
            dptr[0] = p0;
            dptr[1] = p1;
            p0 = sptr[2];
            p1 = sptr[3];
            dptr[2] = p0;
            dptr[3] = p1;

            len -= 4 * sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
            sptr += 4;
            dptr += 4;
        }
#endif // defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
    }
}

FORCEINLINE void InlinedBackwardGCSafeCopyHelper(void *dest, const void *src, size_t len)
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

    // To be able to copy backwards, the source buffer cannot start inside the destination buffer
    _ASSERTE((SIZE_T)src - (SIZE_T)dest >= len);

    // Make sure everything is pointer aligned
    _ASSERTE(IS_ALIGNED(dest, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(src, sizeof(SIZE_T)));
    _ASSERTE(IS_ALIGNED(len, sizeof(SIZE_T)));

    _ASSERTE(CheckPointer(dest));
    _ASSERTE(CheckPointer(src));

    SIZE_T *dptr = (SIZE_T *)((BYTE *)dest + len);
    SIZE_T *sptr = (SIZE_T *)((BYTE *)src + len);

    while (true)
    {
        if ((len & sizeof(SIZE_T)) != 0)
        {
            --sptr;
            --dptr;

            *dptr = *sptr;

            len ^= sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
        }

#if defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
        if ((len & (2 * sizeof(SIZE_T))) != 0)
        {
            sptr -= 2;
            dptr -= 2;

            __m128 v = _mm_loadu_ps((float *)sptr);
            _mm_storeu_ps((float *)dptr, v);

            len ^= 2 * sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
        }

        // Align the destination pointer to 16 bytes for the next set of 16-byte copies
        if (((SIZE_T)dptr & sizeof(SIZE_T)) != 0)
        {
            --sptr;
            --dptr;

            *dptr = *sptr;

            len -= sizeof(SIZE_T);
            if (len < 4 * sizeof(SIZE_T))
            {
                continue;
            }
        }

        // Copy 32 bytes at a time
        _ASSERTE(len >= 4 * sizeof(SIZE_T));
        do
        {
            sptr -= 4;
            dptr -= 4;

            __m128 v = _mm_loadu_ps((float *)(sptr + 2));
            _mm_store_ps((float *)(dptr + 2), v);
            v = _mm_loadu_ps((float *)sptr);
            _mm_store_ps((float *)dptr, v);

            len -= 4 * sizeof(SIZE_T);
        } while (len >= 4 * sizeof(SIZE_T));
        if (len == 0)
        {
            return;
        }
#else // !(defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__)))
        if ((len & (2 * sizeof(SIZE_T))) != 0)
        {
            sptr -= 2;
            dptr -= 2;

            // Read two values and write two values to hint the use of wide loads and stores
            SIZE_T p1 = sptr[1];
            SIZE_T p0 = sptr[0];
            dptr[1] = p1;
            dptr[0] = p0;

            len ^= 2 * sizeof(SIZE_T);
            if (len == 0)
            {
                return;
            }
        }

        // Copy 16 (on 32-bit systems) or 32 (on 64-bit systems) bytes at a time
        _ASSERTE(len >= 4 * sizeof(SIZE_T));
        do
        {
            sptr -= 4;
            dptr -= 4;

            // Read two values and write two values to hint the use of wide loads and stores
            SIZE_T p0 = sptr[2];
            SIZE_T p1 = sptr[3];
            dptr[2] = p0;
            dptr[3] = p1;
            p0 = sptr[0];
            p1 = sptr[1];
            dptr[0] = p0;
            dptr[1] = p1;

            len -= 4 * sizeof(SIZE_T);
        } while (len != 0);
        return;
#endif // defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
    }
}

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

    GCHeapMemoryBarrier();

    // To be able to copy forwards, the destination buffer cannot start inside the source buffer
    if ((size_t)dest - (size_t)src >= len)
    {
        InlinedForwardGCSafeCopyHelper(dest, src, len);
    }
    else
    {
        InlinedBackwardGCSafeCopyHelper(dest, src, len);
    }

    InlinedSetCardsAfterBulkCopyHelper((Object**)dest, len);
}

#endif // !_ARRAYNATIVE_INL_

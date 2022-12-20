// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of Redhawk PAL inline functions

#include <errno.h>

FORCEINLINE int32_t PalInterlockedIncrement(_Inout_ int32_t volatile *pDst)
{
    return __sync_add_and_fetch(pDst, 1);
}

FORCEINLINE int32_t PalInterlockedDecrement(_Inout_ int32_t volatile *pDst)
{
    return __sync_sub_and_fetch(pDst, 1);
}

FORCEINLINE uint32_t PalInterlockedOr(_Inout_ uint32_t volatile *pDst, uint32_t iValue)
{
    return __sync_or_and_fetch(pDst, iValue);
}

FORCEINLINE uint32_t PalInterlockedAnd(_Inout_ uint32_t volatile *pDst, uint32_t iValue)
{
    return __sync_and_and_fetch(pDst, iValue);
}

FORCEINLINE int32_t PalInterlockedExchange(_Inout_ int32_t volatile *pDst, int32_t iValue)
{
#ifdef __clang__
    return __sync_swap(pDst, iValue);
#else
    return __atomic_exchange_n(pDst, iValue, __ATOMIC_ACQ_REL);
#endif
}

FORCEINLINE int64_t PalInterlockedExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue)
{
#ifdef __clang__
    return __sync_swap(pDst, iValue);
#else
    return __atomic_exchange_n(pDst, iValue, __ATOMIC_ACQ_REL);
#endif
}

FORCEINLINE int32_t PalInterlockedCompareExchange(_Inout_ int32_t volatile *pDst, int32_t iValue, int32_t iComparand)
{
    return __sync_val_compare_and_swap(pDst, iComparand, iValue);
}

FORCEINLINE int64_t PalInterlockedCompareExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue, int64_t iComparand)
{
    return __sync_val_compare_and_swap(pDst, iComparand, iValue);
}

#if defined(HOST_AMD64) || defined(HOST_ARM64)
FORCEINLINE uint8_t PalInterlockedCompareExchange128(_Inout_ int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparandAndResult)
{
    __int128_t iComparand = ((__int128_t)pComparandAndResult[1] << 64) + (uint64_t)pComparandAndResult[0];
    __int128_t iResult = __sync_val_compare_and_swap((__int128_t volatile*)pDst, iComparand, ((__int128_t)iValueHigh << 64) + (uint64_t)iValueLow);
    pComparandAndResult[0] = (int64_t)iResult; pComparandAndResult[1] = (int64_t)(iResult >> 64);
    return iComparand == iResult;
}
#endif // HOST_AMD64

#ifdef HOST_64BIT

#define PalInterlockedExchangePointer(_pDst, _pValue) \
    ((void *)PalInterlockedExchange64((int64_t volatile *)(_pDst), (int64_t)(size_t)(_pValue)))

#define PalInterlockedCompareExchangePointer(_pDst, _pValue, _pComparand) \
    ((void *)PalInterlockedCompareExchange64((int64_t volatile *)(_pDst), (int64_t)(size_t)(_pValue), (int64_t)(size_t)(_pComparand)))

#else // HOST_64BIT

#define PalInterlockedExchangePointer(_pDst, _pValue) \
    ((void *)PalInterlockedExchange((int32_t volatile *)(_pDst), (int32_t)(size_t)(_pValue)))

#define PalInterlockedCompareExchangePointer(_pDst, _pValue, _pComparand) \
    ((void *)PalInterlockedCompareExchange((int32_t volatile *)(_pDst), (int32_t)(size_t)(_pValue), (int32_t)(size_t)(_pComparand)))

#endif // HOST_64BIT


FORCEINLINE void PalYieldProcessor()
{
#if defined(HOST_X86) || defined(HOST_AMD64)
    __asm__ __volatile__(
        "rep\n"
        "nop"
        );
#elif defined(HOST_ARM64)
    __asm__ __volatile__(
        "dmb ishst\n"
        "yield"
        );
#endif
}

FORCEINLINE void PalMemoryBarrier()
{
    __sync_synchronize();
}

#define PalDebugBreak() abort()

FORCEINLINE int32_t PalGetLastError()
{
    return errno;
}

FORCEINLINE void PalSetLastError(int32_t error)
{
    errno = error;
}

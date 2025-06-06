// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of NativeAOT PAL inline functions

#include <errno.h>

FORCEINLINE void PalInterlockedOperationBarrier()
{
#if (defined(HOST_ARM64) && !defined(LSE_INSTRUCTIONS_ENABLED_BY_DEFAULT) && !defined(__clang__)) || defined(HOST_LOONGARCH64) || defined(HOST_RISCV64)
    // On arm64, most of the __sync* functions generate a code sequence like:
    //   loop:
    //     ldaxr (load acquire exclusive)
    //     ...
    //     stlxr (store release exclusive)
    //     cbnz loop
    //
    // It is possible for a load following the code sequence above to be reordered to occur prior to the store above due to the
    // release barrier, this is substantiated by https://github.com/dotnet/coreclr/pull/17508. Interlocked operations in the PAL
    // require the load to occur after the store. This memory barrier should be used following a call to a __sync* function to
    // prevent that reordering. Code generated for arm32 includes a 'dmb' after 'cbnz', so no issue there at the moment.
    __sync_synchronize();
#endif
}

FORCEINLINE int32_t PalInterlockedIncrement(_Inout_ int32_t volatile *pDst)
{
    int32_t result = __sync_add_and_fetch(pDst, 1);
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE int64_t PalInterlockedIncrement64(_Inout_ int64_t volatile *pDst)
{
    int64_t result = __sync_add_and_fetch(pDst, 1);
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE int32_t PalInterlockedDecrement(_Inout_ int32_t volatile *pDst)
{
    int32_t result = __sync_sub_and_fetch(pDst, 1);
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE uint32_t PalInterlockedOr(_Inout_ uint32_t volatile *pDst, uint32_t iValue)
{
    int32_t result = __sync_or_and_fetch(pDst, iValue);
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE uint32_t PalInterlockedAnd(_Inout_ uint32_t volatile *pDst, uint32_t iValue)
{
    int32_t result = __sync_and_and_fetch(pDst, iValue);
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE int32_t PalInterlockedExchange(_Inout_ int32_t volatile *pDst, int32_t iValue)
{
#ifdef __clang__
    int32_t result =__sync_swap(pDst, iValue);
#else
    int32_t result =__atomic_exchange_n(pDst, iValue, __ATOMIC_ACQ_REL);
#endif
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE int64_t PalInterlockedExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue)
{
#ifdef __clang__
    int32_t result =__sync_swap(pDst, iValue);
#else
    int32_t result =__atomic_exchange_n(pDst, iValue, __ATOMIC_ACQ_REL);
#endif
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE int32_t PalInterlockedCompareExchange(_Inout_ int32_t volatile *pDst, int32_t iValue, int32_t iComparand)
{
    int32_t result = __sync_val_compare_and_swap(pDst, iComparand, iValue);
    PalInterlockedOperationBarrier();
    return result;
}

FORCEINLINE int64_t PalInterlockedCompareExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue, int64_t iComparand)
{
    int64_t result = __sync_val_compare_and_swap(pDst, iComparand, iValue);
    PalInterlockedOperationBarrier();
    return result;
}

#if defined(HOST_64BIT)
FORCEINLINE uint8_t PalInterlockedCompareExchange128(_Inout_ int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparandAndResult)
{
    __int128_t iComparand = ((__int128_t)pComparandAndResult[1] << 64) + (uint64_t)pComparandAndResult[0];

    // TODO-LOONGARCH64: the 128-bit CAS is supported starting from the 3A6000 CPU (ISA1.1).
    // When running on older hardware that doesn't support native CAS-128, the system falls back
    // to a mutex-based approach via libatomic, which is not suitable for runtime requirements.
    //
    // TODO-RISCV64: double-check if libatomic's emulated CAS-128 works as expected once AOT applications are
    // functional on linux-riscv64: https://github.com/dotnet/runtime/issues/106223.
    // CAS-128 is natively supported starting with the Zacas extension in Linux 6.8; however, hardware support
    // for RVA23 profile is not available at the time of writing.
    //
    // See https://github.com/dotnet/runtime/issues/109276.

    __int128_t iResult = __sync_val_compare_and_swap((__int128_t volatile*)pDst, iComparand, ((__int128_t)iValueHigh << 64) + (uint64_t)iValueLow);
    PalInterlockedOperationBarrier();
    pComparandAndResult[0] = (int64_t)iResult; pComparandAndResult[1] = (int64_t)(iResult >> 64);
    return iComparand == iResult;
}
#endif // HOST_64BIT

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

FORCEINLINE int32_t PalOsPageSize()
{
#if defined(HOST_AMD64)
    // all supported platforms use 4K pages on x64, including emulated environments
    return 0x1000;
#elif defined(HOST_APPLE)
    // OSX and related OS expose 16-kilobyte pages to the 64-bit userspace
    // https://developer.apple.com/library/archive/documentation/Performance/Conceptual/ManagingMemory/Articles/AboutMemory.html
    return 0x4000;
#else
    return PalGetOsPageSize();
#endif
}

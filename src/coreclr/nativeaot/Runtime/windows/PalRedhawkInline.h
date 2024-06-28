// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(HOST_ARM64)
#include <arm64intr.h>
#endif
#define _INC_WINDOWS
#include <windows.h>

// Implementation of Redhawk PAL inline functions

FORCEINLINE int32_t PalInterlockedIncrement(_Inout_ int32_t volatile *pDst)
{
    return InterlockedIncrement((long volatile *)pDst);
}

FORCEINLINE int32_t PalInterlockedDecrement(_Inout_ int32_t volatile *pDst)
{
    return InterlockedDecrement((long volatile *)pDst);
}

FORCEINLINE uint32_t PalInterlockedOr(_Inout_ uint32_t volatile *pDst, uint32_t iValue)
{
    return InterlockedOr((long volatile *)pDst, iValue);
}

FORCEINLINE uint32_t PalInterlockedAnd(_Inout_ uint32_t volatile *pDst, uint32_t iValue)
{
    return InterlockedAnd((long volatile *)pDst, iValue);
}

FORCEINLINE int32_t PalInterlockedExchange(_Inout_ int32_t volatile *pDst, int32_t iValue)
{
    return InterlockedExchange((long volatile *)pDst, iValue);
}

FORCEINLINE int64_t PalInterlockedExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue)
{
    return InterlockedExchange64(pDst, iValue);
}

FORCEINLINE int32_t PalInterlockedCompareExchange(_Inout_ int32_t volatile *pDst, int32_t iValue, int32_t iComparand)
{
    return InterlockedCompareExchange((long volatile *)pDst, iValue, iComparand);
}

FORCEINLINE int64_t PalInterlockedCompareExchange64(_Inout_ int64_t volatile *pDst, int64_t iValue, int64_t iComparand)
{
    return InterlockedCompareExchange64(pDst, iValue, iComparand);
}

#if defined(HOST_AMD64) || defined(HOST_ARM64)
FORCEINLINE uint8_t PalInterlockedCompareExchange128(_Inout_ int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparandAndResult)
{
    return InterlockedCompareExchange128(pDst, iValueHigh, iValueLow, pComparandAndResult);
}
#endif // HOST_AMD64

#ifdef HOST_64BIT

FORCEINLINE void * PalInterlockedExchangePointer(_Inout_ void * volatile *pDst, _In_ void *pValue)
{
    return InterlockedExchangePointer((void * volatile *)pDst, pValue);
}

FORCEINLINE void * PalInterlockedCompareExchangePointer(_Inout_ void * volatile *pDst, _In_ void *pValue, _In_ void *pComparand)
{
    return InterlockedCompareExchangePointer((void * volatile *)pDst, pValue, pComparand);
}

#else // HOST_64BIT

#define PalInterlockedExchangePointer(_pDst, _pValue) \
    ((void *)InterlockedExchange((long volatile *)(_pDst), (long)(size_t)(_pValue)))

#define PalInterlockedCompareExchangePointer(_pDst, _pValue, _pComparand) \
    ((void *)InterlockedCompareExchange((long volatile *)(_pDst), (long)(size_t)(_pValue), (long)(size_t)(_pComparand)))

#endif // HOST_64BIT

EXTERN_C __declspec(dllimport) unsigned long __stdcall GetLastError();
FORCEINLINE int PalGetLastError()
{
    return (int)GetLastError();
}

EXTERN_C __declspec(dllimport) void  __stdcall SetLastError(unsigned long error);
FORCEINLINE void PalSetLastError(int error)
{
    SetLastError((unsigned long)error);
}

#if defined(HOST_X86)

EXTERN_C void _mm_pause();
#pragma intrinsic(_mm_pause)
#define PalYieldProcessor() _mm_pause()

FORCEINLINE void PalMemoryBarrier()
{
    long Barrier;
    InterlockedOr(&Barrier, 0);
}

#elif defined(HOST_AMD64)

EXTERN_C void _mm_pause();
#pragma intrinsic(_mm_pause)
#define PalYieldProcessor() _mm_pause()

EXTERN_C void __faststorefence();
#pragma intrinsic(__faststorefence)
#define PalMemoryBarrier() __faststorefence()

#elif defined(HOST_ARM64)

EXTERN_C void __yield(void);
#pragma intrinsic(__yield)
EXTERN_C void __dmb(unsigned int _Type);
#pragma intrinsic(__dmb)
FORCEINLINE void PalYieldProcessor()
{
    __dmb(_ARM64_BARRIER_ISHST);
    __yield();
}

#define PalMemoryBarrier() __dmb(_ARM64_BARRIER_ISH)

#else
#error Unsupported architecture
#endif

#define PalDebugBreak() __debugbreak()

FORCEINLINE int32_t PalOsPageSize()
{
    return 0x1000;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

                                                      


#ifndef __UNSAFE_H__
#define __UNSAFE_H__

// should we just check proper inclusion?
#include <winwrap.h>

#include "staticcontract.h"

inline VOID UnsafeEnterCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    STATIC_CONTRACT_LEAF;
    EnterCriticalSection(lpCriticalSection);
}

inline VOID UnsafeLeaveCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    STATIC_CONTRACT_LEAF;
    LeaveCriticalSection(lpCriticalSection);
}

inline BOOL UnsafeTryEnterCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    STATIC_CONTRACT_LEAF;
    return TryEnterCriticalSection(lpCriticalSection);
}

inline VOID UnsafeInitializeCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    STATIC_CONTRACT_LEAF;
    InitializeCriticalSection(lpCriticalSection);
}

inline VOID UnsafeDeleteCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    STATIC_CONTRACT_LEAF;
    DeleteCriticalSection(lpCriticalSection);
}

inline HANDLE UnsafeCreateEvent(LPSECURITY_ATTRIBUTES lpEventAttributes, BOOL bManualReset, BOOL bInitialState, LPCWSTR lpName)
{
    STATIC_CONTRACT_WRAPPER;
    return WszCreateEvent(lpEventAttributes, bManualReset, bInitialState, lpName);
}

inline BOOL UnsafeSetEvent(HANDLE hEvent)
{
    STATIC_CONTRACT_LEAF;
    return SetEvent(hEvent);
}

inline BOOL UnsafeResetEvent(HANDLE hEvent)
{
    STATIC_CONTRACT_LEAF;
    return ResetEvent(hEvent);
}

inline HANDLE UnsafeCreateSemaphore(LPSECURITY_ATTRIBUTES lpSemaphoreAttributes, LONG lInitialCount, LONG lMaximumCount, LPCWSTR lpName)
{
    STATIC_CONTRACT_WRAPPER;
    return WszCreateSemaphore(lpSemaphoreAttributes, lInitialCount, lMaximumCount, lpName);
}

inline BOOL UnsafeReleaseSemaphore(HANDLE hSemaphore, LONG lReleaseCount, LPLONG lpPreviousCount)
{
    STATIC_CONTRACT_LEAF;
    return ReleaseSemaphore(hSemaphore, lReleaseCount, lpPreviousCount);
}

inline LPVOID UnsafeTlsGetValue(DWORD dwTlsIndex)
{
    STATIC_CONTRACT_LEAF;
    return TlsGetValue(dwTlsIndex);
}

inline BOOL UnsafeTlsSetValue(DWORD dwTlsIndex, LPVOID lpTlsValue)
{
    STATIC_CONTRACT_LEAF;
    return TlsSetValue(dwTlsIndex, lpTlsValue);
}

inline DWORD UnsafeTlsAlloc(void) 
{
    STATIC_CONTRACT_LEAF;
    return TlsAlloc();
}

inline BOOL UnsafeTlsFree(DWORD dwTlsIndex) 
{
    STATIC_CONTRACT_LEAF;
    return TlsFree(dwTlsIndex);
}

#endif



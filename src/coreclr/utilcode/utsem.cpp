// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/******************************************************************************
    FILE : UTSEM.CPP



    Purpose: Part of the utilities library for the VIPER project

    Abstract : Implements the UTSemReadWrite class.
-------------------------------------------------------------------------------
Revision History:


*******************************************************************************/
#include "stdafx.h"
#include "clrhost.h"
#include "ex.h"

#include <utsem.h>
#include "contract.h"

// Consider replacing this with a #ifdef INTEROP_DEBUGGING
#if !defined(SELF_NO_HOST) && defined(TARGET_X86) && !defined(TARGET_UNIX)
// For Interop debugging, the UTSemReadWrite class must inform the debugger
// that this thread can't be suspended currently.  See vm\util.hpp for the
// implementation of these methods.
void IncCantStopCount();
void DecCantStopCount();
#else
#define IncCantStopCount()
#define DecCantStopCount()
#endif  // !SELF_NO_HOST && TARGET_X86

/******************************************************************************
Definitions of the bit fields in UTSemReadWrite::m_dwFlag:

Warning: The code assume that READER_MASK is in the low-order bits of the DWORD.
******************************************************************************/

const ULONG READERS_MASK      = 0x000003FF;    // field that counts number of readers
const ULONG READERS_INCR      = 0x00000001;    // amount to add to increment number of readers

// The following field is 2 bits long to make it easier to catch errors.
// (If the number of writers ever exceeds 1, we've got problems.)
const ULONG WRITERS_MASK      = 0x00000C00;    // field that counts number of writers
const ULONG WRITERS_INCR      = 0x00000400;    // amount to add to increment number of writers

const ULONG READWAITERS_MASK  = 0x003FF000;    // field that counts number of threads waiting to read
const ULONG READWAITERS_INCR  = 0x00001000;    // amount to add to increment number of read waiters

const ULONG WRITEWAITERS_MASK = 0xFFC00000;    // field that counts number of threads waiting to write
const ULONG WRITEWAITERS_INCR = 0x00400000;    // amount to add to increment number of write waiters

// ======================================================================================
// Spinning support

// Copy of definition from file:..\VM\spinlock.h
#define CALLER_LIMITS_SPINNING 0

#if (defined(SELF_NO_HOST)) || (defined(TARGET_UNIX) && defined(DACCESS_COMPILE))

// When we do not have host, we just call OS - see file:..\VM\hosting.cpp#__SwitchToThread
BOOL __SwitchToThread(DWORD dwSleepMSec, DWORD dwSwitchCount)
{
    // This is just simple implementation that does not support full dwSwitchCount arg
    _ASSERTE(dwSwitchCount == CALLER_LIMITS_SPINNING);
    return SwitchToThread();
}

Volatile<BOOL> g_fInitializedGlobalSystemInfo = FALSE;

// Global System Information
SYSTEM_INFO g_SystemInfo;

// Configurable constants used across our spin locks
SpinConstants g_SpinConstants = {
    50,        // dwInitialDuration
    40000,     // dwMaximumDuration - ideally (20000 * max(2, numProc)) ... updated in code:InitializeSpinConstants_NoHost
    3,         // dwBackoffFactor
    10,        // dwRepetitions
    0          // dwMonitorSpinCount
};

inline void InitializeSpinConstants_NoHost()
{
    g_SpinConstants.dwMaximumDuration = max(2, g_SystemInfo.dwNumberOfProcessors) * 20000;
}

#else //!SELF_NO_HOST

// Use VM/CrossGen functions and variables
BOOL __SwitchToThread (DWORD dwSleepMSec, DWORD dwSwitchCount);
extern SYSTEM_INFO g_SystemInfo;
extern SpinConstants g_SpinConstants;

#endif //!SELF_NO_HOST

/******************************************************************************
Function : UTSemReadWrite::UTSemReadWrite

Abstract: Constructor.
******************************************************************************/
UTSemReadWrite::UTSemReadWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if defined(SELF_NO_HOST)
    if (!g_fInitializedGlobalSystemInfo)
    {
        GetSystemInfo(&g_SystemInfo);
        InitializeSpinConstants_NoHost();

        g_fInitializedGlobalSystemInfo = TRUE;
    }
#endif //SELF_NO_HOST

    m_dwFlag = 0;
    m_hReadWaiterSemaphore = NULL;
    m_hWriteWaiterEvent = NULL;
}


/******************************************************************************
Function : UTSemReadWrite::~UTSemReadWrite

Abstract: Destructor
******************************************************************************/
UTSemReadWrite::~UTSemReadWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE_MSG((m_dwFlag == (ULONG)0), "Destroying a UTSemReadWrite while in use");

    if (m_hReadWaiterSemaphore != NULL)
        CloseHandle(m_hReadWaiterSemaphore);

    if (m_hWriteWaiterEvent != NULL)
        CloseHandle(m_hWriteWaiterEvent);
}

//=======================================================================================
//
// Initialize the lock (its semaphore and event)
//
HRESULT
UTSemReadWrite::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;


    _ASSERTE(m_hReadWaiterSemaphore == NULL);
    _ASSERTE(m_hWriteWaiterEvent == NULL);

    m_hReadWaiterSemaphore = WszCreateSemaphore(NULL, 0, MAXLONG, NULL);
    IfNullRet(m_hReadWaiterSemaphore);

    m_hWriteWaiterEvent = WszCreateEvent(NULL, FALSE, FALSE, NULL);
    IfNullRet(m_hWriteWaiterEvent);

    return S_OK;
} // UTSemReadWrite::Init

/******************************************************************************
Function : UTSemReadWrite::LockRead

Abstract: Obtain a shared lock
******************************************************************************/
HRESULT UTSemReadWrite::LockRead()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Inform CLR that the debugger shouldn't suspend this thread while
    // holding this lock.
    IncCantStopCount();

    // First do some spinning - copied from file:..\VM\crst.cpp#CrstBase::SpinEnter
    for (DWORD iter = 0; iter < g_SpinConstants.dwRepetitions; iter++)
    {
        DWORD i = g_SpinConstants.dwInitialDuration;

        do
        {
            DWORD dwFlag = m_dwFlag;

            if (dwFlag < READERS_MASK)
            {   // There are just readers in the play, try to add one more
                if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, dwFlag + READERS_INCR, dwFlag))
                {
                    goto ReadLockAcquired;
                }
            }

            if (g_SystemInfo.dwNumberOfProcessors <= 1)
            {   // We do not need to spin on a single processor
                break;
            }

            // Delay by approximately 2*i clock cycles (Pentium III).
            YieldProcessorNormalizedForPreSkylakeCount(i);

            // exponential backoff: wait a factor longer in the next iteration
            i *= g_SpinConstants.dwBackoffFactor;
        } while (i < g_SpinConstants.dwMaximumDuration);

        __SwitchToThread(0, CALLER_LIMITS_SPINNING);
    }
    // Stop spinning

    // Start waiting
    for (;;)
    {
        DWORD dwFlag = m_dwFlag;

        if (dwFlag < READERS_MASK)
        {   // There are just readers in the play, try to add one more
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, dwFlag + READERS_INCR, dwFlag))
            {
                break;
            }
        }
        else if ((dwFlag & READERS_MASK) == READERS_MASK)
        {   // The number of readers has reached the maximum (0x3ff), wait 1s
            ClrSleepEx(1000, FALSE);
        }
        else if ((dwFlag & READWAITERS_MASK) == READWAITERS_MASK)
        {   // The number of readers waiting on semaphore has reached the maximum (0x3ff), wait 1s
            ClrSleepEx(1000, FALSE);
        }
        else
        {   // Try to add waiting reader and then wait for signal
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, dwFlag + READWAITERS_INCR, dwFlag))
            {
                WaitForSingleObjectEx(m_hReadWaiterSemaphore, INFINITE, FALSE);
                break;
            }
        }
    }

ReadLockAcquired:
    _ASSERTE ((m_dwFlag & READERS_MASK) != 0 && "reader count is zero after acquiring read lock");
    _ASSERTE ((m_dwFlag & WRITERS_MASK) == 0 && "writer count is nonzero after acquiring write lock");
    EE_LOCK_TAKEN(this);

    return S_OK;
} // UTSemReadWrite::LockRead



/******************************************************************************
Function : UTSemReadWrite::LockWrite

Abstract: Obtain an exclusive lock
******************************************************************************/
HRESULT UTSemReadWrite::LockWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // Inform CLR that the debugger shouldn't suspend this thread while
    // holding this lock.
    IncCantStopCount();

    // First do some spinning - copied from file:..\VM\crst.cpp#CrstBase::SpinEnter
    for (DWORD iter = 0; iter < g_SpinConstants.dwRepetitions; iter++)
    {
        DWORD i = g_SpinConstants.dwInitialDuration;

        do
        {
            DWORD dwFlag = m_dwFlag;

            if (dwFlag == 0)
            {   // No readers/writers in play, try to add a writer
                if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, WRITERS_INCR, dwFlag))
                {
                    goto WriteLockAcquired;
                }
            }

            if (g_SystemInfo.dwNumberOfProcessors <= 1)
            {   // We do not need to spin on a single processor
                break;
            }

            // Delay by approximately 2*i clock cycles (Pentium III).
            YieldProcessorNormalizedForPreSkylakeCount(i);

            // exponential backoff: wait a factor longer in the next iteration
            i *= g_SpinConstants.dwBackoffFactor;
        } while (i < g_SpinConstants.dwMaximumDuration);

        __SwitchToThread(0, CALLER_LIMITS_SPINNING);
    }
    // Stop spinning

    // Start waiting
    for (;;)
    {
        DWORD dwFlag = m_dwFlag;

        if (dwFlag == 0)
        {   // No readers/writers in play, try to add a writer
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, WRITERS_INCR, dwFlag))
            {
                break;
            }
        }
        else if ((dwFlag & WRITEWAITERS_MASK) == WRITEWAITERS_MASK)
        {   // The number of writers waiting on semaphore has reached the maximum (0x3ff), wait 1s
            ClrSleepEx(1000, FALSE);
        }
        else
        {   // Try to add waiting writer and then wait for signal
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, dwFlag + WRITEWAITERS_INCR, dwFlag))
            {
                WaitForSingleObjectEx(m_hWriteWaiterEvent, INFINITE, FALSE);
                break;
            }
        }

    }

WriteLockAcquired:
    _ASSERTE ((m_dwFlag & READERS_MASK) == 0 && "reader count is nonzero after acquiring write lock");
    _ASSERTE ((m_dwFlag & WRITERS_MASK) == WRITERS_INCR && "writer count is not 1 after acquiring write lock");
    EE_LOCK_TAKEN(this);

    return S_OK;
} // UTSemReadWrite::LockWrite



/******************************************************************************
Function : UTSemReadWrite::UnlockRead

Abstract: Release a shared lock
******************************************************************************/
void UTSemReadWrite::UnlockRead()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ULONG dwFlag;


    _ASSERTE ((m_dwFlag & READERS_MASK) != 0 && "reader count is zero before releasing read lock");
    _ASSERTE ((m_dwFlag & WRITERS_MASK) == 0 && "writer count is nonzero before releasing read lock");

    for (;;)
    {
        dwFlag = m_dwFlag;

        if (dwFlag == READERS_INCR)
        {        // we're the last reader, and nobody is waiting
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, (ULONG)0, dwFlag))
            {
                break;
            }
        }

        else if ((dwFlag & READERS_MASK) > READERS_INCR)
        {        // we're not the last reader
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, dwFlag - READERS_INCR, dwFlag))
            {
                break;
            }
        }

        else
        {
            // here, there should be exactly 1 reader (us), and at least one waiting writer.
            _ASSERTE ((dwFlag & READERS_MASK) == READERS_INCR && "UnlockRead consistency error 1");
            _ASSERTE ((dwFlag & WRITEWAITERS_MASK) != 0 && "UnlockRead consistency error 2");

            // one or more writers is waiting, do one of them next
            // (remove a reader (us), remove a write waiter, add a writer
            if (dwFlag ==
                    InterlockedCompareExchangeT(
                        &m_dwFlag,
                        dwFlag - READERS_INCR - WRITEWAITERS_INCR + WRITERS_INCR,
                        dwFlag))
            {
                SetEvent(m_hWriteWaiterEvent);
                break;
            }
        }
    }

    DecCantStopCount();
    EE_LOCK_RELEASED(this);
} // UTSemReadWrite::UnlockRead


/******************************************************************************
Function : UTSemReadWrite::UnlockWrite

Abstract: Release an exclusive lock
******************************************************************************/
void UTSemReadWrite::UnlockWrite()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ULONG dwFlag;
    ULONG count;

    _ASSERTE ((m_dwFlag & READERS_MASK) == 0 && "reader count is nonzero before releasing write lock");
    _ASSERTE ((m_dwFlag & WRITERS_MASK) == WRITERS_INCR && "writer count is not 1 before releasing write lock");

    for (;;)
    {
        dwFlag = m_dwFlag;

        if (dwFlag == WRITERS_INCR)
        {        // nobody is waiting
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, (ULONG)0, dwFlag))
            {
                break;
            }
        }

        else if ((dwFlag & READWAITERS_MASK) != 0)
        {        // one or more readers are waiting, do them all next
            count = (dwFlag & READWAITERS_MASK) / READWAITERS_INCR;
            // remove a writer (us), remove all read waiters, turn them into readers
            if (dwFlag ==
                    InterlockedCompareExchangeT(
                        &m_dwFlag,
                        dwFlag - WRITERS_INCR - count * READWAITERS_INCR + count * READERS_INCR,
                        dwFlag))
            {
                ReleaseSemaphore(m_hReadWaiterSemaphore, count, NULL);
                break;
            }
        }

        else
        {        // one or more writers is waiting, do one of them next
            _ASSERTE ((dwFlag & WRITEWAITERS_MASK) != 0 && "UnlockWrite consistency error");
                // (remove a writer (us), remove a write waiter, add a writer
            if (dwFlag == InterlockedCompareExchangeT (&m_dwFlag, dwFlag - WRITEWAITERS_INCR, dwFlag))
            {
                SetEvent(m_hWriteWaiterEvent);
                break;
            }
        }
    }

    DecCantStopCount();
    EE_LOCK_RELEASED(this);
} // UTSemReadWrite::UnlockWrite

#ifdef _DEBUG

//=======================================================================================
BOOL
UTSemReadWrite::Debug_IsLockedForRead()
{
    return ((m_dwFlag & READERS_MASK) != 0);
}

//=======================================================================================
BOOL
UTSemReadWrite::Debug_IsLockedForWrite()
{
    return ((m_dwFlag & WRITERS_MASK) != 0);
}

#endif //_DEBUG


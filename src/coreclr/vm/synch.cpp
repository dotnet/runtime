// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "common.h"

#include "corhost.h"
#include "synch.h"

void CLREventBase::CreateAutoEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    SetAutoEvent();

    {
        HANDLE h = WszCreateEvent(NULL,FALSE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }

}

BOOL CLREventBase::CreateAutoEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateAutoEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CreateManualEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    {
        HANDLE h = WszCreateEvent(NULL,TRUE,bInitialState,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
}

BOOL CLREventBase::CreateManualEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        // Can not assert here. ASP.NET uses our Threadpool before EE is started.
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateManualEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CreateMonitorEvent(SIZE_T Cookie)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((g_fEEStarted));
        PRECONDITION((GetThreadNULLOk() != NULL));
        PRECONDITION((!IsOSEvent()));
    }
    CONTRACTL_END;

    // thread-safe SetAutoEvent
    FastInterlockOr(&m_dwFlags, CLREVENT_FLAGS_AUTO_EVENT);

    {
        HANDLE h = WszCreateEvent(NULL,FALSE,FALSE,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        if (FastInterlockCompareExchangePointer(&m_handle,
                                                h,
                                                INVALID_HANDLE_VALUE) != INVALID_HANDLE_VALUE)
        {
            // We lost the race
            CloseHandle(h);
        }
    }

    // thread-safe SetInDeadlockDetection
    FastInterlockOr(&m_dwFlags, CLREVENT_FLAGS_IN_DEADLOCK_DETECTION);

    for (;;)
    {
        LONG oldFlags = m_dwFlags;

        if (oldFlags & CLREVENT_FLAGS_MONITOREVENT_ALLOCATED)
        {
            // Other thread has set the flag already. Nothing left for us to do.
            break;
        }

        LONG newFlags = oldFlags | CLREVENT_FLAGS_MONITOREVENT_ALLOCATED;
        if (FastInterlockCompareExchange((LONG*)&m_dwFlags, newFlags, oldFlags) != oldFlags)
        {
            // We lost the race
            continue;
        }

        // Because we set the allocated bit, we are the ones to do the signalling
        if (oldFlags & CLREVENT_FLAGS_MONITOREVENT_SIGNALLED)
        {
            // We got the honour to signal the event
            Set();
        }
        break;
    }
}


void CLREventBase::SetMonitorEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // SetMonitorEvent is robust against initialization races. It is possible to
    // call CLREvent::SetMonitorEvent on event that has not been initialialized yet by CreateMonitorEvent.
    // CreateMonitorEvent will signal the event once it is created if it happens.

    for (;;)
    {
        LONG oldFlags = m_dwFlags;

        if (oldFlags & CLREVENT_FLAGS_MONITOREVENT_ALLOCATED)
        {
            // Event has been allocated already. Use the regular codepath.
            Set();
            break;
        }

        LONG newFlags = oldFlags | CLREVENT_FLAGS_MONITOREVENT_SIGNALLED;
        if (FastInterlockCompareExchange((LONG*)&m_dwFlags, newFlags, oldFlags) != oldFlags)
        {
            // We lost the race
            continue;
        }
        break;
    }
}



void CLREventBase::CreateOSAutoEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    // Can not assert here. ASP.NET uses our Threadpool before EE is started.
    //_ASSERTE (g_fEEStarted);

    SetOSEvent();
    SetAutoEvent();

    HANDLE h = WszCreateEvent(NULL,FALSE,bInitialState,NULL);
    if (h == NULL) {
        ThrowOutOfMemory();
    }
    m_handle = h;
}

BOOL CLREventBase::CreateOSAutoEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateOSAutoEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CreateOSManualEvent (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    // Can not assert here. ASP.NET uses our Threadpool before EE is started.
    //_ASSERTE (g_fEEStarted);

    SetOSEvent();

    HANDLE h = WszCreateEvent(NULL,TRUE,bInitialState,NULL);
    if (h == NULL) {
        ThrowOutOfMemory();
    }
    m_handle = h;
}

BOOL CLREventBase::CreateOSManualEventNoThrow (BOOL bInitialState  // If TRUE, initial state is signalled
                                )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        // disallow creation of Crst before EE starts
        PRECONDITION((m_handle == INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    EX_TRY
    {
        CreateOSManualEvent(bInitialState);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return IsValid();
}

void CLREventBase::CloseEvent()
{
    CONTRACTL
    {
      NOTHROW;
      if (IsInDeadlockDetection()) {GC_TRIGGERS;} else {GC_NOTRIGGER;}
    }
    CONTRACTL_END;

    GCX_MAYBE_PREEMP(IsInDeadlockDetection() && IsValid());

    _ASSERTE(Thread::Debug_AllowCallout());

    if (m_handle != INVALID_HANDLE_VALUE) {
        {
            CloseHandle(m_handle);
        }

        m_handle = INVALID_HANDLE_VALUE;
    }
    m_dwFlags = 0;
}


BOOL CLREventBase::Set()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      PRECONDITION((m_handle != INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    {
        return SetEvent(m_handle);
    }

}


BOOL CLREventBase::Reset()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      PRECONDITION((m_handle != INVALID_HANDLE_VALUE));
    }
    CONTRACTL_END;

    _ASSERTE(Thread::Debug_AllowCallout());

    {
        return ResetEvent(m_handle);
    }
}


static DWORD CLREventWaitHelper2(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_THROWS;

    return WaitForSingleObjectEx(handle,dwMilliseconds,alertable);
}

static DWORD CLREventWaitHelper(HANDLE handle, DWORD dwMilliseconds, BOOL alertable)
{
    STATIC_CONTRACT_NOTHROW;

    struct Param
    {
        HANDLE handle;
        DWORD dwMilliseconds;
        BOOL alertable;
        DWORD result;
    } param;
    param.handle = handle;
    param.dwMilliseconds = dwMilliseconds;
    param.alertable = alertable;
    param.result = WAIT_FAILED;

    // Can not use EX_TRY/CATCH.  EX_CATCH toggles GC mode.  This function is called
    // through RareDisablePreemptiveGC.  EX_CATCH breaks profiler callback.
    PAL_TRY(Param *, pParam, &param)
    {
        // Need to move to another helper (cannot have SEH and C++ destructors
        // on automatic variables in one function)
        pParam->result = CLREventWaitHelper2(pParam->handle, pParam->dwMilliseconds, pParam->alertable);
    }
    PAL_EXCEPT (EXCEPTION_EXECUTE_HANDLER)
    {
        param.result = WAIT_FAILED;
    }
    PAL_ENDTRY;

    return param.result;
}


DWORD CLREventBase::Wait(DWORD dwMilliseconds, BOOL alertable, PendingSync *syncState)
{
    WRAPPER_NO_CONTRACT;
    return WaitEx(dwMilliseconds, alertable?WaitMode_Alertable:WaitMode_None,syncState);
}


DWORD CLREventBase::WaitEx(DWORD dwMilliseconds, WaitMode mode, PendingSync *syncState)
{
    BOOL alertable = (mode & WaitMode_Alertable)!=0;
    CONTRACTL
    {
        if (alertable)
        {
            THROWS;               // Thread::DoAppropriateWait can throw
        }
        else
        {
            NOTHROW;
        }
        if (GetThreadNULLOk())
        {
            if (alertable)
                GC_TRIGGERS;
            else
                GC_NOTRIGGER;
        }
        else
        {
            DISABLED(GC_TRIGGERS);
        }
        PRECONDITION(m_handle != INVALID_HANDLE_VALUE); // Handle has to be valid
    }
    CONTRACTL_END;


    _ASSERTE(Thread::Debug_AllowCallout());

    Thread * pThread = GetThreadNULLOk();

#ifdef _DEBUG
    // If a CLREvent is OS event only, we can not wait for the event on a managed thread
    if (IsOSEvent())
        _ASSERTE (pThread == NULL);
#endif
    _ASSERTE((pThread != NULL) || !g_fEEStarted || dbgOnly_IsSpecialEEThread());

    {
        if (pThread && alertable) {
            DWORD dwRet = WAIT_FAILED;
            dwRet = pThread->DoAppropriateWait(1, &m_handle, FALSE, dwMilliseconds,
                                              mode,
                                              syncState);
            return dwRet;
        }
        else {
            _ASSERTE (syncState == NULL);
            return CLREventWaitHelper(m_handle,dwMilliseconds,alertable);
        }
    }
}

void CLRSemaphore::Create (DWORD dwInitial, DWORD dwMax)
{
    CONTRACTL
    {
      THROWS;
      GC_NOTRIGGER;
      PRECONDITION(m_handle == INVALID_HANDLE_VALUE);
    }
    CONTRACTL_END;

    {
        HANDLE h = WszCreateSemaphore(NULL,dwInitial,dwMax,NULL);
        if (h == NULL) {
            ThrowOutOfMemory();
        }
        m_handle = h;
    }
}


void CLRSemaphore::Close()
{
    LIMITED_METHOD_CONTRACT;

    if (m_handle != INVALID_HANDLE_VALUE) {
        CloseHandle(m_handle);
        m_handle = INVALID_HANDLE_VALUE;
    }
}

BOOL CLRSemaphore::Release(LONG lReleaseCount, LONG *lpPreviousCount)
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      PRECONDITION(m_handle != INVALID_HANDLE_VALUE);
    }
    CONTRACTL_END;

    {
        return ::ReleaseSemaphore(m_handle, lReleaseCount, lpPreviousCount);
    }
}


DWORD CLRSemaphore::Wait(DWORD dwMilliseconds, BOOL alertable)
{
    CONTRACTL
    {
        if (GetThreadNULLOk() && alertable)
        {
            THROWS;               // Thread::DoAppropriateWait can throw
        }
        else
        {
            NOTHROW;
        }

        if (GetThreadNULLOk())
        {
            if (alertable)
                GC_TRIGGERS;
            else
                GC_NOTRIGGER;
        }
        else
        {
            DISABLED(GC_TRIGGERS);
        }

        PRECONDITION(m_handle != INVALID_HANDLE_VALUE); // Invalid to have invalid handle
    }
    CONTRACTL_END;


    Thread *pThread = GetThreadNULLOk();
    _ASSERTE (pThread || !g_fEEStarted || dbgOnly_IsSpecialEEThread());

    {
        // TODO wwl: if alertable is FALSE, do we support a host to break a deadlock?
        // Currently we can not call through DoAppropriateWait because of CannotThrowComplusException.
        // We should re-consider this after our code is exception safe.
        if (pThread && alertable) {
            return pThread->DoAppropriateWait(1, &m_handle, FALSE, dwMilliseconds,
                                              alertable?WaitMode_Alertable:WaitMode_None,
                                              NULL);
        }
        else {
            DWORD result = WAIT_FAILED;
            EX_TRY
            {
                result = WaitForSingleObjectEx(m_handle,dwMilliseconds,alertable);
            }
            EX_CATCH
            {
                result = WAIT_FAILED;
            }
            EX_END_CATCH(SwallowAllExceptions);
            return result;
        }
    }
}

void CLRLifoSemaphore::Create(INT32 initialSignalCount, INT32 maximumSignalCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(maximumSignalCount > 0);
    _ASSERTE(initialSignalCount <= maximumSignalCount);
    _ASSERTE(m_handle == nullptr);

#ifdef TARGET_UNIX
    HANDLE h = WszCreateSemaphore(nullptr, 0, maximumSignalCount, nullptr);
#else // !TARGET_UNIX
    HANDLE h = CreateIoCompletionPort(INVALID_HANDLE_VALUE, nullptr, 0, maximumSignalCount);
#endif // TARGET_UNIX
    if (h == nullptr)
    {
        ThrowOutOfMemory();
    }

    m_handle = h;
    m_counts.signalCount = initialSignalCount;
    INDEBUG(m_maximumSignalCount = maximumSignalCount);
}

void CLRLifoSemaphore::Close()
{
    LIMITED_METHOD_CONTRACT;

    if (m_handle == nullptr)
    {
        return;
    }

    CloseHandle(m_handle);
    m_handle = nullptr;
}

bool CLRLifoSemaphore::WaitForSignal(DWORD timeoutMs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(timeoutMs != 0);
    _ASSERTE(m_handle != nullptr);
    _ASSERTE(m_counts.VolatileLoadWithoutBarrier().waiterCount != (UINT16)0);

    while (true)
    {
        // Wait for a signal
        BOOL waitSuccessful;
        {
#ifdef TARGET_UNIX
            // Do a prioritized wait to get LIFO waiter release order
            DWORD waitResult = PAL_WaitForSingleObjectPrioritized(m_handle, timeoutMs);
            _ASSERTE(waitResult == WAIT_OBJECT_0 || waitResult == WAIT_TIMEOUT);
            waitSuccessful = waitResult == WAIT_OBJECT_0;
#else // !TARGET_UNIX
            // I/O completion ports release waiters in LIFO order, see
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa365198(v=vs.85).aspx
            DWORD numberOfBytes;
            ULONG_PTR completionKey;
            LPOVERLAPPED overlapped;
            waitSuccessful = GetQueuedCompletionStatus(m_handle, &numberOfBytes, &completionKey, &overlapped, timeoutMs);
            _ASSERTE(waitSuccessful || GetLastError() == WAIT_TIMEOUT);
            _ASSERTE(overlapped == nullptr);
#endif // TARGET_UNIX
        }

        if (!waitSuccessful)
        {
            // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
            // not observe a signal to the object being waited upon.
            Counts toSubtract;
            ++toSubtract.waiterCount;
            Counts countsBeforeUpdate = m_counts.ExchangeAdd(-toSubtract);
            _ASSERTE(countsBeforeUpdate.waiterCount != (UINT16)0);
            return false;
        }

        // Unregister the waiter if this thread will not be waiting anymore, and try to acquire the semaphore
        Counts counts = m_counts.VolatileLoadWithoutBarrier();
        while (true)
        {
            _ASSERTE(counts.waiterCount != (UINT16)0);
            Counts newCounts = counts;
            if (counts.signalCount != 0)
            {
                --newCounts.signalCount;
                --newCounts.waiterCount;
            }

            // This waiter has woken up and this needs to be reflected in the count of waiters signaled to wake
            if (counts.countOfWaitersSignaledToWake != (UINT8)0)
            {
                --newCounts.countOfWaitersSignaledToWake;
            }

            Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
            if (countsBeforeUpdate == counts)
            {
                if (counts.signalCount != 0)
                {
                    return true;
                }
                break;
            }

            counts = countsBeforeUpdate;
        }
    }
}

bool CLRLifoSemaphore::Wait(DWORD timeoutMs)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_handle != nullptr);

    // Acquire the semaphore or register as a waiter
    Counts counts = m_counts.VolatileLoadWithoutBarrier();
    while (true)
    {
        _ASSERTE(counts.signalCount <= m_maximumSignalCount);
        Counts newCounts = counts;
        if (counts.signalCount != 0)
        {
            --newCounts.signalCount;
        }
        else if (timeoutMs != 0)
        {
            ++newCounts.waiterCount;
            _ASSERTE(newCounts.waiterCount != (UINT16)0); // overflow check, this many waiters is currently not supported
        }

        Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
        if (countsBeforeUpdate == counts)
        {
            return counts.signalCount != 0 || (timeoutMs != 0 && WaitForSignal(timeoutMs));
        }

        counts = countsBeforeUpdate;
    }
}

bool CLRLifoSemaphore::Wait(DWORD timeoutMs, UINT32 spinCount, UINT32 processorCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_handle != nullptr);

    if (timeoutMs == 0 || spinCount == 0)
    {
        return Wait(timeoutMs);
    }

    // Try to acquire the semaphore or register as a spinner
    Counts counts = m_counts.VolatileLoadWithoutBarrier();
    while (true)
    {
        Counts newCounts = counts;
        if (counts.signalCount != 0)
        {
            --newCounts.signalCount;
        }
        else
        {
            ++newCounts.spinnerCount;
            if (newCounts.spinnerCount == (UINT8)0)
            {
                // Maximum number of spinners reached, register as a waiter instead
                --newCounts.spinnerCount;
                ++newCounts.waiterCount;
                _ASSERTE(newCounts.waiterCount != (UINT16)0); // overflow check, this many waiters is currently not supported
            }
        }

        Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
        if (countsBeforeUpdate == counts)
        {
            if (counts.signalCount != 0)
            {
                return true;
            }
            if (newCounts.waiterCount != counts.waiterCount)
            {
                return WaitForSignal(timeoutMs);
            }
            break;
        }

        counts = countsBeforeUpdate;
    }

#ifdef TARGET_ARM64
    // For now, the spinning changes are disabled on ARM64. The spin loop below replicates how UnfairSemaphore used to spin.
    // Once more tuning is done on ARM64, it should be possible to come up with a spinning scheme that works well everywhere.
    int spinCountPerProcessor = spinCount;
    for (UINT32 i = 1; ; ++i)
    {
        // Wait
        ClrSleepEx(0, false);

        // Try to acquire the semaphore and unregister as a spinner
        counts = m_counts.VolatileLoadWithoutBarrier();
        while (true)
        {
            _ASSERTE(counts.spinnerCount != (UINT8)0);
            if (counts.signalCount == 0)
            {
                break;
            }

            Counts newCounts = counts;
            --newCounts.signalCount;
            --newCounts.spinnerCount;

            Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
            if (countsBeforeUpdate == counts)
            {
                return true;
            }

            counts = countsBeforeUpdate;
        }

        // Determine whether to spin further
        double spinnersPerProcessor = (double)counts.spinnerCount / processorCount;
        UINT32 spinLimit = (UINT32)(spinCountPerProcessor / spinnersPerProcessor + 0.5);
        if (i >= spinLimit)
        {
            break;
        }
    }
#else // !TARGET_ARM64
    const UINT32 Sleep0Threshold = 10;
    YieldProcessorNormalizationInfo normalizationInfo;
#ifdef TARGET_UNIX
    // The PAL's wait subsystem is quite slow, spin more to compensate for the more expensive wait
    spinCount *= 2;
#endif // TARGET_UNIX
    for (UINT32 i = 0; i < spinCount; ++i)
    {
        // Wait
        //
        // (i - Sleep0Threshold) % 2 != 0: The purpose of this check is to interleave Thread.Yield/Sleep(0) with
        // Thread.SpinWait. Otherwise, the following issues occur:
        //   - When there are no threads to switch to, Yield and Sleep(0) become no-op and it turns the spin loop into a
        //     busy-spin that may quickly reach the max spin count and cause the thread to enter a wait state. Completing the
        //     spin loop too early can cause excessive context switcing from the wait.
        //   - If there are multiple threads doing Yield and Sleep(0) (typically from the same spin loop due to contention),
        //     they may switch between one another, delaying work that can make progress.
        if (i < Sleep0Threshold || (i - Sleep0Threshold) % 2 != 0)
        {
            YieldProcessorWithBackOffNormalized(normalizationInfo, i);
        }
        else
        {
            // Not doing SwitchToThread(), it does not seem to have any benefit over Sleep(0)
            ClrSleepEx(0, false);
        }

        // Try to acquire the semaphore and unregister as a spinner
        counts = m_counts.VolatileLoadWithoutBarrier();
        while (true)
        {
            _ASSERTE(counts.spinnerCount != (UINT8)0);
            if (counts.signalCount == 0)
            {
                break;
            }

            Counts newCounts = counts;
            --newCounts.signalCount;
            --newCounts.spinnerCount;

            Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
            if (countsBeforeUpdate == counts)
            {
                return true;
            }

            counts = countsBeforeUpdate;
        }
    }
#endif // TARGET_ARM64

    // Unregister as a spinner, and acquire the semaphore or register as a waiter
    counts = m_counts.VolatileLoadWithoutBarrier();
    while (true)
    {
        _ASSERTE(counts.spinnerCount != (UINT8)0);
        Counts newCounts = counts;
        --newCounts.spinnerCount;
        if (counts.signalCount != 0)
        {
            --newCounts.signalCount;
        }
        else
        {
            ++newCounts.waiterCount;
            _ASSERTE(newCounts.waiterCount != (UINT16)0); // overflow check, this many waiters is currently not supported
        }

        Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
        if (countsBeforeUpdate == counts)
        {
            return counts.signalCount != 0 || WaitForSignal(timeoutMs);
        }

        counts = countsBeforeUpdate;
    }
}

void CLRLifoSemaphore::Release(INT32 releaseCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(releaseCount > 0);
    _ASSERTE((UINT32)releaseCount <= m_maximumSignalCount);
    _ASSERTE(m_handle != INVALID_HANDLE_VALUE);

    INT32 countOfWaitersToWake;
    Counts counts = m_counts.VolatileLoadWithoutBarrier();
    while (true)
    {
        Counts newCounts = counts;

        // Increase the signal count. The addition doesn't overflow because of the limit on the max signal count in Create.
        newCounts.signalCount += releaseCount;
        _ASSERTE(newCounts.signalCount > counts.signalCount);

        // Determine how many waiters to wake, taking into account how many spinners and waiters there are and how many waiters
        // have previously been signaled to wake but have not yet woken
        countOfWaitersToWake =
            (INT32)min(newCounts.signalCount, (UINT32)newCounts.waiterCount + newCounts.spinnerCount) -
            newCounts.spinnerCount -
            newCounts.countOfWaitersSignaledToWake;
        if (countOfWaitersToWake > 0)
        {
            // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but since
            // WaitForSignal() does not have enough information to tell whether a woken thread was signaled, and due to the cap
            // below, it's possible for countOfWaitersSignaledToWake to be less than the number of threads that have actually
            // been signaled to wake.
            if (countOfWaitersToWake > releaseCount)
            {
                countOfWaitersToWake = releaseCount;
            }

            // Cap countOfWaitersSignaledToWake to its max value. It's ok to ignore some woken threads in this count, it just
            // means some more threads will be woken next time. Typically, it won't reach the max anyway.
            newCounts.countOfWaitersSignaledToWake += (UINT8)min(countOfWaitersToWake, (INT32)UINT8_MAX);
            if (newCounts.countOfWaitersSignaledToWake <= counts.countOfWaitersSignaledToWake)
            {
                newCounts.countOfWaitersSignaledToWake = UINT8_MAX;
            }
        }

        Counts countsBeforeUpdate = m_counts.CompareExchange(newCounts, counts);
        if (countsBeforeUpdate == counts)
        {
            _ASSERTE((UINT32)releaseCount <= m_maximumSignalCount - counts.signalCount);
            if (countOfWaitersToWake <= 0)
            {
                return;
            }
            break;
        }

        counts = countsBeforeUpdate;
    }

    // Wake waiters
#ifdef TARGET_UNIX
    BOOL released = ReleaseSemaphore(m_handle, countOfWaitersToWake, nullptr);
    _ASSERTE(released);
#else // !TARGET_UNIX
    while (--countOfWaitersToWake >= 0)
    {
        while (!PostQueuedCompletionStatus(m_handle, 0, 0, nullptr))
        {
            // Probably out of memory. It's not valid to stop and throw here, so try again after a delay.
            ClrSleepEx(1, false);
        }
    }
#endif // TARGET_UNIX
}

void CLRMutex::Create(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCTSTR lpName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(m_handle == INVALID_HANDLE_VALUE && m_handle != NULL);
    }
    CONTRACTL_END;

    m_handle = WszCreateMutex(lpMutexAttributes,bInitialOwner,lpName);
    if (m_handle == NULL)
    {
        ThrowOutOfMemory();
    }
}

void CLRMutex::Close()
{
    LIMITED_METHOD_CONTRACT;

    if (m_handle != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_handle);
        m_handle = INVALID_HANDLE_VALUE;
    }
}

BOOL CLRMutex::Release()
{
    CONTRACTL
    {
      NOTHROW;
      GC_NOTRIGGER;
      PRECONDITION(m_handle != INVALID_HANDLE_VALUE && m_handle != NULL);
    }
    CONTRACTL_END;

    BOOL fRet = ReleaseMutex(m_handle);
    if (fRet)
    {
        EE_LOCK_RELEASED(this);
    }
    return fRet;
}

DWORD CLRMutex::Wait(DWORD dwMilliseconds, BOOL bAlertable)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        PRECONDITION(m_handle != INVALID_HANDLE_VALUE && m_handle != NULL);
    }
    CONTRACTL_END;

    DWORD fRet = WaitForSingleObjectEx(m_handle, dwMilliseconds, bAlertable);

    if (fRet == WAIT_OBJECT_0)
    {
        EE_LOCK_TAKEN(this);
    }

    return fRet;
}

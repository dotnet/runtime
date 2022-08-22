// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _SYNCBLK_INL_
#define _SYNCBLK_INL_

#ifndef DACCESS_COMPILE

FORCEINLINE bool AwareLock::LockState::InterlockedTryLock()
{
    WRAPPER_NO_CONTRACT;
    return InterlockedTryLock(VolatileLoadWithoutBarrier());
}

FORCEINLINE bool AwareLock::LockState::InterlockedTryLock(LockState state)
{
    WRAPPER_NO_CONTRACT;

    // The monitor is fair to release waiters in FIFO order, but allows non-waiters to acquire the lock if it's available to
    // avoid lock convoys.
    //
    // Lock convoys can be detrimental to performance in scenarios where work is being done on multiple threads and the work
    // involves periodically taking a particular lock for a short time to access shared resources. With a lock convoy, once
    // there is a waiter for the lock (which is not uncommon in such scenarios), a worker thread would be forced to
    // context-switch on the subsequent attempt to acquire the lock, often long before the worker thread exhausts its time
    // slice. This process repeats as long as the lock has a waiter, forcing every worker to context-switch on each attempt to
    // acquire the lock, killing performance and creating a negative feedback loop that makes it more likely for the lock to
    // have waiters. To avoid the lock convoy, each worker needs to be allowed to acquire the lock multiple times in sequence
    // despite there being a waiter for the lock in order to have the worker continue working efficiently during its time slice
    // as long as the lock is not contended.
    //
    // This scheme has the possibility to starve waiters. Waiter starvation is mitigated by other means, see
    // InterlockedTrySetShouldNotPreemptWaitersIfNecessary().
    if (state.ShouldNonWaiterAttemptToAcquireLock())
    {
        LockState newState = state;
        newState.InvertIsLocked();

        return CompareExchangeAcquire(newState, state) == state;
    }
    return false;
}

FORCEINLINE bool AwareLock::LockState::InterlockedUnlock()
{
    WRAPPER_NO_CONTRACT;
    static_assert_no_msg(IsLockedMask == 1);
    _ASSERTE(IsLocked());

    LockState state = InterlockedDecrementRelease((LONG *)&m_state);
    while (true)
    {
        // Keep track of whether a thread has been signaled to wake but has not yet woken from the wait.
        // IsWaiterSignaledToWakeMask is cleared when a signaled thread wakes up by observing a signal. Since threads can
        // preempt waiting threads and acquire the lock (see InterlockedTryLock()), it allows for example, one thread to acquire
        // and release the lock multiple times while there are multiple waiting threads. In such a case, we don't want that
        // thread to signal a waiter every time it releases the lock, as that will cause unnecessary context switches with more
        // and more signaled threads waking up, finding that the lock is still locked, and going right back into a wait state.
        // So, signal only one waiting thread at a time.
        if (!state.NeedToSignalWaiter())
        {
            return false;
        }

        LockState newState = state;
        newState.InvertIsWaiterSignaledToWake();

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            return true;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE bool AwareLock::LockState::InterlockedTrySetShouldNotPreemptWaitersIfNecessary(AwareLock *awareLock)
{
    WRAPPER_NO_CONTRACT;
    return InterlockedTrySetShouldNotPreemptWaitersIfNecessary(awareLock, VolatileLoadWithoutBarrier());
}

FORCEINLINE bool AwareLock::LockState::InterlockedTrySetShouldNotPreemptWaitersIfNecessary(
    AwareLock *awareLock,
    LockState state)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(awareLock != nullptr);
    _ASSERTE(&awareLock->m_lockState == this);

    // Normally, threads are allowed to preempt waiters to acquire the lock in order to avoid creating lock convoys, see
    // InterlockedTryLock(). There are cases where waiters can be easily starved as a result. For example, a thread that
    // holds a lock for a significant amount of time (much longer than the time it takes to do a context switch), then
    // releases and reacquires the lock in quick succession, and repeats. Though a waiter would be woken upon lock release,
    // usually it will not have enough time to context-switch-in and take the lock, and can be starved for an unreasonably long
    // duration.
    //
    // In order to prevent such starvation and force a bit of fair forward progress, it is sometimes necessary to change the
    // normal policy and disallow threads from preempting waiters. ShouldNotPreemptWaiters() indicates the current state of the
    // policy and this function determines whether the policy should be changed to disallow non-waiters from preempting waiters.
    //   - When the first waiter begins waiting, it records the current time as a "waiter starvation start time". That is a
    //     point in time after which no forward progress has occurred for waiters. When a waiter acquires the lock, the time is
    //     updated to the current time.
    //   - This function checks whether the starvation duration has crossed a threshold and if so, sets
    //     ShouldNotPreemptWaiters()
    //
    // When unreasonable starvation is occurring, the lock will be released occasionally and if caused by spinners, spinners
    // will be starting to spin.
    //   - Before starting to spin this function is called. If ShouldNotPreemptWaiters() is set, the spinner will skip spinning
    //     and wait instead. Spinners that are already registered at the time ShouldNotPreemptWaiters() is set will stop
    //     spinning as necessary. Eventually, all spinners will drain and no new ones will be registered.
    //   - Upon releasing a lock, if there are no spinners, a waiter will be signaled to wake. On that path, this function
    //     is called.
    //   - Eventually, after spinners have drained, only a waiter will be able to acquire the lock. When a waiter acquires
    //     the lock, or when the last waiter unregisters itself, ShouldNotPreemptWaiters() is cleared to restore the normal
    //     policy.

    while (true)
    {
        if (!state.HasAnyWaiters())
        {
            _ASSERTE(!state.ShouldNotPreemptWaiters());
            return false;
        }
        if (state.ShouldNotPreemptWaiters())
        {
            return true;
        }
        if (!awareLock->ShouldStopPreemptingWaiters())
        {
            return false;
        }

        LockState newState = state;
        newState.InvertShouldNotPreemptWaiters();

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            return true;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE AwareLock::EnterHelperResult AwareLock::LockState::InterlockedTry_LockOrRegisterSpinner(LockState state)
{
    WRAPPER_NO_CONTRACT;

    while (true)
    {
        LockState newState = state;
        if (state.ShouldNonWaiterAttemptToAcquireLock())
        {
            newState.InvertIsLocked();
        }
        else if (state.ShouldNotPreemptWaiters() || !newState.TryIncrementSpinnerCount())
        {
            return EnterHelperResult_UseSlowPath;
        }

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            return state.ShouldNonWaiterAttemptToAcquireLock() ? EnterHelperResult_Entered : EnterHelperResult_Contention;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE AwareLock::EnterHelperResult AwareLock::LockState::InterlockedTry_LockAndUnregisterSpinner()
{
    WRAPPER_NO_CONTRACT;

    // This function is called from inside a spin loop, it must unregister the spinner if and only if the lock is acquired
    LockState state = VolatileLoadWithoutBarrier();
    while (true)
    {
        _ASSERTE(state.HasAnySpinners());
        if (!state.ShouldNonWaiterAttemptToAcquireLock())
        {
            return state.ShouldNotPreemptWaiters() ? EnterHelperResult_UseSlowPath : EnterHelperResult_Contention;
        }

        LockState newState = state;
        newState.InvertIsLocked();
        newState.DecrementSpinnerCount();

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            return EnterHelperResult_Entered;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE bool AwareLock::LockState::InterlockedUnregisterSpinner_TryLock()
{
    WRAPPER_NO_CONTRACT;

    // This function is called at the end of a spin loop, it must unregister the spinner always and acquire the lock if it's
    // available. If the lock is available, a spinner must acquire the lock along with unregistering itself, because a lock
    // releaser does not wake a waiter when there is a spinner registered.

    LockState stateBeforeUpdate = InterlockedExchangeAdd((LONG *)&m_state, -(LONG)SpinnerCountIncrement);
    _ASSERTE(stateBeforeUpdate.HasAnySpinners());
    if (stateBeforeUpdate.IsLocked())
    {
        return false;
    }

    LockState state = stateBeforeUpdate;
    state.DecrementSpinnerCount();
    _ASSERTE(!state.IsLocked());
    do
    {
        LockState newState = state;
        newState.InvertIsLocked();

        LockState stateBeforeUpdate = CompareExchangeAcquire(newState, state);
        if (stateBeforeUpdate == state)
        {
            return true;
        }

        state = stateBeforeUpdate;
    } while (!state.IsLocked());
    return false;
}

FORCEINLINE bool AwareLock::LockState::InterlockedTryLock_Or_RegisterWaiter(AwareLock *awareLock, LockState state)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(awareLock != nullptr);
    _ASSERTE(&awareLock->m_lockState == this);

    bool waiterStarvationStartTimeWasReset = false;
    while (true)
    {
        LockState newState = state;
        if (state.ShouldNonWaiterAttemptToAcquireLock())
        {
            newState.InvertIsLocked();
        }
        else
        {
            newState.IncrementWaiterCount();

            if (!state.HasAnyWaiters() && !waiterStarvationStartTimeWasReset)
            {
                // This would be the first waiter. Once the waiter is registered, another thread may check the waiter starvation
                // start time and the previously recorded value may be stale, causing ShouldNotPreemptWaiters() to be set
                // unnecessarily. Reset the start time before registering the waiter.
                waiterStarvationStartTimeWasReset = true;
                awareLock->ResetWaiterStarvationStartTime();
            }
        }

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            if (state.ShouldNonWaiterAttemptToAcquireLock())
            {
                return true;
            }

            if (!state.HasAnyWaiters())
            {
                // This was the first waiter, record the waiter starvation start time
                _ASSERTE(waiterStarvationStartTimeWasReset);
                awareLock->RecordWaiterStarvationStartTime();
            }
            return false;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE void AwareLock::LockState::InterlockedUnregisterWaiter()
{
    WRAPPER_NO_CONTRACT;

    LockState state = VolatileLoadWithoutBarrier();
    while (true)
    {
        _ASSERTE(state.HasAnyWaiters());

        LockState newState = state;
        newState.DecrementWaiterCount();
        if (newState.ShouldNotPreemptWaiters() && !newState.HasAnyWaiters())
        {
            newState.InvertShouldNotPreemptWaiters();
        }

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            return;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE bool AwareLock::LockState::InterlockedTry_LockAndUnregisterWaiterAndObserveWakeSignal(AwareLock *awareLock)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(awareLock != nullptr);
    _ASSERTE(&awareLock->m_lockState == this);

    // This function is called from the waiter's spin loop and should observe the wake signal only if the lock is taken, to
    // prevent a lock releaser from waking another waiter while one is already spinning to acquire the lock
    bool waiterStarvationStartTimeWasRecorded = false;
    LockState state = VolatileLoadWithoutBarrier();
    while (true)
    {
        _ASSERTE(state.HasAnyWaiters());
        _ASSERTE(state.IsWaiterSignaledToWake());
        if (state.IsLocked())
        {
            return false;
        }

        LockState newState = state;
        newState.InvertIsLocked();
        newState.InvertIsWaiterSignaledToWake();
        newState.DecrementWaiterCount();
        if (newState.ShouldNotPreemptWaiters())
        {
            newState.InvertShouldNotPreemptWaiters();

            if (newState.HasAnyWaiters() && !waiterStarvationStartTimeWasRecorded)
            {
                // Update the waiter starvation start time. The time must be recorded before ShouldNotPreemptWaiters() is
                // cleared, as once that is cleared, another thread may check the waiter starvation start time and the
                // previously recorded value may be stale, causing ShouldNotPreemptWaiters() to be set again unnecessarily.
                waiterStarvationStartTimeWasRecorded = true;
                awareLock->RecordWaiterStarvationStartTime();
            }
        }

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            if (newState.HasAnyWaiters())
            {
                _ASSERTE(!state.ShouldNotPreemptWaiters() || waiterStarvationStartTimeWasRecorded);
                if (!waiterStarvationStartTimeWasRecorded)
                {
                    // Since the lock was acquired successfully by a waiter, update the waiter starvation start time
                    awareLock->RecordWaiterStarvationStartTime();
                }
            }
            return true;
        }

        state = stateBeforeUpdate;
    }
}

FORCEINLINE bool AwareLock::LockState::InterlockedObserveWakeSignal_Try_LockAndUnregisterWaiter(AwareLock *awareLock)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(awareLock != nullptr);
    _ASSERTE(&awareLock->m_lockState == this);

    // This function is called at the end of the waiter's spin loop. It must observe the wake signal always, and if the lock is
    // available, it must acquire the lock and unregister the waiter. If the lock is available, a waiter must acquire the lock
    // along with observing the wake signal, because a lock releaser does not wake a waiter when a waiter was signaled but the
    // wake signal has not been observed.

    LockState stateBeforeUpdate = InterlockedExchangeAdd((LONG *)&m_state, -(LONG)IsWaiterSignaledToWakeMask);
    _ASSERTE(stateBeforeUpdate.IsWaiterSignaledToWake());
    if (stateBeforeUpdate.IsLocked())
    {
        return false;
    }

    bool waiterStarvationStartTimeWasRecorded = false;
    LockState state = stateBeforeUpdate;
    state.InvertIsWaiterSignaledToWake();
    _ASSERTE(!state.IsLocked());
    do
    {
        _ASSERTE(state.HasAnyWaiters());
        LockState newState = state;
        newState.InvertIsLocked();
        newState.DecrementWaiterCount();
        if (newState.ShouldNotPreemptWaiters())
        {
            newState.InvertShouldNotPreemptWaiters();

            if (newState.HasAnyWaiters() && !waiterStarvationStartTimeWasRecorded)
            {
                // Update the waiter starvation start time. The time must be recorded before ShouldNotPreemptWaiters() is
                // cleared, as once that is cleared, another thread may check the waiter starvation start time and the
                // previously recorded value may be stale, causing ShouldNotPreemptWaiters() to be set again unnecessarily.
                waiterStarvationStartTimeWasRecorded = true;
                awareLock->RecordWaiterStarvationStartTime();
            }
        }

        LockState stateBeforeUpdate = CompareExchange(newState, state);
        if (stateBeforeUpdate == state)
        {
            if (newState.HasAnyWaiters())
            {
                _ASSERTE(!state.ShouldNotPreemptWaiters() || waiterStarvationStartTimeWasRecorded);
                if (!waiterStarvationStartTimeWasRecorded)
                {
                    // Since the lock was acquired successfully by a waiter, update the waiter starvation start time
                    awareLock->RecordWaiterStarvationStartTime();
                }
            }
            return true;
        }

        state = stateBeforeUpdate;
    } while (!state.IsLocked());
    return false;
}

FORCEINLINE void AwareLock::ResetWaiterStarvationStartTime()
{
    LIMITED_METHOD_CONTRACT;
    m_waiterStarvationStartTimeMs = 0;
}

FORCEINLINE void AwareLock::RecordWaiterStarvationStartTime()
{
    WRAPPER_NO_CONTRACT;

    DWORD currentTimeMs = GetTickCount();
    if (currentTimeMs == 0)
    {
        // Don't record zero, that value is reserved for identifying that a time is not recorded
        --currentTimeMs;
    }
    m_waiterStarvationStartTimeMs = currentTimeMs;
}

FORCEINLINE bool AwareLock::ShouldStopPreemptingWaiters() const
{
    WRAPPER_NO_CONTRACT;

    // If the recorded time is zero, a time has not been recorded yet
    DWORD waiterStarvationStartTimeMs = m_waiterStarvationStartTimeMs;
    return
        waiterStarvationStartTimeMs != 0 &&
        GetTickCount() - waiterStarvationStartTimeMs >= WaiterStarvationDurationMsBeforeStoppingPreemptingWaiters;
}

FORCEINLINE void AwareLock::SpinWait(const YieldProcessorNormalizationInfo &normalizationInfo, DWORD spinIteration)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 1);
    _ASSERTE(spinIteration < g_SpinConstants.dwMonitorSpinCount);

    YieldProcessorWithBackOffNormalized(normalizationInfo, spinIteration);
}

FORCEINLINE bool AwareLock::TryEnterHelper(Thread* pCurThread)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_lockState.InterlockedTryLock())
    {
        m_HoldingThread = pCurThread;
        m_Recursion = 1;
        return true;
    }

    if (GetOwningThread() == pCurThread) /* monitor is held, but it could be a recursive case */
    {
        m_Recursion++;
        return true;
    }
    return false;
}

FORCEINLINE AwareLock::EnterHelperResult AwareLock::TryEnterBeforeSpinLoopHelper(Thread *pCurThread)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    LockState state = m_lockState.VolatileLoadWithoutBarrier();

    // Check the recursive case once before the spin loop. If it's not the recursive case in the beginning, it will not
    // be in the future, so the spin loop can avoid checking the recursive case.
    if (!state.IsLocked() || GetOwningThread() != pCurThread)
    {
        if (m_lockState.InterlockedTrySetShouldNotPreemptWaitersIfNecessary(this, state))
        {
            // This thread currently should not preempt waiters, just wait
            return EnterHelperResult_UseSlowPath;
        }

        // Not a recursive enter, try to acquire the lock or register the spinner
        EnterHelperResult result = m_lockState.InterlockedTry_LockOrRegisterSpinner(state);
        if (result != EnterHelperResult_Entered)
        {
            // EnterHelperResult_Contention:
            //   Lock was not acquired and the spinner was registered
            // EnterHelperResult_UseSlowPath:
            //   This thread currently should not preempt waiters, or we reached the maximum number of spinners, just wait
            return result;
        }

        // Lock was acquired and the spinner was not registered
        m_HoldingThread = pCurThread;
        m_Recursion = 1;
        return EnterHelperResult_Entered;
    }

    // Recursive enter
    m_Recursion++;
    return EnterHelperResult_Entered;
}

FORCEINLINE AwareLock::EnterHelperResult AwareLock::TryEnterInsideSpinLoopHelper(Thread *pCurThread)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    // Try to acquire the lock and unregister the spinner. The recursive case is not checked here because
    // TryEnterBeforeSpinLoopHelper() would have taken care of that case before the spin loop.
    EnterHelperResult result = m_lockState.InterlockedTry_LockAndUnregisterSpinner();
    if (result != EnterHelperResult_Entered)
    {
        // EnterHelperResult_Contention:
        //   Lock was not acquired and the spinner was not unregistered
        // EnterHelperResult_UseSlowPath:
        //   This thread currently should not preempt waiters, stop spinning and just wait
        return result;
    }

    // Lock was acquired and spinner was unregistered
    m_HoldingThread = pCurThread;
    m_Recursion = 1;
    return EnterHelperResult_Entered;
}

FORCEINLINE bool AwareLock::TryEnterAfterSpinLoopHelper(Thread *pCurThread)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    // Unregister the spinner and try to acquire the lock. A spinner must not unregister itself without trying to acquire the
    // lock because a lock releaser does not wake a waiter when a spinner can acquire the lock.
    if (!m_lockState.InterlockedUnregisterSpinner_TryLock())
    {
        // Spinner was unregistered and the lock was not acquired
        return false;
    }

    // Spinner was unregistered and the lock was acquired
    m_HoldingThread = pCurThread;
    m_Recursion = 1;
    return true;
}

FORCEINLINE AwareLock::EnterHelperResult ObjHeader::EnterObjMonitorHelper(Thread* pCurThread)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    LONG oldValue = m_SyncBlockValue.LoadWithoutBarrier();

    if ((oldValue & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX +
                     BIT_SBLK_SPIN_LOCK +
                     SBLK_MASK_LOCK_THREADID +
                     SBLK_MASK_LOCK_RECLEVEL)) == 0)
    {
        DWORD tid = pCurThread->GetThreadId();
        if (tid > SBLK_MASK_LOCK_THREADID)
        {
            return AwareLock::EnterHelperResult_UseSlowPath;
        }

        LONG newValue = oldValue | tid;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
        if (FastInterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#else   
        if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#endif
        {
            return AwareLock::EnterHelperResult_Entered;
        }

        return AwareLock::EnterHelperResult_Contention;
    }

    if (oldValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
    {
        // If we have a hash code already, we need to create a sync block
        if (oldValue & BIT_SBLK_IS_HASHCODE)
        {
            return AwareLock::EnterHelperResult_UseSlowPath;
        }

        SyncBlock *syncBlock = g_pSyncTable[oldValue & MASK_SYNCBLOCKINDEX].m_SyncBlock;
        _ASSERTE(syncBlock != NULL);
        if (syncBlock->m_Monitor.TryEnterHelper(pCurThread))
        {
            return AwareLock::EnterHelperResult_Entered;
        }

        return AwareLock::EnterHelperResult_Contention;
    }

    // The header is transitioning - treat this as if the lock was taken
    if (oldValue & BIT_SBLK_SPIN_LOCK)
    {
        return AwareLock::EnterHelperResult_Contention;
    }

    // Here we know we have the "thin lock" layout, but the lock is not free.
    // It could still be the recursion case - compare the thread id to check
    if (pCurThread->GetThreadId() != (DWORD)(oldValue & SBLK_MASK_LOCK_THREADID))
    {
        return AwareLock::EnterHelperResult_Contention;
    }

    // Ok, the thread id matches, it's the recursion case.
    // Bump up the recursion level and check for overflow
    LONG newValue = oldValue + SBLK_LOCK_RECLEVEL_INC;

    if ((newValue & SBLK_MASK_LOCK_RECLEVEL) == 0)
    {
        return AwareLock::EnterHelperResult_UseSlowPath;
    }

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
    if (FastInterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#else
    if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#endif
    {
        return AwareLock::EnterHelperResult_Entered;
    }

    // Use the slow path instead of spinning. The compare-exchange above would not fail often, and it's not worth forcing the
    // spin loop that typically follows the call to this function to check the recursive case, so just bail to the slow path.
    return AwareLock::EnterHelperResult_UseSlowPath;
}

// Helper encapsulating the core logic for releasing monitor. Returns what kind of
// follow up action is necessary. This is FORCEINLINE to make it provide a very efficient implementation.
FORCEINLINE AwareLock::LeaveHelperAction AwareLock::LeaveHelper(Thread* pCurThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_HoldingThread != pCurThread)
        return AwareLock::LeaveHelperAction_Error;

    _ASSERTE(m_lockState.VolatileLoadWithoutBarrier().IsLocked());
    _ASSERTE(m_Recursion >= 1);

#if defined(_DEBUG) && defined(TRACK_SYNC)
    // The best place to grab this is from the ECall frame
    Frame   *pFrame = pCurThread->GetFrame();
    int      caller = (pFrame && pFrame != FRAME_TOP ? (int) pFrame->GetReturnAddress() : -1);
    pCurThread->m_pTrackSync->LeaveSync(caller, this);
#endif

    if (--m_Recursion == 0)
    {
        m_HoldingThread = NULL;

        // Clear lock bit and determine whether we must signal a waiter to wake
        if (!m_lockState.InterlockedUnlock())
        {
            return AwareLock::LeaveHelperAction_None;
        }

        // There is a waiter and we must signal a waiter to wake
        return AwareLock::LeaveHelperAction_Signal;
    }
    return AwareLock::LeaveHelperAction_None;
}

// Helper encapsulating the core logic for releasing monitor. Returns what kind of
// follow up action is necessary. This is FORCEINLINE to make it provide a very efficient implementation.
FORCEINLINE AwareLock::LeaveHelperAction ObjHeader::LeaveObjMonitorHelper(Thread* pCurThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    DWORD syncBlockValue = m_SyncBlockValue.LoadWithoutBarrier();

    if ((syncBlockValue & (BIT_SBLK_SPIN_LOCK + BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)) == 0)
    {
        if ((syncBlockValue & SBLK_MASK_LOCK_THREADID) != pCurThread->GetThreadId())
        {
            // This thread does not own the lock.
            return AwareLock::LeaveHelperAction_Error;
        }

        if (!(syncBlockValue & SBLK_MASK_LOCK_RECLEVEL))
        {
            // We are leaving the lock
            DWORD newValue = (syncBlockValue & (~SBLK_MASK_LOCK_THREADID));

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            if (FastInterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) != (LONG)syncBlockValue)
#else
            if (InterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) != (LONG)syncBlockValue)
#endif
            {
                return AwareLock::LeaveHelperAction_Yield;
            }
        }
        else
        {
            // recursion and ThinLock
            DWORD newValue = syncBlockValue - SBLK_LOCK_RECLEVEL_INC;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            if (FastInterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) != (LONG)syncBlockValue)
#else
            if (InterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) != (LONG)syncBlockValue)
#endif
            {
                return AwareLock::LeaveHelperAction_Yield;
            }
        }

        return AwareLock::LeaveHelperAction_None;
    }

    if ((syncBlockValue & (BIT_SBLK_SPIN_LOCK + BIT_SBLK_IS_HASHCODE)) == 0)
    {
        _ASSERTE((syncBlockValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0);
        SyncBlock *syncBlock = g_pSyncTable[syncBlockValue & MASK_SYNCBLOCKINDEX].m_SyncBlock;
        _ASSERTE(syncBlock != NULL);
        return syncBlock->m_Monitor.LeaveHelper(pCurThread);
    }

    if (syncBlockValue & BIT_SBLK_SPIN_LOCK)
    {
        return AwareLock::LeaveHelperAction_Contention;
    }

    // This thread does not own the lock.
    return AwareLock::LeaveHelperAction_Error;
}

#endif // DACCESS_COMPILE

// Provide access to the object associated with this awarelock, so client can
// protect it.
inline OBJECTREF AwareLock::GetOwningObject()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // gcc on mac needs these intermediate casts to avoid some ambiuous overloading in the DAC case
    PTR_SyncTableEntry table = SyncTableEntry::GetSyncTableEntry();
    return (OBJECTREF)(Object*)(PTR_Object)table[(m_dwSyncIndex & ~SyncBlock::SyncBlockPrecious)].m_Object;
}

#endif  // _SYNCBLK_INL_

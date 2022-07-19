// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// spinlock.cpp
//

//


#include "common.h"

#include "slist.h"
#include "spinlock.h"
#include "threads.h"
#include "corhost.h"

enum
{
	BACKOFF_LIMIT = 1000		// used in spin to acquire
};

#ifdef _DEBUG

	// profile information
ULONG	SpinLockProfiler::s_ulBackOffs = 0;
ULONG	SpinLockProfiler::s_ulCollisons [LOCK_TYPE_DEFAULT + 1] = { 0 };
ULONG	SpinLockProfiler::s_ulSpins [LOCK_TYPE_DEFAULT + 1] = { 0 };

#endif

SpinLock::SpinLock()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    m_Initialized = UnInitialized;
}

void SpinLock::Init(LOCK_TYPE type, bool RequireCoopGC)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_Initialized == Initialized)
    {
        _ASSERTE (type == m_LockType);
        _ASSERTE (RequireCoopGC == m_requireCoopGCMode);

        // We have initialized this spinlock.
        return;
    }

    while (TRUE)
    {
        LONG curValue = InterlockedCompareExchange((LONG*)&m_Initialized, BeingInitialized, UnInitialized);
        if (curValue == Initialized)
        {
            return;
        }
        else if (curValue == UnInitialized)
        {
            // We are the first to initialize the lock
            break;
        }
        else
        {
            __SwitchToThread(10, CALLER_LIMITS_SPINNING);
        }
    }

    {
        m_lock = 0;
    }

#ifdef _DEBUG
    m_LockType = type;
    m_requireCoopGCMode = RequireCoopGC;
#endif

    _ASSERTE (m_Initialized == BeingInitialized);
    m_Initialized = Initialized;
}

#ifdef _DEBUG
BOOL SpinLock::OwnedByCurrentThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    return m_holdingThreadId.IsCurrentThread();
}
#endif

DEBUG_NOINLINE void SpinLock::AcquireLock(SpinLock *s)
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_GC_NOTRIGGER;

    s->GetLock();
}

DEBUG_NOINLINE void SpinLock::ReleaseLock(SpinLock *s)
{
    SCAN_SCOPE_END;

    s->FreeLock();
}


void SpinLock::GetLock()
{
    CONTRACTL
    {
        DISABLED(THROWS);  // need to rewrite spin locks to no-throw.
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(m_Initialized == Initialized);

#ifdef _DEBUG
    dbg_PreEnterLock();
#endif

    {
        // Not CLR Sync hosted, so we use interlocked operations on
        // m_lock to acquire the lock.  This will automatically cause
        // us to call EE_LOCK_TAKEN(this);
        if (!GetLockNoWait())
        {
            SpinToAcquire();
        }
    }

#ifdef _DEBUG
    m_holdingThreadId.SetToCurrentThread();
    dbg_EnterLock();
#endif
}

//----------------------------------------------------------------------------
// SpinLock::GetLockNoWait
// used interlocked exchange and fast lock acquire

BOOL SpinLock::GetLockNoWait()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    {
        if (VolatileLoad(&m_lock) == 0 && InterlockedExchange (&m_lock, 1) == 0)
        {
            EE_LOCK_TAKEN(this);
            return 1;
        }
        return 0;
    }
}

//----------------------------------------------------------------------------
// SpinLock::FreeLock
//  Release the spinlock
//
void SpinLock::FreeLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_Initialized == Initialized);

#ifdef _DEBUG
    _ASSERTE(OwnedByCurrentThread());
    m_holdingThreadId.Clear();
    dbg_LeaveLock();
#endif

    {
        VolatileStore(&m_lock, (LONG)0);
    }

    EE_LOCK_RELEASED(this);

} // SpinLock::FreeLock ()


//----------------------------------------------------------------------------
// SpinLock::SpinToAcquire   , non-inline function, called from inline Acquire
//
//  Spin waiting for a spinlock to become free.
//
//
void
SpinLock::SpinToAcquire()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD backoffs = 0;
    ULONG ulSpins = 0;
    YieldProcessorNormalizationInfo normalizationInfo;

    while (true)
    {
        for (ULONG i = ulSpins + 10000;
             ulSpins < i;
             ulSpins++)
        {
            YieldProcessorNormalized(normalizationInfo); // indicate to the processor that we are spinning

            // Note: Must use Volatile to ensure the lock is
            // refetched from memory.
            //
            if (VolatileLoad(&m_lock) == 0)
            {
                break;
            }
        }

        // Try the inline atomic test again.
        //
        if (GetLockNoWait())
        {
            // EE_LOCK_TAKEN(this) has already been called by GetLockNoWait
            break;
        }

        //backoff
        __SwitchToThread(0, backoffs++);
    }

#ifdef _DEBUG
    //profile info
    SpinLockProfiler::IncrementCollisions (m_LockType);
    SpinLockProfiler::IncrementSpins (m_LockType, ulSpins);
    SpinLockProfiler::IncrementBackoffs (backoffs);
#endif

} // SpinLock::SpinToAcquire ()

#ifdef _DEBUG
// If a GC is not allowed when we enter the lock, we'd better not do anything inside
// the lock that could provoke a GC.  Otherwise other threads attempting to block
// (which are presumably in the same GC mode as this one) will block.  This will cause
// a deadlock if we do attempt a GC because we can't suspend blocking threads and we
// can't release the spin lock.
void SpinLock::dbg_PreEnterLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    Thread* pThread = GetThreadNULLOk();
    if (pThread)
    {
        // SpinLock can not be nested.
        _ASSERTE ((pThread->m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

        IncCantAllocCount();
        pThread->SetThreadStateNC(Thread::TSNC_OwnsSpinLock);

        if (!pThread->PreemptiveGCDisabled())
            _ASSERTE(!m_requireCoopGCMode);
    }
}

void SpinLock::dbg_EnterLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    Thread* pThread = GetThreadNULLOk();
    if (pThread)
    {
        INCONTRACT(pThread->BeginNoTriggerGC(__FILE__, __LINE__));
    }
}

void SpinLock::dbg_LeaveLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    Thread* pThread = GetThreadNULLOk();
    if (pThread)
    {
        _ASSERTE ((pThread->m_StateNC & Thread::TSNC_OwnsSpinLock) != 0);
        DecCantAllocCount();
        pThread->ResetThreadStateNC(Thread::TSNC_OwnsSpinLock);
        INCONTRACT(pThread->EndNoTriggerGC());
    }
}


void SpinLockProfiler::InitStatics ()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    s_ulBackOffs = 0;
    memset (s_ulCollisons, 0, sizeof (s_ulCollisons));
    memset (s_ulSpins, 0, sizeof (s_ulSpins));
}

void SpinLockProfiler::IncrementSpins (LOCK_TYPE type, ULONG value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    _ASSERTE(type <= LOCK_TYPE_DEFAULT);
    s_ulSpins [type] += value;
}

void SpinLockProfiler::IncrementCollisions (LOCK_TYPE type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    ++s_ulCollisons [type];
}

void SpinLockProfiler::IncrementBackoffs (ULONG value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    s_ulBackOffs += value;
}

void SpinLockProfiler::DumpStatics()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    //<TODO>todo </TODO>
}

#endif  // _DEBUG

// End of file: spinlock.cpp

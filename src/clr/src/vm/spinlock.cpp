// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    // Global SpinLock variables will cause the constructor to be
    // called during DllInit, which means we cannot use full contracts
    // because we have not called InitUtilCode yet.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    m_hostLock = NULL;
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    m_Initialized = UnInitialized;
}


SpinLock::~SpinLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (CLRSyncHosted() && m_hostLock) {
        m_hostLock->Release();
        m_hostLock = NULL;
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES
}

void SpinLock::Init(LOCK_TYPE type, bool RequireCoopGC)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Disallow creation of locks before EE starts.  But only complain if we end up
    // being hosted, since such locks have escaped the hosting net and will cause
    // AVs on next use.
#ifdef _DEBUG
    static bool fEarlyInit; // = false

    if (!(g_fEEStarted || g_fEEInit))
    {
        if (!CLRSyncHosted())
            fEarlyInit = true;
    }

    // If we are now hosted, we better not have *ever* created some locks that are
    // not known to our host.
    _ASSERTE(!fEarlyInit || !CLRSyncHosted());

#endif

    if (m_Initialized == Initialized)
    {
        _ASSERTE (type == m_LockType);
        _ASSERTE (RequireCoopGC == m_requireCoopGCMode);

        // We have initialized this spinlock.
        return;
    }

    while (TRUE)
    {
        LONG curValue = FastInterlockCompareExchange((LONG*)&m_Initialized, BeingInitialized, UnInitialized);
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    IHostSyncManager *pManager = CorHost2::GetHostSyncManager();
    _ASSERTE((pManager == NULL && m_lock == 0) ||
             (pManager && m_hostLock == NULL));

    if (pManager) 
    {
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = pManager->CreateCrst(&m_hostLock);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (hr != S_OK) {
            _ASSERTE (hr == E_OUTOFMEMORY);
            _ASSERTE (m_Initialized == BeingInitialized);
            m_Initialized = UnInitialized;
            ThrowOutOfMemory();
        }
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
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

DEBUG_NOINLINE void SpinLock::AcquireLock(SpinLock *s, Thread * pThread)
{
    SCAN_SCOPE_BEGIN;
    STATIC_CONTRACT_GC_NOTRIGGER;

    s->GetLock(pThread); 
}

DEBUG_NOINLINE void SpinLock::ReleaseLock(SpinLock *s, Thread * pThread) 
{ 
    SCAN_SCOPE_END;

    s->FreeLock(pThread); 
}


void SpinLock::GetLock(Thread* pThread) 
{
    CONTRACTL
    {
        DISABLED(THROWS);  // need to rewrite spin locks to no-throw.
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    _ASSERTE(m_Initialized == Initialized);

#ifdef _DEBUG
    dbg_PreEnterLock();
#endif

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (CLRSyncHosted())
    {
        DWORD option = WAIT_NOTINDEADLOCK;
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(pThread);
        hr = m_hostLock->Enter(option);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        _ASSERTE(hr == S_OK);
        EE_LOCK_TAKEN(this);
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        // Not CLR Sync hosted, so we use interlocked operations on
        // m_lock to acquire the lock.  This will automatically cause
        // us to call EE_LOCK_TAKEN(this);
        if (!GetLockNoWait()) 
        {
            SpinToAcquire();
        }
    }

    INCTHREADLOCKCOUNTTHREAD(pThread);
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
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (CLRSyncHosted())
    {
        BOOL result;
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = m_hostLock->TryEnter(WAIT_NOTINDEADLOCK, &result);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        _ASSERTE(hr == S_OK);

        if (result)
        {
            EE_LOCK_TAKEN(this);
        }

        return result;
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        if (VolatileLoad(&m_lock) == 0 && FastInterlockExchange (&m_lock, 1) == 0)
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
void SpinLock::FreeLock(Thread* pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

	_ASSERTE(m_Initialized == Initialized);

#ifdef _DEBUG
    _ASSERTE(OwnedByCurrentThread());
    m_holdingThreadId.Clear();
    dbg_LeaveLock();
#endif

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    if (CLRSyncHosted())
    {
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(pThread);
        hr = m_hostLock->Leave();
        END_SO_TOLERANT_CODE_CALLING_HOST;
        _ASSERTE (hr == S_OK);
    }
    else
#endif // FEATURE_INCLUDE_ALL_INTERFACES
    {
        VolatileStore(&m_lock, (LONG)0);
    }

    DECTHREADLOCKCOUNTTHREAD(pThread);
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
        SO_TOLERANT;
    }
    CONTRACTL_END;

    _ASSERTE (!CLRSyncHosted());

    DWORD backoffs = 0;
    ULONG ulSpins = 0;

    while (true)
    {
        for (unsigned i = ulSpins+10000;
             ulSpins < i;
             ulSpins++)
        {
            // Note: Must use Volatile to ensure the lock is
            // refetched from memory.
            //
            if (VolatileLoad(&m_lock) == 0)
            {
                break;
            }
            YieldProcessor();			// indicate to the processor that we are spining 
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

    Thread* pThread = GetThread();
    if (pThread)
    {
        // SpinLock can not be nested.
        _ASSERTE ((pThread->m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

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

    Thread* pThread = GetThread();
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

    Thread* pThread = GetThread();
    if (pThread)
    {
        _ASSERTE ((pThread->m_StateNC & Thread::TSNC_OwnsSpinLock) != 0);
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

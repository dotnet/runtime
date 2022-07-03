// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//----------------------------------------------------------------------------
//  spinlock.h , defines the spin lock class and a profiler class
//

//
//----------------------------------------------------------------------------


//#ifndef _H_UTIL
//#error I am a part of util.hpp Please don't include me alone !
//#endif



#ifndef _H_SPINLOCK_
#define _H_SPINLOCK_

#include <stddef.h>


// #SwitchToThreadSpinning
//
// If you call __SwitchToThread in a loop waiting for a condition to be met,
// it is critical that you insert periodic sleeps.  This is because the thread
// you are waiting for to set that condition may need your CPU, and simply
// calling __SwitchToThread(0) will NOT guarantee that it gets a chance to run.
// If there are other runnable threads of higher priority, or even if there
// aren't and it is in another processor's queue, you will be spinning a very
// long time.
//
// To force all callers to consider this issue and to avoid each having to
// duplicate the same backoff code, __SwitchToThread takes a required second
// parameter.  If you want it to handle backoff for you, this parameter should
// be the number of successive calls you have made to __SwitchToThread (a loop
// count).  If you want to take care of backing off yourself, you can pass
// CALLER_LIMITS_SPINNING.  There are three valid cases for doing this:
//
//     - You count iterations and induce a sleep periodically
//     - The number of consecutive __SwitchToThreads is limited
//     - Your call to __SwitchToThread includes a non-zero sleep duration
//
// Lastly, to simplify this requirement for the following common coding pattern:
//
//     while (!condition)
//         SwitchToThread
//
// you can use the YIELD_WHILE macro.

#define CALLER_LIMITS_SPINNING 0

#define YIELD_WHILE(condition)                                          \
    {                                                                   \
        DWORD __dwSwitchCount = 0;                                      \
        while (condition)                                               \
        {                                                               \
            __SwitchToThread(0, ++__dwSwitchCount);                     \
        }                                                               \
    }

// non-zero return value if this function causes the OS to switch to another thread
BOOL __SwitchToThread (DWORD dwSleepMSec, DWORD dwSwitchCount);


//----------------------------------------------------------------------------
// class: DangerousNonHostedSpinLock
//
// PURPOSE:
//   A simple wrapper around the spinloop without host interactions. To be
//   used for short-time locking in the VM, in particular when the runtime
//   has not been started yet.
//
//----------------------------------------------------------------------------
class DangerousNonHostedSpinLock
{
public:
    FORCEINLINE DangerousNonHostedSpinLock() { LIMITED_METHOD_CONTRACT; m_value = 0; }

private:
    // Intentionally unimplemented - prevents the compiler from generating default copy ctor.
    DangerousNonHostedSpinLock(DangerousNonHostedSpinLock const & other);

    FORCEINLINE void Acquire()
    {
        WRAPPER_NO_CONTRACT;
        YIELD_WHILE(InterlockedExchange(&m_value, 1) == 1);
    }

    FORCEINLINE BOOL TryAcquire()
    {
        WRAPPER_NO_CONTRACT;
        return (InterlockedExchange(&m_value, 1) == 0);
    }

    FORCEINLINE void Release()
    {
        LIMITED_METHOD_CONTRACT;
        m_value = 0;
    }

    inline static void AcquireLock(DangerousNonHostedSpinLock *pLock) { WRAPPER_NO_CONTRACT; pLock->Acquire(); }
    inline static BOOL TryAcquireLock(DangerousNonHostedSpinLock *pLock) { WRAPPER_NO_CONTRACT; return pLock->TryAcquire(); }
    inline static void ReleaseLock(DangerousNonHostedSpinLock *pLock) { WRAPPER_NO_CONTRACT; pLock->Release(); }

    Volatile<LONG> m_value;

public:
    BOOL IsHeld()
    {
        LIMITED_METHOD_CONTRACT;
        return (BOOL)m_value;
    }

    typedef Holder<DangerousNonHostedSpinLock *, DangerousNonHostedSpinLock::AcquireLock, DangerousNonHostedSpinLock::ReleaseLock> Holder;
    typedef ConditionalStateHolder<DangerousNonHostedSpinLock *, DangerousNonHostedSpinLock::TryAcquireLock, DangerousNonHostedSpinLock::ReleaseLock> TryHolder;
};

typedef DangerousNonHostedSpinLock::Holder DangerousNonHostedSpinLockHolder;
typedef DangerousNonHostedSpinLock::TryHolder DangerousNonHostedSpinLockTryHolder;


class SpinLock;

// Lock Types, used in profiling
//
enum LOCK_TYPE
{
    LOCK_PLUSWRAPPER_CACHE = 1,  // change
    LOCK_FCALL = 2,              // leave, but rank to tip
    LOCK_COMCTXENTRYCACHE = 3,   // creates events, allocs memory, SEH, etc.
#ifdef FEATURE_COMINTEROP
    LOCK_COMCALL = 4,
#endif
    LOCK_REFLECTCACHE = 5,
    LOCK_CORMAP = 7,
    LOCK_TYPE_DEFAULT  = 8
};

//----------------------------------------------------------------------------
// class: Spinlock
//
// PURPOSE:
//   spinlock class that contains constructor and out of line spinloop.
//
//----------------------------------------------------------------------------
class SpinLock
{

private:
    union {
        // m_lock has to be the fist data member in the class
        LONG                m_lock;     // LONG used in interlocked exchange
    };

    enum SpinLockState
    {
        UnInitialized,
        BeingInitialized,
        Initialized
    };

    Volatile<SpinLockState>      m_Initialized; // To verify initialized
                                        // And initialize once

#ifdef _DEBUG
    LOCK_TYPE           m_LockType;     // lock type to track statistics

    // Check for dead lock situation.
    bool                m_requireCoopGCMode;
    EEThreadId          m_holdingThreadId;
#endif

public:
    SpinLock ();

    //Init method, initialize lock and _DEBUG flags
    void Init(LOCK_TYPE type, bool RequireCoopGC = FALSE);

    //-----------------------------------------------------------------
    // Is the current thread the owner?
    //-----------------------------------------------------------------
#ifdef _DEBUG
    BOOL OwnedByCurrentThread();
#endif

private:
    void SpinToAcquire (); // out of line call spins

#ifdef _DEBUG
    void dbg_PreEnterLock();
    void dbg_EnterLock();
    void dbg_LeaveLock();
#endif

    // The following 5 APIs must remain private.  We want all entry/exit code to
    // occur via holders, so that exceptions will be sure to release the lock.
private:
    void GetLock();                     // Acquire lock, blocks if unsuccessful
    BOOL GetLockNoWait();               // Acquire lock, fail-fast
    void FreeLock();                    // Release lock

public:
    static void AcquireLock(SpinLock *s);
    static void ReleaseLock(SpinLock *s);

    class Holder
    {
        SpinLock *  m_pSpinLock;
    public:
        Holder(SpinLock * s) :
          m_pSpinLock(s)
        {
            SCAN_SCOPE_BEGIN;
            STATIC_CONTRACT_GC_NOTRIGGER;

            m_pSpinLock->GetLock();
        }

        ~Holder()
        {
            SCAN_SCOPE_END;

            m_pSpinLock->FreeLock();
        }
    };
};


typedef SpinLock::Holder SpinLockHolder;
#define TAKE_SPINLOCK_AND_DONOT_TRIGGER_GC(lock) \
                        SpinLockHolder __spinLockHolder(lock);\
                        GCX_NOTRIGGER ();

#define ACQUIRE_SPINLOCK_NO_HOLDER(lock)        \
{                                               \
    SpinLock::AcquireLock(lock);                \
    GCX_NOTRIGGER();                            \
    CANNOTTHROWCOMPLUSEXCEPTION();              \
    STATIC_CONTRACT_NOTHROW;                    \


#define RELEASE_SPINLOCK_NO_HOLDER(lock)        \
    SpinLock::ReleaseLock(lock);                \
}                                               \

__inline BOOL IsOwnerOfSpinLock (LPVOID lock)
{
    WRAPPER_NO_CONTRACT;
#ifdef _DEBUG
    return ((SpinLock*)lock)->OwnedByCurrentThread();
#else
    // This function should not be called on free build.
    DebugBreak();
    return TRUE;
#endif
}

#ifdef _DEBUG
//----------------------------------------------------------------------------
// class SpinLockProfiler
//  to track contention, useful for profiling
//
//----------------------------------------------------------------------------
class SpinLockProfiler
{
    // Pointer to spinlock names.
    //
    static ULONG    s_ulBackOffs;
    static ULONG    s_ulCollisons [LOCK_TYPE_DEFAULT + 1];
    static ULONG    s_ulSpins [LOCK_TYPE_DEFAULT + 1];

public:

    static void InitStatics ();

    static void IncrementSpins (LOCK_TYPE type, ULONG value);

    static void IncrementCollisions (LOCK_TYPE type);

    static void IncrementBackoffs (ULONG value);

    static void DumpStatics();

};

#endif  // ifdef _DEBUG
#endif //  ifndef _H_SPINLOCK_

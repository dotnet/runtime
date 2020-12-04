// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef _SimpleRWLock_hpp_
#define _SimpleRWLock_hpp_

#include "threads.h"

class SimpleRWLock;

//-------------------------------------------------------------------------------------------
// GC_MODE defines custom CONTRACTs for TryEnterRead and TryEnterWrite.
//
// Contract differs when acquiring the lock depending on its lock mode.
//
// GC/MODE
//     A SimpleRWLock can be one of the following modes. We only want to see the "PREEMPTIVE"
//     type used in new code. Other types, kept for legacy reasons, are listed in
//     order from least objectionable to most objectionable.
//
//         PREEMPTIVE (equivalent to CRST's "normal")
//            This is the preferred type of crst. Enter() will force-switch your thread
//            into preemptive mode if it isn't already. Thus, the effective contract is:
//
//            MODE_ANY
//            GC_TRIGGERS
//
//
//
//         COOPERATIVE (equivalent to CRST_UNSAFE_COOPGC)
//            You can only attempt to acquire this crst if you're already in coop mode. It is
//            guaranteed no GC will occur while waiting to acquire the lock.  While you hold
//            the lock, your thread is in a GCFORBID state.
//
//            MODE_COOP
//            GC_NOTRIGGER
//
//
//
//         COOPERATIVE_OR_PREEMPTIVE (equivalent to CRST_UNSAFE_ANYMODE)
//            You can attempt to acquire this in either mode. Entering the crst will not change
//            your thread mode but it will increment the GCNoTrigger count.
//
//            MODE_ANY
//            GC_NOTRIGGER
//------------------------------------------------------------------------------------------------
enum GC_MODE {
    COOPERATIVE,
    PREEMPTIVE,
    COOPERATIVE_OR_PREEMPTIVE} ;

class SimpleRWLock
{
    // Allow Module access so we can use Offsetof on this class's private members during native image creation (determinism)
    friend class Module;
private:
    BOOL IsWriterWaiting()
    {
        LIMITED_METHOD_CONTRACT;
        return m_WriterWaiting != 0;
    }

    void SetWriterWaiting()
    {
        LIMITED_METHOD_CONTRACT;
        m_WriterWaiting = 1;
    }

    void ResetWriterWaiting()
    {
        LIMITED_METHOD_CONTRACT;
        m_WriterWaiting = 0;
    }

    BOOL TryEnterRead();

    BOOL TryEnterWrite();

#ifdef ENABLE_CONTRACTS_IMPL
    void CheckGCNoTrigger();
#endif  //ENABLE_CONTRACTS_IMPL

    // lock used for R/W synchronization
    Volatile<LONG>                m_RWLock;

    // Does this lock require to be taken in PreemptiveGC mode?
    const GC_MODE          m_gcMode;

    // spin count for a reader waiting for a writer to release the lock
    LONG                m_spinCount;

    // used to prevent writers from being starved by readers
    // we currently do not prevent writers from starving readers since writers
    // are supposed to be rare.
    BOOL                m_WriterWaiting;

#ifdef _DEBUG
    // Check for dead lock situation.
    Volatile<LONG>      m_countNoTriggerGC;

#ifdef HOST_64BIT
    // ensures that we are a multiple of 8-bytes
    UINT32 pad;
#endif

    void                PostEnter ();
    void                PreEnter ();
    void                PreLeave ();
#endif //_DEBUG

#ifndef DACCESS_COMPILE
    static void AcquireReadLock(SimpleRWLock *s) { LIMITED_METHOD_CONTRACT; s->EnterRead(); }
    static void ReleaseReadLock(SimpleRWLock *s) { LIMITED_METHOD_CONTRACT; s->LeaveRead(); }

    static void AcquireWriteLock(SimpleRWLock *s) { LIMITED_METHOD_CONTRACT; s->EnterWrite(); }
    static void ReleaseWriteLock(SimpleRWLock *s) { LIMITED_METHOD_CONTRACT; s->LeaveWrite(); }
#else // DACCESS_COMPILE
    // in DAC builds, we don't actually acquire the lock, we just determine whether the LS
    // already holds it. If so, we assume the data is inconsistent and throw an exception.
    // Argument:
    //     input: s - the lock to be checked.
    // Note: Throws
    static void AcquireReadLock(SimpleRWLock *s)
    {
        SUPPORTS_DAC;
        if (s->IsWriterLock())
        {
            ThrowHR(CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
        }
    };
    static void ReleaseReadLock(SimpleRWLock *s) { };

    static void AcquireWriteLock(SimpleRWLock *s) { SUPPORTS_DAC; ThrowHR(CORDBG_E_TARGET_READONLY); };
    static void ReleaseWriteLock(SimpleRWLock *s) { };
#endif // DACCESS_COMPILE

public:
    SimpleRWLock (GC_MODE gcMode, LOCK_TYPE locktype)
        : m_gcMode (gcMode)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        m_RWLock = 0;
        m_spinCount = (GetCurrentProcessCpuCount() == 1) ? 0 : 4000;
        m_WriterWaiting = FALSE;

#ifdef _DEBUG
        m_countNoTriggerGC = 0;
#endif
    }

    // Special empty CTOR for DAC. We still need to assign to const fields, but they won't actually be used.
    SimpleRWLock()
        : m_gcMode(COOPERATIVE_OR_PREEMPTIVE)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
        m_countNoTriggerGC = 0;
#endif //_DEBUG
    }

#ifndef DACCESS_COMPILE
    // Acquire the reader lock.
    void EnterRead();

    // Acquire the writer lock.
    void EnterWrite();

    // Leave the reader lock.
    void LeaveRead()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
        PreLeave ();
#endif //_DEBUG
        LONG RWLock;
        RWLock = InterlockedDecrement(&m_RWLock);
        _ASSERTE (RWLock >= 0);
        EE_LOCK_RELEASED(this);
    }

    // Leave the writer lock.
    void LeaveWrite()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
        PreLeave ();
#endif //_DEBUG
        LONG RWLock;
        RWLock = InterlockedExchange (&m_RWLock, 0);
        _ASSERTE(RWLock == -1);
        EE_LOCK_RELEASED(this);
    }

#endif // DACCESS_COMPILE

    typedef DacHolder<SimpleRWLock *, SimpleRWLock::AcquireReadLock, SimpleRWLock::ReleaseReadLock> SimpleReadLockHolder;
    typedef DacHolder<SimpleRWLock *, SimpleRWLock::AcquireWriteLock, SimpleRWLock::ReleaseWriteLock> SimpleWriteLockHolder;

#ifdef _DEBUG
    BOOL LockTaken ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_RWLock != 0;
    }

    BOOL IsReaderLock ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_RWLock > 0;
    }

#endif

    BOOL IsWriterLock ()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_RWLock < 0;
    }

};

typedef SimpleRWLock::SimpleReadLockHolder SimpleReadLockHolder;
typedef SimpleRWLock::SimpleWriteLockHolder SimpleWriteLockHolder;
typedef DPTR(SimpleRWLock) PTR_SimpleRWLock;

#ifdef TEST_DATA_CONSISTENCY
// used for test purposes. Determines if a crst is held.
// Arguments:
//     input: pLock - the lock to test
// Note: Throws if the lock is held

FORCEINLINE void DebugTryRWLock(SimpleRWLock * pLock)
{
    SUPPORTS_DAC;

    SimpleReadLockHolder rwLock(pLock);
}
#endif // TEST_DATA_CONSISTENCY
#endif

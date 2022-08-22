// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// CRST.H
//

//
// Debug-instrumented hierarchical critical sections.
//
//
// The hierarchy:
// --------------
//    The EE divides critical sections into numbered groups or "levels."
//    Crsts that guard the lowest level data structures that don't
//    use other services are grouped into the lowest-numbered levels.
//    The higher-numbered levels are reserved for high-level crsts
//    that guard broad swatches of code. Multiple groups can share the
//    same number to indicate that they're disjoint (their locks will never
//    nest.)
//
//    The fundamental rule of the hierarchy that threads can only request
//    a crst whose level is lower than any crst currently held by the thread.
//    E.g. if a thread current holds a level-3 crst, it can try to enter
//    a level-2 crst, but not a level-4 crst, nor a different level-3
//    crst. This prevents the cyclic dependencies that lead to deadlock.
//
//    For debugging purposes Crsts are all also grouped by a type (e.g.
//    CrstRemoting, the type of Crst used to synchronize certain remoting
//    operations). Each type maps to one level (though a level may map to
//    multiple types). The idea here is for the programmer to express Crst types
//    and their dependencies (e.g. a CrstClassInit instance may be acquired
//    while a CrstRemoting instance is already held) in a high level manner
//    while an external script handles the mechanical process of assigning
//    numerical levels to each type. See file:..\inc\CrstTypes.def for these high level
//    type definitions.
//
//
// To create a crst:
//
//    Crst *pcrst = new Crst(type);
//
//      where "type" is one of the enums created in the auto-generated
//      file:..\inc\CrstTypes.h header file (matching the definition in
//      file:..\inc\CrstTypes.def).
//
//      By default, crsts don't support nested enters by the same thread. If
//      you need reentrancy, use the alternate form:
//
//    Crst *pcrst = new Crst(type, TRUE);
//
//      Since reentrancies never block the caller, they're allowed to
//     "violate" the level ordering rule.
//
//
// To enter/leave a crst:
// ----------------------
//
//
//    pcrst->Enter();
//    pcrst->Leave();
//
// An assertion will fire on Enter() if a thread attempts to take locks
// in the wrong order.
//
// Finally, a few DEBUG-only methods:
//
// To assert taking a crst won't violate level order:
// --------------------------------------------------
//
//    _ASSERTE(pcrst->IsSafeToTake());
//
//    This is a good line to put at the start of any function that
//    enters a crst in some circumstances but not others. If it
//    always enters the crst, it's not necessary to call IsSafeToTake()
//    since Enter() does this for you.
//
// To assert that the current thread owns a crst:
// --------------------------------------------------
//
//   _ASSERTE(pcrst->OwnedByCurrentThread());



#ifndef __crst_h__
#define __crst_h__

#include "util.hpp"
#include "debugmacros.h"
#include "log.h"

#define ShutDown_Start                          0x00000001
#define ShutDown_Finalize1                      0x00000002
#define ShutDown_Finalize2                      0x00000004
#define ShutDown_Profiler                       0x00000008
#define ShutDown_COM                            0x00000010
#define ShutDown_SyncBlock                      0x00000020
#define ShutDown_IUnknown                       0x00000040
#define ShutDown_Phase2                         0x00000080

#ifndef DACCESS_COMPILE
extern bool g_fProcessDetach;
extern DWORD g_fEEShutDown;
#endif
// Total count of Crst lock  of the type (Shutdown) that are currently in use
extern Volatile<LONG> g_ShutdownCrstUsageCount;
extern Volatile<LONG> g_fForbidEnterEE;

// The CRST.
class CrstBase
{
// The following classes and methods violate the requirement that Crst usage be
// exception-safe, or they satisfy that requirement using techniques other than
// Holder objects:
friend class Thread;
friend class ThreadStore;
friend class ThreadSuspend;
template <typename ELEMENT>
friend class ListLockBase;
template <typename ELEMENT>
friend class ListLockEntryBase;
friend struct SavedExceptionInfo;
friend void ClrEnterCriticalSection(CRITSEC_COOKIE cookie);
friend void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie);
friend class CodeVersionManager;

friend class Debugger;
friend class Crst;

#ifdef FEATURE_DBGIPC_TRANSPORT_VM
    // The debugger transport code uses a holder for its Crst, but it needs to share the holder implementation
    // with its right side code as well (which can't see the Crst implementation and actually uses a
    // CRITICAL_SECTION as the base lock). So make DbgTransportSession a friend here so we can use Enter() and
    // Leave() in order to build a shared holder class.
    friend class DbgTransportLock;
#endif // FEATURE_DBGIPC_TRANSPORT_VM

    // PendingTypeLoadEntry acquires the lock during construction before anybody has a chance to see it to avoid
    // level violations.
    friend class PendingTypeLoadEntry;

public:
#ifdef _DEBUG
    enum NoLevelCheckFlag
    {
        CRST_NO_LEVEL_CHECK = 1,
        CRST_LEVEL_CHECK = 0,
    };
#endif

private:
    // Some Crsts have a "shutdown" mode.
    // A Crst in shutdown mode can only be taken / released by special
    // (the helper / finalizer / shutdown) threads. Any other thread that tries to take
    // the a "shutdown" crst will immediately release the Crst and instead just block forever.
    //
    // This prevents random threads from blocking the special threads from doing finalization on shutdown.
    //
    // Unfortunately, each Crst needs its own "shutdown" flag because we can't convert all the locks
    // into shutdown locks at once. For eg, the TSL needs to suspend the runtime before
    // converting to a shutdown lock. But it can't suspend the runtime while holding
    // a UNSAFE_ANYMODE lock (such as the debugger-lock). So at least the debugger-lock
    // and TSL need to be set separately.
    //
    // So for such Crsts, it's the caller's responsibility to detect if the crst is in
    // shutdown mode, and if so, call this function after enter.
    void ReleaseAndBlockForShutdownIfNotSpecialThread();

    // Enter & Leave are deliberately private to force callers to use the
    // Holder class.  If you bypass the Holder class and access these members
    // directly, your lock is not exception-safe.
    //
    // noLevelCheckFlag parameter lets you disable the crst level checking. This is
    // very dangerous so it is only used when the constructor is the one performing
    // the Enter (that attempt cannot possibly block since the current thread is
    // the only one with a pointer to the crst.)
    //
    // For obvious reasons, this parameter must never be made public.
    void Enter(INDEBUG(NoLevelCheckFlag noLevelCheckFlag = CRST_LEVEL_CHECK));
    void Leave();

    void SpinEnter();

#ifndef DACCESS_COMPILE
    DEBUG_NOINLINE static void AcquireLock(CrstBase *c) {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
        c->Enter();
    }

    DEBUG_NOINLINE static void ReleaseLock(CrstBase *c) {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
        c->Leave();
    }

#else // DACCESS_COMPILE

    // in DAC builds, we don't actually acquire the lock, we just determine whether the LS
    // already holds it. If so, we assume the data is inconsistent and throw an exception.
    // Argument:
    //     input: c - the lock to be checked.
    // Note: Throws
    static void AcquireLock(CrstBase * c)
    {
        SUPPORTS_DAC;
        if (c->GetEnterCount() != 0)
        {
            ThrowHR(CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
        }
    };

    static void ReleaseLock(CrstBase *c)
    {
        SUPPORTS_DAC;
    };
#endif // DACCESS_COMPILE

public:
    //-----------------------------------------------------------------
    // Clean up critical section
    // Safe to call multiple times or on non-initialized critical section
    //-----------------------------------------------------------------
    void Destroy();

#ifdef _DEBUG
    //-----------------------------------------------------------------
    // Check if attempting to take the lock would violate level order.
    //-----------------------------------------------------------------
    BOOL IsSafeToTake();
    // Checks that the lock can be taken
    BOOL Debug_CanTake()
    {
        WRAPPER_NO_CONTRACT;
        // Actually take the lock and release it immediatelly, that will do all the necessary checks
        Enter();
        Leave();
        return TRUE;
    }
    void SetCantLeave(BOOL bSet)
    {
        LIMITED_METHOD_CONTRACT;
        if (bSet)
            InterlockedIncrement(&m_cannotLeave);
        else
        {
            _ASSERTE(m_cannotLeave);
            InterlockedDecrement(&m_cannotLeave);
        }
    };
    //-----------------------------------------------------------------
    // Is the current thread the owner?
    //-----------------------------------------------------------------
    BOOL OwnedByCurrentThread()
    {
        WRAPPER_NO_CONTRACT;
        return m_holderthreadid.IsCurrentThread();
    }

    NOINLINE EEThreadId GetHolderThreadId()
    {
        LIMITED_METHOD_CONTRACT;
        return m_holderthreadid;
    }

#endif //_DEBUG

    //-----------------------------------------------------------------
    // For clients who want to assert whether they are in or out of the
    // region.
    //-----------------------------------------------------------------
    UINT GetEnterCount()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef _DEBUG
        return m_entercount;
#else
        return 0;
#endif //_DEBUG
    }

protected:

    VOID InitWorker(INDEBUG_COMMA(CrstType crstType) CrstFlags flags);

#ifdef _DEBUG
    void DebugInit(CrstType crstType, CrstFlags flags);
    void DebugDestroy();
#endif

    T_CRITICAL_SECTION m_criticalsection;

    typedef enum
    {
        // Mask to indicate reserved flags
        CRST_RESERVED_FLAGS_MASK = 0xC0000000,
        // private flag to indicate initialized Crsts
        CRST_INITIALIZED = 0x80000000,
        // private flag to indicate Crst is OS Critical Section
        CRST_OS_CRIT_SEC = 0x40000000,
        // rest of the flags are CrstFlags
    } CrstReservedFlags;
    DWORD               m_dwFlags;            // Re-entrancy and same level
#ifdef _DEBUG
    UINT                m_entercount;       // # of unmatched Enters.
    CrstType            m_crstType;         // Type enum (should have a descriptive name for debugging)
    const char         *m_tag;              // Stringized form of the tag for easy debugging
    int                 m_crstlevel;        // what level is the crst in?
    EEThreadId          m_holderthreadid;   // current holder (or NULL)
    CrstBase           *m_next;             // link for global linked list
    CrstBase           *m_prev;             // link for global linked list
    Volatile<LONG>      m_cannotLeave;

    // Check for dead lock situation.
    ULONG               m_countNoTriggerGC;

    void                PostEnter ();
    void                PreEnter ();
    void                PreLeave  ();
#endif //_DEBUG

private:

    void SetOSCritSec ()
    {
        m_dwFlags |= CRST_OS_CRIT_SEC;
    }
    void ResetOSCritSec ()
    {
        m_dwFlags &= ~CRST_OS_CRIT_SEC;
    }
    BOOL IsOSCritSec ()
    {
        return m_dwFlags & CRST_OS_CRIT_SEC;
    }
    void SetCrstInitialized()
    {
        m_dwFlags |= CRST_INITIALIZED;
    }

    BOOL IsCrstInitialized()
    {
        return m_dwFlags & CRST_INITIALIZED;
    }

    BOOL CanBeTakenDuringShutdown()
    {
        return m_dwFlags & CRST_TAKEN_DURING_SHUTDOWN;
    }

    void SetFlags(CrstFlags f)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(((CrstFlags)(f & ~CRST_RESERVED_FLAGS_MASK)) == f);
        m_dwFlags = (f & ~CRST_RESERVED_FLAGS_MASK) | (m_dwFlags & CRST_RESERVED_FLAGS_MASK);
    }

    void ResetFlags() // resets the reserved and the CrstFlags
    {
        m_dwFlags = 0;
    }

    // ------------------------------- Holders ------------------------------
public:
    //
    // CrstHolder is optimized for the common use that takes the lock in constructor
    // and releases it in destructor. Users that require all Holder features
    // can use CrstHolderWithState.
    //
    class CrstHolder
    {
        CrstBase * m_pCrst;

    public:
        inline CrstHolder(CrstBase * pCrst)
            : m_pCrst(pCrst)
        {
            WRAPPER_NO_CONTRACT;
            AcquireLock(pCrst);
        }

        inline ~CrstHolder()
        {
            WRAPPER_NO_CONTRACT;
            ReleaseLock(m_pCrst);
        }
    };

    // Note that the holders for CRSTs are used in extremely low stack conditions. Because of this, they
    // aren't allowed to use more than HOLDER_CODE_MINIMUM_STACK_LIMIT pages of stack.
    typedef DacHolder<CrstBase *, CrstBase::AcquireLock, CrstBase::ReleaseLock, 0, CompareDefault> CrstHolderWithState;

    // We have some situations where we're already holding a lock, and we need to release and reacquire the lock across a window.
    // This is a dangerous construct because the backout code can block.
    // Generally, it's better to use a regular CrstHolder, and then use the Release() / Acquire() methods on it.
    // This just exists to convert legacy OS Critical Section patterns over to holders.
    typedef DacHolder<CrstBase *, CrstBase::ReleaseLock, CrstBase::AcquireLock, 0, CompareDefault> UnsafeCrstInverseHolder;

    class CrstAndForbidSuspendForDebuggerHolder
    {
    private:
        CrstBase *m_pCrst;
        Thread *m_pThreadForExitingForbidRegion;

    public:
        CrstAndForbidSuspendForDebuggerHolder(CrstBase *pCrst);
        ~CrstAndForbidSuspendForDebuggerHolder();
    };
};

typedef CrstBase::CrstHolder CrstHolder;
typedef CrstBase::CrstHolderWithState CrstHolderWithState;
typedef CrstBase::CrstAndForbidSuspendForDebuggerHolder CrstAndForbidSuspendForDebuggerHolder;

// The CRST.
class Crst : public CrstBase
{
public:
    void *operator new(size_t size)
    {
        WRAPPER_NO_CONTRACT;
        return new BYTE[size];
    }

    void operator delete(void* mem)
    {
        WRAPPER_NO_CONTRACT;
        delete[] (BYTE*)mem;
    }

private:
    // Do not use inplace operator new on Crst. A wrong destructor would be called if the constructor fails.
    // Use CrstStatic or CrstExplicitInit instead of the inplace operator new.
    void *operator new(size_t size, void *pInPlace);

public:

#ifndef DACCESS_COMPILE

    //-----------------------------------------------------------------
    // Constructor.
    //-----------------------------------------------------------------
    Crst(CrstType crstType, CrstFlags flags = CRST_DEFAULT)
    {
        WRAPPER_NO_CONTRACT;

        // throw away the debug-only parameter in retail
        InitWorker(INDEBUG_COMMA(crstType) flags);
    }

    //-----------------------------------------------------------------
    // Destructor.
    //-----------------------------------------------------------------
    ~Crst()
    {
        WRAPPER_NO_CONTRACT;

        Destroy();
    };

#else

    Crst(CrstType crstType, CrstFlags flags = CRST_DEFAULT) {
        LIMITED_METHOD_CONTRACT;
    };

#endif

    Crst() {
        LIMITED_METHOD_CONTRACT;
    }
};

typedef DPTR(Crst) PTR_Crst;

/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */
class CrstStatic : public CrstBase
{
public:
    VOID Init(CrstType crstType, CrstFlags flags = CRST_DEFAULT)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE((flags & CRST_INITIALIZED) == 0);

        // throw away the debug-only parameter in retail
        InitWorker(INDEBUG_COMMA(crstType) flags);
    }

    bool InitNoThrow(CrstType crstType, CrstFlags flags = CRST_DEFAULT)
    {
        CONTRACTL {
            NOTHROW;
        } CONTRACTL_END;

        _ASSERTE((flags & CRST_INITIALIZED) == 0);

        bool fSuccess = false;

        EX_TRY
        {
            // throw away the debug-only parameter in retail
            InitWorker(INDEBUG_COMMA(crstType) flags);
            fSuccess = true;
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)

        return fSuccess;
    }
};

/* to be used as regular variable when a explicit call to Init method is needed */
class CrstExplicitInit : public CrstStatic
{
public:
    CrstExplicitInit() {
        m_dwFlags = 0;
    }
     ~CrstExplicitInit() {
#ifndef DACCESS_COMPILE
        Destroy();
#endif
    }
};

__inline BOOL IsOwnerOfCrst(LPVOID lock)
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    return ((Crst*)lock)->OwnedByCurrentThread();
#else
    // This function should not be called on free build.
    DebugBreak();
    return TRUE;
#endif
}

#ifdef TEST_DATA_CONSISTENCY
// used for test purposes. Determines if a crst is held.
void DebugTryCrst(CrstBase * pLock);
#endif
#endif // __crst_h__

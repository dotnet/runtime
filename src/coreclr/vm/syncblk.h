// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// SYNCBLK.H
//

//
// Definition of a SyncBlock and the SyncBlockCache which manages it

// See file:#SyncBlockOverview Sync block overview

#ifndef _SYNCBLK_H_
#define _SYNCBLK_H_

#include "util.hpp"
#include "slist.h"
#include "crst.h"
#include "vars.hpp"

// #SyncBlockOverview
//
// Every Object is preceded by an ObjHeader (at a negative offset). The code:ObjHeader has an index to a
// code:SyncBlock. This index is 0 for the bulk of all instances, which indicates that the object shares a
// dummy SyncBlock with most other objects.
//
// The SyncBlock is primarily responsible for object synchronization. However, it is also a "kitchen sink" of
// sparsely allocated instance data. For instance, the default implementation of Hash() is based on the
// existence of a code:SyncTableEntry. And objects exposed to or from COM, or through context boundaries, can
// store sparse data here.
//
// SyncTableEntries and SyncBlocks are allocated in non-GC memory. A weak pointer from the SyncTableEntry to
// the instance is used to ensure that the SyncBlock and SyncTableEntry are reclaimed (recycled) when the
// instance dies.
//
// The organization of the SyncBlocks isn't intuitive (at least to me). Here's the explanation:
//
// Before each Object is an code:ObjHeader. If the object has a code:SyncBlock, the code:ObjHeader contains a
// non-0 index to it.
//
// The index is looked up in the code:g_pSyncTable of SyncTableEntries. This means the table is consecutive
// for all outstanding indices. Whenever it needs to grow, it doubles in size and copies all the original
// entries. The old table is kept until GC time, when it can be safely discarded.
//
// Each code:SyncTableEntry has a backpointer to the object and a forward pointer to the actual SyncBlock.
// The SyncBlock is allocated out of a SyncBlockArray which is essentially just a block of SyncBlocks.
//
// The code:SyncBlockArray s are managed by a code:SyncBlockCache that handles the actual allocations and
// frees of the blocks.
//
// So...
//
// Each allocation and release has to handle free lists in the table of entries and the table of blocks.
//
// We burn an extra 4 bytes for the pointer from the SyncTableEntry to the SyncBlock.
//
// The reason for this is that many objects have a SyncTableEntry but no SyncBlock. That's because someone
// (e.g. HashTable) called Hash() on them.
//
// Incidentally, there's a better write-up of all this stuff in the archives.

#ifdef TARGET_X86
#include <pshpack4.h>
#endif // TARGET_X86

// forwards:
class SyncBlock;
class SyncBlockCache;
class SyncTableEntry;
class SyncBlockArray;
class AwareLock;
class Thread;
class AppDomain;

#ifdef EnC_SUPPORTED
class EnCSyncBlockInfo;
typedef DPTR(EnCSyncBlockInfo) PTR_EnCSyncBlockInfo;
#endif // EnC_SUPPORTED

#include "eventstore.hpp"
#include "synch.h"

// At a negative offset from each Object is an ObjHeader.  The 'size' of the
// object includes these bytes.  However, we rely on the previous object allocation
// to zero out the ObjHeader for the current allocation.  And the limits of the
// GC space are initialized to respect this "off by one" error.

// m_SyncBlockValue is carved up into an index and a set of bits.  Steal bits by
// reducing the mask.  We use the very high bit, in _DEBUG, to be sure we never forget
// to mask the Value to obtain the Index

#define BIT_SBLK_UNUSED                     0x80000000
#define BIT_SBLK_FINALIZER_RUN              0x40000000
#define BIT_SBLK_GC_RESERVE                 0x20000000

// This lock is only taken when we need to modify the index value in m_SyncBlockValue.
// It should not be taken if the object already has a real syncblock index.
#define BIT_SBLK_SPIN_LOCK                  0x10000000

#define BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX    0x08000000

// if BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX is clear, the rest of the header dword is laid out as follows:
// - lower ten bits (bits 0 thru 9) is thread id used for the thin locks
//   value is zero if no thread is holding the lock
// - following six bits (bits 10 thru 15) is recursion level used for the thin locks
//   value is zero if lock is not taken or only taken once by the same thread
#define SBLK_MASK_LOCK_THREADID             0x000003FF   // special value of 0 + 1023 thread ids
#define SBLK_MASK_LOCK_RECLEVEL             0x0000FC00   // 64 recursion levels
#define SBLK_LOCK_RECLEVEL_INC              0x00000400   // each level is this much higher than the previous one
#define SBLK_RECLEVEL_SHIFT                 10           // shift right this much to get recursion level

// add more bits here... (adjusting the following mask to make room)

// if BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX is set,
// then if BIT_SBLK_IS_HASHCODE is also set, the rest of the dword is the hash code (bits 0 thru 25),
// otherwise the rest of the dword is the sync block index (bits 0 thru 25)
#define BIT_SBLK_IS_HASHCODE            0x04000000

#define HASHCODE_BITS                   26

#define MASK_HASHCODE                   ((1<<HASHCODE_BITS)-1)
#define SYNCBLOCKINDEX_BITS             26
#define MASK_SYNCBLOCKINDEX             ((1<<SYNCBLOCKINDEX_BITS)-1)

// Spin for about 1000 cycles before waiting longer.
#define     BIT_SBLK_SPIN_COUNT         1000

// The GC is highly dependent on SIZE_OF_OBJHEADER being exactly the sizeof(ObjHeader)
// We define this macro so that the preprocessor can calculate padding structures.
#ifdef HOST_64BIT
#define SIZEOF_OBJHEADER    8
#else // !HOST_64BIT
#define SIZEOF_OBJHEADER    4
#endif // !HOST_64BIT


inline void InitializeSpinConstants()
{
    WRAPPER_NO_CONTRACT;

#if !defined(DACCESS_COMPILE)
    g_SpinConstants.dwInitialDuration = g_pConfig->SpinInitialDuration();
    g_SpinConstants.dwMaximumDuration = min(g_pConfig->SpinLimitProcCap(), g_SystemInfo.dwNumberOfProcessors) * g_pConfig->SpinLimitProcFactor() + g_pConfig->SpinLimitConstant();
    g_SpinConstants.dwBackoffFactor   = g_pConfig->SpinBackoffFactor();
    g_SpinConstants.dwRepetitions     = g_pConfig->SpinRetryCount();
    g_SpinConstants.dwMonitorSpinCount = g_SpinConstants.dwMaximumDuration == 0 ? 0 : g_pConfig->MonitorSpinCount();
#endif
}

// this is a 'GC-aware' Lock.  It is careful to enable preemptive GC before it
// attempts any operation that can block.  Once the operation is finished, it
// restores the original state of GC.

// AwareLocks can only be created inside SyncBlocks, since they depend on the
// enclosing SyncBlock for coordination.  This is enforced by the private ctor.
typedef DPTR(class AwareLock) PTR_AwareLock;

class AwareLock
{
    friend class CheckAsmOffsets;

    friend class SyncBlockCache;
    friend class SyncBlock;

public:
    enum EnterHelperResult {
        EnterHelperResult_Entered,
        EnterHelperResult_Contention,
        EnterHelperResult_UseSlowPath
    };

    enum LeaveHelperAction {
        LeaveHelperAction_None,
        LeaveHelperAction_Signal,
        LeaveHelperAction_Yield,
        LeaveHelperAction_Contention,
        LeaveHelperAction_Error,
    };

private:
    class LockState
    {
    private:
        // Layout constants for m_state
        static const UINT32 IsLockedMask = (UINT32)1 << 0; // bit 0
        static const UINT32 ShouldNotPreemptWaitersMask = (UINT32)1 << 1; // bit 1
        static const UINT32 SpinnerCountIncrement = (UINT32)1 << 2;
        static const UINT32 SpinnerCountMask = (UINT32)0x7 << 2; // bits 2-4
        static const UINT32 IsWaiterSignaledToWakeMask = (UINT32)1 << 5; // bit 5
        static const UINT8 WaiterCountShift = 6;
        static const UINT32 WaiterCountIncrement = (UINT32)1 << WaiterCountShift;
        static const UINT32 WaiterCountMask = (UINT32)-1 >> WaiterCountShift << WaiterCountShift; // bits 6-31

    private:
        UINT32 m_state;

    public:
        LockState(UINT32 state = 0) : m_state(state)
        {
            LIMITED_METHOD_CONTRACT;
        }

    public:
        UINT32 GetState() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_state;
        }

        UINT32 GetMonitorHeldState() const
        {
            LIMITED_METHOD_CONTRACT;
            static_assert_no_msg(IsLockedMask == 1);
            static_assert_no_msg(WaiterCountShift >= 1);

            // Return only the locked state and waiter count in the previous (m_MonitorHeld) layout for the debugger:
            //   bit 0: 1 if locked, 0 otherwise
            //   bits 1-31: waiter count
            UINT32 state = m_state;
            return (state & IsLockedMask) + (state >> WaiterCountShift << 1);
        }

    public:
        bool IsUnlockedWithNoWaiters() const
        {
            LIMITED_METHOD_CONTRACT;
            return !(m_state & (IsLockedMask + WaiterCountMask));
        }

        void InitializeToLockedWithNoWaiters()
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(!m_state);

            m_state = IsLockedMask;
        }

    public:
        bool IsLocked() const
        {
            LIMITED_METHOD_CONTRACT;
            return !!(m_state & IsLockedMask);
        }

    private:
        void InvertIsLocked()
        {
            LIMITED_METHOD_CONTRACT;
            m_state ^= IsLockedMask;
        }

    public:
        bool ShouldNotPreemptWaiters() const
        {
            LIMITED_METHOD_CONTRACT;
            return !!(m_state & ShouldNotPreemptWaitersMask);
        }

    private:
        void InvertShouldNotPreemptWaiters()
        {
            WRAPPER_NO_CONTRACT;

            m_state ^= ShouldNotPreemptWaitersMask;
            _ASSERTE(!ShouldNotPreemptWaiters() || HasAnyWaiters());
        }

        bool ShouldNonWaiterAttemptToAcquireLock() const
        {
            WRAPPER_NO_CONTRACT;
            _ASSERTE(!ShouldNotPreemptWaiters() || HasAnyWaiters());

            return !(m_state & (IsLockedMask + ShouldNotPreemptWaitersMask));
        }

    public:
        bool HasAnySpinners() const
        {
            LIMITED_METHOD_CONTRACT;
            return !!(m_state & SpinnerCountMask);
        }

    private:
        bool TryIncrementSpinnerCount()
        {
            WRAPPER_NO_CONTRACT;

            LockState newState = m_state + SpinnerCountIncrement;
            if (newState.HasAnySpinners()) // overflow check
            {
                m_state = newState;
                return true;
            }
            return false;
        }

        void DecrementSpinnerCount()
        {
            WRAPPER_NO_CONTRACT;
            _ASSERTE(HasAnySpinners());

            m_state -= SpinnerCountIncrement;
        }

    public:
        bool IsWaiterSignaledToWake() const
        {
            LIMITED_METHOD_CONTRACT;
            return !!(m_state & IsWaiterSignaledToWakeMask);
        }

    private:
        void InvertIsWaiterSignaledToWake()
        {
            LIMITED_METHOD_CONTRACT;
            m_state ^= IsWaiterSignaledToWakeMask;
        }

    public:
        bool HasAnyWaiters() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_state >= WaiterCountIncrement;
        }

    private:
        void IncrementWaiterCount()
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(m_state + WaiterCountIncrement >= WaiterCountIncrement);

            m_state += WaiterCountIncrement;
        }

        void DecrementWaiterCount()
        {
            WRAPPER_NO_CONTRACT;
            _ASSERTE(HasAnyWaiters());

            m_state -= WaiterCountIncrement;
        }

    private:
        bool NeedToSignalWaiter() const
        {
            WRAPPER_NO_CONTRACT;
            return HasAnyWaiters() && !(m_state & (SpinnerCountMask + IsWaiterSignaledToWakeMask));
        }

    private:
        operator UINT32() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_state;
        }

        LockState &operator =(UINT32 state)
        {
            LIMITED_METHOD_CONTRACT;

            m_state = state;
            return *this;
        }

    public:
        LockState VolatileLoadWithoutBarrier() const
        {
            WRAPPER_NO_CONTRACT;
            return ::VolatileLoadWithoutBarrier(&m_state);
        }

        LockState VolatileLoad() const
        {
            WRAPPER_NO_CONTRACT;
            return ::VolatileLoad(&m_state);
        }

    private:
        LockState CompareExchange(LockState toState, LockState fromState)
        {
            LIMITED_METHOD_CONTRACT;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            return (UINT32)FastInterlockedCompareExchange((LONG *)&m_state, (LONG)toState, (LONG)fromState);
#else
            return (UINT32)InterlockedCompareExchange((LONG *)&m_state, (LONG)toState, (LONG)fromState);
#endif
        }

        LockState CompareExchangeAcquire(LockState toState, LockState fromState)
        {
            LIMITED_METHOD_CONTRACT;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            return (UINT32)FastInterlockedCompareExchangeAcquire((LONG *)&m_state, (LONG)toState, (LONG)fromState);
#else
            return (UINT32)InterlockedCompareExchangeAcquire((LONG *)&m_state, (LONG)toState, (LONG)fromState);
#endif
        }

    public:
        bool InterlockedTryLock();
        bool InterlockedTryLock(LockState state);
        bool InterlockedUnlock();
        bool InterlockedTrySetShouldNotPreemptWaitersIfNecessary(AwareLock *awareLock);
        bool InterlockedTrySetShouldNotPreemptWaitersIfNecessary(AwareLock *awareLock, LockState state);
        EnterHelperResult InterlockedTry_LockOrRegisterSpinner(LockState state);
        EnterHelperResult InterlockedTry_LockAndUnregisterSpinner();
        bool InterlockedUnregisterSpinner_TryLock();
        bool InterlockedTryLock_Or_RegisterWaiter(AwareLock *awareLock, LockState state);
        void InterlockedUnregisterWaiter();
        bool InterlockedTry_LockAndUnregisterWaiterAndObserveWakeSignal(AwareLock *awareLock);
        bool InterlockedObserveWakeSignal_Try_LockAndUnregisterWaiter(AwareLock *awareLock);
    };

    friend class LockState;

private:
    // Take care to use 'm_lockState.VolatileLoadWithoutBarrier()` when loading this value into a local variable that will be
    // reused. That prevents an optimization in the compiler that avoids stack-spilling a value loaded from memory and instead
    // reloads the value from the original memory location under the assumption that it would not be changed by another thread,
    // which can result in the local variable's value changing between reads if the memory location is modifed by another
    // thread. This is important for patterns such as:
    //
    //     T x = m_x; // no barrier
    //     if (meetsCondition(x))
    //     {
    //         assert(meetsCondition(x)); // This may fail!
    //     }
    //
    // The code should be written like this instead:
    //
    //     T x = VolatileLoadWithoutBarrier(&m_x); // compile-time barrier, no run-time barrier
    //     if (meetsCondition(x))
    //     {
    //         assert(meetsCondition(x)); // This will not fail
    //     }
    LockState m_lockState;

    ULONG           m_Recursion;
    PTR_Thread      m_HoldingThread;
    SIZE_T          m_HoldingOSThreadId;

    LONG            m_TransientPrecious;


    // This is a backpointer from the syncblock to the synctable entry.  This allows
    // us to recover the object that holds the syncblock.
    DWORD           m_dwSyncIndex;

    CLREvent        m_SemEvent;

    DWORD m_waiterStarvationStartTimeMs;
    int m_emittedLockCreatedEvent;

    static const DWORD WaiterStarvationDurationMsBeforeStoppingPreemptingWaiters = 100;

    // Only SyncBlocks can create AwareLocks.  Hence this private constructor.
    AwareLock(DWORD indx)
        : m_Recursion(0),
#ifndef DACCESS_COMPILE
// PreFAST has trouble with initializing a NULL PTR_Thread.
          m_HoldingThread(NULL),
#endif // DACCESS_COMPILE
          m_HoldingOSThreadId(0),
          m_TransientPrecious(0),
          m_dwSyncIndex(indx),
          m_waiterStarvationStartTimeMs(0),
          m_emittedLockCreatedEvent(0)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~AwareLock()
    {
        LIMITED_METHOD_CONTRACT;
        // We deliberately allow this to remain incremented if an exception blows
        // through a lock attempt.  This simply prevents the GC from aggressively
        // reclaiming a particular syncblock until the associated object is garbage.
        // From a perf perspective, it's not worth using SEH to prevent this from
        // happening.
        //
        // _ASSERTE(m_TransientPrecious == 0);
    }

#if defined(ENABLE_CONTRACTS_IMPL)
    // The LOCK_TAKEN/RELEASED macros need a "pointer" to the lock object to do
    // comparisons between takes & releases (and to provide debugging info to the
    // developer).  Since AwareLocks are always allocated embedded inside SyncBlocks,
    // and since SyncBlocks don't move (unlike the GC objects that use
    // the syncblocks), it's safe for us to just use the AwareLock pointer directly
    void * GetPtrForLockContract()
    {
        return (void *) this;
    }
#endif // defined(ENABLE_CONTRACTS_IMPL)

public:
    UINT32 GetLockState() const
    {
        WRAPPER_NO_CONTRACT;
        return m_lockState.VolatileLoadWithoutBarrier().GetState();
    }

    bool IsUnlockedWithNoWaiters() const
    {
        WRAPPER_NO_CONTRACT;
        return m_lockState.VolatileLoadWithoutBarrier().IsUnlockedWithNoWaiters();
    }

    UINT32 GetMonitorHeldStateVolatile() const
    {
        WRAPPER_NO_CONTRACT;
        return m_lockState.VolatileLoad().GetMonitorHeldState();
    }

    ULONG GetRecursionLevel() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_Recursion;
    }

    PTR_Thread GetHoldingThread() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_HoldingThread;
    }

private:
    void ResetWaiterStarvationStartTime();
    void RecordWaiterStarvationStartTime();
    bool ShouldStopPreemptingWaiters() const;

private: // friend access is required for this unsafe function
    void InitializeToLockedWithNoWaiters(ULONG recursionLevel, PTR_Thread holdingThread, SIZE_T holdingOSThreadId)
    {
        WRAPPER_NO_CONTRACT;

        m_lockState.InitializeToLockedWithNoWaiters();
        m_Recursion = recursionLevel;
        m_HoldingThread = holdingThread;
        m_HoldingOSThreadId = holdingOSThreadId;
    }

public:
    static void SpinWait(const YieldProcessorNormalizationInfo &normalizationInfo, DWORD spinIteration);

    // Helper encapsulating the fast path entering monitor. Returns what kind of result was achieved.
    bool TryEnterHelper(Thread* pCurThread);

    EnterHelperResult TryEnterBeforeSpinLoopHelper(Thread *pCurThread);
    EnterHelperResult TryEnterInsideSpinLoopHelper(Thread *pCurThread);
    bool TryEnterAfterSpinLoopHelper(Thread *pCurThread);

    // Helper encapsulating the core logic for leaving monitor. Returns what kind of
    // follow up action is necessary
    AwareLock::LeaveHelperAction LeaveHelper(Thread* pCurThread);

    void    Enter();
    BOOL    TryEnter(INT32 timeOut = 0);
    BOOL    EnterEpilog(Thread *pCurThread, INT32 timeOut = INFINITE);
    BOOL    EnterEpilogHelper(Thread *pCurThread, INT32 timeOut);
    BOOL    Leave();

    void    Signal()
    {
        WRAPPER_NO_CONTRACT;

        // CLREvent::SetMonitorEvent works even if the event has not been initialized yet
        m_SemEvent.SetMonitorEvent();

        m_lockState.InterlockedTrySetShouldNotPreemptWaitersIfNecessary(this);
    }

    void    AllocLockSemEvent();
    LONG    LeaveCompletely();
    BOOL    OwnedByCurrentThread();

    void    IncrementTransientPrecious()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement(&m_TransientPrecious);
        _ASSERTE(m_TransientPrecious > 0);
    }

    void    DecrementTransientPrecious()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_TransientPrecious > 0);
        InterlockedDecrement(&m_TransientPrecious);
    }

    DWORD GetSyncBlockIndex();

    void SetPrecious();

    // Provide access to the object associated with this awarelock, so client can
    // protect it.
    inline OBJECTREF GetOwningObject();

    // Provide access to the Thread object that owns this awarelock.  This is used
    // to provide a host to find out owner of a lock.
    inline PTR_Thread GetOwningThread()
    {
        LIMITED_METHOD_CONTRACT;
        return m_HoldingThread;
    }
};

#ifdef FEATURE_COMINTEROP
class ComCallWrapper;
class ComClassFactory;
struct RCW;
class RCWHolder;
typedef DPTR(class ComCallWrapper)        PTR_ComCallWrapper;

#include "shash.h"
#endif // FEATURE_COMINTEROP

using ManagedObjectComWrapperByIdMap = MapSHash<INT64, void*>;
class InteropSyncBlockInfo
{
    friend class RCWHolder;
    friend class ClrDataAccess;

public:
#ifndef TARGET_UNIX
    // List of InteropSyncBlockInfo instances that have been freed since the last syncblock cleanup.
    static SLIST_HEADER s_InteropInfoStandbyList;
#endif // !TARGET_UNIX

    InteropSyncBlockInfo()
        : m_pUMEntryThunk{}
#ifdef FEATURE_COMINTEROP
        , m_pCCW{}
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        , m_pCCF{}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        , m_pRCW{}
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_COMWRAPPERS
        , m_externalComObjectContext{}
        , m_managedObjectComWrapperLock{}
        , m_managedObjectComWrapperMap{}
#endif // FEATURE_COMWRAPPERS
#ifdef FEATURE_OBJCMARSHAL
        , m_taggedMemory{}
        , m_taggedAlloc{}
#endif // FEATURE_OBJCMARSHAL
    {
        LIMITED_METHOD_CONTRACT;

#if defined(FEATURE_COMWRAPPERS)
        // The GC thread does enumerate these objects so add CRST_UNSAFE_COOPGC.
        m_managedObjectComWrapperLock.Init(CrstManagedObjectWrapperMap, CRST_UNSAFE_COOPGC);
#endif // FEATURE_COMWRAPPERS
    }
#ifndef DACCESS_COMPILE
    ~InteropSyncBlockInfo();
#endif

#ifndef TARGET_UNIX
    // Deletes all items in code:s_InteropInfoStandbyList.
    static void FlushStandbyList();
#endif // !TARGET_UNIX

#ifdef FEATURE_COMINTEROP

    //
    // We'll be using the sentinel value of 0x1 to indicate that a particular
    // field was set at one time, but is now NULL.

#ifndef DACCESS_COMPILE
    RCW* GetRawRCW()
    {
        LIMITED_METHOD_CONTRACT;
        return (RCW *)((size_t)m_pRCW & ~1);
    }

    // Returns either NULL or an RCW on which AcquireLock has been called.
    RCW* GetRCWAndIncrementUseCount();

    // Sets the m_pRCW field in a thread-safe manner, pRCW can be NULL.
    void SetRawRCW(RCW* pRCW);

    bool RCWWasUsed()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_pRCW != NULL);
    }
#else // !DACCESS_COMPILE
    TADDR DacGetRawRCW()
    {
        return (TADDR)((size_t)m_pRCW & ~1);
    }
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
    void SetCCW(ComCallWrapper* pCCW)
    {
        LIMITED_METHOD_CONTRACT;

        if (pCCW == NULL)
            pCCW = (ComCallWrapper*) 0x1;

        m_pCCW = pCCW;
    }
#endif // !DACCESS_COMPILE

    PTR_ComCallWrapper GetCCW()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        if (m_pCCW == (PTR_ComCallWrapper)0x1)
            return NULL;

        return m_pCCW;
    }

    bool CCWWasUsed()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pCCW == NULL)
            return false;

        return true;
    }

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    void SetComClassFactory(ComClassFactory* pCCF)
    {
        LIMITED_METHOD_CONTRACT;

        if (pCCF == NULL)
            pCCF = (ComClassFactory*)0x1;

        m_pCCF = pCCF;
    }

    ComClassFactory* GetComClassFactory()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pCCF == (ComClassFactory*)0x1)
            return NULL;

        return m_pCCF;
    }

    bool CCFWasUsed()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pCCF == NULL)
            return false;

        return true;
    }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP

#if !defined(DACCESS_COMPILE)
    // set m_pUMEntryThunk if not already set - return true if not already set
    bool SetUMEntryThunk(void* pUMEntryThunk)
    {
        WRAPPER_NO_CONTRACT;
        return (InterlockedCompareExchangeT(&m_pUMEntryThunk,
                                                    pUMEntryThunk,
                                                    NULL) == NULL);
    }

    void FreeUMEntryThunk();

#endif // DACCESS_COMPILE

    void* GetUMEntryThunk()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pUMEntryThunk;
    }

private:
    // If this is a delegate marshalled out to unmanaged code, this points
    // to the thunk generated for unmanaged code to call back on.
    void*               m_pUMEntryThunk;

#ifdef FEATURE_COMINTEROP
    // If this object is being exposed to COM, it will have an associated CCW object
    PTR_ComCallWrapper  m_pCCW;

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    // If this object represents a type object, it will have an associated class factory
    ComClassFactory*    m_pCCF;
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

public:
#ifndef DACCESS_COMPILE
    // If this is a __ComObject, it will have an associated RCW object
    RCW*                m_pRCW;
#else
    // We can't define this as PTR_RCW, as this would create a typedef cycle. Use TADDR
    // instead.
    TADDR               m_pRCW;
#endif

#endif // FEATURE_COMINTEROP

#if defined(FEATURE_COMWRAPPERS)
public:
    bool TryGetManagedObjectComWrapper(_In_ INT64 wrapperId, _Out_ void** mocw)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        *mocw = NULL;
        if (m_managedObjectComWrapperMap == NULL)
            return false;

        CrstHolder lock(&m_managedObjectComWrapperLock);
        return m_managedObjectComWrapperMap->Lookup(wrapperId, mocw);
    }

#ifndef DACCESS_COMPILE
    bool TrySetManagedObjectComWrapper(_In_ INT64 wrapperId, _In_ void* mocw, _In_ void* curr = NULL)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_managedObjectComWrapperMap == NULL)
        {
            NewHolder<ManagedObjectComWrapperByIdMap> map = new ManagedObjectComWrapperByIdMap();
            if (InterlockedCompareExchangeT((ManagedObjectComWrapperByIdMap**)&m_managedObjectComWrapperMap, (ManagedObjectComWrapperByIdMap *)map, NULL) == NULL)
            {
                map.SuppressRelease();
            }

            _ASSERTE(m_managedObjectComWrapperMap != NULL);
        }

        CrstHolder lock(&m_managedObjectComWrapperLock);

        if (m_managedObjectComWrapperMap->LookupPtr(wrapperId) != curr)
            return false;

        m_managedObjectComWrapperMap->Add(wrapperId, mocw);
        return true;
    }

    using ClearWrappersCallback = void(void* mocw);
    void ClearManagedObjectComWrappers(ClearWrappersCallback* callback)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_managedObjectComWrapperMap == NULL)
            return;

        CQuickArrayList<void*> localList;
        {
            CrstHolder lock(&m_managedObjectComWrapperLock);
            if (callback != NULL)
            {
                ManagedObjectComWrapperByIdMap::Iterator iter = m_managedObjectComWrapperMap->Begin();
                while (iter != m_managedObjectComWrapperMap->End())
                {
                    localList.Push(iter->Value());
                    ++iter;
                }
            }

            m_managedObjectComWrapperMap->RemoveAll();
        }

        for (SIZE_T i = 0; i < localList.Size(); i++)
            callback(localList[i]);
    }

    using EnumWrappersCallback = bool(void* mocw, void* cxt);
    void EnumManagedObjectComWrappers(EnumWrappersCallback* callback, void* cxt)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(callback != NULL);

        if (m_managedObjectComWrapperMap == NULL)
            return;

        CrstHolder lock(&m_managedObjectComWrapperLock);

        ManagedObjectComWrapperByIdMap::Iterator iter = m_managedObjectComWrapperMap->Begin();
        while (iter != m_managedObjectComWrapperMap->End())
        {
            if (!callback(iter->Value(), cxt))
                break;
            ++iter;
        }
    }
#endif // !DACCESS_COMPILE

    bool TryGetExternalComObjectContext(_Out_ void** eoc)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        *eoc = m_externalComObjectContext;
        return (*eoc != NULL);
    }

#ifndef DACCESS_COMPILE
    bool TrySetExternalComObjectContext(_In_ void* eoc, _In_ void* curr = NULL)
    {
        LIMITED_METHOD_CONTRACT;

        return (InterlockedCompareExchangeT(
                        &m_externalComObjectContext,
                        eoc,
                        curr) == curr);
    }
#endif // !DACCESS_COMPILE

private:
    // See InteropLib API for usage.
    void* m_externalComObjectContext;

    CrstExplicitInit m_managedObjectComWrapperLock;
    NewHolder<ManagedObjectComWrapperByIdMap> m_managedObjectComWrapperMap;
#endif // FEATURE_COMWRAPPERS

#ifdef FEATURE_OBJCMARSHAL
public:
#ifndef DACCESS_COMPILE
    PTR_VOID AllocTaggedMemory(_Out_ size_t* memoryInSizeT)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(memoryInSizeT != NULL);

        *memoryInSizeT = GetTaggedMemorySizeInBytes() / sizeof(SIZE_T);

        // The allocation is meant to indicate that memory
        // has been made available by the system. Calling the 'get'
        // without allocating memory indicates there has been
        // no request for reference tracking tagged memory.
        m_taggedMemory = m_taggedAlloc;
        return m_taggedMemory;
    }
#endif // !DACCESS_COMPILE

    PTR_VOID GetTaggedMemory()
    {
        LIMITED_METHOD_CONTRACT;
        return m_taggedMemory;
    }

    size_t GetTaggedMemorySizeInBytes()
    {
        LIMITED_METHOD_CONTRACT;
        return ARRAY_SIZE(m_taggedAlloc);
    }

private:
    PTR_VOID m_taggedMemory;

    // Two pointers worth of bytes of the requirement for
    // the current consuming implementation so that is what
    // is being allocated.
    // If the size of this array is changed, the NativeAOT version
    // should be updated as well.
    // See the TAGGED_MEMORY_SIZE_IN_POINTERS constant in
    // ObjectiveCMarshal.NativeAot.cs
    BYTE m_taggedAlloc[2 * sizeof(void*)];
#endif // FEATURE_OBJCMARSHAL
};

typedef DPTR(InteropSyncBlockInfo) PTR_InteropSyncBlockInfo;

// this is a lazily created additional block for an object which contains
// synchronzation information and other "kitchen sink" data
typedef DPTR(SyncBlock) PTR_SyncBlock;
// See code:#SyncBlockOverview for more
class SyncBlock
{
    // ObjHeader creates our Mutex and Event
    friend class ObjHeader;
    friend class SyncBlockCache;
    friend struct ThreadQueue;
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif
    friend class CheckAsmOffsets;

  protected:
    AwareLock  m_Monitor;                    // the actual monitor

  public:
    // If this object is exposed to unmanaged code, we keep some extra info here.
    PTR_InteropSyncBlockInfo    m_pInteropInfo;

  protected:
#ifdef EnC_SUPPORTED
    // And if the object has new fields added via EnC, this is a list of them
    PTR_EnCSyncBlockInfo m_pEnCInfo;
#endif // EnC_SUPPORTED

    // We thread two different lists through this link.  When the SyncBlock is
    // active, we create a list of waiting threads here.  When the SyncBlock is
    // released (we recycle them), the SyncBlockCache maintains a free list of
    // SyncBlocks here.
    //
    // We can't afford to use an SList<> here because we only want to burn
    // space for the minimum, which is the pointer within an SLink.
    SLink       m_Link;

    // This is the hash code for the object. It can either have been transferred
    // from the header dword, in which case it will be limited to 26 bits, or
    // have been generated right into this member variable here, when it will
    // be a full 32 bits.

    // A 0 in this variable means no hash code has been set yet - this saves having
    // another flag to express this state, and it enables us to use a 32-bit interlocked
    // operation to set the hash code, on the other hand it means that hash codes
    // can never be 0. ObjectNative::GetHashCode in objectnative.cpp makes sure to enforce this.
    DWORD m_dwHashCode;

    // In some early version of VB when there were no arrays developers used to use BSTR as arrays
    // The way this was done was by adding a trail byte at the end of the BSTR
    // To support this scenario, we need to use the sync block for this special case and
    // save the trail character in here.
    // This stores the trail character when a BSTR is used as an array
    WCHAR m_BSTRTrailByte;

  public:
    SyncBlock(DWORD indx)
        : m_Monitor(indx)
#ifdef EnC_SUPPORTED
        , m_pEnCInfo(PTR_NULL)
#endif // EnC_SUPPORTED
        , m_dwHashCode(0)
        , m_BSTRTrailByte(0)
    {
        LIMITED_METHOD_CONTRACT;

        m_pInteropInfo = NULL;

        // The monitor must be 32-bit aligned for atomicity to be guaranteed.
        _ASSERTE((((size_t) &m_Monitor) & 3) == 0);
    }

    DWORD GetSyncBlockIndex()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Monitor.GetSyncBlockIndex();
    }

   // As soon as a syncblock acquires some state that cannot be recreated, we latch
   // a bit.
   void SetPrecious()
   {
       WRAPPER_NO_CONTRACT;
       m_Monitor.SetPrecious();
   }

   BOOL IsPrecious()
   {
       LIMITED_METHOD_CONTRACT;
       return (m_Monitor.m_dwSyncIndex & SyncBlockPrecious) != 0;
   }

    // True is the syncblock and its index are disposable.
    // If new members are added to the syncblock, this
    // method needs to be modified accordingly
    BOOL IsIDisposable()
    {
        WRAPPER_NO_CONTRACT;
        return (!IsPrecious() &&
                m_Monitor.IsUnlockedWithNoWaiters() &&
                m_Monitor.m_TransientPrecious == 0);
    }

    // Gets the InteropInfo block, creates a new one if none is present.
    InteropSyncBlockInfo* GetInteropInfo()
    {
        CONTRACT (InteropSyncBlockInfo*)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        if (!m_pInteropInfo)
        {
            NewHolder<InteropSyncBlockInfo> pInteropInfo;
#ifndef TARGET_UNIX
            pInteropInfo = (InteropSyncBlockInfo *)InterlockedPopEntrySList(&InteropSyncBlockInfo::s_InteropInfoStandbyList);

            if (pInteropInfo != NULL)
            {
                // cache hit - reinitialize the data structure
                new (pInteropInfo) InteropSyncBlockInfo();
            }
            else
#endif // !TARGET_UNIX
            {
                pInteropInfo = new InteropSyncBlockInfo();
            }

            if (SetInteropInfo(pInteropInfo))
                pInteropInfo.SuppressRelease();
        }

        RETURN m_pInteropInfo;
    }

    PTR_InteropSyncBlockInfo GetInteropInfoNoCreate()
    {
        CONTRACT (PTR_InteropSyncBlockInfo)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pInteropInfo;
    }

    // Returns false if the InteropInfo block was already set - does not overwrite the previous value.
    // True if the InteropInfo block was successfully set with the passed in value.
    bool SetInteropInfo(InteropSyncBlockInfo* pInteropInfo);

#ifdef EnC_SUPPORTED
    // Get information about fields added to this object by the Debugger's Edit and Continue support
    PTR_EnCSyncBlockInfo GetEnCInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pEnCInfo;
    }

    // Store information about fields added to this object by the Debugger's Edit and Continue support
    void SetEnCInfo(EnCSyncBlockInfo *pEnCInfo);
#endif // EnC_SUPPORTED

    DWORD GetHashCode()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwHashCode;
    }

    DWORD SetHashCode(DWORD hashCode)
    {
        WRAPPER_NO_CONTRACT;
        DWORD result = InterlockedCompareExchange((LONG*)&m_dwHashCode, hashCode, 0);
        if (result == 0)
        {
            // the sync block now holds a hash code, which we can't afford to lose.
            SetPrecious();
            return hashCode;
        }
        else
            return result;
    }

    void *operator new (size_t sz, void* p)
    {
        LIMITED_METHOD_CONTRACT;
        return p ;
    }
    void operator delete(void *p)
    {
        LIMITED_METHOD_CONTRACT;
        // We've already destructed.  But retain the memory.
    }

    void EnterMonitor()
    {
        WRAPPER_NO_CONTRACT;
        m_Monitor.Enter();
    }

    BOOL TryEnterMonitor(INT32 timeOut = 0)
    {
        WRAPPER_NO_CONTRACT;
        return m_Monitor.TryEnter(timeOut);
    }

    // leave the monitor
    BOOL LeaveMonitor()
    {
        WRAPPER_NO_CONTRACT;
        return m_Monitor.Leave();
    }

    AwareLock* GetMonitor()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        //hold the syncblock
#ifndef DACCESS_COMPILE
        SetPrecious();
#endif

        //Note that for DAC we did not return a PTR_ type. This pointer is interior and
        //the SyncBlock has already been marshaled so that GetMonitor could be called.
        return &m_Monitor;
    }

    AwareLock* QuickGetMonitor()
    {
        LIMITED_METHOD_CONTRACT;
    // Note that the syncblock isn't marked precious, so use caution when
    // calling this method.
        return &m_Monitor;
    }

    BOOL DoesCurrentThreadOwnMonitor()
    {
        WRAPPER_NO_CONTRACT;
        return m_Monitor.OwnedByCurrentThread();
    }

    LONG LeaveMonitorCompletely()
    {
        WRAPPER_NO_CONTRACT;
        return m_Monitor.LeaveCompletely();
    }

    BOOL Wait(INT32 timeOut);
    void Pulse();
    void PulseAll();

    enum
    {
        // This bit indicates that the syncblock is valuable and can neither be discarded
        // nor re-created.
        SyncBlockPrecious   = 0x80000000,
    };

    BOOL HasCOMBstrTrailByte()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_BSTRTrailByte!=0);
    }
    WCHAR GetCOMBstrTrailByte()
    {
        return m_BSTRTrailByte;
    }
    void SetCOMBstrTrailByte(WCHAR trailByte)
    {
        WRAPPER_NO_CONTRACT;
        m_BSTRTrailByte = trailByte;
        SetPrecious();
    }

  protected:
    // <NOTE>
    // This should ONLY be called when initializing a SyncBlock (i.e. ONLY from
    // ObjHeader::GetSyncBlock()), otherwise we'll have a race condition.
    // </NOTE>
    void InitState(ULONG recursionLevel, PTR_Thread holdingThread, SIZE_T holdingOSThreadId)
    {
        WRAPPER_NO_CONTRACT;
        m_Monitor.InitializeToLockedWithNoWaiters(recursionLevel, holdingThread, holdingOSThreadId);
    }

#if defined(ENABLE_CONTRACTS_IMPL)
    // The LOCK_TAKEN/RELEASED macros need a "pointer" to the lock object to do
    // comparisons between takes & releases (and to provide debugging info to the
    // developer).  Use the AwareLock (m_Monitor)
    void * GetPtrForLockContract()
    {
        return m_Monitor.GetPtrForLockContract();
    }
#endif // defined(ENABLE_CONTRACTS_IMPL)
};

class SyncTableEntry
{
  public:
    PTR_SyncBlock    m_SyncBlock;
    VolatilePtr<Object, PTR_Object> m_Object;
    static PTR_SyncTableEntry GetSyncTableEntry();
#ifndef DACCESS_COMPILE
    static SyncTableEntry*& GetSyncTableEntryByRef();
#endif
};

#ifdef _DEBUG
extern void DumpSyncBlockCache();
#endif

// this class stores free sync blocks after they're allocated and
// unused

typedef DPTR(SyncBlockCache) PTR_SyncBlockCache;

// The SyncBlockCache is the data structure that manages SyncBlocks
// as well as SyncTableEntries (See explaintation at top of this file).
//
// There is only one process global SyncBlockCache (SyncBlockCache::s_pSyncBlockCache)
// and SyncTableEntry table (g_pSyncTable).
//
// see code:#SyncBlockOverview for more
class SyncBlockCache
{
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif

    friend class SyncBlock;


  private:
    PTR_SLink   m_pCleanupBlockList;    // list of sync blocks that need cleanup
    SLink*      m_FreeBlockList;        // list of free sync blocks
    CrstStatic  m_CacheLock;            // cache lock
    DWORD       m_FreeCount;            // count of active sync blocks
    DWORD       m_ActiveCount;          // number active
    SyncBlockArray *m_SyncBlocks;       // Array of new SyncBlocks.
    DWORD       m_FreeSyncBlock;        // Next Free Syncblock in the array

        // The next variables deal with SyncTableEntries.  Instead of having the object-header
        // point directly at SyncBlocks, the object points at a syncTableEntry, which in turn points
        // at the syncBlock.  This is done because in a common case (need a hash code for an object)
        // you just need a syncTableEntry.

    DWORD       m_FreeSyncTableIndex;   // We allocate a large array of SyncTableEntry structures.
                                        // This index points at the boundry between used, and never-been
                                        // used SyncTableEntries.
    size_t      m_FreeSyncTableList;    // index of the first free SyncTableEntry in our free list.
                                        // The entry at this index has its m_object field to the index
                                        // of the next element (shifted by 1, low bit marks not in use)
    DWORD       m_SyncTableSize;
    SyncTableEntry *m_OldSyncTables;    // Next old SyncTable

    BOOL        m_bSyncBlockCleanupInProgress;  // A flag indicating if sync block cleanup is in progress.
    DWORD*      m_EphemeralBitmap;      // card table for ephemeral scanning

    BOOL        GCWeakPtrScanElement(int elindex, HANDLESCANPROC scanProc, LPARAM lp1, LPARAM lp2, BOOL& cleanup);

    void SetCard (size_t card);
    void ClearCard (size_t card);
    BOOL CardSetP (size_t card);
    void CardTableSetBit (size_t idx);
    void Grow();


  public:
    SPTR_DECL(SyncBlockCache, s_pSyncBlockCache);
    static SyncBlockCache*& GetSyncBlockCache();

    // Note: No constructors/destructors - global instance
    void Init();
    void Destroy();

    static void Attach();
    static void Detach();
    void DoDetach();

    static void Start();
    static void Stop();

    // returns and removes next from free list
    SyncBlock* GetNextFreeSyncBlock();
    // returns and removes the next from cleanup list
    SyncBlock* GetNextCleanupSyncBlock();
    // inserts a syncblock into the cleanup list
    void    InsertCleanupSyncBlock(SyncBlock* psb);

    // Obtain a new syncblock slot in the SyncBlock table. Used as a hash code
    DWORD   NewSyncBlockSlot(Object *obj);

    // return sync block to cache or delete
    void    DeleteSyncBlock(SyncBlock *sb);

    // returns the sync block memory to the free pool but does not destruct sync block (must own cache lock already)
    void    DeleteSyncBlockMemory(SyncBlock *sb);

    // return sync block to cache or delete, called from GC
    void    GCDeleteSyncBlock(SyncBlock *sb);

    void    GCWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2);

    void    GCDone(BOOL demoting, int max_gen);

    void    CleanupSyncBlocks();

    int GetTableEntryCount()
    {
        LIMITED_METHOD_CONTRACT;
        return m_FreeSyncTableIndex - 1;
    }

    // Determines if a sync block cleanup is in progress.
    BOOL    IsSyncBlockCleanupInProgress()
    {
        LIMITED_METHOD_CONTRACT;
        return m_bSyncBlockCleanupInProgress;
    }

    DWORD GetActiveCount()
    {
        return m_ActiveCount;
    }

    // Encapsulate a CrstHolder, so that clients of our lock don't have to know
    // the details of our implementation.
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(SyncBlockCache *pCache)
            : CrstHolder(&pCache->m_CacheLock)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                CAN_TAKE_LOCK;
            }
            CONTRACTL_END;
        }
    };
    friend class LockHolder;

#ifdef _DEBUG
    friend void DumpSyncBlockCache();
#endif

#ifdef VERIFY_HEAP
    void    VerifySyncTableEntry();
#endif
};

// See code:#SyncBlockOverView for more
class ObjHeader
{
    friend class CheckAsmOffsets;

  private:
    // !!! Notice: m_SyncBlockValue *MUST* be the last field in ObjHeader.
#ifdef HOST_64BIT
    DWORD    m_alignpad;
#endif // HOST_64BIT

    Volatile<DWORD> m_SyncBlockValue;      // the Index and the Bits

#if defined(HOST_64BIT) && defined(_DEBUG)
    void IllegalAlignPad();
#endif // HOST_64BIT && _DEBUG

    INCONTRACT(void * GetPtrForLockContract());

  public:

    // Access to the Sync Block Index, by masking the Value.
    FORCEINLINE DWORD GetHeaderSyncBlockIndex()
    {
        LIMITED_METHOD_DAC_CONTRACT;
#if defined(HOST_64BIT) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
        // On WIN64 this field is never modified, but was initialized to 0
        if (m_alignpad != 0)
            IllegalAlignPad();
#endif // HOST_64BIT && _DEBUG && !DACCESS_COMPILE

        // pull the value out before checking it to avoid race condition
        DWORD value = m_SyncBlockValue.LoadWithoutBarrier();
        if ((value & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE)) != BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
            return 0;
        return value & MASK_SYNCBLOCKINDEX;
    }
    // Ditto for setting the index, which is careful not to disturb the underlying
    // bit field -- even in the presence of threaded access.
    //
    // This service can only be used to transition from a 0 index to a non-0 index.
    void SetIndex(DWORD indx)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
            MODE_ANY;
            PRECONDITION(GetHeaderSyncBlockIndex() == 0);
            PRECONDITION(m_SyncBlockValue & BIT_SBLK_SPIN_LOCK);
        }
        CONTRACTL_END

        LONG newValue;
        LONG oldValue;
        while (TRUE) {
            oldValue = m_SyncBlockValue.LoadWithoutBarrier();
            _ASSERTE(GetHeaderSyncBlockIndex() == 0);
            // or in the old value except any index that is there -
            // note that indx could be carrying the BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX bit that we need to preserve
            newValue = (indx |
                (oldValue & ~(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE | MASK_SYNCBLOCKINDEX)));
            if (InterlockedCompareExchange((LONG*)&m_SyncBlockValue,
                                             newValue,
                                             oldValue)
                == oldValue)
            {
                return;
            }
        }
    }

    // Used only during shutdown
    void ResetIndex()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_SyncBlockValue & BIT_SBLK_SPIN_LOCK);
        InterlockedAnd((LONG*)&m_SyncBlockValue, ~(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE | MASK_SYNCBLOCKINDEX));
    }

    // Used only GC
    void GCResetIndex()
    {
        LIMITED_METHOD_CONTRACT;

        m_SyncBlockValue.RawValue() &=~(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE | MASK_SYNCBLOCKINDEX);
    }

    // For now, use interlocked operations to twiddle bits in the bitfield portion.
    // If we ever have high-performance requirements where we can guarantee that no
    // other threads are accessing the ObjHeader, this can be reconsidered for those
    // particular bits.
    void SetBit(DWORD bit)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE((bit & MASK_SYNCBLOCKINDEX) == 0);
        InterlockedOr((LONG*)&m_SyncBlockValue, bit);
    }
    void ClrBit(DWORD bit)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE((bit & MASK_SYNCBLOCKINDEX) == 0);
        InterlockedAnd((LONG*)&m_SyncBlockValue, ~bit);
    }
    //GC accesses this bit when all threads are stopped.
    void SetGCBit()
    {
        LIMITED_METHOD_CONTRACT;

        m_SyncBlockValue.RawValue() |= BIT_SBLK_GC_RESERVE;
    }
    void ClrGCBit()
    {
        LIMITED_METHOD_CONTRACT;

        m_SyncBlockValue.RawValue() &= ~BIT_SBLK_GC_RESERVE;
    }

    // Don't bother masking out the index since anyone who wants bits will presumably
    // restrict the bits they consider.
    DWORD GetBits()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

#if defined(HOST_64BIT) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
        // On WIN64 this field is never modified, but was initialized to 0
        if (m_alignpad != 0)
            IllegalAlignPad();
#endif // HOST_64BIT && _DEBUG && !DACCESS_COMPILE

        return m_SyncBlockValue.LoadWithoutBarrier();
    }


    DWORD SetBits(DWORD newBits, DWORD oldBits)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE((oldBits & BIT_SBLK_SPIN_LOCK) == 0);
        DWORD result = InterlockedCompareExchange((LONG*)&m_SyncBlockValue, newBits, oldBits);
        return result;
    }

#ifdef _DEBUG
    BOOL HasEmptySyncBlockInfo()
    {
        WRAPPER_NO_CONTRACT;
        return m_SyncBlockValue.LoadWithoutBarrier() == 0;
    }
#endif

    // TRUE if the header has a real SyncBlockIndex (i.e. it has an entry in the
    // SyncTable, though it doesn't necessarily have an entry in the SyncBlockCache)
    BOOL HasSyncBlockIndex()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (GetHeaderSyncBlockIndex() != 0);
    }

    // retrieve or allocate a sync block for this object
    SyncBlock *GetSyncBlock();

    // retrieve sync block but don't allocate
    PTR_SyncBlock PassiveGetSyncBlock()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return g_pSyncTable [(int)GetHeaderSyncBlockIndex()].m_SyncBlock;
    }

    DWORD GetSyncBlockIndex();

    // this enters the monitor of an object
    void EnterObjMonitor();

    // non-blocking version of above
    BOOL TryEnterObjMonitor(INT32 timeOut = 0);

    // Inlineable fast path of EnterObjMonitor/TryEnterObjMonitor. Must be called before EnterObjMonitorHelperSpin.
    AwareLock::EnterHelperResult EnterObjMonitorHelper(Thread* pCurThread);

    // Typically non-inlined spin loop for some fast paths of EnterObjMonitor/TryEnterObjMonitor. EnterObjMonitorHelper must be
    // called before this function.
    AwareLock::EnterHelperResult EnterObjMonitorHelperSpin(Thread* pCurThread);

    // leaves the monitor of an object
    BOOL LeaveObjMonitor();

    // should be called only from unwind code
    BOOL LeaveObjMonitorAtException();

    // Helper encapsulating the core logic for releasing monitor. Returns what kind of
    // follow up action is necessary
    AwareLock::LeaveHelperAction LeaveObjMonitorHelper(Thread* pCurThread);

    // Returns TRUE if the lock is owned and FALSE otherwise
    // threadId is set to the ID (Thread::GetThreadId()) of the thread which owns the lock
    // acquisitionCount is set to the number of times the lock needs to be released before
    // it is unowned
    BOOL GetThreadOwningMonitorLock(DWORD *pThreadId, DWORD *pAcquisitionCount);

    PTR_Object GetBaseObject()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return dac_cast<PTR_Object>(dac_cast<TADDR>(this + 1));
    }

    BOOL Wait(INT32 timeOut);
    void Pulse();
    void PulseAll();

    void EnterSpinLock();
    void ReleaseSpinLock();

    BOOL Validate (BOOL bVerifySyncBlkIndex = TRUE);
};


typedef DPTR(class ObjHeader) PTR_ObjHeader;


#define ENTER_SPIN_LOCK(pOh)        \
    pOh->EnterSpinLock();

#define LEAVE_SPIN_LOCK(pOh)        \
    pOh->ReleaseSpinLock();


#ifdef DACCESS_COMPILE
// A visitor function used to enumerate threads in the ThreadQueue below
typedef void (*FP_TQ_THREAD_ENUMERATION_CALLBACK)(PTR_Thread pThread, VOID* pUserData);
#endif

// A SyncBlock contains an m_Link field that is used for two purposes.  One
// is to manage a FIFO queue of threads that are waiting on this synchronization
// object.  The other is to thread free SyncBlocks into a list for recycling.
// We don't want to burn anything else on the SyncBlock instance, so we can't
// use an SList or similar data structure.  So here's the encapsulation for the
// queue of waiting threads.
//
// Note that Enqueue is slower than it needs to be, because we don't want to
// burn extra space in the SyncBlock to remember the head and the tail of the Q.
// An alternate approach would be to treat the list as a LIFO stack, which is not
// a fair policy because it permits to starvation.
//
// Important!!! While there is a lock that is used in process to keep multiple threads
// from altering the queue simultaneously, the queue must still be consistent at all
// times, even when the lock is held. The debugger inspects the queue from out of process
// and just looks at the memory...it must be valid even if the lock is held. Be careful if you
// change the way the queue is updated.
struct ThreadQueue
{
    // Given a link in the chain, get the Thread that it represents
    static PTR_WaitEventLink WaitEventLinkForLink(PTR_SLink pLink);

    // Unlink the head of the Q.  We are always in the SyncBlock's critical
    // section.
    static WaitEventLink *DequeueThread(SyncBlock *psb);

    // Enqueue is the slow one.  We have to find the end of the Q since we don't
    // want to burn storage for this in the SyncBlock.
    static void          EnqueueThread(WaitEventLink *pWaitEventLink, SyncBlock *psb);

    // Wade through the SyncBlock's list of waiting threads and remove the
    // specified thread.
    static BOOL          RemoveThread (Thread *pThread, SyncBlock *psb);

#ifdef DACCESS_COMPILE
    // Enumerates the threads in the queue from front to back by calling
    // pCallbackFunction on each one
    static void          EnumerateThreads(SyncBlock *psb,
                                          FP_TQ_THREAD_ENUMERATION_CALLBACK pCallbackFunction,
                                          void* pUserData);
#endif
};

inline void AwareLock::SetPrecious()
{
    LIMITED_METHOD_CONTRACT;

    m_dwSyncIndex |= SyncBlock::SyncBlockPrecious;
}

inline DWORD AwareLock::GetSyncBlockIndex()
{
    LIMITED_METHOD_CONTRACT;
    return (m_dwSyncIndex & ~SyncBlock::SyncBlockPrecious);
}

#ifdef TARGET_X86
#include <poppack.h>
#endif // TARGET_X86

#endif // _SYNCBLK_H_



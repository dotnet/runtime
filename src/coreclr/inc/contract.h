// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// Contract.h
//

// ! I am the owner for issues in the contract *infrastructure*, not for every
// ! CONTRACT_VIOLATION dialog that comes up. If you interrupt my work for a routine
// ! CONTRACT_VIOLATION, you will become the new owner of this file.
//--------------------------------------------------------------------------------
// CONTRACTS - User Reference
//
//   A CONTRACT is a container for a set of checked declarations about a
// function.  Besides giving developers a "laundry list" of checks to
// make checking more complete, contracts compile these checks
// as hidden annotations into the checked executable that our static scanner
// uses to detect violations automatically.
//
//   Contracts can be dynamic or static. Dynamic contracts perform runtime checks
// as well as being visible to the static scanner. Static contracts generate no
// runtime code but are still visible to the scanner. Dynamic contracts are
// preferred unless perf or other considerations preclude them.
//
//   The following annotations can appear in contracts:
//
//
//      THROWS          an exception might be thrown out of the function
//      -or- NOTHROW    an exception will NOT be thrown out of the function
//
//
//
//      INJECT_FAULT(statement)   function might require its caller to handle an OOM
//      -or- FAULT_FORBID         function will NOT require its caller to handle an OOM
//
//
//
//      GC_TRIGGERS             the function can trigger a GC
//      -or- GC_NOTRIGGER       the function will never trigger a GC provided its
//                              called in coop mode.
//
//
//      MODE_COOPERATIVE        the function requires Cooperative GC mode on entry
//      -or- MODE_PREEMPTIVE    the function requires Preemptive GC mode on entry
//      -or- MODE_ANY           the function can be entered in either mode
//
//      LOADS_TYPE(level)       the function promises not to load any types beyond "level"
//
//      CAN_TAKE_LOCK           the function has a code path that takes a lock
//      _or_ (CAN_TAKE_LOCK and CANNOT_RETAKE_LOCK)
//                              the function has a code path that takes a lock, but never tries to reenter
//                              locks held at the time this function was called.
//      -or- CANNOT_TAKE_LOCK   the function will never allow a lock to be taken
//      -or-                    the default is WRAPPER(CAN_TAKE_LOCK).  i.e., if any callees take locks,
//                              then it's ok for this function to as well.  If LIMITED_METHOD_CONTRACT is specified,
//                              however, then CANNOT_TAKE_LOCK is assumed.
//
//
//      SUPPORTS_DAC            The function has been written to be callable from out-of-process using DAC.
//                              In builds where DACCESS_COMPILE is defined, such functions can only call
//                              other such functions (and a few primitives like new).  Functions that support
//                              DAC must be carefully written to conform to the rules in daccess.h.
//
//      SUPPORTS_DAC_HOST_ONLY  The function and its call graph has been written to be callable from out of process
//                              using DAC, but it differs from SUPPORTS_DAC in that these functions won't perform
//                              any marshalling. Because it does no marshalling, SUPPORTS_DAC_HOST_ONLY functions
//                              and their call graph won't be checked by DacCop. This should only be used by utility
//                              functions which will never marshal anything.
//
//      PRECONDITION(X) -   generic CHECK or BOOL expression which should be true
//                          on function entry
//
//      POSTCONDITION(X) -  generic CHECK or BOOL expression which should be true
//                          on function entry.  Note that variable RETVAL will be
//                          available for use in the expression.
//
//
//      INSTANCE_CHECK -    equivalent of:
//                          PRECONDITION(CheckPointer(this));
//                          POSTCONDITION(CheckInvariant(this));
//      INSTANCE_CHECK_NULL - equivalent of:
//                          PRECONDITION(CheckPointer(this, NULL_OK));
//                          POSTCONDITION(CheckInvariant(this, NULL_OK));
//      CONSTRUCTOR_CHECK - equivalent of:
//                          POSTCONDITION(CheckPointer(this));
//      DESTRUCTOR_CHECK -  equivalent of:
//                          PRECONDITION(CheckPointer(this));
//
//
//
//
//   Contracts come in the following flavors:
//
//     Dynamic:
//        CONTRACTL          the standard version used for all dynamic contracts
//                           except those including postconditions.
//
//        CONTRACT(rettype)  an uglier version of CONTRACTL that's unfortunately
//                           needed to support postconditions. You must specify
//                           the correct return type and it cannot be "void."
//                           (Use CONTRACT_VOID instead) You must use the
//                           RETURN macro rather than the "return" keyword.
//
//        CONTRACT_VOID      you can't supply "void" to a CONTRACT - use this
//                           instead.
//
//     Static:
//        LIMITED_METHOD_CONTRACT
//                           A static contract equivalent to NOTHROW/GC_NOTRIGGER/FORBID_FAULT/MODE_ANY.
//                           Use only for trivial functions that call only functions with LIMITED_METHOD_CONTRACTs
//                           (as long as there is no cycle that may introduce infinite recursion).
//
//        STATIC_CONTRACT_THROWS
//        STATIC_CONTRACT_NOTHROW
//        STATIC_CONTRACT_GC_TRIGGERS
//        STATIC_CONTRACT_GCNOTRIGGER
//        STATIC_CONTRACT_FAULT
//        STATIC_CONTRACT_FORBID_FAULT
//                           use to implement statically checkable contracts
//                           when runtime contracts cannot be used.
//
//
//   WRAPPER(annotation)
//
// When a function does not explicitly caused a condition, use the WRAPPER macro around
// the declaration.  This implies that the function is dependent on the functions it calls
// for its behaviour, and guarantees nothing.
//
//
//   CONTRACT_VIOLATION(violationmask):
//
//        A bandaid used to suppress contract assertions. A contract violation
//        is always a bug and you're expected to remove it before shipping.
//        If a violation cannot be fixed immediately, however, it's better
//        to use this on the offending callsite than to disable a contract entirely.
//
//        The violationmask can be one or more of the following OR'd together.
//
//              ThrowsViolation
//              GCViolation
//              ModeViolation
//              FaultViolation
//              FaultNotFatal
//              HostViolation
//              LoadsTypeViolation
//              TakesLockViolation
//
//        The associated assertion will be suppressed until you leave the scope
//        containing the CONTRACT_VIOLATION. Note, however, that any called
//        function that redeclares the associated annotation reinstates
//        the assert for the scope of *its* call. This prevents a CONTRACT_VIOLATION
//        placed at the root of a calltree from decimating our entire protection.
//
//
//   PERMANENT_CONTRACT_VIOLATION(violationmask, permanentContractViolationReason):
//
//        Like a CONTRACT_VIOLATION but also indicates that the violation was a deliberate decision
//        and we don't plan on removing the violation in the next release.  The reason
//        for the violation should be given as the second parameter to the macro.  Reasons
//        are currently for documentation purposes only and do not have an effect on the binary.
//        Valid values are listed below in the definition of PermanentContractViolationReason.
//
//
//    CONDITIONAL_CONTRACT_VIOLATION(violationmask, condition):
//
//        Similar to CONTRACT_VIOLATION, but only suppresses the contract if the
//        condition evaluates to non-zero.  The need for this macro should be very
//        rare, but it can be useful if a contract should be suppressed based on a
//        condition known only at run-time.  For example, if a particular test causes
//        call sequences never expected by real scenarios, you may want to suppress
//        resulting violations, but only when that test is run.
//
//   WRAPPER_NO_CONTRACT
//
//        A do-nothing contract used by functions that trivially wrap another.
//
//
// "LEGACY" stuff - these features have been mostly superceded by better solutions
//     so their use should be discouraged.
//
//
//   DISABLED(annotation)
//
//        Indicates that a condition is supposed to be checked but is being suppressed
//        due to some temporary bug. The more surgical CONTRACT_VIOLATION is
//        preferred over DISABLED.
//
//   UNCHECKED(annotation)
//
//        Indicates that a condition is supposed to be checked but is being suppressed
//        due for perf reasons. Use STATIC_CONTRACT over this.
//
//
//   Default values:
//        If you don't specify certain annotaions, you get defaults.
//           - THROWS/NOTHROW          defaults to THROWS
//           - GCTRIGGERS/GCNOTRIGGER  defaults to GCTRIGGERS within the VM directory
//                                     and to no check otherwise
//           - INJECT/FORBID_FAULT     defaults to no check
//           - MODE                    defaults to MODE_ANY
//
//        The problem is that defaults don't work well with static contracts.
//        The scanner will always treat a missing annotation as DISABLED.
//        New code should not rely on defaults. Explicitly state your invariants.
//
//
//--------------------------------------------------------------------------------




#ifndef CONTRACT_H_
#define CONTRACT_H_

#ifdef _MSC_VER
#pragma warning(disable:4189) //local variable is initialized but not referenced
#endif


// We only enable contracts in _DEBUG builds
#if defined(_DEBUG) && !defined(DISABLE_CONTRACTS) && !defined(JIT_BUILD)
#define ENABLE_CONTRACTS_DATA
#endif

// Also, we won't enable contracts if this is a DAC build.
#if defined(ENABLE_CONTRACTS_DATA) && !defined(DACCESS_COMPILE) && !defined(CROSS_COMPILE)
#define ENABLE_CONTRACTS
#endif

// Finally, only define the implementaiton parts of contracts if this isn't a DAC build.
#if defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS)
#define ENABLE_CONTRACTS_IMPL
#endif

#include "specstrings.h"
#include "clrtypes.h"
#include "malloc.h"
#include "check.h"
#include "debugreturn.h"
#include "staticcontract.h"

#ifdef ENABLE_CONTRACTS_DATA

#include "eh.h"

// We chain these onto a stack to give us a stack trace of contract assertions (useful
// when the bug report doesn't contain valid symbols)

struct ContractStackRecord
{
    ContractStackRecord *m_pNext;
    const char          *m_szFunction;
    const char          *m_szFile;
    int                  m_lineNum;
    UINT                 m_testmask;  // Bitmask of Contract::TestEnum bitsf
    const char          *m_construct; // The syntactic construct that pushed this thing
};

class CrstBase;

// The next few enums / structs are used to keep track of all kinds of locks
// currently taken by the current thread (crsts, spinlocks, CLR critical sections).
// Across the VM, there are still multiple counts of locks.  The lock counts in these
// contract structs are used to verify consistency of lock take/release in EE code, and
// for contracts.  Both user and EE locks are tracked here, but it's EE code consistency
// we're verifying.  The Thread object keeps its own counts as well, primarily of user
// locks for implementing thread abort & escalation policy.  We tried to have the Thread
// counts also be used for consistency checking, but that doesn't work.  Thread counters
// have the following behavior that hurts our internal consistency checks:
//      - They only count user locks.
//      - Counters are reset & restored as we leave and return to AppDomains

// An array of these is stored in DbgStateLockData::m_rgTakenLockInfos
// to remember which locks we've taken.  If you hit an assert that
// indicates we're exiting locks in the wrong order, or that locks were
// taken when we expected none to be taken, then you can use
// DbgStateLockData::m_rgTakenLockInfos to see the locks we know about.
struct TakenLockInfo
{
    // Generally, this will be a pointer to the lock, but really it's just
    // a value that identifies which lock is taken.  Ya see, sometimes we don't
    // have a lock pointer handy (e.g., if the lock is based on a GC object,
    // which has no persistent object pointer we can use).  Look at the source
    // indicated by m_szFile / m_lineNum to see what was specified as m_pvLock.
    //
    // A common case is that the lock is just a Crst, so to aid debugging, we
    // also include a statically typed version of this pointer (m_pCrstBase) just
    // for Crsts. Again, you'll want look at m_szFile / m_lineNum to see how to
    // interpret this union.
    union
    {
        void *           m_pvLock;
        CrstBase *       m_pCrstBase;
    };

    // File & line of the *LOCK_TAKEN* macro that added this lock to our list
    const char *         m_szFile;
    int                  m_lineNum;
};

enum DbgStateLockType
{
    // EE locks (used to sync EE structures).  These do not include
    // CRST_HOST_BREAKABLE Crsts, and are thus not held while managed
    // code runs
    kDbgStateLockType_EE,

    // CRST_HOST_BREAKABLE Crsts.  These can be held while arbitrary
    // managed code runs.
    kDbgStateLockType_HostBreakableCrst,

    // User locks (e.g., Monitor.Enter, ReaderWriterLock class)
    kDbgStateLockType_User,

    // add more lock types here

    kDbgStateLockType_Count
};

// This keeps track of how many locks, and which locks, are currently owned
// by the current thread.  There is one instance of this structure per
// thread (no EE Thread object required).  This is in contrast to the
// ClrDebugState structure, which is instantiated once per function
// on the stack.  Reason is that ClrDebugState resets its state on exit
// of function (Contract destructor reinstates previous ClrDebugState), whereas
// we want DbgStateLockData to persist across function enters & exits.
struct DbgStateLockData
{
    // When a lock is taken, we keep track of its pointer and file/line# when it
    // was added in a static-size array DbgStateLockData::m_rgTakenLockInfos.  This is
    // the size of that array, and therefore indicates the maximum number of locks we
    // expect one thread to hold at the same time.  If we should exceed this limit,
    // we'll lose this data for the latter locks that exceed this limit
    // (though still maintaining an accurate *count* of locks).
    static const int     kMaxAllowedSimultaneousLocks = 20;

    // Count of locks taken, separately by type
    UINT                 m_rgcLocksTaken[kDbgStateLockType_Count];

    // List of the specific locks that have been taken (all DbgStateLockTypes
    // intermingled), in the order they were taken.  If we exceed the elements
    // in the array, we just won't track the latter locks in here (though they are
    // included in the counts above)
    TakenLockInfo        m_rgTakenLockInfos[kMaxAllowedSimultaneousLocks];

    void SetStartingValues();
    void LockTaken(DbgStateLockType dbgStateLockType,
                     UINT cEntrances,
                     void * pvLock,
                     _In_z_ const char * szFunction,
                     _In_z_ const char * szFile,
                     int lineNum);
    void LockReleased(DbgStateLockType dbgStateLockType, UINT cExits, void * pvLock);
    UINT GetLockCount(DbgStateLockType dbgStateLockType);
    UINT GetCombinedLockCount();
};

// This struct contains all lock contract information.  It is created and destroyed along with
// ClrDebugState. m_pLockData points to a DbgStateLockData object that is allocated per thread
// and persists across function enters and exists.
struct DbgStateLockState
{
private:
    // Count of locks taken at the time the function with CANNOT_RETAKE_LOCK contract
    // was called
    UINT               m_cLocksEnteringCannotRetakeLock;

    DbgStateLockData * m_pLockData;  // How many and which locks are currently taken on this thread

public:
    void SetStartingValues();
    void OnEnterCannotRetakeLockFunction();
    BOOL IsLockRetaken(void * pvLock);
    BOOL IsSafeToRelease(UINT cReleases);
    void SetDbgStateLockData(DbgStateLockData * pDbgStateLockData);
    DbgStateLockData * GetDbgStateLockData();
};


#define CONTRACT_BITMASK_OK_TO_THROW          0x1 << 0
#define CONTRACT_BITMASK_FAULT_FORBID         0x1 << 1
#define CONTRACT_BITMASK_HOSTCALLS            0x1 << 2
#define CONTRACT_BITMASK_SOTOLERANT           0x1 << 3
#define CONTRACT_BITMASK_DEBUGONLY            0x1 << 4
#define CONTRACT_BITMASK_SONOTMAINLINE        0x1 << 5
#define CONTRACT_BITMASK_OK_TO_LOCK           0x1 << 6
#define CONTRACT_BITMASK_OK_TO_RETAKE_LOCK    0x1 << 7


#define CONTRACT_BITMASK_IS_SET(whichbit)    ((m_flags & (whichbit)) != 0)
#define CONTRACT_BITMASK_SET(whichbit)       (m_flags |= (whichbit))
#define CONTRACT_BITMASK_RESET(whichbit)     (m_flags &= ~(whichbit))
#define CONTRACT_BITMASK_UPDATE(whichbit, value)  ((value)?CONTRACT_BITMASK_SET(whichbit):CONTRACT_BITMASK_RESET(whichbit))

struct ClrDebugState
{
private:
    UINT_PTR              m_flags;
    UINT_PTR             m_violationmask;      // Current CONTRACT_VIOLATIONS in effect
    ContractStackRecord *m_pContractStackTrace;
    UINT                 m_GCNoTriggerCount;
    UINT                 m_GCForbidCount;
    UINT                  m_maxLoadTypeLevel;   // taken from enum ClassLoadLevel
    BOOL                  m_allowGetThread;     // TRUE if GetThread() is ok in this scope
    DbgStateLockState     m_LockState;

public:
    // Use an explicit Init rather than ctor as we don't want automatic
    // construction of the ClrDebugState embedded inside the contract.
    void SetStartingValues()
    {
        m_violationmask     = 0;            // No violations allowed

        // Default is we're in a THROWS scope. This is not ideal, but there are
                                            //  just too many places that I'd have to go clean up right now
                                            //  (hundreds) in order to make this FALSE by default.
        // Faults not forbidden (an unfortunate default but
                                            //  we'd never get this debug infrastructure bootstrapped otherwise.)
        // We start out in SO-tolerant mode and must probe before entering SO-intolerant
        //   any global state updates.
        // Initial mode is non-debug until we say otherwise
        // Everthing defaults to mainline
        // By default, GetThread() is perfectly fine to call
        // By default, it's ok to take a lock (or call someone who does)
        m_flags             = CONTRACT_BITMASK_OK_TO_THROW|
                              CONTRACT_BITMASK_HOSTCALLS|
                              CONTRACT_BITMASK_SOTOLERANT|
                              CONTRACT_BITMASK_OK_TO_LOCK|
                              CONTRACT_BITMASK_OK_TO_RETAKE_LOCK;

        m_pContractStackTrace = NULL;       // At top of stack, no contracts in force
        m_GCNoTriggerCount  = 0;
        m_GCForbidCount     = 0;

        m_maxLoadTypeLevel = ((UINT)(-1));  // ideally CLASS_LOAD_LEVEL_FINAL but we don't have access to that #define, so
                                            // the max integer value will do as a substitute.

        m_allowGetThread = TRUE;            // By default, GetThread() is perfectly fine to call

        m_LockState.SetStartingValues();
    }

    void CheckOkayToThrow(_In_z_ const char *szFunction, _In_z_ const char *szFile, int lineNum); // Asserts if its not okay to throw.
    BOOL CheckOkayToThrowNoAssert(); // Returns if OK to throw

    //--//

    UINT_PTR* ViolationMaskPtr()
    {
        return &m_violationmask;
    }

    UINT_PTR ViolationMask()
    {
        return m_violationmask;
    }

    void ViolationMaskSet( UINT_PTR value )
    {
        m_violationmask |= value;
    }

    void ViolationMaskReset( UINT_PTR value )
    {
        m_violationmask &= ~value;
    }

    //--//

    BOOL IsOkToThrow()
    {
        return CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_OK_TO_THROW);
    }

    void SetOkToThrow()
    {
        CONTRACT_BITMASK_SET(CONTRACT_BITMASK_OK_TO_THROW);
    }

    BOOL SetOkToThrow( BOOL value )
    {
        BOOL prevState = CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_OK_TO_THROW);
        CONTRACT_BITMASK_UPDATE(CONTRACT_BITMASK_OK_TO_THROW, value);
        return prevState;
    }

    void ResetOkToThrow()
    {
        CONTRACT_BITMASK_RESET(CONTRACT_BITMASK_OK_TO_THROW);
    }
    //--//

    BOOL IsFaultForbid()
    {
        return CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_FAULT_FORBID);
    }


    void SetFaultForbid()
    {
        CONTRACT_BITMASK_SET(CONTRACT_BITMASK_FAULT_FORBID);
    }

    BOOL SetFaultForbid(BOOL value)
    {
        BOOL prevState = CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_FAULT_FORBID);
        CONTRACT_BITMASK_UPDATE(CONTRACT_BITMASK_FAULT_FORBID, value);
        return prevState;
    }

    void ResetFaultForbid()
    {
        CONTRACT_BITMASK_RESET(CONTRACT_BITMASK_FAULT_FORBID);
    }

    //--//
    BOOL IsHostCaller()
    {
        return CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_HOSTCALLS);
    }

    void SetHostCaller()
    {
        CONTRACT_BITMASK_SET(CONTRACT_BITMASK_HOSTCALLS);
    }


    BOOL SetHostCaller(BOOL value)
    {
        BOOL prevState = CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_HOSTCALLS);
        CONTRACT_BITMASK_UPDATE(CONTRACT_BITMASK_HOSTCALLS,value);
        return prevState;
    }

    void ResetHostCaller()
    {
        CONTRACT_BITMASK_RESET(CONTRACT_BITMASK_HOSTCALLS);
    }

    //--//
    BOOL IsDebugOnly()
    {
        STATIC_CONTRACT_WRAPPER;
        return CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_DEBUGONLY);
    }

    void SetDebugOnly()
    {
        STATIC_CONTRACT_WRAPPER;
        CONTRACT_BITMASK_SET(CONTRACT_BITMASK_DEBUGONLY);
    }

    BOOL SetDebugOnly(BOOL value)
    {
        STATIC_CONTRACT_WRAPPER;
        BOOL prevState = CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_DEBUGONLY);
        CONTRACT_BITMASK_UPDATE(CONTRACT_BITMASK_DEBUGONLY,value);
        return prevState;
    }

    void ResetDebugOnly()
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        CONTRACT_BITMASK_RESET(CONTRACT_BITMASK_DEBUGONLY);
    }

    //--//
    BOOL IsOkToLock()
    {
        return CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_OK_TO_LOCK);
    }

    void SetOkToLock()
    {
        CONTRACT_BITMASK_SET(CONTRACT_BITMASK_OK_TO_LOCK);
    }

    BOOL SetOkToLock( BOOL value )
    {
        BOOL prevState = CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_OK_TO_LOCK);
        CONTRACT_BITMASK_UPDATE(CONTRACT_BITMASK_OK_TO_LOCK, value);
        return prevState;
    }

    void ResetOkToLock()
    {
        CONTRACT_BITMASK_RESET(CONTRACT_BITMASK_OK_TO_LOCK);
    }

    //--//
    BOOL IsOkToRetakeLock()
    {
        return CONTRACT_BITMASK_IS_SET(CONTRACT_BITMASK_OK_TO_RETAKE_LOCK);
    }

    void ResetOkToRetakeLock()
    {
        CONTRACT_BITMASK_RESET(CONTRACT_BITMASK_OK_TO_RETAKE_LOCK);
    }


    //--//
    void LinkContractStackTrace( ContractStackRecord* pContractStackTrace )
    {
        pContractStackTrace->m_pNext = m_pContractStackTrace;

        m_pContractStackTrace = pContractStackTrace;
    }

    ContractStackRecord* GetContractStackTrace()
    {
        return m_pContractStackTrace;
    }

    void SetContractStackTrace(ContractStackRecord* pContractStackTrace )
    {
        m_pContractStackTrace = pContractStackTrace;
    }

    //--//

    UINT GetGCNoTriggerCount()
    {
        return m_GCNoTriggerCount;
    }

    void DecrementGCNoTriggerCount()
    {
        m_GCNoTriggerCount--;
    }

    void IncrementGCNoTriggerCount()
    {
        m_GCNoTriggerCount++;
    }


    UINT GetGCForbidCount()
    {
        return m_GCForbidCount;
    }

    void DecrementGCForbidCount()
    {
        m_GCForbidCount--;
    }

    void IncrementGCForbidCount()
    {
        m_GCForbidCount++;
    }

    UINT GetMaxLoadTypeLevel()
    {
        return m_maxLoadTypeLevel;
    }

    void SetMaxLoadTypeLevel(UINT newLevel)
    {
        m_maxLoadTypeLevel = newLevel;
    }

    //--//

    void SetDbgStateLockData(DbgStateLockData * pDbgStateLockData)
    {
        m_LockState.SetDbgStateLockData(pDbgStateLockData);
    }

    DbgStateLockData * GetDbgStateLockData()
    {
        return m_LockState.GetDbgStateLockData();
    }

    void OnEnterCannotRetakeLockFunction()
    {
        m_LockState.OnEnterCannotRetakeLockFunction();
    }

    void CheckOkayToLock(_In_z_ const char *szFunction, _In_z_ const char *szFile, int lineNum); // Asserts if its not okay to lock
    BOOL CheckOkayToLockNoAssert(); // Returns if OK to lock
    void LockTaken(DbgStateLockType dbgStateLockType,
                     UINT cEntrances,
                     void * pvLock,
                     _In_z_ const char * szFunction,
                     _In_z_ const char * szFile,
                     int lineNum);
    void LockReleased(DbgStateLockType dbgStateLockType, UINT cExits, void * pvLock);
    UINT GetLockCount(DbgStateLockType dbgStateLockType);
    UINT GetCombinedLockCount();
};

#endif // ENABLE_CONTRACTS

#ifdef ENABLE_CONTRACTS_IMPL
// Create ClrDebugState.
// This routine is not allowed to return NULL. If it can't allocate the memory needed,
// it should return a pointer to a global static ClrDebugState that indicates
// that debug assertions should be skipped.
ClrDebugState *CLRInitDebugState();
ClrDebugState *GetClrDebugState(BOOL fAlloc = TRUE);

extern thread_local ClrDebugState* t_pClrDebugState;

// This function returns a ClrDebugState if one has been created, but will not create one itself.
inline ClrDebugState *CheckClrDebugState()
{
    STATIC_CONTRACT_LIMITED_METHOD;
    return t_pClrDebugState;
}

void CONTRACT_ASSERT(const char *szElaboration,
                     UINT  whichTest,
                     UINT  whichTestMask,
                     const char *szFunction,
                     const char *szFile,
                     int   lineNum
                     );

#endif

// This needs to be defined up here b/c it is used by ASSERT_CHECK which is used by the contract impl
#ifdef _DEBUG
#ifdef ENTER_DEBUG_ONLY_CODE
#undef ENTER_DEBUG_ONLY_CODE
#endif
#ifdef LEAVE_DEBUG_ONLY_CODE
#undef LEAVE_DEBUG_ONLY_CODE
#endif

#ifdef ENABLE_CONTRACTS_IMPL
// This can only appear in a debug function so don't define it non-debug
class DebugOnlyCodeHolder
{
public:
    // We use GetClrDebugState on entry, but CheckClrDebugState on Leave
    // That way we make sure to create one if we need to set state, but
    // we don't recreated one on exit if its been deleted.
    DEBUG_NOINLINE void Enter()
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_DEBUG_ONLY;

        m_pClrDebugState = GetClrDebugState();
        if (m_pClrDebugState)
        {
            m_oldDebugOnlyValue = m_pClrDebugState->IsDebugOnly();
            m_pClrDebugState->SetDebugOnly();
        }
    }

    DEBUG_NOINLINE void Leave()
    {
        SCAN_SCOPE_END;
        STATIC_CONTRACT_DEBUG_ONLY;

        m_pClrDebugState = CheckClrDebugState();
        if (m_pClrDebugState)
        {
            m_pClrDebugState->SetDebugOnly( m_oldDebugOnlyValue );
        }
    }

private:
BOOL           m_oldDebugOnlyValue;
ClrDebugState *m_pClrDebugState;
};

#define ENTER_DEBUG_ONLY_CODE                                                \
    DebugOnlyCodeHolder __debugOnlyCodeHolder;                               \
    __debugOnlyCodeHolder.Enter();

#define LEAVE_DEBUG_ONLY_CODE                                                \
    __debugOnlyCodeHolder.Leave();


class AutoCleanupDebugOnlyCodeHolder : public DebugOnlyCodeHolder
{
public:
    DEBUG_NOINLINE AutoCleanupDebugOnlyCodeHolder()
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_DEBUG_ONLY;

        Enter();
    };

    DEBUG_NOINLINE ~AutoCleanupDebugOnlyCodeHolder()
    {
        SCAN_SCOPE_END;

        Leave();
    };
};

#define DEBUG_ONLY_FUNCTION \
    STATIC_CONTRACT_DEBUG_ONLY;                                             \
    AutoCleanupDebugOnlyCodeHolder __debugOnlyCodeHolder;

#define DEBUG_ONLY_REGION() \
    AutoCleanupDebugOnlyCodeHolder __debugOnlyCodeHolder;


#define BEGIN_DEBUG_ONLY_CODE                                               \
    {                                                                       \
        AutoCleanupDebugOnlyCodeHolder __debugOnlyCodeHolder;

#define END_DEBUG_ONLY_CODE                                                 \
    }

#else // ENABLE_CONTRACTS_IMPL
#define DEBUG_ONLY_FUNCTION STATIC_CONTRACT_DEBUG_ONLY
#define DEBUG_ONLY_REGION()
#define BEGIN_DEBUG_ONLY_CODE
#define END_DEBUG_ONLY_CODE
#define ENTER_DEBUG_ONLY_CODE
#define LEAVE_DEBUG_ONLY_CODE
#endif

#else // _DEBUG
#define DEBUG_ONLY_REGION()
#endif


#ifdef ENABLE_CONTRACTS_IMPL

// These helpers encapsulate our access to our FLS debug state. To improve
// contract perf, we'll soon move these to a private alloced block
// so we can reuse the pointer instead of constantly refetching it.
// Thus, these helpers are just to bridge this transition.
inline LPVOID GetViolationMask()
{
    ClrDebugState *pState = CheckClrDebugState();
    if (pState)
    {
        return (LPVOID)pState->ViolationMask();
    }
    else
    {
        return 0;
    }
}

// This is the default binding of the MAYBETEMPLATE identifier,
// used in the RETURN macro
template <int DUMMY>
class ___maybetemplate
{
  public:
    FORCEINLINE void *operator new (size_t size)
    {
        return NULL;
    }
};

// This is an abstract base class for contracts. The main reason we have this is so that the dtor for many derived class can
// be performant. If this class was not abstract and had a dtor, then the dtor for the derived class adds EH overhead (even if the derived
// class did not anything special in its dtor)
class BaseContract
{
    // Really private, but used by macros
    public:

    // We use a single integer to specify all the settings for intrinsic tests
    // such as THROWS and GC_TRIGGERS. The compiler should be able to fold all
    // these clauses into a single constant.
    //
    // The value "0" is significant as this is what the entire mask will be initialized to
    // in the absence of any clauses. Hence, whichever value is assigned "0" will be the
    // default setting for the test.
    //
    // Also, there must be a "disabled" setting for each category in order to support
    // the DISABLED macro.
    enum TestEnum
    {
        THROWS_Mask         = 0x00000003,
        THROWS_Yes          = 0x00000000,   // the default
        THROWS_No           = 0x00000001,
        THROWS_Disabled     = 0x00000002,

        GC_Mask             = 0x0000000C,
        GC_Triggers         = 0x00000000,   // the default
        GC_NoTrigger        = 0x00000004,
        GC_Disabled         = 0x00000008,

        FAULT_Mask          = 0x00000030,
        FAULT_Disabled      = 0x00000000,   // the default
        FAULT_Inject        = 0x00000010,
        FAULT_Forbid        = 0x00000020,

        MODE_Mask           = 0x000000C0,
        MODE_Disabled       = 0x00000000,   // the default
        MODE_Preempt        = 0x00000040,
        MODE_Coop           = 0x00000080,

        DEBUG_ONLY_Yes          = 0x00000400,  // code runs under debug only

        SO_MAINLINE_No          = 0x00000800,  // code is not part of our mainline SO scenario

        // Any place where we can't safely call into the host should have a HOST_NoCalls contract
        HOST_Mask               = 0x00003000,
        HOST_Calls              = 0x00002000,
        HOST_NoCalls            = 0x00001000,
        HOST_Disabled           = 0x00000000,   // the default

        // These enforce the CAN_TAKE_LOCK / CANNOT_TAKE_LOCK contracts
        CAN_TAKE_LOCK_Mask      = 0x00060000,
        CAN_TAKE_LOCK_Yes       = 0x00020000,
        CAN_TAKE_LOCK_No        = 0x00040000,
        CAN_TAKE_LOCK_Disabled  = 0x00000000,   // the default

        // These enforce the CANNOT_RETAKE_LOCK contract
        CAN_RETAKE_LOCK_No           = 0x00080000,
        CAN_RETAKE_LOCK_No_Disabled  = 0x00000000,   // the default

        PRECONDITION_Used       = 0x00010000,   // a PRECONDITION appeared inside the contract

        // IMPORTANT!!! LOADS_TYPE_Mask and LOADS_TYPE_Shift must be kept in sync.
        LOADS_TYPE_Mask         = 0x00f00000,   // the max loadstype level + 1 ("+1" because 0 is reserved for the default which is "disabled")
        LOADS_TYPE_Shift        = 20,           // # of bits to right-shift to get loadstype bits to rightmost position.
        LOADS_TYPE_Disabled     = 0x00000000,   // the default

        ALL_Disabled            = THROWS_Disabled|GC_Disabled|FAULT_Disabled|MODE_Disabled|LOADS_TYPE_Disabled|
                                  HOST_Disabled|CAN_TAKE_LOCK_Disabled|CAN_RETAKE_LOCK_No_Disabled

    };

    enum Operation
    {
        Setup = 0x01,
        Preconditions = 0x02,
        Postconditions = 0x04,
    };


    NOTHROW_DECL BaseContract() : m_testmask(0), m_pClrDebugState(NULL)
    {
    }
    NOTHROW_DECL void Restore()
    {
        // m_pClrDebugState is setup in BaseContract::DoChecks. If an SO happens after the
        // BaseContract object is constructed but before DoChecks is invoked, m_pClrDebugState
        // will remain NULL (which is what it is set to in the BaseContract ctor).
        //
        // Thus, we should check for it being NULL before dereferencing it.
        if (m_pClrDebugState)
        {
            // Backout all changes to debug state.
            *m_pClrDebugState = m_IncomingClrDebugState;
        }
    }

    void DoChecks(UINT testmask, _In_z_ const char *szFunction, _In_z_ const char *szFile, int lineNum);
    void Disable()
    {
    }
    BOOL CheckFaultInjection();

  protected:
    UINT            m_testmask;
    // Override this function in any derived class to indicate that you have defined a destructor for that class
    // and that dtor calls Restore()
    virtual void DestructorDefinedThatCallsRestore() = 0;


  protected:
    ClrDebugState  *m_pClrDebugState;
    ClrDebugState   m_IncomingClrDebugState;

    ContractStackRecord m_contractStackRecord;

  public:
    // --------------------------------------------------------------------------------
    // These classes and declarations are used to implement our fake return keyword.
    // --------------------------------------------------------------------------------

    // ___box is used to protect the "detected" return value from being combined with other parts
    // of the return expression after we have processed it.  This can happen if the return
    // expression is a non-parenthesized expression with an operator of lower precedence than
    // ">".
    //
    // If you have such a case (and see this class listed in an error message),
    // parenthesize your return value expression.
    template <typename T>
    class Box__USE_PARENS_WITH_THIS_EXPRESSION
    {
        const T &value;

    public:

        FORCEINLINE Box__USE_PARENS_WITH_THIS_EXPRESSION(const T &value)
          : value(value)
          {
          }

        FORCEINLINE const T& Unbox()
          {
              return value;
          }
    };

    // PseudoTemplate is a class which can be instantated with a template-like syntax, resulting
    // in an expression which simply boxes a following value in a Box

    template <typename T>
    class PseudoTemplate
    {
      public:
        FORCEINLINE void *operator new (size_t size)
        {
            return NULL;
        }

        FORCEINLINE Box__USE_PARENS_WITH_THIS_EXPRESSION<T> operator>(const T &value)
        {
            return Box__USE_PARENS_WITH_THIS_EXPRESSION<T>(value);
        }

        FORCEINLINE PseudoTemplate operator<(int dummy)
        {
            return PseudoTemplate();
        }
    };

    // Returner is used to assign the return value to the RETVAL local.  Note the use of
    // operator , because of its low precedence.

    template <typename RETURNTYPE>
    class Returner
    {
        RETURNTYPE      &m_value;
        BOOL            m_got;
    public:

        FORCEINLINE Returner(RETURNTYPE &value)
          : m_value(value),
            m_got(FALSE)
        {
        }

        template <typename T>
        FORCEINLINE RETURNTYPE operator,(Box__USE_PARENS_WITH_THIS_EXPRESSION<T> value)
        {
            m_value = value.Unbox();
            m_got = TRUE;
            return m_value;
        }

        FORCEINLINE void operator,(___maybetemplate<0> &dummy)
        {
            m_got = TRUE;
        }

        FORCEINLINE BOOL GotReturn()
        {
            return m_got;
        }
    };

    // This type ensures that postconditions were run via RETURN or RETURN_VOID
    class RanPostconditions
    {
    public:
        bool ran;
        int count;
        const char *function;

        FORCEINLINE RanPostconditions(const char *function)
          : ran(false),
            count(0),
            function(function)
        {
        }

        FORCEINLINE int operator++()
        {
            return ++count;
        }

        FORCEINLINE ~RanPostconditions()
        {
            // Note: __uncaught_exception() is not a perfect check. It will return TRUE during any exception
            // processing. So, if there is a contract called from an exception filter (like our
            // COMPlusFrameHandler) then it will return TRUE and the saftey check below will not be performed.
            if (!__uncaught_exception())
                ASSERT_CHECK(count == 0 || ran, function, "Didn't run postconditions - be sure to use RETURN at the end of the function");
        }

    };

    // Set contract enforcement level
    static void SetUnconditionalContractEnforcement(BOOL enforceUnconditionally);

    // Check contract enforcement
    static BOOL EnforceContract();

 private:
    static BOOL s_alwaysEnforceContracts;
};

class Contract: public BaseContract
{
   // Have to override this function in any derived class to indicate that a valid destructor is defined for this class
   virtual void DestructorDefinedThatCallsRestore(){}

   public:
    NOTHROW_DECL ~Contract()
    {
        Restore();
    }
};

#endif // ENABLE_CONTRACTS_IMPL


#ifdef _DEBUG

// Valid parameters for CONTRACT_VIOLATION macro
enum ContractViolationBits
{
    ThrowsViolation = 0x00000001,  // suppress THROW tags in this scope
    GCViolation     = 0x00000002,  // suppress GCTRIGGER tags in this scope
    ModeViolation   = 0x00000004,  // suppress MODE_PREEMP and MODE_COOP tags in this scope
    FaultViolation  = 0x00000008,  // suppress INJECT_FAULT assertions in this scope
    FaultNotFatal   = 0x00000010,  // suppress INJECT_FAULT but not fault injection by harness
    LoadsTypeViolation      = 0x00000040,  // suppress LOADS_TYPE tags in this scope
    TakesLockViolation      = 0x00000080,  // suppress CAN_TAKE_LOCK tags in this scope
    HostViolation           = 0x00000100,  // suppress HOST_CALLS tags in this scope

    //These are not violation bits. We steal some bits out of the violation mask to serve as
    // general flag bits.
    CanFreeMe       = 0x00010000,  // If this bit is ON, the ClrDebugState was allocated by
                                   // a version of utilcode that registers an Fls Callback to free
                                   // the state. If this bit is OFF, the ClrDebugState was allocated
                                   // by an old version of utilcode that doesn't. (And you can't
                                   // assume that the old utilcode used the same allocator as the new utilcode.)
                                   // (Most likely, this is because you are using an older shim with
                                   // a newer mscorwks.dll)
                                   //
                                   // The Fls callback must only attempt to free debugstates that
                                   // have this bit on.

    BadDebugState   = 0x00020000,  // If we OOM creating the ClrDebugState, we return a pointer to
                                   // a static ClrDebugState that has this bit turned on. (We don't
                                   // want to slow down contracts with null tests everywhere.)
                                   // Other than this specific bit, all other fields of the DebugState
                                   // must be considered trash. You can stomp on them and you can bit-test them
                                   // but you can't throw up any asserts based on them and you certainly
                                   // can't deref any pointers stored in the bad DebugState.

    AllViolation    = 0xFFFFFFFF,
};

#endif

#ifdef ENABLE_CONTRACTS_IMPL

// Global variables allow PRECONDITION and POSTCONDITION to be used outside contracts
static const BaseContract::Operation ___op = (Contract::Operation) (Contract::Preconditions
                                                                |Contract::Postconditions);
enum {
    ___disabled = 0
};

static UINT ___testmask;

// End of global variables

static int ___ran;

class __SafeToUsePostCondition {
public:
    static int safe_to_use_postcondition() {return 0;};
};

class __YouCannotUseAPostConditionHere {
private:
    static int safe_to_use_postcondition() {return 0;};
};

typedef __SafeToUsePostCondition __PostConditionOK;

// Uncomment the following line to disable runtime contracts completely - PRE/POST conditions will still be present
//#define __FORCE_NORUNTIME_CONTRACTS__ 1

#ifndef __FORCE_NORUNTIME_CONTRACTS__

#define CONTRACT_SETUP(_contracttype, _returntype, _returnexp)          \
    _returntype RETVAL;                                                 \
    _contracttype ___contract;                                          \
    Contract::Returner<_returntype> ___returner(RETVAL);                \
    Contract::RanPostconditions ___ran(__FUNCTION__);                   \
    Contract::Operation ___op = Contract::Setup;                        \
    BOOL ___contract_enabled = FALSE;                                   \
    DEBUG_ASSURE_NO_RETURN_BEGIN(CONTRACT)                              \
    ___contract_enabled = Contract::EnforceContract();                  \
    enum {___disabled = 0};                                             \
    if (!___contract_enabled)                                           \
        ___contract.Disable();                                          \
    else                                                                \
    {                                                                   \
        enum { ___CheckMustBeInside_CONTRACT = 1 };                     \
        if (0)                                                          \
        {                                                               \
        /* If you see an "unreferenced label" warning with this name, */\
        /* Be sure that you have a RETURN at the end of your */         \
        /* CONTRACT_VOID function */                                    \
        ___run_postconditions_DID_YOU_FORGET_A_RETURN:                  \
            if (___contract_enabled)                                    \
            {                                                           \
                ___op = Contract::Postconditions;                       \
                ___ran.ran = true;                                      \
            }                                                           \
            else                                                        \
            {                                                           \
                DEBUG_OK_TO_RETURN_BEGIN(CONTRACT)                      \
              ___run_return:                                            \
                return _returnexp;                                      \
                DEBUG_OK_TO_RETURN_END(CONTRACT)                        \
            }                                                           \
        }                                                               \
        if (0)                                                          \
        {                                                               \
        ___run_preconditions:                                           \
            ___op = Contract::Preconditions;                            \
        }                                                               \
        UINT ___testmask = 0;                                           \

#define CONTRACTL_SETUP(_contracttype)                                  \
    _contracttype ___contract;                                          \
    BOOL ___contract_enabled = Contract::EnforceContract();             \
    enum {___disabled = 0};                                             \
    if (!___contract_enabled)                                           \
        ___contract.Disable();                                          \
    else                                                                \
    {                                                                   \
        typedef __YouCannotUseAPostConditionHere __PostConditionOK;     \
        enum { ___CheckMustBeInside_CONTRACT = 1 };                     \
        Contract::Operation ___op = Contract::Setup;                    \
        enum {___disabled = 0};                                         \
        if (0)                                                          \
        {                                                               \
          ___run_preconditions:                                         \
            ___op = Contract::Preconditions;                            \
        }                                                               \
        if (0)                                                          \
        {                                                               \
        /* define for CONTRACT_END even though we can't get here */     \
          ___run_return:                                                \
            UNREACHABLE();                                              \
        }                                                               \
        UINT ___testmask = 0;                                           \

#else // #ifndef __FORCE_NORUNTIME_CONTRACTS__

#define CONTRACT_SETUP(_contracttype, _returntype, _returnexp)              \
        _returntype RETVAL;                                                 \
        Contract::Returner<_returntype> ___returner(RETVAL);                \
        Contract::RanPostconditions ___ran(__FUNCTION__);                   \
        Contract::Operation ___op = Contract::Setup;                        \
        DEBUG_ASSURE_NO_RETURN_BEGIN(CONTRACT)                              \
        BOOL ___contract_enabled = Contract::EnforceContract();             \
        enum {___disabled = 0};                                             \
        {                                                                   \
            enum { ___CheckMustBeInside_CONTRACT = 1 };                     \
            if (0)                                                          \
            {                                                               \
            /* If you see an "unreferenced label" warning with this name, */\
            /* Be sure that you have a RETURN at the end of your */         \
            /* CONTRACT_VOID function */                                    \
            ___run_postconditions_DID_YOU_FORGET_A_RETURN:                  \
                if (___contract_enabled)                                    \
                {                                                           \
                    ___op = Contract::Postconditions;                       \
                    ___ran.ran = true;                                      \
                }                                                           \
                else                                                        \
                {                                                           \
                    DEBUG_OK_TO_RETURN_BEGIN(CONTRACT)                      \
                  ___run_return:                                            \
                    return _returnexp;                                      \
                    DEBUG_OK_TO_RETURN_END(CONTRACT)                        \
                }                                                           \
            }                                                               \
            if (0)                                                          \
            {                                                               \
            ___run_preconditions:                                           \
                ___op = Contract::Preconditions;                            \
            }                                                               \
            UINT ___testmask = 0;                                           \




#define CONTRACTL_SETUP(_contracttype)                                  \
    BOOL ___contract_enabled = Contract::EnforceContract();             \
    enum {___disabled = 0};                                             \
    {                                                                   \
        typedef __YouCannotUseAPostConditionHere __PostConditionOK;     \
            enum { ___CheckMustBeInside_CONTRACT = 1 };                 \
        Contract::Operation ___op = Contract::Setup;                    \
        enum {___disabled = 0};                                         \
        if (0)                                                          \
        {                                                               \
          ___run_preconditions:                                         \
            ___op = Contract::Preconditions;                            \
        }                                                               \
        if (0)                                                          \
        {                                                               \
        /* define for CONTRACT_END even though we can't get here */     \
          ___run_return:                                                \
            UNREACHABLE();                                              \
        }                                                               \
        UINT ___testmask = 0;                                           \

#endif // __FORCE_NORUNTIME_CONTRACTS__


#define CUSTOM_CONTRACT(_contracttype, _returntype)                     \
        typedef Contract::PseudoTemplate<_returntype> ___maybetemplate; \
        CONTRACT_SETUP(_contracttype, _returntype, RETVAL)

#define CUSTOM_CONTRACT_VOID(_contracttype)                             \
        CONTRACT_SETUP(_contracttype, int, ;)

#define CUSTOM_CONTRACTL(_contracttype)                                 \
        CONTRACTL_SETUP(_contracttype)

// Although this thing only needs to run in the Setup phase, we'll let it
// run unconditionally. This way, the compiler will see a sequence like this:
//
//    THROWS; GC_TRIGGERS; FORBID_FAULT ==>
//
//    ___testmask |= constant
//    ___testmask |= constant
//    ___testmask |= constant
//
// and be able to fold all these into a single constant at runtime.
//
#define REQUEST_TEST(thetest, todisable)   (___testmask |= (___CheckMustBeInside_CONTRACT, (___disabled ? (todisable) : (thetest))))


#define INJECT_FAULT(_statement)                                                            \
        do                                                                                  \
        {                                                                                   \
            STATIC_CONTRACT_FAULT;                                                          \
            REQUEST_TEST(Contract::FAULT_Inject, Contract::FAULT_Disabled);                 \
            if (0)                                                                          \
        {                                                                                   \
            _statement;                                                                     \
            }                                                                               \
        }                                                                                   \
        while(0)                                                                            \


#define FORBID_FAULT  do { STATIC_CONTRACT_FORBID_FAULT; REQUEST_TEST(Contract::FAULT_Forbid, Contract::FAULT_Disabled); } while(0)

#define THROWS        do { STATIC_CONTRACT_THROWS; REQUEST_TEST(Contract::THROWS_Yes, Contract::THROWS_Disabled); } while(0)

#define NOTHROW       do { STATIC_CONTRACT_NOTHROW; REQUEST_TEST(Contract::THROWS_No,  Contract::THROWS_Disabled); } while(0)                                                               \

#define ENTRY_POINT   STATIC_CONTRACT_ENTRY_POINT

#define LOADS_TYPE(maxlevel)  do { REQUEST_TEST( ((maxlevel) + 1) << Contract::LOADS_TYPE_Shift, Contract::LOADS_TYPE_Disabled ); } while(0)

#define CAN_TAKE_LOCK    do { STATIC_CONTRACT_CAN_TAKE_LOCK; REQUEST_TEST(Contract::CAN_TAKE_LOCK_Yes, Contract::CAN_TAKE_LOCK_Disabled); } while(0)

#define CANNOT_TAKE_LOCK   do { STATIC_CONTRACT_CANNOT_TAKE_LOCK; REQUEST_TEST(Contract::CAN_TAKE_LOCK_No,  Contract::CAN_TAKE_LOCK_Disabled); } while(0)

#define CANNOT_RETAKE_LOCK   do { REQUEST_TEST(Contract::CAN_RETAKE_LOCK_No,  Contract::CAN_RETAKE_LOCK_No_Disabled); } while(0)

#define DEBUG_ONLY do { STATIC_CONTRACT_DEBUG_ONLY; REQUEST_TEST(Contract::DEBUG_ONLY_Yes, 0);  } while (0)

#ifndef __DISABLE_PREPOST_CONDITIONS__
#define PRECONDITION_MSG(_expression, _message)                                             \
        do                                                                                  \
        {                                                                                   \
		    enum { ___CheckMustBeInside_CONTRACT = 1 };                                     \
            REQUEST_TEST(Contract::PRECONDITION_Used, 0);                                   \
            if ((___op&Contract::Preconditions) && !___disabled)                            \
                ASSERT_CHECK(_expression, _message, "Precondition failure");                \
        }                                                                                   \
        while(0)


#define PRECONDITION(_expression)                                                           \
        PRECONDITION_MSG(_expression, NULL)

#define POSTCONDITION_MSG(_expression, _message)                                            \
        ++___ran;                                                                           \
        if ((!(0 && __PostConditionOK::safe_to_use_postcondition())) &&                     \
            (___op&Contract::Postconditions) &&                                             \
            !___disabled)                                                                   \
        {                                                                                   \
            ASSERT_CHECK(_expression, _message, "Postcondition failure");                   \
        }

#define POSTCONDITION(_expression)                                                          \
        POSTCONDITION_MSG(_expression, NULL)

#define INSTANCE_CHECK                                                                      \
        ___CheckMustBeInside_CONTRACT;                                                      \
        if ((___op&Contract::Preconditions) && !___disabled)                                \
            ASSERT_CHECK(CheckPointer(this), NULL, "Instance precheck failure");            \
        ++___ran;                                                                           \
        if ((___op&Contract::Postconditions) && !___disabled)                               \
            ASSERT_CHECK(CheckPointer(this), NULL, "Instance postcheck failure");

#define INSTANCE_CHECK_NULL                                                                 \
        ___CheckMustBeInside_CONTRACT;                                                      \
        if ((___op&Contract::Preconditions) && !___disabled)                                \
            ASSERT_CHECK(CheckPointer(this, NULL_OK), NULL, "Instance precheck failure");   \
        ++___ran;                                                                           \
        if ((___op&Contract::Postconditions) && !___disabled)                               \
            ASSERT_CHECK(CheckPointer(this, NULL_OK), NULL, "Instance postcheck failure");

#define CONSTRUCTOR_CHECK                                                                   \
        ___CheckMustBeInside_CONTRACT;                                                      \
        ++___ran;                                                                           \
        if ((___op&Contract::Postconditions) && !___disabled)                               \
            ASSERT_CHECK(CheckPointer(this), NULL, "Instance postcheck failure");

#define DESTRUCTOR_CHECK                                                                    \
        ___CheckMustBeInside_CONTRACT;                                                      \
        NOTHROW;                                                                            \
        if ((___op&Contract::Preconditions) && !___disabled)                                \
            ASSERT_CHECK(CheckPointer(this), NULL, "Instance precheck failure");
#else // __DISABLE_PREPOST_CONDITIONS__


#define PRECONDITION_MSG(_expression, _message)     do { } while(0)
#define PRECONDITION(_expression)                   do { } while(0)
#define POSTCONDITION_MSG(_expression, _message)    do { } while(0)
#define POSTCONDITION(_expression)                  do { } while(0)
#define INSTANCE_CHECK
#define INSTANCE_CHECK_NULL
#define CONSTRUCTOR_CHECK
#define DESTRUCTOR_CHECK

#endif // __DISABLE_PREPOST_CONDITIONS__

#define UNCHECKED(thecheck)                                                                 \
        do {                                                                                \
            ANNOTATION_UNCHECKED(thecheck);                                                 \
            enum {___disabled = 1 };                                                        \
            thecheck;                                                                       \
        } while(0)

#define DISABLED(thecheck) UNCHECKED(thecheck)

#define WRAPPER(thecheck) UNCHECKED(thecheck)

// This keyword is redundant but it's handy for reducing the nuisance editing you
// have to when repeatedly enabling and disabling contract items while debugging.
// You shouldn't check in code that explicitly uses ENABLED.
#define ENABLED(_check) _check


#ifndef __FORCE_NORUNTIME_CONTRACTS__
#define CONTRACTL_END                                                                       \
        if (___op & Contract::Setup)                                                        \
        {                                                                                   \
            ___contract.DoChecks(___testmask, __FUNCTION__, __FILE__, __LINE__);            \
            if (___testmask & Contract::PRECONDITION_Used)                                  \
            {                                                                               \
                goto ___run_preconditions;                                                  \
            }                                                                               \
        }                                                                                   \
        else if (___op & Contract::Postconditions)                                          \
        {                                                                                   \
            goto ___run_return;                                                             \
        }                                                                                   \
        ___CheckMustBeInside_CONTRACT;                                                      \
   }

#else

#define CONTRACTL_END                                                                       \
        if (___op & Contract::Setup)                                                        \
        {                                                                                   \
            if (___testmask & Contract::PRECONDITION_Used)                                  \
            {                                                                               \
                goto ___run_preconditions;                                                  \
            }                                                                               \
        }                                                                                   \
        else if (___op & Contract::Postconditions)                                          \
        {                                                                                   \
            goto ___run_return;                                                             \
        }                                                                                   \
        ___CheckMustBeInside_CONTRACT;                                                      \
   }                                                                                        \

#endif // __FORCE_NORUNTIME_CONTRACTS__

#define CONTRACT_END   CONTRACTL_END                                                        \
   DEBUG_ASSURE_NO_RETURN_END(CONTRACT)                                                     \


// The final expression in the RETURN macro deserves special explanation (or something.)
// The expression is constructed so as to be syntactically ambiguous, depending on whether
// __maybetemplate is a template or not.  If it is a template, the expression is syntactically
// correct as-is.  If it is not, the angle brackets are interpreted as
// less than & greater than, and the expression is incomplete.  This is the point - we can
// choose whether we need an expression or not based on the context in which the macro is used.
// This allows the same RETURN macro to be used both in value-returning and void-returning
// contracts.
//
// The "__returner ," portion of the expression is used instead of "RETVAL =", since ","
// has lower precedence than "=". (Ain't overloaded operators fun.)
//
// Also note that the < and > operators on the non-template version of __maybetemplate
// are overridden to "box" the return value in a special type and pass it
// through to the __returner's "," operator.  This is so we can detect a case where an
// operator with lower precedence than ">" is in the return expression - in such a case we
// will get a type error message, which instructs that parens be placed around the return
// value expression.

#define RETURN_BODY                                                                         \
    if (___returner.GotReturn())                                                            \
        goto ___run_postconditions_DID_YOU_FORGET_A_RETURN;                                 \
    else                                                                                    \
        ___returner, * new ___maybetemplate < 0 >


// We have two versions of the RETURN macro.  CONTRACT_RETURN is for use inside the CONTRACT
// scope where it is OK to return this way, even though the CONTRACT macro itself does not
// allow a return.  RETURN is for use inside the function body where it might not be OK
// to return and we need to ensure that we don't allow a return where one should not happen
//
#define RETURN                                                                              \
    while (DEBUG_ASSURE_SAFE_TO_RETURN, TRUE)                                               \
        RETURN_BODY                                                                         \

#define RETURN_VOID                                                                         \
    RETURN

#define CONTRACT_RETURN                                                                     \
    while (___CheckMustBeInside_CONTRACT, TRUE)                                             \
        RETURN_BODY                                                                         \

#define CONTRACT_RETURN_VOID                                                                \
    CONTRACT_RETURN                                                                         \

#if 0
#define CUSTOM_LIMITED_METHOD_CONTRACT(_contracttype)                                                 \
    {                                                                                       \
        _contracttype ___contract;                                                          \
        STATIC_CONTRACT_LEAF;                                                               \
        ___contract.DoChecks(Contract::THROWS_No|Contract::GC_NoTrigger|Contract::MODE_Disabled|Contract::FAULT_Disabled);     \
        /* Should add some assertion mechanism to ensure no other contracts are called */   \
    }
#else
#define CUSTOM_LIMITED_METHOD_CONTRACT(_contracttype)                                                 \
    {                                                                                       \
        STATIC_CONTRACT_LEAF;                                                               \
    }
#endif

#define CUSTOM_WRAPPER_NO_CONTRACT(_contracttype)                                              \
    {                                                                                       \
        /* Should add some assertion mechanism to ensure one other contract is called */    \
        STATIC_CONTRACT_WRAPPER;                                                            \
    }

#define CONTRACT_THROWS()                                                                   \
    {                                                                                       \
        ::GetClrDebugState()->CheckOkayToThrow(__FUNCTION__, __FILE__, __LINE__);           \
    }

#define CONTRACT_THROWSEX(__func, __file, __line)                                           \
    {                                                                                       \
        ::GetClrDebugState()->CheckOkayToThrow(__func, __file, __line);                     \
    }

#else // ENABLE_CONTRACTS_IMPL
#define CUSTOM_CONTRACT(_contracttype, _returntype)         if (0) {  struct YouCannotUseThisHere { int x; };   // This temporary typedef allows retail use of
#define CUSTOM_CONTRACT_VOID(_contracttype)                 if (0) {  struct YouCannotUseThisHere { int x; };   // FORBIDGC_LOADER_USE_ENABLED
#define CUSTOM_CONTRACTL(_contracttype)                     if (0) {  struct YouCannotUseThisHere { int x; };   // inside contracts and asserts but nowhere else.

#define INJECT_FAULT(_statement)
#define FORBID_FAULT
#define THROWS
#define NOTHROW
#define CAN_TAKE_LOCK
#define CANNOT_TAKE_LOCK
#define CANNOT_RETAKE_LOCK
#define LOADS_TYPE(maxlevel)
#define ENTRY_POINT

#ifdef _DEBUG
// This can only appear in a debug function so don't define it non-debug
#define DEBUG_ONLY STATIC_CONTRACT_DEBUG_ONLY
#else
#define DEBUG_ONLY
#endif

#define PRECONDITION_MSG(_expression, _message)     do { } while(0)
#define PRECONDITION(_expression)                   do { } while(0)
#define POSTCONDITION_MSG(_expression, _message)    do { } while(0)
#define POSTCONDITION(_expression)                  do { } while(0)
#define INSTANCE_CHECK
#define INSTANCE_CHECK_NULL
#define CONSTRUCTOR_CHECK
#define DESTRUCTOR_CHECK
#define UNCHECKED(thecheck)
#define DISABLED(thecheck)
#define WRAPPER(thecheck)
#define ENABLED(_check)
#define CONTRACT_END                                        }
#define CONTRACTL_END                                       }

#define CUSTOM_LIMITED_METHOD_CONTRACT(_contracttype) \
    {                                                                                       \
        /* Should add some assertion mechanism to ensure one other contract is called */    \
        STATIC_CONTRACT_LEAF;                                                            \
    }
#define CUSTOM_WRAPPER_NO_CONTRACT(_contracttype) \
    {                                                                                       \
        /* Should add some assertion mechanism to ensure one other contract is called */    \
        STATIC_CONTRACT_WRAPPER;                                                            \
    }


#define RETURN return
#define RETURN_VOID RETURN

#define CONTRACT_THROWS()
#define CONTRACT_THROWSEX(__func, __file, __line)

#endif  // ENABLE_CONTRACTS_IMPL


#define CONTRACT(_returntype)  CUSTOM_CONTRACT(Contract, _returntype)
#define CONTRACT_VOID  CUSTOM_CONTRACT_VOID(Contract)
#define CONTRACTL CUSTOM_CONTRACTL(Contract)

// See description near the top of the file
#define LIMITED_METHOD_CONTRACT CUSTOM_LIMITED_METHOD_CONTRACT(Contract)

#define WRAPPER_NO_CONTRACT CUSTOM_WRAPPER_NO_CONTRACT(Contract)

// GC_NOTRIGGER allowed but not currently enforced at runtime
#define GC_NOTRIGGER STATIC_CONTRACT_GC_NOTRIGGER
#define GC_TRIGGERS static_assert(false, "TriggersGC not supported in utilcode contracts")

#ifdef ENABLE_CONTRACTS_IMPL
template <UINT_PTR VIOLATION_MASK>
class ContractViolationHolder
{
public:
    ContractViolationHolder()
    {
        m_pviolationmask = NULL;
        m_oldviolationmask = 0;
    }

    DEBUG_NOINLINE void Enter();

    DEBUG_NOINLINE void Leave()
    {
        SCAN_SCOPE_END;
        LeaveInternal();
    };

protected:
    // We require that violationMask is passed as a parameter here to hopefully defeat the
    // compiler's desire to fold all the Enter and Ctor implementations together.
    FORCEINLINE void EnterInternal(UINT_PTR violationMask)
    {
        _ASSERTE(0 == (violationMask & ~(ThrowsViolation | GCViolation | ModeViolation | FaultViolation |
            FaultNotFatal | HostViolation |
            TakesLockViolation | LoadsTypeViolation)) ||
            violationMask == AllViolation);

        m_pviolationmask = GetClrDebugState()->ViolationMaskPtr();
        m_oldviolationmask = *m_pviolationmask;
        *m_pviolationmask = (m_oldviolationmask | violationMask);
    };

    FORCEINLINE void LeaveInternal()
    {
        // This can be used in places where our debug state has been destroyed, so check for it first.
        if (CheckClrDebugState())
        {
            _ASSERTE(m_pviolationmask != NULL);
            *m_pviolationmask = m_oldviolationmask;
        }
    };

    UINT_PTR *m_pviolationmask;
    UINT_PTR m_oldviolationmask;
};

template <UINT_PTR VIOLATION_MASK>
class AutoCleanupContractViolationHolder : ContractViolationHolder<VIOLATION_MASK>
{
public:
    DEBUG_NOINLINE AutoCleanupContractViolationHolder(BOOL fEnterViolation = TRUE);

    DEBUG_NOINLINE ~AutoCleanupContractViolationHolder()
    {
        SCAN_SCOPE_END;
        this->LeaveInternal();
    };
};

#endif  // ENABLE_CONTRACTS_IMPL

#ifdef ENABLE_CONTRACTS_IMPL
#define BEGIN_CONTRACT_VIOLATION(violationmask)                             \
    {                                                                       \
        ContractViolationHolder<violationmask> __violationHolder_onlyOneAllowedPerScope;   \
        __violationHolder_onlyOneAllowedPerScope.Enter();                   \
        DEBUG_ASSURE_NO_RETURN_BEGIN(CONTRACT)                              \

// Use this to jump out prematurely from a violation.  Used for EH
// when the function might not return
#define RESET_CONTRACT_VIOLATION()                                          \
        __violationHolder_onlyOneAllowedPerScope.Leave();                   \

#define END_CONTRACT_VIOLATION                                              \
        DEBUG_ASSURE_NO_RETURN_END(CONTRACT)                                \
        __violationHolder_onlyOneAllowedPerScope.Leave();                   \
    }                                                                       \

// See description near the top of the file
#define CONTRACT_VIOLATION(violationMask)                                   \
    AutoCleanupContractViolationHolder<violationMask> __violationHolder_onlyOneAllowedPerScope;


// Reasons for having the violation.  Use one of these values as an additional parameter to
// E.g. PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonContractInfrastructure)
// New values and explanations can be added when needed.
enum PermanentContractViolationReason
{
    ReasonContractInfrastructure,        // This violation is there for contract test or infrastructure purposes.
    ReasonDebugOnly,                     // Code path doesn't occur on retail builds
    ReasonNonShippingCode,               // Code runs in undocumented non-shipping feature
    ReasonIBC,                           // Code runs in IBC scenarios only and the violation is safe.
    ReasonNGEN,                          // Code runs in NGEN scenarios only and the violation is safe.
    ReasonProfilerCallout,               // Profiler implementers are guaranteed not to throw.
    ReasonUnsupportedForSQLF1Profiling,  // This code path violates HOST_NOCALLS, but that's ok b/c SQL will never
                                         // invoke it, and thus SQL/F1 profiling (the primary reason to enforce
                                         // HOST_NOCALLS) is not in danger.
    ReasonRuntimeReentrancy,             // e.g. SafeQueryInterface
    ReasonShutdownOnly,                  // Code path only runs as part of Shutdown and the violation is safe.
    ReasonSOTolerance,                   // We would like to redesign SO contracts anyways
    ReasonStartupOnly,                   // Code path only runs as part of Startup and the violation is safe.
    ReasonWorkaroundForScanBug,          // Violation is needed because of a bug in SCAN
    ReasonProfilerAsyncCannotRetakeLock, // Profiler may call this from redirected thread, causing a CANNOT_TAKE_LOCK
                                         // violation, but the scope is still protected with CANNOT_RETAKE_LOCK
    ReasonILStubWillNotThrow,            // Specially-crafted reverse COM IL stubs will not throw
};

// See the discussion near the top of the file on the use of PERMANENT_CONTRACT_VIOLATION
// The reasonEnum is currently only used for documentation and searchability.  Here
// we have the compiler check for a typo.
#define PERMANENT_CONTRACT_VIOLATION(violationMask, reasonEnum)            \
    if (0)                                                                 \
        PermanentContractViolationReason reason = reasonEnum;              \
    CONTRACT_VIOLATION(violationMask)

#define CONDITIONAL_CONTRACT_VIOLATION(violationMask, condition)            \
    AutoCleanupContractViolationHolder<violationMask> __violationHolder_onlyOneAllowedPerScope((condition));

#else
#define BEGIN_CONTRACT_VIOLATION(violationmask)
#define RESET_CONTRACT_VIOLATION()
#define END_CONTRACT_VIOLATION
#define CONTRACT_VIOLATION(violationmask)
#define CONDITIONAL_CONTRACT_VIOLATION(violationMask, condition)
#define PERMANENT_CONTRACT_VIOLATION(violationMask, reasonEnum)
#endif



#ifdef ENABLE_CONTRACTS_IMPL
// Holder for setting up a faultforbid region
class FaultForbidHolder
{
 public:
    DEBUG_NOINLINE FaultForbidHolder(BOOL fConditional, BOOL fAlloc, const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_FORBID_FAULT;

        m_fConditional = fConditional;
        if (m_fConditional)
        {
            m_pClrDebugState = GetClrDebugState(fAlloc);

            //
            // If we fail to get a debug state, then we must not be allocating and
            // we simply no-op this holder.
            //
            if (m_pClrDebugState == NULL)
            {
                _ASSERTE(!fAlloc);
                m_fConditional = FALSE;
                return;
            }

            m_oldClrDebugState = *m_pClrDebugState;

            m_pClrDebugState->ViolationMaskReset( FaultViolation|FaultNotFatal );
            m_pClrDebugState->SetFaultForbid();

            m_ContractStackRecord.m_szFunction = szFunction;
            m_ContractStackRecord.m_szFile     = szFile;
            m_ContractStackRecord.m_lineNum    = lineNum;
            m_ContractStackRecord.m_testmask   = (Contract::ALL_Disabled & ~((UINT)(Contract::FAULT_Mask))) | Contract::FAULT_Forbid;
            m_ContractStackRecord.m_construct  = "FAULT_FORBID";
            m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );
        }
    }

    DEBUG_NOINLINE ~FaultForbidHolder()
    {
        SCAN_SCOPE_END;

        if (m_fConditional)
        {
            *m_pClrDebugState = m_oldClrDebugState;
        }
    }

 private:
    ClrDebugState      *m_pClrDebugState;
    ClrDebugState       m_oldClrDebugState;
    BOOL m_fConditional;
    ContractStackRecord m_ContractStackRecord;

};
#endif  // ENABLE_CONTRACTS_IMPL


#ifdef ENABLE_CONTRACTS_IMPL

#define FAULT_FORBID() FaultForbidHolder _ffh(TRUE, TRUE, __FUNCTION__, __FILE__, __LINE__);
#define FAULT_FORBID_NO_ALLOC() FaultForbidHolder _ffh(TRUE, FALSE, __FUNCTION__, __FILE__, __LINE__);
#define MAYBE_FAULT_FORBID(cond) FaultForbidHolder _ffh(cond, TRUE, __FUNCTION__, __FILE__, __LINE__);
#define MAYBE_FAULT_FORBID_NO_ALLOC(cond) FaultForbidHolder _ffh(cond, FALSE, __FUNCTION__, __FILE__, __LINE__);

#else   // ENABLE_CONTRACTS_IMPL

#define FAULT_FORBID() ;
#define FAULT_FORBID_NO_ALLOC() ;
#define MAYBE_FAULT_FORBID(cond) ;
#define MAYBE_FAULT_FORBID_NO_ALLOC(cond) ;

#endif  // ENABLE_CONTRACTS_IMPL


#ifdef ENABLE_CONTRACTS_IMPL

inline BOOL AreFaultsForbiddenHelper()
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;

    ClrDebugState *pClrDebugState = CheckClrDebugState();
    if (!pClrDebugState)
    {
        // By default, faults are not forbidden. Not the most desirable default
        // but we'd never get this debug infrastructure bootstrapped otherwise.
        return FALSE;
    }
    else
    {
        return pClrDebugState->IsFaultForbid() && (!(pClrDebugState->ViolationMask() & (FaultViolation|FaultNotFatal|BadDebugState)));
    }
}

#define ARE_FAULTS_FORBIDDEN() AreFaultsForbiddenHelper()
#else

// If you got an error about ARE_FAULTS_FORBIDDEN being undefined, it's because you tried
// to use this predicate in a free build outside of a CONTRACT or ASSERT.
//
#define ARE_FAULTS_FORBIDDEN() (sizeof(YouCannotUseThisHere) != 0)
#endif


// This allows a fault-forbid region to invoke a non-mandatory allocation, such as for the
// purpose of growing a lookaside cache (if the allocation fails, the code can abandon the
// cache growing operation without negative effect.)
//
// Although it's implemented using CONTRACT_VIOLATION(), it's not a bug to have this in the code.
//
// It *is* a bug to use this to hide a situation where an OOM is genuinely fatal but not handled.
#define FAULT_NOT_FATAL() CONTRACT_VIOLATION(FaultNotFatal)



#ifdef ENABLE_CONTRACTS_IMPL

//------------------------------------------------------------------------------------
// Underlying class support for TRIGGERS_TYPE_LOAD and OVERRIDE_TYPE_LOAD_LEVEL_LIMIT.
// Don't reference this class directly. Use the macros.
//------------------------------------------------------------------------------------
class LoadsTypeHolder
{
 public:
    LoadsTypeHolder(BOOL     fConditional,
                    UINT     newLevel,
                    BOOL     fEnforceLevelChangeDirection,
                    const char    *szFunction,
                    const char    *szFile,
                    int      lineNum
                   );

    ~LoadsTypeHolder();

 private:
    ClrDebugState      *m_pClrDebugState;
    ClrDebugState       m_oldClrDebugState;
    BOOL                m_fConditional;
    ContractStackRecord m_contractStackRecord;

};

#endif  // ENABLE_CONTRACTS_IMPL


//------------------------------------------------------------------------------------
// TRIGGERS_TYPE_LOAD(newLevel)
//    Works just LOADS_TYPE in contracts but lets you protect individual scopes
//
// OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(newLevel)
//    Sets a new limit just like TRIGGERS_TYPE_LOAD but does not restrict you
//    to decreasing the limit. Only the loader should use this and only when it
//    can prove structurally that no recursion will occur as a result.
//------------------------------------------------------------------------------------
#ifdef ENABLE_CONTRACTS_IMPL

#define TRIGGERS_TYPE_LOAD(newLevel)                            LoadsTypeHolder _lth(TRUE,    newLevel, TRUE,  __FUNCTION__, __FILE__, __LINE__);
#define MAYBE_TRIGGERS_TYPE_LOAD(newLevel, fEnable)             LoadsTypeHolder _lth(fEnable, newLevel, TRUE,  __FUNCTION__, __FILE__, __LINE__);
#define OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(newLevel)                LoadsTypeHolder _lth(TRUE,    newLevel, FALSE, __FUNCTION__, __FILE__, __LINE__);
#define MAYBE_OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(newLevel, fEnable) LoadsTypeHolder _lth(fEnable, newLevel, FALSE, __FUNCTION__, __FILE__, __LINE__);

#else   // ENABLE_CONTRACTS_IMPL

#define TRIGGERS_TYPE_LOAD(newLevel)
#define MAYBE_TRIGGERS_TYPE_LOAD(newLevel, fEnable)
#define OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(newLevel)
#define MAYBE_OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(newLevel, fEnable)

#endif  // ENABLE_CONTRACTS_IMPL



#ifdef ENABLE_CONTRACTS_IMPL

// This sets up a marker that says its okay to throw on this thread. This is not a public macro, and should only be
// used from within the implementation of various try/catch macros.
class ClrTryMarkerHolder
{
public:
    DEBUG_NOINLINE ClrTryMarkerHolder()
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_THROWS;

        m_pClrDebugState = GetClrDebugState();
        m_oldOkayToThrowValue = m_pClrDebugState->IsOkToThrow();
        m_pClrDebugState->SetOkToThrow();
    }

    DEBUG_NOINLINE ~ClrTryMarkerHolder()
    {
        SCAN_SCOPE_END;

        m_pClrDebugState->SetOkToThrow( m_oldOkayToThrowValue );
    }

private:
    BOOL           m_oldOkayToThrowValue;
    ClrDebugState *m_pClrDebugState;
};

#define CLR_TRY_MARKER() ClrTryMarkerHolder ___tryMarkerHolder;

#else // ENABLE_CONTRACTS_IMPL

#define CLR_TRY_MARKER()

#endif

#ifdef ENABLE_CONTRACTS_IMPL
// Note: This routine will create a ClrDebugState if called for the first time.
// It cannot return NULL (see comment for InitClrDebugState).
inline ClrDebugState *GetClrDebugState(BOOL fAlloc)
{
    STATIC_CONTRACT_LIMITED_METHOD;

    ClrDebugState *pState = CheckClrDebugState();

    if (pState)
    {
        return pState;
    }

    if (fAlloc)
    {
        return CLRInitDebugState();
    }

    return NULL;
}
#endif // ENABLE_CONTRACTS_IMPL

#ifdef ENABLE_CONTRACTS_IMPL

class HostNoCallHolder
{
    public:
    DEBUG_NOINLINE HostNoCallHolder()
        {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_HOST_NOCALLS;

            m_clrDebugState = GetClrDebugState();
            m_previousState = m_clrDebugState->SetHostCaller(FALSE);
        }

    DEBUG_NOINLINE ~HostNoCallHolder()
        {
        SCAN_SCOPE_END;

            m_clrDebugState->SetHostCaller(m_previousState);
        }

     private:
        BOOL m_previousState;
        ClrDebugState* m_clrDebugState;

};

#define BEGIN_HOST_NOCALL_CODE \
    {                             \
        HostNoCallHolder __hostNoCallHolder;        \
        CantAllocHolder __cantAlloc;

#define END_HOST_NOCALL_CODE   \
    }

#else // ENABLE_CONTRACTS_IMPL
#define BEGIN_HOST_NOCALL_CODE                      \
    {                                               \
        CantAllocHolder __cantAlloc;                \

#define END_HOST_NOCALL_CODE                        \
    }
#endif


#if defined(ENABLE_CONTRACTS_IMPL)

// Macros to indicate we're taking or releasing locks

// Most general macros, not used directly
#define LOCK_TAKEN_MULTIPLE(dbgStateLockType, cEntrances, pvLock)    \
    ::GetClrDebugState()->LockTaken((dbgStateLockType), (cEntrances), (void*) (pvLock), __FUNCTION__, __FILE__, __LINE__)
#define LOCK_RELEASED_MULTIPLE(dbgStateLockType, cExits, pvLock)     \
    ::GetClrDebugState()->LockReleased((dbgStateLockType), (cExits), (void*) (pvLock))

// Use these only if you need to force multiple entrances or exits in a single
// line (e.g., to restore the lock to a previous state). CRWLock in vm\rwlock.cpp does this
#define EE_LOCK_TAKEN_MULTIPLE(cEntrances, pvLock)                          \
    LOCK_TAKEN_MULTIPLE(kDbgStateLockType_EE, cEntrances, pvLock)
#define EE_LOCK_RELEASED_MULTIPLE(cExits, pvLock)                           \
    LOCK_RELEASED_MULTIPLE(kDbgStateLockType_EE, cExits, pvLock)
#define HOST_BREAKABLE_CRST_TAKEN_MULTIPLE(cEntrances, pvLock)              \
    LOCK_TAKEN_MULTIPLE(kDbgStateLockType_HostBreakableCrst, cEntrances, pvLock)
#define HOST_BREAKABLE_CRST_RELEASED_MULTIPLE(cExits, pvLock)               \
    LOCK_RELEASED_MULTIPLE(kDbgStateLockType_HostBreakableCrst, cExits, pvLock)
#define USER_LOCK_TAKEN_MULTIPLE(cEntrances, pvLock)                        \
    LOCK_TAKEN_MULTIPLE(kDbgStateLockType_User, cEntrances, pvLock)
#define USER_LOCK_RELEASED_MULTIPLE(cExits, pvLock)                         \
    LOCK_RELEASED_MULTIPLE(kDbgStateLockType_User, cExits, pvLock)

// These are most typically used
#define EE_LOCK_TAKEN(pvLock)                   \
    LOCK_TAKEN_MULTIPLE(kDbgStateLockType_EE, 1, pvLock)
#define EE_LOCK_RELEASED(pvLock)                \
    LOCK_RELEASED_MULTIPLE(kDbgStateLockType_EE, 1, pvLock)
#define HOST_BREAKABLE_CRST_TAKEN(pvLock)       \
    LOCK_TAKEN_MULTIPLE(kDbgStateLockType_HostBreakableCrst, 1, pvLock)
#define HOST_BREAKABLE_CRST_RELEASED(pvLock)    \
    LOCK_RELEASED_MULTIPLE(kDbgStateLockType_HostBreakableCrst, 1, pvLock)
#define USER_LOCK_TAKEN(pvLock)                 \
    LOCK_TAKEN_MULTIPLE(kDbgStateLockType_User, 1, pvLock)
#define USER_LOCK_RELEASED(pvLock)              \
    LOCK_RELEASED_MULTIPLE(kDbgStateLockType_User, 1, pvLock)

#else // defined(ENABLE_CONTRACTS_IMPL)

#define LOCK_TAKEN_MULTIPLE(dbgStateLockType, cEntrances, pvLock)
#define LOCK_RELEASED_MULTIPLE(dbgStateLockType, cExits, pvLock)
#define EE_LOCK_TAKEN_MULTIPLE(cEntrances, pvLock)
#define EE_LOCK_RELEASED_MULTIPLE(cExits, pvLock)
#define HOST_BREAKABLE_CRST_TAKEN_MULTIPLE(cEntrances, pvLock)
#define HOST_BREAKABLE_CRST_RELEASED_MULTIPLE(cExits, pvLock)
#define USER_LOCK_TAKEN_MULTIPLE(cEntrances, pvLock)
#define USER_LOCK_RELEASED_MULTIPLE(cExits, pvLock)
#define EE_LOCK_TAKEN(pvLock)
#define EE_LOCK_RELEASED(pvLock)
#define HOST_BREAKABLE_CRST_TAKEN(pvLock)
#define HOST_BREAKABLE_CRST_RELEASED(pvLock)
#define USER_LOCK_TAKEN(pvLock)
#define USER_LOCK_RELEASED(pvLock)

#endif // defined(ENABLE_CONTRACTS_IMPL)

#if defined(ENABLE_CONTRACTS_IMPL)

// Abbreviation for an assert that is only considered if there is a valid
// ClrDebugState available.  Useful if you want to assert based on the value
// of GetDbgStateLockCount(), where a return of 0 (the default if there is no
// valid ClrDebugState available) would cause your assert to fire.  The variable
// __pClrDebugState is set to the current ClrDebugState, and may be used within
// your assert expression
#define ASSERT_UNLESS_NO_DEBUG_STATE(e)                                                 \
    {                                                                                   \
        ClrDebugState * __pClrDebugState = GetClrDebugState();                          \
        _ASSERTE(((__pClrDebugState->ViolationMask() & BadDebugState) != 0) || (e));    \
    }

#else // defined(ENABLE_CONTRACTS_IMPL)

#define ASSERT_UNLESS_NO_DEBUG_STATE(e)

#endif // defined(ENABLE_CONTRACTS_IMPL)


//-----------------------------------------------------------------------------
// Debug support to ensure that nobody calls New on the helper thread.
// This is for interop debugging.
// They should be using the InteropSafe heap.
// Having this in the meantime allows us to
// assert that the helper thread never calls new, and maintain a finite list of
// exceptions (bugs).
// Eventually, all those bugs should be fixed this holder can be completely removed.
//
// It is also the case that we disallow allocations when any thread is OS suspended
// This happens for a short time when we are suspending the EE.   We supress both
// of these.
//
// @todo- ideally this would be rolled into the ContractViolation.
// also, we'd have contract bit for whether APIs can be called on the helper thread.
// @todo - if we really wanted to be strict, we should make this per-thread.
//-----------------------------------------------------------------------------
#ifdef ENABLE_CONTRACTS_IMPL
extern Volatile<LONG> g_DbgSuppressAllocationAsserts;
#define SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE CounterHolder _AllowNewOnHelperHolder(&g_DbgSuppressAllocationAsserts);
#else
// Nothing in retail since this holder just disabled an assert.
#define SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE
#endif


//-----------------------------------------------------------------------------
// Support for contracts in DAC builds
//
// At the moment, most of the contract system is disabled in DAC builds.
// We do however want some simple static contracts in order to support static
// analysis tools that run on mscordacwks.dll like DacCop.
// Note that we want these static contracts in both DEBUG and retail builds.
// We also already get simple static contracts like WRAPPER and LEAF.
//
//-----------------------------------------------------------------------------
#if defined(DACCESS_COMPILE)

// SUPPORTS_DAC is an annotation that says the function is designed to be used in DAC builds.
// This enables full DacCop analysis on the function, including verifying that all functions that are
// called also support DAC.
#define SUPPORTS_DAC do { STATIC_CONTRACT_SUPPORTS_DAC; } while(0)

// Normally a function can be annotated just with WRAPPER_NO_CONTRACT, which (in addition to the normal
// contract meaning) indicates to DacCop that the function should be considered to support DAC when
// it is called from a supports-dac function.  This is to avoid having to add a DAC-specific contract
// to all the trivial one-line wrapper functions we have.
// However, we occasionally want these semantics even for functions which are not appropriate to label
// as WRAPPER_NO_CONTRACT.  For example, a template function may support DAC for certain template arguments,
// but not others (due to the functions it calls).  We want to ensure that when such a function is called
// in a DAC code path, analysis is enabled on that particular instantiation including checking all of the
// call targets specific to this template instantiation.  But we don't want to require that the call targets
// for ALL instantiations support dac, since we may not even be using them in DAC code paths.  Ideally we'd
// remove any such code from the DAC build, but this will take time.
#define SUPPORTS_DAC_WRAPPER do { STATIC_CONTRACT_WRAPPER;  } while(0)

// SUPPORTS_DAC_HOST_ONLY indicates that a function is allowed to be called in DAC builds, but rather
// than being a normal DAC function which operates on marshalled data, it is a host-only utility function
// that knows nothing about DAC and operates solely on the host.  For example, DbgAssertDialog is a utility
// function for popping assert dialogs - there is nothing DAC-specific about this.  Ideally such utility
// functions would be confined to their own library which had no access to DAC functionality, and which
// is not analyzed by DacCop.  At the moment splitting utilcode into two variations like this is too
// painful, but we hope to do it in the future (primarily to support functions which can be used in either
// DAC or host-only mode).
// WARNING: This contract disables DacCop analysis  on the function and any functions it calls, so it
// should be used very carefully.
#define SUPPORTS_DAC_HOST_ONLY do { STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY; } while(0)

#else
#define SUPPORTS_DAC
#define SUPPORTS_DAC_HOST_ONLY
#define SUPPORTS_DAC_WRAPPER
#endif // DACCESS_COMPILE

// LIMITED_METHOD_DAC_CONTRACT is a shortcut for LIMITED_METHOD_CONTRACT and SUPPORTS_DAC. Usefull for one-line inline functions.
#define LIMITED_METHOD_DAC_CONTRACT LIMITED_METHOD_CONTRACT; SUPPORTS_DAC

//
// The default contract is the recommended contract for ordinary code.
// The ordinary code can throw or trigger GC any time, does not operate
// on raw object refs, etc.
//

#define STANDARD_VM_CHECK           \
    THROWS;

#define STANDARD_VM_CONTRACT        \
    CONTRACTL                   \
    {                           \
        STANDARD_VM_CHECK;          \
    }                           \
    CONTRACTL_END;              \

#define STATIC_STANDARD_VM_CONTRACT         \
    STATIC_CONTRACT_THROWS;             \
    STATIC_CONTRACT_GC_TRIGGERS;        \
    STATIC_CONTRACT_MODE_PREEMPTIVE;

#define AFTER_CONTRACTS
#include "volatile.h"

#endif  // CONTRACT_H_

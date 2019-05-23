// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// THREADS.H -
//


// 
// 
// Currently represents a logical and physical COM+ thread. Later, these concepts will be separated.
//

// 
// #RuntimeThreadLocals.
// 
// Windows has a feature call Thread Local Storage (TLS, which is data that the OS allocates every time it
// creates a thread). Programs access this storage by using the Windows TlsAlloc, TlsGetValue, TlsSetValue
// APIs (see http://msdn2.microsoft.com/en-us/library/ms686812.aspx). The runtime allocates two such slots
// for its use
// 
//     * A slot that holds a pointer to the runtime thread object code:Thread (see code:#ThreadClass). The
//         runtime has a special optimized version of this helper code:GetThread (we actually emit assembly
//         code on the fly so it is as fast as possible). These code:Thread objects live in the
//         code:ThreadStore.
//         
//      * The other slot holds the current code:AppDomain (a managed equivalent of a process). The
//          runtime thread object also has a pointer to the thread's AppDomain (see code:Thread.m_pDomain,
//          so in theory this TLS is redundant. It is there for speed (one less pointer indirection). The
//          optimized helper for this is code:GetAppDomain (we emit assembly code on the fly for this one
//          too).
//          
// Initially these TLS slots are empty (when the OS starts up), however before we run managed code, we must
// set them properly so that managed code knows what AppDomain it is in and we can suspend threads properly
// for a GC (see code:#SuspendingTheRuntime)
// 
// #SuspendingTheRuntime
// 
// One of the primary differences between runtime code (managed code), and traditional (unmanaged code) is
// the existence of the GC heap (see file:gc.cpp#Overview). For the GC to do its job, it must be able to
// traverse all references to the GC heap, including ones on the stack of every thread, as well as any in
// hardware registers. While it is simple to state this requirement, it has long reaching effects, because
// properly accounting for all GC heap references ALL the time turns out to be quite hard. When we make a
// bookkeeping mistake, a GC reference is not reported at GC time, which means it will not be updated when the
// GC happens. Since memory in the GC heap can move, this can cause the pointer to point at 'random' places
// in the GC heap, causing data corruption. This is a 'GC Hole', and is very bad. We have special modes (see
// code:EEConfig.GetGCStressLevel) called GCStress to help find such issues.
// 
// In order to find all GC references on the stacks we need insure that no thread is manipulating a GC
// reference at the time of the scan. This is the job of code:Thread.SuspendRuntime. Logically it suspends
// every thread in the process. Unfortunately it can not literally simply call the OS SuspendThread API on
// all threads. The reason is that the other threads MIGHT hold important locks (for example there is a lock
// that is taken when unmanaged heap memory is requested, or when a DLL is loaded). In general process
// global structures in the OS will be protected by locks, and if you suspend a thread it might hold that
// lock. If you happen to need that OS service (eg you might need to allocated unmanaged memory), then
// deadlock will occur (as you wait on the suspended thread, that never wakes up).
// 
// Luckily, we don't need to actually suspend the threads, we just need to insure that all GC references on
// the stack are stable. This is where the concept of cooperative mode and preemptive mode (a bad name) come
// from.
// 
// #CooperativeMode
// 
// The runtime keeps a table of all threads that have ever run managed code in the code:ThreadStore table.
// The ThreadStore table holds a list of Thread objects (see code:#ThreadClass). This object holds all
// infomation about managed threads. Cooperative mode is defined as the mode the thread is in when the field
// code:Thread.m_fPreemptiveGCDisabled is non-zero. When this field is zero the thread is said to be in
// Preemptive mode (named because if you preempt the thread in this mode, it is guaranteed to be in a place
// where a GC can occur).
// 
// When a thread is in cooperative mode, it is basically saying that it is potentially modifying GC
// references, and so the runtime must Cooperate with it to get to a 'GC Safe' location where the GC
// references can be enumerated. This is the mode that a thread is in MOST times when it is running managed
// code (in fact if the EIP is in JIT compiled code, there is only one place where you are NOT in cooperative
// mode (Inlined PINVOKE transition code)). Conversely, any time non-runtime unmanaged code is running, the
// thread MUST NOT be in cooperative mode (you risk deadlock otherwise). Only code in mscorwks.dll might be
// running in either cooperative or preemptive mode.
// 
// It is easier to describe the invariant associated with being in Preemptive mode. When the thread is in
// preemptive mode (when code:Thread.m_fPreemptiveGCDisabled is zero), the thread guarantees two things
// 
//     * That it not currently running code that manipulates GC references.
//     * That it has set the code:Thread.m_pFrame pointer in the code:Thread to be a subclass of the class
//         code:Frame which marks the location on the stack where the last managed method frame is. This
//         allows the GC to start crawling the stack from there (essentially skip over the unmanaged frames).
//     * That the thread will not reenter managed code if the global variable code:g_TrapReturningThreads is
//         set (it will call code:Thread.RareDisablePreemptiveGC first which will block if a a suspension is
//         in progress)
// 
// The basic idea is that the suspension logic in code:Thread.SuspendRuntime first sets the global variable
// code:g_TrapReturningThreads and then checks if each thread in the ThreadStore is in Cooperative mode. If a
// thread is NOT in cooperative mode, the logic simply skips the thread, because it knows that the thread
// will stop itself before reentering managed code (because code:g_TrapReturningThreads is set). This avoids
// the deadlock problem mentioned earlier, because threads that are running unmanaged code are allowed to
// run. Enumeration of GC references starts at the first managed frame (pointed at by code:Thread.m_pFrame).
// 
// When a thread is in cooperative mode, it means that GC references might be being manipulated. There are
// two important possibilities
// 
//     * The CPU is running JIT compiled code
//     * The CPU is running code elsewhere (which should only be in mscorwks.dll, because everywhere else a
//         transition to preemptive mode should have happened first)
//     
// * #PartiallyInteruptibleCode
// * #FullyInteruptibleCode
// 
// If the Instruction pointer (x86/x64: EIP, ARM: R15/PC) is in JIT compiled code, we can detect this because we have tables that
// map the ranges of every method back to their code:MethodDesc (this the code:ICodeManager interface). In
// addition to knowing the method, these tables also point at 'GCInfo' that tell for that method which stack
// locations and which registers hold GC references at any particular instruction pointer. If the method is
// what is called FullyInterruptible, then we have information for any possible instruction pointer in the
// method and we can simply stop the thread (however we have to do this carefully TODO explain).
// 
// However for most methods, we only keep GC information for paticular EIP's, in particular we keep track of
// GC reference liveness only at call sites. Thus not every location is 'GC Safe' (that is we can enumerate
// all references, but must be 'driven' to a GC safe location).
// 
// We drive threads to GC safe locations by hijacking. This is a term for updating the return address on the
// stack so that we gain control when a method returns. If we find that we are in JITTed code but NOT at a GC
// safe location, then we find the return address for the method and modfiy it to cause the runtime to stop.
// We then let the method run. Hopefully the method quickly returns, and hits our hijack, and we are now at a
// GC-safe location (all call sites are GC-safe). If not we repeat the procedure (possibly moving the
// hijack). At some point a method returns, and we get control. For methods that have loops that don't make
// calls, we are forced to make the method FullyInterruptible, so we can be sure to stop the mehod.
// 
// This leaves only the case where we are in cooperative modes, but not in JIT compiled code (we should be in
// clr.dll). In this case we simply let the thread run. The idea is that code in clr.dll makes the
// promise that it will not do ANYTHING that will block (which includes taking a lock), while in cooperative
// mode, or do anything that might take a long time without polling to see if a GC is needed. Thus this code
// 'cooperates' to insure that GCs can happen in a timely fashion.
//
// If you need to switch the GC mode of the current thread, look for the GCX_COOP() and GCX_PREEMP() macros.
//

#ifndef __threads_h__
#define __threads_h__

#include "vars.hpp"
#include "util.hpp"
#include "eventstore.hpp"
#include "argslot.h"
#include "regdisp.h"
#include "mscoree.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"
#include "gcinfotypes.h"
#include <clrhost.h>

class     Thread;
class     ThreadStore;
class     MethodDesc;
struct    PendingSync;
class     AppDomain;
class     NDirect;
class     Frame;
class     ThreadBaseObject;
class     AppDomainStack;
class     LoadLevelLimiter;
class     DomainFile;
class     DeadlockAwareLock;
struct    HelperMethodFrameCallerList;
class     ThreadLocalIBCInfo;
class     EECodeInfo;
class     DebuggerPatchSkip;
class     FaultingExceptionFrame;
class     ContextTransitionFrame;
enum      BinderMethodID : int;
class     CRWLock;
struct    LockEntry;
class     PendingTypeLoadHolder;
class     PrepareCodeConfig;

struct    ThreadLocalBlock;
typedef DPTR(struct ThreadLocalBlock) PTR_ThreadLocalBlock;
typedef DPTR(PTR_ThreadLocalBlock) PTR_PTR_ThreadLocalBlock;

typedef void(*ADCallBackFcnType)(LPVOID);

#include "stackwalktypes.h"
#include "log.h"
#include "stackingallocator.h"
#include "excep.h"
#include "synch.h"
#include "exstate.h"
#include "threaddebugblockinginfo.h"
#include "interoputil.h"
#include "eventtrace.h"

#ifdef FEATURE_PERFTRACING
class EventPipeBufferList;
#endif // FEATURE_PERFTRACING

struct TLMTableEntry;

typedef DPTR(struct TLMTableEntry) PTR_TLMTableEntry;
typedef DPTR(struct ThreadLocalModule) PTR_ThreadLocalModule;

class ThreadStaticHandleTable;
struct ThreadLocalModule;
class Module;

struct ThreadLocalBlock
{
    friend class ClrDataAccess;

private:
    PTR_TLMTableEntry   m_pTLMTable;     // Table of ThreadLocalModules
    SIZE_T              m_TLMTableSize;  // Current size of table
    SpinLock            m_TLMTableLock;  // Spinlock used to synchronize growing the table and freeing TLM by other threads

    // Each ThreadLocalBlock has its own ThreadStaticHandleTable. The ThreadStaticHandleTable works
    // by allocating Object arrays on the GC heap and keeping them alive with pinning handles.
    //
    // We use the ThreadStaticHandleTable to allocate space for GC thread statics. A GC thread
    // static is thread static that is either a reference type or a value type whose layout
    // contains a pointer to a reference type.

    ThreadStaticHandleTable * m_pThreadStaticHandleTable;

    // Need to keep a list of the pinning handles we've created
    // so they can be cleaned up when the thread dies
    ObjectHandleList          m_PinningHandleList;

public: 

#ifndef DACCESS_COMPILE
    void AddPinningHandleToList(OBJECTHANDLE oh);
    void FreePinningHandles();
    void AllocateThreadStaticHandles(Module * pModule, ThreadLocalModule * pThreadLocalModule);
    OBJECTHANDLE AllocateStaticFieldObjRefPtrs(int nRequested, OBJECTHANDLE* ppLazyAllocate = NULL);
    void InitThreadStaticHandleTable();

    void AllocateThreadStaticBoxes(MethodTable* pMT);
#endif

public: // used by code generators
    static SIZE_T GetOffsetOfModuleSlotsPointer() { return offsetof(ThreadLocalBlock, m_pTLMTable); }

public:

#ifndef DACCESS_COMPILE
    ThreadLocalBlock()
      : m_pTLMTable(NULL), m_TLMTableSize(0), m_pThreadStaticHandleTable(NULL) 
    {
        m_TLMTableLock.Init(LOCK_TYPE_DEFAULT);
    }

    void    FreeTLM(SIZE_T i, BOOL isThreadShuttingDown);

    void    FreeTable();

    void    EnsureModuleIndex(ModuleIndex index);

#endif

    void SetModuleSlot(ModuleIndex index, PTR_ThreadLocalModule pLocalModule);

    PTR_ThreadLocalModule GetTLMIfExists(ModuleIndex index);
    PTR_ThreadLocalModule GetTLMIfExists(MethodTable* pMT);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

#ifdef CROSSGEN_COMPILE

#include "asmconstants.h"

class Thread
{
    friend class ThreadStatics;

    ThreadLocalBlock m_ThreadLocalBlock;

public:
    BOOL IsAddressInStack (PTR_VOID addr) const { return TRUE; }
    static BOOL IsAddressInCurrentStack (PTR_VOID addr) { return TRUE; }

    StackingAllocator*    m_stackLocalAllocator = NULL;
    bool CheckCanUseStackAlloc() { return true; }

 private:
    LoadLevelLimiter *m_pLoadLimiter;

 public:
    LoadLevelLimiter *GetLoadLevelLimiter()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoadLimiter;
    }

    void SetLoadLevelLimiter(LoadLevelLimiter *limiter)
    {
        LIMITED_METHOD_CONTRACT;
        m_pLoadLimiter = limiter;
    }

    PTR_Frame GetFrame() { return NULL; }
    void SetFrame(Frame *pFrame) { }
    DWORD CatchAtSafePoint() { return 0; }
    DWORD CatchAtSafePointOpportunistic() { return 0; }

    static void ObjectRefProtected(const OBJECTREF* ref) { }
    static void ObjectRefNew(const OBJECTREF* ref) { }

    void EnablePreemptiveGC() { }
    void DisablePreemptiveGC() { }

    inline void IncLockCount() { }
    inline void DecLockCount() { }

    static LPVOID GetStaticFieldAddress(FieldDesc *pFD) { return NULL; }

    PTR_AppDomain GetDomain() { return ::GetAppDomain(); }

    DWORD GetThreadId() { return 0; }

    inline DWORD GetOverridesCount() { return 0; }
    inline BOOL CheckThreadWideSpecialFlag(DWORD flags) { return 0; }

    BOOL PreemptiveGCDisabled() { return false; }
    void PulseGCMode() { }

    OBJECTREF GetThrowable() { return NULL; }

    OBJECTREF LastThrownObject() { return NULL; }

    static BOOL Debug_AllowCallout() { return TRUE; }

    static void IncForbidSuspendThread() { }
    static void DecForbidSuspendThread() { }

    typedef StateHolder<Thread::IncForbidSuspendThread, Thread::DecForbidSuspendThread> ForbidSuspendThreadHolder;

    static BYTE GetOffsetOfCurrentFrame()
    {
        LIMITED_METHOD_CONTRACT;
        size_t ofs = Thread_m_pFrame;
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    static BYTE GetOffsetOfGCFlag()
    {
        LIMITED_METHOD_CONTRACT;
        size_t ofs = Thread_m_fPreemptiveGCDisabled;
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    void SetLoadingFile(DomainFile *pFile)
    {
    }

    typedef Holder<Thread *, DoNothing, DoNothing> LoadingFileHolder;

    enum ThreadState
    {
    };

    BOOL HasThreadState(ThreadState ts)
    {
        LIMITED_METHOD_CONTRACT;
        return ((DWORD)m_State & ts);
    }

    BOOL HasThreadStateOpportunistic(ThreadState ts)
    {
        LIMITED_METHOD_CONTRACT;
        return m_State.LoadWithoutBarrier() & ts;
    }

    Volatile<ThreadState> m_State;

    enum ThreadStateNoConcurrency
    {
        TSNC_OwnsSpinLock               = 0x00000400, // The thread owns a spinlock.

        TSNC_DisableOleaut32Check       = 0x00040000, // Disable oleaut32 delay load check.  Oleaut32 has  
                                                      // been loaded

        TSNC_LoadsTypeViolation         = 0x40000000, // Use by type loader to break deadlocks caused by type load level ordering violations
    };

    ThreadStateNoConcurrency m_StateNC;

    void SetThreadStateNC(ThreadStateNoConcurrency tsnc)
    {
        LIMITED_METHOD_CONTRACT;
        m_StateNC = (ThreadStateNoConcurrency)((DWORD)m_StateNC | tsnc);
    }

    void ResetThreadStateNC(ThreadStateNoConcurrency tsnc)
    {
        LIMITED_METHOD_CONTRACT;
        m_StateNC = (ThreadStateNoConcurrency)((DWORD)m_StateNC & ~tsnc);
    }

    BOOL HasThreadStateNC(ThreadStateNoConcurrency tsnc)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return ((DWORD)m_StateNC & tsnc);
    }

    PendingTypeLoadHolder* m_pPendingTypeLoad;

#ifndef DACCESS_COMPILE
    PendingTypeLoadHolder* GetPendingTypeLoad()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pPendingTypeLoad;
    }

    void SetPendingTypeLoad(PendingTypeLoadHolder* pPendingTypeLoad)
    {
        LIMITED_METHOD_CONTRACT;
        m_pPendingTypeLoad = pPendingTypeLoad;
    }
#endif
    void SetProfilerCallbackFullState(DWORD dwFullState)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
    DWORD SetProfilerCallbackStateFlags(DWORD dwFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return dwFlags;
    }

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    enum ApartmentState { AS_Unknown };
#endif

    DWORD       m_dwLastError;
};

class AVInRuntimeImplOkayHolder
{
public:
    AVInRuntimeImplOkayHolder()
    {
        LIMITED_METHOD_CONTRACT;
    }
    AVInRuntimeImplOkayHolder(Thread * pThread)
    {
        LIMITED_METHOD_CONTRACT;
    }
    ~AVInRuntimeImplOkayHolder()
    {
        LIMITED_METHOD_CONTRACT;
    }
};

inline BOOL dbgOnly_IsSpecialEEThread() { return FALSE; }

#define INCTHREADLOCKCOUNT() { }
#define DECTHREADLOCKCOUNT() { }
#define INCTHREADLOCKCOUNTTHREAD(thread) { }
#define DECTHREADLOCKCOUNTTHREAD(thread) { }

#define FORBIDGC_LOADER_USE_ENABLED() false
#define ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE()    ;

#define BEGIN_FORBID_TYPELOAD()
#define END_FORBID_TYPELOAD()
#define TRIGGERS_TYPELOAD()

#define TRIGGERSGC() ANNOTATION_GC_TRIGGERS

inline void CommonTripThread() { }

class DeadlockAwareLock
{
public:
    DeadlockAwareLock(const char *description = NULL) { }
    ~DeadlockAwareLock() { }

    BOOL CanEnterLock() { return TRUE; }

    BOOL TryBeginEnterLock() { return TRUE; }
    void BeginEnterLock() { }

    void EndEnterLock() { }

    void LeaveLock() { }

public:
    typedef StateHolder<DoNothing,DoNothing> BlockingLockHolder;
};

// Do not include threads.inl
#define _THREADS_INL

typedef Thread::ForbidSuspendThreadHolder ForbidSuspendThreadHolder;

#else // CROSSGEN_COMPILE

#ifdef _TARGET_ARM_
#include "armsinglestepper.h"
#endif

#if !defined(PLATFORM_SUPPORTS_SAFE_THREADSUSPEND)
// DISABLE_THREADSUSPEND controls whether Thread::SuspendThread will be used at all.  
//   This API is dangerous on non-Windows platforms, as it can lead to deadlocks, 
//   due to low level OS resources that the PAL is not aware of, or due to the fact that 
//   PAL-unaware code in the process may hold onto some OS resources.
#define DISABLE_THREADSUSPEND
#endif

// NT thread priorities range from -15 to +15.
#define INVALID_THREAD_PRIORITY  ((DWORD)0x80000000)

// For a fiber which switched out, we set its OSID to a special number
// Note: there's a copy of this macro in strike.cpp
#define SWITCHED_OUT_FIBER_OSID 0xbaadf00d;

#ifdef _DEBUG
// A thread doesn't recieve its id until fully constructed.
#define UNINITIALIZED_THREADID 0xbaadf00d
#endif //_DEBUG

// Capture all the synchronization requests, for debugging purposes
#if defined(_DEBUG) && defined(TRACK_SYNC)

// Each thread has a stack that tracks all enter and leave requests
struct Dbg_TrackSync
{
    virtual ~Dbg_TrackSync() = default;

    virtual void EnterSync    (UINT_PTR caller, void *pAwareLock) = 0;
    virtual void LeaveSync    (UINT_PTR caller, void *pAwareLock) = 0;
};

EXTERN_C void EnterSyncHelper    (UINT_PTR caller, void *pAwareLock);
EXTERN_C void LeaveSyncHelper    (UINT_PTR caller, void *pAwareLock);

#endif  // TRACK_SYNC

//***************************************************************************
#ifdef FEATURE_HIJACK

// Used to capture information about the state of execution of a *SUSPENDED* thread.
struct ExecutionState;

#ifndef PLATFORM_UNIX
// This is the type of the start function of a redirected thread pulled from
// a HandledJITCase during runtime suspension
typedef void (__stdcall *PFN_REDIRECTTARGET)();

// Describes the weird argument sets during hijacking
struct HijackArgs;
#endif // !PLATFORM_UNIX

#endif // FEATURE_HIJACK

//***************************************************************************
#ifdef ENABLE_CONTRACTS_IMPL
inline Thread* GetThreadNULLOk()
{
    LIMITED_METHOD_CONTRACT;
    Thread * pThread;
    BEGIN_GETTHREAD_ALLOWED_IN_NO_THROW_REGION;
    pThread = GetThread();
    END_GETTHREAD_ALLOWED_IN_NO_THROW_REGION;
    return pThread;
}
#else
#define GetThreadNULLOk() GetThread()
#endif

// manifest constant for waiting in the exposed classlibs
const INT32 INFINITE_TIMEOUT = -1;

/***************************************************************************/
// Public enum shared between thread and threadpool
// These are two kinds of threadpool thread that the threadpool mgr needs
// to keep track of
enum ThreadpoolThreadType
{
    WorkerThread,
    CompletionPortThread,
    WaitThread,
    TimerMgrThread
};
//***************************************************************************
// Public functions
//
//      Thread* GetThread()             - returns current Thread
//      Thread* SetupThread()           - creates new Thread.
//      Thread* SetupUnstartedThread()  - creates new unstarted Thread which
//                                        (obviously) isn't in a TLS.
//      void    DestroyThread()         - the underlying logical thread is going
//                                        away.
//      void    DetachThread()          - the underlying logical thread is going
//                                        away but we don't want to destroy it yet.
//
// Public functions for ASM code generators
//
//      Thread* __stdcall CreateThreadBlockThrow() - creates new Thread on reverse p-invoke
//
// Public functions for one-time init/cleanup
//
//      void InitThreadManager()      - onetime init
//      void TerminateThreadManager() - onetime cleanup
//
// Public functions for taking control of a thread at a safe point
//
//      VOID OnHijackTripThread() - we've hijacked a JIT method
//      VOID OnHijackFPTripThread() - we've hijacked a JIT method, 
//                                    and need to save the x87 FP stack.
//
//***************************************************************************


//***************************************************************************
// Public functions
//***************************************************************************

//---------------------------------------------------------------------------
//
//---------------------------------------------------------------------------
Thread* SetupThread(BOOL fInternal);
inline Thread* SetupThread()
{
    WRAPPER_NO_CONTRACT;
    return SetupThread(FALSE);
}
// A host can deny a thread entering runtime by returning a NULL IHostTask.
// But we do want threads used by threadpool.
inline Thread* SetupInternalThread()
{
    WRAPPER_NO_CONTRACT;
    return SetupThread(TRUE);
}
Thread* SetupThreadNoThrow(HRESULT *phresult = NULL);
// WARNING : only GC calls this with bRequiresTSL set to FALSE.
Thread* SetupUnstartedThread(BOOL bRequiresTSL=TRUE);
void    DestroyThread(Thread *th);

DWORD GetRuntimeId();

EXTERN_C Thread* WINAPI CreateThreadBlockThrow();

//---------------------------------------------------------------------------
// One-time initialization. Called during Dll initialization.
//---------------------------------------------------------------------------
void InitThreadManager();


// When we want to take control of a thread at a safe point, the thread will
// eventually come back to us in one of the following trip functions:

#ifdef FEATURE_HIJACK

EXTERN_C void WINAPI OnHijackTripThread();
#ifdef _TARGET_X86_
EXTERN_C void WINAPI OnHijackFPTripThread();  // hijacked JIT code is returning an FP value
#endif // _TARGET_X86_

#endif // FEATURE_HIJACK

void CommonTripThread();

// When we resume a thread at a new location, to get an exception thrown, we have to
// pretend the exception originated elsewhere.
EXTERN_C void ThrowControlForThread(
#ifdef WIN64EXCEPTIONS
        FaultingExceptionFrame *pfef
#endif // WIN64EXCEPTIONS
        );

// RWLock state inside TLS
struct LockEntry
{
    LockEntry *pNext;    // next entry
    LockEntry *pPrev;    // prev entry
    LONG dwULockID;
    LONG dwLLockID;         // owning lock
    WORD wReaderLevel;      // reader nesting level
};

#if defined(_DEBUG)
BOOL MatchThreadHandleToOsId ( HANDLE h, DWORD osId );
#endif

#ifdef FEATURE_COMINTEROP

#define RCW_STACK_SIZE 64

class RCWStack
{
public:
    inline RCWStack()
    {
        LIMITED_METHOD_CONTRACT;
        memset(this, 0, sizeof(RCWStack));
    }

    inline VOID SetEntry(unsigned int index, RCW* pRCW)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(index < RCW_STACK_SIZE);
            PRECONDITION(CheckPointer(pRCW, NULL_OK));
        }
        CONTRACTL_END;

        m_pList[index] = pRCW;
    }

    inline RCW* GetEntry(unsigned int index)
    {
        CONTRACT (RCW*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(index < RCW_STACK_SIZE);
        }
        CONTRACT_END;

        RETURN m_pList[index];
    }

    inline VOID SetNextStack(RCWStack* pStack)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pStack));
            PRECONDITION(m_pNext == NULL);
        }
        CONTRACTL_END;

        m_pNext = pStack;
    }

    inline RCWStack* GetNextStack()
    {
        CONTRACT (RCWStack*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RETURN m_pNext;
    }

private:
    RCWStack*   m_pNext;
    RCW*        m_pList[RCW_STACK_SIZE];
};


class RCWStackHeader
{
public:
    RCWStackHeader()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_iIndex = 0;
        m_iSize = RCW_STACK_SIZE;
        m_pHead = new RCWStack();
    }

    ~RCWStackHeader()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        RCWStack* pStack = m_pHead;
        RCWStack* pNextStack = NULL;

        while (pStack)
        {
            pNextStack = pStack->GetNextStack();
            delete pStack;
            pStack = pNextStack;
        }
    }

    bool Push(RCW* pRCW)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pRCW, NULL_OK));
        }
        CONTRACTL_END;

        if (!GrowListIfNeeded())
            return false;

        // Fast Path
        if (m_iIndex < RCW_STACK_SIZE)
        {
            m_pHead->SetEntry(m_iIndex, pRCW);
            m_iIndex++;
            return true;
        }

        // Slow Path
        unsigned int count = m_iIndex;
        RCWStack* pStack = m_pHead;
        while (count >= RCW_STACK_SIZE)
        {
            pStack = pStack->GetNextStack();
            _ASSERTE(pStack);

            count -= RCW_STACK_SIZE;
        }

        pStack->SetEntry(count, pRCW);
        m_iIndex++;
        return true;
    }

    RCW* Pop()
    {
        CONTRACT (RCW*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_iIndex > 0);
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        RCW* pRCW = NULL;

        m_iIndex--;

        // Fast Path
        if (m_iIndex < RCW_STACK_SIZE)
        {
            pRCW = m_pHead->GetEntry(m_iIndex);
            m_pHead->SetEntry(m_iIndex, NULL);
            RETURN pRCW;
        }

        // Slow Path
        unsigned int count = m_iIndex;
        RCWStack* pStack = m_pHead;
        while (count >= RCW_STACK_SIZE)
        {
            pStack = pStack->GetNextStack();
            _ASSERTE(pStack);
            count -= RCW_STACK_SIZE;
        }

        pRCW = pStack->GetEntry(count);
        pStack->SetEntry(count, NULL);

        RETURN pRCW;
    }

    BOOL IsInStack(RCW* pRCW)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pRCW));
        }
        CONTRACTL_END;

        if (m_iIndex == 0)
            return FALSE;

        // Fast Path
        if (m_iIndex <= RCW_STACK_SIZE)
        {
            for (int i = 0; i < (int)m_iIndex; i++)
            {
                if (pRCW == m_pHead->GetEntry(i))
                    return TRUE;
            }

            return FALSE;
        }

        // Slow Path
        RCWStack* pStack = m_pHead;
        int totalcount = 0;
        while (pStack != NULL)
        {
            for (int i = 0; (i < RCW_STACK_SIZE) && (totalcount < m_iIndex); i++, totalcount++)
            {
                if (pRCW == pStack->GetEntry(i))
                    return TRUE;
            }

            pStack = pStack->GetNextStack();
        }

        return FALSE;
    }

private:
    bool GrowListIfNeeded()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM());
            PRECONDITION(CheckPointer(m_pHead));
        }
        CONTRACTL_END;

        if (m_iIndex == m_iSize)
        {
            RCWStack* pStack = m_pHead;
            RCWStack* pNextStack = NULL;
            while ( (pNextStack = pStack->GetNextStack()) != NULL)
                pStack = pNextStack;

            RCWStack* pNewStack = new (nothrow) RCWStack();
            if (NULL == pNewStack)
                return false;

            pStack->SetNextStack(pNewStack);

            m_iSize += RCW_STACK_SIZE;
        }

        return true;
    }

    // Zero-based index to the first free element in the list.
    int        m_iIndex;

    // Total size of the list, including all stacks.
    int        m_iSize;

    // Pointer to the first stack.
    RCWStack*           m_pHead;
};

#endif // FEATURE_COMINTEROP


typedef DWORD (*AppropriateWaitFunc) (void *args, DWORD timeout, DWORD option);

// The Thread class represents a managed thread.  This thread could be internal
// or external (i.e. it wandered in from outside the runtime).  For internal
// threads, it could correspond to an exposed System.Thread object or it
// could correspond to an internal worker thread of the runtime.
//
// If there's a physical Win32 thread underneath this object (i.e. it isn't an
// unstarted System.Thread), then this instance can be found in the TLS
// of that physical thread.

// FEATURE_MULTIREG_RETURN is set for platforms where a struct return value 
// [GcInfo v2 only]        can be returned in multiple registers
//                         ex: Windows/Unix ARM/ARM64, Unix-AMD64.
//                         
//                       
// UNIX_AMD64_ABI is a specific kind of FEATURE_MULTIREG_RETURN
// [GcInfo v1 and v2]       specified by SystemV ABI for AMD64
//                                   

#ifdef FEATURE_HIJACK                                                    // Hijack function returning
EXTERN_C void STDCALL OnHijackWorker(HijackArgs * pArgs);              
#endif // FEATURE_HIJACK

// This is the code we pass around for Thread.Interrupt, mainly for assertions
#define APC_Code    0xEECEECEE

#ifdef DACCESS_COMPILE
class BaseStackGuard;
#endif

// #ThreadClass
// 
// A code:Thread contains all the per-thread information needed by the runtime.  You can get at this
// structure throught the and OS TLS slot see code:#RuntimeThreadLocals for more 
// Implementing IUnknown would prevent the field (e.g. m_Context) layout from being rearranged (which will need to be fixed in 
// "asmconstants.h" for the respective architecture). As it is, ICLRTask derives from IUnknown and would have got IUnknown implemented
// here - so doing this explicitly and maintaining layout sanity should be just fine.
class Thread: public IUnknown
{
    friend struct ThreadQueue;  // used to enqueue & dequeue threads onto SyncBlocks
    friend class  ThreadStore;
    friend class  ThreadSuspend;
    friend class  SyncBlock;
    friend struct PendingSync;
    friend class  AppDomain;
    friend class  ThreadNative;
    friend class  DeadlockAwareLock;
#ifdef _DEBUG
    friend class  EEContract;
#endif
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
    friend class ClrDataTask;
#endif

    friend BOOL NTGetThreadContext(Thread *pThread, T_CONTEXT *pContext);
    friend BOOL NTSetThreadContext(Thread *pThread, const T_CONTEXT *pContext);

    friend void CommonTripThread();

#ifdef FEATURE_HIJACK
    // MapWin32FaultToCOMPlusException needs access to Thread::IsAddrOfRedirectFunc()
    friend DWORD MapWin32FaultToCOMPlusException(EXCEPTION_RECORD *pExceptionRecord);
    friend void STDCALL OnHijackWorker(HijackArgs * pArgs);
#ifdef PLATFORM_UNIX
    friend void HandleGCSuspensionForInterruptedThread(CONTEXT *interruptedContext);
#endif // PLATFORM_UNIX

#endif // FEATURE_HIJACK

    friend void         InitThreadManager();
    friend void         ThreadBaseObject::SetDelegate(OBJECTREF delegate);

    friend void CallFinalizerOnThreadObject(Object *obj);

    friend class ContextTransitionFrame;  // To set m_dwBeginLockCount

    // Debug and Profiler caches ThreadHandle.
    friend class Debugger;                  // void Debugger::ThreadStarted(Thread* pRuntimeThread, BOOL fAttaching);
#if defined(DACCESS_COMPILE)
    friend class DacDbiInterfaceImpl;       // DacDbiInterfaceImpl::GetThreadHandle(HANDLE * phThread);
#endif // DACCESS_COMPILE
    friend class ProfToEEInterfaceImpl;     // HRESULT ProfToEEInterfaceImpl::GetHandleFromThread(ThreadID threadId, HANDLE *phThread);
    friend class CExecutionEngine;
    friend class UnC;
    friend class CheckAsmOffsets;

    friend class ExceptionTracker;
    friend class ThreadExceptionState;

    friend class StackFrameIterator;

    friend class ThreadStatics;

    VPTR_BASE_CONCRETE_VTABLE_CLASS(Thread)

public:
    enum SetThreadStackGuaranteeScope { STSGuarantee_Force, STSGuarantee_OnlyIfEnabled };
    static BOOL IsSetThreadStackGuaranteeInUse(SetThreadStackGuaranteeScope fScope = STSGuarantee_OnlyIfEnabled)
    {
        WRAPPER_NO_CONTRACT;

        if(STSGuarantee_Force == fScope)
            return TRUE;

        //The runtime must be hosted to have escalation policy
        //If escalation policy is enabled but StackOverflow is not part of the policy
        //   then we don't use SetThreadStackGuarantee 
        if(!CLRHosted() || 
            GetEEPolicy()->GetActionOnFailure(FAIL_StackOverflow) == eRudeExitProcess)
        {
            //FAIL_StackOverflow is ProcessExit so don't use SetThreadStackGuarantee
            return FALSE;
        }
        return TRUE;
    }

public:

    // If we are trying to suspend a thread, we set the appropriate pending bit to
    // indicate why we want to suspend it (TS_GCSuspendPending, TS_UserSuspendPending,
    // TS_DebugSuspendPending).
    //
    // If instead the thread has blocked itself, via WaitSuspendEvent, we indicate
    // this with TS_SyncSuspended.  However, we need to know whether the synchronous
    // suspension is for a user request, or for an internal one (GC & Debug).  That's
    // because a user request is not allowed to resume a thread suspended for
    // debugging or GC.  -- That's not stricly true.  It is allowed to resume such a
    // thread so long as it was ALSO suspended by the user.  In other words, this
    // ensures that user resumptions aren't unbalanced from user suspensions.
    //
    enum ThreadState
    {
        TS_Unknown                = 0x00000000,    // threads are initialized this way

        TS_AbortRequested         = 0x00000001,    // Abort the thread
        TS_GCSuspendPending       = 0x00000002,    // waiting to get to safe spot for GC
        TS_UserSuspendPending     = 0x00000004,    // user suspension at next opportunity
        TS_DebugSuspendPending    = 0x00000008,    // Is the debugger suspending threads?
        TS_GCOnTransitions        = 0x00000010,    // Force a GC on stub transitions (GCStress only)

        TS_LegalToJoin            = 0x00000020,    // Is it now legal to attempt a Join()

        // unused                 = 0x00000040,

#ifdef FEATURE_HIJACK
        TS_Hijacked               = 0x00000080,    // Return address has been hijacked
#endif // FEATURE_HIJACK

        TS_BlockGCForSO           = 0x00000100,    // If a thread does not have enough stack, WaitUntilGCComplete may fail.
                                                   // Either GC suspension will wait until the thread has cleared this bit,
                                                   // Or the current thread is going to spin if GC has suspended all threads.
        TS_Background             = 0x00000200,    // Thread is a background thread
        TS_Unstarted              = 0x00000400,    // Thread has never been started
        TS_Dead                   = 0x00000800,    // Thread is dead

        TS_WeOwn                  = 0x00001000,    // Exposed object initiated this thread
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
        TS_CoInitialized          = 0x00002000,    // CoInitialize has been called for this thread

        TS_InSTA                  = 0x00004000,    // Thread hosts an STA
        TS_InMTA                  = 0x00008000,    // Thread is part of the MTA
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        // Some bits that only have meaning for reporting the state to clients.
        TS_ReportDead             = 0x00010000,    // in WaitForOtherThreads()
        TS_FullyInitialized       = 0x00020000,    // Thread is fully initialized and we are ready to broadcast its existence to external clients

        TS_TaskReset              = 0x00040000,    // The task is reset

        TS_SyncSuspended          = 0x00080000,    // Suspended via WaitSuspendEvent
        TS_DebugWillSync          = 0x00100000,    // Debugger will wait for this thread to sync

        TS_StackCrawlNeeded       = 0x00200000,    // A stackcrawl is needed on this thread, such as for thread abort
                                                   // See comment for s_pWaitForStackCrawlEvent for reason.

        TS_SuspendUnstarted       = 0x00400000,    // latch a user suspension on an unstarted thread

        TS_Aborted                = 0x00800000,    // is the thread aborted?
        TS_TPWorkerThread         = 0x01000000,    // is this a threadpool worker thread?

        TS_Interruptible          = 0x02000000,    // sitting in a Sleep(), Wait(), Join()
        TS_Interrupted            = 0x04000000,    // was awakened by an interrupt APC. !!! This can be moved to TSNC

        TS_CompletionPortThread   = 0x08000000,    // Completion port thread

        TS_AbortInitiated         = 0x10000000,    // set when abort is begun

        TS_Finalized              = 0x20000000,    // The associated managed Thread object has been finalized.
                                                   // We can clean up the unmanaged part now.

        TS_FailStarted            = 0x40000000,    // The thread fails during startup.
        TS_Detached               = 0x80000000,    // Thread was detached by DllMain

        // <TODO> @TODO: We need to reclaim the bits that have no concurrency issues (i.e. they are only
        //         manipulated by the owning thread) and move them off to a different DWORD.  Note if this
        //         enum is changed, we also need to update SOS to reflect this.</TODO>

        // We require (and assert) that the following bits are less than 0x100.
        TS_CatchAtSafePoint = (TS_UserSuspendPending | TS_AbortRequested |
                               TS_GCSuspendPending | TS_DebugSuspendPending | TS_GCOnTransitions),
    };

    // Thread flags that aren't really states in themselves but rather things the thread
    // has to do.
    enum ThreadTasks
    {
        TT_CleanupSyncBlock       = 0x00000001, // The synch block needs to be cleaned up.
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
        TT_CallCoInitialize       = 0x00000002, // CoInitialize needs to be called.
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
    };

    // Thread flags that have no concurrency issues (i.e., they are only manipulated by the owning thread). Use these
    // state flags when you have a new thread state that doesn't belong in the ThreadState enum above.
    //
    // <TODO>@TODO: its possible that the ThreadTasks from above and these flags should be merged.</TODO>
    enum ThreadStateNoConcurrency
    {
        TSNC_Unknown                    = 0x00000000, // threads are initialized this way

        TSNC_DebuggerUserSuspend        = 0x00000001, // marked "suspended" by the debugger
        TSNC_DebuggerReAbort            = 0x00000002, // thread needs to re-abort itself when resumed by the debugger
        TSNC_DebuggerIsStepping         = 0x00000004, // debugger is stepping this thread
        TSNC_DebuggerIsManagedException = 0x00000008, // EH is re-raising a managed exception.
        TSNC_WaitUntilGCFinished        = 0x00000010, // The current thread is waiting for GC.  If host returns
                                                      // SO during wait, we will either spin or make GC wait.
        TSNC_BlockedForShutdown         = 0x00000020, // Thread is blocked in WaitForEndOfShutdown.  We should not hit WaitForEndOfShutdown again.
        TSNC_SOWorkNeeded               = 0x00000040, // The thread needs to wake up AD unload helper thread to finish SO work
        TSNC_CLRCreatedThread           = 0x00000080, // The thread was created through Thread::CreateNewThread
        TSNC_ExistInThreadStore         = 0x00000100, // For dtor to know if it needs to be removed from ThreadStore
        TSNC_UnsafeSkipEnterCooperative = 0x00000200, // This is a "fix" for deadlocks caused when cleaning up COM
        TSNC_OwnsSpinLock               = 0x00000400, // The thread owns a spinlock.
        TSNC_PreparingAbort             = 0x00000800, // Preparing abort.  This avoids recursive HandleThreadAbort call.
        TSNC_OSAlertableWait            = 0x00001000, // Preparing abort.  This avoids recursive HandleThreadAbort call.
        // unused                       = 0x00002000,
        TSNC_CreatingTypeInitException  = 0x00004000, // Thread is trying to create a TypeInitException
        // unused                       = 0x00008000,
        // unused                       = 0x00010000,
        TSNC_InRestoringSyncBlock       = 0x00020000, // The thread is restoring its SyncBlock for Object.Wait.
                                                      // After the thread is interrupted once, we turn off interruption
                                                      // at the beginning of wait.
        TSNC_DisableOleaut32Check       = 0x00040000, // Disable oleaut32 delay load check.  Oleaut32 has  
                                                      // been loaded
        TSNC_CannotRecycle              = 0x00080000, // A host can not recycle this Thread object.  When a thread
                                                      // has orphaned lock, we will apply this.
        TSNC_RaiseUnloadEvent           = 0x00100000, // Finalize thread is raising managed unload event which 
                                                      // may call AppDomain.Unload.
        TSNC_UnbalancedLocks            = 0x00200000, // Do not rely on lock accounting for this thread:
                                                      // we left an app domain with a lock count different from
                                                      // when we entered it
        // unused                       = 0x00400000,
        TSNC_IgnoreUnhandledExceptions  = 0x00800000, // Set for a managed thread born inside an appdomain created with the APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS flag.
        TSNC_ProcessedUnhandledException = 0x01000000,// Set on a thread on which we have done unhandled exception processing so that
                                                      // we dont perform it again when OS invokes our UEF. Currently, applicable threads include:
                                                      // 1) entry point thread of a managed app 
                                                      // 2) new managed thread created in default domain
                                                      //
                                                      // For such threads, we will return to the OS after our UE processing is done
                                                      // and the OS will start invoking the UEFs. If our UEF gets invoked, it will try to 
                                                      // perform the UE processing again. We will use this flag to prevent the duplicated
                                                      // effort.
                                                      // 
                                                      // Once we are completely independent of the OS UEF, we could remove this.
        TSNC_InsideSyncContextWait      = 0x02000000, // Whether we are inside DoSyncContextWait
        TSNC_DebuggerSleepWaitJoin      = 0x04000000, // Indicates to the debugger that this thread is in a sleep wait or join state
                                                      // This almost mirrors the TS_Interruptible state however that flag can change
                                                      // during GC-preemptive mode whereas this one cannot.
#ifdef FEATURE_COMINTEROP
        TSNC_WinRTInitialized           = 0x08000000, // the thread has initialized WinRT
#endif // FEATURE_COMINTEROP

        // TSNC_Unused                  = 0x10000000,

        TSNC_CallingManagedCodeDisabled = 0x20000000, // Use by multicore JIT feature to asert on calling managed code/loading module in background thread
                                                      // Exception, system module is allowed, security demand is allowed
        
        TSNC_LoadsTypeViolation         = 0x40000000, // Use by type loader to break deadlocks caused by type load level ordering violations

        TSNC_EtwStackWalkInProgress     = 0x80000000, // Set on the thread so that ETW can know that stackwalking is in progress
                                                      // and does not proceed with a stackwalk on the same thread
                                                      // There are cases during managed debugging when we can run into this situation
    };

    // Functions called by host
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv)
        DAC_EMPTY_RET(E_NOINTERFACE);
    STDMETHODIMP_(ULONG) AddRef(void)
        DAC_EMPTY_RET(0);
    STDMETHODIMP_(ULONG) Release(void)
        DAC_EMPTY_RET(0);
    STDMETHODIMP Abort()
        DAC_EMPTY_RET(E_FAIL);
    STDMETHODIMP RudeAbort()
        DAC_EMPTY_RET(E_FAIL);
    STDMETHODIMP NeedsPriorityScheduling(BOOL *pbNeedsPriorityScheduling)
        DAC_EMPTY_RET(E_FAIL);

    STDMETHODIMP YieldTask()
        DAC_EMPTY_RET(E_FAIL);
    STDMETHODIMP LocksHeld(SIZE_T *pLockCount)
        DAC_EMPTY_RET(E_FAIL);

    STDMETHODIMP BeginPreventAsyncAbort()
        DAC_EMPTY_RET(E_FAIL);
    STDMETHODIMP EndPreventAsyncAbort()
        DAC_EMPTY_RET(E_FAIL);

    void InternalReset (BOOL fNotFinalizerThread=FALSE, BOOL fThreadObjectResetNeeded=TRUE, BOOL fResetAbort=TRUE);
    INT32 ResetManagedThreadObject(INT32 nPriority); 
    INT32 ResetManagedThreadObjectInCoopMode(INT32 nPriority);
    BOOL  IsRealThreadPoolResetNeeded();
public:
    HRESULT DetachThread(BOOL fDLLThreadDetach);

    void SetThreadState(ThreadState ts)
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr((DWORD*)&m_State, ts);
    }

    void ResetThreadState(ThreadState ts)
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd((DWORD*)&m_State, ~ts);
    }

    BOOL HasThreadState(ThreadState ts)
    {
        LIMITED_METHOD_CONTRACT;
        return ((DWORD)m_State & ts);
    }

    //
    // This is meant to be used for quick opportunistic checks for thread abort and similar conditions. This method 
    // does not erect memory barrier and so it may return wrong result sometime that the caller has to handle.
    //
    BOOL HasThreadStateOpportunistic(ThreadState ts)
    {
        LIMITED_METHOD_CONTRACT;
        return m_State.LoadWithoutBarrier() & ts;
    }

    void SetThreadStateNC(ThreadStateNoConcurrency tsnc)
    {
        LIMITED_METHOD_CONTRACT;
        m_StateNC = (ThreadStateNoConcurrency)((DWORD)m_StateNC | tsnc);
    }

    void ResetThreadStateNC(ThreadStateNoConcurrency tsnc)
    {
        LIMITED_METHOD_CONTRACT;
        m_StateNC = (ThreadStateNoConcurrency)((DWORD)m_StateNC & ~tsnc);
    }

    BOOL HasThreadStateNC(ThreadStateNoConcurrency tsnc)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return ((DWORD)m_StateNC & tsnc);
    }

    void MarkEtwStackWalkInProgress()
    {
        WRAPPER_NO_CONTRACT;
        SetThreadStateNC(Thread::TSNC_EtwStackWalkInProgress);
    }

    void MarkEtwStackWalkCompleted()
    {
        WRAPPER_NO_CONTRACT;
        ResetThreadStateNC(Thread::TSNC_EtwStackWalkInProgress);
    }

    BOOL IsEtwStackWalkInProgress()
    {
        WRAPPER_NO_CONTRACT;
        return HasThreadStateNC(Thread::TSNC_EtwStackWalkInProgress);
    }

    DWORD RequireSyncBlockCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_ThreadTasks & TT_CleanupSyncBlock);
    }

    void SetSyncBlockCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr((ULONG *)&m_ThreadTasks, TT_CleanupSyncBlock);
    }

    void ResetSyncBlockCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd((ULONG *)&m_ThreadTasks, ~TT_CleanupSyncBlock);
    }

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    DWORD IsCoInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_CoInitialized);
    }

    void SetCoInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr((ULONG *)&m_State, TS_CoInitialized);
        FastInterlockAnd((ULONG*)&m_ThreadTasks, ~TT_CallCoInitialize);
    }

    void ResetCoInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd((ULONG *)&m_State,~TS_CoInitialized);
    }

#ifdef FEATURE_COMINTEROP
    BOOL IsWinRTInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        return HasThreadStateNC(TSNC_WinRTInitialized);
    }

    void ResetWinRTInitialized()
    {
        LIMITED_METHOD_CONTRACT;
        ResetThreadStateNC(TSNC_WinRTInitialized);
    }
#endif // FEATURE_COMINTEROP

    DWORD RequiresCoInitialize()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_ThreadTasks & TT_CallCoInitialize);
    }

    void SetRequiresCoInitialize()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockOr((ULONG *)&m_ThreadTasks, TT_CallCoInitialize);
    }

    void ResetRequiresCoInitialize()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd((ULONG *)&m_ThreadTasks,~TT_CallCoInitialize);
    }

    void CleanupCOMState();

#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_COMINTEROP
    bool IsDisableComObjectEagerCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fDisableComObjectEagerCleanup;
    }
    void SetDisableComObjectEagerCleanup()
    {
        LIMITED_METHOD_CONTRACT;
        m_fDisableComObjectEagerCleanup = true;
    }
#endif //FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
    bool HasDeadThreadBeenConsideredForGCTrigger()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsDead());

        return m_fHasDeadThreadBeenConsideredForGCTrigger;
    }

    void SetHasDeadThreadBeenConsideredForGCTrigger()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsDead());

        m_fHasDeadThreadBeenConsideredForGCTrigger = true;
    }
#endif // !DACCESS_COMPILE

    // returns if there is some extra work for the finalizer thread.
    BOOL HaveExtraWorkForFinalizer();

    // do the extra finalizer work.
    void DoExtraWorkForFinalizer();

#ifndef DACCESS_COMPILE
    DWORD CatchAtSafePoint()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_CatchAtSafePoint);
    }

    DWORD CatchAtSafePointOpportunistic()
    {
        LIMITED_METHOD_CONTRACT;
        return HasThreadStateOpportunistic(TS_CatchAtSafePoint);
    }
#endif // DACCESS_COMPILE

    DWORD IsBackground()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_Background);
    }

    DWORD IsUnstarted()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return (m_State & TS_Unstarted);
    }

    DWORD IsDead()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_Dead);
    }

    DWORD IsAborted()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_Aborted);
    }

    void SetAborted()
    {
        FastInterlockOr((ULONG *) &m_State, TS_Aborted);     
    }

    void ClearAborted()
    {
        FastInterlockAnd((ULONG *) &m_State, ~TS_Aborted);     
    }

    DWORD DoWeOwn()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_WeOwn);
    }

    // For reporting purposes, grab a consistent snapshot of the thread's state
    ThreadState GetSnapshotState();

    // For delayed destruction of threads
    DWORD           IsDetached()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_Detached);
    }

    static LONG     m_DetachCount;
    static LONG     m_ActiveDetachCount;  // Count how many non-background detached

    static Volatile<LONG>     m_threadsAtUnsafePlaces;

    // Offsets for the following variables need to fit in 1 byte, so keep near
    // the top of the object.  Also, we want cache line filling to work for us
    // so the critical stuff is ordered based on frequency of use.

    Volatile<ThreadState> m_State;   // Bits for the state of the thread

    // If TRUE, GC is scheduled cooperatively with this thread.
    // NOTE: This "byte" is actually a boolean - we don't allow
    // recursive disables.
    Volatile<ULONG>      m_fPreemptiveGCDisabled;

    PTR_Frame            m_pFrame;  // The Current Frame

    //-----------------------------------------------------------
    // If the thread has wandered in from the outside this is
    // its Domain.
    //-----------------------------------------------------------
    PTR_AppDomain       m_pDomain;

    // Track the number of locks (critical section, spin lock, syncblock lock,
    // EE Crst, GC lock) held by the current thread.
    DWORD                m_dwLockCount;

    // Unique thread id used for thin locks - kept as small as possible, as we have limited space
    // in the object header to store it.
    DWORD                m_ThreadId;


    // RWLock state
    LockEntry           *m_pHead;
    LockEntry            m_embeddedEntry;
    
#ifndef DACCESS_COMPILE
    Frame* NotifyFrameChainOfExceptionUnwind(Frame* pStartFrame, LPVOID pvLimitSP);
#endif // DACCESS_COMPILE

#if defined(FEATURE_COMINTEROP) && !defined(DACCESS_COMPILE)
    void RegisterRCW(RCW *pRCW)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pRCW));
        }
        CONTRACTL_END;

        if (!m_pRCWStack->Push(pRCW))
        {
            ThrowOutOfMemory();
        }
    }

    // Returns false on OOM.
    BOOL RegisterRCWNoThrow(RCW *pRCW)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pRCW, NULL_OK));
        }
        CONTRACTL_END;

        return m_pRCWStack->Push(pRCW);
    }

    RCW *UnregisterRCW(INDEBUG(SyncBlock *pSB))
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pSB));
        }
        CONTRACTL_END;

        RCW* pPoppedRCW = m_pRCWStack->Pop();

#ifdef _DEBUG
        // The RCW we popped must be the one pointed to by pSB if pSB still points to an RCW.
        RCW* pCurrentRCW = pSB->GetInteropInfoNoCreate()->GetRawRCW();
        _ASSERTE(pCurrentRCW == NULL || pPoppedRCW == NULL || pCurrentRCW == pPoppedRCW);
#endif // _DEBUG

        return pPoppedRCW;
    }

    BOOL RCWIsInUse(RCW* pRCW)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pRCW));
        }
        CONTRACTL_END;

        return m_pRCWStack->IsInStack(pRCW);
    }
#endif // FEATURE_COMINTEROP && !DACCESS_COMPILE

    // Lock thread is trying to acquire
    VolatilePtr<DeadlockAwareLock> m_pBlockingLock;

public:

    // on MP systems, each thread has its own allocation chunk so we can avoid
    // lock prefixes and expensive MP cache snooping stuff
    gc_alloc_context        m_alloc_context;

    inline gc_alloc_context *GetAllocContext() { LIMITED_METHOD_CONTRACT; return &m_alloc_context; }

    // This is the type handle of the first object in the alloc context at the time 
    // we fire the AllocationTick event. It's only for tooling purpose.
    TypeHandle m_thAllocContextObj;

#ifndef FEATURE_PAL    
private:
    _NT_TIB *m_pTEB;
public:
    _NT_TIB *GetTEB() {
        LIMITED_METHOD_CONTRACT;
        return m_pTEB;
    }
    PEXCEPTION_REGISTRATION_RECORD *GetExceptionListPtr() {
        WRAPPER_NO_CONTRACT;
        return &GetTEB()->ExceptionList;
    }
#endif // !FEATURE_PAL
    
    inline void SetTHAllocContextObj(TypeHandle th) {LIMITED_METHOD_CONTRACT; m_thAllocContextObj = th; }
    
    inline TypeHandle GetTHAllocContextObj() {LIMITED_METHOD_CONTRACT; return m_thAllocContextObj; }

#ifdef FEATURE_COMINTEROP
    // The header for the per-thread in-use RCW stack.
    RCWStackHeader*      m_pRCWStack;
#endif // FEATURE_COMINTEROP

    // Allocator used during marshaling for temporary buffers, much faster than
    // heap allocation.
    //
    // Uses of this allocator should be effectively statically scoped, i.e. a "region"
    // is started using a CheckPointHolder and GetCheckpoint, and this region can then be used for allocations
    // from that point onwards, and then all memory is reclaimed when the static scope for the
    // checkpoint is exited by the running thread.
    StackingAllocator*    m_stackLocalAllocator = NULL;

    // Flags used to indicate tasks the thread has to do.
    ThreadTasks          m_ThreadTasks;

    // Flags for thread states that have no concurrency issues.
    ThreadStateNoConcurrency m_StateNC;

    inline void IncLockCount();
    inline void DecLockCount();

private:
    DWORD m_dwBeginLockCount;  // lock count when the thread enters current domain

#ifdef _DEBUG
    DWORD dbg_m_cSuspendedThreads;
    // Count of suspended threads that we know are not in native code (and therefore cannot hold OS lock which prevents us calling out to host)
    DWORD dbg_m_cSuspendedThreadsWithoutOSLock;
    EEThreadId m_Creater;
#endif

    // After we suspend a thread, we may need to call EEJitManager::JitCodeToMethodInfo
    // or StressLog which may waits on a spinlock.  It is unsafe to suspend a thread while it
    // is in this state.
    Volatile<LONG> m_dwForbidSuspendThread;
public:

    static void IncForbidSuspendThread()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;
#ifndef DACCESS_COMPILE
        Thread * pThread = GetThreadNULLOk();
        if (pThread)
        {
            _ASSERTE (pThread->m_dwForbidSuspendThread != (LONG)MAXLONG);
#ifdef _DEBUG
            {
                //DEBUG_ONLY;
            STRESS_LOG2(LF_SYNC, LL_INFO100000, "Set forbid suspend [%d] for thread %p.\n", pThread->m_dwForbidSuspendThread.Load(), pThread);
            }    
#endif
            FastInterlockIncrement(&pThread->m_dwForbidSuspendThread);
        }
#endif //!DACCESS_COMPILE
    }

    static void DecForbidSuspendThread()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;
#ifndef DACCESS_COMPILE
        Thread * pThread = GetThreadNULLOk();
        if (pThread)
        {
            _ASSERTE (pThread->m_dwForbidSuspendThread != (LONG)0);
            FastInterlockDecrement(&pThread->m_dwForbidSuspendThread);
#ifdef _DEBUG
            {
                //DEBUG_ONLY;
            STRESS_LOG2(LF_SYNC, LL_INFO100000, "Reset forbid suspend [%d] for thread %p.\n", pThread->m_dwForbidSuspendThread.Load(), pThread);
            }    
#endif
        }
#endif //!DACCESS_COMPILE
    }
    
    bool IsInForbidSuspendRegion()
    {
        return m_dwForbidSuspendThread != (LONG)0;
    }
    
    typedef StateHolder<Thread::IncForbidSuspendThread, Thread::DecForbidSuspendThread> ForbidSuspendThreadHolder;

private:
    // Per thread counter to dispense hash code - kept in the thread so we don't need a lock
    // or interlocked operations to get a new hash code;
    DWORD m_dwHashCodeSeed;

public:

    inline BOOL HasLockInCurrentDomain()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_dwLockCount >= m_dwBeginLockCount);

        // Equivalent to (m_dwLockCount != m_dwBeginLockCount ||
        //                m_dwCriticalRegionCount ! m_dwBeginCriticalRegionCount),
        // but without branching instructions
        BOOL fHasLock = (m_dwLockCount ^ m_dwBeginLockCount);

        return fHasLock; 
    }

    inline BOOL HasCriticalRegion()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;        
    }

    inline DWORD GetNewHashCode()
    {
        LIMITED_METHOD_CONTRACT;
        // Every thread has its own generator for hash codes so that we won't get into a situation
        // where two threads consistently give out the same hash codes.
        // Choice of multiplier guarantees period of 2**32 - see Knuth Vol 2 p16 (3.2.1.2 Theorem A).
        DWORD multiplier = GetThreadId()*4 + 5;
        m_dwHashCodeSeed = m_dwHashCodeSeed*multiplier + 1;
        return m_dwHashCodeSeed;
    }

#ifdef _DEBUG
    // If the current thread suspends other threads, we need to make sure that the thread
    // only allocates memory if the suspended threads do not have OS Heap lock.
    static BOOL Debug_AllowCallout()
    {
        LIMITED_METHOD_CONTRACT;
        Thread * pThread = GetThreadNULLOk();
        return ((pThread == NULL) || (pThread->dbg_m_cSuspendedThreads == pThread->dbg_m_cSuspendedThreadsWithoutOSLock));
    }
    
    // Returns number of threads that are currently suspended by the current thread and that can potentially hold OS lock
    BOOL Debug_GetUnsafeSuspendeeCount()
    {
        LIMITED_METHOD_CONTRACT;
        return (dbg_m_cSuspendedThreads - dbg_m_cSuspendedThreadsWithoutOSLock);
    }
#endif

public:

    BOOL HasThreadAffinity()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

 private:
    LoadLevelLimiter *m_pLoadLimiter;

 public:
    LoadLevelLimiter *GetLoadLevelLimiter()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoadLimiter;
    }

    void SetLoadLevelLimiter(LoadLevelLimiter *limiter)
    {
        LIMITED_METHOD_CONTRACT;
        m_pLoadLimiter = limiter;
    }



public:

    //--------------------------------------------------------------
    // Constructor.
    //--------------------------------------------------------------
#ifndef DACCESS_COMPILE
    Thread();
#endif

    //--------------------------------------------------------------
    // Failable initialization occurs here.
    //--------------------------------------------------------------
    BOOL InitThread(BOOL fInternal);
    BOOL AllocHandles();

    void SetupThreadForHost();

    //--------------------------------------------------------------
    // If the thread was setup through SetupUnstartedThread, rather
    // than SetupThread, complete the setup here when the thread is
    // actually running.
    // WARNING : only GC calls this with bRequiresTSL set to FALSE.
    //--------------------------------------------------------------
    BOOL HasStarted(BOOL bRequiresTSL=TRUE);

    // We don't want ::CreateThread() calls scattered throughout the source.
    // Create all new threads here.  The thread is created as suspended, so
    // you must ::ResumeThread to kick it off.  It is guaranteed to create the
    // thread, or throw.
    BOOL CreateNewThread(SIZE_T stackSize, LPTHREAD_START_ROUTINE start, void *args, LPCWSTR pName=NULL);


    enum StackSizeBucket
    {
        StackSize_Small,
        StackSize_Medium,
        StackSize_Large
    };

    //
    // Creates a raw OS thread; use this only for CLR-internal threads that never execute user code.
    // StackSizeBucket determines how large the stack should be.
    //
    static HANDLE CreateUtilityThread(StackSizeBucket stackSizeBucket, LPTHREAD_START_ROUTINE start, void *args, LPCWSTR pName, DWORD flags = 0, DWORD* pThreadId = NULL);

    //--------------------------------------------------------------
    // Destructor
    //--------------------------------------------------------------
#ifndef DACCESS_COMPILE
    virtual ~Thread();
#else    
    virtual ~Thread() {}
#endif

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    void            CoUninitialize();
    void            BaseCoUninitialize();
    void            BaseWinRTUninitialize();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    void        OnThreadTerminate(BOOL holdingLock);

    static void CleanupDetachedThreads();
    //--------------------------------------------------------------
    // Returns innermost active Frame.
    //--------------------------------------------------------------
    PTR_Frame GetFrame()
    {
        SUPPORTS_DAC;

#ifndef DACCESS_COMPILE
#ifdef _DEBUG_IMPL
        WRAPPER_NO_CONTRACT;
        if (this == GetThreadNULLOk())
        {
            void* curSP;
            curSP = (void *)GetCurrentSP();
            _ASSERTE((curSP <= m_pFrame && m_pFrame < m_CacheStackBase) || m_pFrame == (Frame*) -1);
        }
#else
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(!"NYI");
#endif
#endif // #ifndef DACCESS_COMPILE
        return m_pFrame;
    }

    //--------------------------------------------------------------
    // Replaces innermost active Frames.
    //--------------------------------------------------------------
#ifndef DACCESS_COMPILE
    void  SetFrame(Frame *pFrame)
#ifdef _DEBUG
        ;
#else
    {
        LIMITED_METHOD_CONTRACT;
        m_pFrame = pFrame;
    }
#endif
    ;
#endif
    inline Frame* FindFrame(SIZE_T StackPointer);

    bool DetectHandleILStubsForDebugger();

    void SetWin32FaultAddress(DWORD eip)
    {
        LIMITED_METHOD_CONTRACT;
        m_Win32FaultAddress = eip;
    }

    void SetWin32FaultCode(DWORD code)
    {
        LIMITED_METHOD_CONTRACT;
        m_Win32FaultCode = code;
    }

    DWORD GetWin32FaultAddress()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Win32FaultAddress;
    }

    DWORD GetWin32FaultCode()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Win32FaultCode;
    }

#ifdef ENABLE_CONTRACTS
    ClrDebugState *GetClrDebugState()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pClrDebugState;
    }
#endif

    //**************************************************************
    // GC interaction
    //**************************************************************

    //--------------------------------------------------------------
    // Enter cooperative GC mode. NOT NESTABLE.
    //--------------------------------------------------------------
    FORCEINLINE_NONDEBUG void DisablePreemptiveGC()
    {
#ifndef DACCESS_COMPILE
        WRAPPER_NO_CONTRACT;
        _ASSERTE(this == GetThread());
        _ASSERTE(!m_fPreemptiveGCDisabled);
        // holding a spin lock in preemp mode and transit to coop mode will cause other threads
        // spinning waiting for GC
        _ASSERTE ((m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

#ifdef ENABLE_CONTRACTS_IMPL
        TriggersGC(this);
#endif

        // Logically, we just want to check whether a GC is in progress and halt
        // at the boundary if it is -- before we disable preemptive GC.  However
        // this opens up a race condition where the GC starts after we make the
        // check.  SuspendRuntime will ignore such a thread because it saw it as
        // outside the EE.  So the thread would run wild during the GC.
        //
        // Instead, enter cooperative mode and then check if a GC is in progress.
        // If so, go back out and try again.  The reason we go back out before we
        // try again, is that SuspendRuntime might have seen us as being in
        // cooperative mode if it checks us between the next two statements.
        // In that case, it will be trying to move us to a safe spot.  If
        // we don't let it see us leave, it will keep waiting on us indefinitely.

        // ------------------------------------------------------------------------
        //   ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING **  |
        // ------------------------------------------------------------------------
        //
        //   DO NOT CHANGE THIS METHOD WITHOUT VISITING ALL THE STUB GENERATORS
        //   THAT EFFECTIVELY INLINE IT INTO THEIR STUBS
        //
        // ------------------------------------------------------------------------
        //   ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING **  |
        // ------------------------------------------------------------------------

        m_fPreemptiveGCDisabled.StoreWithoutBarrier(1);

        if (g_TrapReturningThreads.LoadWithoutBarrier())
        {
            RareDisablePreemptiveGC();
        }
#else
        LIMITED_METHOD_CONTRACT;
#endif
    }

    NOINLINE void RareDisablePreemptiveGC();

    void HandleThreadAbort();

    void PreWorkForThreadAbort();

private:
    void HandleThreadAbortTimeout();

public:
    //--------------------------------------------------------------
    // Leave cooperative GC mode. NOT NESTABLE.
    //--------------------------------------------------------------
    FORCEINLINE_NONDEBUG void EnablePreemptiveGC()
    {
        LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
        _ASSERTE(this == GetThread());
        _ASSERTE(m_fPreemptiveGCDisabled);
        // holding a spin lock in coop mode and transit to preemp mode will cause deadlock on GC
        _ASSERTE ((m_StateNC & Thread::TSNC_OwnsSpinLock) == 0);

#ifdef ENABLE_CONTRACTS_IMPL
        _ASSERTE(!GCForbidden());
        TriggersGC(this);
#endif

        // ------------------------------------------------------------------------
        //   ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING **  |
        // ------------------------------------------------------------------------
        //
        //   DO NOT CHANGE THIS METHOD WITHOUT VISITING ALL THE STUB GENERATORS
        //   THAT EFFECTIVELY INLINE IT INTO THEIR STUBS
        //
        // ------------------------------------------------------------------------
        //   ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING ** WARNING **  |
        // ------------------------------------------------------------------------

        m_fPreemptiveGCDisabled.StoreWithoutBarrier(0);
#ifdef ENABLE_CONTRACTS
        m_ulEnablePreemptiveGCCount ++;
#endif  // _DEBUG

        if (CatchAtSafePoint())
            RareEnablePreemptiveGC();
#endif
    }

#if defined(STRESS_HEAP) && defined(_DEBUG)
    void PerformPreemptiveGC();
#endif
    void RareEnablePreemptiveGC();
    void PulseGCMode();

    //--------------------------------------------------------------
    // Query mode
    //--------------------------------------------------------------
    BOOL PreemptiveGCDisabled()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(this == GetThread());
        //
        // m_fPreemptiveGCDisabled is always modified by the thread itself, and so the thread itself
        // can read it without memory barrier.
        //
        return m_fPreemptiveGCDisabled.LoadWithoutBarrier();
    }

    BOOL PreemptiveGCDisabledOther()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_fPreemptiveGCDisabled);
    }

#ifdef ENABLE_CONTRACTS_IMPL

    void BeginNoTriggerGC(const char *szFile, int lineNum)
    {
        WRAPPER_NO_CONTRACT;
        m_pClrDebugState->IncrementGCNoTriggerCount();
        if (PreemptiveGCDisabled())
        {
            m_pClrDebugState->IncrementGCForbidCount();
        }
    }

    void EndNoTriggerGC()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_pClrDebugState->GetGCNoTriggerCount() != 0 || (m_pClrDebugState->ViolationMask() & BadDebugState));
        m_pClrDebugState->DecrementGCNoTriggerCount();

        if (m_pClrDebugState->GetGCForbidCount())
        {
            m_pClrDebugState->DecrementGCForbidCount();
        }
    }

    void BeginForbidGC(const char *szFile, int lineNum)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(this == GetThread());
#ifdef PROFILING_SUPPORTED
        _ASSERTE(PreemptiveGCDisabled()
                 || CORProfilerPresent() ||    // This added to allow profiler to use GetILToNativeMapping
                                            // while in preemptive GC mode
                 (g_fEEShutDown & (ShutDown_Finalize2 | ShutDown_Profiler)) == ShutDown_Finalize2);
#else // PROFILING_SUPPORTED
        _ASSERTE(PreemptiveGCDisabled());
#endif // PROFILING_SUPPORTED
        BeginNoTriggerGC(szFile, lineNum);
    }

    void EndForbidGC()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(this == GetThread());
#ifdef PROFILING_SUPPORTED
        _ASSERTE(PreemptiveGCDisabled() ||
                 CORProfilerPresent() ||    // This added to allow profiler to use GetILToNativeMapping
                                            // while in preemptive GC mode
                 (g_fEEShutDown & (ShutDown_Finalize2 | ShutDown_Profiler)) == ShutDown_Finalize2);
#else // PROFILING_SUPPORTED
        _ASSERTE(PreemptiveGCDisabled());
#endif // PROFILING_SUPPORTED
        EndNoTriggerGC();
    }

    BOOL GCNoTrigger()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(this == GetThread());
        if ( (GCViolation|BadDebugState) & m_pClrDebugState->ViolationMask() )
        {
            return FALSE;
        }
        return m_pClrDebugState->GetGCNoTriggerCount();
    }

    BOOL GCForbidden()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(this == GetThread());
        if ( (GCViolation|BadDebugState) & m_pClrDebugState->ViolationMask())
        {
            return FALSE;
        }
        return m_pClrDebugState->GetGCForbidCount();
    }

    BOOL RawGCNoTrigger()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_pClrDebugState->ViolationMask() & BadDebugState)
        {
            return 0;
        }
        return m_pClrDebugState->GetGCNoTriggerCount();
    }

    BOOL RawGCForbidden()
    {
        LIMITED_METHOD_CONTRACT;
        if (m_pClrDebugState->ViolationMask() & BadDebugState)
        {
            return 0;
        }
        return m_pClrDebugState->GetGCForbidCount();
    }
#endif // ENABLE_CONTRACTS_IMPL

    //---------------------------------------------------------------
    // Expose key offsets and values for stub generation.
    //---------------------------------------------------------------
    static BYTE GetOffsetOfCurrentFrame()
    {
        LIMITED_METHOD_CONTRACT;
        size_t ofs = offsetof(class Thread, m_pFrame);
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    static BYTE GetOffsetOfState()
    {
        LIMITED_METHOD_CONTRACT;
        size_t ofs = offsetof(class Thread, m_State);
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    static BYTE GetOffsetOfGCFlag()
    {
        LIMITED_METHOD_CONTRACT;
        size_t ofs = offsetof(class Thread, m_fPreemptiveGCDisabled);
        _ASSERTE(FitsInI1(ofs));
        return (BYTE)ofs;
    }

    static void StaticDisablePreemptiveGC( Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(pThread != NULL);
        pThread->DisablePreemptiveGC();
    }

    static void StaticEnablePreemptiveGC( Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(pThread != NULL);
        pThread->EnablePreemptiveGC();
    }


    //---------------------------------------------------------------
    // Expose offset of the app domain word for the interop and delegate callback
    //---------------------------------------------------------------
    static SIZE_T GetOffsetOfAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T)(offsetof(class Thread, m_pDomain));
    }

    //---------------------------------------------------------------
    // Expose offset of the place for storing the filter context for the debugger.
    //---------------------------------------------------------------
    static SIZE_T GetOffsetOfDebuggerFilterContext()
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T)(offsetof(class Thread, m_debuggerFilterContext));
    }

    //---------------------------------------------------------------
    // Expose offset of the debugger cant stop count for the debugger
    //---------------------------------------------------------------
    static SIZE_T GetOffsetOfCantStop()
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T)(offsetof(class Thread, m_debuggerCantStop));
    }

    //---------------------------------------------------------------
    // Expose offset of m_StateNC
    //---------------------------------------------------------------
    static SIZE_T GetOffsetOfStateNC()
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T)(offsetof(class Thread, m_StateNC));
    }

    //---------------------------------------------------------------
    // Last exception to be thrown
    //---------------------------------------------------------------
    inline void SetThrowable(OBJECTREF pThrowable 
                             DEBUG_ARG(ThreadExceptionState::SetThrowableErrorChecking stecFlags = ThreadExceptionState::STEC_All));

    OBJECTREF GetThrowable()
    {
        WRAPPER_NO_CONTRACT;

        return m_ExceptionState.GetThrowable();
    }

    // An unmnaged thread can check if a managed is processing an exception
    BOOL HasException()
    {
        LIMITED_METHOD_CONTRACT;
        OBJECTHANDLE pThrowable = m_ExceptionState.GetThrowableAsHandle();
        return pThrowable && *PTR_UNCHECKED_OBJECTREF(pThrowable);
    }

    OBJECTHANDLE GetThrowableAsHandle()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ExceptionState.GetThrowableAsHandle();
    }

    // special null test (for use when we're in the wrong GC mode)
    BOOL IsThrowableNull()
    {
        WRAPPER_NO_CONTRACT;
        return IsHandleNullUnchecked(m_ExceptionState.GetThrowableAsHandle());
    }

    BOOL IsExceptionInProgress()
    {
        SUPPORTS_DAC;
        LIMITED_METHOD_CONTRACT;
        return m_ExceptionState.IsExceptionInProgress();
    }


    void SyncManagedExceptionState(bool fIsDebuggerThread);

    //---------------------------------------------------------------
    // Per-thread information used by handler
    //---------------------------------------------------------------
    // exception handling info stored in thread
    // can't allocate this as needed because can't make exception-handling depend upon memory allocation

    PTR_ThreadExceptionState GetExceptionState()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return PTR_ThreadExceptionState(PTR_HOST_MEMBER_TADDR(Thread, this, m_ExceptionState));
    }

public:

    void DECLSPEC_NORETURN RaiseCrossContextException(Exception* pEx, ContextTransitionFrame* pFrame);

    // ClearContext are to be called only during shutdown
    void ClearContext();

private:
    // don't ever call these except when creating thread!!!!!
    void InitContext();

public:
    PTR_AppDomain GetDomain(INDEBUG(BOOL fMidContextTransitionOK = FALSE))
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pDomain;
    }

    //---------------------------------------------------------------
    // Track use of the thread block.  See the general comments on
    // thread destruction in threads.cpp, for details.
    //---------------------------------------------------------------
    int         IncExternalCount();
    int         DecExternalCount(BOOL holdingLock);


    //---------------------------------------------------------------
    // !!!! THESE ARE NOT SAFE FOR GENERAL USE  !!!!
    //      IncExternalCountDANGEROUSProfilerOnly()
    //      DecExternalCountDANGEROUSProfilerOnly()
    // Currently only the profiler API should be using these
    // functions, because the profiler is responsible for ensuring
    // that the thread exists, undestroyed, before operating on it.
    // All other clients should use IncExternalCount/DecExternalCount
    // instead
    //---------------------------------------------------------------
    int         IncExternalCountDANGEROUSProfilerOnly()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
        int cRefs =
#else   // _DEBUG
        return
#endif //_DEBUG
            FastInterlockIncrement((LONG*)&m_ExternalRefCount);

#ifdef _DEBUG
        // This should never be called on a thread being destroyed
        _ASSERTE(cRefs != 1);
        return cRefs;
#endif //_DEBUG
    }

    int         DecExternalCountDANGEROUSProfilerOnly()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
        int cRefs =
#else   // _DEBUG
        return
#endif //_DEBUG

            FastInterlockDecrement((LONG*)&m_ExternalRefCount);

#ifdef _DEBUG
        // This should never cause the last reference on the thread to be released
        _ASSERTE(cRefs != 0);
        return cRefs;
#endif //_DEBUG
    }

    // Get and Set the exposed System.Thread object which corresponds to
    // this thread.  Also the thread handle and Id.
    OBJECTREF   GetExposedObject();
    OBJECTREF   GetExposedObjectRaw();
    void        SetExposedObject(OBJECTREF exposed);
    OBJECTHANDLE GetExposedObjectHandleForDebugger()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ExposedObject;
    }

    // Query whether the exposed object exists
    BOOL IsExposedObjectSet()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        return (ObjectFromHandle(m_ExposedObject) != NULL) ;
    }

    void GetSynchronizationContext(OBJECTREF *pSyncContextObj)
    {
        CONTRACTL
        {
            MODE_COOPERATIVE;
            GC_NOTRIGGER;
            NOTHROW;
            PRECONDITION(CheckPointer(pSyncContextObj));
        }
        CONTRACTL_END;

        *pSyncContextObj = NULL;

        THREADBASEREF ExposedThreadObj = (THREADBASEREF)GetExposedObjectRaw();
        if (ExposedThreadObj != NULL)
            *pSyncContextObj = ExposedThreadObj->GetSynchronizationContext();
    }


    // When we create a managed thread, the thread is suspended.  We call StartThread to get
    // the thread start.
    DWORD StartThread();

    // The result of attempting to OS-suspend an EE thread.
    enum SuspendThreadResult
    {
        // We successfully suspended the thread.  This is the only
        // case where the caller should subsequently call ResumeThread.
        STR_Success,

        // The underlying call to the operating system's SuspendThread
        // or GetThreadContext failed.  This is usually taken to mean
        // that the OS thread has exited.  (This can possibly also mean
        // 
        // that the suspension count exceeded the allowed maximum, but
        // Thread::SuspendThread asserts that does not happen.)
        STR_Failure,

        // The thread handle is invalid.  This means that the thread
        // is dead (or dying), or that the object has been created for
        // an exposed System.Thread that has not been started yet.
        STR_UnstartedOrDead,

        // The fOneTryOnly flag was set, and we managed to OS suspend the
        // thread, but we found that it had its m_dwForbidSuspendThread
        // flag set.  If fOneTryOnly is not set, Thread::Suspend will
        // retry in this case.
        STR_Forbidden,

        // Stress logging is turned on, but no stress log had been created
        // for the thread yet, and we failed to create one.  This can mean
        // that either we are not allowed to call into the host, or we ran
        // out of memory.
        STR_NoStressLog,

        // The EE thread is currently switched out.  This can only happen
        // if we are hosted and the host schedules EE threads on fibers.
        STR_SwitchedOut,
    };

#if defined(FEATURE_HIJACK) && defined(PLATFORM_UNIX)
    bool InjectGcSuspension();
#endif // FEATURE_HIJACK && PLATFORM_UNIX

#ifndef DISABLE_THREADSUSPEND
    // SuspendThread
    //   Attempts to OS-suspend the thread, whichever GC mode it is in.
    // Arguments:
    //   fOneTryOnly - If TRUE, report failure if the thread has its
    //     m_dwForbidSuspendThread flag set.  If FALSE, retry.
    //   pdwSuspendCount - If non-NULL, will contain the return code
    //     of the underlying OS SuspendThread call on success,
    //     undefined on any kind of failure.
    // Return value:
    //   A SuspendThreadResult value indicating success or failure.
    SuspendThreadResult SuspendThread(BOOL fOneTryOnly = FALSE, DWORD *pdwSuspendCount = NULL);

    DWORD ResumeThread();

#endif  // DISABLE_THREADSUSPEND

    int GetThreadPriority();
    BOOL SetThreadPriority(
        int nPriority   // thread priority level
    );
    BOOL Alert ();
    DWORD Join(DWORD timeout, BOOL alertable);
    DWORD JoinEx(DWORD timeout, WaitMode mode);

    BOOL GetThreadContext(
        LPCONTEXT lpContext   // context structure
    )
    {
        WRAPPER_NO_CONTRACT;
         return ::GetThreadContext (GetThreadHandle(), lpContext);
    }

#ifndef DACCESS_COMPILE
    BOOL SetThreadContext(
        CONST CONTEXT *lpContext   // context structure
    )
    {
        WRAPPER_NO_CONTRACT;
         return ::SetThreadContext (GetThreadHandle(), lpContext);
    }
#endif

    BOOL HasValidThreadHandle ()
    {
        WRAPPER_NO_CONTRACT;
        return GetThreadHandle() != INVALID_HANDLE_VALUE;
    }

    DWORD       GetThreadId()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE(m_ThreadId != UNINITIALIZED_THREADID);
        return m_ThreadId;
    }

    DWORD       GetOSThreadId()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
#ifndef DACCESS_COMPILE
        _ASSERTE (m_OSThreadId != 0xbaadf00d);
#endif // !DACCESS_COMPILE
        return m_OSThreadId;
    }

    // This API is to be used for Debugger only.
    // We need to be able to return the true value of m_OSThreadId.
    //
    DWORD       GetOSThreadIdForDebugger()
    {
        SUPPORTS_DAC;
        LIMITED_METHOD_CONTRACT;
        return m_OSThreadId;
    }

    BOOL        IsThreadPoolThread()
    {
        LIMITED_METHOD_CONTRACT;
        return m_State & (Thread::TS_TPWorkerThread | Thread::TS_CompletionPortThread);
    }

    // public suspend functions.  System ones are internal, like for GC.  User ones
    // correspond to suspend/resume calls on the exposed System.Thread object.
    static bool    SysStartSuspendForDebug(AppDomain *pAppDomain);
    static bool    SysSweepThreadsForDebug(bool forceSync);
    static void    SysResumeFromDebug(AppDomain *pAppDomain);

    void           UserSleep(INT32 time);

    // AD unload uses ThreadAbort support.  We need to distinguish pure ThreadAbort and AD unload
    // cases.
    enum ThreadAbortRequester
    {
        TAR_Thread =      0x00000001,   // Request by Thread
        TAR_FuncEval =    0x00000004,   // Request by Func-Eval
        TAR_ALL = 0xFFFFFFFF,
    };

private:

    //
    // Bit mask for tracking which aborts came in and why.
    //
    enum ThreadAbortInfo
    {
        TAI_ThreadAbort       = 0x00000001,
        TAI_ThreadRudeAbort   = 0x00000004,
        TAI_FuncEvalAbort     = 0x00000040,
        TAI_FuncEvalRudeAbort = 0x00000100,
    };

    static const DWORD TAI_AnySafeAbort = (TAI_ThreadAbort   |
                                           TAI_FuncEvalAbort
                                          );

    static const DWORD TAI_AnyRudeAbort = (TAI_ThreadRudeAbort   |
                                           TAI_FuncEvalRudeAbort
                                          );

    static const DWORD TAI_AnyFuncEvalAbort = (TAI_FuncEvalAbort   |
                                           TAI_FuncEvalRudeAbort
                                          );


    // Specifies type of thread abort.
    DWORD  m_AbortInfo;
    DWORD  m_AbortType;
    ULONGLONG  m_AbortEndTime;
    ULONGLONG  m_RudeAbortEndTime;
    BOOL   m_fRudeAbortInitiated;
    LONG   m_AbortController;

    static ULONGLONG s_NextSelfAbortEndTime;

    void SetRudeAbortEndTimeFromEEPolicy();

    // This is a spin lock to serialize setting/resetting of AbortType and AbortRequest.
    LONG  m_AbortRequestLock;

    static void  LockAbortRequest(Thread *pThread);
    static void  UnlockAbortRequest(Thread *pThread);

    typedef Holder<Thread*, Thread::LockAbortRequest, Thread::UnlockAbortRequest> AbortRequestLockHolder;

    static void AcquireAbortControl(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockIncrement (&pThread->m_AbortController);
    }

    static void ReleaseAbortControl(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (pThread->m_AbortController > 0);
        FastInterlockDecrement (&pThread->m_AbortController);
    }

    typedef Holder<Thread*, Thread::AcquireAbortControl, Thread::ReleaseAbortControl> AbortControlHolder;

public:
#ifdef _DEBUG
    BOOL           m_fRudeAborted;
    DWORD          m_dwAbortPoint;
#endif


public:
    enum UserAbort_Client
    {
        UAC_Normal,
        UAC_Host,       // Called by host through IClrTask::Abort
    };

    HRESULT        UserAbort(ThreadAbortRequester requester,
                             EEPolicy::ThreadAbortTypes abortType,
                             DWORD timeout,
                             UserAbort_Client client
                            );

    BOOL    HandleJITCaseForAbort();

    void           UserResetAbort(ThreadAbortRequester requester)
    {
        InternalResetAbort(requester, FALSE);
    }
    void           EEResetAbort(ThreadAbortRequester requester)
    {
        InternalResetAbort(requester, TRUE);
    }

private:
    void           InternalResetAbort(ThreadAbortRequester requester, BOOL fResetRudeAbort);

    void SetAbortEndTime(ULONGLONG endTime, BOOL fRudeAbort);

public:

    ULONGLONG      GetAbortEndTime()
    {
        WRAPPER_NO_CONTRACT;
        return IsRudeAbort()?m_RudeAbortEndTime:m_AbortEndTime;
    }

    // We distinguish interrupting a thread between Thread.Interrupt and other usage.
    // For Thread.Interrupt usage, we will interrupt an alertable wait using the same
    // rule as ReadyForAbort.  Wait in EH clause or CER region is not interrupted.
    // For other usage, we will try to Abort the thread.
    // If we can not do the operation, we will delay until next wait.
    enum ThreadInterruptMode
    {
        TI_Interrupt = 0x00000001,     // Requested by Thread.Interrupt
        TI_Abort     = 0x00000002,     // Requested by Thread.Abort or AppDomain.Unload
    };

private:
    BOOL           ReadyForAsyncException();

public:
    void           UserInterrupt(ThreadInterruptMode mode);

    BOOL           ReadyForAbort()
    {
        return ReadyForAsyncException();
    }

    BOOL           IsRudeAbort();
    BOOL           IsFuncEvalAbort();

#if defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)
    BOOL           IsSafeToInjectThreadAbort(PTR_CONTEXT pContextToCheck);
#endif // defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)

    inline BOOL IsAbortRequested()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_AbortRequested);
    }

    inline BOOL IsAbortInitiated()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_State & TS_AbortInitiated);
    }

    inline BOOL IsRudeAbortInitiated()
    {
        LIMITED_METHOD_CONTRACT;
        return IsAbortRequested() && m_fRudeAbortInitiated;
    }

    inline void SetAbortInitiated()
    {
        WRAPPER_NO_CONTRACT;
        if (IsRudeAbort()) {
            m_fRudeAbortInitiated = TRUE;
        }
        FastInterlockOr((ULONG *)&m_State, TS_AbortInitiated);
        // The following should be factored better, but I'm looking for a minimal V1 change.
        ResetUserInterrupted();
    }

    inline void ResetAbortInitiated()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockAnd((ULONG *)&m_State, ~TS_AbortInitiated);
        m_fRudeAbortInitiated = FALSE;
    }

    inline void SetPreparingAbort()
    {
        WRAPPER_NO_CONTRACT;
        SetThreadStateNC(TSNC_PreparingAbort);
    }

    inline void ResetPreparingAbort()
    {
        WRAPPER_NO_CONTRACT;
        ResetThreadStateNC(TSNC_PreparingAbort);
    }

private:
    inline static void SetPreparingAbortForHolder()
    {
        GetThread()->SetPreparingAbort();
    }
    inline static void ResetPreparingAbortForHolder()
    {
        GetThread()->ResetPreparingAbort();
    }
    typedef StateHolder<Thread::SetPreparingAbortForHolder, Thread::ResetPreparingAbortForHolder> PreparingAbortHolder;

public:

    inline void SetIsCreatingTypeInitException()
    {
        WRAPPER_NO_CONTRACT;
        SetThreadStateNC(TSNC_CreatingTypeInitException);
    }

    inline void ResetIsCreatingTypeInitException()
    {
        WRAPPER_NO_CONTRACT;
        ResetThreadStateNC(TSNC_CreatingTypeInitException);
    }

    inline BOOL IsCreatingTypeInitException()
    {
        WRAPPER_NO_CONTRACT;
        return HasThreadStateNC(TSNC_CreatingTypeInitException);
    }

private:
    void SetAbortRequestBit();

    void RemoveAbortRequestBit();

public:
    void MarkThreadForAbort(ThreadAbortRequester requester, EEPolicy::ThreadAbortTypes abortType);
    void UnmarkThreadForAbort(ThreadAbortRequester requester, BOOL fForce = TRUE);

    static ULONGLONG GetNextSelfAbortEndTime()
    {
        LIMITED_METHOD_CONTRACT;
        return s_NextSelfAbortEndTime;
    }

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
    // Tricks for resuming threads from fully interruptible code with a ThreadStop.
    BOOL           ResumeUnderControl(T_CONTEXT *pCtx);
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

    enum InducedThrowReason {
        InducedThreadStop = 1,
        InducedThreadRedirect = 2,
        InducedThreadRedirectAtEndOfCatch = 3,
    };

    DWORD          m_ThrewControlForThread;     // flag that is set when the thread deliberately raises an exception for stop/abort

    inline DWORD ThrewControlForThread()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ThrewControlForThread;
    }

    inline void SetThrowControlForThread(InducedThrowReason reason)
    {
        LIMITED_METHOD_CONTRACT;
        m_ThrewControlForThread = reason;
    }

    inline void ResetThrowControlForThread()
    {
        LIMITED_METHOD_CONTRACT;
        m_ThrewControlForThread = 0;
    }

    PTR_CONTEXT m_OSContext;    // ptr to a Context structure used to record the OS specific ThreadContext for a thread
                                // this is used for thread stop/abort and is intialized on demand

    PT_CONTEXT GetAbortContext ();

    // These will only ever be called from the debugger's helper
    // thread.
    //
    // When a thread is being created after a debug suspension has
    // started, we get the event on the debugger helper thread. It
    // will turn around and call this to set the debug suspend pending
    // flag on the newly created flag, since it was missed by
    // SysStartSuspendForGC as it didn't exist when that function was
    // run.
    void           MarkForDebugSuspend();

    // When the debugger uses the trace flag to single step a thread,
    // it also calls this function to mark this info in the thread's
    // state. The out-of-process portion of the debugger will read the
    // thread's state for a variety of reasons, including looking for
    // this flag.
    void           MarkDebuggerIsStepping(bool onOff)
    {
        WRAPPER_NO_CONTRACT;
        if (onOff)
            SetThreadStateNC(Thread::TSNC_DebuggerIsStepping);
        else
            ResetThreadStateNC(Thread::TSNC_DebuggerIsStepping);
    }

#ifdef _TARGET_ARM_
    // ARM doesn't currently support any reliable hardware mechanism for single-stepping. Instead we emulate
    // this in software. This support is used only by the debugger.
private:
    ArmSingleStepper m_singleStepper;
public:
#ifndef DACCESS_COMPILE
    // Given the context with which this thread shall be resumed and the first WORD of the instruction that
    // should be executed next (this is not always the WORD under PC since the debugger uses this mechanism to
    // skip breakpoints written into the code), set the thread up to execute one instruction and then throw an
    // EXCEPTION_SINGLE_STEP. (In fact an EXCEPTION_BREAKPOINT will be thrown, but this is fixed up in our
    // first chance exception handler, see IsDebuggerFault in excep.cpp).
    void EnableSingleStep()
    {
        m_singleStepper.Enable();
    }

    void BypassWithSingleStep(DWORD ip, WORD opcode1, WORD opcode2)
    {
        m_singleStepper.Bypass(ip, opcode1, opcode2);
    }

    void DisableSingleStep()
    {
        m_singleStepper.Disable();
    }

    void ApplySingleStep(T_CONTEXT *pCtx)
    {
        m_singleStepper.Apply(pCtx);
    }

    bool IsSingleStepEnabled() const
    {
        return m_singleStepper.IsEnabled();
    }

    // Fixup code called by our vectored exception handler to complete the emulation of single stepping
    // initiated by EnableSingleStep above. Returns true if the exception was indeed encountered during
    // stepping.
    bool HandleSingleStep(T_CONTEXT *pCtx, DWORD dwExceptionCode)
    {
        return m_singleStepper.Fixup(pCtx, dwExceptionCode);
    }
#endif // !DACCESS_COMPILE
#endif // _TARGET_ARM_

    private:

    PendingTypeLoadHolder* m_pPendingTypeLoad;

    public:

#ifndef DACCESS_COMPILE
    PendingTypeLoadHolder* GetPendingTypeLoad()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pPendingTypeLoad;
    }

    void SetPendingTypeLoad(PendingTypeLoadHolder* pPendingTypeLoad)
    {
        LIMITED_METHOD_CONTRACT;
        m_pPendingTypeLoad = pPendingTypeLoad;
    }
#endif

    private:

    ThreadLocalIBCInfo* m_pIBCInfo;

    public:

#ifndef DACCESS_COMPILE

    ThreadLocalIBCInfo* GetIBCInfo()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(g_IBCLogger.InstrEnabled());
        return m_pIBCInfo;
    }

    void SetIBCInfo(ThreadLocalIBCInfo* pInfo)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(g_IBCLogger.InstrEnabled());
        m_pIBCInfo = pInfo;
    }

    void FlushIBCInfo()
    {
        WRAPPER_NO_CONTRACT;
        if (m_pIBCInfo != NULL)
            m_pIBCInfo->FlushDelayedCallbacks();
    }

#endif // #ifndef DACCESS_COMPILE

    // Indicate whether this thread should run in the background.  Background threads
    // don't interfere with the EE shutting down.  Whereas a running non-background
    // thread prevents us from shutting down (except through System.Exit(), of course)
    // WARNING : only GC calls this with bRequiresTSL set to FALSE.
    void           SetBackground(BOOL isBack, BOOL bRequiresTSL=TRUE);

    // When the thread starts running, make sure it is running in the correct apartment
    // and context.
    BOOL           PrepareApartmentAndContext();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // Retrieve the apartment state of the current thread. There are three possible
    // states: thread hosts an STA, thread is part of the MTA or thread state is
    // undecided. The last state may indicate that the apartment has not been set at
    // all (nobody has called CoInitializeEx) or that the EE does not know the
    // current state (EE has not called CoInitializeEx).
    enum ApartmentState { AS_InSTA, AS_InMTA, AS_Unknown };
    ApartmentState GetApartment();
    ApartmentState GetApartmentRare(Thread::ApartmentState as);
    ApartmentState GetExplicitApartment();

    // Sets the apartment state if it has not already been set and
    // returns the state.
    ApartmentState GetFinalApartment();

    // Attempt to set current thread's apartment state. The actual apartment state
    // achieved is returned and may differ from the input state if someone managed to
    // call CoInitializeEx on this thread first (note that calls to SetApartment made
    // before the thread has started are guaranteed to succeed).
    // The fFireMDAOnMismatch indicates if we should fire the apartment state probe
    // on an apartment state mismatch.
    ApartmentState SetApartment(ApartmentState state, BOOL fFireMDAOnMismatch);

    // when we get apartment tear-down notification,
    // we want reset the apartment state we cache on the thread
    VOID ResetApartment();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    // Either perform WaitForSingleObject or MsgWaitForSingleObject as appropriate.
    DWORD          DoAppropriateWait(int countHandles, HANDLE *handles, BOOL waitAll,
                                     DWORD millis, WaitMode mode,
                                     PendingSync *syncInfo = 0);

    DWORD          DoAppropriateWait(AppropriateWaitFunc func, void *args, DWORD millis,
                                     WaitMode mode, PendingSync *syncInfo = 0);
    DWORD          DoSignalAndWait(HANDLE *handles, DWORD millis, BOOL alertable,
                                     PendingSync *syncState = 0);
private:
    void           DoAppropriateWaitWorkerAlertableHelper(WaitMode mode);
    DWORD          DoAppropriateWaitWorker(int countHandles, HANDLE *handles, BOOL waitAll,
                                           DWORD millis, WaitMode mode);
    DWORD          DoAppropriateWaitWorker(AppropriateWaitFunc func, void *args,
                                           DWORD millis, WaitMode mode);
    DWORD          DoSignalAndWaitWorker(HANDLE* pHandles, DWORD millis,BOOL alertable);
    DWORD          DoAppropriateAptStateWait(int numWaiters, HANDLE* pHandles, BOOL bWaitAll, DWORD timeout, WaitMode mode);
    DWORD          DoSyncContextWait(OBJECTREF *pSyncCtxObj, int countHandles, HANDLE *handles, BOOL waitAll, DWORD millis);
public:

    //************************************************************************
    // Enumerate all frames.
    //************************************************************************

    /* Flags used for StackWalkFramesEx */

    // FUNCTIONSONLY excludes all functionless frames and all funclets
    #define FUNCTIONSONLY                   0x0001

    // SKIPFUNCLETS includes functionless frames but excludes all funclets and everything between funclets and their parent methods
    #define SKIPFUNCLETS                    0x0002

    #define POPFRAMES                       0x0004

    /* use the following  flag only if you REALLY know what you are doing !!! */
    #define QUICKUNWIND                     0x0008 // do not restore all registers during unwind

    #define HANDLESKIPPEDFRAMES             0x0010 // temporary to handle skipped frames for appdomain unload
                                                   // stack crawl. Eventually need to always do this but it
                                                   // breaks the debugger right now.

    #define LIGHTUNWIND                     0x0020 // allow using cache schema (see StackwalkCache class)

    #define NOTIFY_ON_U2M_TRANSITIONS       0x0040 // Provide a callback for native transitions.
                                                   // This is only useful to a debugger trying to find native code
                                                   // in the stack.

    #define DISABLE_MISSING_FRAME_DETECTION 0x0080 // disable detection of missing TransitionFrames

    // One thread may be walking the stack of another thread
    // If you need to use this, you may also need to put a call to CrawlFrame::CheckGSCookies
    // in your callback routine if it does any potentially time-consuming activity.
    #define ALLOW_ASYNC_STACK_WALK          0x0100

    #define THREAD_IS_SUSPENDED             0x0200 // Be careful not to cause deadlocks, this thread is suspended

    // Stackwalk tries to verify some objects, but it could be called in relocate phase of GC,
    // where objects could be in invalid state, this flag is to tell stackwalk to skip the validation
    #define ALLOW_INVALID_OBJECTS           0x0400

    // Caller has verified that the thread to be walked is in the middle of executing
    // JITd or NGENd code, according to the thread's current context (or seeded
    // context if one was provided).  The caller ensures this when the stackwalk
    // is initiated by a profiler.
    #define THREAD_EXECUTING_MANAGED_CODE   0x0800

    // This stackwalk is due to the DoStackSnapshot profiler API
    #define PROFILER_DO_STACK_SNAPSHOT   0x1000

    // When this flag is set, the stackwalker does not automatically advance to the 
    // faulting managed stack frame when it encounters an ExInfo.  This should only be 
    // necessary for native debuggers doing mixed-mode stackwalking.
    #define NOTIFY_ON_NO_FRAME_TRANSITIONS  0x2000

    // Normally, the stackwalker does not stop at the initial CONTEXT if the IP is in native code.
    // This flag changes the stackwalker behaviour.  Currently this is only used in the debugger stackwalking
    // API.
    #define NOTIFY_ON_INITIAL_NATIVE_CONTEXT 0x4000
    
    // Indicates that we are enumerating GC references and should follow appropriate
    // callback rules for parent methods vs funclets. Only supported on non-x86 platforms.
    // 
    // Refer to StackFrameIterator::Filter for detailed comments on this flag.
    #define GC_FUNCLET_REFERENCE_REPORTING 0x8000

    // Stackwalking normally checks GS cookies on the fly, but there are cases in which the JIT reports
    // incorrect epilog information. This causes the debugger to request stack walks in the epilog, checking
    // an now invalid cookie. This flag allows the debugger stack walks to disable GS cookie checking.
    
    // This is a workaround for the debugger stackwalking. In general, the stackwalker and CrawlFrame
    // may still execute GS cookie tracking/checking code paths.
    #define SKIP_GSCOOKIE_CHECK 0x10000

    StackWalkAction StackWalkFramesEx(
                        PREGDISPLAY pRD,        // virtual register set at crawl start
                        PSTACKWALKFRAMESCALLBACK pCallback,
                        VOID *pData,
                        unsigned flags,
                        PTR_Frame pStartFrame = PTR_NULL);

private:
    // private helpers used by StackWalkFramesEx and StackFrameIterator
    StackWalkAction MakeStackwalkerCallback(CrawlFrame* pCF, PSTACKWALKFRAMESCALLBACK pCallback, VOID* pData DEBUG_ARG(UINT32 uLoopIteration));

#ifdef _DEBUG
    void            DebugLogStackWalkInfo(CrawlFrame* pCF, __in_z LPCSTR pszTag, UINT32 uLoopIteration);
#endif // _DEBUG

public:

    StackWalkAction StackWalkFrames(
                        PSTACKWALKFRAMESCALLBACK pCallback,
                        VOID *pData,
                        unsigned flags = 0,
                        PTR_Frame pStartFrame = PTR_NULL);

    bool InitRegDisplay(const PREGDISPLAY, const PT_CONTEXT, bool validContext);
    void FillRegDisplay(const PREGDISPLAY pRD, PT_CONTEXT pctx);

#ifdef WIN64EXCEPTIONS
    static PCODE VirtualUnwindCallFrame(T_CONTEXT* pContext, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers = NULL,
                                           EECodeInfo * pCodeInfo = NULL);
    static UINT_PTR VirtualUnwindCallFrame(PREGDISPLAY pRD, EECodeInfo * pCodeInfo = NULL);
#ifndef DACCESS_COMPILE
    static PCODE VirtualUnwindLeafCallFrame(T_CONTEXT* pContext);
    static PCODE VirtualUnwindNonLeafCallFrame(T_CONTEXT* pContext, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers = NULL,
        PT_RUNTIME_FUNCTION pFunctionEntry = NULL, UINT_PTR uImageBase = NULL);
    static UINT_PTR VirtualUnwindToFirstManagedCallFrame(T_CONTEXT* pContext);
#endif // DACCESS_COMPILE
#endif // WIN64EXCEPTIONS

    // During a <clinit>, this thread must not be asynchronously
    // stopped or interrupted.  That would leave the class unavailable
    // and is therefore a security hole.
    static void        IncPreventAsync()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        FastInterlockIncrement((LONG*)&pThread->m_PreventAsync);
    }
    static void        DecPreventAsync()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        FastInterlockDecrement((LONG*)&pThread->m_PreventAsync);
    }

    bool IsAsyncPrevented()
    {
        return m_PreventAsync != 0;
    }

    typedef StateHolder<Thread::IncPreventAsync, Thread::DecPreventAsync> ThreadPreventAsyncHolder;

    // During a <clinit>, this thread must not be asynchronously
    // stopped or interrupted.  That would leave the class unavailable
    // and is therefore a security hole.
    static void        IncPreventAbort()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        FastInterlockIncrement((LONG*)&pThread->m_PreventAbort);
    }
    static void        DecPreventAbort()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        FastInterlockDecrement((LONG*)&pThread->m_PreventAbort);
    }

    BOOL IsAbortPrevented()
    {
        return m_PreventAbort != 0;
    }

    typedef StateHolder<Thread::IncPreventAbort, Thread::DecPreventAbort> ThreadPreventAbortHolder;

    // The ThreadStore manages a list of all the threads in the system.  I
    // can't figure out how to expand the ThreadList template type without
    // making m_Link public.
    SLink       m_Link;
    
    // For N/Direct calls with the "setLastError" bit, this field stores
    // the errorcode from that call.
    DWORD       m_dwLastError;

#ifdef FEATURE_INTERPRETER
    // When we're interpreting IL stubs for N/Direct calls with the "setLastError" bit,
    // the interpretation will trash the last error before we get to the call to "SetLastError".
    // Therefore, we record it here immediately after the calli, and treat "SetLastError" as an 
    // intrinsic that transfers the value stored here into the field above.
    DWORD       m_dwLastErrorInterp;
#endif

    // Debugger per-thread flag for enabling notification on "manual"
    // method calls,  for stepping logic
    void IncrementTraceCallCount();
    void DecrementTraceCallCount();

    FORCEINLINE int IsTraceCall()
    {
        LIMITED_METHOD_CONTRACT;
        return m_TraceCallCount;
    }

    // Functions to get/set culture information for current thread.
    static OBJECTREF GetCulture(BOOL bUICulture);
    static void SetCulture(OBJECTREF *CultureObj, BOOL bUICulture);

private:
#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
    // Used in suspension code to redirect a thread at a HandledJITCase
    BOOL RedirectThreadAtHandledJITCase(PFN_REDIRECTTARGET pTgt);
    BOOL RedirectCurrentThreadAtHandledJITCase(PFN_REDIRECTTARGET pTgt, T_CONTEXT *pCurrentThreadCtx);

    // Will Redirect the thread using RedirectThreadAtHandledJITCase if necessary
    BOOL CheckForAndDoRedirect(PFN_REDIRECTTARGET pRedirectTarget);
    BOOL CheckForAndDoRedirectForDbg();
    BOOL CheckForAndDoRedirectForGC();
    BOOL CheckForAndDoRedirectForUserSuspend();

    // Exception handling must be very aware of redirection, so we provide a helper
    // to identifying redirection targets
    static BOOL IsAddrOfRedirectFunc(void * pFuncAddr);

#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS)
public:
    BOOL CheckForAndDoRedirectForGCStress (T_CONTEXT *pCurrentThreadCtx);
private:
    bool        m_fPreemptiveGCDisabledForGCStress;
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

public:

#ifndef DACCESS_COMPILE
    // These re-calculate the proper value on each call for the currently executing thread. Use GetCachedStackLimit
    // and GetCachedStackBase for the cached values on this Thread.
    static void * GetStackLowerBound();
    static void * GetStackUpperBound();

    bool CheckCanUseStackAlloc()
    {
        int local;
        UINT_PTR current = reinterpret_cast<UINT_PTR>(&local);
        UINT_PTR limit = GetCachedStackStackAllocNonRiskyExecutionLimit();
        return (current > limit);
    }
#else // DACCESS_COMPILE
    bool CheckCanUseStackAlloc() { return true; }
#endif

    enum SetStackLimitScope { fAll, fAllowableOnly };
    BOOL SetStackLimits(SetStackLimitScope scope);

    // These access the stack base and limit values for this thread. (They are cached during InitThread.) The
    // "stack base" is the "upper bound", i.e., where the stack starts growing from. (Main's call frame is at the
    // upper bound.) The "stack limit" is the "lower bound", i.e., how far the stack can grow down to.
    // The "stack sufficient execution limit" is used by EnsureSufficientExecutionStack() to limit how much stack
    // should remain to execute the average Framework method.
    PTR_VOID GetCachedStackBase() {LIMITED_METHOD_DAC_CONTRACT;  return m_CacheStackBase; }
    PTR_VOID GetCachedStackLimit() {LIMITED_METHOD_DAC_CONTRACT;  return m_CacheStackLimit;}
    UINT_PTR GetCachedStackSufficientExecutionLimit() {LIMITED_METHOD_DAC_CONTRACT; return m_CacheStackSufficientExecutionLimit;}
    UINT_PTR GetCachedStackStackAllocNonRiskyExecutionLimit() {LIMITED_METHOD_DAC_CONTRACT; return m_CacheStackStackAllocNonRiskyExecutionLimit;}

private:
    // Access the base and limit of the stack. (I.e. the memory ranges that the thread has reserved for its stack).
    //
    // Note that the base is at a higher address than the limit, since the stack grows downwards.
    //
    // Note that we generally access the stack of the thread we are crawling, which is cached in the ScanContext.
    PTR_VOID    m_CacheStackBase;
    PTR_VOID    m_CacheStackLimit;
    UINT_PTR    m_CacheStackSufficientExecutionLimit;
    UINT_PTR    m_CacheStackStackAllocNonRiskyExecutionLimit;

#define HARD_GUARD_REGION_SIZE GetOsPageSize()

private:
    //
    static HRESULT CLRSetThreadStackGuarantee(SetThreadStackGuaranteeScope fScope = STSGuarantee_OnlyIfEnabled);

    // try to turn a page into a guard page
    static BOOL MarkPageAsGuard(UINT_PTR uGuardPageBase);

    // scan a region for a guard page
    static BOOL DoesRegionContainGuardPage(UINT_PTR uLowAddress, UINT_PTR uHighAddress);

    // Every stack has a single reserved page at its limit that we call the 'hard guard page'. This page is never
    // committed, and access to it after a stack overflow will terminate the thread.
#define HARD_GUARD_REGION_SIZE GetOsPageSize()
#define SIZEOF_DEFAULT_STACK_GUARANTEE 1 * GetOsPageSize()

public:
    // This will return the last stack address that one could write to before a stack overflow.
    static UINT_PTR GetLastNormalStackAddress(UINT_PTR stackBase);
    UINT_PTR GetLastNormalStackAddress();

    UINT_PTR GetLastAllowableStackAddress()
    {
        return m_LastAllowableStackAddress;
    }

    UINT_PTR GetProbeLimit()
    {
        return m_ProbeLimit;
    }

    void ResetStackLimits()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        if (!IsSetThreadStackGuaranteeInUse())
        {
            return;
        }
        SetStackLimits(fAllowableOnly);
    }

    BOOL IsSPBeyondLimit();

    INDEBUG(static void DebugLogStackMBIs());

#if defined(_DEBUG_IMPL) && !defined(DACCESS_COMPILE)
    // Verify that the cached stack base is for the current thread.
    BOOL HasRightCacheStackBase()
    {
        WRAPPER_NO_CONTRACT;
        return m_CacheStackBase == GetStackUpperBound();
    }
#endif

public:
    static BOOL UniqueStack(void* startLoc = 0);

    BOOL IsAddressInStack (PTR_VOID addr) const
    {
        LIMITED_METHOD_DAC_CONTRACT; 
        _ASSERTE(m_CacheStackBase != NULL);
        _ASSERTE(m_CacheStackLimit != NULL);
        _ASSERTE(m_CacheStackLimit < m_CacheStackBase);
        return m_CacheStackLimit < addr && addr <= m_CacheStackBase;
    }

    static BOOL IsAddressInCurrentStack (PTR_VOID addr)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        Thread* currentThread = GetThread();
        if (currentThread == NULL)
        {
            return FALSE;
        }

        PTR_VOID sp = dac_cast<PTR_VOID>(GetCurrentSP());
        _ASSERTE(currentThread->m_CacheStackBase != NULL);
        _ASSERTE(sp < currentThread->m_CacheStackBase);
        return sp < addr && addr <= currentThread->m_CacheStackBase;
    }

    // DetermineIfGuardPagePresent returns TRUE if the thread's stack contains a proper guard page. This function
    // makes a physical check of the stack, rather than relying on whether or not the CLR is currently processing a
    // stack overflow exception.
    BOOL DetermineIfGuardPagePresent();

    // Returns the amount of stack available after an SO but before the OS rips the process.
    static UINT_PTR GetStackGuarantee();

    // RestoreGuardPage will replace the guard page on this thread's stack. The assumption is that it was removed
    // by the OS due to a stack overflow exception. This function requires that you know that you have enough stack
    // space to restore the guard page, so make sure you know what you're doing when you decide to call this.
    VOID RestoreGuardPage();

#if defined(FEATURE_HIJACK) && !defined(PLATFORM_UNIX)
private:
    // Redirecting of threads in managed code at suspension

    enum RedirectReason {
        RedirectReason_GCSuspension,
        RedirectReason_DebugSuspension,
        RedirectReason_UserSuspension,
#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
        RedirectReason_GCStress,
#endif // HAVE_GCCOVER && USE_REDIRECT_FOR_GCSTRESS
    };
    static void __stdcall RedirectedHandledJITCase(RedirectReason reason);
    static void __stdcall RedirectedHandledJITCaseForDbgThreadControl();
    static void __stdcall RedirectedHandledJITCaseForGCThreadControl();
    static void __stdcall RedirectedHandledJITCaseForUserSuspend();
#if defined(HAVE_GCCOVER) && defined(USE_REDIRECT_FOR_GCSTRESS) // GCCOVER
    static void __stdcall RedirectedHandledJITCaseForGCStress();
#endif // defined(HAVE_GCCOVER) && USE_REDIRECT_FOR_GCSTRESS

    friend void CPFH_AdjustContextForThreadSuspensionRace(T_CONTEXT *pContext, Thread *pThread);
#endif // FEATURE_HIJACK && !PLATFORM_UNIX

private:
    //-------------------------------------------------------------
    // Waiting & Synchronization
    //-------------------------------------------------------------

    // For suspends.  The thread waits on this event.  A client sets the event to cause
    // the thread to resume.
    void    WaitSuspendEvents(BOOL fDoWait = TRUE);
    BOOL    WaitSuspendEventsHelper(void);

    // Helpers to ensure that the bits for suspension and the number of active
    // traps remain coordinated.
    void    MarkForSuspension(ULONG bit);
    void    UnmarkForSuspension(ULONG bit);

    void    SetupForSuspension(ULONG bit)
    {
        WRAPPER_NO_CONTRACT;

        // CoreCLR does not support user-requested thread suspension
        _ASSERTE(!(bit & TS_UserSuspendPending));


        if (bit & TS_DebugSuspendPending) {
            m_DebugSuspendEvent.Reset();
        }
    }

    void    ReleaseFromSuspension(ULONG bit)
    {
        WRAPPER_NO_CONTRACT;

        UnmarkForSuspension(~bit);

        //
        // If the thread is set free, mark it as not-suspended now
        //
        ThreadState oldState = m_State;

        // CoreCLR does not support user-requested thread suspension
        _ASSERTE(!(oldState & TS_UserSuspendPending));

        while ((oldState & (TS_UserSuspendPending | TS_DebugSuspendPending)) == 0)
        {
            // CoreCLR does not support user-requested thread suspension
            _ASSERTE(!(oldState & TS_UserSuspendPending));

            //
            // Construct the destination state we desire - all suspension bits turned off.
            //
            ThreadState newState = (ThreadState)(oldState & ~(TS_UserSuspendPending |
                                                              TS_DebugSuspendPending |
                                                              TS_SyncSuspended));

            if (FastInterlockCompareExchange((LONG *)&m_State, newState, oldState) == (LONG)oldState)
            {
                break;
            }

            //
            // The state changed underneath us, refresh it and try again.
            //
            oldState = m_State;
        }

        // CoreCLR does not support user-requested thread suspension
        _ASSERTE(!(bit & TS_UserSuspendPending));

        if (bit & TS_DebugSuspendPending) {
            m_DebugSuspendEvent.Set();
        }

    }

public:
    FORCEINLINE void UnhijackThreadNoAlloc()
    {
#if defined(FEATURE_HIJACK) && !defined(DACCESS_COMPILE)
        if (m_State & TS_Hijacked)
        {
            *m_ppvHJRetAddrPtr = m_pvHJRetAddr;
            FastInterlockAnd((ULONG *) &m_State, ~TS_Hijacked);
        }
#endif
    }

    void    UnhijackThread();

    // Flags that may be passed to GetSafelyRedirectableThreadContext, to customize
    // which checks it should perform.  This allows a subset of the context verification
    // logic used by HandledJITCase to be shared with other callers, such as profiler
    // stackwalking
    enum GetSafelyRedirectableThreadContextOptions
    {
        // Perform the default thread context checks
        kDefaultChecks              = 0x00000000,
        
        // Compares the thread context's IP against m_LastRedirectIP, and potentially
        // updates m_LastRedirectIP, when determining the safeness of the thread's
        // context.  HandledJITCase will always set this flag.
		// This flag is ignored on non-x86 platforms, and also on x86 if the OS supports
		// trap frame reporting.
        kPerfomLastRedirectIPCheck  = 0x00000001,

        // Use g_pDebugInterface->IsThreadContextInvalid() to see if breakpoints might
        // confuse the stack walker.  HandledJITCase will always set this flag.
        kCheckDebuggerBreakpoints   = 0x00000002,
    };

    // Helper used by HandledJITCase and others who need an absolutely reliable
    // register context.
    BOOL GetSafelyRedirectableThreadContext(DWORD dwOptions, T_CONTEXT * pCtx, REGDISPLAY * pRD);

private:
#ifdef FEATURE_HIJACK
    void    HijackThread(VOID *pvHijackAddr, ExecutionState *esb);

    VOID        *m_pvHJRetAddr;           // original return address (before hijack)
    VOID       **m_ppvHJRetAddrPtr;       // place we bashed a new return address
    MethodDesc  *m_HijackedFunction;      // remember what we hijacked

#ifndef PLATFORM_UNIX
    BOOL    HandledJITCase(BOOL ForTaskSwitchIn = FALSE);

#ifdef _TARGET_X86_
    PCODE       m_LastRedirectIP;
    ULONG       m_SpinCount;
#endif // _TARGET_X86_

#endif // !PLATFORM_UNIX

#endif // FEATURE_HIJACK

    DWORD       m_Win32FaultAddress;
    DWORD       m_Win32FaultCode;

    // Support for Wait/Notify
    BOOL        Block(INT32 timeOut, PendingSync *syncInfo);
    void        Wake(SyncBlock *psb);
    DWORD       Wait(HANDLE *objs, int cntObjs, INT32 timeOut, PendingSync *syncInfo);
    DWORD       Wait(CLREvent* pEvent, INT32 timeOut, PendingSync *syncInfo);

    // support for Thread.Interrupt() which breaks out of Waits, Sleeps, Joins
    LONG        m_UserInterrupt;
    DWORD       IsUserInterrupted()
    {
        LIMITED_METHOD_CONTRACT;
        return m_UserInterrupt;
    }
    void        ResetUserInterrupted()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockExchange(&m_UserInterrupt, 0);
    }

    void        HandleThreadInterrupt();

public:
    static void WINAPI UserInterruptAPC(ULONG_PTR ignore);

#if defined(_DEBUG) && defined(TRACK_SYNC)

// Each thread has a stack that tracks all enter and leave requests
public:
    Dbg_TrackSync   *m_pTrackSync;

#endif // TRACK_SYNC

private:
#ifdef ENABLE_CONTRACTS_DATA
    struct ClrDebugState *m_pClrDebugState; // Pointer to ClrDebugState for quick access

    ULONG  m_ulEnablePreemptiveGCCount;
#endif  // _DEBUG

private:
    // For suspends:
    CLREvent        m_DebugSuspendEvent;

    // For Object::Wait, Notify and NotifyAll, we use an Event inside the
    // thread and we queue the threads onto the SyncBlock of the object they
    // are waiting for.
    CLREvent        m_EventWait;
    WaitEventLink   m_WaitEventLink;
    WaitEventLink* WaitEventLinkForSyncBlock (SyncBlock *psb)
    {
        LIMITED_METHOD_CONTRACT;
        WaitEventLink *walk = &m_WaitEventLink;
        while (walk->m_Next) {
            _ASSERTE (walk->m_Next->m_Thread == this);
            if ((SyncBlock*)(((DWORD_PTR)walk->m_Next->m_WaitSB) & ~1)== psb) {
                break;
            }
            walk = walk->m_Next;
        }
        return walk;
    }

    // Access to thread handle and ThreadId.
    HANDLE      GetThreadHandle()
    {
        LIMITED_METHOD_CONTRACT;
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        {
            CounterHolder handleHolder(&m_dwThreadHandleBeingUsed);
            HANDLE handle = m_ThreadHandle;
            _ASSERTE ( handle == INVALID_HANDLE_VALUE 
                || handle == SWITCHOUT_HANDLE_VALUE
                || m_OSThreadId == 0
                || m_OSThreadId == 0xbaadf00d
                || ::MatchThreadHandleToOsId(handle, m_OSThreadId) );
        }
#endif

        DACCOP_IGNORE(FieldAccess, "Treated as raw address, no marshaling is necessary");
        return m_ThreadHandle;
    }

    void        SetThreadHandle(HANDLE h)
    {
        LIMITED_METHOD_CONTRACT;
#if defined(_DEBUG)
        _ASSERTE ( h == INVALID_HANDLE_VALUE 
            || h == SWITCHOUT_HANDLE_VALUE
            || m_OSThreadId == 0
            || m_OSThreadId == 0xbaadf00d
            || ::MatchThreadHandleToOsId(h, m_OSThreadId) );
#endif
        FastInterlockExchangePointer(&m_ThreadHandle, h);
    }

    // We maintain a correspondence between this object, the ThreadId and ThreadHandle
    // in Win32, and the exposed Thread object.
    HANDLE          m_ThreadHandle;

    // <TODO> It would be nice to remove m_ThreadHandleForClose to simplify Thread.Join,
    //   but at the moment that isn't possible without extensive work.
    //   This handle is used by SwitchOut to store the old handle which may need to be closed
    //   if we are the owner.  The handle can't be closed before checking the external count
    //   which we can't do in SwitchOut since that may require locking or switching threads.</TODO>
    HANDLE          m_ThreadHandleForClose;
    HANDLE          m_ThreadHandleForResume;
    BOOL            m_WeOwnThreadHandle;
    DWORD           m_OSThreadId;

    BOOL CreateNewOSThread(SIZE_T stackSize, LPTHREAD_START_ROUTINE start, void *args);

    OBJECTHANDLE    m_ExposedObject;
    OBJECTHANDLE    m_StrongHndToExposedObject;

    DWORD           m_Priority;     // initialized to INVALID_THREAD_PRIORITY, set to actual priority when a
                                    // thread does a busy wait for GC, reset to INVALID_THREAD_PRIORITY after wait is over
    friend class NDirect; // Quick access to thread stub creation

#ifdef HAVE_GCCOVER
    friend void DoGcStress (PT_CONTEXT regs, MethodDesc *pMD);  // Needs to call UnhijackThread
#endif // HAVE_GCCOVER

    ULONG           m_ExternalRefCount;

    ULONG           m_UnmanagedRefCount;

    LONG            m_TraceCallCount;

    //-----------------------------------------------------------
    // Bytes promoted on this thread since the last GC?
    //-----------------------------------------------------------
    DWORD           m_fPromoted;
public:
    void SetHasPromotedBytes ();
    DWORD GetHasPromotedBytes ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fPromoted;
    }

private:
    //-----------------------------------------------------------
    // Last exception to be thrown.
    //-----------------------------------------------------------
    friend class EEDbgInterfaceImpl;

private:
    // Stores the most recently thrown exception. We need to have a handle in case a GC occurs before
    // we catch so we don't lose the object. Having a static allows others to catch outside of COM+ w/o leaking
    // a handler and allows rethrow outside of COM+ too.
    // Differs from m_pThrowable in that it doesn't stack on nested exceptions.
    OBJECTHANDLE m_LastThrownObjectHandle;      // Unsafe to use directly.  Use accessors instead.
    
    // Indicates that the throwable in m_lastThrownObjectHandle should be treated as
    // unhandled. This occurs during fatal error and a few other early error conditions
    // before EH is fully set up.
    BOOL m_ltoIsUnhandled;

    friend void DECLSPEC_NORETURN EEPolicy::HandleFatalStackOverflow(EXCEPTION_POINTERS *pExceptionInfo, BOOL fSkipDebugger);

public:

    BOOL IsLastThrownObjectNull() { WRAPPER_NO_CONTRACT; return (m_LastThrownObjectHandle == NULL); }

    OBJECTREF LastThrownObject()
    {
        WRAPPER_NO_CONTRACT;

        if (m_LastThrownObjectHandle == NULL)
        {
            return NULL;
        }
        else
        {
            // We only have a handle if we have an object to keep in it.
            _ASSERTE(ObjectFromHandle(m_LastThrownObjectHandle) != NULL);
            return ObjectFromHandle(m_LastThrownObjectHandle);
        }
    }

    OBJECTHANDLE LastThrownObjectHandle()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_LastThrownObjectHandle;
    }

    void SetLastThrownObject(OBJECTREF throwable, BOOL isUnhandled = FALSE);
    void SetSOForLastThrownObject();
    OBJECTREF SafeSetLastThrownObject(OBJECTREF throwable);

    // Inidcates that the last thrown object is now treated as unhandled
    void MarkLastThrownObjectUnhandled()
    {
        LIMITED_METHOD_CONTRACT;
        m_ltoIsUnhandled = TRUE;
    }

    // TRUE if the throwable in LTO should be treated as unhandled
    BOOL IsLastThrownObjectUnhandled()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_ltoIsUnhandled;
    }

    void SafeUpdateLastThrownObject(void);
    OBJECTREF SafeSetThrowables(OBJECTREF pThrowable 
                                DEBUG_ARG(ThreadExceptionState::SetThrowableErrorChecking stecFlags = ThreadExceptionState::STEC_All),
                                BOOL isUnhandled = FALSE);

    bool IsLastThrownObjectStackOverflowException()
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(NULL != g_pPreallocatedStackOverflowException);

        return (m_LastThrownObjectHandle == g_pPreallocatedStackOverflowException);
    }

    // get the current notification (if any) from this thread
    OBJECTHANDLE GetThreadCurrNotification();

    // set the current notification on this thread
    void SetThreadCurrNotification(OBJECTHANDLE handle);

    // clear the current notification (if any) from this thread
    void ClearThreadCurrNotification();

private:
    void SetLastThrownObjectHandle(OBJECTHANDLE h);

    ThreadExceptionState  m_ExceptionState;

    //-----------------------------------------------------------
    // For stack probing.  These are the last allowable addresses that a thread
    // can touch.  Going beyond is a stack overflow.  The ProbeLimit will be
    // set based on whether SO probing is enabled.  The LastAllowableAddress
    // will always represent the true stack limit.
    //-----------------------------------------------------------
    UINT_PTR             m_ProbeLimit;

    UINT_PTR             m_LastAllowableStackAddress;

private:
    //---------------------------------------------------------------
    // m_debuggerFilterContext holds the thread's "filter context" for the
    // debugger.  This filter context is used by the debugger to seed
    // stack walks on the thread.
    //---------------------------------------------------------------
    PTR_CONTEXT m_debuggerFilterContext;

    //---------------------------------------------------------------
    // m_profilerFilterContext holds an additional context for the
    // case when a (sampling) profiler wishes to hijack the thread
    // and do a stack walk on the same thread.
    //---------------------------------------------------------------
    T_CONTEXT *m_pProfilerFilterContext;

    //---------------------------------------------------------------
    // m_hijackLock holds a BOOL that is used for mutual exclusion
    // between profiler stack walks and thread hijacks (bashing 
    // return addresses on the stack)
    //---------------------------------------------------------------
    Volatile<LONG> m_hijackLock;
    //---------------------------------------------------------------
    // m_debuggerCantStop holds a count of entries into "can't stop"
    // areas that the Interop Debugging Services must know about.
    //---------------------------------------------------------------
    DWORD m_debuggerCantStop;

    //---------------------------------------------------------------
    // The current custom notification data object (or NULL if none
    // pending)
    //---------------------------------------------------------------
    OBJECTHANDLE m_hCurrNotification;

    //---------------------------------------------------------------
    // For Interop-Debugging; track if a thread is hijacked.
    //---------------------------------------------------------------
    BOOL    m_fInteropDebuggingHijacked;

    //---------------------------------------------------------------
    // Bitmask to remember per-thread state useful for the profiler API.  See
    // COR_PRF_CALLBACKSTATE_* flags in clr\src\inc\ProfilePriv.h for bit values.
    //---------------------------------------------------------------
    DWORD m_profilerCallbackState;

#if defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)
    //---------------------------------------------------------------
    // m_dwProfilerEvacuationCounter keeps track of how many profiler
    // callback calls remain on the stack
    //---------------------------------------------------------------
    // Why volatile?
    // See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization.
    Volatile<DWORD> m_dwProfilerEvacuationCounter;
#endif // defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)

private:
    UINT32 m_workerThreadPoolCompletionCount;
    static UINT64 s_workerThreadPoolCompletionCountOverflow;
    UINT32 m_ioThreadPoolCompletionCount;
    static UINT64 s_ioThreadPoolCompletionCountOverflow;
    UINT32 m_monitorLockContentionCount;
    static UINT64 s_monitorLockContentionCountOverflow;

#ifndef DACCESS_COMPILE
private:
    static UINT32 *GetThreadLocalCountRef(Thread *pThread, SIZE_T threadLocalCountOffset)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(threadLocalCountOffset <= sizeof(Thread) - sizeof(UINT32));

        return (UINT32 *)((SIZE_T)pThread + threadLocalCountOffset);
    }

    static void IncrementCount(Thread *pThread, SIZE_T threadLocalCountOffset, UINT64 *overflowCount)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(overflowCount != nullptr);

        if (pThread != nullptr)
        {
            UINT32 *threadLocalCount = GetThreadLocalCountRef(pThread, threadLocalCountOffset);
            UINT32 newCount = *threadLocalCount + 1;
            if (newCount != 0)
            {
                VolatileStoreWithoutBarrier(threadLocalCount, newCount);
            }
            else
            {
                OnIncrementCountOverflow(threadLocalCount, overflowCount);
            }
        }
        else
        {
            InterlockedIncrement64((LONGLONG *)overflowCount);
        }
    }

    static void OnIncrementCountOverflow(UINT32 *threadLocalCount, UINT64 *overflowCount);

    static UINT64 GetOverflowCount(UINT64 *overflowCount)
    {
        WRAPPER_NO_CONTRACT;

        if (sizeof(void *) >= sizeof(*overflowCount))
        {
            return VolatileLoad(overflowCount);
        }
        return InterlockedCompareExchange64((LONGLONG *)overflowCount, 0, 0); // prevent tearing
    }

    static UINT64 GetTotalCount(SIZE_T threadLocalCountOffset, UINT64 *overflowCount);

public:
    static void IncrementWorkerThreadPoolCompletionCount(Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        IncrementCount(pThread, offsetof(Thread, m_workerThreadPoolCompletionCount), &s_workerThreadPoolCompletionCountOverflow);
    }

    static UINT64 GetWorkerThreadPoolCompletionCountOverflow()
    {
        WRAPPER_NO_CONTRACT;
        return GetOverflowCount(&s_workerThreadPoolCompletionCountOverflow);
    }

    static UINT64 GetTotalWorkerThreadPoolCompletionCount()
    {
        WRAPPER_NO_CONTRACT;
        return GetTotalCount(offsetof(Thread, m_workerThreadPoolCompletionCount), &s_workerThreadPoolCompletionCountOverflow);
    }

    static void IncrementIOThreadPoolCompletionCount(Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        IncrementCount(pThread, offsetof(Thread, m_ioThreadPoolCompletionCount), &s_ioThreadPoolCompletionCountOverflow);
    }

    static UINT64 GetIOThreadPoolCompletionCountOverflow()
    {
        WRAPPER_NO_CONTRACT;
        return GetOverflowCount(&s_ioThreadPoolCompletionCountOverflow);
    }

    static UINT64 GetTotalThreadPoolCompletionCount();

    static void IncrementMonitorLockContentionCount(Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        IncrementCount(pThread, offsetof(Thread, m_monitorLockContentionCount), &s_monitorLockContentionCountOverflow);
    }

    static UINT64 GetMonitorLockContentionCountOverflow()
    {
        WRAPPER_NO_CONTRACT;
        return GetOverflowCount(&s_monitorLockContentionCountOverflow);
    }

    static UINT64 GetTotalMonitorLockContentionCount()
    {
        WRAPPER_NO_CONTRACT;
        return GetTotalCount(offsetof(Thread, m_monitorLockContentionCount), &s_monitorLockContentionCountOverflow);
    }
#endif // !DACCESS_COMPILE

private:

    //-------------------------------------------------------------------------
    // Support creation of assemblies in DllMain (see ceemain.cpp)
    //-------------------------------------------------------------------------
    DomainFile* m_pLoadingFile;


public:

    void SetInteropDebuggingHijacked(BOOL f)
    {
        LIMITED_METHOD_CONTRACT;
        m_fInteropDebuggingHijacked = f;
    }
    BOOL GetInteropDebuggingHijacked()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fInteropDebuggingHijacked;
    }

    void SetFilterContext(T_CONTEXT *pContext);
    T_CONTEXT *GetFilterContext(void);

    void SetProfilerFilterContext(T_CONTEXT *pContext)
    {
        LIMITED_METHOD_CONTRACT;

        m_pProfilerFilterContext = pContext;
    }

    // Used by the profiler API to find which flags have been set on the Thread object,
    // in order to authorize a profiler's call into ICorProfilerInfo(2).
    DWORD GetProfilerCallbackFullState()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetThread() == this);
        return m_profilerCallbackState;
    }

    // Used by profiler API to set at once all callback flag bits stored on the Thread object.
    // Used to reinstate the previous state that had been modified by a previous call to
    // SetProfilerCallbackStateFlags
    void SetProfilerCallbackFullState(DWORD dwFullState)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetThread() == this);
        m_profilerCallbackState = dwFullState;
    }
    
    // Used by profiler API to set individual callback flags on the Thread object.
    // Returns the previous state of all flags.
    DWORD SetProfilerCallbackStateFlags(DWORD dwFlags)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(GetThread() == this);
        
        DWORD dwRet = m_profilerCallbackState;
        m_profilerCallbackState |= dwFlags;
        return dwRet;
    }

    T_CONTEXT *GetProfilerFilterContext(void)
    {
        LIMITED_METHOD_CONTRACT;
        return m_pProfilerFilterContext;
    }

#ifdef PROFILING_SUPPORTED

    FORCEINLINE DWORD GetProfilerEvacuationCounter(void)
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwProfilerEvacuationCounter;
    }

    FORCEINLINE void IncProfilerEvacuationCounter(void)
    {
        LIMITED_METHOD_CONTRACT;
        m_dwProfilerEvacuationCounter++;
        _ASSERTE(m_dwProfilerEvacuationCounter != 0U);
    }

    FORCEINLINE void DecProfilerEvacuationCounter(void)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_dwProfilerEvacuationCounter != 0U);
        m_dwProfilerEvacuationCounter--;
    }

#endif // PROFILING_SUPPORTED

    //-------------------------------------------------------------------------
    // The hijack lock enforces that a thread on which a profiler is currently
    // performing a stack walk cannot be hijacked.
    //
    // Note that the hijack lock cannot be managed by the host (i.e., this
    // cannot be a Crst), because this could lead to a deadlock:  YieldTask,
    // which is called by the host, may need to hijack, for which it would
    // need to take this lock - but since the host needs not be reentrant,
    // taking the lock cannot cause a call back into the host.
    //-------------------------------------------------------------------------
    static BOOL EnterHijackLock(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;

        return ::InterlockedCompareExchange(&(pThread->m_hijackLock), TRUE, FALSE) == FALSE;
    }

    static void LeaveHijackLock(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;

        pThread->m_hijackLock = FALSE;
    }

    typedef ConditionalStateHolder<Thread *, Thread::EnterHijackLock, Thread::LeaveHijackLock> HijackLockHolder;
    //-------------------------------------------------------------------------

    static bool ThreadsAtUnsafePlaces(void)
    {
        LIMITED_METHOD_CONTRACT;

        return (m_threadsAtUnsafePlaces != (LONG)0);
    }

    static void IncThreadsAtUnsafePlaces(void)
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement(&m_threadsAtUnsafePlaces);
    }

    static void DecThreadsAtUnsafePlaces(void)
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedDecrement(&m_threadsAtUnsafePlaces);
    }

    void PrepareForEERestart(BOOL SuspendSucceeded)
    {
        WRAPPER_NO_CONTRACT;

#ifdef FEATURE_HIJACK
        // Only unhijack the thread if the suspend succeeded. If it failed, 
        // the target thread may currently be using the original stack
        // location of the return address for something else.
        if (SuspendSucceeded)
            UnhijackThread();
#endif // FEATURE_HIJACK

        ResetThreadState(TS_GCSuspendPending);
    }

    void SetDebugCantStop(bool fCantStop);
    bool GetDebugCantStop(void);
    
    static LPVOID GetStaticFieldAddress(FieldDesc *pFD);
    TADDR GetStaticFieldAddrNoCreate(FieldDesc *pFD);
 
    void SetLoadingFile(DomainFile *pFile)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(m_pLoadingFile == NULL);
        m_pLoadingFile = pFile;
    }

    void ClearLoadingFile()
    {
        LIMITED_METHOD_CONTRACT;
        m_pLoadingFile = NULL;
    }

    DomainFile *GetLoadingFile()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoadingFile;
    }

private:
    static void LoadingFileRelease(Thread *pThread)
    {
        WRAPPER_NO_CONTRACT;
        pThread->ClearLoadingFile();
    }

public:
     typedef Holder<Thread *, DoNothing, Thread::LoadingFileRelease> LoadingFileHolder;

private:
    // Don't allow a thread to be asynchronously stopped or interrupted (e.g. because
    // it is performing a <clinit>)
    int         m_PreventAsync;
    int         m_PreventAbort;
    int         m_nNestedMarshalingExceptions;
    BOOL IsMarshalingException()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_nNestedMarshalingExceptions != 0);
    }
    int StartedMarshalingException()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nNestedMarshalingExceptions++;
    }
    void FinishedMarshalingException()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_nNestedMarshalingExceptions > 0);
        m_nNestedMarshalingExceptions--;
    }

    static LONG m_DebugWillSyncCount;

    // IP cache used by QueueCleanupIP.
    #define CLEANUP_IPS_PER_CHUNK 4
    struct CleanupIPs {
        IUnknown    *m_Slots[CLEANUP_IPS_PER_CHUNK];
        CleanupIPs  *m_Next;
        CleanupIPs() {LIMITED_METHOD_CONTRACT; memset(this, 0, sizeof(*this)); }
    };
    CleanupIPs   m_CleanupIPs;

#define BEGIN_FORBID_TYPELOAD() _ASSERTE_IMPL((GetThreadNULLOk() == 0) || ++GetThreadNULLOk()->m_ulForbidTypeLoad)
#define END_FORBID_TYPELOAD()   _ASSERTE_IMPL((GetThreadNULLOk() == 0) || GetThreadNULLOk()->m_ulForbidTypeLoad--)
#define TRIGGERS_TYPELOAD()     _ASSERTE_IMPL((GetThreadNULLOk() == 0) || !GetThreadNULLOk()->m_ulForbidTypeLoad)

#ifdef _DEBUG
public:
        DWORD m_GCOnTransitionsOK;
    ULONG  m_ulForbidTypeLoad;


/****************************************************************************/
/* The code below an attempt to catch people who don't protect GC pointers that
   they should be protecting.  Basically, OBJECTREF's constructor, adds the slot
   to a table.   When we protect a slot, we remove it from the table.  When GC
   could happen, all entries in the table are marked as bad.  When access to
   an OBJECTREF happens (the -> operator) we assert the slot is not bad.  To make
   this fast, the table is not perfect (there can be collisions), but this should
   not cause false positives, but it may allow errors to go undetected  */

#ifdef _WIN64
#define OBJREF_HASH_SHIFT_AMOUNT 3
#else // _WIN64
#define OBJREF_HASH_SHIFT_AMOUNT 2
#endif // _WIN64

        // For debugging, you may want to make this number very large, (8K)
        // should basically insure that no collisions happen
#define OBJREF_TABSIZE              256
        DWORD_PTR dangerousObjRefs[OBJREF_TABSIZE];      // Really objectRefs with lower bit stolen
        // m_allObjRefEntriesBad is TRUE iff dangerousObjRefs are all marked as GC happened
        // It's purely a perf optimization for debug builds that'll help for the cases where we make 2 successive calls
        // to Thread::TriggersGC. In that case, the entire array doesn't need to be walked and marked, since we just did
        // that. 
        BOOL m_allObjRefEntriesBad;

        static DWORD_PTR OBJREF_HASH;
        // Remembers that this object ref pointer is 'alive' and unprotected (Bad if GC happens)
        static void ObjectRefNew(const OBJECTREF* ref) {
            WRAPPER_NO_CONTRACT;
            Thread * curThread = GetThreadNULLOk();
            if (curThread == 0) return;

            curThread->dangerousObjRefs[((size_t)ref >> OBJREF_HASH_SHIFT_AMOUNT) % OBJREF_HASH] = (size_t)ref;
            curThread->m_allObjRefEntriesBad = FALSE;
        }

        static void ObjectRefAssign(const OBJECTREF* ref) {
            WRAPPER_NO_CONTRACT;
            Thread * curThread = GetThreadNULLOk();
            if (curThread == 0) return;

            curThread->m_allObjRefEntriesBad = FALSE;
            DWORD_PTR* slot = &curThread->dangerousObjRefs[((DWORD_PTR) ref >> OBJREF_HASH_SHIFT_AMOUNT) % OBJREF_HASH];
            if ((*slot & ~3) == (size_t) ref)
                *slot = *slot & ~1;                  // Don't care about GC's that have happened
        }

        // If an object is protected, it can be removed from the 'dangerous table'
        static void ObjectRefProtected(const OBJECTREF* ref) {
#ifdef USE_CHECKED_OBJECTREFS
            WRAPPER_NO_CONTRACT;
            _ASSERTE(IsObjRefValid(ref));
            Thread * curThread = GetThreadNULLOk();
            if (curThread == 0) return;

            curThread->m_allObjRefEntriesBad = FALSE;
            DWORD_PTR* slot = &curThread->dangerousObjRefs[((DWORD_PTR) ref >> OBJREF_HASH_SHIFT_AMOUNT) % OBJREF_HASH];
            if ((*slot & ~3) == (DWORD_PTR) ref)
                *slot = (size_t) ref | 2;                             // mark has being protected
#else
            LIMITED_METHOD_CONTRACT;
#endif
        }

        static bool IsObjRefValid(const OBJECTREF* ref) {
            WRAPPER_NO_CONTRACT;
            Thread * curThread = GetThreadNULLOk();
            if (curThread == 0) return(true);

            // If the object ref is NULL, we'll let it pass.
            if (*((DWORD_PTR*) ref) == 0)
                return(true);

            DWORD_PTR val = curThread->dangerousObjRefs[((DWORD_PTR) ref >> OBJREF_HASH_SHIFT_AMOUNT) % OBJREF_HASH];
            // if not in the table, or not the case that it was unprotected and GC happened, return true.
            if((val & ~3) != (size_t) ref || (val & 3) != 1)
                return(true);
            // If the pointer lives in the GC heap, than it is protected, and thus valid.
            if (dac_cast<TADDR>(g_lowest_address) <= val && val < dac_cast<TADDR>(g_highest_address))
                return(true);
            return(false);
        }

        // Clears the table.  Useful to do when crossing the managed-code - EE boundary
        // as you ususally only care about OBJECTREFS that have been created after that
        static void STDCALL ObjectRefFlush(Thread* thread);


#ifdef ENABLE_CONTRACTS_IMPL
        // Marks all Objrefs in the table as bad (since they are unprotected)
        static void TriggersGC(Thread* thread) {
            WRAPPER_NO_CONTRACT;
            if ((GCViolation|BadDebugState) & (UINT_PTR)(GetViolationMask()))
            {
                return;
            }
            if (!thread->m_allObjRefEntriesBad)
            {
                thread->m_allObjRefEntriesBad = TRUE;
            for(unsigned i = 0; i < OBJREF_TABSIZE; i++)
                thread->dangerousObjRefs[i] |= 1;                       // mark all slots as GC happened
        }
        }
#endif // ENABLE_CONTRACTS_IMPL

#endif // _DEBUG

private:
    PTR_CONTEXT m_pSavedRedirectContext;

    BOOL IsContextSafeToRedirect(T_CONTEXT* pContext);

public:
    PT_CONTEXT GetSavedRedirectContext()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pSavedRedirectContext);
    }

#ifndef DACCESS_COMPILE
    void     SetSavedRedirectContext(PT_CONTEXT pCtx)
    {
        LIMITED_METHOD_CONTRACT;
        m_pSavedRedirectContext = pCtx;
    }
#endif

    void EnsurePreallocatedContext();
    
    ThreadLocalBlock m_ThreadLocalBlock;

    // Called during AssemblyLoadContext teardown to clean up all structures
    // associated with thread statics for the specific Module
    void DeleteThreadStaticData(ModuleIndex index);

private:

    // Called during Thread death to clean up all structures
    // associated with thread statics
    void DeleteThreadStaticData();

#ifdef _DEBUG
private:
    // When we create an object, or create an OBJECTREF, or create an Interior Pointer, or enter EE from managed
    // code, we will set this flag.
    // Inside GCHeapUtilities::StressHeap, we only do GC if this flag is TRUE.  Then we reset it to zero.
    BOOL m_fStressHeapCount;
public:
    void EnableStressHeap()
    {
        LIMITED_METHOD_CONTRACT;
        m_fStressHeapCount = TRUE;
    }
    void DisableStressHeap()
    {
        LIMITED_METHOD_CONTRACT;
        m_fStressHeapCount = FALSE;
    }
    BOOL StressHeapIsEnabled()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fStressHeapCount;
    }

    size_t *m_pCleanedStackBase;
#endif

#ifdef DACCESS_COMPILE
public:
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    void EnumMemoryRegionsWorker(CLRDataEnumMemoryFlags flags);
#endif

public:
    // Is the current thread currently executing within a constrained execution region?
    static BOOL IsExecutingWithinCer();

    // Determine whether the method at the given frame in the thread's execution stack is executing within a CER.
    BOOL IsWithinCer(CrawlFrame *pCf);

private:
    // used to pad stack on thread creation to avoid aliasing penalty in P4 HyperThread scenarios

    static DWORD WINAPI intermediateThreadProc(PVOID arg);
    static int m_offset_counter;
    static const int offset_multiplier = 128;

    typedef struct {
        LPTHREAD_START_ROUTINE  lpThreadFunction;
        PVOID lpArg;
    } intermediateThreadParam;

#ifdef _DEBUG
// when the thread is doing a stressing GC, some Crst violation could be ignored, by a non-elegant solution.
private:
    BOOL m_bGCStressing; // the flag to indicate if the thread is doing a stressing GC
    BOOL m_bUniqueStacking; // the flag to indicate if the thread is doing a UniqueStack
public:
    BOOL GetGCStressing ()
    {
        return m_bGCStressing;
    }
    BOOL GetUniqueStacking ()
    {
        return m_bUniqueStacking;
    }
#endif

private:
    //-----------------------------------------------------------------------------
    // AVInRuntimeImplOkay : its okay to have an AV in Runtime implemetation while
    // this holder is in effect.
    //
    //  {
    //      AVInRuntimeImplOkayHolder foo();
    //  } // make AV's in the Runtime illegal on out of scope.
    //-----------------------------------------------------------------------------
    DWORD m_dwAVInRuntimeImplOkayCount;

    static void AVInRuntimeImplOkayAcquire(Thread * pThread)
    {
        LIMITED_METHOD_CONTRACT;

        if (pThread)
        {
            _ASSERTE(pThread->m_dwAVInRuntimeImplOkayCount != (DWORD)-1);
            pThread->m_dwAVInRuntimeImplOkayCount++;
        }
    }

    static void AVInRuntimeImplOkayRelease(Thread * pThread)
    {
        LIMITED_METHOD_CONTRACT;

        if (pThread)
        {
            _ASSERTE(pThread->m_dwAVInRuntimeImplOkayCount > 0);
            pThread->m_dwAVInRuntimeImplOkayCount--;
        }
    }

public:
    static BOOL AVInRuntimeImplOkay(void)
    {
        LIMITED_METHOD_CONTRACT;

        Thread * pThread = GetThreadNULLOk();

        if (pThread)
        {
            return (pThread->m_dwAVInRuntimeImplOkayCount > 0);
        }
        else
        {
            return FALSE;
        }
    }

    class AVInRuntimeImplOkayHolder
    {
        Thread * const m_pThread;
    public:
        AVInRuntimeImplOkayHolder() : 
            m_pThread(GetThread())
        {
            LIMITED_METHOD_CONTRACT;
            AVInRuntimeImplOkayAcquire(m_pThread);
        }
        AVInRuntimeImplOkayHolder(Thread * pThread) : 
            m_pThread(pThread)
        {
            LIMITED_METHOD_CONTRACT;
            AVInRuntimeImplOkayAcquire(m_pThread);
        }
        ~AVInRuntimeImplOkayHolder()
        {
            LIMITED_METHOD_CONTRACT;
            AVInRuntimeImplOkayRelease(m_pThread);
        }
    };
 
#ifdef _DEBUG
private:
    DWORD m_dwUnbreakableLockCount;
public:
    void IncUnbreakableLockCount()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (m_dwUnbreakableLockCount != (DWORD)-1);
        m_dwUnbreakableLockCount ++;
    }
    void DecUnbreakableLockCount()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (m_dwUnbreakableLockCount > 0);
        m_dwUnbreakableLockCount --;
    }
    BOOL HasUnbreakableLock() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwUnbreakableLockCount != 0;
    }
    DWORD GetUnbreakableLockCount() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwUnbreakableLockCount;
    }
#endif // _DEBUG

#ifdef _DEBUG
private:
    friend class FCallTransitionState;
    friend class PermitHelperMethodFrameState;
    friend class CompletedFCallTransitionState;
    HelperMethodFrameCallerList *m_pHelperMethodFrameCallerList;
#endif // _DEBUG

private:
    LONG m_dwHostTaskRefCount;

private:
    // If HasStarted fails, we cache the exception here, and rethrow on the thread which
    // calls Thread.Start.
    Exception* m_pExceptionDuringStartup;

public:
    void HandleThreadStartupFailure();

#ifdef HAVE_GCCOVER
private:
    BYTE* m_pbDestCode;
    BYTE* m_pbSrcCode;
#if defined(GCCOVER_TOLERATE_SPURIOUS_AV)
    LPVOID m_pLastAVAddress;
#endif // defined(GCCOVER_TOLERATE_SPURIOUS_AV)

public:
    void CommitGCStressInstructionUpdate();
    void PostGCStressInstructionUpdate(BYTE* pbDestCode, BYTE* pbSrcCode)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(!HasPendingGCStressInstructionUpdate());

        VolatileStoreWithoutBarrier<BYTE*>(&m_pbSrcCode, pbSrcCode);
        VolatileStore<BYTE*>(&m_pbDestCode, pbDestCode);
    }
    bool HasPendingGCStressInstructionUpdate()
    {
        LIMITED_METHOD_CONTRACT;
        BYTE* dest = VolatileLoad(&m_pbDestCode);
        return dest != NULL;
    }
    bool TryClearGCStressInstructionUpdate(BYTE** ppbDestCode, BYTE** ppbSrcCode)
    {
        LIMITED_METHOD_CONTRACT;
        bool result = false;

        if(HasPendingGCStressInstructionUpdate())
        {
            *ppbDestCode = FastInterlockExchangePointer(&m_pbDestCode, NULL);

            if(*ppbDestCode != NULL)
            {
                result = true;
                *ppbSrcCode = FastInterlockExchangePointer(&m_pbSrcCode, NULL);

                CONSISTENCY_CHECK(*ppbSrcCode != NULL);
            }
        }
        return result;
    }
#if defined(GCCOVER_TOLERATE_SPURIOUS_AV)
    void SetLastAVAddress(LPVOID address)
    {
        LIMITED_METHOD_CONTRACT;
        m_pLastAVAddress = address;
    }
    LPVOID GetLastAVAddress()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLastAVAddress;
    }
#endif // defined(GCCOVER_TOLERATE_SPURIOUS_AV)
#endif // HAVE_GCCOVER

private:
    BOOL m_fCompletionPortDrained;
public:
    void MarkCompletionPortDrained()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockExchange ((LONG*)&m_fCompletionPortDrained, TRUE);
    }
    void UnmarkCompletionPortDrained()
    {
        LIMITED_METHOD_CONTRACT;
        FastInterlockExchange ((LONG*)&m_fCompletionPortDrained, FALSE);
    }
    BOOL IsCompletionPortDrained()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fCompletionPortDrained;
    }

    // --------------------------------
    //  Store the maxReservedStackSize
    //  This is passed in from managed code in the thread constructor
    // ---------------------------------
private:
    SIZE_T m_RequestedStackSize;

public:

    // Get the MaxStackSize
    SIZE_T RequestedThreadStackSize()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_RequestedStackSize);
    }

    // Set the MaxStackSize
    void RequestedThreadStackSize(SIZE_T requestedStackSize)
    {
        LIMITED_METHOD_CONTRACT;
        m_RequestedStackSize = requestedStackSize;
    }

    static BOOL CheckThreadStackSize(SIZE_T *SizeToCommitOrReserve,
                                      BOOL   isSizeToReserve  // When TRUE, the previous argument is the stack size to reserve.
                                                              // Otherwise, it is the size to commit.
                                     );

    static BOOL GetProcessDefaultStackSize(SIZE_T* reserveSize, SIZE_T* commitSize);

private:

    // Although this is a pointer, it is used as a flag to indicate the current context is unsafe 
    // to inspect. When NULL the context is safe to use, otherwise it points to the active patch skipper
    // and the context is unsafe to use. When running a patch skipper we could be in one of two
    // debug-only situations that the context inspecting/modifying code isn't generally prepared
    // to deal with.
    // a) We have set the IP to point somewhere in the patch skip table but have not yet run the
    // instruction
    // b) We executed the instruction in the patch skip table and now the IP could be anywhere
    // The debugger may need to fix up the IP to compensate for the instruction being run
    // from a different address.
    VolatilePtr<DebuggerPatchSkip> m_debuggerActivePatchSkipper;

public:
    VOID BeginDebuggerPatchSkip(DebuggerPatchSkip* patchSkipper)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(!m_debuggerActivePatchSkipper.Load());
        FastInterlockExchangePointer(m_debuggerActivePatchSkipper.GetPointer(), patchSkipper);
        _ASSERTE(m_debuggerActivePatchSkipper.Load());
    }

    VOID EndDebuggerPatchSkip()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_debuggerActivePatchSkipper.Load());
        FastInterlockExchangePointer(m_debuggerActivePatchSkipper.GetPointer(), NULL);
        _ASSERTE(!m_debuggerActivePatchSkipper.Load());
    }

private:

    static BOOL EnterWorkingOnThreadContext(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;

        if(pThread->m_debuggerActivePatchSkipper.Load() != NULL)
        {
            return FALSE;
        }
        return TRUE;
    }

    static void LeaveWorkingOnThreadContext(Thread *pThread)
    {
        LIMITED_METHOD_CONTRACT;
    }

    typedef ConditionalStateHolder<Thread *, Thread::EnterWorkingOnThreadContext, Thread::LeaveWorkingOnThreadContext> WorkingOnThreadContextHolder;

public:
    void PrepareThreadForSOWork()
    {
        WRAPPER_NO_CONTRACT;

#ifdef FEATURE_HIJACK
        UnhijackThread();
#endif // FEATURE_HIJACK

        ResetThrowControlForThread();

        // Since this Thread has taken an SO, there may be state left-over after we
        // short-circuited exception or other error handling, and so we don't want
        // to risk recycling it.
        SetThreadStateNC(TSNC_CannotRecycle);
    }

    void SetSOWorkNeeded()
    {
        SetThreadStateNC(TSNC_SOWorkNeeded);
    }

    BOOL IsSOWorkNeeded()
    {
        return HasThreadStateNC(TSNC_SOWorkNeeded);
    }

    void FinishSOWork();

    void ClearExceptionStateAfterSO(void* pStackFrameSP)
    {
        WRAPPER_NO_CONTRACT;

        // Clear any stale exception state.
        m_ExceptionState.ClearExceptionStateAfterSO(pStackFrameSP);
    }

private:
    BOOL m_fAllowProfilerCallbacks;

public:
    //
    // These two methods are for profiler support.  The profiler clears the allowed
    // value once it has delivered a ThreadDestroyed callback, so that it does not
    // deliver any notifications to the profiler afterwards which reference this 
    // thread.  Callbacks on this thread which do not reference this thread are 
    // allowable.
    //
    BOOL ProfilerCallbacksAllowed(void)
    {
        return m_fAllowProfilerCallbacks;
    }

    void SetProfilerCallbacksAllowed(BOOL fValue)
    {
        m_fAllowProfilerCallbacks = fValue;
    }

private:
    //
    //This context is used for optimizations on I/O thread pool thread. In case the
    //overlapped structure is from a different appdomain, it is stored in this structure
    //to be processed later correctly by entering the right domain.  
    PVOID m_pIOCompletionContext;
    BOOL AllocateIOCompletionContext();
    VOID FreeIOCompletionContext();
public:
    inline PVOID GetIOCompletionContext()
    {
        return m_pIOCompletionContext;
    }    

private:
    // Inside a host, we don't own a thread handle, and we avoid DuplicateHandle call.
    // If a thread is dying after we obtain the thread handle, our SuspendThread may fail
    // because the handle may be closed and reused for a completely different type of handle.
    // To solve this problem, we have a counter m_dwThreadHandleBeingUsed.  Before we grab
    // the thread handle, we increment the counter.  Before we return a thread back to SQL
    // in Reset and ExitTask, we wait until the counter drops to 0.
    Volatile<LONG> m_dwThreadHandleBeingUsed;


private:
    static BOOL s_fCleanFinalizedThread;

public:
#ifndef DACCESS_COMPILE
    static void SetCleanupNeededForFinalizedThread()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (IsFinalizerThread());
        s_fCleanFinalizedThread = TRUE;
    }
#endif //!DACCESS_COMPILE

    static BOOL CleanupNeededForFinalizedThread()
    {
        LIMITED_METHOD_CONTRACT;
        return s_fCleanFinalizedThread;
    }

private:
    // When we create throwable for an exception, we need to run managed code.
    // If the same type of exception is thrown while creating managed object, like InvalidProgramException,
    // we may be in an infinite recursive case.
    Exception *m_pCreatingThrowableForException;
    friend OBJECTREF CLRException::GetThrowable();

#ifdef _DEBUG
private:
    int m_dwDisableAbortCheckCount; // Disable check before calling managed code.
                                    // !!! Use this very carefully.  If managed code runs user code
                                    // !!! or blocks on locks, the thread may not be aborted.
public:
    static void        DisableAbortCheck()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        FastInterlockIncrement((LONG*)&pThread->m_dwDisableAbortCheckCount);
    }
    static void        EnableAbortCheck()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        _ASSERTE (pThread->m_dwDisableAbortCheckCount > 0);
        FastInterlockDecrement((LONG*)&pThread->m_dwDisableAbortCheckCount);
    }

    BOOL IsAbortCheckDisabled()
    {
        return m_dwDisableAbortCheckCount > 0;
    }

    typedef StateHolder<Thread::DisableAbortCheck, Thread::EnableAbortCheck> DisableAbortCheckHolder;
#endif

private:
    // At the end of a catch, we may raise ThreadAbortException.  If catch clause set IP to resume in the
    // corresponding try block, our exception system will execute the same catch clause again and again.
    // So we save reference to the clause post which TA was reraised, which is used in ExceptionTracker::ProcessManagedCallFrame
    // to make ThreadAbort proceed ahead instead of going in a loop.
    // This problem only happens on Win64 due to JIT64.  The common scenario is VB's "On error resume next"
#ifdef WIN64EXCEPTIONS
    DWORD       m_dwIndexClauseForCatch;
    StackFrame  m_sfEstablisherOfActualHandlerFrame;
#endif // WIN64EXCEPTIONS

public:
    // Holds per-thread information the debugger uses to expose locking information
    // See ThreadDebugBlockingInfo.h for more details
    ThreadDebugBlockingInfo DebugBlockingInfo;

private:

    // Disables pumping and thread join in RCW creation
    bool m_fDisableComObjectEagerCleanup;

    // See ThreadStore::TriggerGCForDeadThreadsIfNecessary()
    bool m_fHasDeadThreadBeenConsideredForGCTrigger;

    CLRRandom m_random;

public:
    CLRRandom* GetRandom() {return &m_random;}

#ifdef FEATURE_COMINTEROP
private:
    // Cookie returned from CoRegisterInitializeSpy
    ULARGE_INTEGER m_uliInitializeSpyCookie;
    
    // True if m_uliInitializeSpyCookie is valid
    bool m_fInitializeSpyRegistered;

    // The last STA COM context we saw - used to speed up RCW creation
    LPVOID m_pLastSTACtxCookie;

public:
    inline void RevokeApartmentSpy();
    inline LPVOID GetLastSTACtxCookie(BOOL *pfNAContext);
    inline void SetLastSTACtxCookie(LPVOID pCtxCookie, BOOL fNAContext);
#endif // FEATURE_COMINTEROP

private:
    // This duplicates the ThreadType_GC bit stored in TLS (TlsIdx_ThreadType). It exists
    // so that any thread can query whether any other thread is a "GC Special" thread.
    // (In contrast, ::IsGCSpecialThread() only gives this info about the currently
    // executing thread.) The Profiling API uses this to determine whether it should
    // "hide" the thread from profilers. GC Special threads (in particular the bgc
    // thread) need to be hidden from profilers because the bgc thread creation path
    // occurs while the EE is suspended, and while the thread that's suspending the
    // runtime is waiting for the bgc thread to signal an event. The bgc thread cannot
    // switch to preemptive mode and call into a profiler at this time, or else a
    // deadlock will result when toggling back to cooperative mode (bgc thread toggling
    // to coop will block due to the suspension, and the thread suspending the runtime
    // continues to block waiting for the bgc thread to signal its creation events).
    // Furthermore, profilers have no need to be aware of GC special threads anyway,
    // since managed code never runs on them.
    bool m_fGCSpecial;

public:
    // Profiling API uses this to determine whether it should hide this thread from the
    // profiler.
    bool IsGCSpecial();

    // GC calls this when creating special threads that also happen to have an EE Thread
    // object associated with them (e.g., the bgc thread).
    void SetGCSpecial(bool fGCSpecial);

private:
#ifndef FEATURE_PAL
    WORD m_wCPUGroup;
    DWORD_PTR m_pAffinityMask;
#endif // !FEATURE_PAL
public:
    void ChooseThreadCPUGroupAffinity();
    void ClearThreadCPUGroupAffinity();

private:
    // Per thread table used to implement allocation sampling.
	AllLoggedTypes * m_pAllLoggedTypes;

public:
    AllLoggedTypes * GetAllocationSamplingTable()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pAllLoggedTypes;
    }

    void SetAllocationSamplingTable(AllLoggedTypes * pAllLoggedTypes)
    {
        LIMITED_METHOD_CONTRACT;

        // Assert if we try to set the m_pAllLoggedTypes to a non NULL value if it is already non-NULL.
        // This implies a memory leak.
        _ASSERTE(pAllLoggedTypes != NULL ? m_pAllLoggedTypes == NULL : TRUE);
        m_pAllLoggedTypes = pAllLoggedTypes;
    }

#ifdef FEATURE_PERFTRACING
private:

    // SampleProfiler thread state.  This is set on suspension and cleared before restart.
    // True if the thread was in cooperative mode.  False if it was in preemptive when the suspension started.
    Volatile<ULONG> m_gcModeOnSuspension;

    // The activity ID for the current thread.
    // An activity ID of zero means the thread is not executing in the context of an activity.
    GUID m_activityId;

public:
    bool GetGCModeOnSuspension()
    {
        LIMITED_METHOD_CONTRACT;
        return m_gcModeOnSuspension != 0U;
    }

    void SaveGCModeOnSuspension()
    {
        LIMITED_METHOD_CONTRACT;
        m_gcModeOnSuspension = m_fPreemptiveGCDisabled;
    }

    void ClearGCModeOnSuspension()
    {
        m_gcModeOnSuspension = 0;
    }

    LPCGUID GetActivityId() const
    {
        LIMITED_METHOD_CONTRACT;
        return &m_activityId;
    }

    void SetActivityId(LPCGUID pActivityId)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pActivityId != NULL);

        m_activityId = *pActivityId;
    }
#endif // FEATURE_PERFTRACING

#ifdef FEATURE_HIJACK
private:

    // By the time a frame is scanned by the runtime, m_pHijackReturnKind always 
    // identifies the gc-ness of the return register(s)
    // If the ReturnKind information is not available from the GcInfo, the runtime
    // computes it using the return types's class handle.

    ReturnKind m_HijackReturnKind;

public:

    ReturnKind GetHijackReturnKind()
    {
        LIMITED_METHOD_CONTRACT;

        return m_HijackReturnKind;
    }

    void SetHijackReturnKind(ReturnKind returnKind)
    {
        LIMITED_METHOD_CONTRACT;

        m_HijackReturnKind = returnKind;
    }
#endif // FEATURE_HIJACK

public:
    OBJECTHANDLE GetOrCreateDeserializationTracker();

private:
    OBJECTHANDLE m_DeserializationTracker;

public:
    static uint64_t dead_threads_non_alloc_bytes;

#ifndef DACCESS_COMPILE
public:
    class CurrentPrepareCodeConfigHolder
    {
    private:
        Thread *const m_thread;
#ifdef _DEBUG
        PrepareCodeConfig *const m_config;
#endif

    public:
        CurrentPrepareCodeConfigHolder(Thread *thread, PrepareCodeConfig *config);
        ~CurrentPrepareCodeConfigHolder();
    };

public:
    PrepareCodeConfig *GetCurrentPrepareCodeConfig() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_currentPrepareCodeConfig;
    }
#endif // !DACCESS_COMPILE

private:
    PrepareCodeConfig *m_currentPrepareCodeConfig;
};

// End of class Thread

typedef Thread::ForbidSuspendThreadHolder ForbidSuspendThreadHolder;
typedef Thread::ThreadPreventAsyncHolder ThreadPreventAsyncHolder;
typedef Thread::ThreadPreventAbortHolder ThreadPreventAbortHolder;

// Combines ForBindSuspendThreadHolder and CrstHolder into one.
class ForbidSuspendThreadCrstHolder
{
public:
    // Note: member initialization is intentionally ordered.
    ForbidSuspendThreadCrstHolder(CrstBase * pCrst)
        : m_forbid_suspend_holder()
        , m_lock_holder(pCrst)
    { WRAPPER_NO_CONTRACT; }

private:
    ForbidSuspendThreadHolder   m_forbid_suspend_holder;
    CrstHolder                  m_lock_holder;
};

ETaskType GetCurrentTaskType();



typedef Thread::AVInRuntimeImplOkayHolder AVInRuntimeImplOkayHolder;

BOOL RevertIfImpersonated(BOOL *bReverted, HANDLE *phToken);
void UndoRevert(BOOL bReverted, HANDLE hToken);

// ---------------------------------------------------------------------------
//
//      The ThreadStore manages all the threads in the system.
//
// There is one ThreadStore in the system, available through
// ThreadStore::m_pThreadStore.
// ---------------------------------------------------------------------------

typedef SList<Thread, false, PTR_Thread> ThreadList;


// The ThreadStore is a singleton class
#define CHECK_ONE_STORE()       _ASSERTE(this == ThreadStore::s_pThreadStore);

typedef DPTR(class ThreadStore) PTR_ThreadStore;
typedef DPTR(class ExceptionTracker) PTR_ExceptionTracker;

class ThreadStore
{
    friend class Thread;
    friend class ThreadSuspend;
    friend Thread* SetupThread(BOOL);
    friend class AppDomain;
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
    friend Thread* __stdcall DacGetThread(ULONG32 osThreadID);
#endif

public:

    ThreadStore();

    static void InitThreadStore();
    static void LockThreadStore();
    static void UnlockThreadStore();

    // Add a Thread to the ThreadStore
    // WARNING : only GC calls this with bRequiresTSL set to FALSE.
    static void AddThread(Thread *newThread, BOOL bRequiresTSL=TRUE);

    // RemoveThread finds the thread in the ThreadStore and discards it.
    static BOOL RemoveThread(Thread *target);

    static BOOL CanAcquireLock();

    // Transfer a thread from the unstarted to the started list.
    // WARNING : only GC calls this with bRequiresTSL set to FALSE.
    static void TransferStartedThread(Thread *target, BOOL bRequiresTSL=TRUE);

    // Before using the thread list, be sure to take the critical section.  Otherwise
    // it can change underneath you, perhaps leading to an exception after Remove.
    // Prev==NULL to get the first entry in the list.
    static Thread *GetAllThreadList(Thread *Prev, ULONG mask, ULONG bits);
    static Thread *GetThreadList(Thread *Prev);

    // Every EE process can lazily create a GUID that uniquely identifies it (for
    // purposes of remoting).
    const GUID    &GetUniqueEEId();

    // We shut down the EE when the last non-background thread terminates.  This event
    // is used to signal the main thread when this condition occurs.
    void            WaitForOtherThreads();
    static void     CheckForEEShutdown();
    CLREvent        m_TerminationEvent;
    
    // Have all the foreground threads completed?  In other words, can we release
    // the main thread?
    BOOL        OtherThreadsComplete()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_ThreadCount - m_UnstartedThreadCount - m_DeadThreadCount - Thread::m_ActiveDetachCount + m_PendingThreadCount >= m_BackgroundThreadCount);

        return (m_ThreadCount - m_UnstartedThreadCount - m_DeadThreadCount
                - Thread::m_ActiveDetachCount + m_PendingThreadCount
                == m_BackgroundThreadCount);
    }

    // If you want to trap threads re-entering the EE (be this for GC, or debugging,
    // or Thread.Suspend() or whatever, you need to TrapReturningThreads(TRUE).  When
    // you are finished snagging threads, call TrapReturningThreads(FALSE).  This
    // counts internally.
    //
    // Of course, you must also fix RareDisablePreemptiveGC to do the right thing
    // when the trap occurs.
    static void     TrapReturningThreads(BOOL yes);

private:

    // Enter and leave the critical section around the thread store.  Clients should
    // use LockThreadStore and UnlockThreadStore.
    void Enter();
    void Leave();

    // Critical section for adding and removing threads to the store
    Crst        m_Crst;

    // List of all the threads known to the ThreadStore (started & unstarted).
    ThreadList  m_ThreadList;

    // m_ThreadCount is the count of all threads in m_ThreadList.  This includes
    // background threads / unstarted threads / whatever.
    //
    // m_UnstartedThreadCount is the subset of m_ThreadCount that have not yet been
    // started.
    //
    // m_BackgroundThreadCount is the subset of m_ThreadCount that have been started
    // but which are running in the background.  So this is a misnomer in the sense
    // that unstarted background threads are not reflected in this count.
    //
    // m_PendingThreadCount is used to solve a race condition.  The main thread could
    // start another thread running and then exit.  The main thread might then start
    // tearing down the EE before the new thread moves itself out of m_UnstartedThread-
    // Count in TransferUnstartedThread.  This count is atomically bumped in
    // CreateNewThread, and atomically reduced within a locked thread store.
    //
    // m_DeadThreadCount is the subset of m_ThreadCount which have died.  The Win32
    // thread has disappeared, but something (like the exposed object) has kept the
    // refcount non-zero so we can't destruct yet.
    //
    // m_MaxThreadCount is the maximum value of m_ThreadCount. ie. the largest number
    // of simultaneously active threads

protected:
    LONG        m_ThreadCount;
    LONG        m_MaxThreadCount;
public:
    LONG        ThreadCountInEE ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ThreadCount;
    }
#if defined(_DEBUG) || defined(DACCESS_COMPILE)
    LONG        MaxThreadCountInEE ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_MaxThreadCount;
    }
#endif
private:
    LONG        m_UnstartedThreadCount;
    LONG        m_BackgroundThreadCount;
    LONG        m_PendingThreadCount;

    LONG        m_DeadThreadCount;
    LONG        m_DeadThreadCountForGCTrigger;
    bool        m_TriggerGCForDeadThreads;

private:
    // Space for the lazily-created GUID.
    GUID        m_EEGuid;
    BOOL        m_GuidCreated;

    // Even in the release product, we need to know what thread holds the lock on
    // the ThreadStore.  This is so we never deadlock when the GC thread halts a
    // thread that holds this lock.
    Thread     *m_HoldingThread;
    EEThreadId  m_holderthreadid;   // current holder (or NULL)

private:
    static LONG s_DeadThreadCountThresholdForGCTrigger;
    static DWORD s_DeadThreadGCTriggerPeriodMilliseconds;
    static SIZE_T *s_DeadThreadGenerationCounts;

public:

    static BOOL HoldingThreadStore()
    {
        WRAPPER_NO_CONTRACT;
        // Note that GetThread() may be 0 if it is the debugger thread
        // or perhaps a concurrent GC thread.
        return HoldingThreadStore(GetThread());
    }

    static BOOL HoldingThreadStore(Thread *pThread);

#ifdef DACCESS_COMPILE
    static void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    SPTR_DECL(ThreadStore, s_pThreadStore);

#ifdef _DEBUG
public:
    BOOL        DbgFindThread(Thread *target);
    LONG        DbgBackgroundThreadCount()
    {
        LIMITED_METHOD_CONTRACT;
        return m_BackgroundThreadCount;
    }

    BOOL IsCrstForThreadStore (const CrstBase* const pCrstBase)
    {
        LIMITED_METHOD_CONTRACT;
        return (void *)pCrstBase == (void*)&m_Crst;
    }

#endif
private:
    static CONTEXT *s_pOSContext;
public:
    // We can not do any memory allocation after we suspend a thread in order ot
    // avoid deadlock situation.
    static void AllocateOSContext();
    static CONTEXT *GrabOSContext();

private:
    // Thread abort needs to walk stack to decide if thread abort can proceed.
    // It is unsafe to crawl a stack of thread if the thread is OS-suspended which we do during
    // thread abort.  For example, Thread T1 aborts thread T2.  T2 is suspended by T1. Inside SQL
    // this means that no thread sharing the same scheduler with T2 can run.  If T1 needs a lock which
    // is owned by one thread on the scheduler, T1 will wait forever.
    // Our solution is to move T2 to a safe point, resume it, and then do stack crawl.
    static CLREvent *s_pWaitForStackCrawlEvent;
public:
    static void WaitForStackCrawlEvent()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            CAN_TAKE_LOCK;
        }
        CONTRACTL_END;
        s_pWaitForStackCrawlEvent->Wait(INFINITE,FALSE);
    }
    static void SetStackCrawlEvent()
    {
        LIMITED_METHOD_CONTRACT;
        s_pWaitForStackCrawlEvent->Set();
    }
    static void ResetStackCrawlEvent()
    {
        LIMITED_METHOD_CONTRACT;
        s_pWaitForStackCrawlEvent->Reset();
    }

private:
    void IncrementDeadThreadCountForGCTrigger();
    void DecrementDeadThreadCountForGCTrigger();
public:
    void OnMaxGenerationGCStarted();
    bool ShouldTriggerGCForDeadThreads();
    void TriggerGCForDeadThreadsIfNecessary();
};

struct TSSuspendHelper {
    static void SetTrap() { ThreadStore::TrapReturningThreads(TRUE); }
    static void UnsetTrap() { ThreadStore::TrapReturningThreads(FALSE); }
};
typedef StateHolder<TSSuspendHelper::SetTrap, TSSuspendHelper::UnsetTrap> TSSuspendHolder;

typedef StateHolder<ThreadStore::LockThreadStore,ThreadStore::UnlockThreadStore> ThreadStoreLockHolder;

#endif

// This class dispenses small thread ids for the thin lock mechanism.
// Recently we started using this class to dispense domain neutral module IDs as well.
class IdDispenser
{
private:
    DWORD       m_highestId;          // highest id given out so far
    SIZE_T      m_recycleBin;         // link list to chain all ids returning to us
    Crst        m_Crst;               // lock to protect our data structures
    DPTR(PTR_Thread)    m_idToThread;         // map thread ids to threads
    DWORD       m_idToThreadCapacity; // capacity of the map

#ifndef DACCESS_COMPILE
    void GrowIdToThread()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        DWORD newCapacity = m_idToThreadCapacity == 0 ? 16 : m_idToThreadCapacity*2;
        Thread **newIdToThread = new Thread*[newCapacity];

        newIdToThread[0] = NULL;

        for (DWORD i = 1; i < m_idToThreadCapacity; i++)
        {
            newIdToThread[i] = m_idToThread[i];
        }
        for (DWORD j = m_idToThreadCapacity; j < newCapacity; j++)
        {
            newIdToThread[j] = NULL;
        }
        delete[] m_idToThread;
        m_idToThread = newIdToThread;
        m_idToThreadCapacity = newCapacity;
    }
#endif // !DACCESS_COMPILE

public:
    IdDispenser() :
        // NOTE: CRST_UNSAFE_ANYMODE prevents a GC mode switch when entering this crst.
        // If you remove this flag, we will switch to preemptive mode when entering
        // m_Crst, which means all functions that enter it will become
        // GC_TRIGGERS.  (This includes all uses of CrstHolder.)  So be sure
        // to update the contracts if you remove this flag.
        m_Crst(CrstThreadIdDispenser, CRST_UNSAFE_ANYMODE)
    {
        WRAPPER_NO_CONTRACT;
        m_highestId = 0;
        m_recycleBin = 0;
        m_idToThreadCapacity = 0;
        m_idToThread = NULL;
    }

    ~IdDispenser()
    {
        LIMITED_METHOD_CONTRACT;
        delete[] m_idToThread;
    }

    bool IsValidId(DWORD id)
    {
        LIMITED_METHOD_CONTRACT;
        return (id > 0) && (id <= m_highestId);
    }

#ifndef DACCESS_COMPILE
    void NewId(Thread *pThread, DWORD & newId)
    {
        WRAPPER_NO_CONTRACT;
        DWORD result;
        CrstHolder ch(&m_Crst);

        if (m_recycleBin != 0)
        {
            _ASSERTE(FitsIn<DWORD>(m_recycleBin));
            result = static_cast<DWORD>(m_recycleBin);
            m_recycleBin = reinterpret_cast<SIZE_T>(m_idToThread[m_recycleBin]);
        }
        else
        {
            // we make sure ids don't wrap around - before they do, we always return the highest possible
            // one and rely on our caller to detect this situation
            if (m_highestId + 1 > m_highestId)
                m_highestId = m_highestId + 1;
            result = m_highestId;
            if (result >= m_idToThreadCapacity)
                GrowIdToThread();
        }

        _ASSERTE(result < m_idToThreadCapacity);
        newId = result;
        if (result < m_idToThreadCapacity)
            m_idToThread[result] = pThread;
    }
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
    void DisposeId(DWORD id)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            CAN_TAKE_LOCK;
        }
        CONTRACTL_END;
        CrstHolder ch(&m_Crst);

        _ASSERTE(IsValidId(id));
        if (id == m_highestId)
        {
            m_highestId--;
        }
        else
        {
            m_idToThread[id] = reinterpret_cast<PTR_Thread>(m_recycleBin);
            m_recycleBin = id;
#ifdef _DEBUG
            size_t index = (size_t)m_idToThread[id];
            while (index != 0)
            {
                _ASSERTE(index != id);
                index = (size_t)m_idToThread[index];
            }
#endif
        }
    }
#endif // !DACCESS_COMPILE

    Thread *IdToThread(DWORD id)
    {
        LIMITED_METHOD_CONTRACT;
        CrstHolder ch(&m_Crst);

        Thread *result = NULL;
        if (id <= m_highestId)
            result = m_idToThread[id];
        // m_idToThread may have Thread*, or the next free slot
        _ASSERTE ((size_t)result > m_idToThreadCapacity);

        return result;
    }

    Thread *IdToThreadWithValidation(DWORD id)
    {
        WRAPPER_NO_CONTRACT;

        CrstHolder ch(&m_Crst);

        Thread *result = NULL;
        if (id <= m_highestId)
            result = m_idToThread[id];
        // m_idToThread may have Thread*, or the next free slot
        if ((size_t)result <= m_idToThreadCapacity)
            result = NULL;
        _ASSERTE(result == NULL || ((size_t)result & 0x3) == 0 || ((Thread*)result)->GetThreadId() == id);
        return result;
    }
};
typedef DPTR(IdDispenser) PTR_IdDispenser;

#ifndef CROSSGEN_COMPILE

// Dispenser of small thread ids for thin lock mechanism
GPTR_DECL(IdDispenser,g_pThinLockThreadIdDispenser);

// forward declaration
DWORD MsgWaitHelper(int numWaiters, HANDLE* phEvent, BOOL bWaitAll, DWORD millis, BOOL alertable = FALSE);

// When a thread is being created after a debug suspension has started, it sends an event up to the
// debugger. Afterwards, with the Debugger Lock still held, it will check to see if we had already asked to suspend the
// Runtime. If we have, then it will turn around and call this to set the debug suspend pending flag on the newly
// created thread, since it was missed by SysStartSuspendForDebug as it didn't exist when that function was run.
//
inline void Thread::MarkForDebugSuspend(void)
{
    WRAPPER_NO_CONTRACT;
    if (!(m_State & TS_DebugSuspendPending))
    {
        FastInterlockOr((ULONG *) &m_State, TS_DebugSuspendPending);
        ThreadStore::TrapReturningThreads(TRUE);
    }
}

// Debugger per-thread flag for enabling notification on "manual"
// method calls, for stepping logic.

inline void Thread::IncrementTraceCallCount()
{
    WRAPPER_NO_CONTRACT;
    FastInterlockIncrement(&m_TraceCallCount);
    ThreadStore::TrapReturningThreads(TRUE);
}

inline void Thread::DecrementTraceCallCount()
{
    WRAPPER_NO_CONTRACT;
    ThreadStore::TrapReturningThreads(FALSE);
    FastInterlockDecrement(&m_TraceCallCount);
}

// When we enter an Object.Wait() we are logically inside the synchronized
// region of that object.  Of course, we've actually completely left the region,
// or else nobody could Notify us.  But if we throw ThreadInterruptedException to
// break out of the Wait, all the catchers are going to expect the synchronized
// state to be correct.  So we carry it around in case we need to restore it.
struct PendingSync
{
    LONG            m_EnterCount;
    WaitEventLink  *m_WaitEventLink;
#ifdef _DEBUG
    Thread         *m_OwnerThread;
#endif

    PendingSync(WaitEventLink *s) : m_WaitEventLink(s)
    {
        WRAPPER_NO_CONTRACT;
#ifdef _DEBUG
        m_OwnerThread = GetThread();
#endif
    }
    void Restore(BOOL bRemoveFromSB);
};


#define INCTHREADLOCKCOUNT() { }
#define DECTHREADLOCKCOUNT() { }
#define INCTHREADLOCKCOUNTTHREAD(thread) { }
#define DECTHREADLOCKCOUNTTHREAD(thread) { }


// --------------------------------------------------------------------------------
// GCHolder is used to implement the normal GCX_ macros.
//
// GCHolder is normally used indirectly through GCX_ convenience macros, but can be used
// directly if needed (e.g. due to multiple holders in one scope, or to use
// in class definitions).
//
// GCHolder (or derived types) should only be instantiated as automatic variables
// --------------------------------------------------------------------------------

#ifdef ENABLE_CONTRACTS_IMPL
#define GCHOLDER_CONTRACT_ARGS_NoDtor   , false, szConstruct, szFunction, szFile, lineNum
#define GCHOLDER_CONTRACT_ARGS_HasDtor  , true,  szConstruct, szFunction, szFile, lineNum
#define GCHOLDER_DECLARE_CONTRACT_ARGS_BARE \
          const char * szConstruct = "Unknown" \
        , const char * szFunction = "Unknown" \
        , const char * szFile = "Unknown" \
        , int lineNum = 0
#define GCHOLDER_DECLARE_CONTRACT_ARGS , GCHOLDER_DECLARE_CONTRACT_ARGS_BARE
#define GCHOLDER_DECLARE_CONTRACT_ARGS_INTERNAL , bool fPushStackRecord = true, GCHOLDER_DECLARE_CONTRACT_ARGS_BARE

#define GCHOLDER_SETUP_CONTRACT_STACK_RECORD(mode)                                  \
        m_fPushedRecord = false;                                                    \
                                                                                    \
        if (fPushStackRecord && conditional)                                        \
        {                                                                           \
            m_pClrDebugState = GetClrDebugState();                                  \
            m_oldClrDebugState = *m_pClrDebugState;                                 \
                                                                                    \
            m_pClrDebugState->ViolationMaskReset( ModeViolation );                  \
                                                                                    \
            m_ContractStackRecord.m_szFunction = szFunction;                        \
            m_ContractStackRecord.m_szFile     = szFile;                            \
            m_ContractStackRecord.m_lineNum    = lineNum;                           \
            m_ContractStackRecord.m_testmask   =                                    \
                  (Contract::ALL_Disabled & ~((UINT)(Contract::MODE_Mask)))         \
                | (mode);                                                           \
            m_ContractStackRecord.m_construct  = szConstruct;                       \
            m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );     \
            m_fPushedRecord = true;                                                 \
        }                                                                           
#define GCHOLDER_CHECK_FOR_PREEMP_IN_NOTRIGGER(pThread)                                         \
            if (pThread->GCNoTrigger())                                                         \
            {                                                                                   \
                CONTRACT_ASSERT("Coop->preemp->coop switch attempted in a GC_NOTRIGGER scope",  \
                                Contract::GC_NoTrigger,                                         \
                                Contract::GC_Mask,                                              \
                                szFunction,                                                     \
                                szFile,                                                         \
                                lineNum                                                         \
                                );                                                              \
            }                                                                                   
#else
#define GCHOLDER_CONTRACT_ARGS_NoDtor
#define GCHOLDER_CONTRACT_ARGS_HasDtor
#define GCHOLDER_DECLARE_CONTRACT_ARGS_BARE
#define GCHOLDER_DECLARE_CONTRACT_ARGS
#define GCHOLDER_DECLARE_CONTRACT_ARGS_INTERNAL
#define GCHOLDER_SETUP_CONTRACT_STACK_RECORD(mode)
#define GCHOLDER_CHECK_FOR_PREEMP_IN_NOTRIGGER(pThread)
#endif // ENABLE_CONTRACTS_IMPL

#ifndef DACCESS_COMPILE
class GCHolderBase
{
protected:
    // NOTE: This method is FORCEINLINE'ed into its callers, but the callers are just the 
    // corresponding methods in the derived types, not all sites that use GC holders.  This
    // is done so that the #pragma optimize will take affect since the optimize settings
    // are taken from the template instantiation site, not the template definition site.
    template <BOOL THREAD_EXISTS>
    FORCEINLINE_NONDEBUG
    void PopInternal()
    {
        SCAN_SCOPE_END;
        WRAPPER_NO_CONTRACT;

#ifdef ENABLE_CONTRACTS_IMPL
        if (m_fPushedRecord)
        {
            *m_pClrDebugState = m_oldClrDebugState;
        }
        // Make sure that we're using the version of this template that matches the 
        // invariant setup in EnterInternal{Coop|Preemp}{_HackNoThread}
        _ASSERTE(!!THREAD_EXISTS == m_fThreadMustExist);
#endif

        if (m_WasCoop)
        {
            // m_WasCoop is only TRUE if we've already verified there's an EE thread.
            BEGIN_GETTHREAD_ALLOWED;

            _ASSERTE(m_Thread != NULL);  // Cannot switch to cooperative with no thread
            if (!m_Thread->PreemptiveGCDisabled())
                m_Thread->DisablePreemptiveGC();

            END_GETTHREAD_ALLOWED;
        }
        else
        {
            // Either we initialized m_Thread explicitly with GetThread() in the
            // constructor, or our caller (instantiator of GCHolder) called our constructor
            // with GetThread() (which we already asserted in the constuctor)
            // (i.e., m_Thread == GetThread()).  Also, note that if THREAD_EXISTS,
            // then m_Thread must be non-null (as it's == GetThread()).  So the
            // "if" below looks a little hokey since we're checking for either condition.
            // But the template param THREAD_EXISTS allows us to statically early-out
            // when it's TRUE, so we check it for perf.
            if (THREAD_EXISTS || m_Thread != NULL)
            {
                BEGIN_GETTHREAD_ALLOWED;
                if (m_Thread->PreemptiveGCDisabled())
                    m_Thread->EnablePreemptiveGC();
                END_GETTHREAD_ALLOWED;
            }
        }

        // If we have a thread then we assert that we ended up in the same state
        // which we started in.
        if (THREAD_EXISTS || m_Thread != NULL)
        {
            _ASSERTE(!!m_WasCoop == !!(m_Thread->PreemptiveGCDisabled()));
        }
    }

    // NOTE: The rest of these methods are all FORCEINLINE so that the uses where 'conditional==true' 
    // can have the if-checks removed by the compiler.  The callers are just the corresponding methods
    // in the derived types, not all sites that use GC holders.  

    
    // This is broken - there is a potential race with the GC thread.  It is currently
    // used for a few cases where (a) we potentially haven't started up the EE yet, or
    // (b) we are on a "special thread".  We need a real solution here though.
    FORCEINLINE_NONDEBUG 
    void EnterInternalCoop_HackNoThread(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS_INTERNAL)
    {
        GCHOLDER_SETUP_CONTRACT_STACK_RECORD(Contract::MODE_Coop);

        m_Thread = GetThreadNULLOk();

#ifdef ENABLE_CONTRACTS_IMPL
        m_fThreadMustExist = false;
#endif // ENABLE_CONTRACTS_IMPL

        if (m_Thread != NULL)
        {
            BEGIN_GETTHREAD_ALLOWED;
            m_WasCoop = m_Thread->PreemptiveGCDisabled();

            if (conditional && !m_WasCoop)
            {
                m_Thread->DisablePreemptiveGC();
                _ASSERTE(m_Thread->PreemptiveGCDisabled());
            }
            END_GETTHREAD_ALLOWED;
        }
        else
        {
            m_WasCoop = FALSE;
        }
    }

    FORCEINLINE_NONDEBUG 
    void EnterInternalPreemp(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS_INTERNAL)
    {
        GCHOLDER_SETUP_CONTRACT_STACK_RECORD(Contract::MODE_Preempt);

        m_Thread = GetThreadNULLOk();

#ifdef ENABLE_CONTRACTS_IMPL
        m_fThreadMustExist = false;
        if (m_Thread != NULL && conditional)
        {
            BEGIN_GETTHREAD_ALLOWED;
            GCHOLDER_CHECK_FOR_PREEMP_IN_NOTRIGGER(m_Thread);
            END_GETTHREAD_ALLOWED;
        }
#endif  // ENABLE_CONTRACTS_IMPL

        if (m_Thread != NULL)
        {
            BEGIN_GETTHREAD_ALLOWED;
            m_WasCoop = m_Thread->PreemptiveGCDisabled();

            if (conditional && m_WasCoop)
            {
                m_Thread->EnablePreemptiveGC();
                _ASSERTE(!m_Thread->PreemptiveGCDisabled());
            }
            END_GETTHREAD_ALLOWED;
        }
        else
        {
            m_WasCoop = FALSE;
        }
    }

    FORCEINLINE_NONDEBUG 
    void EnterInternalCoop(Thread *pThread, bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS_INTERNAL)
    {
        // This is the perf version. So we deliberately restrict the calls
        // to already setup threads to avoid the null checks and GetThread call
        _ASSERTE(pThread && (pThread == GetThread()));
#ifdef ENABLE_CONTRACTS_IMPL
        m_fThreadMustExist = true;
#endif // ENABLE_CONTRACTS_IMPL

        GCHOLDER_SETUP_CONTRACT_STACK_RECORD(Contract::MODE_Coop);

        m_Thread = pThread;
        m_WasCoop = m_Thread->PreemptiveGCDisabled();
        if (conditional && !m_WasCoop)
        {
            m_Thread->DisablePreemptiveGC();
            _ASSERTE(m_Thread->PreemptiveGCDisabled());
        }
    }

    template <BOOL THREAD_EXISTS>
    FORCEINLINE_NONDEBUG 
    void EnterInternalPreemp(Thread *pThread, bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS_INTERNAL)
    {
        // This is the perf version. So we deliberately restrict the calls
        // to already setup threads to avoid the null checks and GetThread call
        _ASSERTE(!THREAD_EXISTS || (pThread && (pThread == GetThread())));
#ifdef ENABLE_CONTRACTS_IMPL
        m_fThreadMustExist = !!THREAD_EXISTS;
#endif // ENABLE_CONTRACTS_IMPL

        GCHOLDER_SETUP_CONTRACT_STACK_RECORD(Contract::MODE_Preempt);

        m_Thread = pThread;

        if (THREAD_EXISTS || (m_Thread != NULL))
        {
            GCHOLDER_CHECK_FOR_PREEMP_IN_NOTRIGGER(m_Thread);
            m_WasCoop = m_Thread->PreemptiveGCDisabled();
            if (conditional && m_WasCoop)
            {
                m_Thread->EnablePreemptiveGC();
                _ASSERTE(!m_Thread->PreemptiveGCDisabled());
            }
        }
        else
        {
            m_WasCoop = FALSE;
        }
    }

private:
    Thread * m_Thread;
    BOOL     m_WasCoop;         // This is BOOL and not 'bool' because PreemptiveGCDisabled returns BOOL,
                                // so the codegen is better if we don't have to convert to 'bool'.
#ifdef ENABLE_CONTRACTS_IMPL
    bool                m_fThreadMustExist;     // used to validate that the proper Pop<THREAD_EXISTS> method is used
    bool                m_fPushedRecord;
    ClrDebugState       m_oldClrDebugState;
    ClrDebugState      *m_pClrDebugState;
    ContractStackRecord m_ContractStackRecord;
#endif
};

class GCCoopNoDtor : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    void Enter(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        WRAPPER_NO_CONTRACT;
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_COOPERATIVE;
        }
        // The thread must be non-null to enter MODE_COOP
        this->EnterInternalCoop(GetThread(), conditional GCHOLDER_CONTRACT_ARGS_NoDtor);
    }

    DEBUG_NOINLINE 
    void Leave()
    {
        WRAPPER_NO_CONTRACT;
        SCAN_SCOPE_BEGIN;
        this->PopInternal<TRUE>();  // Thread must be non-NULL
    }
};

class GCPreempNoDtor : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    void Enter(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_PREEMPTIVE;
        }

        this->EnterInternalPreemp(conditional GCHOLDER_CONTRACT_ARGS_NoDtor);
    }

    DEBUG_NOINLINE 
    void Enter(Thread * pThreadNullOk, bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_PREEMPTIVE;
        }

        this->EnterInternalPreemp<FALSE>( // Thread may be NULL
            pThreadNullOk, conditional GCHOLDER_CONTRACT_ARGS_NoDtor);
    }

    DEBUG_NOINLINE 
    void Leave()
    {
        SCAN_SCOPE_END;
        this->PopInternal<FALSE>(); // Thread may be NULL
    }
};

class GCCoop : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    GCCoop(GCHOLDER_DECLARE_CONTRACT_ARGS_BARE)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_COOPERATIVE;

        // The thread must be non-null to enter MODE_COOP
        this->EnterInternalCoop(GetThread(), true GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE 
    GCCoop(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_COOPERATIVE;
        }

        // The thread must be non-null to enter MODE_COOP
        this->EnterInternalCoop(GetThread(), conditional GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE
    ~GCCoop()
    {
        SCAN_SCOPE_END;
        this->PopInternal<TRUE>();  // Thread must be non-NULL
    }
};

// This is broken - there is a potential race with the GC thread.  It is currently
// used for a few cases where (a) we potentially haven't started up the EE yet, or
// (b) we are on a "special thread".  We need a real solution here though.
class GCCoopHackNoThread : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    GCCoopHackNoThread(GCHOLDER_DECLARE_CONTRACT_ARGS_BARE)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_COOPERATIVE;

        this->EnterInternalCoop_HackNoThread(true GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE 
    GCCoopHackNoThread(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_COOPERATIVE;
        }

        this->EnterInternalCoop_HackNoThread(conditional GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE
    ~GCCoopHackNoThread()
    {
        SCAN_SCOPE_END;
        this->PopInternal<FALSE>();  // Thread might be NULL
    }
};

class GCCoopThreadExists : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    GCCoopThreadExists(Thread * pThread GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_COOPERATIVE;

        this->EnterInternalCoop(pThread, true GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE 
    GCCoopThreadExists(Thread * pThread, bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_COOPERATIVE;
        }

        this->EnterInternalCoop(pThread, conditional GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE
    ~GCCoopThreadExists()
    {
        SCAN_SCOPE_END;
        this->PopInternal<TRUE>();  // Thread must be non-NULL
    }
};

class GCPreemp : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    GCPreemp(GCHOLDER_DECLARE_CONTRACT_ARGS_BARE)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_PREEMPTIVE;

        this->EnterInternalPreemp(true GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE 
    GCPreemp(bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_PREEMPTIVE;
        }

        this->EnterInternalPreemp(conditional GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE
    ~GCPreemp()
    {
        SCAN_SCOPE_END;
        this->PopInternal<FALSE>(); // Thread may be NULL
    }
};

class GCPreempThreadExists : public GCHolderBase
{
public:
    DEBUG_NOINLINE 
    GCPreempThreadExists(Thread * pThread GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_PREEMPTIVE;

        this->EnterInternalPreemp<TRUE>(    // Thread must be non-NULL
                pThread, true GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE 
    GCPreempThreadExists(Thread * pThread, bool conditional GCHOLDER_DECLARE_CONTRACT_ARGS)
    {
        SCAN_SCOPE_BEGIN;
        if (conditional)
        {
            STATIC_CONTRACT_MODE_PREEMPTIVE;
        }    

        this->EnterInternalPreemp<TRUE>(    // Thread must be non-NULL
                pThread, conditional GCHOLDER_CONTRACT_ARGS_HasDtor);
    }

    DEBUG_NOINLINE
    ~GCPreempThreadExists()
    {
        SCAN_SCOPE_END;
        this->PopInternal<TRUE>();  // Thread must be non-NULL
    }
};
#endif // DACCESS_COMPILE


// --------------------------------------------------------------------------------
// GCAssert is used to implement the assert GCX_ macros. Usage is similar to GCHolder.
//
// GCAsserting for preemptive mode automatically passes on unmanaged threads.
//
// Note that the assert is "2 sided"; it happens on entering and on leaving scope, to
// help ensure mode integrity.
//
// GCAssert is a noop in a free build
// --------------------------------------------------------------------------------

template<BOOL COOPERATIVE>
class GCAssert
{
    public:
    DEBUG_NOINLINE void BeginGCAssert();
    DEBUG_NOINLINE void EndGCAssert()
    {
        SCAN_SCOPE_END;
    }
};

template<BOOL COOPERATIVE>
class AutoCleanupGCAssert
{
#ifdef _DEBUG_IMPL
public:
    DEBUG_NOINLINE AutoCleanupGCAssert();

    DEBUG_NOINLINE ~AutoCleanupGCAssert()
    {
        SCAN_SCOPE_END;
        WRAPPER_NO_CONTRACT;
        // This is currently disabled; we currently have a lot of code which doesn't
        // back out the GC mode properly (instead relying on the EX_TRY macros.)
        //
        // @todo enable this when we remove raw GC mode switching.
#if 0
        DoCheck();
#endif
    }

    private:
    FORCEINLINE void DoCheck()
    {
        WRAPPER_NO_CONTRACT;
        Thread *pThread = GetThread();
        if (COOPERATIVE)
        {
            _ASSERTE(pThread != NULL);
            _ASSERTE(pThread->PreemptiveGCDisabled());
        }
        else
        {
            _ASSERTE(pThread == NULL || !(pThread->PreemptiveGCDisabled()));
        }
    }
#endif
};


// --------------------------------------------------------------------------------
// GCForbid is used to add ForbidGC semantics to the current GC mode.  Note that
// it requires the thread to be in cooperative mode already.
//
// GCForbid is a noop in a free build
// --------------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
class GCForbid : AutoCleanupGCAssert<TRUE>
{
#ifdef ENABLE_CONTRACTS_IMPL
 public:
    DEBUG_NOINLINE GCForbid(BOOL fConditional, const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_BEGIN;
        if (fConditional)
        {
            STATIC_CONTRACT_MODE_COOPERATIVE;
            STATIC_CONTRACT_GC_NOTRIGGER;
        }

        m_fConditional = fConditional;
        if (m_fConditional)
        {
            Thread *pThread = GetThread();
            m_pClrDebugState = pThread ? pThread->GetClrDebugState() : ::GetClrDebugState();
            m_oldClrDebugState = *m_pClrDebugState;

            m_pClrDebugState->ViolationMaskReset( GCViolation );

            GetThread()->BeginForbidGC(szFile, lineNum);

            m_ContractStackRecord.m_szFunction = szFunction;
            m_ContractStackRecord.m_szFile     = (char*)szFile;
            m_ContractStackRecord.m_lineNum    = lineNum;
            m_ContractStackRecord.m_testmask   = (Contract::ALL_Disabled & ~((UINT)(Contract::GC_Mask))) | Contract::GC_NoTrigger;
            m_ContractStackRecord.m_construct  = "GCX_FORBID";
            m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );
        }
    }

    DEBUG_NOINLINE GCForbid(const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_COOPERATIVE;
        STATIC_CONTRACT_GC_NOTRIGGER;

        m_fConditional = TRUE;

        Thread *pThread = GetThread();
        m_pClrDebugState = pThread ? pThread->GetClrDebugState() : ::GetClrDebugState();
        m_oldClrDebugState = *m_pClrDebugState;

        m_pClrDebugState->ViolationMaskReset( GCViolation );

        GetThread()->BeginForbidGC(szFile, lineNum);

        m_ContractStackRecord.m_szFunction = szFunction;
        m_ContractStackRecord.m_szFile     = (char*)szFile;
        m_ContractStackRecord.m_lineNum    = lineNum;
        m_ContractStackRecord.m_testmask   = (Contract::ALL_Disabled & ~((UINT)(Contract::GC_Mask))) | Contract::GC_NoTrigger;
        m_ContractStackRecord.m_construct  = "GCX_FORBID";
        m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );
    }

    DEBUG_NOINLINE ~GCForbid()
    {
        SCAN_SCOPE_END;

        if (m_fConditional)
        {
            GetThread()->EndForbidGC();
            *m_pClrDebugState = m_oldClrDebugState;
        }
    }

  private:
    BOOL                m_fConditional;
    ClrDebugState      *m_pClrDebugState;
    ClrDebugState       m_oldClrDebugState;
    ContractStackRecord m_ContractStackRecord;
#endif  // _DEBUG_IMPL
};
#endif // !DACCESS_COMPILE

// --------------------------------------------------------------------------------
// GCNoTrigger is used to add NoTriggerGC semantics to the current GC mode.  Unlike
// GCForbid, it does not require a thread to be in cooperative mode.
//
// GCNoTrigger is a noop in a free build
// --------------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
class GCNoTrigger
{
#ifdef ENABLE_CONTRACTS_IMPL
 public:
    DEBUG_NOINLINE GCNoTrigger(BOOL fConditional, const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_BEGIN;
        if (fConditional)
        {
            STATIC_CONTRACT_GC_NOTRIGGER;
        }

        m_fConditional = fConditional;
        
        if (m_fConditional)
        {
            Thread * pThread = GetThreadNULLOk();
            m_pClrDebugState = pThread ? pThread->GetClrDebugState() : ::GetClrDebugState();
            m_oldClrDebugState = *m_pClrDebugState;

            m_pClrDebugState->ViolationMaskReset( GCViolation );

            if (pThread != NULL)
            {
                pThread->BeginNoTriggerGC(szFile, lineNum);
            }

            m_ContractStackRecord.m_szFunction = szFunction;
            m_ContractStackRecord.m_szFile     = (char*)szFile;
            m_ContractStackRecord.m_lineNum    = lineNum;
            m_ContractStackRecord.m_testmask   = (Contract::ALL_Disabled & ~((UINT)(Contract::GC_Mask))) | Contract::GC_NoTrigger;
            m_ContractStackRecord.m_construct  = "GCX_NOTRIGGER";
            m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );
        }
    }

    DEBUG_NOINLINE GCNoTrigger(const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_GC_NOTRIGGER;

        m_fConditional = TRUE;

        Thread * pThread = GetThreadNULLOk();
        m_pClrDebugState = pThread ? pThread->GetClrDebugState() : ::GetClrDebugState();
        m_oldClrDebugState = *m_pClrDebugState;

        m_pClrDebugState->ViolationMaskReset( GCViolation );

        if (pThread != NULL)
        {
            pThread->BeginNoTriggerGC(szFile, lineNum);
        }

        m_ContractStackRecord.m_szFunction = szFunction;
        m_ContractStackRecord.m_szFile     = (char*)szFile;
        m_ContractStackRecord.m_lineNum    = lineNum;
        m_ContractStackRecord.m_testmask   = (Contract::ALL_Disabled & ~((UINT)(Contract::GC_Mask))) | Contract::GC_NoTrigger;
        m_ContractStackRecord.m_construct  = "GCX_NOTRIGGER";
        m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );
    }

    DEBUG_NOINLINE ~GCNoTrigger()
    {
        SCAN_SCOPE_END;

        if (m_fConditional)
        {
            Thread * pThread = GetThreadNULLOk();
            if (pThread)
            {
               pThread->EndNoTriggerGC();
            }
            *m_pClrDebugState = m_oldClrDebugState;
        }
    }

 private:
    BOOL m_fConditional;
    ClrDebugState      *m_pClrDebugState;
    ClrDebugState       m_oldClrDebugState;
    ContractStackRecord m_ContractStackRecord;
#endif  // _DEBUG_IMPL
};
#endif //!DACCESS_COMPILE

class CoopTransitionHolder
{
    Frame * m_pFrame;

public:
    CoopTransitionHolder(Thread * pThread)
        : m_pFrame(pThread->m_pFrame)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~CoopTransitionHolder()
    {
        WRAPPER_NO_CONTRACT;
        if (m_pFrame != NULL)
            COMPlusCooperativeTransitionHandler(m_pFrame);
    }

    void SuppressRelease()
    {
        LIMITED_METHOD_CONTRACT;
        // FRAME_TOP and NULL must be distinct values.
        // static_assert_no_msg(FRAME_TOP_VALUE != NULL);
        m_pFrame = NULL;
    }
};

// --------------------------------------------------------------------------------
// GCX macros - see util.hpp
// --------------------------------------------------------------------------------

#ifdef _DEBUG_IMPL

// Normally, any thread we operate on has a Thread block in its TLS.  But there are
// a few special threads we don't normally execute managed code on.
BOOL dbgOnly_IsSpecialEEThread();
void dbgOnly_IdentifySpecialEEThread();

#ifdef USE_CHECKED_OBJECTREFS
#define ASSERT_PROTECTED(objRef)        Thread::ObjectRefProtected(objRef)
#else
#define ASSERT_PROTECTED(objRef)
#endif

#else

#define ASSERT_PROTECTED(objRef)

#endif


#ifdef ENABLE_CONTRACTS_IMPL

#define BEGINFORBIDGC() {if (GetThreadNULLOk() != NULL) GetThreadNULLOk()->BeginForbidGC(__FILE__, __LINE__);}
#define ENDFORBIDGC()   {if (GetThreadNULLOk() != NULL) GetThreadNULLOk()->EndForbidGC();}

class FCallGCCanTrigger
{
public:
    static DEBUG_NOINLINE void Enter()
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_GC_TRIGGERS;
        Thread * pThread = GetThreadNULLOk();
        if (pThread != NULL)
        {
            Enter(pThread);
        }
    }

    static DEBUG_NOINLINE void Enter(Thread* pThread)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_GC_TRIGGERS;
        pThread->EndForbidGC();
    }

    static DEBUG_NOINLINE void Leave(const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_END;
        Thread * pThread = GetThreadNULLOk();
        if (pThread != NULL)
        {
            Leave(pThread, szFunction, szFile, lineNum);
        }
    }

    static DEBUG_NOINLINE void Leave(Thread* pThread, const char *szFunction, const char *szFile, int lineNum)
    {
        SCAN_SCOPE_END;
        pThread->BeginForbidGC(szFile, lineNum);
    }
};

#define TRIGGERSGC_NOSTOMP()  do {                                           \
                            ANNOTATION_GC_TRIGGERS;                         \
                            Thread* curThread = GetThread();                \
                            if(curThread->GCNoTrigger())                    \
                            {                                               \
                                CONTRACT_ASSERT("TRIGGERSGC found in a GC_NOTRIGGER region.", Contract::GC_NoTrigger, Contract::GC_Mask, __FUNCTION__, __FILE__, __LINE__); \
                            }                                               \
                        } while(0)


#define TRIGGERSGC()    do {                                                \
                            TRIGGERSGC_NOSTOMP();                           \
                            Thread::TriggersGC(GetThread());                \
                        } while(0)

#else // ENABLE_CONTRACTS_IMPL

#define BEGINFORBIDGC()
#define ENDFORBIDGC()
#define TRIGGERSGC_NOSTOMP() ANNOTATION_GC_TRIGGERS
#define TRIGGERSGC() ANNOTATION_GC_TRIGGERS

#endif // ENABLE_CONTRACTS_IMPL

inline BOOL GC_ON_TRANSITIONS(BOOL val) {
    WRAPPER_NO_CONTRACT;
#ifdef _DEBUG
    Thread* thread = GetThread();
    if (thread == 0)
        return(FALSE);
    BOOL ret = thread->m_GCOnTransitionsOK;
    thread->m_GCOnTransitionsOK = val;
    return(ret);
#else // _DEBUG
    return FALSE;
#endif // !_DEBUG
}

#ifdef _DEBUG
inline void ENABLESTRESSHEAP() {
    WRAPPER_NO_CONTRACT;
    Thread * thread = GetThreadNULLOk();
    if (thread) {
        thread->EnableStressHeap();
    }
}

void CleanStackForFastGCStress ();
#define CLEANSTACKFORFASTGCSTRESS()                                         \
if (g_pConfig->GetGCStressLevel() && g_pConfig->FastGCStressLevel() > 1) {   \
    CleanStackForFastGCStress ();                                            \
}

#else   // _DEBUG
#define CLEANSTACKFORFASTGCSTRESS()

#endif  // _DEBUG

#ifdef _DEBUG_IMPL
// Holder for incrementing the ForbidGCLoaderUse counter.
class GCForbidLoaderUseHolder
{
 public:
    GCForbidLoaderUseHolder()
    {
        WRAPPER_NO_CONTRACT;
        ClrFlsIncrementValue(TlsIdx_ForbidGCLoaderUseCount, 1);
    }

    ~GCForbidLoaderUseHolder()
    {
        WRAPPER_NO_CONTRACT;
        ClrFlsIncrementValue(TlsIdx_ForbidGCLoaderUseCount, -1);
    }
};

#endif

// Declaring this macro turns off the GC_TRIGGERS/THROWS/INJECT_FAULT contract in LoadTypeHandle.
// If you do this, you must restrict your use of the loader only to retrieve TypeHandles
// for types that have already been loaded and resolved. If you fail to observe this restriction, you will
// reach a GC_TRIGGERS point somewhere in the loader and assert. If you're lucky, that is.
// (If you're not lucky, you will introduce a GC hole.)
//
// The main user of this workaround is the GC stack crawl. It must parse signatures and retrieve
// type handles for valuetypes in method parameters. Some other uses have creeped into the codebase -
// some justified, others not.
//
// ENABLE_FORBID_GC_LOADER is *not* the same as using tokenNotToLoad to suppress loading.
// You should use tokenNotToLoad in preference to ENABLE_FORBID. ENABLE_FORBID is a fragile
// workaround and places enormous responsibilities on the caller. The only reason it exists at all
// is that the GC stack crawl simply cannot tolerate exceptions or new GC's - that's an immovable
// rock we're faced with.
//
// The key differences are:
//
//      ENABLE_FORBID                                   tokenNotToLoad
//      --------------------------------------------    ------------------------------------------------------
//      caller must guarantee the type is already       caller does not have to guarantee the type
//        loaded - otherwise, we will crash badly.        is already loaded.
//
//      loader will not throw, trigger gc or OOM        loader may throw, trigger GC or OOM.
//
//
//
#ifdef ENABLE_CONTRACTS_IMPL
#define ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE()    GCForbidLoaderUseHolder __gcfluh; \
                                                       CANNOTTHROWCOMPLUSEXCEPTION();  \
                                                       GCX_NOTRIGGER(); \
                                                       FAULT_FORBID();
#else   // _DEBUG_IMPL
#define ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE()    ;
#endif  // _DEBUG_IMPL
// This macro lets us define a conditional CONTRACT for the GC_TRIGGERS behavior.
// This is for the benefit of a select group of callers that use the loader
// in ForbidGC mode strictly to retrieve existing TypeHandles. The reason
// we use a threadstate rather than an extra parameter is that these annoying
// callers call the loader through intermediaries (MetaSig) and it proved to be too
// cumbersome to pass this state down through all those callers.
//
// Don't make GC_TRIGGERS conditional just because your function ends up calling
// LoadTypeHandle indirectly. We don't want to proliferate conditonal contracts more
// than necessary so declare such functions as GC_TRIGGERS until the need
// for the conditional contract is actually proven through code inspection or
// coverage.
#if defined(DACCESS_COMPILE)

// Disable (<non-zero constant> || <expression>) is always a non-zero constant. 
// <expression> is never evaluated and might have side effects, because 
// FORBIDGC_LOADER_USE_ENABLED is used in that pattern and additionally the rule
// has little value.
#ifdef _PREFAST_
#pragma warning(disable:6286)
#endif
#define FORBIDGC_LOADER_USE_ENABLED() true

#else // DACCESS_COMPILE
#if defined (_DEBUG_IMPL) || defined(_PREFAST_)
#ifndef DACCESS_COMPILE 
#define FORBIDGC_LOADER_USE_ENABLED() (ClrFlsGetValue(TlsIdx_ForbidGCLoaderUseCount))
#else 
#define FORBIDGC_LOADER_USE_ENABLED() TRUE 
#endif
#else   // _DEBUG_IMPL

// If you got an error about FORBIDGC_LOADER_USE_ENABLED being undefined, it's because you tried
// to use this predicate in a free build outside of a CONTRACT or ASSERT.
//
#define FORBIDGC_LOADER_USE_ENABLED() (sizeof(YouCannotUseThisHere) != 0)
#endif  // _DEBUG_IMPL
#endif // DACCESS_COMPILE

// We have numerous places where we start up a managed thread.  This includes several places in the
// ThreadPool, the 'new Thread(...).Start()' case, and the Finalizer.  Try to factor the code so our
// base exception handling behavior is consistent across those places.  The resulting code is convoluted,
// but it's better than the prior situation of each thread being on a different plan.

// If you add a new kind of managed thread (i.e. thread proc) to the system, you must:
//
// 1) Call HasStarted() before calling any ManagedThreadBase_* routine.
// 2) Define a ManagedThreadBase_* routine for your scenario and declare it below.
// 3) Always perform any AD transitions through the ManagedThreadBase_* mechanism.
// 4) Allow the ManagedThreadBase_* mechanism to perform all your exception handling, including
//    dispatching of unhandled exception events, deciding what to swallow, etc.
// 5) If you must separate your base thread proc behavior from your AD transitioning behavior,
//    define a second ManagedThreadADCall_* helper and declare it below.
// 6) Never decide this is too much work and that you will roll your own thread proc code.

// intentionally opaque.
struct ManagedThreadCallState;

struct ManagedThreadBase
{
    // The 'new Thread(...).Start()' case from COMSynchronizable kickoff thread worker
    static void KickOff(ADCallBackFcnType pTarget,
                        LPVOID args);

    // The IOCompletion, QueueUserWorkItem, AddTimer, RegisterWaitForSingleObject cases in
    // the ThreadPool
    static void ThreadPool(ADCallBackFcnType pTarget, LPVOID args);

    // The Finalizer thread uses this path
    static void FinalizerBase(ADCallBackFcnType pTarget);
};


// DeadlockAwareLock is a base for building deadlock-aware locks.
// Note that DeadlockAwareLock only works if ALL locks involved in the deadlock are deadlock aware.

class DeadlockAwareLock
{
 private:
    VolatilePtr<Thread> m_pHoldingThread;
#ifdef _DEBUG
    const char  *m_description;
#endif

 public:
    DeadlockAwareLock(const char *description = NULL);
    ~DeadlockAwareLock();

    // Test for deadlock
    BOOL CanEnterLock();

    // Call BeginEnterLock before attempting to acquire the lock
    BOOL TryBeginEnterLock(); // returns FALSE if deadlock
    void BeginEnterLock(); // Asserts if deadlock

    // Call EndEnterLock after acquiring the lock
    void EndEnterLock();

    // Call LeaveLock after releasing the lock
    void LeaveLock();

    const char *GetDescription();

 private:
    CHECK CheckDeadlock(Thread *pThread);

    static void ReleaseBlockingLock()
    {
        Thread *pThread = GetThread();
        _ASSERTE (pThread);
        pThread->m_pBlockingLock = NULL;
    }
public:
    typedef StateHolder<DoNothing,DeadlockAwareLock::ReleaseBlockingLock> BlockingLockHolder;
};

inline void SetTypeHandleOnThreadForAlloc(TypeHandle th)
{
    // We are doing this unconditionally even though th is only used by ETW events in GC. When the ETW
    // event is not enabled we still need to set it because it may not be enabled here but by the 
    // time we are checking in GC, the event is enabled - we don't want GC to read a random value
    // from before in this case.
    GetThread()->SetTHAllocContextObj(th);
}

#endif // CROSSGEN_COMPILE

class Compiler;
// users of OFFSETOF__TLS__tls_CurrentThread macro expect the offset of these variables wrt to _tls_start to be stable. 
// Defining each of the following thread local variable separately without the struct causes the offsets to change in 
// different flavors of build. Eg. in chk build the offset of m_pThread is 0x4 while in ret build it becomes 0x8 as 0x4 is  
// occupied by m_pAddDomain. Packing all thread local variables in a struct and making struct instance to be thread local
// ensures that the offsets of the variables are stable in all build flavors.
struct ThreadLocalInfo
{
    Thread* m_pThread;
    AppDomain* m_pAppDomain; // This field is read only by the SOS plugin to get the AppDomain
    void** m_EETlsData; // ClrTlsInfo::data
};

class ThreadStateHolder
{
public:
    ThreadStateHolder (BOOL fNeed, DWORD state)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (GetThread());
        m_fNeed = fNeed;
        m_state = state;
    }
    ~ThreadStateHolder ()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_fNeed)
        {
            Thread *pThread = GetThread();
            _ASSERTE (pThread);
            FastInterlockAnd((ULONG *) &pThread->m_State, ~m_state);
        }
    }
private:
    BOOL m_fNeed;
    DWORD m_state;
};

// Sets an NC threadstate if not already set, and restores the old state
// of that bit upon destruction

// fNeed > 0,   make sure state is set, restored in destructor
// fNeed = 0,   no change
// fNeed < 0,   make sure state is reset, restored in destructor

class ThreadStateNCStackHolder
{
    public:
    ThreadStateNCStackHolder (BOOL fNeed, Thread::ThreadStateNoConcurrency state)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE (GetThread());
        m_fNeed = fNeed;
        m_state = state;

        if (fNeed)
        {
            Thread *pThread = GetThread();
            _ASSERTE (pThread);

            if (fNeed < 0)
            {
                // if the state is set, reset it
                if (pThread->HasThreadStateNC(state))
                {
                    pThread->ResetThreadStateNC(m_state);
                }
                else
                {
                    m_fNeed = FALSE;
                }
            }
            else
            {
                // if the state is already set then no change is
                // necessary during the back out
                if(pThread->HasThreadStateNC(state))
                {
                    m_fNeed = FALSE;
                }
                else
                {
                    pThread->SetThreadStateNC(state);
                }
            }
        }
    }
    
    ~ThreadStateNCStackHolder()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_fNeed)
        {
            Thread *pThread = GetThread();
            _ASSERTE (pThread);

            if (m_fNeed < 0)
            {
                pThread->SetThreadStateNC(m_state); // set it
            }
            else
            {
                pThread->ResetThreadStateNC(m_state);
            }
        }
    }

private:
    BOOL m_fNeed;
    Thread::ThreadStateNoConcurrency m_state;
};

BOOL Debug_IsLockedViaThreadSuspension();

#endif //__threads_h__

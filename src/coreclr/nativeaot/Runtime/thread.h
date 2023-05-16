// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __thread_h__
#define __thread_h__

#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "forward_declarations.h"

struct gc_alloc_context;
class RuntimeInstance;
class ThreadStore;
class CLREventStatic;
class Thread;

#ifdef TARGET_UNIX
#include "UnixContext.h"
#endif

// The offsets of some fields in the thread (in particular, m_pTransitionFrame) are known to the compiler and get
// inlined into the code.  Let's make sure they don't change just because we enable/disable server GC in a particular
// runtime build.
#define KEEP_THREAD_LAYOUT_CONSTANT

#ifndef HOST_64BIT
# if defined(FEATURE_SVR_GC) || defined(KEEP_THREAD_LAYOUT_CONSTANT)
#  define SIZEOF_ALLOC_CONTEXT 40
# else
#  define SIZEOF_ALLOC_CONTEXT 28
# endif
#else // HOST_64BIT
# if defined(FEATURE_SVR_GC) || defined(KEEP_THREAD_LAYOUT_CONSTANT)
#  define SIZEOF_ALLOC_CONTEXT 56
# else
#  define SIZEOF_ALLOC_CONTEXT 40
# endif
#endif // HOST_64BIT

#define TOP_OF_STACK_MARKER ((PInvokeTransitionFrame*)(ptrdiff_t)-1)

// the thread has been interrupted and context for the interruption point
// can be retrieved via GetInterruptedContext()
#define INTERRUPTED_THREAD_MARKER ((PInvokeTransitionFrame*)(ptrdiff_t)-2)

typedef DPTR(PAL_LIMITED_CONTEXT) PTR_PAL_LIMITED_CONTEXT;

struct ExInfo;
typedef DPTR(ExInfo) PTR_ExInfo;

// Also defined in ExceptionHandling.cs, layouts must match.
// When adding new fields to this struct, ensure they get properly initialized in the exception handling
// assembly stubs
struct ExInfo
{

    PTR_ExInfo              m_pPrevExInfo;
    PTR_PAL_LIMITED_CONTEXT m_pExContext;
    PTR_Object              m_exception;  // actual object reference, specially reported by GcScanRootsWorker
    ExKind                  m_kind;
    uint8_t                 m_passNumber;
    uint32_t                m_idxCurClause;
    StackFrameIterator      m_frameIter;
    volatile void*          m_notifyDebuggerSP;
};

struct GCFrameRegistration
{
    Thread* m_pThread;
    GCFrameRegistration* m_pNext;
    void** m_pObjRefs;
    uint32_t m_numObjRefs;
    int m_MaybeInterior;
};

struct InlinedThreadStaticRoot
{
    Object* m_threadStaticsBase;
    InlinedThreadStaticRoot* m_next;
};

struct ThreadBuffer
{
    uint8_t                 m_rgbAllocContextBuffer[SIZEOF_ALLOC_CONTEXT];
    uint32_t volatile       m_ThreadStateFlags;                     // see Thread::ThreadStateFlags enum
    PInvokeTransitionFrame* m_pTransitionFrame;
    PInvokeTransitionFrame* m_pDeferredTransitionFrame;             // see Thread::EnablePreemptiveMode
    PInvokeTransitionFrame* m_pCachedTransitionFrame;
    PTR_Thread              m_pNext;                                // used by ThreadStore's SList<Thread>
    HANDLE                  m_hPalThread;                           // WARNING: this may legitimately be INVALID_HANDLE_VALUE
    void **                 m_ppvHijackedReturnAddressLocation;
    void *                  m_pvHijackedReturnAddress;
    uintptr_t               m_uHijackedReturnValueFlags;            
    PTR_ExInfo              m_pExInfoStackHead;
    Object*                 m_threadAbortException;                 // ThreadAbortException instance -set only during thread abort
    Object*                 m_pThreadLocalStatics;
    InlinedThreadStaticRoot* m_pInlinedThreadLocalStatics;
    GCFrameRegistration*    m_pGCFrameRegistrations;
    PTR_VOID                m_pStackLow;
    PTR_VOID                m_pStackHigh;
    EEThreadId              m_threadId;                             // OS thread ID
    PTR_VOID                m_pThreadStressLog;                     // pointer to head of thread's StressLogChunks
    NATIVE_CONTEXT*         m_interruptedContext;                   // context for an asynchronously interrupted thread.
#ifdef FEATURE_SUSPEND_REDIRECTION
    uint8_t*                m_redirectionContextBuffer;             // storage for redirection context, allocated on demand
#endif //FEATURE_SUSPEND_REDIRECTION

#ifdef FEATURE_GC_STRESS
    uint32_t                m_uRand;                                // current per-thread random number
#endif // FEATURE_GC_STRESS
};

struct ReversePInvokeFrame
{
    PInvokeTransitionFrame*   m_savedPInvokeTransitionFrame;
    Thread* m_savedThread;
};

class Thread : private ThreadBuffer
{
    friend class AsmOffsets;
    friend struct DefaultSListTraits<Thread>;
    friend class ThreadStore;
    IN_DAC(friend class ClrDataAccess;)

public:
    enum ThreadStateFlags
    {
        TSF_Unknown             = 0x00000000,       // Threads are created in this state
        TSF_Attached            = 0x00000001,       // Thread was inited by first U->M transition on this thread
        TSF_Detached            = 0x00000002,       // Thread was detached by DllMain
        TSF_SuppressGcStress    = 0x00000008,       // Do not allow gc stress on this thread, used in DllMain
                                                    // ...and on the Finalizer thread
        TSF_DoNotTriggerGc      = 0x00000010,       // Do not allow hijacking of this thread, also intended to
                                                    // ...be checked during allocations in debug builds.
        TSF_IsGcSpecialThread   = 0x00000020,       // Set to indicate a GC worker thread used for background GC
#ifdef FEATURE_GC_STRESS
        TSF_IsRandSeedSet       = 0x00000040,       // set to indicate the random number generator for GCStress was inited
#endif // FEATURE_GC_STRESS

#ifdef FEATURE_SUSPEND_REDIRECTION
        TSF_Redirected          = 0x00000080,       // Set to indicate the thread is redirected and will inevitably
                                                    // suspend once resumed.
                                                    // If we see this flag, we skip hijacking as an optimization.
#endif //FEATURE_SUSPEND_REDIRECTION

        TSF_ActivationPending   = 0x00000100,       // An APC with QUEUE_USER_APC_FLAGS_SPECIAL_USER_APC can interrupt another APC.
                                                    // For suspension APCs it is mostly harmless, but wasteful and in extreme
                                                    // cases may force the target thread into stack oveflow.
                                                    // We use this flag to avoid sending another APC when one is still going through.
                                                    // 
                                                    // On Unix this is an optimization to not queue up more signals when one is
                                                    // still being processed.
    };
private:

    void Construct();

    void SetState(ThreadStateFlags flags);
    void ClearState(ThreadStateFlags flags);
    bool IsStateSet(ThreadStateFlags flags);


    static void HijackCallback(NATIVE_CONTEXT* pThreadContext, void* pThreadToHijack);

    //
    // Hijack funcs are not called, they are "returned to". And when done, they return to the actual caller.
    // Thus they cannot have any parameters or return anything.
    //
    typedef void HijackFunc();

    void HijackReturnAddress(PAL_LIMITED_CONTEXT* pSuspendCtx, HijackFunc* pfnHijackFunction);
    void HijackReturnAddress(NATIVE_CONTEXT* pSuspendCtx, HijackFunc* pfnHijackFunction);
    void HijackReturnAddressWorker(StackFrameIterator* frameIterator, HijackFunc* pfnHijackFunction);
    bool InlineSuspend(NATIVE_CONTEXT* interruptedContext);

#ifdef FEATURE_SUSPEND_REDIRECTION
    bool Redirect();
#endif //FEATURE_SUSPEND_REDIRECTION

    bool CacheTransitionFrameForSuspend();
    void ResetCachedTransitionFrame();
    void CrossThreadUnhijack();
    void UnhijackWorker();
    void EnsureRuntimeInitialized();

    //
    // SyncState members
    //
    PInvokeTransitionFrame* GetTransitionFrame();

    void GcScanRootsWorker(void * pfnEnumCallback, void * pvCallbackData, StackFrameIterator & sfIter);

public:
    // First phase of thread destructor, disposes stuff related to GC.
    // Executed with thread store lock taken so GC cannot happen.
    void Detach();
    // Second phase of thread destructor.
    // Executed without thread store lock taken.
    void Destroy();

    bool                IsInitialized();

    gc_alloc_context *  GetAllocContext();

#ifndef DACCESS_COMPILE
    uint64_t            GetPalThreadIdForLogging();
    bool                IsCurrentThread();

    void                GcScanRoots(void * pfnEnumCallback, void * pvCallbackData);
#else
    typedef void GcScanRootsCallbackFunc(PTR_RtuObjectRef ppObject, void* token, uint32_t flags);
    bool GcScanRoots(GcScanRootsCallbackFunc * pfnCallback, void * token, PTR_PAL_LIMITED_CONTEXT pInitialContext);
#endif

    void                Hijack();
    void                Unhijack();
    bool                IsHijacked();
    void*               GetHijackedReturnAddress();

#ifdef FEATURE_GC_STRESS
    static void         HijackForGcStress(PAL_LIMITED_CONTEXT * pSuspendCtx);
#endif // FEATURE_GC_STRESS

    bool                IsSuppressGcStressSet();
    void                SetSuppressGcStress();
    void                ClearSuppressGcStress();
    bool                IsWithinStackBounds(PTR_VOID p);
    void                GetStackBounds(PTR_VOID * ppStackLow, PTR_VOID * ppStackHigh);
    void                PushExInfo(ExInfo * pExInfo);
    void                ValidateExInfoPop(ExInfo * pExInfo, void * limitSP);
    void                ValidateExInfoStack();
    bool                IsDoNotTriggerGcSet();
    void                SetDoNotTriggerGc();
    void                ClearDoNotTriggerGc();

    bool                IsDetached();
    void                SetDetached();

    PTR_VOID            GetThreadStressLog() const;
#ifndef DACCESS_COMPILE
    void                SetThreadStressLog(void * ptsl);
#endif // DACCESS_COMPILE
#ifdef FEATURE_GC_STRESS
    void                SetRandomSeed(uint32_t seed);
    uint32_t            NextRand();
    bool                IsRandInited();
#endif // FEATURE_GC_STRESS
    PTR_ExInfo          GetCurExInfo();

    bool                IsCurrentThreadInCooperativeMode();

    PInvokeTransitionFrame* GetTransitionFrameForStackTrace();
    void *              GetCurrentThreadPInvokeReturnAddress();

    static bool         IsHijackTarget(void * address);

    //
    // The set of operations used to support unmanaged code running in cooperative mode
    //
    void                EnablePreemptiveMode();
    void                DisablePreemptiveMode();

    // Set the m_pDeferredTransitionFrame field for GC allocation helpers that setup transition frame
    // in assembly code. Do not use anywhere else.
    void                SetDeferredTransitionFrame(PInvokeTransitionFrame* pTransitionFrame);

    // Setup the m_pDeferredTransitionFrame field for GC helpers entered via regular PInvoke.
    // Do not use anywhere else.
    void                DeferTransitionFrame();

    //
    // GC support APIs - do not use except from GC itself
    //
    void SetGCSpecial();
    bool IsGCSpecial();
    bool CatchAtSafePoint();

    //
    // Managed/unmanaged interop transitions support APIs
    //
    void WaitForGC(PInvokeTransitionFrame* pTransitionFrame);

    void ReversePInvokeAttachOrTrapThread(ReversePInvokeFrame * pFrame);

    bool InlineTryFastReversePInvoke(ReversePInvokeFrame * pFrame);
    void InlineReversePInvokeReturn(ReversePInvokeFrame * pFrame);

    void InlinePInvoke(PInvokeTransitionFrame * pFrame);
    void InlinePInvokeReturn(PInvokeTransitionFrame * pFrame);

    Object* GetThreadAbortException();
    void SetThreadAbortException(Object *exception);

    Object** GetThreadStaticStorage();

    InlinedThreadStaticRoot* GetInlinedThreadStaticList();
    void RegisterInlinedThreadStaticRoot(InlinedThreadStaticRoot* newRoot);

    NATIVE_CONTEXT* GetInterruptedContext();

    void PushGCFrameRegistration(GCFrameRegistration* pRegistration);
    void PopGCFrameRegistration(GCFrameRegistration* pRegistration);

#ifdef FEATURE_SUSPEND_REDIRECTION
    NATIVE_CONTEXT* EnsureRedirectionContext();
#endif //FEATURE_SUSPEND_REDIRECTION

    bool                IsActivationPending();
    void                SetActivationPending(bool isPending);
};

#ifndef __GCENV_BASE_INCLUDED__
typedef DPTR(Object) PTR_Object;
typedef DPTR(PTR_Object) PTR_PTR_Object;
#endif // !__GCENV_BASE_INCLUDED__
#ifdef DACCESS_COMPILE

// The DAC uses DebuggerEnumGcRefContext in place of a GCCONTEXT when doing reference
// enumeration. The GC passes through additional data in the ScanContext which the debugger
// neither has nor needs. While we could refactor the GC code to make an interface
// with less coupling, that might affect perf or make integration messier. Instead
// we use some typedefs so DAC and runtime can get strong yet distinct types.


// Ideally we wouldn't need this wrapper, but PromoteCarefully needs access to the
// thread and a promotion field. We aren't assuming the user's token will have this data.
struct DacScanCallbackData
{
    Thread* thread_under_crawl;               // the thread being scanned
    bool promotion;                           // are we emulating the GC promote phase or relocate phase?
                                              // different references are reported for each
    void* token;                              // the callback data passed to GCScanRoots
    void* pfnUserCallback;                    // the callback passed in to GcScanRoots
    uintptr_t stack_limit;                    // Lowest point on the thread stack that the scanning logic is permitted to read
};

typedef DacScanCallbackData EnumGcRefScanContext;
typedef void EnumGcRefCallbackFunc(PTR_PTR_Object, EnumGcRefScanContext* callbackData, uint32_t flags);

#else // DACCESS_COMPILE
struct ScanContext;
typedef void promote_func(PTR_PTR_Object, ScanContext*, unsigned);
typedef promote_func EnumGcRefCallbackFunc;
typedef ScanContext  EnumGcRefScanContext;

#endif // DACCESS_COMPILE

#endif // __thread_h__

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "Crst.h"
#include "event.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"
#include "RuntimeInstance.h"
#include "shash.h"
#include "rhbinder.h"
#include "stressLog.h"
#include "RhConfig.h"
#include "RhVolatile.h"

#ifndef DACCESS_COMPILE

EXTERN_C NATIVEAOT_API void* REDHAWK_CALLCONV RhpHandleAlloc(void* pObject, int type);
EXTERN_C NATIVEAOT_API void REDHAWK_CALLCONV RhHandleSet(void* handle, void* pObject);
EXTERN_C NATIVEAOT_API void REDHAWK_CALLCONV RhHandleFree(void* handle);

static int (*g_RuntimeInitializationCallback)();
static Thread* g_RuntimeInitializingThread;

#endif //!DACCESS_COMPILE

PInvokeTransitionFrame* Thread::GetTransitionFrame()
{
    if (ThreadStore::GetSuspendingThread() == this)
    {
        // This thread is in cooperative mode, so we grab the deferred frame
        // which is the frame from the most
        // recent 'cooperative pinvoke' transition that brought us here.
        ASSERT(m_pDeferredTransitionFrame != NULL);
        return m_pDeferredTransitionFrame;
    }

    ASSERT(m_pCachedTransitionFrame != NULL);
    return m_pCachedTransitionFrame;
}

#ifndef DACCESS_COMPILE

PInvokeTransitionFrame* Thread::GetTransitionFrameForStackTrace()
{
    ASSERT_MSG(this == ThreadStore::GetCurrentThread(), "Only supported for current thread.");
    ASSERT(Thread::IsCurrentThreadInCooperativeMode());
    ASSERT(m_pDeferredTransitionFrame != NULL);
    return m_pDeferredTransitionFrame;
}

void Thread::WaitForGC(PInvokeTransitionFrame* pTransitionFrame)
{
    ASSERT(!IsDoNotTriggerGcSet());

    // The wait operation below may trash the last win32 error. We save the error here so that it can be
    // restored after the wait operation;
    int32_t lastErrorOnEntry = PalGetLastError();

    do
    {
        // set preemptive mode
        VolatileStoreWithoutBarrier(&m_pTransitionFrame, pTransitionFrame);

#ifdef FEATURE_SUSPEND_REDIRECTION
        ClearState(TSF_Redirected);
#endif //FEATURE_SUSPEND_REDIRECTION

        RedhawkGCInterface::WaitForGCCompletion();

        // must be in cooperative mode when checking the trap flag
        VolatileStoreWithoutBarrier(&m_pTransitionFrame, NULL);
    }
    while (ThreadStore::IsTrapThreadsRequested());

    // Restore the saved error
    PalSetLastError(lastErrorOnEntry);
}

//
// This is used by the suspension code when driving all threads to unmanaged code.  It is performed after
// the FlushProcessWriteBuffers call so that we know that once the thread reaches unmanaged code, it won't
// reenter managed code.  Therefore, the m_pTransitionFrame is stable.  Except that it isn't.  The return-to-
// managed sequence will temporarily overwrite the m_pTransitionFrame to be 0.  As a result, we need to cache
// the non-zero m_pTransitionFrame value that we saw during suspend so that stackwalks can read this value
// without concern of sometimes reading a 0, as would be the case if they read m_pTransitionFrame directly.
//
// Returns true if it successfully cached the transition frame (i.e. the thread was in unmanaged).
// Returns false otherwise.
//
// WARNING: This method is called by suspension while one thread is interrupted
//          in a random location, possibly holding random locks.
//          It is unsafe to use blocking APIs or allocate in this method.
//          Please ensure that all methods called by this one also have this warning.
bool Thread::CacheTransitionFrameForSuspend()
{
    if (m_pCachedTransitionFrame != NULL)
        return true;

    // Once we see a thread posted a transition frame we can assume it will not enter cooperative mode.
    // It may temporarily set the frame to NULL when checking the trap flag, but will revert.
    // We can safely return true here and ache the frame.
    // Make sure compiler emits only one read.
    PInvokeTransitionFrame* temp = VolatileLoadWithoutBarrier(&m_pTransitionFrame);
    if (temp == NULL)
        return false;

    m_pCachedTransitionFrame = temp;
    return true;
}

void Thread::ResetCachedTransitionFrame()
{
    m_pCachedTransitionFrame = NULL;
}

// This function simulates a PInvoke transition using a frame pointer from somewhere further up the stack that
// was passed in via the m_pDeferredTransitionFrame field.  It is used to allow us to grandfather-in the set of GC
// code that runs in cooperative mode without having to rewrite it in managed code.  The result is that the
// code that calls into this special mode must spill preserved registers as if it's going to PInvoke, but
// record its transition frame pointer in m_pDeferredTransitionFrame and leave the thread in the cooperative
// mode.  Later on, when this function is called, we effect the state transition to 'unmanaged' using the
// previously setup transition frame.
void Thread::EnablePreemptiveMode()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
#if !defined(HOST_WASM)
    ASSERT(m_pDeferredTransitionFrame != NULL);
#endif

    // set preemptive mode
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, m_pDeferredTransitionFrame);
}

void Thread::DisablePreemptiveMode()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);

    // must be in cooperative mode when checking the trap flag
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, NULL);

    if (ThreadStore::IsTrapThreadsRequested() && (this != ThreadStore::GetSuspendingThread()))
    {
        WaitForGC(m_pDeferredTransitionFrame);
    }
}
#endif // !DACCESS_COMPILE

bool Thread::IsCurrentThreadInCooperativeMode()
{
#ifndef DACCESS_COMPILE
    ASSERT(ThreadStore::GetCurrentThread() == this);
#endif // !DACCESS_COMPILE
    return (m_pTransitionFrame == NULL);
}

//
// This is used by the EH system to find the place where execution left managed code when an exception leaks out of a
// pinvoke and we need to FailFast via the appropriate class library.
//
// May only be used from the same thread and while in preemptive mode with an active pinvoke on the stack.
//
#ifndef DACCESS_COMPILE
void * Thread::GetCurrentThreadPInvokeReturnAddress()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(!IsCurrentThreadInCooperativeMode());
    return ((PInvokeTransitionFrame*)m_pTransitionFrame)->m_RIP;
}
#endif // !DACCESS_COMPILE

#if defined(FEATURE_GC_STRESS) & !defined(DACCESS_COMPILE)
void Thread::SetRandomSeed(uint32_t seed)
{
    ASSERT(!IsStateSet(TSF_IsRandSeedSet));
    m_uRand = seed;
    SetState(TSF_IsRandSeedSet);
}

// Generates pseudo random numbers in the range [0, 2^31)
// using only multiplication and addition
uint32_t Thread::NextRand()
{
    // Uses Carta's algorithm for Park-Miller's PRNG:
    // x_{k+1} = 16807 * x_{k} mod (2^31-1)

    uint32_t hi,lo;

    // (high word of seed) * 16807 - at most 31 bits
    hi = 16807 * (m_uRand >> 16);
    // (low word of seed) * 16807 - at most 31 bits
    lo = 16807 * (m_uRand & 0xFFFF);

    // Proof that below operations (multiplication and addition only)
    // are equivalent to the original formula:
    //    x_{k+1} = 16807 * x_{k} mod (2^31-1)
    // We denote hi2 as the low 15 bits in hi,
    //       and hi1 as the remaining 16 bits in hi:
    // (hi                 * 2^16 + lo) mod (2^31-1) =
    // ((hi1 * 2^15 + hi2) * 2^16 + lo) mod (2^31-1) =
    // ( hi1 * 2^31 + hi2 * 2^16  + lo) mod (2^31-1) =
    // ( hi1 * (2^31-1) + hi1 + hi2 * 2^16 + lo) mod (2^31-1) =
    // ( hi2 * 2^16 + hi1 + lo ) mod (2^31-1)

    // lo + (hi2 * 2^16)
    lo += (hi & 0x7FFF) << 16;
    // lo + (hi2 * 2^16) + hi1
    lo += (hi >> 15);
    // modulo (2^31-1)
    if (lo > 0x7fffFFFF)
        lo -= 0x7fffFFFF;

    m_uRand = lo;

    return m_uRand;
}

bool Thread::IsRandInited()
{
    return IsStateSet(TSF_IsRandSeedSet);
}
#endif // FEATURE_GC_STRESS & !DACCESS_COMPILE

PTR_ExInfo Thread::GetCurExInfo()
{
    ValidateExInfoStack();
    return m_pExInfoStackHead;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef DACCESS_COMPILE

void Thread::Construct()
{
#ifndef USE_PORTABLE_HELPERS
    C_ASSERT(OFFSETOF__Thread__m_pTransitionFrame ==
             (offsetof(Thread, m_pTransitionFrame)));
#endif // USE_PORTABLE_HELPERS

    // NOTE: We do not explicitly defer to the GC implementation to initialize the alloc_context.  The
    // alloc_context will be initialized to 0 via the static initialization of tls_CurrentThread. If the
    // alloc_context ever needs different initialization, a matching change to the tls_CurrentThread
    // static initialization will need to be made.

    m_pTransitionFrame = TOP_OF_STACK_MARKER;
    m_pDeferredTransitionFrame = TOP_OF_STACK_MARKER;
    m_hPalThread = INVALID_HANDLE_VALUE;

    m_threadId.SetToCurrentThread();

    HANDLE curProcessPseudo = PalGetCurrentProcess();
    HANDLE curThreadPseudo  = PalGetCurrentThread();

    // This can fail!  Users of m_hPalThread must be able to handle INVALID_HANDLE_VALUE!!
    PalDuplicateHandle(curProcessPseudo, curThreadPseudo, curProcessPseudo, &m_hPalThread,
                       0,      // ignored
                       FALSE,  // inherit
                       DUPLICATE_SAME_ACCESS);

    if (!PalGetMaximumStackBounds(&m_pStackLow, &m_pStackHigh))
        RhFailFast();

#ifdef STRESS_LOG
    if (StressLog::StressLogOn(~0u, 0))
        m_pThreadStressLog = StressLog::CreateThreadStressLog(this);
#endif // STRESS_LOG

    // Everything else should be initialized to 0 via the static initialization of tls_CurrentThread.

    ASSERT(m_pThreadLocalModuleStatics == NULL);

    ASSERT(m_pGCFrameRegistrations == NULL);

    ASSERT(m_threadAbortException == NULL);

#ifdef FEATURE_SUSPEND_REDIRECTION
    ASSERT(m_redirectionContextBuffer == NULL);
#endif //FEATURE_SUSPEND_REDIRECTION
    ASSERT(m_interruptedContext == NULL);
}

bool Thread::IsInitialized()
{
    return (m_ThreadStateFlags != TSF_Unknown);
}

// -----------------------------------------------------------------------------------------------------------
// GC support APIs - do not use except from GC itself
//
void Thread::SetGCSpecial()
{
    if (!IsInitialized())
        Construct();

    SetState(TSF_IsGcSpecialThread);
}

bool Thread::IsGCSpecial()
{
    return IsStateSet(TSF_IsGcSpecialThread);
}

bool Thread::CatchAtSafePoint()
{
    // This is only called by the GC on a background GC worker thread that's explicitly interested in letting
    // a foreground GC proceed at that point. So it's always safe to return true.
    ASSERT(IsGCSpecial());
    return true;
}

uint64_t Thread::GetPalThreadIdForLogging()
{
    return *(uint64_t*)&m_threadId;
}

bool Thread::IsCurrentThread()
{
    return m_threadId.IsCurrentThread();
}

void Thread::Detach()
{
    RedhawkGCInterface::ReleaseAllocContext(GetAllocContext());
    SetDetached();
}

void Thread::Destroy()
{
    ASSERT(IsDetached());

    if (m_hPalThread != INVALID_HANDLE_VALUE)
        PalCloseHandle(m_hPalThread);

#ifdef STRESS_LOG
    ThreadStressLog* ptsl = reinterpret_cast<ThreadStressLog*>(GetThreadStressLog());
    StressLog::ThreadDetach(ptsl);
#endif // STRESS_LOG

#ifdef FEATURE_SUSPEND_REDIRECTION
    if (m_redirectionContextBuffer != NULL)
    {
        delete[] m_redirectionContextBuffer;
    }
#endif //FEATURE_SUSPEND_REDIRECTION

    ASSERT(m_pGCFrameRegistrations == NULL);
}

#ifdef HOST_WASM
extern RtuObjectRef * t_pShadowStackTop;
extern RtuObjectRef * t_pShadowStackBottom;

void GcScanWasmShadowStack(void * pfnEnumCallback, void * pvCallbackData)
{
    // Wasm does not permit iteration of stack frames so is uses a shadow stack instead
    RedhawkGCInterface::EnumGcRefsInRegionConservatively(t_pShadowStackBottom, t_pShadowStackTop, pfnEnumCallback, pvCallbackData);
}
#endif

void Thread::GcScanRoots(void * pfnEnumCallback, void * pvCallbackData)
{
    this->CrossThreadUnhijack();

#ifdef HOST_WASM
    GcScanWasmShadowStack(pfnEnumCallback, pvCallbackData);
#else
    StackFrameIterator frameIterator(this, GetTransitionFrame());
    GcScanRootsWorker(pfnEnumCallback, pvCallbackData, frameIterator);
#endif
}

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
// A trivial wrapper that unpacks the DacScanCallbackData and calls the callback provided to GcScanRoots
void GcScanRootsCallbackWrapper(PTR_RtuObjectRef ppObject, DacScanCallbackData* callbackData, uint32_t flags)
{
    Thread::GcScanRootsCallbackFunc * pfnUserCallback = (Thread::GcScanRootsCallbackFunc *)callbackData->pfnUserCallback;
    pfnUserCallback(ppObject, callbackData->token, flags);
}

bool Thread::GcScanRoots(GcScanRootsCallbackFunc * pfnEnumCallback, void * token, PTR_PAL_LIMITED_CONTEXT pInitialContext)
{
    DacScanCallbackData callbackDataWrapper;
    callbackDataWrapper.thread_under_crawl = this;
    callbackDataWrapper.promotion = true;
    callbackDataWrapper.token = token;
    callbackDataWrapper.pfnUserCallback = pfnEnumCallback;
    //When debugging we might be trying to enumerate with or without a transition frame
    //on top of the stack. If there is one use it, otherwise the debugger provides a set of initial registers
    //to use.
    PInvokeTransitionFrame* pTransitionFrame = GetTransitionFrame();
    if(pTransitionFrame != NULL)
    {
        StackFrameIterator  frameIterator(this, pTransitionFrame);
        GcScanRootsWorker(&GcScanRootsCallbackWrapper, &callbackDataWrapper, frameIterator);
    }
    else
    {
        if(pInitialContext == NULL)
            return false;
        StackFrameIterator frameIterator(this, pInitialContext);
        GcScanRootsWorker(&GcScanRootsCallbackWrapper, &callbackDataWrapper, frameIterator);
    }
    return true;
}
#endif //DACCESS_COMPILE

void Thread::GcScanRootsWorker(void * pfnEnumCallback, void * pvCallbackData, StackFrameIterator & frameIterator)
{
    PTR_RtuObjectRef pHijackedReturnValue = NULL;
    GCRefKind        returnValueKind      = GCRK_Unknown;

    if (frameIterator.GetHijackedReturnValueLocation(&pHijackedReturnValue, &returnValueKind))
    {
        GCRefKind reg0Kind = ExtractReg0ReturnKind(returnValueKind);
        if (reg0Kind != GCRK_Scalar)
        {
            RedhawkGCInterface::EnumGcRef(pHijackedReturnValue, reg0Kind, pfnEnumCallback, pvCallbackData);
        }

#if defined(TARGET_ARM64) || defined(TARGET_UNIX)
        GCRefKind reg1Kind = ExtractReg1ReturnKind(returnValueKind);
        if (reg1Kind != GCRK_Scalar)
        {
            // X0/X1 or RAX/RDX are saved in hijack frame next to each other in this order
            RedhawkGCInterface::EnumGcRef(pHijackedReturnValue + 1, reg1Kind, pfnEnumCallback, pvCallbackData);
        }
#endif  // TARGET_ARM64 || TARGET_UNIX

    }

#ifndef DACCESS_COMPILE
    if (GetRuntimeInstance()->IsConservativeStackReportingEnabled())
    {
        if (frameIterator.IsValid())
        {
            PTR_VOID pLowerBound = dac_cast<PTR_VOID>(frameIterator.GetRegisterSet()->GetSP());

            // Transition frame may contain callee saved registers that need to be reported as well
            PInvokeTransitionFrame* pTransitionFrame = GetTransitionFrame();
            ASSERT(pTransitionFrame != NULL);

            if (pTransitionFrame == INTERRUPTED_THREAD_MARKER)
            {
                GetInterruptedContext()->ForEachPossibleObjectRef
                (
                    [&](size_t* pRef)
                    {
                        RedhawkGCInterface::EnumGcRefConservatively((PTR_RtuObjectRef)pRef, pfnEnumCallback, pvCallbackData);
                    }
                );
            }

            if (pTransitionFrame < pLowerBound)
                pLowerBound = pTransitionFrame;

            PTR_VOID pUpperBound = m_pStackHigh;

            RedhawkGCInterface::EnumGcRefsInRegionConservatively(
                dac_cast<PTR_RtuObjectRef>(pLowerBound),
                dac_cast<PTR_RtuObjectRef>(pUpperBound),
                pfnEnumCallback,
                pvCallbackData);
        }
    }
    else
#endif // !DACCESS_COMPILE
    {
        while (frameIterator.IsValid())
        {
            frameIterator.CalculateCurrentMethodState();

            STRESS_LOG1(LF_GCROOTS, LL_INFO1000, "Scanning method %pK\n", (void*)frameIterator.GetRegisterSet()->IP);

            if (!frameIterator.ShouldSkipRegularGcReporting())
            {
                RedhawkGCInterface::EnumGcRefs(frameIterator.GetCodeManager(),
                                               frameIterator.GetMethodInfo(),
                                               frameIterator.GetEffectiveSafePointAddress(),
                                               frameIterator.GetRegisterSet(),
                                               pfnEnumCallback,
                                               pvCallbackData,
                                               frameIterator.IsActiveStackFrame());
            }

            // Each enumerated frame (including the first one) may have an associated stack range we need to
            // report conservatively (every pointer aligned value that looks like it might be a GC reference is
            // reported as a pinned interior reference). This occurs in an edge case where a managed method whose
            // signature the runtime is not aware of calls into the runtime which subsequently calls back out
            // into managed code (allowing the possibility of a garbage collection). This can happen in certain
            // interface invocation slow paths for instance. Since the original managed call may have passed GC
            // references which are unreported by any managed method on the stack at the time of the GC we
            // identify (again conservatively) the range of the stack that might contain these references and
            // report everything. Since it should be a very rare occurrence indeed that we actually have to do
            // this, it's considered a better trade-off than storing signature metadata for every potential
            // callsite of the type described above.
            if (frameIterator.HasStackRangeToReportConservatively())
            {
                PTR_RtuObjectRef pLowerBound;
                PTR_RtuObjectRef pUpperBound;
                frameIterator.GetStackRangeToReportConservatively(&pLowerBound, &pUpperBound);
                RedhawkGCInterface::EnumGcRefsInRegionConservatively(pLowerBound,
                                                                     pUpperBound,
                                                                     pfnEnumCallback,
                                                                     pvCallbackData);
            }

            frameIterator.Next();
        }
    }

    // ExInfos hold exception objects that are not reported by anyone else.  In fact, sometimes they are in
    // logically dead parts of the stack that the typical GC stackwalk skips.  (This happens in the case where
    // one exception dispatch superseded a previous one.)  We keep them alive as long as they are in the
    // ExInfo chain to aid in post-mortem debugging.  SOS will access them through the DAC and the exported
    // API, RhGetExceptionsForCurrentThread, will access them at runtime to gather additional information to
    // add to a dump file during FailFast.
    for (PTR_ExInfo curExInfo = GetCurExInfo(); curExInfo != NULL; curExInfo = curExInfo->m_pPrevExInfo)
    {
        PTR_RtuObjectRef pExceptionObj = dac_cast<PTR_RtuObjectRef>(&curExInfo->m_exception);
        RedhawkGCInterface::EnumGcRef(pExceptionObj, GCRK_Object, pfnEnumCallback, pvCallbackData);
    }

    for (GCFrameRegistration* pCurGCFrame = m_pGCFrameRegistrations; pCurGCFrame != NULL; pCurGCFrame = pCurGCFrame->m_pNext)
    {
        ASSERT(pCurGCFrame->m_pThread == this);

        for (uint32_t i = 0; i < pCurGCFrame->m_numObjRefs; i++)
        {
            RedhawkGCInterface::EnumGcRef(dac_cast<PTR_RtuObjectRef>(pCurGCFrame->m_pObjRefs + i),
                pCurGCFrame->m_MaybeInterior ? GCRK_Byref : GCRK_Object, pfnEnumCallback, pvCallbackData);
        }
    }

    // Keep alive the ThreadAbortException that's stored in the target thread during thread abort
    PTR_RtuObjectRef pThreadAbortExceptionObj = dac_cast<PTR_RtuObjectRef>(&m_threadAbortException);
    RedhawkGCInterface::EnumGcRef(pThreadAbortExceptionObj, GCRK_Object, pfnEnumCallback, pvCallbackData);
}

#ifndef DACCESS_COMPILE

EXTERN_C void FASTCALL RhpSuspendRedirected();
EXTERN_C void FASTCALL RhpGcProbeHijack();
EXTERN_C void FASTCALL RhpGcStressHijack();

// static
bool Thread::IsHijackTarget(void* address)
{
    if (&RhpGcProbeHijack == address)
        return true;
#ifdef FEATURE_GC_STRESS
    if (&RhpGcStressHijack == address)
        return true;
#endif // FEATURE_GC_STRESS
    return false;
}

void Thread::Hijack()
{
    ASSERT(ThreadStore::GetCurrentThread() == ThreadStore::GetSuspendingThread());
    ASSERT_MSG(ThreadStore::GetSuspendingThread() != this, "You may not hijack a thread from itself.");

    if (m_hPalThread == INVALID_HANDLE_VALUE)
    {
        // cannot proceed
        return;
    }

    if (IsGCSpecial())
    {
        // GC threads can not be forced to run preemptively, so we will not try.
        return;
    }

#ifdef FEATURE_SUSPEND_REDIRECTION
    // if the thread is redirected, leave it as-is.
    if (IsStateSet(TSF_Redirected))
    {
        return;
    }
#endif //FEATURE_SUSPEND_REDIRECTION

    // PalHijack will call HijackCallback or make the target thread call it.
    // It may also do nothing if the target thread is in inconvenient state.
    PalHijack(m_hPalThread, this);
}

void Thread::HijackCallback(NATIVE_CONTEXT* pThreadContext, void* pThreadToHijack)
{
    // If we are no longer trying to suspend, no need to do anything.
    // This is just an optimization. It is ok to race with the setting the trap flag here.
    // If we need to suspend, we will be called again.
    if (!ThreadStore::IsTrapThreadsRequested())
        return;

    Thread* pThread = (Thread*) pThreadToHijack;
    if (pThread == NULL)
    {
        pThread = ThreadStore::GetCurrentThreadIfAvailable();
        if (pThread == NULL)
        {
            ASSERT(!"a not attached thread got signaled");
            // perhaps we share the signal with something else?
            return;
        }

        if (pThread == ThreadStore::GetSuspendingThread())
        {
            ASSERT(!"trying to suspend suspending thread");
            // perhaps we share the signal with something else?
            return;
        }
    }

    // we have a thread stopped, and we do not know where exactly.
    // it could be in a system call or in our own runtime holding locks
    // current thread should not block or allocate while we determine whether the location is in managed code.

    if (pThread->m_pTransitionFrame != NULL)
    {
        // This thread has already made it to preemptive (posted a transition frame)
        // we do not need to hijack it
        return;
    }

    void* pvAddress = (void*)pThreadContext->GetIp();
    RuntimeInstance* runtime = GetRuntimeInstance();
    if (!runtime->IsManaged(pvAddress))
    {
        // Running in cooperative mode, but not managed.
        // We cannot continue.
        return;
    }

    if (pThread->IsDoNotTriggerGcSet())
    {
        return;
    }

    // we may be able to do GC stack walk right where the threads is now,
    // as long as the location is a GC safe point.
    ICodeManager* codeManager = runtime->GetCodeManagerForAddress(pvAddress);
    if (runtime->IsConservativeStackReportingEnabled() ||
        codeManager->IsSafePoint(pvAddress))
    {
        // we may not be able to unwind in some locations, such as epilogs.
        // such locations should not contain safe points.
        // when scanning conservatively we do not need to unwind
        ASSERT(codeManager->IsUnwindable(pvAddress) || runtime->IsConservativeStackReportingEnabled());

        // if we are not given a thread to hijack
        // perform in-line wait on the current thread
        if (pThreadToHijack == NULL)
        {
            ASSERT(pThread->m_interruptedContext == NULL);
            pThread->InlineSuspend(pThreadContext);
            return;
        }

#ifdef FEATURE_SUSPEND_REDIRECTION
        if (pThread->Redirect())
        {
            return;
        }
#endif //FEATURE_SUSPEND_REDIRECTION
    }

    pThread->HijackReturnAddress(pThreadContext, &RhpGcProbeHijack);
}

#ifdef FEATURE_GC_STRESS
// This is a helper called from RhpHijackForGcStress which will place a GC Stress
// hijack on this thread's call stack. This is never called from another thread.
// static
void Thread::HijackForGcStress(PAL_LIMITED_CONTEXT * pSuspendCtx)
{
    Thread * pCurrentThread = ThreadStore::GetCurrentThread();

    // don't hijack for GC stress if we're in a "no GC stress" region
    if (pCurrentThread->IsSuppressGcStressSet())
        return;

    RuntimeInstance * pInstance = GetRuntimeInstance();

    uintptr_t ip = pSuspendCtx->GetIp();

    bool bForceGC = g_pRhConfig->GetGcStressThrottleMode() == 0;
    // we enable collecting statistics by callsite even for stochastic-only
    // stress mode. this will force a stack walk, but it's worthwhile for
    // collecting data (we only actually need the IP when
    // (g_pRhConfig->GetGcStressThrottleMode() & 1) != 0)
    if (!bForceGC)
    {
        StackFrameIterator sfi(pCurrentThread, pSuspendCtx);
        if (sfi.IsValid())
        {
            pCurrentThread->Unhijack();
            sfi.CalculateCurrentMethodState();
            // unwind to method below the one whose epilog set up the hijack
            sfi.Next();
            if (sfi.IsValid())
            {
                ip = sfi.GetRegisterSet()->GetIP();
            }
        }
    }
    if (bForceGC || pInstance->ShouldHijackCallsiteForGcStress(ip))
    {
        pCurrentThread->HijackReturnAddress(pSuspendCtx, &RhpGcStressHijack);
    }
}
#endif // FEATURE_GC_STRESS

// This function is called from a thread to place a return hijack onto its own stack for GC stress cases
// via Thread::HijackForGcStress above. The only constraint on the suspension is that the
// stack be crawlable enough to yield the location of the return address.
void Thread::HijackReturnAddress(PAL_LIMITED_CONTEXT* pSuspendCtx, HijackFunc* pfnHijackFunction)
{
    if (IsDoNotTriggerGcSet())
        return;

    StackFrameIterator frameIterator(this, pSuspendCtx);
    if (!frameIterator.IsValid())
    {
        return;
    }

    HijackReturnAddressWorker(&frameIterator, pfnHijackFunction);
}

// This function is called in one of two scenarios:
// 1) from another thread to place a return hijack onto this thread's stack. In this case the target
//    thread is OS suspended at pSuspendCtx in managed code.
// 2) from a thread to place a return hijack onto its own stack for GC suspension. In this case the target
//    thread is interrupted at pSuspendCtx in managed code via a signal or similar.
void Thread::HijackReturnAddress(NATIVE_CONTEXT* pSuspendCtx, HijackFunc* pfnHijackFunction)
{
    ASSERT(!IsDoNotTriggerGcSet());

    StackFrameIterator frameIterator(this, pSuspendCtx);
    ASSERT(frameIterator.IsValid());

    HijackReturnAddressWorker(&frameIterator, pfnHijackFunction);
}

void Thread::HijackReturnAddressWorker(StackFrameIterator* frameIterator, HijackFunc* pfnHijackFunction)
{
    void** ppvRetAddrLocation;
    GCRefKind retValueKind;

    frameIterator->CalculateCurrentMethodState();
    if (frameIterator->GetCodeManager()->GetReturnAddressHijackInfo(frameIterator->GetMethodInfo(),
        frameIterator->GetRegisterSet(),
        &ppvRetAddrLocation,
        &retValueKind))
    {
        ASSERT(ppvRetAddrLocation != NULL);

        // if the new hijack location is the same, we do nothing
        if (m_ppvHijackedReturnAddressLocation == ppvRetAddrLocation)
            return;

        // we only unhijack if we are going to install a new or better hijack.
        CrossThreadUnhijack();

        void* pvRetAddr = *ppvRetAddrLocation;
        ASSERT(pvRetAddr != NULL);
        ASSERT(StackFrameIterator::IsValidReturnAddress(pvRetAddr));

        m_ppvHijackedReturnAddressLocation = ppvRetAddrLocation;
        m_pvHijackedReturnAddress = pvRetAddr;
        m_uHijackedReturnValueFlags = ReturnKindToTransitionFrameFlags(retValueKind);
        *ppvRetAddrLocation = (void*)pfnHijackFunction;

        STRESS_LOG2(LF_STACKWALK, LL_INFO10000, "InternalHijack: TgtThread = %llx, IP = %p\n",
            GetPalThreadIdForLogging(), frameIterator->GetRegisterSet()->GetIP());
    }
}

NATIVE_CONTEXT* Thread::GetInterruptedContext()
{
    ASSERT(m_interruptedContext != NULL);
    return m_interruptedContext;
}

#ifdef FEATURE_SUSPEND_REDIRECTION

NATIVE_CONTEXT* Thread::EnsureRedirectionContext()
{
    if (m_redirectionContextBuffer == NULL)
    {
        m_interruptedContext = PalAllocateCompleteOSContext(&m_redirectionContextBuffer);
    }

    return m_interruptedContext;
}

bool Thread::Redirect()
{
    ASSERT(!IsDoNotTriggerGcSet());

    NATIVE_CONTEXT* redirectionContext = EnsureRedirectionContext();
    if (redirectionContext == NULL)
        return false;

    if (!PalGetCompleteThreadContext(m_hPalThread, redirectionContext))
        return false;

    uintptr_t origIP = redirectionContext->GetIp();
    redirectionContext->SetIp((uintptr_t)RhpSuspendRedirected);
    if (!PalSetThreadContext(m_hPalThread, redirectionContext))
        return false;

    // the thread will now inevitably try to suspend
    SetState(TSF_Redirected);
    redirectionContext->SetIp(origIP);

    STRESS_LOG2(LF_STACKWALK, LL_INFO10000, "InternalRedirect: TgtThread = %llx, IP = %p\n",
        GetPalThreadIdForLogging(), origIP);

    return true;
}
#endif //FEATURE_SUSPEND_REDIRECTION

bool Thread::InlineSuspend(NATIVE_CONTEXT* interruptedContext)
{
    ASSERT(!IsDoNotTriggerGcSet());

    Unhijack();

    m_interruptedContext = interruptedContext;
    WaitForGC(INTERRUPTED_THREAD_MARKER);
    m_interruptedContext = NULL;

    return true;
}

// This is the standard Unhijack, which is only allowed to be called on your own thread.
// Note that all the asm-implemented Unhijacks should also only be operating on their
// own thread.
void Thread::Unhijack()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(IsCurrentThreadInCooperativeMode());

    UnhijackWorker();
}

// This unhijack routine is called to undo a hijack, that is potentially on a different thread.
// 
// Although there are many code sequences (here and in asm) to
// perform an unhijack operation, they will never execute concurrently:
// 
// - A thread may unhijack itself at any time so long as it does that from unmanaged code while in coop mode.
//   This ensures that coop thread can access its stack synchronously.
//   Unhijacking from unmanaged code ensures that another thread will not attempt to hijack it,
//   since we only hijack threads that are executing managed code.
// 
// - A GC thread may access a thread asynchronously, including unhijacking it.
//   Asynchronously accessed thread must be in preemptive mode and should not
//   access the managed portion of its stack.
// 
// - A thread that owns the suspension can access another thread as long as the other thread is
//   in preemptive mode or suspended in managed code.
//   Either way the other thread cannot be accessing its hijack.
//
void Thread::CrossThreadUnhijack()
{
    ASSERT(((ThreadStore::GetCurrentThread() == this) && IsCurrentThreadInCooperativeMode()) ||
        ThreadStore::GetCurrentThread()->IsGCSpecial() ||
        ThreadStore::GetCurrentThread() == ThreadStore::GetSuspendingThread()
    );

    UnhijackWorker();
}

// This is the hijack worker routine which merely implements the hijack mechanism.
// DO NOT USE DIRECTLY.  Use Unhijack() or CrossThreadUnhijack() instead.
void Thread::UnhijackWorker()
{
    if (m_pvHijackedReturnAddress == NULL)
    {
        ASSERT(m_ppvHijackedReturnAddressLocation == NULL);
        return;
    }

    // Restore the original return address.
    ASSERT(m_ppvHijackedReturnAddressLocation != NULL);
    *m_ppvHijackedReturnAddressLocation = m_pvHijackedReturnAddress;

    // Clear the hijack state.
    m_ppvHijackedReturnAddressLocation  = NULL;
    m_pvHijackedReturnAddress           = NULL;
    m_uHijackedReturnValueFlags         = 0;
}

bool Thread::IsHijacked()
{
    ASSERT(((ThreadStore::GetCurrentThread() == this) && IsCurrentThreadInCooperativeMode()) ||
        ThreadStore::GetCurrentThread()->IsGCSpecial() ||
        ThreadStore::GetCurrentThread() == ThreadStore::GetSuspendingThread()
    );

    return m_pvHijackedReturnAddress != NULL;
}

void* Thread::GetHijackedReturnAddress()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    return m_pvHijackedReturnAddress;
}

void Thread::SetState(ThreadStateFlags flags)
{
    PalInterlockedOr(&m_ThreadStateFlags, flags);
}

void Thread::ClearState(ThreadStateFlags flags)
{
    PalInterlockedAnd(&m_ThreadStateFlags, ~flags);
}

bool Thread::IsStateSet(ThreadStateFlags flags)
{
    return ((m_ThreadStateFlags & flags) == (uint32_t) flags);
}

bool Thread::IsSuppressGcStressSet()
{
    return IsStateSet(TSF_SuppressGcStress);
}

void Thread::SetSuppressGcStress()
{
    ASSERT(!IsStateSet(TSF_SuppressGcStress));
    SetState(TSF_SuppressGcStress);
}

void Thread::ClearSuppressGcStress()
{
    ASSERT(IsStateSet(TSF_SuppressGcStress));
    ClearState(TSF_SuppressGcStress);
}

#endif //!DACCESS_COMPILE

#ifndef DACCESS_COMPILE
#ifdef FEATURE_GC_STRESS
#ifdef HOST_X86 // the others are implemented in assembly code to avoid trashing the argument registers
EXTERN_C void FASTCALL RhpSuppressGcStress()
{
    ThreadStore::GetCurrentThread()->SetSuppressGcStress();
}
#endif // HOST_X86

EXTERN_C void FASTCALL RhpUnsuppressGcStress()
{
    ThreadStore::GetCurrentThread()->ClearSuppressGcStress();
}
#else
EXTERN_C void FASTCALL RhpSuppressGcStress()
{
}
EXTERN_C void FASTCALL RhpUnsuppressGcStress()
{
}
#endif // FEATURE_GC_STRESS

// Standard calling convention variant and actual implementation for RhpWaitForGC
EXTERN_C NOINLINE void FASTCALL RhpWaitForGC2(PInvokeTransitionFrame * pFrame)
{
    Thread * pThread = pFrame->m_pThread;
    if (pThread->IsDoNotTriggerGcSet())
        return;

    pThread->WaitForGC(pFrame);
}

// Standard calling convention variant and actual implementation for RhpGcPoll
EXTERN_C NOINLINE void FASTCALL RhpGcPoll2(PInvokeTransitionFrame* pFrame)
{
    ASSERT(!Thread::IsHijackTarget(pFrame->m_RIP));

    Thread* pThread = ThreadStore::GetCurrentThread();
    pFrame->m_pThread = pThread;

    RhpWaitForGC2(pFrame);
}

#ifdef FEATURE_SUSPEND_REDIRECTION

EXTERN_C NOINLINE void FASTCALL RhpSuspendRedirected()
{
    Thread* pThread = ThreadStore::GetCurrentThread();
    pThread->WaitForGC(INTERRUPTED_THREAD_MARKER);

    // restore execution at interrupted location
    PalRestoreContext(pThread->GetInterruptedContext());
    UNREACHABLE();
}

#endif //FEATURE_SUSPEND_REDIRECTION

void Thread::PushExInfo(ExInfo * pExInfo)
{
    ValidateExInfoStack();

    pExInfo->m_pPrevExInfo = m_pExInfoStackHead;
    m_pExInfoStackHead = pExInfo;
}

void Thread::ValidateExInfoPop(ExInfo * pExInfo, void * limitSP)
{
#ifdef _DEBUG
    ValidateExInfoStack();
    ASSERT_MSG(pExInfo == m_pExInfoStackHead, "not popping the head element");
    pExInfo = pExInfo->m_pPrevExInfo;

    while (pExInfo && pExInfo < limitSP)
    {
        ASSERT_MSG(pExInfo->m_kind & EK_SupersededFlag, "popping a non-superseded ExInfo");
        pExInfo = pExInfo->m_pPrevExInfo;
    }
#else
    UNREFERENCED_PARAMETER(pExInfo);
    UNREFERENCED_PARAMETER(limitSP);
#endif // _DEBUG
}

COOP_PINVOKE_HELPER(void, RhpValidateExInfoPop, (Thread * pThread, ExInfo * pExInfo, void * limitSP))
{
    pThread->ValidateExInfoPop(pExInfo, limitSP);
}

bool Thread::IsDoNotTriggerGcSet()
{
    return IsStateSet(TSF_DoNotTriggerGc);
}

void Thread::SetDoNotTriggerGc()
{
    ASSERT(!IsStateSet(TSF_DoNotTriggerGc));
    SetState(TSF_DoNotTriggerGc);
}

void Thread::ClearDoNotTriggerGc()
{
    // Allowing unmatched clears simplifies the EH dispatch code, so we do not assert anything here.
    ClearState(TSF_DoNotTriggerGc);
}

bool Thread::IsDetached()
{
    return IsStateSet(TSF_Detached);
}

void Thread::SetDetached()
{
    ASSERT(!IsStateSet(TSF_Detached));
    SetState(TSF_Detached);
}

bool Thread::IsActivationPending()
{
    return IsStateSet(TSF_ActivationPending);
}

void Thread::SetActivationPending(bool isPending)
{
    if (isPending)
    {
        SetState(TSF_ActivationPending);
    }
    else
    {
        ClearState(TSF_ActivationPending);
    }
}

#endif // !DACCESS_COMPILE

void Thread::ValidateExInfoStack()
{
#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    ExInfo temp;

    ExInfo* pCur = m_pExInfoStackHead;
    while (pCur)
    {
        ASSERT_MSG((this != ThreadStore::GetCurrentThread()) || (pCur > &temp), "an entry in the ExInfo chain points into dead stack");
        ASSERT_MSG(pCur < m_pStackHigh, "an entry in the ExInfo chain isn't on this stack");
        pCur = pCur->m_pPrevExInfo;
    }
#endif // _DEBUG
#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

#ifndef TARGET_UNIX
EXTERN_C NATIVEAOT_API uint32_t __cdecl RhCompatibleReentrantWaitAny(UInt32_BOOL alertable, uint32_t timeout, uint32_t count, HANDLE* pHandles)
{
    return PalCompatibleWaitAny(alertable, timeout, count, pHandles, /*allowReentrantWait:*/ TRUE);
}
#endif // TARGET_UNIX

FORCEINLINE bool Thread::InlineTryFastReversePInvoke(ReversePInvokeFrame * pFrame)
{
    // Do we need to attach the thread?
    if (!IsStateSet(TSF_Attached))
        return false; // thread is not attached

    // If the thread is already in cooperative mode, this is a bad transition that will be a fail fast unless we are in
    // a do not trigger mode.  The exception to the rule allows us to have [UnmanagedCallersOnly] methods that are called via
    // the "restricted GC callouts" as well as from native, which is necessary because the methods are CCW vtable
    // methods on interfaces passed to native.
    if (IsCurrentThreadInCooperativeMode())
    {
        if (IsDoNotTriggerGcSet())
        {
            // RhpTrapThreads will always be set in this case, so we must skip that check.  We must be sure to
            // zero-out our 'previous transition frame' state first, however.
            pFrame->m_savedPInvokeTransitionFrame = NULL;
            return true;
        }

        return false; // bad transition
    }

    // this is an ordinary transition to managed code
    // GC threads should not do that
    ASSERT(!IsGCSpecial());

    // save the previous transition frame
    pFrame->m_savedPInvokeTransitionFrame = m_pTransitionFrame;

    // must be in cooperative mode when checking the trap flag
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, NULL);

    // now check if we need to trap the thread
    if (ThreadStore::IsTrapThreadsRequested())
    {
        // put the previous frame back (sets us back to preemptive mode)
        m_pTransitionFrame = pFrame->m_savedPInvokeTransitionFrame;
        return false; // need to trap the thread
    }

    return true;
}

EXTERN_C void RhSetRuntimeInitializationCallback(int (*fPtr)())
{
    g_RuntimeInitializationCallback = fPtr;
}

void Thread::ReversePInvokeAttachOrTrapThread(ReversePInvokeFrame * pFrame)
{
    if (!IsStateSet(TSF_Attached))
    {
        if (g_RuntimeInitializationCallback != NULL && g_RuntimeInitializingThread != this)
        {
            EnsureRuntimeInitialized();
        }

        ThreadStore::AttachCurrentThread();
    }

    // If the thread is already in cooperative mode, this is a bad transition.
    if (IsCurrentThreadInCooperativeMode())
    {
        // The TSF_DoNotTriggerGc mode is handled by the fast path (InlineTryFastReversePInvoke or equivalent assembly code)
        ASSERT(!IsDoNotTriggerGcSet());

        PalPrintFatalError("\nFatal error. Invalid Program: attempted to call a UnmanagedCallersOnly method from managed code.\n");
        RhFailFast();
   }

    // save the previous transition frame
    pFrame->m_savedPInvokeTransitionFrame = m_pTransitionFrame;

    // must be in cooperative mode when checking the trap flag
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, NULL);

    // now check if we need to trap the thread
    if (ThreadStore::IsTrapThreadsRequested())
    {
        WaitForGC(pFrame->m_savedPInvokeTransitionFrame);
    }
}

void Thread::EnsureRuntimeInitialized()
{
    while (PalInterlockedCompareExchangePointer((void *volatile *)&g_RuntimeInitializingThread, this, NULL) != NULL)
    {
        PalSleep(1);
    }

    if (g_RuntimeInitializationCallback != NULL)
    {
        if (g_RuntimeInitializationCallback() != 0)
            RhFailFast();

        g_RuntimeInitializationCallback = NULL;
    }

    PalInterlockedExchangePointer((void *volatile *)&g_RuntimeInitializingThread, NULL);
}

FORCEINLINE void Thread::InlineReversePInvokeReturn(ReversePInvokeFrame * pFrame)
{
    // set our mode to preemptive
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, pFrame->m_savedPInvokeTransitionFrame);
}

FORCEINLINE void Thread::InlinePInvoke(PInvokeTransitionFrame * pFrame)
{
    pFrame->m_pThread = this;
    // set our mode to preemptive
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, pFrame);
}

FORCEINLINE void Thread::InlinePInvokeReturn(PInvokeTransitionFrame * pFrame)
{
    // must be in cooperative mode when checking the trap flag
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, NULL);
    if (ThreadStore::IsTrapThreadsRequested())
    {
        RhpWaitForGC2(pFrame);
    }
}

Object * Thread::GetThreadAbortException()
{
    return m_threadAbortException;
}

void Thread::SetThreadAbortException(Object *exception)
{
    m_threadAbortException = exception;
}

COOP_PINVOKE_HELPER(Object *, RhpGetThreadAbortException, ())
{
    Thread * pCurThread = ThreadStore::RawGetCurrentThread();
    return pCurThread->GetThreadAbortException();
}

Object** Thread::GetThreadStaticStorage()
{
    return &m_pThreadLocalModuleStatics;
}

COOP_PINVOKE_HELPER(Object**, RhGetThreadStaticStorage, ())
{
    Thread * pCurrentThread = ThreadStore::RawGetCurrentThread();
    return pCurrentThread->GetThreadStaticStorage();
}

// This is function is used to quickly query a value that can uniquely identify a thread
COOP_PINVOKE_HELPER(uint8_t*, RhCurrentNativeThreadId, ())
{
#ifndef TARGET_UNIX
    return PalNtCurrentTeb();
#else
    return (uint8_t*)ThreadStore::RawGetCurrentThread();
#endif // TARGET_UNIX
}

// This function is used to get the OS thread identifier for the current thread.
COOP_PINVOKE_HELPER(uint64_t, RhCurrentOSThreadId, ())
{
    return PalGetCurrentThreadIdForLogging();
}

// Standard calling convention variant and actual implementation for RhpReversePInvokeAttachOrTrapThread
EXTERN_C NOINLINE void FASTCALL RhpReversePInvokeAttachOrTrapThread2(ReversePInvokeFrame* pFrame)
{
    ASSERT(pFrame->m_savedThread == ThreadStore::RawGetCurrentThread());
    pFrame->m_savedThread->ReversePInvokeAttachOrTrapThread(pFrame);
}

//
// PInvoke
//

COOP_PINVOKE_HELPER(void, RhpReversePInvoke, (ReversePInvokeFrame * pFrame))
{
    Thread * pCurThread = ThreadStore::RawGetCurrentThread();
    pFrame->m_savedThread = pCurThread;
    if (pCurThread->InlineTryFastReversePInvoke(pFrame))
        return;

    RhpReversePInvokeAttachOrTrapThread2(pFrame);
}

COOP_PINVOKE_HELPER(void, RhpReversePInvokeReturn, (ReversePInvokeFrame * pFrame))
{
    pFrame->m_savedThread->InlineReversePInvokeReturn(pFrame);
}

#ifdef USE_PORTABLE_HELPERS

COOP_PINVOKE_HELPER(void, RhpPInvoke2, (PInvokeTransitionFrame* pFrame))
{
    Thread * pCurThread = ThreadStore::RawGetCurrentThread();
    pCurThread->InlinePInvoke(pFrame);
}

COOP_PINVOKE_HELPER(void, RhpPInvokeReturn2, (PInvokeTransitionFrame* pFrame))
{
    //reenter cooperative mode
    pFrame->m_pThread->InlinePInvokeReturn(pFrame);
}

#endif //USE_PORTABLE_HELPERS

#endif // !DACCESS_COMPILE

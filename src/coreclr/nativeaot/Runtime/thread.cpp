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
#include "RWLock.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"
#include "RuntimeInstance.h"
#include "shash.h"
#include "rhbinder.h"
#include "stressLog.h"
#include "RhConfig.h"

#ifndef DACCESS_COMPILE

EXTERN_C REDHAWK_API void* REDHAWK_CALLCONV RhpHandleAlloc(void* pObject, int type);
EXTERN_C REDHAWK_API void REDHAWK_CALLCONV RhHandleSet(void* handle, void* pObject);
EXTERN_C REDHAWK_API void REDHAWK_CALLCONV RhHandleFree(void* handle);

static int (*g_RuntimeInitializationCallback)();
static Thread* g_RuntimeInitializingThread;

#ifdef _MSC_VER
extern "C" void _ReadWriteBarrier(void);
#pragma intrinsic(_ReadWriteBarrier)
#else // _MSC_VER
#define _ReadWriteBarrier() __asm__ volatile("" : : : "memory")
#endif // _MSC_VER
#endif //!DACCESS_COMPILE

PTR_VOID Thread::GetTransitionFrame()
{
    if (ThreadStore::GetSuspendingThread() == this)
    {
        // This thread is in cooperative mode, so we grab the transition frame
        // from the 'tunnel' location, which will have the frame from the most
        // recent 'cooperative pinvoke' transition that brought us here.
        ASSERT(m_pHackPInvokeTunnel != NULL);
        return m_pHackPInvokeTunnel;
    }

    ASSERT(m_pCachedTransitionFrame != NULL);
    return m_pCachedTransitionFrame;
}

#ifndef DACCESS_COMPILE

PTR_VOID Thread::GetTransitionFrameForStackTrace()
{
    ASSERT_MSG(ThreadStore::GetSuspendingThread() == NULL, "Not allowed when suspended for GC.");
    ASSERT_MSG(this == ThreadStore::GetCurrentThread(), "Only supported for current thread.");
    ASSERT(Thread::IsCurrentThreadInCooperativeMode());
    ASSERT(m_pHackPInvokeTunnel != NULL);
    return m_pHackPInvokeTunnel;
}

void Thread::WaitForSuspend()
{
    Unhijack();
    GetThreadStore()->WaitForSuspendComplete();
}

void Thread::WaitForGC(void * pTransitionFrame)
{
    ASSERT(!IsDoNotTriggerGcSet());

    do
    {
        m_pTransitionFrame = pTransitionFrame;

        Unhijack();
        RedhawkGCInterface::WaitForGCCompletion();

        m_pTransitionFrame = NULL;

        // We need to prevent compiler reordering between above write and below read.
        _ReadWriteBarrier();
    }
    while (ThreadStore::IsTrapThreadsRequested());
}

//
// This is used by the suspension code when driving all threads to unmanaged code.  It is performed after
// the FlushProcessWriteBuffers call so that we know that once the thread reaches unmanaged code, it won't
// reenter managed code.  Therefore, the m_pTransitionFrame is stable.  Except that it isn't.  The return-to-
// managed sequence will temporarily overwrite the m_pTransitionFrame to be 0.  As a result, we need to cache
// the non-zero m_pTransitionFrame value that we saw during suspend so that stackwalks can read this value
// without concern of sometimes reading a 0, as would be the case if they read m_pTransitionFrame directly.
//
// Returns true if it sucessfully cached the transition frame (i.e. the thread was in unmanaged).
// Returns false otherwise.
//
bool Thread::CacheTransitionFrameForSuspend()
{
    if (m_pCachedTransitionFrame != NULL)
        return true;

    PTR_VOID temp = m_pTransitionFrame;     // volatile read
    if (temp == NULL)
        return false;

    m_pCachedTransitionFrame = temp;
    return true;
}

void Thread::ResetCachedTransitionFrame()
{
    // @TODO: I don't understand this assert because ResumeAllThreads is clearly written
    // to be reseting other threads' cached transition frames.

    //ASSERT((ThreadStore::GetCurrentThreadIfAvailable() == this) ||
    //       (m_pCachedTransitionFrame != NULL));
    m_pCachedTransitionFrame = NULL;
}

// This function simulates a PInvoke transition using a frame pointer from somewhere further up the stack that
// was passed in via the m_pHackPInvokeTunnel field.  It is used to allow us to grandfather-in the set of GC
// code that runs in cooperative mode without having to rewrite it in managed code.  The result is that the
// code that calls into this special mode must spill preserved registers as if it's going to PInvoke, but
// record its transition frame pointer in m_pHackPInvokeTunnel and leave the thread in the cooperative
// mode.  Later on, when this function is called, we effect the state transition to 'unmanaged' using the
// previously setup transition frame.
void Thread::EnablePreemptiveMode()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
#if !defined(HOST_WASM)
    ASSERT(m_pHackPInvokeTunnel != NULL);
#endif

    Unhijack();

    // ORDERING -- this write must occur before checking the trap
    m_pTransitionFrame = m_pHackPInvokeTunnel;

    // We need to prevent compiler reordering between above write and below read.  Both the read and the write
    // are volatile, so it's possible that the particular semantic for volatile that MSVC provides is enough,
    // but if not, this barrier would be required.  If so, it won't change anything to add the barrier.
    _ReadWriteBarrier();

    if (ThreadStore::IsTrapThreadsRequested())
    {
        WaitForSuspend();
    }
}

void Thread::DisablePreemptiveMode()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);

    // ORDERING -- this write must occur before checking the trap
    m_pTransitionFrame = NULL;

    // We need to prevent compiler reordering between above write and below read.  Both the read and the write
    // are volatile, so it's possible that the particular semantic for volatile that MSVC provides is enough,
    // but if not, this barrier would be required.  If so, it won't change anything to add the barrier.
    _ReadWriteBarrier();

    if (ThreadStore::IsTrapThreadsRequested() && (this != ThreadStore::GetSuspendingThread()))
    {
        WaitForGC(m_pHackPInvokeTunnel);
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

    m_numDynamicTypesTlsCells = 0;
    m_pDynamicTypesTlsCells = NULL;

    m_pThreadLocalModuleStatics = NULL;
    m_numThreadLocalModuleStatics = 0;

    // NOTE: We do not explicitly defer to the GC implementation to initialize the alloc_context.  The
    // alloc_context will be initialized to 0 via the static initialization of tls_CurrentThread. If the
    // alloc_context ever needs different initialization, a matching change to the tls_CurrentThread
    // static initialization will need to be made.

    m_uPalThreadIdForLogging = PalGetCurrentThreadIdForLogging();
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

    m_pTEB = PalNtCurrentTeb();

#ifdef STRESS_LOG
    if (StressLog::StressLogOn(~0u, 0))
        m_pThreadStressLog = StressLog::CreateThreadStressLog(this);
#endif // STRESS_LOG

    m_threadAbortException = NULL;
}

bool Thread::IsInitialized()
{
    return (m_ThreadStateFlags != TSF_Unknown);
}

// -----------------------------------------------------------------------------------------------------------
// GC support APIs - do not use except from GC itself
//
void Thread::SetGCSpecial(bool isGCSpecial)
{
    if (!IsInitialized())
        Construct();
    if (isGCSpecial)
        SetState(TSF_IsGcSpecialThread);
    else
        ClearState(TSF_IsGcSpecialThread);
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
    return m_uPalThreadIdForLogging;
}

bool Thread::IsCurrentThread()
{
    return m_threadId.IsCurrentThread();
}

void Thread::Detach()
{
    // Thread::Destroy is called when the thread's "home" fiber dies.  We mark the thread as "detached" here
    // so that we can validate, in our DLL_THREAD_DETACH handler, that the thread was already destroyed at that
    // point.
    SetDetached();

    RedhawkGCInterface::ReleaseAllocContext(GetAllocContext());
}

void Thread::Destroy()
{
    ASSERT(IsDetached());

    if (m_hPalThread != INVALID_HANDLE_VALUE)
        PalCloseHandle(m_hPalThread);

    if (m_pDynamicTypesTlsCells != NULL)
    {
        for (uint32_t i = 0; i < m_numDynamicTypesTlsCells; i++)
        {
            if (m_pDynamicTypesTlsCells[i] != NULL)
                delete[] m_pDynamicTypesTlsCells[i];
        }
        delete[] m_pDynamicTypesTlsCells;
    }

    if (m_pThreadLocalModuleStatics != NULL)
    {
        for (uint32_t i = 0; i < m_numThreadLocalModuleStatics; i++)
        {
            if (m_pThreadLocalModuleStatics[i] != NULL)
            {
                RhHandleFree(m_pThreadLocalModuleStatics[i]);
            }
        }
        delete[] m_pThreadLocalModuleStatics;
    }

#ifdef STRESS_LOG
    ThreadStressLog* ptsl = reinterpret_cast<ThreadStressLog*>(GetThreadStressLog());
    StressLog::ThreadDetach(ptsl);
#endif // STRESS_LOG
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
#ifdef HOST_WASM
    GcScanWasmShadowStack(pfnEnumCallback, pvCallbackData);
#else
    StackFrameIterator  frameIterator(this, GetTransitionFrame());
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
    PTR_VOID pTransitionFrame = GetTransitionFrame();
    if(pTransitionFrame != NULL)
    {
        StackFrameIterator  frameIterator(this, GetTransitionFrame());
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
#ifdef TARGET_ARM64
        GCRefKind reg0Kind = ExtractReg0ReturnKind(returnValueKind);
        GCRefKind reg1Kind = ExtractReg1ReturnKind(returnValueKind);

        // X0 and X1 are saved next to each other in this order
        if (reg0Kind != GCRK_Scalar)
        {
            RedhawkGCInterface::EnumGcRef(pHijackedReturnValue, reg0Kind, pfnEnumCallback, pvCallbackData);
        }
        if (reg1Kind != GCRK_Scalar)
        {
            RedhawkGCInterface::EnumGcRef(pHijackedReturnValue + 1, reg1Kind, pfnEnumCallback, pvCallbackData);
        }
#else
        RedhawkGCInterface::EnumGcRef(pHijackedReturnValue, returnValueKind, pfnEnumCallback, pvCallbackData);
#endif
    }

#ifndef DACCESS_COMPILE
    if (GetRuntimeInstance()->IsConservativeStackReportingEnabled())
    {
        if (frameIterator.IsValid())
        {
            PTR_VOID pLowerBound = dac_cast<PTR_VOID>(frameIterator.GetRegisterSet()->GetSP());

            // Transition frame may contain callee saved registers that need to be reported as well
            PTR_VOID pTransitionFrame = GetTransitionFrame();
            ASSERT(pTransitionFrame != NULL);
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
                                               pvCallbackData);
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
            // this this, it's considered a better trade-off than storing signature metadata for every potential
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

    // Keep alive the ThreadAbortException that's stored in the target thread during thread abort
    PTR_RtuObjectRef pThreadAbortExceptionObj = dac_cast<PTR_RtuObjectRef>(&m_threadAbortException);
    RedhawkGCInterface::EnumGcRef(pThreadAbortExceptionObj, GCRK_Object, pfnEnumCallback, pvCallbackData);
}

#ifndef DACCESS_COMPILE

#ifndef TARGET_ARM64
EXTERN_C void FASTCALL RhpGcProbeHijackScalar();
EXTERN_C void FASTCALL RhpGcProbeHijackObject();
EXTERN_C void FASTCALL RhpGcProbeHijackByref();

static void* NormalHijackTargets[3] =
{
    reinterpret_cast<void*>(RhpGcProbeHijackScalar), // GCRK_Scalar = 0,
    reinterpret_cast<void*>(RhpGcProbeHijackObject), // GCRK_Object = 1,
    reinterpret_cast<void*>(RhpGcProbeHijackByref)   // GCRK_Byref  = 2,
};
#else // TARGET_ARM64
EXTERN_C void FASTCALL RhpGcProbeHijack();

static void* NormalHijackTargets[1] =
{
    reinterpret_cast<void*>(RhpGcProbeHijack)
};
#endif // TARGET_ARM64

#ifdef FEATURE_GC_STRESS
#ifndef TARGET_ARM64
EXTERN_C void FASTCALL RhpGcStressHijackScalar();
EXTERN_C void FASTCALL RhpGcStressHijackObject();
EXTERN_C void FASTCALL RhpGcStressHijackByref();

static void* GcStressHijackTargets[3] =
{
    reinterpret_cast<void*>(RhpGcStressHijackScalar), // GCRK_Scalar = 0,
    reinterpret_cast<void*>(RhpGcStressHijackObject), // GCRK_Object = 1,
    reinterpret_cast<void*>(RhpGcStressHijackByref)   // GCRK_Byref  = 2,
};
#else // TARGET_ARM64
EXTERN_C void FASTCALL RhpGcStressHijack();

static void* GcStressHijackTargets[1] =
{
    reinterpret_cast<void*>(RhpGcStressHijack)
};
#endif // TARGET_ARM64
#endif // FEATURE_GC_STRESS

// static
bool Thread::IsHijackTarget(void * address)
{
    for (int i = 0; i < COUNTOF(NormalHijackTargets); i++)
    {
        if (NormalHijackTargets[i] == address)
            return true;
    }
#ifdef FEATURE_GC_STRESS
    for (int i = 0; i < COUNTOF(GcStressHijackTargets); i++)
    {
        if (GcStressHijackTargets[i] == address)
            return true;
    }
#endif // FEATURE_GC_STRESS
    return false;
}

bool Thread::Hijack()
{
    ASSERT(ThreadStore::GetCurrentThread() == ThreadStore::GetSuspendingThread());

    ASSERT_MSG(ThreadStore::GetSuspendingThread() != this, "You may not hijack a thread from itself.");

    if (m_hPalThread == INVALID_HANDLE_VALUE)
    {
        // cannot proceed
        return false;
    }

    // requires THREAD_SUSPEND_RESUME / THREAD_GET_CONTEXT / THREAD_SET_CONTEXT permissions

    Thread* pCurrentThread = ThreadStore::GetCurrentThread();
    pCurrentThread->EnterCantAllocRegion();
    uint32_t result = PalHijack(m_hPalThread, HijackCallback, this);
    pCurrentThread->LeaveCantAllocRegion();
    return result == 0;

}

UInt32_BOOL Thread::HijackCallback(HANDLE /*hThread*/, PAL_LIMITED_CONTEXT* pThreadContext, void* pCallbackContext)
{
    Thread* pThread = (Thread*) pCallbackContext;

    //
    // WARNING: The hijack operation will take a read lock on the RuntimeInstance's module list.
    // (This is done to find a Module based on an IP.)  Therefore, if the thread we've just
    // suspended owns the write lock on the module list, we'll deadlock with it when we try to
    // take the read lock below.  So we must attempt a non-blocking acquire of the read lock
    // early and fail the hijack if we can't get it.  This will cause us to simply retry later.
    //
    if (GetRuntimeInstance()->m_ModuleListLock.DangerousTryPulseReadLock())
    {
        if (pThread->CacheTransitionFrameForSuspend())
        {
            // IMPORTANT: GetThreadContext should not be trusted arbitrarily.  We are careful here to recheck
            // the thread's state flag that indicates whether or not it has made it to unmanaged code.  If
            // it has reached unmanaged code (even our own wait helper routines), then we cannot trust the
            // context returned by it.  This is due to various races that occur updating the reported context
            // during syscalls.
            return TRUE;
        }
        else
        {
            return pThread->InternalHijack(pThreadContext, NormalHijackTargets) ? TRUE : FALSE;
        }
    }

    return FALSE;
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
        pCurrentThread->InternalHijack(pSuspendCtx, GcStressHijackTargets);
    }
}
#endif // FEATURE_GC_STRESS

// This function is called in one of two scenarios:
// 1) from a thread to place a return hijack onto its own stack. This is only done for GC stress cases
//    via Thread::HijackForGcStress above.
// 2) from another thread to place a return hijack onto this thread's stack. In this case the target
//    thread is OS suspended someplace in managed code. The only constraint on the suspension is that the
//    stack be crawlable enough to yield the location of the return address.
bool Thread::InternalHijack(PAL_LIMITED_CONTEXT * pSuspendCtx, void * pvHijackTargets[])
{
    bool fSuccess = false;

    if (IsDoNotTriggerGcSet())
        return false;

    StackFrameIterator frameIterator(this, pSuspendCtx);

    if (frameIterator.IsValid())
    {
        frameIterator.CalculateCurrentMethodState();

        frameIterator.GetCodeManager()->UnsynchronizedHijackMethodLoops(frameIterator.GetMethodInfo());

        PTR_PTR_VOID ppvRetAddrLocation;
        GCRefKind retValueKind;

        if (frameIterator.GetCodeManager()->GetReturnAddressHijackInfo(frameIterator.GetMethodInfo(),
            frameIterator.GetRegisterSet(),
            &ppvRetAddrLocation,
            &retValueKind))
        {
            // ARM64 epilogs have a window between loading the hijackable return address into LR and the RET instruction.
            // We cannot hijack or unhijack a thread while it is suspended in that window unless we implement hijacking
            // via LR register modification. Therefore it is important to check our ability to hijack the thread before
            // unhijacking it.
            CrossThreadUnhijack();

            void* pvRetAddr = *ppvRetAddrLocation;
            ASSERT(ppvRetAddrLocation != NULL);
            ASSERT(pvRetAddr != NULL);

            ASSERT(StackFrameIterator::IsValidReturnAddress(pvRetAddr));

            m_ppvHijackedReturnAddressLocation = ppvRetAddrLocation;
            m_pvHijackedReturnAddress = pvRetAddr;
#ifdef TARGET_ARM64
            m_uHijackedReturnValueFlags = ReturnKindToTransitionFrameFlags(retValueKind);
            *ppvRetAddrLocation = pvHijackTargets[0];
#else
            void* pvHijackTarget = pvHijackTargets[retValueKind];
            ASSERT_MSG(IsHijackTarget(pvHijackTarget), "unexpected method used as hijack target");
            *ppvRetAddrLocation = pvHijackTarget;
#endif
            fSuccess = true;
        }
    }

    STRESS_LOG3(LF_STACKWALK, LL_INFO10000, "InternalHijack: TgtThread = %llx, IP = %p, result = %d\n",
        GetPalThreadIdForLogging(), pSuspendCtx->GetIp(), fSuccess);

    return fSuccess;
}

// This is the standard Unhijack, which is only allowed to be called on your own thread.
// Note that all the asm-implemented Unhijacks should also only be operating on their
// own thread.
void Thread::Unhijack()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    UnhijackWorker();
}

// This unhijack routine is only called from Thread::InternalHijack() to undo a possibly existing
// hijack before placing a new one. Although there are many code sequences (here and in asm) to
// perform an unhijack operation, they will never execute concurrently. A thread may unhijack itself
// at any time so long as it does so from unmanaged code. This ensures that another thread will not
// suspend it and attempt to unhijack it, since we only suspend threads that are executing managed
// code.
void Thread::CrossThreadUnhijack()
{
    ASSERT((ThreadStore::GetCurrentThread() == this) || DebugIsSuspended());
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
#ifdef TARGET_ARM64
    m_uHijackedReturnValueFlags         = 0;
#endif
}

#if _DEBUG
bool Thread::DebugIsSuspended()
{
    ASSERT(ThreadStore::GetCurrentThread() != this);
#if 0
    PalSuspendThread(m_hPalThread);
    uint32_t suspendCount = PalResumeThread(m_hPalThread);
    return (suspendCount > 0);
#else
    // @TODO: I don't trust the above implementation, so I want to implement this myself
    // by marking the thread state as "yes, we suspended it" and checking that state here.
    return true;
#endif
}
#endif

// @TODO: it would be very, very nice if we did not have to bleed knowledge of hijacking
// and hijack state to other components in the runtime. For now, these are only used
// when getting EH info during exception dispatch. We should find a better way to encapsulate
// this.
bool Thread::IsHijacked()
{
    // Note: this operation is only valid from the current thread. If one thread invokes
    // this on another then it may be racing with other changes to the thread's hijack state.
    ASSERT(ThreadStore::GetCurrentThread() == this);

    return m_pvHijackedReturnAddress != NULL;
}

//
// WARNING: This method must ONLY be called during stackwalks when we believe that all threads are
// synchronized and there is no other thread racing with us trying to apply hijacks.
//
bool Thread::DangerousCrossThreadIsHijacked()
{
    // If we have a CachedTransitionFrame available, then we're in the proper state.  Otherwise, this method
    // was called from an improper state.
    ASSERT(GetTransitionFrame() != NULL);
    return m_pvHijackedReturnAddress != NULL;
}

void * Thread::GetHijackedReturnAddress()
{
    // Note: this operation is only valid from the current thread. If one thread invokes
    // this on another then it may be racing with other changes to the thread's hijack state.
    ASSERT(IsHijacked());
    ASSERT(ThreadStore::GetCurrentThread() == this);

    return m_pvHijackedReturnAddress;
}

void * Thread::GetUnhijackedReturnAddress(void ** ppvReturnAddressLocation)
{
    ASSERT(ThreadStore::GetCurrentThread() == this);

    void * pvReturnAddress;
    if (m_ppvHijackedReturnAddressLocation == ppvReturnAddressLocation)
        pvReturnAddress = m_pvHijackedReturnAddress;
    else
        pvReturnAddress = *ppvReturnAddressLocation;

    ASSERT(NULL != GetRuntimeInstance()->FindCodeManagerByAddress(pvReturnAddress));
    return pvReturnAddress;
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

// Standard calling convention variant and actual implementation for RhpWaitForSuspend
EXTERN_C NOINLINE void FASTCALL RhpWaitForSuspend2()
{
    // The wait operation below may trash the last win32 error. We save the error here so that it can be
    // restored after the wait operation;
    int32_t lastErrorOnEntry = PalGetLastError();

    ThreadStore::GetCurrentThread()->WaitForSuspend();

    // Restore the saved error
    PalSetLastError(lastErrorOnEntry);
}

// Standard calling convention variant and actual implementation for RhpWaitForGC
EXTERN_C NOINLINE void FASTCALL RhpWaitForGC2(PInvokeTransitionFrame * pFrame)
{

    Thread * pThread = pFrame->m_pThread;

    if (pThread->IsDoNotTriggerGcSet())
        return;

    // The wait operation below may trash the last win32 error. We save the error here so that it can be
    // restored after the wait operation;
    int32_t lastErrorOnEntry = PalGetLastError();

    pThread->WaitForGC(pFrame);

    // Restore the saved error
    PalSetLastError(lastErrorOnEntry);
}

// Standard calling convention variant and actual implementation for RhpGcPoll
EXTERN_C NOINLINE void FASTCALL RhpGcPoll2(PInvokeTransitionFrame* pFrame)
{
    Thread* pThread = ThreadStore::GetCurrentThread();
    pFrame->m_pThread = pThread;

    RhpWaitForGC2(pFrame);
}

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



// Retrieve the start of the TLS storage block allocated for the given thread for a specific module identified
// by the TLS slot index allocated to that module and the offset into the OS allocated block at which
// Redhawk-specific data is stored.
PTR_UInt8 Thread::GetThreadLocalStorage(uint32_t uTlsIndex, uint32_t uTlsStartOffset)
{
#if 0
    return (*(uint8_t***)(m_pTEB + OFFSETOF__TEB__ThreadLocalStoragePointer))[uTlsIndex] + uTlsStartOffset;
#else
    return (*dac_cast<PTR_PTR_PTR_UInt8>(dac_cast<TADDR>(m_pTEB) + OFFSETOF__TEB__ThreadLocalStoragePointer))[uTlsIndex] + uTlsStartOffset;
#endif
}

PTR_UInt8 Thread::GetThreadLocalStorageForDynamicType(uint32_t uTlsTypeOffset)
{
    // Note: When called from GC root enumeration, no changes can be made by the AllocateThreadLocalStorageForDynamicType to
    // the 2 variables accessed here because AllocateThreadLocalStorageForDynamicType is called in cooperative mode.

    uTlsTypeOffset &= ~DYNAMIC_TYPE_TLS_OFFSET_FLAG;
    return dac_cast<PTR_UInt8>(uTlsTypeOffset < m_numDynamicTypesTlsCells ? m_pDynamicTypesTlsCells[uTlsTypeOffset] : NULL);
}

#ifndef DACCESS_COMPILE
PTR_UInt8 Thread::AllocateThreadLocalStorageForDynamicType(uint32_t uTlsTypeOffset, uint32_t tlsStorageSize, uint32_t numTlsCells)
{
    uTlsTypeOffset &= ~DYNAMIC_TYPE_TLS_OFFSET_FLAG;

    if (m_pDynamicTypesTlsCells == NULL || m_numDynamicTypesTlsCells <= uTlsTypeOffset)
    {
        // Keep at least a 2x grow so that we don't have to reallocate everytime a new type with TLS statics is created
        if (numTlsCells < 2 * m_numDynamicTypesTlsCells)
            numTlsCells = 2 * m_numDynamicTypesTlsCells;

        PTR_UInt8* pTlsCells = new (nothrow) PTR_UInt8[numTlsCells];
        if (pTlsCells == NULL)
            return NULL;

        memset(&pTlsCells[m_numDynamicTypesTlsCells], 0, sizeof(PTR_UInt8) * (numTlsCells - m_numDynamicTypesTlsCells));

        if (m_pDynamicTypesTlsCells != NULL)
        {
            memcpy(pTlsCells, m_pDynamicTypesTlsCells, sizeof(PTR_UInt8) * m_numDynamicTypesTlsCells);
            delete[] m_pDynamicTypesTlsCells;
        }

        m_pDynamicTypesTlsCells = pTlsCells;
        m_numDynamicTypesTlsCells = numTlsCells;
    }

    ASSERT(uTlsTypeOffset < m_numDynamicTypesTlsCells);

    if (m_pDynamicTypesTlsCells[uTlsTypeOffset] == NULL)
    {
        uint8_t* pTlsStorage = new (nothrow) uint8_t[tlsStorageSize];
        if (pTlsStorage == NULL)
            return NULL;

        // Initialize storage to 0's before returning it
        memset(pTlsStorage, 0, tlsStorageSize);

        m_pDynamicTypesTlsCells[uTlsTypeOffset] = pTlsStorage;
    }

    return m_pDynamicTypesTlsCells[uTlsTypeOffset];
}

#ifndef TARGET_UNIX
EXTERN_C REDHAWK_API uint32_t __cdecl RhCompatibleReentrantWaitAny(UInt32_BOOL alertable, uint32_t timeout, uint32_t count, HANDLE* pHandles)
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

    // save the previous transition frame
    pFrame->m_savedPInvokeTransitionFrame = m_pTransitionFrame;

    // set our mode to cooperative
    m_pTransitionFrame = NULL;

    // We need to prevent compiler reordering between above write and below read.
    _ReadWriteBarrier();

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

    // set our mode to cooperative
    m_pTransitionFrame = NULL;

    // We need to prevent compiler reordering between above write and below read.
    _ReadWriteBarrier();

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
    m_pTransitionFrame = pFrame->m_savedPInvokeTransitionFrame;
    if (ThreadStore::IsTrapThreadsRequested())
    {
        RhpWaitForSuspend2();
    }
}

FORCEINLINE void Thread::InlinePInvoke(PInvokeTransitionFrame * pFrame)
{
    pFrame->m_pThread = this;
    // set our mode to preemptive
    m_pTransitionFrame = pFrame;

    // We need to prevent compiler reordering between above write and below read.
    _ReadWriteBarrier();

    // now check if we need to trap the thread
    if (ThreadStore::IsTrapThreadsRequested())
    {
        RhpWaitForSuspend2();
    }
}

FORCEINLINE void Thread::InlinePInvokeReturn(PInvokeTransitionFrame * pFrame)
{
    m_pTransitionFrame = NULL;
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

Object* Thread::GetThreadStaticStorageForModule(uint32_t moduleIndex)
{
    // Return a pointer to the TLS storage if it has already been
    // allocated for the specified module.
    if (moduleIndex < m_numThreadLocalModuleStatics)
    {
        Object** threadStaticsStorageHandle = (Object**)m_pThreadLocalModuleStatics[moduleIndex];
        if (threadStaticsStorageHandle != NULL)
        {
            return *threadStaticsStorageHandle;
        }
    }

    return NULL;
}

bool Thread::SetThreadStaticStorageForModule(Object * pStorage, uint32_t moduleIndex)
{
    // Grow thread local storage if needed.
    if (m_numThreadLocalModuleStatics <= moduleIndex)
    {
        uint32_t newSize = moduleIndex + 1;
        if (newSize < moduleIndex)
        {
            return false;
        }

        PTR_PTR_VOID pThreadLocalModuleStatics = new (nothrow) PTR_VOID[newSize];
        if (pThreadLocalModuleStatics == NULL)
        {
            return false;
        }

        memset(&pThreadLocalModuleStatics[m_numThreadLocalModuleStatics], 0, sizeof(PTR_VOID) * (newSize - m_numThreadLocalModuleStatics));

        if (m_pThreadLocalModuleStatics != NULL)
        {
            memcpy(pThreadLocalModuleStatics, m_pThreadLocalModuleStatics, sizeof(PTR_VOID) * m_numThreadLocalModuleStatics);
            delete[] m_pThreadLocalModuleStatics;
        }

        m_pThreadLocalModuleStatics = pThreadLocalModuleStatics;
        m_numThreadLocalModuleStatics = newSize;
    }

    if (m_pThreadLocalModuleStatics[moduleIndex] != NULL)
    {
        RhHandleSet(m_pThreadLocalModuleStatics[moduleIndex], pStorage);
    }
    else
    {
        void* threadStaticsStorageHandle = RhpHandleAlloc(pStorage, 2 /* Normal */);
        if (threadStaticsStorageHandle == NULL)
        {
            return false;
        }
        m_pThreadLocalModuleStatics[moduleIndex] = threadStaticsStorageHandle;
    }

    return true;
}

COOP_PINVOKE_HELPER(Object*, RhGetThreadStaticStorageForModule, (uint32_t moduleIndex))
{
    Thread * pCurrentThread = ThreadStore::RawGetCurrentThread();
    return pCurrentThread->GetThreadStaticStorageForModule(moduleIndex);
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhSetThreadStaticStorageForModule, (Array * pStorage, uint32_t moduleIndex))
{
    Thread * pCurrentThread = ThreadStore::RawGetCurrentThread();
    FC_RETURN_BOOL(pCurrentThread->SetThreadStaticStorageForModule((Object*)pStorage, moduleIndex));
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
EXTERN_C NOINLINE void FASTCALL RhpReversePInvokeAttachOrTrapThread2(ReversePInvokeFrame * pFrame)
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

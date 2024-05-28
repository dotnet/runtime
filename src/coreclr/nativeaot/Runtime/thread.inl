// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __thread_inl__
#define __thread_inl__

// TODO: try to find out where the events symbols are defined
//#include "eventtracebase.h"
//#include "ClrEtwAll.h"

#include "thread.h"

//#include "gcenv.h"
//#include "gcenv.ee.h"

//#include "gcinterface.h"
// TODO: work around to compile because #include "gcinterface.h" generates more compilation errors
//#ifndef gc_alloc_context
//struct gc_alloc_context
//{
//    uint8_t* alloc_ptr;
//    uint8_t* alloc_limit;
//};
//#endif


#ifndef DACCESS_COMPILE
// Set the m_pDeferredTransitionFrame field for GC allocation helpers that setup transition frame
// in assembly code. Do not use anywhere else.
inline void Thread::SetDeferredTransitionFrame(PInvokeTransitionFrame* pTransitionFrame)
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(Thread::IsCurrentThreadInCooperativeMode());
    ASSERT(!Thread::IsHijackTarget(pTransitionFrame->m_RIP));
    m_pDeferredTransitionFrame = pTransitionFrame;
}

// Setup the m_pDeferredTransitionFrame field for GC helpers entered via regular PInvoke.
// Do not use anywhere else.
inline void Thread::DeferTransitionFrame()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(!Thread::IsCurrentThreadInCooperativeMode());
    m_pDeferredTransitionFrame = m_pTransitionFrame;
}
#endif // DACCESS_COMPILE

inline bool Thread::IsWithinStackBounds(PTR_VOID p)
{
    ASSERT((m_pStackLow != 0) && (m_pStackHigh != 0));
    return (m_pStackLow <= p) && (p < m_pStackHigh);
}

inline void Thread::GetStackBounds(PTR_VOID * ppStackLow, PTR_VOID * ppStackHigh)
{
    ASSERT((m_pStackLow != 0) && (m_pStackHigh != 0));
    *ppStackLow = m_pStackLow;
    *ppStackHigh = m_pStackHigh;
}

#ifndef DACCESS_COMPILE
inline void Thread::SetThreadStressLog(void* ptsl)
{
    m_pThreadStressLog = ptsl;
}
#endif // DACCESS_COMPILE

inline PTR_VOID Thread::GetThreadStressLog() const
{
    return m_pThreadStressLog;
}

inline void Thread::PushGCFrameRegistration(GCFrameRegistration* pRegistration)
{
    pRegistration->m_pNext = m_pGCFrameRegistrations;
    m_pGCFrameRegistrations = pRegistration;
}

inline void Thread::PopGCFrameRegistration(GCFrameRegistration* pRegistration)
{
    ASSERT(m_pGCFrameRegistrations == pRegistration);
    m_pGCFrameRegistrations = pRegistration->m_pNext;
}

inline gc_alloc_context* Thread::GetAllocContext()
{
    return (gc_alloc_context*)m_rgbAllocContextBuffer;
}

inline uint8_t** Thread::GetCombinedLimit()
{
    return &m_combined_limit;
}

static inline bool IsRandomizedSamplingEnabled()
{
#ifdef FEATURE_EVENT_TRACE
    // TODO: fix the same compilation error
    // look at eventtrace_gcheap.cpp - RUNTIME_PROVIDER_CATEGORY_ENABLED
    //return ETW_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
    //    TRACE_LEVEL_INFORMATION,
    //    CLR_ALLOCATIONSAMPLING_KEYWORD);
    //return RUNTIME_PROVIDER_CATEGORY_ENABLED(TRACE_LEVEL_INFORMATION, CLR_ALLOCATIONSAMPLING_KEYWORD);

    return false;
#else
    return false;
#endif // FEATURE_EVENT_TRACE
}

static inline int ComputeGeometricRandom()
{
    // TODO: Implement a proper random number generator
    // compute a random sample from the Geometric distribution
    //double probability = GetRandomizer()->NextDouble();
    //int threshold = (int)(-log(1 - probability) * SamplingDistributionMean);
    //return threshold;

    // ensure to never end up inside the allocation context to avoid sampling
    return SamplingDistributionMean;
}

// Regenerate the randomized sampling limit and update the m_combined_limit field.
inline void Thread::UpdateCombinedLimit()
{
    UpdateCombinedLimit(IsRandomizedSamplingEnabled());
}

inline void Thread::UpdateCombinedLimit(bool samplingEnabled)
{
    // TODO: no op implementation but it does not seem possible to access gc_alloc_context fields in this file
    // m_combined_limit = alloc_context->alloc_limit;

    //gc_alloc_context* alloc_context = GetAllocContext();
    //if (!samplingEnabled)
    //{
    //    m_combined_limit = alloc_context->alloc_limit;
    //}
    //else
    //{
    //    // compute the next sampling limit based on a geometric distribution
    //    uint8_t* sampling_limit = alloc_context->alloc_ptr + ComputeGeometricRandom();

    //    // if the sampling limit is larger than the allocation context, no sampling will occur in this AC
    //    m_combined_limit = (sampling_limit < alloc_context->alloc_limit) ? sampling_limit : alloc_context->alloc_limit;
    //}
}

inline bool Thread::IsStateSet(ThreadStateFlags flags)
{
    return ((m_ThreadStateFlags & flags) == (uint32_t)flags);
}

inline bool Thread::IsDoNotTriggerGcSet()
{
    return IsStateSet(TSF_DoNotTriggerGc);
}

inline bool Thread::IsCurrentThreadInCooperativeMode()
{
#ifndef DACCESS_COMPILE
    ASSERT(ThreadStore::GetCurrentThread() == this);
#endif // !DACCESS_COMPILE
    return (m_pTransitionFrame == NULL);
}

// define a specialized version locally so that we do not need to depend on entire gcenv.h
inline void VolatileStoreWithoutBarrier(PInvokeTransitionFrame** frameRef, PInvokeTransitionFrame* val)
{
    *(PInvokeTransitionFrame* volatile*)frameRef = val;
}

FORCEINLINE void Thread::InlineReversePInvokeReturn(ReversePInvokeFrame* pFrame)
{
    // set our mode to preemptive
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, pFrame->m_savedPInvokeTransitionFrame);
}

FORCEINLINE void Thread::InlinePInvoke(PInvokeTransitionFrame* pFrame)
{
    ASSERT(!IsDoNotTriggerGcSet() || ThreadStore::IsTrapThreadsRequested());
    pFrame->m_pThread = this;
    // set our mode to preemptive
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, pFrame);
}

EXTERN_C void FASTCALL RhpWaitForGC2(PInvokeTransitionFrame* pFrame);

FORCEINLINE void Thread::InlinePInvokeReturn(PInvokeTransitionFrame* pFrame)
{
    // must be in cooperative mode when checking the trap flag
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, (PInvokeTransitionFrame*)nullptr);
    if (ThreadStore::IsTrapThreadsRequested())
    {
        RhpWaitForGC2(pFrame);
    }
}

FORCEINLINE bool Thread::InlineTryFastReversePInvoke(ReversePInvokeFrame* pFrame)
{
    // remember the current transition frame, so it will be restored when we return from reverse pinvoke
    pFrame->m_savedPInvokeTransitionFrame = m_pTransitionFrame;

    // If the thread is already in cooperative mode, this is a bad transition that will be a fail fast unless we are in
    // a do not trigger mode.  The exception to the rule allows us to have [UnmanagedCallersOnly] methods that are called via
    // the "restricted GC callouts" as well as from native, which is necessary because the methods are CCW vtable
    // methods on interfaces passed to native.
    // We will allow threads in DoNotTriggerGc mode to do reverse PInvoke regardless of their coop state.
    if (IsDoNotTriggerGcSet())
    {
        // We expect this scenario only when EE is stopped.
        ASSERT(ThreadStore::IsTrapThreadsRequested());
        // no need to do anything
        return true;
    }

    // Do we need to attach the thread?
    if (!IsStateSet(TSF_Attached))
        return false; // thread is not attached

    if (IsCurrentThreadInCooperativeMode())
        return false; // bad transition

    // this is an ordinary transition to managed code
    // GC threads should not do that
    ASSERT(!IsGCSpecial());

    // must be in cooperative mode when checking the trap flag
    VolatileStoreWithoutBarrier(&m_pTransitionFrame, (PInvokeTransitionFrame*)nullptr);

    // now check if we need to trap the thread
    if (ThreadStore::IsTrapThreadsRequested())
    {
        // put the previous frame back (sets us back to preemptive mode)
        m_pTransitionFrame = pFrame->m_savedPInvokeTransitionFrame;
        return false; // need to trap the thread
    }

    return true;
}

#endif // __thread_inl__

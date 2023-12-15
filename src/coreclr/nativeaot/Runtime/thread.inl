// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

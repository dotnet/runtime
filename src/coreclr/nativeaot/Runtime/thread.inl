// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DACCESS_COMPILE
inline void Thread::SetDeferredTransitionFrame(PInvokeTransitionFrame* pTransitionFrame)
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(Thread::IsCurrentThreadInCooperativeMode());
    m_pDeferredTransitionFrame = pTransitionFrame;
}

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

inline void Thread::EnterForbidBlockingRegion()
{
#ifdef _DEBUG
    m_forbidBlocking++;
#endif
}

inline void Thread::LeaveForbidBlockingRegion()
{
#ifdef _DEBUG
    m_forbidBlocking--;
#endif
}

inline bool Thread::IsInForbidBlockingRegion()
{
#ifdef _DEBUG
    return m_forbidBlocking != 0;
#else
    return false;
#endif
}

struct ForbidBlockingHolder
{
    Thread* m_pThread;
    ForbidBlockingHolder(Thread* pThread)
    {
        m_pThread = pThread;
        m_pThread->EnterForbidBlockingRegion();
    }

    ~ForbidBlockingHolder()
    {
        m_pThread->LeaveForbidBlockingRegion();
    }
};

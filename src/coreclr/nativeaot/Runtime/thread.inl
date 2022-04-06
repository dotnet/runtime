// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DACCESS_COMPILE
inline void Thread::SetCurrentThreadPInvokeTunnelForGcAlloc(void * pTransitionFrame)
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(Thread::IsCurrentThreadInCooperativeMode());
    m_pHackPInvokeTunnel = pTransitionFrame;
}

inline void Thread::SetupHackPInvokeTunnel()
{
    ASSERT(ThreadStore::GetCurrentThread() == this);
    ASSERT(!Thread::IsCurrentThreadInCooperativeMode());
    m_pHackPInvokeTunnel = m_pTransitionFrame;
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

inline void Thread::EnterCantAllocRegion()
{
    m_cantAlloc++;
}

inline void Thread::LeaveCantAllocRegion()
{
    m_cantAlloc--;
}

inline bool Thread::IsInCantAllocStressLogRegion()
{
    return m_cantAlloc != 0;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


//
//
/*============================================================
**
** Header:  Threads.inl
**
** Purpose: Implements Thread inline functions
**
**
===========================================================*/
#ifndef _THREADS_INL
#define _THREADS_INL

#include "threads.h"
#include "appdomain.hpp"
#include "frames.h"

#ifndef DACCESS_COMPILE

#ifdef HOST_WINDOWS
EXTERN_C UINT32 _tls_index;
#endif

#ifdef _MSC_VER
__declspec(selectany) __declspec(thread) ThreadLocalInfo gCurrentThreadInfo;
#else
EXTERN_C __thread ThreadLocalInfo gCurrentThreadInfo;
#endif

inline Thread* GetThreadNULLOk()
{
    return gCurrentThreadInfo.m_pThread;
}

inline Thread* GetThread()
{
    Thread* pThread = gCurrentThreadInfo.m_pThread;
    _ASSERTE(pThread);
    return pThread;
}

EXTERN_C inline AppDomain* STDCALL GetAppDomain()
{
    return AppDomain::GetCurrentDomain();
}

#endif // !DACCESS_COMPILE

inline
Frame* Thread::FindFrame(SIZE_T StackPointer)
{
    Frame* pFrame = GetFrame();

    while ((SIZE_T)pFrame < StackPointer)
    {
        pFrame = pFrame->Next();
    }

    return pFrame;
}

inline void Thread::SetThrowable(OBJECTREF pThrowable DEBUG_ARG(ThreadExceptionState::SetThrowableErrorChecking stecFlags))
{
    WRAPPER_NO_CONTRACT;

    m_ExceptionState.SetThrowable(pThrowable DEBUG_ARG(stecFlags));
}

// get the current notification (if any) from this thread
inline OBJECTHANDLE Thread::GetThreadCurrNotification()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return m_hCurrNotification;
}

// set the current notification (if any) from this thread
inline void Thread::SetThreadCurrNotification(OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_hCurrNotification = handle;
}

// clear the current notification (if any) from this thread
inline void Thread::ClearThreadCurrNotification()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_hCurrNotification = NULL;
}


inline OBJECTREF Thread::GetExposedObjectRaw()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return ObjectFromHandle(m_ExposedObject);
}

#ifdef FEATURE_COMINTEROP
inline void Thread::RevokeApartmentSpy()
{
    LIMITED_METHOD_CONTRACT;

    if (m_fInitializeSpyRegistered)
    {
        VERIFY(SUCCEEDED(CoRevokeInitializeSpy(m_uliInitializeSpyCookie)));
        m_fInitializeSpyRegistered = false;
    }
}

inline LPVOID Thread::GetLastSTACtxCookie(BOOL *pfNAContext)
{
    LIMITED_METHOD_CONTRACT;
    *pfNAContext = ((UINT_PTR)m_pLastSTACtxCookie & 1);
    return (LPVOID)((UINT_PTR)m_pLastSTACtxCookie & ~1);
}

inline void Thread::SetLastSTACtxCookie(LPVOID pCtxCookie, BOOL fNAContext)
{
    LIMITED_METHOD_CONTRACT;
    if (fNAContext)
    {
        // The ctx cookie is an interface pointer so we can steal the lowest bit
        // to mark whether the context is known to be Neutral Apartment or not.
        m_pLastSTACtxCookie = (LPVOID)((UINT_PTR)pCtxCookie | 1);
    }
    else
    {
        m_pLastSTACtxCookie = pCtxCookie;
    }
}
#endif // FEATURE_COMINTEROP

inline bool Thread::IsGCSpecial()
{
    LIMITED_METHOD_CONTRACT;
    return m_fGCSpecial;
}

inline void Thread::SetGCSpecial(bool fGCSpecial)
{
    LIMITED_METHOD_CONTRACT;
    m_fGCSpecial = fGCSpecial;
}

#if !defined(DACCESS_COMPILE)

inline Thread::CurrentPrepareCodeConfigHolder::CurrentPrepareCodeConfigHolder(Thread *thread, PrepareCodeConfig *config)
    : m_thread(thread)
#ifdef _DEBUG
    , m_config(config)
#endif
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(thread == GetThread());
    _ASSERTE(config != nullptr);

    PrepareCodeConfig *previousConfig = thread->m_currentPrepareCodeConfig;
    if (previousConfig != nullptr)
    {
        config->SetNextInSameThread(previousConfig);
    }
    thread->m_currentPrepareCodeConfig = config;
}

inline Thread::CurrentPrepareCodeConfigHolder::~CurrentPrepareCodeConfigHolder()
{
    LIMITED_METHOD_CONTRACT;

    PrepareCodeConfig *config = m_thread->m_currentPrepareCodeConfig;
    _ASSERTE(config == m_config);
    m_thread->m_currentPrepareCodeConfig = config->GetNextInSameThread();
    config->SetNextInSameThread(nullptr);
}

inline void Thread::EnterForbidSuspendForDebuggerRegion()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(this == GetThread());

    _ASSERTE(!m_isInForbidSuspendForDebuggerRegion);
    m_isInForbidSuspendForDebuggerRegion = true;
}

inline void Thread::ExitForbidSuspendForDebuggerRegion()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(this == GetThread());

    _ASSERTE(m_isInForbidSuspendForDebuggerRegion);
    m_isInForbidSuspendForDebuggerRegion = false;
}

#ifdef HOST_WINDOWS
inline size_t Thread::GetOffsetOfThreadStatic(void* pThreadStatic)
{
    LIMITED_METHOD_CONTRACT;

    PTEB Teb = NtCurrentTeb();
    BYTE** tlsArray = (BYTE**)Teb->ThreadLocalStoragePointer;
    BYTE* tlsData = (BYTE*)tlsArray[_tls_index];

    return (BYTE*)pThreadStatic - tlsData;
}
#endif

#endif // !DACCESS_COMPILE

#endif

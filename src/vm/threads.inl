// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#ifdef FEATURE_IMPLICIT_TLS

#ifndef __llvm__
EXTERN_C __declspec(thread) ThreadLocalInfo gCurrentThreadInfo;
#else // !__llvm__
EXTERN_C __thread ThreadLocalInfo gCurrentThreadInfo;
#endif // !__llvm__

EXTERN_C inline Thread* STDCALL GetThread()
{
    return gCurrentThreadInfo.m_pThread;
}

EXTERN_C inline AppDomain* STDCALL GetAppDomain()
{
    return gCurrentThreadInfo.m_pAppDomain;
}

#endif // FEATURE_IMPLICIT_TLS
#endif // !DACCESS_COMPILE

#ifdef ENABLE_GET_THREAD_GENERIC_FULL_CHECK
// See code:GetThreadGenericFullCheck
inline /* static */ BOOL Thread::ShouldEnforceEEThreadNotRequiredContracts()
{
    LIMITED_METHOD_CONTRACT;
    return s_fEnforceEEThreadNotRequiredContracts;
}
#endif // ENABLE_GET_THREAD_GENERIC_FULL_CHECK

inline void Thread::IncLockCount()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(GetThread() == this);
    m_dwLockCount++;
    _ASSERTE(m_dwLockCount != 0 || HasThreadStateNC(TSNC_UnbalancedLocks) || GetDomain()->OkToIgnoreOrphanedLocks());
}

inline void Thread::DecLockCount()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(GetThread() == this);
    _ASSERTE(m_dwLockCount > 0 || HasThreadStateNC(TSNC_UnbalancedLocks) || GetDomain()->OkToIgnoreOrphanedLocks());
    m_dwLockCount--;
}

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

inline void Thread::SetKickOffDomainId(ADID ad)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    m_pKickOffDomainId = ad;
}


inline ADID Thread::GetKickOffDomainId()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    _ASSERTE(m_pKickOffDomainId.m_dwId != 0);
    return m_pKickOffDomainId;
}

// get the current notification (if any) from this thread
inline OBJECTHANDLE Thread::GetThreadCurrNotification()
{
    CONTRACTL
    {
        SO_NOT_MAINLINE;
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
        SO_NOT_MAINLINE;
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
        SO_NOT_MAINLINE;
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
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return ObjectFromHandle(m_ExposedObject);
}

inline void Thread::FinishSOWork()
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_STACK_PROBE
    if (HasThreadStateNC(TSNC_SOWorkNeeded))
    {
        ResetThreadStateNC(TSNC_SOWorkNeeded);
        // Wake up AD unload thread to finish SO work that is delayed due to limit stack
        AppDomain::EnableADUnloadWorkerForThreadAbort();
    }
#else
    _ASSERTE(!HasThreadStateNC(TSNC_SOWorkNeeded));
#endif
}

inline DWORD Thread::IncrementOverridesCount()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.IncrementOverridesCount();
}

inline DWORD Thread::DecrementOverridesCount()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.DecrementOverridesCount();
}

inline DWORD Thread::GetOverridesCount()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.GetOverridesCount();
}

inline DWORD Thread::IncrementAssertCount()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.IncrementAssertCount();
}

inline DWORD Thread::DecrementAssertCount()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.DecrementAssertCount();
}

inline DWORD Thread::GetAssertCount()
{
    LIMITED_METHOD_CONTRACT;
    return m_ADStack.GetAssertCount();
}

#ifndef DACCESS_COMPILE
inline void Thread::PushDomain(ADID pDomain)
{
    WRAPPER_NO_CONTRACT;
    m_ADStack.PushDomain(pDomain);
}

inline ADID Thread::PopDomain()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.PopDomain();
}
#endif // DACCESS_COMPILE

inline DWORD Thread::GetNumAppDomainsOnThread()
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.GetNumDomains();
}

inline BOOL Thread::CheckThreadWideSpecialFlag(DWORD flags)
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.GetThreadWideSpecialFlag() & flags;
}

inline void Thread::InitDomainIteration(DWORD *pIndex)
{
    WRAPPER_NO_CONTRACT;
    m_ADStack.InitDomainIteration(pIndex);
}

inline ADID Thread::GetNextDomainOnStack(DWORD *pIndex, DWORD *pOverrides, DWORD *pAsserts)
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.GetNextDomainOnStack(pIndex, pOverrides, pAsserts);
}

inline void Thread::UpdateDomainOnStack(DWORD pIndex, DWORD asserts, DWORD overrides)
{
    WRAPPER_NO_CONTRACT;
    return m_ADStack.UpdateDomainOnStack(pIndex, asserts, overrides);
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

#include "appdomainstack.inl"

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

#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// EEToProfInterfaceImpl.inl
//

//
// Inline implementation of portions of the code that wraps calling into
// the profiler's implementation of ICorProfilerCallback*
//

// ======================================================================================

#ifndef __EETOPROFINTERFACEIMPL_INL__
#define __EETOPROFINTERFACEIMPL_INL__

#include "profilepriv.h"
#include "profilepriv.inl"
#include "simplerwlock.hpp"

// ----------------------------------------------------------------------------
// EEToProfInterfaceImpl::IsCallback3Supported
//
// Description:
//     Returns BOOL indicating whether the profiler implements
//     ICorProfilerCallback3.
//

inline BOOL EEToProfInterfaceImpl::IsCallback3Supported()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pCallback3 != NULL);
}

// ----------------------------------------------------------------------------
// EEToProfInterfaceImpl::IsCallback4Supported
//
// Description:
//     Returns BOOL indicating whether the profiler implements
//     ICorProfilerCallback4.
//

inline BOOL EEToProfInterfaceImpl::IsCallback4Supported()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pCallback4 != NULL);
}

inline BOOL EEToProfInterfaceImpl::IsCallback5Supported()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pCallback5 != NULL);
}

inline BOOL EEToProfInterfaceImpl::IsCallback6Supported()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pCallback6 != NULL);
}

inline BOOL EEToProfInterfaceImpl::IsCallback7Supported()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pCallback7 != NULL);
}

inline BOOL EEToProfInterfaceImpl::IsCallback8Supported()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pCallback8 != NULL);
}

inline FunctionIDMapper * EEToProfInterfaceImpl::GetFunctionIDMapper()
{
    LIMITED_METHOD_CONTRACT;
    return m_pProfilersFuncIDMapper;
}

inline FunctionIDMapper2 * EEToProfInterfaceImpl::GetFunctionIDMapper2()
{
    LIMITED_METHOD_CONTRACT;
    return m_pProfilersFuncIDMapper2;
}

inline void EEToProfInterfaceImpl::SetFunctionIDMapper(FunctionIDMapper * pFunc)
{
    LIMITED_METHOD_CONTRACT;
    m_pProfilersFuncIDMapper = pFunc;
}

inline void EEToProfInterfaceImpl::SetFunctionIDMapper2(FunctionIDMapper2 * pFunc, void * clientData)
{
    LIMITED_METHOD_CONTRACT;
    m_pProfilersFuncIDMapper2 = pFunc;
    m_pProfilersFuncIDMapper2ClientData = clientData;
}

inline BOOL EEToProfInterfaceImpl::IsLoadedViaAttach()
{
    LIMITED_METHOD_CONTRACT;
    return m_fLoadedViaAttach;
}

inline void EEToProfInterfaceImpl::SetUnrevertiblyModifiedILFlag()
{
    LIMITED_METHOD_CONTRACT;
    m_fUnrevertiblyModifiedIL = TRUE;
}

inline void EEToProfInterfaceImpl::SetModifiedRejitState()
{
    LIMITED_METHOD_CONTRACT;
    m_fModifiedRejitState = TRUE;
}

inline FunctionEnter * EEToProfInterfaceImpl::GetEnterHook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pEnter;
}

inline FunctionLeave * EEToProfInterfaceImpl::GetLeaveHook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pLeave;
}

inline FunctionTailcall * EEToProfInterfaceImpl::GetTailcallHook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pTailcall;
}

inline FunctionEnter2 * EEToProfInterfaceImpl::GetEnter2Hook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pEnter2;
}

inline FunctionLeave2 * EEToProfInterfaceImpl::GetLeave2Hook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pLeave2;
}

inline FunctionTailcall2 * EEToProfInterfaceImpl::GetTailcall2Hook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pTailcall2;
}

inline FunctionEnter3 * EEToProfInterfaceImpl::GetEnter3Hook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pEnter3;
}

inline FunctionLeave3 * EEToProfInterfaceImpl::GetLeave3Hook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pLeave3;
}

inline FunctionTailcall3 * EEToProfInterfaceImpl::GetTailcall3Hook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pTailcall3;
}

inline FunctionEnter3WithInfo * EEToProfInterfaceImpl::GetEnter3WithInfoHook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pEnter3WithInfo;
}

inline FunctionLeave3WithInfo * EEToProfInterfaceImpl::GetLeave3WithInfoHook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pLeave3WithInfo;
}

inline FunctionTailcall3WithInfo * EEToProfInterfaceImpl::GetTailcall3WithInfoHook()
{
    LIMITED_METHOD_CONTRACT;
    return m_pTailcall3WithInfo;
}

inline BOOL EEToProfInterfaceImpl::IsClientIDToFunctionIDMappingEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return m_fIsClientIDToFunctionIDMappingEnabled;
}

//---------------------------------------------------------------------------------------
//
// Lookup the clientID for a given functionID
//
// Arguments:
//     functionID
//
// Return Value:
//     If found a match, return clientID; Otherwise return NULL.
//
inline UINT_PTR EEToProfInterfaceImpl::LookupClientIDFromCache(FunctionID functionID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(functionID != NULL);

    SimpleReadLockHolder readLockHolder(m_pFunctionIDHashTableRWLock);
    const FunctionIDAndClientID * entry = m_pFunctionIDHashTable->LookupPtr(functionID);

    // entry can be NULL when OOM happens.
    if (entry != NULL)
    {
        return entry->clientID;
    }
    else
    {
        return NULL;
    }
}

//---------------------------------------------------------------------------------------
//
// Returns whether the profiler chose options that require the JIT to compile with the
// CORINFO_GENERICS_CTXT_KEEP_ALIVE flag.
//
// Return Value:
//    Nonzero iff the JIT should compile with CORINFO_GENERICS_CTXT_KEEP_ALIVE.
//

inline BOOL EEToProfInterfaceImpl::RequiresGenericsContextForEnterLeave()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    return
        CORProfilerPresent() &&
        ((&g_profControlBlock)->globalEventMask.IsEventMaskSet(COR_PRF_ENABLE_FRAME_INFO)) &&
        (
            (m_pEnter2            != NULL) ||
            (m_pLeave2            != NULL) ||
            (m_pTailcall2         != NULL) ||
            (m_pEnter3WithInfo    != NULL) ||
            (m_pLeave3WithInfo    != NULL) ||
            (m_pTailcall3WithInfo != NULL)
        );
}

inline BOOL EEToProfInterfaceImpl::HasTimedOutWaitingForConcurrentGC()
{
    LIMITED_METHOD_CONTRACT;
    return m_bHasTimedOutWaitingForConcurrentGC;
}

#endif // __EETOPROFINTERFACEIMPL_INL__


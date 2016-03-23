// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
** Header:  AppDomain.i
** 

**
** Purpose: Implements AppDomain (loader domain) architecture
** inline functions
**
**
===========================================================*/
#ifndef _APPDOMAIN_I
#define _APPDOMAIN_I

#ifndef DACCESS_COMPILE

#include "appdomain.hpp"

#ifdef FEATURE_CORECLR 
inline void BaseDomain::SetAppDomainCompatMode(AppDomainCompatMode compatMode)
{
    LIMITED_METHOD_CONTRACT;
    m_CompatMode = compatMode;
}

inline BaseDomain::AppDomainCompatMode BaseDomain::GetAppDomainCompatMode()
{
    LIMITED_METHOD_CONTRACT;
    return m_CompatMode;
}
#endif // FEATURE_CORECLR

inline void AppDomain::SetUnloadInProgress(AppDomain *pThis)
{
    WRAPPER_NO_CONTRACT;

    SystemDomain::System()->SetUnloadInProgress(pThis);
}

inline void AppDomain::SetUnloadComplete(AppDomain *pThis)
{
    GCX_COOP();

    SystemDomain::System()->SetUnloadComplete();
}

inline  void AppDomain::EnterContext(Thread* pThread, Context* pCtx,ContextTransitionFrame *pFrame)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(CheckPointer(pCtx));
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(pCtx->GetDomain()==this);
    }
    CONTRACTL_END;
    pThread->EnterContextRestricted(pCtx,pFrame);
};


inline AppDomainFromIDHolder::~AppDomainFromIDHolder()
{
    WRAPPER_NO_CONTRACT;
#ifdef _DEBUG
    if(m_bAcquired)
        Release();
#endif    
}

inline void AppDomainFromIDHolder::Release()
{
    //do not use real contract here!
    WRAPPER_NO_CONTRACT;
#ifdef _DEBUG
    if(m_bAcquired)
    {
        if (m_type==SyncType_GC)
#ifdef ENABLE_CONTRACTS_IMPL
        {
            if (GetThread())
            {
                STRESS_LOG1(LF_APPDOMAIN, LL_INFO10000, "AppDomainFromIDHolder::Assign is allowing GC - %08x",this);
                GetThread()->EndForbidGC();
            }
            else
            {
                if (!IsGCThread())
                {
                    _ASSERTE(!"Should not be called from a non GC thread");
                }
            }
        }
#else
            m_pDomain=NULL;
#endif
        else
        if (m_type==SyncType_ADLock)
            SystemDomain::m_SystemDomainCrst.SetCantLeave(FALSE);
        else
        {
            _ASSERTE(!"Unknown type");        
        }
        m_pDomain=NULL;
        m_bAcquired=FALSE;
    }
#endif
}

inline void AppDomainFromIDHolder::Assign(ADID id, BOOL bUnsafePoint)
{
    //do not use real contract here!
    WRAPPER_NO_CONTRACT;
    TESTHOOKCALL(AppDomainCanBeUnloaded(id.m_dwId, bUnsafePoint));
#ifdef _DEBUG
    m_bChecked=FALSE;
    if (m_type==SyncType_GC)
    {
#ifdef ENABLE_CONTRACTS_IMPL
        if (GetThread())
        {
            _ASSERTE(GetThread()->PreemptiveGCDisabled());
            STRESS_LOG1(LF_APPDOMAIN, LL_INFO10000, "AppDomainFromIDHolder::Assign is forbidding GC - %08x",this);
            GetThread()->BeginForbidGC(__FILE__, __LINE__);
        }
        else
        {
            if (!IsGCThread())
            {
                _ASSERTE(!"Should not be called from a non GC thread");
            }
        }
#endif
    }
    else
    if (m_type==SyncType_ADLock)    
    {
        _ASSERTE(SystemDomain::m_SystemDomainCrst.OwnedByCurrentThread());
        SystemDomain::m_SystemDomainCrst.SetCantLeave(TRUE);
    }
    else
    {
        _ASSERT(!"NI");
    }

    m_bAcquired=TRUE;
 #endif
    m_pDomain=SystemDomain::GetAppDomainAtId(id);

}



inline void AppDomainFromIDHolder::ThrowIfUnloaded()
{
    STATIC_CONTRACT_THROWS;
    if (IsUnloaded())
    {
        COMPlusThrow(kAppDomainUnloadedException);
    }
#ifdef _DEBUG
    m_bChecked=TRUE;
#endif
}

inline AppDomain* AppDomainFromIDHolder::operator ->()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_bChecked && m_bAcquired);    
    return m_pDomain;
}

inline DomainAssembly* AppDomain::FindDomainAssembly(Assembly* assembly)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(assembly));
    }
    CONTRACTL_END;
    return assembly->FindDomainAssembly(this);    
};

inline BOOL AppDomain::IsRunningIn(Thread* pThread)
{
    WRAPPER_NO_CONTRACT;
    if (IsDefaultDomain()) 
        return TRUE;
    return pThread->IsRunningIn(this, NULL)!=NULL;
}



inline void AppDomain::AddMemoryPressure()
{
    STANDARD_VM_CONTRACT;
    m_MemoryPressure=EstimateSize();
    GCInterface::AddMemoryPressure(m_MemoryPressure);
}

inline void AppDomain::RemoveMemoryPressure()
{
    WRAPPER_NO_CONTRACT;

    GCInterface::RemoveMemoryPressure(m_MemoryPressure);
}

#endif // DACCESS_COMPILE

inline void AppDomain::SetAppDomainManagerInfo(LPCWSTR szAssemblyName, LPCWSTR szTypeName, EInitializeNewDomainFlags dwInitializeDomainFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    m_AppDomainManagerAssembly=szAssemblyName;
    m_AppDomainManagerType=szTypeName;
    m_dwAppDomainManagerInitializeDomainFlags = dwInitializeDomainFlags;
}

inline BOOL AppDomain::HasAppDomainManagerInfo()
{
    WRAPPER_NO_CONTRACT;
    return !m_AppDomainManagerAssembly.IsEmpty() && !m_AppDomainManagerType.IsEmpty();
}

inline LPCWSTR AppDomain::GetAppDomainManagerAsm()
{
    WRAPPER_NO_CONTRACT;
    return m_AppDomainManagerAssembly;
}


inline LPCWSTR AppDomain::GetAppDomainManagerType()
{
    WRAPPER_NO_CONTRACT;
    return m_AppDomainManagerType;
}

#ifndef FEATURE_CORECLR
inline BOOL AppDomain::AppDomainManagerSetFromConfig()
{
    WRAPPER_NO_CONTRACT;
    return m_fAppDomainManagerSetInConfig;
}
#endif // !FEATURE_CORECLR

inline EInitializeNewDomainFlags AppDomain::GetAppDomainManagerInitializeNewDomainFlags()
{
    LIMITED_METHOD_CONTRACT;
    return m_dwAppDomainManagerInitializeDomainFlags;
}

#ifdef FEATURE_CORECLR
inline AppDomain::PathIterator AppDomain::IterateNativeDllSearchDirectories()
{
    WRAPPER_NO_CONTRACT;
    PathIterator i;
    i.m_i = m_NativeDllSearchDirectories.Iterate();
    return i;
}

inline BOOL AppDomain::HasNativeDllSearchDirectories()
{
    WRAPPER_NO_CONTRACT;
    return m_NativeDllSearchDirectories.GetCount() !=0;
}

#endif // FEATURE_CORECLR

inline BOOL AppDomain::CanReversePInvokeEnter()
{
    LIMITED_METHOD_CONTRACT;
    return m_ReversePInvokeCanEnter;
}

inline void AppDomain::SetReversePInvokeCannotEnter()
{
    LIMITED_METHOD_CONTRACT;
    m_ReversePInvokeCanEnter=FALSE;
}

inline bool AppDomain::MustForceTrivialWaitOperations()
{
    LIMITED_METHOD_CONTRACT;
    return m_ForceTrivialWaitOperations;
}

inline void AppDomain::SetForceTrivialWaitOperations()
{
    LIMITED_METHOD_CONTRACT;
    m_ForceTrivialWaitOperations = true;
}

inline PTR_LoaderHeap AppDomain::GetHighFrequencyHeap()
{
    WRAPPER_NO_CONTRACT;    
    return GetLoaderAllocator()->GetHighFrequencyHeap();
}

inline PTR_LoaderHeap AppDomain::GetLowFrequencyHeap()
{
    WRAPPER_NO_CONTRACT;    
    return GetLoaderAllocator()->GetLowFrequencyHeap();
}

inline PTR_LoaderHeap AppDomain::GetStubHeap()
{
    WRAPPER_NO_CONTRACT;    
    return GetLoaderAllocator()->GetStubHeap();
}

inline PTR_LoaderAllocator AppDomain::GetLoaderAllocator()
{
    WRAPPER_NO_CONTRACT;
    return PTR_LoaderAllocator(PTR_HOST_MEMBER_TADDR(AppDomain,this,m_LoaderAllocator));
}

/* static */
inline DWORD DomainLocalModule::DynamicEntry::GetOffsetOfDataBlob() 
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(DWORD(offsetof(NormalDynamicEntry, m_pDataBlob)) == offsetof(NormalDynamicEntry, m_pDataBlob));
    return (DWORD)offsetof(NormalDynamicEntry, m_pDataBlob);
}


#endif  // _APPDOMAIN_I


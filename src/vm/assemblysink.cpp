// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  AssemblySink.cpp
**
** Purpose: Implements AssemblySink, event objects that block
**          the current thread waiting for an asynchronous load
**          of an assembly to succeed.
**
**


**
===========================================================*/

#include "common.h"
#ifdef FEATURE_FUSION
#include <stdlib.h>
#include "assemblysink.h"
#include "assemblyspec.hpp"
#include "corpriv.h"
#include "appdomain.inl"

AssemblySink::AssemblySink(AppDomain* pDomain) 
{
    WRAPPER_NO_CONTRACT;
    m_Domain=pDomain->GetId();
    m_pSpec=NULL;
    m_CheckCodebase = FALSE;
}

void AssemblySink::Reset()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_CheckCodebase = FALSE;
    FusionSink::Reset();
}

ULONG AssemblySink::Release()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_TRIGGERS);}
        MODE_ANY;
        PRECONDITION(CheckPointer(this));
    } CONTRACTL_END;
    
    
    ULONG   cRef = InterlockedDecrement(&m_cRef);
    if (!cRef) {
        Reset();
        AssemblySink* ret = this;
        // If we have a domain we keep a pool of one around. If we get an entry
        // back from the pool then we were not added to the pool and need to be deleted.
        // If we do not have a pool then we need to delete it.
        
        


        // TODO: SetupThread may throw.  What do we do with Release?
        HRESULT hr = S_OK;
        SetupThreadNoThrow(&hr);
        {
            GCX_COOP();
        
            if(m_Domain.m_dwId) {
                AppDomainFromIDHolder AD(m_Domain, TRUE);
                if (!AD.IsUnloaded())
                     ret = FastInterlockCompareExchangePointer(&(AD->m_pAsyncPool),
                                                               this,
                                                               NULL);

            }
        }

        if(ret != NULL) 
            delete this;
    }
    return (cRef);
}



STDMETHODIMP AssemblySink::OnProgress(DWORD dwNotification,
                                      HRESULT hrNotification,
                                      LPCWSTR szNotification,
                                      DWORD dwProgress,
                                      DWORD dwProgressMax,
                                      LPVOID pvBindInfo,
                                      IUnknown* punk)
{
    STATIC_CONTRACT_NOTHROW;

    HRESULT hr = S_OK;

    switch(dwNotification) {

    case ASM_NOTIFICATION_BIND_INFO:
        FusionBindInfo          *pBindInfo;

        pBindInfo = (FusionBindInfo *)pvBindInfo;

        if (pBindInfo && pBindInfo->pNamePolicy && m_pSpec) {
            pBindInfo->pNamePolicy->AddRef();
            m_pSpec->SetNameAfterPolicy(pBindInfo->pNamePolicy);
        }
        break;

    default:
        break;
    }

    if (SUCCEEDED(hr))
        hr = FusionSink::OnProgress(dwNotification, hrNotification, szNotification, 
                                    dwProgress, dwProgressMax, pvBindInfo, punk);

    return hr;
}


HRESULT AssemblySink::Wait()
{
    STATIC_CONTRACT_NOTHROW;

    HRESULT hr = FusionSink::Wait();

    if (FAILED(hr)) {
        // If we get an exception then we will just release this sink. It may be the
        // case that the appdomain was terminated. Other exceptions will cause the
        // sink to be scavenged but this is ok. A new one will be generated for the
        // next bind.
        m_Domain.m_dwId = 0;
        // The AssemblySpec passed is stack allocated in some cases.
        // Remove reference to it to prevent AV in delayed fusion bind notifications.
        m_pSpec = NULL;
    }

    return hr;
}
#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  FusionSink.cpp
**
** Purpose: Implements FusionSink, event objects that block 
**          the current thread waiting for an asynchronous load
**          of an assembly to succeed. 
**
**


===========================================================*/

#include "common.h"

#include <stdlib.h>
#include "fusionsink.h"
#include "ex.h"

STDMETHODIMP FusionSink::QueryInterface(REFIID riid, void** ppv)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    
    *ppv = NULL;

    if (riid == IID_IUnknown)
        *ppv = (IUnknown*) (IAssemblyBindSink*) this;
    else if (riid == IID_IAssemblyBindSink)   
        *ppv = (IAssemblyBindSink*)this;
    else if (riid == IID_INativeImageEvaluate)   
        *ppv = (INativeImageEvaluate*)this;
    if (*ppv == NULL)
        hr = E_NOINTERFACE;
    else
        AddRef();   

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

STDMETHODIMP FusionSink::OnProgress(DWORD dwNotification,
                                    HRESULT hrNotification,
                                    LPCWSTR szNotification,
                                    DWORD dwProgress,
                                    DWORD dwProgressMax,
                                    LPVOID pvBindInfo,
                                    IUnknown* punk)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    switch(dwNotification) {
    case ASM_NOTIFICATION_DONE:
        m_LastResult = hrNotification;
        if(m_pAbortUnk) {
            m_pAbortUnk->Release();
            m_pAbortUnk = NULL;
        }

        if(punk && SUCCEEDED(hrNotification))
            hr = punk->QueryInterface(IID_IUnknown, (void**) &m_punk);
        m_hEvent->Set();
        break;
    case ASM_NOTIFICATION_NATIVE_IMAGE_DONE:
        if(punk && SUCCEEDED(hrNotification))
            hr = punk->QueryInterface(IID_IUnknown, (void**) &m_pNIunk);
        break;
        
    case ASM_NOTIFICATION_START:
        if(punk)
            hr = punk->QueryInterface(IID_IUnknown, (void**) &m_pAbortUnk);
        break;

    case ASM_NOTIFICATION_ATTEMPT_NEXT_CODEBASE:
        break;

    case ASM_NOTIFICATION_BIND_INFO:
        FusionBindInfo          *pBindInfo;

        pBindInfo = (FusionBindInfo *)pvBindInfo;

        if (pBindInfo && m_pFusionLog == NULL) {
            m_pFusionLog = pBindInfo->pdbglog;
            if (m_pFusionLog) {
                m_pFusionLog->AddRef();
            }
        }
        break;
    default:
        break;
    }
    
    return hr;
}

ULONG FusionSink::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    ULONG cRefCount = 0;
    //BEGIN_ENTRYPOINT_VOIDRET;

    cRefCount = (InterlockedIncrement(&m_cRef));
    //END_ENTRYPOINT_VOIDRET;
    return cRefCount;
}

ULONG FusionSink::Release()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;
    BEGIN_CLEANUP_ENTRYPOINT;
    
    ULONG   cRef = InterlockedDecrement(&m_cRef);
    if (!cRef) {
        Reset();
        delete this;
    }
    END_CLEANUP_ENTRYPOINT;
    return (cRef);
}

HRESULT FusionSink::AssemblyResetEvent()
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr = AssemblyCreateEvent();
    if(FAILED(hr)) return hr;

    if(!m_hEvent->Reset()) {
        hr = HRESULT_FROM_GetLastErrorNA();
    }

    return hr;
}

HRESULT FusionSink::AssemblyCreateEvent()
{
    STATIC_CONTRACT_NOTHROW;
    HRESULT hr = S_OK;
    if(m_hEvent == NULL) {
        // Initialize the event to require manual reset
        // and to initially signaled.
        EX_TRY {
            m_hEvent = new Event();
            m_hEvent->CreateManualEvent(TRUE);
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    return hr;
}

HRESULT FusionSink::Wait()
{
    STATIC_CONTRACT_NOTHROW;

#if CHECK_INVARIANTS
    _ASSERTE(CheckPointer(this));
    _ASSERTE(CheckPointer(m_hEvent));
#endif  // CHECK_INVARIANTS

    HRESULT hr = S_OK;
    DWORD   dwReturn = 0;

    // CLREvent::Wait will switch mode if needed;

    // Waiting for a signal from fusion - which we are guaranteed to get.
    // We do a WaitForMultipleObjects (STA and MTA) and pump messages in the STA case
    // in the call so we shouldn't freeze the system.
    EX_TRY 
    {
        dwReturn = m_hEvent->Wait(INFINITE,TRUE);
    } 
    EX_CATCH 
    {
        // Fusion uses us via COM interface so we need to swallow exceptions
        hr = GET_EXCEPTION()->GetHR();

        //@todo: is it right thing to do to swallow exceptions here
    } 
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

HRESULT FusionSink::Evaluate (
        IAssembly *pILAssembly, 
        IAssembly *pNativeAssembly,
        BYTE * pbCachedData,
        DWORD dwDataSize
        )
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;

    return S_OK;
}


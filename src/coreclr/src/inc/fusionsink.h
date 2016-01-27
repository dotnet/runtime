// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/*============================================================
**
** Header:  FusionSink.hpp
**
** Purpose: Implements FusionSink
**
**
===========================================================*/
#ifndef _FUSIONSINK_H
#define _FUSIONSINK_H

#include <fusion.h>
#include <fusionpriv.h>
#include "corhlpr.h"
#include "corpriv.h"

class FusionSink : public IAssemblyBindSink, public INativeImageEvaluate
{
public:
    
    FusionSink() :
        m_punk(NULL),
        m_pNIunk(NULL),
        m_pAbortUnk(NULL),
        m_pFusionLog(NULL),
        m_cRef(1),
        m_hEvent(NULL),
        m_LastResult(S_OK)
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void Reset()
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            NOTHROW;
        }
        CONTRACTL_END;

        if(m_pAbortUnk) {
            m_pAbortUnk->Release();
            m_pAbortUnk = NULL;
        }

        if(m_punk) {
            m_punk->Release();
            m_punk = NULL;
        }

        if(m_pNIunk) {
            m_pNIunk->Release();
            m_pNIunk = NULL;
        }
        
        if(m_pFusionLog) {
            m_pFusionLog->Release();
            m_pFusionLog = NULL;
        }

        m_LastResult = S_OK;
    }

    ~FusionSink()
    {
        CONTRACTL
        {
            DESTRUCTOR_CHECK;
            NOTHROW;
        }
        CONTRACTL_END;
 
        if(m_hEvent) {
            delete m_hEvent;
            m_hEvent = NULL;
        }

        Reset();
    }

    HRESULT AssemblyResetEvent();
    HRESULT LastResult()
    {
        LIMITED_METHOD_CONTRACT;
        return m_LastResult;
    }

    STDMETHODIMP QueryInterface(REFIID riid, void **ppInterface);
    ULONG STDMETHODCALLTYPE AddRef(void); 
    ULONG STDMETHODCALLTYPE Release(void);
    
    STDMETHODIMP OnProgress(DWORD dwNotification,
                            HRESULT hrNotification,
                            LPCWSTR szNotification,
                            DWORD dwProgress,
                            DWORD dwProgressMax,
                            LPVOID pvBindInfo,
                            IUnknown* punk);

    // Wait on the event.
    virtual HRESULT Wait();

    STDMETHODIMP Evaluate(
        IAssembly *pILAssembly, 
        IAssembly *pNativeAssembly,
        BYTE * pbCachedData,
        DWORD dwDataSize);
    
    IUnknown*    m_punk;      // Getting an assembly
    IUnknown*    m_pNIunk;      // Getting an assembly    
    IUnknown*    m_pAbortUnk; // pUnk for aborting a bind
    IFusionBindLog *m_pFusionLog;

protected:
    HRESULT AssemblyCreateEvent();

    LONG        m_cRef;    // Ref count.
    Event      *m_hEvent;  // Event to block thread.
    HRESULT     m_LastResult; // Last notification result
};

#endif  // _FUSIONSINK_H

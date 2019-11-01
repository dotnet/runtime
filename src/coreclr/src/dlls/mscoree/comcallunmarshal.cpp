// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: ComCallUnmarshal.cpp
//

//
// Classes used to unmarshal all COM call wrapper IPs.
//


#include "stdafx.h"                     // Standard header.

#ifdef FEATURE_COMINTEROP

#include "ComCallUnmarshal.h"
#include <utilcode.h>                   // Utility helpers.

// For free-threaded marshaling, we must not be spoofed by out-of-process or cross-runtime marshal data.
// Only unmarshal data that comes from our own runtime.
extern BYTE         g_UnmarshalSecret[sizeof(GUID)];
extern bool         g_fInitedUnmarshalSecret;

STDMETHODIMP ComCallUnmarshal::QueryInterface(REFIID iid, void **ppv) 
{
    CONTRACTL 
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    } CONTRACTL_END;

    if (!ppv)
        return E_POINTER;

    *ppv = NULL;
    if (iid == IID_IUnknown) 
    {
        *ppv = (IUnknown *)this;
        AddRef();
    } else if (iid == IID_IMarshal) 
    {
        *ppv = (IMarshal *)this;
        AddRef();
    }
    return (*ppv != NULL) ? S_OK : E_NOINTERFACE;
}

STDMETHODIMP_(ULONG) ComCallUnmarshal::AddRef(void) 
{
    LIMITED_METHOD_CONTRACT;
    return 2; 
}

STDMETHODIMP_(ULONG) ComCallUnmarshal::Release(void) 
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

STDMETHODIMP ComCallUnmarshal::GetUnmarshalClass (REFIID riid, void * pv, ULONG dwDestContext, 
                                                  void * pvDestContext, ULONG mshlflags, 
                                                  LPCLSID pclsid) 
{
    LIMITED_METHOD_CONTRACT;
    // Marshal side only.
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

STDMETHODIMP ComCallUnmarshal::GetMarshalSizeMax (REFIID riid, void * pv, ULONG dwDestContext, 
                                                  void * pvDestContext, ULONG mshlflags, 
                                                  ULONG * pSize) 
{
    LIMITED_METHOD_CONTRACT;
    // Marshal side only.
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

STDMETHODIMP ComCallUnmarshal::MarshalInterface (LPSTREAM pStm, REFIID riid, void * pv,
                                                 ULONG dwDestContext, LPVOID pvDestContext,
                                                 ULONG mshlflags) 
{
    LIMITED_METHOD_CONTRACT;
    // Marshal side only.
    _ASSERTE(FALSE);
    return E_NOTIMPL;
}

STDMETHODIMP ComCallUnmarshal::UnmarshalInterface (LPSTREAM pStm, REFIID riid, void ** ppvObj)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        STATIC_CONTRACT_MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pStm));
        PRECONDITION(CheckPointer(ppvObj));
    } CONTRACTL_END;
                                    
    ULONG bytesRead;
    ULONG mshlflags;
    HRESULT hr = E_FAIL;

    // The marshal code added a reference to the object, but we return a
    // reference to the object as well, so don't change the ref count on the
    // success path. Need to release on error paths though (if we manage to
    // retrieve the IP, that is). If the interface was marshalled
    // TABLESTRONG or TABLEWEAK, there is going to be a ReleaseMarshalData
    // in the future, so we should AddRef the IP we're about to give out.
    // Note also that OLE32 requires us to advance the stream pointer even
    // in failure cases.

    // Read the raw IP out of the marshalling stream.
    hr = pStm->Read (ppvObj, sizeof (void *), &bytesRead);
    if (FAILED (hr) || (bytesRead != sizeof (void *)))
        IfFailGo(RPC_E_INVALID_DATA);

    // And then the marshal flags.
    hr = pStm->Read (&mshlflags, sizeof (ULONG), &bytesRead);
    if (FAILED (hr) || (bytesRead != sizeof (ULONG)))
        IfFailGo(RPC_E_INVALID_DATA);

    // And then verify our secret, to be sure that cross-runtime clients aren't
    // trying to trick us into mis-interpreting their data as a ppvObj.  Note that
    // it is guaranteed that the secret data is initialized, or else we certainly
    // haven't written it into this buffer!
    if (!g_fInitedUnmarshalSecret)
        IfFailGo(E_UNEXPECTED);

    BYTE secret[sizeof(GUID)];

    hr = pStm->Read(secret, sizeof(secret), &bytesRead);
    if (FAILED(hr) || (bytesRead != sizeof(secret)))
        IfFailGo(RPC_E_INVALID_DATA);

    if (memcmp(g_UnmarshalSecret, secret, sizeof(secret)) != 0)
        IfFailGo(E_UNEXPECTED);

    if (ppvObj && ((mshlflags == MSHLFLAGS_TABLESTRONG) || (mshlflags == MSHLFLAGS_TABLEWEAK)))
    {
        // For table access we can just QI for the correct interface (this
        // will addref the IP, but that's OK since we need to keep an extra
        // ref on the IP until ReleaseMarshalData is called).
        hr = ((IUnknown *)*ppvObj)->QueryInterface(riid, ppvObj);
    }
    else 
    {
        // For normal access we QI for the correct interface then release
        // the old IP.
        NonVMComHolder<IUnknown> pOldUnk = (IUnknown *)*ppvObj;
        hr = pOldUnk->QueryInterface(riid, ppvObj);
    }
ErrExit:
    return hr;
}

STDMETHODIMP ComCallUnmarshal::ReleaseMarshalData (LPSTREAM pStm) 
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        STATIC_CONTRACT_MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pStm));
    } CONTRACTL_END;
    
    IUnknown *pUnk;
    ULONG bytesRead;
    ULONG mshlflags;
    HRESULT hr = S_OK;	

    if (!pStm)
        return E_POINTER;

    // Read the raw IP out of the marshalling stream. Do this first since we
    // need to update the stream pointer even in case of failures.
    hr = pStm->Read (&pUnk, sizeof (pUnk), &bytesRead);
    if (FAILED (hr) || (bytesRead != sizeof (pUnk)))
        IfFailGo(RPC_E_INVALID_DATA);

    // Now read the marshal flags.
    hr = pStm->Read (&mshlflags, sizeof (mshlflags), &bytesRead);
    if (FAILED (hr) || (bytesRead != sizeof (mshlflags)))
        IfFailGo(RPC_E_INVALID_DATA);

    if (!g_fInitedUnmarshalSecret)
    {
        IfFailGo(E_UNEXPECTED);        
    }

    BYTE secret[sizeof(GUID)];

    hr = pStm->Read(secret, sizeof(secret), &bytesRead);
    if (FAILED(hr) || (bytesRead != sizeof(secret)))
        IfFailGo(RPC_E_INVALID_DATA);

    if (memcmp(g_UnmarshalSecret, secret, sizeof(secret)) != 0)
        IfFailGo(E_UNEXPECTED);

    pUnk->Release ();

ErrExit:
    return hr;
}

STDMETHODIMP ComCallUnmarshal::DisconnectObject (ULONG dwReserved) 
{
    LIMITED_METHOD_CONTRACT;

    // Nothing we can (or need to) do here. The client is using a raw IP to
    // access this server, so the server shouldn't go away until the client
    // Release()'s it.

    return S_OK;
}

CComCallUnmarshalFactory::CComCallUnmarshalFactory() 
{
    WRAPPER_NO_CONTRACT;
}

STDMETHODIMP CComCallUnmarshalFactory::QueryInterface(REFIID iid, void **ppv) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;
    
    if (!ppv)
        return E_POINTER;

    *ppv = NULL;
    if (iid == IID_IClassFactory || iid == IID_IUnknown) {
        *ppv = (IClassFactory *)this;
        AddRef();
    }
    return (*ppv != NULL) ? S_OK : E_NOINTERFACE;
}

STDMETHODIMP_(ULONG) CComCallUnmarshalFactory::AddRef(void) 
{
    LIMITED_METHOD_CONTRACT;
    return 2; 
}

STDMETHODIMP_(ULONG) CComCallUnmarshalFactory::Release(void) 
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

STDMETHODIMP CComCallUnmarshalFactory::CreateInstance(LPUNKNOWN punkOuter, REFIID iid, LPVOID FAR *ppv) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(ppv));
    } CONTRACTL_END;

    if (!ppv)
        return E_POINTER;

    *ppv = NULL;

    if (punkOuter != NULL)
        return CLASS_E_NOAGGREGATION;

    return m_Unmarshaller.QueryInterface(iid, ppv);
}

STDMETHODIMP CComCallUnmarshalFactory::LockServer(BOOL fLock) 
{
    LIMITED_METHOD_CONTRACT;
    return S_OK;
}

#endif // FEATURE_COMINTEROP

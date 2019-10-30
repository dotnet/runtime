// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: ComCallUnmarshal.h
//

//
// Classes used to unmarshal all COM call wrapper IPs.
//


#pragma once

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

// Class used to unmarshal all COM call wrapper IPs. In order for this to work in side-by-side
// scenarios, the CLSID of this class has to be changed with every side-by-side release.
//
// The class is identified by the following CLSID:
// CLR v1.0, v1.1, v2.0: 3F281000-E95A-11d2-886B-00C04F869F04
// CLR v4.0:             45FB4600-E6E8-4928-B25E-50476FF79425
//
class ComCallUnmarshal : public IMarshal
{
public:

    // *** IUnknown methods ***
    STDMETHODIMP QueryInterface(REFIID iid, void **ppv); 
    STDMETHODIMP_(ULONG) AddRef(void); 
    STDMETHODIMP_(ULONG) Release(void); 

    // *** IMarshal methods ***
    STDMETHODIMP GetUnmarshalClass (REFIID riid, void * pv, ULONG dwDestContext, 
                                    void * pvDestContext, ULONG mshlflags, 
                                    LPCLSID pclsid);

    STDMETHODIMP GetMarshalSizeMax (REFIID riid, void * pv, ULONG dwDestContext, 
                                    void * pvDestContext, ULONG mshlflags, 
                                    ULONG * pSize);

    STDMETHODIMP MarshalInterface (LPSTREAM pStm, REFIID riid, void * pv,
                                   ULONG dwDestContext, LPVOID pvDestContext,
                                   ULONG mshlflags);

    STDMETHODIMP UnmarshalInterface (LPSTREAM pStm, REFIID riid, void ** ppvObj);
    STDMETHODIMP ReleaseMarshalData (LPSTREAM pStm);
    STDMETHODIMP DisconnectObject (ULONG dwReserved);
};

// Class factory for the COM call wrapper unmarshaller.
class CComCallUnmarshalFactory : public IClassFactory
{
    ComCallUnmarshal    m_Unmarshaller;

  public:

    CComCallUnmarshalFactory();

    // *** IUnknown methods ***
	STDMETHODIMP QueryInterface(REFIID iid, void **ppv);
	STDMETHODIMP_(ULONG) AddRef(void);
	STDMETHODIMP_(ULONG) Release(void); 

    // *** IClassFactory methods ***
    STDMETHODIMP CreateInstance(LPUNKNOWN punkOuter, REFIID iid, LPVOID FAR *ppv);
    STDMETHODIMP LockServer(BOOL fLock);
};

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ildbsymbols.cpp
//

// ===========================================================================

#include "pch.h"

#include "classfactory.h"

// GUID identifying the ILDB format version.
extern "C" const GUID ILDB_VERSION_GUID = {0x9e02e5b6, 0x8aef, 0x4d06, { 0x82, 0xe8, 0xe, 0x9b, 0x45, 0x49, 0x97, 0x16} };

// Version used for the "first source release", no longer supported.
extern "C" const GUID ILDB_VERSION_GUID_FSR = {0xCB2F6723, 0xAB3A, 0x11d, { 0x9C, 0x40, 0x00, 0xC0, 0x4F, 0xA3, 0x0A, 0x3E} };

// This map contains the list of coclasses which are exported from this module.
const COCLASS_REGISTER g_CoClasses[] =
{
//  pClsid                      szProgID            pfnCreateObject
    { &CLSID_CorSymReader_SxS,    W("CorSymReader"),    SymReader::NewSymReader},
    { &CLSID_CorSymWriter_SxS,    W("CorSymWriter"),    SymWriter::NewSymWriter},
    { &CLSID_CorSymBinder_SxS,    W("CorSymBinder"),    SymBinder::NewSymBinder},
    { NULL,                       NULL,               NULL }
};

STDAPI IldbSymbolsGetClassObject(REFCLSID rclsid, REFIID riid, void** ppvObject)
{
    CIldbClassFactory *pClassFactory;      // To create class factory object.
    const COCLASS_REGISTER *pCoClass;   // Loop control.
    HRESULT     hr = CLASS_E_CLASSNOTAVAILABLE;

    _ASSERTE(IsValidCLSID(rclsid));
    _ASSERTE(IsValidIID(riid));
    _ASSERTE(IsValidWritePtr(ppvObject, void*));

    if (ppvObject)
    {
        *ppvObject = NULL;

        // Scan for the right one.
        for (pCoClass=g_CoClasses;  pCoClass->pClsid;  pCoClass++)
        {
            if (*pCoClass->pClsid == rclsid)
            {
                // Allocate the new factory object.
                pClassFactory = NEW(CIldbClassFactory(pCoClass));
                if (!pClassFactory)
                    return (E_OUTOFMEMORY);

                // Pick the v-table based on the caller's request.
                hr = pClassFactory->QueryInterface(riid, ppvObject);

                // Always release the local reference, if QI failed it will be
                // the only one and the object gets freed.
                pClassFactory->Release();
                break;
            }
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    return hr;
}

/* ------------------------------------------------------------------------- *
 * CIldbClassFactory class
 * ------------------------------------------------------------------------- */

//*****************************************************************************
// QueryInterface is called to pick a v-table on the co-class.
//*****************************************************************************
HRESULT STDMETHODCALLTYPE CIldbClassFactory::QueryInterface(
    REFIID      riid,
    void        **ppvObject)
{
    HRESULT     hr;

    if (ppvObject == NULL)
    {
        return E_INVALIDARG;
    }

    // Avoid confusion.
    *ppvObject = NULL;

    // Pick the right v-table based on the IID passed in.
    if (riid == IID_IUnknown)
        *ppvObject = (IUnknown *) this;
    else if (riid == IID_IClassFactory)
        *ppvObject = (IClassFactory *) this;

    // If successful, add a reference for out pointer and return.
    if (*ppvObject)
    {
        hr = S_OK;
        AddRef();
    }
    else
        hr = E_NOINTERFACE;
    return (hr);
}


//*****************************************************************************
// CreateInstance is called to create a new instance of the coclass for which
// this class was created in the first place.  The returned pointer is the
// v-table matching the IID if there.
//*****************************************************************************
HRESULT STDMETHODCALLTYPE CIldbClassFactory::CreateInstance(
    IUnknown    *pUnkOuter,
    REFIID      riid,
    void        **ppvObject)
{
    HRESULT     hr;

    _ASSERTE(IsValidIID(riid));
    _ASSERTE(IsValidWritePtr(ppvObject, void*));

    // Avoid confusion.
    *ppvObject = NULL;
    _ASSERTE(m_pCoClass);

    // Aggregation is not supported by these objects.
    if (pUnkOuter)
        return (CLASS_E_NOAGGREGATION);

    // Ask the object to create an instance of itself, and check the iid.
    hr = (*m_pCoClass->pfnCreateObject)(riid, ppvObject);
    return (hr);
}

// Version of CreateInstance called directly from clients
STDAPI IldbSymbolsCreateInstance(REFCLSID rclsid, REFIID riid, void** ppvIUnknown)
{
    IClassFactory *pClassFactory = NULL;
    HRESULT hr = IldbSymbolsGetClassObject(rclsid, IID_IClassFactory, (void**)&pClassFactory);
    if (SUCCEEDED(hr))
        hr = pClassFactory->CreateInstance(NULL, riid, ppvIUnknown);
    if (pClassFactory)
        pClassFactory->Release();
    return hr;
}

HRESULT STDMETHODCALLTYPE CIldbClassFactory::LockServer(
    BOOL        fLock)
{
    return (S_OK);
}

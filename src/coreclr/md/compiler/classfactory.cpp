// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// ClassFactory.cpp
//

//
// Dll* routines for entry points, and support for COM framework.  The class
// factory and other routines live in this module.
//
// This file is not included in the standalone metadata version, because standalone can't use COM,
// let alone COM-activation. So this file gets linked into mscorwks.dll, and then the mscorwks
// class factory delegates to this co-creation routine.
//
//*****************************************************************************
#include "stdafx.h"

#ifdef FEATURE_METADATA_IN_VM

#include "classfactory.h"
#include "disp.h"
#include "regmeta.h"
#include "mscoree.h"
#include "corhost.h"

// This map contains the list of coclasses which are exported from this module.
// NOTE:  CLSID_CorMetaDataDispenser must be the first entry in this table!
const COCLASS_REGISTER g_CoClasses[] =
{
//  pClsid                              szProgID                        pfnCreateObject
    { &CLSID_CorMetaDataDispenser,        W("CorMetaDataDispenser"),        Disp::CreateObject },
    { NULL,                               NULL,                           NULL }
};


//*****************************************************************************
// Called by COM to get a class factory for a given CLSID.  If it is one we
// support, instantiate a class factory object and prepare for create instance.
//
// Notes:
//   This gets invoked from mscorwks's DllGetClassObject.
//*****************************************************************************
STDAPI MetaDataDllGetClassObject(       // Return code.
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv)                   // Return interface pointer here.
{
    MDClassFactory *pClassFactory;      // To create class factory object.
    const COCLASS_REGISTER *pCoClass;   // Loop control.
    HRESULT     hr = CLASS_E_CLASSNOTAVAILABLE;

    // Scan for the right one.
    for (pCoClass=g_CoClasses;  pCoClass->pClsid;  pCoClass++)
    {
        if (*pCoClass->pClsid == rclsid)
        {
            // Allocate the new factory object.
            pClassFactory = new (nothrow) MDClassFactory(pCoClass);
            if (!pClassFactory)
                return (E_OUTOFMEMORY);

            // Pick the v-table based on the caller's request.
            hr = pClassFactory->QueryInterface(riid, ppv);

            // Always release the local reference, if QI failed it will be
            // the only one and the object gets freed.
            pClassFactory->Release();
            break;
        }
    }
    return hr;
}


//*****************************************************************************
//
//********** Class factory code.
//
//*****************************************************************************


//*****************************************************************************
// QueryInterface is called to pick a v-table on the co-class.
//*****************************************************************************
HRESULT STDMETHODCALLTYPE MDClassFactory::QueryInterface(
    REFIID      riid,
    void        **ppvObject)
{
    HRESULT     hr;

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
    return hr;
}


//*****************************************************************************
// CreateInstance is called to create a new instance of the coclass for which
// this class was created in the first place.  The returned pointer is the
// v-table matching the IID if there.
//*****************************************************************************
HRESULT STDMETHODCALLTYPE MDClassFactory::CreateInstance(
    IUnknown    *pUnkOuter,
    REFIID      riid,
    void        **ppvObject)
{
    HRESULT     hr;

    BEGIN_ENTRYPOINT_NOTHROW;


    // Avoid confusion.
    *ppvObject = NULL;
    _ASSERTE(m_pCoClass);

    // Aggregation is not supported by these objects.
    if (pUnkOuter)
        IfFailGo(CLASS_E_NOAGGREGATION);

    // Ask the object to create an instance of itself, and check the iid.
    hr = (*m_pCoClass->pfnCreateObject)(riid, ppvObject);

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT STDMETHODCALLTYPE
MDClassFactory::LockServer(
    BOOL fLock)
{
    // @FUTURE: Should we return E_NOTIMPL instead of S_OK?
    return S_OK;
}

#endif //FEATURE_METADATA_IN_VM

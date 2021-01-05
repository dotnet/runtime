// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//
// InternalUnknownImpl.h
//
// Defines utility class IUnknownCommon, which provides default
// implementations for IUnknown's AddRef, Release, and QueryInterface methods.
//
// Use: a class that implements one or more interfaces should derive from
// IUnknownCommon with a template parameter list consisting of the
// list of implemented interfaces and their IIDs.
//
// Example:
//   class MyInterfacesImpl :
//     public IUnknownCommon<MyInterface, IID_MyInterface>
//   { ... };
//
// IUnknownCommon will provide base AddRef and Release semantics, and will
// also provide an implementation of QueryInterface that will evaluate the
// arguments against the set of supported interfaces and return the
// appropriate result.
//

//
//*****************************************************************************

#ifndef __InternalUnknownImpl_h__
#define __InternalUnknownImpl_h__

#include <winnt.h>
#include "winwrap.h"
#include "contract.h"
#include "ex.h"
#include "volatile.h"
#include "debugmacros.h"

template <class T, REFIID IID_T>
class IUnknownCommon : public T
{
protected:
    LONG m_cRef;

public:
    IUnknownCommon()
        : m_cRef(0)
    {
    }

    // Add a virtual destructor to force derived types to also have virtual destructors.
    virtual ~IUnknownCommon()
    {
    }

    STDMETHOD_(ULONG, AddRef())
    {
        return InterlockedIncrement(&m_cRef);
    }

    STDMETHOD_(ULONG, Release())
    {
        _ASSERTE(m_cRef > 0);

        ULONG cRef = InterlockedDecrement(&m_cRef);

        if (cRef == 0)
            delete this; // Relies on virtual dtor to work properly.

        return cRef;
    }

    STDMETHOD(QueryInterface(REFIID riid, void** ppObj))
    {
        if (ppObj == NULL)
            return E_INVALIDARG;

        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_T))
        {
            AddRef();
            *ppObj = static_cast<T*>(this);
            return S_OK;
        }

        *ppObj = NULL;
        return E_NOINTERFACE;
    }
};

template <class T, REFIID IID_T, class T2, REFIID IID_T2>
class IUnknownCommon2 : public T, public T2
{
protected:
    LONG m_cRef;

public:
    IUnknownCommon2()
        : m_cRef(0)
    {
    }

    // Add a virtual destructor to force derived types to also have virtual destructors.
    virtual ~IUnknownCommon2()
    {
    }

    STDMETHOD_(ULONG, AddRef())
    {
        return InterlockedIncrement(&m_cRef);
    }

    STDMETHOD_(ULONG, Release())
    {
        _ASSERTE(m_cRef > 0);

        ULONG cRef = InterlockedDecrement(&m_cRef);

        if (cRef == 0)
            delete this; // Relies on virtual dtor to work properly.

        return cRef;
    }

    STDMETHOD(QueryInterface(REFIID riid, void** ppObj))
    {
        if (ppObj == NULL)
            return E_INVALIDARG;

        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_T))
        {
            AddRef();
            *ppObj = static_cast<T*>(this);
            return S_OK;
        }
        else if (IsEqualIID(riid, IID_T2))
        {
            AddRef();
            *ppObj = static_cast<T2*>(this);
            return S_OK;
        }

        *ppObj = NULL;
        return E_NOINTERFACE;
    }
};

#endif // __InternalUnknownImpl_h__

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ComConnectionPoints.h
//

// ===========================================================================
// Declaration of the classes used to expose connection points to COM.
// ===========================================================================


#pragma once

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "vars.hpp"
#include "comcallablewrapper.h"
#include "comdelegate.h"

//------------------------------------------------------------------------------------------
//      Definition of helper class used to expose connection points
//------------------------------------------------------------------------------------------

// Structure containing information regarding the methods that make up an event.
struct EventMethodInfo
{
    MethodDesc*     m_pEventMethod;
    MethodDesc*     m_pAddMethod;
    MethodDesc*     m_pRemoveMethod;
};


// Structure passed out as a cookie when Advise is called.
struct ConnectionCookie
{
    ConnectionCookie(OBJECTHANDLE hndEventProvObj) : m_hndEventProvObj(hndEventProvObj)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(NULL != hndEventProvObj);
        }
        CONTRACTL_END;
    }

    ~ConnectionCookie()
    {
        WRAPPER_NO_CONTRACT;
        DestroyHandle(m_hndEventProvObj);
    }

    // Currently called only from Cooperative mode.
    static ConnectionCookie* CreateConnectionCookie(OBJECTHANDLE hndEventProvObj)
    {
        CONTRACT (ConnectionCookie*)
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM());
            PRECONDITION(NULL != hndEventProvObj);
        }
        CONTRACT_END;

        RETURN (new ConnectionCookie(hndEventProvObj));
    }

    SLink           m_Link;
    OBJECTHANDLE    m_hndEventProvObj;
    DWORD           m_id;
};

FORCEINLINE void ConnectionCookieRelease(ConnectionCookie* p)
{
    WRAPPER_NO_CONTRACT;

    delete p;
}

// Connection cookie holder used to ensure the cookies are deleted when required.
class ConnectionCookieHolder : public Wrapper<ConnectionCookie*, ConnectionCookieDoNothing, ConnectionCookieRelease, NULL>
{
public:
    ConnectionCookieHolder(ConnectionCookie* p = NULL)
        : Wrapper<ConnectionCookie*, ConnectionCookieDoNothing, ConnectionCookieRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(ConnectionCookie* p)
    {
        WRAPPER_NO_CONTRACT;
        Wrapper<ConnectionCookie*, ConnectionCookieDoNothing, ConnectionCookieRelease, NULL>::operator=(p);
    }
};

// List of connection cookies.
typedef SList<ConnectionCookie, true> CONNECTIONCOOKIELIST;

// ConnectionPoint class. This class implements IConnectionPoint and does the mapping
// from a CP handler to a TCE provider.
class ConnectionPoint : public IConnectionPoint
{
public:
    // Encapsulate CrstHolder, so that clients of our lock don't have to know the
    // details of its implementation.
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(ConnectionPoint *pCP) : CrstHolder(&pCP->m_Lock)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    ConnectionPoint( ComCallWrapper *pWrap, MethodTable *pEventMT );
    ~ConnectionPoint();

    HRESULT __stdcall QueryInterface(REFIID riid, void** ppv);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();

    HRESULT __stdcall GetConnectionInterface( IID *pIID );
    HRESULT __stdcall GetConnectionPointContainer( IConnectionPointContainer **ppCPC );
    HRESULT __stdcall Advise( IUnknown *pUnk, DWORD *pdwCookie );
    HRESULT __stdcall Unadvise( DWORD dwCookie );
    HRESULT __stdcall EnumConnections( IEnumConnections **ppEnum );

    REFIID GetIID()
    {
        LIMITED_METHOD_CONTRACT;
        return m_rConnectionIID;
    }

    CONNECTIONCOOKIELIST *GetCookieList()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_ConnectionList;
    }

private:
    // Structures used for the AD callback wrappers.
    struct GetConnectionPointContainer_Args
    {
        ConnectionPoint *pThis;
        IConnectionPointContainer **ppCPC;
    };

    struct Advise_Args
    {
        ConnectionPoint *pThis;
        IUnknown *pUnk;
        DWORD *pdwCookie;
    };

    struct Unadvise_Args
    {
        ConnectionPoint *pThis;
        DWORD dwCookie;
    };

    // Worker methods.
    void AdviseWorker(IUnknown *pUnk, DWORD *pdwCookie);
    void UnadviseWorker( DWORD dwCookie );
    IConnectionPointContainer *GetConnectionPointContainerWorker();

    // Helper methods.
    void SetupEventMethods();
    MethodDesc *FindProviderMethodDesc( MethodDesc *pEventMethodDesc, EnumEventMethods MethodType );
    void InvokeProviderMethod( OBJECTREF pProvider, OBJECTREF pSubscriber, MethodDesc *pProvMethodDesc, MethodDesc *pEventMethodDesc );
    void InsertWithLock(ConnectionCookie* pConCookie);
    void FindAndRemoveWithLock(ConnectionCookie* pConCookie);
    ConnectionCookie* FindWithLock(DWORD idOfCookie);

    ComCallWrapper*                 m_pOwnerWrap;
    GUID                            m_rConnectionIID;
    MethodTable*                    m_pTCEProviderMT;
    MethodTable*                    m_pEventItfMT;
    Crst                            m_Lock;
    CONNECTIONCOOKIELIST            m_ConnectionList;
    EventMethodInfo*                m_apEventMethods;
    int                             m_NumEventMethods;
    ULONG                           m_cbRefCount;
    ConnectionCookie*               m_pLastInserted;

    const static DWORD      idUpperLimit        = 0xFFFFFFFF;
};

// Enumeration of connection points.
class ConnectionPointEnum : IEnumConnectionPoints
{
public:
    // Encapsulate CrstHolder, so that clients of our lock don't have to know the
    // details of its implementation.
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(ConnectionPointEnum *pCP) : CrstHolder(&pCP->m_Lock)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    ConnectionPointEnum(ComCallWrapper *pOwnerWrap, CQuickArray<ConnectionPoint*> *pCPList);
    ~ConnectionPointEnum();

    HRESULT __stdcall QueryInterface(REFIID riid, void** ppv);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();

    HRESULT __stdcall Next(ULONG cConnections, IConnectionPoint **ppCP, ULONG *pcFetched);
    HRESULT __stdcall Skip(ULONG cConnections);
    HRESULT __stdcall Reset();
    HRESULT __stdcall Clone(IEnumConnectionPoints **ppEnum);

private:
    ComCallWrapper*                 m_pOwnerWrap;
    CQuickArray<ConnectionPoint*>*  m_pCPList;
    UINT                            m_CurrPos;
    ULONG                           m_cbRefCount;
    Crst                            m_Lock;
};

// Enumeration of connections.
class ConnectionEnum : IEnumConnections
{
public:
    ConnectionEnum(ConnectionPoint *pConnectionPoint);
    ~ConnectionEnum();

    HRESULT __stdcall QueryInterface(REFIID riid, void** ppv);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();

    HRESULT __stdcall Next(ULONG cConnections, CONNECTDATA* rgcd, ULONG *pcFetched);
    HRESULT __stdcall Skip(ULONG cConnections);
    HRESULT __stdcall Reset();
    HRESULT __stdcall Clone(IEnumConnections **ppEnum);

private:
    ConnectionPoint*                m_pConnectionPoint;
    ConnectionCookie*               m_CurrCookie;
    ULONG                           m_cbRefCount;
};

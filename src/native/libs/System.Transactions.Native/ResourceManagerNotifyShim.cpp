//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "StdAfx.h"
#include "notificationshimfactory.h"
#include "enlistmentnotifyshim.h"
#include "transactionshim.h"
#include "resourcemanagernotifyshim.h"

// {27C73B91-99F5-46d5-A247-732A1A16529E}
EXTERN_C const GUID IID_IResourceManagerShim \
            = { 0x27C73B91, 0x99F5, 0x46d5, { 0xA2, 0x47, 0x73, 0x2A, 0x1A, 0x16, 0x52, 0x9E } };

HRESULT __stdcall ResourceManagerNotifyShim::QueryInterface(
    REFIID      i_iid, 
    LPVOID FAR* o_ppv
    )
{
    if (o_ppv == NULL)
    {
        return E_INVALIDARG;
    }
    
    // NULL the interface pointer:
    *o_ppv  = NULL;

    // Check i_iid against each interface supported:
    if (i_iid == IID_IUnknown)
    {
        *o_ppv = reinterpret_cast<IUnknown*> (this);
    }
    else if (i_iid == IID_IResourceManagerSink)
    {
        *o_ppv = (IResourceManagerSink *) this ;
    }
    else
    {
        return E_NOINTERFACE;
    }
    
    // AddRef the interface pointer:
    ((IUnknown *) *o_ppv)->AddRef ();

    return S_OK;
}

ULONG __stdcall ResourceManagerNotifyShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}

ULONG __stdcall ResourceManagerNotifyShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall ResourceManagerNotifyShim::TMDown()
{
    this->notificationType = ResourceManagerTMDownNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

ResourceManagerShim::~ResourceManagerShim(void)
{
    SafeReleaseInterface( (IUnknown**) &this->pResourceManager );
    SafeReleaseInterface( (IUnknown**) &this->pResourceManagerNotifyShim );
    SafeReleaseInterface( (IUnknown**) &this->shimFactory );
    SafeReleaseInterface( (IUnknown**) &this->pMarshaler );
}

HRESULT __stdcall ResourceManagerShim::QueryInterface(
    REFIID      i_iid, 
    LPVOID FAR* o_ppv
    )
{
    if (o_ppv == NULL)
    {
        return E_INVALIDARG;
    }
    
    // NULL the interface pointer:
    *o_ppv  = NULL;

    // Check i_iid against each interface supported:
    if (i_iid == IID_IUnknown)
    {
        *o_ppv = reinterpret_cast<IUnknown*> (this);
    }
    else if (i_iid == IID_IResourceManagerShim)
    {
        *o_ppv = (IResourceManagerShim *) this ;
    }
    else if (i_iid == IID_IMarshal)
    {
        return (this->pMarshaler->QueryInterface(i_iid, o_ppv));
    }
    else
    {
        return E_NOINTERFACE;
    }
    
    // AddRef the interface pointer:
    ((IUnknown *) *o_ppv)->AddRef ();

    return S_OK;
}

ULONG __stdcall ResourceManagerShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}

ULONG __stdcall ResourceManagerShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall ResourceManagerShim::Enlist(
    ITransactionShim* pTransactionShim,
    void* managedIdentifier,
    IEnlistmentShim** ppEnlistmentShim
    )
{
    HRESULT hr = S_OK;
    EnlistmentNotifyShim* pEnlistmentNotifyShim = NULL;
    IUnknown* myNotifyShimRef = NULL;
    EnlistmentShim* pEnlistmentShim = NULL;
    ITransactionEnlistmentAsync* pEnlistmentAsync = NULL;
    XACTUOW txUow;
    LONG isoLevel = 0;
    ITransaction* pTransaction = NULL;


    pEnlistmentNotifyShim = new EnlistmentNotifyShim(
        this->shimFactory,
        managedIdentifier
        );
    if ( NULL == pEnlistmentNotifyShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = pEnlistmentNotifyShim->QueryInterface(
        IID_IUnknown,
        (void**) &myNotifyShimRef
        );
    if ( FAILED( hr ) )
    {
        delete pEnlistmentNotifyShim;
        pEnlistmentNotifyShim = NULL;
        myNotifyShimRef = NULL;
        goto ErrorExit;
    }

#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    pEnlistmentShim = new EnlistmentShim(
        this->shimFactory,
        pEnlistmentNotifyShim
        );
#pragma warning( pop )
    if ( NULL == pEnlistmentShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = pEnlistmentShim->Initialize();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pTransactionShim->GetTransaction( &pTransaction );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = this->pResourceManager->Enlist(
        pTransaction,
        pEnlistmentNotifyShim,
        &txUow,
        &isoLevel,
        &pEnlistmentAsync
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    pEnlistmentNotifyShim->SetEnlistmentAsync( pEnlistmentAsync );
    pEnlistmentShim->SetEnlistmentAsync( pEnlistmentAsync );

    hr = pEnlistmentShim->QueryInterface(
        IID_IEnlistmentShim,
        (void**) ppEnlistmentShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

ErrorExit:

    SafeReleaseInterface( (IUnknown**) &pEnlistmentAsync );
    SafeReleaseInterface( (IUnknown**) &myNotifyShimRef );

    if ( FAILED( hr ) )
    {
        if ( NULL != pEnlistmentShim )
        {
            delete pEnlistmentShim;
            pEnlistmentShim = NULL;
        }
    }


    return hr;
}

HRESULT __stdcall ResourceManagerShim::Reenlist(
    ULONG prepareInfoSize,
    BYTE* prepareInfo,
    TransactionOutcome* pOutcome
    )
{
    HRESULT hr = S_OK;
    XACTSTAT xactStatus = XACTSTAT_ABORTED;

    // Call Reenlist on the proxy, waiting for 5 milliseconds for it to get the outcome.  If it doesn't know that outcome in that
    // amount of time, tell the caller we don't know the outcome yet.  The managed code will reschedule the check by using the
    // ReenlistThread.
    hr = this->pResourceManager->Reenlist(
        prepareInfo,
        prepareInfoSize,
        5,
        &xactStatus
        );
    if ( S_OK == hr )
    {
        switch ( xactStatus )
        {
            case XACTSTAT_ABORTED :
            {
                *pOutcome = Aborted;
                break;
            }
            case XACTSTAT_COMMITTED :
            {
                *pOutcome = Committed;
                break;
            }
            default :
            {
                *pOutcome = Aborted;
                break;
            }
        }
    }
    else if ( XACT_E_REENLISTTIMEOUT == hr )
    {
        *pOutcome = NotKnownYet;
        hr = S_OK;
    }

    return hr;
}

HRESULT __stdcall ResourceManagerShim::ReenlistComplete()
{
    HRESULT hr = S_OK;

    hr = this->pResourceManager->ReenlistmentComplete();

    return hr;
}


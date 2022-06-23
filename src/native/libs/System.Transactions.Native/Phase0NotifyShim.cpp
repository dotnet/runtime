//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "StdAfx.h"
#include "notificationshimfactory.h"
#include "phase0notifyshim.h"

// {55FF6514-948A-4307-A692-73B84E2AF53E}
EXTERN_C const GUID IID_IPhase0EnlistmentShim \
            = { 0x55FF6514, 0x948A, 0x4307, { 0xA6, 0x92, 0x73, 0xB8, 0x4E, 0x2A, 0xF5, 0x3E } };


HRESULT __stdcall Phase0NotifyShim::QueryInterface(
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
    else if (i_iid == IID_ITransactionPhase0EnlistmentAsync)
    {
        *o_ppv = (ITransactionPhase0EnlistmentAsync *) this ;
    }
    else
    {
        return E_NOINTERFACE;
    }
    
    // AddRef the interface pointer:
    ((IUnknown *) *o_ppv)->AddRef ();

    return S_OK;
}


ULONG __stdcall Phase0NotifyShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall Phase0NotifyShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}


HRESULT __stdcall Phase0NotifyShim::Phase0Request(
    BOOL abortingHint
    )
{
    this->abortingHint = abortingHint;
    this->notificationType = Phase0RequestNotify;
    this->shimFactory->NewNotification( this );

    return S_OK;
}


HRESULT __stdcall Phase0NotifyShim::EnlistCompleted(
    HRESULT status
    )
{
    // We don't care about these.  The managed code waited for the
    // enlistment to be completed.
    return S_OK;
}

Phase0Shim::~Phase0Shim(void)
{
    SafeReleaseInterface( (IUnknown**) &this->pPhase0EnlistmentAsync );
    SafeReleaseInterface( (IUnknown**) &this->phase0NotifyShim );
    SafeReleaseInterface( (IUnknown**) &this->shimFactory );
    SafeReleaseInterface( (IUnknown**) &this->pMarshaler );
}

HRESULT __stdcall Phase0Shim::QueryInterface(
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
    else if (i_iid == IID_IPhase0EnlistmentShim)
    {
        *o_ppv = (IPhase0EnlistmentShim *) this ;
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


ULONG __stdcall Phase0Shim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall Phase0Shim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall Phase0Shim::Unenlist()
{
    HRESULT hr = S_OK;

    // VSWhidbey 405624 - There is a race between the enlistment and abort of a transaction
    // that could cause out proxy interface to already be released when Unenlist is called.
    if ( NULL != this->pPhase0EnlistmentAsync )
    {
        hr = this->pPhase0EnlistmentAsync->Unenlist();
    }
    SafeReleaseInterface( (IUnknown**) &this->pPhase0EnlistmentAsync );

    return hr;
}

HRESULT __stdcall Phase0Shim::Phase0Done(
    BOOL voteYes )
{
    HRESULT hr = S_OK;

    if ( voteYes )
    {
        hr = this->pPhase0EnlistmentAsync->Phase0Done();
        // Deal with the proxy bug where we get a Phase0Request( false ) on a
        // TMDown and the proxy object state is not changed.
        if ( XACT_E_PROTOCOL == hr )
        {
            hr = S_OK;
        }
    }
    else
    {
        SafeReleaseInterface( (IUnknown**) &this->pPhase0EnlistmentAsync );
    }

    return hr;
}




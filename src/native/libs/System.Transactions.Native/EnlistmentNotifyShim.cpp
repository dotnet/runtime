//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "StdAfx.h"
#include "notificationshimfactory.h"
#include "enlistmentnotifyshim.h"

// {5EC35E09-B285-422c-83F5-1372384A42CC}
EXTERN_C const GUID IID_IEnlistmentShim \
            = { 0x5EC35E09, 0xB285, 0x422c, { 0x83, 0xF5, 0x13, 0x72, 0x38, 0x4A, 0x42, 0xCC } };

HRESULT __stdcall EnlistmentNotifyShim::QueryInterface(
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
    else if (i_iid == IID_ITransactionResourceAsync)
    {
        *o_ppv = (ITransactionResourceAsync *) this ;
    }
    else
    {
        return E_NOINTERFACE;
    }
    
    // AddRef the interface pointer:
    ((IUnknown *) *o_ppv)->AddRef ();

    return S_OK;
}

ULONG __stdcall EnlistmentNotifyShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall EnlistmentNotifyShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall EnlistmentNotifyShim::PrepareRequest(
    BOOL fRetaining, 
    DWORD grfRM, 
    BOOL fWantMoniker,
    BOOL fSinglePhase
    )
{
    HRESULT hr = S_OK;
    IPrepareInfo* pPrepareInfo = NULL;
    BYTE* prepareInfoBuffer = NULL;
    ULONG prepareInfoLength = 0;
    ITransactionEnlistmentAsync* pEnlistmentAsync = NULL;

#if defined(_X86_)
    pEnlistmentAsync = (ITransactionEnlistmentAsync*)InterlockedExchange((LONG volatile*)&this->pEnlistmentAsync, NULL);
#elif defined(_WIN64)
    pEnlistmentAsync = (ITransactionEnlistmentAsync*)InterlockedExchange64((LONGLONG volatile*)&this->pEnlistmentAsync, NULL);
#endif

    if( pEnlistmentAsync == NULL )
    {
        return E_UNEXPECTED;
    }

    hr = pEnlistmentAsync->QueryInterface(
        IID_IPrepareInfo,
        (void**) &pPrepareInfo
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pPrepareInfo->GetPrepareInfoSize( &prepareInfoLength );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // This buffer will be freed by Managed code through the CoTaskMemHandle object that is
    // created when the pointer to this buffer is returned from GetNotification.
    prepareInfoBuffer = (BYTE*) CoTaskMemAlloc( prepareInfoLength );

    hr = pPrepareInfo->GetPrepareInfo( prepareInfoBuffer );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    this->prepareInfoSize = prepareInfoLength;
    this->pPrepareInfo = prepareInfoBuffer;
    this->isSinglePhase = fSinglePhase;
    this->notificationType = PrepareRequestNotify;
    this->shimFactory->NewNotification( this );

ErrorExit:

    SafeReleaseInterface( (IUnknown**) &pPrepareInfo );
    // We can now release our pEnlistmentAsync reference.  We don't need it any longer
    // and it causes problems if the app responds to SPC with InDoubt.
    SafeReleaseInterface( (IUnknown**) &pEnlistmentAsync );

    // We only delete the prepareinInfoBuffer if we had an error.
    if ( FAILED( hr ) )
    {
        if ( NULL != prepareInfoBuffer )
        {
            CoTaskMemFree( prepareInfoBuffer );
        }
    }

    return hr;
}


HRESULT __stdcall EnlistmentNotifyShim::CommitRequest(
    DWORD grfRM, 
    XACTUOW* pNewUOW
    )
{
    this->notificationType = CommitRequestNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}


HRESULT __stdcall EnlistmentNotifyShim::AbortRequest(
    BOID* pboidReason,
    BOOL  fRetaining,
    XACTUOW* pNewUOW
    )
{
    if( !this->ignoreSpuriousProxyNotifications )
    {
        // Only create the notification if we have not already voted.
        this->notificationType = AbortRequestNotify;
        this->shimFactory->NewNotification( this );
    }
    return S_OK;
}

HRESULT __stdcall EnlistmentNotifyShim::TMDown()
{
    this->notificationType = EnlistmentTMDownNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

EnlistmentShim::~EnlistmentShim(void)
{
    if( NULL != this->enlistmentNotifyShim )
    {
        // Make sure that the enlistmetNotifyShim does not hold onto a
        // reference to the proxy enlistment object after this.
        this->enlistmentNotifyShim->ClearEnlistmentAsync();
    }
    
    SafeReleaseInterface( (IUnknown**) &this->pEnlistmentAsync );
    SafeReleaseInterface( (IUnknown**) &this->enlistmentNotifyShim );
    SafeReleaseInterface( (IUnknown**) &this->shimFactory );
    SafeReleaseInterface( (IUnknown**) &this->pMarshaler );
}

HRESULT __stdcall EnlistmentShim::QueryInterface(
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
    else if (i_iid == IID_IEnlistmentShim)
    {
        *o_ppv = (IEnlistmentShim *) this ;
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
ULONG __stdcall EnlistmentShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall EnlistmentShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall EnlistmentShim::PrepareRequestDone(
    PrepareVoteType voteType
    )
{
    HRESULT hr = S_OK;
    HRESULT voteHr = S_OK;
    BOID dummyBoid = BOID_NULL;
    BOID* pBoid = NULL;
    BOOL releaseEnlistment = FALSE;

    switch ( voteType )
    {
        case ReadOnly:
        {
            // On W2k Proxy may send a spurious aborted notification if the TM goes down.
            this->enlistmentNotifyShim->SetIgnoreSpuriousProxyNotifications();
            voteHr = XACT_S_READONLY;
            break;
        }

        case SinglePhase:
        {
            // On W2k Proxy may send a spurious aborted notification if the TM goes down.
            this->enlistmentNotifyShim->SetIgnoreSpuriousProxyNotifications();
            voteHr = XACT_S_SINGLEPHASE;
            break;
        }
        
        case Prepared:
        {
            voteHr = S_OK;
            break;
        }

        case Failed:
        {
            // Proxy may send a spurious aborted notification if the TM goes down.
            this->enlistmentNotifyShim->SetIgnoreSpuriousProxyNotifications();
            voteHr = E_FAIL;
            pBoid = &dummyBoid;
            break;
        }

        case InDoubt:
        {
            releaseEnlistment = TRUE;
            break;
        }

        default:  // unexpected, vote no.
        {
            voteHr = E_FAIL;
            pBoid = &dummyBoid;
            break;
        }
    }

    if ( releaseEnlistment )
    {
        SafeReleaseInterface( (IUnknown**) &this->pEnlistmentAsync );
    }
    else
    {
        hr = this->pEnlistmentAsync->PrepareRequestDone(
            voteHr,
            NULL,
            pBoid
            );
    }

    return hr;
}

HRESULT __stdcall EnlistmentShim::CommitRequestDone()
{
    HRESULT hr = S_OK;

    hr = this->pEnlistmentAsync->CommitRequestDone( S_OK );

    return hr;
}

HRESULT __stdcall EnlistmentShim::AbortRequestDone()
{
    HRESULT hr = S_OK;

    hr = this->pEnlistmentAsync->AbortRequestDone( S_OK );

    return hr;
}



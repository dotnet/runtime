//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "StdAfx.h"
#include "notificationshimfactory.h"
#include "voternotifyshim.h"

// {A5FAB903-21CB-49eb-93AE-EF72CD45169E}
EXTERN_C const GUID IID_IVoterBallotShim \
            = { 0xA5FAB903, 0x21CB, 0x49eb, { 0x93, 0xAE, 0xEF, 0x72, 0xCD, 0x45, 0x16, 0x9E } };

HRESULT __stdcall VoterNotifyShim::QueryInterface(
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
    else if (i_iid == IID_ITransactionVoterNotifyAsync2)
    {
        *o_ppv = (ITransactionVoterNotifyAsync2 *) this ;
    }
    else
    {
        return E_NOINTERFACE;
    }
    
    // AddRef the interface pointer:
    ((IUnknown *) *o_ppv)->AddRef ();

    return S_OK;
}

ULONG __stdcall VoterNotifyShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall VoterNotifyShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall VoterNotifyShim::VoteRequest()
{
    this->notificationType = VoteRequestNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

HRESULT __stdcall VoterNotifyShim::Committed(
    BOOL       fRetaining, 
    XACTUOW*   pNewUOW,
    HRESULT    hr
    )
{
    this->notificationType = CommittedNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

HRESULT __stdcall VoterNotifyShim::Aborted(
    BOID*      pboidReason,
    BOOL       fRetaining, 
    XACTUOW*   pNewUOW,
    HRESULT    hr
    )
{
    this->notificationType = AbortedNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}


HRESULT __stdcall VoterNotifyShim::HeuristicDecision(
    DWORD      dwDecision, 
    BOID*      pboidReason,
    HRESULT    hr
    )
{
    if ( XACTHEURISTIC_ABORT == dwDecision )
    {
        this->notificationType = AbortedNotify;
    }
    else if ( XACTHEURISTIC_COMMIT == dwDecision )
    {
        this->notificationType = CommittedNotify;
    }
    else
    {
        this->notificationType = InDoubtNotify;
    }

    this->shimFactory->NewNotification( this );
    return S_OK;
}


HRESULT __stdcall VoterNotifyShim::Indoubt()
{
    this->notificationType = InDoubtNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

VoterShim::~VoterShim(void)
{
    SafeReleaseInterface( (IUnknown**) &this->pVoterBallotAsync2 );
    SafeReleaseInterface( (IUnknown**) &this->voterNotifyShim );
    SafeReleaseInterface( (IUnknown**) &this->shimFactory );
    SafeReleaseInterface( (IUnknown**) &this->pMarshaler );
}

HRESULT __stdcall VoterShim::QueryInterface(
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
    else if (i_iid == IID_IVoterBallotShim)
    {
        *o_ppv = (IVoterBallotShim *) this ;
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

ULONG __stdcall VoterShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall VoterShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall VoterShim::Vote(
    BOOL voteYes )
{
    HRESULT hr = S_OK;
    HRESULT voteHr = S_OK;
    BOID dummyBoid = BOID_NULL;
    BOID* pBoid = NULL;


    if ( !voteYes )
    {
        voteHr = E_FAIL;
        pBoid = &dummyBoid;
    }

    hr = this->pVoterBallotAsync2->VoteRequestDone( voteHr, pBoid );

    return hr;
}



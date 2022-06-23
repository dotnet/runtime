//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "StdAfx.h"
#include "notificationshimfactory.h"
#include "voternotifyshim.h"
#include "phase0notifyshim.h"
#include "transactionshim.h"

// {279031AF-B00E-42e6-A617-79747E22DD22}
EXTERN_C const GUID IID_ITransactionShim \
            = { 0x279031AF, 0xB00E, 0x42e6, { 0xA6, 0x17, 0x79, 0x74, 0x7E, 0x22, 0xDD, 0x22 } };

HRESULT __stdcall TransactionNotifyShim::QueryInterface(
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
    else if (i_iid == IID_ITransactionOutcomeEvents)
    {
        *o_ppv = (ITransactionOutcomeEvents *) this ;
    }
    else
    {
        return E_NOINTERFACE;
    }
    
    // AddRef the interface pointer:
    ((IUnknown *) *o_ppv)->AddRef ();

    return S_OK;
}

ULONG __stdcall TransactionNotifyShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}

ULONG __stdcall TransactionNotifyShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall TransactionNotifyShim::Committed(
    BOOL       fRetaining, 
    XACTUOW*   pNewUOW,
    HRESULT    hr
    )
{
    this->notificationType = CommittedNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

HRESULT __stdcall TransactionNotifyShim::Aborted(
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


HRESULT __stdcall TransactionNotifyShim::HeuristicDecision(
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


HRESULT __stdcall TransactionNotifyShim::Indoubt()
{
    this->notificationType = InDoubtNotify;
    this->shimFactory->NewNotification( this );
    return S_OK;
}

TransactionShim::~TransactionShim(void)
{
    SafeReleaseInterface( (IUnknown**) &this->pTransaction );
    SafeReleaseInterface( (IUnknown**) &this->transactionNotifyShim );
    SafeReleaseInterface( (IUnknown**) &this->shimFactory );
    SafeReleaseInterface( (IUnknown**) &this->pMarshaler );
}

HRESULT __stdcall TransactionShim::QueryInterface(
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
    else if (i_iid == IID_ITransactionShim)
    {
        *o_ppv = (ITransactionShim *) this ;
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

ULONG __stdcall TransactionShim::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}

ULONG __stdcall TransactionShim::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT __stdcall TransactionShim::Commit()
{
    HRESULT hr = S_OK;

    hr = this->pTransaction->Commit(
        FALSE,
        XACTTC_ASYNC,
        0
        );

    return hr;
}

HRESULT __stdcall TransactionShim::Abort()
{
    HRESULT hr = S_OK;
    BOID dummyBoid = BOID_NULL;

    hr = this->pTransaction->Abort(
        &dummyBoid,
        FALSE,
        FALSE
        );

    return hr;
}

HRESULT __stdcall TransactionShim::GetITransactionNative(
    ITransaction** ppTransaction
    )
{
    HRESULT hr = S_OK;
    ITransactionCloner* pCloner = NULL;
    ITransaction* returnTransaction = NULL;

    hr = this->pTransaction->QueryInterface(
        IID_ITransactionCloner,
        (void**) &pCloner );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pCloner->CloneWithCommitDisabled( &returnTransaction );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    *ppTransaction = returnTransaction;

ErrorExit:

    if ( FAILED( hr ) )
    {
        SafeReleaseInterface( (IUnknown**) &returnTransaction );
    }

    SafeReleaseInterface( (IUnknown**) &pCloner );

    return hr;
}

HRESULT __stdcall TransactionShim::Export(
    ULONG whereaboutsSize,
    BYTE* pWhereabouts,
    int* pCookieIndex,
    ULONG* pCookieSize,
    void** ppCookieBuffer
    )
{
    HRESULT hr = S_OK;
    ITransactionExportFactory* pExportFactory = NULL;
    ITransactionExport* pExport = NULL;
    BYTE* cookieBuffer = NULL;
    ULONG cookieSize = 0;
    ULONG cookieSizeUsed = 0;
    int nRetries = MaxRetryCount;

    hr = this->shimFactory->GetExportFactory(
        &pExportFactory );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pExportFactory->Create(
        whereaboutsSize,
        pWhereabouts,
        &pExport
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // 
    // Adding retry logic as a work around for MSDTC's Export/GetTransactionCookie API 
    // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
    //
    nRetries = MaxRetryCount;
    while (nRetries > 0)
    {
        hr = pExport->Export(
            this->pTransaction,
            &cookieSize
            );
        if (hr == S_OK)
        {
            break;
        }
        else if (hr == XACT_E_ALREADYINPROGRESS)
        {
            Sleep(RetryInterval);
            nRetries--;
            continue;
        }
        else
        {
            goto ErrorExit;
        }
    }

    // This buffer gets freed by managed code, if we successfully return it.
    cookieBuffer = (BYTE*) CoTaskMemAlloc( cookieSize );
    if ( NULL == cookieBuffer )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    // 
    // Adding retry logic as a work around for MSDTC's Export/GetTransactionCookie API 
    // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
    //
    nRetries = MaxRetryCount;
    while (nRetries > 0)
    {
        hr = pExport->GetTransactionCookie(
            this->pTransaction,
            cookieSize,
            cookieBuffer,
            &cookieSizeUsed
            );
        if (hr == S_OK)
        {
            break;
        }
        else if (hr == XACT_E_ALREADYINPROGRESS)
        {
            Sleep(RetryInterval);
            nRetries--;
            continue;
        }
        else
        {
            goto ErrorExit;
        }
    }

    *pCookieSize = cookieSize;
    *ppCookieBuffer = cookieBuffer;


ErrorExit:

    if ( FAILED( hr ))
    {
        // This buffer gets freed from managed code if we are successful.  But since we failed, we need to free it.
        if ( NULL != cookieBuffer )
        {
            CoTaskMemFree( cookieBuffer );
            cookieBuffer = NULL;
            *ppCookieBuffer = NULL;
            *pCookieSize = 0;
        }
    }

    SafeReleaseInterface( (IUnknown**) &pExport );
    SafeReleaseInterface( (IUnknown**) &pExportFactory );

    return hr;
}

HRESULT __stdcall TransactionShim::CreateVoter(
    void* managedIdentifier,
    IVoterBallotShim** ppVoterBallotShim
    )
{
    HRESULT hr = S_OK;
    ITransactionVoterFactory2* pVoterFactory = NULL;
    VoterNotifyShim* voterNotifyShim = NULL;
    IUnknown* myNotifyShimRef = NULL;
    VoterShim* voterShim = NULL;
    ITransactionVoterBallotAsync2* voterBallot = NULL;

    hr = this->shimFactory->GetVoterFactory( &pVoterFactory );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    voterNotifyShim = new VoterNotifyShim(
        this->shimFactory,
        managedIdentifier
        );
    if ( NULL == voterNotifyShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = voterNotifyShim->QueryInterface(
        IID_IUnknown,
        (void**) &myNotifyShimRef
        );
    if ( FAILED( hr ) )
    {
        delete voterNotifyShim;
        voterNotifyShim = NULL;
        myNotifyShimRef = NULL;
        goto ErrorExit;
    }

#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    voterShim = new VoterShim(
        this->shimFactory,
        voterNotifyShim
        );
#pragma warning( pop )
    if ( NULL == voterShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = voterShim->Initialize();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pVoterFactory->Create(
        this->pTransaction,
        voterNotifyShim,
        &voterBallot
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    voterShim->SetVoterBallotAsync2(
        voterBallot );

    hr = voterShim->QueryInterface(
        IID_IVoterBallotShim,
        (void**) ppVoterBallotShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }


ErrorExit:
    
    SafeReleaseInterface( (IUnknown**) &voterBallot );
    SafeReleaseInterface( (IUnknown**) &pVoterFactory );
    SafeReleaseInterface( (IUnknown**) &myNotifyShimRef );

    // If we failed, we may need to delete the objects we allocated.
    if ( FAILED( hr ) )
    {
        if ( NULL != voterShim )
        {
            delete voterShim;
            voterShim = NULL;
        }
 
    }

    return hr;
}

HRESULT __stdcall TransactionShim::GetPropagationToken(
    ULONG* pPropagationTokenSize,
    BYTE** ppPropagationToken
    )
{
    HRESULT hr = S_OK;
    CachedTransmitter* cachedTransmitter = NULL;
    ULONG propTokenSize = 0;
    BYTE* propToken = NULL;
    ULONG propTokenSizeUsed = 0;


    hr = this->shimFactory->GetCachedTransmitter( this->pTransaction, &cachedTransmitter );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = cachedTransmitter->pTxTransmitter->GetPropagationTokenSize(
        &propTokenSize
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    propToken = (BYTE*) CoTaskMemAlloc( propTokenSize );
    if ( NULL == propToken )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = cachedTransmitter->pTxTransmitter->MarshalPropagationToken(
        propTokenSize,
        propToken,
        &propTokenSizeUsed
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    *pPropagationTokenSize = propTokenSize;
    *ppPropagationToken = propToken;

ErrorExit:

    if ( FAILED( hr ) )
    {
        if ( NULL != propToken )
        {
            CoTaskMemFree( propToken );
            propToken = NULL;
            *ppPropagationToken = NULL;
            *pPropagationTokenSize = 0;
        }
    }

    if ( NULL != cachedTransmitter )
    {
        this->shimFactory->ReturnCachedTransmitter( cachedTransmitter );
    }

    return hr;
}

HRESULT __stdcall TransactionShim::Phase0Enlist(
    void* managedIdentifier,
    IPhase0EnlistmentShim** ppPhase0EnlistmentShim
    )
{
    HRESULT hr = S_OK;
    ITransactionPhase0EnlistmentAsync* phase0Async = NULL;
    Phase0NotifyShim* phase0NotifyShim = NULL;
    IUnknown* myNotifyShimRef = NULL;
    Phase0Shim* phase0Shim = NULL;
    ITransactionPhase0Factory *phase0Factory = NULL;

    hr = this->pTransaction->QueryInterface(
        IID_ITransactionPhase0Factory,
        (void**) &phase0Factory
        );
    if ( FAILED( hr ) )
    {
        return hr;
    }

    phase0NotifyShim = new Phase0NotifyShim(
        this->shimFactory,
        managedIdentifier
        );
    if ( NULL == phase0NotifyShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = phase0NotifyShim->QueryInterface(
        IID_IUnknown,
        (void**) &myNotifyShimRef
        );
    if ( FAILED( hr ) )
    {
        delete phase0NotifyShim;
        phase0NotifyShim = NULL;
        myNotifyShimRef = NULL;
        goto ErrorExit;
    }

#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    phase0Shim = new Phase0Shim(
        this->shimFactory,
        phase0NotifyShim
        );
#pragma warning( pop )
    if ( NULL == phase0Shim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = phase0Shim->Initialize();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = phase0Factory->Create(
        phase0NotifyShim,
        &phase0Async
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    phase0Shim->SetPhase0EnlistmentAsync( phase0Async );


    hr = phase0Async->Enable();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = phase0Async->WaitForEnlistment();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = phase0Shim->QueryInterface(
        IID_IPhase0EnlistmentShim,
        (void**) ppPhase0EnlistmentShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }


ErrorExit:

    SafeReleaseInterface( (IUnknown**) &phase0Async );
    SafeReleaseInterface( (IUnknown**) &myNotifyShimRef );
    SafeReleaseInterface( (IUnknown**) &phase0Factory );

    if ( FAILED( hr ) )
    {
        if ( NULL != phase0Shim )
        {
            delete phase0Shim;
            phase0Shim = NULL;
        }

    }
    return hr;
}



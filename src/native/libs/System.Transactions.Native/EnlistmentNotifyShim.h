//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#pragma once

EXTERN_C const GUID IID_IEnlistmentShim;

class EnlistmentNotifyShim : public ITransactionResourceAsync, public NotificationShimBase
{
public:
    EnlistmentNotifyShim( NotificationShimFactory* shimFactory, void* enlistmentIdentifier ) : NotificationShimBase( shimFactory, enlistmentIdentifier )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
        this->pEnlistmentAsync = NULL;
        this->ignoreSpuriousProxyNotifications = FALSE;
    }

    ~EnlistmentNotifyShim(void)
    {
        SafeReleaseInterface( (IUnknown**) &this->pEnlistmentAsync );
        // If we are getting released and this->pPrepareInfo is not NULL, then get retrieved the prepare info
        // from the proxy, but it was never retreived by managed code via NotificationShimFactory->GetNotification.
        // So we need to free it here.
        if ( NULL != this->pPrepareInfo )
        {
            CoTaskMemFree( this->pPrepareInfo );
            this->pPrepareInfo = NULL;
        }
    }

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall PrepareRequest(
        BOOL fRetaining, 
        DWORD grfRM, 
        BOOL fWantMoniker,
        BOOL fSinglePhase
        );

    HRESULT __stdcall CommitRequest(
        DWORD grfRM, 
        XACTUOW* pNewUOW
        );

    HRESULT __stdcall AbortRequest(
        BOID* pboidReason,
        BOOL  fRetaining,
        XACTUOW* pNewUOW
        );

    HRESULT __stdcall TMDown();

public:
    void SetEnlistmentAsync( ITransactionEnlistmentAsync* enlistmentAsync )
    {
        this->pEnlistmentAsync = enlistmentAsync;
        this->pEnlistmentAsync->AddRef();
    }

    void SetIgnoreSpuriousProxyNotifications()
    {
        this->ignoreSpuriousProxyNotifications = TRUE;
    }

    void ClearEnlistmentAsync()
    {
        IUnknown* pEnlistmentAsync = NULL;

#if defined(_X86_)
        pEnlistmentAsync = (IUnknown*)InterlockedExchange((LONG volatile*)&this->pEnlistmentAsync, NULL);
#elif defined(_WIN64)
        pEnlistmentAsync = (IUnknown*)InterlockedExchange64((LONGLONG volatile*)&this->pEnlistmentAsync, NULL);
#endif
        SafeReleaseInterface(&pEnlistmentAsync);
    }

private:

    ITransactionEnlistmentAsync* pEnlistmentAsync;

    // MSDTCPRX behaves unpredictably in that if the TM is down when we vote
    // no it will send an AbortRequest.  However if the TM does not go down
    // the enlistment is not go down the AbortRequest is not sent.  This
    // makes reliable cleanup a problem.  To work around this the enlisment
    // shim will eat the AbortRequest if it knows that it has voted No.

    // On Win2k this same problem applies to responding Committed to a
    // single phase commit request.
    BOOL ignoreSpuriousProxyNotifications;

};

class EnlistmentShim : public IEnlistmentShim
{
public:
    EnlistmentShim( NotificationShimFactory* shimFactory, EnlistmentNotifyShim* notifyShim )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();
        this->enlistmentNotifyShim = notifyShim;
        this->enlistmentNotifyShim->AddRef();
        this->refCount = 0;
        this->pEnlistmentAsync = NULL;
    }

    ~EnlistmentShim(void);

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall PrepareRequestDone(
        PrepareVoteType voteType
        );

    HRESULT __stdcall CommitRequestDone();

    HRESULT __stdcall AbortRequestDone();

public:
    HRESULT __stdcall Initialize()
    {
        return CoCreateFreeThreadedMarshaler( reinterpret_cast<IUnknown*> (this), &this->pMarshaler );
    }

    void __stdcall SetEnlistmentAsync( ITransactionEnlistmentAsync* enlistmentAsync )
    {
        this->pEnlistmentAsync = enlistmentAsync;
        this->pEnlistmentAsync->AddRef();
    }

private:

    LONG refCount;
    NotificationShimFactory* shimFactory;
    IUnknown* pMarshaler;

    ITransactionEnlistmentAsync* pEnlistmentAsync;
    EnlistmentNotifyShim* enlistmentNotifyShim;

};

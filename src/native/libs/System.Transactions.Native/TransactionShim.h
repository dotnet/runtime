//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#pragma once

EXTERN_C const GUID IID_ITransactionShim;

class TransactionNotifyShim : public ITransactionOutcomeEvents, public NotificationShimBase
{
public:
    TransactionNotifyShim( NotificationShimFactory* shimFactory, void* enlistmentIdentifier ) : NotificationShimBase( shimFactory, enlistmentIdentifier )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
    }
    ~TransactionNotifyShim(void)
    {
    }

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall Committed(
        BOOL       fRetaining, 
        XACTUOW*   pNewUOW,
        HRESULT    hr
        );

    HRESULT __stdcall Aborted(
        BOID*      pboidReason,
        BOOL       fRetaining, 
        XACTUOW*   pNewUOW,
        HRESULT    hr
        );

    HRESULT __stdcall HeuristicDecision(
        DWORD      dwDecision, 
        BOID*      pboidReason,
        HRESULT    hr
        );

    HRESULT __stdcall Indoubt();
};

class TransactionShim : public ITransactionShim
{
public:
    TransactionShim( NotificationShimFactory* shimFactory, TransactionNotifyShim* notifyShim )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();
        this->transactionNotifyShim = notifyShim;
        this->transactionNotifyShim->AddRef();
        this->refCount = 0;
        this->pTransaction = NULL;
    }
    
    ~TransactionShim(void);

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall Commit();

    HRESULT __stdcall Abort();

    HRESULT __stdcall GetITransactionNative(
        ITransaction** ppTransaction
        );

    HRESULT __stdcall Export(
        ULONG whereaboutsSize,
        BYTE* pWhereabouts,
        int* pCookieIndex,
        ULONG* pCookieSize,
        void** ppCookieBuffer
        );

    HRESULT __stdcall CreateVoter(
        void* managedIdentifier,
        IVoterBallotShim** ppVoterBallotShim
        );

    HRESULT __stdcall GetPropagationToken(
        ULONG* pPropagationTokenSize,
        BYTE** ppPropagationToken
        );

    HRESULT __stdcall Phase0Enlist(
        void* managedIdentifier,
        IPhase0EnlistmentShim** ppPhase0EnlistmentShim
        );

public:
    HRESULT __stdcall Initialize()
    {
        return CoCreateFreeThreadedMarshaler( reinterpret_cast<IUnknown*> (this), &this->pMarshaler );
    }

    HRESULT __stdcall GetTransaction(
        ITransaction** ppTransaction
        )
    {
        *ppTransaction = this->pTransaction;
        return S_OK;
    }

    void __stdcall SetTransaction( ITransaction* transaction )
    {
        this->pTransaction = transaction;
        this->pTransaction->AddRef();
    }

private:

    LONG refCount;
    NotificationShimFactory* shimFactory;
    IUnknown* pMarshaler;

    ITransaction* pTransaction;
    TransactionNotifyShim* transactionNotifyShim;


};

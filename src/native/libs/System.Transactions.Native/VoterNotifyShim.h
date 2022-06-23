//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#pragma once

EXTERN_C const GUID IID_IVoterBallotShim;

class VoterNotifyShim : public ITransactionVoterNotifyAsync2, public NotificationShimBase
{
public:
    VoterNotifyShim( NotificationShimFactory* shimFactory, void* enlistmentIdentifier ) : NotificationShimBase( shimFactory, enlistmentIdentifier )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
    }
    ~VoterNotifyShim(void)
    {
    }

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall VoteRequest();

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

class VoterShim : public IVoterBallotShim
{
public:
    VoterShim( NotificationShimFactory* shimFactory, VoterNotifyShim* notifyShim )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();
        this->voterNotifyShim = notifyShim;
        this->voterNotifyShim->AddRef();
        this->refCount = 0;
        this->pVoterBallotAsync2 = NULL;
    }
    
    ~VoterShim(void);

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall Vote(
        BOOL voteYes );

public:
    HRESULT __stdcall Initialize()
    {
        return CoCreateFreeThreadedMarshaler( reinterpret_cast<IUnknown*> (this), &this->pMarshaler );
    }

    void __stdcall SetVoterBallotAsync2( ITransactionVoterBallotAsync2* voterBallotAsync2 )
    {
        this->pVoterBallotAsync2 = voterBallotAsync2;
        this->pVoterBallotAsync2->AddRef();

    }

private:

    LONG refCount;
    NotificationShimFactory* shimFactory;
    IUnknown* pMarshaler;

    ITransactionVoterBallotAsync2* pVoterBallotAsync2;
    VoterNotifyShim* voterNotifyShim;
};


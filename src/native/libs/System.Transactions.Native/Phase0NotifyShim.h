//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#pragma once

EXTERN_C const GUID IID_IPhase0EnlistmentShim;

class Phase0NotifyShim : public ITransactionPhase0NotifyAsync, public NotificationShimBase
{
public:
    Phase0NotifyShim( NotificationShimFactory* shimFactory, void* enlistmentIdentifier ) : NotificationShimBase( shimFactory, enlistmentIdentifier )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
    }
    ~Phase0NotifyShim(void)
    {
    }

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall Phase0Request(
        BOOL abortingHint
        );

    HRESULT __stdcall EnlistCompleted(
        HRESULT status
        );
};

class Phase0Shim : public IPhase0EnlistmentShim
{
public:
    Phase0Shim( NotificationShimFactory* shimFactory, Phase0NotifyShim* notifyShim )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();
        this->phase0NotifyShim = notifyShim;
        this->phase0NotifyShim->AddRef();
        this->refCount = 0;
        this->pPhase0EnlistmentAsync = NULL;
    }
    ~Phase0Shim(void);

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall Unenlist();

    HRESULT __stdcall Phase0Done(
        BOOL voteYes );

public:
    HRESULT __stdcall Initialize()
    {
        return CoCreateFreeThreadedMarshaler( reinterpret_cast<IUnknown*> (this), &this->pMarshaler );
    }

    void __stdcall SetPhase0EnlistmentAsync( ITransactionPhase0EnlistmentAsync* phase0EnlistmentAsync )
    {
        this->pPhase0EnlistmentAsync = phase0EnlistmentAsync;
        this->pPhase0EnlistmentAsync->AddRef();
    }

private:

    LONG refCount;
    NotificationShimFactory* shimFactory;
    IUnknown* pMarshaler;

    ITransactionPhase0EnlistmentAsync* pPhase0EnlistmentAsync;
    Phase0NotifyShim* phase0NotifyShim;
};

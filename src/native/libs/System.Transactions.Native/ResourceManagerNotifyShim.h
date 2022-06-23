//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#pragma once

EXTERN_C const GUID IID_IResourceManagerShim;

class ResourceManagerNotifyShim : public IResourceManagerSink, public NotificationShimBase
{
public:
    ResourceManagerNotifyShim( NotificationShimFactory* shimFactory, void* enlistmentIdentifier ) : NotificationShimBase( shimFactory, enlistmentIdentifier )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
    }
    ~ResourceManagerNotifyShim(void)
    {
    }

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall TMDown();
};

class ResourceManagerShim : public IResourceManagerShim
{
public:
    ResourceManagerShim( NotificationShimFactory* shimFactory, ResourceManagerNotifyShim* pNotifyShim )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();
        this->pResourceManagerNotifyShim = pNotifyShim;
        this->pResourceManagerNotifyShim->AddRef();
        this->refCount = 0;
        this->pResourceManager = NULL;
    }
    
    ~ResourceManagerShim(void);

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall Enlist(
        ITransactionShim* pTransactionShim,
        void* managedIdentifier,
        IEnlistmentShim** ppEnlistmentShim
        );

    HRESULT __stdcall Reenlist(
        ULONG prepareInfoSize,
        BYTE* prepareInfo,
        TransactionOutcome* pOutcome
        );

    HRESULT __stdcall ReenlistComplete();

public:
    void __stdcall SetResourceManager( IResourceManager* resourceManager )
    {
        this->pResourceManager = resourceManager;
        this->pResourceManager->AddRef();
    }

    HRESULT __stdcall Initialize()
    {
        return CoCreateFreeThreadedMarshaler( reinterpret_cast<IUnknown*> (this), &this->pMarshaler );
    }

private:

    LONG refCount;
    NotificationShimFactory* shimFactory;
    IUnknown* pMarshaler;
    IResourceManager* pResourceManager;
    ResourceManagerNotifyShim* pResourceManagerNotifyShim;

};

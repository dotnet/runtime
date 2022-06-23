//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include <specstrings.h>
#include "utlist.h"

// 
// Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API 
// which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
//
#define RetryInterval  50  // in milli seconds.
#define MaxRetryCount  100

enum ShimNotificationType{
    None                        = 0,
    Phase0RequestNotify         = 1,
    VoteRequestNotify           = 2,
    PrepareRequestNotify        = 3,
    CommitRequestNotify         = 4,
    AbortRequestNotify          = 5,
    CommittedNotify             = 6,
    AbortedNotify               = 7,
    InDoubtNotify               = 8,
    EnlistmentTMDownNotify      = 9,
    ResourceManagerTMDownNotify = 10
    };

enum PrepareVoteType{
    ReadOnly                    = 0,
    SinglePhase                 = 1,
    Prepared                    = 2,
    Failed                      = 3,
    InDoubt                     = 4
    };

enum TransactionOutcome{
    NotKnownYet                 = 0,
    Committed                   = 1,
    Aborted                     = 2
    };

static void SafeReleaseInterface(
    IUnknown** ppInterface
    )
{
    if ( NULL != *ppInterface )
    {
        (*ppInterface)->Release();
        *ppInterface = NULL;
    }
}

class NotificationShimBase;
class CachedInterfaceBase;
class CachedOptions;
class CachedTransmitter;
class CachedReceiver;

//MIDL_INTERFACE("A5FAB903-21CB-49eb-93AE-EF72CD45169E")
interface IVoterBallotShim : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE Vote(
        BOOL voteYes ) = 0;

};

//MIDL_INTERFACE("55FF6514-948A-4307-A692-73B84E2AF53E")
interface IPhase0EnlistmentShim : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE Unenlist() = 0;

    virtual HRESULT STDMETHODCALLTYPE Phase0Done(
        BOOL voteYes ) = 0;
};

//MIDL_INTERFACE("5EC35E09-B285-422c-83F5-1372384A42CC")
interface IEnlistmentShim : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE PrepareRequestDone(
        PrepareVoteType voteType
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE CommitRequestDone() = 0;

    virtual HRESULT STDMETHODCALLTYPE AbortRequestDone() = 0;

};

//MIDL_INTERFACE("279031AF-B00E-42e6-A617-79747E22DD22")
interface ITransactionShim : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE Commit() = 0;

    virtual HRESULT STDMETHODCALLTYPE Abort() = 0;

    virtual HRESULT STDMETHODCALLTYPE GetITransactionNative(
        ITransaction** ppTransaction
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE Export(
        ULONG whereaboutsSize,
        BYTE* pWhereabouts,
        int* pCookieIndex,
        ULONG* pCookieSize,
        void** ppCookieBuffer
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE CreateVoter(
        void* managedIdentifier,
        IVoterBallotShim** ppVoterBallotShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetPropagationToken(
        ULONG* pPropagationTokenSize,
        BYTE** ppPropagationToken
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE Phase0Enlist(
        void* managedIdentifier,
        IPhase0EnlistmentShim** ppPhase0EnlistmentShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetTransaction(
        ITransaction** ppTransaction
        ) = 0;
};

//MIDL_INTERFACE("27C73B91-99F5-46d5-A247-732A1A16529E")
interface IResourceManagerShim : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE Enlist(
        ITransactionShim* pTransactionShim,
        void* managedIdentifier,
        IEnlistmentShim** ppEnlistmentShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE Reenlist(
        ULONG prepareInfoSize,
        BYTE* prepareInfo,
        TransactionOutcome* pOutcome
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE ReenlistComplete() = 0;
};



//MIDL_INTERFACE("467C8BCB-BDDE-4885-B143-317107468275")
interface IDtcProxyShimFactory : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE ConnectToProxy(
        LPWSTR nodeName,
        GUID resourceManagerIdentifier,
        void* managedIdentifier,
        BOOL* pNodeNameMatches,
        int* pWhereaboutsSize,
        void** ppWhereaboutsBuffer,
        IResourceManagerShim** ppResourceManagerShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetNotification(
        void** ppManagedIdentifier,
        ShimNotificationType* pShimNotificationType,
        BOOL* pIsSinglePhase,
        BOOL* pAbortingHint,
        BOOL* pReleaseLock,
        ULONG* pPrepareInfoSize,
        void** ppPrepareInfo
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE ReleaseNotificationLock() = 0;

    virtual HRESULT STDMETHODCALLTYPE BeginTransaction(
        ULONG timeout,
        ISOLEVEL isolationLevel,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ITransactionShim** ppTransactionShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE CreateResourceManager(
        GUID resourceManagerIdentifier,
        void* managedIdentifier,
        IResourceManagerShim** ppResourceManagerShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE Import(
        ULONG cookieSize,
        BYTE* pCookie,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE ReceiveTransaction(
        ULONG propagationTokenSize,
        BYTE* propagationToken,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        ) = 0;

    virtual HRESULT STDMETHODCALLTYPE CreateTransactionShim(
        ITransaction* pTransaction,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        ) = 0;

};

class NotificationShimFactory : public IDtcProxyShimFactory
{
public:
    HRESULT Initialize(
        HANDLE hEvent 
        );

    void NewNotification(
        NotificationShimBase* notification
        );

    HRESULT GetExportFactory(
        ITransactionExportFactory** ppExportFactory
        );

    HRESULT GetVoterFactory(
        ITransactionVoterFactory2** ppVoterFactory
        );

    HRESULT GetResourceManagerFactory(
        IResourceManagerFactory2** ppResourceManagerFactory
        );

    HRESULT GetImport(
        ITransactionImport** ppImport
        );

    HRESULT GetCachedOptions(
        CachedOptions** ppCachedOptions
        );

    void ReturnCachedOptions(
        CachedOptions* pCachedOptions
        );

    HRESULT GetCachedTransmitter(
        ITransaction* pTransaction,
        CachedTransmitter** ppCachedTransmitter
        );

    void ReturnCachedTransmitter(
        CachedTransmitter* pCachedTransmitter
        );

    HRESULT GetCachedReceiver(
        CachedReceiver** ppCachedReceiver
        );

    void ReturnCachedReceiver(
        CachedReceiver* pCachedReceiver
        );

    HRESULT SetupTransaction(
        ITransaction* pTx,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        );

public:
    NotificationShimFactory();
    ~NotificationShimFactory(void);

    HRESULT __stdcall QueryInterface(
        REFIID      i_iid, 
        LPVOID FAR* o_ppv
        );

    ULONG __stdcall AddRef();

    ULONG __stdcall Release();

    HRESULT __stdcall ConnectToProxy(
        __in LPWSTR nodeName,
        GUID resourceManagerIdentifier,
        void* managedIdentifier,
        BOOL* pNodeNameMatches,
        int* pWhereaboutsSize,
        void** ppWhereaboutsBuffer,
        IResourceManagerShim** ppResourceManagerShim
        );

    HRESULT __stdcall GetNotification(
        void** ppManagedIdentifier,
        ShimNotificationType* pShimNotificationType,
        BOOL* pIsSinglePhase,
        BOOL* pAbortingHint,
        BOOL* pReleaseLock,
        ULONG* pPrepareInfoSize,
        void** ppPrepareInfo
        );

    HRESULT __stdcall ReleaseNotificationLock();

    HRESULT __stdcall BeginTransaction(
        ULONG timeout,
        ISOLEVEL isolationLevel,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ITransactionShim** ppTransactionShim
        );

    HRESULT __stdcall CreateResourceManager(
        GUID resourceManagerIdentifier,
        void* managedIdentifier,
        IResourceManagerShim** ppResourceManagerShim
        );

    HRESULT __stdcall Import(
        ULONG cookieSize,
        BYTE* pCookie,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        );

    HRESULT __stdcall ReceiveTransaction(
        ULONG propagationTokenSize,
        BYTE* propagationToken,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        );

    HRESULT __stdcall CreateTransactionShim(
        ITransaction* pTransaction,
        void* managedIdentifier,
        GUID* pTransactionIdentifier,
        ISOLEVEL* pIsolationLevel,
        ITransactionShim** ppTransactionShim
        );

private:
    // Used to synchronize access to the proxy.  This is necessary in
    // initialization because the proxy doesn't like multiple simultaneous callers
    // of GetWhereabouts[Size].  We could have this situation in cases where
    // there are multiple app domains being ititialized in the same process
    // at the same time.
    static volatile LPCRITICAL_SECTION s_pcsxProxyInit;

    LONG refCount;
    // Critical section to protect access to listOfNotifications.
    CRITICAL_SECTION csx;
    BOOL csxInited;

    // This is the list of queued NotificationShimBase objects.
    UTStaticList <NotificationShimBase *> listOfNotifications;
    
    // This is the list of cached ITransactionOptions interfaces.
    // Critical section to protect access to listOfOptions.
    CRITICAL_SECTION csxOptions;
    BOOL csxOptionsInited;
    UTStaticList <CachedInterfaceBase *> listOfOptions;
    
    // This is the list of cached ITransactionTransmitter interfaces.
    // Critical section to protect access to listOfTransmitters.
    CRITICAL_SECTION csxTransmitter;
    BOOL csxTransmitterInited;
    UTStaticList <CachedInterfaceBase *> listOfTransmitters;
    
    // This is the list of cached ITransactionReceiver interfaces.
    // Critical section to protect access to listOfReceivers.
    CRITICAL_SECTION csxReceiver;
    BOOL csxReceiverInited;
    UTStaticList <CachedInterfaceBase *> listOfReceivers;
    
    HANDLE eventHandle;

    // The handle returned by LoadLibraryEx of xolehlp.dll and the fptr to DtcGetTransactionManagerEx.
    HMODULE xoleHlpHandle;
    typedef HRESULT (__cdecl* LPDtcGetTransactionManagerExW)(
        WCHAR * pszHost,
        WCHAR * pszTmName,
        REFIID riid,
        DWORD grfOptions,
        void * pvConfigParams,
        void ** ppvObject
        );
    LPDtcGetTransactionManagerExW pfDtcGetTransactionManagerExW;

    // Free Threaded Marshaler
    IUnknown* pMarshaler;

    // The maximum number of cached interfaces of a given type we keep.  Initialized in the constructor
    // to the number of processors, time 2.
    ULONG maxCachedInterfaces;

    ITransactionDispenser* transactionDispenser;
};

class NotificationShimBase
{
public:
    NotificationShimBase( 
        NotificationShimFactory* shimFactory,
        void* enlistmentIdentifier
        )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();
        this->enlistmentIdentifier = enlistmentIdentifier;
        this->refCount = 0;
        this->notificationType = None;
        this->abortingHint = FALSE;
        this->isSinglePhase = FALSE;
        this->prepareInfoSize = 0;
        this->pPrepareInfo = NULL;

// do this in the derived constructors to get offsets right.
//#pragma warning(4 : 4355)
//      link.Init( this );
//#pragma warning(default : 4355)
    }


    virtual ~NotificationShimBase(void)
    {
        this->shimFactory->Release();
    }

public:
    UTLink <NotificationShimBase *> link;
    void* enlistmentIdentifier;
    ShimNotificationType notificationType;
    BOOL abortingHint;
    BOOL isSinglePhase;
    int prepareInfoSize;
    void* pPrepareInfo;

protected:
    LONG refCount;
    NotificationShimFactory* shimFactory;

public:
    virtual ULONG BaseAddRef()
    {
        return InterlockedIncrement( &this->refCount );
    }

    virtual ULONG BaseRelease()
    {
        ULONG localRefCount = InterlockedDecrement( &this->refCount );
        if ( 0 == localRefCount )
        {
            delete this;
        }
        return localRefCount;
    }
};

class CachedInterfaceBase
{
public:
    CachedInterfaceBase( 
        NotificationShimFactory* shimFactory
        )
    {
        this->shimFactory = shimFactory;
        this->shimFactory->AddRef();

// do this in the derived constructors to get offsets right.
//#pragma warning(4 : 4355)
//      link.Init( this );
//#pragma warning(default : 4355)
    }

    ~CachedInterfaceBase(void)
    {
        this->shimFactory->Release();
    }
public:
    UTLink <CachedInterfaceBase *> link;

    NotificationShimFactory* shimFactory;
};

class CachedOptions : public CachedInterfaceBase
{
public:
    CachedOptions( NotificationShimFactory* shimFactory, ITransactionOptions* pOptions ) : CachedInterfaceBase( shimFactory )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
        this->pTxOptions = pOptions;
    }
    ~CachedOptions(void)
    {
        SafeReleaseInterface( (IUnknown**) &this->pTxOptions );
    }

    ITransactionOptions* pTxOptions;
};

class CachedTransmitter : public CachedInterfaceBase
{
public:
    CachedTransmitter( NotificationShimFactory* shimFactory, ITransactionTransmitter* pTransmitter ) : CachedInterfaceBase( shimFactory )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
        this->pTxTransmitter = pTransmitter;
    }
    ~CachedTransmitter(void)
    {
        SafeReleaseInterface( (IUnknown**) &this->pTxTransmitter );
    }

    ITransactionTransmitter* pTxTransmitter;
};

class CachedReceiver : public CachedInterfaceBase
{
public:
    CachedReceiver( NotificationShimFactory* shimFactory, ITransactionReceiver* pReceiver ) : CachedInterfaceBase( shimFactory )
    {
#pragma warning(4 : 4355)
        link.Init( this );
#pragma warning(default : 4355)
        this->pTxReceiver = pReceiver;
    }
    ~CachedReceiver(void)
    {
        SafeReleaseInterface( (IUnknown**) &this->pTxReceiver );
    }

    ITransactionReceiver* pTxReceiver;
};







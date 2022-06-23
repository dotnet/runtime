//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "StdAfx.h"
#include <assert.h>
#include <strsafe.h>
#include "notificationshimfactory.h"
//#include "NotificationShimBase.h"
#include "phase0notifyshim.h"
#include "voternotifyshim.h"
#include "enlistmentnotifyshim.h"
#include "resourcemanagernotifyshim.h"
#include "transactionshim.h"

// {467C8BCB-BDDE-4885-B143-317107468275}
EXTERN_C const GUID IID_IDtcProxyShimFactory \
            = { 0x467c8bcb, 0xbdde, 0x4885, { 0xb1, 0x43, 0x31, 0x71, 0x7, 0x46, 0x82, 0x75 } };

volatile LPCRITICAL_SECTION NotificationShimFactory::s_pcsxProxyInit = NULL;

NotificationShimFactory::NotificationShimFactory()
{
    this->refCount = 0;
    this->eventHandle = INVALID_HANDLE_VALUE;
    this->listOfNotifications.Init();
    this->pMarshaler = NULL;
    this->csxInited = FALSE;
    this->transactionDispenser = NULL;

    SYSTEM_INFO sysInfo;
    GetSystemInfo( &sysInfo );
    this->maxCachedInterfaces = sysInfo.dwNumberOfProcessors * 2;

    this->listOfOptions.Init();
    this->csxOptionsInited = FALSE;

    this->listOfTransmitters.Init();
    this->csxTransmitterInited = FALSE;

    this->listOfReceivers.Init();
    this->csxReceiverInited = FALSE;

}

NotificationShimFactory::~NotificationShimFactory(void)
{
    if( INVALID_HANDLE_VALUE != this->eventHandle )
    {
        ::CloseHandle( this->eventHandle );
    }
    
    if ( this->csxInited )
    {
        DeleteCriticalSection( &this->csx );
    }

    // Clean up the cached ITransactionOptions objects.
    UTLink <CachedInterfaceBase *>   *pOptionsLink;
    bool entryRemoved = FALSE;
    EnterCriticalSection( &this->csxOptions );
    while ( entryRemoved = this->listOfOptions.RemoveFirst (&pOptionsLink) )
    {
        delete (CachedOptions*) pOptionsLink->m_Value;
    }
    LeaveCriticalSection( &this->csxOptions );

    // Clean up the cached ITransctionTransmitter objects.
    UTLink <CachedInterfaceBase *>   *pTransmitterLink;
    entryRemoved = FALSE;
    EnterCriticalSection( &this->csxTransmitter );
    while ( entryRemoved = this->listOfOptions.RemoveFirst (&pTransmitterLink) )
    {
        delete (CachedTransmitter*) pTransmitterLink->m_Value;
    }
    LeaveCriticalSection( &this->csxTransmitter );

    // Clean up the cached ITransctionReceiver objects.
    UTLink <CachedInterfaceBase *>   *pReceiverLink;
    entryRemoved = FALSE;
    EnterCriticalSection( &this->csxReceiver );
    while ( entryRemoved = this->listOfOptions.RemoveFirst (&pReceiverLink) )
    {
        delete (CachedReceiver*) pReceiverLink->m_Value;
    }
    LeaveCriticalSection( &this->csxReceiver );

    if ( this->csxOptionsInited )
    {
        DeleteCriticalSection( &this->csxOptions );
    }

    if ( this->csxTransmitterInited )
    {
        DeleteCriticalSection( &this->csxTransmitter );
    }

    if ( this->csxReceiverInited )
    {
        DeleteCriticalSection( &this->csxReceiver );
    }

    // Release any interface pointers we already have.
    SafeReleaseInterface( (IUnknown**) &this->transactionDispenser );
    SafeReleaseInterface( (IUnknown**) &this->pMarshaler );
}

HRESULT NotificationShimFactory::Initialize( HANDLE hEvent )
{
    BOOL success = FALSE;
    HRESULT hr = S_OK;

    if(!DuplicateHandle(GetCurrentProcess(), hEvent, GetCurrentProcess(), 
            &this->eventHandle, 0, FALSE, DUPLICATE_SAME_ACCESS ))
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        this->eventHandle = INVALID_HANDLE_VALUE;
        goto ErrorExit;
    }

    hr = ::CoCreateFreeThreadedMarshaler( reinterpret_cast<IUnknown*> (this), &this->pMarshaler );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    success = ::InitializeCriticalSectionAndSpinCount( &this->csx, 500 );
    if ( !success )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto ErrorExit;
    }
    this->csxInited = true;

    success = ::InitializeCriticalSectionAndSpinCount( &this->csxOptions, 500 );
    if ( !success )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto ErrorExit;
    }
    this->csxOptionsInited = true;

    success = ::InitializeCriticalSectionAndSpinCount( &this->csxTransmitter, 500 );
    if ( !success )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto ErrorExit;
    }
    this->csxTransmitterInited = true;

    success = ::InitializeCriticalSectionAndSpinCount( &this->csxReceiver, 500 );
    if ( !success )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto ErrorExit;
    }
    this->csxReceiverInited = true;

    if ( NULL == s_pcsxProxyInit )
    {
        CRITICAL_SECTION *pcsxNew = new CRITICAL_SECTION();
        if ( NULL == pcsxNew )
        {
            hr = E_OUTOFMEMORY;
            goto ErrorExit;
        }
        
        success = ::InitializeCriticalSectionAndSpinCount( pcsxNew, 500 );
        if ( !success )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            delete pcsxNew;
            goto ErrorExit;
        }

        ::InterlockedCompareExchangePointer( (volatile PVOID*)&s_pcsxProxyInit, pcsxNew, NULL );
        if( s_pcsxProxyInit != pcsxNew )
        {
            ::DeleteCriticalSection( pcsxNew );
            delete pcsxNew;
        }
    }

    this->xoleHlpHandle = NULL;
    this->pfDtcGetTransactionManagerExW = NULL;

ErrorExit:
    return hr;
}

void NotificationShimFactory::NewNotification(
    NotificationShimBase* notification
    )
{
    assert( ! notification->link.IsLinked() );
    EnterCriticalSection( &this->csx );
    notification->BaseAddRef();
    this->listOfNotifications.InsertLast( &notification->link );
    LeaveCriticalSection( &this->csx );

    SetEvent( this->eventHandle );
}

HRESULT __stdcall NotificationShimFactory::QueryInterface(
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
    else if (i_iid == IID_IDtcProxyShimFactory)
    {
        *o_ppv = (IDtcProxyShimFactory *) this ;
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

ULONG __stdcall NotificationShimFactory::AddRef()
{
    return InterlockedIncrement( &this->refCount );
}


ULONG __stdcall NotificationShimFactory::Release()
{
    ULONG localRefCount = InterlockedDecrement( &this->refCount );
    if ( 0 == localRefCount )
    {
        delete this;
    }
    return localRefCount;
}

HRESULT NotificationShimFactory::GetExportFactory(
    ITransactionExportFactory** ppExportFactory
    )
{
    return this->transactionDispenser->QueryInterface(
        IID_ITransactionExportFactory,
        (void**) ppExportFactory
        );
}

HRESULT NotificationShimFactory::GetVoterFactory(
    ITransactionVoterFactory2** ppVoterFactory
    )
{
    return this->transactionDispenser->QueryInterface(
        IID_ITransactionVoterFactory2,
        (void**) ppVoterFactory
        );
}

HRESULT NotificationShimFactory::GetResourceManagerFactory(
    IResourceManagerFactory2** ppResourceManagerFactory
    )
{
    return this->transactionDispenser->QueryInterface(
        IID_IResourceManagerFactory2,
        (void**) ppResourceManagerFactory
        );
}

HRESULT NotificationShimFactory::GetImport(
    ITransactionImport** ppImport
    )
{
    return this->transactionDispenser->QueryInterface(
        IID_ITransactionImport,
        (void**) ppImport
        );
}

HRESULT __stdcall NotificationShimFactory::ConnectToProxy(
    __in LPWSTR nodeName,
    GUID resourceManagerIdentifier,
    void* managedIdentifier,
    BOOL* pNodeNameMatches,
    int* pWhereaboutsSize,
    void** ppWhereaboutsBuffer,
    IResourceManagerShim** ppResourceManagerShim
    )
{
    HRESULT hr = S_OK;
    ITmNodeName* pTmNodeName = NULL;
    ULONG tmNodeNameLength = 0;
    LPWSTR tmNodeNameBuffer = NULL;
    ITransactionImportWhereabouts* pImportWhereabouts = NULL;
    ULONG whereaboutsSize = 0;
    BYTE* pWhereaboutsBuffer = NULL;
    ULONG whereaboutsSizeUsed = 0;
    IResourceManagerFactory2* rmFactory = NULL;
    ResourceManagerShim* rmShim = NULL;
    ResourceManagerNotifyShim* rmNotifyShim = NULL;
    IUnknown* myNotifyShimRef = NULL;
    IResourceManager* rm = NULL;
    char rmName[] = "System.Transactions.InternalRM";
    int nRetries = MaxRetryCount;

    bool csHeld = FALSE;

    ITransactionDispenser* localDispenser = NULL;
    
    // Dynamically load the XOLEHLP.dll if necessary and get the FunctionPtr for DtcGetTransactionManagerEx.
    if ( NULL == this->pfDtcGetTransactionManagerExW )
    {
        if ( NULL == this->xoleHlpHandle )
        {
            WCHAR fullyQualifiedName[MAX_PATH+40];
            UINT systemDirLength = GetSystemDirectoryW( fullyQualifiedName, MAX_PATH+1 );
            // fullyQualifiedName is null terminated, but systemDirLength is number of TCHARs, NOT including the null terminator.
            // fullyQualifiedName will NOT include a trailing backslash if it is the root directory.
            if ( L'\\' != fullyQualifiedName[systemDirLength-1] )
            {
                fullyQualifiedName[systemDirLength] = L'\\';
                systemDirLength++;
                fullyQualifiedName[systemDirLength] = L'\0';
            }
            // We allocated MAX_PATH+40, so we should have room for XOLEHLP.dll in the buffer.
            hr = StringCchCatW(fullyQualifiedName, sizeof(fullyQualifiedName)/sizeof(WCHAR), L"XOLEHLP.dll");
            if ( FAILED(hr) )
            {
                goto ErrorExit;
            }

            this->xoleHlpHandle = LoadLibraryExW( fullyQualifiedName, NULL, 0 );
            if ( NULL == this->xoleHlpHandle )
            {
                DWORD error = GetLastError();
                hr = HRESULT_FROM_WIN32( error );
                goto ErrorExit;
            }
        }

        this->pfDtcGetTransactionManagerExW = (LPDtcGetTransactionManagerExW) GetProcAddress( this->xoleHlpHandle, "DtcGetTransactionManagerExW" );
        if ( NULL == this->pfDtcGetTransactionManagerExW )
        {
            DWORD error = GetLastError();
            hr = HRESULT_FROM_WIN32( error );
            goto ErrorExit;
        }
    }

    ::EnterCriticalSection( s_pcsxProxyInit );
    csHeld = TRUE;
    hr = this->pfDtcGetTransactionManagerExW(
        nodeName,
        NULL,
        IID_ITransactionDispenser,
        0,
        NULL,
        (void**) &localDispenser
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // Check to make sure the node name matches.
    if ( NULL != nodeName )
    {
        hr = localDispenser->QueryInterface(
            IID_ITmNodeName,
            (void**) &pTmNodeName
            );
        if ( FAILED( hr ) )
        {
            hr = XACT_E_NOTSUPPORTED;
            goto ErrorExit;
        }

        hr = pTmNodeName->GetNodeNameSize(
            &tmNodeNameLength
            );
        if ( FAILED( hr ) )
        {
            hr = XACT_E_NOTSUPPORTED;
            goto ErrorExit;
        }

        tmNodeNameBuffer = (LPWSTR) CoTaskMemAlloc( tmNodeNameLength * sizeof(WCHAR) );
        if ( NULL == tmNodeNameBuffer )
        {
            hr = E_OUTOFMEMORY;
            goto ErrorExit;
        }

        hr = pTmNodeName->GetNodeName(
            tmNodeNameLength,
            tmNodeNameBuffer
            );
        if ( FAILED( hr ) )
        {
            hr = XACT_E_NOTSUPPORTED;
            goto ErrorExit;
        }

        if ( 0 == _wcsicmp( tmNodeNameBuffer, nodeName ) )
        {
            *pNodeNameMatches = TRUE;
        }
        else
        {
            *pNodeNameMatches = FALSE;
        }

        CoTaskMemFree( tmNodeNameBuffer );
        tmNodeNameBuffer = NULL;
    }
    else
        *pNodeNameMatches = TRUE;
    
    hr = localDispenser->QueryInterface(
        IID_ITransactionImportWhereabouts,
        (void**) &pImportWhereabouts
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // 
    // Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API 
    // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
    //
    nRetries = MaxRetryCount;
    while (nRetries > 0)
    {
        hr = pImportWhereabouts->GetWhereaboutsSize(
            &whereaboutsSize
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

    pWhereaboutsBuffer = (BYTE*) CoTaskMemAlloc( whereaboutsSize );
    if ( NULL == pWhereaboutsBuffer )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    // 
    // Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API 
    // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
    //
    nRetries = MaxRetryCount;
    while (nRetries > 0)
    {
        hr = pImportWhereabouts->GetWhereabouts(
                    whereaboutsSize,
                    pWhereaboutsBuffer,
                    &whereaboutsSizeUsed
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

    // Now we need to create the internal resource manager.
    hr = localDispenser->QueryInterface(
                    IID_IResourceManagerFactory2,
                    (void**) &rmFactory
                    );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    rmNotifyShim = new ResourceManagerNotifyShim(
        this,
        managedIdentifier
        );
    if ( NULL == rmNotifyShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = rmNotifyShim->QueryInterface(
        IID_IUnknown,
        (void**) &myNotifyShimRef
        );
    if ( FAILED( hr ) )
    {
        delete rmNotifyShim;
        rmNotifyShim = NULL;
        myNotifyShimRef = NULL;
        goto ErrorExit;
    }

#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    rmShim = new ResourceManagerShim(
        this,
        rmNotifyShim
        );
#pragma warning( pop )
    if ( NULL == rmShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = rmShim->Initialize();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // 
    // Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API 
    // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
    // Resource Manager Factory CreateEx under the covers calls GetWhereAbouts API. 
    //
    nRetries = MaxRetryCount;
    while (nRetries > 0)
    {
        hr = rmFactory->CreateEx(
            &resourceManagerIdentifier,
            (char*) rmName,
            rmNotifyShim,
            IID_IResourceManager,
            (void**) &rm
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

    rmShim->SetResourceManager( rm );

    hr = rmShim->QueryInterface(
        IID_IResourceManagerShim,
        (void**) ppResourceManagerShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    *pWhereaboutsSize = whereaboutsSizeUsed;
    *ppWhereaboutsBuffer = pWhereaboutsBuffer;

ErrorExit:
    if ( SUCCEEDED(hr) )
    {
        if ( NULL == this->transactionDispenser )
        {
            this->transactionDispenser = localDispenser;
        }
        else
        {
            SafeReleaseInterface( (IUnknown**)&localDispenser );
        }
    }

    if ( csHeld )
    {
        ::LeaveCriticalSection( s_pcsxProxyInit );
    }

    SafeReleaseInterface( (IUnknown**) &rm );
    SafeReleaseInterface( (IUnknown**) &myNotifyShimRef );

    if ( FAILED( hr ) )
    {
        if ( NULL != rmShim )
        {
            delete rmShim;
            rmShim = NULL;
        }

        if ( NULL != pWhereaboutsBuffer )
        {
            CoTaskMemFree( pWhereaboutsBuffer );
            pWhereaboutsBuffer = NULL;
        }

        SafeReleaseInterface( (IUnknown**) &localDispenser );
    }

    SafeReleaseInterface( (IUnknown**) &pImportWhereabouts );
    SafeReleaseInterface( (IUnknown**) &rmFactory );
    SafeReleaseInterface( (IUnknown**) &pTmNodeName );
    
    if ( NULL != tmNodeNameBuffer )
    {
        CoTaskMemFree( tmNodeNameBuffer );
        tmNodeNameBuffer = NULL;
    }

    return hr;
}

HRESULT __stdcall NotificationShimFactory::GetNotification(
    void** ppManagedIdentifier,
    ShimNotificationType* pShimNotificationType,
    BOOL* pIsSinglePhase,
    BOOL* pAbortingHint,
    BOOL* pReleaseLock,
    ULONG* pPrepareInfoSize,
    void** ppPrepareInfo
    )
{
    HRESULT hr = S_OK;
    UTLink <NotificationShimBase *>   *plink;
    BOOL entryRemoved = FALSE;
    NotificationShimBase* notification = NULL;

    if ( ( NULL == ppManagedIdentifier ) ||
         ( NULL == pShimNotificationType ) ||
         ( NULL == pIsSinglePhase ) ||
         ( NULL == pAbortingHint ) ||
         ( NULL == pPrepareInfoSize ) ||
         ( NULL == ppPrepareInfo ) ||
         ( NULL == pReleaseLock )
       )
    {
        return E_INVALIDARG;
    }

    *ppManagedIdentifier = NULL;
    *pShimNotificationType = None;
    *pIsSinglePhase = FALSE;
    *pAbortingHint = FALSE;
    *pReleaseLock = FALSE;
    *pPrepareInfoSize = 0;
    *ppPrepareInfo = NULL;
    

    EnterCriticalSection( &this->csx );
    entryRemoved = this->listOfNotifications.RemoveFirst (&plink);

    if ( entryRemoved )
    {
        notification = plink->m_Value;
        *ppManagedIdentifier = notification->enlistmentIdentifier;
        *pShimNotificationType = notification->notificationType;
        *pIsSinglePhase = notification->isSinglePhase;
        *pAbortingHint = notification->abortingHint;
        // only include the prepare info if it is a prepare.  Otherwise the buffer
        // will get freed multiple times.
        if ( PrepareRequestNotify == *pShimNotificationType )
        {
            *pPrepareInfoSize = notification->prepareInfoSize;
            *ppPrepareInfo = notification->pPrepareInfo;
            // The prepareinfo buffer is now owned by the managed code.  We are no longer responsible for freeing it.
            notification->pPrepareInfo = NULL;
        }
    }

    if ( NULL != notification )
    {
        notification->BaseRelease();
    }

    // We release the critical section if we didn't find an entry or if the notification type
    // is NOT ResourceManagerTMDownNotify.  If it is a ResourceManagerTMDownNotify, the managed
    // code will call ReleaseNotificationLock after processing the TMDown.  We need to prevent
    // other notifications from being processed while we are processing TMDown.  But we don't want
    // to force 3 roundtrips to this NotificationShimFactory for all notifications ( 1 to grab the lock,
    // one to get the notification, and one to release the lock).
    if ( ( ! entryRemoved ) || ( ResourceManagerTMDownNotify != *pShimNotificationType ) )
    {
        LeaveCriticalSection( &this->csx );
    }
    else
    {
        *pReleaseLock = TRUE;
    }

    return hr;
}

HRESULT __stdcall NotificationShimFactory::ReleaseNotificationLock()
{
    LeaveCriticalSection( &this->csx );
    return S_OK;
}

HRESULT NotificationShimFactory::SetupTransaction(
    ITransaction* pTx,
    void* managedIdentifier,
    GUID* pTransactionIdentifier,
    ISOLEVEL* pIsolationLevel,
    ITransactionShim** ppTransactionShim
    )
{
    HRESULT hr = S_OK;
    XACTTRANSINFO xactInfo;
    TransactionShim* transactionShim = NULL;
    TransactionNotifyShim* transactionNotifyShim = NULL;
    IUnknown* myNotifyShimRef = NULL;
    IConnectionPoint* pConnPoint = NULL;
    IConnectionPointContainer* pContainer = NULL;
    DWORD connPointCookie;

    transactionNotifyShim = new TransactionNotifyShim( this, managedIdentifier );
    if ( NULL == transactionNotifyShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    // Take a reference on the transactionNotifyShim.  We will release it when we exit.
    hr = transactionNotifyShim->QueryInterface(
        IID_IUnknown,
        (void**) &myNotifyShimRef
        );
    if ( FAILED( hr ) )
    {
        delete transactionNotifyShim;
        transactionNotifyShim = NULL;
        myNotifyShimRef = NULL;
        goto ErrorExit;
    }

#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    transactionShim = new TransactionShim( this, transactionNotifyShim );
#pragma warning( pop )
    if ( NULL == transactionShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = transactionShim->Initialize();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // Get the transaciton id.
    hr = pTx->GetTransactionInfo(
        &xactInfo
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // Register for outcome events.
    hr = pTx->QueryInterface(
        IID_IConnectionPointContainer,
        (void**) &pContainer
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pContainer->FindConnectionPoint(
        IID_ITransactionOutcomeEvents,
        &pConnPoint
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = pConnPoint->Advise(
        reinterpret_cast<IUnknown*> (transactionNotifyShim),
        &connPointCookie
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    transactionShim->SetTransaction( pTx );
    // Don't release the pTx here.  It is owned by our caller!!!

    memcpy( pTransactionIdentifier, &xactInfo.uow.rgb[0], sizeof( GUID ) );
    *pIsolationLevel = xactInfo.isoLevel;

    hr = transactionShim->QueryInterface(
        IID_ITransactionShim,
        (void**) ppTransactionShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

ErrorExit:

    SafeReleaseInterface( (IUnknown**) &pConnPoint );
    SafeReleaseInterface( (IUnknown**) &pContainer );
    SafeReleaseInterface( (IUnknown**) &myNotifyShimRef );

    if ( FAILED( hr ) )
    {
        if ( NULL != transactionShim )
        {
            delete transactionShim;
            transactionShim = NULL;
        }
    }

    return hr;
}

HRESULT __stdcall NotificationShimFactory::BeginTransaction(
    ULONG timeout,
    ISOLEVEL isolationLevel,
    void* managedIdentifier,
    GUID* pTransactionIdentifier,
    ITransactionShim** ppTransactionShim
    )
{
    HRESULT hr = S_OK;
    ITransaction* pTx = NULL;
    CachedOptions* pCachedOptions = NULL;
    ISOLEVEL localIsoLevel;
    XACTOPT xactopt;

    hr = this->GetCachedOptions(
        &pCachedOptions
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    xactopt.ulTimeout = timeout;
    xactopt.szDescription[0] = '\0';

    hr = pCachedOptions->pTxOptions->SetOptions(
        &xactopt
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }
    
    hr = this->transactionDispenser->BeginTransaction(
        NULL,
        isolationLevel,
        0,
        pCachedOptions->pTxOptions,
        &pTx
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = this->SetupTransaction(
        pTx,
        managedIdentifier,
        pTransactionIdentifier,
        &localIsoLevel,
        ppTransactionShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

ErrorExit:

    if ( FAILED( hr ) )
    {
        if ( NULL != pTx )
        {
            BOID dummyBoid = BOID_NULL;
            pTx->Abort(
                &dummyBoid,
                FALSE,
                FALSE
                );
        }
    }

    // If SetupTransaction was successful then it kept its own reference to pTx release
    // the initial one that was created.
    SafeReleaseInterface((IUnknown ** )&pTx);

    if ( NULL != pCachedOptions )
    {
        this->ReturnCachedOptions(
            pCachedOptions
            );
    }

    return hr;

}

HRESULT __stdcall NotificationShimFactory::CreateResourceManager(
    GUID resourceManagerIdentifier,
    void* managedIdentifier,
    IResourceManagerShim** ppResourceManagerShim
    )
{
    HRESULT hr = S_OK;
    IResourceManager* rm = NULL;
    IResourceManagerFactory2* rmFactory = NULL;
    ResourceManagerNotifyShim* rmNotifyShim = NULL;
    IUnknown* myNotifyShimRef = NULL;
    ResourceManagerShim* rmShim = NULL;
    char rmName[] = "System.Transactions.ResourceManager";
    int nRetries = MaxRetryCount;

    hr = GetResourceManagerFactory(
        &rmFactory
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    rmNotifyShim = new ResourceManagerNotifyShim(
        this,
        managedIdentifier
        );
    if ( NULL == rmNotifyShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = rmNotifyShim->QueryInterface(
        IID_IUnknown,
        (void**) &myNotifyShimRef
        );
    if ( FAILED( hr ) )
    {
        delete rmNotifyShim;
        rmNotifyShim = NULL;
        myNotifyShimRef = NULL;
        goto ErrorExit;
    }

#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    rmShim = new ResourceManagerShim(
        this,
        rmNotifyShim
        );
#pragma warning( pop )
    if ( NULL == rmShim )
    {
        hr = E_OUTOFMEMORY;
        goto ErrorExit;
    }

    hr = rmShim->Initialize();
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    // 
    // Adding retry logic as a work around for MSDTC's GetWhereAbouts/GetWhereAboutsSize API 
    // which is single threaded and will return XACT_E_ALREADYINPROGRESS if another thread invokes the API.
    // Resource Manager Factory CreateEx under the covers calls GetWhereAbouts API. 
    //
    nRetries = MaxRetryCount;
    while (nRetries > 0)
    {
        hr = rmFactory->CreateEx(
            &resourceManagerIdentifier,
            (char*) rmName,
            rmNotifyShim,
            IID_IResourceManager,
            (void**) &rm
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

    rmShim->SetResourceManager( rm );

    hr = rmShim->QueryInterface(
        IID_IResourceManagerShim,
        (void**) ppResourceManagerShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

ErrorExit:

    SafeReleaseInterface( (IUnknown**) &myNotifyShimRef );

    if ( FAILED( hr ) )
    {
        if ( NULL != rmShim )
        {
            delete rmShim;
            rmShim = NULL;
        }
    }

    SafeReleaseInterface( (IUnknown**) &rm );
    SafeReleaseInterface( (IUnknown**) &rmFactory );

    return hr;
}

HRESULT __stdcall NotificationShimFactory::Import(
    ULONG cookieSize,
    BYTE* pCookie,
    void* managedIdentifier,
    GUID* pTransactionIdentifier,
    ISOLEVEL* pIsolationLevel,
    ITransactionShim** ppTransactionShim
    )
{
    HRESULT hr = S_OK;
    ITransactionImport* txImport = NULL;
    ITransaction* pTx = NULL;
    IID txIID = IID_ITransaction;


    hr = this->GetImport( &txImport );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = txImport->Import(
        cookieSize,
        pCookie,
        &txIID,
        (void**) &pTx
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = this->SetupTransaction(
        pTx,
        managedIdentifier,
        pTransactionIdentifier,
        pIsolationLevel,
        ppTransactionShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }


ErrorExit:

    SafeReleaseInterface( (IUnknown**) &pTx );
    SafeReleaseInterface( (IUnknown**) &txImport );
    return hr;
}

HRESULT __stdcall NotificationShimFactory::ReceiveTransaction(
    ULONG propagationTokenSize,
    BYTE* propagationToken,
    void* managedIdentifier,
    GUID* pTransactionIdentifier,
    ISOLEVEL* pIsolationLevel,
    ITransactionShim** ppTransactionShim
    )
{
    HRESULT hr = S_OK;
    ITransaction* pTx = NULL;
    CachedReceiver* cachedRcv = NULL;

    hr = this->GetCachedReceiver(
        &cachedRcv
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = cachedRcv->pTxReceiver->UnmarshalPropagationToken(
        propagationTokenSize,
        propagationToken,
        &pTx
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = this->SetupTransaction(
        pTx,
        managedIdentifier,
        pTransactionIdentifier,
        pIsolationLevel,
        ppTransactionShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

ErrorExit:

    SafeReleaseInterface( (IUnknown**) &pTx );
    if ( NULL != cachedRcv )
    {
        this->ReturnCachedReceiver( cachedRcv );
    }

    return hr;
}

HRESULT __stdcall NotificationShimFactory::CreateTransactionShim(
    ITransaction* pTransaction,
    void* managedIdentifier,
    GUID* pTransactionIdentifier,
    ISOLEVEL* pIsolationLevel,
    ITransactionShim** ppTransactionShim
    )
{
    HRESULT hr = S_OK;
    ITransaction* pTx = NULL;
    ITransactionCloner* cloner = NULL;

    hr = pTransaction->QueryInterface(
        IID_ITransactionCloner,
        (void**) &cloner
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = cloner->CloneWithCommitDisabled(
        &pTx
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    hr = this->SetupTransaction(
        pTx,
        managedIdentifier,
        pTransactionIdentifier,
        pIsolationLevel,
        ppTransactionShim
        );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }


ErrorExit:

    SafeReleaseInterface( (IUnknown**) &pTx );
    SafeReleaseInterface( (IUnknown**) &cloner );

    return hr;

}

HRESULT NotificationShimFactory::GetCachedOptions(
    CachedOptions** ppCachedOptions
    )
{
    HRESULT hr = S_OK;
    bool entryRemoved = FALSE;
    UTLink <CachedInterfaceBase *>   *plink;
    ITransactionOptions* pOptions = NULL;
    CachedOptions* localCachedOptions = NULL;

    EnterCriticalSection( &this->csxOptions );

    entryRemoved = this->listOfOptions.RemoveFirst (&plink);
    if ( entryRemoved )
    {
        localCachedOptions = (CachedOptions*) plink->m_Value;
    }
    else
    {
        // We need to allocate a new one.
        hr = this->transactionDispenser->GetOptionsObject(
            &pOptions
            );
        if ( FAILED( hr ) )
        {
            goto ErrorExit;
        }

        localCachedOptions = new CachedOptions( this, pOptions );
        if ( NULL == localCachedOptions )
        {
            hr = E_OUTOFMEMORY;
            goto ErrorExit;
        }
    }

    *ppCachedOptions = localCachedOptions;

ErrorExit:

    LeaveCriticalSection( &this->csxOptions );

    if ( FAILED ( hr ) )
    {
        if ( NULL != localCachedOptions )
        {
            delete localCachedOptions;
            // The delete will release our reference to the options object.
            pOptions = NULL;
        }
        SafeReleaseInterface( (IUnknown**) &pOptions );
    }

    return hr;
}

void NotificationShimFactory::ReturnCachedOptions(
    CachedOptions* pCachedOptions
    )
{
    EnterCriticalSection( &this->csxOptions );
    if ( this->maxCachedInterfaces >= this->listOfOptions.m_ulCount )
    {
        delete pCachedOptions;
    }
    else
    {
        this->listOfOptions.InsertLast( &pCachedOptions->link );
    }
    LeaveCriticalSection( &this->csxOptions );
}

HRESULT NotificationShimFactory::GetCachedTransmitter(
    ITransaction* pTransaction,
    CachedTransmitter** ppCachedTransmitter
    )
{
    HRESULT hr = S_OK;
    bool entryRemoved = FALSE;
    UTLink <CachedInterfaceBase *>   *plink;
    ITransactionTransmitter* pTransmitter = NULL;
    CachedTransmitter* localCachedTransmitter = NULL;
    ITransactionTransmitterFactory* transmitterFactory = NULL;

    EnterCriticalSection( &this->csxTransmitter );

    entryRemoved = this->listOfTransmitters.RemoveFirst (&plink);
    if ( entryRemoved )
    {
        localCachedTransmitter = (CachedTransmitter*) plink->m_Value;
    }
    else
    {
        hr = this->transactionDispenser->QueryInterface(
            IID_ITransactionTransmitterFactory,
            (void**) &transmitterFactory
            );
        if ( FAILED( hr ) )
        {
            goto ReleaseCriticalSection;
        }

        hr = transmitterFactory->Create(
            &pTransmitter
            );
            if ( FAILED( hr ) )
            {
                goto ReleaseCriticalSection;
            }

        localCachedTransmitter = new CachedTransmitter( this, pTransmitter );
        if ( NULL == localCachedTransmitter )
        {
            hr = E_OUTOFMEMORY;
            goto ReleaseCriticalSection;
        }
    }

ReleaseCriticalSection:

    // We need to leave the CriticalSection here, BEFORE we call pTxTransmitter->Set because the Set
    // call may cause transaction promotion, which involves an out-of-process message. We don't
    // want to hold on to the CriticalSection while we do that. The CriticalSection is protecting
    // the listOfTransmitters and we have already done the RemoveFirst and won't be manipulating the
    // list from this point forward.
    LeaveCriticalSection(&this->csxTransmitter);

    // If we had some sort of error above, get out now.
    if (FAILED(hr))
    {
        goto ErrorExit;
    }

    hr = localCachedTransmitter->pTxTransmitter->Set( pTransaction );
    if ( FAILED( hr ) )
    {
        goto ErrorExit;
    }

    *ppCachedTransmitter = localCachedTransmitter;

ErrorExit:

    if ( FAILED ( hr ) )
    {
        if ( NULL != localCachedTransmitter )
        {
            delete localCachedTransmitter;
            // This delete will release the reference we have on the transmitter.
            pTransmitter = NULL;
        }
        SafeReleaseInterface( (IUnknown**) &pTransmitter );
    }

    SafeReleaseInterface( (IUnknown**) &transmitterFactory );

    return hr;
}

void NotificationShimFactory::ReturnCachedTransmitter(
    CachedTransmitter* pCachedTransmitter
    )
{
    EnterCriticalSection( &this->csxTransmitter );
    if ( this->maxCachedInterfaces >= this->listOfTransmitters.m_ulCount )
    {
        delete pCachedTransmitter;
    }
    else
    {
        pCachedTransmitter->pTxTransmitter->Reset();
        this->listOfTransmitters.InsertLast( &pCachedTransmitter->link );
    }
    LeaveCriticalSection( &this->csxTransmitter );
}

HRESULT NotificationShimFactory::GetCachedReceiver(
    CachedReceiver** ppCachedReceiver
    )
{
    HRESULT hr = S_OK;
    bool entryRemoved = FALSE;
    UTLink <CachedInterfaceBase *>   *plink;
    ITransactionReceiver* pReceiver = NULL;
    CachedReceiver* localCachedReceiver = NULL;
    ITransactionReceiverFactory* receiverFactory = NULL;

    EnterCriticalSection( &this->csxReceiver );

    entryRemoved = this->listOfReceivers.RemoveFirst (&plink);
    if ( entryRemoved )
    {
        localCachedReceiver = (CachedReceiver*) plink->m_Value;
    }
    else
    {
        hr = this->transactionDispenser->QueryInterface(
            IID_ITransactionReceiverFactory,
            (void**) &receiverFactory
            );
        if ( FAILED( hr ) )
        {
            goto ErrorExit;
        }

        hr = receiverFactory->Create(
            &pReceiver
            );
            if ( FAILED( hr ) )
            {
                goto ErrorExit;
            }

        localCachedReceiver = new CachedReceiver( this, pReceiver );
        if ( NULL == localCachedReceiver )
        {
            hr = E_OUTOFMEMORY;
            goto ErrorExit;
        }
    }

    *ppCachedReceiver = localCachedReceiver;

ErrorExit:

    LeaveCriticalSection( &this->csxReceiver );

    if ( FAILED ( hr ) )
    {
        if ( NULL != localCachedReceiver )
        {
            delete localCachedReceiver;
            // The delete will release our reference to the receiver object.
            pReceiver = NULL;
        }
        SafeReleaseInterface( (IUnknown**) &pReceiver );
    }

    SafeReleaseInterface( (IUnknown**) &receiverFactory );

    return hr;
}

void NotificationShimFactory::ReturnCachedReceiver(
    CachedReceiver* pCachedReceiver
    )
{
    EnterCriticalSection( &this->csxReceiver );
    if ( this->maxCachedInterfaces >= this->listOfReceivers.m_ulCount )
    {
        delete pCachedReceiver;
    }
    else
    {
        pCachedReceiver->pTxReceiver->Reset();
        this->listOfReceivers.InsertLast( &pCachedReceiver->link );
    }
    LeaveCriticalSection( &this->csxReceiver );
}


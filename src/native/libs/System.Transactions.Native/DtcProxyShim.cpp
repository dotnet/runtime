//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

#include "stdafx.h"
#include "DtcProxyShim.h"
#include "NotificationShimFactory.h"
/*
BOOL APIENTRY DllMain( HANDLE hModule, 
                       DWORD  ul_reason_for_call, 
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

*/
HRESULT GetNotificationFactory(
    HANDLE notificationEventHandle,
    IUnknown** ppProxyShimFactory
    )
{
    HRESULT hr = S_OK;
    IUnknown* pIDtcProxyShimFactory = NULL;
    NotificationShimFactory* factory = NULL;

    if ( NULL == ppProxyShimFactory )
    {
        return E_INVALIDARG;
    }


    // Create the NotificationShimFactory.
#pragma warning( push )
#pragma warning( disable : 4068 )
#pragma prefast(suppress:6014, "The memory is deallocated when the managed code RCW gets collected")
    factory = new NotificationShimFactory();
#pragma warning( pop )
    if ( NULL == factory )
    {
        hr = E_OUTOFMEMORY;
        goto Done;
    }

    hr = factory->Initialize( notificationEventHandle );
    if ( FAILED( hr ) )
    {
        goto Done;
    }

    hr = factory->QueryInterface( IID_IUnknown, (void**) &pIDtcProxyShimFactory );
    if ( FAILED( hr ) )
    {
        goto Done;
    }

    *ppProxyShimFactory = pIDtcProxyShimFactory;

Done:


    if ( S_OK != hr ) 
    {
        if ( NULL != pIDtcProxyShimFactory )
        {
            pIDtcProxyShimFactory->Release();
            factory = NULL;
        }

        if ( NULL != factory )
        {
            delete factory;
        }
    }

    return hr;
}


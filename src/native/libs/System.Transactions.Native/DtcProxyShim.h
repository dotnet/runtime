//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

extern "C"  __declspec(dllexport) HRESULT GetNotificationFactory(
    HANDLE notificationEventHandle,
    IUnknown** ppProxyShimFactory
    );

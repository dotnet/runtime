// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: servprov.h
//
// ===========================================================================
// simplified servprov.h for PAL

#include "rpc.h"
#include "rpcndr.h"

#include "unknwn.h"

#ifndef __IServiceProvider_INTERFACE_DEFINED__
#define __IServiceProvider_INTERFACE_DEFINED__

// 6d5140c1-7436-11ce-8034-00aa006009fa
EXTERN_C const IID IID_IServiceProvider;

interface IServiceProvider : public IUnknown
{
    virtual /* [local] */ HRESULT STDMETHODCALLTYPE QueryService(
        /* [in] */ REFGUID guidService,
        /* [in] */ REFIID riid,
        /* [out] */ void **ppvObject) = 0;
};

#endif // __IServiceProvider_INTERFACE_DEFINED__

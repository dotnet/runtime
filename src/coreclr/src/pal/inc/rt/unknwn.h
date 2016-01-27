// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: unknwn.h
// 
// ===========================================================================
// simplified unknwn.h for PAL

#include "rpc.h"
#include "rpcndr.h"

#ifndef __IUnknown_INTERFACE_DEFINED__
#define __IUnknown_INTERFACE_DEFINED__

typedef interface IUnknown IUnknown;

typedef /* [unique] */ IUnknown *LPUNKNOWN;

// 00000000-0000-0000-C000-000000000046
EXTERN_C const IID IID_IUnknown;

MIDL_INTERFACE("00000000-0000-0000-C000-000000000046")
IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE QueryInterface( 
        REFIID riid,
        void **ppvObject) = 0;
        
    virtual ULONG STDMETHODCALLTYPE AddRef( void) = 0;
        
    virtual ULONG STDMETHODCALLTYPE Release( void) = 0;

    template<class Q>
    HRESULT
    STDMETHODCALLTYPE
    QueryInterface(Q** pp)
    {
        return QueryInterface(__uuidof(Q), (void **)pp);
    }
};

#endif // __IUnknown_INTERFACE_DEFINED__

#ifndef __IClassFactory_INTERFACE_DEFINED__
#define __IClassFactory_INTERFACE_DEFINED__

// 00000001-0000-0000-C000-000000000046
EXTERN_C const IID IID_IClassFactory;
    
MIDL_INTERFACE("00000001-0000-0000-C000-000000000046")
IClassFactory : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE CreateInstance( 
        IUnknown *pUnkOuter,
        REFIID riid,
        void **ppvObject) = 0;
    
    virtual HRESULT STDMETHODCALLTYPE LockServer( 
        BOOL fLock) = 0;
};

#endif // __IClassFactory_INTERFACE_DEFINED__

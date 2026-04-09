// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: weakreference.h
//
// ===========================================================================
// simplified weakreference.h for PAL

#include "rpc.h"
#include "rpcndr.h"

#include "unknwn.h"

#ifndef __IInspectable_INTERFACE_DEFINED__
#define __IInspectable_INTERFACE_DEFINED__

typedef struct HSTRING__{
    int unused;
} HSTRING__;

typedef HSTRING__* HSTRING;

typedef /* [v1_enum] */ 
enum TrustLevel
    {
        BaseTrust	= 0,
        PartialTrust	= ( BaseTrust + 1 ) ,
        FullTrust	= ( PartialTrust + 1 ) 
    } 	TrustLevel;

// AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90
const IID IID_IInspectable = { 0xaf86e2e0, 0xb12d, 0x4c6a, { 0x9c, 0x5a, 0xd7, 0xaa, 0x65, 0x10, 0x1e, 0x90} };

MIDL_INTERFACE("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")
IInspectable : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetIids(
        /* [out] */ ULONG * iidCount,
        /* [size_is][size_is][out] */ IID * *iids) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetRuntimeClassName(
        /* [out] */ HSTRING * className) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetTrustLevel(
        /* [out] */ TrustLevel * trustLevel) = 0;
};
#endif // __IInspectable_INTERFACE_DEFINED__

#ifndef __IWeakReference_INTERFACE_DEFINED__
#define __IWeakReference_INTERFACE_DEFINED__

// 00000037-0000-0000-C000-000000000046
const IID IID_IWeakReference = { 0x00000037, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000037-0000-0000-C000-000000000046")
IWeakReference : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE Resolve(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ IInspectable **objectReference) = 0;

};

#endif // __IWeakReference_INTERFACE_DEFINED__

#ifndef __IWeakReferenceSource_INTERFACE_DEFINED__
#define __IWeakReferenceSource_INTERFACE_DEFINED__

// 00000038-0000-0000-C000-000000000046
const IID IID_IWeakReferenceSource = { 0x00000038, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46} };

MIDL_INTERFACE("00000038-0000-0000-C000-000000000046")
IWeakReferenceSource : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetWeakReference(
        /* [retval][out] */ IWeakReference * *weakReference) = 0;
};

#endif // __IWeakReferenceSource_INTERFACE_DEFINED__

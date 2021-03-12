// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class InspectableTesting : public UnknownImpl, public IInspectableTesting, public IInspectableTesting2
{
public: // IInspectableTesting2
    DEF_FUNC(Add)(
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[out] [retval] */ int* retVal)
    {
        *retVal = a + b;
        return S_OK;
    }

public: // IInspectable
    STDMETHOD(GetIids)( 
        /* [out] */ ULONG *iidCount,
        /* [size_is][size_is][out] */ IID **iids)
    {
        return E_NOTIMPL;
    }
        
    STDMETHOD(GetRuntimeClassName)( 
        /* [out] */ HSTRING *className)
    {
        className = nullptr;
        return S_OK;
    }
    
    STDMETHOD(GetTrustLevel)( 
        /* [out] */ TrustLevel *trustLevel)
    {
        *trustLevel = TrustLevel::FullTrust;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IInspectableTesting *>(this), static_cast<IInspectableTesting2 *>(this), static_cast<IInspectable*>(this));
    }

    DEFINE_REF_COUNTING();
};

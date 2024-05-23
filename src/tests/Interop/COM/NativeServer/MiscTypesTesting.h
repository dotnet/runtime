// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <xplatform.h>
#include "Servers.h"

class MiscTypesTesting : public UnknownImpl, public IMiscTypesTesting
{
public: // IMiscTypesTesting
    DEF_FUNC(Marshal_Variant)(_In_ VARIANT obj, _Out_ VARIANT* result)
    {
        return ::VariantCopy(result, &obj);
    }

    DEF_FUNC(Marshal_Instance_Variant)(_In_ LPCWSTR init, _Out_ VARIANT* result)
    {
        return E_NOTIMPL;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IMiscTypesTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
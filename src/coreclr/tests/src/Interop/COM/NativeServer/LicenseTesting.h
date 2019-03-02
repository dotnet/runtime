// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <xplatform.h>
#include "Servers.h"

class LicenseTesting : public UnknownImpl, public ILicenseTesting
{
private: // static
    static bool s_DenyLicense;
    static BSTR s_License;

public: // static
    static HRESULT RequestLicKey(BSTR *key)
    {
        LPCOLESTR lic = s_License;
        if (lic == nullptr)
            lic = W("__MOCK_LICENSE_KEY__");

        *key = TP_SysAllocString(lic);
        return S_OK;
    }

private:
    BSTR _lic;

public:
    LicenseTesting(_In_opt_ BSTR lic)
        : _lic{ lic }
    {
        if (s_DenyLicense)
            throw CLASS_E_NOTLICENSED;
    }

    ~LicenseTesting()
    {
        CoreClrBStrFree(_lic);
    }

public: // ILicenseTesting
    DEF_FUNC(SetNextDenyLicense)(_In_ VARIANT_BOOL denyLicense)
    {
        s_DenyLicense = (denyLicense == VARIANT_FALSE) ? false : true;
        return S_OK;
    }

    DEF_FUNC(GetLicense)(_Out_ BSTR *lic)
    {
        *lic = TP_SysAllocString(_lic);
        return S_OK;
    }

    DEF_FUNC(SetNextLicense)(_In_z_ LPCOLESTR lic)
    {
        if (s_License != nullptr)
            CoreClrBStrFree(s_License);

        s_License = TP_SysAllocString(lic);
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<ILicenseTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};

bool LicenseTesting::s_DenyLicense = false;
BSTR LicenseTesting::s_License = nullptr;

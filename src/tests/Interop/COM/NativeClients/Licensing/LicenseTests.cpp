// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <cassert>
#include <Server.Contracts.h>
#include <windows_version_helpers.h>

// COM headers
#include <objbase.h>
#include <combaseapi.h>

#define COM_CLIENT
#include <Servers.h>

#define THROW_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { ::printf("FAILURE: 0x%08x = %s\n", hr, #exp); throw hr; } }
#define THROW_FAIL_IF_FALSE(exp) { if (!(exp)) { ::printf("FALSE: %s\n", #exp); throw E_FAIL; } }

template<COINIT TM>
struct ComInit
{
    const HRESULT Result;

    ComInit()
        : Result{ ::CoInitializeEx(nullptr, TM) }
    { }

    ~ComInit()
    {
        if (SUCCEEDED(Result))
            ::CoUninitialize();
    }
};

using ComMTA = ComInit<COINIT_MULTITHREADED>;

void ActivateViaCoCreateInstance();
void ActivateViaCoGetClassObject();

int __cdecl main()
{
    if (is_windows_nano() == S_OK)
    {
        ::puts("RegFree COM is not supported on Windows Nano. Auto-passing this test.\n");
        return 100;
    }
    ComMTA init;
    if (FAILED(init.Result))
        return -1;

    try
    {
        CoreShimComActivation csact{ W("NETServer"), W("LicenseTesting") };

        ActivateViaCoCreateInstance();
        ActivateViaCoGetClassObject();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    return 100;
}

void ActivateViaCoCreateInstance()
{
    ::printf("License test through CoCreateInstance...\n");

    HRESULT hr;

    ILicenseTesting *licenseTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_LicenseTesting, nullptr, CLSCTX_INPROC, IID_ILicenseTesting, (void**)&licenseTesting));
    THROW_IF_FAILED(licenseTesting->SetNextDenyLicense(VARIANT_TRUE));

    ILicenseTesting *failToCreate = nullptr;
    hr = ::CoCreateInstance(CLSID_LicenseTesting, nullptr, CLSCTX_INPROC, IID_ILicenseTesting, (void**)&failToCreate);
    if (hr != CLASS_E_NOTLICENSED || failToCreate != nullptr)
    {
        ::printf("Should fail to activate without license: %#08x\n", hr);
        throw E_FAIL;
    }

    // Reset the environment
    licenseTesting->SetNextDenyLicense(VARIANT_FALSE);
    licenseTesting->Release();
}

void ActivateViaCoGetClassObject()
{
    ::printf("License test through CoGetClassObject...\n");

    HRESULT hr;

    IClassFactory2 *factory;
    THROW_IF_FAILED(::CoGetClassObject(CLSID_LicenseTesting, CLSCTX_INPROC, nullptr, IID_IClassFactory2, (void**)&factory));

    // Validate license info
    LICINFO info;
    THROW_IF_FAILED(factory->GetLicInfo(&info));
    THROW_FAIL_IF_FALSE(info.fLicVerified != FALSE);
    THROW_FAIL_IF_FALSE(info.fRuntimeKeyAvail == FALSE); // Have not populated the cache

    // Initialize to default key.
    LPCOLESTR key = W("__MOCK_LICENSE_KEY__");

    // Validate license key
    BSTR lic;
    THROW_IF_FAILED(factory->RequestLicKey(0, &lic));
    THROW_FAIL_IF_FALSE(::CompareStringOrdinal(lic, -1, key, -1, FALSE) == CSTR_EQUAL);

    // Create instance
    IUnknown *test;
    THROW_IF_FAILED(factory->CreateInstanceLic(nullptr, nullptr, IID_IUnknown, lic, (void**)&test));
    CoreClrBStrFree(lic);

    ILicenseTesting *licenseTesting;
    THROW_IF_FAILED(test->QueryInterface(&licenseTesting));
    test->Release();

    // Validate license key used
    BSTR licMaybe;
    THROW_IF_FAILED(licenseTesting->GetLicense(&licMaybe));
    THROW_FAIL_IF_FALSE(::CompareStringOrdinal(licMaybe, -1, key, -1, FALSE) == CSTR_EQUAL);
    CoreClrBStrFree(licMaybe);
    licMaybe = nullptr;

    // Set new license key
    key = W("__TEST__");
    THROW_IF_FAILED(licenseTesting->SetNextLicense(key));

    // Free previous instance
    licenseTesting->Release();

    // Create instance and validate key used
    THROW_IF_FAILED(factory->RequestLicKey(0, &lic));
    THROW_FAIL_IF_FALSE(::CompareStringOrdinal(lic, -1, key, -1, FALSE) == CSTR_EQUAL);

    test = nullptr;
    THROW_IF_FAILED(factory->CreateInstanceLic(nullptr, nullptr, IID_IUnknown, lic, (void**)&test));
    CoreClrBStrFree(lic);

    licenseTesting = nullptr;
    THROW_IF_FAILED(test->QueryInterface(&licenseTesting));
    test->Release();

    // Validate license key used
    THROW_IF_FAILED(licenseTesting->GetLicense(&licMaybe));
    THROW_FAIL_IF_FALSE(::CompareStringOrdinal(licMaybe, -1, key, -1, FALSE) == CSTR_EQUAL);
    CoreClrBStrFree(licMaybe);

    licenseTesting->Release();
    factory->Release();
}

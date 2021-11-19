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

void ActivateClassWithDefaultInterfaces();
void FailToActivateDefaultInterfaceInstance();
void FailToQueryInterfaceForDefaultInterface();

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
        CoreShimComActivation csact{ W("NetServer.DefaultInterfaces"), W("DefaultInterfaceTesting") };

        ActivateClassWithDefaultInterfaces();
        FailToActivateDefaultInterfaceInstance();
        FailToQueryInterfaceForDefaultInterface();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    return 100;
}

void ActivateClassWithDefaultInterfaces()
{
    ::printf("Activate class using default interfaces via IUnknown...\n");

    HRESULT hr;

    // Validate a class that has an interface with function definitions can be activated
    {
        ComSmartPtr<IUnknown> unknown;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_DefaultInterfaceTesting, nullptr, CLSCTX_INPROC, IID_IUnknown, (void**)&unknown));
        THROW_FAIL_IF_FALSE(unknown != nullptr);
    }

    {
        ComSmartPtr<IClassFactory> classFactory;
        THROW_IF_FAILED(::CoGetClassObject(CLSID_DefaultInterfaceTesting, CLSCTX_INPROC, nullptr, IID_IClassFactory, (void**)&classFactory));

        ComSmartPtr<IUnknown> unknown;
        THROW_IF_FAILED(classFactory->CreateInstance(nullptr, IID_IUnknown, (void**)&unknown));
        THROW_FAIL_IF_FALSE(unknown != nullptr);
    }
}

const int COR_E_INVALIDOPERATION = 0x80131509;

void FailToActivateDefaultInterfaceInstance()
{
    ::printf("Fail to activate class via a default interface...\n");

    HRESULT hr;

    ComSmartPtr<IDefaultInterfaceTesting> defInterface;
    hr = ::CoCreateInstance(CLSID_DefaultInterfaceTesting, nullptr, CLSCTX_INPROC, IID_IDefaultInterfaceTesting, (void**)&defInterface);
    THROW_FAIL_IF_FALSE(hr == COR_E_INVALIDOPERATION);
}

void FailToQueryInterfaceForDefaultInterface()
{
    ::printf("Fail to QueryInterface() for default interface...\n");

    HRESULT hr;

    ComSmartPtr<IDefaultInterfaceTesting2> defInterface2;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_DefaultInterfaceTesting, nullptr, CLSCTX_INPROC, IID_IDefaultInterfaceTesting2, (void**)&defInterface2));

    ComSmartPtr<IDefaultInterfaceTesting> defInterface;
    hr = defInterface2->QueryInterface(&defInterface);
    THROW_FAIL_IF_FALSE(hr == COR_E_INVALIDOPERATION);
}

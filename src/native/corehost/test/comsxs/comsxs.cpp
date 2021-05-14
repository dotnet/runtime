// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <thread>
#include <windows.h>
#include <pal.h>

#define IfFailRet(F) if (FAILED(hr = (F))) return hr

int typelib_lookup(const pal::char_t *typelibidStr)
{
    HRESULT hr;
    IID typelibId;
    IfFailRet(IIDFromString(typelibidStr, &typelibId));
    ITypeLib* typelib;
    IfFailRet(LoadRegTypeLib(typelibId, 0, 0, GetUserDefaultLCID(), &typelib));
    std::cout << "Located type library by typeid." << std::endl;
    typelib->Release();
    return S_OK;
}

int activation(const pal::char_t* clsidStr)
{
    HRESULT hr;

    CLSID clsid;
    IfFailRet(CLSIDFromString(clsidStr, &clsid));
    IUnknown* server;
    IfFailRet(CoCreateInstance(clsid, nullptr, CLSCTX_INPROC, __uuidof(IUnknown), (void**)&server));
    server->Release();

    return S_OK;
}

// comsxs.exe (typelib_lookup | activation) guid
int __cdecl wmain(const int argc, const pal::char_t *argv[])
{
    HRESULT hr;
    IfFailRet(CoInitializeEx(NULL, COINIT_MULTITHREADED));
    if (argc < 3)
    {
        return E_INVALIDARG;
    }
    const pal::string_t scenario = argv[1];
    const pal::char_t* guidStr = argv[2];

    if (scenario == _X("typelib_lookup"))
    {
        hr = typelib_lookup(guidStr);
    }
    else if (scenario == _X("activation"))
    {
        hr = activation(guidStr);
    }
    else
    {
        hr = E_INVALIDARG;
    }

    CoUninitialize();
    return hr;
}

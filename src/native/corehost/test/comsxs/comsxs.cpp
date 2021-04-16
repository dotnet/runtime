// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <iostream>
#include <thread>
#include <windows.h>
#include <pal.h>

#define IfFailRet(F) if (FAILED(hr = (F))) return hr

// comsxs.exe typelibId
int __cdecl wmain(const int argc, const pal::char_t *argv[])
{
    HRESULT hr;
    IfFailRet(CoInitializeEx(NULL, COINIT_MULTITHREADED));
    if (argc < 3)
    {
        return E_INVALIDARG;
    }
    const pal::char_t* typelibidStr = argv[1];
    const pal::char_t* clsidStr = argv[2];
    IID typelibId;
    IfFailRet(IIDFromString(typelibidStr, &typelibId));
    CLSID clsid;
    IfFailRet(CLSIDFromString(clsidStr, &clsid));
    ITypeLib* typelib;
    IfFailRet(LoadRegTypeLib(typelibId, 0, 0, GetUserDefaultLCID(), &typelib));
    typelib->Release();

    IUnknown* server;
    IfFailRet(CoCreateInstance(clsid, nullptr, CLSCTX_INPROC, __uuidof(IUnknown), (void**)&server));

    server->Release();
    CoUninitialize();
    return S_OK;
}

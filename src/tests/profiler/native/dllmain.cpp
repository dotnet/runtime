// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "classfactory.h"
#include <cstdio>

// const IID IID_IUnknown      = { 0x00000000, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };

// const IID IID_IClassFactory = { 0x00000001, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };

#if WIN32
#define EXPORT
#else // WIN32
#define EXPORT __attribute__ ((visibility ("default")))
#endif // WIN32

EXPORT BOOL STDMETHODCALLTYPE DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}

extern "C" EXPORT HRESULT STDMETHODCALLTYPE DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
    printf("Profiler.dll!DllGetClassObject\n");
    fflush(stdout);

    if (ppv == nullptr)
    {
        return E_FAIL;
    }

    auto factory = new ClassFactory(rclsid);
    if (factory == nullptr)
    {
        return E_FAIL;
    }

    return factory->QueryInterface(riid, ppv);
}

extern "C" EXPORT HRESULT STDMETHODCALLTYPE DllCanUnloadNow()
{
    return S_OK;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <platformdefines.h>

#ifdef WINDOWS
#include "mscoree.h"

typedef HRESULT  (STDAPICALLTYPE *FnGetCLRRuntimeHost)(REFIID riid, IUnknown **pUnk);

// Returns the ICLRRuntimeHost instance or nullptr on failure.
ICLRRuntimeHost* GetCLRRuntimeHost()
{
    HMODULE coreCLRModule = ::GetModuleHandle(L"coreclr.dll");
    if (!coreCLRModule)
    {
        coreCLRModule = ::GetModuleHandle(L"coreclr.so");
    }
    if (!coreCLRModule)
    {
        coreCLRModule = ::GetModuleHandle(L"coreclr.dynlib");
    }
    if (!coreCLRModule)
    {
        return nullptr;
    }

    FnGetCLRRuntimeHost pfnGetCLRRuntimeHost = (FnGetCLRRuntimeHost)::GetProcAddress(coreCLRModule, "GetCLRRuntimeHost");
    if (!pfnGetCLRRuntimeHost)
    {
        return nullptr;
    }

    ICLRRuntimeHost* clrRuntimeHost = nullptr;
    HRESULT hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost, (IUnknown**)&clrRuntimeHost);
    if (FAILED(hr)) {
        return nullptr;
    }

    return clrRuntimeHost;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE
CallExecuteInDefaultAppDomain(LPCWSTR pwzAssemblyPath,
                        LPCWSTR pwzTypeName,
                        LPCWSTR pwzMethodName,
                        LPCWSTR pwzArgument,
                        DWORD   *pReturnValue)
{
    ICLRRuntimeHost* host = GetCLRRuntimeHost();

    if (!host)
        return E_FAIL;

    auto result = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

    host->Release();

    return result;
}

#else // WINDOWS

#include <cstdint>

typedef uint32_t DWORD;

extern "C" DLL_EXPORT int STDMETHODCALLTYPE
CallExecuteInDefaultAppDomain(LPCWSTR pwzAssemblyPath,
                        LPCWSTR pwzTypeName,
                        LPCWSTR pwzMethodName,
                        LPCWSTR pwzArgument,
                        DWORD   *pReturnValue)
{
    const int E_FAIL = 0x80004005;

    return E_FAIL;
}

#endif // WINDOWS

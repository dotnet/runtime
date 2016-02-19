// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Several strong name tools are in a special scenario because they build in the CoreCLR build process, but
// are expected to run against the desktop VM. Because of this, we need to setup the callback structure that
// Utilcode asks for to point to callbacks that shim to the desktop.  These methods provide that shimming functionality.
//

#include "common.h"

#if defined(FEATURE_CORECLR) 

CoreClrCallbacks *GetCoreClrCallbacks();

//
// Get a pointer to an API out of the shim
//
// Arguments:
//    szApiName - name of the API to get a pointer to
//
//

template<typename FunctionPointer>
FunctionPointer ApiShim(LPCSTR szApiName)
{
    static FunctionPointer pfnApi = NULL;

    if (pfnApi == NULL)
    {
        CoreClrCallbacks *pCallbacks = GetCoreClrCallbacks();
        pfnApi = reinterpret_cast<FunctionPointer>(GetProcAddress(pCallbacks->m_hmodCoreCLR, szApiName));
        _ASSERTE(pfnApi != NULL);
    }

    return pfnApi;
}

//
// Shim APIs, passing off into the desktop VM
//

IExecutionEngine * __stdcall SnIEE()
{
    typedef IExecutionEngine * ( __stdcall *IEEFn_t)();
    return ApiShim<IEEFn_t>("IEE")();
}

STDAPI SnGetCorSystemDirectory(SString&  pbuffer)
{
    typedef HRESULT (__stdcall *GetCorSystemDirectoryFn_t)(SString&);
    return ApiShim<GetCorSystemDirectoryFn_t>("GetCORSystemDirectory")(pbuffer);
}

//
// Initialize a set of CoreCLR callbacks for utilcode to call into the VM with
//
// Return Value:
//    CoreClrCallbacks for UtilCode
//
// Notes:
//    Will not return NULL
//

CoreClrCallbacks *GetCoreClrCallbacks()
{
    static CoreClrCallbacks coreClrCallbacks = { 0 };
    if (coreClrCallbacks.m_hmodCoreCLR == NULL)
    {
        // Run against the desktop CLR
        coreClrCallbacks.m_hmodCoreCLR = WszLoadLibrary(W("mscoree.dll"));
        coreClrCallbacks.m_pfnIEE = SnIEE;
        coreClrCallbacks.m_pfnGetCORSystemDirectory = SnGetCorSystemDirectory;
        coreClrCallbacks.m_pfnGetCLRFunction = NULL;
    }

    return &coreClrCallbacks;
}

// Initialize Utilcode
//
// Notes:
//    Should only be called once
//

void InitUtilcode()
{
#ifdef _DEBUG
    static bool fAlreadyInitialized = false;
    _ASSERTE(!fAlreadyInitialized);
    fAlreadyInitialized = true;
#endif

    InitUtilcode(*GetCoreClrCallbacks());
}

#endif // FEATURE_CORECLR && !STRONGNAME_IN_VM

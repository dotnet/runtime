// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: wrapper.cpp
// 

// 
// This file implements a simple wrapper DLL (mscorpe.dll) which calls properly into mscorpehost.dll.
// It exists because of compatibility with 1.x/2.0 apps running on CLR 4.0+. Such older apps could pass 
// full path to LoadLibrary() Windows API and get this DLL.
// 
// Noone in CLR should ever try to load this DLL directly (using LoadLibrary API). Note that hosting APIs 
// and PInvoke redirect mscorpe.dll to mscorpehost.dll automatically.
// 

#include <MscorpeSxSWrapper.h>

#include <mscoree.h>
#include <metahost.h>

// Globals
HINSTANCE g_hThisInst;  // This library.

//*****************************************************************************
// Handle lifetime of loaded library.
//*****************************************************************************
extern "C"
BOOL WINAPI 
DllMain(
    HINSTANCE hInstance, 
    DWORD     dwReason, 
    LPVOID    lpReserved)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        {   // Save the module handle.
            g_hThisInst = hInstance;
            DisableThreadLibraryCalls((HMODULE)hInstance);
        }
        break;
    case DLL_PROCESS_DETACH:
        break;
    }

    return TRUE;
} // DllMain

// Implementation for utilcode
HINSTANCE 
GetModuleInst()
{
    return g_hThisInst;
} // GetModuleInst

// Load correct SxS version of mscorpe.dll and initialize it (uses shim).
HRESULT 
LoadMscorpe(HMODULE * phModule)
{
    HRESULT hr = S_OK;
    ICLRMetaHost *    pMetaHost = NULL;
    ICLRRuntimeInfo * pCLRRuntimeInfo = NULL;
    
    // Get full DLL path
    WCHAR wszPath[_MAX_PATH];
    DWORD dwLength = GetModuleFileName((HMODULE)g_hThisInst, wszPath, NumItems(wszPath));
    
    if ((dwLength == 0) || 
        ((dwLength == NumItems(wszPath)) && 
         (GetLastError() == ERROR_INSUFFICIENT_BUFFER)))
    {
        IfFailGo(CLR_E_SHIM_RUNTIMELOAD);
    }
    
    // Find start of '\mscorpe.dll'
    LPWSTR wszSeparator = wcsrchr(wszPath, L'\\');
    if (wszSeparator == NULL)
    {
        IfFailGo(CLR_E_SHIM_RUNTIMELOAD);
    }
    // Check the name of this DLL
    _ASSERTE(_wcsicmp(wszSeparator, L"\\mscorpe.dll") == 0);
    // Remove the DLL name
    *wszSeparator = 0;

    // Find start of last directory name (\<version>),
    // C:\Windows\Microsoft.NET\Framework\[[v4.0.12345]]\mscorpe.dll
    LPWSTR wszLastDirectoryName = wcsrchr(wszPath, L'\\');
    if (wszLastDirectoryName == NULL)
    {
        IfFailGo(CLR_E_SHIM_RUNTIMELOAD);
    }
    LPWSTR wszVersion = wszLastDirectoryName + 1;
    
    IfFailGo(CLRCreateInstance(
        CLSID_CLRMetaHost, 
        IID_ICLRMetaHost, 
        reinterpret_cast<LPVOID *>(&pMetaHost)));
    
    IfFailGo(pMetaHost->GetRuntime(
        wszVersion, 
        IID_ICLRRuntimeInfo, 
        reinterpret_cast<LPVOID *>(&pCLRRuntimeInfo)));
    
    // Shim will load correct SxS version of mscorpe.dll and will initialize it
    IfFailGo(pCLRRuntimeInfo->LoadLibrary(
        L"mscorpe.dll", 
        phModule));
    
ErrExit:
    if (pMetaHost != NULL)
    {
        pMetaHost->Release();
        pMetaHost = NULL;
    }
    if (pCLRRuntimeInfo != NULL)
    {
        pCLRRuntimeInfo->Release();
        pCLRRuntimeInfo = NULL;
    }
    
    if (FAILED(hr))
    {
        *phModule = NULL;
    }
    
    return hr;
} // LoadMscorpe

// SxS wrapper of mscorpe.dll entrypoints
typedef MscorpeSxSWrapper<LoadMscorpe> MscorpeSxS;

// Export of 'original' 1.x/2.0 mscorpe.dll
EXTERN_C 
HRESULT __stdcall 
CreateICeeFileGen(
    ICeeFileGen ** ppCeeFileGen)
{
    return MscorpeSxS::CreateICeeFileGen(ppCeeFileGen);
}

// Export of 'original' 1.x/2.0 mscorpe.dll
EXTERN_C 
HRESULT __stdcall 
DestroyICeeFileGen(ICeeFileGen ** ppCeeFileGen)
{
    return MscorpeSxS::DestroyICeeFileGen(ppCeeFileGen);
}

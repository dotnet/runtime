// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: MscorpeSxSWrapper.h
// 

// 
// This file defines a wrapper for SxS version of mscorpe.dll (dynamically loaded via shim).
// 

#pragma once

#include "utilcode.h"

#include "iceefilegen.h"

// 
// Wrapper for calls into mscorpehost.dll (SxS version of mscorpe.dll).
// Template parameter will load the DLL as it is required in the context.
// 
// Note that _LoadMscorpeDll method can be called more than once and in parallel from more threads if race 
// happens.
// 
template <HRESULT (* _LoadMscorpeDll)(HMODULE * phModule)>
class MscorpeSxSWrapper
{
private:
    // mscorpehost.dll module, if not NULL, entry points are already initialized
    static Volatile<HMODULE>                s_hModule;
    // mscorpehost.dll entry points
    static Volatile<PFN_CreateICeeFileGen>  s_pfnCreateICeeFileGen;
    static Volatile<PFN_DestroyICeeFileGen> s_pfnDestroyICeeFileGen;
    
    // Loads the DLL and sets all statics
    static HRESULT Init();
    
public:
    
    // Wrapper of file:ICeeFileGen.cpp#CreateICeeFileGen from mscorpehost.dll
    static HRESULT CreateICeeFileGen(ICeeFileGen ** ppCeeFileGen)
    {
        HRESULT hr = S_OK;
        IfFailGo(Init());
        hr = s_pfnCreateICeeFileGen(ppCeeFileGen);
    ErrExit:
        return hr;
    }
    
    // Wrapper of file:ICeeFileGen.cpp#DestroyICeeFileGen from mscorpehost.dll
    static HRESULT DestroyICeeFileGen(ICeeFileGen ** ppCeeFileGen)
    {
        HRESULT hr = S_OK;
        IfFailGo(Init());
        hr = s_pfnDestroyICeeFileGen(ppCeeFileGen);
    ErrExit:
        return hr;
    }
    
#ifdef _DEBUG
    // Returns TRUE if the DLL has been already loaded
    static BOOL Debug_IsLoaded()
    {
        return (s_hModule != (HMODULE)NULL);
    }
#endif //_DEBUG
};  // class MscorpeSxS

template <HRESULT (* _LoadMscorpeDll)(HMODULE * phModule)>
// code:MscorpeSxS statics initialization
Volatile<HMODULE> MscorpeSxSWrapper<_LoadMscorpeDll>::s_hModule = NULL;

template <HRESULT (* _LoadMscorpeDll)(HMODULE * phModule)>
Volatile<PFN_CreateICeeFileGen> MscorpeSxSWrapper<_LoadMscorpeDll>::s_pfnCreateICeeFileGen = NULL;

template <HRESULT (* _LoadMscorpeDll)(HMODULE * phModule)>
Volatile<PFN_DestroyICeeFileGen> MscorpeSxSWrapper<_LoadMscorpeDll>::s_pfnDestroyICeeFileGen = NULL;

// Loads the DLL and sets all statics
//static 
template <HRESULT (* _LoadMscorpeDll)(HMODULE * phModule)>
HRESULT 
MscorpeSxSWrapper<_LoadMscorpeDll>::Init()
{
    HRESULT hr = S_OK;
    
    if (s_hModule != (HMODULE)NULL)
    {
        return S_OK;
    }
    
    // Local mscorpehost.dll module
    HMODULE hModule = NULL;
    // Local mscorpehost.dll entry points
    PFN_CreateICeeFileGen  pfnCreateICeeFileGen = NULL;
    PFN_DestroyICeeFileGen pfnDestroyICeeFileGen = NULL;
    
    // Load mscorpehost.dll and initialize it
    IfFailGo(_LoadMscorpeDll(&hModule));
    _ASSERTE(hModule != NULL);
    
    pfnCreateICeeFileGen = (PFN_CreateICeeFileGen)GetProcAddress(hModule, "CreateICeeFileGen");
    if (pfnCreateICeeFileGen == NULL)
    {
        IfFailGo(COR_E_EXECUTIONENGINE);
    }
    
    pfnDestroyICeeFileGen = (PFN_DestroyICeeFileGen)GetProcAddress(hModule, "DestroyICeeFileGen");
    if (pfnDestroyICeeFileGen == NULL)
    {
        IfFailGo(COR_E_EXECUTIONENGINE);
    }
    
ErrExit:
    if (SUCCEEDED(hr))
    {
        // First publish mscorpehost.dll entry points
        s_pfnCreateICeeFileGen = pfnCreateICeeFileGen;
        s_pfnDestroyICeeFileGen = pfnDestroyICeeFileGen;
        // Then we can publish/initialize the mscorpehost.dll module
        s_hModule = hModule;
    }
    
    return hr;
} // MscorpeSxSWrapper::Init

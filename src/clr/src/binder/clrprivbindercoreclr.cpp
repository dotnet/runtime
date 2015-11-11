//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "common.h"
#include "assemblybinder.hpp"
#include "clrprivbindercoreclr.h"
#include "clrprivbinderutil.h"

using namespace BINDER_SPACE;

//=============================================================================
// Helper functions
//-----------------------------------------------------------------------------

HRESULT CLRPrivBinderCoreCLR::BindAssemblyByNameWorker(BINDER_SPACE::AssemblyName *pAssemblyName,
                                                       BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly,
                                                       bool excludeAppPaths)
{
    VALIDATE_ARG_RET(pAssemblyName != nullptr && ppCoreCLRFoundAssembly != nullptr);
    HRESULT hr = S_OK;
    
#ifdef _DEBUG
    // MSCORLIB should be bound using BindToSystem
    _ASSERTE(!pAssemblyName->IsMscorlib());
#endif

    hr = AssemblyBinder::BindAssembly(&m_appContext,
                                      pAssemblyName,
                                      NULL,
                                      NULL,
                                      FALSE, //fNgenExplicitBind,
                                      FALSE, //fExplicitBindToNativeImage,
                                      excludeAppPaths,
                                      ppCoreCLRFoundAssembly);
    if (!FAILED(hr))
    {
        (*ppCoreCLRFoundAssembly)->SetBinder(this);
    }
    
    return hr;
}

// ============================================================================
// CLRPrivBinderCoreCLR implementation
// ============================================================================
HRESULT CLRPrivBinderCoreCLR::BindAssemblyByName(IAssemblyName     *pIAssemblyName,
                                                 ICLRPrivAssembly **ppAssembly)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(pIAssemblyName != nullptr && ppAssembly != nullptr);
    
    EX_TRY
    {
        *ppAssembly = nullptr;

        ReleaseHolder<BINDER_SPACE::Assembly> pCoreCLRFoundAssembly;
        ReleaseHolder<AssemblyName> pAssemblyName;

        SAFE_NEW(pAssemblyName, AssemblyName);
        IF_FAIL_GO(pAssemblyName->Init(pIAssemblyName));
        
        hr = BindAssemblyByNameWorker(pAssemblyName, &pCoreCLRFoundAssembly, false /* excludeAppPaths */);
        IF_FAIL_GO(hr);
            
        *ppAssembly = pCoreCLRFoundAssembly.Extract();

Exit:;        
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
        
HRESULT CLRPrivBinderCoreCLR::VerifyBind(IAssemblyName        *AssemblyName,
                                         ICLRPrivAssembly     *pAssembly,
                                         ICLRPrivAssemblyInfo *pAssemblyInfo)
{
    return E_FAIL;
}
         
HRESULT CLRPrivBinderCoreCLR::GetBinderFlags(DWORD *pBinderFlags)
{
    if (pBinderFlags == NULL)
        return E_INVALIDARG;
    *pBinderFlags = BINDER_NONE;
    return S_OK;
}
         
HRESULT CLRPrivBinderCoreCLR::GetBinderID( 
        UINT_PTR *pBinderId)
{
    *pBinderId = reinterpret_cast<UINT_PTR>(this); 
    return S_OK;
}
         
HRESULT CLRPrivBinderCoreCLR::FindAssemblyBySpec( 
            LPVOID pvAppDomain,
            LPVOID pvAssemblySpec,
            HRESULT *pResult,
            ICLRPrivAssembly **ppAssembly)
{
    // We are not using a cache at this level
    // However, assemblies bound by the CoreCLR binder is already cached in the
    // AppDomain and will be resolved from there if required
    return E_FAIL;
}

HRESULT CLRPrivBinderCoreCLR::SetupBindingPaths(SString  &sTrustedPlatformAssemblies,
                                                SString  &sPlatformResourceRoots,
                                                SString  &sAppPaths,
                                                SString  &sAppNiPaths)
{
    HRESULT hr = S_OK;
    
    EX_TRY
    {
        hr = m_appContext.SetupBindingPaths(sTrustedPlatformAssemblies, sPlatformResourceRoots, sAppPaths, sAppNiPaths, TRUE /* fAcquireLock */);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

bool CLRPrivBinderCoreCLR::IsInTpaList(const SString &sFileName)
{
    bool fIsFileOnTpaList = false;
    
    TpaFileNameHash * tpaFileNameMap = m_appContext.GetTpaFileNameList();
    if (tpaFileNameMap != nullptr)
    {
        const FileNameMapEntry *pTpaEntry = tpaFileNameMap->LookupPtr(sFileName.GetUnicode());
        fIsFileOnTpaList = (pTpaEntry != nullptr);
    }
    
    return fIsFileOnTpaList;
}

// See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
// and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
// for an example of how they're used.
HRESULT CLRPrivBinderCoreCLR::Bind(SString           &assemblyDisplayName,
                                   LPCWSTR            wszCodeBase,
                                   PEAssembly        *pParentAssembly,
                                   BOOL               fNgenExplicitBind,
                                   BOOL               fExplicitBindToNativeImage,
                                   ICLRPrivAssembly **ppAssembly)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(ppAssembly != NULL);

    AssemblyName assemblyName;
    
    ReleaseHolder<AssemblyName> pAssemblyName;
    
    if (!assemblyDisplayName.IsEmpty())
    {
        // AssemblyDisplayName can be empty if wszCodeBase is specified.
        SAFE_NEW(pAssemblyName, AssemblyName);
        IF_FAIL_GO(pAssemblyName->Init(assemblyDisplayName));
    }
    
    EX_TRY
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pAsm;
        hr = AssemblyBinder::BindAssembly(&m_appContext,
                                          pAssemblyName,
                                          wszCodeBase,
                                          pParentAssembly,
                                          fNgenExplicitBind,
                                          fExplicitBindToNativeImage,
                                          false, // excludeAppPaths
                                          &pAsm);
        if(SUCCEEDED(hr))
        {
            _ASSERTE(pAsm != NULL);
            pAsm->SetBinder(this);
            *ppAssembly = pAsm.Extract();
        }
    }
    EX_CATCH_HRESULT(hr);
    
Exit:    
    return hr;
}

#ifndef CROSSGEN_COMPILE
HRESULT CLRPrivBinderCoreCLR::PreBindByteArray(PEImage  *pPEImage, BOOL fInspectionOnly)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(pPEImage != NULL);

    EX_TRY
    {
        hr = AssemblyBinder::PreBindByteArray(&m_appContext, pPEImage, fInspectionOnly);
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
}
#endif // CROSSGEN_COMPILE

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "assemblybinder.hpp"
#include "coreclrbindercommon.h"
#include "clrprivbindercoreclr.h"
#include "clrprivbinderutil.h"

using namespace BINDER_SPACE;

//=============================================================================
// Init code
//-----------------------------------------------------------------------------
/* static */
HRESULT CCoreCLRBinderHelper::Init()
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = AssemblyBinder::Startup();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CCoreCLRBinderHelper::DefaultBinderSetupContext(DWORD dwAppDomainId,CLRPrivBinderCoreCLR **ppTPABinder)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if(ppTPABinder != NULL)
        {
            ReleaseHolder<CLRPrivBinderCoreCLR> pBinder;
            SAFE_NEW(pBinder, CLRPrivBinderCoreCLR);
            
            BINDER_SPACE::ApplicationContext *pApplicationContext = pBinder->GetAppContext();
            hr = pApplicationContext->Init();
            if(SUCCEEDED(hr))
            {
                pApplicationContext->SetAppDomainId(dwAppDomainId);
                pBinder->SetManagedAssemblyLoadContext(NULL);
                *ppTPABinder = clr::SafeAddRef(pBinder.Extract());
            }
        }
    }
    EX_CATCH_HRESULT(hr);

Exit:
    return hr;
}

HRESULT CCoreCLRBinderHelper::GetAssemblyIdentity(LPCSTR     szTextualIdentity,
                                                  BINDER_SPACE::ApplicationContext  *pApplicationContext,
                                                  NewHolder<AssemblyIdentityUTF8> &assemblyIdentityHolder)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(szTextualIdentity != NULL);

    EX_TRY
    {
        AssemblyIdentityUTF8 *pAssemblyIdentity = NULL;
        if (pApplicationContext != NULL)
        {
            // This returns a cached copy owned by application context
            hr = pApplicationContext->GetAssemblyIdentity(szTextualIdentity, &pAssemblyIdentity);
            if(SUCCEEDED(hr))
            {
                assemblyIdentityHolder = pAssemblyIdentity;
                assemblyIdentityHolder.SuppressRelease();
            }
        }
        else
        {
            SString sTextualIdentity;

            sTextualIdentity.SetUTF8(szTextualIdentity);

            // This is a private copy
            pAssemblyIdentity = new AssemblyIdentityUTF8();
            hr = TextualIdentityParser::Parse(sTextualIdentity, pAssemblyIdentity);
            if(SUCCEEDED(hr))
            {
                pAssemblyIdentity->PopulateUTF8Fields();
                assemblyIdentityHolder = pAssemblyIdentity;
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//=============================================================================
// Functions that provides binding services beyond the ICLRPrivInterface
//-----------------------------------------------------------------------------

HRESULT CCoreCLRBinderHelper::BindToSystem(ICLRPrivAssembly **ppSystemAssembly, bool fBindToNativeImage)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(ppSystemAssembly != NULL);
    
    EX_TRY
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pAsm;
        StackSString systemPath(SystemDomain::System()->SystemDirectory());
        hr = AssemblyBinder::BindToSystem(systemPath, &pAsm, fBindToNativeImage);
        if(SUCCEEDED(hr))
        {
            _ASSERTE(pAsm != NULL);
            *ppSystemAssembly = pAsm.Extract();
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CCoreCLRBinderHelper::BindToSystemSatellite(SString            &systemPath,
                                                    SString           &sSimpleName,
                                                    SString           &sCultureName,
                                                    ICLRPrivAssembly **ppSystemAssembly)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(ppSystemAssembly != NULL && !systemPath.IsEmpty());
    
    EX_TRY
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pAsm;
        hr = AssemblyBinder::BindToSystemSatellite(systemPath, sSimpleName, sCultureName, &pAsm);
        if(SUCCEEDED(hr))
        {
            _ASSERTE(pAsm != NULL);
            *ppSystemAssembly = pAsm.Extract();
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CCoreCLRBinderHelper::GetAssemblyFromImage(PEImage           *pPEImage,
                                                   PEImage           *pNativePEImage,
                                                   ICLRPrivAssembly **ppAssembly)
{
    HRESULT hr = S_OK;
    VALIDATE_ARG_RET(pPEImage != NULL && ppAssembly != NULL);

    EX_TRY
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pAsm;
        hr = AssemblyBinder::GetAssemblyFromImage(pPEImage, pNativePEImage, &pAsm);
        if(SUCCEEDED(hr))
        {
            _ASSERTE(pAsm != nullptr);
            *ppAssembly = pAsm.Extract();
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//=============================================================================
// Explicitly bind to an assembly by filepath
//=============================================================================
/* static */
HRESULT CCoreCLRBinderHelper::GetAssembly(/* in */  SString     &assemblyPath,
                                   /* in */  BOOL         fInspectionOnly,
                                   /* in */  BOOL         fIsInGAC,
                                   /* in */  BOOL         fExplicitBindToNativeImage,
                                   /* out */ BINDER_SPACE::Assembly   **ppAssembly)
{
    return AssemblyBinder::GetAssembly(assemblyPath,
                                         fInspectionOnly,
                                         fIsInGAC,
                                         fExplicitBindToNativeImage,
                                         ppAssembly
                                         );
}

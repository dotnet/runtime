// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "assemblybinder.hpp"
#include "coreclrbindercommon.h"
#include "clrprivbindercoreclr.h"
#include "bundle.h"

using namespace BINDER_SPACE;

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
            UINT_PTR binderId;
            pBinder->GetBinderID(&binderId);
            hr = pApplicationContext->Init(binderId);
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

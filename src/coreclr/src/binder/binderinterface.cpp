// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// BinderInterface.cpp
//


//
// Implements the public AssemblyBinder interface
//
// ============================================================

#include "assemblybinder.hpp"
#include "assemblyname.hpp"
#include "applicationcontext.hpp"
#include "binderinterface.hpp"
#include "bindresult.inl"
#include "utils.hpp"

#include "ex.h"

using namespace BINDER_SPACE;

namespace BinderInterface
{

    HRESULT Init()
    {
        HRESULT hr = S_OK;
        
        EX_TRY
        {
            hr = AssemblyBinder::Startup();
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    HRESULT SetupContext(LPCWSTR    wszApplicationBase,
                         DWORD      dwAppDomainId,
                         IUnknown **ppIApplicationContext)
    {
        HRESULT hr = S_OK;
        
        EX_TRY
        {
            BINDER_LOG_LOCK();
            BINDER_LOG_ENTER(L"BinderInterface::SetupContext");

            // Verify input arguments
            IF_FALSE_GO(ppIApplicationContext != NULL);

            {
                ReleaseHolder<ApplicationContext> pApplicationContext;

                SAFE_NEW(pApplicationContext, ApplicationContext);
                IF_FAIL_GO(pApplicationContext->Init());
                pApplicationContext->SetAppDomainId(dwAppDomainId);
                *ppIApplicationContext = static_cast<IUnknown *>(pApplicationContext.Extract());
            }

        Exit:
            BINDER_LOG_LEAVE_HR(L"BinderInterface::SetupContext", hr);
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    // See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
    // and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
    // for an example of how they're used.
    HRESULT Bind(IUnknown    *pIApplicationContext,
                 SString     &assemblyDisplayName,
                 LPCWSTR      wszCodeBase,
                 PEAssembly  *pParentAssembly,
                 BOOL         fNgenExplicitBind,
                 BOOL         fExplicitBindToNativeImage,
                 BINDER_SPACE::Assembly   **ppAssembly)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            BINDER_LOG_LOCK();
            BINDER_LOG_ENTER(L"BinderInterface::Bind");
            
            // Verify input arguments
            IF_FALSE_GO(pIApplicationContext != NULL);
            IF_FALSE_GO(ppAssembly != NULL);

            {
                ApplicationContext *pApplicationContext =
                    static_cast<ApplicationContext *>(pIApplicationContext);

                ReleaseHolder<AssemblyName> pAssemblyName;
                if (!assemblyDisplayName.IsEmpty())
                {
                    SAFE_NEW(pAssemblyName, AssemblyName);
                    IF_FAIL_GO(pAssemblyName->Init(assemblyDisplayName));
                }
                
                IF_FAIL_GO(AssemblyBinder::BindAssembly(pApplicationContext,
                                                        pAssemblyName,
                                                        wszCodeBase,
                                                        pParentAssembly,
                                                        fNgenExplicitBind,
                                                        fExplicitBindToNativeImage,
                                                        false, // excludeAppPaths
                                                        ppAssembly));
            }

        Exit:
            BINDER_LOG_LEAVE_HR(L"BinderInterface::Bind", hr);
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    HRESULT BindToSystem(SString   &sSystemDirectory,
                         BINDER_SPACE::Assembly **ppSystemAssembly,
                         bool fBindToNativeImage)
    {
        HRESULT hr = S_OK;

        IF_FALSE_GO(ppSystemAssembly != NULL);

        EX_TRY
        {
            BINDER_LOG_LOCK();

            IF_FAIL_GO(AssemblyBinder::BindToSystem(sSystemDirectory, ppSystemAssembly, fBindToNativeImage));
        }
        EX_CATCH_HRESULT(hr);

    Exit:
        return hr;
    }

    HRESULT SetupBindingPaths(IUnknown *pIApplicationContext,
                            SString &sTrustedPlatformAssemblies,
                            SString &sPlatformResourceRoots,
                            SString &sAppPaths,
                            SString &sAppNiPaths)
    {
        HRESULT hr = S_OK;

        EX_TRY
        {
            BINDER_LOG_LOCK();
            BINDER_LOG_ENTER(L"BinderInterface::SetupBindingPaths");

            // Verify input arguments
            IF_FALSE_GO(pIApplicationContext != NULL);

            {
                ApplicationContext *pApplicationContext =
                    static_cast<ApplicationContext *>(pIApplicationContext);
                _ASSERTE(pApplicationContext != NULL);

                IF_FAIL_GO(pApplicationContext->SetupBindingPaths(sTrustedPlatformAssemblies, sPlatformResourceRoots, sAppPaths, sAppNiPaths, TRUE /* fAcquireLock */));
            }

        Exit:
            BINDER_LOG_LEAVE_HR(L"BinderInterface::SetupBindingPaths", hr);
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

#ifdef BINDER_DEBUG_LOG
    HRESULT Log(LPCWSTR wszMessage)
    {
        HRESULT hr = S_OK;
        
        EX_TRY
        {
            BINDER_LOG_LOCK();
            BINDER_LOG((WCHAR *) wszMessage);
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }
#endif // BINDER_DEBUG_LOG
};

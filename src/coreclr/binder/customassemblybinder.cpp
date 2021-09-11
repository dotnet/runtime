// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "assemblybindercommon.hpp"
#include "defaultassemblybinder.h"
#include "customassemblybinder.h"

#if !defined(DACCESS_COMPILE)

using namespace BINDER_SPACE;

// ============================================================================
// CustomAssemblyBinder implementation
// ============================================================================
HRESULT CustomAssemblyBinder::BindAssemblyByNameWorker(BINDER_SPACE::AssemblyName *pAssemblyName,
                                                       BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly)
{
    VALIDATE_ARG_RET(pAssemblyName != nullptr && ppCoreCLRFoundAssembly != nullptr);
    HRESULT hr = S_OK;

#ifdef _DEBUG
    // CoreLib should be bound using BindToSystem
    _ASSERTE(!pAssemblyName->IsCoreLib());
#endif

    // Do we have the assembly already loaded in the context of the current binder?
    hr = AssemblyBinderCommon::BindAssembly(this,
                                            pAssemblyName,
                                            NULL,  // szCodeBase
                                            NULL,  // pParentAssembly
                                            false, //excludeAppPaths,
                                            ppCoreCLRFoundAssembly);
    if (!FAILED(hr))
    {
        _ASSERTE(*ppCoreCLRFoundAssembly != NULL);
        (*ppCoreCLRFoundAssembly)->SetBinder(this);
    }

    return hr;
}

HRESULT CustomAssemblyBinder::BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName,
    BINDER_SPACE::Assembly** ppAssembly)
{
    // When LoadContext needs to resolve an assembly reference, it will go through the following lookup order:
    //
    // 1) Lookup the assembly within the LoadContext itself. If assembly is found, use it.
    // 2) Invoke the LoadContext's Load method implementation. If assembly is found, use it.
    // 3) Lookup the assembly within DefaultBinder (except for satellite requests). If assembly is found, use it.
    // 4) Invoke the LoadContext's ResolveSatelliteAssembly method (for satellite requests). If assembly is found, use it.
    // 5) Invoke the LoadContext's Resolving event. If assembly is found, use it.
    // 6) Raise exception.
    //
    // This approach enables a LoadContext to override assemblies that have been loaded in TPA context by loading
    // a different (or even the same!) version.

    HRESULT hr = S_OK;
    ReleaseHolder<BINDER_SPACE::Assembly> pCoreCLRFoundAssembly;

    {
        // Step 1 - Try to find the assembly within the LoadContext.
        hr = BindAssemblyByNameWorker(pAssemblyName, &pCoreCLRFoundAssembly);
        if ((hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND)) ||
            (hr == FUSION_E_APP_DOMAIN_LOCKED) || (hr == FUSION_E_REF_DEF_MISMATCH))
        {
            // If we are here, one of the following is possible:
            //
            // 1) The assembly has not been found in the current binder's application context (i.e. it has not already been loaded), OR
            // 2) An assembly with the same simple name was already loaded in the context of the current binder but we ran into a Ref/Def
            //    mismatch (either due to version difference or strong-name difference).
            //
            // Thus, if default binder has been overridden, then invoke it in an attempt to perform the binding for it make the call
            // of what to do next. The host-overridden binder can either fail the bind or return reference to an existing assembly
            // that has been loaded.
            //
            hr = AssemblyBinderCommon::BindUsingHostAssemblyResolver(GetManagedAssemblyLoadContext(), pAssemblyName, m_pDefaultBinder, &pCoreCLRFoundAssembly);
            if (SUCCEEDED(hr))
            {
                // We maybe returned an assembly that was bound to a different AssemblyBinder instance.
                // In such a case, we will not overwrite the binder (which would be wrong since the assembly would not
                // be present in the cache of the current binding context).
                if (pCoreCLRFoundAssembly->GetBinder() == NULL)
                {
                    pCoreCLRFoundAssembly->SetBinder(this);
                }
            }
        }
    }

    IF_FAIL_GO(hr);

    // Extract the assembly reference.
    //
    // For TPA assemblies that were bound, DefaultBinder
    // would have already set the binder reference for the assembly, so we just need to
    // extract the reference now.
    *ppAssembly = pCoreCLRFoundAssembly.Extract();

Exit:;

    return hr;
}

HRESULT CustomAssemblyBinder::BindUsingPEImage( /* in */ PEImage *pPEImage,
                                                /* [retval][out] */ BINDER_SPACE::Assembly **ppAssembly)
{
    HRESULT hr = S_OK;

    EX_TRY
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pCoreCLRFoundAssembly;
        ReleaseHolder<BINDER_SPACE::AssemblyName> pAssemblyName;
        ReleaseHolder<IMDInternalImport> pIMetaDataAssemblyImport;

        PEKIND PeKind = peNone;

        // Get the Metadata interface
        DWORD dwPAFlags[2];
        IF_FAIL_GO(BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags));
        IF_FAIL_GO(AssemblyBinderCommon::TranslatePEToArchitectureType(dwPAFlags, &PeKind));

        _ASSERTE(pIMetaDataAssemblyImport != NULL);

        // Using the information we just got, initialize the assemblyname
        SAFE_NEW(pAssemblyName, BINDER_SPACE::AssemblyName);
        IF_FAIL_GO(pAssemblyName->Init(pIMetaDataAssemblyImport, PeKind));

        // Validate architecture
        if (!BINDER_SPACE::Assembly::IsValidArchitecture(pAssemblyName->GetArchitecture()))
        {
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }

        // Disallow attempt to bind to the core library. Aside from that,
        // the LoadContext can load any assembly (even if it was in a different LoadContext like TPA).
        if (pAssemblyName->IsCoreLib())
        {
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
        }

        hr = AssemblyBinderCommon::BindUsingPEImage(this, pAssemblyName, pPEImage, PeKind, pIMetaDataAssemblyImport, &pCoreCLRFoundAssembly);
        if (hr == S_OK)
        {
            _ASSERTE(pCoreCLRFoundAssembly != NULL);
            pCoreCLRFoundAssembly->SetBinder(this);
            *ppAssembly = pCoreCLRFoundAssembly.Extract();
        }
Exit:;
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

AssemblyLoaderAllocator* CustomAssemblyBinder::GetLoaderAllocator()
{
    return m_pAssemblyLoaderAllocator;
}

//=============================================================================
// Creates an instance of the CustomAssemblyBinder
//
// This method does not take a lock since it is invoked from the ctor of the
// managed AssemblyLoadContext type.
//=============================================================================
/* static */
HRESULT CustomAssemblyBinder::SetupContext(DefaultAssemblyBinder *pDefaultBinder,
                                           AssemblyLoaderAllocator* pLoaderAllocator,
                                           void* loaderAllocatorHandle,
                                           UINT_PTR ptrAssemblyLoadContext,
                                           CustomAssemblyBinder **ppBindContext)
{
    HRESULT hr = E_FAIL;
    EX_TRY
    {
        if(ppBindContext != NULL)
        {
            NewHolder<CustomAssemblyBinder> pBinder;

            SAFE_NEW(pBinder, CustomAssemblyBinder);
            hr = pBinder->GetAppContext()->Init();
            if(SUCCEEDED(hr))
            {
                // Save reference to the DefaultBinder that is required to be present.
                _ASSERTE(pDefaultBinder != NULL);
                pBinder->m_pDefaultBinder = pDefaultBinder;

                // Save the reference to the IntPtr for GCHandle for the managed
                // AssemblyLoadContext instance
                pBinder->SetManagedAssemblyLoadContext(ptrAssemblyLoadContext);

                if (pLoaderAllocator != NULL)
                {
                    // Link to LoaderAllocator, keep a reference to it
                    VERIFY(pLoaderAllocator->AddReferenceIfAlive());
                }
                pBinder->m_pAssemblyLoaderAllocator = pLoaderAllocator;
                pBinder->m_loaderAllocatorHandle = loaderAllocatorHandle;

#if !defined(DACCESS_COMPILE)
                if (pLoaderAllocator != NULL)
                {
                    ((AssemblyLoaderAllocator*)pLoaderAllocator)->RegisterBinder(pBinder);
                }
#endif
                // Return reference to the allocated Binder instance
                *ppBindContext = pBinder.Extract();
            }
        }
    }
    EX_CATCH_HRESULT(hr);

Exit:
    return hr;
}

void CustomAssemblyBinder::PrepareForLoadContextRelease(INT_PTR ptrManagedStrongAssemblyLoadContext)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Add a strong handle so that the managed assembly load context stays alive until the
    // CustomAssemblyBinder::ReleaseLoadContext is called.
    // We keep the weak handle as well since this method can be running on one thread (e.g. the finalizer one)
    // and other thread can be using the weak handle.
    m_ptrManagedStrongAssemblyLoadContext = ptrManagedStrongAssemblyLoadContext;

    _ASSERTE(m_pAssemblyLoaderAllocator != NULL);
    _ASSERTE(m_loaderAllocatorHandle != NULL);

    // We cannot delete the binder here as it is used indirectly when comparing assemblies with the same binder
    // It will be deleted when the LoaderAllocator will be deleted
    // But we can release the LoaderAllocator as we are no longer using it here
    m_pAssemblyLoaderAllocator->Release();
    m_pAssemblyLoaderAllocator = NULL;

    // Destroy the strong handle to the LoaderAllocator in order to let it reach its finalizer
    DestroyHandle(reinterpret_cast<OBJECTHANDLE>(m_loaderAllocatorHandle));
    m_loaderAllocatorHandle = NULL;
}

CustomAssemblyBinder::CustomAssemblyBinder()
{
    m_pDefaultBinder = NULL;
    m_ptrManagedStrongAssemblyLoadContext = NULL;
}

void CustomAssemblyBinder::ReleaseLoadContext()
{
    VERIFY(GetManagedAssemblyLoadContext() != NULL);
    VERIFY(m_ptrManagedStrongAssemblyLoadContext != NULL);

    // This method is called to release the weak and strong handles on the managed AssemblyLoadContext
    // once the Unloading event has been fired
    OBJECTHANDLE handle = reinterpret_cast<OBJECTHANDLE>(GetManagedAssemblyLoadContext());
    DestroyLongWeakHandle(handle);
    handle = reinterpret_cast<OBJECTHANDLE>(m_ptrManagedStrongAssemblyLoadContext);
    DestroyHandle(handle);
    SetManagedAssemblyLoadContext(NULL);
}

#endif // !defined(DACCESS_COMPILE)


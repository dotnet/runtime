// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "assemblybindercommon.hpp"
#include "defaultassemblybinder.h"
#include "customassemblybinder.h"

#if !defined(DACCESS_COMPILE)

using namespace BINDER_SPACE;

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
    HRESULT hr = S_OK;
    EX_TRY
    {
        if(ppBindContext != NULL)
        {
            NewHolder<CustomAssemblyBinder> pBinder;

            SAFE_NEW(pBinder, CustomAssemblyBinder);
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
    // We need to keep the LoaderAllocator pointer set as it still may be needed for creating references between the
    // native LoaderAllocators of two collectible contexts in case the AssemblyLoadContext.Unload was called on the current
    // context before returning from its AssemblyLoadContext.Load override or the context's Resolving event.
    // But we need to release the LoaderAllocator so that it doesn't prevent completion of the final phase of unloading in
    // some cases. It is safe to do as the AssemblyLoaderAllocator is guaranteed to be alive at least until the 
    // CustomAssemblyBinder::ReleaseLoadContext is called, where we NULL this pointer.
    m_pAssemblyLoaderAllocator->Release();

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

    // The AssemblyLoaderAllocator is in a process of shutdown and should not be used 
    // after this point.
    m_pAssemblyLoaderAllocator = NULL;
}

#endif // !defined(DACCESS_COMPILE)


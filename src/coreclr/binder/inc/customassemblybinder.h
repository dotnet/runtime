// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __CUSTOM_ASSEMBLY_BINDER_H__
#define __CUSTOM_ASSEMBLY_BINDER_H__

#include "applicationcontext.hpp"
#include "defaultassemblybinder.h"

#if !defined(DACCESS_COMPILE)

class AssemblyLoaderAllocator;
class PEImage;

class CustomAssemblyBinder final : public AssemblyLoadContext
{
public:

    //=========================================================================
    // AssemblyBinder functions
    //-------------------------------------------------------------------------
    HRESULT BindUsingPEImage(PEImage* pPEImage,
        BOOL fIsNativeImage,
        BINDER_SPACE::Assembly** ppAssembly);

    HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName,
        BINDER_SPACE::Assembly** ppAssembly);

    AssemblyLoaderAllocator* GetLoaderAllocator();

public:
    //=========================================================================
    // Class functions
    //-------------------------------------------------------------------------

    CustomAssemblyBinder();

    static HRESULT SetupContext(DefaultAssemblyBinder *pTPABinder,
                                AssemblyLoaderAllocator* pLoaderAllocator,
                                void* loaderAllocatorHandle,
                                UINT_PTR ptrAssemblyLoadContext,
                                CustomAssemblyBinder **ppBindContext);

    void PrepareForLoadContextRelease(INT_PTR ptrManagedStrongAssemblyLoadContext);
    void ReleaseLoadContext();

    //=========================================================================
    // Internal implementation details
    //-------------------------------------------------------------------------
private:
    HRESULT BindAssemblyByNameWorker(BINDER_SPACE::AssemblyName *pAssemblyName, BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly);

    DefaultAssemblyBinder *m_pTPABinder;

    // A strong GC handle to the managed AssemblyLoadContext. This handle is set when the unload of the AssemblyLoadContext is initiated
    // to keep the managed AssemblyLoadContext alive until the unload is finished.
    // We still keep the weak handle pointing to the same managed AssemblyLoadContext so that native code can use the handle above
    // to refer to it during the whole lifetime of the AssemblyLoadContext.
    INT_PTR m_ptrManagedStrongAssemblyLoadContext;

    AssemblyLoaderAllocator* m_pAssemblyLoaderAllocator;
    void* m_loaderAllocatorHandle;
};

#endif // !defined(DACCESS_COMPILE)
#endif // __CUSTOM_ASSEMBLY_BINDER_H__

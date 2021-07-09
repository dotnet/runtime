// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__
#define __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__

#include "applicationcontext.hpp"
#include "clrprivbindercoreclr.h"

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

class LoaderAllocator;
class PEImage;

class CLRPrivBinderAssemblyLoadContext : public AssemblyLoadContext
{
public:

    //=========================================================================
    // ICLRPrivBinder functions
    //-------------------------------------------------------------------------
    STDMETHOD(BindAssemblyByName)(
            /* [in] */ struct AssemblyNameData *pAssemblyNameData,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);

    STDMETHOD(GetLoaderAllocator)(
        /* [retval][out] */ LPVOID *pLoaderAllocator);

public:
    //=========================================================================
    // Class functions
    //-------------------------------------------------------------------------

    static HRESULT SetupContext(DWORD      dwAppDomainId,
                                CLRPrivBinderCoreCLR *pTPABinder,
                                LoaderAllocator* pLoaderAllocator,
                                void* loaderAllocatorHandle,
                                UINT_PTR ptrAssemblyLoadContext,
                                CLRPrivBinderAssemblyLoadContext **ppBindContext);

    void PrepareForLoadContextRelease(INT_PTR ptrManagedStrongAssemblyLoadContext);
    void ReleaseLoadContext();

    CLRPrivBinderAssemblyLoadContext();

    inline BINDER_SPACE::ApplicationContext *GetAppContext()
    {
        return &m_appContext;
    }

    HRESULT BindUsingPEImage( /* in */ PEImage *pPEImage,
                              /* in */ BOOL fIsNativeImage,
                              /* [retval][out] */ ICLRPrivAssembly **ppAssembly);

    //=========================================================================
    // Internal implementation details
    //-------------------------------------------------------------------------
private:
    HRESULT BindAssemblyByNameWorker(BINDER_SPACE::AssemblyName *pAssemblyName, BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly);

    BINDER_SPACE::ApplicationContext m_appContext;

    CLRPrivBinderCoreCLR *m_pTPABinder;

    // A strong GC handle to the managed AssemblyLoadContext. This handle is set when the unload of the AssemblyLoadContext is initiated
    // to keep the managed AssemblyLoadContext alive until the unload is finished.
    // We still keep the weak handle pointing to the same managed AssemblyLoadContext so that native code can use the handle above
    // to refer to it during the whole lifetime of the AssemblyLoadContext.
    INT_PTR m_ptrManagedStrongAssemblyLoadContext;

    LoaderAllocator* m_pAssemblyLoaderAllocator;
    void* m_loaderAllocatorHandle;
};

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#endif // __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__

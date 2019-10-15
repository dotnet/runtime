// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__
#define __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__

#include "coreclrbindercommon.h"
#include "applicationcontext.hpp"
#include "clrprivbindercoreclr.h"

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;
};

class AppDomain;

class Object;
class Assembly;
class LoaderAllocator;

class CLRPrivBinderAssemblyLoadContext :
    public IUnknownCommon<ICLRPrivBinder>
{
public:

    //=========================================================================
    // ICLRPrivBinder functions
    //-------------------------------------------------------------------------
    STDMETHOD(BindAssemblyByName)( 
            /* [in] */ IAssemblyName *pIAssemblyName,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
        
    STDMETHOD(GetBinderID)( 
            /* [retval][out] */ UINT_PTR *pBinderId);
         
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
    
    inline INT_PTR GetManagedAssemblyLoadContext()
    {
        return m_ptrManagedAssemblyLoadContext;
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
    
    INT_PTR m_ptrManagedAssemblyLoadContext;

    LoaderAllocator* m_pAssemblyLoaderAllocator;
    void* m_loaderAllocatorHandle;
};

#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#endif // __CLRPRIVBINDERASSEMBLYLOADCONTEXT_H__

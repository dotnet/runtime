// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __CLR_PRIV_BINDER_CORECLR_H__
#define __CLR_PRIV_BINDER_CORECLR_H__

#include "coreclrbindercommon.h"
#include "applicationcontext.hpp"

namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;
};

class CLRPrivBinderCoreCLR : public IUnknownCommon<ICLRPrivBinder, IID_ICLRPrivBinder>
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

    HRESULT SetupBindingPaths(SString  &sTrustedPlatformAssemblies,
                              SString  &sPlatformResourceRoots,
                              SString  &sAppPaths,
                              SString  &sAppNiPaths);

    inline BINDER_SPACE::ApplicationContext *GetAppContext()
    {
        return &m_appContext;
    }

    HRESULT Bind(SString     &assemblyDisplayName,
                 LPCWSTR      wszCodeBase,
                 PEAssembly  *pParentAssembly,
                 BOOL         fNgenExplicitBind,
                 BOOL         fExplicitBindToNativeImage,
                 ICLRPrivAssembly **ppAssembly);

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    HRESULT BindUsingPEImage( /* in */ PEImage *pPEImage,
                              /* in */ BOOL fIsNativeImage,
                              /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

    HRESULT BindAssemblyByNameWorker(
            BINDER_SPACE::AssemblyName *pAssemblyName,
            BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly,
            bool excludeAppPaths);

    INT_PTR GetManagedAssemblyLoadContext()
    {
        return m_ptrManagedAssemblyLoadContext;
    }

    void SetManagedAssemblyLoadContext(INT_PTR ptrManagedTPABinderInstance)
    {
        m_ptrManagedAssemblyLoadContext = ptrManagedTPABinderInstance;
    }

    //=========================================================================
    // Internal implementation details
    //-------------------------------------------------------------------------
private:
    BINDER_SPACE::ApplicationContext m_appContext;

    INT_PTR m_ptrManagedAssemblyLoadContext;
};

#endif // __CLR_PRIV_BINDER_CORECLR_H__

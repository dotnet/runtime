// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __CLR_PRIV_BINDER_CORECLR_H__
#define __CLR_PRIV_BINDER_CORECLR_H__

#include "coreclrbindercommon.h"
#include "applicationcontext.hpp"
#include "assemblyloadcontext.h"

namespace BINDER_SPACE
{
    class AssemblyIdentityUTF8;
};

class CLRPrivBinderCoreCLR : public AssemblyLoadContext
{
public:

    //=========================================================================
    // ICLRPrivBinder functions
    //-------------------------------------------------------------------------
    STDMETHOD(BindAssemblyByName)(
            /* [in] */ IAssemblyName *pIAssemblyName,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);

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

    //=========================================================================
    // Internal implementation details
    //-------------------------------------------------------------------------
private:
    BINDER_SPACE::ApplicationContext m_appContext;
};

#endif // __CLR_PRIV_BINDER_CORECLR_H__

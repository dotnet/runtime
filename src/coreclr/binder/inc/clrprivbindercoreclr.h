// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __CLR_PRIV_BINDER_CORECLR_H__
#define __CLR_PRIV_BINDER_CORECLR_H__

#include "applicationcontext.hpp"
#include "assemblyloadcontext.h"

class PEAssembly;
class PEImage;

class CLRPrivBinderCoreCLR : public AssemblyLoadContext
{
public:
    //=========================================================================
    // ICLRPrivBinder functions
    //-------------------------------------------------------------------------
    HRESULT BindAssemblyByName(
        AssemblyNameData* pAssemblyNameData,
        BINDER_SPACE::Assembly** ppAssembly);

    AssemblyLoaderAllocator* GetLoaderAllocator();

public:

    HRESULT SetupBindingPaths(SString  &sTrustedPlatformAssemblies,
                              SString  &sPlatformResourceRoots,
                              SString  &sAppPaths,
                              SString  &sAppNiPaths);

    HRESULT Bind(LPCWSTR      wszCodeBase,
                 PEAssembly  *pParentAssembly,
                 BOOL         fNgenExplicitBind,
                 BOOL         fExplicitBindToNativeImage,
                 BINDER_SPACE::Assembly **ppAssembly);

    HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName *pAssemblyName,
                                  BINDER_SPACE::Assembly **ppAssembly);

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    HRESULT BindUsingPEImage( /* in */ PEImage *pPEImage,
                              /* in */ BOOL fIsNativeImage,
                              /* [retval][out] */ BINDER_SPACE::Assembly **ppAssembly);
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

    HRESULT BindAssemblyByNameWorker(
            BINDER_SPACE::AssemblyName *pAssemblyName,
            BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly,
            bool excludeAppPaths);
};

#endif // __CLR_PRIV_BINDER_CORECLR_H__

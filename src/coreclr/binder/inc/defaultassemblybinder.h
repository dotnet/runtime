// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __DEFAULT_ASSEMBLY_BINDER_H__
#define __DEFAULT_ASSEMBLY_BINDER_H__

#include "applicationcontext.hpp"
#include "assemblyloadcontext.h"

class PEAssembly;
class PEImage;

class DefaultAssemblyBinder final : public AssemblyLoadContext
{
public:
    //=========================================================================
    // AssemblyBinder functions
    //-------------------------------------------------------------------------

    HRESULT BindUsingPEImage(PEImage* pPEImage,
        BINDER_SPACE::Assembly** ppAssembly);

    HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName,
        BINDER_SPACE::Assembly** ppAssembly);

    AssemblyLoaderAllocator* GetLoaderAllocator()
    {
        // Not supported by this binder
        return NULL;
    }

public:

    HRESULT SetupBindingPaths(SString  &sTrustedPlatformAssemblies,
                              SString  &sPlatformResourceRoots,
                              SString  &sAppPaths);

    HRESULT Bind(LPCWSTR      wszCodeBase,
                 PEAssembly  *pParentAssembly,
                 BINDER_SPACE::Assembly **ppAssembly);

private:

    HRESULT BindAssemblyByNameWorker(
            BINDER_SPACE::AssemblyName *pAssemblyName,
            BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly,
            bool excludeAppPaths);
};

#endif // __DEFAULT_ASSEMBLY_BINDER_H__

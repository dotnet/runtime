// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __DEFAULT_ASSEMBLY_BINDER_H__
#define __DEFAULT_ASSEMBLY_BINDER_H__

#include "applicationcontext.hpp"
#include "assemblybinder.h"

class PEAssembly;
class PEImage;

class DefaultAssemblyBinder final : public AssemblyBinder
{
public:

    HRESULT BindUsingPEImage(PEImage* pPEImage,
        bool excludeAppPaths,
        BINDER_SPACE::Assembly** ppAssembly) override;

    HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName,
        BINDER_SPACE::Assembly** ppAssembly) override;

    AssemblyLoaderAllocator* GetLoaderAllocator() override
    {
        // Not supported by this binder
        return NULL;
    }

    bool IsDefault() override
    {
        return true;
    }

public:

    HRESULT SetupBindingPaths(SString  &sTrustedPlatformAssemblies,
                              SString  &sPlatformResourceRoots,
                              SString  &sAppPaths);

    HRESULT BindToSystem(BINDER_SPACE::Assembly **ppSystemAssembly);

private:

    HRESULT BindAssemblyByNameWorker(
            BINDER_SPACE::AssemblyName *pAssemblyName,
            BINDER_SPACE::Assembly **ppCoreCLRFoundAssembly,
            bool excludeAppPaths);
};

#endif // __DEFAULT_ASSEMBLY_BINDER_H__

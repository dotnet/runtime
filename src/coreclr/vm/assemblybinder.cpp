// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "assemblybinder.h"
#include "nativeimage.h"
#include "../binder/inc/assemblyname.hpp"

#ifndef DACCESS_COMPILE

HRESULT AssemblyBinder::BindAssemblyByName(AssemblyNameData* pAssemblyNameData,
    BINDER_SPACE::Assembly** ppAssembly)
{
    _ASSERTE(pAssemblyNameData != nullptr && ppAssembly != nullptr);

    HRESULT hr = S_OK;
    *ppAssembly = nullptr;

    ReleaseHolder<BINDER_SPACE::AssemblyName> pAssemblyName;
    SAFE_NEW(pAssemblyName, BINDER_SPACE::AssemblyName);
    IF_FAIL_GO(pAssemblyName->Init(*pAssemblyNameData));

    hr = BindUsingAssemblyName(pAssemblyName, ppAssembly);

Exit:
    return hr;
}


NativeImage* AssemblyBinder::LoadNativeImage(Module* componentModule, LPCUTF8 nativeImageName)
{
    STANDARD_VM_CONTRACT;

    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    AssemblyBinder* binder = componentModule->GetFile()->GetAssemblyBinder();
    PTR_LoaderAllocator moduleLoaderAllocator = componentModule->GetLoaderAllocator();

    bool isNewNativeImage;
    NativeImage* nativeImage = NativeImage::Open(componentModule, nativeImageName, binder, moduleLoaderAllocator, &isNewNativeImage);

    if (isNewNativeImage && nativeImage != nullptr)
    {
        m_nativeImages.Append(nativeImage);

        for (COUNT_T assemblyIndex = 0; assemblyIndex < m_loadedAssemblies.GetCount(); assemblyIndex++)
        {
            nativeImage->CheckAssemblyMvid(m_loadedAssemblies[assemblyIndex]);
        }
    }

    return nativeImage;
}

void AssemblyBinder::AddLoadedAssembly(Assembly* loadedAssembly)
{
    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    m_loadedAssemblies.Append(loadedAssembly);
    for (COUNT_T nativeImageIndex = 0; nativeImageIndex < m_nativeImages.GetCount(); nativeImageIndex++)
    {
        m_nativeImages[nativeImageIndex]->CheckAssemblyMvid(loadedAssembly);
    }
}

#endif  //DACCESS_COMPILE

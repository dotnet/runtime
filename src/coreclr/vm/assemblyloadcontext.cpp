// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "nativeimage.h"

AssemblyLoadContext::AssemblyLoadContext()
{
}

#ifndef DACCESS_COMPILE

NativeImage *AssemblyLoadContext::LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName)
{
    STANDARD_VM_CONTRACT;

    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    AssemblyLoadContext *loadContext = componentModule->GetFile()->GetAssemblyLoadContext();
    PTR_LoaderAllocator moduleLoaderAllocator = componentModule->GetLoaderAllocator();

    bool isNewNativeImage;
    NativeImage *nativeImage = NativeImage::Open(componentModule, nativeImageName, loadContext, moduleLoaderAllocator, &isNewNativeImage);

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

void AssemblyLoadContext::AddLoadedAssembly(Assembly *loadedAssembly)
{
    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    m_loadedAssemblies.Append(loadedAssembly);
    for (COUNT_T nativeImageIndex = 0; nativeImageIndex < m_nativeImages.GetCount(); nativeImageIndex++)
    {
        m_nativeImages[nativeImageIndex]->CheckAssemblyMvid(loadedAssembly);
    }
}

#endif  //DACCESS_COMPILE

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "nativeimage.h"

AssemblyLoadContext::AssemblyLoadContext()
{
}

HRESULT AssemblyLoadContext::GetBinderID(
    UINT_PTR* pBinderId)
{
    *pBinderId = reinterpret_cast<UINT_PTR>(this);
    return S_OK;
}

#ifndef DACCESS_COMPILE
NativeImage *AssemblyLoadContext::LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName)
{
    STANDARD_VM_CONTRACT;

    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    AssemblyLoadContext *loadContext = componentModule->GetFile()->GetAssemblyLoadContext();
    PTR_LoaderAllocator moduleLoaderAllocator = componentModule->GetLoaderAllocator();

    return NativeImage::Open(componentModule, nativeImageName, loadContext, moduleLoaderAllocator);
}
#endif

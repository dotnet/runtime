// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    SString path = componentModule->GetPath();
    SString::Iterator lastPathSeparatorIter = path.End();
    size_t pathDirLength = 0;
    if (PEAssembly::FindLastPathSeparator(path, lastPathSeparatorIter))
    {
        pathDirLength = (lastPathSeparatorIter - path.Begin()) + 1;
    }

    SString compositeImageFileName(SString::Utf8, nativeImageName);
    SString fullPath;
    fullPath.Set(path, path.Begin(), (COUNT_T)pathDirLength);
    fullPath += compositeImageFileName;

    return NativeImage::Open(fullPath, nativeImageName, loadContext, moduleLoaderAllocator);
}
#endif

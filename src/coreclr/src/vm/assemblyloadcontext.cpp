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

    int nativeImageCount = m_nativeImages.GetCount();
#ifndef FEATURE_CASE_SENSITIVE_FILESYSTEM
    SString nativeImageNameString;
    nativeImageNameString.SetUTF8(nativeImageName);
#endif
    for (int nativeImageIndex = 0; nativeImageIndex < nativeImageCount; nativeImageIndex++)
    {
        NativeImage *nativeImage = m_nativeImages[nativeImageIndex];
        LPCUTF8 existingImageFileName = nativeImage->GetFileName();
#ifdef FEATURE_CASE_SENSITIVE_FILESYSTEM
        bool match = (strcmp(nativeImageName, existingImageFileName) == 0);
#else
        bool match = SString(SString::Utf8Literal, existingImageFileName).EqualsCaseInsensitive(nativeImageNameString);
#endif
        if (match)
        {
            return nativeImage;
        }
    }

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

    AllocMemTracker amTracker;
    NativeImage *nativeImage = NativeImage::Open(fullPath, nativeImageName, moduleLoaderAllocator, &amTracker);
    if (nativeImage != NULL)
    {
        m_nativeImages.Append(nativeImage);
        amTracker.SuppressRelease();
        return nativeImage;
    }
    
    return NULL;
}
#endif

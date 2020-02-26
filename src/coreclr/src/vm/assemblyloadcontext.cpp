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

NativeImage *AssemblyLoadContext::LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName)
{
    STANDARD_VM_CONTRACT;

#ifndef DACCESS_COMPILE
    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    AssemblyLoadContext *loadContext = componentModule->GetFile()->GetAssemblyLoadContext();
    PTR_LoaderAllocator moduleLoaderAllocator = componentModule->GetLoaderAllocator();

    int nativeImageCount = m_nativeImages.GetCount();
    for (int nativeImageIndex = 0; nativeImageIndex < nativeImageCount; nativeImageIndex++)
    {
        NativeImage *nativeImage = m_nativeImages.Get(nativeImageIndex);
        if (nativeImage->Matches(nativeImageName, loadContext))
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

    PTR_PEImage peImage = PEImage::OpenImage(fullPath);
    PTR_PEFile peFile = PEFile::Open(peImage);

    NativeImage *nativeImage = NativeImage::Open(peFile, peImage, nativeImageName, moduleLoaderAllocator);
    if (nativeImage != NULL)
    {
        m_nativeImages.Append(nativeImage);
        return nativeImage;
    }
#endif
    
    return NULL;
}

AssemblyLoadContext::NativeImageList::NativeImageList()
{
}

bool AssemblyLoadContext::NativeImageList::IsEmpty()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    return (m_array.GetCount() == 0);
}

void AssemblyLoadContext::NativeImageList::Clear()
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
        MODE_ANY;
    } CONTRACTL_END;

    m_array.Clear();
}

int32_t AssemblyLoadContext::NativeImageList::GetCount()
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
        MODE_ANY;
    } CONTRACTL_END;

    return m_array.GetCount();
}

#ifndef DACCESS_COMPILE
// Doesn't lock the assembly list (caller has to hold the lock already).
NativeImage* AssemblyLoadContext::NativeImageList::Get(int32_t index)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return (NativeImage *)m_array.Get(index);
}

void AssemblyLoadContext::NativeImageList::Set(int32_t index, NativeImage* nativeImage)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    m_array.Set(index, dac_cast<PTR_VOID>(nativeImage));
}

HRESULT AssemblyLoadContext::NativeImageList::Append(NativeImage* nativeImage)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    return m_array.Append(nativeImage);
}
#endif

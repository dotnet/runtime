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

NativeImage *AssemblyLoadContext::LoadNativeImage(Module *componentModule, LPCUTF8 nativeImageName, int nativeImageNameLength)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM();); }
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    AssemblyLoadContext *loadContext = componentModule->GetFile()->GetAssemblyLoadContext();
    PTR_LoaderAllocator moduleLoaderAllocator = componentModule->GetLoaderAllocator();

    int nativeImageCount = m_nativeImages.GetCount_Unlocked();
    for (int nativeImageIndex = 0; nativeImageIndex < nativeImageCount; nativeImageIndex++)
    {
        NativeImage *nativeImage = m_nativeImages.Get_UnlockedNoReference(nativeImageIndex);
        if (nativeImage->Matches(nativeImageName, nativeImageNameLength, loadContext))
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

    SString compositeImageFileName(SString::Utf8, nativeImageName, nativeImageNameLength);
    SString fullPath;
    fullPath.Set(path, path.Begin(), (COUNT_T)pathDirLength);
    fullPath += compositeImageFileName;

    PTR_PEImage peImage = PEImage::OpenImage(fullPath);
    PTR_PEFile peFile = PEFile::Open(peImage);

    NativeImage *nativeImage = NativeImage::Open(peFile, peImage, nativeImageName, nativeImageNameLength, moduleLoaderAllocator);
    if (nativeImage != NULL)
    {
        m_nativeImages.Append_Unlocked(nativeImage);
        return nativeImage;
    }
#endif
    
    return NULL;
}

AssemblyLoadContext::NativeImageList::NativeImageList()
{
}

bool AssemblyLoadContext::NativeImageList::IsEmpty_Unlocked()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    return (m_array.GetCount() == 0);
}

void AssemblyLoadContext::NativeImageList::Clear_Unlocked()
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS); // Triggers only in MODE_COOPERATIVE (by taking the lock)
        MODE_ANY;
    } CONTRACTL_END;

    m_array.Clear();
}

int32_t AssemblyLoadContext::NativeImageList::GetCount_Unlocked()
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
NativeImage* AssemblyLoadContext::NativeImageList::Get_UnlockedNoReference(int32_t index)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return (NativeImage *)m_array.Get(index);
}

void AssemblyLoadContext::NativeImageList::Set_Unlocked(int32_t index, NativeImage* nativeImage)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    m_array.Set(index, dac_cast<PTR_VOID>(nativeImage));
}

HRESULT AssemblyLoadContext::NativeImageList::Append_Unlocked(NativeImage* nativeImage)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    return m_array.Append(nativeImage);
}
#endif

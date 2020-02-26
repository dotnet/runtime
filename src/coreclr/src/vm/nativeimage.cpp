// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// NativeImage.cpp
//

// --------------------------------------------------------------------------------

#include "common.h"
#include "nativeimage.h"

// --------------------------------------------------------------------------------
// Headers
// --------------------------------------------------------------------------------

#include <shlwapi.h>

AssemblyNameIndex::AssemblyNameIndex(const SString& name, int32_t index)
    : Name(name), Index(index)
{
    Name.Normalize();
}

NativeImage::NativeImage(
    PEFile *pPeFile,
    PEImage *pPeImage,
    READYTORUN_HEADER *pHeader,
    LPCUTF8 nativeImageName,
    LoaderAllocator *pLoaderAllocator,
    AllocMemTracker& amTracker)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();
    m_pLoadContext = pPeFile->GetAssemblyLoadContext();
    m_utf8SimpleName = nativeImageName;
    m_pPeImage = pPeImage;
    m_runEagerFixups = true;
    
    m_pReadyToRunInfo.Assign(new (amTracker.Track(pHeap->AllocMem((S_SIZE_T)sizeof(ReadyToRunInfo))))
        ReadyToRunInfo(/*pModule*/ NULL, pPeImage->GetLoadedLayout(), pHeader, /*compositeImage*/ NULL, &amTracker), /*takeOwnership*/ TRUE);
    m_pComponentAssemblies = m_pReadyToRunInfo->GetCompositeInfo()->FindSection(ReadyToRunSectionType::ComponentAssemblies);
    m_componentAssemblyCount = m_pComponentAssemblies->Size / sizeof(READYTORUN_COMPONENT_ASSEMBLIES_ENTRY);
    
    // Check if the current module's image has native manifest metadata, otherwise the current->GetNativeAssemblyImport() asserts.
    m_pManifestMetadata = pPeImage->GetNativeMDImport();

    HENUMInternal assemblyEnum;
    HRESULT hr = m_pManifestMetadata->EnumAllInit(mdtAssemblyRef, &assemblyEnum);
    mdAssemblyRef assemblyRef;
    int assemblyIndex = 0;
    while (m_pManifestMetadata->EnumNext(&assemblyEnum, &assemblyRef))
    {
        LPCSTR assemblyName;
        hr = m_pManifestMetadata->GetAssemblyRefProps(assemblyRef, NULL, NULL, &assemblyName, NULL, NULL, NULL, NULL);
        m_assemblySimpleNameToIndexMap.Add(new (amTracker.Track(pHeap->AllocMem((S_SIZE_T)sizeof(AssemblyNameIndex))))
            AssemblyNameIndex(SString(SString::Ansi, assemblyName), assemblyIndex));
        assemblyIndex++;
    }
}

bool NativeImage::Matches(LPCUTF8 utf8SimpleName, const AssemblyLoadContext *pLoadContext) const
{
    return pLoadContext == m_pLoadContext && !strcmp(utf8SimpleName, m_utf8SimpleName);
}

bool NativeImage::EagerFixupsNeedToRun()
{
    bool runEagerFixups = m_runEagerFixups;
    m_runEagerFixups = false;
    return runEagerFixups;
}

NativeImage *NativeImage::Open(
    PEFile *pPeFile,
    PEImage *pPeImage,
    LPCUTF8 nativeImageName,
    LoaderAllocator *pLoaderAllocator)
{
    READYTORUN_HEADER *pHeader = pPeImage->GetLoadedLayout()->GetReadyToRunHeader();
    if (pHeader == NULL)
    {
        return NULL;
    }
    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();
    AllocMemTracker amTracker;
    NativeImage *result = new (amTracker.Track(pHeap->AllocMem((S_SIZE_T)sizeof(NativeImage))))
        NativeImage(pPeFile, pPeImage, pHeader, nativeImageName, pLoaderAllocator, amTracker);
    amTracker.SuppressRelease();
    return result;
}

Assembly *NativeImage::LoadComponentAssembly(uint32_t rowid)
{
#ifndef DACCESS_COMPILE
    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(rowid, mdtAssemblyRef), m_pManifestMetadata, NULL);
    return spec.LoadAssembly(FILE_LOADED);
#else
    return nullptr;
#endif
}

PTR_READYTORUN_CORE_HEADER NativeImage::GetComponentAssemblyHeader(const SString& simpleName)
{
#ifndef DACCESS_COMPILE
    simpleName.Normalize();
    const AssemblyNameIndex *assemblyNameIndex = m_assemblySimpleNameToIndexMap.Lookup(simpleName);
    if (assemblyNameIndex != NULL)
    {
        const BYTE *pImageBase = (const BYTE *)m_pPeImage->GetLoadedLayout()->GetBase();
        const READYTORUN_COMPONENT_ASSEMBLIES_ENTRY *componentAssembly =
            (const READYTORUN_COMPONENT_ASSEMBLIES_ENTRY *)&pImageBase[m_pComponentAssemblies->VirtualAddress] + assemblyNameIndex->Index;
        return (PTR_READYTORUN_CORE_HEADER)&pImageBase[componentAssembly->ReadyToRunCoreHeader.VirtualAddress];
    }
#endif
    return NULL;
}

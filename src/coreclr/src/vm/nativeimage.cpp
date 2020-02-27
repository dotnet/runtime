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
    PEImageLayout *pPeImageLayout,
    READYTORUN_HEADER *pHeader,
    LPCUTF8 nativeImageName,
    LoaderAllocator *pLoaderAllocator,
    AllocMemTracker& amTracker)
    : m_eagerFixupsLock(CrstNativeImageEagerFixups)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();
    m_utf8SimpleName = nativeImageName;
    m_pPeImageLayout = pPeImageLayout;
    m_eagerFixupsHaveRun = false;
    
    m_pReadyToRunInfo.Assign(new (amTracker.Track(pHeap->AllocMem((S_SIZE_T)sizeof(ReadyToRunInfo))))
        ReadyToRunInfo(/*pModule*/ NULL, pPeImageLayout, pHeader, /*compositeImage*/ NULL, &amTracker), /*takeOwnership*/ TRUE);
    m_pComponentAssemblies = m_pReadyToRunInfo->FindSection(ReadyToRunSectionType::ComponentAssemblies);
    m_componentAssemblyCount = m_pComponentAssemblies->Size / sizeof(READYTORUN_COMPONENT_ASSEMBLIES_ENTRY);
    
    // Check if the current module's image has native manifest metadata, otherwise the current->GetNativeAssemblyImport() asserts.
    m_pManifestMetadata = LoadManifestMetadata();

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

NativeImage::~NativeImage()
{
    if (m_pManifestMetadata != NULL)
    {
        m_pManifestMetadata->Release();
    }
}

bool NativeImage::Matches(LPCUTF8 utf8SimpleName) const
{
    return !_stricmp(utf8SimpleName, m_utf8SimpleName);
}

NativeImage *NativeImage::Open(
    PEFile *pPeFile,
    PEImageLayout *pPeImageLayout,
    LPCUTF8 nativeImageName,
    LoaderAllocator *pLoaderAllocator)
{
#ifndef DACCESS_COMPILE
    uint32_t headerRVA = pPeImageLayout->GetExport("RTR_HEADER");
    if (headerRVA + sizeof(READYTORUN_HEADER) > pPeImageLayout->GetSize())
    {
        return NULL;
    }
    READYTORUN_HEADER *pHeader = (READYTORUN_HEADER *)((BYTE *)pPeImageLayout->GetBase() + headerRVA);
    AllocMemTracker amTracker;
    NativeImage *result = new NativeImage(pPeFile, pPeImageLayout, pHeader, nativeImageName, pLoaderAllocator, amTracker);
    amTracker.SuppressRelease();
    return result;
#else
    return NULL;
#endif
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
        const BYTE *pImageBase = (const BYTE *)m_pPeImageLayout->GetBase();
        const READYTORUN_COMPONENT_ASSEMBLIES_ENTRY *componentAssembly =
            (const READYTORUN_COMPONENT_ASSEMBLIES_ENTRY *)&pImageBase[m_pComponentAssemblies->VirtualAddress] + assemblyNameIndex->Index;
        return (PTR_READYTORUN_CORE_HEADER)&pImageBase[componentAssembly->ReadyToRunCoreHeader.VirtualAddress];
    }
#endif
    return NULL;
}

IMDInternalImport *NativeImage::LoadManifestMetadata()
{
    IMAGE_DATA_DIRECTORY *pMeta = m_pReadyToRunInfo->FindSection(ReadyToRunSectionType::ManifestMetadata);

    if (pMeta == NULL)
    {
        return NULL;
    }

    IMDInternalImport *pNewImport = NULL;
#ifndef DACCESS_COMPILE
    IfFailThrow(GetMetaDataInternalInterface((BYTE *)m_pPeImageLayout->GetBase() + VAL32(pMeta->VirtualAddress),
                                             VAL32(pMeta->Size),
                                             ofRead,
                                             IID_IMDInternalImport,
                                             (void **) &pNewImport));

#endif
    return pNewImport;
}

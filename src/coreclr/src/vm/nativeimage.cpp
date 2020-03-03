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

BOOL AssemblyNameIndexHashTraits::Equals(LPCUTF8 a, LPCUTF8 b)
{
    WRAPPER_NO_CONTRACT;

    SString s1;
    SString s2;
    s1.SetUTF8(a);
    s2.SetUTF8(b);
    return s1.CompareCaseInsensitive(s2) == 0;
}

AssemblyNameIndexHashTraits::count_t AssemblyNameIndexHashTraits::Hash(LPCUTF8 s)
{
    WRAPPER_NO_CONTRACT;

    SString s1;
    s1.SetUTF8(s);
    return s1.HashCaseInsensitive();
}

NativeImage::NativeImage(
    NewHolder<PEImageLayout>& peImageLayout,
    READYTORUN_HEADER *pHeader,
    LPCUTF8 nativeImageName,
#ifdef TARGET_UNIX
    PALPEFileHolder& loadedFile,
#endif
    LoaderAllocator *pLoaderAllocator,
    AllocMemTracker *pamTracker)
    : m_eagerFixupsLock(CrstNativeImageEagerFixups)
{
    CONTRACTL
    {
        THROWS;
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();
    m_utf8SimpleName = nativeImageName;
    m_peImageLayout.Assign(peImageLayout.Extract());
    m_eagerFixupsHaveRun = false;

#if TARGET_UNIX
    m_loadedFile = loadedFile;
#endif
    
    m_pReadyToRunInfo.Assign(new ReadyToRunInfo(/*pModule*/ NULL, peImageLayout, pHeader, /*compositeImage*/ NULL, pamTracker), /*takeOwnership*/ TRUE);
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
        m_assemblySimpleNameToIndexMap.Add(AssemblyNameIndex(assemblyName, assemblyIndex));
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

#ifndef DACCESS_COMPILE
NativeImage *NativeImage::Open(
    LPCWSTR fullPath,
    LPCUTF8 nativeImageName,
    LoaderAllocator *pLoaderAllocator,
    AllocMemTracker *pamTracker)
{
    NewHolder<PEImageLayout> peLoadedImage = PEImageLayout::LoadNative(fullPath);

    uint32_t headerRVA = peLoadedImage->GetExport("RTR_HEADER");
    if (headerRVA + sizeof(READYTORUN_HEADER) > peLoadedImage->GetSize())
    {
        return NULL;
    }
    READYTORUN_HEADER *pHeader = (READYTORUN_HEADER *)((BYTE *)peLoadedImage->GetBase() + headerRVA);
    return new NativeImage(
        peLoadedImage, pHeader, nativeImageName,
#ifdef TARGET_UNIX
        loadedFile,
#endif
        pLoaderAllocator, pamTracker);
}
#endif

#ifndef DACCESS_COMPILE
Assembly *NativeImage::LoadComponentAssembly(uint32_t rowid)
{
    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(rowid, mdtAssemblyRef), m_pManifestMetadata, NULL);
    return spec.LoadAssembly(FILE_LOADED);
}
#endif

#ifndef DACCESS_COMPILE
PTR_READYTORUN_CORE_HEADER NativeImage::GetComponentAssemblyHeader(LPCUTF8 simpleName)
{
    const AssemblyNameIndex *assemblyNameIndex = m_assemblySimpleNameToIndexMap.LookupPtr(simpleName);
    if (assemblyNameIndex != NULL)
    {
        const BYTE *pImageBase = (const BYTE *)m_peImageLayout->GetBase();
        const READYTORUN_COMPONENT_ASSEMBLIES_ENTRY *componentAssembly =
            (const READYTORUN_COMPONENT_ASSEMBLIES_ENTRY *)&pImageBase[m_pComponentAssemblies->VirtualAddress] + assemblyNameIndex->Index;
        return (PTR_READYTORUN_CORE_HEADER)&pImageBase[componentAssembly->ReadyToRunCoreHeader.VirtualAddress];
    }
    return NULL;
}
#endif

#ifndef DACCESS_COMPILE
IMDInternalImport *NativeImage::LoadManifestMetadata()
{
    IMAGE_DATA_DIRECTORY *pMeta = m_pReadyToRunInfo->FindSection(ReadyToRunSectionType::ManifestMetadata);

    if (pMeta == NULL)
    {
        return NULL;
    }

    IMDInternalImport *pNewImport = NULL;
    IfFailThrow(GetMetaDataInternalInterface((BYTE *)m_peImageLayout->GetBase() + VAL32(pMeta->VirtualAddress),
                                             VAL32(pMeta->Size),
                                             ofRead,
                                             IID_IMDInternalImport,
                                             (void **) &pNewImport));

    return pNewImport;
}
#endif

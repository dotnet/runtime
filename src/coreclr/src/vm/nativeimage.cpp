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

    return SString(SString::Utf8Literal, a).Compare(SString(SString::Utf8Literal, b)) == 0;
}

AssemblyNameIndexHashTraits::count_t AssemblyNameIndexHashTraits::Hash(LPCUTF8 s)
{
    WRAPPER_NO_CONTRACT;

    return SString(SString::Utf8Literal, s).HashCaseInsensitive();
}

NativeImage::NativeImage(PEImageLayout *pImageLayout, LPCUTF8 imageFileName)
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

    m_pImageLayout = pImageLayout;
    m_fileName = imageFileName;
    m_eagerFixupsHaveRun = false;
}

void NativeImage::Initialize(READYTORUN_HEADER *pHeader, LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
{
    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();

    m_pReadyToRunInfo = new ReadyToRunInfo(/*pModule*/ NULL, m_pImageLayout, pHeader, /*compositeImage*/ NULL, pamTracker);
    m_pComponentAssemblies = m_pReadyToRunInfo->FindSection(ReadyToRunSectionType::ComponentAssemblies);
    m_componentAssemblyCount = m_pComponentAssemblies->Size / sizeof(READYTORUN_COMPONENT_ASSEMBLIES_ENTRY);
    
    // Check if the current module's image has native manifest metadata, otherwise the current->GetNativeAssemblyImport() asserts.
    m_pManifestMetadata = LoadManifestMetadata();

    HENUMInternal assemblyEnum;
    HRESULT hr = m_pManifestMetadata->EnumAllInit(mdtAssemblyRef, &assemblyEnum);
    mdAssemblyRef assemblyRef;
    m_manifestAssemblyCount = 0;
    while (m_pManifestMetadata->EnumNext(&assemblyEnum, &assemblyRef))
    {
        LPCSTR assemblyName;
        hr = m_pManifestMetadata->GetAssemblyRefProps(assemblyRef, NULL, NULL, &assemblyName, NULL, NULL, NULL, NULL);
        m_assemblySimpleNameToIndexMap.Add(AssemblyNameIndex(assemblyName, m_manifestAssemblyCount));
        m_manifestAssemblyCount++;
    }
    
    // When a composite image contributes to a larger version bubble, its manifest assembly
    // count may exceed its component assembly count as it may contain references to
    // assemblies outside of the composite image that are part of its version bubble.
    _ASSERTE(m_manifestAssemblyCount >= m_componentAssemblyCount);
    
    S_SIZE_T dwAllocSize = S_SIZE_T(sizeof(PTR_Assembly)) * S_SIZE_T(m_manifestAssemblyCount);

    // Note: Memory allocated on loader heap is zero filled
    m_pNativeMetadataAssemblyRefMap = (PTR_Assembly*)pamTracker->Track(pLoaderAllocator->GetLowFrequencyHeap()->AllocMem(dwAllocSize));
}

NativeImage::~NativeImage()
{
    STANDARD_VM_CONTRACT;

    delete m_pReadyToRunInfo;
    delete m_pImageLayout;

    if (m_pManifestMetadata != NULL)
    {
        m_pManifestMetadata->Release();
    }
}

#ifndef DACCESS_COMPILE
NativeImage *NativeImage::Open(
    LPCWSTR fullPath,
    LPCUTF8 nativeImageFileName,
    LoaderAllocator *pLoaderAllocator,
    AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    NewHolder<PEImageLayout> peLoadedImage = PEImageLayout::LoadNative(fullPath);

    READYTORUN_HEADER *pHeader = (READYTORUN_HEADER *)peLoadedImage->GetExport("RTR_HEADER");
    if (pHeader == NULL)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    if (pHeader->Signature != READYTORUN_SIGNATURE)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    if (pHeader->MajorVersion < MINIMUM_READYTORUN_MAJOR_VERSION || pHeader->MajorVersion > READYTORUN_MAJOR_VERSION)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    NewHolder<NativeImage> image = new NativeImage(peLoadedImage.Extract(), nativeImageFileName);
    image->Initialize(pHeader, pLoaderAllocator, pamTracker);
    return image.Extract();
}
#endif

#ifndef DACCESS_COMPILE
Assembly *NativeImage::LoadManifestAssembly(uint32_t rowid)
{
    STANDARD_VM_CONTRACT;

    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(rowid, mdtAssemblyRef), m_pManifestMetadata, NULL);
    return spec.LoadAssembly(FILE_LOADED);
}
#endif

#ifndef DACCESS_COMPILE
PTR_READYTORUN_CORE_HEADER NativeImage::GetComponentAssemblyHeader(LPCUTF8 simpleName)
{
    STANDARD_VM_CONTRACT;

    const AssemblyNameIndex *assemblyNameIndex = m_assemblySimpleNameToIndexMap.LookupPtr(simpleName);
    if (assemblyNameIndex != NULL)
    {
        const BYTE *pImageBase = (const BYTE *)m_pImageLayout->GetBase();
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
    STANDARD_VM_CONTRACT;

    IMAGE_DATA_DIRECTORY *pMeta = m_pReadyToRunInfo->FindSection(ReadyToRunSectionType::ManifestMetadata);

    if (pMeta == NULL)
    {
        return NULL;
    }

    IMDInternalImport *pNewImport = NULL;
    IfFailThrow(GetMetaDataInternalInterface((BYTE *)m_pImageLayout->GetBase() + VAL32(pMeta->VirtualAddress),
                                             VAL32(pMeta->Size),
                                             ofRead,
                                             IID_IMDInternalImport,
                                             (void **) &pNewImport));

    return pNewImport;
}
#endif

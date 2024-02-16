// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// NativeImage.cpp
//

// --------------------------------------------------------------------------------

#include "common.h"
#include "nativeimage.h"

// --------------------------------------------------------------------------------
// Headers
// --------------------------------------------------------------------------------

BOOL AssemblyNameIndexHashTraits::Equals(LPCUTF8 a, LPCUTF8 b)
{
    WRAPPER_NO_CONTRACT;

    return SString(SString::Utf8Literal, a).CompareCaseInsensitive(SString(SString::Utf8Literal, b)) == 0;
}

AssemblyNameIndexHashTraits::count_t AssemblyNameIndexHashTraits::Hash(LPCUTF8 s)
{
    WRAPPER_NO_CONTRACT;

    return SString(SString::Utf8Literal, s).HashCaseInsensitive();
}

BOOL NativeImageIndexTraits::Equals(LPCUTF8 a, LPCUTF8 b)
{
    WRAPPER_NO_CONTRACT;

    return SString(SString::Utf8Literal, a).CompareCaseInsensitive(SString(SString::Utf8Literal, b)) == 0;
}

NativeImageIndexTraits::count_t NativeImageIndexTraits::Hash(LPCUTF8 a)
{
    WRAPPER_NO_CONTRACT;

    return SString(SString::Utf8Literal, a).HashCaseInsensitive();
}

NativeImage::NativeImage(AssemblyBinder *pAssemblyBinder, PEImageLayout *pImageLayout, LPCUTF8 imageFileName)
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

    m_pAssemblyBinder = pAssemblyBinder;
    m_pImageLayout = pImageLayout;
    m_fileName = imageFileName;
    m_eagerFixupsHaveRun = false;
    m_readyToRunCodeDisabled = false;
}

void NativeImage::Initialize(READYTORUN_HEADER *pHeader, LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
{
    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();

    m_pReadyToRunInfo = new ReadyToRunInfo(/*pModule*/ NULL, pLoaderAllocator, m_pImageLayout, pHeader, this, pamTracker);
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
    Module *componentModule,
    LPCUTF8 nativeImageFileName,
    AssemblyBinder *pAssemblyBinder,
    LoaderAllocator *pLoaderAllocator,
    /* out */ bool *isNewNativeImage)
{
    STANDARD_VM_CONTRACT;

    NativeImage *pExistingImage = AppDomain::GetCurrentDomain()->GetNativeImage(nativeImageFileName);
    if (pExistingImage != nullptr)
    {
        *isNewNativeImage = false;
        if (pExistingImage->GetAssemblyBinder() == pAssemblyBinder)
        {
            return pExistingImage;
        }
        else
        {
            return nullptr;
        }
    }

    SString path{ componentModule->GetPath() };
    SString::Iterator lastPathSeparatorIter = path.End();
    size_t pathDirLength = 0;
    if (PEAssembly::FindLastPathSeparator(path, lastPathSeparatorIter))
    {
        pathDirLength = (lastPathSeparatorIter - path.Begin()) + 1;
    }

    SString compositeImageFileName(SString::Utf8, nativeImageFileName);
    SString fullPath;
    fullPath.Set(path, path.Begin(), (COUNT_T)pathDirLength);
    fullPath.Append(compositeImageFileName);
    LPWSTR searchPathsConfig;
    IfFailThrow(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NativeImageSearchPaths, &searchPathsConfig));

    PEImageLayoutHolder peLoadedImage;

    BundleFileLocation bundleFileLocation = Bundle::ProbeAppBundle(fullPath, /*pathIsBundleRelative */ true);
    if (bundleFileLocation.IsValid())
    {
        // No need to use cache for this PE image.
        // Composite r2r PE image is not a part of anyone's identity.
        // We only need it to obtain the native image, which will be cached at AppDomain level.
        PEImageHolder pImage = PEImage::OpenImage(fullPath, MDInternalImport_NoCache, bundleFileLocation);
        PEImageLayout* loaded = pImage->GetOrCreateLayout(PEImageLayout::LAYOUT_LOADED);
        // We will let pImage instance be freed after exiting this scope, but we will keep the layout,
        // thus the layout needs an AddRef, or it will be gone together with pImage.
        loaded->AddRef();
        peLoadedImage = loaded;
    }

    if (peLoadedImage.IsNull())
    {
        EX_TRY
        {
            peLoadedImage = PEImageLayout::LoadNative(fullPath);
        }
        EX_CATCH
        {
            SString searchPaths(searchPathsConfig);
            SString::CIterator start = searchPaths.Begin();
            while (start != searchPaths.End())
            {
                SString::CIterator end = start;
                if (!searchPaths.Find(end, PATH_SEPARATOR_CHAR_W))
                {
                    end = searchPaths.End();
                }
                fullPath.Set(searchPaths, start, (COUNT_T)(end - start));

                if (end != searchPaths.End())
                {
                    // Skip path separator character
                    ++end;
                }
                start = end;

                if (fullPath.GetCount() == 0)
                {
                    continue;
                }

                fullPath.Append(DIRECTORY_SEPARATOR_CHAR_W);
                fullPath.Append(compositeImageFileName);

                EX_TRY
                {
                    peLoadedImage = PEImageLayout::LoadNative(fullPath);
                    break;
                }
                EX_CATCH
                {
                }
                EX_END_CATCH(SwallowAllExceptions)
            }
        }
        EX_END_CATCH(SwallowAllExceptions)

        if (peLoadedImage.IsNull())
        {
            // Failed to locate the native composite R2R image
#ifdef LOGGING
            SString searchPaths(searchPathsConfig != nullptr ? searchPathsConfig : W("<use DOTNET_NativeImageSearchPaths to set>"));
            LOG((LF_LOADER, LL_ALWAYS, "LOADER: failed to load native image '%s' for component assembly '%s' using search paths: '%s'\n",
                nativeImageFileName,
                path.GetUTF8(),
                searchPaths.GetUTF8()));
#endif // LOGGING
            RaiseFailFastException(nullptr, nullptr, 0);
        }
    }

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
    NewHolder<NativeImage> image = new NativeImage(pAssemblyBinder, peLoadedImage.Extract(), nativeImageFileName);
    AllocMemTracker amTracker;
    image->Initialize(pHeader, pLoaderAllocator, &amTracker);
    pExistingImage = AppDomain::GetCurrentDomain()->SetNativeImage(nativeImageFileName, image);
    if (pExistingImage == nullptr)
    {
        // No pre-existing image, new image has been stored in the map
        *isNewNativeImage = true;
        amTracker.SuppressRelease();
        return image.Extract();
    }
    // Return pre-existing image if it was loaded into the same ALC, null otherwise
    *isNewNativeImage = false;
    if (pExistingImage->GetAssemblyBinder() == pAssemblyBinder)
    {
        return pExistingImage;
    }
    else
    {
        return nullptr;
    }
}
#endif

#ifndef DACCESS_COMPILE
Assembly *NativeImage::LoadManifestAssembly(uint32_t rowid, DomainAssembly *pParentAssembly)
{
    STANDARD_VM_CONTRACT;

    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(rowid, mdtAssemblyRef), m_pManifestMetadata, pParentAssembly);
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

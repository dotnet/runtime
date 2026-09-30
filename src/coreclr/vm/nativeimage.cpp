// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// NativeImage.cpp
//

// --------------------------------------------------------------------------------

#include "common.h"
#include "nativeimage.h"
#include "hostinformation.h"
#ifdef TARGET_WASM
#include "webcildecoder.h"
#endif
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

NativeImage::NativeImage(AssemblyBinder *pAssemblyBinder, ReadyToRunLoadedImage *pImageLayout, LPCUTF8 imageFileName)
    : m_eagerFixupsLock(CrstNativeImageEagerFixups)
{
    CONTRACTL
    {
        THROWS;
        STANDARD_VM_CHECK;
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
    STANDARD_VM_CONTRACT;

    LoaderHeap *pHeap = pLoaderAllocator->GetHighFrequencyHeap();

    m_pReadyToRunInfo = new ReadyToRunInfo(/*pModule*/ NULL, pLoaderAllocator, pHeader, this, m_pImageLayout, pamTracker);
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
namespace
{
    ReadyToRunLoadedImage* OpenR2RFromPE(const SString& componentModulePath, LPCUTF8 nativeImageFileName, READYTORUN_HEADER** header)
    {
        SString path{ componentModulePath };
        SString::Iterator lastPathSeparatorIter = path.End();
        size_t pathDirLength = 0;
        if (path.FindBack(lastPathSeparatorIter, DIRECTORY_SEPARATOR_CHAR_A))
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

        ProbeExtensionResult probeExtensionResult = AssemblyProbeExtension::Probe(fullPath, /*pathIsBundleRelative */ true);
        if (probeExtensionResult.IsValid())
        {
            // No need to use cache for this PE image.
            // Composite r2r PE image is not a part of anyone's identity.
            // We only need it to obtain the native image, which will be cached at AppDomain level.
            PEImageHolder pImage(PEImage::OpenImage(fullPath, MDInternalImport_NoCache, probeExtensionResult));
#ifdef PEIMAGE_FLAT_LAYOUT_ONLY
            PEImageLayout* loaded = pImage->GetOrCreateLayout(PEImageLayout::LAYOUT_FLAT);
#else
            PEImageLayout* loaded = pImage->GetOrCreateLayout(PEImageLayout::LAYOUT_LOADED);
#endif // PEIMAGE_FLAT_LAYOUT_ONLY
            // We will let pImage instance be freed after exiting this scope, but we will keep the layout,
            // thus the layout needs an AddRef, or it will be gone together with pImage.
            loaded->AddRef();
            peLoadedImage = loaded;
        }

        if (peLoadedImage == NULL)
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
                    EX_END_CATCH
                }
            }
            EX_END_CATCH

            if (peLoadedImage == NULL)
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

#ifdef TARGET_WASM
        // On WebAssembly the runtime only loads flat webcil composites, which do not expose a named
        // "RTR_HEADER" export the way PE R2R images do; obtain the R2R header from the decoder instead.
        // PE R2R images (which rely on the export) cannot be loaded on WASM, so the export path is never
        // taken here. A genuinely non-R2R image still fails validation below via the NULL header check.
        if (peLoadedImage->HasReadyToRunHeader())
            *header = peLoadedImage->GetReadyToRunHeader();
#else // TARGET_WASM
        *header = (READYTORUN_HEADER *)peLoadedImage->GetExport("RTR_HEADER");
#endif // TARGET_WASM
        if (*header == NULL)
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }

        ReadyToRunLoadedImage* r2rImg = new ReadyToRunLoadedImage(
            (TADDR)peLoadedImage->GetBase(),
            peLoadedImage->GetVirtualSize(),
            peLoadedImage,
            [](void* img) { delete (PEImageLayout*)img; });
        peLoadedImage.Detach();
        return r2rImg;
    }
}

NativeImage *NativeImage::Open(
    const SString& componentModulePath,
    LPCUTF8 nativeImageFileName,
    AssemblyBinder *pAssemblyBinder,
    LoaderAllocator *pLoaderAllocator,
    bool isPlatformNative)
{
    STANDARD_VM_CONTRACT;

    NativeImage *pExistingImage = AppDomain::GetCurrentDomain()->GetNativeImage(nativeImageFileName);
    if (pExistingImage != nullptr)
    {
        if (pExistingImage->GetAssemblyBinder() == pAssemblyBinder)
        {
            return pExistingImage;
        }
        else
        {
            return nullptr;
        }
    }

    READYTORUN_HEADER *pHeader = nullptr;
    NewHolder<ReadyToRunLoadedImage> loadedImageHolder;
    if (isPlatformNative)
    {
        // Call into the host to load the composite native image
        size_t image_size;
        void* image_base;
        if (HostInformation::GetNativeCodeData(componentModulePath, nativeImageFileName, reinterpret_cast<void**>(&pHeader), &image_size, &image_base))
        {
            loadedImageHolder = new ReadyToRunLoadedImage((TADDR)image_base, (uint32_t)image_size);
        }
        else
        {
#ifdef TARGET_WINDOWS
            // For platform-native on Windows, fall back to runtime loading the PE
            loadedImageHolder = OpenR2RFromPE(componentModulePath, nativeImageFileName, &pHeader);
#else
            // Match failure behaviour for failing to load from PE
#ifdef LOGGING
            SString path { componentModulePath };
            LOG((LF_LOADER, LL_ALWAYS, "LOADER: failed to load platform-native image '%s' for component assembly '%s' using host callback\n",
                nativeImageFileName,
                path.GetUTF8()));
#endif // LOGGING
            RaiseFailFastException(nullptr, nullptr, 0);
#endif
        }
    }
    else
    {
        loadedImageHolder = OpenR2RFromPE(componentModulePath, nativeImageFileName, &pHeader);
    }

    if (pHeader->Signature != READYTORUN_SIGNATURE)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    if (pHeader->MajorVersion < MINIMUM_READYTORUN_MAJOR_VERSION || pHeader->MajorVersion > READYTORUN_MAJOR_VERSION)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

    NewHolder<NativeImage> image = new NativeImage(pAssemblyBinder, loadedImageHolder.Extract(), nativeImageFileName);
    AllocMemTracker amTracker;
    image->Initialize(pHeader, pLoaderAllocator, &amTracker);
    pExistingImage = AppDomain::GetCurrentDomain()->SetNativeImage(nativeImageFileName, image);
    if (pExistingImage == nullptr)
    {
        // No pre-existing image, new image has been stored in the map
        amTracker.SuppressRelease();
        return image.Extract();
    }
    // Return pre-existing image if it was loaded into the same ALC, null otherwise
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

#if defined(TARGET_WASM) && !defined(DACCESS_COMPILE)

// IMAGE_REL_BASED_PTR is the architecture-specific virtual-address reloc (see PEImageLayout::ApplyBaseRelocations).
#ifdef TARGET_64BIT
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_DIR64
#else
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_HIGHLOW
#endif

// A lazily-attached supplemental R2R image is a host-allocated, memory-resident webcil buffer opened via a
// plain ReadyToRunLoadedImage view -- it never passes through PEImageLayout::ApplyBaseRelocations the way the
// eager (startup-loaded) webcil R2R images do. Its wasm function-table indices (and the min-function-table-index
// stored after the RUNTIME_FUNCTION sentinel) are baked base-0 by crossgen and are meant to be relocated by the
// runtime table base where the host actually placed the module's functions (grown into the shared indirect table
// at load time, written into the webcil header's TableBase by getWebcilPayload). Without this relocation the VM
// computes wrong entry-point indices and the first interp->R2R call traps with "null function or function
// signature mismatch". Apply the same relocations here that ApplyBaseRelocations applies to eager webcil images:
// IMAGE_REL_BASED_PTR (+= load delta; preferred base is 0 for webcil so delta == imageBase) and the additive
// IMAGE_REL_BASED_WASM32/64_TABLE (+= tableBase). The buffer is host-owned and writable, so no page protection
// dance is needed, and it is opened exactly once so a single application is correct.
static void ApplyLazySupplementalWebcilRelocations(TADDR imageBase, WebcilDecoder &decoder)
{
    STANDARD_VM_CONTRACT;

    if (!decoder.HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC))
        return;

    const SSIZE_T delta = (SSIZE_T)imageBase; // GetPreferredBase() == NULL for webcil
    const SSIZE_T tableBaseDelta = decoder.GetTableBaseOffset();

    COUNT_T dirSize = 0;
    TADDR dir = decoder.GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_BASERELOC, &dirSize);

    COUNT_T dirPos = 0;
    // WASM pads each reloc block to a 16-byte boundary, so validate the header is fully readable and stop on a
    // zero-sized (padding) block, mirroring PEImageLayout::ApplyBaseRelocations.
    while (dirPos + sizeof(IMAGE_BASE_RELOCATION) <= dirSize)
    {
        PIMAGE_BASE_RELOCATION r = (PIMAGE_BASE_RELOCATION)(dir + dirPos);
        COUNT_T fixupsSize = VAL32(r->SizeOfBlock);
        if (fixupsSize == 0)
            break;

        USHORT *fixups = (USHORT *)(r + 1);
        COUNT_T fixupsCount = (fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) / 2;
        BYTE *pageAddress = (BYTE *)imageBase + VAL32(r->VirtualAddress);

        for (COUNT_T i = 0; i < fixupsCount; i++)
        {
            USHORT fixup = VAL16(fixups[i]);
            BYTE *address = pageAddress + (fixup & 0xfff);
            switch (fixup >> 12)
            {
            case IMAGE_REL_BASED_PTR:
                *(TADDR *)address += delta;
                break;
            case IMAGE_REL_BASED_WASM32_TABLE:
                *(uint32_t *)address += (uint32_t)tableBaseDelta;
                break;
            case IMAGE_REL_BASED_WASM64_TABLE:
                *(uint64_t *)address += (uint64_t)tableBaseDelta;
                break;
            case IMAGE_REL_BASED_ABSOLUTE:
                break;
            default:
                break;
            }
        }
        dirPos += fixupsSize;
    }
}
NativeImage *NativeImage::OpenFromMemory(
    TADDR imageBase,
    uint32_t imageSize,
    LPCUTF8 nativeImageFileName,
    AssemblyBinder *pAssemblyBinder,
    LoaderAllocator *pLoaderAllocator,
    AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    WebcilDecoder decoder;
    decoder.Init((void *)imageBase, (COUNT_T)imageSize);
    if (!decoder.HasReadyToRunHeader())
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

    READYTORUN_HEADER *pHeader = decoder.GetReadyToRunHeader();
    if (pHeader->Signature != READYTORUN_SIGNATURE)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    if (pHeader->MajorVersion < MINIMUM_READYTORUN_MAJOR_VERSION || pHeader->MajorVersion > READYTORUN_MAJOR_VERSION)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

    // Relocate the memory-resident buffer to the runtime table base before any structure reads its baked
    // (base-0) function-table indices. Must run before the ReadyToRunInfo ctor reads m_minFunctionTableIndex.
    ApplyLazySupplementalWebcilRelocations(imageBase, decoder);

    // The payload buffer has process lifetime (allocated by the host loader), so the image layout is
    // a plain view over it with no cleanup callback.
    NewHolder<ReadyToRunLoadedImage> loadedImageHolder = new ReadyToRunLoadedImage(imageBase, imageSize);
    NewHolder<NativeImage> image = new NativeImage(pAssemblyBinder, loadedImageHolder.Extract(), nativeImageFileName);
    image->Initialize(pHeader, pLoaderAllocator, pamTracker);

    // Supplemental images are not registered in the AppDomain native-image-by-name map; they are owned
    // by the module they attach to (see ReadyToRunInfo::AttachSupplemental).
    return image.Extract();
}
#endif // TARGET_WASM && !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
Assembly *NativeImage::LoadManifestAssembly(uint32_t rowid, Assembly *pParentAssembly)
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
    IfFailThrow(GetMDInternalInterface((BYTE *)m_pImageLayout->GetBase() + VAL32(pMeta->VirtualAddress),
                                        VAL32(pMeta->Size),
                                        ofRead,
                                        IID_IMDInternalImport,
                                        (void **) &pNewImport));

    return pNewImport;
}
#endif

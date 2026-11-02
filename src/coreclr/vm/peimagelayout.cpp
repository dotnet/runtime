// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//

#include "common.h"
#include "peimagelayout.h"
#include "peimagelayout.inl"

#if defined(TARGET_WINDOWS)
#include "amsi.h"
#endif

#if defined(CORECLR_EMBEDDED)
extern "C"
{
#include "pal_zlib.h"
}
#endif

static bool AllowR2RForImage(PEImage* pOwner)
{
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
    return false;
#else
    // Allow R2R for files
    if (pOwner->IsFile())
        return true;

    // Allow R2R for externally provided images
    INT64 size;
    if (pOwner->GetExternalData(&size) != NULL && size > 0)
        return true;

    return false;
#endif
}

#ifndef DACCESS_COMPILE
extern BOOL g_useDefaultBaseAddr;

#ifdef TARGET_WASM
// Guards s_relocatedWebcilBases in ApplyBaseRelocations so concurrent loads of the same shared
// host-probed webcil buffer cannot relocate it more than once. Initialized by PEImageLayout::Startup.
static CrstStatic s_webcilRelocationCrst;

/*static*/
void PEImageLayout::Startup()
{
    WRAPPER_NO_CONTRACT;
    s_webcilRelocationCrst.Init(CrstWebcilImageRelocation);
}
#endif // TARGET_WASM

PEImageLayout* PEImageLayout::CreateFromByteArray(PEImage* pOwner, const BYTE* array, COUNT_T size)
{
    STANDARD_VM_CONTRACT;
    return new FlatImageLayout(pOwner, array, size);
}

#ifndef TARGET_UNIX
PEImageLayout* PEImageLayout::CreateFromHMODULE(HMODULE hModule, PEImage* pOwner)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PEImageLayout* pLoadLayout;
    if (GetModuleHandle(NULL) == hModule)
    {
        return new LoadedImageLayout(pOwner, hModule);
    }
    else
    {
        HRESULT loadFailure = S_OK;
        pLoadLayout = new LoadedImageLayout(pOwner, &loadFailure);

        if (pLoadLayout == NULL)
        {
            loadFailure = FAILED(loadFailure) ? loadFailure : COR_E_BADIMAGEFORMAT;
            EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), loadFailure);
        }
    }

    return pLoadLayout;
}
#endif

PEImageLayout* PEImageLayout::LoadConverted(PEImage* pOwner, bool disableMapping)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(!pOwner->HasLoadedLayout());

    ReleaseHolder<FlatImageLayout> pFlat;
    if (pOwner->IsOpened())
    {
        pFlat = (FlatImageLayout*)pOwner->GetFlatLayout();
        pFlat->AddRef();
    }
    else
    {
        pFlat = new FlatImageLayout(pOwner);
    }

    if (pFlat == NULL || !pFlat->CheckILOnlyFormat())
        EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), COR_E_BADIMAGEFORMAT);

    // If the image is R2R with native code (that is, not a component assembly of composite R2R) or has writeable sections,
    // we need to actually load/map it into virtual addresses
    if ((pFlat->HasReadyToRunHeader() && !pFlat->IsComponentAssembly() && AllowR2RForImage(pOwner))
        || pFlat->HasWriteableSections())
    {
        return new ConvertedImageLayout(pFlat, disableMapping);
    }

    // we can use flat layout for this
    return pFlat.Detach();
}

PEImageLayout* PEImageLayout::Load(PEImage* pOwner, HRESULT* loadFailure)
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_WINDOWS
    if (pOwner->IsFile() && !pOwner->IsInBundle())
    {
        PEImageLayoutHolder pAlloc(new LoadedImageLayout(pOwner, loadFailure));
        if (pAlloc->GetBase() != NULL)
            return pAlloc.Detach();

        // For regular PE files always use OS loader on Windows.
        // If a file cannot be loaded, do not try any further.
        // Even if we may be able to load it, we do not want to support such files.
        return NULL;
    }
#endif // TARGET_WINDOWS

    bool disableMapping = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PELoader_DisableMapping);
    return PEImageLayout::LoadConverted(pOwner, disableMapping);
}

PEImageLayout* PEImageLayout::LoadFlat(PEImage* pOwner)
{
    STANDARD_VM_CONTRACT;
    return new FlatImageLayout(pOwner);
}

PEImageLayout *PEImageLayout::LoadNative(LPCWSTR fullPath)
{
    STANDARD_VM_CONTRACT;
#ifdef TARGET_WINDOWS
    return new NativeImageLayout(fullPath);
#else
    PEImageHolder pOwner(PEImage::OpenImage(fullPath, MDInternalImport_NoCache));
    FlatImageLayout* pFlat = static_cast<FlatImageLayout*>(pOwner->GetOrCreateLayout(LAYOUT_FLAT));
    if (!pFlat->CheckNTFormat() || !pFlat->IsNativeMachineFormat())
        EEFileLoadException::Throw(fullPath, COR_E_BADIMAGEFORMAT);

    bool disableMapping = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PELoader_DisableMapping);
    PEImageLayoutHolder pLayout(new ConvertedImageLayout(pFlat, disableMapping));
    pLayout->m_pOwner = nullptr;
    return pLayout.Detach();
#endif // TARGET_WINDOWS
}

static VMToOSInterface::MemoryProtection GetSectionMemoryProtection(const IMAGE_SECTION_HEADER& section)
{
    UINT characteristics = VAL32(section.Characteristics);
    _ASSERTE((characteristics & VAL32(IMAGE_SCN_MEM_READ)) != 0);

    if ((characteristics & VAL32(IMAGE_SCN_MEM_WRITE)) != 0)
    {
        if ((characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0)
        {
            return VMToOSInterface::MemoryProtection::ExecuteReadWrite;
        }
        else
        {
            return VMToOSInterface::MemoryProtection::ReadWrite;
        }
    }
    else
    {
        if ((characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0)
        {
            return VMToOSInterface::MemoryProtection::ExecuteRead;
        }
        else
        {
            return VMToOSInterface::MemoryProtection::ReadOnly;
        }
    }
}

// Whether the given protection grants execute access, used to decide whether a region of
// relocated code needs an instruction cache flush.
static bool IsExecutableProtection(VMToOSInterface::MemoryProtection protection)
{
    switch (protection)
    {
    case VMToOSInterface::MemoryProtection::Execute:
    case VMToOSInterface::MemoryProtection::ExecuteRead:
    case VMToOSInterface::MemoryProtection::ExecuteReadWrite:
        return true;
    default:
        return false;
    }
}

void PEImageLayout::InitDecoders(void* data, COUNT_T size)
{
#ifdef FEATURE_WEBCIL
    if (WebcilDecoder::DetectWebcilFormat(data, size))
    {
        m_format = FORMAT_WEBCIL;
        m_webcilDecoder.Init(data, size);
        if (HasBaseRelocations())
            ApplyBaseRelocations(true);
        m_peDecoder.Init(data, size); // Initialize base/size/flags for cDAC
    }
    else
#endif
    {
        m_format = FORMAT_PE;
        m_peDecoder.Init(data, size);
    }
}

// IMAGE_REL_BASED_PTR is architecture specific reloc of virtual address
#ifdef TARGET_64BIT
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_DIR64
#else // !TARGET_64BIT
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_HIGHLOW
#endif // !TARGET_64BIT

//To force base relocation on Vista (which uses ASLR), unmask IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE
//(0x40) for OptionalHeader.DllCharacteristics
void PEImageLayout::ApplyBaseRelocations(bool relocationMustWriteCopy)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(IsPEFormat() || IsWebcilFormat());

    SetRelocated();

    //
    // Note that this is not a univeral routine for applying relocations. It handles only the subset
    // required by NGen images. Also, it assumes that the image format is valid.
    //

    SSIZE_T delta = (SIZE_T) GetBase() - (SIZE_T) GetPreferredBase();

#ifdef FEATURE_WEBCIL
    SSIZE_T tableBaseDelta = GetTableBaseOffset();
#endif // FEATURE_WEBCIL

#ifdef TARGET_WASM
    // Host-probed webcil R2R images share one in-memory buffer, so the binder can open the same
    // image twice (identity probe + load). WASM table-index relocations are additive
    // (index += tableBase), so relocating the shared buffer twice doubles tableBase and corrupts
    // indirect calls. Track the buffers already relocated and skip re-relocation.
    //
    // The lock is taken before the Contains check and held for the rest of the method so that, when
    // WASM is built with threads (FEATURE_MULTITHREADING), two concurrent loads of the same shared
    // buffer cannot both pass the check and relocate it twice.
    //
    // INTERIM: the intended production fix is to relocate the host-probed buffer once at the point the
    // host hands it to the runtime and skip webcil relocation here entirely, removing this set.
    typedef SetSHash< TADDR,
                      NoRemoveSHashTraits< NonDacAwareSHashTraits< SetSHashTraits<TADDR> > > > RelocatedWebcilSet;
    static RelocatedWebcilSet s_relocatedWebcilBases;
    CrstHolder relocationGuard(&s_webcilRelocationCrst);
    if (IsWebcilFormat())
    {
        const TADDR webcilBase = (TADDR)GetBase();
        if (s_relocatedWebcilBases.Contains(webcilBase))
            return;
        // Record before applying: once we commit to relocating this shared buffer, no other load may
        // additively relocate it again. A failure partway through relocation is unrecoverable for the
        // additive model and fails the load regardless, so there is no "safe" point to record instead.
        s_relocatedWebcilBases.Add(webcilBase);
    }
#endif // TARGET_WASM

    // Nothing to do - image is loaded at preferred base and no table base offset
    if (delta == 0
#ifdef FEATURE_WEBCIL
        && tableBaseDelta == 0
#endif // FEATURE_WEBCIL
        )
    {
        return;
    }

    LOG((LF_LOADER, LL_INFO100, "PEImage: Applying base relocations (preferred: %p, actual: %p)\n",
        GetPreferredBase(), GetBase()));

    COUNT_T dirSize;
    TADDR dir = GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_BASERELOC, &dirSize);

#ifndef TARGET_WASM
    // Minimize protection changes by keeping a whole section unprotected at a time.
    BYTE * pWriteableRegion = NULL;
    SIZE_T cbWriteableRegion = 0;
    bool hasOldProtection = false;
    VMToOSInterface::MemoryProtection oldProtection = VMToOSInterface::MemoryProtection::NoAccess;
#endif // !TARGET_WASM

    BYTE * pFlushRegion = NULL;
    SIZE_T cbFlushRegion = 0;
    // The page size of PE file relocs is always 4096 bytes
    const SIZE_T cbPageSize = 4096;

    COUNT_T dirPos = 0;
#ifdef TARGET_WASM
    // WASM will pad out the reloc size to the next 16 byte boundary, so we need to validate we can safely read the IMAGE_BASE_RELOCATION struct before processing each entry.
    while (dirPos + sizeof(IMAGE_BASE_RELOCATION) <= dirSize)
#else
    while (dirPos < dirSize)
#endif
    {
        PIMAGE_BASE_RELOCATION r = (PIMAGE_BASE_RELOCATION)(dir + dirPos);

        COUNT_T fixupsSize = VAL32(r->SizeOfBlock);

#ifdef TARGET_WASM
        if (fixupsSize == 0)
        {
            // Since WASM will pad the reloc block to the next 16 byte boundary with 0's we need to allow for a SizeOfBlock being zero.
            // This can only happen for the last block in the relocation list, so we can break here instead of continue.
            break;
        }
#endif

        USHORT *fixups = (USHORT *) (r + 1);

        _ASSERTE(fixupsSize > sizeof(IMAGE_BASE_RELOCATION));
        _ASSERTE((fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) % 2 == 0);

        COUNT_T fixupsCount = (fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) / 2;

        _ASSERTE((BYTE *)(fixups + fixupsCount) <= (BYTE *)(dir + dirSize));

        DWORD rva = VAL32(r->VirtualAddress);

        // For webcil (wasm-only) this relies on the flat-mapped invariant PointerToRawData ==
        // VirtualAddress, so GetBase() + rva is the correct address (equivalent to GetRvaData(rva)).
        BYTE * pageAddress = (BYTE *)GetBase() + rva;

#ifndef TARGET_WASM
        // Check whether the page is outside the unprotected region.
        // Webcil data is already writable, so skip memory protection management.
        if (IsPEFormat() && (SIZE_T)(pageAddress - pWriteableRegion) >= cbWriteableRegion)
        {
            // Restore the protection
            if (hasOldProtection)
            {
#if defined(__APPLE__) && defined(HOST_ARM64)
                // Disable writing on Apple Silicon
                if (IsExecutableProtection(oldProtection))
                    VMToOSInterface::JitWriteProtect(false);
#else
                if (!VMToOSInterface::ProtectMemory(pWriteableRegion, cbWriteableRegion, oldProtection))
                    ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
#endif // __APPLE__ && HOST_ARM64
                hasOldProtection = false;
                oldProtection = VMToOSInterface::MemoryProtection::NoAccess;
            }

            USHORT fixup = VAL16(fixups[0]);

            IMAGE_SECTION_HEADER *pSection = RvaToSection(rva + (fixup & 0xfff));
            _ASSERTE(pSection != NULL);

            pWriteableRegion = (BYTE*)GetRvaData(VAL32(pSection->VirtualAddress));
            cbWriteableRegion = VAL32(pSection->SizeOfRawData);

            // Unprotect the section if it is not writable
            if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_WRITE)) == 0))
            {
                VMToOSInterface::MemoryProtection newProtection = relocationMustWriteCopy ?
                    VMToOSInterface::MemoryProtection::WriteCopy : VMToOSInterface::MemoryProtection::ReadWrite;
#if defined(TARGET_UNIX)
                if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0))
                {
#if defined(__APPLE__) && defined(HOST_ARM64)
                    // Enable writing on Apple Silicon
                    VMToOSInterface::JitWriteProtect(true);
#else
                    // On SELinux, we cannot change protection that doesn't have execute access rights
                    // to one that has it, so we need to set the protection to RWX instead of RW
                    newProtection = VMToOSInterface::MemoryProtection::ExecuteReadWrite;
#endif
                }
#endif // TARGET_UNIX
#if defined(__APPLE__) && defined(HOST_ARM64)
                // Writing was already enabled above via VMToOSInterface::JitWriteProtect.
#else
                if (!VMToOSInterface::ProtectMemory(pWriteableRegion, cbWriteableRegion, newProtection))
                    ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
#endif // __APPLE__ && HOST_ARM64
                oldProtection = GetSectionMemoryProtection(*pSection);
                hasOldProtection = true;
            }
        }
#endif // !TARGET_WASM

        BYTE* pEndAddressToFlush = NULL;
        for (COUNT_T fixupIndex = 0; fixupIndex < fixupsCount; fixupIndex++)
        {
            USHORT fixup = VAL16(fixups[fixupIndex]);

            BYTE * address = pageAddress + (fixup & 0xfff);

            switch (fixup>>12)
            {
            case IMAGE_REL_BASED_PTR:
                *(TADDR *)address += delta;
                pEndAddressToFlush = max(pEndAddressToFlush, address + sizeof(TADDR));
                break;

#ifdef TARGET_ARM
            case IMAGE_REL_BASED_THUMB_MOV32:
                PutThumb2Mov32((UINT16 *)address, GetThumb2Mov32((UINT16 *)address) + (INT32)delta);
                pEndAddressToFlush = max(pEndAddressToFlush, address + 8);
                break;
#endif

#ifdef FEATURE_WEBCIL
            case IMAGE_REL_BASED_WASM32_TABLE:
                *(uint32_t *)address += (uint32_t)tableBaseDelta;
                break;

            case IMAGE_REL_BASED_WASM64_TABLE:
                *(uint64_t *)address += (uint64_t)tableBaseDelta;
                break;
#endif // FEATURE_WEBCIL

            case IMAGE_REL_BASED_ABSOLUTE:
                //no adjustment
                break;

            default:
                _ASSERTE(!"Unhandled reloc type!");
            }
        }

#ifndef TARGET_WASM
        if (IsPEFormat())
        {
            if (IsExecutableProtection(oldProtection) && pEndAddressToFlush != NULL)
            {
                // If the current page is not next to the pending region to flush, flush the current pending region and start a new one
                if (pageAddress >= pFlushRegion + cbFlushRegion + cbPageSize || pageAddress < pFlushRegion)
                {
                    if (pFlushRegion != NULL)
                    {
                        ClrFlushInstructionCache(pFlushRegion, cbFlushRegion);
                    }
                    pFlushRegion = pageAddress;
                }

                cbFlushRegion = pEndAddressToFlush - pFlushRegion;
            }
        }
#endif // !TARGET_WASM

        dirPos += fixupsSize;
    }
    _ASSERTE(dirSize == dirPos || !IsPEFormat());

#ifndef TARGET_WASM
    if (hasOldProtection)
    {
#if defined(__APPLE__) && defined(HOST_ARM64)
        // Disable writing on Apple Silicon
        if (IsExecutableProtection(oldProtection))
            VMToOSInterface::JitWriteProtect(false);
#else
        // Restore the protection
        if (!VMToOSInterface::ProtectMemory(pWriteableRegion, cbWriteableRegion, oldProtection))
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
#endif // __APPLE__ && HOST_ARM64
    }
#endif // !TARGET_WASM
#if defined(TARGET_UNIX) && !defined(TARGET_WASM)
    // Relocations are not needed after they have been applied.
    if (dirSize != 0)
    {
        TADDR discardStart = ALIGN_DOWN(dir, minipal_getpagesize());
        SIZE_T discardSize = ALIGN_UP(dir + dirSize, minipal_getpagesize()) - discardStart;
        int error = VMToOSInterface::MarkMemoryAsNotNeeded(reinterpret_cast<void*>(discardStart), discardSize);
        if (error != 0)
            LOG((LF_LOADER, LL_INFO100, "PEImage: Could not mark base relocations as not needed (%d)\n", error));
    }
#endif // TARGET_UNIX && !TARGET_WASM

    if (pFlushRegion != NULL)
    {
        ClrFlushInstructionCache(pFlushRegion, cbFlushRegion);
    }
}

void ConvertedImageLayout::FreeImageParts()
{
    LIMITED_METHOD_CONTRACT;
    for (int i = 0; i < ConvertedImageLayout::MAX_PARTS; i++)
    {
        PEImagePart imagePart = m_imageParts[i];
        if (imagePart.IsEmpty())
            break;

        PEImagePart nextPart = i + 1 < ConvertedImageLayout::MAX_PARTS ? m_imageParts[i + 1] : PEImagePart{};
        m_imageParts[i].Clear();
        // A final untagged pointer marks the end of the reservation, not a mapped view.
        if (nextPart.IsEmpty())
        {
            _ASSERTE(!imagePart.IsAllocated());
            break;
        }

        SIZE_T partSize = reinterpret_cast<SIZE_T>(nextPart.GetAddress()) -
            reinterpret_cast<SIZE_T>(imagePart.GetAddress());
#ifdef TARGET_WASM
        _ASSERTE(imagePart.IsAllocated());
        if (!VMToOSInterface::ReleaseMemoryReservation(imagePart.GetAddress(), partSize))
        {
            LOG((LF_LOADER, LL_INFO100, "PEImage: Could not release image reservation (0x%08x)\n",
                static_cast<unsigned>(VMToOSInterface::GetLastMemoryMappingError())));
        }
#else
        if (imagePart.IsAllocated())
        {
            if (!VMToOSInterface::ReleaseMemoryReservation(imagePart.GetAddress(), partSize))
            {
                LOG((LF_LOADER, LL_INFO100, "PEImage: Could not release image reservation (0x%08x)\n",
                    static_cast<unsigned>(VMToOSInterface::GetLastMemoryMappingError())));
            }
        }
        else
        {
            if (VMToOSInterface::UnmapMemory(imagePart.GetAddress(), partSize) != 0)
            {
                LOG((LF_LOADER, LL_INFO100, "PEImage: Could not unmap image part (0x%08x)\n",
                    static_cast<unsigned>(VMToOSInterface::GetLastMemoryMappingError())));
            }
        }
#endif // TARGET_WASM
    }
}

static void ReleaseImageParts(ConvertedImageLayout* layout)
{
    WRAPPER_NO_CONTRACT;
    layout->FreeImageParts();
}

ConvertedImageLayout::ConvertedImageLayout(FlatImageLayout* source, bool disableMapping)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    _ASSERTE(source->CheckNTFormat());

    m_pOwner = source->m_pOwner;
    m_pExceptionDir = NULL;
    memset(m_imageParts, 0, sizeof(m_imageParts));
    Holder<ConvertedImageLayout*, DoNothing, ReleaseImageParts> imagePartsHolder(this);

    bool relocationMustWriteCopy = false;
    void* loadedImage = NULL;

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening manually mapped stream\n"));

#ifndef TARGET_WASM
    // LoadImageByMappingParts assumes the source has a file mapping handle created/managed by the runtime.
    // This is not the case for non-file images (for example, external data). Only try to load by mapping for files.
    if (!disableMapping && m_pOwner->IsFile())
    {
        loadedImage = source->LoadImageByMappingParts(this->m_imageParts);
        if (loadedImage == NULL)
        {
            FreeImageParts();
        }
#ifdef TARGET_WINDOWS
        else
        {
            relocationMustWriteCopy = true;
        }
#endif // TARGET_WINDOWS
    }
#endif // !TARGET_WASM

    if (loadedImage == NULL)
    {
        loadedImage = source->LoadImageByCopyingParts(this->m_imageParts);
    }

    IfFailThrow(m_peDecoder.Init(loadedImage));

    if (AllowR2RForImage(m_pOwner) && IsNativeMachineFormat())
    {
        // Do base relocation and exception hookup, if necessary.
        // otherwise R2R will be disabled for this image.

        ApplyBaseRelocations(relocationMustWriteCopy);

        // Check if there is a static function table and install it. (Windows only, except x86)
#if !defined(TARGET_UNIX) && !defined(TARGET_X86)
        COUNT_T cbSize = 0;
        PT_RUNTIME_FUNCTION   pExceptionDir = (PT_RUNTIME_FUNCTION)GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_EXCEPTION, &cbSize);
        DWORD tableSize = cbSize / sizeof(T_RUNTIME_FUNCTION);

        if (tableSize != 0)
        {
            // the only native code that we expect here is from R2R images
            _ASSERTE(HasReadyToRunHeader());

            if (!RtlAddFunctionTable(pExceptionDir, tableSize, (DWORD64)this->GetBase()))
                ThrowLastError();

            m_pExceptionDir = pExceptionDir;
        }
#endif //TARGET_X86
    }
    imagePartsHolder.SuppressRelease();
}

ConvertedImageLayout::~ConvertedImageLayout()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FreeImageParts();

#if !defined(TARGET_UNIX) && !defined(TARGET_X86)
    if (m_pExceptionDir)
    {
        RtlDeleteFunctionTable(m_pExceptionDir);
    }
#endif
}

#ifdef TARGET_WINDOWS
LoadedImageLayout::LoadedImageLayout(PEImage* pOwner, HRESULT* loadFailure)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pOwner));
    }
    CONTRACTL_END;

    m_pOwner = pOwner;
    _ASSERTE(!pOwner->IsCompressed());

    _ASSERTE(!pOwner->IsInBundle());
    m_Module = CLRLoadLibraryEx(pOwner->GetPath(), NULL, GetLoadWithAlteredSearchPathFlag());
    if (m_Module == NULL)
    {
        // Fetch the HRESULT upfront before anybody gets a chance to corrupt it
        *loadFailure = HRESULT_FROM_GetLastError();
        return;
    }

    IfFailThrow(m_peDecoder.Init(m_Module, true));

#ifdef LOGGING
    SString ownerPath{ pOwner->GetPath() };
    LOG((LF_LOADER, LL_INFO1000, "PEImage: Opened HMODULE %s\n", ownerPath.GetUTF8()));
#endif // LOGGING

}

LoadedImageLayout::LoadedImageLayout(PEImage* pOwner, HMODULE hModule)
{
    m_pOwner = pOwner;
    m_peDecoder.Init((void*)hModule, /* relocated */ true);
}

LoadedImageLayout::~LoadedImageLayout()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_Module)
        CLRFreeLibrary(m_Module);
}
#endif // TARGET_WINDOWS

FlatImageLayout::FlatImageLayout(PEImage* pOwner)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pOwner));
    }
    CONTRACTL_END;
    m_pOwner = pOwner;

#ifdef LOGGING
    SString ownerPath{ pOwner->GetPath() };
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening flat %s\n", ownerPath.GetUTF8()));
#endif // LOGGING

    INT64 dataSize;
    void* data = pOwner->GetExternalData(&dataSize);
    if (data != nullptr)
    {
        // Image was provided as flat data via external assembly probing.
        // We do not manage the data - just initialize with it directly.
        _ASSERTE(dataSize != 0);
        InitDecoders(data, (COUNT_T)dataSize);
        return;
    }

    VMToOSInterface::MemoryMapHandle hFile = pOwner->GetFileHandle();
    INT64 offset = pOwner->GetOffset();
    INT64 size = pOwner->GetSize();
    uint64_t fileSize;
    if (!VMToOSInterface::GetFileSize(hFile, &fileSize))
        ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
    if (offset < 0 || size < 0 || fileSize < static_cast<UINT64>(offset))
        EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), COR_E_BADIMAGEFORMAT);
    if (size == 0)
        size = static_cast<INT64>(fileSize - offset);
    if (static_cast<UINT64>(size) > fileSize - offset || static_cast<UINT64>(size) > COUNT_T_MAX)
        EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), COR_E_BADIMAGEFORMAT);

    LPVOID addr = 0;

    // It's okay if resource files are length zero
    if (size > 0)
    {
        INT64 uncompressedSize;
        BOOL isCompressed = pOwner->IsCompressed(&uncompressedSize);

#ifdef TARGET_WASM
        m_wasmData = new BYTE[static_cast<COUNT_T>(size)];
        if (!VMToOSInterface::ReadFile(hFile, static_cast<uint64_t>(offset), m_wasmData.GetValue(),
                static_cast<size_t>(size)))
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
        addr = m_wasmData.GetValue();
#else
        VMToOSInterface::MemoryProtection mapProtection = isCompressed
            ? VMToOSInterface::MemoryProtection::ReadOnly
            : VMToOSInterface::MemoryProtection::ExecuteRead;
        m_fileMapping.Assign(VMToOSInterface::CreateMemoryMapping(hFile, 0, mapProtection));
        if (m_fileMapping.GetValue() == VMToOSInterface::InvalidMemoryMapHandle)
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

        // - Windows - MapViewOfFileEx requires offset to be allocation granularity aligned (typically 64KB)
        // - Linux/OSX - mmap requires offset to be page aligned (PAL sets allocation granularity to page size)
        UINT32 alignment = g_SystemInfo.dwAllocationGranularity;
        UINT64 mapBegin = AlignDown((UINT64)offset, alignment);
        UINT64 mapSize = ((UINT64)(offset + size)) - mapBegin;

        _ASSERTE((offset - mapBegin) < alignment);
        _ASSERTE((offset - mapBegin) < mapSize);
        _ASSERTE(mapSize >= (UINT64)size);

        if (mapSize > SIZE_MAX)
            ThrowOutOfMemory();
        void* view = VMToOSInterface::MapMemory(m_fileMapping.GetValue(), mapBegin, nullptr, static_cast<SIZE_T>(mapSize),
            VMToOSInterface::MemoryProtection::ReadOnly, true);
        if (view == nullptr)
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
        m_fileView.Assign(view, static_cast<SIZE_T>(mapSize));
        addr = (LPVOID)((size_t)view + offset - mapBegin);
#endif // TARGET_WASM

        if (isCompressed)
        {
#if defined(CORECLR_EMBEDDED)
            // Decompress into owned storage and release the compressed image storage after success.

            if (uncompressedSize <= 0 || static_cast<UINT64>(uncompressedSize) > COUNT_T_MAX)
                EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), COR_E_BADIMAGEFORMAT);
#ifdef TARGET_WASM
            NewArrayHolder<BYTE> anonView(new BYTE[static_cast<COUNT_T>(uncompressedSize)]);
            void* uncompressed = anonView.GetValue();
#else
            void* uncompressed = VMToOSInterface::MapAnonymousMemory(static_cast<SIZE_T>(uncompressedSize),
                VMToOSInterface::MemoryProtection::ReadWrite);
            if (uncompressed == nullptr)
                ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
            VMToOSInterface::MemoryMappedViewHolder anonView;
            anonView.Assign(uncompressed, static_cast<SIZE_T>(uncompressedSize));
#endif // TARGET_WASM

            //NB: PE cannot be larger than 4GB and we are decompressing a managed assembly, which is a PE image,
            //    thus converting sizes to uint32 is ok.
            PAL_ZStream zStream;
            zStream.nextIn = (uint8_t*)addr;
            zStream.availIn = (uint32_t)size;
            zStream.nextOut = static_cast<uint8_t*>(uncompressed);
            zStream.availOut = (uint32_t)uncompressedSize;

            // we match the compression side here. 15 is the window sise, negative means no zlib header.
            const int Deflate_DefaultWindowBits = -15;
            if (CompressionNative_InflateInit2_(&zStream, Deflate_DefaultWindowBits) != PAL_Z_OK)
                ThrowHR(COR_E_BADIMAGEFORMAT);

            int ret = CompressionNative_Inflate(&zStream, PAL_Z_NOFLUSH);

            // decompression should have consumed the entire input
            // and the entire output budgets
            if ((ret < 0) ||
                !(zStream.availIn == 0 && zStream.availOut == 0))
            {
                CompressionNative_InflateEnd(&zStream);
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }

            CompressionNative_InflateEnd(&zStream);

            addr = uncompressed;
#ifdef TARGET_WASM
            m_wasmData = anonView.Extract();
#else
            m_fileView = std::move(anonView);
            m_fileMapping.Reset();
#endif // TARGET_WASM
            size = uncompressedSize;
#else
            _ASSERTE(!"Failure extracting contents of the application bundle. Compressed files used with a standalone (not singlefile) apphost.");
            ThrowHR(E_FAIL); // we don't have any indication of what kind of failure. Possibly a corrupt image.
#endif
        }
    }

    InitDecoders(addr, (COUNT_T)size);
}

FlatImageLayout::FlatImageLayout(PEImage* pOwner, const BYTE* array, COUNT_T size)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    m_pOwner = pOwner;

    if (size == 0)
    {
        m_peDecoder.Init((void*)array, size);
    }
    else
    {
#ifdef TARGET_WINDOWS
        if (Amsi::IsBlockedByAmsiScan((void*)array, size))
        {
            SString virusHrString;
            GetHRMsg(HRESULT_FROM_WIN32(ERROR_VIRUS_INFECTED), virusHrString);
            ThrowHR(COR_E_BADIMAGEFORMAT, virusHrString);
        }
#endif // TARGET_WINDOWS
#ifdef TARGET_WASM
        m_wasmData = new BYTE[size];
        memcpy(m_wasmData.GetValue(), array, size);
        InitDecoders(m_wasmData.GetValue(), size);
#else
        void* view = VMToOSInterface::MapAnonymousMemory(size, VMToOSInterface::MemoryProtection::ReadWrite,
            /* executableBacking */ true);
        if (view == nullptr)
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
        m_fileView.Assign(view, size);
        memcpy(view, array, size);
        InitDecoders(view, size);
#endif // TARGET_WASM
    }
}

void* FlatImageLayout::LoadImageByCopyingParts(PEImagePart* imageParts) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    S_SIZE_T allocSize(GetVirtualSize());
    allocSize.AlignUp(g_SystemInfo.dwAllocationGranularity);
    if (allocSize.IsOverflow())
        ThrowOutOfMemory();

    void* preferredBase = NULL;
#ifdef FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION
    if (g_useDefaultBaseAddr)
    {
        preferredBase = (void*)GetPreferredBase();
    }
#endif // FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION

    VMToOSInterface::MemoryProtection initialProtection = VMToOSInterface::MemoryProtection::ReadWrite;
#if defined(HOST_OSX) && defined(HOST_ARM64)
    // On Apple Silicon, allocating the image region as plain PAGE_READWRITE and then promoting
    // executable sections to PAGE_EXECUTE_READ via mprotect has been observed to
    // intermittently leave pages non-executable at the kernel level. This manifests as sporadic
    // AccessViolationException crashes.
    //
    // Avoid the unreliable transition - reserve the whole region as PAGE_NOACCESS first, then
    // commit each section directly with its final protection.
    initialProtection = VMToOSInterface::MemoryProtection::NoAccess;
#endif

    void* base = VMToOSInterface::AllocateMemory(preferredBase, allocSize.Value(), initialProtection, true);
    if (base == NULL && preferredBase != NULL)
        base = VMToOSInterface::AllocateMemory(nullptr, allocSize.Value(), initialProtection, true);

    if (base == NULL)
        ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

    // Copying creates one allocation; the next entry records its end for cleanup.
    imageParts[0] = PEImagePart::Allocated(base);
    imageParts[1] = PEImagePart::Mapped(static_cast<BYTE*>(base) + allocSize.Value());

    IMAGE_SECTION_HEADER* sectionStart = IMAGE_FIRST_SECTION(FindNTHeaders());
    IMAGE_SECTION_HEADER* sectionEnd = sectionStart + VAL16(FindNTHeaders()->FileHeader.NumberOfSections);

#if defined(HOST_OSX) && defined(HOST_ARM64)
    _ASSERTE(initialProtection == VMToOSInterface::MemoryProtection::NoAccess);

    DWORD sizeOfHeaders = VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders);

    // Commit and copy headers, then apply read-only protection.
    if (!VMToOSInterface::ProtectMemory(base, sizeOfHeaders, VMToOSInterface::MemoryProtection::ReadWrite))
        ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

    CopyMemory(base, (void*)GetBase(), sizeOfHeaders);

    if (!VMToOSInterface::ProtectMemory(base, sizeOfHeaders, VMToOSInterface::MemoryProtection::ReadOnly))
        ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

    // Commit and copy each section with its desired protection.
    for (IMAGE_SECTION_HEADER* section = sectionStart; section < sectionEnd; section++)
    {
        DWORD virtualSize = VAL32(section->Misc.VirtualSize);
        if (virtualSize == 0)
            continue;

        bool isExec = (section->Characteristics & IMAGE_SCN_MEM_EXECUTE) != 0;
        BYTE* sectionBase = (BYTE*)base + VAL32(section->VirtualAddress);
        DWORD copySize = min(VAL32(section->SizeOfRawData), virtualSize);

        VMToOSInterface::MemoryProtection protection = isExec
            ? VMToOSInterface::MemoryProtection::ExecuteReadWrite : VMToOSInterface::MemoryProtection::ReadWrite;
        if (!VMToOSInterface::ProtectMemory(sectionBase, virtualSize, protection))
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

        if (isExec)
            VMToOSInterface::JitWriteProtect(true);

        CopyMemory(sectionBase, (BYTE*)GetBase() + VAL32(section->PointerToRawData), copySize);

        if (isExec)
        {
            VMToOSInterface::JitWriteProtect(false);
        }
        else if ((section->Characteristics & IMAGE_SCN_MEM_WRITE) == 0)
        {
            if (!VMToOSInterface::ProtectMemory(sectionBase, virtualSize, VMToOSInterface::MemoryProtection::ReadOnly))
                ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
        }
    }
#else
    // We're going to copy everything first, and write protect what we need to later.

    // First, copy headers
    CopyMemory(base, (void*)GetBase(), VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders));

    // Now, copy all sections to appropriate virtual address

    IMAGE_SECTION_HEADER* section = sectionStart;
    while (section < sectionEnd)
    {
        // Raw data may be less than section size if tail is zero, but may be more since VirtualSize is
        // not padded.
        DWORD size = min(VAL32(section->SizeOfRawData), VAL32(section->Misc.VirtualSize));

        CopyMemory((BYTE*)base + VAL32(section->VirtualAddress), (BYTE*)GetBase() + VAL32(section->PointerToRawData), size);

        // Note that our memory is zeroed already, so no need to initialize any tail.

        section++;
    }

#ifndef TARGET_WASM
    // Apply write protection to copied headers
    if (!VMToOSInterface::ProtectMemory(base, VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders),
        VMToOSInterface::MemoryProtection::ReadOnly))
        ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

    // Finally, apply proper protection to copied sections
    for (section = sectionStart; section < sectionEnd; section++)
    {
        // Add appropriate page protection.
        DWORD virtualSize = VAL32(section->Misc.VirtualSize);
        if (virtualSize == 0)
            continue;

        VMToOSInterface::MemoryProtection newProtection = section->Characteristics & IMAGE_SCN_MEM_EXECUTE ?
            VMToOSInterface::MemoryProtection::ExecuteRead :
            section->Characteristics & IMAGE_SCN_MEM_WRITE ?
            VMToOSInterface::MemoryProtection::ReadWrite :
            VMToOSInterface::MemoryProtection::ReadOnly;

        if (!VMToOSInterface::ProtectMemory((BYTE*)base + VAL32(section->VirtualAddress), virtualSize, newProtection))
        {
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
        }
    }
#endif // !TARGET_WASM
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

    return base;
}

#ifndef TARGET_WASM

void* FlatImageLayout::LoadImageByMappingParts(PEImagePart* imageParts) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckNTFormat());
    }
    CONTRACTL_END;

    if (!VMToOSInterface::SupportsPlaceholderMapping() || m_pOwner->IsCompressed())
    {
        return nullptr;
    }

    UINT32 pageSize = static_cast<UINT32>(minipal_getpagesize());
    UINT64 offset = static_cast<UINT64>(m_pOwner->GetOffset());
    SIZE_T offsetInPage = static_cast<SIZE_T>(offset & (pageSize - 1));
    UINT32 sectionAlignment = VAL32(FindNTHeaders()->OptionalHeader.SectionAlignment);
    SIZE_T alignment = max(pageSize, sectionAlignment);
    SIZE_T headerSize = VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders);
    COUNT_T sectionCount = GetNumberOfSections();
    IMAGE_SECTION_HEADER* sections = FindFirstSection();

    // Each section can contribute a hole, a mapped view, and a copied tail. Keep one slot for the end boundary.
    if (static_cast<UINT64>(sectionCount) * 3 + 3 >= ConvertedImageLayout::MAX_PARTS)
        return nullptr;

    S_SIZE_T imageSize(GetVirtualSize());
    imageSize += S_SIZE_T(offsetInPage);
    imageSize.AlignUp(pageSize);
    S_SIZE_T reserveSize(imageSize);
    reserveSize += S_SIZE_T(alignment - pageSize);
    if (imageSize.IsOverflow() || reserveSize.IsOverflow())
        ThrowOutOfMemory();

    S_SIZE_T previousEnd(headerSize);
    previousEnd += S_SIZE_T(offsetInPage);
    previousEnd.AlignUp(pageSize);
    if (previousEnd.IsOverflow() || previousEnd.Value() > imageSize.Value()
        || static_cast<UINT64>(previousEnd.Value()) > static_cast<UINT64>(GetSize()) + offsetInPage)
    {
        return nullptr;
    }

    for (COUNT_T i = 0; i < sectionCount; i++)
    {
        const IMAGE_SECTION_HEADER& section = sections[i];
        SIZE_T rawSize = VAL32(section.SizeOfRawData);
        SIZE_T virtualSize = VAL32(section.Misc.VirtualSize);
        if (virtualSize == 0 && rawSize == 0)
            continue;
        SIZE_T rva = VAL32(section.VirtualAddress);
        SIZE_T rawOffset = VAL32(section.PointerToRawData);
        S_SIZE_T sectionEnd(rva);
        sectionEnd += S_SIZE_T(offsetInPage);
        sectionEnd += S_SIZE_T(rawSize);
        sectionEnd.AlignUp(pageSize);
        if (virtualSize > rawSize || sectionEnd.IsOverflow() || sectionEnd.Value() > imageSize.Value()
            || ((rva - rawOffset) & (pageSize - 1)) != 0
            || ALIGN_DOWN(rva + offsetInPage, pageSize) < previousEnd.Value())
        {
            return nullptr;
        }
        previousEnd = sectionEnd;
    }

    void* usedBaseAddr = nullptr;
#ifdef FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION
    if (g_useDefaultBaseAddr)
        usedBaseAddr = const_cast<void*>(GetPreferredBase());
#endif // FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION

    void* reserved = VMToOSInterface::ReserveMemoryPlaceholder(usedBaseAddr, reserveSize.Value(), true);
    if (reserved == nullptr && usedBaseAddr != nullptr)
        reserved = VMToOSInterface::ReserveMemoryPlaceholder(nullptr, reserveSize.Value(), true);
    if (reserved == nullptr)
        ThrowHR(VMToOSInterface::GetLastMemoryMappingError());

    VMToOSInterface::MemoryReservationHolder remaining;
    remaining.Assign(reserved, reserveSize.Value());
    int imagePartIndex = 0;
    auto takePart = [&](SIZE_T partSize) -> void*
    {
        void* part = remaining.ExtractPrefix(partSize);
        if (part == nullptr)
            ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
        _ASSERTE(imagePartIndex + 1 < ConvertedImageLayout::MAX_PARTS);
        imageParts[imagePartIndex++] = PEImagePart::Allocated(part);
        imageParts[imagePartIndex] = PEImagePart::Mapped(static_cast<BYTE*>(part) + partSize);
        return part;
    };
    auto mapPart = [&](UINT64 fileOffset, SIZE_T partSize, VMToOSInterface::MemoryProtection protection, bool shared) -> bool
    {
        void* part = takePart(partSize);
        if (VMToOSInterface::MapMemory(m_fileMapping.GetValue(), fileOffset, part, partSize, protection, shared) == nullptr)
        {
            HRESULT error = VMToOSInterface::GetLastMemoryMappingError();
            if (!VMToOSInterface::MappingFailureAllowsCopyFallback)
                ThrowHR(error);
            LOG((LF_LOADER, LL_INFO100, "PEImage: Mapping failed (0x%08x), loading by copying\n", static_cast<unsigned>(error)));
            return false;
        }
        imageParts[imagePartIndex - 1] = PEImagePart::Mapped(part);
        return true;
    };

    BYTE* alignedBase = reinterpret_cast<BYTE*>(ALIGN_UP(reinterpret_cast<SIZE_T>(reserved), alignment));
    SIZE_T prefixSize = alignedBase - static_cast<BYTE*>(reserved);
    if (prefixSize != 0)
        takePart(prefixSize);
    BYTE* imageBase = alignedBase + offsetInPage;
    bool shareReadOnly = VMToOSInterface::UseSharedReadOnlyMapping(sectionAlignment);
    SIZE_T headerMapSize = ALIGN_UP(headerSize + offsetInPage, pageSize);
    if (!mapPart(offset - offsetInPage, headerMapSize, VMToOSInterface::MemoryProtection::ReadOnly, shareReadOnly))
        return nullptr;

    for (COUNT_T i = 0; i < sectionCount; i++)
    {
        const IMAGE_SECTION_HEADER& section = sections[i];
        SIZE_T rawSize = VAL32(section.SizeOfRawData);
        if (rawSize == 0)
            continue;
        BYTE* sectionStart = imageBase + VAL32(section.VirtualAddress);
        BYTE* sectionPage = reinterpret_cast<BYTE*>(ALIGN_DOWN(reinterpret_cast<SIZE_T>(sectionStart), pageSize));
        SIZE_T holeSize = sectionPage - static_cast<BYTE*>(remaining.GetValue());
        if (holeSize != 0)
            takePart(holeSize);

        DWORD characteristics = VAL32(section.Characteristics);
        bool executable = (characteristics & IMAGE_SCN_MEM_EXECUTE) != 0;
        bool writable = (characteristics & IMAGE_SCN_MEM_WRITE) != 0;
        VMToOSInterface::MemoryProtection protection = executable
            ? (writable ? VMToOSInterface::MemoryProtection::ExecuteReadWrite : VMToOSInterface::MemoryProtection::ExecuteRead)
            : (writable ? VMToOSInterface::MemoryProtection::ReadWrite : VMToOSInterface::MemoryProtection::ReadOnly);
        UINT64 dataStart = offset + VAL32(section.PointerToRawData);
        UINT64 dataEnd = dataStart + rawSize;
        UINT64 mapStart = AlignDown(dataStart, pageSize);
        UINT64 mapEnd = min(AlignUp(dataEnd, pageSize), AlignDown(offset + GetSize(), pageSize));
        if (mapEnd > mapStart)
        {
            if (!mapPart(mapStart, static_cast<SIZE_T>(mapEnd - mapStart), protection, shareReadOnly && !writable))
                return nullptr;
        }
        else
        {
            mapEnd = mapStart;
        }
        if (mapEnd < dataEnd)
        {
            SIZE_T copySize = static_cast<SIZE_T>(dataEnd - mapEnd);
            SIZE_T allocationSize = ALIGN_UP(copySize, pageSize);
            void* copied = takePart(allocationSize);
            if (!VMToOSInterface::PopulateMemoryPlaceholder(copied, allocationSize,
                static_cast<BYTE*>(m_fileView.GetValue()) +
                    static_cast<SIZE_T>(mapEnd - AlignDown(offset, g_SystemInfo.dwAllocationGranularity)),
                copySize, protection))
            {
                ThrowHR(VMToOSInterface::GetLastMemoryMappingError());
            }
        }
    }

    if (remaining.GetSize() != 0)
        takePart(remaining.GetSize());
    return imageBase;
}

#endif // !TARGET_WASM

#ifdef TARGET_WINDOWS
NativeImageLayout::NativeImageLayout(LPCWSTR fullPath)
{
    PVOID loadedImage = CLRLoadLibraryEx(fullPath, NULL, GetLoadWithAlteredSearchPathFlag());

    if (loadedImage == NULL)
    {
        ThrowLastError();
    }


    m_peDecoder.Init(loadedImage, /* relocated */ true);
}
#endif // TARGET_WINDOWS
#endif // !DACESS_COMPILE

#ifdef DACCESS_COMPILE
void
PEImageLayout::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p PEAssembly\n", dac_cast<TADDR>(this)));
#ifdef FEATURE_WEBCIL
    if (IsWebcilFormat())
    {
        m_webcilDecoder.EnumMemoryRegions(flags, false);
        return;
    }
#endif
    m_peDecoder.EnumMemoryRegions(flags,false);
}
#endif //DACCESS_COMPILE

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

#ifndef DACCESS_COMPILE
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
    if (WszGetModuleHandle(NULL) == hModule)
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
    else if (pOwner->IsFile())
    {
        pFlat = new FlatImageLayout(pOwner);
    }

    if (pFlat == NULL || !pFlat->CheckILOnlyFormat())
        EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), COR_E_BADIMAGEFORMAT);

// TODO: enable on OSX eventually
//       right now we have binaries that will trigger this in a singlefile bundle.
#ifdef TARGET_LINUX
    // we should not see R2R files here on Unix.
    // ConvertedImageLayout may be able to handle them, but the fact that we were unable to
    // load directly implies that MAPMapPEFile could not consume what crossgen produced.
    // that is suspicious, one or another might have a bug.
    _ASSERTE(!pOwner->IsFile() || !pFlat->HasReadyToRunHeader() || disableMapping);
#endif

    // ignore R2R if the image is not a file.
    if ((pFlat->HasReadyToRunHeader() && pOwner->IsFile()) ||
        pFlat->HasWriteableSections())
    {
        return new ConvertedImageLayout(pFlat, disableMapping);
    }

    // we can use flat layout for this
    return pFlat.Extract();
}

PEImageLayout* PEImageLayout::Load(PEImage* pOwner, HRESULT* loadFailure)
{
    STANDARD_VM_CONTRACT;

    bool disableMapping = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PELoader_DisableMapping);

    if (pOwner->IsFile())
    {
        if (!pOwner->IsInBundle()
#if defined(TARGET_UNIX)
            || (pOwner->GetUncompressedSize() == 0)
#endif
            )
        {
#if defined(TARGET_UNIX)
            if (!disableMapping)
#endif
            {

                PEImageLayoutHolder pAlloc(new LoadedImageLayout(pOwner, loadFailure));
                if (pAlloc->GetBase() != NULL)
                    return pAlloc.Extract();

#if TARGET_WINDOWS
                // For regular PE files always use OS loader on Windows.
                // If a file cannot be loaded, do not try any further.
                // Even if we may be able to load it, we do not want to support such files.
                return NULL;
#endif
            }
        }
    }

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
    return new NativeImageLayout(fullPath);
}

#ifdef TARGET_UNIX
DWORD SectionCharacteristicsToPageProtection(UINT characteristics)
{
    _ASSERTE((characteristics & VAL32(IMAGE_SCN_MEM_READ)) != 0);
    DWORD pageProtection;

    if ((characteristics & VAL32(IMAGE_SCN_MEM_WRITE)) != 0)
    {
        if ((characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0)
        {
            pageProtection = PAGE_EXECUTE_READWRITE;
        }
        else
        {
            pageProtection = PAGE_READWRITE;
        }
    }
    else
    {
        if ((characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0)
        {
            pageProtection = PAGE_EXECUTE_READ;
        }
        else
        {
            pageProtection = PAGE_READONLY;
        }
    }

    return pageProtection;
}
#endif // TARGET_UNIX

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

    SetRelocated();

    //
    // Note that this is not a univeral routine for applying relocations. It handles only the subset
    // required by NGen images. Also, it assumes that the image format is valid.
    //

    SSIZE_T delta = (SIZE_T) GetBase() - (SIZE_T) GetPreferredBase();

    // Nothing to do - image is loaded at preferred base
    if (delta == 0)
        return;

    LOG((LF_LOADER, LL_INFO100, "PEImage: Applying base relocations (preferred: %x, actual: %x)\n",
        GetPreferredBase(), GetBase()));

    COUNT_T dirSize;
    TADDR dir = GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_BASERELOC, &dirSize);

    // Minimize number of calls to VirtualProtect by keeping a whole section unprotected at a time.
    BYTE * pWriteableRegion = NULL;
    SIZE_T cbWriteableRegion = 0;
    DWORD dwOldProtection = 0;

    BYTE * pFlushRegion = NULL;
    SIZE_T cbFlushRegion = 0;
    // The page size of PE file relocs is always 4096 bytes
    const SIZE_T cbPageSize = 4096;

    COUNT_T dirPos = 0;
    while (dirPos < dirSize)
    {
        PIMAGE_BASE_RELOCATION r = (PIMAGE_BASE_RELOCATION)(dir + dirPos);

        COUNT_T fixupsSize = VAL32(r->SizeOfBlock);

        USHORT *fixups = (USHORT *) (r + 1);

        _ASSERTE(fixupsSize > sizeof(IMAGE_BASE_RELOCATION));
        _ASSERTE((fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) % 2 == 0);

        COUNT_T fixupsCount = (fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) / 2;

        _ASSERTE((BYTE *)(fixups + fixupsCount) <= (BYTE *)(dir + dirSize));

        DWORD rva = VAL32(r->VirtualAddress);

        BYTE * pageAddress = (BYTE *)GetBase() + rva;

        // Check whether the page is outside the unprotected region
        if ((SIZE_T)(pageAddress - pWriteableRegion) >= cbWriteableRegion)
        {
            // Restore the protection
            if (dwOldProtection != 0)
            {
#if defined(__APPLE__) && defined(HOST_ARM64)
                BOOL bExecRegion = (dwOldProtection & (PAGE_EXECUTE | PAGE_EXECUTE_READ |
                    PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;

                // Disable writing on Apple Silicon
                if (bExecRegion)
                    PAL_JitWriteProtect(false);
#else
                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       dwOldProtection, &dwOldProtection))
                    ThrowLastError();
#endif // __APPLE__ && HOST_ARM64
                dwOldProtection = 0;
            }

            USHORT fixup = VAL16(fixups[0]);

            IMAGE_SECTION_HEADER *pSection = RvaToSection(rva + (fixup & 0xfff));
            PREFIX_ASSUME(pSection != NULL);

            pWriteableRegion = (BYTE*)GetRvaData(VAL32(pSection->VirtualAddress));
            cbWriteableRegion = VAL32(pSection->SizeOfRawData);

            // Unprotect the section if it is not writable
            if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_WRITE)) == 0))
            {
                DWORD dwNewProtection = relocationMustWriteCopy ? PAGE_WRITECOPY : PAGE_READWRITE;
#if defined(TARGET_UNIX)
                if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0))
                {
#if defined(__APPLE__) && defined(HOST_ARM64)
                    // Enable writing on Apple Silicon
                    PAL_JitWriteProtect(true);
#else
                    // On SELinux, we cannot change protection that doesn't have execute access rights
                    // to one that has it, so we need to set the protection to RWX instead of RW
                    dwNewProtection = PAGE_EXECUTE_READWRITE;
#endif
                }
#endif // TARGET_UNIX
#if !(defined(__APPLE__) && defined(HOST_ARM64))
                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       dwNewProtection, &dwOldProtection))
                    ThrowLastError();
#endif // __APPLE__ && HOST_ARM64
#ifdef TARGET_UNIX
                dwOldProtection = SectionCharacteristicsToPageProtection(pSection->Characteristics);
#endif // TARGET_UNIX
            }
        }

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

            case IMAGE_REL_BASED_ABSOLUTE:
                //no adjustment
                break;

            default:
                _ASSERTE(!"Unhandled reloc type!");
            }
        }

        BOOL bExecRegion = (dwOldProtection & (PAGE_EXECUTE | PAGE_EXECUTE_READ |
            PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;

        if (bExecRegion && pEndAddressToFlush != NULL)
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

        dirPos += fixupsSize;
    }
    _ASSERTE(dirSize == dirPos);

    if (dwOldProtection != 0)
    {
#if defined(__APPLE__) && defined(HOST_ARM64)
        BOOL bExecRegion = (dwOldProtection & (PAGE_EXECUTE | PAGE_EXECUTE_READ |
            PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;

        // Disable writing on Apple Silicon
        if (bExecRegion)
            PAL_JitWriteProtect(false);
#else
        // Restore the protection
        if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                               dwOldProtection, &dwOldProtection))
            ThrowLastError();
#endif // __APPLE__ && HOST_ARM64
    }
#ifdef TARGET_UNIX
    PAL_LOADMarkSectionAsNotNeeded((void*)dir);
#endif // TARGET_UNIX

    if (pFlushRegion != NULL)
    {
        ClrFlushInstructionCache(pFlushRegion, cbFlushRegion);
    }
}

static SIZE_T AllocatedPart(PVOID part)
{
    return (SIZE_T)part + 1;
}

static SIZE_T MappedPart(PVOID part)
{
    return (SIZE_T)part;
}

static PVOID PtrFromPart(SIZE_T part)
{
    return (PVOID)(part & ~1);
}

static SIZE_T IsAllocatedPart(SIZE_T part)
{
    return part & 1;
}

void ConvertedImageLayout::FreeImageParts()
{
    for (int i = 0; i < ConvertedImageLayout::MAX_PARTS; i++)
    {
        SIZE_T imagePart = this->m_imageParts[i];
        if (imagePart == 0)
            break;

        // memory projected into placeholders is page-aligned.
        // we are using "+1" to distinguish committed memory from mapped views, so that we know how to free them
        if (IsAllocatedPart(imagePart))
        {
            ClrVirtualFree(PtrFromPart(imagePart), 0, MEM_RELEASE);
        }
        else
        {
            CLRUnmapViewOfFile(PtrFromPart(imagePart));
        }

        this->m_imageParts[i] = NULL;
    }
}

ConvertedImageLayout::ConvertedImageLayout(FlatImageLayout* source, bool disableMapping)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    _ASSERTE(source->CheckILOnlyFormat());

    m_pOwner = source->m_pOwner;
    m_pExceptionDir = NULL;
    memset(m_imageParts, 0, sizeof(m_imageParts));

    bool relocationMustWriteCopy = false;
    void* loadedImage = NULL;

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening manually mapped stream\n"));

#ifdef TARGET_WINDOWS
    if (!disableMapping)
    {
        loadedImage = source->LoadImageByMappingParts(this->m_imageParts);
        if (loadedImage == NULL)
        {
            FreeImageParts();
        }
        else
        {
            relocationMustWriteCopy = true;
        }
    }
#endif //TARGET_WINDOWS

    if (loadedImage == NULL)
    {
        loadedImage = source->LoadImageByCopyingParts(this->m_imageParts);
    }

    IfFailThrow(Init(loadedImage));

    if (m_pOwner->IsFile() && IsNativeMachineFormat() && g_fAllowNativeImages)
    {
        // Do base relocation and exception hookup, if necessary.
        // otherwise R2R will be disabled for this image.

        ApplyBaseRelocations(relocationMustWriteCopy);

        // Check if there is a static function table and install it. (Windows only, except x86)
#if !defined(TARGET_UNIX) && !defined(TARGET_X86)
        COUNT_T cbSize = 0;
        PT_RUNTIME_FUNCTION   pExceptionDir = (PT_RUNTIME_FUNCTION)GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_EXCEPTION, &cbSize);
        DWORD tableSize = cbSize / sizeof(T_RUNTIME_FUNCTION);

        if (pExceptionDir != NULL)
        {
            // the only native code that we expect here is from R2R images
            _ASSERTE(HasReadyToRunHeader());

            if (!RtlAddFunctionTable(pExceptionDir, tableSize, (DWORD64)this->GetBase()))
                ThrowLastError();

            m_pExceptionDir = pExceptionDir;
        }
#endif //TARGET_X86
    }
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

LoadedImageLayout::LoadedImageLayout(PEImage* pOwner, HRESULT* loadFailure)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pOwner));
    }
    CONTRACTL_END;

    m_pOwner = pOwner;
    _ASSERTE(pOwner->GetUncompressedSize() == 0);

#ifndef TARGET_UNIX
    _ASSERTE(!pOwner->IsInBundle());
    m_Module = CLRLoadLibraryEx(pOwner->GetPath(), NULL, GetLoadWithAlteredSearchPathFlag());
    if (m_Module == NULL)
    {
        // Fetch the HRESULT upfront before anybody gets a chance to corrupt it
        *loadFailure = HRESULT_FROM_GetLastError();
        return;
    }

    IfFailThrow(Init(m_Module, true));

    LOG((LF_LOADER, LL_INFO1000, "PEImage: Opened HMODULE %s\n", pOwner->GetPath().GetUTF8()));

#else
    HANDLE hFile = pOwner->GetFileHandle();
    INT64 offset = pOwner->GetOffset();

    m_LoadedFile = PAL_LOADLoadPEFile(hFile, offset);
    if (m_LoadedFile == NULL)
    {
        // Fetch the HRESULT upfront before anybody gets a chance to corrupt it
        *loadFailure = HRESULT_FROM_GetLastError();
        return;
    }

    LOG((LF_LOADER, LL_INFO1000, "PEImage: image %s (hFile %p) mapped @ %p\n",
        pOwner->GetPath().GetUTF8(), hFile, (void*)m_LoadedFile));

    IfFailThrow(Init((void*)m_LoadedFile));

    if (!HasCorHeader())
    {
        *loadFailure = COR_E_BADIMAGEFORMAT;
        Reset();
        return;
    }

    if (HasReadyToRunHeader() && g_fAllowNativeImages)
    {
        //Do base relocation for PE, if necessary.
        if (!IsNativeMachineFormat())
        {
            *loadFailure = COR_E_BADIMAGEFORMAT;
            Reset();
            return;
        }

        // Unix specifies write sharing at map time (i.e. MAP_PRIVATE implies writecopy).
        ApplyBaseRelocations(/* relocationMustWriteCopy*/ false);
        SetRelocated();
    }
#endif
}

#if !defined(TARGET_UNIX)
LoadedImageLayout::LoadedImageLayout(PEImage* pOwner, HMODULE hModule)
{
    m_pOwner = pOwner;
    PEDecoder::Init((void*)hModule, /* relocated */ true);
}
#endif // !TARGET_UNIX

LoadedImageLayout::~LoadedImageLayout()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#if !defined(TARGET_UNIX)
    if (m_Module)
        CLRFreeLibrary(m_Module);
#endif // !TARGET_UNIX
}

FlatImageLayout::FlatImageLayout(PEImage* pOwner)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pOwner));
    }
    CONTRACTL_END;
    m_pOwner=pOwner;

    HANDLE hFile = pOwner->GetFileHandle();
    INT64 offset = pOwner->GetOffset();
    INT64 size = pOwner->GetSize();

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening flat %s\n", pOwner->GetPath().GetUTF8()));

    // If a size is not specified, load the whole file
    if (size == 0)
    {
        size = SafeGetFileSize(hFile, NULL);
        if (size == 0xffffffff && GetLastError() != NOERROR)
        {
            ThrowLastError();
        }
    }

    LPVOID addr = 0;

    // It's okay if resource files are length zero
    if (size > 0)
    {
        INT64 uncompressedSize = pOwner->GetUncompressedSize();

        DWORD mapAccess = PAGE_READONLY;
#if !defined(TARGET_UNIX)
        // to map sections into executable views on Windows the mapping must have EXECUTE permissions
        if (uncompressedSize == 0)
        {
            mapAccess = PAGE_EXECUTE_READ;
        }
#endif
        m_FileMap.Assign(WszCreateFileMapping(hFile, NULL, mapAccess, 0, 0, NULL));
        if (m_FileMap == NULL)
            ThrowLastError();

        // - Windows - MapViewOfFileEx requires offset to be allocation granularity aligned (typically 64KB)
        // - Linux/OSX - mmap requires offset to be page aligned (PAL sets allocation granularity to page size)
        UINT32 alignment = g_SystemInfo.dwAllocationGranularity;
        UINT64 mapBegin = AlignDown((UINT64)offset, alignment);
        UINT64 mapSize = ((UINT64)(offset + size)) - mapBegin;

        _ASSERTE((offset - mapBegin) < alignment);
        _ASSERTE((offset - mapBegin) < mapSize);
        _ASSERTE(mapSize >= (UINT64)size);

        LPVOID view = CLRMapViewOfFile(m_FileMap, FILE_MAP_READ, mapBegin >> 32, (DWORD)mapBegin, (DWORD)mapSize);
        if (view == NULL)
            ThrowLastError();

        m_FileView.Assign(view);
        addr = (LPVOID)((size_t)view + offset - mapBegin);

        if (uncompressedSize > 0)
        {
#if defined(CORECLR_EMBEDDED)
            // The mapping we have just created refers to the region in the bundle that contains compressed data.
            // We will create another anonymous memory-only mapping and uncompress file there.
            // The flat image will refer to the anonymous mapping instead and we will release the original mapping.

            HandleHolder anonMap = WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, uncompressedSize >> 32, (DWORD)uncompressedSize, NULL);
            if (anonMap == NULL)
                ThrowLastError();

            LPVOID anonView = CLRMapViewOfFile(anonMap, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);
            if (anonView == NULL)
                ThrowLastError();

            //NB: PE cannot be larger than 4GB and we are decompressing a managed assembly, which is a PE image,
            //    thus converting sizes to uint32 is ok.
            PAL_ZStream zStream;
            zStream.nextIn = (uint8_t*)addr;
            zStream.availIn = (uint32_t)size;
            zStream.nextOut = (uint8_t*)anonView;
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

            addr = anonView;
            size = uncompressedSize;
            // Replace file handles with the handles to anonymous map. This will release the handles to the original view and map.
            m_FileView.Assign(anonView);
            m_FileMap.Assign(anonMap);

#else
            _ASSERTE(!"Failure extracting contents of the application bundle. Compressed files used with a standalone (not singlefile) apphost.");
            ThrowHR(E_FAIL); // we don't have any indication of what kind of failure. Possibly a corrupt image.
#endif
        }
    }

    Init(addr, (COUNT_T)size);
}

FlatImageLayout::FlatImageLayout(PEImage* pOwner, const BYTE* array, COUNT_T size)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    m_pOwner = pOwner;

    if (size == 0)
    {
        Init((void*)array, size);
    }
    else
    {
        DWORD mapAccess = PAGE_READWRITE;

#if defined(TARGET_WINDOWS)
        if (Amsi::IsBlockedByAmsiScan((void*)array, size))
        {
            // This is required to throw a BadImageFormatException for compatibility, but
            // use the message from ERROR_VIRUS_INFECTED to give better insight on what's wrong
            SString virusHrString;
            GetHRMsg(HRESULT_FROM_WIN32(ERROR_VIRUS_INFECTED), virusHrString);
            ThrowHR(COR_E_BADIMAGEFORMAT, virusHrString);
        }

        // to map sections into executable views on Windows the mapping must have EXECUTE permissions
        mapAccess = PAGE_EXECUTE_READWRITE;

#endif // defined(TARGET_WINDOWS)

        m_FileMap.Assign(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL, mapAccess, 0, size, NULL));
        if (m_FileMap == NULL)
            ThrowLastError();

        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0));
        if (m_FileView == NULL)
            ThrowLastError();

        memcpy(m_FileView, array, size);
        Init((void*)m_FileView, size);
    }
}

void* FlatImageLayout::LoadImageByCopyingParts(SIZE_T* m_imageParts) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    void* preferredBase = NULL;
#ifdef FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION
    if (g_useDefaultBaseAddr)
    {
        preferredBase = (void*)GetPreferredBase();
    }
#endif // FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION

    DWORD allocationType = MEM_RESERVE | MEM_COMMIT;
#ifdef HOST_UNIX
    // Tell PAL to use the executable memory allocator to satisfy this request for virtual memory.
    // This is required on MacOS and otherwise will allow us to place native R2R code close to the
    // coreclr library and thus improve performance by avoiding jump stubs in managed code.
    allocationType |= MEM_RESERVE_EXECUTABLE;
#endif

    COUNT_T allocSize = ALIGN_UP(this->GetVirtualSize(), g_SystemInfo.dwAllocationGranularity);
    LPVOID base = ClrVirtualAlloc(preferredBase, allocSize, allocationType, PAGE_READWRITE);
    if (base == NULL && preferredBase != NULL)
        base = ClrVirtualAlloc(NULL, allocSize, allocationType, PAGE_READWRITE);

    if (base == NULL)
        ThrowLastError();

    // when loading by copying we have only one part to free.
    m_imageParts[0] = AllocatedPart(base);

    // We're going to copy everything first, and write protect what we need to later.

    // First, copy headers
    CopyMemory(base, (void*)GetBase(), VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders));

    // Now, copy all sections to appropriate virtual address

    IMAGE_SECTION_HEADER* sectionStart = IMAGE_FIRST_SECTION(FindNTHeaders());
    IMAGE_SECTION_HEADER* sectionEnd = sectionStart + VAL16(FindNTHeaders()->FileHeader.NumberOfSections);

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

    // Apply write protection to copied headers
    DWORD oldProtection;
    if (!ClrVirtualProtect((void*)base, VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders),
        PAGE_READONLY, &oldProtection))
        ThrowLastError();

    // Finally, apply proper protection to copied sections
    for (section = sectionStart; section < sectionEnd; section++)
    {
        DWORD executableProtection = PAGE_EXECUTE_READ;
#if defined(__APPLE__) && defined(HOST_ARM64)
        executableProtection = PAGE_EXECUTE_READWRITE;
#endif
        // Add appropriate page protection.
        DWORD newProtection = section->Characteristics & IMAGE_SCN_MEM_EXECUTE ?
            executableProtection :
            section->Characteristics & IMAGE_SCN_MEM_WRITE ?
            PAGE_READWRITE :
            PAGE_READONLY;

        if (!ClrVirtualProtect((void*)((BYTE*)base + VAL32(section->VirtualAddress)),
            VAL32(section->Misc.VirtualSize),
            newProtection, &oldProtection))
        {
            ThrowLastError();
        }
    }

    return base;
}

#if TARGET_WINDOWS

// VirtualAlloc2
typedef PVOID(WINAPI* VirtualAlloc2Fn)(
    HANDLE                 Process,
    PVOID                  BaseAddress,
    SIZE_T                 Size,
    ULONG                  AllocationType,
    ULONG                  PageProtection,
    MEM_EXTENDED_PARAMETER* ExtendedParameters,
    ULONG                  ParameterCount);

VirtualAlloc2Fn pVirtualAlloc2 = NULL;

// MapViewOfFile3
typedef PVOID(WINAPI* MapViewOfFile3Fn)(
    HANDLE                 FileMapping,
    HANDLE                 Process,
    PVOID                  BaseAddress,
    ULONG64                Offset,
    SIZE_T                 ViewSize,
    ULONG                  AllocationType,
    ULONG                  PageProtection,
    MEM_EXTENDED_PARAMETER* ExtendedParameters,
    ULONG                  ParameterCount);

MapViewOfFile3Fn pMapViewOfFile3 = NULL;

static bool HavePlaceholderAPI()
{
    const MapViewOfFile3Fn INVALID_ADDRESS_SENTINEL = (MapViewOfFile3Fn)1;
    if (pMapViewOfFile3 == INVALID_ADDRESS_SENTINEL)
    {
        return false;
    }

    if (pMapViewOfFile3 == NULL)
    {
        HMODULE hm = LoadLibraryW(_T("kernelbase.dll"));
        if (hm != NULL)
        {
            pVirtualAlloc2 = (VirtualAlloc2Fn)GetProcAddress(hm, "VirtualAlloc2");
            pMapViewOfFile3 = (MapViewOfFile3Fn)GetProcAddress(hm, "MapViewOfFile3");

            FreeLibrary(hm);
        }

        if (pMapViewOfFile3 == NULL || pVirtualAlloc2 == NULL)
        {
            pMapViewOfFile3 = INVALID_ADDRESS_SENTINEL;
            return false;
        }
    }

    return true;
}

static PVOID AllocPlaceholder(PVOID BaseAddress, SIZE_T Size)
{
    return pVirtualAlloc2(
        ::GetCurrentProcess(),
        BaseAddress,
        Size,
        MEM_RESERVE | MEM_RESERVE_PLACEHOLDER,
        PAGE_NOACCESS,
        NULL,
        0);
}

static PVOID MapIntoPlaceholder(
    HANDLE                 FileMapping,
    ULONG64                FromOffset,
    PVOID                  ToAddress,
    SIZE_T                 ViewSize,
    ULONG                  PageProtection
)
{
    return pMapViewOfFile3(FileMapping, ::GetCurrentProcess(), ToAddress, FromOffset, ViewSize, MEM_REPLACE_PLACEHOLDER, PageProtection, NULL, 0);
}

static PVOID CommitIntoPlaceholder(
    PVOID                  ToAddress,
    SIZE_T                 Size,
    ULONG                  PageProtection
)
{
    return pVirtualAlloc2(::GetCurrentProcess(), ToAddress, Size, MEM_COMMIT | MEM_RESERVE | MEM_REPLACE_PLACEHOLDER, PageProtection, NULL, 0);
}

static PVOID SplitPlaceholder(
    PVOID& placeholderStart,
    PVOID   placeholderEnd,
    SIZE_T  size
)
{
    _ASSERTE((char*)placeholderStart + size <= placeholderEnd);

    if ((char*)placeholderStart + size < placeholderEnd)
    {
        if (!VirtualFree(placeholderStart, size, MEM_RELEASE | MEM_PRESERVE_PLACEHOLDER))
        {
            return NULL;
        }
    }

    PVOID result = placeholderStart;
    placeholderStart = (char*)placeholderStart + size;

    return result;
}

static SIZE_T OffsetWithinPage(SIZE_T addr)
{
    return addr & (GetOsPageSize() - 1);
}

static SIZE_T RoundToPage(SIZE_T size, SIZE_T offset)
{
    size_t result = size + OffsetWithinPage(offset);
    _ASSERTE(result >= size);
    return ROUND_UP_TO_PAGE(result);
}

void* FlatImageLayout::LoadImageByMappingParts(SIZE_T* m_imageParts) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!HavePlaceholderAPI() || m_pOwner->GetUncompressedSize() != 0)
    {
        return NULL;
    }

    _ASSERTE(HasNTHeaders());

    // offset in m_FileMap is nonzero when the data is in a singlefile bundle.
    SIZE_T offset = (SIZE_T)m_pOwner->GetOffset();
    int imagePartIndex = 0;
    PVOID pReserved = NULL;
    PVOID reservedEnd = NULL;
    IMAGE_NT_HEADERS* ntHeader = FindNTHeaders();

    if  ((ntHeader->OptionalHeader.FileAlignment < GetOsPageSize()) &&
         (ntHeader->OptionalHeader.FileAlignment != ntHeader->OptionalHeader.SectionAlignment))
    {
        goto UNSUPPORTED;
    }

    if (this->GetSize() < GetOsPageSize() * 2)
    {
        goto UNSUPPORTED;
    }

    SIZE_T preferredBase = ntHeader->OptionalHeader.ImageBase;
    SIZE_T virtualSize = ntHeader->OptionalHeader.SizeOfImage;
    SIZE_T reserveSize = RoundToPage(virtualSize, offset);

    PVOID usedBaseAddr = NULL;
#ifdef FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION
    if (g_useDefaultBaseAddr)
    {
        usedBaseAddr = (PVOID)preferredBase;
    }
#endif // FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION

    pReserved = AllocPlaceholder(usedBaseAddr, reserveSize);
    if (pReserved == NULL)
        goto FAILED;

    reservedEnd = (char*)pReserved + reserveSize;
    IMAGE_DOS_HEADER* loadedHeader = (IMAGE_DOS_HEADER*)((SIZE_T)pReserved + OffsetWithinPage(offset));
    _ASSERTE(OffsetWithinPage(offset) == OffsetWithinPage((SIZE_T)loadedHeader));

    //first, map the PE header to the first page in the image.
    SIZE_T dataStart = offset;
    SIZE_T mapFrom = ROUND_DOWN_TO_PAGE(dataStart);
    SIZE_T dataEnd = dataStart + ntHeader->OptionalHeader.SizeOfHeaders;
    SIZE_T mapEnd  = ROUND_UP_TO_PAGE(dataEnd);
    SIZE_T mapSize = mapEnd - mapFrom;
    PVOID  pMapped = SplitPlaceholder(pReserved, reservedEnd, mapSize);
    if (!pMapped)
        goto FAILED;

    pMapped = MapIntoPlaceholder(m_FileMap, mapFrom, pMapped, mapSize, PAGE_READONLY);
    if (!pMapped)
        goto FAILED;

    m_imageParts[imagePartIndex++] = MappedPart(pMapped);

    //Get pointers to the section headers
    IMAGE_SECTION_HEADER* firstSection = (IMAGE_SECTION_HEADER*)(((SIZE_T)loadedHeader)
        + loadedHeader->e_lfanew
        + offsetof(IMAGE_NT_HEADERS, OptionalHeader)
        + VAL16(ntHeader->FileHeader.SizeOfOptionalHeader));

    unsigned numSections = ntHeader->FileHeader.NumberOfSections;
    // in a worst case we need 2 parts for every section and a header + 1 for unused tail.
    if ((numSections + 1) * 2 + 1 > ConvertedImageLayout::MAX_PARTS)
    {
        // too many sections. we do not expect this and do not want to handle here, but it is not an error.
        _ASSERTE(!"too many sections");
        goto UNSUPPORTED;
    }

    for (unsigned i = 0; i < numSections; ++i)
    {
        //for each section, map the section of the file to the correct virtual offset.
        IMAGE_SECTION_HEADER& currentHeader = firstSection[i];
        SIZE_T sectionBase = (SIZE_T)loadedHeader + currentHeader.VirtualAddress;
        SIZE_T sectionBaseAligned = ROUND_DOWN_TO_PAGE(sectionBase);

        if ((SIZE_T)pReserved > sectionBaseAligned)
        {
            // can't allow section mappings to overlap.
            // this could happen with legacy bundles and sub-page aligned data
            // we can't handle such cases here, but it is not an error.
            _ASSERTE(!"can't allow section mappings to overlap");
            goto UNSUPPORTED;
        }

        // Is there space between the previous section and this one? If so, split an unmapped placeholder to cover it.
        if ((SIZE_T)pReserved < sectionBaseAligned)
        {
            // we can handle a hole between sections, but it may indicate a noncompliant R2R PE.
            _ASSERTE(!"Hole between sections");

            SIZE_T holeSize = (SIZE_T)sectionBaseAligned - (SIZE_T)pReserved;
            _ASSERTE((char*)pReserved + holeSize <= reservedEnd);
            PVOID pHole = SplitPlaceholder(pReserved, reservedEnd, holeSize);
            if (!pHole)
                goto FAILED;

            // can't MEM_RELEASE the unused part yet, since the image must be contiguous. (see AssociateMemoryWithLoaderAllocator)
            m_imageParts[imagePartIndex++] = AllocatedPart(pHole);
        }

        DWORD pageProtection = currentHeader.Characteristics & IMAGE_SCN_MEM_EXECUTE ?
            PAGE_EXECUTE_READ :
            currentHeader.Characteristics & IMAGE_SCN_MEM_WRITE ?
                PAGE_WRITECOPY :
                PAGE_READONLY;

        dataStart = offset + currentHeader.PointerToRawData;
        mapFrom = ROUND_DOWN_TO_PAGE(dataStart);
        dataEnd = dataStart + currentHeader.SizeOfRawData;
        mapEnd  = ROUND_UP_TO_PAGE(dataEnd);

        // the aligned end could extend beyond the end of the file.
        // then map only the aligned chunk that fits, the rest we will copy.
        while (mapEnd > offset + this->GetSize())
        {
            mapEnd -= GetOsPageSize();
        }

        // if we have something to map at page granularity, map it
        if (mapEnd > mapFrom)
        {
            mapSize = mapEnd - mapFrom;
            _ASSERTE((char*)pReserved + mapSize <= reservedEnd);
            pMapped = SplitPlaceholder(pReserved, reservedEnd, mapSize);
            if (!pMapped)
                goto FAILED;

            pMapped = MapIntoPlaceholder(m_FileMap, mapFrom, pMapped, mapSize, pageProtection);
            if (!pMapped)
                goto FAILED;

            m_imageParts[imagePartIndex++] = MappedPart(pMapped);
        }

        // if we have something left, copy it
        if (mapEnd < dataEnd)
        {
            SIZE_T toCopy = dataEnd - mapEnd;
            SIZE_T toAllocate = ROUND_UP_TO_PAGE(toCopy);
            _ASSERTE((char*)pReserved + toAllocate <= reservedEnd);
            PVOID pAllocated = SplitPlaceholder(pReserved, reservedEnd, toAllocate);
            if (!pAllocated)
                goto FAILED;

            pAllocated = CommitIntoPlaceholder(pAllocated, toAllocate, PAGE_READWRITE);
            if (!pAllocated)
                goto FAILED;

            m_imageParts[imagePartIndex++] = AllocatedPart(pAllocated);

            CopyMemory(pAllocated, (char*)this->GetBase() + mapEnd - offset, toCopy);
            VirtualProtect(pAllocated, toAllocate, pageProtection, &pageProtection);
        }
    }

    // Is there reserved space that we did not use?
    if (pReserved < reservedEnd)
    {
        // we can handle an image that request extra VM size, but it may indicate a noncompliant R2R PE.
        _ASSERTE(!"Unused part of an image.");
        // can't MEM_RELEASE the unused part yet, since the image must be contiguous. (see AssociateMemoryWithLoaderAllocator)
        m_imageParts[imagePartIndex++] = AllocatedPart(pReserved);
    }

    return loadedHeader;

FAILED:
    _ASSERTE(!"FAILED");
    if (pReserved && pReserved < reservedEnd)
        m_imageParts[imagePartIndex++] = AllocatedPart(pReserved);

    ThrowLastError();

UNSUPPORTED:
    if (pReserved && pReserved < reservedEnd)
        m_imageParts[imagePartIndex++] = AllocatedPart(pReserved);

    return NULL;
}

#endif //TARGET_WINDOWS

NativeImageLayout::NativeImageLayout(LPCWSTR fullPath)
{
    PVOID loadedImage;
#if TARGET_UNIX
    {
        ErrorModeHolder mode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);
        HANDLE fileHandle = WszCreateFile(
            fullPath,
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_DELETE,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL);

        if (fileHandle == INVALID_HANDLE_VALUE)
        {
            ThrowLastError();
        }

        loadedImage = PAL_LOADLoadPEFile(fileHandle, 0);
    }
#else
    loadedImage = CLRLoadLibraryEx(fullPath, NULL, GetLoadWithAlteredSearchPathFlag());
#endif

    if (loadedImage == NULL)
    {
        ThrowLastError();
    }


#if TARGET_UNIX
    PEDecoder::Init(loadedImage, /* relocated */ false);
    // Unix specifies write sharing at map time (i.e. MAP_PRIVATE implies writecopy).
    ApplyBaseRelocations(/* relocationMustWriteCopy*/ false);
#else // TARGET_UNIX
    PEDecoder::Init(loadedImage, /* relocated */ true);
#endif // TARGET_UNIX
}
#endif // !DACESS_COMPILE

#ifdef DACCESS_COMPILE
void
PEImageLayout::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p PEAssembly\n", dac_cast<TADDR>(this)));
    PEDecoder::EnumMemoryRegions(flags,false);
}
#endif //DACCESS_COMPILE

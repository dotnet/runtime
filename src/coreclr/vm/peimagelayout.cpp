// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//

#include "common.h"
#include "peimagelayout.h"
#include "peimagelayout.inl"
#include "dataimage.h"

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

PEImageLayout* PEImageLayout::LoadConverted(PEImage* pOwner)
{
    STANDARD_VM_CONTRACT;

    ReleaseHolder<FlatImageLayout> pFlat(new FlatImageLayout(pOwner));
    if (!pFlat->CheckFormat())
        ThrowHR(COR_E_BADIMAGEFORMAT);

    if (!pFlat->HasNTHeaders() || !pFlat->HasCorHeader() || !pFlat->IsILOnly())
    {
        return NULL;
    }

    bool enableExecution = pFlat->HasReadyToRunHeader() && pFlat->IsNativeMachineFormat() && g_fAllowNativeImages;
    if (!enableExecution)
    {
        return pFlat.Extract();
    }

    return new ConvertedImageLayout(pFlat);
}

PEImageLayout* PEImageLayout::Load(PEImage* pOwner, HRESULT* loadFailure)
{
    STANDARD_VM_CONTRACT;

// TODO: VS HACK HACK

//    if (!pOwner->IsInBundle()
//#if defined(TARGET_UNIX)
//        || (pOwner->GetUncompressedSize() == 0)
//#endif
//        )
//    {
//        PEImageLayoutHolder pAlloc(new LoadedImageLayout(pOwner, loadFailure));
//        if (pAlloc->GetBase() == NULL)
//            return NULL;
//
//        return pAlloc.Extract();
//    }

    return PEImageLayout::LoadConverted(pOwner);
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

//To force base relocation on Vista (which uses ASLR), unmask IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE
//(0x40) for OptionalHeader.DllCharacteristics
void PEImageLayout::ApplyBaseRelocations()
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
                BOOL bExecRegion = (dwOldProtection & (PAGE_EXECUTE | PAGE_EXECUTE_READ |
                    PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;

                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       dwOldProtection, &dwOldProtection))
                    ThrowLastError();

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
                DWORD dwNewProtection = PAGE_READWRITE;
#if defined(TARGET_UNIX)
                if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0))
                {
#ifdef __APPLE__
                    dwNewProtection = PAGE_READWRITE;
#else
                    // On SELinux, we cannot change protection that doesn't have execute access rights
                    // to one that has it, so we need to set the protection to RWX instead of RW
                    dwNewProtection = PAGE_EXECUTE_READWRITE;
#endif
                }
#endif // TARGET_UNIX
                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       dwNewProtection, &dwOldProtection))
                    ThrowLastError();
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
        BOOL bExecRegion = (dwOldProtection & (PAGE_EXECUTE | PAGE_EXECUTE_READ |
            PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0;

        // Restore the protection
        if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                               dwOldProtection, &dwOldProtection))
            ThrowLastError();
    }
#ifdef TARGET_UNIX
    PAL_LOADMarkSectionAsNotNeeded((void*)dir);
#endif // TARGET_UNIX

    if (pFlushRegion != NULL)
    {
        ClrFlushInstructionCache(pFlushRegion, cbFlushRegion);
    }
}

ConvertedImageLayout::ConvertedImageLayout(FlatImageLayout* source)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;
    m_pOwner=source->m_pOwner;
    _ASSERTE(!source->IsMapped());

    m_pExceptionDir = NULL;

    if (!source->HasNTHeaders())
        EEFileLoadException::Throw(source->m_pOwner->GetPathForErrorMessages(), COR_E_BADIMAGEFORMAT);

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening manually mapped stream\n"));

    DWORD mapAccess = PAGE_READWRITE;
    DWORD viewAccess = FILE_MAP_ALL_ACCESS;

#if !defined(TARGET_UNIX)
    // to make sections executable on Windows the view must have EXECUTE permissions
    mapAccess = PAGE_EXECUTE_READWRITE;
    viewAccess = FILE_MAP_EXECUTE | FILE_MAP_WRITE;
#endif

    m_FileMap.Assign(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL,
        mapAccess, 0,
        source->GetVirtualSize(), NULL));

    if (m_FileMap == NULL)
        ThrowLastError();

    m_FileView.Assign(CLRMapViewOfFile(m_FileMap, viewAccess, 0, 0, 0,
                                (void *) source->GetPreferredBase()));
    if (m_FileView == NULL)
        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, viewAccess, 0, 0, 0));

    if (m_FileView == NULL)
        ThrowLastError();

    source->LayoutILOnly(m_FileView);

    IfFailThrow(Init(m_FileView));

    if (!IsNativeMachineFormat())
        ThrowHR(COR_E_BADIMAGEFORMAT);

    // Do base relocation for PE, if necessary.
    // otherwise R2R will be disabled for this image.
    ApplyBaseRelocations();

    // Check if there is a static function table and install it. (Windows only, except x86)
#if !defined(TARGET_UNIX) && !defined(TARGET_X86)
    COUNT_T cbSize = 0;
    PT_RUNTIME_FUNCTION   pExceptionDir = (PT_RUNTIME_FUNCTION)GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_EXCEPTION, &cbSize);
    DWORD tableSize = cbSize / sizeof(T_RUNTIME_FUNCTION);

    if (pExceptionDir != NULL)
    {
        if (!RtlAddFunctionTable(pExceptionDir, tableSize, (DWORD64)this->GetBase()))
            ThrowLastError();

        m_pExceptionDir = pExceptionDir;
    }
#endif //TARGET_X86
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
    LOG((LF_LOADER, LL_INFO1000, "PEImage: Opened HMODULE %S\n", (LPCWSTR)pOwner->GetPath()));

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

    LOG((LF_LOADER, LL_INFO1000, "PEImage: image %S (hFile %p) mapped @ %p\n",
        (LPCWSTR)pOwner->GetPath(), hFile, (void*)m_LoadedFile));

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

        ApplyBaseRelocations();
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

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening flat %S\n", (LPCWSTR) pOwner->GetPath()));

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
        m_FileMap.Assign(WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL));
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

        INT64 uncompressedSize = pOwner->GetUncompressedSize();
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
#if defined(TARGET_WINDOWS)
        if (Amsi::IsBlockedByAmsiScan((void*)array, size))
        {
            // This is required to throw a BadImageFormatException for compatibility, but
            // use the message from ERROR_VIRUS_INFECTED to give better insight on what's wrong
            SString virusHrString;
            GetHRMsg(HRESULT_FROM_WIN32(ERROR_VIRUS_INFECTED), virusHrString);
            ThrowHR(COR_E_BADIMAGEFORMAT, virusHrString);
        }
#endif // defined(TARGET_WINDOWS)

        m_FileMap.Assign(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, size, NULL));
        if (m_FileMap == NULL)
            ThrowLastError();

        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_ALL_ACCESS, 0, 0, 0));
        if (m_FileView == NULL)
            ThrowLastError();

        memcpy(m_FileView, array, size);
        Init((void*)m_FileView, size);
    }
}

void FlatImageLayout::LayoutILOnly(void* base) const
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckZeroedMemory(base, VAL32(FindNTHeaders()->OptionalHeader.SizeOfImage)));
        // Ideally we would require the layout address to honor the section alignment constraints.
        // However, we do have 8K aligned IL only images which we load on 32 bit platforms. In this
        // case, we can only guarantee OS page alignment (which after all, is good enough.)
        PRECONDITION(CheckAligned((SIZE_T)base, GetOsPageSize()));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

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
        // Add appropriate page protection.
        DWORD newProtection = section->Characteristics & IMAGE_SCN_MEM_EXECUTE ?
            PAGE_EXECUTE_READ :
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

    RETURN;
}

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
    ApplyBaseRelocations();
    SetRelocated();
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

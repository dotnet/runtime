// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//

#include "common.h"
#include "peimagelayout.h"
#include "peimagelayout.inl"
#include "dataimage.h"

#if defined(TARGET_WINDOWS) && !defined(CROSSGEN_COMPILE)
#include "amsi.h"
#endif

#if defined(CORECLR_EMBEDDED)
extern "C"
{
#include "pal_zlib.h"
}
#endif

#ifndef DACCESS_COMPILE
PEImageLayout* PEImageLayout::CreateFlat(const void *flat, COUNT_T size,PEImage* pOwner)
{
    STANDARD_VM_CONTRACT;
    return new RawImageLayout(flat,size,pOwner);
}


PEImageLayout* PEImageLayout::CreateFromHMODULE(HMODULE hModule,PEImage* pOwner, BOOL bTakeOwnership)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    return new RawImageLayout(hModule,pOwner,bTakeOwnership,TRUE);
}

PEImageLayout* PEImageLayout::LoadFromFlat(PEImageLayout* pflatimage)
{
    STANDARD_VM_CONTRACT;
    return new ConvertedImageLayout(pflatimage);
}

PEImageLayout* PEImageLayout::LoadConverted(PEImage* pOwner, BOOL isInBundle)
{
    STANDARD_VM_CONTRACT;

    PEImageLayoutHolder pFlat(new FlatImageLayout(pOwner));
    if (!pFlat->CheckFormat())
        ThrowHR(COR_E_BADIMAGEFORMAT);

    return new ConvertedImageLayout(pFlat, isInBundle);
}

PEImageLayout* PEImageLayout::Load(PEImage* pOwner, BOOL bNTSafeLoad, HRESULT* returnDontThrow)
{
    STANDARD_VM_CONTRACT;

#if defined(CROSSGEN_COMPILE) || defined(TARGET_UNIX)
    return PEImageLayout::Map(pOwner);
#else
    if (pOwner->IsInBundle())
    {
        return PEImageLayout::LoadConverted(pOwner, true);
    }

    PEImageLayoutHolder pAlloc(new LoadedImageLayout(pOwner,bNTSafeLoad,returnDontThrow));
    if (pAlloc->GetBase()==NULL)
        return NULL;
    return pAlloc.Extract();
#endif
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

PEImageLayout* PEImageLayout::Map(PEImage* pOwner)
{
    CONTRACT(PEImageLayout*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOwner));
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->CheckFormat());
    }
    CONTRACT_END;

    PEImageLayoutHolder pAlloc = pOwner->GetUncompressedSize() != 0 ?
        LoadConverted(pOwner, /* isInBundle */ true):
        new MappedImageLayout(pOwner);

    if (pAlloc->GetBase()==NULL)
    {
        //cross-platform or a bad image
        pAlloc = LoadConverted(pOwner);
    }
    else
    {
        if (!pAlloc->CheckFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    RETURN pAlloc.Extract();
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
#if defined(TARGET_UNIX) && !defined(CROSSGEN_COMPILE)
                if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0))
                {
                    // On SELinux, we cannot change protection that doesn't have execute access rights
                    // to one that has it, so we need to set the protection to RWX instead of RW
                    dwNewProtection = PAGE_EXECUTE_READWRITE;
                }
#endif // TARGET_UNIX && !CROSSGEN_COMPILE
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

#ifndef CROSSGEN_COMPILE
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
#endif // CROSSGEN_COMPILE

    if (pFlushRegion != NULL)
    {
        ClrFlushInstructionCache(pFlushRegion, cbFlushRegion);
    }
}


RawImageLayout::RawImageLayout(const void *flat, COUNT_T size, PEImage* pOwner)
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
    m_pOwner=pOwner;
    m_Layout=LAYOUT_FLAT;

    if (size)
    {
#if defined(TARGET_WINDOWS) && !defined(CROSSGEN_COMPILE)
        if (Amsi::IsBlockedByAmsiScan((void*)flat, size))
        {
            // This is required to throw a BadImageFormatException for compatibility, but
            // use the message from ERROR_VIRUS_INFECTED to give better insight on what's wrong
            SString virusHrString;
            GetHRMsg(HRESULT_FROM_WIN32(ERROR_VIRUS_INFECTED), virusHrString);
            ThrowHR(COR_E_BADIMAGEFORMAT, virusHrString);
        }
#endif // defined(TARGET_WINDOWS) && !defined(CROSSGEN_COMPILE)

        HandleHolder mapping(WszCreateFileMapping(INVALID_HANDLE_VALUE,
                                                  NULL,
                                                  PAGE_READWRITE,
                                                  0,
                                                  size,
                                                  NULL));
        if (mapping==NULL)
            ThrowLastError();
        m_DataCopy.Assign(CLRMapViewOfFile(mapping, FILE_MAP_ALL_ACCESS, 0, 0, 0));
        if(m_DataCopy==NULL)
            ThrowLastError();
        memcpy(m_DataCopy,flat,size);
        flat=m_DataCopy;
    }
    Init((void*)flat,size);
}
RawImageLayout::RawImageLayout(const void *mapped, PEImage* pOwner, BOOL bTakeOwnership, BOOL bFixedUp)
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
    m_pOwner=pOwner;
    m_Layout=LAYOUT_MAPPED;

    if (bTakeOwnership)
    {
#ifndef TARGET_UNIX
        PathString wszDllName;
        WszGetModuleFileName((HMODULE)mapped, wszDllName);

        m_LibraryHolder=CLRLoadLibraryEx(wszDllName,NULL,GetLoadWithAlteredSearchPathFlag());
#else // !TARGET_UNIX
        _ASSERTE(!"bTakeOwnership Should not be used on TARGET_UNIX");
#endif // !TARGET_UNIX
    }

    IfFailThrow(Init((void*)mapped,(bool)(bFixedUp!=FALSE)));
}

ConvertedImageLayout::ConvertedImageLayout(PEImageLayout* source, BOOL isInBundle)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;
    m_Layout=LAYOUT_LOADED;
    m_pOwner=source->m_pOwner;
    _ASSERTE(!source->IsMapped());

    m_pExceptionDir = NULL;

    if (!source->HasNTHeaders())
        EEFileLoadException::Throw(GetPath(), COR_E_BADIMAGEFORMAT);
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening manually mapped stream\n"));

    // in bundle we may want to enable execution if the image contains R2R sections
    // so must ensure the mapping is compatible with that
    bool enableExecution = isInBundle &&
        source->HasCorHeader() &&
        (source->HasNativeHeader() || source->HasReadyToRunHeader()) &&
        g_fAllowNativeImages;
    
    DWORD mapAccess = PAGE_READWRITE;
    DWORD viewAccess = FILE_MAP_ALL_ACCESS;

#if !defined(CROSSGEN_COMPILE) && !defined(TARGET_UNIX)
    if (enableExecution)
    {
        // to make sections executable on Windows the view must have EXECUTE permissions
        mapAccess = PAGE_EXECUTE_READWRITE;
        viewAccess = FILE_MAP_EXECUTE | FILE_MAP_WRITE;
    }
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

    source->LayoutILOnly(m_FileView, enableExecution);
    IfFailThrow(Init(m_FileView));

#if defined(CROSSGEN_COMPILE)
    if (HasNativeHeader())
    {
        ApplyBaseRelocations();
    }

#else
    if (enableExecution)
    {
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
#endif
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

#if !defined(CROSSGEN_COMPILE) && !defined(TARGET_UNIX) && !defined(TARGET_X86)
    if (m_pExceptionDir)
    {
        RtlDeleteFunctionTable(m_pExceptionDir);
    }
#endif
}

MappedImageLayout::MappedImageLayout(PEImage* pOwner)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;
    m_Layout=LAYOUT_MAPPED;
    m_pOwner=pOwner;

    HANDLE hFile = pOwner->GetFileHandle();
    INT64 offset = pOwner->GetOffset();
    _ASSERTE(pOwner->GetUncompressedSize() == 0);

    // If mapping was requested, try to do SEC_IMAGE mapping
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening OS mapped %S (hFile %p)\n", (LPCWSTR) GetPath(), hFile));

#ifndef TARGET_UNIX
    _ASSERTE(!pOwner->IsInBundle());

    // Let OS map file for us

    // This may fail on e.g. cross-platform (32/64) loads.
    m_FileMap.Assign(WszCreateFileMapping(hFile, NULL, PAGE_READONLY | SEC_IMAGE, 0, 0, NULL));
    if (m_FileMap == NULL)
    {
#ifndef CROSSGEN_COMPILE

        // Capture last error as it may get reset below.

        DWORD dwLastError = GetLastError();
        // There is no reflection-only load on CoreCLR and so we can always throw an error here.
        // It is important on Windows Phone. All assemblies that we load must have SEC_IMAGE set
        // so that the OS can perform signature verification.
        if (pOwner->IsFile())
        {
            EEFileLoadException::Throw(pOwner->GetPathForErrorMessages(), HRESULT_FROM_WIN32(dwLastError));
        }
        else
        {
            // Throw generic exception.
            ThrowWin32(dwLastError);
        }

#endif // !CROSSGEN_COMPILE

        return;
    }

#ifdef _DEBUG
    // Force relocs by occuping the preferred base while the actual mapping is performed
    CLRMapViewHolder forceRelocs;
    if (PEDecoder::GetForceRelocs())
    {
        forceRelocs.Assign(CLRMapViewOfFile(m_FileMap, 0, 0, 0, 0));
    }
#endif // _DEBUG

    m_FileView.Assign(CLRMapViewOfFile(m_FileMap, 0, 0, 0, 0));
    if (m_FileView == NULL)
        ThrowLastError();
    IfFailThrow(Init((void *) m_FileView));

#ifdef CROSSGEN_COMPILE
    //Do base relocation for PE. Unlike LoadLibrary, MapViewOfFile will not do that for us even with SEC_IMAGE
    if (pOwner->IsTrustedNativeImage())
    {
        // This should never happen in correctly setup system, but do a quick check right anyway to
        // avoid running too far with bogus data

        if (!HasCorHeader())
            ThrowHR(COR_E_BADIMAGEFORMAT);

        // For phone, we need to be permissive of MSIL assemblies pretending to be native images,
        // to support forced fall back to JIT
        // if (!HasNativeHeader())
        //     ThrowHR(COR_E_BADIMAGEFORMAT);

        if (HasNativeHeader() && g_fAllowNativeImages)
        {
            if (!IsNativeMachineFormat())
                ThrowHR(COR_E_BADIMAGEFORMAT);

            ApplyBaseRelocations();
        }
    }
    else
#endif // CROSSGEN_COMPILE
    if (!IsNativeMachineFormat() && !IsI386())
    {
        //can't rely on the image
        Reset();
        return;
    }

#ifdef _DEBUG
    if (forceRelocs != NULL)
    {
        forceRelocs.Release();

        if (CheckNTHeaders()) {
            // Reserve the space so nobody can use it. A potential bug is likely to
            // result in a plain AV this way. It is not a good idea to use the original
            // mapping for the reservation since since it would lock the file on the disk.

            // ignore any errors
            ClrVirtualAlloc((void*)GetPreferredBase(), GetVirtualSize(), MEM_RESERVE, PAGE_NOACCESS);
        }
    }
#endif // _DEBUG

#else //!TARGET_UNIX

#ifndef CROSSGEN_COMPILE
    m_LoadedFile = PAL_LOADLoadPEFile(hFile, offset);

    if (m_LoadedFile == NULL)
    {
        // For CoreCLR, try to load all files via LoadLibrary first. If LoadLibrary did not work, retry using
        // regular mapping - but not for native images.
        if (pOwner->IsTrustedNativeImage())
            ThrowHR(E_FAIL); // we don't have any indication of what kind of failure. Possibly a corrupt image.
        return;
    }

    LOG((LF_LOADER, LL_INFO1000, "PEImage: image %S (hFile %p) mapped @ %p\n",
        (LPCWSTR) GetPath(), hFile, (void*)m_LoadedFile));

    IfFailThrow(Init((void *) m_LoadedFile));

    if (!HasCorHeader())
        ThrowHR(COR_E_BADIMAGEFORMAT);

    if ((HasNativeHeader() || HasReadyToRunHeader()) && g_fAllowNativeImages)
    {
        //Do base relocation for PE, if necessary.
        if (!IsNativeMachineFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT);

        ApplyBaseRelocations();
        SetRelocated();
    }

#else // !CROSSGEN_COMPILE
    m_LoadedFile = NULL;
#endif // !CROSSGEN_COMPILE

#endif // !TARGET_UNIX
}

#if !defined(CROSSGEN_COMPILE) && !defined(TARGET_UNIX)
LoadedImageLayout::LoadedImageLayout(PEImage* pOwner, BOOL bNTSafeLoad, HRESULT* returnDontThrow)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pOwner));
    }
    CONTRACTL_END;

    m_Layout=LAYOUT_LOADED;
    m_pOwner=pOwner;

    DWORD dwFlags = GetLoadWithAlteredSearchPathFlag();
    if (bNTSafeLoad)
        dwFlags|=DONT_RESOLVE_DLL_REFERENCES;

    m_Module = CLRLoadLibraryEx(pOwner->GetPath(), NULL, dwFlags);
    if (m_Module == NULL)
    {
        // Fetch the HRESULT upfront before anybody gets a chance to corrupt it
        HRESULT hr = HRESULT_FROM_GetLastError();
        if (returnDontThrow != NULL)
        {
            *returnDontThrow = hr;
            return;
        }

        EEFileLoadException::Throw(pOwner->GetPath(), hr);
    }
    IfFailThrow(Init(m_Module,true));

    LOG((LF_LOADER, LL_INFO1000, "PEImage: Opened HMODULE %S\n", (LPCWSTR) GetPath()));
}
#endif // !CROSSGEN_COMPILE && !TARGET_UNIX

FlatImageLayout::FlatImageLayout(PEImage* pOwner)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pOwner));
    }
    CONTRACTL_END;
    m_Layout=LAYOUT_FLAT;
    m_pOwner=pOwner;

    HANDLE hFile = pOwner->GetFileHandle();
    INT64 offset = pOwner->GetOffset();
    INT64 size = pOwner->GetSize();

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening flat %S\n", (LPCWSTR) GetPath()));

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
    EMEM_OUT(("MEM: %p PEFile\n", dac_cast<TADDR>(this)));
    PEDecoder::EnumMemoryRegions(flags,false);
}
#endif //DACCESS_COMPILE

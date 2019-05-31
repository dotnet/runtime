// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//

#include "common.h"
#include "peimagelayout.h"
#include "peimagelayout.inl"
#include "dataimage.h"

#if defined(PLATFORM_WINDOWS) && !defined(CROSSGEN_COMPILE)
#include "amsi.h"
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

PEImageLayout* PEImageLayout::Load(PEImage* pOwner, BOOL bNTSafeLoad, BOOL bThrowOnError)
{
    STANDARD_VM_CONTRACT;

#if defined(CROSSGEN_COMPILE) || defined(FEATURE_PAL)
    return PEImageLayout::Map(pOwner->GetFileHandle(), pOwner);
#else
    PEImageLayoutHolder pAlloc(new LoadedImageLayout(pOwner,bNTSafeLoad,bThrowOnError));
    if (pAlloc->GetBase()==NULL)
        return NULL;
    return pAlloc.Extract();
#endif
}

PEImageLayout* PEImageLayout::LoadFlat(HANDLE hFile,PEImage* pOwner)
{
    STANDARD_VM_CONTRACT;
    return new FlatImageLayout(hFile,pOwner);
}

PEImageLayout* PEImageLayout::Map(HANDLE hFile, PEImage* pOwner)
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

    PEImageLayoutHolder pAlloc(new MappedImageLayout(hFile,pOwner));
    if (pAlloc->GetBase()==NULL)
    {
        //cross-platform or a bad image
        PEImageLayoutHolder pFlat(new FlatImageLayout(hFile, pOwner));
        if (!pFlat->CheckFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT);

        pAlloc=new ConvertedImageLayout(pFlat);
    }
    else
        if(!pAlloc->CheckFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT);
    RETURN pAlloc.Extract();
}

#ifdef FEATURE_PAL
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
#endif // FEATURE_PAL

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
#if defined(FEATURE_PAL) && !defined(CROSSGEN_COMPILE)
                if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_EXECUTE)) != 0))
                {
                    // On SELinux, we cannot change protection that doesn't have execute access rights
                    // to one that has it, so we need to set the protection to RWX instead of RW
                    dwNewProtection = PAGE_EXECUTE_READWRITE;
                }
#endif // FEATURE_PAL && !CROSSGEN_COMPILE
                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       dwNewProtection, &dwOldProtection))
                    ThrowLastError();
#ifdef FEATURE_PAL
                dwOldProtection = SectionCharacteristicsToPageProtection(pSection->Characteristics);
#endif // FEATURE_PAL
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

#ifdef _TARGET_ARM_
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
#if defined(PLATFORM_WINDOWS) && !defined(CROSSGEN_COMPILE)
        if (Amsi::IsBlockedByAmsiScan((void*)flat, size))
        {
            // This is required to throw a BadImageFormatException for compatibility, but
            // use the message from ERROR_VIRUS_INFECTED to give better insight on what's wrong
            SString virusHrString;
            GetHRMsg(HRESULT_FROM_WIN32(ERROR_VIRUS_INFECTED), virusHrString);
            ThrowHR(COR_E_BADIMAGEFORMAT, virusHrString);
        }
#endif // defined(PLATFORM_WINDOWS) && !defined(CROSSGEN_COMPILE)

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
#ifndef FEATURE_PAL
        PathString wszDllName;
        WszGetModuleFileName((HMODULE)mapped, wszDllName);

        m_LibraryHolder=CLRLoadLibraryEx(wszDllName,NULL,GetLoadWithAlteredSearchPathFlag());
#else // !FEATURE_PAL
        _ASSERTE(!"bTakeOwnership Should not be used on FEATURE_PAL");
#endif // !FEATURE_PAL
    }

    IfFailThrow(Init((void*)mapped,(bool)(bFixedUp!=FALSE)));
}

ConvertedImageLayout::ConvertedImageLayout(PEImageLayout* source)
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

    if (!source->HasNTHeaders())
        EEFileLoadException::Throw(GetPath(), COR_E_BADIMAGEFORMAT);
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening manually mapped stream\n"));


    m_FileMap.Assign(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL,
                                               PAGE_READWRITE, 0,
                                               source->GetVirtualSize(), NULL));
    if (m_FileMap == NULL)
        ThrowLastError();


    m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_ALL_ACCESS, 0, 0, 0,
                                (void *) source->GetPreferredBase()));
    if (m_FileView == NULL)
        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_ALL_ACCESS, 0, 0, 0));

    if (m_FileView == NULL)
        ThrowLastError();

    source->LayoutILOnly(m_FileView, TRUE); //@TODO should be false for streams
    IfFailThrow(Init(m_FileView));

#ifdef CROSSGEN_COMPILE
    if (HasNativeHeader())
        ApplyBaseRelocations();
#endif
}

MappedImageLayout::MappedImageLayout(HANDLE hFile, PEImage* pOwner)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;
    m_Layout=LAYOUT_MAPPED;
    m_pOwner=pOwner;

    // If mapping was requested, try to do SEC_IMAGE mapping
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening OS mapped %S (hFile %p)\n", (LPCWSTR) GetPath(), hFile));

#ifndef FEATURE_PAL


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

#endif // CROSSGEN_COMPILE

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
#endif
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

#else //!FEATURE_PAL

#ifndef CROSSGEN_COMPILE
    m_LoadedFile = PAL_LOADLoadPEFile(hFile);

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

#endif // !FEATURE_PAL
}

#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_PAL)
LoadedImageLayout::LoadedImageLayout(PEImage* pOwner, BOOL bNTSafeLoad, BOOL bThrowOnError)
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
        if (!bThrowOnError)
            return;

        // Fetch the HRESULT upfront before anybody gets a chance to corrupt it
        HRESULT hr = HRESULT_FROM_GetLastError();
        EEFileLoadException::Throw(pOwner->GetPath(), hr, NULL);
    }
    IfFailThrow(Init(m_Module,true));

    LOG((LF_LOADER, LL_INFO1000, "PEImage: Opened HMODULE %S\n", (LPCWSTR) GetPath()));
}
#endif // !CROSSGEN_COMPILE && !FEATURE_PAL

FlatImageLayout::FlatImageLayout(HANDLE hFile, PEImage* pOwner)
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
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening flat %S\n", (LPCWSTR) GetPath()));

    COUNT_T size = SafeGetFileSize(hFile, NULL);
    if (size == 0xffffffff && GetLastError() != NOERROR)
    {
        ThrowLastError();
    }

    // It's okay if resource files are length zero
    if (size > 0)
    {
        m_FileMap.Assign(WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL));
        if (m_FileMap == NULL)
            ThrowLastError();

        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_READ, 0, 0, 0));
        if (m_FileView == NULL)
            ThrowLastError();
    }
    Init(m_FileView, size);
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

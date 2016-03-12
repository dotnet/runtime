// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 

#include "common.h"
#include "peimagelayout.h"
#include "peimagelayout.inl"
#include "pefingerprint.h"

#ifndef DACCESS_COMPILE
PEImageLayout* PEImageLayout::CreateFlat(const void *flat, COUNT_T size,PEImage* pOwner)
{
    STANDARD_VM_CONTRACT;
    return new RawImageLayout(flat,size,pOwner);
}

#ifdef FEATURE_FUSION
PEImageLayout* PEImageLayout::CreateFromStream(IStream* pIStream,PEImage* pOwner)
{
    STANDARD_VM_CONTRACT;
    return new StreamImageLayout(pIStream,pOwner);
}
#endif

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

#ifdef FEATURE_PREJIT
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

    COUNT_T dirPos = 0;
    while (dirPos < dirSize)
    {
        PIMAGE_BASE_RELOCATION r = (PIMAGE_BASE_RELOCATION)(dir + dirPos);

        DWORD rva = VAL32(r->VirtualAddress);

        BYTE * pageAddress = (BYTE *)GetBase() + rva;

        // Check whether the page is outside the unprotected region
        if ((SIZE_T)(pageAddress - pWriteableRegion) >= cbWriteableRegion)
        {
            // Restore the protection
            if (dwOldProtection != 0)
            {
                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       dwOldProtection, &dwOldProtection))
                    ThrowLastError();

                dwOldProtection = 0;
            }

            IMAGE_SECTION_HEADER *pSection = RvaToSection(rva);
            PREFIX_ASSUME(pSection != NULL);

            pWriteableRegion = (BYTE*)GetRvaData(VAL32(pSection->VirtualAddress));
            cbWriteableRegion = VAL32(pSection->SizeOfRawData);

            // Unprotect the section if it is not writable
            if (((pSection->Characteristics & VAL32(IMAGE_SCN_MEM_WRITE)) == 0))
            {
                if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                                       PAGE_READWRITE, &dwOldProtection))
                    ThrowLastError();
            }
        }

        COUNT_T fixupsSize = VAL32(r->SizeOfBlock);

        USHORT *fixups = (USHORT *) (r + 1);

        _ASSERTE(fixupsSize > sizeof(IMAGE_BASE_RELOCATION));
        _ASSERTE((fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) % 2 == 0);

        COUNT_T fixupsCount = (fixupsSize - sizeof(IMAGE_BASE_RELOCATION)) / 2;

        _ASSERTE((BYTE *)(fixups + fixupsCount) <= (BYTE *)(dir + dirSize));

        for (COUNT_T fixupIndex = 0; fixupIndex < fixupsCount; fixupIndex++)
        {
            USHORT fixup = VAL16(fixups[fixupIndex]);

            BYTE * address = pageAddress + (fixup & 0xfff);

            switch (fixup>>12)
            {
            case IMAGE_REL_BASED_PTR:
                *(TADDR *)address += delta;
                break;

#ifdef _TARGET_ARM_
            case IMAGE_REL_BASED_THUMB_MOV32:
                PutThumb2Mov32((UINT16 *)address, GetThumb2Mov32((UINT16 *)address) + delta);
                break;
#endif

            case IMAGE_REL_BASED_ABSOLUTE:
                //no adjustment
                break;

            default:
                _ASSERTE(!"Unhandled reloc type!");
            }
        }

        dirPos += fixupsSize;
    }
    _ASSERTE(dirSize == dirPos);

    if (dwOldProtection != 0)
    {
        // Restore the protection
        if (!ClrVirtualProtect(pWriteableRegion, cbWriteableRegion,
                               dwOldProtection, &dwOldProtection))
            ThrowLastError();
    }
}
#endif // FEATURE_PREJIT

#ifndef FEATURE_CORECLR
// Event Tracing for Windows is used to log data for performance and functional testing purposes.
// The events in this structure are used to measure the time taken by PE image mapping. This is useful to reliably measure the
// performance of the assembly loader by subtracting the time taken by the possibly I/O-intensive work of PE image mapping.
struct ETWLoaderMappingPhaseHolder { // Special-purpose holder structure to ensure the LoaderMappingPhaseEnd ETW event is fired when returning from a function.
    StackSString ETWCodeBase;
    DWORD _dwAppDomainId;
    BOOL initialized;

    ETWLoaderMappingPhaseHolder(){
        LIMITED_METHOD_CONTRACT;
        _dwAppDomainId = ETWAppDomainIdNotAvailable;
        initialized = FALSE;
        }

    void Init(DWORD dwAppDomainId, SString wszCodeBase) {
        _dwAppDomainId = dwAppDomainId;

        EX_TRY
        {
            ETWCodeBase.Append(wszCodeBase);
            ETWCodeBase.Normalize(); // Ensures that the later cast to LPCWSTR does not throw.
        }
        EX_CATCH
        {
            ETWCodeBase.Clear();
        }
        EX_END_CATCH(RethrowTransientExceptions)            

        FireEtwLoaderMappingPhaseStart(_dwAppDomainId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, ETWCodeBase.IsEmpty() ? NULL : (LPCWSTR)ETWCodeBase, NULL, GetClrInstanceId());

        initialized = TRUE;
    }

    ~ETWLoaderMappingPhaseHolder() {
        if (initialized) {
            FireEtwLoaderMappingPhaseEnd(_dwAppDomainId, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderLoadTypeNotAvailable, ETWCodeBase.IsEmpty() ? NULL : (LPCWSTR)ETWCodeBase, NULL, GetClrInstanceId());
        }
    }
};
#endif // FEATURE_CORECLR

RawImageLayout::RawImageLayout(const void *flat, COUNT_T size,PEImage* pOwner)
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

    PEFingerprintVerificationHolder verifyHolder(pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR

    if (size)
    {
        HandleHolder mapping(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL, 
                                               PAGE_READWRITE, 0, 
                                               size, NULL));
        if (mapping==NULL)
            ThrowLastError();
        m_DataCopy.Assign(CLRMapViewOfFile(mapping, FILE_MAP_ALL_ACCESS, 0, 0, 0));
        if(m_DataCopy==NULL)
            ThrowLastError();    
        memcpy(m_DataCopy,flat,size);
        flat=m_DataCopy;
    }
    TESTHOOKCALL(ImageMapped(GetPath(),flat,IM_FLAT));
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

    PEFingerprintVerificationHolder verifyHolder(pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR

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

    TESTHOOKCALL(ImageMapped(GetPath(),mapped,bFixedUp?IM_IMAGEMAP|IM_FIXEDUP:IM_IMAGEMAP));    
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

    PEFingerprintVerificationHolder verifyHolder(source->m_pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR
    
    if (!source->HasNTHeaders())
        EEFileLoadException::Throw(GetPath(), COR_E_BADIMAGEFORMAT);
    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening manually mapped stream\n"));


    m_FileMap.Assign(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL, 
                                               PAGE_READWRITE, 0, 
                                               source->GetVirtualSize(), NULL));
    if (m_FileMap == NULL)
        ThrowLastError();
        

    m_FileView.Assign(CLRMapViewOfFileEx(m_FileMap, FILE_MAP_ALL_ACCESS, 0, 0, 0, 
                                (void *) source->GetPreferredBase()));
    if (m_FileView == NULL)
        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_ALL_ACCESS, 0, 0, 0));
    
    if (m_FileView == NULL)
        ThrowLastError();
    
    source->LayoutILOnly(m_FileView, TRUE); //@TODO should be false for streams
    TESTHOOKCALL(ImageMapped(GetPath(),m_FileView,IM_IMAGEMAP));            
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

    PEFingerprintVerificationHolder verifyHolder(pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_PAL
#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR

    // Let OS map file for us

    // This may fail on e.g. cross-platform (32/64) loads.
    m_FileMap.Assign(WszCreateFileMapping(hFile, NULL, PAGE_READONLY | SEC_IMAGE, 0, 0, NULL));
    if (m_FileMap == NULL)
    {
#ifndef CROSSGEN_COMPILE
#ifdef FEATURE_CORECLR

        // There is no reflection-only load on CoreCLR and so we can always throw an error here.
        // It is important on Windows Phone. All assemblies that we load must have SEC_IMAGE set
        // so that the OS can perform signature verification.
        ThrowLastError();

#else // FEATURE_CORECLR

        // We need to ensure any signature validation errors are caught if Extended Secure Boot (ESB) is on.
        // Also, we have to always throw here during NGen to ensure that the signature validation is never skipped.
        if (GetLastError() != ERROR_BAD_EXE_FORMAT || IsCompilationProcess())
        {
            ThrowLastError();
        }

#endif // FEATURE_CORECLR
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

#ifdef FEATURE_MIXEDMODE
    //
    // For our preliminary loads, we don't want to take the preferred base address. We want to leave
    // that open for a LoadLibrary.  So, we first a phony MapViewOfFile to occupy the base
    // address temporarily. 
    //
    // Note that this is bad if we are racing another thread which is doing a LoadLibrary.  We
    // may want to tweak this logic, but it's pretty difficult to tell MapViewOfFileEx to map
    // a file NOT at its preferred base address.  Hopefully the ulimate solution here will be
    // just mapping the file once.
    //
    // There are two distinct cases that this code takes care of:
    //
    // * NGened IL-only assembly: The IL image will get mapped here and LoadLibrary will be called
    //   on the NGen image later. If we need to, we can avoid creating the fake view on VISTA in this 
    //   case. ASLR will map the IL image and NGen image at different addresses for free.
    //
    // * Mixed-mode assembly (either NGened or not): The mixed-mode image will get mapped here and
    //   LoadLibrary will be called on the same image again later. Note that ASLR does not help 
    //   in this case. The fake view has to be created even on VISTA in this case to avoid relocations.
    //    
    CLRMapViewHolder temp;

    // We don't want to map at the prefered address, so have the temporary view take it. 
    temp.Assign(CLRMapViewOfFile(m_FileMap, 0, 0, 0, 0));
    if (temp == NULL)
        ThrowLastError();
#endif // FEATURE_MIXEDMODE
    m_FileView.Assign(CLRMapViewOfFile(m_FileMap, 0, 0, 0, 0));
    if (m_FileView == NULL)
        ThrowLastError();
    TESTHOOKCALL(ImageMapped(GetPath(),m_FileView,IM_IMAGEMAP));    
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

        if (HasNativeHeader())
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

#ifdef FEATURE_PREJIT
    if (pOwner->IsTrustedNativeImage())
    {
        m_FileView = PAL_LOADLoadPEFile(hFile);
        if (m_FileView == NULL)
            ThrowHR(E_FAIL); // we don't have any indication of what kind of failure. Possibly a corrupt image.

        LOG((LF_LOADER, LL_INFO1000, "PEImage: image %S (hFile %p) mapped @ %p\n",
            (LPCWSTR) GetPath(), hFile, (void*)m_FileView));

        TESTHOOKCALL(ImageMapped(GetPath(),m_FileView,IM_IMAGEMAP));            
        IfFailThrow(Init((void *) m_FileView));

        if (!IsNativeMachineFormat() || !HasCorHeader() || (!HasNativeHeader() && !HasReadyToRunHeader()))
             ThrowHR(COR_E_BADIMAGEFORMAT);

        //Do base relocation for PE, if necessary.
        ApplyBaseRelocations();
    }
#else //FEATURE_PREJIT
    //Do nothing.  The file cannot be mapped unless it is an ngen image.
#endif //FEATURE_PREJIT

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

    PEFingerprintVerificationHolder verifyHolder(pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR

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
    TESTHOOKCALL(ImageMapped(GetPath(),m_Module,IM_LOADLIBRARY));
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

    PEFingerprintVerificationHolder verifyHolder(pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR

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
    TESTHOOKCALL(ImageMapped(GetPath(),m_FileView,IM_FLAT));    
    Init(m_FileView, size);
}

#ifdef FEATURE_FUSION
StreamImageLayout::StreamImageLayout(IStream* pIStream,PEImage* pOwner)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;
    
    m_Layout=LAYOUT_FLAT;
    m_pOwner=pOwner;

    PEFingerprintVerificationHolder verifyHolder(pOwner);  // Do not remove: This holder ensures the IL file hasn't changed since the runtime started making assumptions about it.

#ifndef FEATURE_CORECLR
    ETWLoaderMappingPhaseHolder loaderMappingPhaseHolder;
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD)) {
        loaderMappingPhaseHolder.Init(GetAppDomain() ? GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, GetPath());
    }
#endif // FEATURE_CORECLR
    
    STATSTG statStg;
    IfFailThrow(pIStream->Stat(&statStg, STATFLAG_NONAME));
    if (statStg.cbSize.u.HighPart > 0)
        ThrowHR(COR_E_FILELOAD);

    DWORD cbRead = 0;

    // Resources files may have zero length (and would be mapped as FLAT)
    if (statStg.cbSize.u.LowPart) {
         m_FileMap.Assign(WszCreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, 
                                                   statStg.cbSize.u.LowPart, NULL));
        if (m_FileMap == NULL)
            ThrowWin32(GetLastError());

        m_FileView.Assign(CLRMapViewOfFile(m_FileMap, FILE_MAP_ALL_ACCESS, 0, 0, 0));
        
        if (m_FileView == NULL)
            ThrowWin32(GetLastError());
        
        HRESULT hr = pIStream->Read(m_FileView, statStg.cbSize.u.LowPart, &cbRead);
        if (hr == S_FALSE)
            hr = COR_E_FILELOAD;

        IfFailThrow(hr);
    }
    TESTHOOKCALL(ImageMapped(GetPath(),m_FileView,IM_FLAT));        
    Init(m_FileView,(COUNT_T)cbRead);
}
#endif // FEATURE_FUSION

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

#if defined(_WIN64) && !defined(DACCESS_COMPILE)

#define IMAGE_HEADER_3264_SIZE_DIFF (sizeof(IMAGE_NT_HEADERS64) - sizeof(IMAGE_NT_HEADERS32)) 

// This function is expected to be in sync with LdrpCorFixupImage in the OS loader implementation (//depot/winmain/minkernel/ntdll/ldrcor.c).
bool PEImageLayout::ConvertILOnlyPE32ToPE64Worker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsILOnly()); // This should be called for IL-Only images
        PRECONDITION(Has32BitNTHeaders()); // // Image should be marked to have a PE32 header only.
        PRECONDITION(IsPlatformNeutral());
    }
    CONTRACTL_END;
    
    PBYTE pImage = (PBYTE)GetBase();
    
    IMAGE_DOS_HEADER *pDosHeader = (IMAGE_DOS_HEADER*)pImage;
    IMAGE_NT_HEADERS32 *pHeader32 = GetNTHeaders32();
    IMAGE_NT_HEADERS64 *pHeader64 = GetNTHeaders64();

    _ASSERTE(&pHeader32->OptionalHeader.Magic == &pHeader64->OptionalHeader.Magic);
    _ASSERTE(pHeader32->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC));

    // Move the data directory and section headers down IMAGE_HEADER_3264_SIZE_DIFF bytes.
    PBYTE pStart32 = (PBYTE) &pHeader32->OptionalHeader.DataDirectory[0];
    PBYTE pStart64 = (PBYTE) &pHeader64->OptionalHeader.DataDirectory[0];
    _ASSERTE(pStart64 - pStart32 == IMAGE_HEADER_3264_SIZE_DIFF);

    PBYTE pEnd32 = (PBYTE) (IMAGE_FIRST_SECTION(pHeader32)
                            + VAL16(pHeader32->FileHeader.NumberOfSections));
    
    // On AMD64, used for a 12-byte jump thunk + the original entry point offset.
    if (((pEnd32 + IMAGE_HEADER_3264_SIZE_DIFF /* delta in headers to compute end of 64bit header */) - pImage) > OS_PAGE_SIZE ) {
        // This should never happen.  An IL_ONLY image should at most 3 sections.  
        _ASSERTE(!"ConvertILOnlyPE32ToPE64Worker: Insufficient room to rewrite headers as PE64");
        return false;
    }

    memmove(pStart64, pStart32, pEnd32 - pStart32);

    // Move the tail fields in reverse order.
    pHeader64->OptionalHeader.NumberOfRvaAndSizes = pHeader32->OptionalHeader.NumberOfRvaAndSizes;
    pHeader64->OptionalHeader.LoaderFlags = pHeader32->OptionalHeader.LoaderFlags;
    pHeader64->OptionalHeader.SizeOfHeapCommit = VAL64(VAL32(pHeader32->OptionalHeader.SizeOfHeapCommit));
    pHeader64->OptionalHeader.SizeOfHeapReserve = VAL64(VAL32(pHeader32->OptionalHeader.SizeOfHeapReserve));
    pHeader64->OptionalHeader.SizeOfStackCommit = VAL64(VAL32(pHeader32->OptionalHeader.SizeOfStackCommit));
    pHeader64->OptionalHeader.SizeOfStackReserve = VAL64(VAL32(pHeader32->OptionalHeader.SizeOfStackReserve));

    // One more field that's not the same
    pHeader64->OptionalHeader.ImageBase = VAL64(VAL32(pHeader32->OptionalHeader.ImageBase));

    // The optional header changed size.
    pHeader64->FileHeader.SizeOfOptionalHeader = VAL16(VAL16(pHeader64->FileHeader.SizeOfOptionalHeader) + 16);
    pHeader64->OptionalHeader.Magic = VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC);

    // Now we just have to make a new 16-byte PPLABEL_DESCRIPTOR for the new entry point address & gp
    PBYTE pEnd64 = (PBYTE) (IMAGE_FIRST_SECTION(pHeader64) + VAL16(pHeader64->FileHeader.NumberOfSections));
    pHeader64->OptionalHeader.AddressOfEntryPoint = VAL32((ULONG) (pEnd64 - pImage));
    
    // Should be PE32+ now
    _ASSERTE(!Has32BitNTHeaders());
    
    return true;
}

bool PEImageLayout::ConvertILOnlyPE32ToPE64()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsILOnly()); // This should be called for IL-Only images
        PRECONDITION(Has32BitNTHeaders()); 
    }
    CONTRACTL_END;
    
    bool fConvertedToPE64 = false;

    // Only handle platform neutral IL assemblies
    if (!IsPlatformNeutral())
    {
        return false;
    }

    PBYTE pageBase = (PBYTE)GetBase();
    DWORD oldProtect;

    if (!ClrVirtualProtect(pageBase, OS_PAGE_SIZE, PAGE_READWRITE, &oldProtect))
    {
        // We are not going to be able to update header.
        return false;
    }
        
    fConvertedToPE64 = ConvertILOnlyPE32ToPE64Worker();
    
    DWORD ignore;
    if (!ClrVirtualProtect(pageBase, OS_PAGE_SIZE, oldProtect, &ignore))
    {
        // This is not so bad; just ignore it
    }
    
    return fConvertedToPE64;
}
#endif // defined(_WIN64) && !defined(DACCESS_COMPILE)

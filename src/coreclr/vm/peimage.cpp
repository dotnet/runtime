// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEImage.cpp
//

// --------------------------------------------------------------------------------


#include "common.h"

#include "peimage.h"
#include "eeconfig.h"
#include <objbase.h>

#include "eventtrace.h"
#include "peimagelayout.inl"

#ifndef DACCESS_COMPILE

CrstStatic  PEImage::s_hashLock;
PtrHashMap *PEImage::s_Images = NULL;
CrstStatic  PEImage::s_ijwHashLock;
PtrHashMap *PEImage::s_ijwFixupDataHash;

/* static */
void PEImage::Startup()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckStartup());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (CheckStartup())
        RETURN;

    s_hashLock.Init(CrstPEImage, (CrstFlags)(CRST_REENTRANCY|CRST_TAKEN_DURING_SHUTDOWN));
    LockOwner lock = { &s_hashLock, IsOwnerOfCrst };
    s_Images         = ::new PtrHashMap;
    s_Images->Init(CompareImage, FALSE, &lock);

    s_ijwHashLock.Init(CrstIJWHash, CRST_REENTRANCY);
    LockOwner ijwLock = { &s_ijwHashLock, IsOwnerOfCrst };
    s_ijwFixupDataHash = ::new PtrHashMap;
    s_ijwFixupDataHash->Init(CompareIJWDataBase, FALSE, &ijwLock);

    RETURN;
}

/* static */
CHECK PEImage::CheckStartup()
{
    WRAPPER_NO_CONTRACT;
    CHECK(s_Images != NULL);
    CHECK_OK;
}

CHECK PEImage::CheckILFormat()
{
    WRAPPER_NO_CONTRACT;
    CHECK(GetOrCreateLayout(PEImageLayout::LAYOUT_ANY)->CheckILFormat());
    CHECK_OK;
};

// PEImage is always unique on CoreCLR so a simple pointer check is sufficient in PEImage::Equals
CHECK PEImage::CheckUniqueInstance()
{
    CHECK(GetPath().IsEmpty() || m_bInHashMap);
    CHECK_OK;
}

PEImage::~PEImage()
{
    CONTRACTL
    {
        PRECONDITION(CheckStartup());
        PRECONDITION(m_refCount == 0);
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    if (m_pLayoutLock)
        delete m_pLayoutLock;
    if(m_hFile!=INVALID_HANDLE_VALUE)
        CloseHandle(m_hFile);

    for (unsigned int i=0;i<ARRAY_SIZE(m_pLayouts);i++)
    {
        if (m_pLayouts[i]!=NULL)
            m_pLayouts[i]->Release();
    }

    if (m_pMDImport)
        m_pMDImport->Release();

}

/* static */
BOOL PEImage::CompareIJWDataBase(UPTR base, UPTR mapping)
{
    CONTRACTL{
        PRECONDITION(CheckStartup());
        PRECONDITION(CheckPointer((BYTE *)(base << 1)));
        PRECONDITION(CheckPointer((IJWFixupData *)mapping));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    return ((BYTE *)(base << 1) == ((IJWFixupData*)mapping)->GetBase());
}

ULONG PEImage::Release()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    CONTRACT_VIOLATION(FaultViolation|ThrowsViolation);
    COUNT_T result = 0;
    {
        // Use scoping to hold the hash lock
        CrstHolder holder(&s_hashLock);

        // Decrement and check the refcount - if we hit 0, remove it from the hash and delete it.
        result=InterlockedDecrement(&m_refCount);
        if (result == 0 )
        {
            LOG((LF_LOADER, LL_INFO100, "PEImage: Closing %p\n", this));
            if(m_bInHashMap)
            {
                PEImageLocator locator(this);
                PEImage* deleted = (PEImage *)s_Images->DeleteValue(m_pathHash, &locator);
                _ASSERTE(deleted == this);
            }
        }
    }

    // This needs to be done outside of the hash lock, since this can call FreeLibrary,
    // which can cause _CorDllMain to be executed, which can cause the hash lock to be
    // taken again because we need to release the IJW fixup data in another PEImage hash.
    if (result == 0)
        delete this;

    return result;
}

/* static */
CHECK PEImage::CheckCanonicalFullPath(const SString &path)
{
    CONTRACT_CHECK
    {
        PRECONDITION(CheckValue(path));
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;
#ifdef TARGET_WINDOWS
    CCHECK_START
    {
        // This is not intended to be an exhaustive test, just to provide a sanity check

        SString::CIterator i = path.Begin();

        SString sNetworkPathPrefix(SString::Literal, W("\\\\"));
        if (path.Skip(i, sNetworkPathPrefix))
        {
            // Network path
        }
        else if (iswalpha(*i))
        {
            // Drive path
            i++;
            SString sDrivePath(SString::Literal, W(":\\"));
            CCHECK(path.Skip(i, sDrivePath));
        }
        else
        {
            CCHECK_FAIL("Not a full path");
        }

        while (i != path.End())
        {
            // Check for multiple slashes
            if(*i != DIRECTORY_SEPARATOR_CHAR_A)
            {

                // Check for . or ..
                SString sParentDir(SString::Ascii, "..");
                SString sCurrentDir(SString::Ascii, ".");
                if ((path.Skip(i, sParentDir) || path.Skip(i, sCurrentDir))
                    && (path.Match(i, DIRECTORY_SEPARATOR_CHAR_A)))
                {
                    CCHECK_FAIL("Illegal . or ..");
                }

                if (!path.Find(i, DIRECTORY_SEPARATOR_CHAR_A))
                    break;
            }

            i++;
        }
    }
    CCHECK_END;
#endif // TARGET_WINDOWS

    CHECK_OK;
}

/* static */
BOOL PEImage::CompareImage(UPTR u1, UPTR u2)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This is the input to the lookup
    PEImageLocator *pLocator = (PEImageLocator *) (u1<<1);

    // This is the value stored in the table
    PEImage *pImage = (PEImage *) u2;

    if (pLocator->m_bIsInBundle != pImage->IsInBundle())
    {
        return FALSE;
    }

    BOOL ret = FALSE;
    HRESULT hr;
    EX_TRY
    {
        SString path(SString::Literal, pLocator->m_pPath);
        if (pImage->GetPath().EqualsCaseInsensitive(path))
        {
            ret = TRUE;
        }
    }
    EX_CATCH_HRESULT(hr); //<TODO>ignores failure!</TODO>
    return ret;
}

BOOL PEImage::Equals(PEImage *pImage)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pImage));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // PEImage is always unique on CoreCLR so a simple pointer check is sufficient
    _ASSERTE(CheckUniqueInstance());
    _ASSERTE(pImage->CheckUniqueInstance());

    return dac_cast<TADDR>(pImage) == dac_cast<TADDR>(this);
}


IMDInternalImport* PEImage::GetMDImport()
{
    WRAPPER_NO_CONTRACT;
    if (!m_pMDImport)
        OpenMDImport();
    return m_pMDImport;
}

void PEImage::OpenMDImport()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasCorHeader());
        PRECONDITION(HasContents());
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_pMDImport==NULL)
    {
        IMDInternalImport* m_pNewImport;
        const void* pMeta=NULL;
        COUNT_T cMeta=0;
        if(HasNTHeaders() && HasCorHeader())
            pMeta=GetMetadata(&cMeta);

        if(pMeta==NULL)
            return;

        IfFailThrow(GetMetaDataInternalInterface((void *) pMeta,
                                                 cMeta,
                                                 ofRead,
                                                 IID_IMDInternalImport,
                                                 (void **) &m_pNewImport));

        if(InterlockedCompareExchangeT(&m_pMDImport, m_pNewImport, NULL))
        {
            m_pNewImport->Release();
        }
        else
        {
            // grab the module name. This information is only used for dac. But we need to get
            // it when module is instantiated in the managed process. The module name is stored
            // in Metadata's module table in UTF8. Convert it to unicode.
            //
            if (m_path.IsEmpty())
            {
                // No need to check error here since this info is only used by DAC when inspecting
                // dump file.
                //
                LPCSTR strModuleName;
                IfFailThrow(m_pMDImport->GetScopeProps(&strModuleName, NULL));
                m_sModuleFileNameHintUsedByDac.SetUTF8(strModuleName);
                m_sModuleFileNameHintUsedByDac.Normalize();
            }
         }
    }
    _ASSERTE(m_pMDImport);

}

void PEImage::GetMVID(GUID *pMvid)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMvid));
        PRECONDITION(HasCorHeader());
        PRECONDITION(HasContents());
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    IfFailThrow(GetMDImport()->GetScopeProps(NULL, pMvid));

#ifdef _DEBUG
    COUNT_T cMeta;
    const void *pMeta = GetMetadata(&cMeta);
    GUID MvidDEBUG;

    if (pMeta == NULL)
        ThrowHR(COR_E_BADIMAGEFORMAT);

    SafeComHolder<IMDInternalImport> pMDImport;

    IfFailThrow(GetMetaDataInternalInterface((void *) pMeta,
                                             cMeta,
                                             ofRead,
                                             IID_IMDInternalImport,
                                             (void **) &pMDImport));

    pMDImport->GetScopeProps(NULL, &MvidDEBUG);

    _ASSERTE(memcmp(pMvid, &MvidDEBUG, sizeof(GUID)) == 0);

#endif // _DEBUG
}

//may outlive PEImage
PEImage::IJWFixupData::IJWFixupData(void *pBase)
    : m_lock(CrstIJWFixupData),
    m_base(pBase), m_flags(0), m_DllThunkHeap(NULL), m_iNextFixup(0), m_iNextMethod(0)
{
    WRAPPER_NO_CONTRACT;
}

PEImage::IJWFixupData::~IJWFixupData()
{
    WRAPPER_NO_CONTRACT;
    if (m_DllThunkHeap)
        delete m_DllThunkHeap;
}


// Self-initializing accessor for m_DllThunkHeap
LoaderHeap *PEImage::IJWFixupData::GetThunkHeap()
{
    CONTRACT(LoaderHeap *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    if (!m_DllThunkHeap)
    {
        LoaderHeap *pNewHeap = new LoaderHeap(VIRTUAL_ALLOC_RESERVE_GRANULARITY, // DWORD dwReserveBlockSize
            0,                                 // DWORD dwCommitBlockSize
            ThunkHeapStubManager::g_pManager->GetRangeList(),
            UnlockedLoaderHeap::HeapKind::Executable);

        if (InterlockedCompareExchangeT((PVOID*)&m_DllThunkHeap, (VOID*)pNewHeap, (VOID*)0) != 0)
        {
            delete pNewHeap;
        }
    }

    RETURN m_DllThunkHeap;
}

void PEImage::IJWFixupData::MarkMethodFixedUp(COUNT_T iFixup, COUNT_T iMethod)
{
    LIMITED_METHOD_CONTRACT;
    // supports only sequential fixup/method
    _ASSERTE((iFixup == m_iNextFixup + 1 && iMethod == 0) ||                 //first method of the next fixup or
        (iFixup == m_iNextFixup && iMethod == m_iNextMethod));     //the method that was next to fixup

    m_iNextFixup = iFixup;
    m_iNextMethod = iMethod + 1;
}

BOOL PEImage::IJWFixupData::IsMethodFixedUp(COUNT_T iFixup, COUNT_T iMethod)
{
    LIMITED_METHOD_CONTRACT;
    if (iFixup < m_iNextFixup)
        return TRUE;
    if (iFixup > m_iNextFixup)
        return FALSE;
    if (iMethod < m_iNextMethod)
        return TRUE;

    return FALSE;
}

/*static */
PTR_LoaderHeap PEImage::GetDllThunkHeap(void *pBase)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    return GetIJWData(pBase)->GetThunkHeap();
}

/* static */
PEImage::IJWFixupData *PEImage::GetIJWData(void *pBase)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END

    // Take the IJW hash lock
    CrstHolder hashLockHolder(&s_ijwHashLock);

    // Try to find the data
    IJWFixupData *pData = (IJWFixupData *)s_ijwFixupDataHash->LookupValue((UPTR)pBase, pBase);

    // No data, must create
    if ((UPTR)pData == (UPTR)INVALIDENTRY)
    {
        pData = new IJWFixupData(pBase);
        s_ijwFixupDataHash->InsertValue((UPTR)pBase, pData);
    }

    // Return the new data
    return (pData);
}

/* static */
void PEImage::UnloadIJWModule(void *pBase)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END

    // Take the IJW hash lock
    CrstHolder hashLockHolder(&s_ijwHashLock);

    // Try to delete the hash entry
    IJWFixupData *pData = (IJWFixupData *)s_ijwFixupDataHash->DeleteValue((UPTR)pBase, pBase);

    // Now delete the data
    if ((UPTR)pData != (UPTR)INVALIDENTRY)
        delete pData;
}




#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void PEImage::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // There are codepaths that will enumerate the PEImage without
    // calling EnumMemoryRegions; ensure that we will still get
    // these necessary fields enumerated no matter what.
    m_path.EnumMemoryRegions(flags);

    // We always want this field in mini/triage/heap dumps.
    m_sModuleFileNameHintUsedByDac.EnumMemoryRegions(CLRDATA_ENUM_MEM_DEFAULT);


    EX_TRY
    {
        if (HasLoadedLayout() && HasNTHeaders() && HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG))
        {
            // Get a pointer to the contents and size of the debug directory and report it
            COUNT_T cbDebugDir;
            TADDR taDebugDir = GetLoadedLayout()->GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &cbDebugDir);
            DacEnumMemoryRegion(taDebugDir, cbDebugDir);

            // Report the memory that each debug directory entry points to
            UINT cNumEntries = cbDebugDir / sizeof(IMAGE_DEBUG_DIRECTORY);
            PTR_IMAGE_DEBUG_DIRECTORY pDebugEntry = dac_cast<PTR_IMAGE_DEBUG_DIRECTORY>(taDebugDir);
            for (UINT iIndex = 0; iIndex < cNumEntries; iIndex++)
            {
                TADDR taEntryAddr = GetLoadedLayout()->GetRvaData(pDebugEntry[iIndex].AddressOfRawData);
                DacEnumMemoryRegion(taEntryAddr, pDebugEntry[iIndex].SizeOfData);

                // Triage dumps must not dump full paths as they may contain PII data.
                // Thus, we replace debug directory's pdbs full path for with filaname only.
                if (flags == CLRDATA_ENUM_MEM_TRIAGE &&
                    pDebugEntry[iIndex].Type == IMAGE_DEBUG_TYPE_CODEVIEW)
                {
                    DWORD CvSignature = *(dac_cast<PTR_DWORD>(taEntryAddr));
                    if(CvSignature == CV_SIGNATURE_RSDS)
                    {
                        CV_INFO_PDB70* pCvInfo = (CV_INFO_PDB70*)DacInstantiateTypeByAddressNoReport(taEntryAddr, sizeof(CV_INFO_PDB70), false);

                        if (pCvInfo == NULL)
                        {
                            continue;
                        }
                        // Because data may be corrupted make sure we null terminate the string.
                        pCvInfo->path[MAX_LONGPATH - 1] = '\0';

                        //Find the filename from pdb full path
                        char* fileName = strrchr(pCvInfo->path, '\\');
                        if (fileName != NULL)
                            fileName++;
                        else
                            fileName = pCvInfo->path;

                        size_t fileNameLength = strlen(fileName);
                        size_t fullPathLength = strlen(pCvInfo->path);
                        memmove(pCvInfo->path, fileName, fileNameLength);

                        // NULL out the rest of the path buffer.
                        for (size_t i = fileNameLength; i < MAX_PATH_FNAME - 1; i++)
                        {
                            pCvInfo->path[i] = '\0';
                        }

                        DacUpdateMemoryRegion( taEntryAddr + offsetof(CV_INFO_PDB70, path), sizeof(pCvInfo->path), (PBYTE)pCvInfo->path );
                    }
                }
            }
        }
    }
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED

    DAC_ENUM_DTHIS();

    EMEM_OUT(("MEM: %p PEImage\n", dac_cast<TADDR>(this)));

    // This just gets the image headers into the dump.
    // This is used, for example, for ngen images to ensure we have the debug directory so we
    // can find the managed PDBs.
    // No lock here as the process should be suspended.
    if (m_pLayouts[IMAGE_FLAT].IsValid() && m_pLayouts[IMAGE_FLAT]!=NULL)
        m_pLayouts[IMAGE_FLAT]->EnumMemoryRegions(flags);
    if (m_pLayouts[IMAGE_LOADED].IsValid() &&  m_pLayouts[IMAGE_LOADED]!=NULL)
        m_pLayouts[IMAGE_LOADED]->EnumMemoryRegions(flags);
}

#endif // #ifdef DACCESS_COMPILE


PEImage::PEImage():
    m_path(),
    m_pathHash(0),
    m_refCount(1),
    m_bInHashMap(FALSE),
    m_bundleFileLocation(),
    m_hFile(INVALID_HANDLE_VALUE),
    m_dwPEKind(0),
    m_dwMachine(0),
    m_pMDImport(NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    for (DWORD i=0;i<ARRAY_SIZE(m_pLayouts);i++)
        m_pLayouts[i]=NULL ;
    m_pLayoutLock=new SimpleRWLock(PREEMPTIVE,LOCK_TYPE_DEFAULT);
}

// Misnomer under the DAC, but has a lot of callers. The DAC can't create layouts, so in that
// case this is a get.
PTR_PEImageLayout PEImage::GetOrCreateLayout(DWORD imageLayoutMask)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // First attempt to find an existing layout matching imageLayoutMask.
    // If that fails, try again with auto-creating helper.
    // Note: we use reader-writer lock, but only writes are synchronized.
    PTR_PEImageLayout pRetVal = GetExistingLayoutInternal(imageLayoutMask);

    if (pRetVal == NULL)
    {
#ifndef DACCESS_COMPILE
        GCX_PREEMP();
        SimpleWriteLockHolder lock(m_pLayoutLock);
        pRetVal = GetOrCreateLayoutInternal(imageLayoutMask);
#else
        // In DAC builds, we can't create any layouts - we must require that they already exist.
        // We also don't take any AddRefs or locks in DAC builds - it's inspection-only.
        _ASSERTE_MSG(false, "DACization error - caller expects PEImage layout to exist and it doesn't");
        DacError(E_UNEXPECTED);
#endif
    }

    return pRetVal;
}

#ifndef DACCESS_COMPILE

void PEImage::SetLayout(DWORD dwLayout, PEImageLayout* pLayout)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(dwLayout < IMAGE_COUNT);
    _ASSERTE(m_pLayoutLock->IsWriterLock());
    _ASSERTE(m_pLayouts[dwLayout] == NULL);

    m_pLayouts[dwLayout] = pLayout;
}

PTR_PEImageLayout PEImage::GetOrCreateLayoutInternal(DWORD imageLayoutMask)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PTR_PEImageLayout pRetVal=GetExistingLayoutInternal(imageLayoutMask);

    if (pRetVal==NULL)
    {
        BOOL bIsLoadedLayoutSuitable = ((imageLayoutMask & PEImageLayout::LAYOUT_LOADED) != 0);
        BOOL bIsFlatLayoutSuitable = ((imageLayoutMask & PEImageLayout::LAYOUT_FLAT) != 0);

        BOOL bIsLoadedLayoutPreferred = !bIsFlatLayoutSuitable;

#ifdef TARGET_WINDOWS
        // on Windows we prefer to just load the file using OS loader
        if (!IsInBundle() && bIsLoadedLayoutSuitable)
        {
            bIsLoadedLayoutPreferred = TRUE;
        }
#endif // !TARGET_UNIX

        _ASSERTE(bIsLoadedLayoutSuitable || bIsFlatLayoutSuitable);

        if (bIsLoadedLayoutPreferred)
        {
            _ASSERTE(bIsLoadedLayoutSuitable);
            pRetVal = PEImage::CreateLoadedLayout(!bIsFlatLayoutSuitable);
        }

        if (pRetVal == NULL)
        {
            _ASSERTE(bIsFlatLayoutSuitable);
            pRetVal = PEImage::CreateFlatLayout();
            _ASSERTE(pRetVal != NULL);
        }
    }

    _ASSERTE(pRetVal != NULL);
    _ASSERTE(this->IsOpened());
    return pRetVal;
}

PTR_PEImageLayout PEImage::CreateLoadedLayout(bool throwOnFailure)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pLayoutLock->IsWriterLock());
    }
    CONTRACTL_END;

    PEImageLayout * pLoadLayout = NULL;

    HRESULT loadFailure = S_OK;
    pLoadLayout = PEImageLayout::Load(this, &loadFailure);
    if (pLoadLayout != NULL)
    {
        SetLayout(IMAGE_LOADED,pLoadLayout);
        // loaded layout is functionally a superset of flat,
        // so fill the flat slot, if not filled already.
        if (m_pLayouts[IMAGE_FLAT] == NULL)
        {
            pLoadLayout->AddRef();
            SetLayout(IMAGE_FLAT, pLoadLayout);
        }
    }

    if (pLoadLayout == NULL && throwOnFailure)
    {
        loadFailure = FAILED(loadFailure) ? loadFailure : COR_E_BADIMAGEFORMAT;
        EEFileLoadException::Throw(GetPath(), loadFailure);
    }

    return pLoadLayout;
}

PTR_PEImageLayout PEImage::CreateFlatLayout()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pLayoutLock->IsWriterLock());
    }
    CONTRACTL_END;

    PTR_PEImageLayout pFlatLayout = PEImageLayout::LoadFlat(this);
    SetLayout(IMAGE_FLAT, pFlatLayout);
    return pFlatLayout;
}

/* static */
PTR_PEImage PEImage::CreateFromByteArray(const BYTE* array, COUNT_T size)
{
    CONTRACT(PTR_PEImage)
    {
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    PEImageHolder pImage(new PEImage());
    PTR_PEImageLayout pLayout = PEImageLayout::CreateFromByteArray(pImage, array, size);
    _ASSERTE(!pLayout->IsMapped());

    SimpleWriteLockHolder lock(pImage->m_pLayoutLock);
    pImage->SetLayout(IMAGE_FLAT,pLayout);
    RETURN dac_cast<PTR_PEImage>(pImage.Extract());
}

#ifndef TARGET_UNIX
/* static */
PTR_PEImage PEImage::CreateFromHMODULE(HMODULE hMod)
{
    CONTRACT(PTR_PEImage)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(hMod!=NULL);
        POSTCONDITION(RETVAL->HasLoadedLayout());
    }
    CONTRACT_END;

    StackSString path;
    WszGetModuleFileName(hMod, path);
    PEImageHolder pImage(PEImage::OpenImage(path, MDInternalImport_Default));

    if (!pImage->HasLoadedLayout())
    {
        PTR_PEImageLayout pLayout = PEImageLayout::CreateFromHMODULE(hMod, pImage);

        SimpleWriteLockHolder lock(pImage->m_pLayoutLock);
        pImage->SetLayout(IMAGE_LOADED, pLayout);
        if (pImage->m_pLayouts[IMAGE_FLAT] == NULL)
        {
            pLayout->AddRef();
            pImage->SetLayout(IMAGE_FLAT, pLayout);
        }
    }

    _ASSERTE(pImage->m_pLayouts[IMAGE_FLAT] != NULL);
    RETURN dac_cast<PTR_PEImage>(pImage.Extract());
}
#endif // !TARGET_UNIX

#endif //DACCESS_COMPILE

HANDLE PEImage::GetFileHandle()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(m_pLayoutLock->IsWriterLock());
    }
    CONTRACTL_END;

    if (m_hFile!=INVALID_HANDLE_VALUE)
        return m_hFile;

    HRESULT hr = TryOpenFile(/*takeLock*/ false);

    if (m_hFile == INVALID_HANDLE_VALUE)
    {
#if !defined(DACCESS_COMPILE)
        EEFileLoadException::Throw(GetPathToLoad(), hr);
#else // defined(DACCESS_COMPILE)
        ThrowHR(hr);
#endif // !defined(DACCESS_COMPILE)
    }

    return m_hFile;
}

HRESULT PEImage::TryOpenFile(bool takeLock)
{
    STANDARD_VM_CONTRACT;

    SimpleWriteLockHolder lock(m_pLayoutLock, takeLock);

    if (m_hFile!=INVALID_HANDLE_VALUE)
        return S_OK;

    ErrorModeHolder mode{};
    m_hFile=WszCreateFile((LPCWSTR)GetPathToLoad(),
                          GENERIC_READ
#if TARGET_WINDOWS
                          // the file may have native code sections, make sure we are allowed to execute the file
                          | GENERIC_EXECUTE
#endif
                          ,
                          FILE_SHARE_READ|FILE_SHARE_DELETE,
                          NULL,
                          OPEN_EXISTING,
                          FILE_ATTRIBUTE_NORMAL,
                          NULL);

    if (m_hFile != INVALID_HANDLE_VALUE)
            return S_OK;

    if (GetLastError())
        return HRESULT_FROM_WIN32(GetLastError());

    return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
}


BOOL PEImage::IsPtrInImage(PTR_CVOID data)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    for (int i = 0; i < IMAGE_COUNT; i++)
    {
        if (m_pLayouts[i] != NULL)
        {
            if (m_pLayouts[i]->PointerInPE(data))
                return TRUE;
        }
    }

    return FALSE;
}

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

    PEImageLayout::Startup();

    RETURN;
}

/* static */
CHECK PEImage::CheckStartup()
{
    WRAPPER_NO_CONTRACT;
    CHECK(s_Images != NULL);
    CHECK_OK;
}

/* static */
CHECK PEImage::CheckLayoutFormat(PEDecoder *pe)
{
    CONTRACT_CHECK
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_CHECK_END;

    CHECK(pe->IsILOnly());
    CHECK(!pe->HasNativeHeader());
    CHECK_OK;
}

CHECK PEImage::CheckILFormat()
{
    WRAPPER_NO_CONTRACT;

    PTR_PEImageLayout pLayoutToCheck;
    PEImageLayoutHolder pLayoutHolder;

    if (HasLoadedLayout())
    {
        pLayoutToCheck = GetLoadedLayout();
    }
    else
    {
        pLayoutHolder = GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED);
        pLayoutToCheck = pLayoutHolder;
    }

    CHECK(pLayoutToCheck->CheckILFormat());

    CHECK_OK;
};

/* static */
// This method is only intended to be called during NGen.  It doesn't AddRef to the objects it returns,
// and can be unsafe for general use.
void PEImage::GetAll(SArray<PEImage*> &images)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder holder(&s_hashLock);

    for (PtrHashMap::PtrIterator i = s_Images->begin(); !i.end(); ++i)
    {
        PEImage *image = (PEImage*) i.GetValue();
        images.Append(image);
    }
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
    if(m_hFile!=INVALID_HANDLE_VALUE && m_bOwnHandle)
        CloseHandle(m_hFile);

    for (unsigned int i=0;i<COUNTOF(m_pLayouts);i++)
    {
        if (m_pLayouts[i]!=NULL)
            m_pLayouts[i]->Release();
    }

    if (m_pMDImport)
        m_pMDImport->Release();
    if(m_pNativeMDImport)
        m_pNativeMDImport->Release();
#ifdef METADATATRACKER_ENABLED
    if (m_pMDTracker != NULL)
        m_pMDTracker->Deactivate();
#endif // METADATATRACKER_ENABLED

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
        result=FastInterlockDecrement(&m_refCount);
        if (result == 0 )
        {
            LOG((LF_LOADER, LL_INFO100, "PEImage: Closing Image %S\n", (LPCWSTR) m_path));
            if(m_bInHashMap)
            {
                PEImageLocator locator(this);
                PEImage* deleted = (PEImage *)s_Images->DeleteValue(GetIDHash(), &locator);
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
            SString sDrivePath(SString::Literal, ":\\");
            CCHECK(path.Skip(i, sDrivePath));
        }
        else
        {
            CCHECK_FAIL("Not a full path");
        }

        while (i != path.End())
        {
            // Check for multiple slashes
            if(*i != '\\')
            {

                // Check for . or ..
                SString sParentDir(SString::Ascii, "..");
                SString sCurrentDir(SString::Ascii, ".");
                if ((path.Skip(i, sParentDir) || path.Skip(i, sCurrentDir))
                    && (path.Match(i, '\\')))
                {
                    CCHECK_FAIL("Illegal . or ..");
                }

                if (!path.Find(i, '\\'))
                    break;
            }

            i++;
        }
    }
    CCHECK_END;

    CHECK_OK;
}

BOOL PEImage::PathEquals(const SString &p1, const SString &p2)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_CASE_SENSITIVE_FILESYSTEM
    return p1.Equals(p2);
#else
    return p1.EqualsCaseInsensitive(p2);
#endif
}

#ifndef TARGET_UNIX
/* static */
void PEImage::GetPathFromDll(HINSTANCE hMod, SString &result)
{
    CONTRACTL
    {
        PRECONDITION(CheckStartup());
        PRECONDITION(CheckPointer(hMod));
        PRECONDITION(CheckValue(result));
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    WszGetModuleFileName(hMod, result);

}
#endif // !TARGET_UNIX

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


    BOOL ret = FALSE;
    HRESULT hr;
    EX_TRY
    {
        SString path(SString::Literal, pLocator->m_pPath);
        BOOL isInBundle = pLocator->m_bIsInBundle;
        if (PathEquals(path, pImage->GetPath()) &&
            (!isInBundle == !pImage->IsInBundle()))
            ret = TRUE;
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
    _ASSERTE(m_bInHashMap || GetPath().IsEmpty());
    _ASSERTE(pImage->m_bInHashMap || pImage->GetPath().IsEmpty());

    return dac_cast<TADDR>(pImage) == dac_cast<TADDR>(this);
}


IMDInternalImport* PEImage::GetMDImport()
{
    WRAPPER_NO_CONTRACT;
    if (!m_pMDImport)
        OpenMDImport();
    return m_pMDImport;
}

IMDInternalImport* PEImage::GetNativeMDImport(BOOL loadAllowed)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeHeader() || HasReadyToRunHeader());
        if (loadAllowed) GC_TRIGGERS;                    else GC_NOTRIGGER;
        if (loadAllowed) THROWS;                         else NOTHROW;
        if (loadAllowed) INJECT_FAULT(COMPlusThrowOM()); else FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pNativeMDImport == NULL)
    {
        if (loadAllowed)
            OpenNativeMDImport();
        else
            return NULL;
    }

    _ASSERTE(m_pNativeMDImport);
    return m_pNativeMDImport;
}

void PEImage::OpenNativeMDImport()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeHeader() || HasReadyToRunHeader());
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_pNativeMDImport==NULL)
    {
        IMDInternalImport* m_pNewImport;
        COUNT_T cMeta=0;
        const void* pMeta=GetNativeManifestMetadata(&cMeta);

        if(pMeta==NULL)
            return;

        IfFailThrow(GetMetaDataInternalInterface((void *) pMeta,
                                                 cMeta,
                                                 ofRead,
                                                 IID_IMDInternalImport,
                                                 (void **) &m_pNewImport));

        if(FastInterlockCompareExchangePointer(&m_pNativeMDImport, m_pNewImport, NULL))
            m_pNewImport->Release();
    }
    _ASSERTE(m_pNativeMDImport);
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

#if METADATATRACKER_ENABLED
        m_pMDTracker = MetaDataTracker::GetOrCreateMetaDataTracker((BYTE *)pMeta,
                                                               cMeta,
                                                               GetPath().GetUnicode());
#endif // METADATATRACKER_ENABLED

        IfFailThrow(GetMetaDataInternalInterface((void *) pMeta,
                                                 cMeta,
                                                 ofRead,
                                                 IID_IMDInternalImport,
                                                 (void **) &m_pNewImport));

        if(FastInterlockCompareExchangePointer(&m_pMDImport, m_pNewImport, NULL))
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

        if (IsCompilationProcess())
        {
            m_pMDImport->SetOptimizeAccessForSpeed(TRUE);
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

void PEImage::VerifyIsAssembly()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    VerifyIsILOrNIAssembly(TRUE);
}

void PEImage::VerifyIsNIAssembly()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    VerifyIsILOrNIAssembly(FALSE);
}

void PEImage::VerifyIsILOrNIAssembly(BOOL fIL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // buch of legacy stuff here wrt the error codes...

    if (!HasNTHeaders())
        ThrowFormat(COR_E_BADIMAGEFORMAT);

    if(!HasCorHeader())
        ThrowFormat(COR_E_ASSEMBLYEXPECTED);

    CHECK checkGoodFormat;
    checkGoodFormat = CheckILFormat();
    if (!checkGoodFormat)
        ThrowFormat(COR_E_BADIMAGEFORMAT);

    mdAssembly a;
    if (FAILED(GetMDImport()->GetAssemblyFromScope(&a)))
        ThrowFormat(COR_E_ASSEMBLYEXPECTED);
}

void DECLSPEC_NORETURN PEImage::ThrowFormat(HRESULT hrError)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EEFileLoadException::Throw(m_path, hrError);
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
            TRUE);                             // BOOL fMakeExecutable

        if (FastInterlockCompareExchangePointer((PVOID*)&m_DllThunkHeap, (VOID*)pNewHeap, (VOID*)0) != 0)
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

                        size_t fileNameLenght = strlen(fileName);
                        size_t fullPathLenght = strlen(pCvInfo->path);
                        memmove(pCvInfo->path, fileName, fileNameLenght);

                        // NULL out the rest of the path buffer.
                        for (size_t i = fileNameLenght; i < MAX_PATH_FNAME - 1; i++)
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
    // No lock here as the processs should be suspended.
    if (m_pLayouts[IMAGE_FLAT].IsValid() && m_pLayouts[IMAGE_FLAT]!=NULL)
        m_pLayouts[IMAGE_FLAT]->EnumMemoryRegions(flags);
    if (m_pLayouts[IMAGE_MAPPED].IsValid() &&  m_pLayouts[IMAGE_MAPPED]!=NULL)
        m_pLayouts[IMAGE_MAPPED]->EnumMemoryRegions(flags);
    if (m_pLayouts[IMAGE_LOADED].IsValid() &&  m_pLayouts[IMAGE_LOADED]!=NULL)
        m_pLayouts[IMAGE_LOADED]->EnumMemoryRegions(flags);
}

#endif // #ifdef DACCESS_COMPILE


PEImage::PEImage():
    m_path(),
    m_refCount(1),
    m_bundleFileLocation(),
    m_bIsTrustedNativeImage(FALSE),
    m_bInHashMap(FALSE),
#ifdef METADATATRACKER_DATA
    m_pMDTracker(NULL),
#endif // METADATATRACKER_DATA
    m_pMDImport(NULL),
    m_pNativeMDImport(NULL),
    m_hFile(INVALID_HANDLE_VALUE),
    m_bOwnHandle(true),
    m_dwPEKind(0),
    m_dwMachine(0),
    m_fCachedKindAndMachine(FALSE)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    for (DWORD i=0;i<COUNTOF(m_pLayouts);i++)
        m_pLayouts[i]=NULL ;
    m_pLayoutLock=new SimpleRWLock(PREEMPTIVE,LOCK_TYPE_DEFAULT);
}

PTR_PEImageLayout PEImage::GetLayout(DWORD imageLayoutMask,DWORD flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    PTR_PEImageLayout pRetVal;

#ifndef DACCESS_COMPILE
    // First attempt to find an existing layout matching imageLayoutMask.  If that fails,
    // and the caller has asked us to create layouts if needed, then try again passing
    // the create flag to GetLayoutInternal.  We need this to be synchronized, but the common
    // case is that the layout already exists, so use a reader-writer lock.
    GCX_PREEMP();
    {
        SimpleReadLockHolder lock(m_pLayoutLock);
        pRetVal=GetLayoutInternal(imageLayoutMask,flags&(~LAYOUT_CREATEIFNEEDED));
    }

    if (!(pRetVal || (flags&LAYOUT_CREATEIFNEEDED)==0))
    {
        SimpleWriteLockHolder lock(m_pLayoutLock);
        pRetVal = GetLayoutInternal(imageLayoutMask,flags);
    }

    return pRetVal;

#else
    // In DAC builds, we can't create any layouts - we must require that they already exist.
    // We also don't take any AddRefs or locks in DAC builds - it's inspection-only.
    pRetVal = GetExistingLayoutInternal(imageLayoutMask);
    if ((pRetVal==NULL) && (flags & LAYOUT_CREATEIFNEEDED))
    {
        _ASSERTE_MSG(false, "DACization error - caller expects PEImage layout to exist and it doesn't");
        DacError(E_UNEXPECTED);
    }
    return pRetVal;
#endif
}

#ifndef DACCESS_COMPILE

PTR_PEImageLayout PEImage::GetLayoutInternal(DWORD imageLayoutMask,DWORD flags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PTR_PEImageLayout pRetVal=GetExistingLayoutInternal(imageLayoutMask);

    if (pRetVal==NULL && (flags&LAYOUT_CREATEIFNEEDED))
    {
        _ASSERTE(HasID());

        BOOL bIsMappedLayoutSuitable = ((imageLayoutMask & PEImageLayout::LAYOUT_MAPPED) != 0);
        BOOL bIsFlatLayoutSuitable = ((imageLayoutMask & PEImageLayout::LAYOUT_FLAT) != 0);

#if !defined(TARGET_UNIX)
        if (!IsInBundle() && bIsMappedLayoutSuitable)
        {
            bIsFlatLayoutSuitable = FALSE;
        }
#endif // !TARGET_UNIX

        _ASSERTE(bIsMappedLayoutSuitable || bIsFlatLayoutSuitable);

        BOOL bIsMappedLayoutRequired = !bIsFlatLayoutSuitable;
        BOOL bIsFlatLayoutRequired = !bIsMappedLayoutSuitable;

        if (bIsFlatLayoutRequired
            || (bIsFlatLayoutSuitable && !m_bIsTrustedNativeImage))
        {
          _ASSERTE(bIsFlatLayoutSuitable);

          BOOL bPermitWriteableSections = bIsFlatLayoutRequired;

          pRetVal = PEImage::CreateLayoutFlat(bPermitWriteableSections);
        }

        if (pRetVal == NULL)
        {
          _ASSERTE(bIsMappedLayoutSuitable);

          pRetVal = PEImage::CreateLayoutMapped();
        }
    }

    if (pRetVal != NULL)
    {
        pRetVal->AddRef();
    }

    return pRetVal;
}

PTR_PEImageLayout PEImage::CreateLayoutMapped()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pLayoutLock->IsWriterLock());
    }
    CONTRACTL_END;

    PTR_PEImageLayout pRetVal;

    PEImageLayout * pLoadLayout = NULL;

    HRESULT loadFailure = S_OK;
    if (m_bIsTrustedNativeImage || IsFile())
    {
        // Try to load all files via LoadLibrary first. If LoadLibrary did not work,
        // retry using regular mapping.
        HRESULT* returnDontThrow = m_bIsTrustedNativeImage ? NULL : &loadFailure;
        pLoadLayout = PEImageLayout::Load(this, FALSE /* bNTSafeLoad */, returnDontThrow);
    }

    if (pLoadLayout != NULL)
    {
        SetLayout(IMAGE_MAPPED,pLoadLayout);
        pLoadLayout->AddRef();
        SetLayout(IMAGE_LOADED,pLoadLayout);
        pRetVal=pLoadLayout;
    }
    else if (IsFile())
    {
        PEImageLayoutHolder pLayout(PEImageLayout::Map(this));

        bool fMarkAnyCpuImageAsLoaded = false;

        // Avoid mapping another image if we can. We can only do this for IL-ONLY images
        // since LoadLibrary is needed if we are to actually load code (e.g. IJW).
        if (pLayout->HasCorHeader())
        {
            // IJW images must be successfully loaded by the OS to handle
            // native dependencies, therefore they cannot be mapped.
            if (!pLayout->IsILOnly())
            {
                // For compat with older CoreCLR versions we will fallback to the
                // COR_E_BADIMAGEFORMAT error code if a failure wasn't indicated.
                loadFailure = FAILED(loadFailure) ? loadFailure : COR_E_BADIMAGEFORMAT;
                EEFileLoadException::Throw(GetPath(), loadFailure);
            }

            // IL only images will always be mapped. We don't bother doing a conversion
            // of PE header on 64bit, as done for .NET Framework, since there is no
            // appcompat burden for CoreCLR on 64bit.
            fMarkAnyCpuImageAsLoaded = true;
        }

        pLayout.SuppressRelease();

        SetLayout(IMAGE_MAPPED,pLayout);
        if (fMarkAnyCpuImageAsLoaded)
        {
            pLayout->AddRef();
            SetLayout(IMAGE_LOADED, pLayout);
        }
        pRetVal=pLayout;
    }
    else
    {
        PEImageLayoutHolder flatPE(GetLayoutInternal(PEImageLayout::LAYOUT_FLAT,LAYOUT_CREATEIFNEEDED));
        if (!flatPE->CheckFormat() || !flatPE->IsILOnly())
            ThrowHR(COR_E_BADIMAGEFORMAT);
        pRetVal=PEImageLayout::LoadFromFlat(flatPE);
        SetLayout(IMAGE_MAPPED,pRetVal);
    }

    return pRetVal;
}

PTR_PEImageLayout PEImage::CreateLayoutFlat(BOOL bPermitWriteableSections)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pLayoutLock->IsWriterLock());
    }
    CONTRACTL_END;

    _ASSERTE(m_pLayouts[IMAGE_FLAT] == NULL);

    PTR_PEImageLayout pFlatLayout = PEImageLayout::LoadFlat(this);

    if (!bPermitWriteableSections
        && pFlatLayout->CheckNTHeaders()
        && pFlatLayout->HasWriteableSections())
    {
        pFlatLayout->Release();

        return NULL;
    }
    else
    {
        m_pLayouts[IMAGE_FLAT] = pFlatLayout;

        return pFlatLayout;
    }
}

/* static */
PTR_PEImage PEImage::LoadFlat(const void *flat, COUNT_T size)
{
    CONTRACT(PTR_PEImage)
    {
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    PEImageHolder pImage(new PEImage());
    PTR_PEImageLayout pLayout = PEImageLayout::CreateFlat(flat,size,pImage);
    _ASSERTE(!pLayout->IsMapped());
    pImage->SetLayout(IMAGE_FLAT,pLayout);
    RETURN dac_cast<PTR_PEImage>(pImage.Extract());
}

#ifndef TARGET_UNIX
/* static */
PTR_PEImage PEImage::LoadImage(HMODULE hMod)
{
    CONTRACT(PTR_PEImage)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(hMod!=NULL);
        POSTCONDITION(RETVAL->HasLoadedLayout());
    }
    CONTRACT_END;

    StackSString path;
    GetPathFromDll(hMod, path);
    PEImageHolder pImage(PEImage::OpenImage(path,(MDInternalImportFlags)(0)));
    if (pImage->HasLoadedLayout())
        RETURN dac_cast<PTR_PEImage>(pImage.Extract());

    SimpleWriteLockHolder lock(pImage->m_pLayoutLock);

    if(pImage->m_pLayouts[IMAGE_LOADED]==NULL)
        pImage->SetLayout(IMAGE_LOADED,PEImageLayout::CreateFromHMODULE(hMod,pImage,WszGetModuleHandle(NULL)!=hMod));

    if(pImage->m_pLayouts[IMAGE_MAPPED]==NULL)
    {
        pImage->m_pLayouts[IMAGE_LOADED]->AddRef();
        pImage->SetLayout(IMAGE_MAPPED,pImage->m_pLayouts[IMAGE_LOADED]);
    }

    RETURN dac_cast<PTR_PEImage>(pImage.Extract());
}
#endif // !TARGET_UNIX

void PEImage::Load()
{
    STANDARD_VM_CONTRACT;

    // Performance optimization to avoid lock acquisition
    if (HasLoadedLayout())
    {
        _ASSERTE(GetLoadedLayout()->IsMapped()||GetLoadedLayout()->IsILOnly());
        return;
    }

    SimpleWriteLockHolder lock(m_pLayoutLock);

    // Re-check after lock is acquired as HasLoadedLayout here and the above line
    // may return a different value in multi-threading environment.
    if (HasLoadedLayout())
    {
        return;
    }

#ifdef TARGET_UNIX
    bool canUseLoadedFlat = true;
#else
    bool canUseLoadedFlat = IsInBundle();
#endif // TARGET_UNIX


    if (canUseLoadedFlat
        && m_pLayouts[IMAGE_FLAT] != NULL
        && m_pLayouts[IMAGE_FLAT]->CheckILOnlyFormat()
        && !m_pLayouts[IMAGE_FLAT]->HasWriteableSections())
    {
        // IL-only images with writeable sections are mapped in general way,
        // because the writeable sections should always be page-aligned
        // to make possible setting another protection bits exactly for these sections
        _ASSERTE(!m_pLayouts[IMAGE_FLAT]->HasWriteableSections());

        // As the image is IL-only, there should no be native code to execute
        _ASSERTE(!m_pLayouts[IMAGE_FLAT]->HasNativeEntryPoint());

        m_pLayouts[IMAGE_FLAT]->AddRef();

        SetLayout(IMAGE_LOADED, m_pLayouts[IMAGE_FLAT]);
    }
    else
    {
        if(!IsFile())
        {
            _ASSERTE(m_pLayouts[IMAGE_FLAT] != NULL);

            if (!m_pLayouts[IMAGE_FLAT]->CheckILOnly())
                ThrowHR(COR_E_BADIMAGEFORMAT);
            if(m_pLayouts[IMAGE_LOADED]==NULL)
                SetLayout(IMAGE_LOADED,PEImageLayout::LoadFromFlat(m_pLayouts[IMAGE_FLAT]));
        }
        else
        {
            if(m_pLayouts[IMAGE_LOADED]==NULL)
                SetLayout(IMAGE_LOADED,PEImageLayout::Load(this,TRUE));
        }
    }
}

void PEImage::SetLoadedHMODULE(HMODULE hMod)
{
    WRAPPER_NO_CONTRACT;
    SimpleWriteLockHolder lock(m_pLayoutLock);
    if(m_pLayouts[IMAGE_LOADED])
    {
        _ASSERTE(m_pLayouts[IMAGE_LOADED]->GetBase()==hMod);
        return;
    }
    SetLayout(IMAGE_LOADED,PEImageLayout::CreateFromHMODULE(hMod,this,TRUE));
}

void PEImage::LoadFromMapped()
{
    STANDARD_VM_CONTRACT;

    if (HasLoadedLayout())
    {
        _ASSERTE(GetLoadedLayout()->IsMapped());
        return;
    }

    PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_MAPPED,LAYOUT_CREATEIFNEEDED));
    SimpleWriteLockHolder lock(m_pLayoutLock);
    if(m_pLayouts[IMAGE_LOADED]==NULL)
        SetLayout(IMAGE_LOADED,pLayout.Extract());
}

void PEImage::LoadNoFile()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!IsFile());
    }
    CONTRACTL_END;
    if (HasLoadedLayout())
        return;

    PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,0));
    if (!pLayout->CheckILOnly())
        ThrowHR(COR_E_BADIMAGEFORMAT);
    SimpleWriteLockHolder lock(m_pLayoutLock);
    if(m_pLayouts[IMAGE_LOADED]==NULL)
        SetLayout(IMAGE_LOADED,pLayout.Extract());
}


void PEImage::LoadNoMetaData()
{
    STANDARD_VM_CONTRACT;

    if (HasLoadedLayout())
        return;

    SimpleWriteLockHolder lock(m_pLayoutLock);
    if (m_pLayouts[IMAGE_LOADED]!=NULL)
        return;
    if (m_pLayouts[IMAGE_FLAT]!=NULL)
    {
        m_pLayouts[IMAGE_FLAT]->AddRef();
        SetLayout(IMAGE_LOADED,m_pLayouts[IMAGE_FLAT]);
    }
    else
    {
        _ASSERTE(!m_path.IsEmpty());
        SetLayout(IMAGE_LOADED,PEImageLayout::LoadFlat(this));
    }
}


#endif //DACCESS_COMPILE

//-------------------------------------------------------------------------------
// Make best-case effort to obtain an image name for use in an error message.
//
// This routine must expect to be called before the this object is fully loaded.
// It can return an empty if the name isn't available or the object isn't initialized
// enough to get a name, but it mustn't crash.
//-------------------------------------------------------------------------------
LPCWSTR PEImage::GetPathForErrorMessages()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END

    return m_path;
}


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

    {
        ErrorModeHolder mode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);
        m_hFile=WszCreateFile((LPCWSTR) GetPathToLoad(),
                               GENERIC_READ,
                               FILE_SHARE_READ|FILE_SHARE_DELETE,
                               NULL,
                               OPEN_EXISTING,
                               FILE_ATTRIBUTE_NORMAL,
                               NULL);
    }

    if (m_hFile == INVALID_HANDLE_VALUE)
    {
#if !defined(DACCESS_COMPILE)
        EEFileLoadException::Throw(GetPathToLoad(), HRESULT_FROM_WIN32(GetLastError()));
#else // defined(DACCESS_COMPILE)
        ThrowLastError();
#endif // !defined(DACCESS_COMPILE)
    }

    return m_hFile;
}

void PEImage::SetFileHandle(HANDLE hFile)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    SimpleWriteLockHolder lock(m_pLayoutLock);
    if (m_hFile == INVALID_HANDLE_VALUE)
    {
        m_hFile = hFile;
        m_bOwnHandle = false;
    }
}

HRESULT PEImage::TryOpenFile()
{
    STANDARD_VM_CONTRACT;

    SimpleWriteLockHolder lock(m_pLayoutLock);

    if (m_hFile!=INVALID_HANDLE_VALUE)
        return S_OK;
    {
        ErrorModeHolder mode(SEM_NOOPENFILEERRORBOX | SEM_FAILCRITICALERRORS);
        m_hFile=WszCreateFile((LPCWSTR)GetPathToLoad(), 
                              GENERIC_READ,
                              FILE_SHARE_READ|FILE_SHARE_DELETE,
                              NULL,
                              OPEN_EXISTING,
                              FILE_ATTRIBUTE_NORMAL,
                              NULL);
    }
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

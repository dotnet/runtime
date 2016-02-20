// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEImage.cpp
// 

// --------------------------------------------------------------------------------


#include "common.h"

#include "peimage.h"
#include "eeconfig.h"
#include "apithreadstress.h"
#include <objbase.h>

#include "sha1.h"
#include "eventtrace.h"
#include "peimagelayout.inl"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#ifndef DACCESS_COMPILE


CrstStatic  PEImage::s_hashLock;
PtrHashMap *PEImage::s_Images = NULL;
#ifdef FEATURE_MIXEDMODE
CrstStatic  PEImage::s_ijwHashLock;
PtrHashMap *PEImage::s_ijwFixupDataHash;
#endif

extern LocaleID g_lcid; // fusion path comparison lcid

/* static */
void PEImage::Startup()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        POSTCONDITION(CheckStartup());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (CheckStartup())
        RETURN;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(COMPlusThrowSO());

    s_hashLock.Init(CrstPEImage, (CrstFlags)(CRST_REENTRANCY|CRST_TAKEN_DURING_SHUTDOWN));
    LockOwner lock = { &s_hashLock, IsOwnerOfCrst };
    s_Images         = ::new PtrHashMap;
    s_Images->Init(CompareImage, FALSE, &lock);
#ifdef FEATURE_MIXEDMODE
    s_ijwHashLock.Init(CrstIJWHash, CRST_REENTRANCY);
    LockOwner ijwLock = { &s_ijwHashLock, IsOwnerOfCrst };
    s_ijwFixupDataHash = ::new PtrHashMap;
    s_ijwFixupDataHash->Init(CompareIJWDataBase, FALSE, &ijwLock);
#endif
    PEImageLayout::Startup();
#ifndef FEATURE_FUSION
#ifdef FEATURE_USE_LCID
    g_lcid = MAKELCID(LOCALE_INVARIANT, SORT_DEFAULT);
#else // FEATURE_USE_LCID
    g_lcid = NULL; // invariant
#endif //FEATURE_USE_LCID
#endif
    END_SO_INTOLERANT_CODE;

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

    // If we are in a compilation domain, we will allow
    // non-IL only files to be treated as IL only

    // <TODO>@todo: this is not really the right model here.  This is a per-app domain
    // choice, but an image created this way would become available globally.
    // (Also, this call prevents us from moving peimage into utilcode.)</TODO>

    if (GetAppDomain() == NULL ||
        (!GetAppDomain()->IsCompilationDomain()))
    {
        CHECK(pe->IsILOnly());
    }

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

#ifdef FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
    if (PEFile::ShouldTreatNIAsMSIL())
    {
        // This PEImage may intentionally be an NI image, being used as if it were an
        // MSIL image.  In that case, rather than using CheckILFormat on its layout,
        // do CheckCORFormat(), which is the same as CheckILFormat, except it allows for
        // a native header.  (CheckILFormat() fails if it finds a native header.)
        CHECK(pLayoutToCheck->CheckCORFormat());
    }
    else
#endif // FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
    {
        CHECK(pLayoutToCheck->CheckILFormat());
    }

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

/* static */
ULONG PEImage::HashStreamIds(UINT64 id1, DWORD id2)
{
    LIMITED_METHOD_CONTRACT;

    ULONG hash = 5381;

    hash ^= id2;
    hash = _rotl(hash, 4);

    void *data = &id1;
    hash ^= *(INT32 *) data;

    hash = _rotl(hash, 4);
    ((INT32 *&)data)++;
    hash ^= *(INT32 *) data;

    return hash;
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

#ifdef FEATURE_FUSION
    delete m_pILFingerprint;
#endif // FEATURE_FUSION
}

#ifdef FEATURE_MIXEDMODE
/* static */
BOOL PEImage::CompareIJWDataBase(UPTR base, UPTR mapping)
{
    CONTRACTL {
        PRECONDITION(CheckStartup());
        PRECONDITION(CheckPointer((BYTE *) (base<<1)));
        PRECONDITION(CheckPointer((IJWFixupData *)mapping));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    return ((BYTE *) (base<<1) == ((IJWFixupData*)mapping)->GetBase());
}
#endif // FEATURE_MIXEDMODE

    // Thread stress
#if 0
class OpenFileStress : APIThreadStress
    {
      public:
        const SString &path;
    PEImage::Layout layout;
    OpenFileStress(const SString &path, PEImage::Layout layout)
          : path(path), layout(layout)
        {
            WRAPPER_NO_CONTRACT;

            path.Normalize();
        }
        void Invoke()
        {
            WRAPPER_NO_CONTRACT;

            PEImageHolder result(PEImage::Open(path, layout));
        }
};
#endif

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

#ifdef FEATURE_LAZY_COW_PAGES
    if (result == 0 && m_bAllocatedLazyCOWPages)
        ::FreeLazyCOWPages(GetLoadedLayout());
#endif

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

#ifdef FEATURE_USE_LCID
LCID g_lcid =0; // fusion path comparison lcid
#else
LPCWSTR g_lcid=NULL;
#endif
/* static */
LocaleID PEImage::GetFileSystemLocale()
{
    LIMITED_METHOD_CONTRACT;
    return g_lcid;
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
    return p1.EqualsCaseInsensitive(p2, g_lcid);
#endif
}

#ifndef FEATURE_PAL
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
#endif // !FEATURE_PAL

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

#ifdef FEATURE_FUSION
    if (pLocator->m_fIsIStream)
    {
        return pImage->m_fIsIStream && (pLocator->m_StreamAsmId == pImage->m_StreamAsmId) && (pLocator->m_dwStreamModuleId == pImage->m_dwStreamModuleId);
    }
#endif

    BOOL ret = FALSE;
    HRESULT hr;
    EX_TRY
    {
        SString path(SString::Literal, pLocator->m_pPath);
        if (PathEquals(path, pImage->GetPath()))
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

#ifdef FEATURE_CORECLR
    // PEImage is always unique on CoreCLR so a simple pointer check is sufficient
    _ASSERTE(m_bInHashMap || GetPath().IsEmpty());
    _ASSERTE(pImage->m_bInHashMap || pImage->GetPath().IsEmpty());

    return dac_cast<TADDR>(pImage) == dac_cast<TADDR>(this);
#else // FEATURE_CORECLR
    if (pImage == this)
        return TRUE;

    if (GetPath().IsEmpty())
    {
#ifdef FEATURE_FUSION
        if (m_fIsIStream && pImage->m_fIsIStream)
        {
            return (m_StreamAsmId == pImage->m_StreamAsmId) && (m_dwStreamModuleId == pImage->m_dwStreamModuleId);
        }
#endif

        return FALSE;
    }
    else
    {
        BOOL ret = FALSE;
        HRESULT hr;
        EX_TRY
        {
            if (PathEquals(GetPath(), pImage->GetPath()))
                ret = TRUE;
        }
        EX_CATCH_HRESULT(hr); //<TODO>ignores failure!</TODO>
        return ret;
    }
#endif // FEATURE_CORECLR
}

#ifndef FEATURE_CORECLR
void PEImage::ComputeHash(ALG_ID algorithm, SBuffer &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckStartup());
        PRECONDITION(CheckValue(result));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    PEImageLayoutHolder pview(GetLayout(PEImageLayout::LAYOUT_FLAT,PEImage::LAYOUT_CREATEIFNEEDED));

    if (algorithm == CALG_SHA1)
    {
        SHA1Hash hasher;
        hasher.AddData((BYTE *) pview->GetBase(), pview->GetSize());
        result.Set(hasher.GetHash(), SHA1_HASH_SIZE);
        return;
    }

    DWORD size = 0;
    if(!StrongNameHashSize(algorithm, &size))
    {
        ThrowHR(StrongNameErrorInfo());
    }

    BYTE *buffer = result.OpenRawBuffer(size);
    
    DWORD hashSize;
    IfFailThrow(GetHashFromBlob((BYTE *) pview->GetBase(), pview->GetSize(), &algorithm, buffer, size, &hashSize));

    _ASSERTE(size == hashSize);

    result.CloseRawBuffer(hashSize);
}

CHECK PEImage::CheckHash(ALG_ID algorithm, const void *pbHash, COUNT_T cbHash)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckStartup());
        INSTANCE_CHECK;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    StackSBuffer hash;
    ComputeHash(algorithm, hash);

    CHECK(hash.Equals((const BYTE *) pbHash, cbHash));

    CHECK_OK;
}
#endif // FEATURE_CORECLR

IMDInternalImport* PEImage::GetMDImport()
{
    WRAPPER_NO_CONTRACT;
    if (!m_pMDImport)
        OpenMDImport();
    return m_pMDImport;
}

#ifdef FEATURE_PREJIT
IMDInternalImport* PEImage::GetNativeMDImport(BOOL loadAllowed)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeHeader());
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
        PRECONDITION(HasNativeHeader());
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
#endif

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

void PEImage::GetHashedStrongNameSignature(SBuffer &result)
{
    COUNT_T size;
    const void *sig = GetStrongNameSignature(&size);

    SHA1Hash hasher;
    hasher.AddData((BYTE *) sig, size);
    result.Set(hasher.GetHash(), SHA1_HASH_SIZE);
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
    if (fIL)
    {
        checkGoodFormat = CheckILFormat();
    }
    else
    {
        checkGoodFormat = CheckNativeFormat();
    }
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

#ifdef FEATURE_FUSION
// --------------------------------------------------------------------------------
// Exports for the metadata APIs for fusion.
// --------------------------------------------------------------------------------

HRESULT STDMETHODCALLTYPE RuntimeOpenImage(LPCWSTR pszFileName, HCORMODULE* hHandle)
{
    WRAPPER_NO_CONTRACT;
    return RuntimeOpenImageInternal(pszFileName, hHandle, NULL, MDInternalImport_Default);
}

HRESULT STDMETHODCALLTYPE RuntimeOpenImageInternal(LPCWSTR pszFileName, HCORMODULE* hHandle, DWORD *pdwLength, MDInternalImportFlags flags, HANDLE hFile)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_INTOLERANT;
    HRESULT hr = S_OK;
    ETWOnStartup (LoaderCatchCall_V1,LoaderCatchCallEnd_V1);
    EX_TRY
    {
        PEImage::Startup();
        PEImageHolder pFile(PEImage::OpenImage(pszFileName, flags));
        if (hFile != INVALID_HANDLE_VALUE)
        {
            pFile->SetFileHandle(hFile);
        }
        if (pdwLength)
        {
            PEImageLayoutHolder pLayout(pFile->GetLayout(PEImageLayout::LAYOUT_MAPPED,PEImage::LAYOUT_CREATEIFNEEDED));
            pFile->CachePEKindAndMachine();
            *pdwLength = pLayout->GetSize();
        }
        *hHandle = (HCORMODULE)pFile.Extract();        
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT STDMETHODCALLTYPE RuntimeOpenImageByStream(IStream* pIStream, UINT64 AssemblyId,
                                                   DWORD dwModuleId,
                                                   HCORMODULE* hHandle, DWORD *pdwLength, MDInternalImportFlags flags)
{
    STATIC_CONTRACT_NOTHROW;
    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    EX_TRY
    {
        PEImage::Startup();

        PEImageHolder pFile(PEImage::OpenImage(pIStream, AssemblyId, dwModuleId, FALSE, flags));
        *hHandle = (HCORMODULE) pFile.Extract();
        if (pdwLength)
        {
            PEImageLayoutHolder pImage(pFile->GetLayout(PEImageLayout::LAYOUT_ANY,0));
            pFile->CachePEKindAndMachine();
            *pdwLength = pImage->GetSize();
        }
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE;

    return hr;
}

HRESULT STDMETHODCALLTYPE RuntimeReleaseHandle(HCORMODULE hHandle)
{
    STATIC_CONTRACT_NOTHROW;
    HRESULT hr = S_OK;

    PEImage *pImage = (PEImage*)hHandle;

    if (pImage != NULL)
        pImage->Release();

    return hr;     
}

void RuntimeAddRefHandle(HCORMODULE hHandle)
{
    STATIC_CONTRACT_NOTHROW;

    PEImage *pImage = (PEImage*)hHandle;

    if (pImage != NULL)
        pImage->AddRef();
}

HRESULT STDMETHODCALLTYPE RuntimeGetMDInternalImport(HCORMODULE hHandle, MDInternalImportFlags flags, IMDInternalImport** ppMDImport)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    PEImage* pImage=(PEImage*)hHandle;
    HRESULT hr=S_OK;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    EX_TRY
    {
        if (!pImage->HasNTHeaders() || !pImage->HasCorHeader())
            hr=HRESULT_FROM_WIN32(ERROR_FILE_INVALID);
        else
        {
#ifdef FEATURE_PREJIT
            if (pImage->HasNativeHeader())
            {
                if (!pImage->CheckNativeFormat())
                    hr=COR_E_BADIMAGEFORMAT;
                else
                {
                    if (flags & MDInternalImport_ILMetaData)
                        goto OPEN_IL_METADATA;

                    *ppMDImport=pImage->GetNativeMDImport();
                    if (*ppMDImport)
                        (*ppMDImport)->AddRef();
                    else
                        hr=COR_E_BADIMAGEFORMAT;
                }
            }
            else
#endif  //FEATURE_PREJIT
            {
                if (!pImage->CheckILFormat())
                    hr=COR_E_BADIMAGEFORMAT;
                else
                {
#ifdef FEATURE_PREJIT                    
                OPEN_IL_METADATA:
#endif                    
                    *ppMDImport=pImage->GetMDImport();
                    if (*ppMDImport)
                        (*ppMDImport)->AddRef();
                    else
                        hr=COR_E_BADIMAGEFORMAT;
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE;

    return hr;
}

HRESULT STDMETHODCALLTYPE RuntimeGetImageBase(HCORMODULE hHandle,LPVOID* base, BOOL bMapped, COUNT_T* dwSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    HRESULT hr=S_FALSE;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    EX_TRY
    {
        PEImage* pImage=(PEImage*)hHandle;
        *base=NULL;
        if (!pImage->HasLoadedLayout())
        {
            PEImageLayoutHolder pLayout(pImage->GetLayout(bMapped
                                                            ?PEImageLayout::LAYOUT_MAPPED
                                                            :PEImageLayout::LAYOUT_FLAT,0));
            if (pLayout!=NULL)
            {
                if(dwSize)
                    *dwSize=pLayout->GetSize();
                *base=pLayout->GetBase();
                hr=S_OK;
            }
        }

        if (hr==S_FALSE && pImage->HasLoadedLayout())
        {
            BOOL bIsMapped=pImage->GetLoadedLayout()->IsMapped();
            if ((bIsMapped && bMapped) || (!bIsMapped && !bMapped))
            {
                //the one we want
                *base=pImage->GetLoadedLayout()->GetBase();
                if (dwSize)
                    *dwSize=pImage->GetLoadedLayout()->GetSize();
                hr=S_OK;
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE;

    return hr;
}

HRESULT STDMETHODCALLTYPE RuntimeGetImageKind(HCORMODULE hHandle,DWORD* pdwKind, DWORD* pdwMachine)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    HRESULT hr=S_FALSE;

    PEImage* pImage=(PEImage*)hHandle;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    EX_TRY
    {
        pImage->GetPEKindAndMachine(pdwKind, pdwMachine);
        hr = S_OK;
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE;
    return hr;
}

HRESULT STDMETHODCALLTYPE RuntimeOSHandle(HCORMODULE hHandle, HMODULE* hModule)
{
    LIMITED_METHOD_CONTRACT;
    if(hHandle==NULL || hModule == NULL)
        return E_POINTER;
    PEImage* pImage= (PEImage*) hHandle;
    if (!pImage->HasLoadedLayout())
        return HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
    *hModule=(HMODULE)pImage->GetLoadedLayout()->GetBase();
    return S_OK;
}

HRESULT RuntimeGetAssemblyStrongNameHashForModule(HCORMODULE   hModule,
                                                  IMetaDataImport * pMDImport,
                                                  BYTE        *pbSNHash,
                                                  DWORD       *pcbSNHash)
{
    STATIC_CONTRACT_NOTHROW;
    HRESULT hr = S_OK;

    PEImage* pImage = (PEImage*)hModule;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);
    EX_TRY
    {

        if (pImage->HasStrongNameSignature())
        {
            if (pImage->IsStrongNameSigned())
            {
                SBuffer signature;
                pImage->GetHashedStrongNameSignature(signature);
                *pcbSNHash = min(signature.GetSize(), *pcbSNHash);
                signature.Copy(pbSNHash, signature.Begin(), *pcbSNHash);
            }
            else
            {
                // This assembly is delay signed (in this limited scenario).
                // We'll use the assembly MVID as the hash and leave assembly verification
                // up to the loader to determine if delay signed assemblies are allowed.
                // This allows us to fix the perf degrade observed with the hashing code and
                // detailed in BUG 126760.

                // <TODO>@TODO:workaround: This is a workaround because Fusion is expecting at least 20 bytes of data.</TODO>
                if (max(sizeof(GUID), 20) <= *pcbSNHash)
                {
                    memset(pbSNHash, 0, *pcbSNHash);
                    hr = pMDImport->GetScopeProps(NULL, 0, NULL, (GUID *) pbSNHash);
                }
                else
                    hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);

                *pcbSNHash = max(sizeof(GUID), 20);
            }
        }
        else
        {
            hr = CORSEC_E_MISSING_STRONGNAME;
        }
    }
    EX_CATCH_HRESULT(hr);
    END_SO_INTOLERANT_CODE;

    return hr;
}

#endif // FEATURE_FUSION


#if defined(FEATURE_MIXEDMODE) && !defined(CROSSGEN_COMPILE)

//may outlive PEImage
PEImage::IJWFixupData::IJWFixupData(void *pBase)
    : m_lock(CrstIJWFixupData),
      m_base(pBase), m_flags(0),m_DllThunkHeap(NULL),m_iNextFixup(0),m_iNextMethod(0)
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
    CONTRACT (LoaderHeap *)
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
        size_t * pPrivatePCLBytes = NULL;
        size_t * pGlobalPCLBytes  = NULL;

#ifdef PROFILING_SUPPORTED
        pPrivatePCLBytes   = &(GetPerfCounters().m_Loading.cbLoaderHeapSize);
#endif

        LoaderHeap *pNewHeap = new LoaderHeap(VIRTUAL_ALLOC_RESERVE_GRANULARITY, // DWORD dwReserveBlockSize
                                              0,                                 // DWORD dwCommitBlockSize
                                              pPrivatePCLBytes,
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
    _ASSERTE( (iFixup == m_iNextFixup+1 && iMethod ==0) ||                 //first method of the next fixup or
               (iFixup == m_iNextFixup && iMethod == m_iNextMethod) );     //the method that was next to fixup

    m_iNextFixup = iFixup;
    m_iNextMethod = iMethod+1;
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
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END

    // Take the IJW hash lock
    CrstHolder hashLockHolder(&s_ijwHashLock);

    // Try to find the data
    IJWFixupData *pData = (IJWFixupData *)s_ijwFixupDataHash->LookupValue((UPTR) pBase, pBase);

    // No data, must create
    if ((UPTR)pData == (UPTR)INVALIDENTRY)
    {
        pData = new IJWFixupData(pBase);
        s_ijwFixupDataHash->InsertValue((UPTR) pBase, pData);
    }

    // Return the new data
    return (pData);
}

/* static */
void PEImage::UnloadIJWModule(void *pBase)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END

    // Take the IJW hash lock
    CrstHolder hashLockHolder(&s_ijwHashLock);

    // Try to delete the hash entry
    IJWFixupData *pData = (IJWFixupData *)s_ijwFixupDataHash->DeleteValue((UPTR) pBase, pBase);

    // Now delete the data
    if ((UPTR)pData != (UPTR)INVALIDENTRY)
        delete pData;

}

#endif // FEATURE_MIXEDMODE && !CROSSGEN_COMPILE

#ifdef FEATURE_FUSION
void PEImage::Init(IStream* pIStream, UINT64 uAsmStreamId,
                   DWORD dwModuleId, BOOL resourceFile)
{
    CONTRACT_VOID
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckStartup());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    m_StreamAsmId = uAsmStreamId;
    m_dwStreamModuleId = dwModuleId;
    m_fIsIStream = TRUE;

    LOG((LF_LOADER, LL_INFO100, "PEImage: Opening flat stream\n"));

    if (!pIStream)
        ThrowHR(COR_E_FILELOAD);

    // Just copy bytes.

    PEImageLayoutHolder pFlatLayout(PEImageLayout::CreateFromStream(pIStream, this));

    if (!resourceFile) {
        if (!pFlatLayout->CheckCORFormat())
            ThrowFormat(COR_E_BADIMAGEFORMAT);

        if (!CheckLayoutFormat(pFlatLayout))
            ThrowHR(COR_E_NOTSUPPORTED);
    }

    pFlatLayout.SuppressRelease();
    SetLayout(IMAGE_FLAT, pFlatLayout);

    RETURN;
}
#endif // FEATURE_FUSION

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

                        if (pCvInfo == NULL || pCvInfo->path == NULL)
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
    if (m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION].IsValid() &&  m_pLayouts[IMAGE_LOADED]!=NULL)
        m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION]->EnumMemoryRegions(flags);
}

#endif // #ifdef DACCESS_COMPILE


PEImage::PEImage():
    m_refCount(1),
    m_bIsTrustedNativeImage(FALSE),
    m_bIsNativeImageInstall(FALSE),
    m_bPassiveDomainOnly(FALSE),
#ifndef FEATURE_CORECLR
    m_fReportedToUsageLog(FALSE),
#endif // !FEATURE_CORECLR
    m_bInHashMap(FALSE),
#ifdef METADATATRACKER_DATA
    m_pMDTracker(NULL),
#endif // METADATATRACKER_DATA
    m_pMDImport(NULL),
    m_pNativeMDImport(NULL),
#ifdef FEATURE_FUSION
    m_StreamAsmId(0),
    m_dwStreamModuleId(0),
    m_fIsIStream(FALSE),
#endif
    m_hFile(INVALID_HANDLE_VALUE),
    m_bOwnHandle(true),
    m_bSignatureInfoCached(FALSE),
    m_hrSignatureInfoStatus(E_UNEXPECTED),
    m_dwSignatureInfo(0),
#ifdef FEATURE_FUSION
    m_pILFingerprint(NULL),
#endif //FEATURE_FUSION
    m_dwPEKind(0),
    m_dwMachine(0),
    m_fCachedKindAndMachine(FALSE)
#ifdef FEATURE_APTCA
    , m_fMayBeConditionalAptca(TRUE)
#endif // FEATURE_APTCA
#ifdef FEATURE_LAZY_COW_PAGES
    ,m_bAllocatedLazyCOWPages(FALSE)
#endif // FEATURE_LAZY_COW_PAGES
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
    BEGIN_SO_INTOLERANT_CODE(GetThread());
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
    END_SO_INTOLERANT_CODE;
    
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

        if (imageLayoutMask&PEImageLayout::LAYOUT_MAPPED)
        {
            PEImageLayout * pLoadLayout = NULL;

#ifdef FEATURE_CORECLR
            if (m_bIsTrustedNativeImage || IsFile())
            {
                // For CoreCLR, try to load all files via LoadLibrary first. If LoadLibrary did not work, retry using 
                // regular mapping - but not for native images.
                pLoadLayout = PEImageLayout::Load(this, TRUE /* bNTSafeLoad */, m_bIsTrustedNativeImage /* bThrowOnError */);
            }
#else
            if (m_bIsTrustedNativeImage)
            {
                pLoadLayout = PEImageLayout::Load(this, FALSE);
            }
            else if (m_bIsNativeImageInstall)
            {
                // When ESB (extended secure boot) is enabled, a native image that is being installed can
                // only be loaded flat.
                PEImageLayout* pFlatLayout=PEImageLayout::LoadFlat(GetFileHandle(),this);
                SetLayout(IMAGE_FLAT,pFlatLayout);
                pLoadLayout = new ConvertedImageLayout(pFlatLayout);
            }
#ifdef FEATURE_READYTORUN
            else if (ReadyToRunInfo::IsReadyToRunEnabled() && IsFile())
            {
                pLoadLayout = PEImageLayout::Load(this, FALSE, FALSE);
            }
#endif // FEATURE_READYTORUN

#endif // FEATURE_CORECLR

            if (pLoadLayout != NULL)
            {
                SetLayout(IMAGE_MAPPED,pLoadLayout);
                pLoadLayout->AddRef();
                SetLayout(IMAGE_LOADED,pLoadLayout);
                pRetVal=pLoadLayout;
            }
            else
            if (IsFile())
            {
                PEImageLayoutHolder pLayout(PEImageLayout::Map(GetFileHandle(),this));

                bool fMarkAnyCpuImageAsLoaded = false;
                // Avoid mapping another image if we can.   We can only do this for IL-ONLY images
                // since LoadLibrary is needed if we are to actually load code.  
                if (pLayout->HasCorHeader() && pLayout->IsILOnly())
                {    
#ifdef FEATURE_CORECLR
                    // For CoreCLR, IL only images will always be mapped. We also dont bother doing the conversion of PE header on 64bit,
                    // as done below for the desktop case, as there is no appcompat burden for CoreCLR on 64bit to have that conversion done.
                    fMarkAnyCpuImageAsLoaded = true;
#else // !FEATURE_CORECLR

#ifdef _WIN64
                    // When attempting to load an assembly using LoadLibrary on x64,
                    // the execution will go via the shell-shim that will try to determine
                    // if the assembly is ILOnly with Pe32 header (i.e. built as anycpu). If it is,
                    // it will convert the in-memory PEheader of the image to be PE32+ (i.e. mark it as 64bit image).
                    //
                    // Since we are trying to avoid mapping twice for ILOnly images by simply memory mapping them,
                    // we should emulate the shell-shim behaviour for 64bit. This will allow inproc-components (e.g. ASP.NET),
                    // which check for Pe32+ header, to continue working as expected.
                    //
                    // If we fail for some reason to change the header, in retail build, we will simply fallback to the double-loading behaviour without
                    // any functional problems.
                    if (pLayout->Has32BitNTHeaders())
                    {
                        fMarkAnyCpuImageAsLoaded = pLayout->ConvertILOnlyPE32ToPE64();
                    }
                    else
                    {
                        // Before assuming that PE32+ file can be loaded, confirm that
                        // it is the expected machine type. This will ensure AMD64 does not load ARM64 or IA64 assemblies (and likewise).
                        // If the machine type does not match, the Loader will fail the load at a later point.
                        if (pLayout->GetMachine() == IMAGE_FILE_MACHINE_NATIVE)
                        {
                            fMarkAnyCpuImageAsLoaded = true; // PE32+ (aka native 64bit) binaries dont require any extra processing.
                        }
                    }
#else // !_WIN64
                    // Why can we not blindly assume that on 32bit OS, image should always be loaded? This is because it is possible to load
                    // PE32+ image and map it to the 32bit process in WOW64.
                    if (pLayout->Has32BitNTHeaders())
                        fMarkAnyCpuImageAsLoaded = true;
#endif // _WIN64

#endif // FEATURE_CORECLR
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
                if (!flatPE->CheckFormat())
                    ThrowFormat(COR_E_BADIMAGEFORMAT);
                pRetVal=PEImageLayout::LoadFromFlat(flatPE);
                SetLayout(IMAGE_MAPPED,pRetVal);
            }
        }
        else
        if (imageLayoutMask&PEImageLayout::LAYOUT_FLAT)
        {
#ifdef FEATURE_FUSION
            _ASSERTE(!m_fIsIStream); //images created from streams should always have this one
#endif
            pRetVal=PEImageLayout::LoadFlat(GetFileHandle(),this);
            m_pLayouts[IMAGE_FLAT]=pRetVal;
        }
        
    }
    if (pRetVal)
    {
        pRetVal->AddRef();
    }
    return pRetVal;
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

#ifndef FEATURE_PAL
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
    PEImageHolder pImage(PEImage::OpenImage(path,(MDInternalImportFlags)(MDInternalImport_CheckLongPath|MDInternalImport_CheckShortPath)));
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
#endif // !FEATURE_PAL

void PEImage::Load()
{
    STANDARD_VM_CONTRACT;

    if (HasLoadedLayout())
    {
        _ASSERTE(GetLoadedLayout()->IsMapped()||GetLoadedLayout()->IsILOnly());
        return;
    }

    SimpleWriteLockHolder lock(m_pLayoutLock);
    if(!IsFile())
    {
        if (!m_pLayouts[IMAGE_FLAT]->CheckILOnly())
            ThrowHR(COR_E_BADIMAGEFORMAT);
        if(m_pLayouts[IMAGE_LOADED]==NULL)
            SetLayout(IMAGE_LOADED,PEImageLayout::LoadFromFlat(m_pLayouts[IMAGE_FLAT]));
    }
    else
    {
#ifdef FEATURE_CORECLR
        if(m_pLayouts[IMAGE_LOADED]==NULL)
            SetLayout(IMAGE_LOADED,PEImageLayout::Load(this,TRUE));
#else

        //as part of Load() call we may initialize loaded image in DllMain
        //so we have to leave the lock and be prepared that when PEImageLayout::Load returns
        //m_pLayouts[IMAGE_LOADED] is set to something else
        lock.Release();

        FileHandleHolder pProtect=GetProtectingFileHandle(FALSE);

        // if the image is IL-only, try to load it in the safe manner 
        
        // using the Internal function here because we are under the writer lock
        PEImageLayoutHolder pLayout=GetLayoutInternal(PEImageLayout::LAYOUT_ANY,0);
        BOOL bPreferSafeLoad=(pLayout && pLayout->IsILOnly());

        // Always use safe load during NGen to avoid running unmanaged code in IJW assemblies
        if (IsCompilationProcess())
            bPreferSafeLoad = TRUE;

        PEImageLayoutHolder pLoaded(PEImageLayout::Load(this,bPreferSafeLoad)); 

        lock.Acquire();

        if(m_pLayouts[IMAGE_LOADED]==NULL)
            SetLayout(IMAGE_LOADED,pLoaded.Extract());

#endif // FEATURE_CORECLR
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

void PEImage::LoadForIntrospection()
{
    STANDARD_VM_CONTRACT;

    if (HasLoadedIntrospectionLayout())
        return;

    PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
    SimpleWriteLockHolder lock(m_pLayoutLock);
    if(m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION]==NULL)
        SetLayout(IMAGE_LOADED_FOR_INTROSPECTION,pLayout.Extract());
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


void PEImage::LoadNoMetaData(BOOL bIntrospection)
{
    STANDARD_VM_CONTRACT;

     if (bIntrospection)
     {
        if (HasLoadedIntrospectionLayout())
            return;
     }
     else
         if (HasLoadedLayout())
            return;

    SimpleWriteLockHolder lock(m_pLayoutLock);
    int layoutKind=bIntrospection?IMAGE_LOADED_FOR_INTROSPECTION:IMAGE_LOADED;
    if (m_pLayouts[layoutKind]!=NULL)
        return;
    if (m_pLayouts[IMAGE_FLAT]!=NULL)
    {
        m_pLayouts[IMAGE_FLAT]->AddRef();
        SetLayout(layoutKind,m_pLayouts[IMAGE_FLAT]);
    }
    else
    {
        _ASSERTE(!m_path.IsEmpty());
        SetLayout(layoutKind,PEImageLayout::LoadFlat(GetFileHandle(),this));
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

#ifdef FEATURE_FUSION
PEKIND PEImage::GetFusionProcessorArchitecture()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD dwPEKind, dwMachine;
    GetPEKindAndMachine(&dwPEKind, &dwMachine);

    DWORD dwAssemblyFlags = 0;

    IfFailThrow(m_pMDImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly),
                                              NULL, NULL, NULL,
                                              NULL, NULL, &dwAssemblyFlags));

    PEKIND retval;
    if (FAILED(TranslatePEToArchitectureType(
            (CorPEKind)dwPEKind,
            dwMachine,
            dwAssemblyFlags,
            &retval)))
    {
        return peInvalid;
    }
    return retval;
}
#endif //FEATURE_FUSION

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
        m_hFile=WszCreateFile((LPCWSTR) m_path,
                                            GENERIC_READ,
                                            FILE_SHARE_READ|FILE_SHARE_DELETE,
                                            NULL,
                                            OPEN_EXISTING,
                                            FILE_ATTRIBUTE_NORMAL,
                                            NULL);
    }

    if (m_hFile == INVALID_HANDLE_VALUE)
        ThrowLastError();

    return m_hFile;
}

// Like GetFileHandle, but can be called without the PEImage being locked for writing.
// Only intend to be called by NGen.
HANDLE PEImage::GetFileHandleLocking()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    if (m_hFile!=INVALID_HANDLE_VALUE)
        return m_hFile;

    SimpleWriteLockHolder lock(m_pLayoutLock);
    return GetFileHandle();
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
        ErrorModeHolder mode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);
        m_hFile=WszCreateFile((LPCWSTR) m_path,
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



HANDLE PEImage::GetProtectingFileHandle(BOOL bProtectIfNotOpenedYet)
{
    STANDARD_VM_CONTRACT;

    if (m_hFile==INVALID_HANDLE_VALUE && !bProtectIfNotOpenedYet)
        return INVALID_HANDLE_VALUE;

    HANDLE hRet=INVALID_HANDLE_VALUE;
    {
        ErrorModeHolder mode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);
        hRet=WszCreateFile((LPCWSTR) m_path,
                                            GENERIC_READ,
                                            FILE_SHARE_READ,
                                            NULL,
                                            OPEN_EXISTING,
                                            FILE_ATTRIBUTE_NORMAL,
                                            NULL);
    }
    if (hRet == INVALID_HANDLE_VALUE)
        ThrowLastError();
    if (m_hFile!=INVALID_HANDLE_VALUE && !CompareFiles(m_hFile,hRet))
        ThrowHR(FUSION_E_REF_DEF_MISMATCH);

    return hRet;
}

BOOL PEImage::IsPtrInImage(PTR_CVOID data)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
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

#ifdef FEATURE_FUSION
#ifndef DACCESS_COMPILE
HRESULT PEImage::GetILFingerprint(IILFingerprint **ppFingerprint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    *ppFingerprint = NULL;
    if (m_pILFingerprint == NULL)
    {
        HRESULT hr = S_OK;
        NewHolder<PEFingerprint> pNewFingerprint;
        EX_TRY
        {
            pNewFingerprint = PEFingerprint::CreatePEFingerprint(this);
            hr = S_OK;
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
        {
            return hr;
        }

        if (InterlockedCompareExchangeT(&m_pILFingerprint, (PEFingerprint*)(pNewFingerprint.GetValue()), NULL) == NULL)
        {
            pNewFingerprint.SuppressRelease(); // Won the race
        }
    }

    *ppFingerprint = m_pILFingerprint;
    (*ppFingerprint)->AddRef();
    return S_OK;
}

// NOTE: Performance critical codepaths should cache the result of this function.
HRESULT RuntimeGetILFingerprintForPath(LPCWSTR path, IILFingerprint **ppFingerprint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    HRESULT hr;
    HCORMODULE hCorModule;
    IfFailGo(RuntimeOpenImageInternal(path, &hCorModule, NULL, MDInternalImport_NoCache));
    {
        ReleaseHolder<PEImage> peImage((PEImage*)hCorModule);
        IfFailGo(peImage->GetILFingerprint(ppFingerprint));
    }
    hr = S_OK;
 ErrExit:
    return hr;
}

#endif //!DACCESS_COMPILE
#endif //FEATURE_FUSION

#if defined(FEATURE_HOSTED_BINDER) && !defined(DACCESS_COMPILE)
PEImage * PEImage::OpenImage(
    ICLRPrivResource * pIResource,
    MDInternalImportFlags flags)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    PEImageHolder pPEImage;


    IID iidResource;
    IfFailThrow(pIResource->GetResourceType(&iidResource));

    if (iidResource == __uuidof(ICLRPrivResourcePath))
    {
        ReleaseHolder<ICLRPrivResourcePath> pIResourcePath;
        IfFailThrow(pIResource->QueryInterface(__uuidof(ICLRPrivResourcePath), (LPVOID*)&pIResourcePath));
        WCHAR wzPath[_MAX_PATH];
        DWORD cchPath = NumItems(wzPath);
        IfFailThrow(pIResourcePath->GetPath(cchPath, &cchPath, wzPath));
        pPEImage = PEImage::OpenImage(wzPath, flags);
    }
#ifndef FEATURE_PAL
    else if (iidResource ==__uuidof(ICLRPrivResourceHMODULE))
    {
        ReleaseHolder<ICLRPrivResourceHMODULE> pIResourceHMODULE;
        _ASSERTE(flags == MDInternalImport_Default);
        IfFailThrow(pIResource->QueryInterface(__uuidof(ICLRPrivResourceHMODULE), (LPVOID*)&pIResourceHMODULE));
        HMODULE hMod;
        IfFailThrow(pIResourceHMODULE->GetHMODULE(&hMod));
        pPEImage = PEImage::LoadImage(hMod);
    }
#endif // !FEATURE_PAL    
#ifdef FEATURE_FUSION
    else if (iidResource == __uuidof(ICLRPrivResourceStream))
    {
        ReleaseHolder<ICLRPrivResourceStream> pIResourceStream;
        IfFailThrow(pIResource->QueryInterface(__uuidof(ICLRPrivResourceStream), (LPVOID*)&pIResourceStream));
        ReleaseHolder<IStream> pStream;
        IfFailThrow(pIResourceStream->GetStream(__uuidof(IStream), (LPVOID*)&pStream));
        UINT64 i64AssemblyId = static_cast<UINT64>(reinterpret_cast<UINT_PTR>(reinterpret_cast<ICLRPrivAssembly*>(pIResource)));
        DWORD dwModuleId = static_cast<DWORD>(i64AssemblyId);
        pPEImage = PEImage::OpenImage(pStream, i64AssemblyId, FALSE, dwModuleId, flags);
    }
#endif
    else
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    return pPEImage.Extract();
}
#endif



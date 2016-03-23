// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEImage.inl
// 

// --------------------------------------------------------------------------------

#ifndef PEIMAGE_INL_
#define PEIMAGE_INL_

#include "peimage.h"
#include "../dlls/mscorrc/resource.h"

inline ULONG PEImage::AddRef()
{
    CONTRACT(ULONG)
    {
        PRECONDITION(m_refCount>0 && m_refCount < COUNT_T_MAX);
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN (static_cast<ULONG>(FastInterlockIncrement(&m_refCount)));
}

inline const SString &PEImage::GetPath()
{
    LIMITED_METHOD_DAC_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    return m_path;
}

inline void PEImage::SetModuleFileNameHintForDAC()
{
    LIMITED_METHOD_DAC_CONTRACT;
    STATIC_CONTRACT_SO_INTOLERANT;

    // Grab module name only for triage dumps where full paths are excluded
    // because may contain PII data.
    // m_sModuleFileNameHintUsedByDac will just point to module name starting character.
    const WCHAR* pStartPath = m_path.GetUnicode();
    COUNT_T nChars = m_path.GetCount();
    if (pStartPath != NULL && nChars > 0 && nChars <= MAX_PATH)
    {
        const WCHAR* pChar = pStartPath + nChars;
        nChars = 0;
        while ((pChar >= pStartPath) && (*pChar != L'\\'))
        {
            pChar--;
            nChars++;
        }
        pChar++;
        m_sModuleFileNameHintUsedByDac.SetPreallocated(pChar, nChars);
    }
}

#ifdef DACCESS_COMPILE
inline const SString &PEImage::GetModuleFileNameHintForDAC()
{
    LIMITED_METHOD_CONTRACT;

    return m_sModuleFileNameHintUsedByDac;
}
#endif 



inline BOOL PEImage::IsFile()
{
    WRAPPER_NO_CONTRACT;

    return !m_path.IsEmpty();
}

#ifndef DACCESS_COMPILE
inline void   PEImage::SetLayout(DWORD dwLayout, PEImageLayout* pLayout)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(dwLayout<IMAGE_COUNT);
    _ASSERTE(m_pLayouts[dwLayout]==NULL);
    FastInterlockExchangePointer((m_pLayouts+dwLayout),pLayout);
}
#endif  // DACCESS_COMPILE
inline PTR_PEImageLayout PEImage::GetLoadedLayout()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(m_pLayouts[IMAGE_LOADED]!=NULL);
    return m_pLayouts[IMAGE_LOADED]; //no addref
}

inline PTR_PEImageLayout PEImage::GetLoadedIntrospectionLayout()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION]!=NULL);
    return m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION]; //no addref
}


//
// GetExistingLayout - get an layout corresponding to the specified mask, or null if none.
// Does not take any locks or call AddRef.
//
// Arguments:
//    imageLayoutMask - bits from PEImageLayout specifying which layouts the caller would be 
//                      interested in getting
//
// Return value:
//    a PEImageLayout of a type matching one of the bits specified in the mask, or NULL if
//    none exists yet.  Does not call AddRef on the returned value.
//
inline PTR_PEImageLayout PEImage::GetExistingLayoutInternal(DWORD imageLayoutMask)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    PTR_PEImageLayout pRetVal = NULL;

    if (imageLayoutMask&PEImageLayout::LAYOUT_LOADED)
        pRetVal=m_pLayouts[IMAGE_LOADED];
    if (pRetVal==NULL && (imageLayoutMask & PEImageLayout::LAYOUT_LOADED_FOR_INTROSPECTION)) 
        pRetVal=m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION];
    if (pRetVal==NULL && (imageLayoutMask & PEImageLayout::LAYOUT_MAPPED))
        pRetVal=m_pLayouts[IMAGE_MAPPED];
    if (pRetVal==NULL && (imageLayoutMask & PEImageLayout::LAYOUT_FLAT))
        pRetVal=m_pLayouts[IMAGE_FLAT];

    return pRetVal;
}


inline BOOL PEImage::HasLoadedLayout()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return m_pLayouts[IMAGE_LOADED]!=NULL;
}

inline BOOL PEImage::IsOpened()
{
    LIMITED_METHOD_CONTRACT;
    return m_pLayouts[IMAGE_LOADED]!=NULL ||m_pLayouts[IMAGE_MAPPED]!=NULL || m_pLayouts[IMAGE_FLAT] !=NULL || m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION]!=NULL;
}


inline BOOL PEImage::HasLoadedIntrospectionLayout() //introspection only!!!
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pLayouts[IMAGE_LOADED_FOR_INTROSPECTION]!=NULL;
}


#ifdef FEATURE_PREJIT
inline CHECK PEImage::CheckNativeFormat() 
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        CHECK(GetLoadedLayout()->CheckNativeFormat());
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        CHECK(pLayout->CheckNativeFormat());
    }
    CHECK_OK;
};
#endif // FEATURE_PREJIT

inline BOOL PEImage::IsReferenceAssembly()
{
    CONTRACTL
    {
        PRECONDITION(HasCorHeader());
    }
    CONTRACTL_END;

    IMDInternalImport* mdImport = this->GetMDImport();
    HRESULT hr = mdImport->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly),
                                           g_ReferenceAssemblyAttribute,
                                           NULL,
                                           NULL);
    IfFailThrow(hr);
    if (hr == S_OK) {
        return TRUE;
    }
    _ASSERTE(hr == S_FALSE);
    return FALSE;
}


inline const BOOL PEImage::HasNTHeaders()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasNTHeaders();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasNTHeaders();
    }
}

inline const BOOL PEImage::HasCorHeader()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasCorHeader();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasCorHeader();
    }
}

inline const BOOL PEImage::HasReadyToRunHeader()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasReadyToRunHeader();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasReadyToRunHeader();
    }
}

inline void PEImage::SetPassiveDomainOnly()
{
    LIMITED_METHOD_CONTRACT;
    m_bPassiveDomainOnly=TRUE;
}

inline BOOL PEImage::PassiveDomainOnly()
{
    LIMITED_METHOD_CONTRACT;
    return m_bPassiveDomainOnly;
}

#ifdef FEATURE_PREJIT    

inline const BOOL PEImage::GetNativeILHasSecurityDirectory()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->GetNativeILHasSecurityDirectory();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->GetNativeILHasSecurityDirectory();
    }
}
#endif

inline const BOOL PEImage::HasDirectoryEntry(int entry)   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasDirectoryEntry(entry);
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasDirectoryEntry(entry);
    }
}

inline const mdToken PEImage::GetEntryPointToken()
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
    {
        PTR_PEImageLayout pLayout = GetLoadedLayout();
        if (!pLayout->HasManagedEntryPoint())
            return mdTokenNil;
        return pLayout->GetEntryPointToken();
    }
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        if (!pLayout->HasManagedEntryPoint())
            return mdTokenNil;
        return pLayout->GetEntryPointToken();
    }
}

inline const DWORD PEImage::GetCorHeaderFlags()   
{
    WRAPPER_NO_CONTRACT;

    if (HasLoadedLayout())
    {
        PTR_PEImageLayout pLayout = GetLoadedLayout();
        return VAL32(pLayout->GetCorHeader()->Flags);
    }
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return VAL32(pLayout->GetCorHeader()->Flags);
    }
}

inline BOOL PEImage::MDImportLoaded()
{
    return m_pMDImport != NULL;
}

inline const BOOL PEImage::HasV1Metadata()   
{
    WRAPPER_NO_CONTRACT;
    return GetMDImport()->GetMetadataStreamVersion()==MD_STREAM_VER_1X;
}

inline const BOOL PEImage::IsILOnly()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->IsILOnly();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->IsILOnly();
    }
}

inline const WORD PEImage::GetSubsystem()   
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (HasLoadedLayout())
        return GetLoadedLayout()->GetSubsystem();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->GetSubsystem();
    }
}

#ifdef FEATURE_PREJIT    
inline const BOOL PEImage::IsNativeILILOnly()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->IsNativeILILOnly();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->IsNativeILILOnly();
    }
}

inline void PEImage::GetNativeILPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine)
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        GetLoadedLayout()->GetNativeILPEKindAndMachine(pdwKind, pdwMachine);
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        pLayout->GetNativeILPEKindAndMachine(pdwKind, pdwMachine);
    }
}

inline const BOOL PEImage::IsNativeILDll()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->IsNativeILDll();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->IsNativeILDll();
    }
}
#endif // FEATURE_PREJIT

inline const BOOL PEImage::IsDll()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->IsDll();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->IsDll();
    }
}

inline const BOOL PEImage::HasStrongNameSignature()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasStrongNameSignature();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasStrongNameSignature();
    }
}

#ifndef DACCESS_COMPILE

#if !defined(FEATURE_CORECLR)
inline const HRESULT PEImage::VerifyStrongName(DWORD* verifyOutputFlags)  
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(verifyOutputFlags);
    if (m_bSignatureInfoCached)
    {
        if (SUCCEEDED(m_hrSignatureInfoStatus))
            *verifyOutputFlags=m_dwSignatureInfo;
        return m_hrSignatureInfoStatus;
    }

    BOOL result = FALSE;

    PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_FLAT,0));
    if(pLayout!=NULL)
    {
        result = StrongNameSignatureVerificationFromImage((BYTE *) pLayout->GetBase(), pLayout->GetSize(), 
                                                            SN_INFLAG_INSTALL|SN_INFLAG_ALL_ACCESS, 
                                                            verifyOutputFlags);
    }
    else
    {
        CONSISTENCY_CHECK(!GetPath().IsEmpty());
        _ASSERTE(IsFileLocked());
        result = StrongNameSignatureVerification(GetPath(),
                                                    SN_INFLAG_INSTALL|SN_INFLAG_ALL_ACCESS|SN_INFLAG_RUNTIME,
                                                    verifyOutputFlags);
    }

    HRESULT hr=result?S_OK: StrongNameErrorInfo();

    if (SUCCEEDED(hr) || !Exception::IsTransient(hr))
    {
        m_hrSignatureInfoStatus=hr;
        m_dwSignatureInfo=*verifyOutputFlags;
        m_bSignatureInfoCached=TRUE;
    }
    return hr;
}    
#endif // !FEATURE_CORECLR

#endif // !DACCESS_COMPILE

inline BOOL PEImage::IsStrongNameSigned()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->IsStrongNameSigned();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->IsStrongNameSigned();
    }
}

inline BOOL PEImage::IsIbcOptimized()   
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->GetNativeILIsIbcOptimized();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->GetNativeILIsIbcOptimized();
    }
}

#ifndef DACCESS_COMPILE

inline void PEImage::GetImageBits(DWORD layout, SBuffer &result)
{
    WRAPPER_NO_CONTRACT;
    PEImageLayoutHolder pLayout(GetLayout(layout,LAYOUT_CREATEIFNEEDED));
    BYTE* buffer=result.OpenRawBuffer(pLayout->GetSize());
    PREFIX_ASSUME(buffer != NULL);
    memcpyNoGCRefs(buffer,pLayout->GetBase(),pLayout->GetSize());
    result.CloseRawBuffer(pLayout->GetSize());
}

#endif



#ifdef FEATURE_PREJIT 
inline PTR_CVOID PEImage::GetNativeManifestMetadata(COUNT_T *pSize) 
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->GetNativeManifestMetadata(pSize);
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->GetNativeManifestMetadata(pSize);
    }
}
#endif

inline PTR_CVOID PEImage::GetMetadata(COUNT_T *pSize) 
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->GetMetadata(pSize);
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->GetMetadata(pSize);
    }
}

inline BOOL PEImage::HasNativeHeader() 
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasNativeHeader();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasNativeHeader();
    }
}

inline BOOL PEImage::HasContents() 
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->HasContents();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->HasContents();
    }
}


inline CHECK PEImage::CheckFormat()
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        CHECK(GetLoadedLayout()->CheckFormat());
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        CHECK(pLayout->CheckFormat());
    }
    CHECK_OK;
}
inline PTR_CVOID PEImage::GetStrongNameSignature(COUNT_T *pSize) 
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->GetStrongNameSignature(pSize);
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->GetStrongNameSignature(pSize);
    }
}

inline void  PEImage::Init(LPCWSTR pPath)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    m_path = pPath;
    m_path.Normalize();
    SetModuleFileNameHintForDAC();
}
#ifndef DACCESS_COMPILE

#if !defined(FEATURE_CORECLR)
/*static*/
inline PTR_PEImage PEImage::FindByLongPath(LPCWSTR pPath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(s_hashLock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    PathString sLongPath;
    COUNT_T nLen = WszGetLongPathName(pPath, sLongPath);
   
    // Check for any kind of error other than an insufficient buffer result.
    if (nLen == 0)
    {
        HRESULT hr=HRESULT_FROM_WIN32(GetLastError());
        if(Exception::IsTransient(hr))
            ThrowHR(hr);
        return (PEImage*)INVALIDENTRY;
    }
    return FindByPath(sLongPath.GetUnicode());
}

/*static*/
inline PTR_PEImage PEImage::FindByShortPath(LPCWSTR pPath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(s_hashLock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    PathString sShortPath;
    COUNT_T nLen = WszGetShortPathName(pPath, sShortPath);

    // Check for any kind of error other than an insufficient buffer result.
    if (nLen == 0)
    {
        HRESULT hr=HRESULT_FROM_WIN32(GetLastError());
        if(Exception::IsTransient(hr))
            ThrowHR(hr);
        return (PEImage*)INVALIDENTRY;
    }
    return FindByPath(sShortPath.GetUnicode());
}
#endif // !FEATURE_CORECLR

/*static*/
inline PTR_PEImage PEImage::FindByPath(LPCWSTR pPath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(s_hashLock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    int CaseHashHelper(const WCHAR *buffer, COUNT_T count, LocaleID lcid);

    PEImageLocator locator(pPath);
#ifdef FEATURE_CASE_SENSITIVE_FILESYSTEM
    DWORD dwHash=path.Hash();
#else
    DWORD dwHash = CaseHashHelper(pPath, (COUNT_T) wcslen(pPath), PEImage::GetFileSystemLocale());
#endif
   return (PEImage *) s_Images->LookupValue(dwHash, &locator);
    
}

/* static */
inline PTR_PEImage PEImage::OpenImage(LPCWSTR pPath, MDInternalImportFlags flags /* = MDInternalImport_Default */)
{
    BOOL fUseCache = !((flags & MDInternalImport_NoCache) == MDInternalImport_NoCache);

    if (!fUseCache)
    {
        PEImageHolder pImage(new PEImage);
        pImage->Init(pPath);
        return dac_cast<PTR_PEImage>(pImage.Extract());
    }

    CrstHolder holder(&s_hashLock);
    
    PEImage* found = FindByPath(pPath);

#if !defined(FEATURE_CORECLR)
    if(found == (PEImage*) INVALIDENTRY && (flags & MDInternalImport_CheckLongPath))
         found=FindByLongPath(pPath);

    if(found == (PEImage*) INVALIDENTRY && (flags & MDInternalImport_CheckShortPath))
         found=FindByShortPath(pPath);
#endif

    if (found == (PEImage*) INVALIDENTRY)
    {
        // We did not find the entry in the Cache, and we've been asked to only use the cache.
        if  ((flags & MDInternalImport_OnlyLookInCache) == MDInternalImport_OnlyLookInCache)
        {
            return NULL;
        }

        PEImageHolder pImage(new PEImage);
#ifdef FEATURE_PREJIT        
        if (flags &  MDInternalImport_TrustedNativeImage)
            pImage->SetIsTrustedNativeImage();
        if (flags &  MDInternalImport_NativeImageInstall)
            pImage->SetIsNativeImageInstall();
#endif        
        pImage->Init(pPath);

        pImage->AddToHashMap();
        return dac_cast<PTR_PEImage>(pImage.Extract());
    }
    
    found->AddRef();
    
    return dac_cast<PTR_PEImage>(found);
}
#endif

inline BOOL PEImage::IsFileLocked()
{
    WRAPPER_NO_CONTRACT;
    return (m_pLayouts[IMAGE_FLAT])!=NULL || (m_pLayouts[IMAGE_MAPPED])!=NULL ;
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_FUSION
/* static */
inline PTR_PEImage PEImage::FindById(UINT64 uStreamAsmId, DWORD dwModuleId)
{
    PEImageLocator locator(uStreamAsmId, dwModuleId);
    CrstHolder holder(&s_hashLock);
    PEImage* found = (PEImage *) s_Images->LookupValue(HashStreamIds(uStreamAsmId, dwModuleId), &locator);
    if (found == (PEImage*) INVALIDENTRY)
        return NULL;
    found->AddRef();
    return dac_cast<PTR_PEImage>(found);
}

/* static */
inline PTR_PEImage PEImage::OpenImage(IStream *pIStream, UINT64 uStreamAsmId,
                                      DWORD dwModuleId, BOOL resourceFile, MDInternalImportFlags flags /* = MDInternalImport_Default */)  
{
    BOOL fUseCache = !((flags & MDInternalImport_NoCache) == MDInternalImport_NoCache);

    if (!fUseCache)
    {
        PEImageHolder pImage(new PEImage());
        pImage->Init(pIStream, uStreamAsmId, dwModuleId, resourceFile);
        return dac_cast<PTR_PEImage>(pImage.Extract());
    }

    
    DWORD hash = HashStreamIds(uStreamAsmId, dwModuleId);
    PEImageLocator locator(uStreamAsmId,dwModuleId);
    CrstHolder holder(&s_hashLock);
    PEImage* found = (PEImage *) s_Images->LookupValue(hash, &locator);
    if (found != (PEImage*) INVALIDENTRY)
    {
        found->AddRef();
        return dac_cast<PTR_PEImage>(found);
    }
    PEImageHolder pImage(new PEImage());
    pImage->Init(pIStream, uStreamAsmId, dwModuleId, resourceFile);

    pImage->AddToHashMap();
    return dac_cast<PTR_PEImage>(pImage.Extract());
}
#endif // FEATURE_FUSION

inline void PEImage::AddToHashMap()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(s_hashLock.OwnedByCurrentThread());
    s_Images->InsertValue(GetIDHash(),this);
    m_bInHashMap=TRUE;
}

#endif




inline BOOL PEImage::Has32BitNTHeaders()
{
    WRAPPER_NO_CONTRACT;
    if (HasLoadedLayout())
        return GetLoadedLayout()->Has32BitNTHeaders();
    else
    {
        PEImageLayoutHolder pLayout(GetLayout(PEImageLayout::LAYOUT_ANY,LAYOUT_CREATEIFNEEDED));
        return pLayout->Has32BitNTHeaders();
    }
}

inline BOOL PEImage::HasID()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_FUSION
    if (m_fIsIStream)
        return TRUE;
#endif

    return !GetPath().IsEmpty();
}

inline ULONG PEImage::GetIDHash()
{
    CONTRACT(ULONG)
    {
        PRECONDITION(HasID());
        MODE_ANY;
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACT_END;

#ifdef FEATURE_FUSION
    if (m_fIsIStream)
        RETURN HashStreamIds(m_StreamAsmId, m_dwStreamModuleId);
#endif

#ifdef FEATURE_CASE_SENSITIVE_FILESYSTEM
    RETURN m_path.Hash();
#else
    RETURN m_path.HashCaseInsensitive(PEImage::GetFileSystemLocale());
#endif
}

inline void PEImage::CachePEKindAndMachine()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Do nothing if we have cached the information already
    if(m_fCachedKindAndMachine)
        return;

    PEImageLayoutHolder pLayout;
    if (HasLoadedLayout())
    {
        pLayout.Assign(GetLoadedLayout(), false);
    }
    else
    {
        pLayout.Assign(GetLayout(PEImageLayout::LAYOUT_MAPPED|PEImageLayout::LAYOUT_FLAT, 
                                 PEImage::LAYOUT_CREATEIFNEEDED));
    }

    // Compute result into a local variables first
    DWORD dwPEKind, dwMachine;
    pLayout->GetPEKindAndMachine(&dwPEKind, &dwMachine);

    // Write the final result into the lock-free cache.
    m_dwPEKind = dwPEKind;
    m_dwMachine = dwMachine;
    MemoryBarrier();
    m_fCachedKindAndMachine = TRUE;
}

inline void  PEImage::GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine)
{
    WRAPPER_NO_CONTRACT;
    CachePEKindAndMachine();
    if (pdwKind)
        *pdwKind = m_dwPEKind;
    if (pdwMachine)
        *pdwMachine = m_dwMachine;
}

#ifdef FEATURE_APTCA
inline BOOL PEImage::MayBeConditionalAptca()
{
    LIMITED_METHOD_CONTRACT;
    return m_fMayBeConditionalAptca;
}

inline void PEImage::SetIsNotConditionalAptca()
{
    LIMITED_METHOD_CONTRACT;
    m_fMayBeConditionalAptca = FALSE;
}
#endif // FEATURE_APTCA

#ifndef FEATURE_CORECLR
inline BOOL PEImage::IsReportedToUsageLog()
{
    LIMITED_METHOD_CONTRACT;
    return m_fReportedToUsageLog;
}

inline void PEImage::SetReportedToUsageLog()
{
    LIMITED_METHOD_CONTRACT;
    m_fReportedToUsageLog = TRUE;
}
#endif // !FEATURE_CORECLR

#ifndef DACCESS_COMPILE
inline void PEImage::AllocateLazyCOWPages()
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_LAZY_COW_PAGES
    if (!m_bAllocatedLazyCOWPages && CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ZapLazyCOWPagesEnabled))
    {
        ::AllocateLazyCOWPages(GetLoadedLayout());
        m_bAllocatedLazyCOWPages = TRUE;
    }
#endif
}
#endif

#endif  // PEIMAGE_INL_

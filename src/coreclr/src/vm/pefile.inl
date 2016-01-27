// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEFile.inl
// 

// --------------------------------------------------------------------------------

#ifndef PEFILE_INL_
#define PEFILE_INL_

#include "strongname.h"
#include "strongnameholders.h"
#ifdef FEATURE_FUSION
#include "fusionbind.h"
#endif
#include "check.h"
#include "simplerwlock.hpp"
#include "eventtrace.h"
#include "peimagelayout.inl"

#if CHECK_INVARIANTS
inline CHECK PEFile::Invariant()
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    if (IsDynamic())
    {
        // dynamic module case
        CHECK(m_openedILimage == NULL);
#ifdef FEATURE_PREJIT
        CHECK(m_nativeImage == NULL);
#endif
        CHECK(CheckPointer(m_pEmitter));
    }
    else
    {
        // If m_image is null, then we should have a native image. However, this is not valid initially
        // during construction.  We should find a way to assert this.
        CHECK(CheckPointer((PEImage*) m_openedILimage, NULL_OK));
#ifdef FEATURE_PREJIT
        CHECK(CheckPointer((PEImage*) m_nativeImage, NULL_OK));
#endif
    }
    CHECK_OK;
}
#endif // CHECK_INVARIANTS

// ------------------------------------------------------------
// AddRef/Release
// ------------------------------------------------------------

inline ULONG PEFile::AddRef()
{
    CONTRACTL
    {
        PRECONDITION(m_refCount < COUNT_T_MAX);
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    return FastInterlockIncrement(&m_refCount);
}

inline ULONG PEFile::Release()
{
    CONTRACT(COUNT_T)
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACT_END;
    
    LONG result = FastInterlockDecrement(&m_refCount);
    _ASSERTE(result >= 0);
    if (result == 0)
        delete this;
    
    RETURN result;
}

// ------------------------------------------------------------
// Identity
// ------------------------------------------------------------

inline ULONG PEAssembly::HashIdentity()
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(m_identity));
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
#ifdef FEATURE_VERSIONING
    return BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pHostAssembly)->GetAssemblyName()->Hash(BINDER_SPACE::AssemblyName::INCLUDE_VERSION);
#else
    if (!m_identity->HasID())
    {
        if (!IsLoaded())
            return 0;
        else
            return (ULONG) dac_cast<TADDR>(GetLoaded()->GetBase());
    }
    else
        return m_identity->GetIDHash();
#endif
}

inline void PEFile::ValidateForExecution()
{
    CONTRACTL
    {
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // We do not need to check NGen images; if it had the attribute, it would have failed to load
    // at NGen time and so there would be no NGen image.
    if (HasNativeImage() || IsIntrospectionOnly())
        return;

    //
    // Ensure reference assemblies are not loaded for execution
    //
    ReleaseHolder<IMDInternalImport> mdImport(this->GetMDImportWithRef());
    if (mdImport->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly),
                                           g_ReferenceAssemblyAttribute,
                                           NULL,
                                           NULL) == S_OK) {
        ThrowHR(COR_E_LOADING_REFERENCE_ASSEMBLY, BFA_REFERENCE_ASSEMBLY);
    }

    //
    // Ensure platform is valid for execution
    //
    if (!IsDynamic() && !IsResource()) 
    {
        if (IsMarkedAsNoPlatform())
        {
            if (IsMarkedAsContentTypeWindowsRuntime())
            {
                ThrowHR(COR_E_LOADING_WINMD_REFERENCE_ASSEMBLY);
            }
            else
            {
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }
        }
    }
}


inline BOOL PEFile::IsMarkedAsNoPlatform()
{
    WRAPPER_NO_CONTRACT;
    return (IsAfPA_NoPlatform(GetFlags()));
}

inline BOOL PEFile::IsMarkedAsContentTypeWindowsRuntime()
{
    WRAPPER_NO_CONTRACT;
    return (IsAfContentType_WindowsRuntime(GetFlags()));
}

#ifndef FEATURE_CORECLR
inline BOOL PEFile::IsShareable()
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(m_identity));
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (!m_identity->HasID())
        return FALSE;
    return TRUE ;
}
#endif

inline void PEFile::GetMVID(GUID *pMvid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    IfFailThrow(GetPersistentMDImport()->GetScopeProps(NULL, pMvid));
}

inline BOOL PEFile::PassiveDomainOnly()
{
    WRAPPER_NO_CONTRACT;
    return HasOpenedILimage() && GetOpenedILimage()->PassiveDomainOnly();
}

// ------------------------------------------------------------
// Loader support routines
// ------------------------------------------------------------

inline void PEFile::SetSkipVerification()
{
    LIMITED_METHOD_CONTRACT;

    m_flags |= PEFILE_SKIP_VERIFICATION; 
}

inline BOOL PEFile::HasSkipVerification()
{
    LIMITED_METHOD_CONTRACT;

    return (m_flags & (PEFILE_SKIP_VERIFICATION | PEFILE_SYSTEM)) != 0; 
}

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

inline const SString &PEFile::GetPath()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (IsDynamic())
    {
        return SString::Empty();
    }
    else
        return m_identity->GetPath();
}


#ifdef DACCESS_COMPILE
inline const SString &PEFile::GetModuleFileNameHint()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsDynamic())
    {
        return SString::Empty();
    }
    else
        return m_identity->GetModuleFileNameHintForDAC();
}
#endif // DACCESS_COMPILE

#ifdef LOGGING
inline LPCWSTR PEFile::GetDebugName()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;
    
#ifdef _DEBUG
    return m_pDebugName;
#else
    return GetPath();
#endif
}
#endif

// ------------------------------------------------------------
// Classification
// ------------------------------------------------------------

inline BOOL PEFile::IsAssembly() const
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (m_flags & PEFILE_ASSEMBLY) != 0;
}

inline PTR_PEAssembly PEFile::AsAssembly()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (this == nullptr)
        return dac_cast<PTR_PEAssembly>(nullptr);
    if (IsAssembly())
        return dac_cast<PTR_PEAssembly>(this);
    else
        return dac_cast<PTR_PEAssembly>(nullptr);
}

inline BOOL PEFile::IsModule() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (m_flags & PEFILE_MODULE) != 0;
}

inline PTR_PEModule PEFile::AsModule()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (this == nullptr)
        return dac_cast<PTR_PEModule>(nullptr);
    if (IsModule())
        return dac_cast<PTR_PEModule>(this);
    else
        return dac_cast<PTR_PEModule>(nullptr);
}

inline BOOL PEFile::IsSystem() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (m_flags & PEFILE_SYSTEM) != 0;
}

inline BOOL PEFile::IsDynamic() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_identity == NULL;
}

inline BOOL PEFile::IsResource() const
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    return IsModule() && dac_cast<PTR_PEModule>(this)->IsResource();
#else
    return FALSE;
#endif // FEATURE_MULTIMODULE_ASSEMBLIES
}

inline BOOL PEFile::IsIStream() const
{
    LIMITED_METHOD_CONTRACT;

    return (m_flags & PEFILE_ISTREAM) != 0;
}

inline BOOL PEFile::IsIntrospectionOnly() const
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    if (IsModule())
    {
        return dac_cast<PTR_PEModule>(this)->GetAssembly()->IsIntrospectionOnly();
    }
    else
#endif //  FEATURE_MULTIMODULE_ASSEMBLIES
    {
        return (m_flags & PEFILE_INTROSPECTIONONLY) != 0;
    }
}


inline PEAssembly *PEFile::GetAssembly() const
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    if (IsAssembly())
        return dac_cast<PTR_PEAssembly>(this);
    else
        return dac_cast<PTR_PEModule>(this)->GetAssembly();
#else
    _ASSERTE(IsAssembly());
    return dac_cast<PTR_PEAssembly>(this);

#endif // FEATURE_MULTIMODULE_ASSEMBLIES
}

// ------------------------------------------------------------
// Hash support
// ------------------------------------------------------------

#ifndef DACCESS_COMPILE
inline void PEFile::GetImageBits(SBuffer &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckValue(result));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EnsureImageOpened();
    // We don't cache any other hashes right now.
    if (!IsDynamic())
        GetILimage()->GetImageBits(PEImageLayout::LAYOUT_FLAT,result);
}

inline void PEFile::GetHash(ALG_ID algorithm, SBuffer &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckValue(result));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (algorithm == CALG_SHA1)
    {
        GetSHA1Hash(result);
    }
    else
    {
        EnsureImageOpened();
        // We don't cache any other hashes right now.
        GetILimage()->ComputeHash(algorithm, result);
    }
}
    
inline CHECK PEFile::CheckHash(ALG_ID algorithm, const void *hash, COUNT_T size)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(hash));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    StackSBuffer hashBuffer;
    GetHash(algorithm, hashBuffer);

    CHECK(hashBuffer.Equals((const BYTE *)hash, size));

    CHECK_OK;
}
#endif // DACCESS_COMPILE

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

inline BOOL PEFile::HasMetadata()
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

    return !IsResource();
}

inline IMDInternalImportHolder PEFile::GetMDImport()
{
    WRAPPER_NO_CONTRACT;
    if (m_bHasPersistentMDImport)
        return IMDInternalImportHolder(GetPersistentMDImport(),FALSE);
    else
        return IMDInternalImportHolder(GetMDImportWithRef(),TRUE);
};

inline IMDInternalImport* PEFile::GetPersistentMDImport()
{
/*
    CONTRACT(IMDInternalImport *) 
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsResource());
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;
*/
    SUPPORTS_DAC;
#ifndef FEATURE_CORECLR
_ASSERTE(m_bHasPersistentMDImport);
#endif
#if !defined(__GNUC__)

    _ASSERTE(!IsResource());
#endif
#ifdef DACCESS_COMPILE
    WRAPPER_NO_CONTRACT;
    return DacGetMDImport(this, true);
#else
    LIMITED_METHOD_CONTRACT;

    return m_pMDImport;
#endif
}

inline IMDInternalImport *PEFile::GetMDImportWithRef()
{
/*
    CONTRACT(IMDInternalImport *) 
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsResource());
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACT_END;
*/
#if !defined(__GNUC__)
    _ASSERTE(!IsResource());
#endif
#ifdef DACCESS_COMPILE
    WRAPPER_NO_CONTRACT;
    return DacGetMDImport(this, true);
#else
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        CAN_TAKE_LOCK;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    GCX_PREEMP();
    SimpleReadLockHolder lock(m_pMetadataLock);
    if(m_pMDImport)
        m_pMDImport->AddRef();
    return m_pMDImport;
#endif
}

#ifndef DACCESS_COMPILE

inline IMetaDataImport2 *PEFile::GetRWImporter()
{
    CONTRACT(IMetaDataImport2 *) 
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsResource());
        POSTCONDITION(CheckPointer(RETVAL));
        PRECONDITION(m_bHasPersistentMDImport);
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACT_END;

    if (m_pImporter == NULL)
        OpenImporter();

    RETURN m_pImporter;
}

inline IMetaDataEmit *PEFile::GetEmitter()
{
    CONTRACT(IMetaDataEmit *) 
    {
        INSTANCE_CHECK;
        MODE_ANY;
        GC_NOTRIGGER;
        PRECONDITION(!IsResource());
        POSTCONDITION(CheckPointer(RETVAL));
        PRECONDITION(m_bHasPersistentMDImport);
        THROWS;
    }
    CONTRACT_END;

    if (m_pEmitter == NULL)
        OpenEmitter();

    RETURN m_pEmitter;
}

#ifndef FEATURE_CORECLR
inline IMetaDataAssemblyImport *PEFile::GetAssemblyImporter()
{
    CONTRACT(IMetaDataAssemblyImport *) 
    {
        INSTANCE_CHECK;
        MODE_ANY;
        GC_NOTRIGGER;
        PRECONDITION(!IsResource());
        POSTCONDITION(CheckPointer(RETVAL));
        PRECONDITION(m_bHasPersistentMDImport);
        THROWS;
    }
    CONTRACT_END;

    if (m_pAssemblyImporter == NULL)
        OpenAssemblyImporter();

    RETURN m_pAssemblyImporter;
}

inline IMetaDataAssemblyEmit *PEFile::GetAssemblyEmitter()
{
    CONTRACT(IMetaDataAssemblyEmit *) 
    {
        INSTANCE_CHECK;
        MODE_ANY;
        GC_NOTRIGGER;
        PRECONDITION(!IsResource());
        POSTCONDITION(CheckPointer(RETVAL));
        PRECONDITION(m_bHasPersistentMDImport);
    }
    CONTRACT_END;

    if (m_pAssemblyEmitter == NULL)
        OpenAssemblyEmitter();

    RETURN m_pAssemblyEmitter;
}
#endif // FEATURE_CORECLR

#endif // DACCESS_COMPILE

// The simple name is not actually very simple. The name returned comes from one of
// various metadata tables, depending on whether this is a manifest module,
// non-manifest module, or something else
inline LPCUTF8 PEFile::GetSimpleName()
{
    CONTRACT(LPCUTF8)
    {
        INSTANCE_CHECK;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        SUPPORTS_DAC;
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACT_END;

    if (IsAssembly())
        RETURN dac_cast<PTR_PEAssembly>(this)->GetSimpleName();
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    else if (IsModule())
        RETURN dac_cast<PTR_PEModule>(this)->GetSimpleName();
#endif // FEATURE_MULTIMODULE_ASSEMBLIES
    else 
    {
        LPCUTF8 szScopeName;
        if (FAILED(GetScopeName(&szScopeName)))
        {
            szScopeName = ""; 
        }
        RETURN szScopeName;
    }
}


// Same as the managed Module.ScopeName property, this unconditionally looks in the
// metadata Module table to get the name.  Useful for profilers and others who don't
// like sugar coating on their names.
inline HRESULT PEFile::GetScopeName(LPCUTF8 * pszName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        MODE_ANY;
        NOTHROW;
        SUPPORTS_DAC;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return GetMDImport()->GetScopeProps(pszName, NULL);
}


// ------------------------------------------------------------
// PE file access
// ------------------------------------------------------------

inline BOOL PEFile::HasSecurityDirectory()
{
    WRAPPER_NO_CONTRACT;

    if (IsResource() || IsDynamic())
        return FALSE;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        return m_nativeImage->GetNativeILHasSecurityDirectory();
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage)
            return pNativeImage->GetNativeILHasSecurityDirectory();
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_PREJIT

    if (!GetILimage()->HasNTHeaders())
        return FALSE;

    return GetOpenedILimage()->HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_SECURITY);
}

inline BOOL PEFile::IsIbcOptimized()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        return m_nativeImage->IsIbcOptimized();
    }
#endif

    return FALSE;
}

inline BOOL PEFile::IsILImageReadyToRun()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        return GetLoadedNative()->GetNativeILHasReadyToRunHeader();
    }
    else
#endif // FEATURE_PREJIT
    if (HasOpenedILimage())
    {
        return GetLoadedIL()->HasReadyToRunHeader();
    }
    else
    {
        return FALSE;
    }
}

inline WORD PEFile::GetSubsystem()
{
    WRAPPER_NO_CONTRACT;

    if (IsResource() || IsDynamic())
        return 0;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        return GetLoadedNative()->GetSubsystem();
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage)
            return pNativeImage->GetSubsystem();
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_PREJIT
 
    return GetLoadedIL()->GetSubsystem();
}

inline mdToken PEFile::GetEntryPointToken(
#ifdef _DEBUG
            BOOL bAssumeLoaded
#endif //_DEBUG
            )
{
    WRAPPER_NO_CONTRACT;

    if (IsResource() || IsDynamic())
        return mdTokenNil;


#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());
        _ASSERTE (!bAssumeLoaded || m_nativeImage->HasLoadedLayout ());
        return m_nativeImage->GetEntryPointToken();
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage) {
            _ASSERTE (!bAssumeLoaded || pNativeImage->HasLoadedLayout ());
            return pNativeImage->GetEntryPointToken();
        }
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_PREJIT
    _ASSERTE (!bAssumeLoaded || HasLoadedIL ());
    return GetOpenedILimage()->GetEntryPointToken();
}

#ifdef FEATURE_PREJIT
inline BOOL PEFile::IsNativeLoaded()
{
    WRAPPER_NO_CONTRACT;
    return (m_nativeImage && m_bHasPersistentMDImport && m_nativeImage->HasLoadedLayout());
}
inline void PEFile::MarkNativeImageInvalidIfOwned()
{
    WRAPPER_NO_CONTRACT;
    // If owned, mark the PEFile as dummy, so the image does not get reused 
    PEImageHolder nativeImage(GetNativeImageWithRef());
    Module * pNativeModule = nativeImage->GetLoadedLayout()->GetPersistedModuleImage();
    PEFile ** ppNativeFile = (PEFile**) (PBYTE(pNativeModule) + Module::GetFileOffset());

    // Attempt to write only if we claimed the ownership.
    if (*ppNativeFile == this)
        FastInterlockCompareExchangePointer(ppNativeFile, Dummy(), this);
}


#endif

inline BOOL PEFile::IsILOnly()
{
    STATIC_CONTRACT_SO_TOLERANT;
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    CONTRACT_VIOLATION(ThrowsViolation|GCViolation|FaultViolation);
    
    if (IsResource() || IsDynamic())
        return FALSE;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        return m_nativeImage->IsNativeILILOnly();
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        BOOL retVal = FALSE;

        BEGIN_SO_INTOLERANT_CODE(GetThread());

        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage)
        {
            retVal = pNativeImage->IsNativeILILOnly();
        }

        END_SO_INTOLERANT_CODE;

        return retVal;
    }
#endif // DACCESS_COMPILE
#endif // FEATURE_PREJIT

    return GetOpenedILimage()->IsILOnly();
}

inline BOOL PEFile::IsDll()
{
    WRAPPER_NO_CONTRACT;

    if (IsResource() || IsDynamic())
        return TRUE;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        return m_nativeImage->IsNativeILDll();
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage =GetNativeImageWithRef();
        if (pNativeImage)
            return pNativeImage->IsNativeILDll();
    }
    EnsureImageOpened();
#endif // DACCESS_COMPILE
#endif // FEATURE_PREJIT

    return GetOpenedILimage()->IsDll();
}

inline PTR_VOID PEFile::GetRvaField(RVA field)
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckRvaField(field));
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Note that the native image Rva fields are currently cut off before 
    // this point.  We should not get here for an IL only native image.
    
    RETURN dac_cast<PTR_VOID>(GetLoadedIL()->GetRvaData(field,NULL_OK));
}

inline CHECK PEFile::CheckRvaField(RVA field)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;
        
    // Note that the native image Rva fields are currently cut off before 
    // this point.  We should not get here for an IL only native image.
    
    CHECK(GetLoadedIL()->CheckRva(field,NULL_OK));
    CHECK_OK;
}

inline CHECK PEFile::CheckRvaField(RVA field, COUNT_T size)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    // Note that the native image Rva fields are currently cut off before 
    // this point.  We should not get here for an IL only native image.
    
    CHECK(GetLoadedIL()->CheckRva(field, size,0,NULL_OK));
    CHECK_OK;
}

inline BOOL PEFile::HasTls()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckLoaded());
    }
    CONTRACTL_END;

    // Resource modules do not contain TLS data.
    if (IsResource())
        return FALSE;
    // Dynamic modules do not contain TLS data.
    else if (IsDynamic())
        return FALSE;
    // ILOnly modules do not contain TLS data.
    else if (IsILOnly())
        return FALSE;
    else
        return GetLoadedIL()->HasTls();
}

inline BOOL PEFile::IsRvaFieldTls(RVA field)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckLoaded());
    }
    CONTRACTL_END;

    if (!HasTls())
        return FALSE;

    PTR_VOID address = PTR_VOID(GetLoadedIL()->GetRvaData(field));

    COUNT_T tlsSize;
    PTR_VOID tlsRange = GetLoadedIL()->GetTlsRange(&tlsSize);

    return (address >= tlsRange
            && address < (dac_cast<PTR_BYTE>(tlsRange)+tlsSize));
}

inline UINT32 PEFile::GetFieldTlsOffset(RVA field)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckRvaField(field));
        PRECONDITION(IsRvaFieldTls(field));
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
        
    return (UINT32)(dac_cast<PTR_BYTE>(GetRvaField(field)) - 
                                dac_cast<PTR_BYTE>(GetLoadedIL()->GetTlsRange()));
}
        
inline UINT32 PEFile::GetTlsIndex()
{
    CONTRACTL
    {
        PRECONDITION(CheckLoaded());
        INSTANCE_CHECK;
        PRECONDITION(HasTls());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetLoadedIL()->GetTlsIndex();
}

inline const void *PEFile::GetInternalPInvokeTarget(RVA target)
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckInternalPInvokeTarget(target));
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (void*)GetLoadedIL()->GetRvaData(target);
}

inline CHECK PEFile::CheckInternalPInvokeTarget(RVA target)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    CHECK(!IsILOnly());
    CHECK(GetLoadedIL()->CheckRva(target));
    
    CHECK_OK;
}
    
inline PCCOR_SIGNATURE  PEFile::GetSignature(RVA signature)
{
    CONTRACT(PCCOR_SIGNATURE)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic() || signature == 0);
        PRECONDITION(!IsResource());
        PRECONDITION(CheckSignatureRva(signature));
        POSTCONDITION(CheckSignature(RETVAL));
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    if (signature == 0)
        RETURN NULL;
    else
        RETURN (PCCOR_SIGNATURE) GetLoadedIL()->GetRvaData(signature);
}

inline RVA PEFile::GetSignatureRva(PCCOR_SIGNATURE signature)
{
    CONTRACT(RVA)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic() || signature == NULL);
        PRECONDITION(!IsResource());
        PRECONDITION(CheckSignature(signature));
        POSTCONDITION(CheckSignatureRva(RETVAL));
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    if (signature == NULL)
        RETURN 0;
    else
        RETURN GetLoadedIL()->GetDataRva(
            dac_cast<TADDR>(signature));
}

inline CHECK PEFile::CheckSignature(PCCOR_SIGNATURE signature)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic() || signature == NULL);
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;
        
    CHECK(GetLoadedIL()->CheckData(signature,NULL_OK));
    CHECK_OK;
}

inline CHECK PEFile::CheckSignatureRva(RVA signature)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic() || signature == NULL);
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;
        
    CHECK(GetLoadedIL()->CheckRva(signature,NULL_OK));
    CHECK_OK;
}

inline IMAGE_COR_VTABLEFIXUP *PEFile::GetVTableFixups(COUNT_T *pCount/*=NULL*/)
{
    CONTRACT(IMAGE_COR_VTABLEFIXUP *)
    {
        PRECONDITION(CheckLoaded());
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (IsResource() || IsDynamic() || IsILOnly())
    {
        if (pCount != NULL)
            *pCount = 0;
        RETURN NULL;
    }
    else
        RETURN GetLoadedIL()->GetVTableFixups(pCount);
}

inline void *PEFile::GetVTable(RVA rva)
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        PRECONDITION(!IsILOnly());
        PRECONDITION(GetLoadedIL()->CheckRva(rva));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (void *)GetLoadedIL()->GetRvaData(rva);
}

// @todo: this is bad to expose. But it is needed to support current IJW thunks
inline HMODULE PEFile::GetIJWBase()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
        PRECONDITION(CheckLoaded());
        PRECONDITION(!IsILOnly());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return (HMODULE) dac_cast<TADDR>(GetLoadedIL()->GetBase());
}

inline PTR_VOID PEFile::GetDebuggerContents(COUNT_T *pSize/*=NULL*/)
{
    CONTRACT(PTR_VOID)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // We cannot in general force a LoadLibrary; we might be in the 
    // helper thread.  The debugger will have to expect a zero base
    // in some circumstances.

    if (IsLoaded())
    {
        if (pSize != NULL)
            *pSize = GetLoaded()->GetSize();

        RETURN GetLoaded()->GetBase();
    }
    else
    {
        if (pSize != NULL)
            *pSize = 0;

        RETURN NULL;
    }
}

inline PTR_CVOID PEFile::GetLoadedImageContents(COUNT_T *pSize/*=NULL*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (IsLoaded() && !IsDynamic())
    {
        if (pSize != NULL)
        {
            *pSize = GetLoaded()->GetSize();
        }
        return GetLoaded()->GetBase();
    }
    else
    {
        if (pSize != NULL)
        {
            *pSize = 0;
        }
        return NULL;
    }
}

#ifndef DACCESS_COMPILE
inline const void *PEFile::GetManagedFileContents(COUNT_T *pSize/*=NULL*/)
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckLoaded());
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        POSTCONDITION((!GetLoaded()->GetSize()) || CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Right now, we will trigger a LoadLibrary for the caller's sake, 
    // even if we are in a scenario where we could normally avoid it.
    LoadLibrary(FALSE);

    if (pSize != NULL)
        *pSize = GetLoadedIL()->GetSize();

    
    RETURN GetLoadedIL()->GetBase();
}
#endif // DACCESS_COMPILE

inline BOOL PEFile::IsPtrInILImage(PTR_CVOID data)
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

    if (HasOpenedILimage())
    {
#if defined(FEATURE_PREJIT) && defined(FEATURE_CORECLR)
        if (m_openedILimage == m_nativeImage)
        {
            // On Apollo builds, we sometimes open the native image into the slot
            // normally reserved for the IL image (as the IL image is often not available
            // on the disk at all). In such a case, data is not coming directly from an
            // actual IL image, but see if it's coming from the metadata that we copied
            // from the IL image into the NI.
            TADDR taddrData = dac_cast<TADDR>(data);
            PEDecoder * pDecoder = m_nativeImage->GetLoadedLayout();
            COUNT_T cbILMetadata;
            TADDR taddrILMetadata = dac_cast<TADDR>(pDecoder->GetMetadata(&cbILMetadata));
            return ((taddrILMetadata <= taddrData) && (taddrData < taddrILMetadata + cbILMetadata));
        }
#endif // defined(FEATURE_PREJIT) && defined(FEATURE_CORECLR)
        return GetOpenedILimage()->IsPtrInImage(data);
    }
    else
        return FALSE;
}
// ------------------------------------------------------------
// Native image access
// ------------------------------------------------------------
inline BOOL PEFile::HasNativeImage()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    return (m_nativeImage != NULL);
#else
    return FALSE;
#endif
}

inline PTR_PEImageLayout PEFile::GetLoadedIL() 
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(HasOpenedILimage());
    if(IsIntrospectionOnly())
        return GetOpenedILimage()->GetLoadedIntrospectionLayout();
    
    return GetOpenedILimage()->GetLoadedLayout();
};

inline PTR_PEImageLayout PEFile::GetAnyILWithRef() 
{
    WRAPPER_NO_CONTRACT;
    return GetILimage()->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED);
};


inline BOOL PEFile::IsLoaded(BOOL bAllowNative/*=TRUE*/) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    if(IsDynamic())
        return TRUE;
    if(IsIntrospectionOnly())
    {
        return HasOpenedILimage() && GetOpenedILimage()->HasLoadedIntrospectionLayout();
    }
#ifdef FEATURE_PREJIT
    if (bAllowNative && HasNativeImage())
    {
        PEImage *pNativeImage = GetPersistentNativeImage();
        return pNativeImage->HasLoadedLayout() && (pNativeImage->GetLoadedLayout()->IsNativeILILOnly() || (HasLoadedIL()));
    }
    else
#endif
        return HasLoadedIL();
};


inline PTR_PEImageLayout PEFile::GetLoaded() 
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return HasNativeImage()?GetLoadedNative():GetLoadedIL();
};

inline PTR_PEImageLayout PEFile::GetLoadedNative()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    PEImage* pImage=GetPersistentNativeImage();
    _ASSERTE(pImage && pImage->GetLoadedLayout());
    return pImage->GetLoadedLayout();
#else
    // Should never get here
    PRECONDITION(HasNativeImage());
    return NULL;
#endif
};

#ifdef FEATURE_PREJIT
inline PEImage *PEFile::GetPersistentNativeImage()
{
    CONTRACT(PEImage *)
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        POSTCONDITION(CheckPointer(RETVAL));
        PRECONDITION(m_bHasPersistentMDImport);
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN m_nativeImage;
}

#ifndef DACCESS_COMPILE
inline PEImage *PEFile::GetNativeImageWithRef()
{
    CONTRACT(PEImage *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS; 
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
    }
    CONTRACT_END;
    GCX_PREEMP();
    SimpleReadLockHolder mdlock(m_pMetadataLock);
    if(m_nativeImage)
        m_nativeImage->AddRef();
    RETURN m_nativeImage;
}
#endif // DACCESS_COMPILE

inline BOOL PEFile::HasNativeImageMetadata()
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN ((m_flags & PEFILE_HAS_NATIVE_IMAGE_METADATA) != 0);
}
#endif

 // Function to get the fully qualified name of an assembly
 inline void PEAssembly::GetFullyQualifiedAssemblyName(IMDInternalImport* pImport, mdAssembly mda, SString &result, DWORD flags)
{
    CONTRACTL
    {
        PRECONDITION(CheckValue(result));
#ifndef DACCESS_COMPILE
        THROWS;
#else
        NOTHROW;
 #endif // !DACCESS_COMPILE
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(pImport != NULL)
    {
        // This is for DAC, ONLY for the binding tool.  Don't use for other
        // purposes, since this is not canonicalized through Fusion.
        LPCSTR name;
        AssemblyMetaDataInternal context;
        DWORD dwFlags;
        PBYTE pbPublicKey;
        DWORD cbPublicKey;
        if (FAILED(pImport->GetAssemblyProps(
            mda, 
            (const void **) &pbPublicKey, 
            &cbPublicKey, 
            NULL, 
            &name, 
            &context, 
            &dwFlags)))
        {
            _ASSERTE(!"If this fires, then we have to throw for corrupted images");
            result.SetUTF8("");
            return;
        }
        
        result.SetUTF8(name);
        
        result.AppendPrintf(W(", Version=%u.%u.%u.%u"),
                            context.usMajorVersion, context.usMinorVersion,
                            context.usBuildNumber, context.usRevisionNumber);
        
        result.Append(W(", Culture="));
        if (!*context.szLocale)
        {
            result.Append(W("neutral"));
        }
        else
        {
            result.AppendUTF8(context.szLocale);
        }
        
        if (cbPublicKey != 0)
        {
#ifndef DACCESS_COMPILE

            StrongNameBufferHolder<BYTE> pbToken;
            DWORD cbToken;
            CQuickBytes qb;
            
            if (StrongNameTokenFromPublicKey(pbPublicKey, cbPublicKey,
                                             &pbToken, &cbToken))
            {
                // two hex digits per byte
                WCHAR* szToken = (WCHAR*) qb.AllocNoThrow(sizeof(WCHAR) * (cbToken*2+1));
                if (szToken)
                {
#define TOHEX(a) ((a)>=10 ? L'a'+(a)-10 : L'0'+(a))
                    UINT x;
                    UINT y;
                    for ( x = 0, y = 0; x < cbToken; ++x )
                    {
                        WCHAR v = static_cast<WCHAR>(pbToken[x] >> 4);
                        szToken[y++] = TOHEX( v );
                        v = static_cast<WCHAR>(pbToken[x] & 0x0F);
                        szToken[y++] = TOHEX( v ); 
                    }
                    szToken[y] = L'\0';
                    
                    result.Append(W(", PublicKeyToken="));
                    result.Append(szToken);
#undef TOHEX
                }
            }
#endif

        }
        else
        {
            result.Append(W(", PublicKeyToken=null"));
        }
        
        if (dwFlags & afPA_Mask)
        {
            result.Append(W(", ProcessorArchitecture="));
            
            if (dwFlags & afPA_MSIL)
                result.Append(W("MSIL"));
            else if (dwFlags & afPA_x86)
                result.Append(W("x86"));
            else if (dwFlags & afPA_IA64)
                result.Append(W("IA64"));
            else if (dwFlags & afPA_AMD64)
                result.Append(W("AMD64"));
            else if (dwFlags & afPA_ARM)
                result.Append(W("ARM"));
        }
    }
}
 
 
// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------
inline void PEAssembly::GetDisplayName(SString &result, DWORD flags)
{
    CONTRACTL
    {
        PRECONDITION(CheckValue(result));
#ifndef DACCESS_COMPILE
        THROWS;
#else
        NOTHROW;
#endif // DACCESS_COMPILE
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
 
#ifndef DACCESS_COMPILE

#ifdef FEATURE_FUSION
    FusionBind::GetAssemblyNameDisplayName(GetFusionAssemblyName(), result, flags);
#else
    if ((flags == (ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN)) &&
        !m_sTextualIdentity.IsEmpty())
    {
        result.Set(m_sTextualIdentity);
    }
    else
    {
        AssemblySpec spec;
        spec.InitializeSpec(this);
        spec.GetFileOrDisplayName(flags, result);
    }
#endif // FEATURE_FUSION

#else
    IMDInternalImport *pImport = GetMDImport();
    GetFullyQualifiedAssemblyName(pImport, TokenFromRid(1, mdtAssembly), result, flags);
#endif //DACCESS_COMPILE
}

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

inline LPCSTR PEAssembly::GetSimpleName()
{
    CONTRACTL
    {
        NOTHROW;
        if (!m_bHasPersistentMDImport) { GC_TRIGGERS;} else {DISABLED(GC_TRIGGERS);};
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;
    
    LPCSTR name = "";
    IMDInternalImportHolder pImport = GetMDImport();
    if (pImport != NULL)
    {
        if (FAILED(pImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, &name, NULL, NULL)))
        {
            _ASSERTE(!"If this fires, then we have to throw for corrupted images");
            name = "";
        }
    }
    return name;
}

inline BOOL PEFile::IsStrongNamed()
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_NOTRIGGER);
        MODE_ANY;
    }
    CONTRACTL_END;
    
    DWORD flags = 0;
    IfFailThrow(GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, NULL, &flags));
    return (flags & afPublicKey) != NULL;
}


//---------------------------------------------------------------------------------------
//
// Check to see if this assembly has had its strong name signature verified yet.
//

inline BOOL PEFile::IsStrongNameVerified()
{
    LIMITED_METHOD_CONTRACT;
    return m_fStrongNameVerified;
}

inline const void *PEFile::GetPublicKey(DWORD *pcbPK)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pcbPK, NULL_OK));
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACTL_END;
    
    const void *pPK;
    IfFailThrow(GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), &pPK, pcbPK, NULL, NULL, NULL, NULL));
    return pPK;
}

inline ULONG PEFile::GetHashAlgId()
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACTL_END;
    
    ULONG hashAlgId;
    IfFailThrow(GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, &hashAlgId, NULL, NULL, NULL));
    return hashAlgId;
}

inline LPCSTR PEFile::GetLocale()
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_NOTRIGGER);
        MODE_ANY;
    }
    CONTRACTL_END;
    
    AssemblyMetaDataInternal md;
    IfFailThrow(GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, &md, NULL));
    return md.szLocale;
}

inline DWORD PEFile::GetFlags()
{
    CONTRACTL
    {
        PRECONDITION(IsAssembly());
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD flags;
    IfFailThrow(GetMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, NULL, &flags));
    return flags;
}

// In the cases where you know the module is loaded, and cannot tolerate triggering and
// loading, this alternative to PEFile::GetFlags is useful.  Profiling API uses this.
inline HRESULT PEFile::GetFlagsNoTrigger(DWORD * pdwFlags)
{
    CONTRACTL
    {
        PRECONDITION(IsAssembly());
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE (pdwFlags != NULL);

    if (!m_bHasPersistentMDImport)
        return E_FAIL;

    return GetPersistentMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, NULL, pdwFlags);
}

#ifdef FEATURE_CAS_POLICY
inline COR_TRUST *PEFile::GetAuthenticodeSignature()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (!m_fCheckedCertificate && HasSecurityDirectory())
    {
        CheckAuthenticodeSignature();
    }

    return m_certificate;
}
#endif

// ------------------------------------------------------------
// Hash support
// ------------------------------------------------------------

inline BOOL PEAssembly::HasStrongNameSignature()
{
    WRAPPER_NO_CONTRACT;

    if (IsDynamic())
        return FALSE;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        // The NGen images do not have strong name signature
        return FALSE;
    }
#endif // FEATURE_PREJIT

    return GetILimage()->HasStrongNameSignature();
}

//---------------------------------------------------------------------------------------
//
// Check to see that an assembly is not delay or test signed
//

inline BOOL PEAssembly::IsFullySigned()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        // If we are strongly named and successfully strong named, then we consider ourselves fully
        // signed since either our signature verified at ngen time, or skip verification was in effect
        // The only code that differentiates between skip verification and fully signed is in the strong
        // name verification path itself, and therefore we abstract that away at this level.
        //
        // Note that this is consistent with other abstractions at the PEFile level such as
        // HasStrongNameSignature()
        return IsStrongNamed();
    } else
#endif // FEATURE_PREJIT
    if (HasOpenedILimage())
    {
        return GetOpenedILimage()->IsStrongNameSigned();
    }
    else
    {
        return FALSE;
    }
}

#ifndef FEATURE_CORECLR
//---------------------------------------------------------------------------------------
//
// Mark that an assembly has had its strong name verification bypassed
//

inline void PEAssembly::SetStrongNameBypassed()
{
    LIMITED_METHOD_CONTRACT;
    m_fStrongNameBypassed = TRUE;
}

inline BOOL PEAssembly::NeedsModuleHashChecks()
{
    LIMITED_METHOD_CONTRACT;

    return ((m_flags & PEFILE_SKIP_MODULE_HASH_CHECKS) == 0) && !m_fStrongNameBypassed;
}
#endif // FEATURE_CORECLR

#ifdef FEATURE_CAS_POLICY
//---------------------------------------------------------------------------------------
//
// Verify the Authenticode and strong name signatures of an assembly during the assembly
// load code path.  To verify the strong name signature outside of assembly load, use the
// VefifyStrongName method instead.
// 
// If the applicaiton is using strong name bypass, then this method may not cause a real
// strong name verification, delaying the assembly's strong name load until we know that
// the verification is required.  If the assembly must be forced to have its strong name
// verified, then the VerifyStrongName method should also be chosen.
// 
// See code:AssemblySecurityDescriptor::ResolveWorker#StrongNameBypass
//

inline void PEAssembly::DoLoadSignatureChecks()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS; // Fusion uses crsts on AddRef/Release
        MODE_ANY;
    }
    CONTRACTL_END;

    ETWOnStartup(SecurityCatchCall_V1, SecurityCatchCallEnd_V1);

    // If this isn't mscorlib or a dynamic assembly, verify the Authenticode signature.
    if (IsSystem() || IsDynamic())
    {
        // If it was a dynamic module (or mscorlib), then we don't want to be doing module hash checks on it
        m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
    }

    // Check strong name signature. We only want to do this now if the application is not using the strong
    // name bypass feature.  Otherwise we'll delay strong name verification until we figure out how trusted
    // the assembly is.
    // 
    // For more information see code:AssemblySecurityDescriptor::ResolveWorker#StrongNameBypass

    // Make sure m_pMDImport is initialized as we need to call VerifyStrongName which calls GetFlags
    // BypassTrustedAppStrongNames = false is a relatively uncommon scenario so we need to make sure 
    // the initialization order is always correct and we don't miss this uncommon case
    _ASSERTE(GetMDImport());

    if (!g_pConfig->BypassTrustedAppStrongNames())
    { 
        VerifyStrongName();
    }
}
#endif // FEATURE_CAS_POLICY

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
inline PEAssembly *PEModule::GetAssembly()
{
    CONTRACT(PEAssembly *)
    {
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN m_assembly;
}

inline BOOL PEModule::IsResource()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;
#ifdef DACCESS_COMPILE
    _ASSERTE(m_bIsResource!=-1);
#else
    if (m_bIsResource==-1)
    {
        DWORD flags;
        if (FAILED(m_assembly->GetPersistentMDImport()->GetFileProps(m_token, NULL, NULL, NULL, &flags)))
        {
            _ASSERTE(!"If this fires, then we have to throw for corrupted images");
            flags = 0;
        }
        m_bIsResource=((flags & ffContainsNoMetaData) != 0);
    }
#endif
    
    return m_bIsResource;
}

inline LPCUTF8 PEModule::GetSimpleName()
{
    CONTRACT(LPCUTF8)
    {
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(strlen(RETVAL) > 0);
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACT_END;
    
    LPCUTF8 name;
    
    if (FAILED(m_assembly->GetPersistentMDImport()->GetFileProps(m_token, &name, NULL, NULL, NULL)))
    {
        _ASSERTE(!"If this fires, then we have to throw for corrupted images");
        name = "";
    }
    
    RETURN name;
}

inline mdFile PEModule::GetToken()
{
    LIMITED_METHOD_CONTRACT;
    return m_token;
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

#ifndef DACCESS_COMPILE
inline void PEFile::RestoreMDImport(IMDInternalImport* pImport)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;
    
    _ASSERTE(m_pMetadataLock->LockTaken() && m_pMetadataLock->IsWriterLock());
    if (m_pMDImport != NULL)
        return;
    m_pMDImport=pImport;
    if(m_pMDImport)
        m_pMDImport->AddRef();
}
#endif
inline void PEFile::OpenMDImport()
{
    WRAPPER_NO_CONTRACT;
    //need synchronization
    _ASSERTE(m_pMetadataLock->LockTaken() && m_pMetadataLock->IsWriterLock());
    OpenMDImport_Unsafe();
}

inline PEFile* PEFile::Dummy()
{
    return (PEFile*)(-1);
}

inline bool PEAssembly::HasBindableIdentity()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    return !IsAfContentType_WindowsRuntime(GetFlags());
}

inline bool PEAssembly::IsWindowsRuntime()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    return IsAfContentType_WindowsRuntime(GetFlags());
}

#endif  // PEFILE_INL_

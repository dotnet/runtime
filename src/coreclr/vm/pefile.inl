// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEFile.inl
//

// --------------------------------------------------------------------------------

#ifndef PEFILE_INL_
#define PEFILE_INL_

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
    return BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(m_pHostAssembly)->GetAssemblyName()->Hash(BINDER_SPACE::AssemblyName::INCLUDE_VERSION);
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
    if (HasNativeImage())
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
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
    }
}


inline BOOL PEFile::IsMarkedAsNoPlatform()
{
    WRAPPER_NO_CONTRACT;
    return (IsAfPA_NoPlatform(GetFlags()));
}


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

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

inline const SString& PEFile::GetPath()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (IsDynamic() || m_identity->IsInBundle ())
    {
        return SString::Empty();
    }
    return m_identity->GetPath();
}

//
// Returns the identity path even for single-file/bundled apps.
//
inline const SString& PEFile::GetIdentityPath()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (m_identity == nullptr)
    {
        return SString::Empty();
    }
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

    return FALSE;
}

inline BOOL PEFile::IsIStream() const
{
    LIMITED_METHOD_CONTRACT;

    return FALSE;
}

inline PEAssembly *PEFile::GetAssembly() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsAssembly());
    return dac_cast<PTR_PEAssembly>(this);
}

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

        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage)
        {
            retVal = pNativeImage->IsNativeILILOnly();
        }

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
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (HasOpenedILimage())
    {
#if defined(FEATURE_PREJIT)
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
#endif // defined(FEATURE_PREJIT)
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

inline BOOL PEFile::HasNativeOrReadyToRunImage()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return (HasNativeImage() || IsILImageReadyToRun());
}

inline PTR_PEImageLayout PEFile::GetLoadedIL()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(HasOpenedILimage());

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
        MODE_ANY;
    }
    CONTRACTL_END;
    if(IsDynamic())
        return TRUE;
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
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN ((m_flags & PEFILE_HAS_NATIVE_IMAGE_METADATA) != 0);
}
#endif


// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------
inline void PEAssembly::GetDisplayName(SString &result, DWORD flags)
{
    CONTRACTL
    {
        PRECONDITION(CheckValue(result));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    AssemblySpec spec;
    spec.InitializeSpec(this);
    spec.GetFileOrDisplayName(flags, result);
#else
    DacNotImpl();
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

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

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
#endif  // PEFILE_INL_

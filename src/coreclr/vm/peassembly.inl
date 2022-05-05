// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEAssembly.inl
//

// --------------------------------------------------------------------------------

#ifndef PEASSEMBLY_INL_
#define PEASSEMBLY_INL_

#include "check.h"
#include "simplerwlock.hpp"
#include "eventtrace.h"
#include "peimagelayout.inl"

#if CHECK_INVARIANTS
inline CHECK PEAssembly::Invariant()
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
        CHECK(m_PEImage == NULL);
        CHECK(CheckPointer(m_pEmitter));
    }
    else
    {
       CHECK(CheckPointer((PEImage*)m_PEImage));
    }
    CHECK_OK;
}
#endif // CHECK_INVARIANTS

// ------------------------------------------------------------
// AddRef/Release
// ------------------------------------------------------------

inline ULONG PEAssembly::AddRef()
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

inline ULONG PEAssembly::Release()
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
        PRECONDITION(CheckPointer(m_PEImage));
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;
    return m_pHostAssembly->GetAssemblyName()->Hash(BINDER_SPACE::AssemblyName::INCLUDE_VERSION);
}

inline void PEAssembly::ValidateForExecution()
{
    CONTRACTL
    {
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    //
    // Ensure reference assemblies are not loaded for execution
    //
    IMDInternalImport* mdImport = GetMDImport();
    if (mdImport->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly),
                                           g_ReferenceAssemblyAttribute,
                                           NULL,
                                           NULL) == S_OK) {
        ThrowHR(COR_E_LOADING_REFERENCE_ASSEMBLY, BFA_REFERENCE_ASSEMBLY);
    }

    //
    // Ensure platform is valid for execution
    //
    if (!IsDynamic())
    {
        if (IsMarkedAsNoPlatform())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
    }
}


inline BOOL PEAssembly::IsMarkedAsNoPlatform()
{
    WRAPPER_NO_CONTRACT;
    return (IsAfPA_NoPlatform(GetFlags()));
}


inline void PEAssembly::GetMVID(GUID *pMvid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END;

    IfFailThrow(GetMDImport()->GetScopeProps(NULL, pMvid));
}

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

inline const SString& PEAssembly::GetPath()
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

    if (IsDynamic() || m_PEImage->IsInBundle ())
    {
        return SString::Empty();
    }

    return m_PEImage->GetPath();
}

//
// Returns the identity path even for single-file/bundled apps.
//
inline const SString& PEAssembly::GetIdentityPath()
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

    if (m_PEImage == nullptr)
    {
        return SString::Empty();
    }

    return m_PEImage->GetPath();
}

#ifdef DACCESS_COMPILE
inline const SString &PEAssembly::GetModuleFileNameHint()
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
        return m_PEImage->GetModuleFileNameHintForDAC();
}
#endif // DACCESS_COMPILE

#ifdef LOGGING
inline LPCWSTR PEAssembly::GetDebugName()
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

inline BOOL PEAssembly::IsSystem() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_isSystem;
}

inline BOOL PEAssembly::IsDynamic() const
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_PEImage == NULL;
}

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------
inline IMDInternalImport* PEAssembly::GetMDImport()
{
    WRAPPER_NO_CONTRACT;

#ifdef DACCESS_COMPILE
    WRAPPER_NO_CONTRACT;
    return DacGetMDImport(this, true);
#else
    LIMITED_METHOD_CONTRACT;

    return m_pMDImport;
#endif
};

#ifndef DACCESS_COMPILE

inline IMetaDataImport2 *PEAssembly::GetRWImporter()
{
    CONTRACT(IMetaDataImport2 *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACT_END;

    if (m_pImporter == NULL)
        OpenImporter();

    RETURN m_pImporter;
}

inline IMetaDataEmit *PEAssembly::GetEmitter()
{
    CONTRACT(IMetaDataEmit *)
    {
        INSTANCE_CHECK;
        MODE_ANY;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
    }
    CONTRACT_END;

    if (m_pEmitter == NULL)
        OpenEmitter();

    RETURN m_pEmitter;
}


#endif // DACCESS_COMPILE

// Same as the managed Module.ScopeName property, this unconditionally looks in the
// metadata Module table to get the name.  Useful for profilers and others who don't
// like sugar coating on their names.
inline HRESULT PEAssembly::GetScopeName(LPCUTF8 * pszName)
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

inline BOOL PEAssembly::IsReadyToRun()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (HasPEImage())
    {
        return GetLoadedLayout()->HasReadyToRunHeader();
    }
    else
    {
        return FALSE;
    }
}

inline mdToken PEAssembly::GetEntryPointToken()
{
    WRAPPER_NO_CONTRACT;

    if (IsDynamic())
        return mdTokenNil;

    return GetPEImage()->GetEntryPointToken();
}

inline BOOL PEAssembly::IsILOnly()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    CONTRACT_VIOLATION(ThrowsViolation|GCViolation|FaultViolation);

    if (IsDynamic())
        return FALSE;

    return GetPEImage()->IsILOnly();
}

inline PTR_VOID PEAssembly::GetRvaField(RVA field)
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(CheckRvaField(field));
        PRECONDITION(HasLoadedPEImage());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Note that the native image Rva fields are currently cut off before
    // this point.  We should not get here for an IL only native image.

    RETURN dac_cast<PTR_VOID>(GetLoadedLayout()->GetRvaData(field,NULL_OK));
}

inline CHECK PEAssembly::CheckRvaField(RVA field)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(HasLoadedPEImage());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    // Note that the native image Rva fields are currently cut off before
    // this point.  We should not get here for an IL only native image.

    CHECK(GetLoadedLayout()->CheckRva(field,NULL_OK));
    CHECK_OK;
}

inline CHECK PEAssembly::CheckRvaField(RVA field, COUNT_T size)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(HasLoadedPEImage());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    // Note that the native image Rva fields are currently cut off before
    // this point.  We should not get here for an IL only native image.

    CHECK(GetLoadedLayout()->CheckRva(field, size,0,NULL_OK));
    CHECK_OK;
}

inline BOOL PEAssembly::HasTls()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsLoaded());
    }
    CONTRACTL_END;

    // Dynamic modules do not contain TLS data.
    if (IsDynamic())
        return FALSE;
    // ILOnly modules do not contain TLS data.
    else if (IsILOnly())
        return FALSE;
    else
        return GetLoadedLayout()->HasTls();
}

inline BOOL PEAssembly::IsRvaFieldTls(RVA field)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsLoaded());
    }
    CONTRACTL_END;

    if (!HasTls())
        return FALSE;

    PTR_VOID address = PTR_VOID(GetLoadedLayout()->GetRvaData(field));

    COUNT_T tlsSize;
    PTR_VOID tlsRange = GetLoadedLayout()->GetTlsRange(&tlsSize);

    return (address >= tlsRange
            && address < (dac_cast<PTR_BYTE>(tlsRange)+tlsSize));
}

inline UINT32 PEAssembly::GetFieldTlsOffset(RVA field)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckRvaField(field));
        PRECONDITION(IsRvaFieldTls(field));
        PRECONDITION(HasLoadedPEImage());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return (UINT32)(dac_cast<PTR_BYTE>(GetRvaField(field)) -
                                dac_cast<PTR_BYTE>(GetLoadedLayout()->GetTlsRange()));
}

inline UINT32 PEAssembly::GetTlsIndex()
{
    CONTRACTL
    {
        PRECONDITION(HasLoadedPEImage());
        INSTANCE_CHECK;
        PRECONDITION(HasTls());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return GetLoadedLayout()->GetTlsIndex();
}

inline const void *PEAssembly::GetInternalPInvokeTarget(RVA target)
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(CheckInternalPInvokeTarget(target));
        PRECONDITION(HasLoadedPEImage());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (void*)GetLoadedLayout()->GetRvaData(target);
}

inline CHECK PEAssembly::CheckInternalPInvokeTarget(RVA target)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(HasLoadedPEImage());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    CHECK(!IsILOnly());
    CHECK(GetLoadedLayout()->CheckRva(target));

    CHECK_OK;
}

inline IMAGE_COR_VTABLEFIXUP *PEAssembly::GetVTableFixups(COUNT_T *pCount/*=NULL*/)
{
    CONTRACT(IMAGE_COR_VTABLEFIXUP *)
    {
        PRECONDITION(HasLoadedPEImage());
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (IsDynamic() || IsILOnly())
    {
        if (pCount != NULL)
            *pCount = 0;
        RETURN NULL;
    }
    else
        RETURN GetLoadedLayout()->GetVTableFixups(pCount);
}

inline void *PEAssembly::GetVTable(RVA rva)
{
    CONTRACT(void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(HasLoadedPEImage());
        PRECONDITION(!IsILOnly());
        PRECONDITION(GetLoadedLayout()->CheckRva(rva));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (void *)GetLoadedLayout()->GetRvaData(rva);
}

// @todo: this is bad to expose. But it is needed to support current IJW thunks
inline HMODULE PEAssembly::GetIJWBase()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(!IsDynamic());
        PRECONDITION(HasLoadedPEImage());
        PRECONDITION(!IsILOnly());
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return (HMODULE) dac_cast<TADDR>(GetLoadedLayout()->GetBase());
}

inline PTR_VOID PEAssembly::GetDebuggerContents(COUNT_T *pSize/*=NULL*/)
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

    if (HasLoadedPEImage())
    {
        if (pSize != NULL)
            *pSize = GetLoadedLayout()->GetSize();

        RETURN GetLoadedLayout()->GetBase();
    }
    else
    {
        if (pSize != NULL)
            *pSize = 0;

        RETURN NULL;
    }
}

inline PTR_CVOID PEAssembly::GetLoadedImageContents(COUNT_T *pSize/*=NULL*/)
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

    if (HasLoadedPEImage())
    {
        if (pSize != NULL)
        {
            *pSize = GetLoadedLayout()->GetSize();
        }
        return GetLoadedLayout()->GetBase();
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
inline const void *PEAssembly::GetManagedFileContents(COUNT_T *pSize/*=NULL*/)
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(HasLoadedPEImage());
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        POSTCONDITION((!GetLoadedLayout()->GetSize()) || CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Right now, we will trigger a LoadLibrary for the caller's sake,
    // even if we are in a scenario where we could normally avoid it.
    EnsureLoaded();

    if (pSize != NULL)
        *pSize = GetLoadedLayout()->GetSize();


    RETURN GetLoadedLayout()->GetBase();
}
#endif // DACCESS_COMPILE

inline BOOL PEAssembly::IsPtrInPEImage(PTR_CVOID data)
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

    if (HasPEImage())
    {
        return GetPEImage()->IsPtrInImage(data);
    }
    else
        return FALSE;
}

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
    spec.GetDisplayName(flags, result);
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
        DISABLED(GC_TRIGGERS);
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCSTR name = "";
    IMDInternalImport* pImport = GetMDImport();
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

inline BOOL PEAssembly::IsStrongNamed()
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

inline const void *PEAssembly::GetPublicKey(DWORD *pcbPK)
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

inline ULONG PEAssembly::GetHashAlgId()
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

inline LPCSTR PEAssembly::GetLocale()
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

inline DWORD PEAssembly::GetFlags()
{
    CONTRACTL
    {
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

#endif  // PEASSEMBLY_INL_

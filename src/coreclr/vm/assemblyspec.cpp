// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: AssemblySpec.cpp
**
** Purpose: Implements Assembly binding class
**
**


**
===========================================================*/

#include "common.h"

#include <stdlib.h>

#include "assemblyspec.hpp"
#include "eeconfig.h"
#include "strongnameinternal.h"
#include "strongnameholders.h"
#include "eventtrace.h"
#include "assemblynative.hpp"

#include "../binder/inc/bindertracing.h"

#ifdef _DEBUG
// This debug-only wrapper for LookupAssembly is solely for the use of postconditions and
// assertions. The problem is that the real LookupAssembly can throw an OOM
// simply because it can't allocate scratch space. For the sake of asserting,
// we can treat those as successful lookups.
BOOL UnsafeVerifyLookupAssembly(AssemblySpecBindingCache *pCache, AssemblySpec *pSpec, DomainAssembly *pComparator)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    BOOL result = FALSE;

    EX_TRY
    {
        SCAN_IGNORE_FAULT; // Won't go away: This wrapper exists precisely to turn an OOM here into something our postconditions can deal with.
        result = (pComparator == pCache->LookupAssembly(pSpec));
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        result = ex->IsTransient();
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;

}
#endif

#ifdef _DEBUG
// This debug-only wrapper for LookupFile is solely for the use of postconditions and
// assertions. The problem is that the real LookupFile can throw an OOM
// simply because it can't allocate scratch space. For the sake of asserting,
// we can treat those as successful lookups.
BOOL UnsafeVerifyLookupFile(AssemblySpecBindingCache *pCache, AssemblySpec *pSpec, PEAssembly *pComparator)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    BOOL result = FALSE;

    EX_TRY
    {
        SCAN_IGNORE_FAULT; // Won't go away: This wrapper exists precisely to turn an OOM here into something our postconditions can deal with.
        result = pCache->LookupFile(pSpec)->Equals(pComparator);
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        result = ex->IsTransient();
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;

}

#endif

#ifdef _DEBUG

// This debug-only wrapper for Contains is solely for the use of postconditions and
// assertions. The problem is that the real Contains can throw an OOM
// simply because it can't allocate scratch space. For the sake of asserting,
// we can treat those as successful lookups.
BOOL UnsafeContains(AssemblySpecBindingCache *pCache, AssemblySpec *pSpec)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

    BOOL result = FALSE;

    EX_TRY
    {
        SCAN_IGNORE_FAULT; // Won't go away: This wrapper exists precisely to turn an OOM here into something our postconditions can deal with.
        result = pCache->Contains(pSpec);
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        result = ex->IsTransient();
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;

}
#endif



AssemblySpecHash::~AssemblySpecHash()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PtrHashMap::PtrIterator i = m_map.begin();
    while (!i.end())
    {
        AssemblySpec *s = (AssemblySpec*) i.GetValue();
        if (m_pHeap != NULL)
            s->~AssemblySpec();
        else
            delete s;

        ++i;
    }
}

HRESULT AssemblySpec::InitializeSpecInternal(mdToken kAssemblyToken,
                                  IMDInternalImport *pImport,
                                  DomainAssembly *pStaticParent,
                                  BOOL fAllowAllocation)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (fAllowAllocation) {GC_TRIGGERS;} else {GC_NOTRIGGER;};
        if (fAllowAllocation) {INJECT_FAULT(COMPlusThrowOM());} else {FORBID_FAULT;};
        NOTHROW;
        MODE_ANY;
        PRECONDITION(pImport->IsValidToken(kAssemblyToken));
        PRECONDITION(TypeFromToken(kAssemblyToken) == mdtAssembly
                     || TypeFromToken(kAssemblyToken) == mdtAssemblyRef);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        IfFailThrow(BaseAssemblySpec::Init(kAssemblyToken,pImport));

        // For static binds, we cannot reference a weakly named assembly from a strong named one.
        // (Note that this constraint doesn't apply to dynamic binds which is why this check is
        // not farther down the stack.)
        if (pStaticParent != NULL)
        {
            // We dont validate this for CoreCLR as there is no good use-case for this scenario.

            SetParentAssembly(pStaticParent);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
} // AssemblySpec::InitializeSpecInternal



void AssemblySpec::InitializeSpec(PEAssembly * pFile)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFile));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    IMDInternalImport* pImport = pFile->GetMDImport();
    mdAssembly a;
    IfFailThrow(pImport->GetAssemblyFromScope(&a));

    InitializeSpec(a, pImport, NULL);

    // Set the binding context for the AssemblySpec
    AssemblyBinder* pCurrentBinder = GetBinder();
    if (pCurrentBinder == NULL)
    {
        AssemblyBinder* pExpectedBinder = pFile->GetAssemblyBinder();
        // We should aways have the binding context in the PEAssembly.
        _ASSERTE(pExpectedBinder != NULL);
        SetBinder(pExpectedBinder);
    }
}

void AssemblySpec::AssemblyNameInit(ASSEMBLYNAMEREF* pAsmName)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(IsProtectedByGCFrame (pAsmName));
    }
    CONTRACTL_END;

    NativeAssemblyNameParts nameParts;

    StackSString ssName;
    if (m_pAssemblyName != NULL)
        SString(SString::Utf8Literal, m_pAssemblyName).ConvertToUnicode(ssName);
    nameParts._pName = (m_pAssemblyName != NULL) ? ssName.GetUnicode() : NULL;

    nameParts._major = m_context.usMajorVersion;
    nameParts._minor = m_context.usMinorVersion;
    nameParts._build = m_context.usBuildNumber;
    nameParts._revision = m_context.usRevisionNumber;

    SmallStackSString ssLocale;
    if (m_context.szLocale != NULL)
        SString(SString::Utf8Literal, m_context.szLocale).ConvertToUnicode(ssLocale);
    nameParts._pCultureName = (m_context.szLocale != NULL) ? ssLocale.GetUnicode() : NULL;

    nameParts._pPublicKeyOrToken = m_pbPublicKeyOrToken;
    nameParts._cbPublicKeyOrToken = m_cbPublicKeyOrToken;

    nameParts._flags = m_dwFlags;

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLY_NAME__CTOR);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*pAsmName);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(&nameParts);
    CALL_MANAGED_METHOD_NORET(args);
}

/* static */
void AssemblySpec::InitializeAssemblyNameRef(_In_ BINDER_SPACE::AssemblyName* assemblyName, _Out_ ASSEMBLYNAMEREF* assemblyNameRef)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(assemblyName != NULL);
        PRECONDITION(IsProtectedByGCFrame(assemblyNameRef));
    }
    CONTRACTL_END;

    AssemblySpec spec;
    spec.InitializeWithAssemblyIdentity(assemblyName);

    StackScratchBuffer nameBuffer;
    spec.SetName(assemblyName->GetSimpleName().GetUTF8(nameBuffer));

    StackScratchBuffer cultureBuffer;
    if (assemblyName->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE))
    {
        LPCSTR culture = assemblyName->IsNeutralCulture() ? "" : assemblyName->GetCulture().GetUTF8(cultureBuffer);
        spec.SetCulture(culture);
    }

    spec.AssemblyNameInit(assemblyNameRef);
}


// Check if the supplied assembly's public key matches up with the one in the Spec, if any
// Throws an appropriate exception in case of a mismatch
void AssemblySpec::MatchPublicKeys(Assembly *pAssembly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Check that the public keys are the same as in the AR.
    if (!IsStrongNamed())
        return;

    const void *pbPublicKey;
    DWORD cbPublicKey;
    pbPublicKey = pAssembly->GetPublicKey(&cbPublicKey);
    if (cbPublicKey == 0)
        ThrowHR(FUSION_E_PRIVATE_ASM_DISALLOWED);

    if (IsAfPublicKey(m_dwFlags))
    {
        if ((m_cbPublicKeyOrToken != cbPublicKey) ||
            memcmp(m_pbPublicKeyOrToken, pbPublicKey, m_cbPublicKeyOrToken))
        {
            ThrowHR(FUSION_E_REF_DEF_MISMATCH);
        }
    }
    else
    {
        // Ref has a token
        StrongNameBufferHolder<BYTE> pbStrongNameToken;
        DWORD cbStrongNameToken;

        IfFailThrow(StrongNameTokenFromPublicKey((BYTE*)pbPublicKey,
            cbPublicKey,
            &pbStrongNameToken,
            &cbStrongNameToken));

        if ((m_cbPublicKeyOrToken != cbStrongNameToken) ||
            memcmp(m_pbPublicKeyOrToken, pbStrongNameToken, cbStrongNameToken))
        {
            ThrowHR(FUSION_E_REF_DEF_MISMATCH);
        }
    }
}

Assembly *AssemblySpec::LoadAssembly(FileLoadLevel targetLevel, BOOL fThrowOnFileNotFound)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DomainAssembly * pDomainAssembly = LoadDomainAssembly(targetLevel, fThrowOnFileNotFound);
    if (pDomainAssembly == NULL) {
        _ASSERTE(!fThrowOnFileNotFound);
        return NULL;
    }
    return pDomainAssembly->GetAssembly();
}

AssemblyBinder* AssemblySpec::GetBinderFromParentAssembly(AppDomain *pDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pDomain != NULL);
    }
    CONTRACTL_END;

    AssemblyBinder *pParentAssemblyBinder = NULL;
    DomainAssembly *pParentDomainAssembly = GetParentAssembly();

    if(pParentDomainAssembly != NULL)
    {
        // Get the PEAssembly associated with the parent's domain assembly
        PEAssembly *pParentPEAssembly = pParentDomainAssembly->GetPEAssembly();
        pParentAssemblyBinder = pParentPEAssembly->GetAssemblyBinder();
    }

    if (GetPreferFallbackBinder())
    {
        // If we have been asked to use the fallback load context binder (currently only supported for AssemblyLoadContext.LoadFromAssemblyName),
        // then pretend we do not have any binder yet available.
        _ASSERTE(GetFallbackBinderForRequestingAssembly() != NULL);
        pParentAssemblyBinder = NULL;
    }

    if (pParentAssemblyBinder == NULL)
    {
        // If the parent assembly binder is not available, then we maybe dealing with one of the following
        // assembly scenarios:
        //
        // 1) Domain Neutral assembly
        // 2) Entrypoint assembly
        // 3) AssemblyLoadContext.LoadFromAssemblyName
        //
        // For (1) and (2), we will need to bind against the DefaultContext binder (aka TPA Binder). This happens
        // below if we do not find the parent assembly binder.
        //
        // For (3), fetch the fallback load context binder reference.

        pParentAssemblyBinder = GetFallbackBinderForRequestingAssembly();
    }

    if (!pParentAssemblyBinder)
    {
        // We can be here when loading assemblies via the host (e.g. ICLRRuntimeHost2::ExecuteAssembly) or dealing with assemblies
        // whose parent is a domain neutral assembly (see comment above for details).
        //
        // In such a case, the parent assembly (semantically) is CoreLibrary and thus, the default binding context should be
        // used as the parent assembly binder.
        pParentAssemblyBinder = static_cast<AssemblyBinder*>(pDomain->GetDefaultBinder());
    }

    return pParentAssemblyBinder;
}

DomainAssembly *AssemblySpec::LoadDomainAssembly(FileLoadLevel targetLevel,
                                                 BOOL fThrowOnFileNotFound)
{
    CONTRACT(DomainAssembly *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION((!fThrowOnFileNotFound && CheckPointer(RETVAL, NULL_OK))
                      || CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    ETWOnStartup (LoaderCatchCall_V1, LoaderCatchCallEnd_V1);
    AppDomain* pDomain = GetAppDomain();

    DomainAssembly* pAssembly = pDomain->FindCachedAssembly(this);
    if (pAssembly)
    {
        BinderTracing::AssemblyBindOperation bindOperation(this);
        bindOperation.SetResult(pAssembly->GetPEAssembly(), true /*cached*/);

        pDomain->LoadDomainAssembly(pAssembly, targetLevel);
        RETURN pAssembly;
    }

    PEAssemblyHolder pFile(pDomain->BindAssemblySpec(this, fThrowOnFileNotFound));
    if (pFile == NULL)
        RETURN NULL;

    pAssembly = pDomain->LoadDomainAssembly(this, pFile, targetLevel);

    RETURN pAssembly;
}

/* static */
Assembly *AssemblySpec::LoadAssembly(LPCSTR pSimpleName,
                                     AssemblyMetaDataInternal* pContext,
                                     const BYTE * pbPublicKeyOrToken,
                                     DWORD cbPublicKeyOrToken,
                                     DWORD dwFlags)
{
    CONTRACT(Assembly *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pSimpleName));
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    AssemblySpec spec;
    spec.Init(pSimpleName, pContext, pbPublicKeyOrToken, cbPublicKeyOrToken, dwFlags);

    RETURN spec.LoadAssembly(FILE_LOADED);
}

/* static */
Assembly *AssemblySpec::LoadAssembly(LPCWSTR pFilePath)
{
    CONTRACT(Assembly *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFilePath));
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    GCX_PREEMP();

    PEImageHolder pILImage;

    pILImage = PEImage::OpenImage(pFilePath,
        MDInternalImport_Default,
        Bundle::ProbeAppBundle(pFilePath));

    // Need to verify that this is a valid CLR assembly.
    if (!pILImage->CheckILFormat())
        THROW_BAD_FORMAT(BFA_BAD_IL, pILImage.GetValue());

    RETURN AssemblyNative::LoadFromPEImage(AppDomain::GetCurrentDomain()->GetDefaultBinder(), pILImage);
}

HRESULT AssemblySpec::CheckFriendAssemblyName()
{
    WRAPPER_NO_CONTRACT;

    // Version, Culture, Architecture, and publickeytoken are not permitted
    if ((m_context.usMajorVersion != (USHORT) -1) ||
        (m_context.szLocale != NULL) ||
        (IsAfPA_Specified(m_dwFlags)) ||
        (IsStrongNamed() && !HasPublicKey()))
    {
        return META_E_CA_BAD_FRIENDS_ARGS;
    }
    else
    {
        return S_OK;
    }
}

HRESULT AssemblySpec::EmitToken(
    IMetaDataAssemblyEmit *pEmit,
    mdAssemblyRef *pToken)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        SmallStackSString ssName;
        GetName(ssName);

        ASSEMBLYMETADATA AMD;

        AMD.usMajorVersion = m_context.usMajorVersion;
        AMD.usMinorVersion = m_context.usMinorVersion;
        AMD.usBuildNumber = m_context.usBuildNumber;
        AMD.usRevisionNumber = m_context.usRevisionNumber;

        if (m_context.szLocale) {
            AMD.cbLocale = MultiByteToWideChar(CP_UTF8, 0, m_context.szLocale, -1, NULL, 0);
            if(AMD.cbLocale==0)
                IfFailGo(HRESULT_FROM_GetLastError());
            AMD.szLocale = (LPWSTR) alloca(AMD.cbLocale * sizeof(WCHAR) );
            if(MultiByteToWideChar(CP_UTF8, 0, m_context.szLocale, -1, AMD.szLocale, AMD.cbLocale)==0)
                IfFailGo(HRESULT_FROM_GetLastError());
        }
        else {
            AMD.cbLocale = 0;
            AMD.szLocale = NULL;
        }

        // If we've been asked to emit a public key token in the reference but we've
        // been given a public key then we need to generate the token now.
        if (m_cbPublicKeyOrToken && IsAfPublicKey(m_dwFlags)) {
            StrongNameBufferHolder<BYTE> pbPublicKeyToken;
            DWORD cbPublicKeyToken;
            IfFailThrow(StrongNameTokenFromPublicKey(m_pbPublicKeyOrToken,
                m_cbPublicKeyOrToken,
                &pbPublicKeyToken,
                &cbPublicKeyToken));

            hr = pEmit->DefineAssemblyRef(pbPublicKeyToken,
                                          cbPublicKeyToken,
                                          ssName.GetUnicode(),
                                          &AMD,
                                          NULL,
                                          0,
                                          m_dwFlags & ~afPublicKey,
                                          pToken);
        }
        else {
            hr = pEmit->DefineAssemblyRef(m_pbPublicKeyOrToken,
                                          m_cbPublicKeyOrToken,
                                          ssName.GetUnicode(),
                                          &AMD,
                                          NULL,
                                          0,
                                          m_dwFlags,
                                          pToken);
        }

        hr = S_OK;
    ErrExit:
        ;
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

AssemblySpecBindingCache::AssemblySpecBindingCache()
{
    LIMITED_METHOD_CONTRACT;
}

AssemblySpecBindingCache::~AssemblySpecBindingCache()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Clear();
}

void AssemblySpecBindingCache::Clear()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PtrHashMap::PtrIterator i = m_map.begin();
    while (!i.end())
    {
        AssemblyBinding *b = (AssemblyBinding*) i.GetValue();
        if (m_pHeap == NULL)
            delete b;
        else
            b->~AssemblyBinding();

        ++i;
    }

    m_map.Clear();
}

void AssemblySpecBindingCache::Init(CrstBase *pCrst, LoaderHeap *pHeap)
{
    WRAPPER_NO_CONTRACT;

    LockOwner lock = {pCrst, IsOwnerOfCrst};
    m_map.Init(INITIAL_ASM_SPEC_HASH_SIZE, CompareSpecs, TRUE, &lock);
    m_pHeap = pHeap;
}

AssemblySpecBindingCache::AssemblyBinding* AssemblySpecBindingCache::LookupInternal(AssemblySpec* pSpec, BOOL fThrow)
{
    CONTRACTL
    {
        if (fThrow)
        {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        else
        {
            GC_NOTRIGGER;
            NOTHROW;
            FORBID_FAULT;
        }
        MODE_ANY;
        PRECONDITION(pSpec != NULL);
    }
    CONTRACTL_END;

    UPTR key = (UPTR)pSpec->Hash();

    AssemblyBinder *pBinderForLookup = NULL;
    bool fGetBindingContextFromParent = true;

    // Check if the AssemblySpec already has specified its binding context. This will be set for assemblies that are
    // attempted to be explicitly bound using AssemblyLoadContext LoadFrom* methods.
    pBinderForLookup = pSpec->GetBinder();

    if (pBinderForLookup != NULL)
    {
        // We are working with the actual binding context in which the assembly was expected to be loaded.
        // Thus, we don't need to get it from the parent assembly.
        fGetBindingContextFromParent = false;
    }

    if (fGetBindingContextFromParent)
    {
        pBinderForLookup = pSpec->GetBinderFromParentAssembly(pSpec->GetAppDomain());
        pSpec->SetBinder(pBinderForLookup);
    }

    if (pBinderForLookup)
    {
        key = key ^ (UPTR)pBinderForLookup;
    }

    AssemblyBinding* pEntry = (AssemblyBinding *)m_map.LookupValue(key, pSpec);

    // Reset the binding context if one was originally never present in the AssemblySpec and we didnt find any entry
    // in the cache.
    if (fGetBindingContextFromParent)
    {
        if (pEntry == (AssemblyBinding *) INVALIDENTRY)
        {
            pSpec->SetBinder(NULL);
        }
    }

    return pEntry;
}

BOOL AssemblySpecBindingCache::Contains(AssemblySpec *pSpec)
{
    WRAPPER_NO_CONTRACT;
    return (LookupInternal(pSpec, TRUE) != (AssemblyBinding *) INVALIDENTRY);
}

DomainAssembly *AssemblySpecBindingCache::LookupAssembly(AssemblySpec *pSpec,
                                                         BOOL fThrow /*=TRUE*/)
{
    CONTRACT(DomainAssembly *)
    {
        INSTANCE_CHECK;
        if (fThrow) {
            GC_TRIGGERS;
            THROWS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        else {
            GC_NOTRIGGER;
            NOTHROW;
            FORBID_FAULT;
        }
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AssemblyBinding *entry = (AssemblyBinding *) INVALIDENTRY;

    entry = LookupInternal(pSpec, fThrow);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
        RETURN NULL;
    else
    {
        if ((entry->GetAssembly() == NULL) && fThrow)
        {
            // May be either unloaded, or an exception occurred.
            entry->ThrowIfError();
        }

        RETURN entry->GetAssembly();
    }
}

PEAssembly *AssemblySpecBindingCache::LookupFile(AssemblySpec *pSpec, BOOL fThrow /*=TRUE*/)
{
    CONTRACT(PEAssembly *)
    {
        INSTANCE_CHECK;
        if (fThrow) {
            GC_TRIGGERS;
            THROWS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        else {
            GC_NOTRIGGER;
            NOTHROW;
            FORBID_FAULT;
        }
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AssemblyBinding *entry = (AssemblyBinding *) INVALIDENTRY;
    entry = LookupInternal(pSpec, fThrow);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
        RETURN NULL;
    else
    {
        if (fThrow && (entry->GetFile() == NULL))
        {
            CONSISTENCY_CHECK(entry->IsError());
            entry->ThrowIfError();
        }

        RETURN entry->GetFile();
    }
}


class AssemblyBindingHolder
{
public:
    AssemblyBindingHolder()
    {
        LIMITED_METHOD_CONTRACT;
        m_entry = NULL;
        m_pHeap = NULL;
    }

    AssemblySpecBindingCache::AssemblyBinding *CreateAssemblyBinding(LoaderHeap *pHeap)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        CONTRACTL_END

        m_pHeap = pHeap;
        if (pHeap)
        {
            m_entry = new (m_amTracker.Track(pHeap->AllocMem(S_SIZE_T(sizeof(AssemblySpecBindingCache::AssemblyBinding))))) AssemblySpecBindingCache::AssemblyBinding;
        }
        else
        {
            m_entry = new AssemblySpecBindingCache::AssemblyBinding;
        }
        return m_entry;
    }

    ~AssemblyBindingHolder()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            FORBID_FAULT;
        }
        CONTRACTL_END

        if (m_entry)
        {
            if (m_pHeap)
            {
                // just call destructor - m_amTracker will delete the memory for m_entry itself.
                m_entry->~AssemblyBinding();
            }
            else
            {
                delete m_entry;
            }
        }
    }

    void SuppressRelease()
    {
        LIMITED_METHOD_CONTRACT;
        m_entry = NULL;
        m_pHeap = NULL;
        m_amTracker.SuppressRelease();
    }

    AllocMemTracker *GetPamTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_amTracker;
    }



private:
    AssemblySpecBindingCache::AssemblyBinding *m_entry;
    LoaderHeap                                *m_pHeap;
    AllocMemTracker                            m_amTracker;
};

// NOTE ABOUT STATE OF CACHE ENTRIES:
//
// A cache entry can be in one of 4 states:
// 1. Empty (no entry)
// 2. File (a PEAssembly has been bound, but not yet an Assembly)
// 3. Assembly (Both a PEAssembly & Assembly are available.)
// 4. Error (an error has occurred)
//
// The legal state transitions are:
// 1 -> any
// 2 -> 3
// 2 -> 4


BOOL AssemblySpecBindingCache::StoreAssembly(AssemblySpec *pSpec, DomainAssembly *pAssembly)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(UnsafeContains(this, pSpec));
        POSTCONDITION(UnsafeVerifyLookupAssembly(this, pSpec, pAssembly));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

    AssemblyBinder* pBinderContextForLookup = pAssembly->GetPEAssembly()->GetAssemblyBinder();
    key = key ^ (UPTR)pBinderContextForLookup;

    if (!pSpec->GetBinder())
    {
        pSpec->SetBinder(pBinderContextForLookup);
    }

    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        AssemblyBindingHolder abHolder;

        LoaderHeap* pHeap = m_pHeap;
        if (pAssembly->IsCollectible())
        {
            pHeap = pAssembly->GetLoaderAllocator()->GetHighFrequencyHeap();
        }

        entry = abHolder.CreateAssemblyBinding(pHeap);
        entry->Init(pSpec,pAssembly->GetPEAssembly(),pAssembly,NULL,pHeap, abHolder.GetPamTracker());

        m_map.InsertValue(key, entry);

        abHolder.SuppressRelease();

        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"StorePEAssembly (StoreAssembly): Add cached entry (%p) with PEAssembly %p",entry,pAssembly->GetPEAssembly());
        RETURN TRUE;
    }
    else
    {
        if (!entry->IsError())
        {
            if (entry->GetAssembly() != NULL)
            {
                // OK if this is a duplicate
                if (entry->GetAssembly() == pAssembly)
                    RETURN TRUE;
            }
            else
            {
                // OK if we have a matching PEAssembly
                if (entry->GetFile() != NULL
                    && pAssembly->GetPEAssembly()->Equals(entry->GetFile()))
                {
                    entry->SetAssembly(pAssembly);
                    RETURN TRUE;
                }
            }
        }

        // Invalid cache transition (see above note about state transitions)
        RETURN FALSE;
    }
}

// Note that this routine may be called outside a lock, so may be racing with another thread.
// Returns TRUE if add was successful - if FALSE is returned, caller should honor current
// cached value to ensure consistency.

BOOL AssemblySpecBindingCache::StorePEAssembly(AssemblySpec *pSpec, PEAssembly *pPEAssembly)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION((!RETVAL) || (UnsafeContains(this, pSpec) && UnsafeVerifyLookupFile(this, pSpec, pPEAssembly)));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

    AssemblyBinder* pBinderContextForLookup = pPEAssembly->GetAssemblyBinder();
    key = key ^ (UPTR)pBinderContextForLookup;

    if (!pSpec->GetBinder())
    {
        pSpec->SetBinder(pBinderContextForLookup);
    }

    AssemblyBinding *entry = (AssemblyBinding *) m_map.LookupValue(key, pSpec);

    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        AssemblyBindingHolder abHolder;

        LoaderHeap* pHeap = m_pHeap;

        if (pBinderContextForLookup != NULL)
        {
            LoaderAllocator* pLoaderAllocator = pBinderContextForLookup->GetLoaderAllocator();

            // Assemblies loaded with AssemblyLoadContext need to use a different heap if
            // marked as collectible
            if (pLoaderAllocator)
            {
                pHeap = pLoaderAllocator->GetHighFrequencyHeap();
            }
        }

        entry = abHolder.CreateAssemblyBinding(pHeap);

        entry->Init(pSpec, pPEAssembly,NULL,NULL,pHeap, abHolder.GetPamTracker());

        m_map.InsertValue(key, entry);
        abHolder.SuppressRelease();

        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"StorePEAssembly: Add cached entry (%p) with PEAssembly %p\n", entry, pPEAssembly);

        RETURN TRUE;
    }
    else
    {
        if (!entry->IsError())
        {
            // OK if this is a duplicate
            if (entry->GetFile() != NULL
                && pPEAssembly->Equals(entry->GetFile()))
                RETURN TRUE;
        }
        else
        if (entry->IsPostBindError())
        {
            // Another thread has reported what's going to happen later.
            entry->ThrowIfError();

        }
        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"Incompatible cached entry found (%p) when adding PEAssembly %p\n", entry, pPEAssembly);
        // Invalid cache transition (see above note about state transitions)
        RETURN FALSE;
    }
}

BOOL AssemblySpecBindingCache::StoreException(AssemblySpec *pSpec, Exception* pEx)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        DISABLED(POSTCONDITION(UnsafeContains(this, pSpec))); //<TODO>@todo: Getting violations here - StoreExceptions could happen anywhere so this is possibly too aggressive.</TODO>
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    UPTR key = (UPTR)pSpec->Hash();

    AssemblyBinding *entry = LookupInternal(pSpec, TRUE);
    if (entry == (AssemblyBinding *) INVALIDENTRY)
    {
        // TODO: Merge this with the failure lookup in the binder
        //
        // Since no entry was found for this assembly in any binding context, save the failure
        // in the DefaultBinder context
        AssemblyBinder* pBinderToSaveException = NULL;
        pBinderToSaveException = pSpec->GetBinder();
        if (pBinderToSaveException == NULL)
        {
            pBinderToSaveException = pSpec->GetBinderFromParentAssembly(pSpec->GetAppDomain());
            key = key ^ (UPTR)pBinderToSaveException;
        }
    }

    if (entry == (AssemblyBinding *) INVALIDENTRY) {
        AssemblyBindingHolder abHolder;
        entry = abHolder.CreateAssemblyBinding(m_pHeap);

        entry->Init(pSpec,NULL,NULL,pEx,m_pHeap, abHolder.GetPamTracker());

        m_map.InsertValue(key, entry);
        abHolder.SuppressRelease();

        STRESS_LOG2(LF_CLASSLOADER,LL_INFO10,"StorePEAssembly (StoreException): Add cached entry (%p) with exception %p",entry,pEx);
        RETURN TRUE;
    }
    else
    {
        // OK if this is a duplicate
        if (entry->IsError())
        {
            if (entry->GetHR() == pEx->GetHR())
                RETURN TRUE;
        }
        else
        {
            // OK to transition to error if we don't have an Assembly yet
            if (entry->GetAssembly() == NULL)
            {
                entry->InitException(pEx);
                RETURN TRUE;
            }
        }

        // Invalid cache transition (see above note about state transitions)
        RETURN FALSE;
    }
}

BOOL AssemblySpecBindingCache::RemoveAssembly(DomainAssembly* pAssembly)
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pAssembly != NULL);
    }
    CONTRACT_END;
    BOOL result = FALSE;
    PtrHashMap::PtrIterator i = m_map.begin();
    while (!i.end())
    {
        AssemblyBinding* entry = (AssemblyBinding*)i.GetValue();
        if (entry->GetAssembly() == pAssembly)
        {
            UPTR key = i.GetKey();
            m_map.DeleteValue(key, entry);

            if (m_pHeap == NULL)
                delete entry;
            else
                entry->~AssemblyBinding();

            result = TRUE;
        }
        ++i;
    }

    RETURN result;
}

/* static */
BOOL AssemblySpecHash::CompareSpecs(UPTR u1, UPTR u2)
{
    // the same...
    WRAPPER_NO_CONTRACT;
    return AssemblySpecBindingCache::CompareSpecs(u1,u2);
}

/* static */
BOOL AssemblySpecBindingCache::CompareSpecs(UPTR u1, UPTR u2)
{
    WRAPPER_NO_CONTRACT;
    AssemblySpec *a1 = (AssemblySpec *) (u1 << 1);
    AssemblySpec *a2 = (AssemblySpec *) u2;

    return a1->CompareEx(a2);
}

DomainAssembly * AssemblySpec::GetParentAssembly()
{
    LIMITED_METHOD_CONTRACT;
    return m_pParentAssembly;
}

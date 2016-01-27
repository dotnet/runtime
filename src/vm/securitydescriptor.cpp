// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"

#include "security.h"
#include "eventtrace.h"

///////////////////////////////////////////////////////////////////////////////
//
//  [SecurityDescriptor]
//  |
//  |
//  +----[PEFileSecurityDescriptor]
//
///////////////////////////////////////////////////////////////////////////////

BOOL SecurityDescriptor::CanCallUnmanagedCode () const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved() || m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress());
    } CONTRACTL_END;

    return CheckSpecialFlag(1 << SECURITY_UNMANAGED_CODE);
}

#ifndef DACCESS_COMPILE

OBJECTREF SecurityDescriptor::GetGrantedPermissionSet(OBJECTREF* pRefusedPermissions)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsResolved() || m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress());
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    if (pRefusedPermissions)
        *pRefusedPermissions = ObjectFromLazyHandle(m_hGrantDeniedPermissionSet, m_pLoaderAllocator);
    return ObjectFromLazyHandle(m_hGrantedPermissionSet, m_pLoaderAllocator);
#else
    return NULL;
#endif
}

//
// Returns TRUE if the given zone has the given special permission.
//
#ifdef FEATURE_CAS_POLICY
BOOL SecurityDescriptor::CheckQuickCache(SecurityConfig::QuickCacheEntryType all, DWORD dwZone)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());
        PRECONDITION(SecurityPolicy::s_fPolicyInitialized);
    } CONTRACTL_END;

    static const SecurityConfig::QuickCacheEntryType zoneTable[] =
    {
        SecurityConfig::FullTrustZoneMyComputer,
        SecurityConfig::FullTrustZoneIntranet,
        SecurityConfig::FullTrustZoneTrusted,
        SecurityConfig::FullTrustZoneInternet,
        SecurityConfig::FullTrustZoneUntrusted
    };

    // If an additional evidence was provided, then perform the normal
    // policy resolution. This is true for all AppDomains and also for
    // assemblies loaded with a specific additional evidence. Note that
    // for the default AppDomain, the policy resolution code paths short
    // circuits the parsing of the security XML files by granting FullTrust
    // to the default AppDomain.

    if (m_hAdditionalEvidence != NULL)
        return FALSE;

    BOOL fMachine = SecurityConfig::GetQuickCacheEntry(SecurityConfig::MachinePolicyLevel, all);
    BOOL fUser = SecurityConfig::GetQuickCacheEntry(SecurityConfig::UserPolicyLevel, all);
    BOOL fEnterprise = SecurityConfig::GetQuickCacheEntry(SecurityConfig::EnterprisePolicyLevel, all);

    if (fMachine && fUser && fEnterprise)
        return TRUE;

    // If we can't match for all, try for our zone.
    if (dwZone == 0xFFFFFFFF)
        return FALSE;

    fMachine = SecurityConfig::GetQuickCacheEntry(SecurityConfig::MachinePolicyLevel, zoneTable[dwZone]);
    fUser = SecurityConfig::GetQuickCacheEntry(SecurityConfig::UserPolicyLevel, zoneTable[dwZone]);
    fEnterprise = SecurityConfig::GetQuickCacheEntry(SecurityConfig::EnterprisePolicyLevel, zoneTable[dwZone]);

    return (fMachine && fUser && fEnterprise);
}
#endif // FEATURE_CAS_POLICY

#endif // DACCESS_COMPILE

#ifdef FEATURE_CAS_POLICY
BOOL SecurityDescriptor::IsEvidenceComputed() const
{
    LIMITED_METHOD_CONTRACT;
    return m_fEvidenceComputed;
}
#endif //FEATURE_CAS_POLICY

//
// This method will return TRUE if this object is fully trusted.
//

BOOL SecurityDescriptor::IsFullyTrusted ()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        SO_TOLERANT;
        PRECONDITION(IsResolved() || m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress());
    } CONTRACTL_END;

    return CheckSpecialFlag(1 << SECURITY_FULL_TRUST);
}

BOOL SecurityDescriptor::IsResolved() const
{
    LIMITED_METHOD_CONTRACT;
    return m_fSDResolved;
}

DWORD SecurityDescriptor::GetSpecialFlags() const
{
    LIMITED_METHOD_CONTRACT;
    return m_dwSpecialFlags;
}

#ifndef DACCESS_COMPILE
void SecurityDescriptor::SetGrantedPermissionSet(OBJECTREF GrantedPermissionSet,
                                                        OBJECTREF DeniedPermissionSet,
                                                        DWORD dwSpecialFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    GCPROTECT_BEGIN(DeniedPermissionSet);
    StoreObjectInLazyHandle(m_hGrantedPermissionSet, GrantedPermissionSet, m_pLoaderAllocator);
    StoreObjectInLazyHandle(m_hGrantDeniedPermissionSet, DeniedPermissionSet, m_pLoaderAllocator);
    GCPROTECT_END();
#endif

    if (dwSpecialFlags & (1 << SECURITY_FULL_TRUST))
    {
        m_dwSpecialFlags = 0xFFFFFFFF; // Fulltrust means that all possible quick checks should succeed, so we set all flags
    }
    else
    {
        m_dwSpecialFlags = dwSpecialFlags;
    }

    m_fSDResolved = TRUE;
}


#ifdef FEATURE_CAS_POLICY
void SecurityDescriptor::SetEvidence(OBJECTREF evidence)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(evidence != NULL);
    }
    CONTRACTL_END;

    if (evidence != NULL)
    {
        StoreObjectInLazyHandle(m_hAdditionalEvidence, evidence, m_pLoaderAllocator);
        SetEvidenceComputed();
    }
}
#endif // FEATURE_CAS_POLICY
#endif // !DACCESS_COMPILE

AppDomain* SecurityDescriptor::GetDomain() const
{
    LIMITED_METHOD_CONTRACT;
    return m_pAppDomain;
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_CAS_POLICY

//---------------------------------------------------------------------------------------
//
// Build an evidence collection which can generate evidence about a PEFile
//
// Arguments:
//    pPEFile                 - PEFile the evidence collection will generate evidence for
//    objHostSuppliedEvidence - additional evidence to merge into the collection supplied by the host
//
// Return Value:
//    Evidence collection which targets this PEFile
//
// Notes:
//    Calls System.Security.Policy.PEFileEvidenceFactory.CreateSecurityIdentity
//

// static
OBJECTREF PEFileSecurityDescriptor::BuildEvidence(PEFile *pPEFile, const OBJECTREF& objHostSuppliedEvidence)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pPEFile));
    }
    CONTRACTL_END;

    struct
    {
        SAFEHANDLE objPEFile;
        OBJECTREF objHostSuppliedEvidence;
        OBJECTREF objEvidence;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    BEGIN_SO_INTOLERANT_CODE(GetThread());

    gc.objPEFile = pPEFile->GetSafeHandle();
    gc.objHostSuppliedEvidence = objHostSuppliedEvidence;

    MethodDescCallSite createSecurityIdentity(METHOD__PEFILE_EVIDENCE_FACTORY__CREATE_SECURITY_IDENTITY);

    ARG_SLOT args[] =
    {
        ObjToArgSlot(gc.objPEFile),
        ObjToArgSlot(gc.objHostSuppliedEvidence)
    };

    gc.objEvidence = createSecurityIdentity.Call_RetOBJECTREF(args);

    END_SO_INTOLERANT_CODE;
    GCPROTECT_END();

    return gc.objEvidence;
}

#endif // FEATURE_CAS_POLICY

#ifndef FEATURE_CORECLR
BOOL PEFileSecurityDescriptor::QuickIsFullyTrusted()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef CROSSGEN_COMPILE
    return TRUE;
#else
    if (!m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled())
    {
        return TRUE;
    }
    else if (m_pAppDomain->IsCompilationDomain())
    {
        return TRUE;
    }
    else
    {
        return CheckQuickCache(SecurityConfig::FullTrustAll, GetZone());
    }
#endif
}

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------------------------------------
//
// Get the evidence for this PE file
//

OBJECTREF PEFileSecurityDescriptor::GetEvidence()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_pAppDomain == GetAppDomain());
        INJECT_FAULT(COMPlusThrowOM());
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // If we already have evidence, then just return that
    if (IsEvidenceComputed())
        return ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);

    struct
    {
        OBJECTREF objHostProvidedEvidence;
        OBJECTREF objEvidence;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    BEGIN_SO_INTOLERANT_CODE(GetThread());

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
        _ASSERTE(gc.objHostProvidedEvidence == NULL || GetAppDomain() == gc.objHostProvidedEvidence->GetAppDomain());
#endif // CHECK_APP_DOMAIN_LEAKS

    gc.objHostProvidedEvidence = ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);
    gc.objEvidence = PEFileSecurityDescriptor::BuildEvidence(m_pPEFile, gc.objHostProvidedEvidence);
    SetEvidence(gc.objEvidence);

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
        _ASSERTE(gc.objEvidence == NULL || GetAppDomain() == gc.objEvidence->GetAppDomain());
#endif // CHECK_APP_DOMAIN_LEAKS

    END_SO_INTOLERANT_CODE;

    GCPROTECT_END();

    return gc.objEvidence;
}

DWORD PEFileSecurityDescriptor::GetZone()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());
    }
    CONTRACTL_END;

    SecZone       dwZone = NoZone;
    BEGIN_SO_INTOLERANT_CODE(GetThread());

    StackSString    codebase;
    BYTE        rbUniqueID[MAX_SIZE_SECURITY_ID];
    DWORD       cbUniqueID = sizeof(rbUniqueID);

    m_pPEFile->GetSecurityIdentity(codebase, &dwZone, 0, rbUniqueID, &cbUniqueID);
    END_SO_INTOLERANT_CODE;
    return dwZone;
}
#endif // !CROSSGEN_COMPILE

void PEFileSecurityDescriptor::Resolve()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    if (IsResolved())
        return;
    ResolveWorker();
}

void PEFileSecurityDescriptor::ResolveWorker()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (NingenEnabled()) {
        SetGrantedPermissionSet(NULL, NULL, 0xFFFFFFFF);
    }

#ifndef CROSSGEN_COMPILE
    struct _gc
    {
        OBJECTREF evidence;         // Object containing evidence
        OBJECTREF granted;          // Policy based Granted Permission
        OBJECTREF grantdenied;      // Policy based explicitly Denied Permissions
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    DWORD dwSpecialFlags = 0;
    if (QuickIsFullyTrusted())
    {
        Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
        dwSpecialFlags = 0xFFFFFFFF;
    }
    else
    {
        if (IsEvidenceComputed())
        {
            gc.evidence = ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);
        }
        else
        {
            gc.evidence = GetEvidence();
        }

        if (!m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled())
        {
            gc.granted = SecurityPolicy::ResolveGrantSet(gc.evidence, &dwSpecialFlags, FALSE);
        }
        else
        {
            gc.granted = SecurityPolicy::ResolveCasPolicy(gc.evidence,
                                                          NULL,
                                                          NULL,
                                                          NULL,
                                                          &gc.grantdenied,
                                                          &dwSpecialFlags,
                                                          FALSE);
        }
    }

    SetGrantedPermissionSet(gc.granted, NULL, dwSpecialFlags);

    GCPROTECT_END();
#endif // CROSSGEN_COMPILE
}

BOOL PEFileSecurityDescriptor::AllowBindingRedirects()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsResolved());
    } CONTRACTL_END;

    ETWOnStartup (AllowBindingRedirs_V1, AllowBindingRedirsEnd_V1);

    return CheckSpecialFlag(1 << SECURITY_BINDING_REDIRECTS);
}

#endif // FEATURE_CORECLR

#endif // !DACCESS_COMPILE

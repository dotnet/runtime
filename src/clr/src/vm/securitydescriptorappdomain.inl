// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 

#ifndef __SECURITYDESCRIPTORAPPDOMAIN_INL__
#define __SECURITYDESCRIPTORAPPDOMAIN_INL__

#ifndef DACCESS_COMPILE

inline ApplicationSecurityDescriptor::ApplicationSecurityDescriptor(AppDomain *pAppDomain) :
    SecurityDescriptorBase<IApplicationSecurityDescriptor>(pAppDomain, NULL, NULL, pAppDomain->GetLoaderAllocator()),
#ifdef FEATURE_PLS
    m_hDomainPermissionListSet(NULL),
#endif // FEAUTRE_PLS
    m_dwDomainWideSpecialFlags(0xFFFFFFFF),
    m_fIsInitializationInProgress(TRUE),
    m_fIsDefaultAppdomain(FALSE),
    m_fIsDefaultAppdomainEvidence(FALSE),
    m_fHomogeneous(FALSE),
    m_fRuntimeSuppliedHomogenousGrantSet(FALSE),
#ifdef FEATURE_CAS_POLICY
    m_fLegacyCasPolicy(Security::IsProcessWideLegacyCasPolicyEnabled()),
#endif // FEATURE_CAS_POLICY
    m_dwHostSecurityManagerFlags(HOST_NONE),
    m_fContainsAnyRefusedPermissions(FALSE),
    m_fIsPreResolved(FALSE),
    m_fPreResolutionFullTrust(FALSE),
    m_fPreResolutionHomogeneous(FALSE)
#ifdef FEATURE_APTCA
    ,m_pConditionalAptcaCache(new ConditionalAptcaCache(pAppDomain))
#endif // FEATURE_APTCA
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    return;
}

#ifdef FEATURE_APTCA
inline ApplicationSecurityDescriptor::~ApplicationSecurityDescriptor()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    delete m_pConditionalAptcaCache;
}
#endif // FEATURE_APTCA

inline void ApplicationSecurityDescriptor::ResetInitializationInProgress()
{
    LIMITED_METHOD_CONTRACT;
    m_fIsInitializationInProgress = FALSE;
}

// Checks for one of the special domain wide flags  such as if we are currently in a "fully trusted"
// environment or if unmanaged code access is allowed at this time
inline BOOL ApplicationSecurityDescriptor::CheckDomainWideSpecialFlag(DWORD flags) const
{
    LIMITED_METHOD_CONTRACT;
    return (m_dwDomainWideSpecialFlags & flags);
}
inline void ApplicationSecurityDescriptor::SetDefaultAppDomain()
{
    LIMITED_METHOD_CONTRACT;
    m_fIsDefaultAppdomain = TRUE;
    m_fIsDefaultAppdomainEvidence = TRUE; // Follows from the fact that this is a default AppDomain
}

inline void ApplicationSecurityDescriptor::SetDefaultAppDomainEvidence()
{
    LIMITED_METHOD_CONTRACT;
    m_fIsDefaultAppdomainEvidence = TRUE; // This need not be a default AD, but has no evidence. So we'll use the default AD evidence
}

inline void ApplicationSecurityDescriptor::SetHomogeneousFlag(BOOL fRuntimeSuppliedHomogenousGrantSet)
{
    LIMITED_METHOD_CONTRACT;
    m_fHomogeneous = TRUE;
    m_fRuntimeSuppliedHomogenousGrantSet = fRuntimeSuppliedHomogenousGrantSet;
}

#ifdef FEATURE_CAS_POLICY

// Does the domain's HSM need to be consulted for assemblies loaded into the domain
inline BOOL ApplicationSecurityDescriptor::CallHostSecurityManagerForAssemblies()
{
    LIMITED_METHOD_CONTRACT;

    // We always need to call the HSM if it wants to specify the assembly's grant set
    if (m_dwHostSecurityManagerFlags & HOST_RESOLVE_POLICY)
    {
        return TRUE;
    }

    // In legacy CAS mode, we also need to call the HSM if it wants to supply evidence or if we have an
    // AppDomain policy level
    if (IsLegacyCasPolicyEnabled())
    {
        if ((m_dwHostSecurityManagerFlags & HOST_ASM_EVIDENCE) ||
            (m_dwHostSecurityManagerFlags & HOST_POLICY_LEVEL))
        {
            return TRUE;
        }
    }

    return FALSE;
}

#endif // FEATURE_CAS_POLICY

#endif // #ifndef DACCESS_COMPILE

#endif // !__SECURITYDESCRIPTORAPPDOMAIN_INL__

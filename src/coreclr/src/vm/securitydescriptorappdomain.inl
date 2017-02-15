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
    m_dwDomainWideSpecialFlags(0xFFFFFFFF),
    m_fIsInitializationInProgress(TRUE),
    m_fIsDefaultAppdomain(FALSE),
    m_fIsDefaultAppdomainEvidence(FALSE),
    m_fHomogeneous(FALSE),
    m_fRuntimeSuppliedHomogenousGrantSet(FALSE),
    m_dwHostSecurityManagerFlags(HOST_NONE),
    m_fContainsAnyRefusedPermissions(FALSE),
    m_fIsPreResolved(FALSE),
    m_fPreResolutionFullTrust(FALSE),
    m_fPreResolutionHomogeneous(FALSE)
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


#endif // #ifndef DACCESS_COMPILE

#endif // !__SECURITYDESCRIPTORAPPDOMAIN_INL__

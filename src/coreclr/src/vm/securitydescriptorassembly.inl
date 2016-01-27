// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

//

#ifndef __SECURITYDESCRIPTOR_ASSEMBLY_INL__
#define __SECURITYDESCRIPTOR_ASSEMBLY_INL__

#ifndef DACCESS_COMPILE

inline BOOL AssemblySecurityDescriptor::AlreadyPassedDemand(PsetCacheEntry *pCasDemands)
{
    LIMITED_METHOD_CONTRACT;

    BOOL result = false;
    for (UINT index = 0; index < m_dwNumPassedDemands; index++)
    {
        if (m_arrPassedLinktimeDemands[index] == pCasDemands)
        {
            result = true;
            break;
        }
    }

    return result;
}

inline void AssemblySecurityDescriptor::TryCachePassedDemand(PsetCacheEntry *pCasDemands)
{
    LIMITED_METHOD_CONTRACT;
    
    if (m_dwNumPassedDemands <= (MAX_PASSED_DEMANDS - 1))
        m_arrPassedLinktimeDemands[m_dwNumPassedDemands++] = pCasDemands;
}

#ifdef FEATURE_CAS_POLICY

inline BOOL AssemblySecurityDescriptor::IsAssemblyRequestsComputed() 
{
    LIMITED_METHOD_CONTRACT;
    return m_fAssemblyRequestsComputed;
}

inline BOOL AssemblySecurityDescriptor::IsSignatureLoaded()
{
    LIMITED_METHOD_CONTRACT;
    return m_fIsSignatureLoaded;
}

inline void AssemblySecurityDescriptor::SetSignatureLoaded()
{
    LIMITED_METHOD_CONTRACT;
    m_fIsSignatureLoaded = TRUE;
}

#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_CORECLR

inline BOOL AssemblySecurityDescriptor::IsMicrosoftPlatform() 
{
    LIMITED_METHOD_CONTRACT;
    return m_fMicrosoftPlatform;
}

inline void AssemblySecurityDescriptor::SetMicrosoftPlatform()
{
    LIMITED_METHOD_CONTRACT;
    m_fMicrosoftPlatform = TRUE;
}

#endif // FEATURE_CORECLR

#ifdef FEATURE_APTCA

inline BOOL AssemblySecurityDescriptor::IsConditionalAptca()
{
    WRAPPER_NO_CONTRACT;
    ModuleSecurityDescriptor *pMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(m_pAssem->GetAssembly());
    return (pMSD->GetTokenFlags() & TokenSecurityDescriptorFlags_ConditionalAPTCA) == TokenSecurityDescriptorFlags_ConditionalAPTCA;
}

#endif // FEATURE_APTCA

#endif // !DACCESS_COMPILE

inline BOOL SharedSecurityDescriptor::IsMicrosoftPlatform() 
{
    LIMITED_METHOD_CONTRACT;
    return m_fMicrosoftPlatform;
}

inline AssemblyLoadSecurity::AssemblyLoadSecurity() :
    m_pEvidence(NULL),
    m_pAdditionalEvidence(NULL),
    m_pGrantSet(NULL),
    m_pRefusedSet(NULL),
    m_dwSpecialFlags(0),
    m_fCheckLoadFromRemoteSource(false),
    m_fSuppressSecurityChecks(false),
    m_fPropagatingAnonymouslyHostedDynamicMethodGrant(false)
{
    LIMITED_METHOD_CONTRACT;
    return;
}

// Should the assembly have policy resolved on it, or should it use a pre-determined grant set
inline bool AssemblyLoadSecurity::ShouldResolvePolicy()
{
    LIMITED_METHOD_CONTRACT;
    return m_pGrantSet == NULL;
}

#endif // #define __SECURITYDESCRIPTOR_ASSEMBLY_INL__

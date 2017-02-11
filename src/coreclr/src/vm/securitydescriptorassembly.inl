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



#endif // !DACCESS_COMPILE

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

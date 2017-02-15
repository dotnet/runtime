// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 


#include "common.h"

#include "appdomainstack.h"
#include "appdomainstack.inl"
#include "security.h"
#include "securitypolicy.h"
#include "appdomain.inl"
#include "callhelpers.h"

#ifdef _DEBUG
void AppDomainStack::CheckOverridesAssertCounts()
{
    LIMITED_METHOD_CONTRACT;
    DWORD   dwAppDomainIndex = 0;
    DWORD dwOverrides = 0;
    DWORD dwAsserts = 0;
    AppDomainStackEntry *pEntry = NULL;
    for(dwAppDomainIndex=0;dwAppDomainIndex<m_numEntries;dwAppDomainIndex++)
    {
        pEntry = __GetEntryPtr(dwAppDomainIndex);
        dwOverrides += pEntry->m_dwOverridesCount;
        dwAsserts += pEntry->m_dwAsserts;
    }
    _ASSERTE(dwOverrides == m_dwOverridesCount);
    _ASSERTE(dwAsserts == m_dwAsserts);    
}
#endif

BOOL AppDomainStackEntry::IsFullyTrustedWithNoStackModifiers(void)
{
    LIMITED_METHOD_CONTRACT;   
    if (m_domainID.m_dwId == INVALID_APPDOMAIN_ID || m_dwOverridesCount != 0 || m_dwAsserts != 0)
        return FALSE;

    AppDomainFromIDHolder pDomain(m_domainID, FALSE);
    if (pDomain.IsUnloaded())
        return FALSE;
    IApplicationSecurityDescriptor *currAppSecDesc = pDomain->GetSecurityDescriptor();
    if (currAppSecDesc == NULL)
        return FALSE;
    return Security::CheckDomainWideSpecialFlag(currAppSecDesc, 1 << SECURITY_FULL_TRUST);
}
BOOL AppDomainStackEntry::IsHomogeneousWithNoStackModifiers(void)
{
    LIMITED_METHOD_CONTRACT;
    if (m_domainID.m_dwId == INVALID_APPDOMAIN_ID || m_dwOverridesCount != 0 || m_dwAsserts != 0)
        return FALSE;

    AppDomainFromIDHolder pDomain(m_domainID, FALSE);
    if (pDomain.IsUnloaded())
        return FALSE;
    IApplicationSecurityDescriptor *currAppSecDesc = pDomain->GetSecurityDescriptor();
    if (currAppSecDesc == NULL)
        return FALSE;
    return (currAppSecDesc->IsHomogeneous() && !currAppSecDesc->ContainsAnyRefusedPermissions());
}

BOOL AppDomainStackEntry::HasFlagsOrFullyTrustedWithNoStackModifiers(DWORD flags)
{
    LIMITED_METHOD_CONTRACT;
    if (m_domainID.m_dwId == INVALID_APPDOMAIN_ID || m_dwOverridesCount != 0 || m_dwAsserts != 0)
        return FALSE;

    AppDomainFromIDHolder pDomain(m_domainID, FALSE);
    if (pDomain.IsUnloaded())
        return FALSE;
    IApplicationSecurityDescriptor *currAppSecDesc = pDomain->GetSecurityDescriptor();
    if (currAppSecDesc == NULL)
        return FALSE;
    
    // either the desired flag (often 0) or fully trusted will do
    flags |= (1<<SECURITY_FULL_TRUST);
    return Security::CheckDomainWideSpecialFlag(currAppSecDesc, flags);
}

BOOL AppDomainStack::AllDomainsHomogeneousWithNoStackModifiers()
{
    WRAPPER_NO_CONTRACT;

    // Used primarily by CompressedStack code to decide if a CS has to be constructed 

    DWORD   dwAppDomainIndex = 0;


    InitDomainIteration(&dwAppDomainIndex);
    while (dwAppDomainIndex != 0)
    {
        AppDomainStackEntry* pEntry = GetNextDomainEntryOnStack(&dwAppDomainIndex);
        _ASSERTE(pEntry != NULL);
        
        if (!pEntry->IsHomogeneousWithNoStackModifiers() && !pEntry->IsFullyTrustedWithNoStackModifiers())
            return FALSE;
    }

    return TRUE;
}


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
#ifdef FEATURE_REMOTING
#include "crossdomaincalls.h"
#else
#include "callhelpers.h"
#endif

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


void AppDomainStackEntry::UpdateHomogeneousPLS(OBJECTREF* homogeneousPLS)
{
    
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    AppDomainFromIDHolder domain(m_domainID, TRUE);
    if (domain.IsUnloaded())
        return;

    IApplicationSecurityDescriptor *thisAppSecDesc = domain->GetSecurityDescriptor();

    if (thisAppSecDesc->IsHomogeneous())
    {
        // update the intersection with the current grant set
        
        NewArrayHolder<BYTE> pbtmpSerializedObject(NULL);
        
        struct gc
        {
            OBJECTREF refGrantSet;
        } gc;
        ZeroMemory( &gc, sizeof( gc ) );
        AppDomain* pCurrentDomain;
        pCurrentDomain = GetAppDomain();
            
        GCPROTECT_BEGIN( gc );
#ifdef FEATURE_REMOTING // should not be possible without remoting
        DWORD cbtmpSerializedObject = 0;
        if (pCurrentDomain->GetId() != m_domainID)
        {
            // Unlikely scenario where we have another homogeneous AD on the callstack that's different from
            // the current one. If there's another AD on the callstack, it's likely to be FT.
            ENTER_DOMAIN_ID(m_domainID)
            {
                // Release the holder to allow GCs.  This is safe because we've entered the AD, so it won't go away.
                domain.Release();

                gc.refGrantSet = thisAppSecDesc->GetGrantedPermissionSet(NULL); 
                AppDomainHelper::MarshalObject(GetAppDomain(), &gc.refGrantSet, &pbtmpSerializedObject, &cbtmpSerializedObject);
                if (pbtmpSerializedObject == NULL)
            	{
                    // this is an error: possibly an OOM prevented the blob from getting created.
                    // We could return null and let the managed code use a fully restricted object or throw here.
                    // Let's throw here...
                    COMPlusThrow(kSecurityException);
                }
                gc.refGrantSet = NULL;

            }
            END_DOMAIN_TRANSITION
            AppDomainHelper::UnmarshalObject(pCurrentDomain,pbtmpSerializedObject, cbtmpSerializedObject, &gc.refGrantSet);
        }
        else
#else
        _ASSERTE(pCurrentDomain->GetId() == m_domainID);
#endif //!FEATURE_CORECLR 
        {
            // Release the holder to allow GCs.  This is safe because we're running in this AD, so it won't go away.
            domain.Release();
            gc.refGrantSet = thisAppSecDesc->GetGrantedPermissionSet(NULL); 
        }

        // At this point gc.refGrantSet has the grantSet of pDomain (thisAppSecDesc) in the current domain.
        // We don't care about refused perms since we established there were 
        // none earlier for this call stack.
        // Let's intersect with what we've already got.

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__PERMISSION_LIST_SET__UPDATE);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*homogeneousPLS);    // arg 0
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(gc.refGrantSet);       // arg 1 
        CALL_MANAGED_METHOD_NORET(args);

        GCPROTECT_END();
    }
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


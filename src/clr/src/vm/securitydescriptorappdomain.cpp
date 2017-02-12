// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

//


#include "common.h"
#include "security.h"
#ifdef FEATURE_REMOTING
#include "crossdomaincalls.h"
#else
#include "callhelpers.h"
#endif

#ifndef DACCESS_COMPILE

void ApplicationSecurityDescriptor::Resolve()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        SO_TOLERANT;
    } CONTRACTL_END;

    if (IsResolved())
        return;

    SetGrantedPermissionSet(NULL, NULL, 0xFFFFFFFF);
}

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------------------------------------
//
// Determine the security state of an AppDomain before the domain is fully configured.
// This method is used to detect the input configuration of a domain - specifically, if it
// is homogenous and fully trusted before domain setup is completed.
// 
// Note that this state may not reflect the final state of the AppDomain when it is
// configured, since components like the AppDomainManager can modify these bits during execution.
// 

void ApplicationSecurityDescriptor::PreResolve(BOOL *pfIsFullyTrusted, BOOL *pfIsHomogeneous)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pfIsFullyTrusted));
        PRECONDITION(CheckPointer(pfIsHomogeneous));
        PRECONDITION(IsInitializationInProgress()); // We shouldn't be looking at the pre-resolved state if we've already done real resolution
    }
    CONTRACTL_END;

    if (m_fIsPreResolved)
    {
        *pfIsFullyTrusted = m_fPreResolutionFullTrust;
        *pfIsHomogeneous = m_fPreResolutionHomogeneous;
        return;
    }

    GCX_COOP();

    // On CoreCLR all domains are partial trust homogenous
    m_fPreResolutionFullTrust = FALSE;
    m_fPreResolutionHomogeneous = TRUE;

    *pfIsFullyTrusted = m_fPreResolutionFullTrust;
    *pfIsHomogeneous = m_fPreResolutionHomogeneous;
    m_fIsPreResolved = TRUE;
}
#endif // CROSSGEN_COMPILE


//
// PLS (PermissionListSet) optimization Implementation
//   The idea of the PLS optimization is to maintain the intersection
//   of the grant sets of all assemblies loaded into the AppDomain (plus
//   the grant set of the AppDomain itself) and the union of all denied
//   sets. When a demand is evaluated, we first check the permission
//   that is being demanded against the combined grant and denied set
//   and if that check succeeds, then we know the demand is satisfied
//   in the AppDomain without having to perform an entire stack walk.
//

// Creates the PermissionListSet which holds the AppDomain level intersection of
// granted and denied permission sets of all assemblies in the domain and updates
// the granted and denied set with those of the AppDomain.
void ApplicationSecurityDescriptor::InitializePLS()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsResolved());
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifdef FEATURE_PLS
    struct _gc {
        OBJECTREF refGrantedSet;
        OBJECTREF refDeniedSet;
        OBJECTREF refPermListSet;
        OBJECTREF refRetVal;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    if (!IsFullyTrusted()) {
        GCPROTECT_BEGIN(gc);

        gc.refPermListSet = ObjectFromLazyHandle(m_hDomainPermissionListSet, m_pLoaderAllocator);
        gc.refGrantedSet = GetGrantedPermissionSet(&gc.refDeniedSet);

        MethodDescCallSite updateAppDomainPLS(METHOD__SECURITY_ENGINE__UPDATE_APPDOMAIN_PLS);
        ARG_SLOT args[] = {
            ObjToArgSlot(gc.refPermListSet),
            ObjToArgSlot(gc.refGrantedSet),
            ObjToArgSlot(gc.refDeniedSet),
        };
        gc.refRetVal = updateAppDomainPLS.Call_RetOBJECTREF(args);

        GCPROTECT_END();
    }

    StoreObjectInLazyHandle(m_hDomainPermissionListSet, gc.refRetVal, m_pLoaderAllocator);
#endif // FEATURE_PLS
    m_dwDomainWideSpecialFlags = m_dwSpecialFlags;
}

// Whenever a new assembly is added to the domain, we need to update the PermissionListSet
void ApplicationSecurityDescriptor::AddNewSecDescToPLS(AssemblySecurityDescriptor *pNewSecDescriptor)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pNewSecDescriptor->IsResolved());
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    //
    // If the assembly is fully trusted, this should be a no-op as the PLS is unaffected.
    // Note it's Ok to call this method before the AppDomain is fully initialized (and so
    // before the PLS is created for the AppDomain) because we enforce that all assemblies
    // loaded during that phase are fully trusted.
    //

    if (!pNewSecDescriptor->IsFullyTrusted()) {
#ifdef FEATURE_PLS
        struct _gc {
            OBJECTREF refGrantedSet;
            OBJECTREF refDeniedSet;
            OBJECTREF refPermListSet;
            OBJECTREF refRetVal;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        gc.refGrantedSet = pNewSecDescriptor->GetGrantedPermissionSet(&gc.refDeniedSet);
        if (gc.refDeniedSet != NULL)
            m_fContainsAnyRefusedPermissions = TRUE;

        // Ensure that the handle is created so that the compare exchange below will work correctly
        if (m_hDomainPermissionListSet == NULL)
        {
              LOADERHANDLE tmpHandle = m_pLoaderAllocator->AllocateHandle(NULL);

              // Races where this fails should be sufficiently rare that leaking this handle 
              // until LoaderAllocator destruction is acceptable.
              FastInterlockCompareExchangePointer(&m_hDomainPermissionListSet, tmpHandle, static_cast<LOADERHANDLE>(NULL));
        }

        // we have to synchronize the update to the PLS across concurring threads.
        // we don't care if another thread is checking the existing PLS while this
        // update is in progress as the loaded assembly won't be on the stack for such
        // a demand and so will not affect the result of the existing PLS optimization.
        do {
            gc.refPermListSet = ObjectFromLazyHandle(m_hDomainPermissionListSet, m_pLoaderAllocator);

            MethodDescCallSite updateAppDomainPLS(METHOD__SECURITY_ENGINE__UPDATE_APPDOMAIN_PLS);
            ARG_SLOT args[] = {
                ObjToArgSlot(gc.refPermListSet),
                ObjToArgSlot(gc.refGrantedSet),
                ObjToArgSlot(gc.refDeniedSet),
            };
            // This returns a new copy of the PermissionListSet
            gc.refRetVal = updateAppDomainPLS.Call_RetOBJECTREF(args);
        }
        // If some other thread beat us to the PLS object handle, just try updating the PLS again
        // This race should be rare enough that recomputing the PLS is acceptable.
        while (m_pLoaderAllocator->CompareExchangeValueInHandle(m_hDomainPermissionListSet, gc.refRetVal, gc.refPermListSet) != gc.refPermListSet);

        GCPROTECT_END();
#endif // FEATURE_PLS

        LONG dwNewDomainWideSpecialFlags = 0;
        LONG dwOldDomainWideSpecialFlags = 0;
        do {
            dwOldDomainWideSpecialFlags = m_dwDomainWideSpecialFlags;
            dwNewDomainWideSpecialFlags = (dwOldDomainWideSpecialFlags & pNewSecDescriptor->GetSpecialFlags());
        }
        while (InterlockedCompareExchange((LONG*)&m_dwDomainWideSpecialFlags, dwNewDomainWideSpecialFlags, dwOldDomainWideSpecialFlags) != dwOldDomainWideSpecialFlags);
    }
}

#ifdef FEATURE_PLS
BOOL ApplicationSecurityDescriptor::CheckPLS (OBJECTREF* orDemand, DWORD dwDemandSpecialFlags, BOOL fDemandSet)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    // Check if all assemblies so far have full trust.
    if (CheckDomainWideSpecialFlag(1 << SECURITY_FULL_TRUST))
        return TRUE;

    // Check if this is a one of the well-known permissions tracked in the special flags.
    if (CheckDomainWideSpecialFlag(dwDemandSpecialFlags))
        return TRUE;

    CLR_BOOL fResult = FALSE;
    //
    // only evaluate the PLS if we don't need to marshal the demand across AppDomains
    // This means we would perform a stack walk when there are multiple semi-trust
    // AppDomains on the stack, which is acceptable.
    // In homogeneous cases, the stack walk could potentially detect the situation
    // and avoid the expensive walk of the assemblies if the permission demand is a 
    // subset of the homogeneous grant set applied to the AppDomain.
    //
    if (m_pAppDomain == GetThread()->GetDomain()) {
        OBJECTREF refDomainPLS = NULL;

        GCPROTECT_BEGIN(refDomainPLS);
        refDomainPLS = ObjectFromLazyHandle(m_hDomainPermissionListSet, m_pLoaderAllocator);

        EX_TRY
        {
            if (fDemandSet) {
                PREPARE_NONVIRTUAL_CALLSITE(METHOD__PERMISSION_LIST_SET__CHECK_SET_DEMAND_NO_THROW);
                DECLARE_ARGHOLDER_ARRAY(args, 2);
                args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(refDomainPLS);    // arg 0
                args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(*orDemand);       // arg 1 
                CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
                CALL_MANAGED_METHOD(fResult, CLR_BOOL, args);
            }
            else {
                PREPARE_NONVIRTUAL_CALLSITE(METHOD__PERMISSION_LIST_SET__CHECK_DEMAND_NO_THROW);
                DECLARE_ARGHOLDER_ARRAY(args, 2);
                args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(refDomainPLS);    // arg 0
                args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(*orDemand);       // arg 1 
                CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
                CALL_MANAGED_METHOD(fResult, CLR_BOOL, args);
            }
        }
        EX_SWALLOW_NONTRANSIENT;

        GCPROTECT_END();
    }

    return fResult;
}
#endif // FEATURE_PLS

DWORD ApplicationSecurityDescriptor::GetDomainWideSpecialFlag() const
{
    LIMITED_METHOD_CONTRACT;
    return m_dwDomainWideSpecialFlags;
}

void ApplicationSecurityDescriptor::FinishInitialization()
{
    WRAPPER_NO_CONTRACT;
    // Resolve the AppDomain security descriptor.
    this->Resolve();

    // Reset the initialization in-progress flag.
    this->ResetInitializationInProgress();

    // Initialize the PLS with the grant set of the AppDomain
    this->InitializePLS();
}

void ApplicationSecurityDescriptor::SetHostSecurityManagerFlags(DWORD dwFlags)
{
    LIMITED_METHOD_CONTRACT;
    m_dwHostSecurityManagerFlags |= dwFlags;
}

void ApplicationSecurityDescriptor::SetPolicyLevelFlag()
{
    LIMITED_METHOD_CONTRACT;
    m_dwHostSecurityManagerFlags |= HOST_POLICY_LEVEL;
}

BOOL ApplicationSecurityDescriptor::IsHomogeneous() const
{
    LIMITED_METHOD_CONTRACT;
    return m_fHomogeneous;
}

// Should the HSM be consulted for security decisions in this AppDomain.
BOOL ApplicationSecurityDescriptor::CallHostSecurityManager()
{
    LIMITED_METHOD_CONTRACT;
    return (m_dwHostSecurityManagerFlags & HOST_APP_DOMAIN_EVIDENCE ||
            m_dwHostSecurityManagerFlags & HOST_POLICY_LEVEL ||
            m_dwHostSecurityManagerFlags & HOST_ASM_EVIDENCE ||
            m_dwHostSecurityManagerFlags & HOST_RESOLVE_POLICY);
}

// The AppDomain is considered a default one (FT) if the property is set and it's not a homogeneous AppDomain
BOOL ApplicationSecurityDescriptor::IsDefaultAppDomain() const
{
    LIMITED_METHOD_CONTRACT;
    return  m_fIsDefaultAppdomain
            ;
}

BOOL ApplicationSecurityDescriptor::IsDefaultAppDomainEvidence()
{
    LIMITED_METHOD_CONTRACT;
    return m_fIsDefaultAppdomainEvidence;// This need not be a default AD, but has no evidence. So we'll use the default AD evidence
}

// Indicates whether the initialization phase is in progress.
BOOL ApplicationSecurityDescriptor::IsInitializationInProgress()
{
    LIMITED_METHOD_CONTRACT;
    return m_fIsInitializationInProgress;
}

BOOL ApplicationSecurityDescriptor::ContainsAnyRefusedPermissions()
{
    LIMITED_METHOD_CONTRACT;
    return m_fContainsAnyRefusedPermissions;
}

// Is it possible for the AppDomain to contain partial trust code.  This method may return true even if the
// domain does not currently have partial trust code in it - a true value simply means that it is possible
// for partial trust code to eventually end up in the domain.
BOOL ApplicationSecurityDescriptor::DomainMayContainPartialTrustCode()
{
    WRAPPER_NO_CONTRACT;
    return !m_fHomogeneous || !IsFullyTrusted();
}


#endif // !DACCESS_COMPILE



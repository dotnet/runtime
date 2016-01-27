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

#ifndef FEATURE_CORECLR
BOOL ApplicationSecurityDescriptor::QuickIsFullyTrusted()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifdef CROSSGEN_COMPILE
    return TRUE;
#else
    if (IsDefaultAppDomain())
    {
        return TRUE;
    }

    // NGEN is always done in full trust
    if (m_pAppDomain->IsCompilationDomain())
    {
        return TRUE;
    }

    // Check if we need to call the HostSecurityManager.
    if (CallHostSecurityManager())
    {
        return FALSE;
    }

    APPDOMAINREF adRef = static_cast<APPDOMAINREF>(m_pAppDomain->GetExposedObject());

    // - If this AppDomain is a standard domain (full trust homogeneous), we are full trust
    // - If this is a homogeneous case, get the PermissionSet from managed code
    // - If CAS policy is not enabled, then we are fully trusted
    // - Otherwise, check the quick cache
    if (adRef->GetIsFastFullTrustDomain())
    {
        return TRUE;
    }
    else if (IsHomogeneous())
    {
        // A homogenous domain will be fully trusted if its grant set is full trust
        APPLICATIONTRUSTREF appTrustRef = static_cast<APPLICATIONTRUSTREF>(adRef->GetApplicationTrust());
        POLICYSTATEMENTREF psRef = static_cast<POLICYSTATEMENTREF>(appTrustRef->GetPolicyStatement());
        PERMISSIONSETREF grantSetRef = psRef->GetPermissionSet();
        return grantSetRef->IsUnrestricted();
    }
    else if (!IsLegacyCasPolicyEnabled())
    {
        return TRUE;
    }
    else
    {
        return CheckQuickCache(SecurityConfig::FullTrustAll, GetZone());
    }
#endif // CROSSGEN_COMPILE
}
#endif // FEATURE_CORECLR

#ifdef FEATURE_CAS_POLICY
OBJECTREF ApplicationSecurityDescriptor::GetEvidence()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pAppDomain == GetAppDomain());
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    OBJECTREF retval = ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);
    return retval;
}
#endif // FEATURE_CAS_POLICY

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

#ifndef CROSSGEN_COMPILE
    ResolveWorker();
#else
    SetGrantedPermissionSet(NULL, NULL, 0xFFFFFFFF);
#endif
}

#ifndef CROSSGEN_COMPILE
void ApplicationSecurityDescriptor::ResolveWorker()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    struct _gc
    {
        OBJECTREF evidence;         // Object containing evidence
        OBJECTREF granted;          // Policy based Granted Permission
        OBJECTREF grantdenied;      // Policy based explicitly Denied Permissions
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    DWORD dwSpecialFlags;

    // On debug builds do a pre-resolution so that we can validate pre-resolve and post-resolve match up
    // assuming no host interference.
#ifdef _DEBUG
    // We shouldn't be causing the first pre-resolution if we needed to cache this state for the host
    _ASSERTE(m_fIsPreResolved || 
             (GetAppDomain()->GetAppDomainManagerInitializeNewDomainFlags() & eInitializeNewDomainFlags_NoSecurityChanges) == eInitializeNewDomainFlags_None);

    BOOL fPreResolveFullTrust = FALSE;
    BOOL fPreResolveHomogeneous = FALSE;
    PreResolve(&fPreResolveFullTrust, &fPreResolveHomogeneous);
#endif // _DEBUG

#ifdef FEATURE_CORECLR
    // coreclr has 2 kinds of AppDomains - sandboxed or not. If sandboxed, then the homogeneous flag is set. If not, then it is a full-trust appdomain.
    if (!IsHomogeneous())
    {
        Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
        dwSpecialFlags = 0xFFFFFFFF;
    }
    else
    {
        APPDOMAINREF adRef = (APPDOMAINREF)m_pAppDomain->GetExposedObject();
        if (adRef->GetIsFastFullTrustDomain())
        {
            Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
            dwSpecialFlags = 0xFFFFFFFF;
        }
        else
        {
            APPLICATIONTRUSTREF appTrustRef = (APPLICATIONTRUSTREF)adRef->GetApplicationTrust();
            POLICYSTATEMENTREF psRef = appTrustRef->GetPolicyStatement();
            gc.granted = (OBJECTREF)psRef->GetPermissionSet();

            // We can trust the grant set special flags, since only mscorlib can access the root
            // ApplicationTrust reference.
            dwSpecialFlags = appTrustRef->GetGrantSetSpecialFlags();
        }
    }

#else
    if (QuickIsFullyTrusted())
    {
        Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
        dwSpecialFlags = 0xFFFFFFFF;
    }
    // We need to check the homogeneous flag directly rather than going through the accessor method, since
    // that method also considers the presence of a HostSecurityManager.  The HostSecurityManager should not
    // affect the domain's grant set at this point however, as it does not have any domain policy resolution
    // callbacks and if it wanted customize the homogenous domain grant set it needed to do that when we called 
    // its InitializeNewDomain.  Longer term IsHomogenous should not consider the HostSecurityManager at all.
    else if (m_fHomogeneous)
    {
        // Homogeneous AppDomain case 
        
        APPDOMAINREF adRef = (APPDOMAINREF)m_pAppDomain->GetExposedObject();
        _ASSERTE( adRef != NULL);

        if (adRef->GetIsFastFullTrustDomain())
        {
            Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
            dwSpecialFlags = 0xFFFFFFFF;
        }
        else
        {
            APPLICATIONTRUSTREF appTrustRef = (APPLICATIONTRUSTREF)adRef->GetApplicationTrust();
            _ASSERTE(appTrustRef != NULL);
            POLICYSTATEMENTREF psRef = appTrustRef->GetPolicyStatement();
            _ASSERTE(psRef != NULL);
            gc.granted = (OBJECTREF)psRef->GetPermissionSet();

            // We can trust the grant set special flags, since only mscorlib can access the root
            // ApplicationTrust reference.
            dwSpecialFlags = appTrustRef->GetGrantSetSpecialFlags();
        }
    }
    else
    {
        // Regular AppDomain policy resolution based on AppDomain evidence
        if (IsEvidenceComputed())
        {
            gc.evidence = ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);
        }
        else
        {
            gc.evidence = GetEvidence();
        }

        if (!IsLegacyCasPolicyEnabled())
        {
            // Either we have a host security manager or a homogenous AppDomain that could make this domain be
            // partially trusted.  Call out to managed to get the grant set.
            gc.granted = SecurityPolicy::ResolveGrantSet(gc.evidence, &dwSpecialFlags, FALSE);
        }
        else
        {
            // Legacy CAS policy is enabled, so do a full CAS resolve
            gc.granted = SecurityPolicy::ResolveCasPolicy(gc.evidence,
                                                          NULL,
                                                          NULL,
                                                          NULL,
                                                          &gc.grantdenied,
                                                          &dwSpecialFlags,
                                                          FALSE);
        }
    }
#endif    

    SetGrantedPermissionSet(gc.granted, NULL, dwSpecialFlags);

#ifdef FEATURE_CAS_POLICY
    // If the host promised not to modify the security of the AppDomain, throw an InvalidOperationException
    // if it did.  We specifically want to check the cached version of this state on the security
    // descriptor, rather than any version calculated earlier on in this method since the domain manager has
    // already run by the time ResolveWorker is entered.
    if (GetAppDomain()->GetAppDomainManagerInitializeNewDomainFlags() & eInitializeNewDomainFlags_NoSecurityChanges)
    {
        _ASSERTE(m_fIsPreResolved);

        if (!!m_fPreResolutionFullTrust != !!IsFullyTrusted())
        {
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_HostModifiedSecurityState"));
        }

        if (!!m_fPreResolutionHomogeneous != !!IsHomogeneous())
        {
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_HostModifiedSecurityState"));
        }
    }
#endif // FEATURE_CAS_POLICY

#if defined(_DEBUG) && !defined(FEATURE_CORECLR)
    // Make sure that that our PreResolve routine is consistent with our actual resolution results.  This is
    // only required to be true in the absence of an AppDomainManager.
    // 
    // If any assert fires in this block, it means that PreResolve isn't correctly figuring out what the
    // incoming security state of an AppDomain is going to resolve into.
    if (!GetAppDomain()->HasAppDomainManagerInfo())
    {
#ifdef FEATURE_CLICKONCE
        if (GetAppDomain()->IsClickOnceAppDomain())
        {
            _ASSERTE(!!IsHomogeneous() == !!fPreResolveHomogeneous);
            // We don't check grant set since we don't attempt to pre-resolve that - pre-resolution should
            // have always come back partial trust
            _ASSERTE(!fPreResolveFullTrust);
        }
        else
#endif // FEATURE_CLICKONCE
        if (IsHomogeneous())
        {
            _ASSERTE(!!IsHomogeneous() == !!fPreResolveHomogeneous);
            _ASSERTE(!!IsFullyTrusted() == !!fPreResolveFullTrust);
        }
        else
        {
            _ASSERTE(!!IsHomogeneous() == !!fPreResolveHomogeneous);
            // We don't check grant sets on heterogeneous domains since they are never attempted to be pre-resolved.
        }
    }
#endif // _DEBUG && !FEATURE_CORECLR

    GCPROTECT_END();
}

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

#ifdef FEATURE_CORECLR
    // On CoreCLR all domains are partial trust homogenous
    m_fPreResolutionFullTrust = FALSE;
    m_fPreResolutionHomogeneous = TRUE;

#else // !FEATURE_CORECLR
    if (GetAppDomain()->IsClickOnceAppDomain())
    {
        // In the ClickOnce case we can't pre-resolve the grant set because it's entirely in the control of
        // the ApplicationSecurityManager.  We conservatively assume that it will be partial trust; however
        // we always know that the domain will be homogenous
        m_fPreResolutionFullTrust = FALSE;
        m_fPreResolutionHomogeneous = TRUE;
    }
    else if (GetAppDomain()->IsCompilationDomain())
    {
        // NGEN is always full trust and homogenous
        m_fPreResolutionFullTrust = TRUE;
        m_fPreResolutionHomogeneous = TRUE;
    }
    else if (GetAppDomain()->IsDefaultDomain())
    {
        // Barring any shenanigans from the AppDomainManager, we know that the default domain will be fully
        // trusted and homogenous in the standard case, but heterogenous in the legacy CAS policy case.
        m_fPreResolutionFullTrust = TRUE;
        m_fPreResolutionHomogeneous = !Security::IsProcessWideLegacyCasPolicyEnabled();
    }
    else
    {
        // In all other AppDomains we need to consult the incoming AppDomainSetup in order to figure out if
        // the domain is being setup as full or partial trust.
        CLR_BOOL fPreResolutionFullTrust = FALSE;
        CLR_BOOL fPreResolutionHomogeneous = FALSE;

        MethodDescCallSite preResolve(METHOD__SECURITY_ENGINE__PRE_RESOLVE);

        ARG_SLOT args[] =
        {
            PtrToArgSlot(&fPreResolutionFullTrust),
            PtrToArgSlot(&fPreResolutionHomogeneous)
        };

        preResolve.Call(args);

        m_fPreResolutionFullTrust = !!fPreResolutionFullTrust;
        m_fPreResolutionHomogeneous = !!fPreResolutionHomogeneous;
    }
#endif // FEATURE_CORECLR

    *pfIsFullyTrusted = m_fPreResolutionFullTrust;
    *pfIsHomogeneous = m_fPreResolutionHomogeneous;
    m_fIsPreResolved = TRUE;
}
#endif // CROSSGEN_COMPILE

#ifdef FEATURE_CAS_POLICY
//---------------------------------------------------------------------------------------
//
// Determine if an AppDomain should allow an assembly to be LoadFrom-ed a remote location.
// Since pre-v4 versions of the CLR would implicitly sandbox this load, we only want to
// allow this if the application has either acknowledged it to be safe, or if the application
// has taken control of sandboxing itself.
// 
// This method returns true if the load should be allowed, false if it should be blocked
// from a remote location.
// 

BOOL ApplicationSecurityDescriptor::AllowsLoadsFromRemoteSources()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If the application has explicitly enabled remote LoadFroms then we should allow the load
    if (Security::CanLoadFromRemoteSources())
    {
        return true;
    }

    // Otherwise, we only allow the load if the assembly is going to be sandboxed (or explicitly not sandboxed
    // by a host).  That can happen if we've got legacy CAS polcy enabled, if we're in a homogenous AppDomain,
    // or if there is a HostSecurityManager that cares about assembly policy.
    // 
    // Note that we don't allow LoadFrom a remote source in a domain that had its ApplicationTrust supplied by
    // the CLR, since that domain would have implicitly sandboxed the LoadFrom in CLR v2. Instead, these
    // domains require that there be a HostSecurityManager present which setup the sandbox.

    if (IsHomogeneous() && !m_fRuntimeSuppliedHomogenousGrantSet)
    {
        return true;
    }

    if (IsLegacyCasPolicyEnabled())
    {
        return true;
    }

    return false;
}

DWORD ApplicationSecurityDescriptor::GetZone()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsLegacyCasPolicyEnabled());
    }
    CONTRACTL_END;

    SecZone dwZone = NoZone; 
    if (m_pAppDomain->GetRootAssembly() != NULL && m_pAppDomain->IsDefaultDomain())
    {
        LPCWSTR wszAsmPath = m_pAppDomain->GetRootAssembly()->GetManifestFile()->GetPath();

        if (wszAsmPath)
        {
            StackSString ssPath( W("file://") );
            ssPath.Append( wszAsmPath );

            dwZone = SecurityPolicy::MapUrlToZone(ssPath.GetUnicode());
        }
    }

    return dwZone;
}
#endif // FEATURE_CAS_POLICY


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
#ifndef FEATURE_CORECLR
            && !m_fHomogeneous
#endif // FEATURE_CORECLR
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

#ifdef FEATURE_CAS_POLICY
void ApplicationSecurityDescriptor::SetLegacyCasPolicyEnabled()
{
    STANDARD_VM_CONTRACT;

    // APPX precludes the use of legacy CAS policy
    if (!AppX::IsAppXProcess())
    {
        SecurityPolicy::InitPolicyConfig();
        m_fLegacyCasPolicy = TRUE;
    }
}

BOOL ApplicationSecurityDescriptor::IsLegacyCasPolicyEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return m_fLegacyCasPolicy && !AppX::IsAppXProcess();
}

#endif // FEATURE_CAS_POLICY

// Is it possible for the AppDomain to contain partial trust code.  This method may return true even if the
// domain does not currently have partial trust code in it - a true value simply means that it is possible
// for partial trust code to eventually end up in the domain.
BOOL ApplicationSecurityDescriptor::DomainMayContainPartialTrustCode()
{
    WRAPPER_NO_CONTRACT;
    return !m_fHomogeneous || !IsFullyTrusted();
}

#ifdef FEATURE_APTCA
ConditionalAptcaCache *ApplicationSecurityDescriptor::GetConditionalAptcaCache()
{
    LIMITED_METHOD_CONTRACT;
    return m_pConditionalAptcaCache;
}

void ApplicationSecurityDescriptor::SetCanonicalConditionalAptcaList(LPCWSTR wszCanonicalConditionalAptcaList)
{
    WRAPPER_NO_CONTRACT;
    return this->GetConditionalAptcaCache()->SetCanonicalConditionalAptcaList(wszCanonicalConditionalAptcaList);
}
#endif // FEATURE_APTCA

#endif // !DACCESS_COMPILE



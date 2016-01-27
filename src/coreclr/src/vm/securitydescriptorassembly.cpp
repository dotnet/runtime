// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"
#include "security.h"

#ifndef DACCESS_COMPILE
AssemblySecurityDescriptor::AssemblySecurityDescriptor(AppDomain *pDomain, DomainAssembly *pAssembly, LoaderAllocator *pLoaderAllocator) :
    SecurityDescriptorBase<IAssemblySecurityDescriptor>(pDomain, pAssembly, pAssembly->GetFile(), pLoaderAllocator),
    m_dwNumPassedDemands(0),
    m_pSignature(NULL),
    m_pSharedSecDesc(NULL),
#ifdef FEATURE_CAS_POLICY
    m_hRequiredPermissionSet(NULL),
    m_hOptionalPermissionSet(NULL),
    m_hDeniedPermissionSet(NULL),
    m_fAdditionalEvidence(FALSE),
    m_fIsSignatureLoaded(FALSE),
    m_fAssemblyRequestsComputed(FALSE),
#endif
    m_fMicrosoftPlatform(FALSE),
    m_fAllowSkipVerificationInFullTrust(TRUE)
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    } CONTRACTL_END;
}

#ifdef FEATURE_CAS_POLICY
OBJECTREF AssemblySecurityDescriptor::GetRequestedPermissionSet(OBJECTREF *pOptionalPermissionSet,
                                                                OBJECTREF *pDeniedPermissionSet)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_pAppDomain == GetAppDomain());
        PRECONDITION(CheckPointer(pOptionalPermissionSet));
        PRECONDITION(CheckPointer(pDeniedPermissionSet));
        PRECONDITION(m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    OBJECTREF req = NULL;
    GCPROTECT_BEGIN(req);

    if (!IsAssemblyRequestsComputed())
    {
        ReleaseHolder<IMDInternalImport> pImport (m_pAssem->GetFile()->GetMDImportWithRef());

        // Try to load permission requests from assembly first.
        SecurityAttributes::LoadPermissionRequestsFromAssembly(pImport,
                                                               &req,
                                                               pOptionalPermissionSet,
                                                               pDeniedPermissionSet);

        SetRequestedPermissionSet(req, *pOptionalPermissionSet, *pDeniedPermissionSet);
    }
    else
    {
        *pOptionalPermissionSet = ObjectFromLazyHandle(m_hOptionalPermissionSet, m_pLoaderAllocator);
        *pDeniedPermissionSet = ObjectFromLazyHandle(m_hDeniedPermissionSet, m_pLoaderAllocator);
        req = ObjectFromLazyHandle(m_hRequiredPermissionSet, m_pLoaderAllocator);
    }

    GCPROTECT_END();
    return req;
}

void AssemblySecurityDescriptor::SetRequestedPermissionSet(OBJECTREF RequiredPermissionSet,
                                                                  OBJECTREF OptionalPermissionSet,
                                                                  OBJECTREF DeniedPermissionSet)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct _gc
    {
        OBJECTREF RequiredPermissionSet;
        OBJECTREF OptionalPermissionSet;
        OBJECTREF DeniedPermissionSet;
    } gc;

    gc.RequiredPermissionSet = RequiredPermissionSet;
    gc.OptionalPermissionSet = OptionalPermissionSet;
    gc.DeniedPermissionSet = DeniedPermissionSet;

    GCPROTECT_BEGIN(gc);
    StoreObjectInLazyHandle(m_hRequiredPermissionSet, gc.RequiredPermissionSet, m_pLoaderAllocator);
    StoreObjectInLazyHandle(m_hOptionalPermissionSet, gc.OptionalPermissionSet, m_pLoaderAllocator);
    StoreObjectInLazyHandle(m_hDeniedPermissionSet, gc.DeniedPermissionSet, m_pLoaderAllocator);
    GCPROTECT_END();

    m_fAssemblyRequestsComputed = TRUE;
}
#endif // FEATURE_CAS_POLICY

//
// This method will return TRUE if this assembly is allowed to skip verification.
//

BOOL AssemblySecurityDescriptor::CanSkipVerification()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(IsResolved());
    }
    CONTRACTL_END;


    // Assemblies loaded into the verification domain never get to skip verification
    // unless they are coming from the GAC.
    if (m_pAppDomain->IsVerificationDomain()) 
    {
        if (!m_pAssem->GetFile()->IsSourceGAC() && m_pAssem->IsIntrospectionOnly())
        {
            return FALSE;
        }
    }

    return CheckSpecialFlag(1 << SECURITY_SKIP_VER);
}

BOOL AssemblySecurityDescriptor::AllowSkipVerificationInFullTrust()
{
    LIMITED_METHOD_CONTRACT;
    return m_fAllowSkipVerificationInFullTrust;
}

//
// This method will return TRUE if this assembly has assertion permission.
//

BOOL AssemblySecurityDescriptor::CanAssert()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved());
    } CONTRACTL_END;

    return CheckSpecialFlag(1 << SECURITY_ASSERT);
}

//
// This method will return TRUE if this assembly has unrestricted UI permissions.
//

BOOL AssemblySecurityDescriptor::HasUnrestrictedUIPermission()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved());
    } CONTRACTL_END;

    return CheckSpecialFlag(1 << UI_PERMISSION);
}

//
// Assembly transparency access methods.  These methods what the default transparency level are for methods
// and types introduced by the assembly.
//

BOOL AssemblySecurityDescriptor::IsAllCritical()
{
    STANDARD_VM_CONTRACT;

    ModuleSecurityDescriptor *pMsd = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(GetAssembly());
    return pMsd->IsAllCritical();
}

BOOL AssemblySecurityDescriptor::IsAllSafeCritical()
{
    STANDARD_VM_CONTRACT;

    ModuleSecurityDescriptor *pMsd = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(GetAssembly());
    return pMsd->IsAllCritical() && pMsd->IsTreatAsSafe();
}

BOOL AssemblySecurityDescriptor::IsAllPublicAreaSafeCritical()
{
    STANDARD_VM_CONTRACT;

    ModuleSecurityDescriptor *pMsd = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(GetAssembly());

    bool fIsPublicAreaSafeCritical = SecurityTransparencyBehavior::GetTransparencyBehavior(pMsd->GetSecurityRuleSet())->DoesPublicImplyTreatAsSafe();

    return pMsd->IsAllCritical() && (pMsd->IsTreatAsSafe() || fIsPublicAreaSafeCritical);
}

BOOL AssemblySecurityDescriptor::IsAllTransparent()
{
    STANDARD_VM_CONTRACT;

    ModuleSecurityDescriptor *pMsd = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(GetAssembly());
    return pMsd->IsAllTransparent();
}

BOOL AssemblySecurityDescriptor::QuickIsFullyTrusted()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    if (IsSystem())
        return TRUE;
#ifdef FEATURE_CAS_POLICY

    // NGEN is always done in full trust
    if (m_pAppDomain->IsCompilationDomain())
    {
        return TRUE;
    }

    // If the assembly is in the GAC then it gets FullTrust.
    if (m_pAssem->GetFile()->IsSourceGAC())
        return TRUE;

    // quickly detect if we've got a request refused or a request optional.
    if (m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled())
    {
        ReleaseHolder<IMDInternalImport> pImport(m_pAssem->GetFile()->GetMDImportWithRef());
        if (SecurityAttributes::RestrictiveRequestsInAssembly(pImport))
            return FALSE;
    }

    // Check if we need to call the HostSecurityManager.
    ApplicationSecurityDescriptor* pAppSecDesc = static_cast<ApplicationSecurityDescriptor*>(m_pAppDomain->GetSecurityDescriptor());
    if (pAppSecDesc->CallHostSecurityManagerForAssemblies())
        return FALSE;

    // - If the AppDomain is homogeneous, we currently simply detect the FT case
    // - Not having CAS on implies full trust.  We can get here if we're still in the process of setting up
    //   the AppDomain and the CLR hasn't yet setup the homogenous flag.
    // - Otherwise, check the quick cache
    if (pAppSecDesc->IsHomogeneous())
    {
        return m_pAppDomain->GetSecurityDescriptor()->IsFullyTrusted();
    }
    else if (!m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled())
    {
        return TRUE;
    }
    else if (CheckQuickCache(SecurityConfig::FullTrustAll, GetZone()))
    {
        return TRUE;
    }
#endif

    // See if we've already determined that the assembly is FT
    // in another AppDomain, in case this is a shared assembly.
    SharedSecurityDescriptor* pSharedSecDesc = GetSharedSecDesc();
    if (pSharedSecDesc && pSharedSecDesc->IsResolved() && pSharedSecDesc->IsFullyTrusted())
        return TRUE;

    return FALSE;
}

#ifndef DACCESS_COMPILE

void AssemblySecurityDescriptor::PropagatePermissionSet(OBJECTREF GrantedPermissionSet, OBJECTREF DeniedPermissionSet, DWORD dwSpecialFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we're propagating a permission set, then we don't want to allow an assembly to skip verificaiton in
    // full trust.  This prevents people leapfrogging from the fully trusted anonymously hosted dynamic methods
    // assembly into running unverifiable code.  (Note that we already enforce that transaprent code must only load
    // other transparent code - so this restriction simply enforces that it is truly transparent.)    It would
    // be nicer to throw an exception in this case, however that would be a breaking change.  Instead, since the
    // SkipVerificationInFullTrust feature has always been described as a performance optimization and nothing more,
    // we can simply turn off the optimization in these cases.
    m_fAllowSkipVerificationInFullTrust = FALSE;

    SetGrantedPermissionSet(GrantedPermissionSet, DeniedPermissionSet, dwSpecialFlags);

    // make sure the shared security descriptor is updated in case this 
    // is a security descriptor for a shared assembly.
    Resolve();
}

#ifdef FEATURE_CAS_POLICY
//-----------------------------------------------------------------------------------------------------------
//
// Use the evidence already generated for this assembly's PEFile as the evidence for the assembly
//
// Arguments:
//    pPEFileSecDesc - PEFile security descriptor contining the already generated evidence
//
void AssemblySecurityDescriptor::SetEvidenceFromPEFile(IPEFileSecurityDescriptor *pPEFileSecDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pPEFileSecDesc));
        PRECONDITION(GetPEFile()->Equals(static_cast<PEFileSecurityDescriptor*>(pPEFileSecDesc)->GetPEFile()));
    }
    CONTRACTL_END;

    // If we couldn't determine the assembly was fully trusted without first generating evidence for it,
    // then we cannot reuse the PEFile's evidence.  In that case we'll just use what we've generated for the
    // assembly, and discard the PEFile's version.
    if (!IsEvidenceComputed())
    {
        struct
        {
            OBJECTREF objPEFileEvidence;
            OBJECTREF objEvidence;
        }
        gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        gc.objPEFileEvidence = pPEFileSecDesc->GetEvidence();
        gc.objEvidence = UpgradePEFileEvidenceToAssemblyEvidence(gc.objPEFileEvidence);
        SetEvidence(gc.objEvidence);

        GCPROTECT_END();
    }
}

//---------------------------------------------------------------------------------------
//
// Get the evidence collection for this Assembly
//
//
OBJECTREF AssemblySecurityDescriptor::GetEvidence()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_pAppDomain == GetAppDomain());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // If we already have evidence, then just return that
    if (IsEvidenceComputed())
        return ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);

    struct
    {
        OBJECTREF objHostProvidedEvidence;
        OBJECTREF objPEFileEvidence;
        OBJECTREF objEvidence;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    BEGIN_SO_INTOLERANT_CODE(GetThread());

    gc.objHostProvidedEvidence = ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
    {
        _ASSERTE(gc.objPEFileEvidence == NULL || GetAppDomain() == gc.objPEFileEvidence->GetAppDomain());
        _ASSERTE(gc.objHostProvidedEvidence == NULL || GetAppDomain() == gc.objHostProvidedEvidence->GetAppDomain());
    }
#endif  // CHECK_APP_DOMAIN_LEAKS

    //
    // First get an evidence collection which targets our PEFile, then upgrade it to use this assembly as a
    // target.  We create a new Evidence for the PEFile here, which means that any evidence that PEFile may
    // have already had is not used in this upgrade.  If an existing PEFileSecurityDescriptor exists for the
    // PEFile, then that should be upgraded directly, rather than going through this code path.
    // 
    
    gc.objPEFileEvidence = PEFileSecurityDescriptor::BuildEvidence(m_pPEFile, gc.objHostProvidedEvidence);
    gc.objEvidence = UpgradePEFileEvidenceToAssemblyEvidence(gc.objPEFileEvidence);
    SetEvidence(gc.objEvidence);

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
        _ASSERTE(gc.objEvidence == NULL || GetAppDomain() == gc.objEvidence->GetAppDomain());
#endif // CHECK_APP_DOMAIN_LEAKS

    END_SO_INTOLERANT_CODE;

    GCPROTECT_END();

    return gc.objEvidence;
}
#endif // FEATURE_CAS_POLICY
#endif // !DACCESS_COMPILE

BOOL AssemblySecurityDescriptor::IsSystem()
{
    WRAPPER_NO_CONTRACT;
    return m_pAssem->GetFile()->IsSystem();
}

void AssemblySecurityDescriptor::Resolve()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(m_pAssem != NULL);
        INJECT_FAULT(COMPlusThrowOM(););
        SO_TOLERANT;
    } CONTRACTL_END;

    // Always resolve the assembly security descriptor in the new AppDomain
    if (!IsResolved())
        ResolveWorker();

    // Update the info in the shared security descriptor
    SharedSecurityDescriptor* pSharedSecDesc = GetSharedSecDesc();
    if (pSharedSecDesc)
        pSharedSecDesc->Resolve(this);
}

#ifdef FEATURE_CAS_POLICY
// This routine is called when we have determined that it that there is no SECURITY reason
// to verify an image, but we may want to do so anyway to insure that 3rd parties don't 
// accidentally ship delay signed dlls because the application happens to be full trust.  
//
static bool DontNeedToFlagAccidentalDelaySigning(PEAssembly* assem)
{
    WRAPPER_NO_CONTRACT;

    // If the file has a native image, then either it is strongly named and can be considered
    // fully signed (see additional comments in code:PEAssembly::IsFullySigned), or it is not
    // strong named and thus can't be delay signed. Either way no check is needed.
    // If the file fully signed, then people did not accidentally forget, so no check is needed
    if (assem->HasNativeImage() || assem->IsFullySigned())
        return true;

    // If mscorlib itself is not signed, this is not an offical CLR, you don't need to 
    // to do the checking in this case either because 3rd parties should not be running this way.
    // This is useful because otherwise when we run perf runs on normal CLR lab builds we don't
    // measure the performance that we get for a offical runtime (since official runtimes will
    // be signed).   
    PEAssembly* mscorlib = SystemDomain::SystemFile();
    if (!mscorlib->HasNativeImage())
        return false;
    if ((mscorlib->GetLoadedNative()->GetNativeHeader()->COR20Flags & COMIMAGE_FLAGS_STRONGNAMESIGNED) == 0)
        return true;

    return false;
}
#endif // FEATURE_CAS_POLICY

void AssemblySecurityDescriptor::ResolveWorker()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifdef FEATURE_CORECLR

    if (NingenEnabled())
    {
        int dwSpecialFlags;
        BOOL platformAssembly = FALSE;

        if (IsSystem())
        {
            dwSpecialFlags = 0xFFFFFFFF;
            platformAssembly = TRUE;
        }
        else
        {
            // Decide if this is a platform assembly
            if (m_pAssem->GetFile()->IsProfileAssembly())
                platformAssembly = TRUE;

            // Decide trust level
            if (platformAssembly)
            {
                dwSpecialFlags = 0xFFFFFFFF;
            }
            else
            {
                dwSpecialFlags = m_pAppDomain->GetSecurityDescriptor()->GetSpecialFlags();
            }
        }

        SetGrantedPermissionSet(NULL, NULL, dwSpecialFlags);
        if (platformAssembly)
            SetMicrosoftPlatform();

        return;
    }

#ifndef CROSSGEN_COMPILE
    int dwSpecialFlags;
    BOOL platformAssembly = FALSE;
    BOOL trustedAssembly = FALSE;
    
    struct _gc {
        OBJECTREF granted;          // Policy based Granted Permission
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    if (IsSystem())
    {
        // mscorlib is always FT, but we can't create permissionsets yet. So grantset for mscorlib will be NULL: we should never look at it though.
        dwSpecialFlags = 0xFFFFFFFF;
        platformAssembly = TRUE;
    }
    else
    {
        // Decide if this is a platform assembly
        if (m_pAssem->GetFile()->IsProfileAssembly())
            platformAssembly = TRUE;

        // Decide trust level
        if (platformAssembly || trustedAssembly)
        {
            Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
            dwSpecialFlags = 0xFFFFFFFF;
        }
        else
        {
            // get grant from AppDomain grant set.
            gc.granted = m_pAppDomain->GetSecurityDescriptor()->GetGrantedPermissionSet(NULL);
            dwSpecialFlags = m_pAppDomain->GetSecurityDescriptor()->GetSpecialFlags();
        }
        
    }
    SetGrantedPermissionSet(gc.granted, NULL, dwSpecialFlags);
    if (platformAssembly)
        SetMicrosoftPlatform();
    
    // Only fully trusted assemblies are allowed to be loaded when 
    // the AppDomain is in the initialization phase.
    if (m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress() && !IsFullyTrusted())
        COMPlusThrow(kApplicationException, W("Policy_CannotLoadSemiTrustAssembliesDuringInit"));

    GCPROTECT_END();
#endif // CROSSGEN_COMPILE

#else
    if (CanSkipPolicyResolution() || NingenEnabled()) {
        SetGrantedPermissionSet(NULL, NULL, 0xFFFFFFFF);
        m_pAssem->GetFile()->SetStrongNameBypassed();
        return;
    }

#ifndef CROSSGEN_COMPILE
    struct _gc {
        OBJECTREF reqdPset;         // Required Requested Permissions
        OBJECTREF optPset;          // Optional Requested Permissions
        OBJECTREF denyPset;         // Denied Permissions
        OBJECTREF evidence;         // Object containing evidence
        OBJECTREF granted;          // Policy based Granted Permission
        OBJECTREF grantdenied;      // Policy based explicitly Denied Permissions
    } gc;
    ZeroMemory(&gc, sizeof(gc));
   
    // Policy resolution can cause string comparisons that trigger .nlp module loads. (Specifically,
    // FileIOPermission can trigger this).  At this point mscorlib is already loaded, so we can
    // override the load levels here to allow the .nlp module loads.   
    OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    // Resolve is one of the few SecurityDescriptor routines that may be called
    // from the wrong appdomain context. If that's the case we will transition
    // into the correct appdomain for the duration of the call.
    
    ENTER_DOMAIN_PTR_PREDICATED(m_pAppDomain,ADV_RUNNINGIN,!IsSystem())
    {
        GCPROTECT_BEGIN(gc);

        //
        // GAC assemblies with no RequestRefuse get FullTrust
        // Also AppDomains with an AppTrust that are fully trusted are
        // homogeneous and so every assembly that does not have a RequestRefuse
        // will also get FullTrust.
        //

        DWORD dwSpecialFlags;

        if (QuickIsFullyTrusted())
        {
            Security::GetPermissionInstance(&gc.granted, SECURITY_FULL_TRUST);
            dwSpecialFlags = 0xFFFFFFFF;
        }
        else
        {
            // We couldn't quickly figure out that the assembly was fully trusted, so gather its evidence and
            // call managed code to get the final grant set.
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
                // Either we have a host security manager or a homogenous AppDomain that could make this
                // assembly be partially trusted.  Call out to managed to get the grant set.
                gc.granted = SecurityPolicy::ResolveGrantSet(gc.evidence, &dwSpecialFlags, TRUE);
            }
            else
            {
                // Legacy CAS policy is enabled, so do a full CAS resolve
                gc.reqdPset = GetRequestedPermissionSet(&gc.optPset, &gc.denyPset);
                gc.granted = SecurityPolicy::ResolveCasPolicy(gc.evidence,
                                                              gc.reqdPset,
                                                              gc.optPset,
                                                              gc.denyPset,
                                                              &gc.grantdenied,
                                                              &dwSpecialFlags,
                                                              TRUE);
            }
        }

        SetGrantedPermissionSet(gc.granted, gc.denyPset, dwSpecialFlags);

#ifdef FEATURE_CAS_POLICY
        // If we're delaying verification of the assembly's strong name, check to see if we need to force a
        // verification at this point.
        if (!m_pAssem->IsSystem() && 
            !m_pAssem->GetFile()->IsSourceGAC() &&
            !m_pAssem->GetFile()->IsStrongNameVerified())
        {
            //
            // #StrongNameBypass
            //
            // If the application has opted into the strong name bypass feature, then we will attempt to
            // load its assemblies without verifying their strong names.  We can get away with avoiding the
            // strong name verification in the case where all of the following apply.
            // 
            //   1. The application has asked for strong name bypass
            //   2. The machine administrator has not disabled strong name bypass for the machine
            //   3. The assembly being loaded is fully trusted, and this trust is not soley based upon its
            //      strong name.
            //   4. The AppDomain the assembly is being loaded into is fully trusted
            //   5. The assembly is fully signed
            //   6. The appdomain is not attempting to run ngen.
            //   
            // Condition #3 requires that the grant set of the assembly not be obtained via the strong name
            // evidence of the assembly.  Note that this requirement does not forbid matching a code group
            // with a strong name membership condition, as long as that code group and any children code
            // groups produce a grant set which was less than or equal to the grant set produced by other
            // code groups.  For instance, in standard security policy:
            // 
            //   1.1 MyComputer -> FullTrust
            //      1.1.1 Microsoft Strong Name -> FullTrust
            //      
            // In this case, an assembly loaded from the local machine with the Microsoft strong name is
            // still eligable for strong name bypass via condition #3, since the MyComputer FullTrust grant
            // set unioned with the StrongName FullTrust grant set will produce the same results as if we
            // didn't evaluate the StrongName code group.
            // 
            // Note that strong name bypass is not the same thing as strong name skip verification.  Skip
            // verification is a development time feature which enables developers to test privately built
            // assemblies that do not have a full signature yet.  It is not intended for use at runtime on
            // non-developer machines, nor is it intended for use on assemblies with valid strong names.
            // 
            // In contrast strong name bypass is intended to be used on assemblies with valid strong names
            // that are deployed to end users machines.  It's a performance feature which enables assemblies
            // that were not gaining any benefit from having their strong name validated to avoid having to
            // pay the expense of a full signature verification. This is why ngen does not skip. There
            // are potential ways to use the ngen cache to skip strong name verification if ngen participates
            // in strong name bypass, and the startup performance concern of the application is not a concern
            // when running at ngen time.
            //

            if (IsFullyTrusted() && 
                GetDomain()->GetSecurityDescriptor()->IsFullyTrusted() &&
                !SecurityPolicy::WasStrongNameEvidenceUsed(gc.evidence) &&
                DontNeedToFlagAccidentalDelaySigning(m_pAssem->GetFile()) &&
                !IsCompilationProcess())
            {
                m_pAssem->GetFile()->SetStrongNameBypassed();
            }
            else
            {
                m_pAssem->GetFile()->VerifyStrongName();
            }
        }
#endif // FEATURE_CAS_POLICY

        GCPROTECT_END();
    }
    END_DOMAIN_TRANSITION;
#endif // CROSSGEN_COMPILE

#endif // FEATURE_CORECLR
}

void AssemblySecurityDescriptor::ResolvePolicy(ISharedSecurityDescriptor *pSharedSecDesc, BOOL fShouldSkipPolicyResolution)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pSharedSecDesc));
    } CONTRACTL_END;

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    m_pSharedSecDesc = static_cast<SharedSecurityDescriptor*>(pSharedSecDesc);

    ETWOnStartup (SecurityCatchCall_V1, SecurityCatchCallEnd_V1);
    //
    // In V1.x, we used to check whether execution checking is enabled in caspol.exe
    // or whether the assembly has assembly requests before resolving the assembly.
    // This leads to several unnecessary complications in the code and the way assembly
    // resolution is tracked throughout the lifetime of the AssemblySecurityDescriptor.
    //
    // In Whidbey, we will always resolve the policy eagerly while the assembly is being
    // loaded. The perf concern is less of an issue in Whidbey as GAC assemblies are now
    // automatically granted FullTrust.
    //

    // Push this frame around resolving the assembly for security to ensure the
    // debugger can properly recognize any managed code that gets run
    // as "class initializaion" code.
    FrameWithCookie<DebuggerClassInitMarkFrame> __dcimf;

    Resolve();

    if (!fShouldSkipPolicyResolution)
    {
        // update the PLS with the grant/denied sets of the loaded assembly
        ApplicationSecurityDescriptor* pAppDomainSecDesc = static_cast<ApplicationSecurityDescriptor*>(GetDomain()->GetSecurityDescriptor());
        pAppDomainSecDesc->AddNewSecDescToPLS(this);

        // Make sure that module transparency information is calculated so that we can verify that if the assembly
        // is being loaded in partial trust it is transparent.  This check is done in the ModuleSecurityDescriptor,
        // so we just need to force it to calculate here.
        ModuleSecurityDescriptor *pMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(GetAssembly());
        pMSD->VerifyDataComputed();
        _ASSERTE(IsFullyTrusted() || pMSD->IsAllTransparent());
    }

    __dcimf.Pop();
}

#ifdef FEATURE_CAS_POLICY
DWORD AssemblySecurityDescriptor::GetZone()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(m_pAppDomain->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());
    } CONTRACTL_END;

    StackSString    codebase;
    SecZone         dwZone = NoZone;
    BYTE            rbUniqueID[MAX_SIZE_SECURITY_ID];
    DWORD           cbUniqueID = sizeof(rbUniqueID);

    m_pAssem->GetSecurityIdentity(codebase, &dwZone, 0, rbUniqueID, &cbUniqueID);
    return dwZone;
}
#endif // FEATURE_CAS_POLICY

Assembly* AssemblySecurityDescriptor::GetAssembly()
{
    return m_pAssem->GetAssembly();
}

BOOL AssemblySecurityDescriptor::CanSkipPolicyResolution()
{
    WRAPPER_NO_CONTRACT;
    Assembly* pAssembly = GetAssembly();
    return pAssembly && pAssembly->CanSkipPolicyResolution();
}


#ifdef FEATURE_CAS_POLICY
//-----------------------------------------------------------------------------------------------------------
//
// Upgrade the evidence used for resolving a PEFile to be targeted at the Assembly the PEFile represents
//
// Arguments:
//    objPEFileEvidence - 
//    
// Notes:
//    During CLR startup we may need to resolve policy against a PEFile before we have the associated
//    Assembly.  Once we have the Assembly we don't want to recompute potenially expensive evidence, so this
//    method can be used to upgrade the evidence who's target was the PEFile to target the assembly instead.
//    
//    Will call into System.Reflection.Assembly.UpgradeSecurityIdentity
//

OBJECTREF AssemblySecurityDescriptor::UpgradePEFileEvidenceToAssemblyEvidence(const OBJECTREF& objPEFileEvidence)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(objPEFileEvidence != NULL);
    }
    CONTRACTL_END;

    struct
    {
        OBJECTREF objAssembly;
        OBJECTREF objEvidence;
        OBJECTREF objUpgradedEvidence;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    gc.objAssembly = m_pAssem->GetExposedAssemblyObject();
    gc.objEvidence = objPEFileEvidence;

    MethodDescCallSite upgradeSecurityIdentity(METHOD__ASSEMBLY_EVIDENCE_FACTORY__UPGRADE_SECURITY_IDENTITY);

    ARG_SLOT args[] = 
    {
        ObjToArgSlot(gc.objEvidence),
        ObjToArgSlot(gc.objAssembly)
    };

    gc.objUpgradedEvidence = upgradeSecurityIdentity.Call_RetOBJECTREF(args);

    GCPROTECT_END();

    return gc.objUpgradedEvidence;
}

HRESULT AssemblySecurityDescriptor::LoadSignature(COR_TRUST **ppSignature)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    if (IsSignatureLoaded())
    {
        if (ppSignature)
        {
            *ppSignature = m_pSignature;
        }

        return S_OK;
    }

    GCX_PREEMP();
    m_pSignature = m_pAssem->GetFile()->GetAuthenticodeSignature();

    SetSignatureLoaded();

    if (ppSignature)
    {
        *ppSignature = m_pSignature;
    }

    return S_OK;
}

void AssemblySecurityDescriptor::SetAdditionalEvidence(OBJECTREF evidence)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    StoreObjectInLazyHandle(m_hAdditionalEvidence, evidence, m_pLoaderAllocator);
    m_fAdditionalEvidence = TRUE;
}

BOOL AssemblySecurityDescriptor::HasAdditionalEvidence()
{
    LIMITED_METHOD_CONTRACT;
    return m_fAdditionalEvidence;
}

OBJECTREF AssemblySecurityDescriptor::GetAdditionalEvidence()
{
    WRAPPER_NO_CONTRACT;
    return ObjectFromLazyHandle(m_hAdditionalEvidence, m_pLoaderAllocator);
}
#endif // FEATURE_CAS_POLICY

#ifndef FEATURE_CORECLR 
BOOL AssemblySecurityDescriptor::AllowApplicationSpecifiedAppDomainManager()
{
    WRAPPER_NO_CONTRACT;

    // Only fully trusted assemblies are allowed to specify their AppDomainManager in a config file
    return this->IsFullyTrusted();
}
#endif // FEATURE_CORECLR

// Check to make sure that security will allow this assembly to load.  Throw an exception if the assembly
// should be forbidden from loading for security related purposes
void AssemblySecurityDescriptor::CheckAllowAssemblyLoad()
{
    STANDARD_VM_CONTRACT;
    
    if (m_pAssem->IsSystem())
    {
        return;
    }

    // If we're running PEVerify, then we need to allow the assembly to load in to be verified
    if (m_pAppDomain->IsVerificationDomain())
    {
        return;
    }

    // Similarly, in the NGEN domain we don't want to force policy resolution, and we want
    // to allow all assemblies to load
    if (m_pAppDomain->IsCompilationDomain())
    {
        return;
    }

    // Reflection only loads are also always allowed
    if (m_pAssem->IsIntrospectionOnly())
    {
        return;
    }

    if (!IsResolved())
    {
        GCX_COOP();
        Resolve();
    }

    if (!IsFullyTrusted() && (!m_pAppDomain->IsCompilationDomain() || !NingenEnabled()))
    {
        // Only fully trusted assemblies are allowed to be loaded when 
        // the AppDomain is in the initialization phase.
        if (m_pAppDomain->GetSecurityDescriptor()->IsInitializationInProgress())
        {
            COMPlusThrow(kApplicationException, W("Policy_CannotLoadSemiTrustAssembliesDuringInit"));
        }

#ifdef FEATURE_COMINTEROP
        // WinRT is not supported in partial trust, so block it by throwing if a partially trusted winmd is loaded
        if (IsAfContentType_WindowsRuntime(m_pAssem->GetFile()->GetFlags()))
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_WinRT_PartialTrust"));
        }
#endif // FEATURE_COMINTEROP
    }
}

SharedSecurityDescriptor::SharedSecurityDescriptor(Assembly *pAssembly) :
    m_pAssembly(pAssembly),
    m_fResolved(FALSE),
    m_fFullyTrusted(FALSE),
    m_fCanCallUnmanagedCode(FALSE),
    m_fCanAssert(FALSE),
    m_fMicrosoftPlatform(FALSE)
{
    LIMITED_METHOD_CONTRACT;
}

void SharedSecurityDescriptor::Resolve(IAssemblySecurityDescriptor *pSecDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pSecDesc->IsResolved());
    }
    CONTRACTL_END;

    if (!m_fResolved)
    {
        m_fFullyTrusted = pSecDesc->IsFullyTrusted();
        m_fCanCallUnmanagedCode = pSecDesc->CanCallUnmanagedCode();
        m_fCanAssert = pSecDesc->CanAssert();
#ifdef FEATURE_CORECLR
        m_fMicrosoftPlatform = static_cast<AssemblySecurityDescriptor*>(pSecDesc)->IsMicrosoftPlatform();
#endif // FEATURE_CORECLR

        m_fResolved = TRUE;
    }

    _ASSERTE(!!m_fFullyTrusted == !!pSecDesc->IsFullyTrusted());
    _ASSERTE(!!m_fCanCallUnmanagedCode == !!pSecDesc->CanCallUnmanagedCode());
    _ASSERTE(!!m_fCanAssert == !!pSecDesc->CanAssert());
}

BOOL SharedSecurityDescriptor::IsFullyTrusted()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved());
    } CONTRACTL_END;

    return m_fFullyTrusted;
}

BOOL SharedSecurityDescriptor::CanCallUnmanagedCode() const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved());
    } CONTRACTL_END;

    return m_fCanCallUnmanagedCode;
}

BOOL SharedSecurityDescriptor::IsResolved() const
{
    LIMITED_METHOD_CONTRACT;
    return m_fResolved;
}

BOOL SharedSecurityDescriptor::CanAssert()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsResolved());
    } CONTRACTL_END;

    return m_fCanAssert;
}

BOOL SharedSecurityDescriptor::IsSystem()
{ 
    WRAPPER_NO_CONTRACT;
    return m_pAssembly->IsSystem();
}

Assembly* SharedSecurityDescriptor::GetAssembly()
{ 
    LIMITED_METHOD_CONTRACT;
    return m_pAssembly;
}

SharedSecurityDescriptor *AssemblySecurityDescriptor::GetSharedSecDesc()
{ 
    LIMITED_METHOD_CONTRACT;
    return m_pSharedSecDesc; 
}
#endif // #ifndef DACCESS_COMPILE



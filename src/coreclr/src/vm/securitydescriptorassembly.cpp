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


void AssemblySecurityDescriptor::ResolveWorker()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    SetGrantedPermissionSet(NULL, NULL, 0xFFFFFFFF);
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



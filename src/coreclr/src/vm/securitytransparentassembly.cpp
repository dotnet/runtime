// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// securityTransparentAssembly.cpp
//
// Implementation for transparent code feature
//
//--------------------------------------------------------------------------


#include "common.h"
#include "field.h"
#include "securitydeclarative.h"
#include "security.h"
#include "customattribute.h"
#include "securitytransparentassembly.h"
#include "securitymeta.h"
#include "typestring.h"
#include "comdelegate.h"

#if defined(FEATURE_PREJIT)
#include "compile.h"
#endif

#ifdef _DEBUG
//
// In debug builds of the CLR, we support a mode where transparency errors are not enforced with exceptions; instead
// they are written to the CLR debug log.  This allows us to migrate tests from the v2 to the v4 transparency model by
// allowing test runs to continue to the end of the run, and keeping a log file of which assemblies need migration.
//

// static
void SecurityTransparent::LogTransparencyError(Assembly *pAssembly, const LPCSTR szError)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(szError));
        PRECONDITION(g_pConfig->LogTransparencyErrors());
    }
    CONTRACTL_END;

    const SString &strAssemblyName = pAssembly->GetManifestModule()->GetPath();

    LOG((LF_SECURITY,
         LL_INFO1000,
         "Security Transparency Violation: Assembly '%S': %s\n",
         strAssemblyName.GetUnicode(),
         szError));
}

// static
void SecurityTransparent::LogTransparencyError(MethodTable *pMT, const LPCSTR szError)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(szError));
        PRECONDITION(g_pConfig->LogTransparencyErrors());
    }
    CONTRACTL_END;

    Assembly *pAssembly = pMT->GetAssembly();
    const SString &strAssemblyName = pAssembly->GetManifestModule()->GetPath();

    LOG((LF_SECURITY,
         LL_INFO1000,
         "Security Transparency Violation: Assembly '%S' - Type '%s': %s\n",
         strAssemblyName.GetUnicode(),
         pMT->GetDebugClassName(),
         szError));
}

// static
void SecurityTransparent::LogTransparencyError(MethodDesc *pMD, const LPCSTR szError, MethodDesc *pTargetMD /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(szError));
        PRECONDITION(g_pConfig->LogTransparencyErrors());
    }
    CONTRACTL_END;

    Assembly *pAssembly = pMD->GetAssembly();
    const SString &strAssemblyName = pAssembly->GetManifestModule()->GetPath();

    if (pTargetMD == NULL)
    {
        LOG((LF_SECURITY,
             LL_INFO1000,
             "Security Transparency Violation: Assembly '%S' - Method '%s::%s': %s\n",
             strAssemblyName.GetUnicode(),
             pMD->m_pszDebugClassName,
             pMD->m_pszDebugMethodName,
             szError));
    }
    else
    {
        Assembly *pTargetAssembly = pTargetMD->GetAssembly();
        const SString &strTargetAssemblyName = pTargetAssembly->GetManifestModule()->GetPath();

        LOG((LF_SECURITY,
             LL_INFO1000,
             "Security Transparency Violation: Assembly '%S' - Method '%s::%s' - Target Assembly '%S': %s\n",
             strAssemblyName.GetUnicode(),
             pMD->m_pszDebugClassName,
             pMD->m_pszDebugMethodName,
             strTargetAssemblyName.GetUnicode(),
             szError));
    }
}

#endif // _DEBUG

// There are a few places we throw transparency method access exceptions that aren't "real"
// method access exceptions - such as unverifiable code in a transparent assembly, and having a critical
// attribute on a transparent method.  Those continue to use the one-MethodDesc form of throwing -
// everything else should use the standard ::ThrowMethodAccessException call

// static
void DECLSPEC_NORETURN SecurityTransparent::ThrowMethodAccessException(MethodDesc* pMD,
                                                                       DWORD dwMessageId /* = IDS_CRITICAL_METHOD_ACCESS_DENIED */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    // throw method access exception
    StackSString strMethod;
    TypeString::AppendMethod(strMethod, pMD, pMD->GetClassInstantiation(), TypeString::FormatNamespace | TypeString::FormatAngleBrackets| TypeString::FormatSignature);
    COMPlusThrowHR(COR_E_METHODACCESS, dwMessageId, strMethod.GetUnicode());
}

// static
void DECLSPEC_NORETURN SecurityTransparent::ThrowTypeLoadException(MethodDesc* pMethod, DWORD dwMessageID /* = IDS_METHOD_INHERITANCE_RULES_VIOLATED */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pMethod));
    }
    CONTRACTL_END;

    // Throw an exception here
    StackSString strMethod;
    StackScratchBuffer buffer;            
    TypeString::AppendMethod(strMethod, pMethod, pMethod->GetClassInstantiation(), TypeString::FormatNamespace | TypeString::FormatAngleBrackets | TypeString::FormatSignature);
    pMethod->GetAssembly()->ThrowTypeLoadException(strMethod.GetUTF8(buffer), dwMessageID);
}

// static
void DECLSPEC_NORETURN SecurityTransparent::ThrowTypeLoadException(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Throw an exception here
    StackScratchBuffer buffer;
    SString strType;
    TypeString::AppendType(strType, TypeHandle(pMT), TypeString::FormatNamespace | TypeString::FormatAngleBrackets );
    pMT->GetAssembly()->ThrowTypeLoadException(strType.GetUTF8(buffer), IDS_TYPE_INHERITANCE_RULES_VIOLATED);
}

static BOOL IsTransparentCallerAllowed(MethodDesc *pCallerMD, MethodDesc *pCalleeMD, SecurityTransparencyError *pError)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pCallerMD));
        PRECONDITION(CheckPointer(pCalleeMD));
        PRECONDITION(CheckPointer(pError, NULL_OK));
        PRECONDITION(pCallerMD->IsTransparent());
    }
    CONTRACTL_END;

    // If the target is critical, and not treat as safe, then we cannot allow the call
    if (Security::IsMethodCritical(pCalleeMD) && !Security::IsMethodSafeCritical(pCalleeMD))
    {
        if (pError != NULL)
        {
            *pError = SecurityTransparencyError_CallCriticalMethod;
        }

        return FALSE;
    }

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Convert the critical member to a LinkDemand for FullTrust, and convert that LinkDemand to a
// full demand.  If the current call stack allows this conversion to succeed, this method returns. Otherwise
// a security exception is thrown.
//
// Arguments:
//   pCallerMD - The method calling the critical method
//

static void ConvertCriticalMethodToLinkDemand(MethodDesc *pCallerMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCallerMD));
        PRECONDITION(pCallerMD->IsTransparent());
        PRECONDITION(pCallerMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallLinkDemandMethods());
    }
    CONTRACTL_END;

#if !defined(CROSSGEN_COMPILE) && defined(FEATURE_CAS_POLICY)
    if (NingenEnabled())
        return;

    GCX_COOP();

    OBJECTREF permSet = NULL;
    GCPROTECT_BEGIN(permSet);

    Security::GetPermissionInstance(&permSet, SECURITY_FULL_TRUST);
    Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, permSet);

    GCPROTECT_END();
#endif // !CROSSGEN_COMPILE && FEATURE_CAS_POLICY
}

// static
BOOL SecurityTransparent::CheckCriticalAccess(AccessCheckContext* pContext,
                                              MethodDesc* pOptionalTargetMethod,
                                              FieldDesc* pOptionalTargetField,
                                              MethodTable * pOptionalTargetType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
    }
    CONTRACTL_END;

    // At most one of these should be non-NULL
    _ASSERTE(1 >= ((pOptionalTargetMethod ? 1 : 0) +
                   (pOptionalTargetField ? 1 : 0) +
                   (pOptionalTargetType ? 1 : 0)));

    // okay caller is transparent, additional checks needed
    BOOL fIsTargetCritical = FALSE; // check if target is critical
    BOOL fIsTargetSafe = FALSE; // check if target is marked safe
    Assembly *pTargetAssembly = NULL;

    if (pOptionalTargetMethod != NULL)
    {
        fIsTargetCritical = IsMethodCritical(pOptionalTargetMethod);
        fIsTargetSafe = IsMethodSafeCritical(pOptionalTargetMethod);
        pTargetAssembly = pOptionalTargetMethod->GetAssembly();
    }
    else if (pOptionalTargetField != NULL)
    {
        FieldSecurityDescriptor fieldSecurityDescriptor(pOptionalTargetField);
        fIsTargetCritical = fieldSecurityDescriptor.IsCritical();
        fIsTargetSafe = fieldSecurityDescriptor.IsTreatAsSafe();
        pTargetAssembly = pOptionalTargetField->GetModule()->GetAssembly();
    }
    else if (pOptionalTargetType != NULL)
    {
        fIsTargetCritical = IsTypeAllCritical(pOptionalTargetType); // check for only all critical classes
        fIsTargetSafe = IsTypeSafeCritical(pOptionalTargetType);
        pTargetAssembly = pOptionalTargetType->GetAssembly();
    }

    // If the target is transparent or safe critical, then no further checks are needed.  Otherwise, if a
    // legacy caller is targeting a new critical method, we may be able to allow the call by converting
    // the critical method to a LinkDemand for FullTrust and converting the LinkDemand to a full demand.
    //
    // This allows for the case where a v2 transparent assembly called a method that was proteced by a
    // LinkDemand in v2 and followed our suggested path of converting to being critical in v4.  By treating
    // the v4 critical method as if it were protected with a LinkDmeand instead, we're simply reversing this 
    // conversion to provide compatible behavior with legacy binaries
    if (!fIsTargetCritical || fIsTargetSafe)
    {
        return TRUE;
    }

    if (pContext->IsCalledFromInterop())
        return TRUE;

    MethodDesc* pCurrentMD = pContext->GetCallerMethod();
    MethodTable* pCurrentMT = pContext->GetCallerMT();

    // Not from interop but the caller is NULL, this can only happen
    // when we are checking from a Type/Assembly.
    if (pCurrentMD != NULL)
    {
        // TODO: need to probably CheckCastToClass as well..
        if (!IsMethodTransparent(pCurrentMD))
        {
            // Return TRUE if caller is NULL (interop caller) or critical.
            return TRUE;
        }

#ifdef FEATURE_CORECLR
        // On the coreCLR, a method can be transparent even if the containing type is marked Critical.
        // This will happen when that method is an override of a base transparent method, and the type that
        // contains the override is marked Critical. And that's the only case it can happen.
        // This particular case is not a failure. To state this another way, from a security transpararency perspective,
        // a method will always have access to the type that it is a member of.
        if (pOptionalTargetType == pCurrentMD->GetMethodTable())
        {
            return TRUE;
        }
#endif // FEATURE_CORECLR
    	
        // an attached profiler may wish to have these checks suppressed
        if (Security::BypassSecurityChecksForProfiler(pCurrentMD))
        {
            return TRUE;
        }

        if (pTargetAssembly != NULL &&
            pTargetAssembly->GetSecurityTransparencyBehavior()->CanCriticalMembersBeConvertedToLinkDemand() &&
            pCurrentMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallLinkDemandMethods())
        {
            // Convert the critical member to a LinkDemand for FullTrust, and convert that LinkDemand to a
            // full demand.  If the resulting full demand for FullTrust is successful, then we'll allow the access
            // to the critical method to succeed
            ConvertCriticalMethodToLinkDemand(pCurrentMD);
            return TRUE;
        }
    }
    else if (pCurrentMT != NULL)
    {
        if (!IsTypeTransparent(pCurrentMT))
        {
            return TRUE;
        }
    }

    return FALSE;
}

// Determine if a method is allowed to perform a CAS assert within the transparency rules.  Generally, only
// critical code may assert.  However, for compatibility with v2.0 we allow asserts from transparent code if
// the following criteria are met:
//   1. The assembly is a true v2.0 binary, and is not just using v2.0 transparency rules via the
//      SecurityRuleSet.Level1 annotation.
//   2. The assembly is agnostic to transparency (that is, if it were fully trusted it would be
//      opprotunistically critical).
//   3. We are currently in a heterogenous AppDomain.
//
// This compensates for the fact that while partial trust code could have asserted in v2.0, it can no longer
// assert in v4.0 as we force it to be transparent.  While the v2.0 transparency rules still don't allow
// asserting, assemblies that would have been critical in v2.0 are allowed to continue asserting in v4.0.

// static
BOOL SecurityTransparent::IsAllowedToAssert(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    // Critical code is always allowed to assert
    if (IsMethodCritical(pMD))
    {
        return TRUE;
    }
    
#ifdef FEATURE_CORECLR
    // On CoreCLR only critical code may ever assert - there are no compatibility reasons to allow
    // transparent asserts.
    return FALSE;
#else // !FEATURE_CORECLR
    // We must be in a heterogenous AppDomain for transparent asserts to work
    if (GetAppDomain()->GetSecurityDescriptor()->IsHomogeneous())
    {
        return FALSE;
    }

    ModuleSecurityDescriptor *pMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pMD->GetAssembly());
    
    // Only assemblies whose version requires them to use legacy transparency (rather than assemblies which
    // get legacy transparency via RuleSet.Level1) can assert from transparent code 
    if (!pMSD->AssemblyVersionRequiresLegacyTransparency())
    {
        return FALSE;
    }

    // Finally, the assembly must not have had any of the transparency attributes on it
    const TokenSecurityDescriptorFlags transparencyAwareFlags = 
        TokenSecurityDescriptorFlags_AllCritical    | // [SecurityCritical(SecurityCriticalScope.All)]
        TokenSecurityDescriptorFlags_Critical       | // [SecurityCritical]
        TokenSecurityDescriptorFlags_SafeCritical   | // [SecuritySafeCritical]
        TokenSecurityDescriptorFlags_Transparent    | // [SecurityTransparent]
        TokenSecurityDescriptorFlags_TreatAsSafe;     // [SecurityTreatAsSafe]
    TokenSecurityDescriptorFlags moduleAttributes = pMSD->GetTokenFlags();
    if ((moduleAttributes & transparencyAwareFlags) != TokenSecurityDescriptorFlags_None)
    {
        return FALSE;
    }

    return TRUE;
#endif // FEATURE_CORECLR
}

// Functor class to aid in determining if a type requires a transparency check
class TypeRequiresTransparencyCheckFunctor
{
private:
    bool m_requiresTransparencyCheck;
    bool m_checkForLinkDemands;

public:
    TypeRequiresTransparencyCheckFunctor(bool checkForLinkDemands) :
        m_requiresTransparencyCheck(false),
        m_checkForLinkDemands(checkForLinkDemands)
    {
        LIMITED_METHOD_CONTRACT;
    }

    TypeRequiresTransparencyCheckFunctor(const TypeRequiresTransparencyCheckFunctor &other); // not implemented

    bool RequiresTransparencyCheck() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_requiresTransparencyCheck;
    }

    void operator()(MethodTable *pMT)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        // We only need to do a check if so far none of the other component typpes required a transparency
        // check.  Critical, but not safe critical, types require transparency checks of their callers.
        if (!m_requiresTransparencyCheck)
        {
            m_requiresTransparencyCheck = Security::IsTypeCritical(pMT) && !Security::IsTypeSafeCritical(pMT) &&
                (!m_checkForLinkDemands || pMT->GetAssembly()->GetSecurityTransparencyBehavior()->CanCriticalMembersBeConvertedToLinkDemand());
        }
    }
};

// Determine if accessing a type requires doing a transparency check - this checks to see if the type
// itself, or any of its generic variables are security critical.

// static
bool SecurityTransparent::TypeRequiresTransparencyCheck(TypeHandle type, bool checkForLinkDemands)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    TypeRequiresTransparencyCheckFunctor typeChecker(checkForLinkDemands);
    type.ForEachComponentMethodTable(typeChecker);
    return typeChecker.RequiresTransparencyCheck();
}

CorInfoCanSkipVerificationResult SecurityTransparent::JITCanSkipVerification(MethodDesc * pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    /* XXX Fri 1/12/2007
     * This code is cloned from security.inl!Security::CanSkipVerification(MethodDesc, BOOL).
     */
    // Special case the System.Object..ctor:
    // System.Object..ctor is not verifiable according to current verifier rules (that require to call the
    // base class ctor). But since we want System.Object..ctor() to be marked transparent, it cannot be
    // unverifiable (telesto security rules prohibit transparent code from being unverifiable)

#ifndef DACCESS_COMPILE
    if (g_pObjectCtorMD == pMD)
        return CORINFO_VERIFICATION_CAN_SKIP;
#endif //!DACCESS_COMPILE

    // If a profiler is attached, we may want to bypass verification as well
    if (Security::BypassSecurityChecksForProfiler(pMD))
    {
        return CORINFO_VERIFICATION_CAN_SKIP;
    }

    BOOL hasSkipVerificationPermisson = false;
    DomainAssembly * pDomainAssembly = pMD->GetAssembly()->GetDomainAssembly();
    hasSkipVerificationPermisson = Security::CanSkipVerification(pDomainAssembly);

    CorInfoCanSkipVerificationResult canSkipVerif = hasSkipVerificationPermisson ? CORINFO_VERIFICATION_CAN_SKIP : CORINFO_VERIFICATION_CANNOT_SKIP;

#ifdef FEATURE_CORECLR
    //For Profile assemblies, do not verify any code.  All Transparent methods are guaranteed to be
    //verifiable (verified by tests).  Therefore, skip all verification on platform assemblies.

    //All profile assemblies have skip verification.
    _ASSERTE(!(pDomainAssembly->GetFile()->IsProfileAssembly() && !hasSkipVerificationPermisson));

#ifdef FEATURE_CORESYSTEM
    //
    // On Phone, at runtime, enable verification for user code that will run as transparent:
    // - All Mango applications
    // - Apollo applications with transparent code
    //
    if (hasSkipVerificationPermisson && !pDomainAssembly->GetFile()->IsProfileAssembly())
    {
        if (SecurityTransparent::IsMethodTransparent(pMD))
        {
            canSkipVerif = CORINFO_VERIFICATION_CANNOT_SKIP;
        }
    }
#endif // FEATURE_CORESYSTEM

#else //FEATURE_CORECLR
    // also check to see if the method is marked transparent
    if (hasSkipVerificationPermisson)
    { 
        if (pDomainAssembly == GetAppDomain()->GetAnonymouslyHostedDynamicMethodsAssembly())
        {
            // This assembly is FullTrust. However, it cannot contain unverifiable code.
            // The JIT compiler is not hardened to deal with invalid code. Hence, we cannot
            // return CORINFO_VERIFICATION_RUNTIME_CHECK for IL that could have been generated
            // by a low-trust assembly.
            canSkipVerif = CORINFO_VERIFICATION_CANNOT_SKIP;
        }
        // also check to see if the method is marked transparent
        else if (SecurityTransparent::IsMethodTransparent(pMD))
        {
            // If the assembly requested that even its transparent members not be verified, then we can skip
            // verification.  Otherwise, we need to either inject a runtime demand in the v2 model, or fail
            // verification in the v4 model.
            ModuleSecurityDescriptor *pModuleSecDesc = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pMD->GetAssembly());
            if (pModuleSecDesc->CanTransparentCodeSkipVerification())
            {
                canSkipVerif = CORINFO_VERIFICATION_CAN_SKIP;
            }
            else if (pMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeSkipVerification())
            {
                canSkipVerif = CORINFO_VERIFICATION_RUNTIME_CHECK;
            }
            else
            {
                canSkipVerif = CORINFO_VERIFICATION_CANNOT_SKIP;
            }
        }
    }
#endif //FEATURE_CORECLR

    return canSkipVerif;
}

CorInfoCanSkipVerificationResult SecurityTransparent::JITCanSkipVerification(DomainAssembly * pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    BOOL hasSkipVerificationPermisson = Security::CanSkipVerification(pAssembly);

    CorInfoCanSkipVerificationResult canSkipVerif = hasSkipVerificationPermisson ? CORINFO_VERIFICATION_CAN_SKIP : CORINFO_VERIFICATION_CANNOT_SKIP;

    // If the assembly has permission to skip verification, but its transparency model requires that
    // transparency can only be skipped with a runtime demand, then we need to make sure that there is a
    // runtime check done.
    if (hasSkipVerificationPermisson)
    {
      // In CoreCLR, do not enable transparency checks here.  We depend on this method being "honest" in
      // JITCanSkipVerification to skip transparency checks on profile assemblies.
#ifndef FEATURE_CORECLR
        ModuleSecurityDescriptor *pMsd = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pAssembly->GetAssembly());
        if (pMsd->IsAllTransparent() &&
            pAssembly->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeSkipVerification())
        {
            canSkipVerif = CORINFO_VERIFICATION_RUNTIME_CHECK;
        }
#endif // !FEATURE_CORECLR
    }

    return canSkipVerif;			
}

// Determine if a method can quickly exit a runtime callout from the JIT - a true return value indicates
// that the callout is not needed, false means that we cannot quicky exit

// static
bool SecurityTransparent::SecurityCalloutQuickCheck(MethodDesc *pCallerMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pCallerMD));
        PRECONDITION(pCallerMD->HasCriticalTransparentInfo());
    }
    CONTRACTL_END;

    // In coreclr, we modified the logic in the callout to also do some transparency method access checks
    // These checks need to happen regardless of trust level and we shouldn't be bailing out early 
    // just because we happen to be in Full Trust
#ifndef FEATURE_CORECLR
    // See if we need to process this callout for real, or if we can bail out early before setting up a HMF,
    // and spending a lot of time processing the transparency evaluation.  The simplest case where we can do
    // this is if the caller is critical.  In that case, we know that the caller is allowed to do whatever
    // it wants, so we quit out.
    // 
    // Additionally, if the caller is using SecurityRuleSet.Level1, which turns transparency violations into
    // security demands, we can bail out early if we know for sure all demands will succeed on the current
    // call stack.  (Note: this remains true as long as we don't start generating callouts for transparent
    // level 1 calling critical level 1, or transparent level 1 doing an assert, which are the only two
    // violations which do not succeed in the face of a successful demand).
    if (pCallerMD->IsCritical())
    {
        return true;
    }
    else
    {
        // The caller is transparent, so let's see if demands can cause transparency violations to succeed,
        // and also if all demands issued from this context will succeed.
        const SecurityTransparencyBehavior *pCallerTransparency = pCallerMD->GetAssembly()->TryGetSecurityTransparencyBehavior();
        if (pCallerTransparency != NULL &&
            pCallerTransparency->CanTransparentCodeCallLinkDemandMethods() &&
            SecurityStackWalk::HasFlagsOrFullyTrustedIgnoreMode(0))
        {
            return true;
        }
    }
#endif // !FEATURE_CORECLR

    return false;
}

CorInfoIsAccessAllowedResult SecurityTransparent::RequiresTransparentAssemblyChecks(MethodDesc* pCallerMD,
                                                                                    MethodDesc* pCalleeMD,
                                                                                    SecurityTransparencyError *pError)
{
    LIMITED_METHOD_CONTRACT;
    return RequiresTransparentCodeChecks(pCallerMD, pCalleeMD, pError);
}

CorInfoIsAccessAllowedResult SecurityTransparent::RequiresTransparentCodeChecks(MethodDesc* pCallerMD,
                                                                                MethodDesc* pCalleeMD,
                                                                                SecurityTransparencyError *pError)
{
	CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pCallerMD));
        PRECONDITION(CheckPointer(pCalleeMD));
        PRECONDITION(CheckPointer(pError, NULL_OK));
        PRECONDITION(!pCalleeMD->IsILStub());
    }
    CONTRACTL_END;

	// check if the caller assembly is transparent and NOT an interception stub (e.g. marshalling)
	bool doChecks = !pCallerMD->IsILStub() && IsMethodTransparent(pCallerMD);

    if (doChecks && Security::IsTransparencyEnforcementEnabled())
    {
        if (!IsTransparentCallerAllowed(pCallerMD, pCalleeMD, pError))
        {
            // intercept the call to throw a MAE at runtime (more debuggable than throwing MAE at JIT-time)
            // IsTransparentCallerAllowed will have set pError if necessary
            return CORINFO_ACCESS_RUNTIME_CHECK;
        }
            
        // Check to see if the callee has a LinkDemand, if so we may need to intercept the call.
        if (pCalleeMD->RequiresLinktimeCheck())
        {
            if (pCalleeMD->RequiresLinkTimeCheckHostProtectionOnly()
#ifndef CROSSGEN_COMPILE
                && GetHostProtectionManager()->GetProtectedCategories() == eNoChecks
#endif // CROSSGEN_COMPILE
                )
            {
                // exclude HPA which are marked as LinkDemand and there is no HostProtection enabled currently
                return CORINFO_ACCESS_ALLOWED;
            }

            // There was a reason other than simply conditional APTCA that the method required a linktime
            // check - intercept the call later.
            if (pError != NULL)
            {
                *pError = SecurityTransparencyError_CallLinkDemand;
            }

            return CORINFO_ACCESS_RUNTIME_CHECK;
        }
    }

    return CORINFO_ACCESS_ALLOWED;
}


#ifndef CROSSGEN_COMPILE

// Perform appropriate Transparency checks if the caller to the Load(byte[] ) without passing in an input Evidence is Transparent
VOID SecurityTransparent::PerformTransparencyChecksForLoadByteArray(MethodDesc* pCallerMD, AssemblySecurityDescriptor* pLoadedSecDesc)
{	
	CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

#ifdef FEATURE_CAS_POLICY
	GCX_COOP();
	// check to see if the method that does the Load(byte[] ) is transparent
	if (IsMethodTransparent(pCallerMD))
	{		
		Assembly* pLoadedAssembly = pLoadedSecDesc->GetAssembly();
		// check to see if the byte[] being loaded is critical, i.e. not Transparent
		if (!ModuleSecurityDescriptor::IsMarkedTransparent(pLoadedAssembly))
		{
			// if transparent code loads a byte[] that is critical, need to inject appropriate demands
			if (pLoadedSecDesc->IsFullyTrusted()) // if the loaded code is full-trust
			{
				// do a full-demand for Full-Trust				
				OBJECTREF permSet = NULL;
				GCPROTECT_BEGIN(permSet);
        			Security::GetPermissionInstance(&permSet, SECURITY_FULL_TRUST);
					Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, permSet);
				GCPROTECT_END();// do a full-demand for Full-Trust				
			}
			else
			{
				// otherwise inject a Demand for permissions being granted?
				struct _localGC {
                			OBJECTREF granted;
                			OBJECTREF denied;
            			} localGC;
            	ZeroMemory(&localGC, sizeof(localGC));

            	GCPROTECT_BEGIN(localGC);
				{
					localGC.granted = pLoadedSecDesc->GetGrantedPermissionSet(&(localGC.denied));
					Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, localGC.granted);
            	}
				GCPROTECT_END();
			}
		}		
	}	
#endif // FEATURE_CAS_POLICY
}

static void ConvertLinkDemandToFullDemand(MethodDesc* pCallerMD, MethodDesc* pCalleeMD) 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCallerMD));
        PRECONDITION(CheckPointer(pCalleeMD));
        PRECONDITION(pCallerMD->IsTransparent());
    }
    CONTRACTL_END;

    if (!pCalleeMD->RequiresLinktimeCheck() ||
        pCalleeMD->RequiresLinkTimeCheckHostProtectionOnly()) 
    {
        return;
    }

    if (!Security::IsTransparencyEnforcementEnabled())
    {
        return;
    }

    // Profilers may wish to suppress linktime checks for methods they're profiling
    if (Security::BypassSecurityChecksForProfiler(pCallerMD))
    {
        return;
    }

    struct
    {
        OBJECTREF refClassNonCasDemands;
        OBJECTREF refClassCasDemands;
        OBJECTREF refMethodNonCasDemands;
        OBJECTREF refMethodCasDemands;
        OBJECTREF refThrowable;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    LinktimeCheckReason linktimeCheckReason = Security::GetLinktimeCheckReason(pCalleeMD,
                                                                               &gc.refClassCasDemands,
                                                                               &gc.refClassNonCasDemands,
                                                                               &gc.refMethodCasDemands,
                                                                               &gc.refMethodNonCasDemands);

#ifdef FEATURE_APTCA
    BOOL fCallerIsAPTCA = pCallerMD->GetAssembly()->AllowUntrustedCaller();

    if ((linktimeCheckReason & LinktimeCheckReason_AptcaCheck))
    {
        if (fCallerIsAPTCA &&    
            Security::IsUntrustedCallerCheckNeeded(pCalleeMD, pCallerMD->GetAssembly()))
        {
#ifdef _DEBUG
            if (g_pConfig->LogTransparencyErrors())
            {
                SecurityTransparent::LogTransparencyError(pCallerMD, "Transparent method calling an APTCA protected assembly", pCalleeMD);
            }
            if (!g_pConfig->DisableTransparencyEnforcement())
#endif // _DEBUG
            {
                // Depending on the transparency model, we need to either fail the attempt to call a method
                // protected with the APTCA link demand, or conver it to a full demand.  Note that we need to
                // upgrade to a full demand if either the caller of callee are in v2 mode, the APTCA check is
                // conceptually a link demand, and for link demands we do the conversion if either assembly is
                // using the v2 rules.
                if (pCallerMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallLinkDemandMethods() ||
                    pCalleeMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallLinkDemandMethods())
                {
                    OBJECTREF permSet = NULL;
                    GCPROTECT_BEGIN(permSet);
                    Security::GetPermissionInstance(&permSet, SECURITY_FULL_TRUST);
                    Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, permSet);
                    GCPROTECT_END();
                }
                else
                {
                    ::ThrowMethodAccessException(pCallerMD, pCalleeMD, FALSE, IDS_E_TRANSPARENT_CALL_LINKDEMAND);
                }
            }
        }
    }
#endif // FEATURE_APTCA

            
    // The following logic turns link demands on the target method into full stack walks

    if ((linktimeCheckReason & LinktimeCheckReason_CasDemand) ||
        (linktimeCheckReason & LinktimeCheckReason_NonCasDemand))
    {
        // If we found a link demand, then we need to make sure that both the callee's transparency model
        // allows for it to satisfy a link demand.  We check both since a v4 caller calling a v2 assembly may
        // be attempting to satisfy a LinkDemand which the v2 assembly has not yet had a chance to remove.
        if (!pCallerMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallLinkDemandMethods() &&
            !pCalleeMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallLinkDemandMethods() &&
            (gc.refClassCasDemands != NULL || gc.refMethodCasDemands != NULL))
        {
#ifdef _DEBUG
            if (g_pConfig->LogTransparencyErrors())
            {
                SecurityTransparent::LogTransparencyError(pCallerMD, "Transparent method calling a LinkDemand protected method", pCalleeMD);
            }
            if (!g_pConfig->DisableTransparencyEnforcement())
#endif // _DEBUG
            {
                ::ThrowMethodAccessException(pCallerMD, pCalleeMD, FALSE, IDS_E_TRANSPARENT_CALL_LINKDEMAND);
            }
        }

        // CAS Link Demands
        if (gc.refClassCasDemands != NULL)
            Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, gc.refClassCasDemands);
        if (gc.refMethodCasDemands != NULL)
            Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, gc.refMethodCasDemands);

        // Non-CAS demands are not applied against a grant set, they're standalone.
        if (gc.refClassNonCasDemands != NULL)
            Security::CheckNonCasDemand(&gc.refClassNonCasDemands);
        if (gc.refMethodNonCasDemands != NULL)
            Security::CheckNonCasDemand(&gc.refMethodNonCasDemands);
    }


    //
    // Make sure that the callee is allowed to call unmanaged code if the target is native.
    //

    if (linktimeCheckReason & LinktimeCheckReason_NativeCodeCall)
    {
#ifdef _DEBUG
        if (g_pConfig->LogTransparencyErrors())
        {
            SecurityTransparent::LogTransparencyError(pCallerMD, "Transparent method calling unmanaged code");
        }
#endif // _DEBUG

        if (pCallerMD->GetAssembly()->GetSecurityTransparencyBehavior()->CanTransparentCodeCallUnmanagedCode())
        {
#ifdef FEATURE_APTCA
            if (fCallerIsAPTCA)
            {
                // if the caller assembly is APTCA, then only inject this demand, for NON-APTCA we will allow
                // calls to native code
                // NOTE: the JIT would have already performed the LinkDemand for this anyways
                Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_UNMANAGED_CODE);        
            }
#endif // FEATURE_APTCA
        }
        else
        {
#if defined(FEATURE_CORECLR_COVERAGE_BUILD) && defined(FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED)
            // For code coverage builds we have an issue where the inserted types/methods are not annotated.
            // In patricular, there may be p/invokes from transparent code. Allow that on cov builds for platform assemblies.
            // Paranoia: allow this only on non shp builds - all builds except the SHP type will have
            // FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED defined. So we can use that to figure out if this is a SHP build
            // type that someone is trying to relax that constraint on and not allow that.
            if (!pCalleeMD->GetModule()->GetFile()->GetAssembly()->IsProfileAssembly())
#endif // defined(FEATURE_CORECLR_COVERAGE_BUILD) && defined(FEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED)
            {
                ::ThrowMethodAccessException(pCallerMD, pCalleeMD, FALSE, IDS_E_TRANSPARENT_CALL_NATIVE);
            }
        }
    }

    GCPROTECT_END();
}


VOID SecurityTransparent::EnforceTransparentAssemblyChecks(MethodDesc* pCallerMD, MethodDesc* pCalleeMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pCallerMD));
        PRECONDITION(Security::IsMethodTransparent(pCallerMD));
        PRECONDITION(CheckPointer(pCalleeMD));
	    INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (!Security::IsTransparencyEnforcementEnabled())
    {
        return;
    }

    // Profilers may wish to suppress transparency checks for methods they're profiling
    if (Security::BypassSecurityChecksForProfiler(pCallerMD))
    {
        return;
    }

    // if target is critical, and not marked as TreatAsSafe, Access ERROR.
    if (Security::IsMethodCritical(pCalleeMD) && !Security::IsMethodSafeCritical(pCalleeMD))
    {

        const SecurityTransparencyBehavior *pCalleeTransparency = 
            pCalleeMD->GetAssembly()->GetSecurityTransparencyBehavior();
        const SecurityTransparencyBehavior *pCallerTransparency =
            pCallerMD->GetAssembly()->GetSecurityTransparencyBehavior();

        // If critical methods in the target can be converted to a link demand for legacy callers, then we
        // need to do that conversion.  Otherwise, this access is disallowed.
        if (pCalleeTransparency->CanCriticalMembersBeConvertedToLinkDemand() &&
            pCallerTransparency->CanTransparentCodeCallLinkDemandMethods())
        {
            ConvertCriticalMethodToLinkDemand(pCallerMD);
        }
        else
        {
            // Conversion to a LinkDemand was not allowed, so we need to 
#ifdef _DEBUG
            if (g_pConfig->LogTransparencyErrors())
            {
                LogTransparencyError(pCallerMD, "Transparent method accessing a critical method", pCalleeMD);
            }
#endif // _DEBUG
            ::ThrowMethodAccessException(pCallerMD, pCalleeMD, TRUE, IDS_E_CRITICAL_METHOD_ACCESS_DENIED);
        }
    }

    ConvertLinkDemandToFullDemand(pCallerMD, pCalleeMD);
}


VOID SecurityTransparent::EnforceTransparentDelegateChecks(MethodTable* pDelegateMT, MethodDesc* pCalleeMD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pDelegateMT));
        PRECONDITION(CheckPointer(pCalleeMD));
        INJECT_FAULT(COMPlusThrowOM(););
    } 
    CONTRACTL_END;

#ifdef FEATURE_CORECLR
    // We only enforce delegate binding rules in partial trust
    if (GetAppDomain()->GetSecurityDescriptor()->IsFullyTrusted())
        return;

    StackSString strMethod;
    TypeString::AppendMethod(strMethod, pCalleeMD, pCalleeMD->GetClassInstantiation(), TypeString::FormatNamespace | TypeString::FormatAngleBrackets| TypeString::FormatSignature);
    StackSString strDelegateType;
    TypeString::AppendType(strDelegateType, pDelegateMT, TypeString::FormatNamespace | TypeString::FormatAngleBrackets| TypeString::FormatSignature);

    COMPlusThrowHR(COR_E_METHODACCESS, IDS_E_DELEGATE_BINDING_TRANSPARENCY, strDelegateType.GetUnicode(), strMethod.GetUnicode());
#endif // FEATURE_CORECLR
}

#endif // CROSSGEN_COMPILE


BOOL SecurityTransparent::IsMethodTransparent(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    // Is transparency info cached?
    if (pMD->HasCriticalTransparentInfo())
    {
        return !pMD->IsCritical();
    }

    MethodSecurityDescriptor methSecurityDescriptor(pMD);
    return !methSecurityDescriptor.IsCritical();
}

BOOL SecurityTransparent::IsMethodCritical(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

     // Is transparency info cached?
     if (pMD->HasCriticalTransparentInfo())
     {
         return pMD->IsCritical();
     }

    MethodSecurityDescriptor methSecurityDescriptor(pMD);
    return methSecurityDescriptor.IsCritical();
}

// Returns True if a method is SafeCritical (=> not Transparent and not Critical)
BOOL SecurityTransparent::IsMethodSafeCritical(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    // Is transparency info cached?
    if (pMD->HasCriticalTransparentInfo())
    {
        return (pMD->IsCritical() && pMD->IsTreatAsSafe());
    }

    MethodSecurityDescriptor methSecurityDescriptor(pMD);
    return (methSecurityDescriptor.IsCritical() && methSecurityDescriptor.IsTreatAsSafe());
}

BOOL SecurityTransparent::IsTypeCritical(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    EEClass *pClass = pMT->GetClass();
    if (pClass->HasCriticalTransparentInfo())
    {
        return pClass->IsCritical();
    }

    TypeSecurityDescriptor typeSecurityDescriptor(pMT);
    return typeSecurityDescriptor.IsCritical();
}

BOOL SecurityTransparent::IsTypeSafeCritical(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    EEClass *pClass = pMT->GetClass();
    if (pClass->HasCriticalTransparentInfo())
    {
        return pClass->IsCritical() && pClass->IsTreatAsSafe();
    }

    TypeSecurityDescriptor typeSecurityDescriptor(pMT);
    return typeSecurityDescriptor.IsCritical() &&
           typeSecurityDescriptor.IsTreatAsSafe();
}

BOOL SecurityTransparent::IsTypeTransparent(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    EEClass *pClass = pMT->GetClass();
    if (pClass->HasCriticalTransparentInfo())
    {
        return !pClass->IsCritical();
    }

    TypeSecurityDescriptor typeSecurityDescriptor(pMT);
    return !typeSecurityDescriptor.IsCritical();
}

// Returns TRUE if a type is transparent and contains only transparent members
// static
BOOL SecurityTransparent::IsTypeAllTransparent(MethodTable * pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    EEClass *pClass = pMT->GetClass();
    if (pClass->HasCriticalTransparentInfo())
    {
        return pClass->IsAllTransparent();
    }

    TypeSecurityDescriptor typeSecurityDescriptor(pMT);
    return typeSecurityDescriptor.IsAllTransparent();
}

BOOL SecurityTransparent::IsTypeAllCritical(MethodTable * pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    EEClass *pClass = pMT->GetClass();
    if (pClass->HasCriticalTransparentInfo())
    {
        return pClass->IsAllCritical();
    }

    TypeSecurityDescriptor typeSecurityDescriptor(pMT);
    return typeSecurityDescriptor.IsAllCritical();
}

BOOL SecurityTransparent::IsFieldTransparent(FieldDesc* pFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    FieldSecurityDescriptor fsd(pFD);
    return !fsd.IsCritical();
}

BOOL SecurityTransparent::IsFieldCritical(FieldDesc* pFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    FieldSecurityDescriptor fsd(pFD);
    return fsd.IsCritical();
}

// Returns True if a method is SafeCritical (=> not Transparent and not Critical)
BOOL SecurityTransparent::IsFieldSafeCritical(FieldDesc* pFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    FieldSecurityDescriptor fsd(pFD);
    return fsd.IsCritical() && fsd.IsTreatAsSafe();
}

// Returns True if the token is transparent
BOOL SecurityTransparent::IsTokenTransparent(Module *pModule, mdToken tkToken)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    ModuleSecurityDescriptor *pMsd = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pModule->GetAssembly());
    if (pMsd->IsAllCritical())
    {
        return FALSE;
    }

    const TokenSecurityDescriptorFlags criticalMask = TokenSecurityDescriptorFlags_AllCritical |
                                                      TokenSecurityDescriptorFlags_Critical |
                                                      TokenSecurityDescriptorFlags_SafeCritical;
    TokenSecurityDescriptor tokenSecurityDescriptor(pModule, tkToken);
	return !(tokenSecurityDescriptor.GetMetadataFlags() & criticalMask);
}

// Fuctor type to do perform class access checks on any disallowed transparent -> critical accesses.
class DoSecurityClassAccessChecksFunctor
{
private:
    MethodDesc *m_pCallerMD;
    CorInfoSecurityRuntimeChecks m_check;

public:
    DoSecurityClassAccessChecksFunctor(MethodDesc *pCallerMD, CorInfoSecurityRuntimeChecks check)
        : m_pCallerMD(pCallerMD),
          m_check(check)
    {
        LIMITED_METHOD_CONTRACT;
    }

    DoSecurityClassAccessChecksFunctor(const DoSecurityClassAccessChecksFunctor &other); // not implemented

    void operator()(MethodTable *pMT)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        
        // We can get caller checks of 0 if we're in AlwaysInsertCallout mode, so make sure to do all of our
        // work under checks for specific flags
        if (m_check & CORINFO_ACCESS_SECURITY_TRANSPARENCY)
        {
            StaticAccessCheckContext accessContext(m_pCallerMD);

            if (!Security::CheckCriticalAccess(&accessContext, NULL, NULL, pMT))
            {
                ThrowTypeAccessException(m_pCallerMD, pMT, TRUE, IDS_E_CRITICAL_TYPE_ACCESS_DENIED);
            }
        }
    }
};

// Check that a calling method is allowed to access a type handle for security reasons.  This checks:
//   1. That transparency allows the caller to use the type
//      
// The method returns if the checks succeed and throws on error.
// 
// static
void SecurityTransparent::DoSecurityClassAccessChecks(MethodDesc *pCallerMD,
                                                      const TypeHandle &calleeTH,
                                                      CorInfoSecurityRuntimeChecks check)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DoSecurityClassAccessChecksFunctor classAccessChecks(pCallerMD, check);
    calleeTH.ForEachComponentMethodTable(classAccessChecks);
}

//
// Transparency behavior implementations
//

//---------------------------------------------------------------------------------------
//
// Transparency behavior implementation for v4 and CoreCLR assemblies
//

class TransparencyBehaviorImpl : public ISecurityTransparencyImpl
{
public:

    // Get bits that indicate how transparency should behave in different situations
    virtual SecurityTransparencyBehaviorFlags GetBehaviorFlags() const
    {
        LIMITED_METHOD_CONTRACT;
        return SecurityTransparencyBehaviorFlags_AttributesRequireTransparencyCheck |
               SecurityTransparencyBehaviorFlags_CriticalMembersConvertToLinkDemand |
               SecurityTransparencyBehaviorFlags_InheritanceRulesEnforced |
               SecurityTransparencyBehaviorFlags_PartialTrustImpliesAllTransparent |
               SecurityTransparencyBehaviorFlags_ScopeAppliesOnlyToIntroducedMethods;
    }

    // Transparency field behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  Critical (any)                    Critical
    //  SafeCritical                      Safe critical
    //  TAS (no critical)                 No effect
    //  TAS (with any critical)           Safe critical
    virtual FieldSecurityDescriptorFlags MapFieldAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        FieldSecurityDescriptorFlags fieldFlags = FieldSecurityDescriptorFlags_None;

        if (tokenFlags & TokenSecurityDescriptorFlags_Critical)
        {
            fieldFlags |= FieldSecurityDescriptorFlags_IsCritical;

            if (tokenFlags & TokenSecurityDescriptorFlags_TreatAsSafe)
            {
                fieldFlags |= FieldSecurityDescriptorFlags_IsTreatAsSafe;
            }
        }

        if (tokenFlags & TokenSecurityDescriptorFlags_SafeCritical)
        {
            fieldFlags |= FieldSecurityDescriptorFlags_IsCritical | FieldSecurityDescriptorFlags_IsTreatAsSafe;
        }

        return fieldFlags;
    }

    // Transparency module behavior mappings for an introduced method:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  Critical (any)                    Critical
    //  SafeCritical                      Safe critical
    //  TAS (no critical)                 No effect
    //  TAS (with any critical)           Safe critical
    virtual MethodSecurityDescriptorFlags MapMethodAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        MethodSecurityDescriptorFlags methodFlags = MethodSecurityDescriptorFlags_None;

        if (tokenFlags & TokenSecurityDescriptorFlags_Critical)
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsCritical;

            if (tokenFlags & TokenSecurityDescriptorFlags_TreatAsSafe)
            {
                methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
            }
        }

        if (tokenFlags & TokenSecurityDescriptorFlags_SafeCritical)
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsCritical |
                           MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }

        return methodFlags;
    }

    // Transparency module behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  APTCA                             Mixed transparency + APTCA
    //  Critical (scoped)                 All critical + APTCA
    //  Critical (all)                    All critical + APTCA
    //  SafeCritical                      No effect
    //  TAS (no critical)                 No effect
    //  TAS (with scoped critical)        All safe critical + APTCA
    //  TAS (with all critical)           All safe critical + APTCA
    //  Transparent                       All transparent + APTCA
    //
    // If the assembly has no attributes, then it will be opportunistically critical.
    //
    // APTCA is granted to all assemblies because we rely upon transparent code being unable to call critical
    // code to enforce the APTCA check.  Since all partial trust code must be transparent, this provides the
    // same effect.
    virtual ModuleSecurityDescriptorFlags MapModuleAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        ModuleSecurityDescriptorFlags moduleFlags = ModuleSecurityDescriptorFlags_None;

#if defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)
        if (tokenFlags & TokenSecurityDescriptorFlags_APTCA)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAPTCA;
        }
#endif // defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)

        if (tokenFlags & TokenSecurityDescriptorFlags_Critical)
        {
            // We don't pay attention to the critical scope if we're not a legacy assembly
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAllCritical;

            if (tokenFlags & TokenSecurityDescriptorFlags_TreatAsSafe)
            {
                moduleFlags |= ModuleSecurityDescriptorFlags_IsTreatAsSafe;
            }
        }

        if (tokenFlags & TokenSecurityDescriptorFlags_Transparent)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAllTransparent;
        }

        // If we didn't see APTCA/CA, Transparent, or any form of Critical, then the assembly is opportunistically
        // critical.
        const ModuleSecurityDescriptorFlags transparencyMask = ModuleSecurityDescriptorFlags_IsAPTCA |
                                                               ModuleSecurityDescriptorFlags_IsAllTransparent |
                                                               ModuleSecurityDescriptorFlags_IsAllCritical;
        if (!(moduleFlags & transparencyMask))
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_IsOpportunisticallyCritical;
        }

        // If the token asks to not have IL verification done in full trust, propigate that to the module
        if (tokenFlags & TokenSecurityDescriptorFlags_SkipFullTrustVerification)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_SkipFullTrustVerification;
        }

        // We rely on transparent / critical checks to provide APTCA enforcement in the v4 model, so all assemblies
        // get APTCA.
        moduleFlags |= ModuleSecurityDescriptorFlags_IsAPTCA;

        return moduleFlags;
    }

    // Transparency type behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  Critical (any)                    All critical
    //  SafeCritical                      All safe critical
    //  TAS (no critical)                 No effect on the type, but save TAS bit since members of the type may be critical
    //  TAS (with any critical)           All SafeCritical
    virtual TypeSecurityDescriptorFlags MapTypeAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        TypeSecurityDescriptorFlags typeFlags = TypeSecurityDescriptorFlags_None;

        if (tokenFlags & TokenSecurityDescriptorFlags_Critical)
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsCritical |
                         TypeSecurityDescriptorFlags_IsAllCritical;
        }

        // SafeCritical always means all critical + TAS
        if (tokenFlags & TokenSecurityDescriptorFlags_SafeCritical)
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsCritical |
                         TypeSecurityDescriptorFlags_IsAllCritical |
                         TypeSecurityDescriptorFlags_IsTreatAsSafe;
        }

        if (tokenFlags & TokenSecurityDescriptorFlags_TreatAsSafe)
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsTreatAsSafe;
        }

        return typeFlags;
    }
};

#ifndef FEATURE_CORECLR

//---------------------------------------------------------------------------------------
//
// Transparency behavior implementation for v2 assemblies
//

class LegacyTransparencyBehaviorImpl : public ISecurityTransparencyImpl
{
public:
    // Get bits that indicate how transparency should behave in different situations
    virtual SecurityTransparencyBehaviorFlags GetBehaviorFlags() const
    {
        LIMITED_METHOD_CONTRACT;
        return SecurityTransparencyBehaviorFlags_IntroducedCriticalsMayAddTreatAsSafe |
               SecurityTransparencyBehaviorFlags_OpportunisticIsSafeCriticalMethods |
               SecurityTransparencyBehaviorFlags_PartialTrustImpliesAllTransparent |
               SecurityTransparencyBehaviorFlags_PublicImpliesTreatAsSafe |
               SecurityTransparencyBehaviorFlags_TransparentCodeCanCallLinkDemand |
               SecurityTransaprencyBehaviorFlags_TransparentCodeCanCallUnmanagedCode |
               SecurityTransparencyBehaviorFlags_TransparentCodeCanSkipVerification |
               SecurityTransparencyBehaviorFlags_UnsignedImpliesAPTCA;
    }

    // Legacy transparency field behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  Critical (any)                    Critical
    //  SafeCritical                      Safe critical
    //  TAS (no critical)                 No effect
    //  TAS (with any critical)           Safe critical
    virtual FieldSecurityDescriptorFlags MapFieldAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        WRAPPER_NO_CONTRACT;

        // Legacy transparency behaves the same for fields as the current transparency model, so we just forward
        // this call to that implementation.
        TransparencyBehaviorImpl forwardImpl;
        return forwardImpl.MapFieldAttributes(tokenFlags);
    }


    // Legacy transparency method behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  Critical (any)                    Critical
    //  SafeCritical                      Safe critical
    //  TAS (no critical)                 No effect
    //  TAS (with any critical)           Safe critical
    virtual MethodSecurityDescriptorFlags MapMethodAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        WRAPPER_NO_CONTRACT;

        // Legacy transparency behaves the same for methods as the current transparency model, so we just forward
        // this call to that implementation.
        TransparencyBehaviorImpl forwardImpl;
        return forwardImpl.MapMethodAttributes(tokenFlags);
    }

    // Legacy transparency module behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  APTCA                             APTCA
    //  ConditionlAPTCA                   Exception
    //  Critical (scoped)                 Mixed transparency
    //  Critical (all)                    All critical
    //  SafeCritical                      All safe critical
    //  TAS (no critical)                 No effect
    //  TAS (with scoped critical)        No effect
    //  TAS (with all critical)           All safe critical
    //  Transparent                       All transparent
    //
    // Having no transparent, critical, or safe critical attributes means that the assembly should have all
    // transparent types and all safe critical methods.
    virtual ModuleSecurityDescriptorFlags MapModuleAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        ModuleSecurityDescriptorFlags moduleFlags = ModuleSecurityDescriptorFlags_None;
        bool fShouldBeOpportunisticallyCritical = true;

#if defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)
        if (tokenFlags & TokenSecurityDescriptorFlags_APTCA)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAPTCA;
        }
#endif // defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)

        if (tokenFlags & TokenSecurityDescriptorFlags_Transparent)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAllTransparent;
            fShouldBeOpportunisticallyCritical = false;
        }

        if (tokenFlags & TokenSecurityDescriptorFlags_Critical)
        {
            fShouldBeOpportunisticallyCritical = false;

            // If we're critical, but not all critical that means we're mixed.
            if (tokenFlags & TokenSecurityDescriptorFlags_AllCritical)
            {
                moduleFlags |= ModuleSecurityDescriptorFlags_IsAllCritical;

                // If we're all critical and treat as safe, that means we're safe critical
                if (tokenFlags & TokenSecurityDescriptorFlags_TreatAsSafe)
                {
                    moduleFlags |= ModuleSecurityDescriptorFlags_IsTreatAsSafe;
                }
            }
        }

        // SafeCritical always means Critical + TreatAsSafe; we can get in this case for legacy assemblies if the
        // assembly is actually a v4 assembly which is using the Legacy attribute.
        if (tokenFlags & TokenSecurityDescriptorFlags_SafeCritical)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAllCritical |
                           ModuleSecurityDescriptorFlags_IsTreatAsSafe;
            fShouldBeOpportunisticallyCritical = false;
        }

        // If we didn't find an attribute that indicates the assembly cares about transparency, then it is
        // opportunistically critical.
        if (fShouldBeOpportunisticallyCritical)
        {
            _ASSERTE(!(moduleFlags & ModuleSecurityDescriptorFlags_IsAllTransparent));
            _ASSERTE(!(moduleFlags & ModuleSecurityDescriptorFlags_IsAllCritical));

            moduleFlags |= ModuleSecurityDescriptorFlags_IsOpportunisticallyCritical;
        }

        // If the token asks to not have IL verification done in full trust, propigate that to the module
        if (tokenFlags & TokenSecurityDescriptorFlags_SkipFullTrustVerification)
        {
            moduleFlags |= ModuleSecurityDescriptorFlags_SkipFullTrustVerification;
        }

        return moduleFlags;
    }

    // Legacy transparency type behavior mappings:
    //  Attribute                         Behavior
    //  -----------------------------------------------------
    //  Critical (scoped)                 Critical, but not all critical
    //  Critical (all)                    All critical
    //  SafeCritical                      All safe critical
    //  TAS (no critical)                 No effect on the type, but save TAS bit for members of the type
    //  TAS (with scoped critical)        SafeCritical, but not all critical
    //  TAS (with all critical)           All SafeCritical
    virtual TypeSecurityDescriptorFlags MapTypeAttributes(TokenSecurityDescriptorFlags tokenFlags) const
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        TypeSecurityDescriptorFlags typeFlags = TypeSecurityDescriptorFlags_None;

        if (tokenFlags & TokenSecurityDescriptorFlags_Critical)
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsCritical;

            // We only consider all critical if the critical attribute was present
            if (tokenFlags & TokenSecurityDescriptorFlags_AllCritical)
            {
                typeFlags |= TypeSecurityDescriptorFlags_IsAllCritical;
            }
        }

        // SafeCritical always means all critical + TAS
        if (tokenFlags & TokenSecurityDescriptorFlags_SafeCritical)
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsCritical |
                         TypeSecurityDescriptorFlags_IsAllCritical |
                         TypeSecurityDescriptorFlags_IsTreatAsSafe;
        }

        if (tokenFlags & TokenSecurityDescriptorFlags_TreatAsSafe)
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsTreatAsSafe;
        }

        return typeFlags;
    }
};

#endif // !FEATURE_CORECLR

//
// Shared transparency behavior objects
//

//---------------------------------------------------------------------------------------
//
// Access a shared security transparency behavior object, creating it if the object has
// not yet been used.
//

template <class T>
const SecurityTransparencyBehavior *GetOrCreateTransparencyBehavior(SecurityTransparencyBehavior **ppBehavior)
{
    CONTRACT(const SecurityTransparencyBehavior *)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(ppBehavior));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (*ppBehavior == NULL)
    {
        NewHolder<ISecurityTransparencyImpl> pImpl(new T);
        NewHolder<SecurityTransparencyBehavior> pBehavior(new SecurityTransparencyBehavior(pImpl));

        SecurityTransparencyBehavior *pPrevBehavior = 
            InterlockedCompareExchangeT(ppBehavior, pBehavior.GetValue(), NULL);

        if (pPrevBehavior == NULL)
        {
            pBehavior.SuppressRelease();
            pImpl.SuppressRelease();
        }
    }

    RETURN(*ppBehavior);
}

// Transparency behavior object for v4 transparent assemblies
// static
SecurityTransparencyBehavior *SecurityTransparencyBehavior::s_pStandardTransparencyBehavior = NULL;

#ifndef FEATURE_CORECLR

// Transpraency behavior object for v2 transparent assemblies
// static
SecurityTransparencyBehavior *SecurityTransparencyBehavior::s_pLegacyTransparencyBehavior = NULL;

#endif // !FEATURE_CORECLR

//---------------------------------------------------------------------------------------
//
// Get a security transparency object for an assembly with the specified attributes on
// its manifest
//
// Arguments:
//    moduleTokenFlags - flags from reading the security attributes of the assembly's
//                       manifest module
//

const SecurityTransparencyBehavior *SecurityTransparencyBehavior::GetTransparencyBehavior(SecurityRuleSet ruleSet)
{
    CONTRACT(const SecurityTransparencyBehavior *)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(ruleSet == SecurityRuleSet_Level1 || ruleSet == SecurityRuleSet_Level2);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

#ifndef FEATURE_CORECLR
    if (ruleSet == SecurityRuleSet_Level1)
    {
		// Level 1 rules - v2.0 behavior
        RETURN(GetOrCreateTransparencyBehavior<LegacyTransparencyBehaviorImpl>(&s_pLegacyTransparencyBehavior));
    }
    else
#endif // FEATURE_CORECLR;
    {
		// Level 2 rules - v4.0 behavior
        RETURN(GetOrCreateTransparencyBehavior<TransparencyBehaviorImpl>(&s_pStandardTransparencyBehavior));
    }
}

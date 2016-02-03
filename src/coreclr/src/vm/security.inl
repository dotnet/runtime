// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 

#ifndef _INL_SECURITY_
#define _INL_SECURITY_

#include "securitydescriptorassembly.h"
#include "securitydescriptorappdomain.h"
#include "securitystackwalk.h"

// Init
inline void Security::Start() 
{
    WRAPPER_NO_CONTRACT;
    SecurityPolicy::Start();
}

inline  void Security::Stop()
{
    WRAPPER_NO_CONTRACT;
    SecurityPolicy::Stop();
}
#ifdef FEATURE_CAS_POLICY
inline void Security::SaveCache() 
{
    WRAPPER_NO_CONTRACT;
    SecurityPolicy::SaveCache();
}
#endif
// ----------------------------------------
// SecurityPolicy
// ----------------------------------------

#ifdef FEATURE_CAS_POLICY

//---------------------------------------------------------------------------------------
//
// Determine if the entire process is running with CAS policy enabled for legacy
// compatibility.  If this value is false, the CLR does not apply any security policy.
// Instead, it defers to a host if one is present or grants assemblies full trust.
//

inline bool Security::IsProcessWideLegacyCasPolicyEnabled()
{
    LIMITED_METHOD_CONTRACT;

    // APPX precludes the use of legacy CAS policy
    if (AppX::IsAppXProcess())
    {
        return false;
    }

    return CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Security_LegacyCasPolicy) ||
           CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Security_NetFx40LegacySecurityPolicy);
}

//---------------------------------------------------------------------------------------
//
// In pre-v4 versions of the CLR, doing a LoadFrom for a file in a remote location would
// implicitly sandbox that assembly.  If CAS policy is disabled, then these applications
// will suddenly be granting full trust to assemblies they expected to be sandboxed.  In
// order to prevent this, these LoadFroms are disabled unless the application has explcitly
// configured itself to allow them.
// 
// This method returns the a value that indicates if the application has indicated that it
// is safe to LoadFrom remote locations and that the CLR should not block these loads.
//

inline bool Security::CanLoadFromRemoteSources()
{
    WRAPPER_NO_CONTRACT;
    return !!CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Security_LoadFromRemoteSources);
}

#endif // FEATURE_CAS_POLICY

inline BOOL Security::CanCallUnmanagedCode(Module *pModule) 
{
    WRAPPER_NO_CONTRACT; 
    return SecurityPolicy::CanCallUnmanagedCode(pModule); 
}

#ifndef DACCESS_COMPILE
inline BOOL Security::CanAssert(Module *pModule) 
{
    WRAPPER_NO_CONTRACT; 
    SharedSecurityDescriptor *pSharedSecDesc = static_cast<SharedSecurityDescriptor*>(pModule->GetAssembly()->GetSharedSecurityDescriptor());
    if (pSharedSecDesc)
        return pSharedSecDesc->CanAssert();

    AssemblySecurityDescriptor *pSec = static_cast<AssemblySecurityDescriptor*>(pModule->GetSecurityDescriptor());
    _ASSERTE(pSec);
    return pSec->CanAssert();
}

inline DECLSPEC_NORETURN void Security::ThrowSecurityException(__in_z const char *szDemandClass, DWORD dwFlags) 
{
    WRAPPER_NO_CONTRACT;
    SecurityPolicy::ThrowSecurityException(szDemandClass, dwFlags);
}

inline BOOL Security::CanTailCall(MethodDesc* pMD)
{
    WRAPPER_NO_CONTRACT; 
    return Security::CanSkipVerification(pMD);
}
    
inline BOOL Security::CanAccessNonVerifiableExplicitField(MethodDesc* pMD)
{
    WRAPPER_NO_CONTRACT
    // just check if the method can have unverifiable code
    return Security::CanSkipVerification(pMD);
}
#endif

// ----------------------------------------
// SecurityAttributes
// ----------------------------------------

inline OBJECTREF Security::CreatePermissionSet(BOOL fTrusted) 
{
    WRAPPER_NO_CONTRACT; 
    return SecurityAttributes::CreatePermissionSet(fTrusted); 
}

inline void Security::CopyByteArrayToEncoding(IN U1ARRAYREF* pArray, OUT PBYTE* pbData, OUT DWORD* cbData) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityAttributes::CopyByteArrayToEncoding(pArray, pbData, cbData); 
}

inline void Security::CopyEncodingToByteArray(IN PBYTE   pbData, IN DWORD   cbData, IN OBJECTREF* pArray) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityAttributes::CopyEncodingToByteArray(pbData, cbData, pArray); 
}

// ----------------------------------------
// SecurityDeclarative
// ----------------------------------------

inline HRESULT Security::GetDeclarationFlags(IMDInternalImport *pInternalImport, mdToken token, DWORD* pdwFlags, DWORD* pdwNullFlags, BOOL* fHasSuppressUnmanagedCodeAccessAttr) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityDeclarative::GetDeclarationFlags(pInternalImport, token, pdwFlags, pdwNullFlags, fHasSuppressUnmanagedCodeAccessAttr); 
}

inline void Security::RetrieveLinktimeDemands(MethodDesc* pMD, OBJECTREF* pClassCas, OBJECTREF* pClassNonCas, OBJECTREF* pMethodCas, OBJECTREF* pMethodNonCas) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityDeclarative::RetrieveLinktimeDemands(pMD, pClassCas, pClassNonCas, pMethodCas, pMethodNonCas); 
}

inline LinktimeCheckReason Security::GetLinktimeCheckReason(MethodDesc *pMD,
                                                            OBJECTREF  *pClassCasDemands,
                                                            OBJECTREF  *pClassNonCasDemands,
                                                            OBJECTREF  *pMethodCasDemands,
                                                            OBJECTREF  *pMethodNonCasDemands)
{
    WRAPPER_NO_CONTRACT;
    return SecurityDeclarative::GetLinktimeCheckReason(pMD,
                                                       pClassCasDemands,
                                                       pClassNonCasDemands,
                                                       pMethodCasDemands,
                                                       pMethodNonCasDemands);
}

inline void Security::CheckLinkDemandAgainstAppDomain(MethodDesc *pMD) 
{
    WRAPPER_NO_CONTRACT; 
#ifdef FEATURE_CAS_POLICY
    SecurityDeclarative::CheckLinkDemandAgainstAppDomain(pMD); 
#endif
}

inline void Security::LinktimeCheckMethod(Assembly *pCaller, MethodDesc *pCallee) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityDeclarative::LinktimeCheckMethod(pCaller, pCallee); 
}

inline void Security::ClassInheritanceCheck(MethodTable *pClass, MethodTable *pParent) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityDeclarative::ClassInheritanceCheck(pClass, pParent); 
}

inline void Security::MethodInheritanceCheck(MethodDesc *pMethod, MethodDesc *pParent) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityDeclarative::MethodInheritanceCheck(pMethod, pParent); 
}

inline void Security::GetPermissionInstance(OBJECTREF *perm, int index) 
{
    WRAPPER_NO_CONTRACT; 
    SecurityDeclarative::GetPermissionInstance(perm, index); 
}

inline void Security::DoDeclarativeActions(MethodDesc *pMD, DeclActionInfo *pActions, LPVOID pSecObj, MethodSecurityDescriptor *pMSD) 
{
    WRAPPER_NO_CONTRACT; 
#ifdef FEATURE_CAS_POLICY
    SecurityDeclarative::DoDeclarativeActions(pMD, pActions, pSecObj, pMSD); 
#endif
}

#ifndef DACCESS_COMPILE
inline void Security::CheckNonCasDemand(OBJECTREF *prefDemand) 
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityDeclarative::CheckNonCasDemand(prefDemand); 
#endif
}
#endif // #ifndef DACCESS_COMPILE

inline BOOL Security::MethodIsVisibleOutsideItsAssembly(MethodDesc * pMD) 
{
    WRAPPER_NO_CONTRACT; 
    return SecurityDeclarative::MethodIsVisibleOutsideItsAssembly(pMD); 
}

inline BOOL Security::MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr, DWORD dwClassAttr, BOOL fIsGlobalClass)
{
    WRAPPER_NO_CONTRACT; 
    return SecurityDeclarative::MethodIsVisibleOutsideItsAssembly(dwMethodAttr, dwClassAttr, fIsGlobalClass);
}

inline void Security::CheckBeforeAllocConsole(AppDomain* pDomain, Assembly* pAssembly) 
{
    WRAPPER_NO_CONTRACT; 
#ifdef FEATURE_CAS_POLICY
    SecurityRuntime::CheckBeforeAllocConsole(pDomain, pAssembly); 
#endif
}

// ----------------------------------------
// SecurityStackWalk
// ----------------------------------------

// other CAS Actions
inline void Security::Demand(SecurityStackWalkType eType, OBJECTREF demand) 
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityStackWalk::Demand(eType, demand); 
#endif
}

#ifdef FEATURE_CAS_POLICY
inline void Security::DemandGrantSet(IAssemblySecurityDescriptor *psdAssembly)
{
    WRAPPER_NO_CONTRACT;
    SecurityStackWalk::DemandGrantSet(static_cast<AssemblySecurityDescriptor*>(psdAssembly));
}
#endif // FEATURE_CAS_POLICY

inline void Security::DemandSet(SecurityStackWalkType eType, OBJECTREF demand) 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
#ifdef FEATURE_CAS_POLICY
    SecurityStackWalk::DemandSet(eType, demand); 
#endif
}

inline void Security::DemandSet(SecurityStackWalkType eType, PsetCacheEntry *pPCE, DWORD dwAction) 
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityStackWalk::DemandSet(eType, pPCE, dwAction); 
#endif
}

#ifdef FEATURE_CAS_POLICY
inline void Security::ReflectionTargetDemand(DWORD dwPermission, IAssemblySecurityDescriptor *psdTarget)
{
    WRAPPER_NO_CONTRACT;
    SecurityStackWalk::ReflectionTargetDemand(dwPermission, static_cast<AssemblySecurityDescriptor*>(psdTarget));
}

inline void Security::ReflectionTargetDemand(DWORD dwPermission,
                                             IAssemblySecurityDescriptor *psdTarget,
                                             DynamicResolver * pAccessContext)
{
    WRAPPER_NO_CONTRACT;
    SecurityStackWalk::ReflectionTargetDemand(dwPermission, static_cast<AssemblySecurityDescriptor*>(psdTarget), pAccessContext);
}
#endif // FEATURE_CAS_POLICY

inline void Security::SpecialDemand(SecurityStackWalkType eType, DWORD whatPermission) 
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityStackWalk::SpecialDemand(eType, whatPermission);
#endif
}

inline void Security::InheritanceLinkDemandCheck(Assembly *pTargetAssembly, MethodDesc * pMDLinkDemand)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityDeclarative::InheritanceLinkDemandCheck(pTargetAssembly, pMDLinkDemand);
#endif
}

inline void Security::FullTrustInheritanceDemand(Assembly *pTargetAssembly)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityDeclarative::FullTrustInheritanceDemand(pTargetAssembly);
#endif
}

inline void Security::FullTrustLinkDemand(Assembly *pTargetAssembly)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CAS_POLICY
    SecurityDeclarative::FullTrustLinkDemand(pTargetAssembly);
#endif
}

#ifdef FEATURE_COMPRESSEDSTACK
// Compressed Stack

inline COMPRESSEDSTACKREF Security::GetCSFromContextTransitionFrame(Frame *pFrame) 
{
    WRAPPER_NO_CONTRACT; 
    return SecurityStackWalk::GetCSFromContextTransitionFrame(pFrame); 
}

inline BOOL Security::IsContextTransitionFrameWithCS(Frame *pFrame) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityStackWalk::IsContextTransitionFrameWithCS(pFrame); 
}
#endif // #ifdef FEATURE_COMPRESSEDSTACK
// Misc - todo: put these in better categories

FORCEINLINE  VOID Security::IncrementSecurityPerfCounter() 
{
    WRAPPER_NO_CONTRACT; 
    SecurityStackWalk::IncrementSecurityPerfCounter(); 
}

inline BOOL Security::IsSpecialRunFrame(MethodDesc *pMeth) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityStackWalk::IsSpecialRunFrame(pMeth);
}

inline BOOL Security::SkipAndFindFunctionInfo(INT32 i, MethodDesc** ppMD, OBJECTREF** ppOR, AppDomain **ppAppDomain ) 
{
    WRAPPER_NO_CONTRACT; 
    return SecurityStackWalk::SkipAndFindFunctionInfo(i, ppMD, ppOR, ppAppDomain); 
}

inline BOOL Security::SkipAndFindFunctionInfo(StackCrawlMark* pSCM, MethodDesc** ppMD, OBJECTREF** ppOR, AppDomain **ppAppDomain ) 
{
    WRAPPER_NO_CONTRACT; 
    return SecurityStackWalk::SkipAndFindFunctionInfo(pSCM, ppMD, ppOR, ppAppDomain); 
}

#ifndef DACCESS_COMPILE
inline BOOL Security::AllDomainsOnStackFullyTrusted() 
{ 
    WRAPPER_NO_CONTRACT; 
    return (SecurityStackWalk::HasFlagsOrFullyTrusted(0));
}

inline void Security::SetDefaultAppDomainProperty(IApplicationSecurityDescriptor* pASD)
    {WRAPPER_NO_CONTRACT; static_cast<ApplicationSecurityDescriptor*>(pASD)->SetDefaultAppDomain();}

inline void Security::SetDefaultAppDomainEvidenceProperty(IApplicationSecurityDescriptor* pASD)
    {WRAPPER_NO_CONTRACT; static_cast<ApplicationSecurityDescriptor*>(pASD)->SetDefaultAppDomainEvidence();}

inline BOOL Security::CheckDomainWideSpecialFlag(IApplicationSecurityDescriptor *pASD, DWORD flags)
{
    WRAPPER_NO_CONTRACT;
    return static_cast<ApplicationSecurityDescriptor*>(pASD)->CheckDomainWideSpecialFlag(flags);
}

inline BOOL Security::IsResolved(Assembly *pAssembly)
{
    WRAPPER_NO_CONTRACT;

    ISharedSecurityDescriptor *pSSD = pAssembly->GetSharedSecurityDescriptor();
    if (pSSD != NULL)
    {
        return pSSD->IsResolved();
    }
    else
    {
        IAssemblySecurityDescriptor *pSD = pAssembly->GetSecurityDescriptor();
        return pSD->IsResolved();
    }
}
#endif //! DACCESS_COMPILE

inline BOOL Security::IsMethodTransparent(MethodDesc * pMD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsMethodTransparent(pMD);
}

inline BOOL Security::IsMethodCritical(MethodDesc * pMD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsMethodCritical(pMD);
}

inline BOOL Security::IsMethodSafeCritical(MethodDesc * pMD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsMethodSafeCritical(pMD);
}

inline BOOL Security::IsTypeCritical(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsTypeCritical(pMT);
}

inline BOOL Security::IsTypeSafeCritical(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsTypeSafeCritical(pMT);
}

inline BOOL Security::IsTypeTransparent(MethodTable * pMT)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsTypeTransparent(pMT);
}

inline BOOL Security::IsTypeAllTransparent(MethodTable * pMT)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsTypeAllTransparent(pMT);
}

inline BOOL Security::IsFieldTransparent(FieldDesc * pFD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsFieldTransparent(pFD);
}

inline BOOL Security::IsFieldCritical(FieldDesc * pFD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsFieldCritical(pFD);
}

inline BOOL Security::IsFieldSafeCritical(FieldDesc * pFD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsFieldSafeCritical(pFD);
}

inline BOOL Security::IsTokenTransparent(Module* pModule, mdToken token) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::IsTokenTransparent(pModule, token);
}

inline void Security::DoSecurityClassAccessChecks(MethodDesc *pCallerMD,
                                                  const TypeHandle &calleeTH,
                                                  CorInfoSecurityRuntimeChecks checks)
{
    WRAPPER_NO_CONTRACT;
    SecurityTransparent::DoSecurityClassAccessChecks(pCallerMD, calleeTH, checks);
}

// Transparency checks
inline CorInfoIsAccessAllowedResult Security::RequiresTransparentAssemblyChecks(MethodDesc* pCaller,
                                                                                MethodDesc* pCallee,
                                                                                SecurityTransparencyError *pError) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::RequiresTransparentAssemblyChecks(pCaller, pCallee, pError);
}

inline VOID Security::EnforceTransparentDelegateChecks(MethodTable* pDelegateMT, MethodDesc* pCaller)
{
    WRAPPER_NO_CONTRACT;
    SecurityTransparent::EnforceTransparentDelegateChecks(pDelegateMT, pCaller);
}

inline VOID Security::EnforceTransparentAssemblyChecks( MethodDesc* pCallee, MethodDesc* pCaller)
{
    WRAPPER_NO_CONTRACT;
    SecurityTransparent::EnforceTransparentAssemblyChecks( pCallee, pCaller);
}

inline VOID Security::PerformTransparencyChecksForLoadByteArray(MethodDesc* pCallersMD, IAssemblySecurityDescriptor* pLoadedSecDesc)
{
    WRAPPER_NO_CONTRACT;
    SecurityTransparent::PerformTransparencyChecksForLoadByteArray(pCallersMD, static_cast<AssemblySecurityDescriptor*>(pLoadedSecDesc));
}

inline bool Security::TypeRequiresTransparencyCheck(TypeHandle type, bool checkForLinkDemands /*= false*/)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::TypeRequiresTransparencyCheck(type, checkForLinkDemands);
}

inline BOOL Security::CheckCriticalAccess(AccessCheckContext* pContext,
    MethodDesc* pOptionalTargetMethod,
    FieldDesc* pOptionalTargetField,
    MethodTable * pOptionalTargetType)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::CheckCriticalAccess(pContext,
                pOptionalTargetMethod, 
                pOptionalTargetField, 
                pOptionalTargetType);
}

#ifndef DACCESS_COMPILE
inline BOOL Security::CanHaveRVA(Assembly * pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    return Security::CanSkipVerification(pAssembly->GetDomainAssembly());
}

inline BOOL Security::CanSkipVerification(MethodDesc * pMD) 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Special case the System.Object..ctor:
    // System.Object..ctor is not verifiable according to current verifier rules (that require to call the base 
    // class ctor). But since we want System.Object..ctor() to be marked transparent, it cannot be unverifiable
    // (v4 security rules prohibit transparent code from being unverifiable)

#ifndef DACCESS_COMPILE
    if (g_pObjectCtorMD == pMD)
        return TRUE;
#endif

    // In AppX, all dynamic code (dynamic assemblies and dynamic methods) should be verified..
    if (AppX::IsAppXProcess() && !AppX::IsAppXDesignMode())
    {
        if (pMD->IsLCGMethod() || pMD->GetAssembly()->IsDynamic())
            return FALSE;
    }

    BOOL fCanSkipVerification = Security::CanSkipVerification(pMD->GetAssembly()->GetDomainAssembly()); 
    if (fCanSkipVerification)
    {
#ifdef FEATURE_CORECLR
        //For Profile assemblies, do not verify any code.  All Transparent methods are guaranteed to be
        //verifiable (verified by tests).  Therefore, skip all verification on platform assemblies.
        if(pMD->GetAssembly()->GetDomainAssembly()->GetFile()->IsProfileAssembly())
            return TRUE;
#endif
        // check for transparency
        if (SecurityTransparent::IsMethodTransparent(pMD))
        {
#ifndef FEATURE_CORECLR
            ModuleSecurityDescriptor *pModuleSecDesc = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pMD->GetAssembly());
            if (!pModuleSecDesc->CanTransparentCodeSkipVerification())
#endif // !FEATURE_CORECLR
            {
                return FALSE;
            }
        }
    }
#if defined(_DEBUG) && defined(FEATURE_CORECLR)
    else
    {
        //Profile assemblies must have skip verification.
        _ASSERTE(!pMD->GetAssembly()->GetDomainAssembly()->GetFile()->IsProfileAssembly());
    }
#endif //_DEBUG && FEATURE_CORECLR
   return fCanSkipVerification;
}
#endif //!DACCESS_COMPILE


inline BOOL Security::CanSkipVerification(DomainAssembly * pAssembly)
{
    WRAPPER_NO_CONTRACT;
    return SecurityPolicy::CanSkipVerification(pAssembly);
}

inline CorInfoCanSkipVerificationResult Security::JITCanSkipVerification(DomainAssembly * pAssembly) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::JITCanSkipVerification(pAssembly);
}

inline CorInfoCanSkipVerificationResult Security::JITCanSkipVerification(MethodDesc * pMD) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::JITCanSkipVerification(pMD);
}

inline BOOL Security::ContainsBuiltinCASPermsOnly(CORSEC_ATTRSET* pAttrSet) 
{ 
    WRAPPER_NO_CONTRACT; 
    return SecurityAttributes::ContainsBuiltinCASPermsOnly(pAttrSet); 
}

#ifdef FEATURE_APTCA
inline BOOL Security::IsUntrustedCallerCheckNeeded(MethodDesc *pCalleeMD, Assembly *pCallerAssem)
{ 
    WRAPPER_NO_CONTRACT; 
    return SecurityDeclarative::IsUntrustedCallerCheckNeeded(pCalleeMD, pCallerAssem); 
}
 
inline void Security::DoUntrustedCallerChecks(Assembly *pCaller, MethodDesc *pCalee, BOOL fFullStackWalk) 
{
    WRAPPER_NO_CONTRACT;
    SecurityDeclarative::DoUntrustedCallerChecks(pCaller, pCalee, fFullStackWalk); 
}

inline bool Security::NativeImageHasValidAptcaDependencies(PEImage *pNativeImage, DomainAssembly *pDomainAssembly)
{
    WRAPPER_NO_CONTRACT;
    return ::NativeImageHasValidAptcaDependencies(pNativeImage, pDomainAssembly);
}

inline SString Security::GetAptcaKillBitAccessExceptionContext(Assembly *pTargetAssembly)
{
    WRAPPER_NO_CONTRACT;
    return ::GetAptcaKillBitAccessExceptionContext(pTargetAssembly);
}

inline SString Security::GetConditionalAptcaAccessExceptionContext(Assembly *pTargetAssembly)
{
    WRAPPER_NO_CONTRACT;
    return ::GetConditionalAptcaAccessExceptionContext(pTargetAssembly);
}

#endif // FEATURE_APTCA

#ifdef FEATURE_CORECLR
#ifndef DACCESS_COMPILE

inline BOOL Security::IsMicrosoftPlatform(IAssemblySecurityDescriptor *pSecDesc)
{
    WRAPPER_NO_CONTRACT;
    return static_cast<AssemblySecurityDescriptor*>(pSecDesc)->IsMicrosoftPlatform();
}
#endif // DACCESS_COMPILE
#endif // FEATURE_CORECLR


inline bool Security::SecurityCalloutQuickCheck(MethodDesc *pCallerMD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::SecurityCalloutQuickCheck(pCallerMD);
}

inline bool Security::CanShareAssembly(DomainAssembly *pAssembly)
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_APTCA
    if (!DomainCanShareAptcaAssembly(pAssembly))
    {
        return false;
    }
#endif // FEATURE_APTCA

    return true;
}

inline HRESULT Security::GetDeclaredPermissions(IN IMDInternalImport *pInternalImport, IN mdToken token, IN CorDeclSecurity action, OUT OBJECTREF *pDeclaredPermissions, OUT PsetCacheEntry **pPSCacheEntry ) 
{
    WRAPPER_NO_CONTRACT;
    return SecurityAttributes::GetDeclaredPermissions(pInternalImport, token, action, pDeclaredPermissions, pPSCacheEntry); 
}

#ifndef DACCESS_COMPILE
  // Returns true if everyone is fully trusted or has the indicated flags
FORCEINLINE BOOL SecurityStackWalk::HasFlagsOrFullyTrustedIgnoreMode (DWORD flags) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifndef FEATURE_CAS_POLICY
    return TRUE;
#else
    // either the desired flag (often 0) or fully trusted will do
    flags |= (1<<SECURITY_FULL_TRUST);
  
    // in order for us to use the threadwide state it has to be the case that there have been no
    // overrides since the evaluation (e.g. no denies)  We keep the state up-to-date by updating 
    // it whenever a new AppDomainStackEntry is pushed on the AppDomainStack attached to the thread.
    // When we evaluate the demand, we always intersect the current thread state with the AppDomain
    // wide flags, which are updated anytime a new Assembly is loaded into that domain.
    //
    // note if the flag is clear we still might be able to satisfy the demand if we do the full 
    // stackwalk.
    //
    // this code is very perf sensitive, do not make changes here without running 
    // a lot of interop and declarative security benchmarks
    //
    // it's important that we be able to do these checks without having to touch objects
    // other than the thread itself -- that's where a big part of the speed comes from
    // L1 cache misses are at a premium on this code path -- never mind L2...
    // main memory is right out :)
  
    Thread* pThread = GetThread();
    return ((pThread->GetOverridesCount() == 0) &&
            pThread->CheckThreadWideSpecialFlag(flags) &&
            static_cast<ApplicationSecurityDescriptor*>(pThread->GetDomain()->GetSecurityDescriptor())->CheckDomainWideSpecialFlag(flags));
#endif
}

// Returns true if everyone is fully trusted or has the indicated flags AND we're not in legacy CAS mode
FORCEINLINE BOOL SecurityStackWalk::HasFlagsOrFullyTrusted (DWORD flags) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    return (HasFlagsOrFullyTrustedIgnoreMode(flags));
  
}
  
FORCEINLINE BOOL SecurityStackWalk::QuickCheckForAllDemands(DWORD flags)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;
  
    return (SecurityStackWalk::HasFlagsOrFullyTrusted(flags));
}

inline void StoreObjectInLazyHandle(LOADERHANDLE& handle, OBJECTREF ref, LoaderAllocator* la)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (handle == NULL) 
    {
        // Storing NULL doesn't require us to allocate a handle
        if (ref != NULL)
        {
            GCPROTECT_BEGIN(ref);
            // Atomically create a handle and store it
            LOADERHANDLE tmpHandle = la->AllocateHandle(NULL);
            if (FastInterlockCompareExchangePointer(&handle, tmpHandle, static_cast<LOADERHANDLE>(NULL)) != NULL) 
            {
                // Another thread snuck in and created the handle - this should be unusual and acceptable to leak here. (Only leaks till end of AppDomain or Assembly lifetime)
            }
            else
            {
                la->SetHandleValue(handle, ref);
            }
            GCPROTECT_END();
        }
    }
    else
    {
        la->SetHandleValue(handle, ref);
    }
}
#endif // #ifndef DACCESS_COMPILE


#endif


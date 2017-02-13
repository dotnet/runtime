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
// ----------------------------------------
// SecurityPolicy
// ----------------------------------------


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
}

inline void Security::LinktimeCheckMethod(Assembly *pCaller, MethodDesc *pCallee) 
{
    WRAPPER_NO_CONTRACT; 
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

inline void Security::DoDeclarativeActions(MethodDesc *pMD, DeclActionInfo *pActions, LPVOID pSecObj, MethodSecurityDescriptor *pMSD) 
{
    WRAPPER_NO_CONTRACT; 
}

#ifndef DACCESS_COMPILE
inline void Security::CheckNonCasDemand(OBJECTREF *prefDemand) 
{
    WRAPPER_NO_CONTRACT;
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

// ----------------------------------------
// SecurityStackWalk
// ----------------------------------------

// other CAS Actions
inline void Security::Demand(SecurityStackWalkType eType, OBJECTREF demand) 
{
    WRAPPER_NO_CONTRACT;
}


inline void Security::DemandSet(SecurityStackWalkType eType, OBJECTREF demand) 
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
}

inline void Security::DemandSet(SecurityStackWalkType eType, PsetCacheEntry *pPCE, DWORD dwAction) 
{
    WRAPPER_NO_CONTRACT;
}


inline void Security::SpecialDemand(SecurityStackWalkType eType, DWORD whatPermission) 
{
    WRAPPER_NO_CONTRACT;
}

inline void Security::InheritanceLinkDemandCheck(Assembly *pTargetAssembly, MethodDesc * pMDLinkDemand)
{
    WRAPPER_NO_CONTRACT;
}

inline void Security::FullTrustInheritanceDemand(Assembly *pTargetAssembly)
{
    WRAPPER_NO_CONTRACT;
}

inline void Security::FullTrustLinkDemand(Assembly *pTargetAssembly)
{
    WRAPPER_NO_CONTRACT;
}

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

    // Always skip verification on CoreCLR
    return TRUE;
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


inline bool Security::SecurityCalloutQuickCheck(MethodDesc *pCallerMD)
{
    WRAPPER_NO_CONTRACT;
    return SecurityTransparent::SecurityCalloutQuickCheck(pCallerMD);
}

inline bool Security::CanShareAssembly(DomainAssembly *pAssembly)
{
    WRAPPER_NO_CONTRACT;


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

    return TRUE;
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


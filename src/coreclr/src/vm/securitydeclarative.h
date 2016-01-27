// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 


#ifndef __SECURITYDECLARATIVE_H__
#define __SECURITYDECLARATIVE_H__

class SecurityStackWalk;
class MethodSecurityDescriptor;
class TokenSecurityDescriptor;
struct TokenDeclActionInfo;
class TypeSecurityDescriptor;
class PsetCacheEntry;

// Reasons why a method may have been flagged as requiring a LinkDemand
enum LinktimeCheckReason
{
    LinktimeCheckReason_None                        = 0x00000000,   // The method does not require a LinkDemand
    LinktimeCheckReason_CasDemand                   = 0x00000001,   // The method has CAS LinkDemands
    LinktimeCheckReason_NonCasDemand                = 0x00000002,   // The method has non-CAS LinkDemands
    LinktimeCheckReason_AptcaCheck                  = 0x00000004,   // The method is a member of a non-APTCA assembly that requires its caller to be trusted
    LinktimeCheckReason_NativeCodeCall              = 0x00000008    // The method may represent a call to native code
};

struct DeclActionInfo
{
    DWORD           dwDeclAction;   // This'll tell InvokeDeclarativeSecurity whats the action needed
    PsetCacheEntry *pPCE;           // The cached permissionset on which to demand/assert/deny/blah
    DeclActionInfo *pNext;          // Next declarative action needed on this method, if any.

    static DeclActionInfo *Init(MethodDesc *pMD, DWORD dwAction, PsetCacheEntry *pPCE);
};

inline LinktimeCheckReason operator|(LinktimeCheckReason lhs, LinktimeCheckReason rhs);
inline LinktimeCheckReason operator|=(LinktimeCheckReason &lhs, LinktimeCheckReason rhs);
inline LinktimeCheckReason operator&(LinktimeCheckReason lhs, LinktimeCheckReason rhs);
inline LinktimeCheckReason operator&=(LinktimeCheckReason &lhs, LinktimeCheckReason rhs);

namespace SecurityDeclarative
{
    // Returns an instance of a well-known permission.  (It caches them, so each permission is created only once.)
    void _GetSharedPermissionInstance(OBJECTREF *perm, int index);

    // Perform the declarative actions
    //   Callers:
    //     DoDeclarativeSecurity
    void DoDeclarativeActions(MethodDesc *pMD, DeclActionInfo *pActions, LPVOID pSecObj, MethodSecurityDescriptor *pMSD = NULL);
    void DoDeclarativeStackModifiers(MethodDesc *pMeth, AppDomain* pAppDomain, LPVOID pSecObj);
    void DoDeclarativeStackModifiersInternal(MethodDesc *pMeth, LPVOID pSecObj);
    void EnsureAssertAllowed(MethodDesc *pMeth, MethodSecurityDescriptor* pMSD); // throws exception if assert is not allowed for MethodDesc
    // Determine which declarative SecurityActions are used on this type and return a
    // DWORD of flags to represent the results
    //   Callers:
    //     MethodTableBuilder::CreateClass
    //     MethodTableBuilder::EnumerateClassMembers
    //     MethodDesc::GetSecurityFlags
    HRESULT GetDeclarationFlags(IMDInternalImport *pInternalImport, mdToken token, DWORD* pdwFlags, DWORD* pdwNullFlags, BOOL* fHasSuppressUnmanagedCodeAccessAttr = NULL);

    // Query the metadata to get all LinkDemands on this method (and it's class)
    //   Callers:
    //     CanAccess (ReflectionInvocation)
    //     ReflectionInvocation::GetSpecialSecurityFlags
    //     RuntimeMethodHandle::InvokeMethod_Internal
    //     Security::CheckLinkDemandAgainstAppDomain
    void RetrieveLinktimeDemands(MethodDesc* pMD,
                                        OBJECTREF* pClassCas,
                                        OBJECTREF* pClassNonCas,
                                        OBJECTREF* pMethodCas,
                                        OBJECTREF* pMethodNonCas);

    // Determine why the method is marked as requiring a linktime check, optionally returning the declared
    // CAS link demands on the method itself.
    LinktimeCheckReason GetLinktimeCheckReason(MethodDesc *pMD,
                                                      OBJECTREF  *pClassCasDemands,
                                                      OBJECTREF  *pClassNonCasDemands,
                                                      OBJECTREF  *pMethodCasDemands,
                                                      OBJECTREF  *pMethodNonCasDemands);

    // Used by interop to simulate the effect of link demands when the caller is
    // in fact script constrained by an appdomain setup by IE.
    //   Callers:
    //     DispatchInfo::InvokeMember
    //     COMToCLRWorkerBody (COMToCLRCall)
    void CheckLinkDemandAgainstAppDomain(MethodDesc *pMD);

    // Perform a LinkDemand
    //   Callers:
    //     COMCustomAttribute::CreateCAObject
    //     CheckMethodAccess
    //     InvokeUtil::CheckLinktimeDemand
    //     CEEInfo::findMethod
    //     RuntimeMethodHandle::InvokeMethod_Internal
    void LinktimeCheckMethod(Assembly *pCaller, MethodDesc *pCallee); 

    // Perform inheritance link demand
    //  Called by:
    //     MethodTableBuilder::ConvertLinkDemandToInheritanceDemand
    void InheritanceLinkDemandCheck(Assembly *pTargetAssembly, MethodDesc * pMDLinkDemand);

    // Perform an InheritanceDemand against the target assembly
    void InheritanceDemand(Assembly *pTargetAssembly, OBJECTREF refDemand);

    // Perform a FullTrust InheritanceDemand against the target assembly
    void FullTrustInheritanceDemand(Assembly *pTargetAssembly);

    // Perform a FullTrust LinkDemand against the target assembly
    void FullTrustLinkDemand(Assembly *pTargetAssembly);

    // Do InheritanceDemands on the type
    //   Called by:
    //     MethodTableBuilder::VerifyInheritanceSecurity
    void ClassInheritanceCheck(MethodTable *pClass, MethodTable *pParent);

    // Do InheritanceDemands on the Method
    //   Callers:
    //     MethodTableBuilder::VerifyInheritanceSecurity
    void MethodInheritanceCheck(MethodDesc *pMethod, MethodDesc *pParent);

    // Returns a managed instance of a well-known PermissionSet
    //   Callers:
    //     COMCodeAccessSecurityEngine::SpecialDemand
    //     ReflectionSerialization::GetSafeUninitializedObject
    inline void GetPermissionInstance(OBJECTREF *perm, int index);

    inline BOOL FullTrustCheckForLinkOrInheritanceDemand(Assembly *pAssembly);


#ifdef FEATURE_APTCA
    // Returns TRUE if an APTCA check is necessary
    //   Callers:
    //     CanAccess
    BOOL IsUntrustedCallerCheckNeeded(MethodDesc *pCalleeMD, Assembly *pCallerAssem = NULL);

    // Perform the APTCA check
    //   Callers:
    //     CanAccess
    //     Security::CheckLinkDemandAgainstAppDomain
    void DoUntrustedCallerChecks(
        Assembly *pCaller, MethodDesc *pCalee,
        BOOL fFullStackWalk);
#endif // FEATURE_APTCA

#ifndef DACCESS_COMPILE
    // Calls PermissionSet.Demand
    //   Callers:
    //     CanAccess (ReflectionInvocation)
    //     Security::CheckLinkDemandAgainstAppDomain
    void CheckNonCasDemand(OBJECTREF *prefDemand);
#endif // #ifndef DACCESS_COMPILE

    // Returns TRUE if the method is visible outside its assembly
    //   Callers:
    //     MethodTableBuilder::SetSecurityFlagsOnMethod
    inline BOOL MethodIsVisibleOutsideItsAssembly(MethodDesc * pMD);
    inline BOOL MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr, DWORD dwClassAttr, BOOL fIsGlobalClass);

    BOOL TokenMightHaveDeclarations(IMDInternalImport *pInternalImport, mdToken token, CorDeclSecurity action);
    DeclActionInfo *DetectDeclActions(MethodDesc *pMeth, DWORD dwDeclFlags);
    void DetectDeclActionsOnToken(mdToken tk, DWORD dwDeclFlags, PsetCacheEntry** pSets, IMDInternalImport *pInternalImport);
    void InvokeLinktimeChecks(Assembly *pCaller,
                                     Module *pModule,
                                     mdToken token);

    inline BOOL MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr);

    inline BOOL ClassIsVisibleOutsideItsAssembly(DWORD dwClassAttr, BOOL fIsGlobalClass);

#ifdef FEATURE_APTCA
    // Returns an instance of a SecurityException with the message "This method doesn't allow partially trusted callers"
    //   Callers:
    //     DoUntrustedCallerChecks
    void DECLSPEC_NORETURN ThrowAPTCAException(Assembly *pCaller, MethodDesc *pCallee);
#endif // FEATURE_APTCA
#ifdef FEATURE_CAS_POLICY
    void DECLSPEC_NORETURN ThrowHPException(EApiCategories protectedCategories, EApiCategories demandedCategories);
#endif // FEATURE_CAS_POLICY

    // Add a declarative action and PermissionSet index to the linked list
    void AddDeclAction(CorDeclSecurity action, PsetCacheEntry *pClassPCE, PsetCacheEntry *pMethodPCE, DeclActionInfo** ppActionList, MethodDesc *pMeth);

    // Helper for DoDeclarativeActions
    void InvokeDeclarativeActions(MethodDesc *pMeth, DeclActionInfo *pActions, MethodSecurityDescriptor *pMSD);
    void InvokeDeclarativeStackModifiers (MethodDesc *pMeth, DeclActionInfo *pActions, OBJECTREF * pSecObj);

    bool BlobMightContainNonCasPermission(PBYTE pbPerm, ULONG cbPerm, DWORD dwAction, bool* pHostProtectionOnly);    
    
// Delayed Declarative Security processing
#ifndef DACCESS_COMPILE
    inline void DoDeclarativeSecurityAtStackWalk(MethodDesc* pFunc, AppDomain* pAppDomain, OBJECTREF* pFrameObjectSlot);
#endif
}
    
#endif // __SECURITYDECLARATIVE_H__


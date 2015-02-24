//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 


// 


#ifndef __security_h__
#define __security_h__

#include "securitypolicy.h"
#include "securityattributes.h"
#include "securitydeclarativecache.h"
#include "securitydeclarative.h"
#include "securityimperative.h"
#include "securitytransparentassembly.h"

#ifdef FEATURE_APTCA
#include "aptca.h"
#endif

class IAssemblySecurityDescriptor;
class IApplicationSecurityDescriptor;
class IPEFileSecurityDescriptor;

enum SecurityStackWalkType
{
    SSWT_DECLARATIVE_DEMAND = 1,
    SSWT_IMPERATIVE_DEMAND = 2,
    SSWT_DEMAND_FROM_NATIVE = 3,
    SSWT_IMPERATIVE_ASSERT = 4,
    SSWT_DENY_OR_PERMITONLY = 5,
    SSWT_LATEBOUND_LINKDEMAND = 6,
    SSWT_COUNT_OVERRIDES = 7,
    SSWT_GET_ZONE_AND_URL = 8,
};

// AssemblyLoadSecurity is used to describe to the loader security information to apply to an assembly at
// load time.  This includes information such as the assembly's evidence, as well as if we should resolve
// policy on the assembly or push a grant set to its security descriptor.
struct AssemblyLoadSecurity
{
    OBJECTREF   *m_pEvidence;
    OBJECTREF   *m_pAdditionalEvidence;
    OBJECTREF   *m_pGrantSet;
    OBJECTREF   *m_pRefusedSet;
    DWORD       m_dwSpecialFlags;
    bool        m_fCheckLoadFromRemoteSource;
    bool        m_fSuppressSecurityChecks;
    bool        m_fPropagatingAnonymouslyHostedDynamicMethodGrant;

    inline AssemblyLoadSecurity();

    // Should the assembly have policy resolved on it, or should it use a pre-determined grant set
    inline bool ShouldResolvePolicy();
};

// Ultimately this will become the only interface through
// which the VM will access security code.

namespace Security
{
    // ----------------------------------------
    // SecurityPolicy
    // ----------------------------------------

    // Init
    inline void Start();
    inline void Stop();
    inline void SaveCache();

    // Policy
#ifdef FEATURE_CAS_POLICY
    inline bool IsProcessWideLegacyCasPolicyEnabled();
    inline bool CanLoadFromRemoteSources();
#endif // FEATURE_CAS_POLICY

    BOOL BypassSecurityChecksForProfiler(MethodDesc *pMD);
    inline BOOL CanCallUnmanagedCode(Module *pModule);
    inline BOOL CanAssert(Module *pModule);
    inline DECLSPEC_NORETURN void ThrowSecurityException(__in_z const char *szDemandClass, DWORD dwFlags);

#ifndef DACCESS_COMPILE
    inline BOOL CanTailCall(MethodDesc* pMD);
    inline BOOL CanHaveRVA(Assembly * pAssembly);
    inline BOOL CanAccessNonVerifiableExplicitField(MethodDesc* pMD);
    inline BOOL CanSkipVerification(MethodDesc * pMethod);
#endif

    inline BOOL CanSkipVerification(DomainAssembly * pAssembly);
    inline CorInfoCanSkipVerificationResult JITCanSkipVerification(DomainAssembly * pAssembly);
    inline CorInfoCanSkipVerificationResult JITCanSkipVerification(MethodDesc * pMD);

    // ----------------------------------------
    // SecurityAttributes
    // ----------------------------------------

    inline OBJECTREF CreatePermissionSet(BOOL fTrusted);
    inline void CopyByteArrayToEncoding(IN U1ARRAYREF* pArray, OUT PBYTE* pbData, OUT DWORD* cbData);
    inline void CopyEncodingToByteArray(IN PBYTE   pbData, IN DWORD   cbData, IN OBJECTREF* pArray);    

    // ----------------------------------------
    // SecurityDeclarative
    // ----------------------------------------
    inline HRESULT GetDeclarationFlags(IMDInternalImport *pInternalImport, mdToken token, DWORD* pdwFlags, DWORD* pdwNullFlags, BOOL* fHasSuppressUnmanagedCodeAccessAttr = NULL);
    inline void RetrieveLinktimeDemands(MethodDesc* pMD, OBJECTREF* pClassCas, OBJECTREF* pClassNonCas, OBJECTREF* pMethodCas, OBJECTREF* pMethodNonCas);
    inline void CheckLinkDemandAgainstAppDomain(MethodDesc *pMD) ;

    inline LinktimeCheckReason GetLinktimeCheckReason(MethodDesc *pMD,
                                                      OBJECTREF  *pClassCasDemands,
                                                      OBJECTREF  *pClassNonCasDemands,
                                                      OBJECTREF  *pMethodCasDemands,
                                                      OBJECTREF  *pMethodNonCasDemands);

    inline void LinktimeCheckMethod(Assembly *pCaller, MethodDesc *pCallee);
    inline void ClassInheritanceCheck(MethodTable *pClass, MethodTable *pParent);
    inline void MethodInheritanceCheck(MethodDesc *pMethod, MethodDesc *pParent);
    inline void GetPermissionInstance(OBJECTREF *perm, int index);
    inline void DoDeclarativeActions(MethodDesc *pMD, DeclActionInfo *pActions, LPVOID pSecObj, MethodSecurityDescriptor *pMSD = NULL);
#ifndef DACCESS_COMPILE
    inline void CheckNonCasDemand(OBJECTREF *prefDemand);
#endif // #ifndef DACCESS_COMPILE
    inline BOOL MethodIsVisibleOutsideItsAssembly(MethodDesc * pMD);
    inline BOOL MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr, DWORD dwClassAttr, BOOL fIsGlobalClass);
    inline void CheckBeforeAllocConsole(AppDomain* pDomain, Assembly* pAssembly);

    // ----------------------------------------
    // SecurityStackWalk
    // ----------------------------------------

    // other CAS Actions
    inline void Demand(SecurityStackWalkType eType, OBJECTREF demand) ;
#ifdef FEATURE_CAS_POLICY
    inline void DemandGrantSet(IAssemblySecurityDescriptor *psdAssembly);
#endif // FEATURE_CAS_POLICY
    inline void DemandSet(SecurityStackWalkType eType, OBJECTREF demand) ;
    inline void DemandSet(SecurityStackWalkType eType, PsetCacheEntry *pPCE, DWORD dwAction) ;
#ifdef FEATURE_CAS_POLICY
    inline void ReflectionTargetDemand(DWORD dwPermission, IAssemblySecurityDescriptor *psdTarget);
    inline void ReflectionTargetDemand(DWORD dwPermission,
                                       IAssemblySecurityDescriptor *psdTarget,
                                       DynamicResolver * pAccessContext);
#endif // FEATURE_CAS_POLICY
    inline void SpecialDemand(SecurityStackWalkType eType, DWORD whatPermission) ;

    inline void InheritanceLinkDemandCheck(Assembly *pTargetAssembly, MethodDesc * pMDLinkDemand);

    inline void FullTrustInheritanceDemand(Assembly *pTargetAssembly);
    inline void FullTrustLinkDemand(Assembly *pTargetAssembly);

    // Compressed Stack
#ifdef FEATURE_COMPRESSEDSTACK
    inline COMPRESSEDSTACKREF GetCSFromContextTransitionFrame(Frame *pFrame) ;
    inline BOOL IsContextTransitionFrameWithCS(Frame *pFrame);
#endif // #ifdef FEATURE_COMPRESSEDSTACK

    // Misc - todo: put these in better categories
    
    inline BOOL AllDomainsOnStackFullyTrusted();
    IApplicationSecurityDescriptor* CreateApplicationSecurityDescriptor(AppDomain * pDomain);
    IAssemblySecurityDescriptor* CreateAssemblySecurityDescriptor(AppDomain *pDomain, DomainAssembly *pAssembly, LoaderAllocator *pLoaderAllocator);
    ISharedSecurityDescriptor* CreateSharedSecurityDescriptor(Assembly* pAssembly);
#ifndef FEATURE_CORECLR
    IPEFileSecurityDescriptor* CreatePEFileSecurityDescriptor(AppDomain* pDomain, PEFile *pPEFile);
#endif
    inline void SetDefaultAppDomainProperty(IApplicationSecurityDescriptor* pASD);
    inline void SetDefaultAppDomainEvidenceProperty(IApplicationSecurityDescriptor* pASD);

    
    // Checks for one of the special domain wide flags 
    // such as if we are currently in a "fully trusted" environment
    // or if unmanaged code access is allowed at this time
    // Note: This is an inline method instead of a virtual method on IApplicationSecurityDescriptor
    // for stackwalk perf.
    inline BOOL CheckDomainWideSpecialFlag(IApplicationSecurityDescriptor *pASD, DWORD flags);

    inline BOOL IsResolved(Assembly *pAssembly);

    FORCEINLINE VOID IncrementSecurityPerfCounter() ;
    inline BOOL IsSpecialRunFrame(MethodDesc *pMeth) ;
    inline BOOL SkipAndFindFunctionInfo(INT32 i, MethodDesc** ppMD, OBJECTREF** ppOR, AppDomain **ppAppDomain = NULL);
    inline BOOL SkipAndFindFunctionInfo(StackCrawlMark* pSCM, MethodDesc** ppMD, OBJECTREF** ppOR, AppDomain **ppAppDomain = NULL);

    // Transparency checks
    inline BOOL IsMethodTransparent(MethodDesc * pMD);
    inline BOOL IsMethodCritical(MethodDesc * pMD);
    inline BOOL IsMethodSafeCritical(MethodDesc * pMD);

    inline BOOL IsTypeCritical(MethodTable *pMT);
    inline BOOL IsTypeSafeCritical(MethodTable *pMT);
    inline BOOL IsTypeTransparent(MethodTable * pMT);
    inline BOOL IsTypeAllTransparent(MethodTable * pMT);

    inline BOOL IsFieldTransparent(FieldDesc * pFD);
    inline BOOL IsFieldCritical(FieldDesc * pFD);
    inline BOOL IsFieldSafeCritical(FieldDesc * pFD);

    inline BOOL IsTokenTransparent(Module* pModule, mdToken token);
    
    inline void DoSecurityClassAccessChecks(MethodDesc *pCallerMD,
                                                   const TypeHandle &calleeTH,
                                                   CorInfoSecurityRuntimeChecks check);

    inline CorInfoIsAccessAllowedResult RequiresTransparentAssemblyChecks(MethodDesc* pCaller,
                                                                          MethodDesc* pCallee,
                                                                          SecurityTransparencyError *pError);
    inline VOID EnforceTransparentAssemblyChecks(MethodDesc* pCallee, MethodDesc* pCaller);
    inline VOID EnforceTransparentDelegateChecks(MethodTable* pDelegateMT, MethodDesc* pCaller);
    inline VOID PerformTransparencyChecksForLoadByteArray(MethodDesc* pCallersMD, IAssemblySecurityDescriptor* pLoadedSecDesc);

    inline bool TypeRequiresTransparencyCheck(TypeHandle type, bool checkForLinkDemands = false);

    inline BOOL CheckCriticalAccess(AccessCheckContext* pContext,
        MethodDesc* pOptionalTargetMethod = NULL,
        FieldDesc* pOptionalTargetField = NULL,
        MethodTable * pOptionalTargetType = NULL);

    // declarative security
    inline HRESULT GetDeclaredPermissions(IN IMDInternalImport *pInternalImport, IN mdToken token, IN CorDeclSecurity action, OUT OBJECTREF *pDeclaredPermissions, OUT PsetCacheEntry **pPSCacheEntry = NULL) ;

    // security enforcement
    inline BOOL ContainsBuiltinCASPermsOnly(CORSEC_ATTRSET* pAttrSet);

#ifdef FEATURE_APTCA
    inline BOOL IsUntrustedCallerCheckNeeded(MethodDesc *pCalleeMD, Assembly *pCallerAssem = NULL) ;
    inline void DoUntrustedCallerChecks(Assembly *pCaller, MethodDesc *pCalee, BOOL fFullStackWalk) ;

    inline bool NativeImageHasValidAptcaDependencies(PEImage *pNativeImage, DomainAssembly *pDomainAssembly);

    inline SString GetAptcaKillBitAccessExceptionContext(Assembly *pTargetAssembly);
    inline SString GetConditionalAptcaAccessExceptionContext(Assembly *pTargetAssembly);
#endif // FEATURE_APTCA

#ifdef FEATURE_CORECLR
	inline BOOL IsMicrosoftPlatform(IAssemblySecurityDescriptor *pSecDesc);
#endif // FEATURE_CORECLR

    inline bool SecurityCalloutQuickCheck(MethodDesc *pCallerMD);

    inline bool CanShareAssembly(DomainAssembly *pAssembly);
};

class ISecurityDescriptor
{
public:
#ifndef FEATURE_PAL
    VPTR_BASE_VTABLE_CLASS(ISecurityDescriptor)
#endif
    virtual ~ISecurityDescriptor() { LIMITED_METHOD_CONTRACT; }

    virtual BOOL IsFullyTrusted() = 0;
    
    virtual BOOL CanCallUnmanagedCode() const = 0;

#ifndef DACCESS_COMPILE
    virtual DWORD GetSpecialFlags() const = 0;
    
    virtual AppDomain* GetDomain() const = 0;

    virtual void Resolve() = 0;
    virtual BOOL IsResolved() const = 0;

#ifdef FEATURE_CAS_POLICY
    virtual OBJECTREF GetEvidence() = 0;
    virtual BOOL IsEvidenceComputed() const = 0;
    virtual void SetEvidence(OBJECTREF evidence) = 0;
#endif // FEATURE_CAS_POLICY

    virtual OBJECTREF GetGrantedPermissionSet(OBJECTREF* RefusedPermissions = NULL) = 0;
#endif // !DACCESS_COMPILE
};

class IApplicationSecurityDescriptor : public ISecurityDescriptor
{
public:
#ifndef FEATURE_PAL
    VPTR_ABSTRACT_VTABLE_CLASS(IApplicationSecurityDescriptor, ISecurityDescriptor)
#endif

#ifndef DACCESS_COMPILE
public:
    virtual BOOL IsHomogeneous() const = 0;
    virtual void SetHomogeneousFlag(BOOL fRuntimeSuppliedHomogenousGrantSet) = 0;
    virtual BOOL ContainsAnyRefusedPermissions() = 0;

    virtual BOOL IsDefaultAppDomain() const = 0;
    virtual BOOL IsDefaultAppDomainEvidence() = 0;
    virtual BOOL DomainMayContainPartialTrustCode() = 0;

    virtual BOOL CallHostSecurityManager() = 0;
    virtual void SetHostSecurityManagerFlags(DWORD dwFlags) = 0;
    virtual void SetPolicyLevelFlag() = 0;
    
    virtual void FinishInitialization() = 0;
    virtual BOOL IsInitializationInProgress() = 0;

    // Determine the security state that an AppDomain will arrive in if nothing changes during domain
    // initialization.  (ie, get the input security state of the domain)
    virtual void PreResolve(BOOL *pfIsFullyTrusted, BOOL *pfIsHomogeneous) = 0;
    
    // Gets special domain wide flags that specify things 
    // such as whether we are currently in a "fully trusted" environment
    // or if unmanaged code access is allowed at this time
    virtual DWORD GetDomainWideSpecialFlag() const = 0;

#ifdef FEATURE_CAS_POLICY
    virtual void SetLegacyCasPolicyEnabled() = 0;
    virtual BOOL IsLegacyCasPolicyEnabled() = 0;
    virtual BOOL AllowsLoadsFromRemoteSources() = 0;
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_APTCA
    virtual ConditionalAptcaCache *GetConditionalAptcaCache() = 0;
    virtual void SetCanonicalConditionalAptcaList(LPCWSTR wszCanonicalConditionalAptcaList) = 0;
#endif // FEATURE_APTCA
#endif // !DACCESS_COMPILE
};

class IAssemblySecurityDescriptor : public ISecurityDescriptor
{
public:
#ifndef FEATURE_PAL
    VPTR_ABSTRACT_VTABLE_CLASS(IAssemblySecurityDescriptor, ISecurityDescriptor)
#endif

#ifndef DACCESS_COMPILE
    virtual SharedSecurityDescriptor *GetSharedSecDesc() = 0;
    
    virtual BOOL CanAssert() = 0;
    virtual BOOL HasUnrestrictedUIPermission() = 0;
    virtual BOOL IsAllCritical() = 0;
    virtual BOOL IsAllSafeCritical() = 0;
    virtual BOOL IsAllPublicAreaSafeCritical() = 0;
    virtual BOOL IsAllTransparent() = 0;
    virtual BOOL IsSystem() = 0;
    virtual BOOL AllowSkipVerificationInFullTrust() = 0;

    virtual void ResolvePolicy(ISharedSecurityDescriptor *pSharedDesc, BOOL fShouldSkipPolicyResolution) = 0;

#ifdef FEATURE_CAS_POLICY
    virtual HRESULT LoadSignature( COR_TRUST **ppSignature = NULL) = 0;

    virtual void SetRequestedPermissionSet(OBJECTREF RequiredPermissionSet, OBJECTREF OptionalPermissionSet, OBJECTREF DeniedPermissionSet) = 0;

    virtual void SetAdditionalEvidence(OBJECTREF evidence) = 0;
    virtual BOOL HasAdditionalEvidence() = 0;
    virtual OBJECTREF GetAdditionalEvidence()  = 0;
    virtual void SetEvidenceFromPEFile(IPEFileSecurityDescriptor *pPEFileSecDesc) = 0;
#endif // FEATURE_CAS_POLICY

    virtual void PropagatePermissionSet(OBJECTREF GrantedPermissionSet, OBJECTREF DeniedPermissionSet, DWORD dwSpecialFlags) = 0;

#ifndef FEATURE_CORECLR 
    virtual BOOL AllowApplicationSpecifiedAppDomainManager() = 0;
#endif

    // Check to make sure that security will allow this assembly to load.  Throw an exception if the
    // assembly should be forbidden from loading for security related purposes
    virtual void CheckAllowAssemblyLoad() = 0;
#endif // #ifndef DACCESS_COMPILE
};

class ISharedSecurityDescriptor
{
public:
    virtual void Resolve(IAssemblySecurityDescriptor *pSecDesc = NULL) = 0;
    virtual BOOL IsResolved() const = 0;
    virtual BOOL IsSystem() = 0;
    virtual Assembly* GetAssembly() = 0;
};

#ifndef FEATURE_CORECLR
class IPEFileSecurityDescriptor : public ISecurityDescriptor
{
public:
    virtual BOOL AllowBindingRedirects() = 0;
};
#endif

#include "security.inl"
#include "securitydeclarative.inl"
#include "securityattributes.inl"

#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// securitymeta.h
//
// pre-computes various security information, declarative and runtime meta-info
//


// 
//--------------------------------------------------------------------------


#ifndef __SECURITYMETA_H__
#define __SECURITYMETA_H__

class SecurityStackWalk;
class AssertStackWalk;
class PsetCacheEntry;
class SecurityTransparencyBehavior;
struct DeclActionInfo;

#define INVALID_SET_INDEX ((DWORD)~0)

// The enum that describes the value of the SecurityCriticalFlags in SecurityCritical attribute.
enum SecurityCriticalFlags
{
    SecurityCriticalFlags_None = 0,
    SecurityCriticalFlags_All = 0x1
};

// Security rule sets that can be used - this enum should match the BCL SecurityRuleSet enum
enum SecurityRuleSet
{
    SecurityRuleSet_Level1  = 1,        // v2.0 rules
    SecurityRuleSet_Level2  = 2,        // v4.0 rules

    SecurityRuleSet_Min     = SecurityRuleSet_Level1,   // Smallest rule set we understand
    SecurityRuleSet_Max     = SecurityRuleSet_Level2,   // Largest rule set we understand
    SecurityRuleSet_Default = SecurityRuleSet_Level2    // Rule set to use if unspecified
};

// Partial trust visibility level for APTCA assemblies - this enum should match the BCL
// PartialTrustVisibilityLevel enum
enum PartialTrustVisibilityLevel
{
    PartialTrustVisibilityLevel_VisibleToAllHosts = 0,
    PartialTrustVisibilityLevel_NotVisibleByDefault = 1
};

SELECTANY const DWORD DCL_FLAG_MAP[] =
{
    0,                                  // dclActionNil                 = 0
    DECLSEC_REQUESTS,                   // dclRequest                   = 1
    DECLSEC_DEMANDS,                    // dclDemand                    = 2
    DECLSEC_ASSERTIONS,                 // dclAssert                    = 3
    DECLSEC_DENIALS,                    // dclDeny                      = 4
    DECLSEC_PERMITONLY,                 // dclPermitOnly                = 5
    DECLSEC_LINK_CHECKS,                // dclLinktimeCheck             = 6
    DECLSEC_INHERIT_CHECKS,             // dclInheritanceCheck          = 7
    DECLSEC_REQUESTS,                   // dclRequestMinimum            = 8
    DECLSEC_REQUESTS,                   // dclRequestOptional           = 9
    DECLSEC_REQUESTS,                   // dclRequestRefuse             = 10
    0,                                  // dclPrejitGrant               = 11
    0,                                  // dclPrejitDenied              = 12
    DECLSEC_NONCAS_DEMANDS,             // dclNonCasDemand              = 13
    DECLSEC_NONCAS_LINK_DEMANDS,        // dclNonCasLinkDemand          = 14
    DECLSEC_NONCAS_INHERITANCE,         // dclNonCasInheritance         = 15
};
#define DCL_FLAG_MAP_SIZE (sizeof(DCL_FLAG_MAP)/sizeof(DWORD))
#define  DclToFlag(dcl) (((size_t)dcl < DCL_FLAG_MAP_SIZE) ? DCL_FLAG_MAP[dcl] : 0)


struct TokenDeclActionInfo
{
    DWORD           dwDeclAction;   // This'll tell InvokeDeclarativeSecurity whats the action needed
    PsetCacheEntry *pPCE;     // The cached permissionset on which to demand/assert/deny/etc
    TokenDeclActionInfo* pNext;        // pointer to next action link in chain
    
    static TokenDeclActionInfo *Init(DWORD dwAction, PsetCacheEntry *pPCE);
    static void LinkNewDeclAction(TokenDeclActionInfo** ppActionList, CorDeclSecurity action, PsetCacheEntry *pPCE);


    HRESULT GetDeclaredPermissionsWithCache(IN CorDeclSecurity action,
                                            OUT OBJECTREF *pDeclaredPermissions,
                                            OUT PsetCacheEntry **pPCE);

    OBJECTREF GetLinktimePermissions(OBJECTREF *prefNonCasDemands);
    void InvokeLinktimeChecks(Assembly* pCaller);
};

// Flags about the raw security attributes found on a metadata token, as well as semantic interpretations of
// them in some cases (see code:TokenSecurityDescriptor#TokenSecurityDescriptorSemanticLookup).  These flags
// are split into several sections:
//
// 32              28           16              12               4          0
// | Rules version | Rules Bits | Semantic data | Raw attributes | Metabits |
//
//   Rules version  - the SecurityRuleSet selected by a SecurityRules attribute
//   Rules bits     - extra flags set on a SecurityRules attribute
//   Semantic data  - Flags indicating the security state of the item represented by the token taking into
//                    account parent types and modules - giving the true semantic security state
//                    (see code:TokenSecurityDescriptor#TokenSecurityDescriptorSemanticLookup)
//   Raw attributes - Flags for data we read directly out of metadata; these only indicate that the attributes
//                    are set, and do not indicate the actual security state of the token until they have been
//                    interpreted by the assembly they are applied within.
//   Metabits       - Flags about the state of the token security descriptor itself
enum TokenSecurityDescriptorFlags
{
    // Metabits
    TokenSecurityDescriptorFlags_None                       = 0x00000000,
    TokenSecurityDescriptorFlags_IsComputed                 = 0x00000001,

    // Raw attributes
    TokenSecurityDescriptorFlags_RawAttributeMask           = 0x00000FF0,
    TokenSecurityDescriptorFlags_AllCritical                = 0x00000010,   // [SecurityCritical(SecurityCriticalScope.All)]
    TokenSecurityDescriptorFlags_APTCA                      = 0x00000020,   // [AllowPartiallyTrustedCallers] (VisibleByDefault)
    TokenSecurityDescriptorFlags_ConditionalAPTCA           = 0x00000040,   // [AllowPartiallyTrustedCallers] (NotVisibleByDefault)
    TokenSecurityDescriptorFlags_Critical                   = 0x00000080,   // [SecurityCritical] (regardless of scope)
    TokenSecurityDescriptorFlags_SecurityRules              = 0x00000100,   // [SecurityRules]
    TokenSecurityDescriptorFlags_SafeCritical               = 0x00000200,   // [SecuritySafeCritical]
    TokenSecurityDescriptorFlags_Transparent                = 0x00000400,   // [SecurityTransparent]
    TokenSecurityDescriptorFlags_TreatAsSafe                = 0x00000800,   // [SecurityTreatAsSafe]

    // Semantic data
    TokenSecurityDescriptorFlags_SemanticMask               = 0x000FF000,
    TokenSecurityDescriptorFlags_IsSemanticComputed         = 0x00001000,
    TokenSecurityDescriptorFlags_IsSemanticCritical         = 0x00002000,
    TokenSecurityDescriptorFlags_IsSemanticTreatAsSafe      = 0x00004000,
    TokenSecurityDescriptorFlags_IsSemanticExternallyVisible= 0x00008000,

    // Rules bits
    TokenSecurityDescriptorFlags_RulesMask                  = 0x0FFF0000,
    TokenSecurityDescriptorFlags_SkipFullTrustVerification  = 0x00010000,   // In full trust do not do IL verificaiton for transparent code

    // Rules version
    TokenSecurityDescriptorFlags_RulesVersionMask           = 0xF0000000
};

inline TokenSecurityDescriptorFlags operator|(TokenSecurityDescriptorFlags lhs,
                                              TokenSecurityDescriptorFlags rhs);

inline TokenSecurityDescriptorFlags operator|=(TokenSecurityDescriptorFlags& lhs,
                                               TokenSecurityDescriptorFlags rhs);

inline TokenSecurityDescriptorFlags operator&(TokenSecurityDescriptorFlags lhs,
                                              TokenSecurityDescriptorFlags rhs);

inline TokenSecurityDescriptorFlags operator&=(TokenSecurityDescriptorFlags& lhs,
                                               TokenSecurityDescriptorFlags rhs);

inline TokenSecurityDescriptorFlags operator~(TokenSecurityDescriptorFlags flags);

// Get the version of the security rules that token security descriptor flags are requesting
inline SecurityRuleSet GetSecurityRuleSet(TokenSecurityDescriptorFlags flags);

// Encode a security rule set into token flags - this reverses GetSecurityRuleSet
inline TokenSecurityDescriptorFlags EncodeSecurityRuleSet(SecurityRuleSet ruleSet);


TokenSecurityDescriptorFlags ParseSecurityRulesAttribute(const BYTE *pbSecurityRulesBlob,
                                                         DWORD cbSecurityRulesBlob);

//
// #TokenSecurityDescriptorSemanticLookup
// 
// Token security descriptors are used to get information on the security state of a specific metadata
// token. They have two types of lookup - standard and semantic. Standard lookup is cheaper and only looks at
// the specific metadata token.  Semantic lookup will follow the token to its parents, figuring out if the
// token is semanticaly critical or transparent due to a containing item.  For instance:
// 
//     [SecurityCritical]
//     class A
//     {
//         class B { }
//     }
// 
// A TokenSecurityDescriptor's standard lookup for B will say that it is transparent because B does not
// directly have a critical attribute.  However, a semantic lookup will notice that A is critical and
// contains B, therefore B is also critical.
//

class TokenSecurityDescriptor
{
private:
    PTR_Module                      m_pModule;
    mdToken                         m_token;
    TokenSecurityDescriptorFlags    m_flags;

public:
    inline TokenSecurityDescriptor(PTR_Module pModule, mdToken token);

    void VerifyDataComputed();
    void VerifySemanticDataComputed();

    // Get the raw flags for the token
    inline TokenSecurityDescriptorFlags GetFlags();

    //
    // Critical / transparent checks for the specific metadata token only - these methods do not take into
    // account the containment of the token and therefore only include information about the token itself
    // and cannot be used to determine if the item represented by the token is semantically critical.
    // 
    // See code:TokenSecurityDescriptor#TokenSecurityDescriptorSemanticLookup
    //
    
    // Get the attributes that were set on the token
    inline TokenSecurityDescriptorFlags GetMetadataFlags();

    //
    // Semantic critical / transparent checks for the metadata token - these methods take into account
    // containers of the token to get a true semantic security status for the token.
    // 
    // See code:TokenSecurityDescriptor#TokenSecurityDescriptorSemanticLookup
    //

    inline BOOL IsSemanticCritical();

    inline BOOL IsSemanticTreatAsSafe();
    
    inline BOOL IsSemanticExternallyVisible();

    // static helper to find cached security descriptors based on token
    static HashDatum LookupSecurityDescriptor(void* pKey);

    static HashDatum LookupSecurityDescriptor_Slow(AppDomain* pDomain,
                                                   void* pKey,   
                                                   EEPtrHashTable  &rCachedMethodPermissionsHash );

    // static helper to insert a security descriptor for a token, dupes not allowed, returns previous entry in hash table
    static HashDatum InsertSecurityDescriptor(void* pKey, HashDatum pHashDatum);

    // static helper to parse the security attributes for a token from a given metadata importer
    static TokenSecurityDescriptorFlags ReadSecurityAttributes(IMDInternalImport *pmdImport, mdToken token);

private:
    // does the type represented by this TokenSecurityDescriptor particpate in type equivalence
    inline BOOL IsTypeEquivalent();

private:
    // Helper class which fires transparency calculation begin/end ETW events
    class TokenSecurityDescriptorTransparencyEtwEvents
    {
    private:
        const TokenSecurityDescriptor *m_pTSD;

    public:
        inline TokenSecurityDescriptorTransparencyEtwEvents(const TokenSecurityDescriptor *pTSD);
        inline ~TokenSecurityDescriptorTransparencyEtwEvents();
    };
};

enum MethodSecurityDescriptorFlags
{
    MethodSecurityDescriptorFlags_None                  = 0x0000,
    MethodSecurityDescriptorFlags_IsComputed            = 0x0001,

    // Method transparency info is cached directly on MethodDesc for performance reasons
    // These flags are used only during calculation of transparency information; runtime data
    // should be read from the method desc
    MethodSecurityDescriptorFlags_IsCritical            = 0x0002,
    MethodSecurityDescriptorFlags_IsTreatAsSafe         = 0x0004,

    MethodSecurityDescriptorFlags_IsBuiltInCASPermsOnly = 0x0008,
    MethodSecurityDescriptorFlags_IsDemandsOnly         = 0x0010,
    MethodSecurityDescriptorFlags_AssertAllowed         = 0x0020,
    MethodSecurityDescriptorFlags_CanCache              = 0x0040,
};

inline MethodSecurityDescriptorFlags operator|(MethodSecurityDescriptorFlags lhs,
                                               MethodSecurityDescriptorFlags rhs);

inline MethodSecurityDescriptorFlags operator|=(MethodSecurityDescriptorFlags& lhs,
                                                MethodSecurityDescriptorFlags rhs);

inline MethodSecurityDescriptorFlags operator&(MethodSecurityDescriptorFlags lhs,
                                               MethodSecurityDescriptorFlags rhs);

inline MethodSecurityDescriptorFlags operator&=(MethodSecurityDescriptorFlags& lhs,
                                                MethodSecurityDescriptorFlags rhs);

class MethodSecurityDescriptor 
{
private:
    MethodDesc                      *m_pMD;
    DeclActionInfo                  *m_pRuntimeDeclActionInfo;  // run-time declarative actions list    
    TokenDeclActionInfo             *m_pTokenDeclActionInfo;    // link-time declarative actions list
    MethodSecurityDescriptorFlags   m_flags;
    DWORD                            m_declFlagsDuringPreStub;   // declarative run-time security flags,    

public:
    explicit inline MethodSecurityDescriptor(MethodDesc* pMD, BOOL fCanCache = TRUE);

    inline BOOL CanAssert();
    inline void SetCanAssert();

    inline BOOL CanCache();
    inline void SetCanCache();
    
    inline BOOL HasRuntimeDeclarativeSecurity();
    inline BOOL HasLinkOrInheritanceDeclarativeSecurity();
    inline BOOL HasLinktimeDeclarativeSecurity();
    inline BOOL HasInheritanceDeclarativeSecurity();

    inline mdToken GetToken();
    inline MethodDesc *GetMethod();
    inline IMDInternalImport *GetIMDInternalImport();

    inline BOOL ContainsBuiltInCASDemandsOnly();
    inline DeclActionInfo* GetRuntimeDeclActionInfo();
    inline DWORD GetDeclFlagsDuringPreStub();
    inline TokenDeclActionInfo* GetTokenDeclActionInfo();

    inline BOOL IsCritical();
    inline BOOL IsTreatAsSafe();

    inline BOOL IsOpportunisticallyCritical();

    inline HRESULT GetDeclaredPermissionsWithCache(IN CorDeclSecurity action,
                                                   OUT OBJECTREF *pDeclaredPermissions,
                                                   OUT PsetCacheEntry **pPCE);

    static HRESULT GetDeclaredPermissionsWithCache(MethodDesc* pMD,
                                                   IN CorDeclSecurity action,
                                                   OUT OBJECTREF *pDeclaredPermissions,
                                                   OUT PsetCacheEntry **pPCE);
    
    static OBJECTREF GetLinktimePermissions(MethodDesc* pMD, OBJECTREF *prefNonCasDemands);

    inline void InvokeLinktimeChecks(Assembly* pCaller);
    static inline void InvokeLinktimeChecks(MethodDesc* pMD, Assembly* pCaller);

    void InvokeInheritanceChecks(MethodDesc *pMethod);

    // This method will look for the cached copy of the MethodSecurityDescriptor corresponding to ret_methSecDesc->_pMD
    // If the cache lookup succeeds, we get back the cached copy in ret_methSecDesc
    // If the cache lookup fails, then the data is computed in ret_methSecDesc. If we find that this is a cache-able MSD,
    // a copy is made in AppDomain heap and inserted into the hash table for future lookups.
    static void LookupOrCreateMethodSecurityDescriptor(MethodSecurityDescriptor* ret_methSecDesc);
    static BOOL IsDeclSecurityCASDemandsOnly(DWORD dwMethDeclFlags,
                                             mdToken _mdToken,
                                             IMDInternalImport *pInternalImport);

private:
    void ComputeRuntimeDeclarativeSecurityInfo();
    void ComputeMethodDeclarativeSecurityInfo();

    inline void VerifyDataComputed();
    void VerifyDataComputedInternal();
    
    // Force the type to figure out if it is transparent or critial.
    // NOTE: Generally this is not needed, as the data is cached on the MethodDesc for you. This method should
    // only be called if the MethodDesc is returning FALSE from HasCriticalTransparentInfo
    void ComputeCriticalTransparentInfo();

    static BOOL CanMethodSecurityDescriptorBeCached(MethodDesc* pMD);

private:
    // Helper class which fires transparency calculation begin/end ETW events
    class MethodSecurityDescriptorTransparencyEtwEvents
    {
    private:
        const MethodSecurityDescriptor *m_pMSD;

    public:
        inline MethodSecurityDescriptorTransparencyEtwEvents(const MethodSecurityDescriptor *pMSD);
        inline ~MethodSecurityDescriptorTransparencyEtwEvents();
    };

    // Helper class to iterater over methods that the MethodSecurityDescriptor's MethodDesc may be
    // implementing.  This type iterates over interface implementations followed by MethodImpls for virtuals
    // that the input MethodDesc implements.
    class MethodImplementationIterator
    {
    private:
        DispatchMap::Iterator m_interfaceIterator;
        MethodDesc *m_pMD;
        DWORD m_iMethodImplIndex;
        bool m_fInterfaceIterationBegun;
        bool m_fMethodImplIterationBegun;

    public:
        MethodImplementationIterator(MethodDesc *pMD);

        MethodDesc *Current();
        bool IsValid();
        void Next();
    };
};
            
enum FieldSecurityDescriptorFlags
{
    FieldSecurityDescriptorFlags_None                   = 0x0000,
    FieldSecurityDescriptorFlags_IsComputed             = 0x0001,
    FieldSecurityDescriptorFlags_IsCritical             = 0x0002,
    FieldSecurityDescriptorFlags_IsTreatAsSafe          = 0x0004,
};

inline FieldSecurityDescriptorFlags operator|(FieldSecurityDescriptorFlags lhs,
                                              FieldSecurityDescriptorFlags rhs);

inline FieldSecurityDescriptorFlags operator|=(FieldSecurityDescriptorFlags& lhs,
                                               FieldSecurityDescriptorFlags rhs);

inline FieldSecurityDescriptorFlags operator&(FieldSecurityDescriptorFlags lhs,
                                              FieldSecurityDescriptorFlags rhs);

inline FieldSecurityDescriptorFlags operator&=(FieldSecurityDescriptorFlags& lhs,
                                               FieldSecurityDescriptorFlags rhs);

class FieldSecurityDescriptor
{
private:
    FieldDesc                       *m_pFD;
    FieldSecurityDescriptorFlags    m_flags;

public:
    explicit inline FieldSecurityDescriptor(FieldDesc* pFD);

    void VerifyDataComputed();

    inline BOOL IsCritical();
    inline BOOL IsTreatAsSafe();

private:
    // Helper class which fires transparency calculation begin/end ETW events
    class FieldSecurityDescriptorTransparencyEtwEvents
    {
    private:
        const FieldSecurityDescriptor *m_pFSD;

    public:
        inline FieldSecurityDescriptorTransparencyEtwEvents(const FieldSecurityDescriptor *pFSD);
        inline ~FieldSecurityDescriptorTransparencyEtwEvents();
    };
};

enum TypeSecurityDescriptorFlags
{
    TypeSecurityDescriptorFlags_None                = 0x0000,

    // Type transparency info is cached directly on EEClass for performance reasons; these bits are used only
    // as intermediate state while calculating the final set of bits to cache on  the EEClass
    TypeSecurityDescriptorFlags_IsAllCritical       = 0x0001,   // Everything introduced by this type is critical
    TypeSecurityDescriptorFlags_IsAllTransparent    = 0x0002,   // All code in the type is transparent
    TypeSecurityDescriptorFlags_IsCritical          = 0x0004,   // The type is critical, but its introduced methods may not be
    TypeSecurityDescriptorFlags_IsTreatAsSafe       = 0x0008,   // Combined with IsAllCritical or IsCritical makes the type SafeCritical
};

inline TypeSecurityDescriptorFlags operator|(TypeSecurityDescriptorFlags lhs,
                                             TypeSecurityDescriptorFlags rhs);

inline TypeSecurityDescriptorFlags operator|=(TypeSecurityDescriptorFlags& lhs,
                                              TypeSecurityDescriptorFlags rhs);

inline TypeSecurityDescriptorFlags operator&(TypeSecurityDescriptorFlags lhs,
                                             TypeSecurityDescriptorFlags rhs);

inline TypeSecurityDescriptorFlags operator&=(TypeSecurityDescriptorFlags& lhs,
                                              TypeSecurityDescriptorFlags rhs);

class TypeSecurityDescriptor
{
private:
    MethodTable                 *m_pMT;
    TokenDeclActionInfo         *m_pTokenDeclActionInfo;
    BOOL                        m_fIsComputed;

public:
    explicit inline TypeSecurityDescriptor(MethodTable *pMT);

    inline BOOL HasLinkOrInheritanceDeclarativeSecurity();       
    inline BOOL HasLinktimeDeclarativeSecurity();
    inline BOOL HasInheritanceDeclarativeSecurity();

    // Is everything introduced by the type critical
    inline BOOL IsAllCritical();

    // Does the type contain only transparent code
    inline BOOL IsAllTransparent();

    // Combined with IsCritical/IsAllCritical is the type safe critical
    inline BOOL IsTreatAsSafe();

    // Is the type critical, but not necessarially its conatined methods
    inline BOOL IsCritical();

    // Is the type in an assembly that doesn't care about transparency, and therefore wants the CLR to make
    // sure that all annotations are correct for it.
    inline BOOL IsOpportunisticallyCritical();

    // Should this type be considered externally visible when calculating the transpraency of the type
    // and its members. (For instance, when seeing if public implies treat as safe)
    BOOL IsTypeExternallyVisibleForTransparency();

    inline mdToken GetToken();
    inline IMDInternalImport *GetIMDInternalImport();

    inline TokenDeclActionInfo* GetTokenDeclActionInfo();

    inline HRESULT GetDeclaredPermissionsWithCache(IN CorDeclSecurity action,
                                                   OUT OBJECTREF *pDeclaredPermissions,
                                                   OUT PsetCacheEntry **pPCE);

    static HRESULT GetDeclaredPermissionsWithCache(MethodTable* pTargetMT,
                                                   IN CorDeclSecurity action,
                                                   OUT OBJECTREF *pDeclaredPermissions,
                                                   OUT PsetCacheEntry **pPCE);

    static OBJECTREF GetLinktimePermissions(MethodTable* pMT, OBJECTREF *prefNonCasDemands);

    // Is the type represented by this TypeSecurityDescripter participating in type equivalence
    inline BOOL IsTypeEquivalent();

    void InvokeInheritanceChecks(MethodTable* pMT);
    inline void InvokeLinktimeChecks(Assembly* pCaller);
    static inline void InvokeLinktimeChecks(MethodTable* pMT, Assembly* pCaller);

private:
    inline TypeSecurityDescriptor& operator=(const TypeSecurityDescriptor &tsd);
    void ComputeTypeDeclarativeSecurityInfo();
    static TypeSecurityDescriptor* GetTypeSecurityDescriptor(MethodTable* pMT);    
    void VerifyDataComputedInternal();
    inline void VerifyDataComputed();
    // Force the type to figure out if it is transparent or critial.
    // NOTE: Generally this is not needed, as the data is cached on the EEClass for you. This method should
    // only be called if the EEClass is returning FALSE from HasCriticalTransparentInfo
    void ComputeCriticalTransparentInfo();
    static BOOL CanTypeSecurityDescriptorBeCached(MethodTable* pMT);

private:
    // Helper class which fires transparency calculation begin/end ETW events
    class TypeSecurityDescriptorTransparencyEtwEvents
    {
    private:
        const TypeSecurityDescriptor *m_pTSD;

    public:
        inline TypeSecurityDescriptorTransparencyEtwEvents(const TypeSecurityDescriptor *pTSD);
        inline ~TypeSecurityDescriptorTransparencyEtwEvents();
    };
};


enum ModuleSecurityDescriptorFlags
{
    ModuleSecurityDescriptorFlags_None                          = 0x0000,
    ModuleSecurityDescriptorFlags_IsComputed                    = 0x0001,

    ModuleSecurityDescriptorFlags_IsAPTCA                       = 0x0002,       // The assembly allows partially trusted callers
    ModuleSecurityDescriptorFlags_IsAllCritical                 = 0x0004,       // Every type and method introduced by the assembly is critical
    ModuleSecurityDescriptorFlags_IsAllTransparent              = 0x0008,       // Every type and method in the assembly is transparent
    ModuleSecurityDescriptorFlags_IsTreatAsSafe                 = 0x0010,       // Combined with IsAllCritical - every type and method introduced by the assembly is safe critical
    ModuleSecurityDescriptorFlags_IsOpportunisticallyCritical   = 0x0020,       // Ensure that the assembly follows all transparency rules by making all methods critical or safe critical as needed
    ModuleSecurityDescriptorFlags_SkipFullTrustVerification     = 0x0040,       // Fully trusted transparent code does not require verification
    ModuleSecurityDescriptorFlags_TransparentDueToPartialTrust  = 0x0080,       // Whether we made the assembly all transparent because it was partially-trusted
};

inline ModuleSecurityDescriptorFlags operator|(ModuleSecurityDescriptorFlags lhs,
                                               ModuleSecurityDescriptorFlags rhs);

inline ModuleSecurityDescriptorFlags operator|=(ModuleSecurityDescriptorFlags& lhs,
                                                ModuleSecurityDescriptorFlags rhs);

inline ModuleSecurityDescriptorFlags operator&(ModuleSecurityDescriptorFlags lhs,
                                               ModuleSecurityDescriptorFlags rhs);

inline ModuleSecurityDescriptorFlags operator&=(ModuleSecurityDescriptorFlags& lhs,
                                                ModuleSecurityDescriptorFlags rhs);

inline ModuleSecurityDescriptorFlags operator~(ModuleSecurityDescriptorFlags flags);


// Module security descriptor, this class contains static security information about the module
// this information will get persisted in the NGen image
class ModuleSecurityDescriptor
{
    friend class Module;

private:
    PTR_Module                    m_pModule;
    ModuleSecurityDescriptorFlags m_flags;
    TokenSecurityDescriptorFlags  m_tokenFlags;

private:
    explicit inline ModuleSecurityDescriptor(PTR_Module pModule);

public:
    static inline BOOL IsMarkedTransparent(Assembly* pAssembly);

    static ModuleSecurityDescriptor* GetModuleSecurityDescriptor(Assembly* pAssembly);

    void Save(DataImage *image);
    void Fixup(DataImage *image);

    void VerifyDataComputed();

    inline void OverrideTokenFlags(TokenSecurityDescriptorFlags tokenFlags);
    inline TokenSecurityDescriptorFlags GetTokenFlags();

    inline Module *GetModule();

#ifdef DACCESS_COMPILE
    // Get the value of the module security descriptor flags without forcing them to be computed
    inline ModuleSecurityDescriptorFlags GetRawFlags();
#endif // DACCESS_COMPILE

    // Is every method and type in the assembly transparent
    inline BOOL IsAllTransparent();

    // Is every method and type introduced by the assembly critical
    inline BOOL IsAllCritical();

    // Combined with IsAllCritical - is every method and type introduced by the assembly safe critical
    inline BOOL IsTreatAsSafe();

    // Does the assembly not care about transparency, and wants the CLR to take care of making sure everything
    // is annotated properly in the assembly.
    inline BOOL IsOpportunisticallyCritical();

    // Does the assembly contain a mix of critical and transparent code
    inline BOOL IsMixedTransparency();

    // Partial trust assemblies are forced all-transparent under some conditions. This 
    // tells us whether that is true for this particular assembly.
    inline BOOL IsAllTransparentDueToPartialTrust();

    // Get the rule set the assembly uses
    inline SecurityRuleSet GetSecurityRuleSet();


#if defined(FEATURE_CORESYSTEM)
    // Does the assembly allow partially trusted callers
    inline BOOL IsAPTCA();
#endif // defined(FEATURE_CORESYSTEM)


private:
    // Helper class which fires transparency calculation begin/end ETW events
    class ModuleSecurityDescriptorTransparencyEtwEvents
    {
    private:
        ModuleSecurityDescriptor *m_pMSD;

    public:
        inline ModuleSecurityDescriptorTransparencyEtwEvents(ModuleSecurityDescriptor *pMSD);
        inline ~ModuleSecurityDescriptorTransparencyEtwEvents();
    };
};

#include "securitymeta.inl"

#endif // __SECURITYMETA_H__

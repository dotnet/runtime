// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// securityTransparentAssembly.h
//
// Implementation for transparent code feature
//


//--------------------------------------------------------------------------


#ifndef __SECURITYTRANSPARENT_H__
#define __SECURITYTRANSPARENT_H__

#include "securitymeta.h"

// Reason that a transparency error was flagged
enum SecurityTransparencyError
{
    SecurityTransparencyError_None,
    SecurityTransparencyError_CallCriticalMethod,                 // A transparent method tried to call a critical method
    SecurityTransparencyError_CallLinkDemand                      // A transparent method tried to call a method with a LinkDemand
};

namespace SecurityTransparent
{
//private:
    BOOL IsMethodTransparent(MethodDesc *pMD);
    BOOL IsMethodCritical(MethodDesc *pMD);
    BOOL IsMethodSafeCritical(MethodDesc *pMD);
    BOOL IsTypeCritical(MethodTable *pMT);
    BOOL IsTypeSafeCritical(MethodTable *pMT);
    BOOL IsTypeTransparent(MethodTable *pMT);
    BOOL IsTypeAllTransparent(MethodTable *pMT);
    BOOL IsTypeAllCritical(MethodTable *pMT);
    BOOL IsFieldTransparent(FieldDesc *pFD);
    BOOL IsFieldCritical(FieldDesc *pFD);
    BOOL IsFieldSafeCritical(FieldDesc *pFD);
    BOOL IsTokenTransparent(Module *pModule, mdToken tkToken);

//public:
    bool SecurityCalloutQuickCheck(MethodDesc *pCallerMD);

    CorInfoIsAccessAllowedResult RequiresTransparentCodeChecks(MethodDesc* pCaller,
                                                                      MethodDesc* pCallee,
                                                                      SecurityTransparencyError *pError);
    CorInfoIsAccessAllowedResult RequiresTransparentAssemblyChecks(MethodDesc* pCaller,
                                                                        MethodDesc* pCallee,
                                                                        SecurityTransparencyError *pError);
    void EnforceTransparentAssemblyChecks(MethodDesc* pCaller, MethodDesc* pCallee);
    void EnforceTransparentDelegateChecks(MethodTable* pDelegateMT, MethodDesc* pCallee);
    CorInfoCanSkipVerificationResult JITCanSkipVerification(DomainAssembly * pAssembly);
    CorInfoCanSkipVerificationResult JITCanSkipVerification(MethodDesc * pMD);
    VOID PerformTransparencyChecksForLoadByteArray(MethodDesc* pCallersMD, AssemblySecurityDescriptor* pLoadedSecDesc);
    BOOL CheckCriticalAccess(AccessCheckContext* pContext,
                        MethodDesc* pOptionalTargetMethod, 
                        FieldDesc* pOptionalTargetField,
                        MethodTable * pOptionalTargetType);
    BOOL IsAllowedToAssert(MethodDesc *pMD);

    bool TypeRequiresTransparencyCheck(TypeHandle type, bool checkForLinkDemands);

    void DECLSPEC_NORETURN ThrowMethodAccessException(MethodDesc* pMD, DWORD dwMessageId = IDS_CRITICAL_METHOD_ACCESS_DENIED);

    void DECLSPEC_NORETURN ThrowTypeLoadException(MethodDesc* pMD, DWORD dwMessageId = IDS_METHOD_INHERITANCE_RULES_VIOLATED);
    void DECLSPEC_NORETURN ThrowTypeLoadException(MethodTable* pMT);

    void DoSecurityClassAccessChecks(MethodDesc *pCallerMD,
                                            const TypeHandle &calleeTH,
                                            CorInfoSecurityRuntimeChecks checks);
#ifdef _DEBUG
    void LogTransparencyError(Assembly *pAssembly, const LPCSTR szError);
    void LogTransparencyError(MethodTable *pMT, const LPCSTR szError);
    void LogTransparencyError(MethodDesc *pMD, const LPCSTR szError, MethodDesc *pTargetMD = NULL);
#endif // _DEBUG
};

//
// Transparency is implemented slightly differently between v2 desktop, v4 desktop, and CoreCLR.  In order to
// support running v2 desktop assemblies on the v4 CLR without modifying their expected transparency behavior,
// we indirect all questions about what transparency means through a SecurityTransparencyBehavior object.
//
// The SecurityTransparencyBehavior object uses implementations of ISecurityTransparencyImpl to query about
// specific behavior differences.
//

enum SecurityTransparencyBehaviorFlags
{
    SecurityTransparencyBehaviorFlags_None                                  = 0x0000,

    // Custom attributes require transparency checks in order to be used by transparent code
    SecurityTransparencyBehaviorFlags_AttributesRequireTransparencyCheck    = 0x0001,

    // Public critical members of an assembly can behave as if they were safe critical with a LinkDemand
    // for FullTrust
    SecurityTransparencyBehaviorFlags_CriticalMembersConvertToLinkDemand    = 0x0002,

    // Types and methods must obey the transparency inheritance rules
    SecurityTransparencyBehaviorFlags_InheritanceRulesEnforced              = 0x0004,

    // Members contained within a scope that introduces members as critical may add their own treat as safe
    SecurityTransparencyBehaviorFlags_IntroducedCriticalsMayAddTreatAsSafe  = 0x0008, 

    // Opportunistically critical assemblies consist of entirely transparent types with entirely safe
    // critical methods.
    SecurityTransparencyBehaviorFlags_OpportunisticIsSafeCriticalMethods    = 0x0010,

    // Assemblies loaded in partial trust are implicitly all transparent
    SecurityTransparencyBehaviorFlags_PartialTrustImpliesAllTransparent     = 0x0020,

    // All public critical types and methods get an implicit treat as safe marking
    SecurityTransparencyBehaviorFlags_PublicImpliesTreatAsSafe              = 0x0040,

    // Security critical or safe critical at a larger than method scope applies only to methods introduced
    // within that scope, rather than all methods contained in the scope
    SecurityTransparencyBehaviorFlags_ScopeAppliesOnlyToIntroducedMethods   = 0x0080,

    // Security transparent code can call methods protected with a LinkDemand
    SecurityTransparencyBehaviorFlags_TransparentCodeCanCallLinkDemand      = 0x0100,

    // Security transparent code can call native code via P/Invoke or COM Interop
    SecurityTransaprencyBehaviorFlags_TransparentCodeCanCallUnmanagedCode   = 0x0200,

    // Security transparent code can skip verification with a runtime check
    SecurityTransparencyBehaviorFlags_TransparentCodeCanSkipVerification    = 0x0400,

    // Unsigned assemblies implicitly are APTCA
    SecurityTransparencyBehaviorFlags_UnsignedImpliesAPTCA                  = 0x0800,
};

inline SecurityTransparencyBehaviorFlags operator|(SecurityTransparencyBehaviorFlags lhs,
                                                   SecurityTransparencyBehaviorFlags rhs);

inline SecurityTransparencyBehaviorFlags operator|=(SecurityTransparencyBehaviorFlags& lhs,
                                                    SecurityTransparencyBehaviorFlags rhs);

inline SecurityTransparencyBehaviorFlags operator&(SecurityTransparencyBehaviorFlags lhs,
                                                   SecurityTransparencyBehaviorFlags rhs);

inline SecurityTransparencyBehaviorFlags operator&=(SecurityTransparencyBehaviorFlags &lhs,
                                                    SecurityTransparencyBehaviorFlags rhs);

// Base interface for transparency behavior implementations
class ISecurityTransparencyImpl
{
public:
    virtual ~ISecurityTransparencyImpl()
    {
        LIMITED_METHOD_CONTRACT;
    }

    // Get flags that indicate specific on/off behaviors of transparency
    virtual SecurityTransparencyBehaviorFlags GetBehaviorFlags() const = 0;

    // Map security attributes that a field contains to the set of behaviors it supports
    virtual FieldSecurityDescriptorFlags MapFieldAttributes(TokenSecurityDescriptorFlags tokenFlags) const = 0;

    // Map security attributes that a method contains to the set of behaviors it supports
    virtual MethodSecurityDescriptorFlags MapMethodAttributes(TokenSecurityDescriptorFlags tokenFlags) const = 0;

    // Map security attributes that a module contains to the set of behaviors it supports
    virtual ModuleSecurityDescriptorFlags MapModuleAttributes(TokenSecurityDescriptorFlags tokenFlags) const = 0;

    // Map security attributes that a type contains to the set of behaviors it supports
    virtual TypeSecurityDescriptorFlags MapTypeAttributes(TokenSecurityDescriptorFlags tokenFlags) const = 0;
};

class SecurityTransparencyBehavior
{
public:
    // Get a transparency behavior for a module with the given attributes applied to it
    static
    const SecurityTransparencyBehavior *GetTransparencyBehavior(SecurityRuleSet ruleSet);

public:
    // Are types and methods required to obey the transparency inheritance rules
    inline bool AreInheritanceRulesEnforced() const;

    // Can public critical members of an assembly behave as if they were safe critical with a LinkDemand
    // for FullTrust
    inline bool CanCriticalMembersBeConvertedToLinkDemand() const;

    // Can members contained within a scope that introduces members as critical add their own TreatAsSafe
    // attribute
    inline bool CanIntroducedCriticalMembersAddTreatAsSafe() const;

    // Can transparent methods call methods protected with a LinkDemand
    inline bool CanTransparentCodeCallLinkDemandMethods() const;

    // Can transparent methods call native code
    inline bool CanTransparentCodeCallUnmanagedCode() const;

    // Can transparent members skip verification if the callstack passes a runtime check
    inline bool CanTransparentCodeSkipVerification() const;

    // Custom attributes require transparency checks in order to be used by transparent code
    inline bool DoAttributesRequireTransparencyChecks() const;

    // Opportunistically critical assemblies consist of entirely transparent types with entirely safe
    // critical methods.
    inline bool DoesOpportunisticRequireOnlySafeCriticalMethods() const;

    // Does being loaded in partial trust imply that the assembly is implicitly all transparent
    inline bool DoesPartialTrustImplyAllTransparent() const;

    // Do all public members of the assembly get an implicit treat as safe marking
    inline bool DoesPublicImplyTreatAsSafe() const;

    // Do security critical or safe critical at a larger than method scope apply only to methods introduced
    // within that scope, or to all methods conateind within the scope.
    inline bool DoesScopeApplyOnlyToIntroducedMethods() const;

    // Do unsigned assemblies implicitly become APTCA
    inline bool DoesUnsignedImplyAPTCA() const;

    // Get flags that indicate specific on/off behaviors of transparency
    inline FieldSecurityDescriptorFlags MapFieldAttributes(TokenSecurityDescriptorFlags tokenFlags) const;

    // Map security attributes that a method contains to the set of behaviors it supports
    inline MethodSecurityDescriptorFlags MapMethodAttributes(TokenSecurityDescriptorFlags tokenFlags) const;
    
    // Map security attributes that a module contains to the set of behaviors it supports
    inline ModuleSecurityDescriptorFlags MapModuleAttributes(TokenSecurityDescriptorFlags tokenFlags) const;

    // Map security attributes that a type contains to the set of behaviors it supports
    inline TypeSecurityDescriptorFlags MapTypeAttributes(TokenSecurityDescriptorFlags tokenFlags) const;

private:
    explicit inline SecurityTransparencyBehavior(ISecurityTransparencyImpl *pTransparencyImpl);
    SecurityTransparencyBehavior(const SecurityTransparencyBehavior &);            // not implemented
    SecurityTransparencyBehavior &operator=(const SecurityTransparencyBehavior &); // not implemented

private:
    template <class T>
    friend const SecurityTransparencyBehavior *GetOrCreateTransparencyBehavior(SecurityTransparencyBehavior **ppBehavior);

private:
    static SecurityTransparencyBehavior     *s_pStandardTransparencyBehavior;
    static SecurityTransparencyBehavior     *s_pLegacyTransparencyBehavior;

    ISecurityTransparencyImpl               *m_pTransparencyImpl;
    SecurityTransparencyBehaviorFlags        m_flags;
};

#include "securitytransparentassembly.inl"

#endif // __SECURITYTRANSPARENT_H__

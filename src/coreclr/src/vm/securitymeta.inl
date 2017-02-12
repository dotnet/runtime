// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// securitymeta.inl
//
// pre-computes various security information, declarative and runtime meta-info
//


// 
//--------------------------------------------------------------------------


#include "typestring.h"

#include "securitypolicy.h"
#include "securitydeclarative.h"

#ifndef __SECURITYMETA_INL__
#define __SECURITYMETA_INL__

inline TokenSecurityDescriptorFlags operator|(TokenSecurityDescriptorFlags lhs,
                                              TokenSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<TokenSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                     static_cast<DWORD>(rhs));
}

inline TokenSecurityDescriptorFlags operator|=(TokenSecurityDescriptorFlags& lhs,
                                               TokenSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<TokenSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                    static_cast<DWORD>(rhs));
    return lhs;
}

inline TokenSecurityDescriptorFlags operator&(TokenSecurityDescriptorFlags lhs,
                                              TokenSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<TokenSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                     static_cast<DWORD>(rhs));
}

inline TokenSecurityDescriptorFlags operator&=(TokenSecurityDescriptorFlags& lhs,
                                               TokenSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<TokenSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                    static_cast<DWORD>(rhs));
    return lhs;
}

inline TokenSecurityDescriptorFlags operator~(TokenSecurityDescriptorFlags flags)
{
    LIMITED_METHOD_CONTRACT;

    // Invert all the bits which aren't part of the rules version number
    DWORD flagBits = flags & ~static_cast<DWORD>(TokenSecurityDescriptorFlags_RulesVersionMask);
    return static_cast<TokenSecurityDescriptorFlags>(
        (EncodeSecurityRuleSet(GetSecurityRuleSet(flags)) << 24 ) |
        (~flagBits));
}

// Get the version of the security rules that token security descriptor flags are requesting
inline SecurityRuleSet GetSecurityRuleSet(TokenSecurityDescriptorFlags flags)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<SecurityRuleSet>((flags & TokenSecurityDescriptorFlags_RulesMask) >> 24);
}

// Encode a security rule set into token flags - this reverses GetSecurityRuleSet
inline TokenSecurityDescriptorFlags EncodeSecurityRuleSet(SecurityRuleSet ruleSet)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<TokenSecurityDescriptorFlags>(static_cast<DWORD>(ruleSet) << 24);
}

inline TokenSecurityDescriptor::TokenSecurityDescriptor(PTR_Module pModule, mdToken token)
    : m_pModule(pModule),
      m_token(token),
      m_flags(TokenSecurityDescriptorFlags_None)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pModule);
}

inline TokenSecurityDescriptorFlags TokenSecurityDescriptor::GetFlags()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return m_flags;
}

// Get the attributes that were set on the token
inline TokenSecurityDescriptorFlags TokenSecurityDescriptor::GetMetadataFlags()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return m_flags & TokenSecurityDescriptorFlags_RawAttributeMask;
}

inline BOOL TokenSecurityDescriptor::IsSemanticCritical()
{
    WRAPPER_NO_CONTRACT;
    VerifySemanticDataComputed();
    return !!(m_flags & TokenSecurityDescriptorFlags_IsSemanticCritical);
}

inline BOOL TokenSecurityDescriptor::IsSemanticTreatAsSafe()
{
    WRAPPER_NO_CONTRACT;
    VerifySemanticDataComputed();
    return !!(m_flags & TokenSecurityDescriptorFlags_IsSemanticTreatAsSafe);
}

inline BOOL TokenSecurityDescriptor::IsSemanticExternallyVisible()
{
    WRAPPER_NO_CONTRACT;
    VerifySemanticDataComputed();
    return !!(m_flags & TokenSecurityDescriptorFlags_IsSemanticExternallyVisible);
}

// Determine if the type represented by the token in this TokenSecurityDescriptor is participating in type
// equivalence.
inline BOOL TokenSecurityDescriptor::IsTypeEquivalent()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(TypeFromToken(m_token) == mdtTypeDef);
    return IsTypeDefEquivalent(m_token, m_pModule);
}

#ifndef DACCESS_COMPILE

inline TokenSecurityDescriptor::TokenSecurityDescriptorTransparencyEtwEvents::TokenSecurityDescriptorTransparencyEtwEvents(const TokenSecurityDescriptor *pTSD)
    : m_pTSD(pTSD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END
   
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TokenTransparencyComputationStart))
    {
        LPCWSTR module = m_pTSD->m_pModule->GetPathForErrorMessages();

        ETW::SecurityLog::FireTokenTransparencyComputationStart(m_pTSD->m_token,
                                                                module,
                                                                ::GetAppDomain()->GetId().m_dwId);
    }
}

inline TokenSecurityDescriptor::TokenSecurityDescriptorTransparencyEtwEvents::~TokenSecurityDescriptorTransparencyEtwEvents()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END
   
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TokenTransparencyComputationEnd))
    {
        LPCWSTR module = m_pTSD->m_pModule->GetPathForErrorMessages();

        ETW::SecurityLog::FireTokenTransparencyComputationEnd(m_pTSD->m_token,
                                                              module,
                                                              !!(m_pTSD->m_flags & TokenSecurityDescriptorFlags_IsSemanticCritical),
                                                              !!(m_pTSD->m_flags & TokenSecurityDescriptorFlags_IsSemanticTreatAsSafe),
                                                              ::GetAppDomain()->GetId().m_dwId);
    }
}

#endif //!DACCESS_COMPILE

inline MethodSecurityDescriptor::MethodSecurityDescriptor(MethodDesc* pMD, BOOL fCanCache /* = TRUE */) :
    m_pMD(pMD),
    m_pRuntimeDeclActionInfo(NULL),
    m_pTokenDeclActionInfo(NULL),
    m_flags(MethodSecurityDescriptorFlags_None),
    m_declFlagsDuringPreStub(0)
{
    WRAPPER_NO_CONTRACT;

    if (fCanCache)
    {
        SetCanCache();
    }
}

inline BOOL MethodSecurityDescriptor::CanAssert()
{
    // No need to do a VerifyDataComputed here -> this value is set by SecurityDeclarative::EnsureAssertAllowed as an optmization
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & MethodSecurityDescriptorFlags_AssertAllowed);
}

inline void MethodSecurityDescriptor::SetCanAssert()
{
    LIMITED_METHOD_CONTRACT;
    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), MethodSecurityDescriptorFlags_AssertAllowed);
}

inline BOOL MethodSecurityDescriptor::CanCache()
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & MethodSecurityDescriptorFlags_CanCache);
}

inline void MethodSecurityDescriptor::SetCanCache()
{
    LIMITED_METHOD_CONTRACT;
    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), MethodSecurityDescriptorFlags_CanCache);
}
    
inline BOOL MethodSecurityDescriptor::HasRuntimeDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return m_pMD->IsInterceptedForDeclSecurity();
}

inline BOOL MethodSecurityDescriptor::HasLinkOrInheritanceDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return HasLinktimeDeclarativeSecurity() || HasInheritanceDeclarativeSecurity();
}
       
inline BOOL MethodSecurityDescriptor::HasLinktimeDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return m_pMD->RequiresLinktimeCheck();
}

inline BOOL MethodSecurityDescriptor::HasInheritanceDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return m_pMD->RequiresInheritanceCheck();
}

inline mdToken MethodSecurityDescriptor::GetToken()
{
    WRAPPER_NO_CONTRACT;
    return m_pMD->GetMemberDef();
}

inline MethodDesc *MethodSecurityDescriptor::GetMethod()
{
    WRAPPER_NO_CONTRACT;
    return m_pMD;
}

inline IMDInternalImport *MethodSecurityDescriptor::GetIMDInternalImport()
{
    WRAPPER_NO_CONTRACT;
    return m_pMD->GetMDImport();
}


inline BOOL MethodSecurityDescriptor::ContainsBuiltInCASDemandsOnly()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return ((m_flags & MethodSecurityDescriptorFlags_IsBuiltInCASPermsOnly) &&
            (m_flags & MethodSecurityDescriptorFlags_IsDemandsOnly));
}

inline DeclActionInfo* MethodSecurityDescriptor::GetRuntimeDeclActionInfo()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return m_pRuntimeDeclActionInfo;
}

inline DWORD MethodSecurityDescriptor::GetDeclFlagsDuringPreStub()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return m_declFlagsDuringPreStub;
}

inline TokenDeclActionInfo* MethodSecurityDescriptor::GetTokenDeclActionInfo()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return m_pTokenDeclActionInfo;
}
    
inline BOOL MethodSecurityDescriptor::IsCritical()
{
    WRAPPER_NO_CONTRACT;

    if (!m_pMD->HasCriticalTransparentInfo())
        ComputeCriticalTransparentInfo();
    return m_pMD->IsCritical();
}

inline BOOL MethodSecurityDescriptor::IsTreatAsSafe()
{
    WRAPPER_NO_CONTRACT;

    if (!m_pMD->HasCriticalTransparentInfo())
        ComputeCriticalTransparentInfo();
    return m_pMD->IsTreatAsSafe();
}

inline BOOL MethodSecurityDescriptor::IsOpportunisticallyCritical()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    TypeSecurityDescriptor typeSecDesc(m_pMD->GetMethodTable());
    return typeSecDesc.IsOpportunisticallyCritical();
}

inline HRESULT MethodSecurityDescriptor::GetDeclaredPermissionsWithCache(IN CorDeclSecurity action,
                                                                         OUT OBJECTREF *pDeclaredPermissions,
                                                                         OUT PsetCacheEntry **pPCE)
{
    WRAPPER_NO_CONTRACT;
    return GetTokenDeclActionInfo()->GetDeclaredPermissionsWithCache(action, pDeclaredPermissions, pPCE);
}

// static
inline HRESULT MethodSecurityDescriptor::GetDeclaredPermissionsWithCache(MethodDesc* pMD,
                                                                         IN CorDeclSecurity action,
                                                                         OUT OBJECTREF *pDeclaredPermissions,
                                                                         OUT PsetCacheEntry **pPCE)
{
    WRAPPER_NO_CONTRACT;
    MethodSecurityDescriptor methodSecurityDesc(pMD);
    LookupOrCreateMethodSecurityDescriptor(&methodSecurityDesc);
    return methodSecurityDesc.GetDeclaredPermissionsWithCache(action, pDeclaredPermissions, pPCE);
}

// static
inline OBJECTREF MethodSecurityDescriptor::GetLinktimePermissions(MethodDesc* pMD,
                                                                  OBJECTREF *prefNonCasDemands)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!pMD->RequiresLinktimeCheck())
        return NULL;

    MethodSecurityDescriptor methodSecurityDesc(pMD);
    LookupOrCreateMethodSecurityDescriptor(&methodSecurityDesc);
    return methodSecurityDesc.GetTokenDeclActionInfo()->GetLinktimePermissions(prefNonCasDemands);
}

inline void MethodSecurityDescriptor::InvokeLinktimeChecks(Assembly* pCaller)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!HasLinktimeDeclarativeSecurity()) 
        return;

    GetTokenDeclActionInfo()->InvokeLinktimeChecks(pCaller);
}

// staitc
inline void MethodSecurityDescriptor::InvokeLinktimeChecks(MethodDesc* pMD, Assembly* pCaller)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!pMD->RequiresLinktimeCheck()) 
        return;

    MethodSecurityDescriptor methodSecurityDesc(pMD);
    LookupOrCreateMethodSecurityDescriptor(&methodSecurityDesc);
    methodSecurityDesc.InvokeLinktimeChecks(pCaller);
}

// static
inline BOOL MethodSecurityDescriptor::IsDeclSecurityCASDemandsOnly(DWORD dwMethDeclFlags,
                                                                   mdToken _mdToken,
                                                                   IMDInternalImport *pInternalImport)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Non-CAS demands are not supported in CoreCLR
    return TRUE;
}

#ifndef DACCESS_COMPILE

inline MethodSecurityDescriptor::MethodSecurityDescriptorTransparencyEtwEvents::MethodSecurityDescriptorTransparencyEtwEvents(const MethodSecurityDescriptor *pMSD)
    : m_pMSD(pMSD)
{
    WRAPPER_NO_CONTRACT;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, MethodTransparencyComputationStart))
    {
        LPCWSTR module = m_pMSD->m_pMD->GetModule()->GetPathForErrorMessages();

        SString method;
        m_pMSD->m_pMD->GetFullMethodInfo(method);

        ETW::SecurityLog::FireMethodTransparencyComputationStart(method.GetUnicode(),
                                                                 module,
                                                                 ::GetAppDomain()->GetId().m_dwId);
    }
}

inline MethodSecurityDescriptor::MethodSecurityDescriptorTransparencyEtwEvents::~MethodSecurityDescriptorTransparencyEtwEvents()
{
    WRAPPER_NO_CONTRACT;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, MethodTransparencyComputationEnd))
    {
        LPCWSTR module = m_pMSD->m_pMD->GetModule()->GetPathForErrorMessages();

        SString method;
        m_pMSD->m_pMD->GetFullMethodInfo(method);

        BOOL fIsCritical = FALSE;
        BOOL fIsTreatAsSafe = FALSE;

        if (m_pMSD->m_pMD->HasCriticalTransparentInfo())
        {
            fIsCritical = m_pMSD->m_pMD->IsCritical();
            fIsTreatAsSafe = m_pMSD->m_pMD->IsTreatAsSafe();
        }

        ETW::SecurityLog::FireMethodTransparencyComputationEnd(method.GetUnicode(),
                                                               module,
                                                               ::GetAppDomain()->GetId().m_dwId,
                                                               fIsCritical,
                                                               fIsTreatAsSafe);
    }
}

#endif //!DACCESS_COMPILE

inline FieldSecurityDescriptorFlags operator|(FieldSecurityDescriptorFlags lhs,
                                              FieldSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<FieldSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                     static_cast<DWORD>(rhs));
}

inline FieldSecurityDescriptorFlags operator|=(FieldSecurityDescriptorFlags& lhs,
                                               FieldSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<FieldSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                    static_cast<DWORD>(rhs));
    return lhs;
}

inline FieldSecurityDescriptorFlags operator&(FieldSecurityDescriptorFlags lhs,
                                              FieldSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<FieldSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                     static_cast<DWORD>(rhs));
}

inline FieldSecurityDescriptorFlags operator&=(FieldSecurityDescriptorFlags& lhs,
                                               FieldSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<FieldSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                    static_cast<DWORD>(rhs));
    return lhs;
}

inline FieldSecurityDescriptor::FieldSecurityDescriptor(FieldDesc* pFD) :
    m_pFD(pFD),
    m_flags(FieldSecurityDescriptorFlags_None)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pFD);
}

inline BOOL FieldSecurityDescriptor::IsCritical()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & FieldSecurityDescriptorFlags_IsCritical);
}

inline BOOL FieldSecurityDescriptor::IsTreatAsSafe()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & FieldSecurityDescriptorFlags_IsTreatAsSafe);
}

#ifndef DACCESS_COMPILE

inline FieldSecurityDescriptor::FieldSecurityDescriptorTransparencyEtwEvents::FieldSecurityDescriptorTransparencyEtwEvents(const FieldSecurityDescriptor *pFSD)
    : m_pFSD(pFSD)
{
    WRAPPER_NO_CONTRACT;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, FieldTransparencyComputationStart))
    {
        LPCWSTR module = m_pFSD->m_pFD->GetModule()->GetPathForErrorMessages();

        SString field;
        TypeString::AppendType(field, TypeHandle(m_pFSD->m_pFD->GetApproxEnclosingMethodTable()));
        field.AppendUTF8("::");
        field.AppendUTF8(m_pFSD->m_pFD->GetName());

        ETW::SecurityLog::FireFieldTransparencyComputationStart(field.GetUnicode(),
                                                                module,
                                                                ::GetAppDomain()->GetId().m_dwId);
    }
}

inline FieldSecurityDescriptor::FieldSecurityDescriptorTransparencyEtwEvents::~FieldSecurityDescriptorTransparencyEtwEvents()
{
     WRAPPER_NO_CONTRACT;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, FieldTransparencyComputationEnd))
    {
        LPCWSTR module = m_pFSD->m_pFD->GetModule()->GetPathForErrorMessages();

        SString field;
        TypeString::AppendType(field, TypeHandle(m_pFSD->m_pFD->GetApproxEnclosingMethodTable()));
        field.AppendUTF8("::");
        field.AppendUTF8(m_pFSD->m_pFD->GetName());

        ETW::SecurityLog::FireFieldTransparencyComputationEnd(field.GetUnicode(),
                                                              module,
                                                              ::GetAppDomain()->GetId().m_dwId,
                                                              !!(m_pFSD->m_flags & FieldSecurityDescriptorFlags_IsCritical),
                                                              !!(m_pFSD->m_flags & FieldSecurityDescriptorFlags_IsTreatAsSafe));
    }
}

#endif //!DACCESS_COMPILE

inline TypeSecurityDescriptor::TypeSecurityDescriptor(MethodTable *pMT) :
    m_pMT(pMT->GetCanonicalMethodTable()),
    m_pTokenDeclActionInfo(NULL),
    m_fIsComputed(FALSE)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pMT);
}

inline BOOL TypeSecurityDescriptor::HasLinkOrInheritanceDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return HasLinktimeDeclarativeSecurity() || HasInheritanceDeclarativeSecurity();
}
       
inline BOOL TypeSecurityDescriptor::HasLinktimeDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return m_pMT->GetClass()->RequiresLinktimeCheck();
}

inline BOOL TypeSecurityDescriptor::HasInheritanceDeclarativeSecurity()
{
    WRAPPER_NO_CONTRACT;
    return m_pMT->GetClass()->RequiresInheritanceCheck();
}

inline BOOL TypeSecurityDescriptor::IsCritical()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EEClass *pClass = m_pMT->GetClass();
    if (!pClass->HasCriticalTransparentInfo())
    {
        ComputeCriticalTransparentInfo();
    }

    return  pClass->IsAllCritical()
            ;
}

inline BOOL TypeSecurityDescriptor::IsOpportunisticallyCritical()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    ModuleSecurityDescriptor *pModuleSecDesc = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(m_pMT->GetAssembly());
    return pModuleSecDesc->IsOpportunisticallyCritical();
}

inline BOOL TypeSecurityDescriptor::IsAllCritical()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EEClass *pClass = m_pMT->GetClass();
    if (!pClass->HasCriticalTransparentInfo())
        ComputeCriticalTransparentInfo();
    return pClass->IsAllCritical();
}

inline BOOL TypeSecurityDescriptor::IsAllTransparent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EEClass *pClass = m_pMT->GetClass();
    if (!pClass->HasCriticalTransparentInfo())
        ComputeCriticalTransparentInfo();
    return pClass->IsAllTransparent();
}
       
inline BOOL TypeSecurityDescriptor::IsTreatAsSafe()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EEClass *pClass = m_pMT->GetClass();
    if (!pClass->HasCriticalTransparentInfo())
        ComputeCriticalTransparentInfo();
    return pClass->IsTreatAsSafe();
}

inline mdToken TypeSecurityDescriptor::GetToken()
{
    WRAPPER_NO_CONTRACT;
    return m_pMT->GetCl();
}

inline IMDInternalImport *TypeSecurityDescriptor::GetIMDInternalImport()
{
    WRAPPER_NO_CONTRACT;
    return m_pMT->GetMDImport();
}    

inline TokenDeclActionInfo* TypeSecurityDescriptor::GetTokenDeclActionInfo()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return m_pTokenDeclActionInfo;
}    

inline TypeSecurityDescriptorFlags operator|(TypeSecurityDescriptorFlags lhs,
                                             TypeSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<TypeSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                    static_cast<DWORD>(rhs));
}

inline TypeSecurityDescriptorFlags operator|=(TypeSecurityDescriptorFlags& lhs,
                                              TypeSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<TypeSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                   static_cast<DWORD>(rhs));
    return lhs;
}

inline TypeSecurityDescriptorFlags operator&(TypeSecurityDescriptorFlags lhs,
                                             TypeSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<TypeSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                    static_cast<DWORD>(rhs));
}

inline TypeSecurityDescriptorFlags operator&=(TypeSecurityDescriptorFlags& lhs,
                                              TypeSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<TypeSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                   static_cast<DWORD>(rhs));
    return lhs;
}

inline HRESULT TypeSecurityDescriptor::GetDeclaredPermissionsWithCache(IN CorDeclSecurity action,
                                                                       OUT OBJECTREF *pDeclaredPermissions,
                                                                       OUT PsetCacheEntry **pPCE)
{
    WRAPPER_NO_CONTRACT;
    return GetTokenDeclActionInfo()->GetDeclaredPermissionsWithCache(action, pDeclaredPermissions, pPCE);
}

// static
inline HRESULT TypeSecurityDescriptor::GetDeclaredPermissionsWithCache(MethodTable *pTargetMT,
                                                                       IN CorDeclSecurity action,
                                                                       OUT OBJECTREF *pDeclaredPermissions,
                                                                       OUT PsetCacheEntry **pPCE)
{
    WRAPPER_NO_CONTRACT;
    TypeSecurityDescriptor* pTypeSecurityDesc = GetTypeSecurityDescriptor(pTargetMT);
    _ASSERTE(pTypeSecurityDesc != NULL);
    return pTypeSecurityDesc->GetDeclaredPermissionsWithCache(action, pDeclaredPermissions, pPCE);
}

// static
inline OBJECTREF TypeSecurityDescriptor::GetLinktimePermissions(MethodTable *pMT,
                                                                OBJECTREF *prefNonCasDemands)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (!pMT->GetClass()->RequiresLinktimeCheck())
        return NULL;

    TypeSecurityDescriptor* pTypeSecurityDesc = GetTypeSecurityDescriptor(pMT);
    _ASSERTE(pTypeSecurityDesc != NULL);             
    return pTypeSecurityDesc->GetTokenDeclActionInfo()->GetLinktimePermissions(prefNonCasDemands);
}

inline void TypeSecurityDescriptor::InvokeLinktimeChecks(Assembly* pCaller)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    if (!HasLinktimeDeclarativeSecurity())
        return;
    GetTokenDeclActionInfo()->InvokeLinktimeChecks(pCaller);
}

// Determine if the type by this TypeSecurityDescriptor is participating in type equivalence. Note that this
// is only checking to see if the type would like to participate in equivalence, and not if it is actually
// equivalent to anything - which allows its transparency to be the same regardless of what other types have
// been loaded.
inline BOOL TypeSecurityDescriptor::IsTypeEquivalent()
{
    WRAPPER_NO_CONTRACT;

    return m_pMT->GetClass()->IsEquivalentType();
}

// static
inline void TypeSecurityDescriptor::InvokeLinktimeChecks(MethodTable *pMT, Assembly* pCaller)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    if (!pMT->GetClass()->RequiresLinktimeCheck())
        return;
    GetTypeSecurityDescriptor(pMT)->InvokeLinktimeChecks(pCaller);
}

inline void TypeSecurityDescriptor::VerifyDataComputed()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_fIsComputed)
    {
        return;
    }

    BOOL canTypeSecDescCached = CanTypeSecurityDescriptorBeCached(m_pMT);
    if (!canTypeSecDescCached)
    {
        VerifyDataComputedInternal();
    }
    else
    {
        TypeSecurityDescriptor* pCachedTypeSecurityDesc = GetTypeSecurityDescriptor(m_pMT);
        *this = *pCachedTypeSecurityDesc; // copy the struct
        _ASSERTE(m_fIsComputed);
    }
    
    return;
}

inline TypeSecurityDescriptor& TypeSecurityDescriptor::operator=(const TypeSecurityDescriptor &tsd)
{
    LIMITED_METHOD_CONTRACT;

    m_pMT = tsd.m_pMT;
    m_pTokenDeclActionInfo = tsd.m_pTokenDeclActionInfo;
    m_fIsComputed = tsd.m_fIsComputed;

    return *this;
}

#ifndef DACCESS_COMPILE

inline TypeSecurityDescriptor::TypeSecurityDescriptorTransparencyEtwEvents::TypeSecurityDescriptorTransparencyEtwEvents(const TypeSecurityDescriptor *pTSD)
    : m_pTSD(pTSD)
{
    WRAPPER_NO_CONTRACT;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TypeTransparencyComputationStart))
    {
        LPCWSTR module = m_pTSD->m_pMT->GetModule()->GetPathForErrorMessages();

        SString type;
        if (!IsNilToken(m_pTSD->m_pMT->GetCl()))
        {
            TypeString::AppendType(type, TypeHandle(m_pTSD->m_pMT));
        }

        ETW::SecurityLog::FireTypeTransparencyComputationStart(type.GetUnicode(),
                                                               module,
                                                               ::GetAppDomain()->GetId().m_dwId);
    }

}

inline TypeSecurityDescriptor::TypeSecurityDescriptorTransparencyEtwEvents::~TypeSecurityDescriptorTransparencyEtwEvents()
{
    WRAPPER_NO_CONTRACT;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TypeTransparencyComputationEnd))
    {
        LPCWSTR module = m_pTSD->m_pMT->GetModule()->GetPathForErrorMessages();

        SString type;
        if (!IsNilToken(m_pTSD->m_pMT->GetCl()))
        {
            TypeString::AppendType(type, TypeHandle(m_pTSD->m_pMT));
        }

        BOOL fIsAllCritical = FALSE;
        BOOL fIsAllTransparent = FALSE;
        BOOL fIsCritical = FALSE;
        BOOL fIsTreatAsSafe = FALSE;

        EEClass *pClass = m_pTSD->m_pMT->GetClass();
        if (pClass->HasCriticalTransparentInfo())
        {
            fIsAllCritical = pClass->IsAllCritical();
            fIsAllTransparent = pClass->IsAllTransparent();
            fIsCritical = pClass->IsCritical();
            fIsTreatAsSafe = pClass->IsTreatAsSafe();
        }

        ETW::SecurityLog::FireTypeTransparencyComputationEnd(type.GetUnicode(),
                                                             module,
                                                             ::GetAppDomain()->GetId().m_dwId,
                                                             fIsAllCritical,
                                                             fIsAllTransparent,
                                                             fIsCritical,
                                                             fIsTreatAsSafe);
    }

}

#endif //!DACCESS_COMPILE

inline ModuleSecurityDescriptorFlags operator|(ModuleSecurityDescriptorFlags lhs,
                                               ModuleSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<ModuleSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                      static_cast<DWORD>(rhs));
}

inline ModuleSecurityDescriptorFlags operator|=(ModuleSecurityDescriptorFlags& lhs,
                                                ModuleSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<ModuleSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                     static_cast<DWORD>(rhs));
    return lhs;
}

inline ModuleSecurityDescriptorFlags operator&(ModuleSecurityDescriptorFlags lhs,
                                               ModuleSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<ModuleSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                      static_cast<DWORD>(rhs));
}

inline ModuleSecurityDescriptorFlags operator&=(ModuleSecurityDescriptorFlags& lhs,
                                                ModuleSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<ModuleSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                     static_cast<DWORD>(rhs));
    return lhs;
}

inline ModuleSecurityDescriptorFlags operator~(ModuleSecurityDescriptorFlags flags)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<ModuleSecurityDescriptorFlags>(~static_cast<DWORD>(flags));
}

inline ModuleSecurityDescriptor::ModuleSecurityDescriptor(PTR_Module pModule) :
    m_pModule(pModule),
    m_flags(ModuleSecurityDescriptorFlags_None),
    m_tokenFlags(TokenSecurityDescriptorFlags_None)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pModule);
}

// static
inline BOOL ModuleSecurityDescriptor::IsMarkedTransparent(Assembly* pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    return GetModuleSecurityDescriptor(pAssembly)->IsAllTransparent();
}

//---------------------------------------------------------------------------------------
//
// Override the token flags that would be read from the metadata directly with a
// precomputed set of flags. This is used by reflection emit to create a dynamic assembly
// with security attributes given at creation time.
//

inline void ModuleSecurityDescriptor::OverrideTokenFlags(TokenSecurityDescriptorFlags tokenFlags)
{
    CONTRACTL
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(!(m_flags & ModuleSecurityDescriptorFlags_IsComputed));
        PRECONDITION(m_tokenFlags == TokenSecurityDescriptorFlags_None);
        PRECONDITION(CheckPointer(m_pModule));
        PRECONDITION(m_pModule->GetAssembly()->IsDynamic());                    // Token overrides should only be used by reflection
    }
    CONTRACTL_END;

    m_tokenFlags = tokenFlags;
}

inline TokenSecurityDescriptorFlags ModuleSecurityDescriptor::GetTokenFlags()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (m_tokenFlags == TokenSecurityDescriptorFlags_None)
    {
        Assembly *pAssembly = m_pModule->GetAssembly();
        TokenSecurityDescriptor tsd(pAssembly->GetManifestModule(), pAssembly->GetManifestToken());
        EnsureWritablePages(&m_tokenFlags);
        InterlockedCompareExchange(reinterpret_cast<LONG *>(&m_tokenFlags),
                                   tsd.GetFlags(),
                                   TokenSecurityDescriptorFlags_None);
    }

    return m_tokenFlags;
}

inline Module *ModuleSecurityDescriptor::GetModule()
{
    LIMITED_METHOD_CONTRACT;
    return m_pModule;
}

#ifdef DACCESS_COMPILE
inline ModuleSecurityDescriptorFlags ModuleSecurityDescriptor::GetRawFlags()
{
    LIMITED_METHOD_CONTRACT;
    return m_flags;
}
#endif // DACCESS_COMPILE

inline BOOL ModuleSecurityDescriptor::IsAllTransparent()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & ModuleSecurityDescriptorFlags_IsAllTransparent);
}

inline BOOL ModuleSecurityDescriptor::IsAllCritical()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & ModuleSecurityDescriptorFlags_IsAllCritical);
}

inline BOOL ModuleSecurityDescriptor::IsTreatAsSafe()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & ModuleSecurityDescriptorFlags_IsTreatAsSafe);
}

inline BOOL ModuleSecurityDescriptor::IsOpportunisticallyCritical()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & ModuleSecurityDescriptorFlags_IsOpportunisticallyCritical);
}

inline BOOL ModuleSecurityDescriptor::IsAllTransparentDueToPartialTrust()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & ModuleSecurityDescriptorFlags_TransparentDueToPartialTrust);
}

inline BOOL ModuleSecurityDescriptor::IsMixedTransparency()
{
    WRAPPER_NO_CONTRACT;
    return !IsAllCritical() && !IsAllTransparent();
}


#if defined(FEATURE_CORESYSTEM)
inline BOOL ModuleSecurityDescriptor::IsAPTCA()
{
    WRAPPER_NO_CONTRACT;
    VerifyDataComputed();
    return !!(m_flags & ModuleSecurityDescriptorFlags_IsAPTCA);
}
#endif // defined(FEATURE_CORESYSTEM)

// Get the set of security rules that the assembly is using
inline SecurityRuleSet ModuleSecurityDescriptor::GetSecurityRuleSet()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If the assembly specified a rule set, then use that. If it's a v2 assembly, then use the v2 rules.
    // Otherwise, use the default rule set.
    TokenSecurityDescriptorFlags tokenFlags = GetTokenFlags();
    if (tokenFlags & TokenSecurityDescriptorFlags_SecurityRules)
    {
        return ::GetSecurityRuleSet(tokenFlags);
    }
    else
    {
        // The assembly hasn't specified the rule set that it needs to use.  We'll just use the default rule
        // set unless the environment is overriding that with another value.
        DWORD dwDefaultRuleSet = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Security_DefaultSecurityRuleSet);

        if (dwDefaultRuleSet == 0)
        {
            return SecurityRuleSet_Default;
        }
        else
        {
            return static_cast<SecurityRuleSet>(dwDefaultRuleSet);
        }
    }
}

#ifndef DACCESS_COMPILE

inline ModuleSecurityDescriptor::ModuleSecurityDescriptorTransparencyEtwEvents::ModuleSecurityDescriptorTransparencyEtwEvents(ModuleSecurityDescriptor *pMSD)
    : m_pMSD(pMSD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, ModuleTransparencyComputationStart))
    {
        LPCWSTR module = m_pMSD->m_pModule->GetPathForErrorMessages();

        ETW::SecurityLog::FireModuleTransparencyComputationStart(module,
                                                                 ::GetAppDomain()->GetId().m_dwId);
    }
}

inline ModuleSecurityDescriptor::ModuleSecurityDescriptorTransparencyEtwEvents::~ModuleSecurityDescriptorTransparencyEtwEvents()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END
   
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, ModuleTransparencyComputationEnd))
    {
        LPCWSTR module = m_pMSD->m_pModule->GetPathForErrorMessages();

        ETW::SecurityLog::FireModuleTransparencyComputationEnd(module,
                                                               ::GetAppDomain()->GetId().m_dwId,
                                                               !!(m_pMSD->m_flags & ModuleSecurityDescriptorFlags_IsAllCritical),
                                                               !!(m_pMSD->m_flags & ModuleSecurityDescriptorFlags_IsAllTransparent),
                                                               !!(m_pMSD->m_flags & ModuleSecurityDescriptorFlags_IsTreatAsSafe),
                                                               !!(m_pMSD->m_flags & ModuleSecurityDescriptorFlags_IsOpportunisticallyCritical),
                                                               m_pMSD->GetSecurityRuleSet());
    }
}

#endif //!DACCESS_COMPILE

inline MethodSecurityDescriptorFlags operator|(MethodSecurityDescriptorFlags lhs,
                                               MethodSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<MethodSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                      static_cast<DWORD>(rhs));
}

inline MethodSecurityDescriptorFlags operator|=(MethodSecurityDescriptorFlags& lhs,
                                                MethodSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<MethodSecurityDescriptorFlags>(static_cast<DWORD>(lhs) |
                                                     static_cast<DWORD>(rhs));
    return lhs;
}

inline MethodSecurityDescriptorFlags operator&(MethodSecurityDescriptorFlags lhs,
                                               MethodSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<MethodSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                      static_cast<DWORD>(rhs));
}

inline MethodSecurityDescriptorFlags operator&=(MethodSecurityDescriptorFlags& lhs,
                                                MethodSecurityDescriptorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<MethodSecurityDescriptorFlags>(static_cast<DWORD>(lhs) &
                                                     static_cast<DWORD>(rhs));
    return lhs;
}

inline void MethodSecurityDescriptor::VerifyDataComputed()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_flags & MethodSecurityDescriptorFlags_IsComputed)
        return;

    BOOL canMethSecDescCached = (CanCache() && CanMethodSecurityDescriptorBeCached(m_pMD));
    if (!canMethSecDescCached)
    {
        VerifyDataComputedInternal();
    }
    else
    {
        LookupOrCreateMethodSecurityDescriptor(this);
        _ASSERTE(m_flags & MethodSecurityDescriptorFlags_IsComputed);
    }
    
    return;
}

#endif // __SECURITYMETA_INL__

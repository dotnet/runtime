// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// securitytransparentassembly.inl
//
// Implementation for transparent code feature
//


//--------------------------------------------------------------------------


#ifndef __SECURITYTRANSPARENT_INL__
#define __SECURITYTRANSPARENT_INL__

//---------------------------------------------------------------------------------------
//
// Create a transparency behavior object
//
// Arguments:
//   pTransparencyImpl - transparency implementation to base behavior decisions on
//
// Notes:
//   The tranparency implementation object must have a lifetime at least as long as the
//   created transparency behavior object.
//

inline SecurityTransparencyBehavior::SecurityTransparencyBehavior(ISecurityTransparencyImpl *pTransparencyImpl) :
    m_pTransparencyImpl(pTransparencyImpl),
    m_flags(pTransparencyImpl->GetBehaviorFlags())
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pTransparencyImpl);
}

//
// Typed logical operators for transparency flags
//

inline SecurityTransparencyBehaviorFlags operator|(SecurityTransparencyBehaviorFlags lhs,
                                                   SecurityTransparencyBehaviorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<SecurityTransparencyBehaviorFlags>(static_cast<DWORD>(lhs) |
                                                          static_cast<DWORD>(rhs));
}

inline SecurityTransparencyBehaviorFlags operator|=(SecurityTransparencyBehaviorFlags& lhs,
                                                    SecurityTransparencyBehaviorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<SecurityTransparencyBehaviorFlags>(static_cast<DWORD>(lhs) |
                                                         static_cast<DWORD>(rhs));
    return lhs;
}

inline SecurityTransparencyBehaviorFlags operator&(SecurityTransparencyBehaviorFlags lhs,
                                                   SecurityTransparencyBehaviorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<SecurityTransparencyBehaviorFlags>(static_cast<DWORD>(lhs) &
                                                          static_cast<DWORD>(rhs));
}

inline SecurityTransparencyBehaviorFlags operator&=(SecurityTransparencyBehaviorFlags& lhs,
                                                    SecurityTransparencyBehaviorFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<SecurityTransparencyBehaviorFlags>(static_cast<DWORD>(lhs) &
                                                         static_cast<DWORD>(rhs));
    return lhs;
}

//---------------------------------------------------------------------------------------
//
// Are types and methods required to obey the transparency inheritance rules
//

inline bool SecurityTransparencyBehavior::AreInheritanceRulesEnforced() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_InheritanceRulesEnforced);
}

//---------------------------------------------------------------------------------------
//
// Can public critical members of an assembly behave as if they were safe critical with a
// LinkDemand for FullTrust
//

inline bool SecurityTransparencyBehavior::CanCriticalMembersBeConvertedToLinkDemand() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_CriticalMembersConvertToLinkDemand);
}

//---------------------------------------------------------------------------------------
//
// Can members contained within a scope that introduces members as critical add their own
// TreatAsSafe attribute
//

inline bool SecurityTransparencyBehavior::CanIntroducedCriticalMembersAddTreatAsSafe() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_IntroducedCriticalsMayAddTreatAsSafe);
}

//---------------------------------------------------------------------------------------
//
// Can transparent methods call methods protected with a LinkDemand
//

inline bool SecurityTransparencyBehavior::CanTransparentCodeCallLinkDemandMethods() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_TransparentCodeCanCallLinkDemand);
}

//---------------------------------------------------------------------------------------
//
// Can transparent members call native code directly
//

inline bool SecurityTransparencyBehavior::CanTransparentCodeCallUnmanagedCode() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransaprencyBehaviorFlags_TransparentCodeCanCallUnmanagedCode);
}

//---------------------------------------------------------------------------------------
//
// Can transparent members skip verification if the callstack passes a runtime check
//

inline bool SecurityTransparencyBehavior::CanTransparentCodeSkipVerification() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_TransparentCodeCanSkipVerification);
}

//---------------------------------------------------------------------------------------
//
// Custom attributes require transparency checks in order to be used by critical code
// 

inline bool SecurityTransparencyBehavior::DoAttributesRequireTransparencyChecks() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_AttributesRequireTransparencyCheck);
}

//---------------------------------------------------------------------------------------
//
// Opportunistically critical assemblies consist of entirely transparent types with entirely safe
// critical methods.
inline bool SecurityTransparencyBehavior::DoesOpportunisticRequireOnlySafeCriticalMethods() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_OpportunisticIsSafeCriticalMethods);
}

//---------------------------------------------------------------------------------------
//
// Does being loaded in partial trust imply that the assembly is implicitly all transparent
//

inline bool SecurityTransparencyBehavior::DoesPartialTrustImplyAllTransparent() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_PartialTrustImpliesAllTransparent);
}

//---------------------------------------------------------------------------------------
//
// Do all public types and methods automatically become treat as safe
//

inline bool SecurityTransparencyBehavior::DoesPublicImplyTreatAsSafe() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_PublicImpliesTreatAsSafe);
}

//---------------------------------------------------------------------------------------
//
// Do security critical or safe critical at a larger than method scope apply only to methods introduced
// within that scope, or to all methods conateind within the scope.
//
// For instance, if this method returns true, a critical type does not make a method it overrides critical
// because that method was introduced in a base type.
//

inline bool SecurityTransparencyBehavior::DoesScopeApplyOnlyToIntroducedMethods() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_ScopeAppliesOnlyToIntroducedMethods);
}

//---------------------------------------------------------------------------------------
//
// Do unsigned assemblies implicitly become APTCA
//

inline bool SecurityTransparencyBehavior::DoesUnsignedImplyAPTCA() const
{
    LIMITED_METHOD_CONTRACT;
    return !!(m_flags & SecurityTransparencyBehaviorFlags_UnsignedImpliesAPTCA);
}

//---------------------------------------------------------------------------------------
//
// Map the attributes found on a field into bits that represent what those attributes
// mean to this field.
//

inline FieldSecurityDescriptorFlags SecurityTransparencyBehavior::MapFieldAttributes(TokenSecurityDescriptorFlags tokenFlags) const
{
    WRAPPER_NO_CONTRACT;
    return m_pTransparencyImpl->MapFieldAttributes(tokenFlags);
}

//---------------------------------------------------------------------------------------
//
// Map the attributes found on a method to the security transparency of that method
//

inline MethodSecurityDescriptorFlags SecurityTransparencyBehavior::MapMethodAttributes(TokenSecurityDescriptorFlags tokenFlags) const
{
    WRAPPER_NO_CONTRACT;
    return m_pTransparencyImpl->MapMethodAttributes(tokenFlags);
}

//---------------------------------------------------------------------------------------
//
// Map the attributes found on an assembly into bits that represent what those
// attributes mean to this assembly.
//

inline ModuleSecurityDescriptorFlags SecurityTransparencyBehavior::MapModuleAttributes(TokenSecurityDescriptorFlags tokenFlags) const
{
    WRAPPER_NO_CONTRACT;
    return m_pTransparencyImpl->MapModuleAttributes(tokenFlags);
}

//---------------------------------------------------------------------------------------
//
// Map the attributes found on a type into bits that represent what those
// attributes mean to this type.
//

inline TypeSecurityDescriptorFlags SecurityTransparencyBehavior::MapTypeAttributes(TokenSecurityDescriptorFlags tokenFlags) const
{
    WRAPPER_NO_CONTRACT;
    return m_pTransparencyImpl->MapTypeAttributes(tokenFlags);
}

#endif // __SECURTYTRANSPARENT_INL__

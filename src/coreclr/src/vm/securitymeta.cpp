// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//--------------------------------------------------------------------------
// securitymeta.cpp
//
//pre-computes security meta information, from declarative and run-time information
//


// 
//--------------------------------------------------------------------------



#include "common.h"

#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "security.h"

#include "perfcounters.h"
#include "frames.h"
#include "dllimport.h"
#include "strongname.h"
#include "eeconfig.h"
#include "field.h"
#include "threads.h"
#include "eventtrace.h"
#include "typestring.h"
#include "securitydeclarative.h"
#include "customattribute.h"
#include "../md/compiler/custattr.h"

#include "securitymeta.h"
#include "caparser.h"

void FieldSecurityDescriptor::VerifyDataComputed()
{   
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (m_flags & FieldSecurityDescriptorFlags_IsComputed)
    {
        return;
    }


#ifdef _DEBUG
    // If we've setup a breakpoint when we compute the transparency of this field, then stop in the debugger
    // now.
    static ConfigMethodSet fieldTransparencyBreak;
    fieldTransparencyBreak.ensureInit(CLRConfig::INTERNAL_Security_TransparencyFieldBreak);
    if (fieldTransparencyBreak.contains(m_pFD->GetName(), m_pFD->GetApproxEnclosingMethodTable()->GetDebugClassName()))
    {
        DebugBreak();
    }
#endif // _DEBUG

    FieldSecurityDescriptorFlags fieldFlags = FieldSecurityDescriptorFlags_None;

    // check to see if the class has the critical attribute
    MethodTable* pMT = m_pFD->GetApproxEnclosingMethodTable();
    TypeSecurityDescriptor typeSecDesc(pMT);

    const SecurityTransparencyBehavior *pTransparencyBehavior = m_pFD->GetModule()->GetAssembly()->GetSecurityTransparencyBehavior();
    _ASSERTE(pTransparencyBehavior);

    TokenSecurityDescriptor tokenSecDesc(m_pFD->GetModule(), m_pFD->GetMemberDef());

    // If the containing type is all transparent or all critical / safe critical, then the field must also be
    // transparent or critical / safe critical.  If the type is mixed, then we need to look at the field's
    // token first to see what its transparency level is
    if (typeSecDesc.IsAllTransparent())
    {
        fieldFlags = FieldSecurityDescriptorFlags_None;
    }
    else if (typeSecDesc.IsOpportunisticallyCritical())
    {
        // Field opportunistically critical rules:
        //   Level 1 -> safe critical
        //   Level 2 -> critical
        //   If the containing type is participating in type equivalence -> transparent
        
        if (!typeSecDesc.IsTypeEquivalent())
        {
            fieldFlags |= FieldSecurityDescriptorFlags_IsCritical;

            if (typeSecDesc.IsTreatAsSafe() || pTransparencyBehavior->DoesOpportunisticRequireOnlySafeCriticalMethods())
            {
                fieldFlags |= FieldSecurityDescriptorFlags_IsTreatAsSafe;
            }
        }
    }
    else if (typeSecDesc.IsAllCritical())
    {
        fieldFlags |= FieldSecurityDescriptorFlags_IsCritical;

        if (typeSecDesc.IsTreatAsSafe())
        {
            fieldFlags |= FieldSecurityDescriptorFlags_IsTreatAsSafe;
        }
        else if (pTransparencyBehavior->CanIntroducedCriticalMembersAddTreatAsSafe() &&
                 (tokenSecDesc.GetMetadataFlags() & (TokenSecurityDescriptorFlags_TreatAsSafe | TokenSecurityDescriptorFlags_SafeCritical)))
        {
            // If the transparency model allows members introduced into a critical scope to add their own
            // TreatAsSafe attributes, then we need to look for a token level TreatAsSafe as well.
            fieldFlags |= FieldSecurityDescriptorFlags_IsTreatAsSafe;
        }
    }
    else
    {
        fieldFlags |= pTransparencyBehavior->MapFieldAttributes(tokenSecDesc.GetMetadataFlags());
    }

    // TreatAsSafe from the type we're contained in always propigates to its fields
    if ((fieldFlags & FieldSecurityDescriptorFlags_IsCritical) &&
        typeSecDesc.IsTreatAsSafe())
    {
        fieldFlags |= FieldSecurityDescriptorFlags_IsTreatAsSafe;
    }

    // If the field is public and critical, it may additionally need to be marked treat as safe
    if (pTransparencyBehavior->DoesPublicImplyTreatAsSafe() &&
        typeSecDesc.IsTypeExternallyVisibleForTransparency() &&
        (m_pFD->IsPublic() || m_pFD->IsProtected() || IsFdFamORAssem(m_pFD->GetFieldProtection())) &&
        (fieldFlags & FieldSecurityDescriptorFlags_IsCritical) &&
        !(fieldFlags & FieldSecurityDescriptorFlags_IsTreatAsSafe))
    {
        fieldFlags |= FieldSecurityDescriptorFlags_IsTreatAsSafe;
    }

    // mark computed
    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), fieldFlags | FieldSecurityDescriptorFlags_IsComputed);
}


// All callers to his method will pass in a valid memory location for pMethodSecurityDesc which they are responsible for
// free-ing when done using it. Typically this will be a stack location for perf reasons.
//
// Some details about when we cache MethodSecurityDescriptors and how the linkdemand process works:
// - When we perform the LinkTimeCheck, we follow this order of checks
//     : APTCA check
//     : Class-level declarative security using TypeSecurityDescriptor
//     : Method-level declarative security using MethodSecurityDescriptor
//     : Unmanaged-code check (if required)
//
// For APTCA and Unmanaged code checks, we don't have a permissionset entry in the hashtable that we use when performing the demand. Since
// these are well-known demands, we special-case them. What this means is that we may have a MethodSecurityDescriptor that requires a linktime check
// but does not have DeclActionInfo or TokenDeclActionInfo fields inside. 
// 
// For cases where the Type causes the Link/Inheritance demand, the MethodDesc has the flag set, but the MethodSecurityDescriptor will not have any
// DeclActionInfo or TokenDeclActionInfo.
//
// And the relevance all this has to this method is the following: Don't automatically insert a MethodSecurityDescriptor into the hash table if it has
// linktime or inheritance time check. Only do so if either of the DeclActionInfo or TokenDeclActionInfo fields are non-NULL.
void MethodSecurityDescriptor::LookupOrCreateMethodSecurityDescriptor(MethodSecurityDescriptor* ret_methSecDesc)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ret_methSecDesc));
    } CONTRACTL_END;

    _ASSERTE(CanMethodSecurityDescriptorBeCached(ret_methSecDesc->m_pMD));

    MethodSecurityDescriptor* pMethodSecurityDesc = (MethodSecurityDescriptor*)TokenSecurityDescriptor::LookupSecurityDescriptor(ret_methSecDesc->m_pMD);
    if (pMethodSecurityDesc == NULL)
    {
        ret_methSecDesc->VerifyDataComputedInternal();// compute all the data that is needed.

        // cache method security desc using some simple heuristics
        // we have some token actions computed, let us cache this method security desc

        if (ret_methSecDesc->GetRuntimeDeclActionInfo() != NULL ||
            ret_methSecDesc->GetTokenDeclActionInfo() != NULL ||
            // NGEN accesses MethodSecurityDescriptors frequently to check for security callouts
            IsCompilationProcess())
        {

            // Need to insert this methodSecDesc
            LPVOID pMem = GetAppDomain()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(MethodSecurityDescriptor)));

            // allocate a method security descriptor, using the appdomain heap memory
            pMethodSecurityDesc = new (pMem) MethodSecurityDescriptor(ret_methSecDesc->m_pMD);

            *pMethodSecurityDesc = *ret_methSecDesc; // copy over the fields

            MethodSecurityDescriptor* pExistingMethodSecurityDesc = NULL;
            // insert pMethodSecurityDesc into our hash table
            pExistingMethodSecurityDesc = reinterpret_cast<MethodSecurityDescriptor*>(TokenSecurityDescriptor::InsertSecurityDescriptor(ret_methSecDesc->m_pMD, (HashDatum) pMethodSecurityDesc));
            if (pExistingMethodSecurityDesc != NULL)
            {
                // if we found an existing method security desc, use it
                // no need to delete the one we had created, as we allocated it in the Appdomain heap
                pMethodSecurityDesc = pExistingMethodSecurityDesc;
            }
        }
    }
    else
    {
        *ret_methSecDesc = *pMethodSecurityDesc;
    }

    return;
}

BOOL MethodSecurityDescriptor::CanMethodSecurityDescriptorBeCached(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;

    return  pMD->IsInterceptedForDeclSecurity() || 
            pMD->RequiresLinktimeCheck() ||
            pMD->RequiresInheritanceCheck()||
            pMD->IsVirtual()||
            pMD->IsMethodImpl()||
            pMD->IsLCGMethod();
}

void MethodSecurityDescriptor::VerifyDataComputedInternal()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_flags & MethodSecurityDescriptorFlags_IsComputed)
    {
        return;
    }

    // If the method hasn't already cached it's transparency information, then we need to calculate it here.
    // It can be cached if we're loading the method from a native image, but are creating the security
    // descriptor in order to figure out declarative security.
    if (!m_pMD->HasCriticalTransparentInfo())
    {
        ComputeCriticalTransparentInfo();
    }

    // compute RUN-TIME DECLARATIVE SECURITY STUFF 
    // (merges both class and method level run-time declarative security info).
    if (HasRuntimeDeclarativeSecurity())
    {
        ComputeRuntimeDeclarativeSecurityInfo();
    }

    // compute method specific DECLARATIVE STUFF
    if (HasRuntimeDeclarativeSecurity() || HasLinkOrInheritanceDeclarativeSecurity())
    {
        ComputeMethodDeclarativeSecurityInfo();
    }

    // mark computed
    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), MethodSecurityDescriptorFlags_IsComputed);
}

void MethodSecurityDescriptor::ComputeCriticalTransparentInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;


    MethodTable* pMT = m_pMD->GetMethodTable();

#ifdef _DEBUG
    // If we've setup a breakpoint when we compute the transparency of this method, then stop in the debugger
    // now.
    static ConfigMethodSet methodTransparencyBreak;
    methodTransparencyBreak.ensureInit(CLRConfig::INTERNAL_Security_TransparencyMethodBreak);
    if (methodTransparencyBreak.contains(m_pMD->GetName(), pMT->GetDebugClassName()))
    {
        DebugBreak();
    }
#endif // _DEBUG

    MethodSecurityDescriptorFlags methodFlags = MethodSecurityDescriptorFlags_None;
    TypeSecurityDescriptor typeSecDesc(pMT);

    const SecurityTransparencyBehavior *pTransparencyBehavior = m_pMD->GetAssembly()->GetSecurityTransparencyBehavior();
    _ASSERTE(pTransparencyBehavior);

    // If the transparency model used by this method cares about the location of the introduced method,
    // then we need to figure out where the method was introduced.  This is only important when the type is
    // all critical or opportunistically critical, since otherwise we'll look at the method directly anyway.
    MethodDesc *pIntroducingMD = NULL;
    bool fWasIntroducedLocally = true;
    if (pTransparencyBehavior->DoesScopeApplyOnlyToIntroducedMethods() &&
        (typeSecDesc.IsOpportunisticallyCritical() || typeSecDesc.IsAllCritical()))
    {
        if (m_pMD->IsVirtual() &&
            !m_pMD->IsInterface() &&
            m_pMD->GetSlot() < m_pMD->GetMethodTable()->GetNumVirtuals())
        {
            pIntroducingMD = m_pMD->GetMethodTable()->GetIntroducingMethodDesc(m_pMD->GetSlot());
        }

        fWasIntroducedLocally = pIntroducingMD == NULL || pIntroducingMD == m_pMD;

        //
        // #OpportunisticallyCriticalMultipleImplement
        //
        // One method can be the target of multiple interfaces and also an override of a base class. Further,
        // there could be conflicting inheritance requirements; for instance overriding a critical method and
        // implementing a transparent interface with the same method desc.
        //
        // For APTCA assemblies, we require that they seperate out to explicit interface implementations to
        // solve this problem, however we cannot push this requirement to opportunistically critical
        // assemblies.  Therefore, in those assemblies we create the following non-introduced method rule:
        //
        // 1. If both the base override and all of the interfaces that a method desc is implementing have the
        //    same accessibility, then the method must agree with that accessibility.
        //
        // 2. If there is a mix of transparent accessibilities, then the method desc will be safe critical.
        //    This leads to a situation where a safe critical method can implement a critical interface,
        //    which is not a security hole, but does create some strangeness around the fact that transparent
        //    code can call the method directly but not via the interface (or base type).
        //
        //    Since there is no way for all inheritance requirements to be satisfied here, we choose to
        //    violate the overriding critical one because looking directly at the method will indicate that
        //    it is callable from transparent, whereas allowing a critical implementation of a transparent
        //    interface would create a worse situation of the method desc saying that it is not callable from
        //    transparent, while it would be via the interface.
        //
        // A variation of this problem can also occur with MethodImpls.  For example, a virtual method could
        // implement both a transparent and a critical virtual.  This case follows the same rules laid out
        // above for interface implementations.

        // We need to check the interfaces and MethodImpls if we were introduced locally, or if we're
        // opportunistically critical and the introducing method was not safe critical.
        bool fCheckInterfacesAndMethodImpls = fWasIntroducedLocally;
        if (!fCheckInterfacesAndMethodImpls && typeSecDesc.IsOpportunisticallyCritical())
        {
            _ASSERTE(pIntroducingMD != NULL);
            // Make sure the introducing method has its transparency calculated
            if (!pIntroducingMD->HasCriticalTransparentInfo())
            {
                MethodSecurityDescriptor introducingMSD(pIntroducingMD);
                introducingMSD.ComputeCriticalTransparentInfo();
            }

            // We need to keep looking at the interfaces and MethodImpls if we override a critical method. If
            // we're overriding a safe critical or transparent method, then we'll end up being safe critical
            // anyway.
            fCheckInterfacesAndMethodImpls = pIntroducingMD->IsCritical() && !pIntroducingMD->IsTreatAsSafe();
        }

        if (fCheckInterfacesAndMethodImpls &&
            !m_pMD->IsCtor() &&
            !m_pMD->IsStatic())
        {
            // Interface implementation or MethodImpl that we choose to use to calculate transparency - for
            // opportunistically critical methods, this is the first safe critical / transparent method if one
            // is found, otherwise the first critical method.  For all other methods, it is the first
            // interface / MethodImpl method found.
            MethodDesc *pSelectedMD = NULL;
                                                    
            // Iterate over the implemented methods to see if we're implementing any interfaces or virtuals
            MethodImplementationIterator implementationIterator(m_pMD);
            bool fFoundTargetMethod = false;
            for (; implementationIterator.IsValid() && !fFoundTargetMethod; implementationIterator.Next())
            {
                MethodDesc *pImplementedMD = implementationIterator.Current();

                // If we're opportunistically critical, then we need to figure out if the implemented
                // method is critical or not, and continue looking if we only found critical methods
                // to this point.
                if (typeSecDesc.IsOpportunisticallyCritical())
                {
                    // We should either have not found a candidate yet, or that candidate should be critical
                    _ASSERTE(pSelectedMD == NULL ||
                             (pSelectedMD->IsCritical() && !pSelectedMD->IsTreatAsSafe()));

                    if (!pImplementedMD->HasCriticalTransparentInfo())
                    {
                        MethodSecurityDescriptor implementedMSD(pImplementedMD);
                        implementedMSD.ComputeCriticalTransparentInfo();
                    }

                    // If this is the first interface method or MethodImpl we've seen, save it away. Otherwise,
                    // we've so far implemented only critical interfaces and methods, so if we see a
                    // transparent or safe critical interface method, we should note that and stop looking
                    // further.
                    if (!pImplementedMD->IsCritical() || pImplementedMD->IsTreatAsSafe())
                    {
                        pSelectedMD = pImplementedMD;
                        fFoundTargetMethod = true;
                    }
                    else if (pSelectedMD == NULL)
                    {
                        pSelectedMD = pImplementedMD;
                    }
                }
                else
                {
                    // If we're not opportunistically critical, then we only care about the first interface
                    // implementation or MethodImpl that we see.
                    _ASSERTE(pSelectedMD == NULL);
                    pSelectedMD = pImplementedMD;
                    fFoundTargetMethod = true;
                }
            }

            // If we found an interface method or MethodImpl, then use that as the introducing method
            if (pSelectedMD != NULL)
            {
                pIntroducingMD = pSelectedMD;
                fWasIntroducedLocally = false;
            }
        }

        // If we're not working with a method that we introduced, make sure it has its transparency calculated
        // before we need to use it.
        if (!fWasIntroducedLocally && !pIntroducingMD->HasCriticalTransparentInfo())
        {
            MethodSecurityDescriptor introducingMSD(pIntroducingMD);
            introducingMSD.ComputeCriticalTransparentInfo();
            _ASSERTE(pIntroducingMD->HasCriticalTransparentInfo());
        }
    }

    // In a couple of cases we know the transparency of the method directly:
    //  1. If our parent type is all transparent, we must also be transparent
    //  2. If we're opprotunstically critical, then we can figure out the annotation based upon the override
    //  3. If our parent type is all critical, and we were introduced by that type, we must also be critical
    //     (we could also be safe critical as well).
    //
    // Otherwise, we need to ask the current transparency implementation what this method is, because it
    // will vary depending upon if we're in legacy mode or not.
    TokenSecurityDescriptor methodTokenSecDesc(m_pMD->GetModule(), GetToken());
    if (typeSecDesc.IsAllTransparent())
    {
        methodFlags = MethodSecurityDescriptorFlags_None;
    }
    else if (typeSecDesc.IsOpportunisticallyCritical())
    {
        // Opportunistically critical methods will always be critical
        methodFlags |= MethodSecurityDescriptorFlags_IsCritical;

        // If we're overriding a safe critical or transparent method, we also need to be treat as safe
        //
        // Virtuals on value types have multiple entries in the method table, so we may not have mapped
        // it back to the override that it was implementing.  In order to compensate for this, we simply
        // allow all virtuals in opportunistically critical value types to be safe critical.  This doesn't
        // introduce any extra risk, because unless we're overriding one of the Object overloads, there is
        // nothing that transparent code can cast the ValueType to in order to access the virtual since the
        // value type itself will be critical.
        //
        // If we're in a transparency model where all opportunistically critical methods are safe critical, we
        // need to add the treat as safe bit.
        // 
        // Finally, if we're in a type participating in type equivalence, then we need to add the treat as
        // safe bit.  This keeps the transparency of methods in type equivalent interfaces consistent across
        // security rule sets in opportunistically critical assemblies, which allows types from v2 PIAs to
        // be embedded successfully into v4 assemblies for instance.
        if (!fWasIntroducedLocally &&
            (!pIntroducingMD->IsCritical() || pIntroducingMD->IsTreatAsSafe()))
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
        else if (pMT->IsValueType() && m_pMD->IsVirtual())
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
        else if (pTransparencyBehavior->DoesOpportunisticRequireOnlySafeCriticalMethods())
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
        else if (typeSecDesc.IsTypeEquivalent())
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
    }
    else if (typeSecDesc.IsAllCritical() && fWasIntroducedLocally)
    {
        methodFlags |= MethodSecurityDescriptorFlags_IsCritical;

        if (typeSecDesc.IsTreatAsSafe())
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
        else if (pTransparencyBehavior->CanIntroducedCriticalMembersAddTreatAsSafe() &&
                 (methodTokenSecDesc.GetMetadataFlags() & (TokenSecurityDescriptorFlags_TreatAsSafe | TokenSecurityDescriptorFlags_SafeCritical)))
        {
            // If the transparency model allows members introduced into a critical scope to add their own
            // TreatAsSafe attributes, then we need to look for a token level TreatAsSafe as well.
            methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
    }
    else
    {
        // We don't have a larger scope that tells us what to do with the method, so ask the transparency
        // implementation to map our attributes to a set of flags
        methodFlags |= pTransparencyBehavior->MapMethodAttributes(methodTokenSecDesc.GetMetadataFlags());
    }

    // TreatAsSafe from the type we're contained in always propigates to its methods
    if (fWasIntroducedLocally &&
        (methodFlags & MethodSecurityDescriptorFlags_IsCritical) &&
        typeSecDesc.IsTreatAsSafe())
    {
        methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
    }

    // The compiler can introduce default constructors implicitly, and for an explicitly critical type they
    // will always be transparent - resulting in a type load exception.  If we are a transparent default .ctor
    // of an explicitly critical type, then we'll switch to being safe critical to allow the type to load and
    // allow us access to our this pointer
    if (!typeSecDesc.IsAllCritical() &&
        typeSecDesc.IsCritical() &&
        !(methodFlags & MethodSecurityDescriptorFlags_IsCritical) &&
        m_pMD->IsCtor())
    {
        if (pMT->HasDefaultConstructor() &&
            pMT->GetDefaultConstructor() == m_pMD)
        {
            methodFlags |= MethodSecurityDescriptorFlags_IsCritical |
                           MethodSecurityDescriptorFlags_IsTreatAsSafe;
        }
    }

    // See if we're a public critical method, then we may need to additionally make ourselves treat as safe
    if (pTransparencyBehavior->DoesPublicImplyTreatAsSafe() &&
        typeSecDesc.IsTypeExternallyVisibleForTransparency() &&
        (m_pMD->IsPublic() || m_pMD->IsProtected() || IsMdFamORAssem(m_pMD->GetAttrs())) &&
        (methodFlags & MethodSecurityDescriptorFlags_IsCritical) &&
        !(methodFlags & MethodSecurityDescriptorFlags_IsTreatAsSafe))
    {
        methodFlags |= MethodSecurityDescriptorFlags_IsTreatAsSafe;
    }

    // Cache our state on the MethodDesc
    m_pMD->SetCriticalTransparentInfo(methodFlags & MethodSecurityDescriptorFlags_IsCritical,
                                      methodFlags & MethodSecurityDescriptorFlags_IsTreatAsSafe);
}

void MethodSecurityDescriptor::ComputeRuntimeDeclarativeSecurityInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Load declarative security attributes
    _ASSERTE(HasRuntimeDeclarativeSecurity());
    m_declFlagsDuringPreStub = m_pMD->GetSecurityFlagsDuringPreStub();
    _ASSERTE(m_declFlagsDuringPreStub && " Expected some runtime security action");
    m_pRuntimeDeclActionInfo = SecurityDeclarative::DetectDeclActions(m_pMD, m_declFlagsDuringPreStub);
}

void MethodSecurityDescriptor::ComputeMethodDeclarativeSecurityInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DWORD flags = 0;

    _ASSERTE(HasRuntimeDeclarativeSecurity()|| HasLinkOrInheritanceDeclarativeSecurity());
    DWORD dwDeclFlags;
    HRESULT hr = SecurityDeclarative::GetDeclarationFlags(GetIMDInternalImport(), GetToken(), &dwDeclFlags, NULL, NULL);

    if (SUCCEEDED(hr))
    {
        GCX_COOP();
        PsetCacheEntry *tokenSetIndexes[dclMaximumValue + 1];
        SecurityDeclarative::DetectDeclActionsOnToken(GetToken(), dwDeclFlags, tokenSetIndexes, GetIMDInternalImport());

        // Create single linked list of set indexes
        DWORD dwLocalAction;
        bool builtInCASPermsOnly = TRUE;
        for (dwLocalAction = 0; dwLocalAction <= dclMaximumValue; dwLocalAction++)
        {
            if (tokenSetIndexes[dwLocalAction] != NULL)
            {
                TokenDeclActionInfo::LinkNewDeclAction(&m_pTokenDeclActionInfo, (CorDeclSecurity)dwLocalAction, tokenSetIndexes[dwLocalAction]);
                builtInCASPermsOnly = builtInCASPermsOnly && (tokenSetIndexes[dwLocalAction]->ContainsBuiltinCASPermsOnly(dwLocalAction));
            }
        }

        if (builtInCASPermsOnly)
            flags |= MethodSecurityDescriptorFlags_IsBuiltInCASPermsOnly;
        SecurityProperties sp(dwDeclFlags);
        if (sp.FDemandsOnly())
            flags |= MethodSecurityDescriptorFlags_IsDemandsOnly;
        if (sp.FAssertionsExist())
        {
            // Do a check to see if the assembly has been granted permission to assert and let's cache that value in the MethodSecurityDesriptor
            Module* pModule = m_pMD->GetModule();
            PREFIX_ASSUME_MSG(pModule != NULL, "Should be a Module pointer here");

            if (Security::CanAssert(pModule))
            {
                flags |= MethodSecurityDescriptorFlags_AssertAllowed;
            }
        }
    }

    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), flags);
}

void MethodSecurityDescriptor::InvokeInheritanceChecks(MethodDesc *pChildMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pChildMD));
    }
    CONTRACTL_END;

    const SecurityTransparencyBehavior *pTransparencyBehavior = pChildMD->GetAssembly()->GetSecurityTransparencyBehavior();
    if (pTransparencyBehavior->AreInheritanceRulesEnforced() && Security::IsTransparencyEnforcementEnabled())
    {
        // The profiler may want to suppress these checks if it's currently running on the child type
        if (Security::BypassSecurityChecksForProfiler(pChildMD))
        {
            return;
        }

        /*
        Allowed Inheritance Patterns (cannot change accessibility)
        ----------------------------

        Base Class/Method   Derived Class/ Method
        -----------------   ---------------------
        Transparent     Transparent
        Transparent     SafeCritical
        SafeCritical    SafeCritical
        SafeCritical    Transparent
        Critical        Critical


        Disallowed Inheritance patterns
        -------------------------------

        Base Class/Method   Derived Class /Method
        -----------------   ---------------------
        Transparent     Critical
        SafeCritical    Critical
        Critical        Transparent
        Critical        SafeCritical
        */

        MethodSecurityDescriptor methSecurityDescriptor(pChildMD, FALSE);
        TokenSecurityDescriptor methTokenSecurityDescriptor(pChildMD->GetModule(), pChildMD->GetMemberDef());
        if (IsCritical())
        {
            if (IsTreatAsSafe())
            {
                // Base: SafeCritical. Check if Child is Critical
                if (methSecurityDescriptor.IsCritical() && !methSecurityDescriptor.IsTreatAsSafe())
                {
#ifdef _DEBUG
                    if (g_pConfig->LogTransparencyErrors())
                    {
                        SecurityTransparent::LogTransparencyError(pChildMD, "Critical method overriding a SafeCritical base method", m_pMD);
                    }
#endif // _DEBUG
                    SecurityTransparent::ThrowTypeLoadException(pChildMD);
                }
            }
            else
            {
                // Base: Critical. 
                if (!methSecurityDescriptor.IsCritical()) 
                {
                    // Child is transparent
                    // throw
#ifdef _DEBUG
                    if (g_pConfig->LogTransparencyErrors())
                    {
                        SecurityTransparent::LogTransparencyError(pChildMD, "Transparent method overriding a critical base method", m_pMD);
                    }
#endif // _DEBUG
                    SecurityTransparent::ThrowTypeLoadException(pChildMD);
                }
                else if (methSecurityDescriptor.IsTreatAsSafe() && !methSecurityDescriptor.IsOpportunisticallyCritical())
                {
                    // The child is safe critical and not opportunistically critical (see code:#OpportunisticallyCriticalMultipleImplement)
                    // throw.
#ifdef _DEBUG
                    if (g_pConfig->LogTransparencyErrors())
                    {
                        SecurityTransparent::LogTransparencyError(pChildMD, "Safe critical method overriding a SafeCritical base method", m_pMD);
                    }
#endif // _DEBUG
                    SecurityTransparent::ThrowTypeLoadException(pChildMD);
                }
            }
        }
        else
        {
            // Base: Transparent. Throw if derived is Critical and not SafeCritical
            if (methSecurityDescriptor.IsCritical() && !methSecurityDescriptor.IsTreatAsSafe())
            {
#ifdef _DEBUG
                if (g_pConfig->LogTransparencyErrors())
                {
                    SecurityTransparent::LogTransparencyError(pChildMD, "Critical method overriding a transparent base method", m_pMD);
                }
#endif // _DEBUG
                SecurityTransparent::ThrowTypeLoadException(pChildMD);
            }
        }
    }

}

MethodSecurityDescriptor::MethodImplementationIterator::MethodImplementationIterator(MethodDesc *pMD)
    : m_interfaceIterator(pMD->GetMethodTable()),
      m_pMD(pMD),
      m_iMethodImplIndex(0),
      m_fInterfaceIterationBegun(false),
      m_fMethodImplIterationBegun(false)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(pMD != NULL);
    }
    CONTRACTL_END;

    Next();
}

MethodDesc *MethodSecurityDescriptor::MethodImplementationIterator::Current()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(IsValid());
    }
    CONTRACTL_END;

    if (m_pMD->GetMethodTable()->HasDispatchMap() && m_interfaceIterator.IsValid())
    {
        _ASSERTE(m_fInterfaceIterationBegun);
        MethodTable *pInterface = m_pMD->GetMethodTable()->LookupDispatchMapType(m_interfaceIterator.Entry()->GetTypeID());
        return pInterface->GetMethodDescForSlot(m_interfaceIterator.Entry()->GetSlotNumber());
    }
    else
    {
        _ASSERTE(m_fMethodImplIterationBegun);
        _ASSERTE(m_pMD->IsMethodImpl());
        _ASSERTE(m_iMethodImplIndex < m_pMD->GetMethodImpl()->GetSize());
        return m_pMD->GetMethodImpl()->GetImplementedMDs()[m_iMethodImplIndex];
    }
}

bool MethodSecurityDescriptor::MethodImplementationIterator::IsValid()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    // We're valid as long as we still have interface maps or method impls to process
    if (m_pMD->GetMethodTable()->HasDispatchMap() && m_interfaceIterator.IsValid())
    {
        return true;
    }
    else if (m_pMD->IsMethodImpl())
    {
        return m_iMethodImplIndex < m_pMD->GetMethodImpl()->GetSize();
    }
    else
    {
        return false;
    }
}

void MethodSecurityDescriptor::MethodImplementationIterator::Next()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    bool fFoundImpl = false;

    // First iterate over the interface implementations
    if (m_pMD->GetMethodTable()->HasDispatchMap() && m_interfaceIterator.IsValid())
    {
        while (m_interfaceIterator.IsValid() && !fFoundImpl)
        {
            // If we haven't yet begun iterating interfaces then don't call Next right away - otherwise
            // we'll potentially skip over the first interface method.
            if (m_fInterfaceIterationBegun)
            {
                m_interfaceIterator.Next();
            }
            else
            {
                m_fInterfaceIterationBegun = true;
            }

            if (m_interfaceIterator.IsValid())
            {
                _ASSERTE(!m_interfaceIterator.Entry()->GetTypeID().IsThisClass());
                fFoundImpl = (m_interfaceIterator.Entry()->GetTargetSlotNumber() == m_pMD->GetSlot());
            }
        }
    }

    // Once we're done with the interface implementations, check for a MethodImpl
    if (!fFoundImpl && m_pMD->IsMethodImpl())
    {
        MethodImpl * pMethodImpl = m_pMD->GetMethodImpl();
        while ((m_iMethodImplIndex < pMethodImpl->GetSize()) && !fFoundImpl)
        {
            // If we haven't yet begun iterating method impls then don't move to the next element right away
            // - otehrwise we'll potentially skip over the first MethodImpl
            if (m_fMethodImplIterationBegun)
            {
                ++m_iMethodImplIndex;
            }
            else
            {
                m_fMethodImplIterationBegun = true;
            }

            if (m_iMethodImplIndex < pMethodImpl->GetSize())
            {
                // Skip over the interface MethodImpls since we already processed those
                fFoundImpl = !pMethodImpl->GetImplementedMDs()[m_iMethodImplIndex]->IsInterface();
            }
        }
    }
} // MethodSecurityDescriptor::MethodImplementationIterator::Next

TypeSecurityDescriptor* TypeSecurityDescriptor::GetTypeSecurityDescriptor(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    TypeSecurityDescriptor* pTypeSecurityDesc =NULL;


    pTypeSecurityDesc = (TypeSecurityDescriptor*)TokenSecurityDescriptor::LookupSecurityDescriptor(pMT);
    if (pTypeSecurityDesc == NULL)
    {
        // didn't find a  security descriptor, create one and insert it
        LPVOID pMem = GetAppDomain()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(TypeSecurityDescriptor)));

        // allocate a  security descriptor, using the appdomain help memory
        pTypeSecurityDesc = new (pMem) TypeSecurityDescriptor(pMT);
        pTypeSecurityDesc->VerifyDataComputedInternal(); // compute all the data that is needed.

        TypeSecurityDescriptor* pExistingTypeSecurityDesc = NULL;
        // insert securitydesc into our hash table
        pExistingTypeSecurityDesc = (TypeSecurityDescriptor*)TokenSecurityDescriptor::InsertSecurityDescriptor(pMT, (HashDatum) pTypeSecurityDesc);                        
        if (pExistingTypeSecurityDesc != NULL)
        {
            // if we found an existing  security desc, use it
            // no need to delete the one we had created, as we allocated it in the Appdomain help
            pTypeSecurityDesc = pExistingTypeSecurityDesc;
        }
    }

    return pTypeSecurityDesc;
}


void TypeSecurityDescriptor::ComputeCriticalTransparentInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;


#ifdef _DEBUG
    // If we've setup a breakpoint when we compute the transparency of this type, then stop in the debugger now
    SString strTypeTransparencyBreak(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Security_TransparencyTypeBreak));
    SString strClassName(SString::Utf8, m_pMT->GetDebugClassName());
    if (strTypeTransparencyBreak.EqualsCaseInsensitive(strClassName))
    {
        // Do not break in fuzzed assemblies where class name can be empty
        if (!strClassName.IsEmpty())
        {
            DebugBreak();
        }
    }
#endif // _DEBUG
    
    // check to see if the assembly has the critical attribute
    Assembly* pAssembly = m_pMT->GetAssembly();
    _ASSERTE(pAssembly);
    ModuleSecurityDescriptor* pModuleSecDesc = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pAssembly);
    pModuleSecDesc->VerifyDataComputed();

    EEClass *pClass = m_pMT->GetClass();
    TypeSecurityDescriptorFlags typeFlags = TypeSecurityDescriptorFlags_None;

    // If we're contained within another type, then we inherit the transparency of that type.  Otherwise we
    // check the module to see what type of transparency we have.
    if (pClass->IsNested())
    {
        // If the type is nested, see if the outer class tells us what our transparency is. Note that we cannot
        // use a TypeSecurityDescriptor here since we may still be in the process of loading our outer type.
        TokenSecurityDescriptor enclosingTokenSecurityDescriptor(m_pMT->GetModule(), m_pMT->GetEnclosingCl());
        if (enclosingTokenSecurityDescriptor.IsSemanticCritical())
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsAllCritical;
        }

        // We want to propigate the TreatAsSafe bit even if the outer class is not critical because in the legacy
        // transparency model you could have a TAS but not critical type, and the TAS propigated to all nested
        // types.
        if (enclosingTokenSecurityDescriptor.IsSemanticTreatAsSafe())
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsTreatAsSafe;
        }
    }

    const SecurityTransparencyBehavior *pTransparencyBehavior = m_pMT->GetAssembly()->GetSecurityTransparencyBehavior();
    _ASSERTE(pTransparencyBehavior);

    // If we're not nested, or if the outer type didn't give us enough information to determine what we were,
    // then we need to look at the module to see what we are.
    if (typeFlags == TypeSecurityDescriptorFlags_None)
    {
        if (pModuleSecDesc->IsAllTransparent())
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsAllTransparent;
        }
        else if (pModuleSecDesc->IsOpportunisticallyCritical())
        {
            // In level 1 transparency, opportunistically critical types are transparent, in level 2 they
            // are critical.  However, this causes problems when doing type equivalence between levels (for
            // instance a type from a v2 PIA which was embedded into a v4 assembly).  In order to allow type
            // equivalence to work across security rule sets, we consider all types participating in
            // equivalence to be transparent under the opportunistically critical rules:
            //   Participating in equivalence -> Transparent
            //   Level 1 -> Transparent
            //   Level 2 -> All critical
            if (!pTransparencyBehavior->DoesOpportunisticRequireOnlySafeCriticalMethods() &&
                !IsTypeEquivalent())
            {
                typeFlags |= TypeSecurityDescriptorFlags_IsAllCritical;
            }
        }
        else if (pModuleSecDesc->IsAllCritical())
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsAllCritical;
            if (pModuleSecDesc->IsTreatAsSafe())
            {
                typeFlags |= TypeSecurityDescriptorFlags_IsTreatAsSafe;
            }
        }
    }

    // We need to look at the type token for more information if we still don't know if we're transparent or
    // critical.  This can also happen if the type is in an opportunistically critical module, however the
    // transparency model requires opportunistically critical types to be transparent.  In this case, we need
    // to make sure that we do not look at the metadata token.
    TokenSecurityDescriptor classTokenSecurityDescriptor(m_pMT->GetModule(),
                                                         m_pMT->GetCl());

    const TypeSecurityDescriptorFlags transparencyMask = TypeSecurityDescriptorFlags_IsCritical |
                                                         TypeSecurityDescriptorFlags_IsAllCritical |
                                                         TypeSecurityDescriptorFlags_IsAllTransparent;

    if (!(typeFlags & transparencyMask) &&
        !pModuleSecDesc->IsOpportunisticallyCritical())
    {
        // First, ask the transparency behavior implementation to map from the metadata attributes to the real
        // behavior that we should be seeing.
        typeFlags |= pTransparencyBehavior->MapTypeAttributes(classTokenSecurityDescriptor.GetMetadataFlags());

        // If we still don't know what the transparency of the type is, then we're transparent, but not all
        // transparent.  That implies that we're in a mixed assembly.
        _ASSERTE((typeFlags & transparencyMask) || pModuleSecDesc->IsMixedTransparency());
    }

    // If the transparency behavior dictates that publics must be safe critical, then also set the treat as safe bit.
    if (pTransparencyBehavior->DoesPublicImplyTreatAsSafe() &&
        ((typeFlags & TypeSecurityDescriptorFlags_IsCritical) || (typeFlags & TypeSecurityDescriptorFlags_IsAllCritical)) &&
        !(typeFlags & TypeSecurityDescriptorFlags_IsTreatAsSafe))
    {
        if (IsTypeExternallyVisibleForTransparency())
        {
            typeFlags |= TypeSecurityDescriptorFlags_IsTreatAsSafe;
        }
    }

    // It is common for a v2 assembly to mark a delegate type as explicitly critical rather than all critical,
    // since in C# the syntax for creating a delegate type does not make it obvious that a new type is being
    // defined.  That leads to situations where we commonly have critical types with transparent memebers -
    // a nonsense scenario that we reject due to the members not having access to their own this pointer.
    //
    // For compatibility, we implicitly convert all explicitly critical delegate types into all critical
    // types, which is likely what the code intended in the first place, and allows delegate types which
    // loaded on v2.0 to continue to load on future runtimes.
    //
    // Note: While loading BCL classes, we may be running this codepath before it is safe to call MethodTable::IsDelegate.
    // That call can only happen after CLASS__MULTICASTDELEGATE has been loaded. However, we should not have any
    // explicit critical Delegate types in mscorlib (that can only happen if you're loading v2.0 assembly or have SecurityScope.Explicit).
    if ((typeFlags & TypeSecurityDescriptorFlags_IsCritical) &&
        !(typeFlags & TypeSecurityDescriptorFlags_IsAllCritical) &&
        m_pMT->IsDelegate())
    {
        typeFlags |= TypeSecurityDescriptorFlags_IsAllCritical;
    }

    // Update the cached values in the EE Class.
    g_IBCLogger.LogEEClassCOWTableAccess(m_pMT);
    pClass->SetCriticalTransparentInfo(
                                        typeFlags & TypeSecurityDescriptorFlags_IsTreatAsSafe,
                                        typeFlags & TypeSecurityDescriptorFlags_IsAllTransparent,
                                        typeFlags & TypeSecurityDescriptorFlags_IsAllCritical);
}

void TypeSecurityDescriptor::ComputeTypeDeclarativeSecurityInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // if method doesn't have any security return
    if (!IsTdHasSecurity(m_pMT->GetAttrClass()))
    {
        return;
    }

    DWORD dwDeclFlags;
    HRESULT hr = SecurityDeclarative::GetDeclarationFlags(GetIMDInternalImport(), GetToken(), &dwDeclFlags, NULL, NULL);

    if (SUCCEEDED(hr))
    {
        GCX_COOP();
        PsetCacheEntry *tokenSetIndexes[dclMaximumValue + 1];
        SecurityDeclarative::DetectDeclActionsOnToken(GetToken(), dwDeclFlags, tokenSetIndexes, GetIMDInternalImport());

        // Create single linked list of set indexes
        DWORD dwLocalAction;
        for (dwLocalAction = 0; dwLocalAction <= dclMaximumValue; dwLocalAction++)
        {
            if (tokenSetIndexes[dwLocalAction] != NULL)
            {
                TokenDeclActionInfo::LinkNewDeclAction(&m_pTokenDeclActionInfo,
                                                       (CorDeclSecurity)dwLocalAction,
                                                       tokenSetIndexes[dwLocalAction]);
            }
        }
    }
}

BOOL TypeSecurityDescriptor::CanTypeSecurityDescriptorBeCached(MethodTable* pMT)
{
    LIMITED_METHOD_CONTRACT;

    EEClass *pClass = pMT->GetClass();
    return  pClass->RequiresLinktimeCheck() ||
            pClass->RequiresInheritanceCheck() ||
            // NGEN accesses security descriptors frequently to check for security callouts
            IsCompilationProcess();
}

BOOL TypeSecurityDescriptor::IsTypeExternallyVisibleForTransparency()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pMT->GetAssembly()->GetSecurityTransparencyBehavior()->DoesPublicImplyTreatAsSafe());
    }
    CONTRACTL_END;

    if (m_pMT->IsExternallyVisible())
    {
        // If the type is genuinely externally visible, then it is also visible for transparency
        return TRUE;
    }
    else if (m_pMT->IsGlobalClass())
    {
        // Global methods are externally visible
        return TRUE;
    }
    else if (m_pMT->IsSharedByGenericInstantiations())
    {
        TokenSecurityDescriptor tokenSecDesc(m_pMT->GetModule(), m_pMT->GetCl());

        // Canonical method tables for shared generic instantiations will appear to us as
        // GenericClass<__Canon>, rather than the actual generic type parameter, and since __Canon is not
        // public, these method tables will not appear to be public either.
        //
        // For these types, we'll look at the metadata directly, and ignore generic parameters to see
        // if the type is public.  Note that this will under-enforce; for instance G<CriticalRefType> will
        // have it's G<__Canon> calls refered to as safe critical (which is necessary, since G<__Canon>
        // is also the canonical representation for G<TransparentRefType>.  We rely on the checks done by
        // CheckTransparentAccessToCriticalCode in the CanAccess code path to reject any attempts to use
        // the generic type over a critical parameter.
        if (tokenSecDesc.IsSemanticExternallyVisible())
        {
            return TRUE;
        }
    }
    
    return FALSE;
}

void TypeSecurityDescriptor::VerifyDataComputedInternal()
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

    // If the type hasn't already cached it's transparency information, then we need to calculate it here. It
    // can be cached if we're loading the type from a native image, but are creating the security descriptor
    // in order to figure out declarative security.
    if (!m_pMT->GetClass()->HasCriticalTransparentInfo())
    {
        ComputeCriticalTransparentInfo();
    }

    // COMPUTE Type DECLARATIVE SECURITY INFO
    ComputeTypeDeclarativeSecurityInfo();

    // mark computed
    InterlockedCompareExchange(reinterpret_cast<LONG *>(&m_fIsComputed), TRUE, FALSE);
}

void TypeSecurityDescriptor::InvokeInheritanceChecks(MethodTable* pChildMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pChildMT));
    }
    CONTRACTL_END;

    const SecurityTransparencyBehavior *pChildTransparencyBehavior = pChildMT->GetAssembly()->GetSecurityTransparencyBehavior();
    if (pChildTransparencyBehavior->AreInheritanceRulesEnforced() && Security::IsTransparencyEnforcementEnabled())
    {
        // We compare the child class with the most critical base class in the type hierarchy.
        //
        // We can stop walking the inheritance chain if we find a type that also enforces inheritance rules,
        // since we know that it must be at least as critical as the most critical of all its base types.
        // Similarly, we can stop walking when we find a critical parent, because we know that this is the
        // most critical we can get.
        bool fFoundCriticalParent = false;
        bool fFoundSafeCriticalParent = false;
        bool fFoundParentWithEnforcedInheritance = false;

        for (MethodTable *pParentMT = m_pMT;
             pParentMT != NULL && !fFoundParentWithEnforcedInheritance && !fFoundCriticalParent;
             pParentMT = pParentMT->GetParentMethodTable())
        {
            EEClass *pParentClass = pParentMT->GetClass();

            // Make sure this parent class has its transparency information computed
            if (!pParentClass->HasCriticalTransparentInfo())
            {
                TypeSecurityDescriptor parentSecurityDescriptor(pParentMT);
                parentSecurityDescriptor.ComputeCriticalTransparentInfo();
            }

            // See if it is critical or safe critical
            if (pParentClass->IsCritical() && pParentClass->IsTreatAsSafe())
            {
                fFoundSafeCriticalParent = true;
            }
            else if (pParentClass->IsCritical() && !pParentClass->IsTreatAsSafe())
            {
                fFoundCriticalParent = true;
            }

            // If this parent class enforced transparency, we can stop looking at further parents
            const SecurityTransparencyBehavior *pParentTransparencyBehavior = pParentMT->GetAssembly()->GetSecurityTransparencyBehavior();
            fFoundParentWithEnforcedInheritance = pParentTransparencyBehavior->AreInheritanceRulesEnforced();
        }        

        /*
        Allowed Inheritance Patterns
        ----------------------------

        Base Class/Method   Derived Class/ Method
        -----------------   ---------------------
        Transparent     Transparent
        Transparent     SafeCritical
        Transparent     Critical
        SafeCritical    SafeCritical
        SafeCritical    Critical
        Critical        Critical


        Disallowed Inheritance patterns
        -------------------------------

        Base Class/Method   Derived Class /Method
        -----------------   ---------------------
        SafeCritical    Transparent
        Critical        Transparent
        Critical        SafeCritical
        */

        // Make sure the child class has its transparency calculated
        EEClass *pChildClass = pChildMT->GetClass();
        if (!pChildClass->HasCriticalTransparentInfo())
        {
            TypeSecurityDescriptor childSecurityDescriptor(pChildMT);
            childSecurityDescriptor.ComputeCriticalTransparentInfo();
        }

        if (fFoundCriticalParent)
        {
            if (!pChildClass->IsCritical() || pChildClass->IsTreatAsSafe())
            {
#ifdef _DEBUG
                if (g_pConfig->LogTransparencyErrors())
                {
                    SecurityTransparent::LogTransparencyError(pChildMT, "Transparent or safe critical type deriving from a critical base type");
                }
#endif // _DEBUG
                // The parent class is critical, but the child class is not
                SecurityTransparent::ThrowTypeLoadException(pChildMT);
            }
        }
        else if (fFoundSafeCriticalParent)
        {
            if (!pChildClass->IsCritical())
            {
#ifdef _DEBUG
                if (g_pConfig->LogTransparencyErrors())
                {
                    SecurityTransparent::LogTransparencyError(pChildMT, "Transparent type deriving from a safe critical base type");
                }
#endif // _DEBUG
                // The parent class is safe critical, but the child class is transparent
                SecurityTransparent::ThrowTypeLoadException(pChildMT);
            }
        }
    }

}

// Module security descriptor contains static security information about the module
// this information could get persisted in the NGen image
void ModuleSecurityDescriptor::VerifyDataComputed()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (m_flags & ModuleSecurityDescriptorFlags_IsComputed)
    {
        return;    
    }


    // Read the security attributes from the assembly
    Assembly *pAssembly = m_pModule->GetAssembly();

    // Get the metadata flags on the assembly.  Note that we cannot use a TokenSecurityDescriptor directly
    // here because Reflection.Emit may have overriden the metadata flags with different ones of its own
    // choosing.
    TokenSecurityDescriptorFlags tokenFlags = GetTokenFlags();



    // Get a transparency behavior object for the assembly.
    const SecurityTransparencyBehavior *pTransparencyBehavior =
        SecurityTransparencyBehavior::GetTransparencyBehavior(GetSecurityRuleSet());
    pAssembly->SetSecurityTransparencyBehavior(pTransparencyBehavior);

    ModuleSecurityDescriptorFlags moduleFlags = pTransparencyBehavior->MapModuleAttributes(tokenFlags);

    AssemblySecurityDescriptor *pAssemSecDesc = static_cast<AssemblySecurityDescriptor*>(pAssembly->GetSecurityDescriptor());

    // We shouldn't be both all transparent and all critical
    const ModuleSecurityDescriptorFlags invalidMask = ModuleSecurityDescriptorFlags_IsAllCritical |
                                                      ModuleSecurityDescriptorFlags_IsAllTransparent;
    if ((moduleFlags & invalidMask) == invalidMask)
    {
#ifdef _DEBUG
        if (g_pConfig->LogTransparencyErrors())
        {
            SecurityTransparent::LogTransparencyError(pAssembly, "Found both critical and transparent assembly level annotations");
        }
        if (!g_pConfig->DisableTransparencyEnforcement())
#endif // _DEBUG
        {
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_CriticalTransparentAreMutuallyExclusive"));
        }
    }

    const ModuleSecurityDescriptorFlags transparencyMask = ModuleSecurityDescriptorFlags_IsAllCritical |
                                                           ModuleSecurityDescriptorFlags_IsAllTransparent |
                                                           ModuleSecurityDescriptorFlags_IsTreatAsSafe |
                                                           ModuleSecurityDescriptorFlags_IsOpportunisticallyCritical;

    // See if the assembly becomes implicitly transparent if loaded in partial trust
    if (pTransparencyBehavior->DoesPartialTrustImplyAllTransparent())
    {
        if (!pAssemSecDesc->IsFullyTrusted())
        {
            moduleFlags &= ~transparencyMask;
            moduleFlags |= ModuleSecurityDescriptorFlags_IsAllTransparent;

            moduleFlags |= ModuleSecurityDescriptorFlags_TransparentDueToPartialTrust;

            SString strAssemblyName;
            pAssembly->GetDisplayName(strAssemblyName);
            LOG((LF_SECURITY,
                 LL_INFO10,
                 "Assembly '%S' was loaded in partial trust and was made implicitly all transparent.\n",
                 strAssemblyName.GetUnicode()));
        }
    }

    // If the assembly is not allowed to use the SkipVerificationInFullTrust optimization, then disable that bit
    if (!pAssembly->GetSecurityDescriptor()->AllowSkipVerificationInFullTrust())
    {
        moduleFlags &= ~ModuleSecurityDescriptorFlags_SkipFullTrustVerification;
    }

    // Make sure that if the assembly is being loaded in partial trust that it is all transparent.  This is a
    // change from v2.0 rules, and for compatibility we use the DoesPartialTrustImplyAllTransparent check to
    // ensure that v2 assemblies can load in partial trust unmodified.  This change does allow us to follow
    // the CoreCLR model of using transparency for security enforcement, rather than the v2.0 model of using
    // transparency only for audit.
    if (!pAssembly->GetSecurityDescriptor()->IsFullyTrusted() &&
        !(moduleFlags & ModuleSecurityDescriptorFlags_IsAllTransparent))
    {
        SString strAssemblyName;
        pAssembly->GetDisplayName(strAssemblyName);

#ifdef _DEBUG
        if (g_pConfig->LogTransparencyErrors())
        {
            SecurityTransparent::LogTransparencyError(pAssembly, "Attempt to load an assembly which is not fully transparent in partial trust");
        }
        if (g_pConfig->DisableTransparencyEnforcement())
        {
            SecurityTransparent::LogTransparencyError(pAssembly, "Forcing partial trust assembly to be fully transparent");
            if (!pAssembly->GetSecurityDescriptor()->IsFullyTrusted())
            {
                moduleFlags &= ~transparencyMask;
                moduleFlags |= ModuleSecurityDescriptorFlags_IsAllTransparent;

            }
        }
        else
#endif // _DEBUG
        {
            COMPlusThrow(kFileLoadException, IDS_E_LOAD_CRITICAL_IN_PARTIAL_TRUST, strAssemblyName.GetUnicode());
        }
    }


#ifdef _DEBUG
    // If we're being forced to generate native code for this assembly which can be used in a partial trust
    // context, then we need to ensure that the assembly is entirely transparent -- otherwise the code may
    // perform a critical operation preventing the ngen image from being loaded into partial trust.
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Security_NGenForPartialTrust) != 0)
    {
        moduleFlags &= ~transparencyMask;
        moduleFlags |= ModuleSecurityDescriptorFlags_IsAllTransparent;
    }
#endif // _DEBUG

    // Mark the module as having its security state computed
    moduleFlags |= ModuleSecurityDescriptorFlags_IsComputed;
    InterlockedCompareExchange(reinterpret_cast<LONG *>(&m_flags),
                               moduleFlags,
                               ModuleSecurityDescriptorFlags_None);

    // If this assert fires, we ended up racing to different outcomes
    _ASSERTE(m_flags == moduleFlags);
}


ModuleSecurityDescriptor* ModuleSecurityDescriptor::GetModuleSecurityDescriptor(Assembly *pAssembly)
{
    WRAPPER_NO_CONTRACT;

    Module* pModule = pAssembly->GetManifestModule();
    _ASSERTE(pModule);

    ModuleSecurityDescriptor* pModuleSecurityDesc = pModule->m_pModuleSecurityDescriptor;
    _ASSERTE(pModuleSecurityDesc);

    return pModuleSecurityDesc;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
VOID ModuleSecurityDescriptor::Save(DataImage *image)
{    
    STANDARD_VM_CONTRACT;
    VerifyDataComputed();
    image->StoreStructure(this,
                          sizeof(ModuleSecurityDescriptor),
                          DataImage::ITEM_MODULE_SECDESC);
}

VOID ModuleSecurityDescriptor::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;
    image->FixupPointerField(this, offsetof(ModuleSecurityDescriptor, m_pModule));
}
#endif

#if defined(FEATURE_CORESYSTEM)

//---------------------------------------------------------------------------------------
//
// Parse an APTCA blob into its corresponding token security descriptor flags.
//

TokenSecurityDescriptorFlags ParseAptcaAttribute(const BYTE *pbAptcaBlob, DWORD cbAptcaBlob)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pbAptcaBlob));
    }
    CONTRACTL_END;

    TokenSecurityDescriptorFlags aptcaFlags = TokenSecurityDescriptorFlags_None;

    CustomAttributeParser cap(pbAptcaBlob, cbAptcaBlob);
    if (SUCCEEDED(cap.SkipProlog()))
    {
        aptcaFlags |= TokenSecurityDescriptorFlags_APTCA;

        // Look for the PartialTrustVisibilityLevel named argument
        CaNamedArg namedArgs[1] = {{0}};
        namedArgs[0].InitI4FieldEnum(g_PartialTrustVisibilityLevel, g_SecurityPartialTrustVisibilityLevel);

        if (SUCCEEDED(ParseKnownCaNamedArgs(cap, namedArgs, _countof(namedArgs))))
        {
            // If we have a partial trust visiblity level, then we may additionally be conditionally APTCA.
            PartialTrustVisibilityLevel visibilityLevel = static_cast<PartialTrustVisibilityLevel>(namedArgs[0].val.u4);
            if (visibilityLevel == PartialTrustVisibilityLevel_NotVisibleByDefault)
            {
                aptcaFlags |= TokenSecurityDescriptorFlags_ConditionalAPTCA;
            }
        }
    }

    return aptcaFlags;
}

#endif // defined(FEATURE_CORESYSTEM)

//---------------------------------------------------------------------------------------
//
// Parse a security rules attribute blob into its corresponding token security descriptor
// flags.
//

TokenSecurityDescriptorFlags ParseSecurityRulesAttribute(const BYTE *pbSecurityRulesBlob,
                                                         DWORD cbSecurityRulesBlob)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pbSecurityRulesBlob));
    }
    CONTRACTL_END;

    TokenSecurityDescriptorFlags rulesFlags = TokenSecurityDescriptorFlags_None;

    CustomAttributeParser cap(pbSecurityRulesBlob, cbSecurityRulesBlob);
    if (SUCCEEDED(cap.SkipProlog()))
    {
        rulesFlags |= TokenSecurityDescriptorFlags_SecurityRules;

        // Read out the version number
        UINT8 bRulesLevel = 0;
        if (SUCCEEDED(cap.GetU1(&bRulesLevel)))
        {
            rulesFlags |= EncodeSecurityRuleSet(static_cast<SecurityRuleSet>(bRulesLevel));
        }

        // See if the attribute specified that full trust transparent code should not be verified
        CaNamedArg skipVerificationArg;
        skipVerificationArg.InitBoolField("SkipVerificationInFullTrust", FALSE);
        if (SUCCEEDED(ParseKnownCaNamedArgs(cap, &skipVerificationArg, 1)))
        {
            if (skipVerificationArg.val.boolean)
            {
                rulesFlags |= TokenSecurityDescriptorFlags_SkipFullTrustVerification;
            }
        }
    }

    return rulesFlags;
}

// grok the meta data and compute the necessary attributes
void TokenSecurityDescriptor::VerifyDataComputed()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(m_pModule));
    }
    CONTRACTL_END;

    if (m_flags & TokenSecurityDescriptorFlags_IsComputed)
    {
        return;
    }

    // Loop over the attributes on the token, reading off bits that are interesting for security
    TokenSecurityDescriptorFlags flags = ReadSecurityAttributes(m_pModule->GetMDImport(), m_token);    
    flags |= TokenSecurityDescriptorFlags_IsComputed;
    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), flags);
}

// static
TokenSecurityDescriptorFlags TokenSecurityDescriptor::ReadSecurityAttributes(IMDInternalImport *pmdImport, mdToken token)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        PRECONDITION(CheckPointer(pmdImport));
    }
    CONTRACTL_END;

    TokenSecurityDescriptorFlags flags = TokenSecurityDescriptorFlags_None;

    HENUMInternalHolder hEnum(pmdImport);
    hEnum.EnumInit(mdtCustomAttribute, token);

    mdCustomAttribute currentAttribute;
    while (hEnum.EnumNext(&currentAttribute))
    {
        LPCSTR szAttributeName;
        LPCSTR szAttributeNamespace;

        if (FAILED(pmdImport->GetNameOfCustomAttribute(currentAttribute, &szAttributeNamespace, &szAttributeName)))
        {
            continue;
        }

        // The only attributes we care about are in System.Security, so move on if we found something in a
        // different namespace
        if (szAttributeName != NULL &&
            szAttributeNamespace != NULL &&
            strcmp(g_SecurityNS, szAttributeNamespace) == 0)
        {
#if defined(FEATURE_CORESYSTEM)
            if (strcmp(g_SecurityAPTCA + sizeof(g_SecurityNS), szAttributeName) == 0)
            {
                // Check the visibility parameter
                const BYTE *pbAttributeBlob;
                ULONG cbAttributeBlob;

                if (FAILED(pmdImport->GetCustomAttributeAsBlob(currentAttribute, reinterpret_cast<const void **>(&pbAttributeBlob), &cbAttributeBlob)))
                {
                    continue;
                }

                TokenSecurityDescriptorFlags aptcaFlags = ParseAptcaAttribute(pbAttributeBlob, cbAttributeBlob);
                flags |= aptcaFlags;
            }
            else
#endif // defined(FEATURE_CORESYSTEM)
            if (strcmp(g_SecurityCriticalAttribute + sizeof(g_SecurityNS), szAttributeName) == 0)
            {
                flags |= TokenSecurityDescriptorFlags_Critical;

            }
            else if (strcmp(g_SecuritySafeCriticalAttribute + sizeof(g_SecurityNS), szAttributeName) == 0)
            {
                flags |= TokenSecurityDescriptorFlags_SafeCritical;
            }
            else if (strcmp(g_SecurityTransparentAttribute + sizeof(g_SecurityNS), szAttributeName) == 0)
            {
                flags |= TokenSecurityDescriptorFlags_Transparent;
            }
        }
    }

    return flags;
}

//---------------------------------------------------------------------------------------
//
// Calculate the semantic critical / transparent state for this metadata token.
// See code:TokenSecurityDescriptor#TokenSecurityDescriptorSemanticLookup
//

void TokenSecurityDescriptor::VerifySemanticDataComputed()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_flags & TokenSecurityDescriptorFlags_IsSemanticComputed)
    {
        return;
    }


    bool fIsSemanticallyCritical = false;
    bool fIsSemanticallyTreatAsSafe = false;
    bool fIsSemanticallyExternallyVisible = false;

    // Check the module to see if every type in the module is the same
    Assembly *pAssembly = m_pModule->GetAssembly();
    ModuleSecurityDescriptor* pModuleSecDesc = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pAssembly);
    if (pModuleSecDesc->IsAllTransparent())
    {
        // If the module is explicitly Transparent, then everything in it is Transparent
        fIsSemanticallyCritical = false;
        fIsSemanticallyTreatAsSafe = false;
    }
    else if (pModuleSecDesc->IsAllCritical())
    {
        // If the module is critical or safe critical, then everything in it matches
        fIsSemanticallyCritical = true;

        if (pModuleSecDesc->IsTreatAsSafe())
        {
            fIsSemanticallyTreatAsSafe = true;
        }
    }
    else if (pModuleSecDesc->IsOpportunisticallyCritical())
    {
        // There are three cases for an opportunistically critical type:
        //  1. Level 2 transparency - all types are critical
        //  2. Level 1 transparency - all types are transparent
        //  3. Types participating in type equivalence (regardless of level) - types are transparent
        //  
        // Therefore, we consider the type critical only if it is level 2, otherwise keep it transparent.
        
        const SecurityTransparencyBehavior *pTransparencyBehavior = pAssembly->GetSecurityTransparencyBehavior();
        if (!pTransparencyBehavior->DoesOpportunisticRequireOnlySafeCriticalMethods() &&
            !IsTypeEquivalent())
        {
            // If the module is opportunistically critical, then every type in it is critical
            fIsSemanticallyCritical = true;            
        }
    }
    // Mixed transparency
    else
    {
        const TypeSecurityDescriptorFlags criticalMask = TypeSecurityDescriptorFlags_IsAllCritical |
                                                         TypeSecurityDescriptorFlags_IsCritical;
        const TypeSecurityDescriptorFlags treatAsSafeMask = TypeSecurityDescriptorFlags_IsTreatAsSafe;

        const SecurityTransparencyBehavior *pTransparencyBehavior = pAssembly->GetSecurityTransparencyBehavior();
        _ASSERTE(pTransparencyBehavior != NULL);

        // We don't have full module-level state, so we need to loop over the tokens to figure it out.
        IMDInternalImport* pMdImport = m_pModule->GetMDImport();    
        mdToken tkCurrent = m_token;
        mdToken tkPrev = mdTokenNil;

        // First, we need to walk the chain inside out, building up a stack so that we can pop the stack from
        // the outside in, looking for the largest scope with a statement about the transparency of the types.
        CStackArray<mdToken> typeTokenStack;
        while (tkPrev != tkCurrent)
        {
            typeTokenStack.Push(tkCurrent);
            tkPrev = tkCurrent;
            IfFailThrow(pMdImport->GetParentToken(tkPrev, &tkCurrent));
        }

        //
        // Walk up the chain of containing types, starting with the current metadata token.  At each step on the
        // chain, keep track of if we've been marked critical / treat as safe yet.
        // 
        // It's important that we use only metadata tokens here, rather than using EEClass and
        // TypeSecurityDescriptors, since this method can be called while loading nested types and using
        // TypESecurityDescriptor can lead to recursion during type load.
        //
        // We also need to walk the chain from the outside in, since we listen to the outermost marking.  We
        // can stop looking at tokens once we found one that has a transparency marking (we've become either
        // critical or safe critical), and we've determined that the inner types are not publicly visible.
        // 

        // We'll start out by saying all tokens are not public if public doesn't imply treat as safe - that
        // way we don't flip over to safe critical even if they are all public
        bool fAllTokensPublic = pTransparencyBehavior->DoesPublicImplyTreatAsSafe();

        while (typeTokenStack.Count() > 0 && !fIsSemanticallyCritical)
        {
            mdToken *ptkCurrentType = typeTokenStack.Pop();
            TokenSecurityDescriptor currentTokenSD(m_pModule, *ptkCurrentType);

            // Check to see if the current type is critical / treat as safe.  We only want to check this if we
            // haven't already found an outer type that had a transparency attribute; otherwise we would let
            // an inner scope have more priority than its containing scope
            TypeSecurityDescriptorFlags currentTypeFlags = pTransparencyBehavior->MapTypeAttributes(currentTokenSD.GetMetadataFlags());
            if (!fIsSemanticallyCritical)
            {
                fIsSemanticallyCritical = !!(currentTypeFlags & criticalMask);
                fIsSemanticallyTreatAsSafe |= !!(currentTypeFlags & treatAsSafeMask);
            }

            // If the assembly uses a transparency model where publicly visible items are treat as safe, then
            // we need to check to see if all the types in the containment chain are visible
            if (fAllTokensPublic)
            {
                DWORD dwTypeAttrs;
                IfFailThrow(pMdImport->GetTypeDefProps(tkCurrent, &dwTypeAttrs, NULL));

                fAllTokensPublic = IsTdPublic(dwTypeAttrs) ||
                                   IsTdNestedPublic(dwTypeAttrs) ||
                                   IsTdNestedFamily(dwTypeAttrs) ||
                                   IsTdNestedFamORAssem(dwTypeAttrs);
            }
        }

        // If public implies treat as safe, all the types were visible, and we are semantically critical
        // then we're actually semantically safe critical
        if (fAllTokensPublic)
        {
            _ASSERTE(pTransparencyBehavior->DoesPublicImplyTreatAsSafe());
            
            fIsSemanticallyExternallyVisible = true;

            if (fIsSemanticallyCritical)
            {
                fIsSemanticallyTreatAsSafe = true;
            }
        }
    }

    // Further, if we're critical due to the assembly, and public implies treat as safe,
    // and the outermost nested type is public, then we are safe critical
    if (pModuleSecDesc->IsAllCritical() ||
        pModuleSecDesc->IsOpportunisticallyCritical())
    {
        // We shouldn't have determined if we're externally visible or not yet
        _ASSERTE(!fIsSemanticallyExternallyVisible);

        const SecurityTransparencyBehavior *pTransparencyBehavior = pAssembly->GetSecurityTransparencyBehavior();

        if (pTransparencyBehavior->DoesPublicImplyTreatAsSafe() &&
            fIsSemanticallyCritical &&
            !fIsSemanticallyTreatAsSafe)
        {
            IMDInternalImport* pMdImport = m_pModule->GetMDImport();
            mdToken tkCurrent = m_token;
            mdToken tkPrev = mdTokenNil;
            HRESULT hrIter = S_OK;

            while (SUCCEEDED(hrIter) && tkCurrent != tkPrev)
            {
                tkPrev = tkCurrent;
                hrIter = pMdImport->GetNestedClassProps(tkPrev, &tkCurrent);

                if (!SUCCEEDED(hrIter))
                {
                    if (hrIter == CLDB_E_RECORD_NOTFOUND)
                    {
                        // We don't have a parent class, so use the previous as our outermost
                        tkCurrent = tkPrev;
                    }
                    else
                    {
                        ThrowHR(hrIter);
                    }
                }

                DWORD dwOuterTypeAttrs;
                IfFailThrow(pMdImport->GetTypeDefProps(tkCurrent, &dwOuterTypeAttrs, NULL));
                if (IsTdPublic(dwOuterTypeAttrs))
                {
                    fIsSemanticallyExternallyVisible = true;
                    fIsSemanticallyTreatAsSafe = true;
                }
            }
        }
    }

    // Save away the semantic state that we just computed
    TokenSecurityDescriptorFlags semanticFlags = TokenSecurityDescriptorFlags_IsSemanticComputed;
    if (fIsSemanticallyCritical)
        semanticFlags |= TokenSecurityDescriptorFlags_IsSemanticCritical;
    if (fIsSemanticallyTreatAsSafe)
        semanticFlags |= TokenSecurityDescriptorFlags_IsSemanticTreatAsSafe;
    if (fIsSemanticallyExternallyVisible)
        semanticFlags |= TokenSecurityDescriptorFlags_IsSemanticExternallyVisible;

    FastInterlockOr(reinterpret_cast<DWORD *>(&m_flags), static_cast<DWORD>(semanticFlags));
}

HashDatum TokenSecurityDescriptor::LookupSecurityDescriptor(void* pKey)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HashDatum      datum;
    AppDomain*      pDomain  = GetAppDomain();

    EEPtrHashTable  &rCachedMethodPermissionsHash = pDomain->m_pSecContext->m_pCachedMethodPermissionsHash;

    // We need to switch to cooperative GC here.  But using GCX_COOP here
    // causes 20% perf degrade in some declarative security assert scenario.
    // We should fix this one.
    CONTRACT_VIOLATION(ModeViolation);
    // Fast attempt, that may fail (and return FALSE):
    if (!rCachedMethodPermissionsHash.GetValueSpeculative(pKey, &datum))
    {
        // Slow call
        datum = LookupSecurityDescriptor_Slow(pDomain, pKey, rCachedMethodPermissionsHash);
    }
    return datum;
}

HashDatum TokenSecurityDescriptor::LookupSecurityDescriptor_Slow(AppDomain* pDomain,
                                                                 void* pKey,   
                                                                 EEPtrHashTable  &rCachedMethodPermissionsHash )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HashDatum      datum;
    SimpleRWLock* prGlobalLock = pDomain->m_pSecContext->m_prCachedMethodPermissionsLock;
    // look up the cache in the slow mode
    // in the false failure case, we'll recheck the cache anyway
    SimpleReadLockHolder readLockHolder(prGlobalLock);
    if (rCachedMethodPermissionsHash.GetValue(pKey, &datum))
    {
        return datum;
    }
    return NULL;
}

HashDatum TokenSecurityDescriptor::InsertSecurityDescriptor(void* pKey, HashDatum pHashDatum)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    AppDomain*      pDomain  = GetAppDomain();
    SimpleRWLock* prGlobalLock = pDomain->m_pSecContext->m_prCachedMethodPermissionsLock;
    EEPtrHashTable  &rCachedMethodPermissionsHash = pDomain->m_pSecContext->m_pCachedMethodPermissionsHash;

    HashDatum pFoundHashDatum = NULL;
    // insert the computed details in our hash table
    {
        SimpleWriteLockHolder writeLockHolder(prGlobalLock);
        // since the hash table doesn't support duplicates by
        // default, we need to recheck in case another thread
        // added the value during a context switch
        if (!rCachedMethodPermissionsHash.GetValue(pKey, &pFoundHashDatum))        
        {
            // no entry was found
            _ASSERTE(pFoundHashDatum == NULL);
            // Place the new entry into the hash.
            rCachedMethodPermissionsHash.InsertValue(pKey, pHashDatum);
        }
    }
    // return the value found in the lookup, in case there was a duplicate
    return pFoundHashDatum;
}

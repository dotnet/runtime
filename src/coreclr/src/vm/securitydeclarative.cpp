// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"

#include "security.h"
#include "securitydeclarative.inl"
#include "eventtrace.h"



//-----------------------------------------------------------------------------
//
//
//     CODE FOR MAKING THE SECURITY STUB AT JIT-TIME
//
//
//-----------------------------------------------------------------------------


enum DeclSecMergeMethod
{
    DS_METHOD_OVERRIDE,
    DS_CLASS_OVERRIDE,
    DS_UNION,
    DS_INTERSECT,
    DS_APPLY_METHOD_THEN_CLASS, // not supported with stack modifier actions
    DS_APPLY_CLASS_THEN_METHOD, // not supported with stack modifier actions
    DS_NOT_APPLICABLE, // action not supported on both method and class
};

// (Note: The values that are DS_NOT_APPLICABLE are not hooked up to
// this table, so changing one of those values will have no effect)
const DeclSecMergeMethod g_DeclSecClassAndMethodMergeTable[] =
{
    DS_NOT_APPLICABLE, // dclActionNil = 0
    DS_NOT_APPLICABLE, // dclRequest = 1
    DS_UNION, // dclDemand = 2
    DS_METHOD_OVERRIDE, // dclAssert = 3
    DS_UNION, // dclDeny = 4
    DS_INTERSECT, // dclPermitOnly = 5
    DS_NOT_APPLICABLE, // dclLinktimeCheck = 6
    DS_NOT_APPLICABLE, // dclInheritanceCheck = 7
    DS_NOT_APPLICABLE, // dclRequestMinimum = 8
    DS_NOT_APPLICABLE, // dclRequestOptional = 9
    DS_NOT_APPLICABLE, // dclRequestRefuse = 10
    DS_NOT_APPLICABLE, // dclPrejitGrant = 11
    DS_NOT_APPLICABLE, // dclPrejitDenied = 12
    DS_UNION, // dclNonCasDemand = 13
    DS_NOT_APPLICABLE, // dclNonCasLinkDemand = 14
    DS_NOT_APPLICABLE, // dclNonCasInheritance = 15
};

// This table specifies the order in which runtime declarative actions will be performed
// (Note that for stack-modifying actions, this means the order in which they are applied to the
//  frame descriptor, not the order in which they are evaluated when a demand is performed.
//  That order is determined by the code in System.Security.FrameSecurityDescriptor.)
const CorDeclSecurity g_RuntimeDeclSecOrderTable[] =
{
    dclPermitOnly, // 5
    dclDeny, // 4
    dclAssert, // 3
    dclDemand, // 2
    dclNonCasDemand, // 13
};

#define DECLSEC_RUNTIME_ACTION_COUNT (sizeof(g_RuntimeDeclSecOrderTable) / sizeof(CorDeclSecurity))


TokenDeclActionInfo* TokenDeclActionInfo::Init(DWORD dwAction, PsetCacheEntry *pPCE)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    AppDomain                   *pDomain        = GetAppDomain();

    TokenDeclActionInfo *pTemp = 
        static_cast<TokenDeclActionInfo*>((void*)pDomain->GetLowFrequencyHeap()
                            ->AllocMem(S_SIZE_T(sizeof(TokenDeclActionInfo))));

    pTemp->dwDeclAction = dwAction;
    pTemp->pPCE = pPCE;
    pTemp->pNext = NULL;

    return pTemp;
}

void TokenDeclActionInfo::LinkNewDeclAction(TokenDeclActionInfo** ppActionList, CorDeclSecurity action, PsetCacheEntry *pPCE)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    TokenDeclActionInfo *temp = Init(DclToFlag(action), pPCE);
    if (!(*ppActionList))
        *ppActionList = temp;
    else
    {
        temp->pNext = *ppActionList;
        *ppActionList = temp;
    }
}

DeclActionInfo *DeclActionInfo::Init(MethodDesc *pMD, DWORD dwAction, PsetCacheEntry *pPCE)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    DeclActionInfo *pTemp = (DeclActionInfo *)(void*)pMD->GetDomainSpecificLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(DeclActionInfo)));

    pTemp->dwDeclAction = dwAction;
    pTemp->pPCE = pPCE;
    pTemp->pNext = NULL;

    return pTemp;
}

void LinkNewDeclAction(DeclActionInfo** ppActionList, CorDeclSecurity action, PsetCacheEntry *pPCE, MethodDesc *pMeth)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    DeclActionInfo *temp = DeclActionInfo::Init(pMeth, DclToFlag(action), pPCE);
    if (!(*ppActionList))
        *ppActionList = temp;
    else
    {
        // Add overrides to the end of the list, all others to the front
        if (IsDclActionAnyStackModifier(action))
        {
            DeclActionInfo *w = *ppActionList;
            while (w->pNext != NULL)
                w = w->pNext;
            w->pNext = temp;
        }
        else
        {
            temp->pNext = *ppActionList;
            *ppActionList = temp;
        }
    }
}

void SecurityDeclarative::AddDeclAction(CorDeclSecurity action, PsetCacheEntry *pClassPCE, PsetCacheEntry *pMethodPCE, DeclActionInfo** ppActionList, MethodDesc *pMeth)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    if(pClassPCE == NULL)
    {
        if(pMethodPCE == NULL)
            return;
        LinkNewDeclAction(ppActionList, action, pMethodPCE, pMeth);
        return;
    }
    else if(pMethodPCE == NULL)
    {
        LinkNewDeclAction(ppActionList, action, pClassPCE, pMeth);
        return;
    }

    // Merge class and method declarations
    switch(g_DeclSecClassAndMethodMergeTable[action])
    {
    case DS_METHOD_OVERRIDE:
        LinkNewDeclAction(ppActionList, action, pMethodPCE, pMeth);
        break;

    case DS_CLASS_OVERRIDE:
        LinkNewDeclAction(ppActionList, action, pClassPCE, pMeth);
        break;

    case DS_UNION:
        _ASSERTE(!"Declarative permission sets may not be unioned together in CoreCLR. Are you attempting to have a declarative demand or deny on both a method and its enclosing class?");
        break;

    case DS_INTERSECT:
        _ASSERTE(!"Declarative permission sets may not be intersected in CoreCLR. Are you attempting to have a declarative permit only on both a method and its enclosing class?");
        break;

    case DS_APPLY_METHOD_THEN_CLASS:
        LinkNewDeclAction(ppActionList, action, pClassPCE, pMeth); // note: order reversed because LinkNewDeclAction inserts at beginning of list
        LinkNewDeclAction(ppActionList, action, pMethodPCE, pMeth);
        break;

    case DS_APPLY_CLASS_THEN_METHOD:
        LinkNewDeclAction(ppActionList, action, pMethodPCE, pMeth); // note: order reversed because LinkNewDeclAction inserts at beginning of list
        LinkNewDeclAction(ppActionList, action, pClassPCE, pMeth);
        break;

    case DS_NOT_APPLICABLE:
        _ASSERTE(!"not a runtime action");
        break;

    default:
        _ASSERTE(!"unexpected merge type");
        break;
    }
}


// Here we see what declarative actions are needed everytime a method is called,
// and create a list of these actions, which will be emitted as an argument to
// DoDeclarativeSecurity
DeclActionInfo* SecurityDeclarative::DetectDeclActions(MethodDesc *pMeth, DWORD dwDeclFlags)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    GCX_COOP();

    DeclActionInfo              *pDeclActions = NULL;

    IMDInternalImport *pInternalImport = pMeth->GetMDImport();

    // Lets check the Ndirect/Interop cases first
    if (dwDeclFlags & DECLSEC_UNMNGD_ACCESS_DEMAND)
    {
        HRESULT hr = S_FALSE;
        if (pMeth->HasSuppressUnmanagedCodeAccessAttr())
        {
            dwDeclFlags &= ~DECLSEC_UNMNGD_ACCESS_DEMAND;
        }
        else
        {
            MethodTable * pMT = pMeth->GetMethodTable();
            EEClass * pClass = pMT->GetClass();

            // If speculatively true then check the CA

            if (pClass->HasSuppressUnmanagedCodeAccessAttr())
            {
                hr = S_OK;
                if (hr != S_OK)
                {
                    g_IBCLogger.LogEEClassCOWTableAccess(pMT);
                    pClass->SetDoesNotHaveSuppressUnmanagedCodeAccessAttr();
                }
            }
            _ASSERTE(SUCCEEDED(hr));
            if (hr == S_OK)
                dwDeclFlags &= ~DECLSEC_UNMNGD_ACCESS_DEMAND;
        }
        // Check if now there are no actions left
        if (dwDeclFlags == 0)
            return NULL;

        if (dwDeclFlags & DECLSEC_UNMNGD_ACCESS_DEMAND)
        {
            // A NDirect/Interop demand is required.
            DeclActionInfo *temp = DeclActionInfo::Init(pMeth, DECLSEC_UNMNGD_ACCESS_DEMAND, NULL);
            if (!pDeclActions)
                pDeclActions = temp;
            else
            {
                temp->pNext = pDeclActions;
                pDeclActions = temp;
            }
        }
    } // if DECLSEC_UNMNGD_ACCESS_DEMAND

    // Find class declarations
    PsetCacheEntry* classSetPermissions[dclMaximumValue + 1];
    DetectDeclActionsOnToken(pMeth->GetMethodTable()->GetCl(), dwDeclFlags, classSetPermissions, pInternalImport);

    // Find method declarations
    PsetCacheEntry* methodSetPermissions[dclMaximumValue + 1];
    DetectDeclActionsOnToken(pMeth->GetMemberDef(), dwDeclFlags, methodSetPermissions, pInternalImport);

    // Make sure the g_DeclSecClassAndMethodMergeTable is okay
    _ASSERTE(sizeof(g_DeclSecClassAndMethodMergeTable) == sizeof(DeclSecMergeMethod) * (dclMaximumValue + 1) &&
            "g_DeclSecClassAndMethodMergeTable wrong size!");

    // Merge class and method runtime declarations into a single linked list of set indexes
    int i;
    for(i = DECLSEC_RUNTIME_ACTION_COUNT - 1; i >= 0; i--) // note: the loop uses reverse order because AddDeclAction inserts at beginning of the list
    {
        CorDeclSecurity action = g_RuntimeDeclSecOrderTable[i];
        _ASSERTE(action > dclActionNil && action <= dclMaximumValue && "action out of range");
        AddDeclAction(action, classSetPermissions[action], methodSetPermissions[action], &pDeclActions, pMeth);
    }

    return pDeclActions;
}

void SecurityDeclarative::DetectDeclActionsOnToken(mdToken tk, DWORD dwDeclFlags, PsetCacheEntry** pSets, IMDInternalImport *pInternalImport)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
    
    // Make sure the DCL to Flag table is okay
    _ASSERTE(DclToFlag(dclDemand) == DECLSEC_DEMANDS &&
             sizeof(DCL_FLAG_MAP) == sizeof(DWORD) * (dclMaximumValue + 1) &&
             "DCL_FLAG_MAP out of sync with CorDeclSecurity!");

    // Initialize the array
    int i;
    for(i = 0; i < dclMaximumValue + 1; i++)
        pSets[i] = NULL;

    // Look up declarations on the token for each SecurityAction
    DWORD dwAction;
    for (dwAction = 0; dwAction <= dclMaximumValue; dwAction++)
    {
        // don't bother with actions that are not in the requested mask
        CorDeclSecurity action = (CorDeclSecurity)dwAction;
        DWORD dwActionFlag = DclToFlag(action);
        if ((dwDeclFlags & dwActionFlag) == 0)
            continue;

        // Load the PermissionSet or PermissionSetCollection from the security action table in the metadata
        PsetCacheEntry *pPCE;
        HRESULT hr = SecurityAttributes::GetDeclaredPermissions(pInternalImport, tk, action, NULL, &pPCE);
        if (hr != S_OK) // returns S_FALSE if it didn't find anything in the metadata
            continue;

        pSets[dwAction] = pPCE;
    }
}

// Returns TRUE if there is a possibility that a token has declarations of the type specified by 'action'
// Returns FALSE if it can determine that the token definately does not.
BOOL SecurityDeclarative::TokenMightHaveDeclarations(IMDInternalImport *pInternalImport, mdToken token, CorDeclSecurity action)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    HENUMInternal hEnumDcl;
    DWORD cDcl;

    // Check if the token has declarations for
    // the action specified.
    hr = pInternalImport->EnumPermissionSetsInit(
        token,
        action,
        &hEnumDcl);

    if (FAILED(hr) || hr == S_FALSE)
    {
        // PermissionSets for non-CAS actions are special cases because they may be mixed with
        // the set for the corresponding CAS action in a serialized CORSEC_PSET
        if(action == dclNonCasDemand || action == dclNonCasLinkDemand || action == dclNonCasInheritance)
        {
            // See if the corresponding CAS action has permissions
            BOOL fDoCheck = FALSE;
            if(action == dclNonCasDemand)
                    fDoCheck = TokenMightHaveDeclarations(pInternalImport, token, dclDemand);
            else if(action == dclNonCasLinkDemand)
                    fDoCheck = TokenMightHaveDeclarations(pInternalImport, token, dclLinktimeCheck);
            else if(action == dclNonCasInheritance)
                    fDoCheck = TokenMightHaveDeclarations(pInternalImport, token, dclInheritanceCheck);
            if(fDoCheck)
            {
                // We can't tell for sure if there are declarations unless we deserializing something
                // (which is too expensive), so we'll just return TRUE
                return TRUE;
            /*
                OBJECTREF refPermSet = NULL;
                DWORD dwIndex = ~0;
                hr = SecurityAttributes::GetDeclaredPermissionsWithCache(pInternalImport, token, action, &refPermSet, &dwIndex);
                if(refPermSet != NULL)
                {
                    _ASSERTE(dwIndex != (~0));
                    return TRUE;
                }
            */
            }
        }
        pInternalImport->EnumClose(&hEnumDcl);
        return FALSE;
    }

    cDcl = pInternalImport->EnumGetCount(&hEnumDcl);
    pInternalImport->EnumClose(&hEnumDcl);

    return (cDcl > 0);
}


bool SecurityDeclarative::BlobMightContainNonCasPermission(PBYTE pbAttrSet, ULONG cbAttrSet, DWORD dwAction, bool* pHostProtectionOnly)
{
    CONTRACTL {
        THROWS;
    } CONTRACTL_END;

    // Deserialize the CORSEC_ATTRSET
    CORSEC_ATTRSET attrSet;
    HRESULT hr = BlobToAttributeSet(pbAttrSet, cbAttrSet, &attrSet, dwAction);
    if(FAILED(hr))
        COMPlusThrowHR(hr);

    // this works because SecurityAttributes::CanUnrestrictedOverride only returns
    // true if the attribute set contains only well-known non-CAS permissions
    return !SecurityAttributes::ContainsBuiltinCASPermsOnly(&attrSet, pHostProtectionOnly);
}

// Accumulate status of declarative security.
HRESULT SecurityDeclarative::GetDeclarationFlags(IMDInternalImport *pInternalImport, mdToken token, DWORD* pdwFlags, DWORD* pdwNullFlags, BOOL* pfHasSuppressUnmanagedCodeAccessAttr /*[IN:TRUE if Pinvoke/Cominterop][OUT:FALSE if doesn't have attr]*/)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    HENUMInternal   hEnumDcl;
    HRESULT         hr;
    DWORD           dwFlags = 0;
    DWORD           dwNullFlags = 0;

    _ASSERTE(pdwFlags);
    *pdwFlags = 0;

    if (pdwNullFlags)
        *pdwNullFlags = 0;

    hr = pInternalImport->EnumPermissionSetsInit(token, dclActionNil, &hEnumDcl);
    if (FAILED(hr))
        goto Exit;

    if (hr == S_OK)
    {
        //Look through the security action table in the metadata for declared permission sets
        mdPermission    perms;
        DWORD           dwAction;
        DWORD           dwDclFlags;
        ULONG           cbPerm;
        PBYTE           pbPerm;
        while (pInternalImport->EnumNext(&hEnumDcl, &perms))
        {
            hr = pInternalImport->GetPermissionSetProps(
                perms, 
                &dwAction, 
                (const void**)&pbPerm, 
                &cbPerm);
            if (FAILED(hr))
            {
                goto Exit;
            }
            
            dwDclFlags = DclToFlag(dwAction);
            
            if ((cbPerm > 0) && (pbPerm[0] == LAZY_DECL_SEC_FLAG)) // indicates a serialized CORSEC_PSET
            {
                bool hostProtectionOnly; // gets initialized in call to BlobMightContainNonCasPermission
                if (BlobMightContainNonCasPermission(pbPerm, cbPerm, dwAction, &hostProtectionOnly))
                {
                    switch (dwAction)
                    {
                        case dclDemand:
                            dwFlags |= DclToFlag(dclNonCasDemand);
                            break;
                        case dclLinktimeCheck:
                            dwFlags |= DclToFlag(dclNonCasLinkDemand);
                            break;
                        case dclInheritanceCheck:
                            dwFlags |= DclToFlag(dclNonCasInheritance);
                            break;
                    }
                }
                else
                {
                    if (hostProtectionOnly)
                    {
                        // If this is a linkcheck for HostProtection only, let's capture that in the flags. 
                        // Subsequently, this will be captured in the bit mask on EEClass/MethodDesc
                        // and used when deciding whether to insert runtime callouts for transparency
                        dwDclFlags |= DECLSEC_LINK_CHECKS_HPONLY;
                    }
                }
            }
            
            dwFlags |= dwDclFlags;
        }
    }
    pInternalImport->EnumClose(&hEnumDcl);

    // Disable any runtime checking of UnmanagedCode permission if the correct
    // custom attribute is present.
    // By default, check except when told not to by the passed in BOOL*

    BOOL hasSuppressUnmanagedCodeAccessAttr;
    if (pfHasSuppressUnmanagedCodeAccessAttr == NULL)
    {
        hasSuppressUnmanagedCodeAccessAttr = TRUE;
    }
    else
        hasSuppressUnmanagedCodeAccessAttr = *pfHasSuppressUnmanagedCodeAccessAttr;
        

    if (hasSuppressUnmanagedCodeAccessAttr)
    {
        dwFlags |= DECLSEC_UNMNGD_ACCESS_DEMAND;
        dwNullFlags |= DECLSEC_UNMNGD_ACCESS_DEMAND;
    }

    *pdwFlags = dwFlags;
    if (pdwNullFlags)
        *pdwNullFlags = dwNullFlags;

Exit:
    return hr;
}

void SecurityDeclarative::ClassInheritanceCheck(MethodTable *pClass, MethodTable *pParent)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pClass));
        PRECONDITION(CheckPointer(pParent));
        PRECONDITION(!pClass->IsInterface());
    }
    CONTRACTL_END;

    // Regular check since Fast path check didn't succeed
    TypeSecurityDescriptor typeSecDesc(pParent);
    typeSecDesc.InvokeInheritanceChecks(pClass);
}

void SecurityDeclarative::MethodInheritanceCheck(MethodDesc *pMethod, MethodDesc *pParent)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMethod));
        PRECONDITION(CheckPointer(pParent));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Regular check since Fast path check didn't succeed    
    MethodSecurityDescriptor MDSecDesc(pParent); 
    MDSecDesc.InvokeInheritanceChecks(pMethod);
}

#ifndef CROSSGEN_COMPILE
//-----------------------------------------------------------------------------
//
//
//     CODE FOR PERFORMING JIT-TIME CHECKS
//
//
//-----------------------------------------------------------------------------






// Retrieve all linktime demands sets for a method. This includes both CAS and
// non-CAS sets for LDs at the class and the method level, so we could get up to
// four sets.
void SecurityDeclarative::RetrieveLinktimeDemands(MethodDesc  *pMD,
                                       OBJECTREF   *pClassCas,
                                       OBJECTREF   *pClassNonCas,
                                       OBJECTREF   *pMethodCas,
                                       OBJECTREF   *pMethodNonCas)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

}

//
// Determine the reason why a method has been marked as requiring a link time check
//
// Arguments:
//    pMD                  - the method to figure out what link checks are needed for
//    pClassCasDemands     - [out, optional] the CAS link demands found on the class containing the method
//    pClassNonCasDemands  - [out, optional] the non-CAS link demands found on the class containing the method
//    pMethodCasDemands    - [out, optional] the CAS link demands found on the method itself
//    pMethodNonCasDemands - [out, optional] the non-CAS link demands found on the method itself
//    
// Return Value:
//    Flags indicating why the method has a link time check requirement
//

// static
LinktimeCheckReason SecurityDeclarative::GetLinktimeCheckReason(MethodDesc *pMD,
                                                                OBJECTREF  *pClassCasDemands,
                                                                OBJECTREF  *pClassNonCasDemands,
                                                                OBJECTREF  *pMethodCasDemands,
                                                                OBJECTREF  *pMethodNonCasDemands)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pClassCasDemands, NULL_OK));
        PRECONDITION(CheckPointer(pClassNonCasDemands, NULL_OK));
        PRECONDITION(CheckPointer(pMethodCasDemands, NULL_OK));
        PRECONDITION(CheckPointer(pMethodNonCasDemands, NULL_OK));
        PRECONDITION(pMD->RequiresLinktimeCheck());
    }
    CONTRACTL_END;

    LinktimeCheckReason reason = LinktimeCheckReason_None;

#if defined(FEATURE_CORESYSTEM)
    ModuleSecurityDescriptor *pMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pMD->GetAssembly());

    // If the method does not allow partially trusted callers, then the check is because we need to ensure all
    // callers are fully trusted.
    if (!pMSD->IsAPTCA())
    {
        reason |= LinktimeCheckReason_AptcaCheck;
    }
#endif // defined(FEATURE_CORESYSTEM)

    //
    // If the method has a LinkDemand on it for either CAS or non-CAS permissions, get those and set the
    // flags for the appropriate type of permission.
    //

    struct gc
    {
        OBJECTREF refClassCasDemands;
        OBJECTREF refClassNonCasDemands;
        OBJECTREF refMethodCasDemands;
        OBJECTREF refMethodNonCasDemands;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    // Fetch link demand sets from all the places in metadata where we might
    // find them (class and method). These might be split into CAS and non-CAS
    // sets as well.
    Security::RetrieveLinktimeDemands(pMD,
                                      &gc.refClassCasDemands,
                                      &gc.refClassNonCasDemands,
                                      &gc.refMethodCasDemands,
                                      &gc.refMethodNonCasDemands);

    if (gc.refClassCasDemands != NULL || gc.refMethodCasDemands != NULL)
    {
        reason |= LinktimeCheckReason_CasDemand;

        if (pClassCasDemands != NULL)
        {
            *pClassCasDemands = gc.refClassCasDemands;
        }
        if (pMethodCasDemands != NULL)
        {
            *pMethodCasDemands = gc.refMethodCasDemands;
        }
    }

    if (gc.refClassNonCasDemands != NULL || gc.refMethodNonCasDemands != NULL)
    {
        reason |= LinktimeCheckReason_NonCasDemand;

        if (pClassNonCasDemands != NULL)
        {
            *pClassNonCasDemands = gc.refClassNonCasDemands;
        }

        if (pMethodNonCasDemands != NULL)
        {
            *pMethodNonCasDemands = gc.refMethodNonCasDemands;
        }

    }

    GCPROTECT_END();

    //
    // Check to see if the target of the method is unmanaged code
    //
    // We detect linktime checks for UnmanagedCode in three cases:
    //   o  P/Invoke calls.
    //   o  Calls through an interface that have a suppress runtime check attribute on them (these are almost
    //      certainly interop calls).
    //   o  Interop calls made through method impls.
    //

    if (pMD->IsNDirect())
    {
        reason |= LinktimeCheckReason_NativeCodeCall;
    }
#ifdef FEATURE_COMINTEROP
    else if (pMD->IsComPlusCall() && !pMD->IsInterface())
    {
        reason |= LinktimeCheckReason_NativeCodeCall;
    }
    else if (pMD->IsInterface())
    {
        // We also consider calls to interfaces that contain the SuppressUnmanagedCodeSecurity attribute to
        // be COM calls, so check for those.
        bool fSuppressUnmanagedCheck =
            pMD->GetMDImport()->GetCustomAttributeByName(pMD->GetMethodTable()->GetCl(),
                                                         COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                         NULL,
                                                         NULL) == S_OK ||
            pMD->GetMDImport()->GetCustomAttributeByName(pMD->GetMemberDef(),
                                                         COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                         NULL,
                                                         NULL) == S_OK;
        if (fSuppressUnmanagedCheck)
        {
            reason |= LinktimeCheckReason_NativeCodeCall;
        }
    }
#endif // FEATURE_COMINTEROP

    return reason;
}


#endif // CROSSGEN_COMPILE

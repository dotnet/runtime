// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"

#include "security.h"
#include "securitydeclarative.inl"
#include "eventtrace.h"

#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif // FEATURE_REMOTING


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
#ifdef FEATURE_CAS_POLICY
        LinkNewDeclAction(ppActionList, action, SecurityAttributes::MergePermissionSets(pClassPCE, pMethodPCE, false, action), pMeth);
#else // FEATURE_CAS_POLICY
        _ASSERTE(!"Declarative permission sets may not be unioned together in CoreCLR. Are you attempting to have a declarative demand or deny on both a method and its enclosing class?");
#endif // FEATURE_CAS_POLICY
        break;

    case DS_INTERSECT:
#ifdef FEATURE_CAS_POLICY
        LinkNewDeclAction(ppActionList, action, SecurityAttributes::MergePermissionSets(pClassPCE, pMethodPCE, true, action), pMeth);
#else // FEATURE_CAS_POLICY
        _ASSERTE(!"Declarative permission sets may not be intersected in CoreCLR. Are you attempting to have a declarative permit only on both a method and its enclosing class?");
#endif // FEATURE_CAS_POLICY
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
#ifdef FEATURE_CORECLR
                hr = S_OK;
#else
                hr = pInternalImport->GetCustomAttributeByName(pMT->GetCl(),
                                                               COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                               NULL,
                                                               NULL);
#endif // FEATURE_CORECLR
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
#ifdef FEATURE_CORECLR
        hasSuppressUnmanagedCodeAccessAttr = TRUE;
#else
        hasSuppressUnmanagedCodeAccessAttr = 
          (pInternalImport->GetCustomAttributeByName(token,
                                                     COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                     NULL,
                                                     NULL) == S_OK);
#endif
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

//---------------------------------------------------------
// Invoke linktime checks on the caller if demands exist
// for the callee.
//---------------------------------------------------------
/*static*/
void SecurityDeclarative::LinktimeCheckMethod(Assembly *pCaller, MethodDesc *pCallee)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifdef FEATURE_CAS_POLICY
    // Do a fulltrust check on the caller if the callee is fully trusted         
    if (FullTrustCheckForLinkOrInheritanceDemand(pCaller))
    {
        return;
    }

#ifdef CROSSGEN_COMPILE
    CrossGenNotSupported("SecurityDeclarative::LinktimeCheckMethod");
#else
    GCX_COOP();

    MethodTable *pTargetMT = pCallee->GetMethodTable();

    // If it's a delegate BeginInvoke, we need to perform a HostProtection check for synchronization
    if(pTargetMT->IsDelegate())
    {
        DelegateEEClass* pDelegateClass = (DelegateEEClass*)pTargetMT->GetClass();
        if(pCallee == pDelegateClass->m_pBeginInvokeMethod)
        {
            EApiCategories eProtectedCategories = GetHostProtectionManager()->GetProtectedCategories();
            if((eProtectedCategories & eSynchronization) == eSynchronization)
            {
                if(!pCaller->GetSecurityDescriptor()->IsFullyTrusted())
                {
                    ThrowHPException(eProtectedCategories, eSynchronization);
                }
            }
        }
    }

    // the rest of the LinkDemand checks
    {
        // Track perfmon counters. Linktime security checkes.
        COUNTER_ONLY(GetPerfCounters().m_Security.cLinkChecks++);

#ifdef FEATURE_APTCA
        // APTCA check
        SecurityDeclarative::DoUntrustedCallerChecks(pCaller, pCallee, FALSE);
#endif // FEATURE_APTCA

        // If the class has its own linktime checks, do them first...
        if (pTargetMT->GetClass()->RequiresLinktimeCheck())
        {
            TypeSecurityDescriptor::InvokeLinktimeChecks(pTargetMT, pCaller);
        }

        // If the previous check passed, check the method for
        // method-specific linktime checks...
        if (IsMdHasSecurity(pCallee->GetAttrs()) &&
            (TokenMightHaveDeclarations(pTargetMT->GetMDImport(),
                                  pCallee->GetMemberDef(),
                                  dclLinktimeCheck) ||
             TokenMightHaveDeclarations(pTargetMT->GetMDImport(),
                                  pCallee->GetMemberDef(),
                                  dclNonCasLinkDemand) ))
        {
            MethodSecurityDescriptor::InvokeLinktimeChecks(pCallee, pCaller);
        }

        // We perform automatic linktime checks for UnmanagedCode in three cases:
        //   o  P/Invoke calls
        //   o  Calls through an interface that have a suppress runtime check
        //      attribute on them (these are almost certainly interop calls).
        //   o  Interop calls made through method impls.
        if (pCallee->IsNDirect() ||
            (pTargetMT->IsInterface() &&
             (pTargetMT->GetMDImport()->GetCustomAttributeByName(pTargetMT->GetCl(),
                                                                   COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                                   NULL,
                                                                   NULL) == S_OK ||
              pTargetMT->GetMDImport()->GetCustomAttributeByName(pCallee->GetMemberDef(),
                                                                   COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                                   NULL,
                                                                   NULL) == S_OK) ) ||
            (pCallee->IsComPlusCall() && !pCallee->IsInterface()))
        {
            if (!pCaller->GetSecurityDescriptor()->CanCallUnmanagedCode())
            {
                Security::ThrowSecurityException(g_SecurityPermissionClassName, SPFLAGSUNMANAGEDCODE);
            }
        }
    }

#endif // !CROSSGEN_COMPILE

#endif // FEATURE_CAS_POLICY
}

#ifndef CROSSGEN_COMPILE
//-----------------------------------------------------------------------------
//
//
//     CODE FOR PERFORMING JIT-TIME CHECKS
//
//
//-----------------------------------------------------------------------------

void SecurityDeclarative::_GetSharedPermissionInstance(OBJECTREF *perm, int index)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(index < (int) NUM_PERM_OBJECTS);

    AppDomain *pDomain = GetAppDomain();
    SharedPermissionObjects *pShared = &pDomain->m_pSecContext->m_rPermObjects[index];

    if (pShared->hPermissionObject == NULL) {
        pShared->hPermissionObject = pDomain->CreateHandle(NULL);
        *perm = NULL;
    }
    else
        *perm = ObjectFromHandle(pShared->hPermissionObject);

    if (*perm == NULL)
    {
        MethodTable *pMT = NULL;
        OBJECTREF p = NULL;

        GCPROTECT_BEGIN(p);

        pMT = MscorlibBinder::GetClass(pShared->idClass);
        MethodDescCallSite  ctor(pShared->idConstructor);

        p = AllocateObject(pMT);

        ARG_SLOT argInit[2] =
        {
            ObjToArgSlot(p),
            (ARG_SLOT) pShared->dwPermissionFlag
        };

        ctor.Call(argInit);

        StoreObjectInHandle(pShared->hPermissionObject, p);
        *perm = p;

        GCPROTECT_END();
    }
}

#ifdef FEATURE_APTCA
void DECLSPEC_NORETURN SecurityDeclarative::ThrowAPTCAException(Assembly *pCaller, MethodDesc *pCallee)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

        MethodDescCallSite throwSecurityException(METHOD__SECURITY_ENGINE__THROW_SECURITY_EXCEPTION);

        OBJECTREF callerObj = NULL;
        if (pCaller != NULL && pCaller->GetDomain() == GetAppDomain())
            callerObj = pCaller->GetExposedObject();

        ARG_SLOT args[7];
        args[0] = ObjToArgSlot(callerObj);
        args[1] = ObjToArgSlot(NULL);
        args[2] = ObjToArgSlot(NULL);
        args[3] = PtrToArgSlot(pCallee);
        args[4] = (ARG_SLOT)dclLinktimeCheck;
        args[5] = ObjToArgSlot(NULL);
        args[6] = ObjToArgSlot(NULL);
        throwSecurityException.Call(args);

    UNREACHABLE();
}
#endif // FEATURE_APTCA

#ifdef FEATURE_CAS_POLICY
void DECLSPEC_NORETURN SecurityDeclarative::ThrowHPException(EApiCategories protectedCategories, EApiCategories demandedCategories)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    OBJECTREF hpException = NULL;
    GCPROTECT_BEGIN(hpException);

    MethodTable* pMT = MscorlibBinder::GetClass(CLASS__HOST_PROTECTION_EXCEPTION);
    hpException = (OBJECTREF) AllocateObject(pMT);


    MethodDescCallSite ctor(METHOD__HOST_PROTECTION_EXCEPTION__CTOR);

    ARG_SLOT arg[3] = { 
        ObjToArgSlot(hpException),
        protectedCategories,
        demandedCategories
    };
    ctor.Call(arg);
    
    COMPlusThrow(hpException);

    GCPROTECT_END();
}
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_APTCA
BOOL SecurityDeclarative::IsUntrustedCallerCheckNeeded(MethodDesc *pCalleeMD, Assembly *pCallerAssem)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Assembly *pCalleeAssembly = pCalleeMD->GetAssembly();
    _ASSERTE(pCalleeAssembly != NULL);

    // ATPCA is only enforced for cross-assembly calls, so if the target is not accessable from outside
    // the assembly, or if the caller and callee are both within the same assembly, we do not need to
    // do any APTCA checks
    if (pCallerAssem == pCalleeAssembly)
    {
        return FALSE;
    }

    if (!MethodIsVisibleOutsideItsAssembly(pCalleeMD))
    {
        return FALSE;
    }

    // If the target assembly allows untrusted callers unconditionally, then the call should be allowed
    if (pCalleeAssembly->AllowUntrustedCaller())
    {
        return FALSE;
    }

    // Otherwise, we need to ensure the caller is fully trusted
    return TRUE;
}
#endif // FEATURE_APTCA


#ifdef FEATURE_APTCA
// Do a fulltrust check on the caller if the callee is fully trusted and
// callee did not enable AllowUntrustedCallerChecks
/*static*/
void SecurityDeclarative::DoUntrustedCallerChecks(
        Assembly *pCaller, MethodDesc *pCallee, 
        BOOL fFullStackWalk)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    BOOL fRet = TRUE;

#ifdef _DEBUG
    if (!g_pConfig->Do_AllowUntrustedCaller_Checks())
        return;
#endif

    if (!IsUntrustedCallerCheckNeeded(pCallee, pCaller))
        return;

    // Expensive calls after this point, this could end up resolving policy

    if (fFullStackWalk)
    {
        // It is possible that wrappers like VBHelper libraries that are
        // fully trusted, make calls to public methods that do not have
        // safe for Untrusted caller custom attribute set.
        // Like all other link demand that gets transformed to a full stack
        // walk for reflection, calls to public methods also gets
        // converted to full stack walk

        OBJECTREF permSet = NULL;
        GCPROTECT_BEGIN(permSet);

        GetPermissionInstance(&permSet, SECURITY_FULL_TRUST);
        EX_TRY
        {
            SecurityStackWalk::DemandSet(SSWT_LATEBOUND_LINKDEMAND, permSet);
        }
        EX_CATCH
        {
            fRet = FALSE;
        }
        EX_END_CATCH(RethrowTerminalExceptions);

        GCPROTECT_END();
    }
    else
    {
        _ASSERTE(pCaller);

        // Link Demand only, no full stack walk here
        if (!pCaller->GetSecurityDescriptor()->IsFullyTrusted())
            fRet = FALSE;
    }

    if (!fRet)
    {
        ThrowAPTCAException(pCaller, pCallee);
    }
}

#endif // FEATURE_APTCA

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

#ifdef FEATURE_CAS_POLICY
    MethodTable * pMT = pMD->GetMethodTable();

    // Class level first.
    if (pMT->GetClass()->RequiresLinktimeCheck())
        *pClassCas = TypeSecurityDescriptor::GetLinktimePermissions(pMT, pClassNonCas);

    // Then the method level.
    if (IsMdHasSecurity(pMD->GetAttrs()))
        *pMethodCas = MethodSecurityDescriptor::GetLinktimePermissions(pMD,  pMethodNonCas);
#endif
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

#if defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)
    ModuleSecurityDescriptor *pMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pMD->GetAssembly());

    // If the method does not allow partially trusted callers, then the check is because we need to ensure all
    // callers are fully trusted.
    if (!pMSD->IsAPTCA())
    {
        reason |= LinktimeCheckReason_AptcaCheck;
    }
#endif // defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)

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

#ifdef FEATURE_CAS_POLICY
// Issue an inheritance demand against the target assembly

// static
void SecurityDeclarative::InheritanceDemand(Assembly *pTargetAssembly, OBJECTREF refDemand)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pTargetAssembly));
        PRECONDITION(refDemand != NULL);
    }
    CONTRACTL_END;

    struct
    {
        OBJECTREF refDemand;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.refDemand = refDemand;

    GCPROTECT_BEGIN(gc);

    IAssemblySecurityDescriptor *pTargetASD = pTargetAssembly->GetSecurityDescriptor();
    SecurityStackWalk::LinkOrInheritanceCheck(pTargetASD,
                                              gc.refDemand,
                                              pTargetAssembly,
                                              dclInheritanceCheck);
    GCPROTECT_END();
}

// static
void SecurityDeclarative::InheritanceLinkDemandCheck(Assembly *pTargetAssembly, MethodDesc * pMDLinkDemand)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pTargetAssembly));
        PRECONDITION(CheckPointer(pMDLinkDemand));
    }
    CONTRACTL_END;

    GCX_COOP();
    struct
    {
        OBJECTREF refClassCas;
        OBJECTREF refClassNonCas;
        OBJECTREF refMethodCas;
        OBJECTREF refMethodNonCas;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    Security::RetrieveLinktimeDemands(pMDLinkDemand,
                                        &gc.refClassCas,
                                        &gc.refClassNonCas,
                                        &gc.refMethodCas,
                                        &gc.refMethodNonCas);

    if (gc.refClassCas != NULL)
    {
        InheritanceDemand(pTargetAssembly, gc.refClassCas);
    }

    if (gc.refMethodCas != NULL)
    {
        InheritanceDemand(pTargetAssembly, gc.refMethodCas);
    }

    GCPROTECT_END();
}

// Issue a FullTrust inheritance demand against the target assembly

// static
void SecurityDeclarative::FullTrustInheritanceDemand(Assembly *pTargetAssembly)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pTargetAssembly));
    }
    CONTRACTL_END;

    GCX_COOP();

    struct
    {
        OBJECTREF refFullTrust;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    gc.refFullTrust = Security::CreatePermissionSet(TRUE);
    InheritanceDemand(pTargetAssembly, gc.refFullTrust);

    GCPROTECT_END();
}

// Issue a FullTrust link demand against the target assembly

// static
void SecurityDeclarative::FullTrustLinkDemand(Assembly *pTargetAssembly)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pTargetAssembly));
    }
    CONTRACTL_END;

    struct
    {
        OBJECTREF refFullTrust;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    gc.refFullTrust = Security::CreatePermissionSet(TRUE);
    IAssemblySecurityDescriptor *pTargetASD = pTargetAssembly->GetSecurityDescriptor();
    SecurityStackWalk::LinkOrInheritanceCheck(pTargetASD,
                                              gc.refFullTrust,
                                              pTargetAssembly,
                                              dclLinktimeCheck);
    GCPROTECT_END();
}

// Used by interop to simulate the effect of link demands when the caller is
// in fact script constrained by an appdomain setup by IE.
void SecurityDeclarative::CheckLinkDemandAgainstAppDomain(MethodDesc *pMD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    if (!pMD->RequiresLinktimeCheck())
        return;

    // Find the outermost (closest to caller) appdomain. This
    // represents the domain in which the unmanaged caller is
    // considered to "live" (or, at least, be constrained by).
    AppDomain *pDomain = GetThread()->GetInitialDomain();

    // The link check is only performed if this app domain has
    // security permissions associated with it, which will be
    // the case for all IE scripting callers that have got this
    // far because we automatically reported our managed classes
    // as "safe for scripting".
    // 
    // We also can't do the check if the AppDomain isn't fully
    // setup yet, since we might not have a domain grant set.
    // This is acceptable, since the only code that should run
    // during AppDomain creation is fully trusted.
    IApplicationSecurityDescriptor *pSecDesc = pDomain->GetSecurityDescriptor();
    if (pSecDesc == NULL || pSecDesc->IsInitializationInProgress() || pSecDesc->IsDefaultAppDomain())
        return;

    struct _gc
    {
        OBJECTREF refGrant;
        OBJECTREF refRefused;
        OBJECTREF refClassNonCasDemands;
        OBJECTREF refClassCasDemands;
        OBJECTREF refMethodNonCasDemands;
        OBJECTREF refMethodCasDemands;
        OBJECTREF refAssembly;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

#ifdef FEATURE_APTCA
    // Do a fulltrust check on the caller if the callee did not enable
    // AllowUntrustedCallerChecks. Pass a NULL caller assembly:
    // DoUntrustedCallerChecks needs to be able to cope with this.
    SecurityDeclarative::DoUntrustedCallerChecks(NULL, pMD, TRUE);
#endif // FEATURE_APTCA

    // Fetch link demand sets from all the places in metadata where we might
    // find them (class and method). These might be split into CAS and non-CAS
    // sets as well.
    SecurityDeclarative::RetrieveLinktimeDemands(pMD,
                                      &gc.refClassCasDemands,
                                      &gc.refClassNonCasDemands,
                                      &gc.refMethodCasDemands,
                                      &gc.refMethodNonCasDemands);

    // Check CAS link demands.
    bool fGotGrantSet = false;
    if (gc.refClassCasDemands != NULL || gc.refMethodCasDemands != NULL)
    {
        // Get grant (and possibly denied) sets from the app
        // domain.
        gc.refGrant = pSecDesc->GetGrantedPermissionSet(NULL);
        fGotGrantSet = true;
        gc.refAssembly = pMD->GetAssembly()->GetExposedObject();

        if (gc.refClassCasDemands != NULL)
            SecurityStackWalk::CheckSetHelper(&gc.refClassCasDemands,
                                                        &gc.refGrant,
                                                        &gc.refRefused,
                                                        pDomain,
                                                        pMD,
                                                        &gc.refAssembly,
                                                        dclLinktimeCheck);

        if (gc.refMethodCasDemands != NULL)
            SecurityStackWalk::CheckSetHelper(&gc.refMethodCasDemands,
                                                        &gc.refGrant,
                                                        &gc.refRefused,
                                                        pDomain,
                                                        pMD,
                                                        &gc.refAssembly,
                                                        dclLinktimeCheck);

    }

    // Non-CAS demands are not applied against a grant
    // set, they're standalone.
    if (gc.refClassNonCasDemands != NULL)
        CheckNonCasDemand(&gc.refClassNonCasDemands);

    if (gc.refMethodNonCasDemands != NULL)
        CheckNonCasDemand(&gc.refMethodNonCasDemands);

#ifndef FEATURE_CORECLR   
    // On CORECLR, we do this from the JIT callouts if the caller is transparent: if caller is critical, no checks needed

    // We perform automatic linktime checks for UnmanagedCode in three cases:
    //   o  P/Invoke calls (shouldn't get these here, but let's be paranoid).
    //   o  Calls through an interface that have a suppress runtime check
    //      attribute on them (these are almost certainly interop calls).
    //   o  Interop calls made through method impls.
    // Just walk the stack in these cases, they'll be extremely rare and the
    // perf delta isn't that huge.
    if (pMD->IsNDirect() ||
        (pMD->IsInterface() &&
         (pMD->GetMDImport()->GetCustomAttributeByName(pMD->GetMethodTable()->GetCl(),
                                                      COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                      NULL,
                                                      NULL) == S_OK ||
          pMD->GetMDImport()->GetCustomAttributeByName(pMD->GetMemberDef(),
                                                      COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                      NULL,
                                                      NULL) == S_OK) ) ||
        (pMD->IsComPlusCall() && !pMD->IsInterface()))
        SecurityStackWalk::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_UNMANAGED_CODE);
#endif // FEATURE_CORECLR

    GCPROTECT_END();
}

























//-----------------------------------------------------------------------------
//
//
//     CODE FOR PERFORMING RUN-TIME CHECKS
//
//
//-----------------------------------------------------------------------------

void SecurityDeclarative::EnsureAssertAllowed(MethodDesc *pMeth, MethodSecurityDescriptor *pMSD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pMeth));
        PRECONDITION(pMSD == NULL || pMSD->GetMethod() == pMeth);
    } CONTRACTL_END;

    // Check if this Assembly has permission to assert 
    if (pMSD == NULL || !pMSD->CanAssert()) // early out if we have an MSD and we already have checked this permission
    {
        Module* pModule = pMeth->GetModule();
        PREFIX_ASSUME_MSG(pModule != NULL, "Should be a Module pointer here");

        if (!Security::CanAssert(pModule))
            SecurityPolicy::ThrowSecurityException(g_SecurityPermissionClassName, SPFLAGSASSERTION);
    }

    // Check if the Method is allowed to assert based on transparent/critical classification
    if (!SecurityTransparent::IsAllowedToAssert(pMeth) && Security::IsTransparencyEnforcementEnabled())
    {
#ifdef _DEBUG
        if (g_pConfig->LogTransparencyErrors())
        {
            SecurityTransparent::LogTransparencyError(pMeth, "Transparent method using a security assert");
        }
#endif // _DEBUG
        // if assembly is transparent fail the ASSERT operations
        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_AssertTransparentCode"));
    }

    return;
}

void SecurityDeclarative::InvokeDeclarativeActions (MethodDesc *pMeth, DeclActionInfo *pActions, MethodSecurityDescriptor *pMSD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    OBJECTREF       refPermSet = NULL;
    ARG_SLOT           arg = 0;

    // If we get a real PermissionSet, then invoke the action.
    switch (pActions->dwDeclAction)
    {
    case DECLSEC_DEMANDS:
        SecurityStackWalk::DemandSet(SSWT_DECLARATIVE_DEMAND, pActions->pPCE, dclDemand);
        break;

    case DECLSEC_ASSERTIONS:
        EnsureAssertAllowed(pMeth, pMSD);
        GetThread()->IncrementAssertCount();
                break;

    case DECLSEC_DENIALS:
    case DECLSEC_PERMITONLY:
        GetThread()->IncrementOverridesCount();
        break;

    case DECLSEC_NONCAS_DEMANDS:
        refPermSet = pActions->pPCE->CreateManagedPsetObject (dclNonCasDemand);
        if (refPermSet == NULL)
            break;
        if(!((PERMISSIONSETREF)refPermSet)->CheckedForNonCas() ||((PERMISSIONSETREF)refPermSet)->ContainsNonCas())
        {
            GCPROTECT_BEGIN(refPermSet);
            MethodDescCallSite demand(METHOD__PERMISSION_SET__DEMAND_NON_CAS, &refPermSet);

            arg = ObjToArgSlot(refPermSet);
            demand.Call(&arg);
            GCPROTECT_END();
        }
        break;

    default:
        _ASSERTE(!"Unknown action requested in InvokeDeclarativeActions");
        break;

    } // switch
}


//
// CODE FOR PERFORMING RUN-TIME CHECKS
//
extern LPVOID GetSecurityObjectForFrameInternal(StackCrawlMark *stackMark, INT32 create, OBJECTREF *pRefSecDesc);    

namespace 
{
    inline void UpdateFrameSecurityObj(DWORD dwAction, OBJECTREF *refPermSet, OBJECTREF * pSecObj)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            INJECT_FAULT(COMPlusThrowOM(););
        } CONTRACTL_END;

        GetSecurityObjectForFrameInternal(NULL, true, pSecObj);

        FRAMESECDESCREF fsdRef = (FRAMESECDESCREF)*pSecObj;
        switch (dwAction)
        {
        // currently we require declarative security to store the data in both the fields in the FSD
            case dclAssert:
                fsdRef->SetDeclarativeAssertions(*refPermSet);  
                {
                    PERMISSIONSETREF psRef = (PERMISSIONSETREF)*refPermSet;
                    if (psRef != NULL && psRef->IsUnrestricted())
                        fsdRef->SetAssertFT(TRUE);
                }
                break;

            case dclDeny:
                fsdRef->SetDeclarativeDenials(*refPermSet);
                break;

            case dclPermitOnly:            
                fsdRef->SetDeclarativeRestrictions(*refPermSet);
                break;

            default:
                _ASSERTE(0 && "Unreached, add code to handle if reached here...");
                break;
        }
    }
}

void SecurityDeclarative::InvokeDeclarativeStackModifiers(MethodDesc * pMeth, DeclActionInfo * pActions, OBJECTREF * pSecObj)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    OBJECTREF       refPermSet = NULL;

    // If we get a real PermissionSet, then invoke the action.
    switch (pActions->dwDeclAction)
    {
    case DECLSEC_DEMANDS:
    case DECLSEC_NONCAS_DEMANDS:
        // Nothing to do for demands
        break;

    case DECLSEC_ASSERTIONS:
        refPermSet = pActions->pPCE->CreateManagedPsetObject (dclAssert);
        if (refPermSet == NULL)
            break;
        GCPROTECT_BEGIN(refPermSet);
        // Now update the frame security object
        UpdateFrameSecurityObj(dclAssert, &refPermSet, pSecObj);
        GCPROTECT_END();
        break;

    case DECLSEC_DENIALS:
        // Update the frame security object
        refPermSet = pActions->pPCE->CreateManagedPsetObject (dclDeny);
 
        if (refPermSet == NULL)
            break;

        GCPROTECT_BEGIN(refPermSet);

#ifdef FEATURE_CAS_POLICY
        // Deny is only valid if we're in legacy CAS mode
        IApplicationSecurityDescriptor *pSecDesc = GetAppDomain()->GetSecurityDescriptor();
        if (!pSecDesc->IsLegacyCasPolicyEnabled())
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_CasDeny"));
        }
#endif // FEATURE_CAS_POLICY

        UpdateFrameSecurityObj(dclDeny, &refPermSet, pSecObj);

        GCPROTECT_END();
        break;

    case DECLSEC_PERMITONLY:
        // Update the frame security object
        refPermSet = pActions->pPCE->CreateManagedPsetObject (dclPermitOnly);

        if (refPermSet == NULL)
            break;
        GCPROTECT_BEGIN(refPermSet);
        UpdateFrameSecurityObj(dclPermitOnly, &refPermSet, pSecObj);
        GCPROTECT_END();
        break;


    default:
        _ASSERTE(!"Unknown action requested in InvokeDeclarativeStackModifiers");
        break;

    } // switch
}

void SecurityDeclarative::DoDeclarativeActions(MethodDesc *pMeth, DeclActionInfo *pActions, LPVOID pSecObj, MethodSecurityDescriptor *pMSD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifndef FEATURE_CAS_POLICY
    // In the CoreCLR, we don't support CAS actions outside mscorlib.
    // However, we do have to expose certain types in mscorlib due to compiler requirements 
    // (c# compiler requires us to expose SecurityPermission/SecurityAction etc so that it can
    // insert a RequestMinimum for SkipVerification).
    // This means that code outside mscorlib could construct IL that has declarative security
    // in it. This is not a security issue - even if they try to create IL that asserts for
    // permissions they don't have, it's not going to work for the same reasons it didn't in the desktop.
    // However, we could have bugs like DDB 120109 where they can cause Demands to fail etc.
    // So for goodness, we're not going to do any runtime declarative work on assemblies other than mscorlib.
    if (!pMeth->GetModule()->IsSystem())
    {
        // Non-mscorlib code reached... exit
        return;
    }
#endif //!FEATURE_CAS_POLICY
    

    // --------------------------------------------------------------------------- //
    //          D E C L A R A T I V E   S E C U R I T Y   D E M A N D S            //
    // --------------------------------------------------------------------------- //
    // The frame is now fully formed, arguments have been copied into place,
    // and synchronization monitors have been entered if necessary.  At this
    // point, we are prepared for something to throw an exception, so we may
    // check for declarative security demands and execute them.  We need a
    // well-formed frame and synchronization domain to accept security excep-
    // tions thrown by the SecurityManager.  We MAY need argument values in
    // the frame so that the arguments may be finalized if security throws an
    // exception across them (unknown).  
    if (pActions != NULL && pActions->dwDeclAction == DECLSEC_UNMNGD_ACCESS_DEMAND &&
        pActions->pNext == NULL)
    {
        /* We special-case the security check on single pinvoke/interop calls
           so we can avoid setting up the GCFrame */

        SecurityStackWalk::SpecialDemand(SSWT_DECLARATIVE_DEMAND, SECURITY_UNMANAGED_CODE);
        return;
    }
    else
    {
#ifdef FEATURE_COMPRESSEDSTACK
        // If this is an anonymously hosted dynamic method, there aren't any direct modifiers, but if it has a compressed stack that
        // might have modifiers, mark that there are modifiers so we make sure to do a stack walk
        if(SecurityStackWalk::MethodIsAnonymouslyHostedDynamicMethodWithCSToEvaluate(pMeth)) 
        {
            // We don't know how many asserts or overrides might be in the compressed stack,
            // but we just need to increment the counters to ensure optimizations don't skip CS evaluation
            GetThread()->IncrementAssertCount();
            GetThread()->IncrementOverridesCount();
        }
#endif // FEATURE_COMPRESSEDSTACK

        for (/**/; pActions; pActions = pActions->pNext)
        {
            if (pActions->dwDeclAction == DECLSEC_UNMNGD_ACCESS_DEMAND)
            {
                SecurityStackWalk::SpecialDemand(SSWT_DECLARATIVE_DEMAND, SECURITY_UNMANAGED_CODE);
            }
            else
            {
                InvokeDeclarativeActions(pMeth, pActions, pMSD);
            }
        }

    }
}
void SecurityDeclarative::DoDeclarativeStackModifiers(MethodDesc *pMeth, AppDomain* pAppDomain, LPVOID pSecObj)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifndef FEATURE_CAS_POLICY
    // In the CoreCLR, we don't support CAS actions outside mscorlib.
    // However, we do have to expose certain types in mscorlib due to compiler requirements 
    // (c# compiler requires us to expose SecurityPermission/SecurityAction etc so that it can
    // insert a RequestMinimum for SkipVerification).
    // This means that code outside mscorlib could construct IL that has declarative security
    // in it. This is not a security issue - even if they try to create IL that asserts for
    // permissions they don't have, it's not going to work for the same reasons it didn't in the desktop.
    // However, we could have bugs like DDB 120109 where they can cause Demands to fail etc.
    // So for goodness, we're not going to do any runtime declarative work on assemblies other than mscorlib.
    if (!pMeth->GetModule()->IsSystem())
    {
        // Non-mscorlib code reached... exit
        return;
    }
#endif //!FEATURE_CAS_POLICY
        
    
    AppDomain* pCurrentDomain = GetAppDomain();
    
    if (pCurrentDomain != pAppDomain)
    {
        ENTER_DOMAIN_PTR(pAppDomain, ADV_RUNNINGIN)
        {
            DoDeclarativeStackModifiersInternal(pMeth, pSecObj);
        }
        END_DOMAIN_TRANSITION;
    }
    else
    {
        DoDeclarativeStackModifiersInternal(pMeth, pSecObj);
            }
        }

void SecurityDeclarative::DoDeclarativeStackModifiersInternal(MethodDesc *pMeth, LPVOID pSecObj)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    Object** ppSecObject = (Object**) pSecObj;
    _ASSERTE(pMeth->IsInterceptedForDeclSecurity() && !pMeth->IsInterceptedForDeclSecurityCASDemandsOnly());

    MethodSecurityDescriptor MDSecDesc(pMeth);
    MethodSecurityDescriptor::LookupOrCreateMethodSecurityDescriptor(&MDSecDesc);
    DeclActionInfo* pActions = MDSecDesc.GetRuntimeDeclActionInfo();

    OBJECTREF fsdRef = ObjectToOBJECTREF(*ppSecObject);
    GCPROTECT_BEGIN(fsdRef);

    for (/**/; pActions; pActions = pActions->pNext)
    {
        InvokeDeclarativeStackModifiers(pMeth, pActions, &fsdRef);
    }
    // If we had just NON-CAS demands, we'd come here but not create an FSD.
    if (fsdRef != NULL)
    {
        ((FRAMESECDESCREF)(fsdRef))->SetDeclSecComputed(TRUE);

        if (*ppSecObject == NULL)
        {
            // we came in with a NULL FSD and the FSD got created here...so we need to copy it back
            // If we had come in with a non-NULL FSD, that would have been updated and this (shallow/pointer) copy
            // would not be necessary
            *ppSecObject = OBJECTREFToObject(fsdRef);
    }
}

    GCPROTECT_END();
}


void SecurityDeclarative::CheckNonCasDemand(OBJECTREF *prefDemand)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame (prefDemand));
    } CONTRACTL_END;

    if(((PERMISSIONSETREF)*prefDemand)->CheckedForNonCas())
    {
        if(!((PERMISSIONSETREF)*prefDemand)->ContainsNonCas())
            return;
    }
    MethodDescCallSite demand(METHOD__PERMISSION_SET__DEMAND_NON_CAS, prefDemand);
    ARG_SLOT arg = ObjToArgSlot(*prefDemand);
    demand.Call(&arg);
}

#endif // FEATURE_CAS_POLICY

#endif // CROSSGEN_COMPILE

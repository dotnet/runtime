// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: METHODTABLEBUILDER.CPP
//


//

//
// ============================================================================

#include "common.h"

#include "methodtablebuilder.h"

#include "sigbuilder.h"
#include "dllimport.h"
#include "fieldmarshaler.h"
#include "encee.h"
#include "ecmakey.h"
#include "customattribute.h"
#include "typestring.h"

//*******************************************************************************
// Helper functions to sort GCdescs by offset (decending order)
int __cdecl compareCGCDescSeries(const void *arg1, const void *arg2)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    CGCDescSeries* gcInfo1 = (CGCDescSeries*) arg1;
    CGCDescSeries* gcInfo2 = (CGCDescSeries*) arg2;

    return (int)(gcInfo2->GetSeriesOffset() - gcInfo1->GetSeriesOffset());
}

//*******************************************************************************

const char* FormatSig(MethodDesc* pMD, LoaderHeap *pHeap, AllocMemTracker *pamTracker);

#ifdef _DEBUG
unsigned g_dupMethods = 0;
#endif // _DEBUG

//==========================================================================
// This function is very specific about how it constructs a EEClass.  It first
// determines the necessary size of the vtable and the number of statics that
// this class requires.  The necessary memory is then allocated for a EEClass
// and its vtable and statics.  The class members are then initialized and
// the memory is then returned to the caller
//
// LPEEClass CreateClass()
//
// Parameters :
//      [in] scope - scope of the current class not the one requested to be opened
//      [in] cl - class token of the class to be created.
//      [out] ppEEClass - pointer to pointer to hold the address of the EEClass
//                        allocated in this function.
// Return : returns an HRESULT indicating the success of this function.
//
// This parameter has been removed but might need to be reinstated if the
// global for the metadata loader is removed.
//      [in] pIMLoad - MetaDataLoader class/object for the current scope.


//==========================================================================
/*static*/ EEClass *
MethodTableBuilder::CreateClass( Module *pModule,
                                mdTypeDef cl,
                                BOOL fHasLayout,
                                BOOL fDelegate,
                                BOOL fIsEnum,
                                const MethodTableBuilder::bmtGenericsInfo *bmtGenericsInfo,
                                LoaderAllocator * pAllocator,
                                AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!(fHasLayout && fDelegate));
        PRECONDITION(!(fHasLayout && fIsEnum));
        PRECONDITION(CheckPointer(bmtGenericsInfo));
    }
    CONTRACTL_END;

    EEClass *pEEClass = NULL;
    IMDInternalImport *pInternalImport;

    //<TODO>============================================================================
    // vtabsize and static size need to be converted from pointer sizes to #'s
    // of bytes this will be very important for 64 bit NT!
    // We will need to call on IMetaDataLoad to get these sizes and fill out the
    // tables

    // From the classref call on metadata to resolve the classref and check scope
    // to make sure that this class is in the same scope otherwise we need to open
    // a new scope and possibly file.

    // if the scopes are different call the code to load a new file and get the new scope

    // scopes are the same so we can use the existing scope to get the class info

    // This method needs to be fleshed out.more it currently just returns enough
    // space for the defined EEClass and the vtable and statics are not set.
    //=============================================================================</TODO>

    if (fHasLayout)
    {
        pEEClass = new (pAllocator->GetLowFrequencyHeap(), pamTracker) LayoutEEClass();
    }
    else if (fDelegate)
    {
        pEEClass = new (pAllocator->GetLowFrequencyHeap(), pamTracker) DelegateEEClass();
    }
    else
    {
        pEEClass = new (pAllocator->GetLowFrequencyHeap(), pamTracker) EEClass(sizeof(EEClass));
    }

    DWORD dwAttrClass = 0;
    mdToken tkExtends = mdTokenNil;

    // Set up variance info
    if (bmtGenericsInfo->pVarianceInfo)
    {
        // Variance info is an optional field on EEClass, so ensure the optional field descriptor has been
        // allocated.
        EnsureOptionalFieldsAreAllocated(pEEClass, pamTracker, pAllocator->GetLowFrequencyHeap());
        pEEClass->SetVarianceInfo((BYTE*) pamTracker->Track(
            pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(bmtGenericsInfo->GetNumGenericArgs()))));

        memcpy(pEEClass->GetVarianceInfo(), bmtGenericsInfo->pVarianceInfo, bmtGenericsInfo->GetNumGenericArgs());
    }

    pInternalImport = pModule->GetMDImport();

    if (pInternalImport == NULL)
        COMPlusThrowHR(COR_E_TYPELOAD);

    IfFailThrow(pInternalImport->GetTypeDefProps(
        cl,
        &dwAttrClass,
        &tkExtends));

    pEEClass->m_dwAttrClass = dwAttrClass;

    // MDVal check: can't be both tdSequentialLayout and tdExplicitLayout
    if((dwAttrClass & tdLayoutMask) == tdLayoutMask)
        COMPlusThrowHR(COR_E_TYPELOAD);

    if (IsTdInterface(dwAttrClass))
    {
        // MDVal check: must have nil tkExtends and must be tdAbstract
        if((tkExtends & 0x00FFFFFF)||(!IsTdAbstract(dwAttrClass)))
            COMPlusThrowHR(COR_E_TYPELOAD);
    }

    if (fHasLayout)
        pEEClass->SetHasLayout();

    if (IsTdWindowsRuntime(dwAttrClass))
    {
        COMPlusThrowHR(COR_E_TYPELOAD);
    }

#ifdef _DEBUG
    pModule->GetClassLoader()->m_dwDebugClasses++;
#endif

    return pEEClass;
}

//*******************************************************************************
//
// Create a hash of all methods in this class.  The hash is from method name to MethodDesc.
//
MethodTableBuilder::MethodNameHash *
MethodTableBuilder::CreateMethodChainHash(
    MethodTable *pMT)
{
    STANDARD_VM_CONTRACT;

    MethodNameHash *pHash = new (GetStackingAllocator()) MethodNameHash();
    pHash->Init(pMT->GetNumVirtuals(), GetStackingAllocator());

    unsigned numVirtuals = GetParentMethodTable()->GetNumVirtuals();
    for (unsigned i = 0; i < numVirtuals; ++i)
    {
        bmtMethodSlot &slot = (*bmtParent->pSlotTable)[i];
        bmtRTMethod * pMethod = slot.Decl().AsRTMethod();
        const MethodSignature &sig = pMethod->GetMethodSignature();
        pHash->Insert(sig.GetName(), pMethod);
    }

    // Success
    return pHash;
}

//*******************************************************************************
//
// Find a method in this class hierarchy - used ONLY by the loader during layout.  Do not use at runtime.
//
// *ppMemberSignature must be NULL on entry - it and *pcMemberSignature may or may not be filled out
//
// ppMethodDesc will be filled out with NULL if no matching method in the hierarchy is found.
//
// Returns FALSE if there was an error of some kind.
//
// pMethodConstraintsMatch receives the result of comparing the method constraints.
MethodTableBuilder::bmtRTMethod *
MethodTableBuilder::LoaderFindMethodInParentClass(
    const MethodSignature & methodSig,
    BOOL *                  pMethodConstraintsMatch)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(methodSig.GetModule()));
        PRECONDITION(CheckPointer(methodSig.GetSignature()));
        PRECONDITION(HasParent());
        PRECONDITION(methodSig.GetSignatureLength() != 0);
    }
    CONTRACTL_END;

//#if 0
    MethodNameHash::HashEntry * pEntry;

    // Have we created a hash of all the methods in the class chain?
    if (bmtParent->pParentMethodHash == NULL)
    {
        // There may be such a method, so we will now create a hash table to reduce the pain for
        // further lookups

        // <TODO> Are we really sure that this is worth doing? </TODO>
        bmtParent->pParentMethodHash = CreateMethodChainHash(GetParentMethodTable());
    }

    // We have a hash table, so use it
    pEntry = bmtParent->pParentMethodHash->Lookup(methodSig.GetName());

    // Traverse the chain of all methods with this name
    while (pEntry != NULL)
    {
        bmtRTMethod * pEntryMethod = pEntry->m_data;
        const MethodSignature & entrySig = pEntryMethod->GetMethodSignature();

        // Note instantiation info
        {
            if (methodSig.Equivalent(entrySig))
            {
                if (pMethodConstraintsMatch != NULL)
                {
                    // Check the constraints are consistent,
                    // and return the result to the caller.
                    // We do this here to avoid recalculating pSubst.
                    *pMethodConstraintsMatch = MetaSig::CompareMethodConstraints(
                        &methodSig.GetSubstitution(), methodSig.GetModule(), methodSig.GetToken(),
                        &entrySig.GetSubstitution(),  entrySig.GetModule(),  entrySig.GetToken());
                }

                return pEntryMethod;
            }
        }

        // Advance to next item in the hash chain which has the same name
        pEntry = bmtParent->pParentMethodHash->FindNext(pEntry);
    }
//#endif

//@TODO: Move to this code, as the use of a HashTable is broken; overriding semantics
//@TODO: require matching against the most-derived slot of a given name and signature,
//@TODO: (which deals specifically with newslot methods with identical name and sig), but
//@TODO: HashTables are by definition unordered and so we've only been getting by with the
//@TODO: implementation being compatible with the order in which methods were added to
//@TODO: the HashTable in CreateMethodChainHash.
#if 0
    bmtParentInfo::Iterator it(bmtParent->IterateSlots());
    it.MoveTo(static_cast<size_t>(GetParentMethodTable()->GetNumVirtuals()));
    while (it.Prev())
    {
        bmtMethodHandle decl(it->Decl());
        const MethodSignature &declSig(decl.GetMethodSignature());
        if (declSig == methodSig)
        {
            if (pMethodConstraintsMatch != NULL)
            {
                // Check the constraints are consistent,
                // and return the result to the caller.
                // We do this here to avoid recalculating pSubst.
                *pMethodConstraintsMatch = MetaSig::CompareMethodConstraints(
                    &methodSig.GetSubstitution(), methodSig.GetModule(), methodSig.GetToken(),
                    &declSig.GetSubstitution(),  declSig.GetModule(),  declSig.GetToken());
            }

            return decl.AsRTMethod();
        }
    }
#endif // 0

    return NULL;
}

//*******************************************************************************
//
// Given an interface map to fill out, expand pNewInterface (and its sub-interfaces) into it, increasing
// pdwInterfaceListSize as appropriate, and avoiding duplicates.
//
void
MethodTableBuilder::ExpandApproxInterface(
    bmtInterfaceInfo *          bmtInterface,  // out parameter, various parts cumulatively written to.
    const Substitution *        pNewInterfaceSubstChain,
    MethodTable *               pNewInterface,
    InterfaceDeclarationScope   declScope
    COMMA_INDEBUG(MethodTable * dbg_pClassMT))
{
    STANDARD_VM_CONTRACT;

    if (pNewInterface->HasVirtualStaticMethods())
    {
        bmtProp->fHasVirtualStaticMethods = TRUE;
    }

    //#ExpandingInterfaces
    // We expand the tree of inherited interfaces into a set by adding the
    // current node BEFORE expanding the parents of the current node.
    // ****** This must be consistent with code:ExpandExactInterface *******
    // ****** This must be consistent with code:ClassCompat::MethodTableBuilder::BuildInteropVTable_ExpandInterface *******

    // The interface list contains the fully expanded set of interfaces from the parent then
    // we start adding all the interfaces we declare. We need to know which interfaces
    // we declare but do not need duplicates of the ones we declare. This means we can
    // duplicate our parent entries.

    // Is it already present in the list?
    for (DWORD i = 0; i < bmtInterface->dwInterfaceMapSize; i++)
    {
        bmtInterfaceEntry * pItfEntry = &bmtInterface->pInterfaceMap[i];
        bmtRTType * pItfType = pItfEntry->GetInterfaceType();

        // Type Equivalence is not respected for this comparision as you can have multiple type equivalent interfaces on a class
        TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
        if (MetaSig::CompareTypeDefsUnderSubstitutions(pItfType->GetMethodTable(),
                                                       pNewInterface,
                                                       &pItfType->GetSubstitution(),
                                                       pNewInterfaceSubstChain,
                                                       &newVisited))
        {
            if (declScope.fIsInterfaceDeclaredOnType)
            {
                pItfEntry->IsDeclaredOnType() = true;
            }
#ifdef _DEBUG
            //#InjectInterfaceDuplicates_ApproxInterfaces
            // We can inject duplicate interfaces in check builds.
            // Has to be in sync with code:#InjectInterfaceDuplicates_Main
            if (((dbg_pClassMT == NULL) && bmtInterface->dbg_fShouldInjectInterfaceDuplicates) ||
                ((dbg_pClassMT != NULL) && dbg_pClassMT->Debug_HasInjectedInterfaceDuplicates()))
            {
                // The injected duplicate interface should have the same status 'ImplementedByParent' as
                // the original interface (can be false if the interface is implemented indirectly twice)
                declScope.fIsInterfaceDeclaredOnParent = pItfEntry->IsImplementedByParent();
                // Just pretend we didn't find this match, but mark all duplicates as 'DeclaredOnType' if
                // needed
                continue;
            }
#endif //_DEBUG
            return; // found it, don't add it again
        }
    }

    bmtRTType * pNewItfType =
        new (GetStackingAllocator()) bmtRTType(*pNewInterfaceSubstChain, pNewInterface);

    if (bmtInterface->dwInterfaceMapSize >= bmtInterface->dwInterfaceMapAllocated)
    {
        //
        // Grow the array of interfaces
        //
        S_UINT32 dwNewAllocated = S_UINT32(2) * S_UINT32(bmtInterface->dwInterfaceMapAllocated) + S_UINT32(5);

        if (dwNewAllocated.IsOverflow())
        {
            BuildMethodTableThrowException(COR_E_OVERFLOW);
        }

        S_SIZE_T safeSize = S_SIZE_T(sizeof(bmtInterfaceEntry)) *
                            S_SIZE_T(dwNewAllocated.Value());

        if (safeSize.IsOverflow())
        {
            BuildMethodTableThrowException(COR_E_OVERFLOW);
        }

        bmtInterfaceEntry * pNewMap = (bmtInterfaceEntry *)new (GetStackingAllocator()) BYTE[safeSize.Value()];
        memcpy(pNewMap, bmtInterface->pInterfaceMap, sizeof(bmtInterfaceEntry) * bmtInterface->dwInterfaceMapAllocated);

        bmtInterface->pInterfaceMap = pNewMap;
        bmtInterface->dwInterfaceMapAllocated = dwNewAllocated.Value();
    }

    // The interface map memory was just allocated as an array of bytes, so we use
    // in place new to init the new map entry. No need to do anything with the result,
    // so just chuck it.
    CONSISTENCY_CHECK(bmtInterface->dwInterfaceMapSize < bmtInterface->dwInterfaceMapAllocated);
    new ((void *)&bmtInterface->pInterfaceMap[bmtInterface->dwInterfaceMapSize])
        bmtInterfaceEntry(pNewItfType, declScope);

    bmtInterface->dwInterfaceMapSize++;

    // Checking for further expanded interfaces isn't necessary for the system module, as we can rely on the C# compiler
    // to have found all of the interfaces that the type implements, and to place them in the interface list itself. Also
    // we can assume no ambiguous interfaces
    // Code related to this is marked with #SpecialCorelibInterfaceExpansionAlgorithm
    if (!(GetModule()->IsSystem() && IsValueClass()))
    {
        // Make sure to pass in the substitution from the new itf type created above as
        // these methods assume that substitutions are allocated in the stacking heap,
        // not the stack.
        InterfaceDeclarationScope declaredItfScope(declScope.fIsInterfaceDeclaredOnParent, false);
        ExpandApproxDeclaredInterfaces(
            bmtInterface,
            bmtTypeHandle(pNewItfType),
            declaredItfScope
            COMMA_INDEBUG(dbg_pClassMT));
    }
} // MethodTableBuilder::ExpandApproxInterface

//*******************************************************************************
// Arguments:
//   dbg_pClassMT - Class on which the interfaces are declared (either explicitly or implicitly).
//                  It will never be an interface. It may be NULL (if it is the type being built).
void
MethodTableBuilder::ExpandApproxDeclaredInterfaces(
    bmtInterfaceInfo *          bmtInterface,  // out parameter, various parts cumulatively written to.
    bmtTypeHandle               thType,
    InterfaceDeclarationScope   declScope
    COMMA_INDEBUG(MethodTable * dbg_pClassMT))
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((dbg_pClassMT == NULL) || !dbg_pClassMT->IsInterface());

    HRESULT hr;
    // Iterate the list of interfaces declared by thType and add them to the map.
    InterfaceImplEnum ie(thType.GetModule(), thType.GetTypeDefToken(), &thType.GetSubstitution());
    while ((hr = ie.Next()) == S_OK)
    {
        MethodTable *pGenericIntf = ClassLoader::LoadApproxTypeThrowing(
            thType.GetModule(), ie.CurrentToken(), NULL, NULL).GetMethodTable();
        CONSISTENCY_CHECK(pGenericIntf->IsInterface());

        ExpandApproxInterface(bmtInterface,
                              ie.CurrentSubst(),
                              pGenericIntf,
                              declScope
                              COMMA_INDEBUG(dbg_pClassMT));
    }
    if (FAILED(hr))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
    }
} // MethodTableBuilder::ExpandApproxDeclaredInterfaces

//*******************************************************************************
void
MethodTableBuilder::ExpandApproxInheritedInterfaces(
    bmtInterfaceInfo *      bmtInterface,
    bmtRTType *             pParentType)
{
    STANDARD_VM_CONTRACT;

    // Expand interfaces in superclasses first.  Interfaces inherited from parents
    // must have identical indexes as in the parent.
    bmtRTType * pParentOfParent = pParentType->GetParentType();

    //#InterfaceMap_SupersetOfParent
    // We have to load parent's interface map the same way the parent did it (as open type).
    // Further code depends on this:
    //    code:#InterfaceMap_UseParentInterfaceImplementations
    // We check that it is truth:
    //    code:#ApproxInterfaceMap_SupersetOfParent
    //    code:#ExactInterfaceMap_SupersetOfParent
    //
    //#InterfaceMap_CanonicalSupersetOfParent
    // Note that canonical instantiation of parent can have different interface instantiations in the
    // interface map than derived type:
    //    class MyClass<T> : MyBase<string, T>, I<T>
    //    class MyBase<U, V> : I<U>
    // Type MyClass<_Canon> has MyBase<_Canon,_Canon> as parent. The interface maps are:
    //    MyBase<_Canon,_Canon> ... I<_Canon>
    //    MyClass<_Canon> ... I<string> (#1)
    //                        I<_Canon> (#2)
    // The I's instantiation I<string> (#1) in MyClass and I<_Canon> from MyBase are not the same
    // instantiations.

    // Backup parent substitution
    Substitution parentSubstitution = pParentType->GetSubstitution();
    // Make parent an open type
    pParentType->SetSubstitution(Substitution());

    if (pParentOfParent != NULL)
    {
        ExpandApproxInheritedInterfaces(bmtInterface, pParentOfParent);
    }

    InterfaceDeclarationScope declScope(true, false);
    ExpandApproxDeclaredInterfaces(
        bmtInterface,
        bmtTypeHandle(pParentType),
        declScope
        COMMA_INDEBUG(pParentType->GetMethodTable()));

    // Make sure we loaded the same number of interfaces as the parent type itself
    CONSISTENCY_CHECK(pParentType->GetMethodTable()->GetNumInterfaces() == bmtInterface->dwInterfaceMapSize);

    // Restore parent's substitution
    pParentType->SetSubstitution(parentSubstitution);
} // MethodTableBuilder::ExpandApproxInheritedInterfaces

//*******************************************************************************
// Fill out a fully expanded interface map, such that if we are declared to
// implement I3, and I3 extends I1,I2, then I1,I2 are added to our list if
// they are not already present.
void
MethodTableBuilder::LoadApproxInterfaceMap()
{
    STANDARD_VM_CONTRACT;

    bmtInterface->dwInterfaceMapSize = 0;

#ifdef _DEBUG
    //#InjectInterfaceDuplicates_Main
    // We will inject duplicate interfaces in check builds if env. var.
    // COMPLUS_INTERNAL_TypeLoader_InjectInterfaceDuplicates is set to TRUE for all types (incl. non-generic
    // types).
    // This should allow us better test coverage of duplicates in interface map.
    //
    // The duplicates are legal for some types:
    //     A<T> : I<T>
    //     B<U,V> : A<U>, I<V>
    //     C : B<int,int>
    //   where the interface maps are:
    //     A<T>             ... 1 item:  I<T>
    //     A<int>           ... 1 item:  I<int>
    //     B<U,V>           ... 2 items: I<U>, I<V>
    //     B<int,int>       ... 2 items: I<int>, I<int>
    //     B<_Canon,_Canon> ... 2 items: I<_Canon>, I<_Canon>
    //     B<string,string> ... 2 items: I<string>, I<string>
    //     C                ... 2 items: I<int>, I<int>
    //     Note: C had only 1 item (I<int>) in CLR 2.0 RTM/SP1/SP2 and early in CLR 4.0.
    //
    // We will create duplicate from every re-implemented interface (incl. non-generic):
    //   code:#InjectInterfaceDuplicates_ApproxInterfaces
    //   code:#InjectInterfaceDuplicates_LoadExactInterfaceMap
    //   code:#InjectInterfaceDuplicates_ExactInterfaces
    //
    // Note that we don't have to do anything for COM, because COM has its own interface map
    // (code:InteropMethodTableData)which is independent on type's interface map and is created only from
    // non-generic interfaces (see code:ClassCompat::MethodTableBuilder::BuildInteropVTable_InterfaceList)

    // We need to keep track which interface duplicates were injected. Right now its either all interfaces
    // (declared on the type being built, not inheritted) or none. In the future we could inject duplicates
    // just for some of them.
    bmtInterface->dbg_fShouldInjectInterfaceDuplicates =
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TypeLoader_InjectInterfaceDuplicates) != 0);
    if (bmtGenerics->Debug_GetTypicalMethodTable() != NULL)
    {   // It's safer to require that all instantiations have the same injected interface duplicates.
        // In future we could inject different duplicates for various non-shared instantiations.

        // Use the same injection status as typical instantiation
        bmtInterface->dbg_fShouldInjectInterfaceDuplicates =
            bmtGenerics->Debug_GetTypicalMethodTable()->Debug_HasInjectedInterfaceDuplicates();

        if (GetModule() == g_pObjectClass->GetModule())
        {   // CoreLib has some weird hardcoded information about interfaces (e.g.
            // code:CEEPreloader::ApplyTypeDependencyForSZArrayHelper), so we don't inject duplicates into
            // CoreLib types
            bmtInterface->dbg_fShouldInjectInterfaceDuplicates = FALSE;
        }
    }
#endif //_DEBUG

    // First inherit all the parent's interfaces.  This is important, because our interface map must
    // list the interfaces in identical order to our parent.
    //
    // <NICE> we should document the reasons why.  One reason is that DispatchMapTypeIDs can be indexes
    // into the list </NICE>
    if (HasParent())
    {
        ExpandApproxInheritedInterfaces(bmtInterface, GetParentType());
#ifdef _DEBUG
        //#ApproxInterfaceMap_SupersetOfParent
        // Check that parent's interface map is the same as what we just computed
        // See code:#InterfaceMap_SupersetOfParent
        {
            MethodTable * pParentMT = GetParentMethodTable();
            _ASSERTE(pParentMT->GetNumInterfaces() == bmtInterface->dwInterfaceMapSize);

            MethodTable::InterfaceMapIterator parentInterfacesIterator = pParentMT->IterateInterfaceMap();
            UINT32 nInterfaceIndex = 0;
            while (parentInterfacesIterator.Next())
            {
                // Compare TypeDefs of the parent's interface and this interface (full MT comparison is in
                // code:#ExactInterfaceMap_SupersetOfParent)
                OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOAD_APPROXPARENTS);
                _ASSERTE(parentInterfacesIterator.GetInterfaceInfo()->GetApproxMethodTable(pParentMT->GetLoaderModule())->HasSameTypeDefAs(
                    bmtInterface->pInterfaceMap[nInterfaceIndex].GetInterfaceType()->GetMethodTable()));
                nInterfaceIndex++;
            }
            _ASSERTE(nInterfaceIndex == bmtInterface->dwInterfaceMapSize);
        }
#endif //_DEBUG
    }

    // Now add in any freshly declared interfaces, possibly augmenting the flags
    InterfaceDeclarationScope declScope(false, true);
    ExpandApproxDeclaredInterfaces(
        bmtInterface,
        bmtInternal->pType,
        declScope
        COMMA_INDEBUG(NULL));
} // MethodTableBuilder::LoadApproxInterfaceMap

//*******************************************************************************
// Fills array of TypeIDs with all duplicate occurrences of pDeclIntfMT in the interface map.
//
// Arguments:
//    rg/c DispatchMapTypeIDs - Array of TypeIDs and its count of elements.
//    pcIfaceDuplicates - Number of duplicate occurrences of the interface in the interface map (ideally <=
//         count of elements TypeIDs.
//
// Note: If the passed rgDispatchMapTypeIDs array is smaller than the number of duplicates, fills it
// with the duplicates that fit and returns number of all existing duplicates (not just those fileld in the
// array) in pcIfaceDuplicates.
//
void
MethodTableBuilder::ComputeDispatchMapTypeIDs(
    MethodTable *        pDeclInftMT,
    const Substitution * pDeclIntfSubst,
    DispatchMapTypeID *  rgDispatchMapTypeIDs,
    UINT32               cDispatchMapTypeIDs,
    UINT32 *             pcIfaceDuplicates)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pDeclInftMT->IsInterface());

    // Count of interface duplicates (also used as index into TypeIDs array)
    *pcIfaceDuplicates = 0;
    for (DWORD idx = 0; idx < bmtInterface->dwInterfaceMapSize; idx++)
    {
        bmtInterfaceEntry * pItfEntry = &bmtInterface->pInterfaceMap[idx];
        bmtRTType * pItfType = pItfEntry->GetInterfaceType();
        // Type Equivalence is forbidden in interface type ids.
        TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
        if (MetaSig::CompareTypeDefsUnderSubstitutions(pItfType->GetMethodTable(),
                                                       pDeclInftMT,
                                                       &pItfType->GetSubstitution(),
                                                       pDeclIntfSubst,
                                                       &newVisited))
        {   // We found another occurrence of this interface
            // Can we fit it into the TypeID array?
            if (*pcIfaceDuplicates < cDispatchMapTypeIDs)
            {
                rgDispatchMapTypeIDs[*pcIfaceDuplicates] = DispatchMapTypeID::InterfaceClassID(idx);
            }
            // Increase number of duplicate interfaces
            (*pcIfaceDuplicates)++;
        }
    }
} // MethodTableBuilder::ComputeDispatchMapTypeIDs

//*******************************************************************************
/*static*/
VOID DECLSPEC_NORETURN
MethodTableBuilder::BuildMethodTableThrowException(
    HRESULT hr,
    const bmtErrorInfo & bmtError)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LPCUTF8 pszClassName, pszNameSpace;
    if (FAILED(bmtError.pModule->GetMDImport()->GetNameOfTypeDef(bmtError.cl, &pszClassName, &pszNameSpace)))
    {
        pszClassName = pszNameSpace = "Invalid TypeDef record";
    }

    if (IsNilToken(bmtError.dMethodDefInError) && (bmtError.szMethodNameForError == NULL))
    {
        if (hr == E_OUTOFMEMORY)
        {
            COMPlusThrowOM();
        }
        else
            bmtError.pModule->GetAssembly()->ThrowTypeLoadException(
                pszNameSpace, pszClassName, bmtError.resIDWhy);
    }
    else
    {
        LPCUTF8 szMethodName;
        if (bmtError.szMethodNameForError == NULL)
        {
            if (FAILED((bmtError.pModule->GetMDImport())->GetNameOfMethodDef(bmtError.dMethodDefInError, &szMethodName)))
            {
                szMethodName = "Invalid MethodDef record";
            }
        }
        else
        {
            szMethodName = bmtError.szMethodNameForError;
        }

        bmtError.pModule->GetAssembly()->ThrowTypeLoadException(
            pszNameSpace, pszClassName, szMethodName, bmtError.resIDWhy);
    }
} // MethodTableBuilder::BuildMethodTableThrowException

//*******************************************************************************
void MethodTableBuilder::SetBMTData(
    LoaderAllocator *bmtAllocator,
    bmtErrorInfo *bmtError,
    bmtProperties *bmtProp,
    bmtVtable *bmtVT,
    bmtParentInfo *bmtParent,
    bmtInterfaceInfo *bmtInterface,
    bmtMetaDataInfo *bmtMetaData,
    bmtMethodInfo *bmtMethod,
    bmtMethAndFieldDescs *bmtMFDescs,
    bmtFieldPlacement *bmtFP,
    bmtInternalInfo *bmtInternal,
    bmtGCSeriesInfo *bmtGCSeries,
    bmtMethodImplInfo *bmtMethodImpl,
    const bmtGenericsInfo *bmtGenerics,
    bmtEnumFieldInfo *bmtEnumFields)
{
    LIMITED_METHOD_CONTRACT;
    this->bmtAllocator = bmtAllocator;
    this->bmtError = bmtError;
    this->bmtProp = bmtProp;
    this->bmtVT = bmtVT;
    this->bmtParent = bmtParent;
    this->bmtInterface = bmtInterface;
    this->bmtMetaData = bmtMetaData;
    this->bmtMethod = bmtMethod;
    this->bmtMFDescs = bmtMFDescs;
    this->bmtFP = bmtFP;
    this->bmtInternal = bmtInternal;
    this->bmtGCSeries = bmtGCSeries;
    this->bmtMethodImpl = bmtMethodImpl;
    this->bmtGenerics = bmtGenerics;
    this->bmtEnumFields = bmtEnumFields;
}

//*******************************************************************************
// Used by MethodTableBuilder

MethodTableBuilder::bmtRTType *
MethodTableBuilder::CreateTypeChain(
    MethodTable *        pMT,
    const Substitution & subst)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(GetStackingAllocator()));
        PRECONDITION(CheckPointer(pMT));
    } CONTRACTL_END;

    pMT = pMT->GetCanonicalMethodTable();

    bmtRTType * pType = new (GetStackingAllocator())
        bmtRTType(subst, pMT);

    MethodTable * pMTParent = pMT->GetParentMethodTable();
    if (pMTParent != NULL)
    {
        pType->SetParentType(
            CreateTypeChain(
                pMTParent,
                pMT->GetSubstitutionForParent(&pType->GetSubstitution())));
    }

    return pType;
}

//*******************************************************************************
/* static */
MethodTableBuilder::bmtRTType *
MethodTableBuilder::bmtRTType::FindType(
    bmtRTType *          pType,
    MethodTable *        pTargetMT)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pType));
        PRECONDITION(CheckPointer(pTargetMT));
    } CONTRACTL_END;

    pTargetMT = pTargetMT->GetCanonicalMethodTable();
    while (pType != NULL &&
           pType->GetMethodTable()->GetCanonicalMethodTable() != pTargetMT)
    {
        pType = pType->GetParentType();
    }

    return pType;
}

//*******************************************************************************
mdTypeDef
MethodTableBuilder::bmtRTType::GetEnclosingTypeToken() const
{
    STANDARD_VM_CONTRACT;

    mdTypeDef tok = mdTypeDefNil;

    if (IsNested())
    {   // This is guaranteed to succeed because the EEClass would not have been
        // set as nested unless a valid token was stored in metadata.
        if (FAILED(GetModule()->GetMDImport()->GetNestedClassProps(
            GetTypeDefToken(), &tok)))
        {
            return mdTypeDefNil;
        }
    }

    return tok;
}

//*******************************************************************************
/*static*/ bool
MethodTableBuilder::MethodSignature::NamesEqual(
    const MethodSignature & sig1,
    const MethodSignature & sig2)
{
    STANDARD_VM_CONTRACT;

    if (sig1.GetNameHash() != sig2.GetNameHash())
    {
        return false;
    }

    if (strcmp(sig1.GetName(), sig2.GetName()) != 0)
    {
        return false;
    }

    return true;
}

//*******************************************************************************
/*static*/ bool
MethodTableBuilder::MethodSignature::SignaturesEquivalent(
    const MethodSignature & sig1,
    const MethodSignature & sig2,
    BOOL allowCovariantReturn)
{
    STANDARD_VM_CONTRACT;

    return !!MetaSig::CompareMethodSigs(
        sig1.GetSignature(), static_cast<DWORD>(sig1.GetSignatureLength()), sig1.GetModule(), &sig1.GetSubstitution(),
        sig2.GetSignature(), static_cast<DWORD>(sig2.GetSignatureLength()), sig2.GetModule(), &sig2.GetSubstitution(),
        allowCovariantReturn);
}

//*******************************************************************************
/*static*/ bool
MethodTableBuilder::MethodSignature::SignaturesExactlyEqual(
    const MethodSignature & sig1,
    const MethodSignature & sig2)
{
    STANDARD_VM_CONTRACT;

    TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
    return !!MetaSig::CompareMethodSigs(
        sig1.GetSignature(), static_cast<DWORD>(sig1.GetSignatureLength()), sig1.GetModule(), &sig1.GetSubstitution(),
        sig2.GetSignature(), static_cast<DWORD>(sig2.GetSignatureLength()), sig2.GetModule(), &sig2.GetSubstitution(),
        FALSE, &newVisited);
}

//*******************************************************************************
bool
MethodTableBuilder::MethodSignature::Equivalent(
    const MethodSignature &rhs) const
{
    STANDARD_VM_CONTRACT;

    return NamesEqual(*this, rhs) && SignaturesEquivalent(*this, rhs, FALSE);
}

//*******************************************************************************
bool
MethodTableBuilder::MethodSignature::ExactlyEqual(
    const MethodSignature &rhs) const
{
    STANDARD_VM_CONTRACT;

    return NamesEqual(*this, rhs) && SignaturesExactlyEqual(*this, rhs);
}

//*******************************************************************************
void
MethodTableBuilder::MethodSignature::GetMethodAttributes() const
{
    STANDARD_VM_CONTRACT;

    IMDInternalImport * pIMD = GetModule()->GetMDImport();
    if (TypeFromToken(GetToken()) == mdtMethodDef)
    {
        DWORD cSig;
        if (FAILED(pIMD->GetNameAndSigOfMethodDef(GetToken(), &m_pSig, &cSig, &m_szName)))
        {   // We have empty name or signature on error, do nothing
        }
        m_cSig = static_cast<size_t>(cSig);
    }
    else
    {
        CONSISTENCY_CHECK(TypeFromToken(m_tok) == mdtMemberRef);
        DWORD cSig;
        if (FAILED(pIMD->GetNameAndSigOfMemberRef(GetToken(), &m_pSig, &cSig, &m_szName)))
        {   // We have empty name or signature on error, do nothing
        }
        m_cSig = static_cast<size_t>(cSig);
    }
}

//*******************************************************************************
UINT32
MethodTableBuilder::MethodSignature::GetNameHash() const
{
    STANDARD_VM_CONTRACT;

    CheckGetMethodAttributes();

    if (m_nameHash == INVALID_NAME_HASH)
    {
        ULONG nameHash = HashStringA(GetName());
        if (nameHash == INVALID_NAME_HASH)
        {
            nameHash /= 2;
        }
        m_nameHash = nameHash;
    }

    return m_nameHash;
}

//*******************************************************************************
MethodTableBuilder::bmtMDType::bmtMDType(
    bmtRTType *             pParentType,
    Module *                pModule,
    mdTypeDef               tok,
    const SigTypeContext &  sigContext)
    : m_pParentType(pParentType),
      m_pModule(pModule),
      m_tok(tok),
      m_enclTok(mdTypeDefNil),
      m_sigContext(sigContext),
      m_subst(),
      m_dwAttrs(0),
      m_pMT(NULL)
{
    STANDARD_VM_CONTRACT;

    IfFailThrow(m_pModule->GetMDImport()->GetTypeDefProps(m_tok, &m_dwAttrs, NULL));

    HRESULT hr = m_pModule->GetMDImport()->GetNestedClassProps(m_tok, &m_enclTok);
    if (FAILED(hr))
    {
        if (hr != CLDB_E_RECORD_NOTFOUND)
        {
            ThrowHR(hr);
        }
        // Just in case GetNestedClassProps sets the out param to some other value
        m_enclTok = mdTypeDefNil;
    }
}

//*******************************************************************************
MethodTableBuilder::bmtRTMethod::bmtRTMethod(
    bmtRTType *     pOwningType,
    MethodDesc *    pMD)
    : m_pOwningType(pOwningType),
      m_pMD(pMD),
      m_methodSig(pMD->GetModule(),
                  pMD->GetMemberDef(),
                  &pOwningType->GetSubstitution())
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
}

//*******************************************************************************
MethodTableBuilder::bmtMDMethod::bmtMDMethod(
    bmtMDType * pOwningType,
    mdMethodDef tok,
    DWORD dwDeclAttrs,
    DWORD dwImplAttrs,
    DWORD dwRVA,
    METHOD_TYPE type,
    METHOD_IMPL_TYPE implType)
    : m_pOwningType(pOwningType),
      m_dwDeclAttrs(dwDeclAttrs),
      m_dwImplAttrs(dwImplAttrs),
      m_dwRVA(dwRVA),
      m_type(type),
      m_implType(implType),
      m_methodSig(pOwningType->GetModule(),
                  tok,
                  &pOwningType->GetSubstitution()),
      m_pMD(NULL),
      m_pUnboxedMD(NULL),
      m_slotIndex(INVALID_SLOT_INDEX),
      m_unboxedSlotIndex(INVALID_SLOT_INDEX)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
    }
//*******************************************************************************
void
MethodTableBuilder::ImportParentMethods()
{
    STANDARD_VM_CONTRACT;

    if (!HasParent())
    {   // If there's no parent, there's no methods to import
        return;
    }

    SLOT_INDEX numMethods = static_cast<SLOT_INDEX>
        (GetParentMethodTable()->GetNumMethods());

    bmtParent->pSlotTable = new (GetStackingAllocator())
        bmtMethodSlotTable(numMethods, GetStackingAllocator());

    MethodTable::MethodIterator it(GetParentMethodTable());
    for (;it.IsValid(); it.Next())
    {
        MethodDesc *  pDeclDesc = NULL;
        MethodTable * pDeclMT   = NULL;
        MethodDesc *  pImplDesc = NULL;
        MethodTable * pImplMT   = NULL;

        if (it.IsVirtual())
        {
            pDeclDesc = it.GetDeclMethodDesc();
            pDeclMT = pDeclDesc->GetMethodTable();
            pImplDesc = it.GetMethodDesc();
            pImplMT = pImplDesc->GetMethodTable();
        }
        else
        {
            pDeclDesc = pImplDesc = it.GetMethodDesc();
            pDeclMT = pImplMT = it.GetMethodDesc()->GetMethodTable();
        }

        CONSISTENCY_CHECK(CheckPointer(pDeclDesc));
        CONSISTENCY_CHECK(CheckPointer(pImplDesc));

        // Create and assign to each slot
        bmtMethodSlot newSlot;
        newSlot.Decl() = new (GetStackingAllocator())
            bmtRTMethod(bmtRTType::FindType(GetParentType(), pDeclMT), pDeclDesc);
        if (pDeclDesc == pImplDesc)
        {
            newSlot.Impl() = newSlot.Decl();
        }
        else
        {
            newSlot.Impl() = new (GetStackingAllocator())
                bmtRTMethod(bmtRTType::FindType(GetParentType(), pImplMT), pImplDesc);
        }

        if (!bmtParent->pSlotTable->AddMethodSlot(newSlot))
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
    }
}

//*******************************************************************************
void
MethodTableBuilder::CopyParentVtable()
{
    STANDARD_VM_CONTRACT;

    if (!HasParent())
    {
        return;
    }

    for (bmtParentInfo::Iterator it = bmtParent->IterateSlots();
         !it.AtEnd() && it.CurrentIndex() < GetParentMethodTable()->GetNumVirtuals();
         ++it)
     {
        if (!bmtVT->pSlotTable->AddMethodSlot(*it))
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
        ++bmtVT->cVirtualSlots;
        ++bmtVT->cTotalSlots;
     }
}

//*******************************************************************************
// Determine if this is the special SIMD type System.Numerics.Vector<T>, whose
// size is determined dynamically based on the hardware and the presence of JIT
// support.
// If so:
//   - Update the NumInstanceFieldBytes on the bmtFieldPlacement.
//   - Update the m_cbNativeSize and m_cbManagedSize if HasLayout() is true.
// Return a BOOL result to indicate whether the size has been updated.
//
BOOL MethodTableBuilder::CheckIfSIMDAndUpdateSize()
{
    STANDARD_VM_CONTRACT;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    if (!bmtProp->fIsIntrinsicType)
        return false;

    if (bmtFP->NumInstanceFieldBytes != 16)
        return false;

    LPCUTF8 className;
    LPCUTF8 nameSpace;
    if (FAILED(GetMDImport()->GetNameOfTypeDef(bmtInternal->pType->GetTypeDefToken(), &className, &nameSpace)))
        return false;

    if (strcmp(className, "Vector`1") != 0 || strcmp(nameSpace, "System.Numerics") != 0)
        return false;

    if (!TargetHasAVXSupport())
        return false;

    EEJitManager *jitMgr = ExecutionManager::GetEEJitManager();
    if (jitMgr->LoadJIT())
    {
        CORJIT_FLAGS cpuCompileFlags = jitMgr->GetCPUCompileFlags();
        unsigned intrinsicSIMDVectorLength = jitMgr->m_jit->getMaxIntrinsicSIMDVectorLength(cpuCompileFlags);
        if (intrinsicSIMDVectorLength != 0)
        {
            bmtFP->NumInstanceFieldBytes     = intrinsicSIMDVectorLength;
            if (HasLayout())
            {
                GetLayoutInfo()->m_cbManagedSize = intrinsicSIMDVectorLength;
            }
            return true;
        }
    }
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
    return false;
}

//*******************************************************************************
void
MethodTableBuilder::bmtInterfaceEntry::CreateSlotTable(
    StackingAllocator * pStackingAllocator)
{
    STANDARD_VM_CONTRACT;

    CONSISTENCY_CHECK(m_pImplTable == NULL);

    SLOT_INDEX cSlots = (SLOT_INDEX)GetInterfaceType()->GetMethodTable()->GetNumVirtuals();
    SLOT_INDEX cSlotsTotal = cSlots;

    if (GetInterfaceType()->GetMethodTable()->HasVirtualStaticMethods())
    {
        MethodTable::MethodIterator it(GetInterfaceType()->GetMethodTable());
        for (; it.IsValid(); it.Next())
        {
            MethodDesc *pDeclMD = it.GetDeclMethodDesc();
            if (pDeclMD->IsStatic() && pDeclMD->IsVirtual())
            {
                cSlotsTotal++;
            }
        }
    }

    bmtInterfaceSlotImpl * pST = new (pStackingAllocator) bmtInterfaceSlotImpl[cSlotsTotal];


    MethodTable::MethodIterator it(GetInterfaceType()->GetMethodTable());
    for (; it.IsValid(); it.Next())
    {
        MethodDesc *pDeclMD = it.GetDeclMethodDesc();
        if (!pDeclMD->IsVirtual())
        {
            continue;
        }

        bmtRTMethod * pCurMethod = new (pStackingAllocator)
            bmtRTMethod(GetInterfaceType(), it.GetDeclMethodDesc());

        if (pDeclMD->IsStatic())
        {
            pST[cSlots + m_cImplTableStatics++] = bmtInterfaceSlotImpl(pCurMethod, INVALID_SLOT_INDEX);
        }
        else
        {
            CONSISTENCY_CHECK(m_cImplTable == it.GetSlotNumber());
            pST[m_cImplTable++] = bmtInterfaceSlotImpl(pCurMethod, INVALID_SLOT_INDEX);
        }
    }

    m_pImplTable = pST;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif // _PREFAST_
//---------------------------------------------------------------------------------------
//
// Builds the method table, allocates MethodDesc, handles overloaded members, attempts to compress
// interface storage.  All dependent classes must already be resolved!
//
MethodTable *
MethodTableBuilder::BuildMethodTableThrowing(
    LoaderAllocator *          pAllocator,
    Module *                   pLoaderModule,
    Module *                   pModule,
    mdToken                    cl,
    BuildingInterfaceInfo_t *  pBuildingInterfaceList,
    const LayoutRawFieldInfo * pLayoutRawFieldInfos,
    MethodTable *              pParentMethodTable,
    const bmtGenericsInfo *    bmtGenericsInfo,
    SigPointer                 parentInst,
    WORD                       cBuildingInterfaceList)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(GetHalfBakedClass()));
        PRECONDITION(CheckPointer(bmtGenericsInfo));
    }
    CONTRACTL_END;

    pModule->EnsureLibraryLoaded();

    // The following structs, defined as private members of MethodTableBuilder, contain the necessary local
    // parameters needed for BuildMethodTable Look at the struct definitions for a detailed list of all
    // parameters available to BuildMethodTableThrowing.

    SetBMTData(
        pAllocator,
        new (GetStackingAllocator()) bmtErrorInfo(),
        new (GetStackingAllocator()) bmtProperties(),
        new (GetStackingAllocator()) bmtVtable(),
        new (GetStackingAllocator()) bmtParentInfo(),
        new (GetStackingAllocator()) bmtInterfaceInfo(),
        new (GetStackingAllocator()) bmtMetaDataInfo(),
        new (GetStackingAllocator()) bmtMethodInfo(),
        new (GetStackingAllocator()) bmtMethAndFieldDescs(),
        new (GetStackingAllocator()) bmtFieldPlacement(),
        new (GetStackingAllocator()) bmtInternalInfo(),
        new (GetStackingAllocator()) bmtGCSeriesInfo(),
        new (GetStackingAllocator()) bmtMethodImplInfo(),
        bmtGenericsInfo,
        new (GetStackingAllocator()) bmtEnumFieldInfo(pModule->GetMDImport()));

    //Initialize structs

    bmtError->resIDWhy = IDS_CLASSLOAD_GENERAL;          // Set the reason and the offending method def. If the method information
    bmtError->pThrowable = NULL;
    bmtError->pModule  = pModule;
    bmtError->cl       = cl;

    bmtInternal->pInternalImport = pModule->GetMDImport();
    bmtInternal->pModule = pModule;

    bmtInternal->pParentMT = pParentMethodTable;

    // Create the chain of bmtRTType for the parent types. This allows all imported
    // parent methods to be associated with their declaring types, and as such it is
    // easy to access the appropriate Substitution when comparing signatures.
    bmtRTType * pParent = NULL;
    if (pParentMethodTable != NULL)
    {
        Substitution * pParentSubst =
            new (GetStackingAllocator()) Substitution(pModule, parentInst, NULL);
        pParent = CreateTypeChain(pParentMethodTable, *pParentSubst);
    }

    // Now create the bmtMDType for the type being built.
    bmtInternal->pType = new (GetStackingAllocator())
        bmtMDType(pParent, pModule, cl, bmtGenericsInfo->typeContext);

    // If not NULL, it means there are some by-value fields, and this contains an entry for each inst

#ifdef _DEBUG
    // Set debug class name string for easier debugging.
    LPCUTF8 className;
    LPCUTF8 nameSpace;
    if (FAILED(GetMDImport()->GetNameOfTypeDef(bmtInternal->pType->GetTypeDefToken(), &className, &nameSpace)))
    {
        className = nameSpace = "Invalid TypeDef record";
    }

    {
        S_SIZE_T safeLen = S_SIZE_T(sizeof(char))*(S_SIZE_T(strlen(className)) + S_SIZE_T(strlen(nameSpace)) + S_SIZE_T(2));
        if(safeLen.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);

        size_t len = safeLen.Value();
        char *name = (char*) AllocateFromHighFrequencyHeap(safeLen);
        strcpy_s(name, len, nameSpace);
        if (strlen(nameSpace) > 0) {
            name[strlen(nameSpace)] = '.';
            name[strlen(nameSpace) + 1] = '\0';
        }
        strcat_s(name, len, className);

        GetHalfBakedClass()->SetDebugClassName(name);
    }

    if (g_pConfig->ShouldBreakOnClassBuild(className))
    {
        CONSISTENCY_CHECK_MSGF(false, ("BreakOnClassBuild: typename '%s' ", className));
        GetHalfBakedClass()->m_fDebuggingClass = TRUE;
    }

    LPCUTF8 pszDebugName,pszDebugNamespace;
    if (FAILED(pModule->GetMDImport()->GetNameOfTypeDef(bmtInternal->pType->GetTypeDefToken(), &pszDebugName, &pszDebugNamespace)))
    {
        pszDebugName = pszDebugNamespace = "Invalid TypeDef record";
    }

    StackSString debugName(SString::Utf8, pszDebugName);

    // If there is an instantiation, update the debug name to include instantiation type names.
    if (bmtGenerics->HasInstantiation())
    {
        StackSString debugName(SString::Utf8, GetDebugClassName());
        TypeString::AppendInst(debugName, bmtGenerics->GetInstantiation(), TypeString::FormatBasic);
        StackScratchBuffer buff;
        const char* pDebugNameUTF8 = debugName.GetUTF8(buff);
        S_SIZE_T safeLen = S_SIZE_T(strlen(pDebugNameUTF8)) + S_SIZE_T(1);
        if(safeLen.IsOverflow())
            COMPlusThrowHR(COR_E_OVERFLOW);

        size_t len = safeLen.Value();
        char *name = (char*) AllocateFromLowFrequencyHeap(safeLen);
        strcpy_s(name, len, pDebugNameUTF8);
        GetHalfBakedClass()->SetDebugClassName(name);
        pszDebugName = (LPCUTF8)name;
    }

    LOG((LF_CLASSLOADER, LL_INFO1000, "Loading class \"%s%s%S\" from module \"%ws\" in domain 0x%p %s\n",
        *pszDebugNamespace ? pszDebugNamespace : "",
        *pszDebugNamespace ? NAMESPACE_SEPARATOR_STR : "",
        debugName.GetUnicode(),
        pModule->GetDebugName(),
        pModule->GetDomain(),
        (pModule->IsSystem()) ? "System Domain" : ""
    ));
#endif // _DEBUG

    // If this is CoreLib, then don't perform some sanity checks on the layout
    bmtProp->fNoSanityChecks = pModule->IsSystem() ||
#ifdef FEATURE_READYTORUN
        // No sanity checks for ready-to-run compiled images if possible
        (pModule->IsReadyToRun() && pModule->GetReadyToRunInfo()->SkipTypeValidation()) ||
#endif
        // No sanity checks for real generic instantiations
        !bmtGenerics->IsTypicalTypeDefinition();

    // Interfaces have a parent class of Object, but we don't really want to inherit all of
    // Object's virtual methods, so pretend we don't have a parent class - at the bottom of this
    // function we reset the parent class
    if (IsInterface())
    {
        bmtInternal->pType->SetParentType(NULL);
        bmtInternal->pParentMT = NULL;
    }

    unsigned totalDeclaredFieldSize=0;

    // Check to see if the class is a valuetype; but we don't want to mark System.Enum
    // as a ValueType. To accomplish this, the check takes advantage of the fact
    // that System.ValueType and System.Enum are loaded one immediately after the
    // other in that order, and so if the parent MethodTable is System.ValueType and
    // the System.Enum MethodTable is unset, then we must be building System.Enum and
    // so we don't mark it as a ValueType.
    if(HasParent() &&
       ((g_pEnumClass != NULL && GetParentMethodTable() == g_pValueTypeClass) ||
        GetParentMethodTable() == g_pEnumClass))
    {
        bmtProp->fIsValueClass = true;

        HRESULT hr = GetCustomAttribute(bmtInternal->pType->GetTypeDefToken(),
                                        WellKnownAttribute::UnsafeValueType,
                                        NULL, NULL);
        IfFailThrow(hr);
        if (hr == S_OK)
        {
            SetUnsafeValueClass();
        }

        hr = GetCustomAttribute(bmtInternal->pType->GetTypeDefToken(),
            WellKnownAttribute::IsByRefLike,
            NULL, NULL);
        IfFailThrow(hr);
        if (hr == S_OK)
        {
            bmtFP->fIsByRefLikeType = true;
        }
    }

    // Check to see if the class is an enumeration. No fancy checks like the one immediately
    // above for value types are necessary here.
    if(HasParent() && GetParentMethodTable() == g_pEnumClass)
    {
        bmtProp->fIsEnum = true;

        // Ensure we don't have generic enums, or at least enums that have a
        // different number of type parameters from their enclosing class.
        // The goal is to ensure that the enum's values can't depend on the
        // type parameters in any way.  And we don't see any need for an
        // enum to have additional type parameters.
        if (bmtGenerics->GetNumGenericArgs() != 0)
        {
            // Nested enums can have generic type parameters from their enclosing class.
            // CLS rules require type parameters to be propagated to nested types.
            // Note that class G<T> { enum E { } } will produce "G`1+E<T>".
            // We want to disallow class G<T> { enum E<T, U> { } }
            // Perhaps the IL equivalent of class G<T> { enum E { } } should be legal.
            if (!IsNested())
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_ENUM_EXTRA_GENERIC_TYPE_PARAM);
            }

            mdTypeDef tdEnclosing = mdTypeDefNil;
            HRESULT hr = GetMDImport()->GetNestedClassProps(GetCl(), &tdEnclosing);
            if (FAILED(hr))
                ThrowHR(hr, BFA_UNABLE_TO_GET_NESTED_PROPS);

            HENUMInternalHolder   hEnumGenericPars(GetMDImport());
            if (FAILED(hEnumGenericPars.EnumInitNoThrow(mdtGenericParam, tdEnclosing)))
            {
                GetAssembly()->ThrowTypeLoadException(GetMDImport(), tdEnclosing, IDS_CLASSLOAD_BADFORMAT);
            }

            if (hEnumGenericPars.EnumGetCount() != bmtGenerics->GetNumGenericArgs())
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_ENUM_EXTRA_GENERIC_TYPE_PARAM);
            }
        }
    }

    // If this type is marked by [Intrinsic] attribute, it may be specially treated by the runtime/compiler
    // SIMD types have [Intrinsic] attribute, for example
    //
    // We check this here fairly early to ensure other downstream checks on these types can be slightly more efficient.
    if (GetModule()->IsSystem())
    {
        HRESULT hr = GetCustomAttribute(bmtInternal->pType->GetTypeDefToken(),
            WellKnownAttribute::Intrinsic,
            NULL,
            NULL);

        if (hr == S_OK)
        {
            bmtProp->fIsIntrinsicType = true;
        }
    }

#if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM64)
    if (bmtProp->fIsIntrinsicType && !bmtGenerics->HasInstantiation())
    {
        LPCUTF8 className;
        LPCUTF8 nameSpace;
        HRESULT hr = GetMDImport()->GetNameOfTypeDef(bmtInternal->pType->GetTypeDefToken(), &className, &nameSpace);

        if (bmtInternal->pType->IsNested())
        {
            IfFailThrow(GetMDImport()->GetNameOfTypeDef(bmtInternal->pType->GetEnclosingTypeToken(), NULL, &nameSpace));
        }

#if defined(TARGET_ARM64)
        // All the funtions in System.Runtime.Intrinsics.Arm are hardware intrinsics.
        if (hr == S_OK && strcmp(nameSpace, "System.Runtime.Intrinsics.Arm") == 0)
#else
        // All the funtions in System.Runtime.Intrinsics.X86 are hardware intrinsics.
        if (hr == S_OK && (strcmp(nameSpace, "System.Runtime.Intrinsics.X86") == 0))
#endif
        {
            bmtProp->fIsHardwareIntrinsic = true;
        }
    }
#endif

    // Com Import classes are special. These types must derive from System.Object,
    // and we then substitute the parent with System._ComObject.
    if (IsComImport() && !IsEnum() && !IsInterface() && !IsValueClass() && !IsDelegate())
    {
#ifdef FEATURE_COMINTEROP
        // ComImport classes must extend from Object
        MethodTable* pMTParent = GetParentMethodTable();
        if ((pMTParent == NULL) || (pMTParent != g_pObjectClass))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_CANTEXTEND);
        }

        if (HasLayout())
        {
            // ComImport classes cannot have layout information.
            BuildMethodTableThrowException(IDS_CLASSLOAD_COMIMPCANNOTHAVELAYOUT);
        }

        if (g_pBaseCOMObject != NULL)
        {
            // We could have had COM interop classes derive from System._ComObject,
            // but instead we have them derive from System.Object, have them set the
            // ComImport bit in the type attributes, and then we swap out the parent
            // type under the covers.
            bmtInternal->pType->SetParentType(CreateTypeChain(g_pBaseCOMObject, Substitution()));
            bmtInternal->pParentMT = g_pBaseCOMObject;
        }
#endif
        // if the current class is imported
        bmtProp->fIsComObjectType = true;
    }

#ifdef FEATURE_COMINTEROP
    // Check for special COM interop types.
    CheckForSpecialTypes();

    CheckForTypeEquivalence(cBuildingInterfaceList, pBuildingInterfaceList);

    if (HasParent())
    {   // Types that inherit from com object types are themselves com object types.
        if (GetParentMethodTable()->IsComObjectType())
        {
            // if the parent class is of ComObjectType
            // so is the child
            bmtProp->fIsComObjectType = true;
        }

#ifdef FEATURE_TYPEEQUIVALENCE
        // If your parent is type equivalent then so are you
        if (GetParentMethodTable()->HasTypeEquivalence())
        {
            bmtProp->fHasTypeEquivalence = true;
        }
#endif
    }

#endif // FEATURE_COMINTEROP

    if (!HasParent() && !IsInterface())
    {
        if(g_pObjectClass != NULL)
        {
            if(!IsGlobalClass())
            {
                // Non object derived types that are not the global class are prohibited by spec
                BuildMethodTableThrowException(IDS_CLASSLOAD_PARENTNULL);
            }
        }
    }

    // NOTE: This appears to be the earliest point during class loading that other classes MUST be loaded
    // resolve unresolved interfaces, determine an upper bound on the size of the interface map,
    // and determine the size of the largest interface (in # slots)
    ResolveInterfaces(cBuildingInterfaceList, pBuildingInterfaceList);

    // Enumerate this class's methodImpls
    EnumerateMethodImpls();

    // Enumerate this class's methods and fields
    EnumerateClassMethods();
    ValidateMethods();

    EnumerateClassFields();

    // Import the slots of the parent for use in placing this type's methods.
    ImportParentMethods();

    // This will allocate the working versions of the VTable and NonVTable in bmtVT
    AllocateWorkingSlotTables();

    // Allocate a MethodDesc* for each method (needed later when doing interfaces), and a FieldDesc* for each field
    AllocateFieldDescs();

    // Copy the parent's vtable into the current type's vtable
    CopyParentVtable();

    bmtVT->pDispatchMapBuilder = new (GetStackingAllocator()) DispatchMapBuilder(GetStackingAllocator());

    // Determine vtable placement for each member in this class
    PlaceVirtualMethods();
    PlaceNonVirtualMethods();

    // Allocate MethodDescs (expects methods placed methods)
    AllocAndInitMethodDescs();

    if (IsInterface())
    {
        //
        // We need to process/place method impls for default interface method overrides.
        // We won't build dispatch map for interfaces, though.
        //
        ProcessMethodImpls();
        PlaceMethodImpls();
    }
    else
    {
        //
        // If we are a class, then there may be some unplaced vtable methods (which are by definition
        // interface methods, otherwise they'd already have been placed).  Place as many unplaced methods
        // as possible, in the order preferred by interfaces.  However, do not allow any duplicates - once
        // a method has been placed, it cannot be placed again - if we are unable to neatly place an interface,
        // create duplicate slots for it starting at dwCurrentDuplicateVtableSlot.  Fill out the interface
        // map for all interfaces as they are placed.
        //
        // If we are an interface, then all methods are already placed.  Fill out the interface map for
        // interfaces as they are placed.
        //
        ComputeInterfaceMapEquivalenceSet();

        PlaceInterfaceMethods();

        ProcessMethodImpls();
        ProcessInexactMethodImpls();
        PlaceMethodImpls();

        if (!bmtProp->fNoSanityChecks)
        {
            // Now that interface method implementation have been fully resolved,
            // we need to make sure that type constraints are also met.
            ValidateInterfaceMethodConstraints();
        }
    }

    // Verify that we have not overflowed the number of slots.
    if (!FitsInU2((UINT64)bmtVT->pSlotTable->GetSlotCount()))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
    }

    // ensure we didn't overflow the temporary vtable
    _ASSERTE(bmtVT->pSlotTable->GetSlotCount() <= bmtVT->dwMaxVtableSize);

    // Allocate and initialize the dictionary for the type. This will be filled out later
    // with the final values.
    AllocAndInitDictionary();

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // Fields
    //

    // We decide here if we need a dynamic entry for our statics. We need it here because
    // the offsets of our fields will depend on this. For the dynamic case (which requires
    // an extra indirection (indirect depending of methodtable) we'll allocate the slot
    // in setupmethodtable
    if (((pAllocator->IsCollectible() ||  pModule->IsReflection() || bmtGenerics->HasInstantiation() || !pModule->IsStaticStoragePrepared(cl)) &&
        (bmtVT->GetClassCtorSlotIndex() != INVALID_SLOT_INDEX || bmtEnumFields->dwNumStaticFields !=0))
#ifdef EnC_SUPPORTED
        // Classes in modules that have been edited (would do on class level if there were a
        // way to tell if the class had been edited) also have dynamic statics as the number
        // of statics might have changed, so can't use the static module-wide storage
        || (pModule->IsEditAndContinueEnabled() &&
                ((EditAndContinueModule*)pModule)->GetApplyChangesCount() > CorDB_DEFAULT_ENC_FUNCTION_VERSION)
#endif // EnC_SUPPORTED
        )
    {
        // We will need a dynamic id
        bmtProp->fDynamicStatics = true;

        if (bmtGenerics->HasInstantiation())
        {
            bmtProp->fGenericsStatics = true;
        }
    }

    // If not NULL, it means there are some by-value fields, and this contains an entry for each instance or static field,
    // which is NULL if not a by value field, and points to the EEClass of the field if a by value field.  Instance fields
    // come first, statics come second.
    MethodTable ** pByValueClassCache = NULL;

    // Go thru all fields and initialize their FieldDescs.
    InitializeFieldDescs(GetApproxFieldDescListRaw(), pLayoutRawFieldInfos, bmtInternal, bmtGenerics,
        bmtMetaData, bmtEnumFields, bmtError,
        &pByValueClassCache, bmtMFDescs, bmtFP,
        &totalDeclaredFieldSize);

    // Place regular static fields
    PlaceRegularStaticFields();

    // Place thread static fields
    PlaceThreadStaticFields();

    LOG((LF_CODESHARING,
            LL_INFO10000,
            "Placing %d statics (%d handles) for class %s.\n",
            GetNumStaticFields(), GetNumHandleRegularStatics() + GetNumHandleThreadStatics(),
            pszDebugName));

    if (IsBlittable() || IsManagedSequential())
    {
        bmtFP->NumGCPointerSeries = 0;
        bmtFP->NumInstanceGCPointerFields = 0;

        _ASSERTE(HasLayout());

        bmtFP->NumInstanceFieldBytes = GetLayoutInfo()->m_cbManagedSize;

        // For simple Blittable types we still need to check if they have any overlapping
        // fields and call the method SetHasOverLayedFields() when they are detected.
        //
        if (HasExplicitFieldOffsetLayout())
        {
            _ASSERTE(!bmtGenerics->fContainsGenericVariables);   // A simple Blittable type can't ever be an open generic type.
            HandleExplicitLayout(pByValueClassCache);
        }
    }
    else
    {
        _ASSERTE(!IsBlittable());
        // HandleExplicitLayout fails for the GenericTypeDefinition when
        // it will succeed for some particular instantiations.
        // Thus we only do explicit layout for real instantiations, e.g. C<int>, not
        // the open types such as the GenericTypeDefinition C<!0> or any
        // of the "fake" types involving generic type variables which are
        // used for reflection and verification, e.g. C<List<!0>>.
        //
        if (!bmtGenerics->fContainsGenericVariables && HasExplicitFieldOffsetLayout())
        {
            HandleExplicitLayout(pByValueClassCache);
        }
        else
        {
            // Place instance fields
            PlaceInstanceFields(pByValueClassCache);
        }
    }

    if (CheckIfSIMDAndUpdateSize())
    {
        totalDeclaredFieldSize = bmtFP->NumInstanceFieldBytes;
    }

    // We enforce that all value classes have non-zero size
    if (IsValueClass() && bmtFP->NumInstanceFieldBytes == 0)
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_ZEROSIZE);
    }

    if (bmtFP->fHasSelfReferencingStaticValueTypeField_WithRVA)
    {   // Verify self-referencing statics with RVA (now when the ValueType size is known)
        VerifySelfReferencingStaticValueTypeFields_WithRVA(pByValueClassCache);
    }


    // Now setup the method table

    SetupMethodTable2(pLoaderModule);

    MethodTable * pMT = GetHalfBakedMethodTable();

#ifdef FEATURE_64BIT_ALIGNMENT
    if (GetHalfBakedClass()->IsAlign8Candidate())
        pMT->SetRequiresAlign8();
#endif

    if (bmtGenerics->pVarianceInfo != NULL)
    {
        pMT->SetHasVariance();
    }

    if (bmtFP->NumRegularStaticGCBoxedFields != 0)
    {
        pMT->SetHasBoxedRegularStatics();
    }

    if (bmtFP->fIsByRefLikeType)
    {
        pMT->SetIsByRefLike();
    }

    if (IsValueClass())
    {
        if (bmtFP->NumInstanceFieldBytes != totalDeclaredFieldSize || HasOverLayedField())
            GetHalfBakedClass()->SetIsNotTightlyPacked();

#ifdef FEATURE_HFA
        GetHalfBakedClass()->CheckForHFA(pByValueClassCache);
#endif
#ifdef UNIX_AMD64_ABI
#ifdef FEATURE_HFA
#error "Can't have FEATURE_HFA and UNIX_AMD64_ABI defined at the same time."
#endif // FEATURE_HFA
        SystemVAmd64CheckForPassStructInRegister();
#endif // UNIX_AMD64_ABI
    }

#ifdef _DEBUG
    pMT->SetDebugClassName(GetDebugClassName());
#endif

#ifdef FEATURE_COMINTEROP
    if (IsInterface())
    {
        GetCoClassAttribInfo();
    }
#endif // FEATURE_COMINTEROP

    if (HasExplicitFieldOffsetLayout())
        // Perform relevant GC calculations for tdexplicit
        HandleGCForExplicitLayout();
    else
        // Perform relevant GC calculations for value classes
        HandleGCForValueClasses(pByValueClassCache);

        // GC reqires the series to be sorted.
        // TODO: fix it so that we emit them in the correct order in the first place.
    if (pMT->ContainsPointers())
    {
        CGCDesc* gcDesc = CGCDesc::GetCGCDescFromMT(pMT);
        qsort(gcDesc->GetLowestSeries(), (int)gcDesc->GetNumSeries(), sizeof(CGCDescSeries), compareCGCDescSeries);
    }

    SetFinalizationSemantics();

    // Allocate dynamic slot if necessary
    if (bmtProp->fDynamicStatics)
    {
        if (bmtProp->fGenericsStatics)
        {
            FieldDesc* pStaticFieldDescs = NULL;

            if (bmtEnumFields->dwNumStaticFields != 0)
            {
                pStaticFieldDescs = pMT->GetApproxFieldDescListRaw() + bmtEnumFields->dwNumInstanceFields;
            }

            pMT->SetupGenericsStaticsInfo(pStaticFieldDescs);
        }
        else
        {
            // Get an id for the dynamic class. We store it in the class because
            // no class that is persisted in ngen should have it (ie, if the class is ngened
            // The id is stored in an optional field so we need to ensure an optional field descriptor has
            // been allocated for this EEClass instance.
            EnsureOptionalFieldsAreAllocated(GetHalfBakedClass(), m_pAllocMemTracker, pAllocator->GetLowFrequencyHeap());
            SetModuleDynamicID(GetModule()->AllocateDynamicEntry(pMT));
        }
    }

    //
    // if there are context or thread static set the info in the method table optional members
    //

    // Check for the RemotingProxy Attribute
    // structs with GC pointers MUST be pointer sized aligned because the GC assumes it
    if (IsValueClass() && pMT->ContainsPointers() && (bmtFP->NumInstanceFieldBytes % TARGET_POINTER_SIZE != 0))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
    }

    if (IsInterface())
    {
        // Reset parent class
        pMT->SetParentMethodTable (g_pObjectClass);
    }

#ifdef _DEBUG
    // Reset the debug method names for BoxedEntryPointStubs
    // so they reflect the very best debug information for the methods
    {
        DeclaredMethodIterator methIt(*this);
        while (methIt.Next())
        {
            if (methIt->GetUnboxedMethodDesc() != NULL)
            {
                {
                    MethodDesc *pMD = methIt->GetUnboxedMethodDesc();
                    StackSString name;
                    TypeString::AppendMethodDebug(name, pMD);
                    StackScratchBuffer buff;
                    const char* pDebugNameUTF8 = name.GetUTF8(buff);
                    S_SIZE_T safeLen = S_SIZE_T(strlen(pDebugNameUTF8)) + S_SIZE_T(1);
                    if(safeLen.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
                    size_t len = safeLen.Value();
                    pMD->m_pszDebugMethodName = (char*) AllocateFromLowFrequencyHeap(safeLen);
                    _ASSERTE(pMD->m_pszDebugMethodName);
                    strcpy_s((char *) pMD->m_pszDebugMethodName, len, pDebugNameUTF8);
                }

                {
                    MethodDesc *pMD = methIt->GetMethodDesc();

                    StackSString name;
                    TypeString::AppendMethodDebug(name, pMD);
                    StackScratchBuffer buff;
                    const char* pDebugNameUTF8 = name.GetUTF8(buff);
                    S_SIZE_T safeLen = S_SIZE_T(strlen(pDebugNameUTF8))+S_SIZE_T(1);
                    if(safeLen.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
                    size_t len = safeLen.Value();
                    pMD->m_pszDebugMethodName = (char*) AllocateFromLowFrequencyHeap(safeLen);
                    _ASSERTE(pMD->m_pszDebugMethodName);
                    strcpy_s((char *) pMD->m_pszDebugMethodName, len, pDebugNameUTF8);
                }
            }
        }
    }
#endif // _DEBUG


    //If this is a value type, then propagate the UnsafeValueTypeAttribute from
    //its instance members to this type.
    if (IsValueClass() && !IsUnsafeValueClass())
    {
        ApproxFieldDescIterator fields(GetHalfBakedMethodTable(),
                                       ApproxFieldDescIterator::INSTANCE_FIELDS );
        FieldDesc * current;
        while (NULL != (current = fields.Next()))
        {
            CONSISTENCY_CHECK(!current->IsStatic());
            if (current->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
            {
                TypeHandle th = current->LookupApproxFieldTypeHandle();
                CONSISTENCY_CHECK(!th.IsNull());
                if (th.AsMethodTable()->GetClass()->IsUnsafeValueClass())
                {
                    SetUnsafeValueClass();
                    break;
                }
            }
        }
    }

    if (!IsValueClass())
    {
#ifdef FEATURE_ICASTABLE
        if (g_pICastableInterface != NULL && pMT->CanCastToInterface(g_pICastableInterface))
        {
            pMT->SetICastable();
        }
#endif // FEATURE_ICASTABLE

        if (g_pIDynamicInterfaceCastableInterface != NULL && pMT->CanCastToInterface(g_pIDynamicInterfaceCastableInterface))
        {
            pMT->SetIDynamicInterfaceCastable();
        }
    }

#ifdef FEATURE_OBJCMARSHAL
    // Check if this type has a finalizer and then if it is a referenced tracked type.
    if (pMT->HasFinalizer() && !IsValueClass() && !IsInterface() && !IsDelegate())
    {
        BOOL isTrackedReference = FALSE;
        if (HasParent())
        {
            MethodTable * pParentClass = GetParentMethodTable();
            PREFIX_ASSUME(pParentClass != NULL);
            isTrackedReference = pParentClass->IsTrackedReferenceWithFinalizer();
        }

        if (!isTrackedReference)
        {
            HRESULT hr = GetCustomAttribute(bmtInternal->pType->GetTypeDefToken(),
                WellKnownAttribute::ObjectiveCTrackedTypeAttribute,
                NULL,
                NULL);

            isTrackedReference = hr == S_OK ? TRUE : FALSE;
        }

        if (isTrackedReference)
            pMT->SetIsTrackedReferenceWithFinalizer();
    }
#endif // FEATURE_OBJCMARSHAL

    // Grow the typedef ridmap in advance as we can't afford to
    // fail once we set the resolve bit
    pModule->EnsureTypeDefCanBeStored(bmtInternal->pType->GetTypeDefToken());

    // Grow the tables in advance so that RID map filling cannot fail
    // once we're past the commit point.
    EnsureRIDMapsCanBeFilled();

#ifdef _DEBUG
    if (g_pConfig->ShouldDumpOnClassLoad(pszDebugName))
    {
        LOG((LF_ALWAYS, LL_ALWAYS, "Method table summary for '%s':\n", pszDebugName));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of static fields: %d\n", bmtEnumFields->dwNumStaticFields));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of instance fields: %d\n", bmtEnumFields->dwNumInstanceFields));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of static obj ref fields: %d\n", bmtEnumFields->dwNumStaticObjRefFields));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of static boxed fields: %d\n", bmtEnumFields->dwNumStaticBoxedFields));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of declared fields: %d\n", NumDeclaredFields()));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of declared methods: %d\n", NumDeclaredMethods()));
        LOG((LF_ALWAYS, LL_ALWAYS, "Number of declared non-abstract methods: %d\n", bmtMethod->dwNumDeclaredNonAbstractMethods));

        BOOL debugging = IsDebuggerPresent();
        pMT->Debug_DumpInterfaceMap("Approximate");
        pMT->DebugDumpVtable(pszDebugName, debugging);
        pMT->DebugDumpFieldLayout(pszDebugName, debugging);
        pMT->DebugDumpGCDesc(pszDebugName, debugging);
        pMT->Debug_DumpDispatchMap();
    }
#endif //_DEBUG

    STRESS_LOG3(LF_CLASSLOADER,  LL_INFO1000, "MethodTableBuilder: finished method table for module %p token %x = %pT \n",
        pModule,
        GetCl(),
        GetHalfBakedMethodTable());

    return GetHalfBakedMethodTable();
} // MethodTableBuilder::BuildMethodTableThrowing
#ifdef _PREFAST_
#pragma warning(pop)
#endif


//---------------------------------------------------------------------------------------
//
// Resolve unresolved interfaces, determine an upper bound on the size of the interface map.
//
VOID
MethodTableBuilder::ResolveInterfaces(
    WORD                      cBuildingInterfaceList,
    BuildingInterfaceInfo_t * pBuildingInterfaceList)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtAllocator));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtParent));
    }
    CONTRACTL_END;

    // resolve unresolved interfaces and determine the size of the largest interface (in # slots)


    LoadApproxInterfaceMap();

    // Inherit parental slot counts
    //@TODO: This doesn't belong here.
    if (HasParent())
    {
        MethodTable * pParentClass = GetParentMethodTable();
        PREFIX_ASSUME(pParentClass != NULL);

        bmtParent->NumParentPointerSeries  = pParentClass->ContainsPointers() ?
            (DWORD)CGCDesc::GetCGCDescFromMT(pParentClass)->GetNumSeries() : 0;

        if (pParentClass->HasFieldsWhichMustBeInited())
        {
            SetHasFieldsWhichMustBeInited();
        }
#ifdef FEATURE_READYTORUN
        if (!(IsValueClass() || (pParentClass == g_pObjectClass)))
        {
            CheckLayoutDependsOnOtherModules(pParentClass);
        }
#endif
    }
    else
    {
        bmtParent->NumParentPointerSeries  = 0;
    }
} // MethodTableBuilder::ResolveInterfaces

//*******************************************************************************
/* static */
int __cdecl MethodTableBuilder::bmtMetaDataInfo::MethodImplTokenPair::Compare(
        const void *elem1,
        const void *elem2)
{
    STATIC_CONTRACT_LEAF;
    MethodImplTokenPair *e1 = (MethodImplTokenPair *)elem1;
    MethodImplTokenPair *e2 = (MethodImplTokenPair *)elem2;
    if (e1->methodBody < e2->methodBody) return -1;
    else if (e1->methodBody > e2->methodBody) return 1;
    else if (e1->methodDecl < e2->methodDecl) return -1;
    else if (e1->methodDecl > e2->methodDecl) return 1;
    else return 0;
}

//*******************************************************************************
/* static */
BOOL MethodTableBuilder::bmtMetaDataInfo::MethodImplTokenPair::Equal(
        const MethodImplTokenPair *elem1,
        const MethodImplTokenPair *elem2)
{
    STATIC_CONTRACT_LEAF;
    return ((elem1->methodBody == elem2->methodBody) &&
            (elem1->methodDecl == elem2->methodDecl));
}

//*******************************************************************************
BOOL MethodTableBuilder::IsEligibleForCovariantReturns(mdToken methodDeclToken)
{
    STANDARD_VM_CONTRACT;

    //
    // Note on covariant return types: right now we only support covariant returns for MethodImpls on
    // classes, where the MethodDecl is also on a class. Interface methods are not supported.
    // We will also allow covariant return types if both the MethodImpl and MethodDecl are not on the same type.
    //

    HRESULT hr = S_OK;
    IMDInternalImport* pMDInternalImport = GetMDImport();

    // First, check if the type with the MethodImpl is a class.
    if (IsValueClass() || IsInterface())
        return FALSE;

    mdToken tkParent;
    hr = pMDInternalImport->GetParentToken(methodDeclToken, &tkParent);
    if (FAILED(hr))
        BuildMethodTableThrowException(hr, *bmtError);

    // Second, check that the type with the MethodImpl is not the same as the type with the MethodDecl
    if (GetCl() == tkParent)
        return FALSE;

    // Finally, check that the type with the MethodDecl is not an interface. To do so, we need to compute the TypeDef
    // token of the type with the MethodDecl, as well as its module, in order to use the metadata to check if the type
    // is an interface.
    mdToken declTypeDefToken = mdTokenNil;
    Module* pDeclModule = GetModule();
    if (TypeFromToken(tkParent) == mdtTypeRef || TypeFromToken(tkParent) == mdtTypeDef)
    {
        if (!ClassLoader::ResolveTokenToTypeDefThrowing(GetModule(), tkParent, &pDeclModule, &declTypeDefToken))
            return FALSE;
    }
    else if (TypeFromToken(tkParent) == mdtTypeSpec)
    {
        ULONG cbTypeSig;
        PCCOR_SIGNATURE pTypeSig;
        hr = pMDInternalImport->GetSigFromToken(tkParent, &cbTypeSig, &pTypeSig);
        if (FAILED(hr))
            BuildMethodTableThrowException(hr, *bmtError);

        SigParser parser(pTypeSig, cbTypeSig);

        CorElementType elementType;
        IfFailThrow(parser.GetElemType(&elementType));

        if (elementType == ELEMENT_TYPE_GENERICINST)
        {
            IfFailThrow(parser.GetElemType(&elementType));
        }

        if (elementType == ELEMENT_TYPE_CLASS)
        {
            mdToken declTypeDefOrRefToken;
            IfFailThrow(parser.GetToken(&declTypeDefOrRefToken));
            if (!ClassLoader::ResolveTokenToTypeDefThrowing(GetModule(), declTypeDefOrRefToken, &pDeclModule, &declTypeDefToken))
                return FALSE;
        }
    }

    if (declTypeDefToken == mdTokenNil)
        return FALSE;

    // Now that we have computed the TypeDef token and the module, check its attributes to verify it is not an interface.

    DWORD attr;
    hr = pDeclModule->GetMDImport()->GetTypeDefProps(declTypeDefToken, &attr, NULL);
    if (FAILED(hr))
        BuildMethodTableThrowException(hr, *bmtError);

    return !IsTdInterface(attr);
}

//*******************************************************************************
VOID
MethodTableBuilder::EnumerateMethodImpls()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    IMDInternalImport * pMDInternalImport = GetMDImport();
    DWORD rid, maxRidMD, maxRidMR;
    HENUMInternalMethodImplHolder hEnumMethodImpl(pMDInternalImport);
    hr = hEnumMethodImpl.EnumMethodImplInitNoThrow(GetCl());

    if (FAILED(hr))
    {
        BuildMethodTableThrowException(hr, *bmtError);
    }

    // This gets the count out of the metadata interface.
    bmtMethod->dwNumberMethodImpls = hEnumMethodImpl.EnumMethodImplGetCount();
    bmtMethod->dwNumberInexactMethodImplCandidates = 0;

    // This is the first pass. In this we will simply enumerate the token pairs and fill in
    // the data structures. In addition, we'll sort the list and eliminate duplicates.
    if (bmtMethod->dwNumberMethodImpls > 0)
    {
        //
        // Allocate the structures to keep track of the token pairs
        //
        bmtMetaData->rgMethodImplTokens = new (GetStackingAllocator())
            bmtMetaDataInfo::MethodImplTokenPair[bmtMethod->dwNumberMethodImpls];

        // Iterate through each MethodImpl declared on this class
        for (DWORD i = 0; i < bmtMethod->dwNumberMethodImpls; i++)
        {
            hr = hEnumMethodImpl.EnumMethodImplNext(
                &bmtMetaData->rgMethodImplTokens[i].methodBody,
                &bmtMetaData->rgMethodImplTokens[i].methodDecl);
            bmtMetaData->rgMethodImplTokens[i].fConsiderDuringInexactMethodImplProcessing = false;
            bmtMetaData->rgMethodImplTokens[i].fThrowIfUnmatchedDuringInexactMethodImplProcessing = false;
            bmtMetaData->rgMethodImplTokens[i].interfaceEquivalenceSet = 0;
            bmtMetaData->rgMethodImplTokens[i].fRequiresCovariantReturnTypeChecking = false;

            if (FAILED(hr))
            {
                BuildMethodTableThrowException(hr, *bmtError);
            }
            // Grab the next set of body/decl tokens
            if (hr == S_FALSE)
            {
                // In the odd case that the enumerator fails before we've reached the total reported
                // entries, let's reset the count and just break out. (Should we throw?)
                bmtMethod->dwNumberMethodImpls = i;
                break;
            }
        }

        // No need to do any sorting or duplicate elimination if there's not two or more methodImpls
        if (bmtMethod->dwNumberMethodImpls > 1)
        {
            // Now sort
            qsort(bmtMetaData->rgMethodImplTokens,
                  bmtMethod->dwNumberMethodImpls,
                  sizeof(bmtMetaDataInfo::MethodImplTokenPair),
                  &bmtMetaDataInfo::MethodImplTokenPair::Compare);

            // Now eliminate duplicates
            for (DWORD i = 0; i < bmtMethod->dwNumberMethodImpls - 1; i++)
            {
                CONSISTENCY_CHECK((i + 1) < bmtMethod->dwNumberMethodImpls);

                bmtMetaDataInfo::MethodImplTokenPair *e1 = &bmtMetaData->rgMethodImplTokens[i];
                bmtMetaDataInfo::MethodImplTokenPair *e2 = &bmtMetaData->rgMethodImplTokens[i + 1];

                // If the pair are equal, eliminate the first one, and reduce the total count by one.
                if (bmtMetaDataInfo::MethodImplTokenPair::Equal(e1, e2))
                {
                    DWORD dwCopyNum = bmtMethod->dwNumberMethodImpls - (i + 1);
                    memcpy(e1, e2, dwCopyNum * sizeof(bmtMetaDataInfo::MethodImplTokenPair));
                    bmtMethod->dwNumberMethodImpls--;
                    CONSISTENCY_CHECK(bmtMethod->dwNumberMethodImpls > 0);
                }
            }
        }
    }

    if (bmtMethod->dwNumberMethodImpls != 0)
    {
        //
        // Allocate the structures to keep track of the impl matches
        //
        bmtMetaData->pMethodDeclSubsts = new (GetStackingAllocator())
            Substitution[bmtMethod->dwNumberMethodImpls];

        // These are used for verification
        maxRidMD = pMDInternalImport->GetCountWithTokenKind(mdtMethodDef);
        maxRidMR = pMDInternalImport->GetCountWithTokenKind(mdtMemberRef);

        // Iterate through each MethodImpl declared on this class
        for (DWORD i = 0; i < bmtMethod->dwNumberMethodImpls; i++)
        {
            PCCOR_SIGNATURE pSigDecl = NULL;
            PCCOR_SIGNATURE pSigBody = NULL;
            ULONG           cbSigDecl;
            ULONG           cbSigBody;
            mdToken tkParent;

            mdToken theBody, theDecl;
            Substitution theDeclSubst(GetModule(), SigPointer(), NULL); // this can get updated later below.

            theBody = bmtMetaData->rgMethodImplTokens[i].methodBody;
            theDecl = bmtMetaData->rgMethodImplTokens[i].methodDecl;

            // IMPLEMENTATION LIMITATION: currently, we require that the body of a methodImpl
            // belong to the current type. This is because we need to allocate a different
            // type of MethodDesc for bodies that are part of methodImpls.
            if (TypeFromToken(theBody) != mdtMethodDef)
            {
                hr = FindMethodDeclarationForMethodImpl(
                    theBody,
                    &theBody,
                    TRUE);
                if (FAILED(hr))
                {
                    BuildMethodTableThrowException(hr, IDS_CLASSLOAD_MI_ILLEGAL_BODY, mdMethodDefNil);
                }

                // Make sure to update the stored token with the resolved token.
                bmtMetaData->rgMethodImplTokens[i].methodBody = theBody;
            }

            if (TypeFromToken(theBody) != mdtMethodDef)
            {
                BuildMethodTableThrowException(BFA_METHODDECL_NOT_A_METHODDEF);
            }
            CONSISTENCY_CHECK(theBody == bmtMetaData->rgMethodImplTokens[i].methodBody);

            //
            // Now that the tokens of Decl and Body are obtained, do the MD validation
            //

            rid = RidFromToken(theDecl);

            // Perform initial rudimentary validation of the token. Full token verification
            // will be done in TestMethodImpl when placing the methodImpls.
            if (TypeFromToken(theDecl) == mdtMethodDef)
            {
                // Decl must be valid token
                if ((rid == 0) || (rid > maxRidMD))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_ILLEGAL_TOKEN_DECL);
                }
                // Get signature and length
                if (FAILED(pMDInternalImport->GetSigOfMethodDef(theDecl, &cbSigDecl, &pSigDecl)))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                }
            }

            // The token is not a MethodDef (likely a MemberRef)
            else
            {
                // Decl must be valid token
                if ((TypeFromToken(theDecl) != mdtMemberRef) || (rid == 0) || (rid > maxRidMR))
                {
                    bmtError->resIDWhy = IDS_CLASSLOAD_MI_ILLEGAL_TOKEN_DECL;
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_ILLEGAL_TOKEN_DECL);
                }

                // Get signature and length
                LPCSTR szDeclName;
                if (FAILED(pMDInternalImport->GetNameAndSigOfMemberRef(theDecl, &pSigDecl, &cbSigDecl, &szDeclName)))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                }

                // Get parent
                hr = pMDInternalImport->GetParentToken(theDecl,&tkParent);
                if (FAILED(hr))
                    BuildMethodTableThrowException(hr, *bmtError);

                theDeclSubst = Substitution(tkParent, GetModule(), NULL);
            }

            // Perform initial rudimentary validation of the token. Full token verification
            // will be done in TestMethodImpl when placing the methodImpls.
            {
                // Body must be valid token
                rid = RidFromToken(theBody);
                if ((rid == 0)||(rid > maxRidMD))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_ILLEGAL_TOKEN_BODY);
                }
                // Body's parent must be this class
                hr = pMDInternalImport->GetParentToken(theBody,&tkParent);
                if (FAILED(hr))
                    BuildMethodTableThrowException(hr, *bmtError);
                if(tkParent != GetCl())
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_ILLEGAL_BODY);
                }
            }
            // Decl's and Body's signatures must match
            if(pSigDecl && cbSigDecl)
            {
                if (FAILED(pMDInternalImport->GetSigOfMethodDef(theBody, &cbSigBody, &pSigBody)) ||
                    (pSigBody == NULL) ||
                    (cbSigBody == 0))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MISSING_SIG_BODY);
                }

                // Can't use memcmp because there may be two AssemblyRefs
                // in this scope, pointing to the same assembly, etc.).
                BOOL compatibleSignatures = MetaSig::CompareMethodSigs(pSigDecl, cbSigDecl, GetModule(), &theDeclSubst, pSigBody, cbSigBody, GetModule(), NULL, FALSE);

                if (!compatibleSignatures && IsEligibleForCovariantReturns(theDecl))
                {
                    if (MetaSig::CompareMethodSigs(pSigDecl, cbSigDecl, GetModule(), &theDeclSubst, pSigBody, cbSigBody, GetModule(), NULL, TRUE))
                    {
                        // Signatures matched, except for the return type. Flag that MethodImpl to check the return type at a later
                        // stage for compatibility, and treat it as compatible for now.
                        // For compatibility rules, see ECMA I.8.7.1. We will use the MethodTable::CanCastTo() at a later stage to validate
                        // compatibilities of the return types according to these rules.

                        compatibleSignatures = TRUE;
                        bmtMetaData->rgMethodImplTokens[i].fRequiresCovariantReturnTypeChecking = true;
                        bmtMetaData->fHasCovariantOverride = true;
                    }
                }

                if (!compatibleSignatures)
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_BODY_DECL_MISMATCH);
                }
            }
            else
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MISSING_SIG_DECL);
            }

            bmtMetaData->pMethodDeclSubsts[i] = theDeclSubst;
        }
    }
} // MethodTableBuilder::EnumerateMethodImpls

//*******************************************************************************
//
// Find a method declaration that must reside in the scope passed in. This method cannot be called if
// the reference travels to another scope.
//
// Protect against finding a declaration that lives within
// us (the type being created)
//
HRESULT MethodTableBuilder::FindMethodDeclarationForMethodImpl(
            mdToken  pToken,       // Token that is being located (MemberRef or MemberDef)
            mdToken* pDeclaration, // [OUT] Method definition for Member
            BOOL fSameClass)       // Does the declaration need to be in this class
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    IMDInternalImport *pMDInternalImport = GetMDImport();

    PCCOR_SIGNATURE pSig;  // Signature of Member
    DWORD           cSig;
    LPCUTF8         szMember = NULL;

    // The token should be a member ref or def. If it is a ref then we need to travel
    // back to us hopefully.
    if(TypeFromToken(pToken) == mdtMemberRef)
    {
        // Get the parent
        mdToken typeref;
        if (FAILED(pMDInternalImport->GetParentOfMemberRef(pToken, &typeref)))
        {
            BAD_FORMAT_NOTHROW_ASSERT(!"Invalid MemberRef record");
            IfFailRet(COR_E_TYPELOAD);
        }
        GOTPARENT:
        if (TypeFromToken(typeref) == mdtMethodDef)
        {   // If parent is a method def then this is a varags method
            mdTypeDef typeDef;
            IfFailRet(pMDInternalImport->GetParentToken(typeref, &typeDef));

            if (TypeFromToken(typeDef) != mdtTypeDef)
            {   // A mdtMethodDef must be parented by a mdtTypeDef
                BAD_FORMAT_NOTHROW_ASSERT(!"MethodDef without TypeDef as Parent");
                IfFailRet(COR_E_TYPELOAD);
            }

            BAD_FORMAT_NOTHROW_ASSERT(typeDef == GetCl());

            // This is the real method we are overriding
            *pDeclaration = typeref;
        }
        else if (TypeFromToken(typeref) == mdtTypeSpec)
        {   // Added so that method impls can refer to instantiated interfaces or classes
            if (FAILED(pMDInternalImport->GetSigFromToken(typeref, &cSig, &pSig)))
            {
                BAD_FORMAT_NOTHROW_ASSERT(!"Invalid TypeSpec record");
                IfFailRet(COR_E_TYPELOAD);
            }
            CorElementType elemType = (CorElementType) *pSig++;

            if (elemType == ELEMENT_TYPE_GENERICINST)
            {   // If this is a generic inst, we expect that the next elem is ELEMENT_TYPE_CLASS,
                // which is handled in the case below.
                elemType = (CorElementType) *pSig++;
                BAD_FORMAT_NOTHROW_ASSERT(elemType == ELEMENT_TYPE_CLASS);
            }

            if (elemType == ELEMENT_TYPE_CLASS)
            {   // This covers E_T_GENERICINST and E_T_CLASS typespec formats. We don't expect
                // any other kinds to come through here.
                CorSigUncompressToken(pSig, &typeref);
            }
            else
            {   // This is an unrecognized signature format.
                BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT,
                                               IDS_CLASSLOAD_MI_BAD_SIG,
                                               mdMethodDefNil);
            }
            goto GOTPARENT;
        }
        else
        {   // Verify that the ref points back to us
            mdToken tkDef = mdTokenNil;

            if (TypeFromToken(typeref) == mdtTypeRef)
            {   // We only get here when we know the token does not reference a type in a different scope.
                LPCUTF8 pszNameSpace;
                LPCUTF8 pszClassName;

                if (FAILED(pMDInternalImport->GetNameOfTypeRef(typeref, &pszNameSpace, &pszClassName)))
                {
                    IfFailRet(COR_E_TYPELOAD);
                }
                mdToken tkRes;
                if (FAILED(pMDInternalImport->GetResolutionScopeOfTypeRef(typeref, &tkRes)))
                {
                    IfFailRet(COR_E_TYPELOAD);
                }
                hr = pMDInternalImport->FindTypeDef(pszNameSpace,
                                                    pszClassName,
                                                    (TypeFromToken(tkRes) == mdtTypeRef) ? tkRes : mdTokenNil,
                                                    &tkDef);
                if (FAILED(hr))
                {
                    IfFailRet(COR_E_TYPELOAD);
                }
            }
            else if (TypeFromToken(typeref) == mdtTypeDef)
            {   // We get a typedef when the parent of the token is a typespec to the type.
                tkDef = typeref;
            }
            else
            {
                CONSISTENCY_CHECK_MSGF(FALSE, ("Invalid methodimpl signature in class %s.", GetDebugClassName()));
                BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT,
                                               IDS_CLASSLOAD_MI_BAD_SIG,
                                               mdMethodDefNil);
            }

            if (fSameClass && tkDef != GetCl())
            {   // If we required that the typedef be the same type as the current class,
                // and it doesn't match, we need to return a failure result.
                IfFailRet(COR_E_TYPELOAD);
            }

            IfFailRet(pMDInternalImport->GetNameAndSigOfMemberRef(pToken, &pSig, &cSig, &szMember));

            if (isCallConv(
                MetaSig::GetCallingConvention(Signature(pSig, cSig)),
                IMAGE_CEE_CS_CALLCONV_FIELD))
            {
                return VLDTR_E_MR_BADCALLINGCONV;
            }

            hr = pMDInternalImport->FindMethodDef(
                tkDef, szMember, pSig, cSig, pDeclaration);

            IfFailRet(hr);
        }
    }
    else if (TypeFromToken(pToken) == mdtMethodDef)
    {
        mdTypeDef typeDef;

        // Verify that we are the parent
        hr = pMDInternalImport->GetParentToken(pToken, &typeDef);
        IfFailRet(hr);

        if(typeDef != GetCl())
        {
            IfFailRet(COR_E_TYPELOAD);
        }

        *pDeclaration = pToken;
    }
    else
    {
        IfFailRet(COR_E_TYPELOAD);
    }
    return hr;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif // _PREFAST_
//---------------------------------------------------------------------------------------
//
// Used by BuildMethodTable
//
// Enumerate this class's members
//
VOID
MethodTableBuilder::EnumerateClassMethods()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(bmtInternal));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtError));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD i;
    IMDInternalImport *pMDInternalImport = GetMDImport();
    mdToken tok;
    DWORD dwMemberAttrs;
    BOOL fIsClassEnum = IsEnum();
    BOOL fIsClassInterface = IsInterface();
    BOOL fIsClassValueType = IsValueClass();
    BOOL fIsClassComImport = IsComImport();
    BOOL fIsClassNotAbstract = (IsTdAbstract(GetAttrClass()) == 0);
    PCCOR_SIGNATURE pMemberSignature;
    ULONG           cMemberSignature;

    //
    // Run through the method list and calculate the following:
    // # methods.
    // # "other" methods (i.e. static or private)
    // # non-other methods
    //

    HENUMInternalHolder hEnumMethod(pMDInternalImport);
    hr = hEnumMethod.EnumInitNoThrow(mdtMethodDef, GetCl());
    if (FAILED(hr))
    {
        BuildMethodTableThrowException(hr, *bmtError);
    }

    // Allocate an array to contain the method tokens as well as information about the methods.
    DWORD cMethAndGaps = hEnumMethod.EnumGetCount();

    if ((DWORD)MAX_SLOT_INDEX <= cMethAndGaps)
        BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);

    bmtMethod->m_cMaxDeclaredMethods = (SLOT_INDEX)cMethAndGaps;
    bmtMethod->m_cDeclaredMethods = 0;
    bmtMethod->m_rgDeclaredMethods = new (GetStackingAllocator())
        bmtMDMethod *[bmtMethod->m_cMaxDeclaredMethods];

    enum { SeenCtor = 1, SeenInvoke = 2, SeenBeginInvoke = 4, SeenEndInvoke = 8};
    unsigned delegateMethodsSeen = 0;

    for (i = 0; i < cMethAndGaps; i++)
    {
        ULONG dwMethodRVA;
        DWORD dwImplFlags;
        METHOD_TYPE type;
        METHOD_IMPL_TYPE implType;
        LPSTR strMethodName;

#ifdef FEATURE_TYPEEQUIVALENCE
        // TypeEquivalent structs must not have methods
        if (bmtProp->fIsTypeEquivalent && fIsClassValueType)
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_EQUIVALENTSTRUCTMETHODS);
        }
#endif

        //
        // Go to the next method and retrieve its attributes.
        //

        hEnumMethod.EnumNext(&tok);
        DWORD   rid = RidFromToken(tok);
        if ((rid == 0)||(rid > pMDInternalImport->GetCountWithTokenKind(mdtMethodDef)))
        {
            BuildMethodTableThrowException(BFA_METHOD_TOKEN_OUT_OF_RANGE);
        }
        if (FAILED(pMDInternalImport->GetSigOfMethodDef(tok, &cMemberSignature, &pMemberSignature)))
        {
            BuildMethodTableThrowException(hr, BFA_BAD_SIGNATURE, mdMethodDefNil);
        }
        if (FAILED(pMDInternalImport->GetMethodDefProps(tok, &dwMemberAttrs)))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
        }

        bool isVtblGap = false;
        if (IsMdRTSpecialName(dwMemberAttrs) || IsMdVirtual(dwMemberAttrs) || IsDelegate())
        {
            if (FAILED(pMDInternalImport->GetNameOfMethodDef(tok, (LPCSTR *)&strMethodName)))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
            }
            if(IsStrLongerThan(strMethodName,MAX_CLASS_NAME))
            {
                BuildMethodTableThrowException(BFA_METHOD_NAME_TOO_LONG);
            }

            isVtblGap = IsMdRTSpecialName(dwMemberAttrs) && strncmp(strMethodName, "_VtblGap", 8) == 0;
        }
        else
        {
            strMethodName = NULL;
        }

        // Signature validation
        if (!bmtProp->fNoSanityChecks && !isVtblGap)
        {
            hr = validateTokenSig(tok,pMemberSignature,cMemberSignature,dwMemberAttrs,pMDInternalImport);
            if (FAILED(hr))
            {
                BuildMethodTableThrowException(hr, BFA_BAD_SIGNATURE, mdMethodDefNil);
            }
        }

        DWORD numGenericMethodArgs = 0;

        {
            SigParser genericArgParser(pMemberSignature, cMemberSignature);
            uint32_t ulCallConv;
            hr = genericArgParser.GetCallingConvInfo(&ulCallConv);
            if (FAILED(hr))
            {
                BuildMethodTableThrowException(hr, *bmtError);
            }

            // Only read the generic parameter table if the method signature is generic
            if (ulCallConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
            {
                HENUMInternalHolder hEnumTyPars(pMDInternalImport);
                hr = hEnumTyPars.EnumInitNoThrow(mdtGenericParam, tok);
                if (FAILED(hr))
                {
                    BuildMethodTableThrowException(hr, *bmtError);
                }

                numGenericMethodArgs = hEnumTyPars.EnumGetCount();

                // We do not want to support context-bound objects with generic methods.

                if (numGenericMethodArgs != 0)
                {
                    HENUMInternalHolder hEnumGenericPars(pMDInternalImport);

                    hEnumGenericPars.EnumInit(mdtGenericParam, tok);

                    for (unsigned methIdx = 0; methIdx < numGenericMethodArgs; methIdx++)
                    {
                        mdGenericParam tkTyPar;
                        pMDInternalImport->EnumNext(&hEnumGenericPars, &tkTyPar);
                        DWORD flags;
                        if (FAILED(pMDInternalImport->GetGenericParamProps(tkTyPar, NULL, &flags, NULL, NULL, NULL)))
                        {
                            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                        }

                        if (0 != (flags & ~(gpVarianceMask | gpSpecialConstraintMask)))
                        {
                            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                        }
                        switch (flags & gpVarianceMask)
                        {
                            case gpNonVariant:
                                break;

                            case gpCovariant: // intentional fallthru
                            case gpContravariant:
                                BuildMethodTableThrowException(VLDTR_E_GP_ILLEGAL_VARIANT_MVAR);
                                break;

                            default:
                                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                        }

                    }
                }
            }
        }

        //
        // We need to check if there are any gaps in the vtable. These are
        // represented by methods with the mdSpecial flag and a name of the form
        // _VTblGap_nnn (to represent nnn empty slots) or _VTblGap (to represent a
        // single empty slot).
        //

        if (isVtblGap)
        {
            //
            // This slot doesn't really exist, don't add it to the method
            // table. Instead it represents one or more empty slots, encoded
            // in the method name. Locate the beginning of the count in the
            // name. There are these points to consider:
            //   There may be no count present at all (in which case the
            //   count is taken as one).
            //   There may be an additional count just after Gap but before
            //   the '_'. We ignore this.
            //

            LPCSTR pos = strMethodName + 8;

            // Skip optional number.
            while (IS_DIGIT(*pos))
                pos++;

            WORD n = 0;

            // Check for presence of count.
            if (*pos == '\0')
            {
                n = 1;
            }
            else
            {
                if (*pos != '_')
                {
                    BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT,
                                                    IDS_CLASSLOAD_BADSPECIALMETHOD,
                                                    tok);
                }

                // Skip '_'.
                pos++;

                // Read count.
                bool fReadAtLeastOneDigit = false;
                while (IS_DIGIT(*pos))
                {
                    _ASSERTE(n < 6552);
                    n *= 10;
                    n += DIGIT_TO_INT(*pos);
                    pos++;
                    fReadAtLeastOneDigit = true;
                }

                // Check for end of name.
                if (*pos != '\0' || !fReadAtLeastOneDigit)
                {
                    BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT,
                                                    IDS_CLASSLOAD_BADSPECIALMETHOD,
                                                    tok);
                }
            }

#ifdef FEATURE_COMINTEROP
            // Record vtable gap in mapping list. The map is an optional field, so ensure we've allocated
            // these fields first.
            EnsureOptionalFieldsAreAllocated(GetHalfBakedClass(), m_pAllocMemTracker, GetLoaderAllocator()->GetLowFrequencyHeap());
            if (GetHalfBakedClass()->GetSparseCOMInteropVTableMap() == NULL)
                GetHalfBakedClass()->SetSparseCOMInteropVTableMap(new SparseVTableMap());

            GetHalfBakedClass()->GetSparseCOMInteropVTableMap()->RecordGap((WORD)NumDeclaredMethods(), n);

            bmtProp->fSparse = true;
#endif // FEATURE_COMINTEROP
            continue;
        }

        //
        // This is a real method so add it to the enumeration of methods. We now need to retrieve
        // information on the method and store it for later use.
        //
        if (FAILED(pMDInternalImport->GetMethodImplProps(tok, &dwMethodRVA, &dwImplFlags)))
        {
            BuildMethodTableThrowException(
                COR_E_BADIMAGEFORMAT,
                IDS_CLASSLOAD_BADSPECIALMETHOD,
                tok);
        }

        // Check for the presence of virtual static methods
        if (IsMdVirtual(dwMemberAttrs) && IsMdStatic(dwMemberAttrs))
        {
            bmtProp->fHasVirtualStaticMethods = TRUE;
        }

        //
        // But first - minimal flags validity checks
        //
        // No methods in Enums!
#ifndef _DEBUG // Don't run the minimal validity checks for the system dll/r2r dlls (except in debug builds so we don't build a bad system dll)
        if (!bmtProp->fNoSanityChecks)
#endif
        {
            if (fIsClassEnum)
            {
                BuildMethodTableThrowException(BFA_METHOD_IN_A_ENUM);
            }
            // RVA : 0
            if (dwMethodRVA != 0)
            {
                if(fIsClassComImport)
                {
                    BuildMethodTableThrowException(BFA_METHOD_WITH_NONZERO_RVA);
                }
                if(IsMdAbstract(dwMemberAttrs))
                {
                    BuildMethodTableThrowException(BFA_ABSTRACT_METHOD_WITH_RVA);
                }
                if(IsMiRuntime(dwImplFlags))
                {
                    BuildMethodTableThrowException(BFA_RUNTIME_METHOD_WITH_RVA);
                }
                if(IsMiInternalCall(dwImplFlags))
                {
                    BuildMethodTableThrowException(BFA_INTERNAL_METHOD_WITH_RVA);
                }
            }

            // Abstract / not abstract
            if(IsMdAbstract(dwMemberAttrs))
            {
                if(fIsClassNotAbstract)
                {
                    BuildMethodTableThrowException(BFA_AB_METHOD_IN_AB_CLASS);
                }
                if(!IsMdVirtual(dwMemberAttrs) && !IsMdStatic(dwMemberAttrs))
                {
                    BuildMethodTableThrowException(BFA_NONVIRT_AB_METHOD);
                }
            }
            else if(fIsClassInterface)
            {
                if (IsMdRTSpecialName(dwMemberAttrs))
                {
                    CONSISTENCY_CHECK(CheckPointer(strMethodName));
                    if (strcmp(strMethodName, COR_CCTOR_METHOD_NAME))
                    {
                        BuildMethodTableThrowException(BFA_NONAB_NONCCTOR_METHOD_ON_INT);
                    }
                }
            }

            // Virtual / not virtual
            if(IsMdVirtual(dwMemberAttrs))
            {
                if(IsMdPinvokeImpl(dwMemberAttrs))
                {
                    BuildMethodTableThrowException(BFA_VIRTUAL_PINVOKE_METHOD);
                }
                if(IsMdStatic(dwMemberAttrs))
                {
                    if (!fIsClassInterface)
                    {
                        // Static virtual methods are only allowed to exist in interfaces
                        BuildMethodTableThrowException(BFA_VIRTUAL_STATIC_METHOD);
                    }
                }
                if(strMethodName && (0==strcmp(strMethodName, COR_CTOR_METHOD_NAME)))
                {
                    BuildMethodTableThrowException(BFA_VIRTUAL_INSTANCE_CTOR);
                }
            }

            // Some interface checks.
            // We only need them if default interface method support is disabled or if this is fragile crossgen
#if !defined(FEATURE_DEFAULT_INTERFACES)
            if (fIsClassInterface)
            {
                if (IsMdVirtual(dwMemberAttrs))
                {
                    if (!IsMdAbstract(dwMemberAttrs))
                    {
                        BuildMethodTableThrowException(BFA_VIRTUAL_NONAB_INT_METHOD);
                    }
                }
                else
                {
                    // Instance method
                    if (!IsMdStatic(dwMemberAttrs))
                    {
                        BuildMethodTableThrowException(BFA_NONVIRT_INST_INT_METHOD);
                    }
                }
            }
#endif // !defined(FEATURE_DEFAULT_INTERFACES)

            // No synchronized methods in ValueTypes
            if(fIsClassValueType && IsMiSynchronized(dwImplFlags))
            {
                BuildMethodTableThrowException(BFA_SYNC_METHOD_IN_VT);
            }

            // Global methods:
            if(IsGlobalClass())
            {
                if(!IsMdStatic(dwMemberAttrs))
                {
                    BuildMethodTableThrowException(BFA_NONSTATIC_GLOBAL_METHOD);
                }
                if (strMethodName)  //<TODO>@todo: investigate mc++ generating null name</TODO>
                {
                    if(0==strcmp(strMethodName, COR_CTOR_METHOD_NAME))
                    {
                        BuildMethodTableThrowException(BFA_GLOBAL_INST_CTOR);
                    }
                }
            }
            //@GENERICS:
            // Generic methods or methods in generic classes
            // may not be part of a COM Import class, PInvoke, internal call outside CoreLib.
            if ((bmtGenerics->GetNumGenericArgs() != 0 || numGenericMethodArgs != 0) &&
                (
#ifdef FEATURE_COMINTEROP
                fIsClassComImport ||
                bmtProp->fComEventItfType ||
#endif // FEATURE_COMINTEROP
                IsMdPinvokeImpl(dwMemberAttrs) ||
                (IsMiInternalCall(dwImplFlags) && !GetModule()->IsSystem())))
            {
                BuildMethodTableThrowException(BFA_BAD_PLACE_FOR_GENERIC_METHOD);
            }

            // Generic methods may not be marked "runtime".  However note that
            // methods in generic delegate classes are, hence we don't apply this to
            // methods in generic classes in general.
            if (numGenericMethodArgs != 0 && IsMiRuntime(dwImplFlags))
            {
                BuildMethodTableThrowException(BFA_GENERIC_METHOD_RUNTIME_IMPL);
            }

            // Check the appearance of covariant and contravariant in the method signature
            // Note that variance is only supported for interfaces, and these rules are not
            // checked for non-virtual static methods as they cannot be called variantly.
            if ((bmtGenerics->pVarianceInfo != NULL) && (IsMdVirtual(dwMemberAttrs) || !IsMdStatic(dwMemberAttrs)))
            {
                SigPointer sp(pMemberSignature, cMemberSignature);
                uint32_t callConv;
                IfFailThrow(sp.GetCallingConvInfo(&callConv));

                if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
                    IfFailThrow(sp.GetData(NULL));

                uint32_t numArgs;
                IfFailThrow(sp.GetData(&numArgs));

                // Return type behaves covariantly
                if (!EEClass::CheckVarianceInSig(
                        bmtGenerics->GetNumGenericArgs(),
                        bmtGenerics->pVarianceInfo,
                        GetModule(),
                        sp,
                        gpCovariant))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_VARIANCE_IN_METHOD_RESULT, tok);
                }
                IfFailThrow(sp.SkipExactlyOne());
                for (uint32_t j = 0; j < numArgs; j++)
                {
                    // Argument types behave contravariantly
                    if (!EEClass::CheckVarianceInSig(bmtGenerics->GetNumGenericArgs(),
                                                    bmtGenerics->pVarianceInfo,
                                                    GetModule(),
                                                    sp,
                                                    gpContravariant))
                    {
                        BuildMethodTableThrowException(IDS_CLASSLOAD_VARIANCE_IN_METHOD_ARG, tok);
                    }
                    IfFailThrow(sp.SkipExactlyOne());
                }
            }
        }

        //
        // Determine the method's type
        //

        if (IsReallyMdPinvokeImpl(dwMemberAttrs) || IsMiInternalCall(dwImplFlags))
        {
            hr = NDirect::HasNAT_LAttribute(pMDInternalImport, tok, dwMemberAttrs);

            // There was a problem querying for the attribute
            if (FAILED(hr))
            {
                BuildMethodTableThrowException(hr, IDS_CLASSLOAD_BADPINVOKE, tok);
            }

            // The attribute is not present
            if (hr == S_FALSE)
            {
#ifdef FEATURE_COMINTEROP
                if (fIsClassComImport || bmtProp->fComEventItfType)
                {
                    // ComImport classes have methods which are just used
                    // for implementing all interfaces the class supports
                    type = METHOD_TYPE_COMINTEROP;

                    // constructor is special
                    if (IsMdRTSpecialName(dwMemberAttrs))
                    {
                        // Note: Method name (.ctor) will be checked in code:ValidateMethods
                        type = METHOD_TYPE_FCALL;
                    }
                }
                else
#endif //FEATURE_COMINTEROP
                if (dwMethodRVA == 0)
                {
                    type = METHOD_TYPE_FCALL;
                }
                else
                {
                    type = METHOD_TYPE_NDIRECT;
                }
            }
            // The NAT_L attribute is present, marking this method as NDirect
            else
            {
                CONSISTENCY_CHECK(hr == S_OK);
                type = METHOD_TYPE_NDIRECT;
            }
        }
        else if (IsMiRuntime(dwImplFlags))
        {
                // currently the only runtime implemented functions are delegate instance methods
            if (!IsDelegate() || IsMdStatic(dwMemberAttrs) || IsMdAbstract(dwMemberAttrs))
            {
                BuildMethodTableThrowException(BFA_BAD_RUNTIME_IMPL);
            }

            unsigned newDelegateMethodSeen = 0;

            if (IsMdRTSpecialName(dwMemberAttrs))   // .ctor
            {
                if (strcmp(strMethodName, COR_CTOR_METHOD_NAME) != 0 || IsMdVirtual(dwMemberAttrs))
                {
                    BuildMethodTableThrowException(BFA_BAD_FLAGS_ON_DELEGATE);
                }
                newDelegateMethodSeen = SeenCtor;
                type = METHOD_TYPE_FCALL;
            }
            else
            {
                if (strcmp(strMethodName, "Invoke") == 0)
                    newDelegateMethodSeen = SeenInvoke;
                else if (strcmp(strMethodName, "BeginInvoke") == 0)
                    newDelegateMethodSeen = SeenBeginInvoke;
                else if (strcmp(strMethodName, "EndInvoke") == 0)
                    newDelegateMethodSeen = SeenEndInvoke;
                else
                {
                    BuildMethodTableThrowException(BFA_UNKNOWN_DELEGATE_METHOD);
                }
                type = METHOD_TYPE_EEIMPL;
            }

            // If we get here we have either set newDelegateMethodSeen or we have thrown a BMT exception
            _ASSERTE(newDelegateMethodSeen != 0);

            if ((delegateMethodsSeen & newDelegateMethodSeen) != 0)
            {
                BuildMethodTableThrowException(BFA_DUPLICATE_DELEGATE_METHOD);
            }

            delegateMethodsSeen |= newDelegateMethodSeen;
        }
        else if (numGenericMethodArgs != 0)
        {
            //We use an instantiated method desc to represent a generic method
            type = METHOD_TYPE_INSTANTIATED;
        }
        else if (fIsClassInterface)
        {
#ifdef FEATURE_COMINTEROP
            if (IsMdStatic(dwMemberAttrs))
            {
                // Static methods in interfaces need nothing special.
                type = METHOD_TYPE_NORMAL;
            }
            else if (bmtGenerics->GetNumGenericArgs() != 0 &&
                (bmtGenerics->fSharedByGenericInstantiations))
            {
                // Methods in instantiated interfaces need nothing special - they are not visible from COM etc.
                type = METHOD_TYPE_NORMAL;
            }
            else if (bmtProp->fIsMngStandardItf)
            {
                // If the interface is a standard managed interface then allocate space for an FCall method desc.
                type = METHOD_TYPE_FCALL;
            }
            else if (IsMdAbstract(dwMemberAttrs))
            {
                // If COM interop is supported then all other interface MDs may be
                // accessed via COM interop. mcComInterop MDs have an additional
                // pointer-sized field pointing to COM interop data which are
                // allocated lazily when/if the MD actually gets used for interop.
                type = METHOD_TYPE_COMINTEROP;
            }
            else
#endif // !FEATURE_COMINTEROP
            {
                // This codepath is used by remoting
                type = METHOD_TYPE_NORMAL;
            }
        }
        else
        {
            type = METHOD_TYPE_NORMAL;
        }

        // Generic methods should always be METHOD_TYPE_INSTANTIATED
        if ((numGenericMethodArgs != 0) && (type != METHOD_TYPE_INSTANTIATED))
        {
            BuildMethodTableThrowException(BFA_GENERIC_METHODS_INST);
        }

        // count how many overrides this method does All methods bodies are defined
        // on this type so we can just compare the tok with the body token found
        // from the overrides.
        implType = METHOD_IMPL_NOT;
        for (DWORD impls = 0; impls < bmtMethod->dwNumberMethodImpls; impls++)
        {
            if (bmtMetaData->rgMethodImplTokens[impls].methodBody == tok)
            {
                implType = METHOD_IMPL;
                break;
            }
        }

        // For delegates we don't allow any non-runtime implemented bodies
        // for any of the four special methods
        if (IsDelegate() && !IsMiRuntime(dwImplFlags))
        {
            if ((strcmp(strMethodName, COR_CTOR_METHOD_NAME) == 0) ||
                (strcmp(strMethodName, "Invoke")             == 0) ||
                (strcmp(strMethodName, "BeginInvoke")        == 0) ||
                (strcmp(strMethodName, "EndInvoke")          == 0)   )
            {
                BuildMethodTableThrowException(BFA_ILLEGAL_DELEGATE_METHOD);
            }
        }

        //
        // Create a new bmtMDMethod representing this method and add it to the
        // declared method list.
        //

        bmtMDMethod * pNewMethod = new (GetStackingAllocator()) bmtMDMethod(
            bmtInternal->pType,
            tok,
            dwMemberAttrs,
            dwImplFlags,
            dwMethodRVA,
            type,
            implType);

        bmtMethod->AddDeclaredMethod(pNewMethod);

        //
        // Update the count of the various types of methods.
        //

        bmtVT->dwMaxVtableSize++;

        // Increment the number of non-abstract declared methods
        if (!IsMdAbstract(dwMemberAttrs))
        {
            bmtMethod->dwNumDeclaredNonAbstractMethods++;
        }
    }

    if (bmtMethod->dwNumDeclaredNonAbstractMethods == 0)
    {
        GetHalfBakedClass()->SetHasOnlyAbstractMethods();
    }

    // Check to see that we have all of the required delegate methods (ECMA 13.6 Delegates)
    if (IsDelegate())
    {
        // Do we have all four special delegate methods
        // or just the two special delegate methods
        if ((delegateMethodsSeen != (SeenCtor | SeenInvoke | SeenBeginInvoke | SeenEndInvoke)) &&
            (delegateMethodsSeen != (SeenCtor | SeenInvoke)) )
        {
            BuildMethodTableThrowException(BFA_MISSING_DELEGATE_METHOD);
        }
    }

    if (i != cMethAndGaps)
    {
        BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_METHOD_COUNT, mdTokenNil);
    }

#ifdef FEATURE_COMINTEROP
    //
    // If the interface is sparse, we need to finalize the mapping list by
    // telling it how many real methods we found.
    //

    if (bmtProp->fSparse)
    {
        GetHalfBakedClass()->GetSparseCOMInteropVTableMap()->FinalizeMapping(NumDeclaredMethods());
    }
#endif // FEATURE_COMINTEROP
} // MethodTableBuilder::EnumerateClassMethods
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*******************************************************************************
//
// Run through the field list and calculate the following:
// # static fields
// # static fields that contain object refs.
// # instance fields
//
VOID
MethodTableBuilder::EnumerateClassFields()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    DWORD i;
    IMDInternalImport *pMDInternalImport = GetMDImport();
    mdToken tok;
    DWORD dwMemberAttrs;

    bmtEnumFields->dwNumStaticFields        = 0;
    bmtEnumFields->dwNumStaticObjRefFields  = 0;
    bmtEnumFields->dwNumStaticBoxedFields   = 0;

    bmtEnumFields->dwNumThreadStaticFields  = 0;
    bmtEnumFields->dwNumThreadStaticObjRefFields  = 0;
    bmtEnumFields->dwNumThreadStaticBoxedFields   = 0;

    bmtEnumFields->dwNumInstanceFields      = 0;

    HENUMInternalHolder hEnumField(pMDInternalImport);
    hr = hEnumField.EnumInitNoThrow(mdtFieldDef, GetCl());
    if (FAILED(hr))
    {
        BuildMethodTableThrowException(hr, *bmtError);
    }

    bmtMetaData->cFields = hEnumField.EnumGetCount();

    // Retrieve the fields and store them in a temp array.
    bmtMetaData->pFields = new (GetStackingAllocator()) mdToken[bmtMetaData->cFields];
    bmtMetaData->pFieldAttrs = new (GetStackingAllocator()) DWORD[bmtMetaData->cFields];

    DWORD   dwFieldLiteralInitOnly = fdLiteral | fdInitOnly;
    DWORD   dwMaxFieldDefRid = pMDInternalImport->GetCountWithTokenKind(mdtFieldDef);

    for (i = 0; hEnumField.EnumNext(&tok); i++)
    {
        //
        // Retrieve the attributes of the field.
        //
        DWORD rid = RidFromToken(tok);
        if ((rid == 0)||(rid > dwMaxFieldDefRid))
        {
            BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, BFA_BAD_FIELD_TOKEN, mdTokenNil);
        }

        if (FAILED(pMDInternalImport->GetFieldDefProps(tok, &dwMemberAttrs)))
        {
            BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, BFA_BAD_FIELD_TOKEN, tok);
        }

        //
        // Store the field and its attributes in the bmtMetaData structure for later use.
        //

        bmtMetaData->pFields[i] = tok;
        bmtMetaData->pFieldAttrs[i] = dwMemberAttrs;

        if((dwMemberAttrs & fdFieldAccessMask)==fdFieldAccessMask)
        {
            BuildMethodTableThrowException(BFA_INVALID_FIELD_ACC_FLAGS);
        }
        if((dwMemberAttrs & dwFieldLiteralInitOnly)==dwFieldLiteralInitOnly)
        {
            BuildMethodTableThrowException(BFA_FIELD_LITERAL_AND_INIT);
        }

        // can only have static global fields
        if(IsGlobalClass())
        {
            if(!IsFdStatic(dwMemberAttrs))
            {
                BuildMethodTableThrowException(BFA_NONSTATIC_GLOBAL_FIELD);
            }
        }

        //
        // Update the count of the various types of fields.
        //

        if (IsFdStatic(dwMemberAttrs))
        {
            if (!IsFdLiteral(dwMemberAttrs))
            {
#ifdef FEATURE_TYPEEQUIVALENCE
                if (bmtProp->fIsTypeEquivalent)
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_EQUIVALENTSTRUCTFIELDS);
                }
#endif

                bmtEnumFields->dwNumStaticFields++;

                // If this static field is thread static, then we need
                // to increment bmtEnumFields->dwNumThreadStaticFields
                hr = GetCustomAttribute(tok,
                                        WellKnownAttribute::ThreadStatic,
                                        NULL, NULL);
                IfFailThrow(hr);
                if (hr == S_OK)
                {
                    // It's a thread static, so increment the count
                    bmtEnumFields->dwNumThreadStaticFields++;
                }
            }
        }
        else
        {
#ifdef FEATURE_TYPEEQUIVALENCE
            if (!IsFdPublic(dwMemberAttrs) && bmtProp->fIsTypeEquivalent)
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_EQUIVALENTSTRUCTFIELDS);
            }
#endif

            if (!IsFdLiteral(dwMemberAttrs))
            {
                bmtEnumFields->dwNumInstanceFields++;
            }
            if(IsInterface())
            {
                BuildMethodTableThrowException(BFA_INSTANCE_FIELD_IN_INT);
            }
        }
    }

    if (i != bmtMetaData->cFields)
    {
        BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD_COUNT, mdTokenNil);
    }

    if(IsEnum() && (bmtEnumFields->dwNumInstanceFields==0))
    {
        BuildMethodTableThrowException(BFA_INSTANCE_FIELD_IN_ENUM);
    }

    bmtEnumFields->dwNumDeclaredFields = bmtEnumFields->dwNumStaticFields + bmtEnumFields->dwNumInstanceFields;
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Determines the maximum size of the vtable and allocates the temporary storage arrays
// Also copies the parent's vtable into the working vtable.
//
VOID    MethodTableBuilder::AllocateWorkingSlotTables()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtAllocator));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtFP));
        PRECONDITION(CheckPointer(bmtParent));

    }
    CONTRACTL_END;

    // Allocate a FieldDesc* for each field
    bmtMFDescs->ppFieldDescList = new (GetStackingAllocator()) FieldDesc*[bmtMetaData->cFields];
    ZeroMemory(bmtMFDescs->ppFieldDescList, bmtMetaData->cFields * sizeof(FieldDesc *));

    // Create a temporary function table (we don't know how large the vtable will be until the very end,
    // since we don't yet know how many declared methods are overrides vs. newslots).

    if (IsValueClass())
    {   // ValueClass virtuals are converted into non-virtual methods and the virtual slots
        // become unboxing stubs that forward to these new non-virtual methods. This has the
        // side effect of doubling the number of slots introduced by newslot virtuals.
        bmtVT->dwMaxVtableSize += NumDeclaredMethods();
    }

    _ASSERTE(!HasParent() || (bmtInterface->dwInterfaceMapSize - GetParentMethodTable()->GetNumInterfaces()) >= 0);

    if (HasParent())
    {   // Add parent vtable size. <TODO> This should actually be the parent's virtual method count. </TODO>
        bmtVT->dwMaxVtableSize += bmtParent->pSlotTable->GetSlotCount();
    }

    S_SLOT_INDEX cMaxSlots = AsClrSafeInt(bmtVT->dwMaxVtableSize) + AsClrSafeInt(NumDeclaredMethods());

    if (cMaxSlots.IsOverflow() || MAX_SLOT_INDEX < cMaxSlots.Value())
        cMaxSlots = S_SLOT_INDEX(MAX_SLOT_INDEX);

    // Allocate the temporary vtable
    bmtVT->pSlotTable = new (GetStackingAllocator())
        bmtMethodSlotTable(cMaxSlots.Value(), GetStackingAllocator());

    if (HasParent())
    {
#if 0
        // @<TODO>todo: Figure out the right way to override Equals for value
        // types only.
        //
        // This is broken because
        // (a) g_pObjectClass->FindMethod("Equals", &gsig_IM_Obj_RetBool); will return
        //      the EqualsValue method
        // (b) When CoreLib has been preloaded (and thus the munge already done
        //      ahead of time), we cannot easily find both methods
        //      to compute EqualsAddr & EqualsSlot
        //
        // For now, the Equals method has a runtime check to see if it's
        // comparing value types.
        //</TODO>

        // If it is a value type, over ride a few of the base class methods.
        if (IsValueClass())
        {
            static WORD EqualsSlot;

            // If we haven't been through here yet, get some stuff from the Object class definition.
            if (EqualsSlot == NULL)
            {
                // Get the slot of the Equals method.
                MethodDesc *pEqualsMD = g_pObjectClass->FindMethod("Equals", &gsig_IM_Obj_RetBool);
                THROW_BAD_FORMAT_MAYBE(pEqualsMD != NULL, 0, this);
                EqualsSlot = pEqualsMD->GetSlot();

                // Get the address of the EqualsValue method.
                MethodDesc *pEqualsValueMD = g_pObjectClass->FindMethod("EqualsValue", &gsig_IM_Obj_RetBool);
                THROW_BAD_FORMAT_MAYBE(pEqualsValueMD != NULL, 0, this);

                // Patch the EqualsValue method desc in a dangerous way to
                // look like the Equals method desc.
                pEqualsValueMD->SetSlot(EqualsSlot);
                pEqualsValueMD->SetMemberDef(pEqualsMD->GetMemberDef());
            }

            // Override the valuetype "Equals" with "EqualsValue".
            bmtVT->SetMethodDescForSlot(EqualsSlot, EqualsSlot);
        }
#endif // 0
    }

    S_UINT32 cEntries = S_UINT32(2) * S_UINT32(NumDeclaredMethods());
    if (cEntries.IsOverflow())
    {
        ThrowHR(COR_E_OVERFLOW);
    }
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Allocate a MethodDesc* for each method (needed later when doing interfaces), and a FieldDesc* for each field
//
VOID MethodTableBuilder::AllocateFieldDescs()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtAllocator));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtFP));
        PRECONDITION(CheckPointer(bmtParent));

    }
    CONTRACTL_END;

    // We'll be counting the # fields of each size as we go along
    for (DWORD i = 0; i <= MAX_LOG2_PRIMITIVE_FIELD_SIZE; i++)
    {
        bmtFP->NumRegularStaticFieldsOfSize[i]    = 0;
        bmtFP->NumThreadStaticFieldsOfSize[i]    = 0;
        bmtFP->NumInstanceFieldsOfSize[i]  = 0;
    }

    //
    // Allocate blocks of MethodDescs and FieldDescs for all declared methods and fields
    //
    // In order to avoid allocating a field pointing back to the method
    // table in every single method desc, we allocate memory in the
    // following manner:
    //   o  Field descs get a single contiguous block.
    //   o  Method descs of different sizes (normal vs NDirect) are
    //      allocated in different MethodDescChunks.
    //   o  Each method desc chunk starts with a header, and has
    //      at most MAX_ method descs (if there are more
    //      method descs of a given size, multiple chunks are allocated).
    // This way method descs can use an 8-bit offset field to locate the
    // pointer to their method table.
    //

    /////////////////////////////////////////////////////////////////
    // Allocate fields
    if (NumDeclaredFields() > 0)
    {
        GetHalfBakedClass()->SetFieldDescList((FieldDesc *)
            AllocateFromHighFrequencyHeap(S_SIZE_T(NumDeclaredFields()) * S_SIZE_T(sizeof(FieldDesc))));
        INDEBUG(GetClassLoader()->m_dwDebugFieldDescs += NumDeclaredFields();)
        INDEBUG(GetClassLoader()->m_dwFieldDescData += (NumDeclaredFields() * sizeof(FieldDesc));)
    }
}

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
//*******************************************************************************
//
// Heuristic to determine if we should have instances of this class 8 byte aligned
//
BOOL MethodTableBuilder::ShouldAlign8(DWORD dwR8Fields, DWORD dwTotalFields)
{
    LIMITED_METHOD_CONTRACT;

    return dwR8Fields*2>dwTotalFields && dwR8Fields>=2;
}
#endif

//*******************************************************************************
BOOL MethodTableBuilder::IsSelfReferencingStaticValueTypeField(mdToken     dwByValueClassToken,
                                                               bmtInternalInfo* bmtInternal,
                                                               const bmtGenericsInfo *bmtGenerics,
                                                               PCCOR_SIGNATURE pMemberSignature,
                                                               DWORD       cMemberSignature)
{
    STANDARD_VM_CONTRACT;

    if (dwByValueClassToken != this->GetCl())
    {
        return FALSE;
    }

    if (!bmtGenerics->HasInstantiation())
    {
        return TRUE;
    }

    // The value class is generic.  Check that the signature of the field
    // is _exactly_ equivalent to VC<!0, !1, !2, ...>.  Do this by consing up a fake
    // signature.
    DWORD nGenericArgs = bmtGenerics->GetNumGenericArgs();
    CONSISTENCY_CHECK(nGenericArgs != 0);

    SigBuilder sigBuilder;

    sigBuilder.AppendElementType(ELEMENT_TYPE_GENERICINST);
    sigBuilder.AppendElementType(ELEMENT_TYPE_VALUETYPE);
    sigBuilder.AppendToken(dwByValueClassToken);
    sigBuilder.AppendData(nGenericArgs);
    for (unsigned int typearg = 0; typearg < nGenericArgs; typearg++)
    {
        sigBuilder.AppendElementType(ELEMENT_TYPE_VAR);
        sigBuilder.AppendData(typearg);
    }

    DWORD cFakeSig;
    PCCOR_SIGNATURE pFakeSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cFakeSig);

    PCCOR_SIGNATURE pFieldSig = pMemberSignature + 1; // skip the CALLCONV_FIELD

    return MetaSig::CompareElementType(pFakeSig, pFieldSig,
                                       pFakeSig + cFakeSig,  pMemberSignature + cMemberSignature,
                                       GetModule(), GetModule(),
                                       NULL, NULL, FALSE);

}

//*******************************************************************************
//
// Used pByValueClass cache to mark self-references
//
static BOOL IsSelfRef(MethodTable * pMT)
{
    return pMT == (MethodTable *)-1;
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Go thru all fields and initialize their FieldDescs.
//
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif // _PREFAST_

VOID    MethodTableBuilder::InitializeFieldDescs(FieldDesc *pFieldDescList,
                                                 const LayoutRawFieldInfo* pLayoutRawFieldInfos,
                                                 bmtInternalInfo* bmtInternal,
                                                 const bmtGenericsInfo* bmtGenerics,
                                                 bmtMetaDataInfo* bmtMetaData,
                                                 bmtEnumFieldInfo* bmtEnumFields,
                                                 bmtErrorInfo* bmtError,
                                                 MethodTable *** pByValueClassCache,
                                                 bmtMethAndFieldDescs* bmtMFDescs,
                                                 bmtFieldPlacement* bmtFP,
                                                 unsigned* totalDeclaredSize)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtInternal));
        PRECONDITION(CheckPointer(bmtGenerics));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtError));
        PRECONDITION(CheckPointer(pByValueClassCache));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtFP));
        PRECONDITION(CheckPointer(totalDeclaredSize));
    }
    CONTRACTL_END;

    DWORD i;
    IMDInternalImport * pInternalImport = GetMDImport(); // to avoid multiple dereferencings

//========================================================================
// BEGIN:
//    Go thru all fields and initialize their FieldDescs.
//========================================================================

    DWORD   dwCurrentDeclaredField = 0;
    DWORD   dwCurrentStaticField   = 0;
    DWORD   dwCurrentThreadStaticField = 0;


    DWORD   dwR8Fields              = 0;        // Number of R8's the class has

#ifdef FEATURE_64BIT_ALIGNMENT
    // Track whether any field in this type requires 8-byte alignment
    BOOL    fFieldRequiresAlign8 = HasParent() ? GetParentMethodTable()->RequiresAlign8() : FALSE;
#endif

    for (i = 0; i < bmtMetaData->cFields; i++)
    {
        PCCOR_SIGNATURE pMemberSignature;
        DWORD       cMemberSignature;
        DWORD       dwMemberAttrs;

        dwMemberAttrs = bmtMetaData->pFieldAttrs[i];

        BOOL fIsStatic = IsFdStatic(dwMemberAttrs);

        // We don't store static final primitive fields in the class layout
        if (IsFdLiteral(dwMemberAttrs))
            continue;

        if (!IsFdPublic(dwMemberAttrs))
            SetHasNonPublicFields();

        if (IsFdNotSerialized(dwMemberAttrs))
            SetCannotBeBlittedByObjectCloner();

        IfFailThrow(pInternalImport->GetSigOfFieldDef(bmtMetaData->pFields[i], &cMemberSignature, &pMemberSignature));
        // Signature validation
        IfFailThrow(validateTokenSig(bmtMetaData->pFields[i],pMemberSignature,cMemberSignature,dwMemberAttrs,pInternalImport));

        FieldDesc * pFD;
        DWORD       dwLog2FieldSize = 0;
        BOOL        bCurrentFieldIsGCPointer = FALSE;
        mdToken     dwByValueClassToken = 0;
        MethodTable * pByValueClass = NULL;
        BOOL        fIsByValue = FALSE;
        BOOL        fIsThreadStatic = FALSE;
        BOOL        fHasRVA = FALSE;

        MetaSig fsig(pMemberSignature,
                     cMemberSignature,
                     GetModule(),
                     &bmtGenerics->typeContext,
                     MetaSig::sigField);
        CorElementType ElementType = fsig.NextArg();


        // Get type
        if (!isCallConv(fsig.GetCallingConvention(), IMAGE_CEE_CS_CALLCONV_FIELD))
        {
            IfFailThrow(COR_E_TYPELOAD);
        }

        // Determine if a static field is special i.e. RVA based, local to
        // a thread or a context
        if (fIsStatic)
        {
            if (IsFdHasFieldRVA(dwMemberAttrs))
            {
                fHasRVA = TRUE;
            }

            HRESULT hr;

            hr = GetCustomAttribute(bmtMetaData->pFields[i],
                                    WellKnownAttribute::ThreadStatic,
                                    NULL, NULL);
            IfFailThrow(hr);
            if (hr == S_OK)
            {
                fIsThreadStatic = TRUE;
            }


            if (ElementType == ELEMENT_TYPE_VALUETYPE)
            {
                hr = GetCustomAttribute(bmtMetaData->pFields[i],
                                        WellKnownAttribute::FixedAddressValueType,
                                        NULL, NULL);
                IfFailThrow(hr);
                if (hr == S_OK)
                {
                    bmtFP->fHasFixedAddressValueTypes = true;
                }
            }


            // Do some sanity checks that we are not mixing context and thread
            // relative statics.
            if (fHasRVA && fIsThreadStatic)
            {
                IfFailThrow(COR_E_TYPELOAD);
            }

            if (bmtFP->fHasFixedAddressValueTypes && GetAssembly()->IsCollectible())
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_COLLECTIBLEFIXEDVTATTR);
            }
        }


    GOT_ELEMENT_TYPE:
        // Type to store in FieldDesc - we don't want to have extra case statements for
        // ELEMENT_TYPE_STRING, SDARRAY etc., so we convert all object types to CLASS.
        // Also, BOOLEAN, CHAR are converted to U1, I2.
        CorElementType FieldDescElementType = ElementType;

        switch (ElementType)
        {
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
            {
                dwLog2FieldSize = 0;
                break;
            }

        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            {
                dwLog2FieldSize = 1;
                break;
            }

        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        IN_TARGET_32BIT(case ELEMENT_TYPE_I:)
        IN_TARGET_32BIT(case ELEMENT_TYPE_U:)
        case ELEMENT_TYPE_R4:
            {
                dwLog2FieldSize = 2;
                break;
            }

        case ELEMENT_TYPE_BOOLEAN:
            {
                //                FieldDescElementType = ELEMENT_TYPE_U1;
                dwLog2FieldSize = 0;
                break;
            }

        case ELEMENT_TYPE_CHAR:
            {
                //                FieldDescElementType = ELEMENT_TYPE_U2;
                dwLog2FieldSize = 1;
                break;
            }

        case ELEMENT_TYPE_R8:
            {
                dwR8Fields++;

                // Deliberate fall through...
                FALLTHROUGH;
            }

        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        IN_TARGET_64BIT(case ELEMENT_TYPE_I:)
        IN_TARGET_64BIT(case ELEMENT_TYPE_U:)
            {
#ifdef FEATURE_64BIT_ALIGNMENT
                // Record that this field requires alignment for Int64/UInt64.
                if(!fIsStatic)
                    fFieldRequiresAlign8 = true;
#endif
                dwLog2FieldSize = 3;
                break;
            }

        case ELEMENT_TYPE_FNPTR:
        case ELEMENT_TYPE_PTR:   // ptrs are unmanaged scalars, for layout
            {
                dwLog2FieldSize = LOG2_PTRSIZE;
                break;
            }

        case ELEMENT_TYPE_BYREF:
            {
                dwLog2FieldSize = LOG2_PTRSIZE;
                if (fIsStatic)
                {
                    // Byref-like types cannot be used for static fields
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BYREFLIKE_STATICFIELD);
                }
                if (!bmtFP->fIsByRefLikeType)
                {
                    // Non-byref-like types cannot contain byref-like instance fields
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BYREFLIKE_INSTANCEFIELD);
                }
                break;
            }

        case ELEMENT_TYPE_TYPEDBYREF:
            {
                goto IS_VALUETYPE;
            }

        // Class type variable (method type variables aren't allowed in fields)
        // These only occur in open types used for verification/reflection.
        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
           // deliberate drop through - do fake field layout
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_SZARRAY:      // single dim, zero
        case ELEMENT_TYPE_ARRAY:        // all other arrays
        case ELEMENT_TYPE_CLASS: // objectrefs
        case ELEMENT_TYPE_OBJECT:
            {
                dwLog2FieldSize = LOG2_PTRSIZE;
                bCurrentFieldIsGCPointer = TRUE;
                FieldDescElementType = ELEMENT_TYPE_CLASS;

                if (!fIsStatic)
                {
                    SetHasFieldsWhichMustBeInited();
                    if (ElementType != ELEMENT_TYPE_STRING)
                        SetCannotBeBlittedByObjectCloner();
                }
                else
                {   // EnumerateFieldDescs already counted the total number of static vs. instance
                    // fields, now we're further subdividing the static field count by GC and non-GC.
                    bmtEnumFields->dwNumStaticObjRefFields++;
                    if (fIsThreadStatic)
                        bmtEnumFields->dwNumThreadStaticObjRefFields++;
                }
                break;
            }

        case ELEMENT_TYPE_VALUETYPE: // a byvalue class field
            {
                Module * pTokenModule;
                dwByValueClassToken = fsig.GetArgProps().PeekValueTypeTokenClosed(GetModule(), &bmtGenerics->typeContext, &pTokenModule);

                // By-value class
                BAD_FORMAT_NOTHROW_ASSERT(dwByValueClassToken != 0);

                if (this->IsValueClass() && (pTokenModule == GetModule()))
                {
                    if (TypeFromToken(dwByValueClassToken) == mdtTypeRef)
                    {
                        // It's a typeref - check if it's a class that has a static field of itself
                        LPCUTF8 pszNameSpace;
                        LPCUTF8 pszClassName;
                        if (FAILED(pInternalImport->GetNameOfTypeRef(dwByValueClassToken, &pszNameSpace, &pszClassName)))
                        {
                            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                        }

                        if (IsStrLongerThan((char *)pszClassName, MAX_CLASS_NAME)
                            || IsStrLongerThan((char *)pszNameSpace, MAX_CLASS_NAME)
                            || (strlen(pszClassName) + strlen(pszNameSpace) + 1 >= MAX_CLASS_NAME))
                        {
                            BuildMethodTableThrowException(BFA_TYPEREG_NAME_TOO_LONG, mdMethodDefNil);
                        }

                        mdToken tkRes;
                        if (FAILED(pInternalImport->GetResolutionScopeOfTypeRef(dwByValueClassToken, &tkRes)))
                        {
                            BuildMethodTableThrowException(BFA_BAD_TYPEREF_TOKEN, dwByValueClassToken);
                        }

                        if (TypeFromToken(tkRes) == mdtTypeRef)
                        {
                            if (!pInternalImport->IsValidToken(tkRes))
                            {
                                BuildMethodTableThrowException(BFA_BAD_TYPEREF_TOKEN, mdMethodDefNil);
                            }
                        }
                        else
                        {
                            tkRes = mdTokenNil;
                        }

                        if (FAILED(pInternalImport->FindTypeDef(pszNameSpace,
                                                                pszClassName,
                                                                tkRes,
                                                                &dwByValueClassToken)))
                        {
                            dwByValueClassToken = mdTokenNil;
                        }
                    } // If field is static typeref

                    BOOL selfref = IsSelfReferencingStaticValueTypeField(dwByValueClassToken,
                                                                    bmtInternal,
                                                                    bmtGenerics,
                                                                    pMemberSignature,
                                                                    cMemberSignature);

                    if (selfref)
                    {   // immediately self-referential fields must be static.
                        if (!fIsStatic)
                        {
                            BuildMethodTableThrowException(IDS_CLASSLOAD_VALUEINSTANCEFIELD, mdMethodDefNil);
                        }

                        if (!IsValueClass())
                        {
                            BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_MUST_BE_BYVAL, mdTokenNil);
                        }

                        pByValueClass = (MethodTable *)-1;
                    }
                } // If 'this' is a value class
            }
            // TypedReference shares the rest of the code here
IS_VALUETYPE:
            {
                fIsByValue = TRUE;

                // It's not self-referential so try to load it
                if (pByValueClass == NULL)
                {
                    // Loading a non-self-ref valuetype field.
                    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOAD_APPROXPARENTS);
                    // We load the approximate type of the field to avoid recursion problems.
                    // MethodTable::DoFullyLoad() will later load it fully
                    pByValueClass = fsig.GetArgProps().GetTypeHandleThrowing(GetModule(),
                                                                            &bmtGenerics->typeContext,
                                                                             ClassLoader::LoadTypes,
                                                                             CLASS_LOAD_APPROXPARENTS,
                                                                             TRUE
                                                                             ).GetMethodTable();
                }

                // #FieldDescTypeMorph  IF it is an enum, strip it down to its underlying type
                if (IsSelfRef(pByValueClass) ? IsEnum() : pByValueClass->IsEnum())
                {
                    if (IsSelfRef(pByValueClass))
                    {   // It is self-referencing enum (ValueType) static field - it is forbidden in the ECMA spec, but supported by CLR since v1
                        // Note: literal static fields are skipped early in this loop
                        if (bmtMFDescs->ppFieldDescList[0] == NULL)
                        {   // The field is defined before (the only) instance field
                            // AppCompat with 3.5 SP1 and 4.0 RTM behavior
                            BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD, mdTokenNil);
                        }
                        // We will treat the field type as if it was its underlying type (we know its size and will check correctly RVA with the size
                        // later in this method)
                        // Therefore we do not have to run code:VerifySelfReferencingStaticValueTypeFields_WithRVA or code:#SelfReferencingStaticValueTypeField_Checks
                    }
                    BAD_FORMAT_NOTHROW_ASSERT((IsSelfRef(pByValueClass) ?
                            bmtEnumFields->dwNumInstanceFields : pByValueClass->GetNumInstanceFields())
                                == 1); // enums must have exactly one field
                    FieldDesc * enumField = IsSelfRef(pByValueClass) ?
                            bmtMFDescs->ppFieldDescList[0] : pByValueClass->GetApproxFieldDescListRaw();
                    BAD_FORMAT_NOTHROW_ASSERT(!enumField->IsStatic());   // no real static fields on enums
                    ElementType = enumField->GetFieldType();
                    BAD_FORMAT_NOTHROW_ASSERT(ElementType != ELEMENT_TYPE_VALUETYPE);
                    fIsByValue = FALSE; // we're going to treat it as the underlying type now
                    goto GOT_ELEMENT_TYPE;
                }

                // Check ByRefLike fields
                if (!IsSelfRef(pByValueClass) && pByValueClass->IsByRefLike())
                {
                    if (fIsStatic)
                    {
                        // Byref-like types cannot be used for static fields
                        BuildMethodTableThrowException(IDS_CLASSLOAD_BYREFLIKE_STATICFIELD);
                    }
                    if (!bmtFP->fIsByRefLikeType)
                    {
                        // Non-byref-like types cannot contain byref-like instance fields
                        BuildMethodTableThrowException(IDS_CLASSLOAD_BYREFLIKE_INSTANCEFIELD);
                    }
                }

                if (!IsSelfRef(pByValueClass) && pByValueClass->GetClass()->HasNonPublicFields())
                {   // If a class has a field of type ValueType with non-public fields in it,
                    // the class must "inherit" this characteristic
                    SetHasNonPublicFields();
                }

                if (!fHasRVA)
                {
                    if (!fIsStatic)
                    {
                        // Inherit instance attributes
                        EEClass * pFieldClass = pByValueClass->GetClass();

#ifdef FEATURE_64BIT_ALIGNMENT
                        // If a value type requires 8-byte alignment this requirement must be inherited by any
                        // class/struct that embeds it as a field.
                        if (pFieldClass->IsAlign8Candidate())
                            fFieldRequiresAlign8 = true;
#endif
                        if (pFieldClass->HasNonPublicFields())
                            SetHasNonPublicFields();
                        if (pFieldClass->HasFieldsWhichMustBeInited())
                            SetHasFieldsWhichMustBeInited();

#ifdef FEATURE_READYTORUN
                        if (!(pByValueClass->IsTruePrimitive() || pByValueClass->IsEnum()))
                        {
                            CheckLayoutDependsOnOtherModules(pByValueClass);
                        }
#endif
                    }
                    else
                    {   // Increment the number of static fields that contain object references.
                        bmtEnumFields->dwNumStaticBoxedFields++;
                        if (fIsThreadStatic)
                            bmtEnumFields->dwNumThreadStaticBoxedFields++;
                    }
                }

                if (*pByValueClassCache == NULL)
                {
                    DWORD dwNumFields = bmtEnumFields->dwNumInstanceFields + bmtEnumFields->dwNumStaticFields;

                    *pByValueClassCache = new (GetStackingAllocator()) MethodTable * [dwNumFields];
                    memset (*pByValueClassCache, 0, dwNumFields * sizeof(MethodTable **));
                }

                // Thread static fields come after instance fields and regular static fields in this list
                if (fIsThreadStatic)
                {
                    (*pByValueClassCache)[bmtEnumFields->dwNumInstanceFields + bmtEnumFields->dwNumStaticFields - bmtEnumFields->dwNumThreadStaticFields + dwCurrentThreadStaticField] = pByValueClass;
                    // make sure to record the correct size for static field
                    // layout
                    dwLog2FieldSize = LOG2_PTRSIZE; // handle
                }
                // Regular static fields come after instance fields in this list
                else if (fIsStatic)
                {
                    (*pByValueClassCache)[bmtEnumFields->dwNumInstanceFields + dwCurrentStaticField] = pByValueClass;
                    // make sure to record the correct size for static field
                    // layout
                    dwLog2FieldSize = LOG2_PTRSIZE; // handle
                }
                else
                {
                    (*pByValueClassCache)[dwCurrentDeclaredField] = pByValueClass;
                    dwLog2FieldSize = 0; // unused
                }

                break;
            }
        default:
            {
                BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD, mdTokenNil);
            }
        }

        if (!fIsStatic)
        {
            pFD = &pFieldDescList[dwCurrentDeclaredField];
            *totalDeclaredSize += (1 << dwLog2FieldSize);
        }
        else /* (dwMemberAttrs & mdStatic) */
        {
            if (fIsThreadStatic)
            {
                pFD = &pFieldDescList[bmtEnumFields->dwNumInstanceFields + bmtEnumFields->dwNumStaticFields - bmtEnumFields->dwNumThreadStaticFields + dwCurrentThreadStaticField];
            }
            else
            {
                pFD = &pFieldDescList[bmtEnumFields->dwNumInstanceFields + dwCurrentStaticField];
            }
        }

        bmtMFDescs->ppFieldDescList[i] = pFD;

        const LayoutRawFieldInfo *pLayoutFieldInfo = NULL;

        if (HasLayout())
        {
            const LayoutRawFieldInfo *pwalk = pLayoutRawFieldInfos;
            while (pwalk->m_MD != mdFieldDefNil)
            {
                if (pwalk->m_MD == bmtMetaData->pFields[i])
                {
                    pLayoutFieldInfo = pwalk;
                    break;
                }
                pwalk++;
            }
        }

        LPCSTR pszFieldName = NULL;
#ifdef _DEBUG
        if (FAILED(pInternalImport->GetNameOfFieldDef(bmtMetaData->pFields[i], &pszFieldName)))
        {
            pszFieldName = "Invalid FieldDef record";
        }
#endif
        // #InitCall Initialize contents of the field descriptor called from
        pFD->Init(
                  bmtMetaData->pFields[i],
                  FieldDescElementType,
                  dwMemberAttrs,
                  fIsStatic,
                  fHasRVA,
                  fIsThreadStatic,
                  pszFieldName
                  );

        // We're using FieldDesc::m_pMTOfEnclosingClass to temporarily store the field's size.
        //
        if (fIsByValue)
        {
            if (!fIsStatic &&
                (IsBlittable() || HasExplicitFieldOffsetLayout()))
            {
                (DWORD_PTR &)pFD->m_pMTOfEnclosingClass =
                    (*pByValueClassCache)[dwCurrentDeclaredField]->GetNumInstanceFieldBytes();

                if (pLayoutFieldInfo)
                    IfFailThrow(pFD->SetOffset(pLayoutFieldInfo->m_placement.m_offset));
                else
                    pFD->SetOffset(FIELD_OFFSET_VALUE_CLASS);
            }
            else if (!fIsStatic && IsManagedSequential())
            {
                (DWORD_PTR &)pFD->m_pMTOfEnclosingClass =
                    (*pByValueClassCache)[dwCurrentDeclaredField]->GetNumInstanceFieldBytes();

                IfFailThrow(pFD->SetOffset(pLayoutFieldInfo->m_placement.m_offset));
            }
            else
            {
                // static value class fields hold a handle, which is ptr sized
                // (instance field layout ignores this value)
                (DWORD_PTR&)(pFD->m_pMTOfEnclosingClass) = LOG2_PTRSIZE;
                pFD->SetOffset(FIELD_OFFSET_VALUE_CLASS);
            }
        }
        else
        {
            (DWORD_PTR &)(pFD->m_pMTOfEnclosingClass) = (size_t)dwLog2FieldSize;

            // -1 (FIELD_OFFSET_UNPLACED) means that this is a non-GC field that has not yet been placed
            // -2 (FIELD_OFFSET_UNPLACED_GC_PTR) means that this is a GC pointer field that has not yet been placed

            // If there is any kind of explicit layout information for this field, use it. If not, then
            // mark it as either GC or non-GC and as unplaced; it will get placed later on in an optimized way.

            if ((IsBlittable() || HasExplicitFieldOffsetLayout()) && !fIsStatic)
                IfFailThrow(pFD->SetOffset(pLayoutFieldInfo->m_placement.m_offset));
            else if (IsManagedSequential() && !fIsStatic)
                IfFailThrow(pFD->SetOffset(pLayoutFieldInfo->m_placement.m_offset));
            else if (bCurrentFieldIsGCPointer)
                pFD->SetOffset(FIELD_OFFSET_UNPLACED_GC_PTR);
            else
                pFD->SetOffset(FIELD_OFFSET_UNPLACED);
        }

        if (!fIsStatic)
        {
            if (!fIsByValue)
            {
                if (++bmtFP->NumInstanceFieldsOfSize[dwLog2FieldSize] == 1)
                    bmtFP->FirstInstanceFieldOfSize[dwLog2FieldSize] = dwCurrentDeclaredField;
            }

            dwCurrentDeclaredField++;

            if (bCurrentFieldIsGCPointer)
            {
                bmtFP->NumInstanceGCPointerFields++;
            }
        }
        else /* static fields */
        {
            // Static fields are stored in the vtable after the vtable and interface slots.  We don't
            // know how large the vtable will be, so we will have to fixup the slot number by
            // <vtable + interface size> later.

            if (fIsThreadStatic)
            {
                dwCurrentThreadStaticField++;
            }
            else
            {
                dwCurrentStaticField++;
            }

            if (fHasRVA)
            {
                if (FieldDescElementType == ELEMENT_TYPE_CLASS)
                {   // RVA fields are not allowed to have GC pointers.
                    BAD_FORMAT_NOTHROW_ASSERT(!"ObjectRef in an RVA field");
                    BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD, mdTokenNil);
                }
                if (FieldDescElementType == ELEMENT_TYPE_VALUETYPE)
                {
                    if (IsSelfRef(pByValueClass))
                    {   // We will verify self-referencing statics after the loop through all fields - see code:#SelfReferencingStaticValueTypeField_Checks
                        bmtFP->fHasSelfReferencingStaticValueTypeField_WithRVA = TRUE;
                    }
                    else
                    {
                        if (pByValueClass->GetClass()->HasFieldsWhichMustBeInited())
                        {   // RVA fields are not allowed to have GC pointers.
                            BAD_FORMAT_NOTHROW_ASSERT(!"ObjectRef in an RVA field");
                            BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD, mdTokenNil);
                        }
                    }
                }

                // Set the field offset
                DWORD rva;
                IfFailThrow(pInternalImport->GetFieldRVA(pFD->GetMemberDef(), &rva));

                // The PE should be loaded by now.
                _ASSERT(GetModule()->GetPEAssembly()->IsLoaded());

                DWORD fldSize;
                if (FieldDescElementType == ELEMENT_TYPE_VALUETYPE)
                {
                    if (IsSelfRef(pByValueClass))
                    {
                        _ASSERTE(bmtFP->fHasSelfReferencingStaticValueTypeField_WithRVA);

                        // We do not known the size yet
                        _ASSERTE(bmtFP->NumInstanceFieldBytes == 0);
                        // We will check just the RVA with size 0 now, the full size verification will happen in code:VerifySelfReferencingStaticValueTypeFields_WithRVA
                        fldSize = 0;
                    }
                    else
                    {
                        fldSize = pByValueClass->GetNumInstanceFieldBytes();
                    }
                }
                else
                {
                    fldSize = GetSizeForCorElementType(FieldDescElementType);
                }

                pFD->SetOffsetRVA(rva);
            }
            else if (fIsThreadStatic)
            {
                bmtFP->NumThreadStaticFieldsOfSize[dwLog2FieldSize]++;

                if (bCurrentFieldIsGCPointer)
                    bmtFP->NumThreadStaticGCPointerFields++;

                if (fIsByValue)
                    bmtFP->NumThreadStaticGCBoxedFields++;
            }
            else
            {
                bmtFP->NumRegularStaticFieldsOfSize[dwLog2FieldSize]++;

                if (bCurrentFieldIsGCPointer)
                    bmtFP->NumRegularStaticGCPointerFields++;

                if (fIsByValue)
                    bmtFP->NumRegularStaticGCBoxedFields++;
            }
        }
    }
    // We processed all fields

    //#SelfReferencingStaticValueTypeField_Checks
    if (bmtFP->fHasSelfReferencingStaticValueTypeField_WithRVA)
    {   // The type has self-referencing static ValueType field with RVA, do more checks now that depend on all fields being processed

        // For enums we already checked its underlying type, we should not get here
        _ASSERTE(!IsEnum());

        if (HasFieldsWhichMustBeInited())
        {   // RVA fields are not allowed to have GC pointers.
            BAD_FORMAT_NOTHROW_ASSERT(!"ObjectRef in an RVA self-referencing static field");
            BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD, mdTokenNil);
        }
    }

    DWORD dwNumInstanceFields = dwCurrentDeclaredField + (HasParent() ? GetParentMethodTable()->GetNumInstanceFields() : 0);
    DWORD dwNumStaticFields = bmtEnumFields->dwNumStaticFields;
    DWORD dwNumThreadStaticFields = bmtEnumFields->dwNumThreadStaticFields;

    if (!FitsIn<WORD>(dwNumInstanceFields) ||
        !FitsIn<WORD>(dwNumStaticFields))
    {   // An implementation limitation means that it's an error if there are greater that MAX_WORD fields.
        BuildMethodTableThrowException(IDS_EE_TOOMANYFIELDS);
    }

    GetHalfBakedClass()->SetNumInstanceFields((WORD)dwNumInstanceFields);
    GetHalfBakedClass()->SetNumStaticFields((WORD)dwNumStaticFields);
    GetHalfBakedClass()->SetNumThreadStaticFields((WORD)dwNumThreadStaticFields);

    if (bmtFP->fHasFixedAddressValueTypes)
    {
        // To make things simpler, if the class has any field with this requirement, we'll set
        // all the statics to have this property. This allows us to only need to persist one bit
        // for the ngen case.
        GetHalfBakedClass()->SetHasFixedAddressVTStatics();
    }

#ifdef FEATURE_64BIT_ALIGNMENT
    // For types with layout we drop any 64-bit alignment requirement if the packing size was less than 8
    // bytes (this mimics what the native compiler does and ensures we match up calling conventions during
    // interop).
    // We don't do this for types that are marked as sequential but end up with auto-layout due to containing pointers,
    // as auto-layout ignores any Pack directives.
    if (HasLayout() && (HasExplicitFieldOffsetLayout() || IsManagedSequential()) && GetLayoutInfo()->GetPackingSize() < 8)
    {
        fFieldRequiresAlign8 = false;
    }

    if (fFieldRequiresAlign8)
    {
        SetAlign8Candidate();
    }
#endif // FEATURE_64BIT_ALIGNMENT

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    if (ShouldAlign8(dwR8Fields, dwNumInstanceFields))
    {
        SetAlign8Candidate();
    }
#endif // FEATURE_DOUBLE_ALIGNMENT_HINT


    //========================================================================
    // END:
    //    Go thru all fields and initialize their FieldDescs.
    //========================================================================

    return;
} // MethodTableBuilder::InitializeFieldDescs

#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*******************************************************************************
// Verify self-referencing static ValueType fields with RVA (when the size of the ValueType is known).
void
MethodTableBuilder::VerifySelfReferencingStaticValueTypeFields_WithRVA(
    MethodTable ** pByValueClassCache)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(bmtFP->fHasSelfReferencingStaticValueTypeField_WithRVA);
    // Enum's static self-referencing fields have been verified as the underlying type of the enum, we should not get here for them
    _ASSERTE(!IsEnum());
    // The size of the ValueType should be known at this point (the caller throws if it is 0)
    _ASSERTE(bmtFP->NumInstanceFieldBytes != 0);

    FieldDesc * pFieldDescList = GetApproxFieldDescListRaw();
    DWORD nFirstThreadStaticFieldIndex = bmtEnumFields->dwNumInstanceFields + bmtEnumFields->dwNumStaticFields - bmtEnumFields->dwNumThreadStaticFields;
    for (DWORD i = bmtEnumFields->dwNumInstanceFields; i < nFirstThreadStaticFieldIndex; i++)
    {
        FieldDesc * pFD = &pFieldDescList[i];
        _ASSERTE(pFD->IsStatic());

        if (pFD->IsRVA() && pFD->IsByValue())
        {
            _ASSERTE(pByValueClassCache[i] != NULL);

            if (IsSelfRef(pByValueClassCache[i]))
            {
                DWORD rva;
                IfFailThrow(GetMDImport()->GetFieldRVA(pFD->GetMemberDef(), &rva));
            }
        }
    }
} // MethodTableBuilder::VerifySelfReferencingStaticValueTypeFields_WithRVA

//*******************************************************************************
// Returns true if hEnclosingTypeCandidate encloses, at any arbitrary depth,
// hNestedTypeCandidate; returns false otherwise.

bool MethodTableBuilder::IsEnclosingNestedTypePair(
    bmtTypeHandle hEnclosingTypeCandidate,
    bmtTypeHandle hNestedTypeCandidate)
{
    STANDARD_VM_CONTRACT;

    CONSISTENCY_CHECK(!hEnclosingTypeCandidate.IsNull());
    CONSISTENCY_CHECK(!hNestedTypeCandidate.IsNull());
    CONSISTENCY_CHECK(!bmtTypeHandle::Equal(hEnclosingTypeCandidate, hNestedTypeCandidate));

    Module * pModule = hEnclosingTypeCandidate.GetModule();

    if (pModule != hNestedTypeCandidate.GetModule())
    {   // If the modules aren't the same, then there's no way
        // hBase could be an enclosing type of hChild. We make
        // this check early so that the code can deal with only
        // one Module and IMDInternalImport instance and can avoid
        // extra checks.
        return false;
    }

    IMDInternalImport * pMDImport = pModule->GetMDImport();

    mdTypeDef tkEncl = hEnclosingTypeCandidate.GetTypeDefToken();
    mdTypeDef tkNest = hNestedTypeCandidate.GetTypeDefToken();

    while (tkEncl != tkNest)
    {   // Do this using the metadata APIs because MethodTableBuilder does
        // not construct type representations for enclosing type chains.
        if (FAILED(pMDImport->GetNestedClassProps(tkNest, &tkNest)))
        {   // tokNest is not a nested type.
            return false;
        }
    }

    // tkNest's enclosing type is tkEncl, so we've shown that
    // hEnclosingTypeCandidate encloses hNestedTypeCandidate
    return true;
}

//*******************************************************************************
// Given an arbitrary nesting+subclassing pattern like this:
//
// class C1 {
//     private virtual void Foo() { ... }
//     class C2 : C1 {
//       ...
//         class CN : CN-1 {
//             private override void Foo() { ... }
//         }
//       ...
//     }
// }
//
// this method will return true, where hChild == N and hBase == C1
//
// Note that there is no requirement that a type derive from its immediately
// enclosing type, but can skip a level, such as this example:
//
// class A
// {
//     private virtual void Foo() { }
//     public class B
//     {
//         public class C : A
//         {
//             private override void Foo() { }
//         }
//     }
// }
//
// NOTE: IMPORTANT: This code assumes that hBase is indeed a base type of hChild,
//                  and behaviour is undefined if this is not the case.

bool MethodTableBuilder::IsBaseTypeAlsoEnclosingType(
    bmtTypeHandle hBase,
    bmtTypeHandle hChild)
{
    STANDARD_VM_CONTRACT;

    CONSISTENCY_CHECK(!hBase.IsNull());
    CONSISTENCY_CHECK(!hChild.IsNull());
    CONSISTENCY_CHECK(!bmtTypeHandle::Equal(hBase, hChild));

    // The idea of this algorithm is that if we climb the inheritance chain
    // starting at hChild then we'll eventually hit hBase. If we check that
    // for every (hParent, hChild) pair in the chain that hParent encloses
    // hChild, then we've shown that hBase encloses hChild.

    while (!bmtTypeHandle::Equal(hBase, hChild))
    {
        CONSISTENCY_CHECK(!hChild.GetParentType().IsNull());
        bmtTypeHandle hParent(hChild.GetParentType());

        if (!IsEnclosingNestedTypePair(hParent, hChild))
        {   // First, the parent type must enclose the child type.
            // If this is not the case we fail immediately.
            return false;
        }

        // Move up one in the inheritance chain, and try again.
        hChild = hParent;
    }

    // If the loop worked itself from the original hChild all the way
    // up to hBase, then we know that for every (hParent, hChild)
    // pair in the chain that hParent enclosed hChild, and so we know
    // that hBase encloses the original hChild
    return true;
}

//*******************************************************************************
BOOL MethodTableBuilder::TestOverrideForAccessibility(
    bmtMethodHandle hParentMethod,
    bmtTypeHandle   hChildType)
{
    STANDARD_VM_CONTRACT;

    bmtTypeHandle hParentType(hParentMethod.GetOwningType());

    Module * pParentModule = hParentType.GetModule();
    Module * pChildModule = hChildType.GetModule();

    Assembly * pParentAssembly = pParentModule->GetAssembly();
    Assembly * pChildAssembly = pChildModule->GetAssembly();

    BOOL isSameAssembly = (pChildAssembly == pParentAssembly);

    DWORD dwParentAttrs = hParentMethod.GetDeclAttrs();

    // AKA "strict bit". This means that overridability is tightly bound to accessibility.
    if (IsMdCheckAccessOnOverride(dwParentAttrs))
    {
        // Same Assembly
        if (isSameAssembly || pParentAssembly->GrantsFriendAccessTo(pChildAssembly, hParentMethod.GetMethodDesc())
            || pChildAssembly->IgnoresAccessChecksTo(pParentAssembly))
        {
            // Can always override any method that has accessibility greater than mdPrivate
            if ((dwParentAttrs & mdMemberAccessMask) > mdPrivate)
            {   // Fall through
            }
            // Generally, types cannot override inherited mdPrivate methods, except:
            // Types can access enclosing type's private members, so it can
            // override them if the nested type extends its enclosing type.
            else if ((dwParentAttrs & mdMemberAccessMask) == mdPrivate &&
                     IsBaseTypeAlsoEnclosingType(hParentType, hChildType))
            {   // Fall through
            }
            else
            {
                return FALSE;
            }
        }
        // Cross-Assembly
        else
        {
            // If the method marks itself as check visibility the method must be
            // public, FamORAssem, or family
            if((dwParentAttrs & mdMemberAccessMask) <= mdAssem)
            {
                return FALSE;
            }
        }
    }
    return TRUE;
}

//*******************************************************************************
VOID MethodTableBuilder::TestOverRide(bmtMethodHandle hParentMethod,
                                      bmtMethodHandle hChildMethod)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(IsMdVirtual(hParentMethod.GetDeclAttrs()));
        PRECONDITION(IsMdVirtual(hChildMethod.GetDeclAttrs()));
    } CONTRACTL_END;

    DWORD dwAttrs = hChildMethod.GetDeclAttrs();
    DWORD dwParentAttrs = hParentMethod.GetDeclAttrs();

    Module *pModule = hChildMethod.GetOwningType().GetModule();
    Module *pParentModule = hParentMethod.GetOwningType().GetModule();

    Assembly *pAssembly = pModule->GetAssembly();
    Assembly *pParentAssembly = pParentModule->GetAssembly();

    BOOL isSameModule = (pModule == pParentModule);
    BOOL isSameAssembly = (pAssembly == pParentAssembly);

    if (!TestOverrideForAccessibility(hParentMethod, hChildMethod.GetOwningType()))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_ACCESS_FAILURE, hChildMethod.GetMethodSignature().GetToken());
    }

    //
    // Refer to Partition II, 9.3.3 for more information on what is permitted.
    //

    enum WIDENING_STATUS
    {
        e_NO,       // NO
        e_YES,      // YES
        e_SA,       // YES, but only when same assembly
        e_NSA,      // YES, but only when NOT same assembly
        e_SM,       // YES, but only when same module
    };

    static_assert_no_msg(mdPrivateScope == 0x00);
    static_assert_no_msg(mdPrivate      == 0x01);
    static_assert_no_msg(mdFamANDAssem  == 0x02);
    static_assert_no_msg(mdAssem        == 0x03);
    static_assert_no_msg(mdFamily       == 0x04);
    static_assert_no_msg(mdFamORAssem   == 0x05);
    static_assert_no_msg(mdPublic       == 0x06);

    static const DWORD dwCount = mdPublic - mdPrivateScope + 1;
    static const WIDENING_STATUS rgWideningTable[dwCount][dwCount] =

    //               |        Base type
    // Subtype       |        mdPrivateScope  mdPrivate   mdFamANDAssem   mdAssem     mdFamily    mdFamORAssem    mdPublic
    // --------------+-------------------------------------------------------------------------------------------------------
    /*mdPrivateScope | */ { { e_SM,           e_NO,       e_NO,           e_NO,       e_NO,       e_NO,           e_NO    },
    /*mdPrivate      | */   { e_SM,           e_YES,      e_NO,           e_NO,       e_NO,       e_NO,           e_NO    },
    /*mdFamANDAssem  | */   { e_SM,           e_YES,      e_SA,           e_NO,       e_NO,       e_NO,           e_NO    },
    /*mdAssem        | */   { e_SM,           e_YES,      e_SA,           e_SA,       e_NO,       e_NO,           e_NO    },
    /*mdFamily       | */   { e_SM,           e_YES,      e_YES,          e_NO,       e_YES,      e_NSA,          e_NO    },
    /*mdFamORAssem   | */   { e_SM,           e_YES,      e_YES,          e_SA,       e_YES,      e_YES,          e_NO    },
    /*mdPublic       | */   { e_SM,           e_YES,      e_YES,          e_YES,      e_YES,      e_YES,          e_YES   } };

    DWORD idxParent = (dwParentAttrs & mdMemberAccessMask) - mdPrivateScope;
    DWORD idxMember = (dwAttrs & mdMemberAccessMask) - mdPrivateScope;
    CONSISTENCY_CHECK(idxParent < dwCount);
    CONSISTENCY_CHECK(idxMember < dwCount);

    WIDENING_STATUS entry = rgWideningTable[idxMember][idxParent];

    if (entry == e_NO ||
        (entry == e_SA && !isSameAssembly && !pParentAssembly->GrantsFriendAccessTo(pAssembly, hParentMethod.GetMethodDesc())
         && !pAssembly->IgnoresAccessChecksTo(pParentAssembly)) ||
        (entry == e_NSA && isSameAssembly) ||
        (entry == e_SM && !isSameModule)
       )
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_REDUCEACCESS, hChildMethod.GetMethodSignature().GetToken());
    }

    return;
}

//*******************************************************************************
VOID MethodTableBuilder::TestMethodImpl(
    bmtMethodHandle hDeclMethod,
    bmtMethodHandle hImplMethod)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!hDeclMethod.IsNull());
        PRECONDITION(!hImplMethod.IsNull());
    }
    CONTRACTL_END

    Module * pDeclModule = hDeclMethod.GetOwningType().GetModule();
    Module * pImplModule = hImplMethod.GetOwningType().GetModule();

    mdTypeDef tokDecl = hDeclMethod.GetMethodSignature().GetToken();
    mdTypeDef tokImpl = hImplMethod.GetMethodSignature().GetToken();

    BOOL isSameModule = pDeclModule->Equals(pImplModule);

    IMDInternalImport *pIMDDecl = pDeclModule->GetMDImport();
    IMDInternalImport *pIMDImpl = pImplModule->GetMDImport();

    DWORD dwDeclAttrs;
    if (FAILED(pIMDDecl->GetMethodDefProps(tokDecl, &dwDeclAttrs)))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
    }
    DWORD dwImplAttrs;
    if (FAILED(pIMDImpl->GetMethodDefProps(tokImpl, &dwImplAttrs)))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
    }

    HRESULT hr = COR_E_TYPELOAD;

    if (!IsMdVirtual(dwDeclAttrs))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_NONVIRTUAL_DECL);
    }
    if ((IsMdVirtual(dwImplAttrs) && IsMdStatic(dwImplAttrs)) || (!IsMdVirtual(dwImplAttrs) && !IsMdStatic(dwImplAttrs)))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MUSTBEVIRTUAL);
    }
    // Virtual methods on classes/valuetypes cannot be static
    if (IsMdStatic(dwDeclAttrs) && !hDeclMethod.GetOwningType().IsInterface())
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_STATICVIRTUAL);
    }
    if ((!!IsMdStatic(dwImplAttrs)) != (!!IsMdStatic(dwDeclAttrs)))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_STATICVIRTUAL);
    }
    if (IsMdFinal(dwDeclAttrs))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_FINAL_DECL);
    }

    // Non-static interface method body that has methodimpl should always be final
    if (IsInterface() && !IsMdStatic(dwDeclAttrs) && !IsMdFinal(dwImplAttrs))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_FINAL_IMPL);
    }

    // Since MethodImpl's do not affect the visibility of the Decl method, there's
    // no need to check.

    // If Decl's parent is other than this class, Decl must not be private
    mdTypeDef tkImplParent = mdTypeDefNil;
    mdTypeDef tkDeclParent = mdTypeDefNil;

    if (FAILED(hr = pIMDDecl->GetParentToken(tokDecl, &tkDeclParent)))
    {
        BuildMethodTableThrowException(hr, *bmtError);
    }
    if (FAILED(hr = pIMDImpl->GetParentToken(tokImpl, &tkImplParent)))
    {
        BuildMethodTableThrowException(hr, *bmtError);
    }

    // Make sure that we test for accessibility restrictions only if the decl is
    // not within our own type, as we are allowed to methodImpl a private with the
    // strict bit set if it is in our own type.
    if (!isSameModule || tkDeclParent != tkImplParent)
    {
        if (!TestOverrideForAccessibility(hDeclMethod, hImplMethod.GetOwningType()))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_MI_ACCESS_FAILURE, tokImpl);
        }

        // Decl's parent must not be tdSealed
        mdToken tkGrandParentDummyVar;
        DWORD dwDeclTypeAttrs;
        if (FAILED(hr = pIMDDecl->GetTypeDefProps(tkDeclParent, &dwDeclTypeAttrs, &tkGrandParentDummyVar)))
        {
            BuildMethodTableThrowException(hr, *bmtError);
        }
        if (IsTdSealed(dwDeclTypeAttrs))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_MI_SEALED_DECL);
        }
    }

    return;
}


//*******************************************************************************
//
// Used by BuildMethodTable
//
VOID
MethodTableBuilder::ValidateMethods()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtInternal));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtError));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtMethodImpl));
        PRECONDITION(CheckPointer(bmtVT));
    }
    CONTRACTL_END;

    // Used to keep track of located default and type constructors.
    CONSISTENCY_CHECK(bmtVT->pCCtor == NULL);
    CONSISTENCY_CHECK(bmtVT->pDefaultCtor == NULL);

    // Fetch the hard-coded signatures for the type constructor and the
    // default constructor and create MethodSignature objects for both at
    // the method level so this does not happen for every specialname
    // method.

    Signature sig;

    sig = CoreLibBinder::GetSignature(&gsig_SM_RetVoid);

    MethodSignature cctorSig(CoreLibBinder::GetModule(),
                             COR_CCTOR_METHOD_NAME,
                             sig.GetRawSig(), sig.GetRawSigLen());

    sig = CoreLibBinder::GetSignature(&gsig_IM_RetVoid);

    MethodSignature defaultCtorSig(CoreLibBinder::GetModule(),
                                   COR_CTOR_METHOD_NAME,
                                   sig.GetRawSig(), sig.GetRawSigLen());

    Module * pModule = GetModule();
    DeclaredMethodIterator it(*this);
    while (it.Next())
    {
        // The RVA is only valid/testable if it has not been overwritten
        // for something like edit-and-continue
        // Complete validation of non-zero RVAs is done later inside MethodDesc::GetILHeader.
        if ((it.RVA() == 0) && (pModule->GetDynamicIL(it.Token(), FALSE) == NULL))
        {
            // for IL code that is implemented here must have a valid code RVA
            // this came up due to a linker bug where the ImplFlags/DescrOffset were
            // being set to null and we weren't coping with it
            if((IsMiIL(it.ImplFlags()) || IsMiOPTIL(it.ImplFlags())) &&
                   !IsMdAbstract(it.Attrs()) &&
                   !IsReallyMdPinvokeImpl(it.Attrs()) &&
                !IsMiInternalCall(it.ImplFlags()))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_MISSINGMETHODRVA, it.Token());
            }
        }

        if (IsMdRTSpecialName(it.Attrs()))
        {
            if (IsMdVirtual(it.Attrs()))
            {   // Virtual specialname methods are illegal
                BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
            }

            // Constructors (.ctor) and class initialisers (.cctor) are special
            const MethodSignature &curSig(it->GetMethodSignature());

            if (IsMdStatic(it.Attrs()))
            {   // The only rtSpecialName static method allowed is the .cctor
                if (!curSig.ExactlyEqual(cctorSig))
                {   // Bad method
                    BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
                }

                // Remember it for later
                bmtVT->pCCtor = *it;
            }
            else
            {
                if(!MethodSignature::NamesEqual(curSig, defaultCtorSig))
                {   // The only rtSpecialName instance methods allowed are .ctors
                    BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
                }

                // .ctor must return void
                MetaSig methodMetaSig(curSig.GetSignature(),
                                        static_cast<DWORD>(curSig.GetSignatureLength()),
                                        curSig.GetModule(),
                                        NULL);

                if (methodMetaSig.GetReturnType() != ELEMENT_TYPE_VOID)
                {   // All constructors must have a void return type
                    BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
                }

                // See if this is a default constructor.  If so, remember it for later.
                if (curSig.ExactlyEqual(defaultCtorSig))
                {
                    bmtVT->pDefaultCtor = *it;
                }
            }
        }

        // Make sure that fcalls have a 0 rva.  This is assumed by the prejit fixup logic
        if (it.MethodType() == METHOD_TYPE_FCALL && it.RVA() != 0)
        {
            BuildMethodTableThrowException(BFA_ECALLS_MUST_HAVE_ZERO_RVA, it.Token());
        }

        // check for proper use of the Managed and native flags
        if (IsMiManaged(it.ImplFlags()))
        {
            if (IsMiIL(it.ImplFlags()) || IsMiRuntime(it.ImplFlags())) // IsMiOPTIL(it.ImplFlags()) no longer supported
            {
                // No need to set code address, pre stub used automatically.
            }
            else
            {
                if (IsMiNative(it.ImplFlags()))
                {
                    // For now simply disallow managed native code if you turn this on you have to at least
                    // insure that we have SkipVerificationPermission or equivalent
                    BuildMethodTableThrowException(BFA_MANAGED_NATIVE_NYI, it.Token());
                }
                else
                {
                    BuildMethodTableThrowException(BFA_BAD_IMPL_FLAGS, it.Token());
                }
            }
        }
        else
        {
            if (IsMiNative(it.ImplFlags()) && IsGlobalClass())
            {
                // global function unmanaged entrypoint via IJW thunk was handled
                // above.
            }
            else
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BAD_UNMANAGED_RVA, it.Token());
            }
            if (it.MethodType() != METHOD_TYPE_NDIRECT)
            {
                BuildMethodTableThrowException(BFA_BAD_UNMANAGED_ENTRY_POINT);
            }
        }

        // Vararg methods are not allowed inside generic classes
        // and nor can they be generic methods.
        if (bmtGenerics->GetNumGenericArgs() > 0 || (it.MethodType() == METHOD_TYPE_INSTANTIATED) )
        {
            DWORD cMemberSignature;
            PCCOR_SIGNATURE pMemberSignature = it.GetSig(&cMemberSignature);
            // We've been trying to avoid asking for the signature - now we need it
            if (pMemberSignature == NULL)
            {
                pMemberSignature = it.GetSig(&cMemberSignature);
            }

            if (MetaSig::IsVarArg(Signature(pMemberSignature, cMemberSignature)))
            {
                BuildMethodTableThrowException(BFA_GENCODE_NOT_BE_VARARG);
            }
        }

        if (IsMdVirtual(it.Attrs()) && IsMdPublic(it.Attrs()) && it.Name() == NULL)
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_NOMETHOD_NAME);
        }

        if (it.IsMethodImpl())
        {
            if (!IsMdVirtual(it.Attrs()) && !IsMdStatic(it.Attrs()))
            {
                // Non-virtual methods may only participate in a methodImpl pair when
                // they are static and they implement a virtual static interface method.
                BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MUSTBEVIRTUAL, it.Token());
            }
        }

        // Virtual static methods are only allowed on interfaces and they must be abstract.
        if (IsMdStatic(it.Attrs()) && IsMdVirtual(it.Attrs()))
        {
            if (!IsInterface())
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_STATICVIRTUAL, it.Token());
            }
        }
    }
}

//*******************************************************************************
// Essentially, this is a helper method that combines calls to InitMethodDesc and
// SetSecurityFlagsOnMethod. It then assigns the newly initialized MethodDesc to
// the bmtMDMethod.
VOID
MethodTableBuilder::InitNewMethodDesc(
    bmtMDMethod * pMethod,
    MethodDesc * pNewMD)
{
    STANDARD_VM_CONTRACT;

    //
    // First, set all flags that control layout of optional slots
    //
    pNewMD->SetClassification(GetMethodClassification(pMethod->GetMethodType()));

    if (pMethod->GetMethodImplType() == METHOD_IMPL)
        pNewMD->SetHasMethodImplSlot();

    if (pMethod->GetSlotIndex() >= bmtVT->cVtableSlots)
        pNewMD->SetHasNonVtableSlot();

    if (NeedsNativeCodeSlot(pMethod))
        pNewMD->SetHasNativeCodeSlot();

    // Now we know the classification we can allocate the correct type of
    // method desc and perform any classification specific initialization.

    LPCSTR pName = pMethod->GetMethodSignature().GetName();
    if (pName == NULL)
    {
        if (FAILED(GetMDImport()->GetNameOfMethodDef(pMethod->GetMethodSignature().GetToken(), &pName)))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
        }
    }

#ifdef _DEBUG
    LPCUTF8 pszDebugMethodName;
    if (FAILED(GetMDImport()->GetNameOfMethodDef(pMethod->GetMethodSignature().GetToken(), &pszDebugMethodName)))
    {
        pszDebugMethodName = "Invalid MethodDef record";
    }
    S_SIZE_T safeLen = S_SIZE_T(strlen(pszDebugMethodName)) + S_SIZE_T(1);
    if(safeLen.IsOverflow())
        COMPlusThrowHR(COR_E_OVERFLOW);

    size_t len = safeLen.Value();
    LPCUTF8 pszDebugMethodNameCopy = (char*) AllocateFromLowFrequencyHeap(safeLen);
    strcpy_s((char *) pszDebugMethodNameCopy, len, pszDebugMethodName);
#endif // _DEBUG

    // Do the init specific to each classification of MethodDesc & assign some common fields
    InitMethodDesc(pNewMD,
                   GetMethodClassification(pMethod->GetMethodType()),
                   pMethod->GetMethodSignature().GetToken(),
                   pMethod->GetImplAttrs(),
                   pMethod->GetDeclAttrs(),
                   FALSE,
                   pMethod->GetRVA(),
                   GetMDImport(),
                   pName
                   COMMA_INDEBUG(pszDebugMethodNameCopy)
                   COMMA_INDEBUG(GetDebugClassName())
                   COMMA_INDEBUG("") // FIX this happens on global methods, give better info
                  );

    pMethod->SetMethodDesc(pNewMD);

    bmtRTMethod * pParentMethod = NULL;

    if (HasParent())
    {
        SLOT_INDEX idx = pMethod->GetSlotIndex();
        CONSISTENCY_CHECK(idx != INVALID_SLOT_INDEX);

        if (idx < GetParentMethodTable()->GetNumVirtuals())
        {
            pParentMethod = (*bmtParent->pSlotTable)[idx].Decl().AsRTMethod();
        }
    }

    // Turn off inlining for any calls
    // that are marked in the metadata as not being inlineable.
    if(IsMiNoInlining(pMethod->GetImplAttrs()))
    {
        pNewMD->SetNotInline(true);
    }

    // Check for methods marked as [Intrinsic]
    if (GetModule()->IsSystem())
    {
        if (bmtProp->fIsHardwareIntrinsic || (S_OK == GetCustomAttribute(pMethod->GetMethodSignature().GetToken(),
                                                    WellKnownAttribute::Intrinsic,
                                                    NULL,
                                                    NULL)))
        {
            pNewMD->SetIsIntrinsic();
        }

    }

    pNewMD->SetSlot(pMethod->GetSlotIndex());
}

//*******************************************************************************
// Determine vtable placement for each non-virtual in the class, while also
// looking for default and type constructors.
VOID
MethodTableBuilder::PlaceNonVirtualMethods()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtInternal));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtError));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtMethodImpl));
        PRECONDITION(CheckPointer(bmtVT));
    }
    CONTRACTL_END;

    INDEBUG(bmtVT->SealVirtualSlotSection();)

    //
    // For each non-virtual method, place the method in the next available non-virtual method slot.
    //

    // Place the cctor and default ctor first. code::MethodTableGetCCtorSlot and code:MethodTable::GetDefaultCtorSlot
    // depends on this.
    if (bmtVT->pCCtor != NULL)
    {
        if (!bmtVT->AddNonVirtualMethod(bmtVT->pCCtor))
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
    }

    if (bmtVT->pDefaultCtor != NULL)
    {
        if (!bmtVT->AddNonVirtualMethod(bmtVT->pDefaultCtor))
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
    }

    // We use slot during remoting and to map methods between generic instantiations
    // (see MethodTable::GetParallelMethodDesc). The current implementation
    // of this mechanism requires real slots.
    BOOL fCanHaveNonVtableSlots = (bmtGenerics->GetNumGenericArgs() == 0) && !IsInterface();

    // Flag to avoid second pass when possible
    BOOL fHasNonVtableSlots = FALSE;

    //
    // Place all methods that require real vtable slot first. This is necessary so
    // that they get consequitive slot numbers right after virtual slots.
    //

    DeclaredMethodIterator it(*this);
    while (it.Next())
    {
        // Skip methods that are placed already
        if (it->GetSlotIndex() != INVALID_SLOT_INDEX)
            continue;

#ifdef _DEBUG
        if(GetHalfBakedClass()->m_fDebuggingClass && g_pConfig->ShouldBreakOnMethod(it.Name()))
            CONSISTENCY_CHECK_MSGF(false, ("BreakOnMethodName: '%s' ", it.Name()));
#endif // _DEBUG

        if (!fCanHaveNonVtableSlots ||
            it->GetMethodType() == METHOD_TYPE_INSTANTIATED)
        {
            // We use slot during remoting and to map methods between generic instantiations
            // (see MethodTable::GetParallelMethodDesc). The current implementation
            // of this mechanism requires real slots.
        }
        else
        {
            // This method does not need real vtable slot
            fHasNonVtableSlots = TRUE;
            continue;
        }

        // This will update slot index in bmtMDMethod
        if (!bmtVT->AddNonVirtualMethod(*it))
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
    }

    // Remeber last real vtable slot
    bmtVT->cVtableSlots = bmtVT->cTotalSlots;

    // Are there any Non-vtable slots to place?
    if (!fHasNonVtableSlots)
        return;

    //
    // Now, place the remaining methods. They will get non-vtable slot.
    //

    DeclaredMethodIterator it2(*this);
    while (it2.Next())
    {
        // Skip methods that are placed already
        if (it2->GetSlotIndex() != INVALID_SLOT_INDEX)
            continue;

        if (!bmtVT->AddNonVirtualMethod(*it2))
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
    }

}

//*******************************************************************************
// Determine vtable placement for each virtual member in this class.
VOID
MethodTableBuilder::PlaceVirtualMethods()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtInternal));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtError));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtMethodImpl));
        PRECONDITION(CheckPointer(bmtVT));
    }
    CONTRACTL_END;

#ifdef _DEBUG
    LPCUTF8 pszDebugName, pszDebugNamespace;
    if (FAILED(GetMDImport()->GetNameOfTypeDef(GetCl(), &pszDebugName, &pszDebugNamespace)))
    {
        pszDebugName = pszDebugNamespace = "Invalid TypeDef record";
    }
#endif // _DEBUG

    //
    // For each virtual method
    //  - If the method is not declared as newslot, search all virtual methods in the parent
    //    type for an override candidate.
    //      - If such a candidate is found, test to see if the override is valid. If
    //        the override is not valid, throw TypeLoadException
    //  - If a candidate is found above, place the method in the inherited slot as both
    //    the Decl and the Impl.
    //  - Else, place the method in the next available empty vtable slot.
    //

    DeclaredMethodIterator it(*this);
    while (it.Next())
    {
        if (!IsMdVirtual(it.Attrs()) || IsMdStatic(it.Attrs()))
        {   // Only processing declared virtual instance methods
            continue;
        }

#ifdef _DEBUG
        if(GetHalfBakedClass()->m_fDebuggingClass && g_pConfig->ShouldBreakOnMethod(it.Name()))
            CONSISTENCY_CHECK_MSGF(false, ("BreakOnMethodName: '%s' ", it.Name()));
#endif // _DEBUG

        // If this member is a method which overrides a parent method, it will be set to non-NULL
        bmtRTMethod * pParentMethod = NULL;

        // Hash that a method with this name exists in this class
        // Note that ctors and static ctors are not added to the table
        BOOL fMethodConstraintsMatch = FALSE;

        // If the member is marked with a new slot we do not need to find it in the parent
        if (HasParent() && !IsMdNewSlot(it.Attrs()))
        {
            // Attempt to find the method with this name and signature in the parent class.
            // This method may or may not create pParentMethodHash (if it does not already exist).
            // It also may or may not fill in pMemberSignature/cMemberSignature.
            // An error is only returned when we can not create the hash.
            // NOTE: This operation touches metadata
            pParentMethod = LoaderFindMethodInParentClass(
                it->GetMethodSignature(), bmtProp->fNoSanityChecks ? NULL : &fMethodConstraintsMatch);

            if (pParentMethod != NULL)
            {   // Found an override candidate
                DWORD dwParentAttrs = pParentMethod->GetDeclAttrs();

                if (!IsMdVirtual(dwParentAttrs))
                {   // Can't override a non-virtual methods
                    BuildMethodTableThrowException(BFA_NONVIRT_NO_SEARCH, it.Token());
                }

                if(IsMdFinal(dwParentAttrs))
                {   // Can't override a final methods
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_FINAL_DECL, it.Token());
                }

                if(!bmtProp->fNoSanityChecks)
                {
                    TestOverRide(bmtMethodHandle(pParentMethod),
                                 bmtMethodHandle(*it));

                    if (!fMethodConstraintsMatch)
                    {
                        BuildMethodTableThrowException(
                                IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_IMPLICIT_OVERRIDE,
                                it.Token());
                    }
                }
            }
        }

        // vtable method
        if (IsInterface())
        {
            CONSISTENCY_CHECK(pParentMethod == NULL);
            // Also sets new slot number on bmtRTMethod and MethodDesc
            if (!bmtVT->AddVirtualMethod(*it))
                BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
        }
        else if (pParentMethod != NULL)
        {
            bmtVT->SetVirtualMethodOverride(pParentMethod->GetSlotIndex(), *it);
        }
        else
        {
            if (!bmtVT->AddVirtualMethod(*it))
                BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);
        }
    }
}

// Given an interface map entry, and a name+signature, compute the method on the interface
// that the name+signature corresponds to. Used by ProcessMethodImpls and ProcessInexactMethodImpls
// Always returns the first match that it finds. Affects the ambiguities in code:#ProcessInexactMethodImpls_Ambiguities
MethodTableBuilder::bmtMethodHandle
MethodTableBuilder::FindDeclMethodOnInterfaceEntry(bmtInterfaceEntry *pItfEntry, MethodSignature &declSig, bool searchForStaticMethods)
{
    STANDARD_VM_CONTRACT;

    bmtMethodHandle declMethod;

    bmtInterfaceEntry::InterfaceSlotIterator slotIt =
        pItfEntry->IterateInterfaceSlots(GetStackingAllocator(), searchForStaticMethods);
    // Check for exact match
    for (; !slotIt.AtEnd(); slotIt.Next())
    {
        bmtRTMethod * pCurDeclMethod = slotIt->Decl().AsRTMethod();

        if (declSig.ExactlyEqual(pCurDeclMethod->GetMethodSignature().GetSignatureWithoutSubstitution()))
        {
            declMethod = slotIt->Decl();
            break;
        }
    }
    slotIt.ResetToStart();

    // Check for equivalent match if exact match wasn't found
    if (declMethod.IsNull())
    {
        for (; !slotIt.AtEnd(); slotIt.Next())
        {
            bmtRTMethod * pCurDeclMethod = slotIt->Decl().AsRTMethod();

            // Type Equivalence is forbidden in MethodImpl MemberRefs
            if (declSig.Equivalent(pCurDeclMethod->GetMethodSignature().GetSignatureWithoutSubstitution()))
            {
                declMethod = slotIt->Decl();
                break;
            }
        }
    }

    return declMethod;
}

//*******************************************************************************
//
// Used by BuildMethodTable
// Process the list of inexact method impls generated during ProcessMethodImpls.
// This list is used to cause a methodImpl to an interface to override
// methods on several equivalent interfaces in the interface map. This logic is necessary
// so that in the presence of an embedded interface the behavior appears to mimic
// the behavior if the interface was not embedded.
//
// In particular, the logic here is to handle cases such as
//
//  Assembly A
// [TypeIdentifier("x","y")]
// interface I'
// {  void Method(); }
// interface IOther : I' {}
//
//  Assembly B
// [TypeIdentifier("x","y")]
// interface I
// {  void Method(); }
// class Test : I, IOther
// {
//     void I.Method()
//     {}
// }
//
// In this case, there is one method, and one methodimpl, but there are 2 interfaces on the class that both
// require an implementation of their method. The correct semantic for type equivalence, is that any
// methodimpl directly targeting a method on an interface must be respected, and if it also applies to a type
// equivalent interface method, then if that method was not methodimpl'd directly, then the methodimpl should apply
// there as well. The ProcessInexactMethodImpls function does this secondary MethodImpl mapping.
//
//#ProcessInexactMethodImpls_Ambiguities
// In the presence of ambiguities, such as there are 3 equivalent interfaces implemented on a class and 2 methodimpls,
// we will apply the 2 method impls exactly to appropriate interface methods, and arbitrarily pick one to apply to the
// other interface. This is clearly ambiguous, but tricky to detect in the type loader efficiently, and should hopefully
// not cause too many problems.
//
VOID
MethodTableBuilder::ProcessInexactMethodImpls()
{
    STANDARD_VM_CONTRACT;

    if (bmtMethod->dwNumberInexactMethodImplCandidates == 0)
        return;

    DeclaredMethodIterator it(*this);
    while (it.Next())
    {
        // Non-virtual methods cannot be classified as methodImpl - we should have thrown an
        // error before reaching this point.
        CONSISTENCY_CHECK(!(!IsMdVirtual(it.Attrs()) && it.IsMethodImpl()));

        if (!IsMdVirtual(it.Attrs()))
        {   // Only virtual methods can participate in methodImpls
            continue;
        }

        if(!it.IsMethodImpl())
        {
            // Skip methods which are not the bodies of MethodImpl specifications
            continue;
        }

        // If this method serves as the BODY of a MethodImpl specification, then
        // we should iterate all the MethodImpl's for this class and see just how many
        // of them this method participates in as the BODY.
        for(DWORD m = 0; m < bmtMethod->dwNumberMethodImpls; m++)
        {
            // Inexact matching logic only works on MethodImpls that have been opted into inexactness by ProcessMethodImpls.
            if (!bmtMetaData->rgMethodImplTokens[m].fConsiderDuringInexactMethodImplProcessing)
            {
                continue;
            }

            // If the methodimpl we are working with does not match this method, continue to next methodimpl
            if(it.Token() != bmtMetaData->rgMethodImplTokens[m].methodBody)
            {
                continue;
            }

            bool fMatchFound = false;

            LPCUTF8 szName = NULL;
            PCCOR_SIGNATURE pSig = NULL;
            ULONG cbSig;

            mdToken mdDecl = bmtMetaData->rgMethodImplTokens[m].methodDecl;

            if (TypeFromToken(mdDecl) == mdtMethodDef)
            {   // Different methods are aused to access MethodDef and MemberRef
                // names and signatures.
                if (FAILED(GetMDImport()->GetNameOfMethodDef(mdDecl, &szName)) ||
                    FAILED(GetMDImport()->GetSigOfMethodDef(mdDecl, &cbSig, &pSig)))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                }
            }
            else
            {
                if (FAILED(GetMDImport()->GetNameAndSigOfMemberRef(mdDecl, &pSig, &cbSig, &szName)))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                }
            }

            MethodSignature declSig(GetModule(), szName, pSig, cbSig, NULL);
            bmtInterfaceEntry * pItfEntry = NULL;

            for (DWORD i = 0; i < bmtInterface->dwInterfaceMapSize; i++)
            {
                if (bmtInterface->pInterfaceMap[i].GetInterfaceEquivalenceSet() != bmtMetaData->rgMethodImplTokens[m].interfaceEquivalenceSet)
                    continue;

                bmtMethodHandle declMethod;
                pItfEntry = &bmtInterface->pInterfaceMap[i];

                // Search for declmethod on this interface
                declMethod = FindDeclMethodOnInterfaceEntry(pItfEntry, declSig);

                // If we didn't find a match, continue on to next interface in the equivalence set
                if (declMethod.IsNull())
                    continue;

                if (!IsMdVirtual(declMethod.GetDeclAttrs()))
                {   // Make sure the decl is virtual
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MUSTBEVIRTUAL, it.Token());
                }

                fMatchFound = true;

                bool fPreexistingImplFound = false;

                // Check to ensure there isn't already a matching declMethod in the method impl list
                for (DWORD iMethodImpl = 0; iMethodImpl < bmtMethodImpl->pIndex; iMethodImpl++)
                {
                    if (bmtMethodImpl->GetDeclarationMethod(iMethodImpl) == declMethod)
                    {
                        fPreexistingImplFound = true;
                        break;
                    }
                }

                // Search for other matches
                if (fPreexistingImplFound)
                    continue;

                if (bmtMetaData->rgMethodImplTokens[m].fRequiresCovariantReturnTypeChecking)
                {
                    it->GetMethodDesc()->SetRequiresCovariantReturnTypeChecking();
                }

                // Otherwise, record the method impl discovery if the match is
                bmtMethodImpl->AddMethodImpl(*it, declMethod, bmtMetaData->rgMethodImplTokens[m].methodDecl, GetStackingAllocator());
            }

            if (!fMatchFound && bmtMetaData->rgMethodImplTokens[m].fThrowIfUnmatchedDuringInexactMethodImplProcessing)
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_MI_DECLARATIONNOTFOUND, it.Token());
            }
        }
    }
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
VOID
MethodTableBuilder::ProcessMethodImpls()
{
    STANDARD_VM_CONTRACT;

    if (bmtMetaData->fHasCovariantOverride)
    {
        GetHalfBakedClass()->SetHasCovariantOverride();
    }
    if (GetParentMethodTable() != NULL)
    {
        EEClass* parentClass = GetParentMethodTable()->GetClass();
        if (parentClass->HasCovariantOverride())
            GetHalfBakedClass()->SetHasCovariantOverride();
        if (parentClass->HasVTableMethodImpl())
            GetHalfBakedClass()->SetHasVTableMethodImpl();
    }

    if (bmtMethod->dwNumberMethodImpls == 0)
        return;

    HRESULT hr = S_OK;

    DeclaredMethodIterator it(*this);
    while (it.Next())
    {
        if (!IsMdVirtual(it.Attrs()) && it.IsMethodImpl() && bmtProp->fNoSanityChecks)
        {
            // Non-virtual methods can only be classified as methodImpl when implementing
            // static virtual methods.
            CONSISTENCY_CHECK(IsMdStatic(it.Attrs()));
            continue;
        }

        // If this method serves as the BODY of a MethodImpl specification, then
        // we should iterate all the MethodImpl's for this class and see just how many
        // of them this method participates in as the BODY.
        if(it.IsMethodImpl())
        {
            for(DWORD m = 0; m < bmtMethod->dwNumberMethodImpls; m++)
            {
                if(it.Token() == bmtMetaData->rgMethodImplTokens[m].methodBody)
                {
                    mdToken mdDecl = bmtMetaData->rgMethodImplTokens[m].methodDecl;
                    bmtMethodHandle declMethod;

                    // Get the parent token for the decl method token
                    mdToken tkParent = mdTypeDefNil;
                    if (TypeFromToken(mdDecl) == mdtMethodDef || TypeFromToken(mdDecl) == mdtMemberRef)
                    {
                        if (FAILED(hr = GetMDImport()->GetParentToken(mdDecl,&tkParent)))
                        {
                            BuildMethodTableThrowException(hr, *bmtError);
                        }
                    }

                    if (GetCl() == tkParent)
                    {   // The DECL has been declared within the class that we're currently building.
                        hr = S_OK;

                        if(bmtError->pThrowable != NULL)
                        {
                            *(bmtError->pThrowable) = NULL;
                        }

                        if(TypeFromToken(mdDecl) != mdtMethodDef)
                        {
                            if (FAILED(hr = FindMethodDeclarationForMethodImpl(
                                                mdDecl, &mdDecl, TRUE)))
                            {
                                BuildMethodTableThrowException(hr, *bmtError);
                            }
                        }

                        CONSISTENCY_CHECK(TypeFromToken(mdDecl) == mdtMethodDef);
                        declMethod = bmtMethod->FindDeclaredMethodByToken(mdDecl);
                    }
                    else
                    {   // We can't call GetDescFromMemberDefOrRef here because this
                        // method depends on a fully-loaded type, including parent types,
                        // which is not always guaranteed. In particular, it requires that
                        // the instantiation dictionary be filled. The solution is the following:
                        //   1. Load the approximate type that the method belongs to.
                        //   2. Get or create the correct substitution for the type involved
                        //   3. Iterate the introduced methods on that type looking for a matching
                        //      method.

                        LPCUTF8 szName = NULL;
                        PCCOR_SIGNATURE pSig = NULL;
                        ULONG cbSig;
                        if (TypeFromToken(mdDecl) == mdtMethodDef)
                        {   // Different methods are aused to access MethodDef and MemberRef
                            // names and signatures.
                            if (FAILED(GetMDImport()->GetNameOfMethodDef(mdDecl, &szName)) ||
                                FAILED(GetMDImport()->GetSigOfMethodDef(mdDecl, &cbSig, &pSig)))
                            {
                                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                            }
                        }
                        else
                        {
                            if (FAILED(GetMDImport()->GetNameAndSigOfMemberRef(mdDecl, &pSig, &cbSig, &szName)))
                            {
                                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                            }
                        }

                        Substitution *pDeclSubst = &bmtMetaData->pMethodDeclSubsts[m];

                        MethodTable * pDeclMT = NULL;
                        MethodSignature declSig(GetModule(), szName, pSig, cbSig, NULL);

                        {   // 1. Load the approximate type.
                            // Block for the LoadsTypeViolation.
                            CONTRACT_VIOLATION(LoadsTypeViolation);
                            pDeclMT = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
                                GetModule(),
                                tkParent,
                                &bmtGenerics->typeContext,
                                ClassLoader::ThrowIfNotFound,
                                ClassLoader::PermitUninstDefOrRef,
                                ClassLoader::LoadTypes,
                                CLASS_LOAD_APPROXPARENTS,
                                TRUE).GetMethodTable()->GetCanonicalMethodTable();
                        }

                        {   // 2. Get or create the correct substitution
                            if (pDeclMT->IsInterface())
                            {
                                // If the declaration method is a part of an interface, search through
                                // the interface map to find the matching interface so we can provide
                                // the correct substitution chain.
                                bmtRTType *pDeclType = NULL;

                                bmtInterfaceEntry * pItfEntry = NULL;
                                for (DWORD i = 0; i < bmtInterface->dwInterfaceMapSize; i++)
                                {
                                    bmtRTType * pCurItf = bmtInterface->pInterfaceMap[i].GetInterfaceType();
                                    // Type Equivalence is not respected for this comparision as you can have multiple type equivalent interfaces on a class
                                    TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
                                    if (MetaSig::CompareTypeDefsUnderSubstitutions(
                                        pCurItf->GetMethodTable(),      pDeclMT,
                                        &pCurItf->GetSubstitution(),    pDeclSubst,
                                        &newVisited))
                                    {
                                        pItfEntry = &bmtInterface->pInterfaceMap[i];
                                        pDeclType = pCurItf;
                                        break;
                                    }
                                }

                                if (IsInterface())
                                {
                                    if (pDeclType == NULL)
                                    {
                                        // Interface is not implemented by this type.
                                        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_NOTIMPLEMENTED, it.Token());
                                    }
                                }
                                else
                                {
                                    if (pDeclType == NULL)
                                    {
                                        DWORD equivalenceSet = 0;

                                        for (DWORD i = 0; i < bmtInterface->dwInterfaceMapSize; i++)
                                        {
                                            bmtRTType * pCurItf = bmtInterface->pInterfaceMap[i].GetInterfaceType();
                                            // Type Equivalence is respected for this comparision as we just need to find an
                                            // equivalent interface, the particular interface is unimportant
                                            if (MetaSig::CompareTypeDefsUnderSubstitutions(
                                                pCurItf->GetMethodTable(), pDeclMT,
                                                &pCurItf->GetSubstitution(), pDeclSubst,
                                                NULL))
                                            {
                                                equivalenceSet = bmtInterface->pInterfaceMap[i].GetInterfaceEquivalenceSet();
                                                pItfEntry = &bmtInterface->pInterfaceMap[i];
                                                break;
                                            }
                                        }

                                        if (equivalenceSet == 0)
                                        {
                                            // Interface is not implemented by this type.
                                            BuildMethodTableThrowException(IDS_CLASSLOAD_MI_NOTIMPLEMENTED, it.Token());
                                        }

                                        // Interface is not implemented by this type exactly. We need to consider this MethodImpl on non exact interface matches,
                                        // as the only match may be one of the non-exact matches
                                        bmtMetaData->rgMethodImplTokens[m].fConsiderDuringInexactMethodImplProcessing = true;
                                        bmtMetaData->rgMethodImplTokens[m].fThrowIfUnmatchedDuringInexactMethodImplProcessing = true;
                                        bmtMetaData->rgMethodImplTokens[m].interfaceEquivalenceSet = equivalenceSet;
                                        bmtMethod->dwNumberInexactMethodImplCandidates++;
                                        continue; // Move on to other MethodImpls
                                    }
                                    else
                                    {
                                        // This method impl may need to match other methods during inexact processing
                                        if (pItfEntry->InEquivalenceSetWithMultipleEntries())
                                        {
                                            bmtMetaData->rgMethodImplTokens[m].fConsiderDuringInexactMethodImplProcessing = true;
                                            bmtMetaData->rgMethodImplTokens[m].fThrowIfUnmatchedDuringInexactMethodImplProcessing = false;
                                            bmtMetaData->rgMethodImplTokens[m].interfaceEquivalenceSet = pItfEntry->GetInterfaceEquivalenceSet();
                                            bmtMethod->dwNumberInexactMethodImplCandidates++;
                                        }
                                    }
                                }

                                // 3. Find the matching method.
                                declMethod = FindDeclMethodOnInterfaceEntry(pItfEntry, declSig, !IsMdVirtual(it.Attrs())); // Search for statics when the impl is non-virtual
                            }
                            else
                            {
                                GetHalfBakedClass()->SetHasVTableMethodImpl();
                                declMethod = FindDeclMethodOnClassInHierarchy(it, pDeclMT, declSig);
                            }

                            if (declMethod.IsNull())
                            {   // Would prefer to let this fall out to the BuildMethodTableThrowException
                                // below, but due to v2.0 and earlier behaviour throwing a MissingMethodException,
                                // primarily because this code used to be a simple call to
                                // MemberLoader::GetDescFromMemberDefOrRef (see above for reason why),
                                // we must continue to do the same.
                                MemberLoader::ThrowMissingMethodException(
                                    pDeclMT,
                                    declSig.GetName(),
                                    declSig.GetModule(),
                                    declSig.GetSignature(),
                                    static_cast<DWORD>(declSig.GetSignatureLength()),
                                    &bmtGenerics->typeContext);
                            }
                        }
                    }

                    if (declMethod.IsNull())
                    {   // Method not found, throw.
                        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_DECLARATIONNOTFOUND, it.Token());
                    }

                    if (!IsMdVirtual(declMethod.GetDeclAttrs()))
                    {   // Make sure the decl is virtual
                        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MUSTBEVIRTUAL, it.Token());
                    }

                    if (!IsMdVirtual(it.Attrs()) && it.IsMethodImpl() && IsMdStatic(it.Attrs()))
                    {
                        // Non-virtual methods can only be classified as methodImpl when implementing
                        // static virtual methods.
                        ValidateStaticMethodImpl(declMethod, *it);//bmtMethodHandle(pCurImplMethod));
                        continue;
                    }

                    if (bmtMetaData->rgMethodImplTokens[m].fRequiresCovariantReturnTypeChecking)
                    {
                        it->GetMethodDesc()->SetRequiresCovariantReturnTypeChecking();
                    }

                    bmtMethodImpl->AddMethodImpl(*it, declMethod, mdDecl, GetStackingAllocator());
                }
            }
        }
    } /* end ... for each member */
}


MethodTableBuilder::bmtMethodHandle MethodTableBuilder::FindDeclMethodOnClassInHierarchy(const DeclaredMethodIterator& it, MethodTable * pDeclMT, MethodSignature &declSig)
{
    bmtRTType * pDeclType = NULL;
    bmtMethodHandle declMethod;
    // Assume the MethodTable is a parent of the current type,
    // and create the substitution chain to match it.

    for (bmtRTType *pCur = GetParentType();
            pCur != NULL;
            pCur = pCur->GetParentType())
    {
        if (pCur->GetMethodTable() == pDeclMT)
        {
            pDeclType = pCur;
            break;
        }
    }

    // Instead of using the Substitution chain that reaches back to the type being loaded, instead
    // use a substitution chain that points back to the open type associated with the memberref of the declsig.
    Substitution emptySubstitution;
    Substitution* pDeclTypeSubstitution = &emptySubstitution;
    DWORD lengthOfSubstitutionChainHandled = pDeclType->GetSubstitution().GetLength();

    if (pDeclType == NULL)
    {   // Method's type is not a parent.
        BuildMethodTableThrowException(IDS_CLASSLOAD_MI_DECLARATIONNOTFOUND, it.Token());
    }

    // 3. Find the matching method.
    bmtRTType *pCurDeclType = pDeclType;
    do
    {
        // Update the substitution in use for matching the method. If the substitution length is greater
        // than the previously processed data, add onto the end of the chain.
        {
            DWORD declTypeSubstitionLength = pCurDeclType->GetSubstitution().GetLength();
            if (declTypeSubstitionLength > lengthOfSubstitutionChainHandled)
            {
                void *pNewSubstitutionMem = _alloca(sizeof(Substitution));
                Substitution substitutionToClone = pCurDeclType->GetSubstitution();

                Substitution *pNewSubstitution = new(pNewSubstitutionMem) Substitution(substitutionToClone.GetModule(), substitutionToClone.GetInst(), pDeclTypeSubstitution);
                pDeclTypeSubstitution = pNewSubstitution;
                lengthOfSubstitutionChainHandled = declTypeSubstitionLength;
            }
        }

        // two pass algorithm. search for exact matches followed
        // by equivalent matches.
        for (int iPass = 0; (iPass < 2) && (declMethod.IsNull()); iPass++)
        {
            MethodTable *pCurDeclMT = pCurDeclType->GetMethodTable();

            MethodTable::IntroducedMethodIterator methIt(pCurDeclMT);
            for(; methIt.IsValid(); methIt.Next())
            {
                MethodDesc * pCurMD = methIt.GetMethodDesc();

                if (pCurDeclMT != pDeclMT)
                {
                    // If the method isn't on the declaring type, then it must be virtual.
                    if (!pCurMD->IsVirtual())
                        continue;
                }
                if (strcmp(declSig.GetName(), pCurMD->GetName()) == 0)
                {
                    PCCOR_SIGNATURE pCurMDSig;
                    DWORD cbCurMDSig;
                    pCurMD->GetSig(&pCurMDSig, &cbCurMDSig);

                    // First pass searches for declaration methods should not use type equivalence
                    TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);

                    if (MetaSig::CompareMethodSigs(
                        declSig.GetSignature(),
                        static_cast<DWORD>(declSig.GetSignatureLength()),
                        declSig.GetModule(),
                        NULL, // Do not use the substitution of declSig, as we have adjusted the pDeclTypeSubstitution such that it must not be used.
                        pCurMDSig,
                        cbCurMDSig,
                        pCurMD->GetModule(),
                        pDeclTypeSubstitution,
                        FALSE,
                        iPass == 0 ? &newVisited : NULL))
                    {
                        declMethod = (*bmtParent->pSlotTable)[pCurMD->GetSlot()].Decl();
                        break;
                    }
                }
            }
        }

        pCurDeclType = pCurDeclType->GetParentType();
    } while ((pCurDeclType != NULL) && (declMethod.IsNull()));

    return declMethod;
}
//*******************************************************************************
// InitMethodDesc takes a pointer to space that's already allocated for the
// particular type of MethodDesc, and initializes based on the other info.
// This factors logic between PlaceMembers (the regular code path) & AddMethod
// (Edit & Continue (EnC) code path) so we don't have to maintain separate copies.
VOID
MethodTableBuilder::InitMethodDesc(
    MethodDesc *        pNewMD, // This is should actually be of the correct sub-type, based on Classification
    DWORD               Classification,
    mdToken             tok,
    DWORD               dwImplFlags,
    DWORD               dwMemberAttrs,
    BOOL                fEnC,
    DWORD               RVA,        // Only needed for NDirect case
    IMDInternalImport * pIMDII,     // Needed for NDirect, EEImpl(Delegate) cases
    LPCSTR              pMethodName // Only needed for mcEEImpl (Delegate) case
    COMMA_INDEBUG(LPCUTF8 pszDebugMethodName)
    COMMA_INDEBUG(LPCUTF8 pszDebugClassName)
    COMMA_INDEBUG(LPCUTF8 pszDebugMethodSignature)
    )
{
    CONTRACTL
    {
        THROWS;
        if (fEnC) { GC_NOTRIGGER; } else { GC_TRIGGERS; }
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_EVERYTHING, "EEC::IMD: pNewMD:0x%x for tok:0x%x (%s::%s)\n",
        pNewMD, tok, pszDebugClassName, pszDebugMethodName));

    // Now we know the classification we can perform any classification specific initialization.

    // The method desc is zero inited by the caller.

    switch (Classification)
    {
    case mcNDirect:
        {
            // NDirect specific initialization.
            NDirectMethodDesc *pNewNMD = (NDirectMethodDesc*)pNewMD;

            // Allocate writeable data
            pNewNMD->ndirect.m_pWriteableData = (NDirectWriteableData*)
                AllocateFromHighFrequencyHeap(S_SIZE_T(sizeof(NDirectWriteableData)));

#ifdef HAS_NDIRECT_IMPORT_PRECODE
            pNewNMD->ndirect.m_pImportThunkGlue = Precode::Allocate(PRECODE_NDIRECT_IMPORT, pNewMD,
                GetLoaderAllocator(), GetMemTracker())->AsNDirectImportPrecode();
#else // !HAS_NDIRECT_IMPORT_PRECODE
            pNewNMD->GetNDirectImportThunkGlue()->Init(pNewNMD);
#endif // !HAS_NDIRECT_IMPORT_PRECODE

#if defined(TARGET_X86)
            pNewNMD->ndirect.m_cbStackArgumentSize = 0xFFFF;
#endif // defined(TARGET_X86)

            // If the RVA of a native method is set, this is an early-bound IJW call
            if (RVA != 0 && IsMiUnmanaged(dwImplFlags) && IsMiNative(dwImplFlags))
            {
                // Note that we cannot initialize the stub directly now in the general case,
                // as LoadLibrary may not have been performed yet.
                pNewNMD->SetIsEarlyBound();
            }

            pNewNMD->GetWriteableData()->m_pNDirectTarget = pNewNMD->GetNDirectImportThunkGlue()->GetEntrypoint();
        }
        break;

    case mcFCall:
        break;

    case mcEEImpl:
        // For the Invoke method we will set a standard invoke method.
        BAD_FORMAT_NOTHROW_ASSERT(IsDelegate());

        // For the asserts, either the pointer is NULL (since the class hasn't
        // been constructed yet), or we're in EnC mode, meaning that the class
        // does exist, but we may be re-assigning the field to point to an
        // updated MethodDesc

        // It is not allowed for EnC to replace one of the runtime builtin methods

        if (strcmp(pMethodName, "Invoke") == 0)
        {
            BAD_FORMAT_NOTHROW_ASSERT(((DelegateEEClass*)GetHalfBakedClass())->m_pInvokeMethod == NULL);
            ((DelegateEEClass*)GetHalfBakedClass())->m_pInvokeMethod = pNewMD;
        }
        else if (strcmp(pMethodName, "BeginInvoke") == 0)
        {
            BAD_FORMAT_NOTHROW_ASSERT(((DelegateEEClass*)GetHalfBakedClass())->m_pBeginInvokeMethod == NULL);
            ((DelegateEEClass*)GetHalfBakedClass())->m_pBeginInvokeMethod = pNewMD;
        }
        else if (strcmp(pMethodName, "EndInvoke") == 0)
        {
            BAD_FORMAT_NOTHROW_ASSERT(((DelegateEEClass*)GetHalfBakedClass())->m_pEndInvokeMethod == NULL);
            ((DelegateEEClass*)GetHalfBakedClass())->m_pEndInvokeMethod = pNewMD;
        }
        else
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
        }

        // StoredSig specific intialization
        {
            StoredSigMethodDesc *pNewSMD = (StoredSigMethodDesc*) pNewMD;;
            DWORD cSig;
            PCCOR_SIGNATURE pSig;
            if (FAILED(pIMDII->GetSigOfMethodDef(tok, &cSig, &pSig)))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
            }
            pNewSMD->SetStoredMethodSig(pSig, cSig);
        }
        break;

#ifdef FEATURE_COMINTEROP
    case mcComInterop:
#endif // FEATURE_COMINTEROP
    case mcIL:
        break;

    case mcInstantiated:
#ifdef EnC_SUPPORTED
        if (fEnC)
        {
            // We reuse the instantiated methoddescs to get the slot
            InstantiatedMethodDesc* pNewIMD = (InstantiatedMethodDesc*) pNewMD;
            pNewIMD->SetupEnCAddedMethod();
        }
        else
#endif // EnC_SUPPORTED
        {
            // Initialize the typical instantiation.
            InstantiatedMethodDesc* pNewIMD = (InstantiatedMethodDesc*) pNewMD;
            //data has the same lifetime as method table, use our allocator
            pNewIMD->SetupGenericMethodDefinition(pIMDII, GetLoaderAllocator(), GetMemTracker(), GetModule(),
                                                  tok);
        }
        break;

    default:
        BAD_FORMAT_NOTHROW_ASSERT(!"Failed to set a method desc classification");
    }

    // Check the method desc's classification.
    _ASSERTE(pNewMD->GetClassification() == Classification);

    pNewMD->SetMemberDef(tok);

    if (IsMdStatic(dwMemberAttrs))
        pNewMD->SetStatic();

#ifdef _DEBUG
    // Mark as many methods as synchronized as possible.
    //
    // Note that this can easily cause programs to deadlock, and that
    // should not be treated as a bug in the program.

    static ConfigDWORD stressSynchronized;
    DWORD stressSynchronizedVal = stressSynchronized.val(CLRConfig::INTERNAL_stressSynchronized);

    bool isStressSynchronized =  stressSynchronizedVal &&
        pNewMD->IsIL() && // Synchronized is not supported on Ecalls, NDirect method, etc
        // IsValueClass() and IsEnum() do not work for System.ValueType and System.Enum themselves
        ((g_pValueTypeClass != NULL && g_pEnumClass != NULL &&
          !IsValueClass()) || // Can not synchronize on byref "this"
          IsMdStatic(dwMemberAttrs)) && // IsStatic() blows up in _DEBUG as pNewMD is not fully inited
        g_pObjectClass != NULL; // Ignore Object:* since "this" could be a boxed object

    // stressSynchronized=1 turns off the stress in the system domain to reduce
    // the chances of spurious deadlocks. Deadlocks in user code can still occur.
    // stressSynchronized=2 will probably cause more deadlocks, and is not recommended
    if (stressSynchronizedVal == 1 && GetAssembly()->IsSystem())
        isStressSynchronized = false;

    if (IsMiSynchronized(dwImplFlags) || isStressSynchronized)
#else // !_DEBUG
    if (IsMiSynchronized(dwImplFlags))
#endif // !_DEBUG
        pNewMD->SetSynchronized();

#ifdef _DEBUG
    pNewMD->m_pszDebugMethodName = (LPUTF8)pszDebugMethodName;
    pNewMD->m_pszDebugClassName  = (LPUTF8)pszDebugClassName;
    pNewMD->m_pDebugMethodTable = GetHalfBakedMethodTable();

    if (pszDebugMethodSignature == NULL)
        pNewMD->m_pszDebugMethodSignature = FormatSig(pNewMD,pNewMD->GetLoaderAllocator()->GetLowFrequencyHeap(),GetMemTracker());
    else
        pNewMD->m_pszDebugMethodSignature = pszDebugMethodSignature;
#endif // _DEBUG
} // MethodTableBuilder::InitMethodDesc

//*******************************************************************************
//
// Used by BuildMethodTable
//
VOID
MethodTableBuilder::AddMethodImplDispatchMapping(
    DispatchMapTypeID typeID,
    SLOT_INDEX        slotNumber,
    bmtMDMethod *     pImplMethod)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pMDImpl = pImplMethod->GetMethodDesc();

    // Look for an existing entry in the map.
    DispatchMapBuilder::Iterator it(bmtVT->pDispatchMapBuilder);
    if (bmtVT->pDispatchMapBuilder->Find(typeID, slotNumber, it))
    {
        // Throw if this entry has already previously been MethodImpl'd.
        if (it.IsMethodImpl())
        {
            // NOTE: This is where we check for duplicate overrides. This is the easiest place to check
            //       because duplicate overrides could in fact have separate MemberRefs to the same
            //       member and so just comparing tokens at the very start would not be enough.
            if (it.GetTargetMD() != pMDImpl)
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MULTIPLEOVERRIDES, pMDImpl->GetMemberDef());
            }
        }
        // This is the first MethodImpl. That's ok.
        else
        {
            it.SetTarget(pMDImpl);
            it.SetIsMethodImpl();
        }
    }
    // A mapping for this interface method does not exist, so insert it.
    else
    {
        bmtVT->pDispatchMapBuilder->InsertMDMapping(
            typeID,
            slotNumber,
            pMDImpl,
            TRUE);
    }

    // Save the entry into the vtable as well, if it isn't an interface methodImpl
    if (typeID == DispatchMapTypeID::ThisClassID())
    {
        bmtVT->SetVirtualMethodImpl(slotNumber, pImplMethod);
    }
} // MethodTableBuilder::AddMethodImplDispatchMapping

//*******************************************************************************
VOID
MethodTableBuilder::MethodImplCompareSignatures(
    bmtMethodHandle     hDecl,
    bmtMethodHandle     hImpl,
    BOOL                allowCovariantReturn,
    DWORD               dwConstraintErrorCode)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(!hDecl.IsNull());
        PRECONDITION(!hImpl.IsNull());
        PRECONDITION(TypeFromToken(hDecl.GetMethodSignature().GetToken()) == mdtMethodDef);
        PRECONDITION(TypeFromToken(hImpl.GetMethodSignature().GetToken()) == mdtMethodDef);
    } CONTRACTL_END;

    const MethodSignature &declSig(hDecl.GetMethodSignature());
    const MethodSignature &implSig(hImpl.GetMethodSignature());

    if (!MethodSignature::SignaturesEquivalent(declSig, implSig, allowCovariantReturn))
    {
        LOG((LF_CLASSLOADER, LL_INFO1000, "BADSIG placing MethodImpl: %x\n", declSig.GetToken()));
        BuildMethodTableThrowException(COR_E_TYPELOAD, IDS_CLASSLOAD_MI_BADSIGNATURE, declSig.GetToken());
    }

    //now compare the method constraints
    if (!MetaSig::CompareMethodConstraints(&implSig.GetSubstitution(), implSig.GetModule(), implSig.GetToken(),
                                           &declSig.GetSubstitution(), declSig.GetModule(), declSig.GetToken()))
    {
        BuildMethodTableThrowException(dwConstraintErrorCode, implSig.GetToken());
    }
}

//*******************************************************************************
// We should have collected all the method impls. Cycle through them creating the method impl
// structure that holds the information about which slots are overridden.
VOID
MethodTableBuilder::PlaceMethodImpls()
{
    STANDARD_VM_CONTRACT;

    if(bmtMethodImpl->pIndex == 0)
    {
        return;
    }

    // Allocate some temporary storage. The number of overrides for a single method impl
    // cannot be greater then the number of vtable slots for classes. But for interfaces
    // it might contain overrides for other interface methods.
    DWORD dwMaxSlotSize = IsInterface() ? bmtMethod->dwNumberMethodImpls : bmtVT->cVirtualSlots;

    DWORD * slots = new (GetStackingAllocator()) DWORD[dwMaxSlotSize];
    mdToken * tokens = new (GetStackingAllocator()) mdToken[dwMaxSlotSize];
    MethodDesc ** replaced = new (GetStackingAllocator()) MethodDesc*[dwMaxSlotSize];

    DWORD iEntry = 0;
    bmtMDMethod * pCurImplMethod = bmtMethodImpl->GetImplementationMethod(iEntry);

    DWORD slotIndex = 0;

    // The impls are sorted according to the method descs for the body of the method impl.
    // Loop through the impls until the next body is found. When a single body
    // has been done move the slots implemented and method descs replaced into the storage
    // found on the body method desc.
    while (true)
    {   // collect information until we reach the next body

        tokens[slotIndex] = bmtMethodImpl->GetDeclarationToken(iEntry);

        // Get the declaration part of the method impl. It will either be a token
        // (declaration is on this type) or a method desc.
        bmtMethodHandle hDeclMethod = bmtMethodImpl->GetDeclarationMethod(iEntry);

        // Don't place static virtual method overrides in the vtable
        if (!IsMdStatic(hDeclMethod.GetDeclAttrs()))
        {
            if(hDeclMethod.IsMDMethod())
            {
                // The declaration is on the type being built
                bmtMDMethod * pCurDeclMethod = hDeclMethod.AsMDMethod();

                mdToken mdef = pCurDeclMethod->GetMethodSignature().GetToken();
                if (bmtMethodImpl->IsBody(mdef))
                {   // A method declared on this class cannot be both a decl and an impl
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MULTIPLEOVERRIDES, mdef);
                }

                if (IsInterface())
                {
                    // Throws
                    PlaceInterfaceDeclarationOnInterface(
                        hDeclMethod,
                        pCurImplMethod,
                        slots,              // Adds override to the slot and replaced arrays.
                        replaced,
                        &slotIndex,
                        dwMaxSlotSize);     // Increments count
                }
                else
                {
                    // Throws
                    PlaceLocalDeclarationOnClass(
                        pCurDeclMethod,
                        pCurImplMethod,
                        slots,              // Adds override to the slot and replaced arrays.
                        replaced,
                        &slotIndex,
                        dwMaxSlotSize);     // Increments count
                }
            }
            else
            {
                bmtRTMethod * pCurDeclMethod = hDeclMethod.AsRTMethod();

                if (IsInterface())
                {
                    // Throws
                    PlaceInterfaceDeclarationOnInterface(
                        hDeclMethod,
                        pCurImplMethod,
                        slots,              // Adds override to the slot and replaced arrays.
                        replaced,
                        &slotIndex,
                        dwMaxSlotSize);     // Increments count
                }
                else
                {
                    // Do not use pDecl->IsInterface here as that asks the method table and the MT may not yet be set up.
                    if (pCurDeclMethod->GetOwningType()->IsInterface())
                    {
                        // Throws
                        PlaceInterfaceDeclarationOnClass(
                            pCurDeclMethod,
                            pCurImplMethod);
                    }
                    else
                    {
                        // Throws
                        PlaceParentDeclarationOnClass(
                            pCurDeclMethod,
                            pCurImplMethod,
                            slots,
                            replaced,
                            &slotIndex,
                            dwMaxSlotSize);        // Increments count
                    }
                }
            }
        }

        iEntry++;

        if(iEntry == bmtMethodImpl->pIndex)
        {
            // We hit the end of the list so dump the current data and leave
            WriteMethodImplData(pCurImplMethod, slotIndex, slots, tokens, replaced);
            break;
        }
        else
        {
            bmtMDMethod * pNextImplMethod = bmtMethodImpl->GetImplementationMethod(iEntry);

            if (pNextImplMethod != pCurImplMethod)
            {
                // If we're moving on to a new body, dump the current data and reset the counter
                WriteMethodImplData(pCurImplMethod, slotIndex, slots, tokens, replaced);
                slotIndex = 0;
            }

            pCurImplMethod = pNextImplMethod;
        }
    }  // while(next != NULL)
} // MethodTableBuilder::PlaceMethodImpls

//*******************************************************************************
VOID
MethodTableBuilder::WriteMethodImplData(
    bmtMDMethod * pImplMethod,
    DWORD         cSlots,
    DWORD *       rgSlots,
    mdToken *     rgTokens,
    MethodDesc ** rgDeclMD)
{
    STANDARD_VM_CONTRACT;

    // Use the number of overrides to
    // push information on to the method desc. We store the slots that
    // are overridden and the method desc that is replaced. That way
    // when derived classes need to determine if the method is to be
    // overridden then it can check the name against the replaced
    // method desc not the bodies name.
    if (cSlots == 0)
    {
        //@TODO:NEWVTWORK: Determine methodImpl status so that we don't need this workaround.
        //@TODO:NEWVTWORK: This occurs when only interface decls are involved, since
        //@TODO:NEWVTWORK: these are stored in the dispatch map and not on the methoddesc.
    }
    else
    {
        MethodImpl * pImpl = pImplMethod->GetMethodDesc()->GetMethodImpl();

        // Set the size of the info the MethodImpl needs to keep track of.
        pImpl->SetSize(GetLoaderAllocator()->GetHighFrequencyHeap(), GetMemTracker(), cSlots);

        if (!IsInterface())
        {
            // If we are currently builting an interface, the slots here has no meaning and we can skip it
            // Sort the two arrays in slot index order
            // This is required in MethodImpl::FindSlotIndex and MethodImpl::Iterator as we'll be using
            // binary search later
            for (DWORD i = 0; i < cSlots; i++)
            {
                unsigned int min = i;
                for (DWORD j = i + 1; j < cSlots; j++)
                {
                    if (rgSlots[j] < rgSlots[min])
                    {
                        min = j;
                    }
                }

                if (min != i)
                {
                    MethodDesc * mTmp = rgDeclMD[i];
                    rgDeclMD[i] = rgDeclMD[min];
                    rgDeclMD[min] = mTmp;

                    DWORD sTmp = rgSlots[i];
                    rgSlots[i] = rgSlots[min];
                    rgSlots[min] = sTmp;

                    mdToken tTmp = rgTokens[i];
                    rgTokens[i] = rgTokens[min];
                    rgTokens[min] = tTmp;
                }
            }
        }

        // Go and set the method impl
        pImpl->SetData(rgSlots, rgTokens, rgDeclMD);

        GetHalfBakedClass()->SetContainsMethodImpls();
    }
} // MethodTableBuilder::WriteMethodImplData

//*******************************************************************************
VOID
MethodTableBuilder::PlaceLocalDeclarationOnClass(
    bmtMDMethod * pDecl,
    bmtMDMethod * pImpl,
    DWORD *       slots,
    MethodDesc ** replaced,
    DWORD *       pSlotIndex,
    DWORD         dwMaxSlotSize)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(bmtVT->pDispatchMapBuilder));
        PRECONDITION(CheckPointer(pDecl));
        PRECONDITION(CheckPointer(pImpl));
    }
    CONTRACTL_END

    if (!bmtProp->fNoSanityChecks)
    {
        ///////////////////////////////
        // Verify the signatures match

        MethodImplCompareSignatures(
            pDecl,
            pImpl,
            FALSE /* allowCovariantReturn */,
            IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_LOCAL_METHOD_IMPL);

        ///////////////////////////////
        // Validate the method impl.

        TestMethodImpl(
            bmtMethodHandle(pDecl),
            bmtMethodHandle(pImpl));
    }

    // Don't allow overrides for any of the four special runtime implemented delegate methods
    if (IsDelegate())
    {
        LPCUTF8 strMethodName = pDecl->GetMethodSignature().GetName();
        if ((strcmp(strMethodName, COR_CTOR_METHOD_NAME) == 0) ||
            (strcmp(strMethodName, "Invoke")             == 0) ||
            (strcmp(strMethodName, "BeginInvoke")        == 0) ||
            (strcmp(strMethodName, "EndInvoke")          == 0))
        {
            BuildMethodTableThrowException(
                IDS_CLASSLOAD_MI_CANNOT_OVERRIDE,
                pDecl->GetMethodSignature().GetToken());
        }
    }

    ///////////////////
    // Add the mapping

    // Call helper to add it. Will throw if decl is already MethodImpl'd
    CONSISTENCY_CHECK(pDecl->GetSlotIndex() == static_cast<SLOT_INDEX>(pDecl->GetMethodDesc()->GetSlot()));
    AddMethodImplDispatchMapping(
        DispatchMapTypeID::ThisClassID(),
        pDecl->GetSlotIndex(),
        pImpl);

    // We implement this slot, record it
    ASSERT(*pSlotIndex < dwMaxSlotSize);
    slots[*pSlotIndex] = pDecl->GetSlotIndex();
    replaced[*pSlotIndex] = pDecl->GetMethodDesc();

    // increment the counter
    (*pSlotIndex)++;
} // MethodTableBuilder::PlaceLocalDeclarationOnClass

//*******************************************************************************
VOID MethodTableBuilder::PlaceInterfaceDeclarationOnClass(
    bmtRTMethod *     pDecl,
    bmtMDMethod *     pImpl)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pDecl));
        PRECONDITION(CheckPointer(pImpl));
        PRECONDITION(pDecl->GetMethodDesc()->IsInterface());
        PRECONDITION(CheckPointer(bmtVT->pDispatchMapBuilder));
    } CONTRACTL_END;

    MethodDesc *  pDeclMD = pDecl->GetMethodDesc();
    MethodTable * pDeclMT = pDeclMD->GetMethodTable();

    // Note that the fact that pDecl is non-NULL means that we found the
    // declaration token to be owned by a declared interface for this type.

    if (!bmtProp->fNoSanityChecks)
    {
        ///////////////////////////////
        // Verify the signatures match

        MethodImplCompareSignatures(
            pDecl,
            pImpl,
            FALSE /* allowCovariantReturn */,
            IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_INTERFACE_METHOD_IMPL);

        ///////////////////////////////
        // Validate the method impl.

        TestMethodImpl(
            bmtMethodHandle(pDecl),
            bmtMethodHandle(pImpl));
    }

    ///////////////////
    // Add the mapping

    // Note that we need only one DispatchMapTypeID for this interface (though there might be more if there
    // are duplicates). The first one is easy to get, but we could (in theory) use the last one or a random
    // one.
    // Q: Why don't we have to place this method for all duplicate interfaces? Because VSD knows about
    // duplicates and finds the right (latest) implementation for us - see
    // code:MethodTable::MethodDataInterfaceImpl::PopulateNextLevel#ProcessAllDuplicates.
    UINT32 cInterfaceDuplicates;
    DispatchMapTypeID firstDispatchMapTypeID;
    ComputeDispatchMapTypeIDs(
        pDeclMT,
        &pDecl->GetMethodSignature().GetSubstitution(),
        &firstDispatchMapTypeID,
        1,
        &cInterfaceDuplicates);
    CONSISTENCY_CHECK(cInterfaceDuplicates >= 1);
    CONSISTENCY_CHECK(firstDispatchMapTypeID.IsImplementedInterface());

    // Call helper to add it. Will throw if decl is already MethodImpl'd
    CONSISTENCY_CHECK(pDecl->GetSlotIndex() == static_cast<SLOT_INDEX>(pDecl->GetMethodDesc()->GetSlot()));
    AddMethodImplDispatchMapping(
        firstDispatchMapTypeID,
        pDecl->GetSlotIndex(),
        pImpl);

#ifdef _DEBUG
    if (bmtInterface->dbg_fShouldInjectInterfaceDuplicates)
    {   // We injected interface duplicates

        // We have to MethodImpl all interface duplicates as all duplicates are 'declared on type' (see
        // code:#InjectInterfaceDuplicates_ApproxInterfaces)
        DispatchMapTypeID * rgDispatchMapTypeIDs = (DispatchMapTypeID *)_alloca(sizeof(DispatchMapTypeID) * cInterfaceDuplicates);
        ComputeDispatchMapTypeIDs(
            pDeclMT,
            &pDecl->GetMethodSignature().GetSubstitution(),
            rgDispatchMapTypeIDs,
            cInterfaceDuplicates,
            &cInterfaceDuplicates);
        for (UINT32 nInterfaceDuplicate = 1; nInterfaceDuplicate < cInterfaceDuplicates; nInterfaceDuplicate++)
        {
            // Add MethodImpl record for each injected interface duplicate
            AddMethodImplDispatchMapping(
                rgDispatchMapTypeIDs[nInterfaceDuplicate],
                pDecl->GetSlotIndex(),
                pImpl);
        }
    }
#endif //_DEBUG
} // MethodTableBuilder::PlaceInterfaceDeclarationOnClass

//*******************************************************************************
VOID MethodTableBuilder::PlaceInterfaceDeclarationOnInterface(
    bmtMethodHandle hDecl,
    bmtMDMethod   *pImpl,
    DWORD *       slots,
    MethodDesc ** replaced,
    DWORD *       pSlotIndex,
    DWORD         dwMaxSlotSize)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pImpl));
        PRECONDITION(IsInterface());
        PRECONDITION(hDecl.GetMethodDesc()->IsInterface());
    } CONTRACTL_END;

    MethodDesc *  pDeclMD = hDecl.GetMethodDesc();

    if (!bmtProp->fNoSanityChecks)
    {
        ///////////////////////////////
        // Verify the signatures match

        MethodImplCompareSignatures(
            hDecl,
            bmtMethodHandle(pImpl),
            FALSE /* allowCovariantReturn */,
            IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_INTERFACE_METHOD_IMPL);

        ///////////////////////////////
        // Validate the method impl.

        TestMethodImpl(hDecl, bmtMethodHandle(pImpl));
    }

    // We implement this slot, record it
    ASSERT(*pSlotIndex < dwMaxSlotSize);
    slots[*pSlotIndex] = hDecl.GetSlotIndex();
    replaced[*pSlotIndex] = pDeclMD;

    // increment the counter
    (*pSlotIndex)++;
} // MethodTableBuilder::PlaceInterfaceDeclarationOnInterface

//*******************************************************************************
VOID
MethodTableBuilder::PlaceParentDeclarationOnClass(
    bmtRTMethod * pDecl,
    bmtMDMethod * pImpl,
    DWORD *       slots,
    MethodDesc**  replaced,
    DWORD *       pSlotIndex,
    DWORD         dwMaxSlotSize)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pDecl));
        PRECONDITION(CheckPointer(pImpl));
        PRECONDITION(CheckPointer(bmtVT->pDispatchMapBuilder));
        PRECONDITION(CheckPointer(GetParentMethodTable()));
    } CONTRACTL_END;

    MethodDesc * pDeclMD = pDecl->GetMethodDesc();

    // Note that the fact that pDecl is non-NULL means that we found the
    // declaration token to be owned by a parent type.

    if (!bmtProp->fNoSanityChecks)
    {
        /////////////////////////////////////////
        // Verify that the signatures match

        MethodImplCompareSignatures(
            pDecl,
            pImpl,
            TRUE /* allowCovariantReturn */,
            IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_PARENT_METHOD_IMPL);

        ////////////////////////////////
        // Verify rules of method impls

        TestMethodImpl(
            bmtMethodHandle(pDecl),
            bmtMethodHandle(pImpl));
    }

    ///////////////////
    // Add the mapping

    // Call helper to add it. Will throw if DECL is already MethodImpl'd
    AddMethodImplDispatchMapping(
        DispatchMapTypeID::ThisClassID(),
        pDeclMD->GetSlot(),
        pImpl);

    // We implement this slot, record it
    ASSERT(*pSlotIndex < dwMaxSlotSize);
    slots[*pSlotIndex] = pDeclMD->GetSlot();
    replaced[*pSlotIndex] = pDeclMD;

    // increment the counter
    (*pSlotIndex)++;
} // MethodTableBuilder::PlaceParentDeclarationOnClass

VOID MethodTableBuilder::ValidateStaticMethodImpl(
    bmtMethodHandle     hDecl,
    bmtMethodHandle     hImpl)
{
    // While we don't want to place the static method impl declarations on the class/interface, we do
    // need to validate the method constraints and signature are compatible
    if (!bmtProp->fNoSanityChecks)
    {
        ///////////////////////////////
        // Verify the signatures match

        MethodImplCompareSignatures(
            hDecl,
            hImpl,
            FALSE /* allowCovariantReturn */,
            IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_INTERFACE_METHOD_IMPL);

        ///////////////////////////////
        // Validate the method impl.

        TestMethodImpl(hDecl, hImpl);
    }
}

//*******************************************************************************
// This will validate that all interface methods that were matched during
// layout also validate against type constraints.

VOID MethodTableBuilder::ValidateInterfaceMethodConstraints()
{
    STANDARD_VM_CONTRACT;

    DispatchMapBuilder::Iterator it(bmtVT->pDispatchMapBuilder);
    for (; it.IsValid(); it.Next())
    {
        if (it.GetTypeID() != DispatchMapTypeID::ThisClassID())
        {
            bmtRTType * pItf = bmtInterface->pInterfaceMap[it.GetTypeID().GetInterfaceNum()].GetInterfaceType();

            // Grab the method token
            MethodTable * pMTItf = pItf->GetMethodTable();
            CONSISTENCY_CHECK(CheckPointer(pMTItf->GetMethodDescForSlot(it.GetSlotNumber())));
            mdMethodDef mdTok = pItf->GetMethodTable()->GetMethodDescForSlot(it.GetSlotNumber())->GetMemberDef();

            // Default to the current module. The code immediately below determines if this
            // assumption is incorrect.
            Module *          pTargetModule          = GetModule();

            // Get the module of the target method. Get it through the chunk to
            // avoid triggering the assert that MethodTable is non-NULL. It may
            // be null since it may belong to the type we're building right now.
            MethodDesc *      pTargetMD              = it.GetTargetMD();

            // If pTargetMT is null, this indicates that the target MethodDesc belongs
            // to the current type. Otherwise, the MethodDesc MUST be owned by a parent
            // of the type we're building.
            BOOL              fTargetIsOwnedByParent = pTargetMD->GetMethodTable() != NULL;

            // If the method is owned by a parent, we need to use the parent's module,
            // and we must construct the substitution chain all the way up to the parent.
            const Substitution *pSubstTgt = NULL;
            if (fTargetIsOwnedByParent)
            {
                CONSISTENCY_CHECK(CheckPointer(GetParentType()));
                bmtRTType *pTargetType = bmtRTType::FindType(GetParentType(), pTargetMD->GetMethodTable());
                pSubstTgt = &pTargetType->GetSubstitution();
                pTargetModule = pTargetType->GetModule();
            }

            // Now compare the method constraints.
            if (!MetaSig::CompareMethodConstraints(pSubstTgt,
                                                   pTargetModule,
                                                   pTargetMD->GetMemberDef(),
                                                   &pItf->GetSubstitution(),
                                                   pMTItf->GetModule(),
                                                   mdTok))
            {
                LOG((LF_CLASSLOADER, LL_INFO1000,
                     "BADCONSTRAINTS on interface method implementation: %x\n", pTargetMD));
                // This exception will be due to an implicit implementation, since explicit errors
                // will be detected in MethodImplCompareSignatures (for now, anyway).
                CONSISTENCY_CHECK(!it.IsMethodImpl());
                DWORD idsError = it.IsMethodImpl() ?
                                 IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_INTERFACE_METHOD_IMPL :
                                 IDS_CLASSLOAD_CONSTRAINT_MISMATCH_ON_IMPLICIT_IMPLEMENTATION;
                if (fTargetIsOwnedByParent)
                {
                    DefineFullyQualifiedNameForClass();
                    LPCUTF8 szClassName = GetFullyQualifiedNameForClassNestedAware(pTargetMD->GetMethodTable());
                    LPCUTF8 szMethodName = pTargetMD->GetName();

                    CQuickBytes qb;
                    // allocate enough room for "<class>.<method>\0"
                    size_t cchFullName = strlen(szClassName) + 1 + strlen(szMethodName) + 1;
                    LPUTF8 szFullName = (LPUTF8) qb.AllocThrows(cchFullName);
                    strcpy_s(szFullName, cchFullName, szClassName);
                    strcat_s(szFullName, cchFullName, ".");
                    strcat_s(szFullName, cchFullName, szMethodName);

                    BuildMethodTableThrowException(idsError, szFullName);
                }
                else
                {
                    BuildMethodTableThrowException(idsError, pTargetMD->GetMemberDef());
                }
            }
        }
    }
} // MethodTableBuilder::ValidateInterfaceMethodConstraints

//*******************************************************************************
// Used to allocate and initialize MethodDescs (both the boxed and unboxed entrypoints)
VOID MethodTableBuilder::AllocAndInitMethodDescs()
{
    STANDARD_VM_CONTRACT;

    //
    // Go over all MethodDescs and create smallest number of MethodDescChunks possible.
    //
    // Iterate over all methods and start a new chunk only if:
    //  - Token range (upper 24 bits of the method token) has changed.
    //  - The maximum size of the chunk has been reached.
    //

    int currentTokenRange = -1; // current token range
    SIZE_T sizeOfMethodDescs = 0; // current running size of methodDesc chunk
    int startIndex = 0; // start of the current chunk (index into bmtMethod array)

    DeclaredMethodIterator it(*this);
    while (it.Next())
    {
        int tokenRange = GetTokenRange(it.Token());

        // This code assumes that iterator returns tokens in ascending order. If this assumption does not hold,
        // the code will still work with small performance penalty (method desc chunk layout will be less efficient).
        _ASSERTE(tokenRange >= currentTokenRange);

        SIZE_T size = MethodDesc::GetBaseSize(GetMethodClassification(it->GetMethodType()));

        // Add size of optional slots

        if (it->GetMethodImplType() == METHOD_IMPL)
            size += sizeof(MethodImpl);

        if (it->GetSlotIndex() >= bmtVT->cVtableSlots)
            size += sizeof(MethodDesc::NonVtableSlot); // slot

        if (NeedsNativeCodeSlot(*it))
            size += sizeof(MethodDesc::NativeCodeSlot);

        // See comment in AllocAndInitMethodDescChunk
        if (NeedsTightlyBoundUnboxingStub(*it))
        {
            size *= 2;

            if (bmtGenerics->GetNumGenericArgs() == 0) {
                size += sizeof(MethodDesc::NonVtableSlot);
            }
            else {
                bmtVT->cVtableSlots++;
            }
        }

        if (tokenRange != currentTokenRange ||
            sizeOfMethodDescs + size > MethodDescChunk::MaxSizeOfMethodDescs)
        {
            if (sizeOfMethodDescs != 0)
            {
                AllocAndInitMethodDescChunk(startIndex, it.CurrentIndex() - startIndex, sizeOfMethodDescs);
                startIndex = it.CurrentIndex();
            }

            currentTokenRange = tokenRange;
            sizeOfMethodDescs = 0;
        }

        sizeOfMethodDescs += size;
    }

    if (sizeOfMethodDescs != 0)
    {
        AllocAndInitMethodDescChunk(startIndex, NumDeclaredMethods() - startIndex, sizeOfMethodDescs);
    }
}

//*******************************************************************************
// Allocates and initializes one method desc chunk.
//
// Arguments:
//    startIndex - index of first method in bmtMethod array.
//    count - number of methods in this chunk (contiguous region from startIndex)
//    sizeOfMethodDescs - total expected size of MethodDescs in this chunk
//
// Used by AllocAndInitMethodDescs.
//
VOID MethodTableBuilder::AllocAndInitMethodDescChunk(COUNT_T startIndex, COUNT_T count, SIZE_T sizeOfMethodDescs)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(sizeOfMethodDescs <= MethodDescChunk::MaxSizeOfMethodDescs);
    } CONTRACTL_END;

    void * pMem = GetMemTracker()->Track(
        GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(TADDR) + sizeof(MethodDescChunk) + sizeOfMethodDescs)));

    // Skip pointer to temporary entrypoints
    MethodDescChunk * pChunk = (MethodDescChunk *)((BYTE*)pMem + sizeof(TADDR));

    COUNT_T methodDescCount = 0;

    SIZE_T offset = sizeof(MethodDescChunk);

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22019) // Suppress PREFast warning about integer underflow
#endif // _PREFAST_
    for (COUNT_T i = 0; i < count; i++)
#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_

    {
        bmtMDMethod * pMDMethod = (*bmtMethod)[static_cast<SLOT_INDEX>(startIndex + i)];

        MethodDesc * pMD = (MethodDesc *)((BYTE *)pChunk + offset);

        pMD->SetChunkIndex(pChunk);

        InitNewMethodDesc(pMDMethod, pMD);

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22018) // Suppress PREFast warning about integer underflow
#endif // _PREFAST_
        offset += pMD->SizeOf();
#ifdef _PREFAST_
#pragma warning(pop)
#endif // _PREFAST_

        methodDescCount++;

        // If we're a value class, we want to create duplicate slots
        // and MethodDescs for all methods in the vtable
        // section (i.e. not non-virtual instance methods or statics).
        // In the name of uniformity it would be much nicer
        // if we created _all_ value class BoxedEntryPointStubs at this point.
        // However, non-virtual instance methods only require unboxing
        // stubs in the rare case that we create a delegate to such a
        // method, and thus it would be inefficient to create them on
        // loading: after all typical structs will have many non-virtual
        // instance methods.
        //
        // Unboxing stubs for non-virtual instance methods are created
        // in code:MethodDesc::FindOrCreateAssociatedMethodDesc.

        if (NeedsTightlyBoundUnboxingStub(pMDMethod))
        {
            MethodDesc * pUnboxedMD = (MethodDesc *)((BYTE *)pChunk + offset);

            //////////////////////////////////
            // Initialize the new MethodDesc

            // <NICE> memcpy operations on data structures like MethodDescs are extremely fragile
            // and should not be used.  We should go to the effort of having proper constructors
            // in the MethodDesc class. </NICE>

            memcpy(pUnboxedMD, pMD, pMD->SizeOf());

            // Reset the chunk index
            pUnboxedMD->SetChunkIndex(pChunk);

            if (bmtGenerics->GetNumGenericArgs() == 0) {
                pUnboxedMD->SetHasNonVtableSlot();
            }

            //////////////////////////////////////////////////////////
            // Modify the original MethodDesc to be an unboxing stub

            pMD->SetIsUnboxingStub();

            ////////////////////////////////////////////////////////////////////
            // Add the new MethodDesc to the non-virtual portion of the vtable

            if (!bmtVT->AddUnboxedMethod(pMDMethod))
                BuildMethodTableThrowException(IDS_CLASSLOAD_TOO_MANY_METHODS);

            pUnboxedMD->SetSlot(pMDMethod->GetUnboxedSlotIndex());
            pMDMethod->SetUnboxedMethodDesc(pUnboxedMD);

            offset += pUnboxedMD->SizeOf();
            methodDescCount++;
        }
    }
    _ASSERTE(offset == sizeof(MethodDescChunk) + sizeOfMethodDescs);

    pChunk->SetSizeAndCount(sizeOfMethodDescs, methodDescCount);

    GetHalfBakedClass()->AddChunk(pChunk);
}

//*******************************************************************************
BOOL
MethodTableBuilder::NeedsTightlyBoundUnboxingStub(bmtMDMethod * pMDMethod)
{
    STANDARD_VM_CONTRACT;

    return IsValueClass() &&
           !IsMdStatic(pMDMethod->GetDeclAttrs()) &&
           IsMdVirtual(pMDMethod->GetDeclAttrs()) &&
           (pMDMethod->GetMethodType() != METHOD_TYPE_INSTANTIATED) &&
           !IsMdRTSpecialName(pMDMethod->GetDeclAttrs());
}

//*******************************************************************************
BOOL
MethodTableBuilder::NeedsNativeCodeSlot(bmtMDMethod * pMDMethod)
{
    LIMITED_METHOD_CONTRACT;


#ifdef FEATURE_TIERED_COMPILATION
    // Keep in-sync with MethodDesc::DetermineAndSetIsEligibleForTieredCompilation()
    if ((g_pConfig->TieredCompilation() &&

        // Policy - If QuickJit is disabled and the module does not have any pregenerated code, the method would be ineligible
        // for tiering currently to avoid some unnecessary overhead
        (g_pConfig->TieredCompilation_QuickJit() || GetModule()->IsReadyToRun()) &&

        (pMDMethod->GetMethodType() == METHOD_TYPE_NORMAL || pMDMethod->GetMethodType() == METHOD_TYPE_INSTANTIATED))

#ifdef FEATURE_REJIT
        ||

        // Methods that are R2R need precode if ReJIT is enabled. Keep this in sync with MethodDesc::IsEligibleForReJIT()
        (ReJitManager::IsReJITEnabled() &&

            GetMethodClassification(pMDMethod->GetMethodType()) == mcIL &&

            !GetModule()->IsCollectible() &&

            !GetModule()->IsEditAndContinueEnabled())
#endif // FEATURE_REJIT
        )
    {
        return TRUE;
    }
#endif

#ifdef FEATURE_DEFAULT_INTERFACES
    if (IsInterface())
    {
        DWORD attrs = pMDMethod->GetDeclAttrs();
        if (!IsMdStatic(attrs) && IsMdVirtual(attrs) && !IsMdAbstract(attrs))
        {
            // Default interface method. Since interface methods currently need to have a precode, the native code slot will be
            // used to retrieve the native code entry point, instead of getting it from the precode, which is not reliable with
            // debuggers setting breakpoints.
            return TRUE;
        }
    }
#endif

#if defined(FEATURE_JIT_PITCHING)
    if ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchEnabled) != 0) &&
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitPitchMemThreshold) != 0))
        return TRUE;
#endif

    return GetModule()->IsEditAndContinueEnabled();
}

//*******************************************************************************
VOID
MethodTableBuilder::AllocAndInitDictionary()
{
    STANDARD_VM_CONTRACT;

    // Allocate dictionary layout used by all compatible instantiations

    if (bmtGenerics->fSharedByGenericInstantiations && !bmtGenerics->fContainsGenericVariables)
    {
        // We use the number of methods as a heuristic for the number of slots in the dictionary
        // attached to shared class method tables.
        // If there are no declared methods then we have no slots, and we will never do any token lookups
        //
        // Heuristics
        //  - Classes with a small number of methods (2-3) tend to be more likely to use new slots,
        //    i.e. further methods tend to reuse slots from previous methods.
        //      = treat all classes with only 2-3 methods as if they have an extra method.
        //  - Classes with more generic parameters tend to use more slots.
        //      = multiply by 1.5 for 2 params or more

        DWORD numMethodsAdjusted =
            (bmtMethod->dwNumDeclaredNonAbstractMethods == 0)
            ? 0
            : (bmtMethod->dwNumDeclaredNonAbstractMethods < 3)
            ? 3
            : bmtMethod->dwNumDeclaredNonAbstractMethods;

        _ASSERTE(bmtGenerics->GetNumGenericArgs() != 0);
        DWORD nTypeFactorBy2 = (bmtGenerics->GetNumGenericArgs() == 1)
                               ? 2
                               : 3;

        DWORD estNumTypeSlots = (numMethodsAdjusted * nTypeFactorBy2 + 2) / 3;
        // estNumTypeSlots should fit in a WORD as long as we maintain the current
        // limit on the number of methods in a type (approx 2^16).
        _ASSERTE(FitsIn<WORD>(estNumTypeSlots));
        WORD numTypeSlots = static_cast<WORD>(estNumTypeSlots);

        if (numTypeSlots > 0)
        {
            // Dictionary layout is an optional field on EEClass, so ensure the optional field descriptor has
            // been allocated.
            EnsureOptionalFieldsAreAllocated(GetHalfBakedClass(), m_pAllocMemTracker, GetLoaderAllocator()->GetLowFrequencyHeap());
            GetHalfBakedClass()->SetDictionaryLayout(DictionaryLayout::Allocate(numTypeSlots, bmtAllocator, m_pAllocMemTracker));
        }
    }

}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Compute the set of interfaces which are equivalent. Duplicates in the interface map
// will be placed into different equivalence sets unless they participate in type equivalence.
// This is a bit odd, but it turns out we only need to know about equivalence classes if
// there is type equivalence involved in the interface, and not detecting, or detecting equivalence
// in other cases does not result in differing behavior.
//
// By restricting the reasons for having equivalence matches, we reduce the algorithm from one which
// is O(n*n) best case to an algorithm which will typically execute something more like O(m*n) best case time
// where m is the number of generic interface (although still n*n in worst case). The assumption is that equivalent
// and generic interfaces are relatively rare.
VOID
MethodTableBuilder::ComputeInterfaceMapEquivalenceSet()
{
    STANDARD_VM_CONTRACT;

    UINT32 nextEquivalenceSet = 1;

    for (DWORD dwCurInterface = 0;
         dwCurInterface < bmtInterface->dwInterfaceMapSize;
         dwCurInterface++)
    {
        // Keep track of the current interface we are trying to calculate the equivalence set of
        bmtInterfaceEntry *     pCurItfEntry = &bmtInterface->pInterfaceMap[dwCurInterface];
        bmtRTType *             pCurItf      = pCurItfEntry->GetInterfaceType();
        MethodTable *           pCurItfMT    = pCurItf->GetMethodTable();
        const Substitution *    pCurItfSubst = &pCurItf->GetSubstitution();

        UINT32 currentEquivalenceSet = 0;

        // Only interfaces with type equivalence, or that are generic need to be compared for equivalence
        if (pCurItfMT->HasTypeEquivalence() || pCurItfMT->HasInstantiation())
        {
            for (DWORD dwCurInterfaceCompare = 0;
                 dwCurInterfaceCompare < dwCurInterface;
                 dwCurInterfaceCompare++)
            {
                // Keep track of the current interface we are trying to calculate the equivalence set of
                bmtInterfaceEntry *     pCompareItfEntry = &bmtInterface->pInterfaceMap[dwCurInterfaceCompare];
                bmtRTType *             pCompareItf      = pCompareItfEntry->GetInterfaceType();
                MethodTable *           pCompareItfMT    = pCompareItf->GetMethodTable();
                const Substitution *    pCompareItfSubst = &pCompareItf->GetSubstitution();

                // Only interfaces with type equivalence, or that are generic need to be compared for equivalence
                if (pCompareItfMT->HasTypeEquivalence() || pCompareItfMT->HasInstantiation())
                {
                    if (MetaSig::CompareTypeDefsUnderSubstitutions(pCurItfMT,
                                                                   pCompareItfMT,
                                                                   pCurItfSubst,
                                                                   pCompareItfSubst,
                                                                   NULL))
                    {
                        currentEquivalenceSet = pCompareItfEntry->GetInterfaceEquivalenceSet();
                        // Use the equivalence set of the interface map entry we just found
                        pCurItfEntry->SetInterfaceEquivalenceSet(currentEquivalenceSet, true);
                        // Update the interface map entry we just found to indicate that it is part of an equivalence
                        // set with multiple entries.
                        pCompareItfEntry->SetInterfaceEquivalenceSet(currentEquivalenceSet, true);
                        break;
                    }
                }
            }
        }

        // If we did not find an equivalent interface above, use the next available equivalence set indicator
        if (currentEquivalenceSet == 0)
        {
            pCurItfEntry->SetInterfaceEquivalenceSet(nextEquivalenceSet, false);
            nextEquivalenceSet++;
        }
    }
}

//*******************************************************************************
//
// Used by PlaceInterfaceMethods
//
// Given an interface in our interface map, and a particular method on that interface, place
// a method from the parent types implementation of an equivalent interface into that method
// slot. Used by PlaceInterfaceMethods to make equivalent interface implementations have the
// same behavior as if the parent interface was implemented on this type instead of an equivalent interface.
//
// This logic is used in situations such as below. I and I' are equivalent interfaces
//
//#
// class Base : I
// {void I.Method() { } }
// interface IOther : I' {}
// class Derived : IOther
// { virtual void Method() {}}
//
// We should Map I'.Method to Base.Method, not Derived.Method
//
// Another example
// class Base : I
// { virtual void Method() }
// interface IOther : I' {}
// class Derived : IOther
// { virtual void Method() {}}
//
// We should map I'.Method to Base.Method, not Derived.Method
//
// class Base : I
// {void I.Method() { } }
// class Derived : I'
// {}
//
// We should Map I'.Method to Base.Method, and not throw TypeLoadException
//
#ifdef FEATURE_COMINTEROP
VOID
MethodTableBuilder::PlaceMethodFromParentEquivalentInterfaceIntoInterfaceSlot(
    bmtInterfaceEntry::InterfaceSlotIterator & itfSlotIt,
    bmtInterfaceEntry *                        pCurItfEntry,
    DispatchMapTypeID **                       prgInterfaceDispatchMapTypeIDs,
    DWORD                                      dwCurInterface)
{
    STANDARD_VM_CONTRACT;

    bmtRTMethod * pCurItfMethod = itfSlotIt->Decl().AsRTMethod();

    if (itfSlotIt->Impl() != INVALID_SLOT_INDEX)
    {
        return;
    }

    // For every equivalent interface entry that was actually implemented by parent, then look at equivalent method slot on that entry
    // and if it matches and has a slot implementation, then record and continue
    for (DWORD dwEquivalentInterface = 0;
         (dwEquivalentInterface < bmtInterface->dwInterfaceMapSize) && (itfSlotIt->Impl() == INVALID_SLOT_INDEX);
         dwEquivalentInterface++)
    {
        bmtInterfaceEntry *  pEquivItfEntry = &bmtInterface->pInterfaceMap[dwEquivalentInterface];
        bmtRTType *          pEquivItf      = pEquivItfEntry->GetInterfaceType();
        MethodTable *        pEquivItfMT    = pEquivItf->GetMethodTable();
        const Substitution * pEquivItfSubst = &pEquivItf->GetSubstitution();
        if (pEquivItfEntry->GetInterfaceEquivalenceSet() != pCurItfEntry->GetInterfaceEquivalenceSet())
        {
            // Not equivalent
            continue;
        }
        if (!pEquivItfEntry->IsImplementedByParent())
        {
            // Not implemented by parent
            continue;
        }

        WORD slot = static_cast<WORD>(itfSlotIt.CurrentIndex());
        BOOL fFound = FALSE;

        // Determine which slot on the equivalent interface would map to the slot we are attempting to fill
        // in with an implementation.
        WORD otherMTSlot = GetEquivalentMethodSlot(pCurItfEntry->GetInterfaceType()->GetMethodTable(),
                                                   pEquivItfEntry->GetInterfaceType()->GetMethodTable(),
                                                   slot,
                                                   &fFound);

        if (fFound)
        {
            UINT32 cInterfaceDuplicates;
            if (*prgInterfaceDispatchMapTypeIDs == NULL)
            {
                *prgInterfaceDispatchMapTypeIDs =
                    new (GetStackingAllocator()) DispatchMapTypeID[bmtInterface->dwInterfaceMapSize];
            }

            // Compute all TypeIDs for this interface (all duplicates in the interface map)
            ComputeDispatchMapTypeIDs(
                pEquivItfMT,
                pEquivItfSubst,
                *prgInterfaceDispatchMapTypeIDs,
                bmtInterface->dwInterfaceMapSize,
                &cInterfaceDuplicates);
            // There cannot be more duplicates than number of interfaces
            _ASSERTE(cInterfaceDuplicates <= bmtInterface->dwInterfaceMapSize);
            _ASSERTE(cInterfaceDuplicates > 0);

            // NOTE: This override does not cache the resulting MethodData object
            MethodTable::MethodDataWrapper hParentData;
            hParentData = MethodTable::GetMethodData(
                    *prgInterfaceDispatchMapTypeIDs,
                    cInterfaceDuplicates,
                    pEquivItfMT,
                    GetParentMethodTable());

            SLOT_INDEX slotIndex = static_cast<SLOT_INDEX>
                (hParentData->GetImplSlotNumber(static_cast<UINT32>(otherMTSlot)));

            // Interface is implemented on parent abstract type and this particular slot was not implemented
            if (slotIndex == INVALID_SLOT_INDEX)
            {
                continue;
            }

            bmtMethodSlot & parentSlotImplementation = (*bmtParent->pSlotTable)[slotIndex];
            bmtMethodHandle & parentImplementation = parentSlotImplementation.Impl();

            // Check to verify that the equivalent slot on the equivalent interface actually matches the method
            // on the current interface. If not, then the slot is not a match, and we should search other interfaces
            // for an implementation of the method.
            if (!MethodSignature::SignaturesEquivalent(pCurItfMethod->GetMethodSignature(), parentImplementation.GetMethodSignature(), FALSE))
            {
                continue;
            }

            itfSlotIt->Impl() = slotIndex;

            MethodDesc * pMD = hParentData->GetImplMethodDesc(static_cast<UINT32>(otherMTSlot));

            DispatchMapTypeID dispatchMapTypeID =
                DispatchMapTypeID::InterfaceClassID(dwCurInterface);
            bmtVT->pDispatchMapBuilder->InsertMDMapping(
                dispatchMapTypeID,
                static_cast<UINT32>(itfSlotIt.CurrentIndex()),
                pMD,
                FALSE);
        }
    }
} // MethodTableBuilder::PlaceMethodFromParentEquivalentInterfaceIntoInterfaceSlot
#endif // FEATURE_COMINTEROP

//*******************************************************************************
//
// Used by BuildMethodTable
//
//
// If we are a class, then there may be some unplaced vtable methods (which are by definition
// interface methods, otherwise they'd already have been placed).  Place as many unplaced methods
// as possible, in the order preferred by interfaces.  However, do not allow any duplicates - once
// a method has been placed, it cannot be placed again - if we are unable to neatly place an interface,
// create duplicate slots for it starting at dwCurrentDuplicateVtableSlot.  Fill out the interface
// map for all interfaces as they are placed.
//
// If we are an interface, then all methods are already placed.  Fill out the interface map for
// interfaces as they are placed.
//
// BEHAVIOUR (based on Partition II: 11.2, not including MethodImpls)
//   C is current class, P is a parent class, I is the interface being implemented
//
//   FOREACH interface I implemented by this class C
//     FOREACH method I::M
//       IF I is EXPLICITLY implemented by C
//         IF some method C::M matches I::M
//           USE C::M as implementation for I::M
//         ELIF we inherit a method P::M that matches I::M
//           USE P::M as implementation for I::M
//         ENDIF
//       ELSE
//         IF I::M lacks implementation
//           IF some method C::M matches I::M
//             USE C::M as implementation for I::M
//           ELIF we inherit a method P::M that matches I::M
//             USE P::M as implementation for I::M
//           ELIF I::M was implemented by the parent type with method Parent::M
//             USE Parent::M for the implementation of I::M // VSD does this by default if we really
//                                                           // implemented I on the parent type, but
//                                                           // equivalent interfaces need to make this
//                                                           // explicit
//           ENDIF
//         ENDIF
//       ENDIF
//     ENDFOR
//   ENDFOR
//

VOID
MethodTableBuilder::PlaceInterfaceMethods()
{
    STANDARD_VM_CONTRACT;

    BOOL fParentInterface;
    DispatchMapTypeID * rgInterfaceDispatchMapTypeIDs = NULL;

    for (DWORD dwCurInterface = 0;
         dwCurInterface < bmtInterface->dwInterfaceMapSize;
         dwCurInterface++)
    {
        // Default to being implemented by the current class
        fParentInterface = FALSE;

        // Keep track of the current interface we are trying to place
        bmtInterfaceEntry *     pCurItfEntry = &bmtInterface->pInterfaceMap[dwCurInterface];
        bmtRTType *             pCurItf      = pCurItfEntry->GetInterfaceType();
        MethodTable *           pCurItfMT    = pCurItf->GetMethodTable();
        const Substitution *    pCurItfSubst = &pCurItf->GetSubstitution();

        //
        // There are three reasons why an interface could be in the implementation list
        // 1. Inherited from parent
        // 2. Explicitly declared in the implements list
        // 3. Implicitly declared through the implements list of an explicitly declared interface
        //
        // The reason these cases need to be distinguished is that an inherited interface that is
        // also explicitly redeclared in the implements list must be fully reimplemented using the
        // virtual methods of this type (thereby using matching methods in this type that may have
        // a different slot than an inherited method, but hidden it by name & sig); however all
        // implicitly redeclared interfaces should not be fully reimplemented if they were also
        // inherited from the parent.
        //
        // Example:
        //   interface I1 : I2
        //   class A : I1
        //   class B : A, I1
        //
        // In this example I1 must be fully reimplemented on B, but B can inherit the implementation
        // of I2.
        //

        if (pCurItfEntry->IsImplementedByParent())
        {
            if (!pCurItfEntry->IsDeclaredOnType())
            {
                fParentInterface = TRUE;
            }
        }

        bool fEquivalentInterfaceImplementedByParent = pCurItfEntry->IsImplementedByParent();
        bool fEquivalentInterfaceDeclaredOnType = pCurItfEntry->IsDeclaredOnType();

        if (pCurItfEntry->InEquivalenceSetWithMultipleEntries())
        {
            for (DWORD dwEquivalentInterface = 0;
                 dwEquivalentInterface < bmtInterface->dwInterfaceMapSize;
                 dwEquivalentInterface++)
            {
                bmtInterfaceEntry *     pEquivItfEntry = &bmtInterface->pInterfaceMap[dwEquivalentInterface];
                if (pEquivItfEntry->GetInterfaceEquivalenceSet() != pCurItfEntry->GetInterfaceEquivalenceSet())
                {
                    // Not equivalent
                    continue;
                }
                if (pEquivItfEntry->IsImplementedByParent())
                {
                    fEquivalentInterfaceImplementedByParent = true;
                }
                if (pEquivItfEntry->IsDeclaredOnType())
                {
                    fEquivalentInterfaceDeclaredOnType = true;
                }

                if (fEquivalentInterfaceDeclaredOnType && fEquivalentInterfaceImplementedByParent)
                    break;
            }
        }

        bool fParentInterfaceEquivalent = fEquivalentInterfaceImplementedByParent && !fEquivalentInterfaceDeclaredOnType;

        CONSISTENCY_CHECK(!fParentInterfaceEquivalent || HasParent());

        if (fParentInterfaceEquivalent)
        {
            // In the case the fParentInterface is TRUE, virtual overrides are enough and the interface
            // does not have to be explicitly (re)implemented. The only exception is if the parent is
            // abstract, in which case an inherited interface may not be fully implemented yet.
            // This is an optimization that allows us to skip the more expensive slot filling in below.
            // Note that the check here is for fParentInterface and not for fParentInterfaceEquivalent.
            // This is necessary as if the interface is not actually implemented on the parent type we will
            // need to fill in the slot table below.
            if (fParentInterface && !GetParentMethodTable()->IsAbstract())
            {
                continue;
            }

            {
                // We will reach here in two cases.
                // 1 .The parent is abstract and the interface has been declared on the parent,
                // and possibly partially implemented, so we need to populate the
                // bmtInterfaceSlotImpl table for this interface with the implementation slot
                // information.
                // 2 .The the interface has not been declared on the parent,
                // but an equivalent interface has been. So we need to populate the
                // bmtInterfaceSlotImpl table for this interface with the implementation slot
                // information from one of the parent equivalent interfaces. We may or may not
                // find implementations for all of the methods on the interface on the parent type.
                // The parent type may or may not be abstract.

                MethodTable::MethodDataWrapper hParentData;
                CONSISTENCY_CHECK(CheckPointer(GetParentMethodTable()));

                if (rgInterfaceDispatchMapTypeIDs == NULL)
                {
                    rgInterfaceDispatchMapTypeIDs =
                        new (GetStackingAllocator()) DispatchMapTypeID[bmtInterface->dwInterfaceMapSize];
                }

                if (pCurItfEntry->IsImplementedByParent())
                {
                    UINT32 cInterfaceDuplicates;
                    // Compute all TypeIDs for this interface (all duplicates in the interface map)
                    ComputeDispatchMapTypeIDs(
                        pCurItfMT,
                        pCurItfSubst,
                        rgInterfaceDispatchMapTypeIDs,
                        bmtInterface->dwInterfaceMapSize,
                        &cInterfaceDuplicates);
                    // There cannot be more duplicates than number of interfaces
                    _ASSERTE(cInterfaceDuplicates <= bmtInterface->dwInterfaceMapSize);
                    _ASSERTE(cInterfaceDuplicates > 0);

                    //#InterfaceMap_UseParentInterfaceImplementations
                    // We rely on the fact that interface map of parent type is subset of this type (incl.
                    // duplicates), see code:#InterfaceMap_SupersetOfParent
                    // NOTE: This override does not cache the resulting MethodData object
                    hParentData = MethodTable::GetMethodData(
                            rgInterfaceDispatchMapTypeIDs,
                            cInterfaceDuplicates,
                            pCurItfMT,
                            GetParentMethodTable());

                    bmtInterfaceEntry::InterfaceSlotIterator itfSlotIt =
                        pCurItfEntry->IterateInterfaceSlots(GetStackingAllocator());
                    for (; !itfSlotIt.AtEnd(); itfSlotIt.Next())
                    {
                        itfSlotIt->Impl() = static_cast<SLOT_INDEX>
                            (hParentData->GetImplSlotNumber(static_cast<UINT32>(itfSlotIt.CurrentIndex())));
                    }
                }
#ifdef FEATURE_COMINTEROP
                else
                {
                    // Iterate through the methods on the interface, and if they have a slot which was filled in
                    // on an equivalent interface inherited from the parent fill in the appropriate slot.
                    // This code path is only used when there is an implicit implementation of an interface
                    // that was not implemented on a parent type, but there was an equivalent interface implemented
                    // on a parent type.
                    bmtInterfaceEntry::InterfaceSlotIterator itfSlotIt =
                        pCurItfEntry->IterateInterfaceSlots(GetStackingAllocator());
                    for (; !itfSlotIt.AtEnd(); itfSlotIt.Next())
                    {
                        PlaceMethodFromParentEquivalentInterfaceIntoInterfaceSlot(itfSlotIt, pCurItfEntry, &rgInterfaceDispatchMapTypeIDs, dwCurInterface);
                    }
                }
#endif // FEATURE_COMINTEROP
            }
        }

        // For each method declared in this interface
        bmtInterfaceEntry::InterfaceSlotIterator itfSlotIt =
            pCurItfEntry->IterateInterfaceSlots(GetStackingAllocator());
        for (; !itfSlotIt.AtEnd(); ++itfSlotIt)
        {
            if (fParentInterfaceEquivalent)
            {
                if (itfSlotIt->Impl() != INVALID_SLOT_INDEX)
                {   // If this interface is not explicitly declared on this class, and the interface slot has already been
                    // given an implementation, then the only way to provide a new implementation is through an override
                    // or through a MethodImpl. This is necessary in addition to the continue statement before this for
                    // loop because an abstract interface can still have a partial implementation and it is necessary to
                    // skip those interface slots that have already been satisfied.
                    continue;
                }
            }

            BOOL                    fFoundMatchInBuildingClass = FALSE;
            bmtInterfaceSlotImpl &  curItfSlot = *itfSlotIt;
            bmtRTMethod *           pCurItfMethod = curItfSlot.Decl().AsRTMethod();
            const MethodSignature & curItfMethodSig = pCurItfMethod->GetMethodSignature();

            //
            // First, try to find the method explicitly declared in our class
            //

            DeclaredMethodIterator methIt(*this);
            while (methIt.Next())
            {
                // Note that non-publics can legally be exposed via an interface, but only
                // through methodImpls.
                if (IsMdVirtual(methIt.Attrs()) && IsMdPublic(methIt.Attrs()))
                {
#ifdef _DEBUG
                    if(GetHalfBakedClass()->m_fDebuggingClass && g_pConfig->ShouldBreakOnMethod(methIt.Name()))
                        CONSISTENCY_CHECK_MSGF(false, ("BreakOnMethodName: '%s' ", methIt.Name()));
#endif // _DEBUG

                    if (pCurItfMethod->GetMethodSignature().Equivalent(methIt->GetMethodSignature()))
                    {
                        fFoundMatchInBuildingClass = TRUE;
                        curItfSlot.Impl() = methIt->GetSlotIndex();

                        DispatchMapTypeID dispatchMapTypeID =
                            DispatchMapTypeID::InterfaceClassID(dwCurInterface);
                        bmtVT->pDispatchMapBuilder->InsertMDMapping(
                            dispatchMapTypeID,
                            static_cast<UINT32>(itfSlotIt.CurrentIndex()),
                            methIt->GetMethodDesc(),
                            FALSE);

                        break;
                    }
                }
            } // end ... try to find method

            //
            // The ECMA CLR spec states that a type will inherit interface implementations
            // and that explicit re-declaration of an inherited interface will try to match
            // only newslot methods with methods in the re-declared interface (note that
            // this also takes care of matching against unsatisfied interface methods in
            // the abstract parent type scenario).
            //
            // So, if the interface was not declared on a parent and we haven't found a
            // newslot method declared on this type as a match, search all remaining
            // public virtual methods (including overrides declared on this type) for a
            // match.
            //
            // Please see bug VSW577403 and VSW593884 for details of this breaking change.
            //
            if (!fFoundMatchInBuildingClass &&
                !fEquivalentInterfaceImplementedByParent)
            {
                if (HasParent())
                {
                    // Iterate backward through the parent's method table. This is important to
                    // find the most derived method.
                    bmtParentInfo::Iterator parentMethodIt = bmtParent->IterateSlots();
                    parentMethodIt.ResetToEnd();
                    while (parentMethodIt.Prev())
                    {
                        bmtRTMethod * pCurParentMethod = parentMethodIt->Decl().AsRTMethod();
                        DWORD dwAttrs = pCurParentMethod->GetDeclAttrs();
                        if (!IsMdVirtual(dwAttrs) || !IsMdPublic(dwAttrs))
                        {   // Only match mdPublic mdVirtual methods for interface implementation
                            continue;
                        }

                        if (curItfMethodSig.Equivalent(pCurParentMethod->GetMethodSignature()))
                        {
                            fFoundMatchInBuildingClass = TRUE;
                            curItfSlot.Impl() = pCurParentMethod->GetSlotIndex();

                            DispatchMapTypeID dispatchMapTypeID =
                                DispatchMapTypeID::InterfaceClassID(dwCurInterface);
                            bmtVT->pDispatchMapBuilder->InsertMDMapping(
                                dispatchMapTypeID,
                                static_cast<UINT32>(itfSlotIt.CurrentIndex()),
                                pCurParentMethod->GetMethodDesc(),
                                FALSE);

                            break;
                        }
                    } // end ... try to find parent method
                }
            }

            // For type equivalent interfaces that had an equivalent interface implemented by their parent
            // and where the previous logic to fill in the method based on the virtual mappings on the type have
            // failed, we should attempt to get the mappings from the equivalent interfaces declared on parent types
            // of the type we are currently building.
#ifdef FEATURE_COMINTEROP
            if (!fFoundMatchInBuildingClass && fEquivalentInterfaceImplementedByParent && !pCurItfEntry->IsImplementedByParent())
            {
                PlaceMethodFromParentEquivalentInterfaceIntoInterfaceSlot(itfSlotIt, pCurItfEntry, &rgInterfaceDispatchMapTypeIDs, dwCurInterface);
            }
#endif
        }
    }
} // MethodTableBuilder::PlaceInterfaceMethods


//*******************************************************************************
//
// Used by BuildMethodTable
//
// Place static fields
//
VOID MethodTableBuilder::PlaceRegularStaticFields()
{
    STANDARD_VM_CONTRACT;

    DWORD i;

    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Placing statics for %s\n", this->GetDebugClassName()));

    //
    // Place gc refs and value types first, as they need to have handles created for them.
    // (Placing them together allows us to easily create the handles when Restoring the class,
    // and when initializing new DLS for the class.)
    //

    DWORD   dwCumulativeStaticFieldPos = 0 ;
    DWORD   dwCumulativeStaticGCFieldPos = 0;
    DWORD   dwCumulativeStaticBoxFieldPos = 0;

    // We don't need to do any calculations for the gc refs or valuetypes, as they're
    // guaranteed to be aligned in ModuleStaticsInfo
    bmtFP->NumRegularStaticFieldsOfSize[LOG2_PTRSIZE] -=
        bmtFP->NumRegularStaticGCBoxedFields + bmtFP->NumRegularStaticGCPointerFields;

    // Place fields, largest first, padding so that each group is aligned to its natural size
    for (i = MAX_LOG2_PRIMITIVE_FIELD_SIZE; (signed int) i >= 0; i--)
    {
        // Fields of this size start at the next available location
        bmtFP->RegularStaticFieldStart[i] = dwCumulativeStaticFieldPos;
        dwCumulativeStaticFieldPos += (bmtFP->NumRegularStaticFieldsOfSize[i] << i);

        // Reset counters for the loop after this one
        bmtFP->NumRegularStaticFieldsOfSize[i]    = 0;
    }


    if (dwCumulativeStaticFieldPos > FIELD_OFFSET_LAST_REAL_OFFSET)
        BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);

    DWORD dwNumHandleStatics = bmtFP->NumRegularStaticGCBoxedFields + bmtFP->NumRegularStaticGCPointerFields;
    if (!FitsIn<WORD>(dwNumHandleStatics))
    {   // Overflow.
        BuildMethodTableThrowException(IDS_EE_TOOMANYFIELDS);
    }
    SetNumHandleRegularStatics(static_cast<WORD>(dwNumHandleStatics));

    if (!FitsIn<WORD>(bmtFP->NumRegularStaticGCBoxedFields))
    {   // Overflow.
        BuildMethodTableThrowException(IDS_EE_TOOMANYFIELDS);
    }
    SetNumBoxedRegularStatics(static_cast<WORD>(bmtFP->NumRegularStaticGCBoxedFields));

    // Tell the module to give us the offsets we'll be using and commit space for us
    // if necessary
    DWORD dwNonGCOffset, dwGCOffset;
    GetModule()->GetOffsetsForRegularStaticData(bmtInternal->pType->GetTypeDefToken(),
                                                bmtProp->fDynamicStatics,
                                                GetNumHandleRegularStatics(), dwCumulativeStaticFieldPos,
                                                &dwGCOffset, &dwNonGCOffset);

    // Allocate boxed statics first ("x << LOG2_PTRSIZE" is equivalent to "x * sizeof(void *)")
    dwCumulativeStaticGCFieldPos = bmtFP->NumRegularStaticGCBoxedFields<<LOG2_PTRSIZE;

    FieldDesc *pFieldDescList = GetApproxFieldDescListRaw();
    // Place static fields
    for (i = 0; i < bmtEnumFields->dwNumStaticFields - bmtEnumFields->dwNumThreadStaticFields; i++)
    {
        FieldDesc * pCurField   = &pFieldDescList[bmtEnumFields->dwNumInstanceFields+i];
        DWORD dwLog2FieldSize   = (DWORD)(DWORD_PTR&)pCurField->m_pMTOfEnclosingClass; // log2(field size)
        DWORD dwOffset          = (DWORD) pCurField->m_dwOffset; // offset or type of field

        switch (dwOffset)
        {
        case FIELD_OFFSET_UNPLACED_GC_PTR:
            // Place GC reference static field
            pCurField->SetOffset(dwCumulativeStaticGCFieldPos + dwGCOffset);
            dwCumulativeStaticGCFieldPos += 1<<LOG2_PTRSIZE;
            LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Field placed at GC offset 0x%x\n", pCurField->GetOffset_NoLogging()));

            break;

        case FIELD_OFFSET_VALUE_CLASS:
            // Place boxed GC reference static field
            pCurField->SetOffset(dwCumulativeStaticBoxFieldPos + dwGCOffset);
            dwCumulativeStaticBoxFieldPos += 1<<LOG2_PTRSIZE;
            LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Field placed at GC offset 0x%x\n", pCurField->GetOffset_NoLogging()));

            break;

        case FIELD_OFFSET_UNPLACED:
            // Place non-GC static field
            pCurField->SetOffset(bmtFP->RegularStaticFieldStart[dwLog2FieldSize] +
                                 (bmtFP->NumRegularStaticFieldsOfSize[dwLog2FieldSize] << dwLog2FieldSize) +
                                 dwNonGCOffset);
            bmtFP->NumRegularStaticFieldsOfSize[dwLog2FieldSize]++;
            LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Field placed at non GC offset 0x%x\n", pCurField->GetOffset_NoLogging()));
            break;

        default:
            // RVA field
            break;
        }

        LOG((LF_CLASSLOADER, LL_INFO1000000, "Offset of %s: %i\n", pCurField->m_debugName, pCurField->GetOffset_NoLogging()));
    }

    if (bmtProp->fDynamicStatics)
    {
        _ASSERTE(dwNonGCOffset == 0 ||  // no statics at all
                 dwNonGCOffset == OFFSETOF__DomainLocalModule__NormalDynamicEntry__m_pDataBlob); // We need space to point to the GC statics
        bmtProp->dwNonGCRegularStaticFieldBytes = dwCumulativeStaticFieldPos;
    }
    else
    {
        bmtProp->dwNonGCRegularStaticFieldBytes = 0; // Non dynamics shouldnt be using this
    }
    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Static field bytes needed (0 is normal for non dynamic case)%i\n", bmtProp->dwNonGCRegularStaticFieldBytes));
}


VOID MethodTableBuilder::PlaceThreadStaticFields()
{
    STANDARD_VM_CONTRACT;

    DWORD i;

    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Placing ThreadStatics for %s\n", this->GetDebugClassName()));

    //
    // Place gc refs and value types first, as they need to have handles created for them.
    // (Placing them together allows us to easily create the handles when Restoring the class,
    // and when initializing new DLS for the class.)
    //

    DWORD   dwCumulativeStaticFieldPos = 0 ;
    DWORD   dwCumulativeStaticGCFieldPos = 0;
    DWORD   dwCumulativeStaticBoxFieldPos = 0;

    // We don't need to do any calculations for the gc refs or valuetypes, as they're
    // guaranteed to be aligned in ModuleStaticsInfo
    bmtFP->NumThreadStaticFieldsOfSize[LOG2_PTRSIZE] -=
        bmtFP->NumThreadStaticGCBoxedFields + bmtFP->NumThreadStaticGCPointerFields;

    // Place fields, largest first, padding so that each group is aligned to its natural size
    for (i = MAX_LOG2_PRIMITIVE_FIELD_SIZE; (signed int) i >= 0; i--)
    {
        // Fields of this size start at the next available location
        bmtFP->ThreadStaticFieldStart[i] = dwCumulativeStaticFieldPos;
        dwCumulativeStaticFieldPos += (bmtFP->NumThreadStaticFieldsOfSize[i] << i);

        // Reset counters for the loop after this one
        bmtFP->NumThreadStaticFieldsOfSize[i]    = 0;
    }


    if (dwCumulativeStaticFieldPos > FIELD_OFFSET_LAST_REAL_OFFSET)
        BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);

    DWORD dwNumHandleStatics = bmtFP->NumThreadStaticGCBoxedFields + bmtFP->NumThreadStaticGCPointerFields;
    if (!FitsIn<WORD>(dwNumHandleStatics))
    {   // Overflow.
        BuildMethodTableThrowException(IDS_EE_TOOMANYFIELDS);
    }

    SetNumHandleThreadStatics(static_cast<WORD>(dwNumHandleStatics));

    if (!FitsIn<WORD>(bmtFP->NumThreadStaticGCBoxedFields))
    {   // Overflow.
        BuildMethodTableThrowException(IDS_EE_TOOMANYFIELDS);
    }

    SetNumBoxedThreadStatics(static_cast<WORD>(bmtFP->NumThreadStaticGCBoxedFields));

    // Tell the module to give us the offsets we'll be using and commit space for us
    // if necessary
    DWORD dwNonGCOffset, dwGCOffset;

    GetModule()->GetOffsetsForThreadStaticData(bmtInternal->pType->GetTypeDefToken(),
                                               bmtProp->fDynamicStatics,
                                               GetNumHandleThreadStatics(), dwCumulativeStaticFieldPos,
                                               &dwGCOffset, &dwNonGCOffset);

    // Allocate boxed statics first ("x << LOG2_PTRSIZE" is equivalent to "x * sizeof(void *)")
    dwCumulativeStaticGCFieldPos = bmtFP->NumThreadStaticGCBoxedFields<<LOG2_PTRSIZE;

    FieldDesc *pFieldDescList = GetHalfBakedClass()->GetFieldDescList();
    // Place static fields
    for (i = 0; i < bmtEnumFields->dwNumThreadStaticFields; i++)
    {
        FieldDesc * pCurField   = &pFieldDescList[bmtEnumFields->dwNumInstanceFields + bmtEnumFields->dwNumStaticFields - bmtEnumFields->dwNumThreadStaticFields + i];
        DWORD dwLog2FieldSize   = (DWORD)(DWORD_PTR&)pCurField->m_pMTOfEnclosingClass; // log2(field size)
        DWORD dwOffset          = (DWORD) pCurField->m_dwOffset; // offset or type of field

        switch (dwOffset)
        {
        case FIELD_OFFSET_UNPLACED_GC_PTR:
            // Place GC reference static field
            pCurField->SetOffset(dwCumulativeStaticGCFieldPos + dwGCOffset);
            dwCumulativeStaticGCFieldPos += 1<<LOG2_PTRSIZE;
            LOG((LF_CLASSLOADER, LL_INFO10000, "THREAD STATICS: Field placed at GC offset 0x%x\n", pCurField->GetOffset_NoLogging()));

            break;

        case FIELD_OFFSET_VALUE_CLASS:
            // Place boxed GC reference static field
            pCurField->SetOffset(dwCumulativeStaticBoxFieldPos + dwGCOffset);
            dwCumulativeStaticBoxFieldPos += 1<<LOG2_PTRSIZE;
            LOG((LF_CLASSLOADER, LL_INFO10000, "THREAD STATICS: Field placed at GC offset 0x%x\n", pCurField->GetOffset_NoLogging()));

            break;

        case FIELD_OFFSET_UNPLACED:
            // Place non-GC static field
            pCurField->SetOffset(bmtFP->ThreadStaticFieldStart[dwLog2FieldSize] +
                                 (bmtFP->NumThreadStaticFieldsOfSize[dwLog2FieldSize] << dwLog2FieldSize) +
                                 dwNonGCOffset);
            bmtFP->NumThreadStaticFieldsOfSize[dwLog2FieldSize]++;
            LOG((LF_CLASSLOADER, LL_INFO10000, "THREAD STATICS: Field placed at non GC offset 0x%x\n", pCurField->GetOffset_NoLogging()));
            break;

        default:
            // RVA field
            break;
        }

        LOG((LF_CLASSLOADER, LL_INFO1000000, "Offset of %s: %i\n", pCurField->m_debugName, pCurField->GetOffset_NoLogging()));
    }

    if (bmtProp->fDynamicStatics)
    {
        _ASSERTE(dwNonGCOffset == 0 ||  // no thread statics at all
                 dwNonGCOffset == OFFSETOF__ThreadLocalModule__DynamicEntry__m_pDataBlob); // We need space to point to the GC statics
        bmtProp->dwNonGCThreadStaticFieldBytes = dwCumulativeStaticFieldPos;
    }
    else
    {
        bmtProp->dwNonGCThreadStaticFieldBytes = 0; // Non dynamics shouldnt be using this
    }
    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: ThreadStatic field bytes needed (0 is normal for non dynamic case)%i\n", bmtProp->dwNonGCThreadStaticFieldBytes));
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Place instance fields
//
VOID    MethodTableBuilder::PlaceInstanceFields(MethodTable ** pByValueClassCache)
{
    STANDARD_VM_CONTRACT;


    DWORD i;

        //===============================================================
        // BEGIN: Place instance fields
        //===============================================================

        FieldDesc *pFieldDescList = GetHalfBakedClass()->GetFieldDescList();
        DWORD   dwCumulativeInstanceFieldPos;

        // Instance fields start right after the parent
        dwCumulativeInstanceFieldPos    = HasParent() ? GetParentMethodTable()->GetNumInstanceFieldBytes() : 0;

        DWORD dwOffsetBias = 0;
#ifdef FEATURE_64BIT_ALIGNMENT
        // On platforms where the alignment of 64-bit primitives is a requirement (but we're not guaranteed
        // this implicitly by the GC) field offset 0 is actually not 8-byte aligned in reference classes.
        // That's because all such platforms are currently 32-bit and the 4-byte MethodTable pointer pushes us
        // out of alignment. Ideally we'd solve this by arranging to have the object header allocated at a
        // 4-byte offset from an 8-byte boundary, but this is difficult to achieve for objects allocated on
        // the large object heap (which actually requires headers to be 8-byte aligned).
        //
        // So we adjust dwCumulativeInstanceFieldPos to account for the MethodTable* and our alignment
        // calculations will automatically adjust and add padding as necessary. We need to remove this
        // adjustment when setting the field offset in the field desc, however, since the rest of the system
        // expects that value to not include the MethodTable*.
        //
        // This happens only for reference classes: value type field 0 really does lie at offset 0 for unboxed
        // value types. We deal with boxed value types by allocating their headers mis-aligned (luckily for us
        // value types can never get large enough to allocate on the LOH).
        if (!IsValueClass())
        {
            dwOffsetBias = TARGET_POINTER_SIZE;
            dwCumulativeInstanceFieldPos += dwOffsetBias;
        }
#endif // FEATURE_64BIT_ALIGNMENT

#ifdef FEATURE_READYTORUN
        if (NeedsAlignedBaseOffset())
        {
            // READYTORUN: FUTURE: Use the minimum possible alignment, reduce padding when inheriting within same bubble
            DWORD dwAlignment = DATA_ALIGNMENT;
#ifdef FEATURE_64BIT_ALIGNMENT
            if (GetHalfBakedClass()->IsAlign8Candidate())
                dwAlignment = 8;
#endif
            dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, dwAlignment);
        }
#endif // FEATURE_READYTORUN

        // place small fields first if the parent have a number of field bytes that is not aligned
        if (!IS_ALIGNED(dwCumulativeInstanceFieldPos, DATA_ALIGNMENT))
        {
            for (i = 0; i < MAX_LOG2_PRIMITIVE_FIELD_SIZE; i++) {
                DWORD j;

                if (IS_ALIGNED(dwCumulativeInstanceFieldPos, size_t{ 1 } << (i + 1)))
                    continue;

                // check whether there are any bigger fields
                for (j = i + 1; j <= MAX_LOG2_PRIMITIVE_FIELD_SIZE; j++) {
                    if (bmtFP->NumInstanceFieldsOfSize[j] != 0)
                        break;
                }
                // nothing to gain if there are no bigger fields
                // (the subsequent loop will place fields from large to small fields)
                if (j > MAX_LOG2_PRIMITIVE_FIELD_SIZE)
                    break;

                // check whether there are any small enough fields
                for (j = i; (signed int) j >= 0; j--) {
                    if (bmtFP->NumInstanceFieldsOfSize[j] != 0)
                        break;
                    // TODO: since we will refuse to place GC references we should filter them out here.
                    // otherwise the "back-filling" process stops completely.
                    // (PlaceInstanceFields)
                    // the following code would fix the issue (a replacement for the code above this comment):
                    // if (bmtFP->NumInstanceFieldsOfSize[j] != 0 &&
                    //     (j != LOG2SLOT || bmtFP->NumInstanceFieldsOfSize[j] > bmtFP->NumInstanceGCPointerFields))
                    // {
                    //     break;
                    // }

                }
                // nothing to play with if there are no smaller fields
                if ((signed int) j < 0)
                    break;
                // eventually go back and use the smaller field as filling
                i = j;

                CONSISTENCY_CHECK(bmtFP->NumInstanceFieldsOfSize[i] != 0);

                j = bmtFP->FirstInstanceFieldOfSize[i];

                // Avoid reordering of gcfields
                if (i == LOG2SLOT) {
                    for ( ; j < bmtEnumFields->dwNumInstanceFields; j++) {
                        if ((pFieldDescList[j].GetOffset_NoLogging() == FIELD_OFFSET_UNPLACED) &&
                            ((DWORD_PTR&)pFieldDescList[j].m_pMTOfEnclosingClass == (size_t)i))
                            break;
                    }

                    // out of luck - can't reorder gc fields
                    if (j >= bmtEnumFields->dwNumInstanceFields)
                        break;
                }

                // Place the field
                dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, size_t{ 1 } << i);

                pFieldDescList[j].SetOffset(dwCumulativeInstanceFieldPos - dwOffsetBias);
                dwCumulativeInstanceFieldPos += (1 << i);

                // We've placed this field now, so there is now one less of this size field to place
                if (--bmtFP->NumInstanceFieldsOfSize[i] == 0)
                    continue;

                // We are done in this round if we haven't picked the first field
                if (bmtFP->FirstInstanceFieldOfSize[i] != j)
                    continue;

                // Update FirstInstanceFieldOfSize[i] to point to the next such field
                for (j = j+1; j < bmtEnumFields->dwNumInstanceFields; j++)
                {
                    // The log of the field size is stored in the method table
                    if ((DWORD_PTR&)pFieldDescList[j].m_pMTOfEnclosingClass == (size_t)i)
                    {
                        bmtFP->FirstInstanceFieldOfSize[i] = j;
                        break;
                    }
                }
                _ASSERTE(j < bmtEnumFields->dwNumInstanceFields);
            }
        }

        // Place fields, largest first
        for (i = MAX_LOG2_PRIMITIVE_FIELD_SIZE; (signed int) i >= 0; i--)
        {
            if (bmtFP->NumInstanceFieldsOfSize[i] == 0)
                continue;

            // Align instance fields if we aren't already
#if defined(TARGET_X86) && defined(UNIX_X86_ABI)
            DWORD dwDataAlignment = min(1 << i, DATA_ALIGNMENT);
#else
            DWORD dwDataAlignment = 1 << i;
#endif
            dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, dwDataAlignment);

            // Fields of this size start at the next available location
            bmtFP->InstanceFieldStart[i] = dwCumulativeInstanceFieldPos;
            dwCumulativeInstanceFieldPos += (bmtFP->NumInstanceFieldsOfSize[i] << i);

            // Reset counters for the loop after this one
            bmtFP->NumInstanceFieldsOfSize[i]  = 0;
        }


        // Make corrections to reserve space for GC Pointer Fields
        //
        // The GC Pointers simply take up the top part of the region associated
        // with fields of that size (GC pointers can be 64 bit on certain systems)
        if (bmtFP->NumInstanceGCPointerFields)
        {
            bmtFP->GCPointerFieldStart = bmtFP->InstanceFieldStart[LOG2SLOT] - dwOffsetBias;
            bmtFP->InstanceFieldStart[LOG2SLOT] = bmtFP->InstanceFieldStart[LOG2SLOT] + (bmtFP->NumInstanceGCPointerFields << LOG2SLOT);
            bmtFP->NumInstanceGCPointerFields = 0;     // reset to zero here, counts up as pointer slots are assigned below
        }

        // Place instance fields - be careful not to place any already-placed fields
        for (i = 0; i < bmtEnumFields->dwNumInstanceFields; i++)
        {
            DWORD dwFieldSize   = (DWORD)(DWORD_PTR&)pFieldDescList[i].m_pMTOfEnclosingClass;
            DWORD dwOffset;

            dwOffset = pFieldDescList[i].GetOffset_NoLogging();

            // Don't place already-placed fields
            if ((dwOffset == FIELD_OFFSET_UNPLACED || dwOffset == FIELD_OFFSET_UNPLACED_GC_PTR || dwOffset == FIELD_OFFSET_VALUE_CLASS))
            {
                if (dwOffset == FIELD_OFFSET_UNPLACED_GC_PTR)
                {
                    pFieldDescList[i].SetOffset(bmtFP->GCPointerFieldStart + (bmtFP->NumInstanceGCPointerFields << LOG2SLOT));
                    bmtFP->NumInstanceGCPointerFields++;
                }
                else if (pFieldDescList[i].IsByValue() == FALSE) // it's a regular field
                {
                    pFieldDescList[i].SetOffset(bmtFP->InstanceFieldStart[dwFieldSize] + (bmtFP->NumInstanceFieldsOfSize[dwFieldSize] << dwFieldSize) - dwOffsetBias);
                    bmtFP->NumInstanceFieldsOfSize[dwFieldSize]++;
                }
            }
        }

        DWORD dwNumGCPointerSeries;
        // Save Number of pointer series
        if (bmtFP->NumInstanceGCPointerFields)
            dwNumGCPointerSeries = bmtParent->NumParentPointerSeries + 1;
        else
            dwNumGCPointerSeries = bmtParent->NumParentPointerSeries;

        bool containsGCPointers = bmtFP->NumInstanceGCPointerFields > 0;
        // Place by value class fields last
        // Update the number of GC pointer series
        // Calculate largest alignment requirement
        int largestAlignmentRequirement = 1;
        for (i = 0; i < bmtEnumFields->dwNumInstanceFields; i++)
        {
            if (pFieldDescList[i].IsByValue())
            {
                MethodTable * pByValueMT = pByValueClassCache[i];

#if !defined(TARGET_64BIT) && (DATA_ALIGNMENT > 4)
                if (pByValueMT->GetNumInstanceFieldBytes() >= DATA_ALIGNMENT)
                {
                    dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, DATA_ALIGNMENT);
                    largestAlignmentRequirement = max(largestAlignmentRequirement, DATA_ALIGNMENT);
                }
                else
#elif defined(FEATURE_64BIT_ALIGNMENT)
                if (pByValueMT->RequiresAlign8())
                {
                    dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, 8);
                    largestAlignmentRequirement = max(largestAlignmentRequirement, 8);
                }
                else
#endif // FEATURE_64BIT_ALIGNMENT
                if (pByValueMT->ContainsPointers())
                {
                    // this field type has GC pointers in it, which need to be pointer-size aligned
                    // so do this if it has not been done already
                    dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, TARGET_POINTER_SIZE);
                    largestAlignmentRequirement = max(largestAlignmentRequirement, TARGET_POINTER_SIZE);
                    containsGCPointers = true;
                }
                else
                {
                    int fieldAlignmentRequirement = pByValueMT->GetFieldAlignmentRequirement();
                    largestAlignmentRequirement = max(largestAlignmentRequirement, fieldAlignmentRequirement);
                    dwCumulativeInstanceFieldPos = (DWORD)ALIGN_UP(dwCumulativeInstanceFieldPos, fieldAlignmentRequirement);
                }

                pFieldDescList[i].SetOffset(dwCumulativeInstanceFieldPos - dwOffsetBias);
                dwCumulativeInstanceFieldPos += pByValueMT->GetNumInstanceFieldBytes();

                if (pByValueMT->ContainsPointers())
                {
                    // Add pointer series for by-value classes
                    dwNumGCPointerSeries += (DWORD)CGCDesc::GetCGCDescFromMT(pByValueMT)->GetNumSeries();
                }
            }
            else
            {
                // non-value-type fields always require pointer alignment
                // This does not account for types that are marked IsAlign8Candidate due to 8-byte fields
                // but that is explicitly handled when we calculate the final alignment for the type.
                largestAlignmentRequirement = max(largestAlignmentRequirement, TARGET_POINTER_SIZE);
            }
        }

            // Can be unaligned
        DWORD dwNumInstanceFieldBytes = dwCumulativeInstanceFieldPos - dwOffsetBias;

        if (IsValueClass())
        {
                 // Like C++ we enforce that there can be no 0 length structures.
                // Thus for a value class with no fields, we 'pad' the length to be 1
            if (dwNumInstanceFieldBytes == 0)
                dwNumInstanceFieldBytes = 1;

                // The JITs like to copy full machine words,
                //  so if the size is bigger than a void* round it up to minAlign
                // and if the size is smaller than void* round it up to next power of two
            unsigned minAlign;

#ifdef FEATURE_64BIT_ALIGNMENT
            if (GetHalfBakedClass()->IsAlign8Candidate()) {
                minAlign = 8;
            }
            else
#endif // FEATURE_64BIT_ALIGNMENT
            if (dwNumInstanceFieldBytes > TARGET_POINTER_SIZE) {
                minAlign = containsGCPointers ? TARGET_POINTER_SIZE : (unsigned)largestAlignmentRequirement;
            }
            else {
                minAlign = 1;
                while (minAlign < dwNumInstanceFieldBytes)
                    minAlign *= 2;
            }

            if (minAlign != min(dwNumInstanceFieldBytes, TARGET_POINTER_SIZE))
            {
                EnsureOptionalFieldsAreAllocated(GetHalfBakedClass(), m_pAllocMemTracker, GetLoaderAllocator()->GetLowFrequencyHeap());
                GetHalfBakedClass()->GetOptionalFields()->m_requiredFieldAlignment = (BYTE)minAlign;
                GetHalfBakedClass()->SetHasCustomFieldAlignment();
            }

            dwNumInstanceFieldBytes = (dwNumInstanceFieldBytes + minAlign-1) & ~(minAlign-1);
        }

        if (dwNumInstanceFieldBytes > FIELD_OFFSET_LAST_REAL_OFFSET) {
            BuildMethodTableThrowException(IDS_CLASSLOAD_FIELDTOOLARGE);
        }

        bmtFP->NumInstanceFieldBytes = dwNumInstanceFieldBytes;

        bmtFP->NumGCPointerSeries = dwNumGCPointerSeries;

        //===============================================================
        // END: Place instance fields
        //===============================================================
}

//*******************************************************************************
// this accesses the field size which is temporarily stored in m_pMTOfEnclosingClass
// during class loading. Don't use any other time
DWORD MethodTableBuilder::GetFieldSize(FieldDesc *pFD)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

        // We should only be calling this while this class is being built.
    _ASSERTE(GetHalfBakedMethodTable() == 0);
    BAD_FORMAT_NOTHROW_ASSERT(! pFD->IsByValue() || HasExplicitFieldOffsetLayout());

    if (pFD->IsByValue())
        return (DWORD)(DWORD_PTR&)(pFD->m_pMTOfEnclosingClass);
    return (1 << (DWORD)(DWORD_PTR&)(pFD->m_pMTOfEnclosingClass));
}

#ifdef UNIX_AMD64_ABI
// checks whether the struct is enregisterable.
void MethodTableBuilder::SystemVAmd64CheckForPassStructInRegister()
{
    STANDARD_VM_CONTRACT;

    // This method should be called for valuetypes only
    _ASSERTE(IsValueClass());

    TypeHandle th(GetHalfBakedMethodTable());

    if (th.IsTypeDesc())
    {
        // Not an enregisterable managed structure.
        return;
    }

    DWORD totalStructSize = bmtFP->NumInstanceFieldBytes;

    // If num of bytes for the fields is bigger than CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS
    // pass through stack
    if (totalStructSize > CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS)
    {
        LOG((LF_JIT, LL_EVERYTHING, "**** SystemVAmd64CheckForPassStructInRegister: struct %s is too big to pass in registers (%d bytes)\n",
               this->GetDebugClassName(), totalStructSize));
        return;
    }

    const bool useNativeLayout = false;
    // Iterate through the fields and make sure they meet requirements to pass in registers
    SystemVStructRegisterPassingHelper helper((unsigned int)totalStructSize);
    if (GetHalfBakedMethodTable()->ClassifyEightBytes(&helper, 0, 0, useNativeLayout))
    {
        // All the above tests passed. It's registers passed struct!
        GetHalfBakedMethodTable()->SetRegPassedStruct();

        StoreEightByteClassification(&helper);
    }
}

// Store the eightbyte classification into the EEClass
void MethodTableBuilder::StoreEightByteClassification(SystemVStructRegisterPassingHelper* helper)
{
    EEClass* eeClass = GetHalfBakedMethodTable()->GetClass();
    LoaderAllocator* pAllocator = MethodTableBuilder::GetLoaderAllocator();
    AllocMemTracker* pamTracker = MethodTableBuilder::GetMemTracker();
    EnsureOptionalFieldsAreAllocated(eeClass, pamTracker, pAllocator->GetLowFrequencyHeap());
    eeClass->SetEightByteClassification(helper->eightByteCount, helper->eightByteClassifications, helper->eightByteSizes);
}

#endif // UNIX_AMD64_ABI

//---------------------------------------------------------------------------------------
//
// make sure that no object fields are overlapped incorrectly and define the
// GC pointer series for the class. We are assuming that this class will always be laid out within
// its enclosing class by the compiler in such a way that offset 0 will be the correct alignment
// for object ref fields so we don't need to try to align it
//
VOID
MethodTableBuilder::HandleExplicitLayout(
    MethodTable ** pByValueClassCache)
{
    STANDARD_VM_CONTRACT;

    // Instance slice size is the total size of an instance, and is calculated as
    // the field whose offset and size add to the greatest number.
    UINT instanceSliceSize = 0;

    UINT i;
    for (i = 0; i < bmtMetaData->cFields; i++)
    {
        FieldDesc *pFD = bmtMFDescs->ppFieldDescList[i];
        if (pFD == NULL || pFD->IsStatic())
        {
            continue;
        }

        UINT fieldExtent = 0;
        if (!ClrSafeInt<UINT>::addition(pFD->GetOffset_NoLogging(), GetFieldSize(pFD), fieldExtent))
        {
            BuildMethodTableThrowException(COR_E_OVERFLOW);
        }

        if (fieldExtent > instanceSliceSize)
        {
            instanceSliceSize = fieldExtent;
        }
    }

    CQuickBytes qb;
    PREFIX_ASSUME(sizeof(bmtFieldLayoutTag) == 1);
    bmtFieldLayoutTag *pFieldLayout = (bmtFieldLayoutTag*)qb.AllocThrows(instanceSliceSize * sizeof(bmtFieldLayoutTag));
    for (i=0; i < instanceSliceSize; i++)
    {
        pFieldLayout[i] = empty;
    }

    // Go through each field and look for invalid layout.
    // (note that we are more permissive than what Ecma allows. We only disallow the minimum set necessary to
    // close security holes.)
    //
    // This is what we implement:
    //
    // 1. Verify that every OREF or BYREF is on a valid alignment.
    // 2. Verify that OREFs only overlap with other OREFs.
    // 3. Verify that BYREFs only overlap with other BYREFs.
    // 4. If an OREF does overlap with another OREF, the class is marked unverifiable.
    // 5. If a BYREF does overlap with another BYREF, the class is marked unverifiable.
    // 6. If an overlap of any kind occurs, the class will be marked NotTightlyPacked (affects ValueType.Equals()).
    //
    bmtFieldLayoutTag emptyObject[TARGET_POINTER_SIZE];
    bmtFieldLayoutTag isObject[TARGET_POINTER_SIZE];
    bmtFieldLayoutTag isByRef[TARGET_POINTER_SIZE];
    for (i = 0; i < TARGET_POINTER_SIZE; i++)
    {
        emptyObject[i] = empty;
        isObject[i]    = oref;
        isByRef[i]     = byref;
    }

    ExplicitClassTrust explicitClassTrust;

    UINT valueClassCacheIndex = ((UINT)(-1));
    UINT badOffset = 0;
    FieldDesc * pFD = NULL;
    for (i = 0; i < bmtMetaData->cFields; i++)
    {
        // Note about this loop body:
        //
        // This loop is coded to make it as hard as possible to allow a field to be trusted when it shouldn't.
        //
        // Every path in this loop body must lead to an explicit decision as to whether the field nonoverlaps,
        // overlaps in a verifiable fashion, overlaps in a nonverifiable fashion or overlaps in a completely illegal fashion.
        //
        // It must call fieldTrust.SetTrust() with the appropriate result. If you don't call it, fieldTrust's destructor
        // will intentionally default to kNone and mark the entire class illegal.
        //
        // If your result is anything but kNone (class is illegal), you must also explicitly "continue" the loop.
        // There is a "break" at end of this loop body that will abort the loop if you don't do this. And
        // if you don't finish iterating through all the fields, this function will automatically mark the entire
        // class illegal. This rule is a vestige of an earlier version of this function.

        // This object's dtor will aggregate the trust decision for this field into the trust level for the class as a whole.
        ExplicitFieldTrustHolder fieldTrust(&explicitClassTrust);

        pFD = bmtMFDescs->ppFieldDescList[i];
        if (pFD == NULL || pFD->IsStatic())
        {
            fieldTrust.SetTrust(ExplicitFieldTrust::kNonOverLayed);
            continue;
        }

        // "i" indexes all fields, valueClassCacheIndex indexes non-static fields only. Don't get them confused!
        valueClassCacheIndex++;

        CorElementType type = pFD->GetFieldType();
        if (CorTypeInfo::IsObjRef(type) || CorTypeInfo::IsByRef(type))
        {
            // Check that the field is pointer aligned
            if ((pFD->GetOffset_NoLogging() & ((ULONG)TARGET_POINTER_SIZE - 1)) != 0)
            {
                badOffset = pFD->GetOffset_NoLogging();
                fieldTrust.SetTrust(ExplicitFieldTrust::kNone);

                // If we got here, OREF or BYREF field was not pointer aligned. THROW.
                break;
            }

            // Determine which tag type we are working with.
            bmtFieldLayoutTag tag;
            SIZE_T tagBlockSize;
            void* tagBlock;
            if (CorTypeInfo::IsObjRef(type))
            {
                tagBlockSize = sizeof(isObject);
                tagBlock = (void*)isObject;
                tag = oref;
            }
            else
            {
                _ASSERTE(CorTypeInfo::IsByRef(type));
                tagBlockSize = sizeof(isByRef);
                tagBlock = (void*)isByRef;
                tag = byref;
            }

            // Check if there is overlap with its own tag type
            if (memcmp((void *)&pFieldLayout[pFD->GetOffset_NoLogging()], tagBlock, tagBlockSize) == 0)
            {
                // If we got here, there is tag type overlap. We permit this but mark the class unverifiable.
                fieldTrust.SetTrust(ExplicitFieldTrust::kLegal);
                continue;
            }
            // check if typed layout is empty at this point
            if (memcmp((void *)&pFieldLayout[pFD->GetOffset_NoLogging()], (void *)emptyObject, sizeof(emptyObject)) == 0)
            {
                // If we got here, this tag type is overlapping no other fields (yet).
                // Record that these bytes now contain the current tag type.
                memset((void *)&pFieldLayout[pFD->GetOffset_NoLogging()], tag, tagBlockSize);
                fieldTrust.SetTrust(ExplicitFieldTrust::kNonOverLayed);
                continue;
            }

            // If we got here, the tag overlaps something else. THROW.
            badOffset = pFD->GetOffset_NoLogging();
            fieldTrust.SetTrust(ExplicitFieldTrust::kNone);
            break;
        }
        else
        {
            UINT fieldSize;
            if (!pFD->IsByValue())
            {
                fieldSize = GetFieldSize(pFD);
            }
            else
            {
                MethodTable *pByValueMT = pByValueClassCache[valueClassCacheIndex];
                if (pByValueMT->IsByRefLike() || pByValueMT->ContainsPointers())
                {
                    if ((pFD->GetOffset_NoLogging() & ((ULONG)TARGET_POINTER_SIZE - 1)) != 0)
                    {
                        // If we got here, then a ByRefLike valuetype or a valuetype containing an OREF was misaligned.
                        badOffset = pFD->GetOffset_NoLogging();
                        fieldTrust.SetTrust(ExplicitFieldTrust::kNone);
                        break;
                    }

                    ExplicitFieldTrust::TrustLevel trust = CheckValueClassLayout(pByValueMT, &pFieldLayout[pFD->GetOffset_NoLogging()]);
                    fieldTrust.SetTrust(trust);

                    if (trust != ExplicitFieldTrust::kNone)
                    {
                        continue;
                    }
                    else
                    {
                        // If we got here, then an OREF/BYREF inside the valuetype illegally overlapped a non-OREF field. THROW.
                        badOffset = pFD->GetOffset_NoLogging();
                        break;
                    }
                    break;
                }
                // no pointers so fall through to do standard checking
                fieldSize = pByValueMT->GetNumInstanceFieldBytes();
            }

            // If we got here, we are trying to place a non-OREF (or a valuetype composed of non-OREFs.)
            // Look for any orefs or byrefs under this field
            bmtFieldLayoutTag* loc = NULL;
            bmtFieldLayoutTag* currOffset = pFieldLayout + pFD->GetOffset_NoLogging();
            bmtFieldLayoutTag* endOffset = currOffset + fieldSize;
            for (; currOffset < endOffset; ++currOffset)
            {
                if (*currOffset == oref || *currOffset == byref)
                {
                    loc = currOffset;
                    break;
                }
            }

            if (loc == NULL)
            {
                // If we have a nonoref in the range then we are doing an overlay
                if(memchr((void*)&pFieldLayout[pFD->GetOffset_NoLogging()], nonoref, fieldSize))
                {
                    fieldTrust.SetTrust(ExplicitFieldTrust::kVerifiable);
                }
                else
                {
                    fieldTrust.SetTrust(ExplicitFieldTrust::kNonOverLayed);
                }
                memset((void*)&pFieldLayout[pFD->GetOffset_NoLogging()], nonoref, fieldSize);
                continue;
            }

            // If we got here, we tried to place a non-OREF (or a valuetype composed of non-OREFs)
            // on top of an OREF/BYREF. THROW.
            badOffset = (UINT)(loc - pFieldLayout);
            fieldTrust.SetTrust(ExplicitFieldTrust::kNone);
            break;
            // anything else is an error
        }

        // We have to comment out this assert because otherwise, the compiler refuses to build because the _ASSERT is unreachable
        // (Thanks for nothing, compiler, that's what the assert is trying to enforce!) But the intent of the assert is correct.
        //_ASSERTE(!"You aren't supposed to be here. Some path inside the loop body did not execute an explicit break or continue.");


        // If we got here, some code above failed to execute an explicit "break" or "continue." This is a bug! To be safe,
        // we will put a catchall "break" here which will cause the typeload to abort (albeit with a probably misleading
        // error message.)
        break;
    }

    // We only break out of the loop above if we detected an error.
    if (i < bmtMetaData->cFields || !explicitClassTrust.IsLegal())
    {
        ThrowFieldLayoutError(GetCl(),
                              GetModule(),
                              badOffset,
                              IDS_CLASSLOAD_EXPLICIT_LAYOUT);
    }

    if (!explicitClassTrust.IsNonOverLayed())
    {
        SetHasOverLayedFields();
    }

    FindPointerSeriesExplicit(instanceSliceSize, pFieldLayout);

    // Fixup the offset to include parent as current offsets are relative to instance slice
    // Could do this earlier, but it's just easier to assume instance relative for most
    // of the earlier calculations

    // Instance fields start right after the parent
    S_UINT32 dwInstanceSliceOffset = S_UINT32(HasParent() ? GetParentMethodTable()->GetNumInstanceFieldBytes() : 0);
    if (bmtGCSeries->numSeries != 0)
    {
        dwInstanceSliceOffset.AlignUp(TARGET_POINTER_SIZE);
    }
    if (dwInstanceSliceOffset.IsOverflow())
    {
        // addition overflow or cast truncation
        BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
    }

    S_UINT32 numInstanceFieldBytes = dwInstanceSliceOffset + S_UINT32(instanceSliceSize);

    if (IsValueClass())
    {
        ULONG clstotalsize;
        if (FAILED(GetMDImport()->GetClassTotalSize(GetCl(), &clstotalsize)))
        {
            clstotalsize = 0;
        }

        if (clstotalsize != 0)
        {
            // size must be large enough to accomodate layout. If not, we use the layout size instead.
            if (!numInstanceFieldBytes.IsOverflow() && clstotalsize >= numInstanceFieldBytes.Value())
            {
                numInstanceFieldBytes = S_UINT32(clstotalsize);
            }
        }
        else
        {
            // align up to the alignment requirements of the members of this value type.
            numInstanceFieldBytes.AlignUp(GetLayoutInfo()->m_ManagedLargestAlignmentRequirementOfAllMembers);
            if (numInstanceFieldBytes.IsOverflow())
            {
                // addition overflow or cast truncation
                BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
            }
        }

        if (!numInstanceFieldBytes.IsOverflow() && numInstanceFieldBytes.Value() == 0)
        {
            // If we calculate a 0-byte size here, we should have also calculated a 0-byte size
            // in the initial layout algorithm.
            _ASSERTE(GetLayoutInfo()->IsZeroSized());
            numInstanceFieldBytes = S_UINT32(1);
        }
    }

    // The GC requires that all valuetypes containing orefs be sized to a multiple of TARGET_POINTER_SIZE.
    if (bmtGCSeries->numSeries != 0)
    {
        numInstanceFieldBytes.AlignUp(TARGET_POINTER_SIZE);
    }
    if (numInstanceFieldBytes.IsOverflow())
    {
        // addition overflow or cast truncation
        BuildMethodTableThrowException(IDS_CLASSLOAD_GENERAL);
    }

    // Set the total size
    bmtFP->NumInstanceFieldBytes = numInstanceFieldBytes.Value();

    for (i = 0; i < bmtMetaData->cFields; i++)
    {
        FieldDesc * pTempFD = bmtMFDescs->ppFieldDescList[i];
        if ((pTempFD == NULL) || pTempFD->IsStatic())
        {
            continue;
        }
        HRESULT hr = pTempFD->SetOffset(pTempFD->GetOffset_NoLogging() + dwInstanceSliceOffset.Value());
        if (FAILED(hr))
        {
            BuildMethodTableThrowException(hr, *bmtError);
        }
    }
} // MethodTableBuilder::HandleExplicitLayout

//*******************************************************************************
// make sure that no object fields are overlapped incorrectly, returns the trust level
/*static*/ ExplicitFieldTrust::TrustLevel MethodTableBuilder::CheckValueClassLayout(MethodTable * pMT, bmtFieldLayoutTag *pFieldLayout)
{
    STANDARD_VM_CONTRACT;

    // ByRefLike types need to be checked for ByRef fields.
    if (pMT->IsByRefLike())
        return CheckByRefLikeValueClassLayout(pMT, pFieldLayout);

    // This method assumes there is a GC desc associated with the MethodTable.
    _ASSERTE(pMT->ContainsPointers());

    // Build a layout of the value class (vc). Don't know the sizes of all the fields easily, but
    // do know (a) vc is already consistent so don't need to check it's overlaps and
    // (b) size and location of all objectrefs. So build it by setting all non-oref
    // then fill in the orefs later
    UINT fieldSize = pMT->GetNumInstanceFieldBytes();

    CQuickBytes qb;
    bmtFieldLayoutTag *vcLayout = (bmtFieldLayoutTag*) qb.AllocThrows(fieldSize * sizeof(bmtFieldLayoutTag));
    memset((void*)vcLayout, nonoref, fieldSize);

    // use pointer series to locate the orefs
    CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
    CGCDescSeries *pSeries = map->GetLowestSeries();

    for (SIZE_T j = 0; j < map->GetNumSeries(); j++)
    {
        CONSISTENCY_CHECK(pSeries <= map->GetHighestSeries());

        memset((void*)&vcLayout[pSeries->GetSeriesOffset() - OBJECT_SIZE], oref, pSeries->GetSeriesSize() + pMT->GetBaseSize());
        pSeries++;
    }

    ExplicitClassTrust explicitClassTrust;
    for (UINT i=0; i < fieldSize; i++)
    {
        ExplicitFieldTrustHolder fieldTrust(&explicitClassTrust);

        if (vcLayout[i] == oref) {
            switch (pFieldLayout[i]) {
                // oref <--> empty
                case empty:
                    pFieldLayout[i] = oref;
                    fieldTrust.SetTrust(ExplicitFieldTrust::kNonOverLayed);
                    break;

                // oref <--> nonoref
                case nonoref:
                    fieldTrust.SetTrust(ExplicitFieldTrust::kNone);
                    break;

                // oref <--> oref
                case oref:
                    fieldTrust.SetTrust(ExplicitFieldTrust::kLegal);
                    break;

                default:
                    _ASSERTE(!"Can't get here.");
                }
        } else if (vcLayout[i] == nonoref) {
            switch (pFieldLayout[i]) {
                // nonoref <--> empty
                case empty:
                    pFieldLayout[i] = nonoref;
                    fieldTrust.SetTrust(ExplicitFieldTrust::kNonOverLayed);
                    break;

                // nonoref <--> nonoref
                case nonoref:
                    fieldTrust.SetTrust(ExplicitFieldTrust::kVerifiable);
                    break;

                // nonoref <--> oref
                case oref:
                    fieldTrust.SetTrust(ExplicitFieldTrust::kNone);
                    break;

                default:
                    _ASSERTE(!"Can't get here.");
            }
        } else {
            _ASSERTE(!"Can't get here.");
        }
    }

    return explicitClassTrust.GetTrustLevel();
}


//*******************************************************************************
// make sure that no byref/object fields are overlapped, returns the trust level
/*static*/ ExplicitFieldTrust::TrustLevel MethodTableBuilder::CheckByRefLikeValueClassLayout(MethodTable * pMT, bmtFieldLayoutTag *pFieldLayout)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pMT->IsByRefLike());

    // ByReference<T> is an indication for a byref field, treat it as such.
    if (pMT->HasSameTypeDefAs(g_pByReferenceClass))
         return MarkTagType(pFieldLayout, TARGET_POINTER_SIZE, byref);

    ExplicitClassTrust explicitClassTrust;

    ExplicitFieldTrust::TrustLevel trust;
    ApproxFieldDescIterator fieldIterator(pMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc *pFD = fieldIterator.Next(); pFD != NULL; pFD = fieldIterator.Next())
    {
        ExplicitFieldTrustHolder fieldTrust(&explicitClassTrust);
        int fieldStartIndex = pFD->GetOffset();

        if (pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
        {
            MethodTable *pFieldMT = pFD->GetApproxFieldTypeHandleThrowing().AsMethodTable();
            trust = CheckValueClassLayout(pFieldMT, &pFieldLayout[fieldStartIndex]);
        }
        else if (pFD->IsObjRef())
        {
            _ASSERTE(fieldStartIndex % TARGET_POINTER_SIZE == 0);
            trust = MarkTagType(&pFieldLayout[fieldStartIndex], TARGET_POINTER_SIZE, oref);
        }
        else if (pFD->IsByRef())
        {
            _ASSERTE(fieldStartIndex % TARGET_POINTER_SIZE == 0);
            trust = MarkTagType(&pFieldLayout[fieldStartIndex], TARGET_POINTER_SIZE, byref);
        }
        else
        {
            trust = MarkTagType(&pFieldLayout[fieldStartIndex], pFD->GetSize(), nonoref);
        }

        fieldTrust.SetTrust(trust);

        // Some invalid overlap was detected.
        if (trust == ExplicitFieldTrust::kNone)
            break;
    }

    return explicitClassTrust.GetTrustLevel();
}

//*******************************************************************************
// Set the field's tag type and/or detect invalid overlap
/*static*/ ExplicitFieldTrust::TrustLevel MethodTableBuilder::MarkTagType(bmtFieldLayoutTag* field, SIZE_T fieldSize, bmtFieldLayoutTag tagType)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(field != NULL);
    _ASSERTE(fieldSize != 0);
    _ASSERTE(tagType != empty);

    ExplicitFieldTrust::TrustLevel trust = ExplicitFieldTrust::kMaxTrust;
    for (SIZE_T i = 0; i < fieldSize; ++i)
    {
        if (field[i] == empty)
        {
            // Nothing set for overlap, mark as requested.
            field[i] = tagType;
        }
        else if (field[i] == tagType)
        {
            // Only a overlapped nonoref tag is verifiable, all others are simply legal.
            ExplicitFieldTrust::TrustLevel overlapTrust = tagType == nonoref ? ExplicitFieldTrust::kVerifiable : ExplicitFieldTrust::kLegal;

            // The ExplicitFieldTrust enum is ranked in descending order of trust.
            // We always take the computed minimum trust level.
            trust = min(trust, overlapTrust);
        }
        else
        {
            // A non-equal overlap was detected. There is no trust for the type.
            trust = ExplicitFieldTrust::kNone;
            break;
        }
    }

    return trust;
}

//*******************************************************************************
void MethodTableBuilder::FindPointerSeriesExplicit(UINT instanceSliceSize,
                                                   bmtFieldLayoutTag *pFieldLayout)
{
    STANDARD_VM_CONTRACT;


    // Allocate a structure to track the series. We know that the worst case is a
    // ref-non-ref-non, and since only ref series are recorded and non-ref series
    // are skipped, the max number of series is total instance size / 2 / sizeof(ref).
    // But watch out for the case where we have e.g. an instanceSlizeSize of 4.
    DWORD sz = (instanceSliceSize + (2 * TARGET_POINTER_SIZE) - 1);
    bmtGCSeries->pSeries = new bmtGCSeriesInfo::Series[sz/2/ TARGET_POINTER_SIZE];

    bmtFieldLayoutTag *loc = pFieldLayout;
    bmtFieldLayoutTag *layoutEnd = pFieldLayout + instanceSliceSize;
    while (loc < layoutEnd)
    {
        // Find the next OREF entry.
        loc = (bmtFieldLayoutTag*)memchr((void*)loc, oref, layoutEnd-loc);
        if (loc == NULL)
        {
            break;
        }

        // Find the next non-OREF entry
        bmtFieldLayoutTag *cur = loc;
        while(cur < layoutEnd && *cur == oref)
        {
            cur++;
        }

        // so we have a GC series at loc for cur-loc bytes
        bmtGCSeries->pSeries[bmtGCSeries->numSeries].offset = (DWORD)(loc - pFieldLayout);
        bmtGCSeries->pSeries[bmtGCSeries->numSeries].len = (DWORD)(cur - loc);

        CONSISTENCY_CHECK(IS_ALIGNED(cur - loc, TARGET_POINTER_SIZE));

        bmtGCSeries->numSeries++;
        loc = cur;
    }

    // Calculate the total series count including the parent, if a parent exists.

    bmtFP->NumGCPointerSeries = bmtParent->NumParentPointerSeries + bmtGCSeries->numSeries;

}

//*******************************************************************************
VOID
MethodTableBuilder::HandleGCForExplicitLayout()
{
    STANDARD_VM_CONTRACT;

    MethodTable *pMT = GetHalfBakedMethodTable();

    if (bmtFP->NumGCPointerSeries != 0)
    {
        pMT->SetContainsPointers();

        // Copy the pointer series map from the parent
        CGCDesc::Init( (PVOID) pMT, bmtFP->NumGCPointerSeries );
        if (bmtParent->NumParentPointerSeries != 0)
        {
            size_t ParentGCSize = CGCDesc::ComputeSize(bmtParent->NumParentPointerSeries);
            memcpy( (PVOID) (((BYTE*) pMT) - ParentGCSize),
                    (PVOID) (((BYTE*) GetParentMethodTable()) - ParentGCSize),
                    ParentGCSize - sizeof(size_t)   // sizeof(size_t) is the NumSeries count
                  );
        }

        UINT32 dwInstanceSliceOffset = AlignUp(HasParent() ? GetParentMethodTable()->GetNumInstanceFieldBytes() : 0, TARGET_POINTER_SIZE);

        // Build the pointer series map for this pointers in this instance
        CGCDescSeries *pSeries = ((CGCDesc*)pMT)->GetLowestSeries();
        for (UINT i=0; i < bmtGCSeries->numSeries; i++) {
            // See gcdesc.h for an explanation of why we adjust by subtracting BaseSize
            BAD_FORMAT_NOTHROW_ASSERT(pSeries <= CGCDesc::GetCGCDescFromMT(pMT)->GetHighestSeries());

            pSeries->SetSeriesSize( (size_t) bmtGCSeries->pSeries[i].len - (size_t) pMT->GetBaseSize() );
            pSeries->SetSeriesOffset(bmtGCSeries->pSeries[i].offset + OBJECT_SIZE + dwInstanceSliceOffset);
            pSeries++;
        }

        // Adjust the inherited series - since the base size has increased by "# new field instance bytes", we need to
        // subtract that from all the series (since the series always has BaseSize subtracted for it - see gcdesc.h)
        CGCDescSeries *pHighest = CGCDesc::GetCGCDescFromMT(pMT)->GetHighestSeries();
        while (pSeries <= pHighest)
        {
            CONSISTENCY_CHECK(CheckPointer(GetParentMethodTable()));
            pSeries->SetSeriesSize( pSeries->GetSeriesSize() - ((size_t) pMT->GetBaseSize() - (size_t) GetParentMethodTable()->GetBaseSize()) );
            pSeries++;
        }
    }

    delete [] bmtGCSeries->pSeries;
    bmtGCSeries->pSeries = NULL;
} // MethodTableBuilder::HandleGCForExplicitLayout

static
BOOL
InsertMethodTable(
    MethodTable  *pNew,
    MethodTable **pArray,
    DWORD         nArraySizeMax,
    DWORD        *pNumAssigned)
{
    LIMITED_METHOD_CONTRACT;

    for (DWORD j = 0; j < (*pNumAssigned); j++)
    {
        if (pNew == pArray[j])
        {
#ifdef _DEBUG
            LOG((LF_CLASSLOADER, LL_INFO1000, "GENERICS: Found duplicate interface %s (%p) at position %d out of %d\n", pNew->GetDebugClassName(), pNew, j, *pNumAssigned));
#endif
            return pNew->HasInstantiation(); // bail out - we found a duplicate instantiated interface
        }
        else
        {
#ifdef _DEBUG
            LOG((LF_CLASSLOADER, LL_INFO1000, "  GENERICS: InsertMethodTable ignored interface %s (%p) at position %d out of %d\n", pArray[j]->GetDebugClassName(), pArray[j], j, *pNumAssigned));
#endif
        }
    }
    if (*pNumAssigned >= nArraySizeMax)
    {
        LOG((LF_CLASSLOADER, LL_INFO1000, "GENERICS: Found interface %s (%p) exceeding size %d of interface array\n", pNew->GetDebugClassName(), pNew, nArraySizeMax));
        return TRUE;
    }
    LOG((LF_CLASSLOADER, LL_INFO1000, "GENERICS: Inserting interface %s (%p) at position %d\n", pNew->GetDebugClassName(), pNew, *pNumAssigned));
    pArray[(*pNumAssigned)++] = pNew;
    return FALSE;
} // InsertMethodTable


//*******************************************************************************
// --------------------------------------------------------------------------------------------
// Copy virtual slots inherited from parent:
//
// In types created at runtime, inherited virtual slots are initialized using approximate parent
// during method table building. This method will update them based on the exact parent.
// In types loaded from NGen image, inherited virtual slots from cross-module parents are not
// initialized. This method will initialize them based on the actually loaded exact parent
// if necessary.
/* static */
void MethodTableBuilder::CopyExactParentSlots(MethodTable *pMT, MethodTable *pApproxParentMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    DWORD nParentVirtuals = pMT->GetNumParentVirtuals();
    if (nParentVirtuals == 0)
        return;

    _ASSERTE(nParentVirtuals == pApproxParentMT->GetNumVirtuals());

    //
    // Update all inherited virtual slots to match exact parent
    //

    if (!pMT->IsCanonicalMethodTable())
    {
        //
        // Copy all slots for non-canonical methodtables to avoid touching methoddescs.
        //
        MethodTable * pCanonMT = pMT->GetCanonicalMethodTable();

        // Do not write into vtable chunks shared with parent. It would introduce race
        // with code:MethodDesc::SetStableEntryPointInterlocked.
        //
        // Non-canonical method tables either share everything or nothing so it is sufficient to check
        // just the first indirection to detect sharing.
        if (pMT->GetVtableIndirections()[0] != pCanonMT->GetVtableIndirections()[0])
        {
            MethodTable::MethodDataWrapper hCanonMTData(MethodTable::GetMethodData(pCanonMT, FALSE));
            for (DWORD i = 0; i < nParentVirtuals; i++)
            {
                pMT->CopySlotFrom(i, hCanonMTData, pCanonMT);
            }
        }
    }
    else
    {
        MethodTable::MethodDataWrapper hMTData(MethodTable::GetMethodData(pMT, FALSE));

        MethodTable * pParentMT = pMT->GetParentMethodTable();
        MethodTable::MethodDataWrapper hParentMTData(MethodTable::GetMethodData(pParentMT, FALSE));

        for (DWORD i = 0; i < nParentVirtuals; i++)
        {
            // fix up wrongly-inherited method descriptors
            MethodDesc* pMD = hMTData->GetImplMethodDesc(i);
            CONSISTENCY_CHECK(CheckPointer(pMD));
            CONSISTENCY_CHECK(pMD == pMT->GetMethodDescForSlot(i));

            if (pMD->GetMethodTable() == pMT)
                continue;

            // We need to re-inherit this slot from the exact parent.

            DWORD indirectionIndex = MethodTable::GetIndexOfVtableIndirection(i);
            if (pMT->GetVtableIndirections()[indirectionIndex] == pApproxParentMT->GetVtableIndirections()[indirectionIndex])
            {
                // The slot lives in a chunk shared from the approximate parent MT
                // If so, we need to change to share the chunk from the exact parent MT

                _ASSERTE(MethodTable::CanShareVtableChunksFrom(pParentMT, pMT->GetLoaderModule()));

                pMT->GetVtableIndirections()[indirectionIndex] = pParentMT->GetVtableIndirections()[indirectionIndex];

                i = MethodTable::GetEndSlotForVtableIndirection(indirectionIndex, nParentVirtuals) - 1;
                continue;
            }

            // The slot lives in an unshared chunk. We need to update the slot contents
            pMT->CopySlotFrom(i, hParentMTData, pParentMT);
        }
    }
} // MethodTableBuilder::CopyExactParentSlots

bool InstantiationIsAllTypeVariables(const Instantiation &inst)
{
    for (auto i = inst.GetNumArgs(); i > 0;)
    {
        TypeHandle th = inst[--i];
        if (!th.IsGenericVariable())
            return false;
    }

    return true;
}

//*******************************************************************************
/* static */
void
MethodTableBuilder::LoadExactInterfaceMap(MethodTable *pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    BOOL hasInstantiatedInterfaces = FALSE;
    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
    while (it.Next())
    {
        if (it.GetInterfaceApprox()->HasInstantiation())
        {
            hasInstantiatedInterfaces = TRUE;
            break;
        }
    }

    // If we have some instantiated interfaces, then we have lots more work to do...

    // In the worst case we have to use the metadata to
    //  (a) load the exact interfaces and determine the order in which they
    //      go.  We do those by re-running the interface layout algorithm
    //      and using metadata-comparisons to place interfaces in the list.
    //  (b) do a check to see if any ambiguity in the interface dispatch map is introduced
    //      by the instantiation
    // See code:#LoadExactInterfaceMap_Algorithm2
    //
    // However, we can do something simpler: we just use
    // the loaded interface method tables to determine ordering.  This can be done
    // if there are no duplicate instantiated interfaces in the interface
    // set.
    // See code:#LoadExactInterfaceMap_Algorithm1.

    if (!hasInstantiatedInterfaces)
    {
        return;
    }

    HRESULT hr;
    TypeHandle thisTH(pMT);
    SigTypeContext typeContext(thisTH);
    MethodTable *pParentMT = pMT->GetParentMethodTable();

    //#LoadExactInterfaceMap_Algorithm1
    // Exact interface instantiation loading TECHNIQUE 1.
    // (a) For interfaces inherited from an instantiated parent class, just copy down from exact parent
    // (b) Grab newly declared interfaces by loading and then copying down all their inherited parents
    // (c) But check for any exact duplicates along the way
    // (d) If no duplicates then we can use the computed interface map we've created
    // (e) If duplicates found then use the slow metadata-based technique code:#LoadExactInterfaceMap_Algorithm2
    DWORD nInterfacesCount = pMT->GetNumInterfaces();
    MethodTable **pExactMTs = (MethodTable**) _alloca(sizeof(MethodTable *) * nInterfacesCount);
    BOOL duplicates;
    bool retry = false;

    // Always use exact loading behavior with classes or shared generics, as they have to deal with inheritance, and the
    // inexact matching logic for classes would be more complex to write.
    // Also always use the exact loading behavior with any generic that contains generic variables, as the open type is used
    // to represent a type instantiated over its own generic variables, and the special marker type is currently the open type
    // and we make this case distinguishable by simply disallowing the optimization in those cases.
    bool retryWithExactInterfaces = !pMT->IsValueType() || pMT->IsSharedByGenericInstantiations() || pMT->ContainsGenericVariables();

    DWORD nAssigned = 0;
    do
    {
        nAssigned = 0;
        retry = false;
        duplicates = false;
        if (pParentMT != NULL)
        {
            MethodTable::InterfaceMapIterator parentIt = pParentMT->IterateInterfaceMap();
            while (parentIt.Next())
            {
                duplicates |= InsertMethodTable(parentIt.GetInterface(pParentMT, CLASS_LOAD_EXACTPARENTS), pExactMTs, nInterfacesCount, &nAssigned);
            }
        }
        InterfaceImplEnum ie(pMT->GetModule(), pMT->GetCl(), NULL);
        while ((hr = ie.Next()) == S_OK)
        {
            MethodTable *pNewIntfMT = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pMT->GetModule(),
                                                                                ie.CurrentToken(),
                                                                                &typeContext,
                                                                                ClassLoader::ThrowIfNotFound,
                                                                                ClassLoader::FailIfUninstDefOrRef,
                                                                                ClassLoader::LoadTypes,
                                                                                CLASS_LOAD_EXACTPARENTS,
                                                                                TRUE,
                                                                                (const Substitution*)0,
                                                                                retryWithExactInterfaces ? NULL : pMT).GetMethodTable();

            bool uninstGenericCase = !retryWithExactInterfaces && pNewIntfMT->IsSpecialMarkerTypeForGenericCasting();

            duplicates |= InsertMethodTable(pNewIntfMT, pExactMTs, nInterfacesCount, &nAssigned);

            // We have a special algorithm for interface maps in CoreLib, which doesn't expand interfaces, and assumes no ambiguous
            // duplicates. Code related to this is marked with #SpecialCorelibInterfaceExpansionAlgorithm
            if (!(pMT->GetModule()->IsSystem() && pMT->IsValueType()))
            {
                MethodTable::InterfaceMapIterator intIt = pNewIntfMT->IterateInterfaceMap();
                while (intIt.Next())
                {
                    MethodTable *pItfPossiblyApprox = intIt.GetInterfaceApprox();
                    if (uninstGenericCase && pItfPossiblyApprox->HasInstantiation() && pItfPossiblyApprox->ContainsGenericVariables())
                    {
                        // We allow a limited set of interface generic shapes with type variables. In particular, we require the
                        // instantiations to be exactly simple type variables, and to have a relatively small number of generic arguments
                        // so that the fallback instantiating logic works efficiently
                        if (InstantiationIsAllTypeVariables(pItfPossiblyApprox->GetInstantiation()) && pItfPossiblyApprox->GetInstantiation().GetNumArgs() <= MethodTable::MaxGenericParametersForSpecialMarkerType)
                        {
                            pItfPossiblyApprox = ClassLoader::LoadTypeDefThrowing(pItfPossiblyApprox->GetModule(), pItfPossiblyApprox->GetCl(), ClassLoader::ThrowIfNotFound, ClassLoader::PermitUninstDefOrRef, 0, CLASS_LOAD_EXACTPARENTS).AsMethodTable();
                        }
                        else
                        {
                            retry = true;
                            break;
                        }
                    }
                    duplicates |= InsertMethodTable(intIt.GetInterface(pNewIntfMT, CLASS_LOAD_EXACTPARENTS), pExactMTs, nInterfacesCount, &nAssigned);
                }
            }

            if (retry)
                break;
        }

        if (retry)
        {
            retryWithExactInterfaces = true;
        }
    } while (retry);

    if (FAILED(hr))
    {
        pMT->GetAssembly()->ThrowTypeLoadException(pMT->GetMDImport(), pMT->GetCl(), IDS_CLASSLOAD_BADFORMAT);
    }
#ifdef _DEBUG
    if (!pMT->GetModule()->IsSystem())
    {

        duplicates |= CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AlwaysUseMetadataInterfaceMapLayout);
    }

    //#InjectInterfaceDuplicates_LoadExactInterfaceMap
    // If we are injecting duplicates also for non-generic interfaces in check builds, we have to use
    // algorithm code:#LoadExactInterfaceMap_Algorithm2.
    // Has to be in sync with code:#InjectInterfaceDuplicates_Main.
    duplicates |= pMT->Debug_HasInjectedInterfaceDuplicates();
#endif
    // We have a special algorithm for interface maps in CoreLib, which doesn't expand interfaces, and assumes no ambiguous
    // duplicates. Code related to this is marked with #SpecialCorelibInterfaceExpansionAlgorithm
    _ASSERTE(!duplicates || !(pMT->GetModule()->IsSystem() && pMT->IsValueType()));

    CONSISTENCY_CHECK(duplicates || (nAssigned == pMT->GetNumInterfaces()));
    if (duplicates)
    {
        //#LoadExactInterfaceMap_Algorithm2
        // Exact interface instantiation loading TECHNIQUE 2 - The exact instantiation has caused some duplicates to
        // appear in the interface map!  This may not be an error: if the duplicates were ones that arose because of
        // inheritance from a parent type then we accept that.  For example
        //     class C<T> : I<T>
        //     class D<T> : C<T>, I<string>
        // is acceptable even when loading D<string>.  Note that in such a case
        // there will be two entries for I<string> in the final interface map for D<string>.
        // For dispatch the mappings in D take precedence.
        //
        // However we consider it an error if there is real ambiguity within
        // the interface definitions within the one class, e.g.
        //     class E<T> : I<T>, I<string>
        // In this situation it is not defined how to dispatch calls to I<string>: would
        // we use the bindings for I<T> or I<string>?
        //
        // Because we may had duplicates the interface map we created above may not
        // be the correct one: for example for D<string> above we would have computed
        // a map with only one entry.  This is incorrect: an exact instantiation's interface
        // map must have entries that match the ordering of the interface map in the generic case
        // (this is because code:#InterfaceMap_SupersetOfParent).
        //
        // So, in order to determine how to place the interfaces we need go back to
        // the metadata. We also do this to check if the presence of duplicates
        // has caused any potential ambiguity, i.e. the E<string> case above.

        // First we do a GetCheckpoint for the thread-based allocator.  ExpandExactInheritedInterfaces allocates substitution chains
        // on the thread allocator rather than on the stack.
        ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

        // ***********************************************************
        // ****** This must be consistent with code:ExpandApproxInterface etc. *******
        //
        // The correlation to ExpandApproxInterfaces etc. simply drops out by how we
        // traverse interfaces.
        // ***********************************************************

        bmtExactInterfaceInfo bmtExactInterface;
        bmtExactInterface.pInterfaceSubstitution = new (pStackingAllocator) Substitution[pMT->GetNumInterfaces()];
        bmtExactInterface.pExactMTs = pExactMTs;
        bmtExactInterface.nAssigned = 0;
        bmtExactInterface.typeContext = typeContext;

        // Do the interfaces inherited from a parent class
        if ((pParentMT != NULL) && (pParentMT->GetNumInterfaces() > 0))
        {
            Substitution * pParentSubstForTypeLoad = new (pStackingAllocator) Substitution(
                pMT->GetSubstitutionForParent(NULL));
            Substitution * pParentSubstForComparing = new (pStackingAllocator) Substitution(
                pMT->GetSubstitutionForParent(NULL));
            ExpandExactInheritedInterfaces(
                &bmtExactInterface,
                pParentMT,
                pParentSubstForTypeLoad,
                pParentSubstForComparing,
                pStackingAllocator,
                retryWithExactInterfaces ? NULL : pMT);
        }
#ifdef _DEBUG
        //#ExactInterfaceMap_SupersetOfParent
        // Check that parent's interface map is subset of this interface map
        // See code:#InterfaceMap_SupersetOfParent
        {
            _ASSERTE(pParentMT->GetNumInterfaces() == bmtExactInterface.nAssigned);

            MethodTable::InterfaceMapIterator parentInterfacesIterator = pParentMT->IterateInterfaceMap();
            UINT32 nInterfaceIndex = 0;
            while (parentInterfacesIterator.Next())
            {
                if (pMT->IsSharedByGenericInstantiations())
                {   // The type is a canonical instantiation (contains _Canon)
                    // The interface instantiations of parent can be different (see
                    // code:#InterfaceMap_CanonicalSupersetOfParent), therefore we cannot compare
                    // MethodTables
                    _ASSERTE(parentInterfacesIterator.GetInterfaceInfo()->GetApproxMethodTable(pParentMT->GetLoaderModule())->HasSameTypeDefAs(
                        bmtExactInterface.pExactMTs[nInterfaceIndex]));
                }
                else
                {   // It is not canonical instantiation, we can compare MethodTables
                    _ASSERTE(parentInterfacesIterator.GetInterfaceApprox() == bmtExactInterface.pExactMTs[nInterfaceIndex]);
                }
                nInterfaceIndex++;
            }
            _ASSERTE(nInterfaceIndex == bmtExactInterface.nAssigned);
        }
#endif //_DEBUG

        // If there are any __Canon instances in the type argument list, then we defer the
        // ambiguity checking until an exact instantiation.
        // As the C# compiler won't allow an ambiguous generic interface to be generated, we don't
        // need this logic for CoreLib. We can't use the sanity checks flag here, as these ambiguities
        // are specified to the exact instantiation in use, not just whether or not normal the type is
        // well formed in metadata.
        if (!pMT->IsSharedByGenericInstantiations() && !pMT->GetModule()->IsSystem())
        {
            // There are no __Canon types in the instantiation, so do ambiguity check.
            bmtInterfaceAmbiguityCheckInfo bmtCheckInfo;
            bmtCheckInfo.pMT = pMT;
            bmtCheckInfo.ppInterfaceSubstitutionChains = new (pStackingAllocator) Substitution *[pMT->GetNumInterfaces()];
            bmtCheckInfo.ppExactDeclaredInterfaces = new (pStackingAllocator) MethodTable *[pMT->GetNumInterfaces()];
            bmtCheckInfo.nAssigned = 0;
            bmtCheckInfo.typeContext = typeContext;
            MethodTableBuilder::InterfacesAmbiguityCheck(&bmtCheckInfo, pMT->GetModule(), pMT->GetCl(), NULL, pStackingAllocator);
        }

        // OK, there is no ambiguity amongst the instantiated interfaces declared on this class.
        MethodTableBuilder::ExpandExactDeclaredInterfaces(
            &bmtExactInterface,
            pMT->GetModule(),
            pMT->GetCl(),
            NULL,
            NULL,
            pStackingAllocator,
            retryWithExactInterfaces ? NULL : pMT
            COMMA_INDEBUG(pMT));
        CONSISTENCY_CHECK(bmtExactInterface.nAssigned == pMT->GetNumInterfaces());

        // We cannot process interface duplicates on types with __Canon. The duplicates are processed on
        // exact types only
        if (!pMT->IsSharedByGenericInstantiations())
        {
            // Process all pairs of duplicates in the interface map:
            //     i.e. If there are 3 duplicates of the same interface at indexes: i1, i2 and i3, then
            //     process pairs of indexes [i1,i2], [i1,i3] and [i2,i3].
            //  - Update 'declared on type' flag for those interfaces which duplicate is 'declared on type'
            //  - Check interface method implementation ambiguity code:#DuplicateInterface_MethodAmbiguity
            for (DWORD nOriginalIndex = 0; nOriginalIndex < nInterfacesCount; nOriginalIndex++)
            {
                // Search for duplicates further in the interface map
                for (DWORD nDuplicateIndex = nOriginalIndex + 1; nDuplicateIndex < nInterfacesCount; nDuplicateIndex++)
                {
                    if (pExactMTs[nOriginalIndex] != pExactMTs[nDuplicateIndex])
                    {   // It's not a duplicate of original interface, skip it
                        continue;
                    }
                    // We found a duplicate

                    // Set 'declared on type' flag if either original or duplicate interface is
                    // 'declared on type'
                    if (pMT->IsInterfaceDeclaredOnClass(nOriginalIndex) ||
                        pMT->IsInterfaceDeclaredOnClass(nDuplicateIndex))
                    {
                        //
                        // Note that both checks are needed:
                        //     A<T> : I<T>
                        //     B<T,U> : A<T>, I<U>
                        //     C<T,U> : B<T,U>, I<T>   // Reimplements interface from A<T>
                        // After code:BuildMethodTableThrowing algorithm, this will happen:
                        // B<int,int> will have interface map similar to B<T,U>:
                        //     I<int> ... not 'declared on type'
                        //     I<int> ... 'declared on type'
                        // C<int,int> will have interface map similar to C<T,U>:
                        //     I<int> ... 'declared on type'
                        //     I<int> ... not 'declared on type'
                        //

                        pMT->SetInterfaceDeclaredOnClass(nOriginalIndex);
                        pMT->SetInterfaceDeclaredOnClass(nDuplicateIndex);
                    }

                    //#DuplicateInterface_MethodAmbiguity
                    //
                    // In the ideal world we would now check for interface method implementation
                    // ambiguity in the instantiation, but that would be a technical breaking change
                    // (against 2.0 RTM/SP1).
                    // Therefore we ALLOW when interface method is implemented twice through this
                    // original and duplicate interface.
                    //
                    // This ambiguity pattern is therefore ALLOWED (can be expressed only in IL, not in C#):
                    //     I<T>
                    //         void Print(T t);
                    //     A<T> : I<T>    // abstract class
                    //     B<T,U> : A<T>, I<U>
                    //         void Print(T t) { ... }
                    //         void Print(U u) { ... }
                    //     Now B<int,int> has 2 implementations of I<int>.Print(int), while B<int,char> is
                    //     fine. Therefore an instantiation can introduce ambiguity.

#if 0 // Removing this code for now as it is a technical breaking change (against CLR 2.0 RTM/SP1).
      // We might decide later that we want to take this breaking change.
                    //
                    // Note that dispatch map entries are sorted by interface index and then interface
                    // method slot index.
                    //
                    DispatchMapTypeID originalTypeID = DispatchMapTypeID::InterfaceClassID(nOriginalIndex);
                    DispatchMap::EncodedMapIterator originalIt(pMT);
                    // Find first entry for original interface
                    while (originalIt.IsValid())
                    {
                        DispatchMapEntry *pEntry = originalIt.Entry();
                        if (pEntry->GetTypeID().ToUINT32() >= originalTypeID.ToUINT32())
                        {   // Found the place where original interface entries should be (dispatch map is
                            // sorted)
                            break;
                        }
                        originalIt.Next();
                    }

                    DispatchMapTypeID duplicateTypeID = DispatchMapTypeID::InterfaceClassID(nDuplicateIndex);
                    DispatchMap::EncodedMapIterator duplicateIt(pMT);
                    // Find first entry for duplicate interface
                    while (duplicateIt.IsValid())
                    {
                        DispatchMapEntry *pEntry = duplicateIt.Entry();
                        if (pEntry->GetTypeID().ToUINT32() >= duplicateTypeID.ToUINT32())
                        {   // Found the place where original interface entries should be (dispatch map is
                            // sorted)
                            break;
                        }
                        duplicateIt.Next();
                    }

                    // Compare original and duplicate interface entries in the dispatch map if they contain
                    // different implementation for the same interface method
                    while (true)
                    {
                        if (!originalIt.IsValid() || !duplicateIt.IsValid())
                        {   // We reached end of one dispatch map iterator
                            break;
                        }
                        DispatchMapEntry *pOriginalEntry = originalIt.Entry();
                        if (pOriginalEntry->GetTypeID().ToUINT32() != originalTypeID.ToUINT32())
                        {   // We reached behind original interface entries
                            break;
                        }
                        DispatchMapEntry *pDuplicateEntry = duplicateIt.Entry();
                        if (pDuplicateEntry->GetTypeID().ToUINT32() != duplicateTypeID.ToUINT32())
                        {   // We reached behind duplicate interface entries
                            break;
                        }

                        if (pOriginalEntry->GetSlotNumber() == pDuplicateEntry->GetSlotNumber())
                        {   // Found duplicate implementation of interface method
                            if (pOriginalEntry->GetTargetSlotNumber() != pDuplicateEntry->GetTargetSlotNumber())
                            {   // Implementation of the slots is different
                                bmtErrorInfo bmtError;

                                bmtError.pModule = pMT->GetModule();
                                bmtError.cl = pMT->GetCl();
                                bmtError.resIDWhy = IDS_CLASSLOAD_MI_MULTIPLEOVERRIDES;
                                bmtError.szMethodNameForError = NULL;
                                bmtError.pThrowable = NULL;

                                MethodDesc *pMD = pMT->GetMethodDescForSlot(pDuplicateEntry->GetTargetSlotNumber());
                                bmtError.dMethodDefInError = pMD->GetMemberDef();

                                BuildMethodTableThrowException(COR_E_TYPELOAD, bmtError);
                            }
                            // The method is implemented by the same slot on both interfaces (original and
                            // duplicate)

                            // Process next dispatch map entry
                            originalIt.Next();
                            duplicateIt.Next();
                            continue;
                        }
                        // Move iterator representing smaller interface method slot index (the dispatch map
                        // is sorted by slot indexes)
                        if (pOriginalEntry->GetSlotNumber() < pDuplicateEntry->GetSlotNumber())
                        {
                            originalIt.Next();
                            continue;
                        }
                        _ASSERTE(pOriginalEntry->GetSlotNumber() > pDuplicateEntry->GetSlotNumber());
                        duplicateIt.Next();
                    }
#endif //0
                }
                // All duplicates of this original interface were processed
            }
            // All pairs of duplicates in the interface map are processed
        }
    }
    // Duplicates in the interface map are resolved

    // OK, if we've got this far then pExactMTs should now hold the array of exact instantiated interfaces.
    MethodTable::InterfaceMapIterator thisIt = pMT->IterateInterfaceMap();
    DWORD i = 0;
    while (thisIt.Next())
    {
#ifdef _DEBUG
        MethodTable *pNewMT = pExactMTs[i];
        CONSISTENCY_CHECK(thisIt.HasSameTypeDefAs(pNewMT));
#endif // _DEBUG
        thisIt.SetInterface(pExactMTs[i]);
        i++;
    }

} // MethodTableBuilder::LoadExactInterfaceMap

//*******************************************************************************
void
MethodTableBuilder::ExpandExactInheritedInterfaces(
    bmtExactInterfaceInfo * bmtInfo,
    MethodTable *           pMT,
    const Substitution *    pSubstForTypeLoad,
    Substitution *          pSubstForComparing,
    StackingAllocator *     pStackingAllocator,
    MethodTable *           pMTInterfaceMapOwner)
{
    STANDARD_VM_CONTRACT;

    MethodTable *pParentMT = pMT->GetParentMethodTable();

    // Backup type's substitution chain for comparing interfaces
    Substitution substForComparingBackup = *pSubstForComparing;
    // Make type an open type for comparing interfaces
    *pSubstForComparing = Substitution();

    if (pParentMT)
    {
        // Chain parent's substitution for exact type load
        Substitution * pParentSubstForTypeLoad = new (pStackingAllocator) Substitution(
            pMT->GetSubstitutionForParent(pSubstForTypeLoad));

        // Chain parent's substitution for comparing interfaces (note that this type is temporarily
        // considered as open type)
        Substitution * pParentSubstForComparing = new (pStackingAllocator) Substitution(
            pMT->GetSubstitutionForParent(pSubstForComparing));

        ExpandExactInheritedInterfaces(
            bmtInfo,
            pParentMT,
            pParentSubstForTypeLoad,
            pParentSubstForComparing,
            pStackingAllocator,
            pMTInterfaceMapOwner);
    }
    ExpandExactDeclaredInterfaces(
        bmtInfo,
        pMT->GetModule(),
        pMT->GetCl(),
        pSubstForTypeLoad,
        pSubstForComparing,
        pStackingAllocator,
        pMTInterfaceMapOwner
        COMMA_INDEBUG(pMT));

    // Restore type's subsitution chain for comparing interfaces
    *pSubstForComparing = substForComparingBackup;
} // MethodTableBuilder::ExpandExactInheritedInterfaces

//*******************************************************************************
/* static */
void
MethodTableBuilder::ExpandExactDeclaredInterfaces(
    bmtExactInterfaceInfo *     bmtInfo,
    Module *                    pModule,
    mdToken                     typeDef,
    const Substitution *        pSubstForTypeLoad,
    Substitution *              pSubstForComparing,
    StackingAllocator *         pStackingAllocator,
    MethodTable *               pMTInterfaceMapOwner
    COMMA_INDEBUG(MethodTable * dbg_pClassMT))
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;
    InterfaceImplEnum ie(pModule, typeDef, NULL);
    while ((hr = ie.Next()) == S_OK)
    {
        MethodTable * pInterface = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
            pModule,
            ie.CurrentToken(),
            &bmtInfo->typeContext,
            ClassLoader::ThrowIfNotFound,
            ClassLoader::FailIfUninstDefOrRef,
            ClassLoader::LoadTypes,
            CLASS_LOAD_EXACTPARENTS,
            TRUE,
            pSubstForTypeLoad,
            pMTInterfaceMapOwner).GetMethodTable();

        Substitution ifaceSubstForTypeLoad(ie.CurrentToken(), pModule, pSubstForTypeLoad);
        Substitution ifaceSubstForComparing(ie.CurrentToken(), pModule, pSubstForComparing);
        ExpandExactInterface(
            bmtInfo,
            pInterface,
            &ifaceSubstForTypeLoad,
            &ifaceSubstForComparing,
            pStackingAllocator,
            pMTInterfaceMapOwner
            COMMA_INDEBUG(dbg_pClassMT));
    }
    if (FAILED(hr))
    {
        pModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(), typeDef, IDS_CLASSLOAD_BADFORMAT);
    }
} // MethodTableBuilder::ExpandExactDeclaredInterfaces

//*******************************************************************************
void
MethodTableBuilder::ExpandExactInterface(
    bmtExactInterfaceInfo *     bmtInfo,
    MethodTable *               pIntf,
    const Substitution *        pSubstForTypeLoad_OnStack,   // Allocated on stack!
    const Substitution *        pSubstForComparing_OnStack,  // Allocated on stack!
    StackingAllocator *         pStackingAllocator,
    MethodTable *               pMTInterfaceMapOwner
    COMMA_INDEBUG(MethodTable * dbg_pClassMT))
{
    STANDARD_VM_CONTRACT;

    // ****** This must be consistent with code:MethodTableBuilder::ExpandApproxInterface ******

    // Is it already present according to the "generic" layout of the interfaces.
    // Note we use exactly the same algorithm as when we
    // determined the layout of the interface map for the "generic" version of the class.
    for (DWORD i = 0; i < bmtInfo->nAssigned; i++)
    {
        // Type Equivalence is not respected for this comparision as you can have multiple type equivalent interfaces on a class
        TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
        if (MetaSig::CompareTypeDefsUnderSubstitutions(bmtInfo->pExactMTs[i],
                                                       pIntf,
                                                       &bmtInfo->pInterfaceSubstitution[i],
                                                       pSubstForComparing_OnStack,
                                                       &newVisited))
        {
#ifdef _DEBUG
            //#InjectInterfaceDuplicates_ExactInterfaces
            // We will inject duplicate interfaces in check builds.
            // Has to be in sync with code:#InjectInterfaceDuplicates_Main.
            if (dbg_pClassMT->Debug_HasInjectedInterfaceDuplicates())
            {   // Just pretend we didn't find this match
                break;
            }
#endif //_DEBUG
            return; // found it, don't add it again
        }
    }

    // Add the interface and its sub-interfaces
    DWORD n = bmtInfo->nAssigned;
    bmtInfo->pExactMTs[n] = pIntf;
    bmtInfo->pInterfaceSubstitution[n] = *pSubstForComparing_OnStack;
    bmtInfo->nAssigned++;

    Substitution * pSubstForTypeLoad = new (pStackingAllocator) Substitution(*pSubstForTypeLoad_OnStack);

    ExpandExactDeclaredInterfaces(
        bmtInfo,
        pIntf->GetModule(),
        pIntf->GetCl(),
        pSubstForTypeLoad,
        &bmtInfo->pInterfaceSubstitution[n],
        pStackingAllocator,
        pMTInterfaceMapOwner
        COMMA_INDEBUG(dbg_pClassMT));
} // MethodTableBuilder::ExpandExactInterface

//*******************************************************************************
/* static */
void MethodTableBuilder::InterfacesAmbiguityCheck(bmtInterfaceAmbiguityCheckInfo *bmtCheckInfo,
                                                  Module *pModule,
                                                  mdToken typeDef,
                                                  const Substitution *pSubstChain,
                                                  StackingAllocator *pStackingAllocator)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;
    InterfaceImplEnum ie(pModule, typeDef, pSubstChain);
    while ((hr = ie.Next()) == S_OK)
    {
        MethodTable *pInterface =
            ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, ie.CurrentToken(),
                                                        &bmtCheckInfo->typeContext,
                                                        ClassLoader::ThrowIfNotFound,
                                                        ClassLoader::FailIfUninstDefOrRef,
                                                        ClassLoader::LoadTypes,
                                                        CLASS_LOAD_EXACTPARENTS,
                                                        TRUE,
                                                        pSubstChain).GetMethodTable();
        InterfaceAmbiguityCheck(bmtCheckInfo, ie.CurrentSubst(), pInterface, pStackingAllocator);
    }
    if (FAILED(hr))
    {
        pModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(), typeDef, IDS_CLASSLOAD_BADFORMAT);
    }
}

//*******************************************************************************
void MethodTableBuilder::InterfaceAmbiguityCheck(bmtInterfaceAmbiguityCheckInfo *bmtCheckInfo,
                                                 const Substitution *pItfSubstChain,
                                                 MethodTable *pIntf,
                                                 StackingAllocator *pStackingAllocator)
{
    STANDARD_VM_CONTRACT;

    // Is it already in the generic version of the freshly declared interfaces. We
    // do this based on metadata, i.e. via the substitution chains.
    // Note we use exactly the same algorithm as when we
    // determined the layout of the interface map for the "generic" version of the class.
    for (DWORD i = 0; i < bmtCheckInfo->nAssigned; i++)
    {
        // Type Equivalence is not respected for this comparision as you can have multiple type equivalent interfaces on a class
        TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
        if (MetaSig::CompareTypeDefsUnderSubstitutions(bmtCheckInfo->ppExactDeclaredInterfaces[i],
                                                       pIntf,
                                                       bmtCheckInfo->ppInterfaceSubstitutionChains[i],
                                                       pItfSubstChain,
                                                       &newVisited))
            return; // found it, don't add it again
    }

    // OK, so it isn't a duplicate based on the generic IL, now check if the instantiation
    // makes it a duplicate.
    for (DWORD i = 0; i < bmtCheckInfo->nAssigned; i++)
    {
        if (bmtCheckInfo->ppExactDeclaredInterfaces[i] == pIntf)
        {
                bmtCheckInfo->pMT->GetModule()->GetAssembly()->ThrowTypeLoadException(bmtCheckInfo->pMT->GetMDImport(),
                                                                                      bmtCheckInfo->pMT->GetCl(),
                                                                                      IDS_CLASSLOAD_OVERLAPPING_INTERFACES);
        }
    }

    DWORD n = bmtCheckInfo->nAssigned;
    bmtCheckInfo->ppExactDeclaredInterfaces[n] = pIntf;
    bmtCheckInfo->ppInterfaceSubstitutionChains[n] = new (pStackingAllocator) Substitution[pItfSubstChain->GetLength()];
    pItfSubstChain->CopyToArray(bmtCheckInfo->ppInterfaceSubstitutionChains[n]);

    bmtCheckInfo->nAssigned++;
    InterfacesAmbiguityCheck(bmtCheckInfo,pIntf->GetModule(),pIntf->GetCl(),pItfSubstChain, pStackingAllocator);
}


//*******************************************************************************
void MethodTableBuilder::CheckForSystemTypes()
{
    STANDARD_VM_CONTRACT;

    LPCUTF8 name, nameSpace;

    MethodTable * pMT = GetHalfBakedMethodTable();
    EEClass * pClass = GetHalfBakedClass();

    // We can exit early for generic types - there are just a few cases to check for.
    if (bmtGenerics->HasInstantiation())
    {
        if (pMT->IsIntrinsicType() && pClass->HasLayout())
        {
            if (FAILED(GetMDImport()->GetNameOfTypeDef(GetCl(), &name, &nameSpace)))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
            }

            if (strcmp(nameSpace, g_IntrinsicsNS) == 0)
            {
                EEClassLayoutInfo* pLayout = pClass->GetLayoutInfo();

                // The SIMD Hardware Intrinsic types correspond to fundamental data types in the underlying ABIs:
                // * Vector64<T>:  __m64
                // * Vector128<T>: __m128
                // * Vector256<T>: __m256

                // These __m128 and __m256 types, among other requirements, are special in that they must always
                // be aligned properly.

                if (strcmp(name, g_Vector64Name) == 0)
                {
                    // The System V ABI for i386 defaults to 8-byte alignment for __m64, except for parameter passing,
                    // where it has an alignment of 4.

                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 8; // sizeof(__m64)
                }
                else if (strcmp(name, g_Vector128Name) == 0)
                {
    #ifdef TARGET_ARM
                    // The Procedure Call Standard for ARM defaults to 8-byte alignment for __m128

                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 8;
    #else
                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 16; // sizeof(__m128)
    #endif // TARGET_ARM
                }
                else if (strcmp(name, g_Vector256Name) == 0)
                {
    #ifdef TARGET_ARM
                    // No such type exists for the Procedure Call Standard for ARM. We will default
                    // to the same alignment as __m128, which is supported by the ABI.

                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 8;
    #elif defined(TARGET_ARM64)
                    // The Procedure Call Standard for ARM 64-bit (with SVE support) defaults to
                    // 16-byte alignment for __m256.

                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 16;
    #else
                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 32; // sizeof(__m256)
    #endif // TARGET_ARM elif TARGET_ARM64
                }
                else
                {
                    // These types should be handled or explicitly skipped below to ensure that we don't
                    // miss adding required ABI support for future types.

                    _ASSERTE_MSG(FALSE, "Unhandled Hardware Intrinsic Type.");
                }

                return;
            }
#if defined(UNIX_AMD64_ABI) || defined(TARGET_ARM64)
            else if (strcmp(nameSpace, g_SystemNS) == 0)
            {
                EEClassLayoutInfo* pLayout = pClass->GetLayoutInfo();

                // These types correspond to fundamental data types in the underlying ABIs:
                // * Int128:  __int128
                // * UInt128: unsigned __int128

                if ((strcmp(name, g_Int128Name) == 0) || (strcmp(name, g_UInt128Name) == 0))
                {
                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 16; // sizeof(__int128)
                }
            }
#endif // UNIX_AMD64_ABI || TARGET_ARM64
        }

        if (g_pNullableClass != NULL)
        {
            _ASSERTE(g_pByReferenceClass != NULL);
            _ASSERTE(g_pByReferenceClass->IsByRefLike());

            _ASSERTE(g_pNullableClass->IsNullable());

            // Pre-compute whether the class is a Nullable<T> so that code:Nullable::IsNullableType is efficient
            // This is useful to the performance of boxing/unboxing a Nullable
            if (GetCl() == g_pNullableClass->GetCl())
                pMT->SetIsNullable();

            return;
        }
    }

    if (IsNested() || IsEnum())
        return;

    if (FAILED(GetMDImport()->GetNameOfTypeDef(GetCl(), &name, &nameSpace)))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
    }

    if (IsValueClass())
    {
        //
        // Value types
        //

        // All special value types are in the system namespace
        if (strcmp(nameSpace, g_SystemNS) != 0)
            return;

        // Check if it is a primitive type
        CorElementType type = CorTypeInfo::FindPrimitiveType(name);
        if (type != ELEMENT_TYPE_END)
        {
            pMT->SetInternalCorElementType(type);
            pMT->SetIsTruePrimitive();

#if defined(TARGET_X86) && defined(UNIX_X86_ABI)
            switch (type)
            {
                // The System V ABI for i386 defines different packing for these types.

                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_R8:
                {
                    EEClassLayoutInfo * pLayout = pClass->GetLayoutInfo();
                    pLayout->m_ManagedLargestAlignmentRequirementOfAllMembers = 4;
                    break;
                }

                default:
                    break;
            }
#endif // TARGET_X86 && UNIX_X86_ABI

#ifdef _DEBUG
            if (FAILED(GetMDImport()->GetNameOfTypeDef(GetCl(), &name, &nameSpace)))
            {
                name = nameSpace = "Invalid TypeDef record";
            }
            LOG((LF_CLASSLOADER, LL_INFO10000, "%s::%s marked as primitive type %i\n", nameSpace, name, type));
#endif // _DEBUG
        }
        else if (strcmp(name, g_NullableName) == 0)
        {
            pMT->SetIsNullable();
        }
        else if (strcmp(name, g_RuntimeArgumentHandleName) == 0)
        {
            pMT->SetInternalCorElementType (ELEMENT_TYPE_I);
        }
        else if (strcmp(name, g_RuntimeMethodHandleInternalName) == 0)
        {
            pMT->SetInternalCorElementType (ELEMENT_TYPE_I);
        }
        else if (strcmp(name, g_RuntimeFieldHandleInternalName) == 0)
        {
            pMT->SetInternalCorElementType (ELEMENT_TYPE_I);
        }
    }
    else
    {
        //
        // Reference types
        //
        if (strcmp(name, g_StringName) == 0 && strcmp(nameSpace, g_SystemNS) == 0)
        {
            // Strings are not "normal" objects, so we need to mess with their method table a bit
            // so that the GC can figure out how big each string is...
            DWORD baseSize = StringObject::GetBaseSize();
            pMT->SetBaseSize(baseSize);

            GetHalfBakedClass()->SetBaseSizePadding(baseSize - bmtFP->NumInstanceFieldBytes);

            pMT->SetComponentSize(2);
        }
        else if (strcmp(name, g_CriticalFinalizerObjectName) == 0 && strcmp(nameSpace, g_ConstrainedExecutionNS) == 0)
        {
            // To introduce a class with a critical finalizer,
            // we'll set the bit here.
            pMT->SetHasCriticalFinalizer();
        }
#ifdef FEATURE_COMINTEROP
        else
        {
            bool bIsComObject = false;

            if (strcmp(name, g_ComObjectName) == 0 && strcmp(nameSpace, g_SystemNS) == 0)
                bIsComObject = true;


            if (bIsComObject)
            {
                // Make System.__ComObject a ComImport type
                // We can't do it using attribute as C# won't allow putting code in ComImport types
                pMT->SetComObjectType();

                // COM objects need an optional field on the EEClass, so ensure this class instance has allocated
                // the optional field descriptor.
                EnsureOptionalFieldsAreAllocated(pClass, m_pAllocMemTracker, GetLoaderAllocator()->GetLowFrequencyHeap());
            }
        }
#endif // FEATURE_COMINTEROP
    }
}

//==========================================================================================
// Helper to create a new method table. This is the only
// way to allocate a new MT. Don't try calling new / ctor.
// Called from SetupMethodTable
// This needs to be kept consistent with MethodTable::GetSavedExtent()
MethodTable * MethodTableBuilder::AllocateNewMT(
    Module *pLoaderModule,
    DWORD dwVtableSlots,
    DWORD dwVirtuals,
    DWORD dwGCSize,
    DWORD dwNumInterfaces,
    DWORD dwNumDicts,
    DWORD cbInstAndDict,
    MethodTable *pMTParent,
    ClassLoader *pClassLoader,
    LoaderAllocator *pAllocator,
    BOOL isInterface,
    BOOL fDynamicStatics,
    BOOL fHasGenericsStaticsInfo,
    BOOL fHasVirtualStaticMethods
#ifdef FEATURE_COMINTEROP
        , BOOL fHasDynamicInterfaceMap
#endif
        , AllocMemTracker *pamTracker
    )
{
    CONTRACT (MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    DWORD dwNonVirtualSlots = dwVtableSlots - dwVirtuals;

    // GCSize must be aligned
    _ASSERTE(IS_ALIGNED(dwGCSize, sizeof(void*)));

    // size without the interface map
    S_SIZE_T cbTotalSize = S_SIZE_T(dwGCSize) + S_SIZE_T(sizeof(MethodTable));

    // vtable
    cbTotalSize += MethodTable::GetNumVtableIndirections(dwVirtuals) * sizeof(MethodTable::VTableIndir_t);

    DWORD dwMultipurposeSlotsMask = 0;
    if (dwNumInterfaces != 0)
        dwMultipurposeSlotsMask |= MethodTable::enum_flag_HasInterfaceMap;
    if (dwNumDicts != 0)
        dwMultipurposeSlotsMask |= MethodTable::enum_flag_HasPerInstInfo;
    if (bmtVT->pDispatchMapBuilder->Count() > 0)
        dwMultipurposeSlotsMask |= MethodTable::enum_flag_HasDispatchMapSlot;
    if (dwNonVirtualSlots != 0)
        dwMultipurposeSlotsMask |= MethodTable::enum_flag_HasNonVirtualSlots;
    if (pLoaderModule != GetModule())
        dwMultipurposeSlotsMask |= MethodTable::enum_flag_HasModuleOverride;

    // Add space for optional members here. Same as GetOptionalMembersSize()
    cbTotalSize += MethodTable::GetOptionalMembersAllocationSize(dwMultipurposeSlotsMask,
                                                      fHasGenericsStaticsInfo,
                                                      RidFromToken(GetCl()) >= METHODTABLE_TOKEN_OVERFLOW);

    // Interface map starts here
    S_SIZE_T offsetOfInterfaceMap = cbTotalSize;

    cbTotalSize += S_SIZE_T(dwNumInterfaces) * S_SIZE_T(sizeof(InterfaceInfo_t));

#ifdef FEATURE_COMINTEROP
    // DynamicInterfaceMap have an extra DWORD added to the end of the normal interface
    // map. This will be used to store the count of dynamically added interfaces
    // (the ones that are not in  the metadata but are QI'ed for at runtime).
    cbTotalSize += S_SIZE_T(fHasDynamicInterfaceMap ? sizeof(DWORD_PTR) : 0);
#endif

    // Dictionary pointers start here
    S_SIZE_T offsetOfInstAndDict = cbTotalSize;

    if (dwNumDicts != 0)
    {
        cbTotalSize += sizeof(GenericsDictInfo);
        cbTotalSize += S_SIZE_T(dwNumDicts) * S_SIZE_T(sizeof(MethodTable::PerInstInfoElem_t));
        cbTotalSize += cbInstAndDict;
    }

    S_SIZE_T offsetOfUnsharedVtableChunks = cbTotalSize;

    BOOL canShareVtableChunks = pMTParent && MethodTable::CanShareVtableChunksFrom(pMTParent, pLoaderModule
        );

    // If pMTParent has a generic instantiation, we cannot share its vtable chunks
    // This is because pMTParent is only approximate at this point, and MethodTableBuilder::CopyExactParentSlots
    // may swap in an exact parent that does not satisfy CanShareVtableChunksFrom
    if (pMTParent && pMTParent->HasInstantiation())
    {
        canShareVtableChunks = FALSE;
    }

    // We will share any parent vtable chunk that does not contain a method we overrode (or introduced)
    // For the rest, we need to allocate space
    for (DWORD i = 0; i < dwVirtuals; i++)
    {
        if (!canShareVtableChunks || ChangesImplementationOfVirtualSlot(static_cast<SLOT_INDEX>(i)))
        {
            DWORD chunkStart = MethodTable::GetStartSlotForVtableIndirection(MethodTable::GetIndexOfVtableIndirection(i), dwVirtuals);
            DWORD chunkEnd = MethodTable::GetEndSlotForVtableIndirection(MethodTable::GetIndexOfVtableIndirection(i), dwVirtuals);

            cbTotalSize += S_SIZE_T(chunkEnd - chunkStart) * S_SIZE_T(sizeof(PCODE));

            i = chunkEnd - 1;
        }
    }

    // Add space for the non-virtual slots array (pointed to by an optional member) if required
    // If there is only one non-virtual slot, we store it directly in the optional member and need no array
    S_SIZE_T offsetOfNonVirtualSlots = cbTotalSize;
    if (dwNonVirtualSlots > 1)
    {
        cbTotalSize += S_SIZE_T(dwNonVirtualSlots) * S_SIZE_T(sizeof(PCODE));
    }

    BYTE *pData = (BYTE *)pamTracker->Track(pAllocator->GetHighFrequencyHeap()->AllocMem(cbTotalSize));

    _ASSERTE(IS_ALIGNED(pData, TARGET_POINTER_SIZE));

    // There should be no overflows if we have allocated the memory succesfully
    _ASSERTE(!cbTotalSize.IsOverflow());

    MethodTable* pMT = (MethodTable*)(pData + dwGCSize);

    pMT->SetMultipurposeSlotsMask(dwMultipurposeSlotsMask);

    MethodTableWriteableData * pMTWriteableData = (MethodTableWriteableData *) (BYTE *)
        pamTracker->Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(MethodTableWriteableData))));
    // Note: Memory allocated on loader heap is zero filled
    pMT->SetWriteableData(pMTWriteableData);

    // This also disables IBC logging until the type is sufficiently intitialized so
    // it needs to be done early
    pMTWriteableData->SetIsNotFullyLoadedForBuildMethodTable();

#ifdef _DEBUG
    pClassLoader->m_dwGCSize += dwGCSize;
    pClassLoader->m_dwInterfaceMapSize += (dwNumInterfaces * sizeof(InterfaceInfo_t));
    pClassLoader->m_dwMethodTableSize += (DWORD)cbTotalSize.Value();
    pClassLoader->m_dwVtableData += (dwVtableSlots * sizeof(PCODE));
#endif // _DEBUG

    // There should be no overflows if we have allocated the memory succesfully
    _ASSERTE(!offsetOfUnsharedVtableChunks.IsOverflow());
    _ASSERTE(!offsetOfNonVirtualSlots.IsOverflow());
    _ASSERTE(!offsetOfInterfaceMap.IsOverflow());
    _ASSERTE(!offsetOfInstAndDict.IsOverflow());

    // initialize the total number of slots
    pMT->SetNumVirtuals(static_cast<WORD>(dwVirtuals));
    if (fHasVirtualStaticMethods)
    {
        pMT->SetHasVirtualStaticMethods();
    }

    pMT->SetParentMethodTable(pMTParent);

    // Fill out the vtable indirection slots
    SIZE_T dwCurrentUnsharedSlotOffset = offsetOfUnsharedVtableChunks.Value();
    MethodTable::VtableIndirectionSlotIterator it = pMT->IterateVtableIndirectionSlots();
    while (it.Next())
    {
        BOOL shared = canShareVtableChunks;

        // Recalculate whether we will share this chunk
        if (canShareVtableChunks)
        {
            for (DWORD i = it.GetStartSlot(); i < it.GetEndSlot(); i++)
            {
                if (ChangesImplementationOfVirtualSlot(static_cast<SLOT_INDEX>(i)))
                {
                    shared = FALSE;
                    break;
                }
            }
        }

        if (shared)
        {
            // Share the parent chunk
            _ASSERTE(it.GetEndSlot() <= pMTParent->GetNumVirtuals());
            it.SetIndirectionSlot(pMTParent->GetVtableIndirections()[it.GetIndex()]);
        }
        else
        {
            // Use the locally allocated chunk
            it.SetIndirectionSlot((MethodTable::VTableIndir2_t *)(pData+dwCurrentUnsharedSlotOffset));
            dwCurrentUnsharedSlotOffset += it.GetSize();
        }
    }

#ifdef FEATURE_COMINTEROP
    // Extensible RCW's are prefixed with the count of dynamic interfaces.
    if (fHasDynamicInterfaceMap)
    {
        _ASSERTE (dwNumInterfaces > 0);
        pMT->SetInterfaceMap ((WORD) (dwNumInterfaces), (InterfaceInfo_t*)(pData+offsetOfInterfaceMap.Value()+sizeof(DWORD_PTR)));

        *(((DWORD_PTR *)pMT->GetInterfaceMap()) - 1) = 0;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        // interface map is at the end of the vtable
        pMT->SetInterfaceMap ((WORD) dwNumInterfaces, (InterfaceInfo_t *)(pData+offsetOfInterfaceMap.Value()));
    }

    _ASSERTE(((WORD) dwNumInterfaces) == dwNumInterfaces);

    if (fDynamicStatics)
    {
        pMT->SetDynamicStatics(fHasGenericsStaticsInfo);
    }

    if (dwNonVirtualSlots > 0)
    {
        if (dwNonVirtualSlots > 1)
        {
            pMT->SetNonVirtualSlotsArray((PTR_PCODE)(pData+offsetOfNonVirtualSlots.Value()));
        }
        else
        {
            pMT->SetHasSingleNonVirtualSlot();
        }
    }

    // the dictionary pointers follow the interface map
    if (dwNumDicts)
    {
        MethodTable::PerInstInfoElem_t *pPerInstInfo = (MethodTable::PerInstInfoElem_t *)(pData + offsetOfInstAndDict.Value() + sizeof(GenericsDictInfo));

        pMT->SetPerInstInfo ( pPerInstInfo);

        // Fill in the dictionary for this type, if it's instantiated
        if (cbInstAndDict)
        {
            MethodTable::PerInstInfoElem_t *pPInstInfo = (MethodTable::PerInstInfoElem_t *)(pPerInstInfo + (dwNumDicts-1));
            *pPInstInfo = (Dictionary*) (pPerInstInfo + dwNumDicts);
        }
    }

#ifdef _DEBUG
    pMT->m_pWriteableData->m_dwLastVerifedGCCnt = (DWORD)-1;
#endif // _DEBUG

    RETURN(pMT);
}


//*******************************************************************************
//
// Used by BuildMethodTable
//
// Setup the method table
//
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif // _PREFAST_

VOID
MethodTableBuilder::SetupMethodTable2(
        Module * pLoaderModule)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtInternal));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtMFDescs));
        PRECONDITION(CheckPointer(bmtEnumFields));
        PRECONDITION(CheckPointer(bmtError));
        PRECONDITION(CheckPointer(bmtMetaData));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(bmtGenerics));
    }
    CONTRACTL_END;

    DWORD i;

#ifdef FEATURE_COMINTEROP
    BOOL fHasDynamicInterfaceMap = bmtInterface->dwInterfaceMapSize > 0 &&
                                   bmtProp->fIsComObjectType &&
                                   (GetParentMethodTable() != g_pObjectClass);
#endif // FEATURE_COMINTEROP

    EEClass *pClass = GetHalfBakedClass();

    DWORD cbDictSlotSize = 0;
    DWORD cbDictAllocSize = 0;
    if (bmtGenerics->HasInstantiation())
    {
        cbDictAllocSize = DictionaryLayout::GetDictionarySizeFromLayout(bmtGenerics->GetNumGenericArgs(), pClass->GetDictionaryLayout(), &cbDictSlotSize);
    }

    DWORD dwGCSize;

    if (bmtFP->NumGCPointerSeries > 0)
    {
        dwGCSize = (DWORD)CGCDesc::ComputeSize(bmtFP->NumGCPointerSeries);
    }
    else
    {
        dwGCSize = 0;
    }

    pClass->SetNumMethods(bmtVT->cTotalSlots);
    pClass->SetNumNonVirtualSlots(bmtVT->cVtableSlots - bmtVT->cVirtualSlots);

    // Now setup the method table
    // interface map is allocated along with the method table
    MethodTable *pMT = AllocateNewMT(pLoaderModule,
                                   bmtVT->cVtableSlots,
                                   bmtVT->cVirtualSlots,
                                   dwGCSize,
                                   bmtInterface->dwInterfaceMapSize,
                                   bmtGenerics->numDicts,
                                   cbDictAllocSize,
                                   GetParentMethodTable(),
                                   GetClassLoader(),
                                   bmtAllocator,
                                   IsInterface(),
                                   bmtProp->fDynamicStatics,
                                   bmtProp->fGenericsStatics,
                                   bmtProp->fHasVirtualStaticMethods,
#ifdef FEATURE_COMINTEROP
                                   fHasDynamicInterfaceMap,
#endif
                                   GetMemTracker());

    pMT->SetClass(pClass);
    pClass->m_pMethodTable = pMT;
    m_pHalfBakedMT = pMT;

#ifdef _DEBUG
    pMT->SetDebugClassName(GetDebugClassName());
#endif

    if (IsInterface())
        pMT->SetIsInterface();

    if (GetParentMethodTable() != NULL)
    {
        if (GetParentMethodTable()->HasModuleDependencies())
        {
            pMT->SetHasModuleDependencies();
        }
        else
        {
            Module * pModule = GetModule();
            Module * pParentModule = GetParentMethodTable()->GetModule();
            if (pModule != pParentModule)
            {
                pMT->SetHasModuleDependencies();
            }
        }

        if (GetParentMethodTable()->HasPreciseInitCctors() || !pClass->IsBeforeFieldInit())
        {
            pMT->SetHasPreciseInitCctors();
        }
    }

    // Must be done early because various methods test HasInstantiation() and ContainsGenericVariables()
    if (bmtGenerics->GetNumGenericArgs() != 0)
    {
        pMT->SetHasInstantiation(bmtGenerics->fTypicalInstantiation, bmtGenerics->fSharedByGenericInstantiations);

        if (bmtGenerics->fContainsGenericVariables)
            pMT->SetContainsGenericVariables();
    }

    if (bmtGenerics->numDicts != 0)
    {
        if (!FitsIn<WORD>(bmtGenerics->GetNumGenericArgs()))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_TOOMANYGENERICARGS);
        }

        pMT->SetDictInfo(bmtGenerics->numDicts,
            static_cast<WORD>(bmtGenerics->GetNumGenericArgs()));
    }

    CONSISTENCY_CHECK(pMT->GetNumGenericArgs() == bmtGenerics->GetNumGenericArgs());
    CONSISTENCY_CHECK(pMT->GetNumDicts() == bmtGenerics->numDicts);
    CONSISTENCY_CHECK(pMT->HasInstantiation() == bmtGenerics->HasInstantiation());
    CONSISTENCY_CHECK(pMT->HasInstantiation() == !pMT->GetInstantiation().IsEmpty());

    pMT->SetLoaderModule(pLoaderModule);
    pMT->SetLoaderAllocator(bmtAllocator);

    pMT->SetModule(GetModule());

    pMT->SetInternalCorElementType (ELEMENT_TYPE_CLASS);

    SetNonGCRegularStaticFieldBytes (bmtProp->dwNonGCRegularStaticFieldBytes);
    SetNonGCThreadStaticFieldBytes (bmtProp->dwNonGCThreadStaticFieldBytes);

#ifdef FEATURE_TYPEEQUIVALENCE
    if (bmtProp->fHasTypeEquivalence)
    {
        pMT->SetHasTypeEquivalence();
    }
#endif //FEATURE_TYPEEQUIVALENCE

#ifdef FEATURE_COMINTEROP
    if (bmtProp->fSparse)
        pClass->SetSparseForCOMInterop();
#endif // FEATURE_COMINTEROP

    if (bmtVT->pCCtor != NULL)
    {
        pMT->SetHasClassConstructor();
        CONSISTENCY_CHECK(pMT->GetClassConstructorSlot() == bmtVT->pCCtor->GetSlotIndex());
    }
    if (bmtVT->pDefaultCtor != NULL)
    {
        pMT->SetHasDefaultConstructor();
        CONSISTENCY_CHECK(pMT->GetDefaultConstructorSlot() == bmtVT->pDefaultCtor->GetSlotIndex());
    }

    for (MethodDescChunk *pChunk = GetHalfBakedClass()->GetChunks(); pChunk != NULL; pChunk = pChunk->GetNextChunk())
    {
        pChunk->SetMethodTable(pMT);
    }

#ifdef _DEBUG
    {
        DeclaredMethodIterator it(*this);
        while (it.Next())
        {
            MethodDesc *pMD = it->GetMethodDesc();
            if (pMD != NULL)
            {
                pMD->m_pDebugMethodTable = pMT;
                pMD->m_pszDebugMethodSignature = FormatSig(pMD, GetLoaderAllocator()->GetLowFrequencyHeap(), GetMemTracker());
            }
            MethodDesc *pUnboxedMD = it->GetUnboxedMethodDesc();
            if (pUnboxedMD != NULL)
            {
                pUnboxedMD->m_pDebugMethodTable = pMT;
                pUnboxedMD->m_pszDebugMethodSignature = FormatSig(pUnboxedMD, GetLoaderAllocator()->GetLowFrequencyHeap(), GetMemTracker());
            }
        }
    }
#endif // _DEBUG

    // Note that for value classes, the following calculation is only appropriate
    // when the instance is in its "boxed" state.
    if (!IsInterface())
    {
        DWORD baseSize = Max<DWORD>(bmtFP->NumInstanceFieldBytes + OBJECT_BASESIZE, MIN_OBJECT_SIZE);
        baseSize = (baseSize + ALLOC_ALIGN_CONSTANT) & ~ALLOC_ALIGN_CONSTANT;  // m_BaseSize must be aligned
        pMT->SetBaseSize(baseSize);

        GetHalfBakedClass()->SetBaseSizePadding(baseSize - bmtFP->NumInstanceFieldBytes);

        if (bmtProp->fIsComObjectType)
        {   // Propagate the com specific info
            pMT->SetComObjectType();
#ifdef FEATURE_COMINTEROP
            // COM objects need an optional field on the EEClass, so ensure this class instance has allocated
            // the optional field descriptor.
            EnsureOptionalFieldsAreAllocated(pClass, m_pAllocMemTracker, GetLoaderAllocator()->GetLowFrequencyHeap());
#endif // FEATURE_COMINTEROP
        }
    }
    else
    {
#ifdef FEATURE_COMINTEROP
        // If this is an interface then we need to set the ComInterfaceType to
        // -1 to indicate we have not yet determined the interface type.
        pClass->SetComInterfaceType((CorIfaceAttr)-1);

        // If this is a special COM event interface, then mark the MT as such.
        if (bmtProp->fComEventItfType)
        {
            pClass->SetComEventItfType();
        }
#endif // FEATURE_COMINTEROP
    }
    _ASSERTE((pMT->IsInterface() == 0) == (IsInterface() == 0));

    FieldDesc *pFieldDescList = pClass->GetFieldDescList();
    // Set all field slots to point to the newly created MethodTable
    for (i = 0; i < (bmtEnumFields->dwNumStaticFields + bmtEnumFields->dwNumInstanceFields); i++)
    {
        pFieldDescList[i].m_pMTOfEnclosingClass = pMT;
    }

    // Fill in type parameters before looking up exact parent or fetching the types of any field descriptors!
    // This must come before the use of GetFieldType in the value class representation optimization below.
    if (bmtGenerics->GetNumGenericArgs() != 0)
    {
        // Space has already been allocated for the instantiation but the parameters haven't been filled in
        Instantiation destInst = pMT->GetInstantiation();
        Instantiation inst = bmtGenerics->GetInstantiation();

        // So fill them in...
        TypeHandle * pInstDest = (TypeHandle *)destInst.GetRawArgs();
        for (DWORD j = 0; j < bmtGenerics->GetNumGenericArgs(); j++)
        {
            pInstDest[j] = inst[j];
        }

        PTR_DictionaryLayout pLayout = pClass->GetDictionaryLayout();
        if (pLayout != NULL)
        {
            _ASSERTE(pLayout->GetMaxSlots() > 0);

            PTR_Dictionary pDictionarySlots = pMT->GetPerInstInfo()[bmtGenerics->numDicts - 1];
            DWORD* pSizeSlot = (DWORD*)(pDictionarySlots + bmtGenerics->GetNumGenericArgs());
            *pSizeSlot = cbDictSlotSize;
        }
    }

    CorElementType normalizedType = ELEMENT_TYPE_CLASS;
    if (IsValueClass())
    {
        if (IsEnum())
        {
            if (GetNumInstanceFields() != 1 ||
                !CorTypeInfo::IsPrimitiveType(pFieldDescList[0].GetFieldType()))
            {
                BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_FIELD, mdTokenNil);
            }
            CONSISTENCY_CHECK(!pFieldDescList[0].IsStatic());
            normalizedType = pFieldDescList->GetFieldType();
        }
        else
        {
            normalizedType = ELEMENT_TYPE_VALUETYPE;
        }
    }
    pMT->SetInternalCorElementType(normalizedType);

    if (bmtProp->fIsIntrinsicType)
    {
        pMT->SetIsIntrinsicType();
    }

    if (GetModule()->IsSystem())
    {
        CheckForSystemTypes();
    }

    // Now fill in the real interface map with the approximate interfaces
    if (bmtInterface->dwInterfaceMapSize > 0)
    {
        // First ensure we have enough space to record extra flag information for each interface (we don't
        // record this directly into each interface map entry since these flags don't pack well due to
        // alignment).
        PVOID pExtraInterfaceInfo = NULL;
        SIZE_T cbExtraInterfaceInfo = MethodTable::GetExtraInterfaceInfoSize(bmtInterface->dwInterfaceMapSize);
        if (cbExtraInterfaceInfo)
            pExtraInterfaceInfo = GetMemTracker()->Track(GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(cbExtraInterfaceInfo)));

        // Call this even in the case where pExtraInterfaceInfo == NULL (certain cases are optimized and don't
        // require extra buffer space).
        pMT->InitializeExtraInterfaceInfo(pExtraInterfaceInfo);

        InterfaceInfo_t *pInterfaces = pMT->GetInterfaceMap();

        CONSISTENCY_CHECK(CheckPointer(pInterfaces));

        // Copy the interface map member by member so there is no junk in the padding.
        for (i = 0; i < bmtInterface->dwInterfaceMapSize; i++)
        {
            bmtInterfaceEntry * pEntry = &bmtInterface->pInterfaceMap[i];

            if (pEntry->IsDeclaredOnType())
                pMT->SetInterfaceDeclaredOnClass(i);
            _ASSERTE(!!pEntry->IsDeclaredOnType() == !!pMT->IsInterfaceDeclaredOnClass(i));

            pInterfaces[i].SetMethodTable(pEntry->GetInterfaceType()->GetMethodTable());
        }
    }

    pMT->SetCl(GetCl());

    // The type is sufficiently initialized for most general purpose accessor methods to work.
    // Mark the type as restored to avoid avoid asserts. Note that this also enables IBC logging.
    pMT->GetWriteableDataForWrite_NoLogging()->SetIsRestoredForBuildMethodTable();

#ifdef _DEBUG
    // Store status if we tried to inject duplicate interfaces
    if (bmtInterface->dbg_fShouldInjectInterfaceDuplicates)
        pMT->Debug_SetHasInjectedInterfaceDuplicates();
#endif //_DEBUG

    // Keep bmtInterface data around since we no longer write the flags (IsDeclaredOnType and
    // IsImplementedByParent) into the interface map (these flags are only required during type loading).

    {
        for (MethodDescChunk *pChunk = GetHalfBakedClass()->GetChunks(); pChunk != NULL; pChunk = pChunk->GetNextChunk())
        {
            // Make sure that temporary entrypoints are create for methods. NGEN uses temporary
            // entrypoints as surrogate keys for precodes.
            pChunk->EnsureTemporaryEntryPointsCreated(GetLoaderAllocator(), GetMemTracker());
        }
    }

    {   // copy onto the real vtable (methods only)
        //@GENERICS: Because we sometimes load an inexact parent (see ClassLoader::GetParent) the inherited slots might
        // come from the wrong place and need fixing up once we know the exact parent

        for (bmtVtable::Iterator slotIt = bmtVT->IterateSlots(); !slotIt.AtEnd(); ++slotIt)
        {
            SLOT_INDEX iCurSlot = static_cast<SLOT_INDEX>(slotIt.CurrentIndex());

            // We want the unboxed MethodDesc if we're out of the virtual method range
            // and the method we're dealing with has an unboxing method. If so, then
            // the unboxing method was placed in the virtual section of the vtable and
            // we now need to place the unboxed version.
            MethodDesc * pMD = NULL;
            if (iCurSlot < bmtVT->cVirtualSlots || !slotIt->Impl().AsMDMethod()->IsUnboxing())
            {
                pMD = slotIt->Impl().GetMethodDesc();
                CONSISTENCY_CHECK(slotIt->Decl().GetSlotIndex() == iCurSlot);
            }
            else
            {
                pMD = slotIt->Impl().AsMDMethod()->GetUnboxedMethodDesc();
                CONSISTENCY_CHECK(pMD->GetSlot() == iCurSlot);
            }

            CONSISTENCY_CHECK(CheckPointer(pMD));

            if (pMD->GetMethodTable() != pMT)
            {
                //
                // Inherited slots
                //
                // Do not write into vtable chunks shared with parent. It would introduce race
                // with code:MethodDesc::SetStableEntryPointInterlocked.
                //
                DWORD indirectionIndex = MethodTable::GetIndexOfVtableIndirection(iCurSlot);
                if (GetParentMethodTable()->GetVtableIndirections()[indirectionIndex] != pMT->GetVtableIndirections()[indirectionIndex])
                    pMT->SetSlot(iCurSlot, pMD->GetInitialEntryPointForCopiedSlot());
            }
            else
            {
                //
                // Owned slots
                //
                _ASSERTE(iCurSlot >= bmtVT->cVirtualSlots || ChangesImplementationOfVirtualSlot(iCurSlot));

                PCODE addr = pMD->GetTemporaryEntryPoint();
                _ASSERTE(addr != NULL);

                if (pMD->HasNonVtableSlot())
                {
                    *((PCODE *)pMD->GetAddrOfSlot()) = addr;
                }
                else
                {
                    pMT->SetSlot(iCurSlot, addr);
                }

                if (pMD->GetSlot() == iCurSlot && pMD->RequiresStableEntryPoint())
                {
                    // The rest of the system assumes that certain methods always have stable entrypoints.
                    // Create them now.
                    pMD->GetOrCreatePrecode();
                }
            }
        }
    }

    // If we have any entries, then finalize them and allocate the object in class loader heap
    DispatchMap                 *pDispatchMap        = NULL;
    DispatchMapBuilder          *pDispatchMapBuilder = bmtVT->pDispatchMapBuilder;
    CONSISTENCY_CHECK(CheckPointer(pDispatchMapBuilder));

    if (pDispatchMapBuilder->Count() > 0)
    {
        // Create a map in stacking memory.
        BYTE * pbMap;
        UINT32 cbMap;
        DispatchMap::CreateEncodedMapping(
            pMT,
            pDispatchMapBuilder,
            pDispatchMapBuilder->GetAllocator(),
            &pbMap,
            &cbMap);

        // Now finalize the impltable and allocate the block in the low frequency loader heap
        size_t objSize = (size_t) DispatchMap::GetObjectSize(cbMap);
        void * pv = AllocateFromLowFrequencyHeap(S_SIZE_T(objSize));
        _ASSERTE(pv != NULL);

        // Use placement new
        pDispatchMap = new (pv) DispatchMap(pbMap, cbMap);
        pMT->SetDispatchMap(pDispatchMap);

#ifdef LOGGING
        g_sdStats.m_cDispatchMap++;
        g_sdStats.m_cbDispatchMap += (UINT32) objSize;
        LOG((LF_LOADER, LL_INFO1000, "SD: Dispatch map for %s: %d bytes for map, %d bytes total for object.\n",
            pMT->GetDebugClassName(), cbMap, objSize));
#endif // LOGGING

    }

    // GetMethodData by default will cache its result. However, in the case that we're
    // building a MethodTable, we aren't guaranteed that this type is going to successfully
    // load and so caching it would result in errors down the road since the memory and
    // type occupying the same memory location would very likely be incorrect. The second
    // argument specifies that GetMethodData should not cache the returned object.
    MethodTable::MethodDataWrapper hMTData(MethodTable::GetMethodData(pMT, FALSE));

    if (!IsInterface())
    {
        // Propagate inheritance.

        // NOTE: In the world of unfolded interface this was used to propagate overrides into
        //       the unfolded interface vtables to make sure that overrides of virtual methods
        //       also overrode the interface methods that they contributed to. This had the
        //       unfortunate side-effect of also overwriting regular vtable slots that had been
        //       methodimpl'd and as a result changed the meaning of methodimpl from "substitute
        //       the body of method A with the body of method B" to "unify the slots of methods
        //       A and B". But now compilers have come to rely on this side-effect and it can
        //       not be brought back to its originally intended behaviour.

        // For every slot whose body comes from another slot (determined by getting the MethodDesc
        // for a slot and seeing if MethodDesc::GetSlot returns a different value than the slot
        // from which the MethodDesc was recovered), copy the value of the slot stated by the
        // MethodDesc over top of the current slot.

        // Because of the way slot unification works, we need to iterate the enture vtable until
        // no slots need updated. To understand this, imagine the following:
        //      C1::M1 is overridden by C2::M2
        //      C1::M2 is methodImpled by C1::M3
        //      C1::M3 is overridden by C2::M3
        // This should mean that C1::M1 is implemented by C2::M3, but if we didn't run the below
        // for loop a second time, this would not be propagated properly - it would only be placed
        // into the slot for C1::M2 and never make its way up to C1::M1.

        BOOL fChangeMade;
        do
        {
            fChangeMade = FALSE;
            for (i = 0; i < pMT->GetNumVirtuals(); i++)
            {
                MethodDesc* pMD = hMTData->GetImplMethodDesc(i);

                CONSISTENCY_CHECK(CheckPointer(pMD));
                CONSISTENCY_CHECK(pMD == pMT->GetMethodDescForSlot(i));

                // This indicates that the method body in this slot was copied here through a methodImpl.
                // Thus, copy the value of the slot from which the body originally came, in case it was
                // overridden, to make sure the two slots stay in sync.
                DWORD originalIndex = pMD->GetSlot();
                if (originalIndex != i)
                {
                    MethodDesc *pOriginalMD = hMTData->GetImplMethodDesc(originalIndex);
                    CONSISTENCY_CHECK(CheckPointer(pOriginalMD));
                    CONSISTENCY_CHECK(pOriginalMD == pMT->GetMethodDescForSlot(originalIndex));
                    if (pMD != pOriginalMD)
                    {
                        // Copy the slot value in the method's original slot.
                        pMT->SetSlot(i, pOriginalMD->GetInitialEntryPointForCopiedSlot());
                        hMTData->InvalidateCachedVirtualSlot(i);

                        // Update the pMD to the new method desc we just copied over ourselves with. This will
                        // be used in the check for missing method block below.
                        pMD = pOriginalMD;

                        // This method is now duplicate
                        pMD->SetDuplicate();
                        INDEBUG(g_dupMethods++;)
                        fChangeMade = TRUE;
                    }
                }
            }
        }
        while (fChangeMade);
    }

    if (!bmtProp->fNoSanityChecks)
        VerifyVirtualMethodsImplemented(hMTData);

#ifdef _DEBUG
    {
        for (bmtVtable::Iterator i = bmtVT->IterateSlots();
             !i.AtEnd(); ++i)
        {
            _ASSERTE(i->Impl().GetMethodDesc() != NULL);
        }
    }
#endif // _DEBUG


#ifdef FEATURE_COMINTEROP
    // for ComObject types, i.e. if the class extends from a COM Imported
    // class
    // make sure any interface implementated by the COM Imported class
    // is overridden fully, (OR) not overridden at all..
    if (bmtProp->fIsComObjectType)
    {
        MethodTable::InterfaceMapIterator intIt = pMT->IterateInterfaceMap();
        while (intIt.Next())
        {
            MethodTable* pIntfMT = intIt.GetInterface(pMT, pMT->GetLoadLevel());
            if (pIntfMT->GetNumVirtuals() != 0)
            {
                BOOL hasComImportMethod = FALSE;
                BOOL hasManagedMethod = FALSE;

                // NOTE: Avoid caching the MethodData object for the type being built.
                MethodTable::MethodDataWrapper hItfImplData(MethodTable::GetMethodData(pIntfMT, pMT, FALSE));
                MethodTable::MethodIterator it(hItfImplData);
                for (;it.IsValid(); it.Next())
                {
                    MethodDesc *pClsMD = NULL;
                    // If we fail to find an _IMPLEMENTATION_ for the interface MD, then
                    // we are a ComImportMethod, otherwise we still be a ComImportMethod or
                    // we can be a ManagedMethod.
                    DispatchSlot impl(it.GetTarget());
                    if (!impl.IsNull())
                    {
                        pClsMD = it.GetMethodDesc();

                        CONSISTENCY_CHECK(!pClsMD->IsInterface());
                        if (pClsMD->GetClass()->IsComImport())
                        {
                            hasComImportMethod = TRUE;
                        }
                        else
                        {
                            hasManagedMethod = TRUE;
                        }
                    }
                    else
                    {
                        // Need to set the pClsMD for the error reporting below.
                        pClsMD = it.GetDeclMethodDesc();
                        CONSISTENCY_CHECK(CheckPointer(pClsMD));
                        hasComImportMethod = TRUE;
                    }

                    // One and only one of the two must be set.
                    if ((hasComImportMethod && hasManagedMethod) ||
                        (!hasComImportMethod && !hasManagedMethod))
                    {
                        BuildMethodTableThrowException(IDS_EE_BAD_COMEXTENDS_CLASS, pClsMD->GetNameOnNonArrayClass());
                    }
                }
            }
        }
    }

    // For COM event interfaces, we need to make sure that all the methods are
    // methods to add or remove events. This means that they all need to take
    // a delegate derived class and have a void return type.
    if (bmtProp->fComEventItfType)
    {
        // COM event interfaces had better be interfaces.
        CONSISTENCY_CHECK(IsInterface());

        // Go through all the methods and check the validity of the signature.
        // NOTE: Uses hMTData to avoid caching a MethodData object for the type being built.
        MethodTable::MethodIterator it(hMTData);
        for (;it.IsValid(); it.Next())
        {
            MethodDesc* pMD = it.GetMethodDesc();
            _ASSERTE(pMD);

            MetaSig Sig(pMD);

            {
                CONTRACT_VIOLATION(LoadsTypeViolation);
                if (Sig.GetReturnType() != ELEMENT_TYPE_VOID ||
                    Sig.NumFixedArgs() != 1 ||
                    Sig.NextArg() != ELEMENT_TYPE_CLASS ||
                    !Sig.GetLastTypeHandleThrowing().CanCastTo(TypeHandle(g_pDelegateClass)))
                {
                    BuildMethodTableThrowException(IDS_EE_BAD_COMEVENTITF_CLASS, pMD->GetNameOnNonArrayClass());
                }
            }
        }
    }
#endif // FEATURE_COMINTEROP

    // If this class uses any VTS (Version Tolerant Serialization) features
    // (event callbacks or OptionalField attributes) we've previously cached the
    // additional information in the bmtMFDescs structure. Now it's time to add
    // this information as an optional extension to the MethodTable.
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

// Returns true if there is at least one default implementation for this interface method
// We don't care about conflicts at this stage in order to avoid impact type load performance
BOOL MethodTableBuilder::HasDefaultInterfaceImplementation(bmtRTType *pDeclType, MethodDesc *pDeclMD)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_DEFAULT_INTERFACES
    // If the interface method is already non-abstract, we are done
    if (!pDeclMD->IsAbstract())
        return TRUE;

    // If the method is an abstract MethodImpl, this is a reabstraction:
    //
    // interface IFoo { void Frob() { } }
    // interface IBar : IFoo { abstract void IFoo.Frob() }
    //
    // We don't require these to have an implementation because they're final anyway.
    if (pDeclMD->IsMethodImpl())
    {
        assert(pDeclMD->IsFinal());
        return TRUE;
    }

    int targetSlot = pDeclMD->GetSlot();

    // Iterate over all the interfaces this type implements
    bmtInterfaceEntry * pItfEntry = NULL;
    for (DWORD i = 0; i < bmtInterface->dwInterfaceMapSize; i++)
    {
        bmtRTType * pCurItf = bmtInterface->pInterfaceMap[i].GetInterfaceType();

        Module * pCurIntfModule = pCurItf->GetMethodTable()->GetModule();

        // Go over the methods on the interface
        MethodTable::IntroducedMethodIterator methIt(pCurItf->GetMethodTable());
        for (; methIt.IsValid(); methIt.Next())
        {
            MethodDesc * pPotentialImpl = methIt.GetMethodDesc();

            // If this interface method is not a MethodImpl, it can't possibly implement
            // the interface method we are looking for
            if (!pPotentialImpl->IsMethodImpl())
                continue;

            // Go over all the decls this MethodImpl is implementing
            MethodImpl::Iterator it(pPotentialImpl);
            for (; it.IsValid(); it.Next())
            {
                MethodDesc *pPotentialDecl = it.GetMethodDesc();

                // Check this is a decl with the right slot
                if (pPotentialDecl->GetSlot() != targetSlot)
                    continue;

                // Find out what interface this default implementation is implementing
                mdToken tkParent;
                IfFailThrow(pCurIntfModule->GetMDImport()->GetParentToken(it.GetToken(), &tkParent));

                // We can only load the approximate interface at this point
                MethodTable * pPotentialInterfaceMT = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
                    pCurIntfModule,
                    tkParent,
                    &bmtGenerics->typeContext,
                    ClassLoader::ThrowIfNotFound,
                    ClassLoader::PermitUninstDefOrRef,
                    ClassLoader::LoadTypes,
                    CLASS_LOAD_APPROXPARENTS,
                    TRUE).GetMethodTable()->GetCanonicalMethodTable();

                // Is this a default implementation for the interface we are looking for?
                if (pDeclType->GetMethodTable()->HasSameTypeDefAs(pPotentialInterfaceMT))
                {
                    // If the type is not generic, matching defs are all we need
                    if (!pDeclType->GetMethodTable()->HasInstantiation())
                        return TRUE;

                    // If this is generic, we need to compare under substitutions
                    Substitution curItfSubs(tkParent, pCurIntfModule, &pCurItf->GetSubstitution());

                    // Type Equivalence is not respected for this comparision as you can have multiple type equivalent interfaces on a class
                    TokenPairList newVisited = TokenPairList::AdjustForTypeEquivalenceForbiddenScope(NULL);
                    if (MetaSig::CompareTypeDefsUnderSubstitutions(
                        pPotentialInterfaceMT, pDeclType->GetMethodTable(),
                        &curItfSubs, &pDeclType->GetSubstitution(),
                        &newVisited))
                    {
                        return TRUE;
                    }
                }
            }
        }
    }
#endif // FEATURE_DEFAULT_INTERFACES

    return FALSE;
}

void MethodTableBuilder::VerifyVirtualMethodsImplemented(MethodTable::MethodData * hMTData)
{
    STANDARD_VM_CONTRACT;

    //
    // This verification is not applicable or required in many cases
    //

    if (IsAbstract() || IsInterface())
        return;

#ifdef FEATURE_COMINTEROP
    if (bmtProp->fIsComObjectType)
        return;
#endif // FEATURE_COMINTEROP

    // Since interfaces aren't laid out in the vtable for stub dispatch, what we need to do
    // is try to find an implementation for every interface contract by iterating through
    // the interfaces not declared on a parent.
    BOOL fParentIsAbstract = FALSE;
    if (HasParent())
    {
        fParentIsAbstract = GetParentMethodTable()->IsAbstract();
    }

    // If the parent is abstract, we need to check that each virtual method is implemented
    if (fParentIsAbstract)
    {
        // NOTE: Uses hMTData to avoid caching a MethodData object for the type being built.
        MethodTable::MethodIterator it(hMTData);
        for (; it.IsValid() && it.IsVirtual(); it.Next())
        {
            MethodDesc *pMD = it.GetMethodDesc();
            if (pMD->IsAbstract())
            {
                MethodDesc *pDeclMD = it.GetDeclMethodDesc();
                BuildMethodTableThrowException(IDS_CLASSLOAD_NOTIMPLEMENTED, pDeclMD->GetNameOnNonArrayClass());
            }
        }
    }

    DispatchMapTypeID * rgInterfaceDispatchMapTypeIDs =
        new (GetStackingAllocator()) DispatchMapTypeID[bmtInterface->dwInterfaceMapSize];

    bmtInterfaceInfo::MapIterator intIt = bmtInterface->IterateInterfaceMap();
    for (; !intIt.AtEnd(); intIt.Next())
    {
        if (fParentIsAbstract || !intIt->IsImplementedByParent())
        {
            // Compute all TypeIDs for this interface (all duplicates in the interface map)
            UINT32 cInterfaceDuplicates;
            ComputeDispatchMapTypeIDs(
                intIt->GetInterfaceType()->GetMethodTable(),
                &intIt->GetInterfaceType()->GetSubstitution(),
                rgInterfaceDispatchMapTypeIDs,
                bmtInterface->dwInterfaceMapSize,
                &cInterfaceDuplicates);
            _ASSERTE(cInterfaceDuplicates <= bmtInterface->dwInterfaceMapSize);
            _ASSERTE(cInterfaceDuplicates > 0);

            // NOTE: This override does not cache the resulting MethodData object.
            MethodTable::MethodDataWrapper hData(MethodTable::GetMethodData(
                rgInterfaceDispatchMapTypeIDs,
                cInterfaceDuplicates,
                intIt->GetInterfaceType()->GetMethodTable(),
                GetHalfBakedMethodTable()));
            MethodTable::MethodIterator it(hData);
            for (; it.IsValid() && it.IsVirtual(); it.Next())
            {
                if (it.GetTarget().IsNull())
                {
                    MethodDesc *pMD = it.GetDeclMethodDesc();

                    if (!HasDefaultInterfaceImplementation(intIt->GetInterfaceType(), pMD))
                        BuildMethodTableThrowException(IDS_CLASSLOAD_NOTIMPLEMENTED, pMD->GetNameOnNonArrayClass());
                }
            }
        }
    }
}

INT32 __stdcall IsDefined(Module *pModule, mdToken token, TypeHandle attributeClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    BOOL isDefined = FALSE;

    IMDInternalImport *pInternalImport = pModule->GetMDImport();
    BOOL isSealed = FALSE;

    HENUMInternalHolder hEnum(pInternalImport);
    TypeHandle caTH;

    // Get the enum first but don't get any values
    hEnum.EnumInit(mdtCustomAttribute, token);

    ULONG cMax = pInternalImport->EnumGetCount(&hEnum);
    if (cMax)
    {
        // we have something to look at


        if (!attributeClass.IsNull())
            isSealed = attributeClass.GetMethodTable()->IsSealed();

        // Loop through the Attributes and look for the requested one
        mdCustomAttribute cv;
        while (pInternalImport->EnumNext(&hEnum, &cv))
        {
            //
            // fetch the ctor
            mdToken     tkCtor;
            IfFailThrow(pInternalImport->GetCustomAttributeProps(cv, &tkCtor));

            mdToken tkType = TypeFromToken(tkCtor);
            if(tkType != mdtMemberRef && tkType != mdtMethodDef)
                continue; // we only deal with the ctor case

            //
            // get the info to load the type, so we can check whether the current
            // attribute is a subtype of the requested attribute
            IfFailThrow(pInternalImport->GetParentToken(tkCtor, &tkType));

            _ASSERTE(TypeFromToken(tkType) == mdtTypeRef || TypeFromToken(tkType) == mdtTypeDef);
            // load the type
            if (isSealed)
            {
                caTH=ClassLoader::LoadTypeDefOrRefThrowing(pModule, tkType,
                                                           ClassLoader::ReturnNullIfNotFound,
                                                           ClassLoader::FailIfUninstDefOrRef,
                                                           TypeFromToken(tkType) == mdtTypeDef ? tdAllTypes : tdNoTypes);
            }
            else
            {
                caTH = ClassLoader::LoadTypeDefOrRefThrowing(pModule, tkType,
                                                             ClassLoader::ReturnNullIfNotFound,
                                                             ClassLoader::FailIfUninstDefOrRef);
            }
            if (caTH.IsNull())
                continue;

            // a null class implies all custom attribute
            if (!attributeClass.IsNull())
            {
                if (isSealed)
                {
                    if (attributeClass != caTH)
                        continue;
                }
                else
                {
                    if (!caTH.CanCastTo(attributeClass))
                        continue;
                }
            }

            //
            // if we are here we got one
            isDefined = TRUE;
            break;
        }
    }

    return isDefined;
}

//*******************************************************************************
VOID MethodTableBuilder::CheckForRemotingProxyAttrib()
{
    STANDARD_VM_CONTRACT;

}


//*******************************************************************************
// Checks for a bunch of special interface names and if it matches then it sets
// bmtProp->fIsMngStandardItf to TRUE. Additionally, it checks to see if the
// type is an interface and if it has ComEventInterfaceAttribute custom attribute
// set, then it sets bmtProp->fComEventItfType to true.
//
// NOTE: This only does anything when COM interop is enabled.

VOID MethodTableBuilder::CheckForSpecialTypes()
{
#ifdef FEATURE_COMINTEROP
    STANDARD_VM_CONTRACT;


    Module *pModule = GetModule();
    IMDInternalImport *pMDImport = pModule->GetMDImport();

    // Check to see if this type is a managed standard interface. All the managed
    // standard interfaces live in CoreLib so checking for that first
    // makes the strcmp that comes afterwards acceptable.
    if (pModule->IsSystem())
    {
        if (IsInterface())
        {
            LPCUTF8 pszClassName;
            LPCUTF8 pszClassNamespace;
            if (FAILED(pMDImport->GetNameOfTypeDef(GetCl(), &pszClassName, &pszClassNamespace)))
            {
                pszClassName = pszClassNamespace = NULL;
            }
            if ((pszClassName != NULL) && (pszClassNamespace != NULL))
            {
                LPUTF8 pszFullyQualifiedName = NULL;
                MAKE_FULLY_QUALIFIED_NAME(pszFullyQualifiedName, pszClassNamespace, pszClassName);

                // This is just to give us a scope to break out of.
                do
                {

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
                    if (strcmp(strMngItfName, pszFullyQualifiedName) == 0) \
                    { \
                        bmtProp->fIsMngStandardItf = true; \
                        break; \
                    }

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, ECallMethName, MethName, MethSig, FcallDecl)

#define MNGSTDITF_END_INTERFACE(FriendlyName)

#include "mngstditflist.h"

#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE

                } while (FALSE);
            }
        }
    }

    // Check to see if the type is a COM event interface (classic COM interop only).
    if (IsInterface())
    {
        HRESULT hr = GetCustomAttribute(GetCl(), WellKnownAttribute::ComEventInterface, NULL, NULL);
        if (hr == S_OK)
        {
            bmtProp->fComEventItfType = true;
        }
    }
#endif // FEATURE_COMINTEROP
}

#ifdef FEATURE_READYTORUN

bool ModulesAreDistributedAsAnIndivisibleUnit(Module* module1, Module* module2)
{
    if (module1 == module2)
        return true;

    bool nativeImagesIdentical = false;
    if (module1->GetCompositeNativeImage() != NULL)
    {
        return module1->GetCompositeNativeImage() == module2->GetCompositeNativeImage();
    }

    return false;
}

//*******************************************************************************
VOID MethodTableBuilder::CheckLayoutDependsOnOtherModules(MethodTable * pDependencyMT)
{
    STANDARD_VM_CONTRACT;

    // These cases are expected to be handled by the caller
    _ASSERTE(!(pDependencyMT == g_pObjectClass || pDependencyMT->IsTruePrimitive() || ((g_pEnumClass != NULL) && pDependencyMT->IsEnum())));

    //
    // WARNING: Changes in this algorithm are potential ReadyToRun breaking changes !!!
    //
    // Track whether field layout of this type depend on information outside its containing module and compilation unit
    //
    // It is a stronger condition than MethodTable::IsInheritanceChainLayoutFixedInCurrentVersionBubble().
    // It has to remain fixed across versioning changes in the module dependencies. In particular, it does
    // not take into account NonVersionable attribute. Otherwise, adding NonVersionable attribute to existing
    // type would be ReadyToRun incompatible change.
    //
    bool modulesDefinedInSameDistributionUnit = ModulesAreDistributedAsAnIndivisibleUnit(pDependencyMT->GetModule(), GetModule());
    bool dependsOnOtherModules = !modulesDefinedInSameDistributionUnit || pDependencyMT->GetClass()->HasLayoutDependsOnOtherModules();

    if (dependsOnOtherModules)
        GetHalfBakedClass()->SetHasLayoutDependsOnOtherModules();
}

BOOL MethodTableBuilder::NeedsAlignedBaseOffset()
{
    STANDARD_VM_CONTRACT;

    //
    // WARNING: Changes in this algorithm are potential ReadyToRun breaking changes !!!
    //
    // This method returns whether the type needs aligned base offset in order to have layout resilient to
    // base class layout changes.
    //
    if (IsValueClass())
        return FALSE;

    MethodTable * pParentMT = GetParentMethodTable();

    // Trivial parents
    if (pParentMT == NULL || pParentMT == g_pObjectClass)
        return FALSE;

    // Always use the ReadyToRun field layout algorithm if the source IL image was ReadyToRun, independent on
    // whether ReadyToRun is actually enabled for the module. It is required to allow mixing and matching
    // ReadyToRun images with and without input bubble enabled.
    if (!GetModule()->GetPEAssembly()->IsReadyToRun())
    {
        // Always use ReadyToRun field layout algorithm to produce ReadyToRun images
        return FALSE;
    }

    if (!ModulesAreDistributedAsAnIndivisibleUnit(GetModule(), pParentMT->GetModule()) ||
        pParentMT->GetClass()->HasLayoutDependsOnOtherModules())
    {
        return TRUE;
    }

    return FALSE;
}
#endif // FEATURE_READYTORUN

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Set the HasFinalizer and HasCriticalFinalizer flags
//
VOID MethodTableBuilder::SetFinalizationSemantics()
{
    STANDARD_VM_CONTRACT;

    if (g_pObjectFinalizerMD && !IsInterface() && !IsValueClass())
    {
        WORD slot = g_pObjectFinalizerMD->GetSlot();

        // Objects not derived from Object will get marked as having a finalizer, if they have
        // sufficient virtual methods.  This will only be an issue if they can be allocated
        // in the GC heap (which will cause all sorts of other problems).
        if (slot < bmtVT->cVirtualSlots && (*bmtVT)[slot].Impl().GetMethodDesc() != g_pObjectFinalizerMD)
        {
            GetHalfBakedMethodTable()->SetHasFinalizer();

            // The need for a critical finalizer can be inherited from a parent.
            // Since we set this automatically for CriticalFinalizerObject
            // elsewhere, the code below is the means by which any derived class
            // picks up the attribute.
            if (HasParent() && GetParentMethodTable()->HasCriticalFinalizer())
            {
                GetHalfBakedMethodTable()->SetHasCriticalFinalizer();
            }
        }
    }
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Perform relevant GC calculations for value classes
//
VOID MethodTableBuilder::HandleGCForValueClasses(MethodTable ** pByValueClassCache)
{
    STANDARD_VM_CONTRACT;

    DWORD i;

    EEClass *pClass = GetHalfBakedClass();
    MethodTable *pMT = GetHalfBakedMethodTable();

    FieldDesc *pFieldDescList = pClass->GetFieldDescList();

    // Note that for value classes, the following calculation is only appropriate
    // when the instance is in its "boxed" state.
    if (bmtFP->NumGCPointerSeries != 0)
    {
        CGCDescSeries *pSeries;
        CGCDescSeries *pHighest;

        pMT->SetContainsPointers();

        // Copy the pointer series map from the parent
        CGCDesc::Init( (PVOID) pMT, bmtFP->NumGCPointerSeries );
        if (bmtParent->NumParentPointerSeries != 0)
        {
            size_t ParentGCSize = CGCDesc::ComputeSize(bmtParent->NumParentPointerSeries);
            memcpy( (PVOID) (((BYTE*) pMT) - ParentGCSize),
                    (PVOID) (((BYTE*) GetParentMethodTable()) - ParentGCSize),
                    ParentGCSize - sizeof(size_t)   // sizeof(size_t) is the NumSeries count
                  );

        }

        // Build the pointer series map for this pointers in this instance
        pSeries = ((CGCDesc*)pMT)->GetLowestSeries();
        if (bmtFP->NumInstanceGCPointerFields)
        {
            // See gcdesc.h for an explanation of why we adjust by subtracting BaseSize
            pSeries->SetSeriesSize( (size_t) (bmtFP->NumInstanceGCPointerFields * TARGET_POINTER_SIZE) - (size_t) pMT->GetBaseSize());
            pSeries->SetSeriesOffset(bmtFP->GCPointerFieldStart + OBJECT_SIZE);
            pSeries++;
        }

        // Insert GC info for fields which are by-value classes
        for (i = 0; i < bmtEnumFields->dwNumInstanceFields; i++)
        {
            if (pFieldDescList[i].IsByValue())
            {
                MethodTable *pByValueMT = pByValueClassCache[i];

                if (pByValueMT->ContainsPointers())
                {
                    // Offset of the by value class in the class we are building, does NOT include Object
                    DWORD       dwCurrentOffset = pFieldDescList[i].GetOffset_NoLogging();

                    // The by value class may have more than one pointer series
                    CGCDescSeries * pByValueSeries = CGCDesc::GetCGCDescFromMT(pByValueMT)->GetLowestSeries();
                    SIZE_T dwNumByValueSeries = CGCDesc::GetCGCDescFromMT(pByValueMT)->GetNumSeries();

                    for (SIZE_T j = 0; j < dwNumByValueSeries; j++)
                    {
                        size_t cbSeriesSize;
                        size_t cbSeriesOffset;

                        _ASSERTE(pSeries <= CGCDesc::GetCGCDescFromMT(pMT)->GetHighestSeries());

                        cbSeriesSize = pByValueSeries->GetSeriesSize();

                        // Add back the base size of the by value class, since it's being transplanted to this class
                        cbSeriesSize += pByValueMT->GetBaseSize();

                        // Subtract the base size of the class we're building
                        cbSeriesSize -= pMT->GetBaseSize();

                        // Set current series we're building
                        pSeries->SetSeriesSize(cbSeriesSize);

                        // Get offset into the value class of the first pointer field (includes a +Object)
                        cbSeriesOffset = pByValueSeries->GetSeriesOffset();

                        // Add it to the offset of the by value class in our class
                        cbSeriesOffset += dwCurrentOffset;

                        pSeries->SetSeriesOffset(cbSeriesOffset); // Offset of field
                        pSeries++;
                        pByValueSeries++;
                    }
                }
            }
        }

        // Adjust the inherited series - since the base size has increased by "# new field instance bytes", we need to
        // subtract that from all the series (since the series always has BaseSize subtracted for it - see gcdesc.h)
        pHighest = CGCDesc::GetCGCDescFromMT(pMT)->GetHighestSeries();
        while (pSeries <= pHighest)
        {
            CONSISTENCY_CHECK(CheckPointer(GetParentMethodTable()));
            pSeries->SetSeriesSize( pSeries->GetSeriesSize() - ((size_t) pMT->GetBaseSize() - (size_t) GetParentMethodTable()->GetBaseSize()) );
            pSeries++;
        }

        _ASSERTE(pSeries-1 <= CGCDesc::GetCGCDescFromMT(pMT)->GetHighestSeries());
    }

}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Check for the presence of type equivalence. If present, make sure
// it is permitted to be on this type.
//

void MethodTableBuilder::CheckForTypeEquivalence(
    WORD                     cBuildingInterfaceList,
    BuildingInterfaceInfo_t *pBuildingInterfaceList)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_TYPEEQUIVALENCE
    bmtProp->fIsTypeEquivalent = !!IsTypeDefEquivalent(GetCl(), GetModule());

    if (bmtProp->fIsTypeEquivalent)
    {
        BOOL comImportOrEventInterface = IsComImport();
#ifdef FEATURE_COMINTEROP
        comImportOrEventInterface = comImportOrEventInterface || bmtProp->fComEventItfType;
#endif // FEATURE_COMINTEROP

        BOOL fTypeEquivalentNotPermittedDueToType = !((comImportOrEventInterface && IsInterface()) || IsValueClass() || IsDelegate());
        BOOL fTypeEquivalentNotPermittedDueToGenerics = bmtGenerics->HasInstantiation();

        if (fTypeEquivalentNotPermittedDueToType || fTypeEquivalentNotPermittedDueToGenerics)
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_EQUIVALENTBADTYPE);
        }

        GetHalfBakedClass()->SetIsEquivalentType();
    }

    bmtProp->fHasTypeEquivalence = bmtProp->fIsTypeEquivalent;

    if (!bmtProp->fHasTypeEquivalence)
    {
        // fHasTypeEquivalence flag is inherited from interfaces so we can quickly detect
        // types that implement type equivalent interfaces
        for (WORD i = 0; i < cBuildingInterfaceList; i++)
        {
            MethodTable *pItfMT = pBuildingInterfaceList[i].m_pMethodTable;
            if (pItfMT->HasTypeEquivalence())
            {
                bmtProp->fHasTypeEquivalence = true;
                break;
            }
        }
    }

    if (!bmtProp->fHasTypeEquivalence)
    {
        // fHasTypeEquivalence flag is "inherited" from generic arguments so we can quickly detect
        // types like List<Str> where Str is a structure with the TypeIdentifierAttribute.
        if (bmtGenerics->HasInstantiation() && !bmtGenerics->IsTypicalTypeDefinition())
        {
            Instantiation inst = bmtGenerics->GetInstantiation();
            for (DWORD i = 0; i < inst.GetNumArgs(); i++)
            {
                if (inst[i].HasTypeEquivalence())
                {
                    bmtProp->fHasTypeEquivalence = true;
                    break;
                }
            }
        }
    }
#endif //FEATURE_TYPEEQUIVALENCE
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Before we make the final leap, make sure we've allocated all memory needed to
// fill out the RID maps.
//
VOID MethodTableBuilder::EnsureRIDMapsCanBeFilled()
{
    STANDARD_VM_CONTRACT;


    DWORD i;


    // Rather than call Ensure***CanBeStored() hundreds of times, we
    // will call it once on the largest token we find. This relies
    // on an invariant that RidMaps don't use some kind of sparse
    // allocation.

    {
        mdMethodDef largest = mdMethodDefNil;

        DeclaredMethodIterator it(*this);
        while (it.Next())
        {
            if (it.Token() > largest)
            {
                largest = it.Token();
            }
        }
        if ( largest != mdMethodDefNil )
        {
            GetModule()->EnsureMethodDefCanBeStored(largest);
        }
    }

    {
        mdFieldDef largest = mdFieldDefNil;

        for (i = 0; i < bmtMetaData->cFields; i++)
        {
            if (bmtMetaData->pFields[i] > largest)
            {
                largest = bmtMetaData->pFields[i];
            }
        }
        if ( largest != mdFieldDefNil )
        {
            GetModule()->EnsureFieldDefCanBeStored(largest);
        }
    }
}

#ifdef FEATURE_COMINTEROP
//*******************************************************************************
void MethodTableBuilder::GetCoClassAttribInfo()
{
    STANDARD_VM_CONTRACT;
    // Retrieve the CoClassAttribute CA.
    HRESULT hr = GetCustomAttribute(GetCl(), WellKnownAttribute::CoClass, NULL, NULL);
    if (hr == S_OK)
    {
        // COM class interfaces may lazily populate the m_pCoClassForIntf field of EEClass. This field is
        // optional so we must ensure the optional field descriptor has been allocated.
        EnsureOptionalFieldsAreAllocated(GetHalfBakedClass(), m_pAllocMemTracker, GetLoaderAllocator()->GetLowFrequencyHeap());
        SetIsComClassInterface();
    }
}
#endif // FEATURE_COMINTEROP

//*******************************************************************************
void MethodTableBuilder::bmtMethodImplInfo::AddMethodImpl(
    bmtMDMethod * pImplMethod, bmtMethodHandle declMethod, mdToken declToken,
    StackingAllocator * pStackingAllocator)
{
    STANDARD_VM_CONTRACT;

    CONSISTENCY_CHECK(CheckPointer(pImplMethod));
    CONSISTENCY_CHECK(!declMethod.IsNull());
    if (pIndex >= cMaxIndex)
    {
        DWORD newEntriesCount = 0;

        if (!ClrSafeInt<DWORD>::multiply(cMaxIndex, 2, newEntriesCount))
            ThrowHR(COR_E_OVERFLOW);

        if (newEntriesCount == 0)
            newEntriesCount = 10;

        // If we have to grow this array, we will not free the old array before we clean up the BuildMethodTable operation
        // because this is a stacking allocator. However, the old array will get freed when all the stack allocator is freed.
        Entry *rgEntriesNew = new (pStackingAllocator) Entry[newEntriesCount];
        memcpy(rgEntriesNew, rgEntries, sizeof(Entry) * cMaxIndex);

        // Start using newly allocated array.
        rgEntries = rgEntriesNew;
        cMaxIndex = newEntriesCount;
    }
    rgEntries[pIndex++] = Entry(pImplMethod, declMethod, declToken);
}

//*******************************************************************************
// Returns TRUE if tok acts as a body for any methodImpl entry. FALSE, otherwise.
BOOL MethodTableBuilder::bmtMethodImplInfo::IsBody(mdToken tok)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(TypeFromToken(tok) == mdtMethodDef);
    for (DWORD i = 0; i < pIndex; i++)
    {
        if (GetBodyMethodDesc(i)->GetMemberDef() == tok)
        {
            return TRUE;
        }
    }
    return FALSE;
}

//*******************************************************************************
BYTE *
MethodTableBuilder::AllocateFromHighFrequencyHeap(S_SIZE_T cbMem)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    return (BYTE *)GetMemTracker()->Track(
        GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(cbMem));
}

//*******************************************************************************
BYTE *
MethodTableBuilder::AllocateFromLowFrequencyHeap(S_SIZE_T cbMem)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    return (BYTE *)GetMemTracker()->Track(
        GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(cbMem));
}

//-------------------------------------------------------------------------------
// Make best-case effort to obtain an image name for use in an error message.
//
// This routine must expect to be called before the this object is fully loaded.
// It can return an empty if the name isn't available or the object isn't initialized
// enough to get a name, but it mustn't crash.
//-------------------------------------------------------------------------------
LPCWSTR MethodTableBuilder::GetPathForErrorMessages()
{
    STANDARD_VM_CONTRACT;

    return GetModule()->GetPathForErrorMessages();
}

BOOL MethodTableBuilder::ChangesImplementationOfVirtualSlot(SLOT_INDEX idx)
{
    STANDARD_VM_CONTRACT;

    BOOL fChangesImplementation = TRUE;

    _ASSERTE(idx < bmtVT->cVirtualSlots);

    if (HasParent() && idx < GetParentMethodTable()->GetNumVirtuals())
    {
        _ASSERTE(idx < bmtParent->pSlotTable->GetSlotCount());
        bmtMethodHandle VTImpl = (*bmtVT)[idx].Impl();
        bmtMethodHandle ParentImpl = (*bmtParent)[idx].Impl();

        fChangesImplementation = VTImpl != ParentImpl;

        // See code:MethodTableBuilder::SetupMethodTable2 and its logic
        // for handling MethodImpl's on parent classes which affect non interface
        // methods.
        if (!fChangesImplementation && (ParentImpl.GetSlotIndex() != idx))
            fChangesImplementation = TRUE;

        // If the current vtable slot is MethodImpl, is it possible that it will be updated by
        // the ClassLoader::PropagateCovariantReturnMethodImplSlots.
        if (!fChangesImplementation && VTImpl.GetMethodDesc()->IsMethodImpl())
        {
            // Note: to know exactly whether the slot will be updated or not, we would need to check the
            // PreserveBaseOverridesAttribute presence on the current vtable slot and in the worst case
            // on all of its ancestors. This is expensive, so we don't do that check here and accept
            // the fact that we get some false positives and end up sharing less vtable chunks.

            // Search the previous slots in the parent vtable for the same implementation. If it exists and it was
            // overriden, the ClassLoader::PropagateCovariantReturnMethodImplSlots will propagate the change to the current
            // slot (idx), so the implementation of it will change.
            MethodDesc* pParentMD = ParentImpl.GetMethodDesc();
            for (SLOT_INDEX i = 0; i < idx; i++)
            {
                if ((*bmtParent)[i].Impl().GetMethodDesc() == pParentMD && (*bmtVT)[i].Impl().GetMethodDesc() != pParentMD)
                {
                    fChangesImplementation = TRUE;
                    break;
                }
            }
        }
    }

    return fChangesImplementation;
}

// Must be called prior to setting the value of any optional field on EEClass (on a debug build an assert will
// fire if this invariant is violated).
void MethodTableBuilder::EnsureOptionalFieldsAreAllocated(EEClass *pClass, AllocMemTracker *pamTracker, LoaderHeap *pHeap)
{
    STANDARD_VM_CONTRACT;

    if (pClass->HasOptionalFields())
        return;

    EEClassOptionalFields *pOptFields = (EEClassOptionalFields*)
        pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(EEClassOptionalFields))));

    // Initialize default values for all optional fields.
    pOptFields->Init();

    // Attach optional fields to the class.
    pClass->AttachOptionalFields(pOptFields);
}

//---------------------------------------------------------------------------------------
//
// Gather information about a generic type
// - number of parameters
// - variance annotations
// - dictionaries
// - sharability
//
//static
void
MethodTableBuilder::GatherGenericsInfo(
    Module *          pModule,
    mdTypeDef         cl,
    Instantiation     inst,
    bmtGenericsInfo * bmtGenericsInfo,
    StackingAllocator*pStackingAllocator)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(bmtGenericsInfo));
    }
    CONTRACTL_END;

    IMDInternalImport * pInternalImport = pModule->GetMDImport();

    // Enumerate the formal type parameters
    HENUMInternal   hEnumGenericPars;
    HRESULT hr = pInternalImport->EnumInit(mdtGenericParam, cl, &hEnumGenericPars);
    if (FAILED(hr))
        pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);

    DWORD numGenericArgs = pInternalImport->EnumGetCount(&hEnumGenericPars);

    // Work out what kind of EEClass we're creating w.r.t. generics.  If there
    // are no generics involved this will be a VMFLAG_NONGENERIC.
    BOOL fHasVariance = FALSE;
    if (numGenericArgs > 0)
    {
        // Generic type verification
        {
            DWORD   dwAttr;
            mdToken tkParent;
            if (FAILED(pInternalImport->GetTypeDefProps(cl, &dwAttr, &tkParent)))
            {
                pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
            }
            // A generic with explicit layout is not allowed.
            if (IsTdExplicitLayout(dwAttr))
            {
                pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_EXPLICIT_GENERIC);
            }
        }

        bmtGenericsInfo->numDicts = 1;

        mdGenericParam tkTyPar;
        bmtGenericsInfo->pVarianceInfo = new (pStackingAllocator) BYTE[numGenericArgs];

        // If it has generic arguments but none have been specified, then load the instantiation at the formals
        if (inst.IsEmpty())
        {
            bmtGenericsInfo->fTypicalInstantiation = TRUE;
            S_UINT32 scbAllocSize = S_UINT32(numGenericArgs) * S_UINT32(sizeof(TypeHandle));
            TypeHandle * genericArgs = (TypeHandle *) pStackingAllocator->Alloc(scbAllocSize);

            inst = Instantiation(genericArgs, numGenericArgs);

            bmtGenericsInfo->fSharedByGenericInstantiations = FALSE;
        }
        else
        {
            bmtGenericsInfo->fTypicalInstantiation = FALSE;

            bmtGenericsInfo->fSharedByGenericInstantiations = TypeHandle::IsCanonicalSubtypeInstantiation(inst);
            _ASSERTE(bmtGenericsInfo->fSharedByGenericInstantiations == ClassLoader::IsSharableInstantiation(inst));

#ifdef _DEBUG
            // Set typical instantiation MethodTable
            {
                MethodTable * pTypicalInstantiationMT = pModule->LookupTypeDef(cl).AsMethodTable();
                // Typical instantiation was already loaded by code:ClassLoader::LoadApproxTypeThrowing
                _ASSERTE(pTypicalInstantiationMT != NULL);
                bmtGenericsInfo->dbg_pTypicalInstantiationMT = pTypicalInstantiationMT;
            }
#endif //_DEBUG
        }

        TypeHandle * pDestInst = (TypeHandle *)inst.GetRawArgs();
        {
            // Protect multi-threaded access to Module.m_GenericParamToDescMap. Other threads may be loading the same type
            // to break type recursion dead-locks
            CrstHolder ch(&pModule->GetClassLoader()->m_AvailableTypesLock);

            for (unsigned int i = 0; i < numGenericArgs; i++)
            {
                pInternalImport->EnumNext(&hEnumGenericPars, &tkTyPar);
                DWORD flags;
                if (FAILED(pInternalImport->GetGenericParamProps(tkTyPar, NULL, &flags, NULL, NULL, NULL)))
                {
                    pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
                }

                if (bmtGenericsInfo->fTypicalInstantiation)
                {
                    // code:Module.m_GenericParamToDescMap maps generic parameter RIDs to TypeVarTypeDesc
                    // instances so that we do not leak by allocating them all over again, if the type
                    // repeatedly fails to load.
                    TypeVarTypeDesc* pTypeVarTypeDesc = pModule->LookupGenericParam(tkTyPar);
                    if (pTypeVarTypeDesc == NULL)
                    {
                        // Do NOT use the alloc tracker for this memory as we need it stay allocated even if the load fails.
                        void* mem = (void*)pModule->GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(TypeVarTypeDesc)));
                        pTypeVarTypeDesc = new (mem) TypeVarTypeDesc(pModule, cl, i, tkTyPar);

                        pModule->StoreGenericParamThrowing(tkTyPar, pTypeVarTypeDesc);
                    }
                    pDestInst[i] = TypeHandle(pTypeVarTypeDesc);
                }

                DWORD varianceAnnotation = flags & gpVarianceMask;
                bmtGenericsInfo->pVarianceInfo[i] = static_cast<BYTE>(varianceAnnotation);
                if (varianceAnnotation != gpNonVariant)
                {
                    if (varianceAnnotation != gpContravariant && varianceAnnotation != gpCovariant)
                    {
                        pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADVARIANCE);
                    }
                    else
                    {
                        fHasVariance = TRUE;
                    }
                }
            }
        }

        if (!fHasVariance)
            bmtGenericsInfo->pVarianceInfo = NULL;
    }
    else
    {
        bmtGenericsInfo->fTypicalInstantiation = FALSE;
        bmtGenericsInfo->fSharedByGenericInstantiations = FALSE;
        bmtGenericsInfo->numDicts = 0;
    }

    bmtGenericsInfo->fContainsGenericVariables = MethodTable::ComputeContainsGenericVariables(inst);

    SigTypeContext typeContext(inst, Instantiation());
    bmtGenericsInfo->typeContext = typeContext;
} // MethodTableBuilder::GatherGenericsInfo

//=======================================================================
// This is invoked from the class loader while building the internal structures for a type
// This function should check if explicit layout metadata exists.
//
// Returns:
//  TRUE    - yes, there's layout metadata
//  FALSE   - no, there's no layout.
//  fail    - throws a typeload exception
//
// If TRUE,
//   *pNLType            gets set to nltAnsi or nltUnicode
//   *pPackingSize       declared packing size
//   *pfExplicitoffsets  offsets explicit in metadata or computed?
//=======================================================================
BOOL HasLayoutMetadata(Assembly* pAssembly, IMDInternalImport* pInternalImport, mdTypeDef cl, MethodTable* pParentMT, BYTE* pPackingSize, BYTE* pNLTType, BOOL* pfExplicitOffsets)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pInternalImport));
        PRECONDITION(CheckPointer(pPackingSize));
        PRECONDITION(CheckPointer(pNLTType));
        PRECONDITION(CheckPointer(pfExplicitOffsets));
    }
    CONTRACTL_END;

    HRESULT hr;
    ULONG clFlags;
    if (FAILED(pInternalImport->GetTypeDefProps(cl, &clFlags, NULL)))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    if (IsTdAutoLayout(clFlags))
    {
        return FALSE;
    }
    else if (IsTdSequentialLayout(clFlags))
    {
        *pfExplicitOffsets = FALSE;
    }
    else if (IsTdExplicitLayout(clFlags))
    {
        *pfExplicitOffsets = TRUE;
    }
    else
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    // We now know this class has seq. or explicit layout. Ensure the parent does too.
    if (pParentMT && !(pParentMT->IsObjectClass() || pParentMT->IsValueTypeClass()) && !(pParentMT->HasLayout()))
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);

    if (IsTdAnsiClass(clFlags))
    {
        *pNLTType = nltAnsi;
    }
    else if (IsTdUnicodeClass(clFlags))
    {
        *pNLTType = nltUnicode;
    }
    else if (IsTdAutoClass(clFlags))
    {
#ifdef TARGET_WINDOWS
        *pNLTType = nltUnicode;
#else
        *pNLTType = nltAnsi; // We don't have a utf8 charset in metadata yet, but ANSI == UTF-8 off-Windows
#endif
    }
    else
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    DWORD dwPackSize;
    hr = pInternalImport->GetClassPackSize(cl, &dwPackSize);
    if (FAILED(hr) || dwPackSize == 0)
        dwPackSize = DEFAULT_PACKING_SIZE;

    // This has to be reduced to a BYTE value, so we had better make sure it fits. If
    // not, we'll throw an exception instead of trying to munge the value to what we
    // think the user might want.
    if (!FitsInU1((UINT64)(dwPackSize)))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    *pPackingSize = (BYTE)dwPackSize;

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// This service is called for normal classes -- and for the pseudo class we invent to
// hold the module's public members.
//
//static
TypeHandle
ClassLoader::CreateTypeHandleForTypeDefThrowing(
    Module *          pModule,
    mdTypeDef         cl,
    Instantiation     inst,
    AllocMemTracker * pamTracker)
{
    CONTRACT(TypeHandle)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(!RETVAL.IsNull());
        POSTCONDITION(CheckPointer(RETVAL.GetMethodTable()));
    }
    CONTRACT_END;

    MethodTable * pMT = NULL;

    MethodTable * pParentMethodTable = NULL;
    SigPointer    parentInst;
    mdTypeDef     tdEnclosing = mdTypeDefNil;
    DWORD         cInterfaces;
    BuildingInterfaceInfo_t * pInterfaceBuildInfo = NULL;
    IMDInternalImport *       pInternalImport = NULL;
    LayoutRawFieldInfo *      pLayoutRawFieldInfos = NULL;
    MethodTableBuilder::bmtGenericsInfo genericsInfo;

    Assembly * pAssembly = pModule->GetAssembly();
    pInternalImport = pModule->GetMDImport();

    if (TypeFromToken(cl) != mdtTypeDef || !pInternalImport->IsValidToken(cl))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    // GetCheckpoint for the thread-based allocator
    // This checkpoint provides a scope for all transient allocations of data structures
    // used during class loading.
    // <NICE> Ideally a debug/checked build should pass around tokens indicating the Checkpoint
    // being used and check these dynamically </NICE>
    ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

    // Gather up generics info
    MethodTableBuilder::GatherGenericsInfo(pModule, cl, inst, &genericsInfo, pStackingAllocator);

    Module * pLoaderModule = pModule;
    if (!inst.IsEmpty())
    {
        pLoaderModule = ClassLoader::ComputeLoaderModuleWorker(
            pModule,
            cl,
            inst,
            Instantiation());
        pLoaderModule->GetLoaderAllocator()->EnsureInstantiation(pModule, inst);
    }

    LoaderAllocator * pAllocator = pLoaderModule->GetLoaderAllocator();

    {
        // As this is loading a parent type, we are allowed to override the load type limit.
        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOAD_APPROXPARENTS);
        pParentMethodTable = LoadApproxParentThrowing(pModule, cl, &parentInst, &genericsInfo.typeContext);
    }

    if (pParentMethodTable != NULL)
    {
        // Since methods on System.Array assume the layout of arrays, we can not allow
        // subclassing of arrays, it is sealed from the users point of view.
        // Value types and enums should be sealed - disable inheritting from them (we cannot require sealed
        // flag because of AppCompat)
        if (pParentMethodTable->IsSealed() ||
            (pParentMethodTable == g_pArrayClass) ||
            pParentMethodTable->IsValueType())
        {
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_SEALEDPARENT);
        }

        DWORD dwTotalDicts = genericsInfo.numDicts + pParentMethodTable->GetNumDicts();
        if (!FitsIn<WORD>(dwTotalDicts))
        {
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_TOOMANYGENERICARGS);
        }
        genericsInfo.numDicts = static_cast<WORD>(dwTotalDicts);
    }

    GetEnclosingClassThrowing(pInternalImport, pModule, cl, &tdEnclosing);

    BYTE nstructPackingSize = 0, nstructNLT = 0;
    BOOL fExplicitOffsets = FALSE;
    // NOTE: HasLayoutMetadata does not load classes
    BOOL fHasLayout =
        !genericsInfo.fContainsGenericVariables &&
        HasLayoutMetadata(
            pModule->GetAssembly(),
            pInternalImport,
            cl,
            pParentMethodTable,
            &nstructPackingSize,
            &nstructNLT,
            &fExplicitOffsets);

    BOOL fIsEnum = ((g_pEnumClass != NULL) && (pParentMethodTable == g_pEnumClass));

    // enums may not have layout because they derive from g_pEnumClass and that has no layout
    // this is enforced by HasLayoutMetadata above
    _ASSERTE(!(fIsEnum && fHasLayout));

    // This is a delegate class if it derives from MulticastDelegate (we do not allow single cast delegates)
    BOOL fIsDelegate = pParentMethodTable && pParentMethodTable == g_pMulticastDelegateClass;

    // Create a EEClass entry for it, filling out a few fields, such as the parent class token
    // (and the generic type should we be creating an instantiation)
    EEClass * pClass = MethodTableBuilder::CreateClass(
        pModule,
        cl,
        fHasLayout,
        fIsDelegate,
        fIsEnum,
        &genericsInfo,
        pAllocator,
        pamTracker);

    if ((pParentMethodTable != NULL) && (pParentMethodTable == g_pDelegateClass))
    {
        // Note we do not allow single cast delegates
        if (pModule->GetAssembly() != SystemDomain::SystemAssembly())
        {
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, BFA_CANNOT_INHERIT_FROM_DELEGATE);
        }

#ifdef _DEBUG
        // Only MultiCastDelegate should inherit from Delegate
        LPCUTF8 className;
        LPCUTF8 nameSpace;
        if (FAILED(pInternalImport->GetNameOfTypeDef(cl, &className, &nameSpace)))
        {
            className = nameSpace = "Invalid TypeDef record";
        }
        BAD_FORMAT_NOTHROW_ASSERT(strcmp(className, "MulticastDelegate") == 0);
#endif
    }

    if (fIsDelegate)
    {
        if (!pClass->IsSealed())
        {
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, BFA_DELEGATE_CLASS_NOTSEALED);
        }

        pClass->SetIsDelegate();
    }

    if (tdEnclosing != mdTypeDefNil)
    {
        pClass->SetIsNested();
        THROW_BAD_FORMAT_MAYBE(IsTdNested(pClass->GetProtection()), VLDTR_E_TD_ENCLNOTNESTED, pModule);
    }
    else if (IsTdNested(pClass->GetProtection()))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    // We only permit generic interfaces and delegates to have variant type parameters
    if (genericsInfo.pVarianceInfo != NULL && !pClass->IsInterface() && !fIsDelegate)
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_VARIANCE_CLASS);
    }

    // Now load all the interfaces
    HENUMInternalHolder hEnumInterfaceImpl(pInternalImport);
    hEnumInterfaceImpl.EnumInit(mdtInterfaceImpl, cl);

    cInterfaces = pInternalImport->EnumGetCount(&hEnumInterfaceImpl);

    if (cInterfaces != 0)
    {
        DWORD i;

        // Allocate the BuildingInterfaceList table
        pInterfaceBuildInfo = new (pStackingAllocator) BuildingInterfaceInfo_t[cInterfaces];

        mdInterfaceImpl ii;
        for (i = 0; pInternalImport->EnumNext(&hEnumInterfaceImpl, &ii); i++)
        {
            // Get properties on this interface
            mdTypeRef crInterface;
            if (FAILED(pInternalImport->GetTypeOfInterfaceImpl(ii, &crInterface)))
            {
                pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
            }
            // validate the token
            mdToken crIntType =
                (RidFromToken(crInterface) && pInternalImport->IsValidToken(crInterface)) ?
                TypeFromToken(crInterface) :
                0;
            switch (crIntType)
            {
                case mdtTypeDef:
                case mdtTypeRef:
                case mdtTypeSpec:
                    break;
                default:
                    pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_INTERFACENULL);
            }

            TypeHandle intType;

            {
                OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOAD_APPROXPARENTS);
                intType = LoadApproxTypeThrowing(pModule, crInterface, NULL, &genericsInfo.typeContext);
            }

            pInterfaceBuildInfo[i].m_pMethodTable = intType.AsMethodTable();
            if (pInterfaceBuildInfo[i].m_pMethodTable == NULL)
            {
                pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_INTERFACENULL);
            }

            // Ensure this is an interface
            if (!pInterfaceBuildInfo[i].m_pMethodTable->IsInterface())
            {
                 pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_NOTINTERFACE);
            }

            // Check interface for use of variant type parameters
            if ((genericsInfo.pVarianceInfo != NULL) && (TypeFromToken(crInterface) == mdtTypeSpec))
            {
                ULONG cSig;
                PCCOR_SIGNATURE pSig;
                if (FAILED(pInternalImport->GetTypeSpecFromToken(crInterface, &pSig, &cSig)))
                {
                    pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
                }
                // Interfaces behave covariantly
                if (!EEClass::CheckVarianceInSig(
                        genericsInfo.GetNumGenericArgs(),
                        genericsInfo.pVarianceInfo,
                        pModule,
                        SigPointer(pSig, cSig),
                        gpCovariant))
                {
                    pAssembly->ThrowTypeLoadException(
                        pInternalImport,
                        cl,
                        IDS_CLASSLOAD_VARIANCE_IN_INTERFACE);
                }
            }
        }
        _ASSERTE(i == cInterfaces);
    }

    if (fHasLayout ||
        /* Variant delegates should not have any instance fields of the variant.
           type parameter. For now, we just completely disallow all fields even
           if they are non-variant or static, as it is not a useful scenario.
           @TODO: A more logical place for this check would be in
           MethodTableBuilder::EnumerateClassMembers() */
        (fIsDelegate && genericsInfo.pVarianceInfo))
    {
        // check for fields and variance
        ULONG               cFields;
        HENUMInternalHolder hEnumField(pInternalImport);
        hEnumField.EnumInit(mdtFieldDef, cl);

        cFields = pInternalImport->EnumGetCount(&hEnumField);

        if ((cFields != 0) && fIsDelegate && (genericsInfo.pVarianceInfo != NULL))
        {
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_VARIANCE_IN_DELEGATE);
        }

        if (fHasLayout)
        {
            // Though we fail on this condition, we should never run into it.
            CONSISTENCY_CHECK(nstructPackingSize != 0);
            // MD Val check: PackingSize
            if((nstructPackingSize == 0)  ||
               (nstructPackingSize > 128) ||
               (nstructPackingSize & (nstructPackingSize-1)))
            {
                THROW_BAD_FORMAT_MAYBE(!"ClassLayout:Invalid PackingSize", BFA_BAD_PACKING_SIZE, pModule);
                pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
            }

            pLayoutRawFieldInfos = (LayoutRawFieldInfo *)pStackingAllocator->Alloc(
                (S_UINT32(1) + S_UINT32(cFields)) * S_UINT32(sizeof(LayoutRawFieldInfo)));

            {
                // Warning: this can load classes
                CONTRACT_VIOLATION(LoadsTypeViolation);

                // Set a flag that allows us to break dead-locks that are result of the LoadsTypeViolation
                ThreadStateNCStackHolder tsNC(TRUE, Thread::TSNC_LoadsTypeViolation);

                EEClassLayoutInfo::CollectLayoutFieldMetadataThrowing(
                    cl,
                    nstructPackingSize,
                    nstructNLT,
                    fExplicitOffsets,
                    pParentMethodTable,
                    cFields,
                    &hEnumField,
                    pModule,
                    &genericsInfo.typeContext,
                    &(((LayoutEEClass *)pClass)->m_LayoutInfo),
                    pLayoutRawFieldInfos,
                    pAllocator,
                    pamTracker);
            }
        }
    }

    // Resolve this class, given that we know now that all of its dependencies are loaded and resolved.
    // !!! This must be the last thing in this TRY block: if MethodTableBuilder succeeds, it has published the class
    // and there is no going back.
    MethodTableBuilder builder(
        NULL,
        pClass,
        pStackingAllocator,
        pamTracker);

    pMT = builder.BuildMethodTableThrowing(
        pAllocator,
        pLoaderModule,
        pModule,
        cl,
        pInterfaceBuildInfo,
        pLayoutRawFieldInfos,
        pParentMethodTable,
        &genericsInfo,
        parentInst,
        (WORD)cInterfaces);

    RETURN(TypeHandle(pMT));
} // ClassLoader::CreateTypeHandleForTypeDefThrowing

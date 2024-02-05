// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CLASSCOMPAT.CPP

// ===========================================================================
// This file contains backward compatibility functionality for COM Interop.
// ===========================================================================
//


#include "common.h"

#ifndef DACCESS_COMPILE

#include "clsload.hpp"
#include "method.hpp"
#include "class.h"
#include "classcompat.h"
#include "object.h"
#include "field.h"
#include "util.hpp"
#include "excep.h"
#include "threads.h"
#include "stublink.h"
#include "dllimport.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "log.h"
#include "cgensys.h"
#include "gcheaputilities.h"
#include "dbginterface.h"
#include "comdelegate.h"
#include "sigformat.h"
#include "eeprofinterfaces.h"
#include "dllimportcallback.h"
#include "listlock.h"
#include "methodimpl.h"
#include "guidfromname.h"
#include "encee.h"
#include "encee.h"
#include "comsynchronizable.h"
#include "customattribute.h"
#include "virtualcallstub.h"
#include "eeconfig.h"
#include "contractimpl.h"
#include "prettyprintsig.h"

#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "runtimecallablewrapper.h"

#include "generics.h"
#include "contractimpl.h"

//////////////////////////////////////////////////////////////////////////////////////////////
ClassCompat::InterfaceInfo_t* InteropMethodTableData::FindInterface(MethodTable *pInterface)
{
    WRAPPER_NO_CONTRACT;

    for (DWORD i = 0; i < cInterfaceMap; i++)
    {
        ClassCompat::InterfaceInfo_t *iMap = &pInterfaceMap[i];
        if (iMap->m_pMethodTable->IsEquivalentTo(pInterface))
        {
            // Extensible RCW's need to be handled specially because they can have interfaces
            // in their map that are added at runtime. These interfaces will have a start offset
            // of -1 to indicate this. We cannot take for granted that every instance of this
            // COM object has this interface so FindInterface on these interfaces is made to fail.
            //
            // However, we are only considering the statically available slots here
            // (m_wNumInterface doesn't contain the dynamic slots), so we can safely
            // ignore this detail.
            return iMap;
        }
    }

    return NULL;
}

//////////////////////////////////////////////////////////////////////////////////////////////
// get start slot for interface
// returns -1 if interface not found
WORD InteropMethodTableData::GetStartSlotForInterface(MethodTable* pInterface)
{
    WRAPPER_NO_CONTRACT;

    ClassCompat::InterfaceInfo_t* pInfo = FindInterface(pInterface);

    if (pInfo != NULL)
    {
        WORD startSlot = pInfo->GetInteropStartSlot();
        _ASSERTE(startSlot != MethodTable::NO_SLOT);
        return startSlot;
    }

    return MethodTable::NO_SLOT;
}

//////////////////////////////////////////////////////////////////////////////////////////////
// This will return the interop slot for pMD in pMT. It will traverse the inheritance tree
// to find a match.
/*static*/ WORD InteropMethodTableData::GetSlotForMethodDesc(MethodTable *pMT, MethodDesc *pMD)
{
    while (pMT)
    {
        InteropMethodTableData *pData = pMT->LookupComInteropData();
        _ASSERTE(pData);
        for (DWORD i = 0; i < pData->cVTable; i++)
        {
            if (pData->pVTable[i].pMD == pMD)
                return (WORD) i;
        }
        pMT = pMT->GetParentMethodTable();
    }

    return MethodTable::NO_SLOT;
}

//////////////////////////////////////////////////////////////////////////////////////////////
InteropMethodTableSlotDataMap::InteropMethodTableSlotDataMap(InteropMethodTableSlotData *pSlotData, DWORD cSlotData)
{
    m_pSlotData = pSlotData;
    m_cSlotData = cSlotData;
    m_iCurSlot = 0;
}

//////////////////////////////////////////////////////////////////////////////////////////////
InteropMethodTableSlotData *InteropMethodTableSlotDataMap::Exists_Helper(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;
    for (DWORD i = 0; i < m_cSlotData; i++)
    {
        if (m_pSlotData[i].pDeclMD == pMD)
        {
            return (&m_pSlotData[i]);
        }
    }

    return (NULL);
}

//////////////////////////////////////////////////////////////////////////////////////////////
BOOL InteropMethodTableSlotDataMap::Exists(MethodDesc *pMD)
{
    return (Exists_Helper(pMD) != NULL);
}

//////////////////////////////////////////////////////////////////////////////////////////////
InteropMethodTableSlotData *InteropMethodTableSlotDataMap::GetData(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;
    InteropMethodTableSlotData *pEntry = Exists_Helper(pMD);

    if (pEntry)
        return pEntry;

    pEntry = GetNewEntry();
    pEntry->pMD = pMD;
    pEntry->pDeclMD = pMD;
    return (pEntry);
}

//////////////////////////////////////////////////////////////////////////////////////////////
InteropMethodTableSlotData *InteropMethodTableSlotDataMap::GetNewEntry()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_iCurSlot < m_cSlotData);
    InteropMethodTableSlotData *pEntry = &m_pSlotData[m_iCurSlot++];
    pEntry->pMD = NULL;
    pEntry->wFlags = 0;
    pEntry->wSlot = MethodTable::NO_SLOT;
    pEntry->pDeclMD = NULL;
    return (pEntry);
}

namespace ClassCompat
{

//////////////////////////////////////////////////////////////////////////////////////////////
InteropMethodTableData *MethodTableBuilder::BuildInteropVTable(AllocMemTracker *pamTracker)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    MethodTable * pThisMT = GetHalfBakedMethodTable();

    // This should never be called for interfaces or for generic types.
    _ASSERTE(!pThisMT->IsInterface());
    _ASSERTE(!pThisMT->ContainsGenericVariables());
    _ASSERTE(!pThisMT->HasGenericClassInstantiationInHierarchy());

    // Array method tables are created quite differently
    if (pThisMT->IsArray())
        return BuildInteropVTableForArray(pamTracker);

#ifdef _DEBUG
    BOOL fDump = FALSE;
    LPCUTF8 fullName = pThisMT->GetDebugClassName();
    if (fullName) {
        LPWSTR wszRegName = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnInteropVTableBuild);
        if (wszRegName) {
            { // Poor man's narrow
                LPWSTR fromPtr = wszRegName;
                LPUTF8 toPtr = (LPUTF8) wszRegName;
                LPUTF8 result = toPtr;
                while(*fromPtr != 0)
                    *toPtr++ = (char) *fromPtr++;
                *toPtr = 0;
            }
            LPCUTF8 regName = (LPCUTF8) wszRegName;
            LPCUTF8 bracket = (LPCUTF8) strchr(fullName, '[');
            size_t len = strlen(fullName);
            if (bracket != NULL)
                len = bracket - fullName;
            if (strncmp(fullName, regName, len) == 0) {
                _ASSERTE(!"BreakOnInteropVTableBuild");
                fDump = TRUE;
            }
            delete [] wszRegName;
        }
    }
#endif // _DEBUG

    //Get Check Point for the thread-based allocator

    HRESULT hr = S_OK;
    Module *pModule = pThisMT->GetModule();
    mdToken cl = pThisMT->GetCl();
    MethodTable *pParentMethodTable = pThisMT->GetParentMethodTable();

    // The following structs, defined as private members of MethodTableBuilder, contain the necessary local
    // parameters needed for MethodTableBuilder

    // Look at the struct definitions for a detailed list of all parameters available
    // to MethodTableBuilder.

    bmtErrorInfo bmtError;
    bmtProperties bmtProp;
    bmtVtable bmtVT;
    bmtParentInfo bmtParent;
    bmtInterfaceInfo bmtInterface;
    bmtMethodInfo bmtMethod(pModule->GetMDImport());
    bmtTypeInfo bmtType;
    bmtMethodImplInfo bmtMethodImpl(pModule->GetMDImport());

    //Initialize structs

    bmtError.resIDWhy = IDS_CLASSLOAD_GENERAL;          // Set the reason and the offending method def. If the method information
    bmtError.pThrowable = NULL;
    bmtError.pModule  = pModule;
    bmtError.cl       = cl;

    bmtType.pMDImport = pModule->GetMDImport();
    bmtType.pModule = pModule;
    bmtType.cl = cl;

    bmtParent.parentSubst = GetHalfBakedMethodTable()->GetSubstitutionForParent(NULL);
    if (FAILED(bmtType.pMDImport->GetTypeDefProps(
        bmtType.cl,
        &(bmtType.dwAttr),
        &(bmtParent.token))))
    {
        BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
    }

    SetBMTData(
        &bmtError,
        &bmtProp,
        &bmtVT,
        &bmtParent,
        &bmtInterface,
        &bmtMethod,
        &bmtType,
        &bmtMethodImpl);

    // Populate the BMT data structures from the attributes of the incoming MT
    if (pThisMT->IsValueType()) SetIsValueClass();
    if (pThisMT->IsEnum()) SetEnum();
    if (pThisMT->HasLayout()) SetHasLayout();
    if (pThisMT->IsDelegate()) SetIsDelegate();
#ifdef FEATURE_COMINTEROP
    if(pThisMT->GetClass()->IsComClassInterface()) SetIsComClassInterface();
#endif

    // Populate the interface list - these are allocated on the thread's stacking allocator
    //@TODO: This doesn't work for generics - fix if generics will be exposed to COM
    BuildingInterfaceInfo_t *pBuildingInterfaceList;
    WORD wNumInterfaces;
    BuildInteropVTable_InterfaceList(&pBuildingInterfaceList, &wNumInterfaces);

    bmtInterface.wInterfaceMapSize = wNumInterfaces;

    WORD i;

    // Interfaces have a parent class of Object, but we don't really want to inherit all of
    // Object's virtual methods, so pretend we don't have a parent class - at the bottom of this
    // function we reset the parent method table
    if (IsInterface())
    {
        pParentMethodTable = NULL;
    }

    bmtParent.pParentMethodTable = pParentMethodTable;

    // Com Import classes are special
    if (IsComImport()  && !IsEnum() && !IsInterface() && !IsValueClass() && !IsDelegate())
    {
        _ASSERTE(pParentMethodTable == g_pBaseCOMObject);
        _ASSERTE(!(HasLayout()));

        // if the current class is imported
        bmtProp.fIsComObjectType = TRUE;
    }

    bmtParent.pParentMethodTable = pParentMethodTable;

    if (pParentMethodTable != NULL)
    {
        if (pParentMethodTable->IsComObjectType())
        {
            // if the parent class is of ComObjectType
            // so is the child
            bmtProp.fIsComObjectType = TRUE;
        }
    }

    // resolve unresolved interfaces, determine an upper bound on the size of the interface map,
    // and determine the size of the largest interface (in # slots)
    BuildInteropVTable_ResolveInterfaces(pBuildingInterfaceList, &bmtType, &bmtInterface, &bmtVT, &bmtParent, bmtError);

    // Enumerate this class's members
    EnumerateMethodImpls();

    // Enumerate this class's members
    EnumerateClassMethods();

    AllocateMethodWorkingMemory();

    // Allocate the working memory for the interop data
    {
        ////////////////
        // The interop data for the VTable for COM Interop backward compatibility

        // Allocate space to hold on to the MethodDesc for each entry
        bmtVT.ppSDVtable = new (GetStackingAllocator()) InteropMethodTableSlotData*[bmtVT.dwMaxVtableSize];
        ZeroMemory(bmtVT.ppSDVtable, bmtVT.dwMaxVtableSize * sizeof(InteropMethodTableSlotData*));

        // Allocate space to hold on to the MethodDesc for each entry
        bmtVT.ppSDNonVtable = new (GetStackingAllocator()) InteropMethodTableSlotData*[NumDeclaredMethods()];
        ZeroMemory(bmtVT.ppSDNonVtable , sizeof(InteropMethodTableSlotData*)*NumDeclaredMethods());


        DWORD cMaxEntries = (bmtVT.dwMaxVtableSize * 2) + (NumDeclaredMethods() * 2);
        InteropMethodTableSlotData *pInteropData = new (GetStackingAllocator()) InteropMethodTableSlotData[cMaxEntries];
        memset(pInteropData, 0, cMaxEntries * sizeof(InteropMethodTableSlotData));

        bmtVT.pInteropData = new (GetStackingAllocator()) InteropMethodTableSlotDataMap(pInteropData, cMaxEntries);

        // Initialize the map with parent information
        if (bmtParent.pParentMethodTable != NULL)
        {
            InteropMethodTableData *pParentInteropData = bmtParent.pParentMethodTable->LookupComInteropData();
            _ASSERTE(pParentInteropData);

            for ( i = 0; i < pParentInteropData->cVTable; i++)
            {
                InteropMethodTableSlotData *pParentSlot = &pParentInteropData->pVTable[i];
                InteropMethodTableSlotData *pNewEntry = bmtVT.pInteropData->GetData(pParentSlot->pDeclMD);
                pNewEntry->pMD = pParentSlot->pMD;
                pNewEntry->pDeclMD = pParentSlot->pDeclMD;
                pNewEntry->wFlags = pParentSlot->wFlags;
                pNewEntry->wSlot = pParentSlot->wSlot;

                bmtVT.ppSDVtable[i] = pNewEntry;
            }
        }
    }

    // Determine vtable placement for each member in this class
    BuildInteropVTable_PlaceMembers(&bmtType, wNumInterfaces, pBuildingInterfaceList, &bmtMethod,
                                    &bmtError, &bmtProp, &bmtParent, &bmtInterface, &bmtMethodImpl, &bmtVT);

    // First copy what we can leverage from the parent's interface map.
    // The parent's interface map will be identical to the beginning of this class's interface map (i.e.
    // the interfaces will be listed in the identical order).
    if (bmtParent.wNumParentInterfaces > 0)
    {
        PREFIX_ASSUME(pParentMethodTable != NULL); // We have to have parent to have parent interfaces

        _ASSERTE(pParentMethodTable->LookupComInteropData());
        _ASSERTE(bmtParent.wNumParentInterfaces == pParentMethodTable->LookupComInteropData()->cInterfaceMap);
        InterfaceInfo_t *pParentInterfaceList = pParentMethodTable->LookupComInteropData()->pInterfaceMap;


        for (i = 0; i < bmtParent.wNumParentInterfaces; i++)
        {
#ifdef _DEBUG
            _ASSERTE(pParentInterfaceList[i].m_pMethodTable == bmtInterface.pInterfaceMap[i].m_pMethodTable);

            MethodTable *pMT = pParentInterfaceList[i].m_pMethodTable;

            // If the interface resides entirely inside the parent's class methods (i.e. no duplicate
            // slots), then we can place this interface in an identical spot to in the parent.
            //
            // Note carefully: the vtable for this interface could start within the first GetNumVirtuals()
            // entries, but could actually extend beyond it, if we were particularly efficient at placing
            // this interface, so check that the end of the interface vtable is before
            // pParentMethodTable->GetNumVirtuals().

            _ASSERTE(pParentInterfaceList[i].GetInteropStartSlot() + pMT->GetNumVirtuals() <=
                     pParentMethodTable->LookupComInteropData()->cVTable);
#endif // _DEBUG
            // Interface lies inside parent's methods, so we can place it
            bmtInterface.pInterfaceMap[i].SetInteropStartSlot(pParentInterfaceList[i].GetInteropStartSlot());
        }
    }

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
    if (!IsInterface())
    {
        BuildInteropVTable_PlaceVtableMethods(
            &bmtInterface,
            wNumInterfaces,
            pBuildingInterfaceList,
            &bmtVT,
            &bmtMethod,
            &bmtType,
            &bmtError,
            &bmtProp,
            &bmtParent);

        BuildInteropVTable_PlaceMethodImpls(
            &bmtType,
            &bmtMethodImpl,
            &bmtError,
            &bmtInterface,
            &bmtVT,
            &bmtParent);
    }

#ifdef _DEBUG
    if (IsInterface() == FALSE)
    {
        for (i = 0; i < bmtInterface.wInterfaceMapSize; i++)
        {
            _ASSERTE(bmtInterface.pInterfaceMap[i].GetInteropStartSlot() != MethodTable::NO_SLOT);
    }
    }
#endif // _DEBUG

    // Place all non vtable methods
    for (i = 0; i < bmtVT.wCurrentNonVtableSlot; i++)
    {
        bmtVT.SetMethodDescForSlot(bmtVT.wCurrentVtableSlot + i, bmtVT.ppSDNonVtable[i]->pMD);
        CONSISTENCY_CHECK(bmtVT.ppSDNonVtable[i]->wSlot != MethodTable::NO_SLOT);
        bmtVT.ppSDVtable[bmtVT.wCurrentVtableSlot + i] = bmtVT.ppSDNonVtable[i];
    }

    // Must copy overridden slots to duplicate entries in the vtable
    BuildInteropVTable_PropagateInheritance(&bmtVT);

    // ensure we didn't overflow the temporary vtable
    _ASSERTE(bmtVT.wCurrentNonVtableSlot <= bmtVT.dwMaxVtableSize);

    // Finalize.
    InteropMethodTableData *pInteropMT = NULL;

    FinalizeInteropVTable(
                      pamTracker,
                      pThisMT->GetLoaderAllocator(),
                      &bmtVT,
                      &bmtInterface,
                      &bmtType,
                      &bmtProp,
                      &bmtMethod,
                      &bmtError,
                      &bmtParent,
                      &pInteropMT);
    _ASSERTE(pInteropMT);

#ifdef _DEBUG
    if (fDump)
    {
        CQuickBytes qb;
        DWORD       cb = 0;
        PCCOR_SIGNATURE pSig;
        ULONG           cbSig;

        printf("InteropMethodTable\n--------------\n");
        printf("VTable\n------\n");

        for (DWORD i = 0; i < pInteropMT->cVTable; i++)
        {
            // Print the method name
            InteropMethodTableSlotData *pInteropMD = &pInteropMT->pVTable[i];
            printf(pInteropMD->pMD->GetName());
            printf(" ");

            // Print the sig
            if (FAILED(pInteropMD->pMD->GetMDImport()->GetSigOfMethodDef(pInteropMD->pMD->GetMemberDef(), &cbSig, &pSig)))
            {
                pSig = NULL;
                cbSig = 0;
            }
            PrettyPrintSigInternalLegacy(pSig, cbSig, "", &qb, pInteropMD->pMD->GetMDImport());
            printf((LPCUTF8) qb.Ptr());
            printf("\n");
        }
    }
#endif // _DEBUG

    NullBMTData();

    return pInteropMT;
}

//---------------------------------------------------------------------------------------
InteropMethodTableData *MethodTableBuilder::BuildInteropVTableForArray(AllocMemTracker *pamTracker)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        INSTANCE_CHECK;
        PRECONDITION(GetHalfBakedMethodTable()->IsArray());
        PRECONDITION(GetHalfBakedMethodTable()->GetNumVirtuals() == GetHalfBakedMethodTable()->GetParentMethodTable()->GetNumVirtuals());
    } CONTRACTL_END;

    MethodTable * pThisMT = GetHalfBakedMethodTable();

    // Get the interop data for the parent
    MethodTable *pParentMT = pThisMT->GetParentMethodTable();
    InteropMethodTableData *pParentMTData = pParentMT->GetComInteropData();
    CONSISTENCY_CHECK(pParentMTData != NULL);

    // Allocate in the same heap as the array itself
    LoaderHeap *pHeap = pThisMT->GetLoaderAllocator()->GetLowFrequencyHeap();

    // Allocate the overall structure
    InteropMethodTableData *pMTData = (InteropMethodTableData *)(void *) pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InteropMethodTableData))));
    memset(pMTData, 0, sizeof(InteropMethodTableData));

    // Allocate the vtable - this is just a copy from System.Array
    pMTData->cVTable = pParentMTData->cVTable;
    if (pMTData->cVTable != 0)
    {
        pMTData->pVTable = (InteropMethodTableSlotData *)(void *)
            pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InteropMethodTableSlotData)) * S_SIZE_T(pMTData->cVTable)));

        // Copy the vtable
        for (DWORD i = 0; i < pMTData->cVTable; i++)
            pMTData->pVTable[i] = pParentMTData->pVTable[i];
    }

    // Allocate the non-vtable
    pMTData->cNonVTable = pThisMT->GetNumMethods() - pThisMT->GetNumVirtuals();
    if (pMTData->cNonVTable != 0)
    {
        pMTData->pNonVTable = (InteropMethodTableSlotData *)(void *)
            pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InteropMethodTableSlotData)) * S_SIZE_T(pMTData->cNonVTable)));

        // Copy the non-vtable
        UINT32 iCurRealSlot = pThisMT->GetNumVirtuals();
        WORD iCurInteropSlot = pMTData->cVTable;
        for (DWORD i = 0; i < pMTData->cNonVTable; i++, iCurRealSlot++, iCurInteropSlot++)
        {
            pMTData->pNonVTable[i].wSlot = iCurInteropSlot;
            pMTData->pNonVTable[i].pMD = pThisMT->GetMethodDescForSlot(iCurRealSlot);
        }
    }

    // Allocate the interface map
    pMTData->cInterfaceMap = pParentMTData->cInterfaceMap;
    if (pMTData->cInterfaceMap != 0)
    {
        pMTData->pInterfaceMap = (InterfaceInfo_t *)(void *)
            pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InterfaceInfo_t)) * S_SIZE_T(pMTData->cInterfaceMap)));

        // Copy the interface map
        for (DWORD i = 0; i < pMTData->cInterfaceMap; i++)
            pMTData->pInterfaceMap[i] = pParentMTData->pInterfaceMap[i];
    }

    return pMTData;
}

//---------------------------------------------------------------------------------------
VOID MethodTableBuilder::BuildInteropVTable_InterfaceList(
        BuildingInterfaceInfo_t **ppBuildingInterfaceList,
        WORD *pcBuildingInterfaceList)
{
    STANDARD_VM_CONTRACT;

    // Initialize arguments
    *pcBuildingInterfaceList = 0;
    *ppBuildingInterfaceList = NULL;

    // Get the metadata for enumerating the interfaces of the class
    IMDInternalImport *pMDImport = GetModule()->GetMDImport();

    // Now load all the interfaces
    HENUMInternalHolder hEnumInterfaceImpl(pMDImport);
    hEnumInterfaceImpl.EnumInit(mdtInterfaceImpl, GetCl());

    // Get the count for the number of interfaces from metadata
    DWORD cAllInterfaces = pMDImport->EnumGetCount(&hEnumInterfaceImpl);
    WORD cNonGenericItfs = 0;

    // Iterate through each interface token and get the type for the interface and put
    // it into the BuildingInterfaceInfo_t struct.
    if (cAllInterfaces != 0)
    {
        mdInterfaceImpl ii;
        Module *pModule = GetModule();

        // Allocate the BuildingInterfaceList table
        *ppBuildingInterfaceList = new(GetStackingAllocator()) BuildingInterfaceInfo_t[cAllInterfaces];
        BuildingInterfaceInfo_t *pInterfaceBuildInfo = *ppBuildingInterfaceList;

        while (pMDImport->EnumNext(&hEnumInterfaceImpl, &ii))
        {
            mdTypeRef crInterface;
            TypeHandle intType;

            // Get properties on this interface
            if (FAILED(pMDImport->GetTypeOfInterfaceImpl(ii, &crInterface)))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
            }
            SigTypeContext typeContext = SigTypeContext(TypeHandle(GetHalfBakedMethodTable()));
            intType = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, crInterface, &typeContext,
                                                                  ClassLoader::ThrowIfNotFound,
                                                                  ClassLoader::FailIfUninstDefOrRef);

            // At this point, the interface should never have any non instantiated generic parameters.
            _ASSERTE(!intType.IsGenericTypeDefinition());

            // Skip any generic interfaces.
            if (intType.GetNumGenericArgs() != 0)
                continue;

            pInterfaceBuildInfo[cNonGenericItfs].m_pMethodTable = intType.AsMethodTable();
            _ASSERTE(pInterfaceBuildInfo[cNonGenericItfs].m_pMethodTable != NULL);
            _ASSERTE(pInterfaceBuildInfo[cNonGenericItfs].m_pMethodTable->IsInterface());
            cNonGenericItfs++;
        }
        _ASSERTE(cNonGenericItfs <= cAllInterfaces);
    }

    *pcBuildingInterfaceList = cNonGenericItfs;
}

//---------------------------------------------------------------------------------------
// Used by BuildInteropVTable
//
// Determine vtable placement for each member in this class
//

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
VOID MethodTableBuilder::BuildInteropVTable_PlaceMembers(
    bmtTypeInfo* bmtType,
                           DWORD numDeclaredInterfaces,
                           BuildingInterfaceInfo_t *pBuildingInterfaceList,
    bmtMethodInfo* bmtMethod,
                           bmtErrorInfo* bmtError,
                           bmtProperties* bmtProp,
                           bmtParentInfo* bmtParent,
                           bmtInterfaceInfo* bmtInterface,
                           bmtMethodImplInfo* bmtMethodImpl,
                           bmtVtable* bmtVT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtType));
        PRECONDITION(CheckPointer(bmtMethod));
        PRECONDITION(CheckPointer(bmtError));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(bmtMethodImpl));
        PRECONDITION(CheckPointer(bmtVT));
    }
    CONTRACTL_END;

    _ASSERTE(!IsInterface());

    Module * pModule = GetModule();

#ifdef _DEBUG
    LPCUTF8 pszDebugName,pszDebugNamespace;
    if (FAILED(bmtType->pModule->GetMDImport()->GetNameOfTypeDef(GetCl(), &pszDebugName, &pszDebugNamespace)))
    {
        pszDebugName = pszDebugNamespace = "Invalid TypeDef record";
    }
#endif // _DEBUG

    HRESULT hr = S_OK;
    DWORD i, j;
    DWORD  dwClassDeclFlags = 0xffffffff;
    DWORD  dwClassNullDeclFlags = 0xffffffff;

    for (i = 0; i < NumDeclaredMethods(); i++)
    {
        LPCUTF8     szMemberName = NULL;
        PCCOR_SIGNATURE pMemberSignature = NULL;
        DWORD       cMemberSignature = 0;
        mdToken     tokMember;
        DWORD       dwMemberAttrs;
        DWORD       dwDescrOffset;
        DWORD       dwImplFlags;
        BOOL        fMethodImplementsInterface = FALSE;
        DWORD       dwMDImplementsInterfaceNum = 0;
        DWORD       dwMDImplementsSlotNum = 0;
        DWORD       dwParentAttrs;

        tokMember = bmtMethod->rgMethodTokens[i];
        dwMemberAttrs = bmtMethod->rgMethodAttrs[i];
        dwDescrOffset = bmtMethod->rgMethodRVA[i];
        dwImplFlags = bmtMethod->rgMethodImplFlags[i];

        DWORD Classification = bmtMethod->rgMethodClassifications[i];

        // If this member is a method which overrides a parent method, it will be set to non-NULL
        MethodDesc *pParentMethodDesc = NULL;

        szMemberName = bmtMethod->rgszMethodName[i];

        // constructors and class initialisers are special
        if (!IsMdRTSpecialName(dwMemberAttrs))
        {
            // The method does not have the special marking
            if (IsMdVirtual(dwMemberAttrs))
            {
                // Hash that a method with this name exists in this class
                // Note that ctors and static ctors are not added to the table
                DWORD dwHashName = HashStringA(szMemberName);

                // If the member is marked with a new slot we do not need to find it
                // in the parent
                if (!IsMdNewSlot(dwMemberAttrs))
                {
                    // If we're not doing sanity checks, then assume that any method declared static
                    // does not attempt to override some virtual parent.
                    if (!IsMdStatic(dwMemberAttrs) && bmtParent->pParentMethodTable != NULL)
                    {
                        // Attempt to find the method with this name and signature in the parent class.
                        // This method may or may not create pParentMethodHash (if it does not already exist).
                        // It also may or may not fill in pMemberSignature/cMemberSignature.
                        // An error is only returned when we can not create the hash.
                        // NOTE: This operation touches metadata
                        {
                            BOOL fMethodConstraintsMatch = FALSE;
                            VERIFY(SUCCEEDED(LoaderFindMethodInClass(
                                                          szMemberName,
                                                          bmtType->pModule,
                                                          tokMember,
                                                          &pParentMethodDesc,
                                                          &pMemberSignature, &cMemberSignature,
                                                          dwHashName,
                                                          &fMethodConstraintsMatch)));
                            //this assert should hold because interop methods cannot be generic
                            _ASSERTE(pParentMethodDesc == NULL || fMethodConstraintsMatch);
                        }

                        if (pParentMethodDesc != NULL)
                        {
                            dwParentAttrs = pParentMethodDesc->GetAttrs();

                            _ASSERTE(IsMdVirtual(dwParentAttrs) && "Non virtual methods should not be searched");
                            _ASSERTE(!(IsMdFinal(dwParentAttrs)));
                        }
                    }
                }
            }
        }

        if(pParentMethodDesc == NULL) {
            // This method does not exist in the parent.  If we are a class, check whether this
            // method implements any interface.  If true, we can't place this method now.
            if ((!IsInterface()) &&
                (   IsMdPublic(dwMemberAttrs) &&
                    IsMdVirtual(dwMemberAttrs) &&
                    !IsMdStatic(dwMemberAttrs) &&
                    !IsMdRTSpecialName(dwMemberAttrs))) {

                // Don't check parent class interfaces - if the parent class had to implement an interface,
                // then it is already guaranteed that we inherited that method.
                _ASSERTE(!bmtParent->pParentMethodTable || bmtParent->pParentMethodTable->LookupComInteropData());
                DWORD numInheritedInts = (bmtParent->pParentMethodTable ?
                    (DWORD) bmtParent->pParentMethodTable->LookupComInteropData()->cInterfaceMap: 0);

                for (j = numInheritedInts; j < bmtInterface->wInterfaceMapSize; j++)
                {
                    MethodTable *pInterface = bmtInterface->pInterfaceMap[j].m_pMethodTable;
                    if (pMemberSignature == NULL)
                    {   // We've been trying to avoid asking for the signature - now we need it
                        if (FAILED(bmtType->pMDImport->GetSigOfMethodDef(tokMember, &cMemberSignature, &pMemberSignature)))
                        {
                            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                        }
                    }

                    WORD slotNum = (WORD) (-1);
                    MethodDesc *pItfMD = MemberLoader::FindMethod(pInterface,
                        szMemberName, pMemberSignature, cMemberSignature, bmtType->pModule);

                    if (pItfMD != NULL)
                    {
                        // This method implements an interface - don't place it
                        fMethodImplementsInterface = TRUE;

                        // Keep track of this fact and use it while placing the interface
                        slotNum = (WORD) pItfMD->GetSlot();
                        if (bmtInterface->pppInterfaceImplementingMD[j] == NULL)
                        {
                            bmtInterface->pppInterfaceImplementingMD[j] = new (GetStackingAllocator()) MethodDesc * [pInterface->GetNumVirtuals()];
                            memset(bmtInterface->pppInterfaceImplementingMD[j], 0, sizeof(MethodDesc *) * pInterface->GetNumVirtuals());

                            bmtInterface->pppInterfaceDeclaringMD[j] = new (GetStackingAllocator()) MethodDesc * [pInterface->GetNumVirtuals()];
                            memset(bmtInterface->pppInterfaceDeclaringMD[j], 0, sizeof(MethodDesc *) * pInterface->GetNumVirtuals());
                        }

                        bmtInterface->pppInterfaceDeclaringMD[j][slotNum] = pItfMD;

                        dwMDImplementsInterfaceNum = j;
                        dwMDImplementsSlotNum = slotNum;
                        break;
                    }
                }
            }
        }

        // Now find the MethodDesc associated with this method
        MethodDesc *pNewMD = MemberLoader::FindMethod(GetHalfBakedMethodTable(), tokMember);
        _ASSERTE(!bmtVT->pInteropData->Exists(pNewMD));
        InteropMethodTableSlotData *pNewMDData = bmtVT->pInteropData->GetData(pNewMD);

        _ASSERTE(pNewMD != NULL);
        _ASSERTE(dwMemberAttrs == pNewMD->GetAttrs());

        _ASSERTE(bmtParent->ppParentMethodDescBufPtr != NULL);
        _ASSERTE(((bmtParent->ppParentMethodDescBufPtr - bmtParent->ppParentMethodDescBuf) / sizeof(MethodDesc*))
                  < NumDeclaredMethods());
        *(bmtParent->ppParentMethodDescBufPtr++) = pParentMethodDesc;
        *(bmtParent->ppParentMethodDescBufPtr++) = pNewMD;

        if (fMethodImplementsInterface  && IsMdVirtual(dwMemberAttrs))
        {
            bmtInterface->pppInterfaceImplementingMD[dwMDImplementsInterfaceNum][dwMDImplementsSlotNum] = pNewMD;
        }

        // Set the MethodDesc value
        bmtMethod->ppMethodDescList[i] = pNewMD;

        // Make sure that fcalls have a 0 rva.  This is assumed by the prejit fixup logic
        _ASSERTE(((Classification & ~mdcMethodImpl) != mcFCall) || dwDescrOffset == 0);

        // Non-virtual method
        if (IsMdStatic(dwMemberAttrs) ||
            !IsMdVirtual(dwMemberAttrs) ||
            IsMdRTSpecialName(dwMemberAttrs))
        {
            // Non-virtual method (doesn't go into the vtable)
            _ASSERTE(bmtVT->pNonVtableMD[bmtVT->wCurrentNonVtableSlot] == NULL);

            // Set the data for the method
            pNewMDData->wSlot = bmtVT->wCurrentNonVtableSlot;

            // Add the slot into the non-virtual method table
            bmtVT->pNonVtableMD[bmtVT->wCurrentNonVtableSlot] = pNewMD;
            bmtVT->ppSDNonVtable[bmtVT->wCurrentNonVtableSlot] = pNewMDData;

            // Increment the current non-virtual method table slot
            bmtVT->wCurrentNonVtableSlot++;
        }

        // Virtual method
        else
        {
            if (IsInterface())
            {   // (shouldn't happen for this codepath)
                UNREACHABLE();
            }

            else if (pParentMethodDesc != NULL)
            {   // We are overriding a parent's vtable slot
                CONSISTENCY_CHECK(bmtVT->pInteropData->Exists(pParentMethodDesc));
                WORD slotNumber = bmtVT->pInteropData->GetData(pParentMethodDesc)->wSlot;

                // If the MethodDesc was inherited by an interface but not implemented,
                // then the interface's MethodDesc is sitting in the slot and will not reflect
                // the true slot number. Need to find the starting slot of the interface in
                // the parent class to figure out the true slot (starting slot + itf slot)
                if (pParentMethodDesc->IsInterface())
                {
                    MethodTable *pItfMT = pParentMethodDesc->GetMethodTable();
                    WORD startSlot = bmtParent->pParentMethodTable->LookupComInteropData()->GetStartSlotForInterface(pItfMT);
                    _ASSERTE(startSlot != (WORD) -1);
                    slotNumber += startSlot;
                }

                // we are overriding a parent method, so place this method now
                bmtVT->SetMethodDescForSlot(slotNumber, pNewMD);
                bmtVT->ppSDVtable[slotNumber] = pNewMDData;

                pNewMDData->wSlot = slotNumber;
            }

            else if (!fMethodImplementsInterface)
            {   // Place it unless we will do it when laying out an interface or it is a body to
            // a method impl. If it is an impl then we will use the slots used by the definition.

                // Store the slot for this method
                pNewMDData->wSlot = bmtVT->wCurrentVtableSlot;

                // Now copy the method into the vtable, and interop data
                bmtVT->SetMethodDescForSlot(bmtVT->wCurrentVtableSlot, pNewMD);
                bmtVT->ppSDVtable[bmtVT->wCurrentVtableSlot] = pNewMDData;

                // Increment current vtable slot, since we're not overriding a parent slot
                bmtVT->wCurrentVtableSlot++;
            }
        }

        if (Classification & mdcMethodImpl)
        {   // If this method serves as the BODY of a MethodImpl specification, then
        // we should iterate all the MethodImpl's for this class and see just how many
        // of them this method participates in as the BODY.
            for(DWORD m = 0; m < bmtMethodImpl->dwNumberMethodImpls; m++)
            {
                if(tokMember == bmtMethodImpl->rgMethodImplTokens[m].methodBody)
                {
                    MethodDesc* desc = NULL;
                    mdToken mdDecl = bmtMethodImpl->rgMethodImplTokens[m].methodDecl;
                    Substitution *pDeclSubst = &bmtMethodImpl->pMethodDeclSubsts[m];

                    // Get the parent
                    mdToken tkParent = mdTypeDefNil;
                    if (TypeFromToken(mdDecl) == mdtMethodDef || TypeFromToken(mdDecl) == mdtMemberRef)
                    {
                        hr = bmtType->pMDImport->GetParentToken(mdDecl,&tkParent);
                        if (FAILED(hr))
                        {
                            BuildMethodTableThrowException(hr, *bmtError);
                        }
                    }

                    if (GetCl() == tkParent)
                    {   // The DECL has been declared
                    // within the class that we're currently building.
                        hr = S_OK;

                        if(bmtError->pThrowable != NULL)
                            *(bmtError->pThrowable) = NULL;

                        // <TODO>Verify that the substitution doesn't change for this case </TODO>
                        if(TypeFromToken(mdDecl) != mdtMethodDef) {
                            hr = FindMethodDeclarationForMethodImpl(
                                        bmtType->pMDImport,
                                        GetCl(),
                                        mdDecl,
                                        &mdDecl);
                            _ASSERTE(SUCCEEDED(hr));

                            // Make sure the virtual states are the same
                            DWORD dwDescAttrs;
                            if (FAILED(bmtType->pMDImport->GetMethodDefProps(mdDecl, &dwDescAttrs)))
                            {
                                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                            }
                            _ASSERTE(IsMdVirtual(dwMemberAttrs) == IsMdVirtual(dwDescAttrs));
                        }
                    }
                    else
                    {
                        SigTypeContext typeContext;

                        desc = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(bmtType->pModule,
                                                                               mdDecl,
                                                                               &typeContext,
                                                                               FALSE, FALSE); // don't demand generic method args
                        mdDecl = mdTokenNil;
                        // Make sure the body is virtaul
                        _ASSERTE(IsMdVirtual(dwMemberAttrs));
                    }

                    // Only add the method impl if the interface it is declared on is non generic.
                    // NULL desc represent method impls to methods on the current class which
                    // we know isn't generic.
                    if ((desc == NULL) || (desc->GetMethodTable()->GetNumGenericArgs() == 0))
                    {
                        bmtMethodImpl->AddMethod(pNewMD,
                                                 desc,
                                                 mdDecl,
                                                 pDeclSubst);
                    }
                }
            }
        }
    } /* end ... for each member */
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif

//---------------------------------------------------------------------------------------
// Resolve unresolved interfaces, determine an upper bound on the size of the interface map,
// and determine the size of the largest interface (in # slots)
VOID MethodTableBuilder::BuildInteropVTable_ResolveInterfaces(
                                BuildingInterfaceInfo_t *pBuildingInterfaceList,
                                bmtTypeInfo* bmtType,
                                bmtInterfaceInfo* bmtInterface,
                                bmtVtable* bmtVT,
                                bmtParentInfo* bmtParent,
                                const bmtErrorInfo & bmtError)
{

    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtParent));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD i;

    // resolve unresolved interfaces, determine an upper bound on the size of the interface map,
    // and determine the size of the largest interface (in # slots)
    bmtInterface->dwMaxExpandedInterfaces = 0; // upper bound on max # interfaces implemented by this class

    // First look through the interfaces explicitly declared by this class
    for (i = 0; i < bmtInterface->wInterfaceMapSize; i++)
    {
        MethodTable *pInterface = pBuildingInterfaceList[i].m_pMethodTable;

        bmtInterface->dwMaxExpandedInterfaces += (1+ pInterface->GetNumInterfaces());
    }

    // Now look at interfaces inherited from the parent
    if (bmtParent->pParentMethodTable != NULL)
    {
        _ASSERTE(bmtParent->pParentMethodTable->LookupComInteropData());
        InteropMethodTableData *pInteropData = bmtParent->pParentMethodTable->LookupComInteropData();
        InterfaceInfo_t *pParentInterfaceMap = pInteropData->pInterfaceMap;

        for (i = 0; i < pInteropData->cInterfaceMap; i++)
        {
            MethodTable *pInterface = pParentInterfaceMap[i].m_pMethodTable;

            bmtInterface->dwMaxExpandedInterfaces += (1+pInterface->GetNumInterfaces());
        }
    }

    // Create a fully expanded map of all interfaces we implement
    bmtInterface->pInterfaceMap = new (GetStackingAllocator()) InterfaceInfo_t[bmtInterface->dwMaxExpandedInterfaces];

    // # slots of largest interface
    bmtInterface->dwLargestInterfaceSize = 0;

    DWORD dwNumDeclaredInterfaces = bmtInterface->wInterfaceMapSize;

    BuildInteropVTable_CreateInterfaceMap(pBuildingInterfaceList, bmtInterface, &bmtInterface->wInterfaceMapSize, &bmtInterface->dwLargestInterfaceSize, bmtParent->pParentMethodTable);

    _ASSERTE(bmtInterface->wInterfaceMapSize <= bmtInterface->dwMaxExpandedInterfaces);

    if (bmtInterface->dwLargestInterfaceSize > 0)
    {
        // This is needed later - for each interface, we get the MethodDesc pointer for each
        // method.  We need to be able to persist at most one interface at a time, so we
        // need enough memory for the largest interface.
        bmtInterface->ppInterfaceMethodDescList = new (GetStackingAllocator()) MethodDesc*[bmtInterface->dwLargestInterfaceSize];

        bmtInterface->ppInterfaceDeclMethodDescList = new (GetStackingAllocator()) MethodDesc*[bmtInterface->dwLargestInterfaceSize];
    }

    EEClass *pParentClass = (IsInterface() || bmtParent->pParentMethodTable == NULL) ? NULL : bmtParent->pParentMethodTable->GetClass();

    // For all the new interfaces we bring in, sum the methods
    bmtInterface->dwTotalNewInterfaceMethods = 0;
    if (pParentClass != NULL)
    {
        for (i = bmtParent->pParentMethodTable->GetNumInterfaces(); i < (bmtInterface->wInterfaceMapSize); i++)
            bmtInterface->dwTotalNewInterfaceMethods +=
                bmtInterface->pInterfaceMap[i].m_pMethodTable->GetNumVirtuals();
    }

    // The interface map is probably smaller than dwMaxExpandedInterfaces, so we'll copy the
    // appropriate number of bytes when we allocate the real thing later.

    // Inherit parental slot counts
    if (pParentClass != NULL)
    {
        InteropMethodTableData *pParentInteropMT = bmtParent->pParentMethodTable->LookupComInteropData();
        bmtVT->wCurrentVtableSlot         = pParentInteropMT->cVTable;
        bmtParent->wNumParentInterfaces   = pParentInteropMT->cInterfaceMap;
    }
    else
    {
        bmtVT->wCurrentVtableSlot          = 0;
        bmtParent->wNumParentInterfaces   = 0;
    }

    bmtVT->wCurrentNonVtableSlot      = 0;

    bmtInterface->pppInterfaceImplementingMD = (MethodDesc ***) GetStackingAllocator()->Alloc(S_UINT32(sizeof(MethodDesc *)) * S_UINT32(bmtInterface->dwMaxExpandedInterfaces));
    memset(bmtInterface->pppInterfaceImplementingMD, 0, sizeof(MethodDesc *) * bmtInterface->dwMaxExpandedInterfaces);

    bmtInterface->pppInterfaceDeclaringMD = (MethodDesc ***) GetStackingAllocator()->Alloc(S_UINT32(sizeof(MethodDesc *)) * S_UINT32(bmtInterface->dwMaxExpandedInterfaces));
    memset(bmtInterface->pppInterfaceDeclaringMD, 0, sizeof(MethodDesc *) * bmtInterface->dwMaxExpandedInterfaces);

    return;

}

//---------------------------------------------------------------------------------------
// Fill out a fully expanded interface map, such that if we are declared to implement I3, and I3 extends I1,I2,
// then I1,I2 are added to our list if they are not already present.
//
// Returns FALSE for failure.  <TODO>Currently we don't fail, but @TODO perhaps we should fail if we recurse
// too much.</TODO>
//
VOID MethodTableBuilder::BuildInteropVTable_CreateInterfaceMap(BuildingInterfaceInfo_t *pBuildingInterfaceList,
                                                    bmtInterfaceInfo* bmtInterface,
                                                    WORD *pwInterfaceListSize,
                                                    DWORD *pdwMaxInterfaceMethods,
                                                    MethodTable *pParentMethodTable)
{
    STANDARD_VM_CONTRACT;

    WORD    i;
    InterfaceInfo_t *pInterfaceMap = bmtInterface->pInterfaceMap;
    WORD wNumInterfaces = bmtInterface->wInterfaceMapSize;

    // pdwInterfaceListSize points to bmtInterface->pInterfaceMapSize so we cache it above
    *pwInterfaceListSize = 0;

    // First inherit all the parent's interfaces.  This is important, because our interface map must
    // list the interfaces in identical order to our parent.
    //
    // <NICE> we should document the reasons why.  One reason is that DispatchMapTypeIDs can be indexes
    // into the list </NICE>
    if (pParentMethodTable != NULL)
    {
        _ASSERTE(pParentMethodTable->LookupComInteropData());
        InteropMethodTableData *pInteropData = pParentMethodTable->LookupComInteropData();
        InterfaceInfo_t *pParentInterfaceMap = pInteropData->pInterfaceMap;
        unsigned cParentInterfaceMap = pInteropData->cInterfaceMap;

        // The parent's interface list is known to be fully expanded
        for (i = 0; i < cParentInterfaceMap; i++)
        {
            // Need to keep track of the interface with the largest number of methods
            if (pParentInterfaceMap[i].m_pMethodTable->GetNumVirtuals() > *pdwMaxInterfaceMethods)
            {
                *pdwMaxInterfaceMethods = pParentInterfaceMap[i].m_pMethodTable->GetNumVirtuals();
            }

            pInterfaceMap[*pwInterfaceListSize].m_pMethodTable = pParentInterfaceMap[i].m_pMethodTable;
            pInterfaceMap[*pwInterfaceListSize].SetInteropStartSlot(MethodTable::NO_SLOT);
            pInterfaceMap[*pwInterfaceListSize].m_wFlags = 0;
            (*pwInterfaceListSize)++;
        }
    }

    // Go through each interface we explicitly implement (if a class), or extend (if an interface)
    for (i = 0; i < wNumInterfaces; i++)
    {
        MethodTable *pDeclaredInterface = pBuildingInterfaceList[i].m_pMethodTable;

        BuildInteropVTable_ExpandInterface(pInterfaceMap, pDeclaredInterface,
                                           pwInterfaceListSize, pdwMaxInterfaceMethods,
                                           TRUE);
    }
}

//---------------------------------------------------------------------------------------
// Given an interface map to fill out, expand pNewInterface (and its sub-interfaces) into it, increasing
// pdwInterfaceListSize as appropriate, and avoiding duplicates.
//
VOID MethodTableBuilder::BuildInteropVTable_ExpandInterface(InterfaceInfo_t *pInterfaceMap,
                              MethodTable *pNewInterface,
                              WORD *pwInterfaceListSize,
                              DWORD *pdwMaxInterfaceMethods,
                              BOOL fDirect)
{
    STANDARD_VM_CONTRACT;

    DWORD i;

    // The interface list contains the fully expanded set of interfaces from the parent then
    // we start adding all the interfaces we declare. We need to know which interfaces
    // we declare but do not need duplicates of the ones we declare. This means we can
    // duplicate our parent entries.

    // Is it already present in the list?
    for (i = 0; i < (*pwInterfaceListSize); i++) {
        if (pInterfaceMap[i].m_pMethodTable->IsEquivalentTo(pNewInterface)) {
            if(fDirect) {
                pInterfaceMap[i].m_wFlags |= InterfaceInfo_t::interface_declared_on_class;
            }
            return; // found it, don't add it again
        }
    }

    if (pNewInterface->GetNumVirtuals() > *pdwMaxInterfaceMethods) {
        *pdwMaxInterfaceMethods = pNewInterface->GetNumVirtuals();
    }

    // Add it and each sub-interface
    pInterfaceMap[*pwInterfaceListSize].m_pMethodTable = pNewInterface;
    pInterfaceMap[*pwInterfaceListSize].SetInteropStartSlot(MethodTable::NO_SLOT);
    pInterfaceMap[*pwInterfaceListSize].m_wFlags = 0;

    if(fDirect)
        pInterfaceMap[*pwInterfaceListSize].m_wFlags |= InterfaceInfo_t::interface_declared_on_class;

    (*pwInterfaceListSize)++;

    if (pNewInterface->GetNumInterfaces() != 0) {
        MethodTable::InterfaceMapIterator it = pNewInterface->IterateInterfaceMap();
        while (it.Next()) {
            MethodTable *pItf = it.GetInterfaceApprox();
            if (pItf->HasInstantiation() || pItf->IsSpecialMarkerTypeForGenericCasting())
                continue;

            BuildInteropVTable_ExpandInterface(pInterfaceMap, pItf,
                                               pwInterfaceListSize, pdwMaxInterfaceMethods, FALSE);
        }
    }

    return;
}

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

//---------------------------------------------------------------------------------------
VOID MethodTableBuilder::BuildInteropVTable_PlaceVtableMethods(
    bmtInterfaceInfo* bmtInterface,
                                                       DWORD numDeclaredInterfaces,
                                                       BuildingInterfaceInfo_t *pBuildingInterfaceList,
                                                       bmtVtable* bmtVT,
    bmtMethodInfo* bmtMethod,
    bmtTypeInfo* bmtType,
                                                       bmtErrorInfo* bmtError,
                                                       bmtProperties* bmtProp,
                                                       bmtParentInfo* bmtParent)
{
    STANDARD_VM_CONTRACT;

    DWORD i;
    BOOL fParentInterface;

    for (WORD wCurInterface = 0;
         wCurInterface < bmtInterface->wInterfaceMapSize;
         wCurInterface++)
    {
        fParentInterface = FALSE;
        // Keep track of the current interface
        InterfaceInfo_t *pCurItfInfo = &(bmtInterface->pInterfaceMap[wCurInterface]);
        // The interface we are attempting to place
        MethodTable *pInterface = pCurItfInfo->m_pMethodTable;

        // Did we place this interface already due to the parent class's interface placement?
        if (pCurItfInfo->GetInteropStartSlot() != MethodTable::NO_SLOT)
        {
            // If we have declared it then we re-lay it out
            if(pCurItfInfo->IsDeclaredOnClass())
            {
                // This should be in the outer IF statement, not this inner one, but we'll keep
                // it this way to remain consistent for backward compatibility.
                fParentInterface = TRUE;

                // If the interface is folded into the non-interface portion of the vtable, we need to unfold it.
                WORD wStartSlot = pCurItfInfo->GetInteropStartSlot();
                MethodTable::MethodIterator it(pInterface);
                for (; it.IsValid(); it.Next())
                {
                    if (it.IsVirtual())
                    {
                        if(bmtVT->ppSDVtable[wStartSlot+it.GetSlotNumber()]->wSlot == wStartSlot+it.GetSlotNumber())
                        {   // If the MD slot is equal to the vtable slot number, then this means the interface
                    // was folded into the non-interface part of the vtable and needs to get unfolded
                    // in case a specific override occurs for one of the conceptually two distinct
                    // slots and not the other (i.e., a MethodImpl overrides an interface method but not
                    // the class' virtual method).
                            pCurItfInfo->SetInteropStartSlot(MethodTable::NO_SLOT);
                            fParentInterface = FALSE;
                            break;
                        }
                    }
                }
            }
            else
            {
                continue;
            }
        }

        if (pInterface->GetNumVirtuals() == 0)
        {
            // no calls can be made to this interface anyway
            // so initialize the slot number to 0
            pCurItfInfo->SetInteropStartSlot((WORD) 0);
            continue;
        }

        // If this interface has not been given a starting position do that now.
        if(!fParentInterface)
            pCurItfInfo->SetInteropStartSlot(bmtVT->wCurrentVtableSlot);

        // For each method declared in this interface
        {
            MethodTable::MethodIterator it(pInterface);
            for (; it.IsValid(); it.Next())
            {
                if (it.IsVirtual())
                {
                    DWORD       dwMemberAttrs;

                    // See if we have info gathered while placing members
                    if (bmtInterface->pppInterfaceImplementingMD[wCurInterface] && bmtInterface->pppInterfaceImplementingMD[wCurInterface][it.GetSlotNumber()] != NULL)
                    {
                        bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] = bmtInterface->pppInterfaceImplementingMD[wCurInterface][it.GetSlotNumber()];
                        bmtInterface->ppInterfaceDeclMethodDescList[it.GetSlotNumber()] = bmtInterface->pppInterfaceDeclaringMD[wCurInterface][it.GetSlotNumber()];
                        continue;
                    }

                    MethodDesc *pInterfaceMD = pInterface->GetMethodDescForSlot(it.GetSlotNumber());
                    _ASSERTE(pInterfaceMD  != NULL);

                    LPCUTF8     pszInterfaceMethodName  = pInterfaceMD->GetNameOnNonArrayClass();
                    PCCOR_SIGNATURE pInterfaceMethodSig;
                    DWORD       cInterfaceMethodSig;

                    pInterfaceMD->GetSig(&pInterfaceMethodSig, &cInterfaceMethodSig);

                    // Try to find the method explicitly declared in our class
                    for (i = 0; i < NumDeclaredMethods(); i++)
                    {
                        // look for interface method candidates only
                        dwMemberAttrs = bmtMethod->rgMethodAttrs[i];

                        // Note that non-publics can legally be exposed via an interface.
                        if (IsMdVirtual(dwMemberAttrs) && IsMdPublic(dwMemberAttrs))
                        {
                            LPCUTF8     pszMemberName;

                            pszMemberName = bmtMethod->rgszMethodName[i];
                            _ASSERTE(!(pszMemberName == NULL));

#ifdef _DEBUG
                            if(GetHalfBakedClass()->m_fDebuggingClass && g_pConfig->ShouldBreakOnMethod(pszMemberName))
                                CONSISTENCY_CHECK_MSGF(false, ("BreakOnMethodName: '%s' ", pszMemberName));
#endif // _DEBUG

                            if (strcmp(pszMemberName,pszInterfaceMethodName) == 0)
                            {
                                PCCOR_SIGNATURE pMemberSignature;
                                DWORD       cMemberSignature;

                                _ASSERTE(TypeFromToken(bmtMethod->rgMethodTokens[i]) == mdtMethodDef);
                                if (FAILED(bmtType->pMDImport->GetSigOfMethodDef(
                                    bmtMethod->rgMethodTokens[i],
                                    &cMemberSignature,
                                    &pMemberSignature)))
                                {
                                    BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                                }

                                if (MetaSig::CompareMethodSigs(
                                    pMemberSignature,
                                    cMemberSignature,
                                    bmtType->pModule, NULL,
                                    pInterfaceMethodSig,
                                    cInterfaceMethodSig,
                                    pInterfaceMD->GetModule(), NULL, FALSE))
                                {   // Found match, break from loop
                                    break;
                                }
                            }
                        }
                    } // end ... try to find method

                    _ASSERTE(it.GetSlotNumber() < bmtInterface->dwLargestInterfaceSize);

                    if (i >= NumDeclaredMethods())
                    {
                        // if this interface has been laid out by our parent then
                        // we do not need to define a new method desc for it
                        if(fParentInterface)
                        {
                            bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] = NULL;
                            bmtInterface->ppInterfaceDeclMethodDescList[it.GetSlotNumber()] = NULL;
                        }
                        else
                        {
                            // We will use the interface implementation if we do not find one in the
                            // parent. It will have to be overridden by the a method impl unless the
                            // class is abstract or it is a special COM type class.

                            MethodDesc* pParentMD = NULL;
                            if(bmtParent->pParentMethodTable)
                            {
#ifdef _DEBUG
                                if(GetHalfBakedClass()->m_fDebuggingClass && g_pConfig->ShouldBreakOnMethod(pszInterfaceMethodName))
                                    CONSISTENCY_CHECK_MSGF(false, ("BreakOnMethodName: '%s' ", pszInterfaceMethodName));
#endif // _DEBUG
                                // Check the parent class
                                pParentMD = MemberLoader::FindMethod(bmtParent->pParentMethodTable,
                                    pszInterfaceMethodName,
                                                                     pInterfaceMethodSig,
                                                                     cInterfaceMethodSig,
                                                                     pInterfaceMD->GetModule(),
                                                                     MemberLoader::FM_Default,
                                                                     &bmtParent->parentSubst);
                            }
                            // make sure we do a better back patching for these methods
                            if(pParentMD && IsMdVirtual(pParentMD->GetAttrs()))
                            {
                                bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] = pParentMD;
                                bmtInterface->ppInterfaceDeclMethodDescList[it.GetSlotNumber()] = pInterfaceMD;
                            }
                            else
                            {
                                bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] = pInterfaceMD;
                                bmtVT->pInteropData->GetData(pInterfaceMD)->wSlot = pInterfaceMD->GetSlot();
                                bmtInterface->ppInterfaceDeclMethodDescList[it.GetSlotNumber()] = NULL;
                            }
                        }
                    }
                    else
                    {
                        // Found as declared method in class. If the interface was laid out by the parent we
                        // will be overridding their slot so our method counts do not increase. We will fold
                        // our method into our parent's interface if we have not been placed.
                        if(fParentInterface)
                        {
                            WORD dwSlot = (WORD) (pCurItfInfo->GetInteropStartSlot() + it.GetSlotNumber());
                            _ASSERTE(bmtVT->wCurrentVtableSlot > dwSlot);
                            MethodDesc *pMD = bmtMethod->ppMethodDescList[i];
                            InteropMethodTableSlotData *pMDData = bmtVT->pInteropData->GetData(pMD);
                            _ASSERTE(pMD && "Missing MethodDesc for declared method in class.");
                            if(pMDData->wSlot == MethodTable::NO_SLOT)
                            {
                                pMDData->wSlot = dwSlot;
                            }

                            // Set the slot and interop data
                            bmtVT->SetMethodDescForSlot(dwSlot, pMD);
                            bmtVT->ppSDVtable[dwSlot] = pMDData;
                            _ASSERTE( bmtVT->GetMethodDescForSlot(dwSlot) != NULL);
                            bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] = NULL;
                            bmtInterface->ppInterfaceDeclMethodDescList[it.GetSlotNumber()] = NULL;
                        }
                        else
                        {
                            bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] = bmtMethod->ppMethodDescList[i];
                            bmtInterface->ppInterfaceDeclMethodDescList[it.GetSlotNumber()] = pInterfaceMD;
                        }
                    }
                }
            }
        }

        {
            MethodTable::MethodIterator it(pInterface);
            for (; it.IsValid(); it.Next())
            {
                if (it.IsVirtual())
                {
                    // The entry can be null if the interface was previously
                    // laid out by a parent and we did not have a method
                    // that subclassed the interface.
                    if(bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()] != NULL)
                    {
                        // Get the MethodDesc which was allocated for the method
                        MethodDesc *pMD = bmtInterface->ppInterfaceMethodDescList[it.GetSlotNumber()];
                        InteropMethodTableSlotData *pMDData = bmtVT->pInteropData->GetData(pMD);

                        if (pMDData->wSlot == (WORD) MethodTable::NO_SLOT)
                        {
                            pMDData->wSlot = (WORD) bmtVT->wCurrentVtableSlot;
                        }

                        // Set the vtable slot
                        _ASSERTE(bmtVT->GetMethodDescForSlot(bmtVT->wCurrentVtableSlot) == NULL);
                        bmtVT->SetMethodDescForSlot(bmtVT->wCurrentVtableSlot, pMD);
                        _ASSERTE(bmtVT->GetMethodDescForSlot(bmtVT->wCurrentVtableSlot) != NULL);
                        bmtVT->ppSDVtable[bmtVT->wCurrentVtableSlot] = pMDData;

                        // Increment the current vtable slot
                        bmtVT->wCurrentVtableSlot++;
                    }
                }
            }
        }
    }
}

//---------------------------------------------------------------------------------------
// We should have collected all the method impls. Cycle through them creating the method impl
// structure that holds the information about which slots are overridden.
VOID MethodTableBuilder::BuildInteropVTable_PlaceMethodImpls(
        bmtTypeInfo* bmtType,
        bmtMethodImplInfo* bmtMethodImpl,
        bmtErrorInfo* bmtError,
        bmtInterfaceInfo* bmtInterface,
        bmtVtable* bmtVT,
        bmtParentInfo* bmtParent)

{
    STANDARD_VM_CONTRACT;

    if(bmtMethodImpl->pIndex == 0)
        return;

    DWORD pIndex = 0;

    // Allocate some temporary storage. The number of overrides for a single method impl
    // cannot be greater then the number of vtable slots.
    DWORD* slots = (DWORD*) new (GetStackingAllocator()) DWORD[bmtVT->wCurrentVtableSlot];
    MethodDesc **replaced = new (GetStackingAllocator()) MethodDesc*[bmtVT->wCurrentVtableSlot];

    while(pIndex < bmtMethodImpl->pIndex) {

        DWORD slotIndex = 0;
        DWORD dwItfCount = 0;
        MethodDesc* next = bmtMethodImpl->GetBodyMethodDesc(pIndex);
        MethodDesc* body = NULL;

        // The signature for the body of the method impl. We cache the signature until all
        // the method impl's using the same body are done.
        PCCOR_SIGNATURE pBodySignature = NULL;
        DWORD           cBodySignature = 0;

        // The impls are sorted according to the method descs for the body of the method impl.
        // Loop through the impls until the next body is found. When a single body
        // has been done move the slots implemented and method descs replaced into the storage
        // found on the body method desc.
        do { // collect information until we reach the next body
            body = next;

            // Get the declaration part of the method impl. It will either be a token
            // (declaration is on this type) or a method desc.
            MethodDesc* pDecl = bmtMethodImpl->GetDeclarationMethodDesc(pIndex);
            if(pDecl == NULL) {
                // The declaration is on this type to get the token.
                mdMethodDef mdef = bmtMethodImpl->GetDeclarationToken(pIndex);

                BuildInteropVTable_PlaceLocalDeclaration(mdef,
                                           body,
                                           bmtType,
                                           bmtError,
                                           bmtVT,
                                           slots,             // Adds override to the slot and replaced arrays.
                                           replaced,
                                           &slotIndex,        // Increments count
                                           &pBodySignature,   // Fills in the signature
                                           &cBodySignature);
            }
            else {
                // Method impls to methods on generic interfaces should have already
                // been filtered out.
                _ASSERTE(pDecl->GetMethodTable()->GetNumGenericArgs() == 0);

                if(pDecl->GetMethodTable()->IsInterface()) {
                    BuildInteropVTable_PlaceInterfaceDeclaration(pDecl,
                                                   body,
                                                   bmtMethodImpl->GetDeclarationSubst(pIndex),
                                                   bmtType,
                                                   bmtInterface,
                                                   bmtError,
                                                   bmtVT,
                                                   slots,
                                                   replaced,
                                                   &slotIndex,        // Increments count
                                                   &pBodySignature,   // Fills in the signature
                                                   &cBodySignature);
                }
                else {
                    BuildInteropVTable_PlaceParentDeclaration(pDecl,
                                                body,
                                                bmtMethodImpl->GetDeclarationSubst(pIndex),
                                                bmtType,
                                                bmtError,
                                                bmtVT,
                                                bmtParent,
                                                slots,
                                                replaced,
                                                &slotIndex,        // Increments count
                                                &pBodySignature,   // Fills in the signature
                                                &cBodySignature);
                }
            }

            // Move to the next body
            pIndex++;

            // we hit the end of the list so leave
            next = pIndex < bmtMethodImpl->pIndex ? bmtMethodImpl->GetBodyMethodDesc(pIndex) : NULL;
        } while(next == body) ;
    }  // while(next != NULL)
}

//---------------------------------------------------------------------------------------
VOID MethodTableBuilder::BuildInteropVTable_PlaceLocalDeclaration(
                                       mdMethodDef      mdef,
                                       MethodDesc*      body,
                                       bmtTypeInfo* bmtType,
                                       bmtErrorInfo*    bmtError,
                                       bmtVtable*       bmtVT,
                                       DWORD*           slots,
                                       MethodDesc**     replaced,
                                       DWORD*           pSlotIndex,
                                       PCCOR_SIGNATURE* ppBodySignature,
                                       DWORD*           pcBodySignature)
{
    STANDARD_VM_CONTRACT;

    // we search on the token and m_cl
    for(USHORT i = 0; i < bmtVT->wCurrentVtableSlot; i++)
    {
        // Make sure we haven't already been MethodImpl'd
        _ASSERTE(bmtVT->ppSDVtable[i]->pMD == bmtVT->ppSDVtable[i]->pDeclMD);

        // We get the current slot.  Since we are looking for a method declaration
        // that is on our class we would never match up with a method obtained from
        // one of our parents or an Interface.
        MethodDesc *pMD = bmtVT->ppSDVtable[i]->pMD;

        // If we get a null then we have already replaced this one. We can't check it
        // so we will just by by-pass this.
        if(pMD->GetMemberDef() == mdef)
        {
            InteropMethodTableSlotData *pDeclData = bmtVT->pInteropData->GetData(pMD);
            InteropMethodTableSlotData *pImplData = bmtVT->pInteropData->GetData(body);

            // If the body has not been placed then place it here. We do not
            // place bodies for method impl's until we find a spot for them.
            if (pImplData->wSlot == MethodTable::NO_SLOT)
            {
                pImplData->wSlot = (WORD) i;
            }

            // We implement this slot, record it
            slots[*pSlotIndex] = i;
            replaced[*pSlotIndex] = pMD;
            bmtVT->SetMethodDescForSlot(i, body);
            pDeclData->pMD = pImplData->pMD;
            pDeclData->wSlot = pImplData->wSlot;
            bmtVT->ppSDVtable[i] = pDeclData;

            // increment the counter
            (*pSlotIndex)++;
        }
    }
}

//---------------------------------------------------------------------------------------
VOID MethodTableBuilder::BuildInteropVTable_PlaceInterfaceDeclaration(
                                           MethodDesc*       pItfDecl,
                                           MethodDesc*       pImplBody,
                                           const Substitution *pDeclSubst,
                                           bmtTypeInfo*  bmtType,
                                           bmtInterfaceInfo* bmtInterface,
                                           bmtErrorInfo*     bmtError,
                                           bmtVtable*        bmtVT,
                                           DWORD*            slots,
                                           MethodDesc**      replaced,
                                           DWORD*            pSlotIndex,
                                           PCCOR_SIGNATURE*  ppBodySignature,
                                           DWORD*            pcBodySignature)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pItfDecl && pItfDecl->IsInterface() && !(pItfDecl->IsMethodImpl()));

    // the fact that an interface only shows up once in the vtable
    // When we are looking for a method desc then the declaration is on
    // some class or interface that this class implements. The declaration
    // will either be to an interface or to a class. If it is to a
    // interface then we need to search for that interface. From that
    // slot number of the method in the interface we can calculate the offset
    // into our vtable. If it is to a class it must be a subclass. This uses
    // the fact that an interface only shows up once in the vtable.

    BOOL fInterfaceFound = FALSE;
    // Check our vtable for entries that we are suppose to override.
    // Since this is an external method we must also check the interface map.
    // We want to replace any interface methods even if they have been replaced
    // by a base class.
    for(USHORT i = 0; i < bmtInterface->wInterfaceMapSize; i++)
    {
        MethodTable *pInterface = bmtInterface->pInterfaceMap[i].m_pMethodTable;

        if (pInterface->IsEquivalentTo(pItfDecl->GetMethodTable()))
        {
            // We found an interface so no error
            fInterfaceFound = TRUE;

            WORD wSlot = (WORD) -1;
            MethodDesc *pMD = NULL;

            // Find out where the interface map is set on our vtable
            WORD wStartingSlot = (USHORT) bmtInterface->pInterfaceMap[i].GetInteropStartSlot();

            // We need to duplicate the interface to avoid copies. Currently, interfaces
            // do not overlap so we just need to check to see if there is a non-duplicated
            // MD. If there is then the interface shares it with the class which means
            // we need to copy the whole interface
            for(wSlot = wStartingSlot; wSlot < pInterface->GetNumVirtuals() + wStartingSlot; wSlot++)
            {
                // This check will tell us if the method in this slot is the first instance (not a duplicate)
                if(bmtVT->ppSDVtable[wSlot]->wSlot == wSlot)
                    break;
            }

            if(wSlot < pInterface->GetNumVirtuals() + wStartingSlot)
            {
                // Check to see if we have allocated the temporay array of starting values.
                // This array is used to backpatch entries to the original location. These
                // values are never used but will cause problems later when we finish
                // laying out the method table.
                if(bmtInterface->pdwOriginalStart == NULL)
                {
                    bmtInterface->pdwOriginalStart = new (GetStackingAllocator()) DWORD[bmtInterface->dwMaxExpandedInterfaces];
                    memset(bmtInterface->pdwOriginalStart, 0, sizeof(DWORD)*bmtInterface->dwMaxExpandedInterfaces);
                }

                _ASSERTE(bmtInterface->pInterfaceMap[i].GetInteropStartSlot() != (WORD) 0 && "We assume that an interface does not start at position 0");
                _ASSERTE(bmtInterface->pdwOriginalStart[i] == 0 && "We should not move an interface twice");
                bmtInterface->pdwOriginalStart[i] = bmtInterface->pInterfaceMap[i].GetInteropStartSlot();

                // The interface now starts at the end of the map.
                bmtInterface->pInterfaceMap[i].SetInteropStartSlot(bmtVT->wCurrentVtableSlot);
                for(WORD d = wStartingSlot; d < pInterface->GetNumVirtuals() + wStartingSlot; d++)
                {
                    // Copy the MD
                    //@TODO: Maybe need to create new slot data entries for this copy-out based on
                    //@TODO: the MD's of the interface slots.
                    InteropMethodTableSlotData *pDataCopy = bmtVT->ppSDVtable[d];
                    bmtVT->SetMethodDescForSlot(bmtVT->wCurrentVtableSlot, pDataCopy->pMD);
                    bmtVT->ppSDVtable[bmtVT->wCurrentVtableSlot] = pDataCopy;
                    // Increment the various counters
                    bmtVT->wCurrentVtableSlot++;
                }
                // Reset the starting slot to the known value
                wStartingSlot = bmtInterface->pInterfaceMap[i].GetInteropStartSlot();
            }

            // Make sure we have placed the interface map.
            _ASSERTE(wStartingSlot != MethodTable::NO_SLOT);

            // Get the Slot location of the method desc (slot of the itf MD + start slot for this class)
            wSlot = pItfDecl->GetSlot() + wStartingSlot;
            _ASSERTE(wSlot < bmtVT->wCurrentVtableSlot);

            // Get our current method desc for this slot
            pMD = bmtVT->ppSDVtable[wSlot]->pMD;

            // If we have not got the method impl signature go get it now. It is cached
            // in our caller
            if (*ppBodySignature == NULL)
            {
                if (FAILED(bmtType->pMDImport->GetSigOfMethodDef(
                    pImplBody->GetMemberDef(),
                    pcBodySignature,
                    ppBodySignature)))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
                }
            }

            InteropMethodTableSlotData *pImplSlotData = bmtVT->pInteropData->GetData(pImplBody);
            _ASSERTE(pImplSlotData->wSlot != MethodTable::NO_SLOT);
            // If the body has not been placed then place it now.
            if (pImplSlotData->wSlot == MethodTable::NO_SLOT)
            {
                pImplSlotData->wSlot = wSlot;
            }

            // Store away the values
            InteropMethodTableSlotData *pItfSlotData = bmtVT->pInteropData->GetData(pItfDecl);
            slots[*pSlotIndex] = wSlot;
            replaced[*pSlotIndex] = pItfDecl;
            bmtVT->SetMethodDescForSlot(wSlot, pImplBody);
            pItfSlotData->pMD = pImplBody;
            pItfSlotData->wSlot = pImplSlotData->wSlot;
            bmtVT->ppSDVtable[wSlot] = pItfSlotData;

            // increment the counter
            (*pSlotIndex)++;

            // if we have moved the interface we need to back patch the original location
            // if we had left an interface place holder.
            if(bmtInterface->pdwOriginalStart && bmtInterface->pdwOriginalStart[i] != 0)
            {
                USHORT slot = (USHORT) bmtInterface->pdwOriginalStart[i] + pItfDecl->GetSlot();
                MethodDesc* pSlotMD = bmtVT->ppSDVtable[slot]->pMD;
                if(pSlotMD->GetMethodTable() && pSlotMD->IsInterface())
                {
                    bmtVT->SetMethodDescForSlot(slot, pImplBody);
                    bmtVT->ppSDVtable[slot] = pItfSlotData;
                }
            }
            break;
        }
    }

    _ASSERTE(fInterfaceFound);
}

//---------------------------------------------------------------------------------------
VOID MethodTableBuilder::BuildInteropVTable_PlaceParentDeclaration(
                                        MethodDesc*       pDecl,
                                        MethodDesc*       pImplBody,
                                        const Substitution *pDeclSubst,
                                        bmtTypeInfo*  bmtType,
                                        bmtErrorInfo*     bmtError,
                                        bmtVtable*        bmtVT,
                                        bmtParentInfo*    bmtParent,
                                        DWORD*            slots,
                                        MethodDesc**      replaced,
                                        DWORD*            pSlotIndex,
                                        PCCOR_SIGNATURE*  ppBodySignature,
                                        DWORD*            pcBodySignature)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pDecl && !pDecl->IsInterface());

    BOOL fRet = FALSE;

    // Verify that the class of the declaration is in our hierarchy
    MethodTable* declType = pDecl->GetMethodTable();
    MethodTable* pParentMT = bmtParent->pParentMethodTable;
    while(pParentMT != NULL)
    {

        if(declType == pParentMT)
            break;
        pParentMT = pParentMT->GetParentMethodTable();
    }
    _ASSERTE(pParentMT);

    // Compare the signature for the token in the specified scope
    // If we have not got the method impl signature go get it now
    if (*ppBodySignature == NULL)
    {
        if (FAILED(bmtType->pMDImport->GetSigOfMethodDef(
            pImplBody->GetMemberDef(),
            pcBodySignature,
            ppBodySignature)))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
        }
    }

    // We get the method from the parents slot. We will replace the method that is currently
    // defined in that slot and any duplicates for that method desc.
    WORD wSlot = InteropMethodTableData::GetSlotForMethodDesc(pParentMT, pDecl);
    InteropMethodTableSlotData *pDeclData = bmtVT->ppSDVtable[wSlot];
    InteropMethodTableSlotData *pImplData = bmtVT->pInteropData->GetData(pImplBody);

    // Get the real method desc (a base class may have overridden the method
    // with a method impl)
    MethodDesc* pReplaceDesc = pDeclData->pDeclMD;

    // If the body has not been placed then place it here
    if(pImplData->wSlot == MethodTable::NO_SLOT)
    {
        pImplData->wSlot = wSlot;
    }

    slots[*pSlotIndex] = wSlot;
    replaced[*pSlotIndex] = pReplaceDesc;
    bmtVT->SetMethodDescForSlot(wSlot, pImplBody);
    pDeclData->pMD = pImplData->pMD;
    pDeclData->wSlot = pImplData->wSlot;
    bmtVT->ppSDVtable[wSlot] = pDeclData;

    // increment the counter
    (*pSlotIndex)++;

    // we search for all duplicates
    for(USHORT i = wSlot+1; i < bmtVT->wCurrentVtableSlot; i++)
    {
        MethodDesc *pMD = bmtVT->ppSDVtable[i]->pMD;

        MethodDesc* pRealDesc = bmtVT->ppSDVtable[i]->pDeclMD;

        if(pRealDesc == pReplaceDesc)
        {
            // We do not want to override a body to another method impl
            _ASSERTE(!pRealDesc->IsMethodImpl());

            // Make sure we are not overridding another method impl
            _ASSERTE(!(pMD != pImplBody && pMD->IsMethodImpl() && pMD->GetMethodTable() == NULL));

            slots[*pSlotIndex] = i;
            replaced[*pSlotIndex] = pRealDesc;
            bmtVT->pVtable[i] = bmtVT->pVtable[wSlot];
            bmtVT->pVtableMD[i] = bmtVT->pVtableMD[wSlot];
            bmtVT->ppSDVtable[i] = bmtVT->ppSDVtable[wSlot];

            // increment the counter
            (*pSlotIndex)++;
        }
    }
}

//---------------------------------------------------------------------------------------
VOID   MethodTableBuilder::BuildInteropVTable_PropagateInheritance(
    bmtVtable *bmtVT)
{
    STANDARD_VM_CONTRACT;

    for (DWORD i = 0; i < bmtVT->wCurrentVtableSlot; i++)
    {
        // For now only propagate inheritance for method desc that are not interface MD's.
        // This is not sufficient but InterfaceImpl's will complete the picture.
        InteropMethodTableSlotData *pMDData = bmtVT->ppSDVtable[i];
        MethodDesc* pMD = pMDData->pMD;
        CONSISTENCY_CHECK_MSG(CheckPointer(pMD), "Could not resolve MethodDesc Slot!");

        if(!pMD->IsInterface() && pMDData->GetSlot() != i)
        {
            pMDData->SetDuplicate();
            bmtVT->pVtable[i] = bmtVT->pVtable[pMDData->GetSlot()];
            bmtVT->pVtableMD[i] = bmtVT->pVtableMD[pMDData->GetSlot()];
            bmtVT->ppSDVtable[i]->pMD = bmtVT->ppSDVtable[pMDData->GetSlot()]->pMD;
        }
    }
}


//---------------------------------------------------------------------------------------
VOID   MethodTableBuilder::FinalizeInteropVTable(
        AllocMemTracker *pamTracker,
        LoaderAllocator* pAllocator,
        bmtVtable* bmtVT,
        bmtInterfaceInfo* bmtInterface,
        bmtTypeInfo* bmtType,
        bmtProperties* bmtProp,
        bmtMethodInfo* bmtMethod,
        bmtErrorInfo* bmtError,
        bmtParentInfo* bmtParent,
        InteropMethodTableData **ppInteropMT)
{
    STANDARD_VM_CONTRACT;

    LoaderHeap *pHeap = pAllocator->GetLowFrequencyHeap();

    // Allocate the overall structure
    InteropMethodTableData *pMTData = (InteropMethodTableData *) pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InteropMethodTableData))));
#ifdef LOGGING
    g_sdStats.m_cbComInteropData += sizeof(InteropMethodTableData);
#endif
    memset(pMTData, 0, sizeof(InteropMethodTableData));

    // Allocate the vtable
    pMTData->cVTable = bmtVT->wCurrentVtableSlot;
    if (pMTData->cVTable != 0)
    {
        pMTData->pVTable = (InteropMethodTableSlotData *)
            pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InteropMethodTableSlotData)) * S_SIZE_T(pMTData->cVTable)));
#ifdef LOGGING
        g_sdStats.m_cbComInteropData += sizeof(InteropMethodTableSlotData) * pMTData->cVTable;
#endif

        {   // Copy the vtable
            for (DWORD i = 0; i < pMTData->cVTable; i++)
            {
                CONSISTENCY_CHECK(bmtVT->ppSDVtable[i]->wSlot != MethodTable::NO_SLOT);
                pMTData->pVTable[i] = *bmtVT->ppSDVtable[i];
            }
        }
    }

    // Allocate the non-vtable
    pMTData->cNonVTable = bmtVT->wCurrentNonVtableSlot;
    if (pMTData->cNonVTable != 0)
    {
        pMTData->pNonVTable = (InteropMethodTableSlotData *)
            pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InteropMethodTableSlotData)) * S_SIZE_T(pMTData->cNonVTable)));
#ifdef LOGGING
        g_sdStats.m_cbComInteropData += sizeof(InteropMethodTableSlotData) * pMTData->cNonVTable;
#endif

        {   // Copy the non-vtable
            for (DWORD i = 0; i < pMTData->cNonVTable; i++)
            {
                CONSISTENCY_CHECK(bmtVT->ppSDVtable[i]->wSlot != MethodTable::NO_SLOT);
                pMTData->pNonVTable[i] = *bmtVT->ppSDNonVtable[i];
            }
        }
    }

    // Allocate the interface map
    pMTData->cInterfaceMap = bmtInterface->wInterfaceMapSize;
    if (pMTData->cInterfaceMap != 0)
    {
        pMTData->pInterfaceMap = (InterfaceInfo_t *)
            pamTracker->Track(pHeap->AllocMem(S_SIZE_T(sizeof(InterfaceInfo_t)) * S_SIZE_T(pMTData->cInterfaceMap)));
#ifdef LOGGING
        g_sdStats.m_cbComInteropData += sizeof(InterfaceInfo_t) * pMTData->cInterfaceMap;
#endif

        {   // Copy the interface map
            for (DWORD i = 0; i < pMTData->cInterfaceMap; i++)
            {
                pMTData->pInterfaceMap[i] = bmtInterface->pInterfaceMap[i];
            }
        }
    }

    *ppInteropMT = pMTData;
}

//*******************************************************************************
VOID    MethodTableBuilder::EnumerateMethodImpls()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    IMDInternalImport *pMDInternalImport = bmtType->pMDImport;
    DWORD rid, maxRidMD, maxRidMR;
    hr = bmtMethodImpl->hEnumMethodImpl.EnumMethodImplInitNoThrow(GetCl());

    if (FAILED(hr))
    {
        BuildMethodTableThrowException(hr, *bmtError);
    }

    // This gets the count out of the metadata interface.
    bmtMethodImpl->dwNumberMethodImpls = bmtMethodImpl->hEnumMethodImpl.EnumMethodImplGetCount();

    // This is the first pass. In this we will simply enumerate the token pairs and fill in
    // the data structures. In addition, we'll sort the list and eliminate duplicates.
    if (bmtMethodImpl->dwNumberMethodImpls > 0)
    {
        //
        // Allocate the structures to keep track of the token pairs
        //
        bmtMethodImpl->rgMethodImplTokens = new (GetStackingAllocator())
            bmtMethodImplInfo::MethodImplTokenPair[bmtMethodImpl->dwNumberMethodImpls];

        // Iterate through each MethodImpl declared on this class
        for (DWORD i = 0; i < bmtMethodImpl->dwNumberMethodImpls; i++)
        {
            // Grab the next set of body/decl tokens
            hr = bmtMethodImpl->hEnumMethodImpl.EnumMethodImplNext(
                &bmtMethodImpl->rgMethodImplTokens[i].methodBody,
                &bmtMethodImpl->rgMethodImplTokens[i].methodDecl);
            if (FAILED(hr))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
            }
            if (hr == S_FALSE)
            {
                // In the odd case that the enumerator fails before we've reached the total reported
                // entries, let's reset the count and just break out. (Should we throw?)
                bmtMethodImpl->dwNumberMethodImpls = i;
                break;
            }
        }

        // No need to do any sorting or duplicate elimination if there's not two or more methodImpls
        if (bmtMethodImpl->dwNumberMethodImpls > 1)
        {
            // Now sort
            qsort(bmtMethodImpl->rgMethodImplTokens,
                  bmtMethodImpl->dwNumberMethodImpls,
                  sizeof(bmtMethodImplInfo::MethodImplTokenPair),
                  &bmtMethodImplInfo::MethodImplTokenPair::Compare);

            // Now eliminate duplicates
            for (DWORD i = 0; i < bmtMethodImpl->dwNumberMethodImpls - 1; i++)
            {
                CONSISTENCY_CHECK((i + 1) < bmtMethodImpl->dwNumberMethodImpls);

                bmtMethodImplInfo::MethodImplTokenPair *e1 = &bmtMethodImpl->rgMethodImplTokens[i];
                bmtMethodImplInfo::MethodImplTokenPair *e2 = &bmtMethodImpl->rgMethodImplTokens[i + 1];

                // If the pair are equal, eliminate the first one, and reduce the total count by one.
                if (bmtMethodImplInfo::MethodImplTokenPair::Equal(e1, e2))
                {
                    DWORD dwCopyNum = bmtMethodImpl->dwNumberMethodImpls - (i + 1);
                    memcpy(e1, e2, dwCopyNum * sizeof(bmtMethodImplInfo::MethodImplTokenPair));
                    bmtMethodImpl->dwNumberMethodImpls--;
                    CONSISTENCY_CHECK(bmtMethodImpl->dwNumberMethodImpls > 0);
                }
            }
        }
    }

    if (bmtMethodImpl->dwNumberMethodImpls != 0)
    {
        //
        // Allocate the structures to keep track of the impl matches
        //
        bmtMethodImpl->pMethodDeclSubsts = new (GetStackingAllocator()) Substitution[bmtMethodImpl->dwNumberMethodImpls];
        bmtMethodImpl->rgEntries = new (GetStackingAllocator()) bmtMethodImplInfo::Entry[bmtMethodImpl->dwNumberMethodImpls];

        // These are used for verification
        maxRidMD = pMDInternalImport->GetCountWithTokenKind(mdtMethodDef);
        maxRidMR = pMDInternalImport->GetCountWithTokenKind(mdtMemberRef);

        // Iterate through each MethodImpl declared on this class
        for (DWORD i = 0; i < bmtMethodImpl->dwNumberMethodImpls; i++)
        {
            PCCOR_SIGNATURE pSigDecl = NULL;
            PCCOR_SIGNATURE pSigBody = NULL;
            ULONG           cbSigDecl;
            ULONG           cbSigBody;
            mdToken tkParent;

            mdToken theBody, theDecl;
            Substitution theDeclSubst(bmtType->pModule, SigPointer(), NULL); // this can get updated later below.

            theBody = bmtMethodImpl->rgMethodImplTokens[i].methodBody;
            theDecl = bmtMethodImpl->rgMethodImplTokens[i].methodDecl;

            // IMPLEMENTATION LIMITATION: currently, we require that the body of a methodImpl
            // belong to the current type. This is because we need to allocate a different
            // type of MethodDesc for bodies that are part of methodImpls.
            if (TypeFromToken(theBody) != mdtMethodDef)
            {
                mdToken theNewBody;
                hr = FindMethodDeclarationForMethodImpl(bmtType->pMDImport,
                                                        GetCl(),
                                                        theBody,
                                                        &theNewBody);
                if (FAILED(hr))
                {
                    BuildMethodTableThrowException(hr, IDS_CLASSLOAD_MI_ILLEGAL_BODY, mdMethodDefNil);
                }
                theBody = theNewBody;

                // Make sure to update the stored token with the resolved token.
                bmtMethodImpl->rgMethodImplTokens[i].methodBody = theBody;
            }

            if (TypeFromToken(theBody) != mdtMethodDef)
            {
                BuildMethodTableThrowException(BFA_METHODDECL_NOT_A_METHODDEF);
            }
            CONSISTENCY_CHECK(theBody == bmtMethodImpl->rgMethodImplTokens[i].methodBody);

            //
            // Now that the tokens of Decl and Body are obtained, do the MD validation
            //

            rid = RidFromToken(theDecl);

            // Perform initial rudimentary validation of the token. Full token verification
            // will be done in TestMethodImpl when placing the methodImpls.
            if (TypeFromToken(theDecl) == mdtMethodDef)
            {
                // Decl must be valid token
                if ((rid == 0)||(rid > maxRidMD))
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

                theDeclSubst = Substitution(tkParent, bmtType->pModule, NULL);
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
            if ((pSigDecl != NULL) && (cbSigDecl != 0))
            {
                if (FAILED(pMDInternalImport->GetSigOfMethodDef(theBody,&cbSigBody, &pSigBody)) ||
                    (pSigBody == NULL) ||
                    (cbSigBody == 0))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MISSING_SIG_BODY);
                }
                // Can't use memcmp because there may be two AssemblyRefs
                // in this scope, pointing to the same assembly, etc.).
                if (!MetaSig::CompareMethodSigs(pSigDecl,
                                                cbSigDecl,
                                                bmtType->pModule,
                                                &theDeclSubst,
                                                pSigBody,
                                                cbSigBody,
                                                bmtType->pModule,
                                                NULL,
                                                FALSE))
                {
                    BuildMethodTableThrowException(IDS_CLASSLOAD_MI_BODY_DECL_MISMATCH);
                }
            }
            else
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_MI_MISSING_SIG_DECL);
            }

            bmtMethodImpl->pMethodDeclSubsts[i] = theDeclSubst;

        }
    }
}

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Enumerate this class's members
//
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
VOID    MethodTableBuilder::EnumerateClassMethods()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(bmtType));
        PRECONDITION(CheckPointer(bmtMethod));
        PRECONDITION(CheckPointer(bmtProp));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtError));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD i;
    IMDInternalImport *pMDInternalImport = bmtType->pMDImport;
    mdToken tok;
    DWORD dwMemberAttrs;
    BOOL fIsClassEnum = IsEnum();
    BOOL fIsClassInterface = IsInterface();
    BOOL fIsClassValueType = IsValueClass();
#ifdef FEATURE_COMINTEROP
    BOOL fIsClassComImport = IsComImport();
#endif
    BOOL fIsClassNotAbstract = (IsTdAbstract(GetAttrClass()) == 0);
    PCCOR_SIGNATURE pMemberSignature;
    ULONG           cMemberSignature;

    //
    // Run through the method list and calculate the following:
    // # methods.
    // # "other" methods (i.e. static or private)
    // # non-other methods
    //

    bmtVT->dwMaxVtableSize     = 0; // we'll fix this later to be the real upper bound on vtable size
    bmtMethod->cMethods = 0;

    hr = bmtMethod->hEnumMethod.EnumInitNoThrow(mdtMethodDef, GetCl());
    if (FAILED(hr))
    {
        _ASSERTE(!"Cannot count memberdefs");
        if (FAILED(hr))
        {
            BuildMethodTableThrowException(hr, *bmtError);
        }
    }

    // Allocate an array to contain the method tokens as well as information about the methods.
    bmtMethod->cMethAndGaps = bmtMethod->hEnumMethod.EnumGetCount();

    bmtMethod->rgMethodTokens = new (GetStackingAllocator()) mdToken[bmtMethod->cMethAndGaps];
    bmtMethod->rgMethodRVA = new (GetStackingAllocator()) ULONG[bmtMethod->cMethAndGaps];
    bmtMethod->rgMethodAttrs = new (GetStackingAllocator()) DWORD[bmtMethod->cMethAndGaps];
    bmtMethod->rgMethodImplFlags = new (GetStackingAllocator()) DWORD[bmtMethod->cMethAndGaps];
    bmtMethod->rgMethodClassifications = new (GetStackingAllocator()) DWORD[bmtMethod->cMethAndGaps];

    bmtMethod->rgszMethodName = new (GetStackingAllocator()) LPCSTR[bmtMethod->cMethAndGaps];

    bmtMethod->rgMethodImpl = new (GetStackingAllocator()) BYTE[bmtMethod->cMethAndGaps];
    bmtMethod->rgMethodType = new (GetStackingAllocator()) BYTE[bmtMethod->cMethAndGaps];

    enum { SeenCtor = 1, SeenInvoke = 2, SeenBeginInvoke = 4, SeenEndInvoke = 8};
    unsigned delegateMethodsSeen = 0;

    for (i = 0; i < bmtMethod->cMethAndGaps; i++)
    {
        ULONG dwMethodRVA;
        DWORD dwImplFlags;
        DWORD Classification;
        LPSTR strMethodName;

        //
        // Go to the next method and retrieve its attributes.
        //

        bmtMethod->hEnumMethod.EnumNext(&tok);
        DWORD   rid = RidFromToken(tok);
        if ((rid == 0)||(rid > pMDInternalImport->GetCountWithTokenKind(mdtMethodDef)))
        {
            BuildMethodTableThrowException(BFA_METHOD_TOKEN_OUT_OF_RANGE);
        }

        if (FAILED(pMDInternalImport->GetMethodDefProps(tok, &dwMemberAttrs)))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
        }
        if (IsMdRTSpecialName(dwMemberAttrs) || IsMdVirtual(dwMemberAttrs) || IsDelegate())
        {
            if (FAILED(pMDInternalImport->GetNameOfMethodDef(tok, (LPCSTR *)&strMethodName)))
            {
                BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
            }
            if (IsStrLongerThan(strMethodName,MAX_CLASS_NAME))
            {
                BuildMethodTableThrowException(BFA_METHOD_NAME_TOO_LONG);
            }
        }
        else
            strMethodName = NULL;

        HENUMInternalHolder hEnumTyPars(pMDInternalImport);
        hr = hEnumTyPars.EnumInitNoThrow(mdtGenericParam, tok);
        if (FAILED(hr))
        {
            BuildMethodTableThrowException(hr, *bmtError);
        }

        WORD numGenericMethodArgs = (WORD) hEnumTyPars.EnumGetCount();

        if (numGenericMethodArgs != 0)
        {
            for (unsigned methIdx = 0; methIdx < numGenericMethodArgs; methIdx++)
            {
                mdGenericParam tkTyPar;
                pMDInternalImport->EnumNext(&hEnumTyPars, &tkTyPar);
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

        //
        // We need to check if there are any gaps in the vtable. These are
        // represented by methods with the mdSpecial flag and a name of the form
        // _VTblGap_nnn (to represent nnn empty slots) or _VTblGap (to represent a
        // single empty slot).
        //

        if (IsMdRTSpecialName(dwMemberAttrs))
        {
            PREFIX_ASSUME(strMethodName != NULL); // if we've gotten here we've called GetNameOfMethodDef

            // The slot is special, but it might not be a vtable spacer. To
            // determine that we must look at the name.
            if (strncmp(strMethodName, "_VtblGap", 8) == 0)
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
                    n = 1;
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
                // Record vtable gap in mapping list.
                if (GetHalfBakedClass()->GetSparseCOMInteropVTableMap() == NULL)
                    GetHalfBakedClass()->SetSparseCOMInteropVTableMap(new SparseVTableMap());

                GetHalfBakedClass()->GetSparseCOMInteropVTableMap()->RecordGap(NumDeclaredMethods(), n);

                bmtProp->fSparse = true;
#endif // FEATURE_COMINTEROP
                continue;
            }

        }


        //
        // This is a real method so add it to the enumeration of methods. We now need to retrieve
        // information on the method and store it for later use.
        //
        if (FAILED(pMDInternalImport->GetMethodImplProps(tok, &dwMethodRVA, &dwImplFlags)))
        {
            BuildMethodTableThrowException(BFA_INVALID_TOKEN);
        }
        //
        // But first - minimal flags validity checks
        //
        // No methods in Enums!
        if (fIsClassEnum)
        {
            BuildMethodTableThrowException(BFA_METHOD_IN_A_ENUM);
        }
        // RVA : 0
        if (dwMethodRVA != 0)
        {
#ifdef FEATURE_COMINTEROP
            if(fIsClassComImport)
            {
                BuildMethodTableThrowException(BFA_METHOD_WITH_NONZERO_RVA);
            }
#endif // FEATURE_COMINTEROP
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
            if(!IsMdVirtual(dwMemberAttrs))
            {
                BuildMethodTableThrowException(BFA_NONVIRT_AB_METHOD);
            }
        }
        else if(fIsClassInterface && strMethodName &&
                (strcmp(strMethodName, COR_CCTOR_METHOD_NAME)))
        {
            BuildMethodTableThrowException(BFA_NONAB_NONCCTOR_METHOD_ON_INT);
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
                BuildMethodTableThrowException(BFA_VIRTUAL_STATIC_METHOD);
            }
            if(strMethodName && (0==strcmp(strMethodName, COR_CTOR_METHOD_NAME)))
            {
                BuildMethodTableThrowException(BFA_VIRTUAL_INSTANCE_CTOR);
            }
        }

        // Some interface checks.
        // We only need them if default interface method support is disabled
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
        // may not be part of a COM Import class, PInvoke, internal call.
        if ((numGenericMethodArgs != 0) &&
            (
#ifdef FEATURE_COMINTEROP
             fIsClassComImport ||
             bmtProp->fComEventItfType ||
#endif // FEATURE_COMINTEROP
             IsMdPinvokeImpl(dwMemberAttrs) ||
             IsMiInternalCall(dwImplFlags)))
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

        // Signature validation
        if (FAILED(pMDInternalImport->GetSigOfMethodDef(tok,&cMemberSignature, &pMemberSignature)))
        {
            BuildMethodTableThrowException(IDS_CLASSLOAD_BADFORMAT);
        }
        hr = validateTokenSig(tok,pMemberSignature,cMemberSignature,dwMemberAttrs,pMDInternalImport);
        if (FAILED(hr))
        {
            BuildMethodTableThrowException(hr, BFA_BAD_SIGNATURE, mdMethodDefNil);
        }

        //
        // Determine the method's classification.
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
                    // tlbimported component
                    if (IsMdRTSpecialName(dwMemberAttrs))
                    {
                        // constructor is special
                        Classification = mcFCall;
                    }
                    else
                    {
                        // Tlbimported components we have some
                        // method descs in the call which are just used
                        // for handling methodimpls of all interface methods
                        Classification = mcComInterop;
                    }
                }
                else
#endif // FEATURE_COMINTEROP
                if (dwMethodRVA == 0)
                    Classification = mcFCall;
                else
                    Classification = mcNDirect;
            }
            // The NAT_L attribute is present, marking this method as NDirect
            else
            {
                CONSISTENCY_CHECK(hr == S_OK);
                Classification = mcNDirect;
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
                Classification = mcFCall;
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
                Classification = mcEEImpl;
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
            Classification = mcInstantiated;
        }
        else if (fIsClassInterface)
        {
#ifdef FEATURE_COMINTEROP
            if (IsMdStatic(dwMemberAttrs))
            {
                // Static methods in interfaces need nothing special.
                Classification = mcIL;
            }
            else if (bmtProp->fIsMngStandardItf)
            {
                // If the interface is a standard managed interface then allocate space for an FCall method desc.
                Classification = mcFCall;
            }
            else if (IsMdAbstract(dwMemberAttrs))
            {
                // If COM interop is supported then all other interface MDs may be
                // accessed via COM interop <TODO> mcComInterop MDs are BIG -
                // this is very often a waste of space </TODO>
                // @DIM_TODO - What if default interface method is called through COM interop?
                Classification = mcComInterop;
            }
            else
#endif // !FEATURE_COMINTEROP
            {
                // This codepath is used by remoting and default interface methods
                Classification = mcIL;
            }
        }
        else
        {
            Classification = mcIL;
        }

        // Generic methods should always be mcInstantiated
        if (!((numGenericMethodArgs == 0) || ((Classification & mdcClassification) == mcInstantiated)))
        {
            BuildMethodTableThrowException(BFA_GENERIC_METHODS_INST);
        }
        // count how many overrides this method does All methods bodies are defined
        // on this type so we can just compare the tok with the body token found
        // from the overrides.
        for(DWORD impls = 0; impls < bmtMethodImpl->dwNumberMethodImpls; impls++) {
            if ((bmtMethodImpl->rgMethodImplTokens[impls].methodBody == tok) && !IsMdStatic(dwMemberAttrs)) {
                Classification |= mdcMethodImpl;
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
        // Compute the type & other info
        //

        // Set the index into the storage locations
        BYTE impl;
        if (Classification & mdcMethodImpl)
        {
            impl = METHOD_IMPL;
        }
        else
        {
            impl = METHOD_IMPL_NOT;
        }

        BYTE type;
        if ((Classification & mdcClassification)  == mcNDirect)
        {
            type = METHOD_TYPE_NDIRECT;
        }
        else if ((Classification & mdcClassification) == mcFCall)
        {
            type = METHOD_TYPE_FCALL;
        }
        else if ((Classification & mdcClassification) == mcEEImpl)
        {
            type = METHOD_TYPE_EEIMPL;
        }
#ifdef FEATURE_COMINTEROP
        else if ((Classification & mdcClassification) == mcComInterop)
        {
            type = METHOD_TYPE_INTEROP;
        }
#endif // FEATURE_COMINTEROP
        else if ((Classification & mdcClassification) == mcInstantiated)
        {
            type = METHOD_TYPE_INSTANTIATED;
        }
        else
        {
            type = METHOD_TYPE_NORMAL;
        }

        //
        // Store the method and the information we have gathered on it in the metadata info structure.
        //

        bmtMethod->SetMethodData(NumDeclaredMethods(),
                                 tok,
                                 dwMemberAttrs,
                                 dwMethodRVA,
                                 dwImplFlags,
                                 Classification,
                                 strMethodName,
                                 impl,
                                 type);

        IncNumDeclaredMethods();

        //
        // Update the count of the various types of methods.
        //

        bmtVT->dwMaxVtableSize++;
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

    if (i != bmtMethod->cMethAndGaps)
    {
        BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT, IDS_CLASSLOAD_BAD_METHOD_COUNT, mdTokenNil);
    }

    bmtMethod->hEnumMethod.EnumReset();

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
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*******************************************************************************
//
// Used by BuildMethodTable
//
// Determines the maximum size of the vtable and allocates the temporary storage arrays
// Also copies the parent's vtable into the working vtable.
//
VOID    MethodTableBuilder::AllocateMethodWorkingMemory()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtMethod));
        PRECONDITION(CheckPointer(bmtVT));
        PRECONDITION(CheckPointer(bmtInterface));
        PRECONDITION(CheckPointer(bmtParent));

    }
    CONTRACTL_END;

    DWORD i;
    // Allocate a MethodDesc* for each method (needed later when doing interfaces), and a FieldDesc* for each field
    bmtMethod->ppMethodDescList = new (GetStackingAllocator()) MethodDesc*[NumDeclaredMethods()];
    ZeroMemory(bmtMethod->ppMethodDescList, NumDeclaredMethods() * sizeof(MethodDesc *));

    // Create a temporary function table (we don't know how large the vtable will be until the very end,
    // since duplicated interfaces are stored at the end of it).  Calculate an upper bound.
    //
    // Upper bound is: The parent's class vtable size, plus every method declared in
    //                 this class, plus the size of every interface we implement
    //
    // In the case of value classes, we add # InstanceMethods again, since we have boxed and unboxed versions
    // of every vtable method.
    //
    if (IsValueClass())
    {
        bmtVT->dwMaxVtableSize += NumDeclaredMethods();
        bmtMethod->ppUnboxMethodDescList = new (GetStackingAllocator()) MethodDesc*[NumDeclaredMethods()];
        ZeroMemory(bmtMethod->ppUnboxMethodDescList, NumDeclaredMethods() * sizeof(MethodDesc*));
    }

    // sanity check
    _ASSERTE(bmtParent->pParentMethodTable == NULL ||
             (bmtInterface->wInterfaceMapSize - bmtParent->pParentMethodTable->GetNumInterfaces()) >= 0);

    // add parent vtable size
    bmtVT->dwMaxVtableSize += bmtVT->wCurrentVtableSlot;

    for (i = 0; i < bmtInterface->wInterfaceMapSize; i++)
    {
        // We double the interface size because we may end up duplicating the Interface for MethodImpls
        bmtVT->dwMaxVtableSize += (bmtInterface->pInterfaceMap[i].m_pMethodTable->GetNumVirtuals() * 2);
    }

    // Allocate the temporary vtable
    bmtVT->pVtable = new (GetStackingAllocator())PCODE [bmtVT->dwMaxVtableSize];
    ZeroMemory(bmtVT->pVtable, bmtVT->dwMaxVtableSize * sizeof(PCODE));
    bmtVT->pVtableMD = new (GetStackingAllocator()) MethodDesc*[bmtVT->dwMaxVtableSize];
    ZeroMemory(bmtVT->pVtableMD, bmtVT->dwMaxVtableSize * sizeof(MethodDesc*));

    // Allocate the temporary non-vtable
    bmtVT->pNonVtableMD = new (GetStackingAllocator()) MethodDesc*[NumDeclaredMethods()];
    ZeroMemory(bmtVT->pNonVtableMD, sizeof(MethodDesc*) * NumDeclaredMethods());

    if (bmtParent->pParentMethodTable != NULL)
    {
        // Copy parent's vtable into our "temp" vtable
        {
            MethodTable::MethodIterator it(bmtParent->pParentMethodTable);
            for (;it.IsValid() && it.IsVirtual(); it.Next()) {
                DWORD slot = it.GetSlotNumber();
                bmtVT->pVtable[slot] = it.GetTarget().GetTarget();
                bmtVT->pVtableMD[slot] = NULL; // MethodDescs are resolved lazily
            }
            bmtVT->pParentMethodTable = bmtParent->pParentMethodTable;
        }
    }

    if (NumDeclaredMethods() > 0)
    {
        bmtParent->ppParentMethodDescBuf = (MethodDesc **)
            GetStackingAllocator()->Alloc(S_UINT32(2) * S_UINT32(NumDeclaredMethods()) *
                                          S_UINT32(sizeof(MethodDesc*)));

        bmtParent->ppParentMethodDescBufPtr = bmtParent->ppParentMethodDescBuf;
    }
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
HRESULT MethodTableBuilder::LoaderFindMethodInClass(
    LPCUTF8             pszMemberName,
    Module*             pModule,
    mdMethodDef         mdToken,
    MethodDesc **       ppMethodDesc,
    PCCOR_SIGNATURE *   ppMemberSignature,
    DWORD *             pcMemberSignature,
    DWORD               dwHashName,
    BOOL *              pMethodConstraintsMatch)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(bmtParent));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(ppMethodDesc));
        PRECONDITION(CheckPointer(ppMemberSignature));
        PRECONDITION(CheckPointer(pcMemberSignature));
    }
    CONTRACTL_END;

    HRESULT          hr;
    MethodHashEntry *pEntry;
    DWORD            dwNameHashValue;

    _ASSERTE(pModule);
    _ASSERTE(*ppMemberSignature == NULL);

    // No method found yet
    *ppMethodDesc = NULL;

    // Have we created a hash of all the methods in the class chain?
    if (bmtParent->pParentMethodHash == NULL)
    {
        // There may be such a method, so we will now create a hash table to reduce the pain for
        // further lookups

        // <TODO> Are we really sure that this is worth doing? </TODO>
        bmtParent->pParentMethodHash = CreateMethodChainHash(bmtParent->pParentMethodTable);
    }

    // Look to see if the method exists in the parent hash
    pEntry = bmtParent->pParentMethodHash->Lookup(pszMemberName, dwHashName);
    if (pEntry == NULL)
    {
        return S_OK; // No method by this name exists in the hierarchy
    }

    // Get signature of the method we're searching for - we will need this to verify an exact name-signature match
    IfFailRet(pModule->GetMDImport()->GetSigOfMethodDef(
        mdToken,
        pcMemberSignature,
        ppMemberSignature));

    // Hash value we are looking for in the chain
    dwNameHashValue = pEntry->m_dwHashValue;

    // We've found a method with the same name, but the signature may be different
    // Traverse the chain of all methods with this name
    while (1)
    {
        PCCOR_SIGNATURE     pHashMethodSig  = NULL;
        DWORD               cHashMethodSig  = 0;
        Substitution *      pSubst          = NULL;
        MethodDesc *        entryDesc       = pEntry->m_pDesc;
        MethodTable *       entryMT         = entryDesc->GetMethodTable();
        MethodTable *       entryCanonMT    = entryMT->GetCanonicalMethodTable();

        // If entry is in a parameterized type, its signature may need to be instantiated all the way down the chain
        // To understand why consider the following example:
        //   class C<T> { void m(T) { ...body... } }
        //   class D<T> : C<T[]> { /* inherits m with signature void m(T[]) */ }
        //   class E<T> : D<List<T>> { void m(List<T>[]) { ... body... } }
        // Now suppose that we've got the signature of E::m in our hand and are comparing it with the methoddesc for C.m
        // They're not syntactically the same but are if you instantiate "all the way up"
        // Possible optimization: don't bother constructing the substitution if the signature of pEntry is closed
        if (entryCanonMT->GetNumGenericArgs() > 0)
        {
            MethodTable *here = GetHalfBakedMethodTable();
            _ASSERTE(here->GetModule());
            MethodTable *pParent = bmtParent->pParentMethodTable;

            for (;;)
            {
                Substitution *newSubst = new Substitution;
                *newSubst = here->GetSubstitutionForParent(pSubst);
                pSubst = newSubst;

                here = pParent->GetCanonicalMethodTable();
                if (entryCanonMT == here)
                    break;
                pParent = pParent->GetParentMethodTable();
                _ASSERT(pParent != NULL);
            }
        }

        // Get sig of entry in hash chain
        entryDesc->GetSig(&pHashMethodSig, &cHashMethodSig);

        // Note instantiation info
        {
            hr = E_FAIL;
            EX_TRY
            {
                hr = MetaSig::CompareMethodSigs(*ppMemberSignature, *pcMemberSignature, pModule, NULL,
                                        pHashMethodSig, cHashMethodSig, entryDesc->GetModule(), pSubst, FALSE)
                        ? S_OK
                        : S_FALSE;
            }
            EX_CATCH_HRESULT_NO_ERRORINFO(hr);

            if (hr == S_OK)
            {   // Found a match
                *ppMethodDesc = entryDesc;
                // Check the constraints are consistent,
                // and return the result to the caller.
                // We do this here to avoid recalculating pSubst.
                *pMethodConstraintsMatch =
                    MetaSig::CompareMethodConstraints(NULL, pModule, mdToken, pSubst,
                                                      entryDesc->GetModule(),
                                                      entryDesc->GetMemberDef());
            }

            if (pSubst != NULL)
            {
                pSubst->DeleteChain();
                pSubst = NULL;
            }

            if (FAILED(hr) || hr == S_OK)
            {
                return hr;
            }
        }

        do
        {   // Advance to next item in the hash chain which has the same name
            pEntry = pEntry->m_pNext; // Next entry in the hash chain

            if (pEntry == NULL)
            {
                return S_OK; // End of hash chain, no match found
            }
        } while ((pEntry->m_dwHashValue != dwNameHashValue) || (strcmp(pEntry->m_pKey, pszMemberName) != 0));
    }

    return S_OK;
}

//*******************************************************************************
//
// Find a method declaration that must reside in the scope passed in. This method cannot be called if
// the reference travels to another scope.
//
// Protect against finding a declaration that lives within
// us (the type being created)
//

HRESULT MethodTableBuilder::FindMethodDeclarationForMethodImpl(
    IMDInternalImport * pMDInternalImport, // Scope in which tkClass and tkMethod are defined.
    mdTypeDef           tkClass,           // Type that the method def resides in
    mdToken             tkMethod,          // Token that is being located (MemberRef or MethodDef)
    mdMethodDef *       ptkMethodDef)      // Method definition for Member
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    PCCOR_SIGNATURE pSig;  // Signature of Member
    DWORD           cSig;
    LPCUTF8         szMember = NULL;
    // The token should be a member ref or def. If it is a ref then we need to travel
    // back to us hopefully.
    if(TypeFromToken(tkMethod) == mdtMemberRef)
    {
        // Get the parent
        mdToken typeref;
        if (FAILED(pMDInternalImport->GetParentOfMemberRef(tkMethod, &typeref)))
        {
            BAD_FORMAT_NOTHROW_ASSERT(!"Invalid MemberRef record");
            IfFailRet(COR_E_TYPELOAD);
        }

        while (TypeFromToken(typeref) == mdtTypeSpec)
        {
            // Added so that method impls can refer to instantiated interfaces or classes
            if (FAILED(pMDInternalImport->GetSigFromToken(typeref, &cSig, &pSig)))
            {
                BAD_FORMAT_NOTHROW_ASSERT(!"Invalid TypeSpec record");
                IfFailRet(COR_E_TYPELOAD);
            }
            CorElementType elemType = (CorElementType) *pSig++;

            // If this is a generic inst, we expect that the next elem is ELEMENT_TYPE_CLASS,
            // which is handled in the case below.
            if (elemType == ELEMENT_TYPE_GENERICINST)
            {
                elemType = (CorElementType) *pSig++;
                BAD_FORMAT_NOTHROW_ASSERT(elemType == ELEMENT_TYPE_CLASS);
            }

            // This covers E_T_GENERICINST and E_T_CLASS typespec formats. We don't expect
            // any other kinds to come through here.
            if (elemType == ELEMENT_TYPE_CLASS)
            {
                CorSigUncompressToken(pSig, &typeref);
            }
            else
            {
                // This is an unrecognized signature format.
                BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT,
                                               IDS_CLASSLOAD_MI_BAD_SIG,
                                               mdMethodDefNil);
            }
        }

        // If parent is a method def then this is a varags method
        if (TypeFromToken(typeref) == mdtMethodDef)
        {
            mdTypeDef typeDef;
            IfFailRet(pMDInternalImport->GetParentToken(typeref, &typeDef));

            // Make sure it is a typedef
            if (TypeFromToken(typeDef) != mdtTypeDef)
            {
                BAD_FORMAT_NOTHROW_ASSERT(!"MethodDef without TypeDef as Parent");
                IfFailRet(COR_E_TYPELOAD);
            }
            BAD_FORMAT_NOTHROW_ASSERT(typeDef == tkClass);
            // This is the real method we are overriding
            // <TODO>@TODO: CTS this may be illegal and we could throw an error</TODO>
            *ptkMethodDef = typeref;
        }

        else
        {
            // Verify that the ref points back to us
            mdToken tkDef = mdTokenNil;

            // We only get here when we know the token does not reference a type
            // in a different scope.
            if(TypeFromToken(typeref) == mdtTypeRef)
            {
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
                if(FAILED(hr))
                {
                    IfFailRet(COR_E_TYPELOAD);
                }
            }

            // We get a typedef when the parent of the token is a typespec to the type.
            else if (TypeFromToken(typeref) == mdtTypeDef)
            {
                tkDef = typeref;
            }

            else
            {
                CONSISTENCY_CHECK_MSGF(FALSE, ("Invalid methodimpl signature in class %s.", GetDebugClassName()));
                BuildMethodTableThrowException(COR_E_BADIMAGEFORMAT,
                                               IDS_CLASSLOAD_MI_BAD_SIG,
                                               mdMethodDefNil);
            }

            // If we required that the typedef be the same type as the current class,
            // and it doesn't match, we need to return a failure result.
            if (tkDef != tkClass)
            {
                IfFailRet(COR_E_TYPELOAD);
            }

            IfFailRet(pMDInternalImport->GetNameAndSigOfMemberRef(tkMethod, &pSig, &cSig, &szMember));

            if (isCallConv(
                MetaSig::GetCallingConvention(Signature(pSig, cSig)),
                IMAGE_CEE_CS_CALLCONV_FIELD))
            {
                return VLDTR_E_MR_BADCALLINGCONV;
            }

            hr = pMDInternalImport->FindMethodDef(
                tkDef, szMember, pSig, cSig, ptkMethodDef);
            IfFailRet(hr);
        }
    }

    else if (TypeFromToken(tkMethod) == mdtMethodDef)
    {
        mdTypeDef typeDef;

        // Verify that we are the parent
        hr = pMDInternalImport->GetParentToken(tkMethod, &typeDef);
        IfFailRet(hr);

        if(typeDef != tkClass)
        {
            IfFailRet(COR_E_TYPELOAD);
        }

        *ptkMethodDef = tkMethod;
    }

    else
    {
        IfFailRet(COR_E_TYPELOAD);
    }

    return hr;
}

//*******************************************************************************
void MethodTableBuilder::bmtMethodImplInfo::AddMethod(MethodDesc* pImplDesc, MethodDesc* pDesc, mdToken mdDecl, Substitution *pDeclSubst)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((pDesc == NULL || mdDecl == mdTokenNil) && (pDesc != NULL || mdDecl != mdTokenNil));
    rgEntries[pIndex].pDeclDesc = pDesc;
    rgEntries[pIndex].declToken = mdDecl;
    rgEntries[pIndex].declSubst = *pDeclSubst;
    rgEntries[pIndex].pBodyDesc = pImplDesc;
    pIndex++;
}

//*******************************************************************************
// Returns TRUE if tok acts as a body for any methodImpl entry. FALSE, otherwise.
BOOL MethodTableBuilder::bmtMethodImplInfo::IsBody(mdToken tok)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(TypeFromToken(tok) == mdtMethodDef);
    for (DWORD i = 0; i < pIndex; i++) {
        if (GetBodyMethodDesc(i)->GetMemberDef() == tok) {
            return TRUE;
        }
    }
    return FALSE;
}

//*******************************************************************************
// Returns TRUE for success, FALSE for failure
void MethodNameHash::Init(DWORD dwMaxEntries, StackingAllocator *pAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
    }
    CONTRACTL_END;

    // Given dwMaxEntries, determine a good value for the number of hash buckets
    m_dwNumBuckets = (dwMaxEntries / 10);

    if (m_dwNumBuckets < 5)
        m_dwNumBuckets = 5;

    S_UINT32 scbMemory = (S_UINT32(m_dwNumBuckets) * S_UINT32(sizeof(MethodHashEntry*))) +
                         (S_UINT32(dwMaxEntries) * S_UINT32(sizeof(MethodHashEntry)));

    if (scbMemory.IsOverflow())
    {
        ThrowHR(E_INVALIDARG);
    }

    if (pAllocator)
    {
        m_pMemoryStart = (BYTE*)pAllocator->Alloc(scbMemory);
    }
    else
    {   // We're given the number of hash table entries we're going to insert,
        // so we can allocate the appropriate size
        m_pMemoryStart = new BYTE[scbMemory.Value()];
    }

    INDEBUG(m_pDebugEndMemory = m_pMemoryStart + scbMemory.Value();)

    // Current alloc ptr
    m_pMemory       = m_pMemoryStart;

    // Allocate the buckets out of the alloc ptr
    m_pBuckets      = (MethodHashEntry**) m_pMemory;
    m_pMemory += sizeof(MethodHashEntry*)*m_dwNumBuckets;

    // Buckets all point to empty lists to begin with
    memset(m_pBuckets, 0, scbMemory.Value());
}

//*******************************************************************************
// Insert new entry at head of list
void MethodNameHash::Insert(LPCUTF8 pszName, MethodDesc *pDesc)
{
    LIMITED_METHOD_CONTRACT;
    DWORD           dwHash = HashStringA(pszName);
    DWORD           dwBucket = dwHash % m_dwNumBuckets;
    MethodHashEntry*pNewEntry;

    pNewEntry = (MethodHashEntry *) m_pMemory;
    m_pMemory += sizeof(MethodHashEntry);

    _ASSERTE(m_pMemory <= m_pDebugEndMemory);

    // Insert at head of bucket chain
    pNewEntry->m_pNext        = m_pBuckets[dwBucket];
    pNewEntry->m_pDesc        = pDesc;
    pNewEntry->m_dwHashValue  = dwHash;
    pNewEntry->m_pKey         = pszName;

    m_pBuckets[dwBucket] = pNewEntry;
}

//*******************************************************************************
// Return the first MethodHashEntry with this name, or NULL if there is no such entry
MethodHashEntry *MethodNameHash::Lookup(LPCUTF8 pszName, DWORD dwHash)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;


    if (!dwHash)
        dwHash = HashStringA(pszName);
    DWORD           dwBucket = dwHash % m_dwNumBuckets;
    MethodHashEntry*pSearch;

    for (pSearch = m_pBuckets[dwBucket]; pSearch; pSearch = pSearch->m_pNext)
    {
        if (pSearch->m_dwHashValue == dwHash && !strcmp(pSearch->m_pKey, pszName))
            return pSearch;
    }

    return NULL;
}

//*******************************************************************************
//
// Create a hash of all methods in this class.  The hash is from method name to MethodDesc.
//
MethodNameHash *MethodTableBuilder::CreateMethodChainHash(MethodTable *pMT)
{
    STANDARD_VM_CONTRACT;

    MethodNameHash *pHash = new (GetStackingAllocator()) MethodNameHash();

    pHash->Init(pMT->GetNumVirtuals(), GetStackingAllocator());

    MethodTable::MethodIterator it(pMT);
    for (;it.IsValid(); it.Next())
    {
        if (it.IsVirtual())
            {
            MethodDesc *pImplDesc = it.GetMethodDesc();
            CONSISTENCY_CHECK(CheckPointer(pImplDesc));
            MethodDesc *pDeclDesc = it.GetDeclMethodDesc();
            CONSISTENCY_CHECK(CheckPointer(pDeclDesc));

            CONSISTENCY_CHECK(pMT->IsInterface() || !pDeclDesc->IsInterface());
            pHash->Insert(pDeclDesc->GetNameOnNonArrayClass(), pDeclDesc);
        }
    }

    // Success
    return pHash;
}

//*******************************************************************************
void MethodTableBuilder::SetBMTData(
    bmtErrorInfo *bmtError,
    bmtProperties *bmtProp,
    bmtVtable *bmtVT,
    bmtParentInfo *bmtParent,
    bmtInterfaceInfo *bmtInterface,
    bmtMethodInfo *bmtMethod,
    bmtTypeInfo *bmtType,
    bmtMethodImplInfo *bmtMethodImpl)
{
    LIMITED_METHOD_CONTRACT;
    this->bmtError = bmtError;
    this->bmtProp = bmtProp;
    this->bmtVT = bmtVT;
    this->bmtParent = bmtParent;
    this->bmtInterface = bmtInterface;
    this->bmtMethod = bmtMethod;
    this->bmtType = bmtType;
    this->bmtMethodImpl = bmtMethodImpl;
}

//*******************************************************************************
void MethodTableBuilder::NullBMTData()
{
    LIMITED_METHOD_CONTRACT;
    this->bmtError = NULL;
    this->bmtProp = NULL;
    this->bmtVT = NULL;
    this->bmtParent = NULL;
    this->bmtInterface = NULL;
    this->bmtMethod = NULL;
    this->bmtType = NULL;
    this->bmtMethodImpl = NULL;
}

//*******************************************************************************
/*static*/
VOID DECLSPEC_NORETURN MethodTableBuilder::BuildMethodTableThrowException(
    HRESULT hr,
    const bmtErrorInfo & bmtError)
{
    STANDARD_VM_CONTRACT;

    LPCUTF8 pszClassName, pszNameSpace;
    if (FAILED(bmtError.pModule->GetMDImport()->GetNameOfTypeDef(bmtError.cl, &pszClassName, &pszNameSpace)))
    {
        pszClassName = pszNameSpace = "Invalid TypeDef record";
    }

    if (IsNilToken(bmtError.dMethodDefInError) && bmtError.szMethodNameForError == NULL) {
        if (hr == E_OUTOFMEMORY)
            COMPlusThrowOM();
        else
            bmtError.pModule->GetAssembly()->ThrowTypeLoadException(pszNameSpace, pszClassName,
                                                                    bmtError.resIDWhy);
    }
    else {
        LPCUTF8 szMethodName;
        if (bmtError.szMethodNameForError == NULL)
        {
            if (FAILED((bmtError.pModule->GetMDImport())->GetNameOfMethodDef(bmtError.dMethodDefInError, &szMethodName)))
            {
                szMethodName = "Invalid MethodDef record";
            }
        }
        else
            szMethodName = bmtError.szMethodNameForError;

        bmtError.pModule->GetAssembly()->ThrowTypeLoadException(pszNameSpace, pszClassName,
                                                                szMethodName, bmtError.resIDWhy);
    }

}

//*******************************************************************************
/* static */
int __cdecl MethodTableBuilder::bmtMethodImplInfo::MethodImplTokenPair::Compare(
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
BOOL MethodTableBuilder::bmtMethodImplInfo::MethodImplTokenPair::Equal(
        const MethodImplTokenPair *elem1,
        const MethodImplTokenPair *elem2)
{
    STATIC_CONTRACT_LEAF;
    return ((elem1->methodBody == elem2->methodBody) &&
            (elem1->methodDecl == elem2->methodDecl));
}


}; // namespace ClassCompat

#endif // !DACCESS_COMPILE

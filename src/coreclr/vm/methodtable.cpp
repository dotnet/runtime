// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: methodtable.cpp
//

#include "common.h"

#include "clsload.hpp"
#include "method.hpp"
#include "class.h"
#include "classcompat.h"
#include "object.h"
#include "field.h"
#include "util.hpp"
#include "excep.h"
#include "siginfo.hpp"
#include "threads.h"
#include "stublink.h"
#include "ecall.h"
#include "dllimport.h"
#include "gcdesc.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "log.h"
#include "fieldmarshaler.h"
#include "cgensys.h"
#include "gcheaputilities.h"
#include "dbginterface.h"
#include "comdelegate.h"
#include "eventtrace.h"


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
#include "contractimpl.h"

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

#include "typeequivalencehash.hpp"

#include "generics.h"
#include "genericdict.h"
#include "typestring.h"
#include "typedesc.h"
#include "array.h"
#include "castcache.h"
#include "dynamicinterfacecastable.h"
#include "frozenobjectheap.h"

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

#ifndef DACCESS_COMPILE

// Typedef for string comparison functions.
typedef int (__cdecl *UTF8StringCompareFuncPtr)(const char *, const char *);

MethodDataCache *MethodTable::s_pMethodDataCache = NULL;

#ifdef _DEBUG
extern unsigned g_dupMethods;
#endif

#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
//==========================================================================================
class MethodDataCache
{
    typedef MethodTable::MethodData MethodData;

  public:    // Ctor. Allocates cEntries entries. Throws.
    static UINT32 GetObjectSize(UINT32 cEntries);
    MethodDataCache(UINT32 cEntries);

    MethodData *Find(MethodTable *pMT);
    MethodData *Find(MethodTable *pMTDecl, MethodTable *pMTImpl);
    void Insert(MethodData *pMData);
    void Clear();

  protected:
    // This describes each entry in the cache.
    struct Entry
    {
        MethodData *m_pMData;
        UINT32      m_iTimestamp;
    };

    MethodData *FindHelper(MethodTable *pMTDecl, MethodTable *pMTImpl, UINT32 idx);

    inline UINT32 GetNextTimestamp()
        { return ++m_iCurTimestamp; }

    inline UINT32 NumEntries()
        { LIMITED_METHOD_CONTRACT; return m_cEntries; }

    inline void TouchEntry(UINT32 i)
        { WRAPPER_NO_CONTRACT; m_iLastTouched = i; GetEntry(i)->m_iTimestamp = GetNextTimestamp(); }

    inline UINT32 GetLastTouchedEntryIndex()
        { WRAPPER_NO_CONTRACT; return m_iLastTouched; }

    // The end of this object contains an array of Entry
    inline Entry *GetEntryData()
        { LIMITED_METHOD_CONTRACT; return (Entry *)(this + 1); }

    inline Entry *GetEntry(UINT32 i)
        { WRAPPER_NO_CONTRACT; return GetEntryData() + i; }

  private:
    // This serializes access to the cache
    SimpleRWLock    m_lock;

    // This allows ageing of entries to decide which to punt when
    // inserting a new entry.
    UINT32 m_iCurTimestamp;

    // The number of entries in the cache
    UINT32 m_cEntries;
    UINT32 m_iLastTouched;

#ifdef HOST_64BIT
    UINT32 pad;      // insures that we are a multiple of 8-bytes
#endif
};  // class MethodDataCache

//==========================================================================================
UINT32 MethodDataCache::GetObjectSize(UINT32 cEntries)
{
    LIMITED_METHOD_CONTRACT;
    return sizeof(MethodDataCache) + (sizeof(Entry) * cEntries);
}

//==========================================================================================
MethodDataCache::MethodDataCache(UINT32 cEntries)
    : m_lock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT),
      m_iCurTimestamp(0),
      m_cEntries(cEntries),
      m_iLastTouched(0)
{
    WRAPPER_NO_CONTRACT;
    ZeroMemory(GetEntryData(), cEntries * sizeof(Entry));
}

//==========================================================================================
MethodTable::MethodData *MethodDataCache::FindHelper(
    MethodTable *pMTDecl, MethodTable *pMTImpl, UINT32 idx)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    MethodData *pEntry = GetEntry(idx)->m_pMData;
    if (pEntry != NULL) {
        MethodTable *pMTDeclEntry = pEntry->GetDeclMethodTable();
        MethodTable *pMTImplEntry = pEntry->GetImplMethodTable();
        if (pMTDeclEntry == pMTDecl && pMTImplEntry == pMTImpl) {
            return pEntry;
        }
        else if (pMTDecl == pMTImpl) {
            if (pMTDeclEntry == pMTDecl) {
                return pEntry->GetDeclMethodData();
            }
            if (pMTImplEntry == pMTDecl) {
                return pEntry->GetImplMethodData();
            }
        }
    }

    return NULL;
}

//==========================================================================================
MethodTable::MethodData *MethodDataCache::Find(MethodTable *pMTDecl, MethodTable *pMTImpl)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    } CONTRACTL_END;

#ifdef LOGGING
    g_sdStats.m_cCacheLookups++;
#endif

    SimpleReadLockHolder lh(&m_lock);

    // Check the last touched entry.
    MethodData *pEntry = FindHelper(pMTDecl, pMTImpl, GetLastTouchedEntryIndex());

    // Now search the entire cache.
    if (pEntry == NULL) {
        for (UINT32 i = 0; i < NumEntries(); i++) {
            pEntry = FindHelper(pMTDecl, pMTImpl, i);
            if (pEntry != NULL) {
                TouchEntry(i);
                break;
            }
        }
    }

    if (pEntry != NULL) {
        pEntry->AddRef();
    }

#ifdef LOGGING
    else {
        // Failure to find the entry in the cache.
        g_sdStats.m_cCacheMisses++;
    }
#endif // LOGGING

    return pEntry;
}

//==========================================================================================
MethodTable::MethodData *MethodDataCache::Find(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    return Find(pMT, pMT);
}

//==========================================================================================
void MethodDataCache::Insert(MethodData *pMData)
{
    CONTRACTL {
        NOTHROW; // for now, because it does not yet resize.
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    SimpleWriteLockHolder hLock(&m_lock);

    UINT32 iMin = UINT32_MAX;
    UINT32 idxMin = UINT32_MAX;
    for (UINT32 i = 0; i < NumEntries(); i++) {
        if (GetEntry(i)->m_iTimestamp < iMin) {
            idxMin = i;
            iMin = GetEntry(i)->m_iTimestamp;
        }
    }
    Entry *pEntry = GetEntry(idxMin);
    if (pEntry->m_pMData != NULL) {
        pEntry->m_pMData->Release();
    }
    pMData->AddRef();
    pEntry->m_pMData = pMData;
    pEntry->m_iTimestamp = GetNextTimestamp();
}

//==========================================================================================
void MethodDataCache::Clear()
{
    CONTRACTL {
        NOTHROW; // for now, because it does not yet resize.
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    // Taking the lock here is just a precaution. Really, the runtime
    // should be suspended because this is called while unloading an
    // AppDomain at the SysSuspendEE stage. But, if someone calls it
    // outside of that context, we should be extra cautious.
    SimpleWriteLockHolder lh(&m_lock);

    for (UINT32 i = 0; i < NumEntries(); i++) {
        Entry *pEntry = GetEntry(i);
        if (pEntry->m_pMData != NULL) {
            pEntry->m_pMData->Release();
        }
    }
    ZeroMemory(GetEntryData(), NumEntries() * sizeof(Entry));
    m_iCurTimestamp = 0;
}   // MethodDataCache::Clear

#endif // !DACCESS_COMPILE


//==========================================================================================
// Optimization intended for MethodTable::GetDispatchMap

#include <optsmallperfcritical.h>

//==========================================================================================
PTR_DispatchMap MethodTable::GetDispatchMap()
{
    LIMITED_METHOD_DAC_CONTRACT;

    MethodTable * pMT = this;

    if (!pMT->HasDispatchMapSlot())
    {
        pMT = pMT->GetCanonicalMethodTable();
        if (!pMT->HasDispatchMapSlot())
            return NULL;
    }

    return dac_cast<PTR_DispatchMap>((pMT->GetAuxiliaryData() + 1));
}

#include <optdefault.h>


//==========================================================================================
PTR_Module MethodTable::GetModuleIfLoaded()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return GetModule();
}

//==========================================================================================
BOOL MethodTable::ValidateWithPossibleAV()
{
    CANNOT_HAVE_CONTRACT;
    SUPPORTS_DAC;

    // MethodTables have the canonicalization property below.
    // i.e. canonicalize, and canonicalize again, and check the result are
    // the same.  This is a property that holds for every single valid object in
    // the system, but which should hold for very few other addresses.

    // For non-generic classes, we can rely on comparing
    //    object->methodtable->class->methodtable
    // to
    //    object->methodtable
    //
    //  However, for generic instantiation this does not work. There we must
    //  compare
    //
    //    object->methodtable->class->methodtable->class
    // to
    //    object->methodtable->class
    //
    // Of course, that's not necessarily enough to verify that the method
    // table and class are absolutely valid - we rely on type soundness
    // for that. We need to do more sanity checking to
    // make sure that our pointer here is in fact a valid object.
    PTR_EEClass pEEClass = this->GetClassWithPossibleAV();
    return ((pEEClass && (this == pEEClass->GetMethodTableWithPossibleAV())) ||
        ((HasInstantiation() || IsArray()) &&
        (pEEClass && (pEEClass->GetMethodTableWithPossibleAV()->GetClassWithPossibleAV() == pEEClass))));
}

#ifndef DACCESS_COMPILE

//==========================================================================================
BOOL  MethodTable::IsClassInited()
{
    WRAPPER_NO_CONTRACT;

    if (IsClassPreInited())
        return TRUE;

    if (IsSharedByGenericInstantiations())
        return FALSE;

    DomainLocalModule *pLocalModule = GetDomainLocalModule();

    _ASSERTE(pLocalModule != NULL);

    return pLocalModule->IsClassInitialized(this);
}

//==========================================================================================
BOOL  MethodTable::IsInitError()
{
    WRAPPER_NO_CONTRACT;

    DomainLocalModule *pLocalModule = GetDomainLocalModule();
    _ASSERTE(pLocalModule != NULL);

    return pLocalModule->IsClassInitError(this);
}

//==========================================================================================
// mark the class as having its .cctor run
void MethodTable::SetClassInited()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!IsClassPreInited());
    GetDomainLocalModule()->SetClassInitialized(this);
}

//==========================================================================================
void MethodTable::SetClassInitError()
{
    WRAPPER_NO_CONTRACT;
    GetDomainLocalModule()->SetClassInitError(this);
}

//==========================================================================================
// mark the class as having been restored.
void MethodTable::SetIsRestored()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    PRECONDITION(!IsFullyLoaded());

    InterlockedAnd((LONG*)&GetAuxiliaryDataForWrite()->m_dwFlags, ~MethodTableAuxiliaryData::enum_flag_Unrestored);

#ifndef DACCESS_COMPILE
    if (ETW_PROVIDER_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER))
    {
        ETW::MethodLog::MethodTableRestored(this);
    }
#endif
}

//==========================================================================================
// mark as COM object type (System.__ComObject and types deriving from it)
void MethodTable::SetComObjectType()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_ComObject);
}

#ifdef FEATURE_ICASTABLE
void MethodTable::SetICastable()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_ICastable);
}
#endif

BOOL MethodTable::IsICastable()
{
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_ICASTABLE
    return GetFlag(enum_flag_ICastable);
#else
    return FALSE;
#endif
}

void MethodTable::SetIDynamicInterfaceCastable()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_IDynamicInterfaceCastable);
}

BOOL MethodTable::IsIDynamicInterfaceCastable()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetFlag(enum_flag_IDynamicInterfaceCastable);
}

void MethodTable::SetIsTrackedReferenceWithFinalizer()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_IsTrackedReferenceWithFinalizer);
}

#endif // !DACCESS_COMPILE

BOOL MethodTable::IsTrackedReferenceWithFinalizer()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetFlag(enum_flag_IsTrackedReferenceWithFinalizer);
}

//==========================================================================================
WORD MethodTable::GetNumMethods()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetClass()->GetNumMethods();
}

//==========================================================================================
PTR_BaseDomain MethodTable::GetDomain()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<PTR_BaseDomain>(AppDomain::GetCurrentDomain());
}

//==========================================================================================
BOOL MethodTable::HasSameTypeDefAs(MethodTable *pMT)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (this == pMT)
        return TRUE;

    // optimize for the negative case where we expect RID mismatch
    DWORD rid = GetTypeDefRid();
    if (rid != pMT->GetTypeDefRid())
        return FALSE;

    // Types without RIDs are unrelated to each other. This case is taken for arrays. 
    if (rid == 0)
        return FALSE;

    return (GetModule() == pMT->GetModule());
}

#ifndef DACCESS_COMPILE

//==========================================================================================
PTR_MethodTable InterfaceInfo_t::GetApproxMethodTable(Module * pContainingModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    MethodTable * pItfMT = GetMethodTable();
    ClassLoader::EnsureLoaded(TypeHandle(pItfMT), CLASS_LOAD_APPROXPARENTS);
    return pItfMT;
}

//==========================================================================================
// get the method desc given the interface method desc
/* static */ MethodDesc *MethodTable::GetMethodDescForInterfaceMethodAndServer(
                            TypeHandle ownerType, MethodDesc *pItfMD, OBJECTREF *pServer)
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pItfMD));
        PRECONDITION(pItfMD->IsInterface());
        PRECONDITION(!ownerType.IsNull());
        PRECONDITION(ownerType.GetMethodTable()->HasSameTypeDefAs(pItfMD->GetMethodTable()));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    VALIDATEOBJECTREF(*pServer);

#ifdef _DEBUG
    MethodTable * pItfMT =  ownerType.GetMethodTable();
    PREFIX_ASSUME(pItfMT != NULL);
#endif // _DEBUG

    MethodTable *pServerMT = (*pServer)->GetMethodTable();
    PREFIX_ASSUME(pServerMT != NULL);

#ifdef FEATURE_ICASTABLE
    // In case of ICastable, instead of trying to find method implementation in the real object type
    // we call GetMethodDescForInterfaceMethod() again with whatever type it returns.
    // It allows objects that implement ICastable to mimic behavior of other types.
    if (pServerMT->IsICastable() &&
        !pItfMD->HasMethodInstantiation() &&
        !TypeHandle(pServerMT).CanCastTo(ownerType)) // we need to make sure object doesn't implement this interface in a natural way
    {
        GCStress<cfg_any>::MaybeTrigger();

        // Make call to ICastableHelpers.GetImplType(obj, interfaceTypeObj)
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ICASTABLEHELPERS__GETIMPLTYPE);

        OBJECTREF ownerManagedType = ownerType.GetManagedClassObject(); //GC triggers

        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*pServer);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(ownerManagedType);

        OBJECTREF impTypeObj = NULL;
        CALL_MANAGED_METHOD_RETREF(impTypeObj, OBJECTREF, args);

        INDEBUG(ownerManagedType = NULL); //ownerManagedType wasn't protected during the call
        if (impTypeObj == NULL) // GetImplType returns default(RuntimeTypeHandle)
        {
            COMPlusThrow(kEntryPointNotFoundException);
        }

        ReflectClassBaseObject* resultTypeObj = ((ReflectClassBaseObject*)OBJECTREFToObject(impTypeObj));
        TypeHandle resultTypeHnd = resultTypeObj->GetType();
        MethodTable *pResultMT = resultTypeHnd.GetMethodTable();

        RETURN(pResultMT->GetMethodDescForInterfaceMethod(ownerType, pItfMD, TRUE /* throwOnConflict */));
    }
#endif

    // For IDynamicInterfaceCastable, instead of trying to find method implementation in the real object type
    // we call GetInterfaceImplementation on the object and call GetMethodDescForInterfaceMethod
    // with whatever type it returns.
    if (pServerMT->IsIDynamicInterfaceCastable()
        && !TypeHandle(pServerMT).CanCastTo(ownerType)) // we need to make sure object doesn't implement this interface in a natural way
    {
        TypeHandle implTypeHandle;
        OBJECTREF obj = *pServer;

        GCPROTECT_BEGIN(obj);
        OBJECTREF implTypeRef = DynamicInterfaceCastable::GetInterfaceImplementation(&obj, ownerType);
        _ASSERTE(implTypeRef != NULL);

        ReflectClassBaseObject *implTypeObj = ((ReflectClassBaseObject *)OBJECTREFToObject(implTypeRef));
        implTypeHandle = implTypeObj->GetType();
        GCPROTECT_END();

        RETURN(implTypeHandle.GetMethodTable()->GetMethodDescForInterfaceMethod(ownerType, pItfMD, TRUE /* throwOnConflict */));
    }

#ifdef FEATURE_COMINTEROP
    if (pServerMT->IsComObjectType() && !pItfMD->HasMethodInstantiation())
    {
        // interop needs an exact MethodDesc
        pItfMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pItfMD,
            ownerType.GetMethodTable(),
            FALSE,              // forceBoxedEntryPoint
            Instantiation(),    // methodInst
            FALSE,              // allowInstParam
            TRUE);              // forceRemotableMethod

        RETURN(pServerMT->GetMethodDescForComInterfaceMethod(pItfMD, false));
    }
#endif // !FEATURE_COMINTEROP

    // Handle pure COM+ types.
    RETURN (pServerMT->GetMethodDescForInterfaceMethod(ownerType, pItfMD, TRUE /* throwOnConflict */));
}

#ifdef FEATURE_COMINTEROP
//==========================================================================================
// get the method desc given the interface method desc on a COM implemented server
// (if fNullOk is set then NULL is an allowable return value)
MethodDesc *MethodTable::GetMethodDescForComInterfaceMethod(MethodDesc *pItfMD, bool fNullOk)
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pItfMD));
        PRECONDITION(pItfMD->IsInterface());
        PRECONDITION(IsComObjectType());
        POSTCONDITION(fNullOk || CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodTable * pItfMT =  pItfMD->GetMethodTable();
    PREFIX_ASSUME(pItfMT != NULL);

        // We now handle __ComObject class that doesn't have Dynamic Interface Map
    if (!HasDynamicInterfaceMap())
    {
        RETURN(pItfMD);
    }
    else
    {
        // Now we handle the more complex extensible RCW's. The first thing to do is check
        // to see if the static definition of the extensible RCW specifies that the class
        // implements the interface.
        DWORD slot = (DWORD) -1;

        // Calling GetTarget here instead of FindDispatchImpl gives us caching functionality to increase speed.
        PCODE tgt = VirtualCallStubManager::GetTarget(
            pItfMT->GetLoaderAllocator()->GetDispatchToken(pItfMT->GetTypeID(), pItfMD->GetSlot()), this, TRUE /* throwOnConflict */);

        if (tgt != NULL)
        {
            RETURN(MethodTable::GetMethodDescForSlotAddress(tgt));
        }

        // The interface is not in the static class definition so we need to look at the
        // dynamic interfaces.
        else if (FindDynamicallyAddedInterface(pItfMT))
        {
            // This interface was added to the class dynamically so it is implemented
            // by the COM object. We treat this dynamically added interfaces the same
            // way we treat COM objects. That is by using the interface vtable.
            RETURN(pItfMD);
        }
        else
        {
            RETURN(NULL);
        }
    }
}
#endif // FEATURE_COMINTEROP

void MethodTable::AllocateAuxiliaryData(LoaderAllocator *pAllocator, Module *pLoaderModule, AllocMemTracker *pamTracker, bool hasGenericStatics, WORD nonVirtualSlots, S_SIZE_T extraAllocation)
{
    S_SIZE_T cbAuxiliaryData = S_SIZE_T(sizeof(MethodTableAuxiliaryData));

    size_t prependedAllocationSpace = 0;

    prependedAllocationSpace = nonVirtualSlots * sizeof(TADDR);

    if (hasGenericStatics)
        prependedAllocationSpace = prependedAllocationSpace + sizeof(GenericsStaticsInfo);
    
    cbAuxiliaryData = cbAuxiliaryData + S_SIZE_T(prependedAllocationSpace) + extraAllocation;
    if (cbAuxiliaryData.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    BYTE* pAuxiliaryDataRegion = (BYTE *)
        pamTracker->Track(pAllocator->GetHighFrequencyHeap()->AllocMem(cbAuxiliaryData));
    
    MethodTableAuxiliaryData * pMTAuxiliaryData;
    pMTAuxiliaryData = (MethodTableAuxiliaryData *)(pAuxiliaryDataRegion + prependedAllocationSpace);

    pMTAuxiliaryData->SetLoaderModule(pLoaderModule);
    pMTAuxiliaryData->SetOffsetToNonVirtualSlots(hasGenericStatics ? -(int16_t)sizeof(GenericsStaticsInfo) : 0);
    m_pAuxiliaryData = pMTAuxiliaryData;
}



//---------------------------------------------------------------------------------------
//
MethodTable* CreateMinimalMethodTable(Module* pContainingModule,
                                      LoaderAllocator* pLoaderAllocator,
                                      AllocMemTracker* pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    EEClass* pClass = EEClass::CreateMinimalClass(pLoaderAllocator->GetHighFrequencyHeap(), pamTracker);

    LOG((LF_BCL, LL_INFO100, "Level2 - Creating MethodTable {0x%p}...\n", pClass));

    MethodTable* pMT = (MethodTable *)(void *)pamTracker->Track(pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(MethodTable))));

    // Note: Memory allocated on loader heap is zero filled
    // memset(pMT, 0, sizeof(MethodTable));

    // Allocate the private data block ("private" during runtime in the ngen'ed case).
    pMT->AllocateAuxiliaryData(pLoaderAllocator, pContainingModule, pamTracker);
    pMT->SetModule(pContainingModule);
    pMT->SetLoaderAllocator(pLoaderAllocator);

    //
    // Set up the EEClass
    //
    pClass->SetMethodTable(pMT); // in the EEClass set the pointer to this MethodTable
    pClass->SetAttrClass(tdPublic | tdSealed);

    //
    // Set up the MethodTable
    //
    // Does not need parent. Note that MethodTable for COR_GLOBAL_PARENT_TOKEN does not have parent either,
    // so the system has to be wired for dealing with no parent anyway.
    pMT->SetParentMethodTable(NULL);
    pMT->SetClass(pClass);
    pMT->SetInternalCorElementType(ELEMENT_TYPE_CLASS);
    pMT->SetBaseSize(OBJECT_BASESIZE);

#ifdef _DEBUG
    pClass->SetDebugClassName("dynamicClass");
    pMT->SetDebugClassName("dynamicClass");
#endif

    LOG((LF_BCL, LL_INFO10, "Level1 - MethodTable created {0x%p}\n", pClass));

    return pMT;
}


#ifdef FEATURE_COMINTEROP
//==========================================================================================
OBJECTREF MethodTable::GetObjCreateDelegate()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;
    _ASSERT(!IsInterface());
    if (GetOHDelegate())
        return ObjectFromHandle(GetOHDelegate());
    else
        return NULL;
}

//==========================================================================================
void MethodTable::SetObjCreateDelegate(OBJECTREF orDelegate)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        THROWS; // From CreateHandle
    }
    CONTRACTL_END;

    if (GetOHDelegate())
        StoreObjectInHandle(GetOHDelegate(), orDelegate);
    else
        SetOHDelegate (GetAppDomain()->CreateHandle(orDelegate));
}
#endif // FEATURE_COMINTEROP


//==========================================================================================
void MethodTable::SetInterfaceMap(WORD wNumInterfaces, InterfaceInfo_t* iMap)
{
    LIMITED_METHOD_CONTRACT;
    if (wNumInterfaces == 0)
    {
        _ASSERTE(!HasInterfaceMap());
        return;
    }

    m_wNumInterfaces = wNumInterfaces;

    CONSISTENCY_CHECK(IS_ALIGNED(iMap, sizeof(void*)));
    m_pInterfaceMap = iMap;
}

//==========================================================================================
// Called after GetExtraInterfaceInfoSize above to setup a new MethodTable with the additional memory to track
// extra interface info. If there are a non-zero number of interfaces implemented on this class but
// GetExtraInterfaceInfoSize() returned zero, this call must still be made (with a NULL argument).
void MethodTable::InitializeExtraInterfaceInfo(PVOID pInfo)
{
    STANDARD_VM_CONTRACT;

    // Check that memory was allocated or not allocated in the right scenarios.
    _ASSERTE(((pInfo == NULL) && (GetExtraInterfaceInfoSize(GetNumInterfaces()) == 0)) ||
             ((pInfo != NULL) && (GetExtraInterfaceInfoSize(GetNumInterfaces()) != 0)));

    // This call is a no-op if we don't require extra interface info (in which case a buffer should never have
    // been allocated).
    if (!HasExtraInterfaceInfo())
    {
        _ASSERTE(pInfo == NULL);
        return;
    }

    // Get pointer to optional slot that holds either a small inlined bitmap of flags or the pointer to a
    // larger bitmap.
    PTR_TADDR pInfoSlot = GetExtraInterfaceInfoPtr();

    // In either case, data inlined or held in an external buffer, the correct thing to do is to write pInfo
    // to the slot. In the inlined case we wish to set all flags to their default value (zero, false) and
    // writing NULL does that. Otherwise we simply want to dump the buffer pointer directly into the slot (no
    // need for a discriminator bit, we can always infer which format we're using based on the interface
    // count).
    *pInfoSlot = (TADDR)pInfo;

    // There shouldn't be any need for further initialization in the buffered case since loader heap
    // allocation zeroes data.
#ifdef _DEBUG
    if (pInfo != NULL)
        for (DWORD i = 0; i < GetExtraInterfaceInfoSize(GetNumInterfaces()); i++)
            _ASSERTE(*((BYTE*)pInfo + i) == 0);
#endif // _DEBUG
}

// Define a macro that generates a mask for a given bit in a TADDR correctly on either 32 or 64 bit platforms.
#ifdef HOST_64BIT
#define SELECT_TADDR_BIT(_index) (1ULL << (_index))
#else
#define SELECT_TADDR_BIT(_index) (1U << (_index))
#endif

//==========================================================================================
// For the given interface in the map (specified via map index) mark the interface as declared explicitly on
// this class. This is not legal for dynamically added interfaces (as used by RCWs).
void MethodTable::SetInterfaceDeclaredOnClass(DWORD index)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(HasExtraInterfaceInfo());
    _ASSERTE(index < GetNumInterfaces());

    // Get address of optional slot for extra info.
    PTR_TADDR pInfoSlot = GetExtraInterfaceInfoPtr();

    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshold)
    {
        // Bitmap of flags is stored inline in the optional slot.
        *pInfoSlot |= SELECT_TADDR_BIT(index);
    }
    else
    {
        // Slot points to a buffer containing a larger bitmap.
        TADDR *pBitmap = (PTR_TADDR)*pInfoSlot;

        DWORD idxTaddr = index / (sizeof(TADDR) * 8);   // Select TADDR in array that covers the target bit
        DWORD idxInTaddr = index % (sizeof(TADDR) * 8);
        TADDR bitmask = SELECT_TADDR_BIT(idxInTaddr);

        pBitmap[idxTaddr] |= bitmask;
        _ASSERTE((pBitmap[idxTaddr] & bitmask) == bitmask);
    }
}

//==========================================================================================
// For the given interface return true if the interface was declared explicitly on this class.
bool MethodTable::IsInterfaceDeclaredOnClass(DWORD index)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(HasExtraInterfaceInfo());

    // Dynamic interfaces are always marked as not DeclaredOnClass (I don't know why but this is how the code
    // was originally authored).
    if (index >= GetNumInterfaces())
    {
#ifdef FEATURE_COMINTEROP
        _ASSERTE(HasDynamicInterfaceMap());
#endif // FEATURE_COMINTEROP
        return false;
    }

    // Get data from the optional extra info slot.
    TADDR taddrInfo = *GetExtraInterfaceInfoPtr();

    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshold)
    {
        // Bitmap of flags is stored directly in the value.
        return (taddrInfo & SELECT_TADDR_BIT(index)) != 0;
    }
    else
    {
        // Slot points to a buffer containing a larger bitmap.
        TADDR *pBitmap = (PTR_TADDR)taddrInfo;

        DWORD idxTaddr = index / (sizeof(TADDR) * 8);   // Select TADDR in array that covers the target bit
        DWORD idxInTaddr = index % (sizeof(TADDR) * 8);
        TADDR bitmask = SELECT_TADDR_BIT(idxInTaddr);

        return (pBitmap[idxTaddr] & bitmask) != 0;
    }
}

#ifdef FEATURE_COMINTEROP

//==========================================================================================
PTR_InterfaceInfo MethodTable::GetDynamicallyAddedInterfaceMap()
{
    LIMITED_METHOD_DAC_CONTRACT;
    PRECONDITION(HasDynamicInterfaceMap());

    return GetInterfaceMap() + GetNumInterfaces();
}

//==========================================================================================
unsigned MethodTable::GetNumDynamicallyAddedInterfaces()
{
    LIMITED_METHOD_DAC_CONTRACT;
    PRECONDITION(HasDynamicInterfaceMap());

    PTR_InterfaceInfo pInterfaces = GetInterfaceMap();
    PREFIX_ASSUME(pInterfaces != NULL);
    return (unsigned)*(dac_cast<PTR_SIZE_T>(pInterfaces) - 1);
}

//==========================================================================================
BOOL MethodTable::FindDynamicallyAddedInterface(MethodTable *pInterface)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsRestored());
    _ASSERTE(HasDynamicInterfaceMap());     // This should never be called on for a type that is not an extensible RCW.

    unsigned cDynInterfaces = GetNumDynamicallyAddedInterfaces();
    InterfaceInfo_t *pDynItfMap = GetDynamicallyAddedInterfaceMap();

    for (unsigned i = 0; i < cDynInterfaces; i++)
    {
        if (pDynItfMap[i].GetMethodTable() == pInterface)
            return TRUE;
    }

    return FALSE;
}

//==========================================================================================
void MethodTable::AddDynamicInterface(MethodTable *pItfMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsRestored());
        PRECONDITION(HasDynamicInterfaceMap());    // This should never be called on for a type that is not an extensible RCW.
    }
    CONTRACTL_END;

    unsigned NumDynAddedInterfaces = GetNumDynamicallyAddedInterfaces();
    unsigned TotalNumInterfaces = GetNumInterfaces() + NumDynAddedInterfaces;

    InterfaceInfo_t *pNewItfMap = NULL;
    S_SIZE_T AllocSize =  (S_SIZE_T(S_UINT32(TotalNumInterfaces) + S_UINT32(1)) * S_SIZE_T(sizeof(InterfaceInfo_t))) + S_SIZE_T(sizeof(DWORD_PTR));
    if (AllocSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    // Allocate the new interface table adding one for the new interface and one
    // more for the dummy slot before the start of the table..
    pNewItfMap = (InterfaceInfo_t*)(void*)GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(AllocSize);

    pNewItfMap = (InterfaceInfo_t*)(((BYTE *)pNewItfMap) + sizeof(DWORD_PTR));

    // Copy the old map into the new one.
    if (TotalNumInterfaces > 0) {
        InterfaceInfo_t *pInterfaceMap = GetInterfaceMap();
        PREFIX_ASSUME(pInterfaceMap != NULL);

        for (unsigned index = 0; index < TotalNumInterfaces; ++index)
        {
          InterfaceInfo_t *pIntInfo = (InterfaceInfo_t *) (pNewItfMap + index);
          pIntInfo->SetMethodTable((pInterfaceMap + index)->GetMethodTable());
        }
    }

    // Add the new interface at the end of the map.
    pNewItfMap[TotalNumInterfaces].SetMethodTable(pItfMT);

    // Update the count of dynamically added interfaces.
    *(((DWORD_PTR *)pNewItfMap) - 1) = NumDynAddedInterfaces + 1;

    // Switch the old interface map with the new one.
    VolatileStore(&m_pInterfaceMap, pNewItfMap);

    // Log the fact that we leaked the interface vtable map.
#ifdef _DEBUG
    LOG((LF_INTEROP, LL_EVERYTHING,
        "Extensible RCW %s being cast to interface %s caused an interface vtable map leak",
        GetClass()->GetDebugClassName(), pItfMT->GetClass()->m_szDebugClassName));
#else // !_DEBUG
    LOG((LF_INTEROP, LL_EVERYTHING,
        "Extensible RCW being cast to an interface caused an interface vtable map leak"));
#endif // !_DEBUG
} // MethodTable::AddDynamicInterface

#endif // FEATURE_COMINTEROP

void MethodTable::SetupGenericsStaticsInfo(FieldDesc* pStaticFieldDescs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // No need to generate IDs for open types.  Indeed since we don't save them
    // in the NGEN image it would be actively incorrect to do so.  However
    // we still leave the optional member in the MethodTable holding the value -1 for the ID.

    GenericsStaticsInfo *pInfo = GetGenericsStaticsInfo();
    if (!ContainsGenericVariables() && !IsSharedByGenericInstantiations())
    {
        Module * pModuleForStatics = GetLoaderModule();

        pInfo->m_DynamicTypeID = pModuleForStatics->AllocateDynamicEntry(this);
    }
    else
    {
        pInfo->m_DynamicTypeID = (SIZE_T)-1;
    }

    pInfo->m_pFieldDescs = pStaticFieldDescs;
}

#endif // !DACCESS_COMPILE

//==========================================================================================
// Calculate how many bytes of storage will be required to track additional information for interfaces. This
// will be zero if there are no interfaces, but can also be zero for small numbers of interfaces as well, and
// callers should be ready to handle this.
/* static */ SIZE_T MethodTable::GetExtraInterfaceInfoSize(DWORD cInterfaces)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // For small numbers of interfaces we can record the info in the TADDR of the optional member itself (use
    // the TADDR as a bitmap).
    if (cInterfaces <= kInlinedInterfaceInfoThreshold)
        return 0;

    // Otherwise we'll cause an array of TADDRs to be allocated (use TADDRs since the heap space allocated
    // will almost certainly need to be TADDR aligned anyway).
    return ALIGN_UP(cInterfaces, sizeof(TADDR) * 8) / 8;
}

#ifdef DACCESS_COMPILE
//==========================================================================================
void MethodTable::EnumMemoryRegionsForExtraInterfaceInfo()
{
    SUPPORTS_DAC;

    // No extra data to enum if the number of interfaces is below the threshold -- there is either no data or
    // it all fits into the optional members inline.
    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshold)
        return;

    DacEnumMemoryRegion(*GetExtraInterfaceInfoPtr(), GetExtraInterfaceInfoSize(GetNumInterfaces()));
}
#endif // DACCESS_COMPILE

//==========================================================================================
Module* MethodTable::GetModuleForStatics()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (HasGenericsStaticsInfo())
    {
        DWORD dwDynamicClassDomainID;
        return GetGenericsStaticsModuleAndID(&dwDynamicClassDomainID);
    }
    else
    {
        return GetLoaderModule();
    }
}

//==========================================================================================
DWORD  MethodTable::GetModuleDynamicEntryID()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(IsDynamicStatics() && "Only memory reflection emit types and generics can have a dynamic ID");

    if (HasGenericsStaticsInfo())
    {
        DWORD dwDynamicClassDomainID;
        GetGenericsStaticsModuleAndID(&dwDynamicClassDomainID);
        return dwDynamicClassDomainID;
    }
    else
    {
        return GetClass()->GetModuleDynamicID();
    }
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_TYPEEQUIVALENCE
//==========================================================================================
// Equivalence based on Guid and TypeIdentifier attributes to support the "no-PIA" feature.
BOOL MethodTable::IsEquivalentTo_Worker(MethodTable *pOtherMT COMMA_INDEBUG(TypeHandlePairList *pVisited))
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(HasTypeEquivalence() && pOtherMT->HasTypeEquivalence());


#ifdef _DEBUG
    if (TypeHandlePairList::Exists(pVisited, TypeHandle(this), TypeHandle(pOtherMT)))
    {
        _ASSERTE(!"We are in the process of comparing these types already. That should never happen!");
        return TRUE;
    }
    TypeHandlePairList newVisited(TypeHandle(this), TypeHandle(pOtherMT), pVisited);
#endif


    if (HasInstantiation() != pOtherMT->HasInstantiation())
        return FALSE;

    if (IsArray())
    {
        if (!pOtherMT->IsArray() || GetRank() != pOtherMT->GetRank())
            return FALSE;

        if (IsMultiDimArray() != pOtherMT->IsMultiDimArray())
        {
            // A non-multidimensional array is not equivalent to an SzArray.
            // This case is handling the case of a Rank 1 multidimensional array
            // when compared to a normal array.
            return FALSE;
        }

        // arrays of structures have their own unshared MTs and will take this path
        return (GetArrayElementTypeHandle().IsEquivalentTo(pOtherMT->GetArrayElementTypeHandle() COMMA_INDEBUG(&newVisited)));
    }

    return IsEquivalentTo_WorkerInner(pOtherMT COMMA_INDEBUG(&newVisited));
}

//==========================================================================================
// Type equivalence - SO intolerant part.
BOOL MethodTable::IsEquivalentTo_WorkerInner(MethodTable *pOtherMT COMMA_INDEBUG(TypeHandlePairList *pVisited))
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        LOADS_TYPE(CLASS_DEPENDENCIES_LOADED);
    }
    CONTRACTL_END;

    TypeEquivalenceHashTable *typeHashTable = NULL;
    AppDomain *pDomain = GetAppDomain();
    if (pDomain != NULL)
    {
        typeHashTable = pDomain->GetTypeEquivalenceCache();
        TypeEquivalenceHashTable::EquivalenceMatch match = typeHashTable->CheckEquivalence(TypeHandle(this), TypeHandle(pOtherMT));
        switch (match)
        {
        case TypeEquivalenceHashTable::Match:
            return TRUE;
        case TypeEquivalenceHashTable::NoMatch:
            return FALSE;
        case TypeEquivalenceHashTable::MatchUnknown:
            break;
        default:
            _ASSERTE(FALSE);
            break;
        }
    }

    BOOL fEquivalent = FALSE;

    // Check if type is generic
    if (HasInstantiation())
    {
        // Limit equivalence on generics only to interfaces
        if (!IsInterface() || !pOtherMT->IsInterface())
        {
            fEquivalent = FALSE;
            goto EquivalenceCalculated;
        }

        // check whether the instantiations are equivalent
        Instantiation inst1 = GetInstantiation();
        Instantiation inst2 = pOtherMT->GetInstantiation();

        // Verify generic argument count
        if (inst1.GetNumArgs() != inst2.GetNumArgs())
        {
            fEquivalent = FALSE;
            goto EquivalenceCalculated;
        }

        // Verify each generic argument type
        for (DWORD i = 0; i < inst1.GetNumArgs(); i++)
        {
            if (!inst1[i].IsEquivalentTo(inst2[i] COMMA_INDEBUG(pVisited)))
            {
                fEquivalent = FALSE;
                goto EquivalenceCalculated;
            }
        }

        if (GetTypeDefRid() == pOtherMT->GetTypeDefRid() && GetModule() == pOtherMT->GetModule())
        {
            // it's OK to declare the MTs equivalent at this point; the cases we care
            // about are IList<IFoo> and IList<IBar> where IFoo and IBar are equivalent
            fEquivalent = TRUE;
        }
        else
        {
            fEquivalent = FALSE;
        }
        goto EquivalenceCalculated;
    }

    if (IsArray())
    {
        if (!pOtherMT->IsArray() || GetRank() != pOtherMT->GetRank())
        {
            fEquivalent = FALSE;
            goto EquivalenceCalculated;
        }

        // arrays of structures have their own unshared MTs and will take this path
        TypeHandle elementType1 = GetArrayElementTypeHandle();
        TypeHandle elementType2 = pOtherMT->GetArrayElementTypeHandle();
        fEquivalent = elementType1.IsEquivalentTo(elementType2 COMMA_INDEBUG(pVisited));
        goto EquivalenceCalculated;
    }

    fEquivalent = CompareTypeDefsForEquivalence(GetCl(), pOtherMT->GetCl(), GetModule(), pOtherMT->GetModule(), NULL);

EquivalenceCalculated:
    // Record equivalence matches if a table exists
    if (typeHashTable != NULL)
    {
        // Collectible type results will not get cached.
        if ((!Collectible() && !pOtherMT->Collectible()))
        {
            auto match = fEquivalent ? TypeEquivalenceHashTable::Match : TypeEquivalenceHashTable::NoMatch;
            typeHashTable->RecordEquivalence(TypeHandle(this), TypeHandle(pOtherMT), match);
        }
    }

    return fEquivalent;
}
#endif // FEATURE_TYPEEQUIVALENCE

//==========================================================================================
BOOL MethodTable::CanCastToInterface(MethodTable *pTargetMT, TypeHandlePairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(pTargetMT->IsInterface());
        PRECONDITION(IsRestored());
    }
    CONTRACTL_END

    if (!pTargetMT->HasVariance())
    {
        if (IsInterface() && IsEquivalentTo(pTargetMT))
            return TRUE;

        return ImplementsEquivalentInterface(pTargetMT);
    }
    else
    {
        if (CanCastByVarianceToInterfaceOrDelegate(pTargetMT, pVisited))
            return TRUE;

        if (pTargetMT->IsSpecialMarkerTypeForGenericCasting())
            return FALSE; // The special marker types cannot be cast to (at this time, they are the open generic types, so they are however, valid input to this method).

        InterfaceMapIterator it = IterateInterfaceMap();
        while (it.Next())
        {
            if (it.GetInterfaceApprox()->CanCastByVarianceToInterfaceOrDelegate(pTargetMT, pVisited, this))
                return TRUE;
        }
    }
    return FALSE;
}

//==========================================================================================
BOOL MethodTable::CanCastByVarianceToInterfaceOrDelegate(MethodTable *pTargetMT, TypeHandlePairList *pVisited, MethodTable* pMTInterfaceMapOwner /*= NULL*/)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(pTargetMT->HasVariance());
        PRECONDITION(pTargetMT->IsInterface() || pTargetMT->IsDelegate());
        PRECONDITION(IsRestored());
    }
    CONTRACTL_END

    // shortcut when having same types
    if (this == pTargetMT)
    {
        return TRUE;
    }

    // Shortcut for generic approx type scenario
    if (pMTInterfaceMapOwner != NULL &&
        !pMTInterfaceMapOwner->ContainsGenericVariables() &&
        IsSpecialMarkerTypeForGenericCasting() &&
        GetTypeDefRid() == pTargetMT->GetTypeDefRid() &&
        GetModule() == pTargetMT->GetModule() &&
        pTargetMT->GetInstantiation().ContainsAllOneType(pMTInterfaceMapOwner))
    {
        return TRUE;
    }

    if (GetTypeDefRid() != pTargetMT->GetTypeDefRid() || GetModule() != pTargetMT->GetModule() ||
        TypeHandlePairList::Exists(pVisited, this, pTargetMT))
    {
        return FALSE;
    }

    EEClass* pClass = NULL;
    TypeHandlePairList pairList(this, pTargetMT, pVisited);
    BOOL returnValue = FALSE;

    {
        pClass = pTargetMT->GetClass();
        Instantiation inst = GetInstantiation();
        Instantiation targetInst = pTargetMT->GetInstantiation();

        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            TypeHandle thArg = inst[i];
            if (IsSpecialMarkerTypeForGenericCasting() && pMTInterfaceMapOwner && !pMTInterfaceMapOwner->ContainsGenericVariables())
            {
                thArg = pMTInterfaceMapOwner;
            }

            TypeHandle thTargetArg = targetInst[i];

            // If argument types are not equivalent, test them for compatibility
            // in accordance with the variance annotation
            if (!thArg.IsEquivalentTo(thTargetArg))
            {
                switch (pClass->GetVarianceOfTypeParameter(i))
                {
                case gpCovariant :
                    if (!thArg.IsBoxedAndCanCastTo(thTargetArg, &pairList))
                        goto Exit;
                    break;

                case gpContravariant :
                    if (!thTargetArg.IsBoxedAndCanCastTo(thArg, &pairList))
                        goto Exit;
                    break;

                case gpNonVariant :
                    goto Exit;

                default :
                    _ASSERTE(!"Illegal variance annotation");
                    goto Exit;
                }
            }
        }
    }

    returnValue = TRUE;

Exit:

    return returnValue;
}

//==========================================================================================
BOOL MethodTable::CanCastToClass(MethodTable *pTargetMT, TypeHandlePairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(!pTargetMT->IsArray());
        PRECONDITION(!pTargetMT->IsInterface());
    }
    CONTRACTL_END

    MethodTable *pMT = this;

    // If the target type has variant type parameters, we take a slower path
    if (pTargetMT->HasVariance())
    {
        // At present, we support variance only on delegates and interfaces
        CONSISTENCY_CHECK(pTargetMT->IsDelegate());

        // First chase inheritance hierarchy until we hit a class that only differs in its instantiation
        do {
            // Cheap check for equivalence
            if (pMT->IsEquivalentTo(pTargetMT))
                return TRUE;

            if (pMT->CanCastByVarianceToInterfaceOrDelegate(pTargetMT, pVisited))
                return TRUE;

            pMT = pMT->GetParentMethodTable();
        } while (pMT);
    }

    // If there are no variant type parameters, just chase the hierarchy
    else
    {
        do {
            if (pMT->IsEquivalentTo(pTargetMT))
                return TRUE;

            pMT = pMT->GetParentMethodTable();
        } while (pMT);
    }

    return FALSE;
}

#include <optsmallperfcritical.h>

//==========================================================================================
BOOL MethodTable::CanCastTo(MethodTable* pTargetMT, TypeHandlePairList* pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(IsRestored());
    }
    CONTRACTL_END

    // we cannot cache T --> Nullable<T> here since result is contextual.
    // callers should have handled this already according to their rules.
    _ASSERTE(!Nullable::IsNullableForType(TypeHandle(pTargetMT), this));

    if (IsArray())
    {
        if (pTargetMT->IsArray())
        {
            return ArrayIsInstanceOf(pTargetMT, pVisited);
        }
        else if (pTargetMT->IsInterface() && pTargetMT->HasInstantiation())
        {
            return ArraySupportsBizarreInterface(pTargetMT, pVisited);
        }
    }
    else if (pTargetMT->IsArray())
    {
        CastCache::TryAddToCache(this, pTargetMT, false);
        return false;
    }

    BOOL result = pTargetMT->IsInterface() ?
                                CanCastToInterface(pTargetMT, pVisited) :
                                CanCastToClass(pTargetMT, pVisited);

    // We only consider type-based conversion rules here.
    // Therefore a negative result cannot rule out convertibility for ICastable, IDynamicInterfaceCastable, and COM objects
    if (result || !(pTargetMT->IsInterface() && (this->IsComObjectType() || this->IsICastable() || this->IsIDynamicInterfaceCastable())))
    {
        CastCache::TryAddToCache(this, pTargetMT, (BOOL)result);
    }

    return result;
}

//==========================================================================================
BOOL MethodTable::ArraySupportsBizarreInterface(MethodTable * pInterfaceMT, TypeHandlePairList* pVisited)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(this->IsArray());
        PRECONDITION(pInterfaceMT->IsInterface());
        PRECONDITION(pInterfaceMT->HasInstantiation());
    } CONTRACTL_END;

    // IList<T> & IReadOnlyList<T> only supported for SZ_ARRAYS
    if (this->IsMultiDimArray() ||
        !IsImplicitInterfaceOfSZArray(pInterfaceMT))
    {
        CastCache::TryAddToCache(this, pInterfaceMT, FALSE);
        return FALSE;
    }

    BOOL result = TypeDesc::CanCastParam(this->GetArrayElementTypeHandle(), pInterfaceMT->GetInstantiation()[0], pVisited);

    CastCache::TryAddToCache(this, pInterfaceMT, (BOOL)result);
    return result;
}

BOOL MethodTable::ArrayIsInstanceOf(MethodTable* pTargetMT, TypeHandlePairList* pVisited)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(this->IsArray());
        PRECONDITION(pTargetMT->IsArray());
    } CONTRACTL_END;

    // GetRank touches EEClass. Try to avoid it for SZArrays.
    if (pTargetMT->GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY)
    {
        if (this->IsMultiDimArray())
        {
            CastCache::TryAddToCache(this, pTargetMT, FALSE);
            return TypeHandle::CannotCast;
        }
    }
    else
    {
        if (this->GetRank() != pTargetMT->GetRank())
        {
            CastCache::TryAddToCache(this, pTargetMT, FALSE);
            return TypeHandle::CannotCast;
        }
    }
    _ASSERTE(this->GetRank() == pTargetMT->GetRank());

    TypeHandle elementTypeHandle = this->GetArrayElementTypeHandle();
    TypeHandle toElementTypeHandle = pTargetMT->GetArrayElementTypeHandle();

    BOOL result = (elementTypeHandle == toElementTypeHandle) ||
        TypeDesc::CanCastParam(elementTypeHandle, toElementTypeHandle, pVisited);

    CastCache::TryAddToCache(this, pTargetMT, (BOOL)result);
    return result;
}

#include <optdefault.h>

BOOL
MethodTable::IsExternallyVisible()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (IsArray())
    {
        return GetArrayElementTypeHandle().IsExternallyVisible();
    }

    BOOL bIsVisible = IsTypeDefExternallyVisible(GetCl(), GetModule(), GetClass()->GetAttrClass());

    if (bIsVisible && HasInstantiation() && !IsGenericTypeDefinition())
    {
        for (COUNT_T i = 0; i < GetNumGenericArgs(); i++)
        {
            if (!GetInstantiation()[i].IsExternallyVisible())
                return FALSE;
        }
    }

    return bIsVisible;
} // MethodTable::IsExternallyVisible

BOOL MethodTable::IsAllGCPointers()
{
    if (this->ContainsPointers())
    {
        // check for canonical GC encoding for all-pointer types
        CGCDesc* pDesc = CGCDesc::GetCGCDescFromMT(this);
        if (pDesc->GetNumSeries() != 1)
            return false;

        int offsetToData = IsArray() ? ArrayBase::GetDataPtrOffset(this) : sizeof(size_t);
        CGCDescSeries* pSeries = pDesc->GetHighestSeries();
        return ((int)pSeries->GetSeriesOffset() == offsetToData) &&
            ((SSIZE_T)pSeries->GetSeriesSize() == -(SSIZE_T)(offsetToData + sizeof(size_t)));
    }

    return false;
}


#ifdef _DEBUG

void
MethodTable::DebugDumpVtable(LPCUTF8 szClassName, BOOL fDebug)
{
    //diag functions shouldn't affect normal behavior
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    CQuickBytes qb;
    const size_t cchBuff = MAX_CLASSNAME_LENGTH + 30;
    LPSTR buff = fDebug ? (LPSTR) qb.AllocNoThrow(cchBuff * sizeof(CHAR)) : NULL;

    if ((buff == NULL) && fDebug)
    {
        OutputDebugStringUtf8("OOM when dumping VTable - falling back to logging");
        fDebug = FALSE;
    }

    if (fDebug)
    {
        sprintf_s(buff, cchBuff, "Vtable (with interface dupes) for '%s':\n", szClassName);
#ifdef _DEBUG
        sprintf_s(&buff[strlen(buff)], cchBuff - strlen(buff) , "  Total duplicate slots = %d\n", g_dupMethods);
#endif
        OutputDebugStringUtf8(buff);
    }
    else
    {
        //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
        LOG((LF_ALWAYS, LL_ALWAYS, "Vtable (with interface dupes) for '%s':\n", szClassName));
        LOG((LF_ALWAYS, LL_ALWAYS, "  Total duplicate slots = %d\n", g_dupMethods));
    }

    HRESULT hr;
    EX_TRY
    {
        MethodIterator it(this);
        for (; it.IsValid(); it.Next())
        {
            MethodDesc *pMD = it.GetMethodDesc();
            LPCUTF8     pszName = pMD->GetName((USHORT) it.GetSlotNumber());
            DWORD       dwAttrs = pMD->GetAttrs();

            if (fDebug)
            {
                DefineFullyQualifiedNameForClass();
                LPCUTF8 name = GetFullyQualifiedNameForClass(pMD->GetMethodTable());
                sprintf_s(buff, cchBuff,
                           "  slot %2d: %s::%s%s  0x%p (slot = %2d)\n",
                           it.GetSlotNumber(),
                           name,
                           pszName,
                           IsMdFinal(dwAttrs) ? " (final)" : "",
                           (VOID *)pMD->GetMethodEntryPoint(),
                           pMD->GetSlot()
                          );
                OutputDebugStringUtf8(buff);
            }
            else
            {
                //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
                LOG((LF_ALWAYS, LL_ALWAYS,
                     "  slot %2d: %s::%s%s  0x%p (slot = %2d)\n",
                     it.GetSlotNumber(),
                     pMD->GetClass()->GetDebugClassName(),
                     pszName,
                     IsMdFinal(dwAttrs) ? " (final)" : "",
                     (VOID *)pMD->GetMethodEntryPoint(),
                     pMD->GetSlot()
                    ));
            }
            if (it.GetSlotNumber() == (DWORD)(GetNumMethods()-1))
            {
                if (fDebug)
                {
                    OutputDebugStringUtf8("  <-- vtable ends here\n");
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "  <-- vtable ends here\n"));
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    if (fDebug)
    {
        OutputDebugStringUtf8("\n");
    }
    else
    {
        //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
        LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
    }
} // MethodTable::DebugDumpVtable

void
MethodTable::Debug_DumpInterfaceMap(
    LPCSTR szInterfaceMapPrefix)
{
    // Diagnostic functions shouldn't affect normal behavior
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (GetNumInterfaces() == 0)
    {   // There are no interfaces, no point in printing interface map info
        return;
    }

    //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
    LOG((LF_ALWAYS, LL_ALWAYS,
        "%s Interface Map for '%s':\n",
        szInterfaceMapPrefix,
        GetDebugClassName()));
    LOG((LF_ALWAYS, LL_ALWAYS,
        "  Number of interfaces = %d\n",
        GetNumInterfaces()));

    HRESULT hr;
    EX_TRY
    {
        InterfaceMapIterator it(this);
        while (it.Next())
        {
            MethodTable *pInterfaceMT = it.GetInterfaceApprox();

            //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
            LOG((LF_ALWAYS, LL_ALWAYS,
                "  index %2d: %s  0x%p\n",
                it.GetIndex(),
                pInterfaceMT->GetDebugClassName(),
                pInterfaceMT));
        }
        //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
        LOG((LF_ALWAYS, LL_ALWAYS, "  <-- interface map ends here\n"));
    }
    EX_CATCH_HRESULT(hr);

    //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
    LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
} // MethodTable::Debug_DumpInterfaceMap

void
MethodTable::Debug_DumpDispatchMap()
{
    WRAPPER_NO_CONTRACT;   // It's a dev helper, we don't care about contracts

    if (!HasDispatchMap())
    {   // There is no dipstch map for this type, no point in printing the info
        return;
    }

    //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
    LOG((LF_ALWAYS, LL_ALWAYS, "Dispatch Map for '%s':\n", GetDebugClassName()));

    InterfaceInfo_t * pInterfaceMap = GetInterfaceMap();
    DispatchMap::EncodedMapIterator it(this);

    while (it.IsValid())
    {
        DispatchMapEntry *pEntry = it.Entry();

        UINT32 nInterfaceIndex = pEntry->GetTypeID().GetInterfaceNum();
        _ASSERTE(nInterfaceIndex < GetNumInterfaces());

        MethodTable * pInterface = pInterfaceMap[nInterfaceIndex].GetMethodTable();
        UINT32 nInterfaceSlotNumber = pEntry->GetSlotNumber();
        UINT32 nImplementationSlotNumber = pEntry->GetTargetSlotNumber();
        //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
        LOG((LF_ALWAYS, LL_ALWAYS,
            "  Interface %d (%s) slot %d (%s) implemented in slot %d (%s)\n",
            nInterfaceIndex,
            pInterface->GetDebugClassName(),
            nInterfaceSlotNumber,
            pInterface->GetMethodDescForSlot(nInterfaceSlotNumber)->GetName(),
            nImplementationSlotNumber,
            GetMethodDescForSlot(nImplementationSlotNumber)->GetName()));

        it.Next();
    }
    //LF_ALWAYS allowed here because this is controlled by special env var code:EEConfig::ShouldDumpOnClassLoad
    LOG((LF_ALWAYS, LL_ALWAYS, "  <-- Dispatch map ends here\n"));
} // MethodTable::Debug_DumpDispatchMap

#endif //_DEBUG

//==========================================================================================
NOINLINE BOOL MethodTable::ImplementsInterface(MethodTable *pInterface)
{
    WRAPPER_NO_CONTRACT;

    if (pInterface->IsSpecialMarkerTypeForGenericCasting())
        return FALSE; // The special marker types cannot be cast to (at this time, they are the open generic types, so they are however, valid input to this method).

    return ImplementsInterfaceInline(pInterface);
}

//==========================================================================================
BOOL MethodTable::ImplementsEquivalentInterface(MethodTable *pInterface)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pInterface->IsInterface()); // class we are looking up should be an interface
    }
    CONTRACTL_END;

    if (pInterface->IsSpecialMarkerTypeForGenericCasting())
        return FALSE; // The special marker types cannot be cast to (at this time, they are the open generic types, so they are however, valid input to this method).

    // look for exact match first (optimize for success)
    if (ImplementsInterfaceInline(pInterface))
        return TRUE;

    if (!pInterface->HasTypeEquivalence())
        return FALSE;

    DWORD numInterfaces = GetNumInterfaces();
    if (numInterfaces == 0)
        return FALSE;

    InterfaceInfo_t *pInfo = GetInterfaceMap();

    do
    {
        if (pInfo->GetMethodTable()->IsEquivalentTo(pInterface))
            return TRUE;

        pInfo++;
    }
    while (--numInterfaces);

    return FALSE;
}

//==========================================================================================
MethodDesc *MethodTable::GetMethodDescForInterfaceMethod(MethodDesc *pInterfaceMD, BOOL throwOnConflict)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!pInterfaceMD->HasClassOrMethodInstantiation());
    }
    CONTRACTL_END;
    WRAPPER_NO_CONTRACT;

    return GetMethodDescForInterfaceMethod(TypeHandle(pInterfaceMD->GetMethodTable()), pInterfaceMD, throwOnConflict);
}

//==========================================================================================
MethodDesc *MethodTable::GetMethodDescForInterfaceMethod(TypeHandle ownerType, MethodDesc *pInterfaceMD, BOOL throwOnConflict)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!ownerType.IsNull());
        PRECONDITION(ownerType.GetMethodTable()->IsInterface());
        PRECONDITION(ownerType.GetMethodTable()->HasSameTypeDefAs(pInterfaceMD->GetMethodTable()));
        PRECONDITION(IsArray() || ImplementsEquivalentInterface(ownerType.GetMethodTable()) || ownerType.GetMethodTable()->HasVariance());
    }
    CONTRACTL_END;

    MethodDesc *pMD = NULL;

    MethodTable *pInterfaceMT = ownerType.AsMethodTable();

    PCODE pTgt = VirtualCallStubManager::GetTarget(
        pInterfaceMT->GetLoaderAllocator()->GetDispatchToken(pInterfaceMT->GetTypeID(), pInterfaceMD->GetSlot()),
        this, throwOnConflict);
    if (pTgt == NULL)
    {
        _ASSERTE(!throwOnConflict);
        return NULL;
    }
    pMD = MethodTable::GetMethodDescForSlotAddress(pTgt);

#ifdef _DEBUG
    MethodDesc *pDispSlotMD = FindDispatchSlotForInterfaceMD(ownerType, pInterfaceMD, throwOnConflict).GetMethodDesc();
    _ASSERTE(pDispSlotMD == pMD);
#endif // _DEBUG

    pMD->CheckRestore();

    return pMD;
}
#endif // DACCESS_COMPILE

const DWORD EnCFieldIndex = 0x10000000;

//==========================================================================================
PTR_FieldDesc MethodTable::GetFieldDescByIndex(DWORD fieldIndex)
{
    LIMITED_METHOD_CONTRACT;

    // Check if the field index is for an EnC field lookup.
    // See GetIndexForFieldDesc() for when this is applied and why.
    if ((fieldIndex & EnCFieldIndex) == EnCFieldIndex)
    {
        DWORD rid = fieldIndex & ~EnCFieldIndex;
        LOG((LF_ENC, LL_INFO100, "MT:GFDBI: rid:0x%08x\n", rid));

        mdFieldDef tokenToFind = TokenFromRid(rid, mdtFieldDef);
        EncApproxFieldDescIterator fdIterator(
            this,
            ApproxFieldDescIterator::ALL_FIELDS,
            (EncApproxFieldDescIterator::FixUpEncFields | EncApproxFieldDescIterator::OnlyEncFields));
        PTR_FieldDesc pField;
        while ((pField = fdIterator.Next()) != NULL)
        {
            mdFieldDef token = pField->GetMemberDef();
            if (tokenToFind == token)
            {
                LOG((LF_ENC, LL_INFO100, "MT:GFDBI: Found pField:%p\n", pField));
                return pField;
            }
        }

        LOG((LF_ENC, LL_INFO100, "MT:GFDBI: Failed to find rid:0x%08x\n", rid));
        return NULL;
    }

    if (HasGenericsStaticsInfo() &&
        fieldIndex >= GetNumIntroducedInstanceFields())
    {
        return GetGenericsStaticFieldDescs() + (fieldIndex - GetNumIntroducedInstanceFields());
    }
    else
    {
        return GetClass()->GetFieldDescList() + fieldIndex;
    }
}

//==========================================================================================
DWORD MethodTable::GetIndexForFieldDesc(FieldDesc *pField)
{
    LIMITED_METHOD_CONTRACT;

    // EnC methods are not in a location where computing an index through
    // pointer arithmetic is possible. Instead we use the RID and a high
    // bit that is ECMA encodable (that is, < 0x1fffffff) and also doesn't
    // conflict with any other RID (that is, > 0x00ffffff).
    // See FieldDescSlot usage in the JIT interface.
    if (pField->IsEnCNew())
    {
        mdFieldDef tok = pField->GetMemberDef();
        DWORD rid = RidFromToken(tok);
        LOG((LF_ENC, LL_INFO100, "MT:GIFFD: pField:%p rid:0x%08x\n", pField, rid));
        return rid | EnCFieldIndex;
    }

    if (pField->IsStatic() && HasGenericsStaticsInfo())
    {
        FieldDesc *pStaticFields = GetGenericsStaticFieldDescs();

        return GetNumIntroducedInstanceFields() + DWORD(pField - pStaticFields);

    }
    else
    {
        FieldDesc *pFields = GetClass()->GetFieldDescList();

        return DWORD(pField - pFields);
    }
}

//==========================================================================================
#ifdef _MSC_VER
#pragma optimize("t", on)
#endif // _MSC_VER
// compute whether the type can be considered to have had its
// static initialization run without doing anything at all, i.e. whether we know
// immediately that the type requires nothing to do for initialization
//
// If a type used as a representiative during JITting is PreInit then
// any types that it may represent within a code-sharing
// group are also PreInit.   For example, if List<object> is PreInit then List<string>
// and List<MyType> are also PreInit.  This is because the dynamicStatics, staticRefHandles
// and hasCCtor are all identical given a head type, and weakening the domainNeutrality
// to DomainSpecific only makes more types PreInit.
BOOL MethodTable::IsClassPreInited()
{
    LIMITED_METHOD_CONTRACT;

    if (ContainsGenericVariables())
        return TRUE;

    if (HasClassConstructor())
        return FALSE;

    if (HasBoxedRegularStatics())
        return FALSE;

    if (IsDynamicStatics())
        return FALSE;

    return TRUE;
}
#ifdef _MSC_VER
#pragma optimize("", on)
#endif // _MSC_VER

//========================================================================================

namespace
{
    // Does this type have fields that are implicitly defined through repetition and not explicitly defined in metadata?
    bool HasImpliedRepeatedFields(MethodTable* pMT, MethodTable* pFirstFieldValueType = nullptr)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pMT));
        }
        CONTRACTL_END;

        // InlineArray types and fixed buffer types have implied repeated fields.
        // Checking if a type is an InlineArray type is cheap, so we'll do that first.
        if (pMT->GetClass()->IsInlineArray())
        {
            return true;
        }

        DWORD numIntroducedFields = pMT->GetNumIntroducedInstanceFields();
        FieldDesc *pFieldStart = pMT->GetApproxFieldDescListRaw();
        CorElementType firstFieldElementType = pFieldStart->GetFieldType();

        // A fixed buffer type is always a value type that has exactly one value type field at offset 0
        // and who's size is an exact multiple of the size of the field.
        // It is possible that we catch a false positive with this check, but that chance is extremely slim
        // and the user can always change their structure to something more descriptive of what they want
        // instead of adding additional padding at the end of a one-field structure.
        // We do this check here to save looking up the FixedBufferAttribute when loading the field
        // from metadata.
        return numIntroducedFields == 1
                        && ( CorTypeInfo::IsPrimitiveType_NoThrow(firstFieldElementType)
                            || firstFieldElementType == ELEMENT_TYPE_VALUETYPE)
                        && (pFieldStart->GetOffset() == 0)
                        && pMT->HasLayout()
                        && (pMT->GetNumInstanceFieldBytes() % pFieldStart->GetSize(pFirstFieldValueType) == 0);
    }
}

#if defined(UNIX_AMD64_ABI_ITF)

#if defined(_DEBUG) && defined(LOGGING)
static
const char* GetSystemVClassificationTypeName(SystemVClassificationType t)
{
    switch (t)
    {
    case SystemVClassificationTypeUnknown:              return "Unknown";
    case SystemVClassificationTypeStruct:               return "Struct";
    case SystemVClassificationTypeNoClass:              return "NoClass";
    case SystemVClassificationTypeMemory:               return "Memory";
    case SystemVClassificationTypeInteger:              return "Integer";
    case SystemVClassificationTypeIntegerReference:     return "IntegerReference";
    case SystemVClassificationTypeIntegerByRef:         return "IntegerByReference";
    case SystemVClassificationTypeSSE:                  return "SSE";
    default:                                            return "ERROR";
    }
};
#endif // _DEBUG && LOGGING

// Returns 'true' if the struct is passed in registers, 'false' otherwise.
bool MethodTable::ClassifyEightBytes(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel, unsigned int startOffsetOfStruct, bool useNativeLayout, MethodTable** pByValueClassCache)
{
    if (useNativeLayout)
    {
        _ASSERTE(pByValueClassCache == NULL);
        return ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel, startOffsetOfStruct, GetNativeLayoutInfo());
    }
    else
    {
        return ClassifyEightBytesWithManagedLayout(helperPtr, nestingLevel, startOffsetOfStruct, useNativeLayout, pByValueClassCache);
    }
}

// If we have a field classification already, but there is a union, we must merge the classification type of the field. Returns the
// new, merged classification type.
/* static */
static SystemVClassificationType ReClassifyField(SystemVClassificationType originalClassification, SystemVClassificationType newFieldClassification)
{
    _ASSERTE((newFieldClassification == SystemVClassificationTypeInteger) ||
             (newFieldClassification == SystemVClassificationTypeIntegerReference) ||
             (newFieldClassification == SystemVClassificationTypeIntegerByRef) ||
             (newFieldClassification == SystemVClassificationTypeSSE));

    switch (newFieldClassification)
    {
    case SystemVClassificationTypeInteger:
        // Integer overrides everything; the resulting classification is Integer. Can't merge Integer and IntegerReference.
        _ASSERTE((originalClassification == SystemVClassificationTypeInteger) ||
                 (originalClassification == SystemVClassificationTypeSSE));

        return SystemVClassificationTypeInteger;

    case SystemVClassificationTypeSSE:
        // If the old and new classifications are both SSE, then the merge is SSE, otherwise it will be integer. Can't merge SSE and IntegerReference.
        _ASSERTE((originalClassification == SystemVClassificationTypeInteger) ||
                 (originalClassification == SystemVClassificationTypeSSE));

        if (originalClassification == SystemVClassificationTypeSSE)
        {
            return SystemVClassificationTypeSSE;
        }
        else
        {
            return SystemVClassificationTypeInteger;
        }

    case SystemVClassificationTypeIntegerReference:
        // IntegerReference can only merge with IntegerReference.
        _ASSERTE(originalClassification == SystemVClassificationTypeIntegerReference);
        return SystemVClassificationTypeIntegerReference;

    case SystemVClassificationTypeIntegerByRef:
        // IntegerByReference can only merge with IntegerByReference.
        _ASSERTE(originalClassification == SystemVClassificationTypeIntegerByRef);
        return SystemVClassificationTypeIntegerByRef;

    default:
        _ASSERTE(false); // Unexpected type.
        return SystemVClassificationTypeUnknown;
    }
}

static MethodTable* ByValueClassCacheLookup(MethodTable** pByValueClassCache, unsigned index)
{
    LIMITED_METHOD_CONTRACT;
    if (pByValueClassCache == NULL)
        return NULL;
    else
        return pByValueClassCache[index];
}

// Returns 'true' if the struct is passed in registers, 'false' otherwise.
bool MethodTable::ClassifyEightBytesWithManagedLayout(SystemVStructRegisterPassingHelperPtr helperPtr,
                                                     unsigned int nestingLevel,
                                                     unsigned int startOffsetOfStruct,
                                                     bool useNativeLayout,
                                                     MethodTable** pByValueClassCache)
{
    STANDARD_VM_CONTRACT;

    DWORD numIntroducedFields = GetNumIntroducedInstanceFields();

    // It appears the VM gives a struct with no fields of size 1.
    // Don't pass in register such structure.
    if (numIntroducedFields == 0)
    {
        return false;
    }

    // The SIMD Intrinsic types are meant to be handled specially and should not be passed as struct registers
    if (IsIntrinsicType())
    {
        LPCUTF8 namespaceName;
        LPCUTF8 className = GetFullyQualifiedNameInfo(&namespaceName);

        if ((strcmp(className, "Vector512`1") == 0) || (strcmp(className, "Vector256`1") == 0) ||
            (strcmp(className, "Vector128`1") == 0) || (strcmp(className, "Vector64`1") == 0))
        {
            assert(strcmp(namespaceName, "System.Runtime.Intrinsics") == 0);

            LOG((LF_JIT, LL_EVERYTHING, "%*s**** ClassifyEightBytesWithManagedLayout: struct %s is a SIMD intrinsic type; will not be enregistered\n",
                nestingLevel * 5, "", this->GetDebugClassName()));

            return false;
        }

        if ((strcmp(className, "Vector`1") == 0) && (strcmp(namespaceName, "System.Numerics") == 0))
        {
            LOG((LF_JIT, LL_EVERYTHING, "%*s**** ClassifyEightBytesWithManagedLayout: struct %s is a SIMD intrinsic type; will not be enregistered\n",
                nestingLevel * 5, "", this->GetDebugClassName()));

            return false;
        }
    }

#ifdef _DEBUG
    LOG((LF_JIT, LL_EVERYTHING, "%*s**** Classify %s (%p), startOffset %d, total struct size %d\n",
        nestingLevel * 5, "", this->GetDebugClassName(), this, startOffsetOfStruct, helperPtr->structSize));
#endif // _DEBUG

    FieldDesc *pFieldStart = GetApproxFieldDescListRaw();

    bool hasImpliedRepeatedFields = HasImpliedRepeatedFields(this, ByValueClassCacheLookup(pByValueClassCache, 0));

    if (hasImpliedRepeatedFields)
    {
        numIntroducedFields = GetNumInstanceFieldBytes() / pFieldStart->GetSize(ByValueClassCacheLookup(pByValueClassCache, 0));
    }

    for (unsigned int fieldIndex = 0; fieldIndex < numIntroducedFields; fieldIndex++)
    {
        FieldDesc* pField;
        DWORD fieldOffset;
        unsigned int fieldIndexForCacheLookup = fieldIndex;

        if (hasImpliedRepeatedFields)
        {
            pField = pFieldStart;
            fieldIndexForCacheLookup = 0;
            fieldOffset = fieldIndex * pField->GetSize(ByValueClassCacheLookup(pByValueClassCache, fieldIndexForCacheLookup));
        }
        else
        {
            pField = &pFieldStart[fieldIndex];
            fieldOffset = pField->GetOffset();
        }

        unsigned int normalizedFieldOffset = fieldOffset + startOffsetOfStruct;

        unsigned int fieldSize = pField->GetSize(ByValueClassCacheLookup(pByValueClassCache, fieldIndexForCacheLookup));
        _ASSERTE(fieldSize != (unsigned int)-1);

        // The field can't span past the end of the struct.
        if ((normalizedFieldOffset + fieldSize) > helperPtr->structSize)
        {
            _ASSERTE(false && "Invalid struct size. The size of fields and overall size don't agree");
            return false;
        }

        CorElementType fieldType = pField->GetFieldType();
        SystemVClassificationType fieldClassificationType = CorInfoType2UnixAmd64Classification(fieldType);

#ifdef _DEBUG
        LPCUTF8 fieldName;
        pField->GetName_NoThrow(&fieldName);
#endif // _DEBUG
        if (fieldClassificationType == SystemVClassificationTypeStruct)
        {
            TypeHandle th;
            if (pByValueClassCache != NULL)
                th = TypeHandle(pByValueClassCache[fieldIndexForCacheLookup]);
            else
                th = pField->GetApproxFieldTypeHandleThrowing();
            _ASSERTE(!th.IsNull());
            MethodTable* pFieldMT = th.GetMethodTable();

            bool inEmbeddedStructPrev = helperPtr->inEmbeddedStruct;
            helperPtr->inEmbeddedStruct = true;

            bool structRet = false;
            // If classifying for marshaling/PInvoke and the aggregated struct has a native layout
            // use the native classification. If not, continue using the managed layout.
            if (useNativeLayout && pFieldMT->HasLayout())
            {
                structRet = pFieldMT->ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel + 1, normalizedFieldOffset, pFieldMT->GetNativeLayoutInfo());
            }
            else
            {
                structRet = pFieldMT->ClassifyEightBytesWithManagedLayout(helperPtr, nestingLevel + 1, normalizedFieldOffset, useNativeLayout, NULL);
            }

            helperPtr->inEmbeddedStruct = inEmbeddedStructPrev;

            if (!structRet)
            {
                // If the nested struct says not to enregister, there's no need to continue analyzing at this level. Just return do not enregister.
                return false;
            }

            continue;
        }

        if ((normalizedFieldOffset % fieldSize) != 0)
        {
            // The spec requires that struct values on the stack from register passed fields expects
            // those fields to be at their natural alignment.

            LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Field %d %s: offset %d (normalized %d), size %d not at natural alignment; not enregistering struct\n",
                   nestingLevel * 5, "", fieldIndex, fieldName, fieldOffset, normalizedFieldOffset, fieldSize));
            return false;
        }

        if ((int)normalizedFieldOffset <= helperPtr->largestFieldOffset)
        {
            // Find the field corresponding to this offset and update the size if needed.
            // If the offset matches a previously encountered offset, update the classification and field size.
            int i;
            for (i = helperPtr->currentUniqueOffsetField - 1; i >= 0; i--)
            {
                if (helperPtr->fieldOffsets[i] == normalizedFieldOffset)
                {
                    if (fieldSize > helperPtr->fieldSizes[i])
                    {
                        helperPtr->fieldSizes[i] = fieldSize;
                    }

                    helperPtr->fieldClassifications[i] = ReClassifyField(helperPtr->fieldClassifications[i], fieldClassificationType);

                    LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Field %d %s: offset %d (normalized %d), size %d, union with uniqueOffsetField %d, field type classification %s, reclassified field to %s\n",
                           nestingLevel * 5, "", fieldIndex, fieldName, fieldOffset, normalizedFieldOffset, fieldSize, i,
                           GetSystemVClassificationTypeName(fieldClassificationType),
                           GetSystemVClassificationTypeName(helperPtr->fieldClassifications[i])));

                    break;
                }
            }

            if (i >= 0)
            {
                // The proper size of the union set of fields has been set above; continue to the next field.
                continue;
            }
        }
        else
        {
            helperPtr->largestFieldOffset = (int)normalizedFieldOffset;
        }

        // Set the data for a new field.

        // The new field classification must not have been initialized yet.
        _ASSERTE(helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField] == SystemVClassificationTypeNoClass);

        // There are only a few field classifications that are allowed.
        _ASSERTE((fieldClassificationType == SystemVClassificationTypeInteger) ||
                 (fieldClassificationType == SystemVClassificationTypeIntegerReference) ||
                 (fieldClassificationType == SystemVClassificationTypeIntegerByRef) ||
                 (fieldClassificationType == SystemVClassificationTypeSSE));

        helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField] = fieldClassificationType;
        helperPtr->fieldSizes[helperPtr->currentUniqueOffsetField] = fieldSize;
        helperPtr->fieldOffsets[helperPtr->currentUniqueOffsetField] = normalizedFieldOffset;

        LOG((LF_JIT, LL_EVERYTHING, "     %*s**** Field %d %s: offset %d (normalized %d), size %d, currentUniqueOffsetField %d, field type classification %s, chosen field classification %s\n",
               nestingLevel * 5, "", fieldIndex, fieldName, fieldOffset, normalizedFieldOffset, fieldSize, helperPtr->currentUniqueOffsetField,
               GetSystemVClassificationTypeName(fieldClassificationType),
               GetSystemVClassificationTypeName(helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField])));

        _ASSERTE(helperPtr->currentUniqueOffsetField < SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT);
        helperPtr->currentUniqueOffsetField++;
    } // end per-field for loop

    AssignClassifiedEightByteTypes(helperPtr, nestingLevel);

    return true;
}

// Returns 'true' if the struct is passed in registers, 'false' otherwise.
bool MethodTable::ClassifyEightBytesWithNativeLayout(SystemVStructRegisterPassingHelperPtr helperPtr,
                                                    unsigned int nestingLevel,
                                                    unsigned int startOffsetOfStruct,
                                                    EEClassNativeLayoutInfo const* pNativeLayoutInfo)
{
    STANDARD_VM_CONTRACT;

#ifdef DACCESS_COMPILE
    // No register classification for this case.
    return false;
#else // DACCESS_COMPILE

    if (!HasLayout())
    {
        // If there is no native layout for this struct use the managed layout instead.
        return ClassifyEightBytesWithManagedLayout(helperPtr, nestingLevel, startOffsetOfStruct, true, NULL);
    }

    const NativeFieldDescriptor *pNativeFieldDescs = pNativeLayoutInfo->GetNativeFieldDescriptors();
    UINT  numIntroducedFields = pNativeLayoutInfo->GetNumFields();

    // No fields.
    if (numIntroducedFields == 0)
    {
        return false;
    }

    bool hasImpliedRepeatedFields = HasImpliedRepeatedFields(this);

    if (hasImpliedRepeatedFields)
    {
        numIntroducedFields = pNativeLayoutInfo->GetSize() / pNativeFieldDescs->NativeSize();
    }

    // The SIMD Intrinsic types are meant to be handled specially and should not be passed as struct registers
    if (IsIntrinsicType())
    {
        LPCUTF8 namespaceName;
        LPCUTF8 className = GetFullyQualifiedNameInfo(&namespaceName);

        if ((strcmp(className, "Vector512`1") == 0) || (strcmp(className, "Vector256`1") == 0) ||
            (strcmp(className, "Vector128`1") == 0) || (strcmp(className, "Vector64`1") == 0))
        {
            assert(strcmp(namespaceName, "System.Runtime.Intrinsics") == 0);

            LOG((LF_JIT, LL_EVERYTHING, "%*s**** ClassifyEightBytesWithNativeLayout: struct %s is a SIMD intrinsic type; will not be enregistered\n",
                nestingLevel * 5, "", this->GetDebugClassName()));

            return false;
        }

        if ((strcmp(className, "Vector`1") == 0) && (strcmp(namespaceName, "System.Numerics") == 0))
        {
            LOG((LF_JIT, LL_EVERYTHING, "%*s**** ClassifyEightBytesWithNativeLayout: struct %s is a SIMD intrinsic type; will not be enregistered\n",
                nestingLevel * 5, "", this->GetDebugClassName()));

            return false;
        }
    }

#ifdef _DEBUG
    LOG((LF_JIT, LL_EVERYTHING, "%*s**** Classify for native struct %s (%p), startOffset %d, total struct size %d\n",
        nestingLevel * 5, "", this->GetDebugClassName(), this, startOffsetOfStruct, helperPtr->structSize));
#endif // _DEBUG

    for (unsigned int fieldIndex = 0; fieldIndex < numIntroducedFields; fieldIndex++)
    {
        const NativeFieldDescriptor* pNFD;
        if (hasImpliedRepeatedFields)
        {
            // Reuse the first field marshaler for all fields if a fixed buffer.
            pNFD = pNativeFieldDescs;
        }
        else
        {
            pNFD = &pNativeFieldDescs[fieldIndex];
        }

        FieldDesc *pField = pNFD->GetFieldDesc();
        CorElementType fieldType = pField->GetFieldType();

        // Invalid field type.
        if (fieldType == ELEMENT_TYPE_END)
        {
            return false;
        }

        unsigned int fieldNativeSize = pNFD->NativeSize();
        DWORD fieldOffset = pNFD->GetExternalOffset();

        if (hasImpliedRepeatedFields)
        {
            // Since we reuse the NativeFieldDescriptor for fixed buffers, we need to adjust the offset.
            fieldOffset += fieldIndex * fieldNativeSize;
        }

        unsigned normalizedFieldOffset = fieldOffset + startOffsetOfStruct;


        _ASSERTE(fieldNativeSize != (unsigned int)-1);

        // The field can't span past the end of the struct.
        if ((normalizedFieldOffset + fieldNativeSize) > helperPtr->structSize)
        {
            _ASSERTE(false && "Invalid native struct size. The size of fields and overall size don't agree");
            return false;
        }

        SystemVClassificationType fieldClassificationType = SystemVClassificationTypeUnknown;

#ifdef _DEBUG
        LPCUTF8 fieldName;
        pField->GetName_NoThrow(&fieldName);
#endif // _DEBUG

        NativeFieldCategory nfc = pNFD->GetCategory();

        if (nfc == NativeFieldCategory::NESTED)
        {
            unsigned int numElements = pNFD->GetNumElements();
            unsigned int nestedElementOffset = normalizedFieldOffset;

            MethodTable* pFieldMT = pNFD->GetNestedNativeMethodTable();

            if (pFieldMT == nullptr)
            {
                // If there is no method table that represents the native layout, then assume
                // that the type cannot be enregistered.
                return false;
            }

            const unsigned int nestedElementSize = pFieldMT->GetNativeSize();
            for (unsigned int i = 0; i < numElements; ++i, nestedElementOffset += nestedElementSize)
            {
                bool inEmbeddedStructPrev = helperPtr->inEmbeddedStruct;
                helperPtr->inEmbeddedStruct = true;
                bool structRet = pFieldMT->ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel + 1, nestedElementOffset, pFieldMT->GetNativeLayoutInfo());
                helperPtr->inEmbeddedStruct = inEmbeddedStructPrev;

                if (!structRet)
                {
                    // If the nested struct says not to enregister, there's no need to continue analyzing at this level. Just return do not enregister.
                    return false;
                }
            }
            continue;
        }
        else if (nfc == NativeFieldCategory::FLOAT)
        {
            fieldClassificationType = SystemVClassificationTypeSSE;
        }
        else if (nfc == NativeFieldCategory::INTEGER)
        {
            fieldClassificationType = SystemVClassificationTypeInteger;
        }
        else if (nfc == NativeFieldCategory::ILLEGAL)
        {
            return false;
        }
        else
        {
            UNREACHABLE_MSG("Invalid native field subcategory.");
        }

        if ((normalizedFieldOffset % pNFD->AlignmentRequirement()) != 0)
        {
            // The spec requires that struct values on the stack from register passed fields expects
            // those fields to be at their natural alignment.

            LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Native Field %d %s: offset %d (normalized %d), required alignment %d not at natural alignment; not enregistering struct\n",
                nestingLevel * 5, "", fieldIndex, fieldName, fieldOffset, normalizedFieldOffset, pNFD->AlignmentRequirement()));
            return false;
        }

        if ((int)normalizedFieldOffset <= helperPtr->largestFieldOffset)
        {
            // Find the field corresponding to this offset and update the size if needed.
            // If the offset matches a previously encountered offset, update the classification and field size.
            // We do not need to worry about this change incorrectly updating an eightbyte incorrectly
            // by updating a field that spans multiple eightbytes since the only field that does so is a fixed array
            // and a fixed array is represented by an array object in managed, which nothing can share an offset with.
            int i;
            for (i = helperPtr->currentUniqueOffsetField - 1; i >= 0; i--)
            {
                if (helperPtr->fieldOffsets[i] == normalizedFieldOffset)
                {
                    if (fieldNativeSize > helperPtr->fieldSizes[i])
                    {
                        helperPtr->fieldSizes[i] = fieldNativeSize;
                    }

                    helperPtr->fieldClassifications[i] = ReClassifyField(helperPtr->fieldClassifications[i], fieldClassificationType);

                    LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Native Field %d %s: offset %d (normalized %d), native size %d, union with uniqueOffsetField %d, field type classification %s, reclassified field to %s\n",
                        nestingLevel * 5, "", fieldIndex, fieldName, fieldOffset, normalizedFieldOffset, fieldNativeSize, i,
                        GetSystemVClassificationTypeName(fieldClassificationType),
                        GetSystemVClassificationTypeName(helperPtr->fieldClassifications[i])));

                    break;
                }
            }

            if (i >= 0)
            {
                // The proper size of the union set of fields has been set above; continue to the next field.
                continue;
            }
        }
        else
        {
            helperPtr->largestFieldOffset = (int)normalizedFieldOffset;
        }

        // Set the data for a new field.

        // The new field classification must not have been initialized yet.
        _ASSERTE(helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField] == SystemVClassificationTypeNoClass);

        // There are only a few field classifications that are allowed.
        _ASSERTE((fieldClassificationType == SystemVClassificationTypeInteger) ||
            (fieldClassificationType == SystemVClassificationTypeIntegerReference) ||
            (fieldClassificationType == SystemVClassificationTypeIntegerByRef) ||
            (fieldClassificationType == SystemVClassificationTypeSSE));

        helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField] = fieldClassificationType;
        helperPtr->fieldSizes[helperPtr->currentUniqueOffsetField] = fieldNativeSize;
        helperPtr->fieldOffsets[helperPtr->currentUniqueOffsetField] = normalizedFieldOffset;

        LOG((LF_JIT, LL_EVERYTHING, "     %*s**** Native Field %d %s: offset %d (normalized %d), size %d, currentUniqueOffsetField %d, field type classification %s, chosen field classification %s\n",
            nestingLevel * 5, "", fieldIndex, fieldName, fieldOffset, normalizedFieldOffset, fieldNativeSize, helperPtr->currentUniqueOffsetField,
            GetSystemVClassificationTypeName(fieldClassificationType),
            GetSystemVClassificationTypeName(helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField])));

        _ASSERTE(helperPtr->currentUniqueOffsetField < SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT);
        helperPtr->currentUniqueOffsetField++;
    } // end per-field for loop

    AssignClassifiedEightByteTypes(helperPtr, nestingLevel);

    return true;
#endif // DACCESS_COMPILE
}

// Assigns the classification types to the array with eightbyte types.
void  MethodTable::AssignClassifiedEightByteTypes(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel) const
{
    static const size_t CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS = CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS * SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
    static_assert_no_msg(CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS == SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT);

    if (!helperPtr->inEmbeddedStruct)
    {
        _ASSERTE(nestingLevel == 0);

        int largestFieldOffset = helperPtr->largestFieldOffset;
        _ASSERTE(largestFieldOffset != -1);

        // We're at the top level of the recursion, and we're done looking at the fields.
        // Now sort the fields by offset and set the output data.

        int sortedFieldOrder[CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS];
        for (unsigned i = 0; i < CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS; i++)
        {
            sortedFieldOrder[i] = -1;
        }

        unsigned numFields = helperPtr->currentUniqueOffsetField;
        for (unsigned i = 0; i < numFields; i++)
        {
            _ASSERTE(helperPtr->fieldOffsets[i] < CLR_SYSTEMV_MAX_BYTES_TO_PASS_IN_REGISTERS);
            _ASSERTE(sortedFieldOrder[helperPtr->fieldOffsets[i]] == -1); // we haven't seen this field offset yet.
            sortedFieldOrder[helperPtr->fieldOffsets[i]] = i;
        }

        // Calculate the eightbytes and their types.

        int lastFieldOrdinal = sortedFieldOrder[largestFieldOffset];
        unsigned int offsetAfterLastFieldByte = largestFieldOffset + helperPtr->fieldSizes[lastFieldOrdinal];
        SystemVClassificationType lastFieldClassification = helperPtr->fieldClassifications[lastFieldOrdinal];

        unsigned int usedEightBytes = 0;
        unsigned int accumulatedSizeForEightBytes = 0;
        bool foundFieldInEightByte = false;
        for (unsigned int offset = 0; offset < helperPtr->structSize; offset++)
        {
            SystemVClassificationType fieldClassificationType;
            unsigned int fieldSize = 0;

            int ordinal = sortedFieldOrder[offset];
            if (ordinal == -1)
            {
                if (offset < accumulatedSizeForEightBytes)
                {
                    // We're within a field and there is not an overlapping field that starts here.
                    // There's no work we need to do, so go to the next loop iteration.
                    continue;
                }

                // If there is no field that starts as this offset and we are not within another field,
                // treat its contents as padding.
                // Any padding that follows the last field receives the same classification as the
                // last field; padding between fields receives the NO_CLASS classification as per
                // the SysV ABI spec.
                fieldSize = 1;
                fieldClassificationType = offset < offsetAfterLastFieldByte ? SystemVClassificationTypeNoClass : lastFieldClassification;
            }
            else
            {
                foundFieldInEightByte = true;
                fieldSize = helperPtr->fieldSizes[ordinal];
                _ASSERTE(fieldSize > 0);

                fieldClassificationType = helperPtr->fieldClassifications[ordinal];
                _ASSERTE(fieldClassificationType != SystemVClassificationTypeMemory && fieldClassificationType != SystemVClassificationTypeUnknown);
            }

            unsigned int fieldStartEightByte = offset / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
            unsigned int fieldEndEightByte = (offset + fieldSize - 1) / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;

            _ASSERTE(fieldEndEightByte < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

            usedEightBytes = Max(usedEightBytes, fieldEndEightByte + 1);

            for (unsigned int currentFieldEightByte = fieldStartEightByte; currentFieldEightByte <= fieldEndEightByte; currentFieldEightByte++)
            {
                if (helperPtr->eightByteClassifications[currentFieldEightByte] == fieldClassificationType)
                {
                    // Do nothing. The eight-byte already has this classification.
                }
                else if (helperPtr->eightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeNoClass)
                {
                    helperPtr->eightByteClassifications[currentFieldEightByte] = fieldClassificationType;
                }
                else if ((helperPtr->eightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeInteger) ||
                    (fieldClassificationType == SystemVClassificationTypeInteger))
                {
                    _ASSERTE((fieldClassificationType != SystemVClassificationTypeIntegerReference) &&
                        (fieldClassificationType != SystemVClassificationTypeIntegerByRef));

                    helperPtr->eightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeInteger;
                }
                else if ((helperPtr->eightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeIntegerReference) ||
                    (fieldClassificationType == SystemVClassificationTypeIntegerReference))
                {
                    helperPtr->eightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeIntegerReference;
                }
                else if ((helperPtr->eightByteClassifications[currentFieldEightByte] == SystemVClassificationTypeIntegerByRef) ||
                    (fieldClassificationType == SystemVClassificationTypeIntegerByRef))
                {
                    helperPtr->eightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeIntegerByRef;
                }
                else
                {
                    helperPtr->eightByteClassifications[currentFieldEightByte] = SystemVClassificationTypeSSE;
                }
            }

            if ((offset + 1) % SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES == 0) // If we just finished checking the last byte of an eightbyte
            {
                if (!foundFieldInEightByte)
                {
                    // If we didn't find a field in an eight-byte (i.e. there are no explicit offsets that start a field in this eightbyte)
                    // then the classification of this eightbyte might be NoClass. We can't hand a classification of NoClass to the JIT
                    // so set the class to Integer (as though the struct has a char[8] padding) if the class is NoClass.
                    if (helperPtr->eightByteClassifications[offset / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES] == SystemVClassificationTypeNoClass)
                    {
                        helperPtr->eightByteClassifications[offset / SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES] = SystemVClassificationTypeInteger;
                    }
                }

                foundFieldInEightByte = false;
            }

            accumulatedSizeForEightBytes = Max(accumulatedSizeForEightBytes, offset + fieldSize);
        }

        for (unsigned int currentEightByte = 0; currentEightByte < usedEightBytes; currentEightByte++)
        {
            unsigned int eightByteSize = accumulatedSizeForEightBytes < (SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES * (currentEightByte + 1))
                ? accumulatedSizeForEightBytes % SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES
                :   SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;

            // Save data for this eightbyte.
            helperPtr->eightByteSizes[currentEightByte] = eightByteSize;
            helperPtr->eightByteOffsets[currentEightByte] = currentEightByte * SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
        }

        helperPtr->eightByteCount = usedEightBytes;

        _ASSERTE(helperPtr->eightByteCount <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

#ifdef _DEBUG
        LOG((LF_JIT, LL_EVERYTHING, "     ----\n"));
        LOG((LF_JIT, LL_EVERYTHING, "     **** Number EightBytes: %d\n", helperPtr->eightByteCount));
        for (unsigned i = 0; i < helperPtr->eightByteCount; i++)
        {
            _ASSERTE(helperPtr->eightByteClassifications[i] != SystemVClassificationTypeNoClass);
            LOG((LF_JIT, LL_EVERYTHING, "     **** eightByte %d -- classType: %s, eightByteOffset: %d, eightByteSize: %d\n",
                i, GetSystemVClassificationTypeName(helperPtr->eightByteClassifications[i]), helperPtr->eightByteOffsets[i], helperPtr->eightByteSizes[i]));
        }
#endif // _DEBUG
    }
}

#endif // defined(UNIX_AMD64_ABI_ITF)

#if defined(TARGET_LOONGARCH64)

bool MethodTable::IsLoongArch64OnlyOneField(MethodTable * pMT)
{
    TypeHandle th(pMT);

    bool useNativeLayout      = false;
    bool ret                  = false;
    MethodTable* pMethodTable = nullptr;

    if (!th.IsTypeDesc())
    {
        pMethodTable = th.AsMethodTable();
        if (pMethodTable->HasLayout())
        {
            useNativeLayout = true;
        }
        else if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNumIntroducedInstanceFields();

            if (numIntroducedFields == 1)
            {
                FieldDesc *pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart[0].GetFieldType();

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    ret = true;
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldStart->GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    if (pMethodTable->GetNumIntroducedInstanceFields() == 1)
                    {
                        ret = IsLoongArch64OnlyOneField(pMethodTable);
                    }
                }
            }
            goto _End_arg;
        }
    }
    else
    {
        _ASSERTE(th.IsNativeValueType());

        useNativeLayout = true;
        pMethodTable = th.AsNativeValueType();
    }
    _ASSERTE(pMethodTable != nullptr);

    if (useNativeLayout)
    {
        if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNativeLayoutInfo()->GetNumFields();
            FieldDesc *pFieldStart = nullptr;

            if (numIntroducedFields == 1)
            {
                pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart->GetFieldType();

                // InlineArray types and fixed buffer types have implied repeated fields.
                // Checking if a type is an InlineArray type is cheap, so we'll do that first.
                bool hasImpliedRepeatedFields = HasImpliedRepeatedFields(pMethodTable);

                if (hasImpliedRepeatedFields)
                {
                    numIntroducedFields = pMethodTable->GetNumInstanceFieldBytes() / pFieldStart->GetSize();
                    if (numIntroducedFields != 1)
                    {
                        goto _End_arg;
                    }
                }

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    ret = true;
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();
                    NativeFieldCategory nfc = pNativeFieldDescs->GetCategory();
                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        pMethodTable = pNativeFieldDescs->GetNestedNativeMethodTable();
                        ret = IsLoongArch64OnlyOneField(pMethodTable);
                    }
                    else if (nfc != NativeFieldCategory::ILLEGAL)
                    {
                        ret = true;
                    }
                }
            }
            else
            {
                ret = false;
            }
        }
    }
_End_arg:

    return ret;
}

int MethodTable::GetLoongArch64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE cls)
{
    TypeHandle th(cls);

    bool useNativeLayout           = false;
    int size = STRUCT_NO_FLOAT_FIELD;
    MethodTable* pMethodTable      = nullptr;

    if (!th.IsTypeDesc())
    {
        pMethodTable = th.AsMethodTable();
        if (pMethodTable->HasLayout())
        {
            useNativeLayout = true;
        }
        else if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNumIntroducedInstanceFields();

            if (numIntroducedFields == 1)
            {
                FieldDesc *pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart[0].GetFieldType();

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldStart->GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    size = GetLoongArch64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                }
            }
            else if (numIntroducedFields == 2)
            {
                FieldDesc *pFieldSecond;
                FieldDesc *pFieldFirst = pMethodTable->GetApproxFieldDescListRaw();
                if (pFieldFirst->GetOffset() == 0)
                {
                    pFieldSecond = pFieldFirst + 1;
                }
                else
                {
                    pFieldSecond = pFieldFirst;
                    pFieldFirst  = pFieldFirst + 1;
                }
                assert(pFieldFirst->GetOffset() == 0);

                if (pFieldFirst->GetSize() > 8)
                {
                    goto _End_arg;
                }

                CorElementType fieldType = pFieldFirst[0].GetFieldType();
                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_FIRST;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FIRST_FIELD_DOUBLE;
                    }
                    else if (pFieldFirst[0].GetSize() == 8)
                    {
                        size = STRUCT_FIRST_FIELD_SIZE_IS8;
                    }

                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldFirst->GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    if (IsLoongArch64OnlyOneField(pMethodTable))
                    {
                        size = GetLoongArch64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                        if ((size & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            size = pFieldFirst[0].GetSize() == 8 ? STRUCT_FIRST_FIELD_DOUBLE : STRUCT_FLOAT_FIELD_FIRST;
                        }
                        else if (size == STRUCT_NO_FLOAT_FIELD)
                        {
                            size = pFieldFirst[0].GetSize() == 8 ? STRUCT_FIRST_FIELD_SIZE_IS8: 0;
                        }
                        else
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }
                    }
                    else
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                        goto _End_arg;
                    }
                }
                else if (pFieldFirst[0].GetSize() == 8)
                {
                    size = STRUCT_FIRST_FIELD_SIZE_IS8;
                }

                fieldType = pFieldSecond[0].GetFieldType();
                if (pFieldSecond[0].GetSize() > 8)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                    goto _End_arg;
                }
                else if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                    }
                    else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                    else if (pFieldSecond[0].GetSize() == 8)
                    {
                        size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldSecond[0].GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    if (IsLoongArch64OnlyOneField(pMethodTable))
                    {
                        int size2 = GetLoongArch64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                        if ((size2 & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            if (pFieldSecond[0].GetSize() == 8)
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                            }
                            else
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                            }
                        }
                        else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (size2 == STRUCT_NO_FLOAT_FIELD)
                        {
                            size |= pFieldSecond[0].GetSize() == 8 ? STRUCT_SECOND_FIELD_SIZE_IS8 : 0;
                        }
                        else
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                    }
                    else
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                }
                else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                }
                else if (pFieldSecond[0].GetSize() == 8)
                {
                    size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                }
            }

            goto _End_arg;
        }
    }
    else
    {
        _ASSERTE(th.IsNativeValueType());

        useNativeLayout = true;
        pMethodTable = th.AsNativeValueType();
    }
    _ASSERTE(pMethodTable != nullptr);

    if (useNativeLayout)
    {
        if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNativeLayoutInfo()->GetNumFields();
            FieldDesc *pFieldStart = nullptr;

            if (numIntroducedFields == 1)
            {
                pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart->GetFieldType();

                // InlineArray types and fixed buffer types have implied repeated fields.
                // Checking if a type is an InlineArray type is cheap, so we'll do that first.
                bool hasImpliedRepeatedFields = HasImpliedRepeatedFields(pMethodTable);

                if (hasImpliedRepeatedFields)
                {
                    numIntroducedFields = pMethodTable->GetNumInstanceFieldBytes() / pFieldStart->GetSize();
                    if (numIntroducedFields > 2)
                    {
                        goto _End_arg;
                    }

                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        if (numIntroducedFields == 1)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                        }
                        else if (numIntroducedFields == 2)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_TWO;
                        }
                        goto _End_arg;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        if (numIntroducedFields == 1)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                        }
                        else if (numIntroducedFields == 2)
                        {
                            size = STRUCT_FIELD_TWO_DOUBLES;
                        }
                        goto _End_arg;
                    }
                }

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();
                    NativeFieldCategory nfc = pNativeFieldDescs->GetCategory();
                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        pMethodTable = pNativeFieldDescs->GetNestedNativeMethodTable();
                        size = GetLoongArch64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                        return size;
                    }
                    else if (nfc == NativeFieldCategory::FLOAT)
                    {
                        if (pFieldStart->GetSize() == 4)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                        }
                        else if (pFieldStart->GetSize() == 8)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                        }
                    }
                }
            }
            else if (numIntroducedFields == 2)
            {
                pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                if (pFieldStart->GetSize() > 8)
                {
                    goto _End_arg;
                }

                if (pFieldStart->GetOffset() || !pFieldStart[1].GetOffset() || (pFieldStart[0].GetSize() > pFieldStart[1].GetOffset()))
                {
                    goto _End_arg;
                }

                CorElementType fieldType = pFieldStart[0].GetFieldType();
                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_FIRST;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FIRST_FIELD_DOUBLE;
                    }
                    else if (pFieldStart[0].GetSize() == 8)
                    {
                        size = STRUCT_FIRST_FIELD_SIZE_IS8;
                    }

                    fieldType = pFieldStart[1].GetFieldType();
                    if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                    {
                        if (fieldType == ELEMENT_TYPE_R4)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                        }
                        else if (fieldType == ELEMENT_TYPE_R8)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                        }
                        else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (pFieldStart[1].GetSize() == 8)
                        {
                            size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                        }
                        goto _End_arg;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();

                    NativeFieldCategory nfc = pNativeFieldDescs->GetCategory();

                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        if (pNativeFieldDescs->GetNumElements() != 1)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        MethodTable* pMethodTable2 = pNativeFieldDescs->GetNestedNativeMethodTable();

                        if (!IsLoongArch64OnlyOneField(pMethodTable2))
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        size = GetLoongArch64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable2);
                        if ((size & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            if (pFieldStart->GetSize() == 8)
                            {
                                size = STRUCT_FIRST_FIELD_DOUBLE;
                            }
                            else
                            {
                                size = STRUCT_FLOAT_FIELD_FIRST;
                            }
                        }
                        else if (pFieldStart->GetSize() == 8)
                        {
                            size = STRUCT_FIRST_FIELD_SIZE_IS8;
                        }
                        else
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }
                    }
                    else if (nfc == NativeFieldCategory::FLOAT)
                    {
                        if (pFieldStart[0].GetSize() == 4)
                        {
                            size = STRUCT_FLOAT_FIELD_FIRST;
                        }
                        else if (pFieldStart[0].GetSize() == 8)
                        {
                            _ASSERTE((pMethodTable->GetNativeSize() == 8) || (pMethodTable->GetNativeSize() == 16));
                            size = STRUCT_FIRST_FIELD_DOUBLE;
                        }
                    }
                    else if (pFieldStart[0].GetSize() == 8)
                    {
                        size = STRUCT_FIRST_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_CLASS)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                    goto _End_arg;
                }
                else if (pFieldStart[0].GetSize() == 8)
                {
                    size = STRUCT_FIRST_FIELD_SIZE_IS8;
                }

                fieldType = pFieldStart[1].GetFieldType();
                if (pFieldStart[1].GetSize() > 8)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                    goto _End_arg;
                }
                else if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                    }
                    else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                    else if (pFieldStart[1].GetSize() == 8)
                    {
                        size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();
                    NativeFieldCategory nfc = pNativeFieldDescs[1].GetCategory();

                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        if (pNativeFieldDescs[1].GetNumElements() != 1)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        MethodTable* pMethodTable2 = pNativeFieldDescs[1].GetNestedNativeMethodTable();

                        if (!IsLoongArch64OnlyOneField(pMethodTable2))
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        if ((GetLoongArch64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable2) & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            if (pFieldStart[1].GetSize() == 4)
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                            }
                            else if (pFieldStart[1].GetSize() == 8)
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                            }
                        }
                        else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (pFieldStart[1].GetSize() == 8)
                        {
                            size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                        }
                    }
                    else if (nfc == NativeFieldCategory::FLOAT)
                    {
                        if (pFieldStart[1].GetSize() == 4)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                        }
                        else if (pFieldStart[1].GetSize() == 8)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                        }
                    }
                    else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                    else if (pFieldStart[1].GetSize() == 8)
                    {
                        size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_CLASS)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                    goto _End_arg;
                }
                else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                }
                else if (pFieldStart[1].GetSize() == 8)
                {
                    size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                }
            }
        }
    }
_End_arg:

    return size;
}
#endif

#if defined(TARGET_RISCV64)

bool MethodTable::IsRiscV64OnlyOneField(MethodTable * pMT)
{
    TypeHandle th(pMT);

    bool useNativeLayout      = false;
    bool ret                  = false;
    MethodTable* pMethodTable = nullptr;

    if (!th.IsTypeDesc())
    {
        pMethodTable = th.AsMethodTable();
        if (pMethodTable->HasLayout())
        {
            useNativeLayout = true;
        }
        else if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNumIntroducedInstanceFields();

            if (numIntroducedFields == 1)
            {
                FieldDesc *pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart[0].GetFieldType();

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    ret = true;
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldStart->GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    if (pMethodTable->GetNumIntroducedInstanceFields() == 1)
                    {
                        ret = IsRiscV64OnlyOneField(pMethodTable);
                    }
                }
            }
            goto _End_arg;
        }
    }
    else
    {
        _ASSERTE(th.IsNativeValueType());

        useNativeLayout = true;
        pMethodTable = th.AsNativeValueType();
    }
    _ASSERTE(pMethodTable != nullptr);

    if (useNativeLayout)
    {
        if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNativeLayoutInfo()->GetNumFields();
            FieldDesc *pFieldStart = nullptr;

            if (numIntroducedFields == 1)
            {
                pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart->GetFieldType();

                // InlineArray types and fixed buffer types have implied repeated fields.
                // Checking if a type is an InlineArray type is cheap, so we'll do that first.
                bool hasImpliedRepeatedFields = HasImpliedRepeatedFields(pMethodTable);

                if (hasImpliedRepeatedFields)
                {
                    numIntroducedFields = pMethodTable->GetNumInstanceFieldBytes() / pFieldStart->GetSize();
                    if (numIntroducedFields != 1)
                    {
                        goto _End_arg;
                    }
                }

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    ret = true;
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();
                    NativeFieldCategory nfc = pNativeFieldDescs->GetCategory();
                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        pMethodTable = pNativeFieldDescs->GetNestedNativeMethodTable();
                        ret = IsRiscV64OnlyOneField(pMethodTable);
                    }
                    else if (nfc != NativeFieldCategory::ILLEGAL)
                    {
                        ret = true;
                    }
                }
            }
            else
            {
                ret = false;
            }
        }
    }
_End_arg:

    return ret;
}

int MethodTable::GetRiscV64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE cls)
{
    TypeHandle th(cls);

    bool useNativeLayout           = false;
    int size = STRUCT_NO_FLOAT_FIELD;
    MethodTable* pMethodTable      = nullptr;

    if (!th.IsTypeDesc())
    {
        pMethodTable = th.AsMethodTable();
        if (pMethodTable->HasLayout())
        {
            useNativeLayout = true;
        }
        else if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNumIntroducedInstanceFields();

            if (numIntroducedFields == 1)
            {
                FieldDesc *pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart[0].GetFieldType();

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldStart->GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    size = GetRiscV64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                }
            }
            else if (numIntroducedFields == 2)
            {
                FieldDesc *pFieldSecond;
                FieldDesc *pFieldFirst = pMethodTable->GetApproxFieldDescListRaw();
                if (pFieldFirst->GetOffset() == 0)
                {
                    pFieldSecond = pFieldFirst + 1;
                }
                else
                {
                    pFieldSecond = pFieldFirst;
                    pFieldFirst  = pFieldFirst + 1;
                }
                assert(pFieldFirst->GetOffset() == 0);

                if (pFieldFirst->GetSize() > 8)
                {
                    goto _End_arg;
                }

                CorElementType fieldType = pFieldFirst[0].GetFieldType();
                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_FIRST;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FIRST_FIELD_DOUBLE;
                    }
                    else if (pFieldFirst[0].GetSize() == 8)
                    {
                        size = STRUCT_FIRST_FIELD_SIZE_IS8;
                    }

                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldFirst->GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    if (IsRiscV64OnlyOneField(pMethodTable))
                    {
                        size = GetRiscV64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                        if ((size & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            size = pFieldFirst[0].GetSize() == 8 ? STRUCT_FIRST_FIELD_DOUBLE : STRUCT_FLOAT_FIELD_FIRST;
                        }
                        else if (size == STRUCT_NO_FLOAT_FIELD)
                        {
                            size = pFieldFirst[0].GetSize() == 8 ? STRUCT_FIRST_FIELD_SIZE_IS8: 0;
                        }
                        else
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }
                    }
                    else
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                        goto _End_arg;
                    }
                }
                else if (pFieldFirst[0].GetSize() == 8)
                {
                    size = STRUCT_FIRST_FIELD_SIZE_IS8;
                }

                fieldType = pFieldSecond[0].GetFieldType();
                if (pFieldSecond[0].GetSize() > 8)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                    goto _End_arg;
                }
                else if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                    }
                    else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                    else if (pFieldSecond[0].GetSize() == 8)
                    {
                        size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    pMethodTable  = pFieldSecond[0].GetApproxFieldTypeHandleThrowing().GetMethodTable();
                    if (IsRiscV64OnlyOneField(pMethodTable))
                    {
                        int size2 = GetRiscV64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                        if ((size2 & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            if (pFieldSecond[0].GetSize() == 8)
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                            }
                            else
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                            }
                        }
                        else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (size2 == STRUCT_NO_FLOAT_FIELD)
                        {
                            size |= pFieldSecond[0].GetSize() == 8 ? STRUCT_SECOND_FIELD_SIZE_IS8 : 0;
                        }
                        else
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                    }
                    else
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                }
                else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                }
                else if (pFieldSecond[0].GetSize() == 8)
                {
                    size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                }
            }

            goto _End_arg;
        }
    }
    else
    {
        _ASSERTE(th.IsNativeValueType());

        useNativeLayout = true;
        pMethodTable = th.AsNativeValueType();
    }
    _ASSERTE(pMethodTable != nullptr);

    if (useNativeLayout)
    {
        if (th.GetSize() <= 16 /*MAX_PASS_MULTIREG_BYTES*/)
        {
            DWORD numIntroducedFields = pMethodTable->GetNativeLayoutInfo()->GetNumFields();
            FieldDesc *pFieldStart = nullptr;

            if (numIntroducedFields == 1)
            {
                pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                CorElementType fieldType = pFieldStart->GetFieldType();

                // InlineArray types and fixed buffer types have implied repeated fields.
                // Checking if a type is an InlineArray type is cheap, so we'll do that first.
                bool hasImpliedRepeatedFields = HasImpliedRepeatedFields(pMethodTable);

                if (hasImpliedRepeatedFields)
                {
                    numIntroducedFields = pMethodTable->GetNumInstanceFieldBytes() / pFieldStart->GetSize();
                    if (numIntroducedFields > 2)
                    {
                        goto _End_arg;
                    }

                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        if (numIntroducedFields == 1)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                        }
                        else if (numIntroducedFields == 2)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_TWO;
                        }
                        goto _End_arg;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        if (numIntroducedFields == 1)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                        }
                        else if (numIntroducedFields == 2)
                        {
                            size = STRUCT_FIELD_TWO_DOUBLES;
                        }
                        goto _End_arg;
                    }
                }

                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();
                    NativeFieldCategory nfc = pNativeFieldDescs->GetCategory();
                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        pMethodTable = pNativeFieldDescs->GetNestedNativeMethodTable();
                        size = GetRiscV64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable);
                        return size;
                    }
                    else if (nfc == NativeFieldCategory::FLOAT)
                    {
                        if (pFieldStart->GetSize() == 4)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE;
                        }
                        else if (pFieldStart->GetSize() == 8)
                        {
                            size = STRUCT_FLOAT_FIELD_ONLY_ONE | STRUCT_FIRST_FIELD_SIZE_IS8;
                        }
                    }
                }
            }
            else if (numIntroducedFields == 2)
            {
                pFieldStart = pMethodTable->GetApproxFieldDescListRaw();

                if (pFieldStart->GetSize() > 8)
                {
                    goto _End_arg;
                }

                if (pFieldStart->GetOffset() || !pFieldStart[1].GetOffset() || (pFieldStart[0].GetSize() > pFieldStart[1].GetOffset()))
                {
                    goto _End_arg;
                }

                CorElementType fieldType = pFieldStart[0].GetFieldType();
                if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = STRUCT_FLOAT_FIELD_FIRST;
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = STRUCT_FIRST_FIELD_DOUBLE;
                    }
                    else if (pFieldStart[0].GetSize() == 8)
                    {
                        size = STRUCT_FIRST_FIELD_SIZE_IS8;
                    }

                    fieldType = pFieldStart[1].GetFieldType();
                    if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                    {
                        if (fieldType == ELEMENT_TYPE_R4)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                        }
                        else if (fieldType == ELEMENT_TYPE_R8)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                        }
                        else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (pFieldStart[1].GetSize() == 8)
                        {
                            size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                        }
                        goto _End_arg;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();

                    NativeFieldCategory nfc = pNativeFieldDescs->GetCategory();

                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        if (pNativeFieldDescs->GetNumElements() != 1)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        MethodTable* pMethodTable2 = pNativeFieldDescs->GetNestedNativeMethodTable();

                        if (!IsRiscV64OnlyOneField(pMethodTable2))
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        size = GetRiscV64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable2);
                        if ((size & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            if (pFieldStart->GetSize() == 8)
                            {
                                size = STRUCT_FIRST_FIELD_DOUBLE;
                            }
                            else
                            {
                                size = STRUCT_FLOAT_FIELD_FIRST;
                            }
                        }
                        else if (pFieldStart->GetSize() == 8)
                        {
                            size = STRUCT_FIRST_FIELD_SIZE_IS8;
                        }
                        else
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }
                    }
                    else if (nfc == NativeFieldCategory::FLOAT)
                    {
                        if (pFieldStart[0].GetSize() == 4)
                        {
                            size = STRUCT_FLOAT_FIELD_FIRST;
                        }
                        else if (pFieldStart[0].GetSize() == 8)
                        {
                            _ASSERTE((pMethodTable->GetNativeSize() == 8) || (pMethodTable->GetNativeSize() == 16));
                            size = STRUCT_FIRST_FIELD_DOUBLE;
                        }
                    }
                    else if (pFieldStart[0].GetSize() == 8)
                    {
                        size = STRUCT_FIRST_FIELD_SIZE_IS8;
                    }
                }
                else if (pFieldStart[0].GetSize() == 8)
                {
                    size = STRUCT_FIRST_FIELD_SIZE_IS8;
                }

                fieldType = pFieldStart[1].GetFieldType();
                if (pFieldStart[1].GetSize() > 8)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                    goto _End_arg;
                }
                else if (CorTypeInfo::IsPrimitiveType_NoThrow(fieldType))
                {
                    if (fieldType == ELEMENT_TYPE_R4)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                    }
                    else if (fieldType == ELEMENT_TYPE_R8)
                    {
                        size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                    }
                    else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                    else if (pFieldStart[1].GetSize() == 8)
                    {
                        size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                    }

                    // Pass with two integer registers in `struct {int a, int b, float/double c}` cases
                    if ((size | STRUCT_FIRST_FIELD_SIZE_IS8 | STRUCT_FLOAT_FIELD_SECOND) == size)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                }
                else if (fieldType == ELEMENT_TYPE_VALUETYPE)
                {
                    const NativeFieldDescriptor *pNativeFieldDescs = pMethodTable->GetNativeLayoutInfo()->GetNativeFieldDescriptors();
                    NativeFieldCategory nfc = pNativeFieldDescs[1].GetCategory();

                    if (nfc == NativeFieldCategory::NESTED)
                    {
                        if (pNativeFieldDescs[1].GetNumElements() != 1)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        MethodTable* pMethodTable2 = pNativeFieldDescs[1].GetNestedNativeMethodTable();

                        if (!IsRiscV64OnlyOneField(pMethodTable2))
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                            goto _End_arg;
                        }

                        if ((GetRiscV64PassStructInRegisterFlags((CORINFO_CLASS_HANDLE)pMethodTable2) & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
                        {
                            if (pFieldStart[1].GetSize() == 4)
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                            }
                            else if (pFieldStart[1].GetSize() == 8)
                            {
                                size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                            }
                        }
                        else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                        {
                            size = STRUCT_NO_FLOAT_FIELD;
                        }
                        else if (pFieldStart[1].GetSize() == 8)
                        {
                            size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                        }
                    }
                    else if (nfc == NativeFieldCategory::FLOAT)
                    {
                        if (pFieldStart[1].GetSize() == 4)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND) : (size | STRUCT_FLOAT_FIELD_SECOND);
                        }
                        else if (pFieldStart[1].GetSize() == 8)
                        {
                            size = size & STRUCT_FLOAT_FIELD_FIRST ? (size ^ STRUCT_MERGE_FIRST_SECOND_8) : (size | STRUCT_SECOND_FIELD_DOUBLE);
                        }
                    }
                    else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                    {
                        size = STRUCT_NO_FLOAT_FIELD;
                    }
                    else if (pFieldStart[1].GetSize() == 8)
                    {
                        size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                    }
                }
                else if ((size & STRUCT_FLOAT_FIELD_FIRST) == 0)
                {
                    size = STRUCT_NO_FLOAT_FIELD;
                }
                else if (pFieldStart[1].GetSize() == 8)
                {
                    size |= STRUCT_SECOND_FIELD_SIZE_IS8;
                }
            }
        }
    }
_End_arg:

    return size;
}
#endif

#if !defined(DACCESS_COMPILE)
//==========================================================================================
void MethodTable::AllocateRegularStaticBoxes()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!ContainsGenericVariables());
        PRECONDITION(HasBoxedRegularStatics());
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Instantiating static handles for %s\n", GetDebugClassName()));

    GCX_COOP();

    PTR_BYTE pStaticBase = GetGCStaticsBasePointer();

    GCPROTECT_BEGININTERIOR(pStaticBase);

    {
        FieldDesc *pField = HasGenericsStaticsInfo() ?
            GetGenericsStaticFieldDescs() : (GetApproxFieldDescListRaw() + GetNumIntroducedInstanceFields());
        FieldDesc *pFieldEnd = pField + GetNumStaticFields();

        while (pField < pFieldEnd)
        {
            _ASSERTE(pField->IsStatic());

            if (!pField->IsSpecialStatic() && pField->IsByValue())
            {
                AllocateRegularStaticBox(pField, (Object**)(pStaticBase + pField->GetOffset()));
            }

            pField++;
        }
    }
    GCPROTECT_END();
}

void MethodTable::AllocateRegularStaticBox(FieldDesc* pField, Object** boxedStaticHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END

    _ASSERT(pField->IsStatic() && !pField->IsSpecialStatic() && pField->IsByValue());

    // Static fields are not pinned in collectible types so we need to protect the address
    GCPROTECT_BEGININTERIOR(boxedStaticHandle);
    if (VolatileLoad(boxedStaticHandle) == nullptr)
    {
        // Grab field's type handle before we enter lock
        MethodTable* pFieldMT = pField->GetFieldTypeHandleThrowing().GetMethodTable();
        bool hasFixedAddr = HasFixedAddressVTStatics();

        // Taking a lock since we might come here from multiple threads/places
        CrstHolder crst(GetAppDomain()->GetStaticBoxInitLock());

        // double-checked locking
        if (VolatileLoad(boxedStaticHandle) == nullptr)
        {
            LOG((LF_CLASSLOADER, LL_INFO10000, "\tInstantiating static of type %s\n", pFieldMT->GetDebugClassName()));
            const bool canBeFrozen = !pFieldMT->ContainsPointers() && !Collectible();
            OBJECTREF obj = AllocateStaticBox(pFieldMT, hasFixedAddr, canBeFrozen);
            SetObjectReference((OBJECTREF*)(boxedStaticHandle), obj);
        }
    }
    GCPROTECT_END();
}

//==========================================================================================
OBJECTREF MethodTable::AllocateStaticBox(MethodTable* pFieldMT, BOOL fPinned, bool canBeFrozen)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END

    _ASSERTE(pFieldMT->IsValueType());

    // Activate any dependent modules if necessary
    pFieldMT->EnsureInstanceActive();

    OBJECTREF obj = NULL;
    if (canBeFrozen)
    {
        // In case if we don't plan to collect this handle we may try to allocate it on FOH
        _ASSERT(!pFieldMT->ContainsPointers());
        FrozenObjectHeapManager* foh = SystemDomain::GetFrozenObjectHeapManager();
        obj = ObjectToOBJECTREF(foh->TryAllocateObject(pFieldMT, pFieldMT->GetBaseSize()));
        // obj can be null in case if struct is huge (>64kb)
        if (obj != NULL)
        {
            return obj;
        }
    }

    obj = AllocateObject(pFieldMT, fPinned ? GC_ALLOC_PINNED_OBJECT_HEAP : GC_ALLOC_NO_FLAGS);

    return obj;
}

//==========================================================================================
BOOL MethodTable::RunClassInitEx(OBJECTREF *pThrowable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsFullyLoaded());
        PRECONDITION(IsProtectedByGCFrame(pThrowable));
    }
    CONTRACTL_END;

    // A somewhat unusual function, can both return throwable and throw.
    // The difference is, we throw on restartable operations and just return throwable
    // on exceptions fatal for the .cctor
    // (Of course in the latter case the caller is supposed to throw pThrowable)
    // Doing the opposite ( i.e. throwing on fatal and returning on nonfatal)
    // would be more intuitive but it's more convenient the way it is

    BOOL fRet = FALSE;

    // During the <clinit>, this thread must not be asynchronously
    // stopped or interrupted.  That would leave the class unavailable
    // and is therefore a security hole.  We don't have to worry about
    // multithreading, since we only manipulate the current thread's count.
    ThreadPreventAsyncHolder preventAsync;

    // If the static initialiser throws an exception that it doesn't catch, it has failed
    EX_TRY
    {
        // Activate our module if necessary
        EnsureInstanceActive();

        STRESS_LOG1(LF_CLASSLOADER, LL_INFO1000, "RunClassInit: Calling class constructor for type %pT\n", this);

        MethodTable * pCanonMT = GetCanonicalMethodTable();

        // Call the code method without touching MethodDesc if possible
        PCODE pCctorCode = pCanonMT->GetSlot(pCanonMT->GetClassConstructorSlot());

        if (pCanonMT->IsSharedByGenericInstantiations())
        {
            PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(pCctorCode);
            DECLARE_ARGHOLDER_ARRAY(args, 1);
            args[ARGNUM_0] = PTR_TO_ARGHOLDER(this);
            CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
            CALL_MANAGED_METHOD_NORET(args);
        }
        else
        {
            PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(pCctorCode);
            DECLARE_ARGHOLDER_ARRAY(args, 0);
            CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
            CALL_MANAGED_METHOD_NORET(args);
        }

        STRESS_LOG1(LF_CLASSLOADER, LL_INFO100000, "RunClassInit: Returned Successfully from class constructor for type %pT\n", this);

        fRet = TRUE;
    }
    EX_CATCH
    {
        // Exception set by parent
        // <TODO>@TODO: We should make this an ExceptionInInitializerError if the exception thrown is not
        // a subclass of Error</TODO>
        *pThrowable = GET_THROWABLE();
        _ASSERTE(fRet == FALSE);
    }
    EX_END_CATCH(SwallowAllExceptions)

    return fRet;
}

//==========================================================================================
void MethodTable::DoRunClassInitThrowing()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();

    // This is a fairly aggressive policy. Merely asking that the class be initialized is grounds for kicking you out.
    // Alternately, we could simply NOP out the class initialization. Since the aggressive policy is also the more secure
    // policy, keep this unless it proves intractable to remove all premature classinits in the system.
    EnsureActive();

    Thread* pThread = GetThread();

    AppDomain *pDomain = GetAppDomain();

    HRESULT hrResult = E_FAIL;
    const char *description;
    STRESS_LOG2(LF_CLASSLOADER, LL_INFO100000, "DoRunClassInit: Request to init %pT in appdomain %p\n", this, pDomain);

    //
    // Take the global lock
    //

    ListLock *_pLock = pDomain->GetClassInitLock();

    ListLockHolder pInitLock(_pLock);

    // Check again
    if (IsClassInited())
        goto Exit;

    //
    // Handle cases where the .cctor has already tried to run but failed.
    //


    if (IsInitError())
    {
        // Some error occurred trying to init this class
        ListLockEntry*     pEntry= (ListLockEntry *) _pLock->Find(this);
        _ASSERTE(pEntry!=NULL);
        _ASSERTE(pEntry->m_pLoaderAllocator == GetLoaderAllocator());

        // If this isn't a TypeInitializationException, then its creation failed
        // somehow previously, so we should make one last attempt to create it. If
        // that fails, just throw the exception that was originally thrown.
        // Primarily, this deals with the problem that the exception is a
        // ThreadAbortException, because this must be executing on a different
        // thread. If in fact this thread is also aborting, then rethrowing the
        // other thread's exception will not do any worse.

        // If we need to create the type init exception object, we'll need to
        // GC protect these, so might as well create the structure now.
        struct _gc {
            OBJECTREF pInitException;
            OBJECTREF pNewInitException;
            OBJECTREF pThrowable;
        } gc;

        gc.pInitException = pEntry->m_pLoaderAllocator->GetHandleValue(pEntry->m_hInitException);
        gc.pNewInitException = NULL;
        gc.pThrowable = NULL;

        GCPROTECT_BEGIN(gc);

        // We need to release this lock because CreateTypeInitializationExceptionObject and fetching the TypeLoad exception can cause
        // managed code to re-enter into this codepath, causing a locking order violation.
        pInitLock.Release();

        if (CoreLibBinder::GetException(kTypeInitializationException) != gc.pInitException->GetMethodTable())
        {
            DefineFullyQualifiedNameForClassWOnStack();
            LPCWSTR wszName = GetFullyQualifiedNameForClassW(this);

            CreateTypeInitializationExceptionObject(wszName, &gc.pInitException, &gc.pNewInitException, &gc.pThrowable);

            LOADERHANDLE hOrigInitException = pEntry->m_hInitException;
            if (!CLRException::IsPreallocatedExceptionObject(pEntry->m_pLoaderAllocator->GetHandleValue(hOrigInitException)))
            {
                // Now put the new init exception in the handle. If another thread beat us (because we released the
                // lock above), then we'll just let the extra init exception object get collected later.
                pEntry->m_pLoaderAllocator->CompareExchangeValueInHandle(pEntry->m_hInitException, gc.pNewInitException, gc.pInitException);
            } else {
                // if the stored exception is a preallocated one we cannot store the new Exception object in it.
                // we'll attempt to create a new handle for the new TypeInitializationException object
                LOADERHANDLE hNewInitException = NULL;
                // CreateHandle can throw due to OOM. We need to catch this so that we make sure to set the
                // init error. Whatever exception was thrown will be rethrown below, so no worries.
                EX_TRY {
                    hNewInitException = pEntry->m_pLoaderAllocator->AllocateHandle(gc.pNewInitException);
                } EX_CATCH {
                    // If we failed to create the handle we'll just leave the originally alloc'd one in place.
                } EX_END_CATCH(SwallowAllExceptions);

                // if two threads are racing to set m_hInitException, clear the handle created by the loser
                if (hNewInitException != NULL &&
                    InterlockedCompareExchangeT((&pEntry->m_hInitException), hNewInitException, hOrigInitException) != hOrigInitException)
                {
                    pEntry->m_pLoaderAllocator->FreeHandle(hNewInitException);
                }
            }
        }
        else {
            gc.pThrowable = gc.pInitException;
        }

        GCPROTECT_END();

        // Throw the saved exception. Since we may be rethrowing a previously cached exception, must clear the stack trace first.
        // Rethrowing a previously cached exception is distasteful but is required for appcompat with Everett.
        //
        // (The IsException() is probably more appropriate as an assert but as this isn't a heavily tested code path,
        // I prefer to be defensive here.)
        if (IsException(gc.pThrowable->GetMethodTable()))
        {
            ((EXCEPTIONREF)(gc.pThrowable))->ClearStackTraceForThrow();
        }

        COMPlusThrow(gc.pThrowable);
    }

    description = ".cctor lock";
#ifdef _DEBUG
    description = GetDebugClassName();
#endif

    // Take the lock
    {
        //nontrivial holder, might take a lock in destructor
        ListLockEntryHolder pEntry(ListLockEntry::Find(pInitLock, this, description));

        ListLockEntryLockHolder pLock(pEntry, FALSE);

        // We have a list entry, we can release the global lock now
        pInitLock.Release();

        if (pLock.DeadlockAwareAcquire())
        {
            if (pEntry->m_hrResultCode == S_FALSE)
            {
                if (HasBoxedRegularStatics())
                {
                    // First, instantiate any objects needed for value type statics
                    AllocateRegularStaticBoxes();
                }

                // Nobody has run the .cctor yet
                if (HasClassConstructor())
                {
                    struct _gc {
                        OBJECTREF pInnerException;
                        OBJECTREF pInitException;
                        OBJECTREF pThrowable;
                    } gc;
                    gc.pInnerException = NULL;
                    gc.pInitException = NULL;
                    gc.pThrowable = NULL;
                    GCPROTECT_BEGIN(gc);

                    if (!RunClassInitEx(&gc.pInnerException))
                    {
                        // The .cctor failed and we want to store the exception that resulted
                        // in the entry. Increment the ref count to keep the entry alive for
                        // subsequent attempts to run the .cctor.
                        pEntry->AddRef();
                        // For collectible types, register the entry for cleanup.
                        if (GetLoaderAllocator()->IsCollectible())
                        {
                            GetLoaderAllocator()->RegisterFailedTypeInitForCleanup(pEntry);
                        }

                        _ASSERTE(g_pThreadAbortExceptionClass == CoreLibBinder::GetException(kThreadAbortException));

                        if(gc.pInnerException->GetMethodTable() == g_pThreadAbortExceptionClass)
                        {
                            gc.pThrowable = gc.pInnerException;
                            gc.pInitException = gc.pInnerException;
                            gc.pInnerException = NULL;
                        }
                        else
                        {
                            DefineFullyQualifiedNameForClassWOnStack();
                            LPCWSTR wszName = GetFullyQualifiedNameForClassW(this);

                            // Note that this may not succeed due to problems creating the exception
                            // object. On failure, it will first try to
                            CreateTypeInitializationExceptionObject(
                                wszName, &gc.pInnerException, &gc.pInitException, &gc.pThrowable);
                        }

                        pEntry->m_pLoaderAllocator = GetLoaderAllocator();

                        // CreateHandle can throw due to OOM. We need to catch this so that we make sure to set the
                        // init error. Whatever exception was thrown will be rethrown below, so no worries.
                        EX_TRY {
                            // Save the exception object, and return to caller as well.
                            pEntry->m_hInitException = pEntry->m_pLoaderAllocator->AllocateHandle(gc.pInitException);
                        } EX_CATCH {
                            // If we failed to create the handle (due to OOM), we'll just store the preallocated OOM
                            // handle here instead.
                            pEntry->m_hInitException = pEntry->m_pLoaderAllocator->AllocateHandle(CLRException::GetPreallocatedOutOfMemoryException());
                        } EX_END_CATCH(SwallowAllExceptions);

                        pEntry->m_hrResultCode = E_FAIL;
                        SetClassInitError();

                        COMPlusThrow(gc.pThrowable);
                    }

                    GCPROTECT_END();
                }

                pEntry->m_hrResultCode = S_OK;

                // Set the initialization flags in the DLS and on domain-specific types.
                // Note we also set the flag for dynamic statics, which use the DynamicStatics part
                // of the DLS irrespective of whether the type is domain neutral or not.
                SetClassInited();

            }
            else
            {
                // Use previous result

                hrResult = pEntry->m_hrResultCode;
                if(FAILED(hrResult))
                {
                    // An exception may have occurred in the cctor. DoRunClassInit() should return FALSE in that
                    // case.
                    _ASSERTE(pEntry->m_hInitException);
                    _ASSERTE(pEntry->m_pLoaderAllocator == GetLoaderAllocator());
                    _ASSERTE(IsInitError());

                    // Throw the saved exception. Since we are rethrowing a previously cached exception, must clear the stack trace first.
                    // Rethrowing a previously cached exception is distasteful but is required for appcompat with Everett.
                    //
                    // (The IsException() is probably more appropriate as an assert but as this isn't a heavily tested code path,
                    // I prefer to be defensive here.)
                    if (IsException(pEntry->m_pLoaderAllocator->GetHandleValue(pEntry->m_hInitException)->GetMethodTable()))
                    {
                        ((EXCEPTIONREF)(pEntry->m_pLoaderAllocator->GetHandleValue(pEntry->m_hInitException)))->ClearStackTraceForThrow();
                    }
                    COMPlusThrow(pEntry->m_pLoaderAllocator->GetHandleValue(pEntry->m_hInitException));
                }
            }
        }
    }

    //
    // Notify any entries waiting on the current entry and wait for the required entries.
    //

    // We need to take the global lock before we play with the list of entries.

    STRESS_LOG2(LF_CLASSLOADER, LL_INFO100000, "DoRunClassInit: returning SUCCESS for init %pT in appdomain %p\n", this, pDomain);
    // No need to set pThrowable in case of error it will already have been set.
Exit:
    ;
}

//==========================================================================================
void MethodTable::CheckRunClassInitThrowing()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsFullyLoaded());
    }
    CONTRACTL_END;

    {   // Debug-only code causes SO volation, so add exception.
        CONSISTENCY_CHECK(CheckActivated());
    }

    // To find GC hole easier...
    TRIGGERSGC();

    if (IsClassPreInited())
        return;

    // Don't initialize shared generic instantiations (e.g. MyClass<__Canon>)
    if (IsSharedByGenericInstantiations())
        return;

    DomainLocalModule *pLocalModule = GetDomainLocalModule();
    _ASSERTE(pLocalModule);

    DWORD iClassIndex = GetClassIndex();

    // Check to see if we have already run the .cctor for this class.
    if (!pLocalModule->IsClassAllocated(this, iClassIndex))
        pLocalModule->PopulateClass(this);

    if (!pLocalModule->IsClassInitialized(this, iClassIndex))
        DoRunClassInitThrowing();
}

//==========================================================================================
void MethodTable::CheckRunClassInitAsIfConstructingThrowing()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (HasPreciseInitCctors())
    {
        MethodTable *pMTCur = this;
        while (pMTCur != NULL)
        {
            if (!pMTCur->GetClass()->IsBeforeFieldInit())
                pMTCur->CheckRunClassInitThrowing();

            pMTCur = pMTCur->GetParentMethodTable();
        }
    }
}

//==========================================================================================
OBJECTREF MethodTable::Allocate()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    CONSISTENCY_CHECK(IsFullyLoaded());

    EnsureInstanceActive();

    if (HasPreciseInitCctors())
    {
        CheckRunClassInitAsIfConstructingThrowing();
    }

    return AllocateObject(this);
}

//==========================================================================================
// box 'data' creating a new object and return it.  This routine understands the special
// handling needed for Nullable values.
// see code:Nullable#NullableVerification

OBJECTREF MethodTable::Box(void* data)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsValueType());
    }
    CONTRACTL_END;

    OBJECTREF ref;

    GCPROTECT_BEGININTERIOR (data);

    if (IsByRefLike())
    {
        // We should never box a type that contains stack pointers.
        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_TypeCannotBeBoxed"));
    }

    ref = FastBox(&data);
    GCPROTECT_END ();
    return ref;
}

OBJECTREF MethodTable::FastBox(void** data)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsValueType());
    }
    CONTRACTL_END;

    // See code:Nullable#NullableArchitecture for more
    if (IsNullable())
        return Nullable::Box(*data, this);

    OBJECTREF ref = Allocate();
    CopyValueClass(ref->UnBox(), *data, this);
    return ref;
}

#if TARGET_X86 || TARGET_AMD64
//==========================================================================================
static void FastCallFinalize(Object *obj, PCODE funcPtr, BOOL fCriticalCall)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    BEGIN_CALL_TO_MANAGEDEX(fCriticalCall ? EEToManagedCriticalCall : EEToManagedDefault);

#if defined(TARGET_X86)

    __asm
    {
        mov     ecx, [obj]
        call    [funcPtr]
        INDEBUG(nop)            // Mark the fact that we can call managed code
    }

#else // TARGET_X86

    FastCallFinalizeWorker(obj, funcPtr);

#endif // TARGET_X86

    END_CALL_TO_MANAGED();
}

#endif // TARGET_X86 || TARGET_AMD64

void CallFinalizerOnThreadObject(Object *obj)
{
    STATIC_CONTRACT_MODE_COOPERATIVE;

    THREADBASEREF   refThis = (THREADBASEREF)ObjectToOBJECTREF(obj);
    Thread*         thread  = refThis->GetInternal();

    // Prevent multiple calls to Finalize
    // Objects can be resurrected after being finalized.  However, there is no
    // race condition here.  We always check whether an exposed thread object is
    // still attached to the internal Thread object, before proceeding.
    if (thread)
    {
        refThis->ResetStartHelper();

        // During process shutdown, we finalize even reachable objects.  But if we break
        // the link between the System.Thread and the internal Thread object, the runtime
        // may not work correctly.  In particular, we won't be able to transition between
        // contexts and domains to finalize other objects.  Since the runtime doesn't
        // require that Threads finalize during shutdown, we need to disable this.  If
        // we wait until phase 2 of shutdown finalization (when the EE is suspended and
        // will never resume) then we can simply skip the side effects of Thread
        // finalization.
        if ((g_fEEShutDown & ShutDown_Finalize2) == 0)
        {
            if (GetThreadNULLOk() != thread)
            {
                refThis->ClearInternal();
            }

            thread->SetThreadState(Thread::TS_Finalized);
            Thread::SetCleanupNeededForFinalizedThread();
        }
    }
}

//==========================================================================================
// From the GC finalizer thread, invoke the Finalize() method on an object.
void MethodTable::CallFinalizer(Object *obj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(obj->GetMethodTable()->HasFinalizer());
    }
    CONTRACTL_END;

    MethodTable *pMT = obj->GetMethodTable();

    // Check for precise init class constructors that have failed, if any have failed, then we didn't run the
    // constructor for the object, and running the finalizer for the object would violate the CLI spec by running
    // instance code without having successfully run the precise-init class constructor.
    if (pMT->HasPreciseInitCctors())
    {
        MethodTable *pMTCur = pMT;
        do
        {
            if ((!pMTCur->GetClass()->IsBeforeFieldInit()) && pMTCur->IsInitError())
            {
                // Precise init Type Initializer for type failed... do not run finalizer
                return;
            }

            pMTCur = pMTCur->GetParentMethodTable();
        }
        while (pMTCur != NULL);
    }

    if (pMT == g_pThreadClass)
    {
        // Finalizing Thread object requires ThreadStoreLock.  It is expensive if
        // we keep taking ThreadStoreLock.  This is very bad if we have high retiring
        // rate of Thread objects.
        // To avoid taking ThreadStoreLock multiple times, we mark Thread with TS_Finalized
        // and clean up a batch of them when we take ThreadStoreLock next time.

        // To avoid possible hierarchy requirement between critical finalizers, we call cleanup
        // code directly.
        CallFinalizerOnThreadObject(obj);
        return;
    }


    // Determine if the object has a critical or normal finalizer.
    BOOL fCriticalFinalizer = pMT->HasCriticalFinalizer();

    // There's no reason to actually set up a frame here.  If we crawl out of the
    // Finalize() method on this thread, we will see FRAME_TOP which indicates
    // that the crawl should terminate.  This is analogous to how KickOffThread()
    // starts new threads in the runtime.
    PCODE funcPtr = pMT->GetRestoredSlot(g_pObjectFinalizerMD->GetSlot());

#ifdef STRESS_LOG
    if (fCriticalFinalizer)
    {
        STRESS_LOG1(LF_GCALLOC, LL_INFO100, "Finalizing CriticalFinalizer %pM\n",
                    pMT);
    }
#endif

#if defined(TARGET_X86) || defined(TARGET_AMD64)

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall((const BYTE *) funcPtr);
#endif // DEBUGGING_SUPPORTED

    FastCallFinalize(obj, funcPtr, fCriticalFinalizer);

#else // defined(TARGET_X86) || defined(TARGET_AMD64)

    PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(funcPtr);

    DECLARE_ARGHOLDER_ARRAY(args, 1);

    args[ARGNUM_0] = PTR_TO_ARGHOLDER(obj);

    if (fCriticalFinalizer)
    {
        CRITICAL_CALLSITE;
    }

    CALL_MANAGED_METHOD_NORET(args);

#endif // (defined(TARGET_X86) && defined(TARGET_AMD64)

#ifdef STRESS_LOG
    if (fCriticalFinalizer)
    {
        STRESS_LOG1(LF_GCALLOC, LL_INFO100, "Finalized CriticalFinalizer %pM without exception\n",
                    pMT);
    }
#endif
}

//==========================================================================
// If the MethodTable doesn't yet know the Exposed class that represents it via
// Reflection, acquire that class now.  Regardless, return it to the caller.
//==========================================================================
OBJECTREF MethodTable::GetManagedClassObject()
{
    CONTRACT(OBJECTREF) {

        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(GetAuxiliaryData()->m_hExposedClassObject != 0);
        //REENTRANT
    }
    CONTRACT_END;

#ifdef _DEBUG
    // Force a GC here because GetManagedClassObject could trigger GC nondeterministicaly
    GCStress<cfg_any, PulseGcTriggerPolicy>::MaybeTrigger();
#endif // _DEBUG

    if (GetAuxiliaryData()->m_hExposedClassObject == NULL)
    {
        // Make sure that we have been restored
        CheckRestore();
        TypeHandle(this).AllocateManagedClassObject(&GetAuxiliaryDataForWrite()->m_hExposedClassObject);
    }
    RETURN(GetManagedClassObjectIfExists());
}

#endif //!DACCESS_COMPILE

//==========================================================================================
// This needs to stay consistent with AllocateNewMT() and MethodTable::Save()
//
// <TODO> protect this via some asserts as we've had one hard-to-track-down
// bug already </TODO>
//
void MethodTable::GetSavedExtent(TADDR *pStart, TADDR *pEnd)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    TADDR start;

    if (ContainsPointers())
        start = dac_cast<TADDR>(this) - CGCDesc::GetCGCDescFromMT(this)->GetSize();
    else
        start = dac_cast<TADDR>(this);

    TADDR end = dac_cast<TADDR>(this) + GetEndOffsetOfOptionalMembers();

    _ASSERTE(start && end && (start < end));
    *pStart = start;
    *pEnd = end;
}

//==========================================================================================
void MethodTable::CheckRestore()
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
    }
    CONTRACTL_END

    if (!IsFullyLoaded())
    {
        ClassLoader::EnsureLoaded(this);
        _ASSERTE(IsFullyLoaded());
    }
}

#ifndef DACCESS_COMPILE

BOOL SatisfiesClassConstraints(TypeHandle instanceTypeHnd, TypeHandle typicalTypeHnd,
                               const InstantiationContext *pInstContext);

static VOID DoAccessibilityCheck(MethodTable *pAskingMT, MethodTable *pTargetMT, UINT resIDWhy)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    AccessCheckContext accessContext(NULL, pAskingMT);

    if (!ClassLoader::CanAccessClass(&accessContext,
                                     pTargetMT,                 //the desired class
                                     pTargetMT->GetAssembly(),  //the desired class's assembly
                                     *AccessCheckOptions::s_pNormalAccessChecks
                                    ))
    {
        SString displayName;
        pAskingMT->GetAssembly()->GetDisplayName(displayName);
        SString targetName;

        // Error string is either E_ACCESSDENIED which requires the type name of the target, vs
        // a more normal TypeLoadException which displays the requesting type.
       _ASSERTE((resIDWhy == (UINT)E_ACCESSDENIED) || (resIDWhy == (UINT)IDS_CLASSLOAD_INTERFACE_NO_ACCESS));
        TypeString::AppendType(targetName, TypeHandle((resIDWhy == (UINT)E_ACCESSDENIED) ? pTargetMT : pAskingMT));

        COMPlusThrow(kTypeLoadException, resIDWhy, targetName.GetUnicode(), displayName.GetUnicode());
    }

}

VOID DoAccessibilityCheckForConstraint(MethodTable *pAskingMT, TypeHandle thConstraint, UINT resIDWhy)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (thConstraint.IsArray())
    {
        DoAccessibilityCheckForConstraint(pAskingMT, thConstraint.GetArrayElementTypeHandle(), resIDWhy);
    }
    else if (thConstraint.IsTypeDesc())
    {
        TypeDesc *pTypeDesc = thConstraint.AsTypeDesc();

        if (pTypeDesc->IsGenericVariable())
        {
            // since the metadata respresents a generic type param constraint as an index into
            // the declaring type's list of generic params, it is structurally impossible
            // to express a violation this way. So there's no check to be done here.
        }
        else
        if (pTypeDesc->HasTypeParam())
        {
            DoAccessibilityCheckForConstraint(pAskingMT, pTypeDesc->GetTypeParam(), resIDWhy);
        }
        else
        {
            COMPlusThrow(kTypeLoadException, E_ACCESSDENIED);
        }

    }
    else
    {
        DoAccessibilityCheck(pAskingMT, thConstraint.GetMethodTable(), resIDWhy);
    }

}

VOID DoAccessibilityCheckForConstraints(MethodTable *pAskingMT, TypeVarTypeDesc *pTyVar, UINT resIDWhy)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    DWORD numConstraints;
    TypeHandle *pthConstraints = pTyVar->GetCachedConstraints(&numConstraints);
    for (DWORD cidx = 0; cidx < numConstraints; cidx++)
    {
        TypeHandle thConstraint = pthConstraints[cidx];

        DoAccessibilityCheckForConstraint(pAskingMT, thConstraint, resIDWhy);
    }
}


// Recursive worker that pumps the transitive closure of a type's dependencies to the specified target level.
// Dependencies include:
//
//   - parent
//   - interfaces
//   - canonical type, for non-canonical instantiations
//   - typical type, for non-typical instantiations
//
// Parameters:
//
//   pVisited - used to prevent endless recursion in the case of cyclic dependencies
//
//   level    - target level to pump to - must be CLASS_DEPENDENCIES_LOADED or CLASS_LOADED
//
//              if CLASS_DEPENDENCIES_LOADED, all transitive dependencies are resolved to their
//                 exact types.
//
//              if CLASS_LOADED, all type-safety checks are done on the type and all its transitive
//                 dependencies. Note that for the CLASS_LOADED case, some types may be left
//                 on the pending list rather that pushed to CLASS_LOADED in the case of cyclic
//                 dependencies - the root caller must handle this.
//
//   pfBailed - if we or one of our dependencies bails early due to cyclic dependencies, we
//              must set *pfBailed to TRUE. Otherwise, we must *leave it unchanged* (thus, the
//              boolean acts as a cumulative OR.)
//
//   pPending - if one of our dependencies bailed, the type cannot yet be promoted to CLASS_LOADED
//              as the dependencies will be checked later and may fail a security check then.
//              Instead, DoFullyLoad() will add the type to the pending list - the root caller
//              is responsible for promoting the type after the full transitive closure has been
//              walked. Note that it would be just as correct to always defer to the pending list -
//              however, that is a little less performant.
//


// Closure of locals necessary for implementing CheckForEquivalenceAndFullyLoadType.
// Used so that we can have one valuetype walking algorithm used for type equivalence walking of the parameters of the method.
struct DoFullyLoadLocals
{
    DoFullyLoadLocals(DFLPendingList *pPendingParam, ClassLoadLevel levelParam, MethodTable *pMT, Generics::RecursionGraph *pVisited)
        : newVisited(pVisited, TypeHandle(pMT))
        , pPending(pPendingParam)
        , level(levelParam)
        , fBailed(FALSE)
#ifdef FEATURE_TYPEEQUIVALENCE
        , fHasEquivalentStructParameter(FALSE)
#endif
    {
        LIMITED_METHOD_CONTRACT;
    }

    Generics::RecursionGraph newVisited;
    DFLPendingList * const pPending;
    const ClassLoadLevel level;
    BOOL fBailed;
#ifdef FEATURE_TYPEEQUIVALENCE
    BOOL fHasEquivalentStructParameter;
#endif
};

#if defined(FEATURE_TYPEEQUIVALENCE) && !defined(DACCESS_COMPILE)
static void CheckForEquivalenceAndFullyLoadType(Module *pModule, mdToken token, Module *pDefModule, mdToken defToken, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    SigPointer sigPtr(*ptr);

    DoFullyLoadLocals *pLocals = (DoFullyLoadLocals *)pData;

    if (IsTypeDefEquivalent(defToken, pDefModule))
    {
        TypeHandle th = sigPtr.GetTypeHandleThrowing(pModule, pTypeContext, ClassLoader::LoadTypes, (ClassLoadLevel)(pLocals->level - 1));
        CONSISTENCY_CHECK(!th.IsNull());

        th.DoFullyLoad(&pLocals->newVisited, pLocals->level, pLocals->pPending, &pLocals->fBailed, NULL);
        pLocals->fHasEquivalentStructParameter = TRUE;
    }
}

#endif // defined(FEATURE_TYPEEQUIVALENCE) && !defined(DACCESS_COMPILE)

#endif //!DACCESS_COMPILE

void MethodTable::DoFullyLoad(Generics::RecursionGraph * const pVisited,  const ClassLoadLevel level, DFLPendingList * const pPending,
                              BOOL * const pfBailed, const InstantiationContext * const pInstContext)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(level == CLASS_LOADED || level == CLASS_DEPENDENCIES_LOADED);
    _ASSERTE(pfBailed != NULL);
    _ASSERTE(!(level == CLASS_LOADED && pPending == NULL));


#ifndef DACCESS_COMPILE

    if (Generics::RecursionGraph::HasSeenType(pVisited, TypeHandle(this)))
    {
        *pfBailed = TRUE;
        return;
    }

    if (GetLoadLevel() >= level)
    {
        return;
    }

    if (level == CLASS_LOADED)
    {
        UINT numTH = pPending->Count();
        TypeHandle *pTypeHndPending = pPending->Table();
        for (UINT idxPending = 0; idxPending < numTH; idxPending++)
        {
            if (pTypeHndPending[idxPending] == this)
            {
                *pfBailed = TRUE;
                return;
            }
        }

    }

    // First ensure that we're loaded to just below CLASS_DEPENDENCIES_LOADED
    ClassLoader::EnsureLoaded(this, (ClassLoadLevel) (level-1));

    CONSISTENCY_CHECK(IsRestored());
    CONSISTENCY_CHECK(!HasApproxParent());

    if ((level == CLASS_LOADED) && !IsSharedByGenericInstantiations())
    {
        _ASSERTE(GetLoadLevel() >= CLASS_DEPENDENCIES_LOADED);
        ClassLoader::ValidateMethodsWithCovariantReturnTypes(this);
    }

    if (IsArray())
    {
        Generics::RecursionGraph newVisited(pVisited, TypeHandle(this));

        // Fully load the element type
        GetArrayElementTypeHandle().DoFullyLoad(&newVisited, level, pPending, pfBailed, pInstContext);
    }

    DoFullyLoadLocals locals(pPending, level, this, pVisited);

    bool fNeedsSanityChecks = true;

#ifdef FEATURE_READYTORUN
    Module * pModule = GetModule();

    // No sanity checks for ready-to-run compiled images if possible
    if (pModule->IsSystem() || (pModule->IsReadyToRun() && pModule->GetReadyToRunInfo()->SkipTypeValidation()))
        fNeedsSanityChecks = false;
#endif

    bool fNeedAccessChecks = (level == CLASS_LOADED) &&
                             fNeedsSanityChecks &&
                             IsTypicalTypeDefinition();

    TypeHandle typicalTypeHnd;

    // Fully load the typical instantiation. Make sure that this is done before loading other dependencies
    // as the recursive generics detection algorithm needs to examine typical instantiations of the types
    // in the closure.
    if (!IsTypicalTypeDefinition())
    {
        typicalTypeHnd = ClassLoader::LoadTypeDefThrowing(GetModule(), GetCl(),
            ClassLoader::ThrowIfNotFound, ClassLoader::PermitUninstDefOrRef, tdNoTypes,
            (ClassLoadLevel) (level - 1));
        CONSISTENCY_CHECK(!typicalTypeHnd.IsNull());
        typicalTypeHnd.DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);
    }
    else if (level == CLASS_DEPENDENCIES_LOADED && HasInstantiation())
    {
        // This is a typical instantiation of a generic type. When attaining CLASS_DEPENDENCIES_LOADED, the
        // recursive inheritance graph (ECMA part.II Section 9.2) will be constructed and checked for "expanding
        // cycles" to detect infinite recursion, e.g. A<T> : B<A<A<T>>>.
        //
        // The dependencies loaded by this method (parent type, implemented interfaces, generic arguments)
        // ensure that we will generate the finite instantiation closure as defined in ECMA. This load level
        // is not being attained under lock so it's not possible to use TypeVarTypeDesc to represent graph
        // nodes because multiple threads trying to fully load types from the closure at the same time would
        // interfere with each other. In addition, the graph is only used for loading and can be discarded
        // when the closure is fully loaded (TypeVarTypeDesc need to stay).
        //
        // The graph is represented by Generics::RecursionGraph instances organized in a linked list with
        // each of them holding part of the graph. They live on the stack and are cleaned up automatically
        // before returning from DoFullyLoad.

        if (locals.newVisited.CheckForIllegalRecursion())
        {
            // An expanding cycle was detected, this type is part of a closure that is defined recursively.
            IMDInternalImport* pInternalImport = GetModule()->GetMDImport();
            GetModule()->GetAssembly()->ThrowTypeLoadException(pInternalImport, GetCl(), IDS_CLASSLOAD_GENERICTYPE_RECURSIVE);
        }
    }

    // Fully load the parent
    MethodTable *pParentMT = GetParentMethodTable();

    if (pParentMT)
    {
        pParentMT->DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);

        if (fNeedAccessChecks)
        {
            if (!IsComObjectType()) //RCW's are special - they are manufactured by the runtime and derive from the non-public type System.__ComObject
            {
                // A transparent type should not be allowed to derive from a critical type.
                // However since this has never been enforced before we have many classes that
                // violate this rule. Enforcing it now will be a breaking change.
                DoAccessibilityCheck(this, pParentMT, E_ACCESSDENIED);
            }
        }
    }

    // Fully load the interfaces
    MethodTable::InterfaceMapIterator it = IterateInterfaceMap();
    while (it.Next())
    {
        MethodTable* pItf = it.GetInterfaceApprox();
        pItf->DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);

        if (fNeedAccessChecks)
        {
            if (IsInterfaceDeclaredOnClass(it.GetIndex())) // only test directly implemented interfaces (it's
                                                           // legal for an inherited interface to be private.)
            {
                // A transparent type should not be allowed to implement a critical interface.
                // However since this has never been enforced before we have many classes that
                // violate this rule. Enforcing it now will be a breaking change.
                DoAccessibilityCheck(this, pItf, IDS_CLASSLOAD_INTERFACE_NO_ACCESS);
            }
        }
    }

    // Fully load the generic arguments
    Instantiation inst = GetInstantiation();
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        inst[i].DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);
    }

    // Fully load the canonical methodtable
    if (!IsCanonicalMethodTable())
    {
        GetCanonicalMethodTable()->DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, NULL);
    }

    if (fNeedsSanityChecks)
    {
        // Fully load the exact field types for value type fields
        // Note that MethodTableBuilder::InitializeFieldDescs() loads the type of the
        // field only upto level CLASS_LOAD_APPROXPARENTS.
        FieldDesc *pField = GetApproxFieldDescListRaw();
        FieldDesc *pFieldEnd = pField + GetNumStaticFields() + GetNumIntroducedInstanceFields();

        while (pField < pFieldEnd)
        {
            if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
            {
                TypeHandle th = pField->GetFieldTypeHandleThrowing((ClassLoadLevel) (level - 1));
                CONSISTENCY_CHECK(!th.IsNull());

                th.DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);

                if (fNeedAccessChecks)
                {
                    DoAccessibilityCheck(this, th.GetMethodTable(), E_ACCESSDENIED);
                }

            }
            pField++;
        }

        // Fully load the exact field types for generic value type fields
        if (HasGenericsStaticsInfo())
        {
            FieldDesc *pGenStaticField = GetGenericsStaticFieldDescs();
            FieldDesc *pGenStaticFieldEnd = pGenStaticField + GetNumStaticFields();
            while (pGenStaticField < pGenStaticFieldEnd)
            {
                if (pGenStaticField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
                {
                    TypeHandle th = pGenStaticField->GetFieldTypeHandleThrowing((ClassLoadLevel) (level - 1));
                    CONSISTENCY_CHECK(!th.IsNull());

                    th.DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);

                    // The accessibility check is not necessary for generic fields. The generic fields are copy
                    // of the regular fields, the only difference is that they have the exact type.
                }
                pGenStaticField++;
            }
        }
    }

    // Fully load exact parameter types for value type parameters opted into equivalence. This is required in case GC is
    // triggered during prestub. GC needs to know where references are on the stack and if the parameter (as read from
    // the method signature) is a structure, it relies on the loaded type to get the layout information from. For ordinary
    // structures we are guaranteed to have loaded the type before entering prestub - the caller must have loaded it.
    // However due to type equivalence, the caller may work with a different type than what's in the method signature.
    //
    // We deal with situation by eagerly loading types that may cause these problems, i.e. value types in signatures of
    // methods introduced by this type. To avoid the perf hit for scenarios without type equivalence, we only preload
    // structures that marked as type equivalent. In the no-PIA world
    // these structures are called "local types" and are usually generated automatically by the compiler. Note that there
    // is a related logic in code:CompareTypeDefsForEquivalence that declares two tokens corresponding to structures as
    // equivalent based on an extensive set of equivalency checks.

#ifdef FEATURE_TYPEEQUIVALENCE
    if ((level == CLASS_LOADED)
        && (GetCl() != mdTypeDefNil)
        && !ContainsGenericVariables())
    {
        MethodTable::IntroducedMethodIterator itMethods(this, FALSE);
        for (; itMethods.IsValid(); itMethods.Next())
        {
            MethodDesc * pMD = itMethods.GetMethodDesc();
            if (!pMD->DoesNotHaveEquivalentValuetypeParameters() && pMD->IsVirtual())
            {
                locals.fHasEquivalentStructParameter = FALSE;
                pMD->WalkValueTypeParameters(this, CheckForEquivalenceAndFullyLoadType, &locals);
                if (!locals.fHasEquivalentStructParameter)
                    pMD->SetDoesNotHaveEquivalentValuetypeParameters();
            }
        }
    }
#endif //FEATURE_TYPEEQUIVALENCE

    // The rules for constraint cycles are same as rules for access checks
    if (fNeedAccessChecks)
    {
        // Check for cyclical class constraints
        {
            Instantiation formalParams = GetInstantiation();

            for (DWORD i = 0; i < formalParams.GetNumArgs(); i++)
            {
                BOOL Bounded(TypeVarTypeDesc *tyvar, DWORD depth);

                TypeVarTypeDesc *pTyVar = formalParams[i].AsGenericVariable();
                pTyVar->LoadConstraints(CLASS_DEPENDENCIES_LOADED);
                if (!Bounded(pTyVar, formalParams.GetNumArgs()))
                {
                    COMPlusThrow(kTypeLoadException, VER_E_CIRCULAR_VAR_CONSTRAINTS);
                }

                DoAccessibilityCheckForConstraints(this, pTyVar, E_ACCESSDENIED);
            }
        }

        // Check for cyclical method constraints
        {
            if (GetCl() != mdTypeDefNil)  // Make sure this is actually a metadata type!
            {
                MethodTable::IntroducedMethodIterator itMethods(this, FALSE);
                for (; itMethods.IsValid(); itMethods.Next())
                {
                    MethodDesc * pMD = itMethods.GetMethodDesc();

                    if (pMD->IsGenericMethodDefinition() && pMD->IsTypicalMethodDefinition())
                    {
                        BOOL fHasCircularClassConstraints = TRUE;
                        BOOL fHasCircularMethodConstraints = TRUE;

                        pMD->LoadConstraintsForTypicalMethodDefinition(&fHasCircularClassConstraints, &fHasCircularMethodConstraints, CLASS_DEPENDENCIES_LOADED);

                        if (fHasCircularClassConstraints)
                        {
                            COMPlusThrow(kTypeLoadException, VER_E_CIRCULAR_VAR_CONSTRAINTS);
                        }
                        if (fHasCircularMethodConstraints)
                        {
                            COMPlusThrow(kTypeLoadException, VER_E_CIRCULAR_MVAR_CONSTRAINTS);
                        }
                    }
                }
            }
        }

    }


#ifdef _DEBUG
    if (LoggingOn(LF_CLASSLOADER, LL_INFO10000))
    {
        SString name;
        TypeString::AppendTypeDebug(name, this);
        LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: Completed full dependency load of type %s\n", name.GetUTF8()));
    }
#endif

    switch (level)
    {
        case CLASS_DEPENDENCIES_LOADED:
            SetIsDependenciesLoaded();
            break;

        case CLASS_LOADED:
            if (!IsTypicalTypeDefinition() &&
                !IsSharedByGenericInstantiations())
            {
                TypeHandle thThis = TypeHandle(this);

                // If we got here, we about to mark a generic instantiation as fully loaded. Before we do so,
                // check to see if has constraints that aren't being satisfied.
                SatisfiesClassConstraints(thThis, typicalTypeHnd, pInstContext);

            }

            // Validate implementation of virtual static methods on all implemented interfaces unless:
            // 1) The type resides in a module where sanity checks are disabled (such as System.Private.CoreLib, or an
            //    R2R module with type checks disabled)
            // 2) There are no virtual static methods defined on any of the interfaces implemented by this type;
            // 3) The type is abstract in which case it's allowed to leave some virtual static methods unimplemented
            //    akin to equivalent behavior of virtual instance method overriding in abstract classes;
            // 4) The type is a not the typical type definition. (The typical type is always checked)

            if (fNeedsSanityChecks &&
                IsTypicalTypeDefinition() &&
                !IsAbstract())
            {
                if (HasVirtualStaticMethods())
                    VerifyThatAllVirtualStaticMethodsAreImplemented();
            }

            if (locals.fBailed)
            {
                // We couldn't complete security checks on some dependency because it is already being processed by one of our callers.
                // Do not mark this class fully loaded yet. Put it on the pending list and it will be marked fully loaded when
                // everything unwinds.

                *pfBailed = TRUE;

                TypeHandle *pTHPending = pPending->AppendThrowing();
                *pTHPending = TypeHandle(this);
            }
            else
            {
                // Finally, mark this method table as fully loaded
                SetIsFullyLoaded();
            }
            break;

        default:
            _ASSERTE(!"Can't get here.");
            break;

    }
#endif //!DACCESS_COMPILE
} //MethodTable::DoFullyLoad


#ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP

//==========================================================================================
BOOL MethodTable::IsExtensibleRCW()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(GetClass());
    return IsComObjectType() && !GetClass()->IsComImport();
}

//==========================================================================================
OBJECTHANDLE MethodTable::GetOHDelegate()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(GetClass());
    return GetClass()->GetOHDelegate();
}

//==========================================================================================
void MethodTable::SetOHDelegate (OBJECTHANDLE _ohDelegate)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(GetClass());
    GetClass()->SetOHDelegate(_ohDelegate);
}

//==========================================================================================
// Helper to skip over COM class in the hierarchy
MethodTable* MethodTable::GetComPlusParentMethodTable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    MethodTable* pParent = GetParentMethodTable();

    if (pParent && pParent->IsComImport())
    {
        // Skip the single ComImport class we expect
        _ASSERTE(pParent->GetParentMethodTable() != NULL);
        pParent = pParent->GetParentMethodTable();
        _ASSERTE(!pParent->IsComImport());

        // Skip over System.__ComObject, expect System.MarshalByRefObject
        pParent=pParent->GetParentMethodTable();
        _ASSERTE(pParent != NULL);
        _ASSERTE(pParent->GetParentMethodTable() != NULL);
        _ASSERTE(pParent->GetParentMethodTable() == g_pObjectClass);
    }

    return pParent;
}

#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE

//==========================================================================================
// Return a pointer to the dictionary for an instantiated type
// Return NULL if not instantiated
PTR_Dictionary MethodTable::GetDictionary()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (HasInstantiation())
    {
        // The instantiation for this class is stored in the type slots table
        // *after* any inherited slots
        return GetPerInstInfo()[GetNumDicts()-1];
    }
    else
    {
        return NULL;
    }
}

//==========================================================================================
// As above, but assert if an instantiated type is not restored
Instantiation MethodTable::GetInstantiation()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    if (HasInstantiation())
    {
        PTR_GenericsDictInfo  pDictInfo = GetGenericsDictInfo();
        return Instantiation(GetPerInstInfo()[pDictInfo->m_wNumDicts-1]->GetInstantiation(), pDictInfo->m_wNumTyPars);
    }
    else
    {
        return Instantiation();
    }
}

//==========================================================================================
// Obtain instantiation from an instantiated type or a pointer to the
// element type of an array
Instantiation MethodTable::GetClassOrArrayInstantiation()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    if (IsArray()) {
        return GetArrayInstantiation();
    }
    else {
        return GetInstantiation();
    }
}

//==========================================================================================
Instantiation MethodTable::GetArrayInstantiation()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(IsArray());
    return Instantiation((TypeHandle *)&m_ElementTypeHnd, 1);
}

//==========================================================================================
CorElementType MethodTable::GetInternalCorElementType()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // This should not touch the EEClass, at least not in the
    // common cases of ELEMENT_TYPE_CLASS and ELEMENT_TYPE_VALUETYPE.
    CorElementType ret;

    switch (GetFlag(enum_flag_Category_ElementTypeMask))
    {
    case enum_flag_Category_Array:
        ret = ELEMENT_TYPE_ARRAY;
        break;

    case enum_flag_Category_Array | enum_flag_Category_IfArrayThenSzArray:
        ret = ELEMENT_TYPE_SZARRAY;
        break;

    case enum_flag_Category_ValueType:
        ret = ELEMENT_TYPE_VALUETYPE;
        break;

    case enum_flag_Category_PrimitiveValueType:
        // This path should only be taken for the builtin CoreLib types
        // and primitive valuetypes
        ret = GetClass()->GetInternalCorElementType();
        _ASSERTE((ret != ELEMENT_TYPE_CLASS) &&
                    (ret != ELEMENT_TYPE_VALUETYPE));
        break;

    default:
        ret = ELEMENT_TYPE_CLASS;
        break;
    }

    // DAC may be targeting a dump; dumps do not guarantee you can retrieve the EEClass from
    // the MethodTable so this is not expected to work in a DAC build.
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    if (IsRestored())
    {
        PTR_EEClass pClass = GetClass();
        if (ret != pClass->GetInternalCorElementType())
        {
            _ASSERTE(!"Mismatched results in MethodTable::GetInternalCorElementType");
        }
    }
#endif // defined(_DEBUG) && !defined(DACCESS_COMPILE)
    return ret;
}

//==========================================================================================
CorElementType MethodTable::GetVerifierCorElementType()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // This should not touch the EEClass, at least not in the
    // common cases of ELEMENT_TYPE_CLASS and ELEMENT_TYPE_VALUETYPE.
    CorElementType ret;

    switch (GetFlag(enum_flag_Category_ElementTypeMask))
    {
    case enum_flag_Category_Array:
        ret = ELEMENT_TYPE_ARRAY;
        break;

    case enum_flag_Category_Array | enum_flag_Category_IfArrayThenSzArray:
        ret = ELEMENT_TYPE_SZARRAY;
        break;

    case enum_flag_Category_ValueType:
        ret = ELEMENT_TYPE_VALUETYPE;
        break;

    case enum_flag_Category_PrimitiveValueType:
        //
        // This is the only difference from MethodTable::GetInternalCorElementType()
        //
        if (IsTruePrimitive() || IsEnum())
            ret = GetClass()->GetInternalCorElementType();
        else
            ret = ELEMENT_TYPE_VALUETYPE;
        break;

    default:
        ret = ELEMENT_TYPE_CLASS;
        break;
    }

    return ret;
}

//==========================================================================================
CorElementType MethodTable::GetSignatureCorElementType()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // This should not touch the EEClass, at least not in the
    // common cases of ELEMENT_TYPE_CLASS and ELEMENT_TYPE_VALUETYPE.
    CorElementType ret;

    switch (GetFlag(enum_flag_Category_ElementTypeMask))
    {
    case enum_flag_Category_Array:
        ret = ELEMENT_TYPE_ARRAY;
        break;

    case enum_flag_Category_Array | enum_flag_Category_IfArrayThenSzArray:
        ret = ELEMENT_TYPE_SZARRAY;
        break;

    case enum_flag_Category_ValueType:
        ret = ELEMENT_TYPE_VALUETYPE;
        break;

    case enum_flag_Category_PrimitiveValueType:
        //
        // This is the only difference from MethodTable::GetInternalCorElementType()
        //
        if (IsTruePrimitive())
            ret = GetClass()->GetInternalCorElementType();
        else
            ret = ELEMENT_TYPE_VALUETYPE;
        break;

    default:
        ret = ELEMENT_TYPE_CLASS;
        break;
    }

    return ret;
}

#ifndef DACCESS_COMPILE

//==========================================================================================
void MethodTable::SetInternalCorElementType (CorElementType _NormType)
{
    WRAPPER_NO_CONTRACT;

    switch (_NormType)
    {
    case ELEMENT_TYPE_CLASS:
        _ASSERTE(!IsArray());
        // Nothing to do
        break;
    case ELEMENT_TYPE_VALUETYPE:
        SetFlag(enum_flag_Category_ValueType);
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == enum_flag_Category_ValueType);
        break;
    default:
        SetFlag(enum_flag_Category_PrimitiveValueType);
        _ASSERTE(GetFlag(enum_flag_Category_Mask) == enum_flag_Category_PrimitiveValueType);
        break;
    }

    GetClass()->SetInternalCorElementType(_NormType);
    _ASSERTE(GetInternalCorElementType() == _NormType);
}

#endif // !DACCESS_COMPILE

#ifdef FEATURE_TYPEEQUIVALENCE
#ifndef DACCESS_COMPILE

WORD GetEquivalentMethodSlot(MethodTable * pOldMT, MethodTable * pNewMT, WORD wMTslot, BOOL *pfFound)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    *pfFound = FALSE;

    WORD wVTslot = wMTslot;

#ifdef FEATURE_COMINTEROP
    // Get the COM vtable slot corresponding to the given MT slot
    if (pOldMT->IsSparseForCOMInterop())
        wVTslot = pOldMT->GetClass()->GetSparseCOMInteropVTableMap()->LookupVTSlot(wMTslot);

    // If the other MT is not sparse, we can return the COM slot directly
    if (!pNewMT->IsSparseForCOMInterop())
    {
        if (wVTslot < pNewMT->GetNumVirtuals())
            *pfFound = TRUE;

        return wVTslot;
    }

    // Otherwise we iterate over all virtuals in the other MT trying to find a match
    for (WORD wSlot = 0; wSlot < pNewMT->GetNumVirtuals(); wSlot++)
    {
        if (wVTslot == pNewMT->GetClass()->GetSparseCOMInteropVTableMap()->LookupVTSlot(wSlot))
        {
            *pfFound = TRUE;
            return wSlot;
        }
    }

    _ASSERTE(!*pfFound);
    return 0;

#else
    // No COM means there is no sparse interface
    if (wVTslot < pNewMT->GetNumVirtuals())
        *pfFound = TRUE;

    return wVTslot;

#endif // FEATURE_COMINTEROP
}
#endif // #ifdef DACCESS_COMPILE
#endif // #ifdef FEATURE_TYPEEQUIVALENCE

//==========================================================================================
BOOL
MethodTable::FindEncodedMapDispatchEntry(
    UINT32             typeID,
    UINT32             slotNumber,
    DispatchMapEntry * pEntry)
{
    CONTRACTL {
        // NOTE: LookupDispatchMapType may or may not throw. Currently, it
        // should never throw because lazy interface restore is disabled.
        THROWS;
        GC_TRIGGERS;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pEntry));
        PRECONDITION(typeID != TYPE_ID_THIS_CLASS);
    } CONTRACTL_END;

    CONSISTENCY_CHECK(HasDispatchMap());

    MethodTable * dispatchTokenType = GetThread()->GetDomain()->LookupType(typeID);

    // Search for an exact type match.
    {
        DispatchMap::EncodedMapIterator it(this);
        for (; it.IsValid(); it.Next())
        {
            DispatchMapEntry * pCurEntry = it.Entry();
            if (pCurEntry->GetSlotNumber() == slotNumber)
            {
                if (DispatchMapTypeMatchesMethodTable(pCurEntry->GetTypeID(), dispatchTokenType))
                {
                    *pEntry = *pCurEntry;
                    return TRUE;
                }
            }
        }
    }

    // Repeat the search if any variance is involved, allowing a CanCastTo match.  (We do
    // this in a separate pass because we want to avoid touching the type
    // to see if it has variance or not)
    //
    // NOTE: CERs are not guaranteed for interfaces with co- and contra-variance involved.
    if (dispatchTokenType->HasVariance() || dispatchTokenType->HasTypeEquivalence())
    {
        DispatchMap::EncodedMapIterator it(this);
        for (; it.IsValid(); it.Next())
        {
            DispatchMapEntry * pCurEntry = it.Entry();
            if (pCurEntry->GetSlotNumber() == slotNumber)
            {
#ifndef DACCESS_COMPILE
                MethodTable * pCurEntryType = LookupDispatchMapType(pCurEntry->GetTypeID());
                //@TODO: This is currently not guaranteed to work without throwing,
                //@TODO: even with lazy interface restore disabled.
                if (dispatchTokenType->HasVariance() &&
                    pCurEntryType->CanCastByVarianceToInterfaceOrDelegate(dispatchTokenType, NULL))
                {
                    *pEntry = *pCurEntry;
                    return TRUE;
                }

                if (dispatchTokenType->HasInstantiation() && dispatchTokenType->HasTypeEquivalence())
                {
                    if (dispatchTokenType->IsEquivalentTo(pCurEntryType))
                    {
                        *pEntry = *pCurEntry;
                        return TRUE;
                    }
                }
#endif // !DACCESS_COMPILE
            }
#if !defined(DACCESS_COMPILE) && defined(FEATURE_TYPEEQUIVALENCE)
            if (this->HasTypeEquivalence() &&
                !dispatchTokenType->HasInstantiation() &&
                dispatchTokenType->HasTypeEquivalence() &&
                dispatchTokenType->GetClass()->IsEquivalentType())
            {
                _ASSERTE(dispatchTokenType->IsInterface());
                MethodTable * pCurEntryType = LookupDispatchMapType(pCurEntry->GetTypeID());

                if (pCurEntryType->IsEquivalentTo(dispatchTokenType))
                {
                    MethodDesc * pMD = dispatchTokenType->GetMethodDescForSlot(slotNumber);
                    _ASSERTE(FitsIn<WORD>(slotNumber));
                    BOOL fNewSlotFound = FALSE;
                    DWORD newSlot = GetEquivalentMethodSlot(
                        dispatchTokenType,
                        pCurEntryType,
                        static_cast<WORD>(slotNumber),
                        &fNewSlotFound);
                    if (fNewSlotFound && (newSlot == pCurEntry->GetSlotNumber()))
                    {
                        MethodDesc * pNewMD = pCurEntryType->GetMethodDescForSlot(newSlot);

                        MetaSig msig(pMD);
                        MetaSig msignew(pNewMD);

                        if (MetaSig::CompareMethodSigs(msig, msignew, FALSE))
                        {
                            *pEntry = *pCurEntry;
                            return TRUE;
                        }
                    }
                }
            }
#endif
        }
    }
    return FALSE;
} // MethodTable::FindEncodedMapDispatchEntry

//==========================================================================================
BOOL MethodTable::FindDispatchEntryForCurrentType(UINT32 typeID,
                                                  UINT32 slotNumber,
                                                  DispatchMapEntry *pEntry)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pEntry));
        PRECONDITION(typeID != TYPE_ID_THIS_CLASS);
    } CONTRACTL_END;

    BOOL fRes = FALSE;

    if (HasDispatchMap())
    {
        fRes = FindEncodedMapDispatchEntry(
            typeID, slotNumber, pEntry);
    }

    return fRes;
}

//==========================================================================================
BOOL MethodTable::FindDispatchEntry(UINT32 typeID,
                                    UINT32 slotNumber,
                                    DispatchMapEntry *pEntry)
{
    CONTRACT (BOOL) {
        INSTANCE_CHECK;
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
        POSTCONDITION(!RETVAL || pEntry->IsValid());
        PRECONDITION(typeID != TYPE_ID_THIS_CLASS);
    } CONTRACT_END;

    // Start at the current type and work up the inheritance chain
    MethodTable *pCurMT = this;
    UINT32 iCurInheritanceChainDelta = 0;
    while (pCurMT != NULL)
    {
        if (pCurMT->FindDispatchEntryForCurrentType(
                typeID, slotNumber, pEntry))
        {
            RETURN (TRUE);
        }
        pCurMT = pCurMT->GetParentMethodTable();
        iCurInheritanceChainDelta++;
    }
    RETURN (FALSE);
}

#ifndef DACCESS_COMPILE

void ThrowExceptionForAbstractOverride(
    MethodTable *pTargetClass,
    MethodTable *pInterfaceMT,
    MethodDesc *pInterfaceMD)
{
    LIMITED_METHOD_CONTRACT;

    SString assemblyName;

    pTargetClass->GetAssembly()->GetDisplayName(assemblyName);

    SString strInterfaceName;
    TypeString::AppendType(strInterfaceName, TypeHandle(pInterfaceMT));

    SString strMethodName;
    TypeString::AppendMethod(strMethodName, pInterfaceMD, pInterfaceMD->GetMethodInstantiation());

    SString strTargetClassName;
    TypeString::AppendType(strTargetClassName, pTargetClass);

    COMPlusThrow(
        kEntryPointNotFoundException,
        IDS_CLASSLOAD_METHOD_NOT_IMPLEMENTED,
        strMethodName,
        strInterfaceName,
        strTargetClassName,
        assemblyName);
}

#endif // !DACCESS_COMPILE

//==========================================================================================
// Possible cases:
//      1. Typed (interface) contract
//          a. To non-virtual implementation (NYI). Just
//             return the DispatchSlot as the implementation
//          b. Mapped virtually to virtual slot on 'this'. Need to
//             further resolve the new 'this' virtual slot.
//      2. 'this' contract
//          a. To non-virtual implementation. Return the DispatchSlot
//             as the implementation.
//          b. Mapped virtually to another virtual slot. Need to further
//             resolve the new slot on 'this'.
BOOL
MethodTable::FindDispatchImpl(
    UINT32         typeID,
    UINT32         slotNumber,
    DispatchSlot * pImplSlot,
    BOOL           throwOnConflict)
{
    CONTRACT (BOOL) {
        INSTANCE_CHECK;
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pImplSlot));
        POSTCONDITION(!RETVAL || !pImplSlot->IsNull() || IsComObjectType());
    } CONTRACT_END;

    LOG((LF_LOADER, LL_INFO10000, "SD: MT::FindDispatchImpl: searching %s.\n", GetClass()->GetDebugClassName()));

    ///////////////////////////////////
    // 1. Typed (interface) contract

    INDEBUG(MethodTable *dbg_pMTTok = NULL; dbg_pMTTok = this;)
    DispatchMapEntry declEntry;
    DispatchMapEntry implEntry;

#ifndef DACCESS_COMPILE
    if (typeID != TYPE_ID_THIS_CLASS)
    {
        INDEBUG(dbg_pMTTok = GetThread()->GetDomain()->LookupType(typeID));
        DispatchMapEntry e;
        if (!FindDispatchEntry(typeID, slotNumber, &e))
        {
            // Figure out the interface being called
            MethodTable *pIfcMT = GetThread()->GetDomain()->LookupType(typeID);

            // Figure out which method of the interface the caller requested.
            MethodDesc * pIfcMD = pIfcMT->GetMethodDescForSlot(slotNumber);

            // A call to an array thru IList<T> (or IEnumerable<T> or ICollection<T>) has to be handled specially.
            // These interfaces are "magic" (mostly due to working set concerned - they are created on demand internally
            // even though semantically, these are static interfaces.)
            //
            // NOTE: CERs are not currently supported with generic array interfaces.
            if (IsArray())
            {
                // At this, we know that we're trying to cast an array to an interface and that the normal static lookup failed.

                // FindDispatchImpl assumes that the cast is legal so we should be able to assume now that it is a valid
                // IList<T> call thru an array.

                // Get the MT of IList<T> or IReadOnlyList<T>


                // Quick sanity check
                if (!(pIfcMT->HasInstantiation()))
                {
                    _ASSERTE(!"Should not have gotten here. If you did, it's probably because multiple interface instantiation hasn't been checked in yet. This code only works on top of that.");
                    RETURN(FALSE);
                }

                // Get the type of T (as in IList<T>)
                TypeHandle theT = pIfcMT->GetInstantiation()[0];

                // Retrieve the corresponding method of SZArrayHelper. This is what will actually execute.
                // This method will be an instantiation of a generic method. I.e. if the caller requested
                // IList<T>.Meth(), he will actually be diverted to SZArrayHelper.Meth<T>().
                MethodDesc * pActualImplementor = GetActualImplementationForArrayGenericIListOrIReadOnlyListMethod(pIfcMD, theT);

                // Now, construct a DispatchSlot to return in *pImplSlot
                DispatchSlot ds(pActualImplementor->GetMethodEntryPoint());

                if (pImplSlot != NULL)
                {
                   *pImplSlot = ds;
                }

                RETURN(TRUE);

            }
            else
            {
                //
                // See if we can find a default method from one of the implemented interfaces
                //

                // Try exact match first
                MethodDesc *pDefaultMethod = NULL;

                FindDefaultInterfaceImplementationFlags flags = FindDefaultInterfaceImplementationFlags::InstantiateFoundMethodDesc;
                if (throwOnConflict)
                    flags = flags | FindDefaultInterfaceImplementationFlags::ThrowOnConflict;

                BOOL foundDefaultInterfaceImplementation  = FindDefaultInterfaceImplementation(
                    pIfcMD,     // the interface method being resolved
                    pIfcMT,     // the interface being resolved
                    &pDefaultMethod,
                    flags);

                // If there's no exact match, try a variant match
                if (!foundDefaultInterfaceImplementation && pIfcMT->HasVariance())
                {
                    flags = flags | FindDefaultInterfaceImplementationFlags::AllowVariance;
                    foundDefaultInterfaceImplementation = FindDefaultInterfaceImplementation(
                        pIfcMD,     // the interface method being resolved
                        pIfcMT,     // the interface being resolved
                        &pDefaultMethod,
                        flags);
                }

                if (foundDefaultInterfaceImplementation)
                {
                    //
                    // If the default implementation we found is abstract, we hit a reabstraction.
                    //
                    // interface IFoo { void Frob() { ... } }
                    // interface IBar { abstract void IFoo.Frob() }
                    // class Foo : IBar { /* IFoo.Frob not implemented here */ }
                    //
                    if (pDefaultMethod->IsAbstract())
                    {
                        if (throwOnConflict)
                        {
                            ThrowExceptionForAbstractOverride(this, pIfcMT, pIfcMD);
                        }
                    }
                    else
                    {
                        // Now, construct a DispatchSlot to return in *pImplSlot
                        DispatchSlot ds(pDefaultMethod->GetMethodEntryPoint());

                        if (pImplSlot != NULL)
                        {
                            *pImplSlot = ds;
                        }

                        RETURN(TRUE);
                    }
                }
            }

            // This contract is not implemented by this class or any parent class.
            RETURN(FALSE);
        }


        /////////////////////////////////
        // 1.1. Update the typeID and slotNumber so that the full search can commense below
        typeID = TYPE_ID_THIS_CLASS;
        slotNumber = e.GetTargetSlotNumber();
    }
#endif // !DACCESS_COMPILE

    //////////////////////////////////
    // 2. 'this' contract

    // Just grab the target out of the vtable
    *pImplSlot = GetRestoredSlot(slotNumber);

    // Successfully determined the target for the given target
    RETURN (TRUE);
}

#ifndef DACCESS_COMPILE

struct MatchCandidate
{
    MethodTable *pMT;
    MethodDesc *pMD;
};

void ThrowExceptionForConflictingOverride(
    MethodTable *pTargetClass,
    MethodTable *pInterfaceMT,
    MethodDesc *pInterfaceMD)
{
    LIMITED_METHOD_CONTRACT;

    SString assemblyName;

    pTargetClass->GetAssembly()->GetDisplayName(assemblyName);

    SString strInterfaceName;
    TypeString::AppendType(strInterfaceName, TypeHandle(pInterfaceMT));

    SString strMethodName;
    TypeString::AppendMethod(strMethodName, pInterfaceMD, pInterfaceMD->GetMethodInstantiation());

    SString strTargetClassName;
    TypeString::AppendType(strTargetClassName, pTargetClass);

    COMPlusThrow(
        kAmbiguousImplementationException,
        IDS_CLASSLOAD_AMBIGUOUS_OVERRIDE,
        strMethodName,
        strInterfaceName,
        strTargetClassName,
        assemblyName);
}

namespace
{
    bool TryGetCandidateImplementation(
        MethodTable *pMT,
        MethodDesc *interfaceMD,
        MethodTable *interfaceMT,
        FindDefaultInterfaceImplementationFlags findDefaultImplementationFlags,
        MethodDesc **candidateMD,
        ClassLoadLevel level)
    {
        bool allowVariance = (findDefaultImplementationFlags & FindDefaultInterfaceImplementationFlags::AllowVariance) != FindDefaultInterfaceImplementationFlags::None;
        bool instantiateMethodInstantiation = (findDefaultImplementationFlags & FindDefaultInterfaceImplementationFlags::InstantiateFoundMethodDesc) != FindDefaultInterfaceImplementationFlags::None;

        *candidateMD = NULL;

        MethodDesc *candidateMaybe = NULL;
        if (pMT == interfaceMT)
        {
            if (!interfaceMD->IsAbstract())
            {
                // exact match
                candidateMaybe = interfaceMD;
            }
        }
        else if (pMT->CanCastToInterface(interfaceMT))
        {
            if (pMT->HasSameTypeDefAs(interfaceMT))
            {
                if (allowVariance && !interfaceMD->IsAbstract())
                {
                    // Generic variance match - we'll instantiate pCurMD with the right type arguments later
                    candidateMaybe = interfaceMD;
                }
            }
            else if (!interfaceMD->IsStatic())
            {
                //
                // A more specific interface - search for an methodimpl for explicit override
                // Implicit override in default interface methods are not allowed
                //
                MethodTable::MethodIterator methodIt(pMT);
                for (; methodIt.IsValid() && candidateMaybe == NULL; methodIt.Next())
                {
                    MethodDesc *pMD = methodIt.GetMethodDesc();
                    int targetSlot = interfaceMD->GetSlot();

                    if (pMD->IsMethodImpl())
                    {
                        // We have a MethodImpl with slots - iterate over all the declarations it's implementing,
                        // looking for the interface method we need.
                        MethodImpl::Iterator it(pMD);
                        for (; it.IsValid() && candidateMaybe == NULL; it.Next())
                        {
                            MethodDesc *pDeclMD = it.GetMethodDesc();

                            // Is this the right slot?
                            if (pDeclMD->GetSlot() != targetSlot)
                                continue;

                            // Is this the right interface?
                            if (!pDeclMD->HasSameMethodDefAs(interfaceMD))
                                continue;

                            if (interfaceMD->HasClassInstantiation())
                            {
                                // pInterfaceMD will be in the canonical form, so we need to check the specific
                                // instantiation against pInterfaceMT.
                                //
                                // The parent of pDeclMD is unreliable for this purpose because it may or
                                // may not be canonicalized. Let's go from the metadata.

                                SigTypeContext typeContext = SigTypeContext(pMT);

                                mdTypeRef tkParent;
                                IfFailThrow(pMD->GetModule()->GetMDImport()->GetParentToken(it.GetToken(), &tkParent));

                                MethodTable *pDeclMT = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
                                    pMD->GetModule(),
                                    tkParent,
                                    &typeContext).AsMethodTable();

                                // We do CanCastToInterface to also cover variance.
                                // We already know this is a method on the same type definition as the (generic)
                                // interface but we need to make sure the instantiations match.
                                if ((allowVariance && pDeclMT->CanCastToInterface(interfaceMT))
                                    || pDeclMT == interfaceMT)
                                {
                                    // We have a match
                                    candidateMaybe = pMD;
                                }
                            }
                            else
                            {
                                // No generics involved. If the method definitions match, it's a match.
                                candidateMaybe = pMD;
                            }
                        }
                    }
                }
            }
            else
            {
                // Static virtual methods don't record MethodImpl slots so they need special handling
                ResolveVirtualStaticMethodFlags resolveVirtualStaticMethodFlags = ResolveVirtualStaticMethodFlags::None;
                if (allowVariance)
                {
                    resolveVirtualStaticMethodFlags |= ResolveVirtualStaticMethodFlags::AllowVariantMatches;
                }
                if (instantiateMethodInstantiation)
                {
                    resolveVirtualStaticMethodFlags |= ResolveVirtualStaticMethodFlags::InstantiateResultOverFinalMethodDesc;
                }

                candidateMaybe = pMT->TryResolveVirtualStaticMethodOnThisType(
                    interfaceMT,
                    interfaceMD,
                    resolveVirtualStaticMethodFlags,
                    /* level */ level);
            }
        }

        if (candidateMaybe == NULL)
            return false;

        if (candidateMaybe->HasClassOrMethodInstantiation())
        {
            // Instantiate the MethodDesc
            // We don't want generic dictionary from this pointer - we need pass secret type argument
            // from instantiating stubs to resolve ambiguity
            candidateMaybe = MethodDesc::FindOrCreateAssociatedMethodDesc(
                candidateMaybe,
                pMT,
                FALSE,                  // forceBoxedEntryPoint
                candidateMaybe->HasMethodInstantiation() ?
                candidateMaybe->AsInstantiatedMethodDesc()->IMD_GetMethodInstantiation() :
                Instantiation(),        // for method themselves that are generic
                FALSE,                  // allowInstParam
                TRUE,                   // forceRemoteableMethod
                TRUE,                   // allowCreate
                level                   // level
            );
        }

        *candidateMD = candidateMaybe;
        return true;
    }
}

// Find the default interface implementation method for interface dispatch
// It is either the interface method with default interface method implementation,
// or an most specific interface with an explicit methodimpl overriding the method
BOOL MethodTable::FindDefaultInterfaceImplementation(
    MethodDesc *pInterfaceMD,
    MethodTable *pInterfaceMT,
    MethodDesc **ppDefaultMethod,
    FindDefaultInterfaceImplementationFlags findDefaultImplementationFlags,
    ClassLoadLevel level
)
{
    CONTRACT(BOOL) {
        INSTANCE_CHECK;
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pInterfaceMD));
        PRECONDITION(CheckPointer(pInterfaceMT));
        PRECONDITION(CheckPointer(ppDefaultMethod));
        POSTCONDITION(!RETVAL || (*ppDefaultMethod) != nullptr);
    } CONTRACT_END;

#ifdef FEATURE_DEFAULT_INTERFACES
    bool allowVariance = (findDefaultImplementationFlags & FindDefaultInterfaceImplementationFlags::AllowVariance) != FindDefaultInterfaceImplementationFlags::None;
    CQuickArray<MatchCandidate> candidates;
    unsigned candidatesCount = 0;

    // Check the current method table itself
    MethodDesc *candidateMaybe = NULL;
    if (IsInterface() && TryGetCandidateImplementation(this, pInterfaceMD, pInterfaceMT, findDefaultImplementationFlags, &candidateMaybe, level))
    {
        _ASSERTE(candidateMaybe != NULL);

        candidates.AllocThrows(this->GetNumInterfaces() + 1);
        candidates[candidatesCount].pMT = this;
        candidates[candidatesCount].pMD = candidateMaybe;
        candidatesCount++;
    }
    else
    {
        candidates.AllocThrows(this->GetNumInterfaces());
    }

    //
    // Walk interface from derived class to parent class
    // We went with a straight-forward implementation as in most cases the number of interfaces are small
    // and the result of the interface dispatch are already cached. If there are significant usage of default
    // interface methods in highly complex interface hierarchies we can revisit this
    //
    MethodTable *pMT = this;

    while (pMT != NULL)
    {
        MethodTable *pParentMT = pMT->GetParentMethodTable();
        unsigned dwParentInterfaces = 0;
        if (pParentMT)
            dwParentInterfaces = pParentMT->GetNumInterfaces();

        // Scanning only current class only if the current class have more interface than parent
        // (parent interface are laid out first in interface map)
        if (pMT->GetNumInterfaces() > dwParentInterfaces)
        {
            // Only iterate the interfaceimpls on current class
            MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMapFrom(dwParentInterfaces);
            while (!it.Finished())
            {
                MethodTable *pCurMT = it.GetInterface(pMT, level);

                MethodDesc *pCurMD = NULL;
                if (TryGetCandidateImplementation(pCurMT, pInterfaceMD, pInterfaceMT, findDefaultImplementationFlags, &pCurMD, level))
                {
                    //
                    // Found a match. But is it a more specific match (we want most specific interfaces)
                    //
                    _ASSERTE(pCurMD != NULL);
                    bool needToInsert = true;
                    bool seenMoreSpecific = false;

                    // We need to maintain the invariant that the candidates are always the most specific
                    // in all path scaned so far. There might be multiple incompatible candidates
                    for (unsigned i = 0; i < candidatesCount; ++i)
                    {
                        MethodTable *pCandidateMT = candidates[i].pMT;
                        if (pCandidateMT == NULL)
                            continue;

                        if (pCandidateMT == pCurMT)
                        {
                            // A dup - we are done
                            needToInsert = false;
                            break;
                        }

                        if (allowVariance && pCandidateMT->HasSameTypeDefAs(pCurMT))
                        {
                            // Variant match on the same type - this is a tie
                        }
                        else if (pCurMT->CanCastToInterface(pCandidateMT))
                        {
                            // pCurMT is a more specific choice than IFoo/IBar both overrides IBlah :
                            if (!seenMoreSpecific)
                            {
                                seenMoreSpecific = true;
                                candidates[i].pMT = pCurMT;
                                candidates[i].pMD = pCurMD;
                            }
                            else
                            {
                                candidates[i].pMT = NULL;
                                candidates[i].pMD = NULL;
                            }

                            needToInsert = false;
                        }
                        else if (pCandidateMT->CanCastToInterface(pCurMT))
                        {
                            // pCurMT is less specific - we don't need to scan more entries as this entry can
                            // represent pCurMT (other entries are incompatible with pCurMT)
                            needToInsert = false;
                            break;
                        }
                        else
                        {
                            // pCurMT is incompatible - keep scanning
                        }
                    }

                    if (needToInsert)
                    {
                        ASSERT(candidatesCount < candidates.Size());
                        candidates[candidatesCount].pMT = pCurMT;
                        candidates[candidatesCount].pMD = pCurMD;
                        candidatesCount++;
                    }
                }

                it.Next();
            }
        }

        pMT = pParentMT;
    }

    // scan to see if there are any conflicts
    // If we are doing second pass (allowing variance), we know don't actually look for
    // a conflict anymore, but pick the first match.
    MethodTable *pBestCandidateMT = NULL;
    MethodDesc *pBestCandidateMD = NULL;
    for (unsigned i = 0; i < candidatesCount; ++i)
    {
        if (candidates[i].pMT == NULL)
            continue;

        if (pBestCandidateMT == NULL)
        {
            pBestCandidateMT = candidates[i].pMT;
            pBestCandidateMD = candidates[i].pMD;

            // If this is a second pass lookup, we know this is a variant match. As such
            // we pick the first result as the winner and don't look for a conflict for instance methods.
            if (allowVariance && !pInterfaceMD->IsStatic())
                break;
        }
        else if (pBestCandidateMT != candidates[i].pMT)
        {
            bool throwOnConflict = (findDefaultImplementationFlags & FindDefaultInterfaceImplementationFlags::ThrowOnConflict) != FindDefaultInterfaceImplementationFlags::None;

            if (throwOnConflict)
                ThrowExceptionForConflictingOverride(this, pInterfaceMT, pInterfaceMD);

            *ppDefaultMethod = pBestCandidateMD;
            RETURN(FALSE);
        }
    }

    if (pBestCandidateMD != NULL)
    {
        *ppDefaultMethod = pBestCandidateMD;
        RETURN(TRUE);
    }
#else
    *ppDefaultMethod = NULL;
#endif // FEATURE_DEFAULT_INTERFACES

    RETURN(FALSE);
}
#endif // DACCESS_COMPILE

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlot(UINT32 typeID, UINT32 slotNumber, BOOL throwOnConflict)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DispatchSlot implSlot(NULL);
    FindDispatchImpl(typeID, slotNumber, &implSlot, throwOnConflict);
    return implSlot;
}

#ifndef DACCESS_COMPILE

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlotForInterfaceMD(MethodDesc *pMD, BOOL throwOnConflict)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(pMD));
    CONSISTENCY_CHECK(pMD->IsInterface());
    return FindDispatchSlotForInterfaceMD(TypeHandle(pMD->GetMethodTable()), pMD, throwOnConflict);
}

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlotForInterfaceMD(TypeHandle ownerType, MethodDesc *pMD, BOOL throwOnConflict)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(!ownerType.IsNull());
    CONSISTENCY_CHECK(CheckPointer(pMD));
    CONSISTENCY_CHECK(pMD->IsInterface());
    return FindDispatchSlot(ownerType.GetMethodTable()->GetTypeID(), pMD->GetSlot(), throwOnConflict);
}

//==========================================================================================
// This is used for reverse methodimpl lookups by ComPlusMethodCall MDs.
// This assumes the following:
//      The methodimpl is for an interfaceToken->slotNumber
//      There is ONLY ONE such mapping for this slot number
//      The mapping exists in this type, not a parent type.
MethodDesc * MethodTable::ReverseInterfaceMDLookup(UINT32 slotNumber)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;
    DispatchMap::Iterator it(this);
    for (; it.IsValid(); it.Next())
    {
        if (it.Entry()->GetTargetSlotNumber() == slotNumber)
        {
            DispatchMapTypeID typeID = it.Entry()->GetTypeID();
            _ASSERTE(!typeID.IsThisClass());
            UINT32 slotNum = it.Entry()->GetSlotNumber();
            MethodTable * pMTItf = LookupDispatchMapType(typeID);
            CONSISTENCY_CHECK(CheckPointer(pMTItf));

            MethodDesc *pCanonMD = pMTItf->GetMethodDescForSlot((DWORD)slotNum);
            return MethodDesc::FindOrCreateAssociatedMethodDesc(
                        pCanonMD,
                        pMTItf,
                        FALSE,              // forceBoxedEntryPoint
                        Instantiation(),    // methodInst
                        FALSE,              // allowInstParam
                        TRUE);              // forceRemotableMethod
        }
    }
    return NULL;
}

//==========================================================================================
UINT32 MethodTable::GetTypeID()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    PTR_MethodTable pMT = PTR_MethodTable(this);

    return GetDomain()->GetTypeID(pMT);
}

//==========================================================================================
UINT32 MethodTable::LookupTypeID()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PTR_MethodTable pMT = PTR_MethodTable(this);

    return GetDomain()->LookupTypeID(pMT);
}

//==========================================================================================
BOOL MethodTable::ImplementsInterfaceWithSameSlotsAsParent(MethodTable *pItfMT, MethodTable *pParentMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!IsInterface() && !pParentMT->IsInterface());
        PRECONDITION(pItfMT->IsInterface());
    } CONTRACTL_END;

    MethodTable *pMT = this;
    do
    {
        DispatchMap::EncodedMapIterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            DispatchMapEntry *pCurEntry = it.Entry();
            if (DispatchMapTypeMatchesMethodTable(pCurEntry->GetTypeID(), pItfMT))
            {
                // this class and its parents up to pParentMT must have no mappings for the interface
                return FALSE;
            }
        }

        pMT = pMT->GetParentMethodTable();
        _ASSERTE(pMT != NULL);
    }
    while (pMT != pParentMT);

    return TRUE;
}

#endif // !DACCESS_COMPILE

//==========================================================================================
#ifndef DACCESS_COMPILE
MethodTable * MethodTable::LookupDispatchMapType(DispatchMapTypeID typeID)
{
    CONTRACTL {
        WRAPPER(THROWS);
        GC_TRIGGERS;
    } CONTRACTL_END;

    _ASSERTE(!typeID.IsThisClass());

    InterfaceMapIterator intIt = IterateInterfaceMapFrom(typeID.GetInterfaceNum());
    return intIt.GetInterface(this);
}
#endif // DACCESS_COMPILE

//==========================================================================================
bool MethodTable::DispatchMapTypeMatchesMethodTable(DispatchMapTypeID typeID, MethodTable* pMT)
{
    CONTRACTL {
        WRAPPER(THROWS);
        GC_TRIGGERS;
    } CONTRACTL_END;

    _ASSERTE(!typeID.IsThisClass());
    InterfaceMapIterator intIt = IterateInterfaceMapFrom(typeID.GetInterfaceNum());
    return intIt.CurrentInterfaceMatches(this, pMT);
}

//==========================================================================================
MethodDesc * MethodTable::GetIntroducingMethodDesc(DWORD slotNumber)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc * pCurrentMD = GetMethodDescForSlot(slotNumber);
    DWORD        dwSlot = pCurrentMD->GetSlot();
    MethodDesc * pIntroducingMD = NULL;

    MethodTable * pParentType = GetParentMethodTable();
    MethodTable * pPrevParentType = NULL;

    // Find this method in the parent.
    // If it does exist in the parent, it would be at the same vtable slot.
    while ((pParentType != NULL) &&
           (dwSlot < pParentType->GetNumVirtuals()))
    {
        pPrevParentType = pParentType;
        pParentType = pParentType->GetParentMethodTable();
    }

    if (pPrevParentType != NULL)
    {
        pIntroducingMD = pPrevParentType->GetMethodDescForSlot(dwSlot);
    }

    return pIntroducingMD;
}

//==========================================================================================
// There is a case where a method declared in a type can be explicitly
// overridden by a methodImpl on another method within the same type. In
// this case, we need to call the methodImpl target, and this will map
// things appropriately for us.
MethodDesc * MethodTable::MapMethodDeclToMethodImpl(MethodDesc * pMDDecl)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    MethodTable * pMT = pMDDecl->GetMethodTable();

    //
    // Fast negative case check
    //

    // If it's not virtual, then it could not have been methodImpl'd.
    if (!pMDDecl->IsVirtual() ||
        // Is it a non-virtual call to the instantiating stub
        (pMT->IsValueType() && !pMDDecl->IsUnboxingStub()))
    {
        return pMDDecl;
    }

    MethodDesc * pMDImpl = pMT->GetParallelMethodDesc(pMDDecl);

    // If the method is instantiated, then we need to resolve to the corresponding
    // instantiated MD for the new slot number.
    if (pMDDecl->HasMethodInstantiation())
    {
        if (pMDDecl->GetSlot() != pMDImpl->GetSlot())
        {
            if (!pMDDecl->IsGenericMethodDefinition())
            {
#ifndef DACCESS_COMPILE
                pMDImpl = pMDDecl->FindOrCreateAssociatedMethodDesc(
                                        pMDImpl,
                                        pMT,
                                        pMDDecl->IsUnboxingStub(),
                                        pMDDecl->GetMethodInstantiation(),
                                        pMDDecl->IsInstantiatingStub());
#else
                DacNotImpl();
#endif
            }
        }
        else
        {
            // Since the generic method definition is always in the actual
            // slot for the method table, and since the slot numbers for
            // the Decl and Impl MDs are the same, then the call to
            // FindOrCreateAssociatedMethodDesc would just result in the
            // same pMDDecl being returned. In this case, we can skip all
            // the work.
            pMDImpl = pMDDecl;
        }
    }

    CONSISTENCY_CHECK(CheckPointer(pMDImpl));
    CONSISTENCY_CHECK(!pMDImpl->IsGenericMethodDefinition());
    return pMDImpl;
} // MethodTable::MapMethodDeclToMethodImpl


//==========================================================================================
HRESULT MethodTable::GetGuidNoThrow(GUID *pGuid, BOOL bGenerateIfNotFound, BOOL bClassic /*= TRUE*/)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    EX_TRY
    {
        GetGuid(pGuid, bGenerateIfNotFound, bClassic);
    }
    EX_CATCH_HRESULT(hr);

    // ensure we return a failure hr when pGuid is not filled in
    if (SUCCEEDED(hr) && (*pGuid == GUID_NULL))
        hr = E_FAIL;

    return hr;
}

//==========================================================================================
// Returns the GUID of this MethodTable.
// If metadata does not specify GUID for the type, GUID_NULL is returned (if bGenerateIfNotFound
// is FALSE) or a GUID is auto-generated on the fly from the name and members of the type
// (bGenerateIfNotFound is TRUE).
void MethodTable::GetGuid(GUID *pGuid, BOOL bGenerateIfNotFound, BOOL bClassic /*=TRUE*/)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;


#ifdef DACCESS_COMPILE

    _ASSERTE(pGuid != NULL);
    PTR_GuidInfo pGuidInfo = GetClass()->GetGuidInfo();
    if (pGuidInfo != NULL)
       *pGuid = pGuidInfo->m_Guid;
    else
        *pGuid = GUID_NULL;

#else // DACCESS_COMPILE

    SIZE_T      cchName = 0;            // Length of the name (possibly after decoration).
    SIZE_T      cbCur;                  // Current offset.
    LPCWSTR     szName = NULL;          // Name to turn to a guid.
    CQuickArray<BYTE> rName;            // Buffer to accumulate signatures.
    BOOL        bGenerated = FALSE;     // A flag indicating if we generated the GUID from name.

    _ASSERTE(pGuid != NULL);
    _ASSERTE(!this->IsArray());

    GuidInfo *pInfo = GetClass()->GetGuidInfo();

    // First check to see if we have already cached the guid for this type.
    // We currently only cache guids on interfaces and WinRT delegates.
    // In classic mode, though, ensure we don't retrieve the GuidInfo for redirected interfaces
    if ((IsInterface()) && pInfo != NULL
        && (!bClassic))
    {
        if (pInfo->m_bGeneratedFromName)
        {
            // If the GUID was generated from the name then only return it
            // if bGenerateIfNotFound is set.
            if (bGenerateIfNotFound)
                *pGuid = pInfo->m_Guid;
            else
                *pGuid = GUID_NULL;
            }
        else
        {
            *pGuid = pInfo->m_Guid;
        }
        return;
    }

    if (GetClass()->HasNoGuid())
    {
        *pGuid = GUID_NULL;
    }
    else
    {
        // If there is a GUID in the metadata then return that.
        IfFailThrow(GetMDImport()->GetItemGuid(GetCl(), pGuid));

        if (*pGuid == GUID_NULL)
        {
            // Remember that we didn't find the GUID, so we can skip looking during
            // future checks. (Note that this is a very important optimization in the
            // prejit case.)
            GetClass()->SetHasNoGuid();
        }
    }

    if (*pGuid == GUID_NULL && bGenerateIfNotFound)
    {
        // For interfaces, concatenate the signatures of the methods and fields.
        if (!IsNilToken(GetCl()) && IsInterface())
        {
            // Retrieve the stringized interface definition.
            cbCur = GetStringizedItfDef(TypeHandle(this), rName);

            // Pad up to a whole WCHAR.
            if (cbCur % sizeof(WCHAR))
            {
                SIZE_T cbDelta = sizeof(WCHAR) - (cbCur % sizeof(WCHAR));
                rName.ReSizeThrows(cbCur + cbDelta);
                memset(rName.Ptr() + cbCur, 0, cbDelta);
                cbCur += cbDelta;
            }

            // Point to the new buffer.
            cchName = cbCur / sizeof(WCHAR);
            szName = reinterpret_cast<LPWSTR>(rName.Ptr());
        }
        else
        {
            // Get the name of the class.
            DefineFullyQualifiedNameForClassW();
            szName = GetFullyQualifiedNameForClassNestedAwareW(this);
            if (szName == NULL)
                return;
            cchName = u16_strlen(szName);

            // Enlarge buffer for class name.
            cbCur = cchName * sizeof(WCHAR);
            rName.ReSizeThrows(cbCur + sizeof(WCHAR));
            wcscpy_s(reinterpret_cast<LPWSTR>(rName.Ptr()), cchName + 1, szName);

            // Add the assembly guid string to the class name.
            ULONG cbCurOUT = (ULONG)cbCur;
            IfFailThrow(GetStringizedTypeLibGuidForAssembly(GetAssembly(), rName, (ULONG)cbCur, &cbCurOUT));
            cbCur = (SIZE_T) cbCurOUT;

            // Pad to a whole WCHAR.
            if (cbCur % sizeof(WCHAR))
            {
                rName.ReSizeThrows(cbCur + sizeof(WCHAR)-(cbCur%sizeof(WCHAR)));
                while (cbCur % sizeof(WCHAR))
                    rName[cbCur++] = 0;
            }

            // Point to the new buffer.
            szName = reinterpret_cast<LPWSTR>(rName.Ptr());
            cchName = cbCur / sizeof(WCHAR);
            // Dont' want to have to pad.
            _ASSERTE((sizeof(GUID) % sizeof(WCHAR)) == 0);
        }

        // Generate guid from name.
        CorGuidFromNameW(pGuid, szName, cchName);

        // Remeber we generated the guid from the type name.
        bGenerated = TRUE;
    }

    // Cache the guid in the type, if not already cached.
    // We currently only do this for interfaces.
    // Also, in classic mode do NOT cache GUID for redirected interfaces.
    if ((IsInterface()) && (pInfo == NULL) && (*pGuid != GUID_NULL))
    {
        AllocMemTracker amTracker;

        // We will always store the GuidInfo on the methodTable.
        // Since the GUIDInfo will be stored on the EEClass,
        // the memory should be allocated on the loaderAllocator of the class.
        // The definining module and the loaded module could be different in some scenarios.
        // For example - in case of shared generic instantiations
        // a shared generic i.e. System.__Canon which would be loaded in shared domain
        // but the this->GetLoaderAllocator will be the loader allocator for the definining
        // module which can get unloaded anytime.
        _ASSERTE(GetClass());
        _ASSERTE(GetClass()->GetMethodTable());
        PTR_LoaderAllocator pLoaderAllocator = GetClass()->GetMethodTable()->GetLoaderAllocator();

        _ASSERTE(pLoaderAllocator);

        // Allocate the guid information.
        pInfo = (GuidInfo *)amTracker.Track(
            pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(GuidInfo))));
        pInfo->m_Guid = *pGuid;
        pInfo->m_bGeneratedFromName = bGenerated;

        // Set the per-EEClass GuidInfo
        // The MethodTable may be NGENed and read-only - and there's no point in saving
        // classic GUIDs in non-WinRT MethodTables anyway.
        GetClass()->SetGuidInfo(pInfo);

        amTracker.SuppressRelease();
    }
#endif // !DACCESS_COMPILE
}


//==========================================================================================
MethodDesc* MethodTable::GetMethodDescForSlotAddress(PCODE addr, BOOL fSpeculative /*=FALSE*/)
{
    CONTRACT(MethodDesc *)
    {
        GC_NOTRIGGER;
        NOTHROW;
        POSTCONDITION(CheckPointer(RETVAL, NULL_NOT_OK));
        POSTCONDITION(RETVAL->m_pDebugMethodTable == NULL || // We must be in BuildMethdTableThrowing()
                      RETVAL->SanityCheck());
    }
    CONTRACT_END;

    // If we see shared fcall implementation as an argument to this
    // function, it means that a vtable slot for the shared fcall
    // got backpatched when it shouldn't have.  The reason we can't
    // backpatch this method is that it is an FCall that has many
    // MethodDescs for one implementation.  If we backpatch delegate
    // constructors, this function will not be able to recover the
    // MethodDesc for the method.
    //
    _ASSERTE_IMPL(!ECall::IsSharedFCallImpl(addr) &&
                  "someone backpatched shared fcall implementation -- "
                  "see comment in code");

    MethodDesc* pMethodDesc = ExecutionManager::GetCodeMethodDesc(addr);
    if (NULL != pMethodDesc)
    {
        goto lExit;
    }

#ifdef FEATURE_INTERPRETER
    // I don't really know why this helps.  Figure it out.
#ifndef DACCESS_COMPILE
    // If we didn't find it above, try as an Interpretation stub...
    pMethodDesc = Interpreter::InterpretationStubToMethodInfo(addr);

    if (NULL != pMethodDesc)
    {
        goto lExit;
    }
#endif
#endif // FEATURE_INTERPRETER

    // Is it an FCALL?
    pMethodDesc = ECall::MapTargetBackToMethod(addr);
    if (pMethodDesc != 0)
    {
        goto lExit;
    }

    pMethodDesc = MethodDesc::GetMethodDescFromStubAddr(addr, fSpeculative);

lExit:

    RETURN(pMethodDesc);
}

//==========================================================================================
/* static*/
BOOL MethodTable::ComputeContainsGenericVariables(Instantiation inst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (DWORD j = 0; j < inst.GetNumArgs(); j++)
    {
        if (inst[j].ContainsGenericVariables())
        {
            return TRUE;
        }
    }
    return FALSE;
}

//==========================================================================================
BOOL MethodTable::SanityCheck()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // strings have component size2, all other non-arrays should have 0
    _ASSERTE((GetComponentSize() <= 2) || IsArray());

    if (m_pEEClass == NULL)
    {
        return FALSE;
    }

    EEClass * pClass = GetClass();
    MethodTable * pCanonMT = pClass->GetMethodTable();

    // Let's try to make sure we have a valid EEClass pointer.
    if (pCanonMT == NULL)
        return FALSE;

    if (GetNumGenericArgs() != 0)
        return (pCanonMT->GetClass() == pClass);
    else
        return (pCanonMT == this) || IsArray();
}


//==========================================================================================
void MethodTable::SetCl(mdTypeDef token)
{
    LIMITED_METHOD_CONTRACT;

    unsigned rid = RidFromToken(token);
    m_dwFlags2 = (rid << 8) | (m_dwFlags2 & 0xFF);
    _ASSERTE(GetCl() == token);
}

//==========================================================================================
MethodDesc * MethodTable::GetClassConstructor()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    return GetMethodDescForSlot(GetClassConstructorSlot());
}

//==========================================================================================
DWORD MethodTable::HasFixedAddressVTStatics()
{
    LIMITED_METHOD_CONTRACT;

    return GetClass()->HasFixedAddressVTStatics();
}

//==========================================================================================
BOOL MethodTable::HasOnlyAbstractMethods()
{
    LIMITED_METHOD_CONTRACT;

    return GetClass()->HasOnlyAbstractMethods();
}

//==========================================================================================
WORD MethodTable::GetNumHandleRegularStatics()
{
    LIMITED_METHOD_CONTRACT;

    return GetClass()->GetNumHandleRegularStatics();
}

#ifdef _DEBUG
//==========================================================================================
// Returns true if pointer to the parent method table has been initialized/restored already.
BOOL MethodTable::IsParentMethodTablePointerValid()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // workaround: Type loader accesses partially initialized datastructures
    if (!GetAuxiliaryData()->IsParentMethodTablePointerValid())
        return FALSE;

    return TRUE;
}
#endif


//---------------------------------------------------------------------------------------
//
// Ascends the parent class chain of "this", until a MethodTable is found whose typeDef
// matches that of the specified pWhichParent. Why is this useful? See
// code:MethodTable::GetInstantiationOfParentClass below and
// code:Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation for use
// cases.
//
// Arguments:
//      pWhichParent - MethodTable whose typeDef we're trying to match as we go up
//      "this"'s parent chain.
//
// Return Value:
//      If a matching parent MethodTable is found, it is returned. Else, NULL is
//      returned.
//

MethodTable * MethodTable::GetMethodTableMatchingParentClass(MethodTable * pWhichParent)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pWhichParent));
        PRECONDITION(IsRestored());
        PRECONDITION(pWhichParent->IsRestored());
        SUPPORTS_DAC;
    } CONTRACTL_END;

    MethodTable *pMethodTableSearch = this;

#ifdef DACCESS_COMPILE
    unsigned parentCount = 0;
    MethodTable *pOldMethodTable = NULL;
#endif // DACCESS_COMPILE

    while (pMethodTableSearch != NULL)
    {
#ifdef DACCESS_COMPILE
        if (pMethodTableSearch == pOldMethodTable ||
            parentCount > 1000)
        {
            break;
        }
        pOldMethodTable = pMethodTableSearch;
        parentCount++;
#endif // DACCESS_COMPILE

        if (pMethodTableSearch->HasSameTypeDefAs(pWhichParent))
        {
            return pMethodTableSearch;
        }

        pMethodTableSearch = pMethodTableSearch->GetParentMethodTable();
    }

    return NULL;
}


//==========================================================================================
// Given D<T> : C<List<T>> and a type handle D<string> we sometimes
// need to find the corresponding type handle
// C<List<string>> (C may also be some type
// further up the inheritance hierarchy).  GetInstantiationOfParentClass
// helps us do this by getting the corresponding instantiation of C, i.e.
// <List<string>>.
//
// pWhichParent: this is used identify which parent type we're interested in.
// It must be a canonical EEClass, e.g. for C<ref>.  This is used as a token for
// C<List<T>>. This method can also be called with the minimal methodtable used
// for dynamic methods. In that case, we need to return an empty instantiation.
//
// Note this only works for parent classes, not parent interfaces.
Instantiation MethodTable::GetInstantiationOfParentClass(MethodTable *pWhichParent)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pWhichParent));
        PRECONDITION(IsRestored());
        PRECONDITION(pWhichParent->IsRestored());
        SUPPORTS_DAC;
    } CONTRACTL_END;


    MethodTable * pMatchingParent = GetMethodTableMatchingParentClass(pWhichParent);
    if (pMatchingParent != NULL)
    {
        return pMatchingParent->GetInstantiation();
    }

    // The parameter should always be a parent class or the dynamic method
    // class. Since there is no bit on the dynamicclass methodtable to indicate
    // that it is the dynamic method methodtable, we simply check the debug name
    // This is good enough for an assert.
    _ASSERTE(strcmp(pWhichParent->GetDebugClassName(), "dynamicClass") == 0);
    return Instantiation();
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP

//
// This is for COM Interop backwards compatibility
//

//==========================================================================================
// Returns the data pointer if present, NULL otherwise
InteropMethodTableData *MethodTable::LookupComInteropData()
{
    WRAPPER_NO_CONTRACT;

    return GetLoaderAllocator()->LookupComInteropData(this);
}

//==========================================================================================
// Returns TRUE if successfully inserted, FALSE if this would be a duplicate entry
BOOL MethodTable::InsertComInteropData(InteropMethodTableData *pData)
{
    WRAPPER_NO_CONTRACT;

    return GetLoaderAllocator()->InsertComInteropData(this, pData);
}

//==========================================================================================
InteropMethodTableData *MethodTable::CreateComInteropData(AllocMemTracker *pamTracker)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(GetParentMethodTable() == NULL || GetParentMethodTable()->LookupComInteropData() != NULL);
    } CONTRACTL_END;

    ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

    ClassCompat::MethodTableBuilder builder(this, pStackingAllocator);

    InteropMethodTableData *pData = builder.BuildInteropVTable(pamTracker);
    _ASSERTE(pData);
    return (pData);
}

//==========================================================================================
InteropMethodTableData *MethodTable::GetComInteropData()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    InteropMethodTableData *pData = LookupComInteropData();

    if (!pData)
    {
        GCX_PREEMP();

        // Make sure that the parent's interop data has been created
        MethodTable *pParentMT = GetParentMethodTable();
        if (pParentMT)
            pParentMT->GetComInteropData();

        AllocMemTracker amTracker;

        pData = CreateComInteropData(&amTracker);
        if (InsertComInteropData(pData))
        {
            amTracker.SuppressRelease();
        }
        else
        {
            pData = LookupComInteropData();
        }
    }

    _ASSERTE(pData);
    return (pData);
}

#endif // FEATURE_COMINTEROP

//==========================================================================================
ULONG MethodTable::MethodData::Release()
{
    LIMITED_METHOD_CONTRACT;
    //@TODO: Must adjust this to use an alternate allocator so that we don't
    //@TODO: potentially cause deadlocks on the debug thread.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    ULONG cRef = (ULONG) InterlockedDecrement((LONG*)&m_cRef);
    if (cRef == 0) {
        delete this;
    }
    return (cRef);
}

//==========================================================================================
void
MethodTable::MethodData::ProcessMap(
    const DispatchMapTypeID * rgTypeIDs,
    UINT32                    cTypeIDs,
    MethodTable *             pMT,
    UINT32                    iCurrentChainDepth,
    MethodDataEntry *         rgWorkingData,
    size_t                    cWorkingData)
{
    LIMITED_METHOD_CONTRACT;

    for (DispatchMap::EncodedMapIterator it(pMT); it.IsValid(); it.Next())
    {
        for (UINT32 nTypeIDIndex = 0; nTypeIDIndex < cTypeIDs; nTypeIDIndex++)
        {
            if (it.Entry()->GetTypeID() == rgTypeIDs[nTypeIDIndex])
            {
                UINT32 curSlot = it.Entry()->GetSlotNumber();
                // This if check is defensive, and should never fail
                if (curSlot < cWorkingData)
                {
                    MethodDataEntry * pCurEntry = &rgWorkingData[curSlot];
                    if (!pCurEntry->IsDeclInit() && !pCurEntry->IsImplInit())
                    {
                        pCurEntry->SetImplData(it.Entry()->GetTargetSlotNumber());
                    }
                }
            }
        }
    }
} // MethodTable::MethodData::ProcessMap

//==========================================================================================
UINT32 MethodTable::MethodDataObject::GetObjectSize(MethodTable *pMT, MethodDataComputeOptions computeOptions)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(computeOptions != MethodDataComputeOptions::CacheOnly);

    UINT32 cb = sizeof(MethodTable::MethodDataObject);
    cb += ComputeNumMethods(pMT, computeOptions) * sizeof(MethodDataObjectEntry);
    return cb;
}

//==========================================================================================
// This will fill in all the MethodEntry slots present in the current MethodTable
void MethodTable::MethodDataObject::Init(MethodData *pParentData)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(CheckPointer(m_pDeclMT));
        PRECONDITION(CheckPointer(pParentData, NULL_OK));
        PRECONDITION(!m_pDeclMT->IsInterface());
        PRECONDITION(pParentData == NULL ||
                     (m_pDeclMT->ParentEquals(pParentData->GetDeclMethodTable()) &&
                      m_pDeclMT->ParentEquals(pParentData->GetImplMethodTable())));
    } CONTRACTL_END;

    m_iNextChainDepth = 0;
    m_containsMethodImpl = FALSE;

    ZeroMemory(GetEntryData(), sizeof(MethodDataObjectEntry) * GetNumMethods());
} // MethodTable::MethodDataObject::Init

//==========================================================================================
BOOL MethodTable::MethodDataObject::PopulateNextLevel()
{
    LIMITED_METHOD_CONTRACT;

    // Get the chain depth to next decode.
    UINT32 iChainDepth = GetNextChainDepth();

    // If the chain depth is MAX_CHAIN_DEPTH, then we've already parsed every parent.
    if (iChainDepth == MAX_CHAIN_DEPTH) {
        return FALSE;
    }
    // Now move up the chain to the target.
    MethodTable *pMTCur = m_pDeclMT;
    for (UINT32 i = 0; pMTCur != NULL && i < iChainDepth; i++) {
        pMTCur = pMTCur->GetParentMethodTable();
    }

    // If we reached the end, then we're done.
    if (pMTCur == NULL) {
        SetNextChainDepth(MAX_CHAIN_DEPTH);
        return FALSE;
    }

    FillEntryDataForAncestor(pMTCur);

    SetNextChainDepth(iChainDepth + 1);

    return TRUE;
} // MethodTable::MethodDataObject::PopulateNextLevel

//==========================================================================================
void MethodTable::MethodDataObject::FillEntryDataForAncestor(MethodTable * pMT)
{
    LIMITED_METHOD_CONTRACT;

    // Since we traverse ancestors from lowest in the inheritance hierarchy
    // to highest, the first method we come across for a slot is normally
    // both the declaring and implementing method desc.
    //
    // However if this slot is the target of a methodImpl, pMD is not
    // necessarily either.  Rather than track this on a per-slot basis,
    // we conservatively avoid filling out virtual methods once we
    // have found that this inheritance chain contains a methodImpl.
    //
    // Note that there may be a methodImpl higher in the inheritance chain
    // that we have not seen yet, and so we will fill out virtual methods
    // until we reach that level.  We are safe doing that because the slots
    // we fill have been introduced/overridden by a subclass and so take
    // precedence over any inherited methodImpl.

    // Before we fill the entry data, find if the current ancestor has any methodImpls

    if (pMT->GetClass()->ContainsMethodImpls())
        m_containsMethodImpl = TRUE;

    if (m_containsMethodImpl && pMT != m_pDeclMT)
        return;

    unsigned nVirtuals = pMT->GetNumVirtuals();
    unsigned nVTableLikeSlots = pMT->GetCanonicalMethodTable()->GetNumVtableSlots();

    MethodTable::IntroducedMethodIterator it(pMT, FALSE);
    for (; it.IsValid(); it.Next())
    {
        MethodDesc * pMD = it.GetMethodDesc();

        unsigned slot = pMD->GetSlot();
        if (slot == MethodTable::NO_SLOT)
            continue;

        // We want to fill all methods introduced by the actual type we're gathering
        // data for, and the virtual methods of the parent and above
        if (pMT == m_pDeclMT)
        {
            if (m_containsMethodImpl && slot < nVirtuals)
                continue;

            if (m_virtualsOnly && slot >= nVTableLikeSlots)
            {
                _ASSERTE(!pMD->IsVirtual() || (pMT->IsValueType() && !pMD->IsUnboxingStub()));
                continue;
            }
        }
        else
        {
            if (slot >= nVirtuals)
                continue;
        }

        MethodDataObjectEntry * pEntry = GetEntry(slot);

        if (pEntry->GetDeclMethodDesc() == NULL)
        {
            pEntry->SetDeclMethodDesc(pMD);
        }

        if (pEntry->GetImplMethodDesc() == NULL)
        {
            pEntry->SetImplMethodDesc(pMD);
        }
    }
} // MethodTable::MethodDataObject::FillEntryDataForAncestor

//==========================================================================================
MethodDesc * MethodTable::MethodDataObject::GetDeclMethodDesc(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(slotNumber < GetNumMethods());

    MethodDataObjectEntry * pEntry = GetEntry(slotNumber);

    // Fill the entries one level of inheritance at a time,
    // stopping when we have filled the MD we are looking for.
    while (!pEntry->GetDeclMethodDesc() && PopulateNextLevel());

    MethodDesc * pMDRet = pEntry->GetDeclMethodDesc();
    if (pMDRet == NULL)
    {
        pMDRet = GetImplMethodDesc(slotNumber)->GetDeclMethodDesc(slotNumber);
        _ASSERTE(CheckPointer(pMDRet));
        pEntry->SetDeclMethodDesc(pMDRet);
    }
    else
    {
        _ASSERTE(pMDRet == GetImplMethodDesc(slotNumber)->GetDeclMethodDesc(slotNumber));
    }
    return pMDRet;
}

//==========================================================================================
DispatchSlot MethodTable::MethodDataObject::GetImplSlot(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(slotNumber < GetNumMethods());
    return DispatchSlot(m_pDeclMT->GetRestoredSlot(slotNumber));
}

//==========================================================================================
UINT32 MethodTable::MethodDataObject::GetImplSlotNumber(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(slotNumber < GetNumMethods());
    return slotNumber;
}

//==========================================================================================
MethodDesc *MethodTable::MethodDataObject::GetImplMethodDesc(UINT32 slotNumber)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(slotNumber < GetNumMethods());
    MethodDataObjectEntry *pEntry = GetEntry(slotNumber);

    // Fill the entries one level of inheritance at a time,
    // stopping when we have filled the MD we are looking for.
    while (!pEntry->GetImplMethodDesc() && PopulateNextLevel());

    MethodDesc *pMDRet = pEntry->GetImplMethodDesc();

    if (pMDRet == NULL)
    {
        _ASSERTE(slotNumber < GetNumVirtuals());
        pMDRet = m_pDeclMT->GetMethodDescForSlot(slotNumber);
        _ASSERTE(CheckPointer(pMDRet));
        pEntry->SetImplMethodDesc(pMDRet);
    }
    else
    {
        _ASSERTE(slotNumber >= GetNumVirtuals() || pMDRet == m_pDeclMT->GetMethodDescForSlot(slotNumber));
    }

    return pMDRet;
}

//==========================================================================================
void MethodTable::MethodDataObject::UpdateImplMethodDesc(MethodDesc* pMD, UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(slotNumber < GetNumVirtuals());
    _ASSERTE(pMD->IsMethodImpl());

    MethodDataObjectEntry* pEntry = GetEntry(slotNumber);

    // Fill the entries one level of inheritance at a time,
    // stopping when we have filled the MD we are looking for.
    while (!pEntry->GetImplMethodDesc() && PopulateNextLevel());

    pEntry->SetImplMethodDesc(pMD);
}

//==========================================================================================
void MethodTable::MethodDataObject::InvalidateCachedVirtualSlot(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(slotNumber < GetNumVirtuals());

    MethodDataObjectEntry *pEntry = GetEntry(slotNumber);
    pEntry->SetImplMethodDesc(NULL);
}

//==========================================================================================
MethodDesc *MethodTable::MethodDataInterface::GetDeclMethodDesc(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    return m_pDeclMT->GetMethodDescForSlot(slotNumber);
}

//==========================================================================================
MethodDesc *MethodTable::MethodDataInterface::GetImplMethodDesc(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    return MethodTable::MethodDataInterface::GetDeclMethodDesc(slotNumber);
}

//==========================================================================================
void MethodTable::MethodDataInterface::InvalidateCachedVirtualSlot(UINT32 slotNumber)
{
    LIMITED_METHOD_CONTRACT;

    // MethodDataInterface does not store any cached MethodDesc values
    return;
}

//==========================================================================================
UINT32 MethodTable::MethodDataInterfaceImpl::GetObjectSize(MethodTable *pMTDecl)
{
    WRAPPER_NO_CONTRACT;
    UINT32 cb = sizeof(MethodDataInterfaceImpl);
    cb += pMTDecl->GetNumVirtuals() * sizeof(MethodDataEntry);
    return cb;
}

//==========================================================================================
// This will fill in all the MethodEntry slots present in the current MethodTable
void
MethodTable::MethodDataInterfaceImpl::Init(
    const DispatchMapTypeID * rgDeclTypeIDs,
    UINT32                    cDeclTypeIDs,
    MethodData *              pDecl,
    MethodData *              pImpl)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(CheckPointer(pDecl));
        PRECONDITION(CheckPointer(pImpl));
        PRECONDITION(pDecl->GetDeclMethodTable()->IsInterface());
        PRECONDITION(!pImpl->GetDeclMethodTable()->IsInterface());
        PRECONDITION(pDecl->GetDeclMethodTable() == pDecl->GetImplMethodTable());
        PRECONDITION(pImpl->GetDeclMethodTable() == pImpl->GetImplMethodTable());
        PRECONDITION(pDecl != pImpl);
    } CONTRACTL_END;

    // Store and AddRef the decl and impl data.
    m_pDecl = pDecl;
    m_pDecl->AddRef();
    m_pImpl = pImpl;
    m_pImpl->AddRef();

    m_iNextChainDepth = 0;
    // Need side effects of the calls, but not the result.
    /* MethodTable *pDeclMT = */ pDecl->GetDeclMethodTable();
    /* MethodTable *pImplMT = */ pImpl->GetImplMethodTable();
    m_rgDeclTypeIDs = rgDeclTypeIDs;
    m_cDeclTypeIDs = cDeclTypeIDs;

    // Initialize each entry.
    for (UINT32 i = 0; i < GetNumMethods(); i++) {
        // Initialize the entry
        GetEntry(i)->Init();
    }
} // MethodTable::MethodDataInterfaceImpl::Init

//==========================================================================================
MethodTable::MethodDataInterfaceImpl::MethodDataInterfaceImpl(
    const DispatchMapTypeID * rgDeclTypeIDs,
    UINT32                    cDeclTypeIDs,
    MethodData *              pDecl,
    MethodData *              pImpl) :
    MethodData(pImpl->GetDeclMethodTable(), pDecl->GetDeclMethodTable())
{
    WRAPPER_NO_CONTRACT;
    Init(rgDeclTypeIDs, cDeclTypeIDs, pDecl, pImpl);
}

//==========================================================================================
MethodTable::MethodDataInterfaceImpl::~MethodDataInterfaceImpl()
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(m_pDecl));
    CONSISTENCY_CHECK(CheckPointer(m_pImpl));
    m_pDecl->Release();
    m_pImpl->Release();
}

//==========================================================================================
BOOL
MethodTable::MethodDataInterfaceImpl::PopulateNextLevel()
{
    LIMITED_METHOD_CONTRACT;

    // Get the chain depth to next decode.
    UINT32 iChainDepth = GetNextChainDepth();

    // If the chain depth is MAX_CHAIN_DEPTH, then we've already parsed every parent.
    if (iChainDepth == MAX_CHAIN_DEPTH) {
        return FALSE;
    }

    // Now move up the chain to the target.
    MethodTable *pMTCur = m_pImpl->GetImplMethodTable();
    for (UINT32 i = 0; pMTCur != NULL && i < iChainDepth; i++) {
        pMTCur = pMTCur->GetParentMethodTable();
    }

    // If we reached the end, then we're done.
    if (pMTCur == NULL) {
        SetNextChainDepth(MAX_CHAIN_DEPTH);
        return FALSE;
    }

    if (m_cDeclTypeIDs != 0)
    {   // We got the TypeIDs from TypeLoader, use them
        ProcessMap(m_rgDeclTypeIDs, m_cDeclTypeIDs, pMTCur, iChainDepth, GetEntryData(), GetNumVirtuals());
    }
    else
    {   // We should decode all interface duplicates of code:m_pDecl
        MethodTable * pDeclMT = m_pDecl->GetImplMethodTable();
        INDEBUG(BOOL dbg_fInterfaceFound = FALSE);

        // Call code:ProcessMap for every (duplicate) occurrence of interface code:pDeclMT in the interface
        // map of code:m_pImpl
        MethodTable::InterfaceMapIterator it = m_pImpl->GetImplMethodTable()->IterateInterfaceMap();
        while (it.Next())
        {
            if (it.CurrentInterfaceMatches(m_pImpl->GetImplMethodTable(), pDeclMT))
            {   // We found the interface
                INDEBUG(dbg_fInterfaceFound = TRUE);
                DispatchMapTypeID declTypeID = DispatchMapTypeID::InterfaceClassID(it.GetIndex());

                ProcessMap(&declTypeID, 1, pMTCur, iChainDepth, GetEntryData(), GetNumVirtuals());
            }
        }
        // The interface code:m_Decl should be found at least once in the interface map of code:m_pImpl,
        // otherwise someone passed wrong information
        _ASSERTE(dbg_fInterfaceFound);
    }

    SetNextChainDepth(iChainDepth + 1);

    return TRUE;
} // MethodTable::MethodDataInterfaceImpl::PopulateNextLevel

//==========================================================================================
UINT32 MethodTable::MethodDataInterfaceImpl::MapToImplSlotNumber(UINT32 slotNumber)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(slotNumber < GetNumMethods());

    MethodDataEntry *pEntry = GetEntry(slotNumber);
    while (!pEntry->IsImplInit() && PopulateNextLevel()) {}
    if (pEntry->IsImplInit()) {
        return pEntry->GetImplSlotNum();
    }
    else {
        return INVALID_SLOT_NUMBER;
    }
}

//==========================================================================================
DispatchSlot MethodTable::MethodDataInterfaceImpl::GetImplSlot(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    UINT32 implSlotNumber = MapToImplSlotNumber(slotNumber);
    if (implSlotNumber == INVALID_SLOT_NUMBER) {
        return DispatchSlot(NULL);
    }
    return m_pImpl->GetImplSlot(implSlotNumber);
}

//==========================================================================================
UINT32 MethodTable::MethodDataInterfaceImpl::GetImplSlotNumber(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    return MapToImplSlotNumber(slotNumber);
}

//==========================================================================================
MethodDesc *MethodTable::MethodDataInterfaceImpl::GetImplMethodDesc(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    UINT32 implSlotNumber = MapToImplSlotNumber(slotNumber);
    if (implSlotNumber == INVALID_SLOT_NUMBER) {
        return NULL;
    }
    return m_pImpl->GetImplMethodDesc(implSlotNumber);
}

//==========================================================================================
void MethodTable::MethodDataInterfaceImpl::InvalidateCachedVirtualSlot(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    UINT32 implSlotNumber = MapToImplSlotNumber(slotNumber);
    if (implSlotNumber == INVALID_SLOT_NUMBER) {
        return;
    }
    return m_pImpl->InvalidateCachedVirtualSlot(implSlotNumber);
}

//==========================================================================================
void MethodTable::InitMethodDataCache()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;
    _ASSERTE(s_pMethodDataCache == NULL);

    UINT32 cb = MethodDataCache::GetObjectSize(8);
    NewArrayHolder<BYTE> hb(new BYTE[cb]);
    s_pMethodDataCache = new (hb.GetValue()) MethodDataCache(8);
    hb.SuppressRelease();
}

//==========================================================================================
void MethodTable::ClearMethodDataCache()
{
    LIMITED_METHOD_CONTRACT;
    if (s_pMethodDataCache != NULL) {
        s_pMethodDataCache->Clear();
    }
}

//==========================================================================================
MethodTable::MethodData *MethodTable::FindMethodDataHelper(MethodTable *pMTDecl, MethodTable *pMTImpl)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    return s_pMethodDataCache->Find(pMTDecl, pMTImpl);
}

//==========================================================================================
MethodTable::MethodData *MethodTable::FindParentMethodDataHelper(MethodTable *pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    MethodData *pData = NULL;
    if (!pMT->IsInterface()) {
        //@todo : this won't be correct for non-shared code
        MethodTable *pMTParent = pMT->GetParentMethodTable();
        if (pMTParent != NULL) {
            pData = FindMethodDataHelper(pMTParent, pMTParent);
        }
    }
    return pData;
}

//==========================================================================================
// This method does not cache the resulting MethodData object in the global MethodDataCache.
// The TypeIDs (rgDeclTypeIDs with cDeclTypeIDs items) have to be sorted.
MethodTable::MethodData *
MethodTable::GetMethodDataHelper(
    const DispatchMapTypeID * rgDeclTypeIDs,
    UINT32                    cDeclTypeIDs,
    MethodTable *             pMTDecl,
    MethodTable *             pMTImpl,
    MethodDataComputeOptions  computeOptions)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(CheckPointer(pMTDecl));
        PRECONDITION(CheckPointer(pMTImpl));
    } CONTRACTL_END;

    //@TODO: Must adjust this to use an alternate allocator so that we don't
    //@TODO: potentially cause deadlocks on the debug thread.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

    CONSISTENCY_CHECK(pMTDecl->IsInterface() && !pMTImpl->IsInterface());

#ifdef _DEBUG
    // Check that rgDeclTypeIDs are sorted, are valid interface indexes and reference only pMTDecl interface
    {
        InterfaceInfo_t * rgImplInterfaceMap = pMTImpl->GetInterfaceMap();
        UINT32            cImplInterfaceMap = pMTImpl->GetNumInterfaces();
        // Verify that all types referenced by code:rgDeclTypeIDs are code:pMTDecl (declared interface)
        for (UINT32 nDeclTypeIDIndex = 0; nDeclTypeIDIndex < cDeclTypeIDs; nDeclTypeIDIndex++)
        {
            if (nDeclTypeIDIndex > 0)
            {   // Verify that interface indexes are sorted
                _ASSERTE(rgDeclTypeIDs[nDeclTypeIDIndex - 1].GetInterfaceNum() < rgDeclTypeIDs[nDeclTypeIDIndex].GetInterfaceNum());
            }
            UINT32 nInterfaceIndex = rgDeclTypeIDs[nDeclTypeIDIndex].GetInterfaceNum();
            _ASSERTE(nInterfaceIndex <= cImplInterfaceMap);
            {
                OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOAD_APPROXPARENTS);
                _ASSERTE(rgImplInterfaceMap[nInterfaceIndex].GetApproxMethodTable(pMTImpl->GetLoaderModule())->HasSameTypeDefAs(pMTDecl));
            }
        }
    }
#endif //_DEBUG

    // Can't cache, since this is a custom method used in BuildMethodTable
    MethodDataWrapper hDecl(GetMethodData(pMTDecl, computeOptions));
    MethodDataWrapper hImpl(GetMethodData(pMTImpl, computeOptions));

    MethodDataInterfaceImpl * pData = new ({ pMTDecl}) MethodDataInterfaceImpl(rgDeclTypeIDs, cDeclTypeIDs, hDecl, hImpl);

    return pData;
} // MethodTable::GetMethodDataHelper

//==========================================================================================
// The computeOptions argument determines if the resulting MethodData object can
// be added to the global MethodDataCache. This is used when requesting a
// MethodData object for a type currently being built.
MethodTable::MethodData *MethodTable::GetMethodDataHelper(MethodTable *pMTDecl,
                                                          MethodTable *pMTImpl,
                                                          MethodDataComputeOptions computeOptions)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(CheckPointer(pMTDecl));
        PRECONDITION(CheckPointer(pMTImpl));
        PRECONDITION(pMTDecl == pMTImpl ||
                     (pMTDecl->IsInterface() && !pMTImpl->IsInterface()));
    } CONTRACTL_END;

    //@TODO: Must adjust this to use an alternate allocator so that we don't
    //@TODO: potentially cause deadlocks on the debug thread.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

    MethodData *pData = FindMethodDataHelper(pMTDecl, pMTImpl);
    if (pData != NULL) {
        return pData;
    }

    if (computeOptions == MethodDataComputeOptions::CacheOnly) {
        return NULL;
    }

    // If we get here, there are no entries in the cache.
    if (pMTDecl == pMTImpl) {
        if (pMTDecl->IsInterface()) {
            pData = new MethodDataInterface(pMTDecl);
        }
        else {
            MethodDataHolder h(FindParentMethodDataHelper(pMTDecl));
            pData = new ({pMTDecl}, computeOptions) MethodDataObject(pMTDecl, h.GetValue(), computeOptions);
        }
    }
    else {
        pData = GetMethodDataHelper(
            NULL,
            0,
            pMTDecl,
            pMTImpl,
            computeOptions);
    }

    // Insert in the cache if it is active.
    if (computeOptions == MethodDataComputeOptions::Cache) {
        s_pMethodDataCache->Insert(pData);
    }

    // Do not AddRef, already initialized to 1.
    return pData;
}

//==========================================================================================
// The computeOptions argument determines if the resulting MethodData object can
// be added to the global MethodDataCache. This is used when requesting a
// MethodData object for a type currently being built.
MethodTable::MethodData *MethodTable::GetMethodData(MethodTable *pMTDecl,
                                                    MethodTable *pMTImpl,
                                                    MethodDataComputeOptions computeOptions)
{
    WRAPPER_NO_CONTRACT;
    return GetMethodDataHelper(pMTDecl, pMTImpl, computeOptions);
}

//==========================================================================================
// This method does not cache the resulting MethodData object in the global MethodDataCache.
MethodTable::MethodData *
MethodTable::GetMethodData(
    const DispatchMapTypeID * rgDeclTypeIDs,
    UINT32                    cDeclTypeIDs,
    MethodTable *             pMTDecl,
    MethodTable *             pMTImpl,
    MethodDataComputeOptions  computeOptions)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(pMTDecl != pMTImpl);
        PRECONDITION(pMTDecl->IsInterface());
        PRECONDITION(!pMTImpl->IsInterface());
    } CONTRACTL_END;

    return GetMethodDataHelper(rgDeclTypeIDs, cDeclTypeIDs, pMTDecl, pMTImpl, computeOptions);
}

//==========================================================================================
// The computeOptions argument determines if the resulting MethodData object can
// be added to the global MethodDataCache. This is used when requesting a
// MethodData object for a type currently being built.
MethodTable::MethodData *MethodTable::GetMethodData(MethodTable *pMT,
                                                    MethodDataComputeOptions computeOptions)
{
    WRAPPER_NO_CONTRACT;
    return GetMethodData(pMT, pMT, computeOptions);
}

//==========================================================================================
MethodTable::MethodIterator::MethodIterator(MethodTable *pMTDecl, MethodTable *pMTImpl)
{
    WRAPPER_NO_CONTRACT;
    Init(pMTDecl, pMTImpl);
}

//==========================================================================================
MethodTable::MethodIterator::MethodIterator(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    Init(pMT, pMT);
}

//==========================================================================================
MethodTable::MethodIterator::MethodIterator(MethodData *pMethodData)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMethodData));
    } CONTRACTL_END;

    m_pMethodData = pMethodData;
    m_pMethodData->AddRef();
    m_iCur = 0;
    m_iMethods = (INT32)m_pMethodData->GetNumMethods();
}

//==========================================================================================
MethodTable::MethodIterator::MethodIterator(const MethodIterator &it)
{
    WRAPPER_NO_CONTRACT;
    m_pMethodData = it.m_pMethodData;
    m_pMethodData->AddRef();
    m_iCur = it.m_iCur;
    m_iMethods = it.m_iMethods;
}

//==========================================================================================
void MethodTable::MethodIterator::Init(MethodTable *pMTDecl, MethodTable *pMTImpl)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMTDecl));
        PRECONDITION(CheckPointer(pMTImpl));
    } CONTRACTL_END;

    LOG((LF_LOADER, LL_INFO10000, "MT::MethodIterator created for %s.\n", pMTDecl->GetDebugClassName()));

    m_pMethodData = MethodTable::GetMethodData(pMTDecl, pMTImpl, MethodDataComputeOptions::Cache);
    CONSISTENCY_CHECK(CheckPointer(m_pMethodData));
    m_iCur = 0;
    m_iMethods = (INT32)m_pMethodData->GetNumMethods();
}
#endif // !DACCESS_COMPILE

//==========================================================================================

void MethodTable::IntroducedMethodIterator::SetChunk(MethodDescChunk * pChunk)
{
    LIMITED_METHOD_CONTRACT;

    if (pChunk)
    {
        m_pMethodDesc = pChunk->GetFirstMethodDesc();

        m_pChunk = pChunk;
        m_pChunkEnd = dac_cast<TADDR>(pChunk) + pChunk->SizeOf();
    }
    else
    {
        m_pMethodDesc = NULL;
    }
}

//==========================================================================================

MethodDesc * MethodTable::IntroducedMethodIterator::GetFirst(MethodTable *pMT)
{
    LIMITED_METHOD_CONTRACT;
    MethodDescChunk * pChunk = pMT->GetClass()->GetChunks();
    return (pChunk != NULL) ? pChunk->GetFirstMethodDesc() : NULL;
}

//==========================================================================================
MethodDesc * MethodTable::IntroducedMethodIterator::GetNext(MethodDesc * pMD)
{
    WRAPPER_NO_CONTRACT;

    MethodDescChunk * pChunk = pMD->GetMethodDescChunk();

    // Check whether the next MethodDesc is still within the bounds of the current chunk
    TADDR pNext = dac_cast<TADDR>(pMD) + pMD->SizeOf();
    TADDR pEnd = dac_cast<TADDR>(pChunk) + pChunk->SizeOf();

    if (pNext < pEnd)
    {
        // Just skip to the next method in the same chunk
        pMD = PTR_MethodDesc(pNext);
    }
    else
    {
        _ASSERTE(pNext == pEnd);

        // We have walked all the methods in the current chunk. Move on
        // to the next chunk.
        pChunk = pChunk->GetNextChunk();

        pMD = (pChunk != NULL) ? pChunk->GetFirstMethodDesc() : NULL;
    }

    return pMD;
}

//==========================================================================================
CHECK MethodTable::CheckActivated()
{
    WRAPPER_NO_CONTRACT;

    if (!IsArray())
    {
        CHECK(GetModule()->CheckActivated());
    }

    // <TODO> Check all generic type parameters as well </TODO>

    CHECK_OK;
}

#ifdef _MSC_VER
// Optimization intended for EnsureInstanceActive, EnsureActive only
#pragma optimize("t", on)
#endif // _MSC_VER
//==========================================================================================

#ifndef DACCESS_COMPILE
VOID MethodTable::EnsureInstanceActive()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Module * pModule = GetModule();
    pModule->EnsureActive();

    MethodTable * pMT = this;
    while (pMT->HasModuleDependencies())
    {
        pMT = pMT->GetParentMethodTable();
        _ASSERTE(pMT != NULL);

        Module * pParentModule = pMT->GetModule();
        if (pParentModule != pModule)
        {
            pModule = pParentModule;
            pModule->EnsureActive();
        }
    }

    if (HasInstantiation())
    {
        {
            Instantiation inst = GetInstantiation();
            for (DWORD i = 0; i < inst.GetNumArgs(); i++)
            {
                TypeHandle thArg = inst[i];
                if (!thArg.IsTypeDesc())
                {
                    thArg.AsMethodTable()->EnsureInstanceActive();
                }
            }
        }
    }

}
#endif //!DACCESS_COMPILE

//==========================================================================================
#ifndef DACCESS_COMPILE
VOID MethodTable::EnsureActive()
{
    WRAPPER_NO_CONTRACT;

    GetModule()->EnsureActive();
}
#endif

#ifdef _MSC_VER
#pragma optimize("", on)
#endif // _MSC_VER

//==========================================================================================
CHECK MethodTable::CheckInstanceActivated()
{
    WRAPPER_NO_CONTRACT;

    if (IsArray())
        CHECK_OK;

    Module * pModule = GetModule();
    CHECK(pModule->CheckActivated());

    MethodTable * pMT = this;
    while (pMT->HasModuleDependencies())
    {
        pMT = pMT->GetParentMethodTable();
        _ASSERTE(pMT != NULL);

        Module * pParentModule = pMT->GetModule();
        if (pParentModule != pModule)
        {
            pModule = pParentModule;
            CHECK(pModule->CheckActivated());
        }
    }

    CHECK_OK;
}

#ifdef DACCESS_COMPILE

//==========================================================================================
void
MethodTable::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    DAC_CHECK_ENUM_THIS();
    EMEM_OUT(("MEM: %p MethodTable\n", dac_cast<TADDR>(this)));

    DWORD size = GetEndOffsetOfOptionalMembers();
    DacEnumMemoryRegion(dac_cast<TADDR>(this), size);

    // Make sure the GCDescs are added to the dump
    if (ContainsPointers())
    {
        PTR_CGCDesc gcdesc = CGCDesc::GetCGCDescFromMT(this);
        size_t size = gcdesc->GetSize();
        // Manually calculate the start of the GCDescs because CGCDesc::GetStartOfGCData() isn't DAC'ified.
        TADDR gcdescStart = dac_cast<TADDR>(this) - size;
        DacEnumMemoryRegion(gcdescStart, size);
    }

    if (!IsCanonicalMethodTable())
    {
        PTR_MethodTable pMTCanonical = GetCanonicalMethodTable();

        if (pMTCanonical.IsValid())
        {
            pMTCanonical->EnumMemoryRegions(flags);
        }
        else
        {
            DacLogMessage("MT %p invalid canonical MT %p\n", dac_cast<TADDR>(this), dac_cast<TADDR>(pMTCanonical));
        }
    }
    else
    {
        PTR_EEClass pClass = GetClass();

        if (pClass.IsValid())
        {
            if (IsArray())
            {
                // This is kind of a workaround, in that ArrayClass is derived from EEClass, but
                // it's not virtual, we only cast if the IsArray() predicate holds above.
                // For minidumps, DAC will choke if we don't have the full size given
                // by ArrayClass available. If ArrayClass becomes more complex, it
                // should get it's own EnumMemoryRegions().
                DacEnumMemoryRegion(dac_cast<TADDR>(pClass), sizeof(ArrayClass));
            }
            pClass->EnumMemoryRegions(flags, this);
        }
        else
        {
            DacLogMessage("MT %p invalid class %p\n", dac_cast<TADDR>(this), dac_cast<TADDR>(pClass));
        }
    }

    PTR_MethodTable pMTParent = GetParentMethodTable();

    if (pMTParent.IsValid())
    {
        pMTParent->EnumMemoryRegions(flags);
    }

    if (HasNonVirtualSlots())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(MethodTableAuxiliaryData::GetNonVirtualSlotsArray(GetAuxiliaryData())) - GetNonVirtualSlotsArraySize(), GetNonVirtualSlotsArraySize());
    }

    if (HasGenericsStaticsInfo())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(GetGenericsStaticsInfo()), sizeof(GenericsStaticsInfo));
    }

    if (HasInterfaceMap())
    {
#ifdef FEATURE_COMINTEROP
        if (HasDynamicInterfaceMap())
            DacEnumMemoryRegion(dac_cast<TADDR>(GetInterfaceMap()) - sizeof(DWORD_PTR), GetInterfaceMapSize());
        else
#endif // FEATURE_COMINTEROP
            DacEnumMemoryRegion(dac_cast<TADDR>(GetInterfaceMap()), GetInterfaceMapSize());

        EnumMemoryRegionsForExtraInterfaceInfo();
    }

    if (HasPerInstInfo() != NULL)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(GetPerInstInfo()) - sizeof(GenericsDictInfo), GetPerInstInfoSize() + sizeof(GenericsDictInfo));
    }

    if (GetDictionary() != NULL)
    {
        DWORD slotSize;
        DWORD allocSize = GetInstAndDictSize(&slotSize);
        DacEnumMemoryRegion(dac_cast<TADDR>(GetDictionary()), slotSize);
    }

    VtableIndirectionSlotIterator it = IterateVtableIndirectionSlots();
    while (it.Next())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(it.GetIndirectionSlot()), it.GetSize());
    }

    PTR_MethodTableAuxiliaryData pAuxiliaryData = m_pAuxiliaryData;
    if (pAuxiliaryData.IsValid())
    {
        pAuxiliaryData.EnumMem();
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE && flags != CLRDATA_ENUM_MEM_HEAP2)
    {
        DispatchMap * pMap = GetDispatchMap();
        if (pMap != NULL)
        {
            pMap->EnumMemoryRegions(flags);
        }
    }
} // MethodTable::EnumMemoryRegions

#endif // DACCESS_COMPILE

//==========================================================================================
BOOL MethodTable::ContainsGenericMethodVariables()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    Instantiation inst = GetInstantiation();
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        if (inst[i].ContainsGenericVariables(TRUE))
            return TRUE;
    }

    return FALSE;
}

//==========================================================================================
Module *MethodTable::GetDefiningModuleForOpenType()
{
    CONTRACT(Module*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        POSTCONDITION((ContainsGenericVariables() != 0) == (RETVAL != NULL));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    if (ContainsGenericVariables())
    {
        Instantiation inst = GetInstantiation();
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            Module *pModule = inst[i].GetDefiningModuleForOpenType();
            if (pModule != NULL)
                RETURN pModule;
        }
    }

    RETURN NULL;
}

//==========================================================================================
PCODE MethodTable::GetRestoredSlot(DWORD slotNumber)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Keep in sync with code:MethodTable::GetRestoredSlotMT
    //

    PCODE slot = GetCanonicalMethodTable()->GetSlot(slotNumber);
    _ASSERTE(slot != NULL);
    return slot;
}

//==========================================================================================
MethodTable * MethodTable::GetRestoredSlotMT(DWORD slotNumber)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Keep in sync with code:MethodTable::GetRestoredSlot
    //

    return GetCanonicalMethodTable();
}

//==========================================================================================
namespace
{
    // Methods added by EnC cannot be looked up by slot since
    // they have none, see EEClass::AddMethodDesc(). We must perform
    // a slow lookup instead of using the fast slot lookup path.
    MethodDesc* GetParallelMethodDescForEnC(MethodTable* pMT, MethodDesc* pDefMD)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(pMT != NULL);
            PRECONDITION(pMT->IsCanonicalMethodTable());
            PRECONDITION(pDefMD != NULL);
            PRECONDITION(pDefMD->IsEnCAddedMethod());
            PRECONDITION(pDefMD->GetSlot() == MethodTable::NO_SLOT);
        }
        CONTRACTL_END;

        mdMethodDef tkMethod = pDefMD->GetMemberDef();
        Module* mod = pDefMD->GetModule();
        LOG((LF_ENC, LL_INFO100, "GPMDENC: pMT:%p tok:0x%08x mod:%p\n", pMT, tkMethod, mod));

        MethodTable::IntroducedMethodIterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            MethodDesc* pMD = it.GetMethodDesc();
            if (pMD->GetMemberDef() == tkMethod
                && pMD->GetModule() == mod)
            {
                return pMD;
            }
        }
        LOG((LF_ENC, LL_INFO10000, "GPMDENC: Not found\n"));
        return NULL;
    }
}

MethodDesc* MethodTable::GetParallelMethodDesc(MethodDesc* pDefMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_METADATA_UPDATER
    if (pDefMD->IsEnCAddedMethod())
        return GetParallelMethodDescForEnC(this, pDefMD);
#endif // FEATURE_METADATA_UPDATER

    return GetMethodDescForSlot(pDefMD->GetSlot());
}

#ifndef DACCESS_COMPILE

//==========================================================================================
void MethodTable::SetSlot(UINT32 slotNumber, PCODE slotCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

#ifdef _DEBUG
    if (slotNumber < GetNumVirtuals())
    {
        //
        // Verify that slots in shared vtable chunks not owned by this methodtable are only ever patched to stable entrypoint.
        // This invariant is required to prevent races with code:MethodDesc::SetStableEntryPointInterlocked.
        //
        BOOL fSharedVtableChunk = FALSE;
        DWORD indirectionIndex = MethodTable::GetIndexOfVtableIndirection(slotNumber);

        if (!IsCanonicalMethodTable())
        {
            if (GetVtableIndirections()[indirectionIndex] == GetCanonicalMethodTable()->GetVtableIndirections()[indirectionIndex])
                fSharedVtableChunk = TRUE;
        }

        if (slotNumber < GetNumParentVirtuals())
        {
            if (GetVtableIndirections()[indirectionIndex] == GetParentMethodTable()->GetVtableIndirections()[indirectionIndex])
                fSharedVtableChunk = TRUE;
        }

        if (fSharedVtableChunk)
        {
            MethodDesc* pMD = GetMethodDescForSlotAddress(slotCode);
            _ASSERTE(pMD->IsVersionableWithVtableSlotBackpatch() || pMD->GetStableEntryPoint() == slotCode);
        }
    }
#endif

#ifdef TARGET_ARM
    // Ensure on ARM that all target addresses are marked as thumb code.
    _ASSERTE(IsThumbCode(slotCode));
#endif

    *GetSlotPtrRaw(slotNumber) = slotCode;
}

//==========================================================================================
BOOL MethodTable::HasExplicitOrImplicitPublicDefaultConstructor()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    if (IsValueType())
    {
        // valuetypes have public default ctors implicitly
        return TRUE;
    }

    if (!HasDefaultConstructor())
    {
        return FALSE;
    }

    MethodDesc * pCanonMD = GetMethodDescForSlot(GetDefaultConstructorSlot());
    return pCanonMD != NULL && pCanonMD->IsPublic();
}

//==========================================================================================
MethodDesc *MethodTable::GetDefaultConstructor(BOOL forceBoxedEntryPoint /* = FALSE */)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(HasDefaultConstructor());
    MethodDesc *pCanonMD = GetMethodDescForSlot(GetDefaultConstructorSlot());
    // The default constructor for a value type is an instantiating stub.
    // The easiest way to find the right stub is to use the following function,
    // which in the simple case of the default constructor for a class simply
    // returns pCanonMD immediately.
    return MethodDesc::FindOrCreateAssociatedMethodDesc(pCanonMD,
                                                        this,
                                                        forceBoxedEntryPoint,
                                                        Instantiation(), /* no method instantiation */
                                                        FALSE /* no allowInstParam */);
}

//==========================================================================================
// Finds the (non-unboxing) MethodDesc that implements the interface virtual static method pInterfaceMD.
MethodDesc *
MethodTable::ResolveVirtualStaticMethod(
    MethodTable* pInterfaceType,
    MethodDesc* pInterfaceMD,
    ResolveVirtualStaticMethodFlags resolveVirtualStaticMethodFlags,
    BOOL* uniqueResolution,
    ClassLoadLevel level)
{
    bool verifyImplemented = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::VerifyImplemented) != ResolveVirtualStaticMethodFlags::None;
    bool allowVariantMatches = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::AllowVariantMatches) != ResolveVirtualStaticMethodFlags::None;
    bool instantiateMethodParameters = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::InstantiateResultOverFinalMethodDesc) != ResolveVirtualStaticMethodFlags::None;
    bool allowNullResult = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::AllowNullResult) != ResolveVirtualStaticMethodFlags::None;

    if (uniqueResolution != nullptr)
    {
        *uniqueResolution = TRUE;
    }
    if (!pInterfaceMD->IsSharedByGenericMethodInstantiations() && !pInterfaceType->IsSharedByGenericInstantiations())
    {
        // Check that there is no implementation of the interface on this type which is the canonical interface for a shared generic. If so, that indicates that
        // we cannot exactly compute a target method result, as even if there is an exact match in the type hierarchy
        // it isn't guaranteed that we will always find the right result, as we may find a match on a base type when we should find the match
        // on a more derived type.

        MethodTable *pInterfaceTypeCanonical = pInterfaceType->GetCanonicalMethodTable();
        bool canonicalEquivalentFound = false;
        if (pInterfaceType != pInterfaceTypeCanonical)
        {
            InterfaceMapIterator it = IterateInterfaceMap();
            while (it.Next())
            {
                if (it.CurrentInterfaceMatches(this, pInterfaceTypeCanonical))
                {
                    canonicalEquivalentFound = true;
                    break;
                }
            }
        }

        if (!canonicalEquivalentFound)
        {
            // Search for match on a per-level in the type hierarchy
            for (MethodTable* pMT = this; pMT != nullptr; pMT = pMT->GetParentMethodTable())
            {
                MethodDesc* pMD = pMT->TryResolveVirtualStaticMethodOnThisType(pInterfaceType, pInterfaceMD, resolveVirtualStaticMethodFlags & ~ResolveVirtualStaticMethodFlags::AllowVariantMatches, level);
                if (pMD != nullptr)
                {
                    return pMD;
                }

                if (pInterfaceType->HasVariance() || pInterfaceType->HasTypeEquivalence())
                {
                    // Variant interface dispatch
                    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
                    while (it.Next())
                    {
                        if (it.CurrentInterfaceMatches(this, pInterfaceType))
                        {
                            // This is the variant interface check logic, skip the exact matches as they were handled above
                            continue;
                        }

                        if (!it.HasSameTypeDefAs(pInterfaceType))
                        {
                            // Variance matches require a typedef match
                            // Equivalence isn't sufficient, and is uninteresting as equivalent interfaces cannot have static virtuals.
                            continue;
                        }

                        BOOL equivalentOrVariantCompatible;

                        MethodTable *pItfInMap = it.GetInterface(this, CLASS_LOAD_EXACTPARENTS);

                        if (allowVariantMatches)
                        {
                            equivalentOrVariantCompatible = pItfInMap->CanCastToInterface(pInterfaceType, NULL);
                        }
                        else
                        {
                            // When performing override checking to ensure that a concrete type is valid, require the implementation
                            // actually implement the exact or equivalent interface.
                            equivalentOrVariantCompatible = pItfInMap->IsEquivalentTo(pInterfaceType);
                        }

                        if (equivalentOrVariantCompatible)
                        {
                            // Variant or equivalent matching interface found
                            // Attempt to resolve on variance matched interface
                            pMD = pMT->TryResolveVirtualStaticMethodOnThisType(pItfInMap, pInterfaceMD, resolveVirtualStaticMethodFlags & ~ResolveVirtualStaticMethodFlags::AllowVariantMatches, level);
                            if (pMD != nullptr)
                            {
                                return pMD;
                            }
                        }
                    }
                }
            }

            MethodDesc *pMDDefaultImpl = nullptr;
            BOOL allowVariantMatchInDefaultImplementationLookup = FALSE;
            do
            {
                FindDefaultInterfaceImplementationFlags findDefaultImplementationFlags = FindDefaultInterfaceImplementationFlags::None;
                if (allowVariantMatchInDefaultImplementationLookup)
                {
                    findDefaultImplementationFlags |= FindDefaultInterfaceImplementationFlags::AllowVariance;
                }
                if (uniqueResolution == nullptr)
                {
                    findDefaultImplementationFlags |= FindDefaultInterfaceImplementationFlags::ThrowOnConflict;
                }
                if (instantiateMethodParameters)
                {
                    findDefaultImplementationFlags |= FindDefaultInterfaceImplementationFlags::InstantiateFoundMethodDesc;
                }

                BOOL haveUniqueDefaultImplementation = FindDefaultInterfaceImplementation(
                    pInterfaceMD,
                    pInterfaceType,
                    &pMDDefaultImpl,
                    findDefaultImplementationFlags,
                    level);
                if (haveUniqueDefaultImplementation || (pMDDefaultImpl != nullptr && (verifyImplemented || uniqueResolution != nullptr)))
                {
                    // We tolerate conflicts upon verification of implemented SVMs so that they only blow up when actually called at execution time.
                    if (uniqueResolution != nullptr)
                    {
                        // Always report a unique resolution when reporting results of a variant match
                        if (allowVariantMatchInDefaultImplementationLookup)
                            *uniqueResolution = TRUE;
                        else
                            *uniqueResolution = haveUniqueDefaultImplementation;
                    }
                    return pMDDefaultImpl;
                }

                // We only loop at most twice here
                if (allowVariantMatchInDefaultImplementationLookup)
                {
                    break;
                }

                allowVariantMatchInDefaultImplementationLookup = allowVariantMatches;
            } while (allowVariantMatchInDefaultImplementationLookup);
        }

        // Default implementation logic, which only kicks in for default implementations when looking up on an exact interface target
        if (!pInterfaceMD->IsAbstract() && !(this == g_pCanonMethodTableClass) && !IsSharedByGenericInstantiations())
        {
            return pInterfaceMD->FindOrCreateAssociatedMethodDesc(pInterfaceMD, pInterfaceType, FALSE, pInterfaceMD->GetMethodInstantiation(), FALSE);
        }
    }

    if (allowNullResult)
        return NULL;
    else
        COMPlusThrow(kTypeLoadException, E_NOTIMPL);
}

//==========================================================================================
// Try to locate the appropriate MethodImpl matching a given interface static virtual method.
// Returns nullptr on failure.
MethodDesc*
MethodTable::TryResolveVirtualStaticMethodOnThisType(MethodTable* pInterfaceType, MethodDesc* pInterfaceMD, ResolveVirtualStaticMethodFlags resolveVirtualStaticMethodFlags, ClassLoadLevel level)
{
    bool instantiateMethodParameters = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::InstantiateResultOverFinalMethodDesc) != ResolveVirtualStaticMethodFlags::None;
    bool allowVariance = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::AllowVariantMatches) != ResolveVirtualStaticMethodFlags::None;
    bool verifyImplemented = (resolveVirtualStaticMethodFlags & ResolveVirtualStaticMethodFlags::VerifyImplemented) != ResolveVirtualStaticMethodFlags::None;

    HRESULT hr = S_OK;
    IMDInternalImport* pMDInternalImport = GetMDImport();
    HENUMInternalMethodImplHolder hEnumMethodImpl(pMDInternalImport);
    hr = hEnumMethodImpl.EnumMethodImplInitNoThrow(GetCl());
    SigTypeContext sigTypeContext(this);

    if (FAILED(hr))
    {
        COMPlusThrow(kTypeLoadException, hr);
    }

    // This gets the count out of the metadata interface.
    uint32_t dwNumberMethodImpls = hEnumMethodImpl.EnumMethodImplGetCount();
    MethodDesc* pPrevMethodImpl = nullptr;

    // Iterate through each MethodImpl declared on this class
    for (uint32_t i = 0; i < dwNumberMethodImpls; i++)
    {
        mdToken methodBody;
        mdToken methodDecl;
        hr = hEnumMethodImpl.EnumMethodImplNext(&methodBody, &methodDecl);
        if (FAILED(hr))
        {
            COMPlusThrow(kTypeLoadException, hr);
        }
        if (hr == S_FALSE)
        {
            // In the odd case that the enumerator fails before we've reached the total reported
            // entries, let's reset the count and just break out. (Should we throw?)
            break;
        }
        mdToken tkParent;
        hr = pMDInternalImport->GetParentToken(methodDecl, &tkParent);
        if (FAILED(hr))
        {
            COMPlusThrow(kTypeLoadException, hr);
        }
        MethodTable *pInterfaceMT = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
            GetModule(),
            tkParent,
            &sigTypeContext,
            ClassLoader::ThrowIfNotFound,
            ClassLoader::FailIfUninstDefOrRef,
            ClassLoader::LoadTypes,
            CLASS_LOAD_EXACTPARENTS)
            .GetMethodTable();

        if (allowVariance)
        {
            // Allow variant, but not equivalent interface match
            if (!pInterfaceType->HasSameTypeDefAs(pInterfaceMT) ||
                !pInterfaceMT->CanCastTo(pInterfaceType, NULL))
            {
                continue;
            }
        }
        else
        {
            if (pInterfaceMT != pInterfaceType)
            {
                continue;
            }
        }
        MethodDesc *pMethodDecl;

        if ((TypeFromToken(methodDecl) == mdtMethodDef) || pInterfaceMT->IsFullyLoaded())
        {
            pMethodDecl =  MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(
            GetModule(),
            methodDecl,
            &sigTypeContext,
            /* strictMetadataChecks */ FALSE,
            /* allowInstParam */ FALSE,
            /* owningTypeLoadLevel */ CLASS_LOAD_EXACTPARENTS);
        }
        else if (TypeFromToken(methodDecl) == mdtMemberRef)
        {
            LPCUTF8     szMember;
            PCCOR_SIGNATURE pSig;
            DWORD       cSig;

            IfFailThrow(pMDInternalImport->GetNameAndSigOfMemberRef(methodDecl, &pSig, &cSig, &szMember));

            // Do a quick name check to avoid excess use of FindMethod
            if (strcmp(szMember, pInterfaceMD->GetName()) != 0)
            {
                continue;
            }

            pMethodDecl = MemberLoader::FindMethod(pInterfaceMT, szMember, pSig, cSig, GetModule());
        }
        else
        {
            COMPlusThrow(kTypeLoadException, E_FAIL);
        }

        if (pMethodDecl == nullptr)
        {
            COMPlusThrow(kTypeLoadException, E_FAIL);
        }
        if (!pMethodDecl->HasSameMethodDefAs(pInterfaceMD))
        {
            continue;
        }

        // Spec requires that all body token for MethodImpls that refer to static virtual implementation methods must be MethodDef tokens.
        if (TypeFromToken(methodBody) != mdtMethodDef)
        {
            COMPlusThrow(kTypeLoadException, E_FAIL);
        }

        MethodDesc *pMethodImpl = MemberLoader::GetMethodDescFromMethodDef(
            GetModule(),
            methodBody,
            FALSE,
            CLASS_LOAD_EXACTPARENTS);
        if (pMethodImpl == nullptr)
        {
            COMPlusThrow(kTypeLoadException, E_FAIL);
        }

        // Spec requires that all body token for MethodImpls that refer to static virtual implementation methods must to methods
        // defined on the same type that defines the MethodImpl
        if (!HasSameTypeDefAs(pMethodImpl->GetMethodTable()))
        {
            COMPlusThrow(kTypeLoadException, E_FAIL);
        }

        if (!verifyImplemented && instantiateMethodParameters)
        {
            pMethodImpl = pMethodImpl->FindOrCreateAssociatedMethodDesc(
                pMethodImpl,
                this,
                FALSE,
                pInterfaceMD->GetMethodInstantiation(),
                /* allowInstParam */ FALSE,
                /* forceRemotableMethod */ FALSE,
                /* allowCreate */ TRUE,
                /* level */ level);
        }
        if (pMethodImpl != nullptr)
        {
            if (!verifyImplemented)
            {
                return pMethodImpl;
            }
            if (pPrevMethodImpl != nullptr)
            {
                // Two MethodImpl records found for the same virtual static interface method
                COMPlusThrow(kTypeLoadException, E_FAIL);
            }
            pPrevMethodImpl = pMethodImpl;
        }
    }

    return pPrevMethodImpl;
}

void
MethodTable::VerifyThatAllVirtualStaticMethodsAreImplemented()
{
    InterfaceMapIterator it = IterateInterfaceMap();
    while (it.Next())
    {
        MethodTable *pInterfaceMT = it.GetInterfaceApprox();
        if (pInterfaceMT->HasVirtualStaticMethods())
        {
            pInterfaceMT = it.GetInterface(this, CLASS_LOAD_EXACTPARENTS);
            for (MethodIterator it(pInterfaceMT); it.IsValid(); it.Next())
            {
                MethodDesc *pMD = it.GetMethodDesc();
                // This flag is not really used, passing its address to ResolveVirtualStaticMethod just suppresses
                // the ambiguous resolution exception throw as we should delay the exception until we actually hit
                // the ambiguity at execution time.
                BOOL uniqueResolution;
                if (pMD->IsVirtual() &&
                    pMD->IsStatic() &&
                    !pMD->HasMethodImplSlot() && // Re-abstractions are virtual static abstract with a MethodImpl
                    (pMD->IsAbstract() &&
                    !ResolveVirtualStaticMethod(
                        pInterfaceMT,
                        pMD,
                        ResolveVirtualStaticMethodFlags::AllowNullResult | ResolveVirtualStaticMethodFlags::VerifyImplemented,
                        /* uniqueResolution */ &uniqueResolution,
                        /* level */ CLASS_LOAD_EXACTPARENTS)))
                {
                    IMDInternalImport* pInternalImport = GetModule()->GetMDImport();
                    GetModule()->GetAssembly()->ThrowTypeLoadException(pInternalImport, GetCl(), pMD->GetName(), IDS_CLASSLOAD_STATICVIRTUAL_NOTIMPL);
                }
            }
        }
    }
}

//==========================================================================================
// Finds the (non-unboxing) MethodDesc that implements the interface method pInterfaceMD.
//
// Note our ability to resolve constraint methods is affected by the degree of code sharing we are
// performing for generic code.
//
// Return Value:
//   MethodDesc which can be used as unvirtualized call. Returns NULL if VSD has to be used.
MethodDesc *
MethodTable::TryResolveConstraintMethodApprox(
    TypeHandle   thInterfaceType,
    MethodDesc * pInterfaceMD,
    BOOL *       pfForceUseRuntimeLookup)   // = NULL
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (pInterfaceMD->IsStatic())
    {
        _ASSERTE(!thInterfaceType.IsTypeDesc());
        _ASSERTE(thInterfaceType.IsInterface());
        BOOL uniqueResolution = TRUE;

        ResolveVirtualStaticMethodFlags flags = ResolveVirtualStaticMethodFlags::AllowVariantMatches
                                              | ResolveVirtualStaticMethodFlags::InstantiateResultOverFinalMethodDesc;
        if (pfForceUseRuntimeLookup != NULL)
        {
            flags |= ResolveVirtualStaticMethodFlags::AllowNullResult;
        }

        MethodDesc *result = ResolveVirtualStaticMethod(
            thInterfaceType.GetMethodTable(),
            pInterfaceMD,
            flags,
            (pfForceUseRuntimeLookup != NULL ? &uniqueResolution : NULL));

        if (result == NULL || !uniqueResolution)
        {
            _ASSERTE(pfForceUseRuntimeLookup != NULL);
            *pfForceUseRuntimeLookup = TRUE;
            result = NULL;
        }
        return result;
    }

    // We can't resolve constraint calls effectively for reference types, and there's
    // not a lot of perf. benefit in doing it anyway.
    //
    if (!IsValueType())
    {
        LOG((LF_JIT, LL_INFO10000, "TryResolveConstraintmethodApprox: not a value type %s\n", GetDebugClassName()));
        return NULL;
    }

    // 1. Find the (possibly generic) method that would implement the
    // constraint if we were making a call on a boxed value type.

    MethodTable * pCanonMT = GetCanonicalMethodTable();

    MethodDesc * pGenInterfaceMD = pInterfaceMD->StripMethodInstantiation();
    MethodDesc * pMD = NULL;
    if (pGenInterfaceMD->IsInterface())
    {   // Sometimes (when compiling shared generic code)
        // we don't have enough exact type information at JIT time
        // even to decide whether we will be able to resolve to an unboxed entry point...
        // To cope with this case we always go via the helper function if there's any
        // chance of this happening by checking for all interfaces which might possibly
        // be compatible with the call (verification will have ensured that
        // at least one of them will be)

        // Enumerate all potential interface instantiations
        MethodTable::InterfaceMapIterator it = pCanonMT->IterateInterfaceMap();
        DWORD cPotentialMatchingInterfaces = 0;
        while (it.Next())
        {
            TypeHandle thPotentialInterfaceType(it.GetInterface(pCanonMT));
            if (thPotentialInterfaceType.AsMethodTable()->GetCanonicalMethodTable() ==
                thInterfaceType.AsMethodTable()->GetCanonicalMethodTable())
            {
                cPotentialMatchingInterfaces++;
                pMD = pCanonMT->GetMethodDescForInterfaceMethod(thPotentialInterfaceType, pGenInterfaceMD, FALSE /* throwOnConflict */);

                // See code:#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
                if ((pMD != NULL) && !pMD->GetMethodTable()->IsValueType() && !pMD->IsInterface())
                {
                    LOG((LF_JIT, LL_INFO10000, "TryResolveConstraintMethodApprox: %s::%s not a value type method\n",
                        pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
                    return NULL;
                }
            }
        }

        _ASSERTE_MSG((cPotentialMatchingInterfaces != 0),
            "At least one interface has to implement the method, otherwise there's a bug in JIT/verification.");

        if (cPotentialMatchingInterfaces > 1)
        {   // We have more potentially matching interfaces
            MethodTable * pInterfaceMT = thInterfaceType.GetMethodTable();
            _ASSERTE(pInterfaceMT->HasInstantiation());

            BOOL fIsExactMethodResolved = FALSE;

            if (!pInterfaceMT->IsSharedByGenericInstantiations() &&
                !pInterfaceMT->IsGenericTypeDefinition() &&
                !this->IsSharedByGenericInstantiations() &&
                !this->IsGenericTypeDefinition())
            {   // We have exact interface and type instantiations (no generic variables and __Canon used
                // anywhere)
                if (this->CanCastToInterface(pInterfaceMT))
                {
                    // We can resolve to exact method
                    pMD = this->GetMethodDescForInterfaceMethod(pInterfaceMT, pInterfaceMD, FALSE /* throwOnConflict */);
                    fIsExactMethodResolved = pMD != NULL;
                }
            }

            if (!fIsExactMethodResolved)
            {   // We couldn't resolve the interface statically
                _ASSERTE(pfForceUseRuntimeLookup != NULL);
                // Notify the caller that it should use runtime lookup
                // Note that we can leave pMD incorrect, because we will use runtime lookup
                *pfForceUseRuntimeLookup = TRUE;
            }
        }
        else
        {
            // If we can resolve the interface exactly then do so (e.g. when doing the exact
            // lookup at runtime, or when not sharing generic code).
            if (pCanonMT->CanCastToInterface(thInterfaceType.GetMethodTable()))
            {
                pMD = pCanonMT->GetMethodDescForInterfaceMethod(thInterfaceType, pGenInterfaceMD, FALSE /* throwOnConflict */);
                if (pMD == NULL)
                {
                    LOG((LF_JIT, LL_INFO10000, "TryResolveConstraintMethodApprox: failed to find method desc for interface method\n"));
                }
            }
        }
    }
    else if (pGenInterfaceMD->IsVirtual())
    {
        if (pGenInterfaceMD->HasNonVtableSlot() && pGenInterfaceMD->GetMethodTable()->IsValueType())
        {   // GetMethodDescForSlot would AV for this slot
            // We can get here for (invalid and unverifiable) IL:
            //    constrained. int32
            //    callvirt System.Int32::GetHashCode()
            pMD = pGenInterfaceMD;
        }
        else
        {
            pMD = GetMethodDescForSlot(pGenInterfaceMD->GetSlot());
        }
    }
    else
    {
        // The pMD will be NULL if calling a non-virtual instance
        // methods on System.Object, i.e. when these are used as a constraint.
        pMD = NULL;
    }

    if (pMD == NULL)
    {   // Fall back to VSD
        return NULL;
    }

    if (!pMD->GetMethodTable()->IsInterface())
    {
        //#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
        // Only return a method if the value type itself declares the method
        // otherwise we might get a method from Object or System.ValueType
        if (!pMD->GetMethodTable()->IsValueType())
        {   // Fall back to VSD
            return NULL;
        }

        // We've resolved the method, ignoring its generic method arguments
        // If the method is a generic method then go and get the instantiated descriptor
        pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pMD,
            this,
            FALSE /* no BoxedEntryPointStub */,
            pInterfaceMD->GetMethodInstantiation(),
            FALSE /* no allowInstParam */);

        // FindOrCreateAssociatedMethodDesc won't return an BoxedEntryPointStub.
        _ASSERTE(pMD != NULL);
        _ASSERTE(!pMD->IsUnboxingStub());
    }

    return pMD;
} // MethodTable::TryResolveConstraintMethodApprox

//==========================================================================================
// Make best-case effort to obtain an image name for use in an error message.
//
// This routine must expect to be called before the this object is fully loaded.
// It can return an empty if the name isn't available or the object isn't initialized
// enough to get a name, but it mustn't crash.
//==========================================================================================
LPCWSTR MethodTable::GetPathForErrorMessages()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    Module *pModule = GetModule();

    if (pModule)
    {
        return pModule->GetPathForErrorMessages();
    }
    else
    {
        return W("");
    }
}

BOOL MethodTable::Validate()
{
    LIMITED_METHOD_CONTRACT;

    ASSERT_AND_CHECK(SanityCheck());

#ifdef _DEBUG
    ASSERT_AND_CHECK(m_pAuxiliaryData != NULL);

    MethodTableAuxiliaryData *pAuxiliaryData = m_pAuxiliaryData;
    DWORD dwLastVerifiedGCCnt = pAuxiliaryData->m_dwLastVerifedGCCnt;
    // Here we used to assert that (dwLastVerifiedGCCnt <= GCHeapUtilities::GetGCHeap()->GetGcCount()) but
    // this is no longer true because with background gc. Since the purpose of having
    // m_dwLastVerifedGCCnt is just to only verify the same method table once for each GC
    // I am getting rid of the assert.
    if (g_pConfig->FastGCStressLevel () > 1 && dwLastVerifiedGCCnt == GCHeapUtilities::GetGCHeap()->GetGcCount())
        return TRUE;
#endif //_DEBUG

    if (IsArray())
    {
        if (!SanityCheck())
        {
            ASSERT_AND_CHECK(!"Detected use of a corrupted OBJECTREF. Possible GC hole.");
        }
    }
    else if (!IsCanonicalMethodTable())
    {
        // Non-canonical method tables has to have non-empty instantiation
        if (GetInstantiation().IsEmpty())
        {
            ASSERT_AND_CHECK(!"Detected use of a corrupted OBJECTREF. Possible GC hole.");
        }
    }

#ifdef _DEBUG
    pAuxiliaryData->m_dwLastVerifedGCCnt = GCHeapUtilities::GetGCHeap()->GetGcCount();
#endif //_DEBUG

    return TRUE;
}

#endif // !DACCESS_COMPILE

NOINLINE BYTE *MethodTable::GetLoaderAllocatorObjectForGC()
{
    WRAPPER_NO_CONTRACT;
    if (!Collectible())
    {
        return NULL;
    }
    BYTE * retVal = *(BYTE**)GetLoaderAllocatorObjectHandle();
    return retVal;
}

int MethodTable::GetFieldAlignmentRequirement()
{
    if (HasLayout())
    {
        return GetLayoutInfo()->m_ManagedLargestAlignmentRequirementOfAllMembers;
    }
    else if (GetClass()->HasCustomFieldAlignment())
    {
        return GetClass()->GetOverriddenFieldAlignmentRequirement();
    }
    return min(GetNumInstanceFieldBytes(), TARGET_POINTER_SIZE);
}

UINT32 MethodTable::GetNativeSize()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(GetClass()));
    }
    CONTRACTL_END;
    if (IsBlittable())
    {
        return GetClass()->GetLayoutInfo()->GetManagedSize();
    }
    return GetNativeLayoutInfo()->GetSize();
}

EEClassNativeLayoutInfo const* MethodTable::GetNativeLayoutInfo()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        PRECONDITION(HasLayout());
    }
    CONTRACTL_END;
    EEClassNativeLayoutInfo* pNativeLayoutInfo = GetClass()->GetNativeLayoutInfo();
    if (pNativeLayoutInfo != nullptr)
    {
        return pNativeLayoutInfo;
    }
    return EnsureNativeLayoutInfoInitialized();
}

EEClassNativeLayoutInfo const* MethodTable::EnsureNativeLayoutInfoInitialized()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        PRECONDITION(HasLayout());
    }
    CONTRACTL_END;
#ifndef DACCESS_COMPILE
    EEClassNativeLayoutInfo::InitializeNativeLayoutFieldMetadataThrowing(this);
    return this->GetClass()->GetNativeLayoutInfo();
#else
    DacNotImpl();
    return nullptr;
#endif
}

#ifndef DACCESS_COMPILE
PTR_MethodTable MethodTable::InterfaceMapIterator::GetInterface(MethodTable* pMTOwner, ClassLoadLevel loadLevel /*= CLASS_LOADED*/)
{
    CONTRACT(PTR_MethodTable)
    {
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(m_i != (DWORD) -1 && m_i < m_count);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodTable *pResult = m_pMap->GetMethodTable();
    if (pResult->IsSpecialMarkerTypeForGenericCasting() && !pMTOwner->ContainsGenericVariables())
    {
        TypeHandle ownerAsInst[MaxGenericParametersForSpecialMarkerType];
        for (DWORD i = 0; i < MaxGenericParametersForSpecialMarkerType; i++)
            ownerAsInst[i] = pMTOwner;

        _ASSERTE(pResult->GetInstantiation().GetNumArgs() <= MaxGenericParametersForSpecialMarkerType);
        Instantiation inst(ownerAsInst, pResult->GetInstantiation().GetNumArgs());
        pResult = ClassLoader::LoadGenericInstantiationThrowing(pResult->GetModule(), pResult->GetCl(), inst, ClassLoader::LoadTypes, loadLevel).AsMethodTable();
        if (pResult->IsFullyLoaded())
            SetInterface(pResult);
    }
    RETURN (pResult);
}
#endif // DACCESS_COMPILE

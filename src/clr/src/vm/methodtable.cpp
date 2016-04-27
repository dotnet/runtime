// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: methodtable.cpp
//


//

//
// ============================================================================

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
#include "verifier.hpp"
#include "jitinterface.h"
#include "eeconfig.h"
#include "log.h"
#include "fieldmarshaler.h"
#include "cgensys.h"
#include "gc.h"
#include "security.h"
#include "dbginterface.h"
#include "comdelegate.h"
#include "eventtrace.h"
#include "fieldmarshaler.h"

#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif

#include "eeprofinterfaces.h"
#include "dllimportcallback.h"
#include "listlock.h"
#include "methodimpl.h"
#include "guidfromname.h"
#include "stackprobe.h"
#include "encee.h"
#include "encee.h"
#include "comsynchronizable.h"
#include "customattribute.h"
#include "virtualcallstub.h"
#include "contractimpl.h"
#ifdef FEATURE_PREJIT
#include "zapsig.h"
#endif //FEATURE_PREJIT

#include "hostexecutioncontext.h"

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "runtimecallablewrapper.h"
#include "winrttypenameconverter.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_TYPEEQUIVALENCE
#include "typeequivalencehash.hpp"
#endif

#include "listlock.inl"
#include "generics.h"
#include "genericdict.h"
#include "typestring.h"
#include "typedesc.h"
#ifdef FEATURE_REMOTING
#include "crossdomaincalls.h"
#endif
#include "array.h"

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

#ifndef DACCESS_COMPILE

// Typedef for string comparition functions.
typedef int (__cdecl *UTF8StringCompareFuncPtr)(const char *, const char *);

MethodDataCache *MethodTable::s_pMethodDataCache = NULL;
BOOL MethodTable::s_fUseMethodDataCache = FALSE;
BOOL MethodTable::s_fUseParentMethodData = FALSE;

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

#ifdef _WIN64
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
//
// Initialize the offsets of multipurpose slots at compile time using template metaprogramming
//

template<int N>
struct CountBitsAtCompileTime
{
    enum { value = (N & 1) + CountBitsAtCompileTime<(N >> 1)>::value };
};

template<>
struct CountBitsAtCompileTime<0>
{
    enum { value = 0 };
};

// "mask" is mask of used slots.
template<int mask>
struct MethodTable::MultipurposeSlotOffset
{
    // This is raw index of the slot assigned on first come first served basis
    enum { raw = CountBitsAtCompileTime<mask>::value };

    // This is actual index of the slot. It is equal to raw index except for the case
    // where the first fixed slot is not used, but the second one is. The first fixed
    // slot has to be assigned instead of the second one in this case. This assumes that 
    // there are exactly two fixed slots.
    enum { index = (((mask & 3) == 2) && (raw == 1)) ? 0 : raw };

    // Offset of slot
    enum { slotOffset = (index == 0) ? offsetof(MethodTable, m_pMultipurposeSlot1) :
                        (index == 1) ? offsetof(MethodTable, m_pMultipurposeSlot2) :
                        (sizeof(MethodTable) + index * sizeof(TADDR) - 2 * sizeof(TADDR)) };

    // Size of methodtable with overflow slots. It is used to compute start offset of optional members.
    enum { totalSize = (slotOffset >= sizeof(MethodTable)) ? slotOffset : sizeof(MethodTable) };
};

//
// These macros recursively expand to create 2^N values for the offset arrays
//
#define MULTIPURPOSE_SLOT_OFFSET_1(mask) MULTIPURPOSE_SLOT_OFFSET  (mask) MULTIPURPOSE_SLOT_OFFSET  (mask | 0x01)
#define MULTIPURPOSE_SLOT_OFFSET_2(mask) MULTIPURPOSE_SLOT_OFFSET_1(mask) MULTIPURPOSE_SLOT_OFFSET_1(mask | 0x02)
#define MULTIPURPOSE_SLOT_OFFSET_3(mask) MULTIPURPOSE_SLOT_OFFSET_2(mask) MULTIPURPOSE_SLOT_OFFSET_2(mask | 0x04)
#define MULTIPURPOSE_SLOT_OFFSET_4(mask) MULTIPURPOSE_SLOT_OFFSET_3(mask) MULTIPURPOSE_SLOT_OFFSET_3(mask | 0x08)
#define MULTIPURPOSE_SLOT_OFFSET_5(mask) MULTIPURPOSE_SLOT_OFFSET_4(mask) MULTIPURPOSE_SLOT_OFFSET_4(mask | 0x10)

#define MULTIPURPOSE_SLOT_OFFSET(mask) MultipurposeSlotOffset<mask>::slotOffset,
const BYTE MethodTable::c_DispatchMapSlotOffsets[] = {
    MULTIPURPOSE_SLOT_OFFSET_2(0)
};
const BYTE MethodTable::c_NonVirtualSlotsOffsets[] = {
    MULTIPURPOSE_SLOT_OFFSET_3(0)
};
const BYTE MethodTable::c_ModuleOverrideOffsets[] = {
    MULTIPURPOSE_SLOT_OFFSET_4(0)
};
#undef MULTIPURPOSE_SLOT_OFFSET

#define MULTIPURPOSE_SLOT_OFFSET(mask) MultipurposeSlotOffset<mask>::totalSize,
const BYTE MethodTable::c_OptionalMembersStartOffsets[] = {
    MULTIPURPOSE_SLOT_OFFSET_5(0)
};
#undef MULTIPURPOSE_SLOT_OFFSET


//==========================================================================================
// Optimization intended for MethodTable::GetModule, MethodTable::GetDispatchMap and MethodTable::GetNonVirtualSlotsPtr

#include <optsmallperfcritical.h>

PTR_Module MethodTable::GetModule()
{
    LIMITED_METHOD_DAC_CONTRACT;

    g_IBCLogger.LogMethodTableAccess(this);

    // Fast path for non-generic non-array case
    if ((m_dwFlags & (enum_flag_HasComponentSize | enum_flag_GenericsMask)) == 0)
        return GetLoaderModule();

    MethodTable * pMTForModule = IsArray() ? this : GetCanonicalMethodTable();
    if (!pMTForModule->HasModuleOverride())
        return pMTForModule->GetLoaderModule();

    TADDR pSlot = pMTForModule->GetMultipurposeSlotPtr(enum_flag_HasModuleOverride, c_ModuleOverrideOffsets);
    return RelativeFixupPointer<PTR_Module>::GetValueAtPtr(pSlot);
}

//==========================================================================================
PTR_Module MethodTable::GetModule_NoLogging()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Fast path for non-generic non-array case
    if ((m_dwFlags & (enum_flag_HasComponentSize | enum_flag_GenericsMask)) == 0)
        return GetLoaderModule();

    MethodTable * pMTForModule = IsArray() ? this : GetCanonicalMethodTable();
    if (!pMTForModule->HasModuleOverride())
        return pMTForModule->GetLoaderModule();

    TADDR pSlot = pMTForModule->GetMultipurposeSlotPtr(enum_flag_HasModuleOverride, c_ModuleOverrideOffsets);
    return RelativeFixupPointer<PTR_Module>::GetValueAtPtr(pSlot);
}

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

    g_IBCLogger.LogDispatchMapAccess(pMT);

    TADDR pSlot = pMT->GetMultipurposeSlotPtr(enum_flag_HasDispatchMapSlot, c_DispatchMapSlotOffsets);
    return RelativePointer<PTR_DispatchMap>::GetValueAtPtr(pSlot);
}

//==========================================================================================
TADDR MethodTable::GetNonVirtualSlotsPtr()
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(GetFlag(enum_flag_HasNonVirtualSlots));
    return GetMultipurposeSlotPtr(enum_flag_HasNonVirtualSlots, c_NonVirtualSlotsOffsets);
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

    g_IBCLogger.LogMethodTableAccess(this);

    MethodTable * pMTForModule = IsArray() ? this : GetCanonicalMethodTable();
    if (!pMTForModule->HasModuleOverride())
        return pMTForModule->GetLoaderModule();

    return Module::RestoreModulePointerIfLoaded(pMTForModule->GetModuleOverridePtr(), pMTForModule->GetLoaderModule());
}

#ifndef DACCESS_COMPILE
//==========================================================================================
void MethodTable::SetModule(Module * pModule)
{
    LIMITED_METHOD_CONTRACT;

    if (HasModuleOverride())
    {
        GetModuleOverridePtr()->SetValue(pModule);
    }

    _ASSERTE(GetModule() == pModule);
}
#endif // DACCESS_COMPILE

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
    return ((this == pEEClass->GetMethodTableWithPossibleAV()) ||
        ((HasInstantiation() || IsArray()) &&
        (pEEClass->GetMethodTableWithPossibleAV()->GetClassWithPossibleAV() == pEEClass)));
}

#ifndef DACCESS_COMPILE

//==========================================================================================
BOOL  MethodTable::IsClassInited(AppDomain* pAppDomain /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    if (IsClassPreInited())
        return TRUE;
    
    if (IsSharedByGenericInstantiations())
        return FALSE;

    DomainLocalModule *pLocalModule;
    if (pAppDomain == NULL)
    {
        pLocalModule = GetDomainLocalModule();
    }
    else
    {
        pLocalModule = GetDomainLocalModule(pAppDomain);
    }

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
    _ASSERTE(!IsClassPreInited() || MscorlibBinder::IsClass(this, CLASS__SHARED_STATICS));
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

    // If functions on this type have already been requested for rejit, then give the rejit
    // manager a chance to jump-stamp the code we are implicitly restoring. This ensures the
    // first thread entering the function will jump to the prestub and trigger the
    // rejit. Note that the PublishMethodTableHolder may take a lock to avoid a rejit race.
    // See code:ReJitManager::PublishMethodHolder::PublishMethodHolder#PublishCode
    // for details on the race.
    // 
    {
        ReJitPublishMethodTableHolder(this);
        FastInterlockAnd(EnsureWritablePages(&(GetWriteableDataForWrite()->m_dwFlags)), ~MethodTableWriteableData::enum_flag_Unrestored);
    }
#ifndef DACCESS_COMPILE
    if (ETW_PROVIDER_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER))
    {
        ETW::MethodLog::MethodTableRestored(this);
    }
#endif
}

#ifdef FEATURE_COMINTEROP

//==========================================================================================
// mark as COM object type (System.__ComObject and types deriving from it)
void MethodTable::SetComObjectType()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_ComObject);
}

#endif // FEATURE_COMINTEROP

#if defined(FEATURE_TYPEEQUIVALENCE) || defined(FEATURE_REMOTING)
void MethodTable::SetHasTypeEquivalence()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_HasTypeEquivalence);
}
#endif

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
  

#endif // !DACCESS_COMPILE

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
    g_IBCLogger.LogMethodTableAccess(this);
    return GetLoaderModule()->GetDomain();
}

//==========================================================================================
BOOL MethodTable::IsDomainNeutral()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC;

    BOOL ret = GetLoaderModule()->GetAssembly()->IsDomainNeutral();
#ifndef DACCESS_COMPILE
    _ASSERTE(!ret == !GetLoaderAllocator()->IsDomainNeutral());
#endif

    return ret;
}

//==========================================================================================
BOOL MethodTable::HasSameTypeDefAs(MethodTable *pMT)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (this == pMT)
        return TRUE;

    // optimize for the negative case where we expect RID mismatch
    if (GetTypeDefRid() != pMT->GetTypeDefRid())
        return FALSE;

    if (GetCanonicalMethodTable() == pMT->GetCanonicalMethodTable())
        return TRUE;

    return (GetModule() == pMT->GetModule());
}

//==========================================================================================
BOOL MethodTable::HasSameTypeDefAs_NoLogging(MethodTable *pMT)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (this == pMT)
        return TRUE;

    // optimize for the negative case where we expect RID mismatch
    if (GetTypeDefRid_NoLogging() != pMT->GetTypeDefRid_NoLogging())
        return FALSE;

    if (GetCanonicalMethodTable() == pMT->GetCanonicalMethodTable())
        return TRUE;

    return (GetModule_NoLogging() == pMT->GetModule_NoLogging());
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
#ifdef FEATURE_PREJIT
    if (m_pMethodTable.IsTagged())
    {
        // Ideally, we would use Module::RestoreMethodTablePointer here. Unfortunately, it is not 
        // possible because of the current type loader architecture that restores types incrementally 
        // even in the NGen case.
        MethodTable * pItfMT = *(m_pMethodTable.GetValuePtr());

        // Restore the method table, but do not write it back if it has instantiation. We do not want 
        // to write back the approximate instantiations.
        Module::RestoreMethodTablePointerRaw(&pItfMT, pContainingModule, CLASS_LOAD_APPROXPARENTS);

        if (!pItfMT->HasInstantiation())
        {
            // m_pMethodTable.SetValue() is not used here since we want to update the indirection cell
            *EnsureWritablePages(m_pMethodTable.GetValuePtr()) = pItfMT;
        }

        return pItfMT;
    }
#endif
    MethodTable * pItfMT = m_pMethodTable.GetValue();
    ClassLoader::EnsureLoaded(TypeHandle(pItfMT), CLASS_LOAD_APPROXPARENTS);
    return pItfMT;
}

#ifndef CROSSGEN_COMPILE
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

    if (pServerMT->IsTransparentProxy())
    {
        // If pServer is a TP, then the interface method desc is the one to
        // use to dispatch the call.
        RETURN(pItfMD);
    }

#ifdef FEATURE_ICASTABLE
    // In case of ICastable, instead of trying to find method implementation in the real object type
    // we call pObj.GetValueInternal() and call GetMethodDescForInterfaceMethod() again with whatever type it returns.
    // It allows objects that implement ICastable to mimic behavior of other types.             
    if (pServerMT->IsICastable() && 
        !pItfMD->HasMethodInstantiation() && 
        !TypeHandle(pServerMT).CanCastTo(ownerType)) // we need to make sure object doesn't implement this interface in a natural way 
    {
        GCStress<cfg_any>::MaybeTrigger();

        // Make call to obj.GetImplType(interfaceTypeObj)
        MethodDesc *pGetImplTypeMD = pServerMT->GetMethodDescForInterfaceMethod(MscorlibBinder::GetMethod(METHOD__ICASTABLE__GETIMPLTYPE));
        OBJECTREF ownerManagedType = ownerType.GetManagedClassObject(); //GC triggers

        PREPARE_NONVIRTUAL_CALLSITE_USING_METHODDESC(pGetImplTypeMD);
        
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
        TypeHandle resulTypeHnd = resultTypeObj->GetType();
        MethodTable *pResultMT = resulTypeHnd.GetMethodTable();

        RETURN(pResultMT->GetMethodDescForInterfaceMethod(ownerType, pItfMD));
    }
#endif    

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
    RETURN (pServerMT->GetMethodDescForInterfaceMethod(ownerType, pItfMD));
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
            pItfMT->GetLoaderAllocator()->GetDispatchToken(pItfMT->GetTypeID(), pItfMD->GetSlot()), this);

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

#endif // CROSSGEN_COMPILE

//---------------------------------------------------------------------------------------
// 
MethodTable* CreateMinimalMethodTable(Module* pContainingModule, 
                                      LoaderHeap* pCreationHeap,
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

    EEClass* pClass = EEClass::CreateMinimalClass(pCreationHeap, pamTracker);

    LOG((LF_BCL, LL_INFO100, "Level2 - Creating MethodTable {0x%p}...\n", pClass));

    MethodTable* pMT = (MethodTable *)(void *)pamTracker->Track(pCreationHeap->AllocMem(S_SIZE_T(sizeof(MethodTable))));

    // Note: Memory allocated on loader heap is zero filled
    // memset(pMT, 0, sizeof(MethodTable));
    
    // Allocate the private data block ("private" during runtime in the ngen'ed case).
    BYTE* pMTWriteableData = (BYTE *)
        pamTracker->Track(pCreationHeap->AllocMem(S_SIZE_T(sizeof(MethodTableWriteableData))));
    pMT->SetWriteableData((PTR_MethodTableWriteableData)pMTWriteableData);

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
    pMT->SetLoaderModule(pContainingModule);
    pMT->SetLoaderAllocator(pContainingModule->GetLoaderAllocator());
    pMT->SetInternalCorElementType(ELEMENT_TYPE_CLASS);
    pMT->SetBaseSize(ObjSizeOf(Object));

#ifdef _DEBUG
    pClass->SetDebugClassName("dynamicClass");
    pMT->SetDebugClassName("dynamicClass");
#endif

    LOG((LF_BCL, LL_INFO10, "Level1 - MethodTable created {0x%p}\n", pClass));
    
    return pMT;
}

#ifdef FEATURE_REMOTING  
//==========================================================================================
void MethodTable::SetupRemotableMethodInfo(AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Make RMI for a method table.
    CrossDomainOptimizationInfo *pRMIBegin = NULL;
    if (GetNumMethods() > 0)
    {
        SIZE_T requiredSize = CrossDomainOptimizationInfo::SizeOf(GetNumVtableSlots());
        pRMIBegin = (CrossDomainOptimizationInfo*) pamTracker->Track(GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(requiredSize)));
        _ASSERTE(IS_ALIGNED(pRMIBegin, sizeof(void*)));
    }
    *(GetRemotableMethodInfoPtr()) = pRMIBegin;
}

//==========================================================================================
PTR_RemotingVtsInfo MethodTable::AllocateRemotingVtsInfo(AllocMemTracker *pamTracker, DWORD dwNumFields)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Size the data structure to contain enough bit flags for all the
    // instance fields.
    DWORD cbInfo = RemotingVtsInfo::GetSize(dwNumFields);
    RemotingVtsInfo *pInfo = (RemotingVtsInfo*)pamTracker->Track(GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(cbInfo)));

    // Note: Memory allocated on loader heap is zero filled
    // ZeroMemory(pInfo, cbInfo);

#ifdef _DEBUG
    pInfo->m_dwNumFields = dwNumFields;
#endif

    *(GetRemotingVtsInfoPtr()) = pInfo;

    return pInfo;
}
#endif //  FEATURE_REMOTING  

#ifdef FEATURE_COMINTEROP
#ifndef CROSSGEN_COMPILE
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
#endif //CROSSGEN_COMPILE
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Ngen support.
void MethodTable::SaveExtraInterfaceInfo(DataImage *pImage)
{
    STANDARD_VM_CONTRACT;

    // No extra data to save if the number of interfaces is below the threshhold -- there is either no data or
    // it all fits into the optional members inline.
    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshhold)
        return;

    pImage->StoreStructure((LPVOID)*GetExtraInterfaceInfoPtr(),
                           GetExtraInterfaceInfoSize(GetNumInterfaces()),
                           DataImage::ITEM_INTERFACE_MAP);
}

void MethodTable::FixupExtraInterfaceInfo(DataImage *pImage)
{
    STANDARD_VM_CONTRACT;

    // No pointer to extra data to fixup if the number of interfaces is below the threshhold -- there is
    // either no data or it all fits into the optional members inline.
    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshhold)
        return;

    pImage->FixupPointerField(this, (BYTE*)GetExtraInterfaceInfoPtr() - (BYTE*)this);
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

// Define a macro that generates a mask for a given bit in a TADDR correctly on either 32 or 64 bit platforms.
#ifdef _WIN64
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

    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshhold)
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

    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshhold)
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

    _ASSERTE(IsRestored_NoLogging());
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
        PRECONDITION(IsRestored_NoLogging());
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
        memcpy(pNewItfMap, pInterfaceMap, TotalNumInterfaces * sizeof(InterfaceInfo_t));
    }

    // Add the new interface at the end of the map.
    pNewItfMap[TotalNumInterfaces].SetMethodTable(pItfMT);

    // Update the count of dynamically added interfaces.
    *(((DWORD_PTR *)pNewItfMap) - 1) = NumDynAddedInterfaces + 1;

    // Switch the old interface map with the new one.
    VolatileStore(EnsureWritablePages(&m_pInterfaceMap), pNewItfMap);

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
    if (cInterfaces <= kInlinedInterfaceInfoThreshhold)
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

    // No extra data to enum if the number of interfaces is below the threshhold -- there is either no data or
    // it all fits into the optional members inline.
    if (GetNumInterfaces() <= kInlinedInterfaceInfoThreshhold)
        return;

    DacEnumMemoryRegion(*GetExtraInterfaceInfoPtr(), GetExtraInterfaceInfoSize(GetNumInterfaces()));
}
#endif // DACCESS_COMPILE

//==========================================================================================
Module* MethodTable::GetModuleForStatics()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    g_IBCLogger.LogMethodTableAccess(this);

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
        SO_TOLERANT; // we are called from MethodTable::CanCastToClass
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

        // arrays of structures have their own unshared MTs and will take this path
        return (GetApproxArrayElementTypeHandle().IsEquivalentTo(pOtherMT->GetApproxArrayElementTypeHandle() COMMA_INDEBUG(&newVisited)));
    }

    BOOL bResult = FALSE;
    
    BEGIN_SO_INTOLERANT_CODE(GetThread());
    bResult = IsEquivalentTo_WorkerInner(pOtherMT COMMA_INDEBUG(&newVisited));
    END_SO_INTOLERANT_CODE;

    return bResult;
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
        SO_INTOLERANT;
        LOADS_TYPE(CLASS_DEPENDENCIES_LOADED);
    }
    CONTRACTL_END;

    AppDomain *pDomain = GetAppDomain();
    if (pDomain != NULL)
    {
        TypeEquivalenceHashTable::EquivalenceMatch match = pDomain->GetTypeEquivalenceCache()->CheckEquivalence(TypeHandle(this), TypeHandle(pOtherMT));
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

    if (HasInstantiation())
    {
        // we limit variance on generics only to interfaces
        if (!IsInterface() || !pOtherMT->IsInterface())
        {
            fEquivalent = FALSE;
            goto EquivalenceCalculated;
        }

        // check whether the instantiations are equivalent
        Instantiation inst1 = GetInstantiation();
        Instantiation inst2 = pOtherMT->GetInstantiation();

        if (inst1.GetNumArgs() != inst2.GetNumArgs())
        {
            fEquivalent = FALSE;
            goto EquivalenceCalculated;
        }

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
        fEquivalent = (GetApproxArrayElementTypeHandle().IsEquivalentTo(pOtherMT->GetApproxArrayElementTypeHandle() COMMA_INDEBUG(pVisited)));
        goto EquivalenceCalculated;
    }

    fEquivalent = CompareTypeDefsForEquivalence(GetCl(), pOtherMT->GetCl(), GetModule(), pOtherMT->GetModule(), NULL);

EquivalenceCalculated:
    // Only record equivalence matches if we are in an AppDomain
    if (pDomain != NULL)
    {
        // Collectible type results will not get cached.
        if ((!this->Collectible() && !pOtherMT->Collectible()))
        {
            TypeEquivalenceHashTable::EquivalenceMatch match;
            match = fEquivalent ? TypeEquivalenceHashTable::Match : TypeEquivalenceHashTable::NoMatch;
            pDomain->GetTypeEquivalenceCache()->RecordEquivalence(TypeHandle(this), TypeHandle(pOtherMT), match);
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
        PRECONDITION(!IsTransparentProxy());
        PRECONDITION(IsRestored_NoLogging());
    }
    CONTRACTL_END

    if (!pTargetMT->HasVariance())
    {
        if (HasTypeEquivalence() || pTargetMT->HasTypeEquivalence())
        {
            if (IsInterface() && IsEquivalentTo(pTargetMT))
                return TRUE;

            return ImplementsEquivalentInterface(pTargetMT);
        }

        return CanCastToNonVariantInterface(pTargetMT);
    }
    else
    {
        if (CanCastByVarianceToInterfaceOrDelegate(pTargetMT, pVisited))
            return TRUE;

        InterfaceMapIterator it = IterateInterfaceMap();
        while (it.Next())
        {
            if (it.GetInterface()->CanCastByVarianceToInterfaceOrDelegate(pTargetMT, pVisited))
                return TRUE;
        }
    }
    return FALSE;
}

//==========================================================================================
BOOL MethodTable::CanCastByVarianceToInterfaceOrDelegate(MethodTable *pTargetMT, TypeHandlePairList *pVisited)
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
        PRECONDITION(IsRestored_NoLogging());
    }
    CONTRACTL_END

    BOOL returnValue = FALSE;

    EEClass *pClass = NULL;

    TypeHandlePairList pairList(this, pTargetMT, pVisited);

    if (TypeHandlePairList::Exists(pVisited, this, pTargetMT))
        goto Exit;

    if (GetTypeDefRid() != pTargetMT->GetTypeDefRid() || GetModule() != pTargetMT->GetModule())
    {
        goto Exit;
    }

    {
        pClass = pTargetMT->GetClass();
        Instantiation inst = GetInstantiation();
        Instantiation targetInst = pTargetMT->GetInstantiation();

        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            TypeHandle thArg = inst[i];
            TypeHandle thTargetArg = targetInst[i];

            // If argument types are not equivalent, test them for compatibility
            // in accordance with the the variance annotation
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

            g_IBCLogger.LogMethodTableAccess(pMT);

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

            g_IBCLogger.LogMethodTableAccess(pMT);

            pMT = pMT->GetParentMethodTable();
        } while (pMT);
    }

    return FALSE;
}

#include <optsmallperfcritical.h>
//==========================================================================================
BOOL MethodTable::CanCastToNonVariantInterface(MethodTable *pTargetMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INSTANCE_CHECK;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(pTargetMT->IsInterface());
        PRECONDITION(!pTargetMT->HasVariance());
        PRECONDITION(!IsTransparentProxy());
        PRECONDITION(IsRestored_NoLogging());
    }
    CONTRACTL_END

    // Check to see if the current class is for the interface passed in.
    if (this == pTargetMT)
        return TRUE;

    // Check to see if the static class definition indicates we implement the interface.
    return ImplementsInterfaceInline(pTargetMT);
}

//==========================================================================================
TypeHandle::CastResult MethodTable::CanCastToInterfaceNoGC(MethodTable *pTargetMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INSTANCE_CHECK;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(pTargetMT->IsInterface());
        PRECONDITION(!IsTransparentProxy());
        PRECONDITION(IsRestored_NoLogging());
    }
    CONTRACTL_END

    if (!pTargetMT->HasVariance() && !IsArray() && !HasTypeEquivalence() && !pTargetMT->HasTypeEquivalence())
    {
        return CanCastToNonVariantInterface(pTargetMT) ? TypeHandle::CanCast : TypeHandle::CannotCast;
    }
    else
    {
        // We're conservative on variant interfaces and types with equivalence
        return TypeHandle::MaybeCast;
    }
}

//==========================================================================================
TypeHandle::CastResult MethodTable::CanCastToClassNoGC(MethodTable *pTargetMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INSTANCE_CHECK;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(!pTargetMT->IsArray());
        PRECONDITION(!pTargetMT->IsInterface());
    }
    CONTRACTL_END

    // We're conservative on variant classes
    if (pTargetMT->HasVariance() || g_IBCLogger.InstrEnabled())
    {
        return TypeHandle::MaybeCast;
    }

    // Type equivalence needs the slow path
    if (HasTypeEquivalence() || pTargetMT->HasTypeEquivalence())
    {
        return TypeHandle::MaybeCast;
    }

    // If there are no variant type parameters, just chase the hierarchy
    else
    {
        PTR_VOID pMT = this;

        do {
            if (pMT == pTargetMT)
                return TypeHandle::CanCast;

            pMT = MethodTable::GetParentMethodTableOrIndirection(pMT);
        } while (pMT);
    }

    return TypeHandle::CannotCast;
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
        SO_INTOLERANT;
    }
    CONTRACTL_END;

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

#ifdef FEATURE_PREJIT

BOOL MethodTable::CanShareVtableChunksFrom(MethodTable *pTargetMT, Module *pCurrentLoaderModule, Module *pCurrentPreferredZapModule)
{
    WRAPPER_NO_CONTRACT;

    // These constraints come from two places:
    //   1. A non-zapped MT cannot share with a zapped MT since it may result in SetSlot() on a read-only slot
    //   2. Zapping this MT in MethodTable::Save cannot "unshare" something we decide to share now
    //
    // We could fix both of these and allow non-zapped MTs to share chunks fully by doing the following
    //   1. Fix the few dangerous callers of SetSlot to first check whether the chunk itself is zapped 
    //        (see MethodTableBuilder::CopyExactParentSlots, or we could use ExecutionManager::FindZapModule)
    //   2. Have this function return FALSE if IsCompilationProcess and rely on MethodTable::Save to do all sharing for the NGen case

    return !pTargetMT->IsZapped() &&
            pTargetMT->GetLoaderModule() == pCurrentLoaderModule &&
            pCurrentLoaderModule == pCurrentPreferredZapModule &&
            pCurrentPreferredZapModule == Module::GetPreferredZapModuleForMethodTable(pTargetMT);
}

#else

BOOL MethodTable::CanShareVtableChunksFrom(MethodTable *pTargetMT, Module *pCurrentLoaderModule)
{
    WRAPPER_NO_CONTRACT;
    
    return pTargetMT->GetLoaderModule() == pCurrentLoaderModule;
}

#endif

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
    LPWSTR buff = fDebug ? (LPWSTR) qb.AllocNoThrow(cchBuff * sizeof(WCHAR)) : NULL;
    
    if ((buff == NULL) && fDebug)
    {
        WszOutputDebugString(W("OOM when dumping VTable - falling back to logging"));
        fDebug = FALSE;
    }
    
    if (fDebug)
    {
        swprintf_s(buff, cchBuff, W("Vtable (with interface dupes) for '%S':\n"), szClassName);
#ifdef _DEBUG
        swprintf_s(&buff[wcslen(buff)], cchBuff - wcslen(buff) , W("  Total duplicate slots = %d\n"), g_dupMethods);
#endif
        WszOutputDebugString(buff);
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
                swprintf_s(buff, cchBuff,
                           W("  slot %2d: %S::%S%S  0x%p (slot = %2d)\n"),
                           it.GetSlotNumber(),
                           name,
                           pszName,
                           IsMdFinal(dwAttrs) ? " (final)" : "",
                           pMD->GetMethodEntryPoint(),
                           pMD->GetSlot()
                          );
                WszOutputDebugString(buff);
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
                     pMD->GetMethodEntryPoint(),
                     pMD->GetSlot()
                    ));
            }
            if (it.GetSlotNumber() == (DWORD)(GetNumMethods()-1))
            {
                if (fDebug)
                {
                    WszOutputDebugString(W("  <-- vtable ends here\n"));
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
        WszOutputDebugString(W("\n"));
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
        InterfaceMapIterator it(this, false);
        while (it.Next())
        {
            MethodTable *pInterfaceMT = it.GetInterface();
            
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
    return ImplementsInterfaceInline(pInterface);
}

//==========================================================================================
BOOL MethodTable::ImplementsEquivalentInterface(MethodTable *pInterface)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_TOLERANT;
        PRECONDITION(pInterface->IsInterface()); // class we are looking up should be an interface
    }
    CONTRACTL_END;

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
MethodDesc *MethodTable::GetMethodDescForInterfaceMethod(MethodDesc *pInterfaceMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!pInterfaceMD->HasClassOrMethodInstantiation());
    }
    CONTRACTL_END;
    WRAPPER_NO_CONTRACT;

    return GetMethodDescForInterfaceMethod(TypeHandle(pInterfaceMD->GetMethodTable()), pInterfaceMD);
}

//==========================================================================================
MethodDesc *MethodTable::GetMethodDescForInterfaceMethod(TypeHandle ownerType, MethodDesc *pInterfaceMD)
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

#ifdef CROSSGEN_COMPILE
    DispatchSlot implSlot(FindDispatchSlot(pInterfaceMT->GetTypeID(), pInterfaceMD->GetSlot()));
    PCODE pTgt = implSlot.GetTarget();
#else
    PCODE pTgt = VirtualCallStubManager::GetTarget(
        pInterfaceMT->GetLoaderAllocator()->GetDispatchToken(pInterfaceMT->GetTypeID(), pInterfaceMD->GetSlot()),
        this);
#endif
    pMD = MethodTable::GetMethodDescForSlotAddress(pTgt);

#ifdef _DEBUG
    MethodDesc *pDispSlotMD = FindDispatchSlotForInterfaceMD(ownerType, pInterfaceMD).GetMethodDesc();
    _ASSERTE(pDispSlotMD == pMD);
#endif // _DEBUG

    pMD->CheckRestore();

    return pMD;
}
#endif // DACCESS_COMPILE

//==========================================================================================
PTR_FieldDesc MethodTable::GetFieldDescByIndex(DWORD fieldIndex)
{
    LIMITED_METHOD_CONTRACT;

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

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)

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
    case SystemVClassificationTypeTypedReference:       return "TypedReference";
    default:                                            return "ERROR";
    }
};
#endif // _DEBUG && LOGGING

// Returns 'true' if the struct is passed in registers, 'false' otherwise.
bool MethodTable::ClassifyEightBytes(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel, unsigned int startOffsetOfStruct, bool useNativeLayout)
{
    if (useNativeLayout)
    {
        return ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel, startOffsetOfStruct, useNativeLayout);
    }
    else
    {
        return ClassifyEightBytesWithManagedLayout(helperPtr, nestingLevel, startOffsetOfStruct, useNativeLayout);
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

// Returns 'true' if the struct is passed in registers, 'false' otherwise.
bool MethodTable::ClassifyEightBytesWithManagedLayout(SystemVStructRegisterPassingHelperPtr helperPtr,
                                                     unsigned int nestingLevel, 
                                                     unsigned int startOffsetOfStruct,
                                                     bool useNativeLayout)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    WORD numIntroducedFields = GetNumIntroducedInstanceFields();

    // It appears the VM gives a struct with no fields of size 1.
    // Don't pass in register such structure.
    if (numIntroducedFields == 0)
    {
        return false;
    }

    // No struct register passing with explicit layout. There may be cases where explicit layout may be still
    // eligible for register struct passing, but it is hard to tell the real intent. Make it simple and just 
    // unconditionally disable register struct passing for explicit layout.
    if (GetClass()->HasExplicitFieldOffsetLayout())
    {
        LOG((LF_JIT, LL_EVERYTHING, "%*s**** ClassifyEightBytesWithManagedLayout: struct %s has explicit layout; will not be enregistered\n",
               nestingLevel * 5, "", this->GetDebugClassName()));
        return false;
    }
#ifdef _DEBUG
    LOG((LF_JIT, LL_EVERYTHING, "%*s**** Classify %s (%p), startOffset %d, total struct size %d\n",
        nestingLevel * 5, "", this->GetDebugClassName(), this, startOffsetOfStruct, helperPtr->structSize));
    int fieldNum = -1;
#endif // _DEBUG

    FieldDesc *pField = GetApproxFieldDescListRaw();
    FieldDesc *pFieldEnd = pField + numIntroducedFields;

    for (; pField < pFieldEnd; pField++)
    {
#ifdef _DEBUG
        ++fieldNum;
#endif // _DEBUG

        DWORD fieldOffset = pField->GetOffset();
        unsigned normalizedFieldOffset = fieldOffset + startOffsetOfStruct;

        unsigned int fieldSize = pField->GetSize();
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
            TypeHandle th = pField->GetApproxFieldTypeHandleThrowing();
            _ASSERTE(!th.IsNull());
            MethodTable* pFieldMT = th.GetMethodTable();

            bool inEmbeddedStructPrev = helperPtr->inEmbeddedStruct;
            helperPtr->inEmbeddedStruct = true;

            bool structRet = false;
            // If classifying for marshaling/PInvoke and the aggregated struct has a native layout
            // use the native classification. If not, continue using the managed layout.
            if (useNativeLayout && pFieldMT->HasLayout())
            {
                structRet = pFieldMT->ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel + 1, normalizedFieldOffset, useNativeLayout);
            }
            else
            {
                structRet = pFieldMT->ClassifyEightBytesWithManagedLayout(helperPtr, nestingLevel + 1, normalizedFieldOffset, useNativeLayout);
            }
            
            helperPtr->inEmbeddedStruct = inEmbeddedStructPrev;

            if (!structRet)
            {
                // If the nested struct says not to enregister, there's no need to continue analyzing at this level. Just return do not enregister.
                return false;
            }

            continue;
        }

        if (fieldClassificationType == SystemVClassificationTypeTypedReference || 
            CorInfoType2UnixAmd64Classification(GetClass_NoLogging()->GetInternalCorElementType()) == SystemVClassificationTypeTypedReference)
        {
            // The TypedReference is a very special type.
            // In source/metadata it has two fields - Type and Value and both are defined of type IntPtr.
            // When the VM creates a layout of the type it changes the type of the Value to ByRef type and the
            // type of the Type field is left to IntPtr (TYPE_I internally - native int type.)
            // This requires a special treatment of this type. The code below handles the both fields (and this entire type).

            for (unsigned i = 0; i < 2; i++)
            {
                fieldSize = 8;
                fieldOffset = (i == 0 ? 0 : 8);
                normalizedFieldOffset = fieldOffset + startOffsetOfStruct;
                fieldClassificationType = (i == 0 ? SystemVClassificationTypeIntegerByRef : SystemVClassificationTypeInteger);
                if ((normalizedFieldOffset % fieldSize) != 0)
                {
                    // The spec requires that struct values on the stack from register passed fields expects
                    // those fields to be at their natural alignment.

                    LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Field %d %s: offset %d (normalized %d), size %d not at natural alignment; not enregistering struct\n",
                        nestingLevel * 5, "", fieldNum, fieldNum, (i == 0 ? "Value" : "Type"), fieldOffset, normalizedFieldOffset, fieldSize));
                    return false;
                }

                helperPtr->largestFieldOffset = (int)normalizedFieldOffset;

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
                    nestingLevel * 5, "", fieldNum, (i == 0 ? "Value" : "Type"), fieldOffset, normalizedFieldOffset, fieldSize, helperPtr->currentUniqueOffsetField,
                    GetSystemVClassificationTypeName(fieldClassificationType),
                    GetSystemVClassificationTypeName(helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField])));

                helperPtr->currentUniqueOffsetField++;
#ifdef _DEBUG
                ++fieldNum;
#endif // _DEBUG
            }

            // Both fields of the special TypedReference struct are handled.
            pField = pFieldEnd;

            // Done classifying the System.TypedReference struct fields.
            continue;
        }

        if ((normalizedFieldOffset % fieldSize) != 0)
        {
            // The spec requires that struct values on the stack from register passed fields expects
            // those fields to be at their natural alignment.

            LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Field %d %s: offset %d (normalized %d), size %d not at natural alignment; not enregistering struct\n",
                   nestingLevel * 5, "", fieldNum, fieldNum, fieldName, fieldOffset, normalizedFieldOffset, fieldSize));
            return false;
        }

        if ((int)normalizedFieldOffset <= helperPtr->largestFieldOffset)
        {
            // Find the field corresponding to this offset and update the size if needed.
            // We assume that either it matches the offset of a previously seen field, or
            // it is an out-of-order offset (the VM does give us structs in non-increasing
            // offset order sometimes) that doesn't overlap any other field.

            // REVIEW: will the offset ever match a previously seen field offset for cases that are NOT ExplicitLayout?
            // If not, we can get rid of this loop, and just assume the offset is from an out-of-order field. We wouldn't
            // need to maintain largestFieldOffset, either, since we would then assume all fields are unique. We could
            // also get rid of ReClassifyField().
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
                           nestingLevel * 5, "", fieldNum, fieldName, fieldOffset, normalizedFieldOffset, fieldSize, i,
                           GetSystemVClassificationTypeName(fieldClassificationType),
                           GetSystemVClassificationTypeName(helperPtr->fieldClassifications[i])));

                    break;
                }
                // Make sure the field doesn't start in the middle of another field.
                _ASSERTE((normalizedFieldOffset <  helperPtr->fieldOffsets[i]) ||
                         (normalizedFieldOffset >= helperPtr->fieldOffsets[i] + helperPtr->fieldSizes[i]));
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
               nestingLevel * 5, "", fieldNum, fieldName, fieldOffset, normalizedFieldOffset, fieldSize, helperPtr->currentUniqueOffsetField,
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
                                                    bool useNativeLayout)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Should be in this method only doing a native layout classification.
    _ASSERTE(useNativeLayout);

#ifdef DACCESS_COMPILE
    // No register classification for this case.
    return false;
#else // DACCESS_COMPILE

    if (!HasLayout())
    {
        // If there is no native layout for this struct use the managed layout instead.
        return ClassifyEightBytesWithManagedLayout(helperPtr, nestingLevel, startOffsetOfStruct, useNativeLayout);
    }

    const FieldMarshaler *pFieldMarshaler = GetLayoutInfo()->GetFieldMarshalers();
    UINT  numIntroducedFields = GetLayoutInfo()->GetNumCTMFields();

    // No fields.
    if (numIntroducedFields == 0)
    {
        return false;
    }

    // No struct register passing with explicit layout. There may be cases where explicit layout may be still
    // eligible for register struct passing, but it is hard to tell the real intent. Make it simple and just 
    // unconditionally disable register struct passing for explicit layout.
    if (GetClass()->HasExplicitFieldOffsetLayout())
    {
        LOG((LF_JIT, LL_EVERYTHING, "%*s**** ClassifyEightBytesWithNativeLayout: struct %s has explicit layout; will not be enregistered\n",
            nestingLevel * 5, "", this->GetDebugClassName()));
        return false;
    }

#ifdef _DEBUG
    LOG((LF_JIT, LL_EVERYTHING, "%*s**** Classify for native struct %s (%p), startOffset %d, total struct size %d\n",
        nestingLevel * 5, "", this->GetDebugClassName(), this, startOffsetOfStruct, helperPtr->structSize));
    int fieldNum = -1;
#endif // _DEBUG

    while (numIntroducedFields--)
    {
#ifdef _DEBUG
        ++fieldNum;
#endif // _DEBUG

        FieldDesc *pField = pFieldMarshaler->GetFieldDesc();
        CorElementType fieldType = pField->GetFieldType();

        // Invalid field type.
        if (fieldType == ELEMENT_TYPE_END)
        {
            return false;
        }

        DWORD fieldOffset = pFieldMarshaler->GetExternalOffset();
        unsigned normalizedFieldOffset = fieldOffset + startOffsetOfStruct;

        unsigned int fieldNativeSize = pFieldMarshaler->NativeSize();
        if (fieldNativeSize > SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES)
        {
            // Pass on stack in this case.
            return false;
        }

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

        // Some NStruct Field Types have extra information and require special handling
        NStructFieldType cls = pFieldMarshaler->GetNStructFieldType();
        if (cls == NFT_FIXEDCHARARRAYANSI)
        {
            fieldClassificationType = SystemVClassificationTypeInteger;
        }
        else if (cls == NFT_FIXEDARRAY)
        {
            VARTYPE vtElement = ((FieldMarshaler_FixedArray*)pFieldMarshaler)->GetElementVT();
            switch (vtElement)
            {
            case VT_EMPTY:
            case VT_NULL:
            case VT_BOOL:
            case VT_I1:
            case VT_I2:
            case VT_I4:
            case VT_I8:
            case VT_UI1:
            case VT_UI2:
            case VT_UI4:
            case VT_UI8:
            case VT_PTR:
            case VT_INT:
            case VT_UINT:
            case VT_LPSTR:
            case VT_LPWSTR:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;
            case VT_R4:
            case VT_R8:
                fieldClassificationType = SystemVClassificationTypeSSE;
                break;
            case VT_DECIMAL:
            case VT_DATE:
            case VT_BSTR:
            case VT_UNKNOWN:
            case VT_DISPATCH:
            case VT_SAFEARRAY:
            case VT_ERROR:
            case VT_HRESULT:
            case VT_CARRAY:
            case VT_USERDEFINED:
            case VT_RECORD:
            case VT_FILETIME:
            case VT_BLOB:
            case VT_STREAM:
            case VT_STORAGE:
            case VT_STREAMED_OBJECT:
            case VT_STORED_OBJECT:
            case VT_BLOB_OBJECT:
            case VT_CF:
            case VT_CLSID:
            default:
                // Not supported.
                return false;
            }
        }
#ifdef FEATURE_COMINTEROP
        else if (cls == NFT_INTERFACE)
        {
            // COMInterop not supported for CORECLR.
            _ASSERTE(false && "COMInterop not supported for CORECLR.");
            return false;
        }
#ifdef FEATURE_CLASSIC_COMINTEROP
        else if (cls == NFT_SAFEARRAY)
        {
            // COMInterop not supported for CORECLR.
            _ASSERTE(false && "COMInterop not supported for CORECLR.");
            return false;
        }
#endif // FEATURE_CLASSIC_COMINTEROP
#endif // FEATURE_COMINTEROP
        else if (cls == NFT_NESTEDLAYOUTCLASS)
        {
            MethodTable* pFieldMT = ((FieldMarshaler_NestedLayoutClass*)pFieldMarshaler)->GetMethodTable();

            bool inEmbeddedStructPrev = helperPtr->inEmbeddedStruct;
            helperPtr->inEmbeddedStruct = true;
            bool structRet = pFieldMT->ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel + 1, normalizedFieldOffset, useNativeLayout);
            helperPtr->inEmbeddedStruct = inEmbeddedStructPrev;

            if (!structRet)
            {
                // If the nested struct says not to enregister, there's no need to continue analyzing at this level. Just return do not enregister.
                return false;
            }

            continue;
        }
        else if (cls == NFT_NESTEDVALUECLASS)
        {
            MethodTable* pFieldMT = ((FieldMarshaler_NestedValueClass*)pFieldMarshaler)->GetMethodTable();

            bool inEmbeddedStructPrev = helperPtr->inEmbeddedStruct;
            helperPtr->inEmbeddedStruct = true;
            bool structRet = pFieldMT->ClassifyEightBytesWithNativeLayout(helperPtr, nestingLevel + 1, normalizedFieldOffset, useNativeLayout);
            helperPtr->inEmbeddedStruct = inEmbeddedStructPrev;

            if (!structRet)
            {
                // If the nested struct says not to enregister, there's no need to continue analyzing at this level. Just return do not enregister.
                return false;
            }

            continue;
        }
        else if (cls == NFT_COPY1)
        {
            // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy1. 
            switch (fieldType)
            {
            case ELEMENT_TYPE_I1:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;

            case ELEMENT_TYPE_U1:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;

            default:
                // Invalid entry.
                return false; // Pass on stack.
            }
        }
        else if (cls == NFT_COPY2)
        {
            // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy2. 
            switch (fieldType)
            {
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;

            default:
                // Invalid entry.
                return false; // Pass on stack.
            }
        }
        else if (cls == NFT_COPY4)
        {
            // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy4. 
            switch (fieldType)
            {
                // At this point, ELEMENT_TYPE_I must be 4 bytes long.  Same for ELEMENT_TYPE_U.
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_PTR:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;

            case ELEMENT_TYPE_R4:
                fieldClassificationType = SystemVClassificationTypeSSE;
                break;

            default:
                // Invalid entry.
                return false; // Pass on stack.
            }
        }
        else if (cls == NFT_COPY8)
        {
            // The following CorElementTypes are the only ones handled with FieldMarshaler_Copy8. 
            switch (fieldType)
            {
                // At this point, ELEMENT_TYPE_I must be 8 bytes long.  Same for ELEMENT_TYPE_U.
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_PTR:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;

            case ELEMENT_TYPE_R8:
                fieldClassificationType = SystemVClassificationTypeSSE;
                break;

            default:
                // Invalid entry.
                return false; // Pass on stack.
            }
        }
        else if (cls == NFT_FIXEDSTRINGUNI)
        {
            fieldClassificationType = SystemVClassificationTypeInteger;
        }
        else if (cls == NFT_FIXEDSTRINGANSI)
        {
            fieldClassificationType = SystemVClassificationTypeInteger;
        }
        else
        {
            // All other NStruct Field Types which do not require special handling.
            switch (cls)
            {
#ifdef FEATURE_COMINTEROP
            case NFT_BSTR:
            case NFT_HSTRING:
            case NFT_VARIANT:
            case NFT_VARIANTBOOL:
            case NFT_CURRENCY:
                // COMInterop not supported for CORECLR.
                _ASSERTE(false && "COMInterop not supported for CORECLR.");
                return false;
#endif  // FEATURE_COMINTEROP
            case NFT_STRINGUNI:
            case NFT_STRINGANSI:
            case NFT_ANSICHAR:
            case NFT_WINBOOL:
            case NFT_CBOOL:
            case NFT_DELEGATE:
            case NFT_SAFEHANDLE:
            case NFT_CRITICALHANDLE:
                fieldClassificationType = SystemVClassificationTypeInteger;
                break;

            // It's not clear what the right behavior for NTF_DECIMAL and NTF_DATE is
            // But those two types would only make sense on windows. We can revisit this later
            case NFT_DECIMAL:
            case NFT_DATE:
            case NFT_ILLEGAL:
            default:
                return false;
            }
        }

        if ((normalizedFieldOffset % fieldNativeSize) != 0)
        {
            // The spec requires that struct values on the stack from register passed fields expects
            // those fields to be at their natural alignment.

            LOG((LF_JIT, LL_EVERYTHING, "     %*sxxxx Native Field %d %s: offset %d (normalized %d), native size %d not at natural alignment; not enregistering struct\n",
                nestingLevel * 5, "", fieldNum, fieldNum, fieldName, fieldOffset, normalizedFieldOffset, fieldNativeSize));
            return false;
        }

        if ((int)normalizedFieldOffset <= helperPtr->largestFieldOffset)
        {
            // Find the field corresponding to this offset and update the size if needed.
            // We assume that either it matches the offset of a previously seen field, or
            // it is an out-of-order offset (the VM does give us structs in non-increasing
            // offset order sometimes) that doesn't overlap any other field.

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
                        nestingLevel * 5, "", fieldNum, fieldName, fieldOffset, normalizedFieldOffset, fieldNativeSize, i,
                        GetSystemVClassificationTypeName(fieldClassificationType),
                        GetSystemVClassificationTypeName(helperPtr->fieldClassifications[i])));

                    break;
                }
                // Make sure the field doesn't start in the middle of another field.
                _ASSERTE((normalizedFieldOffset <  helperPtr->fieldOffsets[i]) ||
                    (normalizedFieldOffset >= helperPtr->fieldOffsets[i] + helperPtr->fieldSizes[i]));
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
            nestingLevel * 5, "", fieldNum, fieldName, fieldOffset, normalizedFieldOffset, fieldNativeSize, helperPtr->currentUniqueOffsetField,
            GetSystemVClassificationTypeName(fieldClassificationType),
            GetSystemVClassificationTypeName(helperPtr->fieldClassifications[helperPtr->currentUniqueOffsetField])));

        _ASSERTE(helperPtr->currentUniqueOffsetField < SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT);
        helperPtr->currentUniqueOffsetField++;
        ((BYTE*&)pFieldMarshaler) += MAXFIELDMARSHALERSIZE;
    } // end per-field for loop

    AssignClassifiedEightByteTypes(helperPtr, nestingLevel);

    return true;
#endif // DACCESS_COMPILE
}

// Assigns the classification types to the array with eightbyte types.
void  MethodTable::AssignClassifiedEightByteTypes(SystemVStructRegisterPassingHelperPtr helperPtr, unsigned int nestingLevel)
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
        unsigned int accumulatedSizeForEightByte = 0;
        unsigned int currentEightByteOffset = 0;
        unsigned int currentEightByte = 0;

        int lastFieldOrdinal = sortedFieldOrder[largestFieldOffset];
        unsigned int offsetAfterLastFieldByte = largestFieldOffset + helperPtr->fieldSizes[lastFieldOrdinal];
        SystemVClassificationType lastFieldClassification = helperPtr->fieldClassifications[lastFieldOrdinal];

        unsigned offset = 0;
        for (unsigned fieldSize = 0; offset < helperPtr->structSize; offset += fieldSize)
        {
            SystemVClassificationType fieldClassificationType;

            int ordinal = sortedFieldOrder[offset];
            if (ordinal == -1)
            {
                // If there is no field that starts as this offset, treat its contents as padding.
                // Any padding that follows the last field receives the same classification as the
                // last field; padding between fields receives the NO_CLASS classification as per
                // the SysV ABI spec.
                fieldSize = 1;
                fieldClassificationType = offset < offsetAfterLastFieldByte ? SystemVClassificationTypeNoClass : lastFieldClassification;
            }
            else
            {
                fieldSize = helperPtr->fieldSizes[ordinal];
                _ASSERTE(fieldSize > 0 && fieldSize <= SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES);

                fieldClassificationType = helperPtr->fieldClassifications[ordinal];
                _ASSERTE(fieldClassificationType != SystemVClassificationTypeMemory && fieldClassificationType != SystemVClassificationTypeUnknown);
            }

            if (helperPtr->eightByteClassifications[currentEightByte] == fieldClassificationType)
            {
                // Do nothing. The eight-byte already has this classification.
            }
            else if (helperPtr->eightByteClassifications[currentEightByte] == SystemVClassificationTypeNoClass)
            {
                helperPtr->eightByteClassifications[currentEightByte] = fieldClassificationType;
            }
            else if ((helperPtr->eightByteClassifications[currentEightByte] == SystemVClassificationTypeInteger) ||
                (fieldClassificationType == SystemVClassificationTypeInteger))
            {
                _ASSERTE((fieldClassificationType != SystemVClassificationTypeIntegerReference) && 
                    (fieldClassificationType != SystemVClassificationTypeIntegerByRef));

                helperPtr->eightByteClassifications[currentEightByte] = SystemVClassificationTypeInteger;
            }
            else if ((helperPtr->eightByteClassifications[currentEightByte] == SystemVClassificationTypeIntegerReference) ||
                (fieldClassificationType == SystemVClassificationTypeIntegerReference))
            {
                helperPtr->eightByteClassifications[currentEightByte] = SystemVClassificationTypeIntegerReference;
            }
            else if ((helperPtr->eightByteClassifications[currentEightByte] == SystemVClassificationTypeIntegerByRef) ||
                (fieldClassificationType == SystemVClassificationTypeIntegerByRef))
            {
                helperPtr->eightByteClassifications[currentEightByte] = SystemVClassificationTypeIntegerByRef;
            }
            else
            {
                helperPtr->eightByteClassifications[currentEightByte] = SystemVClassificationTypeSSE;
            }

            accumulatedSizeForEightByte += fieldSize;
            if (accumulatedSizeForEightByte == SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES)
            {
                // Save data for this eightbyte.
                helperPtr->eightByteSizes[currentEightByte] = SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
                helperPtr->eightByteOffsets[currentEightByte] = currentEightByteOffset;

                // Set up for next eightbyte.
                currentEightByte++;
                _ASSERTE(currentEightByte <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

                currentEightByteOffset = offset + fieldSize;
                accumulatedSizeForEightByte = 0;
            }

            _ASSERTE(accumulatedSizeForEightByte < SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES);
        }

        // Handle structs that end in the middle of an eightbyte.
        if (accumulatedSizeForEightByte > 0 && accumulatedSizeForEightByte < SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES)
        {
            _ASSERTE((helperPtr->structSize % SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES) != 0);

            helperPtr->eightByteSizes[currentEightByte] = accumulatedSizeForEightByte;
            helperPtr->eightByteOffsets[currentEightByte] = currentEightByteOffset;
            currentEightByte++;
        }

        helperPtr->eightByteCount = currentEightByte;

        _ASSERTE(helperPtr->eightByteCount <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

#ifdef _DEBUG
        LOG((LF_JIT, LL_EVERYTHING, "     ----\n"));
        LOG((LF_JIT, LL_EVERYTHING, "     **** Number EightBytes: %d\n", helperPtr->eightByteCount));
        for (unsigned i = 0; i < helperPtr->eightByteCount; i++)
        {
            LOG((LF_JIT, LL_EVERYTHING, "     **** eightByte %d -- classType: %s, eightByteOffset: %d, eightByteSize: %d\n",
                i, GetSystemVClassificationTypeName(helperPtr->eightByteClassifications[i]), helperPtr->eightByteOffsets[i], helperPtr->eightByteSizes[i]));
        }
#endif // _DEBUG
    }
}

#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
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

    // In ngened case, we have cached array with boxed statics MTs. In JITed case, we have just the FieldDescs
    ClassCtorInfoEntry *pClassCtorInfoEntry = GetClassCtorInfoIfExists();
    if (pClassCtorInfoEntry != NULL)
    {
        OBJECTREF* pStaticSlots = (OBJECTREF*)(pStaticBase + pClassCtorInfoEntry->firstBoxedStaticOffset);
        GCPROTECT_BEGININTERIOR(pStaticSlots);

        ArrayDPTR(FixupPointer<PTR_MethodTable>) ppMTs = GetLoaderModule()->GetZapModuleCtorInfo()->
            GetGCStaticMTs(pClassCtorInfoEntry->firstBoxedStaticMTIndex);

        DWORD numBoxedStatics = pClassCtorInfoEntry->numBoxedStatics;
        for (DWORD i = 0; i < numBoxedStatics; i++)
        {
#ifdef FEATURE_PREJIT
            Module::RestoreMethodTablePointer(&(ppMTs[i]), GetLoaderModule());
#endif
            MethodTable *pFieldMT = ppMTs[i].GetValue();

            _ASSERTE(pFieldMT);

            LOG((LF_CLASSLOADER, LL_INFO10000, "\tInstantiating static of type %s\n", pFieldMT->GetDebugClassName()));
            OBJECTREF obj = AllocateStaticBox(pFieldMT, pClassCtorInfoEntry->hasFixedAddressVTStatics);

            SetObjectReference( &(pStaticSlots[i]), obj, GetAppDomain() );
        }
        GCPROTECT_END();
    }
    else
    {
        // We should never take this codepath in zapped images.
        _ASSERTE(!IsZapped());

        FieldDesc *pField = HasGenericsStaticsInfo() ? 
            GetGenericsStaticFieldDescs() : (GetApproxFieldDescListRaw() + GetNumIntroducedInstanceFields());
        FieldDesc *pFieldEnd = pField + GetNumStaticFields();

        while (pField < pFieldEnd)
        {
            _ASSERTE(pField->IsStatic());

            if (!pField->IsSpecialStatic() && pField->IsByValue())
            {
                TypeHandle  th = pField->GetFieldTypeHandleThrowing();
                MethodTable* pFieldMT = th.GetMethodTable();

                LOG((LF_CLASSLOADER, LL_INFO10000, "\tInstantiating static of type %s\n", pFieldMT->GetDebugClassName()));
                OBJECTREF obj = AllocateStaticBox(pFieldMT, HasFixedAddressVTStatics());

                SetObjectReference( (OBJECTREF*)(pStaticBase + pField->GetOffset()), obj, GetAppDomain() );
            }

            pField++;
        }
    }
    GCPROTECT_END();
}

//==========================================================================================
OBJECTREF MethodTable::AllocateStaticBox(MethodTable* pFieldMT, BOOL fPinned, OBJECTHANDLE* pHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        CONTRACTL_END;
    }

    _ASSERTE(pFieldMT->IsValueType());

    // Activate any dependent modules if necessary
    pFieldMT->EnsureInstanceActive();

    OBJECTREF obj = AllocateObject(pFieldMT);

    // Pin the object if necessary
    if (fPinned)
    {
        LOG((LF_CLASSLOADER, LL_INFO10000, "\tSTATICS:Pinning static (VT fixed address attribute) of type %s\n", pFieldMT->GetDebugClassName()));
        OBJECTHANDLE oh = GetAppDomain()->CreatePinningHandle(obj);
        if (pHandle)
        {
            *pHandle = oh;
        }
    }
    else
    {
        if (pHandle)
        {
            *pHandle = NULL;
        }
    }

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

        STRESS_LOG1(LF_CLASSLOADER, LL_INFO1000, "RunClassInit: Calling class contructor for type %pT\n", this);

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

        STRESS_LOG1(LF_CLASSLOADER, LL_INFO100000, "RunClassInit: Returned Successfully from class contructor for type %pT\n", this);

        fRet = TRUE;
    }
    EX_CATCH
    {
        // Exception set by parent
        // <TODO>@TODO: We should make this an ExceptionInInitializerError if the exception thrown is not
        // a subclass of Error</TODO>
        *pThrowable = GET_THROWABLE();
        _ASSERTE(fRet == FALSE);

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // If active thread state does not have a CorruptionSeverity set for the exception,
        // then set one up based upon the current exception code and/or the throwable.
        //
        // When can we be here and current exception tracker may not have corruption severity set?
        // Incase of SO in managed code, SO is never seen by CLR's exception handler for managed code
        // and if this happens in cctor, we can end up here without the corruption severity set.
        Thread *pThread = GetThread();
        _ASSERTE(pThread != NULL);
        ThreadExceptionState *pCurTES = pThread->GetExceptionState();
        _ASSERTE(pCurTES != NULL);
        if (pCurTES->GetLastActiveExceptionCorruptionSeverity() == NotSet)
        {
            if (CEHelper::IsProcessCorruptedStateException(GetCurrentExceptionCode()) ||
                CEHelper::IsProcessCorruptedStateException(*pThrowable))
            {
                // Process Corrupting
                pCurTES->SetLastActiveExceptionCorruptionSeverity(ProcessCorrupting);
                LOG((LF_EH, LL_INFO100, "MethodTable::RunClassInitEx - Exception treated as ProcessCorrupting.\n"));
            }
            else
            {
                // Not Corrupting
                pCurTES->SetLastActiveExceptionCorruptionSeverity(NotCorrupting);
                LOG((LF_EH, LL_INFO100, "MethodTable::RunClassInitEx - Exception treated as non-corrupting.\n"));
            }
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "MethodTable::RunClassInitEx - Exception already has corruption severity set.\n"));
        }
#endif // FEATURE_CORRUPTING_EXCEPTIONS
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
        SO_TOLERANT;
    }
    CONTRACTL_END;

    GCX_COOP();

    // This is a fairly aggressive policy. Merely asking that the class be initialized is grounds for kicking you out.
    // Alternately, we could simply NOP out the class initialization. Since the aggressive policy is also the more secure
    // policy, keep this unless it proves intractable to remove all premature classinits in the system.
    EnsureActive();

    Thread *pThread;
    pThread = GetThread();
    _ASSERTE(pThread);
    INTERIOR_STACK_PROBE_FOR(pThread, 8);

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
        _ASSERTE(pEntry->m_pLoaderAllocator == (GetDomain()->IsSharedDomain() ? pDomain->GetLoaderAllocator() : GetLoaderAllocator()));

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

        if (MscorlibBinder::GetException(kTypeInitializationException) != gc.pInitException->GetMethodTable())
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
                    pEntry->m_pLoaderAllocator->ClearHandle(hNewInitException);
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

        // <FEATURE_CORRUPTING_EXCEPTIONS>
        // Specify the corruption severity to be used to raise this exception in COMPlusThrow below.
        // This will ensure that when the exception is seen by the managed code personality routine, 
        // it will setup the correct corruption severity in the exception tracker.
        // </FEATURE_CORRUPTING_EXCEPTIONS>

        COMPlusThrow(gc.pThrowable
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
            , pEntry->m_CorruptionSeverity
#endif // FEATURE_CORRUPTING_EXCEPTIONS
            );
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
                if (!NingenEnabled())
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
    
                            _ASSERTE(g_pThreadAbortExceptionClass == MscorlibBinder::GetException(kThreadAbortException));
    
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
    
                            pEntry->m_pLoaderAllocator = GetDomain()->IsSharedDomain() ? pDomain->GetLoaderAllocator() : GetLoaderAllocator();
    
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
    
    #ifdef FEATURE_CORRUPTING_EXCEPTIONS
                            // Save the corruption severity of the exception so that if the type system
                            // attempts to pick it up from its cache list and throw again, it should
                            // treat the exception as corrupting, if applicable.
                            pEntry->m_CorruptionSeverity = pThread->GetExceptionState()->GetLastActiveExceptionCorruptionSeverity();
                            
                            // We should be having a valid corruption severity at this point
                            _ASSERTE(pEntry->m_CorruptionSeverity != NotSet);
    #endif // FEATURE_CORRUPTING_EXCEPTIONS
    
                            COMPlusThrow(gc.pThrowable
    #ifdef FEATURE_CORRUPTING_EXCEPTIONS
                                , pEntry->m_CorruptionSeverity
    #endif // FEATURE_CORRUPTING_EXCEPTIONS
                                );
                        }
    
                        GCPROTECT_END();
                    }
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
                    _ASSERTE(pEntry->m_pLoaderAllocator == (GetDomain()->IsSharedDomain() ? pDomain->GetLoaderAllocator() : GetLoaderAllocator()));
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

    g_IBCLogger.LogMethodTableAccess(this);
Exit:
    ;
    END_INTERIOR_STACK_PROBE;
}

//==========================================================================================
void MethodTable::CheckRunClassInitThrowing()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_TOLERANT;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsFullyLoaded());
    }
    CONTRACTL_END;

    {   // Debug-only code causes SO volation, so add exception.
        CONTRACT_VIOLATION(SOToleranceViolation);
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
        SO_TOLERANT;
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

    if (ContainsStackPtr())
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
    CopyValueClass(ref->UnBox(), *data, this, ref->GetAppDomain());
    return ref;
}

#if _TARGET_X86_ || _TARGET_AMD64_
//==========================================================================================
static void FastCallFinalize(Object *obj, PCODE funcPtr, BOOL fCriticalCall)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_SO_INTOLERANT;

    BEGIN_CALL_TO_MANAGEDEX(fCriticalCall ? EEToManagedCriticalCall : EEToManagedDefault);

#if defined(_TARGET_X86_)    

    __asm
    {
        mov     ecx, [obj]
        call    [funcPtr]
        INDEBUG(nop)            // Mark the fact that we can call managed code
    }

#else // _TARGET_X86_

    FastCallFinalizeWorker(obj, funcPtr);

#endif // _TARGET_X86_

    END_CALL_TO_MANAGED();
}

#endif // _TARGET_X86_ || _TARGET_AMD64_

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
        refThis->SetDelegate(NULL);

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
            if (GetThread() != thread)
            {
                refThis->ClearInternal();
            }

            FastInterlockOr ((ULONG *)&thread->m_State, Thread::TS_Finalized);
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
        PRECONDITION(obj->GetMethodTable()->HasFinalizer() ||
                     obj->GetMethodTable()->IsTransparentProxy());
    }
    CONTRACTL_END;

    // Never call any finalizers under ngen for determinism
    if (IsCompilationProcess())
    {
        return;
    }

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

#ifdef FEATURE_CAS_POLICY
    // Notify the host to setup the restricted context before finalizing each object
    HostExecutionContextManager::SetHostRestrictedContext();
#endif // FEATURE_CAS_POLICY

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
        STRESS_LOG2(LF_GCALLOC, LL_INFO100, "Finalizing CriticalFinalizer %pM in domain %d\n", 
                    pMT, GetAppDomain()->GetId().m_dwId);
    }
#endif

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerTraceCall())
        g_pDebugInterface->TraceCall((const BYTE *) funcPtr);
#endif // DEBUGGING_SUPPORTED

    FastCallFinalize(obj, funcPtr, fCriticalFinalizer);

#else // defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

    PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(funcPtr);

    DECLARE_ARGHOLDER_ARRAY(args, 1);

    args[ARGNUM_0] = PTR_TO_ARGHOLDER(obj);

    if (fCriticalFinalizer)
    {
        CRITICAL_CALLSITE;
    }

    CALL_MANAGED_METHOD_NORET(args);

#endif // (defined(_TARGET_X86_) && defined(_TARGET_AMD64_)

#ifdef STRESS_LOG
    if (fCriticalFinalizer)
    {
        STRESS_LOG2(LF_GCALLOC, LL_INFO100, "Finalized CriticalFinalizer %pM in domain %d without exception\n", 
                    pMT, GetAppDomain()->GetId().m_dwId);
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
        PRECONDITION(!IsTransparentProxy() && !IsArray());      // Arrays and remoted objects can't go through this path.  
        POSTCONDITION(GetWriteableData()->m_hExposedClassObject != 0);
        //REENTRANT
    }
    CONTRACT_END;

#ifdef _DEBUG
    // Force a GC here because GetManagedClassObject could trigger GC nondeterminsticaly
    GCStress<cfg_any, PulseGcTriggerPolicy>::MaybeTrigger();
#endif // _DEBUG

    if (GetWriteableData()->m_hExposedClassObject == NULL)
    {
        // Make sure that we have been restored
        CheckRestore();

        if (IsTransparentProxy())       // Extra protection in a retail build against doing this on a transparent proxy. 
            return NULL;

        REFLECTCLASSBASEREF  refClass = NULL;
        GCPROTECT_BEGIN(refClass);
        if (GetAssembly()->IsIntrospectionOnly())
            refClass = (REFLECTCLASSBASEREF) AllocateObject(MscorlibBinder::GetClass(CLASS__CLASS_INTROSPECTION_ONLY));
        else
            refClass = (REFLECTCLASSBASEREF) AllocateObject(g_pRuntimeTypeClass);

        LoaderAllocator *pLoaderAllocator = GetLoaderAllocator();

        ((ReflectClassBaseObject*)OBJECTREFToObject(refClass))->SetType(TypeHandle(this));
        ((ReflectClassBaseObject*)OBJECTREFToObject(refClass))->SetKeepAlive(pLoaderAllocator->GetExposedObject());

        // Let all threads fight over who wins using InterlockedCompareExchange.
        // Only the winner can set m_ExposedClassObject from NULL.
        LOADERHANDLE exposedClassObjectHandle = pLoaderAllocator->AllocateHandle(refClass);

        if (FastInterlockCompareExchangePointer(&(EnsureWritablePages(GetWriteableDataForWrite())->m_hExposedClassObject), exposedClassObjectHandle, static_cast<LOADERHANDLE>(NULL)))
        {
            pLoaderAllocator->ClearHandle(exposedClassObjectHandle);
        }

        GCPROTECT_END();
    }
    RETURN(GetManagedClassObjectIfExists());
}

#endif //!DACCESS_COMPILE && !CROSSGEN_COMPILE

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

    if (ContainsPointersOrCollectible())
        start = dac_cast<TADDR>(this) - CGCDesc::GetCGCDescFromMT(this)->GetSize();
    else
        start = dac_cast<TADDR>(this);

    TADDR end = dac_cast<TADDR>(this) + GetEndOffsetOfOptionalMembers();

    _ASSERTE(start && end && (start < end));
    *pStart = start;
    *pEnd = end;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

#ifndef DACCESS_COMPILE

BOOL MethodTable::CanInternVtableChunk(DataImage *image, VtableIndirectionSlotIterator it)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(IsCompilationProcess());

    BOOL canBeSharedWith = TRUE;

    // We allow full sharing except that which would break MethodTable::Fixup -- when the slots are Fixup'd
    // we need to ensure that regardless of who is doing the Fixup the same target is decided on.
    // Note that if this requirement is not met, an assert will fire in ZapStoredStructure::Save

    if (GetFlag(enum_flag_NotInPZM))
    {
        canBeSharedWith = FALSE;
    }

    if (canBeSharedWith)
    {
        for (DWORD slotNumber = it.GetStartSlot(); slotNumber < it.GetEndSlot(); slotNumber++)
        {
            MethodDesc *pMD = GetMethodDescForSlot(slotNumber);
            _ASSERTE(pMD != NULL);
            pMD->CheckRestore();

            if (!image->CanEagerBindToMethodDesc(pMD))
            {
                canBeSharedWith = FALSE;
                break;
            }
        }
    }

    return canBeSharedWith;
}

//==========================================================================================
void MethodTable::PrepopulateDictionary(DataImage * image, BOOL nonExpansive)
{
     STANDARD_VM_CONTRACT;

     if (GetDictionary())
     {
         // We can only save elements of the dictionary if we are sure of its
         // layout, which means we must be either tightly-knit to the EEClass
         // (i.e. be the owner of the EEClass) or else we can hard-bind to the EEClass.
         // There's no point in prepopulating the dictionary if we can't save the entries.
         //
         // This corresponds to the canSaveSlots which we pass to the Dictionary::Fixup

         if (!IsCanonicalMethodTable() && image->CanEagerBindToMethodTable(GetCanonicalMethodTable()))
         {
             LOG((LF_JIT, LL_INFO10000, "GENERICS: Prepopulating dictionary for MT %s\n",  GetDebugClassName()));
             GetDictionary()->PrepopulateDictionary(NULL, this, nonExpansive);
         }
     }
}

//==========================================================================================
void ModuleCtorInfo::AddElement(MethodTable *pMethodTable)
{
    STANDARD_VM_CONTRACT;

    // Get the values for the new entry before we update the
    // cache in the Module

    // Expand the table if needed.  No lock is needed because this is at NGEN time
    if (numElements >= numLastAllocated)
    {
        _ASSERTE(numElements == numLastAllocated);

        MethodTable ** ppOldMTEntries = ppMT;

#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable:22011) // Suppress PREFast warning about integer overflows or underflows
#endif // _PREFAST_
        DWORD numNewAllocated = max(2 * numLastAllocated, MODULE_CTOR_ELEMENTS);
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif // _PREFAST_

        ppMT = new MethodTable* [numNewAllocated];

        _ASSERTE(ppMT);

        memcpy(ppMT, ppOldMTEntries, sizeof(MethodTable *) * numLastAllocated);
        memset(ppMT + numLastAllocated, 0, sizeof(MethodTable *) * (numNewAllocated - numLastAllocated));

        delete[] ppOldMTEntries;

        numLastAllocated = numNewAllocated;
    }

    // Assign the new entry
    //
    // Note the use of two "parallel" arrays.  We do this to keep the workingset smaller since we
    // often search (in GetClassCtorInfoIfExists) for a methodtable pointer but never actually find it.

    ppMT[numElements] = pMethodTable;
    numElements++;
}

//==========================================================================================
void MethodTable::Save(DataImage *image, DWORD profilingFlags)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(IsRestored_NoLogging());
        PRECONDITION(IsFullyLoaded());
        PRECONDITION(image->GetModule()->GetAssembly() ==
                     GetAppDomain()->ToCompilationDomain()->GetTargetAssembly());
    } CONTRACTL_END;

    LOG((LF_ZAP, LL_INFO10000, "MethodTable::Save %s (%p)\n", GetDebugClassName(), this));

    // Be careful about calling DictionaryLayout::Trim - strict conditions apply.
    // See note on that method.
    if (GetDictionary() &&
        GetClass()->GetDictionaryLayout() &&
        image->CanEagerBindToMethodTable(GetCanonicalMethodTable()))
    {
        GetClass()->GetDictionaryLayout()->Trim();
    }

    // Set the "restore" flags. They may not have been set yet.
    // We don't need the return value of this call.
    NeedsRestore(image);

    //check if this is actually in the PZM
    if (Module::GetPreferredZapModuleForMethodTable(this) != GetLoaderModule())
    {
        _ASSERTE(!IsStringOrArray());
        SetFlag(enum_flag_NotInPZM);
    }

    // Set the IsStructMarshallable Bit
    if (::IsStructMarshalable(this))
    {
        SetStructMarshalable();
    }

    TADDR start, end;

    GetSavedExtent(&start, &end);

#ifdef FEATURE_COMINTEROP
    if (HasGuidInfo())
    {
        // Make sure our GUID is computed

        // Generic WinRT types can have their GUID computed only if the instantiation is WinRT-legal
        if (IsLegalNonArrayWinRTType())
        {
            GUID dummy;
            if (SUCCEEDED(GetGuidNoThrow(&dummy, TRUE, FALSE)))
            {
                GuidInfo* pGuidInfo = GetGuidInfo();
                _ASSERTE(pGuidInfo != NULL);

                image->StoreStructure(pGuidInfo,
                                      sizeof(GuidInfo),
                                      DataImage::ITEM_GUID_INFO);

                Module *pModule = GetModule();
                if (pModule->CanCacheWinRTTypeByGuid(this))
                {
                    pModule->CacheWinRTTypeByGuid(this, pGuidInfo);
                }
            }
            else
            {
                GuidInfo** ppGuidInfo = GetGuidInfoPtr();
                *ppGuidInfo = NULL;
            }
        }
    }
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_REMOTING
    if (HasRemotableMethodInfo())
    {
        if (GetNumMethods() > 0)
        {
            // The CrossDomainOptimizationInfo was populated earlier in Module::PrepareTypesForSave
            CrossDomainOptimizationInfo* pRMI = GetRemotableMethodInfo();
            SIZE_T sizeToBeSaved = CrossDomainOptimizationInfo::SizeOf(this);
            image->StoreStructure(pRMI, sizeToBeSaved,
                                  DataImage::ITEM_CROSS_DOMAIN_INFO);
        }
    }

    // Store any optional VTS (Version Tolerant Serialization) info.
    if (HasRemotingVtsInfo())
        image->StoreStructure(GetRemotingVtsInfo(),
                              RemotingVtsInfo::GetSize(GetNumIntroducedInstanceFields()),
                              DataImage::ITEM_VTS_INFO);
#endif //FEATURE_REMOTING

#ifdef _DEBUG
    if (GetDebugClassName() != NULL && !image->IsStored(GetDebugClassName()))
        image->StoreStructure(debug_m_szClassName, (ULONG)(strlen(GetDebugClassName())+1),
                              DataImage::ITEM_DEBUG,
                              1);
#endif // _DEBUG

    DataImage::ItemKind kindBasic    = DataImage::ITEM_METHOD_TABLE;
    if (IsWriteable())
        kindBasic = DataImage::ITEM_METHOD_TABLE_SPECIAL_WRITEABLE;

    ZapStoredStructure * pMTNode = image->StoreStructure((void*) start, (ULONG)(end - start), kindBasic);

    if ((void *)this != (void *)start)
        image->BindPointer(this, pMTNode, (BYTE *)this - (BYTE *)start);

    // Store the vtable chunks
    VtableIndirectionSlotIterator it = IterateVtableIndirectionSlots();
    while (it.Next())
    {
        if (!image->IsStored(it.GetIndirectionSlot()))
        {
            if (CanInternVtableChunk(image, it))
                image->StoreInternedStructure(it.GetIndirectionSlot(), it.GetSize(), DataImage::ITEM_VTABLE_CHUNK);
            else
                image->StoreStructure(it.GetIndirectionSlot(), it.GetSize(), DataImage::ITEM_VTABLE_CHUNK);
        }
        else
        {
            // Tell the interning system that we have already shared this structure without its help
            image->NoteReusedStructure(it.GetIndirectionSlot());
        }
    }

    if (HasNonVirtualSlotsArray())
    {
        image->StoreStructure(GetNonVirtualSlotsArray(), GetNonVirtualSlotsArraySize(), DataImage::ITEM_VTABLE_CHUNK);
    }

    if (HasInterfaceMap())
    {
#ifdef FEATURE_COMINTEROP
        // Dynamic interface maps have an additional DWORD_PTR preceding the InterfaceInfo_t array
        if (HasDynamicInterfaceMap())
        {
            ZapStoredStructure * pInterfaceMapNode = image->StoreInternedStructure(((DWORD_PTR *)GetInterfaceMap()) - 1, 
                                                                                   GetInterfaceMapSize(), 
                                                                                   DataImage::ITEM_INTERFACE_MAP);
                                                                                   
            image->BindPointer(GetInterfaceMap(), pInterfaceMapNode, sizeof(DWORD_PTR));
        }
        else
#endif // FEATURE_COMINTEROP
        {
            image->StoreInternedStructure(GetInterfaceMap(), GetInterfaceMapSize(), DataImage::ITEM_INTERFACE_MAP);
        }

        SaveExtraInterfaceInfo(image);
    }

    // If we have a dispatch map, save it.
    if (HasDispatchMapSlot())
    {
        GetDispatchMap()->Save(image);
    }

    if (HasPerInstInfo())
    {
        ZapStoredStructure * pPerInstInfoNode;
        if (CanEagerBindToParentDictionaries(image, NULL))
        {
            pPerInstInfoNode = image->StoreInternedStructure((BYTE *)GetPerInstInfo() - sizeof(GenericsDictInfo), GetPerInstInfoSize() + sizeof(GenericsDictInfo), DataImage::ITEM_DICTIONARY);
        }
        else
        {
            pPerInstInfoNode = image->StoreStructure((BYTE *)GetPerInstInfo() - sizeof(GenericsDictInfo), GetPerInstInfoSize() + sizeof(GenericsDictInfo), DataImage::ITEM_DICTIONARY_WRITEABLE);
        }
        image->BindPointer(GetPerInstInfo(), pPerInstInfoNode, sizeof(GenericsDictInfo));
    }

    Dictionary * pDictionary = GetDictionary();
    if (pDictionary != NULL)
    {
        BOOL fIsWriteable;

        if (!IsCanonicalMethodTable())
        {
            // CanEagerBindToMethodTable would not work for targeted patching here. The dictionary
            // layout is sensitive to compilation order that can be changed by TP compatible changes.
            BOOL canSaveSlots = (image->GetModule() == GetCanonicalMethodTable()->GetLoaderModule());

            fIsWriteable = pDictionary->IsWriteable(image, canSaveSlots,
                                       GetNumGenericArgs(),
                                       GetModule(),
                                       GetClass()->GetDictionaryLayout());
        }
        else
        {
            fIsWriteable = FALSE;
        }


        if (!fIsWriteable)
        {
            image->StoreInternedStructure(pDictionary, GetInstAndDictSize(), DataImage::ITEM_DICTIONARY);
        }
        else
        {
            image->StoreStructure(pDictionary, GetInstAndDictSize(), DataImage::ITEM_DICTIONARY_WRITEABLE);
        }
    }

    WORD numStaticFields = GetClass()->GetNumStaticFields();

    if (!IsCanonicalMethodTable() && HasGenericsStaticsInfo() && numStaticFields != 0)
    {
        FieldDesc * pGenericsFieldDescs = GetGenericsStaticFieldDescs();

        for (DWORD i = 0; i < numStaticFields; i++)
        {
            FieldDesc *pFld = pGenericsFieldDescs + i;
            pFld->PrecomputeNameHash();
        }

        ZapStoredStructure * pFDNode = image->StoreStructure(pGenericsFieldDescs, sizeof(FieldDesc) * numStaticFields,
                              DataImage::ITEM_GENERICS_STATIC_FIELDDESCS);

        for (DWORD i = 0; i < numStaticFields; i++)
        {
            FieldDesc *pFld = pGenericsFieldDescs + i;
            pFld->SaveContents(image);
            if (pFld != pGenericsFieldDescs)
               image->BindPointer(pFld, pFDNode, (BYTE *)pFld - (BYTE *)pGenericsFieldDescs);
        }
    }

    // Allocate a ModuleCtorInfo entry in the NGEN image if necessary
    if (HasBoxedRegularStatics())
    {
        image->GetModule()->GetZapModuleCtorInfo()->AddElement(this);
    }

    // MethodTable WriteableData

#ifdef FEATURE_REMOTING
    // Store any context static info.
    if (HasContextStatics())
    {
        DataImage::ItemKind kindWriteable = DataImage::ITEM_METHOD_TABLE_DATA_COLD_WRITEABLE;
        if ((profilingFlags & (1 << WriteMethodTableWriteableData)) != 0)
            kindWriteable = DataImage::ITEM_METHOD_TABLE_DATA_HOT_WRITEABLE;

        image->StoreStructure(GetContextStaticsBucket(),
                              sizeof(ContextStaticsBucket),
                              kindWriteable);
    }
#endif // FEATURE_REMOTING

    PTR_Const_MethodTableWriteableData pWriteableData = GetWriteableData_NoLogging();
    _ASSERTE(pWriteableData != NULL);
    if (pWriteableData != NULL)
    {
        pWriteableData->Save(image, this, profilingFlags);
    }

    LOG((LF_ZAP, LL_INFO10000, "MethodTable::Save %s (%p) complete.\n", GetDebugClassName(), this));

    // Save the EEClass at the same time as the method table if this is the canonical method table
    if (IsCanonicalMethodTable())
        GetClass()->Save(image, this);
} // MethodTable::Save

//==========================================================================
// The NeedsRestore Computation.
//
// WARNING: The NeedsRestore predicate on MethodTable and EEClass
// MUST be computable immediately after we have loaded a type.
// It must NOT depend on any additions or changes made to the
// MethodTable as a result of compiling code, or
// later steps such as prepopulating dictionaries.
//==========================================================================
BOOL MethodTable::ComputeNeedsRestore(DataImage *image, TypeHandleList *pVisited)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        // See comment in ComputeNeedsRestoreWorker
        PRECONDITION(GetLoaderModule()->HasNativeImage() || GetLoaderModule() == GetAppDomain()->ToCompilationDomain()->GetTargetModule());
    }
    CONTRACTL_END;

    _ASSERTE(GetAppDomain()->IsCompilationDomain()); // only used at ngen time!

    if (GetWriteableData()->IsNeedsRestoreCached())
    {
        return GetWriteableData()->GetCachedNeedsRestore();
    }

    // We may speculatively assume that any types we've visited on this run of
    // the ComputeNeedsRestore algorithm don't need a restore.  If they
    // do need a restore then we will check that when we first visit that method
    // table.
    if (TypeHandleList::Exists(pVisited, TypeHandle(this)))
    {
        pVisited->MarkBrokenCycle(this);
        return FALSE;
    }
    TypeHandleList newVisited(this, pVisited);

    BOOL needsRestore = ComputeNeedsRestoreWorker(image, &newVisited);

    // Cache the results of running the algorithm.
    // We can only cache the result if we have not speculatively assumed
    // that any types are not NeedsRestore
    if (!newVisited.HasBrokenCycleMark())
    {
        GetWriteableDataForWrite()->SetCachedNeedsRestore(needsRestore);
    }
    else
    {
        _ASSERTE(pVisited != NULL);
    }
    return needsRestore;
}

//==========================================================================================
BOOL MethodTable::ComputeNeedsRestoreWorker(DataImage *image, TypeHandleList *pVisited)
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    // You should only call ComputeNeedsRestoreWorker on things being saved into
    // the current LoaderModule - the NeedsRestore flag should have been computed
    // for all items from NGEN images, and we should never compute NeedsRestore
    // on anything that is not related to an NGEN image.  If this fails then 
    // there is probably a CanEagerBindTo check missing as we trace through a
    // pointer from one data structure to another.  
    // Trace back on the call stack and work out where this condition first fails.

    Module* myModule = GetLoaderModule();
    AppDomain* myAppDomain = GetAppDomain();
    CompilationDomain* myCompilationDomain = myAppDomain->ToCompilationDomain();
    Module* myCompilationModule = myCompilationDomain->GetTargetModule();

    if (myModule !=  myCompilationModule)
    {
        _ASSERTE(!"You should only call ComputeNeedsRestoreWorker on things being saved into the current LoaderModule");
    }
#endif

    if (g_CorCompileVerboseLevel == CORCOMPILE_VERBOSE)
    {
        DefineFullyQualifiedNameForClass();
        LPCUTF8 name = GetFullyQualifiedNameForClass(this);
        printf ("MethodTable %s needs restore? ", name);
    }
    if (g_CorCompileVerboseLevel >= CORCOMPILE_STATS && GetModule()->GetNgenStats())
        GetModule()->GetNgenStats()->MethodTableRestoreNumReasons[TotalMethodTables]++;

    #define UPDATE_RESTORE_REASON(c)                         \
        if (g_CorCompileVerboseLevel == CORCOMPILE_VERBOSE)  \
            printf ("Yes, " #c " \n");                       \
        if (g_CorCompileVerboseLevel >= CORCOMPILE_STATS && GetModule()->GetNgenStats())    \
            GetModule()->GetNgenStats()->MethodTableRestoreNumReasons[c]++;

    // The special method table for IL stubs has to be prerestored. Restore is not able to handle it
    // because of it does not have a token. In particular, this is a problem for /profiling native images.
    if (this == image->GetModule()->GetILStubCache()->GetStubMethodTable())
    {
        return FALSE;
    }

    // When profiling, we always want to perform the restore.
    if (GetAppDomain()->ToCompilationDomain()->m_fForceProfiling)
    {
        UPDATE_RESTORE_REASON(ProfilingEnabled);
        return TRUE;
    }

    if (DependsOnEquivalentOrForwardedStructs())
    {
        UPDATE_RESTORE_REASON(ComImportStructDependenciesNeedRestore);
        return TRUE;
    }

    if (!IsCanonicalMethodTable() && !image->CanPrerestoreEagerBindToMethodTable(GetCanonicalMethodTable(), pVisited))
    {
        UPDATE_RESTORE_REASON(CanNotPreRestoreHardBindToCanonicalMethodTable);
        return TRUE;
    }

    if (!image->CanEagerBindToModule(GetModule()))
    {
        UPDATE_RESTORE_REASON(CrossAssembly);
        return TRUE;
    }

    if (GetParentMethodTable())
    {
        if (!image->CanPrerestoreEagerBindToMethodTable(GetParentMethodTable(), pVisited))
        {
            UPDATE_RESTORE_REASON(CanNotPreRestoreHardBindToParentMethodTable);
            return TRUE;
        }
    }

    // Check per-inst pointers-to-dictionaries.
    if (!CanEagerBindToParentDictionaries(image, pVisited))
    {
        UPDATE_RESTORE_REASON(CanNotHardBindToInstanceMethodTableChain);
        return TRUE;
    }
    
    // Now check if the dictionary (if any) owned by this methodtable needs a restore.
    if (GetDictionary())
    {
        if (GetDictionary()->ComputeNeedsRestore(image, pVisited, GetNumGenericArgs()))
        {
            UPDATE_RESTORE_REASON(GenericsDictionaryNeedsRestore);
            return TRUE;
        }
    }

    // The interface chain is traversed without doing CheckRestore's.  Thus
    // if any of the types in the inherited interfaces hierarchy need a restore
    // or are cross-module pointers then this methodtable will also need a restore.
    InterfaceMapIterator it = IterateInterfaceMap();
    while (it.Next())
    {
        if (!image->CanPrerestoreEagerBindToMethodTable(it.GetInterface(), pVisited))
        {
            UPDATE_RESTORE_REASON(InterfaceIsGeneric);
            return TRUE;
        }
    }
    
    if (NeedsCrossModuleGenericsStaticsInfo())
    {
        UPDATE_RESTORE_REASON(CrossModuleGenericsStatics);
        return TRUE;
    }

    if (IsArray())
    {
        if(!image->CanPrerestoreEagerBindToTypeHandle(GetApproxArrayElementTypeHandle(), pVisited))
        {
            UPDATE_RESTORE_REASON(ArrayElement);
            return TRUE;
        }
    }

    if (g_CorCompileVerboseLevel == CORCOMPILE_VERBOSE)
        printf ("No \n");
    return FALSE;
}

//==========================================================================================
BOOL MethodTable::CanEagerBindToParentDictionaries(DataImage *image, TypeHandleList *pVisited)
{
    STANDARD_VM_CONTRACT;

    MethodTable *pChain = GetParentMethodTable();
    while (pChain != NULL)
    {
        // This is for the case were the method table contains a pointer to
        // an inherited dictionary, e.g. given the case D : C, C : B<int>
        // where B<int> is in another module then D contains a pointer to the
        // dictionary for B<int>.   Note that in this case we might still be
        // able to hadbind to C.
        if (pChain->HasInstantiation())
        {
            if (!image->CanEagerBindToMethodTable(pChain, FALSE, pVisited) ||
                !image->CanHardBindToZapModule(pChain->GetLoaderModule()))
            {
                return FALSE;
            }
        }
        pChain = pChain->GetParentMethodTable();
    }
    return TRUE;
}

//==========================================================================================
BOOL MethodTable::NeedsCrossModuleGenericsStaticsInfo()
{
    STANDARD_VM_CONTRACT;

    return HasGenericsStaticsInfo() && !ContainsGenericVariables() && !IsSharedByGenericInstantiations() &&
        (Module::GetPreferredZapModuleForMethodTable(this) != GetLoaderModule());
}

//==========================================================================================
BOOL MethodTable::IsWriteable()
{
    STANDARD_VM_CONTRACT;

    // Overlapped method table is written into in hosted scenarios
    // (see code:CorHost2::GetHostOverlappedExtensionSize)
    if (MscorlibBinder::IsClass(this, CLASS__OVERLAPPEDDATA))
        return TRUE;

#ifdef FEATURE_COMINTEROP
    // Dynamic expansion of interface map writes into method table
    // (see code:MethodTable::AddDynamicInterface)
    if (HasDynamicInterfaceMap())
        return TRUE;

    // CCW template is created lazily and when that happens, the
    // pointer is written directly into the method table.
    if (HasCCWTemplate())
        return TRUE;

    // RCW per-type data is created lazily at run-time.
    if (HasRCWPerTypeData())
        return TRUE;
#endif

    return FALSE;
}

//==========================================================================================
// This is used when non-canonical (i.e. duplicated) method tables
// attempt to bind to items logically belonging to an EEClass or MethodTable.
// i.e. the contract map in the EEClass and the generic dictionary stored in the canonical
// method table.
//
// We want to check if we can hard bind to the containing structure before
// deciding to hardbind to the inside of it.  This is because we may not be able
// to hardbind to all EEClass and/or MethodTables even if they live in a hradbindable
// target module.  Thus we want to call CanEagerBindToMethodTable
// to check we can hardbind to the containing structure.
static
void HardBindOrClearDictionaryPointer(DataImage *image, MethodTable *pMT, void * p, SSIZE_T offset)
{
    WRAPPER_NO_CONTRACT;

    if (image->CanEagerBindToMethodTable(pMT) &&
        image->CanHardBindToZapModule(pMT->GetLoaderModule()))
    {
        image->FixupPointerField(p, offset);
    }
    else
    {
        image->ZeroPointerField(p, offset);
    }
}

//==========================================================================================
void MethodTable::Fixup(DataImage *image)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsFullyLoaded());
    }
    CONTRACTL_END;

    LOG((LF_ZAP, LL_INFO10000, "MethodTable::Fixup %s\n", GetDebugClassName()));

    if (GetWriteableData()->IsFixedUp())
        return;

    BOOL needsRestore = NeedsRestore(image);
    LOG((LF_ZAP, LL_INFO10000, "MethodTable::Fixup %s (%p), needsRestore=%d\n", GetDebugClassName(), this, needsRestore));

    BOOL isCanonical = IsCanonicalMethodTable();

    Module *pZapModule = image->GetModule();

    MethodTable *pNewMT = (MethodTable *) image->GetImagePointer(this);

    // For canonical method tables, the pointer to the EEClass is never encoded as a fixup
    // even if this method table is not in its preferred zap module, i.e. the two are
    // "tightly-bound".
    if (IsCanonicalMethodTable())
    {
        // Pointer to EEClass
        image->FixupPointerField(this, offsetof(MethodTable, m_pEEClass));
    }
    else
    {
        //
        // Encode m_pEEClassOrCanonMT
        //
        MethodTable * pCanonMT = GetCanonicalMethodTable();

        ZapNode * pImport = NULL;
        if (image->CanEagerBindToMethodTable(pCanonMT))
        {
            if (image->CanHardBindToZapModule(pCanonMT->GetLoaderModule()))
            {
                // Pointer to canonical methodtable
                image->FixupField(this, offsetof(MethodTable, m_pCanonMT), pCanonMT, UNION_METHODTABLE);
            }
            else
            {
                // Pointer to lazy bound indirection cell to canonical methodtable
                pImport = image->GetTypeHandleImport(pCanonMT);
            }
        }
    else
        {
            // Pointer to eager bound indirection cell to canonical methodtable
            _ASSERTE(pCanonMT->IsTypicalTypeDefinition() ||
                     !pCanonMT->ContainsGenericVariables());
            pImport = image->GetTypeHandleImport(pCanonMT);
        }

        if (pImport != NULL)
        {
            image->FixupFieldToNode(this, offsetof(MethodTable, m_pCanonMT), pImport, UNION_INDIRECTION);
        }
    }

    image->FixupField(this, offsetof(MethodTable, m_pLoaderModule), pZapModule);

#ifdef _DEBUG
    image->FixupPointerField(this, offsetof(MethodTable, debug_m_szClassName));
#endif // _DEBUG

    MethodTable * pParentMT = GetParentMethodTable();
    _ASSERTE(!pNewMT->GetFlag(enum_flag_HasIndirectParent));

    if (pParentMT != NULL)
    {
        //
        // Encode m_pParentMethodTable
        //
        ZapNode * pImport = NULL;
        if (image->CanEagerBindToMethodTable(pParentMT))
        {
            if (image->CanHardBindToZapModule(pParentMT->GetLoaderModule()))
            {
                image->FixupPointerField(this, offsetof(MethodTable, m_pParentMethodTable));
            }
            else
            {
                pImport = image->GetTypeHandleImport(pParentMT);
            }
        }
        else
        {
            if (!pParentMT->IsCanonicalMethodTable())
            {
#ifdef _DEBUG
                IMDInternalImport *pInternalImport = GetModule()->GetMDImport();

                mdToken crExtends;
                pInternalImport->GetTypeDefProps(GetCl(),
                                                 NULL,
                                                 &crExtends);

                _ASSERTE(TypeFromToken(crExtends) == mdtTypeSpec);
#endif

                // Use unique cell for now since we are first going to set the parent method table to 
                // approx one first, and then to the exact one later. This would mess up the shared cell.
                // It would be nice to clean it up to use the shared cell - we should set the parent method table 
                // just once at the end.
                pImport = image->GetTypeHandleImport(pParentMT, this /* pUniqueId */);
            }
            else
            {
                pImport = image->GetTypeHandleImport(pParentMT);
            }
        }

        if (pImport != NULL)
        {
            image->FixupFieldToNode(this, offsetof(MethodTable, m_pParentMethodTable), pImport, -(SSIZE_T)offsetof(MethodTable, m_pParentMethodTable));
            pNewMT->SetFlag(enum_flag_HasIndirectParent);
        }
    }

    if (HasNonVirtualSlotsArray())
    {
        TADDR ppNonVirtualSlots = GetNonVirtualSlotsPtr();
        PREFIX_ASSUME(ppNonVirtualSlots != NULL);
        image->FixupRelativePointerField(this, (BYTE *)ppNonVirtualSlots - (BYTE *)this);
    }

    if (HasInterfaceMap())
    {
        image->FixupPointerField(this, offsetof(MethodTable, m_pMultipurposeSlot2));

        FixupExtraInterfaceInfo(image);
    }

    _ASSERTE(GetWriteableData());
    image->FixupPointerField(this, offsetof(MethodTable, m_pWriteableData));
    m_pWriteableData->Fixup(image, this, needsRestore);

#ifdef FEATURE_COMINTEROP
    if (HasGuidInfo())
    {
        GuidInfo **ppGuidInfo = GetGuidInfoPtr();
        if (*ppGuidInfo != NULL)
        {
            image->FixupPointerField(this, (BYTE *)ppGuidInfo - (BYTE *)this);
        }
        else
        {
            image->ZeroPointerField(this, (BYTE *)ppGuidInfo - (BYTE *)this);
        }
    }

    if (HasCCWTemplate())
    {
        ComCallWrapperTemplate **ppTemplate = GetCCWTemplatePtr();
        image->ZeroPointerField(this, (BYTE *)ppTemplate - (BYTE *)this);
    }

    if (HasRCWPerTypeData())
    {
        // it would be nice to save these but the impact on mscorlib.ni size is prohibitive
        RCWPerTypeData **ppData = GetRCWPerTypeDataPtr();
        image->ZeroPointerField(this, (BYTE *)ppData - (BYTE *)this);
    }
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_REMOTING
    if (HasRemotableMethodInfo())
    {
        CrossDomainOptimizationInfo **pRMI = GetRemotableMethodInfoPtr();
        if (*pRMI != NULL)
        {
            image->FixupPointerField(this, (BYTE *)pRMI - (BYTE *)this);
        }
    }

    // Optional VTS (Version Tolerant Serialization) fixups.
    if (HasRemotingVtsInfo())
    {
        RemotingVtsInfo **ppVtsInfo = GetRemotingVtsInfoPtr();
        image->FixupPointerField(this, (BYTE *)ppVtsInfo - (BYTE *)this);

        RemotingVtsInfo *pVtsInfo = *ppVtsInfo;
        for (DWORD i = 0; i < RemotingVtsInfo::VTS_NUM_CALLBACK_TYPES; i++)
            image->FixupMethodDescPointer(pVtsInfo, &pVtsInfo->m_pCallbacks[i]);
    }
#endif //FEATURE_REMOTING

    //
    // Fix flags
    //

    _ASSERTE((pNewMT->GetFlag(enum_flag_IsZapped) == 0));
    pNewMT->SetFlag(enum_flag_IsZapped);

    _ASSERTE((pNewMT->GetFlag(enum_flag_IsPreRestored) == 0));
    if (!needsRestore)
        pNewMT->SetFlag(enum_flag_IsPreRestored);

    //
    // Fixup vtable
    // If the canonical method table lives in a different loader module
    // then just zero out the entries and copy them across from the canonical
    // vtable on restore.
    //
    // Note the canonical method table will be the same as the current method table
    // if the method table is not a generic instantiation.

    if (HasDispatchMapSlot())
    {
        TADDR pSlot = GetMultipurposeSlotPtr(enum_flag_HasDispatchMapSlot, c_DispatchMapSlotOffsets);
        DispatchMap * pDispatchMap = RelativePointer<PTR_DispatchMap>::GetValueAtPtr(pSlot);
        image->FixupField(this, pSlot - (TADDR)this, pDispatchMap, 0, IMAGE_REL_BASED_RelativePointer);
        pDispatchMap->Fixup(image);
    }

    if (HasModuleOverride())
    {
        image->FixupModulePointer(this, GetModuleOverridePtr());
    }

    {
        VtableIndirectionSlotIterator it = IterateVtableIndirectionSlots();
        while (it.Next())
        {
            image->FixupPointerField(this, it.GetOffsetFromMethodTable());
        }
    }

    unsigned numVTableSlots = GetNumVtableSlots();
    for (unsigned slotNumber = 0; slotNumber < numVTableSlots; slotNumber++)
    {
        //
        // Find the method desc from the slot.
        //
        MethodDesc *pMD = GetMethodDescForSlot(slotNumber);
        _ASSERTE(pMD != NULL);
        pMD->CheckRestore();

        PVOID slotBase;
        SSIZE_T slotOffset;

        if (slotNumber < GetNumVirtuals())
        {
            // Virtual slots live in chunks pointed to by vtable indirections

            slotBase = (PVOID) GetVtableIndirections()[GetIndexOfVtableIndirection(slotNumber)];
            slotOffset = GetIndexAfterVtableIndirection(slotNumber) * sizeof(PCODE);
        }
        else if (HasSingleNonVirtualSlot())
        {
            // Non-virtual slots < GetNumVtableSlots live in a single chunk pointed to by an optional member,
            // except when there is only one in which case it lives in the optional member itself

            _ASSERTE(slotNumber == GetNumVirtuals());
            slotBase = (PVOID) this;
            slotOffset = (BYTE *)GetSlotPtr(slotNumber) - (BYTE *)this;
        }
        else
        {
            // Non-virtual slots < GetNumVtableSlots live in a single chunk pointed to by an optional member

            _ASSERTE(HasNonVirtualSlotsArray());
            slotBase = (PVOID) GetNonVirtualSlotsArray();
            slotOffset = (slotNumber - GetNumVirtuals()) * sizeof(PCODE);
        }

        // Attempt to make the slot point directly at the prejitted code.
        // Note that changes to this logic may require or enable an update to CanInternVtableChunk.
        // If a necessary update is not made, an assert will fire in ZapStoredStructure::Save.

        if (pMD->GetMethodTable() == this)
        {
            ZapRelocationType relocType;
            if (slotNumber >= GetNumVirtuals())
                relocType = IMAGE_REL_BASED_RelativePointer;
            else
                relocType = IMAGE_REL_BASED_PTR;

            pMD->FixupSlot(image, slotBase, slotOffset, relocType);
        }
        else
        {

#ifdef _DEBUG

            // Static method should be in the owning methodtable only.
            _ASSERTE(!pMD->IsStatic());

            MethodTable *pSourceMT = isCanonical
                ? GetParentMethodTable()
                : GetCanonicalMethodTable();

            // It must be inherited from the parent or copied from the canonical
            _ASSERTE(pSourceMT->GetMethodDescForSlot(slotNumber) == pMD);
#endif

            if (image->CanEagerBindToMethodDesc(pMD) && pMD->GetLoaderModule() == pZapModule)
            {
                pMD->FixupSlot(image, slotBase, slotOffset);
            }
            else
            {
                if (!pMD->IsGenericMethodDefinition())
                {
                    ZapNode * importThunk = image->GetVirtualImportThunk(pMD->GetMethodTable(), pMD, slotNumber);
                    // On ARM, make sure that the address to the virtual thunk that we write into the
                    // vtable "chunk" has the Thumb bit set.
                    image->FixupFieldToNode(slotBase, slotOffset, importThunk ARM_ARG(THUMB_CODE));
                }
                else
                {
                    // Virtual generic methods don't/can't use their vtable slot 
                    image->ZeroPointerField(slotBase, slotOffset);
                }
            }
        }
    }

    //
    // Fixup Interface map
    //

    InterfaceMapIterator it = IterateInterfaceMap();
    while (it.Next())
    {
        image->FixupMethodTablePointer(GetInterfaceMap(), &it.GetInterfaceInfo()->m_pMethodTable);
    }

    if (IsArray())
    {
        image->HardBindTypeHandlePointer(this, offsetof(MethodTable, m_ElementTypeHnd));
    }

    //
    // Fixup per-inst pointers for this method table
    //

    if (HasPerInstInfo())
    {
        // Fixup the pointer to the per-inst table
        image->FixupPointerField(this, offsetof(MethodTable, m_pPerInstInfo));

        for (MethodTable *pChain = this; pChain != NULL; pChain = pChain->GetParentMethodTable())
        {
            if (pChain->HasInstantiation())
            {
                DWORD dictNum = pChain->GetNumDicts()-1;

                // If we can't hardbind then the value will be copied down from
                // the parent upon restore.

                // We special-case the dictionary for this method table because we must always
                // hard bind to it even if it's not in its preferred zap module
                if (pChain == this)
                    image->FixupPointerField(GetPerInstInfo(), dictNum * sizeof(Dictionary *));
                else
                    HardBindOrClearDictionaryPointer(image, pChain, GetPerInstInfo(), dictNum * sizeof(Dictionary *));
            }
        }
    }
    //
    // Fixup instantiation+dictionary for this method table (if any)
    //
    if (GetDictionary())
    {
        LOG((LF_JIT, LL_INFO10000, "GENERICS: Fixup dictionary for MT %s\n",  GetDebugClassName()));

        // CanEagerBindToMethodTable would not work for targeted patching here. The dictionary
        // layout is sensitive to compilation order that can be changed by TP compatible changes.
        BOOL canSaveSlots = !IsCanonicalMethodTable() && (image->GetModule() == GetCanonicalMethodTable()->GetLoaderModule());

        // See comment on Dictionary::Fixup
        GetDictionary()->Fixup(image,
                               TRUE,
                               canSaveSlots,
                               GetNumGenericArgs(),
                               GetModule(),
                               GetClass()->GetDictionaryLayout());
    }

    // Fixup per-inst statics info
    if (HasGenericsStaticsInfo())
    {
        GenericsStaticsInfo *pInfo = GetGenericsStaticsInfo();

        image->FixupPointerField(this, (BYTE *)&pInfo->m_pFieldDescs - (BYTE *)this);
        if (!isCanonical)
        {
            for (DWORD i = 0; i < GetClass()->GetNumStaticFields(); i++)
            {
                FieldDesc *pFld = GetGenericsStaticFieldDescs() + i;
                pFld->Fixup(image);
            }
        }

        if (NeedsCrossModuleGenericsStaticsInfo())
        {
            MethodTableWriteableData * pNewWriteableData = (MethodTableWriteableData *)image->GetImagePointer(m_pWriteableData);
            CrossModuleGenericsStaticsInfo * pNewCrossModuleGenericsStaticsInfo = pNewWriteableData->GetCrossModuleGenericsStaticsInfo();

            pNewCrossModuleGenericsStaticsInfo->m_DynamicTypeID = pInfo->m_DynamicTypeID;

            image->ZeroPointerField(m_pWriteableData, sizeof(MethodTableWriteableData) + offsetof(CrossModuleGenericsStaticsInfo, m_pModuleForStatics));

            pNewMT->SetFlag(enum_flag_StaticsMask_IfGenericsThenCrossModule);
        }
    }
    else
    {
        _ASSERTE(!NeedsCrossModuleGenericsStaticsInfo());
    }

#ifdef FEATURE_REMOTING
    if (HasContextStatics())
    {
        ContextStaticsBucket **ppInfo = GetContextStaticsBucketPtr();
        image->FixupPointerField(this, (BYTE *)ppInfo - (BYTE *)this);

        ContextStaticsBucket *pNewInfo = (ContextStaticsBucket*)image->GetImagePointer(*ppInfo);
        pNewInfo->m_dwContextStaticsOffset = (DWORD)-1;
    }
#endif // FEATURE_REMOTING

    LOG((LF_ZAP, LL_INFO10000, "MethodTable::Fixup %s (%p) complete\n", GetDebugClassName(), this));

    // If this method table is canonical (one-to-one with EEClass) then fix up the EEClass also
    if (isCanonical)
        GetClass()->Fixup(image, this);

    // Mark method table as fixed-up
    GetWriteableDataForWrite()->SetFixedUp();

} // MethodTable::Fixup

//==========================================================================================
void MethodTableWriteableData::Save(DataImage *image, MethodTable *pMT, DWORD profilingFlags) const
{
    STANDARD_VM_CONTRACT;

    SIZE_T size = sizeof(MethodTableWriteableData);

    // MethodTableWriteableData is followed by optional CrossModuleGenericsStaticsInfo in NGen images
    if (pMT->NeedsCrossModuleGenericsStaticsInfo())
        size += sizeof(CrossModuleGenericsStaticsInfo);

    DataImage::ItemKind kindWriteable = DataImage::ITEM_METHOD_TABLE_DATA_COLD_WRITEABLE;
    if ((profilingFlags & (1 << WriteMethodTableWriteableData)) != 0)
        kindWriteable = DataImage::ITEM_METHOD_TABLE_DATA_HOT_WRITEABLE;

    ZapStoredStructure * pNode = image->StoreStructure(NULL, size, kindWriteable);
    image->BindPointer(this, pNode, 0);
    image->CopyData(pNode, this, sizeof(MethodTableWriteableData));
}

//==========================================================================================
void MethodTableWriteableData::Fixup(DataImage *image, MethodTable *pMT, BOOL needsRestore)
{
    STANDARD_VM_CONTRACT;

    image->ZeroField(this, offsetof(MethodTableWriteableData, m_hExposedClassObject), sizeof(m_hExposedClassObject));

    MethodTableWriteableData *pNewNgenPrivateMT = (MethodTableWriteableData*) image->GetImagePointer(this);
    _ASSERTE(pNewNgenPrivateMT != NULL);

    pNewNgenPrivateMT->m_dwFlags &= ~(enum_flag_RemotingConfigChecked |
                                      enum_flag_CriticalTypePrepared);

    if (needsRestore)
        pNewNgenPrivateMT->m_dwFlags |= (enum_flag_UnrestoredTypeKey |
                                         enum_flag_Unrestored |
                                         enum_flag_HasApproxParent |
                                         enum_flag_IsNotFullyLoaded);

#ifdef _DEBUG
    pNewNgenPrivateMT->m_dwLastVerifedGCCnt = (DWORD)-1;
#endif
}

#endif // !DACCESS_COMPILE

#endif // FEATURE_NATIVE_IMAGE_GENERATION

#ifdef FEATURE_PREJIT

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

    g_IBCLogger.LogMethodTableAccess(this);
}

#else // !FEATURE_PREJIT
//==========================================================================================
void MethodTable::CheckRestore()
{
    LIMITED_METHOD_CONTRACT;
}
#endif // !FEATURE_PREJIT


#ifndef DACCESS_COMPILE

BOOL SatisfiesClassConstraints(TypeHandle instanceTypeHnd, TypeHandle typicalTypeHnd,
                               const InstantiationContext *pInstContext);

static VOID DoAccessibilityCheck(MethodTable *pAskingMT, MethodTable *pTargetMT, UINT resIDWhy, BOOL checkTargetTypeTransparency)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    StaticAccessCheckContext accessContext(NULL, pAskingMT);

    if (!ClassLoader::CanAccessClass(&accessContext,
                                     pTargetMT,                 //the desired class
                                     pTargetMT->GetAssembly(),  //the desired class's assembly
                                     *AccessCheckOptions::s_pNormalAccessChecks,
                                     checkTargetTypeTransparency
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

    if (thConstraint.IsTypeDesc())
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
        DoAccessibilityCheck(pAskingMT, thConstraint.GetMethodTable(), resIDWhy, FALSE);
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
//   pfBailed - if we or one of our depedencies bails early due to cyclic dependencies, we
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
    DoFullyLoadLocals(DFLPendingList *pPendingParam, ClassLoadLevel levelParam, MethodTable *pMT, Generics::RecursionGraph *pVisited) :
        newVisited(pVisited, TypeHandle(pMT)),
        pPending(pPendingParam),
        level(levelParam),
        fBailed(FALSE)
#ifdef FEATURE_COMINTEROP
        , fHasEquivalentStructParameter(FALSE)
#endif
        , fHasTypeForwarderDependentStructParameter(FALSE)
        , fDependsOnEquivalentOrForwardedStructs(FALSE)
    {
        LIMITED_METHOD_CONTRACT;
    }

    Generics::RecursionGraph newVisited;
    DFLPendingList * const pPending;
    const ClassLoadLevel level;
    BOOL fBailed;
#ifdef FEATURE_COMINTEROP
    BOOL fHasEquivalentStructParameter;
#endif
    BOOL fHasTypeForwarderDependentStructParameter;
    BOOL fDependsOnEquivalentOrForwardedStructs;
};

#if defined(FEATURE_TYPEEQUIVALENCE) && !defined(DACCESS_COMPILE)
static void CheckForEquivalenceAndFullyLoadType(Module *pModule, mdToken token, Module *pDefModule, mdToken defToken, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    SigPointer sigPtr(*ptr);

    DoFullyLoadLocals *pLocals = (DoFullyLoadLocals *)pData;

    if (IsTypeDefEquivalent(defToken, pDefModule))
    {
        TypeHandle th = sigPtr.GetTypeHandleThrowing(pModule, pTypeContext, ClassLoader::LoadTypes, (ClassLoadLevel)(pLocals->level - 1));
        CONSISTENCY_CHECK(!th.IsNull());

        th.DoFullyLoad(&pLocals->newVisited, pLocals->level, pLocals->pPending, &pLocals->fBailed, NULL);
        pLocals->fDependsOnEquivalentOrForwardedStructs = TRUE;
        pLocals->fHasEquivalentStructParameter = TRUE;
    }
}

#endif // defined(FEATURE_TYPEEQUIVALENCE) && !defined(DACCESS_COMPILE)

struct CheckForTypeForwardedTypeRefParameterLocals
{
    Module * pModule;
    BOOL *   pfTypeForwarderFound;
};

// Callback for code:WalkValueTypeTypeDefOrRefs of type code:PFN_WalkValueTypeTypeDefOrRefs
static void CheckForTypeForwardedTypeRef(
    mdToken tkTypeDefOrRef, 
    void *  pData)
{
    STANDARD_VM_CONTRACT;
    
    CheckForTypeForwardedTypeRefParameterLocals * pLocals = (CheckForTypeForwardedTypeRefParameterLocals *)pData;
    
    // If a type forwarder was found, return - we're done
    if ((pLocals->pfTypeForwarderFound != NULL) && (*(pLocals->pfTypeForwarderFound)))
        return;
    
    // Only type ref's are interesting
    if (TypeFromToken(tkTypeDefOrRef) == mdtTypeRef)
    {
        Module * pDummyModule;
        mdToken  tkDummy;
        ClassLoader::ResolveTokenToTypeDefThrowing(
            pLocals->pModule, 
            tkTypeDefOrRef, 
            &pDummyModule, 
            &tkDummy, 
            Loader::Load, 
            pLocals->pfTypeForwarderFound);
    }
}

typedef void (* PFN_WalkValueTypeTypeDefOrRefs)(mdToken tkTypeDefOrRef, void * pData);

// Call 'function' for ValueType in the signature.
void WalkValueTypeTypeDefOrRefs(
    const SigParser *              pSig, 
    PFN_WalkValueTypeTypeDefOrRefs function, 
    void *                         pData)
{
    STANDARD_VM_CONTRACT;
    
    SigParser sig(*pSig);
    
    CorElementType typ;
    IfFailThrow(sig.GetElemType(&typ));
    
    switch (typ)
    {
        case ELEMENT_TYPE_VALUETYPE:
            mdToken token;
            IfFailThrow(sig.GetToken(&token));
            function(token, pData);
            break;
        
        case ELEMENT_TYPE_GENERICINST:
            // Process and skip generic type
            WalkValueTypeTypeDefOrRefs(&sig, function, pData);
            IfFailThrow(sig.SkipExactlyOne());
            
            // Get number of parameters
            ULONG argCnt;
            IfFailThrow(sig.GetData(&argCnt));
            while (argCnt-- != 0)
            {   // Process and skip generic parameter
                WalkValueTypeTypeDefOrRefs(&sig, function, pData);
                IfFailThrow(sig.SkipExactlyOne());
            }
            break;
        default:
            break;
    }
}

// Callback for code:MethodDesc::WalkValueTypeParameters (of type code:WalkValueTypeParameterFnPtr)
static void CheckForTypeForwardedTypeRefParameter(
    Module *         pModule, 
    mdToken          token, 
    Module *         pDefModule, 
    mdToken          defToken, 
    const SigParser *ptr, 
    SigTypeContext * pTypeContext, 
    void *           pData)
{
    STANDARD_VM_CONTRACT;
    
    DoFullyLoadLocals * pLocals = (DoFullyLoadLocals *)pData;

    // If a type forwarder was found, return - we're done
    if (pLocals->fHasTypeForwarderDependentStructParameter)
        return;
    
    CheckForTypeForwardedTypeRefParameterLocals locals;
    locals.pModule = pModule;
    locals.pfTypeForwarderFound = &pLocals->fHasTypeForwarderDependentStructParameter; // By not passing NULL here, we determine if there is a type forwarder involved.
    
    WalkValueTypeTypeDefOrRefs(ptr, CheckForTypeForwardedTypeRef, &locals);
    
    if (pLocals->fHasTypeForwarderDependentStructParameter)
        pLocals->fDependsOnEquivalentOrForwardedStructs = TRUE;
}

// Callback for code:MethodDesc::WalkValueTypeParameters (of type code:WalkValueTypeParameterFnPtr)
static void LoadTypeDefOrRefAssembly(
    Module *         pModule, 
    mdToken          token, 
    Module *         pDefModule, 
    mdToken          defToken, 
    const SigParser *ptr, 
    SigTypeContext * pTypeContext, 
    void *           pData)
{
    STANDARD_VM_CONTRACT;
    
    DoFullyLoadLocals * pLocals = (DoFullyLoadLocals *)pData;
    
    CheckForTypeForwardedTypeRefParameterLocals locals;
    locals.pModule = pModule;
    locals.pfTypeForwarderFound = NULL; // By passing NULL here, we simply resolve the token to TypeDef.
    
    WalkValueTypeTypeDefOrRefs(ptr, CheckForTypeForwardedTypeRef, &locals);
}

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

    BEGIN_SO_INTOLERANT_CODE(GetThread());
    // First ensure that we're loaded to just below CLASS_DEPENDENCIES_LOADED
    ClassLoader::EnsureLoaded(this, (ClassLoadLevel) (level-1));

    CONSISTENCY_CHECK(IsRestored_NoLogging());
    CONSISTENCY_CHECK(!HasApproxParent());


    DoFullyLoadLocals locals(pPending, level, this, pVisited);

    bool fNeedsSanityChecks = !IsZapped(); // Validation has been performed for NGened classes already

#ifdef FEATURE_READYTORUN
    if (fNeedsSanityChecks)
    {
        Module * pModule = GetModule();

        // No sanity checks for ready-to-run compiled images if possible
        if (pModule->IsReadyToRun() && pModule->GetReadyToRunInfo()->SkipTypeValidation())
            fNeedsSanityChecks = false;
    }
#endif

    bool fNeedAccessChecks = (level == CLASS_LOADED) &&
                             fNeedsSanityChecks &&
                             IsTypicalTypeDefinition();

    TypeHandle typicalTypeHnd;

    if (!IsZapped()) // Validation has been performed for NGened classes already
    {
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
                // A transparenct type should not be allowed to derive from a critical type.
                // However since this has never been enforced before we have many classes that
                // violate this rule. Enforcing it now will be a breaking change.
                DoAccessibilityCheck(this, pParentMT, E_ACCESSDENIED, /* checkTargetTypeTransparency*/ FALSE);
            }
        }
    }

    // Fully load the interfaces
    MethodTable::InterfaceMapIterator it = IterateInterfaceMap();
    while (it.Next())
    {
        it.GetInterface()->DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);

        if (fNeedAccessChecks)
        {
            if (IsInterfaceDeclaredOnClass(it.GetIndex())) // only test directly implemented interfaces (it's
                                                           // legal for an inherited interface to be private.) 
            {
                // A transparenct type should not be allowed to implement a critical interface.
                // However since this has never been enforced before we have many classes that
                // violate this rule. Enforcing it now will be a breaking change.
                DoAccessibilityCheck(this, it.GetInterface(), IDS_CLASSLOAD_INTERFACE_NO_ACCESS, /* checkTargetTypeTransparency*/ FALSE);
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
            g_IBCLogger.LogFieldDescsAccess(pField);

            if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
            {
                TypeHandle th = pField->GetFieldTypeHandleThrowing((ClassLoadLevel) (level - 1));
                CONSISTENCY_CHECK(!th.IsNull());

                th.DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);

                if (fNeedAccessChecks)
                {
                    DoAccessibilityCheck(this, th.GetMethodTable(), E_ACCESSDENIED, FALSE);
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // Fully load the types of fields associated with a field marshaler when ngenning
    if (HasLayout() && GetAppDomain()->IsCompilationDomain() && !IsZapped())
    {
        FieldMarshaler* pFM                   = this->GetLayoutInfo()->GetFieldMarshalers();
        UINT  numReferenceFields              = this->GetLayoutInfo()->GetNumCTMFields();

        while (numReferenceFields--)
        {
            
            FieldDesc *pMarshalerField = pFM->GetFieldDesc();

            // If the fielddesc pointer here is a token tagged pointer, then the field marshaler that we are
            // working with will not need to be saved into this ngen image. And as that was the reason that we 
            // needed to load this type, thus we will not need to fully load the type associated with this field desc.
            //
            if (!CORCOMPILE_IS_POINTER_TAGGED(pMarshalerField))
            {
                TypeHandle th = pMarshalerField->GetFieldTypeHandleThrowing((ClassLoadLevel) (level-1));
                CONSISTENCY_CHECK(!th.IsNull());
                
                th.DoFullyLoad(&locals.newVisited, level, pPending, &locals.fBailed, pInstContext);
            }
            // The accessibility check is not used here to prevent functional differences between ngen and non-ngen scenarios.
            ((BYTE*&)pFM) += MAXFIELDMARSHALERSIZE;
        }
    }
#endif //FEATURE_NATIVE_IMAGE_GENERATION

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
    // equivalent based on an extensive set of equivalency checks..
    //
    // To address this situation for NGENed types and methods, we prevent pre-restoring them - see code:ComputeNeedsRestoreWorker
    // for details. That forces them to go through the final stages of loading at run-time and hit the same code below.

    if ((level == CLASS_LOADED) 
        && (GetCl() != mdTypeDefNil) 
        && !ContainsGenericVariables() 
        && (!IsZapped() 
            || DependsOnEquivalentOrForwardedStructs()
#ifdef DEBUG
            || TRUE // Always load types in debug builds so that we calculate fDependsOnEquivalentOrForwardedStructs all of the time
#endif
           )
       )
    {
        MethodTable::IntroducedMethodIterator itMethods(this, FALSE);
        for (; itMethods.IsValid(); itMethods.Next())
        {
            MethodDesc * pMD = itMethods.GetMethodDesc();
            
            if (IsCompilationProcess())
            {
                locals.fHasTypeForwarderDependentStructParameter = FALSE;
                EX_TRY
                {
                    pMD->WalkValueTypeParameters(this, CheckForTypeForwardedTypeRefParameter, &locals);
                }
                EX_CATCH
                {
                }
                EX_END_CATCH(RethrowTerminalExceptions);

                // This marks the class as needing restore.
                if (locals.fHasTypeForwarderDependentStructParameter && !pMD->IsZapped())
                    pMD->SetHasForwardedValuetypeParameter();
            }
            else if (pMD->IsZapped() && pMD->HasForwardedValuetypeParameter())
            {
                pMD->WalkValueTypeParameters(this, LoadTypeDefOrRefAssembly, NULL);
                locals.fDependsOnEquivalentOrForwardedStructs = TRUE;
            }

#ifdef FEATURE_TYPEEQUIVALENCE
            if (!pMD->DoesNotHaveEquivalentValuetypeParameters() && pMD->IsVirtual())
            {
                locals.fHasEquivalentStructParameter = FALSE;
                pMD->WalkValueTypeParameters(this, CheckForEquivalenceAndFullyLoadType, &locals);
                if (!locals.fHasEquivalentStructParameter && !IsZapped())
                    pMD->SetDoesNotHaveEquivalentValuetypeParameters();
            }
#else
#ifdef FEATURE_PREJIT
            if (!IsZapped() && pMD->IsVirtual() && !IsCompilationProcess() )
            {
                pMD->PrepareForUseAsADependencyOfANativeImage();
            }
#endif
#endif //FEATURE_TYPEEQUIVALENCE
        }
    }

    _ASSERTE(!IsZapped() || !IsCanonicalMethodTable() || (level != CLASS_LOADED) || ((!!locals.fDependsOnEquivalentOrForwardedStructs) == (!!DependsOnEquivalentOrForwardedStructs())));
    if (locals.fDependsOnEquivalentOrForwardedStructs)
    {
        if (!IsZapped())
        {
            // if this type declares a method that has an equivalent or type forwarded structure as a parameter type,
            // make sure we come here and pre-load these structure types in NGENed cases as well
            SetDependsOnEquivalentOrForwardedStructs();
        }
    }

    // The rules for constraint cycles are same as rules for acccess checks
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
        LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: Completed full dependency load of type %S\n", name.GetUnicode()));
    }
#endif

    switch (level)
    {
        case CLASS_DEPENDENCIES_LOADED:
            SetIsDependenciesLoaded();

#if defined(FEATURE_COMINTEROP) && !defined(DACCESS_COMPILE)
            if (WinRTSupported() && g_fEEStarted && !ContainsIntrospectionOnlyTypes())
            {
                _ASSERTE(GetAppDomain() != NULL);

                AppDomain* pAppDomain = GetAppDomain();
                if (pAppDomain->CanCacheWinRTTypeByGuid(this))
                {
                    pAppDomain->CacheWinRTTypeByGuid(this);
                }
            }
#endif // FEATURE_COMINTEROP && !DACCESS_COMPILE
            
            break;

        case CLASS_LOADED:
            if (!IsZapped() && // Constraint checks have been performed for NGened classes already
                !IsTypicalTypeDefinition() &&
                !IsSharedByGenericInstantiations())
            {
                TypeHandle thThis = TypeHandle(this);
    
                // If we got here, we about to mark a generic instantiation as fully loaded. Before we do so,
                // check to see if has constraints that aren't being satisfied.
                SatisfiesClassConstraints(thThis, typicalTypeHnd, pInstContext);
    
            }

            if (locals.fBailed)
            {
                // We couldn't complete security checks on some dependency because he is already being processed by one of our callers.
                // Do not mark this class fully loaded yet. Put him on the pending list and he will be marked fully loaded when
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

    END_SO_INTOLERANT_CODE;
    
#endif //!DACCESS_COMPILE
} //MethodTable::DoFullyLoad


#ifndef DACCESS_COMPILE

#ifdef FEATURE_PREJIT

// For a MethodTable in a native image, decode sufficient encoded pointers
// that the TypeKey for this type is recoverable.
//
// For instantiated generic types, we need the generic type arguments,
// the EEClass pointer, and its Module pointer.
// (For non-generic types, the EEClass and Module are always hard bound).
//
// The process is applied recursively e.g. consider C<D<string>[]>.
// It is guaranteed to terminate because types cannot contain cycles in their structure.
//
// Also note that no lock is required; the process of restoring this information is idempotent.
// (Note the atomic action at the end though)
//
void MethodTable::DoRestoreTypeKey()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // If we have an indirection cell then restore the m_pCanonMT and its module pointer
    //
    if (union_getLowBits(m_pCanonMT) == UNION_INDIRECTION)
    {
        Module::RestoreMethodTablePointerRaw((MethodTable **)(union_getPointer(m_pCanonMT)), 
            GetLoaderModule(), CLASS_LOAD_UNRESTORED);
    }

    MethodTable * pMTForModule = IsArray() ? this : GetCanonicalMethodTable();
    if (pMTForModule->HasModuleOverride())
    {
        Module::RestoreModulePointer(pMTForModule->GetModuleOverridePtr(), pMTForModule->GetLoaderModule());
    }

    if (IsArray())
    {
        //
        // Restore array element type handle
        //
        Module::RestoreTypeHandlePointerRaw(GetApproxArrayElementTypeHandlePtr(), 
                                            GetLoaderModule(), CLASS_LOAD_UNRESTORED);
    }

    // Next restore the instantiation and recurse
    Instantiation inst = GetInstantiation();
    for (DWORD j = 0; j < inst.GetNumArgs(); j++)
    {
        Module::RestoreTypeHandlePointer(&inst.GetRawArgs()[j], GetLoaderModule(), CLASS_LOAD_UNRESTORED);
    }

    FastInterlockAnd(&(EnsureWritablePages(GetWriteableDataForWrite())->m_dwFlags), ~MethodTableWriteableData::enum_flag_UnrestoredTypeKey);
}

//==========================================================================================
// For a MethodTable in a native image, apply Restore actions
// * Decode any encoded pointers
// * Instantiate static handles
// * Propagate Restore to EEClass
// For array method tables, Restore MUST BE IDEMPOTENT as it can be entered from multiple threads
// For other classes, restore cannot be entered twice because the loader maintains locks
//
// When you actually restore the MethodTable for a generic type, the generic
// dictionary is restored.  That means:
// * Parent slots in the PerInstInfo are restored by this method eagerly.  They are copied down from the
//   parent in code:ClassLoader.LoadExactParentAndInterfacesTransitively
// * Instantiation parameters in the dictionary are restored eagerly when the type is restored.  These are
//   either hard bound pointers, or tagged tokens (fixups).
// * All other dictionary entries are either hard bound pointers or they are NULL (they are cleared when we
//   freeze the Ngen image).  They are *never* tagged tokens.
void MethodTable::Restore()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(IsZapped());
        PRECONDITION(!IsRestored_NoLogging());
        PRECONDITION(!HasUnrestoredTypeKey());
    }
    CONTRACTL_END;

    g_IBCLogger.LogMethodTableAccess(this);

    STRESS_LOG1(LF_ZAP, LL_INFO10000, "MethodTable::Restore: Restoring type %pT\n", this);
    LOG((LF_ZAP, LL_INFO10000,
         "Restoring methodtable %s at " FMT_ADDR ".\n", GetDebugClassName(), DBG_ADDR(this)));

    // Class pointer should be restored already (in DoRestoreTypeKey)
    CONSISTENCY_CHECK(IsClassPointerValid());

    // If this isn't the canonical method table itself, then restore the canonical method table
    // We will load the canonical method table to level EXACTPARENTS in LoadExactParents
    if (!IsCanonicalMethodTable())
    {
        ClassLoader::EnsureLoaded(GetCanonicalMethodTable(), CLASS_LOAD_APPROXPARENTS);
    }

    //
    // Restore parent method table
    //
    Module::RestoreMethodTablePointerRaw(GetParentMethodTablePtr(), GetLoaderModule(), CLASS_LOAD_APPROXPARENTS);

    //
    // Restore interface classes
    //
    InterfaceMapIterator it = IterateInterfaceMap();
    while (it.Next())
    {
        // Just make sure that approximate interface is loaded. LoadExactParents fill in the exact interface later.
        MethodTable * pIftMT;
        pIftMT = it.GetInterfaceInfo()->GetApproxMethodTable(GetLoaderModule());
        _ASSERTE(pIftMT != NULL);
    }
       
    if (HasCrossModuleGenericStaticsInfo())
    {
        MethodTableWriteableData * pWriteableData = GetWriteableDataForWrite();
        CrossModuleGenericsStaticsInfo * pInfo = pWriteableData->GetCrossModuleGenericsStaticsInfo();

        EnsureWritablePages(pWriteableData, sizeof(MethodTableWriteableData) + sizeof(CrossModuleGenericsStaticsInfo));

        if (IsDomainNeutral())
        {
            // If we are domain neutral, we have to use constituent of the instantiation to store
            // statics. We need to ensure that we can create DomainModule in all domains
            // that this instantiations may get activated in. PZM is good approximation of such constituent.
            Module * pModuleForStatics = Module::GetPreferredZapModuleForMethodTable(this);

            pInfo->m_pModuleForStatics = pModuleForStatics;
            pInfo->m_DynamicTypeID = pModuleForStatics->AllocateDynamicEntry(this);
        }
        else
        {
            pInfo->m_pModuleForStatics = GetLoaderModule();
        }
    }

    LOG((LF_ZAP, LL_INFO10000,
         "Restored methodtable %s at " FMT_ADDR ".\n", GetDebugClassName(), DBG_ADDR(this)));

    // This has to be last!
    SetIsRestored();
}
#endif // FEATURE_PREJIT

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
    g_IBCLogger.LogEEClassCOWTableAccess(this);
    GetClass_NoLogging()->SetOHDelegate(_ohDelegate);
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
        if (pParent->IsProjectedFromWinRT())
        {
            // skip all Com Import classes
            do
            {
                pParent = pParent->GetParentMethodTable();
                _ASSERTE(pParent != NULL);
            }while(pParent->IsComImport());

            // Now we have either System.__ComObject or WindowsRuntime.RuntimeClass
            if (pParent != g_pBaseCOMObject)
            {
                return pParent;
            }
        }
        else
        {
            // Skip the single ComImport class we expect
            _ASSERTE(pParent->GetParentMethodTable() != NULL);
            pParent = pParent->GetParentMethodTable();
        }
        _ASSERTE(!pParent->IsComImport());

        // Skip over System.__ComObject, expect System.MarshalByRefObject
        pParent=pParent->GetParentMethodTable();
        _ASSERTE(pParent != NULL);
#ifdef FEATURE_REMOTING
        _ASSERTE(pParent->IsMarshaledByRef());
#endif
        _ASSERTE(pParent->GetParentMethodTable() != NULL);
        _ASSERTE(pParent->GetParentMethodTable() == g_pObjectClass);
    }

    return pParent;
}

BOOL MethodTable::IsWinRTObjectType()
{
    LIMITED_METHOD_CONTRACT;

    // Try to determine if this object represents a WindowsRuntime object - i.e. is either
    // ProjectedFromWinRT or derived from a class that is

    if (!IsComObjectType())
        return FALSE;

    // Ideally we'd compute this once in BuildMethodTable and track it with another
    // flag, but we're now out of bits on m_dwFlags, and this is used very rarely
    // so for now we'll just recompute it when necessary.
    MethodTable* pMT = this;
    do
    {
        if (pMT->IsProjectedFromWinRT())
        {
            // Found a WinRT COM object
            return TRUE;
        }
        if (pMT->IsComImport())
        {
            // Found a class that is actually imported from COM but not WinRT
            // this is definitely a non-WinRT COM object
            return FALSE;
        }
        pMT = pMT->GetParentMethodTable();
    }while(pMT != NULL);

    return FALSE;
}

#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE

//==========================================================================================
// Return a pointer to the dictionary for an instantiated type
// Return NULL if not instantiated
Dictionary* MethodTable::GetDictionary()
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

    g_IBCLogger.LogMethodTableAccess(this);

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
        // This path should only be taken for the builtin mscorlib types
        // and primitive valuetypes
        ret = GetClass()->GetInternalCorElementType();
        _ASSERTE((ret != ELEMENT_TYPE_CLASS) && 
                    (ret != ELEMENT_TYPE_VALUETYPE));
        break;

    default:
        ret = ELEMENT_TYPE_CLASS;
        break;
    }

    // DAC may be targetting a dump; dumps do not guarantee you can retrieve the EEClass from
    // the MethodTable so this is not expected to work in a DAC build.
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    if (IsRestored_NoLogging())
    {
        PTR_EEClass pClass = GetClass_NoLogging();
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

    g_IBCLogger.LogMethodTableAccess(this);

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

    g_IBCLogger.LogMethodTableAccess(this);

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

    GetClass_NoLogging()->SetInternalCorElementType(_NormType);
    _ASSERTE(GetInternalCorElementType() == _NormType);
}

#endif // !DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
BOOL MethodTable::IsLegalWinRTType(OBJECTREF *poref)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame(poref));
        PRECONDITION(CheckPointer(poref));
        PRECONDITION((*poref) != NULL);
    }
    CONTRACTL_END

    if (IsArray())
    {
        BASEARRAYREF arrayRef = (BASEARRAYREF)(*poref);
        
        // WinRT array must be one-dimensional array with 0 lower-bound
        if (arrayRef->GetRank() == 1 && arrayRef->GetLowerBoundsPtr()[0] == 0)
        {
            MethodTable *pElementMT = ((BASEARRAYREF)(*poref))->GetArrayElementTypeHandle().GetMethodTable();

            // Element must be a legal WinRT type and not an array
            if (!pElementMT->IsArray() && pElementMT->IsLegalNonArrayWinRTType())
                return TRUE;
        }
        
        return FALSE;
    }
    else
    {
        // Non-Array version of IsLegalNonArrayWinRTType
        return IsLegalNonArrayWinRTType();
    }
}
#endif //#ifndef CROSSGEN_COMPILE

BOOL MethodTable::IsLegalNonArrayWinRTType()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!IsArray()); // arrays are not fully described by MethodTable
    }
    CONTRACTL_END

    if (WinRTTypeNameConverter::IsWinRTPrimitiveType(this))
        return TRUE;

    // Attributes are not legal
    MethodTable *pParentMT = GetParentMethodTable();
    if (pParentMT == MscorlibBinder::GetExistingClass(CLASS__ATTRIBUTE))
    {
        return FALSE;
    }

    bool fIsRedirected = false;
    if (!IsProjectedFromWinRT() && !IsExportedToWinRT())
    {
        // If the type is not primitive and not coming from .winmd, it can still be legal if
        // it's one of the redirected types (e.g. IEnumerable<T>).
        if (!WinRTTypeNameConverter::IsRedirectedType(this))
            return FALSE;

        fIsRedirected = true;
    }

    if (IsValueType())
    {
        if (!fIsRedirected)
        {
            // check fields
            ApproxFieldDescIterator fieldIterator(this, ApproxFieldDescIterator::INSTANCE_FIELDS);
            for (FieldDesc *pFD = fieldIterator.Next(); pFD != NULL; pFD = fieldIterator.Next())
            {
                TypeHandle thField = pFD->GetFieldTypeHandleThrowing(CLASS_LOAD_EXACTPARENTS);

                if (thField.IsTypeDesc())
                    return FALSE;
                
                MethodTable *pFieldMT = thField.GetMethodTable();

                // the only allowed reference types are System.String and types projected from WinRT value types
                if (!pFieldMT->IsValueType() && !pFieldMT->IsString())
                {
                    WinMDAdapter::RedirectedTypeIndex index;
                    if (!WinRTTypeNameConverter::ResolveRedirectedType(pFieldMT, &index))
                        return FALSE;

                    WinMDAdapter::WinMDTypeKind typeKind;
                    WinMDAdapter::GetRedirectedTypeInfo(index, NULL, NULL, NULL, NULL, NULL, &typeKind);
                    if (typeKind != WinMDAdapter::WinMDTypeKind_Struct && typeKind != WinMDAdapter::WinMDTypeKind_Enum)
                        return FALSE;
                }

                if (!pFieldMT->IsLegalNonArrayWinRTType())
                    return FALSE;
            }
        }
    }

    if (IsInterface() || IsDelegate() || (IsValueType() && fIsRedirected))
    {
        // interfaces, delegates, and redirected structures can be generic - check the instantiation
        if (HasInstantiation())
        {
            Instantiation inst = GetInstantiation();
            for (DWORD i = 0; i < inst.GetNumArgs(); i++)
            {
                // arrays are not allowed as generic arguments
                if (inst[i].IsArrayType())
                    return FALSE;

                if (inst[i].IsTypeDesc())
                    return FALSE;

                if (!inst[i].AsMethodTable()->IsLegalNonArrayWinRTType())
                    return FALSE;
            }
        }
    }
    else
    {
        // generic structures and runtime clases are not supported
        if (HasInstantiation())
            return FALSE;
    }

    return TRUE;
}

//==========================================================================================
// Returns the default WinRT interface if this is a WinRT class, NULL otherwise.
MethodTable *MethodTable::GetDefaultWinRTInterface()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    if (!IsProjectedFromWinRT() && !IsExportedToWinRT())
        return NULL;

    if (IsInterface())
        return NULL;

    // System.Runtime.InteropServices.WindowsRuntime.RuntimeClass is weird
    // It is ProjectedFromWinRT but isn't really a WinRT class
    if (this == g_pBaseRuntimeClass)
        return NULL;

    WinRTClassFactory *pFactory = ::GetComClassFactory(this)->AsWinRTClassFactory();
    return pFactory->GetDefaultInterface();
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
#ifndef DACCESS_COMPILE

WORD GetEquivalentMethodSlot(MethodTable * pOldMT, MethodTable * pNewMT, WORD wMTslot, BOOL *pfFound)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    MethodDesc * pMDRet = NULL;
    *pfFound = FALSE;

    // Get the COM vtable slot corresponding to the given MT slot
    WORD wVTslot;
    if (pOldMT->IsSparseForCOMInterop())
    {
        wVTslot = pOldMT->GetClass()->GetSparseCOMInteropVTableMap()->LookupVTSlot(wMTslot);
    }
    else
    {
        wVTslot = wMTslot;
    }
    
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
}
#endif // #ifdef DACCESS_COMPILE
#endif // #ifdef FEATURE_COMINTEROP

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
                MethodTable * pCurEntryType = LookupDispatchMapType(pCurEntry->GetTypeID());
                if (pCurEntryType == dispatchTokenType)
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
        g_IBCLogger.LogMethodTableAccess(pCurMT);
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
    DispatchSlot * pImplSlot)
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
                MethodTable *pIfcMT = GetThread()->GetDomain()->LookupType(typeID);

                // Quick sanity check
                if (!(pIfcMT->HasInstantiation()))
                {
                    _ASSERTE(!"Should not have gotten here. If you did, it's probably because multiple interface instantiation hasn't been checked in yet. This code only works on top of that.");
                    RETURN(FALSE);
                }

                // Get the type of T (as in IList<T>)
                TypeHandle theT = pIfcMT->GetInstantiation()[0];

                // Figure out which method of IList<T> the caller requested.
                MethodDesc * pIfcMD = pIfcMT->GetMethodDescForSlot(slotNumber);

                // Retrieve the corresponding method of SZArrayHelper. This is the guy that will actually execute.
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

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlot(UINT32 typeID, UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    DispatchSlot implSlot(NULL);
    FindDispatchImpl(typeID, slotNumber, &implSlot);
    return implSlot;
}

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlot(DispatchToken tok)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    return FindDispatchSlot(tok.GetTypeID(), tok.GetSlotNumber());
}

#ifndef DACCESS_COMPILE

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlotForInterfaceMD(MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(pMD));
    CONSISTENCY_CHECK(pMD->IsInterface());
    return FindDispatchSlotForInterfaceMD(TypeHandle(pMD->GetMethodTable()), pMD);
}

//==========================================================================================
DispatchSlot MethodTable::FindDispatchSlotForInterfaceMD(TypeHandle ownerType, MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(!ownerType.IsNull());
    CONSISTENCY_CHECK(CheckPointer(pMD));
    CONSISTENCY_CHECK(pMD->IsInterface());
    return FindDispatchSlot(ownerType.GetMethodTable()->GetTypeID(), pMD->GetSlot());
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
        SO_TOLERANT;
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
            if (LookupDispatchMapType(pCurEntry->GetTypeID()) == pItfMT)
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

//==========================================================================================
BOOL MethodTable::HasSameInterfaceImplementationAsParent(MethodTable *pItfMT, MethodTable *pParentMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(!IsInterface() && !pParentMT->IsInterface());
        PRECONDITION(pItfMT->IsInterface());
    } CONTRACTL_END;

    if (!ImplementsInterfaceWithSameSlotsAsParent(pItfMT, pParentMT))
    {
        // if the slots are not same, this class reimplements the interface
        return FALSE;
    }

    // The target slots are the same, but they can still be overriden. We'll iterate
    // the dispatch map beginning with pParentMT up the hierarchy and for each pItfMT
    // entry check the target slot contents (pParentMT vs. this class). A mismatch
    // means that there is an override. We'll keep track of source (interface) slots
    // we have seen so that we can ignore entries higher in the hierarchy that are no
    // longer in effect at pParentMT level.
    BitMask bitMask;
    
    WORD wSeenSlots = 0;
    WORD wTotalSlots = pItfMT->GetNumVtableSlots();

    MethodTable *pMT = pParentMT;
    do
    {
        DispatchMap::EncodedMapIterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            DispatchMapEntry *pCurEntry = it.Entry();
            if (LookupDispatchMapType(pCurEntry->GetTypeID()) == pItfMT)
            {
                UINT32 ifaceSlot = pCurEntry->GetSlotNumber();
                if (!bitMask.TestBit(ifaceSlot))
                {
                    bitMask.SetBit(ifaceSlot);

                    UINT32 targetSlot = pCurEntry->GetTargetSlotNumber();
                    if (GetRestoredSlot(targetSlot) != pParentMT->GetRestoredSlot(targetSlot))
                    {
                        // the target slot is overriden
                        return FALSE;
                    }

                    if (++wSeenSlots == wTotalSlots)
                    {
                        // we've resolved all slots, no reason to continue
                        break;
                    }
                }
            }
        }
        pMT = pMT->GetParentMethodTable();
    }
    while (pMT != NULL);

    return TRUE;
}

#endif // !DACCESS_COMPILE

//==========================================================================================
MethodTable * MethodTable::LookupDispatchMapType(DispatchMapTypeID typeID)
{
    CONTRACTL {
        WRAPPER(THROWS);
        GC_TRIGGERS;
    } CONTRACTL_END;

    _ASSERTE(!typeID.IsThisClass());
    
    InterfaceMapIterator intIt = IterateInterfaceMapFrom(typeID.GetInterfaceNum());
    return intIt.GetInterface();
}

//==========================================================================================
MethodDesc * MethodTable::GetIntroducingMethodDesc(DWORD slotNumber)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
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
//
// Redirected WinRT types may have two GUIDs, the "classic" one which matches the return value
// of Type.Guid, and the new one which is the GUID of the WinRT type to which it is redirected.
// The bClassic parameter controls which one is returned from this method. Note that the parameter
// is ignored for genuine WinRT types, i.e. types loaded from .winmd files, those always return
// the new GUID.
//
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
    PTR_GuidInfo pGuidInfo = (bClassic ? GetClass()->GetGuidInfo() : GetGuidInfo());
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

    // Use the per-EEClass GuidInfo if we are asked for the "classic" non-WinRT GUID of non-WinRT type
    GuidInfo *pInfo = ((bClassic && !IsProjectedFromWinRT()) ? GetClass()->GetGuidInfo() : GetGuidInfo());

    // First check to see if we have already cached the guid for this type.
    // We currently only cache guids on interfaces and WinRT delegates.
    // In classic mode, though, ensure we don't retrieve the GuidInfo for redirected interfaces
    if ((IsInterface() || IsWinRTDelegate()) && pInfo != NULL
        && (!bClassic || !SupportsGenericInterop(TypeHandle::Interop_NativeToManaged, modeRedirected)))
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

#ifdef FEATURE_COMINTEROP
    if ((SupportsGenericInterop(TypeHandle::Interop_NativeToManaged, modeProjected))
        || (!bClassic
             && SupportsGenericInterop(TypeHandle::Interop_NativeToManaged, modeRedirected)
             && IsLegalNonArrayWinRTType()))
    {
        // Closed generic WinRT interfaces/delegates have their GUID computed
        // based on the "PIID" in metadata and the instantiation.
        // Note that we explicitly do this computation for redirected mscorlib
        // interfaces only if !bClassic, so typeof(Enumerable<T>).GUID 
        // for example still returns the same result as pre-v4.5 runtimes.
        // ComputeGuidForGenericType() may throw for generics nested beyond 64 levels.
        WinRTGuidGenerator::ComputeGuidForGenericType(this, pGuid);

        // This GUID is per-instantiation so make sure that the cache
        // where we are going to keep it is per-instantiation as well.
        _ASSERTE(IsCanonicalMethodTable() || HasGuidInfo());
    }
    else
#endif // FEATURE_COMINTEROP
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
            g_IBCLogger.LogEEClassCOWTableAccess(this);
            GetClass_NoLogging()->SetHasNoGuid();
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
            cchName = wcslen(szName);

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
    if ((IsInterface() || IsWinRTDelegate()) && (pInfo == NULL) && (*pGuid != GUID_NULL)
#ifdef FEATURE_COMINTEROP
        && !(bClassic
             && SupportsGenericInterop(TypeHandle::Interop_NativeToManaged, modeRedirected)
             && IsLegalNonArrayWinRTType())
#endif // FEATURE_COMINTEROP
        )
    {
        AllocMemTracker amTracker;
        BOOL bStoreGuidInfoOnEEClass = false;
        PTR_LoaderAllocator pLoaderAllocator;

#if FEATURE_COMINTEROP
        if ((bClassic && !IsProjectedFromWinRT()) || !HasGuidInfo())
        {
            bStoreGuidInfoOnEEClass = true;
        }
#else
        // We will always store the GuidInfo on the methodTable.
        bStoreGuidInfoOnEEClass = true;
#endif
        if(bStoreGuidInfoOnEEClass)
        {
            // Since the GUIDInfo will be stored on the EEClass, 
            // the memory should be allocated on the loaderAllocator of the class.
            // The definining module and the loaded module could be different in some scenarios.
            // For example - in case of shared generic instantiations 
            // a shared generic i.e. System.__Canon which would be loaded in shared domain
            // but the this->GetLoaderAllocator will be the loader allocator for the definining
            // module which can get unloaded anytime.
            _ASSERTE(GetClass());
            _ASSERTE(GetClass()->GetMethodTable());
            pLoaderAllocator = GetClass()->GetMethodTable()->GetLoaderAllocator();
        }
        else
        {
            pLoaderAllocator = GetLoaderAllocator();
        }

        _ASSERTE(pLoaderAllocator);

        // Allocate the guid information.
        pInfo = (GuidInfo *)amTracker.Track(
            pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(GuidInfo))));
        pInfo->m_Guid = *pGuid;
        pInfo->m_bGeneratedFromName = bGenerated;

        // Set in in the interface method table.
        if (bClassic && !IsProjectedFromWinRT())
        {
            // Set the per-EEClass GuidInfo if we are asked for the "classic" non-WinRT GUID.
            // The MethodTable may be NGENed and read-only - and there's no point in saving
            // classic GUIDs in non-WinRT MethodTables anyway.
            _ASSERTE(bStoreGuidInfoOnEEClass);
            GetClass()->SetGuidInfo(pInfo);
        }
        else
        {
#if FEATURE_COMINTEROP
            _ASSERTE(bStoreGuidInfoOnEEClass || HasGuidInfo());
#else
            _ASSERTE(bStoreGuidInfoOnEEClass);
#endif
            SetGuidInfo(pInfo);
        }

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
        SO_TOLERANT;
        POSTCONDITION(CheckPointer(RETVAL, NULL_NOT_OK));
        POSTCONDITION(RETVAL->m_pDebugMethodTable.IsNull() || // We must be in BuildMethdTableThrowing()
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
        SO_TOLERANT;
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
        if (IsAsyncPinType())
        {
            return TRUE;
        }
        else
        {
            return FALSE;
        }
    }

    EEClass * pClass = GetClass();
    MethodTable * pCanonMT = pClass->GetMethodTable();

    // Let's try to make sure we have a valid EEClass pointer.
    if (pCanonMT == NULL)
        return FALSE;

    if (GetNumGenericArgs() != 0)
        return (pCanonMT->GetClass() == pClass);
    else
        return (pCanonMT == this) || IsArray() || IsTransparentProxy();
}

//==========================================================================================

// Structs containing GC pointers whose size is at most this are always stack-allocated.
const unsigned MaxStructBytesForLocalVarRetBuffBytes = 2 * sizeof(void*);  // 4 pointer-widths.

BOOL MethodTable::IsStructRequiringStackAllocRetBuf()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Disable this optimization. It has limited value (only kicks in on x86, and only for less common structs),
    // causes bugs and introduces odd ABI differences not compatible with ReadyToRun.
    return FALSE;
}

//==========================================================================================
unsigned MethodTable::GetTypeDefRid()
{
    LIMITED_METHOD_DAC_CONTRACT;

    g_IBCLogger.LogMethodTableAccess(this);
    return GetTypeDefRid_NoLogging();
}

//==========================================================================================
unsigned MethodTable::GetTypeDefRid_NoLogging()
{
    LIMITED_METHOD_DAC_CONTRACT;

    WORD token = m_wToken;

    if (token == METHODTABLE_TOKEN_OVERFLOW)
        return (unsigned)*GetTokenOverflowPtr();

    return token;
}

//==========================================================================================
void MethodTable::SetCl(mdTypeDef token)
{
    LIMITED_METHOD_CONTRACT;

    unsigned rid = RidFromToken(token);
    if (rid >= METHODTABLE_TOKEN_OVERFLOW)
    {
        m_wToken = METHODTABLE_TOKEN_OVERFLOW;
        *GetTokenOverflowPtr() = rid;
    }
    else
    {
        _ASSERTE(FitsIn<U2>(rid));
        m_wToken = (WORD)rid;        
    }

    _ASSERTE(GetCl() == token);
}

//==========================================================================================
MethodDesc * MethodTable::GetClassConstructor()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
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
WORD MethodTable::GetNumHandleRegularStatics()
{
    LIMITED_METHOD_CONTRACT;

    return GetClass()->GetNumHandleRegularStatics();
}

//==========================================================================================
WORD MethodTable::GetNumBoxedRegularStatics()
{
    LIMITED_METHOD_CONTRACT;

    return GetClass()->GetNumBoxedRegularStatics();
}

//==========================================================================================
WORD MethodTable::GetNumBoxedThreadStatics ()
{
    LIMITED_METHOD_CONTRACT;

    return GetClass()->GetNumBoxedThreadStatics();
}

//==========================================================================================
ClassCtorInfoEntry* MethodTable::GetClassCtorInfoIfExists()
{
    LIMITED_METHOD_CONTRACT;

    if (!IsZapped())
        return NULL;

    g_IBCLogger.LogCCtorInfoReadAccess(this);

    if (HasBoxedRegularStatics())
    {
        ModuleCtorInfo *pModuleCtorInfo = GetZapModule()->GetZapModuleCtorInfo();
        DPTR(PTR_MethodTable) ppMT = pModuleCtorInfo->ppMT;
        PTR_DWORD hotHashOffsets = pModuleCtorInfo->hotHashOffsets;
        PTR_DWORD coldHashOffsets = pModuleCtorInfo->coldHashOffsets;

        if (pModuleCtorInfo->numHotHashes)
        {
            DWORD hash = pModuleCtorInfo->GenerateHash(PTR_MethodTable(this), ModuleCtorInfo::HOT);
            _ASSERTE(hash < pModuleCtorInfo->numHotHashes);

            for (DWORD i = hotHashOffsets[hash]; i != hotHashOffsets[hash + 1]; i++)
            {
                _ASSERTE(ppMT[i]);
                if (dac_cast<TADDR>(ppMT[i]) == dac_cast<TADDR>(this))
                {
                    return pModuleCtorInfo->cctorInfoHot + i;
                }
            }
        }

        if (pModuleCtorInfo->numColdHashes)
        {
            DWORD hash = pModuleCtorInfo->GenerateHash(PTR_MethodTable(this), ModuleCtorInfo::COLD);
            _ASSERTE(hash < pModuleCtorInfo->numColdHashes);

            for (DWORD i = coldHashOffsets[hash]; i != coldHashOffsets[hash + 1]; i++)
            {
                _ASSERTE(ppMT[i]);
                if (dac_cast<TADDR>(ppMT[i]) == dac_cast<TADDR>(this))
                {
                    return pModuleCtorInfo->cctorInfoCold + (i - pModuleCtorInfo->numElementsHot);
                }
            }
        }
    }

    return NULL;
}

#ifdef _DEBUG
//==========================================================================================
// Returns true if pointer to the parent method table has been initialized/restored already.
BOOL MethodTable::IsParentMethodTablePointerValid()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // workaround: Type loader accesses partially initialized datastructures that interferes with IBC logging.
    // Once type loader is fixed to do not access partially  initialized datastructures, this can go away.
    if (!GetWriteableData_NoLogging()->IsParentMethodTablePointerValid())
        return FALSE;

    if (!GetFlag(enum_flag_HasIndirectParent))
    {
        return TRUE;
    }
    TADDR pMT;
    pMT = *PTR_TADDR(m_pParentMethodTable + offsetof(MethodTable, m_pParentMethodTable));
    return !CORCOMPILE_IS_POINTER_TAGGED(pMT);
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
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pWhichParent));
        PRECONDITION(IsRestored_NoLogging());
        PRECONDITION(pWhichParent->IsRestored_NoLogging());
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
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pWhichParent));
        PRECONDITION(IsRestored_NoLogging());
        PRECONDITION(pWhichParent->IsRestored_NoLogging());
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
    return GetDomain()->LookupComInteropData(this);
}

//==========================================================================================
// Returns TRUE if successfully inserted, FALSE if this would be a duplicate entry
BOOL MethodTable::InsertComInteropData(InteropMethodTableData *pData)
{
    WRAPPER_NO_CONTRACT;
    return GetDomain()->InsertComInteropData(this, pData);
}

//==========================================================================================
InteropMethodTableData *MethodTable::CreateComInteropData(AllocMemTracker *pamTracker)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(GetParentMethodTable() == NULL || GetParentMethodTable()->LookupComInteropData() != NULL);
    } CONTRACTL_END;

    ClassCompat::MethodTableBuilder builder(this);

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
    MethodDataEntry *         rgWorkingData)
{
    LIMITED_METHOD_CONTRACT;

    for (DispatchMap::EncodedMapIterator it(pMT); it.IsValid(); it.Next())
    {
        for (UINT32 nTypeIDIndex = 0; nTypeIDIndex < cTypeIDs; nTypeIDIndex++)
        {
            if (it.Entry()->GetTypeID() == rgTypeIDs[nTypeIDIndex])
            {
                UINT32 curSlot = it.Entry()->GetSlotNumber();
                // If we're processing an interface, or it's for a virtual, or it's for a non-virtual
                // for the most derived type, we want to process the entry. In other words, we
                // want to ignore non-virtuals for parent classes.
                if ((curSlot < pMT->GetNumVirtuals()) || (iCurrentChainDepth == 0))
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
UINT32 MethodTable::MethodDataObject::GetObjectSize(MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;
    UINT32 cb = sizeof(MethodTable::MethodDataObject);
    cb += pMT->GetCanonicalMethodTable()->GetNumMethods() * sizeof(MethodDataObjectEntry);
    return cb;
}

//==========================================================================================
// This will fill in all the MethodEntry slots present in the current MethodTable
void MethodTable::MethodDataObject::Init(MethodTable *pMT, MethodData *pParentData)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pParentData, NULL_OK));
        PRECONDITION(!pMT->IsInterface());
        PRECONDITION(pParentData == NULL ||
                     (pMT->ParentEquals(pParentData->GetDeclMethodTable()) &&
                      pMT->ParentEquals(pParentData->GetImplMethodTable())));
    } CONTRACTL_END;

    m_pMT = pMT;
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
    MethodTable *pMTCur = m_pMT;
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

    if (m_containsMethodImpl && pMT != m_pMT)
        return;

    unsigned nVirtuals = pMT->GetNumVirtuals();

    MethodTable::IntroducedMethodIterator it(pMT, FALSE);
    for (; it.IsValid(); it.Next())
    {
        MethodDesc * pMD = it.GetMethodDesc();
        g_IBCLogger.LogMethodDescAccess(pMD);

        unsigned slot = pMD->GetSlot();
        if (slot == MethodTable::NO_SLOT)
            continue;

        // We want to fill all methods introduced by the actual type we're gathering 
        // data for, and the virtual methods of the parent and above
        if (pMT == m_pMT)
        {
            if (m_containsMethodImpl && slot < nVirtuals)
                continue;
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
    return DispatchSlot(m_pMT->GetRestoredSlot(slotNumber));
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
        pMDRet = m_pMT->GetMethodDescForSlot(slotNumber);
        _ASSERTE(CheckPointer(pMDRet));
        pEntry->SetImplMethodDesc(pMDRet);
    }
    else
    {
        _ASSERTE(slotNumber >= GetNumVirtuals() || pMDRet == m_pMT->GetMethodDescForSlot(slotNumber));
    }

    return pMDRet;
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
    return m_pMT->GetMethodDescForSlot(slotNumber);
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
    cb += pMTDecl->GetNumMethods() * sizeof(MethodDataEntry);
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
    MethodData *              pImpl)
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
        ProcessMap(m_rgDeclTypeIDs, m_cDeclTypeIDs, pMTCur, iChainDepth, GetEntryData());
    }
    else
    {   // We should decode all interface duplicates of code:m_pDecl
        MethodTable * pDeclMT = m_pDecl->GetImplMethodTable();
        INDEBUG(BOOL dbg_fInterfaceFound = FALSE);
        
        // Call code:ProcessMap for every (duplicate) occurence of interface code:pDeclMT in the interface 
        // map of code:m_pImpl
        MethodTable::InterfaceMapIterator it = m_pImpl->GetImplMethodTable()->IterateInterfaceMap();
        while (it.Next())
        {
            if (pDeclMT == it.GetInterface())
            {   // We found the interface
                INDEBUG(dbg_fInterfaceFound = TRUE);
                DispatchMapTypeID declTypeID = DispatchMapTypeID::InterfaceClassID(it.GetIndex());
                
                ProcessMap(&declTypeID, 1, pMTCur, iChainDepth, GetEntryData());
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
    return m_pImpl->GetImplMethodDesc(MapToImplSlotNumber(slotNumber));
}

//==========================================================================================
void MethodTable::MethodDataInterfaceImpl::InvalidateCachedVirtualSlot(UINT32 slotNumber)
{
    WRAPPER_NO_CONTRACT;
    UINT32 implSlotNumber = MapToImplSlotNumber(slotNumber);
    if (implSlotNumber == INVALID_SLOT_NUMBER) {
        return;
    }
    return m_pImpl->InvalidateCachedVirtualSlot(MapToImplSlotNumber(slotNumber));
}

//==========================================================================================
void MethodTable::CheckInitMethodDataCache()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;
    if (s_pMethodDataCache == NULL)
    {
        UINT32 cb = MethodDataCache::GetObjectSize(8);
        NewHolder<BYTE> hb(new BYTE[cb]);
        MethodDataCache *pCache = new (hb.GetValue()) MethodDataCache(8);
        if (InterlockedCompareExchangeT(
                &s_pMethodDataCache, pCache, NULL) == NULL)
        {
            hb.SuppressRelease();
        }
        // If somebody beat us, return and allow the holders to take care of cleanup.
        else
        {
            return;
        }
    }
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
        CONSISTENCY_CHECK(s_fUseMethodDataCache);
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
    if (s_fUseMethodDataCache && s_fUseParentMethodData) {
        if (!pMT->IsInterface()) {
            //@todo : this won't be correct for non-shared code
            MethodTable *pMTParent = pMT->GetParentMethodTable();
            if (pMTParent != NULL) {
                pData = FindMethodDataHelper(pMTParent, pMTParent);
            }
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
    MethodTable *             pMTImpl)
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
    MethodDataWrapper hDecl(GetMethodData(pMTDecl, FALSE));
    MethodDataWrapper hImpl(GetMethodData(pMTImpl, FALSE));

    UINT32 cb = MethodDataInterfaceImpl::GetObjectSize(pMTDecl);
    NewHolder<BYTE> pb(new BYTE[cb]);
    MethodDataInterfaceImpl * pData = new (pb.GetValue()) MethodDataInterfaceImpl(rgDeclTypeIDs, cDeclTypeIDs, hDecl, hImpl);
    pb.SuppressRelease();

    return pData;
} // MethodTable::GetMethodDataHelper

//==========================================================================================
// The fCanCache argument determines if the resulting MethodData object can
// be added to the global MethodDataCache. This is used when requesting a
// MethodData object for a type currently being built.
MethodTable::MethodData *MethodTable::GetMethodDataHelper(MethodTable *pMTDecl,
                                                          MethodTable *pMTImpl,
                                                          BOOL fCanCache)
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

    if (s_fUseMethodDataCache) {
        MethodData *pData = FindMethodDataHelper(pMTDecl, pMTImpl);
        if (pData != NULL) {
            return pData;
        }
    }

    // If we get here, there are no entries in the cache.
    MethodData *pData = NULL;
    if (pMTDecl == pMTImpl) {
        if (pMTDecl->IsInterface()) {
            pData = new MethodDataInterface(pMTDecl);
        }
        else {
            UINT32 cb = MethodDataObject::GetObjectSize(pMTDecl);
            NewHolder<BYTE> pb(new BYTE[cb]);
            MethodDataHolder h(FindParentMethodDataHelper(pMTDecl));
            pData = new (pb.GetValue()) MethodDataObject(pMTDecl, h.GetValue());
            pb.SuppressRelease();
        }
    }
    else {
        pData = GetMethodDataHelper(
            NULL, 
            0, 
            pMTDecl, 
            pMTImpl);
    }

    // Insert in the cache if it is active.
    if (fCanCache && s_fUseMethodDataCache) {
        s_pMethodDataCache->Insert(pData);
    }

    // Do not AddRef, already initialized to 1.
    return pData;
}

//==========================================================================================
// The fCanCache argument determines if the resulting MethodData object can
// be added to the global MethodDataCache. This is used when requesting a
// MethodData object for a type currently being built.
MethodTable::MethodData *MethodTable::GetMethodData(MethodTable *pMTDecl,
                                                    MethodTable *pMTImpl,
                                                    BOOL fCanCache)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
    } CONTRACTL_END;

    MethodDataWrapper hData(GetMethodDataHelper(pMTDecl, pMTImpl, fCanCache));
    hData.SuppressRelease();
    return hData;
}

//==========================================================================================
// This method does not cache the resulting MethodData object in the global MethodDataCache.
MethodTable::MethodData *
MethodTable::GetMethodData(
    const DispatchMapTypeID * rgDeclTypeIDs, 
    UINT32                    cDeclTypeIDs, 
    MethodTable *             pMTDecl, 
    MethodTable *             pMTImpl)
{
    CONTRACTL {
        THROWS;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(pMTDecl != pMTImpl);
        PRECONDITION(pMTDecl->IsInterface());
        PRECONDITION(!pMTImpl->IsInterface());
    } CONTRACTL_END;

    MethodDataWrapper hData(GetMethodDataHelper(rgDeclTypeIDs, cDeclTypeIDs, pMTDecl, pMTImpl));
    hData.SuppressRelease();
    return hData;
}

//==========================================================================================
// The fCanCache argument determines if the resulting MethodData object can
// be added to the global MethodDataCache. This is used when requesting a
// MethodData object for a type currently being built.
MethodTable::MethodData *MethodTable::GetMethodData(MethodTable *pMT,
                                                    BOOL fCanCache)
{
    WRAPPER_NO_CONTRACT;
    return GetMethodData(pMT, pMT, fCanCache);
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

    LOG((LF_LOADER, LL_INFO10000, "SD: MT::MethodIterator created for %s.\n", pMTDecl->GetDebugClassName()));

    m_pMethodData = MethodTable::GetMethodData(pMTDecl, pMTImpl);
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
PTR_GuidInfo MethodTable::GetGuidInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    if (HasGuidInfo())
    {
        return *GetGuidInfoPtr();
    }
#endif // FEATURE_COMINTEROP
    _ASSERTE(GetClass());
    return GetClass()->GetGuidInfo();
}

//==========================================================================================
void MethodTable::SetGuidInfo(GuidInfo* pGuidInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
    if (HasGuidInfo())
    {
        *EnsureWritablePages(GetGuidInfoPtr()) = pGuidInfo;
        return;
    }
#endif // FEATURE_COMINTEROP
    _ASSERTE(GetClass());
    GetClass()->SetGuidInfo (pGuidInfo);

#endif // DACCESS_COMPILE
}

#if defined(FEATURE_COMINTEROP) && !defined(DACCESS_COMPILE)

//==========================================================================================
RCWPerTypeData *MethodTable::CreateRCWPerTypeData(bool bThrowOnOOM)
{
    CONTRACTL
    {
        if (bThrowOnOOM) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(HasRCWPerTypeData());
    }
    CONTRACTL_END;

    AllocMemTracker amTracker;

    RCWPerTypeData *pData;
    if (bThrowOnOOM)
    {
        TaggedMemAllocPtr ptr = GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(RCWPerTypeData)));
        pData = (RCWPerTypeData *)amTracker.Track(ptr);
    }
    else
    {
        TaggedMemAllocPtr ptr = GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem_NoThrow(S_SIZE_T(sizeof(RCWPerTypeData)));
        pData = (RCWPerTypeData *)amTracker.Track_NoThrow(ptr);
        if (pData == NULL)
        {
            return NULL;
        }
    }

    // memory is zero-inited which means that nothing has been computed yet
    _ASSERTE(pData->m_dwFlags == 0);

    RCWPerTypeData **pDataPtr = GetRCWPerTypeDataPtr();

    if (bThrowOnOOM)
    {
        EnsureWritablePages(pDataPtr);
    }
    else
    {
        if (!EnsureWritablePagesNoThrow(pDataPtr, sizeof(*pDataPtr)))
        {
            return NULL;
        }
    }

    if (InterlockedCompareExchangeT(pDataPtr, pData, NULL) == NULL)
    {
        amTracker.SuppressRelease();
    }
    else
    {
        // another thread already published the pointer
        pData = *pDataPtr;
    }

    return pData;
}

//==========================================================================================
RCWPerTypeData *MethodTable::GetRCWPerTypeData(bool bThrowOnOOM /*= true*/)
{
    CONTRACTL
    {
        if (bThrowOnOOM) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!HasRCWPerTypeData())
        return NULL;

    RCWPerTypeData *pData = *GetRCWPerTypeDataPtr();
    if (pData == NULL)
    {
        // creation is factored out into a separate routine to avoid paying the EH cost here
        pData = CreateRCWPerTypeData(bThrowOnOOM);
    }
   
    return pData;
}

#endif // FEATURE_COMINTEROP && !DACCESS_COMPILE

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
// Optimization intended for EnsureInstanceActive, IsIntrospectionOnly, EnsureActive only
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
        // This is going to go recursive, so we need to use an interior stack probe
        
        INTERIOR_STACK_PROBE(GetThread());
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
        END_INTERIOR_STACK_PROBE;
    }

}
#endif //!DACCESS_COMPILE

//==========================================================================================
BOOL MethodTable::IsIntrospectionOnly()
{
    WRAPPER_NO_CONTRACT;
    return GetAssembly()->IsIntrospectionOnly();
}

//==========================================================================================
BOOL MethodTable::ContainsIntrospectionOnlyTypes()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    // check this type
    if (IsIntrospectionOnly())
        return TRUE;

    // check the instantiation
    Instantiation inst = GetInstantiation();
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        CONSISTENCY_CHECK(!inst[i].IsEncodedFixup());
        if (inst[i].ContainsIntrospectionOnlyTypes())
            return TRUE;
    }

    return FALSE;
}

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

    if (!IsCanonicalMethodTable())
    {
        PTR_MethodTable pMTCanonical = GetCanonicalMethodTable();

        if (pMTCanonical.IsValid())
        {
            pMTCanonical->EnumMemoryRegions(flags);
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
    }

    PTR_MethodTable pMTParent = GetParentMethodTable();

    if (pMTParent.IsValid())
    {
        pMTParent->EnumMemoryRegions(flags);
    }

    if (HasNonVirtualSlotsArray())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(GetNonVirtualSlotsArray()), GetNonVirtualSlotsArraySize());
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
        DacEnumMemoryRegion(dac_cast<TADDR>(GetDictionary()), GetInstAndDictSize());
    }

    VtableIndirectionSlotIterator it = IterateVtableIndirectionSlots();
    while (it.Next())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(it.GetIndirectionSlot()), it.GetSize());
    }

    if (m_pWriteableData.IsValid())
    {
        m_pWriteableData.EnumMem();
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
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
        CONSISTENCY_CHECK(!inst[i].IsEncodedFixup());
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
            // Encoded fixups are never open types
            if (!inst[i].IsEncodedFixup())
            {
                Module *pModule = inst[i].GetDefiningModuleForOpenType();
                if (pModule != NULL)
                    RETURN pModule;
            }
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
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Keep in sync with code:MethodTable::GetRestoredSlotMT
    //

    MethodTable * pMT = this;
    while (true)
    {
        g_IBCLogger.LogMethodTableAccess(pMT);

        pMT = pMT->GetCanonicalMethodTable();

        _ASSERTE(pMT != NULL);

        PCODE slot = pMT->GetSlot(slotNumber);

        if ((slot != NULL)
#ifdef FEATURE_PREJIT
            && !pMT->GetLoaderModule()->IsVirtualImportThunk(slot)
#endif
            )
        {
            return slot;
        }

        // This is inherited slot that has not been fixed up yet. Find
        // the value by walking up the inheritance chain
        pMT = pMT->GetParentMethodTable();
    }
}

//==========================================================================================
MethodTable * MethodTable::GetRestoredSlotMT(DWORD slotNumber)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Keep in sync with code:MethodTable::GetRestoredSlot
    //

    MethodTable * pMT = this;
    while (true)
    {
        g_IBCLogger.LogMethodTableAccess(pMT);

        pMT = pMT->GetCanonicalMethodTable();

        _ASSERTE(pMT != NULL);

        PCODE slot = pMT->GetSlot(slotNumber);

        if ((slot != NULL)
#ifdef FEATURE_PREJIT
            && !pMT->GetLoaderModule()->IsVirtualImportThunk(slot)
#endif
            )
        {
            return pMT;
        }

        // This is inherited slot that has not been fixed up yet. Find
        // the value by walking up the inheritance chain
        pMT = pMT->GetParentMethodTable();
    }
}

//==========================================================================================
MethodDesc * MethodTable::GetParallelMethodDesc(MethodDesc * pDefMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
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
#ifndef FEATURE_INTERPRETER
            // TBD: Make this take a "stable" debug arg, determining whether to make these assertions.
            _ASSERTE(pMD->HasStableEntryPoint());
            _ASSERTE(pMD->GetStableEntryPoint() == slotCode);
#endif // FEATURE_INTERPRETER
        }
    }
#endif

    // IBC logging is not needed here - slots in ngen images are immutable.

#ifdef _TARGET_ARM_
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
MethodDesc *MethodTable::GetDefaultConstructor()
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
                                                        FALSE /* no BoxedEntryPointStub */,
                                                        Instantiation(), /* no method instantiation */
                                                        FALSE /* no allowInstParam */);
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
    {
        // Sometimes (when compiling shared generic code)
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
            TypeHandle thPotentialInterfaceType(it.GetInterface());
            if (thPotentialInterfaceType.AsMethodTable()->GetCanonicalMethodTable() == 
                thInterfaceType.AsMethodTable()->GetCanonicalMethodTable())
            {
                cPotentialMatchingInterfaces++;
                pMD = pCanonMT->GetMethodDescForInterfaceMethod(thPotentialInterfaceType, pGenInterfaceMD);
                
                // See code:#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
                if ((pMD != NULL) && !pMD->GetMethodTable()->IsValueType())
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
                    pMD = this->GetMethodDescForInterfaceMethod(pInterfaceMT, pInterfaceMD);
                    _ASSERTE(pMD != NULL);
                    fIsExactMethodResolved = TRUE;
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
                pMD = pCanonMT->GetMethodDescForInterfaceMethod(thInterfaceType, pGenInterfaceMD);
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
    
    //#TryResolveConstraintMethodApprox_DoNotReturnParentMethod
    // Only return a method if the value type itself declares the method, 
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
        FALSE /* no BoxedEntryPointStub */ ,
        pInterfaceMD->GetMethodInstantiation(),
        FALSE /* no allowInstParam */ );
    
    // FindOrCreateAssociatedMethodDesc won't return an BoxedEntryPointStub.
    _ASSERTE(pMD != NULL);
    _ASSERTE(!pMD->IsUnboxingStub());
    
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

#ifdef FEATURE_REMOTING
//==========================================================================================
// context static functions
void MethodTable::SetupContextStatics(AllocMemTracker *pamTracker, WORD wContextStaticsSize)
{
    STANDARD_VM_CONTRACT;

    ContextStaticsBucket* pCSInfo = (ContextStaticsBucket*) pamTracker->Track(GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(ContextStaticsBucket))));
    *(GetContextStaticsBucketPtr()) = pCSInfo;

    pCSInfo->m_dwContextStaticsOffset = (DWORD)-1; // Initialized lazily
    pCSInfo->m_wContextStaticsSize = wContextStaticsSize;
}

#ifndef CROSSGEN_COMPILE
//==========================================================================================
DWORD MethodTable::AllocateContextStaticsOffset()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    g_IBCLogger.LogMethodTableWriteableDataWriteAccess(this);

    BaseDomain* pDomain = IsDomainNeutral() ?  SystemDomain::System() : GetDomain();

    ContextStaticsBucket* pCSInfo = GetContextStaticsBucket();
    DWORD* pOffsetSlot = &pCSInfo->m_dwContextStaticsOffset;

    return pDomain->AllocateContextStaticsOffset(pOffsetSlot);
}
#endif // CROSSGEN_COMPILE

#endif // FEATURE_REMOTING

bool MethodTable::ClassRequiresUnmanagedCodeCheck()
{
    LIMITED_METHOD_CONTRACT;
    
#ifdef FEATURE_CORECLR
    return false;
#else
    // all WinRT types have an imaginary [SuppressUnmanagedCodeSecurity] attribute on them
    if (IsProjectedFromWinRT())
        return false;
        
    // In AppX processes, there is only one full trust AppDomain, so there is never any need to do a security
    // callout on interop stubs
    if (AppX::IsAppXProcess())
        return false;

    return GetMDImport()->GetCustomAttributeByName(GetCl(),
                                                 COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI,
                                                 NULL,
                                                 NULL) == S_FALSE;
#endif // FEATURE_CORECLR
}

#endif // !DACCESS_COMPILE



BOOL MethodTable::Validate()
{
    LIMITED_METHOD_CONTRACT;

    ASSERT_AND_CHECK(SanityCheck());
    
#ifdef _DEBUG    
    if (m_pWriteableData == NULL)
    {
        _ASSERTE(IsAsyncPinType());
        return TRUE;
    }

    DWORD dwLastVerifiedGCCnt = m_pWriteableData->m_dwLastVerifedGCCnt;
    // Here we used to assert that (dwLastVerifiedGCCnt <= GCHeap::GetGCHeap()->GetGcCount()) but 
    // this is no longer true because with background gc. Since the purpose of having 
    // m_dwLastVerifedGCCnt is just to only verify the same method table once for each GC
    // I am getting rid of the assert.
    if (g_pConfig->FastGCStressLevel () > 1 && dwLastVerifiedGCCnt == GCHeap::GetGCHeap()->GetGcCount())
        return TRUE;
#endif //_DEBUG

    if (IsArray())
    {
        if (!IsAsyncPinType())
        {
            if (!SanityCheck())
            {
                ASSERT_AND_CHECK(!"Detected use of a corrupted OBJECTREF. Possible GC hole.");
            }
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
    // It is not a fatal error to fail the update the counter. We will run slower and retry next time, 
    // but the system will function properly.
    if (EnsureWritablePagesNoThrow(m_pWriteableData, sizeof(MethodTableWriteableData)))
        m_pWriteableData->m_dwLastVerifedGCCnt = GCHeap::GetGCHeap()->GetGcCount();
#endif //_DEBUG

    return TRUE;
}

NOINLINE BYTE *MethodTable::GetLoaderAllocatorObjectForGC()
{
    WRAPPER_NO_CONTRACT;
    if (!Collectible() || ((PTR_AppDomain)GetLoaderModule()->GetDomain())->NoAccessToHandleTable())
    {
        return NULL;
    }
    BYTE * retVal = *(BYTE**)GetLoaderAllocatorObjectHandle();
    return retVal;
}

#ifdef FEATURE_COMINTEROP
//==========================================================================================
BOOL MethodTable::IsWinRTRedirectedDelegate()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!IsDelegate())
    {
        return FALSE;
    }

    return !!WinRTDelegateRedirector::ResolveRedirectedDelegate(this, nullptr);
}

//==========================================================================================
BOOL MethodTable::IsWinRTRedirectedInterface(TypeHandle::InteropKind interopKind)
{
    LIMITED_METHOD_CONTRACT;

    if (!IsInterface())
        return FALSE;

    if (!HasRCWPerTypeData())
    {
        // All redirected interfaces have per-type RCW data
        return FALSE;
    }

#ifdef DACCESS_COMPILE
    RCWPerTypeData *pData = NULL;
#else // DACCESS_COMPILE
    // We want to keep this function LIMITED_METHOD_CONTRACT so we call GetRCWPerTypeData with
    // the non-throwing flag. pData can be NULL if it could not be allocated.
    RCWPerTypeData *pData = GetRCWPerTypeData(false);
#endif // DACCESS_COMPILE

    DWORD dwFlags = (pData != NULL ? pData->m_dwFlags : 0);
    if ((dwFlags & RCWPerTypeData::InterfaceFlagsInited) == 0)
    {
        dwFlags = RCWPerTypeData::InterfaceFlagsInited;

        if (WinRTInterfaceRedirector::ResolveRedirectedInterface(this, NULL))
        {
            dwFlags |= RCWPerTypeData::IsRedirectedInterface;
        }
        else if (HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__ICOLLECTIONGENERIC)) ||
                 HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IREADONLYCOLLECTIONGENERIC)) ||
                 this == MscorlibBinder::GetExistingClass(CLASS__ICOLLECTION))
        {
            dwFlags |= RCWPerTypeData::IsICollectionGeneric;
        }

        if (pData != NULL)
        {
            FastInterlockOr(&pData->m_dwFlags, dwFlags);
        }
    }

    if ((dwFlags & RCWPerTypeData::IsRedirectedInterface) != 0)
        return TRUE;

    if (interopKind == TypeHandle::Interop_ManagedToNative)
    {
        // ICollection<T> is redirected in the managed->WinRT direction (i.e. we have stubs
        // that implement ICollection<T> methods in terms of IVector/IMap), but it is not
        // treated specially in the WinRT->managed direction (we don't build a WinRT vtable
        // for a class that only implements ICollection<T>).  IReadOnlyCollection<T> is
        // treated similarly.
        if ((dwFlags & RCWPerTypeData::IsICollectionGeneric) != 0)
            return TRUE;
    }

    return FALSE;
}

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_READYTORUN_COMPILER

static BOOL ComputeIsLayoutFixedInCurrentVersionBubble(MethodTable * pMT)
{
    STANDARD_VM_CONTRACT;

    // Primitive types and enums have fixed layout
    if (pMT->IsTruePrimitive() || pMT->IsEnum())
        return TRUE;

    if (!pMT->GetModule()->IsInCurrentVersionBubble())
    {
        if (!pMT->IsValueType())
        {
            // Eventually, we may respect the non-versionable attribute for reference types too. For now, we are going
            // to play is safe and ignore it.
            return FALSE;
        }

        // Valuetypes with non-versionable attribute are candidates for fixed layout. Reject the rest.
        if (pMT->GetModule()->GetMDImport()->GetCustomAttributeByName(pMT->GetCl(),
                NONVERSIONABLE_TYPE, NULL, NULL) != S_OK)
        {
            return FALSE;
        }
    }

    // If the above condition passed, check that all instance fields have fixed layout as well. In particular, 
    // it is important for generic types with non-versionable layout (e.g. Nullable<T>)
    ApproxFieldDescIterator fieldIterator(pMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc *pFD = fieldIterator.Next(); pFD != NULL; pFD = fieldIterator.Next())
    {
        if (pFD->GetFieldType() != ELEMENT_TYPE_VALUETYPE)
            continue;

        MethodTable * pFieldMT = pFD->GetApproxFieldTypeHandleThrowing().AsMethodTable();
        if (!pFieldMT->IsLayoutFixedInCurrentVersionBubble())
            return FALSE;
    }

    return TRUE;
}

//
// Is field layout in this type fixed within the current version bubble?
// This check does not take the inheritance chain into account.
//
BOOL MethodTable::IsLayoutFixedInCurrentVersionBubble()
{
    STANDARD_VM_CONTRACT;

    const MethodTableWriteableData * pWriteableData = GetWriteableData();
    if (!(pWriteableData->m_dwFlags & MethodTableWriteableData::enum_flag_NGEN_IsLayoutFixedComputed))
    {
        MethodTableWriteableData * pWriteableDataForWrite = GetWriteableDataForWrite();
        if (ComputeIsLayoutFixedInCurrentVersionBubble(this))
            *EnsureWritablePages(&pWriteableDataForWrite->m_dwFlags) |= MethodTableWriteableData::enum_flag_NGEN_IsLayoutFixed;
        *EnsureWritablePages(&pWriteableDataForWrite->m_dwFlags) |= MethodTableWriteableData::enum_flag_NGEN_IsLayoutFixedComputed;
    }

    return (pWriteableData->m_dwFlags & MethodTableWriteableData::enum_flag_NGEN_IsLayoutFixed) != 0;
}

//
// Is field layout of the inheritance chain fixed within the current version bubble?
//
BOOL MethodTable::IsInheritanceChainLayoutFixedInCurrentVersionBubble()
{
    STANDARD_VM_CONTRACT;

    // This method is not expected to be called for value types
    _ASSERTE(!IsValueType());

    MethodTable * pMT = this;

    while ((pMT != g_pObjectClass) && (pMT != NULL))
    {
        if (!pMT->IsLayoutFixedInCurrentVersionBubble())
            return FALSE;

        pMT = pMT->GetParentMethodTable();
    }

    return TRUE;
}
#endif // FEATURE_READYTORUN_COMPILER

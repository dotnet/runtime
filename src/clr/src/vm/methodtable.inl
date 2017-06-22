// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: methodtable.inl
//


//

//
// ============================================================================

#ifndef _METHODTABLE_INL_ 
#define _METHODTABLE_INL_

#include "methodtable.h"
#include "genericdict.h"
#include "threadstatics.h"

//==========================================================================================
inline PTR_EEClass MethodTable::GetClass_NoLogging()
{
    LIMITED_METHOD_DAC_CONTRACT;

    TADDR addr = ReadPointer(this, &MethodTable::m_pCanonMT);

#ifdef _DEBUG 
    LowBits lowBits = union_getLowBits(addr);
    if (lowBits == UNION_EECLASS)
    {
        return PTR_EEClass(addr);
    }
    else if (lowBits == UNION_METHODTABLE)
    {
        // pointer to canonical MethodTable.
        TADDR canonicalMethodTable = union_getPointer(addr);
        return PTR_EEClass(ReadPointer((MethodTable *) PTR_MethodTable(canonicalMethodTable), &MethodTable::m_pCanonMT));
    }
#ifdef FEATURE_PREJIT
    else if (lowBits == UNION_INDIRECTION)
    {
        // pointer to indirection cell that points to canonical MethodTable
        TADDR canonicalMethodTable = *PTR_TADDR(union_getPointer(addr));
        return PTR_EEClass(ReadPointer((MethodTable *) PTR_MethodTable(canonicalMethodTable), &MethodTable::m_pCanonMT));
    }
#endif
#ifdef DACCESS_COMPILE
    // Minidumps don't guarantee that every member of every class will be able to work here.
#else
    _ASSERTE(!"Malformed m_pEEClass in MethodTable");
#endif
    return NULL;

#else

    if ((addr & 2) == 0)
    {
        // pointer to EEClass
        return PTR_EEClass(addr);
    }

#ifdef FEATURE_PREJIT
    if ((addr & 1) != 0)
    {
        // pointer to indirection cell that points to canonical MethodTable
        TADDR canonicalMethodTable = *PTR_TADDR(addr - 3);
        return PTR_EEClass(ReadPointer((MethodTable *) PTR_MethodTable(canonicalMethodTable), &MethodTable::m_pCanonMT));
    }
#endif

    // pointer to canonical MethodTable.
    return PTR_EEClass(ReadPointer((MethodTable *) PTR_MethodTable(addr - 2), &MethodTable::m_pCanonMT));
#endif
}

//==========================================================================================
inline PTR_EEClass MethodTable::GetClass()
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE_IMPL(!IsAsyncPinType());
    _ASSERTE_IMPL(GetClass_NoLogging() != NULL);

    g_IBCLogger.LogEEClassAndMethodTableAccess(this);
    return GetClass_NoLogging();
}

//==========================================================================================
inline Assembly * MethodTable::GetAssembly()
{
    WRAPPER_NO_CONTRACT;
    return GetModule()->GetAssembly();
}

//==========================================================================================
// DO NOT ADD ANY ASSERTS OR ANY OTHER CODE TO THIS METHOD.
// DO NOT USE THIS METHOD.
// Yes folks, for better or worse the debugger pokes supposed object addresses
// to try to see if objects are valid, possibly firing an AccessViolation or
// worse.  Thus it is "correct" behaviour for this to AV, and incorrect
// behaviour for it to assert if called on an invalid pointer.
inline PTR_EEClass MethodTable::GetClassWithPossibleAV()
{
    CANNOT_HAVE_CONTRACT;
    SUPPORTS_DAC;
    return GetClass_NoLogging();
}

//==========================================================================================
inline BOOL MethodTable::IsClassPointerValid()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    TADDR addr = ReadPointer(this, &MethodTable::m_pCanonMT);

    LowBits lowBits = union_getLowBits(addr);
    if (lowBits == UNION_EECLASS)
    {
        return !m_pEEClass.IsNull();
    }
    else if (lowBits == UNION_METHODTABLE)
    {
        // pointer to canonical MethodTable.
        TADDR canonicalMethodTable = union_getPointer(addr);
        return !PTR_MethodTable(canonicalMethodTable)->m_pEEClass.IsNull();
    }
#ifdef FEATURE_PREJIT
    else if (lowBits == UNION_INDIRECTION)
    {
        // pointer to indirection cell that points to canonical MethodTable
        TADDR canonicalMethodTable = *PTR_TADDR(union_getPointer(addr));
        if (CORCOMPILE_IS_POINTER_TAGGED(canonicalMethodTable))
            return FALSE;
        return !PTR_MethodTable(canonicalMethodTable)->m_pEEClass.IsNull();
    }
#endif
    _ASSERTE(!"Malformed m_pEEClass in MethodTable");
    return FALSE;
}

//==========================================================================================
// Does this immediate item live in an NGEN module?
inline BOOL MethodTable::IsZapped()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_PREJIT
    return GetFlag(enum_flag_IsZapped);
#else
    return FALSE;
#endif
}

//==========================================================================================
// For types that are part of an ngen-ed assembly this gets the
// Module* that contains this methodtable.
inline PTR_Module MethodTable::GetZapModule()
{
    LIMITED_METHOD_DAC_CONTRACT;

    PTR_Module zapModule = NULL;
    if (IsZapped())
    {
        zapModule = ReadPointer(this, &MethodTable::m_pLoaderModule);
    }

    return zapModule;
}

//==========================================================================================
inline PTR_Module MethodTable::GetLoaderModule() 
{
    LIMITED_METHOD_DAC_CONTRACT;
    return ReadPointer(this, &MethodTable::m_pLoaderModule);
}

inline PTR_LoaderAllocator MethodTable::GetLoaderAllocator() 
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetLoaderModule()->GetLoaderAllocator();
}



#ifndef DACCESS_COMPILE 
//==========================================================================================
inline void MethodTable::SetLoaderModule(Module* pModule) 
{
    WRAPPER_NO_CONTRACT;
    m_pLoaderModule.SetValue(pModule);
}

inline void MethodTable::SetLoaderAllocator(LoaderAllocator* pAllocator) 
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pAllocator == GetLoaderAllocator());

    if (pAllocator->Id()->IsCollectible())
    {
        SetFlag(enum_flag_Collectible);
    }
}

#endif

//==========================================================================================
inline WORD MethodTable::GetNumNonVirtualSlots()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return HasNonVirtualSlots() ? GetClass()->GetNumNonVirtualSlots() : 0;
}        

//==========================================================================================
inline WORD MethodTable::GetNumInstanceFields()
{
    WRAPPER_NO_CONTRACT;
    return (GetClass()->GetNumInstanceFields());
}

//==========================================================================================
inline WORD MethodTable::GetNumStaticFields()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (GetClass()->GetNumStaticFields());
}

//==========================================================================================
inline WORD MethodTable::GetNumThreadStaticFields()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (GetClass()->GetNumThreadStaticFields());
}

//==========================================================================================
inline DWORD MethodTable::GetNumInstanceFieldBytes()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return(GetBaseSize() - GetClass()->GetBaseSizePadding());
}

//==========================================================================================
inline WORD MethodTable::GetNumIntroducedInstanceFields()
{
    LIMITED_METHOD_DAC_CONTRACT;

    WORD wNumFields = GetNumInstanceFields();

    MethodTable * pParentMT = GetParentMethodTable();
    if (pParentMT != NULL)
    {
        WORD wNumParentFields = pParentMT->GetNumInstanceFields();

        // If this assert fires, then our bookkeeping is bad. Perhaps we incremented the count
        // of fields on the base class w/o incrementing the count in the derived class. (EnC scenarios).
        _ASSERTE(wNumFields >= wNumParentFields);

        wNumFields -= wNumParentFields;
    }

    return(wNumFields);
}

//==========================================================================================
inline DWORD MethodTable::GetAlignedNumInstanceFieldBytes()
{
    WRAPPER_NO_CONTRACT;
    return((GetNumInstanceFieldBytes() + 3) & (~3));
}

//==========================================================================================
inline PTR_FieldDesc MethodTable::GetApproxFieldDescListRaw()
{
    WRAPPER_NO_CONTRACT;
    // Careful about using this method. If it's possible that fields may have been added via EnC, then
    // must use the FieldDescIterator as any fields added via EnC won't be in the raw list

    return GetClass()->GetFieldDescList();
}

#ifdef FEATURE_COMINTEROP
//==========================================================================================
inline DWORD MethodTable::IsComClassInterface()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->IsComClassInterface();
}

//==========================================================================================
inline DWORD MethodTable::IsComImport()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->IsComImport();
}

//==========================================================================================
// Sparse VTables.   These require a SparseVTableMap in the EEClass in
// order to record how the CLR's vtable slots map across to COM
// Interop slots.
//
inline int MethodTable::IsSparseForCOMInterop()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->IsSparseForCOMInterop();
}

//==========================================================================================
inline int MethodTable::IsComEventItfType()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(GetClass());
    return GetClass()->IsComEventItfType();
}

#endif // FEATURE_COMINTEROP

//==========================================================================================
inline DWORD MethodTable::GetAttrClass()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->GetAttrClass();
}

//==========================================================================================
inline BOOL MethodTable::IsSerializable()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->IsSerializable();
}

//==========================================================================================
inline BOOL MethodTable::SupportsGenericInterop(TypeHandle::InteropKind interopKind,
                        MethodTable::Mode mode /*= modeAll*/)
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_COMINTEROP
    return ((IsInterface() || IsDelegate()) &&    // interface or delegate
            HasInstantiation() &&                 // generic
            !IsSharedByGenericInstantiations() && // unshared
            !ContainsGenericVariables() &&        // closed over concrete types
            // defined in .winmd or one of the redirected mscorlib interfaces
            ((((mode & modeProjected) != 0) && IsProjectedFromWinRT()) ||
             (((mode & modeRedirected) != 0) && (IsWinRTRedirectedInterface(interopKind) || IsWinRTRedirectedDelegate()))));
#else // FEATURE_COMINTEROP
    return FALSE;
#endif // FEATURE_COMINTEROP
}


//==========================================================================================
inline BOOL MethodTable::IsNotTightlyPacked()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->IsNotTightlyPacked();
}

//==========================================================================================
inline BOOL MethodTable::HasFieldsWhichMustBeInited()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->HasFieldsWhichMustBeInited();
}

//==========================================================================================
inline BOOL MethodTable::SupportsAutoNGen()
{
    LIMITED_METHOD_CONTRACT;
    return FALSE;
}

//==========================================================================================
inline BOOL MethodTable::RunCCTorAsIfNGenImageExists()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_CORESYSTEM
    return TRUE; // On our coresystem builds we will always be using triton in the customer scenario.
#else
    return FALSE;
#endif
}

//==========================================================================================
inline BOOL MethodTable::IsAbstract()
{
    WRAPPER_NO_CONTRACT;
    return GetClass()->IsAbstract();
}

//==========================================================================================

#ifdef FEATURE_COMINTEROP
//==========================================================================================
inline void MethodTable::SetHasGuidInfo()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsInterface() || (HasCCWTemplate() && IsDelegate()));

    // for delegates, having CCW template implies having GUID info
    if (IsInterface())
        SetFlag(enum_flag_IfInterfaceThenHasGuidInfo);
}

//==========================================================================================
inline BOOL MethodTable::HasGuidInfo()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsInterface())
        return GetFlag(enum_flag_IfInterfaceThenHasGuidInfo);

    // HasCCWTemplate() is intentionally checked first here to avoid hitting 
    // g_pMulticastDelegateClass == NULL inside IsDelegate() during startup
    return HasCCWTemplate() && IsDelegate();
}

//==========================================================================================
// True IFF the type has a GUID explicitly assigned to it (including WinRT generic interfaces
// where the GUID is computed).
inline BOOL MethodTable::HasExplicitGuid()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    GUID guid;
    GetGuid(&guid, FALSE);
    return (guid != GUID_NULL);
}

//==========================================================================================
inline void MethodTable::SetHasCCWTemplate()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_HasCCWTemplate);
}

//==========================================================================================
inline BOOL MethodTable::HasCCWTemplate()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetFlag(enum_flag_HasCCWTemplate);
}

//==========================================================================================
inline void MethodTable::SetHasRCWPerTypeData()
{
    LIMITED_METHOD_CONTRACT;
    SetFlag(enum_flag_HasRCWPerTypeData);
}

//==========================================================================================
inline BOOL MethodTable::HasRCWPerTypeData()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetFlag(enum_flag_HasRCWPerTypeData);
}

//==========================================================================================
// Get the GUID used for WinRT interop
//   * if the type is not a WinRT type or a redirected interfae return FALSE
inline BOOL MethodTable::GetGuidForWinRT(GUID *pGuid)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    BOOL bRes = FALSE;
    if ((IsProjectedFromWinRT() && !HasInstantiation()) || 
        (SupportsGenericInterop(TypeHandle::Interop_NativeToManaged) && IsLegalNonArrayWinRTType()))
    {
        bRes = SUCCEEDED(GetGuidNoThrow(pGuid, TRUE, FALSE));
    }

    return bRes;
}


#endif // FEATURE_COMINTEROP

//==========================================================================================
// The following two methods produce correct results only if this type is
// marked Serializable (verified by assert in checked builds) and the field
// in question was introduced in this type (the index is the FieldDesc
// index).
inline BOOL MethodTable::IsFieldNotSerialized(DWORD dwFieldIndex)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsSerializable());
    return FALSE;
}

//==========================================================================================
inline BOOL MethodTable::IsFieldOptionallySerialized(DWORD dwFieldIndex)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsSerializable());
    return FALSE;
}

//==========================================================================================
// Is pParentMT System.Enum? (Cannot be called before System.Enum is loaded.)
inline BOOL MethodTable::IsEnum()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // We should not be calling this before our parent method table pointer
    // is valid .
    _ASSERTE_IMPL(IsParentMethodTablePointerValid());

    PTR_MethodTable pParentMT = GetParentMethodTable();

    // Make sure that we are not using this method during startup
    _ASSERTE(g_pEnumClass != NULL);

    return (pParentMT == g_pEnumClass);
}

//==========================================================================================
// Is pParentMT either System.ValueType or System.Enum?
inline BOOL MethodTable::IsValueType()
{
    LIMITED_METHOD_DAC_CONTRACT;

    g_IBCLogger.LogMethodTableAccess(this);

    return GetFlag(enum_flag_Category_ValueType_Mask) == enum_flag_Category_ValueType;
}

//==========================================================================================
inline CorElementType MethodTable::GetArrayElementType()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE (IsArray());
    return dac_cast<PTR_ArrayClass>(GetClass())->GetArrayElementType();
}

//==========================================================================================
inline DWORD MethodTable::GetRank()
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE (IsArray());
    if (GetFlag(enum_flag_Category_IfArrayThenSzArray))
        return 1;  // ELEMENT_TYPE_SZARRAY
    else
        return dac_cast<PTR_ArrayClass>(GetClass())->GetRank();
}

//==========================================================================================
inline BOOL MethodTable::IsTruePrimitive()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetFlag(enum_flag_Category_Mask) == enum_flag_Category_TruePrimitive;
}

//==========================================================================================
inline void MethodTable::SetIsTruePrimitive()
{
    LIMITED_METHOD_DAC_CONTRACT;
    SetFlag(enum_flag_Category_TruePrimitive);
}

//==========================================================================================
inline BOOL MethodTable::IsBlittable()
{
    WRAPPER_NO_CONTRACT;
#ifndef DACCESS_COMPILE 
    _ASSERTE(GetClass());
    return GetClass()->IsBlittable();
#else // DACCESS_COMPILE
    DacNotImpl();
    return false;
#endif // DACCESS_COMPILE
}

//==========================================================================================
inline BOOL MethodTable::HasClassConstructor()
{
    WRAPPER_NO_CONTRACT;
    return GetFlag(enum_flag_HasCctor);
}

//==========================================================================================
inline void MethodTable::SetHasClassConstructor()
{
    WRAPPER_NO_CONTRACT;
    return SetFlag(enum_flag_HasCctor);
}

//==========================================================================================
inline WORD MethodTable::GetClassConstructorSlot()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(HasClassConstructor());

    // The class constructor slot is the first non-vtable slot
    return GetNumVirtuals();
}

//==========================================================================================
inline BOOL MethodTable::HasDefaultConstructor()
{
    WRAPPER_NO_CONTRACT;
    return GetFlag(enum_flag_HasDefaultCtor);
}

//==========================================================================================
inline void MethodTable::SetHasDefaultConstructor()
{
    WRAPPER_NO_CONTRACT;
    return SetFlag(enum_flag_HasDefaultCtor);
}

//==========================================================================================
inline WORD MethodTable::GetDefaultConstructorSlot()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(HasDefaultConstructor());

    // The default ctor slot is right after cctor slot if there is one
    return GetNumVirtuals() + (HasClassConstructor() ? 1 : 0);
}

//==========================================================================================
inline BOOL MethodTable::HasLayout()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(GetClass());
    return GetClass()->HasLayout();
}

//==========================================================================================
inline MethodDesc* MethodTable::GetMethodDescForSlot(DWORD slot)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    PCODE pCode = GetRestoredSlot(slot);

    // This is an optimization that we can take advantage of if we're trying
    // to get the MethodDesc for an interface virtual, since their slots
    // always point to the stub.
    if (IsInterface() && slot < GetNumVirtuals())
    {
        return MethodDesc::GetMethodDescFromStubAddr(pCode);
    }

    return MethodTable::GetMethodDescForSlotAddress(pCode);
}

#ifndef DACCESS_COMPILE 

//==========================================================================================
inline INT32 MethodTable::MethodIterator::GetNumMethods() const
{
    LIMITED_METHOD_CONTRACT;
    //  assert that number of methods hasn't changed during the iteration
    CONSISTENCY_CHECK( m_pMethodData->GetNumMethods() == static_cast< UINT32 >( m_iMethods ) );
    return m_iMethods;
}

//==========================================================================================
// Returns TRUE if it's valid to request data from the current position
inline BOOL MethodTable::MethodIterator::IsValid() const
{
    LIMITED_METHOD_CONTRACT;
    return m_iCur >= 0 && m_iCur < GetNumMethods();
}

//==========================================================================================
inline BOOL MethodTable::MethodIterator::MoveTo(UINT32 idx)
{
    LIMITED_METHOD_CONTRACT;
    m_iCur = (INT32)idx;
    return IsValid();
}

//==========================================================================================
inline BOOL MethodTable::MethodIterator::Prev()
{
    WRAPPER_NO_CONTRACT;
    if (IsValid())
        --m_iCur;
    return (IsValid());
}

//==========================================================================================
inline BOOL MethodTable::MethodIterator::Next()
{
    WRAPPER_NO_CONTRACT;
    if (IsValid())
        ++m_iCur;
    return (IsValid());
}

//==========================================================================================
inline void MethodTable::MethodIterator::MoveToBegin()
{
    WRAPPER_NO_CONTRACT;
    m_iCur = 0;
}

//==========================================================================================
inline void MethodTable::MethodIterator::MoveToEnd()
{
    WRAPPER_NO_CONTRACT;
    m_iCur = GetNumMethods() - 1;
}

//==========================================================================================
inline UINT32 MethodTable::MethodIterator::GetSlotNumber() const {
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    return (UINT32)m_iCur;
}

//==========================================================================================
inline UINT32 MethodTable::MethodIterator::GetImplSlotNumber() const {
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    return (UINT32)m_pMethodData->GetImplSlotNumber(m_iCur);
}

//==========================================================================================
inline BOOL MethodTable::MethodIterator::IsVirtual() const {
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    return m_iCur < (INT32)(GetNumVirtuals());
}

//==========================================================================================
inline UINT32 MethodTable::MethodIterator::GetNumVirtuals() const {
    LIMITED_METHOD_CONTRACT;
    return m_pMethodData->GetNumVirtuals();;
}

//==========================================================================================
inline DispatchSlot MethodTable::MethodIterator::GetTarget() const {
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    return m_pMethodData->GetImplSlot(m_iCur);
}

//==========================================================================================
inline MethodDesc *MethodTable::MethodIterator::GetMethodDesc() const {
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    MethodDesc *pMD = m_pMethodData->GetImplMethodDesc(m_iCur);
    CONSISTENCY_CHECK(CheckPointer(pMD));
    return pMD;
}

//==========================================================================================
inline MethodDesc *MethodTable::MethodIterator::GetDeclMethodDesc() const {
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    MethodDesc *pMD = m_pMethodData->GetDeclMethodDesc(m_iCur);
    CONSISTENCY_CHECK(CheckPointer(pMD));
    CONSISTENCY_CHECK(pMD->GetSlot() == GetSlotNumber());
    return pMD;
}

#endif // DACCESS_COMPILE 

//==========================================================================================
// Non-canonical types share the method bodies with the canonical type. So the canonical
// type can be said to own the method bodies. Hence, by default, IntroducedMethodIterator
// only lets you iterate methods of the canonical type. You have to pass in
// restrictToCanonicalTypes=FALSE to iterate methods through a non-canonical type.

inline MethodTable::IntroducedMethodIterator::IntroducedMethodIterator(
        MethodTable *pMT, 
        BOOL restrictToCanonicalTypes /* = TRUE */ )
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(pMT->IsCanonicalMethodTable() || !restrictToCanonicalTypes);

    SetChunk(pMT->GetClass()->GetChunks());
}

//==========================================================================================
FORCEINLINE BOOL MethodTable::IntroducedMethodIterator::Next()
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(IsValid());

    // Check whether the next MethodDesc is still within the bounds of the current chunk
    TADDR pNext = dac_cast<TADDR>(m_pMethodDesc) + m_pMethodDesc->SizeOf();

    if (pNext < m_pChunkEnd)
    {
        // Just skip to the next method in the same chunk
        m_pMethodDesc = PTR_MethodDesc(pNext);
    }
    else
    {
        _ASSERTE(pNext == m_pChunkEnd);

        // We have walked all the methods in the current chunk. Move on
        // to the next chunk.
        SetChunk(m_pChunk->GetNextChunk());
    }

    return IsValid();
}

//==========================================================================================
inline BOOL MethodTable::IntroducedMethodIterator::IsValid() const
{
    LIMITED_METHOD_CONTRACT;
    return m_pMethodDesc != NULL;
}

//==========================================================================================
inline MethodDesc * MethodTable::IntroducedMethodIterator::GetMethodDesc() const
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(IsValid());
    return m_pMethodDesc;
}

//==========================================================================================
inline DWORD MethodTable::GetIndexOfVtableIndirection(DWORD slotNum)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE((1 << VTABLE_SLOTS_PER_CHUNK_LOG2) == VTABLE_SLOTS_PER_CHUNK);

    return slotNum >> VTABLE_SLOTS_PER_CHUNK_LOG2;
}

//==========================================================================================
inline DWORD MethodTable::GetStartSlotForVtableIndirection(UINT32 indirectionIndex, DWORD wNumVirtuals)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(indirectionIndex < GetNumVtableIndirections(wNumVirtuals));

    return indirectionIndex * VTABLE_SLOTS_PER_CHUNK;
}

//==========================================================================================
inline DWORD MethodTable::GetEndSlotForVtableIndirection(UINT32 indirectionIndex, DWORD wNumVirtuals)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(indirectionIndex < GetNumVtableIndirections(wNumVirtuals));

    DWORD end = (indirectionIndex + 1) * VTABLE_SLOTS_PER_CHUNK;

    if (end > wNumVirtuals)
    {
        end = wNumVirtuals;
    }

    return end;
}

//==========================================================================================
inline UINT32 MethodTable::GetIndexAfterVtableIndirection(UINT32 slotNum)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE((1 << VTABLE_SLOTS_PER_CHUNK_LOG2) == VTABLE_SLOTS_PER_CHUNK);

    return (slotNum & (VTABLE_SLOTS_PER_CHUNK - 1));
}

//==========================================================================================
inline DWORD MethodTable::GetNumVtableIndirections(DWORD wNumVirtuals)
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE((1 << VTABLE_SLOTS_PER_CHUNK_LOG2) == VTABLE_SLOTS_PER_CHUNK);

    return (wNumVirtuals + (VTABLE_SLOTS_PER_CHUNK - 1)) >> VTABLE_SLOTS_PER_CHUNK_LOG2;
}

//==========================================================================================
inline PTR_PTR_PCODE MethodTable::GetVtableIndirections()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<PTR_PTR_PCODE>(dac_cast<TADDR>(this) + sizeof(MethodTable));
}

//==========================================================================================
inline DWORD MethodTable::GetNumVtableIndirections()
{
    WRAPPER_NO_CONTRACT;

    return GetNumVtableIndirections(GetNumVirtuals_NoLogging());
}

//==========================================================================================
inline MethodTable::VtableIndirectionSlotIterator::VtableIndirectionSlotIterator(MethodTable *pMT)
  : m_pSlot(pMT->GetVtableIndirections()),
    m_i((DWORD) -1),
    m_count(pMT->GetNumVtableIndirections()),
    m_pMT(pMT)
{
    WRAPPER_NO_CONTRACT;
}

//==========================================================================================
inline MethodTable::VtableIndirectionSlotIterator::VtableIndirectionSlotIterator(MethodTable *pMT, DWORD index)
  : m_pSlot(pMT->GetVtableIndirections() + index),
    m_i(index),
    m_count(pMT->GetNumVtableIndirections()),
    m_pMT(pMT)
{
    WRAPPER_NO_CONTRACT;
    PRECONDITION(index != (DWORD) -1 && index < m_count);
}

//==========================================================================================
inline BOOL MethodTable::VtableIndirectionSlotIterator::Next()
{
    LIMITED_METHOD_DAC_CONTRACT;
    PRECONDITION(!Finished());
    if (m_i != (DWORD) -1)
        m_pSlot++;
    return (++m_i < m_count);
}

//==========================================================================================
inline BOOL MethodTable::VtableIndirectionSlotIterator::Finished()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (m_i == m_count);
}

//==========================================================================================
inline DWORD MethodTable::VtableIndirectionSlotIterator::GetIndex()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_i;
}

//==========================================================================================
inline DWORD MethodTable::VtableIndirectionSlotIterator::GetOffsetFromMethodTable()
{
    WRAPPER_NO_CONTRACT;
    PRECONDITION(m_i != (DWORD) -1 && m_i < m_count);
    
    return GetVtableOffset() + sizeof(PTR_PCODE) * m_i;
}

//==========================================================================================
inline PTR_PCODE MethodTable::VtableIndirectionSlotIterator::GetIndirectionSlot()
{
    LIMITED_METHOD_DAC_CONTRACT;
    PRECONDITION(m_i != (DWORD) -1 && m_i < m_count);

    return *m_pSlot;
}

//==========================================================================================
#ifndef DACCESS_COMPILE
inline void MethodTable::VtableIndirectionSlotIterator::SetIndirectionSlot(PTR_PCODE pChunk)
{
    LIMITED_METHOD_CONTRACT;
    *m_pSlot = pChunk;
}
#endif

//==========================================================================================
inline DWORD MethodTable::VtableIndirectionSlotIterator::GetStartSlot()
{
    WRAPPER_NO_CONTRACT;
    PRECONDITION(m_i != (DWORD) -1 && m_i < m_count);

    return GetStartSlotForVtableIndirection(m_i, m_pMT->GetNumVirtuals());
}

//==========================================================================================
inline DWORD MethodTable::VtableIndirectionSlotIterator::GetEndSlot()
{
    WRAPPER_NO_CONTRACT;
    PRECONDITION(m_i != (DWORD) -1 && m_i < m_count);

    return GetEndSlotForVtableIndirection(m_i, m_pMT->GetNumVirtuals());
}

//==========================================================================================
inline DWORD MethodTable::VtableIndirectionSlotIterator::GetNumSlots()
{
    WRAPPER_NO_CONTRACT;

    return GetEndSlot() - GetStartSlot();
}

//==========================================================================================
inline DWORD MethodTable::VtableIndirectionSlotIterator::GetSize()
{
    WRAPPER_NO_CONTRACT;

    return GetNumSlots() * sizeof(PCODE);
}

//==========================================================================================
// Create a new iterator over the vtable indirection slots
// The iterator starts just before the first item
inline MethodTable::VtableIndirectionSlotIterator MethodTable::IterateVtableIndirectionSlots()
{
    WRAPPER_NO_CONTRACT;
    return VtableIndirectionSlotIterator(this);
}

//==========================================================================================
// Create a new iterator over the vtable indirection slots, starting at the index specified
inline MethodTable::VtableIndirectionSlotIterator MethodTable::IterateVtableIndirectionSlotsFrom(DWORD index)
{
    WRAPPER_NO_CONTRACT;
    return VtableIndirectionSlotIterator(this, index);
}

#ifndef DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP

//==========================================================================================
inline ComCallWrapperTemplate *MethodTable::GetComCallWrapperTemplate()
{
    LIMITED_METHOD_CONTRACT;
    if (HasCCWTemplate())
    {
        return *GetCCWTemplatePtr();
    }
    return GetClass()->GetComCallWrapperTemplate();
}

//==========================================================================================
inline BOOL MethodTable::SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (HasCCWTemplate())
    {
        TypeHandle th(this);
        g_IBCLogger.LogTypeMethodTableWriteableAccess(&th);
        return (InterlockedCompareExchangeT(EnsureWritablePages(GetCCWTemplatePtr()), pTemplate, NULL) == NULL);
    }
    g_IBCLogger.LogEEClassCOWTableAccess(this);
    return GetClass_NoLogging()->SetComCallWrapperTemplate(pTemplate);
}

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
//==========================================================================================
inline ClassFactoryBase *MethodTable::GetComClassFactory()
{
    LIMITED_METHOD_CONTRACT;
    return GetClass()->GetComClassFactory();
}

//==========================================================================================
inline BOOL MethodTable::SetComClassFactory(ClassFactoryBase *pFactory)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    g_IBCLogger.LogEEClassCOWTableAccess(this);
    return GetClass_NoLogging()->SetComClassFactory(pFactory);
}
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP
#endif // DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
//==========================================================================================
inline BOOL MethodTable::IsProjectedFromWinRT()
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(GetClass());
    return GetClass()->IsProjectedFromWinRT();
}

//==========================================================================================
inline BOOL MethodTable::IsExportedToWinRT()
{
    LIMITED_METHOD_DAC_CONTRACT;
    _ASSERTE(GetClass());
    return GetClass()->IsExportedToWinRT();
}

//==========================================================================================
inline BOOL MethodTable::IsWinRTDelegate()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (IsProjectedFromWinRT() && IsDelegate()) || IsWinRTRedirectedDelegate();
}

#else // FEATURE_COMINTEROP

//==========================================================================================
inline BOOL MethodTable::IsProjectedFromWinRT()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return FALSE;
}

//==========================================================================================
inline BOOL MethodTable::IsExportedToWinRT()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return FALSE;
}

//==========================================================================================
inline BOOL MethodTable::IsWinRTDelegate()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return FALSE;
}

#endif // FEATURE_COMINTEROP

//==========================================================================================
inline UINT32 MethodTable::GetNativeSize()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(GetClass());
    return GetClass()->GetNativeSize();
}

//==========================================================================================
inline PTR_MethodTable MethodTable::GetCanonicalMethodTable()
{
    LIMITED_METHOD_DAC_CONTRACT;

    TADDR addr = ReadPointer(this, &MethodTable::m_pCanonMT);

#ifdef _DEBUG
    LowBits lowBits = union_getLowBits(addr);
    if (lowBits == UNION_EECLASS)
    {
        return dac_cast<PTR_MethodTable>(this);
    }
    else if (lowBits == UNION_METHODTABLE)
    {
        // pointer to canonical MethodTable.
        return PTR_MethodTable(union_getPointer(addr));
    }
#ifdef FEATURE_PREJIT
    else if (lowBits == UNION_INDIRECTION)
    {
        return PTR_MethodTable(*PTR_TADDR(union_getPointer(addr)));
    }
#endif
    _ASSERTE(!"Malformed m_pCanonMT in MethodTable");
    return NULL;
#else

    if ((addr & 2) == 0)
        return dac_cast<PTR_MethodTable>(this);

#ifdef FEATURE_PREJIT
    if ((addr & 1) != 0)
        return PTR_MethodTable(*PTR_TADDR(addr - 3));
#endif

    return PTR_MethodTable(addr - 2);
#endif
}

//==========================================================================================
inline TADDR MethodTable::GetCanonicalMethodTableFixup()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_PREJIT
    TADDR addr = ReadPointer(this, &MethodTable::m_pCanonMT);
    LowBits lowBits = union_getLowBits(addr);
    if (lowBits == UNION_INDIRECTION)
    {
        // pointer to canonical MethodTable.
        return *PTR_TADDR(union_getPointer(addr));
    }
    else
#endif
    {
        return NULL;
    }
}

//==========================================================================================
#ifndef DACCESS_COMPILE
FORCEINLINE BOOL MethodTable::IsEquivalentTo(MethodTable *pOtherMT COMMA_INDEBUG(TypeHandlePairList *pVisited /*= NULL*/))
{
    WRAPPER_NO_CONTRACT;

    if (this == pOtherMT)
        return TRUE;

#ifdef FEATURE_TYPEEQUIVALENCE
    // bail early for normal types
    if (!HasTypeEquivalence() || !pOtherMT->HasTypeEquivalence())
        return FALSE;

    if (IsEquivalentTo_Worker(pOtherMT COMMA_INDEBUG(pVisited)))
        return TRUE;
#endif // FEATURE_TYPEEQUIVALENCE

    return FALSE;
}
#endif

//==========================================================================================
inline IMDInternalImport* MethodTable::GetMDImport()
{
    LIMITED_METHOD_CONTRACT;
    return GetModule()->GetMDImport();
}

//==========================================================================================
inline BOOL MethodTable::IsSealed()
{
    LIMITED_METHOD_CONTRACT;
    return GetClass()->IsSealed();
}

//==========================================================================================
inline BOOL MethodTable::IsManagedSequential()
{
    LIMITED_METHOD_CONTRACT;
    return GetClass()->IsManagedSequential();
}

//==========================================================================================
inline BOOL MethodTable::HasExplicitSize()
{
    LIMITED_METHOD_CONTRACT;
    return GetClass()->HasExplicitSize();
}

//==========================================================================================
inline DWORD MethodTable::GetPerInstInfoSize()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetNumDicts() * sizeof(TypeHandle*);
}

//==========================================================================================
inline EEClassLayoutInfo *MethodTable::GetLayoutInfo()
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(HasLayout());
    return GetClass()->GetLayoutInfo();
}

//==========================================================================================
// These come after the pointers to the generic dictionaries (if any)
inline DWORD MethodTable::GetInterfaceMapSize()
{
    LIMITED_METHOD_DAC_CONTRACT;

    DWORD cbIMap = GetNumInterfaces() * sizeof(InterfaceInfo_t);
#ifdef FEATURE_COMINTEROP 
    cbIMap += (HasDynamicInterfaceMap() ? sizeof(DWORD_PTR) : 0);
#endif
    return cbIMap;
}

//==========================================================================================
// These are the generic dictionaries themselves and are come after
//  the interface map.  In principle they need not be inline in the method table.
inline DWORD MethodTable::GetInstAndDictSize()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!HasInstantiation())
        return 0;
    else
        return DictionaryLayout::GetFirstDictionaryBucketSize(GetNumGenericArgs(), GetClass()->GetDictionaryLayout());
}

//==========================================================================================
inline BOOL MethodTable::IsSharedByGenericInstantiations()
{
    LIMITED_METHOD_DAC_CONTRACT;

    g_IBCLogger.LogMethodTableAccess(this);

    return TestFlagWithMask(enum_flag_GenericsMask, enum_flag_GenericsMask_SharedInst);
}

//==========================================================================================
inline BOOL MethodTable::IsCanonicalMethodTable()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (union_getLowBits(ReadPointer(this, &MethodTable::m_pCanonMT)) == UNION_EECLASS);
}

//==========================================================================================
FORCEINLINE BOOL MethodTable::HasInstantiation()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Generics flags cannot be expressed in terms of GetFlag()
    return !TestFlagWithMask(enum_flag_GenericsMask, enum_flag_GenericsMask_NonGeneric);
}

//==========================================================================================
inline void MethodTable::SetHasInstantiation(BOOL fTypicalInstantiation, BOOL fSharedByGenericInstantiations)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!IsStringOrArray());
    SetFlag(fTypicalInstantiation ? enum_flag_GenericsMask_TypicalInst :
        (fSharedByGenericInstantiations ? enum_flag_GenericsMask_SharedInst : enum_flag_GenericsMask_GenericInst));
}
//==========================================================================================
inline BOOL MethodTable::IsGenericTypeDefinition()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Generics flags cannot be expressed in terms of GetFlag()
    return TestFlagWithMask(enum_flag_GenericsMask, enum_flag_GenericsMask_TypicalInst);
}

//==========================================================================================
inline PTR_InterfaceInfo MethodTable::GetInterfaceMap()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return ReadPointer(this, &MethodTable::m_pInterfaceMap);
}

//==========================================================================================
FORCEINLINE TADDR MethodTable::GetMultipurposeSlotPtr(WFLAGS2_ENUM flag, const BYTE * offsets)
{
    LIMITED_METHOD_DAC_CONTRACT;

    _ASSERTE(GetFlag(flag));

    DWORD offset = offsets[GetFlag((WFLAGS2_ENUM)(flag - 1))];

    if (offset >= sizeof(MethodTable)) {
        offset += GetNumVtableIndirections() * sizeof(PTR_PCODE);
    }

    return dac_cast<TADDR>(this) + offset;
}

//==========================================================================================
// This method is dependent on the declared order of optional members
// If you add or remove an optional member or reorder them please change this method
FORCEINLINE DWORD MethodTable::GetOffsetOfOptionalMember(OptionalMemberId id)
{
    LIMITED_METHOD_CONTRACT;

    DWORD offset = c_OptionalMembersStartOffsets[GetFlag(enum_flag_MultipurposeSlotsMask)];

    offset += GetNumVtableIndirections() * sizeof(PTR_PCODE);

#undef METHODTABLE_OPTIONAL_MEMBER
#define METHODTABLE_OPTIONAL_MEMBER(NAME, TYPE, GETTER) \
    if (id == OptionalMember_##NAME) { \
        return offset; \
    } \
    C_ASSERT(sizeof(TYPE) % sizeof(UINT_PTR) == 0); /* To insure proper alignment */ \
    if (Has##NAME()) { \
        offset += sizeof(TYPE); \
    }

    METHODTABLE_OPTIONAL_MEMBERS()

    _ASSERTE(!"Wrong optional member" || id == OptionalMember_Count);
    return offset;
}

//==========================================================================================
// this is not the pretties function however I got bitten pretty hard by the computation 
// of the allocation size of a MethodTable done "by hand" in few places.
// Essentially the idea is that this is going to centralize computation of size for optional
// members so the next morons that need to add an optional member will look at this function
// and hopefully be less exposed to code doing size computation behind their back
inline DWORD MethodTable::GetOptionalMembersAllocationSize(DWORD dwMultipurposeSlotsMask,
                                                           BOOL needsRemotableMethodInfo,
                                                           BOOL needsGenericsStaticsInfo,
                                                           BOOL needsGuidInfo,
                                                           BOOL needsCCWTemplate,
                                                           BOOL needsRCWPerTypeData,
                                                           BOOL needsRemotingVtsInfo,
                                                           BOOL needsContextStatic,
                                                           BOOL needsTokenOverflow)
{
    LIMITED_METHOD_CONTRACT;

    DWORD size = c_OptionalMembersStartOffsets[dwMultipurposeSlotsMask] - sizeof(MethodTable);

    if (needsRemotableMethodInfo)
        size += sizeof(UINT_PTR);
    if (needsGenericsStaticsInfo)
        size += sizeof(GenericsStaticsInfo);
    if (needsGuidInfo)
        size += sizeof(UINT_PTR);
    if (needsCCWTemplate)
        size += sizeof(UINT_PTR);
    if (needsRCWPerTypeData)
        size += sizeof(UINT_PTR);
    if (needsRemotingVtsInfo)
        size += sizeof(UINT_PTR);
    if (needsContextStatic)
        size += sizeof(UINT_PTR);
    if (dwMultipurposeSlotsMask & enum_flag_HasInterfaceMap)
        size += sizeof(UINT_PTR);
    if (needsTokenOverflow)
        size += sizeof(UINT_PTR);

    return size;
}

inline DWORD MethodTable::GetOptionalMembersSize()
{
    WRAPPER_NO_CONTRACT;

    return GetEndOffsetOfOptionalMembers() - GetStartOffsetOfOptionalMembers();
}

#ifndef DACCESS_COMPILE

//==========================================================================================
inline PTR_BYTE MethodTable::GetNonGCStaticsBasePointer()
{
    WRAPPER_NO_CONTRACT;
    return GetDomainLocalModule()->GetNonGCStaticsBasePointer(this);
}

//==========================================================================================
inline PTR_BYTE MethodTable::GetGCStaticsBasePointer()
{
    WRAPPER_NO_CONTRACT;
    return GetDomainLocalModule()->GetGCStaticsBasePointer(this);
}

//==========================================================================================
inline PTR_BYTE MethodTable::GetNonGCThreadStaticsBasePointer()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get the current thread
    PTR_Thread pThread = dac_cast<PTR_Thread>(GetThread());

    // Get the current module's ModuleIndex
    ModuleIndex index = GetModuleForStatics()->GetModuleIndex();
    
    PTR_ThreadLocalBlock pTLB = ThreadStatics::GetCurrentTLBIfExists(pThread, NULL);
    if (pTLB == NULL)
        return NULL;

    PTR_ThreadLocalModule pTLM = pTLB->GetTLMIfExists(index);
    if (pTLM == NULL)
        return NULL;

    return pTLM->GetNonGCStaticsBasePointer(this);
}

//==========================================================================================
inline PTR_BYTE MethodTable::GetGCThreadStaticsBasePointer()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Get the current thread
    PTR_Thread pThread = dac_cast<PTR_Thread>(GetThread());

    // Get the current module's ModuleIndex
    ModuleIndex index = GetModuleForStatics()->GetModuleIndex();
    
    PTR_ThreadLocalBlock pTLB = ThreadStatics::GetCurrentTLBIfExists(pThread, NULL);
    if (pTLB == NULL)
        return NULL;

    PTR_ThreadLocalModule pTLM = pTLB->GetTLMIfExists(index);
    if (pTLM == NULL)
        return NULL;

    return pTLM->GetGCStaticsBasePointer(this);
}

#endif //!DACCESS_COMPILE

//==========================================================================================
inline PTR_BYTE MethodTable::GetNonGCThreadStaticsBasePointer(PTR_Thread pThread, PTR_AppDomain pDomain)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Get the current module's ModuleIndex
    ModuleIndex index = GetModuleForStatics()->GetModuleIndex();
    
    PTR_ThreadLocalBlock pTLB = ThreadStatics::GetCurrentTLBIfExists(pThread, pDomain);
    if (pTLB == NULL)
        return NULL;

    PTR_ThreadLocalModule pTLM = pTLB->GetTLMIfExists(index);
    if (pTLM == NULL)
        return NULL;

    return pTLM->GetNonGCStaticsBasePointer(this);
}

//==========================================================================================
inline PTR_BYTE MethodTable::GetGCThreadStaticsBasePointer(PTR_Thread pThread, PTR_AppDomain pDomain)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Get the current module's ModuleIndex
    ModuleIndex index = GetModuleForStatics()->GetModuleIndex();
    
    PTR_ThreadLocalBlock pTLB = ThreadStatics::GetCurrentTLBIfExists(pThread, pDomain);
    if (pTLB == NULL)
        return NULL;

    PTR_ThreadLocalModule pTLM = pTLB->GetTLMIfExists(index);
    if (pTLM == NULL)
        return NULL;

    return pTLM->GetGCStaticsBasePointer(this);
}

//==========================================================================================
inline PTR_DomainLocalModule MethodTable::GetDomainLocalModule(AppDomain * pAppDomain)
{
    WRAPPER_NO_CONTRACT;
    return GetModuleForStatics()->GetDomainLocalModule(pAppDomain);
}

#ifndef DACCESS_COMPILE
//==========================================================================================
inline PTR_DomainLocalModule MethodTable::GetDomainLocalModule()
{
    WRAPPER_NO_CONTRACT;
    return GetModuleForStatics()->GetDomainLocalModule();
}
#endif //!DACCESS_COMPILE

//==========================================================================================
inline OBJECTREF MethodTable::AllocateNoChecks()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    // we know an instance of this class already exists in the same appdomain
    // therefore, some checks become redundant.
    // this currently only happens for Delegate.Combine
    CONSISTENCY_CHECK(IsRestored_NoLogging());

    CONSISTENCY_CHECK(CheckInstanceActivated());

    return AllocateObject(this);
}


//==========================================================================================
inline DWORD MethodTable::GetClassIndex()
{
    WRAPPER_NO_CONTRACT;
    return GetClassIndexFromToken(GetCl());
}

#ifndef DACCESS_COMPILE
//==========================================================================================
// unbox src into dest, making sure src is of the correct type.

inline BOOL MethodTable::UnBoxInto(void *dest, OBJECTREF src) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (Nullable::IsNullableType(TypeHandle(this)))
        return Nullable::UnBoxNoGC(dest, src, this);
    else  
    {
        if (src == NULL || src->GetMethodTable() != this)
            return FALSE;

        CopyValueClass(dest, src->UnBox(), this, src->GetAppDomain());
    }
    return TRUE;
}

//==========================================================================================
// unbox src into argument, making sure src is of the correct type.

inline BOOL MethodTable::UnBoxIntoArg(ArgDestination *argDest, OBJECTREF src)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (Nullable::IsNullableType(TypeHandle(this)))
        return Nullable::UnBoxIntoArgNoGC(argDest, src, this);
    else  
    {
        if (src == NULL || src->GetMethodTable() != this)
            return FALSE;

        CopyValueClassArg(argDest, src->UnBox(), this, src->GetAppDomain(), 0);
    }
    return TRUE;
}

//==========================================================================================
// unbox src into dest, No checks are done

inline void MethodTable::UnBoxIntoUnchecked(void *dest, OBJECTREF src) 
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (Nullable::IsNullableType(TypeHandle(this))) {
        BOOL ret;
        ret = Nullable::UnBoxNoGC(dest, src, this);
        _ASSERTE(ret);
    }
    else  
    {
        _ASSERTE(src->GetMethodTable()->GetNumInstanceFieldBytes() == GetNumInstanceFieldBytes());

        CopyValueClass(dest, src->UnBox(), this, src->GetAppDomain());
    }
}
#endif
//==========================================================================================
__forceinline TypeHandle::CastResult MethodTable::CanCastToClassOrInterfaceNoGC(MethodTable *pTargetMT)
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
    }
    CONTRACTL_END

    if (pTargetMT->IsInterface())
        return CanCastToInterfaceNoGC(pTargetMT);
    else
        return CanCastToClassNoGC(pTargetMT);
}

//==========================================================================================
inline BOOL MethodTable::CanCastToClassOrInterface(MethodTable *pTargetMT, TypeHandlePairList *pVisited)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pTargetMT));
        PRECONDITION(!pTargetMT->IsArray());
        PRECONDITION(IsRestored_NoLogging());
    }
    CONTRACTL_END

    if (pTargetMT->IsInterface())
        return CanCastToInterface(pTargetMT, pVisited);
    else
        return CanCastToClass(pTargetMT, pVisited);
}

//==========================================================================================
FORCEINLINE PTR_Module MethodTable::GetGenericsStaticsModuleAndID(DWORD * pID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    _ASSERTE(HasGenericsStaticsInfo());

#ifdef FEATURE_PREJIT
    // This is performance sensitive codepath inlined into JIT helpers. Test the flag directly without
    // checking IsStringOrArray() first. IsStringOrArray() will always be false here.
    _ASSERTE(!IsStringOrArray());
    if (m_dwFlags & enum_flag_StaticsMask_IfGenericsThenCrossModule)
    {
        CrossModuleGenericsStaticsInfo *pInfo = ReadPointer(this, &MethodTable::m_pWriteableData)->GetCrossModuleGenericsStaticsInfo();
        _ASSERTE(FitsIn<DWORD>(pInfo->m_DynamicTypeID) || pInfo->m_DynamicTypeID == (SIZE_T)-1);
        *pID = static_cast<DWORD>(pInfo->m_DynamicTypeID);
        return pInfo->m_pModuleForStatics;
    }
#endif // FEATURE_PREJIT

    _ASSERTE(FitsIn<DWORD>(GetGenericsStaticsInfo()->m_DynamicTypeID) || GetGenericsStaticsInfo()->m_DynamicTypeID == (SIZE_T)-1);
    *pID = static_cast<DWORD>(GetGenericsStaticsInfo()->m_DynamicTypeID);
    return GetLoaderModule();
}

//==========================================================================================
inline OBJECTHANDLE MethodTable::GetLoaderAllocatorObjectHandle()
{
    LIMITED_METHOD_CONTRACT;
    return GetLoaderAllocator()->GetLoaderAllocatorObjectHandle();
}

//==========================================================================================
FORCEINLINE OBJECTREF MethodTable::GetManagedClassObjectIfExists() 
{
    LIMITED_METHOD_CONTRACT;

    // Logging will be done by the slow path
    LOADERHANDLE handle = GetWriteableData_NoLogging()->GetExposedClassObjectHandle();

    OBJECTREF retVal;

    // GET_LOADERHANDLE_VALUE_FAST macro is inlined here to let us give hint to the compiler
    // when the return value is not null.
    if (!LoaderAllocator::GetHandleValueFast(handle, &retVal) &&
        !GetLoaderAllocator()->GetHandleValueFastPhase2(handle, &retVal))
    {
        return NULL;
    }

    COMPILER_ASSUME(retVal != NULL);
    return retVal;
}

//==========================================================================================
inline void MethodTable::SetIsArray(CorElementType arrayType, CorElementType elementType)
{
    STANDARD_VM_CONTRACT;

    DWORD category = enum_flag_Category_Array;
    if (arrayType == ELEMENT_TYPE_SZARRAY)
        category |= enum_flag_Category_IfArrayThenSzArray;

    _ASSERTE((m_dwFlags & enum_flag_Category_Mask) == 0);
    m_dwFlags |= category;

    _ASSERTE(GetInternalCorElementType() == arrayType);
}

//==========================================================================================
FORCEINLINE BOOL MethodTable::ImplementsInterfaceInline(MethodTable *pInterface)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        PRECONDITION(pInterface->IsInterface()); // class we are looking up should be an interface
    }
    CONTRACTL_END;

    //
    // Inline InterfaceMapIterator here for performance reasons
    //

    DWORD numInterfaces = GetNumInterfaces();
    if (numInterfaces == 0)
        return FALSE;

    InterfaceInfo_t *pInfo = GetInterfaceMap();

    do
    {
        if (pInfo->GetMethodTable() == pInterface)
        {
            // Extensible RCW's need to be handled specially because they can have interfaces
            // in their map that are added at runtime. These interfaces will have a start offset
            // of -1 to indicate this. We cannot take for granted that every instance of this
            // COM object has this interface so FindInterface on these interfaces is made to fail.
            //
            // However, we are only considering the statically available slots here
            // (m_wNumInterface doesn't contain the dynamic slots), so we can safely
            // ignore this detail.
            return TRUE;
        }
        pInfo++;
    }
    while (--numInterfaces);

    return FALSE;
}

#endif // !_METHODTABLE_INL_

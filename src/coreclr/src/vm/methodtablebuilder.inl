// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: METHODTABLEBUILDER.INL
//


//

//
// ============================================================================

#ifndef _METHODTABLEBUILDER_INL_
#define _METHODTABLEBUILDER_INL_

//***************************************************************************************
inline MethodTableBuilder::DeclaredMethodIterator::DeclaredMethodIterator(
            MethodTableBuilder &mtb) : 
                m_numDeclaredMethods((int)mtb.NumDeclaredMethods()),
                m_declaredMethods(mtb.bmtMethod->m_rgDeclaredMethods),
                m_idx(-1)
{
    LIMITED_METHOD_CONTRACT;
}

//***************************************************************************************
inline int MethodTableBuilder::DeclaredMethodIterator::CurrentIndex()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK_MSG(0 <= m_idx && m_idx < m_numDeclaredMethods,
                          "Invalid iterator state.");
    return m_idx;
}

//***************************************************************************************
inline BOOL MethodTableBuilder::DeclaredMethodIterator::Next()
{
    LIMITED_METHOD_CONTRACT;
    if (m_idx + 1 >= m_numDeclaredMethods)
        return FALSE;
    m_idx++;
    INDEBUG(m_debug_pMethod = GetMDMethod();)
    return TRUE;
}

//***************************************************************************************
inline BOOL MethodTableBuilder::DeclaredMethodIterator::Prev()
{
    LIMITED_METHOD_CONTRACT;
    if (m_idx - 1 <= -1)
        return FALSE;
    m_idx--;
    INDEBUG(m_debug_pMethod = GetMDMethod();)
    return TRUE;
}

//***************************************************************************************
inline void MethodTableBuilder::DeclaredMethodIterator::ResetToEnd()
{
    LIMITED_METHOD_CONTRACT;
    m_idx = m_numDeclaredMethods;
}

//***************************************************************************************
inline mdMethodDef MethodTableBuilder::DeclaredMethodIterator::Token() const
{
    STANDARD_VM_CONTRACT;
    CONSISTENCY_CHECK(TypeFromToken(GetMDMethod()->GetMethodSignature().GetToken()) == mdtMethodDef);
    return GetMDMethod()->GetMethodSignature().GetToken();
}

//***************************************************************************************
inline DWORD MethodTableBuilder::DeclaredMethodIterator::Attrs()
{
    LIMITED_METHOD_CONTRACT;
    return GetMDMethod()->GetDeclAttrs();
}

//***************************************************************************************
inline DWORD MethodTableBuilder::DeclaredMethodIterator::RVA()
{
    LIMITED_METHOD_CONTRACT;
    return GetMDMethod()->GetRVA();
}

//***************************************************************************************
inline DWORD MethodTableBuilder::DeclaredMethodIterator::ImplFlags()
{
    LIMITED_METHOD_CONTRACT;
    return GetMDMethod()->GetImplAttrs();
}

//***************************************************************************************
inline LPCSTR MethodTableBuilder::DeclaredMethodIterator::Name()
{
    STANDARD_VM_CONTRACT;
    return GetMDMethod()->GetMethodSignature().GetName();
}

//***************************************************************************************
inline PCCOR_SIGNATURE MethodTableBuilder::DeclaredMethodIterator::GetSig(DWORD *pcbSig)
{
    STANDARD_VM_CONTRACT;
    *pcbSig = static_cast<DWORD>
        (GetMDMethod()->GetMethodSignature().GetSignatureLength());
    return GetMDMethod()->GetMethodSignature().GetSignature();
}

//***************************************************************************************
inline MethodTableBuilder::METHOD_IMPL_TYPE MethodTableBuilder::DeclaredMethodIterator::MethodImpl()
{
    LIMITED_METHOD_CONTRACT;
    return GetMDMethod()->GetMethodImplType();
}

//***************************************************************************************
inline BOOL  MethodTableBuilder::DeclaredMethodIterator::IsMethodImpl()
{
    LIMITED_METHOD_CONTRACT;
    return MethodImpl() == METHOD_IMPL;
}

//***************************************************************************************
inline MethodTableBuilder::METHOD_TYPE MethodTableBuilder::DeclaredMethodIterator::MethodType()
{
    LIMITED_METHOD_CONTRACT;
    return GetMDMethod()->GetMethodType();
}

//***************************************************************************************
inline MethodTableBuilder::bmtMDMethod *
MethodTableBuilder::DeclaredMethodIterator::GetMDMethod() const
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(FitsIn<SLOT_INDEX>(m_idx)); // Review: m_idx should probably _be_ a SLOT_INDEX, but that asserts.
    return m_declaredMethods[m_idx];
}

//*******************************************************************************
inline class MethodDesc *
MethodTableBuilder::DeclaredMethodIterator::GetIntroducingMethodDesc()
{
    STANDARD_VM_CONTRACT;

    bmtMDMethod *pCurrentMD = GetMDMethod();
    DWORD dwSlot = pCurrentMD->GetSlotIndex();
    MethodDesc *pIntroducingMD  = NULL;

    bmtRTType *pParentType = pCurrentMD->GetOwningType()->GetParentType();
    bmtRTType *pPrevParentType = NULL;

    // Find this method in the parent.
    // If it does exist in the parent, it would be at the same vtable slot.
    while (pParentType != NULL &&
           dwSlot < pParentType->GetMethodTable()->GetNumVirtuals())
    {
        pPrevParentType = pParentType;
        pParentType = pParentType->GetParentType();
    }

    if (pPrevParentType != NULL)
    {
        pIntroducingMD =
            pPrevParentType->GetMethodTable()->GetMethodDescForSlot(dwSlot);
    }

    return pIntroducingMD;
}


//***************************************************************************************
inline MethodTableBuilder::bmtMDMethod *
MethodTableBuilder::DeclaredMethodIterator::operator->()
{
    return GetMDMethod();
}

//***************************************************************************************
inline bool
MethodTableBuilder::bmtMethodHandle::operator==(
    const bmtMethodHandle &rhs) const
{
    return m_handle == rhs.m_handle;
}

//***************************************************************************************
//
// The MethodNameHash is a temporary loader structure which may be allocated if there are a large number of
// methods in a class, to quickly get from a method name to a MethodDesc (potentially a chain of MethodDescs).
//

//***************************************************************************************
// Returns TRUE for success, FALSE for failure
template <typename Data>
void
FixedCapacityStackingAllocatedUTF8StringHash<Data>::Init(
    DWORD               dwMaxEntries,
    StackingAllocator * pAllocator)
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

    S_UINT32 scbMemory = (S_UINT32(m_dwNumBuckets) * S_UINT32(sizeof(HashEntry*))) +
                         (S_UINT32(dwMaxEntries) * S_UINT32(sizeof(HashEntry)));

    if(scbMemory.IsOverflow())
        ThrowHR(E_INVALIDARG);

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
    m_pBuckets      = (HashEntry**) m_pMemory;
    m_pMemory += sizeof(HashEntry*)*m_dwNumBuckets;

    // Buckets all point to empty lists to begin with
    memset(m_pBuckets, 0, scbMemory.Value());
}

//***************************************************************************************
// Insert new entry at head of list
template <typename Data>
void
FixedCapacityStackingAllocatedUTF8StringHash<Data>::Insert(
    LPCUTF8       pszName,
    const Data &  data)
{
    LIMITED_METHOD_CONTRACT;
    DWORD           dwHash = GetHashCode(pszName);
    DWORD           dwBucket = dwHash % m_dwNumBuckets;
    HashEntry *     pNewEntry;

    pNewEntry = (HashEntry *) m_pMemory;
    m_pMemory += sizeof(HashEntry);

    _ASSERTE(m_pMemory <= m_pDebugEndMemory);

    // Insert at head of bucket chain
    pNewEntry->m_pNext        = m_pBuckets[dwBucket];
    pNewEntry->m_data         = data;
    pNewEntry->m_dwHashValue  = dwHash;
    pNewEntry->m_pKey         = pszName;

    m_pBuckets[dwBucket] = pNewEntry;
}

//***************************************************************************************
// Return the first HashEntry with this name, or NULL if there is no such entry
template <typename Data>
typename FixedCapacityStackingAllocatedUTF8StringHash<Data>::HashEntry *
FixedCapacityStackingAllocatedUTF8StringHash<Data>::Lookup(
    LPCUTF8 pszName)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    DWORD dwHash = GetHashCode(pszName);
    DWORD dwBucket = dwHash % m_dwNumBuckets;
    HashEntry * pSearch;

    for (pSearch = m_pBuckets[dwBucket]; pSearch; pSearch = pSearch->m_pNext)
    {
        if (pSearch->m_dwHashValue == dwHash && !strcmp(pSearch->m_pKey, pszName))
        {
            return pSearch;
        }
    }

    return NULL;
}

//***************************************************************************************
// Return the first HashEntry with this name, or NULL if there is no such entry
template <typename Data>
typename FixedCapacityStackingAllocatedUTF8StringHash<Data>::HashEntry *
FixedCapacityStackingAllocatedUTF8StringHash<Data>::FindNext(
    HashEntry * pEntry)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    CONSISTENCY_CHECK(CheckPointer(pEntry));

    LPCUTF8 key = pEntry->m_pKey;
    DWORD   hash = pEntry->m_dwHashValue;

    pEntry = pEntry->m_pNext;
    while (pEntry != NULL)
    {
        if (pEntry->m_dwHashValue == hash &&
            strcmp(pEntry->m_pKey, key) == 0)
        {
            break;
        }
        pEntry = pEntry->m_pNext;
    }

    return pEntry;
}

#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE

//***************************************************************************************
#define CALL_TYPE_HANDLE_METHOD(m) \
    ((IsRTType()) ? (AsRTType()->m()) : (AsMDType()->m()))

//***************************************************************************************
inline MethodTableBuilder::bmtTypeHandle
MethodTableBuilder::bmtTypeHandle::GetParentType() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetParentType);
}

//***************************************************************************************
inline bool
MethodTableBuilder::bmtTypeHandle::IsNested() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(IsNested);
}

//***************************************************************************************
inline mdTypeDef
MethodTableBuilder::bmtTypeHandle::GetEnclosingTypeToken() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetEnclosingTypeToken);
}

//***************************************************************************************
inline Module *
MethodTableBuilder::bmtTypeHandle::GetModule() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetModule);
}

//***************************************************************************************
inline mdTypeDef
MethodTableBuilder::bmtTypeHandle::GetTypeDefToken() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetTypeDefToken);
}

//***************************************************************************************
inline const Substitution &
MethodTableBuilder::bmtTypeHandle::GetSubstitution() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetSubstitution);
}

//***************************************************************************************
inline MethodTable *
MethodTableBuilder::bmtTypeHandle::GetMethodTable() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetMethodTable);
}

//***************************************************************************************
inline DWORD
MethodTableBuilder::bmtTypeHandle::GetAttrs() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(GetAttrs);
}

//***************************************************************************************
inline bool
MethodTableBuilder::bmtTypeHandle::IsInterface() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_TYPE_HANDLE_METHOD(IsInterface);
}

#undef CALL_TYPE_HANDLE_METHOD

//***************************************************************************************
#define CALL_METHOD_HANDLE_METHOD(m) \
    ((IsRTMethod()) ? (AsRTMethod()->m()) : (AsMDMethod()->m()))

//***************************************************************************************
inline MethodTableBuilder::bmtTypeHandle
MethodTableBuilder::bmtMethodHandle::GetOwningType() const
{
    LIMITED_METHOD_CONTRACT;
    if (IsRTMethod())
        return bmtTypeHandle(AsRTMethod()->GetOwningType());
    else
        return bmtTypeHandle(AsMDMethod()->GetOwningType());
}

//***************************************************************************************
inline DWORD
MethodTableBuilder::bmtMethodHandle::GetDeclAttrs() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_METHOD_HANDLE_METHOD(GetDeclAttrs);
}

//***************************************************************************************
inline DWORD
MethodTableBuilder::bmtMethodHandle::GetImplAttrs() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_METHOD_HANDLE_METHOD(GetImplAttrs);
}

//***************************************************************************************
inline MethodTableBuilder::SLOT_INDEX
MethodTableBuilder::bmtMethodHandle::GetSlotIndex() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_METHOD_HANDLE_METHOD(GetSlotIndex);
}

//***************************************************************************************
inline const MethodTableBuilder::MethodSignature &
MethodTableBuilder::bmtMethodHandle::GetMethodSignature() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_METHOD_HANDLE_METHOD(GetMethodSignature);
}

//***************************************************************************************
inline MethodDesc *
MethodTableBuilder::bmtMethodHandle::GetMethodDesc() const
{
    LIMITED_METHOD_CONTRACT;
    return CALL_METHOD_HANDLE_METHOD(GetMethodDesc);
}

#undef CALL_METHOD_HANDLE_METHOD

//***************************************************************************************
inline DWORD
MethodTableBuilder::bmtRTMethod::GetDeclAttrs() const
{
    LIMITED_METHOD_CONTRACT;
    return GetMethodDesc()->GetAttrs();
}

//***************************************************************************************
inline DWORD
MethodTableBuilder::bmtRTMethod::GetImplAttrs() const
{
    LIMITED_METHOD_CONTRACT;
    return GetMethodDesc()->GetImplAttrs();
}

//***************************************************************************************
inline MethodTableBuilder::SLOT_INDEX
MethodTableBuilder::bmtRTMethod::GetSlotIndex() const
{
    LIMITED_METHOD_CONTRACT;
    return GetMethodDesc()->GetSlot();
}

//***************************************************************************************
inline void
MethodTableBuilder::bmtMDMethod::SetSlotIndex(SLOT_INDEX idx)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(m_pMD == NULL);
    m_slotIndex = idx;
}

//***************************************************************************************
inline void
MethodTableBuilder::bmtMDMethod::SetUnboxedSlotIndex(SLOT_INDEX idx)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(m_pUnboxedMD == NULL);
    m_unboxedSlotIndex = idx;
}

//***************************************************************************************
inline DWORD
MethodTableBuilder::GetMethodClassification(MethodTableBuilder::METHOD_TYPE type)
{
    LIMITED_METHOD_CONTRACT;
    // Verify that the enums are in sync, so we can do the conversion by simple cast.
    C_ASSERT((DWORD)METHOD_TYPE_NORMAL       == (DWORD)mcIL);
    C_ASSERT((DWORD)METHOD_TYPE_FCALL        == (DWORD)mcFCall);
    C_ASSERT((DWORD)METHOD_TYPE_NDIRECT      == (DWORD)mcNDirect);
    C_ASSERT((DWORD)METHOD_TYPE_EEIMPL       == (DWORD)mcEEImpl);
    C_ASSERT((DWORD)METHOD_TYPE_INSTANTIATED == (DWORD)mcInstantiated);
#ifdef FEATURE_COMINTEROP
    C_ASSERT((DWORD)METHOD_TYPE_COMINTEROP   == (DWORD)mcComInterop);
#endif

    return (DWORD)type;
}

#endif  // _METHODTABLEBUILDER_INL_


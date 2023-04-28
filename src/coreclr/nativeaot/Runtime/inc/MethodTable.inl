// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __eetype_inl__
#define __eetype_inl__
//-----------------------------------------------------------------------------------------------------------
inline uint32_t MethodTable::GetHashCode()
{
    return m_uHashCode;
}

//-----------------------------------------------------------------------------------------------------------
inline PTR_Code MethodTable::get_Slot(uint16_t slotNumber)
{
    ASSERT(slotNumber < m_usNumVtableSlots);
    return *get_SlotPtr(slotNumber);
}

//-----------------------------------------------------------------------------------------------------------
inline PTR_PTR_Code MethodTable::get_SlotPtr(uint16_t slotNumber)
{
    ASSERT(slotNumber < m_usNumVtableSlots);
    return dac_cast<PTR_PTR_Code>(dac_cast<TADDR>(this) + offsetof(MethodTable, m_VTable)) + slotNumber;
}

#ifdef DACCESS_COMPILE
inline bool MethodTable::DacVerify()
{
    // Use a separate static worker because the worker validates
    // the whole chain of EETypes and we don't want to accidentally
    // answer questions from 'this' that should have come from the
    // 'current' MethodTable.
    return DacVerifyWorker(this);
}
// static
inline bool MethodTable::DacVerifyWorker(MethodTable* pThis)
{
    //*********************************************************************
    //**** ASSUMES MAX TYPE HIERARCHY DEPTH OF 1024 TYPES              ****
    //*********************************************************************
    const int MAX_SANE_RELATED_TYPES = 1024;
    //*********************************************************************
    //**** ASSUMES MAX OF 200 INTERFACES IMPLEMENTED ON ANY GIVEN TYPE ****
    //*********************************************************************
    const int MAX_SANE_NUM_INSTANCES = 200;


    PTR_EEType pCurrentType = dac_cast<PTR_EEType>(pThis);
    for (int i = 0; i < MAX_SANE_RELATED_TYPES; i++)
    {
        // Verify interface map
        if (pCurrentType->GetNumInterfaces() > MAX_SANE_NUM_INSTANCES)
            return false;

        // Validate the current type
        if (!pCurrentType->Validate(false))
            return false;

        //
        // Now on to the next type in the hierarchy.
        //

        pCurrentType = dac_cast<PTR_EEType>(reinterpret_cast<TADDR>(pCurrentType->m_RelatedType.m_pBaseType));

        if (pCurrentType == NULL)
            break;
    }

    if (pCurrentType != NULL)
        return false;   // assume we found an infinite loop

    return true;
}
#endif

#if !defined(DACCESS_COMPILE)
inline PTR_UInt8 FollowRelativePointer(const int32_t* pDist)
{
    int32_t dist = *pDist;

    PTR_UInt8 result = (PTR_UInt8)pDist + dist;

    return result;
}

inline TypeManagerHandle* MethodTable::GetTypeManagerPtr()
{
    uint32_t cbOffset = GetFieldOffset(ETF_TypeManagerIndirection);

#if !defined(USE_PORTABLE_HELPERS)
    if (!IsDynamicType())
    {
        return (TypeManagerHandle*)FollowRelativePointer((int32_t*)((uint8_t*)this + cbOffset));
    }
    else
#endif
    {
        return *(TypeManagerHandle**)((uint8_t*)this + cbOffset);
    }
}
#endif // !defined(DACCESS_COMPILE)

// Calculate the offset of a field of the MethodTable that has a variable offset.
__forceinline uint32_t MethodTable::GetFieldOffset(EETypeField eField)
{
    // First part of MethodTable consists of the fixed portion followed by the vtable.
    uint32_t cbOffset = offsetof(MethodTable, m_VTable) + (sizeof(UIntTarget) * m_usNumVtableSlots);

    // Then we have the interface map.
    if (eField == ETF_InterfaceMap)
    {
        ASSERT(GetNumInterfaces() > 0);
        return cbOffset;
    }
    cbOffset += sizeof(MethodTable*) * GetNumInterfaces();

    const uint32_t relativeOrFullPointerOffset =
#if USE_PORTABLE_HELPERS
        sizeof(UIntTarget);
#else
        IsDynamicType() ? sizeof(UIntTarget) : sizeof(uint32_t);
#endif

    // Followed by the type manager indirection cell.
    if (eField == ETF_TypeManagerIndirection)
    {
        return cbOffset;
    }
    cbOffset += relativeOrFullPointerOffset;

#if SUPPORTS_WRITABLE_DATA
    // Followed by writable data.
    if (eField == ETF_WritableData)
    {
        return cbOffset;
    }
    cbOffset += relativeOrFullPointerOffset;
#endif

    // Followed by the pointer to the finalizer method.
    if (eField == ETF_Finalizer)
    {
        ASSERT(HasFinalizer());
        return cbOffset;
    }
    if (HasFinalizer())
        cbOffset += relativeOrFullPointerOffset;

    // Followed by the pointer to the optional fields.
    if (eField == ETF_OptionalFieldsPtr)
    {
        ASSERT(HasOptionalFields());
        return cbOffset;
    }
    if (HasOptionalFields())
        cbOffset += relativeOrFullPointerOffset;

    // Followed by the pointer to the sealed virtual slots
    if (eField == ETF_SealedVirtualSlots)
        return cbOffset;

    ASSERT(!"Decoding the rest requires rare flags");

    return 0;
}
#endif // __eetype_inl__

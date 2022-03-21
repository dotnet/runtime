// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Low-level types describing GC object layouts.
//

// Bits stolen from the sync block index that the GC/HandleTable knows about (currently these are at the same
// positions as the mainline runtime but we can change this below when it becomes apparent how Redhawk will
// handle sync blocks).
#define BIT_SBLK_GC_RESERVE                 0x20000000
#define BIT_SBLK_FINALIZER_RUN              0x40000000

// The sync block index header (small structure that immediately precedes every object in the GC heap). Only
// the GC uses this so far, and only to store a couple of bits of information.
class ObjHeader
{
private:
#if defined(HOST_64BIT)
    uint32_t   m_uAlignpad;
#endif // HOST_64BIT
    uint32_t   m_uSyncBlockValue;

public:
    uint32_t GetBits() { return m_uSyncBlockValue; }
    void SetBit(uint32_t uBit);
    void ClrBit(uint32_t uBit);
    void SetGCBit() { m_uSyncBlockValue |= BIT_SBLK_GC_RESERVE; }
    void ClrGCBit() { m_uSyncBlockValue &= ~BIT_SBLK_GC_RESERVE; }
};

//-------------------------------------------------------------------------------------------------
static uintptr_t const SYNC_BLOCK_SKEW  = sizeof(void *);

class MethodTable;
typedef DPTR(class MethodTable) PTR_EEType;
class MethodTable;

//-------------------------------------------------------------------------------------------------
class Object
{
    friend class AsmOffsets;

    PTR_EEType  m_pEEType;
public:
    MethodTable * get_EEType() const
        { return m_pEEType; }
    MethodTable * get_SafeEEType() const
#ifdef TARGET_64BIT
        { return dac_cast<PTR_EEType>((dac_cast<TADDR>(m_pEEType)) & ~((uintptr_t)7)); }
#else
        { return dac_cast<PTR_EEType>((dac_cast<TADDR>(m_pEEType)) & ~((uintptr_t)3)); }
#endif
    ObjHeader * GetHeader() { return dac_cast<DPTR(ObjHeader)>(dac_cast<TADDR>(this) - SYNC_BLOCK_SKEW); }
#ifndef DACCESS_COMPILE
    void set_EEType(MethodTable * pEEType)
        { m_pEEType = pEEType; }
    void InitEEType(MethodTable * pEEType);

    size_t GetSize();
#endif

    //
    // Adapter methods for GC code so that GC and runtime code can use the same type.
    // These methods are deprecated -- only use from existing GC code.
    //
    MethodTable * RawGetMethodTable() const
    {
        return (MethodTable*)get_EEType();
    }
    MethodTable * GetGCSafeMethodTable() const
    {
        return (MethodTable *)get_SafeEEType();
    }
    void RawSetMethodTable(MethodTable * pMT)
    {
        m_pEEType = PTR_EEType((MethodTable *)pMT);
    }
    ////// End adaptor methods
};
typedef DPTR(Object) PTR_Object;
typedef DPTR(PTR_Object) PTR_PTR_Object;

//-------------------------------------------------------------------------------------------------
static uintptr_t const MIN_OBJECT_SIZE  = (2 * sizeof(void*)) + sizeof(ObjHeader);

//-------------------------------------------------------------------------------------------------
static uintptr_t const REFERENCE_SIZE   = sizeof(Object *);

//-------------------------------------------------------------------------------------------------
class Array : public Object
{
    friend class ArrayBase;
    friend class AsmOffsets;

    uint32_t       m_Length;
#if defined(HOST_64BIT)
    uint32_t       m_uAlignpad;
#endif // HOST_64BIT
public:
    uint32_t GetArrayLength();
    void InitArrayLength(uint32_t length);
    void* GetArrayData();
};
typedef DPTR(Array) PTR_Array;

//-------------------------------------------------------------------------------------------------
class String : public Object
{
    friend class AsmOffsets;
    friend class StringConstants;

    uint32_t       m_Length;
    uint16_t       m_FirstChar;
};
typedef DPTR(String) PTR_String;

//-------------------------------------------------------------------------------------------------
class StringConstants
{
public:
    static uintptr_t const ComponentSize = sizeof(((String*)0)->m_FirstChar);
    static uintptr_t const BaseSize = sizeof(ObjHeader) + offsetof(String, m_FirstChar) + ComponentSize;
};

//-------------------------------------------------------------------------------------------------
static uintptr_t const STRING_COMPONENT_SIZE = StringConstants::ComponentSize;

//-------------------------------------------------------------------------------------------------
static uintptr_t const STRING_BASE_SIZE = StringConstants::BaseSize;

//-------------------------------------------------------------------------------------------------
static uintptr_t const MAX_STRING_LENGTH = 0x3FFFFFDF;

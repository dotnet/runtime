//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//-------------------------------------------------------------------------------------------------
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
#if defined(BIT64)
    uint32_t m_uAlignpad;
#endif // BIT64
    uint32_t m_uSyncBlockValue;

public:
    uint32_t GetBits() { return m_uSyncBlockValue; }
    void SetBit(uint32_t uBit) { Interlocked::Or(&m_uSyncBlockValue, uBit); }
    void ClrBit(uint32_t uBit) { Interlocked::And(&m_uSyncBlockValue, ~uBit); }
    void SetGCBit() { m_uSyncBlockValue |= BIT_SBLK_GC_RESERVE; }
    void ClrGCBit() { m_uSyncBlockValue &= ~BIT_SBLK_GC_RESERVE; }
};

#define MTFlag_ContainsPointers 1
#define MTFlag_HasFinalizer 2
#define MTFlag_IsArray 4

class MethodTable
{
public:
    uint16_t    m_componentSize;
    uint16_t    m_flags;
    uint32_t    m_baseSize;

    MethodTable * m_pRelatedType;

public:
    void InitializeFreeObject()
    {
        m_baseSize = 3 * sizeof(void *);
        m_componentSize = 1;
        m_flags = 0;
    }

    uint32_t GetBaseSize()
    {
        return m_baseSize;
    }

    uint16_t RawGetComponentSize()
    {
        return m_componentSize;
    }

    bool ContainsPointers()
    {
        return (m_flags & MTFlag_ContainsPointers) != 0;
    }

    bool ContainsPointersOrCollectible()
    {
        return ContainsPointers();
    }

    bool HasComponentSize()
    {
        return m_componentSize != 0;
    }

    bool HasFinalizer()
    {
        return (m_flags & MTFlag_HasFinalizer) != 0;
    }

    bool HasCriticalFinalizer()
    {
        return false;
    }

    bool IsArray()
    {
        return (m_flags & MTFlag_IsArray) != 0;
    }

    MethodTable * GetParent()
    {
        _ASSERTE(!IsArray());
        return m_pRelatedType;
    }

    bool SanityCheck()
    {
        return true;
    }
};

class Object
{
    MethodTable * m_pMethTab;

public:
    ObjHeader * GetHeader()
    { 
        return ((ObjHeader *)this) - 1;
    }

    MethodTable * RawGetMethodTable() const
    {
        return m_pMethTab;
    }

    MethodTable * GetGCSafeMethodTable() const
    {
        return (MethodTable *)((uintptr_t)m_pMethTab & ~3);
    }

    void RawSetMethodTable(MethodTable * pMT)
    {
        m_pMethTab = pMT;
    }
};
#define MIN_OBJECT_SIZE     (2*sizeof(uint8_t*) + sizeof(ObjHeader))

class ArrayBase : public Object
{
    uint32_t m_dwLength;

public:
    uint32_t GetNumComponents()
    {
        return m_dwLength;
    }

    static size_t GetOffsetOfNumComponents()
    {
        return offsetof(ArrayBase, m_dwLength);
    }
};

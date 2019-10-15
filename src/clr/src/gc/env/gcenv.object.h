// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCENV_OBJECT_H__
#define __GCENV_OBJECT_H__

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

static_assert(sizeof(ObjHeader) == sizeof(uintptr_t), "this assumption is made by the VM!");

#define MTFlag_ContainsPointers     0x0100
#define MTFlag_HasCriticalFinalizer 0x0800
#define MTFlag_HasFinalizer         0x0010
#define MTFlag_IsArray              0x0008
#define MTFlag_Collectible          0x1000
#define MTFlag_HasComponentSize     0x8000

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
        m_flags = MTFlag_HasComponentSize | MTFlag_IsArray;
    }

    uint32_t GetBaseSize()
    {
        return m_baseSize;
    }

    uint16_t RawGetComponentSize()
    {
        return m_componentSize;
    }

    bool Collectible()
    {
        return (m_flags & MTFlag_Collectible) != 0;
    }

    bool ContainsPointers()
    {
        return (m_flags & MTFlag_ContainsPointers) != 0;
    }

    bool ContainsPointersOrCollectible()
    {
        return ContainsPointers() || Collectible();
    }

    bool HasComponentSize()
    {
        // Note that we can't just check m_componentSize != 0 here. The VM 
        // may still construct a method table that does not have a component 
        // size, according to this method, but still has a number in the low 
        // 16 bits of the method table flags parameter.
        //
        // The solution here is to do what the VM does and check the
        // HasComponentSize flag so that we're on the same page.
        return (m_flags & MTFlag_HasComponentSize) != 0;
    }

    bool HasFinalizer()
    {
        return (m_flags & MTFlag_HasFinalizer) != 0;
    }

    bool HasCriticalFinalizer()
    {
        return (m_flags & MTFlag_HasCriticalFinalizer) != 0;
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

#endif // __GCENV_OBJECT_H__

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_OBJECT_MONO_H__
#define __GCENV_OBJECT_MONO_H__


#include "gcenv.interlocked.h"

// ARM requires that 64-bit primitive types are aligned at 64-bit boundaries for interlocked-like operations.
// Additionally the platform ABI requires these types and composite type containing them to be similarly
// aligned when passed as arguments.
#ifdef TARGET_ARM
#define FEATURE_64BIT_ALIGNMENT
#endif

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
#if defined(HOST_64BIT)
    uint32_t m_uAlignpad;
#endif // HOST_64BIT
    uint32_t m_uSyncBlockValue;

public:
    uint32_t GetBits() { return m_uSyncBlockValue; }
    void SetBit(uint32_t uBit) { Interlocked::Or(&m_uSyncBlockValue, uBit); }
    void ClrBit(uint32_t uBit) { Interlocked::And(&m_uSyncBlockValue, ~uBit); }
    void SetGCBit() { m_uSyncBlockValue |= BIT_SBLK_GC_RESERVE; }
    void ClrGCBit() { m_uSyncBlockValue &= ~BIT_SBLK_GC_RESERVE; }
};

static_assert(sizeof(ObjHeader) == sizeof(uintptr_t), "this assumption is made by the VM!");

#define MTFlag_RequireAlign8            0x00001000
#define MTFlag_Category_ValueType       0x00040000
#define MTFlag_Category_ValueType_Mask  0x000C0000
#define MTFlag_ContainsPointers         0x01000000
#define MTFlag_HasCriticalFinalizer     0x08000000
#define MTFlag_IsString                 0x00000004
#define MTFlag_HasFinalizer             0x00100000
#define MTFlag_IsArray                  0x00080000
#define MTFlag_Collectible              0x10000000
#define MTFlag_HasComponentSize         0x80000000

typedef struct {
    uint16_t    m_componentSize;
    uint16_t    m_flags;
    uint32_t    m_baseSize;
} mono_gc_descr;

class MethodTable
{
public:
    mono_gc_descr gc_descr;

public:
    void InitializeFreeObject()
    {
        gc_descr.m_baseSize = 4 * sizeof(void *);
        gc_descr.m_flags = MTFlag_HasComponentSize | MTFlag_IsArray;
        gc_descr.m_componentSize = 1;
    }

    uint32_t GetBaseSize()
    {
        return gc_descr.m_baseSize;
    }

    uint16_t RawGetComponentSize()
    {
        return gc_descr.m_componentSize;
    }

    bool Collectible()
    {
        return (gc_descr.m_flags & MTFlag_Collectible) != 0;
    }

    bool ContainsPointers()
    {
        return (gc_descr.m_flags & MTFlag_ContainsPointers) != 0;
    }

    bool ContainsPointersOrCollectible()
    {
        return ContainsPointers() || Collectible();
    }

    bool RequiresAlign8()
    {
        return (gc_descr.m_flags & MTFlag_RequireAlign8) != 0;
    }

    bool IsValueType()
    {
        return (gc_descr.m_flags & MTFlag_Category_ValueType_Mask) == MTFlag_Category_ValueType;
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
        return (gc_descr.m_flags & MTFlag_HasComponentSize) != 0;
    }

    bool HasFinalizer()
    {
        return (gc_descr.m_flags & MTFlag_HasFinalizer) != 0;
    }

    bool HasCriticalFinalizer()
    {
        return (gc_descr.m_flags & MTFlag_HasCriticalFinalizer) != 0;
    }

    bool IsArray()
    {
        return (gc_descr.m_flags & MTFlag_IsArray) != 0;
    }

    MethodTable * GetParent()
    {
        return NULL;
    }

    bool SanityCheck()
    {
        return true;
    }
};

class Object
{
    MethodTable * m_pMethTab;
    void* sync_lock;

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

// CoreGC doesn't accept objects smaller than this, since it needs to replace them with array fill
// vtable which contains header_ptr, vtable_ptr, sync_ptr and length
#define MIN_OBJECT_SIZE     (4*sizeof(uint8_t*))

class ArrayBase : public Object
{
    uint32_t m_dwLength;

public:
    uint32_t GetNumComponents()
    {
        // Null terminator is not included in the length
	    if (GetGCSafeMethodTable ()->gc_descr.m_flags & MTFlag_IsString)
        {
		    return m_dwLength + 1;
        }
        return m_dwLength;
    }

    static size_t GetOffsetOfNumComponents()
    {
        return offsetof(ArrayBase, m_dwLength);
    }
};

#endif // __GCENV_OBJECT_MONO_H__

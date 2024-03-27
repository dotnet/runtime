// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_OBJECT_H__
#define __GCENV_OBJECT_H__

#ifdef BUILD_AS_STANDALONE
extern bool g_oldMethodTableFlags;
#endif

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
// positions as the mainline runtime but we can change this below when it becomes apparent how NativeAOT will
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

#define MTFlag_RequiresAlign8           0x00001000 // enum_flag_RequiresAlign8
#define MTFlag_Category_ValueType       0x00040000 // enum_flag_Category_ValueType
#define MTFlag_Category_ValueType_Mask  0x000C0000 // enum_flag_Category_ValueType_Mask
#define MTFlag_ContainsPointers         0x01000000 // enum_flag_ContainsPointers
#define MTFlag_HasCriticalFinalizer     0x00000002 // enum_flag_HasCriticalFinalizer
#define MTFlag_HasFinalizer             0x00100000 // enum_flag_HasFinalizer
#define MTFlag_IsArray                  0x00080000 // enum_flag_Category_Array
#define MTFlag_Collectible              0x00200000 // enum_flag_Collectible
#define MTFlag_HasComponentSize         0x80000000 // enum_flag_HasComponentSize

class MethodTable
{
public:
    union
    {
        uint16_t    m_componentSize;
        uint32_t    m_flags;
    };

    uint32_t    m_baseSize;

    MethodTable * m_pRelatedType;

public:
    void InitializeFreeObject()
    {
        m_baseSize = 3 * sizeof(void *);
        m_flags = MTFlag_HasComponentSize | MTFlag_IsArray;
        m_componentSize = 1;
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
#ifdef BUILD_AS_STANDALONE
        if (g_oldMethodTableFlags)
        {
            // This flag is used for .NET 8 or below
            const int Old_MTFlag_Collectible = 0x10000000;
            return (m_flags & Old_MTFlag_Collectible) != 0;
        }
#endif
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

    bool RequiresAlign8()
    {
        return (m_flags & MTFlag_RequiresAlign8) != 0;
    }

    bool IsValueType()
    {
        return (m_flags & MTFlag_Category_ValueType_Mask) == MTFlag_Category_ValueType;
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
#ifdef BUILD_AS_STANDALONE
        if (g_oldMethodTableFlags)
        {
            // This flag is used for .NET 8 or below
            const int Old_MTFlag_HasCriticalFinalizer = 0x08000000;
            return (m_flags & Old_MTFlag_HasCriticalFinalizer) != 0;
        }
#endif
        return !HasComponentSize() && (m_flags & MTFlag_HasCriticalFinalizer);
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
#ifdef HOST_64BIT
        return (MethodTable *)((uintptr_t)m_pMethTab & ~7);
#else
        return (MethodTable *)((uintptr_t)m_pMethTab & ~3);
#endif //HOST_64BIT
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

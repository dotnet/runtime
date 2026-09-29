// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CACHEDINTERFACEDISPATCHPAL_H__
#define __CACHEDINTERFACEDISPATCHPAL_H__

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

extern "C" void RhpInitialInterfaceDispatch();


bool InterfaceDispatch_InitializePal();

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size);
// Allocate memory aligned at sizeof(void*) boundaries

void *InterfaceDispatch_AllocPointerAligned(size_t size);

enum Flags
{
    // The low 2 bits of the m_pCache pointer are treated specially so that we can avoid the need for
    // extra fields on this type.
    // OR if the m_pCache value is less than 0x1000 then this is a vtable offset and should be used as such
    IDC_CachePointerPointsIsVTableOffset = 0x2,
    IDC_CachePointerPointsAtCache = 0x0,
    IDC_CachePointerMask = 0x3,
    IDC_CachePointerMaskShift = 0x2,
};

enum class DispatchCellType
{
    InterfaceAndSlot = 0x0,
    VTableOffset = 0x2,
};

struct DispatchCellInfo
{
private:
    static DispatchCellType CellTypeFromToken(DispatchToken token)
    {
        if (token.IsThisToken())
        {
            return DispatchCellType::VTableOffset;
        }
        return DispatchCellType::InterfaceAndSlot;
    }
public: 

    DispatchCellInfo(DispatchToken token, bool hasCache) :
        CellType(CellTypeFromToken(token)),
        Token(token),
        HasCache(hasCache ? 1 : 0)
    {

    }
    const DispatchCellType CellType;
    const DispatchToken Token;

    uintptr_t GetVTableOffset() const
    {
        if (CellType == DispatchCellType::VTableOffset)
        {
            // The vtable offset is stored in a pointer sized field, but actually represents 2 values.
            // 1. The offset of the first indirection to use. which is stored in the upper half of the
            //    pointer sized field (bits 16-31 of a 32 bit pointer, or bits 32-63 of a 64 bit pointer).
            //
            // 2. The offset of the second indirection, which is a stored is the upper half of the lower
            //    half of the pointer size field (bits 8-15 of a 32 bit pointer, or bits 16-31 of a 64
            //    bit pointer) This second offset is always less than 255, so we only really need a single
            //    byte, and the assembly code on some architectures may take a dependency on that
            //    so the VTableOffsetToSlot function has a mask to ensure that it is only ever a single byte.
            uint32_t slot = Token.GetSlotNumber();
            unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
            unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;

            uintptr_t offsetOfIndirectionPortion = (((uintptr_t)offsetOfIndirection) << ((TARGET_POINTER_SIZE * 8) / 2));
            uintptr_t offsetAfterIndirectionPortion = (((uintptr_t)offsetAfterIndirection) << ((TARGET_POINTER_SIZE * 8) / 4));
            uintptr_t flagPortion = (uintptr_t)IDC_CachePointerPointsIsVTableOffset;

            uintptr_t result = offsetOfIndirectionPortion | offsetAfterIndirectionPortion | flagPortion;
            _ASSERTE(slot == VTableOffsetToSlot(result));
            return result;
        }
        return 0;
    }

    static unsigned VTableOffsetToSlot(uintptr_t vtableOffset)
    {
        // See comment in GetVTableOffset() for what we're doing here.
        unsigned offsetOfIndirection = (unsigned)(vtableOffset >> ((TARGET_POINTER_SIZE * 8) / 2));
        unsigned offsetAfterIndirection = (unsigned)(vtableOffset >> ((TARGET_POINTER_SIZE * 8) / 4)) & 0xFF;
        unsigned slotGroupPerChunk = (offsetOfIndirection - MethodTable::GetVtableOffset()) / TARGET_POINTER_SIZE;
        unsigned slot = (slotGroupPerChunk * VTABLE_SLOTS_PER_CHUNK) + (offsetAfterIndirection / TARGET_POINTER_SIZE);
        return slot;
    }

    const uint8_t HasCache = 0;
};

struct InterfaceDispatchCacheHeader
{
private:
    enum Flags
    {
        CH_TypeAndSlotIndex = 0x0,
        CH_MetadataToken = 0x1,
        CH_Mask = 0x3,
        CH_Shift = 0x2,
    };

public:
    void Initialize(DispatchToken token)
    {
        m_token = token;
    }

    void Initialize(const DispatchCellInfo *pNewCellInfo)
    {
        m_token = pNewCellInfo->Token;
    }

    DispatchCellInfo GetDispatchCellInfo()
    {
        DispatchCellInfo cellInfo(m_token, true);
        return cellInfo;
    }

private:
    DispatchToken m_token;
    TADDR padding; // Ensure that the size of this structure is a multiple of 2 pointers
};

// One of these is allocated per interface call site. It holds the stub to call, data to pass to that stub
// (cache information) and the interface contract, i.e. the interface type and slot being called.
struct InterfaceDispatchCell
{
    // The first two fields must remain together and at the beginning of the structure. This is due to the
    // synchronization requirements of the code that updates these at runtime and the instructions generated
    // by the binder for interface call sites.
    TADDR      m_pStub;    // Call this code to execute the interface dispatch
    Volatile<TADDR> m_pCache;   // Context used by the stub above (one or both of the low two bits are set
                                    // for initial dispatch, and if not set, using this as a cache pointer or
                                    // as a vtable offset.)
    DispatchCellInfo GetDispatchCellInfo()
    {
        // Capture m_pCache into a local for safe access (this is a volatile read of a value that may be
        // modified on another thread while this function is executing.)
        TADDR cachePointerValue = m_pCache;

        if (IsCache(cachePointerValue))
        {
            return ((InterfaceDispatchCacheHeader*)cachePointerValue)->GetDispatchCellInfo();
        }
        else if (DispatchToken::IsCachedInterfaceDispatchToken(cachePointerValue))
        {
            return DispatchCellInfo(DispatchToken::FromCachedInterfaceDispatchToken(cachePointerValue), false);
        }
        else
        {
            _ASSERTE(IsVTableOffset(cachePointerValue));
            unsigned slot = DispatchCellInfo::VTableOffsetToSlot(cachePointerValue);
            return DispatchCellInfo(DispatchToken::CreateDispatchToken(slot), false);
        }
    }

    static bool IsCache(TADDR value)
    {
        return (value & IDC_CachePointerMask) == 0;
    }

    static bool IsVTableOffset(TADDR value)
    {
        return (value & IDC_CachePointerPointsIsVTableOffset) == IDC_CachePointerPointsIsVTableOffset;
    }

    InterfaceDispatchCacheHeader* GetCache() const
    {
        // Capture m_pCache into a local for safe access (this is a volatile read of a value that may be
        // modified on another thread while this function is executing.)
        TADDR cachePointerValue = m_pCache;
        if (IsCache(cachePointerValue))
        {
            return (InterfaceDispatchCacheHeader*)cachePointerValue;
        }
        else
        {
            return nullptr;
        }
    }
};

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

#endif // __CACHEDINTERFACEDISPATCHPAL_H__
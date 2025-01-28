// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CACHEDINTERFACEDISPATCHPAL_H__
#define __CACHEDINTERFACEDISPATCHPAL_H__

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

extern "C" void RhpInitialInterfaceDispatch();

#ifndef HOST_WINDOWS
#if defined(HOST_AMD64) || defined(HOST_ARM64) || defined(HOST_LOONGARCH64)
FORCEINLINE uint8_t PalInterlockedCompareExchange128(_Inout_ int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparandAndResult)
{
    __int128_t iComparand = ((__int128_t)pComparandAndResult[1] << 64) + (uint64_t)pComparandAndResult[0];
    // TODO-LOONGARCH64: for LoongArch64, it supports 128bits atomic from 3A6000-CPU which is ISA1.1's version.
    // The LA64's compiler will translate the `__sync_val_compare_and_swap` into calling the libatomic's library interface to emulate
    // the 128-bit CAS by mutex_lock if the target processor doesn't support the ISA1.1.
    // But this emulation by libatomic doesn't satisfy requirements here which it must update two adjacent pointers atomically.
    // this is being discussed in https://github.com/dotnet/runtime/issues/109276.
    __int128_t iResult = __sync_val_compare_and_swap((__int128_t volatile*)pDst, iComparand, ((__int128_t)iValueHigh << 64) + (uint64_t)iValueLow);
    PAL_InterlockedOperationBarrier();
    pComparandAndResult[0] = (int64_t)iResult; pComparandAndResult[1] = (int64_t)(iResult >> 64);
    return iComparand == iResult;
}
#endif // HOST_AMD64 || HOST_ARM64 || HOST_LOONGARCH64
#else // HOST_WINDOWS
#if defined(HOST_AMD64) || defined(HOST_ARM64)
EXTERN_C uint8_t _InterlockedCompareExchange128(int64_t volatile *, int64_t, int64_t, int64_t *);
#pragma intrinsic(_InterlockedCompareExchange128)
FORCEINLINE uint8_t PalInterlockedCompareExchange128(_Inout_ int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparandAndResult)
{
    return _InterlockedCompareExchange128(pDst, iValueHigh, iValueLow, pComparandAndResult);
}
#endif // HOST_AMD64 || HOST_ARM64
#endif // HOST_WINDOWS

bool InterfaceDispatch_InitializePal();

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size);
// Allocate memory aligned at at least sizeof(void*)
void *InterfaceDispatch_AllocPointerAligned(size_t size);

enum Flags
{
    // The low 2 bits of the m_pCache pointer are treated specially so that we can avoid the need for
    // extra fields on this type.
    // OR if the m_pCache value is less than 0x1000 then this it is a vtable offset and should be used as such
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
    volatile TADDR m_pCache;   // Context used by the stub above (one or both of the low two bits are set
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
        if ((value & IDC_CachePointerMask) != 0)
        {
            return false;
        }
        else
        {
            return true;
        }
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
            return 0;
        }
    }
};

#endif // FEATURE_CACHED_INTERFACE_DISPATCH

#endif // __CACHEDINTERFACEDISPATCHPAL_H__
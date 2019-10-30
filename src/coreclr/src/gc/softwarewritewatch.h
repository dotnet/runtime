// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __SOFTWARE_WRITE_WATCH_H__
#define __SOFTWARE_WRITE_WATCH_H__

#include "gcinterface.h"
#include "gc.h"

#define WRITE_WATCH_UNIT_SIZE ((size_t)0x1000)

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#ifndef DACCESS_COMPILE

extern "C"
{
    // Table containing the dirty state. This table is translated to exclude the lowest address it represents, see
    // TranslateTableToExcludeHeapStartAddress.
    extern uint8_t *g_gc_sw_ww_table;

    // Write watch may be disabled when it is not needed (between GCs for instance). This indicates whether it is enabled.
    extern bool g_gc_sw_ww_enabled_for_gc_heap;
}

class SoftwareWriteWatch
{
private:
    // The granularity of dirty state in the table is one page. Dirtiness is tracked per byte of the table so that
    // synchronization is not required when changing the dirty state. Shifting-right an address by the following value yields
    // the byte index of the address into the write watch table. For instance,
    // GetTable()[address >> AddressToTableByteIndexShift] is the byte that represents the region of memory for 'address'.
    static const uint8_t AddressToTableByteIndexShift = SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift;

private:
    static void VerifyCreated();
    static void VerifyMemoryRegion(void *baseAddress, size_t regionByteSize);
    static void VerifyMemoryRegion(void *baseAddress, size_t regionByteSize, void *heapStartAddress, void *heapEndAddress);

public:
    static uint8_t *GetTable();
private:
    static uint8_t *GetUntranslatedTable();
    static uint8_t *GetUntranslatedTable(uint8_t *table, void *heapStartAddress);
    static uint8_t *GetUntranslatedTableEnd();
    static uint8_t *GetUntranslatedTableEnd(uint8_t *table, void *heapEndAddress);
public:
    static void InitializeUntranslatedTable(uint8_t *untranslatedTable, void *heapStartAddress);
private:
    static void SetUntranslatedTable(uint8_t *untranslatedTable, void *heapStartAddress);
public:
    static void SetResizedUntranslatedTable(uint8_t *untranslatedTable, void *heapStartAddress, void *heapEndAddress);
    static bool IsEnabledForGCHeap();
    static void EnableForGCHeap();
    static void DisableForGCHeap();
private:
    static void *GetHeapStartAddress();
    static void *GetHeapEndAddress();

public:
    static void StaticClose();

private:
    static size_t GetTableByteIndex(void *address);
    static void *GetPageAddress(size_t tableByteIndex);
public:
    static size_t GetTableByteSize(void *heapStartAddress, void *heapEndAddress);
    static size_t GetTableStartByteOffset(size_t byteSizeBeforeTable);
private:
    static uint8_t *TranslateTableToExcludeHeapStartAddress(uint8_t *table, void *heapStartAddress);
    static void TranslateToTableRegion(void *baseAddress, size_t regionByteSize, uint8_t **tableBaseAddressRef, size_t *tableRegionByteSizeRef);

public:
    static void ClearDirty(void *baseAddress, size_t regionByteSize);
    static void SetDirty(void *address, size_t writeByteSize);
    static void SetDirtyRegion(void *baseAddress, size_t regionByteSize);
private:
    static bool GetDirtyFromBlock(uint8_t *block, uint8_t *firstPageAddressInBlock, size_t startByteIndex, size_t endByteIndex, void **dirtyPages, size_t *dirtyPageIndexRef, size_t dirtyPageCount, bool clearDirty);
public:
    static void GetDirty(void *baseAddress, size_t regionByteSize, void **dirtyPages, size_t *dirtyPageCountRef, bool clearDirty, bool isRuntimeSuspended);
};

inline void SoftwareWriteWatch::VerifyCreated()
{
    assert(GetTable() != nullptr);
    assert(GetHeapStartAddress() != nullptr);
    assert(GetHeapEndAddress() != nullptr);
    assert(GetHeapStartAddress() < GetHeapEndAddress());
}

inline void SoftwareWriteWatch::VerifyMemoryRegion(void *baseAddress, size_t regionByteSize)
{
    VerifyMemoryRegion(baseAddress, regionByteSize, GetHeapStartAddress(), GetHeapEndAddress());
}

inline void SoftwareWriteWatch::VerifyMemoryRegion(
    void *baseAddress,
    size_t regionByteSize,
    void *heapStartAddress,
    void *heapEndAddress)
{
    VerifyCreated();
    assert(baseAddress != nullptr);
    assert(heapStartAddress != nullptr);
    assert(heapStartAddress >= GetHeapStartAddress());
    assert(heapEndAddress != nullptr);
    assert(heapEndAddress <= GetHeapEndAddress());
    assert(baseAddress >= heapStartAddress);
    assert(baseAddress < heapEndAddress);
    assert(regionByteSize != 0);
    assert(regionByteSize <= reinterpret_cast<size_t>(heapEndAddress) - reinterpret_cast<size_t>(baseAddress));
}

inline uint8_t *SoftwareWriteWatch::GetTable()
{
    return g_gc_sw_ww_table;
}

inline uint8_t *SoftwareWriteWatch::GetUntranslatedTable()
{
    VerifyCreated();
    return GetUntranslatedTable(GetTable(), GetHeapStartAddress());
}

inline uint8_t *SoftwareWriteWatch::GetUntranslatedTable(uint8_t *table, void *heapStartAddress)
{
    assert(table != nullptr);
    assert(heapStartAddress != nullptr);
    assert(heapStartAddress >= GetHeapStartAddress());

    uint8_t *untranslatedTable = table + GetTableByteIndex(heapStartAddress);
    assert(ALIGN_DOWN(untranslatedTable, sizeof(size_t)) == untranslatedTable);
    return untranslatedTable;
}

inline uint8_t *SoftwareWriteWatch::GetUntranslatedTableEnd()
{
    VerifyCreated();
    return GetUntranslatedTableEnd(GetTable(), GetHeapEndAddress());
}

inline uint8_t *SoftwareWriteWatch::GetUntranslatedTableEnd(uint8_t *table, void *heapEndAddress)
{
    assert(table != nullptr);
    assert(heapEndAddress != nullptr);
    assert(heapEndAddress <= GetHeapEndAddress());

    return ALIGN_UP(&table[GetTableByteIndex(reinterpret_cast<uint8_t *>(heapEndAddress) - 1) + 1], sizeof(size_t));
}

inline void SoftwareWriteWatch::InitializeUntranslatedTable(uint8_t *untranslatedTable, void *heapStartAddress)
{
    assert(GetTable() == nullptr);
    SetUntranslatedTable(untranslatedTable, heapStartAddress);
}

inline void SoftwareWriteWatch::SetUntranslatedTable(uint8_t *untranslatedTable, void *heapStartAddress)
{
    assert(untranslatedTable != nullptr);
    assert(ALIGN_DOWN(untranslatedTable, sizeof(size_t)) == untranslatedTable);
    assert(heapStartAddress != nullptr);

    g_gc_sw_ww_table = TranslateTableToExcludeHeapStartAddress(untranslatedTable, heapStartAddress);
}

inline void SoftwareWriteWatch::SetResizedUntranslatedTable(
    uint8_t *untranslatedTable,
    void *heapStartAddress,
    void *heapEndAddress)
{
    // The runtime needs to be suspended during this call, and background GC threads need to synchronize calls to ClearDirty()
    // and GetDirty() such that they are not called concurrently with this function

    VerifyCreated();
    assert(untranslatedTable != nullptr);
    assert(ALIGN_DOWN(untranslatedTable, sizeof(size_t)) == untranslatedTable);
    assert(heapStartAddress != nullptr);
    assert(heapEndAddress != nullptr);
    assert(heapStartAddress <= GetHeapStartAddress());
    assert(heapEndAddress >= GetHeapEndAddress());
    assert(heapStartAddress < GetHeapStartAddress() || heapEndAddress > GetHeapEndAddress());

    uint8_t *oldUntranslatedTable = GetUntranslatedTable();
    void *oldTableHeapStartAddress = GetHeapStartAddress();
    size_t oldTableByteSize = GetTableByteSize(oldTableHeapStartAddress, GetHeapEndAddress());
    SetUntranslatedTable(untranslatedTable, heapStartAddress);

    uint8_t *tableRegionStart = &GetTable()[GetTableByteIndex(oldTableHeapStartAddress)];
    memcpy(tableRegionStart, oldUntranslatedTable, oldTableByteSize);
}

inline bool SoftwareWriteWatch::IsEnabledForGCHeap()
{
    return g_gc_sw_ww_enabled_for_gc_heap;
}

inline void SoftwareWriteWatch::EnableForGCHeap()
{
    // The runtime needs to be suspended during this call. This is how it currently guarantees that GC heap writes from other
    // threads between calls to EnableForGCHeap() and DisableForGCHeap() will be tracked.

    VerifyCreated();
    assert(!IsEnabledForGCHeap());
    g_gc_sw_ww_enabled_for_gc_heap = true;

    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::SwitchToWriteWatch;
    args.write_watch_table = g_gc_sw_ww_table;
    args.is_runtime_suspended = true;
    GCToEEInterface::StompWriteBarrier(&args);
}

inline void SoftwareWriteWatch::DisableForGCHeap()
{
    // The runtime needs to be suspended during this call. This is how it currently guarantees that GC heap writes from other
    // threads between calls to EnableForGCHeap() and DisableForGCHeap() will be tracked.

    VerifyCreated();
    assert(IsEnabledForGCHeap());
    g_gc_sw_ww_enabled_for_gc_heap = false;     

    WriteBarrierParameters args = {};
    args.operation = WriteBarrierOp::SwitchToNonWriteWatch;
    args.is_runtime_suspended = true;
    GCToEEInterface::StompWriteBarrier(&args);
}

inline void *SoftwareWriteWatch::GetHeapStartAddress()
{
    return g_gc_lowest_address;
}

inline void *SoftwareWriteWatch::GetHeapEndAddress()
{
    return g_gc_highest_address;
}

inline size_t SoftwareWriteWatch::GetTableByteIndex(void *address)
{
    assert(address != nullptr);

    size_t tableByteIndex = reinterpret_cast<size_t>(address) >> AddressToTableByteIndexShift;
    assert(tableByteIndex != 0);
    return tableByteIndex;
}

inline void *SoftwareWriteWatch::GetPageAddress(size_t tableByteIndex)
{
    assert(tableByteIndex != 0);

    void *pageAddress = reinterpret_cast<void *>(tableByteIndex << AddressToTableByteIndexShift);
    assert(pageAddress >= GetHeapStartAddress());
    assert(pageAddress < GetHeapEndAddress());
    assert(ALIGN_DOWN(pageAddress, WRITE_WATCH_UNIT_SIZE) == pageAddress);
    return pageAddress;
}

inline size_t SoftwareWriteWatch::GetTableByteSize(void *heapStartAddress, void *heapEndAddress)
{
    assert(heapStartAddress != nullptr);
    assert(heapEndAddress != nullptr);
    assert(heapStartAddress < heapEndAddress);

    size_t tableByteSize =
        GetTableByteIndex(reinterpret_cast<uint8_t *>(heapEndAddress) - 1) - GetTableByteIndex(heapStartAddress) + 1;
    tableByteSize = ALIGN_UP(tableByteSize, sizeof(size_t));
    return tableByteSize;
}

inline size_t SoftwareWriteWatch::GetTableStartByteOffset(size_t byteSizeBeforeTable)
{
    return ALIGN_UP(byteSizeBeforeTable, sizeof(size_t)); // start of the table needs to be aligned to size_t
}

inline uint8_t *SoftwareWriteWatch::TranslateTableToExcludeHeapStartAddress(uint8_t *table, void *heapStartAddress)
{
    assert(table != nullptr);
    assert(heapStartAddress != nullptr);

    // Exclude the table byte index corresponding to the heap start address from the table pointer, so that each lookup in the
    // table by address does not have to calculate (address - heapStartAddress)
    return table - GetTableByteIndex(heapStartAddress);
}

inline void SoftwareWriteWatch::TranslateToTableRegion(
    void *baseAddress,
    size_t regionByteSize,
    uint8_t **tableBaseAddressRef,
    size_t *tableRegionByteSizeRef)
{
    VerifyCreated();
    VerifyMemoryRegion(baseAddress, regionByteSize);
    assert(tableBaseAddressRef != nullptr);
    assert(tableRegionByteSizeRef != nullptr);

    size_t baseAddressTableByteIndex = GetTableByteIndex(baseAddress);
    *tableBaseAddressRef = &GetTable()[baseAddressTableByteIndex];
    *tableRegionByteSizeRef =
        GetTableByteIndex(reinterpret_cast<uint8_t *>(baseAddress) + (regionByteSize - 1)) - baseAddressTableByteIndex + 1;
}

inline void SoftwareWriteWatch::ClearDirty(void *baseAddress, size_t regionByteSize)
{
    VerifyCreated();
    VerifyMemoryRegion(baseAddress, regionByteSize);

    uint8_t *tableBaseAddress;
    size_t tableRegionByteSize;
    TranslateToTableRegion(baseAddress, regionByteSize, &tableBaseAddress, &tableRegionByteSize);
    memset(tableBaseAddress, 0, tableRegionByteSize);
}

inline void SoftwareWriteWatch::SetDirty(void *address, size_t writeByteSize)
{
    VerifyCreated();
    VerifyMemoryRegion(address, writeByteSize);
    assert(address != nullptr);
    assert(writeByteSize <= sizeof(void *));

    size_t tableByteIndex = GetTableByteIndex(address);
    assert(GetTableByteIndex(reinterpret_cast<uint8_t *>(address) + (writeByteSize - 1)) == tableByteIndex);

    uint8_t *tableByteAddress = &GetTable()[tableByteIndex];
    if (*tableByteAddress == 0)
    {
        *tableByteAddress = 0xff;
    }
}

inline void SoftwareWriteWatch::SetDirtyRegion(void *baseAddress, size_t regionByteSize)
{
    VerifyCreated();
    VerifyMemoryRegion(baseAddress, regionByteSize);

    uint8_t *tableBaseAddress;
    size_t tableRegionByteSize;
    TranslateToTableRegion(baseAddress, regionByteSize, &tableBaseAddress, &tableRegionByteSize);
    memset(tableBaseAddress, ~0, tableRegionByteSize);
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#endif // !__SOFTWARE_WRITE_WATCH_H__

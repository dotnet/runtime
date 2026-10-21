// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_WRITE_BARRIER_STANDARD_ABI_TEST

#include "gcinternal.h"
#include "side_metadata.h"
#include "standardwritebarriertest.h"
#ifdef FEATURE_LXR_RC_VERTICAL
#include "lxrrcverticaltest.h"
#endif

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

extern "C" uint32_t WriteBarrierTestGetClobberMask();
extern "C" void WriteBarrierTestClobberSse();
extern "C" void WriteBarrierTestClobberAvx();
extern "C" void WriteBarrierTestClobberAvx512();
extern "C" void WriteBarrierTestClobberApx();

namespace
{
constexpr uint32_t VectorClobberMask = 7;
constexpr uint32_t ApxClobberMask = 8;

uint8_t* g_write_barrier_test_metadata;
uint8_t* g_write_barrier_test_metadata_storage_start;
size_t g_write_barrier_test_metadata_size;
size_t g_write_barrier_test_metadata_storage_size;
uintptr_t g_write_barrier_test_first_metadata_byte;
uint8_t g_write_barrier_test_granularity_shift;
uintptr_t g_write_barrier_test_data_start;
size_t g_write_barrier_test_data_size;
LxrSideMetadataLayout* g_write_barrier_test_layout;
SideMetadataManager* g_write_barrier_test_manager;
volatile int64_t g_write_barrier_test_call_count;
volatile int64_t g_write_barrier_test_attempt_count;
volatile int64_t g_write_barrier_test_win_count;
volatile int64_t g_write_barrier_test_argument_error_count;
volatile int64_t g_write_barrier_test_range_call_count;
volatile int64_t g_write_barrier_test_clear_range_call_count;
volatile int64_t g_write_barrier_test_dependent_edge_call_count;
uint32_t g_write_barrier_test_clobber_mask;
void (*g_write_barrier_test_clobber)();
WriteBarrierSlowPath g_write_barrier_test_selected_slow_path;
bool g_write_barrier_test_uncounted;
bool g_write_barrier_test_clobber_enabled;
Object** g_write_barrier_test_last_destination;
Object* g_write_barrier_test_last_old_reference;
Object* g_write_barrier_test_last_new_reference;
bool g_write_barrier_test_claim_bits;
uintptr_t g_write_barrier_test_first_counted_metadata_byte;
uintptr_t g_write_barrier_test_last_counted_metadata_byte;

bool WriteBarrierTestIsCounted(Object** destination)
{
    if (!g_write_barrier_test_claim_bits)
    {
        return true;
    }

    uintptr_t metadataByte =
        reinterpret_cast<uintptr_t>(destination) >> (g_write_barrier_test_granularity_shift + 3);
    return (metadataByte >= g_write_barrier_test_first_counted_metadata_byte) &&
           (metadataByte <= g_write_barrier_test_last_counted_metadata_byte);
}

bool WriteBarrierTestTryClaim(Object** destination)
{
    if (!g_write_barrier_test_claim_bits)
    {
        Interlocked::ExchangeAdd64(&g_write_barrier_test_attempt_count, static_cast<int64_t>(1));
        Interlocked::ExchangeAdd64(&g_write_barrier_test_win_count, static_cast<int64_t>(1));
        return true;
    }

    uintptr_t address = reinterpret_cast<uintptr_t>(destination);
    uintptr_t metadataByte = address >> (g_write_barrier_test_granularity_shift + 3);
    if ((metadataByte < g_write_barrier_test_first_metadata_byte) ||
        ((metadataByte - g_write_barrier_test_first_metadata_byte) >= g_write_barrier_test_metadata_size))
    {
        return false;
    }

    bool countClaim = WriteBarrierTestIsCounted(destination);
    if (countClaim)
    {
        Interlocked::ExchangeAdd64(&g_write_barrier_test_attempt_count, static_cast<int64_t>(1));
    }

    bool workWhenSet = GCConfig::GetWriteBarrierTestBitMeaning() != 0;
    uintptr_t observed;
    bool exchanged;
    SideMetadataResult result = g_write_barrier_test_manager->CompareExchange(
        LxrSideMetadataKind::FieldUnlogged,
        address,
        workWhenSet ? 0 : 1,
        workWhenSet ? 1 : 0,
        SideMetadataMemoryOrder::SequentiallyConsistent,
        &observed,
        &exchanged);
    if (result != SideMetadataResult::Success)
    {
        return false;
    }
    if (!exchanged)
    {
        return false;
    }
    if (countClaim)
    {
        Interlocked::ExchangeAdd64(&g_write_barrier_test_win_count, static_cast<int64_t>(1));
    }
    return true;
}

void WriteBarrierTestMarkCard(Object** destination)
{
    uint8_t* address = reinterpret_cast<uint8_t*>(destination);
    if ((address < g_gc_lowest_address) || (address >= g_gc_highest_address))
    {
        return;
    }

    constexpr size_t CardByteShift = 11;
    uint8_t* cardByte =
        reinterpret_cast<uint8_t*>(g_gc_card_table) +
        (reinterpret_cast<uintptr_t>(destination) >> CardByteShift);
    *cardByte = 0xFF;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    constexpr size_t CardBundleByteShift = 21;
    uint8_t* cardBundleByte =
        reinterpret_cast<uint8_t*>(g_gc_card_bundle_table) +
        (reinterpret_cast<uintptr_t>(destination) >> CardBundleByteShift);
    *cardBundleByte = 0xFF;
#endif
}

NOINLINE void WriteBarrierTestUncounted(
    Object** destination,
    Object* oldReference,
    Object* newReference)
{
    UNREFERENCED_PARAMETER(oldReference);
    UNREFERENCED_PARAMETER(newReference);
    WriteBarrierTestMarkCard(destination);
    WriteBarrierTestTryClaim(destination);
}

NOINLINE void WriteBarrierTestNoOp(
    Object** destination,
    Object* oldReference,
    Object* newReference)
{
    WriteBarrierTestMarkCard(destination);
    if (!WriteBarrierTestTryClaim(destination))
    {
        return;
    }
#ifdef FEATURE_LXR_RC_VERTICAL
    LxrRcVerticalObserveWrite(destination, oldReference);
#endif

    if (WriteBarrierTestIsCounted(destination) && (*destination != oldReference))
    {
        Interlocked::ExchangeAdd64(&g_write_barrier_test_argument_error_count, static_cast<int64_t>(1));
    }

    g_write_barrier_test_last_destination = destination;
    g_write_barrier_test_last_old_reference = oldReference;
    g_write_barrier_test_last_new_reference = newReference;
    Interlocked::ExchangeAdd64(&g_write_barrier_test_call_count, static_cast<int64_t>(1));
}

NOINLINE void WriteBarrierTestClobber(
    Object** destination,
    Object* oldReference,
    Object* newReference)
{
    WriteBarrierTestMarkCard(destination);
    if (!WriteBarrierTestTryClaim(destination))
    {
        return;
    }
#ifdef FEATURE_LXR_RC_VERTICAL
    LxrRcVerticalObserveWrite(destination, oldReference);
#endif

    if (WriteBarrierTestIsCounted(destination) && (*destination != oldReference))
    {
        Interlocked::ExchangeAdd64(&g_write_barrier_test_argument_error_count, static_cast<int64_t>(1));
    }

    g_write_barrier_test_last_destination = destination;
    g_write_barrier_test_last_old_reference = oldReference;
    g_write_barrier_test_last_new_reference = newReference;
    Interlocked::ExchangeAdd64(&g_write_barrier_test_call_count, static_cast<int64_t>(1));
    g_write_barrier_test_clobber();
    if ((g_write_barrier_test_clobber_mask & ApxClobberMask) != 0)
    {
        WriteBarrierTestClobberApx();
    }
}

NOINLINE void WriteBarrierTestRange(
    Object** destination,
    Object** source,
    size_t referenceCount)
{
    Interlocked::ExchangeAdd64(&g_write_barrier_test_range_call_count, static_cast<int64_t>(1));
    if (source == nullptr)
    {
        Interlocked::ExchangeAdd64(&g_write_barrier_test_clear_range_call_count, static_cast<int64_t>(1));
    }

    for (size_t index = 0; index < referenceCount; index++)
    {
        g_write_barrier_test_selected_slow_path(
            destination + index,
            destination[index],
            (source == nullptr) ? nullptr : source[index]);
    }
}

NOINLINE void WriteBarrierTestDependentEdge(
    void* destination,
    Object* oldReference,
    Object* newReference)
{
    Interlocked::ExchangeAdd64(&g_write_barrier_test_dependent_edge_call_count, static_cast<int64_t>(1));

    Object** slot = reinterpret_cast<Object**>(destination);
    WriteBarrierTestMarkCard(slot);
    if (!WriteBarrierTestTryClaim(slot) || g_write_barrier_test_uncounted)
    {
        return;
    }

    g_write_barrier_test_last_destination = slot;
    g_write_barrier_test_last_old_reference = oldReference;
    g_write_barrier_test_last_new_reference = newReference;
    Interlocked::ExchangeAdd64(&g_write_barrier_test_call_count, static_cast<int64_t>(1));
    if (g_write_barrier_test_clobber_enabled)
    {
        g_write_barrier_test_clobber();
        if ((g_write_barrier_test_clobber_mask & ApxClobberMask) != 0)
        {
            WriteBarrierTestClobberApx();
        }
    }
}
}

uint8_t* GetWriteBarrierTestMetadataBase(
    uint8_t* lowestAddress,
    uint8_t* highestAddress,
    uint8_t granularityShift)
{
    _ASSERTE(lowestAddress < highestAddress);
    _ASSERTE(g_write_barrier_test_metadata == nullptr);

    g_write_barrier_test_granularity_shift = granularityShift;
    g_write_barrier_test_data_start = reinterpret_cast<uintptr_t>(lowestAddress);
    g_write_barrier_test_data_size = static_cast<size_t>(highestAddress - lowestAddress);
    g_write_barrier_test_first_metadata_byte =
        reinterpret_cast<uintptr_t>(lowestAddress) >> (granularityShift + 3);
    uintptr_t lastMetadataByte =
        (reinterpret_cast<uintptr_t>(highestAddress) - 1) >> (granularityShift + 3);
    g_write_barrier_test_metadata_size =
        static_cast<size_t>(lastMetadataByte - g_write_barrier_test_first_metadata_byte + 1);
    g_write_barrier_test_layout = new (nothrow) LxrSideMetadataLayout;
    g_write_barrier_test_manager = new (nothrow) SideMetadataManager;
    if ((g_write_barrier_test_layout == nullptr) || (g_write_barrier_test_manager == nullptr))
    {
        delete g_write_barrier_test_manager;
        delete g_write_barrier_test_layout;
        g_write_barrier_test_manager = nullptr;
        g_write_barrier_test_layout = nullptr;
        return nullptr;
    }

    if ((LxrSideMetadataLayout::Create(1, g_write_barrier_test_layout) != SideMetadataResult::Success) ||
        (g_write_barrier_test_manager->Initialize(
             g_write_barrier_test_layout,
             UINT64_C(1) << static_cast<uint8_t>(LxrSideMetadataKind::FieldUnlogged)) !=
         SideMetadataResult::Success) ||
        (g_write_barrier_test_manager->CommitDataRange(
             g_write_barrier_test_data_start,
             g_write_barrier_test_data_size) != SideMetadataResult::Success))
    {
        delete g_write_barrier_test_manager;
        delete g_write_barrier_test_layout;
        g_write_barrier_test_manager = nullptr;
        g_write_barrier_test_layout = nullptr;
        return nullptr;
    }

    const SideMetadataSpec* fieldSpec =
        g_write_barrier_test_layout->GetSpec(LxrSideMetadataKind::FieldUnlogged);
    uintptr_t firstAddress = fieldSpec->base_address + g_write_barrier_test_first_metadata_byte;
    uintptr_t lastAddress = fieldSpec->base_address + lastMetadataByte + 1;
    uintptr_t storageStart = firstAddress & ~(static_cast<uintptr_t>(sizeof(uintptr_t)) - 1);
    uintptr_t storageEnd =
        (lastAddress + sizeof(uintptr_t) - 1) & ~(static_cast<uintptr_t>(sizeof(uintptr_t)) - 1);
    g_write_barrier_test_metadata = reinterpret_cast<uint8_t*>(firstAddress);
    g_write_barrier_test_metadata_storage_start = reinterpret_cast<uint8_t*>(storageStart);
    g_write_barrier_test_metadata_storage_size = storageEnd - storageStart;
    memset(
        g_write_barrier_test_metadata_storage_start,
        GCConfig::GetWriteBarrierTestBitMeaning() == 0 ? 0 : 0xFF,
        g_write_barrier_test_metadata_storage_size);
    g_write_barrier_test_claim_bits = GCConfig::GetWriteBarrierTestClaimBits();
    return reinterpret_cast<uint8_t*>(fieldSpec->base_address);
}

uint8_t* GetWriteBarrierTestMetadataStart()
{
    return g_write_barrier_test_metadata_storage_start;
}

size_t GetWriteBarrierTestMetadataSize()
{
    return g_write_barrier_test_metadata_storage_size;
}

WriteBarrierSlowPath GetWriteBarrierTestSlowPath()
{
    if (GCConfig::GetWriteBarrierTestUncounted())
    {
        g_write_barrier_test_uncounted = true;
        g_write_barrier_test_clobber_enabled = false;
        g_write_barrier_test_selected_slow_path = WriteBarrierTestUncounted;
        return g_write_barrier_test_selected_slow_path;
    }

    g_write_barrier_test_uncounted = false;
    if (!GCConfig::GetWriteBarrierTestClobber())
    {
        g_write_barrier_test_clobber_enabled = false;
        g_write_barrier_test_selected_slow_path = WriteBarrierTestNoOp;
        return g_write_barrier_test_selected_slow_path;
    }

    g_write_barrier_test_clobber_enabled = true;
    g_write_barrier_test_clobber_mask = WriteBarrierTestGetClobberMask();
    switch (g_write_barrier_test_clobber_mask & VectorClobberMask)
    {
        case 4:
            g_write_barrier_test_clobber = WriteBarrierTestClobberAvx512;
            break;

        case 2:
            g_write_barrier_test_clobber = WriteBarrierTestClobberAvx;
            break;

        default:
            g_write_barrier_test_clobber = WriteBarrierTestClobberSse;
            break;
    }

    g_write_barrier_test_selected_slow_path = WriteBarrierTestClobber;
    return g_write_barrier_test_selected_slow_path;
}

void ResetWriteBarrierTestMetadataForGc()
{
    if (g_write_barrier_test_claim_bits && (g_write_barrier_test_metadata != nullptr))
    {
        SideMetadataResult result = g_write_barrier_test_manager->ResetRangeQuiescent(
            LxrSideMetadataKind::FieldUnlogged,
            g_write_barrier_test_data_start,
            g_write_barrier_test_data_size,
            GCConfig::GetWriteBarrierTestBitMeaning() == 0 ? 0 : 1);
        _ASSERTE(result == SideMetadataResult::Success);
    }
}

WriteBarrierRangeSlowPath GetWriteBarrierTestRangeSlowPath()
{
    return WriteBarrierTestRange;
}

WriteBarrierDependentEdgeSlowPath GetWriteBarrierTestDependentEdgeSlowPath()
{
    return WriteBarrierTestDependentEdge;
}

WriteBarrierEpochReset GetWriteBarrierTestEpochReset()
{
    return ResetWriteBarrierTestMetadataForGc;
}

#ifdef FEATURE_LXR_RC_VERTICAL
bool LxrRcVerticalUnlogObservedSlot(Object** destination)
{
    return g_write_barrier_test_manager != nullptr &&
        GCConfig::GetWriteBarrierTestBitMeaning() == 1 &&
        g_write_barrier_test_manager->Store(
            LxrSideMetadataKind::FieldUnlogged,
            reinterpret_cast<uintptr_t>(destination), 1,
            SideMetadataMemoryOrder::Relaxed) == SideMetadataResult::Success;
}
#endif

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetCallCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_call_count);
}

extern "C" DLLEXPORT uint32_t GC_WriteBarrierTest_GetClobberMask()
{
    return g_write_barrier_test_clobber_mask;
}

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetRangeCallCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_range_call_count);
}

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetClearRangeCallCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_clear_range_call_count);
}

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetDependentEdgeCallCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_dependent_edge_call_count);
}

extern "C" DLLEXPORT void GC_WriteBarrierTest_ResetRange(
    uintptr_t destination,
    size_t referenceCount,
    uint8_t metadataValue,
    bool claimBits)
{
    _ASSERTE(referenceCount != 0);
    _ASSERTE(g_write_barrier_test_manager != nullptr);
    if (g_write_barrier_test_manager == nullptr)
    {
        return;
    }

    uintptr_t firstDestinationMetadataByte =
        destination >> (g_write_barrier_test_granularity_shift + 3);
    uintptr_t lastDestination =
        destination + ((referenceCount - 1) * (static_cast<uintptr_t>(1) << g_write_barrier_test_granularity_shift));
    uintptr_t lastDestinationMetadataByte =
        lastDestination >> (g_write_barrier_test_granularity_shift + 3);
    _ASSERTE(firstDestinationMetadataByte >= g_write_barrier_test_first_metadata_byte);
    _ASSERTE(
        (lastDestinationMetadataByte - g_write_barrier_test_first_metadata_byte) <
        g_write_barrier_test_metadata_size);

    memset(
        &g_write_barrier_test_metadata[
            firstDestinationMetadataByte - g_write_barrier_test_first_metadata_byte],
        metadataValue,
        static_cast<size_t>(lastDestinationMetadataByte - firstDestinationMetadataByte + 1));
    g_write_barrier_test_claim_bits = claimBits;
    g_write_barrier_test_first_counted_metadata_byte = firstDestinationMetadataByte;
    g_write_barrier_test_last_counted_metadata_byte = lastDestinationMetadataByte;
    g_write_barrier_test_call_count = 0;
    g_write_barrier_test_attempt_count = 0;
    g_write_barrier_test_win_count = 0;
    g_write_barrier_test_argument_error_count = 0;
    g_write_barrier_test_range_call_count = 0;
    g_write_barrier_test_clear_range_call_count = 0;
    g_write_barrier_test_dependent_edge_call_count = 0;
    g_write_barrier_test_last_destination = nullptr;
    g_write_barrier_test_last_old_reference = nullptr;
    g_write_barrier_test_last_new_reference = nullptr;
}

extern "C" DLLEXPORT void GC_WriteBarrierTest_Reset(
    uintptr_t destination,
    uint8_t metadataValue,
    bool claimBits)
{
    GC_WriteBarrierTest_ResetRange(destination, 1, metadataValue, claimBits);
}

extern "C" DLLEXPORT void GC_WriteBarrierTest_SetRequiresWork(
    uintptr_t destination,
    bool requiresWork)
{
    uintptr_t metadataByte =
        destination >> (g_write_barrier_test_granularity_shift + 3);
    _ASSERTE(metadataByte >= g_write_barrier_test_first_metadata_byte);
    size_t metadataOffset =
        static_cast<size_t>(metadataByte - g_write_barrier_test_first_metadata_byte);
    _ASSERTE(metadataOffset < g_write_barrier_test_metadata_size);

    bool workWhenSet = GCConfig::GetWriteBarrierTestBitMeaning() != 0;
    _ASSERTE(g_write_barrier_test_manager != nullptr);
    if (g_write_barrier_test_manager == nullptr)
    {
        return;
    }
    SideMetadataResult result = g_write_barrier_test_manager->Store(
        LxrSideMetadataKind::FieldUnlogged,
        destination,
        requiresWork == workWhenSet ? 1 : 0,
        SideMetadataMemoryOrder::SequentiallyConsistent);
    _ASSERTE(result == SideMetadataResult::Success);
}

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetAttemptCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_attempt_count);
}

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetWinCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_win_count);
}

extern "C" DLLEXPORT uint64_t GC_WriteBarrierTest_GetArgumentErrorCount()
{
    return static_cast<uint64_t>(g_write_barrier_test_argument_error_count);
}

extern "C" DLLEXPORT uintptr_t GC_WriteBarrierTest_GetLastDestination()
{
    return reinterpret_cast<uintptr_t>(g_write_barrier_test_last_destination);
}

extern "C" DLLEXPORT uintptr_t GC_WriteBarrierTest_GetSyntheticDestination(
    uint32_t metadataByte,
    uint32_t bit)
{
    return ((g_write_barrier_test_first_metadata_byte + metadataByte)
            << (g_write_barrier_test_granularity_shift + 3)) |
           (static_cast<uintptr_t>(bit & 7) << g_write_barrier_test_granularity_shift);
}

extern "C" DLLEXPORT bool GC_WriteBarrierTest_ClaimSynthetic(uintptr_t destination)
{
    return WriteBarrierTestTryClaim(reinterpret_cast<Object**>(destination));
}

#endif

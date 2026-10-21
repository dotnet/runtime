// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"

#include <stdio.h>
#include <string.h>
#include <type_traits>

static_assert(static_cast<uint32_t>(WriteBarrierShape::CardTable) == 0);
static_assert(static_cast<uint32_t>(WriteBarrierShape::SideMetadataFieldLog) == 1);
static_assert(static_cast<uint32_t>(WriteBarrierRequestStatus::NotProcessed) == 0);
static_assert(static_cast<uint32_t>(WriteBarrierRequestStatus::Accepted) == 1);
static_assert(static_cast<uint32_t>(WriteBarrierRequestStatus::Unsupported) == 2);
static_assert(static_cast<uint8_t>(WriteBarrierMetadataBitMeaning::WorkWhenBitIsClear) == 0);
static_assert(static_cast<uint8_t>(WriteBarrierMetadataBitMeaning::WorkWhenBitIsSet) == 1);
static_assert(std::is_standard_layout_v<WriteBarrierParameters>);
static_assert(std::is_standard_layout_v<WriteBarrierSideMetadataParameters>);
static_assert(GC_WRITE_BARRIER_SHAPE_INTERFACE_MINOR_VERSION == 9);
static_assert(GC_WRITE_BARRIER_COMPLETE_STORE_INTERFACE_MINOR_VERSION == 10);
static_assert(GC_WRITE_BARRIER_EPOCH_RESET_INTERFACE_MINOR_VERSION == 11);
static_assert(GC_INTERFACE_MINOR_VERSION >= GC_WRITE_BARRIER_EPOCH_RESET_INTERFACE_MINOR_VERSION);

struct LegacyWriteBarrierParameters
{
    WriteBarrierOp operation;
    bool is_runtime_suspended;
    bool requires_upper_bounds_check;
    uint32_t* card_table;
    uint32_t* card_bundle_table;
    uint8_t* lowest_address;
    uint8_t* highest_address;
    uint8_t* ephemeral_low;
    uint8_t* ephemeral_high;
    uint8_t* write_watch_table;
    uint8_t* region_to_generation_table;
    uint8_t region_shr;
    bool region_use_bitwise_write_barrier;
};

static_assert(sizeof(LegacyWriteBarrierParameters) == (sizeof(void*) == 8 ? 80 : 44));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_shape) ==
    sizeof(LegacyWriteBarrierParameters));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_request_status) ==
    (sizeof(void*) == 8 ? 84 : 48));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_side_metadata) ==
    (sizeof(void*) == 8 ? 88 : 52));
static_assert(sizeof(WriteBarrierSideMetadataParameters) == (sizeof(void*) == 8 ? 24 : 12));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_range_slow_path) ==
    (sizeof(void*) == 8 ? 112 : 64));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_dependent_edge_slow_path) ==
    (sizeof(void*) == 8 ? 120 : 68));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_epoch_reset) ==
    (sizeof(void*) == 8 ? 128 : 72));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_epoch_reset) ==
    offsetof(WriteBarrierParameters, write_barrier_dependent_edge_slow_path) + sizeof(void*));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_bulk_scan) ==
    (sizeof(void*) == 8 ? 136 : 76));
static_assert(sizeof(WriteBarrierBulkScanParameters) == (sizeof(void*) == 8 ? 16 : 8));
static_assert(sizeof(WriteBarrierParameters) == (sizeof(void*) == 8 ? 152 : 84));

namespace
{
int checks;
int failures;

void Expect(const char* name, bool condition)
{
    checks++;
    if (!condition)
    {
        failures++;
        printf("FAIL: %s\n", name);
    }
}

void SlowPath(Object**, Object*, Object*)
{
}
}

int main()
{
    WriteBarrierParameters defaults = {};
    Expect("zero initialization selects CardTable",
        defaults.write_barrier_shape == WriteBarrierShape::CardTable);
    Expect("zero initialization selects NotProcessed",
        defaults.write_barrier_request_status == WriteBarrierRequestStatus::NotProcessed);
    Expect("zero initialization clears metadata base",
        defaults.write_barrier_side_metadata.metadata_base == nullptr);
    Expect("zero initialization clears slow helper",
        defaults.write_barrier_side_metadata.slow_path == nullptr);
    Expect("zero initialization selects clear-bit polarity",
        defaults.write_barrier_side_metadata.bit_meaning ==
        WriteBarrierMetadataBitMeaning::WorkWhenBitIsClear);
    Expect("zero initialization clears range helper",
        defaults.write_barrier_range_slow_path == nullptr);
    Expect("zero initialization clears dependent-edge helper",
        defaults.write_barrier_dependent_edge_slow_path == nullptr);
    Expect("zero initialization clears epoch reset",
        defaults.write_barrier_epoch_reset == nullptr);
    Expect("zero initialization clears bulk metadata start",
        defaults.write_barrier_bulk_scan.metadata_start == nullptr);
    Expect("zero initialization clears bulk metadata size",
        defaults.write_barrier_bulk_scan.metadata_size == 0);

    uint8_t metadata = 0;
    defaults.write_barrier_shape = WriteBarrierShape::SideMetadataFieldLog;
    defaults.write_barrier_side_metadata.metadata_base = &metadata;
    defaults.write_barrier_side_metadata.slow_path = SlowPath;
    defaults.write_barrier_side_metadata.granularity_shift = 3;
    defaults.write_barrier_side_metadata.bit_meaning =
        WriteBarrierMetadataBitMeaning::WorkWhenBitIsSet;

    Expect("side-metadata shape remains one request",
        defaults.write_barrier_shape == WriteBarrierShape::SideMetadataFieldLog);
    Expect("metadata base round trips",
        defaults.write_barrier_side_metadata.metadata_base == &metadata);
    Expect("slow helper round trips",
        defaults.write_barrier_side_metadata.slow_path == SlowPath);
    Expect("granularity round trips",
        defaults.write_barrier_side_metadata.granularity_shift == 3);
    Expect("set-bit polarity round trips",
        defaults.write_barrier_side_metadata.bit_meaning ==
        WriteBarrierMetadataBitMeaning::WorkWhenBitIsSet);

    LegacyWriteBarrierParameters legacy = {};
    memset(&legacy, 0xA5, sizeof(legacy));
    WriteBarrierParameters current = {};
    memset(&current, 0, sizeof(current));
    memcpy(&current, &legacy, sizeof(legacy));
    Expect("legacy prefix copies exactly",
        memcmp(&current, &legacy, sizeof(legacy)) == 0);
    Expect("tail begins after every legacy byte",
        reinterpret_cast<uint8_t*>(&current.write_barrier_shape) ==
        (reinterpret_cast<uint8_t*>(&current) + sizeof(legacy)));

    printf("%d/%d barrier-shape checks passed\n", checks - failures, checks);
    return failures == 0 ? 0 : 1;
}

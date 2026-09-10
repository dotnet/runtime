// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GC_WRITE_BARRIER_H_
#define _GC_WRITE_BARRIER_H_

#include <stddef.h>
#include <stdint.h>

enum class WriteBarrierOp
{
    StompResize,
    StompEphemeral,
    Initialize,
    SwitchToWriteWatch,
    SwitchToNonWriteWatch
};

struct WriteBarrierParameters
{
    // The update to perform.
    WriteBarrierOp operation;

    // Whether managed threads are already suspended.
    bool is_runtime_suspended;

    // Whether references above the ephemeral range can be in the GC heap.
    bool requires_upper_bounds_check;

    // The new card table location. May or may not be the same as the previous
    // card table. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint32_t* card_table;

    // The new card bundle table location. May or may not be the same as the previous
    // card bundle table. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint32_t* card_bundle_table;

    // The heap's new low boundary. May or may not be the same as the previous
    // value. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint8_t* lowest_address;

    // The heap's new high boundary. May or may not be the same as the previous
    // value. Used for WriteBarrierOp::Initialize and WriteBarrierOp::StompResize.
    uint8_t* highest_address;

    // The new start of the ephemeral generation.
    // Used for WriteBarrierOp::StompEphemeral.
    uint8_t* ephemeral_low;

    // The new end of the ephemeral generation.
    // Used for WriteBarrierOp::StompEphemeral.
    uint8_t* ephemeral_high;

    // The new write watch table, if we are using our own write watch
    // implementation. Used for WriteBarrierOp::SwitchToWriteWatch only.
    uint8_t* write_watch_table;

    // Mapping table from region index to generation.
    uint8_t* region_to_generation_table;

    // Shift count - how many bits to shift right to obtain region index from address.
    uint8_t region_shr;

    // Whether to use the more precise but slower write barrier.
    bool region_use_bitwise_write_barrier;
};

// Describes the source write barrier code and its optional executable copy.
struct WriteBarrierCodeDescriptor
{
    uint8_t* source_start;
    uint8_t* executable_start;
    size_t size;
};

struct WriteBarrierPatchLocations
{
    uint8_t* write_watch_table;
    uint8_t* region_to_generation;
    uint8_t* region_shr_dest;
    uint8_t* region_shr_src;
    uint8_t* lower_bound;
    uint8_t* upper_bound;
    uint8_t* card_table;
    uint8_t* card_bundle_table;
};

constexpr size_t WRITE_BARRIER_REGISTER_HELPER_COUNT = 6;
constexpr size_t WRITE_BARRIER_AV_LOCATION_COUNT = 14;
constexpr size_t WRITE_BARRIER_EXCEPTION_RANGE_COUNT = 4;

struct WriteBarrierExceptionRange
{
    uint8_t* start;
    uint8_t* end;
};

struct WriteBarrierHelperDescriptor
{
    void* assign_ref;
    void* checked_assign_ref;
    void* assign_ref_by_register[WRITE_BARRIER_REGISTER_HELPER_COUNT];
    void* checked_assign_ref_by_register[WRITE_BARRIER_REGISTER_HELPER_COUNT];
    WriteBarrierCodeDescriptor code;
    uintptr_t av_locations[WRITE_BARRIER_AV_LOCATION_COUNT];
    size_t av_location_count;
    WriteBarrierExceptionRange exception_ranges[WRITE_BARRIER_EXCEPTION_RANGE_COUNT];
    size_t exception_range_count;
};

using WriteBarrierFunction = void (*)(void* context, void** destination, void* reference);
using IsInGCHeapFunction = bool (*)(void* context, void* address);
using BulkMoveWithWriteBarrierFunction =
    void (*)(void* destination, const void* source, size_t length);

struct WriteBarrierFunctions
{
    void* context;
    WriteBarrierFunction write_barrier;
    WriteBarrierFunction checked_write_barrier;
    IsInGCHeapFunction is_in_gc_heap;
    BulkMoveWithWriteBarrierFunction bulk_move_with_write_barrier;
};

enum WriteBarrierType : uint8_t
{
    WRITE_BARRIER_UNINITIALIZED,
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    WRITE_BARRIER_PREGROW64,
    WRITE_BARRIER_POSTGROW64,
#ifdef FEATURE_SVR_GC
    WRITE_BARRIER_SVR64,
#endif // FEATURE_SVR_GC
    WRITE_BARRIER_BYTE_REGIONS64,
    WRITE_BARRIER_BIT_REGIONS64,
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    WRITE_BARRIER_WRITE_WATCH_PREGROW64,
    WRITE_BARRIER_WRITE_WATCH_POSTGROW64,
#ifdef FEATURE_SVR_GC
    WRITE_BARRIER_WRITE_WATCH_SVR64,
#endif // FEATURE_SVR_GC
    WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64,
    WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64,
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#endif // TARGET_AMD64 || TARGET_ARM64
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM) || \
    defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    WRITE_BARRIER_BUFFER,
#endif // TARGET_AMD64 || TARGET_ARM64 || TARGET_ARM || TARGET_LOONGARCH64 || TARGET_RISCV64
#ifdef TARGET_ARM
    WRITE_BARRIER_CHECKED_BUFFER,
#endif // TARGET_ARM
#ifdef TARGET_X86
    WRITE_BARRIER_X86_PREGROW,
    WRITE_BARRIER_X86_POSTGROW,
    WRITE_BARRIER_X86_EAX,
    WRITE_BARRIER_X86_ECX,
    WRITE_BARRIER_X86_EBX,
    WRITE_BARRIER_X86_ESI,
    WRITE_BARRIER_X86_EDI,
    WRITE_BARRIER_X86_EBP,
    WRITE_BARRIER_X86_DEBUG_EAX,
    WRITE_BARRIER_X86_DEBUG_ECX,
    WRITE_BARRIER_X86_DEBUG_EBX,
    WRITE_BARRIER_X86_DEBUG_ESI,
    WRITE_BARRIER_X86_DEBUG_EDI,
    WRITE_BARRIER_X86_DEBUG_EBP
#endif // TARGET_X86
};

enum StompWriteBarrierCompletionAction
{
    SWB_PASS = 0x0,
    SWB_ICACHE_FLUSH = 0x1,
    SWB_EE_RESTART = 0x2
};

void InitializeWriteBarrierManager();
void InitializeWriteBarrierStomp();
void StompWriteBarrier(WriteBarrierParameters* args);

int StompWriteBarrierEphemeral(const WriteBarrierParameters& args, bool isRuntimeSuspended);
int StompWriteBarrierResize(
    const WriteBarrierParameters& args,
    bool isRuntimeSuspended,
    bool requiresUpperBoundsCheck);
int SwitchToWriteWatchBarrier(const WriteBarrierParameters& args, bool isRuntimeSuspended);
int SwitchToNonWriteWatchBarrier(const WriteBarrierParameters& args, bool isRuntimeSuspended);
void FlushWriteBarrierInstructionCache();

#endif // _GC_WRITE_BARRIER_H_

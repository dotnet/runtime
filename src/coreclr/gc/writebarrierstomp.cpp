// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "env/common.h"
#include "writebarrier.h"
#include "writebarrierglobals.h"
#include "writebarrierservices.h"
#include "env/gcenv.h"
#include "env/gcenv.ee.h"
#include "env/volatile.h"

#ifdef GC_WRITE_BARRIER_STANDALONE
#include "gcenv.ee.standalone.inl"
#endif // GC_WRITE_BARRIER_STANDALONE

#include <assert.h>
#include <minipal/memorybarrierprocesswide.h>

namespace
{
WriteBarrierParameters s_state;

#if (defined(NATIVEAOT) && !defined(FEATURE_PORTABLE_HELPERS)) || defined(TARGET_WASM)
#ifdef TARGET_WASM
extern "C" void RhpAssignRef(Object** destination, Object* reference);
extern "C" void RhpCheckedAssignRef(Object** destination, Object* reference);
#endif // TARGET_WASM
#if defined(NATIVEAOT) && !defined(FEATURE_PORTABLE_HELPERS)
extern "C" uint8_t RhpAssignRefAVLocation;
extern "C" uint8_t RhpCheckedAssignRefAVLocation;
#ifdef TARGET_X86
#define X86_WRITE_BARRIER_AV_LOCATION(reg) \
    extern "C" uint8_t RhpAssignRef##reg##AVLocation; \
    extern "C" uint8_t RhpCheckedAssignRef##reg##AVLocation;
X86_WRITE_BARRIER_AV_LOCATION(EAX)
X86_WRITE_BARRIER_AV_LOCATION(ECX)
X86_WRITE_BARRIER_AV_LOCATION(EBX)
X86_WRITE_BARRIER_AV_LOCATION(ESI)
X86_WRITE_BARRIER_AV_LOCATION(EDI)
X86_WRITE_BARRIER_AV_LOCATION(EBP)
#undef X86_WRITE_BARRIER_AV_LOCATION
#endif // TARGET_X86
#endif // NATIVEAOT && !FEATURE_PORTABLE_HELPERS

void PublishStaticWriteBarrierHelpers()
{
    WriteBarrierHelperDescriptor helpers = {};
#ifdef TARGET_WASM
    helpers.assign_ref = reinterpret_cast<void*>(RhpAssignRef);
    helpers.checked_assign_ref = reinterpret_cast<void*>(RhpCheckedAssignRef);
#endif // TARGET_WASM
#if defined(NATIVEAOT) && !defined(FEATURE_PORTABLE_HELPERS)
    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpAssignRefAVLocation);
#ifdef TARGET_X86
#define X86_ADD_WRITE_BARRIER_AV_LOCATION(reg) \
    helpers.av_locations[helpers.av_location_count++] = \
        reinterpret_cast<uintptr_t>(&RhpAssignRef##reg##AVLocation);
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EAX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ECX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ESI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EDI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBP)
#undef X86_ADD_WRITE_BARRIER_AV_LOCATION
#endif // TARGET_X86

    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRefAVLocation);
#ifdef TARGET_X86
#define X86_ADD_WRITE_BARRIER_AV_LOCATION(reg) \
    helpers.av_locations[helpers.av_location_count++] = \
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRef##reg##AVLocation);
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EAX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ECX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ESI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EDI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBP)
#undef X86_ADD_WRITE_BARRIER_AV_LOCATION
#endif // TARGET_X86
#endif // NATIVEAOT && !FEATURE_PORTABLE_HELPERS

    GCToEEInterface::SetWriteBarrierHelpers(helpers);
}
#endif // (NATIVEAOT && !FEATURE_PORTABLE_HELPERS) || TARGET_WASM

void PublishCardTableState(const WriteBarrierParameters& state)
{
    VolatileStoreWithoutBarrier(&g_card_table, state.card_table);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    g_card_bundle_table = state.card_bundle_table;
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
}

void PublishHeapBounds(const WriteBarrierParameters& state)
{
    g_lowest_address = state.lowest_address;
    g_highest_address = state.highest_address;
}

void UpdateEphemeralState(const WriteBarrierParameters& state)
{
    s_state.ephemeral_low = state.ephemeral_low;
    s_state.ephemeral_high = state.ephemeral_high;
    s_state.region_to_generation_table = state.region_to_generation_table;
    s_state.region_shr = state.region_shr;
    s_state.region_use_bitwise_write_barrier =
        state.region_use_bitwise_write_barrier && GCToEEInterface::SupportsWriteBarrierBitwiseRegion();
}

void PublishEphemeralState(const WriteBarrierParameters& state)
{
    g_ephemeral_low = state.ephemeral_low;
    g_ephemeral_high = state.ephemeral_high;
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
void PublishWriteWatchState(uint8_t* writeWatchTable, bool enabled)
{
    g_write_watch_table = writeWatchTable;
    g_sw_ww_enabled_for_gc_heap = enabled;
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

void InitializeWriteBarrierStomp()
{
#if defined(FEATURE_NATIVEAOT) || defined(GC_STATIC_WRITE_BARRIER) || defined(TARGET_WASM)
    InitializeWriteBarrierManager();
#if (defined(NATIVEAOT) && !defined(FEATURE_PORTABLE_HELPERS)) || defined(TARGET_WASM)
    PublishStaticWriteBarrierHelpers();
#endif // (NATIVEAOT && !FEATURE_PORTABLE_HELPERS) || TARGET_WASM
#else
    g_writeBarrierServices.PublishWriteBarrierHelpers();
    InitializeWriteBarrierManager();
#endif // FEATURE_NATIVEAOT || GC_STATIC_WRITE_BARRIER || TARGET_WASM
    s_state = {};
}

void StompWriteBarrier(WriteBarrierParameters* args)
{
    assert(args != nullptr);

#ifdef GC_WRITE_BARRIER_STANDALONE
    GCToEEInterface::UpdateRuntimeWriteBarrierState(*args);
#endif // GC_WRITE_BARRIER_STANDALONE

    int completionActions = SWB_PASS;
    bool isRuntimeSuspended = args->is_runtime_suspended;

    switch (args->operation)
    {
    case WriteBarrierOp::StompResize:
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(args->card_bundle_table != nullptr);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

        s_state.card_table = args->card_table;
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        s_state.card_bundle_table = args->card_bundle_table;
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

        // The card table must be visible before the heap bounds are widened.
        PublishCardTableState(s_state);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        if (args->write_watch_table != nullptr)
        {
            assert(args->is_runtime_suspended);
            if (s_state.write_watch_table != nullptr)
            {
                s_state.write_watch_table = args->write_watch_table;
                PublishWriteWatchState(s_state.write_watch_table, true);
            }
        }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        completionActions |= ::StompWriteBarrierResize(
            s_state,
            isRuntimeSuspended,
            args->requires_upper_bounds_check);
        isRuntimeSuspended = (completionActions & SWB_EE_RESTART) || isRuntimeSuspended;

        if (completionActions & SWB_ICACHE_FLUSH)
        {
            ::FlushWriteBarrierInstructionCache();
        }

#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        if (!isRuntimeSuspended)
        {
            minipal_memory_barrier_process_wide();
        }
#endif // TARGET_ARM64 || TARGET_ARM || TARGET_LOONGARCH64 || TARGET_RISCV64

        s_state.lowest_address = args->lowest_address;
        s_state.highest_address = args->highest_address;
        PublishHeapBounds(s_state);

#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // These barriers embed the heap bounds and must be updated after publication.
        completionActions |= ::StompWriteBarrierResize(
            s_state,
            isRuntimeSuspended,
            args->requires_upper_bounds_check);

#ifdef TARGET_ARM
        if (completionActions & SWB_ICACHE_FLUSH)
        {
            ::FlushWriteBarrierInstructionCache();
        }
#endif // TARGET_ARM

        isRuntimeSuspended = (completionActions & SWB_EE_RESTART) || isRuntimeSuspended;
        if (!isRuntimeSuspended)
        {
            minipal_memory_barrier_process_wide();
        }
#endif // TARGET_ARM64 || TARGET_ARM || TARGET_LOONGARCH64 || TARGET_RISCV64

        if (completionActions & SWB_EE_RESTART)
        {
            assert(!args->is_runtime_suspended);
            GCToEEInterface::RestartForWriteBarrier();
        }
        return;

    case WriteBarrierOp::StompEphemeral:
        assert(args->is_runtime_suspended);
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        UpdateEphemeralState(*args);
        PublishEphemeralState(s_state);
        completionActions |=
            ::StompWriteBarrierEphemeral(s_state, args->is_runtime_suspended);
        break;

    case WriteBarrierOp::Initialize:
        assert(args->is_runtime_suspended);
        assert(s_state.card_table == nullptr);
        assert(s_state.lowest_address == nullptr);
        assert(s_state.highest_address == nullptr);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(s_state.card_bundle_table == nullptr);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(args->card_table != nullptr);
        assert(args->lowest_address != nullptr);
        assert(args->highest_address != nullptr);
        assert(args->ephemeral_low != nullptr);
        assert(args->ephemeral_high != nullptr);
        assert(!args->requires_upper_bounds_check);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        assert(args->card_bundle_table != nullptr);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

        s_state = *args;
        UpdateEphemeralState(*args);
        PublishCardTableState(s_state);
        PublishHeapBounds(s_state);
        PublishEphemeralState(s_state);

        completionActions |=
            ::StompWriteBarrierResize(s_state, true, false);
        completionActions |=
            ::StompWriteBarrierEphemeral(s_state, true);
        break;

    case WriteBarrierOp::SwitchToWriteWatch:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(args->is_runtime_suspended);
        assert(args->write_watch_table != nullptr);
        s_state.write_watch_table = args->write_watch_table;
        PublishWriteWatchState(s_state.write_watch_table, true);
        completionActions |=
            ::SwitchToWriteWatchBarrier(s_state, true);
#else
        assert(!"should never be called without FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        break;

    case WriteBarrierOp::SwitchToNonWriteWatch:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        assert(args->is_runtime_suspended);
        s_state.write_watch_table = nullptr;
        PublishWriteWatchState(nullptr, false);
        completionActions |=
            ::SwitchToNonWriteWatchBarrier(s_state, true);
#else
        assert(!"should never be called without FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP");
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        break;

    default:
        assert(!"unknown WriteBarrierOp enum");
    }

    if (completionActions & SWB_ICACHE_FLUSH)
    {
        ::FlushWriteBarrierInstructionCache();
    }

    if (completionActions & SWB_EE_RESTART)
    {
        assert(!args->is_runtime_suspended);
        GCToEEInterface::RestartForWriteBarrier();
    }
}

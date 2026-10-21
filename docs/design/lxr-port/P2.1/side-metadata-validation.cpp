// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/side_metadata.h"

#include <atomic>
#include <stdio.h>
#include <string.h>
#include <thread>
#include <vector>

namespace
{
int s_checks;
int s_failures;

void Expect(const char* name, bool condition)
{
    s_checks++;
    if (!condition)
    {
        s_failures++;
        printf("FAIL: %s\n", name);
    }
}

void ExpectResult(const char* name, SideMetadataResult actual, SideMetadataResult expected)
{
    Expect(name, actual == expected);
}

struct WordSummary
{
    size_t count;
    uintptr_t firstAddress;
    uintptr_t firstMask;
    uintptr_t lastAddress;
    uintptr_t lastMask;
    uintptr_t active;
};

bool CollectWord(uintptr_t address, uintptr_t value, uintptr_t coverage, void* context)
{
    WordSummary* summary = static_cast<WordSummary*>(context);
    if (summary->count == 0)
    {
        summary->firstAddress = address;
        summary->firstMask = coverage;
    }
    summary->count++;
    summary->lastAddress = address;
    summary->lastMask = coverage;
    summary->active |= value & coverage;
    return true;
}

void WaitForStart(
    std::atomic<uint32_t>* ready,
    std::atomic<bool>* start)
{
    ready->fetch_add(1, std::memory_order_seq_cst);
    while (!start->load(std::memory_order_seq_cst))
    {
        std::this_thread::yield();
    }
}

#ifdef HOST_64BIT
void TestLayout()
{
    for (uint8_t logBits = 1; logBits <= 3; logBits++)
    {
        LxrSideMetadataLayout layout;
        ExpectResult(
            "layout accepts RC width",
            LxrSideMetadataLayout::Create(logBits, &layout),
            SideMetadataResult::Success);
        Expect("global anchor", layout.GetGlobalBase() == LxrSideMetadataLayout::GlobalBaseAddress);
        Expect("local anchor", layout.GetLocalBase() == LxrSideMetadataLayout::LocalBaseAddress);
        Expect(
            "global range remains below local",
            layout.GetGlobalBase() + layout.GetGlobalSize() <= layout.GetLocalBase());
        Expect(
            "local range remains below address limit",
            layout.GetLocalBase() + layout.GetLocalSize() <= layout.GetAddressLimit());

        const SideMetadataSpec* rc = layout.GetSpec(LxrSideMetadataKind::ReferenceCount);
        Expect("RC spec exists", rc != nullptr);
        Expect("RC active width", rc->log_bits_per_value == logBits);
        Expect("RC reserves 8-bit width", rc->reserved_log_bits_per_value == 3);
        Expect("RC uses CoreCLR 8-byte address granularity", rc->log_bytes_per_value == 3);
        Expect(
            "RC reserved size is stable",
            rc->reserved_size == (static_cast<size_t>(1) << 44));
        Expect(
            "RC logical size follows width",
            rc->logical_size == (static_cast<size_t>(1) << (41 + logBits)));
    }

    LxrSideMetadataLayout layout;
    ExpectResult(
        "layout rejects one-bit RC selector",
        LxrSideMetadataLayout::Create(0, &layout),
        SideMetadataResult::InvalidArgument);
    ExpectResult(
        "layout rejects sixteen-bit RC selector",
        LxrSideMetadataLayout::Create(4, &layout),
        SideMetadataResult::InvalidArgument);
    ExpectResult(
        "layout rejects null",
        LxrSideMetadataLayout::Create(1, nullptr),
        SideMetadataResult::InvalidArgument);

    constexpr uint32_t BitShift = 1;
    uint32_t littleShift = SideMetadataManager::ComputeWordShiftForByteOrder(
        0,
        1,
        BitShift,
        false);
    uint32_t bigShift = SideMetadataManager::ComputeWordShiftForByteOrder(
        0,
        1,
        BitShift,
        true);
    Expect("little-endian first-byte shift", littleShift == BitShift);
    Expect(
        "big-endian first-byte shift",
        bigShift == ((sizeof(uintptr_t) - 1) * 8) + BitShift);

    SideMetadataLocation little = {
        nullptr,
        static_cast<uintptr_t>(1) << littleShift,
        static_cast<uint8_t>(littleShift),
        1,
    };
    SideMetadataLocation big = {
        nullptr,
        static_cast<uintptr_t>(1) << bigShift,
        static_cast<uint8_t>(bigShift),
        1,
    };
    Expect(
        "little-endian first coverage",
        SideMetadataManager::ComputeFirstWordCoverageForByteOrder(little, false) ==
            (UINTPTR_MAX << littleShift));
    Expect(
        "little-endian last coverage",
        SideMetadataManager::ComputeLastWordCoverageForByteOrder(little, false) ==
            ((static_cast<uintptr_t>(1) << (littleShift + 1)) - 1));
    uintptr_t bigFirst =
        SideMetadataManager::ComputeFirstWordCoverageForByteOrder(big, true);
    uintptr_t bigLast =
        SideMetadataManager::ComputeLastWordCoverageForByteOrder(big, true);
    uintptr_t bigExpected =
        ((static_cast<uintptr_t>(1) << 6) - 1) << (bigShift);
    SideMetadataLocation bigRangeLast = big;
    bigRangeLast.shift = static_cast<uint8_t>(bigShift + 5);
    bigLast = SideMetadataManager::ComputeLastWordCoverageForByteOrder(bigRangeLast, true);
    Expect("big-endian same-byte range coverage", (bigFirst & bigLast) == bigExpected);

    uint32_t middleBigShift = SideMetadataManager::ComputeWordShiftForByteOrder(
        1,
        1,
        BitShift,
        true);
    SideMetadataLocation middleBig = {
        nullptr,
        static_cast<uintptr_t>(1) << middleBigShift,
        static_cast<uint8_t>(middleBigShift),
        1,
    };
    uint32_t middleByteShift = middleBigShift & ~7u;
    uintptr_t middleExpected =
        ((static_cast<uintptr_t>(1) << middleByteShift) - 1) |
        ((((static_cast<uintptr_t>(UINT8_MAX) << BitShift) & UINT8_MAX)) <<
         middleByteShift);
    Expect(
        "big-endian middle-byte first coverage does not spill",
        SideMetadataManager::ComputeFirstWordCoverageForByteOrder(middleBig, true) ==
            middleExpected);
}

void TestCollisionAndCleanup(const LxrSideMetadataLayout& layout)
{
    size_t pageSize = GCToOSInterface::GetPageSize();
    void* globalSentinel = GCToOSInterface::VirtualReserveAt(
        reinterpret_cast<void*>(layout.GetGlobalBase()),
        pageSize,
        VirtualReserveFlags::NoReserve);
    Expect("global collision sentinel reserved", globalSentinel != nullptr);
    if (globalSentinel != nullptr)
    {
        Expect("global sentinel committed", GCToOSInterface::VirtualCommit(globalSentinel, pageSize));
        *static_cast<volatile uint8_t*>(globalSentinel) = 0x5a;

        SideMetadataManager manager;
        ExpectResult(
            "global collision fails closed",
            manager.Initialize(&layout),
            SideMetadataResult::ReservationFailed);
        Expect(
            "failed reservation does not replace collision",
            *static_cast<volatile uint8_t*>(globalSentinel) == 0x5a);
        Expect(
            "global collision sentinel released",
            GCToOSInterface::VirtualRelease(globalSentinel, pageSize));
    }

    void* localSentinel = GCToOSInterface::VirtualReserveAt(
        reinterpret_cast<void*>(layout.GetLocalBase()),
        pageSize,
        VirtualReserveFlags::NoReserve);
    Expect("local collision sentinel reserved", localSentinel != nullptr);
    if (localSentinel != nullptr)
    {
        SideMetadataManager manager;
        ExpectResult(
            "partial reservation fails closed",
            manager.Initialize(&layout),
            SideMetadataResult::ReservationFailed);
        Expect(
            "local collision sentinel released",
            GCToOSInterface::VirtualRelease(localSentinel, pageSize));

        SideMetadataManager retry;
        ExpectResult(
            "partial failure released global reservation",
            retry.Initialize(&layout),
            SideMetadataResult::Success);
        retry.Shutdown();
    }
}

void TestMappingAndOperations(uint8_t logReferenceCountBits)
{
    constexpr uintptr_t DataStart = UINT64_C(0x0000000100000000);
    constexpr size_t DataSize = 64 * 1024;

    LxrSideMetadataLayout layout;
    ExpectResult(
        "create operational layout",
        LxrSideMetadataLayout::Create(logReferenceCountBits, &layout),
        SideMetadataResult::Success);

    SideMetadataManager manager;
    ExpectResult(
        "reserve fixed shadows",
        manager.Initialize(&layout),
        SideMetadataResult::Success);
    ExpectResult(
        "commit data metadata",
        manager.CommitDataRange(DataStart, DataSize),
        SideMetadataResult::Success);
    Expect("owned start", manager.IsDataAddressOwned(DataStart));
    Expect("owned last byte", manager.IsDataAddressOwned(DataStart + DataSize - 1));
    Expect("below range is not owned", !manager.IsDataAddressOwned(DataStart - 1));
    Expect("end is not owned", !manager.IsDataAddressOwned(DataStart + DataSize));

    const SideMetadataSpec* field = layout.GetSpec(LxrSideMetadataKind::FieldUnlogged);
    SideMetadataLocation location;
    ExpectResult(
        "map field-log bit",
        manager.GetLocation(LxrSideMetadataKind::FieldUnlogged, DataStart, &location),
        SideMetadataResult::Success);
    Expect(
        "field-log byte formula",
        reinterpret_cast<uintptr_t>(location.word) ==
            ((field->base_address + (DataStart >> 6)) &
             ~(static_cast<uintptr_t>(sizeof(uintptr_t)) - 1)));
    Expect(
        "field-log bit shift formula",
        location.shift ==
            static_cast<uint8_t>((DataStart >> 3) & ((sizeof(uintptr_t) * 8) - 1)));
    Expect(
        "fixed shadow is its own biased base",
        manager.GetBiasedBase(LxrSideMetadataKind::FieldUnlogged) == field->base_address);
    Expect(
        "multi-bit spec has no field-log biased base",
        manager.GetBiasedBase(LxrSideMetadataKind::ReferenceCount) == 0);

    uintptr_t value;
    ExpectResult(
        "zero field-log load",
        manager.Load(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            &value),
        SideMetadataResult::Success);
    Expect("new metadata is demand zero", value == 0);
    ExpectResult(
        "set field-log bit",
        manager.Store(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            1,
            SideMetadataMemoryOrder::SequentiallyConsistent),
        SideMetadataResult::Success);
    ExpectResult(
        "load set field-log bit",
        manager.Load(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            &value),
        SideMetadataResult::Success);
    Expect("field-log bit is set", value == 1);

    uintptr_t observed;
    bool exchanged;
    ExpectResult(
        "field-log compare exchange",
        manager.CompareExchange(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            0,
            1,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            &observed,
            &exchanged),
        SideMetadataResult::Success);
    Expect("field-log compare exchange observed one", observed == 1);
    Expect("field-log compare exchange won", exchanged);

    ExpectResult(
        "field-log invalid value rejected",
        manager.Store(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            2,
            SideMetadataMemoryOrder::SequentiallyConsistent),
        SideMetadataResult::InvalidArgument);
    ExpectResult(
        "unowned access rejected",
        manager.Load(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart - 8,
            SideMetadataMemoryOrder::Relaxed,
            &value),
        SideMetadataResult::AddressNotOwned);

    const SideMetadataSpec* rc = layout.GetSpec(LxrSideMetadataKind::ReferenceCount);
    uintptr_t rcMaximum =
        (static_cast<uintptr_t>(1) << (static_cast<uint32_t>(1) << rc->log_bits_per_value)) - 1;
    SideMetadataUpdateStatus status;
    for (uintptr_t expected = 0; expected < rcMaximum; expected++)
    {
        ExpectResult(
            "sticky RC increment",
            manager.IncrementSaturating(
                LxrSideMetadataKind::ReferenceCount,
                DataStart,
                SideMetadataMemoryOrder::Relaxed,
                &observed,
                &status),
            SideMetadataResult::Success);
        Expect("sticky RC observed prior value", observed == expected);
        Expect("sticky RC incremented", status == SideMetadataUpdateStatus::Updated);
    }
    ExpectResult(
        "sticky RC maximum",
        manager.IncrementSaturating(
            LxrSideMetadataKind::ReferenceCount,
            DataStart,
            SideMetadataMemoryOrder::Relaxed,
            &observed,
            &status),
        SideMetadataResult::Success);
    Expect("sticky RC remains maximum", observed == rcMaximum);
    Expect("sticky RC reports saturation", status == SideMetadataUpdateStatus::Saturated);
    ExpectResult(
        "sticky RC decrement refused",
        manager.DecrementNonZeroNonSaturated(
            LxrSideMetadataKind::ReferenceCount,
            DataStart,
            SideMetadataMemoryOrder::Relaxed,
            &observed,
            &status),
        SideMetadataResult::Success);
    Expect("sticky RC decrement reports saturation", status == SideMetadataUpdateStatus::Saturated);

    ExpectResult(
        "word-sized block owner store",
        manager.Store(
            LxrSideMetadataKind::BlockOwner,
            DataStart,
            UINT64_C(0x123456789abcdef0),
            SideMetadataMemoryOrder::SequentiallyConsistent),
        SideMetadataResult::Success);
    ExpectResult(
        "word-sized block owner load",
        manager.Load(
            LxrSideMetadataKind::BlockOwner,
            DataStart,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            &value),
        SideMetadataResult::Success);
    Expect("word-sized block owner round trips", value == UINT64_C(0x123456789abcdef0));

    uintptr_t first = DataStart + 8;
    size_t middleSize = 6 * 8;
    ExpectResult(
        "set left reset sentinel",
        manager.Store(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            1,
            SideMetadataMemoryOrder::SequentiallyConsistent),
        SideMetadataResult::Success);
    ExpectResult(
        "set right reset sentinel",
        manager.Store(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart + 7 * 8,
            1,
            SideMetadataMemoryOrder::SequentiallyConsistent),
        SideMetadataResult::Success);
    ExpectResult(
        "set reset range",
        manager.ResetRangeQuiescent(
            LxrSideMetadataKind::FieldUnlogged,
            first,
            middleSize,
            1),
        SideMetadataResult::Success);

    WordSummary summary = {};
    ExpectResult(
        "visit first and last partial word",
        manager.VisitWords(
            LxrSideMetadataKind::FieldUnlogged,
            first,
            middleSize,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            CollectWord,
            &summary),
        SideMetadataResult::Success);
    Expect("partial word visit produced words", summary.count >= 1);
    Expect("partial word visit observed active fields", summary.active != 0);
    Expect("partial word starts with a mask", summary.firstMask != UINTPTR_MAX);
    Expect("partial word ends with a mask", summary.lastMask != UINTPTR_MAX);

    ExpectResult(
        "clear only middle range",
        manager.ResetRangeQuiescent(
            LxrSideMetadataKind::FieldUnlogged,
            first,
            middleSize,
            0),
        SideMetadataResult::Success);
    ExpectResult(
        "left reset sentinel preserved",
        manager.Load(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart,
            SideMetadataMemoryOrder::Relaxed,
            &value),
        SideMetadataResult::Success);
    Expect("left reset sentinel remains set", value == 1);
    ExpectResult(
        "right reset sentinel preserved",
        manager.Load(
            LxrSideMetadataKind::FieldUnlogged,
            DataStart + 7 * 8,
            SideMetadataMemoryOrder::Relaxed,
            &value),
        SideMetadataResult::Success);
    Expect("right reset sentinel remains set", value == 1);

    ExpectResult(
        "set object mark for copy",
        manager.Store(
            LxrSideMetadataKind::ObjectMark,
            DataStart + 16,
            1,
            SideMetadataMemoryOrder::Relaxed),
        SideMetadataResult::Success);
    ExpectResult(
        "copy compatible metadata",
        manager.CopyRangeQuiescent(
            LxrSideMetadataKind::ObjectMark,
            DataStart,
            LxrSideMetadataKind::ValidObject,
            DataStart,
            32),
        SideMetadataResult::Success);
    ExpectResult(
        "load copied valid-object bit",
        manager.Load(
            LxrSideMetadataKind::ValidObject,
            DataStart + 16,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            &value),
        SideMetadataResult::Success);
    Expect("compatible metadata copy preserved value", value == 1);

    manager.Shutdown();

    SideMetadataManager retry;
    ExpectResult(
        "fixed shadows can be reserved after teardown",
        retry.Initialize(&layout),
        SideMetadataResult::Success);
    retry.Shutdown();
}

void TestNeighborContention(uint8_t logReferenceCountBits)
{
    constexpr uintptr_t DataStart = UINT64_C(0x0000000200000000);
    constexpr size_t DataSize = 64 * 1024;
    constexpr uint32_t ThreadCount = 8;

    LxrSideMetadataLayout layout;
    ExpectResult(
        "create contention layout",
        LxrSideMetadataLayout::Create(logReferenceCountBits, &layout),
        SideMetadataResult::Success);
    SideMetadataManager manager;
    ExpectResult(
        "reserve contention shadows",
        manager.Initialize(&layout),
        SideMetadataResult::Success);
    ExpectResult(
        "commit contention range",
        manager.CommitDataRange(DataStart, DataSize),
        SideMetadataResult::Success);

    std::atomic<uint32_t> ready = 0;
    std::atomic<bool> start = false;
    std::vector<std::thread> threads;
    threads.reserve(ThreadCount);
    for (uint32_t index = 0; index < ThreadCount; index++)
    {
        threads.emplace_back(
            [&, index]()
            {
                WaitForStart(&ready, &start);
                SideMetadataResult result = manager.Store(
                    LxrSideMetadataKind::FieldUnlogged,
                    DataStart + index * 8,
                    1,
                    SideMetadataMemoryOrder::SequentiallyConsistent);
                if (result != SideMetadataResult::Success)
                {
                    return;
                }
            });
    }
    while (ready.load(std::memory_order_seq_cst) != ThreadCount)
    {
        std::this_thread::yield();
    }
    start.store(true, std::memory_order_seq_cst);
    for (std::thread& thread : threads)
    {
        thread.join();
    }

    for (uint32_t index = 0; index < ThreadCount; index++)
    {
        uintptr_t value;
        ExpectResult(
            "neighbor field load",
            manager.Load(
                LxrSideMetadataKind::FieldUnlogged,
                DataStart + index * 8,
                SideMetadataMemoryOrder::SequentiallyConsistent,
                &value),
            SideMetadataResult::Success);
        Expect("neighbor field update was not lost", value == 1);
    }

    uint32_t rcBitCount = static_cast<uint32_t>(1) << logReferenceCountBits;
    uint32_t rcFieldsPerByte = 8 / rcBitCount;
    ExpectResult(
        "clear RC contention fields",
        manager.ResetRangeQuiescent(
            LxrSideMetadataKind::ReferenceCount,
            DataStart,
            rcFieldsPerByte * 8,
            0),
        SideMetadataResult::Success);

    ready.store(0, std::memory_order_seq_cst);
    start.store(false, std::memory_order_seq_cst);
    threads.clear();
    for (uint32_t index = 0; index < rcFieldsPerByte; index++)
    {
        threads.emplace_back(
            [&, index]()
            {
                WaitForStart(&ready, &start);
                uintptr_t previous;
                SideMetadataUpdateStatus status;
                manager.IncrementSaturating(
                    LxrSideMetadataKind::ReferenceCount,
                    DataStart + index * 8,
                    SideMetadataMemoryOrder::Relaxed,
                    &previous,
                    &status);
            });
    }
    while (ready.load(std::memory_order_seq_cst) != rcFieldsPerByte)
    {
        std::this_thread::yield();
    }
    start.store(true, std::memory_order_seq_cst);
    for (std::thread& thread : threads)
    {
        thread.join();
    }
    for (uint32_t index = 0; index < rcFieldsPerByte; index++)
    {
        uintptr_t value;
        manager.Load(
            LxrSideMetadataKind::ReferenceCount,
            DataStart + index * 8,
            SideMetadataMemoryOrder::Relaxed,
            &value);
        Expect("neighbor RC update was not lost", value == 1);
    }

    manager.Shutdown();
}

void TestDeterministicBrokenControls()
{
    constexpr uint32_t ThreadCount = 8;
    std::atomic<uintptr_t> word = 0;
    std::atomic<uint32_t> loaded = 0;
    std::atomic<bool> store = false;
    std::vector<std::thread> threads;

    for (uint32_t index = 0; index < ThreadCount; index++)
    {
        threads.emplace_back(
            [&, index]()
            {
                uintptr_t oldValue = word.load(std::memory_order_seq_cst);
                loaded.fetch_add(1, std::memory_order_seq_cst);
                while (!store.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }
                word.store(oldValue | (static_cast<uintptr_t>(1) << index), std::memory_order_seq_cst);
            });
    }
    while (loaded.load(std::memory_order_seq_cst) != ThreadCount)
    {
        std::this_thread::yield();
    }
    store.store(true, std::memory_order_seq_cst);
    for (std::thread& thread : threads)
    {
        thread.join();
    }
    Expect("plain atomic load/store control loses neighbors", word.load() != 0xff);

    word.store(0, std::memory_order_seq_cst);
    loaded.store(0, std::memory_order_seq_cst);
    store.store(false, std::memory_order_seq_cst);
    threads.clear();
    std::atomic<uint32_t> winners = 0;
    for (uint32_t index = 0; index < ThreadCount; index++)
    {
        threads.emplace_back(
            [&, index]()
            {
                uintptr_t oldValue = word.load(std::memory_order_seq_cst);
                loaded.fetch_add(1, std::memory_order_seq_cst);
                while (!store.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }
                uintptr_t desired = oldValue | (static_cast<uintptr_t>(1) << index);
                if (word.compare_exchange_strong(oldValue, desired, std::memory_order_seq_cst))
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
                }
            });
    }
    while (loaded.load(std::memory_order_seq_cst) != ThreadCount)
    {
        std::this_thread::yield();
    }
    store.store(true, std::memory_order_seq_cst);
    for (std::thread& thread : threads)
    {
        thread.join();
    }
    Expect("one-shot CAS control has one winner", winners.load() == 1);
    Expect("one-shot CAS control loses neighbors", word.load() != 0xff);
}

void TestRangeEdges()
{
    LxrSideMetadataLayout layout;
    ExpectResult(
        "create edge layout",
        LxrSideMetadataLayout::Create(1, &layout),
        SideMetadataResult::Success);
    SideMetadataManager manager;
    ExpectResult(
        "reserve edge shadows",
        manager.Initialize(&layout),
        SideMetadataResult::Success);

    constexpr size_t EdgeSize = 4096;
    uintptr_t edgeStart = static_cast<uintptr_t>(LxrSideMetadataLayout::AddressLimit) - EdgeSize;
    ExpectResult(
        "commit last architectural page",
        manager.CommitDataRange(edgeStart, EdgeSize),
        SideMetadataResult::Success);
    SideMetadataLocation location;
    ExpectResult(
        "map last architectural granule",
        manager.GetLocation(
            LxrSideMetadataKind::ReferenceCount,
            static_cast<uintptr_t>(LxrSideMetadataLayout::AddressLimit) - 8,
            &location),
        SideMetadataResult::Success);
    ExpectResult(
        "reject address limit",
        manager.GetLocation(
            LxrSideMetadataKind::ReferenceCount,
            static_cast<uintptr_t>(LxrSideMetadataLayout::AddressLimit),
            &location),
        SideMetadataResult::AddressNotOwned);
    ExpectResult(
        "reject overflowing data range",
        manager.CommitDataRange(UINTPTR_MAX - 3, 8),
        SideMetadataResult::AddressOverflow);
    manager.Shutdown();
}

void TestOwnedRangeFixedPointMerge()
{
    LxrSideMetadataLayout layout;
    ExpectResult(
        "create range-merge layout",
        LxrSideMetadataLayout::Create(1, &layout),
        SideMetadataResult::Success);
    SideMetadataManager manager;
    ExpectResult(
        "reserve range-merge shadows",
        manager.Initialize(&layout),
        SideMetadataResult::Success);

    constexpr uintptr_t Base = UINT64_C(0x0000000400000000);
    ExpectResult(
        "commit middle range",
        manager.CommitDataRange(Base + 250 * 8, 200 * 8),
        SideMetadataResult::Success);
    ExpectResult(
        "commit high range",
        manager.CommitDataRange(Base + 500 * 8, 100 * 8),
        SideMetadataResult::Success);
    ExpectResult(
        "commit bridging range",
        manager.CommitDataRange(Base + 400 * 8, 120 * 8),
        SideMetadataResult::Success);
    Expect(
        "grown range is merged to a fixed point",
        manager.IsDataRangeOwned(Base + 300 * 8, 250 * 8));
    manager.Shutdown();
}

#else
void TestUnsupported32Bit()
{
    LxrSideMetadataLayout layout;
    ExpectResult(
        "32-bit fixed shadow layout fails closed",
        LxrSideMetadataLayout::Create(1, &layout),
        SideMetadataResult::UnsupportedAddressSpace);
}
#endif
}

int main()
{
#ifdef HOST_64BIT
    TestLayout();

    LxrSideMetadataLayout collisionLayout;
    if (LxrSideMetadataLayout::Create(1, &collisionLayout) == SideMetadataResult::Success)
    {
        TestCollisionAndCleanup(collisionLayout);
    }

    for (uint8_t logBits = 1; logBits <= 3; logBits++)
    {
        TestMappingAndOperations(logBits);
        TestNeighborContention(logBits);
    }
    TestDeterministicBrokenControls();
    TestRangeEdges();
    TestOwnedRangeFixedPointMerge();
#else
    TestUnsupported32Bit();
#endif

    printf("%d/%d side metadata checks passed\n", s_checks - s_failures, s_checks);
    return s_failures == 0 ? 0 : 1;
}

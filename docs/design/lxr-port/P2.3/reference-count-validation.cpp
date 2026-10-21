// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/reference_count.h"

#include <atomic>
#include <stdio.h>
#include <thread>
#include <vector>

namespace
{
#ifdef HOST_64BIT
    constexpr uintptr_t DataStart = UINT64_C(0x0000000500000000);
    constexpr size_t DataSize = 8 * ImmixBlockGeometry::BlockBytes;
#endif

    int s_checks;
    int s_failedChecks;
    int s_failedCases;
    int s_cases;
    std::atomic<int> s_invariantChecks;

    void Expect(const char* name, bool condition)
    {
        s_checks++;
        if (!condition)
        {
            printf("FAIL: %s\n", name);
            s_failedChecks++;
        }
    }

    template <typename TAction>
    void RunCase(const char* name, TAction action)
    {
        int failuresBefore = s_failedChecks;
        s_cases++;
        action();
        if (s_failedChecks == failuresBefore)
        {
            printf("PASS: %s\n", name);
        }
        else
        {
            s_failedCases++;
            printf("FAIL: case %s\n", name);
        }
    }

#ifdef HOST_64BIT
    uint64_t KindMask(LxrSideMetadataKind kind)
    {
        return UINT64_C(1) << static_cast<uint8_t>(kind);
    }

    struct Fixture
    {
        LxrSideMetadataLayout layout;
        SideMetadataManager metadata;
        ReferenceCountTable table;
        uint8_t logBits;
        bool initialized;

        explicit Fixture(
            uint8_t requestedLogBits = 1,
            uint64_t enabledSpecMask = UINT64_MAX,
            uintptr_t start = DataStart,
            size_t size = DataSize)
            : logBits(requestedLogBits),
              initialized(false)
        {
            SideMetadataResult result =
                LxrSideMetadataLayout::Create(logBits, &layout);
            if (result == SideMetadataResult::Success)
            {
                result = metadata.Initialize(&layout, enabledSpecMask);
            }
            if (result == SideMetadataResult::Success)
            {
                result = metadata.CommitDataRange(start, size);
            }
            if (result == SideMetadataResult::Success)
            {
                result = table.Initialize(&metadata, &layout);
            }
            initialized = result == SideMetadataResult::Success;
        }

        ~Fixture()
        {
            table.Shutdown();
            metadata.Shutdown();
        }
    };

    bool InitializeObject(
        Fixture& fixture,
        uintptr_t object,
        size_t size = ReferenceCountTable::MinimumObjectSize)
    {
        return fixture.table.InitializeObjectQuiescent(object, size) ==
            SideMetadataResult::Success;
    }

    bool SetCount(Fixture& fixture, uintptr_t object, uintptr_t value)
    {
        uintptr_t observed;
        bool exchanged;
        return (fixture.table.CompareExchange(
                    object,
                    value,
                    0,
                    &observed,
                    &exchanged) == SideMetadataResult::Success) &&
            exchanged &&
            (observed == 0);
    }

    uintptr_t LoadCount(Fixture& fixture, uintptr_t object)
    {
        uintptr_t count = UINTPTR_MAX;
        Expect(
            "load count succeeds",
            fixture.table.Load(object, &count) == SideMetadataResult::Success);
        return count;
    }

    uintptr_t ContinuationLine(uintptr_t object, size_t index = 0)
    {
        return (object & ~ImmixBlockGeometry::LineMask) +
            ((index + 1) * ImmixBlockGeometry::LineBytes);
    }

    bool IsContinuation(Fixture& fixture, uintptr_t line)
    {
        bool continuation = false;
        Expect(
            "continuation query succeeds",
            fixture.table.IsContinuationLine(line, &continuation) ==
                SideMetadataResult::Success);
        return continuation;
    }

    void IncrementMany(
        ReferenceCountTable* table,
        uintptr_t object,
        size_t iterations,
        std::atomic<bool>* start)
    {
        while (!start->load(std::memory_order_acquire))
        {
            YieldProcessor();
        }
        for (size_t iteration = 0; iteration < iterations; iteration++)
        {
            uintptr_t previous;
            SideMetadataUpdateStatus status;
            SideMetadataResult result =
                table->Increment(object, &previous, &status);
            if (result != SideMetadataResult::Success)
            {
                return;
            }
        }
    }

    bool ValidateOldReference(
        ReferenceCountTable& table,
        uintptr_t oldReference)
    {
        s_invariantChecks.fetch_add(1, std::memory_order_relaxed);
        if (oldReference == 0)
        {
            return true;
        }

        uintptr_t count;
        return (table.Load(oldReference, &count) == SideMetadataResult::Success) &&
            (count != 0);
    }

    void RunWidthLayoutCase(uint8_t logBits)
    {
        Fixture fixture(logBits);
        Expect("fixture initialized", fixture.initialized);
        Expect(
            "active width",
            fixture.table.GetActiveBits() ==
                (static_cast<uint8_t>(1) << logBits));
        uintptr_t expectedMaximum =
            (static_cast<uintptr_t>(1) << fixture.table.GetActiveBits()) - 1;
        Expect(
            "derived maximum",
            fixture.table.GetMaximumCount() == expectedMaximum);

        uintptr_t object = DataStart;
        Expect("object initialized", InitializeObject(fixture, object));
        SideMetadataLocation location;
        Expect(
            "RC location",
            fixture.metadata.GetLocation(
                LxrSideMetadataKind::ReferenceCount,
                object,
                &location) == SideMetadataResult::Success);
        Expect(
            "RC location width",
            location.bit_count == fixture.table.GetActiveBits());
    }

    void RunAllValuesCase(uint8_t logBits)
    {
        Fixture fixture(logBits);
        Expect("fixture initialized", fixture.initialized);
        uintptr_t object = DataStart;
        Expect("object initialized", InitializeObject(fixture, object));

        uintptr_t maximum = fixture.table.GetMaximumCount();
        for (uintptr_t value = 0; value <= maximum; value++)
        {
            Expect(
                "reset exhaustive object",
                fixture.table.ResetObjectQuiescent(
                    object,
                    ReferenceCountTable::MinimumObjectSize) ==
                    SideMetadataResult::Success);
            Expect("reinitialize exhaustive object", InitializeObject(fixture, object));
            if (value != 0)
            {
                Expect("set exhaustive value", SetCount(fixture, object, value));
            }
            Expect("read exhaustive value", LoadCount(fixture, object) == value);
        }
    }

    void RunSharedWordCase(uint8_t logBits)
    {
        Fixture fixture(logBits);
        Expect("fixture initialized", fixture.initialized);
        constexpr size_t ObjectCount = 3;
        uintptr_t objects[ObjectCount] = {
            DataStart,
            DataStart + 24,
            DataStart + 48,
        };
        for (uintptr_t object : objects)
        {
            Expect("shared-word object initialized", InitializeObject(fixture, object));
        }

        std::atomic<bool> start(false);
        std::vector<std::thread> threads;
        for (uintptr_t object : objects)
        {
            threads.emplace_back(
                IncrementMany,
                &fixture.table,
                object,
                static_cast<size_t>(fixture.table.GetMaximumCount()) + 32,
                &start);
        }
        start.store(true, std::memory_order_release);
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        for (uintptr_t object : objects)
        {
            Expect(
                "shared-word count preserved",
                LoadCount(fixture, object) == fixture.table.GetMaximumCount());
        }
    }

    void RunStraddleCase(
        uintptr_t object,
        size_t size,
        size_t expectedContinuations)
    {
        Fixture fixture;
        Expect("fixture initialized", fixture.initialized);
        Expect("straddle object initialized", InitializeObject(fixture, object, size));
        Expect(
            "mark straddle lines",
            fixture.table.MarkStraddleLinesQuiescent(object, size) ==
                SideMetadataResult::Success);

        for (size_t index = 0; index < expectedContinuations; index++)
        {
            Expect(
                "expected continuation",
                IsContinuation(fixture, ContinuationLine(object, index)));
        }
        uintptr_t following = ContinuationLine(object, expectedContinuations);
        Expect("following line is not continuation", !IsContinuation(fixture, following));
    }
#endif
}

int main()
{
#ifndef HOST_64BIT
    RunCase("x86 fails closed", []()
    {
        LxrSideMetadataLayout layout;
        Expect(
            "x86 layout rejected",
            LxrSideMetadataLayout::Create(1, &layout) ==
                SideMetadataResult::UnsupportedAddressSpace);
    });
#else
    RunCase("2-bit layout and mapping", []() { RunWidthLayoutCase(1); });
    RunCase("4-bit layout and mapping", []() { RunWidthLayoutCase(2); });
    RunCase("8-bit layout and mapping", []() { RunWidthLayoutCase(3); });
    RunCase("2-bit exhaustive values", []() { RunAllValuesCase(1); });
    RunCase("4-bit exhaustive values", []() { RunAllValuesCase(2); });
    RunCase("8-bit exhaustive values", []() { RunAllValuesCase(3); });

    RunCase("increment returns previous", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        Expect(
            "increment succeeds",
            fixture.table.Increment(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect("increment previous zero", previous == 0);
        Expect("increment updated", status == SideMetadataUpdateStatus::Updated);
    });

    RunCase("decrement returns previous", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        Expect("count set to two", SetCount(fixture, object, 2));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        Expect(
            "decrement succeeds",
            fixture.table.Decrement(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect("decrement previous two", previous == 2);
        Expect("decrement updated", status == SideMetadataUpdateStatus::Updated);
        Expect("decrement result one", LoadCount(fixture, object) == 1);
    });

    RunCase("increment reaches sticky", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        for (uintptr_t count = 0; count < fixture.table.GetMaximumCount(); count++)
        {
            Expect(
                "increment toward sticky",
                fixture.table.Increment(object, &previous, &status) ==
                    SideMetadataResult::Success);
        }
        bool sticky;
        Expect(
            "sticky query",
            fixture.table.IsSticky(object, &sticky) ==
                SideMetadataResult::Success);
        Expect("sticky true", sticky);
        Expect(
            "sticky increment call",
            fixture.table.Increment(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect("sticky increment status", status == SideMetadataUpdateStatus::Saturated);
    });

    RunCase("sticky decrement refused", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        Expect(
            "set sticky",
            SetCount(fixture, object, fixture.table.GetMaximumCount()));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        Expect(
            "sticky decrement call",
            fixture.table.Decrement(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect("sticky decrement previous", previous == fixture.table.GetMaximumCount());
        Expect("sticky decrement status", status == SideMetadataUpdateStatus::Saturated);
        Expect(
            "sticky count changed during decrement",
            LoadCount(fixture, object) == fixture.table.GetMaximumCount());
        uintptr_t observed;
        bool exchanged;
        Expect(
            "compare exchange cannot demote sticky",
            fixture.table.CompareExchange(
                object,
                0,
                fixture.table.GetMaximumCount(),
                &observed,
                &exchanged) == SideMetadataResult::InvalidArgument);
    });

    RunCase("zero decrement refused", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        Expect(
            "zero decrement call",
            fixture.table.Decrement(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect("zero decrement previous", previous == 0);
        Expect("zero decrement status", status == SideMetadataUpdateStatus::Zero);
        Expect("zero remains zero", LoadCount(fixture, object) == 0);
    });

    RunCase("same-slot contention saturates", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        std::atomic<bool> start(false);
        std::vector<std::thread> threads;
        for (size_t worker = 0; worker < 16; worker++)
        {
            threads.emplace_back(
                IncrementMany,
                &fixture.table,
                object,
                1024,
                &start);
        }
        start.store(true, std::memory_order_release);
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        Expect(
            "same-slot final sticky",
            LoadCount(fixture, object) == fixture.table.GetMaximumCount());
    });

    RunCase("sticky transition defeats late decrement", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        Expect(
            "seed one below sticky",
            SetCount(fixture, object, fixture.table.GetMaximumCount() - 1));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        Expect(
            "transition to sticky",
            fixture.table.Increment(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect(
            "late decrement call",
            fixture.table.Decrement(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect("late decrement refused", status == SideMetadataUpdateStatus::Saturated);
        Expect(
            "width maximum did not remain sticky",
            LoadCount(fixture, object) == fixture.table.GetMaximumCount());
    });

    RunCase("adjacent fields survive at every width", []()
    {
        RunSharedWordCase(1);
        RunSharedWordCase(2);
        RunSharedWordCase(3);
    });

    RunCase("separate cache lines survive", []()
    {
        Fixture fixture;
        uintptr_t first = DataStart;
        uintptr_t second = DataStart + 4096;
        Expect("fixture initialized", fixture.initialized);
        Expect("first object initialized", InitializeObject(fixture, first));
        Expect("second object initialized", InitializeObject(fixture, second));
        std::atomic<bool> start(false);
        std::thread firstThread(
            IncrementMany,
            &fixture.table,
            first,
            1024,
            &start);
        std::thread secondThread(
            IncrementMany,
            &fixture.table,
            second,
            1024,
            &start);
        start.store(true, std::memory_order_release);
        firstThread.join();
        secondThread.join();
        Expect(
            "first separate-line count",
            LoadCount(fixture, first) == fixture.table.GetMaximumCount());
        Expect(
            "second separate-line count",
            LoadCount(fixture, second) == fixture.table.GetMaximumCount());
    });

    RunCase("synthetic object registration", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect(
            "unregistered load rejected",
            fixture.table.Load(object, nullptr) ==
                SideMetadataResult::InvalidArgument);
        uintptr_t count;
        Expect(
            "unregistered exact start rejected",
            fixture.table.Load(object, &count) ==
                SideMetadataResult::AddressNotOwned);
        Expect("object initialized", InitializeObject(fixture, object));
        Expect(
            "registered exact start accepted",
            fixture.table.Load(object, &count) == SideMetadataResult::Success);
    });

    RunCase("interior address rejected", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        uintptr_t count;
        Expect(
            "interior exact lattice slot rejected",
            fixture.table.Load(object + 8, &count) ==
                SideMetadataResult::AddressNotOwned);
    });

    RunCase("misaligned byref rejected", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        uintptr_t count;
        Expect(
            "misaligned byref rejected",
            fixture.table.Load(object + 1, &count) ==
                SideMetadataResult::InvalidArgument);
    });

    RunCase("CoreCLR minimum object size", []()
    {
        Fixture fixture;
        Expect("fixture initialized", fixture.initialized);
        Expect(
            "minimum is 24 bytes",
            ReferenceCountTable::MinimumObjectSize == 24);
        Expect(
            "short object rejected",
            fixture.table.InitializeObjectQuiescent(DataStart, 16) ==
                SideMetadataResult::InvalidArgument);
        Expect(
            "minimum object accepted",
            InitializeObject(
                fixture,
                DataStart,
                ReferenceCountTable::MinimumObjectSize));
    });

    RunCase("47-bit upper boundary", []()
    {
        constexpr uintptr_t LastBlock =
            ImmixBlockGeometry::AddressLimit - ImmixBlockGeometry::BlockBytes;
        Fixture fixture(1, UINT64_MAX, LastBlock, ImmixBlockGeometry::BlockBytes);
        Expect("last-block fixture initialized", fixture.initialized);
        uintptr_t lastObject =
            ImmixBlockGeometry::AddressLimit -
            ReferenceCountTable::MinimumObjectSize;
        Expect(
            "last exact object accepted",
            InitializeObject(
                fixture,
                lastObject,
                ReferenceCountTable::MinimumObjectSize));
        Expect(
            "exclusive-limit object rejected",
            fixture.table.InitializeObjectQuiescent(
                ImmixBlockGeometry::AddressLimit,
                ReferenceCountTable::MinimumObjectSize) ==
                SideMetadataResult::InvalidArgument);
    });

    RunCase("endian location formulas", []()
    {
        Expect(
            "little-endian formula",
            SideMetadataManager::ComputeWordShiftForByteOrder(1, 1, 2, false) ==
                10);
        Expect(
            "big-endian formula",
            SideMetadataManager::ComputeWordShiftForByteOrder(1, 1, 2, true) ==
                ((sizeof(uintptr_t) - 2) * 8) + 2);
    });

    RunCase("24-byte object within line or exact line end", []()
    {
        RunStraddleCase(DataStart + 8, 24, 0);
        RunStraddleCase(DataStart + ImmixBlockGeometry::LineBytes - 24, 24, 0);
    });

    RunCase("24-byte partial next line", []()
    {
        RunStraddleCase(DataStart + ImmixBlockGeometry::LineBytes - 16, 24, 0);
    });

    RunCase("257-byte object has no full continuation", []()
    {
        RunStraddleCase(DataStart, 257, 0);
    });

    RunCase("512-byte object has one continuation", []()
    {
        RunStraddleCase(DataStart, 512, 1);
    });

    RunCase("partial tail excluded", []()
    {
        RunStraddleCase(DataStart, 600, 1);
    });

    RunCase("multiple continuation lines", []()
    {
        RunStraddleCase(DataStart, 768, 2);
    });

    RunCase("exact block end accepted", []()
    {
        Fixture fixture;
        uintptr_t object =
            DataStart + ImmixBlockGeometry::BlockBytes - 512;
        Expect("fixture initialized", fixture.initialized);
        Expect("block-end object initialized", InitializeObject(fixture, object, 512));
        Expect(
            "block-end straddles marked",
            fixture.table.MarkStraddleLinesQuiescent(object, 512) ==
                SideMetadataResult::Success);
        Expect(
            "block-end continuation",
            IsContinuation(fixture, ContinuationLine(object)));
    });

    RunCase("cross-block object rejected", []()
    {
        Fixture fixture;
        uintptr_t object =
            DataStart + ImmixBlockGeometry::BlockBytes - 24;
        Expect("fixture initialized", fixture.initialized);
        Expect(
            "one-byte cross-block rejected",
            fixture.table.InitializeObjectQuiescent(object, 25) ==
                SideMetadataResult::InvalidArgument);
    });

    RunCase("sentinel pair and corruption", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        constexpr size_t ObjectSize = 512;
        uintptr_t line = ContinuationLine(object);
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object, ObjectSize));
        Expect(
            "straddle marked",
            fixture.table.MarkStraddleLinesQuiescent(object, ObjectSize) ==
                SideMetadataResult::Success);
        Expect("sentinel query true", IsContinuation(fixture, line));
        Expect(
            "corrupt RC sentinel",
            fixture.metadata.Store(
                LxrSideMetadataKind::ReferenceCount,
                line,
                0,
                SideMetadataMemoryOrder::Relaxed) ==
                SideMetadataResult::Success);
        bool continuation;
        Expect(
            "continuation sentinel RC mismatch",
            fixture.table.IsContinuationLine(line, &continuation) ==
                SideMetadataResult::InconsistentState);
        Expect(
            "restore RC sentinel",
            fixture.metadata.Store(
                LxrSideMetadataKind::ReferenceCount,
                line,
                1,
                SideMetadataMemoryOrder::Relaxed) ==
                SideMetadataResult::Success);
        Expect(
            "remove straddle byte",
            fixture.metadata.Store(
                LxrSideMetadataKind::ReferenceCountStraddle,
                line,
                0,
                SideMetadataMemoryOrder::Relaxed) ==
                SideMetadataResult::Success);
        Expect(
            "continuation sentinel straddle mismatch",
            fixture.table.ClearStraddleLinesQuiescent(object, ObjectSize) ==
                SideMetadataResult::InconsistentState);
    });

    RunCase("straddle clear and reset", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        constexpr size_t ObjectSize = 768;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object, ObjectSize));
        Expect(
            "straddle marked",
            fixture.table.MarkStraddleLinesQuiescent(object, ObjectSize) ==
                SideMetadataResult::Success);
        Expect(
            "straddle cleared",
            fixture.table.ClearStraddleLinesQuiescent(object, ObjectSize) ==
                SideMetadataResult::Success);
        Expect(
            "first continuation cleared",
            !IsContinuation(fixture, ContinuationLine(object)));
        Expect(
            "object reset",
            fixture.table.ResetObjectQuiescent(object, ObjectSize) ==
                SideMetadataResult::Success);
        uintptr_t count;
        Expect(
            "reset object no longer registered",
            fixture.table.Load(object, &count) ==
                SideMetadataResult::AddressNotOwned);
    });

    RunCase("continuation extent requires reclamation before reuse", []()
    {
        Fixture fixture;
        uintptr_t oldObject = DataStart;
        constexpr size_t OldObjectSize = 512;
        uintptr_t reusedStart = ContinuationLine(oldObject);
        Expect("fixture initialized", fixture.initialized);
        Expect(
            "old straddling object initialized",
            InitializeObject(fixture, oldObject, OldObjectSize));
        Expect(
            "old straddling object marked",
            fixture.table.MarkStraddleLinesQuiescent(
                oldObject,
                OldObjectSize) == SideMetadataResult::Success);
        Expect(
            "continuation line start rejected",
            fixture.table.InitializeObjectQuiescent(
                reusedStart,
                ReferenceCountTable::MinimumObjectSize) ==
                SideMetadataResult::InconsistentState);
        Expect(
            "prior object metadata dismantled",
            fixture.table.ResetObjectQuiescent(
                oldObject,
                OldObjectSize) == SideMetadataResult::Success);
        Expect(
            "former continuation start reusable",
            fixture.table.InitializeObjectQuiescent(
                reusedStart,
                ReferenceCountTable::MinimumObjectSize) ==
                SideMetadataResult::Success);
        Expect(
            "former continuation state cleared",
            !IsContinuation(fixture, reusedStart));
    });

    RunCase("mature copy re-derives sentinels", []()
    {
        Fixture fixture;
        uintptr_t source = DataStart + 8;
        uintptr_t destination =
            DataStart + ImmixBlockGeometry::BlockBytes + 128;
        constexpr size_t ObjectSize = 640;
        Expect("fixture initialized", fixture.initialized);
        Expect("source initialized", InitializeObject(fixture, source, ObjectSize));
        Expect("source count set", SetCount(fixture, source, 2));
        Expect(
            "source straddle marked",
            fixture.table.MarkStraddleLinesQuiescent(source, ObjectSize) ==
                SideMetadataResult::Success);
        Expect(
            "object copied",
            fixture.table.CopyObjectQuiescent(
                source,
                destination,
                ObjectSize) == SideMetadataResult::Success);
        Expect("copied count", LoadCount(fixture, destination) == 2);
        Expect(
            "destination sentinel",
            IsContinuation(fixture, ContinuationLine(destination)));
        Expect(
            "second destination sentinel",
            IsContinuation(fixture, ContinuationLine(destination, 1)));
        Expect(
            "source sentinel retained",
            IsContinuation(fixture, ContinuationLine(source)));
    });

    RunCase("disabled plane fails closed", []()
    {
        uint64_t mask =
            UINT64_MAX &
            ~KindMask(LxrSideMetadataKind::ReferenceCountStraddle);
        Fixture fixture(1, mask);
        Expect("disabled plane rejected", !fixture.initialized);
    });

    RunCase("block lifecycle retains metadata", []()
    {
        Fixture fixture;
        ImmixBlockManager blocks;
        uintptr_t object = DataStart;
        constexpr uintptr_t Owner = 0x1234;
        Expect("fixture initialized", fixture.initialized);
        Expect(
            "block manager initialized",
            blocks.Initialize(&fixture.metadata) == SideMetadataResult::Success);
        Expect(
            "block range registered",
            blocks.RegisterBlockRange(
                DataStart,
                ImmixBlockGeometry::BlockBytes) ==
                SideMetadataResult::Success);
        Expect("object initialized", InitializeObject(fixture, object));
        Expect("object count set", SetCount(fixture, object, 2));
        ImmixBlockOperationStatus status;
        Expect(
            "block acquired",
            blocks.TryAcquire(
                DataStart,
                Owner,
                ImmixBlockAcquireKind::MutatorFresh,
                &status) == SideMetadataResult::Success);
        Expect(
            "block released",
            blocks.TryRelease(DataStart, Owner, &status) ==
                SideMetadataResult::Success);
        Expect("release retained count", LoadCount(fixture, object) == 2);
        for (size_t cycle = 0; cycle < 127; cycle++)
        {
            Expect(
                "start epoch cycle",
                blocks.StartGcPause(&status) == SideMetadataResult::Success);
            Expect(
                "release epoch cycle",
                blocks.ReleaseGcPause(&status) == SideMetadataResult::Success);
        }
        Expect(
            "release or epoch wrap cleared live RC metadata",
            LoadCount(fixture, object) == 2);
        Expect(
            "explicit block reset",
            fixture.table.ResetBlockQuiescent(DataStart) ==
                SideMetadataResult::Success);
        uintptr_t count;
        Expect(
            "block reset clears validity",
            fixture.table.Load(object, &count) ==
                SideMetadataResult::AddressNotOwned);
        blocks.Shutdown();
    });

    RunCase("zero-count invariant consumer", []()
    {
        Fixture fixture;
        uintptr_t object = DataStart;
        Expect("fixture initialized", fixture.initialized);
        Expect("object initialized", InitializeObject(fixture, object));
        s_invariantChecks.store(0, std::memory_order_relaxed);
        Expect("null old reference accepted", ValidateOldReference(fixture.table, 0));
        Expect(
            "zero old reference detected",
            !ValidateOldReference(fixture.table, object));
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        Expect(
            "increment old reference",
            fixture.table.Increment(object, &previous, &status) ==
                SideMetadataResult::Success);
        Expect(
            "nonzero old reference accepted",
            ValidateOldReference(fixture.table, object));
        Expect(
            "invariant execution count",
            s_invariantChecks.load(std::memory_order_relaxed) == 3);
    });

    RunCase("deterministic stale-word control loses neighbor", []()
    {
        Fixture fixture;
        uintptr_t first = DataStart;
        uintptr_t second = DataStart + 24;
        Expect("fixture initialized", fixture.initialized);
        Expect("first object initialized", InitializeObject(fixture, first));
        Expect("second object initialized", InitializeObject(fixture, second));

        SideMetadataLocation firstLocation;
        SideMetadataLocation secondLocation;
        Expect(
            "first control location",
            fixture.metadata.GetLocation(
                LxrSideMetadataKind::ReferenceCount,
                first,
                &firstLocation) == SideMetadataResult::Success);
        Expect(
            "second control location",
            fixture.metadata.GetLocation(
                LxrSideMetadataKind::ReferenceCount,
                second,
                &secondLocation) == SideMetadataResult::Success);
        Expect("control fields share word", firstLocation.word == secondLocation.word);

        uintptr_t staleWord = *firstLocation.word;
        Expect("first control count set", SetCount(fixture, first, 1));
        uintptr_t staleSecondWriter =
            (staleWord & ~secondLocation.mask) |
            (static_cast<uintptr_t>(1) << secondLocation.shift);
        *secondLocation.word = staleSecondWriter;
        Expect(
            "broken non-atomic control loses adjacent update",
            (LoadCount(fixture, first) == 0) &&
            (LoadCount(fixture, second) == 1));
    });
#endif

    int passed = s_cases - s_failedCases;
    printf(
        "%d/%d reference count cases passed (%d checks)\n",
        passed,
        s_cases,
        s_checks);
    return s_failedChecks == 0 ? 0 : 1;
}

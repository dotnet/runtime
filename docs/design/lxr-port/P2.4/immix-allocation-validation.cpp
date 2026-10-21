// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/immix_allocation.h"

#include <atomic>
#include <stdio.h>
#include <thread>

namespace
{
#ifdef HOST_64BIT
    constexpr uintptr_t DataStart = UINT64_C(0x0000000300000000);
    constexpr size_t DefaultBlockCount = 8;
    constexpr size_t DefaultDataSize =
        DefaultBlockCount * ImmixBlockGeometry::BlockBytes;
    constexpr uintptr_t OwnerOne = 0x2401;
    constexpr uintptr_t OwnerTwo = 0x2402;
#endif

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

    void ExpectMetadata(
        const char* name,
        SideMetadataResult actual,
        SideMetadataResult expected)
    {
        if (actual != expected)
        {
            printf(
                "DETAIL: %s actual=%u expected=%u\n",
                name,
                static_cast<unsigned>(actual),
                static_cast<unsigned>(expected));
        }
        Expect(name, actual == expected);
    }

#ifdef HOST_64BIT
    struct Fixture
    {
        LxrSideMetadataLayout layout;
        SideMetadataManager metadata;
        ImmixBlockManager blocks;
        ImmixAllocationManager allocation;
        uintptr_t data_start;
        size_t data_size;
        bool data_reserved;
        bool initialized;

        explicit Fixture(
            uint8_t logReferenceCountBits = 1,
            size_t blockCount = DefaultBlockCount,
            size_t minimumReuseLines = 2,
            uint64_t enabledSpecMask = UINT64_MAX)
            : data_start(DataStart)
            , data_size(blockCount * ImmixBlockGeometry::BlockBytes)
            , data_reserved(false)
            , initialized(false)
        {
            void* data = GCToOSInterface::VirtualReserveAt(
                reinterpret_cast<void*>(data_start),
                data_size,
                VirtualReserveFlags::None);
            if (data == reinterpret_cast<void*>(data_start))
            {
                data_reserved = true;
            }

            SideMetadataResult result = data_reserved
                ? LxrSideMetadataLayout::Create(
                    logReferenceCountBits,
                    &layout)
                : SideMetadataResult::ReservationFailed;
            if ((result == SideMetadataResult::Success) &&
                !GCToOSInterface::VirtualCommit(data, data_size))
            {
                result = SideMetadataResult::CommitFailed;
            }
            if (result == SideMetadataResult::Success)
            {
                result = metadata.Initialize(&layout, enabledSpecMask);
            }
            if (result == SideMetadataResult::Success)
            {
                result = blocks.Initialize(&metadata);
            }
            if (result == SideMetadataResult::Success)
            {
                result = blocks.RegisterBlockRange(data_start, data_size);
            }
            if (result == SideMetadataResult::Success)
            {
                result = allocation.Initialize(
                    &metadata,
                    &blocks,
                    data_start,
                    data_size,
                    minimumReuseLines);
            }
            initialized = result == SideMetadataResult::Success;
        }

        ~Fixture()
        {
            allocation.Shutdown();
            blocks.Shutdown();
            metadata.Shutdown();
            if (data_reserved)
            {
                GCToOSInterface::VirtualRelease(
                    reinterpret_cast<void*>(data_start),
                    data_size);
            }
        }
    };

    uint64_t KindMask(LxrSideMetadataKind kind)
    {
        return UINT64_C(1) << static_cast<uint8_t>(kind);
    }

    uintptr_t LineAddress(uintptr_t block, size_t line)
    {
        return block + (line * ImmixBlockGeometry::LineBytes);
    }

    void SeedAllLines(
        Fixture& fixture,
        uintptr_t block,
        uintptr_t value)
    {
        ExpectMetadata(
            "clear RC block",
            fixture.metadata.ResetRangeQuiescent(
                LxrSideMetadataKind::ReferenceCount,
                block,
                ImmixBlockGeometry::BlockBytes,
                0),
            SideMetadataResult::Success);
        if (value == 0)
        {
            return;
        }

        for (size_t line = 0;
             line < ImmixBlockGeometry::LinesPerBlock;
             line++)
        {
            ExpectMetadata(
                "seed live line",
                fixture.metadata.Store(
                    LxrSideMetadataKind::ReferenceCount,
                    LineAddress(block, line),
                    value,
                    SideMetadataMemoryOrder::Relaxed),
                SideMetadataResult::Success);
        }
    }

    void SeedLine(
        Fixture& fixture,
        uintptr_t block,
        size_t line,
        uintptr_t value)
    {
        ExpectMetadata(
            "seed RC line",
            fixture.metadata.Store(
                LxrSideMetadataKind::ReferenceCount,
                LineAddress(block, line),
                value,
                SideMetadataMemoryOrder::Relaxed),
            SideMetadataResult::Success);
    }

    void SeedReusableBlock(
        Fixture& fixture,
        uintptr_t block,
        uint8_t unavailableLines = 1)
    {
        ExpectMetadata(
            "seed reusable state",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockMark,
                block,
                unavailableLines,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed reusable owner",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockOwner,
                block,
                ImmixBlockManager::NoOwner,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed reusable lock",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockInUse,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed reusable phase",
            fixture.metadata.Store(
                LxrSideMetadataKind::PhaseEpoch,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed reusable defrag",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockDefrag,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
    }

    uintptr_t LoadMetadata(
        Fixture& fixture,
        LxrSideMetadataKind kind,
        uintptr_t address)
    {
        uintptr_t value = UINTPTR_MAX;
        ExpectMetadata(
            "load metadata",
            fixture.metadata.Load(
                kind,
                address,
                SideMetadataMemoryOrder::SequentiallyConsistent,
                &value),
            SideMetadataResult::Success);
        return value;
    }

    void ExpectObjectMarkRange(
        const char* name,
        Fixture& fixture,
        uintptr_t start,
        size_t size,
        uintptr_t expected)
    {
        const SideMetadataSpec* spec =
            fixture.layout.GetSpec(LxrSideMetadataKind::ObjectMark);
        Expect("object-mark spec exists", spec != nullptr);
        if (spec == nullptr)
        {
            return;
        }

        size_t stride = size_t{1} << spec->log_bytes_per_value;
        for (uintptr_t address = start;
             address < start + size;
             address += stride)
        {
            Expect(name, LoadMetadata(
                fixture,
                LxrSideMetadataKind::ObjectMark,
                address) == expected);
        }
    }

    void ExpectHole(
        const char* name,
        Fixture& fixture,
        uintptr_t block,
        size_t searchLine,
        size_t expectedFirstLine,
        size_t expectedEndLine,
        size_t expectedNextLine)
    {
        ImmixHole hole = {};
        size_t nextLine = SIZE_MAX;
        ImmixAllocationStatus status =
            ImmixAllocationStatus::InvalidRequest;
        SideMetadataResult result = fixture.allocation.TryFindNextHole(
            block,
            searchLine,
            &hole,
            &nextLine,
            &status);
        ExpectMetadata(name, result, SideMetadataResult::Success);
        Expect(name, status == ImmixAllocationStatus::Ready);
        Expect(
            name,
            hole.start == LineAddress(block, expectedFirstLine));
        Expect(
            name,
            hole.end == LineAddress(block, expectedEndLine));
        Expect(
            name,
            hole.line_count == (expectedEndLine - expectedFirstLine));
        Expect(name, nextLine == expectedNextLine);
    }

    void TestHoleDiscovery(uint8_t logReferenceCountBits)
    {
        Fixture fixture(logReferenceCountBits);
        Expect("hole fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        uintptr_t block = fixture.data_start;
        SeedAllLines(fixture, block, 0);
        ExpectHole(
            "all-dead starts at block",
            fixture,
            block,
            0,
            0,
            ImmixBlockGeometry::LinesPerBlock,
            ImmixBlockGeometry::LinesPerBlock);

        SeedAllLines(fixture, block, 1);
        SeedLine(fixture, block, 1, 0);
        SeedLine(fixture, block, 2, 0);
        SeedLine(fixture, block, 3, 0);
        SeedLine(fixture, block, 8, 0);
        SeedLine(fixture, block, 9, 0);
        SeedLine(fixture, block, 10, 0);
        ExpectHole(
            "interior exact run",
            fixture,
            block,
            0,
            2,
            4,
            4);

        SeedLine(fixture, block, 8, 0);
        SeedLine(fixture, block, 9, 0);
        SeedLine(fixture, block, 10, 0);
        ExpectHole(
            "second interior run",
            fixture,
            block,
            4,
            9,
            11,
            11);

        SeedAllLines(fixture, block, 1);
        SeedLine(fixture, block, 125, 0);
        SeedLine(fixture, block, 126, 0);
        SeedLine(fixture, block, 127, 0);
        ExpectHole(
            "trailing run",
            fixture,
            block,
            0,
            126,
            128,
            128);

        SeedAllLines(fixture, block, 1);
        SeedLine(fixture, block, 1, 0);
        SeedLine(fixture, block, 2, 0);
        ImmixHole hole = {};
        size_t nextLine = 0;
        ImmixAllocationStatus status =
            ImmixAllocationStatus::InvalidRequest;
        ExpectMetadata(
            "short run result",
            fixture.allocation.TryFindNextHole(
                block,
                0,
                &hole,
                &nextLine,
                &status),
            SideMetadataResult::Success);
        Expect("short run exhausted", status == ImmixAllocationStatus::Exhausted);
        Expect(
            "short run next",
            nextLine == ImmixBlockGeometry::LinesPerBlock);

        ExpectMetadata(
            "search past block rejected",
            fixture.allocation.TryFindNextHole(
                block,
                ImmixBlockGeometry::LinesPerBlock + 1,
                &hole,
                &nextLine,
                &status),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "unaligned block rejected",
            fixture.allocation.TryFindNextHole(
                block + 1,
                0,
                &hole,
                &nextLine,
                &status),
            SideMetadataResult::AddressNotOwned);
    }

    void TestFreshAllocation()
    {
        Fixture fixture;
        Expect("fresh fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        memset(
            reinterpret_cast<void*>(fixture.data_start),
            0x5a,
            fixture.data_size);
        ExpectMetadata(
            "seed fresh object marks",
            fixture.metadata.ResetRangeQuiescent(
                LxrSideMetadataKind::ObjectMark,
                fixture.data_start,
                ImmixBlockGeometry::BlockBytes,
                1),
            SideMetadataResult::Success);
        ImmixBumpAllocator allocator;
        ExpectMetadata(
            "fresh allocator initialize",
            allocator.Initialize(&fixture.allocation, OwnerOne),
            SideMetadataResult::Success);
        ImmixAllocationStatus status =
            ImmixAllocationStatus::InvalidRequest;
        ExpectMetadata(
            "fresh refill result",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorFresh,
                ImmixMarkingContext::Inactive,
                &status),
            SideMetadataResult::Success);
        Expect("fresh refill ready", status == ImmixAllocationStatus::Ready);
        Expect("fresh block selected", allocator.GetBlock() == fixture.data_start);

        uint8_t* block = reinterpret_cast<uint8_t*>(fixture.data_start);
        bool zero = true;
        for (size_t i = 0; i < ImmixBlockGeometry::BlockBytes; i++)
        {
            zero &= block[i] == 0;
        }
        Expect("fresh block zeroed", zero);
        Expect(
            "fresh field log initialized",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::FieldUnlogged,
                fixture.data_start) == 0);
        ExpectObjectMarkRange(
            "fresh object mark cleared",
            fixture,
            fixture.data_start,
            ImmixBlockGeometry::BlockBytes,
            0);

        ImmixAllocationRequest minimum = {
            MIN_OBJECT_SIZE,
            DATA_ALIGNMENT,
            0,
        };
        uintptr_t object = 0;
        ExpectMetadata(
            "minimum allocation result",
            allocator.TryAllocate(minimum, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "minimum allocation status",
            status == ImmixAllocationStatus::Allocated);
        Expect("minimum allocation address", object == fixture.data_start);

        ImmixAllocationRequest alignedWithOffset = {
            MIN_OBJECT_SIZE,
            32,
            8,
        };
        ExpectMetadata(
            "offset-aligned allocation result",
            allocator.TryAllocate(alignedWithOffset, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "offset-aligned allocation status",
            status == ImmixAllocationStatus::Allocated);
        Expect(
            "offset-aligned allocation address",
            ((object + alignedWithOffset.alignment_offset) &
             (alignedWithOffset.alignment - 1)) == 0);

        ImmixAllocationRequest aligned = {
            MIN_OBJECT_SIZE,
            32,
            0,
        };
        ExpectMetadata(
            "aligned allocation result",
            allocator.TryAllocate(aligned, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "aligned allocation status",
            status == ImmixAllocationStatus::Allocated);
        Expect("aligned allocation address", (object & 31) == 0);

        uintptr_t oldCursor = allocator.GetCursor();
        ImmixAllocationRequest tooSmall = {
            MIN_OBJECT_SIZE - 1,
            DATA_ALIGNMENT,
            0,
        };
        ExpectMetadata(
            "too-small allocation result",
            allocator.TryAllocate(tooSmall, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "too-small allocation rejected",
            status == ImmixAllocationStatus::InvalidRequest);
        Expect(
            "too-small cursor unchanged",
            allocator.GetCursor() == oldCursor);

        ImmixAllocationRequest tooLarge = {
            ImmixBlockGeometry::BlockBytes,
            DATA_ALIGNMENT,
            0,
        };
        ExpectMetadata(
            "block-size allocation result",
            allocator.TryAllocate(tooLarge, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "block-size allocation rejected",
            status == ImmixAllocationStatus::InvalidRequest);
        Expect(
            "block-size cursor unchanged",
            allocator.GetCursor() == oldCursor);

        ImmixAllocationRequest badAlignment = {
            MIN_OBJECT_SIZE,
            12,
            0,
        };
        ExpectMetadata(
            "bad alignment result",
            allocator.TryAllocate(badAlignment, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "bad alignment rejected",
            status == ImmixAllocationStatus::InvalidRequest);
        Expect(
            "bad alignment cursor unchanged",
            allocator.GetCursor() == oldCursor);

        ImmixAllocationRequest badAlignmentOffset = {
            MIN_OBJECT_SIZE,
            16,
            16,
        };
        ExpectMetadata(
            "bad alignment offset result",
            allocator.TryAllocate(badAlignmentOffset, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "bad alignment offset rejected",
            status == ImmixAllocationStatus::InvalidRequest);
        Expect(
            "bad alignment offset cursor unchanged",
            allocator.GetCursor() == oldCursor);

        ExpectMetadata(
            "fresh reset result",
            allocator.Reset(&status),
            SideMetadataResult::Success);
        Expect("fresh reset ready", status == ImmixAllocationStatus::Ready);
        Expect("fresh reset clears block", allocator.GetBlock() == 0);
        Expect(
            "fresh accounting",
            fixture.allocation.GetFreshBlocksAcquired() == 1);
    }

    void TestRecycledAllocation(ImmixMarkingContext markingContext)
    {
        Fixture fixture;
        Expect("recycled fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        uintptr_t block = fixture.data_start;
        SeedReusableBlock(fixture, block);
        SeedAllLines(fixture, block, 1);
        SeedLine(fixture, block, 1, 0);
        SeedLine(fixture, block, 2, 0);
        SeedLine(fixture, block, 3, 0);
        SeedLine(fixture, block, 8, 0);
        SeedLine(fixture, block, 9, 0);
        SeedLine(fixture, block, 10, 0);
        memset(
            reinterpret_cast<void*>(block),
            0x6b,
            ImmixBlockGeometry::BlockBytes);
        ExpectMetadata(
            "seed recycled object marks",
            fixture.metadata.ResetRangeQuiescent(
                LxrSideMetadataKind::ObjectMark,
                block,
                ImmixBlockGeometry::BlockBytes,
                markingContext == ImmixMarkingContext::Inactive ? 1 : 0),
            SideMetadataResult::Success);
        for (size_t line = 2; line <= 3; line++)
        {
            uintptr_t lineAddress = LineAddress(block, line);
            ExpectMetadata(
                "seed line reuse",
                fixture.metadata.Store(
                    LxrSideMetadataKind::LineReuse,
                    lineAddress,
                    line == 2 ? UINT8_MAX : 9,
                    SideMetadataMemoryOrder::SequentiallyConsistent),
                SideMetadataResult::Success);
        }

        ImmixBumpAllocator allocator;
        ExpectMetadata(
            "recycled allocator initialize",
            allocator.Initialize(&fixture.allocation, OwnerOne),
            SideMetadataResult::Success);
        ImmixAllocationStatus status =
            ImmixAllocationStatus::InvalidRequest;
        ExpectMetadata(
            "recycled refill result",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorRecycled,
                markingContext,
                &status),
            SideMetadataResult::Success);
        Expect(
            "recycled refill ready",
            status == ImmixAllocationStatus::Ready);
        Expect(
            "recycled cursor start",
            allocator.GetCursor() == LineAddress(block, 2));
        Expect(
            "recycled cursor limit",
            allocator.GetLimit() == LineAddress(block, 4));

        uint8_t* bytes = reinterpret_cast<uint8_t*>(block);
        bool holeZero = true;
        for (size_t i = 2 * ImmixBlockGeometry::LineBytes;
             i < 4 * ImmixBlockGeometry::LineBytes;
             i++)
        {
            holeZero &= bytes[i] == 0;
        }
        Expect("recycled hole zeroed", holeZero);
        Expect(
            "recycled live prefix preserved",
            bytes[ImmixBlockGeometry::LineBytes] == 0x6b);
        Expect(
            "recycled live suffix preserved",
            bytes[4 * ImmixBlockGeometry::LineBytes] == 0x6b);

        uintptr_t expectedFirstReuse =
            markingContext == ImmixMarkingContext::ConcurrentOrFinalMark
                ? 0
                : UINT8_MAX;
        uintptr_t expectedSecondReuse =
            markingContext == ImmixMarkingContext::ConcurrentOrFinalMark
                ? 10
                : 9;
        ExpectObjectMarkRange(
            "recycled object mark",
            fixture,
            LineAddress(block, 2),
            2 * ImmixBlockGeometry::LineBytes,
            1);
        uintptr_t adjacentObjectMark =
            markingContext == ImmixMarkingContext::Inactive ? 1 : 0;
        Expect(
            "recycled object-mark prefix preserved",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::ObjectMark,
                LineAddress(block, 1)) == adjacentObjectMark);
        Expect(
            "recycled object-mark suffix preserved",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::ObjectMark,
                LineAddress(block, 4)) == adjacentObjectMark);
        Expect(
            "recycled first reuse value",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::LineReuse,
                LineAddress(block, 2)) == expectedFirstReuse);
        Expect(
            "recycled second reuse value",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::LineReuse,
                LineAddress(block, 3)) == expectedSecondReuse);

        Expect(
            "recycled line accounting",
            fixture.allocation.GetRecycledLinesConsumed() ==
                2);
        Expect(
            "recycled block accounting",
            fixture.allocation.GetRecycledBlocksAcquired() == 1);

        ImmixAllocationRequest exactHole = {
            2 * ImmixBlockGeometry::LineBytes,
            DATA_ALIGNMENT,
            0,
        };
        uintptr_t object = 0;
        ExpectMetadata(
            "exact-hole allocation result",
            allocator.TryAllocate(exactHole, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "exact-hole allocation status",
            status == ImmixAllocationStatus::Allocated);
        Expect(
            "exact-hole allocation address",
            object == LineAddress(block, 2));

        uintptr_t oldCursor = allocator.GetCursor();
        ImmixAllocationRequest overflowHole = {
            MIN_OBJECT_SIZE,
            DATA_ALIGNMENT,
            0,
        };
        ExpectMetadata(
            "hole overflow result",
            allocator.TryAllocate(overflowHole, &object, &status),
            SideMetadataResult::Success);
        Expect(
            "hole overflow exhausted",
            status == ImmixAllocationStatus::Exhausted);
        Expect(
            "hole overflow cursor unchanged",
            allocator.GetCursor() == oldCursor);
        ExpectMetadata(
            "next-hole refill result",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorRecycled,
                markingContext,
                &status),
            SideMetadataResult::Success);
        Expect(
            "next-hole refill ready",
            status == ImmixAllocationStatus::Ready);
        Expect(
            "next-hole retains block",
            allocator.GetBlock() == block);
        Expect(
            "next-hole cursor start",
            allocator.GetCursor() == LineAddress(block, 9));
        Expect(
            "next-hole cursor limit",
            allocator.GetLimit() == LineAddress(block, 11));
        Expect(
            "next-hole line accounting",
            fixture.allocation.GetRecycledLinesConsumed() == 4);
        ExpectMetadata(
            "source-switch refill result",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorFresh,
                markingContext,
                &status),
            SideMetadataResult::Success);
        Expect(
            "source-switch refill ready",
            status == ImmixAllocationStatus::Ready);
        Expect(
            "source-switch selects fresh block",
            allocator.GetBlock() ==
                (block + ImmixBlockGeometry::BlockBytes));
        ExpectObjectMarkRange(
            "source-switch fresh object mark",
            fixture,
            block + ImmixBlockGeometry::BlockBytes,
            ImmixBlockGeometry::BlockBytes,
            markingContext == ImmixMarkingContext::Inactive ? 0 : 1);
        Expect(
            "source-switch returns recycled owner",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::BlockOwner,
                block) == ImmixBlockManager::NoOwner);
        ExpectMetadata(
            "source-switch reset result",
            allocator.Reset(&status),
            SideMetadataResult::Success);
    }

    void TestGcCopyRecycledAllocation()
    {
        Fixture fixture(1, 2, 2);
        Expect("copy recycled fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        uintptr_t block = fixture.data_start;
        SeedReusableBlock(fixture, block);
        SeedAllLines(fixture, block, 1);
        SeedLine(fixture, block, 1, 0);
        SeedLine(fixture, block, 2, 0);
        SeedLine(fixture, block, 3, 0);
        memset(
            reinterpret_cast<void*>(block),
            0x6b,
            ImmixBlockGeometry::BlockBytes);
        ExpectMetadata(
            "clear copy recycled object marks",
            fixture.metadata.ResetRangeQuiescent(
                LxrSideMetadataKind::ObjectMark,
                block,
                ImmixBlockGeometry::BlockBytes,
                0),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed copy recycled reuse",
            fixture.metadata.Store(
                LxrSideMetadataKind::LineReuse,
                LineAddress(block, 2),
                9,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);

        ImmixBlockOperationStatus phaseStatus;
        ExpectMetadata(
            "start copy recycled GC phase",
            fixture.blocks.StartGcPause(&phaseStatus),
            SideMetadataResult::Success);
        Expect(
            "copy recycled GC phase updated",
            phaseStatus == ImmixBlockOperationStatus::Updated);

        ImmixBumpAllocator allocator;
        ExpectMetadata(
            "copy recycled allocator initialize",
            allocator.Initialize(&fixture.allocation, OwnerOne),
            SideMetadataResult::Success);
        ImmixAllocationStatus status =
            ImmixAllocationStatus::InvalidRequest;
        ExpectMetadata(
            "copy recycled refill result",
            allocator.TryRefill(
                ImmixAllocationSource::GcCopyRecycled,
                ImmixMarkingContext::ConcurrentOrFinalMark,
                &status),
            SideMetadataResult::Success);
        Expect(
            "copy recycled refill ready",
            status == ImmixAllocationStatus::Ready);
        Expect(
            "copy recycled cursor start",
            allocator.GetCursor() == LineAddress(block, 2));
        Expect(
            "copy recycled bytes preserved",
            reinterpret_cast<uint8_t*>(block)[
                2 * ImmixBlockGeometry::LineBytes] == 0x6b);
        Expect(
            "copy recycled field log initialized",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::FieldUnlogged,
                LineAddress(block, 2)) == 1);
        ExpectObjectMarkRange(
            "copy recycled object mark",
            fixture,
            LineAddress(block, 2),
            2 * ImmixBlockGeometry::LineBytes,
            1);
        Expect(
            "copy recycled object-mark prefix preserved",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::ObjectMark,
                LineAddress(block, 1)) == 0);
        Expect(
            "copy recycled object-mark suffix preserved",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::ObjectMark,
                LineAddress(block, 4)) == 0);
        Expect(
            "copy recycled reuse incremented",
            LoadMetadata(
                fixture,
                LxrSideMetadataKind::LineReuse,
                LineAddress(block, 2)) == 10);
        Expect(
            "copy recycled accounting excluded",
            fixture.allocation.GetRecycledLinesConsumed() == 0);

        ExpectMetadata(
            "copy recycled reset",
            allocator.Reset(&status),
            SideMetadataResult::Success);
        ExpectMetadata(
            "release copy recycled GC phase",
            fixture.blocks.ReleaseGcPause(&phaseStatus),
            SideMetadataResult::Success);
    }

    void TestPhaseAndContention()
    {
        {
            Fixture fixture;
            Expect("phase fixture initialized", fixture.initialized);
            if (fixture.initialized)
            {
                ImmixBumpAllocator allocator;
                ExpectMetadata(
                    "phase allocator initialize",
                    allocator.Initialize(&fixture.allocation, OwnerOne),
                    SideMetadataResult::Success);
                ImmixAllocationStatus status =
                    ImmixAllocationStatus::InvalidRequest;
                ExpectMetadata(
                    "GC fresh wrong phase result",
                    allocator.TryRefill(
                        ImmixAllocationSource::GcCopyFresh,
                        ImmixMarkingContext::ConcurrentOrFinalMark,
                        &status),
                    SideMetadataResult::Success);
                Expect(
                    "GC fresh wrong phase",
                    status == ImmixAllocationStatus::InvalidPhase);
                ImmixBlockOperationStatus phaseStatus;
                ExpectMetadata(
                    "start GC phase after rejection",
                    fixture.blocks.StartGcPause(&phaseStatus),
                    SideMetadataResult::Success);
                Expect(
                    "start GC phase updated",
                    phaseStatus == ImmixBlockOperationStatus::Updated);
                ExpectMetadata(
                    "GC fresh retry result",
                    allocator.TryRefill(
                        ImmixAllocationSource::GcCopyFresh,
                        ImmixMarkingContext::ConcurrentOrFinalMark,
                        &status),
                    SideMetadataResult::Success);
                Expect(
                    "GC fresh retry ready",
                    status == ImmixAllocationStatus::Ready);
                Expect(
                    "GC phase rejection preserves candidate",
                    allocator.GetBlock() == fixture.data_start);
                ExpectMetadata(
                    "GC fresh retry reset",
                    allocator.Reset(&status),
                    SideMetadataResult::Success);
                ExpectMetadata(
                    "release GC phase after retry",
                    fixture.blocks.ReleaseGcPause(&phaseStatus),
                    SideMetadataResult::Success);
            }
        }

        {
            Fixture fixture(1, 1);
            Expect("contention fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }
            SeedReusableBlock(fixture, fixture.data_start);
            SeedAllLines(fixture, fixture.data_start, 0);

            ImmixBumpAllocator first;
            ImmixBumpAllocator second;
            ExpectMetadata(
                "first contender initialize",
                first.Initialize(&fixture.allocation, OwnerOne),
                SideMetadataResult::Success);
            ExpectMetadata(
                "second contender initialize",
                second.Initialize(&fixture.allocation, OwnerTwo),
                SideMetadataResult::Success);

            std::atomic<bool> start(false);
            ImmixAllocationStatus firstStatus =
                ImmixAllocationStatus::InvalidRequest;
            ImmixAllocationStatus secondStatus =
                ImmixAllocationStatus::InvalidRequest;
            SideMetadataResult firstResult =
                SideMetadataResult::InvalidArgument;
            SideMetadataResult secondResult =
                SideMetadataResult::InvalidArgument;
            std::thread firstThread([&]()
            {
                while (!start.load(std::memory_order_acquire))
                {
                    YieldProcessor();
                }
                firstResult = first.TryRefill(
                    ImmixAllocationSource::MutatorRecycled,
                    ImmixMarkingContext::Inactive,
                    &firstStatus);
            });
            std::thread secondThread([&]()
            {
                while (!start.load(std::memory_order_acquire))
                {
                    YieldProcessor();
                }
                secondResult = second.TryRefill(
                    ImmixAllocationSource::MutatorRecycled,
                    ImmixMarkingContext::Inactive,
                    &secondStatus);
            });
            start.store(true, std::memory_order_release);
            firstThread.join();
            secondThread.join();

            ExpectMetadata(
                "first contention result",
                firstResult,
                SideMetadataResult::Success);
            ExpectMetadata(
                "second contention result",
                secondResult,
                SideMetadataResult::Success);
            int readyCount =
                (firstStatus == ImmixAllocationStatus::Ready ? 1 : 0) +
                (secondStatus == ImmixAllocationStatus::Ready ? 1 : 0);
            Expect("one contention winner", readyCount == 1);

            ImmixAllocationStatus resetStatus;
            if (firstStatus == ImmixAllocationStatus::Ready)
            {
                ExpectMetadata(
                    "first contention reset",
                    first.Reset(&resetStatus),
                    SideMetadataResult::Success);
            }
            if (secondStatus == ImmixAllocationStatus::Ready)
            {
                ExpectMetadata(
                    "second contention reset",
                    second.Reset(&resetStatus),
                    SideMetadataResult::Success);
            }
        }
    }

    void TestFreshReclamation()
    {
        Fixture fixture(1, 1, 1);
        Expect("reclamation fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        ImmixBumpAllocator allocator;
        ExpectMetadata(
            "reclamation allocator initialize",
            allocator.Initialize(&fixture.allocation, OwnerOne),
            SideMetadataResult::Success);
        ImmixAllocationStatus status =
            ImmixAllocationStatus::InvalidRequest;
        ExpectMetadata(
            "initial fresh refill",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorFresh,
                ImmixMarkingContext::Inactive,
                &status),
            SideMetadataResult::Success);
        Expect(
            "initial fresh refill ready",
            status == ImmixAllocationStatus::Ready);
        ExpectMetadata(
            "return nursery block",
            allocator.Reset(&status),
            SideMetadataResult::Success);
        ExpectMetadata(
            "same-phase fresh retry",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorFresh,
                ImmixMarkingContext::Inactive,
                &status),
            SideMetadataResult::Success);
        Expect(
            "same-phase nursery is not clean",
            status == ImmixAllocationStatus::Exhausted);

        ImmixBlockOperationStatus blockStatus;
        ExpectMetadata(
            "reclamation start GC",
            fixture.blocks.StartGcPause(&blockStatus),
            SideMetadataResult::Success);
        Expect(
            "reclamation start GC updated",
            blockStatus == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "GC acquires nursery for release",
            fixture.blocks.TryAcquire(
                fixture.data_start,
                OwnerTwo,
                ImmixBlockAcquireKind::GcMatureEvacuationFresh,
                &blockStatus),
            SideMetadataResult::Success);
        Expect(
            "GC nursery acquisition updated",
            blockStatus == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "GC releases empty block",
            fixture.blocks.TryRelease(
                fixture.data_start,
                OwnerTwo,
                &blockStatus),
            SideMetadataResult::Success);
        Expect(
            "GC empty release updated",
            blockStatus == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "reclamation release GC",
            fixture.blocks.ReleaseGcPause(&blockStatus),
            SideMetadataResult::Success);
        Expect(
            "reclamation release GC updated",
            blockStatus == ImmixBlockOperationStatus::Updated);

        ExpectMetadata(
            "reclaimed fresh refill",
            allocator.TryRefill(
                ImmixAllocationSource::MutatorFresh,
                ImmixMarkingContext::Inactive,
                &status),
            SideMetadataResult::Success);
        Expect(
            "reclaimed fresh refill ready",
            status == ImmixAllocationStatus::Ready);
        Expect(
            "reclaimed block selected",
            allocator.GetBlock() == fixture.data_start);
        ExpectMetadata(
            "reclaimed fresh reset",
            allocator.Reset(&status),
            SideMetadataResult::Success);
    }

    void TestRejectedReusableBlocks()
    {
        {
            Fixture fixture(1, 3, 3);
            Expect("rejected-block fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }

            uintptr_t prefiltered = fixture.data_start;
            uintptr_t scanned =
                fixture.data_start + ImmixBlockGeometry::BlockBytes;
            SeedReusableBlock(
                fixture,
                prefiltered,
                static_cast<uint8_t>(
                    ImmixBlockGeometry::LinesPerBlock - 1));
            SeedAllLines(fixture, prefiltered, 1);

            SeedReusableBlock(fixture, scanned);
            SeedAllLines(fixture, scanned, 1);
            SeedLine(fixture, scanned, 1, 0);
            SeedLine(fixture, scanned, 2, 0);

            ImmixBumpAllocator allocator;
            ExpectMetadata(
                "rejected-block allocator initialize",
                allocator.Initialize(&fixture.allocation, OwnerOne),
                SideMetadataResult::Success);
            ImmixAllocationStatus status =
                ImmixAllocationStatus::InvalidRequest;
            ExpectMetadata(
                "rejected-block refill result",
                allocator.TryRefill(
                    ImmixAllocationSource::MutatorRecycled,
                    ImmixMarkingContext::Inactive,
                    &status),
                SideMetadataResult::Success);
            Expect(
                "rejected-block refill exhausted",
                status == ImmixAllocationStatus::Exhausted);
            Expect(
                "prefiltered block remains unowned",
                LoadMetadata(
                    fixture,
                    LxrSideMetadataKind::BlockOwner,
                    prefiltered) == ImmixBlockManager::NoOwner);
            Expect(
                "scanned block owner returned",
                LoadMetadata(
                    fixture,
                    LxrSideMetadataKind::BlockOwner,
                    scanned) == ImmixBlockManager::NoOwner);
            Expect(
                "only scanned candidate acquired",
                fixture.allocation.GetRecycledBlocksAcquired() == 1);
        }

        {
            Fixture fixture(1, 2, 2);
            Expect("defrag fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }

            uintptr_t defrag = fixture.data_start;
            uintptr_t reusable =
                fixture.data_start + ImmixBlockGeometry::BlockBytes;
            SeedReusableBlock(fixture, defrag);
            SeedAllLines(fixture, defrag, 0);
            ExpectMetadata(
                "seed defrag source",
                fixture.metadata.Store(
                    LxrSideMetadataKind::BlockDefrag,
                    defrag,
                    1,
                    SideMetadataMemoryOrder::SequentiallyConsistent),
                SideMetadataResult::Success);
            SeedReusableBlock(fixture, reusable);
            SeedAllLines(fixture, reusable, 0);

            ImmixBumpAllocator allocator;
            ExpectMetadata(
                "defrag allocator initialize",
                allocator.Initialize(&fixture.allocation, OwnerOne),
                SideMetadataResult::Success);
            ImmixAllocationStatus status =
                ImmixAllocationStatus::InvalidRequest;
            ExpectMetadata(
                "defrag skip refill result",
                allocator.TryRefill(
                    ImmixAllocationSource::MutatorRecycled,
                    ImmixMarkingContext::Inactive,
                    &status),
                SideMetadataResult::Success);
            Expect(
                "defrag skip refill ready",
                status == ImmixAllocationStatus::Ready);
            Expect(
                "defrag candidate skipped",
                allocator.GetBlock() == reusable);
            Expect(
                "defrag candidate unowned",
                LoadMetadata(
                    fixture,
                    LxrSideMetadataKind::BlockOwner,
                    defrag) == ImmixBlockManager::NoOwner);
            ExpectMetadata(
                "defrag skip reset",
                allocator.Reset(&status),
                SideMetadataResult::Success);
        }
    }

    void TestConfigurationFailures()
    {
        {
            LxrSideMetadataLayout layout;
            SideMetadataManager metadata;
            ImmixBlockManager blocks;
            ImmixAllocationManager allocation;
            uint64_t mask =
                ((UINT64_C(1) <<
                  static_cast<uint8_t>(LxrSideMetadataKind::Count)) - 1) &
                ~KindMask(LxrSideMetadataKind::ObjectMark);
            ExpectMetadata(
                "missing object-mark layout",
                LxrSideMetadataLayout::Create(1, &layout),
                SideMetadataResult::Success);
            ExpectMetadata(
                "missing object-mark metadata",
                metadata.Initialize(&layout, mask),
                SideMetadataResult::Success);
            ExpectMetadata(
                "missing object-mark blocks",
                blocks.Initialize(&metadata),
                SideMetadataResult::Success);
            ExpectMetadata(
                "missing object-mark allocator",
                allocation.Initialize(
                    &metadata,
                    &blocks,
                    DataStart,
                    ImmixBlockGeometry::BlockBytes,
                    2),
                SideMetadataResult::InvalidArgument);
            blocks.Shutdown();
            metadata.Shutdown();
        }

        Fixture fixture;
        Expect("configuration fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        ImmixAllocationManager allocation;
        ExpectMetadata(
            "zero minimum reuse rejected",
            allocation.Initialize(
                &fixture.metadata,
                &fixture.blocks,
                fixture.data_start,
                fixture.data_size,
                0),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "over-block minimum reuse rejected",
            allocation.Initialize(
                &fixture.metadata,
                &fixture.blocks,
                fixture.data_start,
                fixture.data_size,
                ImmixBlockGeometry::LinesPerBlock + 1),
            SideMetadataResult::InvalidArgument);

        ImmixBumpAllocator allocator;
        ExpectMetadata(
            "zero owner rejected",
            allocator.Initialize(
                &fixture.allocation,
                ImmixBlockManager::NoOwner),
            SideMetadataResult::InvalidArgument);
    }
#endif
}

int main()
{
#ifndef HOST_64BIT
    LxrSideMetadataLayout layout;
    ExpectMetadata(
        "32-bit fails closed",
        LxrSideMetadataLayout::Create(1, &layout),
        SideMetadataResult::UnsupportedAddressSpace);
#else
    TestHoleDiscovery(1);
    TestHoleDiscovery(2);
    TestHoleDiscovery(3);
    TestFreshAllocation();
    TestRecycledAllocation(ImmixMarkingContext::Inactive);
    TestRecycledAllocation(
        ImmixMarkingContext::ConcurrentOrFinalMark);
    TestGcCopyRecycledAllocation();
    TestPhaseAndContention();
    TestFreshReclamation();
    TestRejectedReusableBlocks();
    TestConfigurationFailures();
#endif

    if (s_failures != 0)
    {
        printf(
            "%d/%d immix allocation checks failed\n",
            s_failures,
            s_checks);
        return 1;
    }

    printf(
        "%d/%d immix allocation checks passed\n",
        s_checks,
        s_checks);
    return 0;
}

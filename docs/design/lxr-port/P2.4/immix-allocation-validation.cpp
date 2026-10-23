// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/immix_allocation.h"
#define LXR_F10_PREFETCH_TEST_HOOK
#include "../../../../src/coreclr/gc/lxr_prefetch.h"

#include <atomic>
#include <stdio.h>
#include <thread>
#include <vector>

namespace
{
#ifdef HOST_64BIT
    constexpr uintptr_t DataStart = UINT64_C(0x0000000300000000);
    constexpr size_t DefaultBlockCount = 8;
    constexpr size_t DefaultDataSize =
        DefaultBlockCount * ImmixBlockGeometry::BlockBytes;
    constexpr uintptr_t OwnerOne = 0x2401;
    constexpr uintptr_t OwnerTwo = 0x2402;
    constexpr uintptr_t OwnerThree = 0x2403;
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

    // ---- P28 (F9): the hole search and the refill as the Java LXR does them ----

    struct F9Hole
    {
        size_t first;
        size_t end;
    };

    // The search of the Java LXR (immixspace.rs: rc_get_next_available_lines), written again from its source over a
    // table of live lines: the whole block is known, and the holes are listed in one pass. The first dead line after a live
    // line is skipped (an object may end in it), a run shorter than the minimum is skipped, and the next search starts at
    // the end of the hole.
    std::vector<F9Hole> F9JavaHoles(const std::vector<bool>& live, size_t minimumLines)
    {
        std::vector<F9Hole> holes;
        size_t search = 0;
        const size_t limit = live.size();
        while (search < limit)
        {
            size_t first = limit;
            bool findFreeLine = false;
            for (size_t i = search; i < limit; i++)
            {
                if (!live[i])
                {
                    if (i == 0)
                    {
                        first = i;
                        break;
                    }
                    else if (!findFreeLine)
                    {
                        findFreeLine = true;
                    }
                    else
                    {
                        first = i;
                        break;
                    }
                }
                else
                {
                    findFreeLine = false;
                }
            }
            if (first == limit)
            {
                break;
            }
            size_t end = first + 1;
            while (end < limit && !live[end])
            {
                end++;
            }
            if (end - first >= minimumLines)
            {
                holes.push_back({first, end});
            }
            search = end;
        }
        return holes;
    }

    uint32_t F9Next(uint64_t* state)
    {
        *state = (*state * UINT64_C(6364136223846793005)) + UINT64_C(1442695040888963407);
        return static_cast<uint32_t>(*state >> 33);
    }

    // Puts a count in one slot of a line, not always the first slot, so that every word of the counts of the line is used.
    void F9SeedLiveLine(Fixture& fixture, uintptr_t block, size_t line, uint8_t logBits, uint64_t* state)
    {
        uintptr_t maximum = (static_cast<uintptr_t>(1) << (1u << logBits)) - 1;
        uintptr_t value = F9Next(state) % 4 == 0 ? maximum : 1 + (F9Next(state) % maximum);
        size_t slot = F9Next(state) % (ImmixBlockGeometry::LineBytes / sizeof(uintptr_t));
        ExpectMetadata(
            "f9 seed live slot",
            fixture.metadata.Store(
                LxrSideMetadataKind::ReferenceCount,
                LineAddress(block, line) + (slot * sizeof(uintptr_t)),
                value,
                SideMetadataMemoryOrder::Relaxed),
            SideMetadataResult::Success);
    }

    // The holes that the lazy search returns one by one are the holes that the eager search of the Java LXR lists, for counts
    // of 2, 4 and 8 bits, for every minimum run length, and for random line patterns, with live lines of every density.
    void TestF9ScanEquivalence()
    {
        constexpr double Densities[] = {0.0, 0.05, 0.3, 0.6, 0.9, 1.0};
        uint64_t state = 28;
        for (uint8_t logBits = 1; logBits <= 3; logBits++)
        {
            for (size_t minimumLines : {1, 2, 3, 7})
            {
                Fixture fixture(logBits, 4, minimumLines);
                Expect("f9 scan fixture initialized", fixture.initialized);
                if (!fixture.initialized)
                {
                    return;
                }
                size_t mismatches = 0;
                size_t compared = 0;
                for (size_t round = 0; round < 40; round++)
                {
                    for (size_t index = 0; index < 4; index++)
                    {
                        uintptr_t block = fixture.data_start + (index * ImmixBlockGeometry::BlockBytes);
                        double density = Densities[(round + index) % 6];
                        std::vector<bool> live(ImmixBlockGeometry::LinesPerBlock, false);
                        SeedAllLines(fixture, block, 0);
                        for (size_t line = 0; line < live.size(); line++)
                        {
                            live[line] = (F9Next(&state) % 1000) < static_cast<uint32_t>(density * 1000);
                            if (live[line])
                            {
                                F9SeedLiveLine(fixture, block, line, logBits, &state);
                            }
                        }
                        std::vector<F9Hole> expected = F9JavaHoles(live, minimumLines);
                        std::vector<F9Hole> actual;
                        size_t search = 0;
                        for (size_t guard = 0; guard <= ImmixBlockGeometry::LinesPerBlock; guard++)
                        {
                            ImmixHole hole = {};
                            size_t next = 0;
                            ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
                            SideMetadataResult result =
                                fixture.allocation.TryFindNextHole(block, search, &hole, &next, &status);
                            if (result != SideMetadataResult::Success || status != ImmixAllocationStatus::Ready)
                            {
                                break;
                            }
                            actual.push_back({
                                static_cast<size_t>(hole.start - block) / ImmixBlockGeometry::LineBytes,
                                static_cast<size_t>(hole.end - block) / ImmixBlockGeometry::LineBytes});
                            if (next != actual.back().end)
                            {
                                mismatches++;
                            }
                            search = next;
                        }
                        compared++;
                        bool same = actual.size() == expected.size();
                        for (size_t i = 0; same && i < actual.size(); i++)
                        {
                            same = actual[i].first == expected[i].first && actual[i].end == expected[i].end;
                        }
                        mismatches += same ? 0 : 1;
                    }
                }
                Expect("f9 the lazy hole search returns the holes of the eager search of the Java LXR",
                    mismatches == 0 && compared == 160);
            }
        }
    }

    // The search reads only the lines that one buffer needs: from the search line to the first live line after the hole.
    void TestF9LazyScan()
    {
        uint64_t state = 7;
        for (uint8_t logBits = 1; logBits <= 3; logBits++)
        {
            Fixture fixture(logBits, 2, 1);
            Expect("f9 lazy fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }
            size_t wrong = 0;
            size_t checked = 0;
            for (size_t round = 0; round < 60; round++)
            {
                uintptr_t block = fixture.data_start;
                std::vector<bool> live(ImmixBlockGeometry::LinesPerBlock, false);
                SeedAllLines(fixture, block, 0);
                for (size_t line = 0; line < live.size(); line++)
                {
                    live[line] = F9Next(&state) % 100 < 40;
                    if (live[line])
                    {
                        F9SeedLiveLine(fixture, block, line, logBits, &state);
                    }
                }
                size_t search = 0;
                while (search < ImmixBlockGeometry::LinesPerBlock)
                {
                    ImmixHole hole = {};
                    size_t next = 0;
                    ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
                    uint64_t before = fixture.allocation.GetLineProbesForTest();
                    SideMetadataResult result = fixture.allocation.TryFindNextHole(block, search, &hole, &next, &status);
                    uint64_t probes = fixture.allocation.GetLineProbesForTest() - before;
                    // The last line that is read: the line after the hole when the hole ends before the block end, else
                    // the last line of the block.
                    size_t lastRead = ImmixBlockGeometry::LinesPerBlock - 1;
                    if (status == ImmixAllocationStatus::Ready)
                    {
                        size_t end = static_cast<size_t>(hole.end - block) / ImmixBlockGeometry::LineBytes;
                        lastRead = end < ImmixBlockGeometry::LinesPerBlock ? end : ImmixBlockGeometry::LinesPerBlock - 1;
                    }
                    checked++;
                    wrong += (result != SideMetadataResult::Success || probes != (lastRead - search + 1)) ? 1 : 0;
                    if (status != ImmixAllocationStatus::Ready)
                    {
                        break;
                    }
                    search = next;
                }
            }
            Expect("f9 one search reads only the lines from the search line to the end of the hole", wrong == 0 && checked != 0);
        }
    }

    // The refill clears only what the Java LXR clears at a refill (the unlog table, and the mark table when a trace runs),
    // plus the ValidObject plane (forced by .NET). It does not touch the count plane and the straddle plane: they are zero
    // in a free range, and a range that is not zero is reported.
    void TestF9RefillSchedule(uint8_t logBits)
    {
        Fixture fixture(logBits, 6, 1);
        Expect("f9 refill fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }
        size_t arenaBytes = 6 * ImmixBlockGeometry::BlockBytes;
        for (LxrSideMetadataKind kind : {LxrSideMetadataKind::ValidObject, LxrSideMetadataKind::FieldUnlogged,
                 LxrSideMetadataKind::ObjectMark})
        {
            ExpectMetadata("f9 poison plane",
                fixture.metadata.FillRangeBytes(kind, fixture.data_start, arenaBytes, 0xff), SideMetadataResult::Success);
        }
        for (size_t block = 0; block < 6; block++)
        {
            ExpectMetadata("f9 preset reuse count",
                fixture.metadata.Store(LxrSideMetadataKind::LineReuse,
                    LineAddress(fixture.data_start + (block * ImmixBlockGeometry::BlockBytes), 5), UINT8_MAX,
                    SideMetadataMemoryOrder::SequentiallyConsistent),
                SideMetadataResult::Success);
        }
        auto planeIs = [&](LxrSideMetadataKind kind, uintptr_t start, size_t size, uintptr_t expected)
        {
            for (uintptr_t address = start; address < start + size; address += sizeof(uintptr_t))
            {
                if (LoadMetadata(fixture, kind, address) != expected)
                {
                    return false;
                }
            }
            return true;
        };
        auto countsAndStraddleZero = [&](uintptr_t start, size_t size)
        {
            bool zero = false;
            bool straddleZero = false;
            return fixture.metadata.IsRangeZero(LxrSideMetadataKind::ReferenceCount, start, size,
                       SideMetadataMemoryOrder::Relaxed, &zero) == SideMetadataResult::Success && zero &&
                fixture.metadata.IsRangeZero(LxrSideMetadataKind::ReferenceCountStraddle, start, size,
                       SideMetadataMemoryOrder::Relaxed, &straddleZero) == SideMetadataResult::Success && straddleZero;
        };

        // A fresh block for a mutator outside a trace: the unlog table, the mark table and the ValidObject plane are clear.
        ImmixBumpAllocator first;
        ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
        ExpectMetadata("f9 first allocator", first.Initialize(&fixture.allocation, OwnerOne), SideMetadataResult::Success);
        ExpectMetadata("f9 fresh refill", first.TryRefill(ImmixAllocationSource::MutatorFresh,
            ImmixMarkingContext::Inactive, &status), SideMetadataResult::Success);
        Expect("f9 fresh refill ready", status == ImmixAllocationStatus::Ready);
        uintptr_t freshBlock = first.GetBlock();
        Expect("f9 fresh block clears ValidObject", planeIs(LxrSideMetadataKind::ValidObject, freshBlock,
            ImmixBlockGeometry::BlockBytes, 0));
        Expect("f9 fresh block clears the unlog table", planeIs(LxrSideMetadataKind::FieldUnlogged, freshBlock,
            ImmixBlockGeometry::BlockBytes, 0));
        Expect("f9 fresh block clears the mark table", planeIs(LxrSideMetadataKind::ObjectMark, freshBlock,
            ImmixBlockGeometry::BlockBytes, 0));
        Expect("f9 fresh block keeps zero counts and no straddle mark",
            countsAndStraddleZero(freshBlock, ImmixBlockGeometry::BlockBytes));
        Expect("f9 fresh block outside a trace leaves the reuse counts",
            LoadMetadata(fixture, LxrSideMetadataKind::LineReuse, LineAddress(freshBlock, 5)) == UINT8_MAX);
        uintptr_t otherBlock = freshBlock == fixture.data_start ? fixture.data_start + ImmixBlockGeometry::BlockBytes
                                                                 : fixture.data_start;
        Expect("f9 a block that no buffer took keeps its planes",
            planeIs(LxrSideMetadataKind::ValidObject, otherBlock, ImmixBlockGeometry::BlockBytes, 1) &&
            planeIs(LxrSideMetadataKind::FieldUnlogged, otherBlock, ImmixBlockGeometry::BlockBytes, 1) &&
            planeIs(LxrSideMetadataKind::ObjectMark, otherBlock, ImmixBlockGeometry::BlockBytes, 1));

        // A fresh block during a trace: marked in bulk, and the reuse counts of its lines are incremented (255 wraps to 0).
        ImmixBumpAllocator second;
        ExpectMetadata("f9 second allocator", second.Initialize(&fixture.allocation, OwnerTwo), SideMetadataResult::Success);
        ExpectMetadata("f9 marking fresh refill", second.TryRefill(ImmixAllocationSource::MutatorFresh,
            ImmixMarkingContext::ConcurrentOrFinalMark, &status), SideMetadataResult::Success);
        uintptr_t markedBlock = second.GetBlock();
        Expect("f9 marking refill took another block", markedBlock != freshBlock && markedBlock != 0);
        Expect("f9 a refill during a trace marks the whole block", planeIs(LxrSideMetadataKind::ObjectMark, markedBlock,
            ImmixBlockGeometry::BlockBytes, 1));
        Expect("f9 a refill during a trace clears the unlog table", planeIs(LxrSideMetadataKind::FieldUnlogged, markedBlock,
            ImmixBlockGeometry::BlockBytes, 0));
        bool reuseOk = LoadMetadata(fixture, LxrSideMetadataKind::LineReuse, LineAddress(markedBlock, 5)) == 0;
        for (size_t line = 0; line < ImmixBlockGeometry::LinesPerBlock; line++)
        {
            reuseOk = reuseOk && (line == 5 ||
                LoadMetadata(fixture, LxrSideMetadataKind::LineReuse, LineAddress(markedBlock, line)) == 1);
        }
        Expect("f9 a refill during a trace increments the reuse count of every line", reuseOk);
        Expect("f9 the marking refill keeps zero counts and no straddle mark",
            countsAndStraddleZero(markedBlock, ImmixBlockGeometry::BlockBytes));

        // A recycled hole: only the hole is changed, and the lines after it are not read (the lazy search).
        uintptr_t reusable = 0;
        for (size_t index = 0; index < 6; index++)
        {
            uintptr_t candidate = fixture.data_start + (index * ImmixBlockGeometry::BlockBytes);
            if (candidate != freshBlock && candidate != markedBlock)
            {
                reusable = candidate;
                break;
            }
        }
        SeedReusableBlock(fixture, reusable);
        SeedAllLines(fixture, reusable, 1);
        SeedLine(fixture, reusable, 6, 0);
        SeedLine(fixture, reusable, 7, 0);
        SeedLine(fixture, reusable, 8, 0);
        ImmixBumpAllocator third;
        ExpectMetadata("f9 third allocator", third.Initialize(&fixture.allocation, OwnerThree), SideMetadataResult::Success);
        uint64_t before = fixture.allocation.GetLineProbesForTest();
        ExpectMetadata("f9 recycled refill", third.TryRefill(ImmixAllocationSource::MutatorRecycled,
            ImmixMarkingContext::Inactive, &status), SideMetadataResult::Success);
        Expect("f9 recycled refill ready", status == ImmixAllocationStatus::Ready);
        Expect("f9 the recycled hole is the run after the skipped line",
            third.GetCursor() == LineAddress(reusable, 7) && third.GetLimit() == LineAddress(reusable, 9));
        Expect("f9 the search of one hole read 10 lines of 128",
            fixture.allocation.GetLineProbesForTest() - before == 10);
        Expect("f9 the recycled hole clears ValidObject and the unlog table",
            planeIs(LxrSideMetadataKind::ValidObject, LineAddress(reusable, 7), 2 * ImmixBlockGeometry::LineBytes, 0) &&
            planeIs(LxrSideMetadataKind::FieldUnlogged, LineAddress(reusable, 7), 2 * ImmixBlockGeometry::LineBytes, 0));
        Expect("f9 the recycled hole leaves its neighbours alone",
            planeIs(LxrSideMetadataKind::ValidObject, LineAddress(reusable, 6), ImmixBlockGeometry::LineBytes, 1) &&
            planeIs(LxrSideMetadataKind::ValidObject, LineAddress(reusable, 9), ImmixBlockGeometry::LineBytes, 1) &&
            planeIs(LxrSideMetadataKind::FieldUnlogged, LineAddress(reusable, 9), ImmixBlockGeometry::LineBytes, 1));
        Expect("f9 outside a trace the recycled hole keeps its mark bits",
            planeIs(LxrSideMetadataKind::ObjectMark, LineAddress(reusable, 7), 2 * ImmixBlockGeometry::LineBytes, 1));

        ImmixBumpAllocator* all[] = {&first, &second, &third};
        for (ImmixBumpAllocator* allocator : all)
        {
            allocator->Reset(&status);
        }
    }

    // A range that a mutator is about to take has zero counts and no straddle mark. When it has, the refill reports it
    // (Debug builds and the test platform), and does not clear the planes.
    void TestF9DirtyRangeIsReported()
    {
        for (bool straddle : {false, true})
        {
            Fixture fixture(2, 2, 1);
            Expect("f9 dirty fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }
            uintptr_t block = fixture.data_start;
            if (straddle)
            {
                ExpectMetadata("f9 dirty straddle",
                    fixture.metadata.Store(LxrSideMetadataKind::ReferenceCountStraddle, LineAddress(block, 3), 1,
                        SideMetadataMemoryOrder::SequentiallyConsistent), SideMetadataResult::Success);
            }
            else
            {
                SeedLine(fixture, block, 3, 1);
            }
            ImmixBumpAllocator allocator;
            ExpectMetadata("f9 dirty allocator", allocator.Initialize(&fixture.allocation, OwnerOne),
                SideMetadataResult::Success);
            ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
            SideMetadataResult result = allocator.TryRefill(ImmixAllocationSource::MutatorFresh,
                ImmixMarkingContext::Inactive, &status);
            // The allocator takes the next block when this one fails, so look at the outcome for the dirty block.
            uintptr_t taken = allocator.GetBlock();
            Expect("f9 a dirty free block is reported and not handed out",
                result == SideMetadataResult::InconsistentState || (result == SideMetadataResult::Success &&
                    taken != block && taken != 0));
            bool zero = true;
            fixture.metadata.IsRangeZero(LxrSideMetadataKind::ReferenceCount, block, ImmixBlockGeometry::BlockBytes,
                SideMetadataMemoryOrder::Relaxed, &zero);
            Expect("f9 the refill does not clear a count that it found", straddle ? true : !zero);
            allocator.Reset(&status);
        }
    }

    // P28 (F10). The metadata loads are plain acquire loads. Sequential consistency between a store and a later load of
    // another location comes from the store (a compare-exchange loop, a full barrier). This is the store-buffer test: two
    // threads each store to their own location and then load the location of the other. Both loads returning zero would be
    // the reordering that a plain store allows. The Java LXR relies on the same property (SeqCst load = plain load on x64).
    void TestF10LoadOrder()
    {
        Fixture fixture(1, 2, 1);
        Expect("f10 order fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }
        SideMetadataAccessor accessor = fixture.metadata.GetAccessor(LxrSideMetadataKind::ObjectMark);
        Expect("f10 order accessor", accessor.IsValid());
        if (!accessor.IsValid())
        {
            return;
        }
        constexpr int Iterations = 300000;
        const uintptr_t first = fixture.data_start;
        const uintptr_t second = fixture.data_start + 4096;
        std::vector<uint8_t> seenFirst(Iterations, 1);
        std::vector<uint8_t> seenSecond(Iterations, 1);
        std::atomic<int> go{-1};
        std::atomic<int> finished{0};
        auto worker = [&](uintptr_t mine, uintptr_t other, std::vector<uint8_t>* seen)
        {
            for (int i = 0; i < Iterations; i++)
            {
                while (go.load(std::memory_order_acquire) < i)
                {
                }
                accessor.Store(mine, 1, SideMetadataMemoryOrder::SequentiallyConsistent);
                (*seen)[i] = static_cast<uint8_t>(accessor.Load(other, SideMetadataMemoryOrder::SequentiallyConsistent));
                finished.fetch_add(1, std::memory_order_acq_rel);
            }
        };
        std::thread a(worker, first, second, &seenFirst);
        std::thread b(worker, second, first, &seenSecond);
        for (int i = 0; i < Iterations; i++)
        {
            accessor.Store(first, 0, SideMetadataMemoryOrder::Relaxed);
            accessor.Store(second, 0, SideMetadataMemoryOrder::Relaxed);
            go.store(i, std::memory_order_release);
            while (finished.load(std::memory_order_acquire) < 2 * (i + 1))
            {
            }
        }
        a.join();
        b.join();
        size_t both = 0;
        for (int i = 0; i < Iterations; i++)
        {
            both += (seenFirst[i] == 0 && seenSecond[i] == 0) ? 1 : 0;
        }
        if (both != 0)
        {
            printf("DETAIL: f10 store-buffer outcomes (both loads zero): %zu of %d\n", both, Iterations);
        }
        Expect("f10 a store followed by a load of another location is not reordered (no both-zero outcome)", both == 0);
    }

    // P28 (F9). The block cursors of the Java LXR: they move forward only, a block that becomes a candidate behind the cursor
    // is found after the cursors are set back (the pause does it), and concurrent callers take every block exactly once.
    void TestF9Cursors()
    {
        {
            Fixture fixture(1, 4, 1);
            Expect("f9 cursor fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }
            ImmixBumpAllocator allocator;
            ExpectMetadata("f9 cursor allocator", allocator.Initialize(&fixture.allocation, OwnerOne),
                SideMetadataResult::Success);
            ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
            ExpectMetadata("f9 first recycled refill", allocator.TryRefill(ImmixAllocationSource::MutatorRecycled,
                ImmixMarkingContext::Inactive, &status), SideMetadataResult::Success);
            Expect("f9 no recycled block while every block is free", status == ImmixAllocationStatus::Exhausted);
            uintptr_t block = fixture.data_start + (2 * ImmixBlockGeometry::BlockBytes);
            SeedReusableBlock(fixture, block);
            SeedAllLines(fixture, block, 1);
            SeedLine(fixture, block, 4, 0);
            SeedLine(fixture, block, 5, 0);
            SeedLine(fixture, block, 6, 0);
            ExpectMetadata("f9 second recycled refill", allocator.TryRefill(ImmixAllocationSource::MutatorRecycled,
                ImmixMarkingContext::Inactive, &status), SideMetadataResult::Success);
            Expect("f9 a block that becomes a candidate behind the cursor is not found before the cursors are reset",
                status == ImmixAllocationStatus::Exhausted);
            fixture.allocation.ResetCursors();
            ExpectMetadata("f9 third recycled refill", allocator.TryRefill(ImmixAllocationSource::MutatorRecycled,
                ImmixMarkingContext::Inactive, &status), SideMetadataResult::Success);
            Expect("f9 the block is found after the cursors are reset",
                status == ImmixAllocationStatus::Ready && allocator.GetBlock() == block);
            allocator.Reset(&status);
        }
        {
            constexpr size_t BlockCount = 64;
            constexpr size_t ThreadCount = 8;
            Fixture fixture(1, BlockCount, 1);
            Expect("f9 concurrent fixture initialized", fixture.initialized);
            if (!fixture.initialized)
            {
                return;
            }
            std::vector<std::vector<uintptr_t>> taken(ThreadCount);
            std::vector<std::thread> threads;
            for (size_t thread = 0; thread < ThreadCount; thread++)
            {
                threads.emplace_back([&, thread]()
                {
                    ImmixBumpAllocator allocator;
                    if (allocator.Initialize(&fixture.allocation, 0x3000 + thread) != SideMetadataResult::Success)
                    {
                        return;
                    }
                    ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
                    while (allocator.TryRefill(ImmixAllocationSource::MutatorFresh, ImmixMarkingContext::Inactive,
                               &status) == SideMetadataResult::Success && status == ImmixAllocationStatus::Ready)
                    {
                        taken[thread].push_back(allocator.GetBlock());
                    }
                });
            }
            for (std::thread& thread : threads)
            {
                thread.join();
            }
            std::vector<size_t> seen(BlockCount, 0);
            size_t total = 0;
            for (const std::vector<uintptr_t>& blocks : taken)
            {
                for (uintptr_t block : blocks)
                {
                    seen[(block - fixture.data_start) / ImmixBlockGeometry::BlockBytes]++;
                    total++;
                }
            }
            bool once = total == BlockCount;
            for (size_t count : seen)
            {
                once = once && count == 1;
            }
            Expect("f9 concurrent callers take every block exactly once", once);
        }
    }

    // P28 (F10). The counts are updated with a compare-exchange on one byte. Neighbouring counts share the byte (four 2-bit counts,
    // two 4-bit counts), so a lost update of one thread shows in the count of another. Each thread adds one to its own count and
    // takes it away again, many times; the counts must end as they started. The control writes the byte with a plain store.
    void TestF10ByteCompareExchange(uint8_t logBits)
    {
        Fixture fixture(logBits, 2, 1);
        Expect("f10 byte fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }
        SideMetadataAccessor accessor = fixture.metadata.GetAccessor(LxrSideMetadataKind::ReferenceCount);
        Expect("f10 byte accessor", accessor.IsValid());
        if (!accessor.IsValid())
        {
            return;
        }
        const size_t entries = 8;
        for (size_t entry = 0; entry < entries; entry++)
        {
            ExpectMetadata("f10 byte seed", fixture.metadata.Store(LxrSideMetadataKind::ReferenceCount,
                fixture.data_start + (entry * sizeof(uintptr_t)), 1, SideMetadataMemoryOrder::Relaxed),
                SideMetadataResult::Success);
        }
        std::atomic<bool> go{false};
        std::vector<std::thread> threads;
        for (size_t entry = 0; entry < entries; entry++)
        {
            threads.emplace_back([&, entry]()
            {
                while (!go.load(std::memory_order_acquire))
                {
                }
                uintptr_t address = fixture.data_start + (entry * sizeof(uintptr_t));
                for (int round = 0; round < 200000; round++)
                {
                    uintptr_t previous;
                    accessor.IncrementSaturating(address, SideMetadataMemoryOrder::Relaxed, &previous);
                    accessor.DecrementNonZeroNonSaturated(address, SideMetadataMemoryOrder::Relaxed, &previous);
                }
            });
        }
        go.store(true, std::memory_order_release);
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        bool same = true;
        for (size_t entry = 0; entry < entries; entry++)
        {
            same = same && LoadMetadata(fixture, LxrSideMetadataKind::ReferenceCount,
                fixture.data_start + (entry * sizeof(uintptr_t))) == 1;
        }
        Expect("f10 counts that share a byte keep their values under concurrent updates", same);
        // A count saturates at its maximum and then stays.
        uintptr_t maximum = (static_cast<uintptr_t>(1) << (1u << logBits)) - 1;
        uintptr_t address = fixture.data_start + (3 * sizeof(uintptr_t));
        uintptr_t previous = 0;
        SideMetadataUpdateStatus status = SideMetadataUpdateStatus::Updated;
        for (uintptr_t value = 1; value < maximum; value++)
        {
            status = accessor.IncrementSaturating(address, SideMetadataMemoryOrder::Relaxed, &previous);
        }
        bool saturated = status == SideMetadataUpdateStatus::Updated &&
            accessor.IncrementSaturating(address, SideMetadataMemoryOrder::Relaxed, &previous) ==
                SideMetadataUpdateStatus::Saturated && previous == maximum;
        saturated = saturated && accessor.DecrementNonZeroNonSaturated(address, SideMetadataMemoryOrder::Relaxed, &previous) ==
            SideMetadataUpdateStatus::Saturated && LoadMetadata(fixture, LxrSideMetadataKind::ReferenceCount, address) == maximum;
        Expect("f10 a count at its maximum is sticky: no increment, no decrement", saturated);
        Expect("f10 the neighbours of a saturated count are unchanged",
            LoadMetadata(fixture, LxrSideMetadataKind::ReferenceCount, address - sizeof(uintptr_t)) == 1 &&
            LoadMetadata(fixture, LxrSideMetadataKind::ReferenceCount, address + sizeof(uintptr_t)) == 1);
    }

    // P28 (F10). The prefetch of the Java LXR loops: while entry i is processed, the object of entry i + 8 is prefetched; nothing past
    // the end, nothing for a null reference; a slot is read for its reference. The hook records the addresses that are prefetched.
    std::vector<const void*> s_prefetched;
    void RecordPrefetch(const void* address)
    {
        s_prefetched.push_back(address);
    }

    void TestF10Prefetch()
    {
        LxrPrefetch::HookRef() = RecordPrefetch;
        constexpr size_t Count = 40;
        std::vector<uintptr_t> cells(Count + 1);
        std::vector<uintptr_t> objects(Count);
        std::vector<uintptr_t> slots(Count);
        for (size_t i = 0; i < Count; i++)
        {
            objects[i] = i % 11 == 9 ? 0 : reinterpret_cast<uintptr_t>(&cells[i]);
            cells[i] = objects[i];
            slots[i] = reinterpret_cast<uintptr_t>(&cells[i]);
        }
        auto run = [&](const std::vector<uintptr_t>& entries, size_t count, bool slotEntries, uintptr_t mask)
        {
            s_prefetched.clear();
            for (size_t i = 0; i < count; i++)
            {
                LxrPrefetch::Ahead(entries.data(), count, i, slotEntries, mask);
            }
            return s_prefetched;
        };
        auto expected = [&](size_t count)
        {
            std::vector<const void*> list;
            for (size_t i = LxrPrefetch::Step; i < count; i++)
            {
                if (objects[i] != 0)
                {
                    list.push_back(reinterpret_cast<const void*>(objects[i]));
                }
            }
            return list;
        };
        Expect("f10 prefetch step is 8", LxrPrefetch::Step == 8);
        Expect("f10 prefetch of object entries: the object of entry i + 8, none past the end, none for null",
            run(objects, Count, false, 0) == expected(Count));
        Expect("f10 prefetch of slot entries: the object that the slot of entry i + 8 holds",
            run(slots, Count, true, 0) == expected(Count));
        std::vector<uintptr_t> flagged = objects;
        for (uintptr_t& entry : flagged)
        {
            entry |= entry != 0 ? 1 : 0;
        }
        Expect("f10 prefetch of flagged entries (roots): the flag bit is removed",
            run(flagged, Count, false, 1) == expected(Count));
        Expect("f10 a packet of 8 entries or less prefetches nothing", run(objects, LxrPrefetch::Step, false, 0).empty() &&
            run(objects, 3, false, 0).empty());
        Expect("f10 a packet of 9 entries prefetches the ninth", run(objects, 9, false, 0) == expected(9));
        LxrPrefetch::HookRef() = nullptr;
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
        // P28 (F9). The block cursors only move forward; the engine sets them back to 0 at the end of a pause (as the Java LXR does).
        fixture.allocation.ResetCursors();

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
            // P28 (F9). Every block that is not free is a candidate, as in the Java LXR (block_is_available): its counts are read when
            // it is taken, and a block without a hole of the minimum length is given back.
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
                "every block that is not free is taken and given back",
                fixture.allocation.GetRecycledBlocksAcquired() == 2);
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
    TestF9ScanEquivalence();
    TestF9LazyScan();
    TestF9RefillSchedule(1);
    TestF9RefillSchedule(3);
    TestF9DirtyRangeIsReported();
    TestF9Cursors();
    TestF10LoadOrder();
    TestF10Prefetch();
    TestF10ByteCompareExchange(1);
    TestF10ByteCompareExchange(2);
    TestF10ByteCompareExchange(3);
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

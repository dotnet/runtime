// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/immix_block.h"

#include <atomic>
#include <stdio.h>
#include <thread>
#include <vector>

namespace
{
#ifdef HOST_64BIT
    constexpr uintptr_t DataStart = UINT64_C(0x0000000100000000);
    constexpr size_t BlockCount = 64;
    constexpr size_t DataSize = BlockCount * ImmixBlockGeometry::BlockBytes;
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
        Expect(name, actual == expected);
    }

    void ExpectGeometry(
        const char* name,
        ImmixGeometryResult actual,
        ImmixGeometryResult expected)
    {
        Expect(name, actual == expected);
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
        ImmixBlockManager blocks;
        bool initialized;

        explicit Fixture(
            uint64_t enabledSpecMask = UINT64_MAX,
            uintptr_t start = DataStart,
            size_t size = DataSize)
            : initialized(false)
        {
            SideMetadataResult result = LxrSideMetadataLayout::Create(1, &layout);
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
                result = blocks.RegisterBlockRange(start, size);
            }
            initialized = result == SideMetadataResult::Success;
        }

        ~Fixture()
        {
            blocks.Shutdown();
            metadata.Shutdown();
        }
    };

    void SeedState(
        Fixture& fixture,
        uintptr_t block,
        uint8_t rawState,
        uintptr_t owner = ImmixBlockManager::NoOwner,
        uint8_t phaseEpoch = 0)
    {
        ExpectMetadata(
            "seed block state",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockMark,
                block,
                rawState,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed block owner",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockOwner,
                block,
                owner,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "clear block lock",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockInUse,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "seed phase epoch",
            fixture.metadata.Store(
                LxrSideMetadataKind::PhaseEpoch,
                block,
                phaseEpoch,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "clear promotion state",
            fixture.metadata.Store(
                LxrSideMetadataKind::NurseryPromotion,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "clear defrag state",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockDefrag,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
    }

    void ExpectState(
        const char* name,
        Fixture& fixture,
        uintptr_t block,
        ImmixBlockState expected,
        uint8_t expectedUnavailableLines = 0)
    {
        ImmixBlockState state = ImmixBlockState::Unallocated;
        uint8_t unavailableLines = UINT8_MAX;
        SideMetadataResult result = fixture.blocks.GetState(
            block,
            &state,
            &unavailableLines);
        ExpectMetadata(name, result, SideMetadataResult::Success);
        Expect(name, state == expected);
        Expect(name, unavailableLines == expectedUnavailableLines);
    }

    void TestGeometry()
    {
        Expect("block bytes", ImmixBlockGeometry::BlockBytes == 32 * 1024);
        Expect("line bytes", ImmixBlockGeometry::LineBytes == 256);
        Expect("lines per block", ImmixBlockGeometry::LinesPerBlock == 128);
        Expect("block mask", ImmixBlockGeometry::BlockMask == 0x7fff);
        Expect("line mask", ImmixBlockGeometry::LineMask == 0xff);

        uintptr_t address = 0;
        ExpectGeometry(
            "block start zero",
            ImmixBlockGeometry::GetBlockStart(0, &address),
            ImmixGeometryResult::Success);
        Expect("block start zero value", address == 0);
        ExpectGeometry(
            "block containing last byte",
            ImmixBlockGeometry::GetBlockStart(
                ImmixBlockGeometry::AddressLimit - 1,
                &address),
            ImmixGeometryResult::Success);
        Expect(
            "last block start",
            address == ImmixBlockGeometry::AddressLimit -
                ImmixBlockGeometry::BlockBytes);
        ExpectGeometry(
            "block rejects exclusive limit",
            ImmixBlockGeometry::GetBlockStart(
                ImmixBlockGeometry::AddressLimit,
                &address),
            ImmixGeometryResult::AddressOverflow);
        ExpectGeometry(
            "block rejects null output",
            ImmixBlockGeometry::GetBlockStart(0, nullptr),
            ImmixGeometryResult::InvalidArgument);

        ExpectGeometry(
            "line containing last byte",
            ImmixBlockGeometry::GetLineStart(
                ImmixBlockGeometry::AddressLimit - 1,
                &address),
            ImmixGeometryResult::Success);
        Expect(
            "last line start",
            address == ImmixBlockGeometry::AddressLimit -
                ImmixBlockGeometry::LineBytes);
        ExpectGeometry(
            "align block top",
            ImmixBlockGeometry::AlignBlockUp(
                ImmixBlockGeometry::AddressLimit - 1,
                &address),
            ImmixGeometryResult::Success);
        Expect("align block top value", address == ImmixBlockGeometry::AddressLimit);
        ExpectGeometry(
            "align line top",
            ImmixBlockGeometry::AlignLineUp(
                ImmixBlockGeometry::AddressLimit - 1,
                &address),
            ImmixGeometryResult::Success);
        Expect("align line top value", address == ImmixBlockGeometry::AddressLimit);
        ExpectGeometry(
            "align exclusive limit",
            ImmixBlockGeometry::AlignBlockUp(
                ImmixBlockGeometry::AddressLimit,
                &address),
            ImmixGeometryResult::Success);
        Expect("align exclusive limit value", address == ImmixBlockGeometry::AddressLimit);

        size_t index = 0;
        ExpectGeometry(
            "block index",
            ImmixBlockGeometry::GetBlockIndex(
                ImmixBlockGeometry::BlockBytes * 7 + 19,
                &index),
            ImmixGeometryResult::Success);
        Expect("block index value", index == 7);
        ExpectGeometry(
            "line index",
            ImmixBlockGeometry::GetLineIndex(
                ImmixBlockGeometry::LineBytes * 17 + 2,
                &index),
            ImmixGeometryResult::Success);
        Expect("line index value", index == 17);
        ExpectGeometry(
            "line index in block",
            ImmixBlockGeometry::GetLineIndexInBlock(
                ImmixBlockGeometry::BlockBytes * 3 +
                    ImmixBlockGeometry::LineBytes * 127 + 255,
                &index),
            ImmixGeometryResult::Success);
        Expect("line index in block value", index == 127);

        ImmixGeometryRange range = {};
        ExpectGeometry(
            "empty range",
            ImmixBlockGeometry::GetLineRange(
                ImmixBlockGeometry::AddressLimit,
                0,
                &range),
            ImmixGeometryResult::Success);
        Expect(
            "empty range values",
            range.first == ImmixBlockGeometry::AddressLimit &&
                range.end == ImmixBlockGeometry::AddressLimit &&
                range.count == 0);
        ExpectGeometry(
            "single aligned line",
            ImmixBlockGeometry::GetLineRange(DataStart, 256, &range),
            ImmixGeometryResult::Success);
        Expect(
            "single aligned line values",
            range.first == DataStart &&
                range.end == DataStart + 256 &&
                range.count == 1);
        ExpectGeometry(
            "single partial line",
            ImmixBlockGeometry::GetLineRange(DataStart + 1, 254, &range),
            ImmixGeometryResult::Success);
        Expect("single partial line count", range.count == 1);
        ExpectGeometry(
            "exact boundary excludes next line",
            ImmixBlockGeometry::GetLineRange(DataStart + 1, 255, &range),
            ImmixGeometryResult::Success);
        Expect("exact boundary line count", range.count == 1);
        ExpectGeometry(
            "cross boundary includes next line",
            ImmixBlockGeometry::GetLineRange(DataStart + 1, 256, &range),
            ImmixGeometryResult::Success);
        Expect("cross boundary line count", range.count == 2);
        ExpectGeometry(
            "object may cross line",
            ImmixBlockGeometry::GetObjectLineRange(
                DataStart + 250,
                12,
                &range),
            ImmixGeometryResult::Success);
        Expect("object crossing line count", range.count == 2);
        ExpectGeometry(
            "object may end at block boundary",
            ImmixBlockGeometry::GetObjectLineRange(
                DataStart + ImmixBlockGeometry::BlockBytes - 10,
                10,
                &range),
            ImmixGeometryResult::Success);
        ExpectGeometry(
            "object may not cross block",
            ImmixBlockGeometry::GetObjectLineRange(
                DataStart + ImmixBlockGeometry::BlockBytes - 10,
                11,
                &range),
            ImmixGeometryResult::CrossesBlock);
        ExpectGeometry(
            "top byte range",
            ImmixBlockGeometry::GetLineRange(
                ImmixBlockGeometry::AddressLimit - 1,
                1,
                &range),
            ImmixGeometryResult::Success);
        Expect(
            "top byte range values",
            range.first == ImmixBlockGeometry::AddressLimit -
                ImmixBlockGeometry::LineBytes &&
                range.end == ImmixBlockGeometry::AddressLimit &&
                range.count == 1);
        ExpectGeometry(
            "range beyond limit",
            ImmixBlockGeometry::GetLineRange(
                ImmixBlockGeometry::AddressLimit - 1,
                2,
                &range),
            ImmixGeometryResult::AddressOverflow);
        ExpectGeometry(
            "native overflow range",
            ImmixBlockGeometry::GetLineRange(UINTPTR_MAX - 1, 4, &range),
            ImmixGeometryResult::AddressOverflow);
    }

    void TestStateEncoding()
    {
        Fixture fixture;
        Expect("state fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        for (uint32_t raw = 0; raw <= UINT8_MAX; raw++)
        {
            ExpectMetadata(
                "raw state store",
                fixture.metadata.Store(
                    LxrSideMetadataKind::BlockMark,
                    DataStart,
                    raw,
                    SideMetadataMemoryOrder::SequentiallyConsistent),
                SideMetadataResult::Success);
            ImmixBlockState state;
            uint8_t unavailableLines;
            ExpectMetadata(
                "raw state load",
                fixture.blocks.GetState(
                    DataStart,
                    &state,
                    &unavailableLines),
                SideMetadataResult::Success);
            ImmixBlockState expectedState =
                raw == 0 ? ImmixBlockState::Unallocated :
                raw == 254 ? ImmixBlockState::Marked :
                raw == 255 ? ImmixBlockState::Unmarked :
                ImmixBlockState::Reusable;
            uint8_t expectedLines =
                expectedState == ImmixBlockState::Reusable
                    ? static_cast<uint8_t>(raw)
                    : 0;
            Expect("raw state decode", state == expectedState);
            Expect("raw unavailable lines decode", unavailableLines == expectedLines);
        }
    }

    void TestLifecycleAndTransitions()
    {
        Fixture fixture;
        Expect("lifecycle fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        constexpr uintptr_t Owner1 = 0x101;
        constexpr uintptr_t Owner2 = 0x202;
        ImmixBlockOperationStatus status = ImmixBlockOperationStatus::Stale;
        bool predicate = false;
        uint8_t epoch = 0;

        Expect("initial global epoch", fixture.blocks.GetGlobalPhaseEpoch() == 1);
        ExpectMetadata(
            "fresh mutator acquire",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner1,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        Expect("fresh mutator acquire updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState("fresh block state unchanged", fixture, DataStart, ImmixBlockState::Unallocated);
        ExpectMetadata(
            "fresh block epoch load",
            fixture.blocks.GetBlockPhaseEpoch(DataStart, &epoch),
            SideMetadataResult::Success);
        Expect("fresh block epoch", epoch == 1);
        ExpectMetadata(
            "fresh block nursery predicate",
            fixture.blocks.IsNursery(DataStart, &predicate),
            SideMetadataResult::Success);
        Expect("fresh block is nursery", predicate);
        ExpectMetadata(
            "fresh block reusing predicate",
            fixture.blocks.IsReusing(DataStart, &predicate),
            SideMetadataResult::Success);
        Expect("fresh block is not reusing", !predicate);

        ExpectMetadata(
            "second acquire sees owner",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner2,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        Expect("second acquire stale", status == ImmixBlockOperationStatus::Stale);
        ExpectMetadata(
            "wrong owner return",
            fixture.blocks.TryReturn(DataStart, Owner2, &status),
            SideMetadataResult::Success);
        Expect("wrong owner return stale", status == ImmixBlockOperationStatus::Stale);
        ExpectMetadata(
            "owner returns nursery",
            fixture.blocks.TryReturn(DataStart, Owner1, &status),
            SideMetadataResult::Success);
        Expect("owner return updated", status == ImmixBlockOperationStatus::Updated);

        uintptr_t nurseryBlock =
            DataStart + 2 * ImmixBlockGeometry::BlockBytes;
        ExpectMetadata(
            "second nursery mutator acquire",
            fixture.blocks.TryAcquire(
                nurseryBlock,
                Owner1,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        Expect("second nursery acquired", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "second nursery return",
            fixture.blocks.TryReturn(nurseryBlock, Owner1, &status),
            SideMetadataResult::Success);
        Expect("second nursery returned", status == ImmixBlockOperationStatus::Updated);

        ExpectMetadata(
            "start pause",
            fixture.blocks.StartGcPause(&status),
            SideMetadataResult::Success);
        Expect("start pause updated", status == ImmixBlockOperationStatus::Updated);
        Expect("pause epoch even", fixture.blocks.GetGlobalPhaseEpoch() == 2);
        ExpectMetadata(
            "old nursery remains nursery in pause",
            fixture.blocks.IsNursery(DataStart, &predicate),
            SideMetadataResult::Success);
        Expect("old nursery predicate in pause", predicate);
        ExpectMetadata(
            "repeat start rejected",
            fixture.blocks.StartGcPause(&status),
            SideMetadataResult::Success);
        Expect("repeat start invalid", status == ImmixBlockOperationStatus::InvalidTransition);

        ExpectMetadata(
            "normal copy rejects prior nursery",
            fixture.blocks.TryAcquire(
                nurseryBlock,
                Owner2,
                ImmixBlockAcquireKind::GcCopyFresh,
                &status),
            SideMetadataResult::Success);
        Expect("normal copy nursery is stale", status == ImmixBlockOperationStatus::Stale);
        ExpectMetadata(
            "mature evacuation accepts prior nursery",
            fixture.blocks.TryAcquire(
                nurseryBlock,
                Owner2,
                ImmixBlockAcquireKind::GcMatureEvacuationFresh,
                &status),
            SideMetadataResult::Success);
        Expect("mature evacuation nursery updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "mature evacuation nursery return",
            fixture.blocks.TryReturn(nurseryBlock, Owner2, &status),
            SideMetadataResult::Success);
        Expect("mature evacuation nursery returned", status == ImmixBlockOperationStatus::Updated);

        ExpectMetadata(
            "promote nursery",
            fixture.blocks.TryPromoteInPlace(DataStart, &status),
            SideMetadataResult::Success);
        Expect("promote nursery updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "promote nursery idempotent",
            fixture.blocks.TryPromoteInPlace(DataStart, &status),
            SideMetadataResult::Success);
        Expect(
            "promote nursery unchanged",
            status == ImmixBlockOperationStatus::Unchanged);
        ExpectState("promoted state", fixture, DataStart, ImmixBlockState::Unmarked);
        ExpectMetadata(
            "promoted epoch load",
            fixture.blocks.GetBlockPhaseEpoch(DataStart, &epoch),
            SideMetadataResult::Success);
        Expect("promoted epoch current", epoch == 2);
        ExpectMetadata(
            "promoted GC reuse",
            fixture.blocks.IsGcReusing(DataStart, &predicate),
            SideMetadataResult::Success);
        Expect("promoted is GC reusing", predicate);

        ExpectMetadata(
            "mark promoted block",
            fixture.blocks.Mark(DataStart, &status),
            SideMetadataResult::Success);
        Expect("mark updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState("marked state", fixture, DataStart, ImmixBlockState::Marked);
        ExpectMetadata(
            "mark idempotent",
            fixture.blocks.Mark(DataStart, &status),
            SideMetadataResult::Success);
        Expect("mark unchanged", status == ImmixBlockOperationStatus::Unchanged);
        ExpectMetadata(
            "set reusable",
            fixture.blocks.SetReusable(DataStart, 37, &status),
            SideMetadataResult::Success);
        Expect("set reusable updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState("reusable state", fixture, DataStart, ImmixBlockState::Reusable, 37);
        ExpectMetadata(
            "update reusable",
            fixture.blocks.SetReusable(DataStart, 12, &status),
            SideMetadataResult::Success);
        Expect("update reusable updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState("updated reusable state", fixture, DataStart, ImmixBlockState::Reusable, 12);
        ExpectMetadata(
            "prepare reusable",
            fixture.blocks.PrepareForTrace(DataStart, &status),
            SideMetadataResult::Success);
        Expect("prepare reusable updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState("prepared state", fixture, DataStart, ImmixBlockState::Unmarked);

        SeedState(fixture, DataStart, 255);
        ExpectMetadata(
            "GC copy reusable acquire",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner2,
                ImmixBlockAcquireKind::GcCopyReusable,
                &status),
            SideMetadataResult::Success);
        Expect("GC copy reusable acquire updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "GC copy return",
            fixture.blocks.TryReturn(DataStart, Owner2, &status),
            SideMetadataResult::Success);
        Expect("GC copy return updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "same-GC copy reuse rejected",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner1,
                ImmixBlockAcquireKind::GcCopyReusable,
                &status),
            SideMetadataResult::Success);
        Expect("same-GC copy reuse stale", status == ImmixBlockOperationStatus::Stale);

        SeedState(fixture, DataStart + ImmixBlockGeometry::BlockBytes, 0);
        ExpectMetadata(
            "GC copy fresh acquire",
            fixture.blocks.TryAcquire(
                DataStart + ImmixBlockGeometry::BlockBytes,
                Owner1,
                ImmixBlockAcquireKind::GcCopyFresh,
                &status),
            SideMetadataResult::Success);
        Expect("GC copy fresh updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState(
            "GC copy fresh state",
            fixture,
            DataStart + ImmixBlockGeometry::BlockBytes,
            ImmixBlockState::Unmarked);
        ExpectMetadata(
            "GC release",
            fixture.blocks.TryRelease(
                DataStart + ImmixBlockGeometry::BlockBytes,
                Owner1,
                &status),
            SideMetadataResult::Success);
        Expect("GC release updated", status == ImmixBlockOperationStatus::Updated);
        ExpectState(
            "GC release state",
            fixture,
            DataStart + ImmixBlockGeometry::BlockBytes,
            ImmixBlockState::Unallocated);
        ExpectMetadata(
            "released block GC reuse predicate",
            fixture.blocks.IsGcReusing(
                DataStart + ImmixBlockGeometry::BlockBytes,
                &predicate),
            SideMetadataResult::Success);
        Expect("released block is not GC reusing", !predicate);

        ExpectMetadata(
            "release pause",
            fixture.blocks.ReleaseGcPause(&status),
            SideMetadataResult::Success);
        Expect("release pause updated", status == ImmixBlockOperationStatus::Updated);
        Expect("release epoch odd", fixture.blocks.GetGlobalPhaseEpoch() == 3);
        ExpectMetadata(
            "unallocated GC predicate in mutator phase",
            fixture.blocks.IsGcReusing(
                DataStart + ImmixBlockGeometry::BlockBytes,
                &predicate),
            SideMetadataResult::Success);
        Expect("unallocated GC predicate remains false", !predicate);
        ExpectMetadata(
            "GC predicate rejects mutator phase",
            fixture.blocks.IsGcReusing(DataStart, &predicate),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "repeat release rejected",
            fixture.blocks.ReleaseGcPause(&status),
            SideMetadataResult::Success);
        Expect("repeat release invalid", status == ImmixBlockOperationStatus::InvalidTransition);

        SeedState(fixture, DataStart, 255);
        ExpectMetadata(
            "mutator reusable acquire",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner1,
                ImmixBlockAcquireKind::MutatorReusable,
                &status),
            SideMetadataResult::Success);
        Expect("mutator reusable updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "mutator reusable predicate",
            fixture.blocks.IsReusing(DataStart, &predicate),
            SideMetadataResult::Success);
        Expect("mutator reusable is reusing", predicate);
        ExpectMetadata(
            "mutator release refuses reusing block",
            fixture.blocks.TryRelease(DataStart, Owner1, &status),
            SideMetadataResult::Success);
        Expect(
            "mutator reusing release invalid",
            status == ImmixBlockOperationStatus::InvalidTransition);
        ExpectMetadata(
            "mutator reusable return",
            fixture.blocks.TryReturn(DataStart, Owner1, &status),
            SideMetadataResult::Success);
        Expect("mutator reusable return updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "same-mutator-phase reuse rejected",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner2,
                ImmixBlockAcquireKind::MutatorReusable,
                &status),
            SideMetadataResult::Success);
        Expect("same-mutator-phase reuse stale", status == ImmixBlockOperationStatus::Stale);
        ExpectMetadata(
            "start GC after mutator reuse",
            fixture.blocks.StartGcPause(&status),
            SideMetadataResult::Success);
        Expect("GC after mutator reuse started", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "copy rejects previous mutator reuse",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner2,
                ImmixBlockAcquireKind::GcCopyReusable,
                &status),
            SideMetadataResult::Success);
        Expect("previous mutator reuse is stale", status == ImmixBlockOperationStatus::Stale);
        ExpectMetadata(
            "release GC after reuse check",
            fixture.blocks.ReleaseGcPause(&status),
            SideMetadataResult::Success);
        Expect("GC after reuse check released", status == ImmixBlockOperationStatus::Updated);

        SeedState(fixture, DataStart, 0);
        ExpectMetadata(
            "promotion rejected in mutator phase",
            fixture.blocks.TryPromoteInPlace(DataStart, &status),
            SideMetadataResult::Success);
        Expect(
            "promotion wrong phase invalid",
            status == ImmixBlockOperationStatus::InvalidTransition);

        ExpectMetadata(
            "mark rejected in mutator phase",
            fixture.blocks.Mark(DataStart, &status),
            SideMetadataResult::Success);
        Expect("mark wrong phase invalid", status == ImmixBlockOperationStatus::InvalidTransition);
        ExpectMetadata(
            "prepare rejected in mutator phase",
            fixture.blocks.PrepareForTrace(DataStart, &status),
            SideMetadataResult::Success);
        Expect("prepare wrong phase invalid", status == ImmixBlockOperationStatus::InvalidTransition);
        ExpectMetadata(
            "reuse count zero rejected",
            fixture.blocks.SetReusable(DataStart, 0, &status),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "reuse count too large rejected",
            fixture.blocks.SetReusable(
                DataStart,
                static_cast<uint8_t>(ImmixBlockGeometry::LinesPerBlock + 1),
                &status),
            SideMetadataResult::InvalidArgument);

        SeedState(fixture, DataStart, 0);
        ExpectMetadata(
            "reusable acquire rejects unallocated",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner1,
                ImmixBlockAcquireKind::MutatorReusable,
                &status),
            SideMetadataResult::Success);
        Expect("reusable acquire invalid", status == ImmixBlockOperationStatus::InvalidTransition);
        SeedState(fixture, DataStart, 255);
        ExpectMetadata(
            "fresh acquire rejects allocated",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner1,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        Expect("fresh acquire invalid", status == ImmixBlockOperationStatus::InvalidTransition);
        ExpectMetadata(
            "invalid acquire kind",
            fixture.blocks.TryAcquire(
                DataStart,
                Owner1,
                static_cast<ImmixBlockAcquireKind>(UINT8_MAX),
                &status),
            SideMetadataResult::InvalidArgument);
    }

    void TestTransitionMatrix()
    {
        Fixture fixture;
        Expect("transition matrix fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        constexpr uint8_t RawStates[] = {0, 255, 254, 37};
        constexpr uintptr_t Owner = 0x333;
        ImmixBlockOperationStatus status;
        ExpectMetadata(
            "matrix enters GC phase",
            fixture.blocks.StartGcPause(&status),
            SideMetadataResult::Success);
        Expect("matrix GC phase entered", status == ImmixBlockOperationStatus::Updated);

        for (uint8_t rawState : RawStates)
        {
            bool allocated = rawState != 0;
            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix prepare",
                fixture.blocks.PrepareForTrace(DataStart, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix prepare status",
                status == (allocated
                    ? (rawState == 255
                        ? ImmixBlockOperationStatus::Unchanged
                        : ImmixBlockOperationStatus::Updated)
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (allocated)
            {
                ExpectState(
                    "matrix prepare target",
                    fixture,
                    DataStart,
                    ImmixBlockState::Unmarked);
            }

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix mark",
                fixture.blocks.Mark(DataStart, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix mark status",
                status == (allocated
                    ? (rawState == 254
                        ? ImmixBlockOperationStatus::Unchanged
                        : ImmixBlockOperationStatus::Updated)
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (allocated)
            {
                ExpectState(
                    "matrix mark target",
                    fixture,
                    DataStart,
                    ImmixBlockState::Marked);
            }

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix set reusable",
                fixture.blocks.SetReusable(DataStart, 11, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix reusable status",
                status == (allocated
                    ? (rawState == 11
                        ? ImmixBlockOperationStatus::Unchanged
                        : ImmixBlockOperationStatus::Updated)
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (allocated)
            {
                ExpectState(
                    "matrix reusable target",
                    fixture,
                    DataStart,
                    ImmixBlockState::Reusable,
                    11);
            }

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix promotion",
                fixture.blocks.TryPromoteInPlace(DataStart, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix promotion status",
                status == ImmixBlockOperationStatus::InvalidTransition);

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix GC fresh acquire",
                fixture.blocks.TryAcquire(
                    DataStart,
                    Owner,
                    ImmixBlockAcquireKind::GcCopyFresh,
                    &status),
                SideMetadataResult::Success);
            Expect(
                "matrix GC fresh status",
                status == (!allocated
                    ? ImmixBlockOperationStatus::Updated
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (!allocated)
            {
                ExpectState(
                    "matrix GC fresh target",
                    fixture,
                    DataStart,
                    ImmixBlockState::Unmarked);
                ExpectMetadata(
                    "matrix GC fresh return",
                    fixture.blocks.TryReturn(DataStart, Owner, &status),
                    SideMetadataResult::Success);
                Expect("matrix GC fresh return updated", status == ImmixBlockOperationStatus::Updated);
            }

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix GC reusable acquire",
                fixture.blocks.TryAcquire(
                    DataStart,
                    Owner,
                    ImmixBlockAcquireKind::GcCopyReusable,
                    &status),
                SideMetadataResult::Success);
            Expect(
                "matrix GC reusable status",
                status == (allocated
                    ? ImmixBlockOperationStatus::Updated
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (allocated)
            {
                ExpectState(
                    "matrix GC reusable target",
                    fixture,
                    DataStart,
                    ImmixBlockState::Unmarked);
                ExpectMetadata(
                    "matrix GC reusable return",
                    fixture.blocks.TryReturn(DataStart, Owner, &status),
                    SideMetadataResult::Success);
                Expect(
                    "matrix GC reusable return updated",
                    status == ImmixBlockOperationStatus::Updated);
            }

            SeedState(fixture, DataStart, rawState, Owner);
            ExpectMetadata(
                "matrix release",
                fixture.blocks.TryRelease(DataStart, Owner, &status),
                SideMetadataResult::Success);
            Expect("matrix release updated", status == ImmixBlockOperationStatus::Updated);
            ExpectState(
                "matrix release target",
                fixture,
                DataStart,
                ImmixBlockState::Unallocated);
        }

        ExpectMetadata(
            "matrix leaves GC phase",
            fixture.blocks.ReleaseGcPause(&status),
            SideMetadataResult::Success);
        Expect("matrix mutator phase entered", status == ImmixBlockOperationStatus::Updated);

        for (uint8_t rawState : RawStates)
        {
            bool allocated = rawState != 0;
            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix mutator fresh acquire",
                fixture.blocks.TryAcquire(
                    DataStart,
                    Owner,
                    ImmixBlockAcquireKind::MutatorFresh,
                    &status),
                SideMetadataResult::Success);
            Expect(
                "matrix mutator fresh status",
                status == (!allocated
                    ? ImmixBlockOperationStatus::Updated
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (!allocated)
            {
                ExpectMetadata(
                    "matrix mutator fresh return",
                    fixture.blocks.TryReturn(DataStart, Owner, &status),
                    SideMetadataResult::Success);
                Expect(
                    "matrix mutator fresh return updated",
                    status == ImmixBlockOperationStatus::Updated);
            }

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix mutator reusable acquire",
                fixture.blocks.TryAcquire(
                    DataStart,
                    Owner,
                    ImmixBlockAcquireKind::MutatorReusable,
                    &status),
                SideMetadataResult::Success);
            Expect(
                "matrix mutator reusable status",
                status == (allocated
                    ? ImmixBlockOperationStatus::Updated
                    : ImmixBlockOperationStatus::InvalidTransition));
            if (allocated)
            {
                ExpectMetadata(
                    "matrix mutator reusable return",
                    fixture.blocks.TryReturn(DataStart, Owner, &status),
                    SideMetadataResult::Success);
                Expect(
                    "matrix mutator reusable return updated",
                    status == ImmixBlockOperationStatus::Updated);
            }

            SeedState(fixture, DataStart, rawState);
            ExpectMetadata(
                "matrix prepare wrong phase",
                fixture.blocks.PrepareForTrace(DataStart, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix prepare wrong phase status",
                status == ImmixBlockOperationStatus::InvalidTransition);
            ExpectMetadata(
                "matrix mark wrong phase",
                fixture.blocks.Mark(DataStart, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix mark wrong phase status",
                status == ImmixBlockOperationStatus::InvalidTransition);
            ExpectMetadata(
                "matrix reusable wrong phase",
                fixture.blocks.SetReusable(DataStart, 1, &status),
                SideMetadataResult::Success);
            Expect(
                "matrix reusable wrong phase status",
                status == ImmixBlockOperationStatus::InvalidTransition);
        }
    }

    void TestLogAndContention()
    {
        Fixture fixture;
        Expect("contention fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        constexpr size_t ThreadCount = 16;
        std::atomic<uint32_t> ready(0);
        std::atomic<bool> start(false);
        std::atomic<uint32_t> winners(0);
        std::atomic<uint32_t> validLosers(0);
        std::vector<std::thread> threads;
        for (size_t index = 0; index < ThreadCount; index++)
        {
            threads.emplace_back([&, index]()
            {
                ready.fetch_add(1, std::memory_order_seq_cst);
                while (!start.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }

                ImmixBlockOperationStatus status;
                SideMetadataResult result = fixture.blocks.TryAcquire(
                    DataStart,
                    0x1000 + index,
                    ImmixBlockAcquireKind::MutatorFresh,
                    &status);
                if ((result == SideMetadataResult::Success) &&
                    (status == ImmixBlockOperationStatus::Updated))
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
                }
                else if ((result == SideMetadataResult::Success) &&
                    ((status == ImmixBlockOperationStatus::Contended) ||
                     (status == ImmixBlockOperationStatus::Stale)))
                {
                    validLosers.fetch_add(1, std::memory_order_seq_cst);
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
        Expect("exactly one acquire winner", winners.load() == 1);
        Expect("all acquire losers classified", validLosers.load() == ThreadCount - 1);

        ExpectMetadata(
            "reset contender owner",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockOwner,
                DataStart,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "reset contender lock",
            fixture.metadata.Store(
                LxrSideMetadataKind::BlockInUse,
                DataStart,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent),
            SideMetadataResult::Success);
        ExpectMetadata(
            "unlog before contention",
            fixture.blocks.Unlog(DataStart),
            SideMetadataResult::Success);

        ready.store(0);
        start.store(false);
        winners.store(0);
        threads.clear();
        for (size_t index = 0; index < ThreadCount; index++)
        {
            threads.emplace_back([&]()
            {
                ready.fetch_add(1, std::memory_order_seq_cst);
                while (!start.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }

                bool logged = false;
                SideMetadataResult result = fixture.blocks.TryLog(DataStart, &logged);
                if ((result == SideMetadataResult::Success) && logged)
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
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
        Expect("exactly one log winner", winners.load() == 1);

        ready.store(0);
        start.store(false);
        winners.store(0);
        threads.clear();
        for (size_t index = 0; index < ThreadCount; index++)
        {
            threads.emplace_back([&, index]()
            {
                ready.fetch_add(1, std::memory_order_seq_cst);
                while (!start.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }

                uintptr_t block =
                    DataStart + index * ImmixBlockGeometry::BlockBytes;
                uintptr_t owner = 0x4000 + index;
                ImmixBlockOperationStatus status;
                SideMetadataResult result = fixture.blocks.TryAcquire(
                    block,
                    owner,
                    ImmixBlockAcquireKind::MutatorFresh,
                    &status);
                if ((result == SideMetadataResult::Success) &&
                    (status == ImmixBlockOperationStatus::Updated) &&
                    (fixture.blocks.TryRelease(block, owner, &status) ==
                     SideMetadataResult::Success) &&
                    (status == ImmixBlockOperationStatus::Updated))
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
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
        Expect("neighboring block updates all win", winners.load() == ThreadCount);

        ready.store(0);
        start.store(false);
        winners.store(0);
        validLosers.store(0);
        threads.clear();
        for (size_t index = 0; index < ThreadCount; index++)
        {
            threads.emplace_back([&]()
            {
                ready.fetch_add(1, std::memory_order_seq_cst);
                while (!start.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }

                ImmixBlockOperationStatus status;
                SideMetadataResult result = fixture.blocks.StartGcPause(&status);
                if ((result == SideMetadataResult::Success) &&
                    (status == ImmixBlockOperationStatus::Updated))
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
                }
                else if ((result == SideMetadataResult::Success) &&
                    ((status == ImmixBlockOperationStatus::InvalidTransition) ||
                     (status == ImmixBlockOperationStatus::Contended)))
                {
                    validLosers.fetch_add(1, std::memory_order_seq_cst);
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
        Expect("exactly one pause-start winner", winners.load() == 1);
        Expect("pause-start losers classified", validLosers.load() == ThreadCount - 1);
        Expect("pause-start increments once", fixture.blocks.GetGlobalPhaseEpoch() == 2);

        ready.store(0);
        start.store(false);
        winners.store(0);
        validLosers.store(0);
        threads.clear();
        for (size_t index = 0; index < ThreadCount; index++)
        {
            threads.emplace_back([&]()
            {
                ready.fetch_add(1, std::memory_order_seq_cst);
                while (!start.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }

                ImmixBlockOperationStatus status;
                SideMetadataResult result = fixture.blocks.ReleaseGcPause(&status);
                if ((result == SideMetadataResult::Success) &&
                    (status == ImmixBlockOperationStatus::Updated))
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
                }
                else if ((result == SideMetadataResult::Success) &&
                    ((status == ImmixBlockOperationStatus::InvalidTransition) ||
                     (status == ImmixBlockOperationStatus::Contended)))
                {
                    validLosers.fetch_add(1, std::memory_order_seq_cst);
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
        Expect("exactly one pause-release winner", winners.load() == 1);
        Expect("pause-release losers classified", validLosers.load() == ThreadCount - 1);
        Expect("pause-release increments once", fixture.blocks.GetGlobalPhaseEpoch() == 3);
    }

    void TestEpochWrap()
    {
        Fixture fixture;
        Expect("wrap fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        constexpr uintptr_t Owner = 0x777;
        uintptr_t oldBlock = DataStart;
        uintptr_t currentBlock = DataStart + ImmixBlockGeometry::BlockBytes;
        ImmixBlockOperationStatus status;
        uint8_t epoch;
        bool predicate;

        ExpectMetadata(
            "stamp epoch one",
            fixture.blocks.TryAcquire(
                oldBlock,
                Owner,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        ExpectMetadata(
            "return epoch one block",
            fixture.blocks.TryReturn(oldBlock, Owner, &status),
            SideMetadataResult::Success);
        ExpectMetadata(
            "commit unrelated non-block range",
            fixture.metadata.CommitDataRange(DataStart + DataSize + 1, 100),
            SideMetadataResult::Success);

        for (size_t cycle = 0; cycle < 126; cycle++)
        {
            ExpectMetadata(
                "wrap-cycle start",
                fixture.blocks.StartGcPause(&status),
                SideMetadataResult::Success);
            Expect("wrap-cycle start updated", status == ImmixBlockOperationStatus::Updated);
            ExpectMetadata(
                "wrap-cycle release",
                fixture.blocks.ReleaseGcPause(&status),
                SideMetadataResult::Success);
            Expect("wrap-cycle release updated", status == ImmixBlockOperationStatus::Updated);
        }
        Expect("epoch reaches 253", fixture.blocks.GetGlobalPhaseEpoch() == 253);

        ExpectMetadata(
            "stamp epoch 253",
            fixture.blocks.TryAcquire(
                currentBlock,
                Owner,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        ExpectMetadata(
            "return epoch 253 block",
            fixture.blocks.TryReturn(currentBlock, Owner, &status),
            SideMetadataResult::Success);
        ExpectMetadata(
            "start epoch 254",
            fixture.blocks.StartGcPause(&status),
            SideMetadataResult::Success);
        Expect("epoch reaches 254", fixture.blocks.GetGlobalPhaseEpoch() == 254);
        ExpectMetadata(
            "epoch 253 remains nursery during GC",
            fixture.blocks.IsNursery(currentBlock, &predicate),
            SideMetadataResult::Success);
        Expect("epoch 253 nursery predicate", predicate);

        ExpectMetadata(
            "wrap release",
            fixture.blocks.ReleaseGcPause(&status),
            SideMetadataResult::Success);
        Expect("wrap returns to epoch one", fixture.blocks.GetGlobalPhaseEpoch() == 1);
        ExpectMetadata(
            "old block epoch reset",
            fixture.blocks.GetBlockPhaseEpoch(oldBlock, &epoch),
            SideMetadataResult::Success);
        Expect("old block epoch is zero", epoch == 0);
        ExpectMetadata(
            "current block epoch reset",
            fixture.blocks.GetBlockPhaseEpoch(currentBlock, &epoch),
            SideMetadataResult::Success);
        Expect("current block epoch is zero", epoch == 0);
        ExpectMetadata(
            "old block is not post-wrap nursery",
            fixture.blocks.IsNursery(oldBlock, &predicate),
            SideMetadataResult::Success);
        Expect("old block does not alias wrap", !predicate);
        ExpectMetadata(
            "post-wrap acquire",
            fixture.blocks.TryAcquire(
                oldBlock,
                Owner,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::Success);
        Expect("post-wrap acquire updated", status == ImmixBlockOperationStatus::Updated);
        ExpectMetadata(
            "post-wrap epoch load",
            fixture.blocks.GetBlockPhaseEpoch(oldBlock, &epoch),
            SideMetadataResult::Success);
        Expect("post-wrap epoch is one", epoch == 1);
    }

    void TestFailuresAndManagerLifetime()
    {
        LxrSideMetadataLayout layout;
        ExpectMetadata(
            "failure layout",
            LxrSideMetadataLayout::Create(1, &layout),
            SideMetadataResult::Success);

        SideMetadataManager first;
        ExpectMetadata(
            "first manager initialize",
            first.Initialize(&layout),
            SideMetadataResult::Success);
        SideMetadataManager second;
        ExpectMetadata(
            "second manager collision",
            second.Initialize(&layout),
            SideMetadataResult::ReservationFailed);
        first.Shutdown();
        ExpectMetadata(
            "second manager after shutdown",
            second.Initialize(&layout),
            SideMetadataResult::Success);
        second.Shutdown();

        constexpr LxrSideMetadataKind RequiredKinds[] = {
            LxrSideMetadataKind::BlockDefrag,
            LxrSideMetadataKind::BlockMark,
            LxrSideMetadataKind::BlockLog,
            LxrSideMetadataKind::NurseryPromotion,
            LxrSideMetadataKind::PhaseEpoch,
            LxrSideMetadataKind::BlockOwner,
            LxrSideMetadataKind::BlockInUse,
        };
        uint64_t allMask =
            (UINT64_C(1) << LxrSideMetadataLayout::SpecCount) - 1;
        for (LxrSideMetadataKind missing : RequiredKinds)
        {
            Fixture fixture(allMask & ~KindMask(missing));
            Expect("required metadata missing fails registration", !fixture.initialized);
            Expect(
                "missing metadata does not admit data range",
                !fixture.metadata.IsDataRangeOwned(DataStart, DataSize));
        }

        Fixture fixture;
        Expect("failure fixture initialized", fixture.initialized);
        if (!fixture.initialized)
        {
            return;
        }

        ImmixBlockOperationStatus status;
        ImmixBlockState state;
        uint8_t unavailableLines;
        ExpectMetadata(
            "unowned block rejected",
            fixture.blocks.GetState(
                DataStart + DataSize,
                &state,
                &unavailableLines),
            SideMetadataResult::AddressNotOwned);
        ExpectMetadata(
            "misaligned range rejected",
            fixture.blocks.RegisterBlockRange(
                DataStart + 1,
                ImmixBlockGeometry::BlockBytes),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "zero range rejected",
            fixture.blocks.RegisterBlockRange(DataStart, 0),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "range beyond limit rejected",
            fixture.blocks.RegisterBlockRange(
                ImmixBlockGeometry::AddressLimit -
                    ImmixBlockGeometry::BlockBytes,
                ImmixBlockGeometry::BlockBytes * 2),
            SideMetadataResult::AddressOverflow);
        ExpectMetadata(
            "null status pause rejected",
            fixture.blocks.StartGcPause(nullptr),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "null status acquire rejected",
            fixture.blocks.TryAcquire(
                DataStart,
                1,
                ImmixBlockAcquireKind::MutatorFresh,
                nullptr),
            SideMetadataResult::InvalidArgument);
        ExpectMetadata(
            "zero owner rejected",
            fixture.blocks.TryAcquire(
                DataStart,
                0,
                ImmixBlockAcquireKind::MutatorFresh,
                &status),
            SideMetadataResult::InvalidArgument);

        fixture.blocks.Shutdown();
        ExpectMetadata(
            "operation after block-manager shutdown",
            fixture.blocks.GetState(DataStart, &state, &unavailableLines),
            SideMetadataResult::InvalidArgument);
    }
#endif // HOST_64BIT
}

int main()
{
#ifdef HOST_64BIT
    TestGeometry();
    TestStateEncoding();
    TestLifecycleAndTransitions();
    TestTransitionMatrix();
    TestLogAndContention();
    TestEpochWrap();
    TestFailuresAndManagerLifetime();
#else
    LxrSideMetadataLayout layout;
    ExpectMetadata(
        "32-bit layout fails closed",
        LxrSideMetadataLayout::Create(1, &layout),
        SideMetadataResult::UnsupportedAddressSpace);
#endif

    if (s_failures == 0)
    {
        printf("%d/%d immix block checks passed\n", s_checks, s_checks);
        return 0;
    }

    printf("%d/%d immix block checks passed\n", s_checks - s_failures, s_checks);
    return 1;
}

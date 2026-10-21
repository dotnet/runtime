// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/large_object_space.h"

#include <atomic>
#include <cstdio>
#include <cstring>
#include <thread>

namespace
{
    int s_checks;
    int s_failures;

    void Check(bool condition, const char* name)
    {
        s_checks++;
        if (!condition)
        {
            s_failures++;
            std::printf("FAIL: %s\n", name);
        }
    }

    uintptr_t Load(
        SideMetadataManager& metadata,
        LxrSideMetadataKind kind,
        uintptr_t address)
    {
        uintptr_t value = UINTPTR_MAX;
        Check(
            metadata.Load(
                kind,
                address,
                SideMetadataMemoryOrder::SequentiallyConsistent,
                &value) == SideMetadataResult::Success,
            "metadata load");
        return value;
    }
}

int main()
{
#ifndef HOST_64BIT
    LxrSideMetadataLayout unsupported;
    Check(
        LxrSideMetadataLayout::Create(1, &unsupported) ==
            SideMetadataResult::UnsupportedAddressSpace,
        "x86 fails closed");
    std::printf("%d/%d large object space checks passed\n", s_checks - s_failures, s_checks);
    return s_failures == 0 ? 0 : 1;
#else
    Check(!LxrLargeObjectSpace::ShouldUseLargeObjectSpace(32767), "threshold minus one");
    Check(LxrLargeObjectSpace::ShouldUseLargeObjectSpace(32768), "threshold exact");
    Check(LxrLargeObjectSpace::ShouldUseLargeObjectSpace(32769), "threshold plus one");
    if (s_failures != 0)
    {
        std::printf("%d/%d large object space checks passed\n", s_checks - s_failures, s_checks);
        return 1;
    }

    LxrLargeObjectAllocation geometry;
    Check(
        LxrLargeObjectSpace::ComputeAllocationGeometry(
            UINT64_C(0x0000000200000000),
            32768,
            64,
            8,
            &geometry) == LxrLargeObjectResult::Success,
        "geometry aligned");
    Check(((geometry.object + 8) & 63) == 0, "geometry offset alignment");
    Check(geometry.page_count == 9, "geometry page count");
    Check(
        LxrLargeObjectSpace::ComputeAllocationGeometry(
            UINT64_C(0x0000000200000000),
            32768,
            LxrLargeObjectSpace::PageBytes * 2,
            0,
            &geometry) == LxrLargeObjectResult::InvalidArgument,
        "geometry rejects super-page alignment");
    Check(
        LxrLargeObjectSpace::ComputeAllocationGeometry(
            LxrSideMetadataLayout::AddressLimit - LxrLargeObjectSpace::PageBytes,
            32768,
            8,
            0,
            &geometry) == LxrLargeObjectResult::AddressOverflow,
        "geometry address limit");
    Check(
        LxrLargeObjectSpace::ComputeAllocationGeometry(
            UINT64_C(0x0000000200000000),
            SIZE_MAX,
            8,
            0,
            &geometry) == LxrLargeObjectResult::AddressOverflow,
        "geometry size overflow");

    for (uint8_t rcWidth = 1; rcWidth <= 3; rcWidth++)
    {
        LxrSideMetadataLayout layout;
        Check(
            LxrSideMetadataLayout::Create(rcWidth, &layout) == SideMetadataResult::Success,
            "layout creation");

        SideMetadataManager metadata;
        Check(
            metadata.Initialize(&layout) == SideMetadataResult::Success,
            "metadata initialization");

        LxrLargeObjectSpace initializationSpace;
        std::atomic<LxrLargeObjectResult> firstInitialization;
        std::atomic<LxrLargeObjectResult> secondInitialization;
        std::thread firstInitializer([&]()
        {
            firstInitialization.store(
                initializationSpace.Initialize(&metadata, 16 * 1024 * 1024),
                std::memory_order_relaxed);
        });
        std::thread secondInitializer([&]()
        {
            secondInitialization.store(
                initializationSpace.Initialize(&metadata, 16 * 1024 * 1024),
                std::memory_order_relaxed);
        });
        firstInitializer.join();
        secondInitializer.join();
        LxrLargeObjectResult firstResult =
            firstInitialization.load(std::memory_order_relaxed);
        LxrLargeObjectResult secondResult =
            secondInitialization.load(std::memory_order_relaxed);
        Check(
            ((firstResult == LxrLargeObjectResult::Success) &&
             (secondResult == LxrLargeObjectResult::InvalidPhase)) ||
                ((firstResult == LxrLargeObjectResult::InvalidPhase) &&
                 (secondResult == LxrLargeObjectResult::Success)),
            "concurrent initialization has one winner");
        initializationSpace.Shutdown();

        LxrLargeObjectSpace space;
        Check(
            space.Initialize(&metadata, 16 * 1024 * 1024) ==
                LxrLargeObjectResult::Success,
            "space initialization");

        constexpr uintptr_t Owner = 0x2500;
        LxrLargeObjectAllocation below;
        Check(
            space.Allocate(32767, 8, 0, Owner, &below) ==
                LxrLargeObjectResult::BelowThreshold,
            "allocation threshold minus one");

        LxrLargeObjectAllocation first;
        Check(
            space.Allocate(32768, 8, 0, Owner, &first) ==
                LxrLargeObjectResult::Success,
            "allocation threshold exact");
        Check(first.page_count == 8, "exact threshold pages");
        Check(
            reinterpret_cast<uint8_t*>(first.object)[0] == 0 &&
                reinterpret_cast<uint8_t*>(first.object)[first.object_bytes - 1] == 0,
            "fresh allocation zero");
        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectMarkNursery, first.object) ==
                LxrLargeObjectSpace::NurseryBit,
            "fresh nursery state");
        Check(
            Load(metadata, LxrSideMetadataKind::ValidObject, first.object) == 1,
            "fresh valid object");
        Check(
            Load(metadata, LxrSideMetadataKind::ReferenceCount, first.object) == 0,
            "fresh reference count");
        Check(
            Load(metadata, LxrSideMetadataKind::FieldUnlogged, first.object) == 0,
            "fresh field log");
        Check(
            space.TryRelease(first.object, Owner) == LxrLargeObjectResult::InvalidPhase,
            "release phase restriction");

        std::memset(
            reinterpret_cast<void*>(first.object),
            0xa5,
            first.object_bytes);
        Check(
            space.BeginCollection(false, false) == LxrLargeObjectResult::Success,
            "begin release collection");
        Check(
            space.TryRelease(first.object, Owner + 1) == LxrLargeObjectResult::NotOwner,
            "release owner restriction");
        Check(
            space.TryRelease(first.object, Owner) == LxrLargeObjectResult::Success,
            "release first");
        Check(
            space.EndCollection() == LxrLargeObjectResult::Success,
            "end release collection");

        uintptr_t neighboringPage = first.page_start +
            (first.page_count * LxrLargeObjectSpace::PageBytes);
        Check(
            metadata.Store(
                LxrSideMetadataKind::LargeObjectPageReuse,
                first.page_start,
                UINT8_MAX,
                SideMetadataMemoryOrder::SequentiallyConsistent) ==
                SideMetadataResult::Success,
            "seed reuse wrap");
        Check(
            metadata.Store(
                LxrSideMetadataKind::LargeObjectPageReuse,
                neighboringPage,
                17,
                SideMetadataMemoryOrder::SequentiallyConsistent) ==
                SideMetadataResult::Success,
            "seed neighboring reuse");

        Check(
            space.BeginCollection(true, true) == LxrLargeObjectResult::Success,
            "begin concurrent full trace");
        LxrLargeObjectAllocation recycled;
        Check(
            space.Allocate(32768, 8, 0, Owner, &recycled) ==
                LxrLargeObjectResult::Success,
            "recycled allocation");
        Check(recycled.page_start == first.page_start, "recycled address");
        Check(
            reinterpret_cast<uint8_t*>(recycled.object)[0] == 0 &&
                reinterpret_cast<uint8_t*>(recycled.object)[recycled.object_bytes - 1] == 0,
            "recycled allocation zero");
        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectPageReuse, recycled.page_start) == 0,
            "reuse wraps");
        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectPageReuse, neighboringPage) == 17,
            "neighboring reuse unchanged");
        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectMarkNursery, recycled.object) ==
                (LxrLargeObjectSpace::MarkBit | LxrLargeObjectSpace::NurseryBit),
            "concurrent nursery marked");

        bool updated;
        Check(
            space.TryMarkAndPromote(recycled.object, Owner, &updated) ==
                LxrLargeObjectResult::InvalidPhase,
            "promotion phase restriction");
        Check(space.BeginRelease() == LxrLargeObjectResult::Success, "begin release");
        Check(
            space.TryMarkAndPromote(recycled.object, Owner, &updated) ==
                LxrLargeObjectResult::Success &&
                updated,
            "mark and promote");
        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectMarkNursery, recycled.object) ==
                LxrLargeObjectSpace::MarkBit,
            "promoted state");
        Check(
            Load(metadata, LxrSideMetadataKind::FieldUnlogged, recycled.object) == 1,
            "promotion initializes field log");
        Check(
            space.TryMarkAndPromote(recycled.object, Owner, &updated) ==
                LxrLargeObjectResult::Success &&
                !updated,
            "mark and promote idempotent");
        Check(
            space.TryRelease(recycled.object, Owner) == LxrLargeObjectResult::Success,
            "release recycled");
        Check(space.EndCollection() == LxrLargeObjectResult::Success, "end collection");

        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectMarkNursery, recycled.object) == 0,
            "release clears mark nursery");
        Check(
            Load(metadata, LxrSideMetadataKind::ValidObject, recycled.object) == 0,
            "release clears valid object");
        Check(
            Load(metadata, LxrSideMetadataKind::LargeObjectPageReuse, recycled.page_start) == 0,
            "release preserves reuse");

        LxrLargeObjectAllocation thresholdPlusOne;
        Check(
            space.Allocate(32769, 8, 0, Owner, &thresholdPlusOne) ==
                LxrLargeObjectResult::Success,
            "threshold plus one allocation");
        Check(thresholdPlusOne.object_bytes == 32769, "threshold plus one logical size");
        Check(thresholdPlusOne.page_count == 9, "threshold plus one page span");
        Check(
            Load(
                metadata,
                LxrSideMetadataKind::LargeObjectMarkNursery,
                thresholdPlusOne.object) ==
                (LxrLargeObjectSpace::MarkBit | LxrLargeObjectSpace::NurseryBit),
            "threshold plus one nursery state");
        Check(
            space.BeginCollection(false, false) == LxrLargeObjectResult::Success,
            "threshold plus one begin release");
        Check(
            space.TryMarkAndPromote(thresholdPlusOne.object, Owner, &updated) ==
                LxrLargeObjectResult::Success &&
                updated,
            "threshold plus one promote");
        Check(
            Load(
                metadata,
                LxrSideMetadataKind::FieldUnlogged,
                thresholdPlusOne.object) == 1,
            "threshold plus one first field log");
        Check(
            Load(
                metadata,
                LxrSideMetadataKind::FieldUnlogged,
                thresholdPlusOne.object + 32768) == 1,
            "threshold plus one rounded field log");
        Check(
            Load(
                metadata,
                LxrSideMetadataKind::LargeObjectMarkNursery,
                thresholdPlusOne.object) == LxrLargeObjectSpace::MarkBit,
            "threshold plus one promoted state");
        Check(
            space.TryRelease(thresholdPlusOne.object, Owner) ==
                LxrLargeObjectResult::Success,
            "threshold plus one release");
        Check(
            space.EndCollection() == LxrLargeObjectResult::Success,
            "threshold plus one end collection");

        space.Shutdown();
        metadata.Shutdown();
    }

    std::printf("%d/%d large object space checks passed\n", s_checks - s_failures, s_checks);
    return s_failures == 0 ? 0 : 1;
#endif
}

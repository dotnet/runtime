// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "immix_allocation.h"

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
#ifdef HOST_64BIT
    constexpr size_t TestBlockCount = 2;
    constexpr size_t TestArenaSize =
        TestBlockCount * ImmixBlockGeometry::BlockBytes;
    constexpr uintptr_t TestOwner = 0x2400;
#endif

}

extern "C" DLLEXPORT int32_t GC_ImmixAllocationTest_Run()
{
#ifndef HOST_64BIT
    LxrSideMetadataLayout unsupportedLayout;
    return LxrSideMetadataLayout::Create(1, &unsupportedLayout) ==
        SideMetadataResult::UnsupportedAddressSpace ? 0 : 1;
#else
    uint8_t* arena = static_cast<uint8_t*>(GCToOSInterface::VirtualReserve(
        TestArenaSize,
        ImmixBlockGeometry::BlockBytes,
        VirtualReserveFlags::None));
    if (arena == nullptr)
    {
        return 2;
    }
    if (reinterpret_cast<uintptr_t>(arena) >=
        ImmixBlockGeometry::AddressLimit)
    {
        GCToOSInterface::VirtualRelease(arena, TestArenaSize);
        return 2;
    }
    if (!GCToOSInterface::VirtualCommit(arena, TestArenaSize))
    {
        GCToOSInterface::VirtualRelease(arena, TestArenaSize);
        return 3;
    }

    int32_t exitCode = 4;
    LxrSideMetadataLayout layout;
    SideMetadataManager metadata;
    ImmixBlockManager blocks;
    ImmixAllocationManager allocation;
    ImmixBumpAllocator allocator;
    uintptr_t arenaAddress = reinterpret_cast<uintptr_t>(arena);
    ImmixAllocationStatus status =
        ImmixAllocationStatus::InvalidRequest;
    ImmixAllocationRequest request = {
        MIN_OBJECT_SIZE,
        DATA_ALIGNMENT,
        0,
    };
    SideMetadataResult refillResult;
    SideMetadataResult resetResult;
    uintptr_t object = 0;
    if (LxrSideMetadataLayout::Create(1, &layout) !=
        SideMetadataResult::Success)
    {
        goto Exit;
    }
    if (metadata.Initialize(&layout) != SideMetadataResult::Success)
    {
        exitCode = 5;
        goto Exit;
    }
    if (blocks.Initialize(&metadata) != SideMetadataResult::Success)
    {
        exitCode = 6;
        goto Exit;
    }
    if (blocks.RegisterBlockRange(arenaAddress, TestArenaSize) !=
        SideMetadataResult::Success)
    {
        exitCode = 7;
        goto Exit;
    }
    if (allocation.Initialize(
            &metadata,
            &blocks,
            arenaAddress,
            TestArenaSize,
            1) != SideMetadataResult::Success)
    {
        exitCode = 8;
        goto Exit;
    }
    if (allocator.Initialize(&allocation, TestOwner) !=
        SideMetadataResult::Success)
    {
        exitCode = 9;
        goto Exit;
    }
    memset(arena, 0x5a, TestArenaSize);
    refillResult = allocator.TryRefill(
        ImmixAllocationSource::MutatorFresh,
        ImmixMarkingContext::Inactive,
        &status);
    if (refillResult != SideMetadataResult::Success)
    {
        exitCode = 100 + static_cast<int32_t>(refillResult);
        goto Exit;
    }
    if (status != ImmixAllocationStatus::Ready)
    {
        exitCode = 200 + static_cast<int32_t>(status);
        goto Exit;
    }
    for (size_t i = 0; i < ImmixBlockGeometry::BlockBytes; i++)
    {
        if (arena[i] != 0)
        {
            exitCode = 11;
            goto Exit;
        }
    }

    if ((allocator.TryAllocate(request, &object, &status) !=
         SideMetadataResult::Success) ||
        (status != ImmixAllocationStatus::Allocated) ||
        (object != arenaAddress))
    {
        exitCode = 12;
        goto Exit;
    }
    resetResult = allocator.Reset(&status);
    if ((resetResult != SideMetadataResult::Success) ||
        (status != ImmixAllocationStatus::Ready))
    {
        exitCode = 13;
        goto Exit;
    }

    exitCode = 0;

Exit:
    allocation.Shutdown();
    blocks.Shutdown();
    metadata.Shutdown();
    GCToOSInterface::VirtualRelease(arena, TestArenaSize);
    return exitCode;
#endif
}

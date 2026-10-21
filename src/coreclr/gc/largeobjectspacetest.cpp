// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "large_object_space.h"

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

extern "C" DLLEXPORT int32_t GC_LargeObjectSpaceTest_Run()
{
#ifndef HOST_64BIT
    LxrSideMetadataLayout unsupported;
    return LxrSideMetadataLayout::Create(1, &unsupported) ==
        SideMetadataResult::UnsupportedAddressSpace ? 0 : 1;
#else
    LxrSideMetadataLayout layout;
    if (LxrSideMetadataLayout::Create(1, &layout) != SideMetadataResult::Success)
    {
        return 2;
    }

    SideMetadataManager metadata;
    if (metadata.Initialize(&layout) != SideMetadataResult::Success)
    {
        return 3;
    }

    LxrLargeObjectSpace space;
    if (space.Initialize(&metadata, 4 * 1024 * 1024) != LxrLargeObjectResult::Success)
    {
        metadata.Shutdown();
        return 4;
    }

    constexpr uintptr_t Owner = 0x2500;
    LxrLargeObjectAllocation first;
    if (space.Allocate(
            LxrLargeObjectSpace::MinimumObjectBytes,
            sizeof(uintptr_t),
            0,
            Owner,
            &first) != LxrLargeObjectResult::Success)
    {
        return 5;
    }
    if ((reinterpret_cast<uint8_t*>(first.object)[0] != 0) ||
        (reinterpret_cast<uint8_t*>(first.object)[first.object_bytes - 1] != 0))
    {
        return 6;
    }
    memset(reinterpret_cast<void*>(first.object), 0xa5, first.object_bytes);

    if ((space.BeginCollection(false, false) != LxrLargeObjectResult::Success) ||
        (space.TryRelease(first.object, Owner) != LxrLargeObjectResult::Success) ||
        (space.EndCollection() != LxrLargeObjectResult::Success))
    {
        return 7;
    }
    if (space.BeginCollection(true, true) != LxrLargeObjectResult::Success)
    {
        return 8;
    }

    LxrLargeObjectAllocation recycled;
    if ((space.Allocate(
            LxrLargeObjectSpace::MinimumObjectBytes,
            sizeof(uintptr_t),
            0,
            Owner,
            &recycled) != LxrLargeObjectResult::Success) ||
        (recycled.page_start != first.page_start) ||
        (reinterpret_cast<uint8_t*>(recycled.object)[0] != 0) ||
        (reinterpret_cast<uint8_t*>(recycled.object)[recycled.object_bytes - 1] != 0))
    {
        return 9;
    }

    uintptr_t reuse;
    if ((metadata.Load(
            LxrSideMetadataKind::LargeObjectPageReuse,
            recycled.page_start,
            SideMetadataMemoryOrder::SequentiallyConsistent,
            &reuse) != SideMetadataResult::Success) ||
        (reuse != 1))
    {
        return 10;
    }

    bool updated;
    if ((space.BeginRelease() != LxrLargeObjectResult::Success) ||
        (space.TryMarkAndPromote(recycled.object, Owner, &updated) !=
            LxrLargeObjectResult::Success) ||
        !updated ||
        (space.TryRelease(recycled.object, Owner) != LxrLargeObjectResult::Success) ||
        (space.EndCollection() != LxrLargeObjectResult::Success))
    {
        return 11;
    }

    space.Shutdown();
    metadata.Shutdown();
    return 0;
#endif
}

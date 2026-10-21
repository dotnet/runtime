// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "immix_block.h"

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
    constexpr uintptr_t TestDataStart = UINT64_C(0x0000000200000000);
    constexpr size_t TestDataSize = 2 * ImmixBlockGeometry::BlockBytes;
#endif

    bool IsSuccess(
        SideMetadataResult result,
        ImmixBlockOperationStatus status,
        ImmixBlockOperationStatus expectedStatus)
    {
        return (result == SideMetadataResult::Success) && (status == expectedStatus);
    }
}

extern "C" DLLEXPORT int32_t GC_ImmixBlockStateTest_Run()
{
#ifndef HOST_64BIT
    LxrSideMetadataLayout unsupportedLayout;
    return LxrSideMetadataLayout::Create(1, &unsupportedLayout) ==
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

    ImmixBlockManager blocks;
    if (blocks.Initialize(&metadata) != SideMetadataResult::Success)
    {
        return 4;
    }
    if (blocks.RegisterBlockRange(TestDataStart, TestDataSize) != SideMetadataResult::Success)
    {
        return 5;
    }

    constexpr uintptr_t Owner = 0x1234;
    ImmixBlockOperationStatus status;
    SideMetadataResult result = blocks.TryAcquire(
        TestDataStart,
        Owner,
        ImmixBlockAcquireKind::MutatorFresh,
        &status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 6;
    }

    bool predicate;
    if ((blocks.IsNursery(TestDataStart, &predicate) != SideMetadataResult::Success) ||
        !predicate)
    {
        return 7;
    }
    result = blocks.TryReturn(TestDataStart, Owner, &status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 8;
    }
    result = blocks.StartGcPause(&status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 9;
    }
    result = blocks.TryPromoteInPlace(TestDataStart, &status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 10;
    }
    if ((blocks.IsGcReusing(TestDataStart, &predicate) != SideMetadataResult::Success) ||
        !predicate)
    {
        return 11;
    }
    result = blocks.Mark(TestDataStart, &status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 12;
    }
    result = blocks.SetReusable(TestDataStart, 1, &status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 13;
    }
    result = blocks.ReleaseGcPause(&status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 14;
    }

    bool logged;
    if ((blocks.TryLog(TestDataStart, &logged) != SideMetadataResult::Success) ||
        !logged)
    {
        return 15;
    }
    if ((blocks.TryLog(TestDataStart, &logged) != SideMetadataResult::Success) ||
        logged)
    {
        return 16;
    }
    if (blocks.Unlog(TestDataStart) != SideMetadataResult::Success)
    {
        return 17;
    }

    for (size_t cycle = 0; cycle < 125; cycle++)
    {
        result = blocks.StartGcPause(&status);
        if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
        {
            return 18;
        }
        result = blocks.ReleaseGcPause(&status);
        if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
        {
            return 18;
        }
    }
    if (blocks.GetGlobalPhaseEpoch() != 253)
    {
        return 19;
    }
    result = blocks.StartGcPause(&status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 20;
    }
    result = blocks.ReleaseGcPause(&status);
    if (!IsSuccess(result, status, ImmixBlockOperationStatus::Updated))
    {
        return 20;
    }
    if (blocks.GetGlobalPhaseEpoch() != ImmixBlockManager::InitialPhaseEpoch)
    {
        return 21;
    }

    uint8_t blockEpoch;
    if ((blocks.GetBlockPhaseEpoch(TestDataStart, &blockEpoch) != SideMetadataResult::Success) ||
        (blockEpoch != 0))
    {
        return 22;
    }

    blocks.Shutdown();
    metadata.Shutdown();
    return 0;
#endif
}

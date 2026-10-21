// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "reference_count.h"

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
    constexpr uintptr_t TestDataStart = UINT64_C(0x0000000300000000);
    constexpr size_t TestDataSize = 2 * ImmixBlockGeometry::BlockBytes;
#endif

    volatile int32_t s_invariant_checks;

    bool ValidateOldReference(
        ReferenceCountTable& table,
        uintptr_t oldReference,
        SideMetadataResult* result)
    {
        s_invariant_checks++;
        if (oldReference == 0)
        {
            *result = SideMetadataResult::Success;
            return true;
        }

        uintptr_t count;
        *result = table.Load(oldReference, &count);
        if (*result != SideMetadataResult::Success)
        {
            return false;
        }

        bool valid = count != 0;
        _ASSERTE(valid && "old reference must not have a zero reference count");
        return valid;
    }
}

extern "C" DLLEXPORT int32_t GC_ReferenceCountTest_Run()
{
#ifndef HOST_64BIT
    LxrSideMetadataLayout unsupportedLayout;
    return LxrSideMetadataLayout::Create(1, &unsupportedLayout) ==
        SideMetadataResult::UnsupportedAddressSpace ? 0 : 1;
#else
    for (uint8_t logBits = 1; logBits <= 3; logBits++)
    {
        LxrSideMetadataLayout layout;
        if (LxrSideMetadataLayout::Create(logBits, &layout) !=
            SideMetadataResult::Success)
        {
            return 2;
        }

        SideMetadataManager metadata;
        if ((metadata.Initialize(&layout) != SideMetadataResult::Success) ||
            (metadata.CommitDataRange(TestDataStart, TestDataSize) !=
             SideMetadataResult::Success))
        {
            return 3;
        }

        ReferenceCountTable table;
        if (table.Initialize(&metadata, &layout) != SideMetadataResult::Success)
        {
            return 4;
        }

        uintptr_t object = TestDataStart + 8;
        constexpr size_t ObjectSize = ImmixBlockGeometry::LineBytes * 2;
        if ((table.InitializeObjectQuiescent(object, ObjectSize) !=
             SideMetadataResult::Success) ||
            (table.MarkStraddleLinesQuiescent(object, ObjectSize) !=
             SideMetadataResult::Success))
        {
            return 5;
        }

        uintptr_t previous;
        SideMetadataUpdateStatus status;
        for (uintptr_t count = 0; count < table.GetMaximumCount(); count++)
        {
            if ((table.Increment(object, &previous, &status) !=
                 SideMetadataResult::Success) ||
                (previous != count) ||
                (status != SideMetadataUpdateStatus::Updated))
            {
                return 6;
            }
        }
        if ((table.Decrement(object, &previous, &status) !=
             SideMetadataResult::Success) ||
            (previous != table.GetMaximumCount()) ||
            (status != SideMetadataUpdateStatus::Saturated))
        {
            return 7;
        }

        bool continuation;
        uintptr_t firstContinuation =
            (object & ~ImmixBlockGeometry::LineMask) +
            ImmixBlockGeometry::LineBytes;
        if ((table.IsContinuationLine(firstContinuation, &continuation) !=
             SideMetadataResult::Success) ||
            !continuation)
        {
            return 8;
        }

        SideMetadataResult invariantResult;
        s_invariant_checks = 0;
        if (!ValidateOldReference(table, 0, &invariantResult) ||
            !ValidateOldReference(table, object, &invariantResult) ||
            (s_invariant_checks != 2))
        {
            return 9;
        }

        if (table.ResetObjectQuiescent(object, ObjectSize) !=
            SideMetadataResult::Success)
        {
            return 10;
        }
    }
    return 0;
#endif
}

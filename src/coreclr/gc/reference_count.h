// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef REFERENCE_COUNT_H
#define REFERENCE_COUNT_H

#include "immix_block.h"

using ReferenceCountDeathCallback = SideMetadataResult (*)(uintptr_t object, void* context);

class ReferenceCountTable final
{
public:
    static constexpr size_t ObjectAlignment = sizeof(uintptr_t);
    static constexpr size_t MinimumObjectSize = 3 * sizeof(uintptr_t);

    ReferenceCountTable();
    ReferenceCountTable(const ReferenceCountTable&) = delete;
    ReferenceCountTable& operator=(const ReferenceCountTable&) = delete;

    SideMetadataResult Initialize(
        SideMetadataManager* metadata,
        const LxrSideMetadataLayout* layout);
    void Shutdown();

    uint8_t GetActiveBits() const;
    uintptr_t GetMaximumCount() const;

    SideMetadataResult InitializeObjectQuiescent(uintptr_t object, size_t size);
    SideMetadataResult RegisterObjectInClearedSpan(uintptr_t object, size_t size);
    SideMetadataResult CopyObjectQuiescent(
        uintptr_t source,
        uintptr_t destination,
        size_t size);
    SideMetadataResult ResetObjectQuiescent(uintptr_t object, size_t size);
    SideMetadataResult ResetRangeQuiescent(uintptr_t start, size_t size);
    SideMetadataResult ResetBlockQuiescent(uintptr_t block);

    SideMetadataResult Load(uintptr_t object, uintptr_t* count) const;
    SideMetadataResult CompareExchange(
        uintptr_t object,
        uintptr_t value,
        uintptr_t comparand,
        uintptr_t* observed,
        bool* exchanged);
    SideMetadataResult Increment(
        uintptr_t object,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);
    SideMetadataResult Decrement(
        uintptr_t object,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);
    SideMetadataResult DecrementWithDeath(
        uintptr_t object,
        ReferenceCountDeathCallback death,
        void* context,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);
    SideMetadataResult IsSticky(uintptr_t object, bool* sticky) const;

    SideMetadataResult MarkStraddleLinesQuiescent(uintptr_t object, size_t size);
    SideMetadataResult ClearStraddleLinesQuiescent(uintptr_t object, size_t size);
    SideMetadataResult IsContinuationLine(uintptr_t line, bool* continuation) const;

private:
    SideMetadataResult ValidateAddress(
        uintptr_t object,
        bool requireRegistered) const;
    SideMetadataResult ValidateObjectRange(
        uintptr_t object,
        size_t size,
        bool requireRegistered) const;
    SideMetadataResult ValidateUnregisteredObjectRange(
        uintptr_t object,
        size_t size) const;
    SideMetadataResult ValidateContinuationLines(
        uintptr_t object,
        size_t size,
        bool requireEmpty) const;
    SideMetadataResult SetValidObject(uintptr_t object, uintptr_t value);
    SideMetadataResult MarkStraddleLinesUnchecked(uintptr_t object, size_t size);
    SideMetadataResult ClearStraddleLinesUnchecked(uintptr_t object, size_t size);
    static void GetContinuationRange(
        uintptr_t object,
        size_t size,
        uintptr_t* first,
        uintptr_t* end);

    SideMetadataManager* m_metadata;
    uint8_t m_log_reference_count_bits;
};

#ifdef HOST_64BIT
static_assert(ReferenceCountTable::MinimumObjectSize == 24);
#endif

#endif // REFERENCE_COUNT_H

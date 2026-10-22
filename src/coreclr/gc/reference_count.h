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

    // Fast path for the pause loops and the allocation path. These functions never validate the
    // address in Release builds. The caller must pass the start of an object that the engine has
    // admitted and registered (and, for the straddle functions, the size of that object). Debug
    // builds run the checked validation and assert that it succeeds, and the inline accessors assert
    // that they give the same location and value as the checked metadata access. Memory orders are
    // the same as in the checked functions.
    SideMetadataResult LoadFast(uintptr_t object, uintptr_t* count) const
    {
#ifdef _DEBUG
        // The object may be young: a young object has no ValidObject bit (P22).
        _ASSERTE(ValidateAddress(object, false) == SideMetadataResult::Success);
#endif // _DEBUG
        *count = m_rc.Load(object, SideMetadataMemoryOrder::Relaxed);
        return SideMetadataResult::Success;
    }

    SideMetadataResult IncrementFast(
        uintptr_t object,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status)
    {
#ifdef _DEBUG
        _ASSERTE(ValidateAddress(object, true) == SideMetadataResult::Success);
#endif // _DEBUG
        *status = m_rc.IncrementSaturating(object, SideMetadataMemoryOrder::Relaxed, previous);
        return SideMetadataResult::Success;
    }

    SideMetadataResult DecrementWithDeathFast(
        uintptr_t object,
        ReferenceCountDeathCallback death,
        void* context,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);

    SideMetadataResult MarkStraddleLinesFast(uintptr_t object, size_t size)
    {
#ifdef _DEBUG
        DebugValidateStraddleRange(object, size, true);
#endif // _DEBUG
        return size <= ImmixBlockGeometry::LineBytes
            ? SideMetadataResult::Success
            : MarkStraddleLinesUnchecked(object, size);
    }

    SideMetadataResult ClearStraddleLinesFast(uintptr_t object, size_t size)
    {
#ifdef _DEBUG
        DebugValidateStraddleRange(object, size, false);
#endif // _DEBUG
        return size <= ImmixBlockGeometry::LineBytes
            ? SideMetadataResult::Success
            : ClearStraddleLinesUnchecked(object, size);
    }

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
    template <bool Fast>
    SideMetadataResult DecrementWithDeathCore(
        uintptr_t object,
        ReferenceCountDeathCallback death,
        void* context,
        uintptr_t* previous,
        SideMetadataUpdateStatus* status);
#ifdef _DEBUG
    void DebugValidateStraddleRange(uintptr_t object, size_t size, bool requireEmpty) const;
#endif // _DEBUG
    static void GetContinuationRange(
        uintptr_t object,
        size_t size,
        uintptr_t* first,
        uintptr_t* end);

    SideMetadataManager* m_metadata;
    SideMetadataAccessor m_rc;
    SideMetadataAccessor m_valid;
    SideMetadataAccessor m_straddle;
    uint8_t m_log_reference_count_bits;
};

#ifdef HOST_64BIT
static_assert(ReferenceCountTable::MinimumObjectSize == 24);
#endif

#endif // REFERENCE_COUNT_H

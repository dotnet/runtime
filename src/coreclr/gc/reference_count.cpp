// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "reference_count.h"

ReferenceCountTable::ReferenceCountTable()
    : m_metadata(nullptr),
      m_log_reference_count_bits(0)
{
}

SideMetadataResult ReferenceCountTable::Initialize(
    SideMetadataManager* metadata,
    const LxrSideMetadataLayout* layout)
{
    if ((metadata == nullptr) ||
        (layout == nullptr) ||
        !metadata->IsSpecEnabled(LxrSideMetadataKind::ValidObject) ||
        !metadata->IsSpecEnabled(LxrSideMetadataKind::ReferenceCount) ||
        !metadata->IsSpecEnabled(LxrSideMetadataKind::ReferenceCountStraddle))
    {
        return SideMetadataResult::InvalidArgument;
    }

    const SideMetadataSpec* referenceCount =
        layout->GetSpec(LxrSideMetadataKind::ReferenceCount);
    if ((referenceCount == nullptr) ||
        (referenceCount->log_bits_per_value < 1) ||
        (referenceCount->log_bits_per_value > 3))
    {
        return SideMetadataResult::InvalidLayout;
    }

    m_metadata = metadata;
    m_log_reference_count_bits = referenceCount->log_bits_per_value;
    m_rc = metadata->GetAccessor(LxrSideMetadataKind::ReferenceCount);
    m_valid = metadata->GetAccessor(LxrSideMetadataKind::ValidObject);
    m_straddle = metadata->GetAccessor(LxrSideMetadataKind::ReferenceCountStraddle);
    if (!m_rc.IsValid() || !m_valid.IsValid() || !m_straddle.IsValid())
    {
        Shutdown();
        return SideMetadataResult::InvalidLayout;
    }
    return SideMetadataResult::Success;
}

void ReferenceCountTable::Shutdown()
{
    m_metadata = nullptr;
    m_rc = SideMetadataAccessor();
    m_valid = SideMetadataAccessor();
    m_straddle = SideMetadataAccessor();
    m_log_reference_count_bits = 0;
}

uint8_t ReferenceCountTable::GetActiveBits() const
{
    return static_cast<uint8_t>(1) << m_log_reference_count_bits;
}

uintptr_t ReferenceCountTable::GetMaximumCount() const
{
    uint8_t activeBits = GetActiveBits();
    return activeBits == (sizeof(uintptr_t) * 8)
        ? UINTPTR_MAX
        : (static_cast<uintptr_t>(1) << activeBits) - 1;
}

SideMetadataResult ReferenceCountTable::InitializeObjectQuiescent(
    uintptr_t object,
    size_t size)
{
    SideMetadataResult result = ValidateUnregisteredObjectRange(object, size);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    result = ValidateContinuationLines(object, size, false);
    if (result == SideMetadataResult::Success)
    {
        result = ClearStraddleLinesUnchecked(object, size);
    }
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCount,
            object,
            0,
            SideMetadataMemoryOrder::Relaxed);
    }
    if (result == SideMetadataResult::Success)
    {
        result = SetValidObject(object, 1);
    }
    return result;
}

SideMetadataResult ReferenceCountTable::RegisterObjectInClearedSpan(uintptr_t object, size_t size)
{
    if (size < MinimumObjectSize || size % sizeof(uintptr_t) != 0)
    {
        return SideMetadataResult::InvalidArgument;
    }
#ifdef _DEBUG
    SideMetadataResult result = ValidateUnregisteredObjectRange(object, size);
    if (result == SideMetadataResult::Success)
    {
        result = ValidateContinuationLines(object, size, true);
    }
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
#endif // _DEBUG
    return SetValidObject(object, 1);
}

SideMetadataResult ReferenceCountTable::CopyObjectQuiescent(
    uintptr_t source,
    uintptr_t destination,
    size_t size)
{
    SideMetadataResult result = ValidateObjectRange(source, size, true);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    result = ValidateUnregisteredObjectRange(destination, size);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t sourceEnd = source + size;
    uintptr_t destinationEnd = destination + size;
    if ((source < destinationEnd) && (destination < sourceEnd))
    {
        return SideMetadataResult::InvalidArgument;
    }

    result = ValidateContinuationLines(destination, size, false);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t count;
    result = Load(source, &count);
    if (result == SideMetadataResult::Success)
    {
        result = ClearStraddleLinesUnchecked(destination, size);
    }
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCount,
            destination,
            count,
            SideMetadataMemoryOrder::Relaxed);
    }
    if ((result == SideMetadataResult::Success) &&
        (size > ImmixBlockGeometry::LineBytes))
    {
        result = MarkStraddleLinesUnchecked(destination, size);
    }
    if (result == SideMetadataResult::Success)
    {
        result = SetValidObject(destination, 1);
    }
    return result;
}

SideMetadataResult ReferenceCountTable::ResetObjectQuiescent(
    uintptr_t object,
    size_t size)
{
    SideMetadataResult result = ValidateObjectRange(object, size, true);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    result = ValidateContinuationLines(object, size, false);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    result = SetValidObject(object, 0);
    if (result == SideMetadataResult::Success)
    {
        result = ClearStraddleLinesUnchecked(object, size);
    }
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->Store(
            LxrSideMetadataKind::ReferenceCount,
            object,
            0,
            SideMetadataMemoryOrder::Relaxed);
    }
    return result;
}

SideMetadataResult ReferenceCountTable::ResetBlockQuiescent(uintptr_t block)
{
    if ((m_metadata == nullptr) ||
        !ImmixBlockGeometry::IsBlockAligned(block))
    {
        return SideMetadataResult::InvalidArgument;
    }

    return ResetRangeQuiescent(block, ImmixBlockGeometry::BlockBytes);
}

SideMetadataResult ReferenceCountTable::ResetRangeQuiescent(
    uintptr_t start,
    size_t size)
{
    if ((m_metadata == nullptr) ||
        (size == 0) ||
        !ImmixBlockGeometry::IsLineAligned(start) ||
        ((size & ImmixBlockGeometry::LineMask) != 0) ||
        !m_metadata->IsDataRangeOwned(start, size))
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataResult result = m_metadata->ResetRangeQuiescent(
        LxrSideMetadataKind::ValidObject,
        start,
        size,
        0);
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->ResetRangeQuiescent(
            LxrSideMetadataKind::ReferenceCount,
            start,
            size,
            0);
    }
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->ResetRangeQuiescent(
            LxrSideMetadataKind::ReferenceCountStraddle,
            start,
            size,
            0);
    }
    return result;
}

SideMetadataResult ReferenceCountTable::Load(
    uintptr_t object,
    uintptr_t* count) const
{
    if (count == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataResult result = ValidateAddress(object, true);
    return result == SideMetadataResult::Success
        ? m_metadata->Load(
            LxrSideMetadataKind::ReferenceCount,
            object,
            SideMetadataMemoryOrder::Relaxed,
            count)
        : result;
}

SideMetadataResult ReferenceCountTable::CompareExchange(
    uintptr_t object,
    uintptr_t value,
    uintptr_t comparand,
    uintptr_t* observed,
    bool* exchanged)
{
    if ((observed == nullptr) || (exchanged == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataResult result = ValidateAddress(object, true);
    uintptr_t maximum = GetMaximumCount();
    if ((value > maximum) ||
        (comparand > maximum) ||
        ((comparand == maximum) && (value != maximum)))
    {
        return SideMetadataResult::InvalidArgument;
    }
    return result == SideMetadataResult::Success
        ? m_metadata->CompareExchange(
            LxrSideMetadataKind::ReferenceCount,
            object,
            value,
            comparand,
            SideMetadataMemoryOrder::Relaxed,
            observed,
            exchanged)
        : result;
}

SideMetadataResult ReferenceCountTable::Increment(
    uintptr_t object,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    SideMetadataResult result = ValidateAddress(object, true);
    return result == SideMetadataResult::Success
        ? m_metadata->IncrementSaturating(
            LxrSideMetadataKind::ReferenceCount,
            object,
            SideMetadataMemoryOrder::Relaxed,
            previous,
            status)
        : result;
}

SideMetadataResult ReferenceCountTable::Decrement(
    uintptr_t object,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    SideMetadataResult result = ValidateAddress(object, true);
    return result == SideMetadataResult::Success
        ? m_metadata->DecrementNonZeroNonSaturated(
            LxrSideMetadataKind::ReferenceCount,
            object,
            SideMetadataMemoryOrder::Relaxed,
            previous,
            status)
        : result;
}

SideMetadataResult ReferenceCountTable::DecrementWithDeath(
    uintptr_t object,
    ReferenceCountDeathCallback death,
    void* context,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    return DecrementWithDeathCore<false>(object, death, context, previous, status);
}

SideMetadataResult ReferenceCountTable::DecrementWithDeathFast(
    uintptr_t object,
    ReferenceCountDeathCallback death,
    void* context,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    return DecrementWithDeathCore<true>(object, death, context, previous, status);
}

template <bool Fast>
SideMetadataResult ReferenceCountTable::DecrementWithDeathCore(
    uintptr_t object,
    ReferenceCountDeathCallback death,
    void* context,
    uintptr_t* previous,
    SideMetadataUpdateStatus* status)
{
    if (death == nullptr || previous == nullptr || status == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }
    *previous = 0;
    *status = SideMetadataUpdateStatus::Unchanged;
    uintptr_t count;
    SideMetadataResult result = Fast ? LoadFast(object, &count) : Load(object, &count);
    while (result == SideMetadataResult::Success)
    {
        *previous = count;
        if (count == 0 || count == GetMaximumCount())
        {
            *status = count == 0
                ? SideMetadataUpdateStatus::Zero
                : SideMetadataUpdateStatus::Saturated;
            return SideMetadataResult::Success;
        }
        if (count == 1)
        {
            result = death(object, context);
            if (result != SideMetadataResult::Success)
            {
                return result;
            }
            // Invalidate the old start while RC still prevents allocation into its lines.
            if (Fast)
            {
                m_valid.Store(object, 0, SideMetadataMemoryOrder::Relaxed);
            }
            else
            {
                result = m_metadata->Store(
                    LxrSideMetadataKind::ValidObject, object, 0,
                    SideMetadataMemoryOrder::Relaxed);
                if (result != SideMetadataResult::Success)
                {
                    return result;
                }
            }
        }
        uintptr_t observed;
        bool exchanged;
        if (Fast)
        {
            exchanged = m_rc.CompareExchange(
                object, count - 1, count, SideMetadataMemoryOrder::Relaxed, &observed);
        }
        else
        {
            result = m_metadata->CompareExchange(
                LxrSideMetadataKind::ReferenceCount, object, count - 1, count,
                SideMetadataMemoryOrder::Relaxed, &observed, &exchanged);
            if (result != SideMetadataResult::Success)
            {
                return result;
            }
        }
        if (exchanged)
        {
            *status = SideMetadataUpdateStatus::Updated;
            return SideMetadataResult::Success;
        }
        // All increments must have drained; a death scan cannot race another last reference.
        if (count == 1)
        {
            return SideMetadataResult::InconsistentState;
        }
        count = observed;
    }
    return result;
}

SideMetadataResult ReferenceCountTable::IsSticky(
    uintptr_t object,
    bool* sticky) const
{
    if (sticky == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t count;
    SideMetadataResult result = Load(object, &count);
    if (result == SideMetadataResult::Success)
    {
        *sticky = count == GetMaximumCount();
    }
    return result;
}

SideMetadataResult ReferenceCountTable::MarkStraddleLinesQuiescent(
    uintptr_t object,
    size_t size)
{
    SideMetadataResult result = ValidateObjectRange(object, size, true);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    if (size <= ImmixBlockGeometry::LineBytes)
    {
        return SideMetadataResult::Success;
    }

    result = ValidateContinuationLines(object, size, true);
    return result == SideMetadataResult::Success
        ? MarkStraddleLinesUnchecked(object, size)
        : result;
}

SideMetadataResult ReferenceCountTable::ClearStraddleLinesQuiescent(
    uintptr_t object,
    size_t size)
{
    SideMetadataResult result = ValidateObjectRange(object, size, true);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    result = ValidateContinuationLines(object, size, false);
    return result == SideMetadataResult::Success
        ? ClearStraddleLinesUnchecked(object, size)
        : result;
}

SideMetadataResult ReferenceCountTable::IsContinuationLine(
    uintptr_t line,
    bool* continuation) const
{
    if ((continuation == nullptr) ||
        (m_metadata == nullptr) ||
        !ImmixBlockGeometry::IsLineAligned(line) ||
        !m_metadata->IsDataAddressOwned(line))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t straddle;
    SideMetadataResult result = m_metadata->Load(
        LxrSideMetadataKind::ReferenceCountStraddle,
        line,
        SideMetadataMemoryOrder::Relaxed,
        &straddle);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    uintptr_t count = 0;
    uintptr_t valid = 0;
    result = m_metadata->Load(
        LxrSideMetadataKind::ReferenceCount,
        line,
        SideMetadataMemoryOrder::Relaxed,
        &count);
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->Load(
            LxrSideMetadataKind::ValidObject,
            line,
            SideMetadataMemoryOrder::Relaxed,
            &valid);
    }
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    if (straddle == 0)
    {
        if ((count != 0) && (valid == 0))
        {
            return SideMetadataResult::InconsistentState;
        }
        *continuation = false;
        return SideMetadataResult::Success;
    }
    if ((straddle != 1) || (count != 1) || (valid != 0))
    {
        return SideMetadataResult::InconsistentState;
    }

    *continuation = true;
    return SideMetadataResult::Success;
}

SideMetadataResult ReferenceCountTable::ValidateAddress(
    uintptr_t object,
    bool requireRegistered) const
{
    if ((m_metadata == nullptr) ||
        ((object & (ObjectAlignment - 1)) != 0) ||
        !m_metadata->IsDataAddressOwned(object) ||
        !m_metadata->IsSpecEnabled(LxrSideMetadataKind::ValidObject) ||
        !m_metadata->IsSpecEnabled(LxrSideMetadataKind::ReferenceCount) ||
        !m_metadata->IsSpecEnabled(LxrSideMetadataKind::ReferenceCountStraddle))
    {
        return SideMetadataResult::InvalidArgument;
    }

    SideMetadataLocation location;
    SideMetadataResult result = m_metadata->GetLocation(
        LxrSideMetadataKind::ReferenceCount,
        object,
        &location);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    if (location.bit_count != GetActiveBits())
    {
        return SideMetadataResult::InvalidLayout;
    }
    if (!requireRegistered)
    {
        return SideMetadataResult::Success;
    }

    uintptr_t valid;
    result = m_metadata->Load(
        LxrSideMetadataKind::ValidObject,
        object,
        SideMetadataMemoryOrder::Relaxed,
        &valid);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    return valid == 1
        ? SideMetadataResult::Success
        : SideMetadataResult::AddressNotOwned;
}

SideMetadataResult ReferenceCountTable::ValidateObjectRange(
    uintptr_t object,
    size_t size,
    bool requireRegistered) const
{
    SideMetadataResult result = ValidateAddress(object, requireRegistered);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    if ((size < MinimumObjectSize) ||
        !m_metadata->IsDataRangeOwned(object, size))
    {
        return SideMetadataResult::InvalidArgument;
    }

    ImmixGeometryRange range;
    ImmixGeometryResult geometryResult =
        ImmixBlockGeometry::GetObjectLineRange(object, size, &range);
    switch (geometryResult)
    {
        case ImmixGeometryResult::Success:
            return SideMetadataResult::Success;
        case ImmixGeometryResult::AddressOverflow:
            return SideMetadataResult::AddressOverflow;
        case ImmixGeometryResult::InvalidArgument:
        case ImmixGeometryResult::CrossesBlock:
            return SideMetadataResult::InvalidArgument;
        default:
            return SideMetadataResult::InvalidArgument;
    }
}

SideMetadataResult ReferenceCountTable::ValidateUnregisteredObjectRange(
    uintptr_t object,
    size_t size) const
{
    SideMetadataResult result = ValidateObjectRange(object, size, false);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t valid;
    result = m_metadata->Load(
        LxrSideMetadataKind::ValidObject,
        object,
        SideMetadataMemoryOrder::SequentiallyConsistent,
        &valid);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    if (valid != 0)
    {
        return SideMetadataResult::InconsistentState;
    }

    uintptr_t line = object & ~ImmixBlockGeometry::LineMask;
    bool continuation;
    result = IsContinuationLine(line, &continuation);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    return continuation
        ? SideMetadataResult::InconsistentState
        : SideMetadataResult::Success;
}

SideMetadataResult ReferenceCountTable::ValidateContinuationLines(
    uintptr_t object,
    size_t size,
    bool requireEmpty) const
{
    uintptr_t first;
    uintptr_t end;
    GetContinuationRange(object, size, &first, &end);
    for (uintptr_t line = first; line < end; line += ImmixBlockGeometry::LineBytes)
    {
        uintptr_t straddle = 0;
        uintptr_t count = 0;
        uintptr_t valid = 0;
        SideMetadataResult result = m_metadata->Load(
            LxrSideMetadataKind::ReferenceCountStraddle,
            line,
            SideMetadataMemoryOrder::Relaxed,
            &straddle);
        if (result == SideMetadataResult::Success)
        {
            result = m_metadata->Load(
                LxrSideMetadataKind::ReferenceCount,
                line,
                SideMetadataMemoryOrder::Relaxed,
                &count);
        }
        if (result == SideMetadataResult::Success)
        {
            result = m_metadata->Load(
                LxrSideMetadataKind::ValidObject,
                line,
                SideMetadataMemoryOrder::Relaxed,
                &valid);
        }
        if (result != SideMetadataResult::Success)
        {
            return result;
        }
        if (valid != 0)
        {
            return SideMetadataResult::InconsistentState;
        }
        if (((straddle == 0) && (count != 0)) ||
            ((straddle != 0) && ((straddle != 1) || (count != 1))))
        {
            return SideMetadataResult::InconsistentState;
        }
        if (requireEmpty && ((straddle != 0) || (count != 0)))
        {
            return SideMetadataResult::InconsistentState;
        }
    }
    return SideMetadataResult::Success;
}

SideMetadataResult ReferenceCountTable::SetValidObject(
    uintptr_t object,
    uintptr_t value)
{
    // Every caller has validated the object address, or comes from the allocation path of the engine.
    m_valid.Store(object, value, SideMetadataMemoryOrder::SequentiallyConsistent);
    return SideMetadataResult::Success;
}

#ifdef _DEBUG
void ReferenceCountTable::DebugValidateStraddleRange(
    uintptr_t object,
    size_t size,
    bool requireEmpty) const
{
    SideMetadataResult result = ValidateObjectRange(object, size, true);
    if ((result == SideMetadataResult::Success) && (size > ImmixBlockGeometry::LineBytes))
    {
        result = ValidateContinuationLines(object, size, requireEmpty);
    }
    _ASSERTE(result == SideMetadataResult::Success);
    (void)result;
}
#endif // _DEBUG

SideMetadataResult ReferenceCountTable::MarkStraddleLinesUnchecked(
    uintptr_t object,
    size_t size)
{
    uintptr_t first;
    uintptr_t end;
    GetContinuationRange(object, size, &first, &end);
    for (uintptr_t line = first; line < end; line += ImmixBlockGeometry::LineBytes)
    {
        m_straddle.Store(line, 1, SideMetadataMemoryOrder::Relaxed);
        m_rc.Store(line, 1, SideMetadataMemoryOrder::Relaxed);
    }
    return SideMetadataResult::Success;
}

SideMetadataResult ReferenceCountTable::ClearStraddleLinesUnchecked(
    uintptr_t object,
    size_t size)
{
    uintptr_t first;
    uintptr_t end;
    GetContinuationRange(object, size, &first, &end);
    for (uintptr_t line = first; line < end; line += ImmixBlockGeometry::LineBytes)
    {
        m_straddle.Store(line, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
        m_rc.Store(line, 0, SideMetadataMemoryOrder::SequentiallyConsistent);
    }
    return SideMetadataResult::Success;
}

void ReferenceCountTable::GetContinuationRange(
    uintptr_t object,
    size_t size,
    uintptr_t* first,
    uintptr_t* end)
{
    if (size <= ImmixBlockGeometry::LineBytes)
    {
        *first = object;
        *end = object;
        return;
    }

    *first = (object & ~ImmixBlockGeometry::LineMask) +
        ImmixBlockGeometry::LineBytes;
    *end = (object + size) & ~ImmixBlockGeometry::LineMask;
}

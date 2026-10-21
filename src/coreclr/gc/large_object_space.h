// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LARGE_OBJECT_SPACE_H
#define LARGE_OBJECT_SPACE_H

#include "immix_block.h"

#include <atomic>

enum class LxrLargeObjectResult : uint8_t
{
    Success,
    InvalidArgument,
    NotInitialized,
    UnsupportedAddressSpace,
    UnsupportedPageSize,
    BelowThreshold,
    AddressOverflow,
    ReservationFailed,
    CommitFailed,
    DecommitFailed,
    OutOfMemory,
    NotFound,
    NotOwner,
    InvalidPhase,
    MetadataFailure,
};

enum class LxrLargeObjectPhase : uint8_t
{
    Mutator,
    ConcurrentMark,
    Release,
    Shutdown,
};

struct LxrLargeObjectAllocation
{
    uintptr_t object;
    uintptr_t page_start;
    size_t object_bytes;
    size_t page_count;
    size_t padding_bytes;
    uintptr_t owner;
};

class LxrLargeObjectSpace final
{
public:
    static constexpr size_t PageBytes = 4 * 1024;
    static constexpr size_t MinimumObjectBytes = ImmixBlockGeometry::BlockBytes;
    static constexpr size_t FieldMetadataGranularity = 8;
    static constexpr uint8_t MarkBit = 0x1;
    static constexpr uint8_t NurseryBit = 0x2;
    static constexpr uint8_t StateMask = MarkBit | NurseryBit;

    LxrLargeObjectSpace();
    ~LxrLargeObjectSpace();
    LxrLargeObjectSpace(const LxrLargeObjectSpace&) = delete;
    LxrLargeObjectSpace& operator=(const LxrLargeObjectSpace&) = delete;

    static bool ShouldUseLargeObjectSpace(size_t objectBytes);
    static LxrLargeObjectResult ComputeAllocationGeometry(
        uintptr_t pageStart,
        size_t objectBytes,
        size_t alignment,
        size_t alignmentOffset,
        LxrLargeObjectAllocation* allocation);

    LxrLargeObjectResult Initialize(
        SideMetadataManager* metadata, size_t reservationBytes, void* reservation = nullptr,
        size_t admittedBytes = 0);
    LxrLargeObjectResult GrowQuiescent(size_t admittedBytes);
    size_t GetAdmittedBytes() const;
    size_t GetRequiredGrowthBytes(size_t objectBytes) const;
    void Shutdown();

    LxrLargeObjectPhase GetPhase() const;
    uint8_t GetMarkState() const;
    LxrLargeObjectResult BeginCollection(bool concurrent, bool fullTrace);
    LxrLargeObjectResult BeginRelease();
    LxrLargeObjectResult EndCollection();

    LxrLargeObjectResult Allocate(
        size_t objectBytes,
        size_t alignment,
        size_t alignmentOffset,
        uintptr_t owner,
        LxrLargeObjectAllocation* allocation);
    LxrLargeObjectResult TryMarkAndPromote(
        uintptr_t object,
        uintptr_t owner,
        bool* updated);
    LxrLargeObjectResult TryRelease(uintptr_t object, uintptr_t owner);
    LxrLargeObjectResult GetAllocation(
        uintptr_t object,
        LxrLargeObjectAllocation* allocation) const;
    LxrLargeObjectResult GetContainingAllocation(
        uintptr_t address,
        LxrLargeObjectAllocation* allocation) const;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrLargeObjectResult ReleaseAfterDecrements(
        uintptr_t object, uintptr_t owner, const LxrBlockReleaseGuard& guard);
    LxrLargeObjectResult SweepNursery(size_t* releasedBytes);
#endif
    uintptr_t GetReservationStart() const;
    size_t GetReservationBytes() const;
    size_t GetCommittedBytes() const;
    LxrLargeObjectResult GetMatureCommittedBytes(size_t* bytes) const;
    // Page bytes of the large objects that are not nursery, and the part of those pages that no object byte uses.
    LxrLargeObjectResult GetMatureStatistics(size_t* bytes, size_t* slackBytes) const;

private:
    struct Span
    {
        uintptr_t page_start;
        size_t page_count;
        uintptr_t object;
        size_t object_bytes;
        size_t padding_bytes;
        uintptr_t owner;
        Span* next;
    };

    class LockScope final
    {
    public:
        explicit LockScope(const LxrLargeObjectSpace* space);
        ~LockScope();

    private:
        const LxrLargeObjectSpace* m_space;
    };

    static bool IsPowerOfTwo(size_t value);
    static bool TryAdd(uintptr_t left, size_t right, uintptr_t* result);
    static bool TryAlignUp(size_t value, size_t alignment, size_t* result);
    static LxrLargeObjectResult FromMetadataResult(SideMetadataResult result);

    void EnterLock() const;
    void ExitLock() const;
    Span* FindActive(uintptr_t object, Span** previous) const;
    Span* FindFree(size_t pageCount, Span** previous) const;
    void RemoveSpan(Span** head, Span* span, Span* previous);
    void InsertFreeAndCoalesce(Span* span);
    void DeleteSpans(Span* span);
    LxrLargeObjectResult ReleaseLocked(uintptr_t object, uintptr_t owner);
    LxrLargeObjectResult ResetAllocationMetadata(
        uintptr_t pageStart,
        size_t pageBytes);
    LxrLargeObjectResult InitializeAllocationMetadata(
        const LxrLargeObjectAllocation& allocation);

    SideMetadataManager* m_metadata;
    void* m_reservation;
    size_t m_reservation_bytes;
    std::atomic<size_t> m_admitted_bytes{0};
    std::atomic<size_t> m_span_bytes{0};
    bool m_owns_reservation;
    Span* m_free_spans;
    Span* m_active_spans;
    Span* m_quarantined_spans;
    std::atomic<LxrLargeObjectPhase> m_phase;
    std::atomic<uint8_t> m_mark_state;
    mutable std::atomic_flag m_lock = ATOMIC_FLAG_INIT;
};

static_assert(LxrLargeObjectSpace::MinimumObjectBytes == 32 * 1024);
static_assert((LxrLargeObjectSpace::PageBytes & (LxrLargeObjectSpace::PageBytes - 1)) == 0);

#endif // LARGE_OBJECT_SPACE_H

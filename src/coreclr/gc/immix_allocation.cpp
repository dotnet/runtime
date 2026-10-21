// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "immix_allocation.h"

namespace
{
    constexpr SideMetadataMemoryOrder MetadataOrder =
        SideMetadataMemoryOrder::SequentiallyConsistent;
    constexpr SideMetadataMemoryOrder RcReadOrder =
        SideMetadataMemoryOrder::Relaxed;

    bool TryAdd(uintptr_t start, size_t size, uintptr_t* end)
    {
        if ((end == nullptr) || (size > (UINTPTR_MAX - start)))
        {
            return false;
        }

        *end = start + size;
        return true;
    }
}

ImmixAllocationManager::ImmixAllocationManager()
    : m_metadata(nullptr)
    , m_blocks(nullptr)
    , m_arena_start(0)
    , m_arena_size(0)
    , m_block_count(0)
    , m_minimum_reuse_lines(0)
    , m_next_fresh_block(0)
    , m_next_recycled_block(0)
    , m_fresh_blocks_acquired(0)
    , m_recycled_blocks_acquired(0)
    , m_recycled_lines_consumed(0)
{
}

SideMetadataResult ImmixAllocationManager::Initialize(
    SideMetadataManager* metadata,
    ImmixBlockManager* blocks,
    uintptr_t arenaStart,
    size_t arenaSize,
    size_t minimumReuseLines)
{
    if ((metadata == nullptr) ||
        (blocks == nullptr) ||
        (m_metadata != nullptr) ||
        !ImmixBlockGeometry::IsBlockAligned(arenaStart) ||
        (arenaSize == 0) ||
        ((arenaSize & ImmixBlockGeometry::BlockMask) != 0) ||
        (minimumReuseLines == 0) ||
        (minimumReuseLines > ImmixBlockGeometry::LinesPerBlock))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t arenaEnd;
    if (!TryAdd(arenaStart, arenaSize, &arenaEnd) ||
        (static_cast<uint64_t>(arenaEnd) > ImmixBlockGeometry::AddressLimit))
    {
        return SideMetadataResult::AddressOverflow;
    }

    constexpr LxrSideMetadataKind RequiredKinds[] = {
        LxrSideMetadataKind::ReferenceCount,
        LxrSideMetadataKind::FieldUnlogged,
        LxrSideMetadataKind::ObjectMark,
        LxrSideMetadataKind::LineReuse,
    };
    for (LxrSideMetadataKind kind : RequiredKinds)
    {
        if (!metadata->IsSpecEnabled(kind))
        {
            return SideMetadataResult::InvalidArgument;
        }
    }
    if (!metadata->IsDataRangeOwned(arenaStart, arenaSize))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    m_metadata = metadata;
    m_reference_count = metadata->GetAccessor(LxrSideMetadataKind::ReferenceCount);
    if (!m_reference_count.IsValid())
    {
        m_metadata = nullptr;
        return SideMetadataResult::InvalidLayout;
    }
    m_blocks = blocks;
    m_arena_start = arenaStart;
    m_arena_size = arenaSize;
    m_block_count = arenaSize / ImmixBlockGeometry::BlockBytes;
    m_minimum_reuse_lines = minimumReuseLines;
    m_next_fresh_block.store(0, std::memory_order_relaxed);
    m_next_recycled_block.store(0, std::memory_order_relaxed);
    m_fresh_blocks_acquired.store(0, std::memory_order_relaxed);
    m_recycled_blocks_acquired.store(0, std::memory_order_relaxed);
    m_recycled_lines_consumed.store(0, std::memory_order_relaxed);
    m_blocks_in_use.store(0, std::memory_order_relaxed);
    return SideMetadataResult::Success;
}

void ImmixAllocationManager::Shutdown()
{
    m_metadata = nullptr;
    m_reference_count = SideMetadataAccessor();
    m_blocks = nullptr;
    m_arena_start = 0;
    m_arena_size = 0;
    m_block_count = 0;
    m_minimum_reuse_lines = 0;
    m_next_fresh_block.store(0, std::memory_order_relaxed);
    m_next_recycled_block.store(0, std::memory_order_relaxed);
    m_fresh_blocks_acquired.store(0, std::memory_order_relaxed);
    m_recycled_blocks_acquired.store(0, std::memory_order_relaxed);
    m_recycled_lines_consumed.store(0, std::memory_order_relaxed);
    m_blocks_in_use.store(0, std::memory_order_relaxed);
    m_notes = AcquireNotes{};
    m_placement = false;
    m_mutator_fresh_blocks = 0;
}

SideMetadataResult ImmixAllocationManager::GrowQuiescent(size_t arenaSize)
{
    if (m_metadata == nullptr || arenaSize <= m_arena_size ||
        arenaSize % ImmixBlockGeometry::BlockBytes != 0 ||
        !m_metadata->IsDataRangeOwned(m_arena_start, arenaSize))
    {
        return SideMetadataResult::InvalidArgument;
    }
    m_arena_size = arenaSize;
    m_block_count = arenaSize / ImmixBlockGeometry::BlockBytes;
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixAllocationManager::ShrinkQuiescent(size_t arenaSize)
{
    if (m_metadata == nullptr || arenaSize == 0 || arenaSize >= m_arena_size ||
        arenaSize % ImmixBlockGeometry::BlockBytes != 0)
    {
        return SideMetadataResult::InvalidArgument;
    }
    m_arena_size = arenaSize;
    m_block_count = arenaSize / ImmixBlockGeometry::BlockBytes;
    return SideMetadataResult::Success;
}

void ImmixAllocationManager::EnablePlacementQuiescent(size_t mutatorFreshBlocks)
{
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_S5B_NO_PLACEMENT_CONTROL)
    m_placement = true;
    m_mutator_fresh_blocks = mutatorFreshBlocks;
    m_next_copy_block.store(0, std::memory_order_relaxed);
#else
    (void)mutatorFreshBlocks;
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_S5B_NO_PLACEMENT_CONTROL
}

void ImmixAllocationManager::SetMutatorFreshBlocksQuiescent(size_t mutatorFreshBlocks)
{
    m_mutator_fresh_blocks = mutatorFreshBlocks;
}

void ImmixAllocationManager::ResetCopyCursorQuiescent()
{
    m_next_copy_block.store(0, std::memory_order_relaxed);
}

SideMetadataResult ImmixAllocationManager::TryFindNextHole(
    uintptr_t block,
    size_t searchLine,
    ImmixHole* hole,
    size_t* nextSearchLine,
    ImmixAllocationStatus* status) const
{
    if ((hole == nullptr) ||
        (nextSearchLine == nullptr) ||
        (status == nullptr) ||
        (m_metadata == nullptr) ||
        (searchLine > ImmixBlockGeometry::LinesPerBlock))
    {
        return SideMetadataResult::InvalidArgument;
    }

    hole->start = block;
    hole->end = block;
    hole->line_count = 0;
    *nextSearchLine = searchLine;
    *status = ImmixAllocationStatus::Exhausted;
    if (!ImmixBlockGeometry::IsBlockAligned(block) ||
        (block < m_arena_start) ||
        (block >= (m_arena_start + m_arena_size)))
    {
        return SideMetadataResult::AddressNotOwned;
    }

    size_t line = searchLine;
    while (line < ImmixBlockGeometry::LinesPerBlock)
    {
        bool previousDead = false;
        size_t firstReusableLine = ImmixBlockGeometry::LinesPerBlock;
        while (line < ImmixBlockGeometry::LinesPerBlock)
        {
            bool dead = false;
            SideMetadataResult result =
                IsLineDead(block, line, &dead);
            if (result != SideMetadataResult::Success)
            {
                return result;
            }
            if (dead && ((line == 0) || previousDead))
            {
                firstReusableLine = line;
                break;
            }

            previousDead = dead;
            line++;
        }
        if (firstReusableLine == ImmixBlockGeometry::LinesPerBlock)
        {
            break;
        }

        line = firstReusableLine + 1;
        while (line < ImmixBlockGeometry::LinesPerBlock)
        {
            bool dead = false;
            SideMetadataResult result =
                IsLineDead(block, line, &dead);
            if (result != SideMetadataResult::Success)
            {
                return result;
            }
            if (!dead)
            {
                break;
            }
            line++;
        }

        size_t reusableLines = line - firstReusableLine;
        *nextSearchLine = line;
        if (reusableLines >= m_minimum_reuse_lines)
        {
            hole->start =
                block + (firstReusableLine * ImmixBlockGeometry::LineBytes);
            hole->end = block + (line * ImmixBlockGeometry::LineBytes);
            hole->line_count = reusableLines;
            *status = ImmixAllocationStatus::Ready;
            return SideMetadataResult::Success;
        }
    }

    *nextSearchLine = ImmixBlockGeometry::LinesPerBlock;
    return SideMetadataResult::Success;
}

size_t ImmixAllocationManager::GetFreshBlocksAcquired() const
{
    return m_fresh_blocks_acquired.load(std::memory_order_relaxed);
}

size_t ImmixAllocationManager::GetRecycledBlocksAcquired() const
{
    return m_recycled_blocks_acquired.load(std::memory_order_relaxed);
}

size_t ImmixAllocationManager::GetRecycledLinesConsumed() const
{
    return m_recycled_lines_consumed.load(std::memory_order_relaxed);
}

size_t ImmixAllocationManager::GetBlocksInUse() const
{
    return m_blocks_in_use.load(std::memory_order_acquire);
}

SideMetadataResult ImmixAllocationManager::NotifyBlockReleased()
{
    size_t current = m_blocks_in_use.load(std::memory_order_relaxed);
    while (current != 0)
    {
        if (m_blocks_in_use.compare_exchange_weak(current, current - 1, std::memory_order_acq_rel))
        {
            return SideMetadataResult::Success;
        }
    }
    return SideMetadataResult::InconsistentState;
}

void ImmixAllocationManager::SetAcquireNotes(const AcquireNotes& notes)
{
    m_notes = notes;
}

void ImmixAllocationManager::NoteAcquired(size_t index, bool fresh, bool mutator)
{
    if (m_notes.refresh_flags == nullptr)
    {
        return;
    }
    if (mutator)
    {
        // The flag makes the entry unique, so the list never needs more entries than there are blocks.
        if (m_notes.allocated_flags[index].exchange(1, std::memory_order_acq_rel) == 0)
        {
            m_notes.allocated_list[m_notes.allocated_count->fetch_add(1, std::memory_order_acq_rel)] =
                static_cast<uint32_t>(index);
        }
    }
    if (!(fresh && mutator))
    {
        m_notes.refresh_flags[index].store(1, std::memory_order_release);
    }
}

bool ImmixAllocationManager::IsCopySource(ImmixAllocationSource source)
{
    return (source == ImmixAllocationSource::GcCopyFresh) ||
        (source == ImmixAllocationSource::GcCopyRecycled);
}

bool ImmixAllocationManager::IsFreshSource(ImmixAllocationSource source)
{
    return (source == ImmixAllocationSource::MutatorFresh) ||
        (source == ImmixAllocationSource::MutatorPinnedFresh) ||
        (source == ImmixAllocationSource::GcCopyFresh);
}

ImmixBlockAcquireKind ImmixAllocationManager::GetAcquireKind(
    ImmixAllocationSource source)
{
    switch (source)
    {
        case ImmixAllocationSource::MutatorFresh:
            return ImmixBlockAcquireKind::MutatorFresh;
        case ImmixAllocationSource::MutatorRecycled:
        case ImmixAllocationSource::MutatorPinnedRecycled:
            return ImmixBlockAcquireKind::MutatorReusable;
        case ImmixAllocationSource::GcCopyFresh:
            return ImmixBlockAcquireKind::GcCopyFresh;
        case ImmixAllocationSource::GcCopyRecycled:
            return ImmixBlockAcquireKind::GcCopyReusable;
        default:
            return ImmixBlockAcquireKind::MutatorFresh;
    }
}

ImmixAllocationStatus ImmixAllocationManager::GetAllocationStatus(
    ImmixBlockOperationStatus status)
{
    switch (status)
    {
        case ImmixBlockOperationStatus::Updated:
        case ImmixBlockOperationStatus::Unchanged:
            return ImmixAllocationStatus::Ready;
        case ImmixBlockOperationStatus::Contended:
            return ImmixAllocationStatus::Contended;
        case ImmixBlockOperationStatus::Stale:
            return ImmixAllocationStatus::Stale;
        case ImmixBlockOperationStatus::InvalidTransition:
            return ImmixAllocationStatus::InvalidPhase;
        default:
            return ImmixAllocationStatus::InvalidRequest;
    }
}

SideMetadataResult ImmixAllocationManager::IsLineDead(
    uintptr_t block,
    size_t lineIndex,
    bool* dead) const
{
    if ((dead == nullptr) ||
        (lineIndex >= ImmixBlockGeometry::LinesPerBlock))
    {
        return SideMetadataResult::InvalidArgument;
    }

    // TryFindNextHole has checked that the block lies in the arena, and the arena is an owned data range.
    uintptr_t line = block + (lineIndex * ImmixBlockGeometry::LineBytes);
    *dead = m_reference_count.IsRangeZero(line, ImmixBlockGeometry::LineBytes, RcReadOrder);
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixAllocationManager::AcquireBlock(
    uintptr_t owner,
    ImmixAllocationSource source,
    uintptr_t* block,
    ImmixAllocationStatus* status)
{
    if ((block == nullptr) ||
        (status == nullptr) ||
        (owner == ImmixBlockManager::NoOwner) ||
        (source > ImmixAllocationSource::MutatorPinnedRecycled))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *block = 0;
    *status = ImmixAllocationStatus::Exhausted;
    bool fresh = IsFreshSource(source);
    bool pinnedSource =
        (source == ImmixAllocationSource::MutatorPinnedFresh) ||
        (source == ImmixAllocationSource::MutatorPinnedRecycled);
    bool mutator =
        (source == ImmixAllocationSource::MutatorFresh) ||
        (source == ImmixAllocationSource::MutatorRecycled) ||
        pinnedSource;
    uint8_t globalEpoch = m_blocks->GetGlobalPhaseEpoch();
    if (mutator ? ((globalEpoch & 1) == 0) : ((globalEpoch & 1) != 0))
    {
        *status = ImmixAllocationStatus::InvalidPhase;
        return SideMetadataResult::Success;
    }

    uint64_t token = m_blocks->GetStateToken();
    std::atomic<uint64_t>& exhaustedToken = pinnedSource ? m_pinned_exhausted_token : m_recycled_exhausted_token;
    if (!fresh && mutator && exhaustedToken.load(std::memory_order_acquire) == token)
    {
        return SideMetadataResult::Success;
    }
    bool placed = fresh && m_placement;
    size_t modulus = placed && mutator && m_mutator_fresh_blocks < m_block_count ? m_mutator_fresh_blocks : m_block_count;
    for (size_t attempt = 0; attempt < modulus; attempt++)
    {
        size_t index;
        if (fresh)
        {
            index = placed && !mutator
                ? m_next_copy_block.fetch_add(1, std::memory_order_relaxed) % m_block_count
                : m_next_fresh_block.fetch_add(1, std::memory_order_relaxed) % modulus;
        }
        else
        {
            index = m_next_recycled_block.fetch_add(
                1,
                std::memory_order_relaxed) % m_block_count;
        }

        uintptr_t candidate =
            m_arena_start + (index * ImmixBlockGeometry::BlockBytes);
        ImmixBlockState state;
        uint8_t unavailableLines;
        SideMetadataResult result =
            m_blocks->GetStateFast(candidate, &state, &unavailableLines);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }
        if (fresh)
        {
            if (state != ImmixBlockState::Unallocated)
            {
                continue;
            }

            bool nursery;
            result = m_blocks->IsNurseryFast(candidate, &nursery);
            if (result != SideMetadataResult::Success)
            {
                return result;
            }
            if (nursery)
            {
                // Block state remains Unallocated while a fresh block is a
                // current-epoch nursery; the pool cursor must not make it
                // available again merely because its owner was returned.
                continue;
            }
        }
        else
        {
            if (state != ImmixBlockState::Reusable)
            {
                continue;
            }
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_S7_POH_RECYCLE_CONTROL)
            // A pinned block holds pinned-object-heap objects only: pinned recycling takes these blocks and no other
            // recycling does.
            bool pinnedBlock = m_notes.pinned_flags != nullptr &&
                (m_notes.pinned_flags[index].load(std::memory_order_acquire) & PinnedBlockFlag) != 0;
            if (pinnedBlock != pinnedSource)
            {
                continue;
            }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_S7_POH_RECYCLE_CONTROL
            if (unavailableLines > ImmixBlockGeometry::LinesPerBlock)
            {
                continue;
            }
            size_t availableLines =
                ImmixBlockGeometry::LinesPerBlock -
                static_cast<size_t>(unavailableLines);
            if (availableLines < m_minimum_reuse_lines)
            {
                continue;
            }
        }

        ImmixBlockOperationStatus blockStatus;
        result = m_blocks->TryAcquire(
            candidate,
            owner,
            GetAcquireKind(source),
            &blockStatus);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }

        ImmixAllocationStatus allocationStatus =
            GetAllocationStatus(blockStatus);
        if (allocationStatus == ImmixAllocationStatus::Ready)
        {
            *block = candidate;
            *status = allocationStatus;
            if (fresh)
            {
                m_blocks_in_use.fetch_add(1, std::memory_order_release);
                m_fresh_blocks_acquired.fetch_add(
                    1,
                    std::memory_order_relaxed);
            }
            else
            {
                m_recycled_blocks_acquired.fetch_add(
                    1,
                    std::memory_order_relaxed);
            }
            NoteAcquired(index, fresh, mutator);
            if (m_notes.pinned_flags != nullptr)
            {
                if (pinnedSource)
                {
                    m_notes.pinned_flags[index].fetch_or(PinnedBlockFlag, std::memory_order_acq_rel);
                }
                else
                {
                    m_notes.pinned_flags[index].fetch_and(
                        static_cast<uint8_t>(~PinnedBlockFlag), std::memory_order_acq_rel);
                }
            }
            return SideMetadataResult::Success;
        }
        if (allocationStatus == ImmixAllocationStatus::InvalidPhase)
        {
            uint8_t currentEpoch = m_blocks->GetGlobalPhaseEpoch();
            bool validPhase =
                mutator ? ((currentEpoch & 1) != 0) : ((currentEpoch & 1) == 0);
            if (!validPhase)
            {
                *status = allocationStatus;
                return SideMetadataResult::Success;
            }
        }
    }

    if (!fresh && mutator && *status == ImmixAllocationStatus::Exhausted)
    {
        exhaustedToken.store(token, std::memory_order_release);
    }
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixAllocationManager::ReturnBlock(
    uintptr_t block,
    uintptr_t owner,
    ImmixAllocationStatus* status)
{
    if (status == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    ImmixBlockOperationStatus blockStatus;
    SideMetadataResult result =
        m_blocks->TryReturn(block, owner, &blockStatus);
    if (result == SideMetadataResult::Success)
    {
        *status = GetAllocationStatus(blockStatus);
    }
    return result;
}

SideMetadataResult ImmixAllocationManager::PrepareFreshBlock(
    uintptr_t block,
    ImmixAllocationSource source,
    ImmixMarkingContext markingContext)
{
    if (!IsCopySource(source))
    {
        memset(
            reinterpret_cast<void*>(block),
            0,
            ImmixBlockGeometry::BlockBytes);
    }

    SideMetadataResult result = InitializeFieldLogRange(
        block,
        ImmixBlockGeometry::BlockBytes,
        IsCopySource(source));
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t objectMark =
        markingContext == ImmixMarkingContext::ConcurrentOrFinalMark
            ? 1
            : 0;
    result = InitializeObjectMarkRange(
        block,
        ImmixBlockGeometry::BlockBytes,
        objectMark);
    if (result == SideMetadataResult::Success &&
        markingContext == ImmixMarkingContext::ConcurrentOrFinalMark)
    {
        // Port safety delta: a fresh block may replace a remembered source during the same SATB.
        result = IncrementLineReuseRange(block, ImmixBlockGeometry::BlockBytes);
    }
    return result;
}

SideMetadataResult ImmixAllocationManager::PrepareRecycledHole(
    const ImmixHole& hole,
    ImmixAllocationSource source,
    ImmixMarkingContext markingContext)
{
    size_t size = static_cast<size_t>(hole.end - hole.start);
    if (!IsCopySource(source))
    {
        memset(reinterpret_cast<void*>(hole.start), 0, size);
    }

    SideMetadataResult result = InitializeFieldLogRange(
        hole.start,
        size,
        IsCopySource(source));
    if (result != SideMetadataResult::Success)
    {
        return result;
    }
    if (markingContext == ImmixMarkingContext::ConcurrentOrFinalMark)
    {
        result = InitializeObjectMarkRange(hole.start, size, 1);
        if (result == SideMetadataResult::Success)
        {
            result = IncrementLineReuseRange(hole.start, size);
        }
    }
    if ((result == SideMetadataResult::Success) && !IsCopySource(source))
    {
        m_recycled_lines_consumed.fetch_add(
            hole.line_count,
            std::memory_order_relaxed);
    }
    return result;
}

SideMetadataResult ImmixAllocationManager::InitializeFieldLogRange(
    uintptr_t start,
    size_t size,
    bool copy)
{
#if !defined(LXR_SIDE_METADATA_TEST_PLATFORM) || !defined(LXR_VALID_CLEAR_CONTROL)
    if (m_metadata->IsSpecEnabled(LxrSideMetadataKind::ValidObject))
    {
        SideMetadataResult result = m_metadata->ResetRangeQuiescent(
            LxrSideMetadataKind::ValidObject, start, size, 0);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }
    }
#endif // !LXR_SIDE_METADATA_TEST_PLATFORM || !LXR_VALID_CLEAR_CONTROL
    uintptr_t value = copy ? 1 : 0;
    return m_metadata->ResetRangeQuiescent(LxrSideMetadataKind::FieldUnlogged, start, size, value);
}

SideMetadataResult ImmixAllocationManager::InitializeObjectMarkRange(
    uintptr_t start,
    size_t size,
    uintptr_t value)
{
    return m_metadata->ResetRangeQuiescent(
        LxrSideMetadataKind::ObjectMark,
        start,
        size,
        value);
}

SideMetadataResult ImmixAllocationManager::IncrementLineReuseRange(
    uintptr_t start,
    size_t size)
{
    uintptr_t end = start + size;
    for (uintptr_t line = start;
         line < end;
         line += ImmixBlockGeometry::LineBytes)
    {
        SideMetadataResult result = m_metadata->FetchAddWrapping(
            LxrSideMetadataKind::LineReuse,
            line,
            1,
            MetadataOrder,
            nullptr);
        if (result != SideMetadataResult::Success)
        {
            return result;
        }
    }

    return SideMetadataResult::Success;
}

ImmixBumpAllocator::ImmixBumpAllocator()
    : m_manager(nullptr)
    , m_owner(ImmixBlockManager::NoOwner)
    , m_block(0)
    , m_cursor(0)
    , m_limit(0)
    , m_next_search_line(0)
    , m_source(ImmixAllocationSource::MutatorFresh)
{
}

SideMetadataResult ImmixBumpAllocator::Initialize(
    ImmixAllocationManager* manager,
    uintptr_t owner)
{
    if ((manager == nullptr) ||
        (owner == ImmixBlockManager::NoOwner) ||
        (m_manager != nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    m_manager = manager;
    m_owner = owner;
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBumpAllocator::TryRefill(
    ImmixAllocationSource source,
    ImmixMarkingContext markingContext,
    ImmixAllocationStatus* status)
{
    if ((status == nullptr) ||
        (m_manager == nullptr) ||
        (source > ImmixAllocationSource::MutatorPinnedRecycled) ||
        (markingContext > ImmixMarkingContext::ConcurrentOrFinalMark))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixAllocationStatus::Exhausted;
    if ((m_block != 0) &&
        (source == m_source) &&
        !ImmixAllocationManager::IsFreshSource(m_source))
    {
        ImmixHole hole;
        SideMetadataResult result = m_manager->TryFindNextHole(
            m_block,
            m_next_search_line,
            &hole,
            &m_next_search_line,
            status);
        if (result != SideMetadataResult::Success)
        {
            return ReleaseAfterFailure(result, status);
        }
        if (*status == ImmixAllocationStatus::Ready)
        {
            result = m_manager->PrepareRecycledHole(
                hole,
                m_source,
                markingContext);
            if (result == SideMetadataResult::Success)
            {
                m_cursor = hole.start;
                m_limit = hole.end;
            }
            if (result != SideMetadataResult::Success)
            {
                return ReleaseAfterFailure(result, status);
            }
            return SideMetadataResult::Success;
        }
    }

    if (m_block != 0)
    {
        SideMetadataResult result = ReturnCurrentBlock(status);
        if ((result != SideMetadataResult::Success) ||
            (*status != ImmixAllocationStatus::Ready))
        {
            return result;
        }
    }

    for (size_t attempt = 0;
         attempt < m_manager->m_block_count;
         attempt++)
    {
        uintptr_t block;
        SideMetadataResult result = m_manager->AcquireBlock(
            m_owner,
            source,
            &block,
            status);
        if ((result != SideMetadataResult::Success) ||
            (*status != ImmixAllocationStatus::Ready))
        {
            return result;
        }

        m_block = block;
        m_source = source;
        m_next_search_line = 0;
        if (ImmixAllocationManager::IsFreshSource(source))
        {
            result = m_manager->PrepareFreshBlock(
                block,
                source,
                markingContext);
            if (result == SideMetadataResult::Success)
            {
                m_cursor = block;
                m_limit = block + ImmixBlockGeometry::BlockBytes;
            }
            if (result != SideMetadataResult::Success)
            {
                return ReleaseAfterFailure(result, status);
            }
            return SideMetadataResult::Success;
        }

        ImmixHole hole;
        result = m_manager->TryFindNextHole(
            block,
            0,
            &hole,
            &m_next_search_line,
            status);
        if (result != SideMetadataResult::Success)
        {
            return ReleaseAfterFailure(result, status);
        }
        if (*status == ImmixAllocationStatus::Ready)
        {
            result = m_manager->PrepareRecycledHole(
                hole,
                source,
                markingContext);
            if (result == SideMetadataResult::Success)
            {
                m_cursor = hole.start;
                m_limit = hole.end;
            }
            if (result != SideMetadataResult::Success)
            {
                return ReleaseAfterFailure(result, status);
            }
            return SideMetadataResult::Success;
        }

        result = ReturnCurrentBlock(status);
        if ((result != SideMetadataResult::Success) ||
            (*status != ImmixAllocationStatus::Ready))
        {
            return result;
        }
    }

    *status = ImmixAllocationStatus::Exhausted;
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBumpAllocator::TryAllocate(
    const ImmixAllocationRequest& request,
    uintptr_t* address,
    ImmixAllocationStatus* status)
{
    if ((address == nullptr) ||
        (status == nullptr) ||
        (m_manager == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *address = 0;
    *status = ImmixAllocationStatus::InvalidRequest;
    if ((request.size < MIN_OBJECT_SIZE) ||
        (request.size >= ImmixBlockGeometry::BlockBytes) ||
        (request.alignment < DATA_ALIGNMENT) ||
        ((request.alignment & (request.alignment - 1)) != 0) ||
        (request.alignment_offset >= request.alignment))
    {
        return SideMetadataResult::Success;
    }

    uintptr_t start;
    if (!TryAlignWithOffset(
            m_cursor,
            request.alignment,
            request.alignment_offset,
            &start))
    {
        return SideMetadataResult::Success;
    }

    uintptr_t end;
    if (!TryAdd(start, request.size, &end) ||
        (start < m_cursor) ||
        (end > m_limit))
    {
        *status = ImmixAllocationStatus::Exhausted;
        return SideMetadataResult::Success;
    }

    m_cursor = end;
    *address = start;
    *status = ImmixAllocationStatus::Allocated;
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBumpAllocator::Reset(
    ImmixAllocationStatus* status)
{
    if ((status == nullptr) || (m_manager == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }
    if (m_block == 0)
    {
        *status = ImmixAllocationStatus::Ready;
        return SideMetadataResult::Success;
    }

    return ReturnCurrentBlock(status);
}

uintptr_t ImmixBumpAllocator::GetBlock() const
{
    return m_block;
}

uintptr_t ImmixBumpAllocator::GetCursor() const
{
    return m_cursor;
}

uintptr_t ImmixBumpAllocator::GetLimit() const
{
    return m_limit;
}

SideMetadataResult ImmixBumpAllocator::SetCursor(uintptr_t cursor)
{
    if (m_block == 0 || cursor < m_cursor || cursor > m_limit ||
        cursor % DATA_ALIGNMENT != 0)
    {
        return SideMetadataResult::InvalidArgument;
    }
    m_cursor = cursor;
    return SideMetadataResult::Success;
}

bool ImmixBumpAllocator::TryAlignWithOffset(
    uintptr_t cursor,
    size_t alignment,
    size_t alignmentOffset,
    uintptr_t* aligned)
{
    if ((aligned == nullptr) ||
        (alignment == 0) ||
        ((alignment & (alignment - 1)) != 0) ||
        (alignmentOffset >= alignment) ||
        (alignmentOffset > (UINTPTR_MAX - cursor)))
    {
        return false;
    }

    uintptr_t adjusted = cursor + alignmentOffset;
    uintptr_t mask = static_cast<uintptr_t>(alignment - 1);
    if (adjusted > (UINTPTR_MAX - mask))
    {
        return false;
    }

    uintptr_t rounded = (adjusted + mask) & ~mask;
    if (rounded < alignmentOffset)
    {
        return false;
    }

    *aligned = rounded - alignmentOffset;
    return true;
}

SideMetadataResult ImmixBumpAllocator::ReleaseAfterFailure(
    SideMetadataResult failure,
    ImmixAllocationStatus* status)
{
    SideMetadataResult releaseResult = ReturnCurrentBlock(status);
    return releaseResult == SideMetadataResult::Success
        ? failure
        : releaseResult;
}

SideMetadataResult ImmixBumpAllocator::ReturnCurrentBlock(
    ImmixAllocationStatus* status)
{
    SideMetadataResult result = m_manager->ReturnBlock(
        m_block,
        m_owner,
        status);
    if ((result == SideMetadataResult::Success) &&
        (*status == ImmixAllocationStatus::Ready))
    {
        m_block = 0;
        m_cursor = 0;
        m_limit = 0;
        m_next_search_line = 0;
    }
    return result;
}

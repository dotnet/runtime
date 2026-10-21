// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef IMMIX_ALLOCATION_H
#define IMMIX_ALLOCATION_H

#include "immix_block.h"

#include <atomic>

enum class ImmixAllocationSource : uint8_t
{
    MutatorFresh,
    MutatorRecycled,
    GcCopyFresh,
    GcCopyRecycled,
    // P17 S7: a mutator takes a clean block for pinned-object-heap objects. The block is marked "pinned" until it is
    // acquired again for another purpose. A pinned object goes only into a pinned block, and no other allocation goes
    // into one: MutatorPinnedRecycled takes a Reusable pinned block, and every other recycled source skips them.
    MutatorPinnedFresh,
    MutatorPinnedRecycled,
};

enum class ImmixMarkingContext : uint8_t
{
    Inactive,
    ConcurrentOrFinalMark,
};

enum class ImmixAllocationStatus : uint8_t
{
    Allocated,
    Ready,
    Exhausted,
    Contended,
    Stale,
    InvalidPhase,
    InvalidRequest,
};

struct ImmixAllocationRequest
{
    size_t size;
    size_t alignment;
    size_t alignment_offset;
};

struct ImmixHole
{
    uintptr_t start;
    uintptr_t end;
    size_t line_count;
};

class ImmixAllocationManager final
{
public:
    ImmixAllocationManager();
    ImmixAllocationManager(const ImmixAllocationManager&) = delete;
    ImmixAllocationManager& operator=(const ImmixAllocationManager&) = delete;

    SideMetadataResult Initialize(
        SideMetadataManager* metadata,
        ImmixBlockManager* blocks,
        uintptr_t arenaStart,
        size_t arenaSize,
        size_t minimumReuseLines);
    // Requires all allocation workers to be quiescent.
    void Shutdown();
    SideMetadataResult GrowQuiescent(size_t arenaSize);
    // Lowers the arena to arenaSize. Requires all allocation workers to be quiescent and no allocator to hold a
    // block at or above the new end. The cursors wrap on the new block count.
    SideMetadataResult ShrinkQuiescent(size_t arenaSize);
    // P17 S5b placement. With it, mutators take fresh blocks only below mutatorFreshBlocks (the kept capacity), and the
    // collector takes fresh blocks for copying from the lowest free block upwards, restarting at block 0 in every pause
    // (ResetCopyCursorQuiescent). Survivors then fill free blocks low in the arena and the borrowed range above them is
    // free at the end of the pause. Without it both use one shared round-robin cursor over the whole arena.
    void EnablePlacementQuiescent(size_t mutatorFreshBlocks);
    void SetMutatorFreshBlocksQuiescent(size_t mutatorFreshBlocks);
    void ResetCopyCursorQuiescent();

    SideMetadataResult TryFindNextHole(
        uintptr_t block,
        size_t searchLine,
        ImmixHole* hole,
        size_t* nextSearchLine,
        ImmixAllocationStatus* status) const;

    size_t GetFreshBlocksAcquired() const;
    size_t GetRecycledBlocksAcquired() const;
    size_t GetRecycledLinesConsumed() const;
    size_t GetBlocksInUse() const;
    SideMetadataResult NotifyBlockReleased();

    // Storage that the owner of the manager provides, one byte per block unless noted. Every successful block
    // acquisition is noted here, so that no pause has to walk all blocks to find the blocks that allocators touched.
    //   allocated_flags / allocated_list / allocated_count: the blocks that mutators took since the last pause, each
    //     once. The pause releases those that are clean nursery blocks and that no object promoted.
    //   refresh_flags: a block that a mutator took as a recycled block, or that a collector thread took for copying.
    //     Its free lines have to be counted again after the next pause.
    //   pinned_flags (P17 S7): the owner's byte for each block. The manager sets PinnedBlockFlag in it when a mutator
    //     takes the block as a pinned block, and clears the flag when the block is taken for any other purpose. The
    //     owner may keep other bits in the same byte.
    struct AcquireNotes
    {
        std::atomic<uint8_t>* allocated_flags;
        uint32_t* allocated_list;
        std::atomic<size_t>* allocated_count;
        std::atomic<uint8_t>* refresh_flags;
        std::atomic<uint8_t>* pinned_flags = nullptr;
    };
    static constexpr uint8_t PinnedBlockFlag = 1;
    void SetAcquireNotes(const AcquireNotes& notes);

private:
    friend class ImmixBumpAllocator;

    static bool IsCopySource(ImmixAllocationSource source);
    static bool IsFreshSource(ImmixAllocationSource source);
    static ImmixBlockAcquireKind GetAcquireKind(ImmixAllocationSource source);
    static ImmixAllocationStatus GetAllocationStatus(
        ImmixBlockOperationStatus status);

    SideMetadataResult IsLineDead(
        uintptr_t block,
        size_t lineIndex,
        bool* dead) const;
    SideMetadataResult AcquireBlock(
        uintptr_t owner,
        ImmixAllocationSource source,
        uintptr_t* block,
        ImmixAllocationStatus* status);
    SideMetadataResult ReturnBlock(
        uintptr_t block,
        uintptr_t owner,
        ImmixAllocationStatus* status);
    SideMetadataResult PrepareFreshBlock(
        uintptr_t block,
        ImmixAllocationSource source,
        ImmixMarkingContext markingContext);
    SideMetadataResult PrepareRecycledHole(
        const ImmixHole& hole,
        ImmixAllocationSource source,
        ImmixMarkingContext markingContext);
    SideMetadataResult InitializeFieldLogRange(
        uintptr_t start,
        size_t size,
        bool copy);
    SideMetadataResult InitializeObjectMarkRange(
        uintptr_t start,
        size_t size,
        uintptr_t value);
    SideMetadataResult IncrementLineReuseRange(
        uintptr_t start,
        size_t size);
    void NoteAcquired(size_t index, bool fresh, bool mutator);

    SideMetadataManager* m_metadata;
    // Inline accessor for the reference-count plane (line scan).
    // The arena that this manager serves is always inside a data range that the metadata manager owns.
    SideMetadataAccessor m_reference_count;
    ImmixBlockManager* m_blocks;
    uintptr_t m_arena_start;
    size_t m_arena_size;
    size_t m_block_count;
    size_t m_minimum_reuse_lines;
    std::atomic<size_t> m_next_fresh_block;
    std::atomic<size_t> m_next_recycled_block;
    bool m_placement = false;
    size_t m_mutator_fresh_blocks = 0;
    std::atomic<size_t> m_next_copy_block{0};
    // Block-state token at which a mutator search for recycled blocks scanned every block and found none.
    // The token changes at every epoch flip and when a block is made Reusable, so the search is not repeated
    // while no block can have become a candidate. (The epoch value itself wraps and cannot be used.)
    std::atomic<uint64_t> m_recycled_exhausted_token{UINT64_MAX};
    std::atomic<uint64_t> m_pinned_exhausted_token{UINT64_MAX};
    AcquireNotes m_notes{};
    std::atomic<size_t> m_fresh_blocks_acquired;
    std::atomic<size_t> m_recycled_blocks_acquired;
    std::atomic<size_t> m_recycled_lines_consumed;
    std::atomic<size_t> m_blocks_in_use{0};
};

class ImmixBumpAllocator final
{
public:
    ImmixBumpAllocator();
    ImmixBumpAllocator(const ImmixBumpAllocator&) = delete;
    ImmixBumpAllocator& operator=(const ImmixBumpAllocator&) = delete;

    SideMetadataResult Initialize(
        ImmixAllocationManager* manager,
        uintptr_t owner);
    SideMetadataResult TryRefill(
        ImmixAllocationSource source,
        ImmixMarkingContext markingContext,
        ImmixAllocationStatus* status);
    SideMetadataResult TryAllocate(
        const ImmixAllocationRequest& request,
        uintptr_t* address,
        ImmixAllocationStatus* status);
    SideMetadataResult Reset(ImmixAllocationStatus* status);

    uintptr_t GetBlock() const;
    uintptr_t GetCursor() const;
    uintptr_t GetLimit() const;
    SideMetadataResult SetCursor(uintptr_t cursor);

private:
    static bool TryAlignWithOffset(
        uintptr_t cursor,
        size_t alignment,
        size_t alignmentOffset,
        uintptr_t* aligned);
    SideMetadataResult ReleaseAfterFailure(
        SideMetadataResult failure,
        ImmixAllocationStatus* status);
    SideMetadataResult ReturnCurrentBlock(ImmixAllocationStatus* status);

    ImmixAllocationManager* m_manager;
    uintptr_t m_owner;
    uintptr_t m_block;
    uintptr_t m_cursor;
    uintptr_t m_limit;
    size_t m_next_search_line;
    ImmixAllocationSource m_source;
};

#endif // IMMIX_ALLOCATION_H

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef IMMIX_ALLOCATION_H
#define IMMIX_ALLOCATION_H

#include "immix_block.h"

#include <atomic>

// Test hooks of F9 (the probe counter, the check of the clean-range invariant and the controls). The native test platform
// of the engine tests enables them; the validation program of P2.4 enables them with -DLXR_F9_TEST_HOOKS.
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && !defined(LXR_F9_TEST_HOOKS)
#define LXR_F9_TEST_HOOKS
#endif

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
    // The Java LXR starts every pause, and ends it, with the block cursors at 0 (BlockPageResource::prepare_gc, reset).
    void ResetCursors();

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
#ifdef LXR_F9_TEST_HOOKS
    // Test only. The number of lines whose counts the hole search has read since the manager was initialized.
    uint64_t GetLineProbesForTest() const
    {
        return m_line_probes.load(std::memory_order_relaxed);
    }
#endif // LXR_F9_TEST_HOOKS
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
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    // Test only. The hook runs in AcquireBlock after a candidate has passed the unlocked checks and before the locked
    // acquire. A test uses it to run the work of another thread inside that window.
    using AfterPrecheckHook = void (*)(void* context, uintptr_t candidate);
    void SetAfterPrecheckHookForTest(AfterPrecheckHook hook, void* context)
    {
        m_after_precheck_hook = hook;
        m_after_precheck_context = context;
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

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
    // The line scan of the Java LXR (RCArray::is_dead): the counts of one line are a few whole words of the count plane, and
    // the line is dead when all of them are zero. blockWords is the first word of the count plane of the block.
    bool IsLineDeadFast(const volatile uintptr_t* blockWords, size_t lineIndex) const
    {
#ifdef LXR_F9_TEST_HOOKS
        m_line_probes.fetch_add(1, std::memory_order_relaxed);
#endif // LXR_F9_TEST_HOOKS
        const volatile uintptr_t* words = blockWords + lineIndex * m_rc_words_per_line;
#if defined(LXR_F9_TEST_HOOKS) && defined(LXR_F9_SCAN_ONE_WORD_CONTROL)
        // Control: only the first word of the counts of a line is read.
        return VolatileLoadWithoutBarrier(words) == 0;
#else
        for (size_t word = 0; word < m_rc_words_per_line; word++)
        {
            if (VolatileLoadWithoutBarrier(words + word) != 0)
            {
                return false;
            }
        }
        return true;
#endif // LXR_F9_TEST_HOOKS && LXR_F9_SCAN_ONE_WORD_CONTROL
    }
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
    // Words of the count plane for one line: 1, 2 or 4 for counts of 2, 4 or 8 bits.
    size_t m_rc_words_per_line = 0;
    ImmixBlockManager* m_blocks;
    uintptr_t m_arena_start;
    size_t m_arena_size;
    size_t m_block_count;
    size_t m_minimum_reuse_lines;
    // The cursors of the block search (the Java LXR: blockpageresource_nosweep.rs, clean_block_cursor and reuse_block_cursor).
    // A cursor only moves forward through the arena, and the search for a block stops at the end. The cursors go back to 0
    // at the start and at the end of every pause, and when the arena changes size (ResetCursorsQuiescent).
    std::atomic<size_t> m_next_fresh_block;
    std::atomic<size_t> m_next_recycled_block;
    std::atomic<size_t> m_next_pinned_block{0};
    bool m_placement = false;
    size_t m_mutator_fresh_blocks = 0;
    std::atomic<size_t> m_next_copy_block{0};
    AcquireNotes m_notes{};
    std::atomic<size_t> m_fresh_blocks_acquired;
    std::atomic<size_t> m_recycled_blocks_acquired;
    std::atomic<size_t> m_recycled_lines_consumed;
    std::atomic<size_t> m_blocks_in_use{0};
#ifdef LXR_F9_TEST_HOOKS
    mutable std::atomic<uint64_t> m_line_probes{0};
#endif // LXR_F9_TEST_HOOKS
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    AfterPrecheckHook m_after_precheck_hook = nullptr;
    void* m_after_precheck_context = nullptr;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
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

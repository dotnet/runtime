// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef IMMIX_BLOCK_H
#define IMMIX_BLOCK_H

#include "side_metadata.h"

#include <atomic>

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
class LxrBlockReleaseGuard;
#endif

enum class ImmixGeometryResult : uint8_t
{
    Success,
    InvalidArgument,
    AddressOverflow,
    CrossesBlock,
};

struct ImmixGeometryRange
{
    uintptr_t first;
    uintptr_t end;
    size_t count;
};

class ImmixBlockGeometry final
{
public:
    static constexpr uint8_t BlockLogBytes = 15;
    static constexpr size_t BlockBytes = static_cast<size_t>(1) << BlockLogBytes;
    static constexpr uintptr_t BlockMask = BlockBytes - 1;
    static constexpr uint8_t LineLogBytes = 8;
    static constexpr size_t LineBytes = static_cast<size_t>(1) << LineLogBytes;
    static constexpr uintptr_t LineMask = LineBytes - 1;
    static constexpr uint8_t BlockLogLines = BlockLogBytes - LineLogBytes;
    static constexpr size_t LinesPerBlock = static_cast<size_t>(1) << BlockLogLines;
    static constexpr uint64_t AddressLimit = LxrSideMetadataLayout::AddressLimit;

    static bool IsBlockAligned(uintptr_t address);
    static bool IsLineAligned(uintptr_t address);

    static ImmixGeometryResult GetBlockStart(uintptr_t address, uintptr_t* blockStart);
    static ImmixGeometryResult GetLineStart(uintptr_t address, uintptr_t* lineStart);
    static ImmixGeometryResult AlignBlockUp(uintptr_t address, uintptr_t* blockEnd);
    static ImmixGeometryResult AlignLineUp(uintptr_t address, uintptr_t* lineEnd);

    static ImmixGeometryResult GetBlockIndex(uintptr_t address, size_t* blockIndex);
    static ImmixGeometryResult GetLineIndex(uintptr_t address, size_t* lineIndex);
    static ImmixGeometryResult GetLineIndexInBlock(uintptr_t address, size_t* lineIndex);

    static ImmixGeometryResult GetBlockRange(
        uintptr_t start,
        size_t size,
        ImmixGeometryRange* range);
    static ImmixGeometryResult GetLineRange(
        uintptr_t start,
        size_t size,
        ImmixGeometryRange* range);
    static ImmixGeometryResult GetObjectLineRange(
        uintptr_t start,
        size_t size,
        ImmixGeometryRange* range);

private:
    static ImmixGeometryResult AlignUp(
        uintptr_t address,
        size_t alignment,
        uintptr_t* result);
    static ImmixGeometryResult GetRange(
        uintptr_t start,
        size_t size,
        uint8_t logBytes,
        uintptr_t mask,
        ImmixGeometryRange* range);
};

enum class ImmixBlockState : uint8_t
{
    Unallocated,
    Unmarked,
    Marked,
    Reusable,
};

enum class ImmixBlockAcquireKind : uint8_t
{
    MutatorFresh,
    MutatorReusable,
    GcCopyFresh,
    GcCopyReusable,
    GcMatureEvacuationFresh,
};

enum class ImmixBlockOperationStatus : uint8_t
{
    Updated,
    Unchanged,
    Contended,
    Stale,
    InvalidTransition,
};

class ImmixBlockManager final
{
public:
    static constexpr uint8_t InitialPhaseEpoch = 1;
    static constexpr uint8_t LastPhaseEpoch = 254;
    static constexpr uintptr_t NoOwner = 0;

    ImmixBlockManager();
    ImmixBlockManager(const ImmixBlockManager&) = delete;
    ImmixBlockManager& operator=(const ImmixBlockManager&) = delete;

    SideMetadataResult Initialize(SideMetadataManager* metadata);
    void Shutdown();
    SideMetadataResult RegisterBlockRange(uintptr_t start, size_t size);

    uint8_t GetGlobalPhaseEpoch() const;
    SideMetadataResult StartGcPause(ImmixBlockOperationStatus* status);
    SideMetadataResult ReleaseGcPause(ImmixBlockOperationStatus* status);

    SideMetadataResult GetState(
        uintptr_t block,
        ImmixBlockState* state,
        uint8_t* unavailableLines) const;
    SideMetadataResult GetBlockPhaseEpoch(uintptr_t block, uint8_t* epoch) const;
    SideMetadataResult IsNursery(uintptr_t block, bool* result) const;
    SideMetadataResult IsReusing(uintptr_t block, bool* result) const;
    SideMetadataResult IsGcReusing(uintptr_t block, bool* result) const;

    SideMetadataResult TryAcquire(
        uintptr_t block,
        uintptr_t owner,
        ImmixBlockAcquireKind kind,
        ImmixBlockOperationStatus* status);
    SideMetadataResult TryReturn(
        uintptr_t block,
        uintptr_t owner,
        ImmixBlockOperationStatus* status);
    SideMetadataResult TryRelease(
        uintptr_t block,
        uintptr_t owner,
        ImmixBlockOperationStatus* status);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    SideMetadataResult TryReleaseIfRcDead(
        uintptr_t block,
        const LxrBlockReleaseGuard* guard,
        ImmixBlockOperationStatus* status);
#endif
    SideMetadataResult PrepareForTrace(
        uintptr_t block,
        ImmixBlockOperationStatus* status);
    SideMetadataResult Mark(
        uintptr_t block,
        ImmixBlockOperationStatus* status);
    SideMetadataResult SetReusable(
        uintptr_t block,
        uint8_t unavailableLines,
        ImmixBlockOperationStatus* status);
    SideMetadataResult TryPromoteInPlace(
        uintptr_t block,
        ImmixBlockOperationStatus* status);

    SideMetadataResult TryLog(uintptr_t block, bool* logged);
    SideMetadataResult Unlog(uintptr_t block);

private:
    class BlockOperationScope final
    {
    public:
        explicit BlockOperationScope(const ImmixBlockManager* manager);
        ~BlockOperationScope();

    private:
        const ImmixBlockManager* m_manager;
    };

    static constexpr uint8_t RawUnallocated = 0;
    static constexpr uint8_t RawMarked = UINT8_MAX - 1;
    static constexpr uint8_t RawUnmarked = UINT8_MAX;

    static uint8_t EncodeState(ImmixBlockState state, uint8_t unavailableLines);
    static void DecodeState(
        uint8_t rawState,
        ImmixBlockState* state,
        uint8_t* unavailableLines);
    static bool IsMutatorAcquire(ImmixBlockAcquireKind kind);
    static bool IsFreshAcquire(ImmixBlockAcquireKind kind);
    static bool IsMatureEvacuationAcquire(ImmixBlockAcquireKind kind);

    SideMetadataResult ValidateBlock(uintptr_t block) const;
    SideMetadataResult LoadByte(
        LxrSideMetadataKind kind,
        uintptr_t block,
        uint8_t* value) const;
    SideMetadataResult StoreByte(
        LxrSideMetadataKind kind,
        uintptr_t block,
        uint8_t value);
    SideMetadataResult CompareExchangeByte(
        LxrSideMetadataKind kind,
        uintptr_t block,
        uint8_t value,
        uint8_t comparand,
        uint8_t* observed,
        bool* exchanged);
    SideMetadataResult TryLock(uintptr_t block, bool* acquired);
    SideMetadataResult Unlock(uintptr_t block);
    SideMetadataResult LoadOwner(uintptr_t block, uintptr_t* owner) const;
    SideMetadataResult StoreOwner(uintptr_t block, uintptr_t owner);
    SideMetadataResult IsNurseryOrReusing(uintptr_t block, bool* result) const;
    SideMetadataResult TransitionLocked(
        uintptr_t block,
        ImmixBlockState expected,
        uint8_t expectedUnavailableLines,
        ImmixBlockState desired,
        uint8_t desiredUnavailableLines,
        ImmixBlockOperationStatus* status);
    void BeginBlockOperation() const;
    void EndBlockOperation() const;
    bool BeginPhaseTransition();
    void EndPhaseTransition();

    SideMetadataManager* m_metadata;
    std::atomic<uint8_t> m_global_phase_epoch;
    mutable std::atomic<uint32_t> m_active_operations;
    mutable std::atomic<bool> m_phase_transition;
};

static_assert(ImmixBlockGeometry::BlockBytes == 32 * 1024);
static_assert(ImmixBlockGeometry::LineBytes == 256);
static_assert(ImmixBlockGeometry::LinesPerBlock == 128);
static_assert((ImmixBlockGeometry::BlockBytes & ImmixBlockGeometry::BlockMask) == 0);
static_assert((ImmixBlockGeometry::LineBytes & ImmixBlockGeometry::LineMask) == 0);

#endif // IMMIX_BLOCK_H

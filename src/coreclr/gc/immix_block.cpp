// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "immix_block.h"
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
#include "lxr_completion_counter.h"
#endif

namespace
{
    constexpr SideMetadataMemoryOrder MetadataOrder =
        SideMetadataMemoryOrder::SequentiallyConsistent;

    bool TryAddSize(uintptr_t start, size_t size, uintptr_t* end)
    {
        if ((end == nullptr) || (size > (UINTPTR_MAX - start)))
        {
            return false;
        }

        *end = start + size;
        return true;
    }
}

bool ImmixBlockGeometry::IsBlockAligned(uintptr_t address)
{
    return (address & BlockMask) == 0;
}

bool ImmixBlockGeometry::IsLineAligned(uintptr_t address)
{
    return (address & LineMask) == 0;
}

ImmixGeometryResult ImmixBlockGeometry::GetBlockStart(
    uintptr_t address,
    uintptr_t* blockStart)
{
    if (blockStart == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }
    if (address >= AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    *blockStart = address & ~BlockMask;
    return ImmixGeometryResult::Success;
}

ImmixGeometryResult ImmixBlockGeometry::GetLineStart(
    uintptr_t address,
    uintptr_t* lineStart)
{
    if (lineStart == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }
    if (address >= AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    *lineStart = address & ~LineMask;
    return ImmixGeometryResult::Success;
}

ImmixGeometryResult ImmixBlockGeometry::AlignBlockUp(
    uintptr_t address,
    uintptr_t* blockEnd)
{
    return AlignUp(address, BlockBytes, blockEnd);
}

ImmixGeometryResult ImmixBlockGeometry::AlignLineUp(
    uintptr_t address,
    uintptr_t* lineEnd)
{
    return AlignUp(address, LineBytes, lineEnd);
}

ImmixGeometryResult ImmixBlockGeometry::GetBlockIndex(
    uintptr_t address,
    size_t* blockIndex)
{
    if (blockIndex == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }
    if (address >= AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    *blockIndex = static_cast<size_t>(address >> BlockLogBytes);
    return ImmixGeometryResult::Success;
}

ImmixGeometryResult ImmixBlockGeometry::GetLineIndex(
    uintptr_t address,
    size_t* lineIndex)
{
    if (lineIndex == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }
    if (address >= AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    *lineIndex = static_cast<size_t>(address >> LineLogBytes);
    return ImmixGeometryResult::Success;
}

ImmixGeometryResult ImmixBlockGeometry::GetLineIndexInBlock(
    uintptr_t address,
    size_t* lineIndex)
{
    if (lineIndex == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }
    if (address >= AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    *lineIndex = static_cast<size_t>((address & BlockMask) >> LineLogBytes);
    return ImmixGeometryResult::Success;
}

ImmixGeometryResult ImmixBlockGeometry::GetBlockRange(
    uintptr_t start,
    size_t size,
    ImmixGeometryRange* range)
{
    return GetRange(start, size, BlockLogBytes, BlockMask, range);
}

ImmixGeometryResult ImmixBlockGeometry::GetLineRange(
    uintptr_t start,
    size_t size,
    ImmixGeometryRange* range)
{
    return GetRange(start, size, LineLogBytes, LineMask, range);
}

ImmixGeometryResult ImmixBlockGeometry::GetObjectLineRange(
    uintptr_t start,
    size_t size,
    ImmixGeometryRange* range)
{
    ImmixGeometryResult result = GetLineRange(start, size, range);
    if ((result != ImmixGeometryResult::Success) || (size == 0))
    {
        return result;
    }

    uintptr_t end = start + size;
    if ((start >> BlockLogBytes) != ((end - 1) >> BlockLogBytes))
    {
        return ImmixGeometryResult::CrossesBlock;
    }

    return ImmixGeometryResult::Success;
}

ImmixGeometryResult ImmixBlockGeometry::AlignUp(
    uintptr_t address,
    size_t alignment,
    uintptr_t* result)
{
    if (result == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }
    if (address > AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    uintptr_t mask = static_cast<uintptr_t>(alignment - 1);
    if ((address & mask) == 0)
    {
        *result = address;
        return ImmixGeometryResult::Success;
    }

    uintptr_t adjustment = alignment - (address & mask);
    if (static_cast<uint64_t>(address) > (AddressLimit - adjustment))
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    *result = address + adjustment;
    return static_cast<uint64_t>(*result) <= AddressLimit
        ? ImmixGeometryResult::Success
        : ImmixGeometryResult::AddressOverflow;
}

ImmixGeometryResult ImmixBlockGeometry::GetRange(
    uintptr_t start,
    size_t size,
    uint8_t logBytes,
    uintptr_t mask,
    ImmixGeometryRange* range)
{
    if (range == nullptr)
    {
        return ImmixGeometryResult::InvalidArgument;
    }

    range->first = start;
    range->end = start;
    range->count = 0;
    if (size == 0)
    {
        return static_cast<uint64_t>(start) <= AddressLimit
            ? ImmixGeometryResult::Success
            : ImmixGeometryResult::AddressOverflow;
    }
    if (start >= AddressLimit)
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    uintptr_t end;
    if (!TryAddSize(start, size, &end) ||
        (static_cast<uint64_t>(end) > AddressLimit))
    {
        return ImmixGeometryResult::AddressOverflow;
    }

    uintptr_t alignedEnd;
    ImmixGeometryResult result = AlignUp(end, static_cast<size_t>(1) << logBytes, &alignedEnd);
    if (result != ImmixGeometryResult::Success)
    {
        return result;
    }

    range->first = start & ~mask;
    range->end = alignedEnd;
    range->count = static_cast<size_t>((alignedEnd - range->first) >> logBytes);
    return ImmixGeometryResult::Success;
}

ImmixBlockManager::ImmixBlockManager()
    : m_metadata(nullptr)
    , m_global_phase_epoch(InitialPhaseEpoch)
    , m_active_operations(0)
    , m_phase_transition(false)
{
    static_assert(std::atomic<uint8_t>::is_always_lock_free);
}

ImmixBlockManager::BlockOperationScope::BlockOperationScope(
    const ImmixBlockManager* manager)
    : m_manager(manager)
{
    m_manager->BeginBlockOperation();
}

ImmixBlockManager::BlockOperationScope::~BlockOperationScope()
{
    m_manager->EndBlockOperation();
}

SideMetadataResult ImmixBlockManager::Initialize(SideMetadataManager* metadata)
{
    if ((metadata == nullptr) || (m_metadata != nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    constexpr LxrSideMetadataKind RequiredKinds[] = {
        LxrSideMetadataKind::BlockDefrag,
        LxrSideMetadataKind::BlockMark,
        LxrSideMetadataKind::BlockLog,
        LxrSideMetadataKind::NurseryPromotion,
        LxrSideMetadataKind::PhaseEpoch,
        LxrSideMetadataKind::BlockOwner,
        LxrSideMetadataKind::BlockInUse,
    };
    for (LxrSideMetadataKind kind : RequiredKinds)
    {
        if (!metadata->IsSpecEnabled(kind))
        {
            return SideMetadataResult::InvalidArgument;
        }
    }

    m_metadata = metadata;
    m_global_phase_epoch.store(InitialPhaseEpoch, std::memory_order_seq_cst);
    m_active_operations.store(0, std::memory_order_seq_cst);
    m_phase_transition.store(false, std::memory_order_seq_cst);
    return SideMetadataResult::Success;
}

void ImmixBlockManager::Shutdown()
{
    m_metadata = nullptr;
    m_global_phase_epoch.store(InitialPhaseEpoch, std::memory_order_seq_cst);
    m_active_operations.store(0, std::memory_order_seq_cst);
    m_phase_transition.store(false, std::memory_order_seq_cst);
}

SideMetadataResult ImmixBlockManager::RegisterBlockRange(uintptr_t start, size_t size)
{
    if ((m_metadata == nullptr) ||
        !ImmixBlockGeometry::IsBlockAligned(start) ||
        (size == 0) ||
        ((size & ImmixBlockGeometry::BlockMask) != 0))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uintptr_t end;
    if (!TryAddSize(start, size, &end) ||
        (static_cast<uint64_t>(end) > ImmixBlockGeometry::AddressLimit))
    {
        return SideMetadataResult::AddressOverflow;
    }

    return m_metadata->CommitDataRange(start, size);
}

uint8_t ImmixBlockManager::GetGlobalPhaseEpoch() const
{
    return m_global_phase_epoch.load(std::memory_order_seq_cst);
}

SideMetadataResult ImmixBlockManager::StartGcPause(ImmixBlockOperationStatus* status)
{
    if ((status == nullptr) || (m_metadata == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::InvalidTransition;
    if (!BeginPhaseTransition())
    {
        *status = ImmixBlockOperationStatus::Contended;
        return SideMetadataResult::Success;
    }

    uint8_t oldEpoch = m_global_phase_epoch.load(std::memory_order_seq_cst);
    if ((oldEpoch & 1) == 0)
    {
        EndPhaseTransition();
        return SideMetadataResult::Success;
    }

    m_global_phase_epoch.store(
        static_cast<uint8_t>(oldEpoch + 1),
        std::memory_order_seq_cst);
    m_flip_serial.fetch_add(1, std::memory_order_acq_rel);
    *status = ImmixBlockOperationStatus::Updated;
    EndPhaseTransition();
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBlockManager::ReleaseGcPause(ImmixBlockOperationStatus* status)
{
    if ((status == nullptr) || (m_metadata == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::InvalidTransition;
    if (!BeginPhaseTransition())
    {
        *status = ImmixBlockOperationStatus::Contended;
        return SideMetadataResult::Success;
    }

    uint8_t oldEpoch = m_global_phase_epoch.load(std::memory_order_seq_cst);
    if ((oldEpoch & 1) != 0)
    {
        EndPhaseTransition();
        return SideMetadataResult::Success;
    }

    uint8_t newEpoch = oldEpoch == LastPhaseEpoch
        ? InitialPhaseEpoch
        : static_cast<uint8_t>(oldEpoch + 1);
    m_global_phase_epoch.store(newEpoch, std::memory_order_seq_cst);
    if (oldEpoch == LastPhaseEpoch)
    {
        SideMetadataResult result = m_metadata->ResetAllDataRangesQuiescent(
            LxrSideMetadataKind::PhaseEpoch,
            0);
        if (result != SideMetadataResult::Success)
        {
            m_global_phase_epoch.store(oldEpoch, std::memory_order_seq_cst);
            EndPhaseTransition();
            return result;
        }
    }

    m_flip_serial.fetch_add(1, std::memory_order_acq_rel);
    *status = ImmixBlockOperationStatus::Updated;
    EndPhaseTransition();
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBlockManager::GetState(
    uintptr_t block,
    ImmixBlockState* state,
    uint8_t* unavailableLines) const
{
    if ((state == nullptr) || (unavailableLines == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    uint8_t rawState;
    SideMetadataResult result = LoadByte(LxrSideMetadataKind::BlockMark, block, &rawState);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    DecodeState(rawState, state, unavailableLines);
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBlockManager::GetBlockPhaseEpoch(
    uintptr_t block,
    uint8_t* epoch) const
{
    return LoadByte(LxrSideMetadataKind::PhaseEpoch, block, epoch);
}

SideMetadataResult ImmixBlockManager::IsNursery(uintptr_t block, bool* result) const
{
    if (result == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *result = false;
    BlockOperationScope operation(this);
    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    SideMetadataResult metadataResult = GetState(block, &state, &unavailableLines);
    if (metadataResult != SideMetadataResult::Success)
    {
        return metadataResult;
    }

    bool nurseryOrReusing;
    metadataResult = IsNurseryOrReusing(block, &nurseryOrReusing);
    if (metadataResult == SideMetadataResult::Success)
    {
        *result = (state == ImmixBlockState::Unallocated) && nurseryOrReusing;
    }
    return metadataResult;
}

SideMetadataResult ImmixBlockManager::IsReusing(uintptr_t block, bool* result) const
{
    if (result == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *result = false;
    BlockOperationScope operation(this);
    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    SideMetadataResult metadataResult = GetState(block, &state, &unavailableLines);
    if (metadataResult != SideMetadataResult::Success)
    {
        return metadataResult;
    }

    bool nurseryOrReusing;
    metadataResult = IsNurseryOrReusing(block, &nurseryOrReusing);
    if (metadataResult == SideMetadataResult::Success)
    {
        *result = (state != ImmixBlockState::Unallocated) && nurseryOrReusing;
    }
    return metadataResult;
}

SideMetadataResult ImmixBlockManager::IsGcReusing(uintptr_t block, bool* result) const
{
    if (result == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *result = false;
    BlockOperationScope operation(this);
    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    SideMetadataResult metadataResult = GetState(block, &state, &unavailableLines);
    if ((metadataResult != SideMetadataResult::Success) ||
        (state == ImmixBlockState::Unallocated))
    {
        return metadataResult;
    }

    uint8_t globalEpoch = GetGlobalPhaseEpoch();
    if ((globalEpoch & 1) != 0)
    {
        return SideMetadataResult::InvalidArgument;
    }

    uint8_t blockEpoch = 0;
    metadataResult = GetBlockPhaseEpoch(block, &blockEpoch);
    if (metadataResult == SideMetadataResult::Success)
    {
        *result = blockEpoch == globalEpoch;
    }
    return metadataResult;
}

SideMetadataResult ImmixBlockManager::TryAcquire(
    uintptr_t block,
    uintptr_t owner,
    ImmixBlockAcquireKind kind,
    ImmixBlockOperationStatus* status)
{
    if ((status == nullptr) ||
        (owner == NoOwner) ||
        (kind > ImmixBlockAcquireKind::GcMatureEvacuationFresh))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    uintptr_t currentOwner = NoOwner;
    result = GetState(block, &state, &unavailableLines);
    if (result == SideMetadataResult::Success)
    {
        result = LoadOwner(block, &currentOwner);
    }
    if (result != SideMetadataResult::Success)
    {
        Unlock(block);
        return result;
    }

    bool mutator = IsMutatorAcquire(kind);
    bool fresh = IsFreshAcquire(kind);
    uint8_t globalEpoch = GetGlobalPhaseEpoch();
    bool validPhase = mutator ? ((globalEpoch & 1) != 0) : ((globalEpoch & 1) == 0);
    bool validState = fresh
        ? state == ImmixBlockState::Unallocated
        : state != ImmixBlockState::Unallocated;
    if (!validPhase || !validState)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (currentOwner != NoOwner)
    {
        *status = ImmixBlockOperationStatus::Stale;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    uint8_t blockEpoch = 0;
    result = GetBlockPhaseEpoch(block, &blockEpoch);
    if (result != SideMetadataResult::Success)
    {
        Unlock(block);
        return result;
    }

    bool nurseryOrReusing = (globalEpoch & 1) != 0
        ? blockEpoch == globalEpoch
        : blockEpoch == static_cast<uint8_t>(globalEpoch - 1);
    bool reusing =
        (state != ImmixBlockState::Unallocated) && nurseryOrReusing;
    bool gcReusing =
        (state != ImmixBlockState::Unallocated) &&
        ((globalEpoch & 1) == 0) &&
        (blockEpoch == globalEpoch);
    bool nursery =
        (state == ImmixBlockState::Unallocated) && nurseryOrReusing;
    if ((!fresh && (reusing || (!mutator && gcReusing))) ||
        ((kind == ImmixBlockAcquireKind::GcCopyFresh) && nursery))
    {
        *status = ImmixBlockOperationStatus::Stale;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    uintptr_t defrag = 0;
    result = m_metadata->Load(
        LxrSideMetadataKind::BlockDefrag,
        block,
        MetadataOrder,
        &defrag);
    if (result != SideMetadataResult::Success)
    {
        Unlock(block);
        return result;
    }
    if (!fresh && (defrag != 0))
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    result = StoreByte(LxrSideMetadataKind::PhaseEpoch, block, globalEpoch);
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::NurseryPromotion, block, 0);
    }
    if (result == SideMetadataResult::Success && !mutator)
    {
        uint8_t rawState = EncodeState(ImmixBlockState::Unmarked, 0);
        result = StoreByte(LxrSideMetadataKind::BlockMark, block, rawState);
    }
    if (result == SideMetadataResult::Success && fresh)
    {
        result = StoreByte(LxrSideMetadataKind::BlockDefrag, block, 0);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreOwner(block, owner);
    }

    SideMetadataResult unlockResult = Unlock(block);
    if (result == SideMetadataResult::Success)
    {
        result = unlockResult;
    }
    if (result == SideMetadataResult::Success)
    {
        *status = ImmixBlockOperationStatus::Updated;
    }
    return result;
}

SideMetadataResult ImmixBlockManager::TryReturn(
    uintptr_t block,
    uintptr_t owner,
    ImmixBlockOperationStatus* status)
{
    if ((status == nullptr) || (owner == NoOwner))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    uintptr_t currentOwner = NoOwner;
    result = LoadOwner(block, &currentOwner);
    if (result == SideMetadataResult::Success && currentOwner != owner)
    {
        *status = ImmixBlockOperationStatus::Stale;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreOwner(block, NoOwner);
    }

    SideMetadataResult unlockResult = Unlock(block);
    if (result == SideMetadataResult::Success)
    {
        result = unlockResult;
    }
    if (result == SideMetadataResult::Success)
    {
        *status = ImmixBlockOperationStatus::Updated;
    }
    return result;
}

SideMetadataResult ImmixBlockManager::TryRelease(
    uintptr_t block,
    uintptr_t owner,
    ImmixBlockOperationStatus* status)
{
    if ((status == nullptr) || (owner == NoOwner))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    uintptr_t currentOwner = NoOwner;
    result = LoadOwner(block, &currentOwner);
    if (result == SideMetadataResult::Success && currentOwner != owner)
    {
        *status = ImmixBlockOperationStatus::Stale;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    bool nurseryOrReusing = false;
    if (result == SideMetadataResult::Success)
    {
        result = GetState(block, &state, &unavailableLines);
    }
    if (result == SideMetadataResult::Success)
    {
        result = IsNurseryOrReusing(block, &nurseryOrReusing);
    }
    if (result == SideMetadataResult::Success &&
        ((GetGlobalPhaseEpoch() & 1) != 0) &&
        (state != ImmixBlockState::Unallocated) &&
        nurseryOrReusing)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::BlockMark, block, RawUnallocated);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::NurseryPromotion, block, 0);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::BlockDefrag, block, 0);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreOwner(block, NoOwner);
    }

    SideMetadataResult unlockResult = Unlock(block);
    if (result == SideMetadataResult::Success)
    {
        result = unlockResult;
    }
    if (result == SideMetadataResult::Success)
    {
        *status = ImmixBlockOperationStatus::Updated;
    }
    return result;
}

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
SideMetadataResult ImmixBlockManager::TryReleaseIfRcDead(
    uintptr_t block,
    const LxrBlockReleaseGuard* guard,
    ImmixBlockOperationStatus* status)
{
    if (status == nullptr || (guard != nullptr && !guard->IsValid()))
    {
        return SideMetadataResult::InvalidArgument;
    }
    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    bool mutatorPhase = (GetGlobalPhaseEpoch() & 1) != 0;
    if (mutatorPhase && guard == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }
    bool acquired;
    SideMetadataResult result = TryLock(block, &acquired);
    if (result != SideMetadataResult::Success || !acquired)
    {
        return result;
    }
    uintptr_t owner = NoOwner;
    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailable = 0;
    bool nurseryOrReusing = false;
    result = LoadOwner(block, &owner);
    if (result == SideMetadataResult::Success)
    {
        result = GetState(block, &state, &unavailable);
    }
    if (result == SideMetadataResult::Success)
    {
        result = IsNurseryOrReusing(block, &nurseryOrReusing);
    }
    if (result == SideMetadataResult::Success && owner != NoOwner)
    {
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success &&
        ((mutatorPhase && nurseryOrReusing) ||
         (state == ImmixBlockState::Unallocated && !nurseryOrReusing)))
    {
        *status = ImmixBlockOperationStatus::Unchanged;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    bool dead = false;
    if (result == SideMetadataResult::Success)
    {
        result = m_metadata->IsRangeZero(
            LxrSideMetadataKind::ReferenceCount, block,
            ImmixBlockGeometry::BlockBytes, MetadataOrder, &dead);
    }
    if (result == SideMetadataResult::Success && !dead)
    {
        *status = ImmixBlockOperationStatus::Unchanged;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    const LxrSideMetadataKind reset[] = {
        LxrSideMetadataKind::ValidObject,
        LxrSideMetadataKind::FieldUnlogged,
        LxrSideMetadataKind::ReferenceCountStraddle,
    };
    for (LxrSideMetadataKind kind : reset)
    {
        if (result == SideMetadataResult::Success)
        {
            result = m_metadata->ResetRangeQuiescent(
                kind, block, ImmixBlockGeometry::BlockBytes, 0);
        }
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::BlockMark, block, RawUnallocated);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::NurseryPromotion, block, 0);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::BlockDefrag, block, 0);
    }
    SideMetadataResult unlocked = Unlock(block);
    if (result == SideMetadataResult::Success)
    {
        result = unlocked;
        if (result == SideMetadataResult::Success)
        {
            *status = ImmixBlockOperationStatus::Updated;
        }
    }
    return result;
}
#endif

SideMetadataResult ImmixBlockManager::PrepareForTrace(
    uintptr_t block,
    ImmixBlockOperationStatus* status)
{
    if (status == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    if ((GetGlobalPhaseEpoch() & 1) != 0)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        return SideMetadataResult::Success;
    }

    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    result = GetState(block, &state, &unavailableLines);
    if (result == SideMetadataResult::Success && state == ImmixBlockState::Unallocated)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            unavailableLines,
            ImmixBlockState::Unmarked,
            0,
            status);
    }

    SideMetadataResult unlockResult = Unlock(block);
    return result == SideMetadataResult::Success ? unlockResult : result;
}

SideMetadataResult ImmixBlockManager::Mark(
    uintptr_t block,
    ImmixBlockOperationStatus* status)
{
    if (status == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    if ((GetGlobalPhaseEpoch() & 1) != 0)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        return SideMetadataResult::Success;
    }

    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    result = GetState(block, &state, &unavailableLines);
    if (result == SideMetadataResult::Success && state == ImmixBlockState::Unallocated)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            unavailableLines,
            ImmixBlockState::Marked,
            0,
            status);
    }

    SideMetadataResult unlockResult = Unlock(block);
    return result == SideMetadataResult::Success ? unlockResult : result;
}

SideMetadataResult ImmixBlockManager::SetReusable(
    uintptr_t block,
    uint8_t unavailableLines,
    ImmixBlockOperationStatus* status)
{
    if ((status == nullptr) ||
        (unavailableLines == 0) ||
        (unavailableLines > ImmixBlockGeometry::LinesPerBlock))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    if ((GetGlobalPhaseEpoch() & 1) != 0)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        return SideMetadataResult::Success;
    }

    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t oldUnavailableLines = 0;
    result = GetState(block, &state, &oldUnavailableLines);
    if (result == SideMetadataResult::Success && state == ImmixBlockState::Unallocated)
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }
    if (result == SideMetadataResult::Success)
    {
        result = TransitionLocked(
            block,
            state,
            oldUnavailableLines,
            ImmixBlockState::Reusable,
            unavailableLines,
            status);
    }

    SideMetadataResult unlockResult = Unlock(block);
    return result == SideMetadataResult::Success ? unlockResult : result;
}

SideMetadataResult ImmixBlockManager::TryPromoteInPlace(
    uintptr_t block,
    ImmixBlockOperationStatus* status)
{
    if (status == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *status = ImmixBlockOperationStatus::Contended;
    BlockOperationScope operation(this);
    bool acquired = false;
    SideMetadataResult result = TryLock(block, &acquired);
    if ((result != SideMetadataResult::Success) || !acquired)
    {
        return result;
    }

    uint8_t promotionState = 0;
    result = LoadByte(
        LxrSideMetadataKind::NurseryPromotion,
        block,
        &promotionState);
    if ((result == SideMetadataResult::Success) && (promotionState == 1))
    {
        *status = ImmixBlockOperationStatus::Unchanged;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    ImmixBlockState state = ImmixBlockState::Unallocated;
    uint8_t unavailableLines = 0;
    bool nurseryOrReusing = false;
    result = GetState(block, &state, &unavailableLines);
    if (result == SideMetadataResult::Success)
    {
        result = IsNurseryOrReusing(block, &nurseryOrReusing);
    }
    if ((result == SideMetadataResult::Success) &&
        (((GetGlobalPhaseEpoch() & 1) != 0) ||
         (state != ImmixBlockState::Unallocated) ||
         !nurseryOrReusing))
    {
        *status = ImmixBlockOperationStatus::InvalidTransition;
        Unlock(block);
        return SideMetadataResult::Success;
    }

    uint8_t observed = 0;
    bool exchanged = false;
    if (result == SideMetadataResult::Success)
    {
        result = CompareExchangeByte(
            LxrSideMetadataKind::NurseryPromotion,
            block,
            1,
            0,
            &observed,
            &exchanged);
        if (result == SideMetadataResult::Success && !exchanged)
        {
            *status = observed == 1
                ? ImmixBlockOperationStatus::Unchanged
                : ImmixBlockOperationStatus::Stale;
            Unlock(block);
            return SideMetadataResult::Success;
        }
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(LxrSideMetadataKind::BlockMark, block, RawUnmarked);
    }
    if (result == SideMetadataResult::Success)
    {
        result = StoreByte(
            LxrSideMetadataKind::PhaseEpoch,
            block,
            GetGlobalPhaseEpoch());
    }

    SideMetadataResult unlockResult = Unlock(block);
    if (result == SideMetadataResult::Success)
    {
        result = unlockResult;
    }
    if (result == SideMetadataResult::Success)
    {
        *status = ImmixBlockOperationStatus::Updated;
    }
    return result;
}

SideMetadataResult ImmixBlockManager::TryLog(uintptr_t block, bool* logged)
{
    if (logged == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *logged = false;
    uint8_t observed = 0;
    bool exchanged = false;
    SideMetadataResult result = CompareExchangeByte(
        LxrSideMetadataKind::BlockLog,
        block,
        1,
        0,
        &observed,
        &exchanged);
    if (result == SideMetadataResult::Success)
    {
        *logged = exchanged;
    }
    return result;
}

SideMetadataResult ImmixBlockManager::Unlog(uintptr_t block)
{
    return StoreByte(LxrSideMetadataKind::BlockLog, block, 0);
}

uint8_t ImmixBlockManager::EncodeState(
    ImmixBlockState state,
    uint8_t unavailableLines)
{
    switch (state)
    {
        case ImmixBlockState::Unallocated:
            return RawUnallocated;
        case ImmixBlockState::Unmarked:
            return RawUnmarked;
        case ImmixBlockState::Marked:
            return RawMarked;
        case ImmixBlockState::Reusable:
            return unavailableLines;
        default:
            return RawUnallocated;
    }
}

void ImmixBlockManager::DecodeState(
    uint8_t rawState,
    ImmixBlockState* state,
    uint8_t* unavailableLines)
{
    *unavailableLines = 0;
    if (rawState == RawUnallocated)
    {
        *state = ImmixBlockState::Unallocated;
    }
    else if (rawState == RawUnmarked)
    {
        *state = ImmixBlockState::Unmarked;
    }
    else if (rawState == RawMarked)
    {
        *state = ImmixBlockState::Marked;
    }
    else
    {
        *state = ImmixBlockState::Reusable;
        *unavailableLines = rawState;
    }
}

bool ImmixBlockManager::IsMutatorAcquire(ImmixBlockAcquireKind kind)
{
    return (kind == ImmixBlockAcquireKind::MutatorFresh) ||
        (kind == ImmixBlockAcquireKind::MutatorReusable);
}

bool ImmixBlockManager::IsFreshAcquire(ImmixBlockAcquireKind kind)
{
    return (kind == ImmixBlockAcquireKind::MutatorFresh) ||
        (kind == ImmixBlockAcquireKind::GcCopyFresh) ||
        IsMatureEvacuationAcquire(kind);
}

bool ImmixBlockManager::IsMatureEvacuationAcquire(ImmixBlockAcquireKind kind)
{
    return kind == ImmixBlockAcquireKind::GcMatureEvacuationFresh;
}

SideMetadataResult ImmixBlockManager::ValidateBlock(uintptr_t block) const
{
    if (m_metadata == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }
    if ((static_cast<uint64_t>(block) >= ImmixBlockGeometry::AddressLimit) ||
        !ImmixBlockGeometry::IsBlockAligned(block))
    {
        return SideMetadataResult::AddressOverflow;
    }
    return m_metadata->IsDataRangeOwned(block, ImmixBlockGeometry::BlockBytes)
        ? SideMetadataResult::Success
        : SideMetadataResult::AddressNotOwned;
}

SideMetadataResult ImmixBlockManager::LoadByte(
    LxrSideMetadataKind kind,
    uintptr_t block,
    uint8_t* value) const
{
    if (value == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *value = 0;
    SideMetadataResult result = ValidateBlock(block);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t loaded;
    result = m_metadata->Load(kind, block, MetadataOrder, &loaded);
    if (result == SideMetadataResult::Success)
    {
        *value = static_cast<uint8_t>(loaded);
    }
    return result;
}

SideMetadataResult ImmixBlockManager::StoreByte(
    LxrSideMetadataKind kind,
    uintptr_t block,
    uint8_t value)
{
    SideMetadataResult result = ValidateBlock(block);
    return result == SideMetadataResult::Success
        ? m_metadata->Store(kind, block, value, MetadataOrder)
        : result;
}

SideMetadataResult ImmixBlockManager::CompareExchangeByte(
    LxrSideMetadataKind kind,
    uintptr_t block,
    uint8_t value,
    uint8_t comparand,
    uint8_t* observed,
    bool* exchanged)
{
    if ((observed == nullptr) || (exchanged == nullptr))
    {
        return SideMetadataResult::InvalidArgument;
    }

    *observed = 0;
    *exchanged = false;
    SideMetadataResult result = ValidateBlock(block);
    if (result != SideMetadataResult::Success)
    {
        return result;
    }

    uintptr_t rawObserved;
    result = m_metadata->CompareExchange(
        kind,
        block,
        value,
        comparand,
        MetadataOrder,
        &rawObserved,
        exchanged);
    if (result == SideMetadataResult::Success)
    {
        *observed = static_cast<uint8_t>(rawObserved);
    }
    return result;
}

SideMetadataResult ImmixBlockManager::TryLock(uintptr_t block, bool* acquired)
{
    if (acquired == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    uint8_t observed = 0;
    return CompareExchangeByte(
        LxrSideMetadataKind::BlockInUse,
        block,
        1,
        0,
        &observed,
        acquired);
}

SideMetadataResult ImmixBlockManager::Unlock(uintptr_t block)
{
    return StoreByte(LxrSideMetadataKind::BlockInUse, block, 0);
}

SideMetadataResult ImmixBlockManager::LoadOwner(
    uintptr_t block,
    uintptr_t* owner) const
{
    if (owner == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *owner = NoOwner;
    SideMetadataResult result = ValidateBlock(block);
    return result == SideMetadataResult::Success
        ? m_metadata->Load(
            LxrSideMetadataKind::BlockOwner,
            block,
            MetadataOrder,
            owner)
        : result;
}

SideMetadataResult ImmixBlockManager::StoreOwner(
    uintptr_t block,
    uintptr_t owner)
{
    SideMetadataResult result = ValidateBlock(block);
    return result == SideMetadataResult::Success
        ? m_metadata->Store(
            LxrSideMetadataKind::BlockOwner,
            block,
            owner,
            MetadataOrder)
        : result;
}

SideMetadataResult ImmixBlockManager::IsNurseryOrReusing(
    uintptr_t block,
    bool* result) const
{
    if (result == nullptr)
    {
        return SideMetadataResult::InvalidArgument;
    }

    *result = false;
    uint8_t blockEpoch;
    SideMetadataResult metadataResult = GetBlockPhaseEpoch(block, &blockEpoch);
    if (metadataResult != SideMetadataResult::Success)
    {
        return metadataResult;
    }

    uint8_t globalEpoch = GetGlobalPhaseEpoch();
    *result = (globalEpoch & 1) != 0
        ? blockEpoch == globalEpoch
        : blockEpoch == static_cast<uint8_t>(globalEpoch - 1);
    return SideMetadataResult::Success;
}

SideMetadataResult ImmixBlockManager::TransitionLocked(
    uintptr_t block,
    ImmixBlockState expected,
    uint8_t expectedUnavailableLines,
    ImmixBlockState desired,
    uint8_t desiredUnavailableLines,
    ImmixBlockOperationStatus* status)
{
    uint8_t expectedRaw = EncodeState(expected, expectedUnavailableLines);
    uint8_t desiredRaw = EncodeState(desired, desiredUnavailableLines);
    if (expectedRaw == desiredRaw)
    {
        *status = ImmixBlockOperationStatus::Unchanged;
        return SideMetadataResult::Success;
    }

    uint8_t observed = 0;
    bool exchanged = false;
    SideMetadataResult result = CompareExchangeByte(
        LxrSideMetadataKind::BlockMark,
        block,
        desiredRaw,
        expectedRaw,
        &observed,
        &exchanged);
    if (result == SideMetadataResult::Success)
    {
        *status = exchanged
            ? ImmixBlockOperationStatus::Updated
            : ImmixBlockOperationStatus::Stale;
    }
    return result;
}

void ImmixBlockManager::BeginBlockOperation() const
{
    while (true)
    {
        while (m_phase_transition.load(std::memory_order_seq_cst))
        {
            YieldProcessor();
        }

        m_active_operations.fetch_add(1, std::memory_order_seq_cst);
        if (!m_phase_transition.load(std::memory_order_seq_cst))
        {
            return;
        }

        m_active_operations.fetch_sub(1, std::memory_order_seq_cst);
    }
}

void ImmixBlockManager::EndBlockOperation() const
{
    m_active_operations.fetch_sub(1, std::memory_order_seq_cst);
}

bool ImmixBlockManager::BeginPhaseTransition()
{
    bool expected = false;
    if (!m_phase_transition.compare_exchange_strong(
            expected,
            true,
            std::memory_order_seq_cst,
            std::memory_order_seq_cst))
    {
        return false;
    }

    while (m_active_operations.load(std::memory_order_seq_cst) != 0)
    {
        YieldProcessor();
    }
    return true;
}

void ImmixBlockManager::EndPhaseTransition()
{
    m_phase_transition.store(false, std::memory_order_seq_cst);
}

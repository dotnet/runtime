// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "lxr_work_packet.h"

#ifdef byte
#pragma push_macro("byte")
#define LXR_WORK_PACKET_CPP_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_WORK_PACKET_CPP_RESTORE_VALID
#undef __valid
#endif

#include <limits>
#include <utility>

#ifdef LXR_WORK_PACKET_CPP_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_WORK_PACKET_CPP_RESTORE_BYTE
#endif
#ifdef LXR_WORK_PACKET_CPP_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_WORK_PACKET_CPP_RESTORE_VALID
#endif

namespace
{
    constexpr const char* StageNames[] = {
        "Unconstrained",
        "Concurrent",
        "ConcurrentResumable",
        "FinishConcurrentWork",
        "Initial",
        "Prepare",
        "Closure",
        "Release",
        "STWRCDecsAndSweep",
        "Final",
    };

    static_assert((sizeof(StageNames) / sizeof(StageNames[0])) ==
        static_cast<size_t>(LxrWorkBucketStage::Count));
}

const char* LxrGetWorkBucketStageName(LxrWorkBucketStage stage)
{
    size_t index = static_cast<size_t>(stage);
    return index < static_cast<size_t>(LxrWorkBucketStage::Count) ?
        StageNames[index] :
        "Invalid";
}

bool LxrIsConcurrentStage(LxrWorkBucketStage stage)
{
    return stage == LxrWorkBucketStage::Unconstrained ||
        stage == LxrWorkBucketStage::Concurrent ||
        stage == LxrWorkBucketStage::ConcurrentResumable;
}

bool LxrIsStwStage(LxrWorkBucketStage stage)
{
    return stage >= LxrFirstStwStage && stage < LxrWorkBucketStage::Count;
}

bool LxrIsSupportedReferencePacketCapacity(size_t capacity)
{
    return capacity == 256 ||
        capacity == 512 ||
        capacity == 1024 ||
        capacity == 2048 ||
        capacity == LxrEdgePacketReferenceCapacity;
}

bool LxrTryGetReferencePacketCount(size_t referenceCount, size_t capacity, size_t* packetCount)
{
    if (packetCount == nullptr || !LxrIsSupportedReferencePacketCapacity(capacity))
    {
        return false;
    }

    if (referenceCount == 0)
    {
        *packetCount = 0;
        return true;
    }

    if (referenceCount > std::numeric_limits<size_t>::max() - (capacity - 1))
    {
        return false;
    }

    *packetCount = (referenceCount + capacity - 1) / capacity;
    return true;
}

LxrWorkPacket::LxrWorkPacket(
    uint64_t id,
    LxrWorkBucketStage stage,
    const uintptr_t* references,
    size_t referenceCount,
    ExecuteCallback callback,
    void* context)
    : m_id(id),
      m_stage(stage),
      m_references(references),
      m_reference_count(referenceCount),
      m_callback(callback),
      m_context(context),
      m_generation(0),
      m_state(LxrWorkPacketState::CallerOwned),
      m_preferred_worker(static_cast<size_t>(-1)),
      m_deferred(false)
{
}

uint64_t LxrWorkPacket::GetId() const
{
    return m_id;
}

LxrWorkBucketStage LxrWorkPacket::GetStage() const
{
    return m_stage;
}

uint64_t LxrWorkPacket::GetGeneration() const
{
    return m_generation.load(std::memory_order_acquire);
}

const uintptr_t* LxrWorkPacket::GetReferences() const
{
    return m_references;
}

size_t LxrWorkPacket::GetReferenceCount() const
{
    return m_reference_count;
}

LxrWorkPacketState LxrWorkPacket::GetState() const
{
    return m_state.load(std::memory_order_acquire);
}

bool LxrWorkPacket::TryTransition(LxrWorkPacketState expected, LxrWorkPacketState desired)
{
    return m_state.compare_exchange_strong(
        expected,
        desired,
        std::memory_order_acq_rel,
        std::memory_order_acquire);
}

void LxrWorkPacket::SetGeneration(uint64_t generation)
{
    m_generation.store(generation, std::memory_order_release);
}

void LxrWorkPacket::SetPreferredWorker(size_t workerIndex)
{
    m_preferred_worker = workerIndex;
}

size_t LxrWorkPacket::GetPreferredWorker() const
{
    return m_preferred_worker;
}

void LxrWorkPacket::SetDeferred(bool deferred)
{
    m_deferred = deferred;
}

bool LxrWorkPacket::IsDeferred() const
{
    return m_deferred;
}

void LxrWorkPacket::Execute(size_t workerIndex)
{
    if (m_callback != nullptr)
    {
        m_callback(this, workerIndex, m_context);
    }
}

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
bool LxrWorkPacket::AttachCompletionRegistration(
    LxrLazyCompletionRegistration&& registration)
{
    if (!registration.IsActive() || m_completion_registration.IsActive())
    {
        return false;
    }

    m_completion_registration = std::move(registration);
    return true;
}

LxrCompletionResult LxrWorkPacket::CompleteRegistration()
{
    return m_completion_registration.IsActive()
        ? m_completion_registration.Complete()
        : LxrCompletionResult::Success;
}

LxrLazyCompletionRegistration LxrWorkPacket::DetachCompletionRegistration()
{
    return std::move(m_completion_registration);
}

const LxrLazyCompletionRegistration&
LxrWorkPacket::GetCompletionRegistration() const
{
    return m_completion_registration;
}

bool LxrWorkPacket::HasCompletionRegistration() const
{
    return m_completion_registration.IsActive();
}

LxrCompletionResult LxrWorkPacket::CreateBlockReleaseGuard(
    LxrBlockReleaseGuard* guard) const
{
    return m_completion_registration.CreateReleaseGuard(guard);
}
#endif

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_WORK_PACKET_H
#define LXR_WORK_PACKET_H

#ifdef byte
#pragma push_macro("byte")
#define LXR_WORK_PACKET_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_WORK_PACKET_RESTORE_VALID
#undef __valid
#endif

#include <atomic>
#include <cstddef>
#include <cstdint>

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
#include "lxr_completion_counter.h"
#endif

enum class LxrWorkBucketStage : uint8_t
{
    Unconstrained,
    Concurrent,
    ConcurrentResumable,
    FinishConcurrentWork,
    Initial,
    Prepare,
    Closure,
    Release,
    STWRCDecsAndSweep,
    Final,
    Count,
};

constexpr LxrWorkBucketStage LxrRCProcessIncs = LxrWorkBucketStage::Initial;
constexpr LxrWorkBucketStage LxrRCProcessDecs = LxrWorkBucketStage::STWRCDecsAndSweep;
constexpr LxrWorkBucketStage LxrFirstStwStage = LxrWorkBucketStage::FinishConcurrentWork;
constexpr size_t LxrLocalPacketCacheCapacity = 16;
constexpr size_t LxrDefaultRcPacketReferenceCapacity = 1024;
constexpr size_t LxrEdgePacketReferenceCapacity = 4096;

static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Unconstrained) == 0);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Concurrent) == 1);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::ConcurrentResumable) == 2);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::FinishConcurrentWork) == 3);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Initial) == 4);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Prepare) == 5);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Closure) == 6);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Release) == 7);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::STWRCDecsAndSweep) == 8);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Final) == 9);
static_assert(static_cast<uint8_t>(LxrWorkBucketStage::Count) == 10);
static_assert(LxrRCProcessIncs == LxrWorkBucketStage::Initial);
static_assert(LxrRCProcessDecs == LxrWorkBucketStage::STWRCDecsAndSweep);

const char* LxrGetWorkBucketStageName(LxrWorkBucketStage stage);
bool LxrIsConcurrentStage(LxrWorkBucketStage stage);
bool LxrIsStwStage(LxrWorkBucketStage stage);
bool LxrIsSupportedReferencePacketCapacity(size_t capacity);
bool LxrTryGetReferencePacketCount(size_t referenceCount, size_t capacity, size_t* packetCount);

enum class LxrWorkPacketState : uint8_t
{
    CallerOwned,
    Queued,
    Executing,
    Completed,
};

class LxrWorkPacket final
{
public:
    using ExecuteCallback = void (*)(
        LxrWorkPacket* packet,
        size_t workerIndex,
        void* context);

    LxrWorkPacket(
        uint64_t id,
        LxrWorkBucketStage stage,
        const uintptr_t* references,
        size_t referenceCount,
        ExecuteCallback callback,
        void* context);
    LxrWorkPacket(const LxrWorkPacket&) = delete;
    LxrWorkPacket& operator=(const LxrWorkPacket&) = delete;

    uint64_t GetId() const;
    LxrWorkBucketStage GetStage() const;
    uint64_t GetGeneration() const;
    const uintptr_t* GetReferences() const;
    size_t GetReferenceCount() const;
    LxrWorkPacketState GetState() const;
    // P26 (F2). The context pointer that the owner gave to the packet.
    void* GetContext() const { return m_context; }
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrCompletionResult CreateBlockReleaseGuard(
        LxrBlockReleaseGuard* guard) const;
#endif

private:
    friend class LxrWorkScheduler;

    bool TryTransition(LxrWorkPacketState expected, LxrWorkPacketState desired);
    void SetGeneration(uint64_t generation);
    void SetPreferredWorker(size_t workerIndex);
    size_t GetPreferredWorker() const;
    void SetDeferred(bool deferred);
    bool IsDeferred() const;
    void Execute(size_t workerIndex);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    bool AttachCompletionRegistration(
        LxrLazyCompletionRegistration&& registration);
    LxrCompletionResult CompleteRegistration();
    LxrLazyCompletionRegistration DetachCompletionRegistration();
    const LxrLazyCompletionRegistration& GetCompletionRegistration() const;
    bool HasCompletionRegistration() const;
#endif

    const uint64_t m_id;
    const LxrWorkBucketStage m_stage;
    const uintptr_t* const m_references;
    const size_t m_reference_count;
    const ExecuteCallback m_callback;
    void* const m_context;
    std::atomic<uint64_t> m_generation;
    std::atomic<LxrWorkPacketState> m_state;
    size_t m_preferred_worker;
    bool m_deferred;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrLazyCompletionRegistration m_completion_registration;
#endif
};

#ifdef LXR_WORK_PACKET_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_WORK_PACKET_RESTORE_BYTE
#endif
#ifdef LXR_WORK_PACKET_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_WORK_PACKET_RESTORE_VALID
#endif

#endif // LXR_WORK_PACKET_H

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_WORK_SCHEDULER_H
#define LXR_WORK_SCHEDULER_H

#include "lxr_work_packet.h"

#ifdef byte
#pragma push_macro("byte")
#define LXR_WORK_SCHEDULER_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_WORK_SCHEDULER_RESTORE_VALID
#undef __valid
#endif

#include <cstddef>
#include <cstdint>

enum class LxrWorkSchedulerResult : uint8_t
{
    Success,
    InvalidArgument,
    NotInitialized,
    AlreadyInitialized,
    InvalidStage,
    InvalidState,
    StaleGeneration,
    PacketAlreadyOwned,
    StageDisabled,
    Busy,
    Timeout,
    Shutdown,
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    CompletionDisabled,
    CompletionOverflow,
    CompletionUnderflow,
    CompletionFailed,
#endif
};

enum class LxrDeferredWorkPriority : uint8_t
{
    Normal,
    Prioritized,
};

constexpr size_t LxrNoDeferredQueue = static_cast<size_t>(-1);

struct LxrWorkSchedulerMetrics
{
    uint64_t submitted;
    uint64_t executed;
    uint64_t stolen;
    uint64_t spilled;
    uint64_t rejected;
    uint64_t ownershipViolations;
    uint64_t parks;
    uint64_t wakes;
    uint64_t deferredSubmitted;
    uint64_t deferredPublished;
    uint64_t deferredExecuted;
    uint64_t deferredFlips;
    uint64_t deferredEmptyFlips;
    // P25 (F4). Number of times the scheduler woke sleeping threads (one per notify call).
    uint64_t signals;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    uint64_t completionRegistrations;
    uint64_t completionCompletions;
    uint64_t decrementZeroTransitions;
    uint64_t overallZeroTransitions;
#endif
};

struct LxrDeferredWorkState
{
    bool enabled;
    bool resumeRecorded;
    size_t activeIndex;
    size_t producerIndex;
    size_t sealedIndex;
    uint64_t producerGeneration;
    uint64_t sealedGeneration;
    uint64_t publishedGeneration;
    uint64_t pendingNormal;
    uint64_t pendingPrioritized;
    uint64_t sealedNormal;
    uint64_t sealedPrioritized;
};

class LxrWorkScheduler final
{
public:
    static constexpr size_t NoPreferredWorker = static_cast<size_t>(-1);

    LxrWorkScheduler();
    ~LxrWorkScheduler();
    LxrWorkScheduler(const LxrWorkScheduler&) = delete;
    LxrWorkScheduler& operator=(const LxrWorkScheduler&) = delete;

    LxrWorkSchedulerResult Initialize(
        size_t workerCount,
        bool enableDeferredConcurrentWork = false
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        ,
        bool enableLazyCompletionCounter = false,
        LxrDecrementsDrainedCallback decrementsDrained = nullptr,
        LxrOverallDrainedCallback overallDrained = nullptr,
        void* completionContext = nullptr,
        uint64_t maximumCompletionOutstanding = UINT64_MAX
#endif
        );
    LxrWorkSchedulerResult Shutdown();
    // P25 (F4). The most workers that may run the packets of the concurrent trace stages while the program runs (1 to the
    // number of workers). The other workers still run all other packets. The default is every worker.
    LxrWorkSchedulerResult ConfigureConcurrentTraceWorkers(size_t limit);
    // P26 (F2). Shutdown calls the callback for every packet that it cancels, after the packet is caller owned again and
    // outside the lock of the scheduler. The owner uses it to free packets that nobody else knows.
    using CancelledPacketCallback = void (*)(LxrWorkPacket* packet, void* context);
    LxrWorkSchedulerResult SetCancelledPacketCallback(CancelledPacketCallback callback, void* context);
    // P26 (F2). Runs the queued packets of one stage on the calling thread. The stage must not be runnable (it is closed
    // for a pause) and none of its packets may be running. A pause uses it to finish the trace backlog without a list
    // of the packets. executionIndex is the worker index that the packets see. ran gets the number of packets.
    LxrWorkSchedulerResult RunQueuedInline(LxrWorkBucketStage stage, size_t executionIndex, uint64_t* ran);

    LxrWorkSchedulerResult Submit(
        LxrWorkPacket* packet,
        size_t preferredWorker = NoPreferredWorker);
    LxrWorkSchedulerResult SubmitDeferredConcurrent(
        LxrWorkPacket* packet,
        LxrDeferredWorkPriority priority,
        size_t preferredWorker = NoPreferredWorker);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrWorkSchedulerResult SubmitTracked(
        LxrWorkPacket* packet,
        LxrCompletionWorkKind kind,
        size_t preferredWorker = NoPreferredWorker);
    LxrWorkSchedulerResult SubmitDeferredConcurrentTracked(
        LxrWorkPacket* packet,
        LxrCompletionWorkKind kind,
        LxrDeferredWorkPriority priority,
        size_t preferredWorker = NoPreferredWorker);
    LxrWorkSchedulerResult SubmitTrackedChild(
        const LxrWorkPacket* parent,
        LxrWorkPacket* child,
        LxrCompletionWorkKind kind,
        size_t preferredWorker = NoPreferredWorker);
    LxrWorkSchedulerResult SubmitMatureSweep(
        LxrMatureSweepContinuation* continuation,
        LxrWorkPacket* packet,
        size_t preferredWorker = NoPreferredWorker);
#endif
    LxrWorkSchedulerResult RecycleCompletedPacket(LxrWorkPacket* packet);
    LxrWorkSchedulerResult ConfigureStwStages(uint16_t enabledStageMask);
    LxrWorkSchedulerResult EnableConcurrentStage(LxrWorkBucketStage stage);
    LxrWorkSchedulerResult InterruptConcurrentWork(uint32_t timeoutMilliseconds);
    LxrWorkSchedulerResult CancelConcurrentInterruption();
    LxrWorkSchedulerResult BeginStwCycle();
    LxrWorkSchedulerResult ResumeConcurrentWork();
    LxrWorkSchedulerResult RecordMutatorResume(uint64_t expectedGeneration);
    LxrWorkSchedulerResult PublishDeferredConcurrentWork(uint64_t expectedGeneration);
    LxrWorkSchedulerResult ResumeInterruptedConcurrentWork();
    LxrWorkSchedulerResult ResetCompletedCycle();

    LxrWorkSchedulerResult WaitForPacket(
        const LxrWorkPacket* packet,
        uint32_t timeoutMilliseconds);
    LxrWorkSchedulerResult WaitForCycleCompletion(uint32_t timeoutMilliseconds);
    LxrWorkSchedulerResult WaitForIdle(uint32_t timeoutMilliseconds);
    LxrWorkSchedulerResult WaitForBucketIdle(LxrWorkBucketStage stage, uint32_t timeoutMilliseconds);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrWorkSchedulerResult WaitForCompletion(
        uint64_t generation,
        uint32_t timeoutMilliseconds);
#endif

    bool IsStageOpen(LxrWorkBucketStage stage) const;
    bool IsStageEnabled(LxrWorkBucketStage stage) const;
    bool IsStageCompleted(LxrWorkBucketStage stage) const;
    bool IsCycleCompleted() const;
    // P26 (F4b). True from the moment that a pause asks the scheduler to interrupt the concurrent work until the work resumes.
    // A long packet polls it, so that the pause does not wait for the end of the packet. No mutex.
    bool IsInterruptionRequested() const;
    uint64_t GetStageOutstanding(LxrWorkBucketStage stage) const;
    uint64_t GetGeneration() const;
    size_t GetWorkerCount() const;
    size_t CopyOpenTrace(LxrWorkBucketStage* stages, size_t capacity) const;
    size_t CopyCompletionTrace(LxrWorkBucketStage* stages, size_t capacity) const;
    LxrWorkSchedulerMetrics GetMetrics() const;
    LxrDeferredWorkState GetDeferredWorkState() const;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrWorkSchedulerResult GetCompletionSnapshot(
        uint64_t generation,
        LxrCompletionSnapshot* snapshot) const;
#endif

private:
    class Impl;
    Impl* m_impl;
};

#ifdef LXR_WORK_SCHEDULER_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_WORK_SCHEDULER_RESTORE_BYTE
#endif
#ifdef LXR_WORK_SCHEDULER_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_WORK_SCHEDULER_RESTORE_VALID
#endif

#endif // LXR_WORK_SCHEDULER_H

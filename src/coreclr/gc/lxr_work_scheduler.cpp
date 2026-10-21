// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "lxr_work_scheduler.h"

#ifdef byte
#pragma push_macro("byte")
#define LXR_WORK_SCHEDULER_CPP_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_WORK_SCHEDULER_CPP_RESTORE_VALID
#undef __valid
#endif

#include <array>
#include <cassert>
#include <chrono>
#include <condition_variable>
#include <deque>
#include <mutex>
#include <new>
#include <thread>
#include <utility>
#include <vector>

#ifdef LXR_WORK_SCHEDULER_CPP_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_WORK_SCHEDULER_CPP_RESTORE_BYTE
#endif
#ifdef LXR_WORK_SCHEDULER_CPP_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_WORK_SCHEDULER_CPP_RESTORE_VALID
#endif

namespace
{
    enum class BucketState : uint8_t
    {
        Closed,
        Open,
        Completed,
    };

    constexpr size_t StageCount = static_cast<size_t>(LxrWorkBucketStage::Count);
    constexpr size_t FirstStwIndex = static_cast<size_t>(LxrFirstStwStage);

    size_t StageIndex(LxrWorkBucketStage stage)
    {
        return static_cast<size_t>(stage);
    }

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrWorkSchedulerResult MapCompletionResult(LxrCompletionResult result)
    {
        switch (result)
        {
            case LxrCompletionResult::Success:
            case LxrCompletionResult::AlreadyCompleted:
                return LxrWorkSchedulerResult::Success;
            case LxrCompletionResult::StaleGeneration:
                return LxrWorkSchedulerResult::StaleGeneration;
            case LxrCompletionResult::Overflow:
                return LxrWorkSchedulerResult::CompletionOverflow;
            case LxrCompletionResult::Underflow:
                return LxrWorkSchedulerResult::CompletionUnderflow;
            case LxrCompletionResult::Cancelled:
                return LxrWorkSchedulerResult::Shutdown;
            case LxrCompletionResult::CallbackFailed:
                return LxrWorkSchedulerResult::CompletionFailed;
            case LxrCompletionResult::InvalidArgument:
                return LxrWorkSchedulerResult::InvalidArgument;
            default:
                return LxrWorkSchedulerResult::InvalidState;
        }
    }
#endif
}

class LxrWorkScheduler::Impl final
{
public:
    struct Bucket final
    {
        BucketState state = BucketState::Closed;
        bool enabled = true;
        bool accepting = true;
        uint64_t outstanding = 0;
        uint64_t executing = 0;
        std::deque<LxrWorkPacket*> queue;
    };

    struct Worker final
    {
        std::deque<LxrWorkPacket*> local;
        std::thread thread;
        bool parked = false;
        uint64_t executed = 0;
        uint64_t stolen = 0;
    };

    struct DeferredQueueSet final
    {
        std::deque<LxrWorkPacket*> normal;
        std::deque<LxrWorkPacket*> prioritized;
        uint64_t generation = 0;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        LxrLazyCompletionGenerationState completion;
        LxrLazyCompletionRegistration completionSeed;
#endif

        bool IsEmpty() const
        {
            return normal.empty() && prioritized.empty();
        }

        uint64_t Count() const
        {
            return static_cast<uint64_t>(normal.size() + prioritized.size());
        }
    };

    explicit Impl(
        size_t workerCount,
        bool enableDeferredConcurrentWork
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        ,
        bool enableLazyCompletionCounter,
        LxrDecrementsDrainedCallback decrementsDrained,
        LxrOverallDrainedCallback overallDrained,
        void* completionContext,
        uint64_t maximumCompletionOutstanding
#endif
        )
        : workers(workerCount),
          shuttingDown(false),
          cycleActive(false),
          cycleCompleted(false),
          interruptionRequested(false),
          concurrentInterrupted(false),
          generation(0),
          currentStwStage(LxrWorkBucketStage::Count),
          deferredEnabled(enableDeferredConcurrentWork),
          activeDeferredIndex(enableDeferredConcurrentWork ? 0 : LxrNoDeferredQueue),
          producerDeferredIndex(enableDeferredConcurrentWork ? 1 : LxrNoDeferredQueue),
          sealedDeferredIndex(LxrNoDeferredQueue),
          resumeRecordedGeneration(0),
          sealedGeneration(0),
          publishedGeneration(0),
          submitted(0),
          executed(0),
          stolen(0),
          spilled(0),
          rejected(0),
          ownershipViolations(0),
          parks(0),
          wakes(0),
          deferredSubmitted(0),
          deferredPublished(0),
          deferredExecuted(0),
          deferredFlips(0),
          deferredEmptyFlips(0)
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
          ,
          completionEnabled(enableLazyCompletionCounter),
          decrementsDrainedCallback(decrementsDrained),
          overallDrainedCallback(overallDrained),
          completionCallbackContext(completionContext),
          maximumCompletionOutstanding(maximumCompletionOutstanding),
          completionFailure(false)
#endif
    {
        buckets[StageIndex(LxrWorkBucketStage::Unconstrained)].state = BucketState::Open;
        buckets[StageIndex(LxrWorkBucketStage::Concurrent)].enabled = false;
        buckets[StageIndex(LxrWorkBucketStage::ConcurrentResumable)].enabled = false;
        if (deferredEnabled)
        {
            deferredQueues[producerDeferredIndex].generation = 1;
        }
        concurrentWasEnabled[0] = false;
        concurrentWasEnabled[1] = false;
        concurrentWasOpen[0] = false;
        concurrentWasOpen[1] = false;
    }

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrCompletionResult InitializeCompletion()
    {
        if (!completionEnabled)
        {
            return LxrCompletionResult::Success;
        }
        if (!deferredEnabled ||
            decrementsDrainedCallback == nullptr ||
            overallDrainedCallback == nullptr ||
            producerDeferredIndex >= deferredQueues.size())
        {
            return LxrCompletionResult::InvalidArgument;
        }

        DeferredQueueSet& producer = deferredQueues[producerDeferredIndex];
        return producer.completion.Initialize(
            producer.generation,
            decrementsDrainedCallback,
            overallDrainedCallback,
            completionCallbackContext,
            &producer.completionSeed,
            maximumCompletionOutstanding);
    }
#endif

    ~Impl()
    {
        Shutdown();
    }

    void Start()
    {
        for (size_t index = 0; index < workers.size(); index++)
        {
            workers[index].thread = std::thread([this, index]() { WorkerLoop(index); });
        }
    }

    void Shutdown()
    {
        std::vector<LxrWorkPacket*> cancelledPackets;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        uint64_t shutdownOwnershipViolations = 0;
        bool shutdownCompletionFailure = false;
#endif
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        std::array<LxrLazyCompletionRegistration, 2> cancelledSeeds;
#endif
        {
            std::lock_guard<std::mutex> guard(lock);
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
            if (completionEnabled)
            {
                for (DeferredQueueSet& queueSet : deferredQueues)
                {
                    LxrCompletionSnapshot snapshot =
                        queueSet.completion.GetSnapshot();
                    if (snapshot.generation != 0 &&
                        snapshot.lifecycle !=
                            LxrCompletionLifecycle::Uninitialized &&
                        snapshot.lifecycle != LxrCompletionLifecycle::Drained &&
                        snapshot.lifecycle !=
                            LxrCompletionLifecycle::Cancelled)
                    {
                        queueSet.completion.Cancel(snapshot.generation);
                    }
                }
            }
#endif

            for (size_t index = 0; index < deferredQueues.size(); index++)
            {
                DeferredQueueSet& queueSet = deferredQueues[index];
                bool isPublished = index == activeDeferredIndex;
                CancelDeferredQueueLocked(
                    queueSet.prioritized,
                    isPublished,
                    &cancelledPackets);
                CancelDeferredQueueLocked(
                    queueSet.normal,
                    isPublished,
                    &cancelledPackets);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
                if (completionEnabled && queueSet.completionSeed.IsActive())
                {
                    cancelledSeeds[index] =
                        std::move(queueSet.completionSeed);
                }
#endif
            }
            for (Bucket& bucket : buckets)
            {
                while (!bucket.queue.empty())
                {
                    LxrWorkPacket* packet = bucket.queue.front();
                    bucket.queue.pop_front();
                    if (packet->TryTransition(
                        LxrWorkPacketState::Queued,
                        LxrWorkPacketState::CallerOwned))
                    {
                        packet->SetGeneration(0);
                        packet->SetPreferredWorker(LxrWorkScheduler::NoPreferredWorker);
                        packet->SetDeferred(false);
                        bucket.outstanding--;
                        cancelledPackets.push_back(packet);
                    }
                    else
                    {
                        ownershipViolations++;
                    }
                }
            }
            for (Worker& worker : workers)
            {
                while (!worker.local.empty())
                {
                    LxrWorkPacket* packet = worker.local.front();
                    worker.local.pop_front();
                    Bucket& bucket = buckets[StageIndex(packet->GetStage())];
                    if (packet->TryTransition(
                        LxrWorkPacketState::Queued,
                        LxrWorkPacketState::CallerOwned))
                    {
                        packet->SetGeneration(0);
                        packet->SetPreferredWorker(LxrWorkScheduler::NoPreferredWorker);
                        packet->SetDeferred(false);
                        bucket.outstanding--;
                        cancelledPackets.push_back(packet);
                    }
                    else
                    {
                        ownershipViolations++;
                    }
                }
            }
            condition.notify_all();
        }

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        for (LxrLazyCompletionRegistration& seed : cancelledSeeds)
        {
            if (seed.IsActive())
            {
                seed.Complete();
            }
        }
        for (LxrWorkPacket* packet : cancelledPackets)
        {
            LxrCompletionResult result = packet->CompleteRegistration();
            if (result != LxrCompletionResult::Success &&
                result != LxrCompletionResult::AlreadyCompleted &&
                result != LxrCompletionResult::Cancelled &&
                result != LxrCompletionResult::CallbackFailed)
            {
                shutdownCompletionFailure = true;
                shutdownOwnershipViolations++;
            }
        }
#endif

        for (Worker& worker : workers)
        {
            if (worker.thread.joinable())
            {
                worker.thread.join();
            }
        }

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        if (completionEnabled)
        {
            for (DeferredQueueSet& queueSet : deferredQueues)
            {
                LxrCompletionSnapshot snapshot =
                    queueSet.completion.GetSnapshot();
                if (snapshot.generation != 0 &&
                    snapshot.lifecycle !=
                        LxrCompletionLifecycle::Uninitialized &&
                    snapshot.lifecycle != LxrCompletionLifecycle::Drained)
                {
                    LxrCompletionResult result =
                        queueSet.completion.FinalizeCancellation(
                            snapshot.generation);
                    if (result != LxrCompletionResult::Success)
                    {
                        shutdownCompletionFailure = true;
                        shutdownOwnershipViolations++;
                    }
                }
            }
            completionFailure =
                completionFailure || shutdownCompletionFailure;
            ownershipViolations += shutdownOwnershipViolations;
        }
#endif
    }

    void CancelDeferredQueueLocked(
        std::deque<LxrWorkPacket*>& queue,
        bool decrementOutstanding,
        std::vector<LxrWorkPacket*>* cancelledPackets)
    {
        while (!queue.empty())
        {
            LxrWorkPacket* packet = queue.front();
            queue.pop_front();
            if (packet->TryTransition(
                LxrWorkPacketState::Queued,
                LxrWorkPacketState::CallerOwned))
            {
                packet->SetGeneration(0);
                packet->SetPreferredWorker(LxrWorkScheduler::NoPreferredWorker);
                packet->SetDeferred(false);
                if (decrementOutstanding)
                {
                    buckets[StageIndex(LxrWorkBucketStage::Unconstrained)].outstanding--;
                }
                cancelledPackets->push_back(packet);
            }
            else
            {
                ownershipViolations++;
            }
        }
    }

    bool IsEligibleLocked(LxrWorkBucketStage stage) const
    {
        const Bucket& bucket = buckets[StageIndex(stage)];
        if (!bucket.enabled || bucket.state != BucketState::Open)
        {
            return false;
        }

        if (!cycleActive)
        {
            return LxrIsConcurrentStage(stage);
        }

        return stage == LxrWorkBucketStage::Unconstrained || stage == currentStwStage;
    }

    LxrWorkPacket* TakeLocalLocked(size_t workerIndex, bool fromBack)
    {
        std::deque<LxrWorkPacket*>& local = workers[workerIndex].local;
        if (fromBack)
        {
            for (
                std::deque<LxrWorkPacket*>::reverse_iterator iterator = local.rbegin();
                iterator != local.rend();
                iterator++)
            {
                if (IsEligibleLocked((*iterator)->GetStage()))
                {
                    LxrWorkPacket* packet = *iterator;
                    local.erase(std::next(iterator).base());
                    return packet;
                }
            }
        }
        else
        {
            for (
                std::deque<LxrWorkPacket*>::iterator iterator = local.begin();
                iterator != local.end();
                iterator++)
            {
                if (IsEligibleLocked((*iterator)->GetStage()))
                {
                    LxrWorkPacket* packet = *iterator;
                    local.erase(iterator);
                    return packet;
                }
            }
        }

        return nullptr;
    }

    LxrWorkPacket* TakeBucketLocked(size_t workerIndex)
    {
        for (size_t stageIndex = 0; stageIndex < StageCount; stageIndex++)
        {
            LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(stageIndex);
            Bucket& bucket = buckets[stageIndex];
            if (!IsEligibleLocked(stage))
            {
                continue;
            }

            if (stage == LxrWorkBucketStage::Unconstrained)
            {
                LxrWorkPacket* deferred = TakeDeferredActiveLocked(workerIndex);
                if (deferred != nullptr)
                {
                    return deferred;
                }
            }
            if (bucket.queue.empty())
            {
                continue;
            }

            LxrWorkPacket* packet = bucket.queue.front();
            bucket.queue.pop_front();
            size_t refill = 0;
            while (!bucket.queue.empty() &&
                workers[workerIndex].local.size() < LxrLocalPacketCacheCapacity &&
                refill < (LxrLocalPacketCacheCapacity / 2))
            {
                workers[workerIndex].local.push_back(bucket.queue.front());
                bucket.queue.pop_front();
                refill++;
            }
            return packet;
        }

        return nullptr;
    }

    LxrWorkPacket* TakeDeferredActiveLocked(size_t workerIndex)
    {
        if (!deferredEnabled || activeDeferredIndex == LxrNoDeferredQueue)
        {
            return nullptr;
        }

        DeferredQueueSet& active = deferredQueues[activeDeferredIndex];
        std::deque<LxrWorkPacket*>* source = nullptr;
        if (!active.prioritized.empty())
        {
            source = &active.prioritized;
        }
        else if (!active.normal.empty())
        {
            source = &active.normal;
        }
        if (source == nullptr)
        {
            return nullptr;
        }

        LxrWorkPacket* packet = source->front();
        source->pop_front();
        size_t refill = 0;
        while (!source->empty() &&
            workers[workerIndex].local.size() < LxrLocalPacketCacheCapacity &&
            refill < (LxrLocalPacketCacheCapacity / 2))
        {
            workers[workerIndex].local.push_back(source->front());
            source->pop_front();
            refill++;
        }
        return packet;
    }

    LxrWorkPacket* StealLocked(size_t workerIndex)
    {
        if (workers.size() <= 1)
        {
            return nullptr;
        }

        for (size_t offset = 1; offset < workers.size(); offset++)
        {
            size_t victim = (workerIndex + offset) % workers.size();
            LxrWorkPacket* packet = TakeLocalLocked(victim, false);
            if (packet != nullptr)
            {
                return packet;
            }
        }

        return nullptr;
    }

    bool TryBeginExecutionLocked(LxrWorkPacket* packet, size_t workerIndex)
    {
        if (!packet->TryTransition(LxrWorkPacketState::Queued, LxrWorkPacketState::Executing))
        {
            ownershipViolations++;
            return false;
        }

        size_t preferredWorker = packet->GetPreferredWorker();
        if (preferredWorker != LxrWorkScheduler::NoPreferredWorker &&
            preferredWorker != workerIndex)
        {
            workers[workerIndex].stolen++;
            stolen++;
        }
        buckets[StageIndex(packet->GetStage())].executing++;
        return true;
    }

    LxrWorkPacket* PollLocked(size_t workerIndex)
    {
        LxrWorkPacket* packet = TakeLocalLocked(workerIndex, true);
        if (packet == nullptr)
        {
            packet = TakeBucketLocked(workerIndex);
        }
        if (packet == nullptr)
        {
            packet = StealLocked(workerIndex);
        }
        if (packet != nullptr && !TryBeginExecutionLocked(packet, workerIndex))
        {
            return nullptr;
        }
        return packet;
    }

    bool AllWorkersParkedLocked() const
    {
        for (const Worker& worker : workers)
        {
            if (!worker.parked)
            {
                return false;
            }
        }
        return true;
    }

    bool IsBucketDrainedLocked(LxrWorkBucketStage stage) const
    {
        const Bucket& bucket = buckets[StageIndex(stage)];
        return bucket.outstanding == 0 &&
            bucket.executing == 0 &&
            bucket.queue.empty();
    }

    void RecordOpenLocked(LxrWorkBucketStage stage)
    {
        openTrace.push_back(stage);
    }

    bool SealDeferredCycleLocked()
    {
        if (!deferredEnabled)
        {
            return true;
        }

        assert(sealedDeferredIndex == LxrNoDeferredQueue);
        assert(producerDeferredIndex < deferredQueues.size());
        assert(deferredQueues[producerDeferredIndex].generation == generation);
        if (activeDeferredIndex != LxrNoDeferredQueue)
        {
            assert(activeDeferredIndex < deferredQueues.size());
            assert(deferredQueues[activeDeferredIndex].IsEmpty());
        }

        size_t nextProducerIndex = producerDeferredIndex == 0 ? 1 : 0;
        assert(nextProducerIndex == activeDeferredIndex);
        assert(deferredQueues[nextProducerIndex].IsEmpty());

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        if (completionEnabled)
        {
            DeferredQueueSet& sealed = deferredQueues[producerDeferredIndex];
            LxrCompletionResult sealResult =
                sealed.completion.Seal(generation);
            if (sealResult != LxrCompletionResult::Success)
            {
                completionFailure = true;
                return false;
            }

            DeferredQueueSet& nextProducer = deferredQueues[nextProducerIndex];
            LxrCompletionResult initializeResult =
                nextProducer.completion.Initialize(
                    generation + 1,
                    decrementsDrainedCallback,
                    overallDrainedCallback,
                    completionCallbackContext,
                    &nextProducer.completionSeed,
                    maximumCompletionOutstanding);
            if (initializeResult != LxrCompletionResult::Success)
            {
                completionFailure = true;
                return false;
            }
        }
#endif

        sealedDeferredIndex = producerDeferredIndex;
        sealedGeneration = generation;
        producerDeferredIndex = nextProducerIndex;
        deferredQueues[producerDeferredIndex].generation = generation + 1;
        activeDeferredIndex = LxrNoDeferredQueue;
        return true;
    }

    void RecordCompletionLocked(LxrWorkBucketStage stage)
    {
        completionTrace.push_back(stage);
    }

    bool AdvanceBucketsLocked()
    {
        bool openedWork = false;
        while (cycleActive && AllWorkersParkedLocked())
        {
            Bucket& current = buckets[StageIndex(currentStwStage)];
            if (!IsBucketDrainedLocked(currentStwStage))
            {
                break;
            }

            current.state = BucketState::Completed;
            RecordCompletionLocked(currentStwStage);
            if (currentStwStage == LxrWorkBucketStage::Final)
            {
                if (!SealDeferredCycleLocked())
                {
                    cycleActive = false;
                    cycleCompleted = true;
                    condition.notify_all();
                    break;
                }
                cycleActive = false;
                cycleCompleted = true;
                condition.notify_all();
                break;
            }

            size_t nextIndex = StageIndex(currentStwStage) + 1;
            currentStwStage = static_cast<LxrWorkBucketStage>(nextIndex);
            Bucket& next = buckets[nextIndex];
            next.state = BucketState::Open;
            RecordOpenLocked(currentStwStage);
            if (next.outstanding != 0)
            {
                openedWork = true;
                break;
            }
        }
        return openedWork;
    }

    void WorkerLoop(size_t workerIndex)
    {
        std::unique_lock<std::mutex> guard(lock);
        while (!shuttingDown)
        {
            Worker& worker = workers[workerIndex];
            if (worker.parked)
            {
                worker.parked = false;
                wakes++;
            }

            LxrWorkPacket* packet = PollLocked(workerIndex);
            if (packet != nullptr)
            {
                guard.unlock();
                packet->Execute(workerIndex);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
                LxrCompletionResult completionResult =
                    packet->CompleteRegistration();
#endif
                guard.lock();

                Bucket& bucket = buckets[StageIndex(packet->GetStage())];
                bucket.executing--;
                if (packet->TryTransition(
                    LxrWorkPacketState::Executing,
                    LxrWorkPacketState::Completed))
                {
                    bucket.outstanding--;
                    worker.executed++;
                    executed++;
                    if (packet->IsDeferred())
                    {
                        deferredExecuted++;
                    }
                }
                else
                {
                    ownershipViolations++;
                }
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
                if (completionResult != LxrCompletionResult::Success &&
                    completionResult != LxrCompletionResult::AlreadyCompleted &&
                    completionResult != LxrCompletionResult::Cancelled &&
                    completionResult != LxrCompletionResult::CallbackFailed)
                {
                    ownershipViolations++;
                    completionFailure = true;
                }
                if (completionResult == LxrCompletionResult::CallbackFailed)
                {
                    completionFailure = true;
                }
#endif
                condition.notify_all();
                continue;
            }

            worker.parked = true;
            parks++;
            bool openedWork = AdvanceBucketsLocked();
            if (openedWork)
            {
                condition.notify_all();
                continue;
            }

            condition.wait(guard);
        }
    }

    std::array<Bucket, StageCount> buckets;
    std::vector<Worker> workers;
    mutable std::mutex lock;
    std::condition_variable condition;
    bool shuttingDown;
    bool cycleActive;
    bool cycleCompleted;
    bool interruptionRequested;
    bool concurrentInterrupted;
    bool concurrentWasEnabled[2];
    bool concurrentWasOpen[2];
    uint64_t generation;
    LxrWorkBucketStage currentStwStage;
    std::vector<LxrWorkBucketStage> openTrace;
    bool deferredEnabled;
    std::array<DeferredQueueSet, 2> deferredQueues;
    size_t activeDeferredIndex;
    size_t producerDeferredIndex;
    size_t sealedDeferredIndex;
    uint64_t resumeRecordedGeneration;
    uint64_t sealedGeneration;
    uint64_t publishedGeneration;
    std::vector<LxrWorkBucketStage> completionTrace;
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
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    bool completionEnabled;
    LxrDecrementsDrainedCallback decrementsDrainedCallback;
    LxrOverallDrainedCallback overallDrainedCallback;
    void* completionCallbackContext;
    uint64_t maximumCompletionOutstanding;
    bool completionFailure;
#endif
};

LxrWorkScheduler::LxrWorkScheduler()
    : m_impl(nullptr)
{
}

LxrWorkScheduler::~LxrWorkScheduler()
{
    Shutdown();
}

LxrWorkSchedulerResult LxrWorkScheduler::Initialize(
    size_t workerCount,
    bool enableDeferredConcurrentWork
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    ,
    bool enableLazyCompletionCounter,
    LxrDecrementsDrainedCallback decrementsDrained,
    LxrOverallDrainedCallback overallDrained,
    void* completionContext,
    uint64_t maximumCompletionOutstanding
#endif
    )
{
    if (workerCount == 0)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (m_impl != nullptr)
    {
        return LxrWorkSchedulerResult::AlreadyInitialized;
    }

    Impl* implementation = new (std::nothrow) Impl(
        workerCount,
        enableDeferredConcurrentWork
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        ,
        enableLazyCompletionCounter,
        decrementsDrained,
        overallDrained,
        completionContext,
        maximumCompletionOutstanding
#endif
        );
    if (implementation == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    m_impl = implementation;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrCompletionResult completionResult =
        implementation->InitializeCompletion();
    if (completionResult != LxrCompletionResult::Success)
    {
        delete implementation;
        m_impl = nullptr;
        return MapCompletionResult(completionResult);
    }
#endif
    implementation->Start();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::Shutdown()
{
    Impl* implementation = m_impl;
    if (implementation == nullptr)
    {
        return LxrWorkSchedulerResult::Success;
    }

    implementation->Shutdown();
    delete implementation;
    m_impl = nullptr;
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::Submit(
    LxrWorkPacket* packet,
    size_t preferredWorker)
{
    if (packet == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    size_t stageIndex = StageIndex(packet->GetStage());
    if (stageIndex >= StageCount)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }
    if ((packet->m_reference_count != 0 && packet->m_references == nullptr) ||
        packet->m_callback == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->shuttingDown)
    {
        return LxrWorkSchedulerResult::Shutdown;
    }

    Impl::Bucket& bucket = m_impl->buckets[stageIndex];
    if (bucket.state == BucketState::Completed)
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::InvalidState;
    }
    if (!bucket.accepting ||
        (LxrIsStwStage(packet->GetStage()) && !bucket.enabled))
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::StageDisabled;
    }

    uint64_t packetGeneration = m_impl->generation;
    if (LxrIsStwStage(packet->GetStage()) && !m_impl->cycleActive)
    {
        packetGeneration++;
    }
    if (packet->GetGeneration() != 0 && packet->GetGeneration() != packetGeneration)
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::StaleGeneration;
    }
    if (!packet->TryTransition(
        LxrWorkPacketState::CallerOwned,
        LxrWorkPacketState::Queued))
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::PacketAlreadyOwned;
    }

    packet->SetGeneration(packetGeneration);
    packet->SetDeferred(false);
    packet->SetPreferredWorker(
        preferredWorker < m_impl->workers.size() ?
            preferredWorker :
            NoPreferredWorker);
    bucket.outstanding++;
    m_impl->submitted++;

    bool canUseLocal =
        preferredWorker < m_impl->workers.size() &&
        bucket.state == BucketState::Open &&
        bucket.enabled &&
        m_impl->workers[preferredWorker].local.size() < LxrLocalPacketCacheCapacity;
    if (canUseLocal)
    {
        m_impl->workers[preferredWorker].local.push_back(packet);
    }
    else
    {
        if (preferredWorker < m_impl->workers.size() &&
            bucket.state == BucketState::Open &&
            bucket.enabled)
        {
            m_impl->spilled++;
        }
        bucket.queue.push_back(packet);
    }

    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::ConfigureStwStages(uint16_t enabledStageMask)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    constexpr uint16_t StwStageMask =
        static_cast<uint16_t>(
            ((UINT32_C(1) << static_cast<uint8_t>(LxrWorkBucketStage::Count)) - 1) &
            ~((UINT32_C(1) << static_cast<uint8_t>(LxrFirstStwStage)) - 1));
    if ((enabledStageMask & ~StwStageMask) != 0)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive ||
        m_impl->cycleCompleted ||
        m_impl->interruptionRequested)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    for (size_t stageIndex = FirstStwIndex; stageIndex < StageCount; stageIndex++)
    {
        Impl::Bucket& bucket = m_impl->buckets[stageIndex];
        if (bucket.state != BucketState::Closed ||
            bucket.outstanding != 0 ||
            bucket.executing != 0 ||
            !bucket.queue.empty())
        {
            return LxrWorkSchedulerResult::Busy;
        }
    }
    for (size_t stageIndex = FirstStwIndex; stageIndex < StageCount; stageIndex++)
    {
        Impl::Bucket& bucket = m_impl->buckets[stageIndex];
        bool enabled = (enabledStageMask & (UINT16_C(1) << stageIndex)) != 0;
        bucket.enabled = enabled;
        bucket.accepting = enabled;
    }
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::SubmitDeferredConcurrent(
    LxrWorkPacket* packet,
    LxrDeferredWorkPriority priority,
    size_t preferredWorker)
{
    if (packet == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if (packet->GetStage() != LxrWorkBucketStage::Unconstrained)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }
    if ((packet->m_reference_count != 0 && packet->m_references == nullptr) ||
        packet->m_callback == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (priority != LxrDeferredWorkPriority::Normal &&
        priority != LxrDeferredWorkPriority::Prioritized)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (!m_impl->deferredEnabled)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    if (m_impl->shuttingDown)
    {
        return LxrWorkSchedulerResult::Shutdown;
    }
    if (m_impl->producerDeferredIndex >= m_impl->deferredQueues.size())
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    Impl::DeferredQueueSet& producer =
        m_impl->deferredQueues[m_impl->producerDeferredIndex];
    uint64_t packetGeneration = producer.generation;
    if (packet->GetGeneration() != 0 &&
        packet->GetGeneration() != packetGeneration)
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::StaleGeneration;
    }
    if (!packet->TryTransition(
        LxrWorkPacketState::CallerOwned,
        LxrWorkPacketState::Queued))
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::PacketAlreadyOwned;
    }

    packet->SetGeneration(packetGeneration);
    packet->SetDeferred(true);
    packet->SetPreferredWorker(
        preferredWorker < m_impl->workers.size() ?
            preferredWorker :
            NoPreferredWorker);
    if (priority == LxrDeferredWorkPriority::Prioritized)
    {
        producer.prioritized.push_back(packet);
    }
    else
    {
        producer.normal.push_back(packet);
    }
    m_impl->submitted++;
    m_impl->deferredSubmitted++;
    return LxrWorkSchedulerResult::Success;
}

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
LxrWorkSchedulerResult LxrWorkScheduler::SubmitTracked(
    LxrWorkPacket* packet,
    LxrCompletionWorkKind kind,
    size_t preferredWorker)
{
    if (packet == nullptr || !LxrIsStwStage(packet->GetStage()))
    {
        return packet == nullptr
            ? LxrWorkSchedulerResult::InvalidArgument
            : LxrWorkSchedulerResult::InvalidStage;
    }
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if ((packet->m_reference_count != 0 && packet->m_references == nullptr) ||
        packet->m_callback == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (!m_impl->completionEnabled)
    {
        return LxrWorkSchedulerResult::CompletionDisabled;
    }
    if (m_impl->shuttingDown)
    {
        return LxrWorkSchedulerResult::Shutdown;
    }
    if (m_impl->producerDeferredIndex >= m_impl->deferredQueues.size())
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    size_t stageIndex = StageIndex(packet->GetStage());
    Impl::Bucket& bucket = m_impl->buckets[stageIndex];
    if (bucket.state == BucketState::Completed)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    uint64_t packetGeneration = m_impl->cycleActive
        ? m_impl->generation
        : m_impl->generation + 1;
    Impl::DeferredQueueSet& producer =
        m_impl->deferredQueues[m_impl->producerDeferredIndex];
    if (producer.generation != packetGeneration)
    {
        return LxrWorkSchedulerResult::StaleGeneration;
    }
    if (!packet->TryTransition(
            LxrWorkPacketState::CallerOwned,
            LxrWorkPacketState::Queued))
    {
        return LxrWorkSchedulerResult::PacketAlreadyOwned;
    }

    LxrLazyCompletionRegistration registration;
    LxrCompletionResult completionResult = producer.completion.RegisterRoot(
        packetGeneration,
        kind,
        &registration);
    if (completionResult != LxrCompletionResult::Success)
    {
        packet->TryTransition(
            LxrWorkPacketState::Queued,
            LxrWorkPacketState::CallerOwned);
        return MapCompletionResult(completionResult);
    }
    if (!packet->AttachCompletionRegistration(std::move(registration)))
    {
        packet->TryTransition(
            LxrWorkPacketState::Queued,
            LxrWorkPacketState::CallerOwned);
        return LxrWorkSchedulerResult::InvalidState;
    }

    packet->SetGeneration(packetGeneration);
    packet->SetDeferred(false);
    packet->SetPreferredWorker(
        preferredWorker < m_impl->workers.size()
            ? preferredWorker
            : NoPreferredWorker);
    bucket.outstanding++;
    m_impl->submitted++;
    bucket.queue.push_back(packet);
    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::SubmitDeferredConcurrentTracked(
    LxrWorkPacket* packet,
    LxrCompletionWorkKind kind,
    LxrDeferredWorkPriority priority,
    size_t preferredWorker)
{
    if (packet == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if (packet->GetStage() != LxrWorkBucketStage::Unconstrained)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }
    if ((packet->m_reference_count != 0 && packet->m_references == nullptr) ||
        packet->m_callback == nullptr ||
        (priority != LxrDeferredWorkPriority::Normal &&
            priority != LxrDeferredWorkPriority::Prioritized))
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (!m_impl->completionEnabled)
    {
        return LxrWorkSchedulerResult::CompletionDisabled;
    }
    if (m_impl->shuttingDown)
    {
        return LxrWorkSchedulerResult::Shutdown;
    }
    if (m_impl->producerDeferredIndex >= m_impl->deferredQueues.size())
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    Impl::DeferredQueueSet& producer =
        m_impl->deferredQueues[m_impl->producerDeferredIndex];
    uint64_t packetGeneration = producer.generation;
    if (packet->GetGeneration() != 0 &&
        packet->GetGeneration() != packetGeneration)
    {
        return LxrWorkSchedulerResult::StaleGeneration;
    }
    if (!packet->TryTransition(
            LxrWorkPacketState::CallerOwned,
            LxrWorkPacketState::Queued))
    {
        return LxrWorkSchedulerResult::PacketAlreadyOwned;
    }

    LxrLazyCompletionRegistration registration;
    LxrCompletionResult completionResult = producer.completion.RegisterRoot(
        packetGeneration,
        kind,
        &registration);
    if (completionResult != LxrCompletionResult::Success)
    {
        packet->TryTransition(
            LxrWorkPacketState::Queued,
            LxrWorkPacketState::CallerOwned);
        return MapCompletionResult(completionResult);
    }
    if (!packet->AttachCompletionRegistration(std::move(registration)))
    {
        packet->TryTransition(
            LxrWorkPacketState::Queued,
            LxrWorkPacketState::CallerOwned);
        return LxrWorkSchedulerResult::InvalidState;
    }

    packet->SetGeneration(packetGeneration);
    packet->SetDeferred(true);
    packet->SetPreferredWorker(
        preferredWorker < m_impl->workers.size()
            ? preferredWorker
            : NoPreferredWorker);
    if (priority == LxrDeferredWorkPriority::Prioritized)
    {
        producer.prioritized.push_back(packet);
    }
    else
    {
        producer.normal.push_back(packet);
    }
    m_impl->submitted++;
    m_impl->deferredSubmitted++;
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::SubmitTrackedChild(
    const LxrWorkPacket* parent,
    LxrWorkPacket* child,
    LxrCompletionWorkKind kind,
    size_t preferredWorker)
{
    if (parent == nullptr || child == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (!parent->HasCompletionRegistration())
    {
        return LxrWorkSchedulerResult::CompletionDisabled;
    }

    LxrLazyCompletionRegistration registration;
    LxrCompletionResult completionResult =
        parent->GetCompletionRegistration().RegisterChild(
            kind,
            &registration);
    if (completionResult != LxrCompletionResult::Success)
    {
        return MapCompletionResult(completionResult);
    }
    if (!child->AttachCompletionRegistration(std::move(registration)))
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    LxrWorkSchedulerResult result = Submit(child, preferredWorker);
    if (result != LxrWorkSchedulerResult::Success)
    {
        LxrLazyCompletionRegistration detached =
            child->DetachCompletionRegistration();
        detached.Complete();
    }
    return result;
}

LxrWorkSchedulerResult LxrWorkScheduler::SubmitMatureSweep(
    LxrMatureSweepContinuation* continuation,
    LxrWorkPacket* packet,
    size_t preferredWorker)
{
    if (continuation == nullptr || packet == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (packet->GetStage() != LxrWorkBucketStage::Unconstrained)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }

    LxrLazyCompletionRegistration registration;
    LxrCompletionResult completionResult =
        continuation->RegisterSweep(&registration);
    if (completionResult != LxrCompletionResult::Success)
    {
        return MapCompletionResult(completionResult);
    }
    if (!packet->AttachCompletionRegistration(std::move(registration)))
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    LxrWorkSchedulerResult result = Submit(packet, preferredWorker);
    if (result != LxrWorkSchedulerResult::Success)
    {
        LxrLazyCompletionRegistration detached =
            packet->DetachCompletionRegistration();
        detached.Complete();
    }
    return result;
}
#endif

LxrWorkSchedulerResult LxrWorkScheduler::RecycleCompletedPacket(LxrWorkPacket* packet)
{
    if (packet == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if (StageIndex(packet->GetStage()) >= StageCount)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    if (packet->HasCompletionRegistration())
    {
        return LxrWorkSchedulerResult::Busy;
    }
#endif
    if (!packet->TryTransition(
        LxrWorkPacketState::Completed,
        LxrWorkPacketState::CallerOwned))
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    packet->SetGeneration(0);
    packet->SetPreferredWorker(NoPreferredWorker);
    packet->SetDeferred(false);
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::EnableConcurrentStage(LxrWorkBucketStage stage)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if (stage != LxrWorkBucketStage::Concurrent &&
        stage != LxrWorkBucketStage::ConcurrentResumable)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive || m_impl->interruptionRequested)
    {
        return LxrWorkSchedulerResult::Busy;
    }
    Impl::Bucket& bucket = m_impl->buckets[StageIndex(stage)];
    bucket.enabled = true;
    bucket.accepting = true;
    bucket.state = BucketState::Open;
    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::InterruptConcurrentWork(uint32_t timeoutMilliseconds)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::unique_lock<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive || m_impl->cycleCompleted || m_impl->concurrentInterrupted)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    constexpr LxrWorkBucketStage ConcurrentStages[] = {
        LxrWorkBucketStage::Concurrent,
        LxrWorkBucketStage::ConcurrentResumable,
    };
    if (!m_impl->interruptionRequested)
    {
        for (size_t index = 0; index < std::size(ConcurrentStages); index++)
        {
            Impl::Bucket& bucket = m_impl->buckets[StageIndex(ConcurrentStages[index])];
            m_impl->concurrentWasEnabled[index] = bucket.enabled;
            m_impl->concurrentWasOpen[index] = bucket.state == BucketState::Open;
        }

        Impl::Bucket& resumable =
            m_impl->buckets[StageIndex(LxrWorkBucketStage::ConcurrentResumable)];
        resumable.accepting = false;
        resumable.enabled = false;
        resumable.state = BucketState::Closed;
        m_impl->interruptionRequested = true;
        m_impl->condition.notify_all();
    }

    bool ready = m_impl->condition.wait_for(
        guard,
        std::chrono::milliseconds(timeoutMilliseconds),
        [this]()
        {
            return m_impl->IsBucketDrainedLocked(LxrWorkBucketStage::Unconstrained) &&
                m_impl->IsBucketDrainedLocked(LxrWorkBucketStage::Concurrent) &&
                m_impl->buckets[
                    StageIndex(LxrWorkBucketStage::ConcurrentResumable)].executing == 0;
        });
    if (!ready)
    {
        return LxrWorkSchedulerResult::Timeout;
    }

    Impl::Bucket& concurrent =
        m_impl->buckets[StageIndex(LxrWorkBucketStage::Concurrent)];
    concurrent.accepting = false;
    concurrent.enabled = false;
    concurrent.state = BucketState::Closed;
    m_impl->concurrentInterrupted = true;
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::CancelConcurrentInterruption()
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive || !m_impl->interruptionRequested)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    constexpr LxrWorkBucketStage ConcurrentStages[] = {
        LxrWorkBucketStage::Concurrent,
        LxrWorkBucketStage::ConcurrentResumable,
    };
    for (size_t index = 0; index < std::size(ConcurrentStages); index++)
    {
        Impl::Bucket& bucket = m_impl->buckets[StageIndex(ConcurrentStages[index])];
        bucket.accepting = true;
        bucket.enabled = m_impl->concurrentWasEnabled[index];
        bucket.state = m_impl->concurrentWasOpen[index] ?
            BucketState::Open :
            BucketState::Closed;
    }
    m_impl->interruptionRequested = false;
    m_impl->concurrentInterrupted = false;
    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::BeginStwCycle()
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive || m_impl->cycleCompleted)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    if (!m_impl->IsBucketDrainedLocked(LxrWorkBucketStage::Unconstrained) ||
        !m_impl->IsBucketDrainedLocked(LxrWorkBucketStage::Concurrent) ||
        m_impl->buckets[StageIndex(LxrWorkBucketStage::ConcurrentResumable)].executing != 0)
    {
        return LxrWorkSchedulerResult::Busy;
    }
    if (m_impl->deferredEnabled)
    {
        if (m_impl->sealedDeferredIndex != LxrNoDeferredQueue ||
            m_impl->producerDeferredIndex >= m_impl->deferredQueues.size() ||
            m_impl->deferredQueues[m_impl->producerDeferredIndex].generation !=
                m_impl->generation + 1)
        {
            return LxrWorkSchedulerResult::InvalidState;
        }
        if (m_impl->activeDeferredIndex != LxrNoDeferredQueue &&
            !m_impl->deferredQueues[m_impl->activeDeferredIndex].IsEmpty())
        {
            return LxrWorkSchedulerResult::Busy;
        }
    }

    m_impl->buckets[StageIndex(LxrWorkBucketStage::Concurrent)].enabled = false;
    m_impl->buckets[StageIndex(LxrWorkBucketStage::Concurrent)].accepting = false;
    m_impl->buckets[StageIndex(LxrWorkBucketStage::Concurrent)].state = BucketState::Closed;
    m_impl->buckets[StageIndex(LxrWorkBucketStage::ConcurrentResumable)].enabled = false;
    m_impl->buckets[StageIndex(LxrWorkBucketStage::ConcurrentResumable)].accepting = false;
    m_impl->buckets[StageIndex(LxrWorkBucketStage::ConcurrentResumable)].state = BucketState::Closed;

    m_impl->generation++;
    m_impl->cycleActive = true;
    m_impl->currentStwStage = LxrFirstStwStage;
    Impl::Bucket& first = m_impl->buckets[FirstStwIndex];
    first.state = BucketState::Open;
    m_impl->RecordOpenLocked(LxrFirstStwStage);
    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::ResumeConcurrentWork()
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive || m_impl->interruptionRequested)
    {
        return LxrWorkSchedulerResult::Busy;
    }
    if (m_impl->deferredEnabled)
    {
        if (!m_impl->cycleCompleted ||
            m_impl->sealedDeferredIndex == LxrNoDeferredQueue ||
            m_impl->sealedGeneration != m_impl->generation ||
            m_impl->resumeRecordedGeneration == m_impl->generation)
        {
            return LxrWorkSchedulerResult::InvalidState;
        }
        m_impl->resumeRecordedGeneration = m_impl->generation;
    }
    for (LxrWorkBucketStage stage :
        {LxrWorkBucketStage::Concurrent, LxrWorkBucketStage::ConcurrentResumable})
    {
        Impl::Bucket& bucket = m_impl->buckets[StageIndex(stage)];
        bucket.enabled = true;
        bucket.accepting = true;
        bucket.state = BucketState::Open;
    }
    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::PublishDeferredConcurrentWork(
    uint64_t expectedGeneration)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::unique_lock<std::mutex> guard(m_impl->lock);
    if (!m_impl->deferredEnabled)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    if (m_impl->shuttingDown)
    {
        return LxrWorkSchedulerResult::Shutdown;
    }
    if (m_impl->cycleActive)
    {
        return LxrWorkSchedulerResult::Busy;
    }
    if (!m_impl->cycleCompleted ||
        expectedGeneration != m_impl->generation ||
        m_impl->resumeRecordedGeneration != expectedGeneration ||
        m_impl->sealedGeneration != expectedGeneration ||
        m_impl->sealedDeferredIndex == LxrNoDeferredQueue ||
        m_impl->activeDeferredIndex != LxrNoDeferredQueue)
    {
        m_impl->rejected++;
        return LxrWorkSchedulerResult::InvalidState;
    }

    size_t publishedIndex = m_impl->sealedDeferredIndex;
    Impl::DeferredQueueSet& published = m_impl->deferredQueues[publishedIndex];
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    if (m_impl->completionEnabled)
    {
        LxrCompletionResult publishResult =
            published.completion.MarkPublished(expectedGeneration);
        if (publishResult != LxrCompletionResult::Success)
        {
            m_impl->completionFailure = true;
            return MapCompletionResult(publishResult);
        }
    }
#endif
    uint64_t publishedCount = published.Count();
    m_impl->activeDeferredIndex = publishedIndex;
    m_impl->sealedDeferredIndex = LxrNoDeferredQueue;
    m_impl->sealedGeneration = 0;
    m_impl->publishedGeneration = expectedGeneration;
    m_impl->deferredFlips++;
    m_impl->deferredPublished += publishedCount;
    m_impl->buckets[StageIndex(LxrWorkBucketStage::Unconstrained)].outstanding +=
        publishedCount;
    if (publishedCount == 0)
    {
        m_impl->deferredEmptyFlips++;
    }
    else
    {
        m_impl->condition.notify_all();
    }
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrLazyCompletionRegistration completionSeed;
    if (m_impl->completionEnabled)
    {
        completionSeed = std::move(published.completionSeed);
    }
    guard.unlock();
    LxrCompletionResult completionResult = completionSeed.IsActive()
        ? completionSeed.Complete()
        : LxrCompletionResult::Success;
    guard.lock();
    if (completionResult == LxrCompletionResult::CallbackFailed)
    {
        m_impl->completionFailure = true;
    }
    else if (completionResult != LxrCompletionResult::Success &&
        completionResult != LxrCompletionResult::AlreadyCompleted)
    {
        m_impl->completionFailure = true;
        m_impl->ownershipViolations++;
    }
    m_impl->condition.notify_all();
    if (completionResult == LxrCompletionResult::CallbackFailed)
    {
        return LxrWorkSchedulerResult::CompletionFailed;
    }
#endif
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::RecordMutatorResume(uint64_t expectedGeneration)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->shuttingDown)
    {
        return LxrWorkSchedulerResult::Shutdown;
    }
    if (!m_impl->deferredEnabled ||
        m_impl->cycleActive ||
        !m_impl->cycleCompleted ||
        m_impl->interruptionRequested ||
        expectedGeneration != m_impl->generation ||
        m_impl->sealedGeneration != expectedGeneration ||
        m_impl->sealedDeferredIndex == LxrNoDeferredQueue ||
        m_impl->resumeRecordedGeneration == expectedGeneration)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    m_impl->resumeRecordedGeneration = expectedGeneration;
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::ResumeInterruptedConcurrentWork()
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive ||
        (m_impl->cycleCompleted && !m_impl->deferredEnabled) ||
        !m_impl->interruptionRequested)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }

    for (LxrWorkBucketStage stage :
        {LxrWorkBucketStage::Concurrent, LxrWorkBucketStage::ConcurrentResumable})
    {
        Impl::Bucket& bucket = m_impl->buckets[StageIndex(stage)];
        bucket.accepting = true;
        bool hasWork = bucket.outstanding != 0 || !bucket.queue.empty();
        bucket.enabled = hasWork;
        bucket.state = hasWork ? BucketState::Open : BucketState::Closed;
    }
    m_impl->interruptionRequested = false;
    m_impl->concurrentInterrupted = false;
    m_impl->condition.notify_all();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::ResetCompletedCycle()
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (m_impl->cycleActive || !m_impl->cycleCompleted)
    {
        return LxrWorkSchedulerResult::InvalidState;
    }
    if (m_impl->deferredEnabled)
    {
        if (m_impl->publishedGeneration != m_impl->generation ||
            m_impl->sealedDeferredIndex != LxrNoDeferredQueue)
        {
            return LxrWorkSchedulerResult::InvalidState;
        }
        if (!m_impl->IsBucketDrainedLocked(LxrWorkBucketStage::Unconstrained) ||
            (m_impl->activeDeferredIndex != LxrNoDeferredQueue &&
                !m_impl->deferredQueues[m_impl->activeDeferredIndex].IsEmpty()))
        {
            return LxrWorkSchedulerResult::Busy;
        }
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        if (m_impl->completionEnabled)
        {
            if (m_impl->activeDeferredIndex == LxrNoDeferredQueue)
            {
                return LxrWorkSchedulerResult::InvalidState;
            }
            Impl::DeferredQueueSet& completed =
                m_impl->deferredQueues[m_impl->activeDeferredIndex];
            LxrCompletionSnapshot snapshot =
                completed.completion.GetSnapshot();
            if (snapshot.generation != m_impl->generation)
            {
                return LxrWorkSchedulerResult::StaleGeneration;
            }
            if (snapshot.lifecycle == LxrCompletionLifecycle::Failed ||
                m_impl->completionFailure)
            {
                return LxrWorkSchedulerResult::CompletionFailed;
            }
            if (snapshot.lifecycle != LxrCompletionLifecycle::Drained ||
                snapshot.overallOutstanding != 0 ||
                snapshot.decrementOutstanding != 0 ||
                snapshot.callbackRunning)
            {
                return LxrWorkSchedulerResult::Busy;
            }
        }
#endif
    }
    for (size_t stageIndex = FirstStwIndex; stageIndex < StageCount; stageIndex++)
    {
        Impl::Bucket& bucket = m_impl->buckets[stageIndex];
        if (bucket.outstanding != 0 || bucket.executing != 0 || !bucket.queue.empty())
        {
            return LxrWorkSchedulerResult::Busy;
        }
        bucket.state = BucketState::Closed;
        bucket.enabled = true;
        bucket.accepting = true;
    }
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    if (m_impl->deferredEnabled &&
        m_impl->completionEnabled &&
        m_impl->activeDeferredIndex != LxrNoDeferredQueue)
    {
        Impl::DeferredQueueSet& completed =
            m_impl->deferredQueues[m_impl->activeDeferredIndex];
        LxrCompletionResult reuseResult =
            completed.completion.PrepareForReuse(m_impl->generation);
        if (reuseResult != LxrCompletionResult::Success)
        {
            return MapCompletionResult(reuseResult);
        }
    }
#endif
    m_impl->cycleCompleted = false;
    m_impl->currentStwStage = LxrWorkBucketStage::Count;
    m_impl->openTrace.clear();
    m_impl->resumeRecordedGeneration = 0;
    m_impl->completionTrace.clear();
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::WaitForPacket(
    const LxrWorkPacket* packet,
    uint32_t timeoutMilliseconds)
{
    if (packet == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::unique_lock<std::mutex> guard(m_impl->lock);
    bool completed = m_impl->condition.wait_for(
        guard,
        std::chrono::milliseconds(timeoutMilliseconds),
        [packet]() { return packet->GetState() == LxrWorkPacketState::Completed; });
    return completed ? LxrWorkSchedulerResult::Success : LxrWorkSchedulerResult::Timeout;
}

LxrWorkSchedulerResult LxrWorkScheduler::WaitForCycleCompletion(uint32_t timeoutMilliseconds)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::unique_lock<std::mutex> guard(m_impl->lock);
    bool completed = m_impl->condition.wait_for(
        guard,
        std::chrono::milliseconds(timeoutMilliseconds),
        [this]() { return m_impl->cycleCompleted; });
    return completed ? LxrWorkSchedulerResult::Success : LxrWorkSchedulerResult::Timeout;
}

LxrWorkSchedulerResult LxrWorkScheduler::WaitForIdle(uint32_t timeoutMilliseconds)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::unique_lock<std::mutex> guard(m_impl->lock);
    bool idle = m_impl->condition.wait_for(
        guard,
        std::chrono::milliseconds(timeoutMilliseconds),
        [this]()
        {
            for (const Impl::Bucket& bucket : m_impl->buckets)
            {
                if (bucket.outstanding != 0 || bucket.executing != 0)
                {
                    return false;
                }
            }
            return true;
        });
    return idle ? LxrWorkSchedulerResult::Success : LxrWorkSchedulerResult::Timeout;
}

LxrWorkSchedulerResult LxrWorkScheduler::WaitForBucketIdle(
    LxrWorkBucketStage stage, uint32_t timeoutMilliseconds)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if (StageIndex(stage) >= StageCount)
    {
        return LxrWorkSchedulerResult::InvalidStage;
    }
    std::unique_lock<std::mutex> guard(m_impl->lock);
    bool idle = m_impl->condition.wait_for(
        guard, std::chrono::milliseconds(timeoutMilliseconds),
        [this, stage]() { return m_impl->IsBucketDrainedLocked(stage); });
    return idle ? LxrWorkSchedulerResult::Success : LxrWorkSchedulerResult::Timeout;
}

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
LxrWorkSchedulerResult LxrWorkScheduler::WaitForCompletion(
    uint64_t expectedGeneration,
    uint32_t timeoutMilliseconds)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    if (!m_impl->completionEnabled)
    {
        return LxrWorkSchedulerResult::CompletionDisabled;
    }

    std::unique_lock<std::mutex> guard(m_impl->lock);
    bool completed = m_impl->condition.wait_for(
        guard,
        std::chrono::milliseconds(timeoutMilliseconds),
        [this, expectedGeneration]()
        {
            for (const Impl::DeferredQueueSet& queueSet :
                m_impl->deferredQueues)
            {
                LxrCompletionSnapshot snapshot =
                    queueSet.completion.GetSnapshot();
                if (snapshot.generation == expectedGeneration)
                {
                    return snapshot.lifecycle ==
                            LxrCompletionLifecycle::Drained ||
                        snapshot.lifecycle ==
                            LxrCompletionLifecycle::Cancelled ||
                        snapshot.lifecycle == LxrCompletionLifecycle::Failed;
                }
            }
            return false;
        });
    if (!completed)
    {
        return LxrWorkSchedulerResult::Timeout;
    }

    for (const Impl::DeferredQueueSet& queueSet : m_impl->deferredQueues)
    {
        LxrCompletionSnapshot snapshot = queueSet.completion.GetSnapshot();
        if (snapshot.generation == expectedGeneration)
        {
            return snapshot.lifecycle == LxrCompletionLifecycle::Drained
                ? LxrWorkSchedulerResult::Success
                : (snapshot.lifecycle == LxrCompletionLifecycle::Cancelled
                    ? LxrWorkSchedulerResult::Shutdown
                    : LxrWorkSchedulerResult::CompletionFailed);
        }
    }
    return LxrWorkSchedulerResult::StaleGeneration;
}
#endif

bool LxrWorkScheduler::IsStageOpen(LxrWorkBucketStage stage) const
{
    if (m_impl == nullptr || StageIndex(stage) >= StageCount)
    {
        return false;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    return m_impl->buckets[StageIndex(stage)].state == BucketState::Open;
}

bool LxrWorkScheduler::IsStageCompleted(LxrWorkBucketStage stage) const
{
    if (m_impl == nullptr || StageIndex(stage) >= StageCount)
    {
        return false;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    return m_impl->buckets[StageIndex(stage)].state == BucketState::Completed;
}

bool LxrWorkScheduler::IsStageEnabled(LxrWorkBucketStage stage) const
{
    if (m_impl == nullptr || StageIndex(stage) >= StageCount)
    {
        return false;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    return m_impl->buckets[StageIndex(stage)].enabled;
}

bool LxrWorkScheduler::IsCycleCompleted() const
{
    if (m_impl == nullptr)
    {
        return false;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    return m_impl->cycleCompleted;
}

uint64_t LxrWorkScheduler::GetStageOutstanding(LxrWorkBucketStage stage) const
{
    if (m_impl == nullptr || StageIndex(stage) >= StageCount)
    {
        return 0;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    return m_impl->buckets[StageIndex(stage)].outstanding;
}

uint64_t LxrWorkScheduler::GetGeneration() const
{
    if (m_impl == nullptr)
    {
        return 0;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    return m_impl->generation;
}

size_t LxrWorkScheduler::GetWorkerCount() const
{
    return m_impl == nullptr ? 0 : m_impl->workers.size();
}

size_t LxrWorkScheduler::CopyOpenTrace(LxrWorkBucketStage* stages, size_t capacity) const
{
    if (m_impl == nullptr)
    {
        return 0;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    size_t count = m_impl->openTrace.size();
    if (stages != nullptr)
    {
        size_t copyCount = count < capacity ? count : capacity;
        for (size_t index = 0; index < copyCount; index++)
        {
            stages[index] = m_impl->openTrace[index];
        }
    }
    return count;
}

size_t LxrWorkScheduler::CopyCompletionTrace(LxrWorkBucketStage* stages, size_t capacity) const
{
    if (m_impl == nullptr)
    {
        return 0;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    size_t count = m_impl->completionTrace.size();
    if (stages != nullptr)
    {
        size_t copyCount = count < capacity ? count : capacity;
        for (size_t index = 0; index < copyCount; index++)
        {
            stages[index] = m_impl->completionTrace[index];
        }
    }
    return count;
}

LxrWorkSchedulerMetrics LxrWorkScheduler::GetMetrics() const
{
    LxrWorkSchedulerMetrics metrics{};
    if (m_impl == nullptr)
    {
        return metrics;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    metrics.submitted = m_impl->submitted;
    metrics.executed = m_impl->executed;
    metrics.stolen = m_impl->stolen;
    metrics.spilled = m_impl->spilled;
    metrics.rejected = m_impl->rejected;
    metrics.ownershipViolations = m_impl->ownershipViolations;
    metrics.parks = m_impl->parks;
    metrics.wakes = m_impl->wakes;
    metrics.deferredSubmitted = m_impl->deferredSubmitted;
    metrics.deferredPublished = m_impl->deferredPublished;
    metrics.deferredExecuted = m_impl->deferredExecuted;
    metrics.deferredFlips = m_impl->deferredFlips;
    metrics.deferredEmptyFlips = m_impl->deferredEmptyFlips;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    if (m_impl->completionEnabled)
    {
        for (const Impl::DeferredQueueSet& queueSet : m_impl->deferredQueues)
        {
            LxrCompletionSnapshot snapshot =
                queueSet.completion.GetSnapshot();
            metrics.completionRegistrations += snapshot.registrations;
            metrics.completionCompletions += snapshot.completions;
            metrics.decrementZeroTransitions +=
                snapshot.decrementZeroTransitions;
            metrics.overallZeroTransitions +=
                snapshot.overallZeroTransitions;
        }
    }
#endif
    return metrics;
}

LxrDeferredWorkState LxrWorkScheduler::GetDeferredWorkState() const
{
    LxrDeferredWorkState state{};
    state.activeIndex = LxrNoDeferredQueue;
    state.producerIndex = LxrNoDeferredQueue;
    state.sealedIndex = LxrNoDeferredQueue;
    if (m_impl == nullptr)
    {
        return state;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    state.enabled = m_impl->deferredEnabled;
    state.resumeRecorded =
        m_impl->resumeRecordedGeneration != 0 &&
        m_impl->resumeRecordedGeneration == m_impl->generation;
    state.activeIndex = m_impl->activeDeferredIndex;
    state.producerIndex = m_impl->producerDeferredIndex;
    state.sealedIndex = m_impl->sealedDeferredIndex;
    state.producerGeneration =
        m_impl->producerDeferredIndex < m_impl->deferredQueues.size() ?
            m_impl->deferredQueues[m_impl->producerDeferredIndex].generation :
            0;
    state.sealedGeneration = m_impl->sealedGeneration;
    state.publishedGeneration = m_impl->publishedGeneration;
    if (m_impl->producerDeferredIndex < m_impl->deferredQueues.size())
    {
        const Impl::DeferredQueueSet& producer =
            m_impl->deferredQueues[m_impl->producerDeferredIndex];
        state.pendingNormal = producer.normal.size();
        state.pendingPrioritized = producer.prioritized.size();
    }
    if (m_impl->sealedDeferredIndex < m_impl->deferredQueues.size())
    {
        const Impl::DeferredQueueSet& sealed =
            m_impl->deferredQueues[m_impl->sealedDeferredIndex];
        state.sealedNormal = sealed.normal.size();
        state.sealedPrioritized = sealed.prioritized.size();
    }
    return state;
}

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
LxrWorkSchedulerResult LxrWorkScheduler::GetCompletionSnapshot(
    uint64_t expectedGeneration,
    LxrCompletionSnapshot* snapshot) const
{
    if (snapshot == nullptr)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    *snapshot = {};
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (!m_impl->completionEnabled)
    {
        return LxrWorkSchedulerResult::CompletionDisabled;
    }
    for (const Impl::DeferredQueueSet& queueSet : m_impl->deferredQueues)
    {
        LxrCompletionSnapshot candidate =
            queueSet.completion.GetSnapshot();
        if (candidate.generation == expectedGeneration)
        {
            *snapshot = candidate;
            return LxrWorkSchedulerResult::Success;
        }
    }
    return LxrWorkSchedulerResult::StaleGeneration;
}
#endif

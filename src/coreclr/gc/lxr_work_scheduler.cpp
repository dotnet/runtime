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

namespace
{
    // P26 (F4b). Queues of the scheduler. They replace the one global mutex of the scheduler on the hot paths: a worker takes
    // and adds packets without the mutex. The mutex is for the control plane (stages, pauses, deferred queues, parking).

    constexpr size_t StealBatchCapacity = 16;

    // The scheduler that the current thread works for, the index of the worker, and whether the packet that the worker runs
    // now is a trace packet. A packet that a worker submits goes to the deque of this worker.
    thread_local const void* tlsScheduler = nullptr;
    thread_local size_t tlsWorkerIndex = 0;
    thread_local bool tlsRunningTrace = false;

    // A small test-and-test-and-set lock for the injectors. The thread yields after a few spins, so a machine with one or two
    // processors does not burn its time slice.
    class SpinLock final
    {
    public:
        void Lock()
        {
            unsigned spins = 0;
            while (m_held.exchange(true, std::memory_order_acquire))
            {
                while (m_held.load(std::memory_order_relaxed))
                {
                    if (++spins > 32)
                    {
                        std::this_thread::yield();
                    }
                }
            }
        }

        void Unlock()
        {
            m_held.store(false, std::memory_order_release);
        }

    private:
        std::atomic<bool> m_held{false};
    };

    // The queue of a bucket for packets that no worker owns: packets from threads that are not workers, packets for a stage
    // that is not open yet, and packets that a worker spills. Workers take packets in batches. The Java LXR uses a lock-free
    // crossbeam Injector here. This one holds a spin lock for a few instructions; a batch take keeps the number of lock
    // operations low (one for up to 16 packets). The count is read without the lock.
    class Injector final
    {
    public:
        void Push(LxrWorkPacket* packet)
        {
            m_lock.Lock();
            m_items.push_back(packet);
            m_count.store(m_items.size(), std::memory_order_seq_cst);
            m_lock.Unlock();
        }

        size_t PopBatch(LxrWorkPacket** out, size_t maximum)
        {
            if (m_count.load(std::memory_order_seq_cst) == 0)
            {
                return 0;
            }
            m_lock.Lock();
            size_t count = m_items.size() < maximum ? m_items.size() : maximum;
            for (size_t index = 0; index < count; index++)
            {
                out[index] = m_items.front();
                m_items.pop_front();
            }
            m_count.store(m_items.size(), std::memory_order_seq_cst);
            m_lock.Unlock();
            return count;
        }

        size_t Count() const
        {
            return m_count.load(std::memory_order_seq_cst);
        }

    private:
        SpinLock m_lock;
        std::deque<LxrWorkPacket*> m_items;
        std::atomic<size_t> m_count{0};
    };

    // Chase-Lev work-stealing deque of packets (Le, Pop, Cohen, Zappa Nardelli: "Correct and Efficient Work-Stealing for Weak
    // Memory Models", with sequentially consistent operations instead of fences, so that ThreadSanitizer can check it).
    // The owner pushes and pops at the bottom; any thread may steal from the top. The Java LXR uses the crossbeam deque.
    // The ring grows when it is full. An old ring stays alive until the deque is destroyed, because a thief may still read it.
    class WorkDeque final
    {
    public:
        WorkDeque()
            : m_top(0), m_bottom(0), m_ring(new Ring(64))
        {
        }

        ~WorkDeque()
        {
            delete m_ring.load();
            for (Ring* ring : m_retired)
            {
                delete ring;
            }
        }

        WorkDeque(const WorkDeque&) = delete;
        WorkDeque& operator=(const WorkDeque&) = delete;

        enum class StealResult
        {
            Empty,
            Success,
            Retry,
        };

        // Owner only.
        void Push(LxrWorkPacket* packet)
        {
            int64_t bottom = m_bottom.load(std::memory_order_relaxed);
            int64_t top = m_top.load(std::memory_order_seq_cst);
            Ring* ring = m_ring.load(std::memory_order_relaxed);
            if (bottom - top >= static_cast<int64_t>(ring->capacity))
            {
                Ring* grown = new Ring(ring->capacity * 2);
                for (int64_t index = top; index < bottom; index++)
                {
                    grown->Put(index, ring->Get(index));
                }
                m_retired.push_back(ring);
                m_ring.store(grown, std::memory_order_seq_cst);
                ring = grown;
            }
            ring->Put(bottom, packet);
            m_bottom.store(bottom + 1, std::memory_order_seq_cst);
        }

        // Owner only. Returns null when the deque is empty.
        LxrWorkPacket* Pop()
        {
            int64_t bottom = m_bottom.load(std::memory_order_relaxed) - 1;
            Ring* ring = m_ring.load(std::memory_order_relaxed);
            m_bottom.store(bottom, std::memory_order_seq_cst);
            int64_t top = m_top.load(std::memory_order_seq_cst);
            if (top > bottom)
            {
                m_bottom.store(bottom + 1, std::memory_order_relaxed);
                return nullptr;
            }
            LxrWorkPacket* packet = ring->Get(bottom);
            if (top == bottom)
            {
                if (!m_top.compare_exchange_strong(top, top + 1, std::memory_order_seq_cst))
                {
                    packet = nullptr;
                }
                m_bottom.store(bottom + 1, std::memory_order_relaxed);
            }
            return packet;
        }

        // Any thread.
        StealResult Steal(LxrWorkPacket** packet)
        {
            int64_t top = m_top.load(std::memory_order_seq_cst);
            int64_t bottom = m_bottom.load(std::memory_order_seq_cst);
            if (top >= bottom)
            {
                return StealResult::Empty;
            }
            Ring* ring = m_ring.load(std::memory_order_seq_cst);
            LxrWorkPacket* value = ring->Get(top);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4B_STEAL_NO_CAS_CONTROL)
            // Control: the thief takes the packet without claiming it. The owner or another thief can take it too.
            m_top.store(top + 1, std::memory_order_seq_cst);
#else
            if (!m_top.compare_exchange_strong(top, top + 1, std::memory_order_seq_cst))
            {
                return StealResult::Retry;
            }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4B_STEAL_NO_CAS_CONTROL
            *packet = value;
            return StealResult::Success;
        }

        bool MaybeNonEmpty() const
        {
            return m_bottom.load(std::memory_order_seq_cst) > m_top.load(std::memory_order_seq_cst);
        }

    private:
        struct Ring final
        {
            explicit Ring(size_t slots)
                : capacity(slots), items(new std::atomic<LxrWorkPacket*>[slots])
            {
            }

            ~Ring()
            {
                delete[] items;
            }

            LxrWorkPacket* Get(int64_t index) const
            {
                return items[static_cast<size_t>(index) & (capacity - 1)].load(std::memory_order_relaxed);
            }

            void Put(int64_t index, LxrWorkPacket* packet)
            {
                items[static_cast<size_t>(index) & (capacity - 1)].store(packet, std::memory_order_relaxed);
            }

            size_t capacity;
            std::atomic<LxrWorkPacket*>* items;
        };

        std::atomic<int64_t> m_top;
        std::atomic<int64_t> m_bottom;
        std::atomic<Ring*> m_ring;
        std::vector<Ring*> m_retired;
    };
}

class LxrWorkScheduler::Impl final
{
public:
    // The state of a bucket changes only under the mutex, but workers read it without the mutex, so the fields are atomic.
    struct alignas(64) Bucket final
    {
        std::atomic<BucketState> state{BucketState::Closed};
        std::atomic<bool> enabled{true};
        std::atomic<bool> accepting{true};
        // Packets that are queued or running, and packets that run. A packet is counted in outstanding from Submit until
        // it has completed. The bucket is drained when both are zero.
        std::atomic<uint64_t> outstanding{0};
        std::atomic<uint64_t> executing{0};
        Injector injector;
    };

    // A worker has two deques. The general deque holds packets of the stages that are always runnable while they are open
    // (Unconstrained and the current STW stage). The trace deque holds packets of the concurrent trace stages. A pause
    // closes these stages. Their packets stay in the deque, so a pause does not move them (P26 F2).
    struct alignas(64) Worker final
    {
        WorkDeque general;
        WorkDeque trace;
        std::thread thread;
        bool parked = false;
        std::atomic<uint64_t> executed{0};
        std::atomic<uint64_t> stolen{0};
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

    // The published deferred packets. Workers take them with one atomic increment each, without the mutex. The vectors are
    // filled at publication, when no worker reads them, and cleared at the end of the next cycle, when every worker is parked.
    struct ActiveDeferred final
    {
        std::vector<LxrWorkPacket*> prioritized;
        std::vector<LxrWorkPacket*> normal;
        std::atomic<size_t> prioritizedNext{0};
        std::atomic<size_t> normalNext{0};
        std::atomic<bool> ready{false};

        uint64_t Remaining() const
        {
            if (!ready.load(std::memory_order_acquire))
            {
                return 0;
            }
            size_t prioritizedTaken = prioritizedNext.load(std::memory_order_seq_cst);
            size_t normalTaken = normalNext.load(std::memory_order_seq_cst);
            uint64_t remaining = 0;
            remaining += prioritizedTaken < prioritized.size() ? prioritized.size() - prioritizedTaken : 0;
            remaining += normalTaken < normal.size() ? normal.size() - normalTaken : 0;
            return remaining;
        }

        LxrWorkPacket* Take()
        {
            if (!ready.load(std::memory_order_seq_cst))
            {
                return nullptr;
            }
            if (prioritizedNext.load(std::memory_order_seq_cst) < prioritized.size())
            {
                size_t index = prioritizedNext.fetch_add(1, std::memory_order_seq_cst);
                if (index < prioritized.size())
                {
                    return prioritized[index];
                }
            }
            if (normalNext.load(std::memory_order_seq_cst) < normal.size())
            {
                size_t index = normalNext.fetch_add(1, std::memory_order_seq_cst);
                if (index < normal.size())
                {
                    return normal[index];
                }
            }
            return nullptr;
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

    // Takes a packet that Shutdown cancels back to the caller. The caller holds the mutex or is the only thread.
    void CancelPacket(LxrWorkPacket* packet, bool decrementOutstanding, std::vector<LxrWorkPacket*>* cancelledPackets)
    {
        if (packet->TryTransition(LxrWorkPacketState::Queued, LxrWorkPacketState::CallerOwned))
        {
            packet->SetGeneration(0);
            packet->SetPreferredWorker(LxrWorkScheduler::NoPreferredWorker);
            packet->SetDeferred(false);
            if (decrementOutstanding)
            {
                buckets[StageIndex(packet->GetStage())].outstanding--;
            }
            cancelledPackets->push_back(packet);
        }
        else
        {
            ownershipViolations++;
        }
    }

    void Shutdown()
    {
        std::vector<LxrWorkPacket*> cancelledPackets;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        uint64_t shutdownOwnershipViolations = 0;
        bool shutdownCompletionFailure = false;
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
            NotifyAllLocked();
        }

        // The workers leave their loop at the next poll. After they have joined, this thread is the only one that touches
        // the queues, so it may empty them without the mutex.
        for (Worker& worker : workers)
        {
            if (worker.thread.joinable())
            {
                worker.thread.join();
            }
        }

        {
            std::lock_guard<std::mutex> guard(lock);
            for (size_t index = 0; index < deferredQueues.size(); index++)
            {
                DeferredQueueSet& queueSet = deferredQueues[index];
                for (std::deque<LxrWorkPacket*>* queue : {&queueSet.prioritized, &queueSet.normal})
                {
                    while (!queue->empty())
                    {
                        LxrWorkPacket* packet = queue->front();
                        queue->pop_front();
                        CancelPacket(packet, false, &cancelledPackets);
                    }
                }
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
                if (completionEnabled && queueSet.completionSeed.IsActive())
                {
                    cancelledSeeds[index] =
                        std::move(queueSet.completionSeed);
                }
#endif
            }
            if (active.ready.load())
            {
                for (std::vector<LxrWorkPacket*>* packets : {&active.prioritized, &active.normal})
                {
                    size_t next = packets == &active.prioritized ? active.prioritizedNext.load() : active.normalNext.load();
                    for (size_t index = next; index < packets->size(); index++)
                    {
                        CancelPacket((*packets)[index], true, &cancelledPackets);
                    }
                    packets->clear();
                }
                active.ready.store(false);
            }
            for (Bucket& bucket : buckets)
            {
                LxrWorkPacket* batch[StealBatchCapacity];
                size_t count;
                while ((count = bucket.injector.PopBatch(batch, StealBatchCapacity)) != 0)
                {
                    for (size_t index = 0; index < count; index++)
                    {
                        CancelPacket(batch[index], true, &cancelledPackets);
                    }
                }
            }
            for (Worker& worker : workers)
            {
                for (WorkDeque* deque : {&worker.general, &worker.trace})
                {
                    while (LxrWorkPacket* packet = deque->Pop())
                    {
                        CancelPacket(packet, true, &cancelledPackets);
                    }
                }
            }
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

        if (cancelledCallback != nullptr)
        {
            for (LxrWorkPacket* packet : cancelledPackets)
            {
                cancelledCallback(packet, cancelledContext);
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

    // A stage is eligible when its packets may run now: it is open and enabled, and either no pause runs and the stage is
    // concurrent, or it is the Unconstrained stage or the current STW stage. Workers call it without the mutex.
    bool IsEligible(LxrWorkBucketStage stage) const
    {
        const Bucket& bucket = buckets[StageIndex(stage)];
        if (!bucket.enabled.load(std::memory_order_seq_cst) ||
            bucket.state.load(std::memory_order_seq_cst) != BucketState::Open)
        {
            return false;
        }

        if (!cycleActive.load(std::memory_order_seq_cst))
        {
            return LxrIsConcurrentStage(stage);
        }

        return stage == LxrWorkBucketStage::Unconstrained ||
            stage == currentStwStage.load(std::memory_order_seq_cst);
    }

    bool IsEligibleLocked(LxrWorkBucketStage stage) const
    {
        return IsEligible(stage);
    }

    static bool IsTraceStage(LxrWorkBucketStage stage)
    {
        return stage == LxrWorkBucketStage::Concurrent || stage == LxrWorkBucketStage::ConcurrentResumable;
    }

    // Eligible, and not a trace packet beyond the limit of concurrent trace workers (P25 F4).
    bool IsRunnable(LxrWorkBucketStage stage) const
    {
        if (!IsEligible(stage))
        {
            return false;
        }
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NO_CONC_LIMIT_CONTROL)
        return true;
#else
        return !(IsTraceStage(stage) &&
            concurrentTraceRunning.load(std::memory_order_seq_cst) >= concurrentTraceLimit.load(std::memory_order_relaxed));
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NO_CONC_LIMIT_CONTROL
    }

    bool IsTraceClassRunnable() const
    {
        return IsRunnable(LxrWorkBucketStage::Concurrent) || IsRunnable(LxrWorkBucketStage::ConcurrentResumable);
    }

    enum class ClaimResult
    {
        Claimed,
        NotRunnable,
        Lost,
    };

    // A worker has taken the packet from a queue. It counts the packet as running first and only then checks that its stage
    // is still runnable. InterruptConcurrentWork closes the stage first and then waits for the count, so one of the two sees the
    // other: a packet never starts after the pause has seen the stage idle.
    ClaimResult TryClaim(LxrWorkPacket* packet, size_t workerIndex)
    {
        LxrWorkBucketStage stage = packet->GetStage();
        Bucket& bucket = buckets[StageIndex(stage)];
        bucket.executing.fetch_add(1, std::memory_order_seq_cst);
        bool trace = IsTraceStage(stage);
        bool runnable = IsEligible(stage);
        if (runnable && trace)
        {
            size_t running = concurrentTraceRunning.fetch_add(1, std::memory_order_seq_cst);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NO_CONC_LIMIT_CONTROL)
            (void)running;
#else
            if (running >= concurrentTraceLimit.load(std::memory_order_relaxed))
            {
                concurrentTraceRunning.fetch_sub(1, std::memory_order_seq_cst);
                runnable = false;
            }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NO_CONC_LIMIT_CONTROL
        }
        if (!runnable)
        {
            bucket.executing.fetch_sub(1, std::memory_order_seq_cst);
            NotifyExternal();
            return ClaimResult::NotRunnable;
        }
        if (!packet->TryTransition(LxrWorkPacketState::Queued, LxrWorkPacketState::Executing))
        {
            ownershipViolations++;
            if (trace)
            {
                concurrentTraceRunning.fetch_sub(1, std::memory_order_seq_cst);
            }
            bucket.executing.fetch_sub(1, std::memory_order_seq_cst);
            NotifyExternal();
            return ClaimResult::Lost;
        }

        size_t preferredWorker = packet->GetPreferredWorker();
        if (preferredWorker != LxrWorkScheduler::NoPreferredWorker &&
            preferredWorker != workerIndex)
        {
            workers[workerIndex].stolen++;
            stolen++;
        }
        if (!packet->IsDeferred())
        {
            size_t queued = queuedPackets.load(std::memory_order_relaxed);
            while (queued != 0 &&
                !queuedPackets.compare_exchange_weak(queued, queued - 1, std::memory_order_relaxed))
            {
            }
        }
        return ClaimResult::Claimed;
    }

    // Gives a packet back that the worker could not run after all. It goes to the injector of its stage.
    void Requeue(LxrWorkPacket* packet)
    {
        buckets[StageIndex(packet->GetStage())].injector.Push(packet);
    }

    // Finds a packet for the worker and claims it. No mutex.
    LxrWorkPacket* Poll(size_t workerIndex)
    {
        Worker& worker = workers[workerIndex];
        LxrWorkPacket* packet;

        // 1. The own deque of general packets, newest first.
        while ((packet = worker.general.Pop()) != nullptr)
        {
            ClaimResult claim = TryClaim(packet, workerIndex);
            if (claim == ClaimResult::Claimed)
            {
                return packet;
            }
            if (claim == ClaimResult::NotRunnable)
            {
                worker.general.Push(packet);
                break;
            }
        }

        // 2. The own deque of trace packets, if the stage is open and the limit allows one more.
        bool traceRunnable = IsTraceClassRunnable();
        if (traceRunnable)
        {
            while ((packet = worker.trace.Pop()) != nullptr)
            {
                ClaimResult claim = TryClaim(packet, workerIndex);
                if (claim == ClaimResult::Claimed)
                {
                    return packet;
                }
                if (claim == ClaimResult::NotRunnable)
                {
                    worker.trace.Push(packet);
                    break;
                }
            }
        }

        // 3. The injectors, in the order of the stages. A batch moves to the own deque, as the Java LXR does with
        // steal_batch_and_pop.
        for (size_t stageIndex = 0; stageIndex < StageCount; stageIndex++)
        {
            LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(stageIndex);
            if (!IsRunnable(stage))
            {
                continue;
            }
            Bucket& bucket = buckets[stageIndex];
            if (stage == LxrWorkBucketStage::Unconstrained)
            {
                packet = active.Take();
                if (packet != nullptr)
                {
                    ClaimResult claim = TryClaim(packet, workerIndex);
                    if (claim == ClaimResult::Claimed)
                    {
                        return packet;
                    }
                    if (claim == ClaimResult::NotRunnable)
                    {
                        Requeue(packet);
                    }
                }
            }
            size_t available = bucket.injector.Count();
            if (available == 0)
            {
                continue;
            }
            LxrWorkPacket* batch[StealBatchCapacity];
            size_t want = available / workers.size() + 1;
            size_t count = bucket.injector.PopBatch(batch, want < StealBatchCapacity ? want : StealBatchCapacity);
            if (count == 0)
            {
                continue;
            }
            WorkDeque& local = IsTraceStage(stage) ? worker.trace : worker.general;
            for (size_t index = 1; index < count; index++)
            {
                local.Push(batch[index]);
            }
            ClaimResult claim = TryClaim(batch[0], workerIndex);
            if (claim == ClaimResult::Claimed)
            {
                return batch[0];
            }
            if (claim == ClaimResult::NotRunnable)
            {
                Requeue(batch[0]);
            }
        }

        // 4. Steal from the other workers.
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4B_NO_STEAL_CONTROL)
        if (false)
#else
        if (workers.size() > 1)
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4B_NO_STEAL_CONTROL
        {
            for (size_t offset = 1; offset < workers.size(); offset++)
            {
                Worker& victim = workers[(workerIndex + offset) % workers.size()];
                for (int cls = 0; cls < 2; cls++)
                {
                    bool traceClass = cls == 1;
                    if (traceClass && !traceRunnable)
                    {
                        continue;
                    }
                    WorkDeque& deque = traceClass ? victim.trace : victim.general;
                    WorkDeque::StealResult result;
                    do
                    {
                        result = deque.Steal(&packet);
                    } while (result == WorkDeque::StealResult::Retry);
                    if (result != WorkDeque::StealResult::Success)
                    {
                        continue;
                    }
                    ClaimResult claim = TryClaim(packet, workerIndex);
                    if (claim == ClaimResult::Claimed)
                    {
                        return packet;
                    }
                    if (claim == ClaimResult::NotRunnable)
                    {
                        (traceClass ? worker.trace : worker.general).Push(packet);
                    }
                }
            }
        }
        return nullptr;
    }

    // True when Poll could find a packet now. The worker has registered as parked before it asks, so a packet that a producer
    // adds after this scan makes the producer see the parked worker.
    bool HasWork(size_t workerIndex) const
    {
        bool traceRunnable = IsTraceClassRunnable();
        for (size_t index = 0; index < workers.size(); index++)
        {
            const Worker& worker = workers[index];
            if (worker.general.MaybeNonEmpty() || (traceRunnable && worker.trace.MaybeNonEmpty()))
            {
                return true;
            }
        }
        (void)workerIndex;
        for (size_t stageIndex = 0; stageIndex < StageCount; stageIndex++)
        {
            LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(stageIndex);
            if (buckets[stageIndex].injector.Count() != 0 && IsRunnable(stage))
            {
                return true;
            }
        }
        return IsEligible(LxrWorkBucketStage::Unconstrained) && active.Remaining() != 0;
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
        return bucket.outstanding.load(std::memory_order_seq_cst) == 0 &&
            bucket.executing.load(std::memory_order_seq_cst) == 0;
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
        // Every worker is parked and the published packets have all been taken (the Unconstrained bucket is drained).
        active.ready.store(false, std::memory_order_seq_cst);
        active.prioritized.clear();
        active.normal.clear();
        active.prioritizedNext.store(0, std::memory_order_relaxed);
        active.normalNext.store(0, std::memory_order_relaxed);
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
                    NotifyAllLocked();
                    break;
                }
                cycleActive = false;
                cycleCompleted = true;
                NotifyAllLocked();
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

    // Wakes the threads that wait for the scheduler (WaitFor...) when a counter that they may wait for has changed.
    void NotifyExternal()
    {
        if (externalWaiters.load(std::memory_order_seq_cst) != 0)
        {
            std::lock_guard<std::mutex> guard(lock);
            condition.notify_all();
        }
    }

    // Runs one claimed packet and completes it. The worker does not touch the packet after the state change to Completed:
    // the owner may free it then. A pause frees processed trace packets only after it has seen executing == 0.
    void RunPacket(LxrWorkPacket* packet, size_t workerIndex)
    {
        LxrWorkBucketStage stage = packet->GetStage();
        bool trace = IsTraceStage(stage);
        bool deferred = packet->IsDeferred();
        tlsRunningTrace = trace;
        packet->Execute(workerIndex);
        tlsRunningTrace = false;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        LxrCompletionResult completionResult = packet->CompleteRegistration();
#endif
        Bucket& bucket = buckets[StageIndex(stage)];
        if (packet->TryTransition(
            LxrWorkPacketState::Executing,
            LxrWorkPacketState::Completed))
        {
            bucket.outstanding--;
            workers[workerIndex].executed++;
            executed++;
            if (deferred)
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
        if (trace)
        {
            concurrentTraceRunning.fetch_sub(1, std::memory_order_seq_cst);
        }
        bucket.executing.fetch_sub(1, std::memory_order_seq_cst);
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NOTIFY_ALL_CONTROL)
        std::lock_guard<std::mutex> guard(lock);
        NotifyAllLocked();
#else
        // P25 (F4). A completed packet wakes only the threads that wait for the scheduler, and only if there are any.
        NotifyExternal();
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NOTIFY_ALL_CONTROL
    }

    void WorkerLoop(size_t workerIndex)
    {
        tlsScheduler = this;
        tlsWorkerIndex = workerIndex;
        while (!shuttingDown.load(std::memory_order_acquire))
        {
            LxrWorkPacket* packet = Poll(workerIndex);
            if (packet != nullptr)
            {
                RunPacket(packet, workerIndex);
                continue;
            }

            std::unique_lock<std::mutex> guard(lock);
            if (shuttingDown)
            {
                break;
            }
            Worker& worker = workers[workerIndex];
            worker.parked = true;
            parkedWorkers.fetch_add(1, std::memory_order_seq_cst);
            parks++;
            // A producer pushes first and reads parkedWorkers second; this thread raised it first and looks second.
            // One of the two sees the other, so a packet cannot be left in a queue while every worker sleeps.
            bool found = HasWork(workerIndex);
            bool openedWork = !found && AdvanceBucketsLocked();
            if (!found && openedWork)
            {
                // P25 (F4). A stage opened: wake as many workers as the stage has packets, not all of them.
                size_t packets = static_cast<size_t>(buckets[StageIndex(currentStwStage)].outstanding);
                if (packets >= parkedWorkers.load())
                {
                    NotifyAllLocked();
                }
                else
                {
                    for (size_t index = 0; index < packets; index++)
                    {
                        signals++;
                        wakeupsInFlight++;
                        workCondition.notify_one();
                    }
                }
            }
            else if (!found)
            {
                workCondition.wait(guard);
                wakes++;
                if (wakeupsInFlight.load() != 0)
                {
                    wakeupsInFlight--;
                }
            }
            worker.parked = false;
            parkedWorkers.fetch_sub(1, std::memory_order_seq_cst);
        }
    }

    std::array<Bucket, StageCount> buckets;
    std::vector<Worker> workers;
    mutable std::mutex lock;
    std::condition_variable condition;
    // P25 (F4). Idle workers sleep on their own condition variable, so that a packet wakes one worker and a packet that
    // completes wakes nobody. The condition variable above is for the threads that wait for the scheduler (WaitFor...).
    std::condition_variable workCondition;
    std::atomic<size_t> parkedWorkers{0};
    // Workers that a submit has woken and that have not run yet, and packets that wait in a queue (not deferred ones).
    std::atomic<size_t> wakeupsInFlight{0};
    std::atomic<size_t> queuedPackets{0};
    std::atomic<size_t> externalWaiters{0};
    std::atomic<uint64_t> signals{0};
    // P25 (F4). At most this many workers run the packets of the concurrent trace while the program runs. The Java LXR
    // uses ConcGCThreads of its ParallelGCThreads workers for the concurrent trace. Default: all workers.
    std::atomic<size_t> concurrentTraceLimit{SIZE_MAX};
    std::atomic<size_t> concurrentTraceRunning{0};
    CancelledPacketCallback cancelledCallback = nullptr;
    void* cancelledContext = nullptr;
    ActiveDeferred active;

    // Wakes every worker and every waiting thread. For changes of state: a stage opens or closes, a pause starts, shutdown.
    void NotifyAllLocked()
    {
        signals++;
        condition.notify_all();
        workCondition.notify_all();
    }

    // Wakes one idle worker for one new packet, but only if the workers that are on their way (woken, not yet running) do
    // not already cover the packets that wait. This is "wake as many workers as there is work". A trace packet that cannot
    // run because of the limit wakes nobody: the worker that finishes a trace packet takes it.
    void NotifyNewPacketLocked(LxrWorkBucketStage stage)
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NOTIFY_ALL_CONTROL)
        // Control: the behaviour before P25 (F4). Every packet wakes everybody.
        (void)stage;
        NotifyAllLocked();
#else
        if (IsTraceStage(stage) &&
            concurrentTraceRunning.load() >= concurrentTraceLimit.load())
        {
            return;
        }
        size_t inFlight = wakeupsInFlight.load();
        if (parkedWorkers.load() > inFlight && queuedPackets.load() > inFlight)
        {
            signals++;
            wakeupsInFlight++;
            workCondition.notify_one();
        }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NOTIFY_ALL_CONTROL
    }

    // The same for a thread that does not hold the mutex. It takes the mutex only when a worker is parked.
    void NotifyNewPacket(LxrWorkBucketStage stage)
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_NOTIFY_ALL_CONTROL)
        std::lock_guard<std::mutex> guard(lock);
        NotifyNewPacketLocked(stage);
#else
        if (parkedWorkers.load(std::memory_order_seq_cst) == 0)
        {
            return;
        }
        std::lock_guard<std::mutex> guard(lock);
        NotifyNewPacketLocked(stage);
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_NOTIFY_ALL_CONTROL
    }

    // Puts a packet that Submit has accepted (and counted in outstanding) into a queue. A worker that submits a packet of a
    // stage that is open keeps it in its own deque; a trace packet only if the worker runs a trace packet now. All other
    // packets go to the injector of the stage.
    void PushQueued(LxrWorkPacket* packet, bool mutexHeld)
    {
        LxrWorkBucketStage stage = packet->GetStage();
        bool onWorker = tlsScheduler == this;
        bool trace = IsTraceStage(stage);
        if (onWorker && IsEligible(stage) && (!trace || tlsRunningTrace))
        {
            Worker& worker = workers[tlsWorkerIndex];
            (trace ? worker.trace : worker.general).Push(packet);
        }
        else
        {
            if (onWorker && IsEligible(stage))
            {
                spilled++;
            }
            buckets[StageIndex(stage)].injector.Push(packet);
        }
        queuedPackets++;
        if (mutexHeld)
        {
            NotifyNewPacketLocked(stage);
        }
        else
        {
            NotifyNewPacket(stage);
        }
    }

    template <typename Predicate>
    bool WaitFor(std::unique_lock<std::mutex>& guard, std::chrono::milliseconds timeout, Predicate predicate)
    {
        externalWaiters++;
        bool result = condition.wait_for(guard, timeout, predicate);
        externalWaiters--;
        return result;
    }
    std::atomic<bool> shuttingDown;
    std::atomic<bool> cycleActive;
    bool cycleCompleted;
    std::atomic<bool> interruptionRequested;
    bool concurrentInterrupted;
    bool concurrentWasEnabled[2];
    bool concurrentWasOpen[2];
    std::atomic<uint64_t> generation;
    std::atomic<LxrWorkBucketStage> currentStwStage;
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
    std::atomic<uint64_t> submitted;
    std::atomic<uint64_t> executed;
    std::atomic<uint64_t> stolen;
    std::atomic<uint64_t> spilled;
    std::atomic<uint64_t> rejected;
    std::atomic<uint64_t> ownershipViolations;
    std::atomic<uint64_t> parks;
    std::atomic<uint64_t> wakes;
    std::atomic<uint64_t> deferredSubmitted;
    std::atomic<uint64_t> deferredPublished;
    std::atomic<uint64_t> deferredExecuted;
    std::atomic<uint64_t> deferredFlips;
    std::atomic<uint64_t> deferredEmptyFlips;
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    bool completionEnabled;
    LxrDecrementsDrainedCallback decrementsDrainedCallback;
    LxrOverallDrainedCallback overallDrainedCallback;
    void* completionCallbackContext;
    uint64_t maximumCompletionOutstanding;
    std::atomic<bool> completionFailure;
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

LxrWorkSchedulerResult LxrWorkScheduler::ConfigureConcurrentTraceWorkers(size_t limit)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    if (limit == 0 || limit > m_impl->workers.size())
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    m_impl->concurrentTraceLimit = limit;
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::SetCancelledPacketCallback(
    CancelledPacketCallback callback, void* context)
{
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    std::lock_guard<std::mutex> guard(m_impl->lock);
    m_impl->cancelledCallback = callback;
    m_impl->cancelledContext = context;
    return LxrWorkSchedulerResult::Success;
}

LxrWorkSchedulerResult LxrWorkScheduler::RunQueuedInline(
    LxrWorkBucketStage stage, size_t executionIndex, uint64_t* ran)
{
    if (ran == nullptr || StageIndex(stage) >= StageCount)
    {
        return LxrWorkSchedulerResult::InvalidArgument;
    }
    *ran = 0;
    if (m_impl == nullptr)
    {
        return LxrWorkSchedulerResult::NotInitialized;
    }
    size_t stageIndex = StageIndex(stage);
    std::vector<LxrWorkPacket*> packets;
    {
        std::lock_guard<std::mutex> guard(m_impl->lock);
        Impl::Bucket& bucket = m_impl->buckets[stageIndex];
        if (m_impl->shuttingDown)
        {
            return LxrWorkSchedulerResult::Shutdown;
        }
        if (m_impl->IsEligibleLocked(stage) || bucket.executing != 0)
        {
            return LxrWorkSchedulerResult::InvalidState;
        }
        LxrWorkPacket* batch[StealBatchCapacity];
        size_t taken;
        while ((taken = bucket.injector.PopBatch(batch, StealBatchCapacity)) != 0)
        {
            for (size_t index = 0; index < taken; index++)
            {
                packets.push_back(batch[index]);
            }
        }
        // The calling thread acts as a thief on the deques of the class of the stage. A packet of another stage goes back to
        // the injector of its own stage.
        bool traceClass = Impl::IsTraceStage(stage);
        for (Impl::Worker& worker : m_impl->workers)
        {
            WorkDeque& deque = traceClass ? worker.trace : worker.general;
            while (true)
            {
                LxrWorkPacket* stolen = nullptr;
                WorkDeque::StealResult stealResult = deque.Steal(&stolen);
                if (stealResult == WorkDeque::StealResult::Retry)
                {
                    continue;
                }
                if (stealResult == WorkDeque::StealResult::Empty)
                {
                    break;
                }
                if (stolen->GetStage() == stage)
                {
                    packets.push_back(stolen);
                }
                else
                {
                    m_impl->Requeue(stolen);
                }
            }
        }
        std::vector<LxrWorkPacket*> claimed;
        for (LxrWorkPacket* packet : packets)
        {
            if (!packet->TryTransition(LxrWorkPacketState::Queued, LxrWorkPacketState::Executing))
            {
                m_impl->ownershipViolations++;
                continue;
            }
            bucket.executing++;
            size_t queued = m_impl->queuedPackets.load();
            while (queued != 0 && !m_impl->queuedPackets.compare_exchange_weak(queued, queued - 1))
            {
            }
            claimed.push_back(packet);
        }
        packets.swap(claimed);
    }

    for (LxrWorkPacket* packet : packets)
    {
        packet->Execute(executionIndex);
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        LxrCompletionResult completionResult = packet->CompleteRegistration();
        if (completionResult != LxrCompletionResult::Success &&
            completionResult != LxrCompletionResult::AlreadyCompleted &&
            completionResult != LxrCompletionResult::Cancelled &&
            completionResult != LxrCompletionResult::CallbackFailed)
        {
            std::lock_guard<std::mutex> guard(m_impl->lock);
            m_impl->ownershipViolations++;
            m_impl->completionFailure = true;
        }
#endif
    }

    std::lock_guard<std::mutex> guard(m_impl->lock);
    Impl::Bucket& bucket = m_impl->buckets[stageIndex];
    for (LxrWorkPacket* packet : packets)
    {
        bucket.executing--;
        if (packet->TryTransition(LxrWorkPacketState::Executing, LxrWorkPacketState::Completed))
        {
            bucket.outstanding--;
            m_impl->executed++;
        }
        else
        {
            m_impl->ownershipViolations++;
        }
    }
    *ran = packets.size();
    if (m_impl->externalWaiters != 0)
    {
        m_impl->condition.notify_all();
    }
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

    // A worker submits without the mutex (P26 F4b); every other thread takes it, as before. The state that the checks read
    // is atomic, and a worker that submits is running a packet, so no stage can complete and no pause can start under it.
    bool onWorker = tlsScheduler == static_cast<const void*>(m_impl);
    std::unique_lock<std::mutex> guard(m_impl->lock, std::defer_lock);
    if (!onWorker)
    {
        guard.lock();
    }
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
    m_impl->PushQueued(packet, !onWorker);
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
            bucket.executing != 0)
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
    uint64_t packetGeneration = m_impl->generation.load();
    if (!m_impl->cycleActive)
    {
        packetGeneration++;
    }
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
    m_impl->PushQueued(packet, true);
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
    m_impl->NotifyAllLocked();
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
        m_impl->NotifyAllLocked();
    }

    bool ready = m_impl->WaitFor(
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
    m_impl->NotifyAllLocked();
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
            m_impl->active.Remaining() != 0)
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
    m_impl->NotifyAllLocked();
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
    m_impl->NotifyAllLocked();
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
    // The packets move to the structure that workers read without the mutex. No worker reads it now (ready is false).
    m_impl->active.prioritized.assign(published.prioritized.begin(), published.prioritized.end());
    m_impl->active.normal.assign(published.normal.begin(), published.normal.end());
    published.prioritized.clear();
    published.normal.clear();
    m_impl->active.prioritizedNext.store(0, std::memory_order_relaxed);
    m_impl->active.normalNext.store(0, std::memory_order_relaxed);
    m_impl->active.ready.store(true, std::memory_order_seq_cst);
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
        m_impl->NotifyAllLocked();
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
    m_impl->NotifyAllLocked();
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
        bool hasWork = bucket.outstanding != 0;
        bucket.enabled = hasWork;
        bucket.state = hasWork ? BucketState::Open : BucketState::Closed;
    }
    m_impl->interruptionRequested = false;
    m_impl->concurrentInterrupted = false;
    m_impl->NotifyAllLocked();
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
                m_impl->active.Remaining() != 0))
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
        if (bucket.outstanding != 0 || bucket.executing != 0)
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
    bool completed = m_impl->WaitFor(
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
    bool completed = m_impl->WaitFor(
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
    bool idle = m_impl->WaitFor(
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
    bool idle = m_impl->WaitFor(
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
    bool completed = m_impl->WaitFor(
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

bool LxrWorkScheduler::IsInterruptionRequested() const
{
    return m_impl != nullptr && m_impl->interruptionRequested.load(std::memory_order_acquire);
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
    metrics.signals = m_impl->signals;
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

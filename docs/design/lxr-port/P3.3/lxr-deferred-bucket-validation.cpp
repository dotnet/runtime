// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_work_packet.h"
#include "../../../../src/coreclr/gc/lxr_work_scheduler.h"

#include <atomic>
#include <chrono>
#include <cstdint>
#include <cstdio>
#include <memory>
#include <mutex>
#include <thread>
#include <vector>

namespace
{
    constexpr uint32_t TimeoutMilliseconds = 30'000;

    int s_checks;
    int s_passed;

    bool Check(bool condition, const char* name)
    {
        s_checks++;
        if (!condition)
        {
            std::printf("FAIL: %s\n", name);
            return false;
        }
        s_passed++;
        return true;
    }

    struct ExecutionState
    {
        std::mutex lock;
        std::vector<uint64_t> order;
        std::atomic<uint64_t> executions{0};
        std::atomic<uint64_t> checksum{0};
        std::atomic<bool> block{false};
        std::atomic<bool> started{false};
    };

    void ExecutePacket(LxrWorkPacket* packet, size_t workerIndex, void* context)
    {
        ExecutionState* state = static_cast<ExecutionState*>(context);
        uint64_t checksum = packet->GetId() ^ workerIndex;
        for (size_t index = 0; index < packet->GetReferenceCount(); index++)
        {
            checksum = (checksum * UINT64_C(1'099'511'628'211)) ^
                packet->GetReferences()[index];
        }
        state->checksum.fetch_xor(checksum, std::memory_order_relaxed);
        state->executions.fetch_add(1, std::memory_order_relaxed);
        {
            std::lock_guard<std::mutex> guard(state->lock);
            state->order.push_back(packet->GetId());
        }

        if (state->block.load(std::memory_order_acquire))
        {
            state->started.store(true, std::memory_order_release);
            while (state->block.load(std::memory_order_acquire))
            {
                std::this_thread::yield();
            }
        }
    }

    bool WaitUntil(const std::atomic<bool>& value)
    {
        std::chrono::steady_clock::time_point deadline =
            std::chrono::steady_clock::now() +
            std::chrono::milliseconds(TimeoutMilliseconds);
        while (!value.load(std::memory_order_acquire))
        {
            if (std::chrono::steady_clock::now() >= deadline)
            {
                return false;
            }
            std::this_thread::yield();
        }
        return true;
    }

    std::unique_ptr<LxrWorkPacket> NewPacket(
        uint64_t id,
        LxrWorkBucketStage stage,
        const std::vector<uintptr_t>& references,
        ExecutionState* state)
    {
        return std::make_unique<LxrWorkPacket>(
            id,
            stage,
            references.data(),
            references.size(),
            ExecutePacket,
            state);
    }

    bool FinishAndPublish(
        LxrWorkScheduler& scheduler,
        uint64_t generation,
        bool expectWork)
    {
        bool success = true;
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "cycle completes");
        success &= Check(
            scheduler.PublishDeferredConcurrentWork(generation) ==
                LxrWorkSchedulerResult::InvalidState,
            "publication before resume rejected");
        success &= Check(
            scheduler.ResumeConcurrentWork() == LxrWorkSchedulerResult::Success,
            "concurrent work resume recorded");
        success &= Check(
            scheduler.PublishDeferredConcurrentWork(generation) ==
                LxrWorkSchedulerResult::Success,
            "deferred work published");
        success &= Check(
            scheduler.PublishDeferredConcurrentWork(generation) ==
                LxrWorkSchedulerResult::InvalidState,
            "duplicate publication rejected");
        LxrDeferredWorkState state = scheduler.GetDeferredWorkState();
        success &= Check(
            state.publishedGeneration == generation &&
                state.sealedIndex == LxrNoDeferredQueue,
            "publication state records generation");
        success &= Check(
            (state.activeIndex != LxrNoDeferredQueue) == true,
            "publication selects an active side");
        if (!expectWork)
        {
            success &= Check(
                scheduler.GetMetrics().deferredEmptyFlips > 0,
                "empty publication recorded");
        }
        return success;
    }

    bool ValidateDisabledAndInvalidInputs()
    {
        LxrWorkScheduler scheduler;
        bool success = Check(
            scheduler.Initialize(1) == LxrWorkSchedulerResult::Success,
            "disabled scheduler initialize");
        std::vector<uintptr_t> references(4, 11);
        ExecutionState state;
        std::unique_ptr<LxrWorkPacket> packet = NewPacket(
            1,
            LxrWorkBucketStage::Unconstrained,
            references,
            &state);
        success &= Check(
            scheduler.SubmitDeferredConcurrent(
                packet.get(),
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::InvalidState,
            "disabled deferred submission rejected");
        success &= Check(
            scheduler.PublishDeferredConcurrentWork(1) ==
                LxrWorkSchedulerResult::InvalidState,
            "disabled publication rejected");
        success &= Check(
            !scheduler.GetDeferredWorkState().enabled,
            "disabled state reported");
        scheduler.Shutdown();

        LxrWorkScheduler enabled;
        success &= Check(
            enabled.Initialize(1, true) == LxrWorkSchedulerResult::Success,
            "enabled scheduler initialize");
        std::unique_ptr<LxrWorkPacket> invalidStage = NewPacket(
            2,
            LxrWorkBucketStage::Initial,
            references,
            &state);
        success &= Check(
            enabled.SubmitDeferredConcurrent(
                invalidStage.get(),
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::InvalidStage,
            "deferred work requires unconstrained stage");
        success &= Check(
            enabled.SubmitDeferredConcurrent(
                packet.get(),
                static_cast<LxrDeferredWorkPriority>(99)) ==
                LxrWorkSchedulerResult::InvalidArgument,
            "invalid deferred priority rejected");
        enabled.Shutdown();
        return success;
    }

    bool ValidateDirectionVisibilityAndGenerations()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(1, true) == LxrWorkSchedulerResult::Success,
            "direction scheduler initialize"))
        {
            return false;
        }

        bool success = true;
        ExecutionState state;
        std::vector<uintptr_t> references(32, 0x1234);
        std::unique_ptr<LxrWorkPacket> normal = NewPacket(
            10,
            LxrWorkBucketStage::Unconstrained,
            references,
            &state);
        std::unique_ptr<LxrWorkPacket> prioritized = NewPacket(
            11,
            LxrWorkBucketStage::Unconstrained,
            references,
            &state);
        success &= Check(
            scheduler.SubmitDeferredConcurrent(
                normal.get(),
                LxrDeferredWorkPriority::Normal,
                0) == LxrWorkSchedulerResult::Success,
            "normal packet enters producer side");
        success &= Check(
            scheduler.SubmitDeferredConcurrent(
                prioritized.get(),
                LxrDeferredWorkPriority::Prioritized,
                0) == LxrWorkSchedulerResult::Success,
            "prioritized packet enters producer side");
        LxrDeferredWorkState before = scheduler.GetDeferredWorkState();
        success &= Check(
            before.activeIndex == 0 &&
                before.producerIndex == 1 &&
                before.producerGeneration == 1 &&
                before.pendingNormal == 1 &&
                before.pendingPrioritized == 1,
            "initial active and producer directions");
        std::this_thread::sleep_for(std::chrono::milliseconds(20));
        success &= Check(
            state.executions.load(std::memory_order_relaxed) == 0,
            "producer side invisible before cycle");
        success &= Check(
            scheduler.GetStageOutstanding(LxrWorkBucketStage::Unconstrained) == 0,
            "producer side excluded from active outstanding count");

        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "first cycle begins");
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "first cycle completes");
        LxrDeferredWorkState sealed = scheduler.GetDeferredWorkState();
        success &= Check(
            sealed.activeIndex == LxrNoDeferredQueue &&
                sealed.sealedIndex == 1 &&
                sealed.producerIndex == 0 &&
                sealed.sealedGeneration == 1 &&
                sealed.producerGeneration == 2 &&
                sealed.sealedNormal == 1 &&
                sealed.sealedPrioritized == 1,
            "cycle completion seals and reverses direction");
        success &= Check(
            state.executions.load(std::memory_order_relaxed) == 0,
            "sealed side invisible before publication");

        std::unique_ptr<LxrWorkPacket> nextGeneration = NewPacket(
            12,
            LxrWorkBucketStage::Unconstrained,
            references,
            &state);
        success &= Check(
            scheduler.SubmitDeferredConcurrent(
                nextGeneration.get(),
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::Success,
            "post-seal producer binds next generation");
        success &= Check(
            nextGeneration->GetGeneration() == 2,
            "post-seal packet generation");

        success &= Check(
            scheduler.ResetCompletedCycle() ==
                LxrWorkSchedulerResult::InvalidState,
            "reset before publication rejected");
        success &= FinishAndPublish(scheduler, 1, true);
        success &= Check(
            scheduler.GetStageOutstanding(LxrWorkBucketStage::Unconstrained) == 2,
            "publication accounts active packets");
        success &= Check(
            scheduler.WaitForPacket(prioritized.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success &&
                scheduler.WaitForPacket(normal.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "published packets execute");
        success &= Check(
            state.executions.load(std::memory_order_relaxed) == 2,
            "next generation remains invisible");
        {
            std::lock_guard<std::mutex> guard(state.lock);
            success &= Check(
                state.order.size() >= 2 &&
                    state.order[0] == 11 &&
                    state.order[1] == 10,
                "prioritized lane executes before normal lane");
        }
        success &= Check(
            scheduler.WaitForIdle(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "published first generation drains");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "first generation resets after drain");

        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "second cycle begins");
        success &= FinishAndPublish(scheduler, 2, true);
        success &= Check(
            scheduler.WaitForPacket(nextGeneration.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "next generation visible after its flip");
        success &= Check(
            state.executions.load(std::memory_order_relaxed) == 3,
            "no loss or duplication across direction reversal");
        success &= Check(
            scheduler.WaitForIdle(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success &&
                scheduler.ResetCompletedCycle() ==
                    LxrWorkSchedulerResult::Success,
            "second generation drains and resets");

        LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
        success &= Check(
            metrics.deferredSubmitted == 3 &&
                metrics.deferredPublished == 3 &&
                metrics.deferredExecuted == 3 &&
                metrics.deferredFlips == 2 &&
                metrics.deferredEmptyFlips == 0 &&
                metrics.ownershipViolations == 0,
            "deferred metrics exact across repeated cycles");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateEmptyAndEagerCycles()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(2, true) == LxrWorkSchedulerResult::Success,
            "empty scheduler initialize"))
        {
            return false;
        }

        bool success = Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "empty cycle begins");
        success &= FinishAndPublish(scheduler, 1, false);
        LxrDeferredWorkState state = scheduler.GetDeferredWorkState();
        success &= Check(
            state.activeIndex == 1 &&
                state.producerIndex == 0 &&
                state.producerGeneration == 2,
            "empty flip still reverses direction");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "empty cycle resets");

        ExecutionState execution;
        std::vector<uintptr_t> references(16, 9);
        std::unique_ptr<LxrWorkPacket> eager = NewPacket(
            20,
            LxrRCProcessDecs,
            references,
            &execution);
        success &= Check(
            scheduler.Submit(eager.get()) == LxrWorkSchedulerResult::Success,
            "lazy-off decrement enters STW bucket");
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "eager cycle begins");
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "eager cycle completes");
        success &= Check(
            execution.executions.load(std::memory_order_relaxed) == 1 &&
                eager->GetState() == LxrWorkPacketState::Completed,
            "lazy-off decrement executes during pause");
        success &= Check(
            scheduler.GetDeferredWorkState().sealedNormal == 0 &&
                scheduler.GetDeferredWorkState().sealedPrioritized == 0,
            "lazy-off cycle seals no deferred packets");
        success &= Check(
            scheduler.ResumeConcurrentWork() == LxrWorkSchedulerResult::Success &&
                scheduler.PublishDeferredConcurrentWork(2) ==
                    LxrWorkSchedulerResult::Success,
            "lazy-off cycle performs explicit empty flip");
        success &= Check(
            scheduler.GetMetrics().deferredEmptyFlips == 2,
            "both empty flips recorded");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "eager cycle resets");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateProducerRaces()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(2, true) == LxrWorkSchedulerResult::Success,
            "race scheduler initialize"))
        {
            return false;
        }

        bool success = true;
        ExecutionState blocking;
        blocking.block.store(true, std::memory_order_release);
        ExecutionState deferred;
        std::vector<uintptr_t> references(8, 7);
        std::unique_ptr<LxrWorkPacket> blocker = NewPacket(
            30,
            LxrWorkBucketStage::Initial,
            references,
            &blocking);
        std::unique_ptr<LxrWorkPacket> beforeSeal = NewPacket(
            31,
            LxrWorkBucketStage::Unconstrained,
            references,
            &deferred);
        success &= Check(
            scheduler.Submit(blocker.get()) == LxrWorkSchedulerResult::Success,
            "blocking STW packet queued");
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "race cycle begins");
        success &= Check(WaitUntil(blocking.started), "blocking packet starts");
        success &= Check(
            scheduler.SubmitDeferredConcurrent(
                beforeSeal.get(),
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::Success &&
                beforeSeal->GetGeneration() == 1,
            "producer before seal binds current generation");
        blocking.block.store(false, std::memory_order_release);
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "race cycle completes after release");

        std::unique_ptr<LxrWorkPacket> afterSeal = NewPacket(
            32,
            LxrWorkBucketStage::Unconstrained,
            references,
            &deferred);
        std::thread producer([&]()
        {
            LxrWorkSchedulerResult result = scheduler.SubmitDeferredConcurrent(
                afterSeal.get(),
                LxrDeferredWorkPriority::Normal);
            if (result != LxrWorkSchedulerResult::Success)
            {
                deferred.checksum.store(UINT64_MAX, std::memory_order_release);
            }
        });
        producer.join();
        success &= Check(
            afterSeal->GetGeneration() == 2 &&
                deferred.checksum.load(std::memory_order_acquire) != UINT64_MAX,
            "producer after seal binds next generation");
        success &= Check(
            scheduler.ResumeConcurrentWork() == LxrWorkSchedulerResult::Success &&
                scheduler.PublishDeferredConcurrentWork(1) ==
                    LxrWorkSchedulerResult::Success,
            "race generation publishes");
        success &= Check(
            scheduler.WaitForPacket(beforeSeal.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "pre-seal packet published");
        success &= Check(
            deferred.executions.load(std::memory_order_relaxed) == 1 &&
                afterSeal->GetState() == LxrWorkPacketState::Queued,
            "post-seal packet remains inactive");
        success &= Check(
            scheduler.WaitForIdle(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success &&
                scheduler.ResetCompletedCycle() ==
                    LxrWorkSchedulerResult::Success,
            "race generation drains");
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "post-race generation begins");
        success &= FinishAndPublish(scheduler, 2, true);
        success &= Check(
            scheduler.WaitForPacket(afterSeal.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success &&
                deferred.executions.load(std::memory_order_relaxed) == 2,
            "post-seal packet publishes in next cycle");
        success &= Check(
            scheduler.WaitForIdle(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success &&
                scheduler.ResetCompletedCycle() ==
                    LxrWorkSchedulerResult::Success,
            "post-race generation resets");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateStealingAndBusyRecovery()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(8, true) == LxrWorkSchedulerResult::Success,
            "stealing scheduler initialize"))
        {
            return false;
        }

        bool success = true;
        ExecutionState state;
        state.block.store(true, std::memory_order_release);
        std::vector<uintptr_t> references(64, 17);
        std::vector<std::unique_ptr<LxrWorkPacket>> packets;
        for (uint64_t id = 64; id < 192; id++)
        {
            packets.push_back(NewPacket(
                id,
                LxrWorkBucketStage::Unconstrained,
                references,
                &state));
            success &= Check(
                scheduler.SubmitDeferredConcurrent(
                    packets.back().get(),
                    LxrDeferredWorkPriority::Normal,
                    0) == LxrWorkSchedulerResult::Success,
                "skewed deferred packet queued");
        }
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "stealing cycle begins");
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success &&
                scheduler.ResumeConcurrentWork() ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.PublishDeferredConcurrentWork(1) ==
                    LxrWorkSchedulerResult::Success,
            "skewed deferred packets publish");
        success &= Check(WaitUntil(state.started), "published work begins");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Busy,
            "reset fails busy while published work executes");
        state.block.store(false, std::memory_order_release);
        success &= Check(
            scheduler.WaitForIdle(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "skewed work drains");
        for (const std::unique_ptr<LxrWorkPacket>& packet : packets)
        {
            success &= Check(
                packet->GetState() == LxrWorkPacketState::Completed,
                "skewed packet executes exactly once");
        }
        LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
        success &= Check(metrics.stolen > 0, "published work can be stolen");
        success &= Check(
            metrics.deferredExecuted == packets.size() &&
                metrics.ownershipViolations == 0,
            "published active side has exact ownership");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "busy reset recovers after drain");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateShutdownOwnership()
    {
        bool success = true;
        std::vector<uintptr_t> references(4, 3);
        ExecutionState state;

        LxrWorkScheduler producerScheduler;
        success &= Check(
            producerScheduler.Initialize(1, true) ==
                LxrWorkSchedulerResult::Success,
            "producer shutdown scheduler initialize");
        std::unique_ptr<LxrWorkPacket> producer = NewPacket(
            200,
            LxrWorkBucketStage::Unconstrained,
            references,
            &state);
        success &= Check(
            producerScheduler.SubmitDeferredConcurrent(
                producer.get(),
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::Success,
            "producer packet queued for shutdown");
        producerScheduler.Shutdown();
        success &= Check(
            producer->GetState() == LxrWorkPacketState::CallerOwned &&
                producer->GetGeneration() == 0,
            "shutdown returns producer-side ownership");

        LxrWorkScheduler sealedScheduler;
        success &= Check(
            sealedScheduler.Initialize(1, true) ==
                LxrWorkSchedulerResult::Success,
            "sealed shutdown scheduler initialize");
        std::unique_ptr<LxrWorkPacket> sealed = NewPacket(
            201,
            LxrWorkBucketStage::Unconstrained,
            references,
            &state);
        success &= Check(
            sealedScheduler.SubmitDeferredConcurrent(
                sealed.get(),
                LxrDeferredWorkPriority::Prioritized) ==
                LxrWorkSchedulerResult::Success &&
                sealedScheduler.BeginStwCycle() ==
                    LxrWorkSchedulerResult::Success &&
                sealedScheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success,
            "packet reaches sealed side");
        sealedScheduler.Shutdown();
        success &= Check(
            sealed->GetState() == LxrWorkPacketState::CallerOwned &&
                sealed->GetGeneration() == 0,
            "shutdown returns sealed-side ownership");
        return success;
    }
}

int main()
{
    bool success = true;
    success &= ValidateDisabledAndInvalidInputs();
    success &= ValidateDirectionVisibilityAndGenerations();
    success &= ValidateEmptyAndEagerCycles();
    success &= ValidateProducerRaces();
    success &= ValidateStealingAndBusyRecovery();
    success &= ValidateShutdownOwnership();

    if (!success || s_passed != s_checks)
    {
        std::printf("FAIL: %d/%d P3.3 validation checks passed\n", s_passed, s_checks);
        return 1;
    }

    std::printf("PASS: %d/%d P3.3 validation checks\n", s_passed, s_checks);
    return 0;
}

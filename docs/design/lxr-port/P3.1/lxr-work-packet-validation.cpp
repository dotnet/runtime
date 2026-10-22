// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_work_packet.h"
#include "../../../../src/coreclr/gc/lxr_work_scheduler.h"

#include <array>
#include <atomic>
#include <chrono>
#include <cstdint>
#include <cstdio>
#include <memory>
#include <mutex>
#include <string_view>
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
        std::vector<LxrWorkBucketStage> stages;
        std::array<std::atomic<uint32_t>, 512> executions{};
        std::atomic<uint64_t> checksum{0};
        std::atomic<bool> block{false};
        std::atomic<bool> blockingStarted{false};
    };

    void ExecutePacket(LxrWorkPacket* packet, size_t workerIndex, void* context)
    {
        ExecutionState* state = static_cast<ExecutionState*>(context);
        if (packet->GetId() < state->executions.size())
        {
            state->executions[packet->GetId()].fetch_add(1, std::memory_order_relaxed);
        }

        uint64_t checksum = packet->GetId() + workerIndex;
        for (size_t index = 0; index < packet->GetReferenceCount(); index++)
        {
            checksum = (checksum * 1'099'511'628'211ULL) ^
                packet->GetReferences()[index];
        }
        state->checksum.fetch_xor(checksum, std::memory_order_relaxed);

        {
            std::lock_guard<std::mutex> guard(state->lock);
            state->stages.push_back(packet->GetStage());
        }

        if (state->block.load(std::memory_order_acquire) &&
            packet->GetStage() == LxrWorkBucketStage::Initial)
        {
            state->blockingStarted.store(true, std::memory_order_release);
            while (state->block.load(std::memory_order_acquire))
            {
                std::this_thread::yield();
            }
        }
    }

    bool WaitUntil(const std::atomic<bool>& value)
    {
        auto deadline = std::chrono::steady_clock::now() +
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

    bool WaitForParks(
        const LxrWorkScheduler& scheduler,
        uint64_t target)
    {
        auto deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(TimeoutMilliseconds);
        while (scheduler.GetMetrics().parks < target)
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

    bool ValidateStageContract()
    {
        constexpr const char* Expected[] = {
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

        bool success = true;
        for (size_t index = 0; index < std::size(Expected); index++)
        {
            LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(index);
            success &= Check(
                std::string_view(LxrGetWorkBucketStageName(stage)) == Expected[index],
                "stage name and ordinal");
        }
        success &= Check(LxrRCProcessIncs == LxrWorkBucketStage::Initial, "increment stage");
        success &= Check(
            LxrRCProcessDecs == LxrWorkBucketStage::STWRCDecsAndSweep,
            "decrement stage");
        success &= Check(LxrFirstStwStage == LxrWorkBucketStage::FinishConcurrentWork, "first STW stage");
        success &= Check(LxrIsConcurrentStage(LxrWorkBucketStage::ConcurrentResumable), "resumable concurrent");
        success &= Check(!LxrIsConcurrentStage(LxrWorkBucketStage::Initial), "initial is STW");
        return success;
    }

    bool ValidatePacketization()
    {
        constexpr size_t Counts[] = {
            0, 1, 255, 256, 511, 512, 1023, 1024, 1025, 2048, 4096, 4097,
        };
        bool success = true;
        for (size_t count : Counts)
        {
            size_t packets = 0;
            bool result = LxrTryGetReferencePacketCount(
                count,
                LxrDefaultRcPacketReferenceCapacity,
                &packets);
            size_t expected = count == 0 ? 0 :
                (count + LxrDefaultRcPacketReferenceCapacity - 1) /
                    LxrDefaultRcPacketReferenceCapacity;
            success &= Check(result && packets == expected, "default packetization boundary");
        }

        for (size_t capacity : {size_t{256}, size_t{512}, size_t{1024}, size_t{2048}, size_t{4096}})
        {
            size_t packets = 0;
            success &= Check(
                LxrTryGetReferencePacketCount(capacity + 1, capacity, &packets) &&
                    packets == 2,
                "supported packet capacity");
        }
        size_t packets = 0;
        success &= Check(
            !LxrTryGetReferencePacketCount(1, 128, &packets),
            "unsupported packet capacity");
        success &= Check(
            !LxrTryGetReferencePacketCount(1, 1024, nullptr),
            "packetization output required");
        return success;
    }

    bool ValidateStealing()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(8) == LxrWorkSchedulerResult::Success,
            "stealing scheduler initialize"))
        {
            return false;
        }

        ExecutionState state;
        std::vector<uintptr_t> references(64);
        for (size_t index = 0; index < references.size(); index++)
        {
            references[index] = index * 17 + 3;
        }

        std::vector<std::unique_ptr<LxrWorkPacket>> packets;
        for (uint64_t id = 0; id < 128; id++)
        {
            packets.push_back(NewPacket(
                id,
                LxrWorkBucketStage::Unconstrained,
                references,
                &state));
            if (!Check(
                scheduler.Submit(packets.back().get(), 0) ==
                    LxrWorkSchedulerResult::Success,
                "submit skewed packet"))
            {
                scheduler.Shutdown();
                return false;
            }
        }

        bool success = Check(
            scheduler.WaitForIdle(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "stealing workload drains");
        for (uint64_t id = 0; id < packets.size(); id++)
        {
            success &= Check(
                state.executions[id].load(std::memory_order_relaxed) == 1,
                "packet executes exactly once");
            success &= Check(
                packets[id]->GetState() == LxrWorkPacketState::Completed,
                "packet ownership returns");
        }

        LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
        success &= Check(metrics.executed == packets.size(), "executed count");
        success &= Check(metrics.submitted == packets.size(), "submitted count");
        success &= Check(metrics.stolen > 0, "non-owner work stealing observed");
        success &= Check(metrics.ownershipViolations == 0, "no ownership violations");
        success &= Check(
            scheduler.Submit(packets[0].get()) ==
                LxrWorkSchedulerResult::PacketAlreadyOwned,
            "completed packet cannot be resubmitted");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateOrderedBuckets()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(8) == LxrWorkSchedulerResult::Success,
            "ordered scheduler initialize"))
        {
            return false;
        }

        ExecutionState state;
        std::vector<uintptr_t> references(32, 0x1234);
        constexpr LxrWorkBucketStage Stages[] = {
            LxrWorkBucketStage::FinishConcurrentWork,
            LxrWorkBucketStage::Initial,
            LxrWorkBucketStage::Prepare,
            LxrWorkBucketStage::Closure,
            LxrWorkBucketStage::Release,
            LxrWorkBucketStage::STWRCDecsAndSweep,
            LxrWorkBucketStage::Final,
        };

        std::vector<std::unique_ptr<LxrWorkPacket>> packets;
        uint64_t id = 128;
        for (LxrWorkBucketStage stage : Stages)
        {
            for (size_t packetIndex = 0; packetIndex < 8; packetIndex++)
            {
                packets.push_back(NewPacket(id++, stage, references, &state));
                if (!Check(
                    scheduler.Submit(packets.back().get()) ==
                        LxrWorkSchedulerResult::Success,
                    "queue closed-stage packet"))
                {
                    scheduler.Shutdown();
                    return false;
                }
            }
        }

        bool success = Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "begin STW cycle");
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "ordered cycle completes");

        std::array<LxrWorkBucketStage, std::size(Stages)> trace{};
        size_t traceCount = scheduler.CopyOpenTrace(trace.data(), trace.size());
        success &= Check(traceCount == trace.size(), "open trace count");
        for (size_t index = 0; index < trace.size(); index++)
        {
            success &= Check(trace[index] == Stages[index], "exact bucket open order");
            success &= Check(scheduler.IsStageCompleted(Stages[index]), "stage completed once");
            success &= Check(scheduler.GetStageOutstanding(Stages[index]) == 0, "stage drained");
        }

        {
            std::lock_guard<std::mutex> guard(state.lock);
            size_t previous = static_cast<size_t>(LxrFirstStwStage);
            for (LxrWorkBucketStage stage : state.stages)
            {
                size_t current = static_cast<size_t>(stage);
                success &= Check(current >= previous, "execution stage order monotonic");
                previous = current;
            }
        }

        for (const std::unique_ptr<LxrWorkPacket>& packet : packets)
        {
            success &= Check(
                state.executions[packet->GetId()].load(std::memory_order_relaxed) == 1,
                "ordered packet executes once");
        }

        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "completed cycle resets");
        scheduler.Shutdown();
        return success;
    }

    bool ValidatePrematureOpening()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(4) == LxrWorkSchedulerResult::Success,
            "blocking scheduler initialize"))
        {
            return false;
        }

        ExecutionState state;
        state.block.store(true, std::memory_order_release);
        std::vector<uintptr_t> references(8, 7);
        std::unique_ptr<LxrWorkPacket> initial = NewPacket(
            384,
            LxrWorkBucketStage::Initial,
            references,
            &state);
        std::unique_ptr<LxrWorkPacket> prepare = NewPacket(
            385,
            LxrWorkBucketStage::Prepare,
            references,
            &state);

        bool success = Check(WaitForParks(scheduler, 4), "workers initially park");
        uint64_t parksBeforeSubmissions = scheduler.GetMetrics().parks;
        success &= Check(
            scheduler.Submit(initial.get()) == LxrWorkSchedulerResult::Success,
            "submit blocking initial");
        success &= Check(
            scheduler.Submit(prepare.get()) == LxrWorkSchedulerResult::Success,
            "submit later prepare");
        success &= Check(
            WaitForParks(scheduler, parksBeforeSubmissions + 1),
            "a worker wakes for a closed-stage submission and reparks (P25 F4: a submission wakes one worker, not all four)");
        uint64_t parksBeforeCycle = scheduler.GetMetrics().parks;
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success,
            "begin blocking cycle");
        success &= Check(WaitUntil(state.blockingStarted), "initial packet starts");
        success &= Check(
            WaitForParks(scheduler, parksBeforeCycle + 3),
            "other workers attempt stage advancement");
        success &= Check(
            !scheduler.IsStageCompleted(LxrWorkBucketStage::Initial),
            "executing initial stage is not completed");
        success &= Check(
            scheduler.GetStageOutstanding(LxrWorkBucketStage::Initial) == 1,
            "executing packet remains outstanding");
        success &= Check(!scheduler.IsStageOpen(LxrWorkBucketStage::Prepare), "prepare remains closed");
        success &= Check(
            state.executions[385].load(std::memory_order_relaxed) == 0,
            "prepare does not execute prematurely");

        state.block.store(false, std::memory_order_release);
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "blocking cycle completes");
        success &= Check(
            state.executions[384].load(std::memory_order_relaxed) == 1 &&
                state.executions[385].load(std::memory_order_relaxed) == 1,
            "blocked and later packets execute once");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateShutdownAndInvalidInputs()
    {
        LxrWorkScheduler scheduler;
        if (!Check(
            scheduler.Initialize(1) == LxrWorkSchedulerResult::Success,
            "shutdown scheduler initialize"))
        {
            return false;
        }

        ExecutionState state;
        std::vector<uintptr_t> references(4, 11);
        std::unique_ptr<LxrWorkPacket> queued = NewPacket(
            386,
            LxrWorkBucketStage::Prepare,
            references,
            &state);
        bool success = Check(
            scheduler.Submit(queued.get()) == LxrWorkSchedulerResult::Success,
            "submit packet for shutdown cancellation");
        success &= Check(
            queued->GetState() == LxrWorkPacketState::Queued,
            "closed-stage packet remains queued");
        success &= Check(
            scheduler.Shutdown() == LxrWorkSchedulerResult::Success,
            "shutdown cancels queued work");
        success &= Check(
            queued->GetState() == LxrWorkPacketState::CallerOwned &&
                queued->GetGeneration() == 0,
            "shutdown returns packet ownership");

        LxrWorkScheduler invalidScheduler;
        success &= Check(
            invalidScheduler.Initialize(1) == LxrWorkSchedulerResult::Success,
            "invalid-input scheduler initialize");
        LxrWorkPacket invalidStage(
            387,
            LxrWorkBucketStage::Count,
            references.data(),
            references.size(),
            ExecutePacket,
            &state);
        success &= Check(
            invalidScheduler.Submit(&invalidStage) == LxrWorkSchedulerResult::InvalidStage,
            "invalid stage rejected before indexing");
        LxrWorkPacket invalidReferences(
            388,
            LxrWorkBucketStage::Unconstrained,
            nullptr,
            1,
            ExecutePacket,
            &state);
        success &= Check(
            invalidScheduler.Submit(&invalidReferences) ==
                LxrWorkSchedulerResult::InvalidArgument,
            "null reference payload rejected");
        LxrWorkPacket invalidCallback(
            389,
            LxrWorkBucketStage::Unconstrained,
            references.data(),
            references.size(),
            nullptr,
            &state);
        success &= Check(
            invalidScheduler.Submit(&invalidCallback) ==
                LxrWorkSchedulerResult::InvalidArgument,
            "null callback rejected");
        invalidScheduler.Shutdown();
        return success;
    }
}

int main()
{
    bool success = true;
    success &= ValidateStageContract();
    success &= ValidatePacketization();
    success &= ValidateStealing();
    success &= ValidateOrderedBuckets();
    success &= ValidatePrematureOpening();
    success &= ValidateShutdownAndInvalidInputs();

    if (!success || s_passed != s_checks)
    {
        std::printf("FAIL: %d/%d P3.1 validation checks passed\n", s_passed, s_checks);
        return 1;
    }

    std::printf("PASS: %d/%d P3.1 validation checks\n", s_passed, s_checks);
    return 0;
}
